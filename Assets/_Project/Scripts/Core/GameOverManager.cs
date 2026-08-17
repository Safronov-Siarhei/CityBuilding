using System;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using UnityEngine;

namespace CityBuilder.Core
{
    /// <summary>
    /// Watches for the end of the map, either way round.
    ///
    /// Defeat, either of the design's two: the Town Hall destroyed, or the settlement emptied of
    /// citizens -- starvation can now do that on its own (see FoodConsumptionManager), which is
    /// what the second condition had been waiting on.
    ///
    /// Victory: every Orc portal on the map destroyed -- one portal today, so the first kill wins
    /// it. Both are event-driven (BuildingInstance.OnDestroyedInCombat / OrcPortal.OnPortalDestroyed)
    /// rather than polled.
    /// </summary>
    public class GameOverManager : MonoBehaviour
    {
        public static GameOverManager Instance { get; private set; }

        /// <summary>True once the map has ended, won or lost -- see IsVictory for which.</summary>
        public bool IsGameOver { get; private set; }

        public bool IsVictory { get; private set; }

        /// <summary>Raised once, with true for a win and false for a loss.</summary>
        public event Action<bool> OnGameEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            BuildingInstance.OnDestroyedInCombat += HandleBuildingDestroyed;
            OrcPortal.OnPortalDestroyed += HandlePortalDestroyed;
        }

        private void OnDisable()
        {
            BuildingInstance.OnDestroyedInCombat -= HandleBuildingDestroyed;
            OrcPortal.OnPortalDestroyed -= HandlePortalDestroyed;
            if (CitizenManager.Instance != null) CitizenManager.Instance.OnPopulationChanged -= HandlePopulationChanged;
        }

        // In Start rather than OnEnable: CitizenManager is a singleton that assigns Instance in its
        // own Awake, and this component's OnEnable can run before that has happened.
        private void Start()
        {
            if (CitizenManager.Instance != null) CitizenManager.Instance.OnPopulationChanged += HandlePopulationChanged;
        }

        /// <summary>
        /// The settlement emptied out. Guarded by having had somebody in the first place: population
        /// is zero for the whole opening stretch of a new game, before the Town Hall is placed, and
        /// losing on the first frame is not the intended reading of "no way to recover".
        /// </summary>
        private void HandlePopulationChanged()
        {
            var population = CitizenManager.Instance != null ? CitizenManager.Instance.TotalPopulation : 0;
            if (population > 0)
            {
                _hadCitizens = true;
                return;
            }

            if (IsGameOver || !_hadCitizens) return;

            EndGame(victory: false, "Поражение: в поселении не осталось жителей.");
        }

        private bool _hadCitizens;

        private void HandleBuildingDestroyed(BuildingInstance instance)
        {
            if (IsGameOver || instance.Data == null || instance.Data.buildingName != BuildingIds.TownHall) return;

            EndGame(victory: false, "Поражение: Ратуша разрушена.");
        }

        /// <summary>
        /// OrcPortal removes itself from its registry before raising this, so All.Count here is
        /// the number still standing. With several portals per map (the design's ~5) this is
        /// already the right check -- the map is won on the last one, not the first.
        /// </summary>
        private void HandlePortalDestroyed(OrcPortal portal)
        {
            if (IsGameOver || OrcPortal.All.Count > 0) return;

            EndGame(victory: true, "Победа: все порталы орков закрыты!");
        }

        private void EndGame(bool victory, string logMessage)
        {
            IsGameOver = true;
            IsVictory = victory;
            EventLogManager.Instance?.Log(logMessage);
            OnGameEnded?.Invoke(victory);
        }
    }
}
