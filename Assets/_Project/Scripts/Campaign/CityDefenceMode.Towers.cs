using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Combat;
using CityBuilder.Grid;
using UnityEngine;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// The gatehouses' towers shoot (the user's eighth playtest: «в чём смысл башен если они не стреляют?»). The town's
    /// only towers are the end towers of the two Barbicans -- WallWalk's stair cells; each, while its gate stands,
    /// looses an arrow every m2_gate_tower_shot_sec at the nearest orc within m2_gate_tower_range_cells for m2_gate_tower_damage.
    /// </summary>
    public partial class CityDefenceMode
    {
        private readonly List<Vector3> _towerTops = new List<Vector3>();
        private readonly List<float> _towerIn = new List<float>();
        private int _towerVersion = -1;
        private readonly List<Vector2Int> _stairs = new List<Vector2Int>();

        private void TickTowerShots(float dt)
        {
            var grid = GridManager.Instance;
            if (grid == null) return;
            WallWalk.Ensure();
            if (_towerVersion != WallWalk.Version)
            {
                _towerVersion = WallWalk.Version;
                CollectTowers(grid);
            }
            var b = B;
            var reach = b.gateTowerRangeCells * grid.CellSize;
            for (var i = 0; i < _towerTops.Count; i++)
            {
                _towerIn[i] -= dt;
                if (_towerIn[i] > 0f) continue;
                var from = _towerTops[i];
                OrcUnit best = null;
                var bestD = reach;
                foreach (var orc in OrcUnit.All)
                {
                    if (orc == null || orc.CurrentHealth <= 0 || orc.Untargetable) continue;
                    var d = Flat(orc.transform.position - from);
                    if (d >= bestD) continue;
                    bestD = d;
                    best = orc;
                }
                if (best == null)
                {
                    _towerIn[i] = 0.3f;
                    continue;
                }
                _towerIn[i] = Mathf.Max(0.2f, b.gateTowerShotSeconds);
                CombatProjectiles.Fire(ProjectileKind.Arrow, from, best, Mathf.Max(1, b.gateTowerDamage));
            }
        }

        /// <summary>One shooting point per end tower: the stair cells, those side by side merged.</summary>
        private void CollectTowers(GridManager grid)
        {
            _towerTops.Clear();
            _towerIn.Clear();
            WallWalk.StairsInto(_stairs);
            foreach (var cell in _stairs)
            {
                if (WallWalk.IsDockStair(cell)) continue;
                var top = WallWalk.Top(cell) + Vector3.up * (0.9f * grid.CellSize);
                var near = false;
                foreach (var t in _towerTops)
                    if (Flat(t - top) < 1.6f * grid.CellSize) { near = true; break; }
                if (near) continue;
                _towerTops.Add(top);
                _towerIn.Add(Random.Range(0f, 1f));
            }
        }
    }
}
