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
        /// <summary>
        /// The free hands, a small disc on the people's icon -- shown only when there are any (the
        /// user, 2026-10-28: «18/18 (18)» did not fit the pill and broke onto two lines).
        /// </summary>
        [SerializeField] private GameObject idleBadge;
        [SerializeField] private Text idleText;
        [SerializeField] private float barWidth = 1800f;

        // Optional pill behind each chip (the skin's resource chip). With them the chips are
        // packed from the left at their own width, like a row of tokens, instead of being spread
        // across the whole bar -- a settlement with three resources should not have three chips
        // half a screen apart. Index-aligned with resourceOrder.
        [SerializeField] private RectTransform[] chipBackgrounds;
        [SerializeField] private RectTransform populationBackground;
        [SerializeField] private float chipWidth = 190f;
        [SerializeField] private float chipGap = 10f;

        // The one plaque the chips sit on (the user's layout, 2026-10-14: one resource bar along
        // the top). Sized to the chips, not to the row, and the chips centred on it -- a young
        // settlement's three resources make a short bar in the middle, not three tokens at the
        // left end of a long empty one.
        [SerializeField] private RectTransform barBackground;
        [SerializeField] private float barPadding = 10f;

        // Where the packed chips start, measured from the row's left end; recomputed each layout.
        private float _packOffset;

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
            // still subscribed while the scene unloads round it
            if (this == null) return;
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
            var run = Invasion.InvasionMode.IsActive;
            for (var i = 0; i < count; i++)
            {
                if (resources.GetAmount(resourceOrder[i]) > 0) _seen[i] = true;
                // a run has wood, stone and gold (and people) and nothing else to show
                if (run && !Invasion.InvasionRules.IsRunResource(resourceOrder[i])) _seen[i] = false;
                if (_seen[i]) visible++;
            }

            var slots = visible + 1; // + the population chip, which is always there
            var packed = HasBackgrounds;
            var slotWidth = packed
                ? Mathf.Min(chipWidth + chipGap, barWidth / slots)
                : barWidth / slots;
            var span = slotWidth * slots;
            _packOffset = packed ? (barWidth - span) * 0.5f : 0f;
            if (barBackground != null)
            {
                barBackground.anchoredPosition = Vector2.zero;
                barBackground.sizeDelta = new Vector2(Mathf.Min(barWidth, span - chipGap + barPadding * 2f), barBackground.sizeDelta.y);
            }
            var slot = 0;

            for (var i = 0; i < count; i++)
            {
                var shown = _seen[i];
                if (icons[i] != null) icons[i].gameObject.SetActive(shown);
                if (amountTexts[i] != null) amountTexts[i].gameObject.SetActive(shown);
                var background = Background(i);
                if (background != null) background.gameObject.SetActive(shown);
                if (!shown) continue;

                // the text first: the chip centres the icon and the number together by its width
                if (amountTexts[i] != null)
                {
                    amountTexts[i].text = Format(
                        resources.GetAmount(resourceOrder[i]),
                        resources.GetCapacity(resourceOrder[i]),
                        infinite);
                }
                PlaceChip(icons[i], amountTexts[i], background, slot++, slotWidth);
            }

            if (populationText != null)
            {
                var people = CitizenManager.Instance;
                populationText.text = people != null ? $"{people.TotalPopulation}/{people.Capacity}" : "0";
                var idle = people != null ? people.IdlePopulation : 0;
                if (idleBadge != null && idleBadge.activeSelf != idle > 0) idleBadge.SetActive(idle > 0);
                if (idleText != null && idle > 0)
                {
                    idleText.text = idle.ToString();
                    idleText.color = Color.white;   // the HUD's theme pass inks every label; this one sits on green
                }
            }
            PlaceChip(populationIcon, populationText, populationBackground, slot, slotWidth);
        }

        /// <summary>Icon then number, centred in the slot -- the same proportions the bar was built with, now applied to however many chips are on screen.</summary>
        private const float IconTextGap = 8f;
        private const float PillInset = 10f;

        private void PlaceChip(RectTransform icon, Text amount, RectTransform background, int slot, float slotWidth)
        {
            var center = -barWidth * 0.5f + _packOffset + slotWidth * (slot + 0.5f);

            if (background != null)
            {
                // A pill: the icon and the number together in its middle (the user, 2026-10-25: they
                // stood at the left end with the right half empty); a long number takes the pill's width.
                var width = slotWidth - chipGap;
                background.anchoredPosition = new Vector2(center, 0f);
                background.sizeDelta = new Vector2(width, background.sizeDelta.y);
                var iconWidth = icon != null ? icon.sizeDelta.x + IconTextGap : 0f;
                var maxText = width - iconWidth - 2f * PillInset;
                var textWidth = amount != null ? Mathf.Min(maxText, amount.preferredWidth + 2f) : 0f;
                var groupLeft = center - (iconWidth + textWidth) * 0.5f;
                if (icon != null) icon.anchoredPosition = new Vector2(groupLeft + icon.sizeDelta.x * 0.5f, 0f);
                if (amount != null)
                {
                    var rect = amount.GetComponent<RectTransform>();
                    rect.anchoredPosition = new Vector2(groupLeft + iconWidth + textWidth * 0.5f, 0f);
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
