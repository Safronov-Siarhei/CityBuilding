using System;
using CityBuilder.Citizens;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    public class ResourceHUDController : MonoBehaviour
    {
        // Parallel arrays wired by SetupProject.BuildResourceHUD -- resourceOrder[i]'s amount is
        // shown by amountTexts[i], next to that resource's icon. Icons replace the old plain-text
        // resource names ("Дерево 50   Камень 20 ...") so the always-on top bar reads visually.
        [SerializeField] private ResourceType[] resourceOrder;
        [SerializeField] private Text[] amountTexts;
        [SerializeField] private Text populationText;

        private void Start()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnResourceChanged += HandleResourceChanged;
                ResourceManager.Instance.OnInfiniteResourcesChanged += HandleInfiniteResourcesChanged;
                ResourceManager.Instance.OnCapacityChanged += Refresh;
            }
            if (CitizenManager.Instance != null) CitizenManager.Instance.OnPopulationChanged += Refresh;
            Refresh();
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

            var infinite = ResourceManager.Instance.InfiniteResources;
            var count = Math.Min(resourceOrder.Length, amountTexts.Length);
            for (var i = 0; i < count; i++)
            {
                if (amountTexts[i] == null) continue;

                var type = resourceOrder[i];
                amountTexts[i].text = Format(
                    ResourceManager.Instance.GetAmount(type),
                    ResourceManager.Instance.GetCapacity(type),
                    infinite);
            }

            if (populationText != null)
            {
                populationText.text = CitizenManager.Instance != null
                    ? $"{CitizenManager.Instance.TotalPopulation} ({CitizenManager.Instance.IdlePopulation})"
                    : "0";
            }
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
