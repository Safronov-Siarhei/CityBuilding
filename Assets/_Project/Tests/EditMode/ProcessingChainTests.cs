using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Resources;
using CityBuilder.UI;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// The first chain of buildings that turns one resource into another: Ферма grows wheat, Ветряк
    /// mills it into flour, Пекарня bakes flour into bread.
    ///
    /// Two things are worth guarding here and they fail in completely different ways. The partial
    /// supply rule is arithmetic -- a mill with three hands and grain for two must mill two sacks,
    /// not none and not three. The chain's shape is spreadsheet data: nothing in the code says the
    /// bakery eats flour, and a single retyped cell in the sheet ("Food" instead of "Flour") would
    /// leave a bakery that consumes something nobody makes and quietly never produces again.
    /// </summary>
    public class ProcessingChainTests
    {
        [Test]
        public void EveryWorkerRuns_WhenThereIsInputForAllOfThem()
        {
            Assert.AreEqual(3, ProductionBuilding.SuppliedWorkers(assignedWorkers: 3, inputPerWorker: 2, inputInStore: 6));
        }

        /// <summary>The interesting case: a half-supplied workshop works at half strength instead of stopping dead or milling grain it doesn't have.</summary>
        [Test]
        public void OnlyTheSuppliedWorkersRun_WhenInputIsShort()
        {
            Assert.AreEqual(2, ProductionBuilding.SuppliedWorkers(assignedWorkers: 3, inputPerWorker: 2, inputInStore: 5));
        }

        [Test]
        public void NobodyRuns_WithoutEnoughInputForASingleWorker()
        {
            Assert.AreEqual(0, ProductionBuilding.SuppliedWorkers(assignedWorkers: 3, inputPerWorker: 2, inputInStore: 1));
            Assert.AreEqual(0, ProductionBuilding.SuppliedWorkers(assignedWorkers: 3, inputPerWorker: 2, inputInStore: 0));
        }

        /// <summary>A lumberjack's hut consumes nothing, and must never be limited by an empty store of it.</summary>
        [Test]
        public void ABuildingWithNoInput_AlwaysRunsAtFullStrength()
        {
            Assert.AreEqual(4, ProductionBuilding.SuppliedWorkers(assignedWorkers: 4, inputPerWorker: 0, inputInStore: 0));
        }

        [TestCase("Farm", ResourceType.Grain)]
        [TestCase("Windmill", ResourceType.Flour)]
        [TestCase("Baker", ResourceType.Bread)]
        public void TheChainProducesWhatTheDesignSays(string id, ResourceType produces)
        {
            var building = Building(id);
            Assert.AreEqual(produces, building.producesResource,
                $"{id} no longer produces {produces} -- check the 'produces' cell in the buildings tab.");
        }

        [TestCase("Windmill", ResourceType.Grain)]
        [TestCase("Baker", ResourceType.Flour)]
        public void TheConvertersEatTheLinkBeforeThem(string id, ResourceType consumes)
        {
            var building = Building(id);
            Assert.AreEqual(consumes, building.consumesResource,
                $"{id} no longer consumes {consumes} -- check the 'consumes' cell in the buildings tab.");

            foreach (var level in building.levels)
            {
                Assert.Greater(level.consumptionPerWorkerPerTick, 0,
                    $"{id} has a consumption of 0 at one of its levels, which turns it back into a building that makes {building.producesResource} out of nothing.");
                Assert.Greater(level.maxWorkers, 0, $"{id} has no worker slots, so nothing will ever run it.");
                Assert.Greater(level.productionPerWorkerPerTick, 0, $"{id} produces nothing per tick, so its input would simply vanish.");
            }
        }

        /// <summary>
        /// The Ферма's wheat has to be storable before the mill is worth building -- and wheat is
        /// the one resource in the chain whose storehouse is neither the Склад nor the Кладовая.
        /// </summary>
        [Test]
        public void TheBarnStoresWhatTheFarmGrows()
        {
            var barn = Building("Barn");
            Assert.AreEqual(ResourceStorage.GroupOf(ResourceType.Grain), barn.storageGroup);
        }

        /// <summary>A converter that produces less than it eats per worker is a building the player is punished for staffing.</summary>
        [TestCase("Baker")]
        public void BakingIsWorthDoing(string id)
        {
            var building = Building(id);
            var level = building.levels[0];

            Assert.GreaterOrEqual(level.productionPerWorkerPerTick, level.consumptionPerWorkerPerTick,
                $"{id} turns {level.consumptionPerWorkerPerTick} into {level.productionPerWorkerPerTick} -- the last link of a chain has to be worth more than its input.");
        }

        [Test]
        public void TheSummaryLine_NamesBothSidesOfAConversion()
        {
            var summary = BuildingInfoPanelController.ProductionSummary(
                ResourceType.Flour, 1, ResourceType.Grain, 2, 6f);

            StringAssert.Contains("мука", summary);
            StringAssert.Contains("пшеница", summary);
            StringAssert.Contains("2", summary);
        }

        /// <summary>A building with no input must not claim one -- "мука 1 из 0 дерево" is what a naive format string produces.</summary>
        [Test]
        public void TheSummaryLine_SaysNothingAboutInputWhenThereIsNone()
        {
            var summary = BuildingInfoPanelController.ProductionSummary(
                ResourceType.Wood, 2, ResourceType.Wood, 0, 6f);

            StringAssert.DoesNotContain(" из ", summary);
        }

        private static BuildingBalance Building(string id)
        {
            var config = UnityEngine.Resources.Load<BalanceConfig>(BalanceConfig.ResourcePath);
            Assert.IsNotNull(config, "No BalanceConfig asset -- rebuild it from the CSVs.");

            foreach (var building in config.Buildings)
            {
                if (building.id == id) return building;
            }

            Assert.Fail($"The buildings tab has no row with id '{id}'.");
            return null;
        }
    }
}
