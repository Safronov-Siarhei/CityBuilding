using System;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Invasion
{
    /// <summary>
    /// What an Invasion run changes about building (the user's answers, 2026-10-21): the menu holds
    /// nine buildings and no houses (the six on the island are all there will ever be); building
    /// costs wood, stone and gold (the invasion tab's cost_gold, the user 2026-10-24), repairing wood
    /// and stone only; an upgrade costs wood, stone and gold -- whatever else the
    /// sandbox's upgrade asks for (iron) is paid in gold instead; nothing waits on the Laboratory or
    /// on another building; and nothing decays.
    ///
    /// Read by BuildingPlacer, BuildingInstance and the build menu through the one question
    /// "is this a run", so the sandbox's own rules stay exactly as they were outside one.
    /// </summary>
    public static class InvasionRules
    {
        /// <summary>The run's build menu, by building id.</summary>
        public static readonly string[] Buildings =
        {
            "Tower", "FortifiedTower", "Fence", "Gate", "FortifiedGate",
            "Sawmill", "Quarry",
            "Barracks",
            "HealerHouse",
            "Road",   // the user, 2026-10-24: at the sandbox's price (the invasion tab has no row for it)
        };

        /// <summary>The house standing on the island from the start; the run's people live in these.</summary>
        public const string House = "Hovel";

        /// <summary>The money of a run: what soldiers are hired with, kills and waves pay, and upgrades cost.</summary>
        public const ResourceType Money = ResourceType.Coins;

        public static bool Active => InvasionMode.IsActive;

        /// <summary>The resources a run has (people are counted apart): what its resource bar shows.</summary>
        public static bool IsRunResource(ResourceType type) => type == ResourceType.Wood || type == ResourceType.Stone || type == Money;

        public static bool Allows(BuildingData data) => data != null && Array.IndexOf(Buildings, data.buildingName) >= 0;

        /// <summary>
        /// How long putting `level` of this building up takes in a run: level 1 is building it, a
        /// higher one the upgrade into it -- the invasion tab's build_sec / build_sec_N, set by hand
        /// (the user, 2026-10-24; it used to follow the price). 0 is at once.
        /// </summary>
        public static float BuildSeconds(BuildingData data, int level) => data == null ? 0f : data.BuildSeconds(level);

        /// <summary>A run's walls and gates (the user, 2026-10-22): obstacles for the orcs, sturdier than the sandbox's (their health in the invasion tab); an orc breaks one only when it stands in its way.</summary>
        private static readonly string[] Barriers = { "Fence", "Gate", "FortifiedGate" };

        public static bool IsBarrier(BuildingData data) => data != null && Array.IndexOf(Barriers, data.buildingName) >= 0;

        /// <summary>A run already has as many of this as it may: its card comes off the menu and it cannot be placed.</summary>
        public static bool AtLimit(BuildingData data)
        {
            var max = LimitOf(data);
            return max > 0 && BuildingInstance.CountOf(data.buildingName) >= max;
        }

        /// <summary>How many of this a run may have, or 0 for no limit: the invasion tab's max_count (the user, 2026-10-23: two sawmills, two quarries).</summary>
        public static int LimitOf(BuildingData data) => data != null ? data.maxCount : 0;

        /// <summary>Whether a build-menu category has anything in a run.</summary>
        public static bool HasAny(BuildingCategory category, IEnumerable<BuildingData> buildings)
        {
            foreach (var data in buildings)
            {
                if (data != null && data.category == category && Allows(data)) return true;
            }
            return false;
        }

        /// <summary>
        /// A building's price in a run: its wood, its stone, and the rest in the run's money -- the
        /// invasion tab's cost_gold reads as ResourceType.Gold and is paid in coins, as an upgrade's is.
        /// </summary>
        public static List<ResourceAmount> BuildCost(BuildingData data) =>
            data == null ? new List<ResourceAmount>() : UpgradeCost(data.cost);

        /// <summary>An upgrade's price in a run: its wood and stone, and everything else it asks for in gold.</summary>
        public static List<ResourceAmount> UpgradeCost(List<ResourceAmount> sandbox)
        {
            if (sandbox == null) return null;
            var cost = new List<ResourceAmount>(3);
            var money = 0;
            foreach (var amount in sandbox)
            {
                if (amount.amount <= 0) continue;
                if (amount.type == ResourceType.Wood || amount.type == ResourceType.Stone) cost.Add(new ResourceAmount { type = amount.type, amount = amount.amount });
                else money += amount.amount;
            }
            if (money > 0) cost.Add(new ResourceAmount { type = Money, amount = money });
            return cost;
        }

        /// <summary>
        /// Mending a damaged building in a run, by how badly it is hurt (the user, 2026-10-25): see
        /// BuildingInstance.RepairCostOf -- a run's price is wood, stone and gold, and mending leaves
        /// the gold out, so it is wood and stone.
        /// </summary>
        public static List<ResourceAmount> RepairCost(BuildingData data, int level, float missing) =>
            BuildingInstance.RepairCostOf(data, level, missing);
    }
}
