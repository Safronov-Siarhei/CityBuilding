using System;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Core
{
    /// <summary>
    /// One hero skill (the user, 2026-10-23), from the economy tab's skill_&lt;id&gt;_* rows: the hero
    /// level that opens it, its cooldown, and its two magnitudes -- `power` (a share of health, a
    /// multiplier) and `duration` -- each growing by its *_per_level with every hero level past the
    /// one that opened it. Which of the two a skill uses is the skill's own (HeroSkills).
    /// </summary>
    /// <summary>
    /// One hero of the Invasion mode (the user, 2026-10-23: the King, the Queen, the Captain of the
    /// guard), from the economy tab's &lt;id&gt;_* keys: the level-1 fighter, what each hero level adds,
    /// and, for the two bought in the castle, the price and the castle level that allows it. The
    /// Queen's flee_* keys say when she backs off from a melee enemy and how far.
    /// </summary>
    [Serializable]
    public class HeroBalance
    {
        public string id;
        public int maxHealth;
        public int attackDamage;
        public float attackIntervalSeconds;
        public float attackRange;
        public float walkSpeed;
        public float engageRadius;
        public int healthPerLevel;
        public int damagePerLevel;
        public int price;
        public int castleLevel = 1;
        public float fleeRadius;
        public float fleeDistance;
    }

    [Serializable]
    public class HeroSkillBalance
    {
        public string id;
        public int unlockLevel = 1;
        public float cooldownSeconds;
        public float durationSeconds;
        public float durationPerLevel;
        public float power;
        public float powerPerLevel;

        /// <summary>Optional: the reach of a skill that has one (skill_&lt;id&gt;_radius), metres.</summary>
        public float radius;

        /// <summary>Optional: a level the skill works at (skill_&lt;id&gt;_tier) -- the Queen's summoned soldiers'.</summary>
        public int tier;

        public float PowerAt(int heroLevel) => power + powerPerLevel * Mathf.Max(0, heroLevel - unlockLevel);
        public float DurationAt(int heroLevel) => durationSeconds + durationPerLevel * Mathf.Max(0, heroLevel - unlockLevel);
    }

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
        /// The Barracks' level that hires this tier (the user, 2026-10-28: «каждый уровень открывал
        /// нового солдата»): units tab `barracks_level` -- militia 1, spearman 2, archer 3,
        /// man-at-arms 4. Both modes; in the sandbox the Laboratory still has to open the tier too.
        /// </summary>
        public int barracksLevel = 1;

        /// <summary>
        /// The roster widening (the user, 2026-11-05): the building that hires this type -- units tab
        /// `hired_at`, a building id: the Barracks (default), the Stable (the knight), the Church (the
        /// monk). `barracks_level` is then that building's level.
        /// </summary>
        public string hiredAt = "Barracks";

        /// <summary>Places in the army cap one of these takes (`army_slots`; the knight 2, a soldier 1).</summary>
        public int armySlots = 1;

        /// <summary>How many of this type the army may hold per level of the hiring building (`cap_per_level`; the knight 4, the monk 2; 0 = no cap of its own).</summary>
        public int capPerLevel;

        /// <summary>Wears armour (`armoured`): a crossbow bolt does `vs_armoured` times its damage to it.</summary>
        public bool armoured;

        /// <summary>A horseman (`cavalry`): a spear does `vs_cavalry` times its damage to it; walks no walls.</summary>
        public bool cavalry;

        /// <summary>Damage multipliers of this type's blows against armoured / cavalry targets (`vs_armoured`, `vs_cavalry`; 1 = none).</summary>
        public float vsArmoured = 1f;
        public float vsCavalry = 1f;

        /// <summary>The knight's charge: the first blow after a run of `charge_run` cells does `charge_mult` times its damage (0 run = no charge).</summary>
        public float chargeMultiplier = 1f;
        public float chargeRun;

        /// <summary>
        /// A healer (`healer`, the monk): never strikes; every attack interval he gives his attack
        /// damage back as health to the most wounded soldier within his unit reach.
        /// </summary>
        public bool healer;

        /// <summary>
        /// The Workshop's machines (the user, 2026-11-05): `crew` citizens it takes to hire (2; a
        /// soldier 1), `vs_buildings` its damage on a structure (catapult 2, ballista 0.3),
        /// `gates_only` (the ram: it strikes nothing but gates), `splash` the catapult's stone's
        /// radius, `min_range` its dead zone, `ranged_taken` the share of an arrow's or a thrown
        /// spear's damage it takes (ram 0.25, tower 0.5), `carrier` (the siege tower: no blows).
        /// </summary>
        public int crew = 1;
        public float vsBuildings = 1f;
        public bool gatesOnly;
        public float splash;
        public float minRange;
        public float rangedTaken = 1f;
        public bool carrier;

        /// <summary>A ship's row (ship_*): how many it carries, the Shipyard level that builds it, how many may sail (0 = no limit).</summary>
        public int capacity;

        /// <summary>
        /// A ship's places at levels 2..5 (units `capacity_2`..`capacity_5`, the user 2026-11-05: transport
        /// 8-10-12-14-16, battleship 12-14-16-18-20; levels from the Laboratory). Index 0 = level 2; an
        /// empty cell repeats the level below.
        /// </summary>
        public int[] capacityUp = new int[4];

        /// <summary>Places at this level (1..5).</summary>
        public int CapacityAt(int level)
        {
            var places = capacity;
            for (var l = 2; l <= level && l - 2 < capacityUp.Length; l++)
                if (capacityUp[l - 2] > 0) places = capacityUp[l - 2];
            return places;
        }

        /// <summary>The ship has levels to research: a later level carries more.</summary>
        public bool HasShipLevels
        {
            get
            {
                foreach (var places in capacityUp) if (places > 0) return true;
                return false;
            }
        }
        public int shipyardLevel = 1;
        public int maxCount;

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

        /// <summary>Wood a machine is built of, on top of its coins (units `recruit_wood`; the ram 20, the catapult 30, the siege tower 60).</summary>
        public int recruitWood;

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

        /// <summary>How many of this an Invasion run may have (the invasion tab's max_count), 0 for no limit.</summary>
        public int maxCount;

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

        /// <summary>The Invasion run's config (the sheet's invasion tab laid over the others, BalanceImporter), loaded from here.</summary>
        public const string RunResourcePath = "BalanceConfigInvasion";

        private static BalanceConfig _instance;
        private static BalanceConfig _runInstance;

        [Header("Units (units.csv)")]
        [SerializeField] private List<UnitBalance> units = new List<UnitBalance>();

        [Header("Buildings (buildings.csv)")]
        [SerializeField] private List<BuildingBalance> buildings = new List<BuildingBalance>();

        [Header("Army (economy.csv)")]
        [SerializeField] private int armyMaxSize = 20;
        // army control by squads (the user, 2026-11-05): how many squads, how far from the Town Hall the Garrison defends
        [SerializeField] private int armyMaxSquads = 5;
        [SerializeField] private float garrisonDefendCells = 30f;
        // squads' behaviours and formations (the user, 2026-11-05)
        [SerializeField] private float squadFormationSpacing = 0.9f;
        [SerializeField] private float squadScatterSpacing = 1.8f;
        [SerializeField] private float squadGuardPatrolCells = 10f;
        [SerializeField] private float squadGuardZoneCells = 22f;
        [SerializeField] private float squadFollowCells = 2f;
        // ship levels (the user, 2026-11-05): the Laboratory's price per level, the places a machine takes
        [SerializeField] private int[] shipLevelCoins = { 150, 300, 500, 800 };
        [SerializeField] private float[] shipLevelSeconds = { 60f, 120f, 180f, 240f };
        [SerializeField] private int shipMachinePlaces = 4;

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
        // coins a citizen pays a day (the user, 2026-11-02: the tax slider is gone, every citizen
        // pays this), and what each level of the Tax House adds to it
        [SerializeField] private float coinsPerCitizenPerDay = 1f;
        [SerializeField] private float taxHouseBonusPerLevel = 0.2f;
        // the Tax House's research in the Laboratory: its opening and levels 2..5 cost these upgrade
        // crystals (tax_house_research_crystals_<1..5>; the user, 2026-11-02)
        [SerializeField] private int[] taxHouseResearchCrystals = { 3, 5, 7, 9, 11 };
        // the walk along the walls (the user, 2026-11-02): from which level a Fence/Gate/Tower takes
        // part, the walk's height by level 3..5 (cells), the range a fighter up there gains (cells),
        // how far an idle ranged fighter looks for orcs and a free wall cell, the climb's seconds
        [SerializeField] private int wallWalkFromLevel = 3;
        [SerializeField] private float[] wallWalkHeight = { 0.9f, 1.1f, 1.3f };
        [SerializeField] private float wallRangeBonusCells = 2f;
        [SerializeField] private float wallSeekRadiusCells = 14f;
        [SerializeField] private float wallClimbSeconds = 1.2f;
        [SerializeField] private float wallJumpHealthShare = 0.3f;
        [SerializeField] private float siegeGangwayHeight = 0.6f;
        // the Moat (the user, 2026-11-02): the crystals its research costs, the speed in its water,
        // and how dear its cells are to a route (NavMesh area cost; dry land is 1)
        [SerializeField] private int moatResearchCrystals = 10;
        [SerializeField] private float moatSpeedMultiplier = 0.4f;
        [SerializeField] private float moatWadeDepth = 0.25f;
        [SerializeField] private float bridgeDeckHeight = 0.22f;
        [SerializeField] private float botRelationsStart = 0f;
        [SerializeField] private float botTradeMinScore = 30f;
        [SerializeField] private float botWarDeclareScoreLoss = 40f;
        [SerializeField] private float botRelationsDriftPerDay = 2f;
        [SerializeField] private float botRebuildCalmSeconds = 60f;
        [SerializeField] private int botRebuildEveryRaids = 3;
        [SerializeField] private float botCalmRadiusCells = 45f;
        [SerializeField] private float botGarrisonRespawnSeconds = 90f;
        [SerializeField] private int botGarrisonPatrols = 4;
        [SerializeField] private int botGarrisonPatrolSize = 3;
        [SerializeField] private int botGarrisonArchers = 16;
        [SerializeField] private int botGarrisonLevel = 5;
        [SerializeField] private float botRaidFirstSeconds = 300f;
        [SerializeField] private float botRaidIntervalSeconds = 600f;
        [SerializeField] private int botRaidSize = 30;
        [SerializeField] private int botRaidLevel = 3;
        // the war with the bot (Bot/BotWar, the user 2026-11-04)
        [SerializeField] private float botWarRaidFirstSeconds = 180.0f;
        [SerializeField] private float botWarRaidIntervalSeconds = 300.0f;
        [SerializeField] private int botWarLevel = 2;
        [SerializeField] private int botWarEscorts = 2;
        [SerializeField] private float botWarGuardRadiusCells = 6.0f;
        [SerializeField] private int botWarContributionBase = 200;
        [SerializeField] private int botWarContributionPerWave = 100;
        [SerializeField] private float botWarContributionHallDiscount = 0.1f;
        [SerializeField] private int botWarCoinsPerCrystal = 100;
        [SerializeField] private int botWarContributionPerBuilding = 5;
        [SerializeField] private int botWarContributionMin = 50;
        [SerializeField] private int botWarStartWaveStep = 2;
        [SerializeField] private float moatPathCost = 6f;
        // the Drawbridge's leaves: rise while an orc is within this many cells, to this angle, in this time
        [SerializeField] private float drawbridgeRaiseRadiusCells = 6f;
        [SerializeField] private float drawbridgeRaiseDegrees = 75f;
        [SerializeField] private float drawbridgeRaiseSeconds = 1.5f;
        // boulders laid on the archipelago's start island on top of its share (island_start_rocks)
        [SerializeField] private int islandStartRocks = 40;
        // crystals for finishing the tutorial, shown on its menu tag (tutorial_reward_crystals)
        [SerializeField] private int tutorialRewardCrystals = 25;
        // the sandbox offer shop («Наборы: настройки»): a slot's real seconds, the slots' stagger, the
        // free bonus's chance per change and its earliest second, the loud notice's seconds, the
        // starter gift's second in the first game, the ad stand-in's seconds
        [SerializeField] private float shopSlotSeconds = 180f;
        [SerializeField] private float shopSlotStaggerSeconds = 60f;
        [SerializeField] private float shopBonusChance = 0.15f;
        [SerializeField] private float shopBonusMinSeconds = 300f;
        [SerializeField] private float shopNoticeSeconds = 30f;
        [SerializeField] private float shopStarterDelaySeconds = 120f;
        [SerializeField] private float shopAdStubSeconds = 2f;
        // a bundle's timer turns red and pulses this many seconds before it changes (shop_timer_warn_seconds)
        [SerializeField] private float shopTimerWarnSeconds = 30f;
        // the bases a crystal offer's bonus (+%) is counted from: a crystal's dollars, an ad's crystals
        [SerializeField] private float shopCrystalUsd = 0.01f;
        [SerializeField] private float shopAdCrystalsBase = 10f;
        // a goods bundle worth more ads than this is sold for crystals only (the user, 2026-11-11: no bundles for nothing)
        [SerializeField] private float shopAdMaxAds = 2f;
        // the loading screen's ship: seconds it sails into a game and out to the menu (loading_seconds_enter / _exit)
        [SerializeField] private float loadingSecondsEnter = 10f;
        [SerializeField] private float loadingSecondsExit = 5f;
        // the tutorial's rules (tutorial_build_sec, tutorial_start_people, tutorial_king_heal_below)
        [SerializeField] private float tutorialBuildSeconds = 5f;
        [SerializeField] private int tutorialStartPeople = 3;
        [SerializeField] private float tutorialKingHealBelow = 0.25f;
        // the tutorial's orcs: their blows and the tower's shots times this; the barge's orcs
        [SerializeField] private float tutorialOrcDamageMult = 0.2f;
        [SerializeField] private int tutorialRaidOrcs = 5;
        [SerializeField] private float tutorialPortalSeconds = 7f;
        [SerializeField] private float tutorialBirthSeconds = 7f;
        [SerializeField] private int tutorialOrcGuards = 3;
        // the tutorial's own tab (the user, 2026-11-05): orcs' health times this; the chop/dig;
        // the camera's distance on a person (share of its distance); fish shoals kept; the
        // «people came» line; one blink of a highlight
        [SerializeField] private float tutorialOrcHealthMult = 3f;
        [SerializeField] private float tutorialGatherSeconds = 5f;
        [SerializeField] private float tutorialPersonZoom = 0.5f;
        [SerializeField] private int tutorialFishShoals = 2;
        [SerializeField] private float tutorialPeopleHintSeconds = 6f;
        [SerializeField] private float tutorialBlinkSeconds = 0.5f;
        [SerializeField] private float tutorialRaidCameraSeconds = 3f;
        [SerializeField] private float victoryDelaySeconds = 3f;
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
        [SerializeField] private float starveLeaveIntervalSeconds = 20f;
        [SerializeField] private float eventCaptionSeconds = 4f;
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
        [SerializeField] private float shipRepairWoodPerHp = 0.5f;
        [SerializeField] private float shipRepairReachMeters = 14f;
        [SerializeField] private float shipIdleReturnSeconds = 30f;
        [SerializeField] private float shipHomeReachCells = 12f;
        [SerializeField] private int orcShipyardHealth = 900;
        [SerializeField] private int orcShipyardDamage = 10;
        [SerializeField] private float orcShipyardRangeMeters = 12f;
        [SerializeField] private float orcShipyardRebuildSeconds = 300f;
        [SerializeField] private int orcFleetEscorts = 1;
        [SerializeField] private float forestCellsPerTree = 6f;
        [SerializeField] private int archipelagoTreesMin = 600;
        [SerializeField] private int archipelagoTreesMax = 3000;
        [SerializeField] private float stonyCellsPerRock = 5f;
        [SerializeField] private int archipelagoRocksMin = 250;
        [SerializeField] private int archipelagoRocksMax = 900;
        [SerializeField] private float rocksScatteredShare = 0.3f;
        [SerializeField] private float sceneryStreamSquare = 24f;
        [SerializeField] private int dungeonEntranceCount = 3;
        [SerializeField] private float dungeonEnterReach = 4f;
        [SerializeField] private float dungeonWadeSpeedMultiplier = 0.7f;
        [SerializeField] private int dungeonRevealCells = 7;
        [SerializeField] private float dungeonCameraDistance = 12f;
        [SerializeField] private float dungeonCameraPitch = 50f;
        [SerializeField] private float dungeonGuardRadius = 4f;
        [SerializeField] private float dungeonBossGuardRadius = 7f;
        [SerializeField] private int dungeonMonsterLevel1 = 1;
        [SerializeField] private int dungeonMonsterLevel2 = 2;
        [SerializeField] private int dungeonMonsterLevel3 = 3;
        [SerializeField] private float dungeonCoinsPerHealth = 0.1f;
        [SerializeField] private float dungeonXpShare = 0.25f;
        [SerializeField] private int dungeonBossCoins = 300;
        [SerializeField] private int dungeonBossCrystals = 5;
        [SerializeField] private int dungeonBossWood = 150;
        [SerializeField] private int dungeonBossStone = 150;
        [SerializeField] private int dungeonBossGold = 50;
        [SerializeField] private float dungeonEngageFactor = 0.6f;
        [SerializeField] private float dungeonRangedRangeFactor = 0.5f;
        [SerializeField] private int lairRats = 8;
        [SerializeField] private int dungeonBossThemeLairs = 2;
        [SerializeField] private float dungeonMonsterWakeMargin = 6f;
        [SerializeField] private int dungeonCrystalsPerRoom = 2;
        [SerializeField] private int dungeonCrystalsLone = 1;
        [SerializeField] private float dungeonCrystalPickReach = 1f;
        [SerializeField] private float dungeonCrystalFloat = 0.45f;
        [SerializeField] private float dungeonLairPackShare = 0.75f;
        [SerializeField] private float dungeonGuardRoomShare = 0.5f;
        [SerializeField] private float dungeonGuardSingleShare = 0.35f;
        [SerializeField] private float dungeonGuardBossReach = 30f;
        [SerializeField] private int dungeonPatrolSize = 3;
        [SerializeField] private float dungeonPatrolPauseSeconds = 2f;
        [SerializeField] private int lairBats = 5;
        [SerializeField] private int lairSpiders = 3;
        [SerializeField] private int lairSpiderlings = 4;
        [SerializeField] private int lairSkeletons = 3;
        [SerializeField] private int lairKobolds = 3;
        [SerializeField] private int lairStoneGolems = 2;
        [SerializeField] private float spiderPoisonSeconds = 3f;
        [SerializeField] private float spiderPoisonKeep = 0.5f;
        [SerializeField] private float skeletonGetUpSeconds = 2.5f;
        [SerializeField] private float skeletonGetUpHealth = 0.5f;
        [SerializeField] private float queenWebEvery = 9f;
        [SerializeField] private float queenWebStun = 2f;
        [SerializeField] private float queenWebRange = 6f;
        [SerializeField] private float queenBroodEvery = 15f;
        [SerializeField] private int queenBroodCount = 2;
        [SerializeField] private float queenCocoonSeconds = 7f;
        [SerializeField] private int queenCocoonBrood = 4;
        [SerializeField] private float queenCocoonHeal = 0.015f;
        [SerializeField] private float golemSlamEvery = 8f;
        [SerializeField] private float golemSlamRadius = 2.2f;
        [SerializeField] private float golemSlamDamage = 1.5f;
        [SerializeField] private float golemBoulderEvery = 6f;
        [SerializeField] private float golemBoulderRange = 9f;
        [SerializeField] private float golemArmour = 0.4f;
        // the level-2 dungeon (Затопленные гномьи чертоги): its lairs and its monsters' powers
        [SerializeField] private int lair2DrownedRats = 8;
        [SerializeField] private int lair2DwarfSkeletons = 3;
        [SerializeField] private int lair2ForgeSpirits = 3;
        [SerializeField] private int lair2DrownedDwarfs = 4;
        [SerializeField] private int lair2Eels = 3;
        [SerializeField] private int lair2StoneGuards = 2;
        [SerializeField] private int dungeon2LoneEels = 12;
        [SerializeField] private float dungeon2EelSpacing = 10f;
        [SerializeField] private float dwarfSkeletonArmour = 0.4f;
        [SerializeField] private float ambushReach = 3f;
        [SerializeField] private float ambushRiseSeconds = 1.2f;
        [SerializeField] private float kingSlamEvery = 8f;
        [SerializeField] private float kingSlamRadius = 2.4f;
        [SerializeField] private float kingSlamDamage = 1.5f;
        [SerializeField] private float kingGuardsEvery = 20f;
        [SerializeField] private int kingGuardsCount = 2;
        [SerializeField] private int kingThroneGuards = 3;
        [SerializeField] private float kingThroneSeconds = 10f;
        [SerializeField] private float kingThroneHeal = 0.012f;
        [SerializeField] private float serpentDiveEvery = 10f;
        [SerializeField] private float serpentDiveSeconds = 3f;
        [SerializeField] private float serpentSurfaceReach = 6f;
        [SerializeField] private float serpentSpitStun = 0.8f;
        [SerializeField] private int dungeon2CrystalsPerRoom = 1;
        [SerializeField] private float dungeonChestReach = 1f;
        // Dungeon 3 (the user, 2026-11-04): a hero standing in lava, or by a fire vent, burns
        [SerializeField] private float dungeonLavaDamagePerSecond = 25f;
        // Dungeon 3, «Огненные глубины»: lairs, crystals, the bosses' powers, the tide, the bridges
        [SerializeField] private int lair3FireImps = 5;
        [SerializeField] private int lair3LavaHounds = 3;
        [SerializeField] private int lair3Salamanders = 3;
        [SerializeField] private int lair3EmberCultists = 2;
        [SerializeField] private int lair3ObsidianGolems = 2;
        [SerializeField] private int lair3MagmaSpawn = 3;
        [SerializeField] private int dungeon3CrystalsPerRoom = 4;
        [SerializeField] private float dragonBreathEvery = 6.0f;
        [SerializeField] private float dragonBreathRange = 4.5f;
        [SerializeField] private float dragonBreathAngle = 60.0f;
        [SerializeField] private float dragonBreathDamage = 1.4f;
        [SerializeField] private float dragonFlightEvery = 14.0f;
        [SerializeField] private float dragonFlightSeconds = 3.0f;
        [SerializeField] private float dragonFlightReach = 7.0f;
        [SerializeField] private float dragonLandingRadius = 2.2f;
        [SerializeField] private float dragonLandingDamage = 1.0f;
        [SerializeField] private float titanRiseReach = 6.0f;
        [SerializeField] private float titanRiseSeconds = 2.5f;
        [SerializeField] private float titanShedEvery = 0.2f;
        [SerializeField] private int titanShedCount = 3;
        [SerializeField] private float tideLowSeconds = 25.0f;
        [SerializeField] private float tideWarnSeconds = 4.0f;
        [SerializeField] private float tideHighSeconds = 12.0f;
        [SerializeField] private float bridgeFallDelay = 1.5f;
        [SerializeField] private float bridgeReformSeconds = 40.0f;
        [SerializeField] private int dungeonChestWood = 20;
        [SerializeField] private int dungeonChestStone = 20;
        [SerializeField] private int dungeonChestGold = 6;
        [SerializeField] private int dungeonChestCoins = 25;
        [SerializeField] private int fertilePatchesPerIsland = 2;
        [SerializeField] private int fertileMinCoastCells = 8;
        [SerializeField] private float fishShoalShoreMeters = 4f;
        [SerializeField] private float fishShoalMinSpacing = 14f;
        [SerializeField] private int dungeonHirelingMax = 5;
        [SerializeField] private float dungeonHirelingFollowCells = 2.5f;
        [SerializeField] private float dungeonHirelingLeashCells = 9f;
        // the dungeon's potions, by Dungeon.PotionKind (healing, rage, speed, defence)
        [SerializeField] private int[] potionCoins = { 30, 40, 25, 35 };
        [SerializeField] private float[] potionSeconds = { 10f, 20f, 30f, 20f };
        [SerializeField] private float[] potionPower = { 0.6f, 1.5f, 1.4f, 0.5f };
        private static readonly string[] PotionIds = { "healing", "rage", "speed", "defence" };
        [SerializeField] private float fertilePatchRadius = 5f;
        [SerializeField] private float fertileFarmBonus = 0.5f;
        [SerializeField] private int fishShoalCount = 12;
        [SerializeField] private int fishShoalStock = 60;
        [SerializeField] private float fishShoalBonus = 1f;
        [SerializeField] private float fishHutReachMeters = 16f;
        [SerializeField] private float fishShoalRespawnSeconds = 180f;
        [SerializeField] private float lighthouseRangeMeters = 80f;
        [SerializeField] private int lighthouseRevealCells = 18;
        [SerializeField] private int lighthouseRevealCoins = 30;
        [SerializeField] private float lighthouseCooldownSeconds = 90f;
        [SerializeField] private int lighthouseCoastCells = 2;
        // the lighthouse's own light by level (cells): a medium island at 1, a large one at 5
        [SerializeField] private int[] lighthouseLightCells = { 45, 52, 60, 70, 80 };
        // the archipelago's islands each have their riches (the user, 2026-11-02): veins per island by its kind
        [SerializeField] private int islandStartCoal = 2;
        [SerializeField] private int islandStartCopper = 1;
        [SerializeField] private int islandIronVeins = 4;
        [SerializeField] private int islandIronCoal = 1;
        [SerializeField] private int islandCopperVeins = 4;
        [SerializeField] private int islandGoldVeins = 3;
        [SerializeField] private int islandStoneCoal = 2;
        [SerializeField] private int islandVeinShoreGap = 4;
        [SerializeField] private int startScoutGraceCells = 50;
        [SerializeField] private int islandFertilePatches = 3;
        // Стражи Глубин's caches (Maps/Archipelago/TideCaches): guards per tier (rows) and kind
        // (crab, shark, puffer, shaman, serpent), the reward per tier (wood, stone, gold, crystals)
        [SerializeField] private int[] tideGuards = { 2, 0, 0, 0, 0, 2, 1, 1, 0, 0, 2, 2, 1, 1, 0, 0, 2, 0, 1, 1 };
        [SerializeField] private int[] tideRewards = { 60, 40, 15, 1, 120, 80, 30, 2, 200, 140, 60, 3, 400, 300, 150, 6 };
        [SerializeField] private float tideGuardRadius = 6f;
        // the food rework (the user, 2026-11-02): Food a day by building and level 1..5, the
        // Bakery's/Smokehouse's bonus to each producer in reach by its level, and where they stand
        // (food_day_<id>_<level>, food_boost_<id>_<level>, food_fertile_bonus, fisher_shore_cells,
        // food_inland_cells)
        [SerializeField] private int[] foodDayFisherHut = { 3, 4, 5, 6, 7 };
        [SerializeField] private int[] foodDayFarm = { 3, 4, 5, 6, 7 };
        [SerializeField] private int[] foodDayOrchard = { 5, 10, 15, 20, 25 };
        [SerializeField] private int[] foodDayPigFarm = { 5, 10, 15, 20, 25 };
        [SerializeField] private int[] foodBoostBaker = { 1, 2, 3, 4, 5 };
        [SerializeField] private int[] foodBoostSmokehouse = { 1, 2, 3, 4, 5 };
        [SerializeField] private int foodFertileBonus = 1;
        [SerializeField] private int fisherShoreCells = 2;
        [SerializeField] private int foodInlandCells = 2;
        [SerializeField] private float tideWakeRadius = 14f;
        [SerializeField] private int tideShamanHeal = 12;
        [SerializeField] private float tideShamanHealEverySeconds = 4f;
        [SerializeField] private float tideShamanHealRadius = 5f;
        [SerializeField] private float kingXpRadius = 12f;
        [SerializeField] private float kingGuardRadius = 30f;
        [SerializeField] private int kingRevealRadius = 9;
        [SerializeField] private float heroReturnWaitSeconds = 10f;
        [SerializeField] private float heroPatrolRadiusCells = 1.5f;
        [SerializeField] private float heroPatrolPauseSeconds = 5f;
        [SerializeField] private float ruinRestoreCostFraction = 0.5f;
        [SerializeField] private float ruinRestoreTimeFraction = 0.5f;
        [SerializeField] private List<HeroSkillBalance> heroSkills = new List<HeroSkillBalance>();
        [SerializeField] private List<HeroBalance> heroes = new List<HeroBalance>();

        /// <summary>The heroes, in the order their portraits stand: the King first, then those bought in the castle.</summary>
        public static readonly string[] HeroIds = { "king", "queen", "captain" };

        // The Invasion run's own economy (InvasionRules): the houses and their people, the opening stock.
        [SerializeField] private int invasionHouses = 6;
        [SerializeField] private float personRespawnSeconds = 15f;
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
        [SerializeField] private float invasionLaneDetourMeters = 6f;
        [SerializeField] private int invasionRaidPartyEvery = 3;
        [SerializeField] private int invasionRaidPartyRaiders = 5;
        [SerializeField] private int invasionRaidPartyThrowers = 2;
        [SerializeField] private float invasionRaidPartyBaseRadius = 22f;
        [SerializeField] private float invasionHealerHealShare = 0.01f;
        [SerializeField] private float invasionHealerHealSharePerLevel = 0.005f;
        // the New Year map's rules (InvasionNewYear; the user, 2026-10-25)
        [SerializeField] private int nyGiftsPerWave = 3;
        [SerializeField] private int nyGiftsMax = 8;
        [SerializeField] private float nyGiftMinRadius = 8f;
        [SerializeField] private float nyGiftMaxRadius = 32f;
        [SerializeField] private float nyGiftPickupMeters = 1.2f;
        [SerializeField] private int nyGiftWood = 15;
        [SerializeField] private int nyGiftStone = 10;
        [SerializeField] private int nyGiftGold = 20;
        [SerializeField] private int nyThiefFromWave = 2;
        [SerializeField] private int nyThievesPerWave = 1;
        [SerializeField] private int nyGoldThief = 5;
        [SerializeField] private float nyBlizzardChance = 0.35f;
        [SerializeField] private float nyBlizzardSeconds = 35f;
        [SerializeField] private float nyBlizzardWarningSeconds = 8f;
        [SerializeField] private float nyBlizzardRangeMult = 0.6f;
        [SerializeField] private float nyBlizzardSpeedMult = 0.8f;
        [SerializeField] private int nyChimesEvery = 12;
        [SerializeField] private float nyChimesOrcsMult = 1.5f;
        [SerializeField] private float nySaluteDelaySeconds = 8f;
        [SerializeField] private float nySaluteDamageShare = 0.3f;
        [SerializeField] private float nyShamanFreezeEverySeconds = 8f;
        [SerializeField] private float nyShamanFreezeRadius = 10f;
        [SerializeField] private float nyShamanFreezeSeconds = 4f;
        [SerializeField] private int nyGoldShaman = 60;

        // The Invasion run's jungle (InvasionJungle): its monsters, camps, shrines and boss.
        /// <summary>Every monster the economy tab has mon_ID_* keys for: the jungle's and the dungeon's.</summary>
        private static readonly string[] MonsterIds =
        {
            "wolf", "bear", "troll", "giant", "rat", "bat", "spider", "spiderling", "skeleton", "kobold", "stone_golem", "spider_queen", "ore_golem",
            "tide_crab", "tide_shark", "tide_shaman", "tide_puffer", "tide_serpent",
            "drowned_rat", "dwarf_skeleton", "stone_guard", "drowned_dwarf", "eel", "forge_spirit", "dwarf_king", "canal_serpent",
            "fire_imp", "lava_hound", "salamander", "obsidian_golem", "ember_cultist", "magma_spawn", "dragon", "lava_titan",
        };

        [SerializeField] private List<MonsterStats> monsters = new List<MonsterStats>
        {
            new MonsterStats { id = "wolf", maxHealth = 30, attackDamage = 3, attackIntervalSeconds = 1f, attackRange = 1.1f, walkSpeed = 2f },
            new MonsterStats { id = "bear", maxHealth = 90, attackDamage = 8, attackIntervalSeconds = 1.4f, attackRange = 1.3f, walkSpeed = 1.5f },
            new MonsterStats { id = "troll", maxHealth = 200, attackDamage = 14, attackIntervalSeconds = 1.5f, attackRange = 1.5f, walkSpeed = 1.3f },
            new MonsterStats { id = "giant", maxHealth = 900, attackDamage = 30, attackIntervalSeconds = 2f, attackRange = 2f, walkSpeed = 1.2f },
            new MonsterStats { id = "rat", maxHealth = 14, attackDamage = 2, attackIntervalSeconds = 0.8f, attackRange = 0.7f, walkSpeed = 2.2f },
            new MonsterStats { id = "bat", maxHealth = 18, attackDamage = 3, attackIntervalSeconds = 1.0f, attackRange = 0.7f, walkSpeed = 2.4f },
            new MonsterStats { id = "spider", maxHealth = 45, attackDamage = 5, attackIntervalSeconds = 1.1f, attackRange = 0.8f, walkSpeed = 2.0f },
            new MonsterStats { id = "spiderling", maxHealth = 14, attackDamage = 2, attackIntervalSeconds = 0.9f, attackRange = 0.6f, walkSpeed = 2.2f },
            new MonsterStats { id = "skeleton", maxHealth = 70, attackDamage = 7, attackIntervalSeconds = 1.4f, attackRange = 0.8f, walkSpeed = 1.2f },
            new MonsterStats { id = "kobold", maxHealth = 35, attackDamage = 6, attackIntervalSeconds = 2.0f, attackRange = 4.0f, walkSpeed = 1.5f },
            new MonsterStats { id = "stone_golem", maxHealth = 180, attackDamage = 12, attackIntervalSeconds = 1.8f, attackRange = 1.0f, walkSpeed = 0.9f },
            new MonsterStats { id = "spider_queen", maxHealth = 1200, attackDamage = 22, attackIntervalSeconds = 1.6f, attackRange = 1.6f, walkSpeed = 1.4f },
            new MonsterStats { id = "ore_golem", maxHealth = 1600, attackDamage = 30, attackIntervalSeconds = 2.2f, attackRange = 1.8f, walkSpeed = 1.0f },
            // Стражи Глубин (the archipelago's caches)
            new MonsterStats { id = "tide_crab", maxHealth = 80, attackDamage = 5, attackIntervalSeconds = 1.4f, attackRange = 0.9f, walkSpeed = 1.2f },
            new MonsterStats { id = "tide_shark", maxHealth = 60, attackDamage = 9, attackIntervalSeconds = 1.0f, attackRange = 0.9f, walkSpeed = 2.2f },
            new MonsterStats { id = "tide_shaman", maxHealth = 50, attackDamage = 5, attackIntervalSeconds = 2.0f, attackRange = 4.5f, walkSpeed = 1.3f },
            new MonsterStats { id = "tide_puffer", maxHealth = 40, attackDamage = 6, attackIntervalSeconds = 2.0f, attackRange = 4.0f, walkSpeed = 1.4f },
            new MonsterStats { id = "tide_serpent", maxHealth = 1400, attackDamage = 26, attackIntervalSeconds = 2.0f, attackRange = 2.2f, walkSpeed = 1.0f },
            // the level-2 dungeon's (monsters2.py)
            new MonsterStats { id = "drowned_rat", maxHealth = 20, attackDamage = 3, attackIntervalSeconds = 0.8f, attackRange = 0.7f, walkSpeed = 2.2f },
            new MonsterStats { id = "dwarf_skeleton", maxHealth = 90, attackDamage = 8, attackIntervalSeconds = 1.4f, attackRange = 0.8f, walkSpeed = 1.1f },
            new MonsterStats { id = "stone_guard", maxHealth = 220, attackDamage = 13, attackIntervalSeconds = 1.8f, attackRange = 1f, walkSpeed = 0.9f },
            new MonsterStats { id = "drowned_dwarf", maxHealth = 60, attackDamage = 7, attackIntervalSeconds = 1.3f, attackRange = 0.8f, walkSpeed = 1.3f },
            new MonsterStats { id = "eel", maxHealth = 40, attackDamage = 7, attackIntervalSeconds = 1.2f, attackRange = 1.2f, walkSpeed = 1.5f },
            new MonsterStats { id = "forge_spirit", maxHealth = 40, attackDamage = 7, attackIntervalSeconds = 2.2f, attackRange = 4f, walkSpeed = 1.3f },
            new MonsterStats { id = "dwarf_king", maxHealth = 2000, attackDamage = 32, attackIntervalSeconds = 2f, attackRange = 1.8f, walkSpeed = 1f },
            new MonsterStats { id = "canal_serpent", maxHealth = 1800, attackDamage = 28, attackIntervalSeconds = 2.4f, attackRange = 5f, walkSpeed = 0.6f },
            // the level-3 dungeon's (monsters3.py)
            new MonsterStats { id = "fire_imp", maxHealth = 35, attackDamage = 5, attackIntervalSeconds = 0.8f, attackRange = 0.7f, walkSpeed = 2.4f },
            new MonsterStats { id = "lava_hound", maxHealth = 110, attackDamage = 10, attackIntervalSeconds = 1.1f, attackRange = 0.9f, walkSpeed = 2.3f },
            new MonsterStats { id = "salamander", maxHealth = 70, attackDamage = 9, attackIntervalSeconds = 2.2f, attackRange = 4.5f, walkSpeed = 1.3f },
            new MonsterStats { id = "obsidian_golem", maxHealth = 380, attackDamage = 18, attackIntervalSeconds = 1.9f, attackRange = 1.1f, walkSpeed = 0.85f },
            new MonsterStats { id = "ember_cultist", maxHealth = 65, attackDamage = 11, attackIntervalSeconds = 2.4f, attackRange = 5.0f, walkSpeed = 1.2f },
            new MonsterStats { id = "magma_spawn", maxHealth = 45, attackDamage = 6, attackIntervalSeconds = 1.0f, attackRange = 0.8f, walkSpeed = 1.6f },
            new MonsterStats { id = "dragon", maxHealth = 3200, attackDamage = 45, attackIntervalSeconds = 2.2f, attackRange = 2.6f, walkSpeed = 1.2f },
            new MonsterStats { id = "lava_titan", maxHealth = 3000, attackDamage = 42, attackIntervalSeconds = 2.4f, attackRange = 2.2f, walkSpeed = 0.8f },
            // the bot kingdom's soldiers at war (Bot/BotWar)
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
        // A run's Barracks soldiers guard the base on their own (InvasionGarrison).
        [SerializeField] private float invasionSoldierGuardRadius = 30f;
        // How close to a building's wall an orc that fights hand to hand must stand to hit it (OrcUnit.CanStrike).
        [SerializeField] private float orcMeleeReachMeters = 0.8f;
        // The Invasion run's score (InvasionScore) and the local records (InvasionRecords).
        [SerializeField] private float invasionOrcHealthMultiplier = 4f;
        [SerializeField] private float invasionOrcDamageMultiplier = 1f;
        [SerializeField] private float invasionStoneReserveMultiplier = 3f;
        [SerializeField] private int invasionWoodPerHarvest = 1;
        [SerializeField] private int invasionStonePerHarvest = 1;
        [SerializeField] private float minimapRadius = 40f;
        [SerializeField] private int invasionCitizenHealth = 12;
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
        [SerializeField] private int researchCrystals = 1;
        [SerializeField] private int researchCrystalsFromLevel = 3;

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
        public int ArmyMaxSquads => armyMaxSquads;
        public float GarrisonDefendCells => garrisonDefendCells;
        public float SquadFormationSpacing => squadFormationSpacing;
        public float SquadScatterSpacing => squadScatterSpacing;
        public float SquadGuardPatrolCells => squadGuardPatrolCells;
        public float SquadGuardZoneCells => squadGuardZoneCells;
        public float SquadFollowCells => squadFollowCells;
        /// <summary>Coins to research ship level 2..5 (economy `ship_level_coins_N`).</summary>
        public int ShipLevelCoins(int level) => level >= 2 && level - 2 < shipLevelCoins.Length ? shipLevelCoins[level - 2] : 0;
        public float ShipLevelSeconds(int level) => level >= 2 && level - 2 < shipLevelSeconds.Length ? shipLevelSeconds[level - 2] : 60f;
        public int ShipMachinePlaces => shipMachinePlaces;
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
        public float CoinsPerCitizenPerDay => coinsPerCitizenPerDay;
        public float TaxHouseBonusPerLevel => taxHouseBonusPerLevel;
        public int IslandStartRocks => islandStartRocks;
        public int TutorialRewardCrystals => tutorialRewardCrystals;
        public float ShopSlotSeconds => shopSlotSeconds;
        public float ShopSlotStaggerSeconds => shopSlotStaggerSeconds;
        public float ShopBonusChance => shopBonusChance;
        public float ShopBonusMinSeconds => shopBonusMinSeconds;
        public float ShopNoticeSeconds => shopNoticeSeconds;
        public float ShopStarterDelaySeconds => shopStarterDelaySeconds;
        public float ShopAdStubSeconds => shopAdStubSeconds;
        public float ShopTimerWarnSeconds => shopTimerWarnSeconds;
        public float ShopCrystalUsd => shopCrystalUsd;
        public float ShopAdCrystalsBase => shopAdCrystalsBase;
        public float ShopAdMaxAds => shopAdMaxAds;
        public float LoadingSecondsEnter => loadingSecondsEnter;
        public float LoadingSecondsExit => loadingSecondsExit;
        public float TutorialBuildSeconds => tutorialBuildSeconds;
        public int TutorialStartPeople => tutorialStartPeople;
        public float TutorialKingHealBelow => tutorialKingHealBelow;
        public float TutorialOrcDamageMult => tutorialOrcDamageMult;
        public int TutorialRaidOrcs => tutorialRaidOrcs;

        /// <summary>The tutorial's portal falls to the King alone in about this many seconds (economy/tutorial `tutorial_portal_sec`; the user, 2026-11-06: ~7).</summary>
        public float TutorialPortalSeconds => tutorialPortalSeconds;

        /// <summary>The tutorial's Hovel brings a person in this many seconds (`tutorial_birth_sec`; the user, 2026-11-06: 7).</summary>
        public float TutorialBirthSeconds => tutorialBirthSeconds;

        /// <summary>Orcs on guard on the tutorial's orc island (`tutorial_orc_guards`; the user, 2026-11-06: at least 3).</summary>
        public int TutorialOrcGuards => tutorialOrcGuards;
        public float TutorialOrcHealthMult => tutorialOrcHealthMult;
        public float TutorialGatherSeconds => tutorialGatherSeconds;
        public float TutorialPersonZoom => tutorialPersonZoom;
        public int TutorialFishShoals => tutorialFishShoals;
        public float TutorialPeopleHintSeconds => tutorialPeopleHintSeconds;
        public float TutorialBlinkSeconds => tutorialBlinkSeconds;

        /// <summary>The tutorial's camera goes to the orcs this long after the soldier is hired (`tutorial_raid_camera_sec`; the user, 2026-11-06: 3).</summary>
        public float TutorialRaidCameraSeconds => tutorialRaidCameraSeconds;

        /// <summary>Seconds from the last portal's fall to the victory panel (economy `victory_delay_sec`; the user, 2026-11-06: 3).</summary>
        public float VictoryDelaySeconds => victoryDelaySeconds;
        public int WallWalkFromLevel => wallWalkFromLevel;
        public float WallRangeBonusCells => wallRangeBonusCells;
        public float WallSeekRadiusCells => wallSeekRadiusCells;
        public float WallClimbSeconds => wallClimbSeconds;

        /// <summary>The share of his health a fighter loses jumping off a wall with no stair (economy `wall_jump_health_share`; the user, 2026-11-05: 30%).</summary>
        public float WallJumpHealthShare => wallJumpHealthShare;

        /// <summary>How high a docked siege tower's gangway runs over a low fence (economy `siege_gangway_height`, world units above the ground).</summary>
        public float SiegeGangwayHeight => siegeGangwayHeight;
        public int MoatResearchCrystals => moatResearchCrystals;
        public float MoatSpeedMultiplier => moatSpeedMultiplier;
        /// <summary>How deep (world units) a figure wading the moat sinks: at least to the knee (the user, 2026-11-03).</summary>
        public float MoatWadeDepth => moatWadeDepth;
        /// <summary>How high (world units) a bridge's or drawbridge's deck stands: the figures crossing walk on it.</summary>
        public float BridgeDeckHeight => bridgeDeckHeight;
        /// <summary>The bot kingdom's relations scale (-100..100, the user 2026-11-03): where a new game starts it.</summary>
        public float BotRelationsStart => botRelationsStart;
        /// <summary>The relations Trade needs.</summary>
        public float BotTradeMinScore => botTradeMinScore;
        /// <summary>What declaring war costs on the scale.</summary>
        public float BotWarDeclareScoreLoss => botWarDeclareScoreLoss;
        /// <summary>How far the scale creeps back towards 0 in a day.</summary>
        public float BotRelationsDriftPerDay => botRelationsDriftPerDay;
        /// <summary>How long the bot's castle must be clear of orcs before it rebuilds what a raid destroyed (the user, 2026-11-03).</summary>
        public float BotRebuildCalmSeconds => botRebuildCalmSeconds;
        /// <summary>The bot raises its ruins after every this many orc raids on it (the user, 2026-11-06: every third).</summary>
        public int BotRebuildEveryRaids => Mathf.Max(1, botRebuildEveryRaids);
        /// <summary>How far round the bot's castle an orc still counts as its raid.</summary>
        public float BotCalmRadiusCells => botCalmRadiusCells;
        /// <summary>The bot's garrison (the user, 2026-11-03): how long a fallen soldier takes to come back while its Town Hall stands.</summary>
        public float BotGarrisonRespawnSeconds => botGarrisonRespawnSeconds;
        /// <summary>Patrols on the avenues (up to 4), and men in each.</summary>
        public int BotGarrisonPatrols => botGarrisonPatrols;
        public int BotGarrisonPatrolSize => botGarrisonPatrolSize;
        /// <summary>Archers on the walls.</summary>
        public int BotGarrisonArchers => botGarrisonArchers;
        /// <summary>The level whose stats the garrison fights with.</summary>
        public int BotGarrisonLevel => botGarrisonLevel;
        /// <summary>The orcs' raids on the bot's castle (the user, 2026-11-03: massive, for show): the first after this long, then every interval; how many orcs, of which level (0 orcs: none).</summary>
        public float BotRaidFirstSeconds => botRaidFirstSeconds;
        public float BotRaidIntervalSeconds => botRaidIntervalSeconds;
        public int BotRaidSize => botRaidSize;
        public int BotRaidLevel => botRaidLevel;
        public float BotWarRaidFirstSeconds => botWarRaidFirstSeconds;
        public float BotWarRaidIntervalSeconds => botWarRaidIntervalSeconds;
        public int BotWarLevel => botWarLevel;

        /// <summary>One row of the bot's raid waves (the botwaves tab): `count` units of `unit` (an OrcKind sheet id) at `level` in wave `wave`.</summary>
        [System.Serializable]
        public class BotWaveRow
        {
            public int wave;
            public string unit;
            public int count;
            public int level;
        }

        [SerializeField] private List<BotWaveRow> botWaves = new List<BotWaveRow>();

        /// <summary>The waves' rows as the sheet has them.</summary>
        public IReadOnlyList<BotWaveRow> BotWaves => botWaves;

        /// <summary>
        /// The rows of the raid number `wave` (1 = the first); past the last wave in the table, the
        /// last one again (the user, 2026-11-04). Empty when the table is.
        /// </summary>
        public List<BotWaveRow> BotWave(int wave)
        {
            var last = 0;
            foreach (var row in botWaves) last = Mathf.Max(last, row.wave);
            var pick = Mathf.Min(Mathf.Max(1, wave), last);
            var rows = new List<BotWaveRow>();
            foreach (var row in botWaves)
                if (row.wave == pick && row.count > 0) rows.Add(row);
            return rows;
        }

#if UNITY_EDITOR
        /// <summary>The importer's way in (BalanceImporter, the botwaves tab).</summary>
        public void EditorSetBotWaves(List<BotWaveRow> rows) => botWaves = rows ?? new List<BotWaveRow>();
#endif

        /// <summary>One good inside an offer bundle: a ResourceType name or `Crystals`, and how many.</summary>
        [System.Serializable]
        public class OfferGood
        {
            public string resource;
            public int amount;
        }

        /// <summary>
        /// One bundle of the «Наборы» tab on one level (the user, 2026-11-11): `kind` is `normal`
        /// (resources for crystals or an ad), `bonus` (the free «Подарок короля»), `starter` (the first
        /// game's free gift, once per account), `ad_crystals` (crystals for an ad) or `paid_crystals`
        /// (crystals for money). Offered to a player whose level is in levelFrom..levelTo.
        /// </summary>
        [System.Serializable]
        public class OfferBundle
        {
            public string id;
            /// <summary>The localization key of its name (#bundle_&lt;the sheet row's id&gt;, the same on every level).</summary>
            public string nameKey;
            public string kind;
            public float weight;
            public int levelFrom;
            public int levelTo;
            public List<OfferGood> goods = new List<OfferGood>();
            public int priceCrystals;
            /// <summary>paid_crystals only: the price in US dollars, the base tier (the game will show Google Play's price).</summary>
            public float priceUsd;
            /// <summary>What it is worth in ads (the sheet's lN_ads formula from «Цены ресурсов»); over shop_ad_max_ads -> crystals only.</summary>
            public float ads;
        }

        /// <summary>One row of «Наборы: уровни»: the player is at `level` once the lab and the citizens reach these.</summary>
        [System.Serializable]
        public class OfferLevel
        {
            public int level;
            public int labFrom;
            public int citizensFrom;
        }

        [SerializeField] private List<OfferBundle> offerBundles = new List<OfferBundle>();
        [SerializeField] private List<OfferLevel> offerLevels = new List<OfferLevel>();
        public IReadOnlyList<OfferBundle> OfferBundles => offerBundles;
        public IReadOnlyList<OfferLevel> OfferLevels => offerLevels;

#if UNITY_EDITOR
        /// <summary>The importer's way in (BalanceImporter, the «Наборы» tabs).</summary>
        public void EditorSetOffers(List<OfferBundle> bundles, List<OfferLevel> levels)
        {
            offerBundles = bundles ?? new List<OfferBundle>();
            offerLevels = levels ?? new List<OfferLevel>();
        }
#endif
        public int BotWarEscorts => botWarEscorts;
        public float BotWarGuardRadiusCells => botWarGuardRadiusCells;
        public int BotWarContributionBase => botWarContributionBase;
        public int BotWarContributionPerWave => botWarContributionPerWave;
        public float BotWarContributionHallDiscount => botWarContributionHallDiscount;
        public int BotWarCoinsPerCrystal => botWarCoinsPerCrystal;
        public int BotWarContributionPerBuilding => botWarContributionPerBuilding;
        public int BotWarContributionMin => botWarContributionMin;
        public int BotWarStartWaveStep => botWarStartWaveStep;
        public float MoatPathCost => moatPathCost;
        public float DrawbridgeRaiseRadiusCells => drawbridgeRaiseRadiusCells;
        public float DrawbridgeRaiseDegrees => drawbridgeRaiseDegrees;
        public float DrawbridgeRaiseSeconds => drawbridgeRaiseSeconds;

        /// <summary>The walk's height above the ground on a wall of `level` (3..5), in cells.</summary>
        public float WallWalkHeight(int level) =>
            wallWalkHeight == null || wallWalkHeight.Length == 0 ? 1f : wallWalkHeight[Mathf.Clamp(level - 3, 0, wallWalkHeight.Length - 1)];

        /// <summary>The crystals the Tax House's research costs: its opening (level 0 or 1) and levels 2..5.</summary>
        public int TaxHouseResearchCrystals(int level)
        {
            var i = Mathf.Max(1, level) - 1;
            return taxHouseResearchCrystals == null || i >= taxHouseResearchCrystals.Length ? 0 : taxHouseResearchCrystals[i];
        }
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
        /// <summary>Past the hungry grace with no food: one person leaves every this many seconds (the user, 2026-11-10).</summary>
        public float StarveLeaveIntervalSeconds => Mathf.Max(1f, starveLeaveIntervalSeconds);
        /// <summary>A tapped event icon shows its line this many seconds (the user, 2026-11-10).</summary>
        public float EventCaptionSeconds => Mathf.Max(0.5f, eventCaptionSeconds);
        public int FoodVarietyTarget => foodVarietyTarget;
        public int DeathsMemoryDays => deathsMemoryDays;
        public int HappinessPenaltyPerDeath => happinessPenaltyPerDeath;
        public float HappinessPerCitizenTarget => happinessPerCitizenTarget;
        public float ResearchSecondsPerWorker => researchSecondsPerWorker;
        public int ResearchFreeWorkers => researchFreeWorkers;
        public int ResearchCancelRefundPercent => researchCancelRefundPercent;
        public float ResearchMinSeconds => researchMinSeconds;

        /// <summary>Upgrade crystals a research costs from `research_crystals_from_level` up (the user, 2026-10-28: levels 3-5, one crystal each).</summary>
        public int ResearchCrystals => researchCrystals;
        public int ResearchCrystalsFromLevel => researchCrystalsFromLevel;
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
        /// <summary>Wood a ship's repair costs per lost health point (the user: repair only for resources).</summary>
        public float ShipRepairWoodPerHp => shipRepairWoodPerHp;
        /// <summary>How near a working Shipyard a ship must be to be repaired, metres.</summary>
        public float ShipRepairReachMeters => shipRepairReachMeters;
        public float ShipIdleReturnSeconds => shipIdleReturnSeconds;
        public float ShipHomeReachCells => shipHomeReachCells;
        /// <summary>The orcs' shipyard on the archipelago (phase 4b): its health, its gun, and how long the orcs take to raise it again once broken.</summary>
        public int OrcShipyardHealth => orcShipyardHealth;
        public int OrcShipyardDamage => orcShipyardDamage;
        public float OrcShipyardRangeMeters => orcShipyardRangeMeters;
        public float OrcShipyardRebuildSeconds => orcShipyardRebuildSeconds;
        /// <summary>War ships that sail with every orc fleet to guard its transports.</summary>
        public int OrcFleetEscorts => orcFleetEscorts;
        /// <summary>The archipelago's forest and boulders: cells per tree/boulder and the bounds a phone carries; the share of boulders scattered over open land.</summary>
        public float ForestCellsPerTree => forestCellsPerTree;
        public int ArchipelagoTreesMin => archipelagoTreesMin;
        public int ArchipelagoTreesMax => archipelagoTreesMax;
        public float StonyCellsPerRock => stonyCellsPerRock;
        public int ArchipelagoRocksMin => archipelagoRocksMin;
        public int ArchipelagoRocksMax => archipelagoRocksMax;
        public float RocksScatteredShare => rocksScatteredShare;
        /// <summary>Trees and boulders are drawn by squares of this side (cells), only near the camera (SceneryStreamer).</summary>
        public float SceneryStreamSquare => sceneryStreamSquare;
        /// <summary>Dungeon entrances on an archipelago, and how near a hero must be to go in (cells).</summary>
        public int DungeonEntranceCount => dungeonEntranceCount;
        public float DungeonEnterReach => dungeonEnterReach;
        /// <summary>A dungeon's shallow water: how much slower everyone wades it (the user, 2026-11-04).</summary>
        public float DungeonWadeSpeedMultiplier => dungeonWadeSpeedMultiplier;
        public int DungeonRevealCells => dungeonRevealCells;
        public float DungeonCameraDistance => dungeonCameraDistance;
        public float DungeonCameraPitch => dungeonCameraPitch;
        /// <summary>The dungeon (Dungeon/DungeonMonsters, DungeonMonsterPowers): lairs, levels, rewards and the monsters' and bosses' powers.</summary>
        public float DungeonGuardRadius => dungeonGuardRadius;
        public float DungeonBossGuardRadius => dungeonBossGuardRadius;
        public int DungeonMonsterLevel1 => dungeonMonsterLevel1;
        public int DungeonMonsterLevel2 => dungeonMonsterLevel2;
        public int DungeonMonsterLevel3 => dungeonMonsterLevel3;
        public float DungeonCoinsPerHealth => dungeonCoinsPerHealth;
        public float DungeonXpShare => dungeonXpShare;
        public int DungeonBossCoins => dungeonBossCoins;
        public int DungeonBossCrystals => dungeonBossCrystals;
        /// <summary>The resources a boss pays (times the dungeon's level), into the town on the way out.</summary>
        public int DungeonBossWood => dungeonBossWood;
        public int DungeonBossStone => dungeonBossStone;
        public int DungeonBossGold => dungeonBossGold;
        /// <summary>Below, how far off a hero or a hireling picks a fight by himself, as a share of his own.</summary>
        public float DungeonEngageFactor => dungeonEngageFactor;
        /// <summary>Below, a shooter's reach as a share of his own.</summary>
        public float DungeonRangedRangeFactor => dungeonRangedRangeFactor;
        public int LairRats => lairRats;
        public int DungeonBossThemeLairs => dungeonBossThemeLairs;
        public float DungeonMonsterWakeMargin => dungeonMonsterWakeMargin;
        public int DungeonCrystalsPerRoom => dungeonCrystalsPerRoom;
        public int DungeonCrystalsLone => dungeonCrystalsLone;
        public float DungeonCrystalPickReach => dungeonCrystalPickReach;
        public float DungeonCrystalFloat => dungeonCrystalFloat;
        public float DungeonLairPackShare => dungeonLairPackShare;
        public float DungeonGuardRoomShare => dungeonGuardRoomShare;
        public float DungeonGuardSingleShare => dungeonGuardSingleShare;
        public float DungeonGuardBossReach => dungeonGuardBossReach;
        public int DungeonPatrolSize => dungeonPatrolSize;
        public float DungeonPatrolPauseSeconds => dungeonPatrolPauseSeconds;
        public int LairBats => lairBats;
        public int LairSpiders => lairSpiders;
        public int LairSpiderlings => lairSpiderlings;
        public int LairSkeletons => lairSkeletons;
        public int LairKobolds => lairKobolds;
        public int LairStoneGolems => lairStoneGolems;
        public float SpiderPoisonSeconds => spiderPoisonSeconds;
        public float SpiderPoisonKeep => spiderPoisonKeep;
        public float SkeletonGetUpSeconds => skeletonGetUpSeconds;
        public float SkeletonGetUpHealth => skeletonGetUpHealth;
        public float QueenWebEvery => queenWebEvery;
        public float QueenWebStun => queenWebStun;
        public float QueenWebRange => queenWebRange;
        public float QueenBroodEvery => queenBroodEvery;
        public int QueenBroodCount => queenBroodCount;
        public float QueenCocoonSeconds => queenCocoonSeconds;
        public int QueenCocoonBrood => queenCocoonBrood;
        public float QueenCocoonHeal => queenCocoonHeal;
        public float GolemSlamEvery => golemSlamEvery;
        public float GolemSlamRadius => golemSlamRadius;
        public float GolemSlamDamage => golemSlamDamage;
        public float GolemBoulderEvery => golemBoulderEvery;
        public float GolemBoulderRange => golemBoulderRange;
        public float GolemArmour => golemArmour;
        public int Lair2DrownedRats => lair2DrownedRats;
        public int Lair2DwarfSkeletons => lair2DwarfSkeletons;
        public int Lair2ForgeSpirits => lair2ForgeSpirits;
        public int Lair2DrownedDwarfs => lair2DrownedDwarfs;
        public int Lair2Eels => lair2Eels;
        public int Lair2StoneGuards => lair2StoneGuards;
        public int Dungeon2LoneEels => dungeon2LoneEels;
        public float Dungeon2EelSpacing => dungeon2EelSpacing;
        public float DwarfSkeletonArmour => dwarfSkeletonArmour;
        public float AmbushReach => ambushReach;
        public float AmbushRiseSeconds => ambushRiseSeconds;
        public float KingSlamEvery => kingSlamEvery;
        public float KingSlamRadius => kingSlamRadius;
        public float KingSlamDamage => kingSlamDamage;
        public float KingGuardsEvery => kingGuardsEvery;
        public int KingGuardsCount => kingGuardsCount;
        public int KingThroneGuards => kingThroneGuards;
        public float KingThroneSeconds => kingThroneSeconds;
        public float KingThroneHeal => kingThroneHeal;
        public float SerpentDiveEvery => serpentDiveEvery;
        public float SerpentDiveSeconds => serpentDiveSeconds;
        public float SerpentSurfaceReach => serpentSurfaceReach;
        public float SerpentSpitStun => serpentSpitStun;
        public int Dungeon2CrystalsPerRoom => dungeon2CrystalsPerRoom;
        public float DungeonChestReach => dungeonChestReach;
        public float DungeonLavaDamagePerSecond => dungeonLavaDamagePerSecond;
        public int Lair3FireImps => lair3FireImps;
        public int Lair3LavaHounds => lair3LavaHounds;
        public int Lair3Salamanders => lair3Salamanders;
        public int Lair3EmberCultists => lair3EmberCultists;
        public int Lair3ObsidianGolems => lair3ObsidianGolems;
        public int Lair3MagmaSpawn => lair3MagmaSpawn;
        public int Dungeon3CrystalsPerRoom => dungeon3CrystalsPerRoom;
        public float DragonBreathEvery => dragonBreathEvery;
        public float DragonBreathRange => dragonBreathRange;
        public float DragonBreathAngle => dragonBreathAngle;
        public float DragonBreathDamage => dragonBreathDamage;
        public float DragonFlightEvery => dragonFlightEvery;
        public float DragonFlightSeconds => dragonFlightSeconds;
        public float DragonFlightReach => dragonFlightReach;
        public float DragonLandingRadius => dragonLandingRadius;
        public float DragonLandingDamage => dragonLandingDamage;
        public float TitanRiseReach => titanRiseReach;
        public float TitanRiseSeconds => titanRiseSeconds;
        public float TitanShedEvery => titanShedEvery;
        public int TitanShedCount => titanShedCount;
        public float TideLowSeconds => tideLowSeconds;
        public float TideWarnSeconds => tideWarnSeconds;
        public float TideHighSeconds => tideHighSeconds;
        public float BridgeFallDelay => bridgeFallDelay;
        public float BridgeReformSeconds => bridgeReformSeconds;
        public int DungeonChestWood => dungeonChestWood;
        public int DungeonChestStone => dungeonChestStone;
        public int DungeonChestGold => dungeonChestGold;
        public int DungeonChestCoins => dungeonChestCoins;
        /// <summary>The archipelago's fertile soil: patches per island, their radius in cells, and a Farm's extra yield on it (0.5 = +50% with its whole plot fertile).</summary>
        public int FertilePatchesPerIsland => fertilePatchesPerIsland;
        public int FertileMinCoastCells => fertileMinCoastCells;
        public float FishShoalShoreMeters => fishShoalShoreMeters;
        public float FishShoalMinSpacing => fishShoalMinSpacing;
        /// <summary>The dungeon's gnome hirelings: how many may be alive at once, how far the hero walks before they set off after him, and how far from him they break off a fight to catch up (cells).</summary>
        public int DungeonHirelingMax => dungeonHirelingMax;
        public float DungeonHirelingFollowCells => dungeonHirelingFollowCells;
        public float DungeonHirelingLeashCells => dungeonHirelingLeashCells;
        /// <summary>A dungeon potion (Dungeon.PotionKind as an index): its price in the shop, how long it works, and its strength (healing: share of health back; rage/speed: factor; defence: share of damage taken).</summary>
        public int PotionCoins(int kind) => potionCoins[kind];
        public float PotionSeconds(int kind) => potionSeconds[kind];
        public float PotionPower(int kind) => potionPower[kind];
        public float FertilePatchRadius => fertilePatchRadius;
        public float FertileFarmBonus => fertileFarmBonus;
        /// <summary>The archipelago's fish shoals: how many, the fish in each, a Fisher's Hut's extra catch by one (1 = double), its reach, and the wait before a fished-out shoal turns up elsewhere.</summary>
        public int FishShoalCount => fishShoalCount;
        public int FishShoalStock => fishShoalStock;
        public float FishShoalBonus => fishShoalBonus;
        public float FishHutReachMeters => fishHutReachMeters;
        public float FishShoalRespawnSeconds => fishShoalRespawnSeconds;
        /// <summary>The lighthouse: how far from it a spot may be lit, how many cells round it open, its price in coins, the lantern's wait, and how near the water it must stand (cells).</summary>
        public float LighthouseRangeMeters => lighthouseRangeMeters;
        public int LighthouseRevealCells => lighthouseRevealCells;
        public int LighthouseRevealCoins => lighthouseRevealCoins;
        public float LighthouseCooldownSeconds => lighthouseCooldownSeconds;
        public int LighthouseCoastCells => lighthouseCoastCells;
        /// <summary>How far round itself (cells) a lighthouse of `level` clears the fog for good.</summary>
        public int LighthouseLightCells(int level) =>
            lighthouseLightCells == null || lighthouseLightCells.Length == 0 ? 45 : lighthouseLightCells[Mathf.Clamp(level, 1, lighthouseLightCells.Length) - 1];
        /// <summary>The archipelago's island riches: ore veins per island of each kind, and fertile patches on a fertile island.</summary>
        public int IslandStartCoal => islandStartCoal;
        public int IslandStartCopper => islandStartCopper;
        public int IslandIronVeins => islandIronVeins;
        public int IslandIronCoal => islandIronCoal;
        public int IslandCopperVeins => islandCopperVeins;
        public int IslandGoldVeins => islandGoldVeins;
        public int IslandStoneCoal => islandStoneCoal;
        public int IslandVeinShoreGap => islandVeinShoreGap;
        public int StartScoutGraceCells => startScoutGraceCells;
        public int IslandFertilePatches => islandFertilePatches;

        private static readonly string[] TideKindIds = { "crab", "shark", "puffer", "shaman", "serpent" };
        private static readonly string[] TideRewardIds = { "wood", "stone", "gold", "crystals" };

        /// <summary>How many guards of `kind` a cache of `tier` (1..4) has (tide_t{tier}_{kind}).</summary>
        public int TideGuards(int tier, Combat.OrcKind kind)
        {
            var k = kind == Combat.OrcKind.TideCrab ? 0 : kind == Combat.OrcKind.TideShark ? 1 : kind == Combat.OrcKind.TidePuffer ? 2
                : kind == Combat.OrcKind.TideShaman ? 3 : kind == Combat.OrcKind.TideSerpent ? 4 : -1;
            var i = (Mathf.Clamp(tier, 1, 4) - 1) * TideKindIds.Length + k;
            return k < 0 || tideGuards == null || i >= tideGuards.Length ? 0 : tideGuards[i];
        }

        /// <summary>What a looted cache of `tier` pays in wood, stone or gold (tide_reward_*_{tier}).</summary>
        public int TideReward(int tier, Resources.ResourceType type)
        {
            var k = type == Resources.ResourceType.Wood ? 0 : type == Resources.ResourceType.Stone ? 1 : type == Resources.ResourceType.Gold ? 2 : -1;
            var i = (Mathf.Clamp(tier, 1, 4) - 1) * TideRewardIds.Length + k;
            return k < 0 || tideRewards == null || i >= tideRewards.Length ? 0 : tideRewards[i];
        }

        public int TideRewardCrystals(int tier)
        {
            var i = (Mathf.Clamp(tier, 1, 4) - 1) * TideRewardIds.Length + 3;
            return tideRewards == null || i >= tideRewards.Length ? 0 : tideRewards[i];
        }

        public float TideGuardRadius => tideGuardRadius;

        private int[] FoodDayArray(string id) =>
            id == BuildingIds.FisherHut ? foodDayFisherHut : id == BuildingIds.Farm ? foodDayFarm
            : id == BuildingIds.Orchard ? foodDayOrchard : id == BuildingIds.PigFarm ? foodDayPigFarm : null;

        private int[] FoodBoostArray(string id) =>
            id == BuildingIds.Baker ? foodBoostBaker : id == BuildingIds.Smokehouse ? foodBoostSmokehouse : null;

        /// <summary>Food a day a producer makes at `level` before its bonuses (food_day_<id>_<level>); 0 for any other building.</summary>
        public int FoodPerDay(string id, int level)
        {
            var a = FoodDayArray(id);
            return a == null || a.Length == 0 ? 0 : a[Mathf.Clamp(level, 1, a.Length) - 1];
        }

        /// <summary>What a Bakery or Smokehouse at `level` adds to each producer in its reach (food_boost_<id>_<level>).</summary>
        public int FoodBoost(string id, int level)
        {
            var a = FoodBoostArray(id);
            return a == null || a.Length == 0 ? 0 : a[Mathf.Clamp(level, 1, a.Length) - 1];
        }

        public int FoodFertileBonus => foodFertileBonus;
        public int FisherShoreCells => fisherShoreCells;
        public int FoodInlandCells => foodInlandCells;
        public float TideWakeRadius => tideWakeRadius;
        public int TideShamanHeal => tideShamanHeal;
        public float TideShamanHealEverySeconds => tideShamanHealEverySeconds;
        public float TideShamanHealRadius => tideShamanHealRadius;
        public float KingXpRadius => kingXpRadius;
        public float KingGuardRadius => kingGuardRadius;
        public int KingRevealRadius => kingRevealRadius;
        public float HeroReturnWaitSeconds => heroReturnWaitSeconds;
        /// <summary>The sandbox's hero let go (the user, 2026-11-01): he stands where he is and strolls this far round it, a step every this many seconds.</summary>
        public float HeroPatrolRadiusCells => heroPatrolRadiusCells;
        public float HeroPatrolPauseSeconds => heroPatrolPauseSeconds;
        /// <summary>Ruins (the user, 2026-11-05): restoring one costs this share of what the building was worth (its price and every upgrade, coins too). The sandbox tab's ruin_restore_cost_fraction.</summary>
        public float RuinRestoreCostFraction => ruinRestoreCostFraction;
        /// <summary>Restoring a ruin takes this share of the time its building took to put up to its level. ruin_restore_time_fraction.</summary>
        public float RuinRestoreTimeFraction => ruinRestoreTimeFraction;

        /// <summary>A hero's row by id ("king", "queen", "captain"), or null.</summary>
        public HeroBalance Hero(string id)
        {
            foreach (var hero in heroes)
            {
                if (hero.id == id) return hero;
            }
            return null;
        }

        [NonSerialized] private Dictionary<string, UnitBalance> _heroUnits;

        /// <summary>
        /// A hero as a unit row, for the SoldierUnit that does his fighting: one level only -- a
        /// hero's levels are his own (Hero), not the Laboratory's.
        /// </summary>
        public UnitBalance HeroUnit(string id)
        {
            _heroUnits ??= new Dictionary<string, UnitBalance>();
            if (_heroUnits.TryGetValue(id, out var unit)) return unit;
            var hero = Hero(id);
            if (hero == null) return KingUnit;
            unit = new UnitBalance
            {
                id = id,
                displayName = id,
                attackRangeUnits = hero.attackRange,
                attackRangeStructures = hero.attackRange + 1.2f,
                engageRadius = hero.engageRadius,
                startsUnlocked = false,
                levels = new List<UnitLevelStats>
                {
                    new UnitLevelStats { maxHealth = hero.maxHealth, attackDamage = hero.attackDamage, attackIntervalSeconds = hero.attackIntervalSeconds, walkSpeed = hero.walkSpeed },
                },
            };
            _heroUnits[id] = unit;
            return unit;
        }

        /// <summary>Every hero skill in the sheet, by id order.</summary>
        public IReadOnlyList<HeroSkillBalance> HeroSkills => heroSkills;
        public int InvasionHouses => invasionHouses;
        /// <summary>How long a house takes to make its next person (HousePeople): the sandbox tab's person_respawn_sec, a run's inv_person_respawn_sec.</summary>
        public float PersonRespawnSeconds => personRespawnSeconds;
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
        /// <summary>How much longer than the straight line a wave orc's way round something on its portal's way may be before it breaks through instead (OrcUnit.WalkTheLane).</summary>
        public float InvasionLaneDetourMeters => invasionLaneDetourMeters;
        /// <summary>The raiding party (InvasionWaves.SendRaidParty): with every how-many-th wave it goes out, its raiders and throwers, and how far from the castle the base ends -- it breaks only what stands further out.</summary>
        public int InvasionRaidPartyEvery => invasionRaidPartyEvery;
        public int InvasionRaidPartyRaiders => invasionRaidPartyRaiders;
        public int InvasionRaidPartyThrowers => invasionRaidPartyThrowers;
        public float InvasionRaidPartyBaseRadius => invasionRaidPartyBaseRadius;
        /// <summary>The share of their health a manned Дом лекаря heals the wounded in its radius each second, at `level` (InvasionHealing).</summary>
        public float InvasionHealerHealShareAt(int level) => invasionHealerHealShare + invasionHealerHealSharePerLevel * (Mathf.Max(1, level) - 1);
        /// <summary>New Year map: Gifts laid on the island after each wave.</summary>
        public int NyGiftsPerWave => nyGiftsPerWave;
        /// <summary>New Year map: Most gifts lying about at once.</summary>
        public int NyGiftsMax => nyGiftsMax;
        /// <summary>New Year map: Nearest to the castle a gift is laid, metres.</summary>
        public float NyGiftMinRadius => nyGiftMinRadius;
        /// <summary>New Year map: Furthest from the castle a gift is laid, metres.</summary>
        public float NyGiftMaxRadius => nyGiftMaxRadius;
        /// <summary>New Year map: How close the King, a soldier or an islander comes to take a gift.</summary>
        public float NyGiftPickupMeters => nyGiftPickupMeters;
        /// <summary>New Year map: Wood in a gift of wood.</summary>
        public int NyGiftWood => nyGiftWood;
        /// <summary>New Year map: Stone in a gift of stone.</summary>
        public int NyGiftStone => nyGiftStone;
        /// <summary>New Year map: Gold in a gift of gold.</summary>
        public int NyGiftGold => nyGiftGold;
        /// <summary>New Year map: From which wave the Thief comes.</summary>
        public int NyThiefFromWave => nyThiefFromWave;
        /// <summary>New Year map: Thieves with each wave (while gifts lie about).</summary>
        public int NyThievesPerWave => nyThievesPerWave;
        /// <summary>New Year map: Gold for a Thief killed.</summary>
        public int NyGoldThief => nyGoldThief;
        /// <summary>New Year map: Chance a wave brings a blizzard (never the midnight wave).</summary>
        public float NyBlizzardChance => nyBlizzardChance;
        /// <summary>New Year map: How long a blizzard lasts.</summary>
        public float NyBlizzardSeconds => nyBlizzardSeconds;
        /// <summary>New Year map: How long before a blizzard it is announced.</summary>
        public float NyBlizzardWarningSeconds => nyBlizzardWarningSeconds;
        /// <summary>New Year map: Towers' reach in a blizzard, times.</summary>
        public float NyBlizzardRangeMult => nyBlizzardRangeMult;
        /// <summary>New Year map: Everyone's walking speed in a blizzard, times.</summary>
        public float NyBlizzardSpeedMult => nyBlizzardSpeedMult;
        /// <summary>New Year map: Every how-many-th wave is midnight: the salute, a bigger wave, the Ice Shaman.</summary>
        public int NyChimesEvery => nyChimesEvery;
        /// <summary>New Year map: The midnight wave's orcs, times a wave's.</summary>
        public float NyChimesOrcsMult => nyChimesOrcsMult;
        /// <summary>New Year map: Seconds after the midnight wave comes out that the salute bursts over it.</summary>
        public float NySaluteDelaySeconds => nySaluteDelaySeconds;
        /// <summary>New Year map: Share of its health the salute takes off every orc of the run.</summary>
        public float NySaluteDamageShare => nySaluteDamageShare;
        /// <summary>New Year map: How often the Ice Shaman freezes a tower.</summary>
        public float NyShamanFreezeEverySeconds => nyShamanFreezeEverySeconds;
        /// <summary>New Year map: How far from him a tower can be frozen.</summary>
        public float NyShamanFreezeRadius => nyShamanFreezeRadius;
        /// <summary>New Year map: How long a frozen tower does not shoot.</summary>
        public float NyShamanFreezeSeconds => nyShamanFreezeSeconds;
        /// <summary>New Year map: Gold for the Ice Shaman killed.</summary>
        public int NyGoldShaman => nyGoldShaman;
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
        public float InvasionSoldierGuardRadius => invasionSoldierGuardRadius;
        public float OrcMeleeReachMeters => orcMeleeReachMeters;
        public float InvasionOrcHealthMultiplier => invasionOrcHealthMultiplier;
        public float InvasionOrcDamageMultiplier => invasionOrcDamageMultiplier;
        public float InvasionStoneReserveMultiplier => invasionStoneReserveMultiplier;
        /// <summary>What a run's sawmill man brings each trip (inv_wood_per_harvest) and a quarry man (inv_stone_per_harvest).</summary>
        public int InvasionWoodPerHarvest => invasionWoodPerHarvest;
        public int InvasionStonePerHarvest => invasionStonePerHarvest;
        /// <summary>Metres round the camera's look the minimap shows: the sandbox tab's minimap_radius, a run's inv_minimap_radius.</summary>
        public float MinimapRadius => minimapRadius;
        public int InvasionCitizenHealth => invasionCitizenHealth;
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
        /// would be blanks around six numbers. His LEVELS are the hero's own (see Hero), not
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
                // A run plays on its own numbers (the user, 2026-10-23): from the menu's «В бой»
                // (the intent, before the scene's Awakes) to the run's end (InvasionMode is gone).
                if (Saving.GameSessionIntent.NewGameIsInvasion || Invasion.InvasionMode.IsActive) return Run;
                return Sandbox;
            }
        }

        /// <summary>The Invasion run's numbers, whether or not a run is on (tests compare the two); the sandbox's if the run asset is missing.</summary>
        public static BalanceConfig Run
        {
            get
            {
                if (_runInstance != null) return _runInstance;
                _runInstance = UnityEngine.Resources.Load<BalanceConfig>(RunResourcePath);
                if (_runInstance == null)
                {
                    Debug.LogError($"BalanceConfig: no asset at Resources/{RunResourcePath} -- an Invasion run plays on the sandbox's numbers. Rebuild the config from the CSVs.");
                    _runInstance = Sandbox;
                }
                return _runInstance;
            }
        }

        /// <summary>The sandbox's numbers, whatever mode is on.</summary>
        public static BalanceConfig Sandbox
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
            armyMaxSquads = (int)Read(economy, "army_max_squads", armyMaxSquads);
            garrisonDefendCells = Read(economy, "garrison_defend_cells", garrisonDefendCells);
            squadFormationSpacing = Read(economy, "squad_formation_spacing", squadFormationSpacing);
            squadScatterSpacing = Read(economy, "squad_scatter_spacing", squadScatterSpacing);
            squadGuardPatrolCells = Read(economy, "squad_guard_patrol_cells", squadGuardPatrolCells);
            squadGuardZoneCells = Read(economy, "squad_guard_zone_cells", squadGuardZoneCells);
            squadFollowCells = Read(economy, "squad_follow_cells", squadFollowCells);
            for (var l = 2; l <= 5; l++)
            {
                shipLevelCoins[l - 2] = (int)Read(economy, "ship_level_coins_" + l, shipLevelCoins[l - 2]);
                shipLevelSeconds[l - 2] = Read(economy, "ship_level_seconds_" + l, shipLevelSeconds[l - 2]);
            }
            shipMachinePlaces = (int)Read(economy, "ship_machine_places", shipMachinePlaces);
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
            coinsPerCitizenPerDay = Read(economy, "coins_per_citizen_per_day", coinsPerCitizenPerDay);
            taxHouseBonusPerLevel = Read(economy, "tax_house_bonus_per_level", taxHouseBonusPerLevel);
            for (var l = 0; l < taxHouseResearchCrystals.Length; l++) taxHouseResearchCrystals[l] = (int)Read(economy, $"tax_house_research_crystals_{l + 1}", taxHouseResearchCrystals[l]);
            islandStartRocks = (int)Read(economy, "island_start_rocks", islandStartRocks);
            loadingSecondsEnter = Read(economy, "loading_seconds_enter", loadingSecondsEnter);
            loadingSecondsExit = Read(economy, "loading_seconds_exit", loadingSecondsExit);
            tutorialRewardCrystals = (int)Read(economy, "tutorial_reward_crystals", tutorialRewardCrystals);
            shopSlotSeconds = Read(economy, "shop_slot_seconds", shopSlotSeconds);
            shopSlotStaggerSeconds = Read(economy, "shop_slot_stagger_seconds", shopSlotStaggerSeconds);
            shopBonusChance = Read(economy, "shop_bonus_chance", shopBonusChance);
            shopBonusMinSeconds = Read(economy, "shop_bonus_min_seconds", shopBonusMinSeconds);
            shopNoticeSeconds = Read(economy, "shop_notice_seconds", shopNoticeSeconds);
            shopStarterDelaySeconds = Read(economy, "shop_starter_delay_seconds", shopStarterDelaySeconds);
            shopAdStubSeconds = Read(economy, "shop_ad_stub_seconds", shopAdStubSeconds);
            shopTimerWarnSeconds = Read(economy, "shop_timer_warn_seconds", shopTimerWarnSeconds);
            shopCrystalUsd = Read(economy, "shop_crystal_usd", shopCrystalUsd);
            shopAdCrystalsBase = Read(economy, "shop_ad_crystals_base", shopAdCrystalsBase);
            shopAdMaxAds = Read(economy, "shop_ad_max_ads", shopAdMaxAds);
            tutorialBuildSeconds = Read(economy, "tutorial_build_sec", tutorialBuildSeconds);
            tutorialStartPeople = (int)Read(economy, "tutorial_start_people", tutorialStartPeople);
            tutorialKingHealBelow = Read(economy, "tutorial_king_heal_below", tutorialKingHealBelow);
            tutorialOrcDamageMult = Read(economy, "tutorial_orc_damage_mult", tutorialOrcDamageMult);
            tutorialOrcHealthMult = Read(economy, "tutorial_orc_health_mult", tutorialOrcHealthMult);
            tutorialGatherSeconds = Read(economy, "tutorial_gather_sec", tutorialGatherSeconds);
            tutorialPersonZoom = Read(economy, "tutorial_person_zoom", tutorialPersonZoom);
            tutorialFishShoals = (int)Read(economy, "tutorial_fish_shoals", tutorialFishShoals);
            tutorialPeopleHintSeconds = Read(economy, "tutorial_people_hint_sec", tutorialPeopleHintSeconds);
            tutorialBlinkSeconds = Read(economy, "tutorial_blink_sec", tutorialBlinkSeconds);
            tutorialRaidCameraSeconds = Read(economy, "tutorial_raid_camera_sec", tutorialRaidCameraSeconds);
            victoryDelaySeconds = Read(economy, "victory_delay_sec", victoryDelaySeconds);
            tutorialRaidOrcs = (int)Read(economy, "tutorial_raid_orcs", tutorialRaidOrcs);
            tutorialPortalSeconds = Read(economy, "tutorial_portal_sec", tutorialPortalSeconds);
            tutorialBirthSeconds = Read(economy, "tutorial_birth_sec", tutorialBirthSeconds);
            tutorialOrcGuards = (int)Read(economy, "tutorial_orc_guards", tutorialOrcGuards);
            wallWalkFromLevel = (int)Read(economy, "wall_walk_from_level", wallWalkFromLevel);
            for (var l = 0; l < wallWalkHeight.Length; l++) wallWalkHeight[l] = Read(economy, $"wall_walk_height_{l + 3}", wallWalkHeight[l]);
            wallRangeBonusCells = Read(economy, "wall_range_bonus_cells", wallRangeBonusCells);
            wallSeekRadiusCells = Read(economy, "wall_seek_radius_cells", wallSeekRadiusCells);
            wallClimbSeconds = Read(economy, "wall_climb_sec", wallClimbSeconds);
            wallJumpHealthShare = Read(economy, "wall_jump_health_share", wallJumpHealthShare);
            siegeGangwayHeight = Read(economy, "siege_gangway_height", siegeGangwayHeight);
            moatResearchCrystals = (int)Read(economy, "moat_research_crystals", moatResearchCrystals);
            moatSpeedMultiplier = Read(economy, "moat_speed_multiplier", moatSpeedMultiplier);
            moatWadeDepth = Read(economy, "moat_wade_depth", moatWadeDepth);
            bridgeDeckHeight = Read(economy, "bridge_deck_height", bridgeDeckHeight);
            botRelationsStart = Read(economy, "bot_relations_start", botRelationsStart);
            botTradeMinScore = Read(economy, "bot_trade_min_score", botTradeMinScore);
            botWarDeclareScoreLoss = Read(economy, "bot_war_declare_score_loss", botWarDeclareScoreLoss);
            botRelationsDriftPerDay = Read(economy, "bot_relations_drift_per_day", botRelationsDriftPerDay);
            botRebuildCalmSeconds = Read(economy, "bot_rebuild_calm_sec", botRebuildCalmSeconds);
            botRebuildEveryRaids = Mathf.RoundToInt(Read(economy, "bot_rebuild_every_raids", botRebuildEveryRaids));
            botCalmRadiusCells = Read(economy, "bot_calm_radius_cells", botCalmRadiusCells);
            botGarrisonRespawnSeconds = Read(economy, "bot_garrison_respawn_sec", botGarrisonRespawnSeconds);
            botGarrisonPatrols = Mathf.RoundToInt(Read(economy, "bot_garrison_patrols", botGarrisonPatrols));
            botGarrisonPatrolSize = Mathf.RoundToInt(Read(economy, "bot_garrison_patrol_size", botGarrisonPatrolSize));
            botGarrisonArchers = Mathf.RoundToInt(Read(economy, "bot_garrison_archers", botGarrisonArchers));
            botGarrisonLevel = Mathf.RoundToInt(Read(economy, "bot_garrison_level", botGarrisonLevel));
            botRaidFirstSeconds = Read(economy, "bot_raid_first_sec", botRaidFirstSeconds);
            botRaidIntervalSeconds = Read(economy, "bot_raid_interval_sec", botRaidIntervalSeconds);
            botRaidSize = Mathf.RoundToInt(Read(economy, "bot_raid_size", botRaidSize));
            botRaidLevel = Mathf.RoundToInt(Read(economy, "bot_raid_level", botRaidLevel));
            botWarRaidFirstSeconds = Read(economy, "bot_war_raid_first_sec", botWarRaidFirstSeconds);
            botWarRaidIntervalSeconds = Read(economy, "bot_war_raid_interval_sec", botWarRaidIntervalSeconds);
            botWarLevel = Mathf.RoundToInt(Read(economy, "bot_war_level", botWarLevel));
            botWarEscorts = Mathf.RoundToInt(Read(economy, "bot_war_escorts", botWarEscorts));
            botWarGuardRadiusCells = Read(economy, "bot_war_guard_radius_cells", botWarGuardRadiusCells);
            botWarContributionBase = Mathf.RoundToInt(Read(economy, "bot_war_contribution_base", botWarContributionBase));
            botWarContributionPerWave = Mathf.RoundToInt(Read(economy, "bot_war_contribution_per_wave", botWarContributionPerWave));
            botWarContributionHallDiscount = Read(economy, "bot_war_contribution_hall_discount", botWarContributionHallDiscount);
            botWarCoinsPerCrystal = Mathf.RoundToInt(Read(economy, "bot_war_coins_per_crystal", botWarCoinsPerCrystal));
            botWarContributionPerBuilding = Mathf.RoundToInt(Read(economy, "bot_war_contribution_per_building", botWarContributionPerBuilding));
            botWarContributionMin = Mathf.RoundToInt(Read(economy, "bot_war_contribution_min", botWarContributionMin));
            botWarStartWaveStep = Mathf.RoundToInt(Read(economy, "bot_war_start_wave_step", botWarStartWaveStep));
            moatPathCost = Read(economy, "moat_path_cost", moatPathCost);
            drawbridgeRaiseRadiusCells = Read(economy, "drawbridge_raise_radius_cells", drawbridgeRaiseRadiusCells);
            drawbridgeRaiseDegrees = Read(economy, "drawbridge_raise_degrees", drawbridgeRaiseDegrees);
            drawbridgeRaiseSeconds = Read(economy, "drawbridge_raise_sec", drawbridgeRaiseSeconds);
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
            starveLeaveIntervalSeconds = Read(economy, "starve_leave_interval_sec", starveLeaveIntervalSeconds);
            eventCaptionSeconds = Read(economy, "event_caption_sec", eventCaptionSeconds);
            foodVarietyTarget = (int)Read(economy, "food_variety_target", foodVarietyTarget);
            deathsMemoryDays = (int)Read(economy, "deaths_memory_days", deathsMemoryDays);
            happinessPenaltyPerDeath = (int)Read(economy, "happiness_penalty_per_death", happinessPenaltyPerDeath);
            happinessPerCitizenTarget = Read(economy, "happiness_per_citizen_target", happinessPerCitizenTarget);
            researchSecondsPerWorker = Read(economy, "research_seconds_per_worker", researchSecondsPerWorker);
            researchFreeWorkers = (int)Read(economy, "research_free_workers", researchFreeWorkers);
            researchCancelRefundPercent = (int)Read(economy, "research_cancel_refund_percent", researchCancelRefundPercent);
            researchMinSeconds = Read(economy, "research_min_seconds", researchMinSeconds);
            researchCrystals = (int)Read(economy, "research_crystals", researchCrystals);
            researchCrystalsFromLevel = (int)Read(economy, "research_crystals_from_level", researchCrystalsFromLevel);
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
            shipRepairWoodPerHp = Read(economy, "ship_repair_wood_per_hp", shipRepairWoodPerHp);
            shipRepairReachMeters = Read(economy, "ship_repair_reach_meters", shipRepairReachMeters);
            shipIdleReturnSeconds = Read(economy, "ship_idle_return_sec", shipIdleReturnSeconds);
            shipHomeReachCells = Read(economy, "ship_home_reach_cells", shipHomeReachCells);
            orcShipyardHealth = (int)Read(economy, "orc_shipyard_health", orcShipyardHealth);
            orcShipyardDamage = (int)Read(economy, "orc_shipyard_damage", orcShipyardDamage);
            orcShipyardRangeMeters = Read(economy, "orc_shipyard_range_meters", orcShipyardRangeMeters);
            orcShipyardRebuildSeconds = Read(economy, "orc_shipyard_rebuild_seconds", orcShipyardRebuildSeconds);
            orcFleetEscorts = (int)Read(economy, "orc_fleet_escorts", orcFleetEscorts);
            forestCellsPerTree = Read(economy, "archipelago_forest_cells_per_tree", forestCellsPerTree);
            archipelagoTreesMin = (int)Read(economy, "archipelago_trees_min", archipelagoTreesMin);
            archipelagoTreesMax = (int)Read(economy, "archipelago_trees_max", archipelagoTreesMax);
            stonyCellsPerRock = Read(economy, "archipelago_stony_cells_per_rock", stonyCellsPerRock);
            archipelagoRocksMin = (int)Read(economy, "archipelago_rocks_min", archipelagoRocksMin);
            archipelagoRocksMax = (int)Read(economy, "archipelago_rocks_max", archipelagoRocksMax);
            rocksScatteredShare = Read(economy, "archipelago_rocks_scattered_share", rocksScatteredShare);
            sceneryStreamSquare = Read(economy, "scenery_stream_square_cells", sceneryStreamSquare);
            dungeonEntranceCount = (int)Read(economy, "dungeon_entrance_count", dungeonEntranceCount);
            dungeonEnterReach = Read(economy, "dungeon_enter_reach_cells", dungeonEnterReach);
            dungeonWadeSpeedMultiplier = Read(economy, "dungeon_wade_speed_multiplier", dungeonWadeSpeedMultiplier);
            dungeonRevealCells = (int)Read(economy, "dungeon_reveal_cells", dungeonRevealCells);
            dungeonCameraDistance = Read(economy, "dungeon_camera_distance", dungeonCameraDistance);
            dungeonCameraPitch = Read(economy, "dungeon_camera_pitch", dungeonCameraPitch);
            dungeonGuardRadius = Read(economy, "dungeon_monster_guard_radius", dungeonGuardRadius);
            dungeonBossGuardRadius = Read(economy, "dungeon_boss_guard_radius", dungeonBossGuardRadius);
            dungeonMonsterLevel1 = (int)Read(economy, "dungeon_monster_level_1", dungeonMonsterLevel1);
            dungeonMonsterLevel2 = (int)Read(economy, "dungeon_monster_level_2", dungeonMonsterLevel2);
            dungeonMonsterLevel3 = (int)Read(economy, "dungeon_monster_level_3", dungeonMonsterLevel3);
            dungeonCoinsPerHealth = Read(economy, "dungeon_coins_per_health", dungeonCoinsPerHealth);
            dungeonXpShare = Read(economy, "dungeon_xp_share", dungeonXpShare);
            dungeonBossCoins = (int)Read(economy, "dungeon_boss_coins", dungeonBossCoins);
            dungeonBossCrystals = (int)Read(economy, "dungeon_boss_crystals", dungeonBossCrystals);
            dungeonBossWood = (int)Read(economy, "dungeon_boss_wood", dungeonBossWood);
            dungeonBossStone = (int)Read(economy, "dungeon_boss_stone", dungeonBossStone);
            dungeonBossGold = (int)Read(economy, "dungeon_boss_gold", dungeonBossGold);
            dungeonEngageFactor = Read(economy, "dungeon_engage_factor", dungeonEngageFactor);
            dungeonRangedRangeFactor = Read(economy, "dungeon_ranged_range_factor", dungeonRangedRangeFactor);
            lairRats = (int)Read(economy, "dungeon_lair_rats", lairRats);
            dungeonBossThemeLairs = (int)Read(economy, "dungeon_boss_theme_lairs", dungeonBossThemeLairs);
            dungeonMonsterWakeMargin = Read(economy, "dungeon_monster_wake_margin", dungeonMonsterWakeMargin);
            dungeonCrystalsPerRoom = (int)Read(economy, "dungeon_crystals_per_room", dungeonCrystalsPerRoom);
            dungeonCrystalsLone = (int)Read(economy, "dungeon_crystals_lone", dungeonCrystalsLone);
            dungeonCrystalPickReach = Read(economy, "dungeon_crystal_pick_reach", dungeonCrystalPickReach);
            dungeonCrystalFloat = Read(economy, "dungeon_crystal_float", dungeonCrystalFloat);
            dungeonLairPackShare = Read(economy, "dungeon_lair_pack_share", dungeonLairPackShare);
            dungeonGuardRoomShare = Read(economy, "dungeon_guard_room_share", dungeonGuardRoomShare);
            dungeonGuardSingleShare = Read(economy, "dungeon_guard_single_share", dungeonGuardSingleShare);
            dungeonGuardBossReach = Read(economy, "dungeon_guard_boss_reach", dungeonGuardBossReach);
            dungeonPatrolSize = (int)Read(economy, "dungeon_patrol_size", dungeonPatrolSize);
            dungeonPatrolPauseSeconds = Read(economy, "dungeon_patrol_pause_sec", dungeonPatrolPauseSeconds);
            lairBats = (int)Read(economy, "dungeon_lair_bats", lairBats);
            lairSpiders = (int)Read(economy, "dungeon_lair_spiders", lairSpiders);
            lairSpiderlings = (int)Read(economy, "dungeon_lair_spiderlings", lairSpiderlings);
            lairSkeletons = (int)Read(economy, "dungeon_lair_skeletons", lairSkeletons);
            lairKobolds = (int)Read(economy, "dungeon_lair_kobolds", lairKobolds);
            lairStoneGolems = (int)Read(economy, "dungeon_lair_stone_golems", lairStoneGolems);
            spiderPoisonSeconds = Read(economy, "dungeon_spider_poison_sec", spiderPoisonSeconds);
            spiderPoisonKeep = Read(economy, "dungeon_spider_poison_keep", spiderPoisonKeep);
            skeletonGetUpSeconds = Read(economy, "dungeon_skeleton_getup_sec", skeletonGetUpSeconds);
            skeletonGetUpHealth = Read(economy, "dungeon_skeleton_getup_health", skeletonGetUpHealth);
            queenWebEvery = Read(economy, "dungeon_queen_web_every_sec", queenWebEvery);
            queenWebStun = Read(economy, "dungeon_queen_web_stun_sec", queenWebStun);
            queenWebRange = Read(economy, "dungeon_queen_web_range", queenWebRange);
            queenBroodEvery = Read(economy, "dungeon_queen_brood_every_sec", queenBroodEvery);
            queenBroodCount = (int)Read(economy, "dungeon_queen_brood_count", queenBroodCount);
            queenCocoonSeconds = Read(economy, "dungeon_queen_cocoon_sec", queenCocoonSeconds);
            queenCocoonBrood = (int)Read(economy, "dungeon_queen_cocoon_brood", queenCocoonBrood);
            queenCocoonHeal = Read(economy, "dungeon_queen_cocoon_heal", queenCocoonHeal);
            golemSlamEvery = Read(economy, "dungeon_golem_slam_every_sec", golemSlamEvery);
            golemSlamRadius = Read(economy, "dungeon_golem_slam_radius", golemSlamRadius);
            golemSlamDamage = Read(economy, "dungeon_golem_slam_damage_mult", golemSlamDamage);
            golemBoulderEvery = Read(economy, "dungeon_golem_boulder_every_sec", golemBoulderEvery);
            golemBoulderRange = Read(economy, "dungeon_golem_boulder_range", golemBoulderRange);
            golemArmour = Read(economy, "dungeon_golem_armour", golemArmour);
            lair2DrownedRats = (int)Read(economy, "dungeon2_lair_drowned_rats", lair2DrownedRats);
            lair2DwarfSkeletons = (int)Read(economy, "dungeon2_lair_dwarf_skeletons", lair2DwarfSkeletons);
            lair2ForgeSpirits = (int)Read(economy, "dungeon2_lair_forge_spirits", lair2ForgeSpirits);
            lair2DrownedDwarfs = (int)Read(economy, "dungeon2_lair_drowned_dwarfs", lair2DrownedDwarfs);
            lair2Eels = (int)Read(economy, "dungeon2_lair_eels", lair2Eels);
            lair2StoneGuards = (int)Read(economy, "dungeon2_lair_stone_guards", lair2StoneGuards);
            dungeon2LoneEels = (int)Read(economy, "dungeon2_lone_eels", dungeon2LoneEels);
            dungeon2EelSpacing = Read(economy, "dungeon2_eel_spacing", dungeon2EelSpacing);
            dwarfSkeletonArmour = Read(economy, "dungeon_dwarf_skeleton_armour", dwarfSkeletonArmour);
            ambushReach = Read(economy, "dungeon_ambush_reach", ambushReach);
            ambushRiseSeconds = Read(economy, "dungeon_ambush_rise_sec", ambushRiseSeconds);
            kingSlamEvery = Read(economy, "dungeon_king_slam_every_sec", kingSlamEvery);
            kingSlamRadius = Read(economy, "dungeon_king_slam_radius", kingSlamRadius);
            kingSlamDamage = Read(economy, "dungeon_king_slam_damage_mult", kingSlamDamage);
            kingGuardsEvery = Read(economy, "dungeon_king_guards_every_sec", kingGuardsEvery);
            kingGuardsCount = (int)Read(economy, "dungeon_king_guards_count", kingGuardsCount);
            kingThroneGuards = (int)Read(economy, "dungeon_king_throne_guards", kingThroneGuards);
            kingThroneSeconds = Read(economy, "dungeon_king_throne_sec", kingThroneSeconds);
            kingThroneHeal = Read(economy, "dungeon_king_throne_heal", kingThroneHeal);
            serpentDiveEvery = Read(economy, "dungeon_serpent_dive_every_sec", serpentDiveEvery);
            serpentDiveSeconds = Read(economy, "dungeon_serpent_dive_sec", serpentDiveSeconds);
            serpentSurfaceReach = Read(economy, "dungeon_serpent_surface_reach", serpentSurfaceReach);
            serpentSpitStun = Read(economy, "dungeon_serpent_spit_stun_sec", serpentSpitStun);
            dungeon2CrystalsPerRoom = (int)Read(economy, "dungeon2_crystals_per_room", dungeon2CrystalsPerRoom);
            dungeonChestReach = Read(economy, "dungeon_chest_reach", dungeonChestReach);
            dungeonLavaDamagePerSecond = Read(economy, "dungeon_lava_damage_per_second", dungeonLavaDamagePerSecond);
            lair3FireImps = (int)Read(economy, "dungeon3_lair_fire_imps", lair3FireImps);
            lair3LavaHounds = (int)Read(economy, "dungeon3_lair_lava_hounds", lair3LavaHounds);
            lair3Salamanders = (int)Read(economy, "dungeon3_lair_salamanders", lair3Salamanders);
            lair3EmberCultists = (int)Read(economy, "dungeon3_lair_ember_cultists", lair3EmberCultists);
            lair3ObsidianGolems = (int)Read(economy, "dungeon3_lair_obsidian_golems", lair3ObsidianGolems);
            lair3MagmaSpawn = (int)Read(economy, "dungeon3_lair_magma_spawn", lair3MagmaSpawn);
            dungeon3CrystalsPerRoom = (int)Read(economy, "dungeon3_crystals_per_room", dungeon3CrystalsPerRoom);
            dragonBreathEvery = Read(economy, "dragon_breath_every", dragonBreathEvery);
            dragonBreathRange = Read(economy, "dragon_breath_range", dragonBreathRange);
            dragonBreathAngle = Read(economy, "dragon_breath_angle", dragonBreathAngle);
            dragonBreathDamage = Read(economy, "dragon_breath_damage", dragonBreathDamage);
            dragonFlightEvery = Read(economy, "dragon_flight_every", dragonFlightEvery);
            dragonFlightSeconds = Read(economy, "dragon_flight_seconds", dragonFlightSeconds);
            dragonFlightReach = Read(economy, "dragon_flight_reach", dragonFlightReach);
            dragonLandingRadius = Read(economy, "dragon_landing_radius", dragonLandingRadius);
            dragonLandingDamage = Read(economy, "dragon_landing_damage", dragonLandingDamage);
            titanRiseReach = Read(economy, "titan_rise_reach", titanRiseReach);
            titanRiseSeconds = Read(economy, "titan_rise_seconds", titanRiseSeconds);
            titanShedEvery = Read(economy, "titan_shed_every", titanShedEvery);
            titanShedCount = (int)Read(economy, "titan_shed_count", titanShedCount);
            tideLowSeconds = Read(economy, "dungeon3_tide_low_seconds", tideLowSeconds);
            tideWarnSeconds = Read(economy, "dungeon3_tide_warn_seconds", tideWarnSeconds);
            tideHighSeconds = Read(economy, "dungeon3_tide_high_seconds", tideHighSeconds);
            bridgeFallDelay = Read(economy, "dungeon3_bridge_fall_delay", bridgeFallDelay);
            bridgeReformSeconds = Read(economy, "dungeon3_bridge_reform_seconds", bridgeReformSeconds);
            dungeonChestWood = (int)Read(economy, "dungeon_chest_wood", dungeonChestWood);
            dungeonChestStone = (int)Read(economy, "dungeon_chest_stone", dungeonChestStone);
            dungeonChestGold = (int)Read(economy, "dungeon_chest_gold", dungeonChestGold);
            dungeonChestCoins = (int)Read(economy, "dungeon_chest_coins", dungeonChestCoins);
            fertilePatchesPerIsland = (int)Read(economy, "fertile_patches_per_island", fertilePatchesPerIsland);
            fertileMinCoastCells = (int)Read(economy, "fertile_min_coast_cells", fertileMinCoastCells);
            fishShoalShoreMeters = Read(economy, "fish_shoal_shore_meters", fishShoalShoreMeters);
            fishShoalMinSpacing = Read(economy, "fish_shoal_min_spacing", fishShoalMinSpacing);
            dungeonHirelingMax = (int)Read(economy, "dungeon_hireling_max", dungeonHirelingMax);
            dungeonHirelingFollowCells = Read(economy, "dungeon_hireling_follow_cells", dungeonHirelingFollowCells);
            dungeonHirelingLeashCells = Read(economy, "dungeon_hireling_leash_cells", dungeonHirelingLeashCells);
            for (var i = 0; i < PotionIds.Length; i++)
            {
                potionCoins[i] = (int)Read(economy, $"potion_{PotionIds[i]}_coins", potionCoins[i]);
                potionSeconds[i] = Read(economy, $"potion_{PotionIds[i]}_seconds", potionSeconds[i]);
                potionPower[i] = Read(economy, $"potion_{PotionIds[i]}_power", potionPower[i]);
            }
            fertilePatchRadius = Read(economy, "fertile_patch_radius", fertilePatchRadius);
            fertileFarmBonus = Read(economy, "fertile_farm_bonus", fertileFarmBonus);
            fishShoalCount = (int)Read(economy, "fish_shoal_count", fishShoalCount);
            fishShoalStock = (int)Read(economy, "fish_shoal_stock", fishShoalStock);
            fishShoalBonus = Read(economy, "fish_shoal_bonus", fishShoalBonus);
            fishHutReachMeters = Read(economy, "fish_hut_reach_meters", fishHutReachMeters);
            fishShoalRespawnSeconds = Read(economy, "fish_shoal_respawn_seconds", fishShoalRespawnSeconds);
            lighthouseRangeMeters = Read(economy, "lighthouse_range_meters", lighthouseRangeMeters);
            lighthouseRevealCells = (int)Read(economy, "lighthouse_reveal_cells", lighthouseRevealCells);
            lighthouseRevealCoins = (int)Read(economy, "lighthouse_reveal_coins", lighthouseRevealCoins);
            lighthouseCooldownSeconds = Read(economy, "lighthouse_cooldown_seconds", lighthouseCooldownSeconds);
            lighthouseCoastCells = (int)Read(economy, "lighthouse_coast_cells", lighthouseCoastCells);
            lighthouseLightCells = new int[5];
            for (var level = 1; level <= 5; level++)
                lighthouseLightCells[level - 1] = (int)Read(economy, "lighthouse_light_cells_" + level, 45 + (level - 1) * 9);
            islandStartCoal = (int)Read(economy, "island_start_coal", islandStartCoal);
            islandStartCopper = (int)Read(economy, "island_start_copper", islandStartCopper);
            islandIronVeins = (int)Read(economy, "island_iron_veins", islandIronVeins);
            islandIronCoal = (int)Read(economy, "island_iron_coal", islandIronCoal);
            islandCopperVeins = (int)Read(economy, "island_copper_veins", islandCopperVeins);
            islandGoldVeins = (int)Read(economy, "island_gold_veins", islandGoldVeins);
            islandStoneCoal = (int)Read(economy, "island_stone_coal", islandStoneCoal);
            islandVeinShoreGap = (int)Read(economy, "island_vein_shore_gap", islandVeinShoreGap);
            startScoutGraceCells = (int)Read(economy, "start_scout_grace_cells", startScoutGraceCells);
            islandFertilePatches = (int)Read(economy, "island_fertile_patches", islandFertilePatches);
            for (var tier = 1; tier <= 4; tier++)
            {
                for (var k = 0; k < TideKindIds.Length; k++)
                {
                    var i = (tier - 1) * TideKindIds.Length + k;
                    tideGuards[i] = (int)Read(economy, $"tide_t{tier}_{TideKindIds[k]}", tideGuards[i]);
                }
                for (var k = 0; k < TideRewardIds.Length; k++)
                {
                    var i = (tier - 1) * TideRewardIds.Length + k;
                    tideRewards[i] = (int)Read(economy, $"tide_reward_{TideRewardIds[k]}_{tier}", tideRewards[i]);
                }
            }
            tideGuardRadius = Read(economy, "tide_guard_radius", tideGuardRadius);
            foreach (var id in new[] { BuildingIds.FisherHut, BuildingIds.Farm, BuildingIds.Orchard, BuildingIds.PigFarm })
            {
                var a = FoodDayArray(id);
                for (var l = 0; l < a.Length; l++) a[l] = (int)Read(economy, $"food_day_{id.ToLowerInvariant()}_{l + 1}", a[l]);
            }
            foreach (var id in new[] { BuildingIds.Baker, BuildingIds.Smokehouse })
            {
                var a = FoodBoostArray(id);
                for (var l = 0; l < a.Length; l++) a[l] = (int)Read(economy, $"food_boost_{id.ToLowerInvariant()}_{l + 1}", a[l]);
            }
            foodFertileBonus = (int)Read(economy, "food_fertile_bonus", foodFertileBonus);
            fisherShoreCells = (int)Read(economy, "fisher_shore_cells", fisherShoreCells);
            foodInlandCells = (int)Read(economy, "food_inland_cells", foodInlandCells);
            tideWakeRadius = Read(economy, "tide_wake_radius", tideWakeRadius);
            tideShamanHeal = (int)Read(economy, "tide_shaman_heal", tideShamanHeal);
            tideShamanHealEverySeconds = Read(economy, "tide_shaman_heal_every_sec", tideShamanHealEverySeconds);
            tideShamanHealRadius = Read(economy, "tide_shaman_heal_radius", tideShamanHealRadius);
            kingXpRadius = Read(economy, "king_xp_radius", kingXpRadius);
            kingGuardRadius = Read(economy, "king_guard_radius", kingGuardRadius);
            kingRevealRadius = (int)Read(economy, "king_reveal_radius", kingRevealRadius);
            heroReturnWaitSeconds = Read(economy, "hero_return_wait_sec", heroReturnWaitSeconds);
            heroPatrolRadiusCells = ReadOptional(economy, "hero_patrol_radius_cells", heroPatrolRadiusCells);
            heroPatrolPauseSeconds = ReadOptional(economy, "hero_patrol_pause_sec", heroPatrolPauseSeconds);
            ruinRestoreCostFraction = ReadOptional(economy, "ruin_restore_cost_fraction", ruinRestoreCostFraction);
            ruinRestoreTimeFraction = ReadOptional(economy, "ruin_restore_time_fraction", ruinRestoreTimeFraction);
            heroSkills = ReadHeroSkills(economy);
            heroes = new List<HeroBalance>();
            foreach (var id in HeroIds) heroes.Add(ReadHero(economy, id));
            _heroUnits = null;
            _kingUnit = null;
            invasionHouses = (int)Read(economy, "inv_houses", invasionHouses);
            // the sandbox's own key first (its tab), else the run's
            personRespawnSeconds = ReadOptional(economy, "person_respawn_sec", Read(economy, "inv_person_respawn_sec", personRespawnSeconds));
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
            invasionLaneDetourMeters = Read(economy, "inv_lane_detour_meters", invasionLaneDetourMeters);
            invasionRaidPartyEvery = (int)Read(economy, "inv_raid_party_every", invasionRaidPartyEvery);
            invasionRaidPartyRaiders = (int)Read(economy, "inv_raid_party_raiders", invasionRaidPartyRaiders);
            invasionRaidPartyThrowers = (int)Read(economy, "inv_raid_party_throwers", invasionRaidPartyThrowers);
            invasionRaidPartyBaseRadius = Read(economy, "inv_raid_party_base_radius", invasionRaidPartyBaseRadius);
            invasionHealerHealShare = Read(economy, "inv_healer_heal_share", invasionHealerHealShare);
            invasionHealerHealSharePerLevel = Read(economy, "inv_healer_heal_share_per_level", invasionHealerHealSharePerLevel);
            nyGiftsPerWave = (int)Read(economy, "inv_ny_gifts_per_wave", nyGiftsPerWave);
            nyGiftsMax = (int)Read(economy, "inv_ny_gifts_max", nyGiftsMax);
            nyGiftMinRadius = Read(economy, "inv_ny_gift_min_radius", nyGiftMinRadius);
            nyGiftMaxRadius = Read(economy, "inv_ny_gift_max_radius", nyGiftMaxRadius);
            nyGiftPickupMeters = Read(economy, "inv_ny_gift_pickup_meters", nyGiftPickupMeters);
            nyGiftWood = (int)Read(economy, "inv_ny_gift_wood", nyGiftWood);
            nyGiftStone = (int)Read(economy, "inv_ny_gift_stone", nyGiftStone);
            nyGiftGold = (int)Read(economy, "inv_ny_gift_gold", nyGiftGold);
            nyThiefFromWave = (int)Read(economy, "inv_ny_thief_from_wave", nyThiefFromWave);
            nyThievesPerWave = (int)Read(economy, "inv_ny_thieves_per_wave", nyThievesPerWave);
            nyGoldThief = (int)Read(economy, "inv_gold_thief", nyGoldThief);
            nyBlizzardChance = Read(economy, "inv_ny_blizzard_chance", nyBlizzardChance);
            nyBlizzardSeconds = Read(economy, "inv_ny_blizzard_sec", nyBlizzardSeconds);
            nyBlizzardWarningSeconds = Read(economy, "inv_ny_blizzard_warning_sec", nyBlizzardWarningSeconds);
            nyBlizzardRangeMult = Read(economy, "inv_ny_blizzard_range_mult", nyBlizzardRangeMult);
            nyBlizzardSpeedMult = Read(economy, "inv_ny_blizzard_speed_mult", nyBlizzardSpeedMult);
            nyChimesEvery = (int)Read(economy, "inv_ny_chimes_every", nyChimesEvery);
            nyChimesOrcsMult = Read(economy, "inv_ny_chimes_orcs_mult", nyChimesOrcsMult);
            nySaluteDelaySeconds = Read(economy, "inv_ny_salute_delay_sec", nySaluteDelaySeconds);
            nySaluteDamageShare = Read(economy, "inv_ny_salute_damage_share", nySaluteDamageShare);
            nyShamanFreezeEverySeconds = Read(economy, "inv_ny_shaman_freeze_every_sec", nyShamanFreezeEverySeconds);
            nyShamanFreezeRadius = Read(economy, "inv_ny_shaman_freeze_radius", nyShamanFreezeRadius);
            nyShamanFreezeSeconds = Read(economy, "inv_ny_shaman_freeze_sec", nyShamanFreezeSeconds);
            nyGoldShaman = (int)Read(economy, "inv_gold_ice_shaman", nyGoldShaman);
            // the asset keeps the list it was first written with: a monster added since must be put in
            foreach (var id in MonsterIds)
            {
                if (!monsters.Exists(m => m.id == id)) monsters.Add(new MonsterStats { id = id });
            }
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
            invasionSoldierGuardRadius = Read(economy, "inv_soldier_guard_radius", invasionSoldierGuardRadius);
            orcMeleeReachMeters = Read(economy, "orc_melee_reach_meters", orcMeleeReachMeters);
            invasionOrcHealthMultiplier = Read(economy, "inv_orc_health_mult", invasionOrcHealthMultiplier);
            invasionOrcDamageMultiplier = Read(economy, "inv_orc_damage_mult", invasionOrcDamageMultiplier);
            invasionStoneReserveMultiplier = Read(economy, "inv_stone_reserve_mult", invasionStoneReserveMultiplier);
            invasionWoodPerHarvest = (int)Read(economy, "inv_wood_per_harvest", invasionWoodPerHarvest);
            invasionStonePerHarvest = (int)Read(economy, "inv_stone_per_harvest", invasionStonePerHarvest);
            minimapRadius = ReadOptional(economy, "minimap_radius", Read(economy, "inv_minimap_radius", minimapRadius));
            invasionCitizenHealth = (int)Read(economy, "inv_citizen_health", invasionCitizenHealth);
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
        private static HeroBalance ReadHero(Dictionary<string, float> economy, string id)
        {
            var k = id + "_";
            return new HeroBalance
            {
                id = id,
                maxHealth = (int)Read(economy, k + "max_health", 100f),
                attackDamage = (int)Read(economy, k + "attack_damage", 8f),
                attackIntervalSeconds = Read(economy, k + "attack_interval_sec", 1f),
                attackRange = Read(economy, k + "attack_range", 1.6f),
                walkSpeed = Read(economy, k + "walk_speed", 2.2f),
                engageRadius = Read(economy, k + "engage_radius", 8f),
                healthPerLevel = (int)Read(economy, k + "health_per_level", 20f),
                damagePerLevel = (int)Read(economy, k + "damage_per_level", 2f),
                // the King is no purchase: he has neither
                price = (int)ReadOptional(economy, k + "price", 0f),
                castleLevel = Mathf.Max(1, (int)ReadOptional(economy, k + "castle_level", 1f)),
                fleeRadius = ReadOptional(economy, k + "flee_radius", 0f),
                fleeDistance = ReadOptional(economy, k + "flee_distance", 0f),
            };
        }

        /// <summary>A key only some rows have (a skill's radius, a hero's price): silent when absent.</summary>
        private static float ReadOptional(Dictionary<string, float> economy, string key, float fallback) =>
            economy.TryGetValue(key, out var value) ? value : fallback;

        /// <summary>The skills are found, not listed: every skill_&lt;id&gt;_level row starts one.</summary>
        private static List<HeroSkillBalance> ReadHeroSkills(Dictionary<string, float> economy)
        {
            const string head = "skill_", tail = "_level";
            var ids = new List<string>();
            foreach (var key in economy.Keys)
            {
                if (key.StartsWith(head, StringComparison.Ordinal) && key.EndsWith(tail, StringComparison.Ordinal) && !key.EndsWith("_per_level", StringComparison.Ordinal))
                {
                    ids.Add(key.Substring(head.Length, key.Length - head.Length - tail.Length));
                }
            }
            ids.Sort(StringComparer.Ordinal);
            var skills = new List<HeroSkillBalance>(ids.Count);
            foreach (var id in ids)
            {
                var k = head + id + "_";
                skills.Add(new HeroSkillBalance
                {
                    id = id,
                    unlockLevel = Mathf.Max(1, (int)Read(economy, k + "level", 1f)),
                    cooldownSeconds = Read(economy, k + "cooldown_sec", 30f),
                    durationSeconds = Read(economy, k + "duration_sec", 0f),
                    durationPerLevel = Read(economy, k + "duration_per_level", 0f),
                    power = Read(economy, k + "power", 0f),
                    powerPerLevel = Read(economy, k + "power_per_level", 0f),
                    radius = ReadOptional(economy, k + "radius", 0f),
                    tier = (int)ReadOptional(economy, k + "tier", 0f),
                });
            }
            return skills;
        }

        private static float Read(Dictionary<string, float> economy, string key, float fallback)
        {
            if (economy.TryGetValue(key, out var value)) return value;

            Debug.LogError($"BalanceConfig: the balance sheet's economy tab has no key '{key}'. Keeping {fallback}.");
            return fallback;
        }
    }
}
