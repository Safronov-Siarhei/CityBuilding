using CityBuilder.Maps;
using UnityEngine;

namespace CityBuilder.Dungeon
{
    /// <summary>
    /// Rings spreading on a dungeon's water round a walker wading it (the user, 2026-11-04: «нужно
    /// добавить эффекты для ходьбы по воде»). One small pool of flat rings, made once and shared
    /// by every walker: a wading step asks for a ring at most every RingEverySeconds (the walker
    /// keeps its own clock), the ring grows and fades on the water's surface and goes back to the
    /// pool. All rings share one mesh and one material and differ by scale and alpha in a
    /// MaterialPropertyBlock, so they batch; nothing is allocated per step (mobile).
    /// </summary>
    public class DungeonWake : MonoBehaviour
    {
        private const int PoolSize = 24;
        private const float RingEverySeconds = 0.32f;
        private const float RingSeconds = 0.9f;
        private const float StartRadius = 0.18f;
        private const float EndRadius = 0.75f;
        private const float Alpha = 0.55f;
        private const float AboveWater = 0.012f;
        private static readonly Color Foam = new Color(0.86f, 0.94f, 1f, 1f);
        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int LegacyColorId = Shader.PropertyToID("_Color");

        private static DungeonWake _instance;

        private Transform[] _rings;
        private Renderer[] _renderers;
        private float[] _age;
        private float[] _size;
        private int _next;
        private MaterialPropertyBlock _block;

        /// <summary>A walker at `position` took a step: a ring round it when it wades and its clock ran out.</summary>
        public static void Step(Vector3 position, ref float clock)
        {
            if (!DungeonFloor.Active) return;
            clock -= Time.deltaTime;
            if (clock > 0f) return;
            clock = RingEverySeconds;
            if (!DungeonFloor.Wading(position)) return;
            if (_instance == null)
            {
                if (DungeonMode.Instance == null) return;
                _instance = DungeonMode.Instance.gameObject.AddComponent<DungeonWake>();
            }
            _instance.Spawn(position);
        }

        /// <summary>A splash: a ring `size` times a step's, wherever the water is (a serpent diving or coming up).</summary>
        public static void Splash(Vector3 position, float size)
        {
            if (!DungeonFloor.Active) return;
            if (_instance == null)
            {
                if (DungeonMode.Instance == null) return;
                _instance = DungeonMode.Instance.gameObject.AddComponent<DungeonWake>();
            }
            _instance.Spawn(position, size);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Build()
        {
            _rings = new Transform[PoolSize];
            _renderers = new Renderer[PoolSize];
            _age = new float[PoolSize];
            _size = new float[PoolSize];
            _block = new MaterialPropertyBlock();
            var mesh = RingMesh(28, 0.82f);
            var material = UI.MoveOrderMarker.CreateTransparentMaterial(new Color(Foam.r, Foam.g, Foam.b, Alpha));
            for (var i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("WadeRing", typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(transform, false);
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.GetComponent<MeshRenderer>();
                r.sharedMaterial = material;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                _rings[i] = go.transform;
                _renderers[i] = r;
                _age[i] = -1f;
                go.SetActive(false);
            }
        }

        private void Spawn(Vector3 at, float size = 1f)
        {
            if (_rings == null) Build();
            var i = _next;
            _next = (_next + 1) % PoolSize;
            _age[i] = 0f;
            _size[i] = size;
            _rings[i].position = new Vector3(at.x, DungeonFloor.WaterLevel + AboveWater, at.z);
            _rings[i].localScale = Vector3.one * (StartRadius * size);
            _rings[i].gameObject.SetActive(true);
            Paint(i, Alpha);
        }

        private void Update()
        {
            if (_rings == null) return;
            var dt = Time.deltaTime;
            for (var i = 0; i < PoolSize; i++)
            {
                if (_age[i] < 0f) continue;
                _age[i] += dt;
                var t = _age[i] / RingSeconds;
                if (t >= 1f)
                {
                    _age[i] = -1f;
                    _rings[i].gameObject.SetActive(false);
                    continue;
                }
                var eased = 1f - (1f - t) * (1f - t);
                _rings[i].localScale = Vector3.one * (Mathf.Lerp(StartRadius, EndRadius, eased) * _size[i]);
                Paint(i, Alpha * (1f - t));
            }
        }

        private void Paint(int i, float alpha)
        {
            var c = new Color(Foam.r, Foam.g, Foam.b, alpha);
            _block.SetColor(ColorId, c);
            _block.SetColor(LegacyColorId, c);
            _renderers[i].SetPropertyBlock(_block);
        }

        /// <summary>A flat ring of radius 1 (inner radius `inner`), lying in the XZ plane.</summary>
        private static Mesh RingMesh(int sides, float inner)
        {
            var vertices = new Vector3[sides * 2];
            var triangles = new int[sides * 6];
            for (var i = 0; i < sides; i++)
            {
                var a = i * Mathf.PI * 2f / sides;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                vertices[i * 2] = d * inner;
                vertices[i * 2 + 1] = d;
                var n = (i + 1) % sides;
                var k = i * 6;
                triangles[k] = i * 2; triangles[k + 1] = n * 2; triangles[k + 2] = i * 2 + 1;
                triangles[k + 3] = i * 2 + 1; triangles[k + 4] = n * 2; triangles[k + 5] = n * 2 + 1;
            }
            var mesh = new Mesh { name = "WadeRing", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
