using CityBuilder.Maps.Archipelago;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// A generated map drawn as a painted chart, in the loading screen's look (the user, 2026-11-10:
    /// «аналогичную как на загрузочном экране»; Tools/loading/loading_maps.py, coloured): grey-blue
    /// sea in horizontal hatching with a pale shallow halo and a dotted line round it, land washed
    /// from ochre at the coast to green inland, an ink coastline with a thin echo offshore, ink peaks
    /// on the relief, a compass rose, paper stains and a darker worn edge. No markers. One pixel per
    /// metre cell (the map a 500 px square), into a reused texture; once per map code, never per frame.
    /// The canvas may be WIDER than the map (the user, 2026-11-17: «сделай карту архипелага прямоугольной, по краям
    /// просто добавь море»): the map sits in the middle, open sea (hatched, no land) on both sides.
    /// </summary>
    public static class InkMapPainter
    {
        // the palette, as loading_maps.py's coloured map
        private static readonly Color SeaDeep = new Color32(164, 168, 150, 255);
        private static readonly Color SeaShallow = new Color32(192, 198, 170, 255);
        private static readonly Color LandCoast = new Color32(222, 178, 88, 255);
        private static readonly Color LandInland = new Color32(156, 160, 74, 255);
        private static readonly Color LandRim = new Color32(196, 140, 64, 255);
        private static readonly Color Ink = new Color32(66, 40, 20, 255);
        private static readonly Color Worn = new Color32(110, 74, 38, 255);
        private static readonly Color RoseMain = new Color32(120, 80, 40, 255);
        private static readonly Color RoseMinor = new Color32(170, 130, 80, 255);

        // in cells; the loading map's strokes scaled to islands some 50 cells across
        private const float HaloFrom = 6f, HaloWidth = 6f;     // the shallow fades out over these
        private const float HatchClear = 12f;                  // no hatching this near the coast
        private const float DottedAt = 12f;                    // the shallow's dotted outline
        private const float EchoAt = 3f;                       // the thin line offshore
        private const float InlandOver = 26f;                  // ochre -> green over this far inland
        private const float RimWidth = 3.5f;
        private const int HatchStep = 4;
        private const int PeakStep = 12;
        private const float WornWidth = 26f;
        private const float WornStrength = 0.75f;

        /// <summary>The corner the compass rose sits in, as a fraction of the chart (x east, y north).</summary>
        public static readonly Vector2 RoseAt = new Vector2(0.1f, 0.9f);
        public const float RoseRadius = 0.06f;

        private static int[] _toLand, _toSea;
        private static Color32[] _pixels;
        private static float[] _ink;
        private static bool[] _land, _relief;
        // the smooth noises (stains, the land's wash) sampled every CoarseStep cells and interpolated
        private const int CoarseStep = 4;
        private static float[] _stains, _wash;

        public static Texture2D NewTexture(int size) => NewTexture(size, size);

        public static Texture2D NewTexture(int width, int height)
        {
            return new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "ChartMap",
            };
        }

        /// <summary>The canvas width for a map drawn `aspect` (width / height) wide; the height is the map's size.</summary>
        public static int WidthFor(int size, float aspect) => Mathf.Max(size, Mathf.RoundToInt(size * aspect));

        /// <summary>Paints the map into the texture and uploads it: the map's size high, `aspect` x that wide (the map in
        /// the middle, open sea on both sides; 1 = the square map alone).</summary>
        public static void Paint(ArchipelagoMap map, Texture2D texture, float aspect = 1f)
        {
            var n = map.Size;
            var h = n;
            var w = WidthFor(n, aspect);
            var pad = (w - n) / 2;
            if (texture.width != w || texture.height != h) texture.Reinitialize(w, h);
            if (_pixels == null || _pixels.Length != w * h)
            {
                _pixels = new Color32[w * h];
                _toLand = new int[w * h];
                _toSea = new int[w * h];
                _ink = new float[w * h];
                _land = new bool[w * h];
                _relief = new bool[w * h];
            }
            var px = _pixels;
            var ink = _ink;
            var land = _land;
            System.Array.Clear(ink, 0, ink.Length);
            System.Array.Clear(px, 0, px.Length);
            System.Array.Clear(land, 0, land.Length);
            System.Array.Clear(_relief, 0, _relief.Length);
            for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
            {
                var j = y * n + x;
                land[y * w + x + pad] = map.Land[j];
                _relief[y * w + x + pad] = map.Land[j] && map.IsRelief(j);
            }
            Distances(land, w, h, true, _toLand);
            Distances(land, w, h, false, _toSea);
            var salt = (uint)map.Seed * 2654435761u;
            var cw = w / CoarseStep + 2;
            var ch = h / CoarseStep + 2;
            if (_stains == null || _stains.Length != cw * ch)
            {
                _stains = new float[cw * ch];
                _wash = new float[cw * ch];
            }
            for (var cy = 0; cy < ch; cy++)
            for (var cx = 0; cx < cw; cx++)
            {
                _stains[cy * cw + cx] = Fbm(cx * CoarseStep, cy * CoarseStep, 110f, salt + 5u, 3);
                _wash[cy * cw + cx] = Fbm(cx * CoarseStep, cy * CoarseStep, 40f, salt + 11u, 3);
            }

            // ---- line art first: hatching, peaks, the rose (ink coverage per pixel; the rose marks its own pixels)
            var rng = new SeedRandom(map.Seed ^ 0x51A7);
            for (var y = 2; y < h; y += HatchStep)
            {
                var x = 0;
                while (x < w)
                {
                    var run = rng.RangeInt(12, 60);
                    var gap = rng.RangeInt(3, 16);
                    var yy = Mathf.Clamp(y + rng.RangeInt(-1, 2), 0, h - 1);
                    for (var k = x; k < Mathf.Min(w, x + run); k++)
                    {
                        var i = yy * w + k;
                        if (land[i] || _toLand[i] / 3f <= HatchClear) continue;
                        if (Fbm(k, yy, 16f, salt, 2) < 0.38f) continue;
                        ink[i] = Mathf.Max(ink[i], 0.42f);
                    }
                    x += run + gap;
                }
            }
            for (var gy = PeakStep / 2; gy < h; gy += PeakStep)
            for (var gx = PeakStep / 2; gx < n; gx += PeakStep)
            {
                int x = gx + pad + rng.RangeInt(-3, 4), y = gy + rng.RangeInt(-3, 4);
                if (x < 10 || y < 10 || x >= w - 10 || y >= h - 14) continue;
                var i = y * w + x;
                if (!land[i] || _toSea[i] / 3f < 8f) continue;
                var ridge = 1f - Mathf.Abs(2f * Fbm(x - pad, y, 60f, salt + 77u, 3) - 1f);
                if (_relief[i] || ridge > 0.82f) Peak(ink, w, h, x, y, rng);
            }
            Rose(px, ink, w, h);

            // ---- the colours, then the ink over them
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = y * w + x;
                Color c;
                var line = ink[i];
                if (px[i].a == 255)
                {
                    c = px[i];                                         // the rose's own colours
                }
                else if (land[i])
                {
                    var inland = _toSea[i] / 3f;
                    var mix = Mathf.Clamp01(inland / InlandOver * 0.9f + (Coarse(_wash, cw, x, y) - 0.5f) * 0.9f);
                    c = Color.Lerp(LandCoast, LandInland, mix);
                    c = Color.Lerp(c, LandRim, Mathf.Clamp01(1f - inland / RimWidth) * 0.6f);
                    line = Mathf.Max(line, Mathf.Clamp01(1.4f - inland));
                }
                else
                {
                    var off = _toLand[i] / 3f;
                    c = Color.Lerp(SeaDeep, SeaShallow, Mathf.Clamp01(1f - (off - HaloFrom) / HaloWidth));
                    line = Mathf.Max(line, Mathf.Clamp01(1.6f - Mathf.Abs(off - 1f)));
                    line = Mathf.Max(line, Mathf.Clamp01(1f - Mathf.Abs(off - EchoAt) * 1.4f) * 0.5f);
                    if (Mathf.Abs(off - DottedAt) < 0.5f && ((x + y) % 5) < 2) line = Mathf.Max(line, 0.55f);
                }
                // paper stains and grain
                var k = (0.9f + 0.14f * Coarse(_stains, cw, x, y)) *(0.97f + 0.06f * Hash(x, y, salt + 9u));
                c = new Color(c.r * k, c.g * k, c.b * k, 1f);
                if (line > 0f) c = Color.Lerp(c, Ink, line);
                // the worn, darker edge (its noise only where it can reach)
                var edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(w - 1 - x, h - 1 - y));
                if (edge < WornWidth + 22f)
                {
                    var wob = Fbm(x, y, 30f, salt + 3u, 2) * 22f;
                    var t = Mathf.Clamp01(1f - (edge - wob * 0.3f) / (WornWidth + wob));
                    c = Color.Lerp(c, Worn, t * Mathf.Sqrt(t) * WornStrength);
                    if (edge < 3f + wob * 0.15f) c *= 0.5f;
                }
                c.a = 1f;
                px[i] = c;
            }
            texture.SetPixels32(px);
            texture.Apply(false, false);
        }

        /// <summary>A two-pass chamfer (3/4) distance to the nearest cell whose land is <paramref name="toLand"/>.</summary>
        private static void Distances(bool[] land, int w, int h, bool toLand, int[] d)
        {
            const int Far = 1 << 20;
            for (var i = 0; i < d.Length; i++) d[i] = land[i] == toLand ? 0 : Far;
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = y * w + x;
                var v = d[i];
                if (v == 0) continue;
                if (x > 0) v = Mathf.Min(v, d[i - 1] + 3);
                if (y > 0)
                {
                    v = Mathf.Min(v, d[i - w] + 3);
                    if (x > 0) v = Mathf.Min(v, d[i - w - 1] + 4);
                    if (x < w - 1) v = Mathf.Min(v, d[i - w + 1] + 4);
                }
                d[i] = v;
            }
            for (var y = h - 1; y >= 0; y--)
            for (var x = w - 1; x >= 0; x--)
            {
                var i = y * w + x;
                var v = d[i];
                if (v == 0) continue;
                if (x < w - 1) v = Mathf.Min(v, d[i + 1] + 3);
                if (y < h - 1)
                {
                    v = Mathf.Min(v, d[i + w] + 3);
                    if (x < w - 1) v = Mathf.Min(v, d[i + w + 1] + 4);
                    if (x > 0) v = Mathf.Min(v, d[i + w - 1] + 4);
                }
                d[i] = v;
            }
        }

        /// <summary>A coarse noise grid (every CoarseStep cells) read back bilinearly at a cell.</summary>
        private static float Coarse(float[] grid, int cn, int x, int y)
        {
            int gx = x / CoarseStep, gy = y / CoarseStep;
            float tx = (x - gx * CoarseStep) / (float)CoarseStep, ty = (y - gy * CoarseStep) / (float)CoarseStep;
            var i = gy * cn + gx;
            var a = grid[i] + (grid[i + 1] - grid[i]) * tx;
            var b = grid[i + cn] + (grid[i + cn + 1] - grid[i + cn]) * tx;
            return a + (b - a) * ty;
        }

        private static float Hash(int x, int y, uint salt)
        {
            var h = (uint)x * 374761393u + (uint)y * 668265263u + salt;
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
        }

        /// <summary>Smooth value noise, `octaves` of it from a `cell`-sized lattice down, 0..1.</summary>
        private static float Fbm(int x, int y, float cell, uint salt, int octaves)
        {
            float sum = 0f, amp = 1f, total = 0f;
            for (var o = 0; o < octaves; o++)
            {
                var fx = x / cell;
                var fy = y / cell;
                int x0 = Mathf.FloorToInt(fx), y0 = Mathf.FloorToInt(fy);
                float tx = fx - x0, ty = fy - y0;
                tx = tx * tx * (3f - 2f * tx);
                ty = ty * ty * (3f - 2f * ty);
                var s = salt + (uint)o * 1013u;
                var a = Mathf.Lerp(Hash(x0, y0, s), Hash(x0 + 1, y0, s), tx);
                var b = Mathf.Lerp(Hash(x0, y0 + 1, s), Hash(x0 + 1, y0 + 1, s), tx);
                sum += amp * Mathf.Lerp(a, b, ty);
                total += amp;
                amp *= 0.5f;
                cell *= 0.5f;
            }
            return sum / total;
        }

        /// <summary>An anti-aliased ink stroke into the coverage buffer.</summary>
        private static void Stroke(float[] ink, int w, int h, float x0, float y0, float x1, float y1, float strength, float width)
        {
            var steps = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0)) * 2f);
            var r = Mathf.CeilToInt(width) + 1;
            for (var s = 0; s <= steps; s++)
            {
                var t = steps == 0 ? 0f : s / (float)steps;
                var x = Mathf.Lerp(x0, x1, t);
                var y = Mathf.Lerp(y0, y1, t);
                for (var dy = -r; dy <= r; dy++)
                for (var dx = -r; dx <= r; dx++)
                {
                    int qx = Mathf.RoundToInt(x) + dx, qy = Mathf.RoundToInt(y) + dy;
                    if (qx < 0 || qy < 0 || qx >= w || qy >= h) continue;
                    var dist = Mathf.Sqrt((qx - x) * (qx - x) + (qy - y) * (qy - y));
                    var a = Mathf.Clamp01(width - dist + 0.5f) * strength;
                    var i = qy * w + qx;
                    if (a > ink[i]) ink[i] = a;
                }
            }
        }

        /// <summary>An ink peak (y is north): two slopes, hatching on the east one, sometimes a smaller one beside.</summary>
        private static void Peak(float[] ink, int w, int h, int x, int y, SeedRandom rng)
        {
            var s = rng.Range(0.8f, 1.15f);
            float tall = 8f * s, half = 7f * s;
            var tipX = x + rng.Range(-1.5f, 1.5f);
            var tipY = y + tall;
            Stroke(ink, w, h, x - half, y, tipX, tipY, 0.95f, 0.6f);
            Stroke(ink, w, h, tipX, tipY, x + half, y, 0.95f, 0.6f);
            for (var k = 1; k < 4; k++)
            {
                var t = k / 4f;
                float hx = tipX + half * 0.9f * t, hy = tipY - tall * t;
                Stroke(ink, w, h, hx, hy, hx - 2.5f, hy - 1f, 0.6f, 0.35f);
            }
            if (rng.Next01() < 0.55f)
            {
                var x2 = x + half * 1.1f;
                Stroke(ink, w, h, x2 - half * 0.6f, y, x2, y + tall * 0.6f, 0.8f, 0.5f);
                Stroke(ink, w, h, x2, y + tall * 0.6f, x2 + half * 0.6f, y, 0.8f, 0.5f);
            }
        }

        /// <summary>The compass rose: rings in ink, four long points (north darkest) and four short ones as opaque pixels.</summary>
        private static void Rose(Color32[] px, float[] ink, int w, int h)
        {
            float cx = RoseAt.x * w, cy = RoseAt.y * h, r = RoseRadius * h;
            const int Segments = 120;
            for (var a = 0; a < Segments; a++)
            {
                var t = a / (float)Segments * Mathf.PI * 2f;
                var t2 = (a + 1) / (float)Segments * Mathf.PI * 2f;
                Ring(ink, w, h, cx, cy, r * 1.05f, t, t2, 0.75f, 0.7f);
                Ring(ink, w, h, cx, cy, r * 0.85f, t, t2, 0.55f, 0.4f);
                Ring(ink, w, h, cx, cy, r * 1.3f, t, t2, 0.35f, 0.4f);
            }
            for (var k = 7; k >= 0; k--)
            {
                var main = k % 2 == 0;
                var a = Mathf.PI / 2f - k * Mathf.PI / 4f;         // k 0 = north, clockwise
                var len = main ? r : r * 0.55f;
                var tip = new Vector2(cx + Mathf.Cos(a) * len, cy + Mathf.Sin(a) * len);
                var l = new Vector2(cx + Mathf.Cos(a + 1.9f) * r * 0.16f, cy + Mathf.Sin(a + 1.9f) * r * 0.16f);
                var rr = new Vector2(cx + Mathf.Cos(a - 1.9f) * r * 0.16f, cy + Mathf.Sin(a - 1.9f) * r * 0.16f);
                var centre = new Vector2(cx, cy);
                Triangle(px, w, h, tip, l, centre, k == 0 ? Ink : main ? RoseMain : RoseMinor);
                Triangle(px, w, h, tip, centre, rr, k == 0 ? Ink : RoseMinor);
            }
        }

        private static void Ring(float[] ink, int w, int h, float cx, float cy, float r, float t, float t2, float strength, float width)
        {
            Stroke(ink, w, h, cx + Mathf.Cos(t) * r, cy + Mathf.Sin(t) * r, cx + Mathf.Cos(t2) * r, cy + Mathf.Sin(t2) * r, strength, width);
        }

        private static void Triangle(Color32[] px, int w, int h, Vector2 a, Vector2 b, Vector2 c, Color fill)
        {
            int x0 = Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))), x1 = Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x)));
            int y0 = Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))), y1 = Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y)));
            var area = Edge(a, b, c);
            if (Mathf.Abs(area) < 1e-3f) return;
            for (var y = y0; y <= y1; y++)
            for (var x = x0; x <= x1; x++)
            {
                if (x < 0 || y < 0 || x >= w || y >= h) continue;
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float w0 = Edge(b, c, p) / area, w1 = Edge(c, a, p) / area, w2 = Edge(a, b, p) / area;
                if (w0 < 0f || w1 < 0f || w2 < 0f) continue;
                var rim = Mathf.Min(w0, Mathf.Min(w1, w2)) < 0.07f;
                px[y * w + x] = (Color32)(rim ? Ink : fill);
            }
        }

        private static float Edge(Vector2 a, Vector2 b, Vector2 p) => (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
    }
}
