using System.Collections;
using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>The main menu's settings card: the fog's look can be chosen there, and the choice sticks.</summary>
    public class SettingsPanelTests
    {
        private FogStyle _playersStyle;

        [UnitySetUp]
        public IEnumerator OpenTheMenu()
        {
            LogAssert.ignoreFailingMessages = true;
            _playersStyle = GameSettings.FogStyle;
            GameSettings.FogStyle = FogStyle.Clouds;

            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        [TearDown]
        public void PutItBack()
        {
            GameSettings.FogStyle = _playersStyle;
            ModalGate.SetBlocked(false);
        }

        [UnityTest]
        public IEnumerator TheFogStyle_CanBeChosenInTheSettings()
        {
            var panel = Object.FindAnyObjectByType<SettingsPanelController>(FindObjectsInactive.Include);
            Assert.IsNotNull(panel, "The main menu has no settings panel.");
            panel.OpenPanel();
            yield return null;

            var buttons = FogButtons(panel);
            Assert.AreEqual(2, buttons.Count, "The settings card should offer exactly two fog styles.");
            Assert.IsTrue(buttons.ContainsKey("Fog_Clouds") && buttons.ContainsKey("Fog_Parchment"));
            foreach (var pair in buttons)
            {
                Assert.IsFalse(string.IsNullOrEmpty(pair.Value.GetComponentInChildren<Text>().text), $"{pair.Key} has no caption.");
            }

            buttons["Fog_Parchment"].onClick.Invoke();
            Assert.AreEqual(FogStyle.Parchment, GameSettings.FogStyle);

            yield return PlaytestCapture.ShootWithUi("settings-fog", Vector3.zero);

            buttons["Fog_Clouds"].onClick.Invoke();
            Assert.AreEqual(FogStyle.Clouds, GameSettings.FogStyle);
            panel.ClosePanel();
        }

        private static Dictionary<string, Button> FogButtons(SettingsPanelController panel)
        {
            var found = new Dictionary<string, Button>();
            foreach (var button in panel.GetComponentsInChildren<Button>(true))
            {
                if (button.name.StartsWith("Fog_")) found[button.name] = button;
            }
            return found;
        }
    }
}
