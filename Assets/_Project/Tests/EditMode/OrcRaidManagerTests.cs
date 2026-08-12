using CityBuilder.Combat;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    public class OrcRaidManagerTests
    {
        [TestCase(1, 2)]
        [TestCase(3, 2)]
        [TestCase(4, 3)]
        [TestCase(7, 4)]
        public void ComputeRaidSize_GrowsWithDayCount(int day, int expected)
        {
            Assert.AreEqual(expected, OrcRaidManager.ComputeRaidSize(day));
        }

        [Test]
        public void ComputeRaidSize_ClampsAtMax()
        {
            Assert.AreEqual(8, OrcRaidManager.ComputeRaidSize(1000));
        }

        [Test]
        public void ComputeRaidSize_NeverBelowBaseSize()
        {
            Assert.AreEqual(2, OrcRaidManager.ComputeRaidSize(0));
        }
    }
}
