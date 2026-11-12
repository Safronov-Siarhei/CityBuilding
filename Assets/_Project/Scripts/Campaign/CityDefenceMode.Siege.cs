using System.Collections.Generic;
using CityBuilder.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// The orcs' SIEGE MACHINES in mission 2 (the user's second playtest: «добавь оркам осадные башни и тараны
    /// после того как половина времени пройдёт»; models approved 2026-11-12). From the second half of a part
    /// (Late) a wave may bring, on one road in turn:
    ///   * a RAM -- a brute in the ram's body: slow, very tough, never turns on the fighters around it, and
    ///     strikes the gate its road ends at very hard;
    ///   * a SIEGE TOWER -- slow and tough, ignores the fighters too; once it is at the wall beside its gate it
    ///     stops, and its crew jump down INSIDE the walls and raid the town; the empty tower stands a while.
    /// Kill a machine on its road and nothing comes of it. Numbers: the m2_ram_* / m2_tower_* / m2_siege_* keys.
    /// </summary>
    public partial class CityDefenceMode
    {
        [SerializeField] private GameObject orcRamModel;     // Models/Orcs/OrcRam.fbx
        [SerializeField] private GameObject orcTowerModel;   // Models/Orcs/OrcSiegeTower.fbx

        private class Tower { public OrcUnit orc; public int lane; }
        private readonly List<Tower> _towers = new List<Tower>();
        private readonly List<(GameObject go, float until)> _docked = new List<(GameObject, float)>();

        /// <summary>Called by SendWave in the second half of a part: maybe a ram, maybe a tower.</summary>
        private void SendSiege(int level)
        {
            var b = B;
            if (b.ramEvery > 0 && Wave % b.ramEvery == 0) SpawnMachine(true, (Wave / b.ramEvery) % 3, level);
            if (b.towerEvery > 0 && Wave % b.towerEvery == 0) SpawnMachine(false, (Wave / b.towerEvery + 1) % 3, level);
        }

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
            orc.FollowLane(_lanes[lane]);
            orc.LaneOnly = true;
            Dress(orc, ram ? orcRamModel : orcTowerModel);
            _laneOrcs[lane].Add(orc);
            if (!ram) _towers.Add(new Tower { orc = orc, lane = lane });
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
            var b = B;
            for (var i = _towers.Count - 1; i >= 0; i--)
            {
                var t = _towers[i];
                if (t.orc == null || t.orc.CurrentHealth <= 0) { _towers.RemoveAt(i); continue; }
                var gate = OuterGates[t.lane] != null ? OuterGates[t.lane].transform.position : _lanes[t.lane].End;
                if (Flat(t.orc.transform.position - gate) > b.towerDockCells) continue;
                Dock(t, gate);
                _towers.RemoveAt(i);
            }
        }

        /// <summary>At the wall: the moving tower becomes a standing one, its crew land inside the walls.</summary>
        private void Dock(Tower t, Vector3 gate)
        {
            var at = t.orc.transform.position;
            var level = t.orc.Level;
            if (orcTowerModel != null)
            {
                var face = gate - at;
                face.y = 0f;
                var stand = Instantiate(orcTowerModel, at, face.sqrMagnitude > 0.01f ? Quaternion.LookRotation(face) : t.orc.transform.rotation);
                stand.name = "OrcSiegeTowerDocked";
                _docked.Add((stand, Time.time + B.towerStaySeconds));
            }
            _laneOrcs[t.lane].Remove(t.orc);
            Destroy(t.orc.gameObject);

            var raids = OrcRaidManager.Instance;
            if (raids == null) return;
            var inward = _square - gate;
            inward.y = 0f;
            inward = inward.sqrMagnitude > 0.01f ? inward.normalized : Vector3.forward;
            var drop = gate + inward * 2.5f + (at - gate) * 0.5f;
            if (NavMesh.SamplePosition(drop, out var hit, 3f, NavMesh.AllAreas)) drop = hit.position;
            for (var k = 0; k < B.towerCrew; k++)
            {
                var spot = drop + new Vector3(Random.Range(-0.8f, 0.8f), 0f, Random.Range(-0.8f, 0.8f));
                var orc = raids.SpawnOrcOfKind(spot, level, OrcKind.Raider);
                if (orc == null) continue;
                orc.Toughen(B.orcHealthMult, B.orcDamageMult);
                _laneOrcs[t.lane].Add(orc);
            }
        }

        private void ForgetSiege()
        {
            foreach (var d in _docked) if (d.go != null) Destroy(d.go);
            _docked.Clear();
            _towers.Clear();
        }
    }
}
