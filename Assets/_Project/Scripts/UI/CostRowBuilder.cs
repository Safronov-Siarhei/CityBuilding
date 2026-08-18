using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// Builds the icon+number chips that show what an action costs. Shared because three cards now
    /// show prices -- a building's own info panel, the Barracks' recruitment block and the
    /// Laboratory's window -- and a cost has to look the same in all of them.
    ///
    /// Runs at play time rather than being laid out in SetupProject: which resources a cost mentions
    /// is a property of the balance sheet and of the action, not of the scene. The row's own
    /// HorizontalLayoutGroup handles spacing; this only adds and removes the leaf children.
    /// </summary>
    public static class CostRowBuilder
    {
        public static void Build(Transform row, List<ResourceAmount> cost, ResourceIconLibrary icons)
        {
            if (row == null) return;

            for (var i = row.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(row.GetChild(i).gameObject);
            }
            if (cost == null) return;

            if (cost.Count == 0)
            {
                CreateChipText(row, Localization.Get("#ui_free"));
                return;
            }

            foreach (var amount in cost)
            {
                CreateChipIcon(row, icons != null ? icons.GetIcon(amount.type) : null);
                CreateChipText(row, amount.amount.ToString());
            }
        }

        private static void CreateChipIcon(Transform parent, Sprite sprite)
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(28f, 28f);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
        }

        private static void CreateChipText(Transform parent, string content)
        {
            var go = new GameObject("Amount", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(70f, 32f);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
        }
    }
}
