using CityBuilder.Combat;
using CityBuilder.Maps;
using UnityEngine;

namespace CityBuilder.Dungeon
{
    /// <summary>
    /// The daylight in the way out's opening (the user, 2026-11-02: «вместо белой стены было жёлтое
    /// свечение, как будто свет из прохода ... свет с поверхности»): a warm yellow halo, breathing
    /// slowly, at the map's `light_day` spot (Tools/blender/dungeon1.py mine_exit), drawn in front of
    /// the rock by the halo shader's pull. One quad, no light source.
    /// </summary>
    public class DungeonDaylight : MonoBehaviour
    {
        private const float Size = 5f;
        private const float Pull = 2.2f;
        private static readonly Color Warm = new Color(1f, 0.82f, 0.35f, 1f);
        private static readonly Color Motes = new Color(1f, 0.96f, 0.75f, 1f);

        private static readonly Color Opening = new Color(1f, 0.86f, 0.42f, 1f);

        public static void LightAll(MeshMapDefinition map, Transform parent)
        {
            // the opening itself, flat and bright: no light or shade on it, so it reads as light
            if (map != null && map.ExitGlowPrefab != null)
            {
                var glow = Instantiate(map.ExitGlowPrefab, Vector3.zero, map.ExitGlowPrefab.transform.rotation, parent);
                glow.name = "ExitGlow";
                var flat = new Material(Core.RuntimeShaders.Unlit) { name = "ExitGlow", color = Opening };
                foreach (var r in glow.GetComponentsInChildren<Renderer>())
                {
                    r.sharedMaterial = flat;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = false;
                }
            }
            var halo = OrcRaidManager.Instance != null ? OrcRaidManager.Instance.CrystalHaloMaterial : null;
            if (map == null || halo == null) return;
            foreach (var spot in map.SpotsOf("light_day"))
            {
                var go = new GameObject("Daylight", typeof(MeshFilter), typeof(MeshRenderer), typeof(DungeonDaylight));
                go.transform.SetParent(parent, false);
                go.transform.position = new Vector3(spot.position.x, spot.height, spot.position.y);
                go.GetComponent<MeshFilter>().sharedMesh = Quad();
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = halo;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", Warm);
                block.SetColor("_SparkColor", Motes);
                block.SetFloat("_Size", Size);
                block.SetFloat("_Phase", 0.3f);
                block.SetFloat("_Pull", Pull);
                renderer.SetPropertyBlock(block);
            }
        }

        private static Mesh _quad;

        private static Mesh Quad()
        {
            if (_quad != null) return _quad;
            var h = Size * 0.5f;
            _quad = new Mesh
            {
                name = "DaylightQuad",
                vertices = new[] { new Vector3(-h, -h, 0f), new Vector3(h, -h, 0f), new Vector3(h, h, 0f), new Vector3(-h, h, 0f) },
                uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) },
                triangles = new[] { 0, 1, 2, 0, 2, 3 },
            };
            _quad.bounds = new Bounds(Vector3.zero, Vector3.one * Size);
            return _quad;
        }
    }
}
