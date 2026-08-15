using CityBuilder.Core;
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
        // Above BuildingInstance.DecayPenaltyThreshold, output falls linearly from 100% down to
        // this floor at full (1.0) decay -- a decrepit building still limps along instead of
        // dropping straight to zero the moment it crosses the threshold.
        // From the balance sheet (min_decay_production_multiplier).

        private BuildingData _data;
        private BuildingInstance _buildingInstance;
        private float _timer;

        public int AssignedWorkers { get; private set; }

        /// <summary>Fired whenever AssignedWorkers changes, so CitizenVisualsManager can keep visible workers in sync.</summary>
        public event Action OnAssignedWorkersChanged;

        // BuildingInstance.Initialize() runs a line after Instantiate() returns, which is after
        // this component's own Awake() already ran — so BuildingData isn't available yet at
        // Awake time. Resolving it lazily on first access sidesteps that ordering entirely.
        private BuildingData Data => _data != null ? _data : (_data = (_buildingInstance = GetComponent<BuildingInstance>())?.Data);

        /// <summary>Worker slots at this building's current upgrade level -- an upgraded workshop can take on more hands the moment it finishes.</summary>
        public int MaxWorkers => Data != null ? Data.LevelStats(CurrentLevel).maxWorkers : 0;

        private int CurrentLevel => _buildingInstance != null ? _buildingInstance.Level : 1;
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

            var baseAmount = AssignedWorkers * data.LevelStats(CurrentLevel).productionPerWorkerPerTick;
            var amount = Mathf.RoundToInt(baseAmount * DecayProductionMultiplier());
            if (amount <= 0) return;

            var stored = ResourceManager.Instance != null ? ResourceManager.Instance.Add(data.producesResource, amount) : amount;
            ReportOverflow(data, amount - stored);
        }

        /// <summary>
        /// Says once that this building's output is going to waste for want of storage, and stays
        /// quiet until it has somewhere to put things again. Without the latch a full granary would
        /// write a line to the event log every few seconds for every farm in the settlement.
        /// </summary>
        private void ReportOverflow(BuildingData data, int wasted)
        {
            if (wasted <= 0)
            {
                _reportedOverflow = false;
                return;
            }

            if (_reportedOverflow) return;
            _reportedOverflow = true;
            EventLogManager.Instance?.Log($"Некуда складывать: {data.displayName} работает впустую, нужно хранилище");
        }

        private bool _reportedOverflow;

        /// <summary>1 below BuildingInstance.DecayPenaltyThreshold, falling linearly to MinDecayProductionMultiplier at full decay -- a neglected building still produces something, just less, right up until it's destroyed outright.</summary>
        private float DecayProductionMultiplier()
        {
            var decay = _buildingInstance != null ? _buildingInstance.Decay : 0f;
            return ComputeDecayProductionMultiplier(decay);
        }

        /// <summary>Pure formula extracted from DecayProductionMultiplier so it's covered by an EditMode test without needing a live BuildingInstance.</summary>
        public static float ComputeDecayProductionMultiplier(float decay)
        {
            if (decay <= BuildingInstance.DecayPenaltyThreshold) return 1f;

            var t = (decay - BuildingInstance.DecayPenaltyThreshold) / (1f - BuildingInstance.DecayPenaltyThreshold);
            return Mathf.Lerp(1f, BalanceConfig.Instance.MinDecayProductionMultiplier, t);
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
