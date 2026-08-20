using CityBuilder.Buildings;
using NUnit.Framework;
using UnityEngine;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// Where a building lands relative to the point the player aimed at.
    ///
    /// Placement used the aimed cell as the footprint's ORIGIN -- its lowest corner -- so a 5x5
    /// Town Hall stood up and to the right of the crosshair rather than on it, and the player had
    /// to aim at a spot the building would not occupy. These assert the PROPERTY that matters
    /// (the aimed cell sits in the middle of what gets built) rather than restating the formula,
    /// so they would still catch a change that keeps the arithmetic and loses the intent.
    /// </summary>
    public class PlacementAimTests
    {
        private static readonly Vector2Int Aim = new Vector2Int(10, 10);

        /// <summary>Distance from the footprint's low edge to the aimed cell, and from the aimed cell to its high edge. Equal means centred.</summary>
        private static void AssertCentred(Vector2Int footprint)
        {
            var origin = BuildingPlacer.OriginForCentredFootprint(Aim, footprint);

            var beforeX = Aim.x - origin.x;
            var afterX = (origin.x + footprint.x - 1) - Aim.x;
            var beforeY = Aim.y - origin.y;
            var afterY = (origin.y + footprint.y - 1) - Aim.y;

            // An even footprint cannot be perfectly centred on a cell -- there is no middle cell --
            // so it may be off by exactly one, never more.
            Assert.LessOrEqual(Mathf.Abs(beforeX - afterX), footprint.x % 2 == 0 ? 1 : 0,
                $"footprint {footprint} is lopsided along X: {beforeX} before, {afterX} after");
            Assert.LessOrEqual(Mathf.Abs(beforeY - afterY), footprint.y % 2 == 0 ? 1 : 0,
                $"footprint {footprint} is lopsided along Y: {beforeY} before, {afterY} after");
        }

        [Test]
        public void TheTownHallStandsOnTheCrosshairRatherThanBesideIt()
        {
            // The 5x5 from the screenshot that started this: two cells of it on every side of the aim.
            AssertCentred(new Vector2Int(5, 5));
        }

        [Test]
        public void TheAimedCellIsAlwaysOneOfTheCellsTheBuildingCovers()
        {
            foreach (var footprint in new[]
                     {
                         new Vector2Int(1, 1), new Vector2Int(2, 2), new Vector2Int(3, 3),
                         new Vector2Int(4, 2), new Vector2Int(2, 4), new Vector2Int(5, 5),
                     })
            {
                var origin = BuildingPlacer.OriginForCentredFootprint(Aim, footprint);
                var delta = Aim - origin;

                Assert.IsTrue(delta.x >= 0 && delta.x < footprint.x && delta.y >= 0 && delta.y < footprint.y,
                    $"aiming at {Aim} with footprint {footprint} put the building at {origin}, which does not cover the aimed cell");
            }
        }

        [Test]
        public void EveryOddFootprintIsCentredExactly()
        {
            AssertCentred(new Vector2Int(1, 1));
            AssertCentred(new Vector2Int(3, 3));
            AssertCentred(new Vector2Int(5, 5));
        }

        [Test]
        public void AnEvenFootprintIsOffByAtMostOneCell()
        {
            AssertCentred(new Vector2Int(2, 2));
            AssertCentred(new Vector2Int(4, 4));
        }

        [Test]
        public void ARotatedFootprintCentresOnItsRotatedShape()
        {
            // A 4x2 turned on its side is a 2x4, and it has to centre as one -- the placer swaps
            // the axes before asking, so the two must be mirror images of each other.
            var flat = BuildingPlacer.OriginForCentredFootprint(Aim, new Vector2Int(4, 2));
            var turned = BuildingPlacer.OriginForCentredFootprint(Aim, new Vector2Int(2, 4));

            Assert.AreEqual(Aim.x - flat.x, Aim.y - turned.y);
            Assert.AreEqual(Aim.y - flat.y, Aim.x - turned.x);
        }

        [Test]
        public void ASingleCellBuildingLandsExactlyWhereItWasAimed()
        {
            // Roads and fences: the drawing mode leans on this, since it treats the cell under the
            // finger as the cell to build on.
            Assert.AreEqual(Aim, BuildingPlacer.OriginForCentredFootprint(Aim, new Vector2Int(1, 1)));
        }
    }
}
