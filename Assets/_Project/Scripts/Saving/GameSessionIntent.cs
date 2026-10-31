namespace CityBuilder.Saving
{
    /// <summary>
    /// Carries the player's menu choice (new game vs. which save to continue) across the scene
    /// load from MainMenu to CityBuilder. A plain static field survives scene transitions
    /// within the same app run without needing a persistent singleton GameObject.
    /// </summary>
    public static class GameSessionIntent
    {
        /// <summary>Null/empty means start a fresh game; otherwise the save slot name to load.</summary>
        public static string SaveNameToLoad;

        /// <summary>Map picked by MapSelector for a fresh game; ignored when loading a save (which carries its own map).</summary>
        public static string NewGameMapId;

        /// <summary>The new game is an Invasion run (the menu's «В бой»), not a sandbox game; consumed by InvasionMode.</summary>
        public static bool NewGameIsInvasion;

        /// <summary>The season the fresh game is dressed in (the menu's theme); null is summer. Ignored when loading a save.</summary>
        public static Core.MapSeason? NewGameSeason;

        /// <summary>The game is a walk through this dungeon (Dungeon.DungeonTravel.Enter): its map id; consumed by DungeonMode.</summary>
        public static string DungeonId;

        /// <summary>The heroes who went down into the dungeon, as they were (consumed by DungeonMode).</summary>
        public static System.Collections.Generic.List<HeroEntry> DungeonHeroes;

        /// <summary>The town's save name while the heroes are below, given back to it when they come up.</summary>
        public static string DungeonReturnName;

        /// <summary>The name a loaded game is saved back under, when the slot it came from is not it (the town coming back from a dungeon).</summary>
        public static string SaveNameAfterLoad;
    }
}
