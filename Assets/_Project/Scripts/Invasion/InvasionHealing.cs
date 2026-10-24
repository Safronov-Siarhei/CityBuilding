using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Invasion
{
    /// <summary>
    /// The Дом лекаря in a run (the user, 2026-10-24): a run has no illness, so its healer tends
    /// the wounded instead. Once built and manned (one worker), it heals every one of the player's
    /// own in its radius (service_radius) -- heroes, soldiers, villagers -- a little each second,
    /// the share growing with its level (inv_healer_heal_share, inv_healer_heal_share_per_level).
    /// </summary>
    public class InvasionHealing : MonoBehaviour
    {
        public const string HealerBuildingName = "HealerHouse";
        private const float TickSeconds = 1f;

        private readonly List<(Vector3 at, float radiusSq, float share)> _healers = new List<(Vector3, float, float)>();
        private float _tick;

        private void Update()
        {
            _tick -= Time.deltaTime;
            if (_tick > 0f) return;
            _tick = TickSeconds;

            FindHealers();
            if (_healers.Count == 0) return;

            foreach (var unit in SoldierUnit.All)
            {
                if (unit == null || unit.CurrentHealth <= 0 || unit.CurrentHealth >= unit.MaxHealth) continue;
                var share = ShareAt(unit.transform.position);
                if (share > 0f) unit.SetCurrentHealth(unit.CurrentHealth + Mathf.Max(1, Mathf.RoundToInt(unit.MaxHealth * share)));
            }

            var visuals = CitizenVisualsManager.Instance;
            if (visuals == null) return;
            var citizenHealth = BalanceConfig.Instance.InvasionCitizenHealth;
            foreach (var agent in visuals.AllAgents)
            {
                if (agent == null || agent.Wounds <= 0 || !agent.gameObject.activeInHierarchy) continue;
                var share = ShareAt(agent.transform.position);
                if (share > 0f) agent.Wounds = Mathf.Max(0, agent.Wounds - Mathf.Max(1, Mathf.RoundToInt(citizenHealth * share)));
            }
        }

        /// <summary>The strongest heal reaching `position`: healers do not add up, the best one in reach heals.</summary>
        private float ShareAt(Vector3 position)
        {
            var best = 0f;
            foreach (var (at, radiusSq, share) in _healers)
            {
                var to = position - at;
                to.y = 0f;
                if (to.sqrMagnitude <= radiusSq && share > best) best = share;
            }
            return best;
        }

        private void FindHealers()
        {
            _healers.Clear();
            var config = BalanceConfig.Instance;
            foreach (var instance in FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
            {
                if (instance.Data == null || instance.Data.buildingName != HealerBuildingName || !instance.IsOperational) continue;
                var radius = instance.ServiceRadius;
                if (radius <= 0) continue;
                var workplace = instance.GetComponent<ProductionBuilding>();
                // an unmanned healer's house is an empty house
                if (workplace == null || workplace.AssignedWorkers <= 0) continue;
                _healers.Add((instance.transform.position, radius * (float)radius, config.InvasionHealerHealShareAt(instance.Level)));
            }
        }
    }
}
