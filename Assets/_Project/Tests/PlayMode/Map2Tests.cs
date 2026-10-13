using System.Collections;
using CityBuilder.Saving;
using CityBuilder.Grid;
using CityBuilder.Maps;
using CityBuilder.Resources;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// Map 2, made in Blender (Tools/blender/map1_claude.py) rather than by hand: one mainland
    /// between stepped mountains and the sea, a river with a ford, a lake. It has to honour the
    /// same contract as Map 1 without any code of its own, and these check that it does -- the
    /// world positions below are the generator's own landmarks, Blender (x, y) being Unity (x, z).
    /// </summary>
    public class Map2Tests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map2";

        private static readonly Vector3 StartMeadow = new Vector3(-18f, 0f, -12f);
        private static readonly Vector3 RiverBed = new Vector3(36f, 0f, 40f);
        private static readonly Vector3 LakeMiddle = new Vector3(-58f, 0f, 18f);
        private static readonly Vector3 WestMountains = new Vector3(-96f, 0f, 0f);

        private static bool _sceneLoaded;

        [UnitySetUp]
        public IEnumerator LoadMap2Once()
        {
            LogAssert.ignoreFailingMessages = true;
            if (_sceneLoaded) yield break;

            Time.timeScale = 1f;
            GameSessionIntent.NewGameMapId = MapId;
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);
            _sceneLoaded = true;
        }

        [OneTimeTearDown]
        public void ForgetTheScene()
        {
            // Whatever runs next loads its own map; this one must not believe Map 2 is still up.
            _sceneLoaded = false;
        }

        [Test]
        public void TheMapLoads_AsMap2()
        {
            Assert.IsNotNull(MeshMapApplier.Instance, "No MeshMapApplier in the scene.");
            Assert.AreEqual(MapId, MeshMapApplier.Instance.CurrentMapId, "Map 2 was not applied -- is it in Resources/MeshMaps?");
        }

        [Test]
        public void LandAndWater_AreWhereTheGeneratorPutThem()
        {
            var grid = GridManager.Instance;
            var map = MeshMapApplier.Instance;

            Assert.IsFalse(map.IsWaterCell(grid.WorldToCell(StartMeadow)), "The start meadow reads as water.");
            Assert.IsTrue(map.IsGroundAt(StartMeadow), "The start meadow is not walkable ground at the game's height.");
            Assert.IsTrue(map.IsWaterCell(grid.WorldToCell(RiverBed)), "The river reads as land -- the map is turned or mirrored.");
            Assert.IsTrue(map.IsWaterCell(grid.WorldToCell(LakeMiddle)), "The lake reads as land -- the map is turned or mirrored.");

            // The mountains are land (not water) but decorative: never ground a citizen may stand on.
            Assert.IsFalse(map.IsWaterCell(grid.WorldToCell(WestMountains)), "The mountains read as water.");
            Assert.IsFalse(map.IsGroundAt(WestMountains), "The mountains read as walkable ground.");

            var water = 0;
            for (var x = 0; x < grid.GridSize.x; x++)
                for (var z = 0; z < grid.GridSize.y; z++)
                    if (map.IsWaterCell(new Vector2Int(x, z))) water++;
            var share = water / (float)(grid.GridSize.x * grid.GridSize.y);
            Assert.That(share, Is.InRange(0.12f, 0.3f), $"{share:P0} of the map is water; the generator makes about a fifth.");
        }

        [Test]
        public void CitizensCanWalkTheMeadow()
        {
            Assert.IsTrue(NavMesh.SamplePosition(StartMeadow, out _, 1f, NavMesh.AllAreas),
                "No NavMesh on the start meadow -- citizens could not walk there.");
        }

        [Test]
        public void TheForestAndTheBouldersAreSpawned()
        {
            var trees = 0;
            var rocks = 0;
            foreach (var node in ResourceNode.All)
            {
                if (node == null) continue;
                if (node.ResourceType == ResourceType.Wood) trees++;
                if (node.ResourceType == ResourceType.Stone) rocks++;
            }
            // The forest zone is sized for the spawner's 600 trees with a cell between them.
            Assert.GreaterOrEqual(trees, 500, "The forest zone is too small or missing.");
            Assert.GreaterOrEqual(rocks, 100, "Few boulders found room on the flat land.");
        }

        [UnityTest]
        public IEnumerator Photograph_Map2()
        {
            yield return PlaytestCapture.Shoot("map2-overview", new Vector3(0f, 0f, 10f), 175f, 52f, 20f);
            yield return PlaytestCapture.Shoot("map2-meadow", StartMeadow, 40f, 50f, 25f);
            yield return PlaytestCapture.Shoot("map2-river", new Vector3(24f, 0f, 18f), 45f, 45f, 25f);
        }
    }
}
