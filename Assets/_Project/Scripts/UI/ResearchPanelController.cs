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
    /// The Laboratory's window, after the user's sketch (2026-10-28). Tapping the Laboratory opens
    /// THIS instead of the ordinary building card, which is why the card's own controls --
    /// scientists, upgrade, repair -- live along its foot.
    ///
    /// Two tabs, «Здания» and «Армия»; the chosen one stands larger and lit. Under them one ROW per
    /// building (in the build menu's order) or per soldier: its name, then its levels left to right,
    /// a picture each joined by a line, the price and a «Прокачать» button under each. A level
    /// already had shows a tick; the one being researched a bar with the time and «Отмена» on its
    /// button; one waiting on the level before it a grey button; one the Laboratory is too small
    /// for a grey «Лаб. ур. N». Tapping a picture says what that level gives. A building with
    /// nothing to research (the flag, the decor, the Laboratory itself) has no row.
    /// </summary>
    public class ResearchPanelController : MonoBehaviour
    {
        /// <summary>How often the open window re-reads the countdown and each cell's state. Every frame would allocate a fistful of strings per cell for no visible gain.</summary>
        private const float RefreshIntervalSeconds = 0.25f;

        /// <summary>How long a message (why a level cannot start, what a level gives) stays on the status line.</summary>
        private const float MessageSeconds = 4f;

        private const float CellStep = 250f;
        private const float RowHeight = 290f;
        private const float IconSize = 112f;
        private const float SelectedTabScale = 1.12f;

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text conditionLabel;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Text crystalsLabel;
        [SerializeField] private Text scientistsLabel;
        [SerializeField] private Button addScientistButton;
        [SerializeField] private Button removeScientistButton;
        [SerializeField] private GameObject upgradeControls;
        [SerializeField] private Transform upgradeCostRow;
        [SerializeField] private GameObject repairControls;
        [SerializeField] private Transform repairCostRow;
        [SerializeField] private RectTransform buildingsTab;
        [SerializeField] private RectTransform unitsTab;
        [SerializeField] private RectTransform listContent;
        [SerializeField] private GameObject emptyLabel;
        [SerializeField] private GameObject hintPanel;
        [SerializeField] private Text hintText;
        [SerializeField] private Sprite rowSprite;
        [SerializeField] private Sprite buttonSprite;
        [SerializeField] private Sprite plaqueSprite;
        [SerializeField] private Sprite completedIcon;
        [SerializeField] private Sprite crystalIcon;
        [SerializeField] private ResourceIconLibrary iconLibrary;

        // Every picture the rows may need, by file name: a building's level 1 as `<id>`, its higher
        // levels as `<id>_L<n>`, a soldier as `Portrait<type>` (SetupProject fills both arrays).
        [SerializeField] private string[] iconKeys = new string[0];
        [SerializeField] private Sprite[] icons = new Sprite[0];

        private static readonly Color AvailableColor = UiTheme.Rgb(0x8ee06a);
        private static readonly Color LockedColor = UiTheme.RowLocked;
        private static readonly Color CancelColor = UiTheme.Rgb(0xf0b8a8);
        private static readonly Color DimmedIcon = new Color(1f, 1f, 1f, 0.45f);
        private static readonly Color SelectedTabColor = UiTheme.TabSelected;
        private static readonly Color UnselectedTabColor = UiTheme.TabIdle;

        /// <summary>One level of one row: its picture, its price, its button, and the topic behind it (null for a level nothing gates -- it is simply had).</summary>
        private class Cell
        {
            public ResearchTopic Topic;
            public string TargetId;
            public int Level;
            public bool IsUnit;
            public Image Icon;
            public GameObject Tick;
            public GameObject Price;
            public GameObject Bar;
            public Image BarFill;
            public Text BarText;
            public Button Action;
            public Image ActionImage;
            public Text ActionLabel;
        }

        private readonly List<Cell> _cells = new List<Cell>();
        private int _rowCount;
        private Dictionary<string, Sprite> _iconsByKey;
        private Dictionary<string, BuildingData> _dataById;

        private BuildingInstance _lab;
        private ProductionBuilding _labWorkplace;
        private bool _unitsTab;
        private float _refreshTimer;
        private string _message;
        private Color _messageColor;
        private float _messageUntil;

        /// <summary>How many rows (buildings or soldiers) the list holds, for the PlayMode tests.</summary>
        public int RowCount => _rowCount;

        /// <summary>Which tab is showing, for the test that switches them.</summary>
        public bool IsUnitsTab => _unitsTab;

        /// <summary>What the window says when it has no rows. Empty while the list has anything in it.</summary>
        public string EmptyMessage
        {
            get
            {
                if (emptyLabel == null || !emptyLabel.activeSelf) return string.Empty;
                var text = emptyLabel.GetComponent<Text>();
                return text != null ? text.text : string.Empty;
            }
        }

        /// <summary>The card is up.</summary>
        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        /// <summary>What the status line says now, for the tests.</summary>
        public string StatusText => statusLabel != null ? statusLabel.text : string.Empty;

        /// <summary>Opens the window on a particular Laboratory (the one that was tapped).</summary>
        public void Show(BuildingInstance laboratory)
        {
            _lab = laboratory;
            _labWorkplace = laboratory != null ? laboratory.GetComponent<ProductionBuilding>() : null;

            // Always opens on the buildings tab: that is where almost all of the list lives.
            _unitsTab = false;
            _message = null;

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                // over the HUD's plaques (the day, the tax), which are built after it on the same canvas
                panelRoot.transform.SetAsLastSibling();
            }
            if (hintPanel != null) hintPanel.SetActive(false);
            ModalGate.SetBlocked(true);

            var research = ResearchManager.Instance;
            if (research != null)
            {
                // Removed first: tapping a second Laboratory without closing the window would
                // otherwise subscribe twice and rebuild the list twice per event.
                research.OnResearchChanged -= Refresh;
                research.OnResearchChanged += Refresh;
            }
            Wallet.UpgradeCrystalsChanged -= Refresh;
            Wallet.UpgradeCrystalsChanged += Refresh;

            Populate();
        }

        public void Close()
        {
            var research = ResearchManager.Instance;
            if (research != null) research.OnResearchChanged -= Refresh;
            Wallet.UpgradeCrystalsChanged -= Refresh;

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
            Refresh();
        }

        public void RepairLab()
        {
            _lab?.TryRepair();
            Refresh();
        }

        public void CancelResearch()
        {
            ResearchManager.Instance?.CancelCurrent();
            Refresh();
        }

        /// <summary>The «!»: why the Laboratory is worth upgrading and what the scientists do.</summary>
        public void ToggleHint()
        {
            if (hintPanel == null) return;
            var show = !hintPanel.activeSelf;
            hintPanel.SetActive(show);
            if (show && hintText != null)
            {
                hintText.text = Localization.Format("#research_hint",
                    Mathf.RoundToInt(BalanceConfig.Instance != null ? BalanceConfig.Instance.ResearchSecondsPerWorker : 5f));
            }
        }

        /// <summary>Starts a research; when it cannot, the status line says why.</summary>
        public void StartResearch(ResearchTopic topic)
        {
            var research = ResearchManager.Instance;
            if (research == null) return;
            var blocker = research.DescribeBlocker(topic);
            if (blocker != null || !research.TryStart(topic))
            {
                Say(blocker ?? Localization.Get("#research_blocked_unknown"), UiTheme.Warning);
                Refresh();
                return;
            }
            _message = null;
            Refresh();
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

        // -- the list -------------------------------------------------------------------------------

        /// <summary>Rebuilds the rows: on open and on a tab switch. Everything else only re-reads the cells (Refresh).</summary>
        private void Populate()
        {
            if (listContent == null) return;

            for (var i = listContent.childCount - 1; i >= 0; i--)
            {
                Destroy(listContent.GetChild(i).gameObject);
            }
            _cells.Clear();
            _rowCount = 0;

            if (_unitsTab) PopulateUnits();
            else PopulateBuildings();

            if (emptyLabel != null) emptyLabel.SetActive(_rowCount == 0);
            Refresh();
        }

        private void PopulateBuildings()
        {
            var topicsByBuilding = new Dictionary<string, List<ResearchTopic>>();
            foreach (var topic in ResearchCatalog.BuildingTopics)
            {
                if (!topicsByBuilding.TryGetValue(topic.TargetId, out var list)) topicsByBuilding[topic.TargetId] = list = new List<ResearchTopic>();
                list.Add(topic);
            }

            foreach (var data in BuildMenuOrder())
            {
                if (!topicsByBuilding.TryGetValue(data.buildingName, out var topics)) continue;
                var levels = new ResearchTopic[BuildingInstance.MaxLevel + 1];
                foreach (var topic in topics) levels[topic.Kind == ResearchKind.UnlockBuilding ? 1 : topic.Level] = topic;
                CreateRow(data.LocalizedName, data.buildingName, levels, BuildingInstance.MaxLevel, isUnit: false);
            }
        }

        private void PopulateUnits()
        {
            var topicsByUnit = new Dictionary<string, ResearchTopic[]>();
            foreach (var topic in ResearchCatalog.UnitTopics)
            {
                if (!topicsByUnit.TryGetValue(topic.TargetId, out var levels)) topicsByUnit[topic.TargetId] = levels = new ResearchTopic[UnitBalance.MaxLevel + 1];
                levels[topic.Kind == ResearchKind.UnlockUnit ? 1 : topic.Level] = topic;
            }

            // in the tiers' own order, militia first
            var units = new List<(SoldierType type, string id)>();
            foreach (var unit in BalanceConfig.Instance.Units)
            {
                if (string.IsNullOrEmpty(unit.id) || !SoldierStats.TryTypeFromSheetId(unit.id, out var type) || SoldierStats.IsHero(type)) continue;
                units.Add((type, unit.id));
            }
            units.Sort((a, b) => ((int)a.type).CompareTo((int)b.type));

            foreach (var (type, id) in units)
            {
                topicsByUnit.TryGetValue(id, out var levels);
                CreateRow(SoldierStats.DisplayName(type), id, levels ?? new ResearchTopic[UnitBalance.MaxLevel + 1], UnitBalance.MaxLevel, isUnit: true);
            }
        }

        /// <summary>The buildings as the build menu lists them: by category, then in the menu's own order.</summary>
        private IEnumerable<BuildingData> BuildMenuOrder()
        {
            var placer = FindAnyObjectByType<BuildingPlacer>();
            var all = new List<BuildingData>();
            if (placer != null)
            {
                foreach (var data in placer.AvailableBuildings)
                {
                    if (data != null) all.Add(data);
                }
            }
            _dataById = new Dictionary<string, BuildingData>();
            foreach (var data in all) _dataById[data.buildingName] = data;

            var ordered = new List<BuildingData>(all);
            // a stable sort by category: List.Sort is not stable, so the menu index breaks the tie
            ordered.Sort((a, b) =>
            {
                var byCategory = ((int)a.category).CompareTo((int)b.category);
                return byCategory != 0 ? byCategory : all.IndexOf(a).CompareTo(all.IndexOf(b));
            });
            return ordered;
        }

        private void CreateRow(string caption, string targetId, ResearchTopic[] levels, int maxLevel, bool isUnit)
        {
            var row = new GameObject($"Row_{targetId}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            row.transform.SetParent(listContent, false);
            var layout = row.GetComponent<LayoutElement>();
            layout.preferredHeight = layout.minHeight = RowHeight;
            var background = row.GetComponent<Image>();
            background.sprite = rowSprite;
            background.type = Image.Type.Sliced;
            background.color = UiTheme.RowNormal;

            var first = -(maxLevel - 1) * 0.5f * CellStep;
            const float iconY = 18f;

            // the line through the pictures, behind them
            var line = NewImage(row.transform, "Line", null, UiTheme.InkSoft, new Vector2(0f, iconY), new Vector2((maxLevel - 1) * CellStep, 6f));
            line.raycastTarget = false;

            var plaque = NewImage(row.transform, "Name", plaqueSprite, Color.white, new Vector2(0f, 122f), new Vector2(300f, 40f));
            plaque.type = Image.Type.Sliced;
            plaque.raycastTarget = false;
            NewText(plaque.transform, "Label", caption, 22, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(290f, 38f), UiTheme.OnWood, UiTheme.Bold);

            for (var level = 1; level <= maxLevel; level++)
            {
                var topic = levels != null && level < levels.Length ? levels[level] : null;
                _cells.Add(CreateCell(row.transform, new Vector2(first + (level - 1) * CellStep, iconY), targetId, level, topic, isUnit));
            }
            _rowCount++;
        }

        private Cell CreateCell(Transform row, Vector2 at, string targetId, int level, ResearchTopic topic, bool isUnit)
        {
            var cell = new Cell { Topic = topic, TargetId = targetId, Level = level, IsUnit = isUnit };

            var frame = NewImage(row, $"Level{level}", rowSprite, UiTheme.RowHeader, at, new Vector2(IconSize + 16f, IconSize + 16f));
            frame.type = Image.Type.Sliced;
            var tap = frame.gameObject.AddComponent<Button>();
            tap.targetGraphic = frame;
            var captured = cell;
            tap.onClick.AddListener(() => ShowLevelInfo(captured));

            cell.Icon = NewImage(frame.transform, "Icon", IconFor(targetId, level, isUnit), Color.white, Vector2.zero, new Vector2(IconSize, IconSize));
            cell.Icon.preserveAspect = true;
            cell.Icon.raycastTarget = false;

            var badge = NewImage(frame.transform, "LevelBadge", plaqueSprite, Color.white, new Vector2(-(IconSize * 0.5f) + 6f, IconSize * 0.5f - 6f), new Vector2(40f, 32f));
            badge.type = Image.Type.Sliced;
            badge.raycastTarget = false;
            NewText(badge.transform, "Label", level.ToString(), 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(40f, 32f), UiTheme.OnWood, UiTheme.Bold);

            var tick = NewImage(frame.transform, "Done", completedIcon, UiTheme.Good, new Vector2(IconSize * 0.5f - 8f, -(IconSize * 0.5f) + 8f), new Vector2(40f, 40f));
            tick.preserveAspect = true;
            tick.raycastTarget = false;
            cell.Tick = tick.gameObject;

            // the bar of the research under way, across the picture's foot
            var bar = NewImage(frame.transform, "Progress", null, new Color(0f, 0f, 0f, 0.6f), new Vector2(0f, -(IconSize * 0.5f) + 14f), new Vector2(IconSize, 28f));
            bar.raycastTarget = false;
            var fill = NewImage(bar.transform, "Fill", null, UiTheme.Good, Vector2.zero, new Vector2(IconSize, 28f));
            fill.raycastTarget = false;
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.sprite = completedIcon;   // a Filled image needs a sprite; its look is the colour
            cell.BarFill = fill;
            cell.BarText = NewText(bar.transform, "Time", string.Empty, 18, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(IconSize, 28f), Color.white, UiTheme.Bold);
            cell.Bar = bar.gameObject;

            cell.Price = CreatePrice(row, new Vector2(at.x, at.y - 88f), topic);

            var action = NewImage(row, $"Action{level}", buttonSprite, AvailableColor, new Vector2(at.x, at.y - 130f), new Vector2(190f, 50f));
            action.type = Image.Type.Sliced;
            cell.ActionImage = action;
            cell.Action = action.gameObject.AddComponent<Button>();
            cell.Action.targetGraphic = action;
            cell.Action.onClick.AddListener(() => Act(captured));
            cell.ActionLabel = NewText(action.transform, "Label", Localization.Get("#research_upgrade"), 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(186f, 48f), UiTheme.Ink, UiTheme.Bold);
            cell.ActionLabel.verticalOverflow = VerticalWrapMode.Overflow;
            return cell;
        }

        /// <summary>The coins and, from level 3, the upgrade crystal -- as the building card shows a price: icon and number.</summary>
        private GameObject CreatePrice(Transform row, Vector2 at, ResearchTopic topic)
        {
            var price = new GameObject("Price", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            price.transform.SetParent(row, false);
            var rect = (RectTransform)price.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = at;
            rect.sizeDelta = new Vector2(230f, 34f);
            var layout = price.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 4f;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            if (topic == null) return price;

            void Chip(Sprite icon, int amount)
            {
                var image = NewImage(price.transform, "Icon", icon, Color.white, Vector2.zero, new Vector2(30f, 30f));
                image.preserveAspect = true;
                image.raycastTarget = false;
                var text = NewText(price.transform, "Amount", amount.ToString(), 20, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(16f + 12f * amount.ToString().Length, 30f), UiTheme.Ink, UiTheme.Bold);
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
            }

            if (topic.Coins > 0) Chip(iconLibrary != null ? iconLibrary.GetIcon(ResourceType.Coins) : null, topic.Coins);
            if (topic.Crystals > 0) Chip(crystalIcon, topic.Crystals);
            return price;
        }

        private void Act(Cell cell)
        {
            var research = ResearchManager.Instance;
            if (research == null || cell.Topic == null) return;
            if (research.Current == cell.Topic)
            {
                CancelResearch();
                return;
            }
            StartResearch(cell.Topic);
        }

        /// <summary>A picture tapped: what that level gives, on the status line.</summary>
        private void ShowLevelInfo(Cell cell)
        {
            var name = cell.IsUnit
                ? (SoldierStats.TryTypeFromSheetId(cell.TargetId, out var type) ? SoldierStats.DisplayName(type) : cell.TargetId)
                : (DataOf(cell.TargetId) != null ? DataOf(cell.TargetId).LocalizedName : cell.TargetId);
            var gain = DescribeGain(cell.TargetId, cell.Level, cell.IsUnit, DataOf(cell.TargetId));
            Say(Localization.Format("#research_level", name, cell.Level) + ": " + gain, UiTheme.Ink);
            Refresh();
        }

        private BuildingData DataOf(string id) => _dataById != null && id != null && _dataById.TryGetValue(id, out var data) ? data : null;

        /// <summary>
        /// What one level gives, in a line: level 1 opens the building or the hiring; a higher level
        /// the numbers it raises, as differences -- the player is choosing whether the coins are
        /// worth it, not reading a stat sheet. Public and static for the EditMode test.
        /// </summary>
        public static string DescribeGain(string targetId, int level, bool isUnit, BuildingData data = null)
        {
            if (isUnit)
            {
                if (level <= 1) return Localization.Get("#research_gain_opens_unit");
                if (!SoldierStats.TryTypeFromSheetId(targetId, out var type)) return string.Empty;
                var before = SoldierStats.StatsAt(type, level - 1);
                var after = SoldierStats.StatsAt(type, level);
                return Localization.Format("#research_unit_gain", after.maxHealth - before.maxHealth, after.attackDamage - before.attackDamage);
            }

            if (level <= 1) return Localization.Get("#research_gain_opens");
            if (data == null) return string.Empty;
            var now = data.LevelStats(level - 1);
            var then = data.LevelStats(level);
            var parts = new List<string>();
            void Gain(string key, int amount)
            {
                if (amount > 0) parts.Add(Localization.Format(key, amount));
            }
            Gain("#research_gain_health", then.maxHealth - now.maxHealth);
            Gain("#research_gain_defense", then.defense - now.defense);
            Gain("#research_gain_housing", then.housingCapacity - now.housingCapacity);
            Gain("#research_gain_workers", then.maxWorkers - now.maxWorkers);
            Gain("#research_gain_storage", then.storageCapacity - now.storageCapacity);
            Gain("#research_gain_radius", Mathf.Max(then.harvestRadius - now.harvestRadius, then.serviceRadius - now.serviceRadius));
            return parts.Count > 0 ? string.Join(", ", parts) : Localization.Get("#research_gain_none");
        }

        private Sprite IconFor(string targetId, int level, bool isUnit)
        {
            if (_iconsByKey == null)
            {
                _iconsByKey = new Dictionary<string, Sprite>();
                for (var i = 0; i < iconKeys.Length && i < icons.Length; i++) _iconsByKey[iconKeys[i]] = icons[i];
            }
            if (isUnit)
            {
                return SoldierStats.TryTypeFromSheetId(targetId, out var type) && _iconsByKey.TryGetValue("Portrait" + type, out var portrait) ? portrait : null;
            }
            // the level's own picture, else the nearest one below it
            for (var l = level; l >= 1; l--)
            {
                if (_iconsByKey.TryGetValue(l == 1 ? targetId : $"{targetId}_L{l}", out var sprite)) return sprite;
            }
            return null;
        }

        // -- refresh ---------------------------------------------------------------------------------

        /// <summary>Re-reads every number and state the window shows, without touching the rows themselves.</summary>
        private void Refresh()
        {
            if (panelRoot == null || !panelRoot.activeSelf) return;
            var research = ResearchManager.Instance;

            RefreshHeader();
            RefreshStatus(research);

            SetTab(buildingsTab, !_unitsTab);
            SetTab(unitsTab, _unitsTab);

            foreach (var cell in _cells) RefreshCell(cell, research);
        }

        private static void SetTab(RectTransform tab, bool selected)
        {
            if (tab == null) return;
            tab.localScale = Vector3.one * (selected ? SelectedTabScale : 1f);
            var image = tab.GetComponent<Image>();
            if (image != null) image.color = selected ? SelectedTabColor : UnselectedTabColor;
        }

        private void RefreshCell(Cell cell, ResearchManager research)
        {
            var topic = cell.Topic;
            var done = topic == null || (research != null && research.IsCompleted(topic.Id));
            var running = !done && research != null && research.Current == topic;

            cell.Tick.SetActive(done);
            cell.Bar.SetActive(running);
            cell.Price.SetActive(!done && !running);
            cell.Action.gameObject.SetActive(!done);
            cell.Icon.color = done || running ? Color.white : DimmedIcon;
            if (done) return;

            if (running)
            {
                var total = research.CurrentTotalSeconds;
                cell.BarFill.fillAmount = total > 0f ? 1f - research.RemainingSeconds / total : 0f;
                cell.BarText.text = research.LabWorkers > 0 ? Clock(research.RemainingSeconds) : "—";
                cell.ActionLabel.text = Localization.Get("#research_cancel_short");
                cell.ActionImage.color = CancelColor;
                cell.Action.interactable = true;
                return;
            }

            var waitsOnPrevious = topic.PrerequisiteTopicId != null && research != null && !research.IsCompleted(topic.PrerequisiteTopicId);
            var labTooSmall = research != null && research.LabLevel < topic.RequiredLabLevel;
            if (labTooSmall && !waitsOnPrevious)
            {
                cell.ActionLabel.text = Localization.Format("#research_cell_lab", topic.RequiredLabLevel);
                cell.ActionImage.color = LockedColor;
                cell.Action.interactable = false;
                return;
            }

            cell.ActionLabel.text = Localization.Get("#research_upgrade");
            if (waitsOnPrevious)
            {
                cell.ActionImage.color = LockedColor;
                cell.Action.interactable = false;
                return;
            }

            // the next step: green when it can start now; otherwise still tappable, so the status line can say why not
            var blocker = research != null ? research.DescribeBlocker(topic) : null;
            cell.ActionImage.color = blocker == null ? AvailableColor : LockedColor;
            cell.Action.interactable = true;
        }

        private void RefreshHeader()
        {
            if (crystalsLabel != null) crystalsLabel.text = Wallet.UpgradeCrystals.ToString();
            if (_lab == null || _lab.Data == null) return;

            if (titleLabel != null) titleLabel.text = Localization.Format("#research_title", _lab.Data.LocalizedName, _lab.Level);
            if (conditionLabel != null)
            {
                conditionLabel.gameObject.SetActive(_lab.CurrentHealth < _lab.MaxHealth);
                conditionLabel.text = $"{_lab.CurrentHealth}/{_lab.MaxHealth}";
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

        private void RefreshStatus(ResearchManager research)
        {
            if (statusLabel == null) return;
            if (_message != null && Time.unscaledTime < _messageUntil)
            {
                statusLabel.text = _message;
                statusLabel.color = _messageColor;
                return;
            }
            _message = null;
            statusLabel.color = UiTheme.Ink;

            var current = research != null ? research.Current : null;
            if (current == null)
            {
                statusLabel.text = Localization.Get("#research_idle");
                return;
            }
            statusLabel.text = research.LabWorkers > 0
                ? Localization.Format("#research_running", current.Title, Mathf.CeilToInt(research.RemainingSeconds))
                : Localization.Format("#research_paused", current.Title);
        }

        private void Say(string message, Color color)
        {
            _message = message;
            _messageColor = color;
            _messageUntil = Time.unscaledTime + MessageSeconds;
        }

        private static string Clock(float seconds)
        {
            var whole = Mathf.CeilToInt(seconds);
            return $"{whole / 60}:{whole % 60:00}";
        }

        // -- building blocks --------------------------------------------------------------------------

        private static Image NewImage(Transform parent, string name, Sprite sprite, Color color, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return image;
        }

        private static Text NewText(Transform parent, string name, string content, int fontSize, TextAnchor anchor, Vector2 position, Vector2 size, Color color, Font font = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.GetComponent<Text>();
            text.font = font != null ? font : UiTheme.Regular;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.text = content;
            text.raycastTarget = false;
            return text;
        }
    }
}
