using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CityBuilder.Buildings;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using CityBuilder.Saving;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// Map 4, the Two Rivers Valley (Tools/blender/map4.py), the first designed map (2026-10-20):
    /// an island, rivers and mountains nobody walks through, fords everyone walks, narrow reaches
    /// a bridge spans, and the orcs on the map's own spots -- the main base in the cirque with its
    /// gate to the south. The positions are the generator's landmarks; Blender (x, y) is Unity (x, z).
    /// </summary>
    public class Map4Tests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map4";

        private static readonly Vector3 TownMeadow = new Vector3(0f, 0f, -22f);
        private static readonly Vector3 Lake = new Vector3(-57f, 0f, 0f);
        private static readonly Vector3 WestBridgeReach = new Vector3(-63f, 0f, -22.3f);
        private static readonly Vector3 NorthFord = new Vector3(-49.7f, 0f, 19.6f);
        private static readonly Vector3 WestMountains = new Vector3(-60f, 0f, 76f);
        private static readonly Vector3 EastMountains = new Vector3(60f, 0f, 76f);
        private static readonly Vector3 OpenSea = new Vector3(0f, 0f, -98f);
        private static readonly Vector3 MainBaseSpot = new Vector3(0f, 0f, 67f);

        [UnitySetUp]
        public IEnumerator LoadMap4()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            GameSessionIntent.NewGameMapId = MapId;
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);
            ModalGate.SetBlocked(false);
            if (OrcRaidManager.Instance != null) OrcRaidManager.Instance.RaidsSuspended = true;
        }

        [Test]
        public void TheMapLoads_AndNewGamesStartOnIt()
        {
            Assert.AreEqual(MapId, MeshMapApplier.Instance.CurrentMapId, "Map 4 was not applied -- is it in Resources/MeshMaps?");
            Assert.AreEqual(MapId, MapSelector.PickForNewGame().MapId, "New games must start on the Two Rivers Valley.");
        }

        [Test]
        public void LandWaterAndRelief_AreWhereTheGeneratorPutThem()
        {
            var grid = GridManager.Instance;
            var map = MeshMapApplier.Instance;

            Assert.IsTrue(map.IsGroundAt(TownMeadow) && !map.IsNoBuildCell(grid.WorldToCell(TownMeadow)), "The town's meadow is not open ground.");
            foreach (var (name, point) in new[] { ("lake", Lake), ("west river", WestBridgeReach), ("sea", OpenSea) })
            {
                Assert.IsTrue(map.IsWaterCell(grid.WorldToCell(point)), $"The {name} reads as land -- the map is turned or mirrored.");
            }
            foreach (var (name, point) in new[] { ("west mountains", WestMountains), ("east mountains", EastMountains) })
            {
                Assert.IsTrue(map.IsNoBuildCell(grid.WorldToCell(point)), $"The {name} can be built on.");
                Assert.IsFalse(NavMesh.SamplePosition(point, out _, 1f, NavMesh.AllAreas), $"The NavMesh covers the {name}.");
            }
        }

        [Test]
        public void AFord_IsWalkedButNotBuiltOn()
        {
            var grid = GridManager.Instance;
            var map = MeshMapApplier.Instance;
            var cell = grid.WorldToCell(NorthFord);
            Assert.IsFalse(map.IsWaterCell(cell), "The ford reads as water -- nobody could cross it.");
            Assert.IsTrue(NavMesh.SamplePosition(NorthFord, out _, 0.6f, NavMesh.AllAreas), "No NavMesh on the ford.");
            Assert.IsTrue(map.IsNoBuildCell(cell), "A building could stand on the ford and shut the crossing.");
        }

        [Test]
        public void TheRiver_IsWalkedRoundByTheFordsNotThrough()
        {
            var east = WestBridgeReach + new Vector3(9f, 0f, 0f);
            var west = WestBridgeReach + new Vector3(-9f, 0f, 0f);
            var length = PathLength(east, west, out var complete);
            Assert.IsTrue(complete, "There is no way across the west river at all -- the fords do not join the banks.");
            Assert.Greater(length, 40f, $"The way across the river is {length:0} m, about the straight line -- the river does not stop anyone.");
        }

        [UnityTest]
        public IEnumerator ABridge_OpensAShorterWayAcross()
        {
            var grid = GridManager.Instance;
            var map = MeshMapApplier.Instance;
            var east = WestBridgeReach + new Vector3(9f, 0f, 0f);
            var west = WestBridgeReach + new Vector3(-9f, 0f, 0f);
            var before = PathLength(east, west, out _);

            var bridge = BridgeData();
            var row = grid.WorldToCell(WestBridgeReach).y;
            var placed = 0;
            for (var x = grid.WorldToCell(west).x; x <= grid.WorldToCell(east).x; x++)
            {
                var cell = new Vector2Int(x, row);
                if (!map.IsWaterCell(cell)) continue;
                Assert.IsTrue(map.IsWaterPlacementZone(cell), $"The bridge reach at {cell} is too deep for a bridge.");
                PlaytestWorld.Place(bridge, cell);
                placed++;
            }
            Assert.Greater(placed, 0, "The bridge reach has no water to span.");
            Assert.LessOrEqual(placed, 8, $"The bridge reach is {placed} cells wide -- too long for a narrow reach.");

            yield return new WaitForSeconds(2.5f);
            var after = PathLength(east, west, out var complete);
            Assert.IsTrue(complete, "With the bridge built there is no way across.");
            Assert.Less(after, before * 0.6f, $"The bridge did not shorten the way across ({before:0} m before, {after:0} m after).");
        }

        [UnityTest]
        public IEnumerator TheOrcs_StandOnTheMapsSpots_TheMainBaseInTheCirque()
        {
            var townHall = PlaytestWorld.Building("Castle");
            PlaytestWorld.Place(townHall, GridManager.Instance.WorldToCell(TownMeadow) - townHall.footprintSize / 2);
            for (var frame = 0; frame < 240 && !OrcRaidManager.Instance.PortalsPlaced; frame++) yield return null;
            Assert.IsTrue(OrcRaidManager.Instance.PortalsPlaced, "The portals never opened.");

            var main = OrcPortal.All.FirstOrDefault(p => p.IsMain);
            Assert.IsNotNull(main, "The main base did not open in the cirque.");
            var offset = main.transform.position - MainBaseSpot;
            offset.y = 0f;
            Assert.Less(offset.magnitude, 4f, $"The main base stands {offset.magnitude:0.0} m off its spot in the cirque.");
            Assert.AreEqual(0, OrcBaseLayout.QuarterTurns(main.transform.eulerAngles.y), "The main base's gate does not face south, out of the cirque.");
            var mainCamp = OrcCamp.Of(main);
            Assert.AreEqual(mainCamp.Layout.Pieces.Count, mainCamp.Buildings.Count, "The main base does not fit the cirque whole.");

            var spots = MeshMapApplier.Instance.CurrentMap.CampSpots;
            var camps = OrcPortal.All.Where(p => !p.IsMain).ToList();
            Assert.AreEqual(spots.Length, camps.Count, "Not every camp spot opened a camp.");
            foreach (var spot in spots)
            {
                var at = new Vector3(spot.x, 0f, spot.y);
                var nearest = camps.Min(p => Vector3.Distance(Flat(p.transform.position), at));
                Assert.Less(nearest, 12f, $"No camp opened at the map's spot {spot}.");
            }
        }

        [UnityTest]
        public IEnumerator Photograph_Map4()
        {
            if (!PlaytestCapture.Available) Assert.Ignore("No graphics device: run without -nographics to photograph.");
            var townHall = PlaytestWorld.Building("Castle");
            PlaytestWorld.Place(townHall, GridManager.Instance.WorldToCell(TownMeadow) - townHall.footprintSize / 2);
            for (var frame = 0; frame < 240 && !OrcRaidManager.Instance.PortalsPlaced; frame++) yield return null;
            yield return new WaitForSeconds(0.5f);
            yield return PlaytestCapture.Shoot("map4-overview", new Vector3(0f, 0f, 4f), 200f, 55f, 0f);
            yield return PlaytestCapture.Shoot("map4-town", TownMeadow, 32f, 50f, 0f, 60f);
            yield return PlaytestCapture.Shoot("map4-cirque", new Vector3(0f, 0f, 56f), 60f, 55f, 0f, 60f);
            yield return PlaytestCapture.Shoot("map4-ford", NorthFord, 26f, 50f, 0f, 60f);
            yield return PlaytestCapture.Shoot("map4-west-camp", new Vector3(-73f, 0f, 30f), 40f, 52f, 0f, 60f);
        }

        private static BuildingData BridgeData()
        {
            var bridge = PlaytestWorld.Building("Bridge");
            Assert.IsNotNull(bridge, "No Bridge in the build menu.");
            Assert.IsTrue(bridge.providesWalkableSurface, "The Bridge is not walkable ground.");
            return bridge;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private static NavMeshPath _path;

        private static float PathLength(Vector3 from, Vector3 to, out bool complete)
        {
            Assert.IsTrue(NavMesh.SamplePosition(from, out var a, 3f, NavMesh.AllAreas), $"No NavMesh near {from}.");
            Assert.IsTrue(NavMesh.SamplePosition(to, out var b, 3f, NavMesh.AllAreas), $"No NavMesh near {to}.");
            _path ??= new NavMeshPath();
            NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, _path);
            complete = _path.status == NavMeshPathStatus.PathComplete;
            var length = 0f;
            for (var i = 1; i < _path.corners.Length; i++) length += Vector3.Distance(_path.corners[i - 1], _path.corners[i]);
            return length;
        }
    }
}
