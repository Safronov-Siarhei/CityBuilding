using System.Collections;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using CityBuilder.Saving;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// The fog of war's cloud layer on the map new games start on: land clears gradually rather
    /// than in one frame, the clouds draw only where there is fog, nothing on the land pokes up
    /// through them -- and how it looks, photographed, because a look is only verified by being
    /// seen.
    /// </summary>
    public class FogTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map3";

        private static bool _sceneLoaded;
        private static Vector3 _townHall;
        private static Vector2Int _townHallCell;

        [UnitySetUp]
        public IEnumerator PrepareScene()
        {
            LogAssert.ignoreFailingMessages = true;
            if (_sceneLoaded) yield break;

            Time.timeScale = 1f;
            GameSessionIntent.NewGameMapId = MapId;
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);

            var hall = PlaytestWorld.Building(BuildingIds.TownHall);
            var origin = PlaytestWorld.FindFreeAreaNearTheMiddle(hall.footprintSize + new Vector2Int(4, 4));
            var placed = PlaytestWorld.Place(hall, origin + new Vector2Int(2, 2));
            _townHall = placed.transform.position;
            _townHallCell = GridManager.Instance.WorldToCell(_townHall);
            Object.FindAnyObjectByType<BuildingPlacer>().MarkMandatoryBuildingAlreadyPlaced();

            Fog.Activate();
            for (var i = 0; i < 3; i++) yield return null;
            _sceneLoaded = true;
        }

        private static FogStyle _playersStyle;

        [OneTimeSetUp]
        public void RememberThePlayersStyle()
        {
            // The setting lives in the same PlayerPrefs the developer plays with.
            _playersStyle = GameSettings.FogStyle;
            GameSettings.FogStyle = FogStyle.Clouds;
        }

        [OneTimeTearDown]
        public void ForgetTheScene()
        {
            _sceneLoaded = false;
            GameSettings.FogStyle = _playersStyle;
        }

        [UnityTest]
        public IEnumerator SwitchingToParchment_RedrawsTheFogAsAChartOfTheLand()
        {
            GameSettings.FogStyle = FogStyle.Parchment;
            try
            {
                yield return WaitForChart();

                foreach (var chunk in Chunks())
                {
                    Assert.AreEqual("CityBuilder/FogParchment", chunk.sharedMaterial.shader.name, $"{chunk.name} still draws clouds.");
                }

                // The chart must say what the map says: water where the map has water, forest
                // where the trees stand, height where the hills are.
                var grid = GridManager.Instance;
                var waterCell = FindCell(cell => MeshMapApplier.Instance.IsWaterCell(cell) && AllAround(cell, 3, MeshMapApplier.Instance.IsWaterCell));
                Assert.IsNotNull(waterCell, "Map 3 has no open water to check the chart against.");
                Assert.Greater(Fog.ChartAt(waterCell.Value).r, 0.9f, "Open water is not water on the chart.");

                Vector2Int? treeCell = null;
                foreach (var node in ResourceNode.All)
                {
                    if (node != null && node.ResourceType == CityBuilder.Resources.ResourceType.Wood) { treeCell = grid.WorldToCell(node.transform.position); break; }
                }
                Assert.IsNotNull(treeCell, "No tree on the map to check the chart against.");
                Assert.Greater(Fog.ChartAt(treeCell.Value).b, 0.3f, "A tree's cell is not forest on the chart.");

                var highest = 0f;
                for (var x = 0; x < grid.GridSize.x; x += 2)
                {
                    for (var z = 0; z < grid.GridSize.y; z += 2) highest = Mathf.Max(highest, Fog.ChartAt(new Vector2Int(x, z)).g);
                }
                Assert.Greater(highest * 32f, 4f, "The chart shows no hill higher than 4 m -- Map 3's mountains are missing from it.");
            }
            finally
            {
                GameSettings.FogStyle = FogStyle.Clouds;
            }

            foreach (var chunk in Chunks())
            {
                Assert.AreEqual("CityBuilder/FogClouds", chunk.sharedMaterial.shader.name, $"{chunk.name} did not go back to clouds.");
            }
        }

        private static IEnumerator WaitForChart()
        {
            var deadline = Time.unscaledTime + 6f;
            while (!Fog.ChartBuilt && Time.unscaledTime < deadline) yield return null;
            Assert.IsTrue(Fog.ChartBuilt, "The parchment never drew its chart of the map.");
        }

        private static Vector2Int? FindCell(System.Func<Vector2Int, bool> wanted)
        {
            var size = GridManager.Instance.GridSize;
            for (var x = 4; x < size.x - 4; x++)
            {
                for (var z = 4; z < size.y - 4; z++)
                {
                    var cell = new Vector2Int(x, z);
                    if (wanted(cell)) return cell;
                }
            }
            return null;
        }

        private static bool AllAround(Vector2Int cell, int radius, System.Func<Vector2Int, bool> test)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                for (var dz = -radius; dz <= radius; dz++)
                {
                    if (!test(cell + new Vector2Int(dx, dz))) return false;
                }
            }
            return true;
        }

        private static FogOfWarManager Fog
        {
            get
            {
                var fog = Object.FindAnyObjectByType<FogOfWarManager>();
                Assert.IsNotNull(fog, "No FogOfWarManager in the scene.");
                return fog;
            }
        }

        [Test]
        public void TheTownIsClear_AndTheFarCornerIsClouded()
        {
            Assert.AreEqual(1f, Fog.DisplayedClearness(_townHallCell), 1e-3f, "The Town Hall's own cell is under cloud.");
            Assert.AreEqual(0f, Fog.DisplayedClearness(FarCorner()), 1e-3f, "The far corner of the map is not under cloud.");
        }

        [UnityTest]
        public IEnumerator NewLandClearsGradually_NotInOneFrame()
        {
            var cell = FarCorner() + new Vector2Int(6, 6);
            Assert.AreEqual(0f, Fog.DisplayedClearness(cell), 1e-3f, "The test cell is already clear -- pick another.");

            Fog.RevealPermanent(cell, 3);
            yield return null;
            yield return null;

            var midway = Fog.DisplayedClearness(cell);
            Assert.Greater(midway, 0f, "Two frames after the reveal, nothing has started to clear.");
            Assert.Less(midway, 1f, "The land cleared in one go -- there is no easing.");

            var until = Time.unscaledTime + FogOfWarManager.RevealSeconds + 0.5f;
            while (Time.unscaledTime < until) yield return null;
            Assert.AreEqual(1f, Fog.DisplayedClearness(cell), 1e-3f, "The land never finished clearing.");
        }

        /// <summary>The explored town is where the camera spends its time; drawing a fully transparent blanket over it would be a screenful of wasted fill on a phone.</summary>
        [UnityTest]
        public IEnumerator CloudIsDrawnOnlyWhereThereIsFog()
        {
            // A wide clearing around the town, so at least one whole chunk (and its margin) is clear.
            Fog.RevealPermanent(_townHallCell, 40);
            yield return new WaitForSeconds(FogOfWarManager.RevealSeconds + 1.2f);

            var chunks = Chunks();
            Assert.Greater(chunks.Count, 0, "The fog built no chunks.");

            MeshRenderer overTown = null;
            var onCount = 0;
            foreach (var chunk in chunks)
            {
                if (chunk.gameObject.activeInHierarchy) onCount++;
                // The mesh's bounds, not the renderer's: a switched-off renderer reports empty ones.
                // The chunks are built in world coordinates, so the two are the same space.
                var bounds = chunk.GetComponent<MeshFilter>().sharedMesh.bounds;
                if (bounds.Contains(new Vector3(_townHall.x, bounds.center.y, _townHall.z))) overTown = chunk;
            }

            Assert.IsNotNull(overTown, "No chunk lies over the Town Hall.");
            Assert.IsFalse(overTown.gameObject.activeInHierarchy, "The chunk over the explored town is drawing cloud it has none of.");
            Assert.Greater(onCount, 0, "No chunk draws any cloud at all.");
            Assert.Less(onCount, chunks.Count, "Every chunk draws -- the explored land is not being skipped.");
        }

        /// <summary>The old lid hovered at a fixed 5 m and Map 3's mountains came up through it.</summary>
        [Test]
        public void NothingOnTheLandPokesThroughTheCloud()
        {
            var lowest = float.MaxValue;
            var checkedPoints = 0;
            foreach (var chunk in Chunks())
            {
                var mesh = chunk.GetComponent<MeshFilter>().sharedMesh;
                Assert.IsNotNull(mesh, $"{chunk.name} has no mesh.");
                foreach (var vertex in mesh.vertices)
                {
                    var ray = new Ray(new Vector3(vertex.x, 200f, vertex.z), Vector3.down);
                    if (!MeshMapApplier.Instance.TryRaycastGround(ray, out var hit)) continue;
                    lowest = Mathf.Min(lowest, vertex.y - hit.point.y);
                    checkedPoints++;
                }
            }

            Assert.Greater(checkedPoints, 1000, "Too few blanket points stand over land to say anything.");
            Assert.GreaterOrEqual(lowest, FogOfWarManager.Clearance - 0.05f,
                $"Somewhere the cloud is only {lowest:0.00} m above the land -- a hill or mountain is poking through it.");
        }

        // First, so the photographs show the opening clearing and not the wider one a later test makes.
        [UnityTest, Order(0)]
        public IEnumerator Photograph_TheFog()
        {
            if (!PlaytestCapture.Available)
            {
                Debug.Log("[Playtest] No graphics device -- re-run without -nographics to photograph the fog.");
                yield break;
            }

            yield return new WaitForSeconds(1f);

            // As the game opens (15 m), zoomed all the way out (25 m), and from further than the
            // game allows, to see the edge's whole shape.
            yield return PlaytestCapture.Shoot("fog-15m", _townHall, 15f, 50f, 0f, 60f);
            yield return PlaytestCapture.Shoot("fog-25m", _townHall, 25f, 50f, 0f, 60f);
            yield return PlaytestCapture.Shoot("fog-45m", _townHall, 45f, 50f, 20f, 60f);
            yield return PlaytestCapture.Shoot("fog-edge-close", _townHall + new Vector3(-14f, 0f, 10f), 9f, 45f, -30f, 60f);

            // The other look, the same views.
            GameSettings.FogStyle = FogStyle.Parchment;
            try
            {
                yield return WaitForChart();
                yield return PlaytestCapture.Shoot("parchment-15m", _townHall, 15f, 50f, 0f, 60f);
                yield return PlaytestCapture.Shoot("parchment-25m", _townHall, 25f, 50f, 0f, 60f);
                yield return PlaytestCapture.Shoot("parchment-45m", _townHall, 45f, 50f, 20f, 60f);
                yield return PlaytestCapture.Shoot("parchment-edge-close", _townHall + new Vector3(-14f, 0f, 10f), 9f, 45f, -30f, 60f);
            }
            finally
            {
                GameSettings.FogStyle = FogStyle.Clouds;
            }
        }

        private static Vector2Int FarCorner()
        {
            var size = GridManager.Instance.GridSize;
            return new Vector2Int(_townHallCell.x < size.x / 2 ? size.x - 20 : 8, _townHallCell.y < size.y / 2 ? size.y - 20 : 8);
        }

        private static List<MeshRenderer> Chunks()
        {
            var chunks = new List<MeshRenderer>();
            foreach (var renderer in Fog.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.name.StartsWith("FogChunk")) chunks.Add(renderer);
            }
            return chunks;
        }
    }
}
