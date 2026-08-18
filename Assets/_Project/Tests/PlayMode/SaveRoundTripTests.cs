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

        /// <summary>Loads the game scene the way the main menu does: a save name to continue one, null to start fresh.</summary>
        private static IEnumerator StartGame(string saveNameToLoad)
        {
            Time.timeScale = 1f;
            GameSessionIntent.SaveNameToLoad = saveNameToLoad;
            GameSessionIntent.NewGameMapId = MapId;
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);

            // GameSaveController restores the saved state in its Start(); give it the frame.
            yield return null;

            ModalGate.SetBlocked(false);
            if (OrcRaidManager.Instance != null) OrcRaidManager.Instance.RaidsSuspended = true;
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
