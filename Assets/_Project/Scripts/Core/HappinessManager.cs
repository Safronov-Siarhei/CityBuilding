using System;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Core
{
    /// <summary>
    /// Composite settlement happiness (Довольство), 0-100. Six of the design backlog's factors are
    /// wired up: tax rate, building decay, defence coverage, food variety, recent deaths and
    /// entertainment. The work week is the one still deferred, for want of a week. The score is a
    /// flat average of whatever factors exist, so adding the seventh extends the list instead of
    /// redesigning the aggregation.
    ///
    /// What reads it: MigrationManager, and so far only MigrationManager. Contentment above its
    /// threshold brings settlers in and the happier the town the sooner; below it, people leave.
    /// That is the first and currently the only consequence the score has -- the design backlog
    /// also wants it touching production, which is still unbuilt.
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

        /// <summary>Factor 1: how many kinds of food actually reached the table on the last day the settlement ate.</summary>
        public int FoodScore { get; private set; } = 100;

        /// <summary>Factor 2: citizens lost over the last few days -- starvation today, other causes as they arrive.</summary>
        public int DeathScore { get; private set; } = 100;

        /// <summary>Factor 3: somewhere to go. Every entertainment building standing in the settlement adds its own value (see BuildingData.happiness), against a target that grows with the population.</summary>
        public int EntertainmentScore { get; private set; } = 100;

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
            // Both this and the meal hang off OnDayPassed, and which of the two runs first depends
            // on the order two Start() methods happened to subscribe in. Listening to the meal
            // itself makes that ordering stop mattering: whichever way round the day ticks, the
            // score is recomputed once more with the food numbers the day actually produced.
            if (FoodConsumptionManager.Instance != null) FoodConsumptionManager.Instance.OnFoodConsumed += Recompute;
            // Three of the six factors are per head -- defence, entertainment and the losses the
            // town remembers. Until migration existed the population only moved when the player
            // built something, so a daily recompute was close enough; now it drifts on its own
            // between arrivals, and a stale score would be one MigrationManager then acts on.
            // This is also what makes the loop self-correcting rather than a spiral: as a
            // miserable town loses people, its defence and entertainment per citizen rise.
            if (CitizenManager.Instance != null) CitizenManager.Instance.OnPopulationChanged += Recompute;
            Recompute();
        }

        private void OnDestroy()
        {
            if (GameCalendar.Instance != null) GameCalendar.Instance.OnDayPassed -= Recompute;
            if (TaxManager.Instance != null) TaxManager.Instance.OnTaxRateChanged -= Recompute;
            if (FoodConsumptionManager.Instance != null) FoodConsumptionManager.Instance.OnFoodConsumed -= Recompute;
            if (CitizenManager.Instance != null) CitizenManager.Instance.OnPopulationChanged -= Recompute;
        }

        public void Recompute()
        {
            TaxScore = TaxManager.Instance != null ? ComputeTaxScore(TaxManager.Instance.TaxRatePercent) : 100;
            ComputeBuildingScores(out var decayScore, out var defenseScore, out var entertainmentScore);
            DecayScore = decayScore;
            DefenseScore = defenseScore;
            EntertainmentScore = entertainmentScore;

            var food = FoodConsumptionManager.Instance;
            FoodScore = ComputeFoodScore(food != null ? food.LastVariety : 0, food != null ? food.HungryDaysInARow : 0);
            DeathScore = ComputeDeathScore(food != null ? food.RecentDeaths : 0);

            HappinessPercent = Mathf.RoundToInt((TaxScore + DecayScore + DefenseScore + FoodScore + DeathScore + EntertainmentScore) / 6f);
            UpdateCriticalState();
            OnHappinessChanged?.Invoke();
        }

        /// <summary>Pure formula extracted so it's covered by an EditMode test without needing a live TaxManager.</summary>
        public static int ComputeTaxScore(int taxRatePercent) => Mathf.Clamp(100 - taxRatePercent, 0, 100);

        /// <summary>Pure formula extracted so it's covered by an EditMode test without needing a live scene.</summary>
        public static int ComputeDecayScore(float totalDecay, int buildingsCounted)
        {
            return buildingsCounted <= 0 ? 100 : Mathf.RoundToInt(100f * (1f - totalDecay / buildingsCounted));
        }

        /// <summary>
        /// Pure formula extracted so it's covered by an EditMode test without needing a live scene.
        /// totalDefense must already be the flat (not level-scaled) sum -- see the comment in
        /// ComputeBuildingScores on why Level must not multiply in here.
        /// </summary>
        public static int ComputeDefenseScore(int totalDefense, int population)
        {
            if (population <= 0) return 100;

            var target = population * DefensePerCitizenTarget;
            return target <= 0f ? 100 : Mathf.Clamp(Mathf.RoundToInt(100f * totalDefense / target), 0, 100);
        }

        /// <summary>
        /// Factor 1 of the design's happiness model: variety of food eaten, against the sheet's
        /// target for a full table. A hungry settlement scores zero outright rather than being
        /// graded on variety -- going short is not a subtler version of eating a dull diet, and a
        /// town eating one kind of bread badly should not out-score a town eating two kinds well.
        ///
        /// Pure so an EditMode test covers it without a live scene.
        /// </summary>
        public static int ComputeFoodScore(int varietyEaten, int hungryDaysInARow)
        {
            if (hungryDaysInARow > 0) return 0;

            var target = BalanceConfig.Instance.FoodVarietyTarget;
            if (target <= 0) return 100;

            return Mathf.Clamp(Mathf.RoundToInt(100f * varietyEaten / target), 0, 100);
        }

        /// <summary>
        /// Factor 3 of the design's model: entertainment. Every building that offers the settlement
        /// something to do adds its value, and what counts as enough grows with the town -- one
        /// tavern delights a hamlet and is nothing to a city.
        ///
        /// Settlement-wide, with no radius: the design says each building "contributes its own
        /// happiness value", and a distance rule would be a mechanic the player was never told
        /// about -- plus a per-citizen proximity scan on a phone, for a stat recomputed once a day.
        ///
        /// Pure, so an EditMode test covers it without a live scene.
        /// </summary>
        public static int ComputeEntertainmentScore(int totalHappiness, int population)
        {
            if (population <= 0) return 100;

            var target = population * BalanceConfig.Instance.HappinessPerCitizenTarget;
            return target <= 0f ? 100 : Mathf.Clamp(Mathf.RoundToInt(100f * totalHappiness / target), 0, 100);
        }

        /// <summary>
        /// Factor 2: recent deaths. Full marks for a settlement that has buried nobody, falling by
        /// the sheet's penalty for each citizen lost inside the remembered window. Pure, as above.
        /// </summary>
        public static int ComputeDeathScore(int recentDeaths)
        {
            if (recentDeaths <= 0) return 100;

            return Mathf.Clamp(100 - recentDeaths * BalanceConfig.Instance.HappinessPenaltyPerDeath, 0, 100);
        }

        private void UpdateCriticalState()
        {
            if (!_isCritical && HappinessPercent <= CriticalThreshold)
            {
                _isCritical = true;
                EventLogManager.Instance?.Log(Localization.Format("#log_happiness_low", HappinessPercent));
            }
            else if (_isCritical && HappinessPercent >= RecoveredThreshold)
            {
                _isCritical = false;
                EventLogManager.Instance?.Log(Localization.Format("#log_happiness_recovered", HappinessPercent));
            }
        }

        /// <summary>Single pass over every BuildingInstance for all three building-derived scores --
        /// decay, defence and entertainment each need to scan the same building list, so this avoids
        /// three separate FindObjectsByType calls per Recompute.</summary>
        private static void ComputeBuildingScores(out int decayScore, out int defenseScore, out int entertainmentScore)
        {
            var totalDecay = 0f;
            var decayCounted = 0;
            var totalDefense = 0;
            var totalHappiness = 0;

            foreach (var instance in FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
            {
                if (instance.DecaysOverTime)
                {
                    totalDecay += instance.Decay;
                    decayCounted++;
                }

                // Defence is authored per upgrade level in the balance sheet, and BuildingInstance
                // already resolves it for the level a building actually stands at -- so this reads
                // it straight and must NOT scale by Level on top, which would count the upgrade twice.
                totalDefense += instance.Defense;

                // Read at the building's current level, like defence above and for the same reason:
                // BuildingInstance has already resolved the level, so scaling by Level here would
                // count the upgrade twice.
                totalHappiness += instance.Happiness;
            }

            decayScore = ComputeDecayScore(totalDecay, decayCounted);

            var population = CitizenManager.Instance != null ? CitizenManager.Instance.TotalPopulation : 0;
            defenseScore = ComputeDefenseScore(totalDefense, population);
            entertainmentScore = ComputeEntertainmentScore(totalHappiness, population);
        }
    }
}
