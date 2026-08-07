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
    }
}
