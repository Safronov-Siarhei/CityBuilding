using System.Collections;
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
    /// The placement looks side by side, for the user to choose from (2026-10-20): each one with
    /// the building on open ground, then half over the Town Hall so some of its cells are in the
    /// way, and the dust as a building lands. Pictures only -- the choice is the user's.
    /// </summary>
    public class PlacementLookPhotoTests
    {
        private const string MapId = "Map5";
                // open meadow north of the Town Hall, and the crater lake's west shore (half the building on water)
        private static readonly Vector3 OpenGround = new Vector3(-6f, 0f, -12f);
        private static readonly Vector3 LakeShore = new Vector3(5f, 0f, 2f);
        private static readonly Vector3 TownMeadow = new Vector3(-6f, 0f, -25f);

        private Vector2Int _hallOrigin;
        private Vector2Int _hallSize;

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
            _hallSize = hall.footprintSize;
            _hallOrigin = GridManager.Instance.WorldToCell(TownMeadow) - _hallSize / 2;
            PlaytestWorld.Place(hall, _hallOrigin);
            Object.FindAnyObjectByType<BuildingPlacer>().ClearSelection();
            CityBuilder.Resources.ResourceManager.Instance?.SetInfiniteResources(true);
            yield return null;
        }

        [TearDown]
        public void PutBack()
        {
            PlacementPreview.Look = PlacementLook.Classic;
            var placer = Object.FindAnyObjectByType<BuildingPlacer>();
            if (placer != null)
            {
                placer.PinnedOriginForTests = null;
                placer.ClearSelection();
            }
        }

        [UnityTest]
        public IEnumerator Photograph_EveryPlacementLook()
        {
            if (!PlaytestCapture.Available) Assert.Ignore("No graphics device: run without -nographics to photograph.");
            var placer = Object.FindAnyObjectByType<BuildingPlacer>();
            // the biggest building that can be picked up at the start of a game (others wait on research)
            BuildingData building = null;
            foreach (var id in new[] { "Tavern", "Warehouse", "Barn", "Cottage", "Sawmill" })
            {
                var candidate = PlaytestWorld.Building(id);
                if (candidate == null) continue;
                placer.SelectBuilding(candidate);
                var picked = placer.IsSelecting;
                placer.ClearSelection();
                if (picked) { building = candidate; break; }
            }
            Assert.IsNotNull(building, "No building could be picked up to photograph.");
            Debug.Log($"[PlacementLook] photographing {building.buildingName} {building.footprintSize}");
            var size = building.footprintSize;

            var grid = GridManager.Instance;
            var free = BuildingPlacer.OriginForCentredFootprint(grid.WorldToCell(OpenGround), size);
            var onTheShore = BuildingPlacer.OriginForCentredFootprint(grid.WorldToCell(LakeShore), size);

            foreach (var look in new[] { PlacementLook.Classic, PlacementLook.OwnColours, PlacementLook.Drawing, PlacementLook.OwnColoursAndCells, PlacementLook.DrawingAndCells, PlacementLook.DrawingOnParchment, PlacementLook.DrawingWithCorners })
            {
                PlacementPreview.Look = look;
                placer.ClearSelection();
                placer.PinnedOriginForTests = free;
                placer.SelectBuilding(building);
                Assert.IsTrue(placer.IsSelecting, $"{building.buildingName} could not be picked up for {look}.");
                yield return new WaitForSecondsRealtime(0.4f);
                yield return PlaytestCapture.Shoot($"placement-{look}-free", OpenGround, 11f, 45f, 25f);

                placer.PinnedOriginForTests = onTheShore;
                yield return new WaitForSecondsRealtime(0.4f);
                yield return PlaytestCapture.Shoot($"placement-{look}-blocked", LakeShore, 11f, 45f, 25f);
            }

            // the dust: put one down and catch it a fifth of a second in
            PlacementPreview.Look = PlacementLook.OwnColours;
            placer.ClearSelection();
            // a building nothing else gates (the Tavern waits on research)
            var sawmill = PlaytestWorld.Building("Sawmill");
            placer.PinnedOriginForTests = BuildingPlacer.OriginForCentredFootprint(grid.WorldToCell(OpenGround), sawmill.footprintSize);
            placer.SelectBuilding(sawmill);
            yield return new WaitForSecondsRealtime(0.3f);
            placer.ConfirmPlacement();
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.IsNotNull(GameObject.Find("DustPuff"), "No dust when the building landed.");
            yield return PlaytestCapture.Shoot("placement-dust", OpenGround, 11f, 45f, 25f);
        }
    }
}
