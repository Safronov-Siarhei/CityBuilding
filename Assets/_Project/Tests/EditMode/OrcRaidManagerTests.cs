using CityBuilder.Combat;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// The three ramps a raid is built from -- how many, how strong, how often -- stated with
    /// explicit numbers rather than whatever the balance sheet currently holds. This is about the
    /// SHAPE of each curve (slow growth, hard ceilings, never below the opening squad, never faster
    /// than the floor), which has to survive any retune. The sheet's own numbers are checked for
    /// sanity in BalanceConfigTests instead.
    ///
    /// All three take the PLAYER'S PROGRESSION SCORE, not the calendar day (see PlayerProgression).
    /// The day count used to drive the size on its own, and it measured how long the player had sat
    /// there rather than what they had built.
    /// </summary>
    public class OrcRaidManagerTests
    {
        private const int BaseSize = 2;
        private const int ProgressPerExtraRaider = 40;
        private const int MaxSize = 8;

        private const int ProgressPerOrcLevel = 150;
        private const int MaxOrcLevel = 5;

        private const float IntervalAtZero = 90f;
        private const float MinInterval = 45f;
        private const int ProgressAtMinInterval = 400;

        private static int RaidSize(int score) => OrcRaidManager.ComputeRaidSize(score, BaseSize, ProgressPerExtraRaider, MaxSize);

        private static int OrcLevel(int score) => OrcRaidManager.ComputeOrcLevel(score, ProgressPerOrcLevel, MaxOrcLevel);

        private static float Interval(int score) =>
            OrcRaidManager.ComputeRaidIntervalSeconds(score, IntervalAtZero, MinInterval, ProgressAtMinInterval);

        [TestCase(0, 2)]
        [TestCase(39, 2)]
        [TestCase(40, 3)]
        [TestCase(170, 6)]
        public void ComputeRaidSize_GrowsWithTheScore(int score, int expected)
        {
            Assert.AreEqual(expected, RaidSize(score));
        }

        [Test]
        public void ComputeRaidSize_ClampsAtMax()
        {
            Assert.AreEqual(MaxSize, RaidSize(100000));
        }

        [Test]
        public void ComputeRaidSize_NeverBelowBaseSize()
        {
            Assert.AreEqual(BaseSize, RaidSize(0));
        }

        [Test]
        public void ComputeRaidSize_SurvivesAZeroStep()
        {
            // A sheet edited to 0 must not divide by zero and take the game down with it.
            Assert.AreEqual(BaseSize, OrcRaidManager.ComputeRaidSize(500, BaseSize, 0, MaxSize));
        }

        [TestCase(0, 1)]
        [TestCase(149, 1)]
        [TestCase(150, 2)]
        [TestCase(450, 4)]
        public void ComputeOrcLevel_GrowsWithTheScore(int score, int expected)
        {
            Assert.AreEqual(expected, OrcLevel(score));
        }

        /// <summary>
        /// The point of the level ramp: past the size cap a raid can only get worse by getting
        /// stronger, so a player who has out-built the ceiling still has something to fear.
        /// </summary>
        [Test]
        public void ComputeOrcLevel_KeepsRisingAfterTheSizeCapIsReached()
        {
            var scoreAtSizeCap = ProgressPerExtraRaider * (MaxSize - BaseSize);

            Assert.AreEqual(MaxSize, RaidSize(scoreAtSizeCap), "The fixture's own numbers were meant to put the size at its ceiling here.");
            Assert.Greater(OrcLevel(scoreAtSizeCap * 3), OrcLevel(scoreAtSizeCap),
                "Once the squad cannot get any bigger, growing the town stops making raids worse at all.");
        }

        [Test]
        public void ComputeOrcLevel_ClampsAtMaxAndNeverBelowOne()
        {
            Assert.AreEqual(MaxOrcLevel, OrcLevel(100000));
            Assert.AreEqual(1, OrcLevel(-500), "A negative score would otherwise produce a level 0 orc with no health at all.");
        }

        [Test]
        public void ComputeOrcLevel_SurvivesAZeroStep()
        {
            Assert.AreEqual(1, OrcRaidManager.ComputeOrcLevel(500, 0, MaxOrcLevel));
        }

        [TestCase(0, IntervalAtZero)]
        [TestCase(200, 67.5f)]
        [TestCase(400, MinInterval)]
        [TestCase(4000, MinInterval)]
        public void ComputeRaidInterval_FallsWithTheScoreAndStopsAtTheFloor(int score, float expected)
        {
            Assert.AreEqual(expected, Interval(score), 0.01f);
        }

        /// <summary>A floor above the ceiling, or a zero threshold, is a sheet mistake -- and it has to make raids rarer, never one per frame.</summary>
        [Test]
        public void ComputeRaidInterval_SurvivesANonsenseSheet()
        {
            Assert.AreEqual(IntervalAtZero, OrcRaidManager.ComputeRaidIntervalSeconds(500, IntervalAtZero, MinInterval, 0), 0.01f);
            Assert.AreEqual(IntervalAtZero, OrcRaidManager.ComputeRaidIntervalSeconds(500, IntervalAtZero, IntervalAtZero * 2f, ProgressAtMinInterval), 0.01f);
            Assert.Greater(Interval(100000), 0f, "A zero or negative interval spawns a squad every frame.");
        }
    }
}
