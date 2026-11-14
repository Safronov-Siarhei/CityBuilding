using CityBuilder.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// One building on fire (sandbox fires v2, the user 2026-11-14). Added by FireManager.Ignite,
    /// removed by itself.
    ///
    /// - The fire has a STRENGTH 0..100: it starts at fire_start_strength and grows fire_grow_per_sec.
    /// - It burns max health x fire_hp_share_per_sec a second, scaled by its strength (half at 0,
    ///   whole at 100): about a minute untended, to nothing -- the building becomes a ruin
    ///   (BuildingRuin), and FireManager counts it for the banner.
    /// - Every fire_spread_every_sec it tries each burnable building TOUCHING it (no gap,
    ///   FireRules.Touching): fire_spread_chance each.
    /// - Firefighters' buckets (FireManager's crews) take strength off; at 0 it is out.
    /// - Under the storm's rain (Thunderstorm) it grows and burns at fire_rain_mult.
    /// </summary>
    public class BuildingFire : MonoBehaviour
    {
        private const float MarkSize = 0.7f;

        private BuildingInstance _building;
        private FireLook _look;
        private GameObject _mark;
        private float _damageDebt;
        private float _spreadIn;
        private float _elapsed;

        public float Strength { get; private set; }

        /// <summary>Seconds it has burnt (the save's, with the strength).</summary>
        public float ElapsedSeconds => _elapsed;

        public BuildingInstance Building => _building;

        private void Awake()
        {
            _building = GetComponent<BuildingInstance>();
            var config = BalanceConfig.Instance;
            Strength = config.FireStartStrength;
            _spreadIn = config.FireSpreadEverySeconds;
            var manager = FireManager.Instance;
            _look = new FireLook(transform, manager != null ? manager.FlameMaterial : null, manager != null ? manager.SmokeMaterial : null);
            _look.Strength(Strength);
            BuildMark(manager != null ? manager.MarkMaterial : null);
        }

        /// <summary>A saved fire put back as it was.</summary>
        public void Restore(float elapsed, float strength)
        {
            _elapsed = Mathf.Max(0f, elapsed);
            if (strength > 0f) Strength = Mathf.Clamp(strength, 1f, 100f);
            _look?.Strength(Strength);
        }

        /// <summary>A bucket of water: so much strength off. True when that put it out.</summary>
        public bool Douse(float amount)
        {
            Strength -= Mathf.Max(0f, amount);
            _look?.Steam();
            if (Strength > 0f)
            {
                _look?.Strength(Strength);
                return false;
            }
            Out(true);
            return true;
        }

        /// <summary>Put out (a crew, the banner): it goes, saying so when `tell`.</summary>
        public void Out(bool tell)
        {
            if (tell && _building != null)
                EventLogManager.Instance?.Post(GameEvent.About(_building, Localization.Format("#log_fire_out",
                    _building.Data != null ? _building.Data.LocalizedName : string.Empty), EventSeverity.Good, EventGlyph.Check));
            Destroy(this);
        }

        private void Update()
        {
            if (_building == null || _building.CurrentHealth <= 0)
            {
                Destroy(this);
                return;
            }
            var config = BalanceConfig.Instance;
            var dt = Time.deltaTime;
            var rain = Thunderstorm.RainOver(transform.position) ? config.FireRainMultiplier : 1f;
            _elapsed += dt;
            Strength = Mathf.Min(100f, Strength + config.FireGrowPerSecond * rain * dt);
            _look?.Strength(Strength);

            _damageDebt += _building.MaxHealth * config.FireHpSharePerSecond * (0.5f + 0.5f * Strength / 100f) * rain * dt;
            var whole = Mathf.FloorToInt(_damageDebt);
            if (whole > 0)
            {
                _damageDebt -= whole;
                _building.TryDamage(whole);
                if (_building == null || _building.CurrentHealth <= 0)
                {
                    FireManager.Instance?.RecordBurnt(_building);
                    Destroy(this);
                    return;
                }
            }

            _spreadIn -= dt;
            if (_spreadIn > 0f) return;
            _spreadIn = config.FireSpreadEverySeconds;
            FireManager.Instance?.SpreadFrom(_building);
        }

        private void OnDestroy()
        {
            _look?.Destroy();
            if (_mark != null) Destroy(_mark);
        }

        /// <summary>A fire mark over the roof the player can find from the camera's height (IconBillboard).</summary>
        private void BuildMark(Material material)
        {
            if (material == null) return;
            var bounds = FireLook.Bounds(transform);
            _mark = new GameObject("FireMark", typeof(MeshFilter), typeof(MeshRenderer));
            _mark.transform.SetParent(transform, true);
            _mark.transform.position = new Vector3(bounds.center.x, bounds.max.y + 0.7f, bounds.center.z);
            _mark.GetComponent<MeshFilter>().sharedMesh = FireManager.MarkQuad();
            var renderer = _mark.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var block = new MaterialPropertyBlock();
            block.SetFloat("_Size", MarkSize);
            renderer.SetPropertyBlock(block);
        }
    }
}
