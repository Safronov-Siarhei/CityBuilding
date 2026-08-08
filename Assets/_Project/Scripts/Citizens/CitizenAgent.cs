using System;
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
        private float _pauseTimer;
        private bool _isWalking;

        // Set whenever SetWorking/SetIdleWander runs while OnWorkVisitCompleted is being
        // dispatched, so OnPauseElapsed can tell a subscriber already reassigned this agent
        // synchronously and skip its own (now-stale) default transition below.
        private bool _reassignedDuringCallback;

        /// <summary>Fired once per completed "at the node" work visit (Working mode only) -- e.g. drives tree felling in CitizenVisualsManager.</summary>
        public event Action OnWorkVisitCompleted;

        /// <summary>First-time spawn: places the agent at the town center and starts it wandering.</summary>
        public void Initialize(Vector3 townCenter)
        {
            transform.position = townCenter;
            SetIdleWander(townCenter);
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
                transform.position = Vector3.MoveTowards(transform.position, _target, WalkSpeed * Time.deltaTime);
                if (Vector3.Distance(transform.position, _target) < ArrivalThreshold)
                {
                    _isWalking = false;
                    OnArrived();
                }
                return;
            }

            _pauseTimer -= Time.deltaTime;
            if (_pauseTimer <= 0f)
            {
                OnPauseElapsed();
            }
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
            _target = target;
            _isWalking = true;
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
