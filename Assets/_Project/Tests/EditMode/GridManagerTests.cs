using System.Reflection;
using CityBuilder.Grid;
using NUnit.Framework;
using UnityEngine;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// GridManager has no public constructor/setters (its fields are only ever wired via
    /// SerializedObject in SetupProject) -- reflection is the only way to configure a test
    /// instance directly. DestroyImmediate in TearDown is required (not Destroy, which defers
    /// outside Play mode) so the next test's SetUp doesn't collide with a leftover Instance.
    /// </summary>
    public class GridManagerTests
    {
        private GameObject _go;
        private GridManager _grid;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestGridManager");
            _grid = _go.AddComponent<GridManager>();
            SetPrivateField("cellSize", 2f);
            SetPrivateField("originWorldPosition", new Vector3(-10f, 0f, -10f));
            SetPrivateField("gridSize", new Vector2Int(10, 10));
            SetPrivateField("groundHeight", 1f);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        private void SetPrivateField(string name, object value)
        {
            typeof(GridManager).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(_grid, value);
        }

        [Test]
        public void WorldToCell_AtOrigin_IsCellZero()
        {
            Assert.AreEqual(new Vector2Int(0, 0), _grid.WorldToCell(new Vector3(-10f, 0f, -10f)));
        }

        [Test]
        public void WorldToCell_OneCellSizeAway_IsCellOne()
        {
            Assert.AreEqual(new Vector2Int(1, 1), _grid.WorldToCell(new Vector3(-8f, 0f, -8f)));
        }

        [Test]
        public void CellToWorld_IsInverseOfWorldToCell_AtCellCorners()
        {
            var cell = new Vector2Int(3, 4);
            var world = _grid.CellToWorld(cell);
            Assert.AreEqual(cell, _grid.WorldToCell(world));
        }

        [Test]
        public void WorldToCell_MidCell_RoundsDownNotToNearest()
        {
            // 1.9 cells in should still floor to cell 1, not round up to 2 -- WorldToCell must use
            // FloorToInt, matching how a click anywhere inside a cell should resolve to that cell.
            var world = new Vector3(-10f + 2f * 1.9f, 0f, -10f);
            Assert.AreEqual(1, _grid.WorldToCell(world).x);
        }

        [Test]
        public void GetFootprintCenterWorld_2x2Footprint_IsOffsetByOneCell()
        {
            var center = _grid.GetFootprintCenterWorld(Vector2Int.zero, new Vector2Int(2, 2));
            Assert.AreEqual(new Vector3(-8f, 1f, -8f), center);
        }

        [Test]
        public void IsWithinBounds_NegativeOrigin_IsFalse()
        {
            Assert.IsFalse(_grid.IsWithinBounds(new Vector2Int(-1, 0), Vector2Int.one));
        }

        [Test]
        public void IsWithinBounds_ExactlyFillingGrid_IsTrue()
        {
            Assert.IsTrue(_grid.IsWithinBounds(Vector2Int.zero, new Vector2Int(10, 10)));
        }

        [Test]
        public void IsWithinBounds_OneCellPastEdge_IsFalse()
        {
            Assert.IsFalse(_grid.IsWithinBounds(new Vector2Int(9, 9), new Vector2Int(2, 2)));
        }

        [Test]
        public void SetAreaOccupied_MakesAreaNotFree()
        {
            Assert.IsTrue(_grid.IsAreaFree(new Vector2Int(2, 2), Vector2Int.one));
            _grid.SetAreaOccupied(new Vector2Int(2, 2), Vector2Int.one, true);
            Assert.IsFalse(_grid.IsAreaFree(new Vector2Int(2, 2), Vector2Int.one));
        }

        [Test]
        public void SetAreaOccupied_False_FreesItAgain()
        {
            _grid.SetAreaOccupied(new Vector2Int(2, 2), Vector2Int.one, true);
            _grid.SetAreaOccupied(new Vector2Int(2, 2), Vector2Int.one, false);
            Assert.IsTrue(_grid.IsAreaFree(new Vector2Int(2, 2), Vector2Int.one));
        }

        [Test]
        public void IsAreaFree_PartiallyOverlappingOccupiedCell_IsFalse()
        {
            _grid.SetAreaOccupied(new Vector2Int(5, 5), Vector2Int.one, true);
            // A 2x2 footprint whose corner touches the single occupied cell must be rejected.
            Assert.IsFalse(_grid.IsAreaFree(new Vector2Int(4, 4), new Vector2Int(2, 2)));
        }

        [Test]
        public void CanPlace_OutOfBoundsEvenIfFree_IsFalse()
        {
            Assert.IsFalse(_grid.CanPlace(new Vector2Int(9, 9), new Vector2Int(5, 5)));
        }
    }
}
