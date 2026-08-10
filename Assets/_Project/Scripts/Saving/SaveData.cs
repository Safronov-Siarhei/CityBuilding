using System;
using System.Collections.Generic;
using CityBuilder.Resources;

namespace CityBuilder.Saving
{
    [Serializable]
    public class GameSaveData
    {
        public int version = 1;
        public string mapId = string.Empty;
        public bool mandatoryBuildingPlaced;
        public int population;
        public List<ResourceEntry> resources = new List<ResourceEntry>();
        public List<BuildingEntry> buildings = new List<BuildingEntry>();
    }

    [Serializable]
    public class ResourceEntry
    {
        public ResourceType type;
        public int amount;
    }

    [Serializable]
    public class BuildingEntry
    {
        public string buildingName;
        public int cellX;
        public int cellY;
        public int assignedWorkers;
        public int level = 1;
        public int currentHealth;
        public float decay;
        public int rotationSteps;
    }
}
