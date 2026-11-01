using CityBuilder.Combat;
using CityBuilder.Maps;
using UnityEngine;

namespace CityBuilder.Dungeon
{
    /// <summary>
    /// The stone spider queen in the middle of the lake (the user, 2026-11-01: «статую какого-нибудь
    /// монстра по центру озера и добавь ей эффект»; Tools/blender/dungeon1.py spider_statue): over
    /// her hangs a big crystal that turns slowly and bobs, in the reward crystals' glowing look
    /// with a wide halo -- nothing to take, only to look at. One per `statue` spot.
    /// </summary>
    public class DungeonStatue : MonoBehaviour
    {
        private const float GemScale = 2.6f;
        private const float HaloSize = 3.2f;
        private const float BobCells = 0.15f;
        private const float SpinDegreesPerSecond = 25f;

        private Transform _gem;
        private Vector3 _at;

        /// <summary>The crystal over every statue of the map.</summary>
        public static void RaiseAll(MeshMapDefinition map, Transform parent)
        {
            foreach (var spot in map.SpotsOf("statue"))
            {
                var at = new Vector3(spot.position.x, spot.height, spot.position.y);
                var go = new GameObject("StatueCrystal");
                go.transform.SetParent(parent, false);
                go.AddComponent<DungeonStatue>().Dress(at);
            }
        }

        private void Dress(Vector3 at)
        {
            _at = at;
            transform.position = at;
            var raids = OrcRaidManager.Instance;
            var model = raids != null ? raids.RewardCrystalModel : null;
            GameObject gem;
            if (model != null) gem = Instantiate(model, transform);
            else
            {
                gem = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(gem.GetComponent<Collider>());
                gem.transform.SetParent(transform, false);
                gem.transform.localScale = new Vector3(0.25f, 0.5f, 0.25f);
            }
            gem.name = "Gem";
            gem.transform.localPosition = Vector3.zero;
            gem.transform.localScale *= GemScale;
            var look = raids != null ? raids.CrystalMaterial : null;
            foreach (var r in gem.GetComponentsInChildren<Renderer>())
            {
                if (look != null) r.sharedMaterial = look;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            _gem = gem.transform;

            var halo = raids != null ? raids.CrystalHaloMaterial : null;
            if (halo == null) return;
            var h = new GameObject("Halo", typeof(MeshFilter), typeof(MeshRenderer));
            h.transform.SetParent(transform, false);
            h.transform.localPosition = Vector3.up * 0.5f;
            h.GetComponent<MeshFilter>().sharedMesh = Quad();
            var renderer = h.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = halo;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var block = new MaterialPropertyBlock();
            block.SetFloat("_Size", HaloSize);
            block.SetFloat("_Phase", 0.7f);
            renderer.SetPropertyBlock(block);
        }

        private static Mesh _quad;

        private static Mesh Quad()
        {
            if (_quad != null) return _quad;
            var h = HaloSize * 0.5f;
            _quad = new Mesh
            {
                name = "StatueHaloQuad",
                vertices = new[] { new Vector3(-h, -h, 0f), new Vector3(h, -h, 0f), new Vector3(h, h, 0f), new Vector3(-h, h, 0f) },
                uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) },
                triangles = new[] { 0, 1, 2, 0, 2, 3 },
            };
            _quad.bounds = new Bounds(Vector3.zero, Vector3.one * HaloSize);
            return _quad;
        }

        private void Update()
        {
            var t = Time.time;
            transform.position = _at + Vector3.up * (Mathf.Sin(t * 0.8f) * BobCells);
            if (_gem != null) _gem.localRotation = Quaternion.Euler(0f, t * SpinDegreesPerSecond, Mathf.Sin(t * 0.6f) * 6f);
        }
    }
}
