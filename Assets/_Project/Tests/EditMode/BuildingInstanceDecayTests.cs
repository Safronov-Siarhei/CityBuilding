using CityBuilder.Buildings;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    public class BuildingInstanceDecayTests
    {
        [Test]
        public void ComputeNextDecay_Level1_AccruesBaseRate()
        {
            // DecayPerDayBase is 2%/day at level 1.
            Assert.AreEqual(0.02f, BuildingInstance.ComputeNextDecay(currentDecay: 0f, level: 1), 0.0001f);
        }

        [TestCase(1, 0.02f)]
        [TestCase(2, 0.01f)]
        [TestCase(3, 0.02f / 3f)]
        public void ComputeNextDecay_HigherLevelAccruesSlower(int level, float expectedDelta)
        {
            var next = BuildingInstance.ComputeNextDecay(currentDecay: 0f, level: level);
            Assert.AreEqual(expectedDelta, next, 0.0001f);
        }

        [Test]
        public void ComputeNextDecay_ClampsAt1()
        {
            Assert.AreEqual(1f, BuildingInstance.ComputeNextDecay(currentDecay: 0.99f, level: 1));
        }

        [Test]
        public void ComputeNextDecay_LevelBelow1_TreatedAsLevel1()
        {
            // Mathf.Max(1, level) guards against a stray 0/negative level ever accelerating decay.
            var atZero = BuildingInstance.ComputeNextDecay(currentDecay: 0f, level: 0);
            var atOne = BuildingInstance.ComputeNextDecay(currentDecay: 0f, level: 1);
            Assert.AreEqual(atOne, atZero, 0.0001f);
        }
    }
}
