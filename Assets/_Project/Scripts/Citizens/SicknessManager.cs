using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Citizens
{
    /// <summary>
    /// The settlement falls ill, once per game day.
    ///
    /// Illness is what finally gives the Дом лекаря and the Колодец something to do -- two of the
    /// buildings that had stood in the hotbar doing nothing at all. The user's design (2026-08-19):
    /// people fall ill from HUNGER and from living out of reach of a well, a sick citizen stops
    /// working, and without treatment they die.
    ///
    /// Modelled as a headcount, like everything else about population (see CitizenManager): there
    /// are no individual citizens to be ill, and inventing them for this would be a rewrite that
    /// bought nothing a count does not already say.
    ///
    /// Deterministic rather than a per-citizen roll. The rest of this game's daily arithmetic --
    /// what a town eats, what it earns, who moves in -- is worked out rather than diced for, and a
    /// player who can see WHY their people are ill can do something about it. The randomness in
    /// fires is deliberate and different: a fire is an event, an epidemic is a condition.
    /// </summary>
    public class SicknessManager : MonoBehaviour
    {
        public static SicknessManager Instance { get; private set; }

        [SerializeField] private CitizenManager citizenManager;

        /// <summary>The id of the building that waters a settlement, and of the one that treats it. Named here for the same reason BuildingIds names the Town Hall: losing the string means losing the mechanic, silently.</summary>
        private const string WellBuildingName = "Well";
        private const string HealerBuildingName = "HealerHouse";

        /// <summary>How many days in a row have ended with somebody still ill and nobody left to treat them. Reset by a single day that clears the beds.</summary>
        public int UntreatedDaysInARow { get; private set; }

        /// <summary>
        /// The share of the settlement's HOUSING that stands outside every well's reach -- 0 when
        /// every home is covered, 1 when none is. Weighted by housing capacity rather than counted
        /// per building, so leaving one Особняк dry matters more than leaving one Лачуга dry.
        /// </summary>
        public float UnwateredShare { get; private set; }

        /// <summary>How many fell ill and how many were nursed back on the last day, for the event log and the tests.</summary>
        public int LastFellIll { get; private set; }

        public int LastHealed { get; private set; }

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
            if (GameCalendar.Instance != null) GameCalendar.Instance.OnDayPassed += PassDay;
        }

        private void OnDestroy()
        {
            if (GameCalendar.Instance != null) GameCalendar.Instance.OnDayPassed -= PassDay;
        }

        /// <summary>
        /// One day of illness: who falls ill, who is nursed, and who does not survive being
        /// neither. Public so a test can pass a day without waiting two real minutes for the
        /// calendar -- the same door FoodConsumptionManager.FeedSettlement opens.
        /// </summary>
        public void PassDay()
        {
            var citizens = citizenManager != null ? citizenManager : CitizenManager.Instance;
            if (citizens == null) return;

            UnwateredShare = ComputeUnwateredShare();
            FallIll(citizens);
            Heal(citizens);
            KillTheUntreated(citizens);
        }

        /// <summary>Restores what a save was written with. The share of dry housing is deliberately not saved: it is recomputed from the buildings, which have just been put back.</summary>
        public void RestoreFromSave(int untreatedDaysInARow)
        {
            UntreatedDaysInARow = Mathf.Max(0, untreatedDaysInARow);
        }

        /// <summary>
        /// The chance one healthy citizen falls ill today. Pure, and taking its weights explicitly,
        /// so a test can state the shape -- background risk, plus what going hungry adds, plus what
        /// living dry adds -- without depending on today's sheet.
        /// </summary>
        public static float ComputeRisk(bool hungry, float unwateredShare, float baseChance, float hungerChance, float thirstChance)
        {
            var risk = baseChance
                       + (hungry ? hungerChance : 0f)
                       + Mathf.Clamp01(unwateredShare) * thirstChance;

            return Mathf.Clamp01(risk);
        }

        /// <summary>The chance at the sheet's current numbers, against the settlement as it stands today.</summary>
        public float CurrentRisk()
        {
            var config = BalanceConfig.Instance;
            var food = FoodConsumptionManager.Instance;
            var hungry = food != null && food.HungryDaysInARow > 0;

            return ComputeRisk(hungry, UnwateredShare,
                config.SicknessBaseChancePerDay, config.SicknessHungerChancePerDay, config.SicknessThirstChancePerDay);
        }

        private void FallIll(CitizenManager citizens)
        {
            // Of the WELL, not of everyone: someone already in bed cannot take to it twice.
            var exposed = citizens.HealthyPopulation;
            LastFellIll = 0;
            if (exposed <= 0) return;

            var fell = Mathf.RoundToInt(exposed * CurrentRisk());
            if (fell <= 0) return;

            LastFellIll = citizens.AddSick(fell);
            if (LastFellIll <= 0) return;

            EventLogManager.Instance?.Log(Localization.Format("#log_sick", LastFellIll));

            // Said once, and only when thirst is actually part of it: an epidemic in a town with a
            // well is a food problem, and telling the player to dig wells would send them the wrong
            // way entirely.
            if (UnwateredShare > 0f) EventLogManager.Instance?.Log(Localization.Get("#log_no_well"));
        }

        private void Heal(CitizenManager citizens)
        {
            LastHealed = 0;
            if (citizens.SickPopulation <= 0) return;

            var capacity = HealingCapacityPerDay();
            if (capacity <= 0) return;

            LastHealed = citizens.HealSick(capacity);
            if (LastHealed > 0) EventLogManager.Instance?.Log(Localization.Format("#log_healed", LastHealed));
        }

        /// <summary>
        /// How many the settlement can nurse in a day: every scientist-style worker standing in a
        /// Дом лекаря, times what one of them is worth.
        ///
        /// A Дом лекаря has worker slots and no recipe, exactly as the Лаборатория does -- the
        /// building holds the people and a separate system does the work.
        /// </summary>
        public int HealingCapacityPerDay()
        {
            var healers = 0;
            foreach (var instance in Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
            {
                if (instance.Data == null || instance.Data.buildingName != HealerBuildingName) continue;

                var workplace = instance.GetComponent<ProductionBuilding>();
                if (workplace != null) healers += workplace.AssignedWorkers;
            }

            return healers * BalanceConfig.Instance.HealPerHealerPerDay;
        }

        /// <summary>
        /// A day that ends with people still in bed is a day nobody could treat them. Enough of
        /// those in a row and they die.
        ///
        /// The streak counts days the beds were not CLEARED, not days without a healer standing
        /// anywhere -- so one lone Дом лекаря does not make a town of forty immune to plague, which
        /// is what a mere "is there a healer" test would have done.
        /// </summary>
        private void KillTheUntreated(CitizenManager citizens)
        {
            if (citizens.SickPopulation <= 0)
            {
                UntreatedDaysInARow = 0;
                return;
            }

            UntreatedDaysInARow++;
            if (UntreatedDaysInARow < BalanceConfig.Instance.SicknessDaysBeforeDeath) return;

            var dying = citizens.SickPopulation;
            var deaths = citizens.KillCitizens(dying);
            if (deaths <= 0) return;

            // The dead are no longer ill. KillCitizens only clamps the sick count into the
            // surviving population, which for any town bigger than its sickbed leaves them all
            // still counted as ill -- and permanently unemployable.
            citizens.HealSick(deaths);
            UntreatedDaysInARow = 0;

            // Into the same window starvation's dead go into, so illness drags the settlement's
            // mood down exactly as hunger does -- see FoodConsumptionManager.RecordDeaths.
            FoodConsumptionManager.Instance?.RecordDeaths(deaths);
            EventLogManager.Instance?.Log(Localization.Format("#log_plague_deaths", deaths));
        }

        /// <summary>
        /// One pass over the buildings for both halves of the question: how much housing there is,
        /// and how much of it stands inside some well's reach.
        ///
        /// Runs once a game day, so a scan is affordable; and a well's radius is per level, so the
        /// answer changes when one is upgraded without anything having to notice.
        /// </summary>
        private static float ComputeUnwateredShare()
        {
            var wells = new List<(Vector3 position, float radiusSquared)>();
            var houses = new List<(Vector3 position, int capacity)>();

            foreach (var instance in Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
            {
                if (instance.Data == null) continue;

                if (instance.Data.buildingName == WellBuildingName)
                {
                    var radius = instance.ServiceRadius;
                    if (radius > 0) wells.Add((instance.transform.position, radius * (float)radius));
                }

                var capacity = instance.Data.LevelStats(instance.Level).housingCapacity;
                if (capacity > 0) houses.Add((instance.transform.position, capacity));
            }

            var total = 0;
            var dry = 0;
            foreach (var (position, capacity) in houses)
            {
                total += capacity;
                if (!IsWatered(position, wells)) dry += capacity;
            }

            // No housing at all reads as watered, not as parched: a settlement with nowhere to live
            // has no thirsty homes to punish, and dividing by zero would hand it the full penalty.
            return total <= 0 ? 0f : dry / (float)total;
        }

        private static bool IsWatered(Vector3 position, List<(Vector3 position, float radiusSquared)> wells)
        {
            foreach (var (wellPosition, radiusSquared) in wells)
            {
                var offset = wellPosition - position;
                offset.y = 0f;
                if (offset.sqrMagnitude <= radiusSquared) return true;
            }
            return false;
        }
    }
}
