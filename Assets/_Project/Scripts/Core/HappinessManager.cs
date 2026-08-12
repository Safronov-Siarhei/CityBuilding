using System;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Core
{
    /// <summary>
    /// Composite settlement happiness (Довольство), 0-100. First slice of the happiness model from
    /// the design backlog -- only wires up the factors that already have a real system behind them
    /// (tax rate, building decay, defense coverage). Food diversity, recent deaths, entertainment
    /// buildings and the work week are explicitly deferred until those systems exist; this averages
    /// whatever factor scores are available so adding a fourth/fifth later just extends the list
    /// instead of redesigning the aggregation. Not yet linked to anything downstream (no gameplay
    /// consequence reads HappinessPercent yet) -- this is the stat itself, first.
    /// </summary>
    public class HappinessManager : MonoBehaviour
    {
        public static HappinessManager Instance { get; private set; }

        // First-pass target, tunable: this many total defense points per citizen counts as "fully
        // defended" (100 score); below that the defense score scales down linearly to 0.
        private const float DefensePerCitizenTarget = 0.5f;

        // Hysteresis band for the EventLog crossing notice below -- entering/leaving this band is
        // logged once each way (see UpdateCriticalState), not every recompute, so a score sitting
        // right at the edge doesn't spam the log on every tax-rate tweak or day tick.
        private const int CriticalThreshold = 30;
        private const int RecoveredThreshold = 70;

        public int HappinessPercent { get; private set; } = 100;
        public int TaxScore { get; private set; } = 100;
        public int DecayScore { get; private set; } = 100;
        public int DefenseScore { get; private set; } = 100;

        private bool _isCritical;

        public event Action OnHappinessChanged;

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
            if (GameCalendar.Instance != null) GameCalendar.Instance.OnDayPassed += Recompute;
            if (TaxManager.Instance != null) TaxManager.Instance.OnTaxRateChanged += Recompute;
            Recompute();
        }

        private void OnDestroy()
        {
            if (GameCalendar.Instance != null) GameCalendar.Instance.OnDayPassed -= Recompute;
            if (TaxManager.Instance != null) TaxManager.Instance.OnTaxRateChanged -= Recompute;
        }

        public void Recompute()
        {
            TaxScore = TaxManager.Instance != null ? Mathf.Clamp(100 - TaxManager.Instance.TaxRatePercent, 0, 100) : 100;
            DecayScore = ComputeDecayScore();
            DefenseScore = ComputeDefenseScore();

            HappinessPercent = Mathf.RoundToInt((TaxScore + DecayScore + DefenseScore) / 3f);
            UpdateCriticalState();
            OnHappinessChanged?.Invoke();
        }

        private void UpdateCriticalState()
        {
            if (!_isCritical && HappinessPercent <= CriticalThreshold)
            {
                _isCritical = true;
                EventLogManager.Instance?.Log($"Довольство упало до критического уровня ({HappinessPercent}%)");
            }
            else if (_isCritical && HappinessPercent >= RecoveredThreshold)
            {
                _isCritical = false;
                EventLogManager.Instance?.Log($"Довольство восстановилось ({HappinessPercent}%)");
            }
        }

        private static int ComputeDecayScore()
        {
            var instances = FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None);
            var total = 0f;
            var counted = 0;
            foreach (var instance in instances)
            {
                if (!instance.DecaysOverTime) continue;
                total += instance.Decay;
                counted++;
            }
            return counted == 0 ? 100 : Mathf.RoundToInt(100f * (1f - total / counted));
        }

        private static int ComputeDefenseScore()
        {
            var instances = FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None);
            var totalDefense = 0;
            foreach (var instance in instances)
            {
                if (instance.Data != null) totalDefense += instance.Data.defense * instance.Level;
            }

            var population = CitizenManager.Instance != null ? CitizenManager.Instance.TotalPopulation : 0;
            if (population <= 0) return 100;

            var target = population * DefensePerCitizenTarget;
            return target <= 0f ? 100 : Mathf.Clamp(Mathf.RoundToInt(100f * totalDefense / target), 0, 100);
        }
    }
}
