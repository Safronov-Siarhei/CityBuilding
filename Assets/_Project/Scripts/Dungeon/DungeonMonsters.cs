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
    /// - The monsters come back only with the next visit: each visit is a fresh scene.
    /// What was earned below goes home with the heroes (DungeonTravel.Leave).
    /// </summary>
    public class DungeonMonsters : MonoBehaviour
    {
        public enum Theme { Rats, Undead, Bats, Pack, Spiders, Golems, BossQueen, BossGolem }

        public class Lair
        {
            public Vector3 Post;
            public Theme Theme;
            public string BossId;
            public readonly List<OrcUnit> Monsters = new List<OrcUnit>();
        }

        public static DungeonMonsters Instance { get; private set; }

        public IReadOnlyList<Lair> Lairs => _lairs;
        public int Level { get; private set; } = 1;
        public int CoinsEarned { get; private set; }
        public int CrystalsEarned { get; private set; }

        /// <summary>Bosses slain on this visit, by id (dungeon, level, which).</summary>
        public IReadOnlyCollection<string> BossesSlainNow => _slainNow;

        private readonly List<Lair> _lairs = new List<Lair>();
        private readonly Dictionary<OrcUnit, Lair> _lairOf = new Dictionary<OrcUnit, Lair>();
        private readonly HashSet<string> _slainNow = new HashSet<string>();
        private int _monsterLevel = 1;

        private void Awake() => Instance = this;

        private void OnEnable() => OrcUnit.Died += OnDied;

        private void OnDisable() => OrcUnit.Died -= OnDied;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
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
            var queen = map.SpotsOf("boss_spiders");
            var golem = map.SpotsOf("boss_golem");
            Vector3? queenAt = queen.Count > 0 ? Ground(queen[0].position) : (Vector3?)null;
            Vector3? golemAt = golem.Count > 0 ? Ground(golem[0].position) : (Vector3?)null;

            // the lairs nearest each boss's hall take its theme
            var themes = new Theme?[lairs.Count];
            ThemeNearest(lairs, themes, queenAt, Theme.Spiders, config.DungeonBossThemeLairs);
            ThemeNearest(lairs, themes, golemAt, Theme.Golems, config.DungeonBossThemeLairs);
            var cycle = new[] { Theme.Rats, Theme.Undead, Theme.Bats, Theme.Pack };
            var next = 0;
            for (var i = 0; i < lairs.Count; i++)
            {
                var lair = new Lair { Post = lairs[i], Theme = themes[i] ?? cycle[next++ % cycle.Length] };
                _lairs.Add(lair);
                Fill(lair);
            }

            if (queenAt.HasValue) AddBoss(queenAt.Value, Theme.BossQueen, BossId(dungeonId, Level, "spiders"), slainBefore);
            if (golemAt.HasValue) AddBoss(golemAt.Value, Theme.BossGolem, BossId(dungeonId, Level, "golem"), slainBefore);
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
            Spawn(lair, theme == Theme.BossQueen ? OrcKind.SpiderQueen : OrcKind.OreGolem, 1, BalanceConfig.Instance.DungeonBossGuardRadius);
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
                case Theme.Pack: return new List<(OrcKind, int)> { (OrcKind.Rat, c.LairRats), (OrcKind.Bat, c.LairBats) };
                case Theme.Spiders: return new List<(OrcKind, int)> { (OrcKind.Spider, c.LairSpiders), (OrcKind.Spiderling, c.LairSpiderlings) };
                case Theme.Golems: return new List<(OrcKind, int)> { (OrcKind.StoneGolem, c.LairStoneGolems), (OrcKind.Skeleton, c.LairSkeletons) };
                default: return new List<(OrcKind, int)>();
            }
        }

        private void Fill(Lair lair)
        {
            var roster = Roster(lair.Theme);
            var total = 0;
            foreach (var (_, count) in roster) total += count;
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
            if (kind == OrcKind.Spider || kind == OrcKind.SpiderQueen || kind == OrcKind.OreGolem)
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
                var name = Localization.Get(monster.Kind == OrcKind.SpiderQueen ? "#monster_spider_queen" : "#monster_ore_golem");
                EventLogManager.Instance?.Post(GameEvent.At(Localization.Format("#log_dungeon_boss_slain", name, coins, crystals),
                    EventSeverity.Good, EventGlyph.Star, null, monster.transform.position));
            }
            CoinsEarned += coins;
            ResourceManager.Instance?.Add(HeroRoster.Money, coins);
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

    /// <summary>The bosses the town has slain below, by id -- kept in the town's save (GameSaveData.dungeonBossesSlain).</summary>
    public static class DungeonRecord
    {
        public static readonly HashSet<string> BossesSlain = new HashSet<string>();

        public static void Load(IEnumerable<string> ids)
        {
            BossesSlain.Clear();
            if (ids == null) return;
            foreach (var id in ids) BossesSlain.Add(id);
        }
    }
}
