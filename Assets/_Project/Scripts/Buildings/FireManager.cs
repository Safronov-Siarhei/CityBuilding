using System.Collections.Generic;
using CityBuilder.Citizens;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// Sandbox fires v2 (the user, 2026-11-14; the rules in Design/features/hazards-fire-sickness.md).
    /// The old fires (chance = decay) died with decay (OFF since 2026-10-25); these are the sandbox's only.
    ///
    /// - Once a game day every burnable building (FireRules.Burns) may catch: fire_chance_per_day,
    ///   x fire_dry_mult with no water within fire_water_cells, x fire_prevent_mult on an island whose
    ///   Пожарная бригада is level fire_prevent_level. Not in the first hazard_grace_sec of a town,
    ///   nor while the banner's shield lasts.
    /// - Other fires: a touching neighbour (BuildingFire), an orc torchbearer (Combat.TorchErrand),
    ///   lightning (Thunderstorm).
    /// - The CREWS: a Пожарная бригада's workers answer every fire on their island (no radius): each
    ///   runs to the nearest water (a well, the sea, a river), fills his bucket (fire_fill_sec), runs to
    ///   the fire and pours (fire_bucket; fire_bucket_big from its level 2), and again until it is out.
    ///   Level 3: a barrel at the station (the first trip is already full); level 4: they run
    ///   fire_fast_mult faster. No brigade on the island: nobody puts it out.
    /// - The BANNER: fire destroyed fire_banner_share of the player's buildings within
    ///   fire_banner_window_sec -> the game pauses and FireBanner offers to put every fire out (an ad
    ///   or fire_end_crystals), which also shields the town for fire_shield_sec.
    /// </summary>
    public class FireManager : MonoBehaviour
    {
        public static FireManager Instance { get; private set; }

        public const string BrigadeId = "FireBrigade";
        private const float DispatchSeconds = 0.5f;
        private const float NoticeEverySeconds = 30f;
        private const float ArriveMeters = 0.7f;

        [SerializeField] private Material flameMaterial;
        [SerializeField] private Material smokeMaterial;
        [SerializeField] private Material markMaterial;
        [SerializeField] private Material bucketMaterial;

        public Material FlameMaterial => flameMaterial;
        public Material SmokeMaterial => smokeMaterial;
        public Material MarkMaterial => markMaterial;

        /// <summary>Seconds left of the banner's shield: no fire starts meanwhile.</summary>
        public float ShieldLeft { get; private set; }

        private enum Leg { ToWater, Filling, ToFire }

        private class Crew
        {
            public CitizenAgent Agent;
            public BuildingInstance Brigade;
            public BuildingFire Fire;
            public Leg Leg;
            public float Timer;
            public Vector3 Goal;
            public bool Full;
            public GameObject Bucket;
        }

        private readonly List<Crew> _crews = new List<Crew>();
        private readonly List<BuildingFire> _fires = new List<BuildingFire>();
        private readonly List<CitizenAgent> _workers = new List<CitizenAgent>();
        private readonly List<float> _burnt = new List<float>();
        private readonly Dictionary<int, float> _told = new Dictionary<int, float>();
        private float _dispatchIn;
        private static Mesh _markQuad;
        private static Mesh _bucketMesh;

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
            if (Instance == this) Instance = null;
        }

        /// <summary>The first minutes of a town: neither fires nor illness (hazard_grace_sec, MigrationManager).</summary>
        public static bool InGrace => MigrationManager.Instance != null && MigrationManager.Instance.HazardGraceRemaining > 0f;

        private static bool CanStart => FireRules.Active && !InGrace && (Instance == null || Instance.ShieldLeft <= 0f);

        /// <summary>One game day of fire risk. Public so a test can pass a day.</summary>
        public void PassDay()
        {
            if (!CanStart) return;
            var config = BalanceConfig.Instance;
            var standing = new List<BuildingInstance>(BuildingInstance.All);
            foreach (var b in standing)
            {
                if (!FireRules.Burns(b) || b.GetComponent<BuildingFire>() != null) continue;
                var chance = config.FireChancePerDay;
                if (!FireRules.WaterNear(b)) chance *= config.FireDryMultiplier;
                if (BrigadeLevelOn(HousePeople.IslandOf(b)) >= config.FirePreventLevel) chance *= config.FirePreventMultiplier;
                if (Random.value < chance) Ignite(b, "#log_fire_started");
            }
        }

        /// <summary>
        /// Sets a building alight (sandbox, a burnable one, no shield or grace). Anybody inside dies
        /// (ResidentVisits). `force` for a save's fires, which the shield must not put out.
        /// </summary>
        public static BuildingFire Ignite(BuildingInstance building, string logKey = null, bool force = false)
        {
            if (building == null || building.GetComponent<BuildingFire>() != null) return null;
            if (!FireRules.Burns(building)) return null;
            if (!force && !CanStart) return null;
            var fire = building.gameObject.AddComponent<BuildingFire>();
            Instance?._fires.Add(fire);
            ResidentVisits.Instance?.Burned(building);
            if (logKey != null)
                EventLogManager.Instance?.Post(GameEvent.About(building, Localization.Format(logKey,
                    building.Data != null ? building.Data.LocalizedName : string.Empty), EventSeverity.Danger, EventGlyph.Fire));
            Instance?.TellNoBrigade(building);
            return fire;
        }

        /// <summary>A fire's try at the burnable buildings touching it (BuildingFire, every fire_spread_every_sec).</summary>
        public void SpreadFrom(BuildingInstance source)
        {
            if (source == null || !CanStart) return;
            var chance = BalanceConfig.Instance.FireSpreadChance;
            var all = new List<BuildingInstance>(BuildingInstance.All);
            foreach (var b in all)
            {
                if (b == source || b.GetComponent<BuildingFire>() != null || !FireRules.Burns(b)) continue;
                if (!FireRules.Touching(source, b)) continue;
                if (Random.value < chance) Ignite(b, "#log_fire_spread");
            }
        }

        /// <summary>A building the fire took down: counted for the banner.</summary>
        public void RecordBurnt(BuildingInstance building)
        {
            if (!FireRules.Active) return;
            var config = BalanceConfig.Instance;
            var now = Time.time;
            _burnt.Add(now);
            _burnt.RemoveAll(t => now - t > config.FireBannerWindowSeconds);
            var standing = 0;
            foreach (var b in BuildingInstance.All)
                if (b != null && !b.IsBot && b.Data != null && b.CurrentHealth > 0 && !b.Data.IsGroundWork) standing++;
            if (_burnt.Count < config.FireBannerShare * Mathf.Max(1, standing + _burnt.Count)) return;
            _burnt.Clear();
            UI.FireBanner.Instance?.Show(0);
        }

        /// <summary>The banner: every fire out, and the shield up for fire_shield_sec.</summary>
        public void EndByBanner()
        {
            ExtinguishAll();
            ShieldLeft = Mathf.Max(ShieldLeft, BalanceConfig.Instance.FireShieldSeconds);
            EventLogManager.Instance?.Post(GameEvent.ForSettlement(Localization.Format("#log_fire_shield",
                Mathf.RoundToInt(ShieldLeft / 60f)), EventSeverity.Good, EventGlyph.Check, BrigadeId));
        }

        public void ExtinguishAll()
        {
            foreach (var f in new List<BuildingFire>(_fires)) if (f != null) f.Out(false);
            _fires.Clear();
        }

        public IEnumerable<BuildingFire> Fires
        {
            get
            {
                _fires.RemoveAll(f => f == null);
                return _fires;
            }
        }

        // ---------------------------------------------------------------- the crews

        private void Update()
        {
            if (ShieldLeft > 0f) ShieldLeft = Mathf.Max(0f, ShieldLeft - Time.deltaTime);
            _fires.RemoveAll(f => f == null);
            for (var i = _crews.Count - 1; i >= 0; i--)
                if (!Step(_crews[i])) Release(i);
            if (_fires.Count == 0) return;
            _dispatchIn -= Time.deltaTime;
            if (_dispatchIn > 0f) return;
            _dispatchIn = DispatchSeconds;
            Dispatch();
        }

        /// <summary>Every brigade's free workers to the nearest fire on its island.</summary>
        private void Dispatch()
        {
            var visuals = CitizenVisualsManager.Instance;
            if (visuals == null) return;
            foreach (var brigade in BuildingInstance.All)
            {
                if (!IsBrigade(brigade)) continue;
                var island = HousePeople.IslandOf(brigade);
                var production = brigade.GetComponent<ProductionBuilding>();
                if (production == null) continue;
                visuals.WorkersAt(production, _workers);
                foreach (var agent in _workers)
                {
                    if (agent == null || Busy(agent)) continue;
                    var fire = NearestFire(agent.transform.position, island);
                    if (fire == null) break;
                    var crew = new Crew { Agent = agent, Brigade = brigade, Fire = fire, Full = brigade.Level >= BalanceConfig.Instance.FireBarrelLevel };
                    agent.SpeedBonus = brigade.Level >= BalanceConfig.Instance.FireFastLevel ? BalanceConfig.Instance.FireFastMultiplier : 1f;
                    crew.Bucket = Bucket(agent.transform);
                    _crews.Add(crew);
                    if (crew.Full) GoToFire(crew);
                    else GoToWater(crew);
                }
            }
        }

        private bool Busy(CitizenAgent agent)
        {
            foreach (var c in _crews) if (c.Agent == agent) return true;
            return false;
        }

        private BuildingFire NearestFire(Vector3 from, int island)
        {
            BuildingFire best = null;
            var bestD = float.MaxValue;
            foreach (var f in _fires)
            {
                if (f == null || !HousePeople.Same(HousePeople.IslandOf(f.transform.position), island)) continue;
                var d = (f.transform.position - from).sqrMagnitude;
                if (d < bestD) { bestD = d; best = f; }
            }
            return best;
        }

        /// <summary>One crew's frame. False when the job is over (the fire out, the man gone).</summary>
        private bool Step(Crew c)
        {
            if (c.Agent == null || c.Brigade == null) return false;
            if (c.Fire == null)
            {
                // this fire is out: another on the island, or home
                c.Fire = NearestFire(c.Agent.transform.position, HousePeople.IslandOf(c.Brigade));
                if (c.Fire == null) return false;
                if (c.Full) GoToFire(c);
                else GoToWater(c);
            }
            switch (c.Leg)
            {
                case Leg.ToWater:
                    if (!Arrived(c)) return true;
                    c.Leg = Leg.Filling;
                    c.Timer = BalanceConfig.Instance.FireFillSeconds;
                    return true;
                case Leg.Filling:
                    c.Timer -= Time.deltaTime;
                    if (c.Timer > 0f) return true;
                    c.Full = true;
                    GoToFire(c);
                    return true;
                default:
                    if (!Arrived(c)) return true;
                    var config = BalanceConfig.Instance;
                    var bucket = c.Brigade.Level >= config.FireBucketBigLevel ? config.FireBucketBig : config.FireBucket;
                    c.Full = false;
                    if (c.Fire.Douse(bucket)) c.Fire = null;
                    else GoToWater(c);
                    return true;
            }
        }

        private bool Arrived(Crew c)
        {
            var to = c.Goal - c.Agent.transform.position;
            to.y = 0f;
            return to.magnitude <= ArriveMeters || !c.Agent.ManualMoving;
        }

        private void GoToWater(Crew c)
        {
            var water = FireRules.NearestWater(c.Agent.transform.position, HousePeople.IslandOf(c.Brigade));
            if (!water.HasValue)
            {
                // no water anywhere near: straight to the fire with what he has (sand and blankets)
                c.Full = true;
                GoToFire(c);
                return;
            }
            c.Leg = Leg.ToWater;
            c.Goal = water.Value;
            c.Agent.MoveTo(c.Goal);
        }

        private void GoToFire(Crew c)
        {
            c.Leg = Leg.ToFire;
            var visuals = CitizenVisualsManager.Instance;
            var building = c.Fire.Building;
            c.Goal = visuals != null && building != null ? visuals.DoorOf(building) : c.Fire.transform.position;
            c.Agent.MoveTo(c.Goal);
        }

        private void Release(int index)
        {
            var c = _crews[index];
            _crews.RemoveAt(index);
            if (c.Bucket != null) Destroy(c.Bucket);
            if (c.Agent == null) return;
            c.Agent.SpeedBonus = 1f;
            // home: the walk back ends in his work again (CitizenAgent's ManualMove resumes it)
            var visuals = CitizenVisualsManager.Instance;
            if (visuals != null && c.Brigade != null) c.Agent.MoveTo(visuals.DoorOf(c.Brigade));
        }

        private static bool IsBrigade(BuildingInstance b) =>
            b != null && !b.IsBot && b.Data != null && b.Data.buildingName == BrigadeId && b.IsOperational && b.CurrentHealth > 0;

        /// <summary>The highest level of a staffed brigade on the island, 0 for none.</summary>
        public static int BrigadeLevelOn(int island)
        {
            var level = 0;
            foreach (var b in BuildingInstance.All)
            {
                if (!IsBrigade(b) || !HousePeople.Same(HousePeople.IslandOf(b), island)) continue;
                var p = b.GetComponent<ProductionBuilding>();
                if (p == null || p.AssignedWorkers <= 0) continue;
                level = Mathf.Max(level, b.Level);
            }
            return level;
        }

        /// <summary>A fire on an island with no staffed brigade: «Пожар! Постройте пожарную бригаду» / «Назначьте пожарного».</summary>
        private void TellNoBrigade(BuildingInstance building)
        {
            var island = HousePeople.IslandOf(building);
            if (_told.TryGetValue(island, out var last) && Time.time - last < NoticeEverySeconds) return;
            var any = false;
            var staffed = false;
            foreach (var b in BuildingInstance.All)
            {
                if (!IsBrigade(b) || !HousePeople.Same(HousePeople.IslandOf(b), island)) continue;
                any = true;
                var p = b.GetComponent<ProductionBuilding>();
                if (p != null && p.AssignedWorkers > 0) staffed = true;
            }
            if (staffed) return;
            _told[island] = Time.time;
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Get(any ? "#log_brigade_no_worker" : "#log_fire_no_brigade"),
                EventSeverity.Warning, EventGlyph.Fire, BrigadeId, building.transform.position));
        }

        // ---------------------------------------------------------------- looks

        /// <summary>A small wooden bucket in the firefighter's hand while he is on the job.</summary>
        private GameObject Bucket(Transform agent)
        {
            var go = new GameObject("Bucket", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(agent, false);
            go.transform.localPosition = new Vector3(0.12f, 0.16f, 0.04f);
            go.GetComponent<MeshFilter>().sharedMesh = BucketMesh();
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = bucketMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        private static Mesh BucketMesh()
        {
            if (_bucketMesh != null) return _bucketMesh;
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _bucketMesh = Instantiate(cube.GetComponent<MeshFilter>().sharedMesh);
            Destroy(cube);
            var v = _bucketMesh.vertices;
            for (var i = 0; i < v.Length; i++)
            {
                var widen = v[i].y > 0f ? 1.2f : 1f;   // wider at the rim
                v[i] = new Vector3(v[i].x * 0.09f * widen, v[i].y * 0.05f, v[i].z * 0.09f * widen);
            }
            _bucketMesh.vertices = v;
            _bucketMesh.RecalculateBounds();
            return _bucketMesh;
        }

        public static Mesh MarkQuad()
        {
            if (_markQuad != null) return _markQuad;
            var h = 0.5f;
            _markQuad = new Mesh
            {
                name = "FireMarkQuad",
                vertices = new[] { new Vector3(-h, -h, 0f), new Vector3(h, -h, 0f), new Vector3(h, h, 0f), new Vector3(-h, h, 0f) },
                uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) },
                triangles = new[] { 0, 2, 1, 0, 3, 2 },
            };
            _markQuad.bounds = new Bounds(Vector3.zero, Vector3.one * 2f);
            return _markQuad;
        }

        // ---------------------------------------------------------------- save

        /// <summary>The save's shield (the fires themselves come back one by one through Ignite(force)).</summary>
        public void RestoreShield(float seconds) => ShieldLeft = Mathf.Max(0f, seconds);
    }
}
