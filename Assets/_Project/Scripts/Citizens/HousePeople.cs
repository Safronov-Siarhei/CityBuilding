using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Citizens
{
    /// <summary>
    /// Where the people come from and where they live (the user: 2026-10-21 for a run, 2026-10-25 for
    /// the sandbox; RESIDENTS BOUND TO THEIR HOUSE 2026-11-12). Each standing house -- every building
    /// that holds people but the Town Hall (BuildingInstance.Houses) -- makes ITS OWN residents, one
    /// every person_respawn_sec (a bar over it), until its residents and the soldiers who came out of
    /// it fill its places (housing_capacity at its level); then it stops. So every island holds exactly
    /// the people its houses have room for, and the people are where their houses are.
    ///
    /// - A house burnt or pulled down: its residents LEAVE the town, and the soldiers who came from it
    ///   leave the army at once (the user's answers).
    /// - A soldier keeps his place in the house he was hired from (Enlist); disbanded he comes home,
    ///   killed his place frees and the house grows a new resident.
    /// - The Town Hall silently keeps a few permanent residents (its citizens_on_build), regrown with no
    ///   bar and never shown as making anyone -- a hidden guard against an outright loss.
    /// - Hunger, sickness deaths and the like take people from ANY house (the fullest first, the hall
    ///   last): the town's count is the truth, the ledger follows it (Reconcile).
    /// - Work and hiring go by island: IdleOn(island) is what a building or a Barracks there may take.
    ///
    /// The headcount stays CitizenManager's pool; this keeps who lives where beside it. Not saved: a
    /// loaded town's people are put back into houses with room (the islands short of workers first, the hall last),
    /// and its soldiers into houses near them.
    ///
    /// **A hungry town makes no one** (the user, 2026-11-10): while the last meal fell short the bars
    /// stand still. Not in the tutorial (its own pacing).
    /// </summary>
    public class HousePeople : MonoBehaviour
    {
        private class House
        {
            public BuildingInstance Building;
            public bool IsHall;
            public int Island;
            public int Citizens;
            public int Soldiers;
            public float Timer;
            public HarvestProgressBar Bar;
        }

        public static HousePeople Instance { get; private set; }

        private readonly List<House> _houses = new List<House>();
        private House _hall;
        private int _homeless;
        private readonly Dictionary<SoldierUnit, List<House>> _soldierHomes = new Dictionary<SoldierUnit, List<House>>();
        private readonly List<SoldierUnit> _goneSoldiers = new List<SoldierUnit>();

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---------------------------------------------------------------- islands

        /// <summary>The archipelago island under a point; -1 for any point of a map with no islands (every map but the archipelago), which matches every island.</summary>
        public static int IslandOf(Vector3 position)
        {
            var map = MeshMapApplier.Instance;
            var grid = GridManager.Instance;
            if (map == null || grid == null || map.IslandLabels == null) return -1;
            return map.IslandAt(grid.WorldToCell(position));
        }

        public static int IslandOf(BuildingInstance building) => building == null ? -1 : IslandOf(building.transform.position);

        /// <summary>Two islands that are one for walking: equal, or either unknown (not the archipelago, a beach's edge).</summary>
        public static bool Same(int a, int b) => a < 0 || b < 0 || a == b;

        // ---------------------------------------------------------------- counts

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

        private static int HallPlaces(BuildingInstance hall) =>
            hall == null || hall.Data == null || hall.CurrentHealth <= 0 ? 0 : Mathf.Max(0, hall.Data.citizensOnBuild);

        private static int PlacesFor(House house) => house.IsHall ? HallPlaces(house.Building) : PlacesOf(house.Building);

        private static int Room(House house) => Mathf.Max(0, PlacesFor(house) - house.Citizens - house.Soldiers);

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

        /// <summary>The houses wait: the town is short of food.</summary>
        public static bool PausedByHunger =>
            !Tutorial.TutorialMode.IsActive && FoodConsumptionManager.Instance != null && FoodConsumptionManager.Instance.Hungry;

        /// <summary>People the houses are making right now (the room left in them; the hall's hidden regrowth not counted).</summary>
        public int Owed
        {
            get
            {
                var owed = 0;
                foreach (var house in _houses) owed += Room(house);
                return owed;
            }
        }

        /// <summary>The house's progress towards its next person, 0 when it is full -- for the tests and the HUD.</summary>
        public float ProgressOf(BuildingInstance building)
        {
            foreach (var house in _houses)
            {
                if (house.Building == building) return Room(house) > 0 ? house.Timer / Seconds : 0f;
            }
            return 0f;
        }

        /// <summary>Residents of this house (the Town Hall's hidden ones for the hall), soldiers not counted.</summary>
        public int CitizensOf(BuildingInstance building)
        {
            if (building == null) return 0;
            if (_hall != null && _hall.Building == building) return _hall.Citizens;
            foreach (var house in _houses)
            {
                if (house.Building == building) return house.Citizens;
            }
            return 0;
        }

        /// <summary>Residents living on this island (houses and the hall there).</summary>
        public int CitizensOn(int island)
        {
            var n = _hall != null && Same(_hall.Island, island) ? _hall.Citizens : 0;
            foreach (var house in _houses)
            {
                if (Same(house.Island, island)) n += house.Citizens;
            }
            return n;
        }

        /// <summary>Workers the buildings on this island hold.</summary>
        public static int WorkersOn(int island)
        {
            var n = 0;
            foreach (var building in ProductionBuilding.All)
            {
                if (building != null && building.AssignedWorkers > 0 && Same(building.Island, island)) n += building.AssignedWorkers;
            }
            return n;
        }

        /// <summary>Residents of this island with no job -- what a building or a Barracks there may take.</summary>
        public int IdleOn(int island) => Mathf.Max(0, CitizensOn(island) - SickOn(island) - WorkersOn(island));

        /// <summary>The sick of this island (the sandbox Лечебница's patients): residents, but no hands.</summary>
        private static int SickOn(int island) => Infirmary.Active ? Infirmary.Instance.SickOn(island) : 0;

        /// <summary>Every home (the hall first) as (building, residents) -- for the walking figures, who live where these do.</summary>
        public void Homes(List<KeyValuePair<BuildingInstance, int>> into)
        {
            into.Clear();
            if (_hall != null && _hall.Building != null) into.Add(new KeyValuePair<BuildingInstance, int>(_hall.Building, _hall.Citizens));
            foreach (var house in _houses) into.Add(new KeyValuePair<BuildingInstance, int>(house.Building, house.Citizens));
        }

        // the tutorial's Hovel brings its person in tutorial_birth_sec (the user, 2026-11-06: it took too long)
        private static float Seconds => Mathf.Max(0.1f, Tutorial.TutorialMode.IsActive
            ? BalanceConfig.Instance.TutorialBirthSeconds : BalanceConfig.Instance.PersonRespawnSeconds);

        /// <summary>Every house full at once -- a run's island, whose people are there from the start.</summary>
        public void FillNow()
        {
            SyncCapacity();
            SyncHouses();
            CitizenManager.Instance?.AddCitizens(Mathf.Max(0, Places - People));
            Reconcile();
        }

        // ---------------------------------------------------------------- the frame

        private void Update()
        {
            SyncHouses();
            SyncSoldiers();   // first: a loaded town's soldiers take their places before its people do
            Reconcile();
            SendOvercrowdedAway();

            var seconds = Seconds;
            var paused = PausedByHunger;
            foreach (var house in _houses) Grow(house, seconds, paused, true);
            if (_hall != null) Grow(_hall, seconds, paused, false);
        }

        private void Grow(House house, float seconds, bool paused, bool showBar)
        {
            if (Room(house) <= 0 || (house.Building.Site != null && !house.Building.Site.IsUpgrade))
            {
                house.Timer = 0f;
                if (house.Bar != null) house.Bar.Hide();
                return;
            }

            if (!paused) house.Timer += Time.deltaTime;
            if (showBar)
            {
                if (house.Bar == null) house.Bar = HarvestProgressBar.CreateFor(house.Building.transform, ResourceType.Population);
                house.Bar.Report(house.Timer / seconds);
            }
            if (house.Timer < seconds) return;

            house.Timer = 0f;
            house.Citizens++;
            CitizenManager.Instance?.AddCitizens(1);
        }

        /// <summary>
        /// Makes the homes' residents add up to the town's count. People added elsewhere (the founding
        /// party, a booster, a freed prisoner, a load) move into the hall and the houses with room -- the
        /// islands whose buildings hold more workers than residents first -- and those with nowhere to go
        /// are homeless (they walk out, SendOvercrowdedAway). People lost elsewhere (hunger, a death)
        /// come off the homeless first, then the fullest house, the hall last.
        /// </summary>
        private void Reconcile()
        {
            var citizens = CitizenManager.Instance;
            if (citizens == null) return;
            var housed = _homeless + (_hall != null ? _hall.Citizens : 0);
            foreach (var house in _houses) housed += house.Citizens;
            var diff = citizens.TotalPopulation - housed;
            if (diff == 0) return;

            if (diff > 0)
            {
                // islands short of their workers' homes first (a loaded town)
                foreach (var house in _houses)
                {
                    if (diff <= 0) break;
                    if (WorkersOn(house.Island) > CitizensOn(house.Island)) diff -= Fill(house, Mathf.Min(diff, WorkersOn(house.Island) - CitizensOn(house.Island)));
                }
                foreach (var house in _houses)
                {
                    if (diff <= 0) break;
                    diff -= Fill(house, diff);
                }
                // the hall last: the founding party, who arrive before any house stands
                if (_hall != null && diff > 0) diff -= Fill(_hall, diff);
                _homeless += Mathf.Max(0, diff);
                return;
            }

            var lose = -diff;
            var off = Mathf.Min(lose, _homeless);
            _homeless -= off;
            lose -= off;
            while (lose > 0)
            {
                House fullest = null;
                foreach (var house in _houses)
                {
                    if (house.Citizens > 0 && (fullest == null || house.Citizens > fullest.Citizens)) fullest = house;
                }
                if (fullest == null) fullest = _hall != null && _hall.Citizens > 0 ? _hall : null;
                if (fullest == null) break;
                fullest.Citizens--;
                lose--;
            }
            LayOffIslandExcess();
        }

        private static int Fill(House house, int want)
        {
            var take = Mathf.Min(want, Room(house));
            if (take <= 0) return 0;
            house.Citizens += take;
            return take;
        }

        /// <summary>A building whose island has fewer healthy residents than workers loses the difference (the place stays wanted).</summary>
        public void LayOffIslandExcess()
        {
            foreach (var building in ProductionBuilding.All)
            {
                if (building == null || building.AssignedWorkers <= 0) continue;
                var island = building.Island;
                while (building.AssignedWorkers > 0 && WorkersOn(island) > CitizensOn(island) - SickOn(island)) building.LayOffWorker();
            }
        }

        /// <summary>This one walking figure's home lost him (an orc struck him down): the town's count follows by itself.</summary>
        public void Lose(BuildingInstance home)
        {
            if (home == null) return;
            if (_hall != null && _hall.Building == home && _hall.Citizens > 0) { _hall.Citizens--; return; }
            foreach (var house in _houses)
            {
                if (house.Building == home && house.Citizens > 0) { house.Citizens--; return; }
            }
        }

        // ---------------------------------------------------------------- soldiers

        /// <summary>
        /// A soldier hired at `at` (his Barracks, Stable, Church, Workshop): the people he is made of come
        /// out of houses on that island (the fullest first) and keep their places there.
        /// </summary>
        public void Enlist(SoldierUnit unit, Vector3 at, int crew)
        {
            if (unit == null || crew <= 0) return;
            var island = IslandOf(at);
            var homes = new List<House>(crew);
            for (var i = 0; i < crew; i++)
            {
                House from = null;
                foreach (var house in _houses)
                {
                    if (house.Citizens > 0 && Same(house.Island, island) && (from == null || house.Citizens > from.Citizens)) from = house;
                }
                if (from == null && _hall != null && _hall.Citizens > 0 && Same(_hall.Island, island)) from = _hall;
                if (from == null) break;
                from.Citizens--;
                from.Soldiers++;
                homes.Add(from);
            }
            _soldierHomes[unit] = homes;
        }

        /// <summary>A soldier sent home (disbanded): his people live in their houses again. The caller hands the town its citizens back.</summary>
        public void Discharge(SoldierUnit unit)
        {
            if (unit == null || !_soldierHomes.TryGetValue(unit, out var homes)) return;
            _soldierHomes.Remove(unit);
            foreach (var house in homes)
            {
                house.Soldiers = Mathf.Max(0, house.Soldiers - 1);
                if (house.Building != null && house.Building.CurrentHealth > 0) house.Citizens++;
            }
        }

        /// <summary>Whether this island has `count` residents with no job -- the hiring building's check.</summary>
        public bool CanHire(Vector3 at, int count) => IdleOn(IslandOf(at)) >= count;

        /// <summary>A killed soldier frees his place (his house grows someone new); one with no home yet (a loaded town) moves into a house with room near him.</summary>
        private void SyncSoldiers()
        {
            _goneSoldiers.Clear();
            foreach (var pair in _soldierHomes)
            {
                if (pair.Key == null) _goneSoldiers.Add(pair.Key);
            }
            foreach (var unit in _goneSoldiers)
            {
                foreach (var house in _soldierHomes[unit]) house.Soldiers = Mathf.Max(0, house.Soldiers - 1);
                _soldierHomes.Remove(unit);
            }

            var army = ArmyManager.Instance;
            if (army == null) return;
            foreach (var group in army.Groups)
            {
                if (SoldierStats.IsHero(group.Type) || group.IsSummoned) continue;
                var members = group.Members;
                for (var i = 0; i < members.Count; i++)
                {
                    var unit = members[i];
                    if (unit == null || _soldierHomes.ContainsKey(unit)) continue;
                    var homes = new List<House>();
                    var island = IslandOf(unit.transform.position);
                    for (var c = 0; c < SoldierStats.Crew(unit.Type); c++)
                    {
                        House room = null;
                        foreach (var house in _houses)
                        {
                            if (Room(house) > 0 && (room == null || (Same(house.Island, island) && !Same(room.Island, island)))) room = house;
                        }
                        if (room == null) break;
                        room.Soldiers++;
                        homes.Add(room);
                    }
                    _soldierHomes[unit] = homes;
                }
            }
        }

        // ---------------------------------------------------------------- houses coming and going

        /// <summary>Follows BuildingInstance.Houses and the Town Hall: a new one joins, a lost one's people leave.</summary>
        private void SyncHouses()
        {
            for (var i = _houses.Count - 1; i >= 0; i--)
            {
                var house = _houses[i];
                if (house.Building != null && house.Building.CurrentHealth > 0 && house.Building.isActiveAndEnabled) continue;
                _houses.RemoveAt(i);
                Abandon(house);
            }
            if (_hall != null && (_hall.Building == null || _hall.Building.CurrentHealth <= 0))
            {
                var hall = _hall;
                _hall = null;
                Abandon(hall);
            }

            var standing = BuildingInstance.Houses;
            for (var i = 0; i < standing.Count; i++)
            {
                var building = standing[i];
                if (building == null || building.IsBot || building.CurrentHealth <= 0 || Tracks(building)) continue;
                _houses.Add(new House { Building = building, Island = IslandOf(building) });
            }
            var townHall = BuildingInstance.TownHall;
            if (_hall == null && townHall != null && townHall.CurrentHealth > 0)
                _hall = new House { Building = townHall, IsHall = true, Island = IslandOf(townHall) };
        }

        /// <summary>A house gone: its residents walk out of the town, and its soldiers leave the army at once (the user, 2026-11-12).</summary>
        private void Abandon(House house)
        {
            if (house.Bar != null) house.Bar.Hide();
            var citizens = CitizenManager.Instance;
            var left = house.Citizens;
            house.Citizens = 0;
            if (left > 0 && citizens != null) citizens.RemoveCitizens(left);

            var soldiers = 0;
            _goneSoldiers.Clear();
            foreach (var pair in _soldierHomes)
            {
                if (pair.Key != null && pair.Value.Contains(house)) _goneSoldiers.Add(pair.Key);
            }
            foreach (var unit in _goneSoldiers)
            {
                foreach (var other in _soldierHomes[unit])
                {
                    if (other != house) other.Soldiers = Mathf.Max(0, other.Soldiers - 1);
                }
                _soldierHomes.Remove(unit);
                ArmyManager.Instance?.SendAway(unit);
                soldiers++;
            }
            LayOffIslandExcess();

            if ((left > 0 || soldiers > 0) && !house.IsHall)
                EventLogManager.Instance?.Post(GameEvent.ForSettlement(Localization.Format("#log_house_lost", left + soldiers),
                    EventSeverity.Warning, EventGlyph.Sad, "Res_Population"));
        }

        private bool Tracks(BuildingInstance building)
        {
            foreach (var house in _houses)
            {
                if (house.Building == building) return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- the homeless

        // People with no home -- the Новосёлы booster beyond the houses' room, a cheat -- leave ONE AT A
        // TIME, one every crowded_leave_interval_sec (the user, 2026-11-12). The sandbox only: not a run,
        // not the tutorial. Not deaths: RemoveCitizens, not KillCitizens.
        private float _crowdTimer;

        /// <summary>People with no home to live in (0 where the rule does not apply).</summary>
        public int Overcrowded =>
            Invasion.InvasionMode.IsActive || Tutorial.TutorialMode.IsActive ? 0 : _homeless;

        private void SendOvercrowdedAway()
        {
            var citizens = CitizenManager.Instance;
            if (citizens == null || Overcrowded <= 0)
            {
                _crowdTimer = 0f;
                return;
            }
            _crowdTimer += Time.deltaTime;
            if (_crowdTimer < BalanceConfig.Instance.CrowdedLeaveIntervalSeconds) return;
            _crowdTimer = 0f;
            if (citizens.RemoveCitizens(1) <= 0) return;
            EventLogManager.Instance?.Post(GameEvent.ForSettlement(Localization.Get("#log_crowded"), EventSeverity.Warning,
                EventGlyph.Sad, "Res_Population"));
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
