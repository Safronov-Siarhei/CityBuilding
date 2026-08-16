using CityBuilder.Core;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// Guards the balance sheet itself. Once numbers are authored in a spreadsheet, the ways they
    /// break change: not "someone wrote a bad line of code" but "a cell got emptied, a decimal comma
    /// slipped in, a row was renamed, the CSV never got re-imported". None of those produce a
    /// compile error, and most of them produce a game that boots and then behaves like a ghost.
    ///
    /// So this checks that the asset is real, that every unit the code asks for exists in it, and
    /// that nothing landed in a range that would quietly break the game (a zero interval divides
    /// time into infinite swings, zero health kills a unit on spawn). It does NOT assert specific
    /// values -- that's what the sheet is for.
    /// </summary>
    public class BalanceConfigTests
    {
        [Test]
        public void ConfigAsset_Exists()
        {
            var config = UnityEngine.Resources.Load<BalanceConfig>(BalanceConfig.ResourcePath);

            Assert.IsNotNull(config,
                $"No BalanceConfig at Resources/{BalanceConfig.ResourcePath}. Rebuild it from the CSVs " +
                "(CityBuilder/Balance/Rebuild Config From CSV, or any full SetupProject run) -- without it " +
                "the game silently falls back to the values hardcoded in BalanceConfig.");
        }

        [TestCase("militia")]
        [TestCase("orc")]
        public void UnitsTab_HasEveryUnitTheCodeAsksFor(string id)
        {
            var config = UnityEngine.Resources.Load<BalanceConfig>(BalanceConfig.ResourcePath);
            Assert.IsNotNull(config);

            var found = false;
            foreach (var unit in config.Units)
            {
                if (unit.id != id) continue;
                found = true;
                break;
            }

            Assert.IsTrue(found, $"The units tab has no row with id '{id}'.");
        }

        [Test]
        public void EveryUnit_HasNumbersThatCanBeFoughtWith()
        {
            var config = UnityEngine.Resources.Load<BalanceConfig>(BalanceConfig.ResourcePath);
            Assert.IsNotNull(config);
            Assert.IsNotEmpty(config.Units, "The units tab imported as empty -- most likely the CSV lost its rows or its header.");

            foreach (var unit in config.Units)
            {
                Assert.Greater(unit.maxHealth, 0, $"{unit.id}: zero health dies the frame it spawns.");
                Assert.Greater(unit.attackDamage, 0, $"{unit.id}: zero damage can never finish a fight.");
                Assert.Greater(unit.attackIntervalSeconds, 0f, $"{unit.id}: a zero attack interval swings every frame.");
                Assert.Greater(unit.walkSpeed, 0f, $"{unit.id}: zero speed never reaches anything.");
                Assert.Greater(unit.attackRangeUnits, 0f, $"{unit.id}: zero reach can never land a hit.");
                Assert.GreaterOrEqual(unit.attackRangeStructures, unit.attackRangeUnits,
                    $"{unit.id}: reach against a structure should be at least the melee reach -- a building's transform sits at its centre.");
                Assert.GreaterOrEqual(unit.engageRadius, unit.attackRangeUnits,
                    $"{unit.id}: a unit that engages from closer than it can hit will walk up and stare.");
                Assert.IsNotEmpty(unit.displayName, $"{unit.id}: an empty display name shows up as a blank row in the army panel.");
            }
        }

        [TestCase("Hovel")]
        [TestCase("Cottage")]
        [TestCase("Manor")]
        [TestCase("TownSquare")]
        [TestCase("Flag")]
        [TestCase("DecTree")]
        [TestCase("DecBush")]
        [TestCase("DecGarden")]
        [TestCase("TownHall")]
        [TestCase("FisherHut")]
        [TestCase("Farm")]
        [TestCase("Sawmill")]
        [TestCase("Quarry")]
        [TestCase("IronMine")]
        [TestCase("CoalMine")]
        [TestCase("Warehouse")]
        [TestCase("Barn")]
        [TestCase("Treasury")]
        [TestCase("BigWarehouse")]
        [TestCase("BigBarn")]
        [TestCase("BigTreasury")]
        [TestCase("Tower")]
        [TestCase("Barracks")]
        [TestCase("Gate")]
        [TestCase("FortifiedTower")]
        [TestCase("FortifiedGate")]
        [TestCase("Road")]
        [TestCase("Bridge")]
        [TestCase("WaterMill")]
        [TestCase("Dock")]
        public void BuildingsTab_HasEveryBuildingTheCodeAsksFor(string id)
        {
            var config = UnityEngine.Resources.Load<BalanceConfig>(BalanceConfig.ResourcePath);
            Assert.IsNotNull(config);

            Assert.IsNotNull(config.Building(id),
                $"The buildings tab has no row with id '{id}', so SetupProject generates it with placeholder " +
                "numbers. A renamed or deleted row looks exactly like this.");
        }

        [Test]
        public void EveryBuilding_HasNumbersThatCanBeBuiltWith()
        {
            var config = UnityEngine.Resources.Load<BalanceConfig>(BalanceConfig.ResourcePath);
            Assert.IsNotNull(config);
            Assert.IsNotEmpty(config.Buildings, "The buildings tab imported as empty -- most likely the CSV lost its rows or its header.");

            foreach (var building in config.Buildings)
            {
                Assert.IsNotEmpty(building.displayName, $"{building.id}: an empty display name shows up as a blank hotbar tooltip.");
                Assert.Greater(building.productionIntervalSeconds, 0f, $"{building.id}: a zero production interval ticks every frame.");
                Assert.GreaterOrEqual(building.fogRevealRadius, 0, $"{building.id}: negative reveal radius.");
                Assert.AreEqual(3, building.levels.Count, $"{building.id}: every building needs stats for all three upgrade levels.");

                for (var level = 1; level <= building.levels.Count; level++)
                {
                    var stats = building.levels[level - 1];
                    Assert.Greater(stats.maxHealth, 0, $"{building.id} lvl {level}: zero health collapses the moment it's placed.");
                    Assert.GreaterOrEqual(stats.defense, 0, $"{building.id} lvl {level}: negative defence would heal the raiders.");
                    Assert.GreaterOrEqual(stats.maxWorkers, 0, $"{building.id} lvl {level}: negative worker slots.");

                    if (stats.productionPerWorkerPerTick > 0)
                    {
                        Assert.Greater(stats.maxWorkers, 0,
                            $"{building.id} lvl {level}: produces something but has no worker slots, so it can never produce anything.");
                    }
                }

                foreach (var amount in building.cost)
                {
                    Assert.Greater(amount.amount, 0, $"{building.id}: a cost entry of {amount.amount} {amount.type} should simply be absent.");
                }
            }
        }

        /// <summary>
        /// The prerequisite column is the one place in the buildings tab that points at another row,
        /// so it's the one that can dangle: a typo there silently costs the building its requirement.
        /// </summary>
        [Test]
        public void BuildingPrerequisites_PointAtRealBuildings()
        {
            var config = UnityEngine.Resources.Load<BalanceConfig>(BalanceConfig.ResourcePath);
            Assert.IsNotNull(config);

            foreach (var building in config.Buildings)
            {
                if (string.IsNullOrEmpty(building.requiredBuildingId)) continue;

                Assert.AreNotEqual(building.id, building.requiredBuildingId,
                    $"{building.id} requires itself, which can never be satisfied.");
                Assert.IsNotNull(config.Building(building.requiredBuildingId),
                    $"{building.id} requires '{building.requiredBuildingId}', which is not a row in the buildings tab.");
            }
        }

        /// <summary>
        /// An upgrade costs real resources, so it must never hand back a weaker building. Catches
        /// the obvious sheet slip -- typing a level-2 value into the level-3 column, or leaving a
        /// decimal point out of one cell in a row of three.
        /// </summary>
        [Test]
        public void UpgradingNeverMakesABuildingWorse()
        {
            var config = UnityEngine.Resources.Load<BalanceConfig>(BalanceConfig.ResourcePath);
            Assert.IsNotNull(config);

            foreach (var building in config.Buildings)
            {
                for (var level = 2; level <= building.levels.Count; level++)
                {
                    var previous = building.levels[level - 2];
                    var current = building.levels[level - 1];

                    Assert.GreaterOrEqual(current.maxHealth, previous.maxHealth, $"{building.id}: level {level} is less sturdy than level {level - 1}.");
                    Assert.GreaterOrEqual(current.defense, previous.defense, $"{building.id}: level {level} defends worse than level {level - 1}.");
                    Assert.GreaterOrEqual(current.citizensGranted, previous.citizensGranted, $"{building.id}: level {level} houses fewer people than level {level - 1}.");
                    Assert.GreaterOrEqual(current.maxWorkers, previous.maxWorkers, $"{building.id}: level {level} employs fewer people than level {level - 1}.");
                    Assert.GreaterOrEqual(current.productionPerWorkerPerTick, previous.productionPerWorkerPerTick, $"{building.id}: level {level} produces less than level {level - 1}.");
                }
            }
        }

        /// <summary>Upgrades are supposed to get steeper, not cheaper -- the kind of thing a mistyped formula in the sheet inverts without looking wrong.</summary>
        [Test]
        public void UpgradeCosts_RiseWithLevel()
        {
            var config = UnityEngine.Resources.Load<BalanceConfig>(BalanceConfig.ResourcePath);
            Assert.IsNotNull(config);

            foreach (var building in config.Buildings)
            {
                var level2 = 0;
                foreach (var amount in building.upgradeToLevel2Cost) level2 += amount.amount;

                var level3 = 0;
                foreach (var amount in building.upgradeToLevel3Cost) level3 += amount.amount;

                Assert.GreaterOrEqual(level3, level2,
                    $"{building.id}: reaching level 3 costs {level3} resources in total, less than the {level2} level 2 costs.");
            }
        }

        /// <summary>
        /// A storehouse has to declare BOTH what it keeps and how much, or it is a building that
        /// costs resources and does nothing. The two halves live in different columns, so it is
        /// entirely possible to fill in one and forget the other.
        /// </summary>
        [Test]
        public void StorehousesDeclareBothWhatTheyKeepAndHowMuch()
        {
            var config = UnityEngine.Resources.Load<BalanceConfig>(BalanceConfig.ResourcePath);
            Assert.IsNotNull(config);

            var storehouses = 0;
            foreach (var building in config.Buildings)
            {
                var capacity = building.levels[0].storageCapacity;
                var stores = building.storageGroup != CityBuilder.Resources.ResourceStorageGroup.None;

                if (stores)
                {
                    storehouses++;
                    Assert.Greater(capacity, 0, $"{building.id}: says it stores {building.storageGroup} but holds nothing.");
                }
                else
                {
                    Assert.AreEqual(0, capacity, $"{building.id}: has storage capacity but no storage_group, so it holds nothing in practice.");
                }
            }

            Assert.Greater(storehouses, 0, "No building stores anything -- every resource would be stuck at the settlement's base capacity forever.");
        }

        [Test]
        public void BaseCapacities_LeaveRoomForTheStartingResources()
        {
            var config = BalanceConfig.Instance;

            Assert.Greater(config.BaseCapacityMaterials, 0, "A zero materials ceiling means the first tree felled is wasted.");
            Assert.Greater(config.BaseCapacityFood, 0);
            Assert.Greater(config.BaseCapacityValuables, 0);
        }

        [Test]
        public void Economy_IsInsideSaneRanges()
        {
            var config = BalanceConfig.Instance;

            Assert.Greater(config.DayLengthSeconds, 0f, "A zero-length day fires every day-based system every frame.");
            Assert.Greater(config.ArmyMaxSize, 0);
            Assert.Greater(config.PortalMaxHealth, 0, "A portal with no health is destroyed by the first swing, ending the map instantly.");
            Assert.Greater(config.RaidIntervalSeconds, 0f, "A zero raid interval spawns a squad every frame.");
            Assert.GreaterOrEqual(config.RaidMaxSize, config.RaidBaseSize, "The raid ceiling can't be below the opening squad.");
            Assert.Greater(config.DefenceAttackIntervalSeconds, 0f);
            Assert.GreaterOrEqual(config.CoinsPerCitizenPerDayAtMaxTax, 0f);
            Assert.Greater(config.DecayPerDayAtLevel1, 0f, "Zero decay per day quietly disables the whole decay/repair loop.");
            Assert.That(config.DecayPenaltyThreshold, Is.InRange(0f, 1f), "Decay is a 0..1 fraction.");
            Assert.That(config.MinDecayProductionMultiplier, Is.InRange(0f, 1f), "A decayed building produces LESS, never more.");
            Assert.That(config.RepairCostFraction, Is.InRange(0f, 1f), "Repair is meant to be cheaper than rebuilding.");
            Assert.Greater(config.WoodPerTree, 0);
            Assert.Greater(config.StonePerRock, 0);
        }
    }
}
