using System.Collections;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Saving;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// A building whose look comes from an authored FBX rather than from the procedural generator --
    /// today the Town Hall, tomorrow all of them, since SetupProject now picks up any
    /// `<id>1-lvl1.fbx` by name.
    ///
    /// The invariant worth guarding is the click target. The box a player's clicks meet is built at
    /// project-setup time and used to be a number typed next to the building's colours; the model
    /// is whatever the artist exported. When those two disagree the symptoms are quiet and
    /// horrible: a roof that cannot be clicked, or an invisible box standing beside the building
    /// swallowing clicks meant for the ground -- which is precisely the bug that once cost this
    /// project a week (see ForestClickTargetingTests). Stand-in models are deliberately not the
    /// final height, so this has to hold by measurement, not by discipline.
    /// </summary>
    public class AuthoredModelTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map1";

        /// <summary>The building whose model is authored today. Anything else is still a placeholder built to a declared height.</summary>
        private const string AuthoredBuildingId = "TownHall";

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
            yield return null;
        }

        [TearDown]
        public void ClearPlacedBuildings()
        {
            foreach (var building in _placed)
            {
                if (building != null) Object.DestroyImmediate(building.gameObject);
            }
            _placed.Clear();
        }

        [Test]
        public void TheClickBox_IsTheSizeOfTheModelItStandsFor()
        {
            var building = PlaceAuthoredBuilding();
            var collider = building.GetComponent<BoxCollider>();
            Assert.IsNotNull(collider, $"{AuthoredBuildingId} has no collider -- it cannot be clicked at all.");

            var drawn = VisibleBounds(building);

            // Half a metre of slack, because a click target is not a physics hull: a chimney or a
            // flagpole may poke out of the top. Anything more and the box is answering for a
            // building of a different size.
            Assert.AreEqual(drawn.size.y, collider.bounds.size.y, 0.5f,
                $"The click box is {collider.bounds.size.y:0.##}m tall and the model is {drawn.size.y:0.##}m. " +
                "Either part of the building cannot be clicked, or there is an invisible box above it eating clicks.");

            Assert.AreEqual(GridManager.Instance.GroundHeight, collider.bounds.min.y, 0.1f, "The click box does not start at the ground.");
            Assert.AreEqual(GridManager.Instance.GroundHeight, drawn.min.y, 0.1f, "The model is not standing on the ground.");
        }

        [Test]
        public void TheModel_SitsOnThePlotItWasGiven()
        {
            var building = PlaceAuthoredBuilding();
            var footprint = building.RotatedFootprint();
            var centre = GridManager.Instance.GetFootprintCenterWorld(building.OriginCell, footprint);
            var drawn = VisibleBounds(building);
            var cell = GridManager.Instance.CellSize;

            Assert.AreEqual(centre.x, drawn.center.x, 0.35f, "The model is drawn off to one side of its plot.");
            Assert.AreEqual(centre.z, drawn.center.z, 0.35f, "The model is drawn off to one side of its plot.");

            // Half a cell of overhang is the same tolerance the build warns at (ModelPlotTolerance).
            Assert.LessOrEqual(drawn.size.x, footprint.x * cell + 0.5f, "The model is wider than the cells it was given.");
            Assert.LessOrEqual(drawn.size.z, footprint.y * cell + 0.5f, "The model is longer than the cells it was given.");
        }

        [UnityTest]
        public IEnumerator Photograph_TheAuthoredBuilding()
        {
            var building = PlaceAuthoredBuilding();
            yield return PlaytestCapture.Shoot("townhall", VisibleBounds(building).center, 16f, 30f, 35f);
        }

        private BuildingInstance PlaceAuthoredBuilding()
        {
            var data = PlaytestWorld.Building(AuthoredBuildingId);
            Assert.IsNotNull(data, $"No {AuthoredBuildingId} in the building catalogue.");
            Assert.IsNotNull(data.prefab, $"{AuthoredBuildingId} has no prefab -- its authored model was not found at project setup.");

            var cell = PlaytestWorld.FindFreeArea(data.footprintSize + Vector2Int.one);
            Assert.AreNotEqual(new Vector2Int(-1, -1), cell, $"Nowhere free on the map to place a {AuthoredBuildingId}.");

            var building = PlaytestWorld.Place(data, cell);
            _placed.Add(building);
            return building;
        }

        private static Bounds VisibleBounds(BuildingInstance building)
        {
            var renderers = building.GetComponentsInChildren<MeshRenderer>();
            Assert.Greater(renderers.Length, 0, $"{building.Data.buildingName} draws nothing at all.");

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }
    }
}
