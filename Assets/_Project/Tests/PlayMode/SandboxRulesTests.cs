using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Resources;
using CityBuilder.Saving;
using CityBuilder.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// What the sandbox took over from the Invasion mode (the user, 2026-10-25): buildings and
    /// upgrades on a timer, mending priced by damage and no decay, people made by the houses (not
    /// the Town Hall) instead of migration, all three heroes bought in the Town Hall for coins, the
    /// run's building card -- and that a save keeps the half-built buildings and the heroes.
    /// </summary>
    public class SandboxRulesTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map3";
        private const string SaveName = "PlayModeSandboxRules";

        private static bool _sceneLoaded;
        private readonly List<BuildingInstance> _placed = new List<BuildingInstance>();

        [UnitySetUp]
        public IEnumerator PrepareScene()
        {
            LogAssert.ignoreFailingMessages = true;
            if (!_sceneLoaded)
            {
                yield return StartGame(null);
                _sceneLoaded = true;
            }
            ModalGate.SetBlocked(false);
            Time.timeScale = 1f;
            ResourceManager.Instance.SetInfiniteResources(false);
            Research.ResearchManager.Instance?.CompleteEverything();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Clear()
        {
            Time.timeScale = 1f;
            foreach (var building in _placed)
            {
                if (building != null) Object.DestroyImmediate(building.gameObject);
            }
            _placed.Clear();
            SaveSystem.Delete(SaveName);
            yield return null;
        }

        private static IEnumerator StartGame(string saveNameToLoad)
        {
            Time.timeScale = 1f;
            GameSessionIntent.SaveNameToLoad = saveNameToLoad;
            GameSessionIntent.NewGameMapId = MapId;
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);
            yield return null;
            ModalGate.SetBlocked(false);
            if (Combat.OrcRaidManager.Instance != null) Combat.OrcRaidManager.Instance.RaidsSuspended = true;
        }

        private BuildingInstance PlaceFinished(string id)
        {
            var data = PlaytestWorld.Building(id);
            var building = PlaytestWorld.Place(data, PlaytestWorld.FindFreeAreaNearTheMiddle(data.footprintSize));
            _placed.Add(building);
            return building;
        }

        private BuildingInstance TownHall()
        {
            if (BuildingInstance.TownHall != null) return BuildingInstance.TownHall;
            var castle = PlaceFinished(BuildingIds.TownHall);
            Object.FindAnyObjectByType<BuildingPlacer>().MarkMandatoryBuildingAlreadyPlaced();
            return castle;
        }

        [UnityTest]
        public IEnumerator ABuildingAndItsUpgrade_TakeTheSandboxTabsTime()
        {
            TownHall();
            var placer = Object.FindAnyObjectByType<BuildingPlacer>();
            var sawmill = PlaytestWorld.Building("Sawmill");
            ResourceManager.Instance.SetInfiniteResources(true);
            var cell = PlaytestWorld.FindFreeAreaNearTheMiddle(sawmill.footprintSize);
            placer.PinnedOriginForTests = cell;
            placer.SelectBuilding(sawmill);
            yield return null;
            placer.ConfirmPlacement();
            placer.PinnedOriginForTests = null;
            placer.ClearSelection();
            var built = Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None).FirstOrDefault(b => b.Data == sawmill && b.OriginCell == cell);
            Assert.IsNotNull(built, "The sawmill was not placed.");
            _placed.Add(built);

            var expected = BalanceConfig.Sandbox.Building("Sawmill").levels[0].buildSeconds;
            Assert.Greater(expected, 0, "The sandbox tab gives the sawmill no build_sec.");
            Assert.IsFalse(built.IsOperational, "A sandbox building stood finished the moment it was placed.");
            Assert.AreEqual(expected, built.Site.Total, 0.01f, "The sawmill is not built in the sandbox tab's time.");
            Assert.IsFalse(built.TryUpgrade(), "A building under construction could be upgraded.");

            built.Site.Finish();
            yield return null;
            Assert.IsTrue(built.IsOperational, "The sawmill did not work once built.");
            var level = built.Level;
            Assert.IsTrue(built.TryUpgrade(), "The finished sawmill could not be upgraded.");
            Assert.IsFalse(built.IsOperational, "An upgrade did not rest the building.");
            Assert.AreEqual(level, built.Level, "The level rose before the upgrade's time was up.");
            Assert.AreEqual(BalanceConfig.Sandbox.Building("Sawmill").levels[1].buildSeconds, built.Site.Total, 0.01f, "The upgrade does not take build_sec_2.");
            built.Site.Finish();
            Assert.AreEqual(level + 1, built.Level, "The upgrade's level was not applied at the end.");
        }

        [Test]
        public void AStorehouse_GivesItsRoom_OnlyOnceBuilt()
        {
            var warehouse = PlaytestWorld.Building("Warehouse");
            var before = ResourceManager.Instance.GetCapacity(ResourceType.Wood);
            var building = PlaytestWorld.Place(warehouse, PlaytestWorld.FindFreeAreaNearTheMiddle(warehouse.footprintSize));
            _placed.Add(building);
            Assert.Greater(ResourceManager.Instance.GetCapacity(ResourceType.Wood), before, "The test's warehouse holds no wood.");
            Object.DestroyImmediate(building.gameObject);
            Assert.AreEqual(before, ResourceManager.Instance.GetCapacity(ResourceType.Wood));

            building = PlaytestWorld.Place(warehouse, PlaytestWorld.FindFreeAreaNearTheMiddle(warehouse.footprintSize));
            _placed.Add(building);
            building.BuildOnTimer(warehouse.BuildSeconds(1));
            Assert.AreEqual(before, ResourceManager.Instance.GetCapacity(ResourceType.Wood), "A warehouse still going up already gave its room.");
            building.Site.Finish();
            Assert.Greater(ResourceManager.Instance.GetCapacity(ResourceType.Wood), before, "The finished warehouse gave no room.");
            Object.DestroyImmediate(building.gameObject);
            Assert.AreEqual(before, ResourceManager.Instance.GetCapacity(ResourceType.Wood), "Taking a warehouse down left its room behind -- or took it twice.");

            building = PlaytestWorld.Place(warehouse, PlaytestWorld.FindFreeAreaNearTheMiddle(warehouse.footprintSize));
            _placed.Add(building);
            building.BuildOnTimer(warehouse.BuildSeconds(1));
            Object.DestroyImmediate(building.gameObject);
            Assert.AreEqual(before, ResourceManager.Instance.GetCapacity(ResourceType.Wood), "A half-built warehouse taken down took room it never gave.");
        }

        [Test]
        public void ADamagedBuilding_IsMendedByItsDamage_AndNothingDecays()
        {
            var tower = PlaceFinished("Tower");
            Assert.IsNull(tower.GetRepairCost(), "A whole building asks to be mended.");
            Assert.IsFalse(BuildingInstance.DecayEnabled, "Buildings still age in the sandbox.");
            tower.SetCondition(tower.MaxHealth, 0.9f);
            Assert.AreEqual(0f, tower.Decay, "A save's decay came back.");

            tower.TryDamage(tower.MaxHealth / 2);
            var half = tower.GetRepairCost();
            Assert.IsNotNull(half, "A damaged building cannot be mended.");
            var full = BuildingInstance.RepairCostOf(tower.Data, tower.Level, 1f);
            Assert.Less(half.Sum(c => c.amount), full.Sum(c => c.amount), "Half the damage costs as much as all of it.");
            Assert.IsFalse(full.Any(c => c.type == ResourceType.Coins), "Mending asks for coins.");
            var worth = BuildingPlacer.CostOf(tower.Data).Where(c => c.type != ResourceType.Coins).Sum(c => c.amount);
            Assert.LessOrEqual(full.Sum(c => c.amount), Mathf.CeilToInt(worth * BalanceConfig.Sandbox.RepairCostFraction) + full.Count,
                "A repair from nothing costs more than the sandbox tab's share of the building.");

            ResourceManager.Instance.SetInfiniteResources(true);
            Assert.IsTrue(tower.TryRepair(), "The mending was refused with everything to pay with.");
            Assert.AreEqual(tower.MaxHealth, tower.CurrentHealth, "Mending did not make the building whole.");
            Assert.IsNull(tower.GetRepairCost());
        }

        [UnityTest]
        public IEnumerator TheHouses_MakeThePeople_NotTheTownHall()
        {
            TownHall();
            var people = HousePeople.Instance;
            Assert.IsNotNull(people, "No HousePeople in the sandbox scene.");
            foreach (var house in BuildingInstance.Houses.ToList()) Object.DestroyImmediate(house.gameObject);
            yield return null;
            Assert.AreEqual(0, people.Places, "The Town Hall gives places of its own.");

            var hovel = PlaceFinished("Hovel");
            var places = HousePeople.PlacesOf(hovel);
            Assert.Greater(places, 0, "The sandbox's Hovel holds no one.");
            Assert.AreEqual(places, people.Places);
            var going = PlaceFinished("Cottage");
            going.BuildOnTimer(going.Data.BuildSeconds(1));
            Assert.AreEqual(0, HousePeople.PlacesOf(going), "A house still going up already holds people.");
            yield return null;
            Assert.AreEqual(places, CitizenManager.Instance.Capacity, "The town's room is not the houses' places.");

            CitizenManager.Instance.SetPopulation(0);
            var seconds = BalanceConfig.Sandbox.PersonRespawnSeconds;
            Time.timeScale = 20f;
            for (var t = 0f; t < seconds * 1.5f && CitizenManager.Instance.TotalPopulation == 0; t += Time.deltaTime) yield return null;
            Time.timeScale = 1f;
            Assert.AreEqual(1, CitizenManager.Instance.TotalPopulation, "The house did not make a person in person_respawn_sec.");
            Assert.IsFalse(Citizens.MigrationManager.MovesPeople, "Migration still brings people.");
        }

        [UnityTest]
        public IEnumerator EveryHero_IsBoughtInTheTownHall_ForCoins()
        {
            var castle = TownHall();
            var roster = HeroRoster.Instance;
            Assert.IsNotNull(roster, "No HeroRoster in the sandbox scene.");
            Assert.Greater(BalanceConfig.Sandbox.Hero("king").price, 0, "The sandbox's King is free.");
            if (roster.HeroOf(SoldierType.King) == null)
            {
                ResourceManager.Instance.SetAmount(ResourceType.Coins, 0);
                Assert.AreEqual(HeroRoster.Offer.NotEnoughMoney, roster.Buy(SoldierType.King), "The King came for nothing.");
                ResourceManager.Instance.SetAmount(ResourceType.Coins, 100000);
                Assert.AreEqual(HeroRoster.Offer.Bought, roster.Buy(SoldierType.King));
            }
            Assert.AreEqual(HeroRoster.Offer.Owned, roster.Buy(SoldierType.King), "A second King could be bought.");
            yield return null;

            ResourceManager.Instance.SetAmount(ResourceType.Coins, 100000);
            if (roster.HeroOf(SoldierType.Queen) == null)
            {
                if (castle.Level < BalanceConfig.Sandbox.Hero("queen").castleLevel)
                    Assert.AreEqual(HeroRoster.Offer.CastleTooLow, roster.CanBuy(SoldierType.Queen), "The Queen is for sale at a low Town Hall.");
                ResourceManager.Instance.SetInfiniteResources(true);
                while (castle.Level < BalanceConfig.Sandbox.Hero("queen").castleLevel) Assert.IsTrue(PlaytestWorld.UpgradeNow(castle), "The Town Hall could not be upgraded.");
                ResourceManager.Instance.SetInfiniteResources(false);
                ResourceManager.Instance.SetAmount(ResourceType.Coins, 100000);
                Assert.AreEqual(HeroRoster.Offer.Bought, roster.Buy(SoldierType.Queen));
            }
            Assert.IsNotNull(roster.HeroOf(SoldierType.Queen).Unit, "The Queen has no fighter.");
            var army = ArmyManager.Instance;
            Assert.AreEqual(0, army.DailyUpkeep - army.Groups.Where(g => !SoldierStats.IsHero(g.Type) && !g.IsSummoned).Sum(g => g.Count * SoldierStats.UpkeepCoinsPerDay(g.Type)), "A hero is paid by the day.");
        }

        [Test]
        public void TheBuildingCard_IsTheRunsCard_WithWhatItMakes()
        {
            var sawmill = PlaceFinished("Sawmill");
            var panel = Object.FindAnyObjectByType<BuildingInfoPanelController>(FindObjectsInactive.Include);
            Assert.IsNotNull(panel);
            panel.Show(sawmill);
            try
            {
                var card = Object.FindAnyObjectByType<RunBuildingCard>(FindObjectsInactive.Include);
                Assert.IsTrue(card.gameObject.activeInHierarchy, "The sandbox does not show the run's building card.");
                var band = card.transform.Find("Band");
                Assert.IsNotNull(band, "The card has no band.");
                Assert.IsTrue(band.gameObject.activeSelf, "The sawmill's card does not say what it makes.");
                Assert.IsNotEmpty(band.Find("Makes").GetComponent<UnityEngine.UI.Text>().text);
            }
            finally
            {
                panel.Close();
            }
        }

        [UnityTest]
        public IEnumerator HeroesAndHalfBuiltBuildings_ComeBackFromASave()
        {
            yield return StartGame(null);
            _sceneLoaded = false;
            Research.ResearchManager.Instance?.CompleteEverything();
            TownHall();
            ResourceManager.Instance.SetAmount(ResourceType.Coins, 100000);
            Assert.AreEqual(HeroRoster.Offer.Bought, HeroRoster.Instance.Buy(SoldierType.King));
            var king = HeroRoster.Instance.HeroOf(SoldierType.King);
            king.AddExperience(Hero.XpForLevel(1) + 1);
            var kingLevel = king.Level;
            Assert.Greater(kingLevel, 1, "The test's experience did not raise the King.");

            var sawmill = PlaceFinished("Sawmill");
            sawmill.BuildOnTimer(sawmill.Data.BuildSeconds(1));
            var sawmillCell = sawmill.OriginCell;
            var hovel = PlaceFinished("Hovel");
            ResourceManager.Instance.SetInfiniteResources(true);
            Assert.IsTrue(hovel.TryUpgrade());
            ResourceManager.Instance.SetInfiniteResources(false);
            var hovelCell = hovel.OriginCell;
            yield return null;

            Object.FindAnyObjectByType<GameSaveController>().SaveGame(SaveName);
            _placed.Clear();
            yield return StartGame(SaveName);

            var back = HeroRoster.Instance.HeroOf(SoldierType.King);
            Assert.IsNotNull(back, "The King did not come back from the save.");
            Assert.AreEqual(kingLevel, back.Level, "The King came back at another level.");
            Assert.AreEqual(1, HeroRoster.Instance.Heroes.Count, "A hero came back twice (as a hero and as a soldier group?).");
            Assert.IsFalse(ArmyManager.Instance.Groups.Count(g => SoldierStats.IsHero(g.Type)) > 1, "The King's group came back twice.");

            var all = Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None);
            var site = all.First(b => b.Data.buildingName == "Sawmill" && b.OriginCell == sawmillCell);
            Assert.IsNotNull(site.Site, "A half-built sawmill came back finished.");
            Assert.IsFalse(site.Site.IsUpgrade);
            var house = all.First(b => b.Data.buildingName == "Hovel" && b.OriginCell == hovelCell);
            Assert.IsNotNull(house.Site, "An upgrade under way was lost by the save.");
            Assert.IsTrue(house.Site.IsUpgrade);
            Assert.AreEqual(1, house.Level, "The upgrade's level came back before its time was up.");
            house.Site.Finish();
            Assert.AreEqual(2, house.Level, "The saved upgrade did not raise the house at its end.");
        }
    }
}
