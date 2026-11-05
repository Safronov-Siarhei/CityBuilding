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
        private const string MapId = "Map3";

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
            army.Disband(army.Garrison.Members[0]);

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

        /// <summary>The user, 2026-11-05: a soldier is hired once and never paid by the day -- an empty treasury keeps the army.</summary>
        [Test]
        public void NoUpkeep_AnEmptyTreasuryKeepsTheArmy()
        {
            var army = ArmyManager.Instance;
            var spot = WalkablePoint();
            for (var i = 0; i < 3; i++)
            {
                Assert.IsTrue(army.TryRecruit(SoldierType.Militia, spot));
            }

            ResourceManager.Instance.SetAmount(ResourceType.Coins, 0);
            army.ChargeDailyUpkeep();

            Assert.AreEqual(0, army.DailyUpkeep, "The army costs nothing by the day.");
            Assert.AreEqual(3, army.SoldierCount, "Nobody is let go for lack of pay.");
        }

        /// <summary>
        /// The roster widening (the user, 2026-11-05): a knight takes two places in the army and
        /// the Stable allows four a level; a monk strikes nobody and gives a wounded soldier beside
        /// him his health back.
        /// </summary>
        /// <summary>
        /// The siege rules (the user, 2026-11-05): a siege tower sent at a low fence rolls up and
        /// docks, its gangway making the fence a walk; a spearman goes up by it; sent on past the
        /// fence he jumps off and loses a share of his health; a knight is never let up.
        /// </summary>
        [UnityTest]
        public IEnumerator TheSiegeTowerDocks_AFootmanGoesUp_AndJumpsOffTheFarSide()
        {
            var army = ArmyManager.Instance;
            ResourceManager.Instance.SetAmount(ResourceType.IronBar, 50);
            ResourceManager.Instance.SetAmount(ResourceType.Wood, 500);
            ResourceManager.Instance.SetAmount(ResourceType.Coins, 5000);
            var origin = PlaytestWorld.FindFreeAreaNearTheMiddle(new Vector2Int(8, 6));
            var fenceCell = origin + new Vector2Int(2, 2);
            var fence = PlaytestWorld.Place(PlaytestWorld.Building("Fence"), fenceCell);
            Assert.IsNotNull(fence, "No fence could be set down for the siege.");
            yield return new WaitForSeconds(1f);
            var near = PlaytestWorld.CellCenter(fenceCell + new Vector2Int(-2, 0));
            var far = PlaytestWorld.CellCenter(fenceCell + new Vector2Int(3, 0));

            Assert.IsTrue(army.TryRecruit(SoldierType.SiegeTower, near, 4), army.DescribeRecruitBlocker(SoldierType.SiegeTower, 4));
            SoldierUnit tower = null, footman = null, knight = null;
            foreach (var u in SoldierUnit.All) if (u != null && u.Type == SoldierType.SiegeTower) tower = u;
            Assert.IsTrue(tower.DockAt(fence, fence.transform.position), "The siege tower found no cell beside the fence.");
            var waited = 0f;
            while (!tower.IsDocked && waited < 20f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(tower.IsDocked, "The siege tower never docked at the fence.");
            Assert.IsTrue(CityBuilder.Buildings.WallWalk.IsWalk(fenceCell), "A docked tower made no gangway over the low fence.");

            Assert.IsTrue(army.TryRecruit(SoldierType.Spearman, near, 5));
            Assert.IsTrue(army.TryRecruit(SoldierType.Knight, near, 5));
            foreach (var u in SoldierUnit.All)
            {
                if (u == null) continue;
                if (u.Type == SoldierType.Spearman) footman = u;
                if (u.Type == SoldierType.Knight) knight = u;
            }
            // a recruit joins the Garrison, which takes no orders: the spearman goes into a squad
            IntoNewSquad(army, footman);
            Assert.IsFalse(knight.GoToWall(fenceCell), "A knight was let up a wall.");
            Assert.IsTrue(footman.GoToWall(fenceCell), "A spearman was not let up the docked tower.");
            waited = 0f;
            while (!(footman.OnWall && !footman.IsClimbing) && waited < 25f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(footman.OnWall, "The spearman never got up onto the fence.");
            var before = footman.CurrentHealth;
            footman.Group.OrderMoveTo(far);
            yield return null;
            Assert.IsFalse(footman.OnWall, "Sent past the fence, the spearman stayed up.");
            Assert.Less(footman.CurrentHealth, before, "Jumping off the wall cost the spearman nothing.");
            Assert.Greater(Vector3.Distance(footman.transform.position, far), 0f);
            tower.Undock();
        }

        /// <summary>
        /// The Workshop (the user, 2026-11-05): a machine takes its crew of two citizens and gives
        /// them back when sent home; each Workshop level opens the next machine; the ram knows a
        /// gate from a wall.
        /// </summary>
        [Test]
        public void AMachineTakesItsCrew_AndTheWorkshopLevelOpensIt()
        {
            var army = ArmyManager.Instance;
            var spot = WalkablePoint();
            ResourceManager.Instance.SetAmount(ResourceType.IronBar, 50);
            ResourceManager.Instance.SetAmount(ResourceType.Wood, 500);
            ResourceManager.Instance.SetAmount(ResourceType.Coins, 5000);

            Assert.AreEqual(2, SoldierStats.Crew(SoldierType.Ram), "A machine's crew is not two citizens.");
            Assert.IsNotNull(army.DescribeRecruitBlocker(SoldierType.Ballista, 1), "A level-1 Workshop built a ballista.");
            var idle = CitizenManager.Instance.IdlePopulation;
            Assert.IsTrue(army.TryRecruit(SoldierType.Ram, spot, 1), army.DescribeRecruitBlocker(SoldierType.Ram, 1));
            Assert.AreEqual(idle - 2, CitizenManager.Instance.IdlePopulation, "The ram did not take its crew of two.");
            SoldierUnit ram = null;
            foreach (var unit in SoldierUnit.All) if (unit != null && unit.Type == SoldierType.Ram) ram = unit;
            Assert.IsNotNull(ram);
            Assert.Less(ram.RangedTakenShare, 1f, "Arrows hit the ram under its hides at full strength.");
            army.Disband(ram);
            Assert.AreEqual(idle, CitizenManager.Instance.IdlePopulation, "Sending the ram home did not give its crew back.");
        }

        /// <summary>
        /// Army control by squads (the user, 2026-11-05): a recruit joins the Garrison, a machine
        /// stands alone; a squad takes soldiers from the Garrison, five at most; a squad broken up
        /// sends its soldiers back to the Garrison.
        /// </summary>
        [Test]
        public void ARecruitJoinsTheGarrison_ASquadTakesHim_AndABrokenSquadSendsHimBack()
        {
            var army = ArmyManager.Instance;
            var spot = WalkablePoint();
            ResourceManager.Instance.SetAmount(ResourceType.Wood, 500);
            ResourceManager.Instance.SetAmount(ResourceType.Coins, 5000);
            Assert.IsTrue(army.TryRecruit(SoldierType.Militia, spot));
            Assert.IsTrue(army.TryRecruit(SoldierType.Militia, spot));
            Assert.IsTrue(army.TryRecruit(SoldierType.Ram, spot, 1), army.DescribeRecruitBlocker(SoldierType.Ram, 1));
            Assert.AreEqual(2, army.Garrison.Count, "A recruit did not join the Garrison.");
            Assert.AreEqual(1, army.Machines.Count, "The ram is not a machine standing alone.");

            var squad = army.CreateSquad();
            army.MoveSoldier(army.Garrison.Members[0], squad);
            Assert.AreEqual(1, squad.Count, "The squad did not take the soldier.");
            Assert.AreEqual(1, army.Garrison.Count);
            Assert.AreEqual(2, army.CountOf(SoldierType.Militia), "Moving a soldier changed the army's count.");
            army.MoveSoldier(army.Machines[0].Members[0], squad);
            Assert.AreEqual(1, squad.Count, "A machine went into a squad.");

            while (army.Squads.Count < ArmyManager.MaxSquads) Assert.IsNotNull(army.CreateSquad());
            Assert.IsNull(army.CreateSquad(), "More squads than the cap.");

            army.DeleteSquad(squad);
            Assert.AreEqual(2, army.Garrison.Count, "A broken-up squad's soldier did not go back to the Garrison.");
            Assert.AreEqual(ArmyManager.MaxSquads - 1, army.Squads.Count);
        }

        [UnityTest]
        public IEnumerator TheKnightTakesTwoPlaces_AndTheMonkHeals()
        {
            var army = ArmyManager.Instance;
            var spot = WalkablePoint();
            ResourceManager.Instance.SetAmount(ResourceType.IronBar, 50);
            ResourceManager.Instance.SetAmount(ResourceType.Coins, 5000);

            var cap = SoldierStats.CapPerLevel(SoldierType.Knight);
            Assert.Greater(cap, 0, "The Stable sets no cap on its knights.");
            for (var i = 0; i < cap; i++) Assert.IsTrue(army.TryRecruit(SoldierType.Knight, spot, 1), $"Knight {i + 1} of {cap} refused: {army.DescribeRecruitBlocker(SoldierType.Knight, 1)}");
            Assert.AreEqual(cap * 2, army.SoldierCount, "A knight is not counted as two places in the army.");
            Assert.IsFalse(army.TryRecruit(SoldierType.Knight, spot, 1), "A level-1 Stable hired past its cap.");
            Assert.IsTrue(army.TryRecruit(SoldierType.Knight, spot, 2), "A level-2 Stable is held to the level-1 cap.");
            ClearArmy();

            Assert.IsTrue(army.TryRecruit(SoldierType.Militia, spot));
            Assert.IsTrue(army.TryRecruit(SoldierType.Monk, spot, 1));
            SoldierUnit wounded = null;
            foreach (var unit in SoldierUnit.All) if (unit != null && unit.Type == SoldierType.Militia) wounded = unit;
            Assert.IsNotNull(wounded);
            wounded.TakeDamage(wounded.MaxHealth / 2);
            var hurt = wounded.CurrentHealth;
            var waited = 0f;
            while (wounded != null && wounded.CurrentHealth <= hurt && waited < 6f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            Assert.IsNotNull(wounded, "The wounded militiaman died beside a monk.");
            Assert.Greater(wounded.CurrentHealth, hurt, "The monk stood beside a wounded soldier and did not heal him.");
        }

        /// <summary>
        /// The two gates on a tier above the militia: the Laboratory has to open it, and the
        /// Плавильня has to have made the metal it is equipped with. Both are checked here rather
        /// than by eye, because a tier that is quietly recruitable for coins alone looks exactly
        /// like a tier that is working.
        /// </summary>
        [Test]
        public void ALockedTier_NeedsBothItsResearchAndItsMetal()
        {
            var research = Research.ResearchManager.Instance;
            Assert.IsNotNull(research, "No ResearchManager in the loaded scene.");
            research.ResetForTesting();

            var army = ArmyManager.Instance;
            Assert.IsNull(army.DescribeRecruitBlocker(SoldierType.Militia),
                "Militia is the tier a settlement starts with; nothing should be refusing it with coins and citizens to spare.");

            Assert.IsNotNull(army.DescribeRecruitBlocker(SoldierType.ManAtArms, 1),
                "A level-1 Barracks raised a man-at-arms, so the Barracks' level gates nothing.");
            Assert.IsFalse(army.TryRecruit(SoldierType.ManAtArms, WalkablePoint(), 1));

            ResourceManager.Instance.SetAmount(ResourceType.IronBar, 0);
            ResourceManager.Instance.SetAmount(ResourceType.CopperBar, 0);
            Assert.IsNotNull(army.DescribeRecruitBlocker(SoldierType.ManAtArms),
                "A researched tier was raised out of an empty treasury of metal, so the bars in its cost are decoration.");

            ResourceManager.Instance.SetAmount(ResourceType.IronBar, 10);
            ResourceManager.Instance.SetAmount(ResourceType.CopperBar, 10);
            Assert.IsNull(army.DescribeRecruitBlocker(SoldierType.ManAtArms), "Researched and paid for, and still refused.");
            Assert.IsTrue(army.TryRecruit(SoldierType.ManAtArms, WalkablePoint()));

            Assert.AreEqual(8, ResourceManager.Instance.GetAmount(ResourceType.IronBar),
                "Two iron bars are what a man-at-arms is armoured with, and they have to actually leave the store.");
        }

        /// <summary>
        /// The reason the archer tier is a trade rather than a free win. It shoots from eight
        /// metres and an orc only looks six, so without OrcUnit.NotifyAttackedBy a line of archers
        /// would take a raid apart untouched.
        ///
        /// There is no building anywhere on this map -- ArmyCombatTests never places one -- so an
        /// orc has nothing to walk towards and stands exactly where it was spawned. That makes
        /// "the orc moved" mean one thing only: it came for the archer.
        /// </summary>
        [UnityTest]
        public IEnumerator AnOrcComesAfterTheArcherShootingIt()
        {
            Research.ResearchManager.Instance.CompleteEverything();
            ResourceManager.Instance.SetAmount(ResourceType.IronBar, 20);
            ResourceManager.Instance.SetAmount(ResourceType.CopperBar, 20);

            var orcSpot = WalkablePoint();
            OrcRaidManager.Instance.SpawnOrcs(orcSpot, count: 1, level: 1);
            Assert.AreEqual(1, OrcUnit.All.Count, "Nothing to shoot at.");
            var orc = OrcUnit.All[0];
            var orcStart = orc.transform.position;

            // Beyond the orc's six-metre awareness and inside the archer's ten-metre watch, so the
            // archer opens fire on something that has no idea it is there.
            var archerSpot = WalkablePointNear(orcStart, 8.5f);
            var army = ArmyManager.Instance;
            Assert.IsTrue(army.TryRecruit(SoldierType.Archer, archerSpot), "The archer was refused even with everything researched and paid for.");

            // Explicitly, because a group survives its last member: an Archer group left over from
            // an earlier test would still be holding wherever that test put it.
            ArcherGroup(army).OrderMoveTo(archerSpot);

            // Measured while it lives: a researched archer kills an orc in two arrows, and the second
            // one -- flying at an orc already closing in (see CombatProjectiles) -- can land before
            // it has gone far, so "where it stood on the last frame" would read as never having come.
            var farthest = 0f;
            yield return WaitUntil(() =>
            {
                if (orc == null) return true;
                farthest = Mathf.Max(farthest, Vector3.Distance(orc.transform.position, orcStart));
                return farthest > 1.5f;
            }, realSecondsTimeout: 25f);

            Assert.Greater(farthest, 1f,
                "The orc stood still while it was shot from eight metres. With nothing on the map for it to walk to, only retaliation could have moved it -- so archers take raids apart without ever being touched.");
        }

        private static ArmyGroup ArcherGroup(ArmyManager army)
        {
            foreach (var unit in SoldierUnit.All)
            {
                if (unit != null && unit.Type == SoldierType.Archer) return IntoNewSquad(army, unit);
            }

            Assert.Fail("The recruited archer is in no group.");
            return null;
        }

        /// <summary>Recruits join the Garrison (army-control.md): these soldiers go into a new squad of the player's, which takes orders.</summary>
        private static ArmyGroup IntoNewSquad(ArmyManager army, params SoldierUnit[] units)
        {
            var squad = army.CreateSquad();
            Assert.IsNotNull(squad, "No squad could be made.");
            foreach (var unit in units) army.MoveSoldier(unit, squad);
            return squad;
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

            var group = IntoNewSquad(army, new System.Collections.Generic.List<SoldierUnit>(army.Garrison.Members).ToArray());
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

        /// <summary>
        /// Walkable ground about `distance` metres from `origin` -- for standing two units at a
        /// chosen range from each other, which is what a test about reach is made of.
        /// </summary>
        private static Vector3 WalkablePointNear(Vector3 origin, float distance)
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

            Assert.Fail($"No walkable ground {distance}m from {origin}, so the two units cannot be stood at the range this test needs.");
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
            while (army.Squads.Count > 0) army.DeleteSquad(army.Squads[0]);
            // a copy: a machine's group goes with its machine
            foreach (var group in new System.Collections.Generic.List<ArmyGroup>(army.Groups))
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
