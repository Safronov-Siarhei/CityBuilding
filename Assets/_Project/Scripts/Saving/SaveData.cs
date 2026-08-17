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
        public int currentDay = 1;
        public int taxRatePercent = 10;
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

        /// <summary>
        /// Which recipe the building was set to, by its stable id -- empty for the great majority
        /// that only know one thing. An index would have been shorter and wrong: reordering the
        /// recipes tab would silently turn every saved furnace into a different metal.
        /// </summary>
        public string selectedRecipeId = string.Empty;
        public int level = 1;
        public int currentHealth;
        public float decay;
        public int rotationSteps;
    }
}
