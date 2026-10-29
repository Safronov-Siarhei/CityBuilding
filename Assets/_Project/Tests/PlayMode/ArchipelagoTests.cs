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

            // the map's code at the foot of the pause menu (the user, 2026-10-28: share a map with a friend)
            var menu = Object.FindAnyObjectByType<CityBuilder.UI.GameMenuController>();
            menu.Open();
            yield return null;
            var code = GameObject.Find("MapCode");
            Assert.IsNotNull(code, "The pause menu shows no map code.");
            StringAssert.Contains(ArchipelagoMapId.Code(MapId), code.GetComponentInChildren<UnityEngine.UI.Text>().text);
            yield return PlaytestCapture.ShootWithUi("archipelago-menu-code", start);
            menu.Close();
        }

        /// <summary>A save on the archipelago brings back its own islands: the same seed, the 500 m world, the Town Hall where it stood.</summary>
        [UnityTest]
        public IEnumerator ASave_BringsBackItsArchipelago()
        {
            const string saveName = "ArchipelagoRoundTrip";
            var world = ArchipelagoGenerator.Generate(Seed);
            var hallData = PlaytestWorld.Building(BuildingIds.TownHall);
            var hall = PlaytestWorld.Place(hallData, world.StartCell - hallData.footprintSize / 2);
            var hallAt = hall.transform.position;
            yield return null;
            try
            {
                Object.FindAnyObjectByType<GameSaveController>().SaveGame(saveName);
                GameSessionIntent.NewGameMapId = null;
                GameSessionIntent.SaveNameToLoad = saveName;
                SceneManager.LoadScene("CityBuilder", LoadSceneMode.Single);
                yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);

                Assert.AreEqual(MapId, MeshMapApplier.Instance.CurrentMapId, "The save did not bring back its archipelago.");
                Assert.AreEqual(ArchipelagoGenerator.World, GridManager.Instance.GridSize.x, "The loaded world is not the archipelago's size.");
                var n = world.Size;
                for (var i = 0; i < n * n; i += 1999)
                {
                    Assert.AreEqual(!world.Land[i], MeshMapApplier.Instance.IsWaterCell(new Vector2Int(i % n, i / n)), "The loaded islands are not the saved ones.");
                }
                var halls = Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None);
                Assert.IsTrue(System.Array.Exists(halls, b => b.Data != null && b.Data.buildingName == BuildingIds.TownHall && Vector3.Distance(b.transform.position, hallAt) < 0.1f),
                    "The Town Hall is not where it stood.");
            }
            finally
            {
                SaveSystem.Delete(saveName);
            }
        }

        /// <summary>
        /// The Shipyard (the user, 2026-10-28): its level opens the ships -- a level 1 builds the
        /// scout, not the transport; a ship built appears on the water beside it and sails where
        /// it is sent.
        /// </summary>
        [UnityTest]
        public IEnumerator AShipyard_BuildsTheShipsItsLevelOpens_AndTheySail()
        {
            var applier = MeshMapApplier.Instance;
            var grid = GridManager.Instance;
            var world = ArchipelagoGenerator.Generate(Seed);
            var ships = CityBuilder.Ships.ShipManager.Instance;
            Assert.IsNotNull(ships, "No ship manager.");
            Assert.IsTrue(ships.HasSea, "The sea has no NavMesh for ships.");
            CityBuilder.Resources.ResourceManager.Instance.SetInfiniteResources(true);

            // a Shipyard on the water by the start island: the nearest cell where its 3 x 4 fits
            var data = PlaytestWorld.Building(CityBuilder.Ships.ShipManager.ShipyardId);
            Assert.IsNotNull(data, "No Shipyard in the catalogue.");
            Vector2Int? spot = null;
            for (var r = 0; r < 120 && spot == null; r += 2)
            for (var dz = -r; dz <= r && spot == null; dz += 2)
            for (var dx = -r; dx <= r && spot == null; dx += 2)
            {
                var c = world.StartCell + new Vector2Int(dx, dz);
                var fits = true;
                for (var x = 0; x < 3 && fits; x++)
                for (var z = 0; z < 4 && fits; z++) fits = applier.IsWaterPlacementZone(c + new Vector2Int(x, z));
                if (fits) spot = c;
            }
            Assert.IsTrue(spot.HasValue, "No shore water for a Shipyard by the start island.");
            var shipyard = PlaytestWorld.Place(data, spot.Value);

            Assert.IsNull(ships.DescribeBuildBlocker(CityBuilder.Ships.ShipKind.Scout, shipyard), "A level 1 Shipyard refuses the scout.");
            Assert.IsNotNull(ships.DescribeBuildBlocker(CityBuilder.Ships.ShipKind.Transport, shipyard), "A level 1 Shipyard builds the transport.");
            var ship = ships.TryBuild(CityBuilder.Ships.ShipKind.Scout, shipyard);
            Assert.IsNotNull(ship, "The scout was not built.");
            Assert.IsTrue(applier.IsWaterCell(grid.WorldToCell(ship.transform.position)), "The scout was put on land.");

            // somewhere on open water round it: the first point of a widening ring the scout can reach
            var from = ship.transform.position;
            var sailing = false;
            for (var r = 20f; r <= 120f && !sailing; r += 10f)
            for (var k = 0; k < 8 && !sailing; k++)
            {
                var a = k * Mathf.PI / 4f;
                sailing = ship.SailTo(from + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r);
            }
            Assert.IsTrue(sailing, "The scout has no course anywhere on the sea.");
            yield return new WaitForSeconds(2f);
            Assert.Greater(Vector3.Distance(ship.transform.position, from), 2f, "The scout did not sail.");
            CityBuilder.Resources.ResourceManager.Instance.SetInfiniteResources(false);
        }

        /// <summary>
        /// Boarding and landing (the user, 2026-10-28): a group sent to a transport walks to the
        /// beach it stands at and goes aboard -- out of the world, still in its group -- and the
        /// ship lands them again only by a beach; the scout carries nobody.
        /// </summary>
        [UnityTest]
        public IEnumerator AGroup_BoardsATransportAtABeach_AndLandsAgain()
        {
            var world = ArchipelagoGenerator.Generate(Seed);
            var grid = GridManager.Instance;
            var ships = CityBuilder.Ships.ShipManager.Instance;
            var start = grid.GetFootprintCenterWorld(world.StartCell, Vector2Int.one);

            // a spot on the sea by a beach of the start island
            Vector3? sea = null;
            for (var r = 10f; r <= 160f && sea == null; r += 4f)
            for (var k = 0; k < 24 && sea == null; k++)
            {
                var a = k * Mathf.PI / 12f;
                var near = start + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
                if (ships.TrySampleSea(near, 2f, out var p) && CityBuilder.Ships.ShipManager.TryFindLandingAt(p, out _)) sea = p;
            }
            Assert.IsTrue(sea.HasValue, "No sea by a beach of the start island.");
            var transport = ships.Spawn(CityBuilder.Ships.ShipKind.Transport, sea.Value, Quaternion.identity, 100);
            var scout = ships.Spawn(CityBuilder.Ships.ShipKind.Scout, sea.Value + Vector3.right * 0.5f, Quaternion.identity, 50);
            Assert.IsTrue(ships.TryFindLanding(transport, out var beach));

            var army = CityBuilder.Combat.ArmyManager.Instance;
            var group = army.RestoreGroup(CityBuilder.Combat.SoldierType.Militia, beach, CityBuilder.Combat.TargetPriority.Units);
            var first = army.RestoreSoldier(group, beach + new Vector3(3f, 0f, 0f), 50);
            var second = army.RestoreSoldier(group, beach + new Vector3(0f, 0f, 3f), 50);
            yield return null;

            Assert.IsNotNull(ships.DescribeBoardBlocker(group, scout), "The scout takes passengers.");
            Assert.IsTrue(ships.OrderBoard(group, transport, out _), ships.DescribeBoardBlocker(group, transport));
            for (var t = 0f; t < 15f && transport.Aboard.Count < 2; t += Time.deltaTime) yield return null;
            Assert.AreEqual(2, transport.Aboard.Count, "The group did not go aboard.");
            Assert.IsFalse(first.gameObject.activeSelf, "A soldier aboard still stands in the world.");
            Assert.AreEqual(2, group.Count, "Boarding took the soldiers out of their group.");

            // out at sea there is no beach to land on
            var open = ships.SeaPoint(sea.Value + (sea.Value - beach).normalized * 40f);
            if (!CityBuilder.Ships.ShipManager.TryFindLandingAt(open, out _))
            {
                transport.transform.position = open;
                Assert.AreEqual(0, ships.TryLand(transport), "Landed with no beach in reach.");
                Assert.AreEqual(2, transport.Aboard.Count);
            }

            transport.transform.position = sea.Value;
            Assert.AreEqual(2, ships.TryLand(transport), "The transport did not land its people at the beach.");
            Assert.IsTrue(first.gameObject.activeSelf && second.gameObject.activeSelf, "A landed soldier is still put away.");
            Assert.IsFalse(MeshMapApplier.Instance.IsWaterCell(grid.WorldToCell(first.transform.position)), "A soldier was landed in the sea.");
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
