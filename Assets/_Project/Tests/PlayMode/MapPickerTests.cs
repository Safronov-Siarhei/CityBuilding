using System.Collections;
using System.Linq;
using CityBuilder.Core;
using CityBuilder.Maps;
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
    /// The sandbox's screen in the main menu (the user's sketch, 2026-10-25; the rule since
    /// 2026-10-20: every designed map is the player's to choose): a card per map, the newest picked;
    /// the theme buttons dress the new game in their season; a theme no map comes in shows none.
    /// </summary>
    public class MapPickerTests
    {
        private MapTheme _playersTheme;

        [UnitySetUp]
        public IEnumerator OpenTheMenu()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            _playersTheme = GameSettings.SandboxTheme;
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        [TearDown]
        public void PutItBack()
        {
            GameSettings.SandboxTheme = _playersTheme;
            ModalGate.SetBlocked(false);
        }

        [UnityTest]
        public IEnumerator TheSandbox_ShowsACardPerMap_TheNewestPicked()
        {
            var screen = OpenSandbox();
            screen.GetComponentInChildren<ThemePicker>().Pick(MapTheme.Summer);
            yield return null;

            var cards = Cards(screen);
            var expected = MapSelector.Choosable().Select(m => "Map_" + m.MapId).ToArray();
            // the archipelago alone since 2026-10-29 (the user: the old maps are out)
            CollectionAssert.AreEqual(new[] { "Map_Archipelago" }, expected, "The sandbox's map is not the archipelago alone.");
            CollectionAssert.AreEqual(expected, cards.Where(c => c.gameObject.activeSelf).Select(c => c.name).ToArray(), "The sandbox does not show every designed map, newest first.");
            foreach (var card in cards)
            {
                var text = card.Find("Name").GetComponent<Text>().text;
                StringAssert.DoesNotContain("#", text, $"{card.name} shows a raw localization key: \"{text}\"");
                Assert.IsNotNull(card.Find("Picture").GetComponent<Image>().sprite, $"{card.name} has no picture.");
            }
            Assert.IsTrue(cards[0].Find("Picked").gameObject.activeSelf, "The newest map is not picked to begin with.");
            Assert.IsTrue(Start(screen).interactable, "«Начать» is not available with a map picked.");

            yield return new WaitForSecondsRealtime(0.9f);
            yield return PlaytestCapture.ShootWithUi("map-picker", Vector3.zero);
            yield return PlaytestCapture.ShootWithUiAt("map-picker-phone", 2400, 1080, Vector3.zero, 40f, 50f, 25f);
        }

        /// <summary>The New Year is the Invasion's own map: in the sandbox that theme has no map to show, and nothing to start.</summary>
        [UnityTest]
        public IEnumerator AThemeWithNoMaps_ShowsNone()
        {
            var screen = OpenSandbox();
            screen.GetComponentInChildren<ThemePicker>().Pick(MapTheme.NewYear);
            yield return null;
            Assert.IsFalse(Cards(screen).Any(c => c.gameObject.activeSelf), "A map is shown in a theme it does not come in.");
            Assert.IsFalse(Start(screen).interactable, "«Начать» is available with no map to start on.");
        }

        /// <summary>A map picked in the winter theme starts in winter, and its save keeps the season.</summary>
        [UnityTest]
        public IEnumerator PickingAMapInATheme_StartsTheGameOnItInThatSeason()
        {
            var screen = OpenSandbox();
            screen.GetComponentInChildren<ThemePicker>().Pick(MapTheme.Winter);
            var map = Cards(screen).First(r => r.name == "Map_Archipelago");
            map.GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(map.Find("Picked").gameObject.activeSelf, "Tapping a card does not pick it.");

            screen.StartGame();
            // a new game on the archipelago is a fresh seed of its own
            var id = GameSessionIntent.NewGameMapId;
            Assert.IsTrue(Maps.Archipelago.ArchipelagoMapId.TryParse(id, out _, out _), $"«Начать» started on \"{id}\", not a seeded archipelago.");
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(id);
            Assert.AreEqual(id, MeshMapApplier.Instance.CurrentMapId, "«Начать» did not start on the map picked.");
            Assert.AreEqual(MapSeason.Winter, SeasonStyle.Current, "The game is not in the theme's season.");
        }

        /// <summary>A friend's code typed in starts their very map (the user, 2026-10-28); a bad code starts nothing and says so.</summary>
        [UnityTest]
        public IEnumerator AMapCode_StartsThatVeryMap()
        {
            var screen = OpenSandbox();
            screen.GetComponentInChildren<ThemePicker>().Pick(MapTheme.Summer);
            var field = screen.transform.Find("Window/MapCode").GetComponent<InputField>();
            field.text = "nonsense";
            screen.StartGame();
            Assert.IsTrue(screen.transform.Find("Window/MapCodeError").gameObject.activeSelf, "A bad code started a game without a word.");

            var id = Maps.Archipelago.ArchipelagoMapId.For(4242);
            field.text = Maps.Archipelago.ArchipelagoMapId.Code(id);
            screen.StartGame();
            Assert.AreEqual(id, GameSessionIntent.NewGameMapId, "The typed code did not start its map.");
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(id);
            Assert.AreEqual(id, MeshMapApplier.Instance.CurrentMapId);
            // the loading screen lets the next test load its own scene only once it is gone
            for (var f = 0; f < 600 && LoadingScreen.Busy; f++) yield return null;
        }

        private static SandboxScreenController OpenSandbox()
        {
            var menu = Object.FindAnyObjectByType<MainMenuController>();
            Assert.IsNotNull(menu, "The main menu has no controller.");
            menu.StartNewGame();
            var screen = Object.FindAnyObjectByType<SandboxScreenController>(FindObjectsInactive.Include);
            Assert.IsNotNull(screen, "The main menu has no sandbox screen.");
            return screen;
        }

        private static Button Start(SandboxScreenController screen) => screen.transform.Find("Window/StartButton").GetComponent<Button>();

        private static Transform[] Cards(SandboxScreenController screen)
        {
            var grid = screen.transform.Find("Window/Maps");
            Assert.IsNotNull(grid, "The sandbox has no map cards.");
            return Enumerable.Range(0, grid.childCount).Select(grid.GetChild).ToArray();
        }
    }
}
