using System.Collections;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using CityBuilder.Saving;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// The gate (the user, 2026-10-21): one cell, set down on a road, open to citizens and shut to
    /// orcs -- and the road is still there when the gate goes.
    /// </summary>
    public class GateTests
    {
        [UnitySetUp]
        public IEnumerator NewGame()
        {
            LogAssert.ignoreFailingMessages = true;
            GameSessionIntent.SaveNameToLoad = null;
            GameSessionIntent.NewGameMapId = "Map3";
            SceneManager.LoadScene("CityBuilder", LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady("Map3");
        }

        [UnityTest]
        public IEnumerator AGateGoesOnARoad_OpenToCitizens_ShutToOrcs()
        {
            var gate = PlaytestWorld.Building("Gate");
            var road = PlaytestWorld.Building("Road");
            Assert.AreEqual(Vector2Int.one, gate.footprintSize, "The gate is not one cell.");

            var cell = PlaytestWorld.FindFreeAreaNearTheMiddle(Vector2Int.one);
            PlaytestWorld.Place(road, cell);
            RoadNetwork.Instance.RegisterRoad(cell);
            Assert.IsTrue(BuildingPlacer.IsFreeFor(gate, cell, Vector2Int.one), "A gate cannot be set down on a road.");
            Assert.IsFalse(BuildingPlacer.IsFreeFor(PlaytestWorld.Building("Hovel"), cell, Vector2Int.one), "Any building can now be set down on a road.");

            var placed = PlaytestWorld.Place(gate, cell, 1); // across a road that runs north-south
            Assert.IsFalse(BuildingPlacer.IsFreeFor(gate, cell, Vector2Int.one), "A second gate fits on the same road cell.");

            // the NavMesh is rebuilt a moment after a gate goes down
            yield return new WaitForSeconds(2f);
            var at = GridManager.Instance.GetFootprintCenterWorld(cell, Vector2Int.one);
            Assert.IsTrue(NavMesh.SamplePosition(at, out var open, 0.3f, NavMesh.AllAreas) && open.mask == 1 << NavAreas.Gate,
                "The gate's cell is not walkable gate ground for citizens.");
            Assert.IsFalse(NavMesh.SamplePosition(at, out _, 0.3f, NavAreas.WithoutGates), "Orcs can walk the gate's cell.");
            Assert.IsTrue(placed.GetComponent<BoxCollider>().isTrigger, "The gate's body still stops citizens walking through.");

            // a picture for the user: the road running through, a fence either side
            var fence = PlaytestWorld.Building("Fence");
            foreach (var dz in new[] { -1, 1 })
            {
                var r = cell + new Vector2Int(0, dz);
                if (GridManager.Instance.IsAreaFree(r, Vector2Int.one)) { PlaytestWorld.Place(road, r); RoadNetwork.Instance.RegisterRoad(r); }
            }
            foreach (var dx in new[] { -2, -1, 1, 2 })
            {
                var f = cell + new Vector2Int(dx, 0);
                if (GridManager.Instance.IsAreaFree(f, Vector2Int.one)) PlaytestWorld.Place(fence, f);
            }
            var placer = Object.FindAnyObjectByType<BuildingPlacer>();
            placer.MarkMandatoryBuildingAlreadyPlaced();
            placer.ClearSelection();
            foreach (var ghost in GameObject.FindObjectsByType<PlacementPreview>(FindObjectsSortMode.None)) ghost.gameObject.SetActive(false);
            yield return PlaytestCapture.Shoot("gate-on-road", at, 4f, 30f, 30f);

            // the gate goes, the road stays
            PlaytestWorld.Demolish(placed);
            yield return null;
            Assert.IsFalse(GridManager.Instance.IsAreaFree(cell, Vector2Int.one), "Knocking the gate down freed the road's cell.");
        }
    }
}
