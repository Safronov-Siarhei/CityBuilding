using System.Collections;
using System.Collections.Generic;
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
            var placer = Object.FindAnyObjectByType<BuildingPlacer>();
            placer.MarkMandatoryBuildingAlreadyPlaced();
            // The game drops the Town Hall from the hand once it stands; placed by hand here, it
            // has to be put down by hand too, or the build menu stays hidden as if mid-placement.
            placer.ClearSelection();

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

        /// <summary>
        /// The screens the game has to look right on: a 20:9 phone (most of the test fleet), a
        /// 16:10 tablet (the Redmi Pad, 2000x1200), plain 16:9, and a 4:3 window on a PC. The HUD
        /// used to be laid out from the screen's centre for 16:9 only -- on the phone it stood in
        /// from both edges, on the tablet and the window its corners went off the screen.
        /// </summary>
        private static readonly Vector2Int[] ScreenShapes =
        {
            new Vector2Int(2400, 1080),
            new Vector2Int(2000, 1200),
            new Vector2Int(1920, 1080),
            new Vector2Int(1024, 768),
        };

        // The HUD's standing pieces, which must neither leave the screen nor sit on each other.
        private static readonly string[] HudPieces =
        {
            "MenuButton", "WorkforceButton", "SettlementStatus",
            "TaxRate", "EventFeed", "Happiness", "Army", "Migration", "Minimap",
            "BuildingMenu/Hotbar", "BuildingMenu/CategoryBar",
        };

        [UnityTest]
        public IEnumerator TheHudFitsEveryScreenShape()
        {
            if (!PlaytestCapture.Available)
            {
                Debug.Log("[Playtest] No graphics device -- the HUD cannot be laid out at another screen size. Re-run without -nographics.");
                yield break;
            }

            var failures = new List<string>();
            foreach (var shape in ScreenShapes)
            {
                var label = $"{shape.x}x{shape.y}";
                yield return PlaytestCapture.ShootWithUiAt($"hud-{label}", shape.x, shape.y, _townHall, 34f, 52f, 20f,
                    () =>
                    {
                        // Closed, as the player first sees it -- then open, the tallest the menu gets.
                        CheckLayout(label, failures);
                        var menu = Menu();
                        menu.Open(FirstCategory(), true);
                        Canvas.ForceUpdateCanvases();
                        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)menu.transform.Find("Hotbar"));
                        CheckLayout(label + " with the build menu open", failures);
                        menu.Close(true);
                    });
            }

            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        private static void CheckLayout(string screen, List<string> failures)
        {
            var safeArea = Object.FindAnyObjectByType<SafeAreaFitter>().GetComponent<RectTransform>();
            var screenRect = safeArea.rect;
            const float tolerance = 1f;

            // Every visible thing inside the screen.
            foreach (var graphic in safeArea.GetComponentsInChildren<Graphic>(false))
            {
                if (!graphic.enabled || graphic.color.a <= 0.01f) continue;
                if (graphic is Text text && string.IsNullOrEmpty(text.text)) continue;
                if (graphic.GetComponentInParent<Mask>() != null && graphic.GetComponent<Mask>() == null) continue;

                var r = RectIn(safeArea, graphic.rectTransform);
                if (r.xMin < screenRect.xMin - tolerance || r.xMax > screenRect.xMax + tolerance
                    || r.yMin < screenRect.yMin - tolerance || r.yMax > screenRect.yMax + tolerance)
                {
                    failures.Add($"{screen}: '{Path(graphic.transform, safeArea)}' leaves the screen ({r} outside {screenRect}).");
                }
            }

            // The standing pieces, and each resource chip, clear of one another.
            var pieces = new List<(string name, Rect rect)>();
            foreach (var name in HudPieces)
            {
                var piece = safeArea.Find(name);
                if (piece == null || !piece.gameObject.activeInHierarchy) continue;
                pieces.Add((name, RectIn(safeArea, (RectTransform)piece)));
            }
            var resourceRow = safeArea.Find("ResourceHUD");
            if (resourceRow != null)
            {
                foreach (Transform chip in resourceRow)
                {
                    if (chip.name.StartsWith("Chip_") && chip.gameObject.activeInHierarchy) pieces.Add((chip.name, RectIn(safeArea, (RectTransform)chip)));
                }
            }

            for (var i = 0; i < pieces.Count; i++)
            {
                for (var j = i + 1; j < pieces.Count; j++)
                {
                    var a = Shrink(pieces[i].rect, tolerance);
                    var b = Shrink(pieces[j].rect, tolerance);
                    if (a.Overlaps(b)) failures.Add($"{screen}: '{pieces[i].name}' overlaps '{pieces[j].name}' ({pieces[i].rect} / {pieces[j].rect}).");
                }
            }
        }

        private static Rect RectIn(RectTransform space, RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var corner in corners)
            {
                var local = (Vector2)space.InverseTransformPoint(corner);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static Rect Shrink(Rect rect, float by)
        {
            return Rect.MinMaxRect(rect.xMin + by, rect.yMin + by, rect.xMax - by, rect.yMax - by);
        }

        private static string Path(Transform t, Transform root)
        {
            var path = t.name;
            for (var p = t.parent; p != null && p != root; p = p.parent) path = p.name + "/" + path;
            return path;
        }

        // ---------------------------------------------------------------- the build menu

        private static BuildingCategoryPanel Menu() => Object.FindAnyObjectByType<BuildingCategoryPanel>(FindObjectsInactive.Include);

        private static CategoryButtonHandler[] Tabs() =>
            Menu().GetComponentsInChildren<CategoryButtonHandler>(true);

        private static CityBuilder.Buildings.BuildingCategory FirstCategory() => Tabs()[0].Category;

        private static void TapTab(CategoryButtonHandler tab) => tab.GetComponent<Button>().onClick.Invoke();

        private static IEnumerator LetItSlide()
        {
            for (var i = 0; i < 40; i++) yield return null;
        }

        [UnityTest]
        public IEnumerator TheBuildMenu_StartsClosed_WithOnlyTheTabsOnTheBottomEdge()
        {
            var menu = Menu();
            menu.Close(true);
            yield return null;

            Assert.IsFalse(menu.IsOpen);
            var hotbar = menu.transform.Find("Hotbar");
            Assert.IsFalse(hotbar.gameObject.activeSelf, "The cards show before any category was tapped -- the player sees what is in a category without opening it.");
            var bar = (RectTransform)menu.transform.Find("CategoryBar");
            Assert.Less(bar.anchoredPosition.y, 60f, "The closed tab row is not down on the bottom edge.");
        }

        [UnityTest]
        public IEnumerator ATab_OpensItsCardsUnderTheRaisedRow_AndTappingItAgainClosesIt()
        {
            var menu = Menu();
            menu.Close(true);
            var tab = Tabs()[0];

            TapTab(tab);
            yield return LetItSlide();

            Assert.IsTrue(menu.IsOpen, "Tapping a tab did not open its category.");
            var hotbar = (RectTransform)menu.transform.Find("Hotbar");
            Assert.IsTrue(hotbar.gameObject.activeSelf, "The category opened but its cards did not appear.");
            var shown = 0;
            foreach (var card in hotbar.GetComponentsInChildren<HotbarButtonHandler>(false))
            {
                Assert.AreEqual(tab.Category, card.Building.category, $"'{card.Building.buildingName}' shows under the wrong tab.");
                shown++;
            }
            Assert.Greater(shown, 0, "The open category shows no cards at all.");

            var bar = (RectTransform)menu.transform.Find("CategoryBar");
            var safeArea = Object.FindAnyObjectByType<SafeAreaFitter>().GetComponent<RectTransform>();
            Assert.GreaterOrEqual(RectIn(safeArea, bar).yMin, RectIn(safeArea, hotbar).yMax - 1f,
                "The tab row did not rise above the cards -- they sit on top of each other.");

            TapTab(tab);
            yield return LetItSlide();
            Assert.IsFalse(menu.IsOpen, "Tapping the open tab again did not close the menu.");
            Assert.IsFalse(hotbar.gameObject.activeSelf, "The menu closed but the cards stayed up.");
            Assert.Less(bar.anchoredPosition.y, 60f, "The tab row did not come back down to the bottom edge.");
        }

        [UnityTest]
        public IEnumerator AnotherTab_SwitchesTheOpenCategory()
        {
            var menu = Menu();
            menu.Close(true);
            var tabs = Tabs();
            Assert.Greater(tabs.Length, 1, "Only one category -- nothing to switch to.");

            TapTab(tabs[0]);
            TapTab(tabs[1]);
            yield return null;

            Assert.IsTrue(menu.IsOpen, "Tapping a second tab closed the menu instead of switching to it.");
            Assert.AreEqual(tabs[1].Category, menu.OpenCategory);
            menu.Close(true);
        }

        [UnityTest]
        public IEnumerator AMapTap_ClosesTheMenu_ButNotWhileABuildingIsBeingPlaced()
        {
            // The fixture places the Town Hall by hand, so the placer is still holding the Town Hall
            // the game would have dropped on placing it -- put it down first, as the game does.
            var placer = Object.FindAnyObjectByType<BuildingPlacer>();
            placer.ClearSelection();

            var menu = Menu();
            menu.Open(FirstCategory(), true);
            menu.HandleMapTap(new Vector2(500f, 500f));
            yield return null;
            Assert.IsFalse(menu.IsOpen, "A tap on the map left the build menu open.");

            menu.Open(FirstCategory(), true);
            var card = menu.transform.Find("Hotbar").GetComponentInChildren<HotbarButtonHandler>(false);
            Assert.IsNotNull(card, "The open category has no card to pick.");
            placer.SelectBuilding(card.Building);
            try
            {
                Assert.IsTrue(placer.IsSelecting, $"'{card.Building.buildingName}' could not be picked up -- the test proves nothing.");
                menu.HandleMapTap(new Vector2(500f, 500f));
                yield return null;
                Assert.IsTrue(menu.IsOpen, "A tap that placed or aimed a building closed the build menu under the player's thumb.");
            }
            finally
            {
                placer.ClearSelection();
                menu.Close(true);
            }
        }

        [UnityTest]
        public IEnumerator Photograph_TheOpenBuildMenu()
        {
            var menu = Menu();
            menu.Open(FirstCategory(), true);
            yield return null;
            yield return PlaytestCapture.ShootWithUi("hud-build-open", _townHall, 34f, 52f, 20f);
            menu.Close(true);
        }

        // ---------------------------------------------------------------- the layout sketch (2026-10-14)

        private static GameMenuController GameMenu() => Object.FindAnyObjectByType<GameMenuController>();
        private static HudHider Hud() => Object.FindAnyObjectByType<HudHider>();
        private static Transform HudRoot() => Object.FindAnyObjectByType<SafeAreaFitter>().transform;

        private static float Alpha(string piece)
        {
            var group = HudRoot().Find(piece).GetComponent<CanvasGroup>();
            return group != null ? group.alpha : 1f;
        }

        [UnityTest]
        public IEnumerator TheMenu_PausesTheGame_AndHidesTheHudBehindIt()
        {
            var menu = GameMenu();
            Assert.IsNotNull(menu, "No in-game menu.");
            Time.timeScale = 1f;

            menu.Open();
            yield return null;
            try
            {
                Assert.AreEqual(0f, Time.timeScale, "The game keeps running behind the open menu.");
                Assert.IsTrue(HudRoot().Find("GameMenu").gameObject.activeSelf, "The menu did not open.");
                foreach (var entry in new[] { "SaveEntry", "SettingsEntry", "MainMenuEntry" })
                {
                    Assert.IsNotNull(HudRoot().Find("GameMenu/" + entry), $"The menu has no {entry}.");
                }
                foreach (var piece in new[] { "ResourceHUD", "TaxRate", "Happiness", "WorkforceButton", "BuildingMenu", "Minimap" })
                {
                    Assert.AreEqual(0f, Alpha(piece), $"'{piece}' still shows behind the open menu.");
                }
            }
            finally
            {
                menu.Close();
            }
            yield return null;

            Assert.AreEqual(1f, Time.timeScale, "Closing the menu left the game paused.");
            Assert.AreEqual(1f, Alpha("ResourceHUD"), "Closing the menu did not bring the HUD back.");
            Assert.IsFalse(Core.ModalGate.IsBlocked, "Closing the menu left the world blocked.");
        }

        [UnityTest]
        public IEnumerator SavingFromTheMenu_ComesBackToTheMenu_StillPaused()
        {
            var menu = GameMenu();
            menu.Open();
            menu.OpenSave();
            yield return null;
            try
            {
                var dialog = HudRoot().Find("SaveDialog").gameObject;
                Assert.IsTrue(dialog.activeSelf, "The save entry did not open the save dialog.");
                Assert.IsFalse(HudRoot().Find("GameMenu").gameObject.activeSelf, "The menu stays drawn over the save dialog.");

                Object.FindAnyObjectByType<CityBuilder.Saving.SaveDialogController>(FindObjectsInactive.Include).CloseDialog();
                yield return null;
                yield return null;
                Assert.IsTrue(HudRoot().Find("GameMenu").gameObject.activeSelf, "Closing the save dialog dropped the player out of the menu.");
                Assert.AreEqual(0f, Time.timeScale, "The game started running again behind the menu.");
                Assert.AreEqual(0f, Alpha("ResourceHUD"), "The HUD came back while the menu is still open.");
            }
            finally
            {
                menu.Close();
            }
        }

        [UnityTest]
        public IEnumerator TheCitizensPanel_HidesTheHudBehindIt()
        {
            var panel = Object.FindAnyObjectByType<WorkforcePanelController>(FindObjectsInactive.Include);
            panel.OpenPanel();
            yield return null;
            try
            {
                Assert.AreEqual(0f, Alpha("ResourceHUD"), "The HUD still shows behind the citizens panel.");
                Assert.AreEqual(1f, Alpha("WorkforcePanel"), "The citizens panel hid itself with the rest.");
            }
            finally
            {
                panel.ClosePanel();
            }
            yield return null;
            Assert.AreEqual(1f, Alpha("ResourceHUD"), "Closing the citizens panel did not bring the HUD back.");
        }

        [UnityTest]
        public IEnumerator Happiness_IsANumber_AndATapShowsItsBreakdown()
        {
            var label = HudRoot().Find("Happiness/HappinessLabel").GetComponent<Text>();
            StringAssert.IsMatch(@"^\d+%$", label.text, "The happiness button should carry the number alone.");

            var breakdown = HudRoot().Find("HappinessBreakdown").gameObject;
            Assert.IsFalse(breakdown.activeSelf, "The breakdown shows before anyone asked for it.");
            HudRoot().Find("Happiness").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.IsTrue(breakdown.activeSelf, "Tapping the happiness number did not show its breakdown.");
            HudRoot().Find("Happiness").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.IsFalse(breakdown.activeSelf, "A second tap did not hide the breakdown.");
        }

        [Test]
        public void TheDayLine_NamesNoSettlementTier()
        {
            var day = HudRoot().Find("SettlementStatus").GetComponent<Text>().text;
            Assert.IsFalse(string.IsNullOrEmpty(day), "The day line is empty.");
            foreach (var tier in new[] { "hamlet", "village", "town", "city", "kingdom" })
            {
                StringAssert.DoesNotContain(Core.Localization.Get("#tier_" + tier), day, "The day line still names the settlement's tier.");
            }
            Assert.IsNull(HudRoot().Find("SettlementTierToast"), "The tier-up toast is still in the HUD.");
        }

        [Test]
        public void TheTax_SaysWhatItIs()
        {
            // The one HUD control that keeps a word (the user, 2026-10-14): "Налог: 10%", not a coin and a number.
            var rate = HudRoot().Find("TaxRate/RatePlaque/RateLabel").GetComponent<Text>().text;
            var rateNow = TaxManager.Instance != null ? TaxManager.Instance.TaxRatePercent : 10;
            Assert.AreEqual(Core.Localization.Format("#tax_rate", rateNow), rate, "The tax plaque should name the tax in words.");
            Assert.IsNull(HudRoot().Find("TaxRate/RatePlaque/Coin"), "The coin picture is still on the tax plaque.");
        }

        [Test]
        public void TheCitizensButton_StandsUnderTheMenuOnTheLeft()
        {
            var menu = (RectTransform)HudRoot().Find("MenuButton");
            var citizens = (RectTransform)HudRoot().Find("WorkforceButton");
            var menuCorners = new Vector3[4];
            var citizenCorners = new Vector3[4];
            menu.GetWorldCorners(menuCorners);
            citizens.GetWorldCorners(citizenCorners);
            Assert.Less(citizenCorners[1].y, menuCorners[0].y + 0.01f, "The citizens button is not below the menu button.");
            var menuMid = (menuCorners[0].x + menuCorners[2].x) * 0.5f;
            var citizenMid = (citizenCorners[0].x + citizenCorners[2].x) * 0.5f;
            Assert.AreEqual(menuMid, citizenMid, Mathf.Abs(menuCorners[2].x - menuCorners[0].x) * 0.05f, "The citizens button is not in the menu's column.");
        }

        [UnityTest]
        public IEnumerator PlacingABuilding_SwapsTheBuildMenuForCancelRotateConfirm()
        {
            var placer = Object.FindAnyObjectByType<BuildingPlacer>();
            placer.ClearSelection();
            var menu = Menu();
            menu.Open(FirstCategory(), true);
            var card = menu.transform.Find("Hotbar").GetComponentInChildren<HotbarButtonHandler>(false);
            Assert.IsNotNull(card, "The open category has no card to pick.");

            placer.SelectBuilding(card.Building);
            yield return null;
            yield return null;
            try
            {
                Assert.IsTrue(placer.IsSelecting, "The building could not be picked up -- the test proves nothing.");
                Assert.IsFalse(menu.gameObject.activeSelf, "The build menu stays up while a building is being placed.");
                Assert.IsTrue(HudRoot().Find("PlacementControls/CancelPlacementButton").gameObject.activeSelf, "No cancel button while placing.");
                Assert.IsTrue(HudRoot().Find("PlacementControls/RotateButton").gameObject.activeSelf, "No rotate button while placing.");
            }
            finally
            {
                placer.ClearSelection();
            }
            yield return null;
            yield return null;
            Assert.IsTrue(menu.gameObject.activeSelf, "The build menu did not come back once the building was put down.");
            Assert.IsFalse(HudRoot().Find("PlacementControls/CancelPlacementButton").gameObject.activeSelf, "The cancel button stayed after placing ended.");
            menu.Close(true);
        }

        [UnityTest]
        public IEnumerator ACategoryTooLongForTheScreen_Scrolls_WithAScrollbarUnderTheCards()
        {
            Research.ResearchManager.Instance?.CompleteEverything();
            yield return null;
            var menu = Menu();
            var scrolled = 0;
            foreach (var tab in Tabs())
            {
                menu.Open(tab.Category, true);
                yield return null;
                var row = (RectTransform)menu.transform.Find("Hotbar");
                var scrollbar = menu.transform.Find("Hotbar/Scrollbar").gameObject;
                Assert.LessOrEqual(row.rect.width, 1040.5f, $"The {tab.Category} cards run wider than the room between the minimap and the tax.");
                Assert.AreEqual(menu.CardsScroll, scrollbar.activeSelf,
                    $"{tab.Category}: the scrollbar {(scrollbar.activeSelf ? "shows with nothing to scroll" : "is missing while the cards overflow")}.");
                if (menu.CardsScroll) scrolled++;
            }
            menu.Close(true);
            Assert.Greater(scrolled, 0, "No category overflows with everything researched -- the scroll was never exercised.");
        }

        [UnityTest]
        public IEnumerator Photograph_TheGameMenu()
        {
            var menu = GameMenu();
            menu.Open();
            yield return null;
            yield return PlaytestCapture.ShootWithUi("hud-game-menu", _townHall, 34f, 52f, 20f);
            menu.Close();
        }

        [UnityTest]
        public IEnumerator Photograph_PlacingABuilding()
        {
            var placer = Object.FindAnyObjectByType<BuildingPlacer>();
            placer.ClearSelection();
            var menu = Menu();
            menu.Open(FirstCategory(), true);
            var card = menu.transform.Find("Hotbar").GetComponentInChildren<HotbarButtonHandler>(false);
            placer.SelectBuilding(card.Building);
            yield return null;
            yield return null;
            yield return PlaytestCapture.ShootWithUi("hud-placing", _townHall, 34f, 52f, 20f);
            placer.ClearSelection();
            menu.Close(true);
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

        /// <summary>
        /// What a label is drawn on: an image above it in the hierarchy, or -- the case the resource
        /// chips are built as -- an earlier sibling it sits inside, drawn just before it. Checking
        /// only the parents missed the chips, which is how white numbers on parchment shipped.
        /// </summary>
        private static Image WrittenOn(Text text)
        {
            var parent = text.transform.parent;
            if (parent != null)
            {
                var centre = text.rectTransform.TransformPoint(text.rectTransform.rect.center);
                for (var i = text.transform.GetSiblingIndex() - 1; i >= 0; i--)
                {
                    var sibling = parent.GetChild(i).GetComponent<Image>();
                    if (sibling == null || sibling.sprite == null || !sibling.gameObject.activeSelf) continue;
                    var local = sibling.rectTransform.InverseTransformPoint(centre);
                    if (sibling.rectTransform.rect.Contains(local)) return sibling;
                }
            }

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
