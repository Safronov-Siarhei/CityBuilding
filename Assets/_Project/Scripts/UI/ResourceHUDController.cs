using System;
using CityBuilder.Citizens;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The always-on resource bar.
    ///
    /// It shows only the resources the settlement has actually met. A fixed chip per ResourceType
    /// worked while there were six of them; at fifteen -- three ores, three bars, four foods -- a
    /// full row is unreadable, and most of it is zeroes for things the player has not built the
    /// mine for yet. So a chip appears the first time its resource does, and then stays for the
    /// rest of the session even if the stock falls back to zero: a number that vanishes when you
    /// spend it all is worse than a zero, because the player cannot tell empty from missing.
    /// </summary>
    public class ResourceHUDController : MonoBehaviour
    {
        // Parallel arrays wired by SetupProject.BuildResourceHUD: resourceOrder[i] is drawn by
        // icons[i] and amountTexts[i]. Positions are decided here rather than at build time,
        // because which chips are on screen changes as the settlement grows.
        [SerializeField] private ResourceType[] resourceOrder;
        [SerializeField] private RectTransform[] icons;
        [SerializeField] private Text[] amountTexts;
        [SerializeField] private RectTransform populationIcon;
        [SerializeField] private Text populationText;
        [SerializeField] private float barWidth = 1800f;

        // Optional pill behind each chip (the skin's resource chip). With them the chips are
        // packed from the left at their own width, like a row of tokens, instead of being spread
        // across the whole bar -- a settlement with three resources should not have three chips
        // half a screen apart. Index-aligned with resourceOrder.
        [SerializeField] private RectTransform[] chipBackgrounds;
        [SerializeField] private RectTransform populationBackground;
        [SerializeField] private float chipWidth = 190f;
        [SerializeField] private float chipGap = 10f;

        /// <summary>Which resources have ever been in the settlement's hands. Index-aligned with resourceOrder.</summary>
        private bool[] _seen;

        private void Start()
        {
            _seen = new bool[resourceOrder != null ? resourceOrder.Length : 0];

            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnResourceChanged += HandleResourceChanged;
                ResourceManager.Instance.OnInfiniteResourcesChanged += HandleInfiniteResourcesChanged;
                ResourceManager.Instance.OnCapacityChanged += Refresh;
            }
            if (CitizenManager.Instance != null) CitizenManager.Instance.OnPopulationChanged += Refresh;
            Refresh();
        }

        /// <summary>A rotation, a resized window or a fold changes the row's width: lay the chips out again.</summary>
        private void OnRectTransformDimensionsChange()
        {
            if (_seen != null) Refresh();
        }

        private void HandleResourceChanged(ResourceType type, int amount)
        {
            Refresh();
        }

        private void HandleInfiniteResourcesChanged(bool enabled)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (ResourceManager.Instance == null || resourceOrder == null || amountTexts == null) return;

            // The row stretches across the screen, so its width is the screen's, not a constant.
            var rowWidth = ((RectTransform)transform).rect.width;
            if (rowWidth > 0f) barWidth = rowWidth;

            var resources = ResourceManager.Instance;
            var infinite = resources.InfiniteResources;
            var count = Math.Min(resourceOrder.Length, amountTexts.Length);

            // Which chips belong on screen, and how many -- the slot width depends on the count,
            // so this has to be known before anything is positioned.
            var visible = 0;
            for (var i = 0; i < count; i++)
            {
                if (resources.GetAmount(resourceOrder[i]) > 0) _seen[i] = true;
                if (_seen[i]) visible++;
            }

            var slots = visible + 1; // + the population chip, which is always there
            var packed = HasBackgrounds;
            var slotWidth = packed
                ? Mathf.Min(chipWidth + chipGap, barWidth / slots)
                : barWidth / slots;
            var slot = 0;

            for (var i = 0; i < count; i++)
            {
                var shown = _seen[i];
                if (icons[i] != null) icons[i].gameObject.SetActive(shown);
                if (amountTexts[i] != null) amountTexts[i].gameObject.SetActive(shown);
                var background = Background(i);
                if (background != null) background.gameObject.SetActive(shown);
                if (!shown) continue;

                PlaceChip(icons[i], amountTexts[i], background, slot++, slotWidth);
                if (amountTexts[i] != null)
                {
                    amountTexts[i].text = Format(
                        resources.GetAmount(resourceOrder[i]),
                        resources.GetCapacity(resourceOrder[i]),
                        infinite);
                }
            }

            PlaceChip(populationIcon, populationText, populationBackground, slot, slotWidth);
            if (populationText != null)
            {
                populationText.text = CitizenManager.Instance != null
                    ? $"{CitizenManager.Instance.TotalPopulation}/{CitizenManager.Instance.Capacity} ({CitizenManager.Instance.IdlePopulation})"
                    : "0";
            }
        }

        /// <summary>Icon then number, centred in the slot -- the same proportions the bar was built with, now applied to however many chips are on screen.</summary>
        private void PlaceChip(RectTransform icon, Text amount, RectTransform background, int slot, float slotWidth)
        {
            var center = -barWidth * 0.5f + slotWidth * (slot + 0.5f);

            if (background != null)
            {
                // A pill: the icon sits in its left end, the number fills the rest.
                var width = slotWidth - chipGap;
                background.anchoredPosition = new Vector2(center, 0f);
                background.sizeDelta = new Vector2(width, background.sizeDelta.y);
                var left = center - width * 0.5f;
                if (icon != null) icon.anchoredPosition = new Vector2(left + icon.sizeDelta.x * 0.5f + 6f, 0f);
                if (amount != null)
                {
                    var rect = amount.GetComponent<RectTransform>();
                    var textLeft = left + (icon != null ? icon.sizeDelta.x + 10f : 14f);
                    var textWidth = width - (textLeft - left) - 12f;
                    rect.anchoredPosition = new Vector2(textLeft + textWidth * 0.5f, 0f);
                    rect.sizeDelta = new Vector2(textWidth, rect.sizeDelta.y);
                }
                return;
            }

            if (icon != null) icon.anchoredPosition = new Vector2(center - slotWidth * 0.22f, 0f);
            if (amount != null)
            {
                var rect = amount.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(center + slotWidth * 0.14f, 0f);
                rect.sizeDelta = new Vector2(slotWidth * 0.6f, 50f);
            }
        }

        private bool HasBackgrounds => chipBackgrounds != null && chipBackgrounds.Length > 0;

        private RectTransform Background(int index)
        {
            return chipBackgrounds != null && index < chipBackgrounds.Length ? chipBackgrounds[index] : null;
        }

        /// <summary>
        /// What one resource reads as in the top bar.
        ///
        /// The ceiling is only worth showing once it's close enough to matter -- a permanent
        /// "50/200" on every resource is noise, but a stockpile quietly stuck at its limit with no
        /// explanation is worse. Public and static so the rule itself is covered by an EditMode
        /// test without a canvas.
        /// </summary>
        public static string Format(int amount, int capacity, bool infinite)
        {
            if (infinite) return "∞";
            if (capacity == int.MaxValue) return amount.ToString();

            return (long)amount * CapacityVisibleDenominator >= (long)capacity * CapacityVisibleNumerator
                ? $"{amount}/{capacity}"
                : amount.ToString();
        }

        // Four-fifths full. Compared as integers rather than against amount >= capacity * 0.8f:
        // 0.8f is a hair above four fifths, so a store at exactly 80% -- 80/100, the roundest case
        // there is -- used to keep its ceiling hidden.
        private const int CapacityVisibleNumerator = 4;
        private const int CapacityVisibleDenominator = 5;
    }
}
