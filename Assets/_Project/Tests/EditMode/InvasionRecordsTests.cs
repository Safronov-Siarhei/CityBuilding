using System;
using System.Collections.Generic;
using CityBuilder.Combat;
using CityBuilder.Invasion;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// The Invasion's points as the user set them (2026-10-21/22), with the numbers stated here: a
    /// run is the waves survived squared; a day is the sum of its best five runs; a week (from
    /// Monday) and a month are sums of their days.
    /// </summary>
    public class InvasionRecordsTests
    {
        private static readonly DateTime Wednesday = new DateTime(2026, 10, 21, 18, 0, 0);

        [Test]
        public void ARun_ScoresTheWavesSquared()
        {
            Assert.AreEqual(100, InvasionScore.WavePointsFor(10, 1));
            Assert.AreEqual(400, InvasionScore.WavePointsFor(20, 1));
            Assert.AreEqual(900, InvasionScore.WavePointsFor(30, 1));
            Assert.AreEqual(0, InvasionScore.WavePointsFor(0, 1));
            Assert.IsTrue(InvasionScore.Scores(OrcCampBuildingKind.Watchtower));
            Assert.IsFalse(InvasionScore.Scores(OrcCampBuildingKind.Palisade), "A wall piece scores: a wall is a hundred of them.");
            Assert.IsFalse(InvasionScore.Scores(OrcCampBuildingKind.Gate));
        }

        [Test]
        public void ADay_CountsOnlyItsBestRuns()
        {
            var days = new List<InvasionRecords.Day>();
            foreach (var points in new[] { 10, 50, 30, 70, 20, 90, 40 }) InvasionRecords.Add(days, points, Wednesday, 5);
            Assert.AreEqual(90 + 70 + 50 + 40 + 30, InvasionRecords.DayPoints(days, Wednesday, 5));
            Assert.AreEqual(5, InvasionRecords.RunsCounted(days, Wednesday, 5));
            Assert.AreEqual(5, days[0].runs.Count, "The day keeps more runs than count.");

            var thursday = Wednesday.AddDays(1);
            InvasionRecords.Add(days, 25, thursday, 5);
            Assert.AreEqual(1, InvasionRecords.RunsCounted(days, thursday, 5));
            Assert.AreEqual(25, InvasionRecords.DayPoints(days, thursday, 5), "A new day did not start from nothing.");
        }

        [Test]
        public void AWeekFromMonday_AndAMonth_SumTheirDays()
        {
            var days = new List<InvasionRecords.Day>();
            InvasionRecords.Add(days, 100, new DateTime(2026, 10, 18), 5);   // Sunday: the week before
            InvasionRecords.Add(days, 10, new DateTime(2026, 10, 19), 5);    // Monday
            InvasionRecords.Add(days, 20, Wednesday, 5);
            InvasionRecords.Add(days, 30, new DateTime(2026, 10, 25), 5);    // Sunday
            InvasionRecords.Add(days, 40, new DateTime(2026, 10, 26), 5);    // the next Monday
            InvasionRecords.Add(days, 1000, new DateTime(2026, 9, 30), 5);   // the month before

            Assert.AreEqual(10 + 20 + 30, InvasionRecords.WeekPoints(days, Wednesday, 5));
            Assert.AreEqual(10 + 20 + 30, InvasionRecords.WeekPoints(days, new DateTime(2026, 10, 25), 5), "Sunday is not the week's last day.");
            Assert.AreEqual(100 + 10 + 20 + 30 + 40, InvasionRecords.MonthPoints(days, Wednesday, 5));
        }
    }
}
