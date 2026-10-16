using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>One building of a camp's plan: what, at which level, and its plot in the camp's own cells.</summary>
    public readonly struct CampPiece
    {
        public CampPiece(OrcCampBuildingKind kind, int level, Vector2Int min, Vector2Int size)
        {
            Kind = kind;
            Level = level;
            Min = min;
            Size = size;
        }

        public OrcCampBuildingKind Kind { get; }
        public int Level { get; }

        /// <summary>The plot's lower-left cell in the camp's frame (see OrcBaseLayout).</summary>
        public Vector2Int Min { get; }
        public Vector2Int Size { get; }
    }

    /// <summary>One guard's post in a camp's plan: what kind of orc stands there, at which level, and where (camp frame, metres).</summary>
    public readonly struct GuardSlot
    {
        public GuardSlot(OrcKind kind, int level, Vector2 local)
        {
            Kind = kind;
            Level = level;
            Local = local;
        }

        public OrcKind Kind { get; }
        public int Level { get; }
        public Vector2 Local { get; }
    }

    /// <summary>A camp at one level: its buildings, its walls, its guards, and how big a square it covers.</summary>
    public sealed class CampLayout
    {
        public CampLayout(int level, int halfExtent, List<CampPiece> pieces, List<GuardSlot> guards)
        {
            Level = level;
            HalfExtent = halfExtent;
            Pieces = pieces;
            Guards = guards;
        }

        public int Level { get; }

        /// <summary>The camp covers cells -HalfExtent .. HalfExtent-1 on both axes of its frame.</summary>
        public int HalfExtent { get; }

        public IReadOnlyList<CampPiece> Pieces { get; }
        public IReadOnlyList<GuardSlot> Guards { get; }
    }

    /// <summary>
    /// The walled orc bases, drawn by the user per level (2026-10-16) and filled in with the
    /// spawners they asked for. Pure data and pure geometry, so the plans can be tested without
    /// a scene.
    ///
    /// THE CAMP'S FRAME: cells, with the portal's 2x2 plot at (-1,-1)..(0,0) -- the origin is the
    /// portal's centre, on a cell corner. +x is the portal's right, +z its back, so the gate (and
    /// the town) is at -z. The camp turns with its portal in quarter turns (OrcRaidManager snaps
    /// the portal to face the Town Hall), so every plot maps onto whole cells (see ToWorld).
    ///
    /// Level 1: no walls -- four towers round the portal, a war tent behind it, four orcs.
    /// Level 2: a palisade with a gate, four level-2 towers, a loot store and a totem at the
    ///          sides, the war tent and the brute pit behind the portal, orcs and brutes.
    /// Level 3: two rings of the level-2 palisade; the outer one with a gate, eight level-3
    ///          towers, the orcs' tent and the throwers' range between the rings; the inner ring
    ///          entered through a gate under a tower, the brute pit, two stores and the totem
    ///          inside.
    /// </summary>
    public static class OrcBaseLayout
    {
        public const int MaxLevel = 3;

        /// <summary>The biggest camp's half extent: the square a portal reserves from the start, so the camp can grow into it.</summary>
        public static int HalfExtentAt(int level) => For(level).HalfExtent;

        private static readonly CampLayout[] _layouts = { Level1(), Level2(), Level3() };

        public static CampLayout For(int level) => _layouts[Mathf.Clamp(level, 1, MaxLevel) - 1];

        /// <summary>The orc kind a spawner keeps topped up, or null for a building that spawns nothing.</summary>
        public static OrcKind? SpawnedBy(OrcCampBuildingKind kind) => kind switch
        {
            OrcCampBuildingKind.WarTent => OrcKind.Raider,
            OrcCampBuildingKind.BrutePit => OrcKind.Brute,
            OrcCampBuildingKind.ThrowerRange => OrcKind.Thrower,
            _ => (OrcKind?)null,
        };

        public static bool IsWall(OrcCampBuildingKind kind) =>
            kind == OrcCampBuildingKind.Palisade || kind == OrcCampBuildingKind.Gate || kind == OrcCampBuildingKind.GateTower;

        private static CampLayout Level1()
        {
            var pieces = new List<CampPiece>();
            Towers(pieces, 1, 6);
            pieces.Add(Piece(OrcCampBuildingKind.WarTent, 1, -1, 3, 2, 2));
            var guards = new List<GuardSlot>();
            foreach (var p in Diagonals(2.5f)) guards.Add(new GuardSlot(OrcKind.Raider, 1, p));
            return new CampLayout(1, 6, pieces, guards);
        }

        private static CampLayout Level2()
        {
            var pieces = new List<CampPiece>();
            Ring(pieces, 7, 1, OrcCampBuildingKind.Gate, 1);
            Towers(pieces, 2, 6);
            pieces.Add(Piece(OrcCampBuildingKind.WarTent, 1, -3, 4, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.BrutePit, 1, 1, 4, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.Storehouse, 1, -6, -1, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.Totem, 1, 4, 0, 1, 1));
            var guards = new List<GuardSlot>();
            foreach (var p in Diagonals(2.5f)) guards.Add(new GuardSlot(OrcKind.Brute, 1, p));
            foreach (var p in new[]
                     {
                         new Vector2(-3.5f, 5.5f), new Vector2(3.5f, 5.5f), new Vector2(-2.5f, -5f), new Vector2(2.5f, -5f),
                         new Vector2(-5f, 2.5f), new Vector2(-5f, -2.5f), new Vector2(5f, 2.5f), new Vector2(5f, -2.5f),
                     })
            {
                guards.Add(new GuardSlot(OrcKind.Raider, 2, p));
            }
            return new CampLayout(2, 7, pieces, guards);
        }

        private static CampLayout Level3()
        {
            var pieces = new List<CampPiece>();
            Ring(pieces, 11, 2, OrcCampBuildingKind.Gate, 2);
            Ring(pieces, 7, 2, OrcCampBuildingKind.GateTower, 1);
            Towers(pieces, 3, 10);
            Towers(pieces, 3, 6);
            // between the rings
            pieces.Add(Piece(OrcCampBuildingKind.WarTent, 1, -10, -1, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.ThrowerRange, 1, 7, 2, 2, 2));
            // inside
            pieces.Add(Piece(OrcCampBuildingKind.BrutePit, 1, -1, 3, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.Storehouse, 2, -6, 0, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.Storehouse, 2, -6, -3, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.Totem, 2, 4, 0, 1, 1));

            var guards = new List<GuardSlot>();
            foreach (var p in Diagonals(2.5f)) guards.Add(new GuardSlot(OrcKind.Brute, 2, p));
            foreach (var p in new[]
                     {
                         // inside the inner ring
                         new Vector2(-2.5f, 4.5f), new Vector2(2.5f, 4.5f), new Vector2(5f, 2.5f), new Vector2(5f, -2.5f),
                         new Vector2(-2.5f, -5f), new Vector2(2.5f, -5f),
                         // between the rings
                         new Vector2(0f, 8.5f), new Vector2(-8.5f, 3f), new Vector2(-8.5f, -3f), new Vector2(8.5f, 6.2f),
                         new Vector2(8.5f, -1.5f), new Vector2(-4.5f, -8.5f), new Vector2(2.5f, -8.5f),
                     })
            {
                guards.Add(new GuardSlot(OrcKind.Raider, 3, p));
            }
            foreach (var p in new[]
                     {
                         new Vector2(-6f, 8.5f), new Vector2(-2f, 8.5f), new Vector2(2f, 8.5f), new Vector2(6f, 8.5f),
                         new Vector2(-8.5f, 5.5f), new Vector2(-8.5f, -5.5f),
                         new Vector2(8.5f, 5.2f), new Vector2(8.5f, 0.5f), new Vector2(8.5f, -3.5f), new Vector2(8.5f, -6f),
                         new Vector2(-6.5f, -8.5f), new Vector2(-2.5f, -8.5f), new Vector2(4.5f, -8.5f), new Vector2(6.5f, -8.5f),
                     })
            {
                guards.Add(new GuardSlot(OrcKind.Thrower, 1, p));
            }
            return new CampLayout(3, 11, pieces, guards);
        }

        private static CampPiece Piece(OrcCampBuildingKind kind, int level, int x, int z, int w, int h) =>
            new CampPiece(kind, level, new Vector2Int(x, z), new Vector2Int(w, h));

        /// <summary>Four 2x2 towers in the corners just inside a ring of half extent `ring`.</summary>
        private static void Towers(List<CampPiece> pieces, int level, int ring)
        {
            foreach (var x in new[] { -ring + 1, ring - 3 })
            {
                foreach (var z in new[] { -ring + 1, ring - 3 })
                {
                    pieces.Add(Piece(OrcCampBuildingKind.Watchtower, level, x, z, 2, 2));
                }
            }
        }

        /// <summary>A square of palisade on the cells -half..half-1, its gate (2 cells, along x) in the middle of the front side.</summary>
        private static void Ring(List<CampPiece> pieces, int half, int wallLevel, OrcCampBuildingKind gate, int gateLevel)
        {
            for (var x = -half; x < half; x++)
            {
                for (var z = -half; z < half; z++)
                {
                    var onEdge = x == -half || x == half - 1 || z == -half || z == half - 1;
                    if (!onEdge) continue;
                    if (z == -half && (x == -1 || x == 0)) continue;
                    pieces.Add(Piece(OrcCampBuildingKind.Palisade, wallLevel, x, z, 1, 1));
                }
            }
            pieces.Add(Piece(gate, gateLevel, -1, -half, 2, 1));
        }

        private static IEnumerable<Vector2> Diagonals(float d)
        {
            yield return new Vector2(-d, d);
            yield return new Vector2(d, d);
            yield return new Vector2(-d, -d);
            yield return new Vector2(d, -d);
        }

        /// <summary>How many quarter turns (0..3, clockwise from above) a yaw in degrees is.</summary>
        public static int QuarterTurns(float yawDegrees) => ((Mathf.RoundToInt(yawDegrees / 90f) % 4) + 4) % 4;

        /// <summary>A point of the camp's frame turned `quarters` quarter turns, as Unity's +Y rotation turns it.</summary>
        public static Vector2 Rotate(Vector2 local, int quarters)
        {
            switch (((quarters % 4) + 4) % 4)
            {
                case 1: return new Vector2(local.y, -local.x);
                case 2: return new Vector2(-local.x, -local.y);
                case 3: return new Vector2(-local.y, local.x);
                default: return local;
            }
        }

        /// <summary>
        /// A plot of the camp's frame on the world grid: its lower-left cell and its size. `centre`
        /// is the portal's centre as a cell CORNER (the portal's cell + (1,1)).
        /// </summary>
        public static void ToWorld(Vector2Int localMin, Vector2Int size, Vector2Int centre, int quarters,
            out Vector2Int worldMin, out Vector2Int worldSize)
        {
            var a = Rotate(localMin, quarters);
            var b = Rotate(localMin + size, quarters);
            worldMin = centre + new Vector2Int(Mathf.RoundToInt(Mathf.Min(a.x, b.x)), Mathf.RoundToInt(Mathf.Min(a.y, b.y)));
            worldSize = quarters % 2 == 0 ? size : new Vector2Int(size.y, size.x);
        }

        /// <summary>
        /// The level a camp has grown to at this progression score. Each camp further out (higher
        /// `rank`, 0 = the nearest) grows `perRank` points sooner -- that is how difficulty goes by
        /// distance while every camp still starts small.
        /// </summary>
        public static int LevelFor(int score, int rank, int level2Score, int level3Score, int perRank, int maxLevel)
        {
            var shift = Mathf.Max(0, rank) * Mathf.Max(0, perRank);
            var level = 1;
            if (score >= level2Score - shift) level = 2;
            if (score >= level3Score - shift) level = 3;
            return Mathf.Clamp(level, 1, Mathf.Max(1, maxLevel));
        }
    }
}
