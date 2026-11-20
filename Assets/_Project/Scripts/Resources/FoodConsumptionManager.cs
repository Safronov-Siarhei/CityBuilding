using System;
using System.Collections.Generic;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Resources
{
    /// <summary>
    /// The settlement eats, once per game day.
    ///
    /// Until this existed, food was a resource that only ever went up: farms filled a store nothing
    /// ever drew from, and the whole bread chain ended in a number on the HUD. Feeding the town is
    /// what turns food production into a reason to build anything.
    ///
    /// Soldiers eat too, on top of citizens -- the design says so explicitly, and it is what makes
    /// a standing army cost more than the coins in ArmyManager's upkeep.
    ///
    /// Running out is not instantly fatal. A settlement that misses a meal goes hungry, and only
    /// starts losing people after HungryDaysBeforeDeaths days short in a row: one bad harvest
    /// should be a warning the player can act on, not a population wipe they watch happen.
    /// </summary>
    public class FoodConsumptionManager : MonoBehaviour
    {
        public static FoodConsumptionManager Instance { get; private set; }

        [SerializeField] private CitizenManager citizenManager;

        /// <summary>What the event feed shows for food, and where it sends the camera: the store the food comes out of.</summary>
        private const string FoodPicture = "Res_Food";
        private const string GranaryBuildingName = "Granary";

        /// <summary>How much the settlement needed on the last day it ate, and how much of that it found.</summary>
        public int LastDemand { get; private set; }
        public int LastEaten { get; private set; }

        /// <summary>How many distinct edible resources went into the settlement's mouths on the last day -- the raw input to the happiness variety score.</summary>
        public int LastVariety { get; private set; }

        /// <summary>Days short of food in a row. Reset by a single day of eating properly.</summary>
        public int HungryDaysInARow { get; private set; }

        /// <summary>Citizens lost to starvation on each of the last few days, newest last. Drives the happiness "recent deaths" factor.</summary>
        private readonly Queue<int> _recentDeaths = new Queue<int>();

        // Starving people leave ONE AT A TIME, one every starve_leave_interval_sec (the user,
        // 2026-11-10: they all vanished at once), while the town is past its hungry grace and has
        // no food in store; how many left today goes into the death register at the next meal.
        private float _leaveTimer;
        private int _leftToday;

        /// <summary>Past the hungry grace with an empty store: people are leaving. The HUD's food chip warns.</summary>
        public bool Starving
        {
            get
            {
                var config = BalanceConfig.Instance;
                var resources = ResourceManager.Instance;
                if (config == null || resources == null || HungryDaysInARow < config.HungryDaysBeforeDeaths) return false;
                for (var i = 0; i < ResourceDiet.Edible.Count; i++)
                    if (resources.GetAmount(ResourceDiet.Edible[i]) > 0) return false;
                return true;
            }
        }

        /// <summary>Short of food at the last meal: the HUD's food chip warns from the first hungry day.</summary>
        public bool Hungry => HungryDaysInARow > 0;

        // Meals spread over the day (the user, 2026-11-20: the whole day's food vanished at once at the day's end,
        // which looked strange and could ruin a player): one ration at a time as the day goes; the day's end eats
        // only what the day still owes and settles hunger on the day's total, as before.
        private float _owed;
        private int _demandedToday;
        private int _eatenToday;
        private int _varietyMask;

        private void TickMeals()
        {
            var calendar = GameCalendar.Instance;
            var resources = ResourceManager.Instance;
            if (calendar == null || resources == null || calendar.DayLengthSeconds <= 0f) return;
            var mouths = Mouths;
            if (mouths <= 0) return;
            _owed += mouths * BalanceConfig.Instance.FoodPerMouthPerDay * Time.deltaTime / calendar.DayLengthSeconds;
            // never more than the day's whole demand before the day's end settles it
            var cap = ComputeDemand(mouths);
            while (_owed >= 1f && _demandedToday < cap)
            {
                _owed -= 1f;
                _demandedToday++;
                _eatenToday += EatRations(resources, 1);
            }
        }

        /// <summary>Eats up to <paramref name="rations"/> from the stores (Distribute picks the kinds); returns how many were found.</summary>
        private int EatRations(ResourceManager resources, int rations)
        {
            if (rations <= 0) return 0;
            var stocks = new int[ResourceDiet.Edible.Count];
            for (var i = 0; i < stocks.Length; i++) stocks[i] = resources.GetAmount(ResourceDiet.Edible[i]);
            var eaten = Distribute(rations, stocks);
            var total = 0;
            for (var i = 0; i < eaten.Length; i++)
            {
                if (eaten[i] <= 0) continue;
                resources.Add(ResourceDiet.Edible[i], -eaten[i]);
                total += eaten[i];
                _varietyMask |= 1 << i;
            }
            return total;
        }

        private void Update()
        {
            TickMeals();
            if (!Starving || citizenManager == null)
            {
                _leaveTimer = 0f;
                return;
            }
            _leaveTimer += Time.deltaTime;
            if (_leaveTimer < BalanceConfig.Instance.StarveLeaveIntervalSeconds) return;
            _leaveTimer = 0f;
            if (citizenManager.KillCitizens(1) <= 0) return;
            _leftToday++;
            EventLogManager.Instance?.Post(GameEvent.ForSettlement(Localization.Format("#log_starved", 1), EventSeverity.Danger, EventGlyph.Skull, FoodPicture, GranaryBuildingName));
        }

        public event Action OnFoodConsumed;

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
            if (GameCalendar.Instance != null) GameCalendar.Instance.OnDayPassed += FeedSettlement;
        }

        private void OnDestroy()
        {
            if (GameCalendar.Instance != null) GameCalendar.Instance.OnDayPassed -= FeedSettlement;
        }

        /// <summary>Citizens plus soldiers -- everyone the settlement has to feed.</summary>
        public int Mouths
        {
            get
            {
                var citizens = citizenManager != null ? citizenManager.TotalPopulation : 0;
                var soldiers = ArmyManager.Instance != null ? ArmyManager.Instance.SoldierCount : 0;
                return citizens + soldiers;
            }
        }

        /// <summary>What the settlement will eat tomorrow, at today's headcount. Shown by the HUD so a shortage is visible before it bites.</summary>
        public int DailyDemand => ComputeDemand(Mouths);

        /// <summary>How many citizens starved over the window the happiness model remembers.</summary>
        public int RecentDeaths
        {
            get
            {
                var total = 0;
                foreach (var deaths in _recentDeaths) total += deaths;
                return total;
            }
        }

        /// <summary>Starvation deaths per day over the window happiness remembers, oldest first -- read by the save, which has to carry the penalty across a reload rather than pardon it.</summary>
        public IEnumerable<int> RecentDeathsPerDay => _recentDeaths;

        /// <summary>
        /// Puts a loaded settlement back on the hunger it was saved with. Without this a reload was
        /// a pardon: the streak restarted at zero, so a town one day from its first deaths got its
        /// whole grace period back, and the people it had already buried stopped counting against
        /// its happiness.
        /// </summary>
        public void RestoreFromSave(int hungryDaysInARow, IEnumerable<int> recentDeaths)
        {
            HungryDaysInARow = Mathf.Max(0, hungryDaysInARow);

            _recentDeaths.Clear();
            if (recentDeaths == null) return;

            foreach (var deaths in recentDeaths)
            {
                _recentDeaths.Enqueue(Mathf.Max(0, deaths));
            }

            // A save written when the sheet allowed a longer memory than it does now would
            // otherwise keep counting days the model no longer looks at.
            while (_recentDeaths.Count > BalanceConfig.Instance.DeathsMemoryDays) _recentDeaths.Dequeue();
        }

        /// <summary>
        /// One day's meal. Public so a test can pass a day directly rather than waiting out the
        /// calendar -- the same reason ArmyManager.ChargeDailyUpkeep is public.
        /// </summary>
        public void FeedSettlement()
        {
            var resources = ResourceManager.Instance;
            if (resources == null) return;

            // what the day still owes (all of it when no time ran, as in the tests and the cheat's day-end)
            var rest = Mathf.Max(0, ComputeDemand(Mouths) - _demandedToday);
            var eatenRest = EatRations(resources, rest);
            LastDemand = _demandedToday + rest;
            LastEaten = _eatenToday + eatenRest;
            LastVariety = 0;
            for (var m = _varietyMask; m != 0; m &= m - 1) LastVariety++;
            _demandedToday = 0;
            _eatenToday = 0;
            _varietyMask = 0;
            _owed = 0f;

            ResolveShortfall(LastDemand - LastEaten);
            // yesterday's leavers (Update) into the register the happiness model reads
            RememberDeaths(_leftToday);
            _leftToday = 0;
            OnFoodConsumed?.Invoke();
        }

        /// <summary>
        /// Turns a day's shortfall into however many citizens are lost, and reports it. Returns the
        /// number who died so the happiness window can remember it.
        /// </summary>
        private int ResolveShortfall(int shortfall)
        {
            if (shortfall <= 0)
            {
                if (HungryDaysInARow > 0) EventLogManager.Instance?.Post(GameEvent.ForSettlement(Localization.Get("#log_fed"), EventSeverity.Good, EventGlyph.Check, FoodPicture, GranaryBuildingName));
                HungryDaysInARow = 0;
                return 0;
            }

            HungryDaysInARow++;

            var config = BalanceConfig.Instance;
            if (HungryDaysInARow < config.HungryDaysBeforeDeaths)
            {
                EventLogManager.Instance?.Post(GameEvent.ForSettlement(Localization.Format("#log_hungry", shortfall), EventSeverity.Warning, EventGlyph.Alert, FoodPicture, GranaryBuildingName));
                return 0;
            }

            // past the grace: no one dies at the meal any more -- they leave one by one (Update)
            EventLogManager.Instance?.Post(GameEvent.ForSettlement(Localization.Format("#log_hungry", shortfall), EventSeverity.Danger, EventGlyph.Alert, FoodPicture, GranaryBuildingName));
            return 0;
        }

        /// <summary>
        /// Records deaths this class did not cause. Illness kills too (see SicknessManager), and
        /// the happiness model has exactly one window of recent dead -- so this class has quietly
        /// become the settlement's death register rather than starvation's.
        ///
        /// The save field beside it is still called recentStarvationDeaths, and stays that way on
        /// purpose: renaming it would drop the value out of every save already written.
        /// </summary>
        public void RecordDeaths(int deaths)
        {
            if (deaths > 0) RememberDeaths(deaths);
        }

        private void RememberDeaths(int deaths)
        {
            _recentDeaths.Enqueue(deaths);
            while (_recentDeaths.Count > BalanceConfig.Instance.DeathsMemoryDays) _recentDeaths.Dequeue();
        }

        /// <summary>Pure: what a headcount eats in a day. Rounded up, so the last few citizens of a dying town still need feeding rather than becoming free to keep.</summary>
        public static int ComputeDemand(int mouths)
        {
            if (mouths <= 0) return 0;
            return Mathf.CeilToInt(mouths * BalanceConfig.Instance.FoodPerMouthPerDay);
        }

        /// <summary>Pure: how many citizens a day's shortfall kills once the settlement is past its grace days -- one death per ration missing, so the loss is proportional to how badly short it was.</summary>
        public static int ComputeStarvationDeaths(int shortfall)
        {
            if (shortfall <= 0) return 0;

            var perMouth = BalanceConfig.Instance.FoodPerMouthPerDay;
            return perMouth <= 0f ? 0 : Mathf.Max(1, Mathf.FloorToInt(shortfall / perMouth));
        }

        /// <summary>
        /// Serves `demand` out of the stocks, one unit at a time, round-robin. Pure and static so
        /// the rule is testable without a scene.
        ///
        /// Round-robin rather than draining the largest store first, because what the settlement
        /// ate is also what its happiness variety score is read from: eating a little of everything
        /// is both what a town actually does and what makes stocking two kinds of food worth the
        /// trouble. Cost is one pass per unit of demand, once per game day, over a table of two.
        /// </summary>
        public static int[] Distribute(int demand, int[] stocks)
        {
            var eaten = new int[stocks.Length];
            if (demand <= 0) return eaten;

            var served = 0;
            var servedThisRound = -1;

            while (served < demand && servedThisRound != 0)
            {
                servedThisRound = 0;
                for (var i = 0; i < stocks.Length && served < demand; i++)
                {
                    if (eaten[i] >= stocks[i]) continue;

                    eaten[i]++;
                    served++;
                    servedThisRound++;
                }
            }

            return eaten;
        }
    }
}
