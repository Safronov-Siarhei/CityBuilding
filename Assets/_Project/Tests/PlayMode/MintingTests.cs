using System.Collections;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Resources;
using CityBuilder.Saving;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// Minting: the Плавильня turning gold bars into coins, in the running game.
    ///
    /// The data half is guarded in EditMode (ProcessingChainTests). This file exists for the half
    /// that reading the code cannot settle -- **that the generic production tick really can pay
    /// out a currency.** Coins had only ever arrived through TaxManager, which adds them directly;
    /// no recipe had ever named Coins as an output, so nothing had ever proved that a workshop
    /// could make money the same way it makes flour. It can, and this is the proof rather than the
    /// assumption.
    ///
    /// Two things it also pins down, both of which are the difference between a mechanic and a
    /// faucet:
    /// - the coins are CONVERTED, not conjured: the gold bars go away;
    /// - a full treasury stops the furnace instead of burning bars into money that lands on the
    ///   floor (ProductionBuilding refuses to convert into a store that cannot hold the output).
    /// </summary>
    public class MintingTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map3";

        private const int StockedBars = 200;
        private const int StockedCoal = 200;

        private readonly List<BuildingInstance> _placed = new List<BuildingInstance>();

        [UnitySetUp]
        public IEnumerator PrepareScene()
        {
            LogAssert.ignoreFailingMessages = true;

            Time.timeScale = 1f;
            GameSessionIntent.NewGameMapId = MapId;
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);

            ModalGate.SetBlocked(false);
            ResourceManager.Instance.SetInfiniteResources(false);

            // Tax off, or this suite is flaky rather than wrong. A game day is 120 s and the
            // waits below are about 24 s, so roughly one run in five would have a day boundary
            // fall inside the window -- TaxManager would drop its own coins into the treasury and
            // the batch arithmetic asserted below would come out short by exactly a day's tax.
            CityBuilder.Resources.TaxManager.Instance?.SetTaxRate(0);

            // A Town Hall first: CitizenVisualsManager resolves the town centre from it and spawns
            // nobody without one, and a furnace with no citizen to staff it mints nothing.
            Place("Castle");
            CitizenManager.Instance.SetPopulation(10);
            CityBuilder.Research.ResearchManager.Instance?.CompleteEverything();
            yield return null;
        }

        [TearDown]
        public void CleanUp()
        {
            ModalGate.SetBlocked(false);
            Time.timeScale = 1f;
            foreach (var building in _placed)
            {
                if (building != null) Object.Destroy(building.gameObject);
            }
            _placed.Clear();
        }

        [UnityTest]
        public IEnumerator TheFurnaceTurnsGoldBarsIntoCoins()
        {
            var smelter = Place("Smelter");
            var furnace = Staff(smelter);
            furnace.SelectRecipeById("coins");

            var recipe = furnace.SelectedRecipe;
            Assert.IsNotNull(recipe);
            Assert.AreEqual("coins", recipe.id, "The furnace did not switch to minting, so this measures smelting instead.");

            var resources = ResourceManager.Instance;
            // Headroom, so a full vault cannot be what this test ends up measuring. The ceiling is
            // per resource against the group's capacity, not a pool shared with the gold bars
            // stocked below -- but a settlement with no Сокровищница holds only 300 of each, and
            // a retuned rate could reach that inside the two ticks waited for.
            resources.AddCapacity(ResourceStorageGroup.Valuables, 5000);
            resources.SetAmount(ResourceType.GoldBar, StockedBars);
            resources.SetAmount(ResourceType.Coal, StockedCoal);
            resources.SetAmount(ResourceType.Coins, 0);

            var barsBefore = resources.GetAmount(ResourceType.GoldBar);

            // Long enough for two ticks, so a single missed frame at the boundary cannot decide
            // the result.
            yield return new WaitForSeconds(smelter.Data.productionIntervalSeconds * 2f + 0.2f);

            var coins = resources.GetAmount(ResourceType.Coins);
            var barsAfter = resources.GetAmount(ResourceType.GoldBar);

            Assert.Greater(coins, 0, "The furnace was set to mint, had bars and coal, and produced no coins at all -- a recipe whose output is a currency does not reach the treasury.");
            Assert.Less(barsAfter, barsBefore, "Coins appeared without any gold bar being spent, which makes minting a faucet rather than a conversion.");

            // The rate itself lives in the sheet; what is asserted here is that the two sides move
            // together in the proportion the recipe states, whatever that proportion is retuned to.
            var barsSpent = barsBefore - barsAfter;
            var expected = barsSpent / InputAmount(recipe, ResourceType.GoldBar) * recipe.outputAmount;
            Assert.AreEqual(expected, coins, $"{barsSpent} gold bars became {coins} coins, which is not what the recipe says a batch is worth.");
        }

        [UnityTest]
        public IEnumerator AFullTreasuryStopsTheFurnaceInsteadOfBurningBars()
        {
            var smelter = Place("Smelter");
            var furnace = Staff(smelter);
            furnace.SelectRecipeById("coins");

            var resources = ResourceManager.Instance;
            resources.SetAmount(ResourceType.Coal, StockedCoal);
            resources.SetAmount(ResourceType.GoldBar, StockedBars);

            // Fill what is left of the vault with coins, so there is nowhere for a minted one to go.
            var capacity = resources.GetCapacity(ResourceType.Coins);
            resources.SetAmount(ResourceType.Coins, capacity);

            var barsBefore = resources.GetAmount(ResourceType.GoldBar);
            yield return new WaitForSeconds(smelter.Data.productionIntervalSeconds * 2f + 0.2f);

            Assert.AreEqual(barsBefore, resources.GetAmount(ResourceType.GoldBar),
                "The furnace melted gold bars into a vault that could not hold the coins -- the bars are simply gone, and the player has no way to see why.");
        }

        private static int InputAmount(BuildingRecipe recipe, ResourceType type)
        {
            foreach (var input in recipe.inputs)
            {
                if (input.type == type) return input.amount;
            }
            Assert.Fail($"The minting recipe no longer takes {type}.");
            return 1;
        }

        private static ProductionBuilding Staff(BuildingInstance building)
        {
            var production = building.GetComponent<ProductionBuilding>();
            Assert.IsNotNull(production, $"{building.Data.buildingName} has no worker slots.");
            Assert.IsTrue(production.TryAssignWorker(), "Could not put a worker on the furnace.");
            return production;
        }

        private BuildingInstance Place(string id)
        {
            var data = PlaytestWorld.Building(id);
            Assert.IsNotNull(data, $"No {id} in the building catalogue.");

            var cell = PlaytestWorld.FindFreeArea(data.footprintSize);
            Assert.AreNotEqual(new Vector2Int(-1, -1), cell, $"Nowhere free to put a {id}.");

            var instance = PlaytestWorld.Place(data, cell);
            _placed.Add(instance);
            return instance;
        }
    }
}
