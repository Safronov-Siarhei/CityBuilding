using System.Collections.Generic;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Invasion
{
    /// <summary>
    /// The Invasion run's jungle between the lanes (the user's rules, 2026-10-21): the forest's own
    /// monsters -- not orcs -- in camps at the map's "monsters" spots, a boss in the southern lair,
    /// healing shrines at the "shrine" spots.
    ///
    /// - A camp's spot level says who lives there: wolves (1), bears (2), rock trolls (3); the lair
    ///   holds the hill giant. They hold their ground like an orc camp's guards: fight whoever
    ///   comes within inv_monster_guard_radius of their post, never leave it to chase, walk back.
    /// - A camp wiped out comes back inv_camp_respawn_sec later, the boss inv_boss_respawn_sec later,
    ///   and they come back STRONGER with the run (the user's choice): a level higher for every
    ///   inv_monster_level_every_waves waves the base has survived. The level multiplies health and
    ///   damage, so the King's experience (an enemy's health) and the gold (InvasionWaves.GoldForKill)
    ///   grow with it.
    /// - The boss pays gold, wood and stone (× his level) and blesses the King for a while: more
    ///   damage and health (KingHero.Empower).
    /// - A shrine heals the King and soldiers standing within inv_shrine_radius, a share of their
    ///   health a second, for as long as they stay.
    /// </summary>
    public class InvasionJungle : MonoBehaviour
    {
        private const float ShrineTickSeconds = 0.25f;
        /// <summary>How far round its camp's spot each monster of the camp gets its own post.</summary>
        private const float CampSpreadMeters = 1.6f;

        /// <summary>One camp of monsters, or the boss's lair.</summary>
        public class Camp
        {
            public Vector3 Post;
            public int Tier;
            public bool IsBoss;
            /// <summary>The level its monsters came back at last.</summary>
            public int Level;
            /// <summary>Seconds until it comes back; 0 while its monsters stand.</summary>
            public float RespawnIn;
            /// <summary>How many times it has been wiped out.</summary>
            public int TimesSlain;
            public readonly List<OrcUnit> Monsters = new List<OrcUnit>();
        }

        public IReadOnlyList<Camp> Camps => _camps;
        public Camp BossLair { get; private set; }
        public IReadOnlyList<Vector3> Shrines => _shrines;

        private readonly List<Camp> _camps = new List<Camp>();
        private readonly List<Vector3> _shrines = new List<Vector3>();
        private readonly Dictionary<OrcUnit, Camp> _campOf = new Dictionary<OrcUnit, Camp>();
        private float _sinceHealing;

        private void OnEnable() => OrcUnit.Died += OnMonsterDied;

        private void OnDisable() => OrcUnit.Died -= OnMonsterDied;

        /// <summary>Puts the camps, the boss and the shrines down at the map's spots.</summary>
        public void Begin(IEnumerable<MapSpot> monsterSpots, IEnumerable<MapSpot> bossSpots, IEnumerable<MapSpot> shrineSpots, GameObject shrineModel)
        {
            foreach (var spot in monsterSpots)
            {
                var camp = new Camp { Post = Ground(spot.position), Tier = Mathf.Clamp(spot.level, 1, 3) };
                _camps.Add(camp);
                Spawn(camp);
            }
            foreach (var spot in bossSpots)
            {
                BossLair = new Camp { Post = Ground(spot.position), Tier = 3, IsBoss = true };
                _camps.Add(BossLair);
                Spawn(BossLair);
                break;
            }
            foreach (var spot in shrineSpots)
            {
                var at = Ground(spot.position);
                _shrines.Add(at);
                PlaceShrine(shrineModel, at);
            }
        }

        private void Update() => Tick(Time.deltaTime);

        /// <summary>The jungle's clock: the camps' return and the shrines' healing. Public so the tests can run it on.</summary>
        public void Tick(float dt)
        {
            foreach (var camp in _camps)
            {
                if (camp.RespawnIn <= 0f) continue;
                camp.RespawnIn -= dt;
                if (camp.RespawnIn <= 0f) Spawn(camp);
            }

            // the shrines heal a few times a second, for the time since they last did
            _sinceHealing += dt;
            if (_sinceHealing < ShrineTickSeconds || _shrines.Count == 0) return;
            HealAtShrines(_sinceHealing);
            _sinceHealing = 0f;
        }

        private void Spawn(Camp camp)
        {
            camp.RespawnIn = 0f;
            var raids = OrcRaidManager.Instance;
            if (raids == null) return;

            var config = BalanceConfig.Instance;
            var waves = InvasionMode.Instance != null ? InvasionMode.Instance.Waves : null;
            camp.Level = MonsterLevel(waves != null ? waves.WavesSurvived : 0, config.InvasionMonsterLevelEveryWaves);
            var kind = camp.IsBoss ? OrcKind.Giant : KindOfCamp(camp.Tier);
            var count = camp.IsBoss ? 1 : CampSize(camp.Tier);
            var radius = camp.IsBoss ? config.InvasionBossGuardRadius : config.InvasionMonsterGuardRadius;

            camp.Monsters.Clear();
            for (var i = 0; i < count; i++)
            {
                // each its own post round the spot, all facing out of the middle
                var angle = i * Mathf.PI * 2f / Mathf.Max(1, count);
                var post = count > 1 ? camp.Post + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * CampSpreadMeters : camp.Post;
                if (NavMesh.SamplePosition(post, out var hit, 2f, NavMesh.AllAreas)) post = hit.position;
                var monster = raids.SpawnOrcOfKind(post, camp.Level, kind);
                if (monster == null) continue;
                monster.transform.position = post;
                monster.SetGuardPost(null, post, radius);
                camp.Monsters.Add(monster);
                _campOf[monster] = camp;
            }
            if (camp.IsBoss && camp.TimesSlain > 0)
            {
                EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_inv_boss_back"), EventSeverity.Warning,
                    EventGlyph.Skull, null, camp.Post));
            }
        }

        private void OnMonsterDied(OrcUnit monster)
        {
            if (monster == null || !_campOf.TryGetValue(monster, out var camp)) return;
            _campOf.Remove(monster);
            camp.Monsters.Remove(monster);
            if (camp.Monsters.Count > 0) return;

            var config = BalanceConfig.Instance;
            camp.TimesSlain++;
            camp.RespawnIn = camp.IsBoss ? config.InvasionBossRespawnSeconds : config.InvasionCampRespawnSeconds;
            if (camp.IsBoss) RewardTheBoss(camp.Level, monster.transform.position);
        }

        /// <summary>The giant is down: resources × his level and the King's blessing.</summary>
        private static void RewardTheBoss(int level, Vector3 where)
        {
            var config = BalanceConfig.Instance;
            var coins = config.InvasionBossCoins * level;
            var wood = config.InvasionBossWood * level;
            var stone = config.InvasionBossStone * level;
            var resources = ResourceManager.Instance;
            if (resources != null)
            {
                resources.Add(InvasionRules.Money, coins);
                resources.Add(ResourceType.Wood, wood);
                resources.Add(ResourceType.Stone, stone);
            }
            var king = InvasionMode.Instance != null ? InvasionMode.Instance.King : null;
            if (king != null) king.Empower(config.InvasionBossBuffSeconds, config.InvasionBossBuffDamage, config.InvasionBossBuffHealth);
            EventLogManager.Instance?.Post(GameEvent.At(
                Localization.Format("#log_inv_boss_slain", coins, wood, stone, Mathf.RoundToInt(config.InvasionBossBuffSeconds)),
                EventSeverity.Good, EventGlyph.Star, null, where));
        }

        /// <summary>Everyone of the player's side standing at a shrine gets back a share of their health.</summary>
        private void HealAtShrines(float seconds)
        {
            var config = BalanceConfig.Instance;
            var radius = config.InvasionShrineRadius;
            var share = config.InvasionShrineHealPerSecond * seconds;
            foreach (var soldier in SoldierUnit.All)
            {
                if (soldier == null || soldier.CurrentHealth <= 0 || soldier.CurrentHealth >= soldier.MaxHealth) continue;
                if (!AtAShrine(soldier.transform.position, radius)) continue;
                var amount = Mathf.Max(1, Mathf.CeilToInt(soldier.MaxHealth * share));
                soldier.SetCurrentHealth(soldier.CurrentHealth + amount);
            }
        }

        private bool AtAShrine(Vector3 position, float radius)
        {
            foreach (var shrine in _shrines)
            {
                var offset = position - shrine;
                offset.y = 0f;
                if (offset.sqrMagnitude <= radius * radius) return true;
            }
            return false;
        }

        private static void PlaceShrine(GameObject model, Vector3 at)
        {
            GameObject shrine;
            if (model != null) shrine = Instantiate(model, at, Quaternion.identity);
            else
            {
                shrine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(shrine.GetComponent<Collider>());
                shrine.transform.position = at + Vector3.up * 0.3f;
                shrine.transform.localScale = new Vector3(1f, 0.3f, 1f);
            }
            shrine.name = "Shrine";
            SceneryShadows.DisableCasting(shrine);
        }

        /// <summary>A spot on the walkable ground, or at the ground height where there is none.</summary>
        private static Vector3 Ground(Vector2 spot)
        {
            var at = new Vector3(spot.x, GridManager.Instance != null ? GridManager.Instance.GroundHeight : 0f, spot.y);
            if (NavMesh.SamplePosition(at, out var hit, 6f, NavMesh.AllAreas)) return hit.position;
            Debug.LogError($"InvasionJungle: no walkable ground near the spot at {spot}.");
            return at;
        }

        // ---------------------------------------------------------------- the rules, pure

        /// <summary>The monsters' level after `wavesSurvived` waves: 1, a level higher every `every` waves.</summary>
        public static int MonsterLevel(int wavesSurvived, int every) => every > 0 ? 1 + Mathf.Max(0, wavesSurvived) / every : 1;

        /// <summary>Who lives in a camp of this level: wolves, bears, rock trolls.</summary>
        public static OrcKind KindOfCamp(int tier)
        {
            switch (tier)
            {
                case 1: return OrcKind.Wolf;
                case 2: return OrcKind.Bear;
                default: return OrcKind.Troll;
            }
        }

        public static int CampSize(int tier)
        {
            var config = BalanceConfig.Instance;
            switch (tier)
            {
                case 1: return config.InvasionCamp1Monsters;
                case 2: return config.InvasionCamp2Monsters;
                default: return config.InvasionCamp3Monsters;
            }
        }
    }
}
