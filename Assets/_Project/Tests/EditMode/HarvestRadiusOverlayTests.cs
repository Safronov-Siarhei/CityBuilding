using CityBuilder.Grid;
using NUnit.Framework;
using UnityEngine;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// The shape of the green carpet under a gatherer.
    ///
    /// What matters is not that it looks tidy but that it tells the truth: the cells drawn have to
    /// be exactly the cells a worker will actually walk to. A carpet that reached one ring further
    /// than the workers do would have the player siting a Quarry to cover boulders it then refused
    /// to touch -- and with stone that never grows back, that mistake is permanent.
    /// </summary>
    public class HarvestRadiusOverlayTests
    {
        [Test]
        public void NoRadius_DrawsNothing()
        {
            Assert.IsEmpty(HarvestRadiusOverlay.CellsWithin(0, 1f));
            Assert.IsEmpty(HarvestRadiusOverlay.CellsWithin(-5, 1f));
        }

        [Test]
        public void EveryCellDrawn_IsInsideTheRadius()
        {
            const int radius = 12;
            foreach (var cell in HarvestRadiusOverlay.CellsWithin(radius, 1f))
            {
                var distance = new Vector2(cell.x, cell.y).magnitude;
                Assert.LessOrEqual(distance, radius + 0.001f,
                    $"Cell {cell} is {distance:0.00} away and would promise the player reach the workers do not have.");
            }
        }

        [Test]
        public void TheBuildingsOwnCell_IsAlwaysDrawn()
        {
            Assert.Contains(Vector2Int.zero, HarvestRadiusOverlay.CellsWithin(4, 1f));
        }

        [Test]
        public void TheEdge_IsIncludedAndTheRingBeyondIsNot()
        {
            const int radius = 10;
            var cells = HarvestRadiusOverlay.CellsWithin(radius, 1f);

            // Exactly on the line counts, matching the <= the node search uses.
            Assert.Contains(new Vector2Int(radius, 0), cells);
            Assert.Contains(new Vector2Int(0, -radius), cells);
            Assert.IsFalse(cells.Contains(new Vector2Int(radius + 1, 0)), "A cell past the radius was drawn.");
        }

        [Test]
        public void TheCarpet_IsRoundRatherThanSquare()
        {
            // A square would be the easy mistake and the wrong picture: reach is a distance, so
            // the corners of the bounding box are outside it. Area of a disc, within a cell or two
            // of quantisation either way.
            const int radius = 20;
            var cells = HarvestRadiusOverlay.CellsWithin(radius, 1f);

            var expected = Mathf.PI * radius * radius;
            Assert.That(cells.Count, Is.EqualTo(expected).Within(expected * 0.05f),
                "The drawn area is not a disc -- a square carpet would show reach into corners a worker cannot use.");
        }

        [Test]
        public void ALargerCell_MeansFewerOfThem()
        {
            // The overlay is drawn in whatever the grid's cell size happens to be, not in a
            // hardcoded metre, so doubling the cell size must quarter the count rather than
            // silently drawing a carpet of the wrong size.
            var fine = HarvestRadiusOverlay.CellsWithin(16, 1f).Count;
            var coarse = HarvestRadiusOverlay.CellsWithin(16, 2f).Count;

            Assert.That(coarse, Is.EqualTo(fine / 4f).Within(fine * 0.1f));
        }
    }
}
