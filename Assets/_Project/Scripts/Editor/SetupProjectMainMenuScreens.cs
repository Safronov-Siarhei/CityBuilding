using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Maps;
using CityBuilder.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// The main menu's screens after the user's sketches (2026-10-25), in the menu's own sprites
    /// (the Classic look's are the ones drawn for it; the other looks show the same layout in theirs):
    ///
    ///   main     the settings at the top; «Продолжить» and «Предложения» in the lower middle; Discord
    ///            and YouTube under them -- the shop, Discord and YouTube wear a «!» until first opened;
    ///   modes    back and settings in the top corners, Discord and YouTube in the bottom ones, the two
    ///            modes side by side, each with its «!»;
    ///   sandbox  a window: the map cards in the theme picked, the five themes, «Начать»; back, the
    ///            saves, the «!» and the settings in the corners;
    ///   invasion a window: the theme's picture with the King and the Queen leaning in at its sides,
    ///            the points, the crystals, the «!», the themes on it; under it the period's best runs,
    ///            the player's row, «Забег»; the day / week / month tabs under the window.
    /// </summary>
    public static partial class SetupProject
    {
        private static readonly Vector2 MenuWindowSize = new Vector2(1560f, 840f);
        private static readonly Vector2 MenuWindowPos = new Vector2(0f, -10f);
        private const float CornerButton = 140f;
        /// <summary>The period tabs: centred between the window's foot and the screen's (1080 high), the user 2026-10-25.</summary>
        private static float PeriodTabsY => (MenuWindowPos.y - MenuWindowSize.y * 0.5f - 540f) * 0.5f;
        private const float CornerInset = 100f;
        private static readonly Color BadgeColour = new Color(0.94f, 0.54f, 0.17f);

        private static void BuildMainMenu(Transform canvas, Sprite panelSprite)
        {
            BeginHudSkins();
            var controller = canvas.gameObject.AddComponent<MainMenuController>();
            var shopBadges = new List<Object>();
            var discordBadges = new List<Object>();
            var youTubeBadges = new List<Object>();

            var vignette = MenuImage(canvas, "Vignette", "vignette", Vector2.zero, Vector2.zero, Image.Type.Simple);
            StretchFull(vignette.rectTransform);
            vignette.raycastTarget = false;

            // ---- main
            var main = StretchedChild(canvas, "MainScreen");
            // top left (the piggy bank took the top right; the language button stands beside it, 2026-11-17)
            var settings = Corner(main, "SettingsButton", "Settings", new Vector2(0f, 1f), new Vector2(CornerInset, -CornerInset), 150f);
            var play = MenuTextButton(main, "ContinueButton", "button", "#menu_continue", HudInk.OnButton, Vector2.zero, new Vector2(560f, 140f), 50);
            Anchor((RectTransform)play.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(560f, 140f));
            var offers = MenuTextButton(main, "OffersButton", "plaque", "#menu_offers", HudInk.OnMenuPlaque, Vector2.zero, new Vector2(440f, 110f), 38);
            Anchor((RectTransform)offers.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 195f), new Vector2(440f, 110f));
            ShopStall(offers.transform);
            shopBadges.Add(Badge(offers.transform, left: true));
            var discord = BrandCorner(main, "DiscordButton", "Discord", new Vector2(0.5f, 0f), new Vector2(-340f, 120f), CornerButton, DiscordColour);
            discordBadges.Add(Badge(discord.transform));
            var youTube = BrandCorner(main, "YouTubeButton", "YouTube", new Vector2(0.5f, 0f), new Vector2(340f, 120f), CornerButton, YouTubeColour);
            youTubeBadges.Add(Badge(youTube.transform));
            // the game's name over the village (concept 1, the user 2026-11-17: gold letters, a dark rim, a soft
            // shadow; Tools/ui/main_menu_village_mockup.py draws it), at the mock-up's place and size
            var logo = CreateImage(main, "Logo", Color.white);
            logo.sprite = MenuArt("Logo_CityCraft");
            logo.preserveAspect = true;
            logo.raycastTarget = false;
            Anchor(logo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(638f, 170f));
            BuildLanguagePicker(main);   // right of the settings button; last, so its open list lies over the rest

            // ---- the modes
            var modes = StretchedChild(canvas, "ModesScreen");
            var dim = CreateImage(modes, "Dim", new Color(0.05f, 0.03f, 0.01f, 0.25f));
            StretchBleed(dim.rectTransform);
            dim.raycastTarget = false;
            var modesBack = Corner(modes, "BackButton", "Back", new Vector2(0f, 1f), new Vector2(CornerInset, -CornerInset), CornerButton);
            var modesSettings = Corner(modes, "SettingsButton", "Settings", new Vector2(1f, 1f), new Vector2(-CornerInset, -CornerInset), CornerButton);
            var modesDiscord = BrandCorner(modes, "DiscordButton", "Discord", new Vector2(0f, 0f), new Vector2(CornerInset, CornerInset), CornerButton, DiscordColour);
            discordBadges.Add(Badge(modesDiscord.transform));
            var modesYouTube = BrandCorner(modes, "YouTubeButton", "YouTube", new Vector2(1f, 0f), new Vector2(-CornerInset, CornerInset), CornerButton, YouTubeColour);
            youTubeBadges.Add(Badge(modesYouTube.transform));
            // the hall of banners (the user, 2026-11-10, «Зал знамён»): Tutorial, Sandbox, Invasion,
            // Campaign, drawn in the HUD's Classic kit by Tools/ui/make_mode_banners.py
            BuildModeBanners(modes, controller);

            // ---- the sandbox and the invasion
            var sandboxScreen = BuildSandboxScreen(canvas, controller, out var sandbox, out var sandboxSettings, out var sandboxFront);
            var invasionScreen = BuildInvasionScreen(canvas, controller, out var invasionSettings);

            // ---- dialogs over everything
            var settingsPanel = BuildSettingsPanel(canvas, panelSprite, settings);
            foreach (var other in new[] { modesSettings, sandboxSettings, invasionSettings }) UnityEventTools.AddPersistentListener(other.onClick, settingsPanel.OpenPanel);

            var exitDialog = BuildMenuDialog(canvas, "ExitDialog", "#quit_question", null, new Vector2(820f, 420f), out var exitCard);
            var stay = MenuTextButton(exitCard, "StayButton", "plaque", "#quit_stay", HudInk.OnMenuPlaque, new Vector2(-180f, -90f), new Vector2(320f, 120f));
            var quit = MenuTextButton(exitCard, "QuitButton", "button", "#quit_confirm", HudInk.OnButton, new Vector2(180f, -90f), new Vector2(320f, 120f));
            UnityEventTools.AddPersistentListener(stay.onClick, controller.CloseExitDialog);
            UnityEventTools.AddPersistentListener(quit.onClick, controller.QuitGame);

            // «Магазин» is built in code when opened (UI/MenuShopWindow, the user, 2026-11-15) -- no dialog here

            var infoDialog = BuildMenuDialog(canvas, "InfoDialog", "#mode_sandbox", null, new Vector2(1000f, 620f), out var infoCard);
            var infoTitle = infoCard.Find("TitlePlate/Title").GetComponent<Text>();
            Object.DestroyImmediate(infoTitle.GetComponent<LocalizedText>());
            var infoText = MenuLabel(infoCard, "Text", null, 32, new Vector2(0f, 40f), new Vector2(840f, 340f), HudInk.OnPanel);
            var closeInfo = MenuTextButton(infoCard, "CloseButton", "plaque", "#ui_close", HudInk.OnMenuPlaque, new Vector2(0f, -220f), new Vector2(320f, 110f));
            UnityEventTools.AddPersistentListener(closeInfo.onClick, controller.CloseInfo);

            var savesDialog = BuildSavesDialog(canvas, controller, out var loadPanel);
            // «Свобода песочницы»'s purchase banner, over everything; it hides the sandbox screen's front
            BuildFreedomBanner(canvas, SandLayout(), new[] { sandboxFront }, false);

            UnityEventTools.AddPersistentListener(play.onClick, controller.OpenModes);
            UnityEventTools.AddPersistentListener(offers.onClick, controller.OpenShop);
            foreach (var d in new[] { discord, modesDiscord }) UnityEventTools.AddPersistentListener(d.onClick, controller.OpenDiscord);
            foreach (var y in new[] { youTube, modesYouTube }) UnityEventTools.AddPersistentListener(y.onClick, controller.OpenYouTube);
            UnityEventTools.AddPersistentListener(modesBack.onClick, controller.Back);

            var so = new SerializedObject(controller);
            so.FindProperty("mainScreen").objectReferenceValue = Group(main.gameObject);
            so.FindProperty("modesScreen").objectReferenceValue = Group(modes.gameObject);
            so.FindProperty("sandboxScreen").objectReferenceValue = sandboxScreen;
            so.FindProperty("invasionScreen").objectReferenceValue = invasionScreen;
            so.FindProperty("invasionHub").objectReferenceValue = invasionScreen.GetComponent<InvasionHubController>();
            so.FindProperty("exitDialog").objectReferenceValue = exitDialog;
            so.FindProperty("infoDialog").objectReferenceValue = infoDialog;
            so.FindProperty("infoTitle").objectReferenceValue = infoTitle;
            so.FindProperty("infoText").objectReferenceValue = infoText;
            so.FindProperty("savesDialog").objectReferenceValue = savesDialog;
            so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            so.FindProperty("sandbox").objectReferenceValue = sandbox;
            so.FindProperty("loadGamePanel").objectReferenceValue = loadPanel;
            WriteArray(so.FindProperty("shopBadges"), shopBadges.ToArray());
            WriteArray(so.FindProperty("discordBadges"), discordBadges.ToArray());
            WriteArray(so.FindProperty("youTubeBadges"), youTubeBadges.ToArray());
            so.ApplyModifiedPropertiesWithoutUndo();

            BuildMenuSkinApplier(canvas);
        }

        private static void WriteArray(SerializedProperty property, Object[] values)
        {
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static void WriteStrings(SerializedProperty property, IList<string> values)
        {
            property.arraySize = values.Count;
            for (var i = 0; i < values.Count; i++) property.GetArrayElementAtIndex(i).stringValue = values[i];
        }

        /// <summary>A round picture button held at an anchor of the screen (a corner, a side's middle).</summary>
        private static Button Corner(Transform parent, string name, string glyph, Vector2 anchor, Vector2 offset, float size)
        {
            var button = MenuRoundButton(parent, name, "round", glyph, HudInk.OnRound, size, 0.5f);
            Anchor((RectTransform)button.transform, anchor, new Vector2(0.5f, 0.5f), offset, new Vector2(size, size));
            return button;
        }

        /// <summary>Discord's own blurple and YouTube's red (the user, 2026-10-28: «цветными», each in its brand's colour).</summary>
        private static readonly Color DiscordColour = new Color32(0x58, 0x65, 0xF2, 0xFF);
        private static readonly Color YouTubeColour = new Color32(0xFF, 0x00, 0x00, 0xFF);

        /// <summary>
        /// A corner button in a brand's colour: the kit's wooden ring kept, its face filled with
        /// the colour, the glyph white on it -- whatever the menu's look.
        /// </summary>
        private static Button BrandCorner(Transform parent, string name, string glyph, Vector2 anchor, Vector2 offset, float size, Color colour)
        {
            var button = MenuRoundButton(parent, name, "round", glyph, HudInk.Keep, size, 0.5f);
            Anchor((RectTransform)button.transform, anchor, new Vector2(0.5f, 0.5f), offset, new Vector2(size, size));
            var face = CreateImage(button.transform, "BrandFace", colour);
            face.sprite = SkinSprite("circle_mask");
            face.raycastTarget = false;
            Anchor(face.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), new Vector2(size * 0.78f, size * 0.78f));
            face.transform.SetSiblingIndex(0);
            var icon = button.transform.Find("Icon").GetComponent<Image>();
            icon.color = Color.white;
            return button;
        }

        /// <summary>The shop's plaque: the word moved left, the stall icon of the in-game shop in the right end
        /// (the user, 2026-11-15: «переименовать в Магазин, а в правой части кнопки пускай будет иконка ларька», variant 1).</summary>
        private static void ShopStall(Transform plaque)
        {
            var size = ((RectTransform)plaque).sizeDelta;
            var label = (RectTransform)plaque.Find("Label");
            if (label != null)
            {
                label.anchoredPosition = new Vector2(-46f, 4f);
                label.sizeDelta = new Vector2(size.x - 170f, size.y - 20f);
            }
            var stall = CreateImage(plaque, "Stall", Color.white);
            stall.sprite = ShopSprite("Stall");
            stall.preserveAspect = true;
            stall.raycastTarget = false;
            Anchor(stall.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(size.x * 0.5f - 74f, 2f), new Vector2(96f, 96f));   // the rich stall fills its square (the user 2026-11-16: 130 too big)
        }

        /// <summary>The orange «!» on a button's top-right (top-left if <paramref name="left"/>), until its place is first opened (MainMenuController).</summary>
        private static GameObject Badge(Transform target, bool left = false)
        {
            var badge = CreateImage(target, "Badge", BadgeColour);
            badge.sprite = SkinSprite("circle_mask");
            badge.raycastTarget = false;
            var size = ((RectTransform)target).sizeDelta;
            Anchor(badge.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((size.x * 0.5f - 14f) * (left ? -1f : 1f), size.y * 0.5f - 14f), new Vector2(56f, 56f));
            var outline = badge.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.35f, 0.15f, 0.03f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);
            var mark = CreateText(badge.transform, "Mark", "!", 40, new Vector2(0f, 1f), new Vector2(56f, 56f), Color.white);
            mark.font = UiTheme.Heavy;
            mark.alignment = TextAnchor.MiddleCenter;
            mark.raycastTarget = false;
            return badge.gameObject;
        }

        /// <summary>A mode's window, its title on a plate across the top edge.</summary>
        private static Transform MenuWindow(Transform screen, string titleKey)
        {
            var window = MenuImage(screen, "Window", "panel", MenuWindowPos, MenuWindowSize, Image.Type.Tiled);
            var title = MenuImage(window.transform, "TitlePlate", "title", new Vector2(0f, MenuWindowSize.y * 0.5f - 6f), new Vector2(560f, 110f));
            title.raycastTarget = false;
            MenuLabel(title.transform, "Title", titleKey, 44, new Vector2(0f, 4f), new Vector2(500f, 70f), HudInk.OnTitle, bold: true);
            return window.transform;
        }

        /// <summary>The five themes as tabs with their pictures (the sandbox board, 2026-11-10: pictures, not words); the picked tab stands out.</summary>
        private static ThemePicker BuildThemeTabs(Transform parent, Vector2 centre)
        {
            const float width = 170f, height = 96f, step = 184f;
            var row = new GameObject("ThemeTabs", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            Anchor((RectTransform)row.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), centre, new Vector2(step * MapThemes.All.Length, height));
            var picker = row.AddComponent<ThemePicker>();
            var buttons = new Object[MapThemes.All.Length];
            var picked = new Object[MapThemes.All.Length];
            for (var i = 0; i < MapThemes.All.Length; i++)
            {
                var id = MapThemes.Id(MapThemes.All[i]);
                var size = new Vector2(width, height);
                var idle = MenuImage(row.transform, $"Theme_{id}", "tab", new Vector2((i - (MapThemes.All.Length - 1) * 0.5f) * step, 0f), size);
                buttons[i] = idle.gameObject.AddComponent<Button>();
                MenuGlyph(idle.transform, $"Theme_{id}", HudInk.OnTab, 58f);
                var lit = MenuImage(idle.transform, "Picked", "tab_selected", new Vector2(0f, 8f), size + new Vector2(0f, 16f));
                lit.raycastTarget = false;
                MenuGlyph(lit.transform, $"Theme_{id}", HudInk.OnTabSelected, 66f);
                lit.gameObject.SetActive(false);
                picked[i] = lit.gameObject;
            }
            var so = new SerializedObject(picker);
            so.FindProperty("invasion").boolValue = false;
            WriteArray(so.FindProperty("buttons"), buttons);
            WriteArray(so.FindProperty("picked"), picked);
            so.ApplyModifiedPropertiesWithoutUndo();
            return picker;
        }

        /// <summary>The five theme buttons in a row about `centre`; the picked one wears the gold face.</summary>
        private static ThemePicker BuildThemes(Transform parent, Vector2 centre, float size, float spacing, bool invasion)
        {
            var row = new GameObject("Themes", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            Anchor((RectTransform)row.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), centre, new Vector2(spacing * MapThemes.All.Length, size));
            var picker = row.AddComponent<ThemePicker>();
            var buttons = new Object[MapThemes.All.Length];
            var picked = new Object[MapThemes.All.Length];
            for (var i = 0; i < MapThemes.All.Length; i++)
            {
                var id = MapThemes.Id(MapThemes.All[i]);
                var button = MenuRoundButton(row.transform, $"Theme_{id}", "round", $"Theme_{id}", HudInk.OnRound, size, 0.56f);
                Anchor((RectTransform)button.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - (MapThemes.All.Length - 1) * 0.5f) * spacing, 0f), new Vector2(size, size));
                buttons[i] = button;
                var lit = MenuImage(button.transform, "Picked", "round_main", Vector2.zero, new Vector2(size * 1.12f, size * 1.12f), Image.Type.Simple);
                lit.preserveAspect = true;
                lit.raycastTarget = false;
                MenuGlyph(lit.transform, $"Theme_{id}", HudInk.OnMain, size * 0.6f);
                lit.gameObject.SetActive(false);
                picked[i] = lit.gameObject;
            }
            var so = new SerializedObject(picker);
            so.FindProperty("invasion").boolValue = invasion;
            WriteArray(so.FindProperty("buttons"), buttons);
            WriteArray(so.FindProperty("picked"), picked);
            so.ApplyModifiedPropertiesWithoutUndo();
            return picker;
        }

        // ---------------------------------------------------------------- the sandbox

        // the screen itself: SetupProjectSandboxHub.cs («Стол картографа», 2026-11-17)

        private static void AddArt(List<string> keys, List<Object> sprites, string key, string art)
        {
            var sprite = MenuArt(art);
            if (sprite == null) return;
            keys.Add(key);
            sprites.Add(sprite);
        }

        /// <summary>The sandbox's saves, in a window over the screen: the list, which one is picked, «Загрузить».</summary>
        private static GameObject BuildSavesDialog(Transform canvas, MainMenuController controller, out LoadGamePanelController loadPanel)
        {
            var dialog = BuildMenuDialog(canvas, "SavesDialog", "#sandbox_saves", null, new Vector2(1300f, 860f), out var card);
            var body = StretchedChild(card, "Saves");
            BuildScrollList(body, new Vector2(0f, 50f), new Vector2(1140f, 520f), "#load_empty", out var saveList, out var empty);
            var viewport = body.Find("ScrollView/Viewport");
            if (viewport != null) viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.06f);
            SkinInk(empty.GetComponent<Text>(), HudInk.OnPanel);
            var picked = MenuLabel(body, "SelectedLabel", null, 28, new Vector2(-330f, -300f), new Vector2(460f, 60f), HudInk.OnPanel, align: TextAnchor.MiddleLeft);
            var close = MenuTextButton(body, "CloseButton", "plaque", "#ui_close", HudInk.OnMenuPlaque, new Vector2(120f, -310f), new Vector2(300f, 110f));
            var load = MenuTextButton(body, "LoadButton", "button", "#load_confirm", HudInk.OnButton, new Vector2(430f, -310f), new Vector2(300f, 110f), 40);
            loadPanel = body.gameObject.AddComponent<LoadGamePanelController>();
            var so = new SerializedObject(loadPanel);
            so.FindProperty("panelRoot").objectReferenceValue = body.gameObject;
            so.FindProperty("listContent").objectReferenceValue = saveList;
            so.FindProperty("emptyLabel").objectReferenceValue = empty;
            so.FindProperty("loadSelectedButton").objectReferenceValue = load;
            so.FindProperty("selectedNameLabel").objectReferenceValue = picked;
            so.FindProperty("menuLook").boolValue = true;
            // each row: the save's theme on a round, then day, people, buildings and the map code
            // the theme's picture alone, no round under it (the user, 2026-11-10)
            var badge = new GameObject("ThemeBadgeTemplate", typeof(RectTransform));
            badge.transform.SetParent(body, false);
            ((RectTransform)badge.transform).sizeDelta = new Vector2(84f, 84f);
            MenuGlyph(badge.transform, "Theme_summer", HudInk.OnPanel, 66f);
            badge.SetActive(false);
            so.FindProperty("themeBadgeTemplate").objectReferenceValue = badge;
            var glyphs = new Object[MapThemes.All.Length];
            for (var i = 0; i < glyphs.Length; i++) glyphs[i] = HudGlyph($"Theme_{MapThemes.Id(MapThemes.All[i])}");
            WriteArray(so.FindProperty("themeGlyphs"), glyphs);
            // pieces, not buildings: roads, fences, moats, bridges and the decor pieces
            WriteStrings(so.FindProperty("uncountedBuildings"), new[] { "Road", "Fence", "Moat", "Bridge", "Drawbridge", "DecTree", "DecBush", "DecGarden" });
            // the bin on each row and its «sure?» card (the user, 2026-11-17)
            so.FindProperty("trashGlyph").objectReferenceValue = HudGlyph("Trash");
            var confirm = new GameObject("DeleteConfirm", typeof(RectTransform));
            confirm.transform.SetParent(body, false);
            StretchFull((RectTransform)confirm.transform);
            var shade = CreateImage(confirm.transform, "Dim", new Color(0f, 0f, 0f, 0.45f));
            StretchBleed(shade.rectTransform);      // takes the taps meant for the list
            var small = MenuImage(confirm.transform, "Card", "panel", new Vector2(0f, 20f), new Vector2(720f, 340f), Image.Type.Tiled);
            var question = MenuLabel(small.transform, "Question", null, 32, new Vector2(0f, 60f), new Vector2(640f, 130f), HudInk.OnPanel, bold: true);
            question.horizontalOverflow = HorizontalWrapMode.Wrap;
            var no = MenuTextButton(small.transform, "No", "plaque", "#ui_cancel", HudInk.OnMenuPlaque, new Vector2(-160f, -90f), new Vector2(280f, 100f), 34);
            var yes = MenuTextButton(small.transform, "Yes", "button", "#load_delete", HudInk.OnButton, new Vector2(160f, -90f), new Vector2(280f, 100f), 34);
            so.FindProperty("deleteConfirm").objectReferenceValue = confirm;
            so.FindProperty("deleteQuestion").objectReferenceValue = question;
            so.FindProperty("deleteYes").objectReferenceValue = yes;
            so.FindProperty("deleteNo").objectReferenceValue = no;
            confirm.SetActive(false);
            so.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(load.onClick, loadPanel.ConfirmLoad);
            UnityEventTools.AddPersistentListener(close.onClick, controller.CloseSaves);
            return dialog;
        }

        // ---------------------------------------------------------------- the invasion

        // The user's pick «Зал знамён» (concept B2, approved 2026-11-16): a castle hall; the theme's picture in a gilt
        // frame with its season on a brass plate and the seasons as wax seals on a shelf under it; three period
        // banners hang from a beam -- the picked one big and lit with its 5 best runs and the player's line, the
        // others their points only; the gold «В бой». The pieces and their places: Tools/ui/invasion_hall_art.py
        // (MenuArt/InvHall_*.png + InvHall_layout.json, a 1920 x 1080 frame, y down).
        [System.Serializable] private class HallRect { public string name; public float[] rect; public float[] at; }
        [System.Serializable] private class HallLayout { public HallRect[] pieces = new HallRect[0], texts = new HallRect[0], points = new HallRect[0]; }

        private static HallRect HallFind(HallRect[] list, string name) => System.Array.Find(list, r => r.name == name);

        private static void HallPlace(RectTransform rect, float[] r)
        {
            Anchor(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2((r[0] + r[2]) * 0.5f - 960f, 540f - (r[1] + r[3]) * 0.5f), new Vector2(r[2] - r[0], r[3] - r[1]));
        }

        private static Image HallPiece(Transform parent, HallLayout layout, string name)
        {
            var image = CreateImage(parent, name, Color.white);
            image.sprite = MenuArt("InvHall_" + name);
            image.raycastTarget = false;
            var r = HallFind(layout.pieces, name);
            if (r != null) HallPlace(image.rectTransform, r.rect);
            return image;
        }

        private static Text HallText(Transform parent, HallLayout layout, string name, string key, int size, Color colour, Color? outline, bool bold = true, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var text = HallLabel(parent, name, key, size, bold, align);
            text.color = colour;
            var r = HallFind(layout.texts, name);
            if (r != null) HallPlace(text.rectTransform, r.rect);
            if (outline != null)
            {
                var o = text.gameObject.AddComponent<Outline>();
                o.effectColor = outline.Value;
                o.effectDistance = new Vector2(2.5f, -2.5f);
            }
            return text;
        }

        // the hall's own words: not the skin's labels (the skin repaints those in its ink, dark on the banners)
        private static Text HallLabel(Transform parent, string name, string key, int size, bool bold, TextAnchor align)
        {
            var text = CreateText(parent, name, string.Empty, size, Vector2.zero, new Vector2(100f, 40f));
            if (key != null) Localize(text, key);
            text.font = bold ? UiTheme.Bold : UiTheme.Regular;
            text.alignment = align;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Vector2 HallPoint(HallLayout layout, string name)
        {
            var p = HallFind(layout.points, name);
            return p == null ? Vector2.zero : new Vector2(p.at[0] - 960f, 540f - p.at[1]);
        }

        /// <summary>A layer as big as the reference frame, centred: pieces placed by the layout land where drawn.</summary>
        private static RectTransform HallFrame(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            Anchor(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));
            return rect;
        }

        private static void Outlined(Text text, Color colour, Color? outline, float distance = 2.5f)
        {
            text.color = colour;
            if (outline == null) return;
            var o = text.gameObject.AddComponent<Outline>();
            o.effectColor = outline.Value;
            o.effectDistance = new Vector2(distance, -distance);
        }

        /// <summary>A number with the crystal after it, the pair laid out by its width (the game's rule: the number
        /// first), centred or right-aligned on `at`.</summary>
        private static (GameObject group, Text number) HallGemAmount(Transform parent, Vector2 at, int size, TextAnchor align, Color colour, Color? outline)
        {
            var go = new GameObject("Prize", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var pivotX = align == TextAnchor.MiddleRight ? 1f : align == TextAnchor.MiddleLeft ? 0f : 0.5f;
            Anchor((RectTransform)go.transform, new Vector2(0.5f, 0.5f), new Vector2(pivotX, 0.5f), at, new Vector2(260f, size * 1.6f));
            var row = go.AddComponent<HorizontalLayoutGroup>();
            row.childAlignment = align;
            row.spacing = 6f;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            var number = HallLabel(go.transform, "Number", null, size, true, TextAnchor.MiddleCenter);
            number.horizontalOverflow = HorizontalWrapMode.Overflow;
            Outlined(number, colour, outline);
            var gem = CreateImage(go.transform, "Crystal", Color.white);
            gem.sprite = IconSprite("Res_UpgradeCrystal");
            gem.preserveAspect = true;
            gem.raycastTarget = false;
            var le = gem.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.preferredHeight = size * 1.3f;
            return (go, number);
        }

        /// <summary>The rating medallion between the banners (approved 2026-11-17): it hangs on two chains from the
        /// beam and swings round their eyes; rays, glow, glint and sparkles alive (MedallionMotion); a tap opens the
        /// rating.</summary>
        private static void BuildMedallion(Transform screen, HallLayout layout, InvasionHubController hub)
        {
            var hang = HallFind(layout.points, "MedalPivot");
            var px = hang != null ? hang.at[0] : 960f;
            var py = hang != null ? hang.at[1] : 522f;
            var swing = HallFrame(screen, "Medallion");
            swing.pivot = new Vector2(px / 1920f, 1f - py / 1080f);
            swing.anchoredPosition = new Vector2(px - 960f, 540f - py);
            var glow = HallPiece(swing, layout, "MedalGlow");
            var rays = HallPiece(swing, layout, "MedalRays");
            HallPiece(swing, layout, "MedalChains");
            HallPiece(swing, layout, "Medal");
            var glint = HallPiece(swing, layout, "MedalGlint");
            var sparkles = HallPiece(swing, layout, "MedalSparkles");
            HallPiece(swing, layout, "MedalRibbon");
            var label = HallText(swing, layout, "MedalLabel", "#invasion_rating", 30, new Color(1f, 0.94f, 0.78f), new Color(0.17f, 0.05f, 0.27f));
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            var tap = CreateImage(swing, "Tap", new Color(1f, 1f, 1f, 0f));
            var tapBox = HallFind(layout.texts, "MedalTap");
            if (tapBox != null) HallPlace(tap.rectTransform, tapBox.rect);
            UnityEventTools.AddPersistentListener(tap.gameObject.AddComponent<Button>().onClick, hub.OpenRating);
            var motion = swing.gameObject.AddComponent<MedallionMotion>();
            var mso = new SerializedObject(motion);
            mso.FindProperty("swing").objectReferenceValue = swing;
            mso.FindProperty("rays").objectReferenceValue = rays.rectTransform;
            mso.FindProperty("glint").objectReferenceValue = glint.rectTransform;
            mso.FindProperty("glow").objectReferenceValue = glow;
            mso.FindProperty("sparkles").objectReferenceValue = sparkles;
            mso.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>«Доска славы» (approved 2026-11-17, Design/ui/InvasionRating_Window.png): its pieces from the layout,
        /// its words and numbers the game's (InvasionRatingWindow).</summary>
        private static InvasionRatingWindow BuildRatingWindow(Transform screen, HallLayout layout)
        {
            var cream = new Color(1f, 0.94f, 0.78f);
            var ink = new Color(0.35f, 0.22f, 0.09f);
            var soft = new Color(0.59f, 0.41f, 0.16f);
            var wood = new Color(0.17f, 0.09f, 0.03f);
            var root = new GameObject("RatingWindow", typeof(RectTransform));
            root.transform.SetParent(screen, false);
            StretchBleed((RectTransform)root.transform);
            var dim = CreateImage(root.transform, "Dim", new Color(0.03f, 0.015f, 0.01f, 0.84f));
            StretchBleed(dim.rectTransform);
            dim.raycastTarget = true;
            var w = HallFrame(root.transform, "Window");
            var window = root.AddComponent<InvasionRatingWindow>();
            HallPiece(w, layout, "RateFrame");

            // the title's ribbon on the frame's top edge, its word in the band's middle
            var ribbon = HallFind(layout.pieces, "Ribbon");
            var at = HallPoint(layout, "RateRibbon");
            if (ribbon != null)
            {
                var img = CreateImage(w, "Ribbon", Color.white);
                img.sprite = MenuArt("InvHall_Ribbon");
                img.raycastTarget = false;
                var size = new Vector2(ribbon.rect[2] - ribbon.rect[0], ribbon.rect[3] - ribbon.rect[1]);
                Anchor(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), at, size);
                // the band is 22..106 of the drawing: its middle is (64 - the piece's middle) below the piece's
                var bandOffset = (ribbon.rect[1] + ribbon.rect[3]) * 0.5f - 64f;
                var title = HallLabel(w, "Title", "#rating_title", 50, true, TextAnchor.MiddleCenter);
                Outlined(title, cream, new Color(0.17f, 0.05f, 0.27f));
                Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), at + new Vector2(0f, bandOffset), new Vector2(560f, 70f));
            }

            // the tabs: the picked one parchment and joined to the table, the other wood
            var tabsOn = new Object[2];
            var tabsOff = new Object[2];
            var tabKeys = new[] { "#invasion_day", "#invasion_week" };
            for (var i = 0; i < 2; i++)
            {
                var on = HallPiece(w, layout, $"RateTab{i}On");
                var off = HallPiece(w, layout, $"RateTab{i}Off");
                var box = HallFind(layout.texts, $"RateTab{i}");
                foreach (var (piece, picked) in new[] { (on, true), (off, false) })
                {
                    var t = HallLabel(w, "Label", tabKeys[i], picked ? 34 : 32, true, TextAnchor.MiddleCenter);
                    Outlined(t, picked ? ink : cream, picked ? (Color?)null : wood);
                    if (box != null) HallPlace(t.rectTransform, box.rect);
                    t.transform.SetParent(piece.transform, true);
                }
                tabsOn[i] = on.gameObject;
                tabsOff[i] = off.gameObject;
                var tap = CreateImage(w, $"Tab{i}Tap", new Color(1f, 1f, 1f, 0f));
                if (box != null) HallPlace(tap.rectTransform, box.rect);
                UnityEventTools.AddIntPersistentListener(tap.gameObject.AddComponent<Button>().onClick, window.SelectTab, i);
            }
            var timer = HallText(w, layout, "RateTimer", null, 28, cream, wood, bold: false, align: TextAnchor.MiddleRight);

            // the podium
            HallPiece(w, layout, "RatePodium");
            var podium = new List<(Text place, Text name, Text points, Text prize, GameObject group)>();
            for (var k = 0; k < 3; k++)
            {
                var number = HallText(w, layout, $"RatePlace{k}Number", null, 46, cream, ink);
                number.text = (k + 1).ToString();
                var name = HallText(w, layout, $"RatePlace{k}Name", null, 26, ink, null);
                var points = HallText(w, layout, $"RatePlace{k}Points", null, 24, soft, null, bold: false);
                var (group, prize) = HallGemAmount(w, HallPoint(layout, $"RatePlace{k}Prize"), 26, TextAnchor.MiddleCenter, cream, ink);
                podium.Add((number, name, points, prize, group));
            }

            // the list from the 4th place, scrolling, and the notice while there is no board
            var listBox = HallFind(layout.texts, "RateList");
            var viewport = new GameObject("List", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewport.transform.SetParent(w, false);
            var vimg = viewport.GetComponent<Image>();
            vimg.color = new Color(1f, 1f, 1f, 0f);
            if (listBox != null) HallPlace((RectTransform)viewport.transform, listBox.rect);
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var crect = (RectTransform)content.transform;
            crect.anchorMin = new Vector2(0f, 1f);
            crect.anchorMax = new Vector2(1f, 1f);
            crect.pivot = new Vector2(0.5f, 1f);
            crect.anchoredPosition = Vector2.zero;
            crect.sizeDelta = Vector2.zero;
            var column = content.AddComponent<VerticalLayoutGroup>();
            column.spacing = 6f;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.AddComponent<ScrollRect>();
            scroll.content = crect;
            scroll.viewport = (RectTransform)viewport.transform;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var rowA = MenuArt("InvHall_RateRowA");
            var rowB = MenuArt("InvHall_RateRowB");
            var template = CreateImage(content.transform, "RowTemplate", Color.white);
            template.sprite = rowA;
            template.raycastTarget = false;
            template.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
            void RowText(string name, Vector2 anchor, Vector2 pivot, Vector2 pos, float width, TextAnchor align, Color colour, bool bold)
            {
                var t = HallLabel(template.transform, name, null, 26, bold, align);
                t.color = colour;
                Anchor(t.rectTransform, anchor, pivot, pos, new Vector2(width, 40f));
            }
            RowText("Place", new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40f, 0f), 70f, TextAnchor.MiddleCenter, ink, true);
            RowText("Name", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(90f, 0f), 420f, TextAnchor.MiddleLeft, ink, true);
            RowText("Points", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-190f, 0f), 200f, TextAnchor.MiddleRight, ink, true);
            var (rowPrize, _) = HallGemAmount(template.transform, Vector2.zero, 26, TextAnchor.MiddleRight, cream, ink);
            var rp = (RectTransform)rowPrize.transform;
            rp.anchorMin = rp.anchorMax = new Vector2(1f, 0.5f);
            rp.anchoredPosition = new Vector2(-20f, 0f);
            template.gameObject.SetActive(false);
            var empty = HallLabel(w, "Empty", null, 26, false, TextAnchor.MiddleCenter);
            empty.color = soft;
            if (listBox != null) HallPlace(empty.rectTransform, new[] { listBox.rect[0], listBox.rect[1], listBox.rect[2], listBox.rect[3] });

            // the player's own line
            HallPiece(w, layout, "RateYou");
            var youPlace = HallText(w, layout, "RateYouPlace", null, 26, ink, null);
            var youName = HallText(w, layout, "RateYouName", "#invasion_you", 28, ink, null, align: TextAnchor.MiddleLeft);
            var youPoints = HallText(w, layout, "RateYouPoints", null, 26, ink, null, align: TextAnchor.MiddleRight);
            var (youGroup, youPrize) = HallGemAmount(w, HallPoint(layout, "RateYouPrize"), 26, TextAnchor.MiddleRight, cream, ink);

            // the rewards by place
            HallPiece(w, layout, "RateRewards");
            var rewardsTitle = HallText(w, layout, "RateRewardsTitle", null, 32, cream, wood);
            var tierWho = new Object[6];
            var tierPrize = new Object[6];
            for (var k = 0; k < 6; k++)
            {
                var tier = HallFrame(w, $"Tier{k}");
                tierWho[k] = HallText(tier, layout, $"RateTier{k}Who", null, 28, cream, wood, align: TextAnchor.MiddleLeft);
                tierPrize[k] = HallGemAmount(tier, HallPoint(layout, $"RateTier{k}Prize"), 28, TextAnchor.MiddleRight, cream, wood).number;
            }
            var note = HallText(w, layout, "RateNote", null, 21, cream, wood, bold: false);

            var close = MenuRoundButton(w, "Close", "round", "Cancel", HudInk.OnRound, 84f, 0.5f);
            Anchor((RectTransform)close.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), HallPoint(layout, "RateClose"), new Vector2(84f, 84f));
            UnityEventTools.AddPersistentListener(close.onClick, window.Close);

            var so = new SerializedObject(window);
            WriteArray(so.FindProperty("tabOn"), tabsOn);
            WriteArray(so.FindProperty("tabOff"), tabsOff);
            so.FindProperty("timerText").objectReferenceValue = timer;
            var lines = so.FindProperty("podium");
            lines.arraySize = podium.Count;
            for (var k = 0; k < podium.Count; k++)
                WriteLine(lines.GetArrayElementAtIndex(k), podium[k].place, podium[k].name, podium[k].points, podium[k].prize, podium[k].group);
            so.FindProperty("listContent").objectReferenceValue = crect;
            so.FindProperty("rowTemplate").objectReferenceValue = template.gameObject;
            WriteArray(so.FindProperty("rowSprites"), new Object[] { rowA, rowB });
            so.FindProperty("emptyText").objectReferenceValue = empty;
            WriteLine(so.FindProperty("you"), youPlace, youName, youPoints, youPrize, youGroup);
            so.FindProperty("rewardsTitle").objectReferenceValue = rewardsTitle;
            WriteArray(so.FindProperty("tierWho"), tierWho);
            WriteArray(so.FindProperty("tierPrize"), tierPrize);
            so.FindProperty("noteText").objectReferenceValue = note;
            so.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(false);
            return window;
        }

        private static void WriteLine(SerializedProperty line, Text place, Text name, Text points, Text prize, GameObject group)
        {
            line.FindPropertyRelative("place").objectReferenceValue = place;
            line.FindPropertyRelative("name").objectReferenceValue = name;
            line.FindPropertyRelative("points").objectReferenceValue = points;
            line.FindPropertyRelative("prize").objectReferenceValue = prize;
            line.FindPropertyRelative("prizeGroup").objectReferenceValue = group;
        }

        private static CanvasGroup BuildInvasionScreen(Transform canvas, MainMenuController controller, out Button settings)
        {
            var screen = StretchedChild(canvas, "InvasionScreen");
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>($"{UiTextureImport.MenuArtFolder}/InvHall_layout.json");
            var layout = json != null ? JsonUtility.FromJson<HallLayout>(json.text) : new HallLayout();
            if (json == null) Debug.LogWarning("[MainMenu] no InvHall_layout.json -- run Tools/ui/invasion_hall_art.py");

            // behind it all mission 2's throne room, live, drifting by (the user, 2026-11-17): drawn small and
            // stretched (its blur), dimmed by the tint and the dark edges -- InvasionHallBackdrop
            var hall = new GameObject("Hall", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            hall.transform.SetParent(screen, false);
            StretchBleed(hall.rectTransform);
            hall.color = new Color(0.24f, 0.21f, 0.19f);
            hall.raycastTarget = true;     // nothing behind takes a tap
            var edges = CreateImage(screen, "Vignette", Color.white);
            edges.sprite = MenuArt("InvHall_Vignette");
            edges.raycastTarget = false;
            StretchBleed(edges.rectTransform);
            var backdrop = screen.gameObject.AddComponent<InvasionHallBackdrop>();
            var bso = new SerializedObject(backdrop);
            bso.FindProperty("hallSet").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Cinematic/M2ThroneSet.fbx");
            bso.FindProperty("target").objectReferenceValue = hall;
            var menuCameraGO = GameObject.Find("Menu Camera");
            if (menuCameraGO != null) bso.FindProperty("menuCamera").objectReferenceValue = menuCameraGO.GetComponent<Camera>();
            bso.ApplyModifiedPropertiesWithoutUndo();
            // the beam runs past both edges, behind the seals
            HallPiece(screen, layout, "Beam");

            var hub = screen.gameObject.AddComponent<InvasionHubController>();
            var cream = new Color(1f, 0.94f, 0.78f);

            // the theme: the picture under the gilt frame, the season's plate, the seals' shelf and the seals
            var picture = CreateImage(screen, "Picture", new Color(0.3f, 0.45f, 0.25f));
            picture.raycastTarget = false;
            var hole = HallFind(layout.texts, "Picture");
            if (hole != null) HallPlace(picture.rectTransform, hole.rect);
            var firstPicture = MenuArt("Inv_summer");
            if (firstPicture != null) { picture.sprite = firstPicture; picture.color = Color.white; }
            HallPiece(screen, layout, "Frame");
            HallPiece(screen, layout, "Plate");
            var season = HallText(screen, layout, "Season", null, 32, new Color(0.27f, 0.16f, 0.04f), null);
            HallPiece(screen, layout, "Shelf");
            HallPiece(screen, layout, "Ribbon");
            HallText(screen, layout, "Title", "#mode_invasion", 58, cream, new Color(0.19f, 0.05f, 0.29f));

            var row = new GameObject("Themes", typeof(RectTransform));
            row.transform.SetParent(screen, false);
            Anchor((RectTransform)row.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));
            var themes = row.AddComponent<ThemePicker>();
            var seal = HallFind(layout.pieces, "Seal");
            var sealPicked = HallFind(layout.pieces, "SealPicked");
            var buttons = new Object[MapThemes.All.Length];
            var picked = new Object[MapThemes.All.Length];
            for (var i = 0; i < MapThemes.All.Length; i++)
            {
                var id = MapThemes.Id(MapThemes.All[i]);
                var at = HallPoint(layout, $"Seal{i}");
                var wax = CreateImage(row.transform, $"Theme_{id}", Color.white);
                wax.sprite = MenuArt("InvHall_Seal");
                if (seal != null)
                    Anchor(wax.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        at + new Vector2((seal.rect[0] + seal.rect[2]) * 0.5f, -(seal.rect[1] + seal.rect[3]) * 0.5f),
                        new Vector2(seal.rect[2] - seal.rect[0], seal.rect[3] - seal.rect[1]));
                buttons[i] = wax.gameObject.AddComponent<Button>();
                var sign = MenuGlyph(wax.transform, $"Theme_{id}", HudInk.Keep, 62f);
                sign.color = new Color(0.93f, 0.67f, 0.55f);
                sign.rectTransform.anchoredPosition = Vector2.zero;
                var lit = CreateImage(wax.transform, "Picked", Color.white);
                lit.sprite = MenuArt("InvHall_SealPicked");
                lit.raycastTarget = false;
                if (sealPicked != null && seal != null)
                    Anchor(lit.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2((sealPicked.rect[0] + sealPicked.rect[2]) * 0.5f - (seal.rect[0] + seal.rect[2]) * 0.5f,
                            -((sealPicked.rect[1] + sealPicked.rect[3]) * 0.5f - (seal.rect[1] + seal.rect[3]) * 0.5f)),
                        new Vector2(sealPicked.rect[2] - sealPicked.rect[0], sealPicked.rect[3] - sealPicked.rect[1]));
                var litSign = MenuGlyph(lit.transform, $"Theme_{id}", HudInk.Keep, 74f);
                litSign.color = new Color(1f, 0.96f, 0.84f);
                litSign.rectTransform.anchoredPosition = Vector2.zero;
                lit.gameObject.SetActive(false);
                picked[i] = lit.gameObject;
            }
            var tso = new SerializedObject(themes);
            tso.FindProperty("invasion").boolValue = true;
            WriteArray(tso.FindProperty("buttons"), buttons);
            WriteArray(tso.FindProperty("picked"), picked);
            tso.ApplyModifiedPropertiesWithoutUndo();

            // the three period banners, on chains from the beam
            var periods = new[] { "#invasion_day", "#invasion_week", "#invasion_month" };
            // day and week only: the month went with the move to Google Play Games (the user, 2026-11-17)
            const int bannerCount = 2;
            var darks = new[] { new Color(0.35f, 0.06f, 0.04f), new Color(0.06f, 0.12f, 0.31f) };
            var bigs = new Object[bannerCount];
            var smalls = new Object[bannerCount];
            var glows = new Object[bannerCount];
            var details = new Object[bannerCount];
            var hints = new Object[bannerCount];
            var points = new Object[bannerCount];
            var places = new Object[bannerCount];
            var runs = new Object[bannerCount * 5];
            for (var i = 0; i < bannerCount; i++)
            {
                var banner = new GameObject($"Banner{i}", typeof(RectTransform));
                banner.transform.SetParent(screen, false);
                Anchor((RectTransform)banner.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));
                var b = banner.transform;
                glows[i] = HallPiece(b, layout, $"Banner{i}_Glow").gameObject;
                bigs[i] = HallPiece(b, layout, $"Banner{i}_Big").gameObject;
                smalls[i] = HallPiece(b, layout, $"Banner{i}_Small").gameObject;
                HallText(b, layout, $"Title{i}", periods[i], 40, cream, darks[i]);
                var trophy = MenuGlyph(b, "Trophy", HudInk.Keep, 56f);
                trophy.color = new Color(0.98f, 0.81f, 0.31f);
                trophy.rectTransform.anchoredPosition = HallPoint(layout, $"Trophy{i}");
                points[i] = HallText(b, layout, $"Points{i}", null, 50, cream, darks[i], align: TextAnchor.MiddleLeft);
                hints[i] = HallText(b, layout, $"Hint{i}", "#invasion_banner_open", 22, cream, darks[i], bold: false).gameObject;
                var group = new GameObject("Details", typeof(RectTransform));
                group.transform.SetParent(b, false);
                Anchor((RectTransform)group.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));
                details[i] = group;
                HallPiece(group.transform, layout, $"Banner{i}_Rows");
                for (var k = 0; k < 5; k++)
                {
                    var rank = HallText(group.transform, layout, $"Rank{i}_{k}", null, 24, cream, darks[i]);
                    rank.text = (k + 1).ToString();
                    runs[i * 5 + k] = HallText(group.transform, layout, $"Run{i}_{k}", null, 26, cream, darks[i], align: TextAnchor.MiddleRight);
                }
                var you = CreateImage(group.transform, "YouRow", Color.white);
                you.sprite = MenuArt("InvHall_YouRow");
                you.raycastTarget = false;
                var youBox = HallFind(layout.texts, $"You{i}");
                if (youBox != null) HallPlace(you.rectTransform, new[] { youBox.rect[0] - 16f, youBox.rect[1] - 2f, youBox.rect[2] + 16f, youBox.rect[3] + 2f });
                var ink = new Color(0.35f, 0.22f, 0.09f);
                HallText(group.transform, layout, $"You{i}", "#invasion_you", 26, ink, null, align: TextAnchor.MiddleLeft);
                // the place: the controller's, from the rating («место — скоро» while it is not known)
                var soon = HallLabel(group.transform, "Place", null, 22, false, TextAnchor.MiddleRight);
                soon.color = new Color(0.59f, 0.41f, 0.16f);
                if (youBox != null) HallPlace(soon.rectTransform, youBox.rect);
                places[i] = soon;
                // the whole banner takes the tap: it picks its period
                var tap = CreateImage(b, "Tap", new Color(1f, 1f, 1f, 0f));
                var tapBox = HallFind(layout.texts, $"Tap{i}");
                if (tapBox != null) HallPlace(tap.rectTransform, tapBox.rect);
                UnityEventTools.AddIntPersistentListener(tap.gameObject.AddComponent<Button>().onClick, hub.SelectPeriod, i);
            }

            BuildMedallion(screen, layout, hub);

            // «В бой», the crystals, «?», the corners, the King and the Queen at the sides
            var buttonBox = HallFind(layout.texts, "Button");
            var fightSize = buttonBox != null ? new Vector2(buttonBox.rect[2] - buttonBox.rect[0], buttonBox.rect[3] - buttonBox.rect[1]) : new Vector2(400f, 82f);
            var fightAt = buttonBox != null ? new Vector2((buttonBox.rect[0] + buttonBox.rect[2]) * 0.5f - 960f, 540f - (buttonBox.rect[1] + buttonBox.rect[3]) * 0.5f) : new Vector2(0f, -480f);
            // «В бой»: crossed swords at both ends, a glow behind, the whole of it beating (the user, 2026-11-17:
            // «добавь мечи + эффект пульсации, чтобы игроку хотелось нажать»)
            var fightGlow = CreateImage(screen, "RunGlow", new Color(1f, 0.82f, 0.4f, 0.4f));
            fightGlow.sprite = MenuArt("InvHall_ButtonGlow");
            fightGlow.raycastTarget = false;
            Anchor(fightGlow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), fightAt, new Vector2(fightSize.x + 160f, 230f));
            var fight = MenuTextButton(screen, "RunButton", "button", "#invasion_fight", HudInk.OnButton, fightAt, new Vector2(fightSize.x, 96f), 52);
            UnityEventTools.AddPersistentListener(fight.onClick, controller.StartInvasion);
            var swords = AssetDatabase.LoadAssetAtPath<Sprite>($"{UiTextureImport.GlyphsFolder}/Swords.png");
            foreach (var side in new[] { -1f, 1f })
            {
                if (swords == null) break;
                var sword = CreateImage(fight.transform, side < 0 ? "SwordsLeft" : "SwordsRight", new Color(0.33f, 0.19f, 0.07f));
                sword.sprite = swords;
                sword.preserveAspect = true;
                sword.raycastTarget = false;
                Anchor(sword.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(side * (fightSize.x * 0.5f - 82f), 4f), new Vector2(66f, 66f));
                sword.rectTransform.localScale = new Vector3(side, 1f, 1f);
            }
            var pulse = fight.gameObject.AddComponent<ButtonPulse>();
            var pso = new SerializedObject(pulse);
            pso.FindProperty("glow").objectReferenceValue = fightGlow;
            pso.ApplyModifiedPropertiesWithoutUndo();
            foreach (var (hero, anchor, x) in new[] { ("King", 0f, 185f), ("Queen", 1f, -185f) })
            {
                var art = MenuArt($"Peek_{hero}");
                if (art == null) continue;
                var figure = CreateImage(screen, hero, Color.white);
                figure.sprite = art;
                figure.preserveAspect = true;
                figure.raycastTarget = false;
                Anchor(figure.rectTransform, new Vector2(anchor, 0f), new Vector2(0.5f, 0f), new Vector2(x, -20f), new Vector2(560f * art.rect.width / art.rect.height, 560f));
            }
            // the crystals as the game shows them: the number, the crystal after it (the user, 2026-11-17)
            // no plate behind it (the user, the same day): the number and the crystal, left of the settings
            var crystals = new GameObject("Crystals", typeof(RectTransform));
            crystals.transform.SetParent(screen, false);
            Anchor((RectTransform)crystals.transform, new Vector2(1f, 1f), new Vector2(1f, 0.5f),
                new Vector2(-(CornerInset + CornerButton * 0.5f + 24f), -CornerInset), new Vector2(240f, 80f));
            var crystalsText = HallLabel(crystals.transform, "Number", null, 46, true, TextAnchor.MiddleRight);
            crystalsText.color = cream;
            Anchor(crystalsText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-70f, 2f), new Vector2(170f, 64f));
            var rim = crystalsText.gameObject.AddComponent<Outline>();
            rim.effectColor = new Color(0.16f, 0.08f, 0.03f);
            rim.effectDistance = new Vector2(2.5f, -2.5f);
            Gem(crystals.transform, new Vector2(85f, 2f), 62f);
            // «!» under the settings, as big (the user, the same day)
            var info = Corner(screen, "InfoButton", "Info", new Vector2(1f, 1f), new Vector2(-CornerInset, -CornerInset - CornerButton - 20f), CornerButton);
            UnityEventTools.AddIntPersistentListener(info.onClick, controller.ShowInfo, (int)GameMode.Invasion);
            var backButton = Corner(screen, "BackButton", "Back", new Vector2(0f, 1f), new Vector2(CornerInset, -CornerInset), CornerButton);
            settings = Corner(screen, "SettingsButton", "Settings", new Vector2(1f, 1f), new Vector2(-CornerInset, -CornerInset), CornerButton);
            UnityEventTools.AddPersistentListener(backButton.onClick, controller.Back);
            var rating = BuildRatingWindow(screen, layout);
            // all that stands in front of the hall, in one group a window over the screen hides
            var content = StretchedChild(screen, "Content");
            content.SetSiblingIndex(2);
            var keep = new HashSet<Transform> { hall.transform, edges.transform, rating.transform, content };
            var front = new List<Transform>();
            foreach (Transform child in screen) if (!keep.Contains(child)) front.Add(child);
            foreach (var child in front) child.SetParent(content, false);
            var contentGroup = content.gameObject.AddComponent<CanvasGroup>();
            var rso = new SerializedObject(rating);
            rso.FindProperty("hub").objectReferenceValue = hub;
            rso.ApplyModifiedPropertiesWithoutUndo();

            var so = new SerializedObject(hub);
            so.FindProperty("themes").objectReferenceValue = themes;
            so.FindProperty("picture").objectReferenceValue = picture;
            so.FindProperty("seasonText").objectReferenceValue = season;
            var themeIds = new List<string>();
            var sprites = new List<Object>();
            foreach (var theme in MapThemes.All) AddArt(themeIds, sprites, MapThemes.Id(theme), $"Inv_{MapThemes.Id(theme)}");
            WriteStrings(so.FindProperty("pictureThemes"), themeIds);
            WriteArray(so.FindProperty("pictureSprites"), sprites.ToArray());
            WriteArray(so.FindProperty("bannerBig"), bigs);
            WriteArray(so.FindProperty("bannerSmall"), smalls);
            WriteArray(so.FindProperty("bannerGlow"), glows);
            WriteArray(so.FindProperty("bannerDetails"), details);
            WriteArray(so.FindProperty("bannerHint"), hints);
            WriteArray(so.FindProperty("bannerPoints"), points);
            WriteArray(so.FindProperty("runTexts"), runs);
            WriteArray(so.FindProperty("bannerPlace"), places);
            so.FindProperty("ratingWindow").objectReferenceValue = rating;
            so.FindProperty("content").objectReferenceValue = contentGroup;
            so.FindProperty("crystalsText").objectReferenceValue = crystalsText;
            so.ApplyModifiedPropertiesWithoutUndo();

            var group2 = Group(screen.gameObject);
            group2.alpha = 0f;
            bso.FindProperty("screen").objectReferenceValue = group2;
            bso.ApplyModifiedPropertiesWithoutUndo();
            return group2;
        }

        /// <summary>
        /// The modes screen's banners and «Играть» (ModeBannersController). Each banner hangs from
        /// its top (the pivot), so it unrolls downward and sways from its perch; the name sits on the
        /// kit's parchment plaque, the motto and the live line in light ink on the cloth.
        /// </summary>
        private static void BuildModeBanners(RectTransform modes, MainMenuController menu)
        {
            // indices = ModeBannersController's mode constants; the row's order is the controller's (v2, the user 2026-11-16)
            var names = new[] { "Tutorial", "Sandbox", "Invasion", "Campaign", "Online", "Workshop" };
            var keys = new[] { "#mode_tutorial", "#mode_sandbox", "#mode_invasion", "#mode_campaign", "#mode_online", "#mode_workshop" };
            // a swipe anywhere across the banners' band scrolls the row (behind the banners; the corners' buttons stay free)
            var swipe = CreateImage(modes, "SwipeArea", new Color(0f, 0f, 0f, 0f));
            var swipeRect = swipe.rectTransform;
            swipeRect.anchorMin = new Vector2(0f, 0f);
            swipeRect.anchorMax = new Vector2(1f, 1f);
            swipeRect.offsetMin = new Vector2(0f, 300f);
            swipeRect.offsetMax = new Vector2(0f, -170f);
            var hall = StretchedChild(modes, "Banners");
            var banners = new Object[names.Length];
            var cloths = new Object[names.Length];
            var lines = new Object[names.Length];
            var bannerControl = modes.gameObject.AddComponent<ModeBannersController>();
            Image rewardIcon = null;
            Text rewardAmount = null;
            // a banner sprite is 380 x 660; on screen 300 wide
            const float width = 300f;
            const float k = width / 380f;
            var size = new Vector2(width, 660f * k);
            for (var i = 0; i < names.Length; i++)
            {
                var cloth = CreateImage(hall, $"{names[i]}Banner", Color.white);
                cloth.sprite = MenuArt($"Banner_{names[i]}");
                cloth.preserveAspect = true;
                var rect = cloth.rectTransform;
                Anchor(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, 380f), size);   // ModeBannersController lays the row out
                var button = cloth.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                UnityEventTools.AddIntPersistentListener(button.onClick, bannerControl.TapBanner, i);
                var press = cloth.gameObject.AddComponent<LongPress>();
                UnityEventTools.AddIntPersistentListener(press.onLongPress, bannerControl.HoldBanner, i);

                // the name on a small plaque (the user, 2026-11-10: the first ones were too big)
                var plaque = MenuImage(cloth.transform, "NamePlaque", "plaque", Vector2.zero, new Vector2(196f, 54f));
                Anchor(plaque.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -318f * k), new Vector2(196f, 54f));
                plaque.raycastTarget = false;
                MenuLabel(plaque.transform, "Label", keys[i], 26, new Vector2(0f, 2f), new Vector2(180f, 46f), HudInk.OnMenuPlaque, bold: true);

                // the live line, shrunk to fit inside the stitched seam whatever the number
                var line = BannerLine(cloth.transform, "Line", -392f * k, 26, true);
                if (i == 0)
                {
                    // the tutorial's reward (the user, 2026-11-10): «Награда:» centred, under it «25» and the
                    // upgrade crystal, centred as a pair (ModeBannersController.ShowReward places them)
                    var lineRect = line.rectTransform;
                    lineRect.anchoredPosition = new Vector2(0f, -392f * k + 16f);
                    var amount = BannerLine(cloth.transform, "RewardAmount", -392f * k - 22f, 26, true);
                    amount.rectTransform.sizeDelta = new Vector2(120f, 40f);
                    amount.horizontalOverflow = HorizontalWrapMode.Overflow;
                    amount.resizeTextForBestFit = false;
                    rewardAmount = amount;
                    var crystal = CreateImage(cloth.transform, "RewardCrystal", Color.white);
                    crystal.sprite = IconSprite("Res_UpgradeCrystal");
                    crystal.preserveAspect = true;
                    crystal.raycastTarget = false;
                    Anchor(crystal.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(30f, -392f * k - 22f), new Vector2(36f, 36f));
                    rewardIcon = crystal;
                }
                banners[i] = rect;
                cloths[i] = cloth;
                lines[i] = line;
            }

            var play = MenuTextButton(modes, "PlayButton", "button", "#newgame_play", HudInk.OnButton, Vector2.zero, new Vector2(420f, 120f), 48);
            Anchor((RectTransform)play.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(420f, 120f));
            UnityEventTools.AddPersistentListener(play.onClick, bannerControl.Play);
            // the picked mode's motto, bottom centre over «Играть» (the user, 2026-11-10)
            var motto = BannerLine(modes, "Motto", 0f, 32, true);
            Anchor(motto.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 205f), new Vector2(760f, 52f));
            motto.resizeTextForBestFit = false;

            // the row's scroll chevrons at the screen's edges, at the banners' mid height: a gold chevron (variant A,
            // the user 2026-11-16 -- not like the round «Назад»), a twin and a glow for the nudge hint; the right one mirrored
            var arrowRoots = new Object[2];
            var chevronRects = new Object[2];
            var twinImages = new Object[2];
            var glowImages = new Object[2];
            for (var a = 0; a < 2; a++)
            {
                var root = CreateImage(modes, a == 0 ? "ArrowLeft" : "ArrowRight", new Color(0f, 0f, 0f, 0f));
                Anchor(root.rectTransform, new Vector2(a, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(a == 0 ? 80f : -80f, 100f), new Vector2(130f, 170f));
                if (a == 1) root.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
                var arrowButton = root.gameObject.AddComponent<Button>();
                arrowButton.transition = Selectable.Transition.None;
                UnityEventTools.AddPersistentListener(arrowButton.onClick, a == 0 ? bannerControl.ScrollLeft : bannerControl.ScrollRight);
                var halo = CreateImage(root.transform, "Glow", new Color(1f, 0.84f, 0.43f, 1f));
                halo.sprite = MenuArt("Glow");
                halo.raycastTarget = false;
                Anchor(halo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f));
                var twin = CreateImage(root.transform, "Twin", Color.white);
                twin.sprite = MenuArt("Chevron");
                twin.raycastTarget = false;
                Anchor(twin.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80f, 120f));
                var chevron = CreateImage(root.transform, "Chevron", Color.white);
                chevron.sprite = MenuArt("Chevron");
                chevron.raycastTarget = false;
                Anchor(chevron.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80f, 120f));
                arrowRoots[a] = root.rectTransform;
                chevronRects[a] = chevron.rectTransform;
                twinImages[a] = twin;
                glowImages[a] = halo;
            }

            var so = new SerializedObject(bannerControl);
            so.FindProperty("menu").objectReferenceValue = menu;
            WriteArray(so.FindProperty("arrows"), arrowRoots);
            WriteArray(so.FindProperty("chevrons"), chevronRects);
            WriteArray(so.FindProperty("twins"), twinImages);
            WriteArray(so.FindProperty("glows"), glowImages);
            so.FindProperty("screen").objectReferenceValue = Group(modes.gameObject);
            so.FindProperty("playButton").objectReferenceValue = play.GetComponent<Image>();
            WriteArray(so.FindProperty("banners"), banners);
            WriteArray(so.FindProperty("cloths"), cloths);
            so.FindProperty("motto").objectReferenceValue = motto;
            so.FindProperty("rewardIcon").objectReferenceValue = rewardIcon;
            so.FindProperty("rewardAmount").objectReferenceValue = rewardAmount;
            WriteArray(so.FindProperty("lines"), lines);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>A line on a banner's cloth: light ink with a dark edge, so it reads on every cloth colour.</summary>
        private static Text BannerLine(Transform banner, string name, float y, int size, bool bold)
        {
            // 190 wide: inside the stitched seam (268 sprite px x 300/380 = 211, less a margin)
            var text = CreateText(banner, name, string.Empty, size, Vector2.zero, new Vector2(190f, 40f));
            Anchor(text.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(190f, 40f));
            text.font = bold ? UiTheme.Bold : UiTheme.Regular;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            // one line, shrunk rather than spilling over the seam (three-digit points, many saves)
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = size;
            text.color = new Color(1f, 0.96f, 0.86f);
            text.raycastTarget = false;
            var edge = text.gameObject.AddComponent<Outline>();
            edge.effectColor = new Color(0.18f, 0.1f, 0.05f, 0.85f);
            edge.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        /// <summary>A number on a parchment capsule with a disc on its left -- the HUD's resource chip, as the user picked for the points and the crystals.</summary>
        private static Text Counter(Transform parent, string name, Vector2 position, Color disc, string glyph, Sprite picture, bool discRight = false)
        {
            var plaque = MenuImage(parent, name, "plaque", position, new Vector2(230f, 84f));
            plaque.raycastTarget = false;
            var round = CreateImage(plaque.transform, "Disc", disc);
            round.sprite = SkinSprite("circle_mask");
            round.raycastTarget = false;
            // the crystals: the disc on the RIGHT, the number before it (the user, 2026-11-11: the rule for the whole game)
            Anchor(round.rectTransform, new Vector2(discRight ? 1f : 0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(discRight ? -40f : 40f, 3f), new Vector2(62f, 62f));
            var rim = round.gameObject.AddComponent<Outline>();
            rim.effectColor = new Color(0.35f, 0.2f, 0.09f);
            rim.effectDistance = new Vector2(3f, -3f);
            if (glyph != null)
            {
                var sign = MenuGlyph(round.transform, glyph, HudInk.Keep, 40f);
                sign.color = new Color(0.25f, 0.13f, 0.06f);
                sign.rectTransform.anchoredPosition = Vector2.zero;
                Object.DestroyImmediate(sign.GetComponent<Shadow>());
            }
            if (picture != null)
            {
                var gem = CreateImage(round.transform, "Picture", Color.white);
                gem.sprite = picture;
                gem.preserveAspect = true;
                gem.raycastTarget = false;
                Anchor(gem.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 46f));
            }
            return MenuLabel(plaque.transform, "Number", null, 42, new Vector2(discRight ? -38f : 38f, 3f), new Vector2(130f, 60f), HudInk.OnMenuPlaque, bold: true,
                             align: discRight ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft);
        }

        /// <summary>The crystal, the game's one currency: the sandbox's icon everywhere (the user, 2026-11-11).</summary>
        private static void Gem(Transform parent, Vector2 position, float size)
        {
            var art = IconSprite("Res_UpgradeCrystal");
            if (art == null) return;
            var gem = CreateImage(parent, "Crystal", Color.white);
            gem.sprite = art;
            gem.preserveAspect = true;
            gem.raycastTarget = false;
            Anchor(gem.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(size, size));
        }

        /// <summary>A hero leaning in at a side of the invasion's picture (Tools/blender/render_peek.py), standing on its foot.</summary>
        private static void Peek(Transform frame, string hero, Vector2 position, Vector2 pivot)
        {
            var art = MenuArt($"Peek_{hero}");
            if (art == null) return;
            var image = CreateImage(frame, $"Peek_{hero}", Color.white);
            image.sprite = art;
            image.preserveAspect = true;
            image.raycastTarget = false;
            // as tall as the picture, so the crown is never cut off by its edge
            var height = ((RectTransform)frame).sizeDelta.y;
            Anchor(image.rectTransform, new Vector2(0.5f, 0.5f), pivot, position, new Vector2(height * art.rect.width / art.rect.height, height));
        }
    }
}
