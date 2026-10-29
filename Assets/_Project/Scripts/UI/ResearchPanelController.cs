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
    /// The Laboratory's window, after the user's sketch and final mockup (2026-10-28). Tapping the
    /// Laboratory opens THIS instead of the ordinary building card, which is why the card's own
    /// controls -- scientists, upgrade, repair -- live along its foot.
    ///
    /// «Лаборатория: уровень N» between the tabs «Здания» and «Армия» (the chosen one larger and
    /// lit); under it the player's coins and upgrade crystals, nothing else. Then one ROW per
    /// building (in the build menu's order) or per soldier: its name, its levels left to right, a
    /// picture each joined by a line, the price and a «Прокачать» button under each. A level already
    /// had shows a tick; one being researched its own timer and «Отмена» -- as many at once as there
    /// are scientists; one waiting on the level before it a grey button; one the Laboratory is too
    /// small for a grey «Лаб. ур. N». Tapping a picture shows what that level gives in a bubble over
    /// it; a level that cannot start says why there, or flashes the coins or the crystals red.
    /// «Улучшить лабораторию» and «Отремонтировать» open their own small window to confirm.
    /// </summary>
    public class ResearchPanelController : MonoBehaviour
    {
        /// <summary>How often the open window re-reads the countdowns and each cell's state. Every frame would allocate a fistful of strings per cell for no visible gain.</summary>
        private const float RefreshIntervalSeconds = 0.25f;

        private const float BubbleSeconds = 3f;
        private const float FlashSeconds = 0.8f;
        private const float ShakeSeconds = 0.35f;

        private const float CellStep = 250f;
        private const float RowHeight = 290f;
        private const float IconSize = 112f;
        private const float SelectedTabScale = 1.12f;

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text coinsLabel;
        [SerializeField] private Text crystalsLabel;
        [SerializeField] private Text scientistsLabel;
        [SerializeField] private Button addScientistButton;
        [SerializeField] private Button removeScientistButton;
        [SerializeField] private GameObject upgradeControls;
        [SerializeField] private GameObject repairControls;
        [SerializeField] private RectTransform buildingsTab;
        [SerializeField] private RectTransform unitsTab;
        [SerializeField] private RectTransform listContent;
        [SerializeField] private GameObject emptyLabel;
        [SerializeField] private GameObject hintPanel;
        [SerializeField] private Text hintText;
        [SerializeField] private RectTransform bubble;
        [SerializeField] private Text bubbleText;

        // the small window «Улучшить лабораторию» / «Отремонтировать» opens
        [SerializeField] private GameObject dialog;
        [SerializeField] private Text dialogTitle;
        [SerializeField] private Image dialogIcon;
        [SerializeField] private Text dialogGain;
        [SerializeField] private Transform dialogCostRow;
        [SerializeField] private Text dialogConfirmLabel;

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
            public RectTransform Frame;
            public Image Icon;
            public GameObject Tick;
            public GameObject Price;
            public GameObject Bar;
            public Image BarFill;
            public Text BarText;
            public Button Action;
            public Image ActionImage;
            public Text ActionLabel;
            public Vector2 ActionHome;
        }

        private readonly List<Cell> _cells = new List<Cell>();
        private int _rowCount;
        private Dictionary<string, Sprite> _iconsByKey;
        private Dictionary<string, BuildingData> _dataById;

        private BuildingInstance _lab;
        private ProductionBuilding _labWorkplace;
        private bool _unitsTab;
        private bool _dialogIsRepair;
        private float _refreshTimer;
        private float _bubbleUntil;
        private Text _flashing;
        private float _flashUntil;
        private Cell _shaking;
        private float _shakeUntil;

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

        /// <summary>What the bubble over a picture says while it is up, for the tests.</summary>
        public string BubbleText => bubble != null && bubble.gameObject.activeSelf && bubbleText != null ? bubbleText.text : string.Empty;

        /// <summary>The confirm window is up, for the tests.</summary>
        public bool IsDialogOpen => dialog != null && dialog.activeSelf;

        /// <summary>Opens the window on a particular Laboratory (the one that was tapped).</summary>
        public void Show(BuildingInstance laboratory)
        {
            _lab = laboratory;
            _labWorkplace = laboratory != null ? laboratory.GetComponent<ProductionBuilding>() : null;

            // Always opens on the buildings tab: that is where almost all of the list lives.
            _unitsTab = false;

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                // over the HUD's plaques (the day, the tax), which are built after it on the same canvas
                panelRoot.transform.SetAsLastSibling();
            }
            if (hintPanel != null) hintPanel.SetActive(false);
            if (dialog != null) dialog.SetActive(false);
            HideBubble();
            ModalGate.SetBlocked(true);

            var research = ResearchManager.Instance;
            if (research != null)
            {
                // Removed first: tapping a second Laboratory without closing the window would
                // otherwise subscribe twice and refresh twice per event.
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
            if (dialog != null) dialog.SetActive(false);
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

        /// <summary>«Улучшить лабораторию»: its own window with the next level's picture, what it gives and the price.</summary>
        public void OpenUpgradeDialog()
        {
            if (_lab == null || _lab.GetUpgradeCost() == null) return;
            _dialogIsRepair = false;
            var next = _lab.Level + 1;
            dialogTitle.text = Localization.Format("#research_level", _lab.Data.LocalizedName, next);
            dialogIcon.sprite = IconFor(_lab.Data.buildingName, next, isUnit: false);
            dialogGain.text = DescribeGain(_lab.Data.buildingName, next, false, _lab.Data) + "\n" + Localization.Format("#research_lab_opens", next);
            dialogConfirmLabel.text = Localization.Get("#building_upgrade");
            CostRowBuilder.Build(dialogCostRow, _lab.GetUpgradeCost(), iconLibrary);
            dialog.SetActive(true);
        }

        /// <summary>«Отремонтировать»: its own window with the Laboratory's picture and the price.</summary>
        public void OpenRepairDialog()
        {
            if (_lab == null || _lab.GetRepairCost() == null) return;
            _dialogIsRepair = true;
            dialogTitle.text = _lab.Data.LocalizedName;
            dialogIcon.sprite = IconFor(_lab.Data.buildingName, _lab.Level, isUnit: false);
            dialogGain.text = $"{_lab.CurrentHealth}/{_lab.MaxHealth}";
            dialogConfirmLabel.text = Localization.Get("#building_repair");
            CostRowBuilder.Build(dialogCostRow, _lab.GetRepairCost(), iconLibrary);
            dialog.SetActive(true);
        }

        /// <summary>The confirm window's own button: the upgrade or the repair it was opened for.</summary>
        public void ConfirmDialog()
        {
            if (_lab != null)
            {
                if (_dialogIsRepair) _lab.TryRepair();
                else _lab.TryUpgrade();
            }
            CloseDialog();
        }

        public void CloseDialog()
        {
            if (dialog != null) dialog.SetActive(false);
            Refresh();
        }

        /// <summary>The «!»: why the Laboratory is worth upgrading and what the scientists do.</summary>
        public void ToggleHint()
        {
            if (hintPanel == null) return;
            var show = !hintPanel.activeSelf;
            hintPanel.SetActive(show);
            if (show && hintText != null) hintText.text = Localization.Get("#research_hint");
        }

        /// <summary>Starts a research; when it cannot, the counter it is short of flashes red, or a bubble over the picture says why.</summary>
        public void StartResearch(ResearchTopic topic)
        {
            var research = ResearchManager.Instance;
            if (research == null) return;
            var blocker = research.DescribeBlocker(topic);
            if (blocker == null && research.TryStart(topic))
            {
                HideBubble();
                Refresh();
                return;
            }

            var cell = CellOf(topic);
            if (cell != null)
            {
                _shaking = cell;
                _shakeUntil = Time.unscaledTime + ShakeSeconds;
            }
            if (blocker == Localization.Get("#research_blocked_coins")) Flash(coinsLabel);
            else if (blocker == Localization.Get("#research_blocked_crystals")) Flash(crystalsLabel);
            else if (cell != null) ShowBubble(cell, blocker ?? Localization.Get("#research_blocked_unknown"));
            Refresh();
        }

        /// <summary>«Отмена» under a level being researched.</summary>
        public void CancelResearch(ResearchTopic topic)
        {
            ResearchManager.Instance?.Cancel(topic);
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

            Animate();

            _refreshTimer += Time.unscaledDeltaTime;
            if (_refreshTimer < RefreshIntervalSeconds) return;
            _refreshTimer = 0f;
            Refresh();
        }

        /// <summary>The red flash on a counter, the shake of a button, the bubble's time running out.</summary>
        private void Animate()
        {
            var now = Time.unscaledTime;
            if (_flashing != null)
            {
                var on = now < _flashUntil;
                _flashing.color = on ? Color.Lerp(UiTheme.Ink, UiTheme.Danger, Mathf.PingPong(now * 8f, 1f)) : UiTheme.Ink;
                _flashing.transform.localScale = Vector3.one * (on ? 1.15f : 1f);
                if (!on) _flashing = null;
            }
            if (_shaking != null)
            {
                var rect = (RectTransform)_shaking.Action.transform;
                var on = now < _shakeUntil;
                rect.anchoredPosition = _shaking.ActionHome + (on ? new Vector2(Mathf.Sin(now * 60f) * 8f, 0f) : Vector2.zero);
                if (!on) _shaking = null;
            }
            if (bubble != null && bubble.gameObject.activeSelf && now > _bubbleUntil) HideBubble();
        }

        private void Flash(Text counter)
        {
            if (_flashing != null && _flashing != counter)
            {
                _flashing.color = UiTheme.Ink;
                _flashing.transform.localScale = Vector3.one;
            }
            _flashing = counter;
            _flashUntil = Time.unscaledTime + FlashSeconds;
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
            _shaking = null;
            HideBubble();

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
            cell.Frame = frame.rectTransform;
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

            // the research's own timer, across the picture's foot
            var bar = NewImage(frame.transform, "Progress", null, new Color(0f, 0f, 0f, 0.6f), new Vector2(0f, -(IconSize * 0.5f) + 14f), new Vector2(IconSize, 28f));
            bar.raycastTarget = false;
            var fill = NewImage(bar.transform, "Fill", completedIcon, UiTheme.Good, Vector2.zero, new Vector2(IconSize, 28f));
            fill.raycastTarget = false;
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            fill.type = Image.Type.Filled;    // a Filled image needs a sprite; its look is the colour
            fill.fillMethod = Image.FillMethod.Horizontal;
            cell.BarFill = fill;
            cell.BarText = NewText(bar.transform, "Time", string.Empty, 18, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(IconSize, 28f), Color.white, UiTheme.Bold);
            cell.Bar = bar.gameObject;

            cell.Price = CreatePrice(row, new Vector2(at.x, at.y - 88f), topic);

            cell.ActionHome = new Vector2(at.x, at.y - 130f);
            var action = NewImage(row, $"Action{level}", buttonSprite, AvailableColor, cell.ActionHome, new Vector2(190f, 50f));
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

            if (topic.Coins > 0) Chip(price.transform, iconLibrary != null ? iconLibrary.GetIcon(ResourceType.Coins) : null, topic.Coins);
            if (topic.Crystals > 0) Chip(price.transform, crystalIcon, topic.Crystals);
            return price;
        }

        private static void Chip(Transform parent, Sprite icon, int amount)
        {
            var image = NewImage(parent, "Icon", icon, Color.white, Vector2.zero, new Vector2(30f, 30f));
            image.preserveAspect = true;
            image.raycastTarget = false;
            var text = NewText(parent, "Amount", amount.ToString(), 20, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(16f + 12f * amount.ToString().Length, 30f), UiTheme.Ink, UiTheme.Bold);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        private void Act(Cell cell)
        {
            var research = ResearchManager.Instance;
            if (research == null || cell.Topic == null) return;
            if (research.IsRunning(cell.Topic))
            {
                CancelResearch(cell.Topic);
                return;
            }
            StartResearch(cell.Topic);
        }

        private Cell CellOf(ResearchTopic topic)
        {
            foreach (var cell in _cells)
            {
                if (cell.Topic == topic) return cell;
            }
            return null;
        }

        /// <summary>A picture tapped: what that level gives, in a bubble over it.</summary>
        private void ShowLevelInfo(Cell cell)
        {
            var data = DataOf(cell.TargetId);
            var name = cell.IsUnit
                ? (SoldierStats.TryTypeFromSheetId(cell.TargetId, out var type) ? SoldierStats.DisplayName(type) : cell.TargetId)
                : (data != null ? data.LocalizedName : cell.TargetId);
            ShowBubble(cell, Localization.Format("#research_level", name, cell.Level) + "\n" + DescribeGain(cell.TargetId, cell.Level, cell.IsUnit, data));
        }

        private void ShowBubble(Cell cell, string text)
        {
            if (bubble == null || bubbleText == null) return;
            bubbleText.text = text;
            bubble.gameObject.SetActive(true);
            bubble.SetAsLastSibling();
            // the rows may have been laid out this very frame (a tab switch): place by where they end up
            Canvas.ForceUpdateCanvases();
            // over the picture, kept inside the card
            bubble.position = cell.Frame.position;
            var local = bubble.anchoredPosition + new Vector2(0f, IconSize * 0.5f + bubble.sizeDelta.y * 0.5f + 12f);
            var card = (RectTransform)bubble.parent;
            var halfW = card.rect.width * 0.5f - bubble.sizeDelta.x * 0.5f - 10f;
            local.x = Mathf.Clamp(local.x, -halfW, halfW);
            bubble.anchoredPosition = local;
            _bubbleUntil = Time.unscaledTime + BubbleSeconds;
        }

        private void HideBubble()
        {
            if (bubble != null) bubble.gameObject.SetActive(false);
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
            var running = !done && research != null && research.IsRunning(topic);

            cell.Tick.SetActive(done);
            cell.Bar.SetActive(running);
            cell.Price.SetActive(!done && !running);
            cell.Action.gameObject.SetActive(!done);
            cell.Icon.color = done || running ? Color.white : DimmedIcon;
            if (done) return;

            if (running)
            {
                var total = ResearchManager.TotalSeconds(topic);
                var left = research.RemainingSeconds(topic);
                cell.BarFill.fillAmount = total > 0f ? 1f - left / total : 0f;
                cell.BarText.text = research.IsPaused(topic) ? "—" : Clock(left);
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

            // the next step: green when it can start now; otherwise still tappable, so it can say why not
            var blocker = research != null ? research.DescribeBlocker(topic) : null;
            cell.ActionImage.color = blocker == null ? AvailableColor : LockedColor;
            cell.Action.interactable = true;
        }

        private void RefreshHeader()
        {
            var resources = ResourceManager.Instance;
            if (coinsLabel != null) coinsLabel.text = resources != null ? resources.GetAmount(ResourceType.Coins).ToString() : "0";
            if (crystalsLabel != null) crystalsLabel.text = Wallet.UpgradeCrystals.ToString();
            if (_lab == null || _lab.Data == null) return;

            if (titleLabel != null) titleLabel.text = Localization.Format("#research_title", _lab.Data.LocalizedName, _lab.Level);

            var assigned = _labWorkplace != null ? _labWorkplace.AssignedWorkers : 0;
            var max = _labWorkplace != null ? _labWorkplace.MaxWorkers : 0;
            var idle = CitizenManager.Instance != null ? CitizenManager.Instance.IdlePopulation : 0;
            if (scientistsLabel != null) scientistsLabel.text = Localization.Format("#research_scientists", assigned, max, idle);
            if (addScientistButton != null) addScientistButton.interactable = assigned < max && idle > 0;
            if (removeScientistButton != null) removeScientistButton.interactable = assigned > 0;

            if (upgradeControls != null) upgradeControls.SetActive(_lab.GetUpgradeCost() != null);
            if (repairControls != null) repairControls.SetActive(_lab.GetRepairCost() != null);
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
