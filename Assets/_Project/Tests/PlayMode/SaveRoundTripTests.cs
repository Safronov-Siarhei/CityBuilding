using System.Collections;
using System.Linq;
using System.Collections.Generic;
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

            // The raid side: portals the army has already started grinding down, and one orc in the
            // field. The portals only open once a Town Hall exists for them to be anchored to.
            var townHall = PlaytestWorld.Building("Castle");
            PlaytestWorld.Place(townHall, PlaytestWorld.FindFreeArea(townHall.footprintSize));
            yield return WaitForPortals();

            var portalCount = OrcPortal.All.Count;
            // The camps' totems shield the portals (see OrcCamp); dropped here so the hits land.
            // The save writes the totems, so the shields come back with them on load.
            foreach (var portal in OrcPortal.All) portal.IsShielded = false;
            // Two of them wounded, by DIFFERENT amounts: a save that wrote one portal's health over
            // all of them, or that restored the list in some other order, passes a single-portal
            // check and fails this one.
            OrcPortal.All[0].TakeDamage(OrcPortal.All[0].MaxHealth / 4);
            OrcPortal.All[portalCount - 1].TakeDamage(OrcPortal.All[portalCount - 1].MaxHealth / 2);
            var portalHealths = PortalHealthByCell();

            // One camp loses its totem: its ruin and its portal's lost shield must survive the
            // reload, and the other camps' shields must come back up.
            var brokenCamp = OrcCamp.All[0];
            var brokenTotem = brokenCamp.Buildings.First(b => b.Kind == OrcCampBuildingKind.Totem);
            var brokenTotemCell = brokenTotem.Cell;
            var brokenPortalCell = brokenCamp.PortalCell;
            brokenTotem.TakeDamage(brokenTotem.MaxHealth);
            CollectionAssert.IsNotEmpty(portalHealths, "No portals opened, so there is nothing to save.");
            foreach (var health in portalHealths.Values)
            {
                Assert.Greater(health, 0, "The test's own damage was meant to wound the portals, not to close one.");
            }

            // Well away from the soldiers: within their engage radius they would start trading
            // blows with it, and the health this test is about would drift between save and assert.
            // The orc the raid sent, not one of the camps' guards (those are not saved: the tents
            // muster them again on load).
            var orc = OrcRaidManager.Instance.SpawnOrcs(WalkablePointAwayFrom(holdPosition, 25f), 1, level: 1);
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

            Assert.AreEqual(portalCount, OrcPortal.All.Count,
                OrcPortal.All.Count == 0
                    ? "The portals did not come back at all."
                    : "A fresh set of portals was opened on top of the restored ones -- the raid manager did not know they already stood there.");
            CollectionAssert.AreEquivalent(portalHealths, PortalHealthByCell(),
                "The portals came back somewhere else or repaired, which makes saving and loading a way to undo an assault on the map's objective.");

            var raiders = OrcUnit.All.Where(o => !o.IsGuard).ToList();
            Assert.AreEqual(1, raiders.Count, "The orc already in the field vanished, so reloading is a way to call off a raid.");
            Assert.AreEqual(orcHealth, raiders[0].CurrentHealth, "The orc came back healed.");

            Assert.Greater(OrcRaidManager.Instance.SecondsUntilNextRaid, 0f, "The raid clock came back stopped.");

            Assert.AreEqual(portalCount, OrcCamp.All.Count, "The camps did not come back one per portal.");
            Assert.IsNotNull(GameObject.Find($"OrcCampRuin {brokenTotemCell.x},{brokenTotemCell.y}"), "The broken totem's ruin did not come back.");
            foreach (var portal in OrcPortal.All)
            {
                Assert.AreEqual(portal.Cell != brokenPortalCell, portal.IsShielded,
                    portal.Cell == brokenPortalCell
                        ? "The portal whose totem the player broke came back shielded -- reloading would undo the assault."
                        : $"The portal at {portal.Cell} came back unshielded though its totem still stands.");
            }
            foreach (var camp in OrcCamp.All)
            {
                Assert.AreEqual(BalanceConfig.Instance.OrcGarrisonSize, camp.Guards.Count, "A camp came back without its garrison.");
            }
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

            // A portal is the target worth testing: it is the map's objective, and unlike an orc it
            // stands still, so the group's rally point cannot drift between the save and the assert
            // and disguise a lost order as a moved one.
            var townHall = PlaytestWorld.Building("Castle");
            PlaytestWorld.Place(townHall, PlaytestWorld.FindFreeArea(townHall.footprintSize));
            yield return WaitForPortals();

            var army = ArmyManager.Instance;
            CitizenManager.Instance.SetPopulation(6);
            ResourceManager.Instance.SetAmount(ResourceType.Coins, 500);
            Assert.IsTrue(army.TryRecruit(SoldierType.Militia, WalkablePoint()), "Recruitment was refused with population and coins to spare.");

            var group = army.Groups[0];
            // The LAST portal, not the first: with several on the map, a save that stores "a
            // portal" rather than WHICH portal restores the order onto whichever one comes back
            // first, and pointing at index zero is exactly the bug that would hide.
            var targetPortal = OrcPortal.All[OrcPortal.All.Count - 1];
            var targetCell = targetPortal.Cell;
            group.OrderAttack(targetPortal);
            army.SelectGroup(group);
            Assert.IsNotNull(group.AttackTarget, "The test's own order did not take, so there is nothing to save.");

            Object.FindAnyObjectByType<GameSaveController>().SaveGame(SaveName);

            // Loaded WITHOUT the usual suspend, so that the flag coming back true can only have
            // come out of the save file -- a fresh OrcRaidManager has it false.
            yield return StartGame(SaveName, suspendRaidsAfterLoad: false);

            var loadedArmy = ArmyManager.Instance;
            Assert.AreEqual(1, loadedArmy.Groups.Count, "The group did not come back, so there is nothing to have been ordered.");

            var loadedGroup = loadedArmy.Groups[0];
            CollectionAssert.IsNotEmpty(OrcPortal.All, "The portals did not come back, so the order has nothing to point at.");
            Assert.IsNotNull(loadedGroup.AttackTarget,
                "The assault was called off by the reload: the group came back holding its ground.");
            var restoredTarget = loadedGroup.AttackTarget as OrcPortal;
            Assert.IsNotNull(restoredTarget, "The group came back attacking something that is not a portal at all.");
            Assert.AreEqual(targetCell, restoredTarget.Cell,
                "The group came back attacking a different portal than the one it was sent to break.");

            Assert.AreSame(loadedGroup, loadedArmy.SelectedGroup,
                "The player came back with no group selected, so a tap on the world opens a building panel instead of ordering the army.");

            Assert.IsTrue(OrcRaidManager.Instance.RaidsSuspended,
                "The suspended raid clock started itself again on load, so a wave arrives in the middle of whatever it was switched off to watch.");
        }

        /// <summary>
        /// OrcRaidManager opens the map's portals in its own Update, on the first frame it sees a
        /// Town Hall -- all of them within that one frame, so this is really waiting for the Town
        /// Hall to register rather than for them to trickle in.
        ///
        /// Asserts on the count only enough for the tests here to mean something: at least two, so
        /// that "the save restored the RIGHT portal" is a question with a wrong answer. Not the
        /// sheet's full count, deliberately -- these tests place their Town Hall wherever
        /// PlaytestWorld finds room, which is the map's corner, and a slot pointing off the edge of
        /// the map is skipped by design. What is on trial here is the save surviving whatever the
        /// map opened, not how many it opened; PortalOpeningTests is where the count belongs.
        /// </summary>
        private static IEnumerator WaitForPortals()
        {
            for (var frame = 0; frame < 120 && !OrcRaidManager.Instance.PortalsPlaced; frame++)
            {
                yield return null;
            }

            Assert.IsTrue(OrcRaidManager.Instance.PortalsPlaced, "The raid manager never got round to opening the portals.");
            Assert.GreaterOrEqual(OrcPortal.All.Count, 2,
                "Fewer than two portals opened next to the Town Hall, so there is nothing here to tell one apart from another.");
        }

        /// <summary>Every standing portal's health, keyed by the cell it stands on -- the only identity a portal keeps across a save, since the load builds new objects.</summary>
        private static Dictionary<Vector2Int, int> PortalHealthByCell()
        {
            var healths = new Dictionary<Vector2Int, int>();
            foreach (var portal in OrcPortal.All)
            {
                healths[portal.Cell] = portal.CurrentHealth;
            }

            return healths;
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
