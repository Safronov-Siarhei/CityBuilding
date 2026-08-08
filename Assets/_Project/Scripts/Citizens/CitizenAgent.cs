using CityBuilder.Grid;
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

        /// <summary>First-time spawn: places the agent at the town center and starts it wandering.</summary>
        public void Initialize(Vector3 townCenter)
        {
            transform.position = townCenter;
            SetIdleWander(townCenter);
        }

        /// <summary>Switches (or keeps) the agent to ambient wandering from wherever it currently is.</summary>
        public void SetIdleWander(Vector3 townCenter)
        {
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

            _headingToNode = !_headingToNode;
            WalkTo(_headingToNode ? _nodePos.Value : _buildingPos);
        }

        private void BeginIdlePause()
        {
            _pauseTimer = Random.Range(MinIdleSeconds, MaxIdleSeconds);
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
                var offset = Random.insideUnitCircle * (WanderRadiusCells * grid.CellSize);
                var candidate = _townCenter + new Vector3(offset.x, 0f, offset.y);
                var cell = grid.WorldToCell(candidate);

                if (!grid.IsWithinBounds(cell, Vector2Int.one) || !grid.IsAreaFree(cell, Vector2Int.one)) continue;

                WalkTo(new Vector3(candidate.x, grid.GroundHeight, candidate.z));
                return;
            }

            // No free spot found nearby this cycle — wait and try again on the next idle tick.
            BeginIdlePause();
        }
    }
}
