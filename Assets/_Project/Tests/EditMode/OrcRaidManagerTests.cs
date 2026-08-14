using CityBuilder.Combat;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// The ramp formula itself, stated with explicit numbers rather than whatever the balance sheet
    /// currently holds: this is about the SHAPE of the curve (slow growth, hard ceiling, never below
    /// the opening squad), which must survive any retune. The sheet's own numbers are checked for
    /// sanity in BalanceConfigTests instead.
    /// </summary>
    public class OrcRaidManagerTests
    {
        private const int BaseSize = 2;
        private const int DaysPerExtraRaider = 3;
        private const int MaxSize = 8;

        private static int RaidSize(int day) => OrcRaidManager.ComputeRaidSize(day, BaseSize, DaysPerExtraRaider, MaxSize);

        [TestCase(1, 2)]
        [TestCase(3, 2)]
        [TestCase(4, 3)]
        [TestCase(7, 4)]
        public void ComputeRaidSize_GrowsWithDayCount(int day, int expected)
        {
            Assert.AreEqual(expected, RaidSize(day));
        }

        [Test]
        public void ComputeRaidSize_ClampsAtMax()
        {
            Assert.AreEqual(MaxSize, RaidSize(1000));
        }

        [Test]
        public void ComputeRaidSize_NeverBelowBaseSize()
        {
            Assert.AreEqual(BaseSize, RaidSize(0));
        }

        [Test]
        public void ComputeRaidSize_SurvivesAZeroDayStep()
        {
            // A sheet edited to 0 must not divide by zero and take the game down with it.
            Assert.AreEqual(BaseSize, OrcRaidManager.ComputeRaidSize(50, BaseSize, 0, MaxSize));
        }
    }
}
