using System.Collections;
using CityBuilder.Buildings;
using CityBuilder.Grid;
using CityBuilder.Maps;
using CityBuilder.Maps.Archipelago;
using CityBuilder.Saving;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// The archipelago in the game (the user, 2026-10-29): a new game on a seed builds its 500 m
    /// world -- the grid sized for it, land where the generator put land and sea elsewhere, the plain
    /// walkable and the relief not, the start zone on the start island where the Town Hall goes, the
    /// orcs' main base on their spot and no raids over land. Photographed for the user.
    /// </summary>
    public class ArchipelagoTests
    {
        private const int Seed = 1;
        private static readonly string MapId = ArchipelagoMapId.For(Seed);

        [UnitySetUp]
        public IEnumerator NewGame()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            GameSessionIntent.SaveNameToLoad = null;
            GameSessionIntent.NewGameMapId = MapId;
            SceneManager.LoadScene("CityBuilder", LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);
        }

        [UnityTest]
        public IEnumerator ASeed_BuildsItsArchipelago_AndTheTownHallGoesInTheStartZone()
        {
            var grid = GridManager.Instance;
            var applier = MeshMapApplier.Instance;
            Assert.AreEqual(MapId, applier.CurrentMapId);
            Assert.AreEqual(new Vector2Int(ArchipelagoGenerator.World, ArchipelagoGenerator.World), grid.GridSize, "The grid was not sized for the archipelago.");

            var world = ArchipelagoGenerator.Generate(Seed);
            var n = world.Size;
            // the land test agrees with the generator, cell for cell on a sample
            for (var i = 0; i < n * n; i += 997)
            {
                var cell = new Vector2Int(i % n, i / n);
                Assert.AreEqual(!world.Land[i], applier.IsWaterCell(cell), $"Cell {cell}: the game and the generator disagree on land.");
            }

            // the plain is walkable, the relief and the sea are not
            var start = grid.GetFootprintCenterWorld(world.StartCell, Vector2Int.one);
            Assert.IsTrue(NavMesh.SamplePosition(start, out _, 1f, NavMesh.AllAreas), "No NavMesh in the start zone.");
            for (var i = 0; i < n * n; i += 211)
            {
                // deep in the relief: its whole neighbourhood relief too (a slope's foot may be plain)
                if (!DeepRelief(world, i)) continue;
                var cell = new Vector2Int(i % n, i / n);
                var at = grid.GetFootprintCenterWorld(cell, Vector2Int.one);
                Assert.IsFalse(NavMesh.SamplePosition(at, out var hit, 0.1f, NavMesh.AllAreas) && Mathf.Abs(hit.position.y - at.y) < 0.2f,
                    $"Relief at {cell} is walkable.");
                break;
            }

            var map = applier.CurrentMap;
            Assert.IsTrue(map.HasMainBaseSpot, "The orcs' main base has no spot.");
            Assert.AreEqual(0, map.CampSpots.Length, "Camps on other islands (only the main island for now).");
            Assert.IsTrue(map.NoRaids, "Raids over land on the archipelago.");
            Assert.GreaterOrEqual(map.TownHallZone.Length, 3, "No start zone.");

            // the Town Hall goes down in the zone
            var hallData = PlaytestWorld.Building(BuildingIds.TownHall);
            var hallCell = world.StartCell - hallData.footprintSize / 2;
            var hall = PlaytestWorld.Place(hallData, hallCell);
            Assert.IsNotNull(hall, "No Town Hall in the start zone.");
            yield return null;

            // what the phone draws: the land, the relief, the sea
            var triangles = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var filter in applier.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null || !filter.sharedMesh.isReadable) continue;
                var part = filter.transform.parent != null ? filter.transform.parent.name : filter.name;
                triangles.TryGetValue(part, out var sum);
                triangles[part] = sum + filter.sharedMesh.triangles.Length / 3;
            }
            foreach (var pair in triangles) Debug.Log($"ARCHIPELAGO triangles {pair.Key}: {pair.Value}");

            yield return PlaytestCapture.Shoot("archipelago-overview", Vector3.zero, 420f, 89f, 0f);
            yield return PlaytestCapture.Shoot("archipelago-start-island", start, 70f, 50f, 20f);
            var orc = new Vector3(map.MainBaseSpot.x, 0f, map.MainBaseSpot.y);
            yield return PlaytestCapture.Shoot("archipelago-orc-island", orc, 80f, 55f, 30f);
        }

        private static bool DeepRelief(ArchipelagoMap world, int i)
        {
            var n = world.Size;
            int x = i % n, z = i / n;
            for (var dz = -2; dz <= 2; dz++)
            for (var dx = -2; dx <= 2; dx++)
            {
                int xx = x + dx, zz = z + dz;
                if (xx < 0 || zz < 0 || xx >= n || zz >= n || !world.IsRelief(zz * n + xx)) return false;
            }
            return true;
        }
    }
}
