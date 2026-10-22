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
            if (_buildingInstance == null || _buildingInstance.Data == null || _buildingInstance.Defense <= 0) return;
            // not while it is being built or upgraded (ConstructionSite)
            if (!_buildingInstance.IsOperational) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = _attackIntervalSeconds;

            // the Data is set after Awake, so the range is asked here
            _attackRangeMeters = RangeOf(_buildingInstance);

            var target = FindNearestOrcInRange();
            if (target == null) return;
            // Seen flying from the parapet and landing (see CombatProjectiles); the damage lands with it.
            CombatProjectiles.Fire(ProjectileKind.Arrow, CombatProjectiles.MuzzleOf(this, target.transform.position), target, _buildingInstance.Defense);
        }

        /// <summary>How far a building shoots: the sheet's defence radius, castle_attack_range_mult further for the main building (the castle, the Town Hall -- the user, 2026-10-22: +50%).</summary>
        public static float RangeOf(BuildingInstance building)
        {
            var range = BalanceConfig.Instance.DefenceAttackRangeMeters;
            if (building != null && building.Data != null && building.Data.buildingName == BuildingIds.TownHall)
                range *= BalanceConfig.Instance.CastleAttackRangeMultiplier;
            return range;
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
