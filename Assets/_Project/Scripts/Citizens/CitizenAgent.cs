using System;
using CityBuilder.Buildings;
using CityBuilder.Grid;
using CityBuilder.Maps;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Citizens
{
    /// <summary>
    /// Per-citizen behavior: ambient wander when idle, or a building&lt;-&gt;resource-node commute
    /// loop when working (see CitizenVisualsManager, which switches agents between the two).
    /// </summary>
    public class CitizenAgent : MonoBehaviour
    {
        private const float WalkSpeed = 1.5f;
        private const float RoadSpeedMultiplier = 1.6f;
        private const float WanderRadiusCells = 8f;
        private const float MinIdleSeconds = 1f;
        private const float MaxIdleSeconds = 5f;
        private const float GroundRaycastHeight = 200f;
        private const float GroundHeightTolerance = 0.5f;
        private const float WorkPauseSeconds = 3f;
        private const float BuildingRestSeconds = 1.5f;
        private const float ArrivalThreshold = 0.05f;
        private const int MaxTargetAttempts = 8;
        // BuildRoute routes around known obstacles via NavMesh, but the bake/carve isn't a
        // perfect guarantee (e.g. an obstacle whose carved shape doesn't exactly match its solid
        // collider) -- if a leg still ends up blocked, CharacterController.Move just holds the
        // agent pinned there with zero progress toward _target. If the remaining distance hasn't
        // improved for this long, the destination is treated as unreachable from here instead of
        // leaving the agent stuck indefinitely.
        private const float StuckTimeoutSeconds = 1.5f;
        private const float StuckRetryPauseSeconds = 1f;
        private const float ProgressEpsilon = 0.02f;
        // A manual move order (see MoveTo) retries a blocked destination a bounded number of
        // times instead of forever like Working does -- Working's node/building is known
        // reachable (it was walked to before), but a player-clicked point might genuinely be
        // behind something with no way around it, and retrying that forever would just look like
        // the citizen froze rather than the game giving up on an impossible order.
        private const int MaxManualMoveStuckRetries = 3;

        private enum Mode { Wandering, Working, ManualMove }

        private Mode _mode = Mode.Wandering;
        private Vector3 _townCenter;

        private Vector3 _buildingPos;
        private Vector3? _nodePos;
        private bool _headingToNode;

        // Snapshot of what this agent was doing right before a manual move order (see MoveTo),
        // so it resumes exactly that -- idle wander from its new spot, or the interrupted job's
        // commute leg -- once it arrives (see OnArrived) instead of just stopping there forever.
        private Mode _resumeMode;
        private Vector3 _resumeTownCenter;
        private Vector3 _manualDestination;
        private int _manualStuckRetries;

        private Vector3 _target;
        private Vector3[] _route = { Vector3.zero };
        private int _routeIndex;
        private float _pauseTimer;
        private bool _isWalking;

        private float _bestRemainingDistance;
        private float _stuckTimer;
        private bool _resumingAfterStuck;

        private CharacterController _controller;

        // Set whenever SetWorking/SetIdleWander runs while OnWorkVisitCompleted is being
        // dispatched, so OnPauseElapsed can tell a subscriber already reassigned this agent
        // synchronously and skip its own (now-stale) default transition below.
        private bool _reassignedDuringCallback;

        /// <summary>Fired once per completed "at the node" work visit (Working mode only) -- e.g. drives tree felling in CitizenVisualsManager.</summary>
        public event Action OnWorkVisitCompleted;

        /// <summary>True only while idle-wandering -- see CitizenSelector, which restricts manual move orders to this so redirecting a citizen never desyncs a ProductionBuilding's worker count or a claimed ResourceNode.</summary>
        public bool IsIdle => _mode == Mode.Wandering;

        /// <summary>True from MoveTo until the agent either arrives or gives up (see MaxManualMoveStuckRetries) -- CitizenSelector uses this to know when to hide the persistent target-cell highlight.</summary>
        public bool IsManualMoving => _mode == Mode.ManualMove;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        /// <summary>
        /// First-time spawn: places the agent at spawnPosition (expected to be clear of any
        /// building's solid collider -- see CitizenVisualsManager.SpawnAgent, which no longer
        /// passes the Town Hall's own footprint center now that citizens carry a
        /// CharacterController) and starts it wandering around wanderCenter.
        /// </summary>
        public void Initialize(Vector3 spawnPosition, Vector3 wanderCenter)
        {
            transform.position = spawnPosition;
            SetIdleWander(wanderCenter);
        }

        /// <summary>Switches (or keeps) the agent to ambient wandering from wherever it currently is.</summary>
        public void SetIdleWander(Vector3 townCenter)
        {
            _reassignedDuringCallback = true;
            _resumingAfterStuck = false;
            _mode = Mode.Wandering;
            _townCenter = townCenter;
            BeginIdlePause();
        }

        /// <summary>
        /// Switches the agent into a building&lt;-&gt;node commute loop. If nodePos is null (e.g. a
        /// Food-producing building with no tree/rock concept) the agent just walks to the
        /// building and stays.
        /// </summary>
        public void SetWorking(Vector3 buildingPos, Vector3? nodePos)
        {
            _reassignedDuringCallback = true;
            _resumingAfterStuck = false;
            _mode = Mode.Working;
            _buildingPos = buildingPos;
            _nodePos = nodePos;
            _headingToNode = nodePos.HasValue;
            WalkTo(_headingToNode ? nodePos.Value : buildingPos);
        }

        /// <summary>
        /// Interrupts whatever this agent is currently doing and walks it to an explicit point
        /// (see CitizenSelector: click a citizen, then click a destination). Resumes the
        /// interrupted behavior on arrival -- see OnArrived's ManualMove branch.
        /// </summary>
        public void MoveTo(Vector3 destination)
        {
            if (_mode != Mode.ManualMove)
            {
                _resumeMode = _mode;
                _resumeTownCenter = _townCenter;
            }
            _reassignedDuringCallback = true;
            _resumingAfterStuck = false;
            _manualDestination = destination;
            _manualStuckRetries = 0;
            _mode = Mode.ManualMove;
            WalkTo(destination);
        }

        private void Update()
        {
            if (_isWalking)
            {
                // Horizontal-only distance/direction: SimpleMove's own gravity governs vertical
                // position (settling onto whatever collider is underneath), so comparing full 3D
                // distance against _target (whose Y is just a hint) could stall just short of
                // ArrivalThreshold if gravity has settled the agent at a slightly different height.
                var toTarget = _target - transform.position;
                toTarget.y = 0f;
                var distance = toTarget.magnitude;

                if (distance < ArrivalThreshold)
                {
                    _routeIndex++;
                    if (_routeIndex < _route.Length)
                    {
                        // Reached an intermediate waypoint (a NavMesh path corner, see
                        // BuildRoute) -- keep walking toward the next leg instead of treating
                        // this as arrival.
                        _target = _route[_routeIndex];
                        ResetStuckTracking();
                        return;
                    }

                    _isWalking = false;
                    OnArrived();
                    return;
                }

                if (distance < _bestRemainingDistance - ProgressEpsilon)
                {
                    _bestRemainingDistance = distance;
                    _stuckTimer = 0f;
                }
                else
                {
                    _stuckTimer += Time.deltaTime;
                    if (_stuckTimer >= StuckTimeoutSeconds)
                    {
                        OnStuck();
                        return;
                    }
                }

                var direction = toTarget / distance;
                var speed = CurrentWalkSpeed();
                if (_controller != null && _controller.enabled)
                {
                    // CharacterController collides with building colliders (see BuildingPlacer's
                    // procedurally generated BoxColliders), so citizens can no longer walk through
                    // a placed building the way a direct transform.position set would allow.
                    //
                    // Move() (not SimpleMove()) so gravity never touches Y -- the map is flat by
                    // design (GridManager.GroundHeight is one fixed constant used everywhere else:
                    // spawn points, wander targets, building placement) and letting the built-in
                    // CharacterController gravity settle onto the real Map1 mesh collider instead
                    // let citizens hover near buildings while gravity caught up, or fall through
                    // thin/seamed spots in that mesh. Y is pinned explicitly every frame instead.
                    _controller.Move(direction * speed * Time.deltaTime);
                    PinToGroundHeight();
                }
                else
                {
                    transform.position += direction * speed * Time.deltaTime;
                }
                return;
            }

            _pauseTimer -= Time.deltaTime;
            if (_pauseTimer <= 0f)
            {
                if (_resumingAfterStuck)
                {
                    _resumingAfterStuck = false;
                    var retryTarget = _mode == Mode.ManualMove
                        ? _manualDestination
                        : (_headingToNode ? _nodePos.Value : _buildingPos);
                    WalkTo(retryTarget);
                    return;
                }
                OnPauseElapsed();
            }
        }

        /// <summary>
        /// Abandons the current unreachable-from-here destination (see StuckTimeoutSeconds).
        /// Wandering just picks a fresh target on the next idle tick. Working retries the exact
        /// same leg after a short breather instead of toggling _headingToNode -- this wasn't a
        /// real arrival, so treating it as one would wrongly fire OnWorkVisitCompleted or send
        /// the agent back before it ever reached the node.
        /// </summary>
        private void OnStuck()
        {
            _isWalking = false;

            if (_mode == Mode.Wandering)
            {
                BeginIdlePause();
                return;
            }

            if (_mode == Mode.ManualMove)
            {
                _manualStuckRetries++;
                if (_manualStuckRetries > MaxManualMoveStuckRetries)
                {
                    // Genuinely unreachable from here -- stop retrying and resume whatever this
                    // agent was doing before the order instead of looking frozen forever.
                    _mode = _resumeMode;
                    if (_mode == Mode.Wandering)
                    {
                        _townCenter = _resumeTownCenter;
                        BeginIdlePause();
                    }
                    else
                    {
                        _pauseTimer = _headingToNode ? WorkPauseSeconds : BuildingRestSeconds;
                    }
                    return;
                }
            }

            _resumingAfterStuck = true;
            _pauseTimer = StuckRetryPauseSeconds;
        }

        private void ResetStuckTracking()
        {
            _bestRemainingDistance = float.MaxValue;
            _stuckTimer = 0f;
        }

        /// <summary>Terrain is flat by design -- clamps back to the one fixed ground height every move instead of trusting whatever Y the CharacterController's collision resolution landed on.</summary>
        private void PinToGroundHeight()
        {
            var grid = GridManager.Instance;
            if (grid == null) return;

            var pos = transform.position;
            pos.y = grid.GroundHeight;
            transform.position = pos;
        }

        /// <summary>Faster while standing on a road cell (RoadNetwork) -- see RoadSpeedMultiplier.</summary>
        private float CurrentWalkSpeed()
        {
            var grid = GridManager.Instance;
            var roads = RoadNetwork.Instance;
            if (grid != null && roads != null && roads.IsRoad(grid.WorldToCell(transform.position)))
            {
                return WalkSpeed * RoadSpeedMultiplier;
            }
            return WalkSpeed;
        }

        private void OnArrived()
        {
            if (_mode == Mode.ManualMove)
            {
                _mode = _resumeMode;
                if (_mode == Mode.Wandering)
                {
                    _townCenter = _resumeTownCenter;
                    BeginIdlePause();
                }
                else
                {
                    // Working: resume the interrupted commute leg from here.
                    _pauseTimer = _headingToNode ? WorkPauseSeconds : BuildingRestSeconds;
                }
                return;
            }

            if (_mode == Mode.Wandering)
            {
                BeginIdlePause();
                return;
            }

            // Working: pause at whichever end was just reached before looping to the other.
            _pauseTimer = _headingToNode ? WorkPauseSeconds : BuildingRestSeconds;
        }

        private void OnPauseElapsed()
        {
            if (_mode == Mode.Wandering)
            {
                PickNewWanderTarget();
                return;
            }

            if (!_nodePos.HasValue)
            {
                // No node to commute to (Food-type building) — just keep resting at the building.
                _pauseTimer = BuildingRestSeconds;
                return;
            }

            // The pause that just elapsed was the one spent AT the node -- one visit completed.
            if (_headingToNode)
            {
                _reassignedDuringCallback = false;
                OnWorkVisitCompleted?.Invoke();
                // A subscriber may have synchronously called SetWorking/SetIdleWander on this
                // same agent (e.g. felled the tree and reassigned it) -- if so, its fresh state
                // must not be immediately overwritten by the default transition below.
                if (_reassignedDuringCallback) return;
            }

            _headingToNode = !_headingToNode;
            WalkTo(_headingToNode ? _nodePos.Value : _buildingPos);
        }

        private void BeginIdlePause()
        {
            _pauseTimer = UnityEngine.Random.Range(MinIdleSeconds, MaxIdleSeconds);
        }

        private void WalkTo(Vector3 target)
        {
            _route = BuildRoute(transform.position, target);
            _routeIndex = 0;
            _target = _route[0];
            _isWalking = true;
            ResetStuckTracking();
        }

        // Reused across every BuildRoute call instead of allocating a fresh NavMeshPath each
        // time -- CalculatePath just overwrites its contents.
        private static readonly NavMeshPath SharedPath = new NavMeshPath();

        /// <summary>
        /// Routes around solid obstacles (buildings, trees) via the NavMesh baked in
        /// MeshMapApplier.BuildNavMesh, instead of the straight line this used to be -- those
        /// obstacles each carve themselves out of it dynamically (BuildingInstance/
        /// TreesAreaSpawner), so this always sees the current layout with no extra bookkeeping
        /// here. The road speed bonus (CurrentWalkSpeed) is unaffected either way -- it already
        /// just checks whatever cell the agent is currently standing on, not how the route was
        /// planned. Falls back to a direct line if no NavMesh exists yet or no path is found at
        /// all, so the agent still attempts something instead of never moving.
        /// </summary>
        private static Vector3[] BuildRoute(Vector3 from, Vector3 to)
        {
            if (NavMesh.CalculatePath(from, to, NavMesh.AllAreas, SharedPath) && SharedPath.corners.Length > 0)
            {
                var corners = SharedPath.corners;
                // corners[0] is (very close to) `from` itself -- drop it so the agent doesn't
                // spend a frame "arriving" at where it's already standing before starting to walk.
                if (corners.Length > 1 && (corners[0] - from).sqrMagnitude < 0.0001f)
                {
                    var trimmed = new Vector3[corners.Length - 1];
                    Array.Copy(corners, 1, trimmed, 0, trimmed.Length);
                    return trimmed;
                }
                return corners;
            }

            return new[] { to };
        }

        /// <summary>
        /// Idle citizens wander to a random point within WanderRadiusCells of the town center,
        /// picked once the current idle pause elapses (up to MaxIdleSeconds). Grid bounds/
        /// occupancy/water are cheap early-outs; a final downward raycast against the real scene
        /// geometry confirms solid ground actually exists there (hit height close to
        /// GridManager.GroundHeight -- a building or tree collider hit would land well above that
        /// tolerance and gets rejected) before committing to it as the new route.
        /// </summary>
        private void PickNewWanderTarget()
        {
            var grid = GridManager.Instance;
            if (grid == null)
            {
                BeginIdlePause();
                return;
            }

            for (var attempt = 0; attempt < MaxTargetAttempts; attempt++)
            {
                var offset = UnityEngine.Random.insideUnitCircle * (WanderRadiusCells * grid.CellSize);
                var candidate = _townCenter + new Vector3(offset.x, 0f, offset.y);
                var cell = grid.WorldToCell(candidate);

                if (!grid.IsWithinBounds(cell, Vector2Int.one) || !grid.IsAreaFree(cell, Vector2Int.one)) continue;
                if (MeshMapApplier.Instance != null && MeshMapApplier.Instance.IsWaterCell(cell)) continue;
                if (!TryFindWalkablePoint(grid, candidate, out var walkable)) continue;

                WalkTo(walkable);
                return;
            }

            // No free spot found nearby this cycle — wait and try again on the next idle tick.
            BeginIdlePause();
        }

        private static bool TryFindWalkablePoint(GridManager grid, Vector3 candidate, out Vector3 result)
        {
            result = new Vector3(candidate.x, grid.GroundHeight, candidate.z);

            var origin = new Vector3(candidate.x, GroundRaycastHeight, candidate.z);
            if (!Physics.Raycast(origin, Vector3.down, out var hit, GroundRaycastHeight * 2f)) return false;

            return Mathf.Abs(hit.point.y - grid.GroundHeight) <= GroundHeightTolerance;
        }
    }
}
