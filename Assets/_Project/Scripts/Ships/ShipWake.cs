using UnityEngine;

namespace CityBuilder.Ships
{
    /// <summary>
    /// The water a sailing ship cuts (the user, 2026-11-05: «у плывущих кораблей должен быть эффект
    /// рассечения воды»): foam at the bow and two arms of a V spreading out behind it. Each moving
    /// ship drops a pair of flat foam ovals at its bow every EmitEverySeconds, one a side; an oval
    /// stays where it fell on the water, drifts outwards, widens and fades, so the trail behind the
    /// ship opens into a V. One shared pool of ovals, one mesh, one material, alpha per oval in a
    /// MaterialPropertyBlock -- nothing allocated per frame (mobile).
    /// </summary>
    public class ShipWake : MonoBehaviour
    {
        private const int PoolSize = 96;
        private const float EmitEverySeconds = 0.14f;
        private const float LifeSeconds = 1.8f;
        private const float DriftSpeed = 0.55f;
        private const float Alpha = 0.6f;
        private const float AboveWater = 0.03f;
        private static readonly Color Foam = new Color(0.92f, 0.97f, 1f, 1f);
        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int LegacyColorId = Shader.PropertyToID("_Color");

        private static ShipWake _instance;

        private Transform[] _ovals;
        private Renderer[] _renderers;
        private Vector3[] _drift;
        private float[] _age;
        private float[] _size;
        private int _next;
        private MaterialPropertyBlock _block;

        /// <summary>
        /// A ship sailing at `speedShare` (0..1) of its speed: a pair of ovals at its bow when its
        /// clock is up. `beam` is about half its width, in metres.
        /// </summary>
        public static void Sail(Transform ship, float speedShare, float beam, ref float clock)
        {
            clock -= Time.deltaTime;
            if (clock > 0f || speedShare < 0.15f) return;
            clock = EmitEverySeconds;
            if (_instance == null)
            {
                if (ShipManager.Instance == null) return;
                _instance = ShipManager.Instance.gameObject.AddComponent<ShipWake>();
            }
            var forward = ship.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) return;
            forward.Normalize();
            var right = new Vector3(forward.z, 0f, -forward.x);
            var bow = ship.position + forward * (beam * 2.2f);
            bow.y = ShipManager.WaterLevel + AboveWater;
            var size = Mathf.Lerp(0.6f, 1f, speedShare) * beam;
            _instance.Spawn(bow + right * (beam * 0.5f), right * DriftSpeed, size);
            _instance.Spawn(bow - right * (beam * 0.5f), -right * DriftSpeed, size);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Build()
        {
            _ovals = new Transform[PoolSize];
            _renderers = new Renderer[PoolSize];
            _drift = new Vector3[PoolSize];
            _age = new float[PoolSize];
            _size = new float[PoolSize];
            _block = new MaterialPropertyBlock();
            var mesh = Disc(14);
            var material = UI.MoveOrderMarker.CreateTransparentMaterial(new Color(Foam.r, Foam.g, Foam.b, Alpha));
            for (var i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("WakeFoam", typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(transform, false);
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.GetComponent<MeshRenderer>();
                r.sharedMaterial = material;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                go.SetActive(false);
                _ovals[i] = go.transform;
                _renderers[i] = r;
                _age[i] = LifeSeconds;
            }
        }

        private void Spawn(Vector3 at, Vector3 drift, float size)
        {
            if (_ovals == null) Build();
            var i = _next;
            _next = (_next + 1) % PoolSize;
            _ovals[i].position = at;
            _ovals[i].rotation = Quaternion.LookRotation(drift.sqrMagnitude > 0.0001f ? drift.normalized : Vector3.forward, Vector3.up);
            _drift[i] = drift;
            _age[i] = 0f;
            _size[i] = size;
            _ovals[i].gameObject.SetActive(true);
            Paint(i);
        }

        private void Update()
        {
            if (_ovals == null) return;
            var t = Time.deltaTime;
            for (var i = 0; i < PoolSize; i++)
            {
                if (_age[i] >= LifeSeconds) continue;
                _age[i] += t;
                if (_age[i] >= LifeSeconds)
                {
                    _ovals[i].gameObject.SetActive(false);
                    continue;
                }
                // slowing as it spreads
                _ovals[i].position += _drift[i] * (t * (1f - _age[i] / LifeSeconds));
                Paint(i);
            }
        }

        private void Paint(int i)
        {
            var k = _age[i] / LifeSeconds;
            var grow = Mathf.Lerp(0.5f, 1.6f, k) * _size[i];
            // long along the way it drifts out, thin across: a streak of foam
            _ovals[i].localScale = new Vector3(grow * 0.55f, 1f, grow);
            _block.SetColor(ColorId, new Color(Foam.r, Foam.g, Foam.b, Alpha * (1f - k) * (1f - k)));
            _block.SetColor(LegacyColorId, new Color(Foam.r, Foam.g, Foam.b, Alpha * (1f - k) * (1f - k)));
            _renderers[i].SetPropertyBlock(_block);
        }

        /// <summary>A flat disc of radius 0.5 lying on the water, facing up.</summary>
        private static Mesh Disc(int sides)
        {
            var vertices = new Vector3[sides + 1];
            var triangles = new int[sides * 3];
            vertices[0] = Vector3.zero;
            for (var k = 0; k < sides; k++)
            {
                var a = k * Mathf.PI * 2f / sides;
                vertices[k + 1] = new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f);
                triangles[k * 3] = 0;
                triangles[k * 3 + 1] = 1 + (k + 1) % sides;
                triangles[k * 3 + 2] = 1 + k;
            }
            var mesh = new Mesh { name = "WakeFoam", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
