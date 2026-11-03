using System.Collections;
using System.Collections.Generic;
using CityBuilder.Combat;
using CityBuilder.Dungeon;
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
    /// The level-1 dungeons in several generations (the user, 2026-11-03: «чтобы игроки не
    /// заскучали»): every variant behind a mouth is a map of its own that the heroes come down
    /// into at the camp, with both bosses, crystals and lairs, each boss reachable on foot along a
    /// winding way -- and the variants are not one labyrinth drawn again.
    /// </summary>
    public class DungeonVariantTests
    {
        [UnityTest]
        public IEnumerator EveryLevelOneDungeon_LoadsWithItsBossesReachable_AndNoTwoAlike()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            DungeonMode.LeaveWhenAllFall = false;
            var bossSpots = new List<Vector2>();
            foreach (var id in DungeonTravel.LevelOneIds)
            {
                SaveSystem.Delete(SaveSystem.HiddenTownSlot);
                GameSessionIntent.DungeonId = id;
                GameSessionIntent.DungeonHeroes = new List<HeroEntry> { new HeroEntry { type = SoldierType.King, level = 3 } };
                GameSessionIntent.DungeonReturnName = "town";
                GameSessionIntent.NewGameMapId = id;
                var before = DungeonMode.Instance;
                SceneManager.LoadScene("CityBuilder", LoadSceneMode.Single);
                for (var frame = 0; frame < 600; frame++)
                {
                    if (DungeonMode.Instance != null && DungeonMode.Instance != before && DungeonMode.Instance.Arrived) break;
                    yield return null;
                }
                yield return new WaitForFixedUpdate();
                Physics.SyncTransforms();

                Assert.IsNotNull(DungeonMode.Instance, $"{id}: no dungeon came up.");
                Assert.IsTrue(DungeonMode.Instance.Arrived, $"{id}: the heroes never arrived.");
                Assert.AreEqual(id, DungeonMode.Instance.DungeonId);
                var map = MeshMapApplier.Instance.CurrentMap;
                Assert.IsNotNull(map, $"{id}: no map.");
                Assert.AreEqual(id, MeshMapApplier.Instance.CurrentMapId, $"{id}: another map came up.");
                Assert.Greater(map.SpotsOf("lair").Count, 5, $"{id}: hardly any monster lairs.");
                Assert.Greater(map.SpotsOf("reward").Count + map.SpotsOf("crystal").Count, 6, $"{id}: hardly any crystals.");

                var start = DungeonMode.Instance.StartSpot;
                Assert.IsTrue(NavMesh.SamplePosition(start, out var from, 1f, NavMesh.AllAreas), $"{id}: the camp has no NavMesh.");
                foreach (var boss in new[] { "boss_golem", "boss_spiders" })
                {
                    var spots = map.SpotsOf(boss);
                    Assert.AreEqual(1, spots.Count, $"{id}: no {boss} spot.");
                    bossSpots.Add(spots[0].position);
                    var at = new Vector3(spots[0].position.x, 0f, spots[0].position.y);
                    Assert.IsTrue(NavMesh.SamplePosition(at, out var to, 6f, NavMesh.AllAreas), $"{id}: {boss} has no floor to walk on.");
                    var path = new NavMeshPath();
                    NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path);
                    Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status, $"{id}: {boss} cannot be reached from the camp.");
                    var length = 0f;
                    for (var i = 1; i < path.corners.Length; i++) length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
                    Assert.Greater(length, Vector3.Distance(from.position, to.position) * 1.3f, $"{id}: the way to {boss} is nearly straight -- the labyrinth leaks.");
                }
            }

            // no two variants keep both bosses where another has them
            for (var a = 0; a < bossSpots.Count; a += 2)
            for (var b = a + 2; b < bossSpots.Count; b += 2)
            {
                var same = Vector2.Distance(bossSpots[a], bossSpots[b]) < 8f && Vector2.Distance(bossSpots[a + 1], bossSpots[b + 1]) < 8f;
                Assert.IsFalse(same, $"{DungeonTravel.LevelOneIds[a / 2]} and {DungeonTravel.LevelOneIds[b / 2]} have their bosses in the same halls.");
            }
            DungeonMode.LeaveWhenAllFall = true;
        }

        /// <summary>
        /// The level-2 dungeon, the drowned dwarven halls (the user, 2026-11-03: «подключи эту карту
        /// в подземелье 2 ... пока что не добавляй монстров и ресурсы, я хочу просто пройтись»): the
        /// heroes come down at its camp, nothing lives there and no crystal lies there yet, and the
        /// throne and both levers are reached on foot -- through the wading passages and over the
        /// bridges, as the water is laid.
        /// </summary>
        [UnityTest]
        public IEnumerator TheLevelTwoDungeon_LoadsEmpty_AndItsHallsAreReachedOnFoot()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            DungeonMode.LeaveWhenAllFall = false;
            var id = DungeonTravel.LevelTwoId;
            SaveSystem.Delete(SaveSystem.HiddenTownSlot);
            GameSessionIntent.DungeonId = id;
            GameSessionIntent.DungeonLevel = 2;
            GameSessionIntent.DungeonHeroes = new List<HeroEntry> { new HeroEntry { type = SoldierType.King, level = 3 } };
            GameSessionIntent.DungeonReturnName = "town";
            GameSessionIntent.NewGameMapId = id;
            var before = DungeonMode.Instance;
            SceneManager.LoadScene("CityBuilder", LoadSceneMode.Single);
            for (var frame = 0; frame < 900; frame++)
            {
                if (DungeonMode.Instance != null && DungeonMode.Instance != before && DungeonMode.Instance.Arrived) break;
                yield return null;
            }
            yield return new WaitForFixedUpdate();
            Physics.SyncTransforms();

            Assert.IsNotNull(DungeonMode.Instance, "No dungeon came up.");
            Assert.IsTrue(DungeonMode.Instance.Arrived, "The heroes never arrived.");
            Assert.AreEqual(id, MeshMapApplier.Instance.CurrentMapId, "Another map came up.");
            var map = MeshMapApplier.Instance.CurrentMap;
            foreach (var kind in new[] { "lair", "reward", "crystal", "patrol_0", "boss_golem", "boss_spiders" })
                Assert.AreEqual(0, map.SpotsOf(kind).Count, $"The walk-through dungeon has {kind} spots.");
            Assert.AreEqual(0, Object.FindObjectsByType<CityBuilder.Combat.OrcUnit>(FindObjectsSortMode.None).Length, "Monsters live in the walk-through dungeon.");

            // the water is waded knee-deep and slower, and a bridge's deck stands up out of it
            Assert.IsTrue(DungeonFloor.Active, "The dungeon's floor was not read.");
            var wades = 0;
            var decks = 0;
            var half = DungeonMode.Instance.MapHalf;
            var ground = CityBuilder.Grid.GridManager.Instance.GroundHeight;
            for (var x = -half; x < half; x += 1f)
            for (var z = -half; z < half; z += 1f)
            {
                var at = new Vector3(x, 0f, z);
                if (!NavMesh.SamplePosition(at, out var on, 0.3f, NavMesh.AllAreas)) continue;
                if (DungeonFloor.Wading(at))
                {
                    wades++;
                    var depth = DungeonFloor.WaterLevel - (ground + DungeonFloor.StandHeightAt(at));
                    Assert.Less(depth, 0.4f, $"The water at {at} is deeper than a knee and still walked.");
                    Assert.Less(CityBuilder.Buildings.Moat.SpeedAt(at), 1f, "Wading is not slower.");
                }
                else if (DungeonFloor.StandHeightAt(at) > 0.06f) decks++;   // a deck: 0.18-0.43 m up, 0.08-0.19 cells
            }
            Assert.Greater(wades, 200, "Nowhere is the water waded.");
            Assert.Greater(decks, 10, "No bridge's deck stands up out of the water.");

            var start = DungeonMode.Instance.StartSpot;
            Assert.IsTrue(NavMesh.SamplePosition(start, out var from, 1f, NavMesh.AllAreas), "The camp has no NavMesh.");
            var throne = map.SpotsOf("throne");
            var levers = map.SpotsOf("lever");
            Assert.AreEqual(1, throne.Count, "The throne is not on the map.");
            Assert.AreEqual(2, levers.Count, "The two levers are not on the map.");
            var doors = DungeonLevers.Instance;
            Assert.IsNotNull(doors, "No levers and door were made.");
            Assert.AreEqual(2, doors.LeverCount, "The levers were not all made.");
            Assert.IsFalse(doors.DoorOpen, "The door stands open before a lever is pulled.");
            // the bars' NavMeshObstacle carves the NavMesh a few frames after it is made
            for (var frame = 0; frame < 10; frame++) yield return null;
            // the way a hero walks (NavRoute: the NavMesh, and the dungeon's road map for the long ways round)
            Assert.Greater(DungeonWaypoints.NodeCount, 50, "The dungeon's road map was not laid.");
            foreach (var goal in levers)
                Assert.IsTrue(Reaches(from.position, goal), $"The lever at {goal.position} cannot be reached from the camp.");
            Assert.IsFalse(Reaches(from.position, throne[0]), "The throne is reached through the shut door.");

            // a hero by each lever pulls it; with both down the door is up and the throne reached
            var hero = HeroRoster.Instance.Heroes[0].Unit;
            foreach (var place in Object.FindObjectsByType<DungeonEntrance>(FindObjectsSortMode.None))
            {
                if (place.Kind != DungeonEntrance.Place.Lever) continue;
                hero.transform.position = place.ApproachFrom(start);
                place.OpenCard();
            }
            Assert.AreEqual(2, doors.PulledCount, "A hero by a lever did not pull it.");
            Assert.IsTrue(doors.DoorOpen, "Both levers are down and the door is shut.");
            for (var frame = 0; frame < 10; frame++) yield return null;
            Assert.IsTrue(Reaches(from.position, throne[0]), "The door is up and the throne still cannot be reached.");
            DungeonMode.LeaveWhenAllFall = true;
        }

        private static bool Reaches(Vector3 from, MapSpot goal)
        {
            var at = new Vector3(goal.position.x, 0f, goal.position.y);
            Assert.IsTrue(NavMesh.SamplePosition(at, out var to, 4f, NavMesh.AllAreas), $"The {goal.kind} at {goal.position} has no floor.");
            var route = new NavRoute();
            route.SetDestination(from, to.position, straightLineFallback: false);
            return !route.Blocked && route.RemainingLength(from) > 1f;
        }

        /// <summary>The mouths of one map lead to different labyrinths, and the pick changes from map to map; an old (version-2) map keeps Dungeon 1.</summary>
        [Test]
        public void TheMouthsOfAMap_LeadToDifferentDungeons()
        {
            var firsts = new HashSet<string>();
            for (var seed = 1; seed <= 20; seed++)
            {
                var ids = new HashSet<string>();
                for (var k = 0; k < 3; k++) ids.Add(DungeonTravel.LevelOneFor(seed, 3, k));
                Assert.AreEqual(3, ids.Count, $"Seed {seed}: two mouths lead to one dungeon.");
                firsts.Add(DungeonTravel.LevelOneFor(seed, 3, 0));
                Assert.AreEqual(DungeonTravel.FirstDungeonId, DungeonTravel.LevelOneFor(seed, 2, 1), "An old map's dungeon changed.");
            }
            Assert.AreEqual(DungeonTravel.LevelOneIds.Length, firsts.Count, "Some level-1 dungeon is never the first on any map.");
        }
    }
}
