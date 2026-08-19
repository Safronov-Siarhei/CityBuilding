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
        private int _sickPopulation;

        public int TotalPopulation => _totalPopulation;

        /// <summary>
        /// How many of them are ill (see SicknessManager). A count rather than a set of people,
        /// because population here has never been individuals -- it is a headcount pool, and the
        /// citizens walking about the map are visuals drawn from it.
        /// </summary>
        public int SickPopulation => _sickPopulation;

        /// <summary>Everyone well enough to hold a job. This, not TotalPopulation, is what the settlement can staff -- an illness that did not cost the town its work would not be worth having.</summary>
        public int HealthyPopulation => Mathf.Max(0, _totalPopulation - _sickPopulation);

        public int IdlePopulation => Mathf.Max(0, HealthyPopulation - _assignedPopulation);

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
            // Whoever left, the sick can never outnumber the living -- and a sick count above the
            // population would drive HealthyPopulation to zero and lay the whole town off.
            _sickPopulation = Mathf.Min(_sickPopulation, _totalPopulation);
            LayOffWorkersBeyondWorkforce();
            OnPopulationChanged?.Invoke();
            return left;
        }

        /// <summary>
        /// Somebody fell ill. Returns how many actually did, which is fewer than asked for once
        /// everyone left is already in bed.
        ///
        /// Taking to bed lays workers off, which is the whole cost of an epidemic: the buildings
        /// with nobody left in them stop producing, and the player watches their economy stall
        /// before anybody dies.
        /// </summary>
        public int AddSick(int amount)
        {
            if (amount <= 0) return 0;

            var fell = Mathf.Min(amount, _totalPopulation - _sickPopulation);
            if (fell <= 0) return 0;

            _sickPopulation += fell;
            LayOffWorkersBeyondWorkforce();
            OnPopulationChanged?.Invoke();
            return fell;
        }

        /// <summary>Back on their feet. Their old jobs are NOT given back -- the player reassigns them, the same as for any other idle citizen.</summary>
        public int HealSick(int amount)
        {
            if (amount <= 0) return 0;

            var recovered = Mathf.Min(amount, _sickPopulation);
            if (recovered <= 0) return 0;

            _sickPopulation -= recovered;
            OnPopulationChanged?.Invoke();
            return recovered;
        }

        /// <summary>Used by save/load, like SetPopulation beside it. Clamped, because a save written before this existed has no sick at all and a corrupted one must not empty the workforce.</summary>
        public void SetSickPopulation(int amount)
        {
            _sickPopulation = Mathf.Clamp(amount, 0, _totalPopulation);
            LayOffWorkersBeyondWorkforce();
            OnPopulationChanged?.Invoke();
        }

        /// <summary>
        /// Hands back worker slots until the assigned count fits the people well enough to fill
        /// them. Each ProductionBuilding owns its own count, so they have to be asked one at a
        /// time.
        ///
        /// Measured against HealthyPopulation rather than the headcount, so that taking to one's
        /// bed empties a workplace exactly the way dying does.
        /// </summary>
        private void LayOffWorkersBeyondWorkforce()
        {
            var workforce = HealthyPopulation;
            if (_assignedPopulation <= workforce) return;

            foreach (var building in FindObjectsByType<ProductionBuilding>(FindObjectsSortMode.None))
            {
                while (_assignedPopulation > workforce && building.AssignedWorkers > 0)
                {
                    // Decrements _assignedPopulation through NotifyWorkerUnassigned.
                    building.TryUnassignWorker();
                }
                if (_assignedPopulation <= workforce) return;
            }

            // Nothing left to unassign (buildings gone, counts already out of step) -- the invariant
            // matters more than where the discrepancy came from.
            _assignedPopulation = Mathf.Min(_assignedPopulation, workforce);
        }

        /// <summary>Used by save/load to set population directly, bypassing the placement-grant path.</summary>
        public void SetPopulation(int amount)
        {
            _totalPopulation = amount;
            _sickPopulation = Mathf.Min(_sickPopulation, _totalPopulation);
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
