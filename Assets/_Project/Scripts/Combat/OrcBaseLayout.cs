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

    /// <summary>A camp at one level: its buildings, its walls, its guards, and the ground it covers.</summary>
    public sealed class CampLayout
    {
        public CampLayout(int level, int halfExtent, List<CampPiece> pieces, List<GuardSlot> guards)
            : this(level, new RectInt(-halfExtent, -halfExtent, 2 * halfExtent, 2 * halfExtent), pieces, guards,
                new List<RectInt>(), 2)
        {
        }

        public CampLayout(int level, RectInt bounds, List<CampPiece> pieces, List<GuardSlot> guards,
            List<RectInt> mountains, int portalSize)
        {
            Level = level;
            Bounds = bounds;
            HalfExtent = Mathf.Max(Mathf.Max(-bounds.xMin, bounds.xMax), Mathf.Max(-bounds.yMin, bounds.yMax));
            Pieces = pieces;
            Guards = guards;
            Mountains = mountains;
            PortalSize = portalSize;
        }

        public int Level { get; }

        /// <summary>The cells the camp covers, in its frame (yMin is the front, the gate's side).</summary>
        public RectInt Bounds { get; }

        /// <summary>The largest distance from the portal's centre to an edge of Bounds -- what a square camp was described by.</summary>
        public int HalfExtent { get; }

        /// <summary>Impassable rock the camp is backed into (the main base): blocks of cells, in its frame.</summary>
        public IReadOnlyList<RectInt> Mountains { get; }

        /// <summary>The portal's plot: 2 for a camp's portal, 4 for the great portal.</summary>
        public int PortalSize { get; }

        public bool IsMain => Level == OrcBaseLayout.MainLevel;

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

        /// <summary>The great portal's base: its own plan and its own stone-and-green models, every building beyond the camps' top level.</summary>
        public const int MainLevel = 4;

        /// <summary>The ground a camp of this level covers in its frame -- reserved from the start, so a camp can grow into it.</summary>
        public static RectInt BoundsAt(int level) => For(level).Bounds;

        private static readonly CampLayout[] _layouts = { Level1(), Level2(), Level3(), Main() };

        public static CampLayout For(int level) => _layouts[Mathf.Clamp(level, 1, MainLevel) - 1];

        /// <summary>The orc kind a spawner keeps topped up, or null for a building that spawns nothing.</summary>
        public static OrcKind? SpawnedBy(OrcCampBuildingKind kind) => kind switch
        {
            OrcCampBuildingKind.WarTent => OrcKind.Raider,
            OrcCampBuildingKind.BrutePit => OrcKind.Brute,
            OrcCampBuildingKind.ThrowerRange => OrcKind.Thrower,
            OrcCampBuildingKind.ChiefHouse => OrcKind.Boss,
            _ => (OrcKind?)null,
        };

        public static bool IsWall(OrcCampBuildingKind kind) =>
            kind == OrcCampBuildingKind.Palisade || kind == OrcCampBuildingKind.Gate || kind == OrcCampBuildingKind.GateTower;

        private static CampLayout Level1()
        {
            var pieces = new List<CampPiece>();
            Towers(pieces, 1, 6);
            pieces.Add(Piece(OrcCampBuildingKind.WarTent, 1, -1, 3, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.Cage, 1, 3, -1, 1, 1));
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
            pieces.Add(Piece(OrcCampBuildingKind.Cage, 2, 4, -2, 1, 1));
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
            pieces.Add(Piece(OrcCampBuildingKind.Cage, 3, 4, -2, 1, 1));
            pieces.Add(Piece(OrcCampBuildingKind.Cage, 3, -4, -1, 1, 1));

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

        /// <summary>
        /// The main base, after the user's sketch (2026-10-19): backed into mountains behind and on
        /// both sides of its two inner zones, three stone walls one inside the other, each entered
        /// through a stone gate under a tower. The great portal (4x4) at the back between two
        /// totems; the chief's house and the chief in the middle zone; the widest zone in front.
        /// Every building at MainLevel -- drawn from the main base's own models.
        /// </summary>
        private static CampLayout Main()
        {
            const int L = MainLevel;
            var pieces = new List<CampPiece>();
            void Wall(int x, int z) => pieces.Add(Piece(OrcCampBuildingKind.Palisade, L, x, z, 1, 1));
            void HLine(int x0, int x1, int z, bool gate)
            {
                for (var x = x0; x <= x1; x++)
                {
                    if (gate && (x == -1 || x == 0)) continue;
                    Wall(x, z);
                }
                if (gate) pieces.Add(Piece(OrcCampBuildingKind.GateTower, L, -1, z, 2, 1));
            }
            void VLine(int x, int z0, int z1)
            {
                for (var z = z0; z <= z1; z++) Wall(x, z);
            }

            // the back zone's front, the middle zone's front and its short sides, the front zone
            HLine(-13, 12, -8, gate: true);
            HLine(-14, 13, -17, gate: true);
            VLine(-14, -16, -15);
            VLine(13, -16, -15);
            HLine(-19, -15, -15, gate: false);
            HLine(14, 18, -15, gate: false);
            HLine(-19, 18, -23, gate: true);
            VLine(-19, -22, -16);
            VLine(18, -22, -16);

            // the back zone
            pieces.Add(Piece(OrcCampBuildingKind.Totem, L, -5, 0, 1, 1));
            pieces.Add(Piece(OrcCampBuildingKind.Totem, L, 4, 0, 1, 1));
            pieces.Add(Piece(OrcCampBuildingKind.BrutePit, L, -12, -2, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.BrutePit, L, 10, -2, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.Storehouse, L, -13, -5, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.Storehouse, L, 11, -5, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.Watchtower, L, -9, -7, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.Watchtower, L, 7, -7, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.Cage, L, -7, -4, 1, 1));
            pieces.Add(Piece(OrcCampBuildingKind.Cage, L, 6, -4, 1, 1));
            // the middle zone
            pieces.Add(Piece(OrcCampBuildingKind.ChiefHouse, L, -2, -13, 3, 3));
            pieces.Add(Piece(OrcCampBuildingKind.ThrowerRange, L, -6, -11, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.ThrowerRange, L, 3, -11, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.Storehouse, L, -9, -11, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.Storehouse, L, 7, -11, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.Watchtower, L, -11, -16, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.Watchtower, L, 9, -16, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.Cage, L, 2, -9, 1, 1));
            // the front zone
            pieces.Add(Piece(OrcCampBuildingKind.WarTent, L, -16, -17, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.WarTent, L, 14, -17, 2, 2));
            foreach (var (x, z) in new[] { (-18, -17), (16, -17), (-18, -22), (-9, -22), (7, -22), (16, -22) })
            {
                pieces.Add(Piece(OrcCampBuildingKind.Watchtower, L, x, z, 2, 2));
            }
            pieces.Add(Piece(OrcCampBuildingKind.Storehouse, L, -13, -22, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.Storehouse, L, 10, -22, 2, 2));

            var guards = new List<GuardSlot>();
            void Post(OrcKind kind, int level, params (float x, float z)[] at)
            {
                foreach (var (x, z) in at) guards.Add(new GuardSlot(kind, level, new Vector2(x, z)));
            }
            Post(OrcKind.Thrower, 1, (-7.5f, 1.1f), (7.4f, 1.4f), (0f, -4.7f),
                (-11.5f, -10.3f), (11.8f, -10.4f), (-4.4f, -15.5f), (4.1f, -15.1f),
                (-13.3f, -19.6f), (-2.2f, -20.5f), (1f, -20.5f), (11f, -19.4f));
            Post(OrcKind.Raider, 3, (-9f, -3.1f), (-2.7f, -2.7f), (2.6f, -2.7f), (9.3f, -3.3f),
                (-10.7f, -13.2f), (10.4f, -13.4f), (-7.5f, -15.7f), (8f, -15.7f),
                (-17f, -18.7f), (-14.4f, -21.6f), (-10.1f, -21.1f), (-5f, -21.3f), (3.1f, -21.1f),
                (9.5f, -19.5f), (13.5f, -21.1f), (16.5f, -18.7f));
            Post(OrcKind.Brute, 2, (-11.4f, -6.6f), (-4.8f, -6.4f), (4.9f, -6.7f), (11.6f, -6.7f),
                (-6.3f, -13.1f), (6.3f, -13.1f));
            Post(OrcKind.Boss, 1, (-0.5f, -15f));

            var mountains = new List<RectInt>
            {
                new RectInt(-19, 3, 38, 6),
                new RectInt(-19, -14, 6, 17),
                new RectInt(13, -14, 6, 17),
            };
            return new CampLayout(L, new RectInt(-19, -23, 38, 32), pieces, guards, mountains, 4);
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
