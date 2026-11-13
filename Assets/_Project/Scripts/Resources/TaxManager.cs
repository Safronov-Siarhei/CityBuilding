using System;
using CityBuilder.Citizens;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Resources
{
    /// <summary>
    /// The town's tax, collected as Coins once per GameCalendar day (the user, 2026-11-02): every
    /// citizen pays coins_per_citizen_per_day, and the Tax House adds tax_house_bonus_per_level for
    /// each of its levels (the best one standing counts). The old 0-100% slider is gone from the HUD
    /// -- with contentment off in the sandbox it only ever cost the player money. The rate is kept
    /// (saved, and contentment's tax score still reads it) but no longer changes the income.
    /// Fractions of a coin carry over to the next day.
    /// </summary>
    public class TaxManager : MonoBehaviour
    {
        public static TaxManager Instance { get; private set; }

        // Coins collected per citizen per day at 100% tax -- first-pass, tunable.
        // From the balance sheet (coins_per_citizen_per_day_at_max_tax).
        private const int MaxTaxRatePercent = 100;

        [SerializeField] private CitizenManager citizenManager;

        public int TaxRatePercent { get; private set; } = 10;

        private float _carry;

        /// <summary>Coins one citizen pays a day now: the base and the Tax House's bonus.</summary>
        public static float PerCitizen => PerCitizenAt(TaxHouseLevel());

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
            // the sandbox's sick pay no tax (the user, 2026-11-13)
            var population = citizenManager == null ? 0
                : Citizens.Infirmary.Active ? citizenManager.HealthyPopulation : citizenManager.TotalPopulation;
            // Казна multiplies the day's tax (the user, 2026-11-12)
            // + the decor's coins to the people living round it (the user, 2026-11-12; DecorBonus)
            var due = (population * PerCitizen + DecorBonus.DailyCoins()) * Shop.Boosters.TaxFactor + _carry;
            var income = Mathf.FloorToInt(due + 0.0001f);
            _carry = Mathf.Max(0f, due - income);
            if (income > 0) ResourceManager.Instance?.Add(ResourceType.Coins, income);
        }

        /// <summary>Coins a citizen pays a day with a Tax House at `taxHouseLevel` (0 = none).</summary>
        public static float PerCitizenAt(int taxHouseLevel)
        {
            var b = BalanceConfig.Instance;
            return b == null ? 1f : b.CoinsPerCitizenPerDay + b.TaxHouseBonusPerLevel * Mathf.Max(0, taxHouseLevel);
        }

        /// <summary>The whole coins `population` pays a day with a Tax House at `taxHouseLevel` (rounded; the game carries the fraction).</summary>
        public static int ComputeDailyIncome(int population, int taxHouseLevel) =>
            Mathf.RoundToInt(population * PerCitizenAt(taxHouseLevel));

        /// <summary>The level of the best finished Tax House standing, 0 for none.</summary>
        public static int TaxHouseLevel()
        {
            var best = 0;
            // once a day: a scan is cheap enough
            foreach (var b in UnityEngine.Object.FindObjectsByType<Buildings.BuildingInstance>(FindObjectsSortMode.None))
            {
                if (b == null || b.Data == null || b.Data.buildingName != Buildings.BuildingIds.TaxHouse || !b.IsOperational) continue;
                if (b.Level > best) best = b.Level;
            }
            return best;
        }
    }
}
