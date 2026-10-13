using System.Collections;
using CityBuilder.Grid;
using CityBuilder.Maps;
using CityBuilder.Resources;
using CityBuilder.Saving;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// Map 3, the summer valley (Tools/blender/map3.py): faceted land, relief as its own mesh,
    /// several forests, stony clearings and thousands of props. The world positions below are the
    /// generator's own landmarks -- Blender (x, y) is Unity (x, z).
    /// </summary>
    public class Map3Tests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map3";

        private static readonly Vector3 StartMeadow = new Vector3(-4f, 0f, -16f);
        private static readonly Vector3 RiverBed = new Vector3(31f, 0f, 30f);
        private static readonly Vector3 LakeMiddle = new Vector3(-52f, 0f, -76f);
        private static readonly Vector3 MarshPond = new Vector3(-70f, 0f, 14f);
        private static readonly Vector3 NorthMountains = new Vector3(-40f, 0f, 93f);
        private static readonly Vector3 EastHillTop = new Vector3(86f, 0f, -8f);

        private static bool _sceneLoaded;

        [UnitySetUp]
        public IEnumerator LoadMap3Once()
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
            _sceneLoaded = false;
        }

        [Test]
        public void TheMapLoads_AsMap3()
        {
            Assert.IsNotNull(MeshMapApplier.Instance, "No MeshMapApplier in the scene.");
            Assert.AreEqual(MapId, MeshMapApplier.Instance.CurrentMapId, "Map 3 was not applied -- is it in Resources/MeshMaps?");
        }

        [Test]
        public void ANewGame_StartsOnMap3()
        {
            var picked = MapSelector.PickForNewGame();
            Assert.IsNotNull(picked, "No map in the catalog.");
            Assert.AreEqual(MapId, picked.MapId, "While Map 3 is being tested every new game must start on it.");
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
            Assert.IsTrue(map.IsWaterCell(grid.WorldToCell(MarshPond)), "The forest pond reads as land.");

            var water = 0;
            for (var x = 0; x < grid.GridSize.x; x++)
                for (var z = 0; z < grid.GridSize.y; z++)
                    if (map.IsWaterCell(new Vector2Int(x, z))) water++;
            var share = water / (float)(grid.GridSize.x * grid.GridSize.y);
            Assert.That(share, Is.InRange(0.07f, 0.16f), $"{share:P0} of the map is water; the generator makes about 11%.");
        }

        [Test]
        public void TheRelief_IsLandButNeitherWalkedNorBuiltOn()
        {
            var grid = GridManager.Instance;
            var map = MeshMapApplier.Instance;

            foreach (var (name, point) in new[] { ("mountains", NorthMountains), ("hill top", EastHillTop) })
            {
                var cell = grid.WorldToCell(point);
                Assert.IsFalse(map.IsWaterCell(cell), $"The {name} read as water -- the relief mesh is not part of the land test.");
                Assert.IsFalse(map.IsGroundAt(point), $"The {name} read as ground a citizen may stand on.");
                Assert.IsTrue(map.IsNoBuildCell(cell), $"The {name} can be built on.");
                Assert.IsFalse(NavMesh.SamplePosition(point, out _, 1f, NavMesh.AllAreas),
                    $"The NavMesh covers the {name}: citizens pinned to the plain would walk through it.");
            }

            Assert.IsFalse(map.IsNoBuildCell(grid.WorldToCell(StartMeadow)), "The start meadow is marked as no-build.");
        }

        [Test]
        public void CitizensCanWalkTheMeadow()
        {
            Assert.IsTrue(NavMesh.SamplePosition(StartMeadow, out _, 1f, NavMesh.AllAreas),
                "No NavMesh on the start meadow -- citizens could not walk there.");
        }

        [Test]
        public void TheForests_AndTheBoulders_AreSpawned_TheBouldersInTheClearings()
        {
            var map = MeshMapApplier.Instance;
            var grid = GridManager.Instance;
            var clearings = new System.Collections.Generic.HashSet<Vector2Int>(map.RocksZoneCells);
            Assert.Greater(clearings.Count, 300, "The map's stony clearings did not come through.");

            var trees = 0;
            var rocks = 0;
            foreach (var node in ResourceNode.All)
            {
                if (node == null) continue;
                if (node.ResourceType == ResourceType.Wood) trees++;
                if (node.ResourceType != ResourceType.Stone) continue;
                rocks++;
                Assert.IsTrue(clearings.Contains(grid.WorldToCell(node.transform.position)),
                    $"A boulder at {node.transform.position} lies outside the stony clearings.");
            }
            Assert.GreaterOrEqual(trees, 500, "The forests are too small or missing.");
            Assert.GreaterOrEqual(rocks, 100, "Few boulders found room in the clearings.");
        }

        [UnityTest]
        public IEnumerator TheProps_AreDrawn_AndHideUnderABuilding()
        {
            var decor = MapDecorRenderer.Instance;
            Assert.IsNotNull(decor, "Map 3 has no decoration renderer.");
            Assert.Greater(decor.TotalCount, 8000, "Map 3's props did not come through.");
            Assert.Greater(decor.VisibleCount, decor.TotalCount / 2, "Most of the props are hidden.");

            var grid = GridManager.Instance;
            var map = MeshMapApplier.Instance;
            var near = grid.WorldToCell(StartMeadow + new Vector3(14f, 0f, 6f));
            Assert.IsTrue(decor.TryFindDecoratedCell(near, 40, out var cell), "No prop anywhere near the meadow.");
            Assert.IsTrue(grid.IsAreaFree(cell, Vector2Int.one) && !map.IsWaterCell(cell), "The test picked a cell it cannot build on.");

            var visibleBefore = decor.VisibleCount;
            var road = PlaytestWorld.Place(PlaytestWorld.Building("Road"), cell);
            yield return null;
            yield return null;

            Assert.IsFalse(decor.IsCellDecorVisible(cell), "A prop still stands in a cell a road now covers.");
            Assert.Less(decor.VisibleCount, visibleBefore, "Covering a decorated cell hid nothing.");

            PlaytestWorld.Demolish(road);
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Photograph_Map3()
        {
            yield return PlaytestCapture.Shoot("map3-overview", new Vector3(0f, 0f, 6f), 190f, 52f, 0f);
            yield return PlaytestCapture.Shoot("map3-meadow", StartMeadow, 30f, 50f, 0f, 60f);
            yield return PlaytestCapture.Shoot("map3-river", new Vector3(20f, 0f, 2f), 30f, 50f, 0f, 60f);
            yield return PlaytestCapture.Shoot("map3-marsh", MarshPond, 30f, 50f, 0f, 60f);
            yield return PlaytestCapture.Shoot("map3-hills", new Vector3(68f, 0f, 8f), 45f, 48f, 0f, 60f);
            yield return PlaytestCapture.Shoot("map3-mountains", new Vector3(28f, 0f, 66f), 45f, 45f, 0f, 60f);
        }
    }
}
