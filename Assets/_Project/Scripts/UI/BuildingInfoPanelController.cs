using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Core;
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

        public void Show(BuildingInstance instance)
        {
            _currentInstance = instance;
            _currentProduction = instance != null ? instance.GetComponent<ProductionBuilding>() : null;
            if (panelRoot != null) panelRoot.SetActive(true);
            ModalGate.SetBlocked(true);
            Refresh();
        }

        public void Close()
        {
            _currentInstance = null;
            _currentProduction = null;
            if (panelRoot != null) panelRoot.SetActive(false);
            ModalGate.SetBlocked(false);
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
    }
}
