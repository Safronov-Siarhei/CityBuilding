using CityBuilder.Grid;
using UnityEngine;

namespace CityBuilder.Citizens
{
    /// <summary>
    /// Purely ambient wander behavior for a spawned citizen visual — picks a random nearby
    /// point, walks to it, pauses, repeats. No task assignment or pathfinding yet; see
    /// CitizenVisualsManager for the phase this belongs to.
    /// </summary>
    public class CitizenAgent : MonoBehaviour
    {
        private const float WalkSpeed = 1.5f;
        private const float WanderRadiusCells = 8f;
        private const float MinIdleSeconds = 1f;
        private const float MaxIdleSeconds = 3f;
        private const float ArrivalThreshold = 0.05f;
        private const int MaxTargetAttempts = 8;

        private Vector3 _townCenter;
        private Vector3 _target;
        private float _idleTimer;
        private bool _isWalking;

        public void Initialize(Vector3 townCenter)
        {
            _townCenter = townCenter;
            transform.position = townCenter;
            BeginIdle();
        }

        private void Update()
        {
            if (_isWalking)
            {
                transform.position = Vector3.MoveTowards(transform.position, _target, WalkSpeed * Time.deltaTime);
                if (Vector3.Distance(transform.position, _target) < ArrivalThreshold)
                {
                    BeginIdle();
                }
                return;
            }

            _idleTimer -= Time.deltaTime;
            if (_idleTimer <= 0f)
            {
                PickNewTarget();
            }
        }

        private void BeginIdle()
        {
            _isWalking = false;
            _idleTimer = Random.Range(MinIdleSeconds, MaxIdleSeconds);
        }

        private void PickNewTarget()
        {
            var grid = GridManager.Instance;
            if (grid == null)
            {
                BeginIdle();
                return;
            }

            for (var attempt = 0; attempt < MaxTargetAttempts; attempt++)
            {
                var offset = Random.insideUnitCircle * (WanderRadiusCells * grid.CellSize);
                var candidate = _townCenter + new Vector3(offset.x, 0f, offset.y);
                var cell = grid.WorldToCell(candidate);

                if (!grid.IsWithinBounds(cell, Vector2Int.one) || !grid.IsAreaFree(cell, Vector2Int.one)) continue;

                _target = new Vector3(candidate.x, grid.GroundHeight, candidate.z);
                _isWalking = true;
                return;
            }

            // No free spot found nearby this cycle — wait and try again on the next idle tick.
            BeginIdle();
        }
    }
}
