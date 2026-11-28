using UnityEngine;

namespace CityBuilder.Core
{
    /// <summary>
    /// Has this ACCOUNT played the sandbox before (the user, 2026-11-28)? A new player's first visit goes straight into a
    /// game on the start map (no map table, no choice -- and that map is in the install); a player who played before
    /// (the flag comes back from the cloud after a reinstall, or a save is on the phone) gets the map table at once, any
    /// code, random maps -- a map whose islands are still downloading waits on the loading screen.
    /// </summary>
    public static class SandboxHistory
    {
        /// <summary>Written through Prefs, so it rides in the cloud's progress snapshot.</summary>
        public const string PlayedKey = "sandbox.played";

        public static bool PlayedBefore => PlayerPrefs.GetInt(PlayedKey, 0) == 1 || Saving.SaveSystem.HasAnySave();

        public static void MarkPlayed()
        {
            if (PlayerPrefs.GetInt(PlayedKey, 0) != 1) Prefs.SetInt(PlayedKey, 1);
        }
    }
}
