using System.Collections;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Research;
using CityBuilder.Resources;
using CityBuilder.Saving;
using CityBuilder.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// The Laboratory as the player meets it, in the real scene.
    ///
    /// EditMode covers the arithmetic and the shape of the tech list (see ResearchTests). What only a
    /// running scene can show is the wiring: that a locked building really cannot be picked up, that
    /// an unresearched level really refuses to be paid for, that scientists actually make a research
    /// finish, and that losing the Laboratory mid-research gives the coins back instead of eating them.
    /// </summary>
    public class ResearchGateTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map1";

        /// <summary>A research is a minute long; a test that waits one out in real time is a test nobody runs twice.</summary>
        private const float TestTimeScale = 20f;

        /// <summary>
        /// Comfortably more than any research costs, and comfortably UNDER what the settlement can
        /// hold. The second half is not incidental: a treasury already over its ceiling silently
        /// swallows a refund, the same way it swallows tax income, and a test funded with thousands of
        /// coins would be measuring that instead of the refund.
        /// </summary>
        private const int StartingCoins = 200;

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
            Time.timeScale = 1f;
            ResourceManager.Instance.SetInfiniteResources(false);
            CitizenManager.Instance.SetPopulation(20);

            // Every test here starts from nothing researched -- the class is about the gates, and the
            // scene is shared between tests, so a previous one must not leave its unlocks behind.
            Research().ResetForTesting();
            yield return null;
        }

        [TearDown]
        public void CleanUp()
        {
            var panel = Object.FindFirstObjectByType<ResearchPanelController>(FindObjectsInactive.Include);
            if (panel != null) panel.Close();

            Time.timeScale = 1f;
            foreach (var building in _placed)
            {
                if (building != null) Object.DestroyImmediate(building.gameObject);
            }
            _placed.Clear();
            ModalGate.SetBlocked(false);
        }

        [Test]
        public void ALockedBuilding_CannotBePickedUpFromTheHotbar()
        {
            var placer = Object.FindAnyObjectByType<BuildingPlacer>();
            var smelter = PlaytestWorld.Building("Smelter");
            Assert.IsNotNull(smelter, "No Smelter in the building catalogue.");
            Assert.IsTrue(ResearchCatalog.NeedsUnlock("Smelter"), "The Smelter is supposed to start locked.");

            // The scene starts with the mandatory Town Hall already in hand (BuildingPlacer.Start),
            // so the slate has to be cleared before "nothing is selected" means anything.
            placer.ClearSelection();

            placer.SelectBuilding(smelter);
            Assert.IsFalse(placer.IsSelecting, "A building nobody has researched was picked up anyway.");

            Research().CompleteInstantly(ResearchTopic.UnlockBuildingId("Smelter"));
            placer.SelectBuilding(smelter);
            Assert.IsTrue(placer.IsSelecting, "The Smelter is researched and still cannot be selected.");

            placer.ClearSelection();
        }

        [Test]
        public void AnUnresearchedLevel_RefusesToBeUpgraded_AndChargesNothingForTrying()
        {
            var resources = ResourceManager.Instance;
            resources.SetAmount(ResourceType.Wood, 5000);
            resources.SetAmount(ResourceType.Stone, 5000);
            resources.SetAmount(ResourceType.Iron, 5000);

            var warehouse = Place("Warehouse");
            var woodBefore = resources.GetAmount(ResourceType.Wood);

            Assert.IsFalse(warehouse.NextLevelResearched, "The Warehouse's level 2 is supposed to need research.");
            Assert.IsFalse(warehouse.TryUpgrade(), "An unresearched level was upgraded anyway.");
            Assert.AreEqual(1, warehouse.Level);
            Assert.AreEqual(woodBefore, resources.GetAmount(ResourceType.Wood),
                "The refused upgrade still took the player's timber -- a gate must never charge for saying no.");

            Research().CompleteInstantly(ResearchTopic.BuildingLevelId("Warehouse", 2));

            Assert.IsTrue(warehouse.NextLevelResearched);
            Assert.IsTrue(warehouse.TryUpgrade(), "The level is researched and the upgrade was still refused.");
            Assert.AreEqual(2, warehouse.Level);
        }

        [UnityTest]
        public IEnumerator AResearchRunsOnlyWhileTheLabIsStaffed_AndOpensWhatItPromised()
        {
            var research = Research();
            var lab = PlaceStaffedLab(out var workplace);
            var resources = ResourceManager.Instance;
            resources.SetAmount(ResourceType.Coins, StartingCoins);

            var topic = ResearchCatalog.ById(ResearchTopic.UnlockBuildingId("Smelter"));
            Assert.IsNotNull(topic);

            var coinsBefore = resources.GetAmount(ResourceType.Coins);
            Assert.IsTrue(research.TryStart(topic), $"The research would not start: {research.DescribeBlocker(topic)}");
            Assert.AreEqual(coinsBefore - topic.Coins, resources.GetAmount(ResourceType.Coins), "Starting a research did not take its coins.");

            // With nobody in the lab it stands still rather than being lost.
            workplace.SetAssignedWorkers(0);
            var remainingWhenEmptied = research.RemainingSeconds;
            Time.timeScale = TestTimeScale;
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(remainingWhenEmptied, research.RemainingSeconds, 0.01f,
                "The research kept running with no scientists in the Laboratory.");

            // And a full staff shortens what is left, rather than only speeding up from here.
            workplace.SetAssignedWorkers(workplace.MaxWorkers);
            Assert.Less(research.RemainingSeconds, remainingWhenEmptied,
                "Adding scientists did not take seconds off the research that was already under way.");

            yield return WaitForResearchToFinish(research);
            Time.timeScale = 1f;

            Assert.IsNull(research.Current, "The research never finished.");
            Assert.IsTrue(research.IsBuildingUnlocked("Smelter"), "The finished research did not open the building it was for.");
            Assert.IsFalse(lab == null, "The Laboratory disappeared during the test.");
        }

        [Test]
        public void CancellingAResearch_PaysBackMostOfTheCoins()
        {
            var research = Research();
            PlaceStaffedLab(out _);
            var resources = ResourceManager.Instance;
            resources.SetAmount(ResourceType.Coins, StartingCoins);

            var topic = ResearchCatalog.ById(ResearchTopic.UnlockBuildingId("Smelter"));
            var before = resources.GetAmount(ResourceType.Coins);
            Assert.IsTrue(research.TryStart(topic));

            var refund = research.CurrentCancelRefund;
            Assert.Greater(refund, 0);
            Assert.Less(refund, topic.Coins, "Cancelling pays back a share, not the whole price -- otherwise starting a research costs nothing.");

            research.CancelCurrent();

            Assert.IsNull(research.Current);
            Assert.AreEqual(before - topic.Coins + refund, resources.GetAmount(ResourceType.Coins));
            Assert.IsFalse(research.IsBuildingUnlocked("Smelter"), "A cancelled research still opened its building.");
        }

        /// <summary>
        /// The player did not choose this, an orc did -- so all of the coins come back, unlike a
        /// cancellation, and the research is simply not done.
        /// </summary>
        [UnityTest]
        public IEnumerator LosingTheLabMidResearch_RefundsEveryCoin()
        {
            var research = Research();
            var lab = PlaceStaffedLab(out _);
            var resources = ResourceManager.Instance;
            resources.SetAmount(ResourceType.Coins, StartingCoins);

            var topic = ResearchCatalog.ById(ResearchTopic.UnlockBuildingId("Smelter"));
            var before = resources.GetAmount(ResourceType.Coins);
            Assert.IsTrue(research.TryStart(topic));

            PlaytestWorld.Demolish(lab);
            // Twice: Destroy is deferred to the end of this frame, and ResearchManager.Update only
            // notices the lab is gone on the frame after that.
            yield return null;
            yield return null;

            Assert.IsNull(research.Current, "The research carried on with no Laboratory to do it in.");
            Assert.AreEqual(before, resources.GetAmount(ResourceType.Coins),
                "Losing the Laboratory ate the coins the research had been paid for.");
            Assert.IsFalse(research.IsBuildingUnlocked("Smelter"));
        }

        /// <summary>
        /// The window's list is deliberately compact: what can be researched now plus what already
        /// has been. At a level-1 Laboratory that means unlocks and nothing else -- a level 2 needs a
        /// level-2 Laboratory, so listing it would be a row the player cannot act on.
        /// </summary>
        [Test]
        public void TheWindowListsWhatThisLabCanActuallyResearch()
        {
            var research = Research();
            var lab = PlaceStaffedLab(out _);

            var panel = Object.FindFirstObjectByType<ResearchPanelController>(FindObjectsInactive.Include);
            Assert.IsNotNull(panel, "The scene has no research window.");

            panel.Show(lab);
            Assert.Greater(panel.RowCount, 0, "A level-1 Laboratory offers nothing to research at all.");

            foreach (var topic in ResearchCatalog.BuildingTopics)
            {
                if (topic.RequiredLabLevel <= 1) continue;
                Assert.IsFalse(research.IsAvailable(topic),
                    $"{topic.Id} is offered by a level-1 Laboratory, which cannot research it.");
            }

            Assert.IsFalse(panel.IsUnitsTab);
            panel.SelectUnitsTab();
            Assert.IsTrue(panel.IsUnitsTab, "The soldiers tab did not open.");

            panel.Close();
        }

        /// <summary>
        /// The soldiers' tab is empty on a level-1 Laboratory and there is nothing wrong with that:
        /// the militia starts unlocked, so its only topics are its two levels, and a level-N
        /// research needs a level-N Laboratory. What was wrong was the window saying only "nothing
        /// to research here yet", which reads as a broken tab rather than as a locked one.
        /// </summary>
        [Test]
        public void TheSoldiersTabNamesTheLabLevelThatWouldOpenIt()
        {
            var lab = PlaceStaffedLab(out _);
            var panel = Object.FindFirstObjectByType<ResearchPanelController>(FindObjectsInactive.Include);
            Assert.IsNotNull(panel, "The scene has no research window.");

            var wanted = int.MaxValue;
            foreach (var topic in ResearchCatalog.UnitTopics)
            {
                if (topic.RequiredLabLevel < wanted) wanted = topic.RequiredLabLevel;
            }
            Assert.AreNotEqual(int.MaxValue, wanted, "The units tab of the sheet authors no research at all, so there is nothing for this tab to promise.");

            panel.Show(lab);
            panel.SelectUnitsTab();

            Assert.AreEqual(0, panel.RowCount, "A level-1 Laboratory is not supposed to be able to research a level-2 soldier.");
            Assert.AreEqual(Localization.Format("#research_empty_lab_level", wanted), panel.EmptyMessage,
                "The empty soldiers tab does not say which Laboratory level would fill it.");

            panel.Close();
        }

        private static ResearchManager Research()
        {
            var research = ResearchManager.Instance;
            Assert.IsNotNull(research, "The scene has no ResearchManager.");
            return research;
        }

        /// <summary>A Laboratory with every scientist slot filled, which is the state most of these tests need before they can start anything.</summary>
        private BuildingInstance PlaceStaffedLab(out ProductionBuilding workplace)
        {
            var lab = Place(ResearchManager.LaboratoryBuildingId);
            workplace = lab.GetComponent<ProductionBuilding>();
            Assert.IsNotNull(workplace, "The Laboratory prefab has no worker slots -- max_workers on its sheet row is 0.");
            Assert.Greater(workplace.MaxWorkers, 1, "The Laboratory is supposed to take up to ten scientists.");

            workplace.SetAssignedWorkers(workplace.MaxWorkers);
            Assert.Greater(workplace.AssignedWorkers, 0, "Nobody could be put to work in the Laboratory.");
            return lab;
        }

        private BuildingInstance Place(string buildingId)
        {
            var data = PlaytestWorld.Building(buildingId);
            Assert.IsNotNull(data, $"No {buildingId} in the building catalogue.");

            var cell = PlaytestWorld.FindFreeArea(data.footprintSize);
            var instance = PlaytestWorld.Place(data, cell);
            _placed.Add(instance);
            return instance;
        }

        private static IEnumerator WaitForResearchToFinish(ResearchManager research)
        {
            // Bounded rather than open-ended: a research that never completes must fail the test
            // rather than hang the whole run.
            var deadline = Time.realtimeSinceStartup + 20f;
            while (research.Current != null && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }
    }
}
