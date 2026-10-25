using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Invasion
{
    /// <summary>The Ice Shaman's power (InvasionNewYear): every few seconds the nearest working tower within reach freezes.</summary>
    public class IceShamanPower : MonoBehaviour
    {
        private float _clock;

        private void Update()
        {
            var config = BalanceConfig.Instance;
            _clock += Time.deltaTime;
            if (_clock < config.NyShamanFreezeEverySeconds) return;
            _clock = 0f;
            var tower = NearestTower(config.NyShamanFreezeRadius);
            if (tower != null) tower.Freeze(config.NyShamanFreezeSeconds);
        }

        /// <summary>The nearest tower not frozen yet within `radius`. Public for the tests.</summary>
        public DefensiveBuilding NearestTower(float radius)
        {
            DefensiveBuilding best = null;
            var bestSq = radius * radius;
            foreach (var tower in FindObjectsByType<DefensiveBuilding>(FindObjectsSortMode.None))
            {
                if (tower == null || tower.FrozenFor > 0f) continue;
                var d = tower.transform.position - transform.position;
                d.y = 0f;
                if (d.sqrMagnitude > bestSq) continue;
                bestSq = d.sqrMagnitude;
                best = tower;
            }
            return best;
        }
    }
}
