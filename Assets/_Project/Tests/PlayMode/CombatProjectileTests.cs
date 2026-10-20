using System;
using System.Collections;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Maps;
using CityBuilder.Resources;
using CityBuilder.Saving;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// Ranged shots are SEEN (CombatProjectiles): a tower's arrow is in the air before the orc it
    /// is meant for is hurt, and the damage arrives with it. Run with graphics, the test also
    /// photographs the arrow in flight into playtest_shots/.
    /// </summary>
    public class CombatProjectileTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map1";

        [UnitySetUp]
        public IEnumerator PrepareScene()
        {
            LogAssert.ignoreFailingMessages = true;

            Time.timeScale = 1f;
            GameSessionIntent.NewGameMapId = MapId;
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);

            ModalGate.SetBlocked(false);
            if (OrcRaidManager.Instance != null) OrcRaidManager.Instance.RaidsSuspended = true;
        }

        [TearDown]
        public void RestoreTimeScale()
        {
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator ATowersArrowFliesBeforeItHurtsAndHurtsWhenItLands()
        {
            var towerData = PlaytestWorld.Building("Tower");
            Assert.IsNotNull(towerData, "No Tower in the building catalogue.");
            var cell = PlaytestWorld.FindFreeAreaNearTheMiddle(towerData.footprintSize + new Vector2Int(10, 10));
            var tower = PlaytestWorld.Place(towerData, cell + new Vector2Int(5, 5));
            Assert.IsTrue(tower.Defense > 0, "The tower has no defence, so it would never shoot.");

            var orcSpot = WalkableNear(tower.transform.position, 4f);
            OrcRaidManager.Instance.SpawnOrcs(orcSpot, count: 1, level: 1);
            Assert.AreEqual(1, OrcUnit.All.Count, "Nothing to shoot at.");
            var orc = OrcUnit.All[0];
            var fullHealth = orc.CurrentHealth;

            yield return WaitUntil(() => CombatProjectiles.LiveShotCount > 0 || orc == null, realSecondsTimeout: 15f);
            Assert.IsTrue(CombatProjectiles.LiveShotCount > 0, "The tower never loosed a visible arrow.");
            Assert.IsTrue(orc != null && orc.CurrentHealth == fullHealth,
                "The orc was hurt as the arrow left the tower -- the damage must land with the arrow, not before it.");

            if (PlaytestCapture.Available)
            {
                // Slowed almost to a stop, so the arrow is still in the air when the photo is taken.
                yield return LetGameTimePass(0.12f);
                var middle = (tower.transform.position + orc.transform.position) * 0.5f;
                var across = orc.transform.position - tower.transform.position;
                var yaw = Mathf.Atan2(across.x, across.z) * Mathf.Rad2Deg + 90f;
                yield return PlaytestCapture.Shoot("projectile-tower-arrow", middle, distance: 8f, pitch: 30f, yaw: yaw);
                Time.timeScale = 1f;

                yield return WaitUntil(() => CombatProjectiles.LiveChipCount > 0, realSecondsTimeout: 5f);
                yield return LetGameTimePass(0.05f);
                yield return PlaytestCapture.Shoot("projectile-tower-hit", orc != null ? orc.transform.position : middle, distance: 4f, pitch: 30f, yaw: yaw);
                Time.timeScale = 1f;
            }

            yield return WaitUntil(() => orc == null || orc.CurrentHealth < fullHealth, realSecondsTimeout: 5f);
            Assert.IsTrue(orc == null || orc.CurrentHealth < fullHealth, "The arrow landed and the orc was not hurt.");
        }

        [UnityTest]
        public IEnumerator AThrowersJavelinFliesBeforeItHurtsTheSoldier()
        {
            CitizenManager.Instance.SetPopulation(40);
            ResourceManager.Instance.SetAmount(ResourceType.Coins, 1000);

            var cell = PlaytestWorld.FindFreeAreaNearTheMiddle(new Vector2Int(12, 12));
            var soldierSpot = WalkableNear(PlaytestWorld.CellCenter(cell + new Vector2Int(6, 6)), 0.5f);
            Assert.IsTrue(ArmyManager.Instance.TryRecruit(SoldierType.Militia, soldierSpot), "No soldier to throw at.");
            yield return null;
            var soldier = SoldierUnit.All[0];
            var fullHealth = soldier.CurrentHealth;

            var thrower = OrcRaidManager.Instance.SpawnOrcs(WalkableNear(soldier.transform.position, 5f), count: 1, level: 1);
            thrower.Initialize(1, OrcKind.Thrower);
            Assert.IsTrue(thrower.IsRanged, "A thrower that does not throw.");

            yield return WaitUntil(() => CombatProjectiles.LiveShotCount > 0 || soldier == null, realSecondsTimeout: 15f);
            Assert.IsTrue(CombatProjectiles.LiveShotCount > 0, "The thrower never threw a visible javelin.");
            Assert.IsTrue(soldier != null && soldier.CurrentHealth == fullHealth,
                "The soldier was hurt as the javelin left the orc's hand -- the damage must land with it.");

            if (PlaytestCapture.Available)
            {
                yield return LetGameTimePass(0.2f);
                var middle = (thrower.transform.position + soldier.transform.position) * 0.5f;
                var across = soldier.transform.position - thrower.transform.position;
                var yaw = Mathf.Atan2(across.x, across.z) * Mathf.Rad2Deg + 90f;
                yield return PlaytestCapture.Shoot("projectile-orc-javelin", middle, distance: 5f, pitch: 25f, yaw: yaw);
                Time.timeScale = 1f;
            }

            yield return WaitUntil(() => soldier == null || soldier.CurrentHealth < fullHealth, realSecondsTimeout: 5f);
            Assert.IsTrue(soldier == null || soldier.CurrentHealth < fullHealth, "The javelin landed and the soldier was not hurt.");
        }

        private static Vector3 WalkableNear(Vector3 origin, float distance)
        {
            var mapApplier = MeshMapApplier.Instance;
            for (var i = 0; i < 16; i++)
            {
                var angle = i * Mathf.PI / 8f;
                var candidate = origin + new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
                if (mapApplier != null && !mapApplier.IsGroundAt(candidate)) continue;
                if (!UnityEngine.AI.NavMesh.SamplePosition(candidate, out var hit, 1f, UnityEngine.AI.NavMesh.AllAreas)) continue;
                return hit.position;
            }

            Assert.Fail($"No walkable ground {distance}m from {origin}.");
            return Vector3.zero;
        }

        /// <summary>Lets about `gameSeconds` of game time pass slowly, then all but stops the clock for a photograph.</summary>
        private static IEnumerator LetGameTimePass(float gameSeconds)
        {
            Time.timeScale = 0.05f;
            var until = Time.time + gameSeconds;
            var deadline = Time.realtimeSinceStartup + 10f;
            while (Time.time < until && Time.realtimeSinceStartup < deadline) yield return null;
            Time.timeScale = 0.002f;
        }

        /// <summary>Polls on REAL time, so a slowed or frozen clock cannot hang the suite.</summary>
        private static IEnumerator WaitUntil(Func<bool> condition, float realSecondsTimeout)
        {
            var deadline = Time.realtimeSinceStartup + realSecondsTimeout;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }
    }
}
