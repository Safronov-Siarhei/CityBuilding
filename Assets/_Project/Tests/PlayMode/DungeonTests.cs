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

        private static readonly OrcKind[] DungeonKinds =
        {
            OrcKind.Rat, OrcKind.Bat, OrcKind.Spider, OrcKind.Spiderling, OrcKind.Skeleton, OrcKind.Kobold,
            OrcKind.StoneGolem, OrcKind.SpiderQueen, OrcKind.OreGolem,
        };

        private static OrcUnit First(OrcKind kind)
        {
            foreach (var orc in OrcUnit.All)
                if (orc != null && orc.Kind == kind && orc.CurrentHealth > 0) return orc;
            return null;
        }

        /// <summary>
        /// The user, 2026-10-31: the monsters in the game and every one of them animated. A lair at
        /// every lair spot and both bosses; all nine kinds are there, each drawn by its own jointed
        /// model (not the cube stand-in) and moved by a FigureAnimator; a bat's wings beat; every
        /// monster holds its lair.
        /// </summary>
        [UnityTest]
        public IEnumerator TheMonsters_StandInTheirLairs_EachKindJointedAndAnimated()
        {
            var monsters = DungeonMonsters.Instance;
            Assert.IsNotNull(monsters, "No monsters were put down in the dungeon.");
            var map = MeshMapApplier.Instance.CurrentMap;
            Assert.AreEqual(map.SpotsOf("lair").Count + 2, monsters.Lairs.Count, "Not every lair and boss hall has its monsters.");
            foreach (var kind in DungeonKinds)
            {
                var monster = First(kind);
                Assert.IsNotNull(monster, $"No {kind} in the dungeon.");
                Assert.IsTrue(monster.IsGuard, $"The {kind} does not hold its lair.");
                Assert.IsNotNull(monster.transform.Find(kind.ToString()), $"The {kind} is not drawn by its model.");
                Assert.IsNotNull(monster.GetComponent<FigureAnimator>(), $"The {kind} has no animation.");
            }
            var bat = First(OrcKind.Bat).GetComponent<FigureAnimator>();
            var seen = new HashSet<int>();
            Camera.main.transform.position = First(OrcKind.Bat).transform.position + new Vector3(0f, 6f, -4f);
            Camera.main.transform.LookAt(First(OrcKind.Bat).transform.position);
            for (var i = 0; i < 20; i++)
            {
                yield return null;
                seen.Add(Mathf.RoundToInt(bat.WingAngle / 10f));
            }
            Assert.Greater(seen.Count, 2, "The bat's wings do not beat.");
        }

        /// <summary>A skeleton struck down falls apart, lies, and gets up once; struck down again it stays down.</summary>
        [UnityTest]
        public IEnumerator ASkeleton_GetsUpOnce()
        {
            var skeleton = First(OrcKind.Skeleton);
            Assert.IsNotNull(skeleton, "No skeleton in the dungeon.");
            skeleton.TakeDamage(skeleton.MaxHealth * 10);
            Assert.Greater(skeleton.CurrentHealth, 0, "A skeleton struck down the first time did not get up.");
            Assert.Greater(skeleton.BusyFor, 0f, "The skeleton did not lie in pieces before getting up.");
            yield return null;
            Assert.IsTrue(skeleton.GetComponent<FigureAnimator>().IsDown, "The skeleton's fall is not shown.");
            skeleton.TakeDamage(skeleton.MaxHealth * 10);
            yield return null;
            Assert.IsTrue(skeleton == null, "A skeleton got up twice.");
        }

        /// <summary>
        /// The queen at half health seals herself in a cocoon: she does nothing, spiderlings come, and
        /// while they live she heals. The ore golem at half health grows his armour. A boss slain
        /// pays coins and crystals and is written down as slain.
        /// </summary>
        [UnityTest]
        public IEnumerator TheBosses_CocoonAndArmour_AndTheyPay()
        {
            var queen = First(OrcKind.SpiderQueen);
            Assert.IsNotNull(queen, "No spider queen.");
            // the sheet's numbers, not a fallback: a boss is far tougher than a rat
            Assert.AreEqual(CityBuilder.Core.BalanceConfig.Instance.MonsterUnit("spider_queen").LevelStats(1).maxHealth, queen.MaxHealth,
                "The queen does not have the sheet's health.");
            Assert.Greater(queen.MaxHealth, First(OrcKind.Rat).MaxHealth * 10, "The queen is no tougher than a rat.");
            var lings = 0;
            foreach (var o in OrcUnit.All) if (o != null && o.Kind == OrcKind.Spiderling) lings++;
            queen.TakeDamage(queen.MaxHealth / 2 + 1);
            var powers = queen.GetComponent<DungeonMonsterPowers>();
            Assert.IsTrue(powers.InCocoon, "The queen at half health is not in her cocoon.");
            Assert.Greater(queen.BusyFor, 0f, "The queen in her cocoon still fights.");
            var after = 0;
            foreach (var o in OrcUnit.All) if (o != null && o.Kind == OrcKind.Spiderling) after++;
            Assert.Greater(after, lings, "No spiderlings came to the cocoon.");
            var health = queen.CurrentHealth;
            yield return new WaitForSeconds(1.5f);
            Assert.Greater(queen.CurrentHealth, health, "The living spiderlings do not heal the queen in her cocoon.");

            var golem = First(OrcKind.OreGolem);
            Assert.IsNotNull(golem, "No ore golem.");
            golem.TakeDamage(golem.MaxHealth / 2 + 1);
            Assert.Greater(golem.Armour, 0f, "The ore golem at half health has no armour.");
            golem.GetComponent<FigureAnimator>().Slam();
            Assert.IsTrue(golem.GetComponent<FigureAnimator>().IsSlamming, "The golem's slam is not shown.");

            var monsters = DungeonMonsters.Instance;
            var crystals = CityBuilder.Core.Wallet.UpgradeCrystals;
            golem.Armour = 0f;
            golem.TakeDamage(golem.MaxHealth * 10);
            yield return null;
            Assert.Greater(monsters.CoinsEarned, 0, "The golem slain paid no coins.");
            Assert.Greater(CityBuilder.Core.Wallet.UpgradeCrystals, crystals, "The golem slain paid no crystals.");
            Assert.Contains(DungeonMonsters.BossId(DungeonTravel.FirstDungeonId, 1, "golem"), new List<string>(monsters.BossesSlainNow),
                "The slain golem is not written down.");
            CityBuilder.Core.Wallet.UpgradeCrystals = crystals;
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
