using System.Collections;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Combat;
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
    /// An easy start (the user, 2026-10-20: "у игрока должен быть лёгкий старт и достаток ресурсов
    /// на постройку нужных зданий, чтобы он сразу не падал в проблемы"): the opening stock pays for
    /// the first working town, and for the seven minutes the town settles in, no orc comes and
    /// nobody falls ill for want of a well.
    /// </summary>
    public class EasyStartTests
    {
        private const string MapId = "Map3";

        // What a first town needs before anything else: a sawmill, a quarry, food, water, a roof,
        // and a road to tie them together.
        private static readonly (string id, int count)[] StarterSet =
        {
            ("Sawmill", 1), ("Quarry", 1), ("FisherHut", 1), ("Well", 1), ("Hovel", 1), ("Road", 10),
        };

        [UnitySetUp]
        public IEnumerator NewGame()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            GameSessionIntent.SaveNameToLoad = null;
            GameSessionIntent.NewGameMapId = MapId;
            SceneManager.LoadScene("CityBuilder", LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);
            ModalGate.SetBlocked(false);
        }

        [UnityTest]
        public IEnumerator TheOpeningStockBuildsTheFirstTown()
        {
            var resources = ResourceManager.Instance;
            var config = BalanceConfig.Instance;
            Assert.AreEqual(config.StartWood, resources.GetAmount(ResourceType.Wood), "The opening wood is not the sheet's start_wood.");
            Assert.AreEqual(config.StartStone, resources.GetAmount(ResourceType.Stone), "The opening stone is not the sheet's start_stone.");
            Assert.AreEqual(config.StartFood, resources.GetAmount(ResourceType.Food), "The opening food is not the sheet's start_food.");
            Assert.AreEqual(config.StartGold, resources.GetAmount(ResourceType.Gold), "The opening gold is not the sheet's start_gold.");

            int wood = 0, stone = 0;
            foreach (var (id, count) in StarterSet)
            {
                var data = PlaytestWorld.Building(id);
                Assert.IsNotNull(data, $"No {id} in the building catalogue.");
                foreach (var cost in data.cost)
                {
                    if (cost.type == ResourceType.Wood) wood += cost.amount * count;
                    if (cost.type == ResourceType.Stone) stone += cost.amount * count;
                }
            }

            Assert.LessOrEqual(wood, resources.GetAmount(ResourceType.Wood), $"The first town costs {wood} wood and the player opens with less.");
            Assert.LessOrEqual(stone, resources.GetAmount(ResourceType.Stone), $"The first town costs {stone} stone and the player opens with less.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator NoRaidWhileTheTownSettlesIn()
        {
            var townHall = PlaytestWorld.Building(BuildingIds.TownHall);
            PlaytestWorld.Place(townHall, PlaytestWorld.FindFreeAreaNearTheMiddle(townHall.footprintSize));
            for (var frame = 0; frame < 120 && !OrcRaidManager.Instance.PortalsPlaced; frame++) yield return null;
            Assert.IsTrue(OrcRaidManager.Instance.PortalsPlaced, "The portals never opened.");

            Assert.GreaterOrEqual(OrcRaidManager.Instance.SecondsUntilNextRaid, BalanceConfig.Instance.SettlingInSeconds - 1f,
                "The first raid comes before the town has settled in.");
        }

        [UnityTest]
        public IEnumerator NobodyFallsIllWhileTheTownSettlesIn()
        {
            var townHall = PlaytestWorld.Building(BuildingIds.TownHall);
            PlaytestWorld.Place(townHall, PlaytestWorld.FindFreeAreaNearTheMiddle(townHall.footprintSize));
            var citizens = CitizenManager.Instance;
            citizens.SetPopulation(20);
            citizens.SetSickPopulation(0);

            // As dry and as hungry as a town gets: no well, and nothing to eat.
            ResourceManager.Instance.SetAmount(ResourceType.Food, 0);
            FoodConsumptionManager.Instance.FeedSettlement();
            yield return null;

            var migration = MigrationManager.Instance;
            migration.RestoreFromSave(0f, BalanceConfig.Instance.SettlingInSeconds);
            SicknessManager.Instance.PassDay();
            Assert.AreEqual(0, citizens.SickPopulation, "Someone fell ill while the town was still settling in.");

            // ...and the same day once it has settled in does put people in bed, so it was the
            // settling-in that kept them well, not a town too healthy to fall ill.
            migration.RestoreFromSave(0f, 0f);
            SicknessManager.Instance.PassDay();
            Assert.Greater(citizens.SickPopulation, 0, "A dry, hungry, settled town stayed well -- the check above proved nothing.");
        }
    }
}
