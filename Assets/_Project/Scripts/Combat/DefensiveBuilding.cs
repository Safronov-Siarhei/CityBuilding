using CityBuilder.Core;
using CityBuilder.Buildings;
using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// Attached (see SetupProject.CreateBuildingData) to every building with a real
    /// BuildingData.defense stat -- currently Wall/Tower/Barracks/Gate. Periodically attacks the
    /// nearest OrcUnit within range, using the building's own defense value as attack damage --
    /// the same stat HappinessManager already reads for the defense-coverage score, so a
    /// higher-defense settlement is both "happier" and literally hits harder.
    /// </summary>
    public class DefensiveBuilding : MonoBehaviour
    {
        // Both come from the balance sheet's economy tab, cached at Awake. The range is in
        // world-space metres, not cells -- GridManager.CellSize is 1f in this project so the two
        // happen to match, but this is deliberately independent of that.
        private float _attackIntervalSeconds;
        private float _attackRangeMeters;

        private BuildingInstance _buildingInstance;
        private float _timer;

        private void Awake()
        {
            _buildingInstance = GetComponent<BuildingInstance>();
            _attackIntervalSeconds = BalanceConfig.Instance.DefenceAttackIntervalSeconds;
            _attackRangeMeters = BalanceConfig.Instance.DefenceAttackRangeMeters;
        }

        private void Update()
        {
            if (_buildingInstance == null || _buildingInstance.Data == null || _buildingInstance.Data.defense <= 0) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = _attackIntervalSeconds;

            var target = FindNearestOrcInRange();
            target?.TakeDamage(_buildingInstance.Data.defense);
        }

        private OrcUnit FindNearestOrcInRange()
        {
            OrcUnit nearest = null;
            var nearestDistSq = _attackRangeMeters * _attackRangeMeters;

            foreach (var orc in OrcUnit.All)
            {
                if (orc == null) continue;
                var distSq = (orc.transform.position - transform.position).sqrMagnitude;
                if (distSq > nearestDistSq) continue;

                nearestDistSq = distSq;
                nearest = orc;
            }

            return nearest;
        }
    }
}
