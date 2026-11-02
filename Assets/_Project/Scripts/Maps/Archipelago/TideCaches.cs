using System.Collections.Generic;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Maps.Archipelago
{
    /// <summary>
    /// Guarded caches on the archipelago's islands (the user, 2026-11-02: «тайники под охраной»; the
    /// guards a faction of their own, Стражи Глубин, who ONLY guard -- no raids, no ships).
    /// - One cache on every ordinary island (not the start, the orcs', the bot's or an islet): a
    ///   stone ruin with a chest in it (Tools/blender/treasures.py), on grass near the island's middle,
    ///   clear of a dungeon's mouth. Laid with the map, from its seed, so cache k is always the same.
    /// - Its tier by how far the island is from the start: the nearest third 1, the middle 2, the
    ///   far 3, the gold island 4 (Archipelago.IslandRiches). The guards are the sheet's
    ///   tide_t{tier}_{kind} counts; the reward tide_reward_{wood,stone,gold,crystals}_{tier}.
    /// - The guards hold the chest (OrcUnit.SetGuardPost, tide_guard_radius) and sleep -- no thinking
    ///   at all -- until one of the player's units comes within tide_wake_radius; a shaman heals the
    ///   most hurt of its band every tide_shaman_heal_every_sec.
    /// - The last guard down, the chest swings open and pays out; the cache is LOOTED for good
    ///   (Looted, saved as tideCachesLooted) and stands open, empty and unguarded after a load.
    /// The guards are not saved (OrcUnit.IsGuard): the cache musters them again on load.
    /// </summary>
    public class TideCaches : MonoBehaviour
    {
        public static TideCaches Instance { get; private set; }

        /// <summary>Caches already looted, by index: they stay empty.</summary>
        public static readonly HashSet<int> Looted = new HashSet<int>();

        public static void LoadLooted(IEnumerable<int> looted)
        {
            Looted.Clear();
            if (looted != null) foreach (var k in looted) Looted.Add(k);
        }

        public class Cache
        {
            public int Index;
            public int Tier;
            public Vector3 Centre;
            public Transform Lid;
            public Quaternion LidRest;
            public float LidSign = 1f;
            public readonly List<OrcUnit> Guards = new List<OrcUnit>();
            public bool Mustered;
            public float HealClock;
            public float Opening = -1f;
        }

        private static readonly OrcKind[] GuardKinds = { OrcKind.TideCrab, OrcKind.TideShark, OrcKind.TidePuffer, OrcKind.TideShaman, OrcKind.TideSerpent };
        private const float LidOpenDegrees = 105f;
        private const float LidOpenSeconds = 0.8f;
        private const float SleepCheckSeconds = 0.4f;
        private const float StayAwakeAfterHurtSeconds = 6f;

        private readonly List<Cache> _caches = new List<Cache>();
        private readonly Dictionary<OrcUnit, Cache> _cacheOf = new Dictionary<OrcUnit, Cache>();
        private float _sleepClock;
        private float _musterAt;

        public IReadOnlyList<Cache> Caches => _caches;

        private void Awake() => Instance = this;
        private void OnEnable() => OrcUnit.Died += OnDied;
        private void OnDisable() => OrcUnit.Died -= OnDied;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Lays the caches on the map's islands; the guards come a moment later, once the ground can be walked.</summary>
        public static TideCaches Place(ArchipelagoMap world, MeshMapApplier applier, int[] islandOf, IslandRiches.Riches[] riches,
            GameObject ruin, GameObject chest, HashSet<Vector2Int> noBuild)
        {
            var grid = GridManager.Instance;
            if (world == null || grid == null || riches == null || Invasion.InvasionRules.Active) return null;
            var go = new GameObject("TideCaches");
            go.transform.SetParent(applier.transform, false);
            var caches = go.AddComponent<TideCaches>();

            // the islands that get one, nearest the start first (their order is their index)
            var start = new Vector2(world.StartCell.x, world.StartCell.y);
            var islands = new List<int>();
            for (var k = 0; k < world.Islands.Count; k++)
            {
                if (riches[k] == IslandRiches.Riches.None || riches[k] == IslandRiches.Riches.Start) continue;
                if (world.Islands[k].Kind == ArchipelagoGenerator.Kind.Islet) continue;
                islands.Add(k);
            }
            islands.Sort((a, b) => Vector2.Distance(world.Islands[a].Centre, start).CompareTo(Vector2.Distance(world.Islands[b].Centre, start)));
            var footprint = DungeonEntrances.Footprint;
            for (var n = 0; n < islands.Count; n++)
            {
                var k = islands[n];
                if (!DungeonEntrances.TryFindSpot(world, world.Islands[k], applier, out var origin, null, freeOnGrid: true)) continue;
                var tier = riches[k] == IslandRiches.Riches.Gold ? 4 : 1 + Mathf.Min(2, n * 3 / Mathf.Max(1, islands.Count));
                var cache = new Cache { Index = caches._caches.Count, Tier = tier, Centre = grid.GetFootprintCenterWorld(origin, footprint) };
                caches.Build(cache, ruin, chest);
                for (var z = 0; z < footprint.y; z++)
                for (var x = 0; x < footprint.x; x++) noBuild.Add(origin + new Vector2Int(x, z));
                grid.SetAreaOccupied(origin, footprint, true);
                caches._caches.Add(cache);
            }
            caches._musterAt = Time.time + 1.5f;
            return caches;
        }

        private void Build(Cache cache, GameObject ruin, GameObject chest)
        {
            var root = new GameObject($"TideCache{cache.Index}");
            root.transform.SetParent(transform, false);
            if (MeshMapApplier.Instance != null && MeshMapApplier.Instance.TryGetGroundHeight(cache.Centre, out var y)) cache.Centre.y = y;
            root.transform.position = cache.Centre;
            // the open side of the ruin and the chest's front to the camera, which never turns
            var cam = Camera.main != null ? Camera.main.transform.forward : Vector3.forward;
            cam.y = 0f;
            if (cam.sqrMagnitude > 0.01f) root.transform.rotation = Quaternion.LookRotation(cam.normalized);
            if (ruin != null) Model(ruin, root.transform, "Ruin");
            if (chest != null)
            {
                var c = Model(chest, root.transform, "Chest");
                foreach (var t in c.GetComponentsInChildren<Transform>(true))
                    if (t.name.EndsWith("Lid")) cache.Lid = t;
                if (cache.Lid != null)
                {
                    cache.LidRest = cache.Lid.localRotation;
                    // which way round the hinge lifts the lid's front: the import's axes decide, so try it
                    var lid = cache.Lid.GetComponent<Renderer>();
                    if (lid != null)
                    {
                        var rest = lid.bounds.center.y;
                        cache.Lid.localRotation = cache.LidRest * Quaternion.Euler(60f, 0f, 0f);
                        cache.LidSign = lid.bounds.center.y >= rest ? 1f : -1f;
                        cache.Lid.localRotation = cache.LidRest;
                    }
                }
            }
            if (Looted.Contains(cache.Index)) OpenLid(cache, 1f);
        }

        private static GameObject Model(GameObject source, Transform parent, string name)
        {
            var model = Instantiate(source, parent);
            model.name = name;
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = source.transform.localRotation;
            SceneryShadows.DisableCasting(model);
            SeasonStyle.NoSnow(model);
            return model;
        }

        private void Update()
        {
            if (_musterAt > 0f && Time.time >= _musterAt)
            {
                _musterAt = 0f;
                foreach (var cache in _caches) Muster(cache);
            }
            var dt = Time.deltaTime;
            foreach (var cache in _caches)
            {
                if (cache.Opening >= 0f && cache.Opening < 1f)
                {
                    cache.Opening = Mathf.Min(1f, cache.Opening + dt / LidOpenSeconds);
                    OpenLid(cache, cache.Opening);
                }
                if (cache.Guards.Count > 0) Heal(cache, dt);
            }
            _sleepClock -= Time.unscaledDeltaTime;
            if (_sleepClock > 0f) return;
            _sleepClock = SleepCheckSeconds;
            RefreshSleep();
        }

        // ------------------------------------------------------------------ the guards

        private void Muster(Cache cache)
        {
            if (cache.Mustered || Looted.Contains(cache.Index)) return;
            cache.Mustered = true;
            var raids = OrcRaidManager.Instance;
            var config = BalanceConfig.Instance;
            if (raids == null || config == null) return;
            var roster = new List<OrcKind>();
            foreach (var kind in GuardKinds)
                for (var i = config.TideGuards(cache.Tier, kind); i > 0; i--) roster.Add(kind);
            for (var i = 0; i < roster.Count; i++)
            {
                // the boss on the chest's far side, the rest in a ring round it
                var boss = OrcUnit.IsBossKind(roster[i]);
                var angle = i * Mathf.PI * 2f / roster.Count;
                var post = cache.Centre + (boss ? Vector3.forward * 2.2f : new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 2.4f);
                if (NavMesh.SamplePosition(post, out var hit, 3f, NavMesh.AllAreas)) post = hit.position;
                var guard = raids.SpawnOrcOfKind(post, 1, roster[i]);
                if (guard == null) continue;
                guard.transform.position = post;
                guard.SetGuardPost(null, post, config.TideGuardRadius);
                guard.Asleep = true;
                cache.Guards.Add(guard);
                _cacheOf[guard] = cache;
            }
        }

        /// <summary>A guard sleeps unless one of the player's units is near, it is fighting, or it was hurt a moment ago.</summary>
        private void RefreshSleep()
        {
            var config = BalanceConfig.Instance;
            var wake = config != null ? config.TideWakeRadius : 14f;
            foreach (var cache in _caches)
            {
                if (cache.Guards.Count == 0) continue;
                var near = PlayerNear(cache.Centre, wake);
                foreach (var guard in cache.Guards)
                {
                    if (guard == null) continue;
                    guard.Asleep = !(near || guard.Fighting != null || Time.time - guard.LastHurtAt < StayAwakeAfterHurtSeconds);
                }
            }
        }

        private static bool PlayerNear(Vector3 at, float radius)
        {
            var r2 = radius * radius;
            foreach (var unit in SoldierUnit.All)
            {
                if (unit == null) continue;
                var d = unit.transform.position - at;
                d.y = 0f;
                if (d.sqrMagnitude < r2) return true;
            }
            return false;
        }

        /// <summary>A shaman's touch: the most hurt guard of its band, within reach, healed every so often.</summary>
        private static void Heal(Cache cache, float dt)
        {
            var config = BalanceConfig.Instance;
            if (config == null) return;
            cache.HealClock -= dt;
            if (cache.HealClock > 0f) return;
            cache.HealClock = config.TideShamanHealEverySeconds;
            foreach (var shaman in cache.Guards)
            {
                if (shaman == null || shaman.Kind != OrcKind.TideShaman || shaman.Asleep || shaman.CurrentHealth <= 0) continue;
                OrcUnit worst = null;
                var worstShare = 1f;
                foreach (var guard in cache.Guards)
                {
                    if (guard == null || guard.CurrentHealth <= 0) continue;
                    if ((guard.transform.position - shaman.transform.position).sqrMagnitude > config.TideShamanHealRadius * config.TideShamanHealRadius) continue;
                    var share = guard.CurrentHealth / (float)Mathf.Max(1, guard.MaxHealth);
                    if (share < worstShare) { worstShare = share; worst = guard; }
                }
                if (worst != null) worst.Heal(config.TideShamanHeal);
            }
        }

        private void OnDied(OrcUnit unit)
        {
            if (unit == null || !_cacheOf.TryGetValue(unit, out var cache)) return;
            _cacheOf.Remove(unit);
            cache.Guards.Remove(unit);
            cache.Guards.RemoveAll(g => g == null);
            if (cache.Guards.Count == 0 && !Looted.Contains(cache.Index)) Loot(cache);
        }

        // ------------------------------------------------------------------ the chest

        private static void Loot(Cache cache)
        {
            Looted.Add(cache.Index);
            cache.Opening = 0f;
            var config = BalanceConfig.Instance;
            if (config == null) return;
            var at = cache.Centre + Vector3.up * 1.6f;
            var popups = UI.ResourceGainPopups.Instance;
            var wood = config.TideReward(cache.Tier, ResourceType.Wood);
            var stone = config.TideReward(cache.Tier, ResourceType.Stone);
            var gold = config.TideReward(cache.Tier, ResourceType.Gold);
            var lift = 0f;
            foreach (var (type, amount) in new[] { (ResourceType.Wood, wood), (ResourceType.Stone, stone), (ResourceType.Gold, gold) })
            {
                if (amount <= 0) continue;
                ResourceManager.Instance?.Add(type, amount);
                if (popups != null) popups.Show(at + Vector3.up * lift, amount, type);
                lift += 0.7f;
            }
            var crystals = config.TideRewardCrystals(cache.Tier);
            if (crystals > 0)
            {
                Wallet.UpgradeCrystals += crystals;
                if (popups != null) popups.ShowCrystals(at + Vector3.up * lift, crystals);
            }
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Format("#log_tide_cache_looted", wood, stone, gold, crystals),
                EventSeverity.Good, EventGlyph.Star, null, cache.Centre));
        }

        private static void OpenLid(Cache cache, float t)
        {
            if (cache.Lid == null) return;
            // swings back on its hinge, a little past upright; eased so it pops then settles
            var eased = 1f - (1f - t) * (1f - t);
            cache.Lid.localRotation = cache.LidRest * Quaternion.Euler(cache.LidSign * LidOpenDegrees * eased, 0f, 0f);
        }
    }
}
