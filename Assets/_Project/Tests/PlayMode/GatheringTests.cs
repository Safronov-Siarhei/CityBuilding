using System.Collections;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
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
    /// Gathering after it stopped being a timer.
    ///
    /// The headline change is invisible from EditMode and easy to get wrong in a way nothing else
    /// would catch: a Sawmill and a Quarry no longer tick their resource out of thin air, so if
    /// ProductionBuilding kept paying them, both the radius and the depletion would be decoration
    /// over an economy that ignored them.
    ///
    /// Everything here has to be deterministic on a map that already has 140 boulders scattered
    /// at random, which is harder than it looks: the first version of the radius tests passed
    /// with the radius check deleted, because the worker simply picked one of the map's own rocks
    /// instead of the far one the test had planted. HideScatteredNodes is the answer, and the
    /// reason those two tests are worth anything.
    /// </summary>
    public class GatheringTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map1";

        private static bool _sceneLoaded;

        private readonly List<BuildingInstance> _placed = new List<BuildingInstance>();
        private readonly List<GameObject> _nodes = new List<GameObject>();
        private readonly List<GameObject> _hidden = new List<GameObject>();

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

            Time.timeScale = 1f;
            ResourceManager.Instance.SetInfiniteResources(false);

            // A Town Hall FIRST, and not as scenery: CitizenVisualsManager resolves the town
            // centre from it and spawns nobody at all without one. The first version of these
            // tests had no Town Hall, so there were no citizens, so no worker ever claimed
            // anything -- and "the far boulder was not taken" passed for the emptiest of reasons.
            Place("Castle");
            CitizenManager.Instance.SetPopulation(10);

            // Upgrading needs the level researched (see ResearchGateTests, where that rule is
            // actually tested). These tests are about what a gatherer's reach does, so they start
            // with the tech list granted.
            Research.ResearchManager.Instance?.CompleteEverything();
            yield return null;
        }

        [TearDown]
        public void CleanUp()
        {
            ModalGate.SetBlocked(false);
            Grid.HarvestRadiusOverlay.HideIfShown();

            foreach (var node in _hidden)
            {
                if (node != null) node.SetActive(true);
            }
            _hidden.Clear();

            foreach (var node in _nodes)
            {
                if (node != null) Object.DestroyImmediate(node);
            }
            _nodes.Clear();

            foreach (var building in _placed)
            {
                if (building != null) Object.DestroyImmediate(building.gameObject);
            }
            _placed.Clear();
        }

        private BuildingInstance Place(string id)
        {
            var data = PlaytestWorld.Building(id);
            Assert.IsNotNull(data, $"No {id} in the building catalogue.");

            var cell = PlaytestWorld.FindFreeArea(data.footprintSize);
            Assert.AreNotEqual(new Vector2Int(-1, -1), cell, $"Nowhere free to put a {id}.");

            var instance = PlaytestWorld.Place(data, cell);
            _placed.Add(instance);
            return instance;
        }

        /// <summary>A bare harvestable node at an exact distance from a point -- the scattered boulders land wherever Random puts them, which is no use for asking about a specific distance.</summary>
        private ResourceNode PlaceNode(ResourceType type, Vector3 origin, float metresAway)
        {
            var go = new GameObject($"Test{type}Node");
            go.transform.position = origin + new Vector3(metresAway, 0f, 0f);
            var node = go.AddComponent<ResourceNode>();
            node.Initialize(type);
            _nodes.Add(go);
            return node;
        }

        /// <summary>
        /// Clears the map's own scattered boulders out of the way for the duration of one test.
        ///
        /// Without this, a radius test proves nothing: 140 boulders land at random, so the nearest
        /// one is almost always closer than any node a test deliberately puts out of reach, and a
        /// worker with the radius check REMOVED would still pick a near rock and look correct.
        /// Verified by breaking it -- with the filter deleted, this is what makes the test fail.
        ///
        /// Deactivating rather than destroying: ResourceNode deregisters itself in OnDisable, so
        /// the node search stops seeing them, and TearDown hands the map back intact.
        /// </summary>
        private void HideScatteredNodes(ResourceType type)
        {
            foreach (var node in new List<ResourceNode>(ResourceNode.All))
            {
                if (node == null || node.ResourceType != type) continue;
                if (_nodes.Contains(node.gameObject)) continue;

                node.gameObject.SetActive(false);
                _hidden.Add(node.gameObject);
            }
        }

        /// <summary>
        /// Puts one worker on a building, and insists there is a citizen in the world to BE that
        /// worker. The assertion is not paranoia: without it, every "nobody gathered the far
        /// boulder" test passes on an empty map with nobody in it.
        /// </summary>
        private static ProductionBuilding Staff(BuildingInstance building)
        {
            var production = building.GetComponent<ProductionBuilding>();
            Assert.IsNotNull(production, $"{building.Data.buildingName} has no worker slots.");
            Assert.IsTrue(production.TryAssignWorker(), "Could not put a worker on the building.");

            Assert.Greater(CitizenVisualsManager.Instance.AllAgents.Count, 0,
                "No citizens exist, so nothing here is being tested -- is the Town Hall missing?");
            return production;
        }

        [UnityTest]
        public IEnumerator AGatherer_EarnsNothingFromTheTick()
        {
            // The whole point of the change: before it, a Quarry ticked stone out of thin air and
            // would have kept doing so with every boulder in the county worked out.
            var quarry = Place("Quarry");
            var production = Staff(quarry);

            Assert.IsTrue(production.GathersFromNodes, "A Quarry is supposed to earn by sending workers out, not by ticking.");

            // The window has to cover several production ticks and still be shorter than the
            // fastest possible delivery, or a worker genuinely earning its keep would look like
            // the tick that is supposed to be gone. Asserted rather than assumed, so retuning
            // harvest_seconds down fails here loudly instead of making this test lie.
            var window = quarry.Data.productionIntervalSeconds * 2f + 0.1f;
            Assert.Greater(BalanceConfig.Instance.HarvestSeconds, window,
                "This test can no longer tell a tick from a delivery -- one dig now finishes inside the window it waits.");

            var before = ResourceManager.Instance.GetAmount(ResourceType.Stone);
            yield return new WaitForSeconds(window);

            Assert.AreEqual(before, ResourceManager.Instance.GetAmount(ResourceType.Stone),
                "A Quarry produced stone before any worker could possibly have carried some home -- the recipe tick is still paying it.");
        }

        [UnityTest]
        public IEnumerator AConverter_StillEarnsOnItsTimer()
        {
            // The other half of the same claim: only gatherers stepped off the tick. A farm that
            // stopped producing would be a far worse bug than the one being fixed.
            var farm = Place("Farm");
            var production = Staff(farm);
            Assert.IsFalse(production.GathersFromNodes, "A Farm does not send anyone out to a tree; it must still tick.");

            var before = ResourceManager.Instance.GetAmount(ResourceType.Grain);
            yield return new WaitForSeconds(farm.Data.productionIntervalSeconds * 2f + 0.5f);

            Assert.Greater(ResourceManager.Instance.GetAmount(ResourceType.Grain), before,
                "The Farm stopped producing -- the gatherer exemption is catching buildings it should not.");
        }

        [UnityTest]
        public IEnumerator ABoulderOutsideTheRadius_IsLeftAlone()
        {
            var quarry = Place("Quarry");
            var radius = quarry.HarvestRadius;
            Assert.Greater(radius, 0, "A Quarry with no harvest radius would send its workers to the far end of the map.");

            HideScatteredNodes(ResourceType.Stone);
            var tooFar = PlaceNode(ResourceType.Stone, quarry.transform.position, radius + 12f);

            Staff(quarry);
            yield return null;

            Assert.IsFalse(tooFar.IsClaimed,
                "The only boulder on the map sits outside the Quarry's radius and a worker took it anyway.");
        }

        [UnityTest]
        public IEnumerator ABoulderInsideTheRadius_IsTakenUp()
        {
            // The other half: the radius must not be so strict that a gatherer refuses work it can
            // plainly reach. Together with the test above, one boulder each side of the line.
            var quarry = Place("Quarry");
            var radius = quarry.HarvestRadius;

            HideScatteredNodes(ResourceType.Stone);
            var withinReach = PlaceNode(ResourceType.Stone, quarry.transform.position, radius - 2f);

            Staff(quarry);
            yield return null;

            Assert.IsTrue(withinReach.IsClaimed, "The only boulder on the map is inside the radius and nobody went for it.");
        }

        [UnityTest]
        public IEnumerator UpgradingAGatherer_WidensItsReach()
        {
            var sawmill = Place("Sawmill");
            var atLevelOne = sawmill.HarvestRadius;
            Assert.Greater(atLevelOne, 0);

            ResourceManager.Instance.SetInfiniteResources(true);
            for (var level = 2; level <= BuildingInstance.MaxLevel; level++)
            {
                var before = sawmill.HarvestRadius;
                Assert.IsTrue(sawmill.TryUpgrade(), $"The Sawmill refused to upgrade to level {level} with infinite resources.");
                Assert.Greater(sawmill.HarvestRadius, before,
                    $"Level {level} of the Sawmill reaches no further than level {level - 1} -- upgrading it buys nothing for where it already stands.");
            }

            Assert.Greater(sawmill.HarvestRadius, atLevelOne);
            yield break;
        }

        [UnityTest]
        public IEnumerator SelectingAGatherer_ShowsItsReach_AndOtherBuildingsShowNone()
        {
            var panel = Object.FindFirstObjectByType<UI.BuildingInfoPanelController>(FindObjectsInactive.Include);
            Assert.IsNotNull(panel, "No building info panel in the scene.");

            var sawmill = Place("Sawmill");
            panel.Show(sawmill);
            yield return null;

            Assert.IsTrue(Grid.HarvestRadiusOverlay.Instance.gameObject.activeSelf,
                "Tapping a Sawmill showed no reach -- the player is being asked to judge it by eye.");

            panel.Close();
            yield return null;
            Assert.IsFalse(Grid.HarvestRadiusOverlay.Instance.gameObject.activeSelf,
                "The reach stayed on screen after the panel closed.");

            // A building with no reach must not leave the last one's carpet lying under it.
            var farm = Place("Farm");
            panel.Show(farm);
            yield return null;

            Assert.IsFalse(Grid.HarvestRadiusOverlay.Instance.gameObject.activeSelf,
                "A Farm was drawn with a harvest radius it does not have.");

            panel.Close();
            ModalGate.SetBlocked(false);
        }

        [UnityTest]
        public IEnumerator ABoulderGivesUpASliceAtATime_AndIsEmptiedInTheEnd()
        {
            var node = PlaceNode(ResourceType.Stone, Vector3.zero, 0f);
            var total = node.TotalYield;
            var slice = ResourceNode.YieldPerHarvestFor(ResourceType.Stone);

            Assert.AreEqual(slice, node.TakeYield(), "The first visit should carry away exactly one trip's worth.");
            Assert.AreEqual(total - slice, node.RemainingYield);
            Assert.IsFalse(node.IsDepleted, "One visit is not supposed to empty a boulder.");

            var guard = 0;
            while (!node.IsDepleted && guard++ < 1000) node.TakeYield();

            Assert.AreEqual(0, node.RemainingYield, "A worked-out boulder should hold nothing, not a negative amount.");
            Assert.AreEqual(0, node.TakeYield(), "An empty boulder handed out stone it did not have.");
            yield break;
        }

        /// <summary>
        /// Finds the bar a node built for itself. Searched in the scene rather than among the
        /// node's children ON PURPOSE: the bar deliberately does not live under the node (see
        /// HarvestProgressBar.CreateFor), and a helper that looked there would go on reporting
        /// "no bar" however well the thing worked.
        /// </summary>
        private static GameObject BarOn(ResourceNode node)
        {
            foreach (var bar in Object.FindObjectsByType<HarvestProgressBar>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var offset = bar.transform.position - node.transform.position;
                offset.y = 0f;
                if (offset.sqrMagnitude < 0.25f) return bar.gameObject;
            }
            return null;
        }

        [UnityTest]
        public IEnumerator ANodeNobodyIsWorking_HasNoBarAtAll()
        {
            // Not a detail: there are 600 trees and 140 boulders on this map, and building a bar
            // for every one of them up front is exactly the sort of thing that quietly costs a
            // phone its frame rate for something nobody is looking at.
            var tree = PlaceNode(ResourceType.Wood, Vector3.zero, 0f);

            Assert.IsNull(BarOn(tree), "A tree nobody has touched already carries a progress bar.");
            yield break;
        }

        [UnityTest]
        public IEnumerator ReportingProgress_ShowsTheBar_AndReleasingHidesIt()
        {
            var tree = PlaceNode(ResourceType.Wood, Vector3.zero, 0f);

            tree.ReportHarvestProgress(0.5f);
            var bar = BarOn(tree);
            Assert.IsNotNull(bar, "Working a tree built no progress bar over it.");
            Assert.IsTrue(bar.activeSelf, "The bar exists but is not on screen while the tree is being chopped.");

            // Release is the path that matters when a worker is pulled off mid-dig: the bar must
            // not be left hanging over a tree nobody is at.
            tree.Release();
            Assert.IsFalse(bar.activeSelf, "Letting go of a tree left its progress bar up.");
            yield break;
        }

        [UnityTest]
        public IEnumerator TheBar_HangsAboveTheNode_WhateverRotationAndScaleTheModelArrivedWith()
        {
            // The bug this is here for: the real tree prefabs are FBX models carrying a corrective
            // 90-degree root rotation from Blender's Z-up authoring, so the bar's original local
            // +Y offset pointed SIDEWAYS. It sat two metres from the trunk at ground level, which
            // is why the player reported seeing no indicator at all. The old test missed it by
            // building its node out of a bare GameObject with no rotation and no scale -- exactly
            // the one shape the bug could not show up in.
            var tree = PlaceNode(ResourceType.Wood, Vector3.zero, 0f);
            tree.transform.rotation = Quaternion.Euler(-90f, 37f, 0f);
            tree.transform.localScale = Vector3.one * 0.1f;

            tree.ReportHarvestProgress(0.4f);
            var bar = BarOn(tree);
            Assert.IsNotNull(bar, "The bar is not anywhere near the tree it belongs to.");

            var offset = bar.transform.position - tree.transform.position;
            Assert.Less(new Vector2(offset.x, offset.z).magnitude, 0.05f,
                "The bar drifted sideways off the tree -- it is being positioned in the model's local space again.");
            Assert.Greater(offset.y, 0f, "The bar is not above the tree.");

            // And it must not shrink with a sapling: TreeGrowth scales young trees to a tenth.
            Assert.AreEqual(1f, bar.transform.lossyScale.x, 0.001f,
                "The bar inherited the tree's scale -- on a sapling it would be a tenth of the size.");
            yield break;
        }

        [UnityTest]
        public IEnumerator ACitizenSentToChop_FillsTheBarOverTheTree()
        {
            // End to end, and the only test here that proves the bar is wired to the actual work
            // rather than merely capable of being shown. Fifteen seconds is a long time to watch a
            // citizen stand next to a tree with no idea whether anything is happening -- that is
            // the entire reason the bar exists, so it is worth one slow test.
            var agent = CitizenVisualsManager.Instance.AllAgents[0];
            Assert.IsNotNull(agent, "No citizen to send.");

            // Right under their feet, so the walk is over before it starts and the test is timing
            // the dig rather than the pathfinding.
            var tree = PlaceNode(ResourceType.Wood, agent.transform.position, 0f);
            Assert.IsTrue(tree.TryClaim());
            agent.GatherFrom(tree);

            yield return new WaitForSeconds(2f);

            var bar = BarOn(tree);
            Assert.IsNotNull(bar, "Two seconds into chopping and the tree has no progress bar.");
            Assert.IsTrue(bar.activeSelf, "The bar was built but never shown while the citizen worked.");

            // Well short of the whole dig: what is being pinned is that it MOVES with the work,
            // not the exact fraction, which would just be re-deriving harvest_seconds here.
            var filled = bar.transform.Find("Fill");
            Assert.IsNotNull(filled);
            Assert.Greater(filled.localScale.x, 0f, "The bar is showing but has not filled at all -- it is not tracking the dig.");
        }

        [UnityTest]
        public IEnumerator ATreeComesDownWhole()
        {
            var tree = PlaceNode(ResourceType.Wood, Vector3.zero, 0f);

            Assert.AreEqual(tree.TotalYield, tree.TakeYield(), "A tree should hand over everything it has in one felling.");
            Assert.IsTrue(tree.IsDepleted);
            yield break;
        }
    }
}
