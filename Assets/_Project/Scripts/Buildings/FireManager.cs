using System.Collections.Generic;
using CityBuilder.Citizens;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// Buildings catch fire, once per game day, and the fire spreads.
    ///
    /// This is what the Пожарная бригада has been standing in the hotbar waiting for. The user's
    /// design (2026-08-19): a building's chance of going up is its DECAY, and the fire jumps to
    /// its neighbours.
    ///
    /// Tying it to decay is the whole reason the mechanic is worth having. A maintained town never
    /// catches fire at all, so repair stops being an errand and becomes fire insurance -- and a
    /// district the player has let go is not merely producing less, it is dangerous to everything
    /// standing near it.
    ///
    /// Randomness here is deliberate, and the opposite of the choice made for illness
    /// (SicknessManager, which is worked out rather than diced for): a fire is an EVENT, and an
    /// event the player could predict to the day is not one they would ever hurry to prevent.
    /// </summary>
    public class FireManager : MonoBehaviour
    {
        public static FireManager Instance { get; private set; }

        /// <summary>The id of the building that puts fires out. Named here for the same reason BuildingIds names the Town Hall.</summary>
        private const string FireBrigadeBuildingName = "FireBrigade";

        /// <summary>How often the brigade list is rebuilt while anything is burning. A building is not placed twice a second, and a fire lasts a minute.</summary>
        private const float BrigadeRefreshSeconds = 0.5f;

        private readonly List<(Vector3 position, float radiusSquared, int workers)> _brigades =
            new List<(Vector3, float, int)>();

        private float _brigadeRefreshTimer;

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
        /// One day of fire risk: what catches, and where what is already burning spreads to.
        /// Public so a test can pass a day without waiting two real minutes for the calendar.
        /// </summary>
        public void PassDay()
        {
            var config = BalanceConfig.Instance;

            // Collected first. Igniting inside the enumeration would let a building that caught
            // fire this very morning spread it the same morning, which reads as two fires rather
            // than one.
            var standing = Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None);

            var burning = new List<BuildingInstance>();
            foreach (var instance in standing)
            {
                if (instance != null && instance.GetComponent<BuildingFire>() != null) burning.Add(instance);
            }

            foreach (var instance in standing)
            {
                if (instance == null || instance.Data == null) continue;
                if (instance.GetComponent<BuildingFire>() != null) continue;

                // Decay IS the chance. A building at zero decay cannot catch fire at all.
                if (Random.value >= config.FireChancePerDayAtFullDecay * instance.Decay) continue;

                Ignite(instance, "#log_fire_started");
            }

            SpreadFrom(burning, standing, config);
        }

        /// <summary>
        /// Sets a building alight. Safe to call on one already burning (it does nothing), and on
        /// one with no health left (BuildingFire retires itself on the next frame).
        /// </summary>
        public static void Ignite(BuildingInstance building, string logKey = null)
        {
            if (building == null || building.GetComponent<BuildingFire>() != null) return;

            building.gameObject.AddComponent<BuildingFire>();
            if (logKey != null) EventLogManager.Instance?.Log(Localization.Format(logKey, building.Data != null ? building.Data.LocalizedName : string.Empty));
        }

        private static void SpreadFrom(List<BuildingInstance> burning, BuildingInstance[] standing, BalanceConfig config)
        {
            if (burning.Count == 0) return;

            var radiusSquared = config.FireSpreadRadiusMeters * config.FireSpreadRadiusMeters;

            foreach (var source in burning)
            {
                if (source == null) continue;

                foreach (var neighbour in standing)
                {
                    if (neighbour == null || neighbour == source || neighbour.Data == null) continue;
                    if (neighbour.GetComponent<BuildingFire>() != null) continue;

                    var offset = neighbour.transform.position - source.transform.position;
                    offset.y = 0f;
                    if (offset.sqrMagnitude > radiusSquared) continue;

                    // Unlike catching fire on its own, spreading does NOT care how well kept the
                    // neighbour is: a new house next to a burning one is still a house next to a
                    // burning one. That is what makes the gap between districts worth leaving.
                    if (Random.value >= config.FireSpreadChancePerDay) continue;

                    Ignite(neighbour, "#log_fire_spread");
                }
            }
        }

        /// <summary>
        /// How many firefighters can reach this point. Read by every fire a few times a second, so
        /// the scan behind it is cached and rebuilt at most every BrigadeRefreshSeconds -- once for
        /// all the fires rather than once per fire.
        /// </summary>
        public int FirefightersCovering(Vector3 position)
        {
            RefreshBrigadesIfStale();

            var covering = 0;
            foreach (var (brigadePosition, radiusSquared, workers) in _brigades)
            {
                var offset = brigadePosition - position;
                offset.y = 0f;
                if (offset.sqrMagnitude <= radiusSquared) covering += workers;
            }
            return covering;
        }

        private void RefreshBrigadesIfStale()
        {
            if (_brigadeRefreshTimer > Time.time) return;
            _brigadeRefreshTimer = Time.time + BrigadeRefreshSeconds;

            _brigades.Clear();
            foreach (var instance in Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
            {
                if (instance.Data == null || instance.Data.buildingName != FireBrigadeBuildingName) continue;

                var radius = instance.ServiceRadius;
                if (radius <= 0) continue;

                var workplace = instance.GetComponent<ProductionBuilding>();
                var workers = workplace != null ? workplace.AssignedWorkers : 0;
                // An unstaffed fire station is a shed. Left out entirely rather than added with a
                // zero, so a player who built one and never manned it sees no effect at all.
                if (workers <= 0) continue;

                _brigades.Add((instance.transform.position, radius * (float)radius, workers));
            }
        }
    }
}
