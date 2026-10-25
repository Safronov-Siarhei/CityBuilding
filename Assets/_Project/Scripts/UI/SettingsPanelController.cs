using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The settings card: the language, how the fog of war looks (see GameSettings.FogStyle), and
    /// how the HUD looks (GameSettings.HudSkin -- four looks, see HudSkinApplier), and the maps'
    /// season (GameSettings.MapSeason, Maps.SeasonStyle; taken at the next map's load).
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
        [SerializeField] private RectTransform seasonRow;
        [SerializeField] private RectTransform invasionMapRow;
        [SerializeField] private Sprite buttonSprite;

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
        private readonly List<(MapSeason season, Image background)> _seasonButtons = new List<(MapSeason, Image)>();
        private readonly List<(InvasionMap map, Image background)> _invasionMapButtons = new List<(InvasionMap, Image)>();

        private static readonly (InvasionMap map, string key)[] InvasionMaps =
        {
            (InvasionMap.Island, "#settings_invasion_map_island"),
            (InvasionMap.NewYear, "#settings_invasion_map_newyear"),
        };

        /// <summary>The seasons in the order their buttons stand, each with its caption's sheet key.</summary>
        private static readonly (MapSeason season, string key)[] Seasons =
        {
            (MapSeason.Spring, "#settings_season_spring"),
            (MapSeason.Summer, "#settings_season_summer"),
            (MapSeason.Autumn, "#settings_season_autumn"),
            (MapSeason.Winter, "#settings_season_winter"),
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
            if (_seasonButtons.Count == 0) BuildSeasonButtons();
            if (_invasionMapButtons.Count == 0) BuildInvasionMapButtons();
            HighlightSelected();
            if (panelRoot != null) panelRoot.SetActive(true);
            ModalGate.SetBlocked(true);
        }

        public void ClosePanel()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            ModalGate.SetBlocked(false);
        }

        public void SelectLanguage(string code)
        {
            Localization.SetLanguage(code);
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

        public void SelectSeason(MapSeason season)
        {
            GameSettings.MapSeason = season;
            HighlightSelected();
        }

        public void SelectInvasionMap(InvasionMap map)
        {
            GameSettings.InvasionMap = map;
            HighlightSelected();
        }

        private void BuildInvasionMapButtons()
        {
            if (invasionMapRow == null) return;

            const float width = 260f;
            const float spacing = 16f;
            for (var i = 0; i < InvasionMaps.Length; i++)
            {
                var (map, key) = InvasionMaps[i];
                var x = (i - (InvasionMaps.Length - 1) * 0.5f) * (width + spacing);
                var image = CreateRowButton(invasionMapRow, $"InvasionMap_{map}", x, width, out var label);
                label.gameObject.AddComponent<LocalizedText>().SetKey(key);
                var captured = map;
                image.GetComponent<Button>().onClick.AddListener(() => SelectInvasionMap(captured));
                _invasionMapButtons.Add((map, image));
            }
        }

        private void BuildSeasonButtons()
        {
            if (seasonRow == null) return;

            const float width = 170f;
            const float spacing = 12f;
            for (var i = 0; i < Seasons.Length; i++)
            {
                var (season, key) = Seasons[i];
                var x = (i - (Seasons.Length - 1) * 0.5f) * (width + spacing);
                var image = CreateRowButton(seasonRow, $"Season_{season}", x, width, out var label);
                label.gameObject.AddComponent<LocalizedText>().SetKey(key);
                var captured = season;
                image.GetComponent<Button>().onClick.AddListener(() => SelectSeason(captured));
                _seasonButtons.Add((season, image));
            }
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
            foreach (var (map, background) in _invasionMapButtons)
            {
                background.color = map == GameSettings.InvasionMap ? SelectedColor : UnselectedColor;
            }
            foreach (var (season, background) in _seasonButtons)
            {
                background.color = season == GameSettings.MapSeason ? SelectedColor : UnselectedColor;
            }
            foreach (var (style, background) in _fogButtons)
            {
                background.color = style == GameSettings.FogStyle ? SelectedColor : UnselectedColor;
            }
            foreach (var (skin, background) in _hudButtons)
            {
                background.color = skin == GameSettings.HudSkin ? SelectedColor : UnselectedColor;
            }
        }
    }
}
