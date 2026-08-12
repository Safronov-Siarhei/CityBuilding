using CityBuilder.Citizens;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    public class ProductionBuildingDecayMultiplierTests
    {
        [Test]
        public void BelowPenaltyThreshold_ProducesAtFullRate()
        {
            Assert.AreEqual(1f, ProductionBuilding.ComputeDecayProductionMultiplier(0.3f));
        }

        [Test]
        public void AtPenaltyThreshold_ProducesAtFullRate()
        {
            // Boundary itself (0.7) is inclusive of the "still full rate" side.
            Assert.AreEqual(1f, ProductionBuilding.ComputeDecayProductionMultiplier(0.7f));
        }

        [Test]
        public void AtFullDecay_ProducesAtFloorRate()
        {
            Assert.AreEqual(0.5f, ProductionBuilding.ComputeDecayProductionMultiplier(1f), 0.0001f);
        }

        [Test]
        public void MidwayThroughPenaltyBand_IsMidwayBetweenFullAndFloor()
        {
            // Halfway between 0.7 and 1.0 decay (0.85) should read halfway between 1.0 and 0.5 output.
            Assert.AreEqual(0.75f, ProductionBuilding.ComputeDecayProductionMultiplier(0.85f), 0.0001f);
        }
    }
}
