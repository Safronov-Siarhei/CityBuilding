using System.Collections;
using System.Linq;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using CityBuilder.Resources;
using CityBuilder.Saving;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// Guards the one rule in SceneryShadows: the scenery is out of the shadow pass and the
    /// buildings are still in it.
    ///
    /// Worth a test rather than a comment because it is invisible when it breaks. Nothing looks
    /// wrong if a future spawner forgets the call -- the game simply costs more, and the only
    /// symptom is a number on a phone nobody is holding at the time. The counts are the point:
    /// trees and boulders are spawned in the hundreds, so ONE missed path is most of the saving.
    /// </summary>
    public class SceneryShadowTests
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
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheGroundDoesNotCastShadows()
        {
            var applier = Object.FindFirstObjectByType<MeshMapApplier>();
            Assert.IsNotNull(applier, "No MeshMapApplier in the scene -- the map never loaded.");

            // Only the MAP's own geometry, identified by its layer. The spawners hang their trees
            // and rocks under this same object, so an unfiltered sweep here reported a forgotten
            // tree as a ground fault -- which is exactly what happened the first time this suite
            // was verified by breaking it.
            //
            // Renderers that are switched OFF are excluded too: the authored placement zones are
            // instantiated with their renderers disabled (MeshMapApplier.SetRenderersEnabled), so
            // they draw nothing at all and their casting mode is moot.
            var casters = applier.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.gameObject.layer == TerrainPhysicsLayer.Layer)
                .Where(r => r.enabled && r.shadowCastingMode != ShadowCastingMode.Off)
                .ToArray();

            Assert.IsEmpty(casters.Select(r => r.name),
                "The map's own geometry is back in the shadow pass. The ground mesh alone is 35 728 triangles drawn a second time for a shadow that falls on nothing.");
            yield break;
        }

        [UnityTest]
        public IEnumerator TheFogDoesNotCastShadows()
        {
            var fog = Object.FindFirstObjectByType<FogOfWarManager>();
            Assert.IsNotNull(fog, "No FogOfWarManager in the scene.");

            // By name, because the fog manager shares its GameObject with the map: an unfiltered
            // sweep here picks up the authored zone instances as well (whose renderers are off and
            // draw nothing) and reports one of those as a fog chunk. The count guard below is what
            // catches this filter going stale if FogOfWarManager ever renames its chunks.
            var chunks = fog.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.name.StartsWith("FogChunk"))
                .ToArray();
            Assert.Greater(chunks.Length, 0, "The fog built no chunks -- this test would pass vacuously.");

            foreach (var renderer in chunks)
            {
                Assert.AreEqual(ShadowCastingMode.Off, renderer.shadowCastingMode,
                    $"Fog chunk '{renderer.name}' casts a shadow. There are 100 of these covering the whole map, and each one is an opaque lid over ground the player cannot see anyway.");
            }
            yield break;
        }

        [UnityTest]
        public IEnumerator TreesAndRocksDoNotCastShadows()
        {
            var nodes = Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);
            Assert.Greater(nodes.Length, 0, "No trees or rocks on the map at all -- this test would pass vacuously.");

            foreach (var node in nodes)
            {
                foreach (var renderer in node.GetComponentsInChildren<Renderer>(true))
                {
                    Assert.AreEqual(ShadowCastingMode.Off, renderer.shadowCastingMode,
                        $"{node.ResourceType} node '{renderer.name}' casts a shadow. There are ~600 trees and ~140 rock clusters on this map.");
                }
            }
            yield break;
        }

        [UnityTest]
        public IEnumerator CitizensDoNotCastShadows()
        {
            // A fresh map has nobody on it until a settlement exists, and an empty sweep would
            // pass this test without checking anything. Citizens spawn relative to the town
            // centre, so no agent appears at all until a Town Hall is standing -- see
            // CitizenVisualsManager.SyncVisualCount, which gives up when it cannot resolve one.
            var townHall = PlaytestWorld.Place(PlaytestWorld.Building(BuildingIds.TownHall),
                PlaytestWorld.FindFreeArea(new Vector2Int(6, 6)));
            Assert.IsNotNull(townHall, "The Town Hall would not go down -- citizens cannot spawn without it.");
            yield return null;

            CitizenManager.Instance.SetPopulation(10);
            yield return null;
            yield return null;

            var citizens = Object.FindObjectsByType<CitizenAgent>(FindObjectsSortMode.None);
            Assert.Greater(citizens.Length, 0, "SetPopulation(10) produced no citizen agents -- this test would pass vacuously.");

            foreach (var renderer in citizens[0].GetComponentsInChildren<Renderer>(true))
            {
                Assert.AreEqual(ShadowCastingMode.Off, renderer.shadowCastingMode,
                    $"Citizen part '{renderer.name}' casts a shadow -- two cubes each, up to 150 of them.");
            }

            PlaytestWorld.Demolish(townHall);
        }

        [UnityTest]
        public IEnumerator BuildingsStillCastShadows()
        {
            var cell = PlaytestWorld.FindFreeArea(new Vector2Int(6, 6));
            var data = PlaytestWorld.Building(BuildingIds.TownHall);
            Assert.IsNotNull(data, "No Town Hall in the catalogue.");

            var building = PlaytestWorld.Place(data, cell);
            Assert.IsNotNull(building, "The Town Hall would not go down on a free area.");
            yield return null;

            var casts = building.GetComponentsInChildren<Renderer>(true)
                .Any(r => r.shadowCastingMode != ShadowCastingMode.Off);

            Assert.IsTrue(casts,
                "Buildings must keep casting: their shadow is what tells the player where a thing stands and how tall it is.");

            PlaytestWorld.Demolish(building);
        }
    }
}
