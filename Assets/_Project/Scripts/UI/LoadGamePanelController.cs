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

        [Header("The row's details (the user, 2026-11-10: theme badge, map code, people, buildings, day)")]
        /// <summary>A round with an Icon child, copied to the row's left for the save's theme; none = no badge.</summary>
        [SerializeField] private GameObject themeBadgeTemplate;
        /// <summary>The theme glyphs in MapThemes.All order.</summary>
        [SerializeField] private Sprite[] themeGlyphs = new Sprite[0];
        /// <summary>Building ids not counted as the player's buildings (roads, fences, moats, bridges, decor pieces).</summary>
        [SerializeField] private string[] uncountedBuildings = new string[0];

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
            var data = SaveSystem.Load(info.Name);
            var badge = themeBadgeTemplate != null && data != null;
            var rowGO = new GameObject($"Save_{info.Name}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            rowGO.transform.SetParent(listContent, false);

            var layoutElement = rowGO.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 120f;
            layoutElement.minHeight = 120f;

            var image = rowGO.GetComponent<Image>();
            image.sprite = rowSprite;
            image.type = Image.Type.Sliced;
            image.color = NormalRowColor;
            if (_look != null) _look.Style(image, false);

            var left = 28f;
            if (badge)
            {
                var round = Instantiate(themeBadgeTemplate, rowGO.transform, false);
                round.name = "Theme";
                round.SetActive(true);
                var rect = (RectTransform)round.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(70f, 0f);
                var icon = round.transform.Find("Icon");
                var glyph = ThemeGlyph(data.season);
                if (icon != null && glyph != null) icon.GetComponent<Image>().sprite = glyph;
                left = 130f;
            }

            var ink = _look != null ? _look.Ink : UiTheme.Ink;
            var localTime = info.LastModifiedUtc.ToLocalTime();
            var mapExists = MapStillExists(data);
            var title = Line(rowGO.transform, "Name", left, 0.5f, 1f, UiTheme.Bold, 28, ink);
            title.text = $"{DisplayName(info.Name)}   <size=22>{localTime:yyyy-MM-dd HH:mm}</size>";
            var details = Line(rowGO.transform, "Details", left, 0f, 0.5f, UiTheme.Regular, 24, ink);
            details.text = data == null ? string.Empty
                : mapExists ? Details(data)
                : Localization.Get("#load_map_removed");

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
                var faded = new Color(ink.r, ink.g, ink.b, 0.45f);
                title.color = faded;
                details.color = faded;
            }

            _rows.Add((info.Name, image));
        }

        private static Text Line(Transform row, string name, float left, float bottom, float top, Font font, int size, Color ink)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(row, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, bottom);
            rect.anchorMax = new Vector2(1f, top);
            rect.offsetMin = new Vector2(left, top > 0.9f ? 0f : 8f);
            rect.offsetMax = new Vector2(-28f, top > 0.9f ? -8f : 0f);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.supportRichText = true;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.color = ink;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>«День 42 · Жителей 86 · Зданий 31 · Код карты 3-83115» (no code on a hand-made map).</summary>
        private string Details(GameSaveData data)
        {
            var line = Localization.Format("#load_row_stats", data.currentDay, data.population, CountBuildings(data));
            var code = Maps.Archipelago.ArchipelagoMapId.Code(data.mapId);
            return string.IsNullOrEmpty(code) ? line : line + "  ·  " + Localization.Format("#load_row_code", code);
        }

        private int CountBuildings(GameSaveData data)
        {
            var count = 0;
            foreach (var entry in data.buildings)
            {
                if (entry != null && System.Array.IndexOf(uncountedBuildings, entry.buildingName) < 0) count++;
            }
            return count;
        }

        /// <summary>The save's season as a theme glyph: the first theme dressed in it (winter, not the New Year); -1 = an old save = summer.</summary>
        private Sprite ThemeGlyph(int season)
        {
            var wanted = season < 0 ? MapSeason.Summer : (MapSeason)season;
            for (var i = 0; i < MapThemes.All.Length && i < themeGlyphs.Length; i++)
            {
                if (MapThemes.SeasonOf(MapThemes.All[i]) == wanted) return themeGlyphs[i];
            }
            return null;
        }

        /// <summary>Whether the map this save was made on is still in the game. A save that names no map, or one of the old PNG maps, counts as loadable.</summary>
        private static bool MapStillExists(GameSaveData data)
        {
            var mapId = data?.mapId;
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
