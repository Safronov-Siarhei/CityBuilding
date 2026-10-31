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

        /// <summary>
        /// Sea war, phase 4 (the user, 2026-10-28): a war ship's cannons hit an orc on the beach in
        /// reach; a ship holed through goes down with everyone aboard; a holed ship is mended only
        /// by a Shipyard, for wood.
        /// </summary>
        [UnityTest]
        public IEnumerator AWarShip_ShellsAnOrc_AndASunkShipDrownsEveryoneAboard()
        {
            var world = ArchipelagoGenerator.Generate(Seed);
            var grid = GridManager.Instance;
            var ships = CityBuilder.Ships.ShipManager.Instance;
            var start = grid.GetFootprintCenterWorld(world.StartCell, Vector2Int.one);
            Vector3? sea = null;
            for (var r = 10f; r <= 160f && sea == null; r += 4f)
            for (var k = 0; k < 24 && sea == null; k++)
            {
                var a = k * Mathf.PI / 12f;
                var near = start + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
                if (ships.TrySampleSea(near, 2f, out var p) && CityBuilder.Ships.ShipManager.TryFindLandingAt(p, out _)) sea = p;
            }
            Assert.IsTrue(sea.HasValue, "No sea by a beach of the start island.");

            // the war ship and an orc on the beach it lies off
            var war = ships.Spawn(CityBuilder.Ships.ShipKind.War, sea.Value, Quaternion.identity, CityBuilder.Ships.ShipStats.MaxHealth(CityBuilder.Ships.ShipKind.War));
            Assert.Greater(CityBuilder.Ships.ShipStats.AttackDamage(CityBuilder.Ships.ShipKind.War), 0, "The war ship has no guns in the sheet.");
            Assert.IsTrue(ships.TryFindLanding(war, out var beach));
            var orc = CityBuilder.Combat.OrcRaidManager.Instance.SpawnOrcs(beach, count: 1, level: 1);
            Assert.IsNotNull(orc, "No orc spawned on the beach.");
            yield return null;
            var full = orc.CurrentHealth;
            for (var t = 0f; t < 10f && orc != null && orc.CurrentHealth == full; t += Time.deltaTime) yield return null;
            Assert.IsTrue(orc == null || orc.CurrentHealth < full, "The war ship never fired at the orc on the beach.");
            ships.Remove(war);

            // a battleship with a soldier aboard, holed through
            var army = CityBuilder.Combat.ArmyManager.Instance;
            var group = army.RestoreGroup(CityBuilder.Combat.SoldierType.Militia, beach, CityBuilder.Combat.TargetPriority.Units);
            var soldier = army.RestoreSoldier(group, beach, 50);
            var battleship = ships.Spawn(CityBuilder.Ships.ShipKind.Battleship, sea.Value, Quaternion.identity, CityBuilder.Ships.ShipStats.MaxHealth(CityBuilder.Ships.ShipKind.Battleship));
            Assert.Greater(battleship.Capacity, 0, "The battleship carries nobody.");
            Assert.IsTrue(battleship.Embark(soldier));
            yield return null;

            // holed: mended only by a Shipyard (none here), the price in wood
            battleship.TakeDamage(10);
            Assert.AreEqual(battleship.MaxHealth - 10, battleship.Health);
            Assert.IsNotNull(ships.DescribeRepairBlocker(battleship), "Repaired with no Shipyard by.");
            var cost = CityBuilder.Ships.ShipManager.RepairCost(battleship);
            Assert.AreEqual(CityBuilder.Resources.ResourceType.Wood, cost[0].type);
            Assert.Greater(cost[0].amount, 0);

            battleship.TakeDamage(battleship.MaxHealth);
            Assert.IsTrue(battleship.Sinking, "A ship at no health is still afloat.");
            Assert.IsFalse(System.Linq.Enumerable.Contains(ships.Ships, battleship), "A sinking ship is still counted (and saved).");
            yield return null;
            Assert.IsTrue(soldier == null, "The soldier aboard survived the sinking.");
            Assert.AreEqual(0, group.Count, "The drowned soldier is still in his group.");
            for (var t = 0f; t < 6f && battleship != null; t += Time.deltaTime) yield return null;
            Assert.IsTrue(battleship == null, "The sunk ship never went down.");
        }

        /// <summary>
        /// The orcs at sea (phase 4b): their shipyard rises by the main base once the Town Hall
        /// stands; a fleet out of it sails for the beach nearest the Town Hall and puts its raid
        /// ashore on the player's island; broken, the shipyard waits to be raised again.
        /// </summary>
        [UnityTest]
        public IEnumerator AnOrcFleet_SailsFromTheirShipyard_AndLandsItsRaid()
        {
            var world = ArchipelagoGenerator.Generate(Seed);
            var hallData = PlaytestWorld.Building(BuildingIds.TownHall);
            Assert.IsNotNull(PlaytestWorld.Place(hallData, world.StartCell - hallData.footprintSize / 2), "No Town Hall.");
            var ships = CityBuilder.Ships.ShipManager.Instance;
            for (var t = 0f; t < 5f && ships.OrcShipyard == null; t += Time.deltaTime) yield return null;
            var shipyard = ships.OrcShipyard;
            Assert.IsNotNull(shipyard, "The orcs raised no shipyard by their main base.");
            Assert.IsTrue(System.Linq.Enumerable.Contains(CityBuilder.Combat.OrcCampBuilding.All, shipyard), "The orc shipyard is no target for the army.");
            var map = MeshMapApplier.Instance.CurrentMap;
            var mainBase = new Vector3(map.MainBaseSpot.x, 0f, map.MainBaseSpot.y);

            var orcsBefore = CityBuilder.Combat.OrcUnit.All.Count;
            Assert.IsTrue(ships.LaunchOrcFleet(3, 1), "No fleet sailed.");
            CityBuilder.Ships.ShipUnit transport = null;
            var escorts = 0;
            foreach (var ship in ships.Ships)
            {
                if (ship.Kind == CityBuilder.Ships.ShipKind.OrcTransport) transport = ship;
                if (ship.Kind == CityBuilder.Ships.ShipKind.OrcWar) escorts++;
            }
            Assert.IsNotNull(transport, "The fleet has no transport.");
            Assert.AreEqual(3, transport.OrcCargo, "The raid is not aboard.");
            Assert.Greater(escorts, 0, "The fleet sailed with no war ship.");
            var start = GridManager.Instance.GetFootprintCenterWorld(world.StartCell, Vector2Int.one);
            Assert.Less(Vector3.Distance(transport.OrcDestination, start), Vector3.Distance(transport.OrcDestination, mainBase),
                "The fleet is not headed for the player's island.");

            // the crossing takes minutes: put it off its beach
            transport.Stop();
            transport.transform.position = transport.OrcDestination;
            for (var t = 0f; t < 5f && !transport.OrcHomeward; t += Time.deltaTime) yield return null;
            Assert.IsTrue(transport.OrcHomeward, "The transport never put its orcs ashore.");
            Assert.AreEqual(orcsBefore + 3, CityBuilder.Combat.OrcUnit.All.Count, "The raid did not land.");
            var landed = CityBuilder.Combat.OrcUnit.All[CityBuilder.Combat.OrcUnit.All.Count - 1];
            Assert.IsFalse(MeshMapApplier.Instance.IsWaterCell(GridManager.Instance.WorldToCell(landed.transform.position)), "An orc was landed in the sea.");
            Assert.Less(Vector3.Distance(landed.transform.position, start), Vector3.Distance(landed.transform.position, mainBase), "The orcs landed off the player's island.");

            // broken, the shipyard is raised again only after a wait
            shipyard.TakeDamage(shipyard.MaxHealth);
            yield return null;
            Assert.IsNull(ships.OrcShipyard, "The broken shipyard still stands.");
            Assert.Greater(ships.OrcShipyardRebuildIn, 0f, "No wait before the orcs rebuild.");
            Assert.IsFalse(ships.LaunchOrcFleet(3, 1), "A fleet sailed with the shipyard broken.");
        }

        /// <summary>
        /// The player puts a Shipyard down as on the phone, through the placer (the user,
        /// 2026-10-29: it went down neither at the beach nor on the water): out on the shore water,
        /// and across the shoreline with at least half of it in the water; not with most of it on land.
        /// </summary>
        [UnityTest]
        public IEnumerator TheShipyard_GoesDownOnShoreWater_AndAcrossTheShoreline()
        {
            var world = ArchipelagoGenerator.Generate(Seed);
            var applier = MeshMapApplier.Instance;
            var grid = GridManager.Instance;
            var hallData = PlaytestWorld.Building(BuildingIds.TownHall);
            Assert.IsNotNull(PlaytestWorld.Place(hallData, world.StartCell - hallData.footprintSize / 2), "No Town Hall.");
            var placer = Object.FindAnyObjectByType<BuildingPlacer>();
            placer.MarkMandatoryBuildingAlreadyPlaced();
            CityBuilder.Research.ResearchManager.Instance.CompleteEverything();
            CityBuilder.Resources.ResourceManager.Instance.SetInfiniteResources(true);
            var data = PlaytestWorld.Building(CityBuilder.Ships.ShipManager.ShipyardId);
            var size = data.footprintSize;

            // spots round the start island by how much of the 3 x 4 is shore water (the rest plain land)
            Vector2Int? wet = null, straddle = null, dry = null;
            for (var r = 0; r < 140 && (wet == null || straddle == null || dry == null); r++)
            for (var dz = -r; dz <= r; dz++)
            for (var dx = -r; dx <= r; dx++)
            {
                if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != r) continue;
                var c = world.StartCell + new Vector2Int(dx, dz);
                if (!grid.IsWithinBounds(c, size) || !grid.IsAreaFree(c, size)) continue;
                int water = 0, land = 0;
                for (var x = 0; x < size.x; x++)
                for (var z = 0; z < size.y; z++)
                {
                    var cell = c + new Vector2Int(x, z);
                    if (applier.IsNoBuildCell(cell)) land = -999;
                    else if (applier.IsWaterCell(cell)) water += applier.IsWaterPlacementZone(cell) ? 1 : -999;
                    else land++;
                }
                if (water < 0 || land < 0) continue;
                var total = size.x * size.y;
                // apart from one another: the first Shipyard placed must not stand on the next spot
                bool Apart(Vector2Int? other) => other == null || Mathf.Abs(other.Value.x - c.x) >= size.x + 1 || Mathf.Abs(other.Value.y - c.y) >= size.y + 1;
                if (!Apart(wet) || !Apart(straddle) || !Apart(dry)) continue;
                if (water == total) wet ??= c;
                else if (water * 2 >= total && land > 0) straddle ??= c;
                else if (water > 0 && water * 2 < total) dry ??= c;
            }
            Assert.IsTrue(wet.HasValue && straddle.HasValue && dry.HasValue, $"No test spots: water {wet}, shoreline {straddle}, mostly land {dry}.");

            foreach (var (spot, should) in new[] { (wet.Value, true), (straddle.Value, true), (dry.Value, false) })
            {
                placer.PinnedOriginForTests = spot;
                placer.SelectBuilding(data);
                Assert.IsTrue(placer.IsSelecting, "The Shipyard cannot be picked up.");
                yield return null;
                var status = placer.StatusText;
                var before = Shipyards();
                placer.ConfirmPlacement();
                yield return null;
                Assert.AreEqual(should, Shipyards() > before, $"Shipyard at {spot}: placed should be {should}; the placer said «{status}».");
                placer.ClearSelection();
            }
            placer.PinnedOriginForTests = null;

            // aimed by a tap on the water, as with a mouse: the ray must find the water's surface,
            // not the seabed or nothing (the islands are the only ground mesh there is)
            // a free spot all on the shore water, found now the first ones stand
            Vector2Int? aimed = null;
            for (var r = 0; r < 140 && aimed == null; r++)
            for (var dz = -r; dz <= r && aimed == null; dz++)
            for (var dx = -r; dx <= r && aimed == null; dx++)
            {
                if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != r) continue;
                var c = world.StartCell + new Vector2Int(dx, dz);
                if (!grid.IsWithinBounds(c, size) || !grid.IsAreaFree(c, size)) continue;
                var all = true;
                for (var x = 0; x < size.x && all; x++)
                for (var z = 0; z < size.y && all; z++) all = applier.IsWaterPlacementZone(c + new Vector2Int(x, z)) && !applier.IsNoBuildCell(c + new Vector2Int(x, z));
                if (all) aimed = c;
            }
            Assert.IsTrue(aimed.HasValue, "No free shore-water spot left.");
            // the cell the placer centres the footprint on (BuildingPlacer.OriginForCentredFootprint)
            var target = grid.GetFootprintCenterWorld(aimed.Value + new Vector2Int(size.x / 2, size.y / 2), Vector2Int.one);
            target.y = CityBuilder.Ships.ShipManager.WaterLevel;
            var camera = Camera.main;
            camera.transform.position = target + new Vector3(0f, 30f, -18f);
            camera.transform.LookAt(target);
            placer.SelectBuilding(data);
            var count = Shipyards();
            placer.HandleWorldTap(camera.WorldToScreenPoint(target));
            yield return null;
            var screen = camera.WorldToScreenPoint(target);
            var ray = camera.ScreenPointToRay(screen);
            var onGround = applier.TryRaycastGround(ray, out var groundHit);
            new Plane(Vector3.up, target).Raycast(ray, out var toSea);
            var why = $"screen {screen} of {Screen.width}x{Screen.height}, ground hit {onGround} at {groundHit.point}, sea cell {grid.WorldToCell(ray.GetPoint(toSea))}, "
                + $"selected {placer.SelectedBuilding?.buildingName} water {placer.SelectedBuilding?.isWaterCategory}, touch {placer.UsesTouchAiming}, sea {CityBuilder.Ships.ShipManager.Instance.HasSea}";
            Assert.Greater(Shipyards(), count, $"A tap on the water at {aimed} put no Shipyard there; {why}");
        }

        /// <summary>
        /// The user, 2026-10-31: three dungeon entrances on three islands, level 1 nearest the start,
        /// none built over and no tree in them; a hero walking up opens the entrance's card. And the
        /// denser forest and boulders, drawn only near the camera.
        /// </summary>
        [UnityTest]
        public IEnumerator DungeonEntrances_AndTheForest_DrawnNearTheCameraOnly()
        {
            var world = ArchipelagoGenerator.Generate(Seed);
            var applier = MeshMapApplier.Instance;
            var balance = CityBuilder.Core.BalanceConfig.Instance;
            var entrances = new System.Collections.Generic.List<CityBuilder.Dungeon.DungeonEntrance>(CityBuilder.Dungeon.DungeonEntrance.All);
            Assert.AreEqual(balance.DungeonEntranceCount, entrances.Count, "Not every dungeon entrance is on the map.");
            entrances.Sort((a, b) => a.Level.CompareTo(b.Level));
            var start = new Vector2(world.StartCell.x, world.StartCell.y);
            var last = -1f;
            for (var k = 0; k < entrances.Count; k++)
            {
                var e = entrances[k];
                Assert.AreEqual(k + 1, e.Level, "The levels are not 1, 2, 3.");
                Assert.AreEqual(CityBuilder.Dungeon.DungeonTravel.FirstDungeonId, e.DungeonId);
                Assert.IsNotNull(e.transform.Find("Model"), "An entrance has no mine model.");
                var d = Vector2.Distance(new Vector2(e.Cell.x, e.Cell.y), start);
                Assert.Greater(d, last, "A deeper dungeon is nearer the start than a shallower one.");
                last = d;
                for (var x = 0; x < DungeonEntrances.Footprint.x; x++)
                for (var z = 0; z < DungeonEntrances.Footprint.y; z++)
                {
                    var cell = e.Cell + new Vector2Int(x, z);
                    Assert.IsTrue(applier.IsNoBuildCell(cell), "One may build over a dungeon entrance.");
                    Assert.IsFalse(applier.IsWaterCell(cell), "A dungeon entrance stands in the water.");
                    var c = GridManager.Instance.GetFootprintCenterWorld(cell, Vector2Int.one);
                    Assert.IsTrue(applier.TryRaycastGround(new Ray(c + Vector3.up * 50f, Vector3.down), out var hit)
                        && ArchipelagoMeshes.IsGrassUv(hit.textureCoord), $"A dungeon entrance stands on sand at {cell}.");
                }
                // its mouth (the model's -z) to the camera, which never turns
                var view = Camera.main.transform.forward;
                view.y = 0f;
                Assert.Greater(Vector3.Dot(e.transform.forward, view.normalized), 0.99f, "A dungeon entrance does not face the camera.");
                foreach (var node in ResourceNode.All)
                {
                    if (node == null) continue;
                    var c = GridManager.Instance.WorldToCell(node.transform.position) - e.Cell;
                    Assert.IsFalse(c.x >= 0 && c.y >= 0 && c.x < DungeonEntrances.Footprint.x && c.y < DungeonEntrances.Footprint.y,
                        "A tree or boulder grows in a dungeon's mouth.");
                }
            }

            // a hero at the mouth opens the card; «Войти» is live
            var card = CityBuilder.UI.DungeonEntranceCard.Instance;
            Assert.IsNotNull(card, "No dungeon entrance card in the scene.");
            var first = entrances[0];
            var towards = new Vector3(start.x, 0f, start.y) - new Vector3(first.Cell.x + 2.5f, 0f, first.Cell.y + 2.5f);
            var spot = first.transform.position + towards.normalized * 3.2f;
            var roster = CityBuilder.Combat.HeroRoster.Instance;
            var king = roster.HeroOf(CityBuilder.Combat.SoldierType.King) ?? roster.Restore(CityBuilder.Combat.SoldierType.King, spot, 1, 0, 50, false, 0f);
            Assert.IsNotNull(king, "No King to send down.");
            var agent = king.Unit.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled) agent.Warp(spot);
            else king.Unit.transform.position = spot;
            yield return new WaitForSeconds(1.2f);
            Assert.IsTrue(first.HeroNear(), "The King at the mouth is not near the entrance.");
            Assert.IsTrue(card.IsOpen && card.Shown == first, "A hero at the mouth does not open the entrance's card.");
            card.Close();

            // the forest and the boulders: as dense as the sheet says, drawn only near the camera
            int trees = 0, rocks = 0, drawn = 0, hidden = 0;
            foreach (var node in ResourceNode.All)
            {
                if (node == null) continue;
                if (node.ResourceType == CityBuilder.Resources.ResourceType.Wood) trees++;
                else if (node.ResourceType == CityBuilder.Resources.ResourceType.Stone) rocks++;
                var r = node.GetComponentInChildren<Renderer>();
                if (r == null) continue;
                if (r.enabled) drawn++;
                else hidden++;
            }
            Assert.GreaterOrEqual(trees, balance.ArchipelagoTreesMin * 0.9f, $"Only {trees} trees on the archipelago.");
            Assert.GreaterOrEqual(rocks, balance.ArchipelagoRocksMin * 0.8f, $"Only {rocks} boulders on the archipelago.");
            Assert.Greater(drawn, 0, "No tree or boulder is drawn near the camera.");
            Assert.Greater(hidden, drawn, "The trees and boulders far from the camera are still drawn.");
        }

        /// <summary>
        /// Phase 5 (the user, 2026-10-28): fertile soil lies on the start island and a Farm on it
        /// yields more (a Farm elsewhere does not); a shoal lies by the start island, a Fisher's Hut
        /// by it catches more and fishes it out, and the shoal goes; the lighthouse stands only at
        /// the shore and, for coins, opens the fog over a spot in reach, then has to recover.
        /// </summary>
        [UnityTest]
        public IEnumerator FertileLand_FishShoals_AndTheLighthouse()
        {
            var world = ArchipelagoGenerator.Generate(Seed);
            var applier = MeshMapApplier.Instance;
            var grid = GridManager.Instance;
            var ships = CityBuilder.Ships.ShipManager.Instance;
            CityBuilder.Resources.ResourceManager.Instance.SetInfiniteResources(true);

            // fertile soil near the start, and a Farm on it beats a Farm off it
            Vector2Int? fertile = null, plain = null;
            var farm = PlaytestWorld.Building("Farm");
            var size = farm.footprintSize;
            for (var r = 0; r < 60 && (fertile == null || plain == null); r++)
            for (var dz = -r; dz <= r; dz++)
            for (var dx = -r; dx <= r; dx++)
            {
                if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != r) continue;
                var c = world.StartCell + new Vector2Int(dx, dz);
                int rich = 0, poor = 0, bad = 0;
                for (var x = 0; x < size.x; x++)
                for (var z = 0; z < size.y; z++)
                {
                    var cell = c + new Vector2Int(x, z);
                    if (applier.IsWaterCell(cell) || applier.IsNoBuildCell(cell) || !grid.IsAreaFree(cell, Vector2Int.one)) bad++;
                    else if (applier.IsFertileCell(cell)) rich++;
                    else poor++;
                }
                if (bad > 0) continue;
                if (poor == 0) fertile ??= c;
                if (rich == 0 && Vector2Int.Distance(c, world.StartCell) > 6) plain ??= c;
            }
            Assert.IsTrue(fertile.HasValue, "No fertile soil within 60 cells of the start.");
            Assert.IsTrue(plain.HasValue);
            var richFarm = PlaytestWorld.Place(farm, fertile.Value).GetComponent<CityBuilder.Citizens.ProductionBuilding>();
            var poorFarm = PlaytestWorld.Place(farm, plain.Value).GetComponent<CityBuilder.Citizens.ProductionBuilding>();
            Assert.AreEqual(1f + CityBuilder.Core.BalanceConfig.Instance.FertileFarmBonus, richFarm.SiteMultiplier(), 0.001f, "A Farm on fertile soil yields no more.");
            Assert.AreEqual(1f, poorFarm.SiteMultiplier(), 0.001f, "A Farm off fertile soil yields more.");

            // a shoal by the start island; a Fisher's Hut by it catches double and fishes it out
            var start = grid.GetFootprintCenterWorld(world.StartCell, Vector2Int.one);
            CityBuilder.Ships.ShipManager.Shoal near = null;
            foreach (var shoal in ships.Shoals)
            {
                if (shoal.Alive && (near == null || Vector3.Distance(shoal.At, start) < Vector3.Distance(near.At, start))) near = shoal;
            }
            Assert.IsNotNull(near, "No fish shoals on the archipelago.");
            // a tap on fertile soil lights the whole patch's edge
            var patch = applier.FertilePatchAt(fertile.Value);
            Assert.Greater(patch.Count, 4, "The fertile patch under the Farm is a scrap.");
            var flash = FertileLand.Flash(patch, applier);
            Assert.IsNotNull(flash, "The fertile patch's edge does not light up.");
            Assert.Greater(flash.GetComponent<MeshFilter>().sharedMesh.vertexCount, patch.Count / 2, "The lit edge does not go round the patch.");
            Assert.Less(Vector3.Distance(near.At, start), 80f, "No shoal by the start island.");
            Assert.IsTrue(ShipManagerFishVisible(near), "The shoal cannot be seen.");
            // shoals only on the light coastal water (the user, 2026-10-31)
            foreach (var shoal in ships.Shoals)
                if (shoal.Alive) Assert.IsTrue(applier.IsWaterPlacementZone(grid.WorldToCell(shoal.At)), $"A shoal out at sea at {shoal.At}.");
            Assert.AreEqual(1f + CityBuilder.Core.BalanceConfig.Instance.FishShoalBonus, ships.FishMultiplier(near.At), 0.001f, "A hut by the shoal catches no more.");
            ships.TakeFish(near.At, near.Stock);
            Assert.IsFalse(near.Alive, "The fished-out shoal is still there.");
            Assert.AreEqual(1f, ships.FishMultiplier(near.At), 0.001f, "A fished-out shoal still adds to the catch.");
            Assert.Greater(near.RespawnIn, 0f, "The shoal will not turn up again.");

            // the lighthouse: at the shore only; lights a spot in reach for coins, then recovers
            var lighthouseData = PlaytestWorld.Building(CityBuilder.Ships.LighthouseAim.BuildingId);
            Assert.IsNotNull(lighthouseData, "No lighthouse in the build menu.");
            Vector2Int? coast = null;
            for (var r = 0; r < 120 && coast == null; r++)
            for (var dz = -r; dz <= r && coast == null; dz++)
            for (var dx = -r; dx <= r && coast == null; dx++)
            {
                if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != r) continue;
                var c = world.StartCell + new Vector2Int(dx, dz);
                var ok = grid.IsAreaFree(c, lighthouseData.footprintSize);
                for (var x = 0; x < lighthouseData.footprintSize.x && ok; x++)
                for (var z = 0; z < lighthouseData.footprintSize.y && ok; z++)
                    ok = !applier.IsWaterCell(c + new Vector2Int(x, z)) && !applier.IsNoBuildCell(c + new Vector2Int(x, z));
                if (ok && applier.IsWaterCell(c + new Vector2Int(-1, 0))) coast = c;
            }
            Assert.IsTrue(coast.HasValue, "No shore for a lighthouse.");
            var lighthouse = PlaytestWorld.Place(lighthouseData, coast.Value);
            yield return null;
            var fog = FogOfWarManager.Instance;
            var target = lighthouse.transform.position + new Vector3(-50f, 0f, 0f);
            var coins = CityBuilder.Resources.ResourceManager.Instance;
            coins.SetInfiniteResources(false);
            coins.SetAmount(CityBuilder.Resources.ResourceType.Coins, 50);
            Assert.IsFalse(CityBuilder.Ships.LighthouseAim.LightAt(lighthouse, lighthouse.transform.position + new Vector3(-300f, 0f, 0f)), "The lighthouse lit a spot out of its reach.");
            Assert.AreEqual(50, coins.GetAmount(CityBuilder.Resources.ResourceType.Coins), "Paid for a light out of reach.");
            Assert.IsTrue(CityBuilder.Ships.LighthouseAim.LightAt(lighthouse, target), CityBuilder.Ships.LighthouseAim.DescribeBlocker(lighthouse));
            Assert.AreEqual(50 - CityBuilder.Ships.LighthouseAim.Price, coins.GetAmount(CityBuilder.Resources.ResourceType.Coins), "The light was not paid for.");
            if (fog != null) Assert.IsTrue(fog.IsCellRevealed(grid.WorldToCell(target)), "The lit spot is still under the fog.");
            Assert.IsNotNull(CityBuilder.Ships.LighthouseAim.DescribeBlocker(lighthouse), "The lighthouse lights again at once.");
        }

        private static bool ShipManagerFishVisible(CityBuilder.Ships.ShipManager.Shoal shoal) =>
            shoal.Visual != null && shoal.Visual.GetComponentsInChildren<MeshRenderer>().Length > 0;

        private static int Shipyards()
        {
            var count = 0;
            foreach (var b in Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
            {
                if (b.Data != null && b.Data.buildingName == CityBuilder.Ships.ShipManager.ShipyardId) count++;
            }
            return count;
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
