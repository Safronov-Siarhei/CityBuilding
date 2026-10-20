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
    /// <summary>
    /// Paints a price red where the stock falls short, and keeps it current while the card is open
    /// (the user, 2026-10-20, read the Laboratory's upgrade price -- 64 wood beside a stock of 11 --
    /// as a second, contradictory resource count). A few times a second, a handful of chips.
    /// </summary>
    public class CostRowLive : MonoBehaviour
    {
        private readonly List<(ResourceType type, int amount, Text text)> _chips = new List<(ResourceType, int, Text)>();
        private float _refreshIn;

        public void Clear() => _chips.Clear();

        public void Add(ResourceAmount amount, Text text) => _chips.Add((amount.type, amount.amount, text));

        /// <summary>Whether the chip for this resource is painted short -- for the tests.</summary>
        public bool IsShort(ResourceType type)
        {
            foreach (var chip in _chips)
            {
                if (chip.type == type && chip.text != null) return chip.text.color == UiTheme.Danger;
            }
            return false;
        }

        private void Update()
        {
            _refreshIn -= Time.unscaledDeltaTime;
            if (_refreshIn > 0f) return;
            _refreshIn = 0.25f;
            Refresh();
        }

        public void Refresh()
        {
            var resources = ResourceManager.Instance;
            foreach (var chip in _chips)
            {
                if (chip.text == null) continue;
                var short_ = resources != null && !resources.InfiniteResources && resources.GetAmount(chip.type) < chip.amount;
                chip.text.color = short_ ? UiTheme.Danger : UiTheme.Ink;
            }
        }
    }

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

            var live = row.GetComponent<CostRowLive>();
            if (live == null) live = row.gameObject.AddComponent<CostRowLive>();
            live.Clear();
            foreach (var amount in cost)
            {
                CreateChipIcon(row, icons != null ? icons.GetIcon(amount.type) : null);
                live.Add(amount, CreateChipText(row, amount.amount.ToString()));
            }
            live.Refresh();
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

        private static Text CreateChipText(Transform parent, string content)
        {
            var go = new GameObject("Amount", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(70f, 32f);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = UiTheme.Regular;
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = UiTheme.Ink;
            return text;
        }
    }
}
