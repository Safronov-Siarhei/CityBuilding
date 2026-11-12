using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    public class BuildingInfoPanelController : MonoBehaviour
    {
        /// <summary>The one building that recruits. Which tiers it offers is SoldierStats.All, and which of those it will actually raise is the Laboratory's business (see ArmyManager.DescribeRecruitBlocker).</summary>
        private const string RecruitBuildingName = "Barracks";

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text levelLabel;
        [SerializeField] private Text conditionLabel;
        [SerializeField] private Text productionLabel;
        [SerializeField] private Text warningLabel;
        [SerializeField] private GameObject recipeControls;
        [SerializeField] private RectTransform recipeRow;
        [SerializeField] private Sprite recipeButtonSprite;
        [SerializeField] private GameObject workerControls;
        [SerializeField] private Text workersLabel;
        [SerializeField] private Text idleLabel;
        [SerializeField] private GameObject upgradeControls;
        [SerializeField] private Transform upgradeCostRow;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Text upgradeLockLabel;
        [SerializeField] private GameObject repairControls;
        [SerializeField] private Transform repairCostRow;
        [SerializeField] private GameObject recruitControls;
        [SerializeField] private Text recruitLabel;
        [SerializeField] private Transform recruitCostRow;
        [SerializeField] private RectTransform recruitTypeRow;
        [SerializeField] private GameObject demolishControls;
        [SerializeField] private GameObject demolishConfirmRoot;
        [SerializeField] private Text demolishConfirmLabel;
        [SerializeField] private ResourceIconLibrary iconLibrary;

        [Header("An Invasion run's card (the user's sketch, 2026-10-25) in place of the one above")]
        [SerializeField] private GameObject sandboxCard;
        [SerializeField] private RunBuildingCard runCard;

        private BuildingInstance _currentInstance;
        private ProductionBuilding _currentProduction;

        /// <summary>The building whose card this is, or null.</summary>
        public BuildingInstance CurrentInstance => _currentInstance;

        /// <summary>The card is up.</summary>
        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        /// <summary>«Улучшить» (BuildHints points at it), or null while hidden.</summary>
        public RectTransform UpgradeButtonRect => upgradeButton != null && upgradeButton.gameObject.activeInHierarchy ? (RectTransform)upgradeButton.transform : null;

        public void Show(BuildingInstance instance)
        {
            _currentInstance = instance;
            _currentProduction = instance != null ? instance.GetComponent<ProductionBuilding>() : null;
            if (panelRoot != null) panelRoot.SetActive(true);
            var run = RunCard;
            if (sandboxCard != null) sandboxCard.SetActive(!run);
            if (runCard != null) runCard.gameObject.SetActive(run);
            ModalGate.SetBlocked(true);
            WireUpgradeLock();
            Refresh();
            ShowHarvestRadius();
        }

        /// <summary>
        /// «Уровень N: нужно исследование» takes a tap: to that research in the Laboratory (BuildHints;
        /// the user, 2026-11-11). Wired once, at the first card.
        /// </summary>
        private void WireUpgradeLock()
        {
            if (upgradeLockLabel == null || upgradeLockLabel.GetComponent<Button>() != null) return;
            upgradeLockLabel.raycastTarget = true;
            var button = upgradeLockLabel.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(ShowUpgradeResearch);
        }

        public void ShowUpgradeResearch()
        {
            var b = _currentInstance;
            if (b == null || b.Data == null || b.Site != null || b.NextLevelResearched || !BuildHints.Allowed) return;
            BuildHints.LeadToResearch(Research.ResearchTopic.BuildingLevelId(b.Data.buildingName, b.Level + 1));
        }

        /// <summary>m:ss.</summary>
        private static string Clock(float seconds)
        {
            var s = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            return $"{s / 60}:{s % 60:00}";
        }

        private float _siteRefreshIn;

        /// <summary>A card on a building under construction counts down, and opens up when it is done.</summary>
        private void Update()
        {
            if (_currentInstance == null) return;
            if (_currentInstance.Site == null && !_wasSite) return;
            _siteRefreshIn -= Time.unscaledDeltaTime;
            if (_siteRefreshIn > 0f) return;
            _siteRefreshIn = 0.25f;
            _wasSite = _currentInstance.Site != null;
            Refresh();
        }

        private bool _wasSite;

        public void Close()
        {
            CancelDemolish();
            _currentInstance = null;
            _currentProduction = null;
            if (panelRoot != null) panelRoot.SetActive(false);
            ModalGate.SetBlocked(false);
            HideHarvestRadius();
        }

        /// <summary>
        /// Demolition asks first, and says what comes back before it does. It is the only
        /// irreversible button on the card, it sits a finger's width from Upgrade, and a phone is
        /// where the wrong one gets pressed -- so a confirmation is not ceremony here.
        /// </summary>
        public void OpenDemolishConfirm()
        {
            if (_currentInstance == null || !_currentInstance.CanDemolish) return;

            if (demolishConfirmLabel != null)
            {
                demolishConfirmLabel.text = Localization.Format(
                    "#demolish_confirm", _currentInstance.Data.LocalizedName, DescribeRefund());
            }
            if (demolishConfirmRoot != null) demolishConfirmRoot.SetActive(true);
        }

        public void CancelDemolish()
        {
            if (demolishConfirmRoot != null) demolishConfirmRoot.SetActive(false);
        }

        public void ConfirmDemolish()
        {
            var instance = _currentInstance;

            // Closed BEFORE the building goes, or the card is left pointing at a destroyed object
            // and its next Refresh walks into a null.
            Close();
            if (instance != null) instance.Demolish();
        }

        /// <summary>What the refund actually is, spelled out -- "half of everything" is not a number a player can act on.</summary>
        private string DescribeRefund()
        {
            var refund = _currentInstance != null ? _currentInstance.GetDemolishRefund() : null;
            if (refund == null || refund.Count == 0) return Localization.Get("#demolish_no_refund");

            var text = string.Empty;
            foreach (var amount in refund)
            {
                if (text.Length > 0) text += ", ";
                text += ResourceNames.Of(amount.type) + " " + amount.amount;
            }
            return text;
        }

        public void AssignWorker()
        {
            _currentProduction?.TryAssignWorker();
            Refresh();
        }

        public void UnassignWorker()
        {
            _currentProduction?.TryUnassignWorker();
            Refresh();
        }

        public void Upgrade()
        {
            // the button keeps no pressed look after the click
            if (UnityEngine.EventSystems.EventSystem.current != null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            if (_currentInstance == null) return;
            if (!_currentInstance.TryUpgrade() && _currentInstance.NextLevelResearched && _currentInstance.IsOperational
                && _currentInstance.Site == null && Shop.OfferShop.Instance != null)
            {
                // too poor for it (sandbox only): «Не хватает ресурсов» with exactly the shortfall; after
                // buying the player presses Upgrade again (the user, 2026-11-11)
                Shop.OfferShopWindow.ShowShortage(Shop.OfferShop.Instance.ShortageFor(_currentInstance.GetUpgradeCost()));
            }
            Refresh();
        }

        public void Repair()
        {
            _currentInstance?.TryRepair();
            Refresh();
        }

        /// <summary>
        /// Raises one soldier of the selected tier at this Barracks -- costs coins, the tier's
        /// smelted kit, and one idle citizen (see ArmyManager). The panel stays open so the player
        /// can raise several in a row.
        /// </summary>
        public void Recruit()
        {
            if (_currentInstance == null || ArmyManager.Instance == null) return;

            ArmyManager.Instance.TryRecruit(_recruitType, _currentInstance.transform.position, _currentInstance.Level);
            Refresh();
        }

        /// <summary>Picks the tier the Recruit button will raise. A locked tier can be selected on purpose: the label then says what is missing and the price is still readable, which is how a player finds out what the Laboratory would buy them.</summary>
        public void SelectRecruitType(int index)
        {
            if (index < 0 || index >= SoldierStats.All.Length) return;

            _recruitType = SoldierStats.All[index];
            Refresh();
        }

        /// <summary>The user's card (RunBuildingCard) stands in for the old one -- in a run, and in the sandbox since 2026-10-25.</summary>
        private bool RunCard => runCard != null;

        private void Refresh()
        {
            if (_currentInstance == null || _currentInstance.Data == null) return;
            var data = _currentInstance.Data;
            if (RunCard)
            {
                runCard.Refresh(_currentInstance, _currentProduction, BuildCostRow, Clock);
                // the sandbox's band: what it makes, a bare reach, a Плавильня's metals (a run's card has none)
                if (Invasion.InvasionMode.IsActive || data.isMoat)
                {
                    runCard.ShowBand(null, false, false);
                    return;
                }
                var empty = _currentProduction != null && _currentProduction.NothingLeftInReach;
                var choice = _currentProduction != null && _currentProduction.HasRecipeChoice;
                runCard.ShowBand(empty ? Localization.Format("#building_nothing_in_reach", ResourceNames.Of(_currentProduction.ProducesResource)) : ProductionLine(data) ?? DecorLine(), empty, choice);
                if (choice) RefreshRecipeControls(null, runCard.RecipeRow);
                return;
            }

            if (titleLabel != null) titleLabel.text = data.LocalizedName;
            if (levelLabel != null) levelLabel.text = Localization.Format("#building_level", _currentInstance.Level, BuildingInstance.MaxLevel);
            if (conditionLabel != null)
            {
                // Roads/Town Hall never accrue decay at all (see BuildingInstance.DecaysOverTime) --
                // showing them as a static "0%" reads like a decaying building that just hasn't
                // aged yet, so they get their own label instead.
                var decayText = _currentInstance.DecaysOverTime
                    ? $"{Mathf.RoundToInt(_currentInstance.Decay * 100f)}%"
                    : Localization.Get("#building_no_decay");
                conditionLabel.text = Localization.Format("#building_condition", _currentInstance.CurrentHealth, _currentInstance.MaxHealth, _currentInstance.Defense, decayText);
            }

            var hasProduction = _currentProduction != null && _currentProduction.MaxWorkers > 0;
            if (workerControls != null) workerControls.SetActive(hasProduction);
            if (productionLabel != null && !hasProduction && _currentInstance.Happiness > 0)
            {
                // An entertainment building has no workers and makes nothing, so this line would
                // otherwise be blank -- and a tavern whose card says nothing about what it is for
                // reads exactly like the placeholder it used to be.
                productionLabel.text = Localization.Format("#building_happiness", _currentInstance.Happiness);
            }
            else if (productionLabel != null)
            {
                productionLabel.text = hasProduction
                    ? FoodLine() ?? ProductionSummary(_currentProduction.SelectedRecipe, _currentProduction.BatchesPerWorkerPerTick, data.productionIntervalSeconds)
                    : string.Empty;
            }
            RefreshRecipeControls();
            RefreshGatherWarning();
            if (hasProduction)
            {
                if (workersLabel != null) workersLabel.text = Localization.Format("#building_workers", _currentProduction.AssignedWorkers, _currentProduction.MaxWorkers);
                if (idleLabel != null)
                {
                    var idle = CitizenManager.Instance != null ? CitizenManager.Instance.IdlePopulation : 0;
                    idleLabel.text = Localization.Format("#building_idle_citizens", idle);
                }
            }

            // Three states, not two: no next level at all (the block disappears), a next level the
            // Laboratory has not permitted yet (the price is replaced by what is missing), or an
            // upgrade the player can actually pay for.
            var upgradeCost = _currentInstance.GetUpgradeCost();
            var researched = _currentInstance.NextLevelResearched;
            // being built or upgraded (an Invasion run): the time left stands where the price would
            var site = _currentInstance.Site;
            var open = researched && site == null;
            if (upgradeControls != null) upgradeControls.SetActive(upgradeCost != null || site != null);
            if (upgradeButton != null) upgradeButton.gameObject.SetActive(open);
            if (upgradeCostRow != null) upgradeCostRow.gameObject.SetActive(open);
            if (upgradeLockLabel != null)
            {
                upgradeLockLabel.gameObject.SetActive(!open);
                if (site != null) upgradeLockLabel.text = Localization.Format(site.IsUpgrade ? "#building_upgrading" : "#building_constructing", Clock(site.Remaining));
                else if (!researched) upgradeLockLabel.text = Localization.Format("#building_upgrade_locked", _currentInstance.Level + 1);
            }
            BuildCostRow(upgradeCostRow, open ? upgradeCost : null);

            var repairCost = _currentInstance.GetRepairCost();
            if (repairControls != null) repairControls.SetActive(repairCost != null);
            BuildCostRow(repairCostRow, repairCost);

            // Hidden entirely on the Town Hall rather than shown greyed out: there is no version of
            // this game where demolishing it is a thing the player may do.
            if (demolishControls != null) demolishControls.SetActive(_currentInstance.CanDemolish);

            RefreshRecruitControls(data);
        }

        /// <summary>What a building makes, per worker and tick -- or null for one that makes nothing.</summary>
        private string ProductionLine(BuildingData data)
        {
            var hasProduction = _currentProduction != null && _currentProduction.MaxWorkers > 0;
            if (!hasProduction) return null;
            var line = FoodLine() ?? ProductionSummary(_currentProduction.SelectedRecipe, _currentProduction.BatchesPerWorkerPerTick, data.productionIntervalSeconds);
            return string.IsNullOrEmpty(line) ? null : line;
        }

        /// <summary>
        /// Decor's money (the user, 2026-11-12): a decoration says what it gives each resident of the
        /// houses in its reach and how far that is; a house says what its residents get from the decor
        /// round it. Null for anything else, and for a house no decor reaches.
        /// </summary>
        private string DecorLine()
        {
            if (_currentInstance == null || _currentInstance.Data == null) return null;
            var stats = _currentInstance.Data.LevelStats(_currentInstance.Level);
            if (stats.residentCoins > 0f)
                return Localization.Format("#decor_card_gives", Citizens.DecorBonus.Coins(stats.residentCoins), stats.serviceRadius);
            if (!BuildingInstance.IsHouse(_currentInstance.Data)) return null;
            var bonus = Citizens.DecorBonus.PerResident(_currentInstance);
            return bonus > 0f ? Localization.Format("#decor_card_house", Citizens.DecorBonus.Coins(bonus)) : null;
        }

        /// <summary>
        /// A food building's line (the food rework, 2026-11-02): «Еда: +N в день» with every bonus
        /// counted, or what a Bakery/Smokehouse adds to its neighbours and how far it reaches; null
        /// for any other building.
        /// </summary>
        private string FoodLine()
        {
            var id = _currentProduction != null ? _currentProduction.BuildingId : null;
            if (id == null) return null;
            if (Citizens.FoodOutput.IsProducer(id)) return Localization.Format("#food_card_per_day", Citizens.FoodOutput.PerDay(_currentProduction));
            if (!Citizens.FoodOutput.IsBooster(id)) return null;
            var reach = _currentInstance != null && _currentInstance.Data != null ? _currentInstance.Data.LevelStats(_currentInstance.Level).serviceRadius : 0;
            return Localization.Format(id == BuildingIds.Baker ? "#food_card_boost_baker" : "#food_card_boost_smokehouse",
                Citizens.FoodOutput.BoostGiven(_currentProduction), reach);
        }

        /// <summary>
        /// Says, for as long as it is true, that this gatherer has nothing left inside its radius.
        ///
        /// The event log alone was not enough and cost a real game: the line is written ONCE, the
        /// day it becomes true (CitizenVisualsManager.ReportRadiusEmpty), and by the time the
        /// player wonders why no wood is arriving it has scrolled out of an eight-line log. After
        /// that a starved sawmill and a working one are the same picture -- the workers stand on
        /// the plot either way.
        ///
        /// Asked fresh on every Refresh rather than mirroring that latch, because the latch only
        /// moves while somebody is employed here (CitizenVisualsManager.AssignAgentToBuilding): an
        /// unstaffed building would otherwise report whatever was true when its last worker left.
        /// </summary>
        private void RefreshGatherWarning()
        {
            if (warningLabel == null) return;

            var empty = _currentProduction != null && _currentProduction.NothingLeftInReach;

            warningLabel.gameObject.SetActive(empty);
            if (empty)
            {
                warningLabel.text = Localization.Format(
                    "#building_nothing_in_reach", ResourceNames.Of(_currentProduction.ProducesResource));
            }
        }

        /// <summary>Where the building actually stands, footprint and rotation included -- the same point its workers measure their radius from.</summary>
        private Vector3 FootprintCenter()
        {
            var grid = GridManager.Instance;
            if (grid == null || _currentInstance == null) return Vector3.zero;
            return grid.GetFootprintCenterWorld(_currentInstance.OriginCell, _currentInstance.RotatedFootprint());
        }

        /// <summary>
        /// The production line: what one worker gets through in a tick, spelled as the recipe
        /// itself. Stated per worker because that is how the numbers are authored and how the
        /// player reasons about the +/- buttons right below it.
        ///
        /// Pure and static so the wording is covered by an EditMode test without a canvas.
        /// </summary>
        public static string ProductionSummary(BuildingRecipe recipe, int batchesPerWorker, float intervalSeconds)
        {
            if (recipe == null || batchesPerWorker <= 0) return string.Empty;

            // The space belongs HERE, not inside the localized string. It used to live at the
            // front of `#recipe_batches` (" x{0}" with a leading space) because this line
            // concatenates directly -- and an invisible leading space in a spreadsheet cell is a
            // thing nobody can see, nobody can review, and Sheets had already silently dropped
            // once. The symptom was "Мукаx2" with no clue where the space went. The cell now says
            // `x{0}`, which is what it means, and the layout decision lives in the layout code.
            var batches = batchesPerWorker > 1
                ? " " + Localization.Format("#recipe_batches", batchesPerWorker)
                : string.Empty;
            return Localization.Format("#building_produces", recipe.Describe() + batches, intervalSeconds.ToString("0.#"));
        }

        /// <summary>
        /// The metal buttons on a Плавильня. Built at runtime and only for a building that has a
        /// real choice -- a sawmill with one recipe would otherwise get a row holding a single
        /// button that does nothing, which reads as broken rather than as "no choice here".
        /// </summary>
        private void RefreshRecipeControls() => RefreshRecipeControls(recipeControls, recipeRow);

        private void RefreshRecipeControls(GameObject controls, RectTransform row)
        {
            var hasChoice = _currentProduction != null && _currentProduction.HasRecipeChoice;
            if (controls != null) controls.SetActive(hasChoice);
            if (!hasChoice || row == null) return;

            // Rebuilt only when a different building is selected: unlike the workforce list this is
            // a handful of buttons that never change while one card is open.
            if (_recipeButtonsOwner != _currentProduction || _recipeButtonsRow != row)
            {
                for (var i = row.childCount - 1; i >= 0; i--)
                {
                    Destroy(row.GetChild(i).gameObject);
                }
                _recipeButtons.Clear();
                _recipeButtonsOwner = _currentProduction;
                _recipeButtonsRow = row;

                for (var i = 0; i < _currentProduction.Recipes.Count; i++)
                {
                    _recipeButtons.Add(CreateRecipeButton(row, i, _currentProduction.Recipes[i]));
                }
            }

            for (var i = 0; i < _recipeButtons.Count; i++)
            {
                _recipeButtons[i].color = i == _currentProduction.SelectedRecipeIndex ? SelectedRecipeColor : UnselectedRecipeColor;
            }
        }

        private Image CreateRecipeButton(RectTransform row, int index, BuildingRecipe recipe)
        {
            const float width = 210f;
            const float spacing = 12f;
            var count = _currentProduction.Recipes.Count;

            var go = new GameObject($"Recipe_{recipe.id}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(row, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2((index - (count - 1) * 0.5f) * (width + spacing), 0f);
            rect.sizeDelta = new Vector2(width, 58f);

            var image = go.GetComponent<Image>();
            image.sprite = recipeButtonSprite;
            image.type = Image.Type.Sliced;

            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGO.transform.SetParent(go.transform, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(6f, 4f);
            labelRect.offsetMax = new Vector2(-6f, -4f);

            var label = labelGO.GetComponent<Text>();
            label.font = UiTheme.Regular;
            label.fontSize = 18;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UiTheme.Ink;
            label.text = recipe.LocalizedName;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var captured = index;
            button.onClick.AddListener(() => SelectRecipe(captured));

            return image;
        }

        public void SelectRecipe(int index)
        {
            _currentProduction?.SelectRecipe(index);
            Refresh();
        }

        private static readonly Color SelectedRecipeColor = UiTheme.RowSelected;
        private static readonly Color UnselectedRecipeColor = UiTheme.RowNormal;

        /// <summary>A tier the Laboratory has not opened yet: dimmer than an available one, and still tappable so its price and its refusal can be read.</summary>
        private static readonly Color LockedTierColor = UiTheme.RowLocked;

        private readonly List<Image> _recipeButtons = new List<Image>();
        private ProductionBuilding _recipeButtonsOwner;
        private RectTransform _recipeButtonsRow;

        private readonly List<Image> _recruitButtons = new List<Image>();

        /// <summary>The tier the Recruit button will raise. Starts on Militia, the one tier open from the first minute.</summary>
        private SoldierType _recruitType = SoldierType.Militia;

        /// <summary>
        /// Recruitment lives on the Barracks only. The label doubles as the refusal reason (army
        /// full / no idle citizens / not enough coins) so a player who taps a button that does
        /// nothing is told which of the three limits they hit, rather than being left guessing at
        /// a greyed-out button.
        /// </summary>
        private void RefreshRecruitControls(BuildingData data)
        {
            var army = ArmyManager.Instance;
            var isBarracks = data.buildingName == RecruitBuildingName;

            if (recruitControls != null) recruitControls.SetActive(isBarracks && army != null && _currentInstance.IsOperational);
            if (!isBarracks || army == null) return;

            RefreshRecruitTypeButtons();
            BuildCostRow(recruitCostRow, SoldierStats.RecruitCost(_recruitType));

            if (recruitLabel == null) return;
            var blocker = army.DescribeRecruitBlocker(_recruitType, _currentInstance.Level);
            // a run's soldiers are bought once and cost nothing by the day (the user, 2026-10-25): no upkeep shown
            recruitLabel.text = blocker ?? (Invasion.InvasionMode.IsActive || !ArmyManager.UpkeepCharged
                ? Localization.Format("#army_summary_run", army.SoldierCount, SoldierStats.MaxArmySize)
                : Localization.Format("#army_summary", army.SoldierCount, SoldierStats.MaxArmySize, army.DailyUpkeep));
        }

        /// <summary>
        /// One button per tier, in the sheet's own order. Built once for the lifetime of the panel
        /// -- unlike the recipe row there is nothing per-building about them, every Barracks offers
        /// the same four.
        ///
        /// Three colours rather than two: selected, available, and locked. A locked tier stays
        /// tappable, because a greyed-out row that says nothing is how a player never learns what
        /// the Laboratory is for.
        /// </summary>
        private void RefreshRecruitTypeButtons()
        {
            if (recruitTypeRow == null) return;

            if (_recruitButtons.Count == 0)
            {
                for (var i = 0; i < SoldierStats.All.Length; i++)
                {
                    _recruitButtons.Add(CreateRecruitTypeButton(i, SoldierStats.All[i]));
                }
            }

            for (var i = 0; i < _recruitButtons.Count; i++)
            {
                var type = SoldierStats.All[i];
                _recruitButtons[i].color = type == _recruitType
                    ? SelectedRecipeColor
                    : (SoldierStats.IsUnlocked(type) ? UnselectedRecipeColor : LockedTierColor);
            }
        }

        private Image CreateRecruitTypeButton(int index, SoldierType type)
        {
            // Narrower than the recipe buttons because there are four of them rather than three:
            // 4 x 155 plus the gaps is 644, which fits the row's 660.
            const float width = 124f;   // five since the crossbowman (2026-11-05): 5 x 124 plus the gaps fits 660
            const float spacing = 8f;
            var count = SoldierStats.All.Length;

            var go = new GameObject($"Recruit_{type}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(recruitTypeRow, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2((index - (count - 1) * 0.5f) * (width + spacing), 0f);
            rect.sizeDelta = new Vector2(width, 58f);

            var image = go.GetComponent<Image>();
            image.sprite = recipeButtonSprite;
            image.type = Image.Type.Sliced;

            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGO.transform.SetParent(go.transform, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(4f, 4f);
            labelRect.offsetMax = new Vector2(-4f, -4f);

            var label = labelGO.GetComponent<Text>();
            label.font = UiTheme.Regular;
            label.fontSize = 17;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.color = UiTheme.Ink;
            label.text = SoldierStats.DisplayName(type);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var captured = index;
            button.onClick.AddListener(() => SelectRecruitType(captured));

            return image;
        }

        /// <summary>The icon+number chips for one cost -- see CostRowBuilder, which the Laboratory's window shares.</summary>
        private void BuildCostRow(Transform row, List<ResourceAmount> cost)
        {
            CostRowBuilder.Build(row, cost, iconLibrary);
        }

        /// <summary>
        /// The gatherer's reach, at the level it actually stands at rather than the level it was
        /// built at -- upgrading a Sawmill widens it, and watching that happen is most of the
        /// reason to upgrade one. Nothing is drawn for the other 47 buildings.
        ///
        /// There used to be a second overlay here as well: a blue square sized to fogRevealRadius,
        /// added because that stat had no visible representation anywhere. It is gone at the
        /// user's decision -- how far a building lifts the fog is not a number the player is asked
        /// to make decisions with, and two radii around one building read as clutter. It also
        /// lied: the fog is revealed in a CIRCLE (see FogOfWarManager.RevealPermanent) and the
        /// indicator drew a square, whose corners promised ground that stayed fogged.
        /// </summary>
        private void ShowHarvestRadius()
        {
            if (_currentInstance == null || _currentInstance.Data == null) return;
            var grid = GridManager.Instance;
            if (grid == null) return;

            Grid.HarvestRadiusOverlay.ShowFor(FootprintCenter(), _currentInstance.HarvestRadius, _currentInstance.Data);
        }

        private void HideHarvestRadius()
        {
            Grid.HarvestRadiusOverlay.HideIfShown();
        }
    }
}
