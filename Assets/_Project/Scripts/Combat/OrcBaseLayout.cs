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
            Route = null;
        }

        /// <summary>A patrol's member: it walks the loop of `route` (camp frame, metres), starting at its first point.</summary>
        public GuardSlot(OrcKind kind, int level, Vector2[] route)
        {
            Kind = kind;
            Level = level;
            Local = route[0];
            Route = route;
        }

        public OrcKind Kind { get; }
        public int Level { get; }
        public Vector2 Local { get; }

        /// <summary>The patrol's loop, or null for a guard who holds its post.</summary>
        public Vector2[] Route { get; }
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

        /// <summary>Ground outside Bounds that is still the camp's zone for its guards (the main base's skull way in), or empty.</summary>
        public RectInt Apron { get; set; }
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
        /// The main base, living variant A «Логово в рёбрах» (the user approved the scheme
        /// Design/orc_base_living_a.png, 2026-11-06): three zones one behind the other, each gate
        /// offset from the last, so the way to the portal zig-zags through the whole base -- the
        /// yard (two war tents, two stores) behind the skull's way in; the chief's quarter (his
        /// house, the throwers' ranges, the brute pits, the treasury, its front wall bulging round
        /// the war drum); the portal's sanctum, a fortress in the fortress (the user: the player may
        /// come at it from any side, not only through the gates) -- two walls all round with a
        /// corridor between, the outer gate right, the inner one left. 17 towers stand on the
        /// walls' bends. Five patrols walk their loops (GuardSlot.Route) and a guard on each zone's
        /// spots. Every building at MainLevel -- drawn from the main base's own models.
        /// The scheme's cells sit 7 further back: here the portal's 4x4 is centred on the origin.
        /// </summary>
        private static CampLayout Main()
        {
            const int L = MainLevel;
            var pieces = new List<CampPiece>();
            var walls = new HashSet<Vector2Int>();
            var gates = new List<Vector2Int>();
            void H(int x0, int x1, int z, int gateX = int.MinValue)
            {
                for (var x = x0; x <= x1; x++)
                {
                    if (gateX != int.MinValue && (x == gateX || x == gateX + 1)) continue;
                    walls.Add(new Vector2Int(x, z));
                }
                if (gateX != int.MinValue) gates.Add(new Vector2Int(gateX, z));
            }
            void V(int x, int z0, int z1)
            {
                for (var z = z0; z <= z1; z++) walls.Add(new Vector2Int(x, z));
            }

            // the yard: its front with the skull's way in, its sides, its back on both sides of the chief's quarter
            H(-19, 18, -30, gateX: -1);
            V(-19, -29, -19);
            V(18, -29, -19);
            H(-19, -15, -19);
            H(14, 18, -19);
            // the chief's quarter: its gate LEFT of the yard's, its front bulging forward round the drum
            H(-14, -9, -19, gateX: -10);
            H(-8, 7, -21);
            V(-8, -20, -20);
            V(7, -20, -20);
            H(8, 13, -19);
            V(-14, -18, -10);
            V(13, -18, -10);
            // the sanctum: two walls all round, the outer gate RIGHT, the inner LEFT
            H(-14, 13, -9, gateX: 5);
            V(-14, -8, 7);
            V(13, -8, 7);
            H(-14, 13, 7);
            H(-11, 10, -6, gateX: -6);
            V(-11, -5, 3);
            V(10, -5, 3);
            H(-11, 10, 4);

            // the towers on the walls' bends take the wall cells they stand on
            foreach (var (x, z) in new[]
                     {
                         (-19, -30), (17, -30), (-19, -20), (17, -20), (-9, -22), (6, -22),
                         (-14, -10), (12, -10), (-14, 6), (12, 6), (-14, -2), (12, -2), (-1, 6),
                         (-11, 3), (9, 3), (-11, -6), (9, -6),
                     })
            {
                pieces.Add(Piece(OrcCampBuildingKind.Watchtower, L, x, z, 2, 2));
                for (var dx = 0; dx < 2; dx++)
                    for (var dz = 0; dz < 2; dz++)
                        walls.Remove(new Vector2Int(x + dx, z + dz));
            }
            foreach (var cell in walls) pieces.Add(Piece(OrcCampBuildingKind.Palisade, L, cell.x, cell.y, 1, 1));
            foreach (var gate in gates) pieces.Add(Piece(OrcCampBuildingKind.GateTower, L, gate.x, gate.y, 2, 1));

            // the yard
            pieces.Add(Piece(OrcCampBuildingKind.WarTent, L, -17, -28, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.WarTent, L, 14, -28, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.Storehouse, L, -17, -23, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.Storehouse, L, 15, -23, 2, 2));
            // the chief's quarter
            pieces.Add(Piece(OrcCampBuildingKind.ThrowerRange, L, -13, -18, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.ThrowerRange, L, 10, -18, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.BrutePit, L, -12, -14, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.BrutePit, L, 10, -14, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.ChiefHouse, L, -1, -17, 3, 3));
            pieces.Add(Piece(OrcCampBuildingKind.Cage, L, -4, -16, 1, 1));
            pieces.Add(Piece(OrcCampBuildingKind.Cage, L, 3, -16, 1, 1));
            pieces.Add(Piece(OrcCampBuildingKind.Treasury, L, -7, -13, 2, 2));
            // the sanctum
            pieces.Add(Piece(OrcCampBuildingKind.Totem, L, -5, -1, 1, 1));
            pieces.Add(Piece(OrcCampBuildingKind.Totem, L, 4, -1, 1, 1));
            pieces.Add(Piece(OrcCampBuildingKind.Storehouse, L, -9, -4, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.Storehouse, L, 7, -4, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.Cage, L, 0, -4, 1, 1));

            var guards = new List<GuardSlot>();
            void Post(OrcKind kind, int level, params (float x, float z)[] at)
            {
                foreach (var (x, z) in at) guards.Add(new GuardSlot(kind, level, new Vector2(x, z)));
            }
            void Patrol((OrcKind kind, int level)[] members, params (float x, float z)[] at)
            {
                var route = new Vector2[at.Length];
                for (var i = 0; i < at.Length; i++) route[i] = new Vector2(at[i].x, at[i].z);
                foreach (var (kind, level) in members) guards.Add(new GuardSlot(kind, level, route));
            }
            var grunt = (OrcKind.Raider, 3);
            var brute = (OrcKind.Brute, 2);
            // 1: the yard's watch, three grunts round the yard
            Patrol(new[] { grunt, grunt, grunt }, (-13f, -28.5f), (12.5f, -28.5f), (12.5f, -24.5f), (-13f, -24.5f));
            // 2: round the chief's quarter, a brute and two grunts, in through its gate from the yard
            Patrol(new[] { brute, grunt, grunt }, (-9f, -23f), (-9f, -17.5f), (-9f, -10.5f), (8.5f, -10.5f), (8.5f, -17.5f), (-9f, -17.5f));
            // 3: the corridor between the sanctum's two walls, two brutes
            Patrol(new[] { brute, brute }, (-11.5f, -7f), (11.5f, -7f), (11.5f, 5.5f), (-11.5f, 5.5f));
            // 5: round the portal itself, two brutes and a grunt
            Patrol(new[] { brute, brute, grunt }, (-9.5f, -4.5f), (9.5f, -4.5f), (9.5f, 1.5f), (8.5f, 3.4f), (-8.5f, 3.4f), (-9.5f, 1.5f));
            // 4: at the skull, two grunts from its jaws out to the stakes and back
            Patrol(new[] { grunt, grunt }, (0f, -31.5f), (0f, -41f));

            // the yard's campfire, the chief's campfire, the sanctum's outer gate
            Post(OrcKind.Raider, 3, (3f, -26f), (6f, -26.5f), (4.5f, -25f), (4.6f, -11.6f), (7.4f, -13.4f));
            Post(OrcKind.Brute, 2, (4.5f, -10.3f), (7.5f, -10.3f));
            // a thrower by the towers: the yard's corners, the bulge, the quarter, the sanctum
            Post(OrcKind.Thrower, 1, (-16f, -28.6f), (15.5f, -28.6f), (-17.5f, -20.5f), (16.5f, -20.5f),
                (-6.5f, -19.5f), (5.5f, -19.5f), (-12.5f, -10.5f), (11.5f, -10.5f),
                (-11.5f, 1f), (11.5f, 1f), (2.5f, 5.5f), (-8f, 0f), (8f, 0f));
            // the chief by his throne behind his house
            Post(OrcKind.Boss, 1, (0.5f, -11.5f));

            // the skull's way in before the gate is the base's ground too: its patrol fights there
            return new CampLayout(L, new RectInt(-19, -30, 38, 38), pieces, guards, new List<RectInt>(), 4)
            {
                Apron = new RectInt(-5, -44, 10, 14),
            };
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
