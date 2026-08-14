using CityBuilder.Buildings;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// The autotiling rule, checked over every one of the 16 ways a segment can be surrounded.
    ///
    /// Worth this much coverage because the failure mode is silent and visual: a wrong rotation
    /// doesn't throw, it just leaves a fence facing the wrong way somewhere on a map nobody looks
    /// at twice. The base orientations asserted here come from the FBX models themselves -- the
    /// straight run north-south, the corner joining south and east -- so if a model is ever
    /// re-exported turned differently, these tests are what says so.
    /// </summary>
    public class FenceShapeTests
    {
        private static (FenceVariant variant, int steps) Resolve(bool n, bool e, bool s, bool w)
        {
            FenceShape.Resolve(n, e, s, w, out var variant, out var steps);
            return (variant, steps);
        }

        [Test]
        public void NoNeighbours_KeepsTheModelsOwnOrientation()
        {
            Assert.AreEqual((FenceVariant.Straight, 0), Resolve(false, false, false, false));
        }

        [Test]
        public void OppositeNeighbours_UseTheStraightRun()
        {
            Assert.AreEqual((FenceVariant.Straight, 0), Resolve(true, false, true, false), "north+south is the model's own axis");
            Assert.AreEqual((FenceVariant.Straight, 1), Resolve(false, true, false, true), "east+west is that axis turned once");
        }

        [Test]
        public void SingleNeighbour_LinesUpWithIt()
        {
            Assert.AreEqual((FenceVariant.Straight, 0), Resolve(true, false, false, false));
            Assert.AreEqual((FenceVariant.Straight, 0), Resolve(false, false, true, false));
            Assert.AreEqual((FenceVariant.Straight, 1), Resolve(false, true, false, false));
            Assert.AreEqual((FenceVariant.Straight, 1), Resolve(false, false, false, true));
        }

        [Test]
        public void AdjacentNeighbours_UseTheCornerTurnedToMatch()
        {
            Assert.AreEqual((FenceVariant.Corner, 0), Resolve(false, true, true, false), "south+east is the corner model as authored");
            Assert.AreEqual((FenceVariant.Corner, 1), Resolve(false, false, true, true), "south+west");
            Assert.AreEqual((FenceVariant.Corner, 2), Resolve(true, false, false, true), "north+west");
            Assert.AreEqual((FenceVariant.Corner, 3), Resolve(true, true, false, false), "north+east");
        }

        /// <summary>
        /// There is no T model and no crossroads model, so those have to degrade to something that
        /// still reads as a continuous fence rather than a hole -- the straight run laid along
        /// whichever axis is occupied on both sides.
        /// </summary>
        [Test]
        public void ThreeOrFourNeighbours_FallBackToTheStraightRunAlongTheFullAxis()
        {
            Assert.AreEqual((FenceVariant.Straight, 0), Resolve(true, true, true, false), "north+south is complete, east tees in");
            Assert.AreEqual((FenceVariant.Straight, 0), Resolve(true, false, true, true), "north+south is complete, west tees in");
            Assert.AreEqual((FenceVariant.Straight, 1), Resolve(true, true, false, true), "east+west is complete, north tees in");
            Assert.AreEqual((FenceVariant.Straight, 1), Resolve(false, true, true, true), "east+west is complete, south tees in");
            Assert.AreEqual((FenceVariant.Straight, 0), Resolve(true, true, true, true), "a crossroads picks an axis and keeps it");
        }

        /// <summary>A corner is only ever the two-neighbour case: three sides is a junction, and picking a corner there would leave one side visibly unfinished.</summary>
        [Test]
        public void CornerIsNeverUsedForMoreThanTwoNeighbours()
        {
            for (var mask = 0; mask < 16; mask++)
            {
                var n = (mask & 1) != 0;
                var e = (mask & 2) != 0;
                var s = (mask & 4) != 0;
                var w = (mask & 8) != 0;
                var count = (n ? 1 : 0) + (e ? 1 : 0) + (s ? 1 : 0) + (w ? 1 : 0);

                var (variant, steps) = Resolve(n, e, s, w);

                if (variant == FenceVariant.Corner)
                {
                    Assert.AreEqual(2, count, $"mask {mask}: a corner needs exactly two neighbours");
                    Assert.IsFalse(n && s, $"mask {mask}: opposite sides are a straight run, not a corner");
                    Assert.IsFalse(e && w, $"mask {mask}: opposite sides are a straight run, not a corner");
                }

                Assert.That(steps, Is.InRange(0, 3), $"mask {mask}: rotation must stay within four quarter turns");
            }
        }
    }
}
