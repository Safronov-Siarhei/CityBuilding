using System;
using System.Collections.Generic;
using CityBuilder.Core;

namespace CityBuilder.Invasion
{
    /// <summary>One line of the online rating.</summary>
    public struct RatingEntry
    {
        public int rank;
        public string name;
        public long score;
    }

    /// <summary>What a rating source answers for a period: its best players and the player's own line.</summary>
    public class RatingPage
    {
        public InvasionRecords.Period period;
        public readonly List<RatingEntry> top = new List<RatingEntry>();
        /// <summary>0 = not known (offline, not signed in, no score sent yet).</summary>
        public int playerRank;
        public long playerScore;
        /// <summary>False: the board is not reachable -- only the player's own points are known.</summary>
        public bool online;
    }

    /// <summary>Where the rating comes from: Google Play Games leaderboards once they are set up, the device until then.</summary>
    public interface IRatingSource
    {
        void Load(InvasionRecords.Period period, int count, Action<RatingPage> done);
        void Submit(InvasionRecords.Period period, long score);
    }

    /// <summary>
    /// The Invasion's online rating (the user, 2026-11-17): Google Play Games leaderboards, DAY and WEEK only (the
    /// month is gone), a «Доска славы» window with places and rewards. GPGS resets its boards on US Pacific time
    /// (the day at midnight, the week between Saturday and Sunday), so the periods' ends are counted the same way.
    /// The rewards by place are the sheet's (inv_rating_{day|week}_upto_N / _prize_N); the game itself gives them.
    /// Until the plugin and the Play Console boards exist, <see cref="Source"/> is the device: the player's own points,
    /// no place.
    /// </summary>
    public static class InvasionRating
    {
        public static IRatingSource Source = new LocalRatingSource();

        private static TimeZoneInfo _pacific;

        /// <summary>US Pacific time (Android knows it by its IANA name, Windows by its own); a fixed UTC-8 if neither.</summary>
        private static TimeZoneInfo Pacific
        {
            get
            {
                if (_pacific != null) return _pacific;
                foreach (var id in new[] { "America/Los_Angeles", "Pacific Standard Time" })
                {
                    try { return _pacific = TimeZoneInfo.FindSystemTimeZoneById(id); }
                    catch (Exception) { /* the next name */ }
                }
                return _pacific = TimeZoneInfo.CreateCustomTimeZone("PT-fixed", TimeSpan.FromHours(-8), "PT", "PT");
            }
        }

        /// <summary>When the period that holds `utcNow` ends, in UTC: the next Pacific midnight; for the week, the next
        /// one that starts a Sunday.</summary>
        public static DateTime PeriodEndUtc(InvasionRecords.Period period, DateTime utcNow)
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), Pacific);
            var end = local.Date.AddDays(1);
            if (period == InvasionRecords.Period.Week)
                while (end.DayOfWeek != DayOfWeek.Sunday) end = end.AddDays(1);
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(end, DateTimeKind.Unspecified), Pacific);
        }

        /// <summary>The prize for a place (0 if none or the place is not known).</summary>
        public static int PrizeFor(InvasionRecords.Period period, int rank)
        {
            if (rank <= 0) return 0;
            var config = BalanceConfig.Instance;
            if (config == null) return 0;
            var upTo = config.InvasionRatingUpTo(period == InvasionRecords.Period.Week);
            var prize = config.InvasionRatingPrize(period == InvasionRecords.Period.Week);
            for (var i = 0; i < upTo.Length && i < prize.Length; i++)
                if (upTo[i] <= 0 || rank <= upTo[i]) return prize[i];
            return 0;
        }
    }

    /// <summary>The device alone: the player's own points for the period, no board, no place.</summary>
    public class LocalRatingSource : IRatingSource
    {
        public void Load(InvasionRecords.Period period, int count, Action<RatingPage> done)
        {
            var page = new RatingPage { period = period, online = false };
            var counted = BalanceConfig.Instance != null ? BalanceConfig.Instance.InvasionRunsCountedPerDay : 5;
            foreach (var run in InvasionRecords.TopRuns(InvasionRecords.LoadDays(), DateTime.Now, period, counted)) page.playerScore += run;
            done?.Invoke(page);
        }

        public void Submit(InvasionRecords.Period period, long score) { }
    }
}
