using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CityBuilder.Buildings;
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
    /// One rule, and every building in the catalogue has to keep it: **a placed building draws
    /// through at most one renderer.**
    ///
    /// All three kinds of building satisfy it in a different way, which is why the rule is worth
    /// stating in one place rather than three:
    /// - a procedurally assembled one is baked into a single mesh at project setup
    ///   (SetupProject.CombineParts), one submesh per material;
    /// - an authored one shows one level model at a time (BuildingLevelAppearance), and each
    ///   model is a single-mesh FBX;
    /// - a fence shows one of its two variants (FenceAppearance).
    ///
    /// It is worth a test because breaking it is invisible. Nothing looks wrong when a building
    /// comes apart into twenty-five renderers -- it just costs twenty-five draw calls instead of
    /// one, on a phone nobody is holding at the time. That is not hypothetical: RenderCostTests
    /// measured exactly that on 2026-08-31, thirty buildings adding 488 forward draws and 488
    /// shadow draws, and this file is what stops it coming back.
    ///
    /// It also catches the other way in: an FBX exported with its walls and roof as separate
    /// objects inside the file. Unity gives each of those its own renderer, so a model drawn
    /// perfectly to the triangle budget can still arrive costing five draws per copy.
    /// </summary>
    public class BuildingDrawCountTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map3";

        [UnitySetUp]
        public IEnumerator PrepareScene()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            GameSessionIntent.NewGameMapId = MapId;
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);

            ModalGate.SetBlocked(false);
            CityBuilder.Research.ResearchManager.Instance?.CompleteEverything();

            var resources = ResourceManager.Instance;
            resources.SetAmount(ResourceType.Coins, 100000);
            resources.SetAmount(ResourceType.Food, 100000);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EveryBuildingDrawsThroughOneRenderer()
        {
            var placer = Object.FindAnyObjectByType<BuildingPlacer>();
            Assert.IsNotNull(placer, "No BuildingPlacer in the scene -- nothing to read a catalogue from.");

            var offenders = new List<string>();
            var placed = 0;

            foreach (var data in Catalogue(placer))
            {
                var cell = PlaytestWorld.FindFreeArea(data.footprintSize);
                if (cell.x < 0) continue;

                var building = PlaytestWorld.Place(data, cell);
                if (building == null) continue;
                placed++;

                // Measured on the frame it is placed, before anything can decay, catch fire or be
                // upgraded: those add their own marker objects, and they are not what this counts.
                var drawing = building.GetComponentsInChildren<Renderer>()
                    .Where(r => r.enabled && r.gameObject.activeInHierarchy)
                    .ToArray();

                if (drawing.Length > 1)
                {
                    var parts = string.Join(", ", drawing.Take(6).Select(r => r.gameObject.name));
                    offenders.Add($"{data.buildingName}: {drawing.Length} renderers ({parts}{(drawing.Length > 6 ? ", ..." : string.Empty)})");
                }

                PlaytestWorld.Demolish(building);
                yield return null;
            }

            Assert.Greater(placed, 10, "Almost nothing could be placed, so this test proved nothing.");
            Assert.IsEmpty(offenders,
                $"{offenders.Count} of {placed} buildings draw through more than one renderer -- each extra one is a draw call per copy on the map:\n" +
                string.Join("\n", offenders));
        }

        private static IEnumerable<BuildingData> Catalogue(BuildingPlacer placer)
        {
            if (placer.MandatoryFirstBuilding != null) yield return placer.MandatoryFirstBuilding;
            foreach (var data in placer.AvailableBuildings)
            {
                // Water buildings need a shore, which the free-area search does not look for.
                if (data == null || data.isWaterCategory || data.prefab == null) continue;
                yield return data;
            }
        }
    }
}
