using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Grid;
using UnityEngine;

namespace CityBuilder.Citizens
{
    /// <summary>
    /// What decor is for (the user, 2026-11-12): every decoration -- a tree, a bush, a garden, a flag,
    /// the town square, the tavern, the church, the theatre, the colosseum -- adds coins a day to each
    /// person living in a house inside its reach. A flag or a tree gives little over a short reach, a
    /// theatre a lot over a wide one; the numbers are the buildings tab's resident_coins(_N) and
    /// service_radius(_N) (cells). Several decorations reaching one house ADD UP (the user's choice), up
    /// to decor_coins_cap_per_resident. The money comes with the day's tax (TaxManager), and the same
    /// bonus makes gift bubbles appear over the house (DecorGifts).
    /// Only finished buildings count -- a decoration being built or upgraded gives nothing that day.
    /// </summary>
    public static class DecorBonus
    {
        /// <summary>Whether this building gives decor money at any level.</summary>
        public static bool IsDecor(BuildingData data)
        {
            if (data == null) return false;
            for (var l = 1; l <= BuildingInstance.MaxLevel; l++)
            {
                if (data.LevelStats(l).residentCoins > 0f) return true;
            }
            return false;
        }

        /// <summary>The coins a day this decoration gives each resident in its reach, at the level it stands at (0 while building).</summary>
        public static float GivenBy(BuildingInstance decor) =>
            decor == null || decor.Data == null || !decor.IsOperational || decor.IsBot ? 0f : decor.Data.LevelStats(decor.Level).residentCoins;

        /// <summary>Coins a day one resident of a house standing at `here` gets from all the decor reaching it (capped).</summary>
        public static float PerResidentAt(Vector3 here)
        {
            var grid = GridManager.Instance;
            var cell = grid != null ? grid.CellSize : 1f;
            var bonus = 0f;
            var all = BuildingInstance.All;
            for (var i = 0; i < all.Count; i++)
            {
                var decor = all[i];
                var give = GivenBy(decor);
                if (give <= 0f) continue;
                var reach = decor.ServiceRadius * cell;
                var d = decor.transform.position - here;
                d.y = 0f;
                if (d.sqrMagnitude <= reach * reach) bonus += give;
            }
            var config = BalanceConfig.Instance;
            return config != null ? Mathf.Min(bonus, config.DecorCoinsCapPerResident) : bonus;
        }

        /// <summary>Coins a day one resident of this house gets from the decor round it.</summary>
        public static float PerResident(BuildingInstance house) =>
            house == null ? 0f : PerResidentAt(house.transform.position);

        /// <summary>The decor's coins for the whole town a day: every house's residents x its bonus.</summary>
        public static float DailyCoins()
        {
            var total = 0f;
            var houses = BuildingInstance.Houses;
            for (var i = 0; i < houses.Count; i++)
            {
                var house = houses[i];
                if (house == null || house.IsBot) continue;
                var people = HousePeople.PlacesOf(house);
                if (people > 0) total += people * PerResident(house);
            }
            return total;
        }

        /// <summary>A bonus as the player reads it: up to two decimals, no trailing zeros, the locale's comma («0,08»).</summary>
        public static string Coins(float value) =>
            value.ToString("0.##", System.Globalization.CultureInfo.CurrentCulture);

        /// <summary>How many houses a decoration with `radiusCells` reach would cover from `here` -- for the placing hint.</summary>
        public static int HousesReachedFrom(Vector3 here, int radiusCells)
        {
            if (radiusCells <= 0) return 0;
            var grid = GridManager.Instance;
            var reach = radiusCells * (grid != null ? grid.CellSize : 1f);
            var count = 0;
            var houses = BuildingInstance.Houses;
            for (var i = 0; i < houses.Count; i++)
            {
                var house = houses[i];
                if (house == null || house.IsBot) continue;
                var d = house.transform.position - here;
                d.y = 0f;
                if (d.sqrMagnitude <= reach * reach) count++;
            }
            return count;
        }
    }
}
