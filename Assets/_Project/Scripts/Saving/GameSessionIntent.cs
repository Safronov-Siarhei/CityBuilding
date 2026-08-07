namespace CityBuilder.Saving
{
    /// <summary>
    /// Carries the player's menu choice (new game vs. continue) across the scene load from
    /// MainMenu to CityBuilder. A plain static field survives scene transitions within the
    /// same app run without needing a persistent singleton GameObject.
    /// </summary>
    public static class GameSessionIntent
    {
        public static bool LoadSavedGame;
    }
}
