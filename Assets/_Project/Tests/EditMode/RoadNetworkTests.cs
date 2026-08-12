using CityBuilder.Buildings;
using NUnit.Framework;
using UnityEngine;

namespace CityBuilder.Tests.EditMode
{
    public class RoadNetworkTests
    {
        private GameObject _go;
        private RoadNetwork _roads;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestRoadNetwork");
            _roads = _go.AddComponent<RoadNetwork>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void UnregisteredCell_IsNotARoad()
        {
            Assert.IsFalse(_roads.IsRoad(new Vector2Int(3, 3)));
        }

        [Test]
        public void RegisterRoad_MakesOnlyThatCellARoad()
        {
            _roads.RegisterRoad(new Vector2Int(3, 3));

            Assert.IsTrue(_roads.IsRoad(new Vector2Int(3, 3)));
            Assert.IsFalse(_roads.IsRoad(new Vector2Int(3, 4)));
        }

        [Test]
        public void RegisterRoad_SameCellTwice_StaysIdempotent()
        {
            _roads.RegisterRoad(new Vector2Int(1, 1));
            _roads.RegisterRoad(new Vector2Int(1, 1));

            Assert.IsTrue(_roads.IsRoad(new Vector2Int(1, 1)));
        }
    }
}
