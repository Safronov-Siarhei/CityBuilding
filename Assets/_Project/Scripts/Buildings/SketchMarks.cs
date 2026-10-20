using System.Collections.Generic;
using CityBuilder.Grid;
using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// The ground under the placement drawing in PlacementLook.DrawingSketched, as if sketched with
    /// a pen: each side of the plot is two wandering strokes of uneven weight that run on past the
    /// corners, the cells are faint dotted lines, and a cell in the way is hatched with red ink
    /// the way old maps mark ground nobody may cross.
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
        private static readonly Color Dots = new Color(0.16f, 0.27f, 0.55f, 0.35f);
        private static readonly Color Hatch = new Color(0.66f, 0.1f, 0.07f, 0.9f);

        private static Material _blue, _blueLight, _red, _redLight, _dots, _hatch;

        public static void Build(Mesh mesh, MeshRenderer renderer, float groundY, Vector2Int origin, Vector2Int footprint, bool valid, List<bool> blocked)
        {
            var grid = GridManager.Instance;
            var size = grid.CellSize;
            var corner = grid.CellToWorld(origin);
            var x0 = corner.x;
            var z0 = corner.z;
            var x1 = x0 + footprint.x * size;
            var z1 = z0 + footprint.y * size;
            var h = groundY + Height;

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var strong = new List<int>();
            var light = new List<int>();
            var dots = new List<int>();
            var hatch = new List<int>();

            // A pen stroke from a to b: wandering off the line and back, thicker in the middle
            // where the pen pressed, tapering at both ends. The seed fixes its wobble to the side
            // of the footprint, so the sketch does not crawl as the drawing moves.
            void Stroke(List<int> into, Vector2 a, Vector2 b, float width, int seed, float lift)
            {
                var along = (b - a).normalized;
                var across = new Vector2(-along.y, along.x);
                var length = Vector2.Distance(a, b);
                var steps = Mathf.Max(2, Mathf.CeilToInt(length / Step));
                var first = vertices.Count;
                for (var i = 0; i <= steps; i++)
                {
                    var t = i / (float)steps;
                    var wobble = Wander * (ParchmentSheet.Smooth(t * length * 1.6f, seed) * 2f - 1f);
                    var pressure = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t * 1.15f - 0.05f));
                    var w = width * (0.35f + 0.65f * pressure) * (0.85f + 0.3f * ParchmentSheet.Smooth(t * 5f, seed + 3));
                    var p = Vector2.Lerp(a, b, t) + across * wobble;
                    vertices.Add(new Vector3(p.x - across.x * w * 0.5f, h + lift, p.y - across.y * w * 0.5f));
                    vertices.Add(new Vector3(p.x + across.x * w * 0.5f, h + lift, p.y + across.y * w * 0.5f));
                    uvs.Add(Vector2.zero);
                    uvs.Add(Vector2.zero);
                }
                for (var i = 0; i < steps; i++)
                {
                    var v = first + i * 2;
                    into.AddRange(new[] { v, v + 2, v + 1, v + 1, v + 2, v + 3 });
                }
            }

            // the outline: per side one firm stroke and a lighter second pass beside it
            var sides = new[]
            {
                (new Vector2(x0, z0), new Vector2(x1, z0)), (new Vector2(x1, z0), new Vector2(x1, z1)),
                (new Vector2(x1, z1), new Vector2(x0, z1)), (new Vector2(x0, z1), new Vector2(x0, z0)),
            };
            for (var s = 0; s < sides.Length; s++)
            {
                var (a, b) = sides[s];
                var along = (b - a).normalized;
                var outward = new Vector2(along.y, -along.x);
                var seed = s * 13 + footprint.x * 7 + footprint.y * 3;
                var over1 = Overshoot * (0.6f + 0.6f * ParchmentSheet.Hash(s, 1, seed));
                var over2 = Overshoot * (0.6f + 0.6f * ParchmentSheet.Hash(s, 2, seed));
                Stroke(strong, a - along * over1 * 0.7f, b + along * over2, 0.07f, seed, 0.006f);
                var shift = outward * (0.05f + 0.03f * ParchmentSheet.Hash(s, 3, seed));
                Stroke(light, a + shift - along * over2 * 0.4f, b + shift + along * over1 * 0.5f, 0.045f, seed + 101, 0.005f);
            }

            // the cells: faint dots along the lines between them
            void Dotted(Vector2 a, Vector2 b)
            {
                var length = Vector2.Distance(a, b);
                var count = Mathf.FloorToInt(length / 0.22f);
                for (var i = 1; i < count; i++)
                {
                    var p = Vector2.Lerp(a, b, i / (float)count);
                    var v = vertices.Count;
                    const float r = 0.028f;
                    vertices.Add(new Vector3(p.x - r, h + 0.003f, p.y - r));
                    vertices.Add(new Vector3(p.x - r, h + 0.003f, p.y + r));
                    vertices.Add(new Vector3(p.x + r, h + 0.003f, p.y + r));
                    vertices.Add(new Vector3(p.x + r, h + 0.003f, p.y - r));
                    for (var u = 0; u < 4; u++) uvs.Add(Vector2.zero);
                    dots.AddRange(new[] { v, v + 1, v + 2, v, v + 2, v + 3 });
                }
            }
            for (var x = 1; x < footprint.x; x++) Dotted(new Vector2(x0 + x * size, z0), new Vector2(x0 + x * size, z1));
            for (var z = 1; z < footprint.y; z++) Dotted(new Vector2(x0, z0 + z * size), new Vector2(x1, z0 + z * size));

            // the hatching: one patch per cell in the way, all at the same slant so neighbours read as one area
            var k = 0;
            for (var z = 0; z < footprint.y; z++)
            {
                for (var x = 0; x < footprint.x; x++, k++)
                {
                    if (k >= blocked.Count || !blocked[k]) continue;
                    var ax = x0 + x * size - 0.04f;
                    var az = z0 + z * size - 0.04f;
                    var bx = x0 + (x + 1) * size + 0.04f;
                    var bz = z0 + (z + 1) * size + 0.04f;
                    var v = vertices.Count;
                    vertices.Add(new Vector3(ax, h + 0.004f, az));
                    vertices.Add(new Vector3(ax, h + 0.004f, bz));
                    vertices.Add(new Vector3(bx, h + 0.004f, bz));
                    vertices.Add(new Vector3(bx, h + 0.004f, az));
                    // a different corner of the texture for each cell, so no two patches are alike
                    var shift = new Vector2(ParchmentSheet.Hash(x, z, 51) < 0.5f ? 0f : 0.5f, ParchmentSheet.Hash(x, z, 53) < 0.5f ? 0f : 0.5f);
                    uvs.Add(shift);
                    uvs.Add(shift + new Vector2(0f, 0.5f));
                    uvs.Add(shift + new Vector2(0.5f, 0.5f));
                    uvs.Add(shift + new Vector2(0.5f, 0f));
                    hatch.AddRange(new[] { v, v + 1, v + 2, v, v + 2, v + 3 });
                }
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            var parts = new List<List<int>>();
            var materials = new List<Material>();
            void Add(List<int> part, Material material)
            {
                if (part.Count == 0) return;
                parts.Add(part);
                materials.Add(material);
            }
            Add(dots, Mat(ref _dots, Dots, null));
            Add(hatch, Mat(ref _hatch, Hatch, PaintHatching()));
            Add(light, valid ? Mat(ref _blueLight, InkBlueLight, null) : Mat(ref _redLight, InkRedLight, null));
            Add(strong, valid ? Mat(ref _blue, InkBlue, null) : Mat(ref _red, InkRed, null));
            mesh.subMeshCount = parts.Count;
            for (var s = 0; s < parts.Count; s++) mesh.SetTriangles(parts[s], s);
            mesh.RecalculateBounds();
            renderer.sharedMaterials = materials.ToArray();
        }

        private static Material Mat(ref Material cached, Color color, Texture2D texture)
        {
            if (cached != null) return cached;
            return cached = ParchmentSheet.Make(texture != null ? texture : Texture2D.whiteTexture, color, true);
        }

        private static Texture2D _hatching;

        /// <summary>
        /// Two cells' worth of hatching (the quads take a half-size window of it): slanted pen lines
        /// of uneven weight, each starting and stopping a little short of the edge at its own place,
        /// so the patch has the ragged rim of hand work.
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
                    var along = (u - v);                                  // where on that line
                    var wobble = 0.08f * (ParchmentSheet.Smooth(along * 12f, index * 7 + 1) - 0.5f);
                    var weight = 0.13f + 0.08f * ParchmentSheet.Hash(index, 0, 61);
                    var stroke = Mathf.Clamp01((weight - Mathf.Abs(offset + wobble)) * 40f);
                    // the ragged rim: each cell-sized window fades out near its border at a noisy depth
                    var cu = Mathf.Repeat(u * 2f, 1f);
                    var cv = Mathf.Repeat(v * 2f, 1f);
                    var edge = Mathf.Min(Mathf.Min(cu, 1f - cu), Mathf.Min(cv, 1f - cv));
                    var ragged = 0.03f + 0.06f * ParchmentSheet.Hash(index, Mathf.FloorToInt(along * 6f), 67);
                    var keep = Mathf.Clamp01((edge - ragged) * 60f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, stroke * keep));
                }
            }
            texture.Apply(true);
            return _hatching = texture;
        }
    }
}
