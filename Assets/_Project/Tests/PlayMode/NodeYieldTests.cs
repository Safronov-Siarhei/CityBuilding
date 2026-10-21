using System.Collections;
using System.Linq;
using CityBuilder.Core;
using CityBuilder.Maps;
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
    /// Trees and boulders as stocks (the user, 2026-10-21): a tree holds five and gives one per
    /// visit; a boulder's stock follows its size; a tap shows what is left, counting down.
    /// </summary>
    public class NodeYieldTests
    {
        [UnitySetUp]
        public IEnumerator NewGame()
        {
            LogAssert.ignoreFailingMessages = true;
            GameSessionIntent.SaveNameToLoad = null;
            GameSessionIntent.NewGameMapId = "Map3";
            SceneManager.LoadScene("CityBuilder", LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady("Map3");
            ModalGate.SetBlocked(false);
        }

        [UnityTest]
        public IEnumerator ATreeGivesOneAtATime_ABouldersStockFollowsItsSize_ATapShowsWhatIsLeft()
        {
            var config = BalanceConfig.Instance;
            var tree = ResourceNode.All.First(n => n.ResourceType == ResourceType.Wood && n.GetComponent<TreeGrowth>() == null);
            Assert.AreEqual(config.WoodPerTree, tree.TotalYield, "A tree does not hold wood_per_tree.");
            Assert.AreEqual(1, tree.TakeYield(), "A visit to a tree does not give one wood.");
            Assert.AreEqual(config.WoodPerTree - 1, tree.RemainingYield, "The tree did not go down by one.");

            var rocks = ResourceNode.All.Where(n => n.ResourceType == ResourceType.Stone).ToList();
            var table = Enumerable.Range(0, 10).Select(config.StoneForRockSize).ToList();
            Assert.IsTrue(rocks.All(r => table.Contains(r.TotalYield)), "A boulder holds a stock that is not in the size table.");
            Assert.Greater(rocks.Select(r => r.TotalYield).Distinct().Count(), 2, "Boulders of different sizes hold the same stock.");

            // a tap on the tree: the badge over it says four, then three after the next visit
            var selector = Object.FindAnyObjectByType<CitizenSelector>();
            var camera = selector.TapCamera;
            var rig = Object.FindAnyObjectByType<CityBuilder.CameraControl.RTSCameraController>();
            rig.JumpTo(tree.transform.position);
            yield return null;
            yield return null;
            var screen = camera.WorldToScreenPoint(tree.transform.position + Vector3.up * 0.8f);
            Assert.AreEqual(tree, selector.FindNodeAt(screen), "A tap on the tree does not find it.");

            NodeYieldBubble.Show(tree, camera);
            yield return null;
            Assert.AreEqual(tree, NodeYieldBubble.Showing, "No badge over the tapped tree.");
            Assert.AreEqual((config.WoodPerTree - 1).ToString(), NodeYieldBubble.Text, "The badge does not show what is left.");
            tree.TakeYield();
            yield return null;
            Assert.AreEqual((config.WoodPerTree - 2).ToString(), NodeYieldBubble.Text, "The badge did not count down.");
            // a picture for the user
            var placer = Object.FindAnyObjectByType<CityBuilder.Buildings.BuildingPlacer>();
            placer.MarkMandatoryBuildingAlreadyPlaced();
            placer.ClearSelection();
            foreach (var ghost in Object.FindObjectsByType<CityBuilder.Buildings.PlacementPreview>(FindObjectsSortMode.None)) ghost.gameObject.SetActive(false);
            NodeYieldBubble.Show(tree, camera);
            yield return PlaytestCapture.ShootWithUiAt("node-yield-badge", 1600, 900, tree.transform.position, 6f, 40f, 20f);

            // and the bar over a tree being worked, and over a boulder
            tree.ReportHarvestProgress(0.6f);
            var rock = ResourceNode.All.OrderBy(r => (r.transform.position - tree.transform.position).sqrMagnitude)
                .First(r => r.ResourceType == ResourceType.Stone);
            rock.ReportHarvestProgress(0.3f);
            // through the game's own camera: the bars turn to face that one
            rig.JumpTo(tree.transform.position);
            yield return null;
            yield return PlaytestCapture.ShootThrough("harvest-bar", camera, 1600, 900);
            rig.JumpTo(rock.transform.position);
            yield return null;
            yield return PlaytestCapture.ShootThrough("harvest-bar-stone", camera, 1600, 900);
        }
    }
}
