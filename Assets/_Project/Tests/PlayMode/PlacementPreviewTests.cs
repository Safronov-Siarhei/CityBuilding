using System.Collections;
using System.Linq;
using CityBuilder.Buildings;
using CityBuilder.Combat;
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
    /// The placement preview the user picked (2026-10-20): an ink drawing of the building over a
    /// pen sketch of its plot, red and hatched where it cannot stand; a dragged road sketched cell
    /// by cell. Checked on the Caldera's meadow and on its crater lake's shore, and photographed
    /// when there is a graphics device.
    /// </summary>
    public class PlacementPreviewTests
    {
        private const string MapId = "Map5";
        private static readonly Vector3 TownMeadow = new Vector3(-6f, 0f, -25f);
        // open meadow north of the Town Hall, and the crater lake's west shore (half a building on water)
        private static readonly Vector3 OpenGround = new Vector3(-6f, 0f, -12f);
        private static readonly Vector3 LakeShore = new Vector3(5f, 0f, 2f);

        private BuildingPlacer _placer;

        [UnitySetUp]
        public IEnumerator Load()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            GameSessionIntent.NewGameMapId = MapId;
            SceneManager.LoadScene("CityBuilder", LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);
            ModalGate.SetBlocked(false);
            if (OrcRaidManager.Instance != null) OrcRaidManager.Instance.RaidsSuspended = true;

            var hall = PlaytestWorld.Building("Castle");
            PlaytestWorld.Place(hall, GridManager.Instance.WorldToCell(TownMeadow) - hall.footprintSize / 2);
            _placer = Object.FindAnyObjectByType<BuildingPlacer>();
            _placer.ClearSelection();
            CityBuilder.Resources.ResourceManager.Instance?.SetInfiniteResources(true);
            yield return null;
        }

        [TearDown]
        public void PutBack()
        {
            if (_placer == null) return;
            _placer.PinnedOriginForTests = null;
            _placer.ClearSelection();
        }

        [UnityTest]
        public IEnumerator TheDrawingAndItsSketch_SayWhereItCanStand()
        {
            var building = PickableBuilding();
            var grid = GridManager.Instance;

            _placer.PinnedOriginForTests = BuildingPlacer.OriginForCentredFootprint(grid.WorldToCell(OpenGround), building.footprintSize);
            _placer.SelectBuilding(building);
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.IsTrue(_placer.CanConfirm, "The building is refused on open meadow.");
            var ground = GameObject.Find("PlacementGround");
            Assert.IsNotNull(ground, "No pen sketch under the building.");
            Assert.Greater(ground.GetComponent<MeshFilter>().sharedMesh.vertexCount, 0, "The pen sketch is empty.");
            var ghost = Object.FindAnyObjectByType<PlacementPreview>();
            Assert.IsNotNull(ghost, "The ghost has no placement preview.");
            foreach (var renderer in ghost.GetComponentsInChildren<Renderer>())
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    Assert.AreEqual(RuntimeShaders.PlacementGhostName, material.shader.name, $"{renderer.name} is not drawn in ink.");
                }
            }

            _placer.PinnedOriginForTests = BuildingPlacer.OriginForCentredFootprint(grid.WorldToCell(LakeShore), building.footprintSize);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsFalse(_placer.CanConfirm, "The building may stand half in the lake.");
            Assert.Greater(ground.GetComponent<MeshRenderer>().sharedMaterials.Length, 4, "No hatching under the cells in the water.");
        }

        /// <summary>
        /// Roads and fences go down one piece at a time on the tick, like any building (the user's
        /// call, 2026-10-20) -- no more drawing a whole line with one drag -- and after each piece
        /// the next is offered right after it, carrying on the way the last two went.
        /// </summary>
        [UnityTest]
        public IEnumerator ARoad_IsLaidPieceByPiece_TheNextOfferedOnTheWayItRuns()
        {
            var road = PlaytestWorld.Building("Road");
            Assert.IsNotNull(road, "No Road in the build menu.");
            var grid = GridManager.Instance;
            var start = grid.WorldToCell(OpenGround + new Vector3(4f, 0f, 0f));

            _placer.PinnedOriginForTests = start;
            _placer.SelectBuilding(road);
            Assert.AreEqual(CityBuilder.InputControl.DragOwner.Camera, CityBuilder.InputControl.TouchInputRouter.Instance.SingleDragOwner,
                "Picking up a road still takes the one-finger drag away from the camera -- roads are drawn by dragging again.");
            yield return null;
            _placer.ConfirmPlacement();
            Assert.IsFalse(grid.IsAreaFree(start, Vector2Int.one), "The tick did not lay the road piece.");
            Assert.IsTrue(_placer.IsSelecting, "The road was put away after one piece -- the next should be offered.");

            // the first piece: the next is offered to one side of it
            _placer.PinnedOriginForTests = null;
            yield return null;
            var offered = _placer.GhostOriginForTests;
            Assert.IsTrue(offered.HasValue, "No next piece offered.");
            var step = offered.Value - start;
            Assert.AreEqual(1, Mathf.Abs(step.x) + Mathf.Abs(step.y), $"The next piece is offered at {offered}, not beside the first at {start}.");

            // a turn: lay one north of the first, and the offer carries on north
            var north = start + Vector2Int.up;
            _placer.PinnedOriginForTests = north;
            yield return null;
            _placer.ConfirmPlacement();
            _placer.PinnedOriginForTests = null;
            yield return null;
            Assert.AreEqual(north + Vector2Int.up, _placer.GhostOriginForTests, "After a turn north the next piece is not offered further north.");

            // and the tick on the offer lays it, offering the one after
            _placer.ConfirmPlacement();
            yield return null;
            Assert.IsFalse(grid.IsAreaFree(north + Vector2Int.up, Vector2Int.one), "The tick on the offered piece did not lay it.");
            Assert.AreEqual(north + Vector2Int.up * 2, _placer.GhostOriginForTests, "The road does not keep running on.");

            if (PlaytestCapture.Available)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                yield return PlaytestCapture.Shoot("placement-road-chain", grid.GetFootprintCenterWorld(north, Vector2Int.one), 9f, 50f, 15f);
            }
        }

        [UnityTest]
        public IEnumerator Photograph_ThePlacementPreview()
        {
            if (!PlaytestCapture.Available) Assert.Ignore("No graphics device: run without -nographics to photograph.");
            var building = PickableBuilding();
            var grid = GridManager.Instance;
            var free = BuildingPlacer.OriginForCentredFootprint(grid.WorldToCell(OpenGround), building.footprintSize);
            var onTheShore = BuildingPlacer.OriginForCentredFootprint(grid.WorldToCell(LakeShore), building.footprintSize);

            // the pen at work: a frame every ~0.12 s from the moment it is picked up
            _placer.PinnedOriginForTests = free;
            var started = Time.realtimeSinceStartup;
            _placer.SelectBuilding(building);
            for (var frame = 0; frame < 6; frame++)
            {
                while (Time.realtimeSinceStartup - started < 0.06f + frame * 0.12f) yield return null;
                yield return PlaytestCapture.Shoot($"placement-pen-{frame}", OpenGround, 9f, 45f, 25f);
            }
            yield return PlaytestCapture.Shoot("placement-free", OpenGround, 11f, 45f, 25f);

            _placer.PinnedOriginForTests = onTheShore;
            yield return new WaitForSecondsRealtime(0.5f);
            yield return PlaytestCapture.Shoot("placement-blocked", LakeShore, 11f, 45f, 25f);

            // the dust as one lands
            var sawmill = PlaytestWorld.Building("Sawmill");
            _placer.ClearSelection();
            _placer.PinnedOriginForTests = BuildingPlacer.OriginForCentredFootprint(grid.WorldToCell(OpenGround), sawmill.footprintSize);
            _placer.SelectBuilding(sawmill);
            yield return new WaitForSecondsRealtime(0.3f);
            _placer.ConfirmPlacement();
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.IsNotNull(GameObject.Find("DustPuff"), "No dust when the building landed.");
            yield return PlaytestCapture.Shoot("placement-dust", OpenGround, 11f, 45f, 25f);
        }

        /// <summary>The biggest building that can be picked up at the start of a game (others wait on research).</summary>
        private BuildingData PickableBuilding()
        {
            foreach (var id in new[] { "Tavern", "Warehouse", "Barn", "Cottage", "Sawmill" })
            {
                var candidate = PlaytestWorld.Building(id);
                if (candidate == null) continue;
                _placer.SelectBuilding(candidate);
                var picked = _placer.IsSelecting;
                _placer.ClearSelection();
                if (picked) return candidate;
            }
            Assert.Fail("No building could be picked up.");
            return null;
        }
    }
}
