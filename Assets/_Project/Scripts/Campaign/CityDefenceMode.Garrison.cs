using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using CityBuilder.Grid;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Mission 2's living town (the user's first playtest, 2026-11-12): inside each gate a squad of its own
    /// (infantry, crossbows, a monk who heals), archers and crossbows on the walls beside it, patrols walking
    /// the lower town's streets, and the town's people running for their houses -- one runs to a door and is
    /// gone (hidden), and a while later another runs out of some other house. All of it fights or runs by
    /// itself; the player has only Ардан.
    /// </summary>
    public partial class CityDefenceMode
    {
        private readonly List<SoldierUnit> _garrison = new List<SoldierUnit>();
        private readonly List<(ArmyGroup group, Vector3[] route, int step)> _patrols = new List<(ArmyGroup, Vector3[], int)>();
        private float _patrolCheck;
        // the shooters on the walls: two posts each, walked between (the user's second playtest: «патрулировать стену»)
        private readonly List<(SoldierUnit unit, Vector2Int a, Vector2Int b, bool atB)> _wallmen = new List<(SoldierUnit, Vector2Int, Vector2Int, bool)>();
        private float _wallPatrolIn;
        // the garrison's pen: the town inside the outer walls (part 1) / the inner walls (part 2) -- nobody goes
        // out of the gates after the orcs (the user's second playtest)
        private Rect _pen;

        public int Guards => _garrison.Count;

        private SoldierUnit Hire(SoldierType type, Vector3 at, ArmyGroup group)
        {
            var army = ArmyManager.Instance;
            if (army == null) return null;
            var stats = SoldierStats.StatsAt(type, B.guardLevel);
            var unit = army.SpawnHireling(type, at, group);
            if (unit == null) return null;
            var ranged = unit.IsRanged;
            unit.SetHeroStats(stats.maxHealth, ranged ? Mathf.Max(1, Mathf.RoundToInt(stats.attackDamage * B.rangedDamageMult)) : stats.attackDamage);
            if (ranged) unit.RangedRangeFactor = B.rangedRangeMult;
            unit.SetCurrentHealth(stats.maxHealth);
            unit.Pen = _pen;
            _garrison.Add(unit);
            return unit;
        }

        /// <summary>A wall between an orc and a soldier (OrcUnit.WallBetween, the user's fourth playtest: «орки агрятся через
        /// стену и упираются в неё»): one inside the garrison's pen, the other outside it. A man up on the wall is
        /// left to OrcUnit's own rule (only a thrower reaches him). The orc's side gets 0.6 of a cell of slack: one in
        /// a broken gateway is through.</summary>
        private bool WallBetween(OrcUnit orc, SoldierUnit soldier)
        {
            if (_pen.width <= 0f || orc == null || soldier == null || soldier.OnWall) return false;
            var o = orc.transform.position;
            var s = soldier.transform.position;
            var orcIn = Breach(o) || o.x >= _pen.xMin - 0.6f && o.x <= _pen.xMax + 0.6f && o.z >= _pen.yMin - 0.6f && o.z <= _pen.yMax + 0.6f;
            return orcIn != _pen.Contains(new Vector2(s.x, s.z));
        }

        // where a piece of wall or a gate has fallen (the user's sixth playtest: orcs standing in the rubble of a wall
        // were not fought, being not yet inside)
        private readonly List<Vector3> _breaches = new List<Vector3>();
        private const float BreachReach = 1.8f;

        private void NoteBreach(BuildingInstance building)
        {
            if (building == null || building.Data == null || !(building.Data.connectsToFences || building.Data.isGate)) return;
            _breaches.Add(building.transform.position);
        }

        /// <summary>A point in the rubble of a fallen wall piece or gate, at the garrison pen's edge.</summary>
        private bool Breach(Vector3 at)
        {
            if (_breaches.Count == 0 || _pen.width <= 0f) return false;
            const float edge = 2.5f;
            if (at.x < _pen.xMin - edge || at.x > _pen.xMax + edge || at.z < _pen.yMin - edge || at.z > _pen.yMax + edge) return false;
            foreach (var b in _breaches)
                if (Flat(at - b) <= BreachReach) return true;
            return false;
        }

        private static Vector3 OnMesh(Vector3 at)
        {
            return NavMesh.SamplePosition(at, out var hit, 2.5f, NavMesh.AllAreas) ? hit.position : at;
        }

        private void ClearGarrison()
        {
            foreach (var unit in _garrison)
                if (unit != null) Destroy(unit.gameObject);
            _garrison.Clear();
            _patrols.Clear();
            _wallmen.Clear();
        }

        /// <summary>The gates' squads and the shooters on the walls beside them (outer: part 1; inner: part 2).</summary>
        private void Man(bool outer)
        {
            var b = B;
            _pen = PenOf(outer);
            // each gate (plan) and the way into the town from it
            // the ONE gate of the ring (the user's third playtest), the way into the town from it: south
            var gate = outer ? World(50.5f, 34.5f) : World(50.5f, 51.5f);
            var inward = new Vector3(0f, 0f, -1f);
            {
                const int lane = 1;
                var side = new Vector3(inward.z, 0f, -inward.x);
                if (outer)
                {
                    // the squad: infantry in front, the crossbows behind them, the monk behind those
                    Squad(SoldierType.ManAtArms, b.squadInfantry, OnMesh(gate + inward * 3f), side);
                    Squad(SoldierType.Crossbowman, b.squadCrossbows, OnMesh(gate + inward * 4.5f), side);
                    Squad(SoldierType.Monk, b.squadMonks, OnMesh(gate + inward * 5.5f), side);
                }
                else
                {
                    Squad(SoldierType.ManAtArms, b.innerGuards, OnMesh(gate + inward * 2.5f), side);
                }
                var shooters = outer ? b.wallArchers + b.wallCrossbows : b.innerArchers;
                var cells = WallCellsBeside(lane, outer, shooters);
                for (var i = 0; i < cells.Count; i++)
                {
                    var type = outer && i >= b.wallArchers ? SoldierType.Crossbowman : SoldierType.Archer;
                    var post = OnMesh(gate + inward * 2f);
                    var unit = Hire(type, post, new ArmyGroup(type, post));
                    if (unit == null) continue;
                    // facing out, towards the orcs (the user's second playtest)
                    unit.WallLookout = -inward;
                    unit.GoToWall(cells[i].a);
                    _wallmen.Add((unit, cells[i].a, cells[i].b, false));
                }
            }
        }

        private void Squad(SoldierType type, int count, Vector3 post, Vector3 side)
        {
            if (count <= 0) return;
            var group = new ArmyGroup(type, post);
            for (var k = 0; k < count; k++)
                Hire(type, OnMesh(post + side * ((k - (count - 1) * 0.5f) * 0.7f)), group);
            group.OrderMoveTo(post);
        }

        /// <summary>The town INSIDE the outer (or the inner) walls, the walls themselves left out, in world x/z: nobody
        /// steps into a gateway or onto the far side of it (the user's third playtest, again).</summary>
        private Rect PenOf(bool outer)
        {
            var a = outer ? World(29f, 66f) : World(42f, 66f);
            var c = outer ? World(72f, 35f) : World(59f, 52f);
            return Rect.MinMaxRect(Mathf.Min(a.x, c.x), Mathf.Min(a.z, c.z), Mathf.Max(a.x, c.x), Mathf.Max(a.z, c.z));
        }

        /// <summary>`count` wall posts on the gate's side, spread either side of it (not the gate's own three):
        /// each a cell and a second one m2_wall_patrol_cells further from the gate, walked between.</summary>
        private List<(Vector2Int a, Vector2Int b)> WallCellsBeside(int lane, bool outer, int count)
        {
            var found = new List<(Vector2Int, Vector2Int)>();
            if (count <= 0) return found;
            var candidates = new List<(Vector2Int, Vector2Int)>();
            var walk = Mathf.Max(1, B.wallPatrolCells);
            int fixedC, from, to, gateAt;
            bool vertical;
            if (outer)
            {
                if (lane == 0) { fixedC = 28; from = 35; to = 65; gateAt = 40; vertical = true; }
                else if (lane == 1) { fixedC = 34; from = 29; to = 71; gateAt = 50; vertical = false; }
                else { fixedC = 72; from = 35; to = 65; gateAt = 40; vertical = true; }
            }
            else
            {
                if (lane == 0) { fixedC = 41; from = 52; to = 65; gateAt = 56; vertical = true; }
                else if (lane == 1) { fixedC = 51; from = 42; to = 58; gateAt = 50; vertical = false; }
                else { fixedC = 59; from = 52; to = 65; gateAt = 56; vertical = true; }
            }
            // nearest the gate first, alternating sides
            // every other cell out from the gate, BOTH sides of it in turn (all the posts went to the left, the user's
            // seventh playtest: the candidates alternate sides, and taking every second one took one side only)
            for (var d = 2; d <= Mathf.Max(gateAt - from, to - gateAt); d += 2)
            {
                foreach (var a in new[] { gateAt - d, gateAt + d })
                {
                    if (a < from || a > to) continue;
                    var further = Mathf.Clamp(a < gateAt ? a - walk : a + walk, from, to);
                    candidates.Add(vertical ? (CellOf(fixedC, a), CellOf(fixedC, further)) : (CellOf(a, fixedC), CellOf(further, fixedC)));
                }
            }
            for (var i = 0; i < candidates.Count && found.Count < count; i++) found.Add(candidates[i]);
            return found;
        }

        // ------------------------------------------------------------------ the patrols

        /// <summary>The lower town's street loops (plan cells): the west half and the east half, between the two cross streets.</summary>
        private static readonly float[][] PatrolLoops =
        {
            new[] { 30.5f, 36.5f, 49.5f, 36.5f, 49.5f, 47.5f, 30.5f, 47.5f },
            new[] { 51.5f, 36.5f, 70.5f, 36.5f, 70.5f, 47.5f, 51.5f, 47.5f },
            new[] { 30.5f, 47.5f, 36.5f, 47.5f, 36.5f, 64.5f, 30.5f, 64.5f },
            new[] { 64.5f, 47.5f, 70.5f, 47.5f, 70.5f, 64.5f, 64.5f, 64.5f },
        };

        private void RaisePatrols()
        {
            var b = B;
            for (var p = 0; p < b.patrols && p < PatrolLoops.Length; p++)
            {
                var loop = PatrolLoops[p];
                var route = new Vector3[loop.Length / 2];
                for (var i = 0; i < route.Length; i++) route[i] = OnMesh(World(loop[2 * i], loop[2 * i + 1]));
                var group = new ArmyGroup(SoldierType.Spearman, route[0]);
                for (var k = 0; k < b.patrolSize; k++)
                    Hire(SoldierType.Spearman, OnMesh(route[0] + new Vector3(k * 0.6f, 0f, 0f)), group);
                group.OrderMoveTo(route[1]);
                _patrols.Add((group, route, 1));
            }
        }

        /// <summary>The shooters walk to their other post now and then while nobody is in reach.</summary>
        private void TickWallPatrol(float dt)
        {
            _wallPatrolIn -= dt;
            if (_wallPatrolIn > 0f) return;
            _wallPatrolIn = Mathf.Max(1f, B.wallPatrolSeconds) * Random.Range(0.4f, 0.7f);
            for (var i = _wallmen.Count - 1; i >= 0; i--)
            {
                var (unit, a, b, atB) = _wallmen[i];
                if (unit == null || unit.CurrentHealth <= 0)
                {
                    _wallmen.RemoveAt(i);
                    continue;
                }
                if (!unit.IdleOnWall || Random.value > 0.5f) continue;
                if (unit.GoToWall(atB ? a : b)) _wallmen[i] = (unit, a, b, !atB);
            }
        }

        private void TickGarrison(float dt)
        {
            _garrison.RemoveAll(u => u == null || u.CurrentHealth <= 0);
            TickWallPatrol(dt);
            _patrolCheck -= dt;
            if (_patrolCheck > 0f) return;
            _patrolCheck = 0.5f;
            for (var i = 0; i < _patrols.Count; i++)
            {
                var (group, route, step) = _patrols[i];
                if (group == null || group.Count == 0) continue;
                // the next corner once the patrol's middle is there (fighting holds them where they are)
                var mid = Vector3.zero;
                var n = 0;
                foreach (var unit in group.Members)
                {
                    if (unit == null) continue;
                    mid += unit.transform.position;
                    n++;
                }
                if (n == 0) continue;
                mid /= n;
                if (Flat(mid - route[step]) > 1.5f) continue;
                step = (step + 1) % route.Length;
                group.OrderMoveTo(route[step]);
                _patrols[i] = (group, route, step);
            }
        }

        // ------------------------------------------------------------------ the town's people

        private readonly List<(CitizenAgent who, Vector3 door)> _runners = new List<(CitizenAgent, Vector3)>();
        private readonly List<float> _comingOut = new List<float>();

        private void StartTownsfolk()
        {
            if (CitizenVisualsManager.Instance == null || _houses.Count < 2) return;
            for (var i = 0; i < B.townsfolk; i++) RunOne(midway: true);
        }

        private Vector3 DoorOf(BuildingInstance house)
        {
            var grid = GridManager.Instance;
            var footprint = house.RotatedFootprint();
            var centre = grid.GetFootprintCenterWorld(house.OriginCell, footprint);
            return OnMesh(new Vector3(centre.x, centre.y, centre.z - footprint.y * grid.CellSize * 0.5f - grid.CellSize * 0.4f));
        }

        private BuildingInstance AnyHouse()
        {
            for (var tries = 0; tries < 8; tries++)
            {
                var house = _houses[Random.Range(0, _houses.Count)];
                if (house != null && house.CurrentHealth > 0) return house;
            }
            return null;
        }

        /// <summary>One out of a house (or, at the start, already in the street) running for another house.</summary>
        private void RunOne(bool midway)
        {
            var from = AnyHouse();
            var to = AnyHouse();
            if (from == null || to == null || from == to) return;
            var start = DoorOf(from);
            var door = DoorOf(to);
            if (midway) start = OnMesh(Vector3.Lerp(start, door, Random.Range(0.1f, 0.7f)));
            var who = CitizenVisualsManager.Instance.SpawnLoose(start);
            if (who == null) return;
            who.MoveTo(door);
            _runners.Add((who, door));
        }

        private void TickTownsfolk(float dt)
        {
            for (var i = _runners.Count - 1; i >= 0; i--)
            {
                var (who, door) = _runners[i];
                if (who != null && Flat(who.transform.position - door) > 0.5f && who.CurrentActivity == CitizenAgent.Activity.Order) continue;
                // in at the door: hidden; another comes out somewhere else after a while
                if (who != null) Destroy(who.gameObject);
                _runners.RemoveAt(i);
                _comingOut.Add(B.townsfolkRespawnSeconds * Random.Range(0.5f, 1.5f));
            }
            for (var i = _comingOut.Count - 1; i >= 0; i--)
            {
                _comingOut[i] -= dt;
                if (_comingOut[i] > 0f) continue;
                _comingOut.RemoveAt(i);
                if (CitizenVisualsManager.Instance != null && _houses.Count > 1) RunOne(midway: false);
            }
        }
    }
}
