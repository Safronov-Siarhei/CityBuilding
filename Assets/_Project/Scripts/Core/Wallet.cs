using UnityEngine;

namespace CityBuilder.Core
{
    /// <summary>
    /// The player's crystals, the game's own currency (the user, 2026-10-25): won by places in the
    /// Invasion's leaderboards, spent in the shop later. Neither is built yet -- the menu shows the
    /// count, 0 for now. Kept on the device.
    /// </summary>
    public static class Wallet
    {
        private const string CrystalsKey = "wallet.crystals";

        public static int Crystals
        {
            get => PlayerPrefs.GetInt(CrystalsKey, 0);
            set
            {
                PlayerPrefs.SetInt(CrystalsKey, Mathf.Max(0, value));
                PlayerPrefs.Save();
            }
        }
    }
}
