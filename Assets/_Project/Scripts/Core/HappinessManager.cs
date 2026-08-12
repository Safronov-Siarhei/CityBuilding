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
            ComputeBuildingScores(out var decayScore, out var defenseScore);
            DecayScore = decayScore;
            DefenseScore = defenseScore;

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

        /// <summary>Single pass over every BuildingInstance for both scores -- decay and defense
        /// each need to scan the same building list, so this avoids two separate FindObjectsByType
        /// calls per Recompute.</summary>
        private static void ComputeBuildingScores(out int decayScore, out int defenseScore)
        {
            var totalDecay = 0f;
            var decayCounted = 0;
            var totalDefense = 0;

            foreach (var instance in FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
            {
                if (instance.DecaysOverTime)
                {
                    totalDecay += instance.Decay;
                    decayCounted++;
                }

                // BuildingData.defense is a level-1 base stat -- BuildingInstance.TryUpgrade only
                // advances Level so far, it doesn't scale defense (see BuildingData's "Condition"
                // header and BuildingInfoPanelController, which both treat defense as flat), so
                // this must not multiply by Level either.
                if (instance.Data != null) totalDefense += instance.Data.defense;
            }

            decayScore = decayCounted == 0 ? 100 : Mathf.RoundToInt(100f * (1f - totalDecay / decayCounted));

            var population = CitizenManager.Instance != null ? CitizenManager.Instance.TotalPopulation : 0;
            if (population <= 0)
            {
                defenseScore = 100;
                return;
            }

            var target = population * DefensePerCitizenTarget;
            defenseScore = target <= 0f ? 100 : Mathf.Clamp(Mathf.RoundToInt(100f * totalDefense / target), 0, 100);
        }
    }
}
