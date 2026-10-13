using System.Collections;
using CityBuilder.Buildings;
using CityBuilder.Resources;
using CityBuilder.Saving;
using CityBuilder.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// The interface, photographed and checked in the running game -- the skin (style "D": parchment,
    /// dark wood, gold, Rubik) is a look, and a look is only verified by being seen. The few things
    /// worth asserting are the ones a broken skin hides silently: a label left white on parchment
    /// (unreadable), and the old Arial face still in use.
    /// </summary>
    public class HudPhotoTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map2";

        private static bool _sceneLoaded;
        private static Vector3 _townHall;

        [UnitySetUp]
        public IEnumerator BuildATownOnce()
        {
            LogAssert.ignoreFailingMessages = true;
            if (_sceneLoaded) yield break;

            Time.timeScale = 1f;
            GameSessionIntent.NewGameMapId = MapId;
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);

            var hall = PlaytestWorld.Building(BuildingIds.TownHall);
            var origin = PlaytestWorld.FindFreeAreaNearTheMiddle(hall.footprintSize + new Vector2Int(4, 4));
            var placed = PlaytestWorld.Place(hall, origin + new Vector2Int(2, 2));
            _townHall = placed.transform.position;
            Object.FindAnyObjectByType<BuildingPlacer>().MarkMandatoryBuildingAlreadyPlaced();

            var resources = ResourceManager.Instance;
            resources.SetAmount(ResourceType.Wood, 148);
            resources.SetAmount(ResourceType.Stone, 62);
            resources.SetAmount(ResourceType.Iron, 14);
            resources.SetAmount(ResourceType.Gold, 3);
            resources.SetAmount(ResourceType.Coins, 85);
            resources.SetAmount(ResourceType.Food, 120);

            // The hotbar comes back once the mandatory placement is over, and the bar re-lays
            // itself on the next resource change -- a few frames for both.
            for (var i = 0; i < 5; i++) yield return null;
            _sceneLoaded = true;
        }

        [OneTimeTearDown]
        public void ForgetTheScene()
        {
            _sceneLoaded = false;
        }

        [Test]
        public void NoLabelOnParchmentIsWhite()
        {
            foreach (var text in Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var backing = WrittenOn(text);
                if (backing == null || backing.sprite == null) continue;
                var name = backing.sprite.name;
                if (name == "pill_wood" || name == "pill_wood_light") continue;
                if (!(name.StartsWith("panel") || name.StartsWith("pill") || name.StartsWith("card"))) continue;

                Color.RGBToHSV(text.color, out _, out var s, out var v);
                Assert.IsFalse(v > 0.85f && s < 0.2f,
                    $"'{text.name}' under '{backing.name}' is written in white on parchment -- it cannot be read.");
            }
        }

        [Test]
        public void TheInterfaceIsSetInRubik()
        {
            foreach (var text in Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Assert.IsNotNull(text.font, $"'{text.name}' has no font.");
                StringAssert.StartsWith("Rubik", text.font.name, $"'{text.name}' is still set in {text.font.name}.");
            }
        }

        [UnityTest]
        public IEnumerator Photograph_TheHud()
        {
            yield return PlaytestCapture.ShootWithUi("hud", _townHall, 34f, 52f, 20f);
        }

        [UnityTest]
        public IEnumerator Photograph_TheExitDialog()
        {
            var dialog = Object.FindAnyObjectByType<ExitToMenuController>(FindObjectsInactive.Include);
            if (dialog != null) dialog.OpenDialog();
            yield return null;
            yield return PlaytestCapture.ShootWithUi("hud-exit-dialog", _townHall, 34f, 52f, 20f);
            if (dialog != null) dialog.CloseDialog();
        }

        [UnityTest]
        public IEnumerator Photograph_TheWorkforcePanel()
        {
            var panel = Object.FindAnyObjectByType<WorkforcePanelController>(FindObjectsInactive.Include);
            if (panel != null) panel.OpenPanel();
            yield return null;
            yield return PlaytestCapture.ShootWithUi("hud-workforce", _townHall, 34f, 52f, 20f);
            if (panel != null) panel.ClosePanel();
        }

        private static Image WrittenOn(Text text)
        {
            for (var t = text.transform.parent; t != null; t = t.parent)
            {
                var image = t.GetComponent<Image>();
                if (image != null && image.sprite != null) return image;
                if (t.GetComponent<Canvas>() != null) return null;
            }
            return null;
        }
    }
}
