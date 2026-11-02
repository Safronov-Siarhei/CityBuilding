using CityBuilder.Combat;
using CityBuilder.Maps;
using UnityEngine;

namespace CityBuilder.Dungeon
{
    /// <summary>
    /// The stone spider queen in the middle of the lake (the user, 2026-11-01: «статую какого-нибудь
    /// монстра по центру озера и добавь ей эффект»; Tools/blender/dungeon1.py spider_statue). No
    /// crystal over her and nothing to take (the user, 2026-11-02: «не нужно делать статую
    /// наградой ... добавь какой-то эффект самой статуе»): a violet aura breathes round the stone
    /// itself, sparkling, slowly swelling and fading. One quad in the crystals' halo shader.
    /// </summary>
    public class DungeonStatue : MonoBehaviour
    {
        private const float AuraSize = 4.4f;
        private const float BreathSeconds = 3.2f;
        // the `statue` spot is over her head: the aura's middle this far down, on her body
        private const float BodyBelowSpot = 1.4f;
        private static readonly Color Aura = new Color(0.62f, 0.28f, 1f, 1f);
        private static readonly Color Sparks = new Color(0.75f, 1f, 0.55f, 1f);

        /// <summary>The aura round every statue of the map.</summary>
        public static void RaiseAll(MeshMapDefinition map, Transform parent)
        {
            var halo = OrcRaidManager.Instance != null ? OrcRaidManager.Instance.CrystalHaloMaterial : null;
            if (map == null || halo == null) return;
            foreach (var spot in map.SpotsOf("statue"))
            {
                var go = new GameObject("StatueAura", typeof(MeshFilter), typeof(MeshRenderer), typeof(DungeonStatue));
                go.transform.SetParent(parent, false);
                go.transform.position = new Vector3(spot.position.x, spot.height - BodyBelowSpot, spot.position.y);
                go.GetComponent<MeshFilter>().sharedMesh = Quad();
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = halo;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", Aura);
                block.SetColor("_SparkColor", Sparks);
                block.SetFloat("_Size", AuraSize);
                block.SetFloat("_Phase", 1.7f);
                block.SetFloat("_Pull", 1.2f);
                renderer.SetPropertyBlock(block);
            }
        }

        private static Mesh _quad;

        private static Mesh Quad()
        {
            if (_quad != null) return _quad;
            var h = AuraSize * 0.5f;
            _quad = new Mesh
            {
                name = "StatueAuraQuad",
                vertices = new[] { new Vector3(-h, -h, 0f), new Vector3(h, -h, 0f), new Vector3(h, h, 0f), new Vector3(-h, h, 0f) },
                uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) },
                triangles = new[] { 0, 1, 2, 0, 2, 3 },
            };
            _quad.bounds = new Bounds(Vector3.zero, Vector3.one * AuraSize * 1.3f);
            return _quad;
        }

        private MeshRenderer _renderer;
        private MaterialPropertyBlock _block;

        private void Update()
        {
            // a slow breath: the aura swells and sinks back (the halo is sized by _Size, not the scale)
            if (_renderer == null)
            {
                _renderer = GetComponent<MeshRenderer>();
                _block = new MaterialPropertyBlock();
            }
            var breath = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI * 2f / BreathSeconds);
            _renderer.GetPropertyBlock(_block);
            _block.SetFloat("_Size", AuraSize * (0.8f + 0.3f * breath));
            _renderer.SetPropertyBlock(_block);
        }
    }
}
