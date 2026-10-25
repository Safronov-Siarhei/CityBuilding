using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Maps;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Citizens
{
    /// <summary>
    /// Where the people come from (the user: 2026-10-21 for an Invasion run, 2026-10-25 for the
    /// sandbox too, in place of migration): the houses make them. Each standing house -- every
    /// building that holds people but the Town Hall (BuildingInstance.Houses: in a run the six
    /// Hovels on the island, in the sandbox every Лачуга, Коттедж and Усадьба) -- has as many
    /// places as its level's housing_capacity, and while the town has fewer people than its houses
    /// have places, the houses make the shortfall, each one at a time, one every person_respawn_sec
    /// (a bar over the house shows it filling). A house being built or upgraded makes no one and
    /// counts only what it held before; a burnt house takes its places with it.
    ///
    /// People are counted as the town's population plus its soldiers (a hero is no one's
    /// householder). Which house makes the next person does not matter to the count; the houses
    /// share the shortfall. The town's room (CitizenManager.Capacity) is the houses' places and
    /// nothing else -- not the Town Hall's, whose founding party simply lives there.
    /// </summary>
    public class HousePeople : MonoBehaviour
    {
        private class House
        {
            public BuildingInstance Building;
            public int Owed;
            public float Timer;
            public HarvestProgressBar Bar;
        }

        public static HousePeople Instance { get; private set; }

        private readonly List<House> _houses = new List<House>();

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Places in the standing houses, each at its own level.</summary>
        public int Places
        {
            get
            {
                var places = 0;
                var houses = BuildingInstance.Houses;
                for (var i = 0; i < houses.Count; i++) places += PlacesOf(houses[i]);
                return places;
            }
        }

        /// <summary>How many live in this house at its level (its housing_capacity) -- none while it is first going up, and none once it has burnt.</summary>
        public static int PlacesOf(BuildingInstance house)
        {
            if (house == null || house.Data == null || house.CurrentHealth <= 0) return 0;
            if (house.Site != null && !house.Site.IsUpgrade) return 0;
            return Mathf.Max(0, house.Data.LevelStats(house.Level).housingCapacity);
        }

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

        public int StandingHouses => BuildingInstance.Houses.Count;

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
                if (house.Building == building) return house.Owed > 0 ? house.Timer / Seconds : 0f;
            }
            return 0f;
        }

        private static float Seconds => Mathf.Max(0.1f, BalanceConfig.Instance.PersonRespawnSeconds);

        /// <summary>Every house full at once -- a run's island, whose people are there from the start.</summary>
        public void FillNow()
        {
            SyncCapacity();
            CitizenManager.Instance?.AddCitizens(Mathf.Max(0, Places - People));
        }

        private void Update()
        {
            SyncHouses();

            // what the standing houses still owe the town, shared out one per house at a time
            var shortfall = Mathf.Max(0, Places - People);
            Balance(shortfall);

            var seconds = Seconds;
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

        /// <summary>Follows BuildingInstance.Houses: a new house joins, a gone one leaves. No allocation unless one did.</summary>
        private void SyncHouses()
        {
            var removed = _houses.RemoveAll(h => h.Building == null || h.Building.CurrentHealth <= 0);
            var standing = BuildingInstance.Houses;
            if (removed == 0 && _houses.Count == standing.Count) return;
            for (var i = 0; i < standing.Count; i++)
            {
                var building = standing[i];
                if (building == null || building.CurrentHealth <= 0 || Tracks(building)) continue;
                _houses.Add(new House { Building = building });
            }
        }

        private bool Tracks(BuildingInstance building)
        {
            foreach (var house in _houses)
            {
                if (house.Building == building) return true;
            }
            return false;
        }

        /// <summary>Spreads the shortfall over the houses so that the total owed matches it: new debts to the least-owing house, cleared debts from the most-owing.</summary>
        private void Balance(int shortfall)
        {
            if (_houses.Count == 0) return;
            var owed = Owed;
            while (owed < shortfall)
            {
                House least = null;
                foreach (var house in _houses)
                {
                    if (house.Owed < PlacesOf(house.Building) && (least == null || house.Owed < least.Owed)) least = house;
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

        /// <summary>The town's room is the houses' places and nothing else: not the Town Hall's.</summary>
        private void SyncCapacity()
        {
            var citizens = CitizenManager.Instance;
            if (citizens == null) return;
            citizens.ChangeCapacity(Places - citizens.Capacity);
        }

        private void LateUpdate()
        {
            // a building's own Initialize/upgrade/teardown hands the town its sheet capacity; put it back
            var citizens = CitizenManager.Instance;
            if (citizens != null && citizens.Capacity != Places) SyncCapacity();
        }
    }
}
