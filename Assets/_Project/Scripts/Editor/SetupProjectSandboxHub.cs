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
    /// The sandbox screen «Стол картографа» (concept A3, approved by the user 2026-11-17) and the pieces of «Свобода
    /// песочницы» it shares with the game (card Design/features/sandbox-freedom.md): the world settings' scroll and the
    /// purchase banner. The pieces and their places come from Tools/ui/sandbox_hub_art.py (MenuArt/SandHub_*.png,
    /// Freedom_*.png + SandHub_layout.json, a 1920 x 1080 frame, y down), placed the way the invasion hall is.
    /// </summary>
    public static partial class SetupProject
    {
        private static readonly Color HubInk = new Color(0.2f, 0.14f, 0.086f);
        private static readonly Color HubCream = new Color(1f, 0.94f, 0.78f);
        private static readonly Color HubWood = new Color(0.24f, 0.13f, 0.04f);

        private static HallLayout SandLayout()
        {
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>($"{UiTextureImport.MenuArtFolder}/SandHub_layout.json");
            if (json == null) Debug.LogWarning("[MainMenu] no SandHub_layout.json -- run Tools/ui/sandbox_hub_art.py");
            return json != null ? JsonUtility.FromJson<HallLayout>(json.text) : new HallLayout();
        }

        private static Image SandPiece(Transform parent, HallLayout layout, string name, string prefix = "SandHub_")
        {
            var image = CreateImage(parent, name, Color.white);
            image.sprite = MenuArt(prefix + name);
            image.raycastTarget = false;
            var r = HallFind(layout.pieces, name);
            if (r != null) HallPlace(image.rectTransform, r.rect);
            return image;
        }

        /// <summary>A bit drawn to be stretched (the segment bars): its rounded ends kept by a 9-slice border.</summary>
        private static Sprite SlicedMenuArt(string name, float border)
        {
            var path = $"{UiTextureImport.MenuArtFolder}/{name}.png";
            if (AssetImporter.GetAtPath(path) is TextureImporter importer && importer.spriteBorder.x != border)
            {
                importer.spriteBorder = new Vector4(border, border, border, border);
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static readonly string[][] WorldOptionKeys =
        {
            new[] { "#world_raids_0", "#world_raids_1", "#world_raids_2", "#world_raids_3", "#world_raids_4" },
            new[] { "#world_peace", "#world_usual" },
            new[] { "#world_quiet_0", "#world_quiet_1", "#world_quiet_2", "#world_quiet_3" },
            new[] { "#world_off", "#world_on" },
            new[] { "#world_off", "#world_on" },
            new[] { "#world_off", "#world_on" },
            new[] { "#world_goods_0", "#world_goods_1", "#world_goods_2" },
            new[] { "#world_speed_0", "#world_speed_1", "#world_speed_2" },
            new[] { "#world_speed_0", "#world_speed_1", "#world_speed_2" },
            new[] { "#world_fog_0", "#world_fog_1" },
        };

        private static readonly string[] WorldRowKeys =
        {
            "#world_raids", "#world_neighbour", "#world_quiet", "#world_fires", "#world_storms", "#world_plague",
            "#world_goods", "#world_build", "#world_research", "#world_fog",
        };

        /// <summary>
        /// The «Настройки мира» scroll in a frame-sized layer (its pieces where the art drew them): a row per setting
        /// -- the glyph, the name, the options as a wooden bar with the picked one gold, the struck pig of an easier
        /// choice -- the hint or the piggy warning at the foot and «По умолчанию».
        /// </summary>
        private static WorldSettingsScroll BuildWorldScroll(Transform parent, HallLayout layout, bool inGame)
        {
            var frame = HallFrame(parent, "WorldScroll");
            SandPiece(frame, layout, "Scroll");
            HallText(frame, layout, "ScrollTitle", "#world_title", 40, HubInk, null);

            var bar = SlicedMenuArt("SandHub_SegBar", 22f);
            var pick = SlicedMenuArt("SandHub_SegPick", 19f);
            var segments = new List<Object>();
            var picks = new List<Object>();
            var pigs = new Object[WorldSettings.All.Length];
            var groups = new Object[WorldSettings.All.Length];
            for (var r = 0; r < WorldSettings.All.Length; r++)
            {
                var box = HallFind(layout.texts, $"Row{r}");
                var row = new GameObject($"Row{r}", typeof(RectTransform), typeof(CanvasGroup));
                row.transform.SetParent(frame, false);
                var rect = (RectTransform)row.transform;
                if (box != null) HallPlace(rect, box.rect);
                groups[r] = row.GetComponent<CanvasGroup>();
                var w = box != null ? box.rect[2] - box.rect[0] : 560f;
                var n = WorldSettings.Options(WorldSettings.All[r]);

                var glyph = CreateImage(row.transform, "Glyph", Color.white);
                glyph.sprite = MenuArt($"SandHub_RowGlyph{r}");
                glyph.preserveAspect = true;
                glyph.raycastTarget = false;
                Anchor(glyph.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(26f, 0f), new Vector2(36f, 36f));

                var barX = w * (n >= 5 ? 0.27f : 0.45f);
                var name = HallLabel(row.transform, "Name", WorldRowKeys[r], 23, true, TextAnchor.MiddleLeft);
                name.color = HubInk;
                Anchor(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(54f, 0f), new Vector2(barX - 58f, 40f));
                name.resizeTextForBestFit = true;
                name.resizeTextMinSize = 14;
                name.resizeTextMaxSize = 23;

                var back = CreateImage(row.transform, "Bar", Color.white);
                back.sprite = bar;
                back.type = Image.Type.Sliced;
                back.pixelsPerUnitMultiplier = 46f / 37f;
                back.raycastTarget = false;
                Anchor(back.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(barX, 0f), new Vector2(w - barX, 37f));
                var seg = (w - barX) / n;
                var size = n <= 2 ? 23 : n == 3 ? 22 : n == 4 ? 20 : 17;
                for (var o = 0; o < n; o++)
                {
                    var cell = CreateImage(row.transform, $"Option{o}", Color.clear);
                    Anchor(cell.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(barX + seg * (o + 0.5f), 0f), new Vector2(seg, 44f));
                    var button = cell.gameObject.AddComponent<Button>();
                    button.transition = Selectable.Transition.None;
                    segments.Add(button);
                    var gold = CreateImage(cell.transform, "Picked", Color.white);
                    gold.sprite = pick;
                    gold.type = Image.Type.Sliced;
                    gold.pixelsPerUnitMultiplier = 40f / 33f;
                    gold.raycastTarget = false;
                    Anchor(gold.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(seg - 6f, 33f));
                    picks.Add(gold.gameObject);
                    var label = HallLabel(cell.transform, "Label", WorldOptionKeys[r][o], size, true, TextAnchor.MiddleCenter);
                    label.color = HubInk;
                    Anchor(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(seg - 4f, 36f));
                    label.resizeTextForBestFit = true;
                    label.resizeTextMinSize = 11;
                    label.resizeTextMaxSize = size;
                }

                var pig = CreateImage(row.transform, "PigMark", Color.white);
                pig.sprite = MenuArt("SandHub_PigMark");
                pig.raycastTarget = false;
                Anchor(pig.rectTransform, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(24f, 0f), new Vector2(26f, 26f));
                pig.gameObject.SetActive(false);
                pigs[r] = pig.gameObject;
            }

            // the foot: the piggy warning (an easier world) or the hint, and «По умолчанию»
            var warning = new GameObject("Warning", typeof(RectTransform));
            warning.transform.SetParent(frame, false);
            StretchFull((RectTransform)warning.transform);
            SandPiece(warning.transform, layout, "Warn");
            var warnText = HallText(warning.transform, layout, "WarnText", "#world_warn", 17, new Color(0.5f, 0.1f, 0.08f), null, true, TextAnchor.MiddleLeft);
            warnText.resizeTextForBestFit = true;
            warnText.resizeTextMinSize = 12;
            warnText.resizeTextMaxSize = 17;
            warnText.verticalOverflow = VerticalWrapMode.Truncate;
            warning.SetActive(false);
            var hint = HallText(frame, layout, "Hint", inGame ? "#world_hint_game" : "#world_hint", 22, new Color(0.47f, 0.33f, 0.17f), null, false, TextAnchor.MiddleLeft);
            var defaults = SandPiece(frame, layout, "Defaults");
            defaults.raycastTarget = true;
            var defaultsButton = defaults.gameObject.AddComponent<Button>();
            var defaultsText = HallText(frame, layout, "DefaultsText", "#world_defaults", 22, HubCream, HubWood);
            defaultsText.resizeTextForBestFit = true;
            defaultsText.resizeTextMinSize = 12;
            defaultsText.resizeTextMaxSize = 22;
            defaultsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            defaultsText.verticalOverflow = VerticalWrapMode.Truncate;

            var scroll = frame.gameObject.AddComponent<WorldSettingsScroll>();
            var so = new SerializedObject(scroll);
            so.FindProperty("inGame").boolValue = inGame;
            WriteArray(so.FindProperty("segments"), segments.ToArray());
            WriteArray(so.FindProperty("picks"), picks.ToArray());
            WriteArray(so.FindProperty("pigMarks"), pigs);
            WriteArray(so.FindProperty("rowGroups"), groups);
            so.FindProperty("warning").objectReferenceValue = warning;
            so.FindProperty("hint").objectReferenceValue = hint.gameObject;
            so.FindProperty("defaultsButton").objectReferenceValue = defaultsButton;
            so.ApplyModifiedPropertiesWithoutUndo();
            return scroll;
        }

        /// <summary>
        /// The purchase banner (the plague and fire banners' build, approved 2026-11-17): a dim over everything, the
        /// wood window with the King and the Queen, the teal ribbon, what it opens, «Открыть за …», «Закрыть». While it
        /// is up `hide` is hidden (the screen's front). Built last under `parent`, so it lies over the rest.
        /// </summary>
        private static FreedomBanner BuildFreedomBanner(Transform parent, HallLayout layout, CanvasGroup[] hide, bool overlay)
        {
            var root = new GameObject("FreedomBanner", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            StretchFull((RectTransform)root.transform);
            if (overlay) root.AddComponent<HudOverlay>();
            var dim = CreateImage(root.transform, "Dim", new Color(0f, 0f, 0f, 0.55f));
            StretchBleed(dim.rectTransform);     // takes every tap behind the window
            var frame = HallFrame(root.transform, "Window");
            SandPiece(frame, layout, "Window", "Freedom_");
            SandPiece(frame, layout, "Band", "Freedom_");
            HallText(frame, layout, "BTitle", "#freedom_title", 60, HubCream, new Color(0.03f, 0.2f, 0.25f));
            HallText(frame, layout, "BLead", "#freedom_lead", 30, HubInk, null);
            HallText(frame, layout, "BBody", "#freedom_body", 24, HubInk, null, false);
            HallText(frame, layout, "BNote", "#freedom_note", 22, new Color(0.42f, 0.28f, 0.14f), null, false);
            var buy = SandPiece(frame, layout, "Buy", "Freedom_");
            buy.raycastTarget = true;
            var buyButton = buy.gameObject.AddComponent<Button>();
            var buyLabel = HallText(frame, layout, "BBuy", null, 40, new Color(0.32f, 0.18f, 0.03f), null);
            var close = HallText(frame, layout, "BClose", "#ui_close", 32, HubCream, HubWood);
            close.raycastTarget = true;
            var closeButton = close.gameObject.AddComponent<Button>();

            var banner = root.AddComponent<FreedomBanner>();
            var so = new SerializedObject(banner);
            so.FindProperty("root").objectReferenceValue = root;
            so.FindProperty("buyLabel").objectReferenceValue = buyLabel;
            so.FindProperty("buyButton").objectReferenceValue = buyButton;
            so.FindProperty("closeButton").objectReferenceValue = closeButton;
            WriteArray(so.FindProperty("hide"), hide);
            so.ApplyModifiedPropertiesWithoutUndo();
            return banner;
        }

        // ---------------------------------------------------------------- the sandbox screen

        private static CanvasGroup BuildSandboxScreen(Transform canvas, MainMenuController controller, out SandboxScreenController sandbox,
            out Button settings, out CanvasGroup front)
        {
            var layout = SandLayout();
            var screen = StretchedChild(canvas, "SandboxScreen");
            // everything the purchase banner hides, in one group
            var frontRect = StretchedChild(screen, "Front");
            front = Group(frontRect.gameObject);
            var f = (Transform)frontRect;

            SandPiece(f, layout, "Frame");
            var back = Corner(f, "BackButton", "Back", new Vector2(0f, 1f), new Vector2(CornerInset, -CornerInset), CornerButton);
            var info = Corner(f, "InfoButton", "Info", new Vector2(1f, 1f), new Vector2(-CornerInset - CornerButton - 20f, -CornerInset), CornerButton);
            settings = Corner(f, "SettingsButton", "Settings", new Vector2(1f, 1f), new Vector2(-CornerInset, -CornerInset), CornerButton);
            UnityEventTools.AddPersistentListener(back.onClick, controller.Back);
            UnityEventTools.AddIntPersistentListener(info.onClick, controller.ShowInfo, (int)GameMode.Sandbox);

            // the seasons as wax seals, the brass plate under the picked one
            var plate = SandPiece(f, layout, "Plate");
            var season = HallText(plate.transform, layout, "Season", null, 26, new Color(0.27f, 0.16f, 0.04f), null);
            season.rectTransform.anchoredPosition = Vector2.zero;
            var row = new GameObject("Themes", typeof(RectTransform));
            row.transform.SetParent(f, false);
            Anchor((RectTransform)row.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));
            var themes = row.AddComponent<ThemePicker>();
            var buttons = new Object[MapThemes.All.Length];
            var picked = new Object[MapThemes.All.Length];
            var seals = new Object[MapThemes.All.Length];
            for (var i = 0; i < MapThemes.All.Length; i++)
            {
                var id = MapThemes.Id(MapThemes.All[i]);
                var wax = CreateImage(row.transform, $"Theme_{id}", Color.white);
                wax.sprite = MenuArt("InvHall_Seal");
                Anchor(wax.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), HallPoint(layout, $"Seal{i}"), new Vector2(84f, 84f));
                buttons[i] = wax.gameObject.AddComponent<Button>();
                seals[i] = wax.rectTransform;
                var sign = MenuGlyph(wax.transform, $"Theme_{id}", HudInk.Keep, 52f);
                sign.color = new Color(0.93f, 0.67f, 0.55f);
                sign.rectTransform.anchoredPosition = Vector2.zero;
                var lit = CreateImage(wax.transform, "Picked", Color.white);
                lit.sprite = MenuArt("InvHall_SealPicked");
                lit.raycastTarget = false;
                Anchor(lit.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(176f, 176f));
                var litSign = MenuGlyph(lit.transform, $"Theme_{id}", HudInk.Keep, 62f);
                litSign.color = new Color(1f, 0.96f, 0.84f);
                litSign.rectTransform.anchoredPosition = Vector2.zero;
                lit.gameObject.SetActive(false);
                picked[i] = lit.gameObject;
            }
            var tso = new SerializedObject(themes);
            tso.FindProperty("invasion").boolValue = false;
            WriteArray(tso.FindProperty("buttons"), buttons);
            WriteArray(tso.FindProperty("picked"), picked);
            tso.ApplyModifiedPropertiesWithoutUndo();

            // the chart (InkMapPainter, as wide as its frame: open sea at the sides) under its gilt frame
            var ids = MapSelector.PlayerMapIds;
            var chart = CreateImage(f, "Chart", new Color(0.12f, 0.08f, 0.05f));
            var chartBox = HallFind(layout.texts, "Chart");
            if (chartBox != null) HallPlace(chart.rectTransform, chartBox.rect);
            if (ids.Length > 0)
            {
                var plain = MenuArt($"Map_{ids[0]}");
                if (plain != null) { chart.sprite = plain; chart.color = Color.white; }
            }
            var card = chart.gameObject.AddComponent<Button>();
            card.transition = Selectable.Transition.None;
            SandPiece(f, layout, "ChartFrame");
            SandPiece(f, layout, "NameTag");
            HallText(f, layout, "MapName", ids.Length > 0 ? MapSelectPanelController.NameKey(ids[0]) : null, 28, HubInk, null);
            var noMaps = HallText(f, layout, "Chart", "#sandbox_no_maps", 32, HubCream, HubWood);
            noMaps.name = "NoMaps";
            var aspect = HallFind(layout.points, "ChartAspect");

            // the map's code: the field, its words, the input, the dice, the error over it
            SandPiece(f, layout, "Quill");
            SandPiece(f, layout, "CodeField");
            HallText(f, layout, "CodeLabel", "#sandbox_code_label", 28, HubInk, null, true, TextAnchor.MiddleLeft);
            var mapCode = CreateInputField(f, "MapCode", string.Empty, Vector2.zero, new Vector2(500f, 60f), null);
            mapCode.GetComponent<Image>().color = Color.clear;
            var codeBox = HallFind(layout.texts, "CodeInput");
            if (codeBox != null) HallPlace((RectTransform)mapCode.transform, codeBox.rect);
            mapCode.contentType = InputField.ContentType.Standard;
            mapCode.characterLimit = 16;
            mapCode.textComponent.alignment = TextAnchor.MiddleCenter;
            mapCode.textComponent.color = HubInk;
            mapCode.textComponent.fontSize = 32;
            mapCode.textComponent.font = UiTheme.Bold;
            var codePlaceholder = (Text)mapCode.placeholder;
            codePlaceholder.alignment = TextAnchor.MiddleCenter;
            Localize(codePlaceholder, "#sandbox_code_hint");
            var dice = SandPiece(f, layout, "Random");
            dice.raycastTarget = true;
            var random = dice.gameObject.AddComponent<Button>();
            var codeError = HallText(f, layout, "CodeError", null, 22, UiTheme.Danger, new Color(0f, 0f, 0f, 0.6f));
            codeError.gameObject.SetActive(false);

            // the settings' scroll
            BuildWorldScroll(f, layout, false);

            // the foot: the saves with their count, «Начать» breathing
            var savesPiece = SandPiece(f, layout, "Saves");
            savesPiece.raycastTarget = true;
            var saves = savesPiece.gameObject.AddComponent<Button>();
            UnityEventTools.AddPersistentListener(saves.onClick, controller.OpenSaves);
            HallText(f, layout, "SavesText", "#sandbox_saves", 30, HubCream, HubWood);
            var badge = SandPiece(f, layout, "Badge");
            var badgeCount = HallText(badge.transform, layout, "BadgeText", null, 26, Color.white, HubWood);
            badgeCount.rectTransform.anchoredPosition = Vector2.zero;
            var startPiece = SandPiece(f, layout, "Start");
            startPiece.raycastTarget = true;
            var start = startPiece.gameObject.AddComponent<Button>();
            var startText = HallText(startPiece.transform, layout, "StartText", "#sandbox_start", 46, HubCream, new Color(0.4f, 0.22f, 0.02f));
            var startBox = HallFind(layout.pieces, "Start");
            var textBox = HallFind(layout.texts, "StartText");
            if (startBox != null && textBox != null)
                startText.rectTransform.anchoredPosition = new Vector2(
                    (textBox.rect[0] + textBox.rect[2] - startBox.rect[0] - startBox.rect[2]) * 0.5f,
                    -(textBox.rect[1] + textBox.rect[3] - startBox.rect[1] - startBox.rect[3]) * 0.5f);
            startPiece.gameObject.AddComponent<ButtonBreath>();

            // the ribbon with the mode's name, over the frame's top
            SandPiece(f, layout, "Ribbon");
            HallText(f, layout, "Title", "#mode_sandbox", 58, HubCream, new Color(0.24f, 0.12f, 0.03f));

            sandbox = screen.gameObject.AddComponent<SandboxScreenController>();
            var so = new SerializedObject(sandbox);
            so.FindProperty("themes").objectReferenceValue = themes;
            so.FindProperty("startButton").objectReferenceValue = start;
            so.FindProperty("mapCode").objectReferenceValue = mapCode;
            so.FindProperty("mapCodeError").objectReferenceValue = codeError;
            so.FindProperty("randomCodeButton").objectReferenceValue = random;
            so.FindProperty("noMapsLabel").objectReferenceValue = noMaps.gameObject;
            so.FindProperty("chartAspect").floatValue = aspect != null ? aspect.at[0] / aspect.at[1] : 2f;
            so.FindProperty("seasonPlate").objectReferenceValue = plate.rectTransform;
            so.FindProperty("seasonText").objectReferenceValue = season;
            WriteArray(so.FindProperty("seals"), seals);
            so.FindProperty("savesBadge").objectReferenceValue = badge.gameObject;
            so.FindProperty("savesCount").objectReferenceValue = badgeCount;
            WriteStrings(so.FindProperty("mapIds"), ids);
            WriteArray(so.FindProperty("cards"), ids.Length > 0 ? new Object[] { card } : new Object[0]);
            WriteArray(so.FindProperty("pictures"), ids.Length > 0 ? new Object[] { chart } : new Object[0]);
            WriteArray(so.FindProperty("pickedFrames"), new Object[0]);
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

        // ---------------------------------------------------------------- in the game

        /// <summary>
        /// The game menu's «Настройки мира» (under «Сохранить», sandbox only): the same scroll over a dim, centred,
        /// editing the running world, with a round ✕; the purchase banner over it. Returns the panel's root.
        /// </summary>
        private static GameObject BuildWorldPanel(Transform uiRoot)
        {
            var layout = SandLayout();
            var root = new GameObject("WorldPanel", typeof(RectTransform));
            root.transform.SetParent(uiRoot, false);
            StretchFull((RectTransform)root.transform);
            root.AddComponent<HudOverlay>();
            var front = Group(root);
            var dim = CreateImage(root.transform, "Dim", new Color(0f, 0f, 0f, 0.5f));
            StretchBleed(dim.rectTransform);
            var scroll = BuildWorldScroll(root.transform, layout, true);
            // the scroll centred on the screen
            var box = HallFind(layout.pieces, "Scroll");
            if (box != null)
                ((RectTransform)scroll.transform).anchoredPosition = new Vector2(960f - (box.rect[0] + box.rect[2]) * 0.5f, (box.rect[1] + box.rect[3]) * 0.5f - 540f);
            var close = CreateGlyphButton(root.transform, "CloseButton", "round", HudInk.OnRound, "Cancel", null, 84f);
            if (box != null)
                Anchor((RectTransform)close.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2((box.rect[2] - box.rect[0]) * 0.5f - 40f, (box.rect[3] - box.rect[1]) * 0.5f - 40f), new Vector2(84f, 84f));
            UnityEventTools.AddBoolPersistentListener(close.onClick, root.SetActive, false);
            BuildFreedomBanner(uiRoot, layout, new[] { front }, true);
            root.SetActive(false);
            return root;
        }
    }
}
