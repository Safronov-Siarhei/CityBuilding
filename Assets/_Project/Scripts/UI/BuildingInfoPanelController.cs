using CityBuilder.Citizens;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    public class BuildingInfoPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text workersLabel;
        [SerializeField] private Text idleLabel;

        private ProductionBuilding _current;

        public void Show(ProductionBuilding building)
        {
            _current = building;
            if (panelRoot != null) panelRoot.SetActive(true);
            ModalGate.SetBlocked(true);
            Refresh();
        }

        public void Close()
        {
            _current = null;
            if (panelRoot != null) panelRoot.SetActive(false);
            ModalGate.SetBlocked(false);
        }

        public void AssignWorker()
        {
            _current?.TryAssignWorker();
            Refresh();
        }

        public void UnassignWorker()
        {
            _current?.TryUnassignWorker();
            Refresh();
        }

        private void Refresh()
        {
            if (_current == null) return;

            if (titleLabel != null) titleLabel.text = _current.DisplayName;
            if (workersLabel != null) workersLabel.text = $"Рабочие: {_current.AssignedWorkers} / {_current.MaxWorkers}";

            if (idleLabel != null)
            {
                var idle = CitizenManager.Instance != null ? CitizenManager.Instance.IdlePopulation : 0;
                idleLabel.text = $"Свободных жителей: {idle}";
            }
        }
    }
}
