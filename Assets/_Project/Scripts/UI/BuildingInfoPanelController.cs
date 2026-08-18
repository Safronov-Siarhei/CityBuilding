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
        /// <summary>The one building that recruits, and the one tier it can recruit today -- armoured tiers need the equipment buildings and the Laboratory unlock that don't exist yet.</summary>
        private const string RecruitBuildingName = "Barracks";
        private const SoldierType RecruitableType = SoldierType.Militia;

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
        [SerializeField] private ResourceIconLibrary iconLibrary;

        private BuildingInstance _currentInstance;
        private ProductionBuilding _currentProduction;
        private GameObject _radiusIndicator;
        private Material _radiusIndicatorMaterial;

        public void Show(BuildingInstance instance)
        {
            _currentInstance = instance;
            _currentProduction = instance != null ? instance.GetComponent<ProductionBuilding>() : null;
            if (panelRoot != null) panelRoot.SetActive(true);
            ModalGate.SetBlocked(true);
            Refresh();
            ShowRadiusIndicator();
        }

        public void Close()
        {
            _currentInstance = null;
            _currentProduction = null;
            if (panelRoot != null) panelRoot.SetActive(false);
            ModalGate.SetBlocked(false);
            HideRadiusIndicator();
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

        /// <summary>Recruits one militiaman at this Barracks -- costs coins and one idle citizen (see ArmyManager). The panel stays open so the player can raise several in a row.</summary>
        public void Recruit()
        {
            if (_currentInstance == null || ArmyManager.Instance == null) return;

            ArmyManager.Instance.TryRecruit(RecruitableType, _currentInstance.transform.position);
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
            if (productionLabel != null)
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

        private readonly List<Image> _recipeButtons = new List<Image>();
        private ProductionBuilding _recipeButtonsOwner;

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

            BuildCostRow(recruitCostRow, SoldierStats.RecruitCost(RecruitableType));

            if (recruitLabel == null) return;
            var blocker = army.DescribeRecruitBlocker(RecruitableType);
            recruitLabel.text = blocker ?? Localization.Format("#army_summary", army.SoldierCount, SoldierStats.MaxArmySize, army.DailyUpkeep);
        }

        /// <summary>The icon+number chips for one cost -- see CostRowBuilder, which the Laboratory's window shares.</summary>
        private void BuildCostRow(Transform row, List<ResourceAmount> cost)
        {
            CostRowBuilder.Build(row, cost, iconLibrary);
        }

        /// <summary>
        /// Ground-flat hollow square around the selected building sized to its fogRevealRadius --
        /// otherwise that stat (how far it permanently clears fog) has no visible representation
        /// anywhere in the game. Square, not a circle, matching the project's straight-edges-only
        /// icon/highlight style (see CitizenSelector's target-cell frame, same 4-bar approach).
        /// </summary>
        private void ShowRadiusIndicator()
        {
            if (_currentInstance == null || _currentInstance.Data == null) return;
            var grid = GridManager.Instance;
            if (grid == null) return;

            var data = _currentInstance.Data;
            var center = grid.GetFootprintCenterWorld(_currentInstance.OriginCell, data.footprintSize);
            var size = data.fogRevealRadius * 2f * grid.CellSize;

            EnsureRadiusIndicator();
            _radiusIndicator.transform.position = new Vector3(center.x, grid.GroundHeight + 0.05f, center.z);
            _radiusIndicator.transform.localScale = new Vector3(size, size, 1f);
            _radiusIndicator.SetActive(true);
        }

        private void HideRadiusIndicator()
        {
            if (_radiusIndicator != null) _radiusIndicator.SetActive(false);
        }

        private void EnsureRadiusIndicator()
        {
            if (_radiusIndicator != null) return;

            _radiusIndicator = new GameObject("BuildingRadiusIndicator");
            _radiusIndicator.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            // Fraction of the square's own (radius-dependent) size, same approach as
            // CitizenSelector's target-cell frame -- absolute border thickness varies a bit
            // between a small and a large fogRevealRadius, which reads fine in practice.
            const float t = 0.02f;
            CreateBar(_radiusIndicator.transform, "Top", new Vector3(0f, 0.5f - t * 0.5f, 0f), new Vector3(1f, t, 1f));
            CreateBar(_radiusIndicator.transform, "Bottom", new Vector3(0f, -0.5f + t * 0.5f, 0f), new Vector3(1f, t, 1f));
            CreateBar(_radiusIndicator.transform, "Left", new Vector3(-0.5f + t * 0.5f, 0f, 0f), new Vector3(t, 1f, 1f));
            CreateBar(_radiusIndicator.transform, "Right", new Vector3(0.5f - t * 0.5f, 0f, 0f), new Vector3(t, 1f, 1f));

            _radiusIndicatorMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = new Color(0.4f, 0.8f, 0.95f) };
            foreach (var bar in _radiusIndicator.GetComponentsInChildren<Renderer>())
            {
                bar.sharedMaterial = _radiusIndicatorMaterial;
            }

            _radiusIndicator.SetActive(false);
        }

        private static void CreateBar(Transform parent, string name, Vector3 localPos, Vector3 localScale)
        {
            var bar = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bar.name = name;
            Destroy(bar.GetComponent<Collider>());
            bar.transform.SetParent(parent, false);
            bar.transform.localPosition = localPos;
            bar.transform.localScale = localScale;
        }
    }
}
