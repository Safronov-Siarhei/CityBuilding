using System.Collections;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Resources;
using CityBuilder.Saving;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// The half of the progression score that PlayerProgressionTests cannot reach: the SCAN.
    ///
    /// Compute is a pure sum, pinned term by term in EditMode -- but it is only ever as good as the
    /// numbers handed to it, and those come from a walk over the live scene's BuildingInstances,
    /// the citizen count and the army's groups. A term that scans the wrong thing, or nothing at
    /// all, still yields a score that ramps and looks perfectly plausible; raids would simply stop
    /// noticing one whole half of what the player had done.
    ///
    /// So every test here changes ONE thing in the running game and asserts the score moved. They
    /// are written as DELTAS on purpose: the scene is loaded once for the fixture, so a test can
    /// never assume it starts on a bare map.
    /// </summary>
    public class ProgressionScoreTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map1";

        private static bool _sceneLoaded;

        private readonly List<BuildingInstance> _placed = new List<BuildingInstance>();

        [UnitySetUp]
        public IEnumerator PrepareScene()
        {
            LogAssert.ignoreFailingMessages = true;

            if (!_sceneLoaded)
            {
                Time.timeScale = 1f;
                GameSessionIntent.NewGameMapId = MapId;
                SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
                yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);
                _sceneLoaded = true;
            }

            ModalGate.SetBlocked(false);
            // Nothing here is about fighting, and a wave arriving mid-test would move the score
            // under the assert by knocking a building down.
            if (OrcRaidManager.Instance != null) OrcRaidManager.Instance.RaidsSuspended = true;
            yield return null;
        }

        [TearDown]
        public void CleanUp()
        {
            foreach (var building in _placed)
            {
                if (building != null) Object.DestroyImmediate(building.gameObject);
            }
            _placed.Clear();
        }

        private BuildingInstance Place(string buildingId)
        {
            var data = PlaytestWorld.Building(buildingId);
            Assert.IsNotNull(data, $"No '{buildingId}' in the catalogue, so this test has nothing to build.");

            var instance = PlaytestWorld.Place(data, PlaytestWorld.FindFreeArea(data.footprintSize));
            _placed.Add(instance);
            return instance;
        }

        [UnityTest]
        public IEnumerator BuildingSomething_RaisesTheScore()
        {
            var before = PlayerProgression.Score();

            Place("Barn");
            yield return null;

            Assert.Greater(PlayerProgression.Score(), before,
                "Putting a building up did not register at all -- the buildings term is scanning nothing.");
        }

        /// <summary>Levels are summed rather than buildings counted, so upgrading has to move the score on its own, with nothing new built.</summary>
        [UnityTest]
        public IEnumerator UpgradingSomething_RaisesTheScoreWithoutBuildingMore()
        {
            var instance = Place("Barn");
            yield return null;

            var beforeUpgrade = PlayerProgression.Score();
            instance.SetLevel(instance.Level + 1);
            yield return null;

            Assert.Greater(PlayerProgression.Score(), beforeUpgrade,
                "An upgraded settlement scores the same as an un-upgraded one, so the buildings term is counting heads instead of levels.");
        }

        [UnityTest]
        public IEnumerator MorePeople_RaiseTheScore()
        {
            CitizenManager.Instance.SetPopulation(5);
            yield return null;
            var before = PlayerProgression.Score();

            CitizenManager.Instance.SetPopulation(25);
            yield return null;

            Assert.Greater(PlayerProgression.Score(), before, "Population does not count towards the raids a settlement draws.");
        }

        /// <summary>Defence is read as the defence STAT rather than as a count of walls, which is why a tower has to outweigh a storehouse of the same level.</summary>
        [UnityTest]
        public IEnumerator ATower_CountsForMoreThanAStorehouse()
        {
            var barn = Place("Barn");
            yield return null;
            var withBarn = PlayerProgression.Score();

            Object.DestroyImmediate(barn.gameObject);
            _placed.Remove(barn);
            yield return null;

            var tower = Place("Tower");
            Assert.Greater(tower.Defense, 0, "The fixture picked a building with no defence, so it cannot tell the two terms apart.");
            yield return null;

            Assert.Greater(PlayerProgression.Score(), withBarn,
                "A tower and a storehouse are worth the same to the orcs, so the defence term is not being read.");
        }

        [UnityTest]
        public IEnumerator RecruitingASoldier_RaisesTheScore()
        {
            CitizenManager.Instance.SetPopulation(10);
            ResourceManager.Instance.SetAmount(ResourceType.Coins, 500);
            yield return null;

            var before = PlayerProgression.Score();

            var soldier = ArmyManager.Instance.TryRecruit(SoldierType.Militia, Vector3.zero);
            Assert.IsTrue(soldier, "Recruitment was refused with population and coins to spare.");
            yield return null;

            Assert.Greater(PlayerProgression.Score(), before, "The garrison does not count, so an army is free as far as the orcs are concerned.");
        }

        /// <summary>
        /// The production term, and the property that makes it worth having: what the settlement has
        /// MADE raises the score, and spending it again does not lower it. A stockpile-based score
        /// would make emptying the stores just before a raid the cheapest defence in the game.
        /// </summary>
        [UnityTest]
        public IEnumerator WhatWasProducedCounts_AndSpendingItDoesNot()
        {
            // Emptied first, and the store checked afterwards. AddProduced counts what FITTED, and
            // by the time this test runs the fixture's citizens have been felling trees for several
            // tests -- the first version of it produced into a nearly full warehouse, stored almost
            // nothing, and failed while looking like the production term was not wired at all.
            ResourceManager.Instance.SetAmount(ResourceType.Wood, 0);
            yield return null;

            var before = PlayerProgression.Score();

            var stored = ResourceManager.Instance.AddProduced(ResourceType.Wood, 150);
            Assert.AreEqual(150, stored, "The fixture could not store its own output, so it proves nothing about the score.");
            yield return null;
            var afterProducing = PlayerProgression.Score();

            Assert.Greater(afterProducing, before, "Everything the settlement has ever made does not count.");

            ResourceManager.Instance.Add(ResourceType.Wood, -150);
            yield return null;

            Assert.AreEqual(afterProducing, PlayerProgression.Score(),
                "Spending lowered the score, so a player could be poor on demand and be raided as a pauper.");
        }

        /// <summary>
        /// What all of it is for, and the one balance claim worth pinning here: a settlement of the
        /// size a player actually reaches scores high enough to CHANGE the raid. Weights that are
        /// all technically wired but two orders of magnitude too small would pass every test above
        /// and leave raids permanently at the opening squad.
        /// </summary>
        [UnityTest]
        public IEnumerator ARealSettlement_DrawsMoreThanTheOpeningSquad()
        {
            CitizenManager.Instance.SetPopulation(60);
            // Emptied first for the same reason as the test above: output that does not fit is
            // output the settlement never made.
            ResourceManager.Instance.SetAmount(ResourceType.Wood, 0);
            ResourceManager.Instance.AddProduced(ResourceType.Wood, 5000);
            for (var i = 0; i < 4; i++)
            {
                Place("Tower");
            }
            yield return null;

            var score = PlayerProgression.Score();

            Assert.Greater(OrcRaidManager.ComputeRaidSize(score), OrcRaidManager.ComputeRaidSize(0),
                "A whole town draws the same squad an empty map does -- the weights are too small to reach the sheet's step.");
            Assert.Less(OrcRaidManager.ComputeRaidIntervalSeconds(score), OrcRaidManager.ComputeRaidIntervalSeconds(0),
                "Growing the settlement does not bring the waves any closer together.");
        }
    }
}
