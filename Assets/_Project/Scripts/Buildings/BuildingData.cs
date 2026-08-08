using System.Collections.Generic;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Buildings
{
    [CreateAssetMenu(fileName = "NewBuilding", menuName = "CityBuilder/Building Data")]
    public class BuildingData : ScriptableObject
    {
        [Header("Identity")]
        public string buildingName = "Building"; // stable id — used for save files and catalog lookup
        public string displayName = "Building"; // shown in UI
        public GameObject prefab;
        public Vector2Int footprintSize = Vector2Int.one;
        public List<ResourceAmount> cost = new List<ResourceAmount>();

        [Header("Placement")]
        // Allows placement on cells inside a mesh map's water-placement zone (MeshMapApplier)
        // even though those cells are normally water-blocked -- e.g. a bridge or water wheel.
        // No such building exists yet; this is the (unverified) infrastructure for one.
        public bool isWaterCategory = false;

        [Header("Population")]
        public int citizensGranted = 0;

        [Header("Production")]
        public int maxWorkers = 0;
        public ResourceType producesResource = ResourceType.Wood;
        public int productionPerWorkerPerTick = 0;
        public float productionIntervalSeconds = 6f;
    }
}
