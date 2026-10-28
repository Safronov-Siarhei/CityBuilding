using System.Collections;
using CityBuilder.Core;
using CityBuilder.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// The main menu after the user's sketches (2026-10-25): «Продолжить» brings up the two modes, a
    /// mode brings in its screen, Back walks it all backwards and on the main screen asks whether
    /// to quit -- and every look in the settings draws the whole menu in its own kit.
    /// </summary>
    public class MainMenuTests
    {
        private HudSkin _playersSkin;

        [UnitySetUp]
        public IEnumerator OpenTheMenu()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            _playersSkin = GameSettings.HudSkin;
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        [TearDown]
        public void PutItBack()
        {
            GameSettings.HudSkin = _playersSkin;
            ModalGate.SetBlocked(false);
        }

        [UnityTest]
        public IEnumerator ContinueModesAndBack_WalkTheScreens()
        {
            var menu = Object.FindAnyObjectByType<MainMenuController>();
            Assert.IsNotNull(menu);
            var exit = GameObject.Find("Canvas").transform.Find("ExitDialog").gameObject;

            menu.OpenModes();
            yield return Settle();
            Assert.IsNull(menu.ChosenMode);
            Assert.Greater(Group("ModesScreen").alpha, 0.95f, "«Продолжить» did not bring up the modes.");

            menu.ChooseMode((int)GameMode.Invasion);
            yield return Settle();
            Assert.AreEqual(GameMode.Invasion, menu.ChosenMode);
            Assert.Greater(Group("InvasionScreen").alpha, 0.95f, "The invasion's screen did not come in.");
            Assert.Less(Group("ModesScreen").alpha, 0.05f, "The modes stayed on the screen.");

            menu.Back();
            yield return Settle();
            Assert.IsNull(menu.ChosenMode, "Back did not return to the modes.");
            menu.Back();
            yield return Settle();
            Assert.Greater(Group("MainScreen").alpha, 0.95f, "Back did not return to the main screen.");
            Assert.IsFalse(exit.activeSelf);
            menu.Back();
            Assert.IsTrue(exit.activeSelf, "Back on the main screen did not ask whether to quit.");
            menu.Back();
            Assert.IsFalse(exit.activeSelf, "Back did not close the question.");
        }

        /// <summary>The shop's «!» (the user, 2026-10-25) stands until the shop is first opened, and stays gone.</summary>
        [UnityTest]
        public IEnumerator TheShopsBadge_GoesOnceItIsOpened()
        {
            PlayerPrefs.DeleteKey("menu.seen.shop");
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            yield return null;
            yield return null;
            var menu = Object.FindAnyObjectByType<MainMenuController>();
            var badge = GameObject.Find("Canvas").transform.Find("MainScreen/OffersButton/Badge").gameObject;
            Assert.IsTrue(badge.activeSelf, "The shop has no «!» before it was ever opened.");
            menu.OpenShop();
            Assert.IsFalse(badge.activeSelf, "The shop's «!» stayed after it was opened.");
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            yield return null;
            yield return null;
            badge = GameObject.Find("Canvas").transform.Find("MainScreen/OffersButton/Badge").gameObject;
            Assert.IsFalse(badge.activeSelf, "The shop's «!» came back with the menu.");
            PlayerPrefs.DeleteKey("menu.seen.shop");
        }

        /// <summary>A window over the menu -- the offers, a mode's «!» -- puts the screen behind it away, and closing it brings it back (the user, 2026-10-28).</summary>
        [UnityTest]
        public IEnumerator AWindowOverTheMenu_PutsTheScreenBehindItAway()
        {
            var menu = Object.FindAnyObjectByType<MainMenuController>();
            var main = Group("MainScreen");
            menu.ShowMain();
            yield return Settle();
            menu.OpenShop();
            yield return Settle();
            Assert.Less(main.alpha, 0.05f, "The main screen shows round the offers.");
            Assert.IsFalse(main.blocksRaycasts, "The main screen behind the offers still takes taps.");
            menu.CloseShop();
            yield return Settle();
            Assert.Greater(main.alpha, 0.95f, "The main screen did not come back when the offers closed.");

            menu.OpenModes();
            yield return Settle();
            var modes = Group("ModesScreen");
            menu.ShowInfo((int)GameMode.Sandbox);
            yield return Settle();
            Assert.Less(modes.alpha, 0.05f, "The modes show under a mode's «!» -- two «Песочница» plates over each other.");
            yield return Shoot("menu-info-over-modes");
            menu.CloseInfo();
            menu.ShowMain();
            yield return Settle();
        }

        [UnityTest]
        public IEnumerator Photograph_EveryLook()
        {
            var menu = Object.FindAnyObjectByType<MainMenuController>();
            foreach (var skin in HudSkins.All)
            {
                GameSettings.HudSkin = skin;
                menu.ShowMain();
                yield return Settle();
                yield return Shoot($"menu-{skin}-main");

                menu.OpenModes();
                yield return Settle();
                yield return Shoot($"menu-{skin}-modes");

                foreach (var mode in new[] { GameMode.Sandbox, GameMode.Invasion })
                {
                    menu.ChooseMode((int)mode);
                    yield return Settle();
                    yield return Shoot($"menu-{skin}-{mode}");
                    if (skin == HudSkin.Classic)
                    {
                        menu.ShowInfo((int)mode);
                        yield return Settle();
                        yield return Shoot($"menu-{skin}-{mode}-info");
                        menu.CloseInfo();
                    }
                    menu.OpenModes();
                    yield return Settle();
                }
            }
        }

        /// <summary>
        /// The live scene behind the menu (the user's sketch): each of the four, staged on a map,
        /// with people at work in it -- photographed through the menu's own camera.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryBackgroundScene_IsStagedWithPeopleAtWork()
        {
            foreach (MainMenuBackground.Vignette vignette in System.Enum.GetValues(typeof(MainMenuBackground.Vignette)))
            {
                MainMenuBackground.ForcedVignette = vignette;
                SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
                yield return null;
                yield return null;
                var background = Object.FindAnyObjectByType<MainMenuBackground>();
                Assert.IsNotNull(background, "The menu has no background.");
                Assert.AreEqual(vignette, background.Current);
                Assert.Greater(Object.FindObjectsByType<MenuWorker>(FindObjectsSortMode.None).Length, 1, $"No one is at work in the {vignette} scene.");
                yield return new WaitForSeconds(2.5f);
                yield return Shoot($"menu-scene-{vignette}");
            }
        }

        private static IEnumerator Shoot(string name)
        {
            var camera = GameObject.Find("Menu Camera").GetComponent<Camera>();
            return PlaytestCapture.ShootThrough(name, camera, 1920, 1080);
        }

        private static IEnumerator Settle()
        {
            yield return new WaitForSecondsRealtime(0.9f);
        }

        private static CanvasGroup Group(string path)
        {
            var found = GameObject.Find("Canvas").transform.Find(path);
            Assert.IsNotNull(found, $"The menu has no {path}.");
            return found.GetComponent<CanvasGroup>();
        }
    }
}
