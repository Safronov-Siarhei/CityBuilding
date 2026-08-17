using System;
using System.Collections.Generic;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// Groups buildings in the hotbar (see BuildingCategoryPanel) -- purely a UI grouping, not a
    /// gameplay mechanic. These seven are the design's own categories, spelled the way the taxonomy
    /// diagrams spell them: Город, Склады, Развлечения, Оборонительные, Производственные,
    /// Производство еды, Водные.
    /// </summary>
    public enum BuildingCategory { City, Storage, Entertainment, Defence, Production, Food, Water }

    /// <summary>
    /// What a building is worth at one upgrade level. Every stat that a level is supposed to
    /// improve lives here rather than on BuildingData, so no caller can read a building's strength
    /// without saying which level it means -- upgrading used to advance a number and change nothing
    /// else, and a flat "maxHealth" next to a Level property was exactly how that stayed unnoticed.
    ///
    /// Authored per level in the balance sheet's buildings tab: max_health / max_health_2 /
    /// max_health_3 and so on, where a missing or empty higher-level column simply repeats the
    /// level below it.
    /// </summary>
    [Serializable]
    public class BuildingLevelStats
    {
        public int maxHealth = 100;
        public int defense;
        public int citizensGranted;
        public int maxWorkers;

        /// <summary>
        /// How many batches of the selected recipe one worker gets through per tick. What a batch
        /// consists of is the recipe's business (see BuildingRecipe) -- this is the only half an
        /// upgrade improves, so a better workshop makes more of whatever it is set to rather than
        /// changing what it makes.
        /// </summary>
        public int batchesPerWorkerPerTick;

        /// <summary>How much room this building adds to its storage group (see BuildingData.storageGroup). Zero for anything that isn't a storehouse.</summary>
        public int storageCapacity;
    }

    [CreateAssetMenu(fileName = "NewBuilding", menuName = "CityBuilder/Building Data")]
    public class BuildingData : ScriptableObject
    {
        [Header("Identity")]
        public string buildingName = "Building"; // stable id — used for save files and catalog lookup
        public string displayName = "Building"; // the sheet's own Russian label; the fallback for LocalizedName
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

        [Header("Per-level stats")]
        // Three entries, one per upgrade level. Filled from the balance sheet by SetupProject --
        // read it through LevelStats(level), never by index, so a building whose sheet row is
        // missing levels still answers with something sane.
        public List<BuildingLevelStats> levels = new List<BuildingLevelStats>();

        [Header("Production")]
        // Everything this building knows how to make, from the balance sheet's recipes tab. Empty
        // for anything that produces nothing at all -- a house, a wall, the Пристань. One entry is
        // the ordinary case; several means the player chooses (see ProductionBuilding).
        public List<BuildingRecipe> recipes = new List<BuildingRecipe>();

        public float productionIntervalSeconds = 6f;

        [Header("Storage")]
        // Which family of resources this building stores. None for everything that isn't a
        // storehouse -- the amount it holds is per level, in BuildingLevelStats.storageCapacity.
        public ResourceStorageGroup storageGroup = ResourceStorageGroup.None;

        [Header("Upgrades")]
        // Level 1 is the building as placed (free). These are what BuildingInstance.TryUpgrade
        // spends to reach level 2 / level 3. What the levels themselves are worth lives in
        // `levels` above, and which model each one shows in BuildingLevelAppearance.
        public List<ResourceAmount> upgradeToLevel2Cost = new List<ResourceAmount>();
        public List<ResourceAmount> upgradeToLevel3Cost = new List<ResourceAmount>();

        [Header("Fog of War")]
        // Cells around this building permanently cleared of fog once placed -- see
        // FogOfWarManager.RevealPermanent, called from BuildingInstance.Initialize.
        public int fogRevealRadius = 8;

        [Header("Requirements")]
        // Null = no prerequisite. Otherwise at least one already-placed instance of this building
        // (by buildingName -- see BuildingInstance.HasAny) must exist before this one can be
        // placed. Checked by BuildingPlacer alongside affordability, not by GridManager.
        public BuildingData requiredBuilding;

        /// <summary>What to show the player: the localization sheet's `building.<id>`, falling back to the buildings tab's own display_name.</summary>
        public string LocalizedName => Core.Localization.GetOrDefault("building." + buildingName, displayName);

        /// <summary>
        /// This building's stats at the given level (1..3), clamped into whatever the sheet
        /// actually provided. A building with no levels at all answers with defaults rather than
        /// null: a missing sheet row is already reported loudly at build time (see
        /// SetupProject.Balance), and returning null here would turn that one error into a crash
        /// somewhere far away.
        /// </summary>
        public BuildingLevelStats LevelStats(int level)
        {
            if (levels == null || levels.Count == 0) return Fallback;

            var index = Mathf.Clamp(level, 1, levels.Count) - 1;
            return levels[index] ?? Fallback;
        }

        private static readonly BuildingLevelStats Fallback = new BuildingLevelStats();
    }
}
