using System;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Resources;
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
    /// One row of the balance sheet's "buildings" tab: everything about a building that is a NUMBER
    /// rather than a shape. What the building looks like -- footprint, height, colours, which
    /// procedural generator or FBX builds its prefab -- stays in SetupProject, because none of it is
    /// balance and a spreadsheet is a bad place to author geometry.
    ///
    /// Read at build time only: SetupProject bakes these into the BuildingData assets the game
    /// actually loads, so the runtime never walks this list.
    /// </summary>
    [Serializable]
    public class BuildingBalance
    {
        public string id = string.Empty;
        public string displayName = string.Empty;
        public BuildingCategory category = BuildingCategory.Production;

        /// <summary>Placement cost. Built from the tab's cost_wood/cost_stone/... columns, zeroes omitted.</summary>
        public List<ResourceAmount> cost = new List<ResourceAmount>();

        public ResourceType producesResource = ResourceType.Wood;
        public float productionIntervalSeconds = 6f;
        public int fogRevealRadius = 8;

        /// <summary>Which family of resources this building stores, if any. How much it holds is per level.</summary>
        public ResourceStorageGroup storageGroup = ResourceStorageGroup.None;

        /// <summary>
        /// One entry per upgrade level, always three of them. The sheet authors level 1 in the
        /// plain columns (max_health, defense, ...) and the higher levels in suffixed ones
        /// (max_health_2, max_health_3); an absent or empty suffixed column repeats the level below,
        /// so a building that gains nothing from upgrading simply has nothing extra to fill in.
        /// </summary>
        public List<BuildingLevelStats> levels = new List<BuildingLevelStats>();

        /// <summary>Id of a building that must already stand somewhere before this one can be placed. Empty = no prerequisite.</summary>
        public string requiredBuildingId = string.Empty;

        /// <summary>
        /// Upgrade costs, spelled out per level instead of derived in code from a multiplier. The
        /// sheet computes the ordinary cases with a formula (base cost x the level's multiplier) and
        /// the exceptions are authored by hand -- the Town Hall, which is free to place and so has
        /// nothing to scale, and the iron/coal that gate the mines and the defence line. That gating
        /// used to live in six scattered mutations in SetupProject, where it was invisible next to
        /// the numbers it was balancing against.
        /// </summary>
        public List<ResourceAmount> upgradeToLevel2Cost = new List<ResourceAmount>();
        public List<ResourceAmount> upgradeToLevel3Cost = new List<ResourceAmount>();
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

        [Header("Buildings (buildings.csv)")]
        [SerializeField] private List<BuildingBalance> buildings = new List<BuildingBalance>();

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

        [Header("Storage (economy.csv)")]
        // What the settlement can hold before a single storehouse is built. Without these a new
        // game would be unable to keep the resources it starts with.
        [SerializeField] private int baseCapacityMaterials = 200;
        [SerializeField] private int baseCapacityFood = 100;
        [SerializeField] private int baseCapacityValuables = 300;

        public IReadOnlyList<UnitBalance> Units => units;
        public IReadOnlyList<BuildingBalance> Buildings => buildings;
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
        public int BaseCapacityMaterials => baseCapacityMaterials;
        public int BaseCapacityFood => baseCapacityFood;
        public int BaseCapacityValuables => baseCapacityValuables;

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

        /// <summary>
        /// A building row by id ("House", "Tower"), or null if the sheet has no such row. Callers
        /// decide how loud a miss is: SetupProject treats it as a build error, because a building
        /// with no balance row would otherwise be generated silently full of defaults.
        /// </summary>
        public BuildingBalance Building(string id)
        {
            foreach (var building in buildings)
            {
                if (building.id == id) return building;
            }
            return null;
        }

        /// <summary>Editor-side entry point for the CSV importer -- see BalanceImporter.</summary>
        public void OverwriteFrom(List<UnitBalance> importedUnits, List<BuildingBalance> importedBuildings, Dictionary<string, float> economy)
        {
            units = importedUnits;
            buildings = importedBuildings;

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
            baseCapacityMaterials = (int)Read(economy, "base_capacity_materials", baseCapacityMaterials);
            baseCapacityFood = (int)Read(economy, "base_capacity_food", baseCapacityFood);
            baseCapacityValuables = (int)Read(economy, "base_capacity_valuables", baseCapacityValuables);
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
