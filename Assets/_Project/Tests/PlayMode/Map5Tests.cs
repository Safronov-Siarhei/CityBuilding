using System.Collections;
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
    /// Map 5, the Caldera (Tools/blender/map5.py, 2026-10-20): the town on the floor of an old
    /// volcano round a crater lake, a ring of mountains that only four gorges cut through, the
    /// main base in a valley walled by two spurs that opens only to the north gorge, the camps
    /// outside the ring. The positions are the generator's landmarks; Blender (x, y) is Unity (x, z).
    /// </summary>
    public class Map5Tests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map5";

        private static readonly Vector3 TownMeadow = new Vector3(-6f, 0f, -25f);
        private static readonly Vector3 Lake = new Vector3(15f, 0f, 2f);
        private static readonly Vector3 OpenSea = new Vector3(0f, 0f, -98f);
        private static readonly Vector3 RingNorthWest = new Vector3(-33.9f, 0f, 19.9f);
        private static readonly Vector3 OldCone = new Vector3(33.9f, 0f, 19.9f);
        private static readonly Vector3 RingSouthWest = new Vector3(-33.9f, 0f, -47.9f);
        private static readonly Vector3 OutsideNorthWest = new Vector3(-50f, 0f, 30f);
        private static readonly Vector3 MainValley = new Vector3(0f, 0f, 50f);
        private static readonly Vector3 WestOfTheSpur = new Vector3(-50f, 0f, 50f);
        private static readonly Vector3 InnerFord = new Vector3(35.4f, 0f, -5.6f);
        private static readonly Vector3 BridgeReach = new Vector3(63.6f, 0f, -10.6f);
        private static readonly Vector3[] Gorges = { new Vector3(0f, 0f, 30f), new Vector3(-48f, 0f, -15f), new Vector3(48f, 0f, -1f), new Vector3(-4f, 0f, -62f) };
        private static readonly Vector2[] VikingBeaches = { new Vector2(0f, -90f), new Vector2(-68f, -74f), new Vector2(72f, 72f) };
        private static readonly Vector3 MainBaseSpot = new Vector3(0f, 0f, 66f);
        private const int CampCount = 4;

        [UnitySetUp]
        public IEnumerator LoadMap5()
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
        public void TheMapLoads_AndIsTheDefaultInThePicker()
        {
            Assert.AreEqual(MapId, MeshMapApplier.Instance.CurrentMapId, "Map 5 was not applied -- is it in Resources/MeshMaps?");
            var choosable = MapSelector.Choosable().ConvertAll(m => m.MapId);
            Assert.AreEqual(MapId, choosable.FirstOrDefault(), "The newest map heads the picker.");
        }

        [Test]
        public void LandWaterAndTheRing_AreWhereTheGeneratorPutThem()
        {
            var grid = GridManager.Instance;
            var map = MeshMapApplier.Instance;

            Assert.IsTrue(map.IsGroundAt(TownMeadow) && !map.IsNoBuildCell(grid.WorldToCell(TownMeadow)), "The caldera's floor is not open ground.");
            foreach (var (name, point) in new[] { ("crater lake", Lake), ("sea", OpenSea) })
            {
                Assert.IsTrue(map.IsWaterCell(grid.WorldToCell(point)), $"The {name} reads as land -- the map is turned or mirrored.");
            }
            foreach (var (name, point) in new[] { ("ring in the north-west", RingNorthWest), ("old cone", OldCone), ("ring in the south-west", RingSouthWest) })
            {
                Assert.IsTrue(map.IsNoBuildCell(grid.WorldToCell(point)), $"The {name} can be built on.");
                Assert.IsFalse(NavMesh.SamplePosition(point, out _, 1f, NavMesh.AllAreas), $"The NavMesh covers the {name}.");
            }
            foreach (var gorge in Gorges)
            {
                Assert.IsTrue(NavMesh.SamplePosition(gorge, out _, 1f, NavMesh.AllAreas), $"The gorge at {gorge} is not walkable.");
            }
        }

        [Test]
        public void TheRing_IsLeftOnlyThroughTheGorges()
        {
            // Over the north-west of the ring as the crow flies it is about 67 m; the way round is
            // out through the west gorge and up the outside.
            var length = PathLength(TownMeadow, OutsideNorthWest, out var complete);
            Assert.IsTrue(complete, "There is no way out of the caldera to the north-west at all.");
            Assert.Greater(length, Vector3.Distance(TownMeadow, OutsideNorthWest) * 1.3f, $"The way out is {length:0} m, about the straight line -- the ring does not stop anyone.");
        }

        [Test]
        public void TheMainBasesValley_OpensOnlyToTheNorthGorge()
        {
            var straight = Vector3.Distance(WestOfTheSpur, MainValley);
            var length = PathLength(WestOfTheSpur, MainValley, out var complete);
            Assert.IsTrue(complete, "The main base's valley cannot be reached from the west at all.");
            Assert.Greater(length, straight * 2.5f, $"From beyond the west spur to the main base is {length:0} m ({straight:0} m straight) -- the spur does not wall the valley.");
        }

        [Test]
        public void TheInnerFord_IsWalkedButNotBuiltOn()
        {
            var grid = GridManager.Instance;
            var map = MeshMapApplier.Instance;
            var cell = grid.WorldToCell(InnerFord);
            Assert.IsFalse(map.IsWaterCell(cell), "The ford reads as water -- nobody could cross it.");
            Assert.IsTrue(NavMesh.SamplePosition(InnerFord, out _, 0.6f, NavMesh.AllAreas), "No NavMesh on the ford.");
            Assert.IsTrue(map.IsNoBuildCell(cell), "A building could stand on the ford and shut the crossing.");
        }

        [UnityTest]
        public IEnumerator ABridge_OverTheCreek_OpensAShorterWayAcross()
        {
            var grid = GridManager.Instance;
            var map = MeshMapApplier.Instance;
            var north = BridgeReach + new Vector3(0f, 0f, 8f);
            var south = BridgeReach + new Vector3(0f, 0f, -8f);
            var before = PathLength(north, south, out var completeBefore);
            Assert.IsTrue(completeBefore, "The fords do not join the creek's banks.");
            Assert.Greater(before, 30f, $"The way across the creek is {before:0} m -- it does not stop anyone.");

            var bridge = PlaytestWorld.Building("Bridge");
            Assert.IsNotNull(bridge, "No Bridge in the build menu.");
            var column = grid.WorldToCell(BridgeReach).x;
            var placed = 0;
            for (var z = grid.WorldToCell(south).y; z <= grid.WorldToCell(north).y; z++)
            {
                var cell = new Vector2Int(column, z);
                if (!map.IsWaterCell(cell)) continue;
                Assert.IsTrue(map.IsWaterPlacementZone(cell), $"The bridge reach at {cell} is too deep for a bridge.");
                PlaytestWorld.Place(bridge, cell);
                placed++;
            }
            Assert.Greater(placed, 0, "The bridge reach has no water to span.");
            Assert.LessOrEqual(placed, 8, $"The bridge reach is {placed} cells wide -- too long for a narrow reach.");

            yield return new WaitForSeconds(2.5f);
            var after = PathLength(north, south, out var complete);
            Assert.IsTrue(complete, "With the bridge built there is no way across.");
            Assert.Less(after, before * 0.6f, $"The bridge did not shorten the way across ({before:0} m before, {after:0} m after).");
        }

        [UnityTest]
        public IEnumerator TheOrcs_StandOnTheMapsSpots_OutsideTheRing()
        {
            var townHall = PlaytestWorld.Building("Castle");
            PlaytestWorld.Place(townHall, GridManager.Instance.WorldToCell(TownMeadow) - townHall.footprintSize / 2);
            for (var frame = 0; frame < 240 && !OrcRaidManager.Instance.PortalsPlaced; frame++) yield return null;
            Assert.IsTrue(OrcRaidManager.Instance.PortalsPlaced, "The portals never opened.");

            var main = OrcPortal.All.FirstOrDefault(p => p.IsMain);
            Assert.IsNotNull(main, "The main base did not open in its valley.");
            var offset = main.transform.position - MainBaseSpot;
            offset.y = 0f;
            Assert.Less(offset.magnitude, 4f, $"The main base stands {offset.magnitude:0.0} m off its spot.");
            Assert.AreEqual(0, OrcBaseLayout.QuarterTurns(main.transform.eulerAngles.y), "The main base's gate does not face south, to the north gorge.");
            var mainCamp = OrcCamp.Of(main);
            Assert.AreEqual(mainCamp.Layout.Pieces.Count, mainCamp.Buildings.Count, "The main base does not fit its valley whole.");

            var map = MeshMapApplier.Instance.CurrentMap;
            Assert.AreEqual(CampCount, map.CampSpots.Length, "The Caldera has four camps.");
            var camps = OrcCamp.All.Where(c => !c.IsMain).ToList();
            Assert.AreEqual(CampCount, camps.Count, "Not every camp spot opened a camp.");
            for (var i = 0; i < map.CampSpots.Length; i++)
            {
                var at = new Vector3(map.CampSpots[i].x, 0f, map.CampSpots[i].y);
                var camp = camps.OrderBy(c => Vector3.Distance(Flat(c.LocalToWorld(Vector2.zero)), at)).First();
                var off = Vector3.Distance(Flat(camp.LocalToWorld(Vector2.zero)), at);
                Assert.Less(off, 3f, $"The camp for the spot {map.CampSpots[i]} stands {off:0.0} m off it -- its ground was not kept for it.");
                Assert.AreEqual(map.CampLevels[i], camp.MaxLevel, $"The camp at {map.CampSpots[i]} cannot grow to level {map.CampLevels[i]} -- something stands on its square.");
                foreach (var beach in VikingBeaches)
                {
                    Assert.Greater(Vector2.Distance(map.CampSpots[i], beach), 25f, $"The camp at {map.CampSpots[i]} sits on the Vikings' beach at {beach}.");
                }
            }
        }

        [Test]
        public void TheTownHall_GoesInTheStartZone_FarFromTheOrcs()
        {
            var map = MeshMapApplier.Instance.CurrentMap;
            Assert.GreaterOrEqual(map.TownHallZone.Length, 3, "The map has no start zone.");
            var grid = GridManager.Instance;
            var size = PlaytestWorld.Building("Castle").footprintSize;
            Vector2Int CellAt(Vector3 p) => grid.WorldToCell(p) - size / 2;

            Assert.IsTrue(StartZone.Allows(CellAt(TownMeadow), size), "The Town Hall may not go on the town's meadow.");
            Assert.IsFalse(StartZone.Allows(CellAt(MainValley), size), "The Town Hall may go outside the start zone, in the orcs' valley.");
            var minDistance = BalanceConfig.Instance.TownHallMinDistanceToOrcs;
            foreach (var corner in map.TownHallZone)
            {
                foreach (var camp in map.CampSpots.Append(map.MainBaseSpot))
                {
                    Assert.GreaterOrEqual(Vector2.Distance(corner, camp), minDistance - 2f,
                        $"The start zone's corner {corner} reaches within {minDistance} m of the orcs at {camp}.");
                }
            }
        }

        [UnityTest]
        public IEnumerator Photograph_Map5()
        {
            if (!PlaytestCapture.Available) Assert.Ignore("No graphics device: run without -nographics to photograph.");
            var townHall = PlaytestWorld.Building("Castle");
            PlaytestWorld.Place(townHall, GridManager.Instance.WorldToCell(TownMeadow) - townHall.footprintSize / 2);
            for (var frame = 0; frame < 240 && !OrcRaidManager.Instance.PortalsPlaced; frame++) yield return null;
            yield return new WaitForSeconds(0.5f);
            yield return PlaytestCapture.Shoot("map5-overview", new Vector3(0f, 0f, -4f), 200f, 55f, 0f);
            yield return PlaytestCapture.Shoot("map5-town", TownMeadow, 32f, 50f, 0f, 60f);
            yield return PlaytestCapture.Shoot("map5-main-base", new Vector3(0f, 0f, 56f), 60f, 55f, 0f, 60f);
            yield return PlaytestCapture.Shoot("map5-east-gorge", new Vector3(52f, 0f, -6f), 36f, 50f, 0f, 60f);
            yield return PlaytestCapture.Shoot("map5-west-camp", new Vector3(-74f, 0f, -16f), 40f, 52f, 0f, 60f);
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
