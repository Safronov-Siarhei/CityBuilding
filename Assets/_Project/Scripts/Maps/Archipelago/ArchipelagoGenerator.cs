using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Maps.Archipelago
{
    /// <summary>
    /// The sandbox's archipelago from a seed (the user, 2026-10-29) -- the C# of the prototype in
    /// Tools/archipelago (islands_preview.py: the island kinds; archipelago_preview.py: the layout).
    ///
    /// A 500 x 500 m sea with a stormy band round the edge. The player's start island stands near one
    /// side, the orcs' main island far across (its centre at least 55 % of the world away), the bot's
    /// island on a flank; then 5-9 medium islands of the ordinary kinds, then islets in the gaps, 12-20
    /// islands in all, never closer than a 15 m strait. Every island's coast is bent by noise (no two
    /// alike), every island has at least one beach, high ground over the plain is relief (impassable,
    /// unbuildable), forests and stony clearings are patches of noise on the plain.
    ///
    /// Deterministic: everything comes from <see cref="SeedRandom"/> on the seed, never UnityEngine.Random,
    /// so a seed is the same map on every device -- that is what makes a seed worth sharing.
    /// </summary>
    public static class ArchipelagoGenerator
    {
        /// <summary>Bumped whenever a seed would come out as a different map; it is part of the map's code.</summary>
        // 2: the islands come from the Blender library (2026-10-29)
        public const int Version = 2;

        public const int World = 500;
        public const int Storm = 24;
        private const int StraitMin = 15;
        private const int Calm = Storm + 6;

        /// <summary>Over this the land is relief: hills and mountains, out of the plain.</summary>
        public const float ReliefFrom = 0.45f;

        public enum Kind { Start, Orc, Bot, Flat, Hilly, Rocky, Long, Crescent, Atoll, Islet }

        private static readonly (Kind kind, int size, float weight)[] Medium =
        {
            (Kind.Flat, 150, 2f), (Kind.Hilly, 150, 2f), (Kind.Rocky, 150, 1.5f), (Kind.Long, 170, 1.5f),
            (Kind.Crescent, 150, 1f), (Kind.Atoll, 150, 0.6f),
        };

        /// <summary>The orcs' main base (OrcBaseLayout.Main: 38 x 32 cells) and a margin round it, flattened into a valley.</summary>
        private static readonly Vector2Int MainBaseHalf = new Vector2Int(23, 20);
        private const int StartZoneRadius = 18;

        public static ArchipelagoMap Generate(int seed)
        {
            var rng = new SeedRandom(seed);
            var map = new ArchipelagoMap(seed, World);
            var library = ArchipelagoLibrary.Load();
            var used = new HashSet<IslandAsset>();
            Island Make(Kind kind, int size, int shapeSeed)
            {
                return library != null ? FromLibrary(library, kind, rng, used) ?? IslandShape.Make(kind, size, shapeSeed)
                    : IslandShape.Make(kind, size, shapeSeed);
            }
            var claimed = new bool[World * World];
            var placed = new List<Island>();

            // the three named islands: start on one side, orcs across, the bot on a flank
            var side = rng.Next01() * Mathf.PI * 2f;
            var c = World / 2f;
            var flank = Mathf.PI / 2f * (rng.Next01() < 0.5f ? 1f : -1f);
            foreach (var (kind, size, turn) in new[] { (Kind.Start, 130, 0f), (Kind.Orc, 125, Mathf.PI), (Kind.Bot, 120, flank) })
            {
                var done = false;
                for (var attempt = 0; attempt < 25 && !done; attempt++)
                {
                    var spots = new List<Vector2>();
                    for (var i = 0; i < 60; i++)
                    {
                        var a = side + turn + rng.Normal() * 0.25f;
                        var d = rng.Range(90f, 200f);
                        spots.Add(new Vector2(c + Mathf.Cos(a) * d, c + Mathf.Sin(a) * d));
                    }
                    // the first that fits is taken, so the farthest out come first
                    spots.Sort((p, q) => Vector2.Distance(q, new Vector2(c, c)).CompareTo(Vector2.Distance(p, new Vector2(c, c))));
                    if (kind == Kind.Orc)
                    {
                        var start = placed[0].Centre;
                        spots.RemoveAll(p => Vector2.Distance(p, start) < 0.55f * World);
                    }
                    var island = Make(kind, size, seed * 1000 + attempt * 7 + placed.Count);
                    // a named island with no room for its zone is no use: try another
                    done = island.Zone.HasValue && TryPlace(island, spots, claimed, placed);
                }
                if (!done) throw new InvalidOperationException($"Archipelago {seed}: no room for the {kind} island");
            }

            // medium islands while there is sea for them, then islets in the gaps, 12-20 in all
            var total = rng.RangeInt(12, 21);
            var wantMedium = rng.RangeInt(5, 10);
            var tries = 0;
            float weightSum = 0f;
            foreach (var m in Medium) weightSum += m.weight;
            while (placed.Count < 3 + wantMedium && tries < 60)
            {
                tries++;
                var pick = rng.Next01() * weightSum;
                var choice = Medium[0];
                foreach (var m in Medium)
                {
                    choice = m;
                    pick -= m.weight;
                    if (pick <= 0f) break;
                }
                var size = Mathf.RoundToInt(choice.size * rng.Range(0.75f, 1f));
                TryPlace(Make(choice.kind, size, seed * 1000 + 500 + tries), RandomSpots(rng, 60), claimed, placed);
            }
            while (placed.Count < total && tries < 160)
            {
                tries++;
                var size = Mathf.RoundToInt(70 * rng.Range(0.8f, 1.3f));
                TryPlace(Make(Kind.Islet, size, seed * 1000 + 500 + tries), RandomSpots(rng, 40), claimed, placed);
            }

            foreach (var island in placed) Stamp(map, island);
            // a library island brings its own forest, stone and clear zones from Blender
            if (library == null) Dress(map, rng);
            map.Islands.AddRange(placed);
            return map;
        }

        /// <summary>
        /// A library island of the kind, turned a random quarter (0-3): one not yet on this map when
        /// there is one, so two islands of a map are rarely the same. Null when the library has none
        /// of the kind.
        /// </summary>
        private static Island FromLibrary(ArchipelagoLibrary library, Kind kind, SeedRandom rng, HashSet<IslandAsset> used)
        {
            var fresh = new List<IslandAsset>();
            var any = new List<IslandAsset>();
            foreach (var asset in library.Islands)
            {
                if (asset == null || asset.Kind != kind) continue;
                any.Add(asset);
                if (!used.Contains(asset)) fresh.Add(asset);
            }
            var pool = fresh.Count > 0 ? fresh : any;
            if (pool.Count == 0) return null;
            var chosen = pool[rng.RangeInt(0, pool.Count)];
            used.Add(chosen);
            return IslandShape.FromAsset(chosen, rng.RangeInt(0, 4));
        }

        private static List<Vector2> RandomSpots(SeedRandom rng, int count)
        {
            var spots = new List<Vector2>(count);
            for (var i = 0; i < count; i++) spots.Add(new Vector2(rng.Range(Calm, World - Calm), rng.Range(Calm, World - Calm)));
            return spots;
        }

        /// <summary>Puts the island at the first spot where its land stays in the calm sea and a strait's width off every other island's land.</summary>
        private static bool TryPlace(Island island, List<Vector2> spots, bool[] claimed, List<Island> placed)
        {
            foreach (var spot in spots)
            {
                var ox = Mathf.RoundToInt(spot.x - island.LandCentre.x);
                var oy = Mathf.RoundToInt(spot.y - island.LandCentre.y);
                if (!Fits(island, ox, oy, claimed)) continue;
                island.OffsetX = ox;
                island.OffsetY = oy;
                Claim(island, claimed);
                placed.Add(island);
                return true;
            }
            return false;
        }

        private static bool Fits(Island island, int ox, int oy, bool[] claimed)
        {
            var n = island.Frame;
            for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
            {
                if (!island.Land[y * n + x]) continue;
                int wx = x + ox, wy = y + oy;
                if (wx < Calm || wy < Calm || wx >= World - Calm || wy >= World - Calm) return false;
                if (claimed[wy * World + wx]) return false;
            }
            return true;
        }

        /// <summary>The island's land grown by a strait's width: what the next island may not touch.</summary>
        private static void Claim(Island island, bool[] claimed)
        {
            var n = island.Frame;
            var r2 = StraitMin * StraitMin;
            for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
            {
                // only the coast need spread: the inside of a claim is already inside it
                if (!island.Land[y * n + x] || !IslandShape.IsCoast(island.Land, n, x, y)) continue;
                for (var dy = -StraitMin; dy <= StraitMin; dy++)
                for (var dx = -StraitMin; dx <= StraitMin; dx++)
                {
                    if (dx * dx + dy * dy > r2) continue;
                    int wx = x + island.OffsetX + dx, wy = y + island.OffsetY + dy;
                    if (wx >= 0 && wy >= 0 && wx < World && wy < World) claimed[wy * World + wx] = true;
                }
            }
            for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
            {
                if (island.Land[y * n + x]) claimed[(y + island.OffsetY) * World + x + island.OffsetX] = true;
            }
        }

        private static void Stamp(ArchipelagoMap map, Island island)
        {
            var n = island.Frame;
            for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
            {
                var i = y * n + x;
                if (island.Shore != null && island.Shore[i])
                {
                    map.Shore[(y + island.OffsetY) * World + x + island.OffsetX] = true;
                }
                if (!island.Land[i]) continue;
                var w = (y + island.OffsetY) * World + x + island.OffsetX;
                map.Land[w] = true;
                map.Height[w] = island.Height[i];
                map.Beach[w] = island.Beach[i];
                map.Cliff[w] = island.Cliff[i];
                if (island.Forest != null)
                {
                    map.Forest[w] = island.Forest[i];
                    map.Stony[w] = island.Stony[i];
                }
            }
            if (island.Zone.HasValue)
            {
                var z = island.Zone.Value + new Vector2Int(island.OffsetX, island.OffsetY);
                if (island.Kind == Kind.Start && island.StartZone != null)
                {
                    map.StartZone = new Vector2[island.StartZone.Length];
                    for (var k = 0; k < island.StartZone.Length; k++) map.StartZone[k] = island.StartZone[k] + new Vector2(island.OffsetX, island.OffsetY);
                }
                switch (island.Kind)
                {
                    case Kind.Start: map.StartCell = z; break;
                    case Kind.Orc: map.OrcBaseCell = z; break;
                    case Kind.Bot: map.BotCell = z; break;
                }
            }
        }

        /// <summary>
        /// What grows on the plain: forest patches and stony clearings, kept off the beaches and the
        /// relief; the start zone and the orcs' valley kept open and flat.
        /// </summary>
        private static void Dress(ArchipelagoMap map, SeedRandom rng)
        {
            var forest = Noise.Fbm(World, rng, World / 18, 4);
            var stony = Noise.Fbm(World, rng, World / 30, 3);
            for (var i = 0; i < map.Land.Length; i++)
            {
                if (!map.Land[i] || map.Beach[i] || map.Height[i] >= ReliefFrom) continue;
                if (forest[i] > 0.56f) map.Forest[i] = true;
                else if (stony[i] > 0.7f) map.Stony[i] = true;
            }

            Clear(map, map.StartCell, new Vector2Int(StartZoneRadius + 6, StartZoneRadius + 6), flatten: true);
            Clear(map, map.OrcBaseCell, MainBaseHalf, flatten: true);
            Clear(map, map.BotCell, new Vector2Int(14, 14), flatten: true);
        }

        private static void Clear(ArchipelagoMap map, Vector2Int centre, Vector2Int half, bool flatten)
        {
            for (var y = centre.y - half.y; y <= centre.y + half.y; y++)
            for (var x = centre.x - half.x; x <= centre.x + half.x; x++)
            {
                if (x < 0 || y < 0 || x >= World || y >= World) continue;
                var i = y * World + x;
                if (!map.Land[i]) continue;
                map.Forest[i] = false;
                map.Stony[i] = false;
                if (flatten && map.Height[i] >= ReliefFrom - 0.05f) map.Height[i] = ReliefFrom - 0.1f;
            }
        }

        /// <summary>The zone each named island needs, as half extents: what IslandShape must find room for.</summary>
        internal static Vector2Int ZoneHalf(Kind kind) => kind switch
        {
            Kind.Start => new Vector2Int(StartZoneRadius + 4, StartZoneRadius + 4),
            Kind.Orc => MainBaseHalf,
            Kind.Bot => new Vector2Int(14, 14),
            _ => Vector2Int.zero,
        };

        public static int StartZoneRadiusMeters => StartZoneRadius;
    }

    /// <summary>One island in its own square frame before and after it is placed.</summary>
    public sealed class Island
    {
        public ArchipelagoGenerator.Kind Kind;
        public int Frame;
        public bool[] Land;
        public float[] Height;
        public bool[] Beach;
        public bool[] Cliff;
        public Vector2 LandCentre;
        public Vector2Int? Zone;
        public int OffsetX, OffsetY;

        // a library island: what it is, how far it is turned, its own forest, stone and shore water,
        // and its start zone (frame cells), all already turned
        public IslandAsset Asset;
        public int Turns;
        public bool[] Forest, Stony, Shore;
        public Vector2[] StartZone;

        public Vector2 Centre => LandCentre + new Vector2(OffsetX, OffsetY);
    }

    /// <summary>The generated world: per metre cell, row-major from the south-west corner.</summary>
    public sealed class ArchipelagoMap
    {
        public readonly int Seed;
        public readonly int Size;
        public readonly bool[] Land;
        public readonly float[] Height;
        public readonly bool[] Beach;
        public readonly bool[] Cliff;
        public readonly bool[] Forest;
        public readonly bool[] Stony;
        /// <summary>Water a dock may stand on, from a library island's own shore; empty on a drawn map.</summary>
        public readonly bool[] Shore;
        /// <summary>The start zone's corners in world cells, from a library start island; null on a drawn map.</summary>
        public Vector2[] StartZone;
        public Vector2Int StartCell;
        public Vector2Int OrcBaseCell;
        public Vector2Int BotCell;
        public readonly List<Island> Islands = new List<Island>();

        public ArchipelagoMap(int seed, int size)
        {
            Seed = seed;
            Size = size;
            Land = new bool[size * size];
            Height = new float[size * size];
            Beach = new bool[size * size];
            Cliff = new bool[size * size];
            Forest = new bool[size * size];
            Stony = new bool[size * size];
            Shore = new bool[size * size];
        }

        public bool IsRelief(int i) => Land[i] && Height[i] >= ArchipelagoGenerator.ReliefFrom;
    }

    /// <summary>A small, fast, seedable generator (xorshift64*) -- the same numbers on every platform.</summary>
    public sealed class SeedRandom
    {
        private ulong _state;

        public SeedRandom(int seed)
        {
            _state = (ulong)(uint)seed * 0x9E3779B97F4A7C15UL + 0x632BE59BD9B4E019UL;
            if (_state == 0) _state = 1;
            for (var i = 0; i < 4; i++) NextULong();
        }

        public ulong NextULong()
        {
            _state ^= _state >> 12;
            _state ^= _state << 25;
            _state ^= _state >> 27;
            return _state * 2685821657736338717UL;
        }

        /// <summary>[0, 1).</summary>
        public float Next01() => (NextULong() >> 40) / (float)(1UL << 24);

        public float Range(float min, float max) => min + (max - min) * Next01();

        /// <summary>[min, max).</summary>
        public int RangeInt(int min, int max) => min + (int)(Next01() * (max - min));

        /// <summary>A standard normal deviate (Box-Muller).</summary>
        public float Normal()
        {
            var u = Mathf.Max(1e-6f, Next01());
            var v = Next01();
            return Mathf.Sqrt(-2f * Mathf.Log(u)) * Mathf.Cos(2f * Mathf.PI * v);
        }
    }

    /// <summary>Value noise on a random grid, smoothstepped; fractal sums of it.</summary>
    public static class Noise
    {
        public static float[] Value(int size, int cell, SeedRandom rng)
        {
            cell = Mathf.Max(2, cell);
            var n = size / cell + 3;
            var grid = new float[n * n];
            for (var i = 0; i < grid.Length; i++) grid[i] = rng.Next01();
            var result = new float[size * size];
            for (var y = 0; y < size; y++)
            {
                var gy = (float)y / cell;
                var y0 = (int)gy;
                var fy = gy - y0;
                var sy = fy * fy * (3f - 2f * fy);
                for (var x = 0; x < size; x++)
                {
                    var gx = (float)x / cell;
                    var x0 = (int)gx;
                    var fx = gx - x0;
                    var sx = fx * fx * (3f - 2f * fx);
                    var a = grid[y0 * n + x0];
                    var b = grid[y0 * n + x0 + 1];
                    var c = grid[(y0 + 1) * n + x0];
                    var d = grid[(y0 + 1) * n + x0 + 1];
                    result[y * size + x] = (a * (1f - sx) + b * sx) * (1f - sy) + (c * (1f - sx) + d * sx) * sy;
                }
            }
            return result;
        }

        public static float[] Fbm(int size, SeedRandom rng, int baseCell, int octaves = 4)
        {
            var total = new float[size * size];
            float amp = 1f, norm = 0f;
            var cell = baseCell;
            for (var o = 0; o < octaves; o++)
            {
                var layer = Value(size, Mathf.Max(2, cell), rng);
                for (var i = 0; i < total.Length; i++) total[i] += amp * layer[i];
                norm += amp;
                amp *= 0.5f;
                cell /= 2;
            }
            for (var i = 0; i < total.Length; i++) total[i] /= norm;
            return total;
        }
    }
}
