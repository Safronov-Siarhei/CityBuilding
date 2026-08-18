using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Research;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The Laboratory's window. Tapping the Laboratory opens THIS instead of the ordinary building
    /// card, which is why the card's own controls -- scientists, upgrade, repair -- are folded into
    /// its header: they would otherwise be unreachable.
    ///
    /// Two tabs, buildings and soldiers, and a deliberately COMPACT list: what can be researched
    /// right now, plus what already has been, greyed out with a tick. Everything waiting on a bigger
    /// Laboratory or on the level below it stays hidden -- listing all of it would be a hundred rows
    /// of things the player cannot act on.
    /// </summary>
    public class ResearchPanelController : MonoBehaviour
    {
        /// <summary>How often the open window re-reads the countdown and the affordability of each row. Every frame would allocate a fistful of strings per row for no visible gain.</summary>
        private const float RefreshIntervalSeconds = 0.25f;

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text conditionLabel;
        [SerializeField] private Text scientistsLabel;
        [SerializeField] private Button addScientistButton;
        [SerializeField] private Button removeScientistButton;
        [SerializeField] private GameObject upgradeControls;
        [SerializeField] private Transform upgradeCostRow;
        [SerializeField] private GameObject repairControls;
        [SerializeField] private Transform repairCostRow;
        [SerializeField] private Text progressLabel;
        [SerializeField] private GameObject cancelControls;
        [SerializeField] private Text cancelLabel;
        [SerializeField] private Image buildingsTabBackground;
        [SerializeField] private Image unitsTabBackground;
        [SerializeField] private RectTransform listContent;
        [SerializeField] private GameObject emptyLabel;
        [SerializeField] private Sprite rowSprite;
        [SerializeField] private Sprite completedIcon;
        [SerializeField] private ResourceIconLibrary iconLibrary;

        private static readonly Color RowColor = new Color(0.22f, 0.24f, 0.2f, 0.95f);
        private static readonly Color CompletedRowColor = new Color(0.17f, 0.18f, 0.16f, 0.75f);
        private static readonly Color HeaderColor = new Color(0.12f, 0.13f, 0.11f, 0.9f);
        private static readonly Color SelectedTabColor = new Color(0.36f, 0.5f, 0.3f, 0.95f);
        private static readonly Color UnselectedTabColor = new Color(0.26f, 0.29f, 0.24f, 0.95f);
        private static readonly Color DetailColor = new Color(1f, 1f, 1f, 0.65f);
        private static readonly Color BlockedColor = new Color(0.95f, 0.72f, 0.4f);

        /// <summary>One built row, kept so a tick of the refresh timer can update its numbers instead of rebuilding the list under the player's finger.</summary>
        private class Row
        {
            public ResearchTopic Topic;
            public Image Background;
            public Text Detail;
            public Button Action;

            /// <summary>Built once, at row creation: the price and duration never change while the window is open, and formatting it four times a second for every row would be a steady drip of garbage on a phone.</summary>
            public string CostLine;
        }

        private readonly List<Row> _rows = new List<Row>();

        private BuildingInstance _lab;
        private ProductionBuilding _labWorkplace;
        private bool _unitsTab;
        private float _refreshTimer;

        /// <summary>How many rows the list holds, for the PlayMode test -- Destroy is deferred to end of frame, so counting the hierarchy's children counts last time's rows too (see WorkforcePanelController).</summary>
        public int RowCount => _rows.Count;

        /// <summary>Which tab is showing, for the test that switches them.</summary>
        public bool IsUnitsTab => _unitsTab;

        /// <summary>Opens the window on a particular Laboratory (the one that was tapped).</summary>
        public void Show(BuildingInstance laboratory)
        {
            _lab = laboratory;
            _labWorkplace = laboratory != null ? laboratory.GetComponent<ProductionBuilding>() : null;

            if (panelRoot != null) panelRoot.SetActive(true);
            ModalGate.SetBlocked(true);

            var research = ResearchManager.Instance;
            if (research != null)
            {
                // Removed first: tapping a second Laboratory without closing the window would
                // otherwise subscribe twice and rebuild the list twice per event.
                research.OnResearchChanged -= Populate;
                research.OnResearchChanged += Populate;
            }

            Populate();
        }

        public void Close()
        {
            var research = ResearchManager.Instance;
            if (research != null) research.OnResearchChanged -= Populate;

            _lab = null;
            _labWorkplace = null;
            if (panelRoot != null) panelRoot.SetActive(false);
            ModalGate.SetBlocked(false);
        }

        public void SelectBuildingsTab()
        {
            if (!_unitsTab) return;
            _unitsTab = false;
            Populate();
        }

        public void SelectUnitsTab()
        {
            if (_unitsTab) return;
            _unitsTab = true;
            Populate();
        }

        public void AddScientist()
        {
            _labWorkplace?.TryAssignWorker();
            Refresh();
        }

        public void RemoveScientist()
        {
            _labWorkplace?.TryUnassignWorker();
            Refresh();
        }

        /// <summary>The Laboratory's own upgrade, paid in resources like any other building's -- its levels are deliberately not researched (see ResearchManager).</summary>
        public void UpgradeLab()
        {
            _lab?.TryUpgrade();
            // A bigger Laboratory permits more, so the list itself changes, not just the numbers.
            Populate();
        }

        public void RepairLab()
        {
            _lab?.TryRepair();
            Refresh();
        }

        public void CancelResearch()
        {
            ResearchManager.Instance?.CancelCurrent();
            Populate();
        }

        public void StartResearch(ResearchTopic topic)
        {
            var research = ResearchManager.Instance;
            if (research == null || !research.TryStart(topic)) { Refresh(); return; }

            Populate();
        }

        private void Update()
        {
            if (panelRoot == null || !panelRoot.activeSelf) return;

            // The Laboratory can be destroyed while its own window is open: ModalGate blocks input,
            // it does not stop time. Closing is the honest answer -- there is nothing left to show.
            if (_lab == null)
            {
                Close();
                return;
            }

            _refreshTimer += Time.unscaledDeltaTime;
            if (_refreshTimer < RefreshIntervalSeconds) return;
            _refreshTimer = 0f;
            Refresh();
        }

        /// <summary>Rebuilds the rows: on open, on a tab switch, and whenever a research starts or finishes and changes which rows belong in the list.</summary>
        private void Populate()
        {
            if (listContent == null) return;

            for (var i = listContent.childCount - 1; i >= 0; i--)
            {
                Destroy(listContent.GetChild(i).gameObject);
            }
            _rows.Clear();

            var research = ResearchManager.Instance;
            var topics = _unitsTab ? ResearchCatalog.UnitTopics : ResearchCatalog.BuildingTopics;

            var shownCategory = (BuildingCategory)(-1);
            var shown = 0;
            foreach (var topic in topics)
            {
                if (research != null && !research.IsAvailable(topic) && !research.IsCompleted(topic.Id)) continue;

                // Category headings only in the buildings tab, and only for categories that actually
                // have a row under them.
                if (!_unitsTab && topic.Category != shownCategory)
                {
                    shownCategory = topic.Category;
                    CreateHeader(CategoryName(shownCategory));
                }

                CreateRow(topic);
                shown++;
            }

            if (emptyLabel != null) emptyLabel.SetActive(shown == 0);

            Refresh();
        }

        /// <summary>Re-reads every number the window shows, without touching the rows themselves.</summary>
        private void Refresh()
        {
            var research = ResearchManager.Instance;

            RefreshHeader();
            RefreshProgress(research);

            if (buildingsTabBackground != null) buildingsTabBackground.color = _unitsTab ? UnselectedTabColor : SelectedTabColor;
            if (unitsTabBackground != null) unitsTabBackground.color = _unitsTab ? SelectedTabColor : UnselectedTabColor;

            foreach (var row in _rows)
            {
                var completed = research != null && research.IsCompleted(row.Topic.Id);
                row.Background.color = completed ? CompletedRowColor : RowColor;

                if (completed)
                {
                    row.Detail.text = Localization.Get("#research_done");
                    row.Detail.color = DetailColor;
                    continue;
                }

                var blocker = research != null ? research.DescribeBlocker(row.Topic) : null;
                row.Detail.text = blocker ?? row.CostLine;
                row.Detail.color = blocker != null ? BlockedColor : DetailColor;
                if (row.Action != null) row.Action.interactable = blocker == null;
            }
        }

        private void RefreshHeader()
        {
            if (_lab == null || _lab.Data == null) return;

            if (titleLabel != null) titleLabel.text = _lab.Data.LocalizedName;
            if (conditionLabel != null)
            {
                var decay = $"{Mathf.RoundToInt(_lab.Decay * 100f)}%";
                conditionLabel.text = Localization.Format("#research_lab_state", _lab.Level, BuildingInstance.MaxLevel,
                    _lab.CurrentHealth, _lab.MaxHealth, decay);
            }

            var assigned = _labWorkplace != null ? _labWorkplace.AssignedWorkers : 0;
            var max = _labWorkplace != null ? _labWorkplace.MaxWorkers : 0;
            var idle = CitizenManager.Instance != null ? CitizenManager.Instance.IdlePopulation : 0;
            if (scientistsLabel != null) scientistsLabel.text = Localization.Format("#research_scientists", assigned, max, idle);
            if (addScientistButton != null) addScientistButton.interactable = assigned < max && idle > 0;
            if (removeScientistButton != null) removeScientistButton.interactable = assigned > 0;

            var upgradeCost = _lab.GetUpgradeCost();
            if (upgradeControls != null) upgradeControls.SetActive(upgradeCost != null);
            CostRowBuilder.Build(upgradeCostRow, upgradeCost, iconLibrary);

            var repairCost = _lab.GetRepairCost();
            if (repairControls != null) repairControls.SetActive(repairCost != null);
            CostRowBuilder.Build(repairCostRow, repairCost, iconLibrary);
        }

        private void RefreshProgress(ResearchManager research)
        {
            var current = research != null ? research.Current : null;

            if (cancelControls != null) cancelControls.SetActive(current != null);
            if (progressLabel == null) return;

            if (current == null)
            {
                progressLabel.text = Localization.Get("#research_idle");
                return;
            }

            progressLabel.text = research.LabWorkers > 0
                ? Localization.Format("#research_running", current.Title, Mathf.CeilToInt(research.RemainingSeconds))
                : Localization.Format("#research_paused", current.Title);

            if (cancelLabel != null) cancelLabel.text = Localization.Format("#research_cancel", research.CurrentCancelRefund);
        }

        /// <summary>
        /// What a row costs, in one line: coins and seconds, plus -- for a soldier level -- what the
        /// level is actually worth, since "уровень 2" on its own says nothing.
        ///
        /// Public and static so the wording is covered by an EditMode test without a canvas.
        /// </summary>
        public static string DescribeCost(ResearchTopic topic)
        {
            if (topic == null) return string.Empty;

            var line = Localization.Format("#research_cost", topic.Coins, Mathf.RoundToInt(topic.BaseSeconds));
            if (topic.Kind != ResearchKind.UnitLevel) return line;

            var gain = DescribeUnitGain(topic);
            return string.IsNullOrEmpty(gain) ? line : line + " · " + gain;
        }

        /// <summary>What a soldier level adds, as differences: the player is choosing whether the coins are worth it, not reading a stat sheet.</summary>
        private static string DescribeUnitGain(ResearchTopic topic)
        {
            if (!SoldierStats.TryTypeFromSheetId(topic.TargetId, out var type)) return string.Empty;

            var before = SoldierStats.StatsAt(type, topic.Level - 1);
            var after = SoldierStats.StatsAt(type, topic.Level);
            return Localization.Format("#research_unit_gain",
                after.maxHealth - before.maxHealth, after.attackDamage - before.attackDamage);
        }

        private static string CategoryName(BuildingCategory category)
        {
            return Localization.Get("#category_" + category.ToString().ToLowerInvariant());
        }

        private void CreateHeader(string caption)
        {
            var go = new GameObject("Header", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(listContent, false);

            var layoutElement = go.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 44f;
            layoutElement.minHeight = 44f;

            var background = go.GetComponent<Image>();
            background.sprite = rowSprite;
            background.type = Image.Type.Sliced;
            background.color = HeaderColor;

            CreateRowText(go.transform, "Caption", caption, 22, TextAnchor.MiddleLeft,
                new Vector2(-230f, 0f), new Vector2(620f, 40f), new Color(0.82f, 0.88f, 0.7f));
        }

        private void CreateRow(ResearchTopic topic)
        {
            var rowGO = new GameObject($"Topic_{topic.Id}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            rowGO.transform.SetParent(listContent, false);

            var layoutElement = rowGO.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 88f;
            layoutElement.minHeight = 88f;

            var background = rowGO.GetComponent<Image>();
            background.sprite = rowSprite;
            background.type = Image.Type.Sliced;
            background.color = RowColor;

            CreateRowText(rowGO.transform, "Title", topic.Title, 24, TextAnchor.LowerLeft,
                new Vector2(-230f, 16f), new Vector2(620f, 34f), Color.white);
            var detail = CreateRowText(rowGO.transform, "Detail", string.Empty, 18, TextAnchor.UpperLeft,
                new Vector2(-230f, -18f), new Vector2(620f, 30f), DetailColor);

            var research = ResearchManager.Instance;
            var completed = research != null && research.IsCompleted(topic.Id);

            Button action = null;
            if (completed)
            {
                // A tick rather than a glyph in the label: the built-in font has no check mark, and a
                // missing glyph draws as an empty box.
                var tick = new GameObject("Completed", typeof(RectTransform), typeof(Image));
                tick.transform.SetParent(rowGO.transform, false);
                var tickRect = tick.GetComponent<RectTransform>();
                tickRect.anchorMin = tickRect.anchorMax = new Vector2(0.5f, 0.5f);
                tickRect.anchoredPosition = new Vector2(470f, 0f);
                tickRect.sizeDelta = new Vector2(48f, 48f);
                var tickImage = tick.GetComponent<Image>();
                tickImage.sprite = completedIcon;
                tickImage.preserveAspect = true;
                tickImage.color = new Color(0.6f, 0.85f, 0.5f);
            }
            else
            {
                action = CreateRowButton(rowGO.transform, "Start", Localization.Get("#research_start"), new Vector2(470f, 0f));
                var captured = topic;
                action.onClick.AddListener(() => StartResearch(captured));
            }

            _rows.Add(new Row
            {
                Topic = topic,
                Background = background,
                Detail = detail,
                Action = action,
                CostLine = DescribeCost(topic),
            });
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
            rect.sizeDelta = new Vector2(200f, 64f);

            var image = go.GetComponent<Image>();
            image.sprite = rowSprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.3f, 0.34f, 0.28f, 0.95f);

            CreateRowText(go.transform, "Label", label, 22, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(200f, 64f), Color.white);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            return button;
        }
    }
}
