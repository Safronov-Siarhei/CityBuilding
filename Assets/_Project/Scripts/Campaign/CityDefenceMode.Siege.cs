using System.Collections;
using System.Collections.Generic;
using CityBuilder.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// The orcs' SIEGE MACHINES in mission 2 (models approved 2026-11-12). Which wave brings them, and on which road, is
    /// the m2waves tab's `rams_*` / `towers_*` columns:
    ///   * a RAM -- a brute in the ram's body: slow, very tough (it must reach the gate and hold there a while unless
    ///     Ардан steps in, the user's fourth playtest), never turns on the fighters around it, and strikes the gate its
    ///     road ends at very hard;
    ///   * a SIEGE TOWER -- slow and very tough, ignores the fighters too; it LEADS its road (the user, 2026-11-13: «она
    ///     должна ехать первая со стороны атаки, а остальные солдаты должны идти за ней»): that road's other orcs of the
    ///     wave walk behind it at its pace. It rolls to a section of the wall beside the gate, stands against it for
    ///     m2_tower_stay_sec (still to be broken), and its crew -- then every escort that reaches its foot -- climb it one
    ///     by one: up the tower, over onto the wall-walk, and down into the town. Broken on the way, or once it has
    ///     stood its time, the escorts left go on along their road to the gate.
    /// Part 2 docks at the inner wall's north side. Numbers: the m2_ram_* / m2_tower_* / m2_siege_* keys.
    /// </summary>
    public partial class CityDefenceMode
    {
        [SerializeField] private GameObject orcRamModel;     // Models/Orcs/OrcRam.fbx
        [SerializeField] private GameObject orcTowerModel;   // Models/Orcs/OrcSiegeTower.fbx

        private class Tower
        {
            public OrcUnit orc;
            public int lane;
            public float wallX, wallY;
            public OrcLane road;
            public readonly List<OrcUnit> escorts = new List<OrcUnit>();
            public bool docked;
            public float until, nextClimb;
            public Vector3 foot, deck, walk, drop;
            // a tower the wall's own body keeps off its spot docks where it stuck (the user's sixth playtest: it rolled
            // up to the wall and did nothing)
            public Vector3 lastAt;
            public float stillFor;
            // the wall-walk cell it leans on (its climbers step onto it) and its own cell, a stair for the defenders;
            // onWall false: no walk cell found, its climbers jump straight in as before
            public Vector2Int wallCell, towerCell;
            public bool onWall;
        }

        private readonly List<Tower> _towers = new List<Tower>();
        private int _towerSide;

        // the north side of the outer (part 1) / inner (part 2) wall, plan y; a tower stands this far out in front of it,
        // the deck as high as the walk
        private const float NorthWallY = 34f, InnerNorthWallY = 51f;
        private const float TowerStandOff = 1.1f;
        private const float WalkHeight = 1.15f;
        // an escort this near the docked tower's foot climbs it
        private const float FootReach = 3.5f;
        // stuck this near its spot, it docks there
        private const float StuckDockCells = 5f;
        // the drawbridge drops onto the wall-walk in this long
        private const float BridgeDropSeconds = 0.7f;

        /// <summary>A ram or a tower on road `lane`; the tower's record (escorts join it), null for a ram.</summary>
        private Tower SpawnMachine(bool ram, int lane, int level)
        {
            var raids = OrcRaidManager.Instance;
            if (raids == null || _lanes[lane] == null) return null;
            var b = B;
            var orc = raids.SpawnOrcOfKind(_spawns[lane], level, OrcKind.Brute);
            if (orc == null) return null;
            orc.name = ram ? "OrcRam" : "OrcSiegeTower";
            orc.Toughen(b.orcHealthMult * (ram ? b.ramHealthMult : b.towerHealthMult),
                b.orcDamageMult * (ram ? b.ramDamageMult : 0.01f));
            orc.Slow(b.siegeSpeed);
            orc.IgnoresSoldiers = true;
            orc.LaneOnly = true;
            Dress(orc, ram ? orcRamModel : orcTowerModel);
            _laneOrcs[lane].Add(orc);
            if (ram)
            {
                orc.FollowLane(_lanes[lane]);
                orc.OnlyTarget = OuterGates[lane];
                return null;
            }
            // the tower's own road: the lane to the gate's front, then along the wall to a section either side of the gate
            // the outer gate broken: the towers go for the inner wall
            var inner = OuterGates[0] == null && InnerGates[0] != null;
            var side = (_towerSide++ % 2 == 0) ? -1f : 1f;
            var from = b.towerWallFromCells;
            var to = inner ? Mathf.Min(b.towerWallToCells, 7f) : b.towerWallToCells;
            var wallX = 50.5f + side * Random.Range(Mathf.Min(from, to), to);
            var wallY = inner ? InnerNorthWallY : NorthWallY;
            var road = TowerRoad(lane, wallX, wallY);
            orc.FollowLane(road);
            orc.NeverStrikes = true;
            var tower = new Tower { orc = orc, lane = lane, wallX = wallX, wallY = wallY, road = road };
            _towers.Add(tower);
            return tower;
        }

        /// <summary>An orc of the tower's road walks behind it at its pace, to climb it at the wall.</summary>
        private void Escort(Tower tower, OrcUnit orc)
        {
            orc.FollowLane(tower.road);
            orc.Slow(B.siegeSpeed);
            // set off one after another, behind the tower
            orc.BusyFor = Mathf.Max(orc.BusyFor, 1.2f + tower.escorts.Count * 0.6f);
            tower.escorts.Add(orc);
        }

        private OrcLane TowerRoad(int lane, float wallX, float wallY)
        {
            var p = _plan.lanes[lane].points;
            var corners = new List<Vector3>();
            var front = wallY - TowerStandOff - 1.3f;
            for (var k = 0; k + 1 < p.Length; k += 2)
            {
                if (p[k + 1] > front) break;   // up to the gate's front, not through it
                corners.Add(World(p[k], p[k + 1]));
            }
            corners.Add(World(Mathf.Lerp(50.5f, wallX, 0.5f), front));
            corners.Add(World(wallX, wallY - TowerStandOff));
            return new OrcLane(corners.ToArray());
        }

        /// <summary>The brute's own figure hidden, the machine's model in its place (models are in cells = metres). The
        /// models face the other way from the orc's forward (they rolled back first, the user's fourth playtest).</summary>
        private static void Dress(OrcUnit orc, GameObject model)
        {
            if (model == null) return;
            foreach (var r in orc.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            var go = Instantiate(model, orc.transform, false);
            go.name = model.name;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            go.transform.localScale = model.transform.localScale / Mathf.Max(0.01f, orc.transform.lossyScale.x);
        }

        private void TickSiege(float dt)
        {
            for (var i = _towers.Count - 1; i >= 0; i--)
            {
                var t = _towers[i];
                t.escorts.RemoveAll(e => e == null || e.CurrentHealth <= 0);
                if (t.orc == null || t.orc.CurrentHealth <= 0)
                {
                    if (t.orc != null) Buildings.WallWalk.Undock(t.orc);
                    ReleaseEscorts(t);
                    _towers.RemoveAt(i);
                    continue;
                }
                if (!t.docked)
                {
                    // its road's orcs keep BEHIND it (the user's seventh playtest: they walked ahead and hit the wall)
                    HoldEscorts(t);
                    var spot = World(t.wallX, t.wallY - TowerStandOff);
                    var at = t.orc.transform.position;
                    var near = Flat(at - spot);
                    t.stillFor = Flat(at - t.lastAt) < 0.25f * dt ? t.stillFor + dt : 0f;
                    t.lastAt = at;
                    if (near <= Mathf.Max(B.towerDockCells, 1.2f) || (near <= StuckDockCells && t.stillFor >= 1.2f)) Dock(t);
                    continue;
                }
                // standing against the wall: it does nothing but stand (and can still be broken)
                t.orc.BusyFor = 1f;
                if (Time.time >= t.nextClimb)
                {
                    foreach (var e in t.escorts)
                    {
                        if (Flat(e.transform.position - t.foot) > FootReach) continue;
                        t.escorts.Remove(e);
                        StartCoroutine(Climb(e, t, 0f, true));
                        t.nextClimb = Time.time + B.towerClimbGapSeconds;
                        break;
                    }
                }
                if (Time.time < t.until) continue;
                // its time is up: the tower is left behind, the escorts still on the ground go for the gate
                ReleaseEscorts(t);
                Buildings.WallWalk.Undock(t.orc);
                _laneOrcs[t.lane].Remove(t.orc);
                Destroy(t.orc.gameObject);
                _towers.RemoveAt(i);
            }
        }

        /// <summary>An escort level with its rolling tower or ahead of it waits a moment (nearer its dock spot = ahead).</summary>
        private void HoldEscorts(Tower t)
        {
            var spot = World(t.wallX, t.wallY - TowerStandOff);
            var tower = Flat(t.orc.transform.position - spot);
            foreach (var e in t.escorts)
            {
                if (e == null) continue;
                if (Flat(e.transform.position - spot) < tower + EscortGap) e.BusyFor = Mathf.Max(e.BusyFor, 0.3f);
            }
        }

        // an escort keeps this far (cells) behind its tower
        private const float EscortGap = 1.5f;

        private void ReleaseEscorts(Tower t)
        {
            foreach (var e in t.escorts)
            {
                if (e == null || e.CurrentHealth <= 0) continue;
                e.Slow(1f / Mathf.Max(0.05f, B.siegeSpeed));
                if (_lanes[t.lane] != null) e.FollowLane(_lanes[t.lane]);
            }
            t.escorts.Clear();
        }

        /// <summary>Against the wall: the tower stops, turned to the wall, and its crew climb it into the town.</summary>
        private void Dock(Tower t)
        {
            t.docked = true;
            var ground = t.orc.transform.position.y;
            t.foot = t.orc.transform.position;
            t.orc.FollowLane(null);
            t.orc.BusyFor = 1f;
            // the drawbridge side towards the wall (south: plan y grows to the south, world z shrinks)
            t.orc.transform.rotation = Quaternion.LookRotation(Vector3.back);
            t.until = Time.time + B.towerStaySeconds + B.towerCrew * B.towerClimbGapSeconds;
            t.deck = World(t.wallX, t.wallY - 0.35f);
            t.deck.y = ground + WalkHeight;
            t.walk = World(t.wallX, t.wallY + 0.5f);
            t.walk.y = ground + WalkHeight;
            t.drop = OnMesh(World(t.wallX, t.wallY + 2.2f));
            // the wall-walk cell where it actually stands (it may have stuck short of its spot): its climbers step onto
            // the wall THERE and fight (the user's seventh playtest), and it is a stair for the defenders too
            var grid = Grid.GridManager.Instance;
            if (grid != null)
            {
                var cell = grid.CellSize;
                for (var k = 1; k <= 4 && !t.onWall; k++)
                {
                    var c = grid.WorldToCell(t.foot + Vector3.back * (k * cell));
                    if (!Buildings.WallWalk.IsWalk(c) || Buildings.WallWalk.IsStair(c)) continue;
                    t.wallCell = c;
                    t.onWall = true;
                }
                if (t.onWall)
                {
                    var top = Buildings.WallWalk.Top(t.wallCell);
                    t.towerCell = grid.WorldToCell(top + Vector3.forward * cell);
                    t.walk = top;
                    t.deck = top + Vector3.forward * (0.6f * cell);
                    t.drop = OnMesh(top + Vector3.back * (2.2f * cell));
                    Buildings.WallWalk.Dock(t.orc, t.towerCell, t.wallCell, top.y);
                }
            }
            // the wooden drawbridge falls onto the wall first (the user's sixth playtest), then the crew climb (they rode
            // inside), then the escorts as they reach its foot
            StartCoroutine(LowerBridge(t.orc.transform, Vector3.back));
            t.until += BridgeDropSeconds;
            t.nextClimb = Time.time + BridgeDropSeconds + B.towerCrew * B.towerClimbGapSeconds;
            var raids = OrcRaidManager.Instance;
            if (raids == null) return;
            for (var k = 0; k < B.towerCrew; k++)
            {
                var orc = raids.SpawnOrcOfKind(t.foot, t.orc.Level, OrcKind.Raider);
                if (orc == null) continue;
                orc.Toughen(B.orcHealthMult, B.orcDamageMult);
                _laneOrcs[t.lane].Add(orc);
                StartCoroutine(Climb(orc, t, BridgeDropSeconds + k * B.towerClimbGapSeconds, false));
            }
        }

        /// <summary>One climbing the tower: waits his turn (inside it, unseen and out of reach), climbs to the deck,
        /// crosses onto the wall-walk, jumps down inside. An escort then drops his road and his tower's pace.</summary>
        private IEnumerator Climb(OrcUnit orc, Tower tower, float wait, bool escort)
        {
            var foot = tower.foot;
            var deck = tower.deck;
            var walk = tower.walk;
            var drop = tower.drop;
            var body = orc.GetComponent<CharacterController>();
            if (body != null) body.enabled = false;
            orc.enabled = false;
            orc.Untargetable = true;
            var looks = orc.GetComponentsInChildren<Renderer>();
            foreach (var r in looks) r.enabled = false;
            orc.transform.position = foot;
            for (var t = 0f; t < wait && orc != null; t += Time.deltaTime) yield return null;
            if (orc == null) yield break;
            foreach (var r in looks) if (r != null) r.enabled = true;
            orc.transform.rotation = Quaternion.LookRotation(Vector3.back);
            var climb = Mathf.Max(0.2f, B.towerClimbSeconds);
            var legs = tower.onWall
                ? new[] { (foot, deck, climb, 0f), (deck, walk, climb * 0.5f, 0.15f) }
                : new[] { (foot, deck, climb, 0f), (deck, walk, climb * 0.5f, 0.15f), (walk, drop, climb * 0.5f, 0.35f) };
            foreach (var (a, b, seconds, hop) in legs)
            {
                for (var t = 0f; t < seconds; t += Time.deltaTime)
                {
                    if (orc == null || orc.CurrentHealth <= 0) yield break;
                    var k = t / seconds;
                    orc.transform.position = Vector3.Lerp(a, b, k) + Vector3.up * (hop * 4f * k * (1f - k));
                    yield return null;
                }
            }
            if (orc == null) yield break;
            if (escort) orc.Slow(1f / Mathf.Max(0.05f, B.siegeSpeed));
            if (tower.onWall)
            {
                // onto the wall-walk: the fight up there first (CityDefenceMode.WallFight)
                PutOnWall(orc, tower.wallCell);
                yield break;
            }
            orc.transform.position = drop;
            if (body != null) body.enabled = true;
            orc.Untargetable = false;
            orc.enabled = true;
            // inside the walls: on to the inner main gate (the user's sixth playtest)
            ToInnerGate(orc);
        }

        /// <summary>The tower model's «Bridge» (orc_siege_drafts.py: hinged at its foot on the top deck, folded up) swings
        /// down 90 degrees towards the wall, with a bounce at the end.</summary>
        private static IEnumerator LowerBridge(Transform tower, Vector3 toWall)
        {
            Transform bridge = null;
            foreach (var child in tower.GetComponentsInChildren<Transform>(true))
                if (child.name == "Bridge") { bridge = child; break; }
            if (bridge == null) yield break;
            var look = bridge.GetComponentInChildren<Renderer>();
            var up = look != null ? look.bounds.center - bridge.position : Vector3.up;
            up.y = Mathf.Max(up.y, 0.01f);
            var axis = Vector3.Cross(up.normalized, toWall.normalized);
            if (axis.sqrMagnitude < 0.0001f) yield break;
            axis.Normalize();
            var start = bridge.rotation;
            for (var t = 0f; t < BridgeDropSeconds; t += Time.deltaTime)
            {
                if (bridge == null) yield break;
                var k = t / BridgeDropSeconds;
                var angle = 90f * k * k + Mathf.Sin(Mathf.Clamp01((k - 0.8f) / 0.2f) * Mathf.PI) * -6f;
                bridge.rotation = Quaternion.AngleAxis(angle, axis) * start;
                yield return null;
            }
            if (bridge != null) bridge.rotation = Quaternion.AngleAxis(90f, axis) * start;
        }

        private void ForgetSiege()
        {
            foreach (var t in _towers) if (t.orc != null) Buildings.WallWalk.Undock(t.orc);
            _towers.Clear();
            ForgetWallFight();
            _toInner.Clear();
        }
    }
}
