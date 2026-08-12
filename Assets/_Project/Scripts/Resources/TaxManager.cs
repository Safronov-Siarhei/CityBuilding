using System;
using CityBuilder.Citizens;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Resources
{
    /// <summary>
    /// Player-adjustable tax rate (0-100%), collected as Coins income once per GameCalendar day,
    /// proportional to population. Coins are a distinct currency from Gold (see the design
    /// backlog): Gold is the raw metal a Mine produces, Coins are spending money -- a lower tax
    /// rate is intended to raise happiness once that system exists; this only owns the economic
    /// side of taxation for now.
    /// </summary>
    public class TaxManager : MonoBehaviour
    {
        public static TaxManager Instance { get; private set; }

        // Coins collected per citizen per day at 100% tax -- first-pass, tunable.
        private const float CoinsPerCitizenPerDayAtMaxTax = 0.5f;
        private const int MaxTaxRatePercent = 100;

        [SerializeField] private CitizenManager citizenManager;

        public int TaxRatePercent { get; private set; } = 10;

        public event Action OnTaxRateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (GameCalendar.Instance != null) GameCalendar.Instance.OnDayPassed += CollectTaxes;
        }

        private void OnDestroy()
        {
            if (GameCalendar.Instance != null) GameCalendar.Instance.OnDayPassed -= CollectTaxes;
        }

        public void SetTaxRate(int percent)
        {
            TaxRatePercent = Mathf.Clamp(percent, 0, MaxTaxRatePercent);
            OnTaxRateChanged?.Invoke();
        }

        private void CollectTaxes()
        {
            var population = citizenManager != null ? citizenManager.TotalPopulation : 0;
            var income = Mathf.RoundToInt(population * (TaxRatePercent / 100f) * CoinsPerCitizenPerDayAtMaxTax);
            if (income > 0) ResourceManager.Instance?.Add(ResourceType.Coins, income);
        }
    }
}
