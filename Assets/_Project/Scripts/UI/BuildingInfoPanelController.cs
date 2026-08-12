using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    public class BuildingInfoPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text levelLabel;
        [SerializeField] private Text conditionLabel;
        [SerializeField] private GameObject workerControls;
        [SerializeField] private Text workersLabel;
        [SerializeField] private Text idleLabel;
        [SerializeField] private GameObject upgradeControls;
        [SerializeField] private Transform upgradeCostRow;
        [SerializeField] private GameObject repairControls;
        [SerializeField] private Transform repairCostRow;
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

        private void Refresh()
        {
            if (_currentInstance == null || _currentInstance.Data == null) return;
            var data = _currentInstance.Data;

            if (titleLabel != null) titleLabel.text = data.displayName;
            if (levelLabel != null) levelLabel.text = $"Уровень: {_currentInstance.Level} / {BuildingInstance.MaxLevel}";
            if (conditionLabel != null)
            {
                conditionLabel.text = $"Прочность: {_currentInstance.CurrentHealth}/{data.maxHealth}   Защита: {data.defense}   Ветхость: {Mathf.RoundToInt(_currentInstance.Decay * 100f)}%";
            }

            var hasProduction = _currentProduction != null && _currentProduction.MaxWorkers > 0;
            if (workerControls != null) workerControls.SetActive(hasProduction);
            if (hasProduction)
            {
                if (workersLabel != null) workersLabel.text = $"Рабочие: {_currentProduction.AssignedWorkers} / {_currentProduction.MaxWorkers}";
                if (idleLabel != null)
                {
                    var idle = CitizenManager.Instance != null ? CitizenManager.Instance.IdlePopulation : 0;
                    idleLabel.text = $"Свободных жителей: {idle}";
                }
            }

            var upgradeCost = _currentInstance.GetUpgradeCost();
            if (upgradeControls != null) upgradeControls.SetActive(upgradeCost != null);
            BuildCostRow(upgradeCostRow, upgradeCost);

            var repairCost = _currentInstance.GetRepairCost();
            if (repairControls != null) repairControls.SetActive(repairCost != null);
            BuildCostRow(repairCostRow, repairCost);
        }

        /// <summary>
        /// Rebuilds an icon+number chip per resource in cost (icon from iconLibrary, number as a
        /// small Text next to it) instead of a "40 дерева, 24 камня" sentence -- cost lists vary
        /// per building/action, so this runs at runtime rather than being laid out once in
        /// SetupProject. The row's own HorizontalLayoutGroup (see SetupProject.BuildBuildingInfoPanel)
        /// handles spacing/centering; this only needs to add/remove the leaf icon+text children.
        /// </summary>
        private void BuildCostRow(Transform row, List<ResourceAmount> cost)
        {
            if (row == null) return;

            for (var i = row.childCount - 1; i >= 0; i--)
            {
                Destroy(row.GetChild(i).gameObject);
            }
            if (cost == null) return;

            if (cost.Count == 0)
            {
                CreateChipText(row, "Бесплатно");
                return;
            }

            foreach (var amount in cost)
            {
                CreateChipIcon(row, iconLibrary != null ? iconLibrary.GetIcon(amount.type) : null);
                CreateChipText(row, amount.amount.ToString());
            }
        }

        private static void CreateChipIcon(Transform parent, Sprite sprite)
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(28f, 28f);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
        }

        private static void CreateChipText(Transform parent, string content)
        {
            var go = new GameObject("Amount", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(70f, 32f);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
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
