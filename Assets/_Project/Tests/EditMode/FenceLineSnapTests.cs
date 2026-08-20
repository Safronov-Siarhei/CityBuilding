using CityBuilder.Buildings;
using NUnit.Framework;
using UnityEngine;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// The fence-drawing geometry: a line dragged from an anchor snaps to one of three axes.
    ///
    /// This is the half of the drawing mode that decides what the player ends up with, and it is
    /// the reason a fence is not drawn freehand like a road: a wall is nearly always a perimeter,
    /// and a crooked section cannot be taken back on its own -- demolition works a whole building
    /// at a time.
    /// </summary>
    public class FenceLineSnapTests
    {
        private static readonly Vector2Int Start = new Vector2Int(10, 10);

        [Test]
        public void AClearlyHorizontalDragStaysOnItsRow()
        {
            // 8 across, 1 up: nowhere near a diagonal.
            var end = BuildingPlacer.SnapStraightEnd(Start, new Vector2Int(18, 11));
            Assert.AreEqual(new Vector2Int(18, 10), end);
        }

        [Test]
        public void AClearlyVerticalDragStaysOnItsColumn()
        {
            var end = BuildingPlacer.SnapStraightEnd(Start, new Vector2Int(11, 18));
            Assert.AreEqual(new Vector2Int(10, 18), end);
        }

        [Test]
        public void ADragAtRoughlyFortyFiveDegreesBecomesADiagonal()
        {
            // 6 across, 5 up -- neither axis is twice the other, so it is read as a diagonal and
            // cut to the SHORTER of the two, which is what keeps it exactly 45 degrees.
            var end = BuildingPlacer.SnapStraightEnd(Start, new Vector2Int(16, 15));
            Assert.AreEqual(new Vector2Int(15, 15), end);
        }

        [Test]
        public void ADiagonalKeepsTheDirectionTheFingerWent()
        {
            var end = BuildingPlacer.SnapStraightEnd(Start, new Vector2Int(4, 5));
            Assert.AreEqual(new Vector2Int(5, 5), end, "down-left must stay down-left");
        }

        [Test]
        public void TheTwoToOneRuleIsWhereHorizontalTakesOver()
        {
            // Exactly 2:1 is horizontal; one cell less on that axis is not.
            Assert.AreEqual(new Vector2Int(14, 10), BuildingPlacer.SnapStraightEnd(Start, new Vector2Int(14, 12)),
                "4 across against 2 up is exactly the 2:1 threshold and must snap flat");

            // 3 across against 2 up falls short of 2:1, so it is a diagonal -- and a diagonal is
            // cut to the SHORTER axis, which lands it at +2/+2 rather than at the finger.
            Assert.AreEqual(new Vector2Int(12, 12), BuildingPlacer.SnapStraightEnd(Start, new Vector2Int(13, 12)),
                "3 across against 2 up is under the threshold, so it is a diagonal");
        }

        [Test]
        public void ADragThatWentNowhereEndsWhereItStarted()
        {
            Assert.AreEqual(Start, BuildingPlacer.SnapStraightEnd(Start, Start));
        }
    }
}
