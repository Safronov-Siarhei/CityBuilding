using System.Collections.Generic;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>Groups buildings in the hotbar (see BuildingCategoryPanel) -- purely a UI grouping, not a gameplay mechanic.</summary>
    public enum BuildingCategory { Housing, Food, Production, Infrastructure, Military }

    [CreateAssetMenu(fileName = "NewBuilding", menuName = "CityBuilder/Building Data")]
    public class BuildingData : ScriptableObject
    {
        [Header("Identity")]
        public string buildingName = "Building"; // stable id — used for save files and catalog lookup
        public string displayName = "Building"; // shown in UI
        public GameObject prefab;
        public Vector2Int footprintSize = Vector2Int.one;
        public List<ResourceAmount> cost = new List<ResourceAmount>();
        public BuildingCategory category = BuildingCategory.Production;

        [Header("Placement")]
        // Allows placement on cells inside a mesh map's water-placement zone (MeshMapApplier)
        // even though those cells are normally water-blocked -- e.g. a bridge or water wheel.
        // No such building exists yet; this is the (unverified) infrastructure for one.
        public bool isWaterCategory = false;

        // Registers the placed cell(s) with RoadNetwork so CitizenAgent can detect and speed up
        // on them -- see BuildingPlacer.TryPlace/GameSaveController.ApplyLoadedState.
        public bool isRoad = false;

        // Contributes its footprint to the NavMesh as walkable ground (see
        // MeshMapApplier.RegisterWalkableSurface). Only meaningful for something spanning terrain
        // citizens otherwise can't cross -- a Bridge over water. The NavMesh is baked once from
        // the ground mesh, and ordinary buildings only ever carve holes OUT of it, so without this
        // a bridge is decoration: the water underneath stays unwalkable and nobody crosses it.
        public bool providesWalkableSurface = false;

        // Part of the fence line: registers this building's cells with FenceNetwork, so fence
        // segments next to it shape themselves as if the line continued through it. A Fence sets
        // this AND carries a FenceAppearance (it changes its own model); a Gate or Tower would set
        // only this, joining the line while keeping its own look.
        public bool connectsToFences = false;

        // Keeps this building selected (ghost stays active) after a successful placement instead
        // of clearing the selection -- lets the player lay several tiles in a row (e.g. a road)
        // without re-opening the hotbar each time.
        public bool keepSelectedAfterPlacement = false;

        [Header("Population")]
        public int citizensGranted = 0;

        [Header("Production")]
        public int maxWorkers = 0;
        public ResourceType producesResource = ResourceType.Wood;
        public int productionPerWorkerPerTick = 0;
        public float productionIntervalSeconds = 6f;

        [Header("Upgrades")]
        // Level 1 is the building as placed (free). These are what BuildingInstance.TryUpgrade
        // spends to reach level 2 / level 3. Changing the model or stats per level is planned
        // for later -- for now upgrading only advances BuildingInstance.Level.
        public List<ResourceAmount> upgradeToLevel2Cost = new List<ResourceAmount>();
        public List<ResourceAmount> upgradeToLevel3Cost = new List<ResourceAmount>();

        [Header("Condition")]
        // Base (level 1) stats. Intended to scale with upgrade level in the future -- see
        // BuildingInstance.CurrentHealth/Decay for the per-instance runtime values these seed.
        public int maxHealth = 100;
        public int defense = 0;

        [Header("Fog of War")]
        // Cells around this building permanently cleared of fog once placed -- see
        // FogOfWarManager.RevealPermanent, called from BuildingInstance.Initialize.
        public int fogRevealRadius = 8;

        [Header("Requirements")]
        // Null = no prerequisite. Otherwise at least one already-placed instance of this building
        // (by buildingName -- see BuildingInstance.HasAny) must exist before this one can be
        // placed. Checked by BuildingPlacer alongside affordability, not by GridManager.
        public BuildingData requiredBuilding;
    }
}
