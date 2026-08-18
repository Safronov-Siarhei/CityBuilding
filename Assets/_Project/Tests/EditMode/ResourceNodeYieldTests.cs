using CityBuilder.Maps;
using CityBuilder.Resources;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// What a tree and a boulder are worth, and the relationship between the two that makes them
    /// feel different to play against. Relationships rather than the numbers themselves, the same
    /// way FoodBalanceTests and MigrationBalanceTests are written: the sheet stays free to be
    /// retuned, while a retune that collapses the distinction fails loudly.
    ///
    /// The yields moved here from ManualGathering when gathering stopped being a hand-only affair:
    /// a hired worker and a citizen sent by hand take exactly the same slice, because it is the
    /// node that decides, not who is holding the axe.
    /// </summary>
    public class ResourceNodeYieldTests
    {
        [Test]
        public void ATree_ComesDownInOneVisit()
        {
            // The two being equal IS the mechanic: a tree has no half-felled state, so its whole
            // stock has to leave with the first worker who finishes chopping it.
            Assert.AreEqual(ResourceNode.TotalYieldFor(ResourceType.Wood),
                ResourceNode.YieldPerHarvestFor(ResourceType.Wood),
                "A tree should be felled by a single visit -- a per-trip yield below its stock would leave half a tree standing forever.");
        }

        [Test]
        public void ABoulder_TakesSeveralVisitsToWorkOut()
        {
            var total = ResourceNode.TotalYieldFor(ResourceType.Stone);
            var perVisit = ResourceNode.YieldPerHarvestFor(ResourceType.Stone);

            Assert.Greater(perVisit, 0, "A boulder that gives nothing per visit can never be worked at all.");
            Assert.Greater(total, perVisit * 2,
                "Stone is the map's finite resource and a boulder is supposed to be worth returning to; emptying one in a couple of trips makes depletion invisible.");
        }

        [TestCase(ResourceType.Food)]
        [TestCase(ResourceType.Gold)]
        [TestCase(ResourceType.Iron)]
        [TestCase(ResourceType.Coal)]
        [TestCase(ResourceType.Coins)]
        [TestCase(ResourceType.Population)]
        public void NothingElseGrowsOnTrees(ResourceType type)
        {
            // Only trees and boulders exist as world nodes -- anything else must yield nothing
            // rather than silently minting a resource with no source object.
            Assert.AreEqual(0, ResourceNode.TotalYieldFor(type));
            Assert.AreEqual(0, ResourceNode.YieldPerHarvestFor(type));
        }

        [Test]
        public void OneTree_StaysWellBelowABuildingCost()
        {
            // Hand gathering is the anti-deadlock floor, not a replacement for an economy: a
            // Sawmill costs 40 Wood, so one hand-chopped tree must not come close to paying for it.
            Assert.Less(ResourceNode.TotalYieldFor(ResourceType.Wood), 40);
        }
    }
}
