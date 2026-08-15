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
            // Freshly placed, so level 1 -- the citizens an upgrade adds on top come from
            // BuildingInstance.TryUpgrade, which knows the level it just reached.
            var granted = data != null ? data.LevelStats(1).citizensGranted : 0;
            if (granted > 0) AddCitizens(granted);
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

        /// <summary>
        /// Moves one idle citizen out of the settlement entirely -- they've been recruited into the
        /// army (see ArmyManager). Total population drops, assigned workers don't: the citizen who
        /// leaves is by definition one who wasn't working, so nobody's job is disturbed and
        /// IdlePopulation stays a truthful number. False when there is nobody idle to take.
        /// </summary>
        public bool TryTakeIdleCitizen()
        {
            if (IdlePopulation <= 0) return false;

            _totalPopulation--;
            OnPopulationChanged?.Invoke();
            return true;
        }

        /// <summary>A recruited citizen coming home (a disbanded soldier). The mirror of TryTakeIdleCitizen -- a soldier killed in battle is never returned this way.</summary>
        public void ReturnCitizen()
        {
            _totalPopulation++;
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
