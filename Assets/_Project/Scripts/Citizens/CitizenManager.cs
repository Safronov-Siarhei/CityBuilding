using System;
using CityBuilder.Buildings;
using UnityEngine;

namespace CityBuilder.Citizens
{
    /// <summary>
    /// Tracks total population as a headcount pool (no individual citizen identity), how many are
    /// currently assigned to jobs at ProductionBuilding instances, and how many the settlement has
    /// room for at all.
    ///
    /// Housing does not hand out people. A house adds capacity — see BuildingData.housingCapacity —
    /// and the people themselves walk in over time, at a pace set by how content the settlement is
    /// (MigrationManager). The one exception is the founding party that arrives with the Town Hall,
    /// which is authored as BuildingData.citizensOnBuild rather than special-cased here.
    /// </summary>
    public class CitizenManager : MonoBehaviour
    {
        public static CitizenManager Instance { get; private set; }

        [SerializeField] private BuildingPlacer buildingPlacer;

        private int _totalPopulation;
        private int _assignedPopulation;
        private int _capacity;

        public int TotalPopulation => _totalPopulation;
        public int IdlePopulation => Mathf.Max(0, _totalPopulation - _assignedPopulation);

        /// <summary>How many people the settlement has room for, summed over every standing building at the level it stands at.</summary>
        public int Capacity => _capacity;

        /// <summary>
        /// Room nobody is living in yet — what MigrationManager needs before it lets anyone in.
        /// Clamped at zero because population is allowed to exceed capacity: the founding party
        /// outnumbers the Town Hall's own room, and a house burnt down in a raid takes its room
        /// with it without evicting anybody.
        /// </summary>
        public int FreeSpace => Mathf.Max(0, _capacity - _totalPopulation);

        /// <summary>Raised for a change to the headcount OR to the room available — the HUD shows the two as one figure, and nothing cares which half moved.</summary>
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

        private void OnDestroy()
        {
            if (buildingPlacer != null) buildingPlacer.OnBuildingPlaced -= HandleBuildingPlaced;
        }

        private void HandleBuildingPlaced(BuildingData data)
        {
            // The room itself is handed over by BuildingInstance.Initialize, the same way storage
            // room is. What is left here is the founding party that arrives with the Town Hall --
            // every other building adds room and nobody to fill it.
            var arrivals = data != null ? data.citizensOnBuild : 0;
            if (arrivals > 0) AddCitizens(arrivals);
        }

        /// <summary>
        /// Hands the settlement a building's housing, or takes it back.
        ///
        /// Kept the way BuildingInstance already keeps storage room -- added in Initialize,
        /// returned in OnDestroy, and the difference handed over on upgrade and on restore from a
        /// save. A scan over every standing building was the other option, but the moment a house
        /// is destroyed is exactly when a scan is least trustworthy: Unity defers the actual
        /// teardown, so a scan run from the destruction path still counts the house that just
        /// burnt down. Following the existing pattern also leaves one place to look when something
        /// does go out of step, rather than two mechanisms that can disagree.
        /// </summary>
        public void ChangeCapacity(int delta)
        {
            if (delta == 0) return;

            _capacity = Mathf.Max(0, _capacity + delta);
            OnPopulationChanged?.Invoke();
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
        /// </summary>
        public int KillCitizens(int amount) => RemoveCitizens(amount);

        /// <summary>
        /// Citizens who are no longer part of the settlement, for any reason. Returns how many
        /// actually left, which is fewer than asked for once the town runs out of people.
        ///
        /// Deliberately separate from KillCitizens even though the body is the same one: a citizen
        /// who packs up and walks out because the town is miserable is not a casualty, and must not
        /// reach the happiness model's "recent deaths" factor. Only FoodConsumptionManager counts
        /// its own dead into that factor, so routing departures through a differently named door is
        /// what keeps a bad mood from feeding itself — people leave, the deaths score stays clean,
        /// and the per-capita factors (defence, entertainment) actually rise as the town shrinks.
        ///
        /// Takes the idle ones first and only then lays workers off, because a loss that left
        /// _assignedPopulation above _totalPopulation would quietly break the whole worker economy:
        /// IdlePopulation clamps at zero, so the settlement would look fully employed forever while
        /// production buildings kept ticking on staff who no longer exist.
        /// </summary>
        public int RemoveCitizens(int amount)
        {
            if (amount <= 0) return 0;

            var left = Mathf.Min(amount, _totalPopulation);
            if (left <= 0) return 0;

            _totalPopulation -= left;
            LayOffWorkersBeyondPopulation();
            OnPopulationChanged?.Invoke();
            return left;
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
