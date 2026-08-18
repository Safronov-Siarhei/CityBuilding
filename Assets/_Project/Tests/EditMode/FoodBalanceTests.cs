using CityBuilder.Core;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// Pins down the relationships the food economy was tuned around, not the numbers themselves --
    /// same reasoning as ArmyBalanceTests, and for a sharper reason here: for a while every food
    /// building out-produced the whole settlement's appetite by more than twenty times, which left
    /// hunger unreachable and the bread chain optional decoration. Nothing in the game said so; the
    /// numbers had to be multiplied out by hand to notice.
    ///
    /// Everything below is read out of the balance sheet, so a retune is checked against the rest
    /// of the sheet rather than against a number copied in here once.
    /// </summary>
    public class FoodBalanceTests
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

        private static float TicksPerDay(BuildingBalance building) =>
            building.productionIntervalSeconds > 0f ? Config.DayLengthSeconds / building.productionIntervalSeconds : 0f;

        /// <summary>Batches a fully staffed building gets through in a game day at level 1 -- workers x batches x ticks.</summary>
        private static float BatchesPerDay(BuildingBalance building)
        {
            var stats = building.levels[0];
            return stats.maxWorkers * stats.batchesPerWorkerPerTick * TicksPerDay(building);
        }

        /// <summary>What a fully staffed building puts into the stores in a day.</summary>
        private static float OutputPerDay(BuildingBalance building)
        {
            Assert.IsNotEmpty(building.recipes, $"{building.id} makes nothing -- check the recipes tab.");
            return BatchesPerDay(building) * building.recipes[0].outputAmount;
        }

        /// <summary>What a fully staffed building takes out of the stores in a day, of its first ingredient.</summary>
        private static float InputPerDay(BuildingBalance building)
        {
            Assert.IsNotEmpty(building.recipes[0].inputs, $"{building.id} consumes nothing, so it is a gatherer, not a converter.");
            return BatchesPerDay(building) * building.recipes[0].inputs[0].amount;
        }

        private static float MouthsFedBy(float foodPerDay) => foodPerDay / Config.FoodPerMouthPerDay;

        /// <summary>
        /// The single number the whole feature rests on: a hut of fishermen feeds a couple of
        /// houses, not the entire map. Stated in houses rather than in units of food because that
        /// is the decision the player actually makes -- another home, or the hands to feed it.
        /// </summary>
        [Test]
        public void OneFisherHut_FeedsOnlyACoupleOfHouses()
        {
            var housefuls = MouthsFedBy(OutputPerDay(Building("FisherHut"))) / Building("Hovel").levels[0].citizensGranted;

            Assert.GreaterOrEqual(housefuls, 1f, "A fully staffed hut that cannot even feed the house its fishermen came from makes food unplayable rather than tight.");
            Assert.LessOrEqual(housefuls, 3f, "One hut feeding more than three houses is how food stopped being a constraint at all -- hunger became unreachable and the bread chain decoration.");
        }

        /// <summary>
        /// The chain is three buildings, two researches and a barn; if it did not beat throwing the
        /// same hands into fishing huts, there would be no reason to ever build it.
        /// </summary>
        [Test]
        public void TheBreadChain_FeedsMorePeoplePerWorkerThanFishing()
        {
            var baker = Building("Baker");
            var windmill = Building("Windmill");
            var farm = Building("Farm");

            // Workers a matched chain needs to keep one full bakery in flour, and that mill in grain.
            var windmillsPerBakery = InputPerDay(baker) / OutputPerDay(windmill);
            var farmsPerMill = InputPerDay(windmill) / OutputPerDay(farm);
            var workers = baker.levels[0].maxWorkers
                          + windmillsPerBakery * windmill.levels[0].maxWorkers
                          + windmillsPerBakery * farmsPerMill * farm.levels[0].maxWorkers;

            var breadPerWorker = MouthsFedBy(OutputPerDay(baker)) / workers;
            var fishPerWorker = MouthsFedBy(OutputPerDay(Building("FisherHut"))) / Building("FisherHut").levels[0].maxWorkers;

            Assert.Greater(breadPerWorker, fishPerWorker,
                "Grain -> flour -> bread costs three buildings and two researches, so it has to feed more mouths per pair of hands than a fishing hut does.");
        }

        /// <summary>
        /// One farm, one mill, one bakery: the chain is meant to be readable as a straight line,
        /// not as a ratio the player has to work out on paper. Tolerant enough that a retune can
        /// move all three together, tight enough to catch one of them drifting alone.
        /// </summary>
        [Test]
        public void TheChain_IsOneFarmToOneMillToOneBakery()
        {
            Assert.AreEqual(1f, InputPerDay(Building("Windmill")) / OutputPerDay(Building("Farm")), 0.2f,
                "A mill should eat about exactly what one farm grows.");
            Assert.AreEqual(1f, InputPerDay(Building("Baker")) / OutputPerDay(Building("Windmill")), 0.2f,
                "A bakery should eat about exactly what one mill grinds.");
        }

        /// <summary>
        /// Storage has to hold more than a single day's meal, or a village would starve the moment
        /// its stores filled and its farms started tipping the surplus on the floor.
        /// </summary>
        [Test]
        public void FoodStores_HoldMoreThanTheGracePeriodsWorthOfMeals()
        {
            const int villagePopulation = 20;

            var stores = Config.BaseCapacityFood + Building("Granary").levels[0].storageCapacity;
            var daysCovered = stores / (villagePopulation * Config.FoodPerMouthPerDay);

            Assert.Greater(daysCovered, Config.HungryDaysBeforeDeaths,
                "A village with a granary must be able to bank more food than the grace period it gets before people start dying.");
        }
    }
}
