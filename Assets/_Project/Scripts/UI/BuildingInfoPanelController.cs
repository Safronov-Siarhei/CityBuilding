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
        [SerializeField] private ResourceIconLibrary iconLibrary;

        private BuildingInstance _currentInstance;
        private ProductionBuilding _currentProduction;

        public void Show(BuildingInstance instance)
        {
            _currentInstance = instance;
            _currentProduction = instance != null ? instance.GetComponent<ProductionBuilding>() : null;
            if (panelRoot != null) panelRoot.SetActive(true);
            ModalGate.SetBlocked(true);
            Refresh();
            ShowHarvestRadius();
        }

        public void Close()
        {
            _currentInstance = null;
            _currentProduction = null;
            if (panelRoot != null) panelRoot.SetActive(false);
            ModalGate.SetBlocked(false);
            HideHarvestRadius();
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
            _currentInstance?.TryUpgrade();
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

            ArmyManager.Instance.TryRecruit(_recruitType, _currentInstance.transform.position);
            Refresh();
        }

        /// <summary>Picks the tier the Recruit button will raise. A locked tier can be selected on purpose: the label then says what is missing and the price is still readable, which is how a player finds out what the Laboratory would buy them.</summary>
        public void SelectRecruitType(int index)
        {
            if (index < 0 || index >= SoldierStats.All.Length) return;

            _recruitType = SoldierStats.All[index];
            Refresh();
        }

        private void Refresh()
        {
            if (_currentInstance == null || _currentInstance.Data == null) return;
            var data = _currentInstance.Data;

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
                    ? ProductionSummary(_currentProduction.SelectedRecipe, _currentProduction.BatchesPerWorkerPerTick, data.productionIntervalSeconds)
                    : string.Empty;
            }
            RefreshRecipeControls();
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
            if (upgradeControls != null) upgradeControls.SetActive(upgradeCost != null);
            if (upgradeButton != null) upgradeButton.gameObject.SetActive(researched);
            if (upgradeCostRow != null) upgradeCostRow.gameObject.SetActive(researched);
            if (upgradeLockLabel != null)
            {
                upgradeLockLabel.gameObject.SetActive(!researched);
                if (!researched) upgradeLockLabel.text = Localization.Format("#building_upgrade_locked", _currentInstance.Level + 1);
            }
            BuildCostRow(upgradeCostRow, researched ? upgradeCost : null);

            var repairCost = _currentInstance.GetRepairCost();
            if (repairControls != null) repairControls.SetActive(repairCost != null);
            BuildCostRow(repairCostRow, repairCost);

            RefreshRecruitControls(data);
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

            var batches = batchesPerWorker > 1 ? Localization.Format("#recipe_batches", batchesPerWorker) : string.Empty;
            return Localization.Format("#building_produces", recipe.Describe() + batches, intervalSeconds.ToString("0.#"));
        }

        /// <summary>
        /// The metal buttons on a Плавильня. Built at runtime and only for a building that has a
        /// real choice -- a sawmill with one recipe would otherwise get a row holding a single
        /// button that does nothing, which reads as broken rather than as "no choice here".
        /// </summary>
        private void RefreshRecipeControls()
        {
            var hasChoice = _currentProduction != null && _currentProduction.HasRecipeChoice;
            if (recipeControls != null) recipeControls.SetActive(hasChoice);
            if (!hasChoice || recipeRow == null) return;

            // Rebuilt only when a different building is selected: unlike the workforce list this is
            // a handful of buttons that never change while one card is open.
            if (_recipeButtonsOwner != _currentProduction)
            {
                for (var i = recipeRow.childCount - 1; i >= 0; i--)
                {
                    Destroy(recipeRow.GetChild(i).gameObject);
                }
                _recipeButtons.Clear();
                _recipeButtonsOwner = _currentProduction;

                for (var i = 0; i < _currentProduction.Recipes.Count; i++)
                {
                    _recipeButtons.Add(CreateRecipeButton(i, _currentProduction.Recipes[i]));
                }
            }

            for (var i = 0; i < _recipeButtons.Count; i++)
            {
                _recipeButtons[i].color = i == _currentProduction.SelectedRecipeIndex ? SelectedRecipeColor : UnselectedRecipeColor;
            }
        }

        private Image CreateRecipeButton(int index, BuildingRecipe recipe)
        {
            const float width = 210f;
            const float spacing = 12f;
            var count = _currentProduction.Recipes.Count;

            var go = new GameObject($"Recipe_{recipe.id}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(recipeRow, false);

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
            label.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 18;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
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

        private static readonly Color SelectedRecipeColor = new Color(0.36f, 0.5f, 0.3f, 0.95f);
        private static readonly Color UnselectedRecipeColor = new Color(0.26f, 0.29f, 0.24f, 0.95f);

        /// <summary>A tier the Laboratory has not opened yet: dimmer than an available one, and still tappable so its price and its refusal can be read.</summary>
        private static readonly Color LockedTierColor = new Color(0.22f, 0.2f, 0.2f, 0.95f);

        private readonly List<Image> _recipeButtons = new List<Image>();
        private ProductionBuilding _recipeButtonsOwner;

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

            if (recruitControls != null) recruitControls.SetActive(isBarracks && army != null);
            if (!isBarracks || army == null) return;

            RefreshRecruitTypeButtons();
            BuildCostRow(recruitCostRow, SoldierStats.RecruitCost(_recruitType));

            if (recruitLabel == null) return;
            var blocker = army.DescribeRecruitBlocker(_recruitType);
            recruitLabel.text = blocker ?? Localization.Format("#army_summary", army.SoldierCount, SoldierStats.MaxArmySize, army.DailyUpkeep);
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
            const float width = 155f;
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
            label.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 17;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.color = Color.white;
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

            var center = grid.GetFootprintCenterWorld(_currentInstance.OriginCell, _currentInstance.Data.footprintSize);
            Grid.HarvestRadiusOverlay.ShowFor(center, _currentInstance.HarvestRadius);
        }

        private void HideHarvestRadius()
        {
            Grid.HarvestRadiusOverlay.HideIfShown();
        }
    }
}
