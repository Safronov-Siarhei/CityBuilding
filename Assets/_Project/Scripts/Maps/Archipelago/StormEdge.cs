using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Maps.Archipelago
{
    /// <summary>
    /// The archipelago's edge (the user, 2026-10-31: «край карты ... это всё ещё просто полоса"):
    /// a dark storm sea beyond the last calm water, its line ragged rather than ruled -- a band of
    /// surf along it, sea stacks breaking it, whitecaps over the dark water. It starts a cell short
    /// of where the ships stop (ArchipelagoGenerator.Storm), so no ship ever sails into it.
    /// Two meshes in the ground's own palette: the surf and the storm water in one (a darkened copy
    /// of the material), the rocks and whitecaps in the other -- two draws for the whole edge.
    /// </summary>
    public static class StormEdge
    {
        private const float Step = 1.5f;          // the lattice the ragged line is traced on, metres
        private const float Wobble = 5f;          // how far the line wanders in from the ships' limit
        private const float Surf = 1.4f;          // the white band's width
        private const int Margin = 60;            // as far out as the sea is drawn (ArchipelagoMeshes.Sea)
        private static readonly Color StormTint = new Color(0.42f, 0.47f, 0.58f, 1f);

        public static GameObject Build(ArchipelagoMap map, Vector3 origin, Material terrain, Transform parent)
        {
            if (map == null || terrain == null) return null;
            var n = map.Size;
            var seed = map.Seed * 13 + 5;
            var root = new GameObject("StormEdge");
            root.transform.SetParent(parent, false);

            // > 0 in the storm: past the ships' limit, pushed in by a slow wobble and a quick one
            float Field(float x, float z)
            {
                var edge = Mathf.Min(Mathf.Min(x, z), Mathf.Min(n - x, n - z));
                var wobble = Wobble * Mathf.PerlinNoise(x * 0.035f + seed % 97, z * 0.035f + seed % 89)
                    + 1.2f * Mathf.PerlinNoise(x * 0.21f + 11f, z * 0.21f + 7f);
                return ArchipelagoGenerator.Storm - 1f - Mathf.Min(wobble, Wobble) - edge;
            }

            var water = new List<Vector3>();
            var waterUv = new List<Vector2>();
            var waterTris = new List<int>();
            var surfUv = ArchipelagoMeshes.PaletteUvOf(ArchipelagoMeshes.FoamIndex);
            var stormUv = ArchipelagoMeshes.PaletteUvOf(ArchipelagoMeshes.WaterDeepIndex);
            var y = ArchipelagoMeshes.WaterLevel;

            // the transition belt: traced on the lattice, four sides of it
            var inner = ArchipelagoGenerator.Storm + 1f;               // nothing of the storm further in
            var outer = ArchipelagoGenerator.Storm - Wobble - 4f;      // all of it storm further out
            void Belt(float x0, float z0, float x1, float z1)
            {
                var nx = Mathf.CeilToInt((x1 - x0) / Step);
                var nz = Mathf.CeilToInt((z1 - z0) / Step);
                for (var j = 0; j < nz; j++)
                for (var i = 0; i < nx; i++)
                {
                    float ax = x0 + i * Step, az = z0 + j * Step;
                    Trace(ax, az, Step, Field, -Surf, y + 0.015f, surfUv, water, waterUv, waterTris);
                    Trace(ax, az, Step, Field, 0f, y + 0.03f, stormUv, water, waterUv, waterTris);
                }
            }
            Belt(outer, outer, n - outer, inner);              // south
            Belt(outer, n - inner, n - outer, n - outer);      // north
            Belt(outer, inner, inner, n - inner);              // west
            Belt(n - inner, inner, n - outer, n - inner);      // east

            // everything beyond the belt out to the drawn sea: four plain storm rectangles
            void Rect(float x0, float z0, float x1, float z1)
            {
                var s = water.Count;
                water.Add(origin + new Vector3(x0, y + 0.03f, z0));
                water.Add(origin + new Vector3(x1, y + 0.03f, z0));
                water.Add(origin + new Vector3(x1, y + 0.03f, z1));
                water.Add(origin + new Vector3(x0, y + 0.03f, z1));
                for (var k = 0; k < 4; k++) waterUv.Add(stormUv);
                waterTris.AddRange(new[] { s, s + 2, s + 1, s, s + 3, s + 2 });
            }
            Rect(-Margin, -Margin, n + Margin, outer);
            Rect(-Margin, n - outer, n + Margin, n + Margin);
            Rect(-Margin, outer, outer, n - outer);
            Rect(n - outer, outer, n + Margin, n - outer);

            var stormMaterial = new Material(SeasonStyle.TerrainMaterial(terrain)) { name = "StormSea", color = StormTint };
            Emit("StormWater", root.transform, water, waterUv, waterTris, stormMaterial);

            // sea stacks along the line, whitecaps over the dark water
            var rng = new SeedRandom(seed);
            var bits = new List<Vector3>();
            var bitsUv = new List<Vector2>();
            var bitsTris = new List<int>();
            var foam = ArchipelagoMeshes.PaletteUvOf(ArchipelagoMeshes.FoamIndex);
            var perimeter = 4 * (n + 2 * Margin);
            for (var k = 0; k < perimeter / 9; k++)
            {
                var p = OnBand(n, rng, ArchipelagoGenerator.Storm - Wobble - 7f, ArchipelagoGenerator.Storm - 3f);
                if (Field(p.x, p.y) < 0.5f) continue;
                var clusters = rng.RangeInt(1, 4);
                for (var c = 0; c < clusters; c++)
                {
                    var at = p + new Vector2(rng.Range(-1.6f, 1.6f), rng.Range(-1.6f, 1.6f));
                    Rock(origin + new Vector3(at.x, y - 0.3f, at.y), rng.Range(0.5f, 1.3f), rng.Range(1.2f, 3.6f), rng, bits, bitsUv, bitsTris);
                }
            }
            for (var k = 0; k < perimeter * 2; k++)
            {
                var p = OnBand(n, rng, -Margin, ArchipelagoGenerator.Storm - 2f);
                if (Field(p.x, p.y) < 1f) continue;
                Whitecap(origin + new Vector3(p.x, y + 0.05f, p.y), rng.Range(0.6f, 1.6f), rng.Range(0f, 180f), foam, bits, bitsUv, bitsTris);
            }
            Emit("StormRocks", root.transform, bits, bitsUv, bitsTris, SeasonStyle.TerrainMaterial(terrain));
            return root;

            // marching squares: the piece of one lattice square where the field is >= level
            void Trace(float ax, float az, float step, System.Func<float, float, float> f, float level, float height, Vector2 uv,
                List<Vector3> into, List<Vector2> intoUv, List<int> tris)
            {
                var px = new[] { ax, ax + step, ax + step, ax };
                var pz = new[] { az, az, az + step, az + step };
                var v = new float[4];
                var inside = 0;
                for (var k = 0; k < 4; k++)
                {
                    v[k] = f(px[k], pz[k]);
                    if (v[k] >= level) inside++;
                }
                if (inside == 0) return;
                var s = into.Count;
                for (var k = 0; k < 4; k++)
                {
                    var b = (k + 1) % 4;
                    if (v[k] >= level) into.Add(origin + new Vector3(px[k], height, pz[k]));
                    if ((v[k] >= level) != (v[b] >= level))
                    {
                        var t = (level - v[k]) / (v[b] - v[k]);
                        into.Add(origin + new Vector3(Mathf.Lerp(px[k], px[b], t), height, Mathf.Lerp(pz[k], pz[b], t)));
                    }
                }
                var count = into.Count - s;
                for (var k = 0; k < count; k++) intoUv.Add(uv);
                for (var k = 1; k < count - 1; k++) tris.AddRange(new[] { s, s + k + 1, s + k });
            }
        }

        /// <summary>A point in the band `from`..`to` metres in from the map's edge (negative = out past it), on any of the four sides.</summary>
        private static Vector2 OnBand(int n, SeedRandom rng, float from, float to)
        {
            var along = rng.Range(-Margin, n + Margin);
            var depth = rng.Range(from, to);
            switch (rng.RangeInt(0, 4))
            {
                case 0: return new Vector2(along, depth);
                case 1: return new Vector2(along, n - depth);
                case 2: return new Vector2(depth, along);
                default: return new Vector2(n - depth, along);
            }
        }

        /// <summary>A jagged sea stack: a five-sided spire, leaning a little, rock below and darker rock at the top.</summary>
        private static void Rock(Vector3 at, float radius, float height, SeedRandom rng, List<Vector3> v, List<Vector2> uv, List<int> tris)
        {
            const int sides = 5;
            var turn = rng.Range(0f, Mathf.PI * 2f);
            var lean = new Vector3(rng.Range(-0.3f, 0.3f), 0f, rng.Range(-0.3f, 0.3f)) * radius;
            var top = at + new Vector3(0f, height, 0f) + lean;
            var mid = new Vector3[sides];
            var low = new Vector3[sides];
            for (var k = 0; k < sides; k++)
            {
                var a = turn + k * Mathf.PI * 2f / sides;
                var r = radius * rng.Range(0.75f, 1.15f);
                low[k] = at + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                mid[k] = at + new Vector3(Mathf.Cos(a) * r * 0.6f, height * rng.Range(0.45f, 0.65f), Mathf.Sin(a) * r * 0.6f) + lean * 0.5f;
            }
            var rock = ArchipelagoMeshes.PaletteUvOf(ArchipelagoMeshes.RockMidIndex);
            var dark = ArchipelagoMeshes.PaletteUvOf(ArchipelagoMeshes.RockDarkIndex);
            for (var k = 0; k < sides; k++)
            {
                var b = (k + 1) % sides;
                Face(v, uv, tris, rock, at, low[k], mid[k], mid[b], low[b]);
                Face(v, uv, tris, dark, at, mid[k], top, top, mid[b]);
            }
        }

        /// <summary>A flat face of its own corners (flat-shaded), wound to face outward from the spire's axis.</summary>
        private static void Face(List<Vector3> v, List<Vector2> uv, List<int> tris, Vector2 colour, Vector3 axis, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var s = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            for (var k = 0; k < 4; k++) uv.Add(colour);
            var centre = (a + b + c + d) * 0.25f;
            var outward = centre - new Vector3(axis.x, centre.y, axis.z);
            var normal = Vector3.Cross(b - a, d - a);
            if (Vector3.Dot(normal, outward) >= 0f) tris.AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3 });
            else tris.AddRange(new[] { s, s + 2, s + 1, s, s + 3, s + 2 });
        }

        /// <summary>A whitecap: a thin diamond of foam lying on the water.</summary>
        private static void Whitecap(Vector3 at, float size, float degrees, Vector2 foam, List<Vector3> v, List<Vector2> uv, List<int> tris)
        {
            var rotation = Quaternion.Euler(0f, degrees, 0f);
            var s = v.Count;
            v.Add(at + rotation * new Vector3(-size, 0f, 0f));
            v.Add(at + rotation * new Vector3(0f, 0f, size * 0.22f));
            v.Add(at + rotation * new Vector3(size, 0f, 0f));
            v.Add(at + rotation * new Vector3(0f, 0f, -size * 0.22f));
            for (var k = 0; k < 4; k++) uv.Add(foam);
            tris.AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3 });
        }

        private static void Emit(string name, Transform parent, List<Vector3> vertices, List<Vector2> uvs, List<int> triangles, Material material)
        {
            if (triangles.Count == 0) return;
            var mesh = new Mesh { name = name, indexFormat = vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
}
