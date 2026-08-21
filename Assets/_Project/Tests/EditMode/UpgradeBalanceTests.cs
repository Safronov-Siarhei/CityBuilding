using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Resources;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// What an upgrade has to be WORTH, checked as relationships rather than as numbers -- the same
    /// reasoning as FoodBalanceTests, and written after the whole upgrade economy turned out to be
    /// dead on arrival.
    ///
    /// The hole it closes: `batches_per_tick_2/_3` were empty for every building in the game, so a
    /// producer made exactly as much per worker at level 3 as at level 1. An upgrade was a more
    /// expensive way to hire one more person -- a second building cost less, employed the same
    /// hands and produced the same amount, so upgrading was strictly dominated for all eleven
    /// producers. Four mines gained literally nothing but hit points. None of it was visible in any
    /// number the sheet showed, because payback_days only ever described level 1.
    ///
    /// The design these assertions pin down: sprawl stays cheaper in RESOURCES, upgrading is
    /// cheaper in PEOPLE. People are the scarce thing -- migration is gated on contentment, not on
    /// timber -- so that is what makes an upgrade worth paying for.
    /// </summary>
    public class UpgradeBalanceTests
    {
        private static BalanceConfig Config
        {
            get
            {
                var config = UnityEngine.Resources.Load<BalanceConfig>(BalanceConfig.ResourcePath);
                Assert.IsNotNull(config, "No BalanceConfig asset -- rebuild it from the CSVs.");
                return config;
            }
        }

        private static BuildingBalance Building(string id)
        {
            var building = Config.Building(id);
            Assert.IsNotNull(building, $"The buildings tab has no row with id '{id}'.");
            return building;
        }

        /// <summary>
        /// A gatherer's income is the walk, not the tick: ProductionBuilding.Update returns early
        /// for wood and stone, so batches_per_tick is never read for them and cannot be what their
        /// upgrade buys.
        /// </summary>
        private static bool IsGatherer(BuildingBalance building) =>
            building.recipes.Count > 0 &&
            (building.recipes[0].output == ResourceType.Wood || building.recipes[0].output == ResourceType.Stone);

        private static IEnumerable<BuildingBalance> Producers()
        {
            foreach (var building in Config.Buildings)
            {
                if (building.recipes.Count > 0 && building.levels.Count == BuildingInstance.MaxLevel) yield return building;
            }
        }

        [Test]
        public void EveryWorkshop_MakesMorePerWorkerAtEveryLevel()
        {
            var checkedAny = false;
            foreach (var building in Producers())
            {
                if (IsGatherer(building)) continue;
                checkedAny = true;

                for (var level = 1; level < BuildingInstance.MaxLevel; level++)
                {
                    var before = building.levels[level - 1].batchesPerWorkerPerTick;
                    var after = building.levels[level].batchesPerWorkerPerTick;
                    Assert.Greater(after, before,
                        $"{building.id} gets through no more batches per worker at level {level + 1} than at level {level}. " +
                        "An upgrade that only adds worker slots is a more expensive way to hire somebody -- a second building would do the same for less.");
                }
            }
            Assert.IsTrue(checkedAny, "No workshops found at all -- the recipes tab is not reaching the config.");
        }

        [Test]
        public void EveryGatherer_GainsHandsOrReachAtEveryLevel()
        {
            var checkedAny = false;
            foreach (var building in Producers())
            {
                if (!IsGatherer(building)) continue;
                checkedAny = true;

                for (var level = 1; level < BuildingInstance.MaxLevel; level++)
                {
                    var before = building.levels[level - 1];
                    var after = building.levels[level];
                    Assert.IsTrue(after.maxWorkers > before.maxWorkers || after.harvestRadius > before.harvestRadius,
                        $"{building.id} sends out no more gatherers and no further at level {level + 1} than at level {level}. " +
                        "Batches are never read for a gatherer, so hands and reach are the only things its upgrade can buy.");
                }
            }
            Assert.IsTrue(checkedAny, "No gatherers found -- Sawmill and Quarry should both be here.");
        }

        [Test]
        public void EveryServiceBuilding_ReachesFurtherAtEveryLevel()
        {
            var checkedAny = false;
            foreach (var building in Config.Buildings)
            {
                if (building.levels.Count != BuildingInstance.MaxLevel || building.levels[0].serviceRadius <= 0) continue;
                checkedAny = true;

                for (var level = 1; level < BuildingInstance.MaxLevel; level++)
                {
                    Assert.Greater(building.levels[level].serviceRadius, building.levels[level - 1].serviceRadius,
                        $"{building.id} serves exactly the same circle at level {level + 1} as at level {level}.");
                }
            }
            Assert.IsTrue(checkedAny, "No building serves a radius at all -- the well, the healer and the fire brigade should.");
        }

        /// <summary>
        /// The trade itself, stated in both directions on the building the whole grain chain starts
        /// from. Either half slipping turns the decision back into a non-decision: cheaper in
        /// resources AND in people would make upgrading strictly right, dearer in both would make
        /// it strictly wrong -- which is exactly what it was.
        /// </summary>
        [Test]
        public void UpgradingCostsMoreTimberThanSprawlAndFewerPeople()
        {
            var farm = Building("Farm");
            var ticks = Config.DayLengthSeconds / farm.productionIntervalSeconds;
            var output = farm.recipes[0].outputAmount;

            float PerDay(int level) => farm.levels[level - 1].maxWorkers * farm.levels[level - 1].batchesPerWorkerPerTick * output * ticks;
            float PerWorker(int level) => PerDay(level) / farm.levels[level - 1].maxWorkers;

            var buildCost = Total(farm.cost);
            var upgradeCost = Total(farm.upgradeToLevel2Cost);

            Assert.Greater(upgradeCost / (PerDay(2) - PerDay(1)), buildCost / PerDay(1),
                "Upgrading a farm buys grain more cheaply than building another one does -- then there is never a reason to spread out, and the map stops mattering.");
            Assert.Greater(PerWorker(2), PerWorker(1),
                "Upgrading a farm does not raise what one pair of hands brings in -- then there is never a reason to upgrade, which is the state this whole test file was written for.");
        }

        private static int Total(List<ResourceAmount> amounts)
        {
            var total = 0;
            foreach (var amount in amounts) total += amount.amount;
            return total;
        }
    }
}
