using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Maps;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Invasion
{
    /// <summary>
    /// The people of an Invasion run (the user, 2026-10-21): they live in the six houses that stand
    /// on the island from the start, three to a house, and nowhere else -- no house is ever built
    /// again. People are workers or, hired at the Barracks, soldiers; a soldier who falls is replaced
    /// by his house after a wait (a bar over the house shows it filling), but only while the island
    /// has fewer people than its standing houses have places. A burnt house takes its places with it
    /// for good, so the houses are worth defending.
    ///
    /// People are counted as the town's population plus its soldiers (the King is no one's
    /// householder). Which house makes the next person does not matter to the count; the houses
    /// share the shortfall, each making one at a time.
    /// </summary>
    public class InvasionPeople : MonoBehaviour
    {
        private class House
        {
            public BuildingInstance Building;
            public int Owed;
            public float Timer;
            public HarvestProgressBar Bar;
        }

        private readonly List<House> _houses = new List<House>();

        /// <summary>Places in the standing houses.</summary>
        public int Places => _houses.Count * BalanceConfig.Instance.InvasionPeoplePerHouse;

        /// <summary>Everyone alive: the town's people and its soldiers.</summary>
        public static int People
        {
            get
            {
                var citizens = CitizenManager.Instance != null ? CitizenManager.Instance.TotalPopulation : 0;
                var soldiers = ArmyManager.Instance != null ? ArmyManager.Instance.SoldierCount : 0;
                return citizens + soldiers;
            }
        }

        public int StandingHouses => _houses.Count;

        /// <summary>People the houses are making right now (one at a time each).</summary>
        public int Owed
        {
            get
            {
                var owed = 0;
                foreach (var house in _houses) owed += house.Owed;
                return owed;
            }
        }

        /// <summary>The house's progress towards its next person, 0 when it owes no one -- for the tests and the HUD.</summary>
        public float ProgressOf(BuildingInstance building)
        {
            foreach (var house in _houses)
            {
                if (house.Building == building) return house.Owed > 0 ? house.Timer / BalanceConfig.Instance.InvasionPersonRespawnSeconds : 0f;
            }
            return 0f;
        }

        /// <summary>The run's houses, as InvasionMode stood them; the people move in at once.</summary>
        public void Settle(IEnumerable<BuildingInstance> houses)
        {
            foreach (var building in houses)
            {
                if (building != null) _houses.Add(new House { Building = building });
            }
            var citizens = CitizenManager.Instance;
            if (citizens == null) return;
            SyncCapacity();
            citizens.AddCitizens(Mathf.Max(0, Places - People));
        }

        private void Update()
        {
            var removed = _houses.RemoveAll(h => h.Building == null || h.Building.CurrentHealth <= 0);
            if (removed > 0) SyncCapacity();

            // what the standing houses still owe the island, shared out one per house at a time
            var shortfall = Mathf.Max(0, Places - People);
            Balance(shortfall);

            var seconds = BalanceConfig.Instance.InvasionPersonRespawnSeconds;
            foreach (var house in _houses)
            {
                if (house.Owed <= 0)
                {
                    house.Timer = 0f;
                    if (house.Bar != null) house.Bar.Hide();
                    continue;
                }

                house.Timer += Time.deltaTime;
                if (house.Bar == null) house.Bar = HarvestProgressBar.CreateFor(house.Building.transform, ResourceType.Population);
                house.Bar.Report(house.Timer / seconds);
                if (house.Timer < seconds) continue;

                house.Timer = 0f;
                house.Owed--;
                CitizenManager.Instance?.AddCitizens(1);
            }
        }

        /// <summary>Spreads the shortfall over the houses so that the total owed matches it: new debts to the least-owing house, cleared debts from the most-owing.</summary>
        private void Balance(int shortfall)
        {
            if (_houses.Count == 0) return;
            var perHouse = BalanceConfig.Instance.InvasionPeoplePerHouse;
            var owed = Owed;
            while (owed < shortfall)
            {
                House least = null;
                foreach (var house in _houses)
                {
                    if (house.Owed < perHouse && (least == null || house.Owed < least.Owed)) least = house;
                }
                if (least == null) break;
                least.Owed++;
                owed++;
            }
            while (owed > shortfall)
            {
                House most = null;
                foreach (var house in _houses)
                {
                    if (house.Owed > 0 && (most == null || house.Owed > most.Owed)) most = house;
                }
                if (most == null) break;
                most.Owed--;
                if (most.Owed == 0) most.Timer = 0f;
                owed--;
            }
        }

        /// <summary>The town's room is the houses' places and nothing else: not the castle's, not a Hovel's sandbox capacity.</summary>
        private void SyncCapacity()
        {
            var citizens = CitizenManager.Instance;
            if (citizens == null) return;
            citizens.ChangeCapacity(Places - citizens.Capacity);
        }

        private void LateUpdate()
        {
            // a house going down or up a level elsewhere changes the sandbox's capacity; put it back
            var citizens = CitizenManager.Instance;
            if (citizens != null && citizens.Capacity != Places) SyncCapacity();
        }
    }
}
