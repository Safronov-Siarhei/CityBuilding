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
    /// Save browser: a scrollable list of save slots, tap one to select (highlight) it, then a
    /// separate "Load" button actually confirms and loads it — a misclick on a row alone never
    /// starts a scene transition.
    /// </summary>
    public class LoadGamePanelController : MonoBehaviour
    {
        [SerializeField] private string gameplaySceneName = "CityBuilder";
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform listContent;
        [SerializeField] private GameObject emptyLabel;
        [SerializeField] private Button loadSelectedButton;
        [SerializeField] private Text selectedNameLabel;
        [SerializeField] private Sprite rowSprite;
        /// <summary>On in the main menu: rows take the menu's look (MenuRowLook) instead of the tinted rowSprite.</summary>
        [SerializeField] private bool menuLook;

        private MenuRowLook _look;

        private static readonly Color NormalRowColor = UiTheme.RowNormal;
        private static readonly Color SelectedRowColor = UiTheme.RowSelected;

        private readonly List<(string name, Image image)> _rows = new List<(string, Image)>();
        private string _selectedName;

        public void OpenPanel()
        {
            Populate();
            if (panelRoot != null) panelRoot.SetActive(true);
        }

        public void ClosePanel()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        public void ConfirmLoad()
        {
            if (string.IsNullOrEmpty(_selectedName)) return;
            GameSessionIntent.SaveNameToLoad = _selectedName;
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
            _selectedName = null;
            UpdateSelectionUI();

            var saves = SaveSystem.ListSaves();
            if (emptyLabel != null) emptyLabel.SetActive(saves.Count == 0);

            // The autosave heads the list whatever its date: it is the "pick up where I was" row.
            var autosave = saves.FindIndex(s => s.Name == GameSaveController.AutosaveName);
            if (autosave > 0)
            {
                var row = saves[autosave];
                saves.RemoveAt(autosave);
                saves.Insert(0, row);
            }

            foreach (var save in saves)
            {
                CreateRow(save);
            }
        }

        private void CreateRow(SaveSlotInfo info)
        {
            var rowGO = new GameObject($"Save_{info.Name}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            rowGO.transform.SetParent(listContent, false);

            var layoutElement = rowGO.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 96f;
            layoutElement.minHeight = 96f;

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
            textRect.offsetMin = new Vector2(28f, 6f);
            textRect.offsetMax = new Vector2(-28f, -6f);

            var text = textGO.GetComponent<Text>();
            text.font = UiTheme.Regular;
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = _look != null ? _look.Ink : UiTheme.Ink;
            var localTime = info.LastModifiedUtc.ToLocalTime();
            var mapExists = MapStillExists(info.Name);
            text.text = mapExists
                ? $"{DisplayName(info.Name)}\n{localTime:yyyy-MM-dd HH:mm}"
                : $"{DisplayName(info.Name)}\n{localTime:yyyy-MM-dd HH:mm} \u00b7 {Localization.Get("#load_map_removed")}";

            var button = rowGO.GetComponent<Button>();
            if (mapExists)
            {
                var capturedName = info.Name;
                button.onClick.AddListener(() => Select(capturedName));
            }
            else
            {
                // A save on a map that is no longer in the game (Maps 1 and 2 went on 2026-10-20)
                // is shown, so the player knows where it went, but cannot be picked.
                button.interactable = false;
                var ink = _look != null ? _look.Ink : UiTheme.Ink;
                text.color = new Color(ink.r, ink.g, ink.b, 0.45f);
            }

            _rows.Add((info.Name, image));
        }

        /// <summary>Whether the map this save was made on is still in the game. A save that names no map, or one of the old PNG maps, counts as loadable.</summary>
        private static bool MapStillExists(string saveName)
        {
            var mapId = SaveSystem.Load(saveName)?.mapId;
            return string.IsNullOrEmpty(mapId) || MeshMapCatalog.GetById(mapId) != null || MapCatalog.GetById(mapId) != null;
        }

        private static string DisplayName(string saveName) =>
            saveName == GameSaveController.AutosaveName ? Localization.Get("#save_autosave") : saveName;

        private void Select(string name)
        {
            _selectedName = name;
            foreach (var row in _rows)
            {
                if (_look != null) _look.Style(row.image, row.name == name);
                else row.image.color = row.name == name ? SelectedRowColor : NormalRowColor;
            }
            UpdateSelectionUI();
        }

        private void UpdateSelectionUI()
        {
            if (loadSelectedButton != null) loadSelectedButton.interactable = !string.IsNullOrEmpty(_selectedName);
            if (selectedNameLabel != null) selectedNameLabel.text = string.IsNullOrEmpty(_selectedName) ? string.Empty : Localization.Format("#load_selected", DisplayName(_selectedName));
        }
    }
}
