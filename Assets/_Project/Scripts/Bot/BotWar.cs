using System.Collections.Generic;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Diplomacy;
using CityBuilder.Ships;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Bot
{
    /// <summary>
    /// The bot kingdom at war (the user, 2026-11-04):
    /// - its garrison turns on the town: the town's-side garrison stands down (BotGarrison) and
    ///   the kingdom's own guards -- Солдат/Лучник королевства, hostile units of the orcs' system
    ///   (OrcKind.BotSoldier/BotArcher) -- hold the same posts, rushing the town's soldiers within
    ///   bot_war_guard_radius_cells; a fallen one comes back while the hall stands;
    /// - its towers shoot the town's soldiers (DefensiveBuilding), its buildings may be struck
    ///   (BotWarTarget);
    /// - it raids by sea, «десант и обстрел вместе»: the first raid bot_war_raid_first_sec after
    ///   the war began, then every bot_war_raid_interval_sec; what each raid carries is the
    ///   botwaves tab's wave of its number (a row per unit type: count and level; past the last
    ///   wave, the last again), in as many transports as it needs, with bot_war_escorts war ships
    ///   that shell the shore (ShipManager.LaunchBotFleet);
    /// - the war's clock runs (BotRelations.WarSeconds) and with it the ransom.
    /// At peace again every soldier of the kingdom on the field goes home (they are removed) and the
    /// old garrison comes back.
    /// </summary>
    public class BotWar : MonoBehaviour
    {
        private class Guard
        {
            public Vector3 post;
            public OrcKind kind;
            public OrcUnit unit;
            public float downFor;
        }

        private readonly List<Guard> _guards = new List<Guard>();
        // a raid's soldiers still aboard, by its number: unloaded transport by transport (Land)
        private static readonly Dictionary<int, Queue<(OrcKind kind, int level)>> Aboard = new Dictionary<int, Queue<(OrcKind, int)>>();
        private bool _atWar;
        private float _tick;
        private float _lastRaidAt = -1f;

        public static BotWar Instance { get; private set; }

        /// <summary>Where the kingdom's fleet comes from and goes home to: its Shipyard, else its hall.</summary>
        public static Vector3? Harbour
        {
            get
            {
                var kingdom = BotKingdom.Instance;
                if (kingdom == null) return null;
                var yard = kingdom.Shipyard;
                if (yard != null) return yard.transform.position;
                return kingdom.Hall != null ? kingdom.Hall.transform.position : (Vector3?)null;
            }
        }

        private void Awake()
        {
            Instance = this;
            Aboard.Clear();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            var kingdom = BotKingdom.Instance;
            var config = BalanceConfig.Instance;
            if (kingdom == null || !kingdom.Placed || config == null) return;
            var war = BotRelations.Stance == BotStance.War && !kingdom.HallRazed;
            if (war != _atWar)
            {
                _atWar = war;
                if (war) Begin(kingdom, config);
                else End();
            }
            if (!war) return;

            BotRelations.TickWar(Time.deltaTime);
            RaidOnTheClock(config);

            _tick += Time.deltaTime;
            if (_tick < 1f) return;
            var dt = _tick;
            _tick = 0f;
            if (kingdom.Hall == null) return;
            foreach (var guard in _guards)
            {
                if (guard.unit != null && guard.unit.CurrentHealth > 0) continue;
                guard.unit = null;
                guard.downFor += dt;
                if (guard.downFor < config.BotGarrisonRespawnSeconds) continue;
                guard.downFor = 0f;
                Raise(guard, kingdom, config);
            }
        }

        private void Begin(BotKingdom kingdom, BalanceConfig config)
        {
            var garrison = kingdom.GetComponent<BotGarrison>();
            if (garrison != null) garrison.StandDown(true);
            _guards.Clear();
            var melee = new List<Vector3>();
            var ranged = new List<Vector3>();
            BotGarrison.WarPosts(kingdom, melee, ranged);
            var soldiers = Mathf.Max(1, config.BotGarrisonPatrols * config.BotGarrisonPatrolSize + 1);
            for (var k = 0; k < soldiers; k++) _guards.Add(new Guard { post = melee[k % melee.Count], kind = OrcKind.BotSoldier });
            var archers = Mathf.Min(ranged.Count, config.BotGarrisonArchers);
            for (var k = 0; k < archers; k++) _guards.Add(new Guard { post = ranged[k * ranged.Count / Mathf.Max(1, archers)], kind = OrcKind.BotArcher });
            foreach (var guard in _guards) Raise(guard, kingdom, config);
            // a war loaded half-way through: the next raid is the one its clock is on
            _lastRaidAt = -1f;
        }

        private void End()
        {
            // the kingdom's soldiers go home: the guards and every landed raider
            var gone = new List<OrcUnit>();
            foreach (var orc in OrcUnit.All)
                if (orc != null && orc.BotSide) gone.Add(orc);
            foreach (var orc in gone) Destroy(orc.gameObject);
            _guards.Clear();
            var kingdom = BotKingdom.Instance;
            var garrison = kingdom != null ? kingdom.GetComponent<BotGarrison>() : null;
            if (garrison != null) garrison.StandDown(false);
        }

        private void Raise(Guard guard, BotKingdom kingdom, BalanceConfig config)
        {
            var raids = OrcRaidManager.Instance;
            if (raids == null) return;
            var post = guard.post;
            if (NavMesh.SamplePosition(post, out var hit, 3f, NavMesh.AllAreas)) post = hit.position;
            var cell = Grid.GridManager.Instance != null ? Grid.GridManager.Instance.CellSize : 1f;
            var unit = raids.SpawnRingGuard(post, config.BotWarLevel, guard.kind, config.BotWarGuardRadiusCells * cell);
            if (unit == null) return;
            kingdom.DressAsBot(unit.gameObject);
            guard.unit = unit;
        }

        /// <summary>A raid each time the war's clock passes the next one's mark: first, first + interval, ...</summary>
        private void RaidOnTheClock(BalanceConfig config)
        {
            var t = BotRelations.WarSeconds;
            var first = config.BotWarRaidFirstSeconds;
            var every = Mathf.Max(30f, config.BotWarRaidIntervalSeconds);
            if (t < first) return;
            var index = Mathf.FloorToInt((t - first) / every);
            var mark = first + index * every;
            if (_lastRaidAt < 0f)
            {
                // just begun (or loaded): a raid only if its mark was this very moment
                _lastRaidAt = t - mark < 1f ? mark - 1f : mark;
            }
            if (mark <= _lastRaidAt) return;
            _lastRaidAt = mark;
            RaidNow(index, config);
        }

        /// <summary>The raid number `index` (0 = the first): the botwaves tab's wave index + 1, out of the harbour.</summary>
        public void RaidNow(int index, BalanceConfig config)
        {
            var harbour = Harbour;
            var ships = ShipManager.Instance;
            if (!harbour.HasValue || ships == null) return;
            var units = new Queue<(OrcKind, int)>();
            foreach (var row in config.BotWave(index + 1))
            {
                if (!TryKindOf(row.unit, out var kind))
                {
                    Debug.LogWarning($"BotWar: the botwaves tab's unit '{row.unit}' is not a kingdom's unit (bot_soldier, bot_archer).");
                    continue;
                }
                for (var k = 0; k < row.count; k++) units.Enqueue((kind, Mathf.Max(1, row.level)));
            }
            if (units.Count == 0) return;
            var raid = ships.LaunchBotFleet(harbour.Value, units.Count, config.BotWarLevel, Mathf.Max(0, config.BotWarEscorts));
            if (raid != 0) Aboard[raid] = units;
        }

        private static bool TryKindOf(string id, out OrcKind kind)
        {
            foreach (OrcKind k in System.Enum.GetValues(typeof(OrcKind)))
            {
                if (!OrcUnit.IsBotSide(k) || OrcUnit.SheetIdOf(k) != id) continue;
                kind = k;
                return true;
            }
            kind = OrcKind.BotSoldier;
            return false;
        }

        /// <summary>A transport of raid `raid` puts `count` of its soldiers ashore (the wave's kinds and levels, in the table's order); they go for the town.</summary>
        public static OrcUnit Land(int raid, Vector3 beach, int count)
        {
            var raids = OrcRaidManager.Instance;
            if (raids == null) return null;
            Aboard.TryGetValue(raid, out var queue);
            OrcUnit first = null;
            for (var i = 0; i < count; i++)
            {
                // a raid loaded from a save has lost its manifest: a soldier of the guards' level
                var (kind, level) = queue != null && queue.Count > 0 ? queue.Dequeue()
                    : (OrcKind.BotSoldier, BalanceConfig.Instance != null ? BalanceConfig.Instance.BotWarLevel : 1);
                var unit = raids.SpawnOrcOfKind(beach, level, kind);
                if (unit == null) continue;
                if (BotKingdom.Instance != null) BotKingdom.Instance.DressAsBot(unit.gameObject);
                if (first == null) first = unit;
            }
            if (queue != null && queue.Count == 0) Aboard.Remove(raid);
            return first;
        }
    }
}
