using System.Collections;
using System.Collections.Generic;
using CityBuilder.CameraControl;
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
    /// Dungeon 1 in the game (Tools/blender/dungeon1.py; card Design/features/dungeon.md): the
    /// heroes come down to the camp by the way in, the labyrinth's halls are all reachable on foot,
    /// only the squares near the camera are drawn, and nothing below writes over the town.
    /// The spots are the generator's: a build unit is 0.44 of a cell, centred on (100, 100).
    /// </summary>
    public class DungeonTests
    {
        private const string GameSceneName = "CityBuilder";

        private static Vector3 Cell(float buildX, float buildY) => new Vector3((buildX - 100f) * 0.44f, 0f, (buildY - 100f) * 0.44f);

        // the plan's halls (dungeon1-plan.json, 14.29-unit cells): the two bosses in the far corners, the arenas
        private static readonly (string name, Vector3 at)[] Halls =
        {
            ("the golem's hall", Cell(28.6f, 171.4f)), ("the spiders' lair", Cell(171.4f, 171.4f)),
            ("the west arena", Cell(28.6f, 71.4f)), ("the east arena", Cell(171.4f, 71.4f)), ("the deep arena", Cell(100f, 171.4f)),
        };

        [UnitySetUp]
        public IEnumerator GoDown()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            GameSessionIntent.DungeonId = DungeonTravel.FirstDungeonId;
            GameSessionIntent.DungeonHeroes = new List<HeroEntry> { new HeroEntry { type = SoldierType.King, level = 3 } };
            GameSessionIntent.DungeonReturnName = "town";
            GameSessionIntent.NewGameMapId = DungeonTravel.FirstDungeonId;
            var before = DungeonMode.Instance;   // the last test's, alive until the load lands
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            for (var frame = 0; frame < 600; frame++)
            {
                if (DungeonMode.Instance != null && DungeonMode.Instance != before && DungeonMode.Instance.Arrived) break;
                yield return null;
            }
            yield return new WaitForFixedUpdate();
            Physics.SyncTransforms();
        }

        [Test]
        public void TheHeroComesDownToTheCamp_AndTheTownIsNotThere()
        {
            Assert.IsTrue(DungeonMode.IsActive, "The game did not go into the dungeon.");
            var map = MeshMapApplier.Instance.CurrentMap;
            Assert.IsTrue(map != null && map.IsDungeon, "The dungeon's map was not applied -- is it in Resources/MeshMaps?");
            var king = HeroRoster.Instance.HeroOf(SoldierType.King);
            Assert.IsNotNull(king, "The King who went down is not there.");
            Assert.AreEqual(3, king.Level, "The King lost his level on the way down.");
            var start = DungeonMode.Instance.StartSpot;
            Assert.Less(Vector3.Distance(Flat(king.transform.position), Flat(start)), 3f, "The King is not at the camp.");
            Assert.IsTrue(NavMesh.SamplePosition(start, out _, 1f, NavMesh.AllAreas), "The camp has no NavMesh to walk on.");
            Assert.AreEqual(88f, Mathf.Abs(DungeonMode.Instance.ExitSpot.z) * 2f, 2f, "The dungeon is not 88 cells across.");
            Assert.IsFalse(Object.FindAnyObjectByType<CityBuilder.Buildings.BuildingPlacer>().IsPlacingMandatoryBuilding,
                "The game asks for a Town Hall below ground.");
        }

        [Test]
        public void EveryHall_IsReachableOnFoot_FromTheCamp()
        {
            var start = DungeonMode.Instance.StartSpot;
            Assert.IsTrue(NavMesh.SamplePosition(start, out var from, 1f, NavMesh.AllAreas), "The camp has no NavMesh.");
            foreach (var (name, at) in Halls)
            {
                Assert.IsTrue(NavMesh.SamplePosition(at, out var to, 6f, NavMesh.AllAreas), $"{name} has no floor to walk on.");
                var path = new NavMeshPath();
                NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path);
                Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status, $"{name} cannot be reached from the camp.");
                var length = 0f;
                for (var i = 1; i < path.corners.Length; i++) length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
                Assert.Greater(length, Vector3.Distance(from.position, to.position) * 1.3f, $"The way to {name} is nearly straight -- the labyrinth leaks.");
            }
        }

        [Test]
        public void OnlyTheSquaresNearTheCamera_AreDrawn()
        {
            var streamer = MeshMapApplier.Instance.GetComponent<DungeonChunkStreamer>();
            Assert.IsNotNull(streamer, "The dungeon has no square streamer.");
            streamer.Refresh();
            Assert.Greater(streamer.Drawn, 0, "Nothing is drawn at the camp.");
            Assert.Less(streamer.Drawn, streamer.Total / 2, $"Most of the dungeon is drawn ({streamer.Drawn} of {streamer.Total}).");

            var rig = Object.FindAnyObjectByType<RTSCameraController>();
            var near = streamer.Drawn;
            rig.JumpTo(Halls[1].at);
            streamer.Refresh();
            Assert.Greater(streamer.Drawn, 0, "Nothing is drawn by the spiders' lair.");
            var groundAtCamp = FindGroundRendererNear(DungeonMode.Instance.StartSpot);
            Assert.IsNotNull(groundAtCamp, "No ground square at the camp.");
            Assert.IsFalse(groundAtCamp.enabled, $"The camp's square is still drawn with the camera at the far corner ({near} -> {streamer.Drawn}).");
        }

        [Test]
        public void NothingBelow_IsSaved()
        {
            var save = Object.FindAnyObjectByType<GameSaveController>();
            const string slot = "DungeonTestSlot";
            SaveSystem.Delete(slot);
            save.SaveGame(slot);
            var written = SaveSystem.SaveExists(slot);
            SaveSystem.Delete(slot);
            Assert.IsFalse(written, "A save was written in the dungeon -- it would hold the dungeon, not the town.");
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private static Renderer FindGroundRendererNear(Vector3 point)
        {
            foreach (var renderer in MeshMapApplier.Instance.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.gameObject.name.Contains("Ground")) continue;
                var b = renderer.bounds;
                if (point.x >= b.min.x && point.x <= b.max.x && point.z >= b.min.z && point.z <= b.max.z) return renderer;
            }
            return null;
        }
    }
}
