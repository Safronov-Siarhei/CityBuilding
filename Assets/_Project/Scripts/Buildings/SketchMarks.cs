using System.Collections.Generic;
using CityBuilder.Grid;
using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// The pen-and-ink marks on the ground while a building is being placed (the user's pick,
    /// 2026-10-20). Under a building: each side of the plot is two wandering strokes of uneven
    /// weight that run on past the corners, the cells are faint dotted lines, a cell in the way is
    /// hatched with red ink the way old maps mark ground nobody may cross, and a soft shadow lies
    /// under the hovering drawing.
    /// </summary>
    public static class SketchMarks
    {
        private const float Height = 0.05f;
        private const float Step = 0.15f;           // metres between points along a stroke
        private const float Wander = 0.035f;        // how far a stroke strays from the true line
        private const float Overshoot = 0.28f;      // how far past a corner a stroke runs

        private static readonly Color InkBlue = new Color(0.16f, 0.27f, 0.55f, 0.95f);
        private static readonly Color InkBlueLight = new Color(0.16f, 0.27f, 0.55f, 0.5f);
        private static readonly Color InkRed = new Color(0.62f, 0.17f, 0.1f, 0.95f);
        private static readonly Color InkRedLight = new Color(0.62f, 0.17f, 0.1f, 0.5f);
        private static readonly Color DotColour = new Color(0.16f, 0.27f, 0.55f, 0.35f);
        private static readonly Color HatchColour = new Color(0.66f, 0.1f, 0.07f, 0.9f);
        private static readonly Color ShadowColour = new Color(0.08f, 0.1f, 0.06f, 0.32f);

        private static Material _blue, _blueLight, _red, _redLight, _dots, _hatch, _shadow;

        // Reused from call to call: while the pen draws the outline the mesh is rebuilt every frame
        // for a fraction of a second, and that should not leave garbage behind each time.
        private static readonly List<Vector3> Vertices = new List<Vector3>();
        private static readonly List<Vector2> Uvs = new List<Vector2>();
        private static readonly List<int> Strong = new List<int>();
        private static readonly List<int> Light = new List<int>();
        private static readonly List<int> DotTris = new List<int>();
        private static readonly List<int> HatchTris = new List<int>();
        private static readonly List<int> ShadowTris = new List<int>();
        private static readonly List<List<int>> Parts = new List<List<int>>();
        private static readonly List<Material> Materials = new List<Material>();

        private static float _h;

        /// <summary>
        /// The marks under a building. `reveal` runs 0..1 while the pen draws the plot
        /// (PlacementPreview animates it): the four sides one after another, each firm stroke
        /// followed by its light second pass, then the hatching, then the dots.
        /// </summary>
        public static void Build(Mesh mesh, MeshRenderer renderer, float groundY, Vector2Int origin, Vector2Int footprint, bool valid, List<bool> blocked, float reveal = 1f)
        {
            Begin(groundY);
            var grid = GridManager.Instance;
            var size = grid.CellSize;
            var corner = grid.CellToWorld(origin);
            var x0 = corner.x;
            var z0 = corner.z;
            var x1 = x0 + footprint.x * size;
            var z1 = z0 + footprint.y * size;

            // the drawing's shadow: a soft dark patch a little inside the plot, under where it hovers
            Quad(ShadowTris, new Vector2(x0 + 0.1f, z0 + 0.1f), new Vector2(x1 - 0.1f, z1 - 0.1f), -0.02f, Vector2.zero, Vector2.one);

            // the outline: per side one firm stroke and a lighter second pass beside it
            for (var s = 0; s < 4; s++)
            {
                var a = s == 0 ? new Vector2(x0, z0) : s == 1 ? new Vector2(x1, z0) : s == 2 ? new Vector2(x1, z1) : new Vector2(x0, z1);
                var b = s == 0 ? new Vector2(x1, z0) : s == 1 ? new Vector2(x1, z1) : s == 2 ? new Vector2(x0, z1) : new Vector2(x0, z0);
                var along = (b - a).normalized;
                var outward = new Vector2(along.y, -along.x);
                // seeded by the side and the footprint's size, not by where it is, so it does not crawl as it moves
                var seed = s * 13 + footprint.x * 7 + footprint.y * 3;
                var over1 = Overshoot * (0.6f + 0.6f * Ink.Hash(s, 1, seed));
                var over2 = Overshoot * (0.6f + 0.6f * Ink.Hash(s, 2, seed));
                Stroke(Strong, a - along * over1 * 0.7f, b + along * over2, 0.07f, seed, 0.006f, Mathf.Clamp01((reveal - s * 0.15f) / 0.3f));
                var shift = outward * (0.05f + 0.03f * Ink.Hash(s, 3, seed));
                Stroke(Light, a + shift - along * over2 * 0.4f, b + shift + along * over1 * 0.5f, 0.045f, seed + 101, 0.005f,
                    Mathf.Clamp01((reveal - s * 0.15f - 0.12f) / 0.3f));
            }

            // the cells: faint dots along the lines between them
            if (reveal >= 0.95f)
            {
                for (var x = 1; x < footprint.x; x++) Dotted(new Vector2(x0 + x * size, z0), new Vector2(x0 + x * size, z1));
                for (var z = 1; z < footprint.y; z++) Dotted(new Vector2(x0, z0 + z * size), new Vector2(x1, z0 + z * size));
            }

            // the hatching: one patch per cell in the way, all at the same slant so neighbours read as one area
            if (reveal >= 0.75f)
            {
                var k = 0;
                for (var z = 0; z < footprint.y; z++)
                {
                    for (var x = 0; x < footprint.x; x++, k++)
                    {
                        if (k < blocked.Count && blocked[k]) Hatch(new Vector2(x0 + x * size, z0 + z * size), size, x, z);
                    }
                }
            }

            Finish(mesh, renderer, valid);
        }

        // ------------------------------------------------------------------ pieces

        private static void Begin(float groundY)
        {
            _h = groundY + Height;
            Vertices.Clear();
            Uvs.Clear();
            Strong.Clear();
            Light.Clear();
            DotTris.Clear();
            HatchTris.Clear();
            ShadowTris.Clear();
        }

        /// <summary>
        /// A pen stroke from a to b, drawn as far as `drawn` (0..1): wandering off the line and back,
        /// thicker in the middle where the pen pressed, tapering at both ends.
        /// </summary>
        private static void Stroke(List<int> into, Vector2 a, Vector2 b, float width, int seed, float lift, float drawn)
        {
            if (drawn <= 0f) return;
            var along = (b - a).normalized;
            var across = new Vector2(-along.y, along.x);
            var length = Vector2.Distance(a, b);
            var steps = Mathf.Max(2, Mathf.CeilToInt(length / Step));
            // as far as the pen has got: whole steps, then the last one cut where the nib is
            var reach = drawn * steps;
            var shown = Mathf.CeilToInt(reach);
            var first = Vertices.Count;
            for (var i = 0; i <= shown; i++)
            {
                var t = Mathf.Min(i, reach) / steps;
                var wobble = Wander * (Ink.Smooth(t * length * 1.6f, seed) * 2f - 1f);
                var pressure = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t * 1.15f - 0.05f));
                var w = width * (0.35f + 0.65f * pressure) * (0.85f + 0.3f * Ink.Smooth(t * 5f, seed + 3));
                var p = Vector2.Lerp(a, b, t) + across * wobble;
                Vertices.Add(new Vector3(p.x - across.x * w * 0.5f, _h + lift, p.y - across.y * w * 0.5f));
                Vertices.Add(new Vector3(p.x + across.x * w * 0.5f, _h + lift, p.y + across.y * w * 0.5f));
                Uvs.Add(Vector2.zero);
                Uvs.Add(Vector2.zero);
            }
            for (var i = 0; i < shown; i++)
            {
                var v = first + i * 2;
                into.Add(v); into.Add(v + 2); into.Add(v + 1);
                into.Add(v + 1); into.Add(v + 2); into.Add(v + 3);
            }
        }

        private static void Quad(List<int> into, Vector2 min, Vector2 max, float lift, Vector2 uvMin, Vector2 uvMax)
        {
            var v = Vertices.Count;
            Vertices.Add(new Vector3(min.x, _h + lift, min.y));
            Vertices.Add(new Vector3(min.x, _h + lift, max.y));
            Vertices.Add(new Vector3(max.x, _h + lift, max.y));
            Vertices.Add(new Vector3(max.x, _h + lift, min.y));
            Uvs.Add(uvMin);
            Uvs.Add(new Vector2(uvMin.x, uvMax.y));
            Uvs.Add(uvMax);
            Uvs.Add(new Vector2(uvMax.x, uvMin.y));
            into.Add(v); into.Add(v + 1); into.Add(v + 2);
            into.Add(v); into.Add(v + 2); into.Add(v + 3);
        }

        private static void Dotted(Vector2 a, Vector2 b)
        {
            var count = Mathf.FloorToInt(Vector2.Distance(a, b) / 0.22f);
            for (var i = 1; i < count; i++)
            {
                var p = Vector2.Lerp(a, b, i / (float)count);
                const float r = 0.028f;
                Quad(DotTris, p - new Vector2(r, r), p + new Vector2(r, r), 0.003f, Vector2.zero, Vector2.zero);
            }
        }

        /// <summary>One cell's patch of hatching; a different quarter of the texture per cell, so no two patches are alike.</summary>
        private static void Hatch(Vector2 cellCorner, float size, int x, int z)
        {
            var shift = new Vector2(Ink.Hash(x, z, 51) < 0.5f ? 0f : 0.5f, Ink.Hash(x, z, 53) < 0.5f ? 0f : 0.5f);
            Quad(HatchTris, cellCorner - new Vector2(0.04f, 0.04f), cellCorner + new Vector2(size + 0.04f, size + 0.04f), 0.004f,
                shift, shift + new Vector2(0.5f, 0.5f));
        }

        private static void Finish(Mesh mesh, MeshRenderer renderer, bool valid)
        {
            mesh.Clear();
            mesh.SetVertices(Vertices);
            mesh.SetUVs(0, Uvs);
            Parts.Clear();
            Materials.Clear();
            Add(ShadowTris, ShadowMaterial());
            Add(DotTris, Mat(ref _dots, DotColour, null));
            Add(HatchTris, Mat(ref _hatch, HatchColour, PaintHatching()));
            Add(Light, valid ? Mat(ref _blueLight, InkBlueLight, null) : Mat(ref _redLight, InkRedLight, null));
            Add(Strong, valid ? Mat(ref _blue, InkBlue, null) : Mat(ref _red, InkRed, null));
            mesh.subMeshCount = Parts.Count;
            for (var s = 0; s < Parts.Count; s++) mesh.SetTriangles(Parts[s], s);
            mesh.RecalculateBounds();
            renderer.sharedMaterials = Materials.ToArray();
        }

        private static void Add(List<int> part, Material material)
        {
            if (part.Count == 0) return;
            Parts.Add(part);
            Materials.Add(material);
        }

        // ------------------------------------------------------------------ materials and textures

        private static Material Mat(ref Material cached, Color color, Texture2D texture)
        {
            if (cached != null) return cached;
            return cached = Ink.Make(texture != null ? texture : Texture2D.whiteTexture, color);
        }

        private static Material ShadowMaterial()
        {
            if (_shadow != null) return _shadow;
            // soft on every side: opaque in the middle, nothing at the rim
            const int n = 32;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < n; y++)
            {
                for (var x = 0; x < n; x++)
                {
                    var edge = Mathf.Min(Mathf.Min(x + 0.5f, n - x - 0.5f), Mathf.Min(y + 0.5f, n - y - 0.5f)) / (n * 0.5f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(edge / 0.55f))));
                }
            }
            texture.Apply();
            _shadow = Ink.Make(texture, ShadowColour);
            _shadow.renderQueue -= 1;
            return _shadow;
        }

        private static Texture2D _hatching;

        /// <summary>
        /// Two cells' worth of hatching (a cell takes a quarter of it): slanted pen lines of uneven
        /// weight, each stopping a little short of the cell's edge at its own place, so the patch has
        /// the ragged rim of hand work.
        /// </summary>
        private static Texture2D PaintHatching()
        {
            if (_hatching != null) return _hatching;
            const int n = 256;
            const float spacing = 1f / 14f;       // in texture units: seven lines per cell
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat };
            for (var y = 0; y < n; y++)
            {
                for (var x = 0; x < n; x++)
                {
                    var u = (x + 0.5f) / n;
                    var v = (y + 0.5f) / n;
                    var diagonal = (u + v) / spacing;                     // which line, along the slant
                    var index = Mathf.FloorToInt(diagonal + 0.5f);
                    var offset = Mathf.Abs(diagonal - index);
                    var along = u - v;                                    // where on that line
                    var wobble = 0.08f * (Ink.Smooth(along * 12f, index * 7 + 1) - 0.5f);
                    var weight = 0.13f + 0.08f * Ink.Hash(index, 0, 61);
                    var stroke = Mathf.Clamp01((weight - Mathf.Abs(offset + wobble)) * 40f);
                    // the ragged rim: each cell-sized window fades out near its border at a noisy depth
                    var cu = Mathf.Repeat(u * 2f, 1f);
                    var cv = Mathf.Repeat(v * 2f, 1f);
                    var edge = Mathf.Min(Mathf.Min(cu, 1f - cu), Mathf.Min(cv, 1f - cv));
                    var ragged = 0.03f + 0.06f * Ink.Hash(index, Mathf.FloorToInt(along * 6f), 67);
                    var keep = Mathf.Clamp01((edge - ragged) * 60f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, stroke * keep));
                }
            }
            texture.Apply(true);
            return _hatching = texture;
        }
    }
}
