using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CityBuilder.Invasion
{
    /// <summary>
    /// The player's Invasion points, kept on the device until there is an online leaderboard
    /// (the user, 2026-10-22: local first). The rules are the user's (2026-10-20/22):
    ///
    /// - a run counts only when it ends with the castle fallen -- one left or cut short by a crash is
    ///   simply not written down (the run is not counted);
    /// - a day's points are the sum of its best inv_runs_per_day runs: play as often as you like,
    ///   only the best few add up;
    /// - a week's and a month's points are the sums of their best inv_runs_per_day runs (the user,
    ///   2026-10-25; was the sum of their days' points), local calendar, the week from Monday.
    ///
    /// The file keeps each day's best runs and nothing older than a couple of months. The sums are
    /// pure statics over a list of days, so the tests need no file and no clock.
    /// </summary>
    public static class InvasionRecords
    {
        private const string FileName = "invasion_records.json";
        private const int KeepDays = 62;

        [Serializable]
        public class Day
        {
            /// <summary>yyyy-MM-dd, local time.</summary>
            public string date;
            /// <summary>The day's best runs, best first, at most the counted number.</summary>
            public List<int> runs = new List<int>();
        }

        [Serializable]
        private class Store
        {
            public List<Day> days = new List<Day>();
        }

        /// <summary>Set by the tests to a temporary file, so a test run never touches the player's records.</summary>
        public static string FilePathOverride;

        public static string FilePath => FilePathOverride ?? Path.Combine(Application.persistentDataPath, FileName);

        public static string DateKey(DateTime day) => day.ToString("yyyy-MM-dd");

        /// <summary>Writes a finished run's points under `when`'s day and keeps only that day's best `counted`.</summary>
        public static void Record(int points, DateTime when, int counted)
        {
            var store = Load();
            Add(store.days, points, when, counted);
            Prune(store.days, when);
            Save(store);
        }

        public static List<Day> LoadDays() => Load().days;

        /// <summary>Adds the run to its day: the day keeps its best `counted` runs, best first.</summary>
        public static void Add(List<Day> days, int points, DateTime when, int counted)
        {
            var key = DateKey(when);
            var day = days.Find(d => d.date == key);
            if (day == null)
            {
                day = new Day { date = key };
                days.Add(day);
            }
            day.runs.Add(points);
            day.runs.Sort((a, b) => b.CompareTo(a));
            if (day.runs.Count > counted) day.runs.RemoveRange(counted, day.runs.Count - counted);
        }

        /// <summary>The day's points: the sum of its best `counted` runs.</summary>
        public static int DayPoints(List<Day> days, DateTime day, int counted)
        {
            var found = days.Find(d => d.date == DateKey(day));
            if (found == null) return 0;
            var sum = 0;
            for (var i = 0; i < found.runs.Count && i < counted; i++) sum += found.runs[i];
            return sum;
        }

        /// <summary>How many of the day's runs count so far (at most `counted`).</summary>
        public static int RunsCounted(List<Day> days, DateTime day, int counted)
        {
            var found = days.Find(d => d.date == DateKey(day));
            return found == null ? 0 : Mathf.Min(found.runs.Count, counted);
        }

        /// <summary>A leaderboard's span: the day, the week (Monday to Sunday) or the month round a date.</summary>
        public enum Period { Day = 0, Week = 1, Month = 2 }

        /// <summary>
        /// The period's best `counted` runs, best first (the user, 2026-10-25: the hub shows them,
        /// and their sum is the period's points). Each day keeps its own best `counted`, so these are
        /// exact for a week and a month too.
        /// </summary>
        public static List<int> TopRuns(List<Day> days, DateTime day, Period period, int counted)
        {
            var first = period == Period.Day ? day.Date
                : period == Period.Week ? day.Date.AddDays(-(((int)day.DayOfWeek + 6) % 7))
                : new DateTime(day.Year, day.Month, 1);
            var last = period == Period.Day ? first : period == Period.Week ? first.AddDays(6) : first.AddMonths(1).AddDays(-1);
            var from = DateKey(first);
            var to = DateKey(last);
            var runs = new List<int>();
            foreach (var d in days)
            {
                if (string.CompareOrdinal(d.date, from) < 0 || string.CompareOrdinal(d.date, to) > 0) continue;
                runs.AddRange(d.runs);
            }
            runs.Sort((a, b) => b.CompareTo(a));
            if (runs.Count > counted) runs.RemoveRange(counted, runs.Count - counted);
            return runs;
        }

        /// <summary>The period's points: the sum of its best `counted` runs.</summary>
        public static int PeriodPoints(List<Day> days, DateTime day, Period period, int counted)
        {
            var sum = 0;
            foreach (var run in TopRuns(days, day, period, counted)) sum += run;
            return sum;
        }

        /// <summary>The week's points, Monday to Sunday round `day`: its best `counted` runs.</summary>
        public static int WeekPoints(List<Day> days, DateTime day, int counted) => PeriodPoints(days, day, Period.Week, counted);

        /// <summary>The month's points round `day`: its best `counted` runs.</summary>
        public static int MonthPoints(List<Day> days, DateTime day, int counted) => PeriodPoints(days, day, Period.Month, counted);

        private static void Prune(List<Day> days, DateTime now)
        {
            var oldest = DateKey(now.Date.AddDays(-KeepDays));
            days.RemoveAll(d => string.CompareOrdinal(d.date, oldest) < 0);
        }

        private static Store Load()
        {
            try
            {
                if (File.Exists(FilePath)) return JsonUtility.FromJson<Store>(File.ReadAllText(FilePath)) ?? new Store();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"InvasionRecords: could not read {FilePath} -- starting afresh. {e.Message}");
            }
            return new Store();
        }

        private static void Save(Store store)
        {
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(store));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"InvasionRecords: could not write {FilePath}. {e.Message}");
            }
        }
    }
}
