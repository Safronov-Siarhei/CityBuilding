using CityBuilder.Maps.Archipelago;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// A generated map drawn as an ink chart on parchment (the user's pick, 2026-11-10, concept 2 for
    /// the sandbox board): paper sea with wave strokes and two ripple lines along the coasts, pale land
    /// with sand beaches, an inked coastline, little ink trees on the forests and peaks on the relief,
    /// a compass rose and a darker worn edge. One pixel per metre cell (a 500 px square), drawn into a
    /// reused texture; the markers (home, orcs, neighbour) are UI glyphs laid over it by the caller.
    /// Runs once per map code, not per frame; it allocates only its two distance buffers on first use.
    /// </summary>
    public static class InkMapPainter
    {
        // the chart's colours
        private static readonly Color32 Paper = new Color32(238, 224, 186, 255);
        private static readonly Color32 LandTone = new Color32(246, 236, 206, 255);
        private static readonly Color32 Sand = new Color32(228, 210, 162, 255);
        private static readonly Color32 Relief = new Color32(234, 220, 184, 255);
        private static readonly Color32 Ink = new Color32(74, 46, 22, 255);
        private static readonly Color32 RippleNear = new Color32(196, 178, 136, 255);
        private static readonly Color32 RippleFar = new Color32(212, 196, 156, 255);
        private static readonly Color32 Wave = new Color32(150, 128, 92, 255);
        private static readonly Color32 TreeFill = new Color32(170, 186, 120, 255);
        private static readonly Color32 TreeLine = new Color32(96, 112, 64, 255);
        private static readonly Color32 Peak = new Color32(110, 90, 66, 255);
        private static readonly Color32 RoseMain = new Color32(120, 80, 40, 255);
        private static readonly Color32 RoseMinor = new Color32(170, 130, 80, 255);
        private static readonly Color32 Worn = new Color32(150, 112, 66, 255);

        // the strokes, in cells (chamfer distances are x3 per straight step)
        private const int CoastInk = 4;                 // land within ~1.3 cells of the sea
        private const int RippleNearFrom = 12, RippleNearTo = 15;
        private const int RippleFarFrom = 27, RippleFarTo = 30;
        private const int WaveClear = 36;               // waves stay ~12 cells off the coast
        private const int WaveCount = 260;
        private const int TreeStep = 8;
        private const int PeakStep = 11;
        private const int WornWidth = 16;
        private const float WornStrength = 0.6f;
        private const float Grain = 5f;

        /// <summary>The corner the compass rose sits in, as a fraction of the chart (x east, y north).</summary>
        public static readonly Vector2 RoseAt = new Vector2(0.9f, 0.1f);
        public const float RoseRadius = 0.06f;

        private static int[] _toLand, _toSea;
        private static Color32[] _pixels;

        public static Texture2D NewTexture(int size)
        {
            return new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "InkMap",
            };
        }

        /// <summary>Paints the map into the texture (square, the map's size) and uploads it.</summary>
        public static void Paint(ArchipelagoMap map, Texture2D texture)
        {
            var n = map.Size;
            if (texture.width != n || texture.height != n) texture.Reinitialize(n, n);
            if (_pixels == null || _pixels.Length != n * n)
            {
                _pixels = new Color32[n * n];
                _toLand = new int[n * n];
                _toSea = new int[n * n];
            }
            var px = _pixels;
            Distances(map.Land, n, true, _toLand);
            Distances(map.Land, n, false, _toSea);

            for (var i = 0; i < px.Length; i++)
            {
                Color32 c;
                if (map.Land[i])
                {
                    if (_toSea[i] <= CoastInk) c = Ink;
                    else if (map.Beach[i]) c = Sand;
                    else if (map.IsRelief(i)) c = Relief;
                    else c = LandTone;
                }
                else
                {
                    var d = _toLand[i];
                    c = d >= RippleNearFrom && d <= RippleNearTo ? RippleNear
                        : d >= RippleFarFrom && d <= RippleFarTo ? RippleFar
                        : Paper;
                }
                px[i] = c;
            }

            var rng = new SeedRandom(map.Seed ^ 0x51A7);
            // waves on the open sea
            for (var k = 0; k < WaveCount; k++)
            {
                int x = rng.RangeInt(8, n - 40), y = rng.RangeInt(8, n - 8);
                if (_toLand[y * n + x] < WaveClear || _toLand[y * n + Mathf.Min(x + 26, n - 1)] < WaveClear) continue;
                WaveArc(px, n, x, y);
                WaveArc(px, n, x + 12, y);
            }
            // ink trees and peaks on a jittered grid
            for (var gy = TreeStep / 2; gy < n; gy += TreeStep)
            for (var gx = TreeStep / 2; gx < n; gx += TreeStep)
            {
                int x = gx + rng.RangeInt(-2, 3), y = gy + rng.RangeInt(-2, 3);
                if (x < 6 || y < 6 || x >= n - 6 || y >= n - 10) continue;
                var i = y * n + x;
                if (map.Forest[i] && _toSea[i] > 12) Tree(px, n, x, y);
            }
            for (var gy = PeakStep / 2; gy < n; gy += PeakStep)
            for (var gx = PeakStep / 2; gx < n; gx += PeakStep)
            {
                int x = gx + rng.RangeInt(-3, 4), y = gy + rng.RangeInt(-3, 4);
                if (x < 10 || y < 10 || x >= n - 10 || y >= n - 10 || rng.Next01() > 0.6f) continue;
                var i = y * n + x;
                if (map.IsRelief(i) && _toSea[i] > 12) PeakMark(px, n, x, y);
            }
            Rose(px, n);
            WornEdge(px, n, map.Seed);

            texture.SetPixels32(px);
            texture.Apply(false, false);
        }

        /// <summary>A two-pass chamfer (3/4) distance to the nearest cell whose land is <paramref name="toLand"/>.</summary>
        private static void Distances(bool[] land, int n, bool toLand, int[] d)
        {
            const int Far = 1 << 20;
            for (var i = 0; i < d.Length; i++) d[i] = land[i] == toLand ? 0 : Far;
            for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
            {
                var i = y * n + x;
                var v = d[i];
                if (v == 0) continue;
                if (x > 0) v = Mathf.Min(v, d[i - 1] + 3);
                if (y > 0)
                {
                    v = Mathf.Min(v, d[i - n] + 3);
                    if (x > 0) v = Mathf.Min(v, d[i - n - 1] + 4);
                    if (x < n - 1) v = Mathf.Min(v, d[i - n + 1] + 4);
                }
                d[i] = v;
            }
            for (var y = n - 1; y >= 0; y--)
            for (var x = n - 1; x >= 0; x--)
            {
                var i = y * n + x;
                var v = d[i];
                if (v == 0) continue;
                if (x < n - 1) v = Mathf.Min(v, d[i + 1] + 3);
                if (y < n - 1)
                {
                    v = Mathf.Min(v, d[i + n] + 3);
                    if (x < n - 1) v = Mathf.Min(v, d[i + n + 1] + 4);
                    if (x > 0) v = Mathf.Min(v, d[i + n - 1] + 4);
                }
                d[i] = v;
            }
        }

        private static void Put(Color32[] px, int n, int x, int y, Color32 c)
        {
            if (x >= 0 && y >= 0 && x < n && y < n) px[y * n + x] = c;
        }

        /// <summary>A small wave stroke, an arc bowed up (y is north), 14 x 4 cells.</summary>
        private static void WaveArc(Color32[] px, int n, int x, int y)
        {
            for (var t = 0; t <= 14; t++)
            {
                var u = t / 14f * 2f - 1f;
                var h = Mathf.RoundToInt((1f - u * u) * 4f);
                Put(px, n, x + t, y + h, Wave);
                Put(px, n, x + t, y + h + 1, Wave);
            }
        }

        /// <summary>A little ink tree: a filled triangle, tip north, outlined.</summary>
        private static void Tree(Color32[] px, int n, int x, int y)
        {
            const int Height = 10, Half = 5;
            for (var h = 0; h <= Height; h++)
            {
                var w = Mathf.RoundToInt(Half * (1f - h / (float)Height));
                for (var dx = -w; dx <= w; dx++)
                    Put(px, n, x + dx, y - 4 + h, dx == -w || dx == w || h == 0 ? TreeLine : TreeFill);
            }
        }

        /// <summary>A peak mark: an ink «^», two cells thick.</summary>
        private static void PeakMark(Color32[] px, int n, int x, int y)
        {
            for (var t = 0; t <= 8; t++)
            {
                var h = 12 - Mathf.RoundToInt(t * 1.5f);
                for (var w = 0; w < 2; w++)
                {
                    Put(px, n, x - t, y - 6 + h + w, Peak);
                    Put(px, n, x + t, y - 6 + h + w, Peak);
                }
            }
        }

        /// <summary>The compass rose: four long points (north darkest) and four short ones.</summary>
        private static void Rose(Color32[] px, int n)
        {
            float cx = RoseAt.x * n, cy = RoseAt.y * n, r = RoseRadius * n;
            for (var k = 7; k >= 0; k--)
            {
                var main = k % 2 == 0;
                var a = Mathf.PI / 2f - k * Mathf.PI / 4f;         // k 0 = north, clockwise
                var len = main ? r : r * 0.55f;
                var tip = new Vector2(cx + Mathf.Cos(a) * len, cy + Mathf.Sin(a) * len);
                var l = new Vector2(cx + Mathf.Cos(a + 1.9f) * r * 0.2f, cy + Mathf.Sin(a + 1.9f) * r * 0.2f);
                var rr = new Vector2(cx + Mathf.Cos(a - 1.9f) * r * 0.2f, cy + Mathf.Sin(a - 1.9f) * r * 0.2f);
                var centre = new Vector2(cx, cy);
                var fill = k == 0 ? Ink : main ? RoseMain : RoseMinor;
                Triangle(px, n, tip, l, centre, fill);
                Triangle(px, n, tip, centre, rr, k == 0 ? Ink : RoseMinor);
            }
        }

        private static void Triangle(Color32[] px, int n, Vector2 a, Vector2 b, Vector2 c, Color32 fill)
        {
            int x0 = Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))), x1 = Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x)));
            int y0 = Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))), y1 = Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y)));
            var area = Edge(a, b, c);
            if (Mathf.Abs(area) < 1e-3f) return;
            for (var y = y0; y <= y1; y++)
            for (var x = x0; x <= x1; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float w0 = Edge(b, c, p) / area, w1 = Edge(c, a, p) / area, w2 = Edge(a, b, p) / area;
                if (w0 < 0f || w1 < 0f || w2 < 0f) continue;
                // an ink line along the triangle's outer edges
                var rim = Mathf.Min(w0, Mathf.Min(w1, w2)) < 0.06f;
                Put(px, n, x, y, rim ? Ink : fill);
            }
        }

        private static float Edge(Vector2 a, Vector2 b, Vector2 p) => (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);

        /// <summary>A darker, uneven worn edge and a faint paper grain over everything.</summary>
        private static void WornEdge(Color32[] px, int n, int seed)
        {
            var salt = (uint)seed * 2654435761u;
            for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
            {
                var i = y * n + x;
                var edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(n - 1 - x, n - 1 - y));
                // the edge's width wanders with a slow wobble along the border
                var along = (x + y) * 0.07f;
                var width = WornWidth * (0.7f + 0.3f * Mathf.Sin(along + (salt & 255) * 0.1f));
                var c = px[i];
                if (edge < width)
                {
                    var t = 1f - edge / width;
                    var k = WornStrength * t * t;
                    c = Color32.Lerp(c, Worn, k);
                }
                var h = (uint)i * 747796405u + salt;
                h = (h >> 16) ^ h;
                var g = ((h & 255) / 255f - 0.5f) * Grain;
                px[i] = new Color32(Clamp(c.r + g), Clamp(c.g + g), Clamp(c.b + g), 255);
            }
        }

        private static byte Clamp(float v) => (byte)Mathf.Clamp(Mathf.RoundToInt(v), 0, 255);
    }
}
