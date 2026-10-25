using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// The King back on his feet (the user, 2026-10-22: show the player he has come back): a
    /// column of gold light over him that rises and fades, and two gold rings spreading over the
    /// ground. Made once, reused for every return; nothing runs between them.
    /// </summary>
    public class RespawnFlash : MonoBehaviour
    {
        private const float Seconds = 1.4f;
        private const float PillarHeight = 4f;
        private const float PillarWidth = 0.7f;
        private const float RingEndRadius = 2.2f;
        private const int RingPoints = 32;
        private static readonly Color Gold = new Color(1f, 0.85f, 0.35f, 1f);

        private static RespawnFlash _instance;

        private Transform _pillar;
        private Material _pillarMaterial;
        private LineRenderer[] _rings;
        private float _clock = -1f;
        private Transform _follow;

        public static void Play(Vector3 at) => Play(at, null);

        /// <summary>On someone: the light goes along with him while it lasts (the user, 2026-10-25).</summary>
        public static void Play(Transform follow) => Play(follow.position, follow);

        private static void Play(Vector3 at, Transform follow)
        {
            if (_instance == null) _instance = new GameObject("RespawnFlash").AddComponent<RespawnFlash>().Build();
            _instance._follow = follow;
            _instance.transform.position = at;
            _instance._clock = 0f;
            _instance.gameObject.SetActive(true);
            _instance.Tick(0f);
        }

        private RespawnFlash Build()
        {
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(pillar.GetComponent<Collider>());
            pillar.name = "Pillar";
            pillar.transform.SetParent(transform, false);
            _pillarMaterial = MoveOrderMarker.CreateTransparentMaterial(Gold);
            var renderer = pillar.GetComponent<Renderer>();
            renderer.sharedMaterial = _pillarMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _pillar = pillar.transform;

            _rings = new LineRenderer[2];
            for (var i = 0; i < _rings.Length; i++)
            {
                var ring = new GameObject($"Ring{i}").AddComponent<LineRenderer>();
                ring.transform.SetParent(transform, false);
                ring.useWorldSpace = false;
                ring.loop = true;
                ring.positionCount = RingPoints;
                ring.widthMultiplier = 0.12f;
                ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                ring.receiveShadows = false;
                ring.material = MoveOrderMarker.CreateTransparentMaterial(Gold);
                _rings[i] = ring;
            }
            return this;
        }

        private void Update()
        {
            if (_clock < 0f) return;
            _clock += Time.deltaTime;
            if (_follow != null) transform.position = _follow.position;
            Tick(_clock / Seconds);
        }

        private void Tick(float t)
        {
            if (t >= 1f)
            {
                _clock = -1f;
                gameObject.SetActive(false);
                return;
            }

            // the column shoots up in the first fifth, then thins and fades
            var rise = Mathf.Clamp01(t / 0.2f);
            var width = PillarWidth * (1f - 0.6f * t);
            _pillar.localScale = new Vector3(width, PillarHeight * 0.5f * rise, width);
            _pillar.localPosition = new Vector3(0f, PillarHeight * 0.5f * rise, 0f);
            var c = Gold;
            c.a = 0.75f * (1f - t);
            _pillarMaterial.color = c;

            for (var i = 0; i < _rings.Length; i++)
            {
                var local = Mathf.Clamp01(t * 1.3f - i * 0.25f);
                var spread = 1f - (1f - local) * (1f - local);
                var radius = Mathf.Lerp(0.2f, RingEndRadius, spread);
                for (var p = 0; p < RingPoints; p++)
                {
                    var a = p / (float)RingPoints * Mathf.PI * 2f;
                    _rings[i].SetPosition(p, new Vector3(Mathf.Cos(a) * radius, 0.05f, Mathf.Sin(a) * radius));
                }
                var rc = Gold;
                rc.a = local <= 0f ? 0f : 0.9f * (1f - local);
                _rings[i].material.color = rc;
            }
        }
    }
}
