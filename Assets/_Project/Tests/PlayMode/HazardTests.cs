using System.Collections;
using System.Collections.Generic;
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
    /// Fire and illness in the running scene -- the wiring EditMode cannot see.
    ///
    /// Both mechanics are mostly SCANS over the live buildings: which homes a well reaches, how
    /// many healers are actually standing in a Дом лекаря, which brigade can get to a fire. A scan
    /// that finds the wrong thing still produces plausible numbers, so each test here changes one
    /// building in the world and asserts the answer moved.
    /// </summary>
    public class HazardTests
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
            ModalGate.SetBlocked(false);
            if (OrcRaidManager.Instance != null) OrcRaidManager.Instance.RaidsSuspended = true;

            // Upgrading and staffing need the tech list; these tests are about hazards, not gates.
            Research.ResearchManager.Instance?.CompleteEverything();

            CitizenManager.Instance.SetPopulation(20);
            CitizenManager.Instance.SetSickPopulation(0);
            ResourceManager.Instance.SetAmount(ResourceType.Food, 500);
            ResourceManager.Instance.SetAmount(ResourceType.Coins, 500);
            yield return null;
        }

        [TearDown]
        public void CleanUp()
        {
            foreach (var building in _placed)
            {
                if (building != null) Object.DestroyImmediate(building.gameObject);
            }
            _placed.Clear();

            CitizenManager.Instance?.SetSickPopulation(0);
            Time.timeScale = 1f;
        }

        private BuildingInstance Place(string buildingId, Vector2Int? cell = null)
        {
            var data = PlaytestWorld.Building(buildingId);
            Assert.IsNotNull(data, $"No '{buildingId}' in the catalogue.");

            var instance = PlaytestWorld.Place(data, cell ?? PlaytestWorld.FindFreeArea(data.footprintSize));
            _placed.Add(instance);
            return instance;
        }

        /// <summary>
        /// The Колодец, finally doing something. A town whose homes are all out of reach of one
        /// counts as entirely dry; putting a well beside them counts as entirely watered.
        /// </summary>
        [UnityTest]
        public IEnumerator AWellWatersTheHomesWithinItsReach()
        {
            var sickness = SicknessManager.Instance;
            Assert.IsNotNull(sickness, "The scene has no SicknessManager.");

            var house = Place("Hovel");
            Assert.Greater(house.Data.LevelStats(1).housingCapacity, 0, "The fixture picked a building nobody lives in.");
            yield return null;

            sickness.PassDay();
            Assert.AreEqual(1f, sickness.UnwateredShare, 0.001f, "A town with no well at all counted as watered.");

            // Right on top of the house, which is well inside any well's radius.
            Place("Well", house.OriginCell + Vector2Int.right * 2);
            yield return null;

            sickness.PassDay();
            Assert.AreEqual(0f, sickness.UnwateredShare, 0.001f, "A well standing two cells from a house did not reach it.");
        }

        /// <summary>A dry, hungry settlement has to actually put people in bed -- and taking to bed has to cost the town its work, which is the entire point of the mechanic.</summary>
        [UnityTest]
        public IEnumerator ADryHungrySettlement_FallsIllAndStopsWorking()
        {
            var citizens = CitizenManager.Instance;
            var sickness = SicknessManager.Instance;

            // A home nobody can reach a well from, and a workplace with hands in it.
            Place("Hovel");
            var workplace = Place("Sawmill").GetComponent<ProductionBuilding>();
            Assert.IsNotNull(workplace, "The Sawmill has no worker slots.");
            workplace.SetAssignedWorkers(workplace.MaxWorkers);
            var staffed = workplace.AssignedWorkers;
            Assert.Greater(staffed, 0, "Nobody could be put to work, so there is no work for illness to cost.");

            // Starve them: hunger is the larger half of the risk, and without it the background
            // chance rounds to nobody in a town this size.
            ResourceManager.Instance.SetAmount(ResourceType.Food, 0);
            ResourceManager.Instance.SetAmount(ResourceType.Bread, 0);
            FoodConsumptionManager.Instance.FeedSettlement();
            Assert.Greater(FoodConsumptionManager.Instance.HungryDaysInARow, 0, "The town was supposed to go hungry.");
            yield return null;

            sickness.PassDay();

            Assert.Greater(citizens.SickPopulation, 0,
                "A hungry town with no well kept its health -- either the risk is not being read or nobody is being put in a bed.");

            // Everybody ill at once, so the lay-off is unambiguous.
            citizens.AddSick(citizens.HealthyPopulation);
            yield return null;

            Assert.AreEqual(0, workplace.AssignedWorkers,
                "A workplace kept its staff after every one of them took to bed, so illness costs the settlement nothing.");
        }

        /// <summary>The Дом лекаря, finally doing something. Empty it does nothing; staffed it clears beds.</summary>
        [UnityTest]
        public IEnumerator AStaffedHealerHouse_GetsPeopleBackOnTheirFeet()
        {
            var citizens = CitizenManager.Instance;
            var sickness = SicknessManager.Instance;

            var healer = Place("HealerHouse");
            var workplace = healer.GetComponent<ProductionBuilding>();
            Assert.IsNotNull(workplace, "The Дом лекаря has no worker slots, so nobody can staff it.");
            yield return null;

            Assert.AreEqual(0, sickness.HealingCapacityPerDay(), "An empty Дом лекаря is treating people.");

            workplace.SetAssignedWorkers(workplace.MaxWorkers);
            yield return null;
            var capacity = sickness.HealingCapacityPerDay();
            Assert.Greater(capacity, 0, "A fully staffed Дом лекаря can treat nobody.");

            citizens.AddSick(capacity);
            var sickBefore = citizens.SickPopulation;
            Assert.Greater(sickBefore, 0);

            sickness.PassDay();

            Assert.Less(citizens.SickPopulation, sickBefore, "The staffed Дом лекаря cured nobody.");
        }

        /// <summary>A fire has to actually eat the building, and then stop -- a town with no brigade should lose a chunk of a building, not every building that ever catches.</summary>
        [UnityTest]
        public IEnumerator AFireEatsTheBuildingAndThenGoesOut()
        {
            var barn = Place("Barn");
            yield return null;

            var healthBefore = barn.CurrentHealth;
            FireManager.Ignite(barn);
            var fire = barn.GetComponent<BuildingFire>();
            Assert.IsNotNull(fire, "Igniting a building did not set it alight.");

            // Faster than real time, or the suite waits out a full minute of burning.
            Time.timeScale = 20f;
            yield return WaitUntil(() => barn == null || barn.GetComponent<BuildingFire>() == null, realSecondsTimeout: 25f);
            Time.timeScale = 1f;

            Assert.IsTrue(barn == null || barn.GetComponent<BuildingFire>() == null,
                "The fire never went out, so an unattended one burns forever.");
            if (barn != null)
            {
                Assert.Less(barn.CurrentHealth, healthBefore, "The building came through the fire without a scratch.");
            }
        }

        /// <summary>What the Пожарная бригада is for, and the only decision a fire offers: staffing it shortens the burn, and an empty station does nothing at all.</summary>
        [UnityTest]
        public IEnumerator AStaffedBrigadeShortensTheFireAndAnEmptyOneDoesNot()
        {
            var barn = Place("Barn");
            var station = Place("FireBrigade", barn.OriginCell + Vector2Int.right * 3);
            var workplace = station.GetComponent<ProductionBuilding>();
            Assert.IsNotNull(workplace, "The Пожарная бригада has no worker slots, so nobody can man it.");
            yield return null;

            FireManager.Ignite(barn);
            var fire = barn.GetComponent<BuildingFire>();
            Assert.IsNotNull(fire);

            // The coverage is re-read on a timer, so give it the frames to notice each state.
            yield return WaitFrames(60);
            var unmanned = fire.TotalSeconds;

            workplace.SetAssignedWorkers(workplace.MaxWorkers);
            Assert.Greater(workplace.AssignedWorkers, 0, "Nobody could be put in the fire station.");
            yield return WaitFrames(60);

            Assert.Less(fire.TotalSeconds, unmanned,
                "Manning the fire station beside a burning building did not shorten the fire, so the brigade is decoration.");
        }

        private static IEnumerator WaitFrames(int frames)
        {
            for (var i = 0; i < frames; i++) yield return null;
        }

        /// <summary>Polls on REAL time, not scaled -- the same reason ArmyCombatTests does.</summary>
        private static IEnumerator WaitUntil(System.Func<bool> condition, float realSecondsTimeout)
        {
            var deadline = Time.realtimeSinceStartup + realSecondsTimeout;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }
    }
}
