using System.Collections;
using System.Linq;
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
    /// Map 3, the summer valley (Tools/blender/map3.py), redrawn on 2026-10-21 to the designed
    /// maps' rules: an island, a lake under the northern hills, a river from it to the sea, forests,
    /// stony clearings and thousands of props, the start zone and the orcs' spots fixed. The world
    /// positions below are the generator's own landmarks -- Blender (x, y) is Unity (x, z).
    /// </summary>
    public class Map3Tests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map3";

        private static readonly Vector3 StartMeadow = new Vector3(-40f, 0f, -46f);
        private static readonly Vector3 RiverBed = new Vector3(24f, 0f, -38f);
        private static readonly Vector3 LakeMiddle = new Vector3(-30f, 0f, 36f);
        private static readonly Vector3 OpenSea = new Vector3(0f, 0f, -97f);
        private static readonly Vector3 NorthHills = new Vector3(-40f, 0f, 80f);
        private static readonly Vector3 EastHillTop = new Vector3(66f, 0f, 22f);
        private static readonly Vector3 MainValley = new Vector3(44f, 0f, 56f);

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
        public void Map3_StaysInTheCatalog_ForItsSaves()
        {
            Assert.IsNotNull(MeshMapCatalog.GetById(MapId), "Map 3 left the catalog -- its saves would no longer load.");
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
            Assert.IsTrue(map.IsWaterCell(grid.WorldToCell(OpenSea)), "The sea round the island reads as land.");

            var water = 0;
            for (var x = 0; x < grid.GridSize.x; x++)
                for (var z = 0; z < grid.GridSize.y; z++)
                    if (map.IsWaterCell(new Vector2Int(x, z))) water++;
            var share = water / (float)(grid.GridSize.x * grid.GridSize.y);
            Assert.That(share, Is.InRange(0.24f, 0.36f), $"{share:P0} of the map is water; the generator makes about 30%.");
        }

        [Test]
        public void TheRelief_IsLandButNeitherWalkedNorBuiltOn()
        {
            var grid = GridManager.Instance;
            var map = MeshMapApplier.Instance;

            foreach (var (name, point) in new[] { ("northern hills", NorthHills), ("hill top", EastHillTop) })
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

        [Test]
        public void TheOrcs_AndTheTownHall_HaveTheirPlaces()
        {
            var map = MeshMapApplier.Instance.CurrentMap;
            Assert.IsTrue(map.HasMainBaseSpot, "Map 3 has no spot for the orcs' main base.");
            Assert.AreEqual(4, map.CampSpots.Length, "Map 3 plans four camps.");
            Assert.GreaterOrEqual(map.TownHallZone.Length, 3, "Map 3 has no start zone.");

            var grid = GridManager.Instance;
            var size = PlaytestWorld.Building("Castle").footprintSize;
            Vector2Int CellAt(Vector3 p) => grid.WorldToCell(p) - size / 2;
            Assert.IsTrue(StartZone.Allows(CellAt(StartMeadow), size), "The Town Hall may not go on the start meadow.");
            Assert.IsFalse(StartZone.Allows(CellAt(MainValley), size), "The Town Hall may go in the orcs' valley.");

            var minDistance = CityBuilder.Core.BalanceConfig.Instance.TownHallMinDistanceToOrcs;
            foreach (var corner in map.TownHallZone)
                foreach (var orcs in map.CampSpots.Append(map.MainBaseSpot))
                    Assert.GreaterOrEqual(Vector2.Distance(corner, orcs), minDistance - 2f,
                        $"The start zone's corner {corner} reaches within {minDistance} m of the orcs at {orcs}.");
        }

        [UnityTest]
        public IEnumerator Photograph_Map3()
        {
            yield return PlaytestCapture.Shoot("map3-overview", new Vector3(0f, 0f, 6f), 190f, 52f, 0f);
            yield return PlaytestCapture.Shoot("map3-meadow", StartMeadow, 30f, 50f, 0f, 60f);
            yield return PlaytestCapture.Shoot("map3-river", RiverBed, 30f, 50f, 0f, 60f);
            yield return PlaytestCapture.Shoot("map3-lake", LakeMiddle, 40f, 50f, 0f, 60f);
            yield return PlaytestCapture.Shoot("map3-main-valley", MainValley, 55f, 52f, 0f, 60f);
        }
    }
}
