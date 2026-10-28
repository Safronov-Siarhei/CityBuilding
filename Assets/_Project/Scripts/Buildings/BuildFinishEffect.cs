using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// The moment a construction is done (ConstructionLook.Complete), hiding the site going and the
    /// building standing (the user, 2026-10-28: the dust one, made «сочнее»):
    ///
    /// - a burst of puffy dust round the footprint and up its walls, two shades, swelling and settling;
    /// - a ring of dust running out along the ground;
    /// - the site thrown off: planks, poles, scraps of the green net and stones, tumbling, bouncing
    ///   once or twice on the ground;
    /// - the building squashed and springing up on a wobble that dies away;
    /// - a few twinkles over the roof as the dust clears.
    ///
    /// Billboards and small boxes, as DustPuff is, so no particle shader is needed in the build;
    /// about two seconds, then it removes itself.
    /// </summary>
    public class BuildFinishEffect : MonoBehaviour
    {
        private const float Seconds = 2f;
        private const float Gravity = 11f;

        private static readonly Color DustLight = new Color(0.95f, 0.89f, 0.76f, 0.95f);
        private static readonly Color DustDark = new Color(0.74f, 0.66f, 0.54f, 0.95f);
        private static readonly Color Twinkle = new Color(1f, 0.95f, 0.7f, 1f);
        private static Mesh _quad, _box;
        private static Material _puff, _ring, _star, _wood, _netScrap, _stone;

        private enum Kind { Puff, Ring, Star, Debris }

        private class Bit
        {
            public Kind Kind;
            public Transform T;
            public Renderer R;
            public Vector3 Velocity;
            public Vector3 Spin;
            public float Size;
            public float Grow;
            public float Delay;
            public float Life;
            public float Turn;
            public Color Colour;
            public Vector3 Own;
        }

        private readonly List<Bit> _bits = new List<Bit>();
        private MaterialPropertyBlock _block;
        private Transform _model;
        private Vector3 _modelScale;
        /// <summary>How deep the building's spring goes: a new building's full squash, an upgrade's nod.</summary>
        private float _squash = 0.3f;
        private float _age;

        public static BuildFinishEffect Play(Transform building, Transform model, Vector2 footprint, float height)
        {
            var go = new GameObject("BuildFinish");
            go.transform.SetPositionAndRotation(building.position, building.rotation);
            var effect = go.AddComponent<BuildFinishEffect>();
            effect._block = new MaterialPropertyBlock();
            effect._model = model;
            if (model != null) effect._modelScale = model.localScale;
            effect.Burst(footprint, Mathf.Max(1f, height));
            effect.Spring();
            return effect;
        }

        /// <summary>
        /// An upgrade done (the user, 2026-10-28: «нужно всё же какой-то эффектик при улучшении»),
        /// lighter than a new building's: a little dust at the foot, a few of the scaffold's planks
        /// thrown off, gold sparks spiralling up the walls to the roof, the building nodding once.
        /// </summary>
        public static BuildFinishEffect PlayUpgrade(Transform building, Transform model, Vector2 footprint, float height)
        {
            var go = new GameObject("UpgradeFinish");
            go.transform.SetPositionAndRotation(building.position, building.rotation);
            var effect = go.AddComponent<BuildFinishEffect>();
            effect._block = new MaterialPropertyBlock();
            effect._model = model;
            effect._squash = 0.1f;
            if (model != null) effect._modelScale = model.localScale;
            effect.Sparkle(footprint, Mathf.Max(1f, height));
            effect.Spring();
            return effect;
        }

        private void Sparkle(Vector2 footprint, float height)
        {
            var hx = footprint.x * 0.5f;
            var hz = footprint.y * 0.5f;
            var span = footprint.x + footprint.y;
            var scale = Mathf.Clamp(0.25f * span, 0.6f, 1.4f);

            var ground = Mathf.Clamp(Mathf.RoundToInt(3f * span), 8, 20);
            for (var i = 0; i < ground; i++)
            {
                var dir = Around(i, ground, 0.25f);
                Add(Kind.Puff, Edge(dir, hx, hz) + Vector3.up * 0.15f, dir * Random.Range(1.2f, 2f) + Vector3.up * 0.3f,
                    Random.Range(0.7f, 1f) * scale, 1.3f, 0f, Random.Range(0.7f, 1f), DustColour());
            }
            Throw(Mathf.Clamp(Mathf.RoundToInt(1.2f * span), 4, 8), hx, hz, height, Plank(), Wood(), new Vector3(0.24f, 0.05f, 0.9f));
            Throw(Mathf.Clamp(Mathf.RoundToInt(0.6f * span), 2, 4), hx, hz, height, Plank(), Wood(), new Vector3(0.07f, 0.07f, 1.8f));

            // the sparks: round the walls and up, each a turn of a spiral
            var sparks = Mathf.Clamp(Mathf.RoundToInt(4f * span), 12, 28);
            for (var i = 0; i < sparks; i++)
            {
                var dir = Around(i, sparks, 0.1f);
                var rise = Random.Range(0.6f, 1f) * height / 0.9f;
                var swirl = new Vector3(-dir.z, 0f, dir.x) * Random.Range(0.6f, 1.2f);
                Add(Kind.Star, Edge(dir, hx, hz) * 0.95f + Vector3.up * Random.Range(0.1f, height * 0.4f), Vector3.up * rise + swirl,
                    Random.Range(0.3f, 0.5f), 1f, i * 0.025f, Random.Range(0.75f, 1f), Twinkle);
            }
            Add(Kind.Ring, Vector3.up * 0.06f, Vector3.zero, Mathf.Max(footprint.x, footprint.y) * 1.05f, 1.6f, 0f, 0.45f, Twinkle);
        }

        private void Burst(Vector2 footprint, float height)
        {
            var hx = footprint.x * 0.5f;
            var hz = footprint.y * 0.5f;
            var span = footprint.x + footprint.y;
            var scale = Mathf.Clamp(0.35f * span, 0.9f, 2.2f);

            // the ground cloud, round the footprint's edge, rolling out
            var ground = Mathf.Clamp(Mathf.RoundToInt(7f * span), 16, 44);
            for (var i = 0; i < ground; i++)
            {
                var dir = Around(i, ground, 0.2f);
                Add(Kind.Puff, Edge(dir, hx, hz) * Random.Range(0.7f, 1f) + Vector3.up * Random.Range(0.1f, 0.5f),
                    dir * Random.Range(2.2f, 3.8f) + Vector3.up * Random.Range(0.2f, 0.7f),
                    Random.Range(0.9f, 1.4f) * scale, 1.6f, 0f, Random.Range(1.1f, 1.6f), DustColour());
            }
            // the column, up the walls, so the swap is under cover
            var column = Mathf.Clamp(Mathf.RoundToInt(4.5f * span), 12, 30);
            for (var i = 0; i < column; i++)
            {
                var dir = Around(i, column, 0.5f);
                var at = Edge(dir, hx, hz) * Random.Range(0.3f, 0.9f) + Vector3.up * Random.Range(0.3f, height);
                Add(Kind.Puff, at, dir * Random.Range(0.6f, 1.4f) + Vector3.up * Random.Range(0.6f, 1.6f),
                    Random.Range(1.1f, 1.6f) * scale, 1.2f, 0f, Random.Range(0.8f, 1.15f), DustColour());
            }
            // a second, slower puff a moment later, so it does not stop dead
            for (var i = 0; i < ground / 2; i++)
            {
                var dir = Around(i, ground / 2, 0.4f);
                Add(Kind.Puff, Edge(dir, hx, hz) * 1.1f + Vector3.up * 0.2f, dir * Random.Range(1f, 1.8f) + Vector3.up * 0.35f,
                    Random.Range(0.8f, 1.2f) * scale, 1.4f, 0.15f, Random.Range(1f, 1.4f), DustColour());
            }

            // the ring along the ground
            Add(Kind.Ring, Vector3.up * 0.06f, Vector3.zero, Mathf.Max(footprint.x, footprint.y) * 1.1f, 2.4f + 3f / span, 0f, 0.55f, DustLight);

            // the site thrown off
            Throw(Mathf.Clamp(Mathf.RoundToInt(2.2f * span), 6, 16), hx, hz, height, Plank(), Wood(), new Vector3(0.24f, 0.05f, 0.9f));
            Throw(Mathf.Clamp(Mathf.RoundToInt(1.2f * span), 3, 8), hx, hz, height, Plank(), Wood(), new Vector3(0.07f, 0.07f, 1.8f));
            Throw(Mathf.Clamp(Mathf.RoundToInt(1.5f * span), 4, 10), hx, hz, height, Plank(), NetScrap(), new Vector3(0.6f, 0.02f, 0.45f));
            Throw(Mathf.Clamp(Mathf.RoundToInt(2f * span), 5, 12), hx, hz, 0.4f, Plank(), Stone(), new Vector3(0.16f, 0.13f, 0.15f));

            // twinkles over the roof as it clears
            for (var i = 0; i < 4; i++)
            {
                var at = new Vector3(Random.Range(-hx, hx) * 0.8f, height * Random.Range(0.75f, 1.05f), Random.Range(-hz, hz) * 0.8f);
                Add(Kind.Star, at, Vector3.up * 0.2f, Random.Range(0.45f, 0.7f), 1f, 0.55f + i * 0.12f, 0.45f, Twinkle);
            }
        }

        private static Vector3 Around(int i, int count, float jitter)
        {
            var angle = i * Mathf.PI * 2f / count + Random.Range(-jitter, jitter);
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }

        /// <summary>Where a ray out of the middle meets the footprint's edge.</summary>
        private static Vector3 Edge(Vector3 dir, float hx, float hz) =>
            dir / Mathf.Max(Mathf.Abs(dir.x) / Mathf.Max(0.1f, hx), Mathf.Abs(dir.z) / Mathf.Max(0.1f, hz));

        private static Color DustColour() => Color.Lerp(DustLight, DustDark, Random.Range(0f, 1f) * Random.Range(0.3f, 1f));

        private void Throw(int count, float hx, float hz, float height, Mesh mesh, Material material, Vector3 size)
        {
            for (var i = 0; i < count; i++)
            {
                var dir = Around(i, count, 0.35f);
                var go = new GameObject("Debris", typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Edge(dir, hx, hz) + Vector3.up * Random.Range(0.2f, height);
                go.transform.localRotation = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                _bits.Add(new Bit
                {
                    Kind = Kind.Debris, T = go.transform, R = renderer, Life = Random.Range(1.2f, 1.7f),
                    Velocity = dir * Random.Range(2.2f, 4.2f) + Vector3.up * Random.Range(3f, 5.5f),
                    Spin = new Vector3(Random.Range(-600f, 600f), Random.Range(-400f, 400f), Random.Range(-600f, 600f)),
                    Own = size,
                });
                go.transform.localScale = size;
            }
        }

        private void Add(Kind kind, Vector3 at, Vector3 velocity, float size, float grow, float delay, float life, Color colour)
        {
            var go = new GameObject(kind.ToString(), typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = at;
            go.GetComponent<MeshFilter>().sharedMesh = Quad();
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = kind == Kind.Ring ? RingMaterial() : kind == Kind.Star ? StarMaterial() : PuffMaterial();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var bit = new Bit
            {
                Kind = kind, T = go.transform, R = renderer, Velocity = velocity, Size = size, Grow = grow, Delay = delay,
                Life = life, Colour = colour, Turn = Random.Range(-90f, 90f),
            };
            _bits.Add(bit);
            go.transform.localScale = Vector3.zero;
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
                var t = (_age - bit.Delay) / bit.Life;
                if (t < 0f) continue;
                if (t >= 1f)
                {
                    if (bit.T.gameObject.activeSelf) bit.T.gameObject.SetActive(false);
                    continue;
                }
                switch (bit.Kind)
                {
                    case Kind.Puff:
                        // fast out, then hanging in the air
                        bit.Velocity *= 1f - Mathf.Min(1f, 3.2f * dt);
                        bit.T.localPosition += bit.Velocity * dt;
                        Face(bit, camera, bit.Size * (0.6f + bit.Grow * (1f - (1f - t) * (1f - t))), bit.Turn * t);
                        Tint(bit, Mathf.SmoothStep(0f, 1f, t / 0.08f) * Mathf.Pow(1f - t, 1.6f));
                        break;
                    case Kind.Ring:
                        var r = bit.Size * (0.6f + bit.Grow * (1f - (1f - t) * (1f - t)));
                        bit.T.localRotation = Quaternion.Euler(90f, 0f, 0f);
                        bit.T.localScale = new Vector3(r, r, 1f);
                        Tint(bit, 0.85f * (1f - t) * (1f - t));
                        break;
                    case Kind.Star:
                        var pulse = Mathf.Sin(t * Mathf.PI);
                        bit.T.localPosition += bit.Velocity * dt;
                        Face(bit, camera, bit.Size * pulse, 180f * t);
                        Tint(bit, pulse);
                        break;
                    default:
                        Fly(bit, dt, t);
                        break;
                }
            }
            if (_age >= Seconds) Destroy(gameObject);
        }

        private static void Face(Bit bit, Camera camera, float size, float roll)
        {
            bit.T.localScale = Vector3.one * size;
            if (camera != null) bit.T.rotation = camera.transform.rotation * Quaternion.Euler(0f, 0f, roll);
        }

        /// <summary>A thrown piece: falls, tumbles, bounces off the ground losing most of its speed, shrinks away at the end.</summary>
        private static void Fly(Bit bit, float dt, float t)
        {
            bit.Velocity += Vector3.down * (Gravity * dt);
            var p = bit.T.localPosition + bit.Velocity * dt;
            if (p.y < 0.03f && bit.Velocity.y < 0f)
            {
                p.y = 0.03f;
                bit.Velocity = new Vector3(bit.Velocity.x * 0.45f, -bit.Velocity.y * 0.32f, bit.Velocity.z * 0.45f);
                bit.Spin *= 0.4f;
            }
            bit.T.localPosition = p;
            bit.T.localRotation *= Quaternion.Euler(bit.Spin * dt);
            bit.T.localScale = bit.Own * Mathf.Clamp01((1f - t) * 4f);
        }

        private void Tint(Bit bit, float alpha)
        {
            var colour = bit.Colour;
            colour.a *= alpha;
            bit.R.GetPropertyBlock(_block);
            _block.SetColor("_BaseColor", colour);
            bit.R.SetPropertyBlock(_block);
        }

        /// <summary>The building squashed, springing up past its height and wobbling to rest.</summary>
        private void Spring()
        {
            if (_model == null) return;
            var y = 1f - _squash * Mathf.Exp(-5.5f * _age) * Mathf.Cos(13f * _age);
            var xz = 1f + (1f - y) * 0.45f;
            _model.localScale = Vector3.Scale(_modelScale, new Vector3(xz, y, xz));
            if (_age >= 1.1f)
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

        private static Mesh Quad() => _quad != null ? _quad : _quad = DustPuff.Quad();

        /// <summary>A unit box, scaled to each piece.</summary>
        private static Mesh Plank()
        {
            if (_box != null) return _box;
            var bits = new ConstructionLook.MeshBits();
            bits.Box(Vector3.zero, Vector3.one, Quaternion.identity);
            _box = bits.ToMesh("Debris");
            return _box;
        }

        private static Material Wood() => _wood != null ? _wood : _wood = new Material(RuntimeShaders.Lit) { name = "ThrownWood", color = new Color(0.55f, 0.37f, 0.2f) };

        private static Material NetScrap() => _netScrap != null ? _netScrap : _netScrap = new Material(RuntimeShaders.Lit) { name = "ThrownNet", color = new Color(0.33f, 0.47f, 0.39f) };

        private static Material Stone() => _stone != null ? _stone : _stone = new Material(RuntimeShaders.Lit) { name = "ThrownStone", color = new Color(0.6f, 0.58f, 0.54f) };

        /// <summary>A cloud of dust: lobes, lit from above (lighter at the top, shaded under), soft at the edge.</summary>
        private static Material PuffMaterial()
        {
            if (_puff != null) return _puff;
            const int size = 64;
            var lobes = new[] { new Vector3(0.5f, 0.42f, 0.34f), new Vector3(0.3f, 0.5f, 0.24f), new Vector3(0.7f, 0.5f, 0.24f), new Vector3(0.42f, 0.66f, 0.22f), new Vector3(0.62f, 0.68f, 0.2f), new Vector3(0.5f, 0.3f, 0.26f) };
            return _puff = Transparent("DustCloud", Paint(size, (u, v) =>
            {
                var a = 0f;
                foreach (var lobe in lobes)
                {
                    var d = Vector2.Distance(new Vector2(u, v), new Vector2(lobe.x, lobe.y)) / lobe.z;
                    a = Mathf.Max(a, Mathf.Clamp01(1.25f - d * d * 1.25f));
                }
                var shade = Mathf.Lerp(0.76f, 1f, Mathf.Clamp01(v * 1.4f - 0.1f));
                return new Color(shade, shade, shade, Mathf.SmoothStep(0f, 1f, a));
            }));
        }

        /// <summary>A ring, soft both sides.</summary>
        private static Material RingMaterial()
        {
            if (_ring != null) return _ring;
            return _ring = Transparent("DustRing", Paint(64, (u, v) =>
            {
                var d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
                var a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.78f) / 0.2f);
                return new Color(1f, 1f, 1f, a * a);
            }));
        }

        /// <summary>A four-pointed twinkle with a soft core.</summary>
        private static Material StarMaterial()
        {
            if (_star != null) return _star;
            return _star = Transparent("Twinkle", Paint(32, (u, v) =>
            {
                var x = Mathf.Abs(u - 0.5f) * 2f;
                var y = Mathf.Abs(v - 0.5f) * 2f;
                var rays = Mathf.Max(Mathf.Clamp01(1f - x * 8f) * (1f - y), Mathf.Clamp01(1f - y * 8f) * (1f - x));
                var core = Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y) * 2.5f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(rays + core));
            }));
        }

        private static Texture2D Paint(int size, System.Func<float, float, Color> pixel)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++) texture.SetPixel(x, y, pixel((x + 0.5f) / size, (y + 0.5f) / size));
            }
            texture.Apply();
            return texture;
        }

        private static Material Transparent(string name, Texture2D texture)
        {
            var material = new Material(RuntimeShaders.Unlit) { name = name, color = Color.white, mainTexture = texture };
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            return material;
        }
    }
}
