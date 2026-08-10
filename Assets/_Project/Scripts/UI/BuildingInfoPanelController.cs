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
        [SerializeField] private Text upgradeCostLabel;

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
            if (upgradeCost != null && upgradeCostLabel != null)
            {
                upgradeCostLabel.text = upgradeCost.Count == 0 ? "Улучшить (бесплатно)" : "Улучшить: " + FormatCost(upgradeCost);
            }
        }

        private static string FormatCost(System.Collections.Generic.List<ResourceAmount> cost)
        {
            var parts = new string[cost.Count];
            for (var i = 0; i < cost.Count; i++)
            {
                parts[i] = $"{cost[i].amount} {ResourceLabel(cost[i].type)}";
            }
            return string.Join(", ", parts);
        }

        private static string ResourceLabel(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Wood: return "дерева";
                case ResourceType.Stone: return "камня";
                case ResourceType.Food: return "еды";
                case ResourceType.Gold: return "золота";
                default: return type.ToString();
            }
        }
    }
}
