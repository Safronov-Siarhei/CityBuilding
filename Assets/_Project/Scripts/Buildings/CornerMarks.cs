using System.Collections.Generic;
using CityBuilder.Grid;
using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// The ground under the placement drawing in PlacementLook.DrawingWithCorners, the way an
    /// architect marks a plot: an ink bracket at each corner, a dimension line with arrowheads and
    /// a tick per cell along the near and the left side, and under every cell in the way a pale
    /// red watercolour wash (the drawing above it turns red there too -- see PlacementPreview's
    /// mask). Indigo ink where the building can stand, red-brown where it cannot.
    /// </summary>
    public static class CornerMarks
    {
        private const float Height = 0.05f;
        private const float Line = 0.065f;          // a bracket's stroke
        private const float Thin = 0.035f;          // a dimension line's stroke
        private const float BracketGap = 0.12f;     // brackets sit this far outside the footprint
        private const float DimensionGap = 0.5f;    // dimension lines this far
        private const float Arrow = 0.22f;

        private static readonly Color InkBlue = new Color(0.16f, 0.27f, 0.55f, 0.95f);
        private static readonly Color InkRed = new Color(0.62f, 0.17f, 0.1f, 0.95f);
        private static readonly Color Wash = new Color(0.82f, 0.18f, 0.12f, 0.55f);

        private static Material _blue;
        private static Material _red;
        private static Material _wash;

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
            var ink = new List<int>();
            var wash = new List<int>();

            void Bar(Vector2 a, Vector2 b, float width)
            {
                var along = (b - a).normalized;
                var side = new Vector2(-along.y, along.x) * (width * 0.5f);
                // square ends: run half a stroke past each point so two bars meet without a notch
                a -= along * (width * 0.5f);
                b += along * (width * 0.5f);
                var i = vertices.Count;
                foreach (var p in new[] { a - side, b - side, b + side, a + side })
                {
                    vertices.Add(new Vector3(p.x, h + 0.006f, p.y));
                    uvs.Add(Vector2.zero);
                }
                ink.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }
            void Head(Vector2 tip, Vector2 pointing)
            {
                var back = tip - pointing * Arrow;
                var side = new Vector2(-pointing.y, pointing.x) * (Arrow * 0.38f);
                var i = vertices.Count;
                foreach (var p in new[] { tip, back + side, back - side })
                {
                    vertices.Add(new Vector3(p.x, h + 0.006f, p.y));
                    uvs.Add(Vector2.zero);
                }
                ink.AddRange(new[] { i, i + 1, i + 2 });
            }

            // the corner brackets
            var arm = Mathf.Min(0.7f, 0.3f * Mathf.Min(x1 - x0, z1 - z0) + 0.15f);
            foreach (var (cx, cz, sx, sz) in new[] { (x0, z0, 1f, 1f), (x1, z0, -1f, 1f), (x1, z1, -1f, -1f), (x0, z1, 1f, -1f) })
            {
                var c = new Vector2(cx - sx * BracketGap, cz - sz * BracketGap);
                Bar(c, c + new Vector2(sx * arm, 0f), Line);
                Bar(c, c + new Vector2(0f, sz * arm), Line);
            }

            // the dimension lines: the near side (south) and the left side (west)
            void Dimension(Vector2 from, Vector2 to, Vector2 outward, int cells)
            {
                var a = from + outward * DimensionGap;
                var b = to + outward * DimensionGap;
                var along = (b - a).normalized;
                Bar(a + along * Arrow, b - along * Arrow, Thin);
                Head(a, -along);
                Head(b, along);
                // extension lines from near the footprint out past the dimension line
                Bar(from + outward * (BracketGap + 0.1f), from + outward * (DimensionGap + 0.15f), Thin);
                Bar(to + outward * (BracketGap + 0.1f), to + outward * (DimensionGap + 0.15f), Thin);
                // a tick per cell, like a ruler
                for (var i = 1; i < cells; i++)
                {
                    var p = Vector2.Lerp(a, b, i / (float)cells);
                    Bar(p - outward * 0.08f, p + outward * 0.08f, Thin);
                }
            }
            Dimension(new Vector2(x0, z0), new Vector2(x1, z0), Vector2.down, footprint.x);
            Dimension(new Vector2(x0, z0), new Vector2(x0, z1), Vector2.left, footprint.y);

            // the wash: one soft blot per cell in the way, a little bigger than the cell, each turned its own way
            var k = 0;
            for (var z = 0; z < footprint.y; z++)
            {
                for (var x = 0; x < footprint.x; x++, k++)
                {
                    if (k >= blocked.Count || !blocked[k]) continue;
                    var centre = new Vector2(x0 + (x + 0.5f) * size, z0 + (z + 0.5f) * size);
                    var angle = ParchmentSheet.Hash(x, z, 29) * Mathf.PI * 2f;
                    var half = size * (0.78f + 0.12f * ParchmentSheet.Hash(x, z, 31));
                    var axisA = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * half;
                    var axisB = new Vector2(-axisA.y, axisA.x);
                    var i = vertices.Count;
                    foreach (var (p, uv) in new[]
                    {
                        (centre - axisA - axisB, new Vector2(0f, 0f)), (centre - axisA + axisB, new Vector2(0f, 1f)),
                        (centre + axisA + axisB, new Vector2(1f, 1f)), (centre + axisA - axisB, new Vector2(1f, 0f)),
                    })
                    {
                        vertices.Add(new Vector3(p.x, h + 0.002f, p.y));
                        uvs.Add(uv);
                    }
                    wash.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
                }
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            var parts = new List<List<int>>();
            var materials = new List<Material>();
            if (wash.Count > 0)
            {
                parts.Add(wash);
                materials.Add(WashMaterial());
            }
            parts.Add(ink);
            materials.Add(valid ? BlueMaterial() : RedMaterial());
            mesh.subMeshCount = parts.Count;
            for (var s = 0; s < parts.Count; s++) mesh.SetTriangles(parts[s], s);
            mesh.RecalculateBounds();
            renderer.sharedMaterials = materials.ToArray();
        }

        private static Material BlueMaterial() => _blue != null ? _blue : _blue = ParchmentSheet.Make(Texture2D.whiteTexture, InkBlue, true);
        private static Material RedMaterial() => _red != null ? _red : _red = ParchmentSheet.Make(Texture2D.whiteTexture, InkRed, true);

        private static Material WashMaterial()
        {
            if (_wash != null) return _wash;
            _wash = ParchmentSheet.Make(PaintWash(), Wash, true);
            _wash.renderQueue -= 1; // under the ink
            return _wash;
        }

        /// <summary>
        /// A watercolour wash: a soft, lobed pool, pale and mottled inside, a darker tide-line where
        /// the paint dried at its edge.
        /// </summary>
        private static Texture2D PaintWash()
        {
            const int n = 128;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < n; y++)
            {
                for (var x = 0; x < n; x++)
                {
                    var p = new Vector2((x + 0.5f) / n, (y + 0.5f) / n);
                    var d = p - new Vector2(0.5f, 0.5f);
                    var angle = Mathf.Atan2(d.y, d.x);
                    var radius = 0.38f + 0.04f * Mathf.Sin(angle * 2f + 0.7f) + 0.03f * Mathf.Sin(angle * 5f + 2.1f)
                                 + 0.05f * (ParchmentSheet.Fbm(Mathf.Cos(angle) * 1.5f + 5f, Mathf.Sin(angle) * 1.5f + 5f, 37) - 0.5f);
                    var inside = radius - d.magnitude;
                    var body = Mathf.Clamp01(inside * 18f);                          // a soft edge, not a cut
                    var tide = Mathf.Clamp01(1f - Mathf.Abs(inside - 0.02f) * 45f);  // the dried rim
                    var mottle = 0.55f + 0.3f * ParchmentSheet.Fbm(p.x * 7f, p.y * 7f, 43);
                    var alpha = Mathf.Clamp01(body * mottle + tide * 0.45f * body);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply(true);
            return texture;
        }
    }
}
