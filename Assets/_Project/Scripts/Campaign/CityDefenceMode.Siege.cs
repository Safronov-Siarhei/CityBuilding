using System.Collections;
using System.Collections.Generic;
using CityBuilder.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// The orcs' SIEGE MACHINES in mission 2 (models approved 2026-11-12). Which wave brings them is the m2waves tab's
    /// `rams` / `towers` columns (the user's third playtest), one road each in turn:
    ///   * a RAM -- a brute in the ram's body: slow, very tough, never turns on the fighters around it, and
    ///     strikes the gate its road ends at very hard;
    ///   * a SIEGE TOWER (the user's third playtest: «она должна ехать к участку стены, вставать около него и на стену
    ///     будут забираться орки») -- slow and tough, ignores the fighters too; it rolls to a section of the wall
    ///     beside the gate (its own road: the lane up to the gate's front, then along the wall to its spot), stands
    ///     against it, and its crew climb it one by one: up the tower, over onto the wall-walk, and down into the
    ///     town. The empty tower stands a while.
    /// Kill a machine on its way and nothing comes of it. Numbers: the m2_ram_* / m2_tower_* / m2_siege_* keys.
    /// </summary>
    public partial class CityDefenceMode
    {
        [SerializeField] private GameObject orcRamModel;     // Models/Orcs/OrcRam.fbx
        [SerializeField] private GameObject orcTowerModel;   // Models/Orcs/OrcSiegeTower.fbx

        private class Tower { public OrcUnit orc; public int lane; public float wallX; }
        private readonly List<Tower> _towers = new List<Tower>();
        private readonly List<(GameObject go, float until)> _docked = new List<(GameObject, float)>();
        private int _towerSide;

        // the outer wall's north side, plan y; a tower stands this far out in front of it, the deck as high as the walk
        private const float NorthWallY = 34f;
        private const float TowerStandOff = 1.1f;
        private const float WalkHeight = 1.15f;

        private void SpawnMachine(bool ram, int lane, int level)
        {
            var raids = OrcRaidManager.Instance;
            if (raids == null || _lanes[lane] == null) return;
            var b = B;
            var orc = raids.SpawnOrcOfKind(_spawns[lane], level, OrcKind.Brute);
            if (orc == null) return;
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
                return;
            }
            // the tower's own road: the lane to the gate's front, then along the wall to a section either side of the gate
            var side = (_towerSide++ % 2 == 0) ? -1f : 1f;
            var wallX = 50.5f + side * Random.Range(b.towerWallFromCells, b.towerWallToCells);
            orc.FollowLane(TowerRoad(lane, wallX));
            _towers.Add(new Tower { orc = orc, lane = lane, wallX = wallX });
        }

        private OrcLane TowerRoad(int lane, float wallX)
        {
            var p = _plan.lanes[lane].points;
            var corners = new List<Vector3>();
            var front = NorthWallY - TowerStandOff - 1.3f;
            for (var k = 0; k + 1 < p.Length; k += 2)
            {
                if (p[k + 1] > front) break;   // up to the gate's front, not through it
                corners.Add(World(p[k], p[k + 1]));
            }
            corners.Add(World(Mathf.Lerp(50.5f, wallX, 0.5f), front));
            corners.Add(World(wallX, NorthWallY - TowerStandOff));
            return new OrcLane(corners.ToArray());
        }

        /// <summary>The brute's own figure hidden, the machine's model in its place (models are in cells = metres).</summary>
        private static void Dress(OrcUnit orc, GameObject model)
        {
            if (model == null) return;
            foreach (var r in orc.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            var go = Instantiate(model, orc.transform, false);
            go.name = model.name;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = model.transform.localScale / Mathf.Max(0.01f, orc.transform.lossyScale.x);
        }

        private void TickSiege(float dt)
        {
            for (var i = _docked.Count - 1; i >= 0; i--)
            {
                if (_docked[i].go != null && Time.time < _docked[i].until) continue;
                if (_docked[i].go != null) Destroy(_docked[i].go);
                _docked.RemoveAt(i);
            }
            for (var i = _towers.Count - 1; i >= 0; i--)
            {
                var t = _towers[i];
                if (t.orc == null || t.orc.CurrentHealth <= 0) { _towers.RemoveAt(i); continue; }
                var spot = World(t.wallX, NorthWallY - TowerStandOff);
                if (Flat(t.orc.transform.position - spot) > B.towerDockCells) continue;
                Dock(t, spot);
                _towers.RemoveAt(i);
            }
        }

        /// <summary>Against the wall: the moving tower becomes a standing one, and its crew climb it into the town.</summary>
        private void Dock(Tower t, Vector3 spot)
        {
            var level = t.orc.Level;
            var ground = t.orc.transform.position.y;
            spot.y = ground;
            if (orcTowerModel != null)
            {
                // facing the wall (south), the drawbridge side towards it
                var stand = Instantiate(orcTowerModel, spot, Quaternion.LookRotation(Vector3.back));
                stand.name = "OrcSiegeTowerDocked";
                _docked.Add((stand, Time.time + B.towerStaySeconds + B.towerCrew * B.towerClimbGapSeconds));
            }
            _laneOrcs[t.lane].Remove(t.orc);
            Destroy(t.orc.gameObject);
            var raids = OrcRaidManager.Instance;
            if (raids == null) return;
            var deck = World(t.wallX, NorthWallY - 0.35f);
            deck.y = ground + WalkHeight;
            var walk = World(t.wallX, NorthWallY + 0.5f);
            walk.y = ground + WalkHeight;
            var drop = OnMesh(World(t.wallX, NorthWallY + 2.2f));
            for (var k = 0; k < B.towerCrew; k++)
            {
                var orc = raids.SpawnOrcOfKind(spot, level, OrcKind.Raider);
                if (orc == null) continue;
                orc.Toughen(B.orcHealthMult, B.orcDamageMult);
                _laneOrcs[t.lane].Add(orc);
                StartCoroutine(Climb(orc, spot, deck, walk, drop, k * B.towerClimbGapSeconds));
            }
        }

        /// <summary>One of the crew: waits his turn inside the tower, climbs to the deck, crosses onto the wall-walk, jumps down inside.</summary>
        private IEnumerator Climb(OrcUnit orc, Vector3 foot, Vector3 deck, Vector3 walk, Vector3 drop, float wait)
        {
            var body = orc.GetComponent<CharacterController>();
            if (body != null) body.enabled = false;
            orc.enabled = false;
            var looks = orc.GetComponentsInChildren<Renderer>();
            foreach (var r in looks) r.enabled = false;
            orc.transform.position = foot;
            for (var t = 0f; t < wait && orc != null; t += Time.deltaTime) yield return null;
            if (orc == null) yield break;
            foreach (var r in looks) if (r != null) r.enabled = true;
            orc.transform.rotation = Quaternion.LookRotation(Vector3.back);
            var climb = Mathf.Max(0.2f, B.towerClimbSeconds);
            var legs = new[] { (foot, deck, climb, 0f), (deck, walk, climb * 0.5f, 0.15f), (walk, drop, climb * 0.5f, 0.35f) };
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
            orc.transform.position = drop;
            if (body != null) body.enabled = true;
            orc.enabled = true;
        }

        private void ForgetSiege()
        {
            foreach (var d in _docked) if (d.go != null) Destroy(d.go);
            _docked.Clear();
            _towers.Clear();
        }
    }
}
