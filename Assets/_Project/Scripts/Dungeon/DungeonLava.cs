using System.Collections.Generic;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Maps;
using UnityEngine;

namespace CityBuilder.Dungeon
{
    /// <summary>
    /// Dungeon 3's lava burns (the user, 2026-11-04: «при проходе по ней игрок получал урон от лавы
    /// и при касании других кусков лавы тоже»): a hero whose feet are under the lava's surface
    /// (DungeonFloor.Wading -- on this map the "water" is the lava: a river's bank, a crossing the
    /// floor dips under) or within a fire vent's round (the map's `vent` spots, their radius in
    /// the height column) or on the tide floor at high tide (DungeonTide) takes
    /// dungeon_lava_damage_per_second.
    /// </summary>
    public class DungeonLava : MonoBehaviour
    {
        private const float TickSeconds = 0.5f;

        private readonly List<Vector3> _vents = new List<Vector3>();   // x, z = where; y = radius
        private float _timer;
        private float _carry;

        public void Begin(MeshMapDefinition map)
        {
            foreach (var spot in map.SpotsOf("vent"))
                _vents.Add(new Vector3(spot.position.x, spot.height, spot.position.y));
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = TickSeconds;
            // whole points a tick, the fractions carried on so a low rate still adds up
            _carry += BalanceConfig.Instance.DungeonLavaDamagePerSecond * TickSeconds;
            var damage = Mathf.FloorToInt(_carry);
            if (damage <= 0) return;
            _carry -= damage;
            var all = SoldierUnit.All;
            for (var i = all.Count - 1; i >= 0; i--)
            {
                var soldier = all[i];
                if (soldier == null || soldier.CurrentHealth <= 0) continue;
                if (Burns(soldier.transform.position)) soldier.TakeDamage(damage);
            }
        }

        /// <summary>In the lava or by a fire vent. Public for the tests.</summary>
        public bool Burns(Vector3 position)
        {
            if (DungeonFloor.Wading(position)) return true;
            if (DungeonTide.Floods(position)) return true;
            foreach (var vent in _vents)
            {
                var dx = position.x - vent.x;
                var dz = position.z - vent.z;
                if (dx * dx + dz * dz < vent.y * vent.y) return true;
            }
            return false;
        }
    }
}
