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
            var settings = Corner(main, "SettingsButton", "Settings", new Vector2(0.5f, 1f), new Vector2(0f, -CornerInset), 150f);
            var play = MenuTextButton(main, "ContinueButton", "button", "#menu_continue", HudInk.OnButton, Vector2.zero, new Vector2(560f, 140f), 50);
            Anchor((RectTransform)play.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(560f, 140f));
            var offers = MenuTextButton(main, "OffersButton", "plaque", "#menu_offers", HudInk.OnMenuPlaque, Vector2.zero, new Vector2(440f, 110f), 38);
            Anchor((RectTransform)offers.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 195f), new Vector2(440f, 110f));
            shopBadges.Add(Badge(offers.transform));
            var discord = BrandCorner(main, "DiscordButton", "Discord", new Vector2(0.5f, 0f), new Vector2(-340f, 120f), CornerButton, DiscordColour);
            discordBadges.Add(Badge(discord.transform));
            var youTube = BrandCorner(main, "YouTubeButton", "YouTube", new Vector2(0.5f, 0f), new Vector2(340f, 120f), CornerButton, YouTubeColour);
            youTubeBadges.Add(Badge(youTube.transform));

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
            var sandboxScreen = BuildSandboxScreen(canvas, controller, out var sandbox, out var sandboxSettings);
            var invasionScreen = BuildInvasionScreen(canvas, controller, out var invasionSettings);

            // ---- dialogs over everything
            var settingsPanel = BuildSettingsPanel(canvas, panelSprite, settings);
            foreach (var other in new[] { modesSettings, sandboxSettings, invasionSettings }) UnityEventTools.AddPersistentListener(other.onClick, settingsPanel.OpenPanel);

            var exitDialog = BuildMenuDialog(canvas, "ExitDialog", "#quit_question", null, new Vector2(820f, 420f), out var exitCard);
            var stay = MenuTextButton(exitCard, "StayButton", "plaque", "#quit_stay", HudInk.OnMenuPlaque, new Vector2(-180f, -90f), new Vector2(320f, 120f));
            var quit = MenuTextButton(exitCard, "QuitButton", "button", "#quit_confirm", HudInk.OnButton, new Vector2(180f, -90f), new Vector2(320f, 120f));
            UnityEventTools.AddPersistentListener(stay.onClick, controller.CloseExitDialog);
            UnityEventTools.AddPersistentListener(quit.onClick, controller.QuitGame);

            var shopDialog = BuildMenuDialog(canvas, "ShopDialog", "#menu_offers", "#shop_soon", new Vector2(1000f, 640f), out var shopCard);
            for (var s = 0; s < 4; s++)
            {
                var slot = MenuImage(shopCard, $"Slot{s}", "row", new Vector2(-330f + s * 220f, -40f), new Vector2(190f, 190f));
                slot.raycastTarget = false;
                MenuGlyph(slot.transform, "Lock", HudInk.OnPanel, 70f).color = new Color(1f, 1f, 1f, 0.5f);
            }
            var closeShop = MenuTextButton(shopCard, "CloseButton", "plaque", "#ui_close", HudInk.OnMenuPlaque, new Vector2(0f, -230f), new Vector2(320f, 110f));
            UnityEventTools.AddPersistentListener(closeShop.onClick, controller.CloseShop);

            var infoDialog = BuildMenuDialog(canvas, "InfoDialog", "#mode_sandbox", null, new Vector2(1000f, 620f), out var infoCard);
            var infoTitle = infoCard.Find("TitlePlate/Title").GetComponent<Text>();
            Object.DestroyImmediate(infoTitle.GetComponent<LocalizedText>());
            var infoText = MenuLabel(infoCard, "Text", null, 32, new Vector2(0f, 40f), new Vector2(840f, 340f), HudInk.OnPanel);
            var closeInfo = MenuTextButton(infoCard, "CloseButton", "plaque", "#ui_close", HudInk.OnMenuPlaque, new Vector2(0f, -220f), new Vector2(320f, 110f));
            UnityEventTools.AddPersistentListener(closeInfo.onClick, controller.CloseInfo);

            var savesDialog = BuildSavesDialog(canvas, controller, out var loadPanel);

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
            so.FindProperty("exitDialog").objectReferenceValue = exitDialog;
            so.FindProperty("shopDialog").objectReferenceValue = shopDialog;
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

        /// <summary>The orange «!» on a button's top-right, until its place is first opened (MainMenuController).</summary>
        private static GameObject Badge(Transform target)
        {
            var badge = CreateImage(target, "Badge", BadgeColour);
            badge.sprite = SkinSprite("circle_mask");
            badge.raycastTarget = false;
            var size = ((RectTransform)target).sizeDelta;
            Anchor(badge.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(size.x * 0.5f - 14f, size.y * 0.5f - 14f), new Vector2(56f, 56f));
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

        // The user's pick of three concepts (2026-11-10, «Знамя и доска»): the sandbox banner of the
        // hall on the left, a board on the right with the themes as picture tabs along its top, the
        // map on it, and a column of the map code, Saves and Start.
        private static readonly Vector2 SandboxBoardPos = new Vector2(180f, -60f);
        private static readonly Vector2 SandboxBoardSize = new Vector2(1240f, 720f);

        private static CanvasGroup BuildSandboxScreen(Transform canvas, MainMenuController controller, out SandboxScreenController sandbox, out Button settings)
        {
            var screen = StretchedChild(canvas, "SandboxScreen");
            var back = Corner(screen, "BackButton", "Back", new Vector2(0f, 1f), new Vector2(CornerInset, -CornerInset), CornerButton);
            var info = Corner(screen, "InfoButton", "Info", new Vector2(1f, 1f), new Vector2(-CornerInset - CornerButton - 20f, -CornerInset), CornerButton);
            settings = Corner(screen, "SettingsButton", "Settings", new Vector2(1f, 1f), new Vector2(-CornerInset, -CornerInset), CornerButton);
            UnityEventTools.AddPersistentListener(back.onClick, controller.Back);
            UnityEventTools.AddIntPersistentListener(info.onClick, controller.ShowInfo, (int)GameMode.Sandbox);

            // the banner, as it hangs in the hall (a sprite is 380 x 660)
            const float bannerWidth = 340f;
            const float k = bannerWidth / 380f;
            var banner = CreateImage(screen, "SandboxBanner", Color.white);
            banner.sprite = MenuArt("Banner_Sandbox");
            banner.preserveAspect = true;
            Anchor(banner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(-700f, 330f), new Vector2(bannerWidth, 660f * k));
            // tap it and it swings like a struck cloth (the user, 2026-11-10: a small touch)
            banner.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
            banner.gameObject.AddComponent<BannerKnock>();
            var plaque = MenuImage(banner.transform, "NamePlaque", "plaque", Vector2.zero, new Vector2(220f, 60f));
            Anchor(plaque.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -318f * k), new Vector2(220f, 60f));
            plaque.raycastTarget = false;
            MenuLabel(plaque.transform, "Label", "#mode_sandbox", 28, new Vector2(0f, 2f), new Vector2(200f, 50f), HudInk.OnMenuPlaque, bold: true);

            var window = MenuImage(screen, "Window", "panel", SandboxBoardPos, SandboxBoardSize, Image.Type.Tiled).transform;
            var themes = BuildThemeTabs(screen, new Vector2(SandboxBoardPos.x, SandboxBoardPos.y + SandboxBoardSize.y * 0.5f + 44f));

            // the map card(s), left on the board
            var grid = new GameObject("Maps", typeof(RectTransform), typeof(GridLayoutGroup));
            grid.transform.SetParent(window, false);
            Anchor((RectTransform)grid.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-200f, 0f), new Vector2(760f, 580f));
            var layout = grid.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(760f, 580f);
            layout.spacing = new Vector2(30f, 30f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = Mathf.Max(1, MapSelector.PlayerMapIds.Length);
            layout.childAlignment = TextAnchor.MiddleCenter;

            var ids = MapSelector.PlayerMapIds;
            var cards = new Object[ids.Length];
            var pictures = new Object[ids.Length];
            var picked = new Object[ids.Length];
            for (var i = 0; i < ids.Length; i++)
            {
                var card = MenuImage(grid.transform, $"Map_{ids[i]}", "row", Vector2.zero, layout.cellSize);
                cards[i] = card.gameObject.AddComponent<Button>();
                var lit = MenuImage(card.transform, "Picked", "row_selected", Vector2.zero, Vector2.zero);
                StretchFull(lit.rectTransform);
                lit.raycastTarget = false;
                // one map: nothing to pick between, so no lit frame
                lit.color = ids.Length > 1 ? Color.white : Color.clear;
                picked[i] = lit.gameObject;
                var picture = CreateImage(card.transform, "Picture", new Color(0.12f, 0.08f, 0.05f));
                var plain = MenuArt($"Map_{ids[i]}");
                if (plain != null) { picture.sprite = plain; picture.color = Color.white; }
                picture.raycastTarget = false;
                picture.preserveAspect = true;
                var rect = picture.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(20f, 70f);
                rect.offsetMax = new Vector2(-20f, -20f);
                pictures[i] = picture;
                var name = MenuLabel(card.transform, "Name", MapSelectPanelController.NameKey(ids[i]), 34, Vector2.zero, new Vector2(600f, 50f), HudInk.OnPanel, bold: true);
                Anchor(name.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(600f, 50f));
            }
            var noMaps = MenuLabel(window, "NoMaps", "#sandbox_no_maps", 32, new Vector2(-200f, 0f), new Vector2(700f, 120f), HudInk.OnPanel);

            // the column: the map code (+ a random one), Saves, Start
            const float colX = 425f;
            MenuLabel(window, "MapCodeLabel", "#sandbox_code_label", 28, new Vector2(colX, 230f), new Vector2(340f, 44f), HudInk.OnPanel, bold: true);
            // the archipelago's code: empty = a random map, a friend's code = their map
            var mapCode = CreateInputField(window, "MapCode", string.Empty, new Vector2(colX - 45f, 165f), new Vector2(250f, 80f),
                AssetDatabase.LoadAssetAtPath<Sprite>($"{UiTextureImport.HudSkinsFolder}/Classic/plaque.png"));
            mapCode.contentType = InputField.ContentType.Standard;
            mapCode.characterLimit = 16;
            mapCode.textComponent.alignment = TextAnchor.MiddleCenter;
            var codePlaceholder = (Text)mapCode.placeholder;
            codePlaceholder.alignment = TextAnchor.MiddleCenter;
            Localize(codePlaceholder, "#sandbox_code_hint");
            var random = MenuRoundButton(window, "RandomCodeButton", "round", "Rotate", HudInk.OnRound, 84f, 0.5f);
            Anchor((RectTransform)random.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(colX + 130f, 165f), new Vector2(84f, 84f));
            var codeError = CreateText(window, "MapCodeError", string.Empty, 22, new Vector2(colX, 95f), new Vector2(360f, 60f), UiTheme.Danger);
            codeError.horizontalOverflow = HorizontalWrapMode.Wrap;
            codeError.gameObject.SetActive(false);
            var saves = MenuTextButton(window, "SavesButton", "plaque", "#sandbox_saves", HudInk.OnMenuPlaque, new Vector2(colX, -10f), new Vector2(340f, 104f), 34);
            UnityEventTools.AddPersistentListener(saves.onClick, controller.OpenSaves);
            var start = MenuTextButton(window, "StartButton", "button", "#sandbox_start", HudInk.OnButton, new Vector2(colX, -240f), new Vector2(340f, 130f), 44);

            sandbox = screen.gameObject.AddComponent<SandboxScreenController>();
            var so = new SerializedObject(sandbox);
            so.FindProperty("themes").objectReferenceValue = themes;
            so.FindProperty("startButton").objectReferenceValue = start;
            so.FindProperty("mapCode").objectReferenceValue = mapCode;
            so.FindProperty("mapCodeError").objectReferenceValue = codeError;
            so.FindProperty("randomCodeButton").objectReferenceValue = random;
            so.FindProperty("noMapsLabel").objectReferenceValue = noMaps.gameObject;
            WriteStrings(so.FindProperty("mapIds"), ids);
            WriteArray(so.FindProperty("cards"), cards);
            WriteArray(so.FindProperty("pictures"), pictures);
            WriteArray(so.FindProperty("pickedFrames"), picked);
            var keys = new List<string>();
            var sprites = new List<Object>();
            foreach (var id in ids)
            {
                AddArt(keys, sprites, id, $"Map_{id}");
                foreach (var theme in MapThemes.All) AddArt(keys, sprites, $"{id}_{MapThemes.Id(theme)}", $"Map_{id}_{MapThemes.Id(theme)}");
            }
            WriteStrings(so.FindProperty("pictureKeys"), keys);
            WriteArray(so.FindProperty("pictureSprites"), sprites.ToArray());
            so.ApplyModifiedPropertiesWithoutUndo();

            var group = Group(screen.gameObject);
            group.alpha = 0f;
            return group;
        }

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
            so.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(load.onClick, loadPanel.ConfirmLoad);
            UnityEventTools.AddPersistentListener(close.onClick, controller.CloseSaves);
            return dialog;
        }

        // ---------------------------------------------------------------- the invasion

        private static CanvasGroup BuildInvasionScreen(Transform canvas, MainMenuController controller, out Button settings)
        {
            var screen = StretchedChild(canvas, "InvasionScreen");
            var window = MenuWindow(screen, "#mode_invasion");
            var back = Corner(screen, "BackButton", "Back", new Vector2(0f, 1f), new Vector2(CornerInset, -CornerInset), CornerButton);
            settings = Corner(screen, "SettingsButton", "Settings", new Vector2(1f, 1f), new Vector2(-CornerInset, -CornerInset), CornerButton);
            UnityEventTools.AddPersistentListener(back.onClick, controller.Back);
            var hub = screen.gameObject.AddComponent<InvasionHubController>();

            // the theme's picture across the top, the heroes leaning in at its sides
            // under the title plate, which hangs over the window's top edge
            var frameSize = new Vector2(MenuWindowSize.x - 48f, 300f);
            var frame = new GameObject("Picture", typeof(RectTransform), typeof(RectMask2D));
            frame.transform.SetParent(window, false);
            Anchor((RectTransform)frame.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 196f), frameSize);
            var picture = CreateImage(frame.transform, "Image", new Color(0.3f, 0.45f, 0.25f));
            // baked at 1512x340 (MenuArtBake): shown whole across, its top and bottom trimmed by the frame
            Anchor(picture.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(frameSize.x, 340f));
            picture.raycastTarget = false;
            var firstPicture = MenuArt("Inv_summer");
            if (firstPicture != null) { picture.sprite = firstPicture; picture.color = Color.white; }
            Peek(frame.transform, "King", new Vector2(-frameSize.x * 0.5f - 20f, -frameSize.y * 0.5f), new Vector2(0f, 0f));
            Peek(frame.transform, "Queen", new Vector2(frameSize.x * 0.5f + 20f, -frameSize.y * 0.5f), new Vector2(1f, 0f));

            // the points and the crystals as the game's resource chips (the user's pick «А», 2026-10-25):
            // a parchment capsule, a disc with its sign on the left, the number
            var pointsText = Counter(frame.transform, "Points", new Vector2(-frameSize.x * 0.5f + 330f, 80f), new Color(0.88f, 0.66f, 0.23f), "Trophy", null);
            var crystalsText = Counter(frame.transform, "Crystals", new Vector2(frameSize.x * 0.5f - 400f, 80f), new Color(0.5f, 0.77f, 0.94f), null, MenuArt("Crystal"));
            var info = MenuRoundButton(frame.transform, "InfoButton", "round", "Info", HudInk.OnRound, 96f, 0.5f);
            Anchor((RectTransform)info.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(frameSize.x * 0.5f - 230f, 80f), new Vector2(96f, 96f));
            UnityEventTools.AddIntPersistentListener(info.onClick, controller.ShowInfo, (int)GameMode.Invasion);

            var themes = BuildThemes(frame.transform, new Vector2(0f, -86f), 96f, 128f, invasion: true);

            // the period's best runs
            var heading = MenuLabel(window, "TopHeading", null, 34, new Vector2(0f, 6f), new Vector2(1000f, 50f), HudInk.OnPanel, bold: true);
            var dot = SkinSprite("circle_mask");
            var runs = new Object[5];
            for (var i = 0; i < runs.Length; i++)
            {
                var x = (i - 2) * 250f;
                var pip = CreateImage(window, $"Run{i + 1}Pip", Color.white);
                pip.sprite = dot;
                pip.raycastTarget = false;
                Anchor(pip.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x - 60f, -50f), new Vector2(26f, 26f));
                SkinInk(pip, HudInk.OnPanel);
                pip.color = new Color(pip.color.r, pip.color.g, pip.color.b, 0.35f);
                runs[i] = MenuLabel(window, $"Run{i + 1}", null, 36, new Vector2(x + 25f, -50f), new Vector2(120f, 50f), HudInk.OnPanel, bold: true, align: TextAnchor.MiddleLeft);
            }
            var divider = MenuImage(window, "Divider", "divider", new Vector2(0f, -100f), new Vector2(1100f, 8f), Image.Type.Sliced);
            divider.raycastTarget = false;

            // the leaderboard: the player's own row, lit, until the online one exists
            var row = MenuImage(window, "PlayerRow", "row_selected", new Vector2(0f, -162f), new Vector2(1100f, 80f));
            row.raycastTarget = false;
            var place = MenuLabel(row.transform, "Place", null, 32, new Vector2(-480f, 2f), new Vector2(80f, 50f), HudInk.OnPanel, bold: true);
            var name = MenuLabel(row.transform, "Name", null, 32, new Vector2(-170f, 2f), new Vector2(460f, 50f), HudInk.OnPanel, align: TextAnchor.MiddleLeft);
            var score = MenuLabel(row.transform, "Points", null, 32, new Vector2(220f, 2f), new Vector2(200f, 50f), HudInk.OnPanel, bold: true, align: TextAnchor.MiddleRight);
            Gem(row.transform, new Vector2(380f, 2f), 44f);
            var reward = MenuLabel(row.transform, "Reward", null, 32, new Vector2(460f, 2f), new Vector2(110f, 50f), HudInk.OnPanel, bold: true, align: TextAnchor.MiddleLeft);
            MenuLabel(window, "Soon", "#invasion_board_soon", 24, new Vector2(0f, -226f), new Vector2(1100f, 40f), HudInk.OnPanel);

            var run = MenuTextButton(window, "RunButton", "button", "#invasion_run", HudInk.OnButton, new Vector2(0f, -330f), new Vector2(420f, 110f), 46);
            UnityEventTools.AddPersistentListener(run.onClick, controller.StartInvasion);

            // the periods, under the window
            var periods = new[] { "#invasion_day", "#invasion_week", "#invasion_month" };
            var selected = new Object[periods.Length];
            for (var i = 0; i < periods.Length; i++)
            {
                var tab = MenuTab(screen, $"Tab_{i}", periods[i], new Vector2((i - 1) * 480f, PeriodTabsY), new Vector2(300f, 72f));
                tab.selected.SetActive(i == 0);
                selected[i] = tab.selected;
                UnityEventTools.AddIntPersistentListener(tab.button.onClick, hub.SelectPeriod, i);
            }

            var so = new SerializedObject(hub);
            so.FindProperty("themes").objectReferenceValue = themes;
            so.FindProperty("picture").objectReferenceValue = picture;
            var themeIds = new List<string>();
            var sprites = new List<Object>();
            foreach (var theme in MapThemes.All) AddArt(themeIds, sprites, MapThemes.Id(theme), $"Inv_{MapThemes.Id(theme)}");
            WriteStrings(so.FindProperty("pictureThemes"), themeIds);
            WriteArray(so.FindProperty("pictureSprites"), sprites.ToArray());
            so.FindProperty("pointsText").objectReferenceValue = pointsText;
            so.FindProperty("topHeading").objectReferenceValue = heading;
            WriteArray(so.FindProperty("topRuns"), runs);
            WriteArray(so.FindProperty("tabSelected"), selected);
            so.FindProperty("playerRowPlace").objectReferenceValue = place;
            so.FindProperty("playerRowName").objectReferenceValue = name;
            so.FindProperty("playerRowPoints").objectReferenceValue = score;
            so.FindProperty("playerRowReward").objectReferenceValue = reward;
            so.FindProperty("crystalsText").objectReferenceValue = crystalsText;
            so.ApplyModifiedPropertiesWithoutUndo();

            var group = Group(screen.gameObject);
            group.alpha = 0f;
            return group;
        }

        /// <summary>
        /// The modes screen's banners and «Играть» (ModeBannersController). Each banner hangs from
        /// its top (the pivot), so it unrolls downward and sways from its perch; the name sits on the
        /// kit's parchment plaque, the motto and the live line in light ink on the cloth.
        /// </summary>
        private static void BuildModeBanners(RectTransform modes, MainMenuController menu)
        {
            var names = new[] { "Tutorial", "Sandbox", "Invasion", "Campaign" };
            var keys = new[] { "#mode_tutorial", "#mode_sandbox", "#mode_invasion", "#mode_campaign" };
            var hall = StretchedChild(modes, "Banners");
            var banners = new Object[names.Length];
            var cloths = new Object[names.Length];
            var lines = new Object[names.Length];
            var bannerControl = modes.gameObject.AddComponent<ModeBannersController>();
            Image rewardIcon = null;
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
                Anchor(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2((i - 1.5f) * 350f, 380f), size);
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
                    // the tutorial's reward: «Награда: 25» and the upgrade crystal (the user, 2026-11-10)
                    var crystal = CreateImage(cloth.transform, "RewardCrystal", Color.white);
                    crystal.sprite = IconSprite("Res_UpgradeCrystal");
                    crystal.preserveAspect = true;
                    crystal.raycastTarget = false;
                    Anchor(crystal.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(95f - 24f, -392f * k), new Vector2(40f, 40f));
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

            var so = new SerializedObject(bannerControl);
            so.FindProperty("menu").objectReferenceValue = menu;
            so.FindProperty("screen").objectReferenceValue = Group(modes.gameObject);
            so.FindProperty("playButton").objectReferenceValue = play.GetComponent<Image>();
            WriteArray(so.FindProperty("banners"), banners);
            WriteArray(so.FindProperty("cloths"), cloths);
            so.FindProperty("motto").objectReferenceValue = motto;
            so.FindProperty("rewardIcon").objectReferenceValue = rewardIcon;
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
        private static Text Counter(Transform parent, string name, Vector2 position, Color disc, string glyph, Sprite picture)
        {
            var plaque = MenuImage(parent, name, "plaque", position, new Vector2(230f, 84f));
            plaque.raycastTarget = false;
            var round = CreateImage(plaque.transform, "Disc", disc);
            round.sprite = SkinSprite("circle_mask");
            round.raycastTarget = false;
            Anchor(round.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40f, 3f), new Vector2(62f, 62f));
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
            return MenuLabel(plaque.transform, "Number", null, 42, new Vector2(38f, 3f), new Vector2(130f, 60f), HudInk.OnMenuPlaque, bold: true, align: TextAnchor.MiddleLeft);
        }

        /// <summary>The crystal, the game's currency, in its own colours (Tools/ui/make_hud_glyphs.py draws it into MenuArt).</summary>
        private static void Gem(Transform parent, Vector2 position, float size)
        {
            var art = MenuArt("Crystal");
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
