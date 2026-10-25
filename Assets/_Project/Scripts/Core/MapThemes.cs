namespace CityBuilder.Core
{
    /// <summary>What a MapTheme means: its season, and the id its button's picture and the menu's art go by.</summary>
    public static class MapThemes
    {
        /// <summary>The themes the menus offer, in the order their buttons stand (the user, 2026-10-25: five for now).</summary>
        public static readonly MapTheme[] All = { MapTheme.Spring, MapTheme.Summer, MapTheme.Autumn, MapTheme.Winter, MapTheme.NewYear };

        /// <summary>The season a theme dresses the map in; the New Year's is winter.</summary>
        public static MapSeason SeasonOf(MapTheme theme)
        {
            switch (theme)
            {
                case MapTheme.Spring: return MapSeason.Spring;
                case MapTheme.Autumn: return MapSeason.Autumn;
                case MapTheme.Winter:
                case MapTheme.NewYear: return MapSeason.Winter;
                default: return MapSeason.Summer;
            }
        }

        /// <summary>Whether the sandbox's maps come in this theme: every season, not the New Year (an Invasion map).</summary>
        public static bool InSandbox(MapTheme theme) => theme != MapTheme.NewYear;

        /// <summary>The theme button's glyph and the invasion picture's name: Theme_spring, …</summary>
        public static string Id(MapTheme theme) => theme == MapTheme.NewYear ? "newyear" : theme.ToString().ToLowerInvariant();
    }
}
