using System;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// The daily login bonus (the user, 2026-11-11; card Design/features/monetization.md): a 10-day
    /// chain of crystals (daily_bonus_1..10 on the sheet: 10-10-10-10-50-10-10-10-10-99), one day per
    /// calendar day, then it starts again; a missed day starts it from day 1. «×2» for a rewarded ad.
    /// Kept on the device (PlayerPrefs) like the crystals' purse. Plain logic: DailyBonusWindow shows it.
    /// </summary>
    public static class DailyBonus
    {
        private const string LastPref = "daily.last_claim";
        private const string StreakPref = "daily.streak";
        public const int Days = 10;

        /// <summary>Today's bonus is waiting.</summary>
        public static bool Due => Last != Key(DateTime.Now);

        /// <summary>The day of the chain (0-based) today's bonus is: the next one after yesterday's, else day 1.</summary>
        public static int TodayIndex => IndexFor(Last, Streak, DateTime.Now);

        /// <summary>Days of the current chain already taken (shown ticked), today's included once taken.</summary>
        public static int Taken => Due ? (Last == Key(DateTime.Now.AddDays(-1)) ? Streak % Days : 0) : (Streak - 1) % Days + 1;

        private static string Last => PlayerPrefs.GetString(LastPref, string.Empty);
        private static int Streak => PlayerPrefs.GetInt(StreakPref, 0);

        public static string Key(DateTime day) => day.ToString("yyyy-MM-dd");

        /// <summary>The chain's rule, testable: after a claim yesterday the next day (wrapping after the 10th), else day 1.</summary>
        public static int IndexFor(string lastClaim, int streak, DateTime now)
        {
            if (lastClaim == Key(now)) return Math.Max(0, (streak - 1) % Days);
            return lastClaim == Key(now.AddDays(-1)) ? streak % Days : 0;
        }

        public static int Reward(int index)
        {
            var list = BalanceConfig.Sandbox != null ? BalanceConfig.Sandbox.DailyBonusCrystals : null;
            return list != null && index >= 0 && index < list.Length ? list[index] : 0;
        }

        /// <summary>Takes today's crystals (doubled after an ad). Returns how many; 0 if already taken.</summary>
        public static int Claim(bool doubled)
        {
            if (!Due) return 0;
            var index = TodayIndex;
            var amount = Reward(index);
            if (doubled) amount = Mathf.RoundToInt(amount * (BalanceConfig.Sandbox.DailyBonusAdMultiplier));
            Wallet.UpgradeCrystals += amount;
            PlayerPrefs.SetString(LastPref, Key(DateTime.Now));
            PlayerPrefs.SetInt(StreakPref, index + 1);
            PlayerPrefs.Save();
            // the chain's big days: a happy moment for the rate-us window (the user, 2026-11-11)
            if (index == 4 || index == Days - 1) RateUsDirector.Note("daily_big", menu: true);
            return amount;
        }

        /// <summary>The cheat: the bonus waits again (as if yesterday's was the last).</summary>
        public static void ResetToday()
        {
            if (Due) return;
            PlayerPrefs.SetString(LastPref, Key(DateTime.Now.AddDays(-1)));
            PlayerPrefs.Save();
        }
    }
}
