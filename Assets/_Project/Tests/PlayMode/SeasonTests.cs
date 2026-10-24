using System.Collections;
using System.Linq;
using CityBuilder.Core;
using CityBuilder.Invasion;
using CityBuilder.Maps;
using CityBuilder.Saving;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// A map dressed in each season (Maps.SeasonStyle; the user, 2026-10-24): the ground, the broadleaf
    /// trees and winter's snow change, the conifers keep their needles, the figures never take snow --
    /// and the photographs show each season on the Invasion map.
    /// </summary>
    public class SeasonTests
    {
        private const string GameSceneName = "CityBuilder";

        [UnityTearDown]
        public IEnumerator Reset()
        {
            SeasonStyle.Override = null;
            GameSessionIntent.NewGameIsInvasion = false;
            InvasionRecords.FilePathOverride = null;
            Shader.SetGlobalFloat("_SeasonSnow", 0f);
            yield return null;
        }

        private static IEnumerator StartARunIn(MapSeason season)
        {
            LogAssert.ignoreFailingMessages = true;
            SeasonStyle.Override = season;
            InvasionRecords.FilePathOverride = System.IO.Path.Combine(Application.temporaryCachePath, "invasion_records_test.json");
            GameSessionIntent.NewGameMapId = InvasionMode.MapId;
            GameSessionIntent.NewGameIsInvasion = true;
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(InvasionMode.MapId);
            for (var frame = 0; frame < 300 && (InvasionMode.Instance == null || !InvasionMode.Instance.RunStarted); frame++) yield return null;
            Assert.IsTrue(InvasionMode.IsActive && InvasionMode.Instance.RunStarted, "The run never set itself up.");
        }

        private static Material GroundMaterial() =>
            Object.FindObjectsByType<MeshMapApplier>(FindObjectsInactive.Exclude).First()
                .GetComponentsInChildren<MeshRenderer>().Select(r => r.sharedMaterial).First(m => m != null && m.name.StartsWith("Terrain"));

        private static Material[] TreeMaterials(bool conifer) =>
            Object.FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude)
                .Where(n => n.ResourceType == CityBuilder.Resources.ResourceType.Wood)
                .Where(n => conifer == new[] { "Tree2", "Tree5", "Tree6", "Tree9" }.Any(t => n.name.StartsWith(t)))
                .Select(n => n.GetComponentInChildren<Renderer>().sharedMaterial).Distinct().ToArray();

        [UnityTest]
        public IEnumerator EverySeason_DressesTheMap()
        {
            foreach (var season in new[] { MapSeason.Summer, MapSeason.Autumn, MapSeason.Winter, MapSeason.Spring })
            {
                yield return StartARunIn(season);
                yield return new WaitForSeconds(0.5f);
                var ground = GroundMaterial();
                var broadleaf = TreeMaterials(conifer: false);
                var conifers = TreeMaterials(conifer: true);
                var snow = Shader.GetGlobalFloat("_SeasonSnow");
                Debug.Log($"SEASON {season}: ground={ground.name} broadleaf=[{string.Join(", ", broadleaf.Select(m => m.name))}] conifers=[{string.Join(", ", conifers.Select(m => m.name))}] snow={snow}");
                if (season == MapSeason.Summer)
                {
                    Assert.AreEqual("TerrainPalette", ground.name, "Summer is not today's look.");
                    Assert.AreEqual(0f, snow);
                }
                else
                {
                    Assert.AreEqual($"Terrain-{season}", ground.name, $"The ground is not dressed for {season}.");
                    Assert.IsTrue(broadleaf.All(m => m.name.StartsWith($"Trees-{season}-") && !m.name.EndsWith("Conifer")), $"A broadleaf tree is not in {season}'s crowns.");
                    Assert.IsTrue(conifers.All(m => m.name == $"Trees-{season}-Conifer"), $"A conifer is not in {season}'s needles.");
                    Assert.AreEqual(season == MapSeason.Winter ? 1f : 0f, snow, $"{season}'s snow is wrong.");
                }
                if (season == MapSeason.Autumn) Assert.Greater(broadleaf.Length, 1, "An autumn wood is all one colour.");
                var king = InvasionMode.Instance.King;
                Assert.IsTrue(king.GetComponentsInChildren<Renderer>().All(r => r.sharedMaterial == null || !r.sharedMaterial.name.StartsWith("BuildingsPalette")),
                    "The King wears the material that takes snow.");

                if (PlaytestCapture.Available)
                {
                    var castle = king.Home;
                    // the whole island in view, not the fog's parchment
                    var cell = Grid.GridManager.Instance.WorldToCell(castle);
                    FogOfWarManager.Instance?.RevealPermanent(cell, 60);
                    yield return new WaitForSeconds(FogOfWarManager.RevealSeconds + 0.3f);
                    yield return PlaytestCapture.Shoot($"season-{season.ToString().ToLowerInvariant()}-base", castle, 26f, 48f, 20f, 45f);
                    yield return PlaytestCapture.Shoot($"season-{season.ToString().ToLowerInvariant()}-wide", castle, 70f, 55f, 20f, 45f);
                }
            }
        }
    }
}
