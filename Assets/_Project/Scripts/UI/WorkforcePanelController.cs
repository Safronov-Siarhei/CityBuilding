using System.Collections.Generic;
using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// Every workplace in the settlement in one list, with the +/- controls next to each.
    ///
    /// Reassigning a citizen used to mean finding the building that had them on the map, opening
    /// its card, taking one off, then finding the building that needed them and opening that -- and
    /// knowing, before starting, which of a dozen workshops was the one to rob. This is the same
    /// two buttons, gathered where the comparison can actually be made.
    ///
    /// A row per BUILDING, not per citizen: citizens have no identity in this game (CitizenManager
    /// is a headcount pair and each ProductionBuilding keeps its own count), and the thing the
    /// player is actually doing is moving hands between workplaces, not managing people.
    /// </summary>
    public class WorkforcePanelController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform listContent;
        [SerializeField] private GameObject emptyLabel;
        [SerializeField] private Text summaryLabel;
        [SerializeField] private Sprite rowSprite;

        private static readonly Color RowColor = new Color(0.22f, 0.24f, 0.2f, 0.95f);
        private static readonly Color IdleRowColor = new Color(0.3f, 0.26f, 0.18f, 0.95f);
        private static readonly Color StarvedTextColor = new Color(0.95f, 0.72f, 0.4f);

        /// <summary>One built row, kept so a tap can refresh the numbers instead of rebuilding the whole list.</summary>
        private class Row
        {
            public ProductionBuilding Building;
            public Image Background;
            public Text Detail;
            public Text Count;
            public Button Add;
            public Button Remove;
        }

        private readonly List<Row> _rows = new List<Row>();

        /// <summary>
        /// How many workplaces the list currently holds. Exposed for the PlayMode test, which
        /// cannot count the rows in the hierarchy instead: Destroy is deferred to the end of the
        /// frame, so a freshly repopulated list still has last time's row objects parented to it.
        /// </summary>
        public int RowCount => _rows.Count;

        public void OpenPanel()
        {
            Populate();
            if (panelRoot != null) panelRoot.SetActive(true);
            ModalGate.SetBlocked(true);
        }

        public void ClosePanel()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            ModalGate.SetBlocked(false);
        }

        public void AssignTo(ProductionBuilding building)
        {
            if (building == null) return;
            building.TryAssignWorker();
            Refresh();
        }

        public void RemoveFrom(ProductionBuilding building)
        {
            if (building == null) return;
            building.TryUnassignWorker();
            Refresh();
        }

        private void Populate()
        {
            for (var i = listContent.childCount - 1; i >= 0; i--)
            {
                Destroy(listContent.GetChild(i).gameObject);
            }
            _rows.Clear();

            var workplaces = new List<ProductionBuilding>();
            foreach (var building in FindObjectsByType<ProductionBuilding>(FindObjectsSortMode.None))
            {
                if (building.MaxWorkers > 0) workplaces.Add(building);
            }

            // Alphabetical, so the same workshop is in the same place every time the panel opens --
            // scene order is whatever the engine happens to return and would shuffle between opens.
            workplaces.Sort((a, b) => string.CompareOrdinal(a.DisplayName, b.DisplayName));

            if (emptyLabel != null) emptyLabel.SetActive(workplaces.Count == 0);

            foreach (var workplace in workplaces)
            {
                CreateRow(workplace);
            }

            Refresh();
        }

        /// <summary>
        /// Re-reads the numbers into the existing rows. Rebuilding the list instead would throw
        /// away every row on each tap of +/-, which is both wasteful and visibly janky at a
        /// settlement's worth of buildings (see the mobile-first note in the design backlog).
        /// </summary>
        private void Refresh()
        {
            var citizens = CitizenManager.Instance;
            var idle = citizens != null ? citizens.IdlePopulation : 0;

            if (summaryLabel != null)
            {
                var total = citizens != null ? citizens.TotalPopulation : 0;
                summaryLabel.text = Localization.Format("#workforce_summary", idle, total);
            }

            var lostABuilding = false;
            foreach (var row in _rows)
            {
                // A workshop can decay to destruction while the panel is open -- the game keeps
                // running behind it (ModalGate blocks input, it does not stop time).
                if (row.Building == null)
                {
                    lostABuilding = true;
                    continue;
                }

                var assigned = row.Building.AssignedWorkers;
                var max = row.Building.MaxWorkers;

                row.Count.text = $"{assigned} / {max}";
                row.Background.color = assigned > 0 ? RowColor : IdleRowColor;
                row.Detail.text = DescribeWork(row.Building);
                row.Detail.color = assigned > 0 ? new Color(1f, 1f, 1f, 0.65f) : StarvedTextColor;
                row.Add.interactable = assigned < max && idle > 0;
                row.Remove.interactable = assigned > 0;
            }

            if (lostABuilding) Populate();
        }

        /// <summary>
        /// What a workshop does, in one line: the resource it makes, and the one it eats if it is
        /// a converter. Public and static so the wording is covered by an EditMode test.
        /// </summary>
        public static string DescribeWork(ProductionBuilding building)
        {
            if (building == null) return string.Empty;

            var recipe = building.SelectedRecipe;
            if (recipe == null) return Localization.Get("#workforce_idle_building");

            return Localization.Format("#workforce_gives", recipe.Describe());
        }

        private void CreateRow(ProductionBuilding building)
        {
            var rowGO = new GameObject($"Workplace_{building.DisplayName}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            rowGO.transform.SetParent(listContent, false);

            var layoutElement = rowGO.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 88f;
            layoutElement.minHeight = 88f;

            var background = rowGO.GetComponent<Image>();
            background.sprite = rowSprite;
            background.type = Image.Type.Sliced;
            background.color = RowColor;

            var title = CreateRowText(rowGO.transform, "Title", building.DisplayName, 24, TextAnchor.LowerLeft,
                new Vector2(-140f, 16f), new Vector2(520f, 34f), Color.white);
            var detail = CreateRowText(rowGO.transform, "Detail", string.Empty, 18, TextAnchor.UpperLeft,
                new Vector2(-140f, -18f), new Vector2(520f, 30f), new Color(1f, 1f, 1f, 0.65f));
            var count = CreateRowText(rowGO.transform, "Count", string.Empty, 24, TextAnchor.MiddleCenter,
                new Vector2(190f, 0f), new Vector2(120f, 60f), Color.white);

            var remove = CreateRowButton(rowGO.transform, "Remove", "-", new Vector2(290f, 0f));
            var add = CreateRowButton(rowGO.transform, "Add", "+", new Vector2(370f, 0f));

            var captured = building;
            remove.onClick.AddListener(() => RemoveFrom(captured));
            add.onClick.AddListener(() => AssignTo(captured));

            _rows.Add(new Row
            {
                Building = building,
                Background = background,
                Detail = detail,
                Count = count,
                Add = add,
                Remove = remove,
            });

            // Title never changes after this, so it is not kept on the Row.
            title.text = building.DisplayName;
        }

        private static Text CreateRowText(Transform parent, string name, string content, int fontSize, TextAnchor anchor, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var text = go.GetComponent<Text>();
            text.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.text = content;
            return text;
        }

        private Button CreateRowButton(Transform parent, string name, string label, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(64f, 64f);

            var image = go.GetComponent<Image>();
            image.sprite = rowSprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.3f, 0.34f, 0.28f, 0.95f);

            CreateRowText(go.transform, "Label", label, 30, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(64f, 64f), Color.white);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            return button;
        }
    }
}
