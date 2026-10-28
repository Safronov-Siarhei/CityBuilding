using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// The moment a construction is done (ConstructionLook.Complete), which hides the scaffolding
    /// going and the building coming (the user, 2026-10-28; both looks made to pick from in the
    /// settings, GameSettings.BuildFinish):
    ///
    /// - Dust: a thick cloud round the footprint, the scaffold's planks thrown off and falling, the
    ///   building squashed down and springing up past its height and back;
    /// - Fanfare: a gold flash over the building, a ring of sparks flying out and up, the building
    ///   popping up out of the flash from small.
    ///
    /// Soft billboards and a few boxes, as DustPuff is, so no particle shader is needed in the build;
    /// it all goes in a second and a half and removes itself.
    /// </summary>
    public class BuildFinishEffect : MonoBehaviour
    {
        private const float Seconds = 1.5f;
        private const float SpringSeconds = 0.55f;
        private const float Gravity = 9f;

        private static readonly Color DustColour = new Color(0.86f, 0.78f, 0.62f, 0.9f);
        private static readonly Color FlashColour = new Color(1f, 0.93f, 0.62f, 0.95f);
        private static readonly Color SparkColour = new Color(1f, 0.8f, 0.3f, 1f);
        private static Mesh _plank;
        private static Material _wood;

        private class Bit
        {
            public Transform T;
            public Renderer R;
            public Vector3 Velocity;
            public Vector3 Spin;
            public float Size;
            public float Life;
            public bool Billboard;
            public bool Falls;
            public Color Colour;
        }

        private readonly List<Bit> _bits = new List<Bit>();
        private MaterialPropertyBlock _block;
        private BuildFinish _look;
        private Transform _model;
        private Vector3 _modelScale;
        private float _age;

        public static BuildFinishEffect Play(Transform building, Transform model, Vector2 footprint, BuildFinish look)
        {
            var go = new GameObject("BuildFinish");
            go.transform.position = building.position;
            var effect = go.AddComponent<BuildFinishEffect>();
            effect._block = new MaterialPropertyBlock();
            effect._look = look;
            effect._model = model;
            if (model != null)
            {
                effect._modelScale = model.localScale;
                model.localScale = effect._modelScale * (look == BuildFinish.Dust ? 0.82f : 0.15f);
            }
            var height = 1.2f + 0.25f * (footprint.x + footprint.y);
            if (look == BuildFinish.Dust) effect.Dust(footprint, building.rotation);
            else effect.Fanfare(footprint, height);
            return effect;
        }

        private void Dust(Vector2 footprint, Quaternion turn)
        {
            var hx = footprint.x * 0.5f;
            var hz = footprint.y * 0.5f;
            var count = Mathf.Clamp(Mathf.RoundToInt(6f * (footprint.x + footprint.y)), 14, 40);
            for (var i = 0; i < count; i++)
            {
                var angle = i * Mathf.PI * 2f / count + Random.Range(-0.2f, 0.2f);
                var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var edge = 1f / Mathf.Max(Mathf.Abs(dir.x) / hx, Mathf.Abs(dir.z) / hz);
                var up = Random.Range(0f, 1.2f);
                Mote(dir * (edge * Random.Range(0.5f, 1f)) + Vector3.up * (0.2f + up),
                    dir * Random.Range(1f, 2.2f) + Vector3.up * Random.Range(0.4f, 1.2f),
                    Random.Range(0.8f, 1.4f) * Mathf.Clamp(0.4f * (footprint.x + footprint.y), 0.8f, 2f), Random.Range(0.9f, 1.3f), DustColour);
            }
            // the scaffold's planks, thrown off every side
            var planks = Mathf.Clamp(Mathf.RoundToInt(2f * (footprint.x + footprint.y)), 6, 16);
            for (var i = 0; i < planks; i++)
            {
                var angle = i * Mathf.PI * 2f / planks + Random.Range(-0.3f, 0.3f);
                var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var edge = 1f / Mathf.Max(Mathf.Abs(dir.x) / hx, Mathf.Abs(dir.z) / hz);
                var go = new GameObject("Plank", typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(transform, false);
                go.transform.localPosition = dir * edge + Vector3.up * Random.Range(0.5f, 1.8f);
                go.transform.localRotation = turn * Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), 0f);
                go.GetComponent<MeshFilter>().sharedMesh = Plank();
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = Wood();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                _bits.Add(new Bit
                {
                    T = go.transform, R = renderer, Size = Random.Range(0.8f, 1.2f), Life = Random.Range(0.9f, 1.3f),
                    Velocity = dir * Random.Range(2f, 3.5f) + Vector3.up * Random.Range(2.5f, 4f),
                    Spin = new Vector3(Random.Range(-540f, 540f), Random.Range(-360f, 360f), Random.Range(-540f, 540f)),
                    Falls = true,
                });
            }
        }

        private void Fanfare(Vector2 footprint, float height)
        {
            var reach = 0.5f * Mathf.Max(footprint.x, footprint.y);
            // the flash: one big soft light over the building, and a ring of light on the ground
            Mote(Vector3.up * (height * 0.5f), Vector3.zero, reach * 3.2f, 0.45f, FlashColour);
            Mote(Vector3.up * (height * 0.3f), Vector3.zero, reach * 2.2f, 0.6f, Color.white);
            var count = Mathf.Clamp(Mathf.RoundToInt(6f * (footprint.x + footprint.y)), 16, 36);
            for (var i = 0; i < count; i++)
            {
                var angle = i * Mathf.PI * 2f / count + Random.Range(-0.1f, 0.1f);
                var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var bit = Mote(dir * (reach * 0.6f) + Vector3.up * Random.Range(0.3f, height),
                    dir * Random.Range(2.5f, 4.5f) + Vector3.up * Random.Range(2f, 4.5f),
                    Random.Range(0.18f, 0.32f), Random.Range(0.8f, 1.4f), SparkColour);
                bit.Falls = true;
            }
        }

        private Bit Mote(Vector3 at, Vector3 velocity, float size, float life, Color colour)
        {
            var go = new GameObject("Mote", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = at;
            go.GetComponent<MeshFilter>().sharedMesh = DustPuff.Quad();
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = DustPuff.Material();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var bit = new Bit { T = go.transform, R = renderer, Velocity = velocity, Size = size, Life = life, Billboard = true, Colour = colour };
            _bits.Add(bit);
            Tint(bit, 0f);
            return bit;
        }

        private void Update()
        {
            var dt = Time.unscaledDeltaTime;
            _age += dt;
            Spring();
            var camera = Camera.main;
            foreach (var bit in _bits)
            {
                if (bit.T == null) continue;
                var t = _age / bit.Life;
                if (t >= 1f)
                {
                    if (bit.T.gameObject.activeSelf) bit.T.gameObject.SetActive(false);
                    continue;
                }
                if (bit.Falls) bit.Velocity += Vector3.down * (Gravity * dt);
                else bit.Velocity *= 1f - Mathf.Min(1f, 2.2f * dt);
                bit.T.localPosition += bit.Velocity * dt;
                if (bit.Billboard)
                {
                    var swell = bit.Falls ? 1f - t : 1f + 1.4f * t;
                    bit.T.localScale = Vector3.one * (bit.Size * swell);
                    if (camera != null) bit.T.rotation = camera.transform.rotation;
                    Tint(bit, t);
                }
                else
                {
                    bit.T.localRotation *= Quaternion.Euler(bit.Spin * dt);
                    bit.T.localScale = Vector3.one * (bit.Size * Mathf.Min(1f, (1f - t) * 4f));
                }
            }
            if (_age >= Seconds) Destroy(gameObject);
        }

        private void Tint(Bit bit, float t)
        {
            var colour = bit.Colour;
            // a light comes and goes at once; dust thins out slowly
            colour.a *= bit.Falls ? 1f - t * t : (1f - t) * (1f - t);
            bit.R.GetPropertyBlock(_block);
            _block.SetColor("_BaseColor", colour);
            bit.R.SetPropertyBlock(_block);
        }

        /// <summary>The building up to its full size: squashed and springing up (dust), or popped from small (fanfare).</summary>
        private void Spring()
        {
            if (_model == null) return;
            var t = Mathf.Clamp01(_age / SpringSeconds);
            Vector3 scale;
            if (_look == BuildFinish.Dust)
            {
                // squashed, up past its height, back: wider while it is low
                var y = t < 0.45f ? Mathf.Lerp(0.82f, 1.12f, t / 0.45f) : Mathf.Lerp(1.12f, 1f, (t - 0.45f) / 0.55f);
                var xz = 1f + (1f - y) * 0.5f;
                scale = new Vector3(xz, y, xz);
            }
            else
            {
                // out of the flash: overshoot and settle
                var s = t < 0.6f ? Mathf.Lerp(0.15f, 1.1f, Mathf.SmoothStep(0f, 1f, t / 0.6f)) : Mathf.Lerp(1.1f, 1f, (t - 0.6f) / 0.4f);
                scale = Vector3.one * s;
            }
            _model.localScale = Vector3.Scale(_modelScale, scale);
            if (t >= 1f)
            {
                _model.localScale = _modelScale;
                _model = null;
            }
        }

        private void OnDestroy()
        {
            // cut short (the scene going): the building is left at its own size
            if (_model != null) _model.localScale = _modelScale;
        }

        private static Mesh Plank()
        {
            if (_plank != null) return _plank;
            var bits = new ConstructionLook.MeshBits();
            bits.Box(Vector3.zero, new Vector3(0.28f, 0.05f, 0.9f), Quaternion.identity);
            _plank = bits.ToMesh("ThrownPlank");
            return _plank;
        }

        private static Material Wood() => _wood != null ? _wood : _wood = new Material(RuntimeShaders.Lit) { name = "ThrownPlank", color = new Color(0.55f, 0.37f, 0.2f) };
    }
}
