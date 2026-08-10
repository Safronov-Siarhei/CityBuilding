using System.Collections.Generic;
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

        private static readonly Color NormalRowColor = new Color(0.22f, 0.24f, 0.2f, 0.95f);
        private static readonly Color SelectedRowColor = new Color(0.32f, 0.5f, 0.28f, 0.95f);

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
            SceneManager.LoadScene(gameplaySceneName);
        }

        private void Populate()
        {
            for (var i = listContent.childCount - 1; i >= 0; i--)
            {
                Destroy(listContent.GetChild(i).gameObject);
            }
            _rows.Clear();
            _selectedName = null;
            UpdateSelectionUI();

            var saves = SaveSystem.ListSaves();
            if (emptyLabel != null) emptyLabel.SetActive(saves.Count == 0);

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

            var textGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(rowGO.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(28f, 6f);
            textRect.offsetMax = new Vector2(-28f, -6f);

            var text = textGO.GetComponent<Text>();
            text.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            var localTime = info.LastModifiedUtc.ToLocalTime();
            text.text = $"{info.Name}\n{localTime:yyyy-MM-dd HH:mm}";

            var button = rowGO.GetComponent<Button>();
            var capturedName = info.Name;
            button.onClick.AddListener(() => Select(capturedName));

            _rows.Add((info.Name, image));
        }

        private void Select(string name)
        {
            _selectedName = name;
            foreach (var row in _rows)
            {
                row.image.color = row.name == name ? SelectedRowColor : NormalRowColor;
            }
            UpdateSelectionUI();
        }

        private void UpdateSelectionUI()
        {
            if (loadSelectedButton != null) loadSelectedButton.interactable = !string.IsNullOrEmpty(_selectedName);
            if (selectedNameLabel != null) selectedNameLabel.text = string.IsNullOrEmpty(_selectedName) ? string.Empty : $"Выбрано: {_selectedName}";
        }
    }
}
