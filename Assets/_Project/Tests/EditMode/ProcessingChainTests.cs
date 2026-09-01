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
        public void TheFurnaceOffersEveryMetalAndTheMint()
        {
            var recipes = Building("Smelter").recipes;
            Assert.AreEqual(4, recipes.Count, "The Плавильня should offer iron, copper, gold and the mint.");

            var outputs = new List<ResourceType>();
            foreach (var recipe in recipes)
            {
                Assert.IsTrue(recipe.HasInputs, $"Smelter recipe '{recipe.id}' smelts nothing into {recipe.output}.");
                Assert.IsNotEmpty(recipe.displayName, $"Smelter recipe '{recipe.id}' has no name, so its button would be blank.");
                outputs.Add(recipe.output);
            }

            CollectionAssert.AreEquivalent(
                new[] { ResourceType.IronBar, ResourceType.CopperBar, ResourceType.GoldBar, ResourceType.Coins }, outputs);
        }

        /// <summary>
        /// Minting: the gold bar's only destination, and the only source of coins that is not tax.
        ///
        /// Both halves matter. **A gold bar was spent on nothing at all** -- the Плавильня made
        /// them and no building, upgrade or unit ever asked for one, so the whole gold branch was
        /// a dead end that produced a trophy. And **tax was the only tap coins ever came out of**,
        /// at 0,5 per citizen per day and paid for directly out of happiness, which put the full
        /// research tree 860 game days away.
        ///
        /// The furnace runs ONE recipe at a time, which is what stops this being free money: a
        /// settlement minting coins is a settlement not making iron.
        /// </summary>
        [Test]
        public void TheFurnaceMintsCoinsOutOfGoldBars()
        {
            var mint = FindRecipe("Smelter", "coins");
            Assert.IsNotNull(mint, "The Плавильня has no 'coins' recipe -- check the recipes tab.");

            Assert.AreEqual(ResourceType.Coins, mint.output);
            Assert.Greater(mint.outputAmount, 0);

            var eatsGoldBars = false;
            foreach (var input in mint.inputs)
            {
                if (input.type == ResourceType.GoldBar) eatsGoldBars = true;
            }
            Assert.IsTrue(eatsGoldBars, "Minting no longer consumes gold bars, which leaves GoldBar a resource nothing in the game spends.");
        }

        /// <summary>
        /// The gold bar has somewhere to go. It is checked on its own because the other two metals
        /// are spent OUTSIDE the recipes tab -- a spearman costs an iron bar and an archer a copper
        /// one (`recruit_ironbar`/`recruit_copperbar` on the units tab) -- while gold was spent
        /// nowhere at all, in any tab, until minting. That is the exact shape of the dead end this
        /// guards: a building that faithfully produces a trophy.
        /// </summary>
        [Test]
        public void TheGoldBarIsSpentOnSomething()
        {
            var config = UnityEngine.Resources.Load<BalanceConfig>(BalanceConfig.ResourcePath);
            foreach (var building in config.Buildings)
            {
                foreach (var recipe in building.recipes)
                {
                    foreach (var input in recipe.inputs)
                    {
                        if (input.type == ResourceType.GoldBar) return;
                    }
                }
            }
            Assert.Fail("Nothing consumes GoldBar. The Плавильня is back to producing a trophy, and the whole gold branch with it.");
        }

        private static BuildingRecipe FindRecipe(string buildingId, string recipeId)
        {
            foreach (var recipe in Building(buildingId).recipes)
            {
                if (recipe.id == recipeId) return recipe;
            }
            return null;
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

        /// <summary>
        /// The batch multiplier is separated from the recipe by exactly one space, and the space
        /// comes from the CODE rather than from the sheet.
        ///
        /// It used to be the first character of `#recipe_batches` (" x{0}"), because the summary
        /// concatenates the two directly. That put a load-bearing space at the front of a
        /// spreadsheet cell, where it is invisible to everyone who edits the sheet -- and Sheets
        /// had already dropped it, so the repo said " x{0}" and the sheet said "x{0}" and the next
        /// Pull would have rendered "Мукаx2". Found by diffing the published tabs against the repo,
        /// not by anyone looking at the game.
        ///
        /// So the assertion is about the SPACING, which is the thing that broke, rather than about
        /// the wording, which the sheet is allowed to change.
        /// </summary>
        [Test]
        public void TheBatchMultiplier_IsSpacedAwayFromTheRecipe()
        {
            var recipe = new BuildingRecipe { output = ResourceType.Flour, outputAmount = 1 };
            var described = recipe.Describe();
            var multiplier = Localization.Format("#recipe_batches", 3);

            var several = BuildingInfoPanelController.ProductionSummary(recipe, 3, 6f);

            StringAssert.Contains(described + " " + multiplier, several,
                "The batch multiplier is stuck to the end of the recipe -- \"Мукаx3\". The space " +
                "between them is added by ProductionSummary; it is not the sheet's job.");
            StringAssert.DoesNotContain(described + "  " + multiplier.Trim(), several,
                "Two spaces before the multiplier -- the sheet's string has grown its leading " +
                "space back, on top of the one the code now adds.");

            // A single batch says nothing at all rather than "x1".
            var one = BuildingInfoPanelController.ProductionSummary(recipe, 1, 6f);
            StringAssert.DoesNotContain(Localization.Format("#recipe_batches", 1), one,
                "A building running one batch per worker is advertising a multiplier of one.");
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
