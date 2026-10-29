using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Maps.Archipelago
{
    /// <summary>
    /// One island in its own frame (the user approved these kinds on 2026-10-29): a base shape --
    /// round, long and bent, a crescent, an atoll with one or two channels, an islet -- whose coast is
    /// bent by three octaves of noise, so no two islands of a kind share a shore. Height follows the
    /// kind (flat meadow, central hill, rocky ridges, a spine along a long island); sandy beaches go
    /// on 1-3 arcs of gentle coast (always at least one: ships land only on beaches), cliffs where
    /// high ground meets the sea. The start, the orcs' and the bot's islands also get the spot for
    /// their zone. The frame is 40 % bigger than the island, so no cape runs into its edge.
    /// </summary>
    public static class IslandShape
    {
        public static Island Make(ArchipelagoGenerator.Kind kind, int scale, int seed)
        {
            var rng = new SeedRandom(seed);
            var n = Mathf.RoundToInt(scale * 1.4f);
            var c = n / 2f;
            var angle0 = rng.Next01() * Mathf.PI * 2f;
            var w1 = Noise.Fbm(n, rng, scale / 3);
            var w2 = Noise.Fbm(n, rng, scale / 7);
            var w3 = Noise.Fbm(n, rng, Mathf.Max(4, scale / 18));
            float[] thick = null;
            if (kind == ArchipelagoGenerator.Kind.Atoll) thick = Noise.Fbm(n, rng, scale / 4);

            float squash = 1f, radius = 0f;
            var channels = new List<float>();
            switch (kind)
            {
                case ArchipelagoGenerator.Kind.Start: radius = 0.33f; break;
                case ArchipelagoGenerator.Kind.Flat: radius = 0.33f; break;
                case ArchipelagoGenerator.Kind.Hilly: radius = 0.34f; break;
                case ArchipelagoGenerator.Kind.Rocky: radius = 0.32f; break;
                case ArchipelagoGenerator.Kind.Orc: radius = 0.38f; break;
                case ArchipelagoGenerator.Kind.Bot: radius = 0.33f; break;
                case ArchipelagoGenerator.Kind.Atoll:
                    var count = rng.RangeInt(1, 3);
                    for (var i = 0; i < count; i++) channels.Add(rng.Next01() * Mathf.PI * 2f);
                    break;
            }
            if (radius > 0f) squash = 1f + rng.Next01() * 0.35f;
            radius *= scale;

            var land = new bool[n * n];
            var inland = new float[n * n];
            var ca = Mathf.Cos(angle0);
            var sa = Mathf.Sin(angle0);
            for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
            {
                var i = y * n + x;
                float dx = x - c, dy = y - c;
                float u = dx * ca + dy * sa, v = -dx * sa + dy * ca;
                float d;
                switch (kind)
                {
                    case ArchipelagoGenerator.Kind.Long:
                        var bend = (u / scale) * (u / scale) * scale * 0.5f;
                        d = Mathf.Sqrt(Sq(u / (0.46f * scale)) + Sq((v - bend) / (0.14f * scale)));
                        break;
                    case ArchipelagoGenerator.Kind.Crescent:
                        var ring = Mathf.Abs(Mathf.Sqrt(u * u + v * v) - 0.32f * scale) / (0.11f * scale);
                        var bite = Mathf.Max(0f, u / (0.4f * scale) + 0.35f);
                        d = ring + bite * 1.4f;
                        break;
                    case ArchipelagoGenerator.Kind.Atoll:
                        var t = 0.06f + thick[i] * 0.06f;
                        d = Mathf.Abs(Mathf.Sqrt(u * u + v * v) - 0.34f * scale) / (t * scale);
                        var ang = Mathf.Atan2(v, u);
                        foreach (var ch in channels)
                        {
                            var off = Mathf.Repeat(ang - ch + Mathf.PI, Mathf.PI * 2f) - Mathf.PI;
                            d += Mathf.Exp(-Sq(off / 0.12f)) * 2f;
                        }
                        break;
                    case ArchipelagoGenerator.Kind.Islet:
                        d = Mathf.Sqrt(u * u + v * v) / (0.16f * scale);
                        break;
                    default:
                        d = Mathf.Sqrt(Sq(u / squash) + Sq(v * squash * 0.9f)) / radius;
                        break;
                }
                var shore = d + (w1[i] - 0.5f) * 0.75f + (w2[i] - 0.5f) * 0.45f + (w3[i] - 0.5f) * 0.3f;
                land[i] = shore < 1f;
                inland[i] = Mathf.Clamp01(1f - shore);
            }
            land = Biggest(land, n);

            var detail = Noise.Fbm(n, rng, scale / 6);
            float[] ridges = null;
            if (kind == ArchipelagoGenerator.Kind.Rocky || kind == ArchipelagoGenerator.Kind.Orc)
            {
                ridges = Noise.Fbm(n, rng, scale / 4);
            }
            var height = new float[n * n];
            for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
            {
                var i = y * n + x;
                if (!land[i]) continue;
                var inl = inland[i];
                float h;
                switch (kind)
                {
                    case ArchipelagoGenerator.Kind.Hilly:
                        h = Mathf.Pow(inl, 0.8f) * 0.75f + detail[i] * 0.2f;
                        break;
                    case ArchipelagoGenerator.Kind.Rocky:
                    case ArchipelagoGenerator.Kind.Orc:
                        var r = 1f - Mathf.Abs(ridges[i] - 0.5f) * 2f;
                        h = Mathf.Pow(inl, 0.6f) * 0.55f + r * r * r * inl * 0.6f;
                        break;
                    case ArchipelagoGenerator.Kind.Long:
                        float dx = x - c, dy = y - c;
                        float u = dx * ca + dy * sa, v = -dx * sa + dy * ca;
                        var bend = (u / scale) * (u / scale) * scale * 0.5f;
                        var spine = Mathf.Exp(-Sq((v - bend) / (0.06f * scale)));
                        h = spine * inl * 1.1f + detail[i] * 0.15f;
                        break;
                    case ArchipelagoGenerator.Kind.Crescent:
                        h = inl * 0.5f + detail[i] * 0.2f;
                        break;
                    default:
                        h = inl * 0.25f + detail[i] * 0.08f;
                        break;
                }
                height[i] = Mathf.Clamp01(h);
            }

            var toWater = DistanceToWater(land, n);
            var beach = Beaches(kind, land, height, toWater, n, rng);
            var cliff = Cliffs(land, height, beach, n);

            var island = new Island
            {
                Kind = kind,
                Frame = n,
                Land = land,
                Height = height,
                Beach = beach,
                Cliff = cliff,
                LandCentre = Centroid(land, n),
            };

            var half = ArchipelagoGenerator.ZoneHalf(kind);
            if (half != Vector2Int.zero) island.Zone = FindZone(island, toWater, half, kind == ArchipelagoGenerator.Kind.Orc, rng);
            return island;
        }

        /// <summary>
        /// A library island turned `turns` quarters: its cells, start zone and spots in the frame's
        /// cells, turned the way the game will turn its meshes (Quaternion.Euler(0, 90 * turns, 0),
        /// which takes a point (x, z) to (z, -x) per quarter).
        /// </summary>
        public static Island FromAsset(IslandAsset asset, int turns)
        {
            var n = asset.Frame;
            var half = n / 2f;
            var island = new Island
            {
                Kind = asset.Kind,
                Frame = n,
                Land = new bool[n * n],
                Height = new float[n * n],
                Beach = new bool[n * n],
                Cliff = new bool[n * n],
                Forest = new bool[n * n],
                Stony = new bool[n * n],
                Shore = new bool[n * n],
                Asset = asset,
                Turns = turns,
            };
            for (var j = 0; j < n; j++)
            for (var i = 0; i < n; i++)
            {
                var flags = asset.Cell(i, j);
                if (flags == 0) continue;
                var p = Turn(new Vector2(i - half + 0.5f, j - half + 0.5f), turns);
                var di = Mathf.FloorToInt(p.x + half);
                var dj = Mathf.FloorToInt(p.y + half);
                if (di < 0 || dj < 0 || di >= n || dj >= n) continue;
                var k = dj * n + di;
                island.Land[k] = (flags & IslandAsset.Land) != 0;
                island.Height[k] = (flags & IslandAsset.NoBuild) != 0 ? 1f : 0f;
                island.Forest[k] = (flags & IslandAsset.Forest) != 0;
                island.Stony[k] = (flags & IslandAsset.Stony) != 0;
                island.Shore[k] = (flags & IslandAsset.Shore) != 0;
            }
            // a beach for every island is the library's promise; the drawn shapes' flags are not needed
            island.LandCentre = Centroid(island.Land, n);
            Vector2Int Cell(Vector2 local)
            {
                var p = Turn(local, turns);
                return new Vector2Int(Mathf.FloorToInt(p.x + half), Mathf.FloorToInt(p.y + half));
            }
            if (asset.Kind == ArchipelagoGenerator.Kind.Start && asset.StartZone.Length > 0)
            {
                var centre = Vector2.zero;
                island.StartZone = new Vector2[asset.StartZone.Length];
                for (var k = 0; k < asset.StartZone.Length; k++)
                {
                    island.StartZone[k] = Turn(asset.StartZone[k], turns) + new Vector2(half, half);
                    centre += asset.StartZone[k];
                }
                island.Zone = Cell(centre / asset.StartZone.Length);
            }
            if (asset.HasMainBase) island.Zone = Cell(asset.MainBase);
            if (asset.HasBot) island.Zone = Cell(asset.Bot);
            return island;
        }

        public static Vector2 Turn(Vector2 p, int turns)
        {
            for (var t = 0; t < (turns & 3); t++) p = new Vector2(p.y, -p.x);
            return p;
        }

        private static float Sq(float v) => v * v;

        public static bool IsCoast(bool[] land, int n, int x, int y)
        {
            if (x == 0 || y == 0 || x == n - 1 || y == n - 1) return true;
            return !land[y * n + x - 1] || !land[y * n + x + 1] || !land[(y - 1) * n + x] || !land[(y + 1) * n + x];
        }

        /// <summary>The largest 4-connected piece of land: the fringe can cut specks off, and a speck is not an island.</summary>
        private static bool[] Biggest(bool[] land, int n)
        {
            var label = new int[n * n];
            int best = 0, bestCount = 0, current = 0;
            var stack = new Stack<int>();
            for (var start = 0; start < land.Length; start++)
            {
                if (!land[start] || label[start] != 0) continue;
                current++;
                var count = 0;
                label[start] = current;
                stack.Push(start);
                while (stack.Count > 0)
                {
                    var i = stack.Pop();
                    count++;
                    int x = i % n, y = i / n;
                    if (x > 0) Visit(i - 1);
                    if (x < n - 1) Visit(i + 1);
                    if (y > 0) Visit(i - n);
                    if (y < n - 1) Visit(i + n);
                }
                if (count > bestCount)
                {
                    best = current;
                    bestCount = count;
                }
            }
            var result = new bool[n * n];
            for (var i = 0; i < result.Length; i++) result[i] = label[i] == best && best != 0;
            return result;

            void Visit(int j)
            {
                if (!land[j] || label[j] != 0) return;
                label[j] = current;
                stack.Push(j);
            }
        }

        /// <summary>Metres (4-connected steps) from each land cell to the nearest water.</summary>
        private static int[] DistanceToWater(bool[] land, int n)
        {
            var dist = new int[n * n];
            var queue = new Queue<int>();
            for (var i = 0; i < dist.Length; i++)
            {
                if (land[i]) dist[i] = int.MaxValue;
                else
                {
                    dist[i] = 0;
                    queue.Enqueue(i);
                }
            }
            while (queue.Count > 0)
            {
                var i = queue.Dequeue();
                int x = i % n, y = i / n;
                var next = dist[i] + 1;
                if (x > 0 && dist[i - 1] > next) { dist[i - 1] = next; queue.Enqueue(i - 1); }
                if (x < n - 1 && dist[i + 1] > next) { dist[i + 1] = next; queue.Enqueue(i + 1); }
                if (y > 0 && dist[i - n] > next) { dist[i - n] = next; queue.Enqueue(i - n); }
                if (y < n - 1 && dist[i + n] > next) { dist[i + n] = next; queue.Enqueue(i + n); }
            }
            return dist;
        }

        /// <summary>Sand on 1-3 arcs of gentle coast, 3-6 m deep; at least one, under a cliff if it must.</summary>
        private static bool[] Beaches(ArchipelagoGenerator.Kind kind, bool[] land, float[] h, int[] toWater, int n, SeedRandom rng)
        {
            var count = kind switch
            {
                ArchipelagoGenerator.Kind.Rocky => 1,
                ArchipelagoGenerator.Kind.Orc => 2,
                ArchipelagoGenerator.Kind.Islet => 1,
                ArchipelagoGenerator.Kind.Atoll => 2,
                _ => rng.RangeInt(2, 4),
            };
            var c = n / 2f;
            var beach = new bool[n * n];
            for (var pass = 0; pass < 2; pass++)
            {
                for (var b = 0; b < count; b++)
                {
                    var centre = rng.Next01() * Mathf.PI * 2f;
                    var width = 0.35f + rng.Next01() * 0.45f;
                    var depth = 3 + rng.RangeInt(0, 4);
                    for (var y = 0; y < n; y++)
                    for (var x = 0; x < n; x++)
                    {
                        var i = y * n + x;
                        if (!land[i] || toWater[i] != 1) continue;
                        // the second pass, for an island whose arcs all fell under cliffs, ignores the height
                        if (pass == 0 && h[i] > 0.5f) continue;
                        var ang = Mathf.Atan2(y - c, x - c);
                        var off = Mathf.Abs(Mathf.Repeat(ang - centre + Mathf.PI, Mathf.PI * 2f) - Mathf.PI);
                        if (off > width / 2f) continue;
                        for (var yy = Mathf.Max(0, y - depth); yy <= Mathf.Min(n - 1, y + depth); yy++)
                        for (var xx = Mathf.Max(0, x - depth); xx <= Mathf.Min(n - 1, x + depth); xx++)
                        {
                            var j = yy * n + xx;
                            if (land[j] && toWater[j] <= 6 && (pass == 1 || h[j] < ArchipelagoGenerator.ReliefFrom)) beach[j] = true;
                        }
                    }
                }
                var any = false;
                for (var i = 0; i < beach.Length && !any; i++) any = beach[i];
                if (any) break;
                count = 1;
            }
            // a beach is plain: the land under it is brought down to the sea
            for (var i = 0; i < beach.Length; i++)
            {
                if (beach[i] && h[i] >= ArchipelagoGenerator.ReliefFrom) h[i] = ArchipelagoGenerator.ReliefFrom - 0.1f;
            }
            return beach;
        }

        /// <summary>The coast that is not beach where high ground comes down to the sea, two cells deep.</summary>
        private static bool[] Cliffs(bool[] land, float[] h, bool[] beach, int n)
        {
            var high = new bool[n * n];
            for (var i = 0; i < high.Length; i++) high[i] = h[i] > 0.28f;
            high = Grow(high, n, 9);
            var cliff = new bool[n * n];
            for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
            {
                var i = y * n + x;
                if (land[i] && !beach[i] && high[i] && IsCoast(land, n, x, y)) cliff[i] = true;
            }
            var thick = Grow(cliff, n, 1);
            for (var i = 0; i < thick.Length; i++) thick[i] = thick[i] && land[i] && !beach[i];
            return thick;
        }

        private static bool[] Grow(bool[] mask, int n, int steps)
        {
            var current = (bool[])mask.Clone();
            for (var s = 0; s < steps; s++)
            {
                var next = (bool[])current.Clone();
                for (var y = 0; y < n; y++)
                for (var x = 0; x < n; x++)
                {
                    if (!current[y * n + x]) continue;
                    if (x > 0) next[y * n + x - 1] = true;
                    if (x < n - 1) next[y * n + x + 1] = true;
                    if (y > 0) next[(y - 1) * n + x] = true;
                    if (y < n - 1) next[(y + 1) * n + x] = true;
                }
                current = next;
            }
            return current;
        }

        private static Vector2 Centroid(bool[] land, int n)
        {
            double sx = 0, sy = 0;
            var count = 0;
            for (var i = 0; i < land.Length; i++)
            {
                if (!land[i]) continue;
                sx += i % n;
                sy += i / n;
                count++;
            }
            return count > 0 ? new Vector2((float)(sx / count), (float)(sy / count)) : new Vector2(n / 2f, n / 2f);
        }

        /// <summary>
        /// The spot for a named island's zone: a rectangle of land with two metres to spare, as flat
        /// as can be found (relief under it is flattened later, so little of it is better), the orcs'
        /// base near the coast where their shipyard will stand. Shrinks the rectangle step by step on
        /// a narrow island rather than give none.
        /// </summary>
        private static Vector2Int? FindZone(Island island, int[] toWater, Vector2Int half, bool nearCoast, SeedRandom rng)
        {
            var n = island.Frame;
            for (var shrink = 0; shrink < 6; shrink++)
            {
                var hx = Mathf.Max(6, half.x - shrink * 3);
                var hy = Mathf.Max(6, half.y - shrink * 3);
                Vector2Int? best = null;
                var bestScore = float.MinValue;
                for (var t = 0; t < 500; t++)
                {
                    var x = rng.RangeInt(hx + 2, n - hx - 2);
                    var y = rng.RangeInt(hy + 2, n - hy - 2);
                    if (!island.Land[y * n + x]) continue;
                    var ok = true;
                    float relief = 0f, sum = 0f;
                    var count = 0;
                    for (var yy = y - hy - 2; yy <= y + hy + 2 && ok; yy++)
                    for (var xx = x - hx - 2; xx <= x + hx + 2; xx++)
                    {
                        var i = yy * n + xx;
                        if (!island.Land[i] || island.Beach[i]) { ok = false; break; }
                        if (island.Height[i] >= ArchipelagoGenerator.ReliefFrom) relief++;
                        sum += island.Height[i];
                        count++;
                    }
                    if (!ok) continue;
                    var score = -relief * 0.05f - sum / count;
                    if (nearCoast) score -= toWater[y * n + x] * 0.02f;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = new Vector2Int(x, y);
                    }
                }
                if (best.HasValue) return best;
            }
            return null;
        }
    }
}
