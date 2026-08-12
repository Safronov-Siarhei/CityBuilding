using System;
using CityBuilder.Buildings;
using UnityEngine;

namespace CityBuilder.Core
{
    /// <summary>
    /// Watches for the defeat condition decided in the design backlog: the Town Hall destroyed.
    /// (The other stated condition, zero population, needs a death mechanic -- starvation and/or
    /// combat casualties -- that doesn't exist yet; OrcUnit only ever damages buildings, never
    /// citizens, in this first combat slice. Deferred until that exists.) Listens for
    /// BuildingInstance.OnDestroyedInCombat rather than polling every frame.
    /// </summary>
    public class GameOverManager : MonoBehaviour
    {
        public static GameOverManager Instance { get; private set; }

        public bool IsGameOver { get; private set; }

        public event Action OnGameOver;

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
        }

        private void OnDisable()
        {
            BuildingInstance.OnDestroyedInCombat -= HandleBuildingDestroyed;
        }

        private void HandleBuildingDestroyed(BuildingInstance instance)
        {
            if (IsGameOver || instance.Data == null || instance.Data.buildingName != "TownHall") return;

            IsGameOver = true;
            EventLogManager.Instance?.Log("Поражение: Ратуша разрушена.");
            OnGameOver?.Invoke();
        }
    }
}
