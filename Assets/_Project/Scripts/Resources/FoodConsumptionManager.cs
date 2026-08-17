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

        /// <summary>How much the settlement needed on the last day it ate, and how much of that it found.</summary>
        public int LastDemand { get; private set; }
        public int LastEaten { get; private set; }

        /// <summary>How many distinct edible resources went into the settlement's mouths on the last day -- the raw input to the happiness variety score.</summary>
        public int LastVariety { get; private set; }

        /// <summary>Days short of food in a row. Reset by a single day of eating properly.</summary>
        public int HungryDaysInARow { get; private set; }

        /// <summary>Citizens lost to starvation on each of the last few days, newest last. Drives the happiness "recent deaths" factor.</summary>
        private readonly Queue<int> _recentDeaths = new Queue<int>();

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

        /// <summary>
        /// One day's meal. Public so a test can pass a day directly rather than waiting out the
        /// calendar -- the same reason ArmyManager.ChargeDailyUpkeep is public.
        /// </summary>
        public void FeedSettlement()
        {
            var resources = ResourceManager.Instance;
            if (resources == null) return;

            LastDemand = ComputeDemand(Mouths);

            var stocks = new int[ResourceDiet.Edible.Count];
            for (var i = 0; i < stocks.Length; i++)
            {
                stocks[i] = resources.GetAmount(ResourceDiet.Edible[i]);
            }

            var eaten = Distribute(LastDemand, stocks);

            LastEaten = 0;
            LastVariety = 0;
            for (var i = 0; i < eaten.Length; i++)
            {
                if (eaten[i] <= 0) continue;

                resources.Add(ResourceDiet.Edible[i], -eaten[i]);
                LastEaten += eaten[i];
                LastVariety++;
            }

            RememberDeaths(ResolveShortfall(LastDemand - LastEaten));
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
                if (HungryDaysInARow > 0) EventLogManager.Instance?.Log("Поселение снова сыто.");
                HungryDaysInARow = 0;
                return 0;
            }

            HungryDaysInARow++;

            var config = BalanceConfig.Instance;
            if (HungryDaysInARow < config.HungryDaysBeforeDeaths)
            {
                EventLogManager.Instance?.Log($"Голод: не хватает еды ({shortfall}). Люди начнут умирать, если не накормить.");
                return 0;
            }

            if (citizenManager == null) return 0;

            var deaths = citizenManager.KillCitizens(ComputeStarvationDeaths(shortfall));
            if (deaths <= 0) return 0;

            EventLogManager.Instance?.Log($"Голод: умерло жителей — {deaths}");
            return deaths;
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
