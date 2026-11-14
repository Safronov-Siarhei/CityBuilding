using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// A thunderstorm (the user, 2026-11-14: the seasons changed nothing, so a weather event for every
    /// map; sandbox only). Every storm_every_min_sec..storm_every_max_sec a dark cloud with rain crosses
    /// the town over storm_cross_sec (its path runs over one of the player's buildings, so it matters).
    /// Every strike_every_min_sec..strike_every_max_sec lightning strikes: with strike_chance it hits
    /// the tallest burnable building under the cloud and sets it alight. Fires under the rain grow and
    /// burn at fire_rain_mult (BuildingFire). Not in the town's first minutes (hazard_grace_sec).
    /// </summary>
    public class Thunderstorm : MonoBehaviour
    {
        public static Thunderstorm Instance { get; private set; }

        private const float CloudHeight = 6f;

        [SerializeField] private Material cloudMaterial;
        [SerializeField] private Material rainMaterial;
        [SerializeField] private Material boltMaterial;

        private float _next = -1f;
        private float _strikeIn;
        private bool _on;
        private Vector3 _from, _to, _centre;
        private float _t, _radius;
        private GameObject _look;
        private ParticleSystem _cloud, _rain;
        private LineRenderer _bolt;
        private Light _flash;
        private float _flashLeft;

        public static bool Raging => Instance != null && Instance._on;

        /// <summary>Is the storm's rain falling here now (BuildingFire's slower fires).</summary>
        public static bool RainOver(Vector3 at)
        {
            if (!Raging) return false;
            var s = Instance;
            var d = at - s._centre;
            d.y = 0f;
            return d.sqrMagnitude <= s._radius * s._radius;
        }

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!FireRules.Active)
            {
                if (_on) Stop();
                return;
            }
            var config = BalanceConfig.Instance;
            if (!_on)
            {
                if (FireManager.InGrace) return;
                if (_next < 0f) _next = Random.Range(config.StormEveryMinSeconds, config.StormEveryMaxSeconds);
                _next -= Time.deltaTime;
                if (_next > 0f) return;
                _next = -1f;
                Begin(config);
                return;
            }
            _t += Time.deltaTime / Mathf.Max(1f, config.StormCrossSeconds);
            _centre = Vector3.Lerp(_from, _to, _t);
            if (_look != null) _look.transform.position = _centre + Vector3.up * CloudHeight;
            Flash();
            if (_t >= 1f)
            {
                Stop();
                return;
            }
            _strikeIn -= Time.deltaTime;
            if (_strikeIn > 0f) return;
            _strikeIn = Random.Range(config.StrikeEveryMinSeconds, config.StrikeEveryMaxSeconds);
            Strike(config);
        }

        private void Begin(BalanceConfig config)
        {
            // over one of the town's buildings, coming from a random side
            BuildingInstance over = null;
            var seen = 0;
            foreach (var b in BuildingInstance.All)
            {
                if (!FireRules.Burns(b)) continue;
                seen++;
                if (Random.Range(0, seen) == 0) over = b;
            }
            if (over == null) return;
            var grid = Grid.GridManager.Instance;
            _radius = config.StormRadiusCells * (grid != null ? grid.CellSize : 1f);
            var dir = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
            var p = over.transform.position;
            _from = p - dir * _radius * 2.5f;
            _to = p + dir * _radius * 2.5f;
            _centre = _from;
            _t = 0f;
            _on = true;
            _strikeIn = Random.Range(config.StrikeEveryMinSeconds, config.StrikeEveryMaxSeconds) * 0.5f;
            BuildLook();
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_storm"), EventSeverity.Warning, EventGlyph.Fire,
                FireManager.BrigadeId, p));
        }

        private void Stop()
        {
            _on = false;
            if (_look != null) Destroy(_look);
            _look = null;
        }

        private void Strike(BalanceConfig config)
        {
            // the tallest burnable building under the cloud
            BuildingInstance best = null;
            var top = float.MinValue;
            foreach (var b in BuildingInstance.All)
            {
                if (!FireRules.Burns(b) || !RainOver(b.transform.position)) continue;
                var h = FireLook.Bounds(b.transform).max.y;
                if (h > top) { top = h; best = b; }
            }
            var hits = best != null && Random.value < config.StrikeChance;
            var at = hits ? new Vector3(best.transform.position.x, top, best.transform.position.z)
                : _centre + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(0f, _radius);
            Bolt(at);
            if (hits) FireManager.Ignite(best, "#log_fire_lightning");
        }

        // ---------------------------------------------------------------- looks

        private void BuildLook()
        {
            _look = new GameObject("Thunderstorm");
            _look.transform.SetParent(transform, false);
            _look.transform.position = _centre + Vector3.up * CloudHeight;
            if (cloudMaterial != null)
            {
                _cloud = FireLook.Particles(_look.transform, "Cloud", cloudMaterial, 6f, 6f, 9f, 0.02f, 0.08f, _radius * 0.5f, _radius * 0.8f,
                    new Vector3(_radius * 1.6f, 0.6f, _radius * 1.6f), 1.2f, true);
                var main = _cloud.main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.startColor = new Color(0.22f, 0.23f, 0.27f, 0.85f);
                main.maxParticles = 40;
                _cloud.Emit(24);
            }
            if (rainMaterial != null)
            {
                _rain = FireLook.Particles(_look.transform, "Rain", rainMaterial, 140f, 0.5f, 0.7f, 9f, 11f, 0.03f, 0.05f,
                    new Vector3(_radius * 1.5f, 0.2f, _radius * 1.5f), 1f, false);
                _rain.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);      // falls
                var main = _rain.main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.startColor = new Color(0.75f, 0.82f, 0.95f, 0.55f);
                main.maxParticles = 160;
                var r = _rain.GetComponent<ParticleSystemRenderer>();
                r.velocityScale = 0.06f;
                r.lengthScale = 2f;
            }
            if (boltMaterial != null)
            {
                _bolt = new GameObject("Bolt").AddComponent<LineRenderer>();
                _bolt.transform.SetParent(_look.transform, false);
                _bolt.sharedMaterial = boltMaterial;
                _bolt.useWorldSpace = true;
                _bolt.widthMultiplier = 0.12f;
                _bolt.startColor = _bolt.endColor = new Color(0.9f, 0.95f, 1f, 1f);
                _bolt.positionCount = 0;
                _bolt.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            _flash = new GameObject("Flash").AddComponent<Light>();
            _flash.transform.SetParent(_look.transform, false);
            _flash.type = LightType.Point;
            _flash.range = _radius * 2f;
            _flash.color = new Color(0.85f, 0.9f, 1f);
            _flash.intensity = 0f;
            _flash.shadows = LightShadows.None;
        }

        private void Bolt(Vector3 ground)
        {
            _flashLeft = 0.25f;
            if (_flash != null) _flash.transform.position = ground + Vector3.up * 3f;
            if (_bolt == null) return;
            const int n = 9;
            _bolt.positionCount = n;
            var top = new Vector3(ground.x + Random.Range(-1f, 1f), _centre.y + CloudHeight, ground.z + Random.Range(-1f, 1f));
            for (var i = 0; i < n; i++)
            {
                var k = i / (float)(n - 1);
                var p = Vector3.Lerp(top, ground, k);
                if (i > 0 && i < n - 1) p += new Vector3(Random.Range(-0.4f, 0.4f), 0f, Random.Range(-0.4f, 0.4f));
                _bolt.SetPosition(i, p);
            }
        }

        private void Flash()
        {
            if (_flashLeft <= 0f) return;
            _flashLeft -= Time.deltaTime;
            var k = Mathf.Clamp01(_flashLeft / 0.25f);
            if (_flash != null) _flash.intensity = 6f * k;
            if (_flashLeft <= 0f && _bolt != null) _bolt.positionCount = 0;
        }
    }
}
