using System.Collections;
using System.Linq;
using CityBuilder.Core;
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
    /// One food counter (the user, 2026-10-25): the resource bar along the top shows Food and none
    /// of the food chain's middle links; the round inventory button opens every resource, those
    /// links included; and an old save's bread comes back baked into Food.
    /// </summary>
    public class FoodAndInventoryTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map3";
        private const string SaveName = "PlayModeFoodAndInventory";

        [UnityTearDown]
        public IEnumerator Clear()
        {
            SaveSystem.Delete(SaveName);
            Time.timeScale = 1f;
            yield return null;
        }

        private static IEnumerator StartGame(string saveNameToLoad)
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            GameSessionIntent.SaveNameToLoad = saveNameToLoad;
            GameSessionIntent.NewGameMapId = MapId;
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);
            yield return null;
            ModalGate.SetBlocked(false);
        }

        [UnityTest]
        public IEnumerator TheInventory_HoldsEveryResource_TheBarOnlyFood()
        {
            yield return StartGame(null);
            var bar = GameObject.Find("ResourceHUD");
            Assert.IsNotNull(bar, "No resource bar.");
            Assert.IsNotNull(bar.transform.Find("Icon_Food"), "The resource bar has no Food.");
            foreach (var middle in new[] { "Grain", "Flour", "Meat", "Bread" })
            {
                Assert.IsNull(bar.transform.Find("Icon_" + middle), $"The resource bar still shows {middle} -- only Food belongs up there.");
            }

            var inventory = Object.FindAnyObjectByType<InventoryPanelController>(FindObjectsInactive.Include);
            Assert.IsNotNull(inventory, "No inventory in the scene.");
            Assert.IsNotNull(GameObject.Find("InventoryButton"), "No round inventory button.");
            CollectionAssert.IsSubsetOf(new[] { ResourceType.Food, ResourceType.Grain, ResourceType.Flour, ResourceType.Meat, ResourceType.Wood, ResourceType.Coins }, inventory.Types);
            CollectionAssert.DoesNotContain(inventory.Types, ResourceType.Bread, "The inventory lists bread, which nothing makes any more.");

            ResourceManager.Instance.SetAmount(ResourceType.Wood, 120);
            ResourceManager.Instance.SetAmount(ResourceType.Stone, 60);
            ResourceManager.Instance.SetAmount(ResourceType.Food, 45);
            ResourceManager.Instance.SetAmount(ResourceType.Flour, 12);
            ResourceManager.Instance.SetAmount(ResourceType.Meat, 4);
            ResourceManager.Instance.SetAmount(ResourceType.Grain, 7);
            if (PlaytestCapture.Available) yield return PlaytestCapture.ShootWithUiAt("inventory-closed", 1600, 900, Vector3.zero, 40f, 50f, 25f);
            inventory.OpenPanel();
            try
            {
                if (PlaytestCapture.Available) yield return PlaytestCapture.ShootWithUiAt("inventory-open", 1600, 900, Vector3.zero, 40f, 50f, 25f);
                Assert.IsTrue(inventory.IsOpen);
                var row = inventory.transform.GetComponentsInChildren<Transform>(true).First(t => t.name == "Row_Grain");
                Assert.AreEqual("7", row.Find("Amount").GetComponent<UnityEngine.UI.Text>().text, "The wheat's row does not say how much there is.");
                ResourceManager.Instance.SetAmount(ResourceType.Grain, 9);
                Assert.AreEqual("9", row.Find("Amount").GetComponent<UnityEngine.UI.Text>().text, "The open card did not follow the amount.");
            }
            finally
            {
                inventory.ClosePanel();
            }
        }

        [UnityTest]
        public IEnumerator AnOldSavesBread_ComesBackAsFood()
        {
            yield return StartGame(null);
            ResourceManager.Instance.SetAmount(ResourceType.Food, 5);
            ResourceManager.Instance.SetAmount(ResourceType.Bread, 10);
            Object.FindAnyObjectByType<GameSaveController>().SaveGame(SaveName);

            yield return StartGame(SaveName);
            Assert.AreEqual(0, ResourceManager.Instance.GetAmount(ResourceType.Bread), "Bread came back as bread.");
            Assert.AreEqual(5 + Mathf.CeilToInt(10 * GameSaveController.BreadToFood), ResourceManager.Instance.GetAmount(ResourceType.Food),
                "The save's bread was not baked into Food.");
        }
    }
}
