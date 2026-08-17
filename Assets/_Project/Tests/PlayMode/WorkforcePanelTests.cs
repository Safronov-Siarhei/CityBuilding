using System.Collections;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
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
    /// The workforce panel, exercised in the real scene.
    ///
    /// EditMode cannot reach this: the panel's whole job is to find every ProductionBuilding in a
    /// live settlement and move workers between them, and both halves only exist once the scene is
    /// running with real buildings in it. What is worth pinning down is that the list finds the
    /// workplaces at all, and that its buttons really move a worker from one building to another --
    /// the exact thing the panel was built for.
    /// </summary>
    public class WorkforcePanelTests
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

            ModalGate.SetBlocked(false);
            Time.timeScale = 1f;
            CitizenManager.Instance.SetPopulation(20);
            yield return null;
        }

        [TearDown]
        public void ClearPlacedBuildings()
        {
            var panel = Object.FindFirstObjectByType<WorkforcePanelController>(FindObjectsInactive.Include);
            if (panel != null) panel.ClosePanel();

            foreach (var building in _placed)
            {
                if (building != null) Object.DestroyImmediate(building.gameObject);
            }
            _placed.Clear();
            ModalGate.SetBlocked(false);
        }

        [UnityTest]
        public IEnumerator ThePanelListsTheWorkplacesThatExist()
        {
            var before = OpenAndCountRows();
            yield return null;

            PlaceWorkplaces("Sawmill", "Farm");
            yield return null;

            var after = OpenAndCountRows();
            Assert.AreEqual(before + 2, after, "Two workplaces were placed; the panel's list did not grow by two.");
        }

        /// <summary>The panel's entire reason to exist: take a worker off one building and put them on another without hunting for either on the map.</summary>
        [UnityTest]
        public IEnumerator AWorkerMovesBetweenBuildings()
        {
            PlaceWorkplaces("Sawmill", "Farm");
            yield return null;

            var sawmill = _placed[0].GetComponent<ProductionBuilding>();
            var farm = _placed[1].GetComponent<ProductionBuilding>();
            Assert.IsNotNull(sawmill, "The Sawmill has no ProductionBuilding, so it has no worker slots at all.");
            Assert.IsNotNull(farm);

            var panel = Panel();
            panel.OpenPanel();
            yield return null;

            panel.AssignTo(sawmill);
            Assert.AreEqual(1, sawmill.AssignedWorkers, "The panel's + hired nobody.");

            panel.RemoveFrom(sawmill);
            panel.AssignTo(farm);

            Assert.AreEqual(0, sawmill.AssignedWorkers);
            Assert.AreEqual(1, farm.AssignedWorkers, "The worker taken off the sawmill never arrived at the farm.");
        }

        /// <summary>Nobody can be hired out of an empty pool -- the panel must refuse rather than conjure a citizen.</summary>
        [UnityTest]
        public IEnumerator NobodyIsHiredWithoutAFreeCitizen()
        {
            PlaceWorkplaces("Sawmill");
            yield return null;

            CitizenManager.Instance.SetPopulation(0);
            var sawmill = _placed[0].GetComponent<ProductionBuilding>();

            var panel = Panel();
            panel.OpenPanel();
            yield return null;

            panel.AssignTo(sawmill);
            Assert.AreEqual(0, sawmill.AssignedWorkers, "A worker was hired out of a settlement with no citizens in it.");
        }

        /// <summary>A converter has to say what it eats: "даёт мука" alone gives the player no reason to prefer it over anything else.</summary>
        [UnityTest]
        public IEnumerator AConverterRowNamesItsInput()
        {
            PlaceWorkplaces("Windmill");
            yield return null;

            var description = WorkforcePanelController.DescribeWork(_placed[0].GetComponent<ProductionBuilding>());

            StringAssert.Contains(ResourceNames.Of(ResourceType.Flour), description);
            StringAssert.Contains(ResourceNames.Of(ResourceType.Grain), description);
        }

        private void PlaceWorkplaces(params string[] ids)
        {
            var origin = PlaytestWorld.FindFreeArea(new Vector2Int(4 * ids.Length, 4));

            for (var i = 0; i < ids.Length; i++)
            {
                var data = PlaytestWorld.Building(ids[i]);
                Assert.IsNotNull(data, $"The catalogue has no building called '{ids[i]}'.");
                _placed.Add(PlaytestWorld.Place(data, origin + new Vector2Int(i * 4, 0)));
            }
        }

        private static WorkforcePanelController Panel()
        {
            var panel = Object.FindFirstObjectByType<WorkforcePanelController>(FindObjectsInactive.Include);
            Assert.IsNotNull(panel, "The scene has no WorkforcePanelController -- SetupProject did not build the panel.");
            return panel;
        }

        private static int OpenAndCountRows()
        {
            var panel = Panel();
            panel.OpenPanel();
            return panel.RowCount;
        }
    }
}
