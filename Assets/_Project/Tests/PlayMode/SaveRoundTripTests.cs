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
    /// Save and load for real: build a state in the running game, write the file the player would
    /// write, then reload the scene the way the main menu's Continue does and look at what came
    /// back.
    ///
    /// Worth the two scene loads it costs. Everything that goes wrong here goes wrong SILENTLY --
    /// the army used to be dropped by every save with nothing logged, and the citizens it had been
    /// recruited from went with it, so a reload quietly cost the player people. A round trip is the
    /// only test shape that can see that at all.
    /// </summary>
    public class SaveRoundTripTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map1";
        private const string SaveName = "PlayModeRoundTrip";

        [UnityTearDown]
        public IEnumerator RemoveTheTestSave()
        {
            // The saves folder is the editor's own; leaving this behind would put a test slot in
            // the player's Continue list.
            SaveSystem.Delete(SaveName);
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheArmyAndTheHungerStreak_ComeBackFromASave()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return StartGame(null);

            var army = ArmyManager.Instance;
            CitizenManager.Instance.SetPopulation(6);
            ResourceManager.Instance.SetAmount(ResourceType.Coins, 500);

            Assert.IsTrue(army.TryRecruit(SoldierType.Militia, WalkablePoint()), "Recruitment was refused with population and coins to spare.");
            Assert.IsTrue(army.TryRecruit(SoldierType.Militia, WalkablePoint()));

            var group = army.Groups[0];
            group.SetPriority(TargetPriority.Structures);
            group.OrderMoveTo(WalkablePoint());
            var holdPosition = group.HoldPosition;

            // One of them takes a hit, so the reload has something to get wrong: a save that healed
            // the survivors would make reloading mid-raid the cheapest heal in the game.
            var wounded = group.Members[0];
            wounded.TakeDamage(1);
            var woundedHealth = wounded.CurrentHealth;
            var woundedPosition = wounded.transform.position;
            Assert.Greater(woundedHealth, 0, "The test's own hit was meant to wound, not to kill.");

            // Nothing in the stores, so the day's meal comes up short and the town goes hungry.
            ResourceManager.Instance.SetAmount(ResourceType.Food, 0);
            ResourceManager.Instance.SetAmount(ResourceType.Bread, 0);
            FoodConsumptionManager.Instance.FeedSettlement();
            Assert.AreEqual(1, FoodConsumptionManager.Instance.HungryDaysInARow, "The town was supposed to go a day hungry before the save.");

            var populationWithArmy = CitizenManager.Instance.TotalPopulation;
            Assert.AreEqual(4, populationWithArmy, "Two of the six citizens went into the army.");

            // The raid side: a portal the army has already ground down, and one orc in the field.
            // The portal only opens once a Town Hall exists for it to be anchored to.
            var townHall = PlaytestWorld.Building("Castle");
            PlaytestWorld.Place(townHall, PlaytestWorld.FindFreeArea(townHall.footprintSize));
            yield return WaitForPortal();

            var portal = OrcPortal.All[0];
            portal.TakeDamage(portal.MaxHealth / 4);
            var portalHealth = portal.CurrentHealth;
            Assert.Greater(portalHealth, 0, "The test's own damage was meant to wound the portal, not to close it.");

            // Well away from the soldiers: within their engage radius they would start trading
            // blows with it, and the health this test is about would drift between save and assert.
            OrcRaidManager.Instance.SpawnOrcs(WalkablePointAwayFrom(holdPosition, 25f), 1, level: 1);
            var orc = OrcUnit.All[0];
            orc.TakeDamage(1);
            var orcHealth = orc.CurrentHealth;

            Object.FindAnyObjectByType<GameSaveController>().SaveGame(SaveName);

            yield return StartGame(SaveName);

            var loadedArmy = ArmyManager.Instance;
            Assert.AreEqual(2, loadedArmy.SoldierCount, "The soldiers did not come back -- this is the whole bug: saving used to disband the army.");
            Assert.AreEqual(populationWithArmy, CitizenManager.Instance.TotalPopulation,
                "The citizens the soldiers were recruited from must not be charged again on load, nor handed back: the army is still holding them.");

            var loadedGroup = loadedArmy.Groups[0];
            Assert.AreEqual(TargetPriority.Structures, loadedGroup.Priority, "The group's standing target priority is an order the player gave.");
            Assert.AreEqual(holdPosition.x, loadedGroup.HoldPosition.x, 0.01f, "The group came back somewhere other than its rally point.");
            Assert.AreEqual(holdPosition.z, loadedGroup.HoldPosition.z, 0.01f);

            var loadedWounded = TheWoundedOne(loadedGroup, woundedHealth);
            Assert.Less(Vector3.Distance(loadedWounded.transform.position, woundedPosition), 2.5f,
                "The wounded soldier came back somewhere other than where it was standing.");

            Assert.AreEqual(1, FoodConsumptionManager.Instance.HungryDaysInARow,
                "The hunger streak restarted at zero, so reloading hands a starving town its whole grace period back.");

            Assert.AreEqual(1, OrcPortal.All.Count,
                OrcPortal.All.Count == 0
                    ? "The portal did not come back at all."
                    : "A second portal was opened on top of the restored one -- the raid manager did not know one already stood there.");
            Assert.AreEqual(portalHealth, OrcPortal.All[0].CurrentHealth,
                "The portal reloaded repaired, which makes saving and loading a way to undo an assault on the map's objective.");

            Assert.AreEqual(1, OrcUnit.All.Count, "The orc already in the field vanished, so reloading is a way to call off a raid.");
            Assert.AreEqual(orcHealth, OrcUnit.All[0].CurrentHealth, "The orc came back healed.");

            Assert.Greater(OrcRaidManager.Instance.SecondsUntilNextRaid, 0f, "The raid clock came back stopped.");
        }

        /// <summary>
        /// The orders half of the army: what the group was told to attack, and which group the
        /// player was commanding. Both used to be dropped, and both are invisible losses -- a group
        /// sent across the map to break the portal came back standing where it had got to, doing
        /// nothing, and the player came back in build mode instead of command mode.
        ///
        /// A separate test from the one above because an attack order is not inert: the soldiers
        /// set off towards the portal and start damaging it, which is exactly the number the other
        /// test asserts has not changed.
        /// </summary>
        [UnityTest]
        public IEnumerator TheAttackOrderAndTheSelection_ComeBackFromASave()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return StartGame(null);

            // The portal is the target worth testing: it is the map's objective, and unlike an orc
            // it stands still, so the group's rally point cannot drift between the save and the
            // assert and disguise a lost order as a moved one.
            var townHall = PlaytestWorld.Building("Castle");
            PlaytestWorld.Place(townHall, PlaytestWorld.FindFreeArea(townHall.footprintSize));
            yield return WaitForPortal();

            var army = ArmyManager.Instance;
            CitizenManager.Instance.SetPopulation(6);
            ResourceManager.Instance.SetAmount(ResourceType.Coins, 500);
            Assert.IsTrue(army.TryRecruit(SoldierType.Militia, WalkablePoint()), "Recruitment was refused with population and coins to spare.");

            var group = army.Groups[0];
            group.OrderAttack(OrcPortal.All[0]);
            army.SelectGroup(group);
            Assert.IsNotNull(group.AttackTarget, "The test's own order did not take, so there is nothing to save.");

            Object.FindAnyObjectByType<GameSaveController>().SaveGame(SaveName);

            // Loaded WITHOUT the usual suspend, so that the flag coming back true can only have
            // come out of the save file -- a fresh OrcRaidManager has it false.
            yield return StartGame(SaveName, suspendRaidsAfterLoad: false);

            var loadedArmy = ArmyManager.Instance;
            Assert.AreEqual(1, loadedArmy.Groups.Count, "The group did not come back, so there is nothing to have been ordered.");

            var loadedGroup = loadedArmy.Groups[0];
            Assert.AreEqual(1, OrcPortal.All.Count, "The portal did not come back, so the order has nothing to point at.");
            Assert.AreSame(OrcPortal.All[0], loadedGroup.AttackTarget,
                loadedGroup.AttackTarget == null
                    ? "The assault was called off by the reload: the group came back holding its ground."
                    : "The group came back attacking something other than the portal it was sent to break.");

            Assert.AreSame(loadedGroup, loadedArmy.SelectedGroup,
                "The player came back with no group selected, so a tap on the world opens a building panel instead of ordering the army.");

            Assert.IsTrue(OrcRaidManager.Instance.RaidsSuspended,
                "The suspended raid clock started itself again on load, so a wave arrives in the middle of whatever it was switched off to watch.");
        }

        /// <summary>OrcRaidManager opens the portal in its own Update, on the first frame it sees a Town Hall.</summary>
        private static IEnumerator WaitForPortal()
        {
            for (var frame = 0; frame < 120 && OrcPortal.All.Count == 0; frame++)
            {
                yield return null;
            }

            Assert.AreEqual(1, OrcPortal.All.Count, "No portal opened next to the Town Hall, so there is nothing to save.");
        }

        /// <summary>
        /// The restored soldiers are identical apart from their health, and the save gives them no
        /// ids -- so the wound is what tells them apart. Matching on health rather than on position
        /// also keeps the test honest about the few frames of walking that happen between the load
        /// and the assert.
        /// </summary>
        private static SoldierUnit TheWoundedOne(ArmyGroup group, int woundedHealth)
        {
            SoldierUnit found = null;
            var matches = 0;

            foreach (var member in group.Members)
            {
                if (member.CurrentHealth != woundedHealth) continue;
                found = member;
                matches++;
            }

            Assert.AreEqual(1, matches,
                matches == 0
                    ? "No soldier came back on the health the wounded one was saved with -- reloading healed the army."
                    : "Every soldier came back wounded; the health in the save was applied to the wrong ones.");
            return found;
        }

        /// <summary>
        /// Loads the game scene the way the main menu does: a save name to continue one, null to
        /// start fresh.
        ///
        /// Raids are switched off afterwards so a wave cannot wander into the middle of a test --
        /// except for the test that is about the flag itself, which needs to see what the save put
        /// there rather than what this helper put there.
        /// </summary>
        private static IEnumerator StartGame(string saveNameToLoad, bool suspendRaidsAfterLoad = true)
        {
            Time.timeScale = 1f;
            GameSessionIntent.SaveNameToLoad = saveNameToLoad;
            GameSessionIntent.NewGameMapId = MapId;
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);

            // GameSaveController restores the saved state in its Start(); give it the frame.
            yield return null;

            ModalGate.SetBlocked(false);
            if (suspendRaidsAfterLoad && OrcRaidManager.Instance != null) OrcRaidManager.Instance.RaidsSuspended = true;
        }

        /// <summary>The same, but at arm's length from somewhere -- for putting an enemy on the map without putting it in a fight.</summary>
        private static Vector3 WalkablePointAwayFrom(Vector3 avoid, float minDistance)
        {
            var mapApplier = MeshMapApplier.Instance;

            for (var radius = minDistance; radius <= minDistance + 40f; radius += 5f)
            {
                for (var i = 0; i < 16; i++)
                {
                    var angle = i * Mathf.PI / 8f;
                    var candidate = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                    if (Vector3.Distance(candidate, avoid) < minDistance) continue;
                    if (mapApplier != null && !mapApplier.IsGroundAt(candidate)) continue;
                    if (!UnityEngine.AI.NavMesh.SamplePosition(candidate, out var hit, 2f, UnityEngine.AI.NavMesh.AllAreas)) continue;
                    return hit.position;
                }
            }

            Assert.Fail($"No walkable ground at least {minDistance}m from {avoid} -- the test cannot separate the orc from the soldiers.");
            return Vector3.zero;
        }

        /// <summary>Somewhere a soldier can actually stand -- dry ground with NavMesh under it.</summary>
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

            return Vector3.zero;
        }
    }
}
