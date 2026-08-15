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
    /// A rectangular building placed on its side, which is the one case where "the footprint" is
    /// two different numbers: what the balance/setup says it is (2x1) and what it actually covers
    /// once turned (1x2).
    ///
    /// Grid occupancy, the collider and the NavMesh obstacle all knew about the difference. Two
    /// places did not -- the fog reveal in BuildingInstance.Initialize and the point a worker walks
    /// to in CitizenVisualsManager -- and both asked for the centre of the UNROTATED shape, which
    /// for a 4x2 lands a whole building away from the real one. It never showed up because every
    /// building that matters today is square, and a square footprint is its own rotation.
    ///
    /// So the invariant is worth pinning down while the first rectangular models are still being
    /// drawn: what a building reserves, where it stands, and what it draws are the same rectangle.
    /// </summary>
    public class RotatedFootprintTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map1";

        /// <summary>Half a cell of slack: the visible geometry is inset from its cell edges (BuildingInset) and carries whatever porch or chimney the model has, but its middle cannot wander off the footprint.</summary>
        private const float AllowedCentreError = 0.35f;

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
        public void ARectangularBuilding_ReportsTheFootprintItActuallyCovers()
        {
            var data = RectangularBuilding();

            var upright = Place(data, 0);
            Assert.AreEqual(data.footprintSize, upright.RotatedFootprint(), "An unrotated building covers exactly what its data says.");

            var sideways = Place(data, 1);
            Assert.AreEqual(new Vector2Int(data.footprintSize.y, data.footprintSize.x), sideways.RotatedFootprint(),
                "Turned a quarter of a turn, a rectangle covers its own footprint with X and Z swapped.");

            // Two steps is a half turn: back to the original outline, facing the other way.
            var backwards = Place(data, 2);
            Assert.AreEqual(data.footprintSize, backwards.RotatedFootprint());
        }

        /// <summary>The three answers that used to disagree: the cells the grid reserved, the point the building was placed at, and the geometry the player sees.</summary>
        [Test]
        public void ASidewaysBuilding_ReservesStandsOnAndDrawsOverTheSameCells()
        {
            var data = RectangularBuilding();
            var sideways = Place(data, 1);
            var footprint = sideways.RotatedFootprint();
            var origin = sideways.OriginCell;
            var grid = GridManager.Instance;

            Assert.IsFalse(grid.IsAreaFree(origin, footprint), "The turned building did not reserve the cells it stands on.");

            // The cell just past its short side has to be free -- if the unrotated outline had been
            // reserved instead, this is the cell it would have taken.
            Assert.IsTrue(grid.IsAreaFree(origin + new Vector2Int(footprint.x, 0), Vector2Int.one),
                "The turned building reserved cells to the east, which is where its UNROTATED outline would have reached.");

            var centre = grid.GetFootprintCenterWorld(origin, footprint);
            Assert.AreEqual(centre.x, sideways.transform.position.x, 0.001f);
            Assert.AreEqual(centre.z, sideways.transform.position.z, 0.001f);

            var drawn = VisibleBounds(sideways);
            Assert.AreEqual(centre.x, drawn.center.x, AllowedCentreError, "The turned building draws itself off to one side of the cells it reserved.");
            Assert.AreEqual(centre.z, drawn.center.z, AllowedCentreError, "The turned building draws itself off to one side of the cells it reserved.");

            // And it really is longer along the axis it was turned onto, rather than merely sitting
            // in the right place while still drawn the wrong way round.
            var upright = Place(data, 0);
            var uprightDrawn = VisibleBounds(upright);
            Assert.Greater(drawn.size.z, drawn.size.x, "The turned building is still wider than it is long -- its model did not turn with it.");
            Assert.Greater(uprightDrawn.size.x, uprightDrawn.size.z);
        }

        /// <summary>The narrowest real rectangle in the catalogue. Everything here is about the difference between X and Z, so a square building would prove nothing.</summary>
        private static BuildingData RectangularBuilding()
        {
            var data = PlaytestWorld.Building("FishermanHut");
            Assert.IsNotNull(data, "No FishermanHut in the building catalogue.");
            Assert.AreNotEqual(data.footprintSize.x, data.footprintSize.y,
                "The FishermanHut is square now -- these tests need a rectangular building, pick another one.");
            return data;
        }

        private BuildingInstance Place(BuildingData data, int rotationSteps)
        {
            // Room for the footprint whichever way round it ends up, plus a margin so the "the cell
            // next door is still free" check is asking about open ground.
            var side = Mathf.Max(data.footprintSize.x, data.footprintSize.y) + 2;
            var cell = PlaytestWorld.FindFreeArea(new Vector2Int(side, side));
            Assert.AreNotEqual(new Vector2Int(-1, -1), cell, $"Nowhere free on the map to place a {data.buildingName}.");

            var building = PlaytestWorld.Place(data, cell, rotationSteps);
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
