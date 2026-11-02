using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Maps.Archipelago
{
    /// <summary>
    /// The surf along every island's coast (the user, 2026-11-02: «вода вокруг островов без
    /// какого-либо движения, она не выглядит живой, нужно сделать так, что её края покачивались»;
    /// options 1 + 2 chosen): a band from just up the sand to a couple of metres out, its uv.x the
    /// distance from the shore, drawn by Shaders/ShoreSurf -- the water's edge running up the beach
    /// and back with a white crest, a broken foam line breathing beyond. Built once from the land
    /// cells (a signed distance, traced by marching squares), cut in tiles so the off-screen ones
    /// are not drawn; nothing runs per frame on the CPU.
    /// </summary>
    public static class ShoreSurf
    {
        private const float From = -0.6f;     // the band's land side, metres over the sand
        private const float To = 2.4f;        // its sea side
        private const int Tile = 64;          // cells a side of one drawn piece
        private const float Lift = 0.04f;     // over the coastal water

        public static GameObject Build(ArchipelagoMap map, Vector3 origin, Material material, Transform parent)
        {
            if (map == null || material == null) return null;
            var n = map.Size;
            var field = SignedDistance(map);
            var root = new GameObject("ShoreSurf");
            root.transform.SetParent(parent, false);
            var y = ArchipelagoMeshes.WaterLevel + Lift;

            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            var polygon = new List<Vector3>(8);
            var clipped = new List<Vector3>(8);
            for (var tz = 0; tz < n - 1; tz += Tile)
            for (var tx = 0; tx < n - 1; tx += Tile)
            {
                verts.Clear();
                uvs.Clear();
                tris.Clear();
                for (var z = tz; z < Mathf.Min(tz + Tile, n - 1); z++)
                for (var x = tx; x < Mathf.Min(tx + Tile, n - 1); x++)
                {
                    float a = field[z * n + x], b = field[z * n + x + 1], c = field[(z + 1) * n + x + 1], d = field[(z + 1) * n + x];
                    var lo = Mathf.Min(Mathf.Min(a, b), Mathf.Min(c, d));
                    var hi = Mathf.Max(Mathf.Max(a, b), Mathf.Max(c, d));
                    if (hi < From || lo > To) continue;
                    // the square's corners at the cells' centres, each carrying its distance in .y
                    polygon.Clear();
                    polygon.Add(new Vector3(x + 0.5f, a, z + 0.5f));
                    polygon.Add(new Vector3(x + 1.5f, b, z + 0.5f));
                    polygon.Add(new Vector3(x + 1.5f, c, z + 1.5f));
                    polygon.Add(new Vector3(x + 0.5f, d, z + 1.5f));
                    Clip(polygon, clipped, From, true);
                    Clip(clipped, polygon, To, false);
                    if (polygon.Count < 3) continue;
                    var first = verts.Count;
                    foreach (var p in polygon)
                    {
                        verts.Add(origin + new Vector3(p.x, y, p.z));
                        uvs.Add(new Vector2(p.y, 0f));
                    }
                    for (var k = 1; k < polygon.Count - 1; k++)
                    {
                        tris.Add(first);
                        tris.Add(first + k + 1);
                        tris.Add(first + k);
                    }
                }
                if (tris.Count == 0) continue;
                var mesh = new Mesh { name = $"ShoreSurf_{tx}_{tz}" };
                if (verts.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(verts);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(tris, 0);
                mesh.RecalculateBounds();
                var go = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(root.transform, false);
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            return root;
        }

        /// <summary>Keeps the part of a polygon on one side of `level` (its .y the distance): above it when `keepAbove`.</summary>
        private static void Clip(List<Vector3> from, List<Vector3> into, float level, bool keepAbove)
        {
            into.Clear();
            for (var k = 0; k < from.Count; k++)
            {
                var p = from[k];
                var q = from[(k + 1) % from.Count];
                var pIn = keepAbove ? p.y >= level : p.y <= level;
                var qIn = keepAbove ? q.y >= level : q.y <= level;
                if (pIn) into.Add(p);
                if (pIn != qIn) into.Add(Vector3.Lerp(p, q, (level - p.y) / (q.y - p.y)));
            }
        }

        /// <summary>
        /// Metres from the coast at every cell's centre: positive out at sea, negative inland, 0 on
        /// the line between a land cell and a water cell. Two-pass chamfer distances, capped -- only
        /// the band near the shore matters.
        /// </summary>
        private static float[] SignedDistance(ArchipelagoMap map)
        {
            var n = map.Size;
            var toLand = Chamfer(map.Land, n, true);
            var toWater = Chamfer(map.Land, n, false);
            var field = new float[n * n];
            for (var i = 0; i < n * n; i++)
                field[i] = map.Land[i] ? -(toWater[i] - 0.5f) : toLand[i] - 0.5f;
            return field;
        }

        private const float Cap = 6f;

        /// <summary>Distance to the nearest cell whose Land equals `target` (0 on those cells), capped.</summary>
        private static float[] Chamfer(bool[] land, int n, bool target)
        {
            const float diagonal = 1.41421356f;
            var d = new float[n * n];
            for (var i = 0; i < n * n; i++) d[i] = land[i] == target ? 0f : Cap;
            for (var z = 0; z < n; z++)
            for (var x = 0; x < n; x++)
            {
                var i = z * n + x;
                if (d[i] == 0f) continue;
                var v = d[i];
                if (x > 0) v = Mathf.Min(v, d[i - 1] + 1f);
                if (z > 0)
                {
                    v = Mathf.Min(v, d[i - n] + 1f);
                    if (x > 0) v = Mathf.Min(v, d[i - n - 1] + diagonal);
                    if (x < n - 1) v = Mathf.Min(v, d[i - n + 1] + diagonal);
                }
                d[i] = v;
            }
            for (var z = n - 1; z >= 0; z--)
            for (var x = n - 1; x >= 0; x--)
            {
                var i = z * n + x;
                if (d[i] == 0f) continue;
                var v = d[i];
                if (x < n - 1) v = Mathf.Min(v, d[i + 1] + 1f);
                if (z < n - 1)
                {
                    v = Mathf.Min(v, d[i + n] + 1f);
                    if (x < n - 1) v = Mathf.Min(v, d[i + n + 1] + diagonal);
                    if (x > 0) v = Mathf.Min(v, d[i + n - 1] + diagonal);
                }
                d[i] = v;
            }
            return d;
        }
    }
}
