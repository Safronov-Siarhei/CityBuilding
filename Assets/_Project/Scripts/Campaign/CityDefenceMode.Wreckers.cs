using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Combat;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// The wreckers (the user, 2026-11-13): once the outer gate is broken a band of m2_wreckers orcs comes through it
    /// and goes from house to house breaking them -- ordinary buildings only, never a wall, a gate or a tower
    /// (OrcUnit.OnlyTarget). They are not a wave: no «!» on the minimap, the waves' count and the square's watch leave
    /// them out, Ардан gets no level from them, and they need not be killed. When the whole band is gone the next one
    /// comes m2_wreckers_respawn_sec later. While the inner gate stands they keep to the houses outside it.
    /// </summary>
    public partial class CityDefenceMode
    {
        private readonly HashSet<OrcUnit> _wreckers = new HashSet<OrcUnit>();
        private bool _wrecking;
        private float _wreckersIn, _wreckThink;

        private void StartWreckers()
        {
            if (_wrecking) return;
            _wrecking = true;
            _wreckersIn = 0f;
        }

        private void TickWreckers(float dt)
        {
            if (!_wrecking || Current != Stage.Part1) return;
            _wreckers.RemoveWhere(o => o == null || o.CurrentHealth <= 0);
            if (_wreckers.Count == 0)
            {
                _wreckersIn -= dt;
                if (_wreckersIn > 0f) return;
                _wreckersIn = Mathf.Max(1f, B.wreckersRespawnSeconds);
                SendWreckers();
                return;
            }
            _wreckThink -= dt;
            if (_wreckThink > 0f) return;
            _wreckThink = 0.5f;
            // the house each is after is down: on to the next one
            foreach (var orc in _wreckers)
            {
                var house = orc.OnlyTarget;
                if (house != null && house.CurrentHealth > 0) continue;
                orc.OnlyTarget = NextHouse(orc.transform.position);
            }
        }

        private void SendWreckers()
        {
            var raids = OrcRaidManager.Instance;
            if (raids == null) return;
            var b = B;
            var row = BalanceConfig.Instance.M2Wave(1, Mathf.Max(1, Wave));
            var level = row != null ? row.level : 1;
            // in through the broken outer gate
            var at = OnMesh(World(50.5f, NorthWallY + 2f));
            for (var i = 0; i < b.wreckers; i++)
            {
                var orc = raids.SpawnOrcOfKind(at + new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1f, 1f)), level, OrcKind.Raider);
                if (orc == null) continue;
                orc.Toughen(b.orcHealthMult, b.orcDamageMult);
                orc.LaneOnly = false;
                orc.FollowLane(null);
                orc.OnlyTarget = NextHouse(orc.transform.position);
                _wreckers.Add(orc);
            }
        }

        /// <summary>A house still standing near `from` (one of the nearest few, so the band spreads), or null.</summary>
        private BuildingInstance NextHouse(Vector3 from)
        {
            var pool = InnerGates[0] != null ? _lowerTown : _houses;
            BuildingInstance a = null, c = null;
            float da = float.MaxValue, dc = float.MaxValue;
            foreach (var house in pool)
            {
                if (house == null || house.CurrentHealth <= 0) continue;
                var d = Flat(house.transform.position - from);
                if (d < da) { c = a; dc = da; a = house; da = d; }
                else if (d < dc) { c = house; dc = d; }
            }
            return c != null && Random.value < 0.5f ? c : a;
        }
    }
}
