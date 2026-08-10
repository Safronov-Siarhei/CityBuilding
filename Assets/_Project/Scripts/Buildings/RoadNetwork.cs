using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// Tracks which grid cells have a road (BuildingData.isRoad) on them, populated by
    /// BuildingPlacer.TryPlace (new roads) and GameSaveController.ApplyLoadedState (restored
    /// ones). CitizenAgent queries this to detect nearby roads and speed up while walking on
    /// one -- see BuildRoute/CurrentSpeed there.
    /// </summary>
    public class RoadNetwork : MonoBehaviour
    {
        public static RoadNetwork Instance { get; private set; }

        private readonly HashSet<Vector2Int> _roadCells = new HashSet<Vector2Int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void RegisterRoad(Vector2Int cell)
        {
            _roadCells.Add(cell);
        }

        public bool IsRoad(Vector2Int cell)
        {
            return _roadCells.Contains(cell);
        }
    }
}
