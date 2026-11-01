using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Combat
{
    /// <summary>
    /// What makes an orc portal alive (the user, 2026-11-01, picked A + B + D of the proposal):
    /// - A: a whirlpool turning in the arch (Shaders/PortalVortex), fast and bright on the portal
    ///   the next wave comes out of, slow and dim on a banked one;
    /// - B: a breathing glow with embers rising round the arch (the crystals' CrystalHalo in the
    ///   portal's colours) and a circle of runes on the ground (Shaders/PortalRunes);
    /// - D: a wave coming out throws the whirlpool white for a moment and rolls a shock ring out
    ///   over the grass (Flare).
    /// The pieces are children of the looks (OrcPortal.SetLooks), so switching or rebuilding a look
    /// takes them along. Placed in the model's own units: Blender (x, y, z) lands on the look's
    /// (x, z, y) and the arch's front is -z (Tools/blender/export_fbx.py); the opening's size comes
    /// from orcportal1.py / orcmainbase2.py. Three transparent quads a portal, no particles, no
    /// per-frame work except during a flare.
    /// </summary>
    public class OrcPortalFx : MonoBehaviour
    {
        /// <summary>An arch's opening in its model's units: half width, bottom and top of the opening, the ground the runes lie on, the runes' radius.</summary>
        private readonly struct Arch
        {
            public readonly float HalfWidth, Bottom, Top, Ground, Runes, Front;

            public Arch(float halfWidth, float bottom, float top, float ground, float runes, float front)
            {
                HalfWidth = halfWidth; Bottom = bottom; Top = top; Ground = ground; Runes = runes; Front = front;
            }
        }

        // orcportal1.py: OPEN_W 0.42, the opening from 0.09 to 1.42, the platform's stakes at 0.86
        private static readonly Arch Camp = new Arch(0.42f, 0.09f, 1.42f, 0.1f, 1.7f, -0.04f);
        // orcmainbase2.py: OPEN_W 0.78, from DAIS + 0.02 to 2.62, the dais 1.6 across
        private static readonly Arch Main = new Arch(0.78f, 0.27f, 2.62f, 0.27f, 3.2f, -0.07f);

        private const float FlareSeconds = 1.4f;
        private const float FlashSeconds = 0.6f;
        private const float ActiveIntensity = 1f, ActiveSpeed = 1f;
        private const float PassiveIntensity = 0.32f, PassiveSpeed = 0.35f;

        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int SpeedId = Shader.PropertyToID("_Speed");
        private static readonly int FlashId = Shader.PropertyToID("_Flash");
        private static readonly int WaveId = Shader.PropertyToID("_Wave");
        private static readonly int SizeId = Shader.PropertyToID("_Size");
        private static readonly int PhaseId = Shader.PropertyToID("_Phase");

        private static Mesh _upright, _flat;

        private Renderer _vortex, _runes;
        private float _intensity;
        private float _flareAge = -1f;
        private MaterialPropertyBlock _block;

        /// <summary>Dresses a portal's looks: the lit one in full, the banked one dim and without the glow. A look may be null.</summary>
        public static void Dress(GameObject activeLook, GameObject passiveLook, bool main)
        {
            var raids = OrcRaidManager.Instance;
            if (raids == null || raids.PortalVortexMaterial == null) return;
            var arch = main ? Main : Camp;
            if (activeLook != null) activeLook.AddComponent<OrcPortalFx>().Build(raids, arch, true);
            if (passiveLook != null) passiveLook.AddComponent<OrcPortalFx>().Build(raids, arch, false);
        }

        /// <summary>A wave comes out: the whirlpool flashes white and a shock ring rolls out from the runes.</summary>
        public static void FlareOn(OrcPortal portal)
        {
            if (portal == null) return;
            foreach (var fx in portal.GetComponentsInChildren<OrcPortalFx>())
                fx._flareAge = 0f;
        }

        private void Build(OrcRaidManager raids, Arch arch, bool lit)
        {
            _block = new MaterialPropertyBlock();
            _intensity = lit ? ActiveIntensity : PassiveIntensity;
            var height = arch.Top - arch.Bottom;
            var middle = arch.Bottom + height * 0.5f;

            // A: the whirlpool, filling the opening just in front of the model's own dark rim
            _vortex = Piece("PortalVortex", Upright(), raids.PortalVortexMaterial,
                new Vector3(0f, middle, arch.Front), new Vector3(arch.HalfWidth * 2f, height, 1f));
            _block.Clear();
            _block.SetFloat(IntensityId, _intensity);
            _block.SetFloat(SpeedId, lit ? ActiveSpeed : PassiveSpeed);
            _block.SetFloat(FlashId, 0f);
            _vortex.SetPropertyBlock(_block);

            // B: the circle of runes on the platform
            _runes = Piece("PortalRunes", Flat(), raids.PortalRunesMaterial,
                new Vector3(0f, arch.Ground + 0.02f, 0f), Vector3.one * arch.Runes * 2f);
            _block.Clear();
            _block.SetFloat(IntensityId, lit ? 1f : 0.4f);
            _block.SetFloat(WaveId, 0f);
            _runes.SetPropertyBlock(_block);

            // B: the breathing glow with embers rising, only round a lit portal
            if (lit && raids.PortalHaloMaterial != null)
            {
                var halo = Piece("PortalHalo", Upright(), raids.PortalHaloMaterial,
                    new Vector3(0f, middle, arch.Front - 0.05f), Vector3.one);
                // CrystalHalo sizes itself in world units, turned to the camera
                var size = height * 1.9f * transform.lossyScale.y;
                halo.GetComponent<MeshFilter>().sharedMesh = HaloQuad(size);
                _block.Clear();
                _block.SetFloat(SizeId, size);
                _block.SetFloat(PhaseId, transform.position.x * 0.37f);
                halo.SetPropertyBlock(_block);
            }
        }

        private Renderer Piece(string name, Mesh mesh, Material material, Vector3 at, Vector3 scale)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = at;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = scale;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            return renderer;
        }

        private void Update()
        {
            if (_flareAge < 0f) return;
            _flareAge += Time.deltaTime;
            var done = _flareAge >= FlareSeconds;
            if (_vortex != null)
            {
                _vortex.GetPropertyBlock(_block);
                _block.SetFloat(FlashId, done ? 0f : Mathf.Clamp01(1f - _flareAge / FlashSeconds));
                _vortex.SetPropertyBlock(_block);
            }
            if (_runes != null)
            {
                _runes.GetPropertyBlock(_block);
                _block.SetFloat(WaveId, done ? 0f : _flareAge / FlareSeconds);
                _runes.SetPropertyBlock(_block);
            }
            if (done) _flareAge = -1f;
        }

        /// <summary>The flare in progress (0..1), or -1 -- for the tests.</summary>
        public float Flaring => _flareAge < 0f ? -1f : Mathf.Clamp01(_flareAge / FlareSeconds);

        /// <summary>A unit quad standing up, facing -z (the arch's front), UVs 0..1.</summary>
        private static Mesh Upright()
        {
            if (_upright != null) return _upright;
            _upright = new Mesh
            {
                name = "PortalUpright",
                vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f) },
                uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) },
                triangles = new[] { 0, 2, 1, 0, 3, 2 },
            };
            _upright.RecalculateBounds();
            return _upright;
        }

        /// <summary>A unit quad lying flat, UVs 0..1.</summary>
        private static Mesh Flat()
        {
            if (_flat != null) return _flat;
            _flat = new Mesh
            {
                name = "PortalFlat",
                vertices = new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(-0.5f, 0f, 0.5f) },
                uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) },
                triangles = new[] { 0, 2, 1, 0, 3, 2 },
            };
            _flat.RecalculateBounds();
            return _flat;
        }

        /// <summary>The glow's quad: the shader turns it to the camera and sizes it, so its bounds must hold the whole glow.</summary>
        private static Mesh HaloQuad(float size)
        {
            var h = 0.5f;
            var mesh = new Mesh
            {
                name = "PortalHaloQuad",
                vertices = new[] { new Vector3(-h, -h, 0f), new Vector3(h, -h, 0f), new Vector3(h, h, 0f), new Vector3(-h, h, 0f) },
                uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) },
                triangles = new[] { 0, 1, 2, 0, 2, 3 },
            };
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * size * 2f);
            return mesh;
        }
    }
}
