using System;
using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The settings card: the language, how the fog of war looks (see GameSettings.FogStyle), and
    /// how the HUD looks (GameSettings.HudSkin -- four looks, see HudSkinApplier). The maps' season
    /// and the Invasion's map are the menu's theme buttons now (the user, 2026-10-25).
    /// The language buttons are built at play time from whatever columns the localization sheet
    /// has, so adding a third language is a column in the spreadsheet and nothing here.
    ///
    /// Each language is labelled in ITSELF ("Русский", "English"), never translated: a player who
    /// opened this screen because the game is in a language they cannot read needs to recognise
    /// their own, and a translated list is exactly the wrong thing to hand them.
    /// </summary>
    public class SettingsPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform languageRow;
        [SerializeField] private RectTransform fogRow;
        [SerializeField] private RectTransform hudRow;
        [SerializeField] private RectTransform graphicsRow;
        [SerializeField] private Sprite buttonSprite;
        // the manual graphics settings (the user, 2026-11-10): the rows under the Auto/Manual row,
        // a caption to copy for their headers (the card's own look), and the card for the "!" window
        [SerializeField] private RectTransform graphicsDetails;
        [SerializeField] private Text labelTemplate;
        [SerializeField] private Image cardImage;

        private static readonly Color SelectedColor = UiTheme.TabSelected;
        private static readonly Color UnselectedColor = UiTheme.TabIdle;

        /// <summary>A language's name in its own language. A code with no entry here shows as the code itself, which is still recognisable.</summary>
        private static readonly Dictionary<string, string> NativeNames = new Dictionary<string, string>
        {
            { "ru", "Русский" },
            { "en", "English" },
        };

        private readonly List<(string code, Image background)> _buttons = new List<(string, Image)>();
        private readonly List<(FogStyle style, Image background)> _fogButtons = new List<(FogStyle, Image)>();
        private readonly List<(HudSkin skin, Image background)> _hudButtons = new List<(HudSkin, Image)>();
        private readonly List<(bool manual, Image background)> _graphicsButtons = new List<(bool, Image)>();
        // every button of the detail rows with "is it the chosen one" -- rebuilt with the rows
        private readonly List<(Func<bool> selected, Image background)> _detailButtons = new List<(Func<bool>, Image)>();
        private GameObject _info;
        private LocalizedText _infoTitle, _infoBody;

        private static readonly (GraphicsTier tier, string key)[] Levels =
        {
            (GraphicsTier.Low, "#settings_graphics_low"),
            (GraphicsTier.Medium, "#settings_graphics_medium"),
            (GraphicsTier.High, "#settings_graphics_high"),
        };
        /// <summary>The HUD's looks in the order their buttons stand -- А, В, Г, Д of the mockups -- each with its caption's sheet key.</summary>
        private static readonly (HudSkin skin, string key)[] HudSkinChoices =
        {
            (HudSkin.Classic, "#settings_hud_classic"),
            (HudSkin.Soft, "#settings_hud_soft"),
            (HudSkin.Glass, "#settings_hud_glass"),
            (HudSkin.Dock, "#settings_hud_dock"),
        };

        /// <summary>The fog styles in the order their buttons stand, each with the sheet key of its caption.</summary>
        private static readonly (FogStyle style, string key)[] FogStyles =
        {
            (FogStyle.Clouds, "#settings_fog_clouds"),
            (FogStyle.Parchment, "#settings_fog_parchment"),
        };

        public void OpenPanel()
        {
            if (_buttons.Count == 0) BuildLanguageButtons();
            if (_fogButtons.Count == 0) BuildFogButtons();
            if (_hudButtons.Count == 0) BuildHudButtons();
            if (_graphicsButtons.Count == 0) BuildGraphicsButtons();
            BuildGraphicsDetails();
            HighlightSelected();
            if (panelRoot != null) panelRoot.SetActive(true);
            ModalGate.SetBlocked(true);
        }

        public void ClosePanel()
        {
            if (_info != null) _info.SetActive(false);
            if (panelRoot != null) panelRoot.SetActive(false);
            ModalGate.SetBlocked(false);
        }

        public void SelectLanguage(string code)
        {
            Localization.SetLanguage(code);
            BuildGraphicsDetails();   // the Auto line is formatted, not a LocalizedText
            HighlightSelected();
        }

        public void SelectFogStyle(FogStyle style)
        {
            GameSettings.FogStyle = style;
            HighlightSelected();
        }

        public void SelectHudSkin(HudSkin skin)
        {
            GameSettings.HudSkin = skin;
            HighlightSelected();
        }

        /// <summary>Auto or Manual; Manual opens the level and the details under the row.</summary>
        public void SelectGraphicsManual(bool manual)
        {
            GraphicsQuality.SetManual(manual);
            BuildGraphicsDetails();
            HighlightSelected();
        }

        private void BuildGraphicsButtons()
        {
            if (graphicsRow == null) return;

            const float width = 260f;
            const float spacing = 16f;
            var choices = new[] { (false, "#settings_graphics_auto"), (true, "#settings_graphics_manual") };
            for (var i = 0; i < choices.Length; i++)
            {
                var (manual, key) = choices[i];
                var x = (i - (choices.Length - 1) * 0.5f) * (width + spacing);
                var image = CreateRowButton(graphicsRow, manual ? "Graphics_Manual" : "Graphics_Auto", x, width, out var label);
                label.gameObject.AddComponent<LocalizedText>().SetKey(key);
                image.GetComponent<Button>().onClick.AddListener(() => SelectGraphicsManual(manual));
                _graphicsButtons.Add((manual, image));
            }
        }

        // ---- the rows under Auto/Manual -------------------------------------------------------

        /// <summary>Auto: one line of what the device draws. Manual: the level, then each detail, each with its "!".</summary>
        private void BuildGraphicsDetails()
        {
            if (graphicsDetails == null || labelTemplate == null) return;
            for (var i = graphicsDetails.childCount - 1; i >= 0; i--) Destroy(graphicsDetails.GetChild(i).gameObject);
            _detailButtons.Clear();

            if (!GraphicsQuality.Manual)
            {
                var line = Caption(graphicsDetails, "AutoNow", 46f);
                line.text = string.Format(Localization.Get("#settings_gfx_auto_now"), Localization.Get(LevelKey(GraphicsQuality.Detect())));
                return;
            }

            // the level: picking one sets every detail; a changed detail shows "custom"
            var levelRow = DetailRow("#settings_gfx_level", "#settings_gfx_level_info", 4);
            for (var i = 0; i < Levels.Length; i++)
            {
                var (tier, key) = Levels[i];
                AddChoice(levelRow, i, 4, key, null, () => GraphicsQuality.MatchingLevel == tier, () => GraphicsQuality.SetLevel(tier));
            }
            AddChoice(levelRow, 3, 4, "#settings_gfx_custom", null, () => GraphicsQuality.MatchingLevel == null, null);

            var fps = DetailRow("#settings_gfx_fps", "#settings_gfx_fps_info", GraphicsQuality.FpsChoices.Length);
            for (var i = 0; i < GraphicsQuality.FpsChoices.Length; i++)
            {
                var value = GraphicsQuality.FpsChoices[i];
                AddChoice(fps, i, GraphicsQuality.FpsChoices.Length, null, value.ToString(),
                    () => GraphicsQuality.Current.Fps == value, () => Change(d => d.Fps = value));
            }

            var msaa = DetailRow("#settings_gfx_msaa", "#settings_gfx_msaa_info", GraphicsQuality.MsaaChoices.Length);
            for (var i = 0; i < GraphicsQuality.MsaaChoices.Length; i++)
            {
                var value = GraphicsQuality.MsaaChoices[i];
                AddChoice(msaa, i, GraphicsQuality.MsaaChoices.Length, value <= 1 ? "#settings_gfx_off" : null, value <= 1 ? null : value + "x",
                    () => GraphicsQuality.Current.Msaa == value, () => Change(d => d.Msaa = value));
            }

            var scale = DetailRow("#settings_gfx_scale", "#settings_gfx_scale_info", GraphicsQuality.ScaleChoices.Length);
            for (var i = 0; i < GraphicsQuality.ScaleChoices.Length; i++)
            {
                var value = GraphicsQuality.ScaleChoices[i];
                AddChoice(scale, i, GraphicsQuality.ScaleChoices.Length, null, value + "%",
                    () => GraphicsQuality.Current.ScalePercent == value, () => Change(d => d.ScalePercent = value));
            }

            var shadowKeys = new[] { "#settings_gfx_off", "#settings_gfx_shadows_simple", "#settings_gfx_shadows_sharp" };
            var shadows = DetailRow("#settings_gfx_shadows", "#settings_gfx_shadows_info", shadowKeys.Length);
            for (var i = 0; i < shadowKeys.Length; i++)
            {
                var value = i;
                AddChoice(shadows, i, shadowKeys.Length, shadowKeys[i], null,
                    () => GraphicsQuality.Current.Shadows == value, () => Change(d => d.Shadows = value));
            }

            var reachKeys = new[] { "#settings_gfx_near", "#settings_gfx_mid", "#settings_gfx_far" };
            var reach = DetailRow("#settings_gfx_shadow_reach", "#settings_gfx_shadow_reach_info", reachKeys.Length);
            for (var i = 0; i < reachKeys.Length; i++)
            {
                var value = i;
                AddChoice(reach, i, reachKeys.Length, reachKeys[i], null,
                    () => GraphicsQuality.Current.ShadowReach == value, () => Change(d => d.ShadowReach = value));
            }

            var counter = DetailRow("#settings_gfx_show_fps", "#settings_gfx_show_fps_info", 2);
            AddChoice(counter, 0, 2, "#settings_gfx_on", null, () => GraphicsQuality.ShowFps, () => GraphicsQuality.ShowFps = true);
            AddChoice(counter, 1, 2, "#settings_gfx_off", null, () => !GraphicsQuality.ShowFps, () => GraphicsQuality.ShowFps = false);
        }

        private static string LevelKey(GraphicsTier tier)
        {
            foreach (var (t, key) in Levels) if (t == tier) return key;
            return "#settings_graphics_medium";
        }

        private void Change(Action<Box> edit)
        {
            var box = new Box { Detail = GraphicsQuality.Current };
            edit(box);
            GraphicsQuality.Current = box.Detail;   // saves and applies
        }

        /// <summary>A mutable holder, so a lambda can set one field of the struct.</summary>
        private sealed class Box
        {
            public GraphicsQuality.Detail Detail;
            public int Fps { set => Detail.Fps = value; }
            public int Msaa { set => Detail.Msaa = value; }
            public int ScalePercent { set => Detail.ScalePercent = value; }
            public int Shadows { set => Detail.Shadows = value; }
            public int ShadowReach { set => Detail.ShadowReach = value; }
        }

        /// <summary>A copy of the card's caption: same font, colour and skin.</summary>
        private Text Caption(Transform parent, string name, float height)
        {
            var text = Instantiate(labelTemplate, parent, false);
            text.name = name;
            var localized = text.GetComponent<LocalizedText>();
            if (localized != null) Destroy(localized);
            var rect = text.rectTransform;
            rect.sizeDelta = new Vector2(680f, height);
            return text;
        }

        /// <summary>A detail's header (name + "!") over an empty row for its buttons.</summary>
        private RectTransform DetailRow(string nameKey, string infoKey, int buttons)
        {
            var header = new GameObject(nameKey.TrimStart('#'), typeof(RectTransform));
            header.transform.SetParent(graphicsDetails, false);
            header.GetComponent<RectTransform>().sizeDelta = new Vector2(760f, 50f);
            var caption = Caption(header.transform, "Label", 50f);
            caption.gameObject.AddComponent<LocalizedText>().SetKey(nameKey);
            caption.rectTransform.anchoredPosition = Vector2.zero;

            var info = CreateRowButton(header.GetComponent<RectTransform>(), "Info", 330f, 50f, out var mark);
            info.rectTransform.sizeDelta = new Vector2(50f, 50f);
            mark.text = "!";
            mark.fontSize = 30;
            info.color = UnselectedColor;
            info.GetComponent<Button>().onClick.AddListener(() => ShowInfo(nameKey, infoKey));

            var row = new GameObject("Row", typeof(RectTransform));
            row.transform.SetParent(graphicsDetails, false);
            var rect = row.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(760f, 72f);
            return rect;
        }

        private void AddChoice(RectTransform row, int index, int count, string key, string literal, Func<bool> selected, Action pick)
        {
            const float spacing = 12f;
            var width = count >= 4 ? 170f : count == 3 ? 220f : 260f;
            var x = (index - (count - 1) * 0.5f) * (width + spacing);
            var image = CreateRowButton(row, $"Choice_{index}", x, width, out var label);
            if (key != null) label.gameObject.AddComponent<LocalizedText>().SetKey(key);
            else label.text = literal;
            if (pick != null)
                image.GetComponent<Button>().onClick.AddListener(() => { pick(); HighlightSelected(); });
            _detailButtons.Add((selected, image));
        }

        // ---- the "!" window ----------------------------------------------------------------

        private void ShowInfo(string titleKey, string bodyKey)
        {
            if (_info == null) BuildInfo();
            _infoTitle.SetKey(titleKey);
            _infoBody.SetKey(bodyKey);
            _info.SetActive(true);
            _info.transform.SetAsLastSibling();
        }

        private void BuildInfo()
        {
            var parent = panelRoot != null ? panelRoot.transform : transform;
            _info = new GameObject("GraphicsInfo", typeof(RectTransform), typeof(Image), typeof(Button));
            _info.transform.SetParent(parent, false);
            var rootRect = _info.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
            _info.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            _info.GetComponent<Button>().onClick.AddListener(() => _info.SetActive(false));

            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(_info.transform, false);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(720f, 460f);
            var image = card.GetComponent<Image>();
            if (cardImage != null)
            {
                image.sprite = cardImage.sprite;
                image.type = cardImage.type;
                image.color = cardImage.color;
            }

            var title = Caption(card.transform, "Title", 60f);
            title.fontSize = 34;
            title.rectTransform.anchoredPosition = new Vector2(0f, 170f);
            _infoTitle = title.gameObject.AddComponent<LocalizedText>();

            var body = Caption(card.transform, "Body", 250f);
            body.fontSize = 26;
            body.alignment = TextAnchor.MiddleCenter;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Truncate;
            body.rectTransform.sizeDelta = new Vector2(640f, 250f);
            body.rectTransform.anchoredPosition = new Vector2(0f, 10f);
            _infoBody = body.gameObject.AddComponent<LocalizedText>();

            var ok = CreateRowButton(cardRect, "Ok", 0f, 260f, out var okLabel);
            ok.rectTransform.anchoredPosition = new Vector2(0f, -170f);
            ok.color = SelectedColor;
            okLabel.gameObject.AddComponent<LocalizedText>().SetKey("#settings_info_ok");
            ok.GetComponent<Button>().onClick.AddListener(() => _info.SetActive(false));
        }

        private void BuildHudButtons()
        {
            if (hudRow == null) return;

            const float width = 170f;
            const float spacing = 12f;
            for (var i = 0; i < HudSkinChoices.Length; i++)
            {
                var (skin, key) = HudSkinChoices[i];
                var x = (i - (HudSkinChoices.Length - 1) * 0.5f) * (width + spacing);
                var image = CreateRowButton(hudRow, $"Hud_{skin}", x, width, out var label);
                label.gameObject.AddComponent<LocalizedText>().SetKey(key);
                var captured = skin;
                image.GetComponent<Button>().onClick.AddListener(() => SelectHudSkin(captured));
                _hudButtons.Add((skin, image));
            }
        }

        private void BuildFogButtons()
        {
            if (fogRow == null) return;

            const float width = 260f;
            const float spacing = 16f;
            for (var i = 0; i < FogStyles.Length; i++)
            {
                var (style, key) = FogStyles[i];
                var x = (i - (FogStyles.Length - 1) * 0.5f) * (width + spacing);
                var image = CreateRowButton(fogRow, $"Fog_{style}", x, width, out var label);
                // Translated, unlike the language names, and re-translated when the language changes.
                label.gameObject.AddComponent<LocalizedText>().SetKey(key);
                var captured = style;
                image.GetComponent<Button>().onClick.AddListener(() => SelectFogStyle(captured));
                _fogButtons.Add((style, image));
            }
        }

        private void BuildLanguageButtons()
        {
            var config = Localization.Config;
            if (config == null || languageRow == null) return;

            const float width = 220f;
            const float spacing = 16f;
            var count = config.Languages.Count;

            for (var i = 0; i < count; i++)
            {
                var code = config.Languages[i];
                var x = (i - (count - 1) * 0.5f) * (width + spacing);
                _buttons.Add((code, CreateLanguageButton(code, x, width)));
            }
        }

        private Image CreateLanguageButton(string code, float x, float width)
        {
            var image = CreateRowButton(languageRow, $"Language_{code}", x, width, out var label);
            label.text = NativeNames.TryGetValue(code, out var native) ? native : code;
            var captured = code;
            image.GetComponent<Button>().onClick.AddListener(() => SelectLanguage(captured));
            return image;
        }

        private Image CreateRowButton(RectTransform row, string name, float x, float width, out Text label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(row, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(width, 72f);

            var image = go.GetComponent<Image>();
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;

            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGO.transform.SetParent(go.transform, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 6f);
            labelRect.offsetMax = new Vector2(-8f, -6f);

            label = labelGO.GetComponent<Text>();
            label.font = UiTheme.Regular;
            label.fontSize = 26;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UiTheme.Ink;

            go.GetComponent<Button>().targetGraphic = image;
            return image;
        }

        private void HighlightSelected()
        {
            foreach (var (code, background) in _buttons)
            {
                background.color = code == Localization.Language ? SelectedColor : UnselectedColor;
            }
            foreach (var (style, background) in _fogButtons)
            {
                background.color = style == GameSettings.FogStyle ? SelectedColor : UnselectedColor;
            }
            foreach (var (skin, background) in _hudButtons)
            {
                background.color = skin == GameSettings.HudSkin ? SelectedColor : UnselectedColor;
            }
            foreach (var (manual, background) in _graphicsButtons)
            {
                background.color = manual == GraphicsQuality.Manual ? SelectedColor : UnselectedColor;
            }
            foreach (var (selected, background) in _detailButtons)
            {
                if (background != null) background.color = selected() ? SelectedColor : UnselectedColor;
            }
        }
    }
}
