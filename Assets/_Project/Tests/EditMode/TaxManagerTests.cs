using CityBuilder.Resources;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    public class TaxManagerTests
    {
        [Test]
        public void ComputeDailyIncome_ZeroPopulation_IsZero()
        {
            Assert.AreEqual(0, TaxManager.ComputeDailyIncome(population: 0, taxRatePercent: 100));
        }

        [Test]
        public void ComputeDailyIncome_ZeroTaxRate_IsZero()
        {
            Assert.AreEqual(0, TaxManager.ComputeDailyIncome(population: 50, taxRatePercent: 0));
        }

        [Test]
        public void ComputeDailyIncome_MaxTaxRate_IsHalfCoinPerCitizen()
        {
            // 0.5 coins/citizen/day at 100% tax (first-pass constant) -- 20 citizens -> 10 coins.
            Assert.AreEqual(10, TaxManager.ComputeDailyIncome(population: 20, taxRatePercent: 100));
        }

        [Test]
        public void ComputeDailyIncome_ScalesWithTaxRate()
        {
            // Half the tax rate should roughly halve the income (rounded).
            Assert.AreEqual(5, TaxManager.ComputeDailyIncome(population: 20, taxRatePercent: 50));
        }

        [Test]
        public void ComputeDailyIncome_RoundsToNearestInt()
        {
            // 3 citizens * 0.5 * 1.0 = 1.5 -> rounds to 2 (Mathf.RoundToInt banker's-adjacent, but
            // this exact case rounds up).
            Assert.AreEqual(2, TaxManager.ComputeDailyIncome(population: 3, taxRatePercent: 100));
        }
    }
}
