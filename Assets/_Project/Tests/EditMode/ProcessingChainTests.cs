using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Resources;
using CityBuilder.UI;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// Recipes: what each building turns into what, and how much of it gets made when the store is
    /// short.
    ///
    /// Two very different things fail here. The batch arithmetic is code -- a furnace with ore for
    /// ten ingots and coal for two must smelt two, and the scarcest ingredient deciding is the part
    /// that is easy to get wrong. The chains themselves are spreadsheet data: nothing in the code
    /// says a bakery eats flour, and one retyped cell would leave a bakery consuming something
    /// nobody makes and quietly never producing again.
    /// </summary>
    public class ProcessingChainTests
    {
        [Test]
        public void EveryBatchRuns_WhenThereIsMaterialForAllOfThem()
        {
            Assert.AreEqual(3, ProductionBuilding.AffordableBatches(3, Inputs((ResourceType.Grain, 2)), new[] { 6 }));
        }

        /// <summary>The interesting case: a half-supplied workshop works at half strength instead of stopping dead or milling grain it does not have.</summary>
        [Test]
        public void OnlySomeBatchesRun_WhenMaterialIsShort()
        {
            Assert.AreEqual(2, ProductionBuilding.AffordableBatches(3, Inputs((ResourceType.Grain, 2)), new[] { 5 }));
        }

        [Test]
        public void NoBatchRuns_WithoutEnoughForASingleOne()
        {
            Assert.AreEqual(0, ProductionBuilding.AffordableBatches(3, Inputs((ResourceType.Grain, 2)), new[] { 1 }));
            Assert.AreEqual(0, ProductionBuilding.AffordableBatches(3, Inputs((ResourceType.Grain, 2)), new[] { 0 }));
        }

        /// <summary>The whole reason a recipe carries several inputs: the ore is useless without the coal to fire it, and the smaller of the two has to win.</summary>
        [Test]
        public void TheScarcestIngredientDecides()
        {
            var inputs = Inputs((ResourceType.Iron, 2), (ResourceType.Coal, 1));

            Assert.AreEqual(2, ProductionBuilding.AffordableBatches(10, inputs, new[] { 20, 2 }));
            Assert.AreEqual(3, ProductionBuilding.AffordableBatches(10, inputs, new[] { 6, 50 }));
        }

        /// <summary>A mine names no ingredients and must never be stopped by an empty store of something it never asked for.</summary>
        [Test]
        public void AGathererIsLimitedOnlyByItsWorkers()
        {
            Assert.AreEqual(4, ProductionBuilding.AffordableBatches(4, new List<ResourceAmount>(), new int[0]));
            Assert.AreEqual(4, ProductionBuilding.AffordableBatches(4, null, new int[0]));
        }

        [TestCase("Farm", ResourceType.Grain)]
        [TestCase("Windmill", ResourceType.Flour)]
        [TestCase("Baker", ResourceType.Bread)]
        [TestCase("CopperMine", ResourceType.CopperOre)]
        public void TheChainsProduceWhatTheDesignSays(string id, ResourceType produces)
        {
            var recipes = Building(id).recipes;
            Assert.AreEqual(1, recipes.Count, $"{id} is expected to know exactly one recipe.");
            Assert.AreEqual(produces, recipes[0].output, $"{id} no longer produces {produces} -- check its row in the recipes tab.");
        }

        [TestCase("Windmill", ResourceType.Grain)]
        [TestCase("Baker", ResourceType.Flour)]
        public void TheConvertersEatTheLinkBeforeThem(string id, ResourceType consumes)
        {
            var recipe = Building(id).recipes[0];

            Assert.IsTrue(recipe.HasInputs, $"{id} has no inputs, which turns it back into a building that makes {recipe.output} out of nothing.");
            Assert.AreEqual(consumes, recipe.inputs[0].type, $"{id} no longer consumes {consumes} -- check its row in the recipes tab.");
            Assert.Greater(recipe.inputs[0].amount, 0);
            Assert.Greater(recipe.outputAmount, 0, $"{id} produces nothing per batch, so its input would simply vanish.");
        }

        /// <summary>The furnace is the one building with a choice, and the reason recipes are a list at all.</summary>
        [Test]
        public void TheFurnaceOffersEveryMetal()
        {
            var recipes = Building("Smelter").recipes;
            Assert.AreEqual(3, recipes.Count, "The Плавильня should offer iron, copper and gold.");

            var outputs = new List<ResourceType>();
            foreach (var recipe in recipes)
            {
                Assert.IsTrue(recipe.HasInputs, $"Smelter recipe '{recipe.id}' smelts nothing into {recipe.output}.");
                Assert.IsNotEmpty(recipe.displayName, $"Smelter recipe '{recipe.id}' has no name, so its button would be blank.");
                outputs.Add(recipe.output);
            }

            CollectionAssert.AreEquivalent(
                new[] { ResourceType.IronBar, ResourceType.CopperBar, ResourceType.GoldBar }, outputs);
        }

        /// <summary>Coal was produced by a mine and consumed by nothing at all until the furnace needed firing.</summary>
        [Test]
        public void SmeltingBurnsCoal()
        {
            foreach (var recipe in Building("Smelter").recipes)
            {
                var burnsCoal = false;
                foreach (var input in recipe.inputs)
                {
                    if (input.type == ResourceType.Coal) burnsCoal = true;
                }
                Assert.IsTrue(burnsCoal, $"Smelter recipe '{recipe.id}' needs no coal, which leaves coal a resource nothing in the game consumes.");
            }
        }

        /// <summary>The user called both of these nonsense outright: a dock that mines gold, a water mill that grows food.</summary>
        [TestCase("Dock")]
        [TestCase("WaterMill")]
        public void TheWaterBuildingsProduceNothing(string id)
        {
            Assert.IsEmpty(Building(id).recipes, $"{id} produces something again -- it is meant to be a placeholder until the Водные are designed.");
        }

        /// <summary>Every ingredient has to be something a building somewhere actually makes, or the workshop that wants it can never run.</summary>
        [Test]
        public void EveryIngredientIsProducedBySomething()
        {
            var config = UnityEngine.Resources.Load<BalanceConfig>(BalanceConfig.ResourcePath);
            var produced = new HashSet<ResourceType>();
            foreach (var building in config.Buildings)
            {
                foreach (var recipe in building.recipes) produced.Add(recipe.output);
            }

            foreach (var building in config.Buildings)
            {
                foreach (var recipe in building.recipes)
                {
                    foreach (var input in recipe.inputs)
                    {
                        Assert.IsTrue(produced.Contains(input.type),
                            $"{building.id}'s recipe '{recipe.id}' needs {input.type}, which no building in the game produces.");
                    }
                }
            }
        }

        /// <summary>Smelted metal had no use whatsoever until the three iron-costing buildings were switched over to it -- a resource nothing spends is the dead end this change exists to remove.</summary>
        [Test]
        public void SmeltedMetalIsSpentOnSomething()
        {
            var config = UnityEngine.Resources.Load<BalanceConfig>(BalanceConfig.ResourcePath);

            var spenders = 0;
            foreach (var building in config.Buildings)
            {
                foreach (var amount in building.cost)
                {
                    if (amount.type == ResourceType.IronBar) spenders++;
                }
            }

            Assert.Greater(spenders, 0, "Nothing in the game costs a smelted bar, so the Плавильня produces something no one can use.");
        }

        [Test]
        public void ARecipeReadsAsAConversion()
        {
            var recipe = new BuildingRecipe
            {
                inputs = Inputs((ResourceType.Iron, 2), (ResourceType.Coal, 1)),
                output = ResourceType.IronBar,
                outputAmount = 1,
            };

            var described = recipe.Describe();
            StringAssert.Contains(ResourceNames.Of(ResourceType.Iron), described);
            StringAssert.Contains(ResourceNames.Of(ResourceType.Coal), described);
            StringAssert.Contains(ResourceNames.Of(ResourceType.IronBar), described);
            StringAssert.Contains("->", described);
        }

        /// <summary>A gatherer must not claim an input -- "-> 1 дерево" with nothing before the arrow is what a naive format string produces.</summary>
        [Test]
        public void AGathererReadsAsJustItsOutput()
        {
            var recipe = new BuildingRecipe { output = ResourceType.Wood, outputAmount = 2 };

            StringAssert.DoesNotContain("->", recipe.Describe());
        }

        [Test]
        public void TheSummaryLine_IsEmptyForABuildingThatDoesNoWork()
        {
            Assert.IsEmpty(BuildingInfoPanelController.ProductionSummary(null, 1, 6f));
            Assert.IsEmpty(BuildingInfoPanelController.ProductionSummary(new BuildingRecipe(), 0, 6f));
        }

        private static List<ResourceAmount> Inputs(params (ResourceType type, int amount)[] inputs)
        {
            var list = new List<ResourceAmount>();
            foreach (var (type, amount) in inputs)
            {
                list.Add(new ResourceAmount { type = type, amount = amount });
            }
            return list;
        }

        private static BuildingBalance Building(string id)
        {
            var config = UnityEngine.Resources.Load<BalanceConfig>(BalanceConfig.ResourcePath);
            Assert.IsNotNull(config, "No BalanceConfig asset -- rebuild it from the CSVs.");

            var building = config.Building(id);
            Assert.IsNotNull(building, $"The buildings tab has no row with id '{id}'.");
            return building;
        }
    }
}
