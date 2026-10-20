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
    /// The main menu after the user's sketch (2026-10-20) -- see MainMenuController for the screens
    /// and the steps between them. Everything is drawn in the menu's own sprites
    /// (Resources/MenuSkins, Tools/ui/make_menu_kit.py), one set per HUD look, and a HudSkinApplier
    /// reading that folder switches the whole menu when the player picks another look in the settings.
    ///
    /// The mode cards and the map list show pictures taken in the game itself
    /// (Assets/_Project/UI/MenuArt, MenuArtBake); while one is missing its place stays dark.
    /// </summary>
    public static partial class SetupProject
    {
        private static readonly string[] ModeIds = { "Campaign", "Sandbox", "Invasion" };
        private static readonly string[] ModeKeys = { "#mode_campaign", "#mode_sandbox", "#mode_invasion" };

        private static readonly Vector2 CardSize = new Vector2(440f, 680f);
        private const float CardSpacing = 500f;
        private const float CardY = -20f;
        private static readonly Vector2 ModePanelSize = new Vector2(1240f, 820f);
        private static readonly Vector2 ModePanelPos = new Vector2(250f, -30f);

        private static Sprite MenuSprite(string role)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{UiTextureImport.MenuSkinsFolder}/{GameSettings.DefaultHudSkin}/{role}.png");
        }

        private static Sprite MenuArt(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{UiTextureImport.MenuArtFolder}/{name}.png");
        }

        /// <summary>An image drawn by the menu's look; centred, untinted.</summary>
        private static Image MenuImage(Transform parent, string name, string role, Vector2 position, Vector2 size, Image.Type type = Image.Type.Sliced)
        {
            var image = CreateImage(parent, name, Color.white);
            Anchor(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            image.sprite = MenuSprite(role);
            image.type = type;
            SkinImages.Add(new HudSkinImage { image = image, role = role, ink = HudInk.Keep });
            return image;
        }

        private static Image MenuGlyph(Transform parent, string glyph, HudInk ink, float size)
        {
            var icon = CreateImage(parent, "Icon", Color.white);
            icon.sprite = HudGlyph(glyph);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            Anchor(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(size, size));
            // a soft dark under the glyph, so it reads as pressed into the button
            var shadow = icon.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(0f, -3f);
            SkinInk(icon, ink);
            return icon;
        }

        /// <summary>A label written in the look's ink for what it stands on; `key` null for one the controller writes.</summary>
        private static Text MenuLabel(Transform parent, string name, string key, int size, Vector2 position, Vector2 box, HudInk ink, bool bold = false, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var text = CreateText(parent, name, string.Empty, size, position, box);
            if (key != null) Localize(text, key);
            text.font = bold ? UiTheme.Bold : UiTheme.Regular;
            text.alignment = align;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            SkinInk(text, ink);
            return text;
        }

        private static Button MenuRoundButton(Transform parent, string name, string role, string glyph, HudInk ink, float size, float glyphScale = 0.46f)
        {
            var image = MenuImage(parent, name, role, Vector2.zero, new Vector2(size, size), Image.Type.Simple);
            image.preserveAspect = true;
            var button = image.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            MenuGlyph(image.transform, glyph, ink, size * glyphScale);
            return button;
        }

        private static Button MenuTextButton(Transform parent, string name, string role, string key, HudInk ink, Vector2 position, Vector2 size, int fontSize = 38)
        {
            var image = MenuImage(parent, name, role, position, size);
            var button = image.gameObject.AddComponent<Button>();
            MenuLabel(image.transform, "Label", key, fontSize, new Vector2(0f, 4f), size - new Vector2(40f, 20f), ink, bold: true);
            return button;
        }

        /// <summary>A tab: the idle look, with the picked look stacked over it and switched on by the controller.</summary>
        private static (Button button, GameObject selected) MenuTab(Transform parent, string name, string key, Vector2 position, Vector2 size)
        {
            var idle = MenuImage(parent, name, "tab", position, size);
            var button = idle.gameObject.AddComponent<Button>();
            MenuLabel(idle.transform, "Label", key, 32, new Vector2(0f, 3f), size - new Vector2(30f, 10f), HudInk.OnTab, bold: true);
            var picked = MenuImage(idle.transform, "Selected", "tab_selected", Vector2.zero, size);
            picked.raycastTarget = false;
            MenuLabel(picked.transform, "Label", key, 32, new Vector2(0f, 3f), size - new Vector2(30f, 10f), HudInk.OnTabSelected, bold: true);
            picked.gameObject.SetActive(false);
            return (button, picked.gameObject);
        }

        private static CanvasGroup Group(GameObject go)
        {
            var group = go.GetComponent<CanvasGroup>();
            return group != null ? group : go.AddComponent<CanvasGroup>();
        }

        private static RectTransform StretchedChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            StretchFull(rect);
            return rect;
        }

        // ---------------------------------------------------------------- the whole menu

        private static void BuildMainMenu(Transform canvas, Sprite panelSprite)
        {
            BeginHudSkins();
            var controller = canvas.gameObject.AddComponent<MainMenuController>();

            var vignette = MenuImage(canvas, "Vignette", "vignette", Vector2.zero, Vector2.zero, Image.Type.Simple);
            StretchFull(vignette.rectTransform);
            vignette.raycastTarget = false;

            // ---- main screen
            var main = StretchedChild(canvas, "MainScreen");
            var play = MenuRoundButton(main, "PlayButton", "round_main", "Play", HudInk.OnMain, 300f, 0.4f);
            Anchor((RectTransform)play.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(300f, 300f));
            var shop = MenuRoundButton(main, "ShopButton", "round", "Shop", HudInk.OnRound, 180f, 0.5f);
            Anchor((RectTransform)shop.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-270f, 150f), new Vector2(180f, 180f));
            var settings = MenuRoundButton(main, "SettingsButton", "round", "Settings", HudInk.OnRound, 180f, 0.5f);
            Anchor((RectTransform)settings.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(270f, 150f), new Vector2(180f, 180f));

            var cont = MenuImage(main, "ContinueButton", "plaque", Vector2.zero, new Vector2(600f, 180f));
            Anchor(cont.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-48f, 48f), new Vector2(600f, 180f));
            var continueButton = cont.gameObject.AddComponent<Button>();
            var contIcon = MenuRoundButton(cont.transform, "Medallion", "round", "Resume", HudInk.OnRound, 150f, 0.5f);
            contIcon.GetComponent<Image>().raycastTarget = false;
            contIcon.interactable = false;
            Anchor((RectTransform)contIcon.transform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(70f, 6f), new Vector2(150f, 150f));
            MenuLabel(cont.transform, "Title", "#menu_continue", 42, new Vector2(70f, 42f), new Vector2(400f, 56f), HudInk.OnMenuPlaque, bold: true, align: TextAnchor.MiddleLeft);
            var continueDetail = MenuLabel(cont.transform, "Detail", null, 26, new Vector2(70f, -26f), new Vector2(400f, 76f), HudInk.OnMenuPlaque, align: TextAnchor.MiddleLeft);

            // ---- the modes
            var modes = StretchedChild(canvas, "ModesScreen");
            var dim = CreateImage(modes, "Dim", new Color(0.05f, 0.03f, 0.01f, 0.45f));
            StretchFull(dim.rectTransform);
            dim.raycastTarget = false;

            var back = MenuRoundButton(modes, "BackButton", "round", "Back", HudInk.OnRound, 140f, 0.5f);
            Anchor((RectTransform)back.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -36f), new Vector2(140f, 140f));
            var settings2 = MenuRoundButton(modes, "SettingsButton", "round", "Settings", HudInk.OnRound, 140f, 0.5f);
            Anchor((RectTransform)settings2.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -36f), new Vector2(140f, 140f));

            var cards = new RectTransform[3];
            var status = new Text[3];
            for (var i = 0; i < 3; i++)
            {
                cards[i] = BuildModeCard(modes, i, out status[i]);
                UnityEventTools.AddIntPersistentListener(cards[i].GetComponent<Button>().onClick, controller.ChooseMode, i);
            }

            var campaign = BuildCampaignPanel(modes);
            var sandbox = BuildSandboxPanel(modes, controller, out var mapSelect, out var loadPanel, out var newTabSelected, out var savesTabSelected);
            var invasion = BuildInvasionPanel(modes, out var timer);

            // ---- dialogs over everything
            var settingsPanel = BuildSettingsPanel(canvas, panelSprite, settings);
            UnityEventTools.AddPersistentListener(settings2.onClick, settingsPanel.OpenPanel);
            var exitDialog = BuildMenuDialog(canvas, "ExitDialog", "#quit_question", null, new Vector2(820f, 420f), out var exitCard);
            var stay = MenuTextButton(exitCard, "StayButton", "plaque", "#quit_stay", HudInk.OnMenuPlaque, new Vector2(-180f, -90f), new Vector2(320f, 120f));
            var quit = MenuTextButton(exitCard, "QuitButton", "button", "#quit_confirm", HudInk.OnButton, new Vector2(180f, -90f), new Vector2(320f, 120f));
            UnityEventTools.AddPersistentListener(stay.onClick, controller.CloseExitDialog);
            UnityEventTools.AddPersistentListener(quit.onClick, controller.QuitGame);

            var shopDialog = BuildMenuDialog(canvas, "ShopDialog", "#shop_title", "#shop_soon", new Vector2(1000f, 640f), out var shopCard);
            for (var s = 0; s < 4; s++)
            {
                var slot = MenuImage(shopCard, $"Slot{s}", "row", new Vector2(-330f + s * 220f, -40f), new Vector2(190f, 190f));
                slot.raycastTarget = false;
                MenuGlyph(slot.transform, "Lock", HudInk.OnPanel, 70f).color = new Color(1f, 1f, 1f, 0.5f);
            }
            var closeShop = MenuTextButton(shopCard, "CloseButton", "plaque", "#ui_close", HudInk.OnMenuPlaque, new Vector2(0f, -230f), new Vector2(320f, 110f));
            UnityEventTools.AddPersistentListener(closeShop.onClick, controller.CloseShop);

            UnityEventTools.AddPersistentListener(play.onClick, controller.OpenModes);
            UnityEventTools.AddPersistentListener(shop.onClick, controller.OpenShop);
            UnityEventTools.AddPersistentListener(continueButton.onClick, controller.ContinueGame);
            UnityEventTools.AddPersistentListener(back.onClick, controller.Back);

            var so = new SerializedObject(controller);
            so.FindProperty("mainScreen").objectReferenceValue = Group(main.gameObject);
            so.FindProperty("modesScreen").objectReferenceValue = Group(modes.gameObject);
            WriteArray(so.FindProperty("cards"), cards);
            WriteArray(so.FindProperty("cardStatus"), status);
            WriteArray(so.FindProperty("panels"), new Object[] { campaign, sandbox, invasion });
            so.FindProperty("chosenCardX").floatValue = ModePanelPos.x - ModePanelSize.x * 0.5f - 60f - CardSize.x * 0.5f;
            so.FindProperty("continueButton").objectReferenceValue = cont.gameObject;
            so.FindProperty("continueDetail").objectReferenceValue = continueDetail;
            so.FindProperty("exitDialog").objectReferenceValue = exitDialog;
            so.FindProperty("shopDialog").objectReferenceValue = shopDialog;
            so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            so.FindProperty("mapSelectPanel").objectReferenceValue = mapSelect;
            so.FindProperty("loadGamePanel").objectReferenceValue = loadPanel;
            so.FindProperty("newGameTabSelected").objectReferenceValue = newTabSelected;
            so.FindProperty("savesTabSelected").objectReferenceValue = savesTabSelected;
            so.FindProperty("invasionTimer").objectReferenceValue = timer;
            so.ApplyModifiedPropertiesWithoutUndo();

            BuildMenuSkinApplier(canvas);
        }

        private static void WriteArray(SerializedProperty property, Object[] values)
        {
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        /// <summary>
        /// A mode card: the picture from the game in the upper part, the card's lower part under it,
        /// the frame over both, the mode's name on a title plate across the seam, an ornament, and a
        /// line the controller writes (the number of saves, "coming soon").
        /// </summary>
        private static RectTransform BuildModeCard(Transform parent, int index, out Text status)
        {
            var root = new GameObject($"Card_{ModeIds[index]}", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(parent, false);
            var rect = (RectTransform)root.transform;
            Anchor(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((index - 1) * CardSpacing, CardY), CardSize);

            const float inset = 30f;
            const float artHeight = 430f;
            var art = CreateImage(root.transform, "Picture", new Color(0.12f, 0.08f, 0.05f));
            var artSprite = MenuArt($"Card_{ModeIds[index]}");
            if (artSprite != null)
            {
                art.sprite = artSprite;
                art.color = Color.white;
                art.preserveAspect = false;
            }
            Anchor(art.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -inset), new Vector2(CardSize.x - inset * 2f, artHeight));

            var footer = MenuImage(root.transform, "Footer", "card_footer", Vector2.zero, Vector2.zero);
            Anchor(footer.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, inset), new Vector2(CardSize.x - inset * 2f, CardSize.y - artHeight - inset * 2f));

            var frame = MenuImage(root.transform, "Frame", "card", Vector2.zero, Vector2.zero);
            StretchFull(frame.rectTransform);
            var button = root.AddComponent<Button>();
            button.targetGraphic = frame;

            var title = MenuImage(root.transform, "TitlePlate", "title", new Vector2(0f, CardSize.y * 0.5f - inset - artHeight), new Vector2(400f, 104f));
            title.raycastTarget = false;
            MenuLabel(title.transform, "Name", ModeKeys[index], 40, new Vector2(0f, 4f), new Vector2(340f, 70f), HudInk.OnTitle, bold: true);

            var divider = MenuImage(root.transform, "Divider", "divider", new Vector2(0f, -CardSize.y * 0.5f + 140f), new Vector2(300f, 30f), Image.Type.Simple);
            divider.raycastTarget = false;
            status = MenuLabel(root.transform, "Status", null, 30, new Vector2(0f, -CardSize.y * 0.5f + 92f), new Vector2(360f, 50f), HudInk.OnPanel, bold: true);
            return rect;
        }

        private static RectTransform ModePanel(Transform parent, string name, string titleKey, out Transform body)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(parent, false);
            var rect = (RectTransform)root.transform;
            Anchor(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), ModePanelPos, ModePanelSize);
            var bg = MenuImage(root.transform, "Background", "panel", Vector2.zero, Vector2.zero, Image.Type.Tiled);
            StretchFull(bg.rectTransform);
            var title = MenuImage(root.transform, "TitlePlate", "title", new Vector2(0f, ModePanelSize.y * 0.5f - 6f), new Vector2(560f, 110f));
            MenuLabel(title.transform, "Title", titleKey, 44, new Vector2(0f, 4f), new Vector2(500f, 70f), HudInk.OnTitle, bold: true);
            body = root.transform;
            Group(root).alpha = 0f;
            return rect;
        }

        // ---------------------------------------------------------------- the campaign: a map of levels

        private static CanvasGroup BuildCampaignPanel(Transform parent)
        {
            var rect = ModePanel(parent, "CampaignPanel", "#campaign_title", out var body);

            // The levels along a winding road, the first open and the rest locked -- the campaign
            // itself is still to be designed, so this is its shape, not its content.
            var nodes = new[]
            {
                new Vector2(-470f, -170f), new Vector2(-330f, -40f), new Vector2(-380f, 130f), new Vector2(-190f, 210f), new Vector2(-30f, 100f),
                new Vector2(20f, -90f), new Vector2(200f, -170f), new Vector2(340f, -30f), new Vector2(430f, 160f),
            };
            var dot = SkinSprite("circle_mask");
            for (var i = 0; i + 1 < nodes.Length; i++)
            {
                for (var k = 1; k < 6; k++)
                {
                    var p = Vector2.Lerp(nodes[i], nodes[i + 1], k / 6f);
                    var d = CreateImage(body, $"Path{i}_{k}", Color.white);
                    d.sprite = dot;
                    d.raycastTarget = false;
                    Anchor(d.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), p, new Vector2(14f, 14f));
                    SkinInk(d, HudInk.OnPanel);
                }
            }
            for (var i = 0; i < nodes.Length; i++)
            {
                var first = i == 0;
                var size = first ? 150f : 116f;
                var node = MenuImage(body, $"Level{i + 1}", first ? "round_main" : "round", nodes[i], new Vector2(size, size), Image.Type.Simple);
                node.preserveAspect = true;
                if (first)
                {
                    MenuLabel(node.transform, "Number", null, 56, new Vector2(0f, 4f), new Vector2(100f, 80f), HudInk.OnMain, bold: true).text = "1";
                }
                else
                {
                    MenuGlyph(node.transform, "Lock", HudInk.OnRound, size * 0.42f);
                }
            }
            MenuLabel(body, "Soon", "#campaign_soon", 30, new Vector2(0f, -330f), new Vector2(1000f, 60f), HudInk.OnPanel);
            return Group(rect.gameObject);
        }

        // ---------------------------------------------------------------- the sandbox: new game and saves

        private static CanvasGroup BuildSandboxPanel(Transform parent, MainMenuController controller, out MapSelectPanelController mapSelect,
            out LoadGamePanelController loadPanel, out GameObject newTabSelected, out GameObject savesTabSelected)
        {
            var rect = ModePanel(parent, "SandboxPanel", "#mode_sandbox", out var body);

            var newTab = MenuTab(body, "NewGameTab", "#sandbox_tab_new", new Vector2(-380f, 300f), new Vector2(340f, 100f));
            var savesTab = MenuTab(body, "SavesTab", "#sandbox_tab_saves", new Vector2(0f, 300f), new Vector2(340f, 100f));
            newTabSelected = newTab.selected;
            savesTabSelected = savesTab.selected;
            UnityEventTools.AddPersistentListener(newTab.button.onClick, controller.ShowNewGameTab);
            UnityEventTools.AddPersistentListener(savesTab.button.onClick, controller.ShowSavesTab);

            // new game: the maps, Play
            var newBody = StretchedChild(body, "NewGame");
            BuildScrollList(newBody, new Vector2(0f, -10f), new Vector2(1100f, 440f), "#newgame_no_maps", out var mapList, out var noMaps);
            noMaps.SetActive(false);
            Well(newBody);
            var play = MenuTextButton(newBody, "PlayButton", "button", "#newgame_play", HudInk.OnButton, new Vector2(370f, -310f), new Vector2(400f, 130f), 44);
            mapSelect = newBody.gameObject.AddComponent<MapSelectPanelController>();
            var mso = new SerializedObject(mapSelect);
            mso.FindProperty("panelRoot").objectReferenceValue = newBody.gameObject;
            mso.FindProperty("listContent").objectReferenceValue = mapList;
            mso.FindProperty("playButton").objectReferenceValue = play;
            mso.FindProperty("menuLook").boolValue = true;
            var ids = MapSelector.PlayerMapIds;
            var withPreview = new System.Collections.Generic.List<(string id, Sprite sprite)>();
            foreach (var id in ids)
            {
                var preview = MenuArt($"Map_{id}");
                if (preview != null) withPreview.Add((id, preview));
            }
            var previewIds = mso.FindProperty("previewMapIds");
            var previews = mso.FindProperty("previews");
            previewIds.arraySize = previews.arraySize = withPreview.Count;
            for (var i = 0; i < withPreview.Count; i++)
            {
                previewIds.GetArrayElementAtIndex(i).stringValue = withPreview[i].id;
                previews.GetArrayElementAtIndex(i).objectReferenceValue = withPreview[i].sprite;
            }
            mso.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(play.onClick, mapSelect.ConfirmPlay);

            // saves: the list, which one is picked, Load
            var savesBody = StretchedChild(body, "Saves");
            BuildScrollList(savesBody, new Vector2(0f, -10f), new Vector2(1100f, 440f), "#load_empty", out var saveList, out var empty);
            Well(savesBody);
            SkinInk(empty.GetComponent<Text>(), HudInk.OnPanel);
            var picked = MenuLabel(savesBody, "SelectedLabel", null, 28, new Vector2(-200f, -310f), new Vector2(640f, 60f), HudInk.OnPanel, align: TextAnchor.MiddleLeft);
            var load = MenuTextButton(savesBody, "LoadButton", "button", "#load_confirm", HudInk.OnButton, new Vector2(370f, -310f), new Vector2(400f, 130f), 44);
            loadPanel = savesBody.gameObject.AddComponent<LoadGamePanelController>();
            var lso = new SerializedObject(loadPanel);
            lso.FindProperty("panelRoot").objectReferenceValue = savesBody.gameObject;
            lso.FindProperty("listContent").objectReferenceValue = saveList;
            lso.FindProperty("emptyLabel").objectReferenceValue = empty;
            lso.FindProperty("loadSelectedButton").objectReferenceValue = load;
            lso.FindProperty("selectedNameLabel").objectReferenceValue = picked;
            lso.FindProperty("menuLook").boolValue = true;
            lso.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(load.onClick, loadPanel.ConfirmLoad);

            newBody.gameObject.SetActive(false);
            savesBody.gameObject.SetActive(false);
            return Group(rect.gameObject);
        }

        /// <summary>The scroll list's dark well, made faint: the rows carry the look, the well only holds them.</summary>
        private static void Well(Transform body)
        {
            var viewport = body.Find("ScrollView/Viewport");
            if (viewport != null) viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.06f);
        }

        // ---------------------------------------------------------------- the invasion: its hub

        private static CanvasGroup BuildInvasionPanel(Transform parent, out Text timer)
        {
            var rect = ModePanel(parent, "InvasionPanel", "#mode_invasion", out var body);

            // left: today's challenge and the player's points
            var challenge = MenuImage(body, "Challenge", "plaque", new Vector2(-300f, 150f), new Vector2(540f, 260f));
            MenuLabel(challenge.transform, "Heading", "#invasion_daily", 38, new Vector2(0f, 70f), new Vector2(480f, 56f), HudInk.OnMenuPlaque, bold: true);
            MenuLabel(challenge.transform, "What", "#invasion_daily_soon", 26, new Vector2(0f, 8f), new Vector2(480f, 70f), HudInk.OnMenuPlaque);
            timer = MenuLabel(challenge.transform, "Timer", null, 30, new Vector2(0f, -66f), new Vector2(480f, 50f), HudInk.OnMenuPlaque, bold: true);

            MenuLabel(body, "PointsHeading", "#invasion_my_points", 30, new Vector2(-300f, -40f), new Vector2(540f, 50f), HudInk.OnPanel);
            MenuLabel(body, "Points", null, 84, new Vector2(-300f, -120f), new Vector2(540f, 100f), HudInk.OnPanel, bold: true).text = "0";
            var counted = MenuLabel(body, "Counted", null, 26, new Vector2(-300f, -200f), new Vector2(540f, 44f), HudInk.OnPanel);
            counted.text = Localization.Format("#invasion_counted", 0, 5);
            var dot = SkinSprite("circle_mask");
            for (var i = 0; i < 5; i++)
            {
                var pip = CreateImage(body, $"Run{i + 1}", Color.white);
                pip.sprite = dot;
                Anchor(pip.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-300f + (i - 2) * 64f, -256f), new Vector2(40f, 40f));
                SkinInk(pip, HudInk.OnPanel);
                pip.color = new Color(pip.color.r, pip.color.g, pip.color.b, 0.25f);
            }

            // right: the leaderboards
            var periods = new[] { "#invasion_day", "#invasion_week", "#invasion_month" };
            for (var i = 0; i < periods.Length; i++)
            {
                var tab = MenuTab(body, $"Tab_{i}", periods[i], new Vector2(130f + i * 180f, 250f), new Vector2(170f, 86f));
                tab.selected.SetActive(i == 0);
            }
            var board = MenuImage(body, "Board", "card_footer", new Vector2(310f, -20f), new Vector2(540f, 400f));
            board.raycastTarget = false;
            for (var r = 0; r < 4; r++)
            {
                var row = MenuImage(board.transform, $"Place{r + 1}", "row", new Vector2(0f, 150f - r * 74f), new Vector2(500f, 66f));
                row.color = new Color(1f, 1f, 1f, 0.55f);
                MenuLabel(row.transform, "Place", null, 26, new Vector2(-210f, 2f), new Vector2(60f, 40f), HudInk.OnPanel, bold: true).text = $"{r + 1}";
                MenuLabel(row.transform, "Name", null, 26, new Vector2(10f, 2f), new Vector2(360f, 40f), HudInk.OnPanel, align: TextAnchor.MiddleLeft).text = "—";
            }
            MenuLabel(board.transform, "Soon", "#invasion_board_soon", 26, new Vector2(0f, -150f), new Vector2(500f, 50f), HudInk.OnPanel);

            var fight = MenuTextButton(body, "FightButton", "button", "#invasion_fight", HudInk.OnButton, new Vector2(370f, -310f), new Vector2(400f, 130f), 44);
            fight.interactable = false;
            return Group(rect.gameObject);
        }

        // ---------------------------------------------------------------- dialogs

        private static GameObject BuildMenuDialog(Transform canvas, string name, string titleKey, string textKey, Vector2 size, out Transform card)
        {
            var root = StretchedChild(canvas, name);
            var backdrop = CreateImage(root, "Backdrop", new Color(0.04f, 0.02f, 0.01f, 0.65f));
            StretchFull(backdrop.rectTransform);
            var panel = MenuImage(root, "Card", "panel", Vector2.zero, size, Image.Type.Tiled);
            card = panel.transform;
            var title = MenuImage(card, "TitlePlate", "title", new Vector2(0f, size.y * 0.5f - 6f), new Vector2(560f, 110f));
            MenuLabel(title.transform, "Title", titleKey, 42, new Vector2(0f, 4f), new Vector2(500f, 70f), HudInk.OnTitle, bold: true);
            if (textKey != null) MenuLabel(card, "Text", textKey, 32, new Vector2(0f, size.y * 0.5f - 150f), new Vector2(size.x - 160f, 90f), HudInk.OnPanel);
            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        /// <summary>The menu's own HudSkinApplier: the same switch the HUD has, reading Resources/MenuSkins.</summary>
        private static void BuildMenuSkinApplier(Transform canvas)
        {
            var applier = canvas.gameObject.AddComponent<HudSkinApplier>();
            var so = new SerializedObject(applier);
            so.FindProperty("resourcesFolder").stringValue = MenuRowLook.ResourcesFolder;
            var images = so.FindProperty("images");
            images.arraySize = SkinImages.Count;
            for (var i = 0; i < SkinImages.Count; i++)
            {
                var element = images.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("image").objectReferenceValue = SkinImages[i].image;
                element.FindPropertyRelative("role").stringValue = SkinImages[i].role;
                element.FindPropertyRelative("ink").enumValueIndex = (int)SkinImages[i].ink;
            }
            var labels = so.FindProperty("labels");
            labels.arraySize = SkinLabels.Count;
            for (var i = 0; i < SkinLabels.Count; i++)
            {
                var element = labels.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("text").objectReferenceValue = SkinLabels[i].text;
                element.FindPropertyRelative("ink").enumValueIndex = (int)SkinLabels[i].ink;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
