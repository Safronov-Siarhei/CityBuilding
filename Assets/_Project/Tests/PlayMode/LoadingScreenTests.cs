using System.Collections;
using CityBuilder.Core;
using CityBuilder.Invasion;
using CityBuilder.Maps;
using CityBuilder.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// The drawn loading screen (the user, 2026-10-25): between the menu and a game both ways, the
    /// sandbox's quill over the map's chart, the Invasion's painted scene, a tip; it goes once the
    /// new scene is up.
    /// </summary>
    public class LoadingScreenTests
    {
        private MapTheme _theme;

        [UnitySetUp]
        public IEnumerator OpenTheMenu()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            _theme = GameSettings.InvasionTheme;
            InvasionRecords.FilePathOverride = System.IO.Path.Combine(Application.temporaryCachePath, "invasion_records_test.json");
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator PutItBack()
        {
            for (var t = 0f; t < 20f && LoadingScreen.Busy; t += Time.unscaledDeltaTime) yield return null;
            GameSettings.InvasionTheme = _theme;
            InvasionRecords.FilePathOverride = null;
            ModalGate.SetBlocked(false);
        }

        private static IEnumerator WaitForTheScreenToGo()
        {
            for (var t = 0f; t < 25f && LoadingScreen.Busy; t += Time.unscaledDeltaTime) yield return null;
            Assert.IsFalse(LoadingScreen.Busy, "The loading screen never went.");
        }

        [UnityTest]
        public IEnumerator ASandboxGame_LoadsBehindTheQuill_AndTheMenuBehindItAgain()
        {
            var sandbox = Object.FindAnyObjectByType<SandboxScreenController>(FindObjectsInactive.Include);
            Object.FindAnyObjectByType<MainMenuController>().StartNewGame();
            sandbox.Pick("Map3");
            sandbox.StartGame();
            yield return null;
            Assert.IsTrue(LoadingScreen.Busy, "No loading screen on the way into the game.");
            var screen = Object.FindAnyObjectByType<LoadingScreen>();
            Assert.IsTrue(screen.transform.Find("Sandbox").gameObject.activeSelf, "The sandbox is not loading behind its chart.");
            yield return new WaitForSecondsRealtime(1.3f);
            yield return PlaytestCapture.ShootWithUi("loading-sandbox", Vector3.zero);
            yield return WaitForTheScreenToGo();
            Assert.AreEqual("Map3", MeshMapApplier.Instance.CurrentMapId, "The game behind the screen is not on the map picked.");

            Object.FindAnyObjectByType<GameMenuController>().GoToMainMenu();
            yield return null;
            Assert.IsTrue(LoadingScreen.Busy, "No loading screen on the way out to the menu.");
            yield return WaitForTheScreenToGo();
            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator ARun_LoadsBehindItsThemesPainting()
        {
            GameSettings.InvasionTheme = MapTheme.NewYear;
            Object.FindAnyObjectByType<MainMenuController>().StartInvasion();
            yield return null;
            Assert.IsTrue(LoadingScreen.Busy, "No loading screen on the way into the run.");
            var screen = Object.FindAnyObjectByType<LoadingScreen>();
            Assert.IsTrue(screen.transform.Find("Invasion").gameObject.activeSelf, "The run is not loading behind its painting.");
            var tip = screen.transform.Find("TipPlaque/Tip").GetComponent<UnityEngine.UI.Text>().text;
            Assert.IsFalse(string.IsNullOrEmpty(tip) || tip.StartsWith("#"), $"No tip, or a raw key: \"{tip}\"");
            yield return new WaitForSecondsRealtime(1.3f);
            yield return PlaytestCapture.ShootWithUi("loading-invasion", Vector3.zero);
            yield return WaitForTheScreenToGo();
            Assert.IsTrue(InvasionMode.IsNewYear, "The run behind the screen is not the theme's map.");
        }
    }
}
