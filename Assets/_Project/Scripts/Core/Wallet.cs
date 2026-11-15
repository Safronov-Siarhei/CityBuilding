using UnityEngine;

namespace CityBuilder.Core
{
    /// <summary>
    /// The player's crystals, the game's ONE currency (the user, 2026-11-11: the sandbox's crystals and
    /// the Invasion's are the same; there was a second purse until then). Found in dungeons and caches,
    /// won in the tutorial, spent on the Laboratory's levels 3-5 and ransoms; the Invasion's leaderboards,
    /// ads and the shop later. Kept on the device, between games.
    /// </summary>
    public static class Wallet
    {
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

        private const string CrystalMineKey = "wallet.crystal_mine_owned";

        /// <summary>
        /// The Рудник кристаллов was bought (the user, 2026-11-14: a paid building, ~$8 in the shop). Bought
        /// once, it may stand on every sandbox map (one per map). Set by Store (the main menu's «Магазин», the
        /// mine's window in the sandbox) and the cheats. Kept on the device, like the crystals.
        /// </summary>
        public static bool CrystalMineOwned
        {
            get => PlayerPrefs.GetInt(CrystalMineKey, 0) == 1;
            set
            {
                var was = CrystalMineOwned;
                PlayerPrefs.SetInt(CrystalMineKey, value ? 1 : 0);
                PlayerPrefs.Save();
                if (was != value) CrystalMineChanged?.Invoke();
            }
        }

        /// <summary>Raised when the mine is bought or its purchase is taken back (a refund): the sandbox unlocks or stops it.</summary>
        public static event System.Action CrystalMineChanged;
    }
}
