using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Core
{
    /// <summary>One row of the balance sheet's "units" tab -- everything that makes a fighting unit what it is.</summary>
    [Serializable]
    public class UnitBalance
    {
        public string id = string.Empty;
        public string displayName = string.Empty;
        public int maxHealth = 1;
        public int attackDamage;
        public float attackIntervalSeconds = 1f;

        /// <summary>Melee reach against another unit.</summary>
        public float attackRangeUnits = 1.4f;

        /// <summary>Reach against a building/portal. Wider on purpose: a structure's transform sits at its centre, metres from the wall being hit.</summary>
        public float attackRangeStructures = 3f;

        public float walkSpeed = 1.3f;

        /// <summary>How far from where it belongs a unit will engage something it wasn't ordered to.</summary>
        public float engageRadius = 6f;

        /// <summary>Recruitment cost in coins. Zero for units the player can't recruit (orcs).</summary>
        public int recruitCoins;

        public int upkeepCoinsPerDay;
    }

    /// <summary>
    /// Every tunable gameplay number in one asset, generated from the balance spreadsheet's CSV
    /// export (see Assets/_Project/Balance and the Editor-side BalanceImporter). The spreadsheet is
    /// where balance is AUTHORED -- with derived columns showing what the numbers actually mean
    /// (dps, time-to-kill, payback in days) -- and this asset is what the game reads.
    ///
    /// Read once and cached: consumers pull what they need into locals/fields at startup rather
    /// than reaching through Instance every frame, so a number here costs exactly what a const did.
    ///
    /// The values written below as field initializers are a LAST-RESORT fallback for a build whose
    /// asset failed to generate -- never the place to change balance, since any import overwrites
    /// them. They exist so a broken import ships a playable game with a loud error rather than a
    /// game full of zeroes.
    /// </summary>
    public class BalanceConfig : ScriptableObject
    {
        /// <summary>Resources path (no extension) the runtime loads this from.</summary>
        public const string ResourcePath = "BalanceConfig";

        private static BalanceConfig _instance;

        [Header("Units (units.csv)")]
        [SerializeField] private List<UnitBalance> units = new List<UnitBalance>();

        [Header("Army (economy.csv)")]
        [SerializeField] private int armyMaxSize = 20;

        [Header("Raids (economy.csv)")]
        [SerializeField] private float raidIntervalSeconds = 90f;
        [SerializeField] private int raidBaseSize = 2;
        [SerializeField] private int raidDaysPerExtraRaider = 3;
        [SerializeField] private int raidMaxSize = 8;
        [SerializeField] private int portalMaxHealth = 320;

        [Header("Defensive buildings (economy.csv)")]
        [SerializeField] private float defenceAttackIntervalSeconds = 1f;
        [SerializeField] private float defenceAttackRangeMeters = 6f;

        [Header("Economy (economy.csv)")]
        [SerializeField] private float dayLengthSeconds = 120f;
        [SerializeField] private float coinsPerCitizenPerDayAtMaxTax = 0.5f;
        [SerializeField] private float decayPerDayAtLevel1 = 0.02f;
        [SerializeField] private float decayPenaltyThreshold = 0.7f;
        [SerializeField] private float minDecayProductionMultiplier = 0.5f;
        [SerializeField] private float repairCostFraction = 0.4f;
        [SerializeField] private int woodPerTree = 5;
        [SerializeField] private int stonePerRock = 4;

        public IReadOnlyList<UnitBalance> Units => units;
        public int ArmyMaxSize => armyMaxSize;
        public float RaidIntervalSeconds => raidIntervalSeconds;
        public int RaidBaseSize => raidBaseSize;
        public int RaidDaysPerExtraRaider => raidDaysPerExtraRaider;
        public int RaidMaxSize => raidMaxSize;
        public int PortalMaxHealth => portalMaxHealth;
        public float DefenceAttackIntervalSeconds => defenceAttackIntervalSeconds;
        public float DefenceAttackRangeMeters => defenceAttackRangeMeters;
        public float DayLengthSeconds => dayLengthSeconds;
        public float CoinsPerCitizenPerDayAtMaxTax => coinsPerCitizenPerDayAtMaxTax;
        public float DecayPerDayAtLevel1 => decayPerDayAtLevel1;
        public float DecayPenaltyThreshold => decayPenaltyThreshold;
        public float MinDecayProductionMultiplier => minDecayProductionMultiplier;
        public float RepairCostFraction => repairCostFraction;
        public int WoodPerTree => woodPerTree;
        public int StonePerRock => stonePerRock;

        /// <summary>
        /// The loaded config. Falls back to an in-memory instance carrying the field defaults above
        /// if the asset is missing, with one error in the log -- a build that lost its balance asset
        /// stays playable and says so, instead of silently running on zeroes.
        /// </summary>
        public static BalanceConfig Instance
        {
            get
            {
                if (_instance != null) return _instance;

                _instance = UnityEngine.Resources.Load<BalanceConfig>(ResourcePath);
                if (_instance == null)
                {
                    Debug.LogError($"BalanceConfig: no asset at Resources/{ResourcePath}. Running on built-in fallback values -- " +
                                   "regenerate it from Assets/_Project/Balance/*.csv (CityBuilder/Balance menu, or a full SetupProject run).");
                    _instance = CreateInstance<BalanceConfig>();
                }
                return _instance;
            }
        }

        /// <summary>A unit row by id ("militia", "orc"), or a defaulted row plus an error if the sheet has no such unit.</summary>
        public UnitBalance Unit(string id)
        {
            foreach (var unit in units)
            {
                if (unit.id == id) return unit;
            }

            Debug.LogError($"BalanceConfig: no unit '{id}' in the balance sheet's units tab. Using fallback stats.");
            return new UnitBalance { id = id, displayName = id };
        }

        /// <summary>Editor-side entry point for the CSV importer -- see BalanceImporter.</summary>
        public void OverwriteFrom(List<UnitBalance> importedUnits, Dictionary<string, float> economy)
        {
            units = importedUnits;

            armyMaxSize = (int)Read(economy, "army_max_size", armyMaxSize);
            raidIntervalSeconds = Read(economy, "raid_interval_seconds", raidIntervalSeconds);
            raidBaseSize = (int)Read(economy, "raid_base_size", raidBaseSize);
            raidDaysPerExtraRaider = (int)Read(economy, "raid_days_per_extra_raider", raidDaysPerExtraRaider);
            raidMaxSize = (int)Read(economy, "raid_max_size", raidMaxSize);
            portalMaxHealth = (int)Read(economy, "portal_max_health", portalMaxHealth);
            defenceAttackIntervalSeconds = Read(economy, "defence_attack_interval_seconds", defenceAttackIntervalSeconds);
            defenceAttackRangeMeters = Read(economy, "defence_attack_range_meters", defenceAttackRangeMeters);
            dayLengthSeconds = Read(economy, "day_length_seconds", dayLengthSeconds);
            coinsPerCitizenPerDayAtMaxTax = Read(economy, "coins_per_citizen_per_day_at_max_tax", coinsPerCitizenPerDayAtMaxTax);
            decayPerDayAtLevel1 = Read(economy, "decay_per_day_at_level1", decayPerDayAtLevel1);
            decayPenaltyThreshold = Read(economy, "decay_penalty_threshold", decayPenaltyThreshold);
            minDecayProductionMultiplier = Read(economy, "min_decay_production_multiplier", minDecayProductionMultiplier);
            repairCostFraction = Read(economy, "repair_cost_fraction", repairCostFraction);
            woodPerTree = (int)Read(economy, "wood_per_tree", woodPerTree);
            stonePerRock = (int)Read(economy, "stone_per_rock", stonePerRock);
        }

        /// <summary>A missing key is an error, not a silent default: the sheet and the game are supposed to describe the same set of numbers.</summary>
        private static float Read(Dictionary<string, float> economy, string key, float fallback)
        {
            if (economy.TryGetValue(key, out var value)) return value;

            Debug.LogError($"BalanceConfig: the balance sheet's economy tab has no key '{key}'. Keeping {fallback}.");
            return fallback;
        }
    }
}
