using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Maps.Archipelago
{
    /// <summary>
    /// The archipelago's edge (the user, 2026-10-31: «край карты ... это всё ещё просто полоса»,
    /// then «белые ромбы, которые мало напоминают шторм»): a storm sea beyond the last calm water.
    /// - A ragged line of white surf where the calm water ends, broken and uneven.
    /// - The storm water darkening in three steps outwards.
    /// - Rows of long wave crests, roughly along the edge, rolling in and out (StormSwell).
    /// - Massive sea stacks along the line, foam round their feet.
    /// - Dark clouds hanging over the far storm.
    /// It starts a cell short of where the ships stop (ArchipelagoGenerator.Storm), so no ship
    /// ever sails into it. Ten draws for the whole edge, none with shadows.
    /// </summary>
    public static class StormEdge
    {
        private const float Step = 1.5f;          // the lattice the ragged line is traced on, metres
        private const float Wobble = 5f;          // how far the line wanders in from the ships' limit
        private const int Margin = 60;            // as far out as the sea is drawn (ArchipelagoMeshes.Sea)
        private static readonly float[] Levels = { 0f, 6f, 13f };
        private static readonly Color[] Tints =
        {
            new Color(0.17f, 0.31f, 0.47f), new Color(0.12f, 0.23f, 0.37f), new Color(0.08f, 0.16f, 0.27f),
        };
        private static readonly Color Foam = new Color(0.93f, 0.97f, 1f);
        private static readonly Color Cloud = new Color(0.36f, 0.39f, 0.46f);

        public static GameObject Build(ArchipelagoMap map, Vector3 origin, Material terrain, Transform parent)
        {
            if (map == null) return null;
            var n = map.Size;
            var seed = map.Seed * 13 + 5;
            var root = new GameObject("StormEdge");
            root.transform.SetParent(parent, false);
            var y = ArchipelagoMeshes.WaterLevel;

            // > 0 in the storm, growing outwards: past the ships' limit, pushed in by a slow wobble and a quick one
            float Field(float x, float z)
            {
                var edge = Mathf.Min(Mathf.Min(x, z), Mathf.Min(n - x, n - z));
                var wobble = Wobble * Mathf.PerlinNoise(x * 0.035f + seed % 97, z * 0.035f + seed % 89)
                    + 1.2f * Mathf.PerlinNoise(x * 0.21f + 11f, z * 0.21f + 7f);
                return ArchipelagoGenerator.Storm - 1f - Mathf.Min(wobble, Wobble) - edge;
            }
            // the surf's own field: its width wanders from nothing to two metres, so the line breaks
            float SurfField(float x, float z) => Field(x, z) + 2.2f * Mathf.PerlinNoise(x * 0.3f + 3f, z * 0.3f + 5f) - 0.4f;

            var white = new Mesher();
            var bands = new[] { new Mesher(), new Mesher(), new Mesher() };

            // the belt the lines run through, traced on the lattice, four sides of it
            var inner = ArchipelagoGenerator.Storm + 2f;
            var outer = ArchipelagoGenerator.Storm - Wobble - Levels[Levels.Length - 1] - 2f;
            void Belt(float x0, float z0, float x1, float z1)
            {
                var nx = Mathf.CeilToInt((x1 - x0) / Step);
                var nz = Mathf.CeilToInt((z1 - z0) / Step);
                for (var j = 0; j < nz; j++)
                for (var i = 0; i < nx; i++)
                {
                    float ax = x0 + i * Step, az = z0 + j * Step;
                    Trace(white, origin, ax, az, SurfField, 0f, y + 0.02f);
                    for (var k = 0; k < Levels.Length; k++) Trace(bands[k], origin, ax, az, Field, Levels[k] + (k == 0 ? 0.3f : 0f), y + 0.03f + 0.01f * k);
                }
            }
            Belt(outer, outer, n - outer, inner);              // south
            Belt(outer, n - inner, n - outer, n - outer);      // north
            Belt(outer, inner, inner, n - inner);              // west
            Belt(n - inner, inner, n - outer, n - inner);      // east
            // beyond the belt out to the drawn sea: the darkest water
            var far = bands[bands.Length - 1];
            var h = y + 0.03f + 0.01f * (Levels.Length - 1);
            far.Rect(origin, -Margin, -Margin, n + Margin, outer, h);
            far.Rect(origin, -Margin, n - outer, n + Margin, n + Margin, h);
            far.Rect(origin, -Margin, outer, outer, n - outer, h);
            far.Rect(origin, n - outer, outer, n + Margin, n - outer, h);

            // sea stacks along the line, foam round their feet
            var rng = new SeedRandom(seed);
            var rocks = new Mesher();
            var perimeter = 4 * (n + 2 * Margin);
            for (var k = 0; k < perimeter / 14; k++)
            {
                var p = OnBand(n, rng, ArchipelagoGenerator.Storm - Wobble - 9f, ArchipelagoGenerator.Storm - 2f, out _);
                if (Field(p.x, p.y) < 1f) continue;
                var clusters = rng.RangeInt(1, 3);
                for (var c = 0; c < clusters; c++)
                {
                    var at = p + new Vector2(rng.Range(-2f, 2f), rng.Range(-2f, 2f));
                    var radius = rng.Range(0.9f, 2.1f);
                    var foot = origin + new Vector3(at.x, y - 0.3f, at.y);
                    Rock(rocks, foot, radius, rng.Range(1f, 2.8f), rng);
                    white.Ring(new Vector3(foot.x, y + 0.06f, foot.z), radius * 0.9f, radius * 1.45f, 9);
                }
            }

            // the wave crests: rows along each side, rolling in and out (StormSwell), one object a side
            var crestMaterial = new Material(RuntimeShaders.Unlit) { name = "StormFoam", color = Foam };
            var swell = root.AddComponent<StormSwell>();
            for (var side = 0; side < 4; side++)
            {
                var crests = new Mesher();
                for (var depth = ArchipelagoGenerator.Storm - 4f; depth > -24f; depth -= 4.5f)
                for (var along = -Margin + rng.Range(0f, 6f); along < n + Margin; along += rng.Range(7f, 13f))
                {
                    var d = depth + rng.Range(-1.2f, 1.2f);
                    var p = OnSide(n, side, along, d);
                    if (Field(p.x, p.y) < 2f) continue;
                    var length = rng.Range(2.5f, 6.5f);
                    crests.Crest(origin, n, side, along, d, length, rng.Range(0.18f, 0.3f), rng.Range(0.3f, 0.9f), y + 0.07f);
                }
                var go = crests.Emit($"StormCrests{side}", root.transform, crestMaterial);
                if (go != null) swell.Add(go.transform, Inward(side), side * 1.3f);
            }

            // dark clouds over the far storm
            var clouds = new Mesher();
            for (var k = 0; k < perimeter / 16; k++)
            {
                var p = OnBand(n, rng, -Margin + 10f, ArchipelagoGenerator.Storm - 16f, out _);
                var at = origin + new Vector3(p.x, rng.Range(5f, 8f), p.y);
                var puffs = rng.RangeInt(2, 5);
                for (var c = 0; c < puffs; c++)
                {
                    var offset = new Vector3(rng.Range(-3f, 3f), rng.Range(-0.4f, 0.6f), rng.Range(-3f, 3f));
                    var size = rng.Range(2f, 4f);
                    clouds.Puff(at + offset, new Vector3(size, size * 0.45f, size * rng.Range(0.7f, 1.1f)));
                }
            }

            white.Emit("StormSurf", root.transform, new Material(RuntimeShaders.Unlit) { name = "StormSurf", color = Foam });
            for (var k = 0; k < bands.Length; k++)
                bands[k].Emit($"StormWater{k}", root.transform, new Material(RuntimeShaders.Lit) { name = $"StormSea{k}", color = Tints[k] });
            var rockMaterial = terrain != null
                ? SeasonStyle.TerrainMaterial(terrain)
                : new Material(RuntimeShaders.Lit) { color = new Color(0.45f, 0.43f, 0.4f) };
            rocks.Emit("StormRocks", root.transform, rockMaterial);
            clouds.Emit("StormClouds", root.transform, new Material(RuntimeShaders.Lit) { name = "StormClouds", color = Cloud });
            return root;
        }

        /// <summary>The way into the map from a side (0 south, 1 north, 2 west, 3 east).</summary>
        private static Vector3 Inward(int side) =>
            side == 0 ? Vector3.forward : side == 1 ? Vector3.back : side == 2 ? Vector3.right : Vector3.left;

        /// <summary>A point `along` a side, `depth` metres in from the map's edge (negative = out past it).</summary>
        private static Vector2 OnSide(int n, int side, float along, float depth)
        {
            switch (side)
            {
                case 0: return new Vector2(along, depth);
                case 1: return new Vector2(along, n - depth);
                case 2: return new Vector2(depth, along);
                default: return new Vector2(n - depth, along);
            }
        }

        /// <summary>A point in the band `from`..`to` metres in from the map's edge, on any of the four sides.</summary>
        private static Vector2 OnBand(int n, SeedRandom rng, float from, float to, out int side)
        {
            side = rng.RangeInt(0, 4);
            return OnSide(n, side, rng.Range(-Margin, n + Margin), rng.Range(from, to));
        }

        /// <summary>The piece of one lattice square where the field is >= level (marching squares).</summary>
        private static void Trace(Mesher into, Vector3 origin, float ax, float az, System.Func<float, float, float> f, float level, float height)
        {
            var px = new[] { ax, ax + Step, ax + Step, ax };
            var pz = new[] { az, az, az + Step, az + Step };
            var v = new float[4];
            var inside = 0;
            for (var k = 0; k < 4; k++)
            {
                v[k] = f(px[k], pz[k]);
                if (v[k] >= level) inside++;
            }
            if (inside == 0) return;
            var polygon = new List<Vector3>(6);
            for (var k = 0; k < 4; k++)
            {
                var b = (k + 1) % 4;
                if (v[k] >= level) polygon.Add(origin + new Vector3(px[k], height, pz[k]));
                if ((v[k] >= level) != (v[b] >= level))
                {
                    var t = (level - v[k]) / (v[b] - v[k]);
                    polygon.Add(origin + new Vector3(Mathf.Lerp(px[k], px[b], t), height, Mathf.Lerp(pz[k], pz[b], t)));
                }
            }
            into.Fan(polygon);
        }

        /// <summary>A massive sea stack: a seven-sided crag, broad at the foot, leaning, its top broken.</summary>
        private static void Rock(Mesher m, Vector3 at, float radius, float height, SeedRandom rng)
        {
            const int sides = 7;
            var turn = rng.Range(0f, Mathf.PI * 2f);
            var lean = new Vector3(rng.Range(-0.25f, 0.25f), 0f, rng.Range(-0.25f, 0.25f)) * radius;
            var low = new Vector3[sides];
            var mid = new Vector3[sides];
            var top = new Vector3[sides];
            for (var k = 0; k < sides; k++)
            {
                var a = turn + k * Mathf.PI * 2f / sides;
                var r = radius * rng.Range(0.8f, 1.15f);
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                low[k] = at + dir * r;
                mid[k] = at + dir * r * rng.Range(0.7f, 0.85f) + new Vector3(0f, height * rng.Range(0.45f, 0.6f), 0f) + lean * 0.5f;
                top[k] = at + dir * r * rng.Range(0.3f, 0.5f) + new Vector3(0f, height * rng.Range(0.85f, 1.05f), 0f) + lean;
            }
            var rock = ArchipelagoMeshes.PaletteUvOf(ArchipelagoMeshes.RockMidIndex);
            var dark = ArchipelagoMeshes.PaletteUvOf(ArchipelagoMeshes.RockDarkIndex);
            var cap = Vector3.zero;
            for (var k = 0; k < sides; k++)
            {
                var b = (k + 1) % sides;
                m.Face(at, low[k], mid[k], mid[b], low[b], dark);
                m.Face(at, mid[k], top[k], top[b], mid[b], rock);
                cap += top[k];
            }
            cap /= sides;
            for (var k = 0; k < sides; k++) m.Face(at, top[k], cap, cap, top[(k + 1) % sides], rock);
        }

        /// <summary>Vertices, uvs and triangles of one mesh being built.</summary>
        private sealed class Mesher
        {
            private readonly List<Vector3> _v = new List<Vector3>();
            private readonly List<Vector2> _uv = new List<Vector2>();
            private readonly List<int> _t = new List<int>();

            /// <summary>A flat polygon (corners running x+, then z+), wound to face up.</summary>
            public void Fan(List<Vector3> polygon)
            {
                if (polygon.Count < 3) return;
                var s = _v.Count;
                foreach (var p in polygon)
                {
                    _v.Add(p);
                    _uv.Add(Vector2.zero);
                }
                for (var k = 1; k < polygon.Count - 1; k++) _t.AddRange(new[] { s, s + k + 1, s + k });
            }

            public void Rect(Vector3 origin, float x0, float z0, float x1, float z1, float height)
            {
                Fan(new List<Vector3>
                {
                    origin + new Vector3(x0, height, z0), origin + new Vector3(x1, height, z0),
                    origin + new Vector3(x1, height, z1), origin + new Vector3(x0, height, z1),
                });
            }

            /// <summary>A flat ring of foam round a point.</summary>
            public void Ring(Vector3 centre, float from, float to, int segments)
            {
                for (var k = 0; k < segments; k++)
                {
                    float a0 = k * Mathf.PI * 2f / segments, a1 = (k + 1) * Mathf.PI * 2f / segments;
                    var wobble = 1f + 0.15f * Mathf.Sin(k * 2.3f);
                    var s = _v.Count;
                    _v.Add(centre + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * from);
                    _v.Add(centre + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * to * wobble);
                    _v.Add(centre + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * to * wobble);
                    _v.Add(centre + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * from);
                    for (var i = 0; i < 4; i++) _uv.Add(Vector2.zero);
                    // seen from above the corners run outwards then round anticlockwise: wind it the other way
                    _t.AddRange(new[] { s, s + 2, s + 1, s, s + 3, s + 2 });
                }
            }

            /// <summary>
            /// A wave crest: a long thin bow of foam along the side, tapered at both ends, bowed
            /// towards the islands like a breaking wave.
            /// </summary>
            public void Crest(Vector3 origin, int n, int side, float along, float depth, float length, float width, float bow, float height)
            {
                const int pieces = 6;
                var start = _v.Count;
                for (var k = 0; k <= pieces; k++)
                {
                    var t = k / (float)pieces;
                    var a = along + (t - 0.5f) * length;
                    var arc = Mathf.Sin(t * Mathf.PI);
                    var d = depth + arc * bow;
                    var half = width * (0.25f + 0.75f * arc);
                    var inner = OnSide(n, side, a, d + half);
                    var outer = OnSide(n, side, a, d - half);
                    _v.Add(origin + new Vector3(inner.x, height, inner.y));
                    _v.Add(origin + new Vector3(outer.x, height, outer.y));
                    _uv.Add(Vector2.zero);
                    _uv.Add(Vector2.zero);
                }
                for (var k = 0; k < pieces; k++)
                {
                    var s = start + k * 2;
                    // both windings: the four sides run their 'along' opposite ways round
                    _t.AddRange(new[] { s, s + 1, s + 2, s + 1, s + 3, s + 2 });
                    _t.AddRange(new[] { s, s + 2, s + 1, s + 1, s + 2, s + 3 });
                }
            }

            /// <summary>A cloud's puff: a squashed eight-faced blob.</summary>
            public void Puff(Vector3 centre, Vector3 size)
            {
                var top = centre + new Vector3(0f, size.y, 0f);
                var bottom = centre - new Vector3(0f, size.y * 0.4f, 0f);
                var ring = new[]
                {
                    centre + new Vector3(size.x, 0f, 0f), centre + new Vector3(0f, 0f, size.z),
                    centre + new Vector3(-size.x, 0f, 0f), centre + new Vector3(0f, 0f, -size.z),
                };
                for (var k = 0; k < 4; k++)
                {
                    var a = ring[k];
                    var b = ring[(k + 1) % 4];
                    Tri(top, b, a, centre);
                    Tri(bottom, a, b, centre);
                }
            }

            /// <summary>One flat triangle, wound to face away from `inside`.</summary>
            private void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 inside)
            {
                var s = _v.Count;
                _v.Add(a);
                _v.Add(b);
                _v.Add(c);
                for (var i = 0; i < 3; i++) _uv.Add(Vector2.zero);
                var normal = Vector3.Cross(b - a, c - a);
                if (Vector3.Dot(normal, (a + b + c) / 3f - inside) >= 0f) _t.AddRange(new[] { s, s + 1, s + 2 });
                else _t.AddRange(new[] { s, s + 2, s + 1 });
            }

            /// <summary>A flat four-cornered face in one palette colour, wound to face out from `axis`.</summary>
            public void Face(Vector3 axis, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 colour)
            {
                var s = _v.Count;
                _v.Add(a);
                _v.Add(b);
                _v.Add(c);
                _v.Add(d);
                for (var k = 0; k < 4; k++) _uv.Add(colour);
                var centre = (a + b + c + d) * 0.25f;
                var outward = centre - new Vector3(axis.x, centre.y - 0.5f, axis.z);
                var normal = Vector3.Cross(b - a, d - a);
                if (normal.sqrMagnitude < 1e-8f) normal = Vector3.Cross(c - b, d - b);
                if (Vector3.Dot(normal, outward) >= 0f) _t.AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3 });
                else _t.AddRange(new[] { s, s + 2, s + 1, s, s + 3, s + 2 });
            }

            public GameObject Emit(string name, Transform parent, Material material)
            {
                if (_t.Count == 0) return null;
                var mesh = new Mesh { name = name, indexFormat = _v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(_v);
                mesh.SetUVs(0, _uv);
                mesh.SetTriangles(_t, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(parent, false);
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                return go;
            }
        }
    }
}
