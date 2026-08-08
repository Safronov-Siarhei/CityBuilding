using System;
using CityBuilder.Buildings;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Citizens
{
    /// <summary>
    /// Attached to placed buildings that have worker slots (BuildingData.maxWorkers > 0).
    /// Ticks resource production while workers are assigned to it.
    /// </summary>
    public class ProductionBuilding : MonoBehaviour
    {
        private BuildingData _data;
        private float _timer;

        public int AssignedWorkers { get; private set; }

        /// <summary>Fired whenever AssignedWorkers changes, so CitizenVisualsManager can keep visible workers in sync.</summary>
        public event Action OnAssignedWorkersChanged;

        // BuildingInstance.Initialize() runs a line after Instantiate() returns, which is after
        // this component's own Awake() already ran — so BuildingData isn't available yet at
        // Awake time. Resolving it lazily on first access sidesteps that ordering entirely.
        private BuildingData Data => _data != null ? _data : (_data = GetComponent<BuildingInstance>()?.Data);

        public int MaxWorkers => Data != null ? Data.maxWorkers : 0;
        public ResourceType ProducesResource => Data != null ? Data.producesResource : ResourceType.Wood;
        public string DisplayName => Data != null ? Data.displayName : "?";

        private void Start()
        {
            CitizenVisualsManager.Instance?.RegisterProductionBuilding(this);
        }

        private void OnDestroy()
        {
            CitizenVisualsManager.Instance?.UnregisterProductionBuilding(this);
        }

        private void Update()
        {
            var data = Data;
            if (data == null || AssignedWorkers <= 0 || data.productionIntervalSeconds <= 0f) return;

            _timer += Time.deltaTime;
            if (_timer < data.productionIntervalSeconds) return;
            _timer -= data.productionIntervalSeconds;

            ResourceManager.Instance?.Add(data.producesResource, AssignedWorkers * data.productionPerWorkerPerTick);
        }

        public bool TryAssignWorker()
        {
            if (AssignedWorkers >= MaxWorkers) return false;
            if (CitizenManager.Instance == null || !CitizenManager.Instance.NotifyWorkerAssigned()) return false;
            AssignedWorkers++;
            OnAssignedWorkersChanged?.Invoke();
            return true;
        }

        public bool TryUnassignWorker()
        {
            if (AssignedWorkers <= 0) return false;
            AssignedWorkers--;
            CitizenManager.Instance?.NotifyWorkerUnassigned();
            OnAssignedWorkersChanged?.Invoke();
            return true;
        }

        /// <summary>Used by save/load to restore a worker count directly (already-valid state).</summary>
        public void SetAssignedWorkers(int count)
        {
            AssignedWorkers = Mathf.Clamp(count, 0, MaxWorkers);
            CitizenManager.Instance?.NotifyWorkersAssignedBulk(AssignedWorkers);
            OnAssignedWorkersChanged?.Invoke();
        }
    }
}
