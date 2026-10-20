using CityBuilder.Buildings;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Maps
{
    /// <summary>
    /// The start zone drawn on the ground (see StartZone) while the Town Hall is still to be
    /// placed: a pale gold wash with a brighter rim. One mesh, two draws, gone for good once the
    /// Town Hall stands.
    /// </summary>
    public class StartZoneOverlay : MonoBehaviour
    {
        private static readonly Color FillColor = new Color(1f, 0.85f, 0.35f, 0.18f);
        private static readonly Color RimColor = new Color(1f, 0.82f, 0.25f, 0.85f);
        private const float Height = 0.06f;
        private const float RimWidth = 0.5f;
        private const float CheckSeconds = 0.5f;

        private float _timer;

        public static StartZoneOverlay Create(Transform parent, Vector2[] zone)
        {
            var go = new GameObject("StartZone");
            go.transform.SetParent(parent, false);
            var overlay = go.AddComponent<StartZoneOverlay>();
            overlay.Build(zone);
            return overlay;
        }

        private void Build(Vector2[] zone)
        {
            var centre = Vector2.zero;
            foreach (var p in zone) centre += p;
            centre /= zone.Length;

            // the fill: a fan from the centre (the zones are convex); the rim: a strip inside the edge
            var n = zone.Length;
            var vertices = new Vector3[1 + n + 2 * n];
            vertices[0] = new Vector3(centre.x, Height, centre.y);
            for (var i = 0; i < n; i++)
            {
                vertices[1 + i] = new Vector3(zone[i].x, Height, zone[i].y);
                var inward = (centre - zone[i]).normalized * RimWidth;
                vertices[1 + n + 2 * i] = new Vector3(zone[i].x, Height + 0.01f, zone[i].y);
                vertices[1 + n + 2 * i + 1] = new Vector3(zone[i].x + inward.x, Height + 0.01f, zone[i].y + inward.y);
            }
            // two sub-meshes, one per colour: URP's Unlit draws a material's colour, not the vertices'
            var fill = new int[n * 3];
            var rim = new int[n * 6];
            int f = 0, r = 0;
            for (var i = 0; i < n; i++)
            {
                var next = (i + 1) % n;
                fill[f++] = 0;
                fill[f++] = 1 + next;
                fill[f++] = 1 + i;
                var o0 = 1 + n + 2 * i;
                var i0 = o0 + 1;
                var o1 = 1 + n + 2 * next;
                var i1 = o1 + 1;
                rim[r++] = o0; rim[r++] = o1; rim[r++] = i0;
                rim[r++] = i0; rim[r++] = o1; rim[r++] = i1;
            }
            var mesh = new Mesh { name = "StartZone", subMeshCount = 2 };
            mesh.vertices = vertices;
            mesh.SetTriangles(fill, 0);
            mesh.SetTriangles(rim, 1);
            mesh.RecalculateBounds();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { Transparent(FillColor), Transparent(RimColor) };
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        /// <summary>A see-through URP Unlit material, both faces drawn (the corners may run either way round).</summary>
        private static Material Transparent(Color color)
        {
            var material = new Material(Core.RuntimeShaders.Unlit) { color = color };
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

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = CheckSeconds;
            if (BuildingInstance.HasAny(BuildingIds.TownHall)) Destroy(gameObject);
        }
    }
}
