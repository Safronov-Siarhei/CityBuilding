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
    /// The army slice end to end in the real scene: recruit a militiaman, watch a group actually
    /// fight, and take a portal down to win the map. Deliberately exercised through the same public
    /// entry points the UI uses (ArmyManager.TryRecruit, ArmyGroup.OrderAttack) so a break in the
    /// wiring between them shows up here rather than in a playtest.
    ///
    /// Buildings are never placed: a fresh game has no Town Hall until the player puts one down,
    /// and none of this needs one -- population and coins are granted directly, and the portal is
    /// created on the spot rather than waiting for OrcRaidManager to anchor one to a Town Hall.
    /// </summary>
    public class ArmyCombatTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map1";

        /// <summary>Fights are seconds long in game time; running them faster keeps the suite quick without changing any of the logic under test.</summary>
        private const float TestTimeScale = 8f;

        private static bool _sceneLoaded;

        [UnitySetUp]
        public IEnumerator PrepareScene()
        {
            // -nographics has no render target, so the minimap's RenderTexture logs an error the
            // runner would otherwise count as a failure.
            LogAssert.ignoreFailingMessages = true;

            if (!_sceneLoaded)
            {
                Time.timeScale = 1f;
                GameSessionIntent.NewGameMapId = MapId;
                SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
                yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);
                _sceneLoaded = true;
            }

            // A previous test may have won the map, which freezes time and blocks input.
            ModalGate.SetBlocked(false);
            Time.timeScale = TestTimeScale;

            // Automatic raids would wander into the middle of a test's fight.
            if (OrcRaidManager.Instance != null) OrcRaidManager.Instance.RaidsSuspended = true;

            ClearArmy();
            ClearOrcs();

            // Enough of both that no test is measuring the wrong limit.
            CitizenManager.Instance.SetPopulation(40);
            ResourceManager.Instance.SetAmount(ResourceType.Coins, 1000);
            yield return null;
        }

        [TearDown]
        public void RestoreTimeScale()
        {
            Time.timeScale = 1f;
        }

        [Test]
        public void Recruiting_CostsOneIdleCitizenAndItsCoins()
        {
            var army = ArmyManager.Instance;
            Assert.IsNotNull(army, "No ArmyManager in the loaded scene.");

            var populationBefore = CitizenManager.Instance.TotalPopulation;
            var coinsBefore = ResourceManager.Instance.GetAmount(ResourceType.Coins);
            var cost = SoldierStats.RecruitCost(SoldierType.Militia)[0].amount;

            Assert.IsTrue(army.TryRecruit(SoldierType.Militia, WalkablePoint()), "Recruitment was refused with population and coins to spare.");

            Assert.AreEqual(1, army.SoldierCount);
            Assert.AreEqual(populationBefore - 1, CitizenManager.Instance.TotalPopulation,
                "A recruit is supposed to leave the town's headcount -- the army is paid for in working hands, not just coins.");
            Assert.AreEqual(coinsBefore - cost, ResourceManager.Instance.GetAmount(ResourceType.Coins));
        }

        [Test]
        public void DisbandingSendsTheCitizenHome()
        {
            var army = ArmyManager.Instance;
            Assert.IsTrue(army.TryRecruit(SoldierType.Militia, WalkablePoint()));

            var populationWithSoldier = CitizenManager.Instance.TotalPopulation;
            army.Disband(army.Groups[0].Members[0]);

            Assert.AreEqual(0, army.SoldierCount);
            Assert.AreEqual(populationWithSoldier + 1, CitizenManager.Instance.TotalPopulation,
                "A disbanded soldier walks home; only death is meant to cost the settlement a citizen for good.");
        }

        [Test]
        public void RecruitmentIsRefused_WithoutIdleCitizens()
        {
            CitizenManager.Instance.SetPopulation(0);

            var blocker = ArmyManager.Instance.DescribeRecruitBlocker(SoldierType.Militia);

            Assert.IsNotNull(blocker, "With nobody to recruit, the Barracks must say so rather than raising a soldier from nowhere.");
            Assert.IsFalse(ArmyManager.Instance.TryRecruit(SoldierType.Militia, WalkablePoint()));
            Assert.AreEqual(0, ArmyManager.Instance.SoldierCount);
        }

        /// <summary>
        /// Reported from a real session: with the infinite-resources cheat on and an empty
        /// treasury, day 2 disbanded the whole army for non-payment. Upkeep was comparing against
        /// the raw coin count instead of asking ResourceManager whether it could afford the cost,
        /// and the cheat lives in that question.
        /// </summary>
        [Test]
        public void InfiniteResources_KeepsTheArmyPaid()
        {
            var army = ArmyManager.Instance;
            var spot = WalkablePoint();
            for (var i = 0; i < 3; i++)
            {
                Assert.IsTrue(army.TryRecruit(SoldierType.Militia, spot));
            }

            ResourceManager.Instance.SetAmount(ResourceType.Coins, 0);
            ResourceManager.Instance.SetInfiniteResources(true);
            try
            {
                army.ChargeDailyUpkeep();

                Assert.AreEqual(3, army.SoldierCount, "With infinite resources on, an empty treasury must not disband anyone.");
            }
            finally
            {
                ResourceManager.Instance.SetInfiniteResources(false);
            }
        }

        [Test]
        public void UnpayableUpkeep_DisbandsSoldiersAndSendsThemHome()
        {
            var army = ArmyManager.Instance;
            var spot = WalkablePoint();
            for (var i = 0; i < 3; i++)
            {
                Assert.IsTrue(army.TryRecruit(SoldierType.Militia, spot));
            }

            var populationWithArmy = CitizenManager.Instance.TotalPopulation;
            ResourceManager.Instance.SetAmount(ResourceType.Coins, 0);

            army.ChargeDailyUpkeep();

            Assert.AreEqual(0, army.SoldierCount, "Nobody can be paid, so nobody stays.");
            Assert.AreEqual(populationWithArmy + 3, CitizenManager.Instance.TotalPopulation,
                "Soldiers let go for lack of pay walk home as citizens -- being unpaid is not the same as dying.");
        }

        [UnityTest]
        public IEnumerator AGroupKillsAnOrcThatWalksIntoIt()
        {
            var army = ArmyManager.Instance;
            var spot = WalkablePoint();
            for (var i = 0; i < 3; i++)
            {
                Assert.IsTrue(army.TryRecruit(SoldierType.Militia, spot));
            }

            OrcRaidManager.Instance.SpawnOrcs(spot, count: 1, level: 1);
            Assert.AreEqual(1, OrcUnit.All.Count);

            yield return WaitUntil(() => OrcUnit.All.Count == 0, realSecondsTimeout: 25f);

            Assert.AreEqual(0, OrcUnit.All.Count,
                "Three militia standing on top of an orc should have killed it -- either they never engaged it, " +
                "or nothing connects a soldier's attack to an orc's health.");
        }

        [UnityTest]
        public IEnumerator OrcsFightBack_AndAKilledSoldierCostsAPermanentCitizen()
        {
            var army = ArmyManager.Instance;
            var spot = WalkablePoint();
            Assert.IsTrue(army.TryRecruit(SoldierType.Militia, spot));

            var populationAfterRecruit = CitizenManager.Instance.TotalPopulation;

            // Enough orcs that the lone militiaman certainly loses -- the point is that soldiers
            // are killable at all, and what that costs.
            OrcRaidManager.Instance.SpawnOrcs(spot, count: 3, level: 1);

            yield return WaitUntil(() => army.SoldierCount == 0, realSecondsTimeout: 25f);

            Assert.AreEqual(0, army.SoldierCount, "A militiaman surrounded by three orcs is supposed to die.");
            Assert.AreEqual(populationAfterRecruit, CitizenManager.Instance.TotalPopulation,
                "A soldier killed in battle must NOT come back as a citizen -- that asymmetry with disbanding is the cost of losing a fight.");
        }

        [UnityTest]
        public IEnumerator AttackOrderOnAPortalDestroysItAndWinsTheMap()
        {
            var army = ArmyManager.Instance;
            var spot = WalkablePoint();

            var portalGO = new GameObject("TestPortal");
            portalGO.transform.position = spot + new Vector3(2f, 0f, 0f);
            var portal = portalGO.AddComponent<OrcPortal>();

            for (var i = 0; i < 6; i++)
            {
                Assert.IsTrue(army.TryRecruit(SoldierType.Militia, spot));
            }

            var group = army.Groups[0];
            group.SetPriority(TargetPriority.Structures);
            group.OrderAttack(portal);

            yield return WaitUntil(() => portal == null || portal.CurrentHealth <= 0, realSecondsTimeout: 40f);

            Assert.IsTrue(portal == null || portal.CurrentHealth <= 0,
                "Six militia ordered onto a portal two metres away should have brought it down.");
            Assert.IsTrue(GameOverManager.Instance.IsGameOver && GameOverManager.Instance.IsVictory,
                "Closing the last portal is the map's win condition.");
        }

        /// <summary>
        /// Somewhere a unit can actually stand: real ground at the playable height with NavMesh on
        /// it. Searched outward from the map's centre rather than hardcoded, so this survives a map
        /// whose middle happens to be water.
        /// </summary>
        private static Vector3 WalkablePoint()
        {
            var mapApplier = MeshMapApplier.Instance;

            for (var radius = 0f; radius <= 40f; radius += 5f)
            {
                for (var i = 0; i < 8; i++)
                {
                    var angle = i * Mathf.PI * 0.25f;
                    var candidate = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                    if (mapApplier != null && !mapApplier.IsGroundAt(candidate)) continue;
                    if (!UnityEngine.AI.NavMesh.SamplePosition(candidate, out var hit, 2f, UnityEngine.AI.NavMesh.AllAreas)) continue;
                    return hit.position;
                }
            }

            Assert.Fail("No walkable ground found anywhere near the middle of the map.");
            return Vector3.zero;
        }

        /// <summary>Polls on REAL time, not scaled: a won map sets Time.timeScale to 0, and a scaled wait would hang the suite forever.</summary>
        private static IEnumerator WaitUntil(Func<bool> condition, float realSecondsTimeout)
        {
            var deadline = Time.realtimeSinceStartup + realSecondsTimeout;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }

        private static void ClearArmy()
        {
            var army = ArmyManager.Instance;
            if (army == null) return;

            army.SelectGroup(null);
            foreach (var group in army.Groups)
            {
                while (group.Count > 0)
                {
                    army.Disband(group.Members[0]);
                }
            }
        }

        private static void ClearOrcs()
        {
            for (var i = OrcUnit.All.Count - 1; i >= 0; i--)
            {
                var orc = OrcUnit.All[i];
                if (orc != null) UnityEngine.Object.DestroyImmediate(orc.gameObject);
            }
        }
    }
}
