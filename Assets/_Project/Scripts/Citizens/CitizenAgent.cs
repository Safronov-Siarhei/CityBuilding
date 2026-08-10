using System;
using CityBuilder.Buildings;
using CityBuilder.Grid;
using CityBuilder.Maps;
using UnityEngine;

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
        // How far (in cells) a citizen "looks" for a road to detour onto before giving up and
        // walking straight there -- a lightweight stand-in for real pathfinding across the road
        // network: it finds the nearest road near the start and near the destination and routes
        // through both, rather than searching a path along the road graph itself.
        private const int RoadSearchRadiusCells = 6;
        private const float WanderRadiusCells = 8f;
        private const float MinIdleSeconds = 1f;
        private const float MaxIdleSeconds = 3f;
        private const float WorkPauseSeconds = 3f;
        private const float BuildingRestSeconds = 1.5f;
        private const float ArrivalThreshold = 0.05f;
        private const int MaxTargetAttempts = 8;

        private enum Mode { Wandering, Working }

        private Mode _mode = Mode.Wandering;
        private Vector3 _townCenter;

        private Vector3 _buildingPos;
        private Vector3? _nodePos;
        private bool _headingToNode;

        private Vector3 _target;
        private Vector3[] _route = { Vector3.zero };
        private int _routeIndex;
        private float _pauseTimer;
        private bool _isWalking;

        private CharacterController _controller;

        // Set whenever SetWorking/SetIdleWander runs while OnWorkVisitCompleted is being
        // dispatched, so OnPauseElapsed can tell a subscriber already reassigned this agent
        // synchronously and skip its own (now-stale) default transition below.
        private bool _reassignedDuringCallback;

        /// <summary>Fired once per completed "at the node" work visit (Working mode only) -- e.g. drives tree felling in CitizenVisualsManager.</summary>
        public event Action OnWorkVisitCompleted;

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
            _mode = Mode.Working;
            _buildingPos = buildingPos;
            _nodePos = nodePos;
            _headingToNode = nodePos.HasValue;
            WalkTo(_headingToNode ? nodePos.Value : buildingPos);
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
                        // Reached an intermediate waypoint (a road detour, see BuildRoute) --
                        // keep walking toward the next leg instead of treating this as arrival.
                        _target = _route[_routeIndex];
                        return;
                    }

                    _isWalking = false;
                    OnArrived();
                    return;
                }

                var direction = toTarget / distance;
                var speed = CurrentWalkSpeed();
                if (_controller != null && _controller.enabled)
                {
                    // CharacterController collides with building colliders (see BuildingPlacer's
                    // procedurally generated BoxColliders), so citizens can no longer walk through
                    // a placed building the way a direct transform.position set would allow.
                    _controller.SimpleMove(direction * speed);
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
                OnPauseElapsed();
            }
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
        }

        /// <summary>
        /// If there's a road within RoadSearchRadiusCells of both the start and the destination,
        /// routes through the nearest road cell at each end (start -&gt; road near start -&gt; road
        /// near destination -&gt; destination) instead of a straight line, so the agent picks up the
        /// road speed bonus for the middle of the trip. Falls back to a direct single-leg route
        /// when no nearby road exists at either end, or when both ends resolve to the same cell
        /// (already effectively on/at the road).
        /// </summary>
        private static Vector3[] BuildRoute(Vector3 from, Vector3 to)
        {
            var grid = GridManager.Instance;
            var roads = RoadNetwork.Instance;
            if (grid == null || roads == null) return new[] { to };

            var startRoad = FindNearestRoadCell(grid, roads, from);
            var endRoad = FindNearestRoadCell(grid, roads, to);
            if (!startRoad.HasValue || !endRoad.HasValue || startRoad.Value == endRoad.Value)
            {
                return new[] { to };
            }

            var startRoadPos = grid.GetFootprintCenterWorld(startRoad.Value, Vector2Int.one);
            var endRoadPos = grid.GetFootprintCenterWorld(endRoad.Value, Vector2Int.one);
            return new[] { startRoadPos, endRoadPos, to };
        }

        private static Vector2Int? FindNearestRoadCell(GridManager grid, RoadNetwork roads, Vector3 worldPos)
        {
            var center = grid.WorldToCell(worldPos);
            Vector2Int? nearest = null;
            var nearestDistSq = int.MaxValue;

            for (var dx = -RoadSearchRadiusCells; dx <= RoadSearchRadiusCells; dx++)
            {
                for (var dz = -RoadSearchRadiusCells; dz <= RoadSearchRadiusCells; dz++)
                {
                    var cell = center + new Vector2Int(dx, dz);
                    if (!roads.IsRoad(cell)) continue;

                    var distSq = dx * dx + dz * dz;
                    if (distSq < nearestDistSq)
                    {
                        nearestDistSq = distSq;
                        nearest = cell;
                    }
                }
            }

            return nearest;
        }

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

                WalkTo(new Vector3(candidate.x, grid.GroundHeight, candidate.z));
                return;
            }

            // No free spot found nearby this cycle — wait and try again on the next idle tick.
            BeginIdlePause();
        }
    }
}
