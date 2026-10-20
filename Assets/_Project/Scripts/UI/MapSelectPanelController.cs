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
            SceneManager.LoadScene(gameplaySceneName);
        }

        private void Populate()
        {
            for (var i = listContent.childCount - 1; i >= 0; i--)
            {
                Destroy(listContent.GetChild(i).gameObject);
            }
            _rows.Clear();

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

            var layoutElement = rowGO.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 120f;
            layoutElement.minHeight = 120f;

            var image = rowGO.GetComponent<Image>();
            image.sprite = rowSprite;
            image.type = Image.Type.Sliced;
            image.color = NormalRowColor;

            var textGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(rowGO.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(28f, 6f);
            textRect.offsetMax = new Vector2(-28f, -6f);

            var text = textGO.GetComponent<Text>();
            text.font = UiTheme.Regular;
            text.fontSize = 24;
            text.supportRichText = true;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = UiTheme.Ink;
            text.text = $"<size=30><b>{Localization.Get(NameKey(map.MapId))}</b></size>\n{Localization.Get(DescriptionKey(map.MapId))}";

            var button = rowGO.GetComponent<Button>();
            var captured = map;
            button.onClick.AddListener(() => Select(captured));

            _rows.Add((map, image));
        }

        private void Select(MeshMapDefinition map)
        {
            _selected = map;
            foreach (var row in _rows)
            {
                row.image.color = row.map == map ? SelectedRowColor : NormalRowColor;
            }
            if (playButton != null) playButton.interactable = map != null;
        }
    }
}
