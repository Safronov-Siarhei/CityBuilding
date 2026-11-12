using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using UnityEngine;

namespace CityBuilder.Citizens
{
    /// <summary>
    /// The food rules (the user, 2026-11-02 -- simpler, for the phone): one Food, and buildings that
    /// each make so much of it a day with their one worker, whatever they are --
    /// - the Fisher's Hut (at the shore): food_day_fisherhut_N, doubled by a fish shoal in its reach;
    /// - the Farm: food_day_farm_N, + food_fertile_bonus on fertile land;
    /// - the Orchard and the Pig Farm: food_day_orchard_N / food_day_pigfarm_N (dearer, researched);
    /// - the Bakery makes nothing itself: every working Farm in its service_radius gets
    ///   food_boost_baker_N more; the Smokehouse the same for Fisher's Huts and Pig Farms. Two of them
    ///   in reach give both bonuses (an L1 and an L2 Bakery: +3).
    /// The day's food arrives one by one, evenly over the day (ProductionBuilding.TickFood).
    /// </summary>
    public static class FoodOutput
    {
        public static bool IsProducer(string id) =>
            id == BuildingIds.FisherHut || id == BuildingIds.Farm || id == BuildingIds.Orchard || id == BuildingIds.PigFarm;

        public static bool IsBooster(string id) => id == BuildingIds.Baker || id == BuildingIds.Smokehouse;

        public static bool IsFood(string id) => IsProducer(id) || IsBooster(id);

        /// <summary>Whether this booster's bonus goes to this producer.</summary>
        public static bool Boosts(string booster, string producer) =>
            booster == BuildingIds.Baker ? producer == BuildingIds.Farm
            : booster == BuildingIds.Smokehouse && (producer == BuildingIds.FisherHut || producer == BuildingIds.PigFarm);

        /// <summary>Whether a food building is at work: built (not under construction) and staffed.</summary>
        public static bool Working(ProductionBuilding building) =>
            building != null && building.AssignedWorkers > 0 && building.Instance != null && building.Instance.IsOperational;

        /// <summary>What a booster gives each producer in its reach at its level.</summary>
        public static int BoostGiven(ProductionBuilding booster)
        {
            var config = BalanceConfig.Instance;
            return config == null || booster == null ? 0 : config.FoodBoost(booster.BuildingId, booster.Level);
        }

        /// <summary>The bonus this producer gets from every working Bakery/Smokehouse that reaches it.</summary>
        public static int BoostFor(ProductionBuilding producer)
        {
            var grid = GridManager.Instance;
            var cell = grid != null ? grid.CellSize : 1f;
            var here = producer.transform.position;
            var bonus = 0;
            foreach (var booster in ProductionBuilding.All)
            {
                if (booster == null || !IsBooster(booster.BuildingId) || !Boosts(booster.BuildingId, producer.BuildingId) || !Working(booster)) continue;
                var reach = booster.Instance.ServiceRadius * cell;
                var d = booster.transform.position - here;
                d.y = 0f;
                if (d.sqrMagnitude <= reach * reach) bonus += BoostGiven(booster);
            }
            return bonus;
        }

        /// <summary>
        /// The bonus a producer of this kind standing at `here` would get from the working boosters
        /// reaching it -- shown over the building being placed (the user, 2026-11-10).
        /// </summary>
        public static int BoostAt(string producerId, Vector3 here)
        {
            if (!IsProducer(producerId)) return 0;
            var grid = GridManager.Instance;
            var cell = grid != null ? grid.CellSize : 1f;
            var bonus = 0;
            foreach (var booster in ProductionBuilding.All)
            {
                if (booster == null || !IsBooster(booster.BuildingId) || !Boosts(booster.BuildingId, producerId) || !Working(booster)) continue;
                var reach = booster.Instance.ServiceRadius * cell;
                var d = booster.transform.position - here;
                d.y = 0f;
                if (d.sqrMagnitude <= reach * reach) bonus += BoostGiven(booster);
            }
            return bonus;
        }

        /// <summary>How many producers a booster of this kind, at `radiusCells` reach, would boost from `here`.</summary>
        public static int ReachedFrom(string boosterId, Vector3 here, int radiusCells)
        {
            if (!IsBooster(boosterId) || radiusCells <= 0) return 0;
            var grid = GridManager.Instance;
            var reach = radiusCells * (grid != null ? grid.CellSize : 1f);
            var count = 0;
            foreach (var producer in ProductionBuilding.All)
            {
                if (producer == null || !Boosts(boosterId, producer.BuildingId) || producer.Instance == null) continue;
                var d = producer.transform.position - here;
                d.y = 0f;
                if (d.sqrMagnitude <= reach * reach) count++;
            }
            return count;
        }

        /// <summary>Food a day this producer makes now, every bonus counted; 0 for anything else.</summary>
        public static int PerDay(ProductionBuilding producer)
        {
            var config = BalanceConfig.Instance;
            if (config == null || producer == null || !IsProducer(producer.BuildingId)) return 0;
            var amount = config.FoodPerDay(producer.BuildingId, producer.Level) + BoostFor(producer);
            var map = MeshMapApplier.Instance;
            // a Farm with most of its plot on fertile soil
            if (producer.BuildingId == BuildingIds.Farm && map != null && producer.Instance != null
                && ProductionBuilding.FertileShare(producer.Instance, map) >= 0.5f) amount += config.FoodFertileBonus;
            // a Fisher's Hut with a shoal in its reach: the whole catch doubled
            if (producer.BuildingId == BuildingIds.FisherHut) amount = Mathf.RoundToInt(amount * producer.SiteMultiplier());
            // Изобилие: +N to every food building's day (the user, 2026-11-12)
            return amount + Shop.Boosters.FoodBonus;
        }
    }
}
