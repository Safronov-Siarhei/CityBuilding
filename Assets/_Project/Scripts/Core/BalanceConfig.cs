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

    /// <summary>
    /// One of the Invasion jungle's monsters at level 1, from the economy tab's mon_ID_* keys --
    /// four short rows of numbers, kept beside the King's rather than as blank-ridden rows of the
    /// units tab. Its level (InvasionJungle) multiplies health and damage, as an orc's does.
    /// </summary>
    [Serializable]
    public class MonsterStats
    {
        public string id = string.Empty;
        public int maxHealth = 30;
        public int attackDamage = 4;
        public float attackIntervalSeconds = 1f;
        public float attackRange = 1.2f;
        public float walkSpeed = 1.6f;
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

        /// <summary>False = this unit type cannot be recruited until it is opened in the Laboratory. Militia starts open; every tier above it does not.</summary>
        public bool startsUnlocked = true;

        /// <summary>
        /// Smelted metal a recruit of this type has to be equipped with, on top of the coins.
        /// Militia is zero on both -- an armed peasant needs no forge, which is what makes it the
        /// tier a settlement can raise before it has an industry.
        ///
        /// FLAT, not per level, and deliberately: a level is training, and the kit a spearman is
        /// handed is the same kit whether or not the Laboratory has taught him to use it better.
        /// The level tax is paid in coins, where recruitCoins already rises.
        /// </summary>
        public int recruitIronBars;

        public int recruitCopperBars;

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
        /// One entry per upgrade level, always BuildingInstance.MaxLevel of them. The sheet authors level 1 in the
        /// plain columns (max_health, defense, ...) and the higher levels in suffixed ones
        /// (max_health_2 .. max_health_5); an absent or empty suffixed column repeats the level below,
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
        public List<ResourceAmount> upgradeToLevel4Cost = new List<ResourceAmount>();
        public List<ResourceAmount> upgradeToLevel5Cost = new List<ResourceAmount>();

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

        /// <summary>The research that permits upgrading to the given level (2..MaxLevel), or null when the sheet authors none -- which is how the Laboratory itself stays outside its own gate.</summary>
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
        // The orc camp round each portal (see OrcCamp).
        [SerializeField] private int orcTowerHealth = 160;
        [SerializeField] private int orcTowerDamage = 3;
        [SerializeField] private float orcTowerIntervalSeconds = 1.5f;
        [SerializeField] private float orcTowerRangeMeters = 7f;
        [SerializeField] private int orcTentHealth = 220;
        [SerializeField] private int orcTotemHealth = 120;
        [SerializeField] private int orcGarrisonSize = 3;
        [SerializeField] private int raidThrowerFromLevel = 2;
        [SerializeField] private int raidThrowerEvery = 3;
        [SerializeField] private int raidChiefFromLevel = 3;
        [SerializeField] private float orcGarrisonRespawnSeconds = 30f;
        [SerializeField] private float orcGarrisonGuardRadiusMeters = 8f;
        // The walled bases (the user's sketches, 2026-10-16): numbers per LEVEL of the building,
        // index 0 = level 1. Level 1 of the tower, tent and totem keep their old keys.
        [SerializeField] private int[] portalHealthByLevel = { 320, 480, 700 };
        [SerializeField] private int[] orcTowerHealthByLevel = { 160, 220, 300 };
        [SerializeField] private int[] orcTowerDamageByLevel = { 3, 4, 5 };
        [SerializeField] private float[] orcTowerRangeByLevel = { 7f, 7.5f, 8f };
        [SerializeField] private int orcGateTowerHealth = 260;
        [SerializeField] private int orcGateTowerDamage = 4;
        [SerializeField] private float orcGateTowerRangeMeters = 7f;
        [SerializeField] private int[] orcPalisadeHealthByLevel = { 60, 100 };
        [SerializeField] private int[] orcGateHealthByLevel = { 120, 180 };
        [SerializeField] private int[] orcTotemHealthByLevel = { 120, 200 };
        [SerializeField] private int[] orcStorehouseHealthByLevel = { 150, 220 };
        [SerializeField] private int orcBrutePitHealth = 240;
        [SerializeField] private int orcThrowerRangeHealth = 200;
        // How a camp grows with the player (see OrcCamp.LevelFor), and what breaking one pays.
        [SerializeField] private int orcCampLevel2Score = 200;
        [SerializeField] private int orcCampLevel3Score = 450;
        [SerializeField] private int orcCampLevelScorePerRank = 40;
        [SerializeField] private int orcCampLootCoins = 40;
        [SerializeField] private int orcCampLootWood = 60;
        [SerializeField] private int orcCampLootStone = 40;
        [SerializeField] private float orcCampBreatherSeconds = 240f;
        // The main base (the great portal's): its own stone buildings, its miniboss.
        [SerializeField] private int mainPortalHealth = 1200;
        [SerializeField] private int orcMainWallHealth = 160;
        [SerializeField] private int orcMainGateHealth = 450;
        [SerializeField] private int orcMainGateDamage = 6;
        [SerializeField] private float orcMainGateRangeMeters = 8f;
        [SerializeField] private int orcMainTowerHealth = 420;
        [SerializeField] private int orcMainTowerDamage = 6;
        [SerializeField] private float orcMainTowerRangeMeters = 8.5f;
        [SerializeField] private int orcMainTotemHealth = 320;
        [SerializeField] private int orcMainStorehouseHealth = 300;
        [SerializeField] private int orcMainSpawnerHealth = 380;
        [SerializeField] private int orcChiefHouseHealth = 600;
        [SerializeField] private float orcChiefRespawnSeconds = 120f;
        // The prisoners' cages (the user's idea, 2026-10-19) and the townsfolk they hold.
        [SerializeField] private int[] orcCageHealthByLevel = { 120, 180, 260 };
        [SerializeField] private int orcMainCageHealth = 360;
        [SerializeField] private int[] orcCagePrisonersByLevel = { 2, 3, 4 };
        [SerializeField] private int orcMainCagePrisoners = 5;
        [SerializeField] private int prisonerHealth = 20;
        [SerializeField] private float prisonerWalkSpeed = 1.2f;
        // The chief's treasury on the main base: its health and what breaking it pays.
        [SerializeField] private int orcMainTreasuryHealth = 500;
        // How near a designed map's orc camps and main base the Town Hall may stand.
        [SerializeField] private float townHallMinDistanceToOrcs = 45f;
        [SerializeField] private int orcTreasuryLootCoins = 400;
        [SerializeField] private int orcTreasuryLootIronBars = 30;
        [SerializeField] private int orcTreasuryLootCopperBars = 30;
        // How many raid sources the map opens and where they stand -- see PortalPlacement, which
        // turns these three into the spiral of slots around the Town Hall. Kept here rather than
        // as constants because "spread the portals over the whole map" has to be a number the user
        // can try in the sheet, not a code change.
        [SerializeField] private int portalCount = 5;
        [SerializeField] private float portalDistanceFromTownHallMeters = 20f;
        [SerializeField] private float portalDistanceStepMeters = 8f;

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
        [SerializeField] private float castleAttackRangeMultiplier = 1.5f;

        [Header("Economy (economy.csv)")]
        [SerializeField] private float dayLengthSeconds = 120f;
        [SerializeField] private float coinsPerCitizenPerDayAtMaxTax = 0.5f;
        [SerializeField] private float decayPerDayAtLevel1 = 0.02f;
        [SerializeField] private float decayPenaltyThreshold = 0.7f;
        [SerializeField] private float minDecayProductionMultiplier = 0.5f;
        [SerializeField] private float repairCostFraction = 0.4f;

        /// <summary>Share of everything sunk into a building (its cost plus every upgrade) handed back when it is demolished.</summary>
        [SerializeField] private float demolishRefundFraction = 0.5f;
        // What one tree or one boulder holds in total, what a worker carries away per trip, and
        // how long that trip's digging takes. A tree's per-trip yield equals its whole stock, so
        // one visit fells it; a boulder is chipped away over many, and never comes back.
        [SerializeField] private int woodPerTree = 5;
        [SerializeField] private int stonePerRock = 20;
        [SerializeField] private int woodPerHarvest = 5;
        [SerializeField] private int stonePerHarvest = 1;

        /// <summary>
        /// A boulder's stock by its size, smallest first -- the ten Stone models RockSpawner deals
        /// (the user, 2026-10-21: every stone gets its own stock). stone_rock_size_1..10.
        /// </summary>
        [SerializeField] private int[] stoneRockSizes = { 12, 12, 12, 15, 18, 22, 27, 33, 40, 50 };
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

        [Header("Fires (economy.csv)")]
        // A building only burns if it has been let go: the chance is the full-decay figure scaled
        // by how decayed it actually is, so a maintained town never catches fire at all and repair
        // stops being a chore and starts being fire insurance.
        [SerializeField] private float fireChancePerDayAtFullDecay = 0.3f;
        [SerializeField] private float fireSpreadChancePerDay = 0.4f;
        [SerializeField] private float fireSpreadRadiusMeters = 6f;
        [SerializeField] private int fireDamagePerSecond = 3;
        // How long a fire nobody attends lasts, and what each firefighter within reach takes off
        // that -- the same shape the Laboratory's scientists use on a research, and floored the
        // same way so no number of them makes a fire instantaneous.
        [SerializeField] private float fireBurnSeconds = 60f;
        [SerializeField] private float fireSecondsSavedPerFirefighter = 12f;
        [SerializeField] private float fireMinBurnSeconds = 5f;

        [Header("Illness (economy.csv)")]
        // Three chances that add up to one: the background risk, what going hungry adds, and what
        // living out of reach of a well adds. Then how long the ill last untreated, and how many
        // one healer gets back on their feet in a day.
        [SerializeField] private float sicknessBaseChancePerDay = 0.01f;
        [SerializeField] private float sicknessHungerChancePerDay = 0.15f;
        [SerializeField] private float sicknessThirstChancePerDay = 0.12f;
        [SerializeField] private int sicknessDaysBeforeDeath = 3;
        [SerializeField] private int healPerHealerPerDay = 2;

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

        // The King, the hero of the Invasion mode: his numbers at level 1 and what each level adds.
        [SerializeField] private int kingMaxHealth = 120;
        [SerializeField] private int kingAttackDamage = 10;
        [SerializeField] private float kingAttackIntervalSeconds = 1f;
        [SerializeField] private float kingAttackRange = 1.6f;
        [SerializeField] private float kingWalkSpeed = 2.2f;
        [SerializeField] private float kingEngageRadius = 8f;
        [SerializeField] private int kingHealthPerLevel = 20;
        [SerializeField] private int kingDamagePerLevel = 2;
        [SerializeField] private int kingXpPerLevel = 60;
        [SerializeField] private int kingMaxLevel = 25;
        [SerializeField] private float kingRespawnBaseSeconds = 8f;
        [SerializeField] private float kingRespawnPerLevelSeconds = 2f;
        [SerializeField] private float kingXpRadius = 12f;
        [SerializeField] private float kingGuardRadius = 30f;
        [SerializeField] private int kingRevealRadius = 9;

        // The Invasion run's own economy (InvasionRules): the houses and their people, the opening stock.
        [SerializeField] private int invasionHouses = 6;
        [SerializeField] private int invasionPeoplePerHouse = 3;
        [SerializeField] private float invasionPersonRespawnSeconds = 15f;
        [SerializeField] private int invasionStartWood = 60;
        [SerializeField] private int invasionStartStone = 40;
        [SerializeField] private int invasionStartCoins = 50;

        // The Invasion run's waves (InvasionWaves): when they come, how many lanes, what is in them, the gold.
        [SerializeField] private float invasionFirstWaveSeconds = 60f;
        [SerializeField] private float invasionBreatherSeconds = 25f;
        [SerializeField] private float invasionWaveOvertimeSeconds = 240f;
        [SerializeField] private int invasionTwoLanesFromWave = 4;
        [SerializeField] private int invasionThreeLanesFromWave = 8;
        [SerializeField] private int invasionWaveBaseOrcs = 3;
        [SerializeField] private int invasionWaveExtraOrcEvery = 2;
        [SerializeField] private int invasionWaveMaxOrcs = 10;
        [SerializeField] private int invasionWaveLevelEvery = 5;
        [SerializeField] private int invasionWaveMaxOrcLevel = 12;
        [SerializeField] private int invasionThrowerFromWave = 3;
        [SerializeField] private int invasionThrowerEvery = 3;
        [SerializeField] private int invasionBruteFromWave = 6;
        [SerializeField] private int invasionBruteEvery = 4;
        [SerializeField] private int invasionBossWaveEvery = 5;
        [SerializeField] private int invasionGoldOrc = 2;
        [SerializeField] private int invasionGoldThrower = 3;
        [SerializeField] private int invasionGoldBrute = 6;
        [SerializeField] private int invasionGoldChief = 20;
        [SerializeField] private int invasionGoldPerWave = 10;
        [SerializeField] private float invasionOrcBuildingRadius = 6f;

        // The Invasion run's jungle (InvasionJungle): its monsters, camps, shrines and boss.
        [SerializeField] private List<MonsterStats> monsters = new List<MonsterStats>
        {
            new MonsterStats { id = "wolf", maxHealth = 30, attackDamage = 3, attackIntervalSeconds = 1f, attackRange = 1.1f, walkSpeed = 2f },
            new MonsterStats { id = "bear", maxHealth = 90, attackDamage = 8, attackIntervalSeconds = 1.4f, attackRange = 1.3f, walkSpeed = 1.5f },
            new MonsterStats { id = "troll", maxHealth = 200, attackDamage = 14, attackIntervalSeconds = 1.5f, attackRange = 1.5f, walkSpeed = 1.3f },
            new MonsterStats { id = "giant", maxHealth = 900, attackDamage = 30, attackIntervalSeconds = 2f, attackRange = 2f, walkSpeed = 1.2f },
        };
        [SerializeField] private int invasionCamp1Monsters = 3;
        [SerializeField] private int invasionCamp2Monsters = 2;
        [SerializeField] private int invasionCamp3Monsters = 2;
        [SerializeField] private float invasionCampRespawnSeconds = 90f;
        [SerializeField] private float invasionBossRespawnSeconds = 300f;
        [SerializeField] private float invasionMonsterGuardRadius = 6f;
        [SerializeField] private float invasionBossGuardRadius = 9f;
        [SerializeField] private int invasionMonsterLevelEveryWaves = 5;
        [SerializeField] private int invasionGoldWolf = 3;
        [SerializeField] private int invasionGoldBear = 8;
        [SerializeField] private int invasionGoldTroll = 15;
        [SerializeField] private float invasionShrineRadius = 5f;
        [SerializeField] private float invasionShrineHealPerSecond = 0.15f;
        [SerializeField] private int invasionBossCoins = 150;
        [SerializeField] private int invasionBossWood = 80;
        [SerializeField] private int invasionBossStone = 60;
        [SerializeField] private float invasionBossBuffSeconds = 120f;
        [SerializeField] private float invasionBossBuffDamage = 0.5f;
        [SerializeField] private float invasionBossBuffHealth = 0.3f;
        // The Invasion run's round orc camps (InvasionOrcCamp): their buildings' level and their garrison.
        [SerializeField] private int invasionCampBuildingLevel = 3;
        [SerializeField] private int invasionCampGuardLevel = 5;
        [SerializeField] private int invasionCampRaiders = 8;
        [SerializeField] private int invasionCampThrowers = 4;
        [SerializeField] private float invasionCampGuardRadius = 8f;
        [SerializeField] private float invasionCampGuardRespawnSeconds = 20f;
        // The Invasion run's score (InvasionScore) and the local records (InvasionRecords).
        [SerializeField] private float invasionOrcHealthMultiplier = 4f;
        [SerializeField] private float invasionOrcDamageMultiplier = 1f;
        [SerializeField] private int invasionMaxEachYard = 1;
        [SerializeField] private float invasionMinimapRadius = 40f;
        [SerializeField] private float invasionWallHealthMultiplier = 4f;
        [SerializeField] private int invasionCitizenHealth = 12;
        [SerializeField] private float invasionBuildSecondsPerUnit = 0.5f;
        [SerializeField] private float invasionBuildSecondsMin = 3f;
        [SerializeField] private float invasionBuildSecondsMax = 45f;
        [SerializeField] private int invasionScorePerWaveSquared = 1;
        [SerializeField] private int invasionScoreBoss = 50;
        [SerializeField] private int invasionScoreCampBuilding = 10;
        [SerializeField] private int invasionRunsCountedPerDay = 5;

        /// <summary>What a new game opens with, before the Town Hall goes down.</summary>
        [SerializeField] private int startWood = 80;
        [SerializeField] private int startStone = 30;
        [SerializeField] private int startFood = 50;
        [SerializeField] private int startGold = 100;

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
        public float FireChancePerDayAtFullDecay => fireChancePerDayAtFullDecay;
        public float FireSpreadChancePerDay => fireSpreadChancePerDay;
        public float FireSpreadRadiusMeters => fireSpreadRadiusMeters;
        public int FireDamagePerSecond => fireDamagePerSecond;
        public float FireBurnSeconds => fireBurnSeconds;
        public float FireSecondsSavedPerFirefighter => fireSecondsSavedPerFirefighter;
        public float FireMinBurnSeconds => fireMinBurnSeconds;
        public float SicknessBaseChancePerDay => sicknessBaseChancePerDay;
        public float SicknessHungerChancePerDay => sicknessHungerChancePerDay;
        public float SicknessThirstChancePerDay => sicknessThirstChancePerDay;
        public int SicknessDaysBeforeDeath => sicknessDaysBeforeDeath;
        public int HealPerHealerPerDay => healPerHealerPerDay;
        public int PortalMaxHealth => portalMaxHealth;
        public int OrcTowerHealth => orcTowerHealth;
        public int OrcTowerDamage => orcTowerDamage;
        public float OrcTowerIntervalSeconds => orcTowerIntervalSeconds;
        public float OrcTowerRangeMeters => orcTowerRangeMeters;
        public int OrcTentHealth => orcTentHealth;
        public int OrcTotemHealth => orcTotemHealth;
        public int OrcGarrisonSize => orcGarrisonSize;
        /// <summary>The orc level from which waves and garrisons include throwers -- see OrcRaidManager.KindInGroup.</summary>
        public int RaidThrowerFromLevel => raidThrowerFromLevel;
        /// <summary>Every this-many-th orc of a wave or a garrison is a thrower.</summary>
        public int RaidThrowerEvery => raidThrowerEvery;
        /// <summary>The orc level from which a chief leads each wave.</summary>
        public int RaidChiefFromLevel => raidChiefFromLevel;
        public float OrcGarrisonRespawnSeconds => orcGarrisonRespawnSeconds;
        public float OrcGarrisonGuardRadiusMeters => orcGarrisonGuardRadiusMeters;
        public int PortalHealthAt(int level) => AtLevel(portalHealthByLevel, level);
        public int OrcTowerHealthAt(int level) => AtLevel(orcTowerHealthByLevel, level);
        public int OrcTowerDamageAt(int level) => AtLevel(orcTowerDamageByLevel, level);
        public float OrcTowerRangeAt(int level) => AtLevel(orcTowerRangeByLevel, level);
        public int OrcGateTowerHealth => orcGateTowerHealth;
        public int OrcGateTowerDamage => orcGateTowerDamage;
        public float OrcGateTowerRangeMeters => orcGateTowerRangeMeters;
        public int OrcPalisadeHealthAt(int level) => AtLevel(orcPalisadeHealthByLevel, level);
        public int OrcGateHealthAt(int level) => AtLevel(orcGateHealthByLevel, level);
        public int OrcTotemHealthAt(int level) => AtLevel(orcTotemHealthByLevel, level);
        public int OrcStorehouseHealthAt(int level) => AtLevel(orcStorehouseHealthByLevel, level);
        public int OrcBrutePitHealth => orcBrutePitHealth;
        public int OrcThrowerRangeHealth => orcThrowerRangeHealth;
        public int OrcCampLevel2Score => orcCampLevel2Score;
        public int OrcCampLevel3Score => orcCampLevel3Score;
        public int OrcCampLevelScorePerRank => orcCampLevelScorePerRank;
        public int OrcCampLootCoins => orcCampLootCoins;
        public int OrcCampLootWood => orcCampLootWood;
        public int OrcCampLootStone => orcCampLootStone;
        public float OrcCampBreatherSeconds => orcCampBreatherSeconds;
        public int MainPortalHealth => mainPortalHealth;
        public int OrcMainWallHealth => orcMainWallHealth;
        public int OrcMainGateHealth => orcMainGateHealth;
        public int OrcMainGateDamage => orcMainGateDamage;
        public float OrcMainGateRangeMeters => orcMainGateRangeMeters;
        public int OrcMainTowerHealth => orcMainTowerHealth;
        public int OrcMainTowerDamage => orcMainTowerDamage;
        public float OrcMainTowerRangeMeters => orcMainTowerRangeMeters;
        public int OrcMainTotemHealth => orcMainTotemHealth;
        public int OrcMainStorehouseHealth => orcMainStorehouseHealth;
        public int OrcMainSpawnerHealth => orcMainSpawnerHealth;
        public int OrcChiefHouseHealth => orcChiefHouseHealth;
        public float OrcChiefRespawnSeconds => orcChiefRespawnSeconds;
        public int OrcCageHealthAt(int level) => AtLevel(orcCageHealthByLevel, level);
        public int OrcMainCageHealth => orcMainCageHealth;
        public int OrcCagePrisonersAt(int level) => AtLevel(orcCagePrisonersByLevel, level);
        public int OrcMainCagePrisoners => orcMainCagePrisoners;
        public int PrisonerHealth => prisonerHealth;
        public float PrisonerWalkSpeed => prisonerWalkSpeed;
        public int OrcMainTreasuryHealth => orcMainTreasuryHealth;
        public float TownHallMinDistanceToOrcs => townHallMinDistanceToOrcs;
        public int OrcTreasuryLootCoins => orcTreasuryLootCoins;
        public int OrcTreasuryLootIronBars => orcTreasuryLootIronBars;
        public int OrcTreasuryLootCopperBars => orcTreasuryLootCopperBars;

        private static T AtLevel<T>(T[] byLevel, int level)
        {
            if (byLevel == null || byLevel.Length == 0) return default;
            return byLevel[Mathf.Clamp(level, 1, byLevel.Length) - 1];
        }
        public int PortalCount => portalCount;
        public float PortalDistanceFromTownHallMeters => portalDistanceFromTownHallMeters;
        public float PortalDistanceStepMeters => portalDistanceStepMeters;
        public float DefenceAttackIntervalSeconds => defenceAttackIntervalSeconds;
        public float DefenceAttackRangeMeters => defenceAttackRangeMeters;
        public float CastleAttackRangeMultiplier => castleAttackRangeMultiplier;
        public float DayLengthSeconds => dayLengthSeconds;
        public float CoinsPerCitizenPerDayAtMaxTax => coinsPerCitizenPerDayAtMaxTax;
        public float DecayPerDayAtLevel1 => decayPerDayAtLevel1;
        public float DecayPenaltyThreshold => decayPenaltyThreshold;
        public float MinDecayProductionMultiplier => minDecayProductionMultiplier;
        public float RepairCostFraction => repairCostFraction;
        public float DemolishRefundFraction => demolishRefundFraction;
        public int WoodPerTree => woodPerTree;
        public int StonePerRock => stonePerRock;
        public int WoodPerHarvest => woodPerHarvest;
        public int StonePerHarvest => stonePerHarvest;

        /// <summary>The stock of a boulder of this size (0 = the smallest model); stone_per_rock past the table.</summary>
        public int StoneForRockSize(int sizeIndex) =>
            stoneRockSizes != null && sizeIndex >= 0 && sizeIndex < stoneRockSizes.Length ? stoneRockSizes[sizeIndex] : stonePerRock;
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
        public int KingHealthPerLevel => kingHealthPerLevel;
        public int KingDamagePerLevel => kingDamagePerLevel;
        public int KingXpPerLevel => kingXpPerLevel;
        public int KingMaxLevel => kingMaxLevel;
        public float KingRespawnBaseSeconds => kingRespawnBaseSeconds;
        public float KingRespawnPerLevelSeconds => kingRespawnPerLevelSeconds;
        public float KingXpRadius => kingXpRadius;
        public float KingGuardRadius => kingGuardRadius;
        public int KingRevealRadius => kingRevealRadius;
        public int InvasionHouses => invasionHouses;
        public int InvasionPeoplePerHouse => invasionPeoplePerHouse;
        public float InvasionPersonRespawnSeconds => invasionPersonRespawnSeconds;
        public int InvasionStartWood => invasionStartWood;
        public int InvasionStartStone => invasionStartStone;
        public int InvasionStartCoins => invasionStartCoins;
        public float InvasionFirstWaveSeconds => invasionFirstWaveSeconds;
        public float InvasionBreatherSeconds => invasionBreatherSeconds;
        public float InvasionWaveOvertimeSeconds => invasionWaveOvertimeSeconds;
        public int InvasionTwoLanesFromWave => invasionTwoLanesFromWave;
        public int InvasionThreeLanesFromWave => invasionThreeLanesFromWave;
        public int InvasionWaveBaseOrcs => invasionWaveBaseOrcs;
        public int InvasionWaveExtraOrcEvery => invasionWaveExtraOrcEvery;
        public int InvasionWaveMaxOrcs => invasionWaveMaxOrcs;
        public int InvasionWaveLevelEvery => invasionWaveLevelEvery;
        public int InvasionWaveMaxOrcLevel => invasionWaveMaxOrcLevel;
        public int InvasionThrowerFromWave => invasionThrowerFromWave;
        public int InvasionThrowerEvery => invasionThrowerEvery;
        public int InvasionBruteFromWave => invasionBruteFromWave;
        public int InvasionBruteEvery => invasionBruteEvery;
        public int InvasionBossWaveEvery => invasionBossWaveEvery;
        public int InvasionGoldOrc => invasionGoldOrc;
        public int InvasionGoldThrower => invasionGoldThrower;
        public int InvasionGoldBrute => invasionGoldBrute;
        public int InvasionGoldChief => invasionGoldChief;
        public int InvasionGoldPerWave => invasionGoldPerWave;
        public float InvasionOrcBuildingRadius => invasionOrcBuildingRadius;
        public int InvasionCamp1Monsters => invasionCamp1Monsters;
        public int InvasionCamp2Monsters => invasionCamp2Monsters;
        public int InvasionCamp3Monsters => invasionCamp3Monsters;
        public float InvasionCampRespawnSeconds => invasionCampRespawnSeconds;
        public float InvasionBossRespawnSeconds => invasionBossRespawnSeconds;
        public float InvasionMonsterGuardRadius => invasionMonsterGuardRadius;
        public float InvasionBossGuardRadius => invasionBossGuardRadius;
        public int InvasionMonsterLevelEveryWaves => invasionMonsterLevelEveryWaves;
        public int InvasionGoldWolf => invasionGoldWolf;
        public int InvasionGoldBear => invasionGoldBear;
        public int InvasionGoldTroll => invasionGoldTroll;
        public float InvasionShrineRadius => invasionShrineRadius;
        public float InvasionShrineHealPerSecond => invasionShrineHealPerSecond;
        public int InvasionBossCoins => invasionBossCoins;
        public int InvasionBossWood => invasionBossWood;
        public int InvasionBossStone => invasionBossStone;
        public float InvasionBossBuffSeconds => invasionBossBuffSeconds;
        public float InvasionBossBuffDamage => invasionBossBuffDamage;
        public float InvasionBossBuffHealth => invasionBossBuffHealth;
        public int InvasionCampBuildingLevel => invasionCampBuildingLevel;
        public int InvasionCampGuardLevel => invasionCampGuardLevel;
        public int InvasionCampRaiders => invasionCampRaiders;
        public int InvasionCampThrowers => invasionCampThrowers;
        public float InvasionCampGuardRadius => invasionCampGuardRadius;
        public float InvasionCampGuardRespawnSeconds => invasionCampGuardRespawnSeconds;
        public float InvasionOrcHealthMultiplier => invasionOrcHealthMultiplier;
        public float InvasionOrcDamageMultiplier => invasionOrcDamageMultiplier;
        public int InvasionMaxEachYard => invasionMaxEachYard;
        public float InvasionMinimapRadius => invasionMinimapRadius;
        public float InvasionWallHealthMultiplier => invasionWallHealthMultiplier;
        public int InvasionCitizenHealth => invasionCitizenHealth;
        public float InvasionBuildSecondsPerUnit => invasionBuildSecondsPerUnit;
        public float InvasionBuildSecondsMin => invasionBuildSecondsMin;
        public float InvasionBuildSecondsMax => invasionBuildSecondsMax;
        public int InvasionScorePerWaveSquared => invasionScorePerWaveSquared;
        public int InvasionScoreBoss => invasionScoreBoss;
        public int InvasionScoreCampBuilding => invasionScoreCampBuilding;
        public int InvasionRunsCountedPerDay => invasionRunsCountedPerDay;

        /// <summary>A jungle monster (wolf, bear, troll, giant) as a unit row, built from its mon_ID_* keys like the King's.</summary>
        public UnitBalance MonsterUnit(string id)
        {
            _monsterUnits ??= new Dictionary<string, UnitBalance>();
            if (_monsterUnits.TryGetValue(id, out var unit)) return unit;

            MonsterStats stats = null;
            foreach (var m in monsters)
            {
                if (m.id == id) stats = m;
            }
            if (stats == null)
            {
                Debug.LogError($"BalanceConfig: no monster '{id}' (economy keys mon_{id}_*). Using fallback stats.");
                stats = new MonsterStats { id = id };
            }
            unit = new UnitBalance
            {
                id = id,
                displayName = id,
                attackRangeUnits = stats.attackRange,
                attackRangeStructures = stats.attackRange + 1.2f,
                engageRadius = invasionMonsterGuardRadius,
                startsUnlocked = false,
                levels = new List<UnitLevelStats>
                {
                    new UnitLevelStats { maxHealth = stats.maxHealth, attackDamage = stats.attackDamage, attackIntervalSeconds = stats.attackIntervalSeconds, walkSpeed = stats.walkSpeed },
                },
            };
            _monsterUnits[id] = unit;
            return unit;
        }

        [NonSerialized] private Dictionary<string, UnitBalance> _monsterUnits;

        /// <summary>
        /// The King as a unit row, built from the economy tab's king_* keys rather than read from
        /// the units tab: he is never recruited, researched or paid, so the units tab's columns
        /// would be blanks around six numbers. His LEVELS are the hero's own (see KingHero), not
        /// the Laboratory's, so the row has the one level.
        /// </summary>
        public UnitBalance KingUnit => _kingUnit ??= new UnitBalance
        {
            id = "king",
            displayName = "King",
            attackRangeUnits = kingAttackRange,
            attackRangeStructures = kingAttackRange + 1.2f,
            engageRadius = kingEngageRadius,
            startsUnlocked = false,
            levels = new List<UnitLevelStats>
            {
                new UnitLevelStats { maxHealth = kingMaxHealth, attackDamage = kingAttackDamage, attackIntervalSeconds = kingAttackIntervalSeconds, walkSpeed = kingWalkSpeed },
            },
        };

        [NonSerialized] private UnitBalance _kingUnit;
        public int StartWood => startWood;
        public int StartStone => startStone;
        public int StartFood => startFood;
        public int StartGold => startGold;

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
            fireChancePerDayAtFullDecay = Read(economy, "fire_chance_per_day_at_full_decay", fireChancePerDayAtFullDecay);
            fireSpreadChancePerDay = Read(economy, "fire_spread_chance_per_day", fireSpreadChancePerDay);
            fireSpreadRadiusMeters = Read(economy, "fire_spread_radius_meters", fireSpreadRadiusMeters);
            fireDamagePerSecond = (int)Read(economy, "fire_damage_per_second", fireDamagePerSecond);
            fireBurnSeconds = Read(economy, "fire_burn_seconds", fireBurnSeconds);
            fireSecondsSavedPerFirefighter = Read(economy, "fire_seconds_saved_per_firefighter", fireSecondsSavedPerFirefighter);
            fireMinBurnSeconds = Read(economy, "fire_min_burn_seconds", fireMinBurnSeconds);
            sicknessBaseChancePerDay = Read(economy, "sickness_base_chance_per_day", sicknessBaseChancePerDay);
            sicknessHungerChancePerDay = Read(economy, "sickness_hunger_chance_per_day", sicknessHungerChancePerDay);
            sicknessThirstChancePerDay = Read(economy, "sickness_thirst_chance_per_day", sicknessThirstChancePerDay);
            sicknessDaysBeforeDeath = (int)Read(economy, "sickness_days_before_death", sicknessDaysBeforeDeath);
            healPerHealerPerDay = (int)Read(economy, "heal_per_healer_per_day", healPerHealerPerDay);
            portalMaxHealth = (int)Read(economy, "portal_max_health", portalMaxHealth);
            orcTowerHealth = (int)Read(economy, "orc_tower_health", orcTowerHealth);
            orcTowerDamage = (int)Read(economy, "orc_tower_damage", orcTowerDamage);
            orcTowerIntervalSeconds = Read(economy, "orc_tower_interval_seconds", orcTowerIntervalSeconds);
            orcTowerRangeMeters = Read(economy, "orc_tower_range_meters", orcTowerRangeMeters);
            orcTentHealth = (int)Read(economy, "orc_tent_health", orcTentHealth);
            orcTotemHealth = (int)Read(economy, "orc_totem_health", orcTotemHealth);
            orcGarrisonSize = (int)Read(economy, "orc_garrison_size", orcGarrisonSize);
            raidThrowerFromLevel = (int)Read(economy, "raid_thrower_from_level", raidThrowerFromLevel);
            raidThrowerEvery = (int)Read(economy, "raid_thrower_every", raidThrowerEvery);
            raidChiefFromLevel = (int)Read(economy, "raid_chief_from_level", raidChiefFromLevel);
            orcGarrisonRespawnSeconds = Read(economy, "orc_garrison_respawn_seconds", orcGarrisonRespawnSeconds);
            orcGarrisonGuardRadiusMeters = Read(economy, "orc_garrison_guard_radius_meters", orcGarrisonGuardRadiusMeters);
            portalHealthByLevel = new[] { portalMaxHealth,
                (int)Read(economy, "portal_max_health_2", 480), (int)Read(economy, "portal_max_health_3", 700) };
            orcTowerHealthByLevel = new[] { orcTowerHealth,
                (int)Read(economy, "orc_tower_health_2", 220), (int)Read(economy, "orc_tower_health_3", 300) };
            orcTowerDamageByLevel = new[] { orcTowerDamage,
                (int)Read(economy, "orc_tower_damage_2", 4), (int)Read(economy, "orc_tower_damage_3", 5) };
            orcTowerRangeByLevel = new[] { orcTowerRangeMeters,
                Read(economy, "orc_tower_range_meters_2", 7.5f), Read(economy, "orc_tower_range_meters_3", 8f) };
            orcGateTowerHealth = (int)Read(economy, "orc_gatetower_health", orcGateTowerHealth);
            orcGateTowerDamage = (int)Read(economy, "orc_gatetower_damage", orcGateTowerDamage);
            orcGateTowerRangeMeters = Read(economy, "orc_gatetower_range_meters", orcGateTowerRangeMeters);
            orcPalisadeHealthByLevel = new[] { (int)Read(economy, "orc_palisade_health", 60),
                (int)Read(economy, "orc_palisade_health_2", 100) };
            orcGateHealthByLevel = new[] { (int)Read(economy, "orc_gate_health", 120),
                (int)Read(economy, "orc_gate_health_2", 180) };
            orcTotemHealthByLevel = new[] { orcTotemHealth, (int)Read(economy, "orc_totem_health_2", 200) };
            orcStorehouseHealthByLevel = new[] { (int)Read(economy, "orc_storehouse_health", 150),
                (int)Read(economy, "orc_storehouse_health_2", 220) };
            orcBrutePitHealth = (int)Read(economy, "orc_brutepit_health", orcBrutePitHealth);
            orcThrowerRangeHealth = (int)Read(economy, "orc_thrower_range_health", orcThrowerRangeHealth);
            orcCampLevel2Score = (int)Read(economy, "orc_camp_level2_score", orcCampLevel2Score);
            orcCampLevel3Score = (int)Read(economy, "orc_camp_level3_score", orcCampLevel3Score);
            orcCampLevelScorePerRank = (int)Read(economy, "orc_camp_level_score_per_rank", orcCampLevelScorePerRank);
            orcCampLootCoins = (int)Read(economy, "orc_camp_loot_coins", orcCampLootCoins);
            orcCampLootWood = (int)Read(economy, "orc_camp_loot_wood", orcCampLootWood);
            orcCampLootStone = (int)Read(economy, "orc_camp_loot_stone", orcCampLootStone);
            orcCampBreatherSeconds = Read(economy, "orc_camp_breather_seconds", orcCampBreatherSeconds);
            mainPortalHealth = (int)Read(economy, "main_portal_health", mainPortalHealth);
            orcMainWallHealth = (int)Read(economy, "orc_main_wall_health", orcMainWallHealth);
            orcMainGateHealth = (int)Read(economy, "orc_main_gate_health", orcMainGateHealth);
            orcMainGateDamage = (int)Read(economy, "orc_main_gate_damage", orcMainGateDamage);
            orcMainGateRangeMeters = Read(economy, "orc_main_gate_range_meters", orcMainGateRangeMeters);
            orcMainTowerHealth = (int)Read(economy, "orc_main_tower_health", orcMainTowerHealth);
            orcMainTowerDamage = (int)Read(economy, "orc_main_tower_damage", orcMainTowerDamage);
            orcMainTowerRangeMeters = Read(economy, "orc_main_tower_range_meters", orcMainTowerRangeMeters);
            orcMainTotemHealth = (int)Read(economy, "orc_main_totem_health", orcMainTotemHealth);
            orcMainStorehouseHealth = (int)Read(economy, "orc_main_storehouse_health", orcMainStorehouseHealth);
            orcMainSpawnerHealth = (int)Read(economy, "orc_main_spawner_health", orcMainSpawnerHealth);
            orcChiefHouseHealth = (int)Read(economy, "orc_chief_house_health", orcChiefHouseHealth);
            orcChiefRespawnSeconds = Read(economy, "orc_chief_respawn_seconds", orcChiefRespawnSeconds);
            orcCageHealthByLevel = new[] { (int)Read(economy, "orc_cage_health", 120),
                (int)Read(economy, "orc_cage_health_2", 180), (int)Read(economy, "orc_cage_health_3", 260) };
            orcMainCageHealth = (int)Read(economy, "orc_main_cage_health", orcMainCageHealth);
            orcCagePrisonersByLevel = new[] { (int)Read(economy, "orc_cage_prisoners", 2),
                (int)Read(economy, "orc_cage_prisoners_2", 3), (int)Read(economy, "orc_cage_prisoners_3", 4) };
            orcMainCagePrisoners = (int)Read(economy, "orc_main_cage_prisoners", orcMainCagePrisoners);
            prisonerHealth = (int)Read(economy, "prisoner_health", prisonerHealth);
            prisonerWalkSpeed = Read(economy, "prisoner_walk_speed", prisonerWalkSpeed);
            orcMainTreasuryHealth = (int)Read(economy, "orc_main_treasury_health", orcMainTreasuryHealth);
            townHallMinDistanceToOrcs = Read(economy, "town_hall_min_distance_to_orcs", townHallMinDistanceToOrcs);
            orcTreasuryLootCoins = (int)Read(economy, "orc_treasury_loot_coins", orcTreasuryLootCoins);
            orcTreasuryLootIronBars = (int)Read(economy, "orc_treasury_loot_ironbar", orcTreasuryLootIronBars);
            orcTreasuryLootCopperBars = (int)Read(economy, "orc_treasury_loot_copperbar", orcTreasuryLootCopperBars);
            portalCount = (int)Read(economy, "portal_count", portalCount);
            portalDistanceFromTownHallMeters = Read(economy, "portal_distance_from_town_hall_meters", portalDistanceFromTownHallMeters);
            portalDistanceStepMeters = Read(economy, "portal_distance_step_meters", portalDistanceStepMeters);
            defenceAttackIntervalSeconds = Read(economy, "defence_attack_interval_seconds", defenceAttackIntervalSeconds);
            defenceAttackRangeMeters = Read(economy, "defence_attack_range_meters", defenceAttackRangeMeters);
            castleAttackRangeMultiplier = Read(economy, "castle_attack_range_mult", castleAttackRangeMultiplier);
            dayLengthSeconds = Read(economy, "day_length_seconds", dayLengthSeconds);
            coinsPerCitizenPerDayAtMaxTax = Read(economy, "coins_per_citizen_per_day_at_max_tax", coinsPerCitizenPerDayAtMaxTax);
            decayPerDayAtLevel1 = Read(economy, "decay_per_day_at_level1", decayPerDayAtLevel1);
            decayPenaltyThreshold = Read(economy, "decay_penalty_threshold", decayPenaltyThreshold);
            minDecayProductionMultiplier = Read(economy, "min_decay_production_multiplier", minDecayProductionMultiplier);
            repairCostFraction = Read(economy, "repair_cost_fraction", repairCostFraction);
            demolishRefundFraction = Read(economy, "demolish_refund_fraction", demolishRefundFraction);
            woodPerTree = (int)Read(economy, "wood_per_tree", woodPerTree);
            stonePerRock = (int)Read(economy, "stone_per_rock", stonePerRock);
            woodPerHarvest = (int)Read(economy, "wood_per_harvest", woodPerHarvest);
            stonePerHarvest = (int)Read(economy, "stone_per_harvest", stonePerHarvest);
            for (var i = 0; i < stoneRockSizes.Length; i++)
            {
                stoneRockSizes[i] = (int)Read(economy, $"stone_rock_size_{i + 1}", stoneRockSizes[i]);
            }
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
            kingMaxHealth = (int)Read(economy, "king_max_health", kingMaxHealth);
            kingAttackDamage = (int)Read(economy, "king_attack_damage", kingAttackDamage);
            kingAttackIntervalSeconds = Read(economy, "king_attack_interval_sec", kingAttackIntervalSeconds);
            kingAttackRange = Read(economy, "king_attack_range", kingAttackRange);
            kingWalkSpeed = Read(economy, "king_walk_speed", kingWalkSpeed);
            kingEngageRadius = Read(economy, "king_engage_radius", kingEngageRadius);
            kingHealthPerLevel = (int)Read(economy, "king_health_per_level", kingHealthPerLevel);
            kingDamagePerLevel = (int)Read(economy, "king_damage_per_level", kingDamagePerLevel);
            kingXpPerLevel = (int)Read(economy, "king_xp_per_level", kingXpPerLevel);
            kingMaxLevel = (int)Read(economy, "king_max_level", kingMaxLevel);
            kingRespawnBaseSeconds = Read(economy, "king_respawn_base_sec", kingRespawnBaseSeconds);
            kingRespawnPerLevelSeconds = Read(economy, "king_respawn_per_level_sec", kingRespawnPerLevelSeconds);
            kingXpRadius = Read(economy, "king_xp_radius", kingXpRadius);
            kingGuardRadius = Read(economy, "king_guard_radius", kingGuardRadius);
            kingRevealRadius = (int)Read(economy, "king_reveal_radius", kingRevealRadius);
            _kingUnit = null;
            invasionHouses = (int)Read(economy, "inv_houses", invasionHouses);
            invasionPeoplePerHouse = (int)Read(economy, "inv_people_per_house", invasionPeoplePerHouse);
            invasionPersonRespawnSeconds = Read(economy, "inv_person_respawn_sec", invasionPersonRespawnSeconds);
            invasionStartWood = (int)Read(economy, "inv_start_wood", invasionStartWood);
            invasionStartStone = (int)Read(economy, "inv_start_stone", invasionStartStone);
            invasionStartCoins = (int)Read(economy, "inv_start_coins", invasionStartCoins);
            invasionFirstWaveSeconds = Read(economy, "inv_first_wave_sec", invasionFirstWaveSeconds);
            invasionBreatherSeconds = Read(economy, "inv_breather_sec", invasionBreatherSeconds);
            invasionWaveOvertimeSeconds = Read(economy, "inv_wave_overtime_sec", invasionWaveOvertimeSeconds);
            invasionTwoLanesFromWave = (int)Read(economy, "inv_two_lanes_from_wave", invasionTwoLanesFromWave);
            invasionThreeLanesFromWave = (int)Read(economy, "inv_three_lanes_from_wave", invasionThreeLanesFromWave);
            invasionWaveBaseOrcs = (int)Read(economy, "inv_wave_base_orcs", invasionWaveBaseOrcs);
            invasionWaveExtraOrcEvery = (int)Read(economy, "inv_wave_extra_orc_every", invasionWaveExtraOrcEvery);
            invasionWaveMaxOrcs = (int)Read(economy, "inv_wave_max_orcs", invasionWaveMaxOrcs);
            invasionWaveLevelEvery = (int)Read(economy, "inv_wave_level_every", invasionWaveLevelEvery);
            invasionWaveMaxOrcLevel = (int)Read(economy, "inv_wave_max_orc_level", invasionWaveMaxOrcLevel);
            invasionThrowerFromWave = (int)Read(economy, "inv_thrower_from_wave", invasionThrowerFromWave);
            invasionThrowerEvery = (int)Read(economy, "inv_thrower_every", invasionThrowerEvery);
            invasionBruteFromWave = (int)Read(economy, "inv_brute_from_wave", invasionBruteFromWave);
            invasionBruteEvery = (int)Read(economy, "inv_brute_every", invasionBruteEvery);
            invasionBossWaveEvery = (int)Read(economy, "inv_boss_wave_every", invasionBossWaveEvery);
            invasionGoldOrc = (int)Read(economy, "inv_gold_orc", invasionGoldOrc);
            invasionGoldThrower = (int)Read(economy, "inv_gold_thrower", invasionGoldThrower);
            invasionGoldBrute = (int)Read(economy, "inv_gold_brute", invasionGoldBrute);
            invasionGoldChief = (int)Read(economy, "inv_gold_chief", invasionGoldChief);
            invasionGoldPerWave = (int)Read(economy, "inv_gold_per_wave", invasionGoldPerWave);
            invasionOrcBuildingRadius = Read(economy, "inv_orc_building_radius", invasionOrcBuildingRadius);
            foreach (var m in monsters)
            {
                m.maxHealth = (int)Read(economy, $"mon_{m.id}_health", m.maxHealth);
                m.attackDamage = (int)Read(economy, $"mon_{m.id}_damage", m.attackDamage);
                m.attackIntervalSeconds = Read(economy, $"mon_{m.id}_attack_interval_sec", m.attackIntervalSeconds);
                m.attackRange = Read(economy, $"mon_{m.id}_attack_range", m.attackRange);
                m.walkSpeed = Read(economy, $"mon_{m.id}_walk_speed", m.walkSpeed);
            }
            _monsterUnits = null;
            invasionCamp1Monsters = (int)Read(economy, "inv_camp1_monsters", invasionCamp1Monsters);
            invasionCamp2Monsters = (int)Read(economy, "inv_camp2_monsters", invasionCamp2Monsters);
            invasionCamp3Monsters = (int)Read(economy, "inv_camp3_monsters", invasionCamp3Monsters);
            invasionCampRespawnSeconds = Read(economy, "inv_camp_respawn_sec", invasionCampRespawnSeconds);
            invasionBossRespawnSeconds = Read(economy, "inv_boss_respawn_sec", invasionBossRespawnSeconds);
            invasionMonsterGuardRadius = Read(economy, "inv_monster_guard_radius", invasionMonsterGuardRadius);
            invasionBossGuardRadius = Read(economy, "inv_boss_guard_radius", invasionBossGuardRadius);
            invasionMonsterLevelEveryWaves = (int)Read(economy, "inv_monster_level_every_waves", invasionMonsterLevelEveryWaves);
            invasionGoldWolf = (int)Read(economy, "inv_gold_wolf", invasionGoldWolf);
            invasionGoldBear = (int)Read(economy, "inv_gold_bear", invasionGoldBear);
            invasionGoldTroll = (int)Read(economy, "inv_gold_troll", invasionGoldTroll);
            invasionShrineRadius = Read(economy, "inv_shrine_radius", invasionShrineRadius);
            invasionShrineHealPerSecond = Read(economy, "inv_shrine_heal_per_sec", invasionShrineHealPerSecond);
            invasionBossCoins = (int)Read(economy, "inv_boss_coins", invasionBossCoins);
            invasionBossWood = (int)Read(economy, "inv_boss_wood", invasionBossWood);
            invasionBossStone = (int)Read(economy, "inv_boss_stone", invasionBossStone);
            invasionBossBuffSeconds = Read(economy, "inv_boss_buff_sec", invasionBossBuffSeconds);
            invasionBossBuffDamage = Read(economy, "inv_boss_buff_damage", invasionBossBuffDamage);
            invasionBossBuffHealth = Read(economy, "inv_boss_buff_health", invasionBossBuffHealth);
            invasionCampBuildingLevel = (int)Read(economy, "inv_camp_building_level", invasionCampBuildingLevel);
            invasionCampGuardLevel = (int)Read(economy, "inv_camp_guard_level", invasionCampGuardLevel);
            invasionCampRaiders = (int)Read(economy, "inv_camp_raiders", invasionCampRaiders);
            invasionCampThrowers = (int)Read(economy, "inv_camp_throwers", invasionCampThrowers);
            invasionCampGuardRadius = Read(economy, "inv_camp_guard_radius", invasionCampGuardRadius);
            invasionCampGuardRespawnSeconds = Read(economy, "inv_camp_guard_respawn_sec", invasionCampGuardRespawnSeconds);
            invasionOrcHealthMultiplier = Read(economy, "inv_orc_health_mult", invasionOrcHealthMultiplier);
            invasionOrcDamageMultiplier = Read(economy, "inv_orc_damage_mult", invasionOrcDamageMultiplier);
            invasionMaxEachYard = (int)Read(economy, "inv_max_each_yard", invasionMaxEachYard);
            invasionMinimapRadius = Read(economy, "inv_minimap_radius", invasionMinimapRadius);
            invasionWallHealthMultiplier = Read(economy, "inv_wall_health_mult", invasionWallHealthMultiplier);
            invasionCitizenHealth = (int)Read(economy, "inv_citizen_health", invasionCitizenHealth);
            invasionBuildSecondsPerUnit = Read(economy, "inv_build_sec_per_unit", invasionBuildSecondsPerUnit);
            invasionBuildSecondsMin = Read(economy, "inv_build_sec_min", invasionBuildSecondsMin);
            invasionBuildSecondsMax = Read(economy, "inv_build_sec_max", invasionBuildSecondsMax);
            invasionScorePerWaveSquared = (int)Read(economy, "inv_score_wave_sq", invasionScorePerWaveSquared);
            invasionScoreBoss = (int)Read(economy, "inv_score_boss", invasionScoreBoss);
            invasionScoreCampBuilding = (int)Read(economy, "inv_score_camp_building", invasionScoreCampBuilding);
            invasionRunsCountedPerDay = (int)Read(economy, "inv_runs_per_day", invasionRunsCountedPerDay);
            startWood = (int)Read(economy, "start_wood", startWood);
            startStone = (int)Read(economy, "start_stone", startStone);
            startFood = (int)Read(economy, "start_food", startFood);
            startGold = (int)Read(economy, "start_gold", startGold);
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
