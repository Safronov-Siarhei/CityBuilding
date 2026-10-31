using System.Collections.Generic;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Maps;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Dungeon
{
    /// <summary>
    /// The upgrade crystals lying below (the user, 2026-10-31: they FLOAT in the air, so the player
    /// sees at once they are a reward; each one is guarded -- DungeonMonsters puts a band by every
    /// spot).
    /// - dungeon_crystals_per_room at every "reward" spot (the crystal rooms at the dead ends' far
    ///   ends), dungeon_crystals_lone at every "crystal" spot (a lone crystal in an empty dead end).
    /// - A hero within dungeon_crystal_pick_reach cells takes it: straight into the Laboratory's
    ///   wallet (Wallet.UpgradeCrystals, global), with a line in the feed.
    /// - A crystal taken never comes back: its id (dungeon, level, index) goes into DungeonRecord and
    ///   the town's save, like a slain boss.
    /// </summary>
    public class DungeonCrystals : MonoBehaviour
    {
        public class Crystal
        {
            public string Id;
            public Vector3 At;
            public Transform Shown;
        }

        public static DungeonCrystals Instance { get; private set; }

        /// <summary>The crystals still lying here.</summary>
        public IReadOnlyList<Crystal> Lying => _lying;

        /// <summary>Taken on this visit, by id.</summary>
        public IReadOnlyCollection<string> TakenNow => _takenNow;

        public int CrystalsTaken { get; private set; }

        private readonly List<Crystal> _lying = new List<Crystal>();
        private readonly HashSet<string> _takenNow = new HashSet<string>();
        private float _pickTimer;
        private const float PickCheckSeconds = 0.2f;
        private const float SpinDegreesPerSecond = 70f;
        private const float BobCells = 0.06f;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static string CrystalId(string dungeonId, int level, int index) => $"{dungeonId}-L{level}-c{index}";

        public void Begin(MeshMapDefinition map, string dungeonId, int level, ICollection<string> takenBefore)
        {
            var config = BalanceConfig.Instance;
            var index = 0;
            foreach (var spot in map.SpotsOf("reward")) Put(spot.position, config.DungeonCrystalsPerRoom, dungeonId, level, ref index, takenBefore);
            foreach (var spot in map.SpotsOf("crystal")) Put(spot.position, config.DungeonCrystalsLone, dungeonId, level, ref index, takenBefore);
        }

        private void Put(Vector2 spot, int count, string dungeonId, int level, ref int index, ICollection<string> takenBefore)
        {
            var ground = new Vector3(spot.x, 0f, spot.y);
            if (MeshMapApplier.Instance != null && MeshMapApplier.Instance.TryGetGroundHeight(ground, out var y)) ground.y = y;
            if (NavMesh.SamplePosition(ground, out var hit, 3f, NavMesh.AllAreas)) ground = hit.position;
            for (var k = 0; k < count; k++)
            {
                var id = CrystalId(dungeonId, level, index++);
                if (takenBefore != null && takenBefore.Contains(id)) continue;
                // two side by side in a room, a little apart
                var at = ground + (count > 1 ? new Vector3((k - (count - 1) * 0.5f) * 0.5f, 0f, 0f) : Vector3.zero);
                _lying.Add(new Crystal { Id = id, At = at, Shown = Show(at, k) });
            }
        }

        private Transform Show(Vector3 at, int k)
        {
            var model = OrcRaidManager.Instance != null ? OrcRaidManager.Instance.RewardCrystalModel : null;
            GameObject shown;
            if (model != null) shown = Instantiate(model);
            else
            {
                shown = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(shown.GetComponent<Collider>());
                shown.transform.localScale = new Vector3(0.2f, 0.4f, 0.2f);
            }
            shown.name = "RewardCrystal";
            shown.transform.SetParent(transform, false);
            shown.transform.SetPositionAndRotation(at + Vector3.up * BalanceConfig.Instance.DungeonCrystalFloat, Quaternion.Euler(0f, k * 40f, 0f));
            SceneryShadows.DisableCasting(shown);
            return shown.transform;
        }

        private void Update()
        {
            var t = Time.time;
            var lift = BalanceConfig.Instance.DungeonCrystalFloat;
            for (var i = 0; i < _lying.Count; i++)
            {
                var c = _lying[i];
                if (c.Shown == null) continue;
                c.Shown.Rotate(0f, SpinDegreesPerSecond * Time.deltaTime, 0f, Space.World);
                var p = c.At;
                p.y += lift + Mathf.Sin(t * 2f + i) * BobCells;
                c.Shown.position = p;
            }
            _pickTimer -= Time.deltaTime;
            if (_pickTimer > 0f) return;
            _pickTimer = PickCheckSeconds;
            PickUp();
        }

        /// <summary>Any hero within reach takes the crystals there. Public for the tests.</summary>
        public void PickUp()
        {
            var reach = BalanceConfig.Instance.DungeonCrystalPickReach;
            for (var i = _lying.Count - 1; i >= 0; i--)
            {
                var c = _lying[i];
                foreach (var soldier in SoldierUnit.All)
                {
                    if (soldier == null || soldier.CurrentHealth <= 0) continue;
                    var d = soldier.transform.position - c.At;
                    d.y = 0f;
                    if (d.magnitude > reach) continue;
                    Take(i);
                    break;
                }
            }
        }

        private void Take(int i)
        {
            var c = _lying[i];
            _lying.RemoveAt(i);
            if (c.Shown != null) Destroy(c.Shown.gameObject);
            _takenNow.Add(c.Id);
            DungeonRecord.CrystalsTaken.Add(c.Id);
            CrystalsTaken++;
            Wallet.UpgradeCrystals += 1;
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Format("#log_dungeon_crystal", 1), EventSeverity.Good,
                EventGlyph.Upgrade, null, c.At));
        }
    }
}
