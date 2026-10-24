using CityBuilder.UI;
using NUnit.Framework;
using UnityEngine;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>A build card's price (the user, 2026-10-24): two prices in one row, a third on a second row below; hidden prices take no place.</summary>
    public class CardPriceRowsTests
    {
        private GameObject _root;
        private CardPriceRows _rows;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Cost", typeof(RectTransform));
            var top = new GameObject("Top", typeof(RectTransform)).GetComponent<RectTransform>();
            var bottom = new GameObject("Bottom", typeof(RectTransform)).GetComponent<RectTransform>();
            top.SetParent(_root.transform, false);
            bottom.SetParent(_root.transform, false);
            _rows = _root.AddComponent<CardPriceRows>();
            _rows.Wire(top, bottom);
            foreach (var type in new[] { "Wood", "Stone", "Coins" })
            {
                new GameObject("Icon_" + type, typeof(RectTransform)).transform.SetParent(top, false);
                new GameObject("Amount_" + type, typeof(RectTransform)).transform.SetParent(top, false);
            }
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        private void Show(string type, bool shown)
        {
            foreach (var row in new[] { _rows.Top, _rows.Bottom })
            {
                var icon = row.Find("Icon_" + type);
                if (icon != null) icon.gameObject.SetActive(shown);
                var amount = row.Find("Amount_" + type);
                if (amount != null) amount.gameObject.SetActive(shown);
            }
        }

        [Test]
        public void ThreePrices_TakeTwoRows_TheThirdBelow()
        {
            _rows.Arrange();
            Assert.IsTrue(_rows.Bottom.gameObject.activeSelf, "Three prices stayed in one row.");
            Assert.IsNotNull(_rows.Bottom.Find("Icon_Coins"), "The third price is not on the second row.");
            Assert.IsNotNull(_rows.Bottom.Find("Amount_Coins"), "The third price's number did not go with its icon.");
            Assert.IsNotNull(_rows.Top.Find("Icon_Wood"));
            Assert.IsNotNull(_rows.Top.Find("Icon_Stone"));
        }

        [Test]
        public void TwoPrices_StandInOneRow_AndTheSecondRowLeaves()
        {
            _rows.Arrange();
            Show("Stone", false);
            _rows.Arrange();
            Assert.IsFalse(_rows.Bottom.gameObject.activeSelf, "Two prices left an empty second row.");
            Assert.IsNotNull(_rows.Top.Find("Icon_Coins"), "The gold did not come up into the one row.");
        }
    }
}
