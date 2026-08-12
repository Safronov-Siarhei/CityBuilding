using CityBuilder.Citizens;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    public class SettlementTierManagerTests
    {
        // Index mirrors SettlementTierManager.Tiers: 0=Хутор(0) 1=Деревня(20) 2=Городок(50)
        // 3=Город(100) 4=Королевство(200). If those thresholds ever change, update this test to
        // match -- it's asserting the *behavior*, not pinning the current numbers as sacred.
        [TestCase(0, 0)]
        [TestCase(19, 0)]
        [TestCase(20, 1)]
        [TestCase(49, 1)]
        [TestCase(50, 2)]
        [TestCase(99, 2)]
        [TestCase(100, 3)]
        [TestCase(199, 3)]
        [TestCase(200, 4)]
        [TestCase(1000, 4)]
        public void ResolveTierIndex_PicksHighestTierAtOrBelowPopulation(int population, int expectedIndex)
        {
            Assert.AreEqual(expectedIndex, SettlementTierManager.ResolveTierIndex(population));
        }
    }
}
