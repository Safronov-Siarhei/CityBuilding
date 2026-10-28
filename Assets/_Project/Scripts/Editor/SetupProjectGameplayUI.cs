using System.Collections.Generic;
using System.IO;
using CityBuilder.Buildings;
using CityBuilder.CameraControl;
using CityBuilder.Cheats;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using CityBuilder.Resources;
using CityBuilder.Saving;
using CityBuilder.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// The in-game canvas: BuildGameplayUI and the HUD pieces it builds directly -- resource bar,
    /// minimap, migration, selection badge, aim marker, game over, save dialog, scroll lists.
    /// </summary>
    public static partial class SetupProject
    {
        private static void BuildGameplayUI(BuildingPlacer placer, GameSaveController saveController, Camera targetCamera, RTSCameraController cameraRig, SettlementTierManager settlementTierManager, GameCalendar gameCalendar, List<BuildingData> hotbarBuildings, RenderTexture minimapTexture, BuildingData mandatoryBuilding)
        {
            // Created here (no NewScene() call happens between this and its uses below/this
            // scene being saved) rather than reused from BuildMainMenuScene's sprite, since that
            // one goes stale the moment this method's caller (BuildCityScene) switched scenes.
            var panelSprite = CreatePanelSprite();

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            ConfigureScaler(canvasGO.GetComponent<CanvasScaler>());

            // Everything in the HUD hangs off this rather than off the Canvas directly: it is the
            // rect that shrinks to Screen.safeArea, keeping the HUD clear of the camera cutout and
            // the system gesture bar. The Canvas itself cannot do the job -- Unity drives its own
            // RectTransform, so anchors written onto it are overwritten every frame.
            var safeAreaGO = new GameObject("SafeArea", typeof(RectTransform));
            safeAreaGO.transform.SetParent(canvasGO.transform, false);
            StretchFull(safeAreaGO.GetComponent<RectTransform>());
            safeAreaGO.AddComponent<SafeAreaFitter>();
            var uiRoot = safeAreaGO.transform;
            BeginHudSkins();
            BuildDockRails(uiRoot);


            // Hint shown only while the player must place the mandatory Town Hall: at the bottom in
            // the middle, with the rotate button lifted just above it (the user, 2026-10-20).
            var hintRoot = CreateImage(uiRoot, "PlacementHint", new Color(0f, 0f, 0f, 0.6f));
            hintRoot.sprite = panelSprite;
            hintRoot.type = Image.Type.Sliced;
            var hintRect = hintRoot.GetComponent<RectTransform>();
            hintRect.anchorMin = hintRect.anchorMax = new Vector2(0.5f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = new Vector2(0f, HudMargin);
            hintRect.sizeDelta = new Vector2(780f, 84f);
            var footprint = mandatoryBuilding != null ? mandatoryBuilding.footprintSize : new Vector2Int(4, 4);
            CreateText(hintRoot.transform, "Text", Localization.Format("#hud_place_hint", footprint.x, footprint.y), 30, Vector2.zero, new Vector2(740f, 74f));

            // Touch-friendly building hotbar, grouped into categories. Closed at rest: only the row
            // of category tabs, on the bottom edge; a tab slides it up and opens that category's
            // cards beneath it -- see BuildingCategoryPanel.
            // Shown only once the Town Hall is placed (there's nothing else buildable before that
            // point). Number-key hotkeys still work on PC, against the same availableBuildings
            // list regardless of which category is currently shown.
            var menuGO = new GameObject("BuildingMenu", typeof(RectTransform));
            menuGO.transform.SetParent(uiRoot, false);
            StretchFull(menuGO.GetComponent<RectTransform>());
            var categoryPanel = menuGO.AddComponent<BuildingCategoryPanel>();

            // Cards, not bare icons: the rendered icon with the building's name and price under it
            // (see CreateHotbarCard), on a parchment panel with a wooden lip -- the skin's hotbar.
            var cardSize = new Vector2(124f, 164f);
            const float spacing = 10f;
            const float buttonSize = 164f + 36f; // the card plus the panel's padding around it

            // The cards row: a panel the width of its cards (BuildingCategoryPanel sizes it, up to
            // maxCardsWidth), holding a scroll view -- a category with more buildings than fit between
            // the minimap and the tax control scrolls sideways, with a scrollbar under the cards. The
            // layout group inside excludes inactive cards and re-packs whichever category is shown.
            const float rowHeight = buttonSize + 14f; // + the scrollbar's strip under the cards
            var hotbarGO = new GameObject("Hotbar", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            hotbarGO.transform.SetParent(menuGO.transform, false);
            var hotbarRect = hotbarGO.GetComponent<RectTransform>();
            hotbarRect.anchorMin = new Vector2(0.5f, 0f);
            hotbarRect.anchorMax = new Vector2(0.5f, 0f);
            hotbarRect.pivot = new Vector2(0.5f, 0f);
            hotbarRect.anchoredPosition = new Vector2(0f, 20f);
            hotbarRect.sizeDelta = new Vector2(600f, rowHeight);
            var hotbarImage = hotbarGO.GetComponent<Image>();
            SkinRole(hotbarImage, "panel");

            var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportGO.transform.SetParent(hotbarGO.transform, false);
            var viewportRect = (RectTransform)viewportGO.transform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(14f, 30f);
            viewportRect.offsetMax = new Vector2(-14f, -20f);

            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRect = (RectTransform)contentGO.transform;
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(0f, 1f);
            contentRect.pivot = new Vector2(0f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);
            var hotbarLayout = contentGO.GetComponent<HorizontalLayoutGroup>();
            hotbarLayout.spacing = spacing;
            hotbarLayout.childAlignment = TextAnchor.MiddleLeft;
            hotbarLayout.childControlWidth = false;
            hotbarLayout.childControlHeight = false;
            hotbarLayout.childForceExpandWidth = false;
            hotbarLayout.childForceExpandHeight = false;
            var hotbarFitter = contentGO.GetComponent<ContentSizeFitter>();
            hotbarFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            hotbarFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var scrollbarGO = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            scrollbarGO.transform.SetParent(hotbarGO.transform, false);
            var scrollbarRect = (RectTransform)scrollbarGO.transform;
            scrollbarRect.anchorMin = new Vector2(0f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 0f);
            scrollbarRect.pivot = new Vector2(0.5f, 0f);
            scrollbarRect.offsetMin = new Vector2(30f, 12f);
            scrollbarRect.offsetMax = new Vector2(-30f, 24f);
            var scrollbarTrack = scrollbarGO.GetComponent<Image>();
            SkinRole(scrollbarTrack, "scroll_track");
            var handleArea = new GameObject("SlidingArea", typeof(RectTransform));
            handleArea.transform.SetParent(scrollbarGO.transform, false);
            StretchFull((RectTransform)handleArea.transform);
            var handle = CreateImage(handleArea.transform, "Handle", Color.white);
            SkinRole(handle, "scroll_handle");
            StretchFull(handle.rectTransform);
            var scrollbar = scrollbarGO.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.LeftToRight;
            scrollbar.handleRect = handle.rectTransform;
            scrollbar.targetGraphic = handle;

            var scroll = hotbarGO.GetComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.horizontalScrollbar = scrollbar;
            scroll.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            for (var i = 0; i < hotbarBuildings.Count; i++)
            {
                var data = hotbarBuildings[i];
                var button = CreateHotbarCard(contentGO.transform, data, cardSize);

                var handler = button.gameObject.AddComponent<HotbarButtonHandler>();
                var handlerSO = new SerializedObject(handler);
                handlerSO.FindProperty("buildingPlacer").objectReferenceValue = placer;
                handlerSO.FindProperty("building").objectReferenceValue = data;
                handlerSO.ApplyModifiedPropertiesWithoutUndo();

                UnityEventTools.AddPersistentListener(button.onClick, handler.SelectThisBuilding);
            }

            // Fixed display order; only categories that actually have a hotbar building get a tab.
            var categoryOrder = new[] { BuildingCategory.City, BuildingCategory.Storage, BuildingCategory.Entertainment, BuildingCategory.Defence, BuildingCategory.Production, BuildingCategory.Food, BuildingCategory.Water };
            var presentCategories = new List<BuildingCategory>();
            foreach (var cat in categoryOrder)
            {
                if (hotbarBuildings.Exists(b => b.category == cat)) presentCategories.Add(cat);
            }

            // 110, not 80: at a 1080-high reference 80 px lands at roughly 5 mm on a dense
            // phone screen, half of what a finger reliably hits.
            const float categoryButtonSize = 96f;
            var categoryTotalWidth = presentCategories.Count * categoryButtonSize + Mathf.Max(0, presentCategories.Count - 1) * spacing;

            var categoryBarGO = new GameObject("CategoryBar", typeof(RectTransform));
            categoryBarGO.transform.SetParent(menuGO.transform, false);
            var categoryBarRect = categoryBarGO.GetComponent<RectTransform>();
            categoryBarRect.anchorMin = new Vector2(0.5f, 0f);
            categoryBarRect.anchorMax = new Vector2(0.5f, 0f);
            categoryBarRect.pivot = new Vector2(0.5f, 0f);
            var openBarY = hotbarRect.anchoredPosition.y + rowHeight + 12f;
            categoryBarRect.anchoredPosition = new Vector2(0f, hotbarRect.anchoredPosition.y);
            categoryBarRect.sizeDelta = new Vector2(categoryTotalWidth, categoryButtonSize);
            hotbarGO.SetActive(false);

            // Behind the tabs, moving with them: a tray in the looks that draw one (Soft, Glass).
            var tray = HudImage(categoryBarGO.transform, "Tray", "tray", Vector2.zero);
            tray.raycastTarget = false;
            tray.rectTransform.anchorMin = Vector2.zero;
            tray.rectTransform.anchorMax = Vector2.one;
            tray.rectTransform.offsetMin = new Vector2(-12f, -10f);
            tray.rectTransform.offsetMax = new Vector2(12f, 10f);

            for (var i = 0; i < presentCategories.Count; i++)
            {
                var category = presentCategories[i];
                var icon = CreateCategoryIcon(category);
                var x = -categoryTotalWidth * 0.5f + categoryButtonSize * 0.5f + i * (categoryButtonSize + spacing);
                var categoryButton = CreateIconButton(categoryBarGO.transform, panelSprite, icon, $"Category_{category}", new Vector2(x, 0f), new Vector2(categoryButtonSize, categoryButtonSize));
                // The tab's sprite is the look's (BuildingCategoryPanel.SetLook paints them).
                var tabImage = categoryButton.GetComponent<Image>();
                tabImage.sprite = HudSprite("tab");
                tabImage.type = Image.Type.Simple;
                tabImage.color = Color.white;

                var categoryHandler = categoryButton.gameObject.AddComponent<CategoryButtonHandler>();
                var categoryHandlerSO = new SerializedObject(categoryHandler);
                categoryHandlerSO.FindProperty("panel").objectReferenceValue = categoryPanel;
                categoryHandlerSO.FindProperty("category").enumValueIndex = (int)category;
                categoryHandlerSO.ApplyModifiedPropertiesWithoutUndo();

                UnityEventTools.AddPersistentListener(categoryButton.onClick, categoryHandler.SelectThisCategory);
            }

            // The open tab and the rest, in the default look (HudSkinApplier swaps them per look).
            var categorySO = new SerializedObject(categoryPanel);
            categorySO.FindProperty("selectedSprite").objectReferenceValue = HudSprite("tab_selected");
            categorySO.FindProperty("unselectedSprite").objectReferenceValue = HudSprite("tab");
            categorySO.FindProperty("categoryBar").objectReferenceValue = categoryBarRect;
            categorySO.FindProperty("hotbar").objectReferenceValue = hotbarGO;
            categorySO.FindProperty("buildingPlacer").objectReferenceValue = placer;
            categorySO.FindProperty("closedBarY").floatValue = hotbarRect.anchoredPosition.y;
            categorySO.FindProperty("openBarY").floatValue = openBarY;
            categorySO.FindProperty("cardsScroll").objectReferenceValue = scroll;
            categorySO.FindProperty("cardsContent").objectReferenceValue = contentRect;
            categorySO.ApplyModifiedPropertiesWithoutUndo();

            var visibility = canvasGO.AddComponent<BuildingPlacerUIVisibility>();
            var visibilitySO = new SerializedObject(visibility);
            visibilitySO.FindProperty("buildingPlacer").objectReferenceValue = placer;
            visibilitySO.FindProperty("showWhilePlacingMandatory").objectReferenceValue = hintRoot.gameObject;
            visibilitySO.FindProperty("hideWhilePlacingMandatory").objectReferenceValue = menuGO;
            visibilitySO.ApplyModifiedPropertiesWithoutUndo();

            // The placement controls: cancel, rotate, confirm in a row at the bottom centre, where the
            // build menu stands -- it steps aside while a building is in hand (see
            // BuildingPlacerUIVisibility and PlacementHudController for which are visible when).
            var (cancelButton, rotateButton, confirmButton, placementRow) = BuildPlacementControls(uiRoot);
            visibilitySO.Update();
            visibilitySO.FindProperty("placementControls").objectReferenceValue = placementRow;
            visibilitySO.FindProperty("liftWhilePlacingMandatory").floatValue = 84f + 12f;
            visibilitySO.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(rotateButton.onClick, placer.RotateSelection);
            // Cancel. On a phone there was NO way to put a building back down: ClearSelection was
            // reachable only through Escape and the right mouse button.
            UnityEventTools.AddPersistentListener(cancelButton.onClick, placer.ClearSelection);

            // The crosshair marking where the ghost is aimed. Positioned in SCREEN pixels by the
            // controller, which is why it is anchored to a corner rather than centred here.
            var aimMarker = CreateAimMarker(uiRoot);

            // One line saying why confirm is dark, or what a drawn line will cost. Bottom-centre
            // above the hotbar, where the eye already is while placing.
            var placementStatus = CreateText(uiRoot, "PlacementStatus", string.Empty, 30,
                Vector2.zero, new Vector2(900f, 44f), new Color(1f, 0.72f, 0.62f), addShadow: true);
            var statusRect = placementStatus.GetComponent<RectTransform>();
            statusRect.anchorMin = statusRect.anchorMax = statusRect.pivot = new Vector2(0.5f, 0f);
            statusRect.anchoredPosition = new Vector2(0f, 190f);
            placementStatus.gameObject.SetActive(false);

            // Lives on the Canvas itself (an always-active object), not on the buttons it
            // controls -- a script polling its own GameObject's active state would stop
            // receiving Update() calls the moment it deactivated it, and never reactivate it.
            var placementHud = canvasGO.AddComponent<PlacementHudController>();
            var placementHudSO = new SerializedObject(placementHud);
            placementHudSO.FindProperty("buildingPlacer").objectReferenceValue = placer;
            placementHudSO.FindProperty("confirmButton").objectReferenceValue = confirmButton.gameObject;
            placementHudSO.FindProperty("confirmImage").objectReferenceValue = confirmButton.GetComponent<Image>();
            placementHudSO.FindProperty("rotateButton").objectReferenceValue = rotateButton.gameObject;
            placementHudSO.FindProperty("cancelButton").objectReferenceValue = cancelButton.gameObject;
            placementHudSO.FindProperty("aimMarker").objectReferenceValue = aimMarker;
            placementHudSO.FindProperty("statusLabel").objectReferenceValue = placementStatus;
            placementHudSO.ApplyModifiedPropertiesWithoutUndo();
            // Confirm places the building -- or, while drawing a road, finishes drawing.
            UnityEventTools.AddPersistentListener(confirmButton.onClick, placementHud.ConfirmPressed);

            var hud = safeAreaGO.AddComponent<HudHider>();
            var (saveDialog, saveDialogRoot) = BuildSaveUI(uiRoot, panelSprite, saveController);
            BuildGameMenu(uiRoot, panelSprite, hud, saveController, saveDialog, saveDialogRoot);
            BuildGameOverUI(uiRoot, panelSprite);
            BuildResourceHUD(uiRoot);
            BuildMinimap(uiRoot, panelSprite, minimapTexture, cameraRig);

            var iconLibrary = CreateResourceIconLibrary();
            BuildHarvestBarSkin(iconLibrary);

            var infoPanel = BuildBuildingInfoPanel(uiRoot, panelSprite, iconLibrary);
            var researchPanel = BuildResearchPanel(uiRoot, panelSprite, iconLibrary);
            var buildingSelector = BuildBuildingSelector(uiRoot, targetCamera, infoPanel, researchPanel);
            var citizenSelector = BuildCitizenSelector(uiRoot, targetCamera);
            BuildBuildingFocus(uiRoot, buildingSelector, infoPanel, researchPanel, citizenSelector, placer, iconLibrary);
            BuildEventFeed(uiRoot, cameraRig);
            BuildTaxControl(uiRoot);
            BuildDayLabel(uiRoot, gameCalendar);
            BuildHappiness(uiRoot);
            BuildKingPortrait(uiRoot);
            BuildHeroSkillBar(uiRoot);
            BuildMigrationPanel(uiRoot, panelSprite);
            BuildWorkforcePanel(uiRoot, panelSprite, hud);
            BuildInventoryPanel(uiRoot, panelSprite, hud, iconLibrary);
            BuildArmyPanel(uiRoot, panelSprite);
            var armyOrderInput = BuildArmyOrderInput(uiRoot, targetCamera);

            // One tap, one owner, decided in one place -- see WorldInputDispatcher for why the
            // four systems no longer check each other's state.
            var dispatcherGO = new GameObject("WorldInputDispatcher");
            dispatcherGO.transform.SetParent(uiRoot, false);
            var dispatcher = dispatcherGO.AddComponent<CityBuilder.InputControl.WorldInputDispatcher>();
            var dispatcherSO = new SerializedObject(dispatcher);
            dispatcherSO.FindProperty("buildingPlacer").objectReferenceValue = placer;
            dispatcherSO.FindProperty("armyOrderInput").objectReferenceValue = armyOrderInput;
            dispatcherSO.FindProperty("citizenSelector").objectReferenceValue = citizenSelector;
            dispatcherSO.FindProperty("buildingSelector").objectReferenceValue = buildingSelector;
            dispatcherSO.ApplyModifiedPropertiesWithoutUndo();

            BuildSelectionBadge(uiRoot, panelSprite, citizenSelector);
            // «+1» over a sawmill or quarry as its worker brings a load in
            var popups = uiRoot.gameObject.AddComponent<ResourceGainPopups>();
            var popupsSO = new SerializedObject(popups);
            popupsSO.FindProperty("icons").objectReferenceValue = iconLibrary;
            popupsSO.ApplyModifiedPropertiesWithoutUndo();
            // a hammer and the time left over each building under construction (an Invasion run)
            var construction = uiRoot.gameObject.AddComponent<ConstructionLabels>();
            var constructionSO = new SerializedObject(construction);
            constructionSO.FindProperty("hammer").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Glyphs/Hammer.png");
            constructionSO.FindProperty("plaque").objectReferenceValue = HudSprite("plaque");
            constructionSO.ApplyModifiedPropertiesWithoutUndo();

            PinHudToEdges(uiRoot);
            ApplyTheme(canvasGO.transform);
            BuildHudSkinApplier(uiRoot, categoryPanel, hotbarRect.anchoredPosition.y, openBarY);
        }

        /// <summary>
        /// The migration line, in the bottom-right corner where the player's thumb already is.
        ///
        /// Its own panel rather than another line inside the Happiness one, because it is a live
        /// countdown and the happiness panel is a thing you glance at -- and because the resource
        /// bar along the top is laid out in fixed-width slots (see ResourceHUDController.PlaceChip)
        /// where squeezing in a seventh chip would re-proportion the whole row.
        ///
        /// y is 200, which is what it takes to clear everything already stacked in that corner:
        /// the rotate button (40 to 130) and, more to the point, the hotbar (28 to 158), which at
        /// a full category of twelve buildings is 1736 wide and reaches well past x 520. The
        /// category bar above it is only 656 wide and never gets out this far. The Army panel is
        /// the ceiling, at 370 up from the bottom.
        ///
        /// Anchored to the corner itself rather than positioned from the centre, so it stays put
        /// on a phone's aspect ratio instead of drifting off the edge the way the Happiness panel
        /// had to be pulled in from 810 to 760 to avoid.
        /// </summary>
        private static void BuildMigrationPanel(Transform canvasParent, Sprite panelSprite)
        {
            var frame = HudImage(canvasParent, "Migration", "plaque", new Vector2(360f, 60f));
            var frameRect = frame.GetComponent<RectTransform>();
            // Under the happiness number in the top-right corner (the user's layout, 2026-10-14).
            frameRect.anchorMin = frameRect.anchorMax = frameRect.pivot = new Vector2(1f, 1f);
            frameRect.anchoredPosition = new Vector2(-HudMargin, -HudMargin * 0.7f - RoundButton - 14f);
            frameRect.sizeDelta = new Vector2(360f, 60f);

            var label = CreateText(frame.transform, "MigrationLabel", string.Empty, 24, Vector2.zero, new Vector2(336f, 52f), UiTheme.Ink);
            label.font = UiTheme.Bold;
            // "Поселение обживается: 6:12" is wider than the plaque at 24: shrink to fit rather
            // than lose the clock off the end (the user's screenshot, 2026-10-20).
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 14;
            label.resizeTextMaxSize = 24;

            var go = new GameObject("MigrationController");
            go.transform.SetParent(canvasParent, false);
            var controller = go.AddComponent<MigrationController>();
            var so = new SerializedObject(controller);
            so.FindProperty("panel").objectReferenceValue = frame.gameObject;
            so.FindProperty("label").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildResourceHUD(Transform canvasParent)
        {
            // Sits just below the top row (hint banner + Меню/Сохранить corners, all in the
            // y 420-510 band) in the same center-anchored coordinate space as those, so it
            // never overlaps them regardless of which of those happen to be visible. Laid out as
            // icon+number chips (see CreateResourceIcon) instead of one plain-text label so the
            // always-on bar reads at a glance rather than as a wall of Russian resource names.
            var root = new GameObject("ResourceHUD", typeof(RectTransform));
            root.transform.SetParent(canvasParent, false);
            var rect = root.GetComponent<RectTransform>();
            // The second row from the top, under Меню/Сохранить/the status, across the whole
            // width of the screen with a 20-unit margin each side: each resource a parchment pill,
            // packed from the left by ResourceHUDController, which reads the row's real width --
            // 1880 on 16:9, ~2360 on a 20:9 phone.
            // One bar along the top between the menu button and the happiness number (the user's
            // layout, 2026-10-14): the row spans the gap between the two corners, and the chips sit
            // centred on a wooden plaque sized to them (ResourceHUDController).
            const float barWidth = 1580f;
            const float chipHeight = 60f;
            const float margin = 170f;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -64f);
            rect.sizeDelta = new Vector2(-2f * margin, chipHeight);
            // In the looks: a capsule behind each resource (chip), one behind them all (bar), or both.
            var chipSprite = HudSprite("chip") ?? HudSprite("bar");
            var barPlaque = HudImage(root.transform, "ResourceBar", "bar", new Vector2(400f, chipHeight + 18f));

            // Reading order: raw materials, then the ore and the metal it becomes side by side,
            // then money, then Food -- the one food counter (the user, 2026-10-25); the food
            // chain's middle links (wheat, flour, pork) are in the inventory window only. A chip is
            // only on screen once its resource has existed -- ResourceHUDController lays the
            // visible ones out, which is why nothing here is positioned.
            var resourceTypes = new[]
            {
                ResourceType.Wood, ResourceType.Stone, ResourceType.Coal,
                ResourceType.Iron, ResourceType.IronBar,
                ResourceType.CopperOre, ResourceType.CopperBar,
                ResourceType.Gold, ResourceType.GoldBar,
                ResourceType.Coins,
                ResourceType.Food,
            };
            var icons = new RectTransform[resourceTypes.Length];
            var amountTexts = new Text[resourceTypes.Length];
            var backgrounds = new RectTransform[resourceTypes.Length];

            RectTransform Chip(string name)
            {
                if (chipSprite == null) return null;
                // Created before its icon and number, so it draws behind them.
                return HudImage(root.transform, $"Chip_{name}", "chip", new Vector2(190f, chipHeight)).rectTransform;
            }

            Text Amount(string name)
            {
                var amountText = CreateText(root.transform, $"Amount_{name}", "0", 24, Vector2.zero, new Vector2(160f, chipHeight - 8f));
                amountText.alignment = TextAnchor.MiddleLeft;
                amountText.font = UiTheme.Heavy;
                amountText.resizeTextForBestFit = true;
                amountText.resizeTextMinSize = 15;
                amountText.resizeTextMaxSize = 24;
                // Set here, not by ApplyTheme: the number is the chip's SIBLING, drawn over it,
                // so the theme pass sees nothing under it and left it white -- on parchment.
                SkinInk(amountText, HudInk.OnChip);
                return amountText;
            }

            for (var i = 0; i < resourceTypes.Length; i++)
            {
                backgrounds[i] = Chip(resourceTypes[i].ToString());
                var icon = CreateImage(root.transform, $"Icon_{resourceTypes[i]}", Color.white);
                icon.sprite = CreateResourceIcon(resourceTypes[i]);
                icon.preserveAspect = true;
                var iconRect = icon.GetComponent<RectTransform>();
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.sizeDelta = new Vector2(48f, 48f);
                icons[i] = iconRect;
                amountTexts[i] = Amount(resourceTypes[i].ToString());
            }

            var populationBackground = Chip("Population");
            var popIcon = CreateImage(root.transform, "Icon_Population", Color.white);
            popIcon.sprite = CreatePopulationIcon();
            popIcon.preserveAspect = true;
            var popIconRect = popIcon.GetComponent<RectTransform>();
            popIconRect.anchorMin = popIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            popIconRect.sizeDelta = new Vector2(48f, 48f);
            // the free hands: a small disc on the icon's corner (ResourceHUDController.idleBadge)
            var idleBadge = CreateImage(popIcon.transform, "Idle", UiTheme.Good);
            idleBadge.sprite = SkinSprite("circle_mask");
            idleBadge.raycastTarget = false;
            var idleRect = idleBadge.rectTransform;
            // on the pill's upper-left rim, off the portrait's face
            idleRect.anchorMin = idleRect.anchorMax = new Vector2(0f, 1f);
            idleRect.anchoredPosition = new Vector2(-10f, 4f);
            idleRect.sizeDelta = new Vector2(34f, 34f);
            var idleOutline = idleBadge.gameObject.AddComponent<Outline>();
            idleOutline.effectColor = new Color(0.15f, 0.08f, 0.03f, 0.9f);
            idleOutline.effectDistance = new Vector2(1.5f, -1.5f);
            var idleText = CreateText(idleBadge.transform, "Count", "0", 22, new Vector2(0f, 1f), new Vector2(34f, 34f), Color.white);
            idleText.font = UiTheme.Heavy;
            idleText.raycastTarget = false;
            idleText.resizeTextForBestFit = true;
            idleText.resizeTextMinSize = 12;
            idleText.resizeTextMaxSize = 22;
            idleBadge.gameObject.SetActive(false);

            var populationText = Amount("Population");

            var hud = root.AddComponent<ResourceHUDController>();
            var hudSO = new SerializedObject(hud);
            var resourceOrderProp = hudSO.FindProperty("resourceOrder");
            resourceOrderProp.arraySize = resourceTypes.Length;
            var iconsProp = hudSO.FindProperty("icons");
            iconsProp.arraySize = icons.Length;
            var amountTextsProp = hudSO.FindProperty("amountTexts");
            amountTextsProp.arraySize = amountTexts.Length;
            for (var i = 0; i < resourceTypes.Length; i++)
            {
                resourceOrderProp.GetArrayElementAtIndex(i).enumValueIndex = (int)resourceTypes[i];
                iconsProp.GetArrayElementAtIndex(i).objectReferenceValue = icons[i];
                amountTextsProp.GetArrayElementAtIndex(i).objectReferenceValue = amountTexts[i];
            }
            hudSO.FindProperty("populationIcon").objectReferenceValue = popIconRect;
            hudSO.FindProperty("populationText").objectReferenceValue = populationText;
            hudSO.FindProperty("idleBadge").objectReferenceValue = idleBadge.gameObject;
            hudSO.FindProperty("idleText").objectReferenceValue = idleText;
            hudSO.FindProperty("barWidth").floatValue = barWidth;
            hudSO.FindProperty("barBackground").objectReferenceValue = barPlaque.rectTransform;
            if (chipSprite != null)
            {
                var backgroundsProp = hudSO.FindProperty("chipBackgrounds");
                backgroundsProp.arraySize = backgrounds.Length;
                for (var i = 0; i < backgrounds.Length; i++) backgroundsProp.GetArrayElementAtIndex(i).objectReferenceValue = backgrounds[i];
                hudSO.FindProperty("populationBackground").objectReferenceValue = populationBackground;
            }
            hudSO.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Top-right corner, stacked just below the Меню/Сохранить row (see BuildResourceHUD's
        /// comment for that shared y 420-510 band) so it never overlaps them. Square, not
        /// circular -- the game's no-circles cubic style applies to UI chrome too.
        /// </summary>
        private static void BuildMinimap(Transform canvasParent, Sprite panelSprite, RenderTexture minimapTexture, RTSCameraController cameraRig)
        {
            if (minimapTexture == null) return;

            // Round, in the bottom-left corner, in a wooden ring with a gold rim (the skin's
            // minimap). Without the skin files it falls back to the old square frame top-right.
            var round = SkinSprite("circle_mask") != null && SkinSprite("minimap_ring") != null;
            const float size = 212f;
            var border = round ? 6f : 8f;

            Image frame;
            if (round)
            {
                frame = CreateSkinImage(canvasParent, "Minimap", "circle_mask", new Vector2(-834f, -414f), new Vector2(size + border * 2f, size + border * 2f));
                frame.type = Image.Type.Simple;
                frame.color = UiTheme.Parchment;
                frame.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            }
            else
            {
                frame = CreateImage(canvasParent, "Minimap", new Color(0.26f, 0.29f, 0.24f, 0.95f));
                frame.sprite = panelSprite;
                frame.type = Image.Type.Sliced;
                var squareRect = frame.GetComponent<RectTransform>();
                squareRect.anchorMin = squareRect.anchorMax = new Vector2(0.5f, 0.5f);
                squareRect.anchoredPosition = new Vector2(810f, 300f);
                squareRect.sizeDelta = new Vector2(size + border * 2f, size + border * 2f);
            }

            var viewGO = new GameObject("MinimapView", typeof(RectTransform), typeof(RawImage));
            viewGO.transform.SetParent(frame.transform, false);
            var viewRect = viewGO.GetComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.offsetMin = new Vector2(border, border);
            viewRect.offsetMax = new Vector2(-border, -border);
            viewGO.GetComponent<RawImage>().texture = minimapTexture;

            // Diamond dot tracking the camera rig's ground position (see MinimapCameraIndicator)
            // -- so a player who's panned/zoomed off somewhere can tell where they're looking
            // from at a glance, instead of having to recognize terrain on the small live render.
            var markerGO = new GameObject("CameraMarker", typeof(RectTransform), typeof(Image));
            markerGO.transform.SetParent(viewGO.transform, false);
            var markerRect = markerGO.GetComponent<RectTransform>();
            markerRect.anchorMin = markerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerRect.pivot = new Vector2(0.5f, 0.5f);
            markerRect.sizeDelta = new Vector2(12f, 12f);
            markerRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            markerGO.GetComponent<Image>().color = new Color(1f, 0.92f, 0.3f);

            if (round)
            {
                // Over the map rather than inside the mask, so the ring is never clipped.
                var ring = HudImage(canvasParent, "MinimapRing", "minimap_ring", frame.rectTransform.sizeDelta + new Vector2(10f, 10f), sliced: false);
                ring.rectTransform.anchoredPosition = frame.rectTransform.anchoredPosition;
                ring.raycastTarget = false;
            }

            // An Invasion run's clock, on a plaque laid over the ring's foot (the screen's edge is
            // right under the map) -- RunClockLabel shows it in a run only.
            var clockRoot = new GameObject("RunClock", typeof(RectTransform));
            clockRoot.transform.SetParent(canvasParent, false);
            var clockRect = (RectTransform)clockRoot.transform;
            clockRect.anchorMin = clockRect.anchorMax = new Vector2(0.5f, 0.5f);
            clockRect.anchoredPosition = frame.rectTransform.anchoredPosition + new Vector2(0f, -frame.rectTransform.sizeDelta.y * 0.5f + 22f);
            clockRect.sizeDelta = new Vector2(132f, 46f);
            var clockPlaque = HudImage(clockRoot.transform, "Plaque", "plaque", new Vector2(132f, 46f));
            clockPlaque.raycastTarget = false;
            var clockText = CreateText(clockPlaque.transform, "Time", "0:00", 30, new Vector2(0f, 2f), new Vector2(124f, 42f), UiTheme.Ink);
            clockText.font = UiTheme.Heavy;
            clockText.raycastTarget = false;
            SkinInk(clockText, HudInk.OnPlaque);
            var clock = clockRoot.AddComponent<RunClockLabel>();
            var clockSO = new SerializedObject(clock);
            clockSO.FindProperty("plaque").objectReferenceValue = clockPlaque.gameObject;
            clockSO.FindProperty("time").objectReferenceValue = clockText;
            clockSO.ApplyModifiedPropertiesWithoutUndo();
            clockPlaque.gameObject.SetActive(false);

            var indicator = viewGO.AddComponent<MinimapCameraIndicator>();
            var indicatorSO = new SerializedObject(indicator);
            indicatorSO.FindProperty("cameraRig").objectReferenceValue = cameraRig;
            indicatorSO.FindProperty("marker").objectReferenceValue = markerRect;
            // Same GridCellsX/CellSize world span the minimap camera (CreateMinimapCamera) is
            // framed to, expressed as UI units per world unit -- see MinimapCameraIndicator.
            indicatorSO.FindProperty("worldToMapScale").floatValue = size / (GridCellsX * CellSize);
            indicatorSO.FindProperty("mapHalfExtent").floatValue = size * 0.5f;
            // the minimap's camera: an Invasion run's follows the camera rig (MinimapCameraUpdater)
            var updater = Object.FindAnyObjectByType<MinimapCameraUpdater>();
            if (updater != null)
            {
                indicatorSO.FindProperty("minimapCamera").objectReferenceValue = updater.MinimapCamera;
                indicatorSO.FindProperty("follow").objectReferenceValue = updater;
                var updaterSO = new SerializedObject(updater);
                updaterSO.FindProperty("cameraRig").objectReferenceValue = cameraRig;
                updaterSO.FindProperty("view").objectReferenceValue = viewGO.GetComponent<RawImage>();
                updaterSO.ApplyModifiedPropertiesWithoutUndo();
            }
            indicatorSO.ApplyModifiedPropertiesWithoutUndo();

            // An Invasion run's attack alerts: a blinking «!» per lane a wave is coming down
            // (MinimapAttackAlerts places and shows them; hidden otherwise).
            var marks = new Object[3];
            for (var i = 0; i < marks.Length; i++)
            {
                var badge = CreateImage(viewGO.transform, $"AttackAlert{i}", UiTheme.Danger);
                badge.sprite = SkinSprite("circle_mask");
                badge.raycastTarget = false;
                badge.rectTransform.sizeDelta = new Vector2(34f, 34f);
                var bang = CreateText(badge.transform, "Mark", "!", 28, Vector2.zero, new Vector2(34f, 34f), Color.white);
                bang.fontStyle = FontStyle.Bold;
                bang.raycastTarget = false;
                marks[i] = badge.gameObject.AddComponent<CanvasGroup>();
                badge.gameObject.SetActive(false);
            }
            // the raiding party's own «!», in another colour than the waves' (the user, 2026-10-24)
            var partyBadge = CreateImage(viewGO.transform, "RaidPartyAlert", UiTheme.Magic);
            partyBadge.sprite = SkinSprite("circle_mask");
            partyBadge.raycastTarget = false;
            partyBadge.rectTransform.sizeDelta = new Vector2(34f, 34f);
            var partyBang = CreateText(partyBadge.transform, "Mark", "!", 28, Vector2.zero, new Vector2(34f, 34f), Color.white);
            partyBang.fontStyle = FontStyle.Bold;
            partyBang.raycastTarget = false;
            var partyMark = partyBadge.gameObject.AddComponent<CanvasGroup>();
            partyBadge.gameObject.SetActive(false);
            // the New Year map's Thief, gold
            var thiefBadge = CreateImage(viewGO.transform, "ThiefAlert", UiTheme.Gold);
            thiefBadge.sprite = SkinSprite("circle_mask");
            thiefBadge.raycastTarget = false;
            thiefBadge.rectTransform.sizeDelta = new Vector2(34f, 34f);
            var thiefBang = CreateText(thiefBadge.transform, "Mark", "!", 28, Vector2.zero, new Vector2(34f, 34f), Color.white);
            thiefBang.fontStyle = FontStyle.Bold;
            thiefBang.raycastTarget = false;
            var thiefMark = thiefBadge.gameObject.AddComponent<CanvasGroup>();
            thiefBadge.gameObject.SetActive(false);

            var alerts = viewGO.AddComponent<MinimapAttackAlerts>();
            var alertsSO = new SerializedObject(alerts);
            alertsSO.FindProperty("map").objectReferenceValue = indicator;
            alertsSO.FindProperty("partyMark").objectReferenceValue = partyMark;
            alertsSO.FindProperty("thiefMark").objectReferenceValue = thiefMark;
            var list = alertsSO.FindProperty("marks");
            list.arraySize = marks.Length;
            for (var i = 0; i < marks.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = marks[i];
            alertsSO.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Click-to-select-a-citizen-then-click-to-move-it (see CitizenSelector). The feedback
        /// label is dead center, large and shadowed like the main menu title so the momentary
        /// OK!/NO! flash reads clearly regardless of what's behind it; hidden until the first
        /// destination click.
        /// </summary>
        private static CitizenSelector BuildCitizenSelector(Transform canvasParent, Camera targetCamera)
        {
            var feedbackText = CreateText(canvasParent, "CitizenMoveFeedback", string.Empty, 64, Vector2.zero, new Vector2(400f, 100f), addShadow: true);
            feedbackText.gameObject.SetActive(false);

            var go = new GameObject("CitizenSelector");
            go.transform.SetParent(canvasParent, false);
            var selector = go.AddComponent<CitizenSelector>();
            var so = new SerializedObject(selector);
            so.FindProperty("targetCamera").objectReferenceValue = targetCamera;
            so.FindProperty("feedbackText").objectReferenceValue = feedbackText;
            // The flag a move order plants (Tools/blender/orderflag.py). Referenced from the scene
            // so it travels into the build; without it the order still shows its ripple and trail.
            var orderFlag = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsBuildingsFolder}/OrderFlag.fbx");
            if (orderFlag == null) Debug.LogWarning("[SetupProject] Models/Buildings/OrderFlag.fbx is missing -- move orders will show no flag.");
            so.FindProperty("orderFlagPrefab").objectReferenceValue = orderFlag;
            so.ApplyModifiedPropertiesWithoutUndo();
            return selector;
        }

        /// <summary>
        /// The crosshair sitting at the placement aim point: a hollow square of four thin bars,
        /// which reads as "this cell" without covering the ghost standing in it. Positioned in
        /// screen pixels every frame by PlacementHudController.
        /// </summary>
        private static RectTransform CreateAimMarker(Transform canvasParent)
        {
            var root = new GameObject("PlacementAimMarker", typeof(RectTransform));
            root.transform.SetParent(canvasParent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(96f, 96f);

            var color = new Color(1f, 1f, 1f, 0.85f);
            const float length = 34f;
            const float thickness = 4f;

            CreateAimBar(root.transform, "Top", new Vector2(0f, 48f), new Vector2(length, thickness), color);
            CreateAimBar(root.transform, "Bottom", new Vector2(0f, -48f), new Vector2(length, thickness), color);
            CreateAimBar(root.transform, "Left", new Vector2(-48f, 0f), new Vector2(thickness, length), color);
            CreateAimBar(root.transform, "Right", new Vector2(48f, 0f), new Vector2(thickness, length), color);

            root.SetActive(false);
            return rect;
        }

        private static void CreateAimBar(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color color)
        {
            var bar = CreateImage(parent, name, color);
            var rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
        }

        /// <summary>
        /// The badge naming whatever is selected, with the X that clears it. Top-centre, clear of
        /// the minimap on the right and the event log on the left.
        ///
        /// It exists because selection had no visible exit on a phone at all: a selected citizen
        /// could only be released with Escape or a right click, and while one is selected every
        /// world tap is an order to them -- so the player was stuck in a mode they could not see
        /// and could not leave.
        /// </summary>
        private static void BuildSelectionBadge(Transform canvasParent, Sprite panelSprite, CitizenSelector citizenSelector)
        {
            var badge = CreateImage(canvasParent, "SelectionBadge", new Color(0.16f, 0.18f, 0.15f, 0.92f));
            badge.sprite = panelSprite;
            badge.type = Image.Type.Sliced;
            var badgeRect = badge.GetComponent<RectTransform>();
            badgeRect.anchorMin = badgeRect.anchorMax = badgeRect.pivot = new Vector2(0.5f, 1f);
            // Under the resource row (which ends 165 down), not over it: at -30 it covered the
            // status pill and the right end of the chips whenever a citizen was selected.
            badgeRect.anchoredPosition = new Vector2(0f, -274f);
            badgeRect.sizeDelta = new Vector2(460f, 96f);

            var label = CreateText(badge.transform, "Label", string.Empty, 30, new Vector2(-40f, 0f), new Vector2(300f, 60f));

            var cancelIcon = CreateCancelIcon();
            var clearButton = CreateIconButton(badge.transform, panelSprite, cancelIcon, "ClearSelectionButton", new Vector2(170f, 0f), new Vector2(76f, 76f));

            // On the Canvas, not on the badge: a component that deactivates its own GameObject
            // stops getting Update() and could never switch itself back on.
            var controller = canvasParent.gameObject.AddComponent<SelectionBadgeController>();
            var controllerSO = new SerializedObject(controller);
            controllerSO.FindProperty("citizenSelector").objectReferenceValue = citizenSelector;
            controllerSO.FindProperty("badgeRoot").objectReferenceValue = badge.gameObject;
            controllerSO.FindProperty("label").objectReferenceValue = label;
            controllerSO.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(clearButton.onClick, controller.ClearSelection);

            badge.gameObject.SetActive(false);
        }

        /// <summary>Full-screen defeat panel, same backdrop+card pattern as the dialog above -- shown by GameOverController once GameOverManager reports the Town Hall destroyed. Only action is back to the menu; there's nothing left to confirm/cancel.</summary>
        private static void BuildGameOverUI(Transform canvasParent, Sprite panelSprite)
        {
            var panelRoot = new GameObject("GameOverPanel", typeof(RectTransform));
            panelRoot.AddComponent<HudOverlay>();
            panelRoot.transform.SetParent(canvasParent, false);
            StretchFull(panelRoot.GetComponent<RectTransform>());

            var backdrop = CreateImage(panelRoot.transform, "Backdrop", new Color(0f, 0f, 0f, 0.8f));
            StretchFull(backdrop.GetComponent<RectTransform>());

            var card = CreateImage(panelRoot.transform, "Card", new Color(0.16f, 0.18f, 0.15f, 0.98f));
            card.sprite = panelSprite;
            card.type = Image.Type.Sliced;
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(760f, 440f);
            cardRect.anchoredPosition = Vector2.zero;

            // Text and colour are set per outcome by GameOverController -- one panel serves both
            // the defeat and the "all portals closed" victory.
            var title = CreateText(card.transform, "Title", string.Empty, 44, new Vector2(0f, 135f), new Vector2(680f, 70f), new Color(0.9f, 0.35f, 0.3f));
            // three lines in an Invasion run: waves, points, today's points
            var reason = CreateText(card.transform, "Reason", string.Empty, 26, new Vector2(0f, 25f), new Vector2(680f, 130f), new Color(1f, 1f, 1f, 0.8f));

            var menuButton = CreateButton(card.transform, panelSprite, "GameOverMenuButton", "#over_to_menu", new Vector2(-190f, -135f), new Vector2(340f, 90f));
            // a win offers the user's free mode: keep playing the map
            var continueButton = CreateButton(card.transform, panelSprite, "GameOverContinueButton", "#over_continue", new Vector2(190f, -135f), new Vector2(340f, 90f));

            // The controller lives on its own always-active object, NOT on the panel it shows.
            // A component on a deactivated GameObject never gets Start(), so putting it on
            // panelRoot (deactivated just below) meant it never subscribed to
            // GameOverManager.OnGameOver -- the Town Hall could be destroyed and the defeat
            // screen would simply never appear. Same reasoning as PlacementHudController.
            var controllerGO = new GameObject("GameOverController");
            controllerGO.transform.SetParent(canvasParent, false);
            var controller = controllerGO.AddComponent<GameOverController>();
            var controllerSO = new SerializedObject(controller);
            controllerSO.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            controllerSO.FindProperty("titleLabel").objectReferenceValue = title;
            controllerSO.FindProperty("reasonLabel").objectReferenceValue = reason;
            controllerSO.FindProperty("continueButton").objectReferenceValue = continueButton.gameObject;
            controllerSO.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(menuButton.onClick, controller.ReturnToMenu);
            UnityEventTools.AddPersistentListener(continueButton.onClick, controller.ContinuePlaying);

            panelRoot.SetActive(false);
        }

        /// <summary>The save dialog, opened from the in-game menu (BuildGameMenu); the autosave runs on its own.</summary>
        private static (SaveDialogController dialog, GameObject root) BuildSaveUI(Transform canvasParent, Sprite panelSprite, GameSaveController saveController)
        {
            var dialogRoot = new GameObject("SaveDialog", typeof(RectTransform));
            dialogRoot.AddComponent<HudOverlay>();
            dialogRoot.transform.SetParent(canvasParent, false);
            StretchFull(dialogRoot.GetComponent<RectTransform>());

            var backdrop = CreateImage(dialogRoot.transform, "Backdrop", new Color(0f, 0f, 0f, 0.7f));
            StretchFull(backdrop.GetComponent<RectTransform>());

            var card = CreateImage(dialogRoot.transform, "Card", new Color(0.16f, 0.18f, 0.15f, 0.98f));
            card.sprite = panelSprite;
            card.type = Image.Type.Sliced;
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(760f, 420f);
            cardRect.anchoredPosition = Vector2.zero;

            Localize(CreateText(card.transform, "Title", string.Empty, 40, new Vector2(0f, 150f), new Vector2(680f, 70f)), "#save_title");

            var nameInput = CreateInputField(card.transform, "NameInput", "MyCityName", new Vector2(0f, 30f), new Vector2(600f, 90f), panelSprite);

            var confirmButton = CreateButton(card.transform, panelSprite, "ConfirmSaveButton", "#save_confirm", new Vector2(-160f, -100f), new Vector2(300f, 90f));
            var cancelButton = CreateButton(card.transform, panelSprite, "CancelSaveButton", "#ui_cancel", new Vector2(160f, -100f), new Vector2(300f, 90f));

            var dialogController = dialogRoot.AddComponent<SaveDialogController>();
            var dialogSO = new SerializedObject(dialogController);
            dialogSO.FindProperty("saveController").objectReferenceValue = saveController;
            dialogSO.FindProperty("dialogRoot").objectReferenceValue = dialogRoot;
            dialogSO.FindProperty("nameInput").objectReferenceValue = nameInput;
            dialogSO.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(confirmButton.onClick, dialogController.ConfirmSave);
            UnityEventTools.AddPersistentListener(cancelButton.onClick, dialogController.CloseDialog);

            dialogRoot.SetActive(false);
            return (dialogController, dialogRoot);
        }

        /// <summary>A scrollable, vertically stacked list whose rows are built at runtime -- the save browser's slots, the workforce panel's workplaces.</summary>
        private static void BuildScrollList(Transform parent, Vector2 position, Vector2 size, string emptyKey, out RectTransform content, out GameObject emptyLabelGO)
        {
            var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollGO.transform.SetParent(parent, false);
            var scrollRectTransform = scrollGO.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = scrollRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchoredPosition = position;
            scrollRectTransform.sizeDelta = size;

            var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var viewportRect = viewportGO.GetComponent<RectTransform>();
            StretchFull(viewportRect);
            viewportGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);

            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRect = contentGO.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            var layoutGroup = contentGO.GetComponent<VerticalLayoutGroup>();
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 12f;
            layoutGroup.padding = new RectOffset(8, 8, 8, 8);

            var sizeFitter = contentGO.GetComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = scrollGO.GetComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            var emptyLabel = Localize(CreateText(viewportGO.transform, "EmptyLabel", string.Empty, 28, Vector2.zero, new Vector2(700f, 80f), new Color(1f, 1f, 1f, 0.6f)), emptyKey);

            content = contentRect;
            emptyLabelGO = emptyLabel.gameObject;
        }
    }
}
