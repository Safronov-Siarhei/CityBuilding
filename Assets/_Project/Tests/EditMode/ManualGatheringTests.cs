using CityBuilder.Citizens;
using CityBuilder.Resources;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    public class ManualGatheringTests
    {
        [Test]
        public void YieldFor_Wood_IsPositive()
        {
            Assert.Greater(ManualGathering.YieldFor(ResourceType.Wood), 0);
        }

        [Test]
        public void YieldFor_Stone_IsPositive()
        {
            Assert.Greater(ManualGathering.YieldFor(ResourceType.Stone), 0);
        }

        [TestCase(ResourceType.Food)]
        [TestCase(ResourceType.Gold)]
        [TestCase(ResourceType.Iron)]
        [TestCase(ResourceType.Coal)]
        [TestCase(ResourceType.Coins)]
        [TestCase(ResourceType.Population)]
        public void YieldFor_NonGatherableTypes_IsZero(ResourceType type)
        {
            // Only trees and boulders exist as hand-gatherable world nodes -- anything else must
            // yield nothing rather than silently minting a resource with no source object.
            Assert.AreEqual(0, ManualGathering.YieldFor(type));
        }

        [Test]
        public void YieldFor_StaysWellBelowABuildingCost()
        {
            // The anti-deadlock floor is meant to be slow, not a replacement for an economy: a
            // Lumberjack costs 40 Wood, so one hand-chopped tree must not come close to it.
            Assert.Less(ManualGathering.YieldFor(ResourceType.Wood), 40);
        }
    }
}
