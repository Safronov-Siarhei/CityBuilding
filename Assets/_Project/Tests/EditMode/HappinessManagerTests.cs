using CityBuilder.Core;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    public class HappinessManagerTests
    {
        [TestCase(0, 100)]
        [TestCase(10, 90)]
        [TestCase(100, 0)]
        // Rates outside 0-100 can't actually happen (TaxManager.SetTaxRate clamps), but the score
        // formula itself must not go out of its own 0-100 range if it's ever called with one.
        [TestCase(150, 0)]
        [TestCase(-10, 100)]
        public void ComputeTaxScore_IsInverseOfTaxRate(int taxRatePercent, int expected)
        {
            Assert.AreEqual(expected, HappinessManager.ComputeTaxScore(taxRatePercent));
        }

        [Test]
        public void ComputeDecayScore_NoDecayingBuildings_Returns100()
        {
            Assert.AreEqual(100, HappinessManager.ComputeDecayScore(totalDecay: 0f, buildingsCounted: 0));
        }

        [Test]
        public void ComputeDecayScore_NoDecayAccrued_Returns100()
        {
            Assert.AreEqual(100, HappinessManager.ComputeDecayScore(totalDecay: 0f, buildingsCounted: 5));
        }

        [Test]
        public void ComputeDecayScore_FullyDecayed_Returns0()
        {
            Assert.AreEqual(0, HappinessManager.ComputeDecayScore(totalDecay: 3f, buildingsCounted: 3));
        }

        [Test]
        public void ComputeDecayScore_AveragesAcrossBuildings()
        {
            // Two buildings at 0 decay, one at full (1.0) -- average decay 1/3, score should read
            // roughly 2/3 of the way to 100, not be dragged all the way down by the one building.
            var score = HappinessManager.ComputeDecayScore(totalDecay: 1f, buildingsCounted: 3);
            Assert.AreEqual(67, score);
        }

        [Test]
        public void ComputeDefenseScore_NoPopulation_Returns100()
        {
            // No citizens to defend yet -- an empty settlement isn't "under-defended".
            Assert.AreEqual(100, HappinessManager.ComputeDefenseScore(totalDefense: 0, population: 0));
        }

        [Test]
        public void ComputeDefenseScore_NoDefenseWithPopulation_Returns0()
        {
            Assert.AreEqual(0, HappinessManager.ComputeDefenseScore(totalDefense: 0, population: 10));
        }

        [Test]
        public void ComputeDefenseScore_MeetsTarget_Returns100()
        {
            // DefensePerCitizenTarget is 0.5 -- 10 population needs 5 total defense to hit 100.
            Assert.AreEqual(100, HappinessManager.ComputeDefenseScore(totalDefense: 5, population: 10));
        }

        [Test]
        public void ComputeDefenseScore_ClampsAboveTarget()
        {
            // Regression guard for the bug where this used to multiply defense by BuildingInstance
            // .Level -- massively over-defending must still clamp at 100, not overflow past it.
            Assert.AreEqual(100, HappinessManager.ComputeDefenseScore(totalDefense: 500, population: 10));
        }

        [Test]
        public void ComputeDefenseScore_PartialCoverage_ScalesLinearly()
        {
            // Half the target defense (2.5 of 5) should read as roughly half the score.
            Assert.AreEqual(50, HappinessManager.ComputeDefenseScore(totalDefense: 2, population: 8));
        }
    }
}
