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

        public static bool Allows(BuildingData data) => data != null && Array.IndexOf(Buildings, data.buildingName) >= 0;

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
