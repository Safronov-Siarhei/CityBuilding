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

        /// <summary>
        /// Citizens lost for good -- starvation today, plague or fire later. Returns how many
        /// actually died, which is fewer than asked for once the town runs out of people.
        ///
        /// Takes the idle ones first and only then lays workers off, because a death that leaves
        /// _assignedPopulation above _totalPopulation would quietly break the whole worker economy:
        /// IdlePopulation clamps at zero, so the settlement would look fully employed forever while
        /// production buildings kept ticking on staff who no longer exist.
        /// </summary>
        public int KillCitizens(int amount)
        {
            if (amount <= 0) return 0;

            var died = Mathf.Min(amount, _totalPopulation);
            if (died <= 0) return 0;

            _totalPopulation -= died;
            LayOffWorkersBeyondPopulation();
            OnPopulationChanged?.Invoke();
            return died;
        }

        /// <summary>Hands back worker slots until the assigned count fits the surviving population. Each ProductionBuilding owns its own count, so they have to be asked one at a time.</summary>
        private void LayOffWorkersBeyondPopulation()
        {
            if (_assignedPopulation <= _totalPopulation) return;

            foreach (var building in FindObjectsByType<ProductionBuilding>(FindObjectsSortMode.None))
            {
                while (_assignedPopulation > _totalPopulation && building.AssignedWorkers > 0)
                {
                    // Decrements _assignedPopulation through NotifyWorkerUnassigned.
                    building.TryUnassignWorker();
                }
                if (_assignedPopulation <= _totalPopulation) return;
            }

            // Nothing left to unassign (buildings gone, counts already out of step) -- the invariant
            // matters more than where the discrepancy came from.
            _assignedPopulation = Mathf.Min(_assignedPopulation, _totalPopulation);
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
