using System;
using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Shop;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// The main menu's «Новости» (the user, 2026-11-17; card Design/features/news.md): what the sheet's tab «Новости»
    /// holds (BalanceConfig.News, newest first), what this device has seen and claimed. Plain logic: NewsWindow draws
    /// it, NewsButton puts the red dot on the menu's letter and opens the window by itself.
    ///   read     once the item was shown open (PlayerPrefs news.read.&lt;number&gt;);
    ///   the dot  while any listed item is unread, or read with its reward still unclaimed;
    ///   by itself  only for a player with news_auto_open_hours of play behind them (PlayTime), once a launch,
    ///            when something is unread -- a new player gets the dot only.
    /// </summary>
    public static class News
    {
        private const string ReadPref = "news.read.";
        private const string ClaimedPref = "news.claimed.";

        private static bool _autoOpenedThisLaunch;

        /// <summary>Something read or claimed: the dot and the window redraw.</summary>
        public static event Action Changed;

        private static BalanceConfig Config => BalanceConfig.Instance;

        /// <summary>The items the window lists: the newest news_shown_count.</summary>
        public static List<BalanceConfig.NewsItem> Shown()
        {
            var result = new List<BalanceConfig.NewsItem>();
            var config = Config;
            if (config == null) return result;
            var all = config.News;
            for (var i = 0; i < all.Count && result.Count < config.NewsShownCount; i++) result.Add(all[i]);
            return result;
        }

        public static string Title(BalanceConfig.NewsItem item) => Localization.Get($"#news_{item.number}_title");
        public static string Text(BalanceConfig.NewsItem item) => Localization.Get($"#news_{item.number}_text");

        public static bool IsRead(BalanceConfig.NewsItem item) => PlayerPrefs.GetInt(ReadPref + item.number, 0) == 1;
        public static bool IsClaimed(BalanceConfig.NewsItem item) => PlayerPrefs.GetInt(ClaimedPref + item.number, 0) == 1;
        public static bool HasReward(BalanceConfig.NewsItem item) => item.rewards != null && item.rewards.Count > 0;
        public static bool RewardWaiting(BalanceConfig.NewsItem item) => HasReward(item) && !IsClaimed(item);

        /// <summary>This item keeps a red dot: never opened, or opened with its reward not taken.</summary>
        public static bool NeedsDot(BalanceConfig.NewsItem item) => !IsRead(item) || RewardWaiting(item);

        public static bool AnyDot
        {
            get
            {
                foreach (var item in Shown()) if (NeedsDot(item)) return true;
                return false;
            }
        }

        public static bool AnyUnread
        {
            get
            {
                foreach (var item in Shown()) if (!IsRead(item)) return true;
                return false;
            }
        }

        public static void MarkRead(BalanceConfig.NewsItem item)
        {
            if (item == null || IsRead(item)) return;
            CityBuilder.Core.Prefs.SetInt(ReadPref + item.number, 1);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>«Забрать»: the crystals into the purse, the boosters into the sandbox's stock; once per item.</summary>
        public static void Claim(BalanceConfig.NewsItem item)
        {
            if (item == null || !RewardWaiting(item)) return;
            CityBuilder.Core.Prefs.SetInt(ClaimedPref + item.number, 1);
            foreach (var reward in item.rewards)
            {
                if (reward.kind == "crystal") Wallet.UpgradeCrystals += reward.amount;
                else if (reward.kind == "booster" && !string.IsNullOrEmpty(reward.id)) Boosters.AddStock(reward.id, reward.amount);
            }
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>The window should open by itself now (and this launch has not done it yet).</summary>
        public static bool ShouldOpenByItself
        {
            get
            {
                var config = Config;
                return !_autoOpenedThisLaunch && config != null && AnyUnread && PlayTime.Hours >= config.NewsAutoOpenHours;
            }
        }

        public static void NoteOpenedByItself() => _autoOpenedThisLaunch = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _autoOpenedThisLaunch = false;
            Changed = null;
        }
    }
}
