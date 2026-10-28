using UnityEngine;

namespace CityBuilder.Core
{
    /// <summary>
    /// The player's crystals, the game's own currency (the user, 2026-10-25): won by places in the
    /// Invasion's leaderboards, spent in the shop later. Neither is built yet -- the menu shows the
    /// count, 0 for now. Kept on the device.
    ///
    /// And the UPGRADE crystals (the user, 2026-10-28), a second purse: the Laboratory's levels 3-5
    /// cost one each on top of the coins. Sandbox only, and they stay between games -- found in
    /// dungeons, for an ad or bought in the shop later; only the cheat gives them for now.
    /// </summary>
    public static class Wallet
    {
        private const string CrystalsKey = "wallet.crystals";
        private const string UpgradeCrystalsKey = "wallet.upgrade_crystals";

        /// <summary>Raised when the upgrade crystals change, so an open window can redraw its count.</summary>
        public static event System.Action UpgradeCrystalsChanged;

        public static int UpgradeCrystals
        {
            get => PlayerPrefs.GetInt(UpgradeCrystalsKey, 0);
            set
            {
                PlayerPrefs.SetInt(UpgradeCrystalsKey, Mathf.Max(0, value));
                PlayerPrefs.Save();
                UpgradeCrystalsChanged?.Invoke();
            }
        }

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
