using System;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Core
{
    /// <summary>
    /// What one research costs and how long it takes at an unstaffed Laboratory. Seconds are the
    /// BASE duration: scientists shorten it (see CityBuilder.Research.ResearchManager), they don't
    /// multiply it, which is why this is a plain number and not a rate.
    /// </summary>
    [Serializable]
    public class ResearchStep
    {
        public int coins;
        public float seconds;

        /// <summary>A step the sheet never filled in -- nothing to research, so nothing to gate on.</summary>
        public bool IsAuthored => seconds > 0f || coins > 0;
    }

    /// <summary>
    /// What a fighting unit is worth at one level. The same reasoning as BuildingLevelStats: these
    /// are exactly the stats a Laboratory upgrade improves, so nobody can read a soldier's strength
    /// without saying which level they mean.
    ///
    /// Authored per level in the units tab -- max_health / max_health_2 / max_health_3 and so on,
    /// where an empty higher-level column repeats the level below it.
    /// </summary>
    [Serializable]
    public class UnitLevelStats
    {
        public int maxHealth = 1;
        public int attackDamage;
        public float attackIntervalSeconds = 1f;
        public float walkSpeed = 1.3f;

        /// <summary>Recruitment cost in coins. Zero for units the player can't recruit (orcs).</summary>
        public int recruitCoins;

        public int upkeepCoinsPerDay;
    }

    /// <summary>One row of the balance sheet's "units" tab -- everything that makes a fighting unit what it is.</summary>
    [Serializable]
    public class UnitBalance
    {
        /// <summary>Levels a unit can reach. Level 1 is how it is recruited; 2 and 3 come from the Laboratory.</summary>
        public const int MaxLevel = 3;

        public string id = string.Empty;
        public string displayName = string.Empty;

        /// <summary>Melee reach against another unit.</summary>
        public float attackRangeUnits = 1.4f;

        /// <summary>Reach against a building/portal. Wider on purpose: a structure's transform sits at its centre, metres from the wall being hit.</summary>
        public float attackRangeStructures = 3f;

        /// <summary>How far from where it belongs a unit will engage something it wasn't ordered to.</summary>
        public float engageRadius = 6f;

        /// <summary>
        /// Reach and engage radius are deliberately NOT per level: they are the unit's geometry, not
        /// its strength, and a tier that suddenly outranges itself would be a surprise rather than an
        /// upgrade. Everything the player would call a stat is in `levels`.
        /// </summary>
        public List<UnitLevelStats> levels = new List<UnitLevelStats>();

        /// <summary>False = this unit type cannot be recruited until it is opened in the Laboratory. Militia starts open; later tiers will not.</summary>
        public bool startsUnlocked = true;

        /// <summary>What it costs to research levels 2 and 3, index 0 being level 2. Shorter than MaxLevel-1 for a unit the sheet gives no upgrades.</summary>
        public List<ResearchStep> levelResearch = new List<ResearchStep>();

        /// <summary>What it costs to make this type recruitable at all. Unused for a type that starts open.</summary>
        public ResearchStep unlockResearch = new ResearchStep();

        /// <summary>This unit's stats at the given level, clamped into what the sheet actually provided (see BuildingData.LevelStats for why this never returns null).</summary>
        public UnitLevelStats LevelStats(int level)
        {
            if (levels == null || levels.Count == 0) return FallbackLevel;

            var index = Mathf.Clamp(level, 1, levels.Count) - 1;
            return levels[index] ?? FallbackLevel;
        }

        /// <summary>The research that reaches the given level (2 or 3), or null when the sheet authors none.</summary>
        public ResearchStep ResearchToReach(int level)
        {
            var index = level - 2;
            if (levelResearch == null || index < 0 || index >= levelResearch.Count) return null;
            return levelResearch[index] != null && levelResearch[index].IsAuthored ? levelResearch[index] : null;
        }

        private static readonly UnitLevelStats FallbackLevel = new UnitLevelStats();
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

        /// <summary>
        /// What this building can make, from the recipes tab -- matched to this row by its id.
        /// Empty means it produces nothing, which is the ordinary case for a house or a wall and
        /// also the deliberate answer for the Пристань and the Водяная мельница.
        /// </summary>
        public List<BuildingRecipe> recipes = new List<BuildingRecipe>();

        public float productionIntervalSeconds = 6f;
        public int fogRevealRadius = 8;

        /// <summary>People who arrive with the building itself when it is placed -- see BuildingData.citizensOnBuild. Only the Town Hall has any.</summary>
        public int citizensOnBuild;

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

        /// <summary>
        /// False = this building cannot be built until it is opened in the Laboratory. Eighteen of
        /// the forty-nine start open (the Town Hall, the first houses, the first two gatherers, the
        /// fence line); everything else is a research away.
        /// </summary>
        public bool startsUnlocked = true;

        /// <summary>What opening this building for construction costs. Unused for a building that starts open.</summary>
        public ResearchStep unlockResearch = new ResearchStep();

        /// <summary>
        /// What researching each higher level costs, index 0 being level 2. Paying the upgrade cost
        /// in BuildingInstance.TryUpgrade is a separate, later transaction -- the research permits the
        /// upgrade, it does not perform it.
        /// </summary>
        public List<ResearchStep> levelResearch = new List<ResearchStep>();

        /// <summary>The research that permits upgrading to the given level (2 or 3), or null when the sheet authors none -- which is how the Laboratory itself stays outside its own gate.</summary>
        public ResearchStep ResearchToReach(int level)
        {
            var index = level - 2;
            if (levelResearch == null || index < 0 || index >= levelResearch.Count) return null;
            return levelResearch[index] != null && levelResearch[index].IsAuthored ? levelResearch[index] : null;
        }
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
        [SerializeField] private int raidProgressPerExtraRaider = 40;
        [SerializeField] private int raidMaxSize = 8;
        // A raid grows in two directions, not one: more raiders up to the cap, and tougher ones
        // past it. Without the second, a settlement that has out-built the size ceiling is never
        // threatened again.
        [SerializeField] private int raidProgressPerOrcLevel = 150;
        [SerializeField] private int raidMaxOrcLevel = 5;
        // And they come more often. raidIntervalSeconds is the wait at a standing start, this is
        // the wait once the player has become the score below -- interpolated in between.
        [SerializeField] private float raidMinIntervalSeconds = 45f;
        [SerializeField] private int raidProgressAtMinInterval = 400;
        [SerializeField] private int portalMaxHealth = 320;

        [Header("Player progression (economy.csv)")]
        // What each part of the settlement is worth to the progression score raids are measured
        // against -- see PlayerProgression, which explains why these five terms and not others.
        // Defence is weighted per POINT of defence rather than per building, and the sheet's
        // defence values run from 10 to 138, which is why its weight is a fraction where the
        // others are whole numbers.
        [SerializeField] private float progressPerBuildingLevel = 1f;
        [SerializeField] private float progressPerCitizen = 1f;
        [SerializeField] private float progressPerSoldierLevel = 2f;
        [SerializeField] private float progressPerDefencePoint = 0.2f;
        [SerializeField] private float progressPerProducedUnit = 0.02f;

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
        // What one tree or one boulder holds in total, what a worker carries away per trip, and
        // how long that trip's digging takes. A tree's per-trip yield equals its whole stock, so
        // one visit fells it; a boulder is chipped away over many, and never comes back.
        [SerializeField] private int woodPerTree = 5;
        [SerializeField] private int stonePerRock = 20;
        [SerializeField] private int woodPerHarvest = 5;
        [SerializeField] private int stonePerHarvest = 2;
        [SerializeField] private float harvestSeconds = 15f;

        [Header("Food (economy.csv)")]
        // What one mouth -- citizen or soldier -- eats per day, how long a settlement may go short
        // before people start dying, how many kinds of food count as a varied diet, and how many
        // days of deaths the happiness model keeps remembering.
        [SerializeField] private float foodPerMouthPerDay = 0.5f;
        [SerializeField] private int hungryDaysBeforeDeaths = 2;
        [SerializeField] private int foodVarietyTarget = 2;
        [SerializeField] private int deathsMemoryDays = 3;
        [SerializeField] private int happinessPenaltyPerDeath = 10;

        /// <summary>Entertainment points per citizen that count as a fully entertained settlement -- the same shape as the defence factor's own target, which is a constant in HappinessManager.</summary>
        [SerializeField] private float happinessPerCitizenTarget = 0.8f;

        [Header("Migration (economy.csv)")]
        // How fast people find their way to the settlement, or away from it. Contentment above the
        // threshold brings settlers in and below it drives them out, and the further from the
        // threshold in either direction the shorter the wait -- the threshold itself is the dead
        // point where nobody moves. The floor is what keeps unhappiness from ever ending the map:
        // a miserable town stalls, it does not empty, and starvation stays the way to lose people.
        [SerializeField] private int migrationHappinessThreshold = 30;
        [SerializeField] private float migrationArriveIntervalAtThresholdSeconds = 90f;
        [SerializeField] private float migrationArriveIntervalAtFullSeconds = 30f;
        [SerializeField] private float migrationLeaveIntervalAtThresholdSeconds = 90f;
        [SerializeField] private float migrationLeaveIntervalAtZeroSeconds = 30f;
        [SerializeField] private int migrationMinPopulation = 1;

        /// <summary>How long after the Town Hall goes up the settlement is left alone to find its feet -- migration frozen, contentment still reported honestly. Without it a first settlement is abandoned before the player has anything to be content about.</summary>
        [SerializeField] private float settlingInSeconds = 420f;

        [Header("Research (economy.csv)")]
        // How the Laboratory's scientists shorten a research, what a cancelled one pays back, and
        // the floor a fully staffed lab can never dip below. The first `research_free_workers`
        // scientists buy no speed at all -- one of them is what makes the research run in the first
        // place, so the bonus starts with the second.
        [SerializeField] private float researchSecondsPerWorker = 5f;
        [SerializeField] private int researchFreeWorkers = 1;
        [SerializeField] private int researchCancelRefundPercent = 70;
        [SerializeField] private float researchMinSeconds = 5f;

        [Header("Storage (economy.csv)")]
        // What the settlement can hold before a single storehouse is built. Without these a new
        // game would be unable to keep the resources it starts with.
        [SerializeField] private int baseCapacityMaterials = 200;
        [SerializeField] private int baseCapacityFood = 100;
        [SerializeField] private int baseCapacityValuables = 300;
        [SerializeField] private int baseCapacityGrain = 150;

        public IReadOnlyList<UnitBalance> Units => units;
        public IReadOnlyList<BuildingBalance> Buildings => buildings;
        public int ArmyMaxSize => armyMaxSize;
        public float RaidIntervalSeconds => raidIntervalSeconds;
        public int RaidBaseSize => raidBaseSize;
        public int RaidProgressPerExtraRaider => raidProgressPerExtraRaider;
        public int RaidMaxSize => raidMaxSize;
        public int RaidProgressPerOrcLevel => raidProgressPerOrcLevel;
        public int RaidMaxOrcLevel => raidMaxOrcLevel;
        public float RaidMinIntervalSeconds => raidMinIntervalSeconds;
        public int RaidProgressAtMinInterval => raidProgressAtMinInterval;
        public float ProgressPerBuildingLevel => progressPerBuildingLevel;
        public float ProgressPerCitizen => progressPerCitizen;
        public float ProgressPerSoldierLevel => progressPerSoldierLevel;
        public float ProgressPerDefencePoint => progressPerDefencePoint;
        public float ProgressPerProducedUnit => progressPerProducedUnit;
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
        public int WoodPerHarvest => woodPerHarvest;
        public int StonePerHarvest => stonePerHarvest;
        public float HarvestSeconds => harvestSeconds;
        public int BaseCapacityMaterials => baseCapacityMaterials;
        public int BaseCapacityFood => baseCapacityFood;
        public int BaseCapacityValuables => baseCapacityValuables;
        public int BaseCapacityGrain => baseCapacityGrain;
        public float FoodPerMouthPerDay => foodPerMouthPerDay;
        public int HungryDaysBeforeDeaths => hungryDaysBeforeDeaths;
        public int FoodVarietyTarget => foodVarietyTarget;
        public int DeathsMemoryDays => deathsMemoryDays;
        public int HappinessPenaltyPerDeath => happinessPenaltyPerDeath;
        public float HappinessPerCitizenTarget => happinessPerCitizenTarget;
        public float ResearchSecondsPerWorker => researchSecondsPerWorker;
        public int ResearchFreeWorkers => researchFreeWorkers;
        public int ResearchCancelRefundPercent => researchCancelRefundPercent;
        public float ResearchMinSeconds => researchMinSeconds;
        public int MigrationHappinessThreshold => migrationHappinessThreshold;
        public float MigrationArriveIntervalAtThresholdSeconds => migrationArriveIntervalAtThresholdSeconds;
        public float MigrationArriveIntervalAtFullSeconds => migrationArriveIntervalAtFullSeconds;
        public float MigrationLeaveIntervalAtThresholdSeconds => migrationLeaveIntervalAtThresholdSeconds;
        public float MigrationLeaveIntervalAtZeroSeconds => migrationLeaveIntervalAtZeroSeconds;
        public int MigrationMinPopulation => migrationMinPopulation;
        public float SettlingInSeconds => settlingInSeconds;

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
            raidProgressPerExtraRaider = (int)Read(economy, "raid_progress_per_extra_raider", raidProgressPerExtraRaider);
            raidMaxSize = (int)Read(economy, "raid_max_size", raidMaxSize);
            raidProgressPerOrcLevel = (int)Read(economy, "raid_progress_per_orc_level", raidProgressPerOrcLevel);
            raidMaxOrcLevel = (int)Read(economy, "raid_max_orc_level", raidMaxOrcLevel);
            raidMinIntervalSeconds = Read(economy, "raid_min_interval_seconds", raidMinIntervalSeconds);
            raidProgressAtMinInterval = (int)Read(economy, "raid_progress_at_min_interval", raidProgressAtMinInterval);
            progressPerBuildingLevel = Read(economy, "progress_per_building_level", progressPerBuildingLevel);
            progressPerCitizen = Read(economy, "progress_per_citizen", progressPerCitizen);
            progressPerSoldierLevel = Read(economy, "progress_per_soldier_level", progressPerSoldierLevel);
            progressPerDefencePoint = Read(economy, "progress_per_defence_point", progressPerDefencePoint);
            progressPerProducedUnit = Read(economy, "progress_per_produced_unit", progressPerProducedUnit);
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
            woodPerHarvest = (int)Read(economy, "wood_per_harvest", woodPerHarvest);
            stonePerHarvest = (int)Read(economy, "stone_per_harvest", stonePerHarvest);
            harvestSeconds = Read(economy, "harvest_seconds", harvestSeconds);
            baseCapacityMaterials = (int)Read(economy, "base_capacity_materials", baseCapacityMaterials);
            baseCapacityFood = (int)Read(economy, "base_capacity_food", baseCapacityFood);
            baseCapacityValuables = (int)Read(economy, "base_capacity_valuables", baseCapacityValuables);
            baseCapacityGrain = (int)Read(economy, "base_capacity_grain", baseCapacityGrain);
            foodPerMouthPerDay = Read(economy, "food_per_mouth_per_day", foodPerMouthPerDay);
            hungryDaysBeforeDeaths = (int)Read(economy, "hungry_days_before_deaths", hungryDaysBeforeDeaths);
            foodVarietyTarget = (int)Read(economy, "food_variety_target", foodVarietyTarget);
            deathsMemoryDays = (int)Read(economy, "deaths_memory_days", deathsMemoryDays);
            happinessPenaltyPerDeath = (int)Read(economy, "happiness_penalty_per_death", happinessPenaltyPerDeath);
            happinessPerCitizenTarget = Read(economy, "happiness_per_citizen_target", happinessPerCitizenTarget);
            researchSecondsPerWorker = Read(economy, "research_seconds_per_worker", researchSecondsPerWorker);
            researchFreeWorkers = (int)Read(economy, "research_free_workers", researchFreeWorkers);
            researchCancelRefundPercent = (int)Read(economy, "research_cancel_refund_percent", researchCancelRefundPercent);
            researchMinSeconds = Read(economy, "research_min_seconds", researchMinSeconds);
            migrationHappinessThreshold = (int)Read(economy, "migration_happiness_threshold", migrationHappinessThreshold);
            migrationArriveIntervalAtThresholdSeconds = Read(economy, "migration_arrive_interval_at_threshold_sec", migrationArriveIntervalAtThresholdSeconds);
            migrationArriveIntervalAtFullSeconds = Read(economy, "migration_arrive_interval_at_full_sec", migrationArriveIntervalAtFullSeconds);
            migrationLeaveIntervalAtThresholdSeconds = Read(economy, "migration_leave_interval_at_threshold_sec", migrationLeaveIntervalAtThresholdSeconds);
            migrationLeaveIntervalAtZeroSeconds = Read(economy, "migration_leave_interval_at_zero_sec", migrationLeaveIntervalAtZeroSeconds);
            migrationMinPopulation = (int)Read(economy, "migration_min_population", migrationMinPopulation);
            settlingInSeconds = Read(economy, "settling_in_seconds", settlingInSeconds);
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
