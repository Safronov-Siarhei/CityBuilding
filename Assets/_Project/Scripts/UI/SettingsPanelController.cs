using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The settings card. One setting today -- the language -- and the buttons for it are built at
    /// play time from whatever columns the localization sheet has, so adding a third language is a
    /// column in the spreadsheet and nothing here.
    ///
    /// Each language is labelled in ITSELF ("Русский", "English"), never translated: a player who
    /// opened this screen because the game is in a language they cannot read needs to recognise
    /// their own, and a translated list is exactly the wrong thing to hand them.
    /// </summary>
    public class SettingsPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform languageRow;
        [SerializeField] private Sprite buttonSprite;

        private static readonly Color SelectedColor = new Color(0.36f, 0.5f, 0.3f, 0.95f);
        private static readonly Color UnselectedColor = new Color(0.26f, 0.29f, 0.24f, 0.95f);

        /// <summary>A language's name in its own language. A code with no entry here shows as the code itself, which is still recognisable.</summary>
        private static readonly Dictionary<string, string> NativeNames = new Dictionary<string, string>
        {
            { "ru", "Русский" },
            { "en", "English" },
        };

        private readonly List<(string code, Image background)> _buttons = new List<(string, Image)>();

        public void OpenPanel()
        {
            if (_buttons.Count == 0) BuildLanguageButtons();
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
            var go = new GameObject($"Language_{code}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(languageRow, false);

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

            var label = labelGO.GetComponent<Text>();
            label.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 26;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = NativeNames.TryGetValue(code, out var native) ? native : code;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var captured = code;
            button.onClick.AddListener(() => SelectLanguage(captured));

            return image;
        }

        private void HighlightSelected()
        {
            foreach (var (code, background) in _buttons)
            {
                background.color = code == Localization.Language ? SelectedColor : UnselectedColor;
            }
        }
    }
}
