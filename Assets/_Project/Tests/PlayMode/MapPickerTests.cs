using System.Collections;
using System.Linq;
using CityBuilder.Core;
using CityBuilder.Maps;
using CityBuilder.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// The new-game map picker (2026-10-20, the user's rule: every new map is the player's to
    /// choose). "New Game" opens it with one row per designed map, the newest selected; "Play"
    /// starts on the row picked.
    /// </summary>
    public class MapPickerTests
    {
        [UnitySetUp]
        public IEnumerator OpenTheMenu()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        [TearDown]
        public void Unblock() => ModalGate.SetBlocked(false);

        [UnityTest]
        public IEnumerator NewGame_OpensThePicker_WithEveryMap_TheNewestSelected()
        {
            var picker = OpenPicker();
            yield return null;

            var rows = Rows(picker);
            var expected = MapSelector.Choosable().Select(m => "Map_" + m.MapId).ToArray();
            Assert.GreaterOrEqual(expected.Length, 3, "Fewer than three designed maps are imported.");
            CollectionAssert.AreEqual(expected, rows.Select(r => r.name).ToArray(), "The picker does not list every designed map, newest first.");
            foreach (var row in rows)
            {
                var text = row.GetComponentInChildren<Text>().text;
                StringAssert.DoesNotContain("#", text, $"{row.name} shows a raw localization key: \"{text}\"");
            }
            Assert.AreEqual(UiTheme.RowSelected, rows[0].GetComponent<Image>().color, "The newest map is not selected to begin with.");
            Assert.IsTrue(picker.transform.Find("Card/PlayButton").GetComponent<Button>().interactable, "Play is not available with a map selected.");

            yield return PlaytestCapture.ShootWithUi("map-picker", Vector3.zero);
            yield return PlaytestCapture.ShootWithUiAt("map-picker-phone", 2400, 1080, Vector3.zero, 40f, 50f, 25f);
        }

        [UnityTest]
        public IEnumerator PickingAMap_StartsTheGameOnIt()
        {
            var picker = OpenPicker();
            yield return null;

            var row = Rows(picker).First(r => r.name == "Map_Map3");
            row.GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(UiTheme.RowSelected, row.GetComponent<Image>().color, "Tapping a row does not select it.");

            picker.ConfirmPlay();
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady("Map3");
            Assert.AreEqual("Map3", MeshMapApplier.Instance.CurrentMapId, "Play did not start on the map picked.");
        }

        private static MapSelectPanelController OpenPicker()
        {
            var menu = Object.FindAnyObjectByType<MainMenuController>();
            Assert.IsNotNull(menu, "The main menu has no controller.");
            menu.StartNewGame();
            var picker = Object.FindAnyObjectByType<MapSelectPanelController>(FindObjectsInactive.Include);
            Assert.IsNotNull(picker, "The main menu has no map picker.");
            Assert.IsTrue(picker.gameObject.activeInHierarchy, "New Game did not open the map picker.");
            return picker;
        }

        private static Transform[] Rows(MapSelectPanelController picker)
        {
            var content = picker.GetComponentsInChildren<VerticalLayoutGroup>(true).First().transform;
            return Enumerable.Range(0, content.childCount).Select(content.GetChild).ToArray();
        }
    }
}
