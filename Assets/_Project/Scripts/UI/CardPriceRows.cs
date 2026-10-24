using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// A build card's price in one row or two (the user, 2026-10-24): up to two prices stand in one
    /// row, centred between the name and the card's foot; a third goes to a second row below, so
    /// three prices never run off the card. The rows sit in a vertical layout that centres whatever
    /// is showing -- a hidden second row leaves no gap.
    ///
    /// A price is a pair of children, Icon_X and Amount_X; hidden pairs (a resource this price does
    /// not ask for) are ignored. Arrange again after showing or hiding any of them.
    /// </summary>
    public class CardPriceRows : MonoBehaviour
    {
        public const int PerRow = 2;

        [SerializeField] private RectTransform top;
        [SerializeField] private RectTransform bottom;

        public RectTransform Top => top;
        public RectTransform Bottom => bottom;

        public void Wire(RectTransform topRow, RectTransform bottomRow)
        {
            top = topRow;
            bottom = bottomRow;
        }

        public void Arrange()
        {
            if (top == null || bottom == null) return;
            var pairs = new List<(Transform icon, Transform amount)>();
            Collect(top, pairs);
            Collect(bottom, pairs);

            var shown = 0;
            foreach (var (icon, amount) in pairs)
            {
                var row = icon.gameObject.activeSelf && shown++ >= PerRow ? bottom : top;
                icon.SetParent(row, false);
                if (amount != null) amount.SetParent(row, false);
            }
            bottom.gameObject.SetActive(shown > PerRow);
        }

        private static void Collect(Transform row, List<(Transform, Transform)> into)
        {
            for (var i = 0; i < row.childCount; i++)
            {
                var child = row.GetChild(i);
                if (!child.name.StartsWith("Icon_")) continue;
                into.Add((child, row.Find("Amount_" + child.name.Substring("Icon_".Length))));
            }
        }
    }
}
