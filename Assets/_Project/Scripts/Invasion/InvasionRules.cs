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
    /// nine buildings and no houses (the six on the island are all there will ever be); building and
    /// repairing cost wood and stone only; an upgrade costs wood, stone and gold -- whatever else the
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
        /// How long building or upgrading takes in a run (the user, 2026-10-22): its price in
        /// resources × inv_build_sec_per_unit, held between inv_build_sec_min and _max -- the
        /// sheet's costs decide it, a fence is quick and a fortified tower is not.
        /// </summary>
        public static float BuildSeconds(List<ResourceAmount> cost)
        {
            var units = 0;
            if (cost != null) foreach (var c in cost) units += Mathf.Max(0, c.amount);
            return BuildSecondsFor(units, BalanceConfig.Instance.InvasionBuildSecondsPerUnit, BalanceConfig.Instance.InvasionBuildSecondsMin, BalanceConfig.Instance.InvasionBuildSecondsMax);
        }

        public static float BuildSecondsFor(int units, float perUnit, float min, float max) => Mathf.Clamp(units * perUnit, min, max);

        /// <summary>A run's walls and gates (the user, 2026-10-22): obstacles for the orcs, sturdier than the sandbox's (inv_wall_health_mult); an orc breaks one only when it stands in its way.</summary>
        private static readonly string[] Barriers = { "Fence", "Gate", "FortifiedGate" };

        public static bool IsBarrier(BuildingData data) => data != null && Array.IndexOf(Barriers, data.buildingName) >= 0;

        /// <summary>The gatherers' yards a run may have of each (the user, 2026-10-22: one sawmill, one quarry).</summary>
        private static readonly string[] Limited = { "Sawmill", "Quarry" };

        /// <summary>A run already has as many of this as it may: its card comes off the menu and it cannot be placed.</summary>
        public static bool AtLimit(BuildingData data)
        {
            var max = LimitOf(data);
            return max > 0 && BuildingInstance.CountOf(data.buildingName) >= max;
        }

        /// <summary>How many of this a run may have, or 0 for no limit: sawmills inv_max_each_yard, quarries inv_max_quarries (two, the user 2026-10-22).</summary>
        public static int LimitOf(BuildingData data)
        {
            if (data == null || Array.IndexOf(Limited, data.buildingName) < 0) return 0;
            return data.buildingName == "Quarry" ? BalanceConfig.Instance.InvasionMaxQuarries : BalanceConfig.Instance.InvasionMaxEachYard;
        }

        /// <summary>Whether a build-menu category has anything in a run.</summary>
        public static bool HasAny(BuildingCategory category, IEnumerable<BuildingData> buildings)
        {
            foreach (var data in buildings)
            {
                if (data != null && data.category == category && Allows(data)) return true;
            }
            return false;
        }

        /// <summary>A building's price in a run: its wood and its stone, nothing else.</summary>
        public static List<ResourceAmount> BuildCost(BuildingData data)
        {
            var cost = new List<ResourceAmount>(2);
            if (data == null) return cost;
            foreach (var amount in data.cost)
            {
                if ((amount.type == ResourceType.Wood || amount.type == ResourceType.Stone) && amount.amount > 0)
                {
                    cost.Add(new ResourceAmount { type = amount.type, amount = amount.amount });
                }
            }
            return cost;
        }

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

        /// <summary>Mending a damaged building in a run: the sheet's repair share of its wood and stone.</summary>
        public static List<ResourceAmount> RepairCost(BuildingData data)
        {
            var cost = new List<ResourceAmount>(2);
            foreach (var amount in BuildCost(data))
            {
                cost.Add(new ResourceAmount { type = amount.type, amount = Mathf.Max(1, Mathf.RoundToInt(amount.amount * BalanceConfig.Instance.RepairCostFraction)) });
            }
            return cost;
        }
    }
}
