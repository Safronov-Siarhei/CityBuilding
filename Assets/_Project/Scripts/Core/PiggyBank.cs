using System;
using UnityEngine;

namespace CityBuilder.Core
{
    /// <summary>
    /// The piggy bank («Копилка», the user 2026-11-15/16; card Design/features/piggy-bank.md). Crystals pile up in it while
    /// the player plays (a crystal a minute of active play, PiggyRunner) and does its tasks (PiggyTasks); it holds
    /// piggy_cap_N of them and takes no more when full. From piggy_break_min the player may break it for real money
    /// (Store product piggy_N): every crystal goes to the Wallet, and the piggy comes back one level up -- bigger and
    /// dearer; the fifth level stays for good. A FULL piggy is ~+50 % crystals a dollar over the regular pack of its size.
    /// Kept on the device between games, like the wallet; its numbers are always the sandbox's (it is the account's,
    /// not a mode's).
    /// </summary>
    public static class PiggyBank
    {
        private const string LevelKey = "piggy.level";
        private const string StoredKey = "piggy.stored";

        /// <summary>The crystals in it or its level changed: buttons and the window redraw.</summary>
        public static event Action Changed;

        /// <summary>It was broken (bought): its crystals went to the wallet and the next level's piggy stands.</summary>
        public static event Action Broken;

        private static BalanceConfig Config => BalanceConfig.Sandbox;

        public static int Levels => Config != null ? Mathf.Max(1, Config.PiggyLevels) : 5;

        /// <summary>0-based: 0 = the pink ceramic one, Levels-1 = the amethyst one that stays for good.</summary>
        public static int Level => Mathf.Clamp(PlayerPrefs.GetInt(LevelKey, 0), 0, Levels - 1);

        public static int Stored => Mathf.Max(0, PlayerPrefs.GetInt(StoredKey, 0));

        public static int Capacity => Config != null ? Config.PiggyCap(Level) : 300;

        public static bool Full => Stored >= Capacity;

        public static int BreakMin => Config != null ? Config.PiggyBreakMin : 150;

        public static bool CanBreak => Stored >= BreakMin;

        /// <summary>The Store product that breaks the piggy now (piggy_1..5, one per level: each has its price).</summary>
        public static string ProductId => Store.PiggyId(Level);

        /// <summary>Puts crystals in, as many as fit; how many went in (0 when full).</summary>
        public static int Add(int crystals)
        {
            if (crystals <= 0) return 0;
            var room = Mathf.Max(0, Capacity - Stored);
            var added = Mathf.Min(room, crystals);
            if (added <= 0) return 0;
            CityBuilder.Core.Prefs.SetInt(StoredKey, Stored + added);
            Changed?.Invoke();
            return added;
        }

        /// <summary>
        /// The REAL gain of breaking it with `stored` crystals in it (the user, 2026-11-16: no «+50 %» while it is not
        /// full): its crystals a dollar against the regular pack it is compared with, in whole percent; 0 or less = none.
        /// </summary>
        public static int GainPercent(int stored) => GainPercent(stored, Level);

        public static int GainPercent(int stored, int level)
        {
            var c = Config;
            if (c == null || c.MenuShopPackCount == 0) return 0;
            var pack = c.PiggyRefPack(level);
            var packRate = c.MenuShopPackCrystals(pack) / Mathf.Max(0.01f, c.MenuShopPackUsd(pack));
            var rate = stored / Mathf.Max(0.01f, c.PiggyUsd(level));
            return Mathf.RoundToInt((rate / Mathf.Max(0.01f, packRate) - 1f) * 100f);
        }

        /// <summary>What a FULL piggy of the current level gains (the «!» popup's line).</summary>
        public static int FullGainPercent => GainPercent(Capacity, Level);

        /// <summary>The piggy just broken: its level (0-based) and the crystals that came out (the break animation).</summary>
        public static int LastBrokenLevel { get; private set; }

        public static int LastBrokenCrystals { get; private set; }

        /// <summary>The purchase went through (Store.Grant, after it paid the crystals out): empty, one level up.</summary>
        internal static void OnBroken()
        {
            LastBrokenLevel = Level;
            LastBrokenCrystals = Stored;
            CityBuilder.Core.Prefs.SetInt(LevelKey, Mathf.Min(Level + 1, Levels - 1));
            CityBuilder.Core.Prefs.SetInt(StoredKey, 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
            Broken?.Invoke();
        }

        /// <summary>Cheats: set the level (0-based) and the crystals in it.</summary>
        public static void CheatSet(int level, int stored)
        {
            CityBuilder.Core.Prefs.SetInt(LevelKey, Mathf.Clamp(level, 0, Levels - 1));
            CityBuilder.Core.Prefs.SetInt(StoredKey, Mathf.Max(0, stored));
            CityBuilder.Core.Prefs.SetInt(StoredKey, Mathf.Min(Stored, Capacity));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
