using CityBuilder.Resources;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>The tax (the user, 2026-11-02): 1 coin a citizen a day, +0.2 per Tax House level.</summary>
    public class TaxManagerTests
    {
        [Test]
        public void ComputeDailyIncome_ZeroPopulation_IsZero()
        {
            Assert.AreEqual(0, TaxManager.ComputeDailyIncome(population: 0, taxHouseLevel: 5));
        }

        [Test]
        public void ComputeDailyIncome_NoTaxHouse_IsOneCoinPerCitizen()
        {
            Assert.AreEqual(20, TaxManager.ComputeDailyIncome(population: 20, taxHouseLevel: 0));
        }

        [Test]
        public void ComputeDailyIncome_EachTaxHouseLevel_AddsAFifthOfACoin()
        {
            Assert.AreEqual(24, TaxManager.ComputeDailyIncome(population: 20, taxHouseLevel: 1));
            Assert.AreEqual(40, TaxManager.ComputeDailyIncome(population: 20, taxHouseLevel: 5));
        }
    }
}
