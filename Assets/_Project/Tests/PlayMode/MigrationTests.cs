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
    /// Contentment moving people, in the real scene.
    ///
    /// EditMode covers the shape of the curve (MigrationBalanceTests); what only a running game can
    /// show is that the curve is actually wired to anything -- that a house really does add room
    /// rather than people, that the room really does fill, that it stops filling when it is full,
    /// and that a miserable settlement empties down to the floor and no further.
    ///
    /// Contentment is pushed around here through the same public doors the game uses -- the tax
    /// rate, the hunger record, a building's decay -- rather than through a test-only setter, so a
    /// change that breaks the link between a factor and the score shows up here too. Each test
    /// asserts the contentment it engineered before relying on it: a test that silently failed to
    /// make the town miserable would pass by watching nobody leave.
    /// </summary>
    public class MigrationTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map1";

        private static bool _sceneLoaded;

        private readonly List<BuildingInstance> _placed = new List<BuildingInstance>();

        [UnitySetUp]
        public IEnumerator PrepareScene()
        {
            LogAssert.ignoreFailingMessages = true;

            if (!_sceneLoaded)
            {
                Time.timeScale = 1f;
                GameSessionIntent.NewGameMapId = MapId;
                SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
                yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);
                _sceneLoaded = true;
            }

            Time.timeScale = 1f;
            CitizenManager.Instance.SetPopulation(0);
            FoodConsumptionManager.Instance.RestoreFromSave(0, new int[0]);
            yield return null;
        }

        [TearDown]
        public void ClearPlacedBuildings()
        {
            // Destroying them hands their housing back through BuildingInstance.OnDestroy, which is
            // what keeps the settlement's capacity from accumulating across tests.
            foreach (var building in _placed)
            {
                if (building != null) Object.DestroyImmediate(building.gameObject);
            }
            _placed.Clear();
            CitizenManager.Instance.SetPopulation(0);
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

        /// <summary>Drives the migration clock without waiting the minutes out in real time -- each call is one whole interval, so at most one person moves per call.</summary>
        private static void RunClock(int intervals)
        {
            for (var i = 0; i < intervals; i++) MigrationManager.Instance.Step(600f);
        }

        [UnityTest]
        public IEnumerator AHouseAddsRoom_NotPeople()
        {
            var before = CitizenManager.Instance.Capacity;
            var house = Place("Hovel");

            Assert.AreEqual(before + house.HousingCapacity, CitizenManager.Instance.Capacity,
                "A house should hand the settlement somewhere to put people.");
            Assert.AreEqual(0, CitizenManager.Instance.TotalPopulation,
                "A house should not conjure up the people to fill it -- that is migration's job now.");
            yield break;
        }

        [UnityTest]
        public IEnumerator LosingAHouse_TakesItsRoomWithIt()
        {
            var house = Place("Hovel");
            var withHouse = CitizenManager.Instance.Capacity;
            // Read before the demolition: the component is gone afterwards and asking it anything
            // throws, which would fail this test for the wrong reason.
            var beds = house.HousingCapacity;

            PlaytestWorld.Demolish(house);
            yield return null;

            Assert.AreEqual(withHouse - beds, CitizenManager.Instance.Capacity,
                "A house burnt down in a raid should take its beds with it.");
        }

        [UnityTest]
        public IEnumerator NobodyMovesWhileTheSettlementIsFindingItsFeet()
        {
            Place("Castle");
            CitizenManager.Instance.SetPopulation(2);
            MigrationManager.Instance.RestoreFromSave(0f, 300f);

            // Well past several arrival intervals, but all of it inside the grace period.
            for (var i = 0; i < 5; i++) MigrationManager.Instance.Step(50f);

            Assert.AreEqual(MigrationManager.MigrationState.SettlingIn, MigrationManager.Instance.State);
            Assert.AreEqual(2, CitizenManager.Instance.TotalPopulation,
                "The settling-in period is supposed to freeze migration, not just slow it down.");
            yield break;
        }

        [UnityTest]
        public IEnumerator AContentSettlement_FillsItsHousingAndThenStops()
        {
            var hall = Place("Castle");
            CitizenManager.Instance.SetPopulation(1);

            MakeThemContent();
            Assert.Greater(HappinessManager.Instance.HappinessPercent, BalanceConfig.Instance.MigrationHappinessThreshold,
                "This test needs a contented settlement to be about anything; it did not get one.");

            MigrationManager.Instance.RestoreFromSave(0f, 0f);
            RunClock(40);

            Assert.AreEqual(hall.HousingCapacity, CitizenManager.Instance.TotalPopulation,
                "A content settlement should fill every bed it has.");
            Assert.AreEqual(MigrationManager.MigrationState.NoRoom, MigrationManager.Instance.State,
                "With every bed taken it should be waiting for housing, not still counting settlers in.");
            yield break;
        }

        [UnityTest]
        public IEnumerator AMiserableSettlement_LosesPeopleButNeverEmpties()
        {
            Place("Castle");
            CitizenManager.Instance.SetPopulation(6);

            MakeThemMiserable();
            Assert.Less(HappinessManager.Instance.HappinessPercent, BalanceConfig.Instance.MigrationHappinessThreshold,
                "This test needs a miserable settlement to be about anything; it did not get one.");

            MigrationManager.Instance.RestoreFromSave(0f, 0f);
            RunClock(40);

            Assert.AreEqual(BalanceConfig.Instance.MigrationMinPopulation, CitizenManager.Instance.TotalPopulation,
                "Unhappiness is meant to stall a settlement, not play out its defeat -- the floor should have held.");
            Assert.AreEqual(MigrationManager.MigrationState.Deserted, MigrationManager.Instance.State);
            yield break;
        }

        /// <summary>
        /// Departures must not feed the happiness model's own "recent deaths" factor. If they did,
        /// every citizen who walked out would make the settlement unhappier and hurry the next one
        /// along -- a spiral with no way back up, from a town that was merely a bit glum.
        /// </summary>
        [UnityTest]
        public IEnumerator PeopleWhoLeave_AreNotCountedAsDead()
        {
            Place("Castle");
            CitizenManager.Instance.SetPopulation(6);

            MakeThemMiserable();
            var deathsBefore = FoodConsumptionManager.Instance.RecentDeaths;
            var scoreBefore = HappinessManager.Instance.DeathScore;

            MigrationManager.Instance.RestoreFromSave(0f, 0f);
            RunClock(10);

            Assert.Less(CitizenManager.Instance.TotalPopulation, 6, "Nobody left, so this test proved nothing.");
            Assert.AreEqual(deathsBefore, FoodConsumptionManager.Instance.RecentDeaths,
                "Citizens who moved away were counted among the settlement's dead.");

            HappinessManager.Instance.Recompute();
            Assert.AreEqual(scoreBefore, HappinessManager.Instance.DeathScore,
                "Losing people to migration dragged the losses factor down, which is how a bad mood would feed itself.");
            yield break;
        }

        /// <summary>Tax at zero and a settlement with nothing wrong with it -- comfortably above the threshold without depending on the exact balance numbers.</summary>
        private static void MakeThemContent()
        {
            TaxManager.Instance.SetTaxRate(0);
            FoodConsumptionManager.Instance.RestoreFromSave(0, new int[0]);
            HappinessManager.Instance.Recompute();
        }

        /// <summary>
        /// The whole misery kit: the heaviest tax there is, a town that went to bed hungry, people
        /// buried in the last few days, and a building falling down. Reached through the same doors
        /// the game itself uses, so this stays honest about what actually drives the score.
        /// </summary>
        private void MakeThemMiserable()
        {
            TaxManager.Instance.SetTaxRate(100);
            FoodConsumptionManager.Instance.RestoreFromSave(1, new[] { 10 });

            var ruin = Place("Hovel");
            ruin.SetCondition(ruin.MaxHealth, 1f);

            HappinessManager.Instance.Recompute();
        }
    }
}
