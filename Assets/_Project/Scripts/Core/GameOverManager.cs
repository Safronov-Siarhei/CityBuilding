using System;
using CityBuilder.Buildings;
using CityBuilder.Combat;
using UnityEngine;

namespace CityBuilder.Core
{
    /// <summary>
    /// Watches for the end of the map, either way round.
    ///
    /// Defeat: the Town Hall destroyed. (The other stated defeat condition, zero population, needs
    /// a starvation mechanic that doesn't exist yet -- orcs still never damage citizens directly.
    /// A recruited citizen killed as a soldier IS a permanent population loss now, so this is
    /// closer than it was, but "population hits zero" still can't happen on its own.)
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
        }

        private void HandleBuildingDestroyed(BuildingInstance instance)
        {
            if (IsGameOver || instance.Data == null || instance.Data.buildingName != "TownHall") return;

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
