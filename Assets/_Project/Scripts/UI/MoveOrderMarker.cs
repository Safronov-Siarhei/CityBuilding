using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.UI
{
    /// <summary>
    /// What a citizen's order looks like on the ground (see CitizenSelector), replacing the flat
    /// yellow square that used to mark the target cell:
    ///
    /// - a RIPPLE of two rings spreading from the tapped point and fading -- the "order taken"
    ///   beat. Red, and nothing else, for a tap with nowhere valid to go.
    /// - a FLAG planted at the destination (Models/Buildings/OrderFlag.fbx, made in Blender with
    ///   the rest of the set), swaying while the citizen walks to it. A gather order has no flag --
    ///   the tree or boulder is the target -- and gets a ring round its foot instead.
    /// - a TRAIL of dots along the route the citizen will actually take: the agent's own NavMesh
    ///   corners (CitizenAgent.Route), not a second path query that could disagree with it. The
    ///   dots the citizen has walked past shrink away, so what is left is the way still to go.
    ///
    /// Mobile cost, deliberately small: every object is made once and reused; the trail is laid
    /// out only when the agent plans a route (CitizenAgent.RouteVersion), with one ground probe
    /// per dot; per frame it is a distance along the route and a scale per dot. All dots share one
    /// material and differ only in scale, so they batch. Only the selected citizen has a trail.
    /// </summary>
    public class MoveOrderMarker : MonoBehaviour
    {
        // Colours of the parchment/gold skin: cream for "yes", the UI's red for "no".
        private static readonly Color OkColor = new Color(1f, 0.965f, 0.84f, 1f);
        private static readonly Color RefusedColor = new Color(0.9f, 0.3f, 0.25f, 1f);

        private const float LiftOffGround = 0.03f;

        private const int RippleCount = 2;
        private const float RippleSeconds = 0.7f;
        private const float RefusedRippleSeconds = 0.9f;
        private const float RippleStagger = 0.16f;
        private const float RippleStartRadius = 0.12f;
        private const float RippleEndRadius = 0.8f;

        private const float FlagRingRadius = 0.22f;
        private const float GatherRingRadius = 0.5f;
        private const float RingAlpha = 0.75f;

        private const float PlantSeconds = 0.35f;
        private const float PlantDrop = 0.45f;
        private const float LeaveSeconds = 0.2f;
        private const float SwayDegrees = 9f;
        private const float SwaySpeed = 2.4f;

        private const int MaxDots = 64;
        private const float DotSpacing = 0.4f;
        private const float DotRadius = 0.05f;
        private const float DotAlpha = 0.8f;
        // Dots this close ahead of the citizen are shrinking; the ones behind are gone.
        private const float DotFadeDistance = 0.35f;
        // Where the trail stops short of its end: the flag's own ring, or the tree's trunk.
        private const float TrailStopShortOfFlag = 0.25f;
        private const float TrailStopShortOfNode = 0.55f;
        private const int MaxRoutePoints = 64;

        private GameObject _flagPrefab;
        private Transform _flag;
        private Ring _targetRing;
        private Ripple[] _ripples;
        private Transform[] _dots;
        private Material _dotMaterial;

        private CitizenAgent _citizen;
        private bool _plantFlag;
        private Vector3 _target;
        private float _flagClock;
        private float _leaveClock = -1f;

        private int _builtRouteVersion = -1;
        private int _builtRouteStartIndex;
        private readonly Vector3[] _routePoints = new Vector3[MaxRoutePoints];
        private readonly float[] _routeDistances = new float[MaxRoutePoints];
        private int _routePointCount;
        private readonly float[] _dotDistances = new float[MaxDots];
        private int _dotCount;

        private static Mesh _discMesh;
        private static Mesh _ringMesh;

        /// <summary>The flag model comes from the scene (SetupProject wires it), so it travels into the build with it.</summary>
        public void Configure(GameObject flagPrefab)
        {
            _flagPrefab = flagPrefab;
        }

        /// <summary>True while an order is on show: the flag (or the gather ring) and the trail.</summary>
        public bool IsShowingOrder => _citizen != null;

        /// <summary>
        /// An accepted order: the citizen is already walking (MoveTo/GatherFrom has run), so its
        /// route is there to draw. plantFlag is false for a gather order, whose target is the node.
        /// </summary>
        public void ShowOrder(CitizenAgent citizen, Vector3 target, bool plantFlag)
        {
            EnsureBuilt();

            _citizen = citizen;
            _plantFlag = plantFlag;
            _target = OnGround(target);
            _builtRouteVersion = -1;
            _leaveClock = -1f;

            PlayRipple(_target, OkColor, RippleSeconds);

            if (plantFlag && _flag != null)
            {
                _flagClock = 0f;
                _flag.gameObject.SetActive(true);
                PoseFlag();
            }
            else if (_flag != null)
            {
                _flag.gameObject.SetActive(false);
            }

            _targetRing.Show(_target, plantFlag ? FlagRingRadius : GatherRingRadius, OkColor, RingAlpha);
            RebuildTrailIfRerouted();
        }

        /// <summary>A tap that could not become an order: a red ripple, and whatever order was already on show stays.</summary>
        public void ShowRefused(Vector3 point)
        {
            EnsureBuilt();
            PlayRipple(OnGround(point), RefusedColor, RefusedRippleSeconds);
        }

        /// <summary>Takes the order's flag and trail down (the citizen arrived, gave up, or was put down). Ripples already playing finish.</summary>
        public void HideOrder()
        {
            if (_citizen == null) return;
            _citizen = null;
            HideTrail();
            if (_targetRing != null) _targetRing.Hide();

            // The flag pops out rather than vanishing, unless it was never up.
            if (_flag != null && _flag.gameObject.activeSelf && _plantFlag) _leaveClock = 0f;
            else if (_flag != null) _flag.gameObject.SetActive(false);
        }

        private void Update()
        {
            var dt = Time.unscaledDeltaTime;

            if (_ripples != null)
            {
                foreach (var ripple in _ripples) ripple.Tick(dt);
            }

            if (_leaveClock >= 0f) TickLeaving(dt);

            if (_citizen == null) return;
            if (!_citizen.IsExecutingOrder)
            {
                HideOrder();
                return;
            }

            if (_plantFlag && _flag != null)
            {
                _flagClock += dt;
                PoseFlag();
            }

            // A gatherer stops short of its tree (CitizenAgent.NodeReachDistance) and chops from
            // there; the last stretch of trail would otherwise sit at its feet the whole time.
            if (!_plantFlag && HorizontalDistance(_citizen.transform.position, _target) <= CitizenAgent.NodeReachDistance)
            {
                HideTrail();
                _builtRouteVersion = _citizen.RouteVersion;
                return;
            }

            RebuildTrailIfRerouted();
            ConsumeTrail();
        }

        private void OnDestroy()
        {
            if (_dotMaterial != null) Destroy(_dotMaterial);
            _targetRing?.Destroy();
            if (_ripples != null)
            {
                foreach (var ripple in _ripples) ripple.Destroy();
            }
        }

        // --- the flag ---------------------------------------------------------------------------

        private void PoseFlag()
        {
            // Planting: drops in from above and settles with a small overshoot.
            var plant = Mathf.Clamp01(_flagClock / PlantSeconds);
            var eased = EaseOutBack(plant);
            var drop = (1f - Mathf.Clamp01(eased)) * PlantDrop;
            _flag.position = _target + new Vector3(0f, drop, 0f);
            _flag.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, eased);

            // Then a lazy sway about the stake, as if in a breeze.
            var sway = Mathf.Sin(_flagClock * SwaySpeed) * SwayDegrees * plant;
            var lean = Mathf.Sin(_flagClock * SwaySpeed * 0.7f + 1f) * 2.5f * plant;
            _flag.rotation = Quaternion.Euler(0f, sway, lean);
        }

        private void TickLeaving(float dt)
        {
            _leaveClock += dt;
            var t = Mathf.Clamp01(_leaveClock / LeaveSeconds);
            if (_flag != null) _flag.localScale = Vector3.one * (1f - t * t);
            if (t < 1f) return;
            _leaveClock = -1f;
            // A new order may have planted the flag again meanwhile: that one is not leaving.
            if (_flag != null && _citizen == null) _flag.gameObject.SetActive(false);
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.7f;
            const float c3 = c1 + 1f;
            var u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        // --- the trail --------------------------------------------------------------------------

        private void RebuildTrailIfRerouted()
        {
            if (_citizen == null || _citizen.RouteVersion == _builtRouteVersion) return;
            _builtRouteVersion = _citizen.RouteVersion;
            _builtRouteStartIndex = _citizen.RouteIndex;

            // The polyline: where the citizen stands now, then every corner still ahead of it.
            var route = _citizen.Route;
            _routePointCount = 0;
            AddRoutePoint(_citizen.transform.position);
            for (var i = _builtRouteStartIndex; i < route.Count && _routePointCount < MaxRoutePoints; i++)
            {
                AddRoutePoint(route[i]);
            }

            var total = _routeDistances[_routePointCount - 1];
            var end = total - (_plantFlag ? TrailStopShortOfFlag : TrailStopShortOfNode);
            // A long walk spreads the same number of dots further apart rather than running out.
            var spacing = Mathf.Max(DotSpacing, end / MaxDots);

            _dotCount = 0;
            var segment = 1;
            for (var distance = spacing; distance < end && _dotCount < MaxDots; distance += spacing)
            {
                while (segment < _routePointCount - 1 && _routeDistances[segment] < distance) segment++;
                var a = _routePoints[segment - 1];
                var b = _routePoints[segment];
                var length = _routeDistances[segment] - _routeDistances[segment - 1];
                var t = length > 0.0001f ? (distance - _routeDistances[segment - 1]) / length : 1f;

                var dot = _dots[_dotCount];
                dot.position = OnGround(Vector3.Lerp(a, b, t));
                dot.localScale = Vector3.one * DotRadius;
                dot.gameObject.SetActive(true);
                _dotDistances[_dotCount] = distance;
                _dotCount++;
            }
            for (var i = _dotCount; i < MaxDots; i++) _dots[i].gameObject.SetActive(false);

            ConsumeTrail();
        }

        private void AddRoutePoint(Vector3 point)
        {
            var index = _routePointCount++;
            _routePoints[index] = point;
            _routeDistances[index] = index == 0 ? 0f : _routeDistances[index - 1] + HorizontalDistance(_routePoints[index - 1], point);
        }

        /// <summary>How far along the trail the citizen has got, from the corner it is heading for.</summary>
        private float WalkedDistance()
        {
            var heading = _citizen.RouteIndex - _builtRouteStartIndex + 1;
            if (heading >= _routePointCount) return _routeDistances[_routePointCount - 1];
            if (heading < 1) return 0f;
            var left = HorizontalDistance(_citizen.transform.position, _routePoints[heading]);
            return Mathf.Clamp(_routeDistances[heading] - left, _routeDistances[heading - 1], _routeDistances[heading]);
        }

        private void ConsumeTrail()
        {
            if (_dotCount == 0) return;
            var walked = WalkedDistance();
            for (var i = 0; i < _dotCount; i++)
            {
                var ahead = _dotDistances[i] - walked;
                var dot = _dots[i];
                if (ahead <= 0f)
                {
                    if (dot.gameObject.activeSelf) dot.gameObject.SetActive(false);
                    continue;
                }
                var size = ahead < DotFadeDistance ? ahead / DotFadeDistance : 1f;
                dot.localScale = Vector3.one * (DotRadius * size);
            }
        }

        private void HideTrail()
        {
            _dotCount = 0;
            _builtRouteVersion = -1;
            if (_dots == null) return;
            foreach (var dot in _dots) dot.gameObject.SetActive(false);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        // --- the ripple -------------------------------------------------------------------------

        private void PlayRipple(Vector3 point, Color color, float seconds)
        {
            for (var i = 0; i < _ripples.Length; i++) _ripples[i].Play(point, color, seconds, i * RippleStagger);
        }

        // --- building it, once ------------------------------------------------------------------

        /// <summary>Where a point lies on the drawn land (a hill, a dip at the beach), lifted just clear of it.</summary>
        private static Vector3 OnGround(Vector3 point)
        {
            var y = point.y;
            var map = MeshMapApplier.Instance;
            if (map != null && map.TryGetGroundHeight(point, out var ground)) y = ground;
            else if (GridManager.Instance != null) y = GridManager.Instance.GroundHeight;
            return new Vector3(point.x, y + LiftOffGround, point.z);
        }

        private void EnsureBuilt()
        {
            if (_ripples != null) return;

            _discMesh ??= BuildDisc(12);
            _ringMesh ??= BuildRing(40, 0.78f);

            _ripples = new Ripple[RippleCount];
            for (var i = 0; i < RippleCount; i++) _ripples[i] = new Ripple(transform, _ringMesh);
            _targetRing = new Ring(transform, _ringMesh, "OrderTargetRing");

            _dotMaterial = CreateTransparentMaterial(new Color(OkColor.r, OkColor.g, OkColor.b, DotAlpha));
            _dots = new Transform[MaxDots];
            for (var i = 0; i < MaxDots; i++)
            {
                _dots[i] = CreateFlatPiece(transform, "OrderTrailDot", _discMesh, _dotMaterial);
                _dots[i].gameObject.SetActive(false);
            }

            if (_flagPrefab != null)
            {
                _flag = Instantiate(_flagPrefab, transform).transform;
                _flag.name = "OrderFlag";
                foreach (var collider in _flag.GetComponentsInChildren<Collider>()) Destroy(collider);
                _flag.gameObject.SetActive(false);
            }
        }

        private static Transform CreateFlatPiece(Transform parent, string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go.transform;
        }

        /// <summary>Same incantation as HarvestRadiusOverlay's: a URP Unlit made in code is opaque until told otherwise. Double-sided, so winding never hides it.</summary>
        private static Material CreateTransparentMaterial(Color color)
        {
            var material = new Material(RuntimeShaders.Unlit) { color = color };
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
            return material;
        }

        /// <summary>A flat disc of radius 1 lying on XZ, facing up.</summary>
        private static Mesh BuildDisc(int sides)
        {
            var vertices = new Vector3[sides + 1];
            var triangles = new int[sides * 3];
            for (var i = 0; i < sides; i++)
            {
                var angle = i * Mathf.PI * 2f / sides;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = (i + 1) % sides + 1;
                triangles[i * 3 + 2] = i + 1;
            }
            var mesh = new Mesh { name = "OrderDisc", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A flat ring of outer radius 1 and the given inner radius, lying on XZ, facing up.</summary>
        private static Mesh BuildRing(int sides, float inner)
        {
            var vertices = new Vector3[sides * 2];
            var triangles = new int[sides * 6];
            for (var i = 0; i < sides; i++)
            {
                var angle = i * Mathf.PI * 2f / sides;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices[i * 2] = direction * inner;
                vertices[i * 2 + 1] = direction;

                var next = (i + 1) % sides;
                var t = i * 6;
                triangles[t] = i * 2;
                triangles[t + 1] = next * 2 + 1;
                triangles[t + 2] = i * 2 + 1;
                triangles[t + 3] = i * 2;
                triangles[t + 4] = next * 2;
                triangles[t + 5] = next * 2 + 1;
            }
            var mesh = new Mesh { name = "OrderRing", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A ring lying on the ground with a material of its own, so its colour and fade are its own.</summary>
        private class Ring
        {
            private readonly Transform _transform;
            private readonly Material _material;

            public Ring(Transform parent, Mesh mesh, string name)
            {
                _material = CreateTransparentMaterial(OkColor);
                _transform = CreateFlatPiece(parent, name, mesh, _material);
                _transform.gameObject.SetActive(false);
            }

            public void Show(Vector3 point, float radius, Color color, float alpha)
            {
                _transform.position = point;
                SetLook(radius, color, alpha);
                _transform.gameObject.SetActive(true);
            }

            public void SetLook(float radius, Color color, float alpha)
            {
                _transform.localScale = new Vector3(radius, 1f, radius);
                color.a = alpha;
                _material.color = color;
            }

            public void Hide() => _transform.gameObject.SetActive(false);

            public void Destroy() => Object.Destroy(_material);
        }

        /// <summary>One ring of the tap's ripple: waits out its stagger, spreads, fades, and switches itself off.</summary>
        private class Ripple
        {
            private readonly Ring _ring;
            private Color _color;
            private float _seconds;
            private float _clock = -1000f;

            public Ripple(Transform parent, Mesh mesh)
            {
                _ring = new Ring(parent, mesh, "OrderRipple");
            }

            public void Play(Vector3 point, Color color, float seconds, float delay)
            {
                _color = color;
                _seconds = seconds;
                _clock = -delay;
                _ring.Show(point, RippleStartRadius, color, 0f);
            }

            public void Tick(float dt)
            {
                if (_clock < -900f) return;
                _clock += dt;
                if (_clock < 0f) return;
                var t = _clock / _seconds;
                if (t >= 1f)
                {
                    _ring.Hide();
                    _clock = -1000f;
                    return;
                }
                var spread = 1f - (1f - t) * (1f - t);
                _ring.SetLook(Mathf.Lerp(RippleStartRadius, RippleEndRadius, spread), _color, 0.9f * (1f - t));
            }

            public void Destroy() => _ring.Destroy();
        }
    }
}
