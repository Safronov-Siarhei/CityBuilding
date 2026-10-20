using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Grid;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// The ground under the placement drawing in PlacementLook.DrawingOnParchment: a sheet of old
    /// parchment a little bigger than the footprint, its edge torn, its rim browned, a soft shadow
    /// under it; a red ink blot on every cell that is in the way; the whole sheet dulled when the
    /// building cannot stand. Textures are painted once, in code -- nothing to ship.
    /// </summary>
    public static class ParchmentSheet
    {
        private const float Margin = 0.35f;        // how far the sheet reaches past the footprint
        private const float TearDepth = 0.14f;     // how deep the torn edge bites in
        private const float Step = 0.2f;           // metres between points along the edge
        private const float SheetHeight = 0.05f;
        private const float ShadowOffset = 0.12f;

        private static readonly Color PaperTint = Color.white;
        private static readonly Color PaperDulled = new Color(0.78f, 0.7f, 0.64f, 1f);
        private static readonly Color Shadow = new Color(0.12f, 0.09f, 0.05f, 0.35f);
        private static readonly Color Ink = new Color(0.58f, 0.07f, 0.05f, 0.92f);

        private static Material _paper;
        private static Material _paperDulled;
        private static Material _shadow;
        private static Material _blot;

        public static void Build(Mesh mesh, MeshRenderer renderer, float groundY, Vector2Int origin, Vector2Int footprint, bool valid, List<bool> blocked)
        {
            var grid = GridManager.Instance;
            var size = grid.CellSize;
            var corner = grid.CellToWorld(origin);
            var min = new Vector2(corner.x - Margin, corner.z - Margin);
            var max = new Vector2(corner.x + footprint.x * size + Margin, corner.z + footprint.y * size + Margin);
            var h = groundY + SheetHeight;

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var shadow = new List<int>();
            var paper = new List<int>();
            var blots = new List<int>();

            // The torn edge depends on the footprint only, not on where the sheet is, so it does not
            // shimmer as the drawing hops from cell to cell.
            var outline = TornOutline(max - min, footprint.x * 31 + footprint.y * 17);

            // the shadow: the same outline, a little larger, nudged down-right, soft at its edge
            Fan(vertices, uvs, shadow, outline, min + new Vector2(ShadowOffset, -ShadowOffset), max - min, h - 0.01f, 0.12f, true);
            Fan(vertices, uvs, paper, outline, min, max - min, h, 0f, false);

            var k = 0;
            for (var z = 0; z < footprint.y; z++)
            {
                for (var x = 0; x < footprint.x; x++, k++)
                {
                    if (k >= blocked.Count || !blocked[k]) continue;
                    var centre = new Vector2(corner.x + (x + 0.5f) * size, corner.z + (z + 0.5f) * size);
                    // each cell its own turn and size, fixed by where it is in the footprint
                    var angle = Hash(x, z, 7) * Mathf.PI * 2f;
                    var half = size * (0.62f + 0.18f * Hash(x, z, 11));
                    var axisA = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * half;
                    var axisB = new Vector2(-axisA.y, axisA.x);
                    var i = vertices.Count;
                    foreach (var (corner2, uv) in new[]
                    {
                        (centre - axisA - axisB, new Vector2(0f, 0f)), (centre - axisA + axisB, new Vector2(0f, 1f)),
                        (centre + axisA + axisB, new Vector2(1f, 1f)), (centre + axisA - axisB, new Vector2(1f, 0f)),
                    })
                    {
                        vertices.Add(new Vector3(corner2.x, h + 0.008f, corner2.y));
                        uvs.Add(uv);
                    }
                    blots.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
                }
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            var materials = new List<Material>();
            var sub = new List<List<int>> { shadow, paper };
            materials.Add(ShadowMaterial());
            materials.Add(valid ? PaperMaterial() : PaperDulledMaterial());
            if (blots.Count > 0)
            {
                sub.Add(blots);
                materials.Add(BlotMaterial());
            }
            mesh.subMeshCount = sub.Count;
            for (var s = 0; s < sub.Count; s++) mesh.SetTriangles(sub[s], s);
            mesh.RecalculateBounds();
            renderer.sharedMaterials = materials.ToArray();
        }

        /// <summary>Points round a sheet of this size (from its lower-left corner), the edge bitten in irregularly.</summary>
        private static List<Vector2> TornOutline(Vector2 size, int seed)
        {
            var points = new List<Vector2>();
            void Side(Vector2 from, Vector2 to, Vector2 inward, int side)
            {
                var length = Vector2.Distance(from, to);
                var steps = Mathf.Max(2, Mathf.RoundToInt(length / Step));
                for (var i = 0; i < steps; i++)
                {
                    var t = i / (float)steps;
                    var bite = TearDepth * (0.35f * Hash(i, side, seed) + 0.65f * Smooth(i * 0.35f, side + seed));
                    // corners bite deeper: old paper goes at the corners first
                    var cornerness = 1f - Mathf.Min(t, 1f - t) * 2f;
                    bite += TearDepth * 0.8f * cornerness * cornerness * Hash(side, seed, 3);
                    points.Add(Vector2.Lerp(from, to, t) + inward * bite);
                }
            }
            Side(Vector2.zero, new Vector2(size.x, 0f), Vector2.up, 0);
            Side(new Vector2(size.x, 0f), size, Vector2.left, 1);
            Side(size, new Vector2(0f, size.y), Vector2.down, 2);
            Side(new Vector2(0f, size.y), Vector2.zero, Vector2.right, 3);
            return points;
        }

        /// <summary>A fan from the middle; `grow` pushes the outline outward (the shadow), `soft` fades it to nothing at the rim.</summary>
        private static void Fan(List<Vector3> vertices, List<Vector2> uvs, List<int> triangles, List<Vector2> outline, Vector2 min, Vector2 size, float y, float grow, bool soft)
        {
            var centre = size * 0.5f;
            var c = vertices.Count;
            vertices.Add(new Vector3(min.x + centre.x, y, min.y + centre.y));
            uvs.Add(soft ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0.5f));
            foreach (var p in outline)
            {
                var q = p + (p - centre).normalized * grow;
                vertices.Add(new Vector3(min.x + q.x, y, min.y + q.y));
                // the shadow's texture fades with distance from the middle, so its rim gets uv (1, *)
                uvs.Add(soft ? new Vector2(1f, 0.5f) : new Vector2(q.x / size.x, q.y / size.y));
            }
            for (var i = 0; i < outline.Count; i++)
            {
                triangles.Add(c);
                triangles.Add(c + 1 + (i + 1) % outline.Count);
                triangles.Add(c + 1 + i);
            }
        }

        // ------------------------------------------------------------------ materials and textures

        private static Material PaperMaterial() => _paper != null ? _paper : _paper = Make(PaintParchment(), PaperTint, false);
        private static Material PaperDulledMaterial() => _paperDulled != null ? _paperDulled : _paperDulled = Make(PaintParchment(), PaperDulled, false);
        private static Material ShadowMaterial() => _shadow != null ? _shadow : _shadow = Make(PaintFade(), Shadow, true);
        private static Material BlotMaterial() => _blot != null ? _blot : _blot = Make(PaintBlot(), Ink, true);

        private static Material Make(Texture2D texture, Color color, bool transparent)
        {
            var material = new Material(RuntimeShaders.Unlit) { color = color, mainTexture = texture };
            material.SetFloat("_Cull", (float)CullMode.Off);
            if (transparent)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                material.renderQueue = (int)RenderQueue.Geometry + 10;
            }
            return material;
        }

        private static Texture2D _parchmentTexture;

        /// <summary>Old paper: cream, mottled, a few fibres, browning towards the rim.</summary>
        private static Texture2D PaintParchment()
        {
            if (_parchmentTexture != null) return _parchmentTexture;
            const int n = 128;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var cream = new Color(0.95f, 0.89f, 0.74f);
            var stain = new Color(0.82f, 0.7f, 0.5f);
            var burnt = new Color(0.55f, 0.38f, 0.2f);
            for (var y = 0; y < n; y++)
            {
                for (var x = 0; x < n; x++)
                {
                    var u = x / (float)n;
                    var v = y / (float)n;
                    var mottle = Fbm(u * 6f, v * 6f, 5);
                    var fibre = Mathf.Pow(Mathf.Abs(Mathf.Sin((u * 40f + Fbm(u * 3f, v * 30f, 9) * 6f))), 30f) * 0.25f;
                    var edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
                    var rim = 1f - Mathf.Clamp01(edge / (0.12f + 0.06f * Fbm(u * 9f, v * 9f, 13)));
                    var c = Color.Lerp(cream, stain, Mathf.Clamp01(mottle * 1.2f - 0.35f) * 0.55f);
                    c = Color.Lerp(c, stain, fibre);
                    c = Color.Lerp(c, burnt, rim * rim * 0.8f);
                    c.a = 1f;
                    texture.SetPixel(x, y, c);
                }
            }
            texture.Apply(true);
            return _parchmentTexture = texture;
        }

        private static Texture2D _fadeTexture;

        /// <summary>Opaque at u = 0.5 (the middle of the fan), clear at u = 1 (its rim).</summary>
        private static Texture2D PaintFade()
        {
            if (_fadeTexture != null) return _fadeTexture;
            const int n = 32;
            var texture = new Texture2D(n, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var x = 0; x < n; x++)
            {
                var t = Mathf.Clamp01((x / (float)(n - 1) - 0.5f) * 2f);
                var a = 1f - Mathf.SmoothStep(0.55f, 1f, t);
                texture.SetPixel(x, 0, new Color(1f, 1f, 1f, a));
            }
            texture.Apply();
            return _fadeTexture = texture;
        }

        private static Texture2D _blotTexture;

        /// <summary>A splash of ink: a ragged pool, darker where it dried at the rim, and a scatter of drops round it.</summary>
        private static Texture2D PaintBlot()
        {
            if (_blotTexture != null) return _blotTexture;
            const int n = 128;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            var drops = new List<(Vector2 c, float r)>();
            for (var i = 0; i < 9; i++)
            {
                var angle = Hash(i, 1, 41) * Mathf.PI * 2f;
                var distance = 0.3f + 0.15f * Hash(i, 2, 41);
                drops.Add((new Vector2(0.5f, 0.5f) + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance, 0.015f + 0.035f * Hash(i, 3, 41)));
            }
            for (var y = 0; y < n; y++)
            {
                for (var x = 0; x < n; x++)
                {
                    var p = new Vector2((x + 0.5f) / n, (y + 0.5f) / n);
                    var d = p - new Vector2(0.5f, 0.5f);
                    var angle = Mathf.Atan2(d.y, d.x);
                    // the pool's rim wanders: a few lobes, then finer ragging
                    var radius = 0.24f + 0.05f * Mathf.Sin(angle * 3f + 1.3f) + 0.03f * Mathf.Sin(angle * 7f + 0.4f)
                                 + 0.04f * (Fbm(Mathf.Cos(angle) * 2f + 3f, Mathf.Sin(angle) * 2f + 3f, 21) - 0.5f);
                    var inside = radius - d.magnitude;
                    var alpha = Mathf.Clamp01(inside * 90f);
                    foreach (var (c, r) in drops) alpha = Mathf.Max(alpha, Mathf.Clamp01((r - Vector2.Distance(p, c)) * 160f));
                    // dried rim: a touch darker (more opaque) just inside the edge, lighter in the middle
                    var rimDark = Mathf.Clamp01(1f - Mathf.Abs(inside - 0.012f) * 60f);
                    var body = 0.82f + 0.18f * rimDark - 0.1f * Fbm(p.x * 8f, p.y * 8f, 23);
                    texture.SetPixel(x, y, new Color(body, body, body, alpha));
                }
            }
            texture.Apply(true);
            return _blotTexture = texture;
        }

        // ------------------------------------------------------------------ noise

        private static float Hash(int a, int b, int seed)
        {
            unchecked
            {
                var h = (uint)(a * 374761393 + b * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        private static float Smooth(float x, int seed)
        {
            var i = Mathf.FloorToInt(x);
            var f = x - i;
            f = f * f * (3f - 2f * f);
            return Mathf.Lerp(Hash(i, 0, seed), Hash(i + 1, 0, seed), f);
        }

        private static float Value(float x, float y, int seed)
        {
            var ix = Mathf.FloorToInt(x);
            var iy = Mathf.FloorToInt(y);
            var fx = x - ix;
            var fy = y - iy;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            var a = Mathf.Lerp(Hash(ix, iy, seed), Hash(ix + 1, iy, seed), fx);
            var b = Mathf.Lerp(Hash(ix, iy + 1, seed), Hash(ix + 1, iy + 1, seed), fx);
            return Mathf.Lerp(a, b, fy);
        }

        private static float Fbm(float x, float y, int seed)
        {
            var sum = 0f;
            var amplitude = 0.5f;
            for (var o = 0; o < 4; o++)
            {
                sum += Value(x, y, seed + o * 17) * amplitude;
                x *= 2f;
                y *= 2f;
                amplitude *= 0.5f;
            }
            return sum / 0.9375f;
        }
    }
}
