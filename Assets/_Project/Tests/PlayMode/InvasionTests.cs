using System.Collections;
using System.Linq;
using CityBuilder.Buildings;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Invasion;
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
    /// The Invasion mode's first step: «В бой» starts a run on the mode's own map with the castle,
    /// a tower at each causeway and the King in place, and the King is a hero -- he levels on
    /// what he kills and comes back after he falls. The landmarks are invasion1.py's: Blender
    /// (x, y) is Unity (x, z).
    /// </summary>
    public class InvasionTests
    {
        private const string GameSceneName = "CityBuilder";
        private static readonly Vector3[] Causeways = { new Vector3(0f, 0f, 34f), new Vector3(-29.4f, 0f, -17f), new Vector3(29.4f, 0f, -17f) };

        /// <summary>Fresh per test: a run sets itself up once, on the frames after the scene loads.</summary>
        [UnitySetUp]
        public IEnumerator StartARun()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            GameSessionIntent.NewGameMapId = InvasionMode.MapId;
            GameSessionIntent.NewGameIsInvasion = true;
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(InvasionMode.MapId);
            for (var frame = 0; frame < 300 && (InvasionMode.Instance == null || !InvasionMode.Instance.RunStarted); frame++) yield return null;
            Assert.IsTrue(InvasionMode.IsActive && InvasionMode.Instance.RunStarted, "The run never set itself up.");
        }

        [UnityTearDown]
        public IEnumerator Reset()
        {
            Time.timeScale = 1f;
            GameSessionIntent.NewGameIsInvasion = false;
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheRun_StartsWithTheCastleTheTowersAndTheKing_AndTheCausewaysCross()
        {
            var mode = InvasionMode.Instance;
            Assert.IsNotNull(mode.Castle, "No castle.");
            var castleAt = mode.Castle.transform.position;
            Assert.Less(new Vector2(castleAt.x, castleAt.z).magnitude, 3f, "The castle is not in the middle of the island.");
            Assert.AreEqual(3, Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None).Count(b => b.Data != null && b.Data.buildingName == "Tower"), "Not one tower per causeway.");
            Assert.AreEqual(0, Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None).Count(b => b.Data != null && b.Data.buildingName == "Bridge"), "Plank bridges were laid -- the crossings are earth now.");
            Assert.IsNotNull(mode.King, "No King.");
            Assert.IsNotNull(mode.King.transform.Find("King"), "The King is drawn in cubes -- his model (Models/Heroes/HeroKing) is not wired.");
            Assert.IsFalse(OrcRaidManager.Instance.enabled, "The sandbox's raids are still running in a run.");

            // every causeway carries the way off the island: from the tower to the lane beyond, on the ground
            yield return null;
            foreach (var bridge in Causeways)
            {
                var direction = bridge.normalized;
                var length = PathLength(bridge - direction * 9f, bridge + direction * 9f, out var complete);
                Assert.IsTrue(complete, $"No way across the causeway at {bridge}.");
                Assert.IsFalse(MeshMapApplier.Instance.IsWaterCell(GridManager.Instance.WorldToCell(bridge)), $"The causeway at {bridge} is water.");
                Assert.Less(length, 30f, $"The way across the causeway at {bridge} is {length:0} m -- it does not carry the lane.");
            }
        }

        [Test]
        public void TheKing_LevelsUp_AndGrowsStronger()
        {
            var king = InvasionMode.Instance.King;
            var health = king.Unit.MaxHealth;
            Assert.AreEqual(1, king.Level);

            king.AddExperience(KingHero.XpForLevel(1) - 1);
            Assert.AreEqual(1, king.Level, "A level came before its experience was full.");
            king.AddExperience(1 + KingHero.XpForLevel(2));
            Assert.AreEqual(3, king.Level, "Experience enough for two levels gave a different number.");
            Assert.AreEqual(health + 2 * BalanceConfig.Instance.KingHealthPerLevel, king.Unit.MaxHealth, "A level did not add the sheet's health.");
            Assert.AreEqual(KingHero.DamageAt(3), KingHero.DamageAt(1) + 2 * BalanceConfig.Instance.KingDamagePerLevel);
        }

        [UnityTest]
        public IEnumerator TheKing_GainsExperience_FromAnOrcHeKills()
        {
            var king = InvasionMode.Instance.King;
            var near = king.transform.position + new Vector3(3f, 0f, 0f);
            Assert.IsTrue(NavMesh.SamplePosition(near, out var hit, 3f, NavMesh.AllAreas), "Nowhere by the King to stand an orc.");
            var orc = OrcRaidManager.Instance.SpawnOrcs(hit.position, count: 1, level: 1);
            var worth = orc.MaxHealth;

            for (var t = 0f; t < 30f && orc != null && orc.CurrentHealth > 0; t += Time.deltaTime) yield return null;
            Assert.IsTrue(orc == null || orc.CurrentHealth <= 0, "The King never killed the orc beside him.");
            Assert.AreEqual(worth, king.Experience + (king.Level - 1) * KingHero.XpForLevel(1), "The kill was not worth the orc's health in experience.");
        }

        [UnityTest]
        public IEnumerator TheKing_Falls_AndComesBackAtTheCastle_KeepingHisLevel()
        {
            var king = InvasionMode.Instance.King;
            king.AddExperience(KingHero.XpForLevel(1));
            var level = king.Level;
            king.transform.position += new Vector3(4f, 0f, 4f);

            king.Unit.TakeDamage(king.Unit.MaxHealth);
            Assert.IsTrue(king.IsFallen, "Struck to zero, the King did not fall.");
            Assert.IsNotNull(king, "The King was destroyed instead of falling.");
            Assert.IsFalse(SoldierUnit.All.Contains(king.Unit), "A fallen King is still there for the orcs to fight.");
            Assert.AreEqual(KingHero.RespawnSecondsAt(level), king.RespawnRemaining, 0.01f);

            Time.timeScale = 4f;
            for (var t = 0f; t < 20f && king.IsFallen; t += Time.unscaledDeltaTime) yield return null;
            Time.timeScale = 1f;
            Assert.IsFalse(king.IsFallen, "The King never came back.");
            Assert.AreEqual(level, king.Level, "Coming back cost him his level.");
            Assert.AreEqual(king.Unit.MaxHealth, king.Unit.CurrentHealth, "He came back hurt.");
            Assert.Less(Vector3.Distance(Flat(king.transform.position), Flat(king.Home)), 1f, "He came back somewhere other than the castle.");
            Assert.IsTrue(SoldierUnit.All.Contains(king.Unit), "Back on his feet, he is still out of the fight.");
        }

        /// <summary>The King's figure stands, runs when he walks and swings when he strikes -- and the photographs show it.</summary>
        [UnityTest]
        public IEnumerator TheKing_RunsAndStrikes()
        {
            var king = InvasionMode.Instance.King;
            var animator = king.GetComponent<FigureAnimator>();
            Assert.IsNotNull(animator, "The King's figure is not animated.");
            var leg = king.GetComponentsInChildren<Transform>().First(t => t.name.EndsWith("LegL"));
            yield return new WaitForSeconds(0.5f);
            Assert.Less(animator.Run, 0.2f, "Standing still, the King is drawn running.");
            if (PlaytestCapture.Available) yield return PlaytestCapture.Shoot("king-stand", king.transform.position + Vector3.up * 0.35f, 2.2f, 20f, 200f, 35f);

            var target = king.transform.position + new Vector3(-8f, 0f, 0f);
            king.Unit.Group.OrderMoveTo(target);
            yield return new WaitForSeconds(0.8f);
            Assert.Greater(animator.Run, 0.6f, "Walking, the King is not drawn running.");
            var swing = 0f;
            for (var t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                swing = Mathf.Max(swing, Quaternion.Angle(leg.localRotation, Quaternion.identity));
                yield return null;
            }
            Assert.Greater(swing, 15f, "Running, his legs do not stride.");
            if (PlaytestCapture.Available) yield return PlaytestCapture.Shoot("king-run", king.transform.position + Vector3.up * 0.35f, 2.2f, 20f, 160f, 35f);

            king.Unit.Group.OrderMoveTo(king.transform.position);
            yield return new WaitForSeconds(1f);
            animator.OnStruck();
            Assert.IsTrue(animator.IsStriking, "A blow is not drawn.");
            yield return new WaitForSeconds(FigureAnimator.StrikeSeconds * 0.3f);
            if (PlaytestCapture.Available) yield return PlaytestCapture.Shoot("king-strike-back", king.transform.position + Vector3.up * 0.35f, 2.2f, 20f, 250f, 35f);
            animator.OnStruck();
            yield return new WaitForSeconds(FigureAnimator.StrikeSeconds * 0.55f);
            if (PlaytestCapture.Available) yield return PlaytestCapture.Shoot("king-strike-cut", king.transform.position + Vector3.up * 0.35f, 2.2f, 20f, 250f, 35f);
        }

        [UnityTest]
        public IEnumerator Photograph_TheRun()
        {
            if (!PlaytestCapture.Available) Assert.Ignore("No graphics device: run without -nographics to photograph.");
            yield return new WaitForSeconds(1f);
            yield return PlaytestCapture.Shoot("invasion-overview", Vector3.zero, 200f, 55f, 0f);
            yield return PlaytestCapture.Shoot("invasion-base", Vector3.zero, 44f, 52f, 0f, 60f);
            yield return PlaytestCapture.Shoot("invasion-king", InvasionMode.Instance.King.transform.position, 9f, 40f, 0f, 60f);
            yield return PlaytestCapture.Shoot("invasion-causeway", Causeways[1], 22f, 50f, 0f, 60f);
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
