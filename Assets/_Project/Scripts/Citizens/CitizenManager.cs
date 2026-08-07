using System;
using CityBuilder.Buildings;
using UnityEngine;

namespace CityBuilder.Citizens
{
    /// <summary>
    /// Tracks total population as a headcount pool (no individual citizen identity) and how
    /// many are currently assigned to jobs at ProductionBuilding instances. Population grows
    /// only from buildings that grant citizens (Town Hall, House) — see BuildingData.citizensGranted.
    /// </summary>
    public class CitizenManager : MonoBehaviour
    {
        public static CitizenManager Instance { get; private set; }

        [SerializeField] private BuildingPlacer buildingPlacer;

        private int _totalPopulation;
        private int _assignedPopulation;

        public int TotalPopulation => _totalPopulation;
        public int IdlePopulation => Mathf.Max(0, _totalPopulation - _assignedPopulation);

        public event Action OnPopulationChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (buildingPlacer != null)
            {
                buildingPlacer.OnBuildingPlaced += HandleBuildingPlaced;
            }
        }

        private void HandleBuildingPlaced(BuildingData data)
        {
            if (data != null && data.citizensGranted > 0)
            {
                AddCitizens(data.citizensGranted);
            }
        }

        public void AddCitizens(int amount)
        {
            if (amount == 0) return;
            _totalPopulation += amount;
            OnPopulationChanged?.Invoke();
        }

        /// <summary>Used by save/load to set population directly, bypassing the placement-grant path.</summary>
        public void SetPopulation(int amount)
        {
            _totalPopulation = amount;
            OnPopulationChanged?.Invoke();
        }

        /// <summary>Checks idle availability and commits one worker if available.</summary>
        public bool NotifyWorkerAssigned()
        {
            if (IdlePopulation <= 0) return false;
            _assignedPopulation++;
            OnPopulationChanged?.Invoke();
            return true;
        }

        public void NotifyWorkerUnassigned()
        {
            if (_assignedPopulation <= 0) return;
            _assignedPopulation--;
            OnPopulationChanged?.Invoke();
        }

        /// <summary>Used by save/load to restore a known-valid worker count without the idle check.</summary>
        public void NotifyWorkersAssignedBulk(int count)
        {
            if (count <= 0) return;
            _assignedPopulation += count;
            OnPopulationChanged?.Invoke();
        }
    }
}
