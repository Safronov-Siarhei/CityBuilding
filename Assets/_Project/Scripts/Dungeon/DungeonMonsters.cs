using System.Collections.Generic;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Maps;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Dungeon
{
    /// <summary>
    /// The dungeon's monsters (the user, 2026-10-31), put down as the heroes arrive.
    /// - A lair at every "lair" spot of the map (Tools/blender/dungeon_lairs.py). The lairs nearest
    ///   each boss's hall take that boss's theme: spiders and spiderlings by the queen, stone golems
    ///   and skeletons by the ore golem. The rest take turns: rats; skeletons with kobolds; bats;
    ///   rats with bats. How many of each is the sheet's (dungeon_lair_*).
    /// - The two bosses in their halls, unless the town already slew them on this dungeon's level.
    ///   Bosses never come back.
    /// - Everything holds its lair: it fights a hero within dungeon_monster_guard_radius (a boss
    ///   within dungeon_boss_guard_radius) of its post and walks back, never chases further.
    /// - Levels 1/2/3: the same monsters at the sheet's dungeon_monster_level_N, which multiplies
    ///   their health and damage. A kill pays coins by its health (dungeon_coins_per_health); a boss
    ///   pays dungeon_boss_coins and dungeon_boss_crystals times the dungeon's level.
    /// - EVERY CRYSTAL IS GUARDED (the user, 2026-10-31): a band at each 2-crystal room ("reward"
    ///   spot, dungeon_guard_room_share of a lair of the place's theme) and at each lone crystal
    ///   ("crystal" spot, dungeon_guard_single_share). The theme is the nearer boss's within
    ///   dungeon_guard_boss_reach cells, else the lairs' cycle.
    /// - PATROLS: dungeon_patrol_size monsters walk a "patrol_<k>" route there and back, pausing
    ///   dungeon_patrol_pause_sec at each end; they fight whoever comes near the point they are at
    ///   and take up the route again after.
    /// - The monsters come back only with the next visit: each visit is a fresh scene.
    /// - A monster SLEEPS (OrcUnit.Asleep: no thinking at all) until a hero comes within
    ///   dungeon_monster_wake_margin cells (the user, 2026-11-01: «просыпаются при подходе героя
    ///   близко»), and a sleeper in sight shows «Zzz» rising from its head (Shaders/SleepZ). One in a
    ///   fight, lying in pieces or hurt in the last seconds stays awake; a patrol in sight walks its
    ///   route. With the heroes gone it dozes off again -- for the phone's CPU too: the labyrinth
    ///   holds ~140 monsters and only a lair or two is ever near a hero.
    /// What was earned below goes home with the heroes (DungeonTravel.Leave).
    /// LEVEL 2, «Затопленные гномьи чертоги» (a map with a "boss_king" spot; the user's roster,
    /// 2026-11-04): the bosses are the cursed dwarf king (by his throne) and the canal serpent; the
    /// lairs nearest the king hold stone guards and dwarf skeletons, those nearest the serpent eels
    /// and drowned dwarfs; the rest take turns: drowned rats; dwarf skeletons with forge spirits;
    /// an ambush of drowned dwarfs with drowned rats; forge spirits with dwarf skeletons. An eel
    /// lives only in the water (a lair's eel is put on the nearest wading spot, a drowned rat
    /// instead where there is none) and dungeon2_lone_eels more lie alone in the flooded passages.
    /// </summary>
    public class DungeonMonsters : MonoBehaviour
    {
        public enum Theme
        {
            Rats, Undead, Bats, Pack, Spiders, Golems, BossQueen, BossGolem,
            // level 2
            Drowned, DwarfUndead, Ambush, Forge, Guards, Deep, BossKing, BossSerpent, LoneEel,
        }

        /// <summary>One boss of a map: its spot, the theme of the lairs round it, its own theme, its id's tail.</summary>
        private struct BossSlot
        {
            public Vector3? At;
            public Theme Near, Own;
            public string Which;
        }

        public class Lair
        {
            public Vector3 Post;
            public Theme Theme;
            public string BossId;
            public bool Guard;
            public readonly List<OrcUnit> Monsters = new List<OrcUnit>();
            // a patrol's route (null for a lair), where along it the band is headed, and its pause
            public List<Vector3> Route;
            public int RouteAt;
            public int RouteStep = 1;
            public float Pause;
        }

        public static DungeonMonsters Instance { get; private set; }

        public IReadOnlyList<Lair> Lairs => _lairs;
        public int Level { get; private set; } = 1;
        public int CoinsEarned { get; private set; }
        public int CrystalsEarned { get; private set; }

        /// <summary>The resources the bosses slain on this visit paid, carried up to the town (DungeonTravel.Leave).</summary>
        public Dictionary<ResourceType, int> ResourcesEarned { get; } = new Dictionary<ResourceType, int>();

        /// <summary>Bosses slain on this visit, by id (dungeon, level, which).</summary>
        public IReadOnlyCollection<string> BossesSlainNow => _slainNow;

        private readonly List<Lair> _lairs = new List<Lair>();
        private readonly Dictionary<OrcUnit, Lair> _lairOf = new Dictionary<OrcUnit, Lair>();
        private readonly HashSet<string> _slainNow = new HashSet<string>();
        private int _monsterLevel = 1;
        // this map's two bosses and its lairs' cycle (level 1: queen and ore golem; level 2: king and serpent)
        private BossSlot _bossA, _bossB;
        private Theme[] _cycle = { Theme.Rats, Theme.Undead, Theme.Bats, Theme.Pack };

        /// <summary>The level-2 king's throne, where he sits at half health (null elsewhere).</summary>
        public Vector3? ThroneAt { get; private set; }
        private float _sleepTimer;
        private const float SleepCheckSeconds = 0.3f;
        private const float StayAwakeAfterHurtSeconds = 6f;
        // the «Zzz» over a sleeper's head: its size (world) and how far above the model's top
        private const float SleepMarkSize = 0.9f;
        private const float SleepMarkLift = 0.15f;
        private readonly Dictionary<OrcUnit, GameObject> _sleepMarks = new Dictionary<OrcUnit, GameObject>();
        private static Mesh _markQuad;

        /// <summary>Monsters thinking now, out of all the living ones.</summary>
        public int AwakeCount { get; private set; }

        private void Awake() => Instance = this;

        private void OnEnable() => OrcUnit.Died += OnDied;

        private void OnDisable() => OrcUnit.Died -= OnDied;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            foreach (var lair in _lairs)
                if (lair.Route != null) Patrol(lair);
            _sleepTimer -= Time.unscaledDeltaTime;
            if (_sleepTimer > 0f) return;
            _sleepTimer = SleepCheckSeconds;
            RefreshSleep();
        }

        /// <summary>Lays far monsters to sleep and wakes near ones. Public for the tests.</summary>
        public void RefreshSleep()
        {
            var margin = BalanceConfig.Instance.DungeonMonsterWakeMargin;
            var cam = Camera.main;
            var awake = 0;
            foreach (var lair in _lairs)
            {
                foreach (var monster in lair.Monsters)
                {
                    if (monster == null) continue;
                    // under the water (a drowned dwarf's ambush, the serpent's dive): its powers wake it
                    if (!monster.enabled)
                    {
                        ShowSleep(monster, false);
                        continue;
                    }
                    var seen = cam != null && InFrame(cam, monster.transform.position);
                    var on = cam == null || monster.Fighting != null || monster.BusyFor > 0f
                        || Time.time - monster.LastHurtAt < StayAwakeAfterHurtSeconds
                        || (lair.Route != null && seen) || HeroNear(monster.transform.position, margin);
                    monster.Asleep = !on;
                    if (on) awake++;
                    ShowSleep(monster, !on && seen && monster.CurrentHealth > 0);
                }
            }
            AwakeCount = awake;
        }

        /// <summary>Is the «Zzz» up over this monster? -- for the tests.</summary>
        public bool ShowsSleep(OrcUnit monster) => monster != null && _sleepMarks.TryGetValue(monster, out var mark) && mark != null && mark.activeSelf;

        /// <summary>Puts the «Zzz» up over a sleeper in sight, or takes it down; made once per monster, the first time it is wanted.</summary>
        private void ShowSleep(OrcUnit monster, bool show)
        {
            _sleepMarks.TryGetValue(monster, out var mark);
            if (!show)
            {
                if (mark != null && mark.activeSelf) mark.SetActive(false);
                return;
            }
            if (mark == null)
            {
                var raids = OrcRaidManager.Instance;
                var material = raids != null ? raids.SleepMaterial : null;
                if (material == null) return;
                var top = monster.transform.position.y;
                foreach (var r in monster.GetComponentsInChildren<Renderer>()) top = Mathf.Max(top, r.bounds.max.y);
                mark = new GameObject("Zzz", typeof(MeshFilter), typeof(MeshRenderer));
                mark.transform.SetParent(monster.transform, false);
                mark.transform.position = new Vector3(monster.transform.position.x, top + SleepMarkLift + SleepMarkSize * 0.35f, monster.transform.position.z);
                mark.GetComponent<MeshFilter>().sharedMesh = MarkQuad();
                var renderer = mark.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                var block = new MaterialPropertyBlock();
                block.SetFloat("_Size", SleepMarkSize);
                block.SetFloat("_Phase", _sleepMarks.Count * 0.77f);
                renderer.SetPropertyBlock(block);
                _sleepMarks[monster] = mark;
            }
            if (!mark.activeSelf) mark.SetActive(true);
        }

        /// <summary>The «Zzz» quad: the shader turns it to the camera, so its bounds hold it whichever way.</summary>
        private static Mesh MarkQuad()
        {
            if (_markQuad != null) return _markQuad;
            var h = 0.5f;
            _markQuad = new Mesh
            {
                name = "SleepMarkQuad",
                vertices = new[] { new Vector3(-h, -h, 0f), new Vector3(h, -h, 0f), new Vector3(h, h, 0f), new Vector3(-h, h, 0f) },
                uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) },
                triangles = new[] { 0, 1, 2, 0, 2, 3 },
            };
            _markQuad.bounds = new Bounds(Vector3.zero, Vector3.one * SleepMarkSize * 2f);
            return _markQuad;
        }

        private const float FrameBorder = 0.25f;

        private static bool InFrame(Camera cam, Vector3 at)
        {
            var v = cam.WorldToViewportPoint(at);
            return v.z > 0f && v.x > -FrameBorder && v.x < 1f + FrameBorder && v.y > -FrameBorder && v.y < 1f + FrameBorder;
        }

        private static bool Near(Vector3 a, Vector3 b, float reach) => Mathf.Abs(a.x - b.x) <= reach && Mathf.Abs(a.z - b.z) <= reach;

        private static bool HeroNear(Vector3 at, float margin)
        {
            foreach (var soldier in SoldierUnit.All)
            {
                if (soldier == null || soldier.CurrentHealth <= 0) continue;
                if (Near(soldier.transform.position, at, margin)) return true;
            }
            return false;
        }

        /// <summary>The boss's id in the town's record: one per dungeon, level and hall.</summary>
        public static string BossId(string dungeonId, int level, string which) => $"{dungeonId}-L{level}-{which}";

        public void Begin(MeshMapDefinition map, string dungeonId, int level, ICollection<string> slainBefore)
        {
            var config = BalanceConfig.Instance;
            Level = Mathf.Clamp(level, 1, 3);
            _monsterLevel = Mathf.Max(1, Level == 1 ? config.DungeonMonsterLevel1 : Level == 2 ? config.DungeonMonsterLevel2 : config.DungeonMonsterLevel3);

            var lairs = new List<Vector3>();
            foreach (var spot in map.SpotsOf("lair")) lairs.Add(Ground(spot.position));
            var levelTwo = map.SpotsOf("boss_king").Count > 0 || map.SpotsOf("boss_serpent").Count > 0;
            if (levelTwo)
            {
                // the king stands by his throne; his hall's spot is where he holds
                var throne = map.SpotsOf("throne");
                ThroneAt = throne.Count > 0 ? Ground(throne[0].position) : (Vector3?)null;
                _bossA = new BossSlot { At = ThroneAt ?? SpotAt(map, "boss_king"), Near = Theme.Guards, Own = Theme.BossKing, Which = "king" };
                _bossB = new BossSlot { At = SpotAt(map, "boss_serpent"), Near = Theme.Deep, Own = Theme.BossSerpent, Which = "serpent" };
                _cycle = new[] { Theme.Drowned, Theme.DwarfUndead, Theme.Ambush, Theme.Forge };
            }
            else
            {
                _bossA = new BossSlot { At = SpotAt(map, "boss_spiders"), Near = Theme.Spiders, Own = Theme.BossQueen, Which = "spiders" };
                _bossB = new BossSlot { At = SpotAt(map, "boss_golem"), Near = Theme.Golems, Own = Theme.BossGolem, Which = "golem" };
            }

            // the lairs nearest each boss's hall take its theme
            var themes = new Theme?[lairs.Count];
            ThemeNearest(lairs, themes, _bossA.At, _bossA.Near, config.DungeonBossThemeLairs);
            ThemeNearest(lairs, themes, _bossB.At, _bossB.Near, config.DungeonBossThemeLairs);
            var next = 0;
            for (var i = 0; i < lairs.Count; i++)
            {
                var lair = new Lair { Post = lairs[i], Theme = themes[i] ?? _cycle[next++ % _cycle.Length] };
                _lairs.Add(lair);
                Fill(lair);
            }

            foreach (var boss in new[] { _bossA, _bossB })
                if (boss.At.HasValue) AddBoss(boss.At.Value, boss.Own, BossId(dungeonId, Level, boss.Which), slainBefore);

            // a band at every crystal: the rooms' and the lone ones'
            var k = 0;
            foreach (var spot in map.SpotsOf("reward")) AddGuard(Ground(spot.position), config.DungeonGuardRoomShare, k++);
            foreach (var spot in map.SpotsOf("crystal")) AddGuard(Ground(spot.position), config.DungeonGuardSingleShare, k++);

            // the patrols, route by route
            for (var r = 0; ; r++)
            {
                var spots = map.SpotsOf($"patrol_{r}");
                if (spots.Count < 2) break;
                var route = new List<Vector3>(spots.Count);
                foreach (var spot in spots) route.Add(Ground(spot.position));
                AddPatrol(route, ThemeAt(route[route.Count / 2], k++));
            }

            // level 2: eels alone in the flooded passages, far from the way in and from each other
            if (levelTwo) ScatterEels(config.Dungeon2LoneEels, config.Dungeon2EelSpacing);
        }

        private static Vector3? SpotAt(MeshMapDefinition map, string kind)
        {
            var spots = map.SpotsOf(kind);
            return spots.Count > 0 ? Ground(spots[0].position) : (Vector3?)null;
        }

        /// <summary>Who guards a place: the nearer boss's own kind when one is near, else the lairs' cycle.</summary>
        private Theme ThemeAt(Vector3 at, int k)
        {
            var reach = BalanceConfig.Instance.DungeonGuardBossReach;
            var da = _bossA.At.HasValue ? Flat(at - _bossA.At.Value) : float.MaxValue;
            var db = _bossB.At.HasValue ? Flat(at - _bossB.At.Value) : float.MaxValue;
            if (da < reach && da <= db) return _bossA.Near;
            if (db < reach) return _bossB.Near;
            return _cycle[k % _cycle.Length];
        }

        /// <summary>Every walkable spot under the dungeon's water within `reach` of `at`, a cell apart (nearest first).</summary>
        public static List<Vector3> WadingSpots(Vector3 at, float reach, float step = 1f)
        {
            var found = new List<Vector3>();
            if (!DungeonFloor.Active) return found;
            var n = Mathf.CeilToInt(reach / step);
            for (var x = -n; x <= n; x++)
            for (var z = -n; z <= n; z++)
            {
                var p = at + new Vector3(x * step, 0f, z * step);
                if (Flat(p - at) > reach) continue;
                if (!NavMesh.SamplePosition(p, out var hit, step * 0.6f, NavMesh.AllAreas)) continue;
                if (!DungeonFloor.Wading(hit.position)) continue;
                found.Add(hit.position);
            }
            found.Sort((a, b) => Flat(a - at).CompareTo(Flat(b - at)));
            return found;
        }

        /// <summary>Lone eels: wading spots on a coarse grid over the map, taken in a fixed scattered order, each `spacing` from the others and from the heroes' way in.</summary>
        private void ScatterEels(int count, float spacing)
        {
            if (count <= 0 || !DungeonFloor.Active) return;
            var mode = DungeonMode.Instance;
            var half = mode != null ? mode.MapHalf : 70f;
            var start = mode != null ? mode.StartSpot : Vector3.zero;
            var taken = new List<Vector3> { start };
            var candidates = new List<(int key, Vector3 at)>();
            const float step = 3f;
            for (var x = -half; x <= half; x += step)
            for (var z = -half; z <= half; z += step)
            {
                var p = new Vector3(x, 0f, z);
                if (MeshMapApplier.Instance != null && MeshMapApplier.Instance.TryGetGroundHeight(p, out var y)) p.y = y;
                if (!NavMesh.SamplePosition(p, out var hit, 1.2f, NavMesh.AllAreas) || !DungeonFloor.Wading(hit.position)) continue;
                candidates.Add((((int)(x * 73.1f) * 31 + (int)(z * 41.7f) * 17) & 0x7fffffff, hit.position));
            }
            candidates.Sort((a, b) => a.key.CompareTo(b.key));
            var placed = 0;
            foreach (var (_, at) in candidates)
            {
                if (placed >= count) break;
                var clear = true;
                foreach (var t in taken)
                    if (Flat(at - t) < spacing) { clear = false; break; }
                if (!clear) continue;
                foreach (var lair in _lairs)
                    if (Flat(at - lair.Post) < spacing * 0.5f) { clear = false; break; }
                if (!clear) continue;
                taken.Add(at);
                var eel = new Lair { Post = at, Theme = Theme.LoneEel, Guard = true };
                _lairs.Add(eel);
                SpawnAt(eel, OrcKind.Eel, at, BalanceConfig.Instance.DungeonGuardRadius);
                placed++;
            }
        }

        private static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;

        private void AddGuard(Vector3 at, float share, int k)
        {
            var lair = new Lair { Post = at, Theme = ThemeAt(at, k), Guard = true };
            _lairs.Add(lair);
            Fill(lair, share);
        }

        private void AddPatrol(List<Vector3> route, Theme theme)
        {
            var lair = new Lair { Post = route[0], Theme = theme, Route = route };
            _lairs.Add(lair);
            // the band: as many as the sheet says, the theme's kinds in turn
            var roster = Roster(theme);
            var size = Mathf.Max(1, BalanceConfig.Instance.DungeonPatrolSize);
            for (var i = 0; i < size; i++)
                SpawnAt(lair, roster[i % roster.Count].kind, route[0] + PatrolOffset(i), BalanceConfig.Instance.DungeonGuardRadius);
        }

        /// <summary>Where member `i` keeps round the band's point: a little cluster, not one spot.</summary>
        private static Vector3 PatrolOffset(int i) => i == 0 ? Vector3.zero : new Vector3(Mathf.Cos(i * 2.4f), 0f, Mathf.Sin(i * 2.4f)) * 0.7f;

        /// <summary>The band walks on when its first living member has reached the point and waited; never mid-fight.</summary>
        private void Patrol(Lair lair)
        {
            OrcUnit lead = null;
            foreach (var m in lair.Monsters)
            {
                if (m == null) continue;
                if (m.Fighting != null || m.BusyFor > 0f) return;
                if (lead == null) lead = m;
            }
            if (lead == null || lead.Asleep) return;
            var to = lead.GuardPost - lead.transform.position;
            to.y = 0f;
            if (to.magnitude > 0.8f) return;
            lair.Pause -= Time.deltaTime;
            if (lair.Pause > 0f) return;
            var last = lair.Route.Count - 1;
            if (lair.RouteAt + lair.RouteStep > last || lair.RouteAt + lair.RouteStep < 0) lair.RouteStep = -lair.RouteStep;
            lair.RouteAt += lair.RouteStep;
            if (lair.RouteAt == 0 || lair.RouteAt == last) lair.Pause = BalanceConfig.Instance.DungeonPatrolPauseSeconds;
            var point = lair.Route[lair.RouteAt];
            lair.Post = point;
            var i = 0;
            foreach (var m in lair.Monsters)
                if (m != null) m.MovePost(point + PatrolOffset(i++));
        }

        private static void ThemeNearest(List<Vector3> lairs, Theme?[] themes, Vector3? boss, Theme theme, int count)
        {
            if (!boss.HasValue) return;
            for (var n = 0; n < count; n++)
            {
                var best = -1;
                var bestD = float.MaxValue;
                for (var i = 0; i < lairs.Count; i++)
                {
                    if (themes[i].HasValue) continue;
                    var d = (lairs[i] - boss.Value).sqrMagnitude;
                    if (d >= bestD) continue;
                    bestD = d;
                    best = i;
                }
                if (best < 0) return;
                themes[best] = theme;
            }
        }

        private void AddBoss(Vector3 at, Theme theme, string id, ICollection<string> slainBefore)
        {
            if (slainBefore != null && slainBefore.Contains(id)) return;
            var lair = new Lair { Post = at, Theme = theme, BossId = id };
            _lairs.Add(lair);
            var kind = theme == Theme.BossQueen ? OrcKind.SpiderQueen : theme == Theme.BossGolem ? OrcKind.OreGolem
                : theme == Theme.BossKing ? OrcKind.DwarfKing : OrcKind.CanalSerpent;
            Spawn(lair, kind, 1, BalanceConfig.Instance.DungeonBossGuardRadius);
        }

        /// <summary>Who lives in a lair of this theme, from the sheet's counts.</summary>
        public static List<(OrcKind kind, int count)> Roster(Theme theme)
        {
            var c = BalanceConfig.Instance;
            switch (theme)
            {
                case Theme.Rats: return new List<(OrcKind, int)> { (OrcKind.Rat, c.LairRats) };
                case Theme.Undead: return new List<(OrcKind, int)> { (OrcKind.Skeleton, c.LairSkeletons), (OrcKind.Kobold, c.LairKobolds) };
                case Theme.Bats: return new List<(OrcKind, int)> { (OrcKind.Bat, c.LairBats) };
                case Theme.Pack:
                    return new List<(OrcKind, int)>
                    {
                        (OrcKind.Rat, Mathf.CeilToInt(c.LairRats * c.DungeonLairPackShare - 0.001f)),
                        (OrcKind.Bat, Mathf.CeilToInt(c.LairBats * c.DungeonLairPackShare - 0.001f)),
                    };
                case Theme.Spiders: return new List<(OrcKind, int)> { (OrcKind.Spider, c.LairSpiders), (OrcKind.Spiderling, c.LairSpiderlings) };
                case Theme.Golems: return new List<(OrcKind, int)> { (OrcKind.StoneGolem, c.LairStoneGolems), (OrcKind.Skeleton, c.LairSkeletons) };
                // level 2
                case Theme.Drowned: return new List<(OrcKind, int)> { (OrcKind.DrownedRat, c.Lair2DrownedRats) };
                case Theme.DwarfUndead:
                    return new List<(OrcKind, int)> { (OrcKind.DwarfSkeleton, c.Lair2DwarfSkeletons), (OrcKind.ForgeSpirit, Mathf.Max(1, c.Lair2ForgeSpirits - 1)) };
                case Theme.Ambush:
                    return new List<(OrcKind, int)>
                    {
                        (OrcKind.DrownedDwarf, c.Lair2DrownedDwarfs),
                        (OrcKind.DrownedRat, Mathf.CeilToInt(c.Lair2DrownedRats * c.DungeonLairPackShare * 0.5f - 0.001f)),
                    };
                case Theme.Forge:
                    return new List<(OrcKind, int)> { (OrcKind.ForgeSpirit, c.Lair2ForgeSpirits), (OrcKind.DwarfSkeleton, Mathf.Max(1, c.Lair2DwarfSkeletons - 1)) };
                case Theme.Guards: return new List<(OrcKind, int)> { (OrcKind.StoneGuard, c.Lair2StoneGuards), (OrcKind.DwarfSkeleton, c.Lair2DwarfSkeletons) };
                case Theme.Deep: return new List<(OrcKind, int)> { (OrcKind.Eel, c.Lair2Eels), (OrcKind.DrownedDwarf, Mathf.Max(1, c.Lair2DrownedDwarfs - 1)) };
                default: return new List<(OrcKind, int)>();
            }
        }

        /// <summary>Puts the theme's roster down round the lair; `share` of each kind (rounded up) for a crystal's guard.</summary>
        private void Fill(Lair lair, float share = 1f)
        {
            var roster = Roster(lair.Theme);
            var total = 0;
            for (var i = 0; i < roster.Count; i++)
            {
                roster[i] = (roster[i].kind, Mathf.CeilToInt(roster[i].count * share - 0.001f));
                total += roster[i].count;
            }
            var index = 0;
            foreach (var (kind, count) in roster)
            for (var k = 0; k < count; k++) Spawn(lair, kind, Mathf.Max(1, total), BalanceConfig.Instance.DungeonGuardRadius, index++);
        }

        private OrcUnit Spawn(Lair lair, OrcKind kind, int ofCount, float radius, int index = 0)
        {
            // each its own post round the lair's middle, a cell or so apart
            var angle = index * Mathf.PI * 2f / Mathf.Max(1, ofCount);
            var post = ofCount > 1 ? lair.Post + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (0.6f + 0.15f * ofCount) : lair.Post;
            return SpawnAt(lair, kind, post, radius);
        }

        private OrcUnit SpawnAt(Lair lair, OrcKind kind, Vector3 post, float radius)
        {
            var raids = OrcRaidManager.Instance;
            if (raids == null) return null;
            if (NavMesh.SamplePosition(post, out var hit, 2f, NavMesh.AllAreas)) post = hit.position;
            // an eel lives in the water: the nearest wading spot, else a drowned rat in its place
            if (kind == OrcKind.Eel && !DungeonFloor.Wading(post))
            {
                var wet = WadingSpots(post, 5f);
                if (wet.Count > 0) post = wet[0];
                else kind = OrcKind.DrownedRat;
            }
            var monster = raids.SpawnOrcOfKind(post, _monsterLevel, kind);
            if (monster == null) return null;
            monster.transform.position = post;
            monster.SetGuardPost(null, post, radius);
            lair.Monsters.Add(monster);
            _lairOf[monster] = lair;
            var config = BalanceConfig.Instance;
            if (kind == OrcKind.Skeleton)
            {
                monster.GetsUpOnce = true;
                monster.GetUpSeconds = config.SkeletonGetUpSeconds;
                monster.GetUpHealthShare = config.SkeletonGetUpHealth;
            }
            if (kind == OrcKind.DwarfSkeleton)
            {
                // the rusted armour holds to half health (DungeonMonsterPowers drops it), then it falls once
                monster.GetsUpOnce = true;
                monster.GetUpSeconds = config.SkeletonGetUpSeconds;
                monster.GetUpHealthShare = config.SkeletonGetUpHealth;
                monster.Armour = config.DwarfSkeletonArmour;
            }
            if (kind == OrcKind.Spider || kind == OrcKind.SpiderQueen || kind == OrcKind.OreGolem || kind == OrcKind.DwarfSkeleton
                || kind == OrcKind.DrownedDwarf || kind == OrcKind.DwarfKing || kind == OrcKind.CanalSerpent)
                monster.gameObject.AddComponent<DungeonMonsterPowers>().Bind(monster, this);
            return monster;
        }

        /// <summary>A monster called up by another (the queen's brood), holding `post` with the lair of `parent`.</summary>
        public OrcUnit Summon(OrcUnit parent, OrcKind kind, Vector3 post)
        {
            if (parent == null || !_lairOf.TryGetValue(parent, out var lair)) return null;
            return SpawnAt(lair, kind, post, BalanceConfig.Instance.DungeonBossGuardRadius);
        }

        private void OnDied(OrcUnit monster)
        {
            if (monster == null || !_lairOf.TryGetValue(monster, out var lair)) return;
            _lairOf.Remove(monster);
            lair.Monsters.Remove(monster);
            var config = BalanceConfig.Instance;
            var coins = Mathf.Max(1, Mathf.CeilToInt(monster.MaxHealth * config.DungeonCoinsPerHealth));
            if (OrcUnit.IsBossKind(monster.Kind) && lair.BossId != null)
            {
                coins += config.DungeonBossCoins * Level;
                var crystals = config.DungeonBossCrystals * Level;
                CrystalsEarned += crystals;
                Wallet.UpgradeCrystals += crystals;
                _slainNow.Add(lair.BossId);
                DungeonRecord.BossesSlain.Add(lair.BossId);
                // resources too (the user, 2026-11-02: «после убийства босса я ничего не получил»),
                // each rising over the boss as it falls
                var popups = UI.ResourceGainPopups.Instance;
                var at = monster.transform.position + Vector3.up * 2.5f;
                foreach (var (type, amount) in new[] { (ResourceType.Wood, config.DungeonBossWood), (ResourceType.Stone, config.DungeonBossStone), (ResourceType.Gold, config.DungeonBossGold) })
                {
                    var paid = amount * Level;
                    if (paid <= 0) continue;
                    ResourcesEarned[type] = (ResourcesEarned.TryGetValue(type, out var had) ? had : 0) + paid;
                    if (popups != null) popups.Show(at + Vector3.up * (0.8f * (int)type % 3), paid, type);
                }
                // and the crystals, the highest of them (the user, 2026-11-02: «добавь и кристаллы»)
                if (popups != null) popups.ShowCrystals(at + Vector3.up * 3.2f, crystals);
                var name = Localization.Get("#monster_" + OrcUnit.SheetIdOf(monster.Kind));
                EventLogManager.Instance?.Post(GameEvent.At(Localization.Format("#log_dungeon_boss_slain", name, coins, crystals),
                    EventSeverity.Good, EventGlyph.Star, null, monster.transform.position));
            }
            CoinsEarned += coins;
            ResourceManager.Instance?.Add(HeroRoster.Money, coins);
            // «+N» over every one that falls, as in a run: what it paid is seen
            UI.ResourceGainPopups.Instance?.ShowCoins(monster.transform.position, coins);
        }

        private static Vector3 Ground(Vector2 spot)
        {
            var at = new Vector3(spot.x, 0f, spot.y);
            if (MeshMapApplier.Instance != null && MeshMapApplier.Instance.TryGetGroundHeight(at, out var y)) at.y = y;
            if (NavMesh.SamplePosition(at, out var hit, 6f, NavMesh.AllAreas)) return hit.position;
            Debug.LogWarning($"DungeonMonsters: no walkable ground near the spot at {spot}.");
            return at;
        }
    }

    /// <summary>
    /// What never comes back below, by id -- the bosses the town has slain and the crystals it has
    /// taken -- and the gnomes hired there, waiting for the heroes' next visit; kept in the town's
    /// save (dungeonBossesSlain, dungeonCrystalsTaken, dungeonHirelings).
    /// </summary>
    public static class DungeonRecord
    {
        public static readonly HashSet<string> BossesSlain = new HashSet<string>();
        public static readonly HashSet<string> CrystalsTaken = new HashSet<string>();
        public static readonly List<Saving.HirelingEntry> Hirelings = new List<Saving.HirelingEntry>();
        public static readonly List<int> Potions = new List<int>();

        public static void Load(IEnumerable<string> bosses, IEnumerable<string> crystals = null, IEnumerable<Saving.HirelingEntry> hirelings = null, IEnumerable<int> potions = null)
        {
            Potions.Clear();
            if (potions != null) Potions.AddRange(potions);
            BossesSlain.Clear();
            CrystalsTaken.Clear();
            Hirelings.Clear();
            if (bosses != null) foreach (var id in bosses) BossesSlain.Add(id);
            if (crystals != null) foreach (var id in crystals) CrystalsTaken.Add(id);
            if (hirelings != null) Hirelings.AddRange(hirelings);
        }
    }
}
