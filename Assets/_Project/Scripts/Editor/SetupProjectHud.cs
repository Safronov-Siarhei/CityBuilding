using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Resources;
using CityBuilder.Saving;
using CityBuilder.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// The HUD after the user's layout sketch (2026-10-14): pictures instead of words, and each
    /// piece on the edge it belongs to --
    ///
    ///   top-left      the menu (save, settings, main menu), which pauses the game, and the
    ///                 citizens button under it;
    ///   top-centre    one resource bar, the day under it;
    ///   top-right     happiness as a round number (tap for the breakdown), then migration, army;
    ///   left          the event feed;
    ///   bottom-left   the minimap;
    ///   bottom-centre the build menu -- swapped for cancel/rotate/confirm while placing;
    ///   bottom-right  the tax: minus, "Налог: N%" in words, plus.
    ///
    /// Everything here is anchored to its own corner or edge directly, so nothing needs
    /// SetupProjectLayout's centre-to-corner pinning.
    /// </summary>
    public static partial class SetupProject
    {
        private const float HudMargin = 28f;
        private const float RoundButton = 108f;
        private const float MenuButtonTop = -HudMargin * 0.7f;
        private const float WorkforceButtonSize = 96f;
        private const float WorkforceButtonTop = MenuButtonTop - RoundButton - 18f;
        /// <summary>Where the left column under the menu and the citizens button frees up -- the event feed starts here.</summary>
        private const float LeftColumnTop = WorkforceButtonTop - WorkforceButtonSize - 20f;

        /// <summary>One of Tools/ui/make_hud_glyphs.py's white silhouettes, or null if it was never drawn.</summary>
        private static Sprite HudGlyph(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{UiTextureImport.GlyphsFolder}/Hud_{name}.png");
        }

        private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>
        /// A round button with a picture on it and no words, drawn by the HUD's look (`role`, see
        /// Tools/ui/make_hud_skins.py); the white glyph takes the look's ink for that role.
        /// </summary>
        private static Button CreateGlyphButton(Transform parent, string name, string role, HudInk ink, string glyph, Sprite fallback, float size, float glyphScale = 0.56f)
        {
            var image = HudImage(parent, name, role, new Vector2(size, size), sliced: false);
            var button = image.gameObject.AddComponent<Button>();

            var icon = CreateImage(image.transform, "Icon", Color.white);
            icon.sprite = HudGlyph(glyph) ?? fallback;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            if (ink != HudInk.Keep) SkinInk(icon, ink);
            var iconRect = icon.rectTransform;
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, 3f);
            iconRect.sizeDelta = new Vector2(size * glyphScale, size * glyphScale);
            return button;
        }

        // ---------------------------------------------------------------- top-left: the menu

        /// <summary>
        /// The menu button and what it opens: a dim layer over the paused game with three round
        /// entries down from the button -- save, settings, home. Tapping the dim layer closes it.
        /// The save dialog and the settings card are the existing ones, opened over the menu.
        /// </summary>
        private static void BuildGameMenu(Transform uiRoot, Sprite panelSprite, HudHider hud, GameSaveController saveController,
            SaveDialogController saveDialog, GameObject saveDialogRoot)
        {
            var menuButton = CreateGlyphButton(uiRoot, "MenuButton", "round_main", HudInk.OnMain, "Menu", null, RoundButton);
            Anchor(menuButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(HudMargin, MenuButtonTop), new Vector2(RoundButton, RoundButton));

            var root = new GameObject("GameMenu", typeof(RectTransform));
            root.transform.SetParent(uiRoot, false);
            StretchFull((RectTransform)root.transform);
            root.AddComponent<HudOverlay>();

            var backdrop = CreateImage(root.transform, "Backdrop", new Color(0f, 0f, 0f, 0.45f));
            StretchFull(backdrop.rectTransform);
            var backdropButton = backdrop.gameObject.AddComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;

            // The menu's own button again, on top of the dim layer, so the same spot closes it.
            var closeButton = CreateGlyphButton(root.transform, "MenuCloseButton", "round_main", HudInk.OnMain, "Menu", null, RoundButton);
            Anchor(closeButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(HudMargin, MenuButtonTop), new Vector2(RoundButton, RoundButton));

            var entries = new[] { ("SaveEntry", "Save"), ("SettingsEntry", "Settings"), ("MainMenuEntry", "Home") };
            var buttons = new Button[entries.Length];
            for (var i = 0; i < entries.Length; i++)
            {
                buttons[i] = CreateGlyphButton(root.transform, entries[i].Item1, "round_alt", HudInk.OnAlt, entries[i].Item2, null, 96f);
                Anchor(buttons[i].GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(HudMargin + (RoundButton - 96f) * 0.5f, MenuButtonTop - RoundButton - 18f - i * 112f), new Vector2(96f, 96f));
            }

            var settings = BuildSettingsPanel(uiRoot, panelSprite, null);
            settings.gameObject.AddComponent<HudOverlay>();

            var controllerGO = new GameObject("GameMenuController");
            controllerGO.transform.SetParent(uiRoot, false);
            var controller = controllerGO.AddComponent<GameMenuController>();
            var so = new SerializedObject(controller);
            so.FindProperty("menuRoot").objectReferenceValue = root;
            so.FindProperty("hud").objectReferenceValue = hud;
            so.FindProperty("saveController").objectReferenceValue = saveController;
            so.FindProperty("saveDialog").objectReferenceValue = saveDialog;
            so.FindProperty("saveDialogRoot").objectReferenceValue = saveDialogRoot;
            so.FindProperty("settingsPanel").objectReferenceValue = settings;
            so.FindProperty("settingsPanelRoot").objectReferenceValue = settings.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(menuButton.onClick, controller.Toggle);
            UnityEventTools.AddPersistentListener(closeButton.onClick, controller.Close);
            UnityEventTools.AddPersistentListener(backdropButton.onClick, controller.Close);
            UnityEventTools.AddPersistentListener(buttons[0].onClick, controller.OpenSave);
            UnityEventTools.AddPersistentListener(buttons[1].onClick, controller.OpenSettings);
            UnityEventTools.AddPersistentListener(buttons[2].onClick, controller.GoToMainMenu);

            root.SetActive(false);
        }

        // ---------------------------------------------------------------- bottom-centre: placing

        /// <summary>
        /// Cancel, rotate, confirm -- in a row where the build menu stands, which steps aside while
        /// a building is in hand (BuildingPlacerUIVisibility). A layout group keeps the visible ones
        /// centred: rotate goes away for a road, cancel for the Town Hall, confirm for a mouse.
        /// </summary>
        private static (Button cancel, Button rotate, Button confirm, RectTransform row) BuildPlacementControls(Transform uiRoot)
        {
            var row = new GameObject("PlacementControls", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(uiRoot, false);
            var rowRect = (RectTransform)row.transform;
            Anchor(rowRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, HudMargin), new Vector2(520f, 140f));
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 44f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            var cancel = CreateGlyphButton(row.transform, "CancelPlacementButton", "round", HudInk.OnRound, "Cancel", CreateCancelIcon(), 112f);
            var rotate = CreateGlyphButton(row.transform, "RotateButton", "round", HudInk.OnRound, "Rotate", CreateRotateIcon(), 112f);
            // A neutral pill, because PlacementHudController tints it: green when it can be placed.
            var confirm = CreateGlyphButton(row.transform, "ConfirmPlacementButton", "round_tint", HudInk.Keep, "Check", CreateConfirmIcon(), 136f);
            confirm.transform.Find("Icon").GetComponent<Image>().color = Color.white;
            return (cancel, rotate, confirm, rowRect);
        }

        // ---------------------------------------------------------------- bottom-right: the tax

        /// <summary>
        /// Minus, "Налог: N%", plus -- on the bottom-right corner, where the thumb is. The one HUD
        /// control that keeps a word: a coin and a number alone did not say what they were (the
        /// user, 2026-10-14).
        /// </summary>
        private static void BuildTaxControl(Transform uiRoot)
        {
            const float sign = 76f, plaqueWidth = 232f, gap = 8f;
            var root = new GameObject("TaxRate", typeof(RectTransform));
            root.transform.SetParent(uiRoot, false);
            Anchor((RectTransform)root.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-HudMargin, HudMargin), new Vector2(sign * 2f + gap * 2f + plaqueWidth, 84f));

            var minus = CreateGlyphButton(root.transform, "MinusButton", "round_step", HudInk.Keep, "Minus", null, sign);
            Anchor(minus.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(sign, sign));
            var plus = CreateGlyphButton(root.transform, "PlusButton", "round_step", HudInk.Keep, "Plus", null, sign);
            Anchor(plus.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(sign, sign));
            foreach (var (button, glyph) in new[] { (minus, "–"), (plus, "+") })
            {
                // Signs, not words: the glyph files have no minus/plus, the font draws them crisply.
                Object.DestroyImmediate(button.transform.Find("Icon").gameObject);
                var label = CreateText(button.transform, "Sign", glyph, 44, new Vector2(0f, 4f), new Vector2(70f, 70f));
                label.font = UiTheme.Heavy;
                SkinInk(label, HudInk.OnStep);
            }

            var plaque = HudImage(root.transform, "RatePlaque", "plaque", new Vector2(plaqueWidth, 72f));
            var rateLabel = CreateText(plaque.transform, "RateLabel", "Налог: 10%", 28, new Vector2(0f, 3f), new Vector2(plaqueWidth - 24f, 56f));
            rateLabel.font = UiTheme.Heavy;
            SkinInk(rateLabel, HudInk.OnPlaque);

            var go = new GameObject("TaxRateController");
            go.transform.SetParent(uiRoot, false);
            var controller = go.AddComponent<TaxRateController>();
            var so = new SerializedObject(controller);
            so.FindProperty("rateLabel").objectReferenceValue = rateLabel;
            so.FindProperty("widget").objectReferenceValue = root;
            so.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(minus.onClick, controller.Decrease);
            UnityEventTools.AddPersistentListener(plus.onClick, controller.Increase);
        }

        // ---------------------------------------------------------------- top-right: happiness

        /// <summary>The percentage on a round parchment button; a tap opens the factor breakdown under the corner.</summary>
        private static void BuildHappiness(Transform uiRoot)
        {
            var button = CreateGlyphButton(uiRoot, "Happiness", "round_value", HudInk.Keep, "-", null, RoundButton);
            Object.DestroyImmediate(button.transform.Find("Icon").gameObject);
            Anchor(button.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-HudMargin, -HudMargin * 0.7f), new Vector2(RoundButton, RoundButton));
            var label = CreateText(button.transform, "HappinessLabel", "99%", 30, new Vector2(0f, 4f), new Vector2(RoundButton - 12f, 50f));
            label.font = UiTheme.Heavy;

            var details = CreateSkinImage(uiRoot, "HappinessBreakdown", "panel", Vector2.zero, Vector2.zero);
            Anchor(details.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-HudMargin, -HudMargin * 0.7f - RoundButton - 86f), new Vector2(430f, 112f));
            var breakdown = CreateText(details.transform, "BreakdownLabel", string.Empty, 19, new Vector2(0f, 2f), new Vector2(400f, 90f));
            breakdown.color = UiTheme.Ink;
            details.gameObject.SetActive(false);

            var go = new GameObject("HappinessController");
            go.transform.SetParent(uiRoot, false);
            var controller = go.AddComponent<HappinessController>();
            var so = new SerializedObject(controller);
            so.FindProperty("happinessLabel").objectReferenceValue = label;
            so.FindProperty("breakdownLabel").objectReferenceValue = breakdown;
            so.FindProperty("breakdownPanel").objectReferenceValue = details.gameObject;
            so.FindProperty("widget").objectReferenceValue = button.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(button.onClick, controller.ToggleBreakdown);
        }

        // ---------------------------------------------------------------- top-right in a run: the King

        private const string KingPortraitPath = UiTextureImport.IconsFolder + "/PortraitKing.png";

        /// <summary>
        /// The King's portrait for an Invasion run, in the corner the sandbox gives contentment
        /// (KingPortraitController shows it only in a run): his face in a round frame with his level
        /// on a badge, a plaque to its left with his health and experience bars, a gold ring round
        /// the frame while he is selected, a dark cover with a countdown while he is down, and a
        /// castle button under it that sends him back to guard the base.
        /// </summary>
        private static void BuildKingPortrait(Transform uiRoot)
        {
            var portraitSprite = LoadSprite(KingPortraitPath);
            var castleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Textures/Icon_Bld_Castle.asset");
            var circle = SkinSprite("circle_mask");

            var root = new GameObject("KingPortrait", typeof(RectTransform));
            root.transform.SetParent(uiRoot, false);
            // stays up while the King is selected and the rest of the HUD steps aside (HudHider)
            root.AddComponent<HudOverlay>();
            Anchor((RectTransform)root.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-HudMargin, -HudMargin * 0.7f), new Vector2(380f, 190f));

            // behind the frame: the selection ring
            var selected = CreateImage(root.transform, "Selected", UiTheme.Gold);
            selected.sprite = circle;
            Anchor(selected.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-RoundButton * 0.5f, -RoundButton * 0.5f), new Vector2(RoundButton + 18f, RoundButton + 18f));
            selected.raycastTarget = false;

            var frame = HudImage(root.transform, "Portrait", "round_value", new Vector2(RoundButton, RoundButton), sliced: false);
            Anchor(frame.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(RoundButton, RoundButton));
            var portraitButton = frame.gameObject.AddComponent<Button>();
            var mask = CreateImage(frame.transform, "Mask", UiTheme.Parchment);
            mask.sprite = circle;
            mask.raycastTarget = false;
            Anchor(mask.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(RoundButton - 16f, RoundButton - 16f));
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var face = CreateImage(mask.transform, "Face", Color.white);
            face.sprite = portraitSprite;
            face.preserveAspect = true;
            face.raycastTarget = false;
            StretchFull(face.rectTransform);

            var fallen = CreateImage(frame.transform, "Fallen", new Color(0.05f, 0.03f, 0.02f, 0.7f));
            fallen.sprite = circle;
            fallen.raycastTarget = false;
            Anchor(fallen.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(RoundButton - 16f, RoundButton - 16f));
            var fallenText = CreateText(fallen.transform, "Countdown", "8", 44, Vector2.zero, new Vector2(RoundButton, 60f), Color.white, addShadow: true);
            fallenText.font = UiTheme.Heavy;
            fallen.gameObject.SetActive(false);

            var badge = CreateImage(frame.transform, "LevelBadge", UiTheme.Wood);
            badge.sprite = circle;
            badge.raycastTarget = false;
            Anchor(badge.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(10f, 10f), new Vector2(42f, 42f));
            var level = CreateText(badge.transform, "Level", "1", 24, new Vector2(0f, 1f), new Vector2(42f, 36f), UiTheme.OnWood);
            level.font = UiTheme.Heavy;

            // left of the frame: the bars on a plaque
            var plaque = HudImage(root.transform, "Bars", "plaque", new Vector2(220f, 62f));
            Anchor(plaque.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-RoundButton - 8f, -RoundButton * 0.5f), new Vector2(220f, 62f));
            var health = Bar(plaque.transform, "Health", new Vector2(0f, 9f), 18f, new Color(0.2f, 0.62f, 0.22f));
            var xp = Bar(plaque.transform, "Experience", new Vector2(0f, -13f), 10f, UiTheme.Gold);

            // under the frame: back to guarding the base, marked while he guards
            var guardMark = CreateImage(root.transform, "Guarding", UiTheme.Gold);
            guardMark.sprite = circle;
            guardMark.raycastTarget = false;
            Anchor(guardMark.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-RoundButton * 0.5f, -RoundButton - 44f), new Vector2(76f, 76f));
            var guard = CreateGlyphButton(root.transform, "GuardButton", "round_value", HudInk.Keep, null, castleSprite, 66f, 0.7f);
            Anchor(guard.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-RoundButton * 0.5f, -RoundButton - 44f), new Vector2(66f, 66f));

            var go = new GameObject("KingPortraitController");
            go.transform.SetParent(uiRoot, false);
            var controller = go.AddComponent<KingPortraitController>();
            var so = new SerializedObject(controller);
            so.FindProperty("widget").objectReferenceValue = root;
            so.FindProperty("levelText").objectReferenceValue = level;
            so.FindProperty("healthFill").objectReferenceValue = health;
            so.FindProperty("xpFill").objectReferenceValue = xp;
            so.FindProperty("fallenOverlay").objectReferenceValue = fallen.gameObject;
            so.FindProperty("fallenText").objectReferenceValue = fallenText;
            so.FindProperty("selectedFrame").objectReferenceValue = selected.gameObject;
            so.FindProperty("guardMark").objectReferenceValue = guardMark.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(portraitButton.onClick, controller.SelectKing);
            UnityEventTools.AddPersistentListener(guard.onClick, controller.GuardBase);
            root.SetActive(false);   // KingPortraitController turns it on in a run
        }

        /// <summary>A dark track with a coloured fill whose right edge the controller moves.</summary>
        private static RectTransform Bar(Transform parent, string name, Vector2 position, float height, Color colour)
        {
            var track = CreateImage(parent, name, new Color(0.17f, 0.11f, 0.07f, 0.9f));
            track.raycastTarget = false;
            Anchor(track.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(190f, height));
            var fill = CreateImage(track.transform, "Fill", colour);
            fill.raycastTarget = false;
            var rect = fill.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(2f, 2f);
            rect.offsetMax = new Vector2(-2f, -2f);
            return rect;
        }

        /// <summary>A sprite from UI/Icons (UiTextureImport makes every PNG there a single UI sprite).</summary>
        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogWarning($"[SetupProject] {path} is missing -- run Tools/blender/render_portrait.py.");
            return sprite;
        }

        // ---------------------------------------------------------------- top-centre: day and citizens

        /// <summary>"День N" under the resource bar, in light letters with a shadow over the world -- no plaque, as sketched.</summary>
        private static void BuildDayLabel(Transform uiRoot, GameCalendar gameCalendar)
        {
            var statusText = CreateText(uiRoot, "SettlementStatus", string.Empty, 28, Vector2.zero, new Vector2(320f, 44f), new Color(1f, 0.98f, 0.92f), addShadow: true);
            statusText.font = UiTheme.Bold;
            Anchor(statusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(320f, 44f));
            var shadow = statusText.GetComponent<Shadow>();
            if (shadow != null)
            {
                shadow.effectColor = new Color(0.18f, 0.1f, 0.04f, 0.85f);
                shadow.effectDistance = new Vector2(2f, -2f);
            }

            var go = new GameObject("SettlementStatusController");
            go.transform.SetParent(uiRoot, false);
            var controller = go.AddComponent<SettlementStatusController>();
            var so = new SerializedObject(controller);
            so.FindProperty("gameCalendar").objectReferenceValue = gameCalendar;
            so.FindProperty("statusText").objectReferenceValue = statusText;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
