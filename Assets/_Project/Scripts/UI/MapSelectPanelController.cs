using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Maps;
using CityBuilder.Saving;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The new-game map picker: one row per MapSelector.Choosable map (name and a line about it),
    /// the newest selected to begin with, and a "Play" button that starts on the selected one.
    /// Same select-then-confirm shape as the save browser, so a stray tap never loads a scene.
    /// </summary>
    public class MapSelectPanelController : MonoBehaviour
    {
        [SerializeField] private string gameplaySceneName = "CityBuilder";
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform listContent;
        [SerializeField] private Button playButton;
        [SerializeField] private Sprite rowSprite;
        /// <summary>On in the main menu: rows take the menu's look (MenuRowLook) instead of the tinted rowSprite.</summary>
        [SerializeField] private bool menuLook;

        private MenuRowLook _look;

        /// <summary>A picture of each map from above (MenuArtBake), shown at the row's left; a map with none gets a text-only row.</summary>
        [SerializeField] private string[] previewMapIds = new string[0];
        [SerializeField] private Sprite[] previews = new Sprite[0];

        private static readonly Color NormalRowColor = UiTheme.RowNormal;
        private static readonly Color SelectedRowColor = UiTheme.RowSelected;

        private readonly List<(MeshMapDefinition map, Image image)> _rows = new List<(MeshMapDefinition, Image)>();
        private MeshMapDefinition _selected;

        public static string NameKey(string mapId) => $"#map_name_{mapId.ToLowerInvariant()}";
        public static string DescriptionKey(string mapId) => $"#map_desc_{mapId.ToLowerInvariant()}";

        public void OpenPanel()
        {
            Populate();
            if (panelRoot != null) panelRoot.SetActive(true);
        }

        public void ClosePanel()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        public void ConfirmPlay()
        {
            if (_selected == null) return;
            MapSelector.MarkChosen(_selected);
            GameSessionIntent.SaveNameToLoad = null;
            GameSessionIntent.NewGameMapId = _selected.MapId;
            LoadingScreen.IntoGame(gameplaySceneName);
        }

        private void Populate()
        {
            for (var i = listContent.childCount - 1; i >= 0; i--)
            {
                Destroy(listContent.GetChild(i).gameObject);
            }
            _rows.Clear();
            _look = menuLook ? MenuRowLook.Current() : null;

            var maps = MapSelector.Choosable();
            foreach (var map in maps)
            {
                CreateRow(map);
            }
            Select(maps.Count > 0 ? maps[0] : null);
        }

        private void CreateRow(MeshMapDefinition map)
        {
            var rowGO = new GameObject($"Map_{map.MapId}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            rowGO.transform.SetParent(listContent, false);

            var preview = PreviewOf(map.MapId);
            var rowHeight = preview != null ? 150f : 120f;
            var layoutElement = rowGO.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = rowHeight;
            layoutElement.minHeight = rowHeight;

            var image = rowGO.GetComponent<Image>();
            image.sprite = rowSprite;
            image.type = Image.Type.Sliced;
            image.color = NormalRowColor;
            if (_look != null) _look.Style(image, false);

            var textGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(rowGO.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(preview != null ? 230f : 28f, 6f);
            textRect.offsetMax = new Vector2(-28f, -6f);

            if (preview != null)
            {
                var pictureGO = new GameObject("Preview", typeof(RectTransform), typeof(Image), typeof(Outline));
                pictureGO.transform.SetParent(rowGO.transform, false);
                var pictureRect = pictureGO.GetComponent<RectTransform>();
                pictureRect.anchorMin = pictureRect.anchorMax = new Vector2(0f, 0.5f);
                pictureRect.pivot = new Vector2(0f, 0.5f);
                pictureRect.anchoredPosition = new Vector2(22f, 0f);
                pictureRect.sizeDelta = new Vector2(190f, 118f);
                var picture = pictureGO.GetComponent<Image>();
                picture.sprite = preview;
                picture.preserveAspect = false;
                picture.raycastTarget = false;
                var outline = pictureGO.GetComponent<Outline>();
                outline.effectColor = new Color(0.16f, 0.09f, 0.03f, 0.85f);
                outline.effectDistance = new Vector2(3f, -3f);
            }

            var text = textGO.GetComponent<Text>();
            text.font = UiTheme.Regular;
            text.fontSize = 24;
            text.supportRichText = true;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = _look != null ? _look.Ink : UiTheme.Ink;
            text.text = $"<size=30><b>{Localization.Get(NameKey(map.MapId))}</b></size>\n{Localization.Get(DescriptionKey(map.MapId))}";

            var button = rowGO.GetComponent<Button>();
            var captured = map;
            button.onClick.AddListener(() => Select(captured));

            _rows.Add((map, image));
        }

        private Sprite PreviewOf(string mapId)
        {
            for (var i = 0; i < previewMapIds.Length && i < previews.Length; i++)
            {
                if (previewMapIds[i] == mapId) return previews[i];
            }
            return null;
        }

        private void Select(MeshMapDefinition map)
        {
            _selected = map;
            foreach (var row in _rows)
            {
                if (_look != null) _look.Style(row.image, row.map == map);
                else row.image.color = row.map == map ? SelectedRowColor : NormalRowColor;
            }
            if (playButton != null) playButton.interactable = map != null;
        }
    }
}
