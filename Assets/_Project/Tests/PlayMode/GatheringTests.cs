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
