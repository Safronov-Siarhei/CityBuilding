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

        /// <summary>The Mountains draw no rocks: a bog («Гать», the island's own swamp water shows) or the beast's spine («Хребет», its vertebrae are the skeleton's dress).</summary>
        public bool BogMountains { get; set; }
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

        // the main base's living variants (the user, 2026-11-06): A «Логово в рёбрах», B «Гать», C «Хребет»
        private static readonly CampLayout[] _mains = { _layouts[MainLevel - 1], MainGat(), MainSpine() };

        public const int MainVariantCount = 3;

        /// <summary>
        /// Which living variant the map's main base is (0 = A, 1 = B «Гать»). One main base a map, so
        /// one value a session: OrcRaidManager sets it from the map before it builds or restores the
        /// main base; a map that says nothing has A.
        /// </summary>
        public static int MainVariant { get; set; }

        public static CampLayout For(int level) => level >= MainLevel
            ? _mains[Mathf.Clamp(MainVariant, 0, MainVariantCount - 1)]
            : _layouts[Mathf.Clamp(level, 1, MainLevel) - 1];

        /// <summary>
        /// The variant an orc library island's base is: «Гать» on Isl-orc2/4/6 (their terrain is
        /// carved for it -- Tools/blender/islands.py, GAT), «Хребет» on Isl-orc3/5 (their pad is
        /// long -- SPINE), A on Isl-orc1.
        /// </summary>
        public static int VariantOfIsland(string islandName)
        {
            if (string.IsNullOrEmpty(islandName)) return 0;
            var last = islandName[islandName.Length - 1];
            if (!char.IsDigit(last)) return 0;
            var n = last - '0';
            if (n % 2 == 0) return 1;
            return n == 3 || n == 5 ? 2 : 0;
        }

        /// <summary>«Хребет»'s spine: the beast's giant vertebrae down the base's middle (x -1..0), impassable, cut by the cross walls and by one gap a zone.</summary>
        public static bool SpineCell(int x, int z)
        {
            if (x < -1 || x > 0 || z < -41 || z > -8) return false;
            if (z == -30 || z == -18) return false;                       // the cross walls
            return !(z == -33 || z == -32 || z == -21 || z == -20 || z == -10 || z == -9);   // the gaps
        }

        /// <summary>«Гать»'s dry patch: an octagon x -15..14, z -16..11 with its corners cut 4 cells (its edge is the wall).</summary>
        public static bool GatDry(int x, int z)
        {
            if (x < -15 || x > 14 || z < -16 || z > 11) return false;
            return Mathf.Min(x + 15, 14 - x) + Mathf.Min(z + 16, 11 - z) >= 4;
        }

        /// <summary>«Гать»'s causeways, 2 cells wide: the front one zig-zagging, the left and the right straight.</summary>
        public static bool GatCauseway(int x, int z)
        {
            if (x >= -1 && x <= 0 && ((z >= -21 && z <= -17) || (z >= -28 && z <= -25))) return true;   // the front, its two straight legs
            if (z >= -21 && z <= -20 && x >= -8 && x <= 0) return true;                                // ... its first bend, out left
            if (x >= -8 && x <= -7 && z >= -25 && z <= -21) return true;                               // ... down
            if (z >= -25 && z <= -24 && x >= -8 && x <= 0) return true;                                // ... and back
            if (z >= -2 && z <= -1 && x >= -26 && x <= -16) return true;                               // the left one
            if (z >= 3 && z <= 4 && x >= 15 && x <= 25) return true;                                   // the right one
            return false;
        }

        /// <summary>«Гать»'s towers standing on piles out in the bog (their ground is kept dry).</summary>
        // (properties, not fields: _mains above is built from them while the class initialises)
        public static Vector2Int[] GatPileTowers => _piles ??= new[]
        {
            new Vector2Int(-11, -23), new Vector2Int(2, -23), new Vector2Int(-11, -27), new Vector2Int(2, -27),
            new Vector2Int(-21, 0), new Vector2Int(20, 5),
        };

        private static Vector2Int[] _piles;

        /// <summary>«Гать»'s bounds: the bog round the patch, the causeways' land ends on its edge.</summary>
        public static RectInt GatBounds => new RectInt(-26, -28, 52, 48);

        /// <summary>Whether a cell of «Гать»'s ground is bog: inside its bounds, a cell off the dry patch (and the strip round it), the causeways and the piles.</summary>
        public static bool GatBog(int x, int z)
        {
            if (!GatBounds.Contains(new Vector2Int(x, z))) return false;
            for (var dx = -1; dx <= 1; dx++)
                for (var dz = -1; dz <= 1; dz++)
                    if (GatDry(x + dx, z + dz)) return false;
            if (GatCauseway(x, z)) return false;
            foreach (var t in GatPileTowers)
                if (x >= t.x && x <= t.x + 1 && z >= t.y && z <= t.y + 1) return false;
            return true;
        }

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

        /// <summary>
        /// The main base, living variant B «Гать» (the user approved the scheme
        /// Design/orc_base_living_b.png, 2026-11-06): a dry patch in the middle of an impassable bog,
        /// reached only by three plank causeways two cells wide -- the front one zig-zagging under
        /// towers on piles, the left and the right straight to gates in the side walls. One stone
        /// wall along the patch's edge (the bog is the outer ring); the portal on its mound behind
        /// a palisade gated at the BACK, so from any gate the way to it goes round. The bog is the
        /// layout's Mountains (BogMountains): impassable ground with no rocks drawn on it.
        /// </summary>
        private static CampLayout MainGat()
        {
            const int L = MainLevel;
            var pieces = new List<CampPiece>();
            var walls = new HashSet<Vector2Int>();
            for (var z = -16; z <= 11; z++)
            {
                for (var x = -15; x <= 14; x++)
                {
                    if (!GatDry(x, z)) continue;
                    if (GatDry(x - 1, z) && GatDry(x + 1, z) && GatDry(x, z - 1) && GatDry(x, z + 1)) continue;
                    walls.Add(new Vector2Int(x, z));
                }
            }
            // the three gates: the front one along the wall, the side ones across it
            foreach (var (x, z, w, h) in new[] { (-1, -16, 2, 1), (-15, -2, 1, 2), (14, 3, 1, 2) })
            {
                for (var dx = 0; dx < w; dx++)
                    for (var dz = 0; dz < h; dz++)
                        walls.Remove(new Vector2Int(x + dx, z + dz));
                pieces.Add(Piece(OrcCampBuildingKind.GateTower, L, x, z, w, h));
            }
            // towers: the cut corners, two beside each gate, and the piles in the bog
            var towers = new List<Vector2Int>
            {
                new Vector2Int(-15, -12), new Vector2Int(13, -12), new Vector2Int(-14, 7), new Vector2Int(12, 7),
                new Vector2Int(-4, -17), new Vector2Int(2, -17), new Vector2Int(-16, 1), new Vector2Int(-16, -5),
                new Vector2Int(14, 0), new Vector2Int(14, 5),
            };
            towers.AddRange(GatPileTowers);
            foreach (var t in towers)
            {
                pieces.Add(Piece(OrcCampBuildingKind.Watchtower, L, t.x, t.y, 2, 2));
                for (var dx = 0; dx < 2; dx++)
                    for (var dz = 0; dz < 2; dz++)
                        walls.Remove(new Vector2Int(t.x + dx, t.y + dz));
            }
            foreach (var cell in walls) pieces.Add(Piece(OrcCampBuildingKind.Palisade, L, cell.x, cell.y, 1, 1));

            // the portal's mound behind a wooden palisade, its gate at the back
            for (var x = -7; x <= 6; x++)
            {
                pieces.Add(Piece(OrcCampBuildingKind.Palisade, 2, x, -7, 1, 1));
                if (x != -1 && x != 0) pieces.Add(Piece(OrcCampBuildingKind.Palisade, 2, x, 6, 1, 1));
            }
            for (var z = -6; z <= 5; z++)
            {
                pieces.Add(Piece(OrcCampBuildingKind.Palisade, 2, -7, z, 1, 1));
                pieces.Add(Piece(OrcCampBuildingKind.Palisade, 2, 6, z, 1, 1));
            }
            pieces.Add(Piece(OrcCampBuildingKind.Gate, 2, -1, 6, 2, 1));

            pieces.Add(Piece(OrcCampBuildingKind.WarTent, L, -12, -13, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.WarTent, L, 9, -13, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.BrutePit, L, -12, 3, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.BrutePit, L, 9, -5, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.ThrowerRange, L, -11, -8, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.ThrowerRange, L, 9, 0, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.Storehouse, L, -5, -13, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.Storehouse, L, 3, -13, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.ChiefHouse, L, -1, 8, 3, 3));
            pieces.Add(Piece(OrcCampBuildingKind.Treasury, L, 4, 8, 2, 2));
            pieces.Add(Piece(OrcCampBuildingKind.Cage, L, -6, 9, 1, 1));
            pieces.Add(Piece(OrcCampBuildingKind.Cage, L, -9, -12, 1, 1));
            pieces.Add(Piece(OrcCampBuildingKind.Cage, L, 7, 4, 1, 1));
            pieces.Add(Piece(OrcCampBuildingKind.Totem, L, -4, 3, 1, 1));
            pieces.Add(Piece(OrcCampBuildingKind.Totem, L, 3, -4, 1, 1));

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
            // 1: the front causeway, out to its land end and back
            Patrol(new[] { grunt, grunt }, (0f, -17.5f), (0f, -20f), (-7f, -20f), (-7f, -24f), (0f, -24f), (0f, -27.5f),
                (0f, -24f), (-7f, -24f), (-7f, -20f), (0f, -20f));
            // 2, 3: the side causeways
            Patrol(new[] { grunt, grunt }, (-16.5f, -1f), (-25.5f, -1f));
            Patrol(new[] { grunt, grunt }, (15.5f, 4f), (24.5f, 4f));
            // 4: along the wall inside, a brute and two grunts
            Patrol(new[] { brute, grunt, grunt }, (-10.5f, -14.3f), (9.5f, -14.3f), (12.5f, -9.5f), (11.5f, 6.5f), (9f, 9.5f),
                (-10f, 9.5f), (-12.5f, 6f), (-13.2f, -9f));
            // 5: round the portal's mound
            Patrol(new[] { brute, brute }, (-5.5f, -5.5f), (4.5f, -5.5f), (4.5f, 4.5f), (-5.5f, 4.5f));

            // the fire, the ambush at the front causeway's bend, the mound's back gate
            Post(OrcKind.Raider, 3, (-8f, -9.5f), (-5f, -9.4f), (-6.5f, -11.6f), (-5f, -24.5f), (-3.5f, -24.5f));
            Post(OrcKind.Brute, 2, (-1.5f, 7.3f), (1.5f, 7.3f));
            Post(OrcKind.Thrower, 1, (-12.5f, -10.5f), (12.5f, -10.5f), (-11.5f, 7.5f), (11.5f, 8f),
                (-3f, -15f), (2.5f, -15f), (-13.5f, 1.5f), (-13.5f, -4.5f), (13.2f, 1f), (13.2f, 6f));
            Post(OrcKind.Boss, 1, (-3.5f, 8.2f));

            // the bog: rows of bog cells merged into rects, one obstacle each
            var bog = new List<RectInt>();
            var open = new Dictionary<(int, int), RectInt>();
            for (var z = GatBounds.yMin; z < GatBounds.yMax; z++)
            {
                var runs = new List<(int, int)>();
                for (var x = GatBounds.xMin; x < GatBounds.xMax; x++)
                {
                    if (!GatBog(x, z)) continue;
                    var x0 = x;
                    while (x + 1 < GatBounds.xMax && GatBog(x + 1, z)) x++;
                    runs.Add((x0, x));
                }
                var next = new Dictionary<(int, int), RectInt>();
                foreach (var run in runs)
                {
                    next[run] = open.TryGetValue(run, out var r) ? new RectInt(r.x, r.y, r.width, r.height + 1)
                        : new RectInt(run.Item1, z, run.Item2 - run.Item1 + 1, 1);
                    open.Remove(run);
                }
                bog.AddRange(open.Values);
                open = next;
            }
            bog.AddRange(open.Values);

            return new CampLayout(L, GatBounds, pieces, guards, bog, 4) { BogMountains = true };
        }

        /// <summary>
        /// The main base, living variant C «Хребет» (the user approved the scheme
        /// Design/orc_base_living_c.png, 2026-11-06): the base built inside the beast's ribcage, 24
        /// cells wide and long, its spine down the middle from the skull at the gate to the tail
        /// behind the portal. The spine's vertebrae are impassable and split every zone in two;
        /// the gaps in it and the cross walls' gates alternate sides, so the way to the portal
        /// snakes: the plaza, the left half, across the spine, the right half, through a gate, and
        /// on. The sanctum is walled twice round its sides and back (the user: the player may go
        /// straight at the portal), its corridor entered only from inside by a side gate.
        /// </summary>
        private static CampLayout MainSpine()
        {
            const int L = MainLevel;
            var pieces = new List<CampPiece>();
            var walls = new HashSet<Vector2Int>();
            var gates = new List<(int x, int z, int w, int h)>();
            void H(int x0, int x1, int z, int gateX = int.MinValue)
            {
                for (var x = x0; x <= x1; x++)
                {
                    if (gateX != int.MinValue && (x == gateX || x == gateX + 1)) continue;
                    walls.Add(new Vector2Int(x, z));
                }
                if (gateX != int.MinValue) gates.Add((gateX, z, 2, 1));
            }
            void V(int x, int z0, int z1)
            {
                for (var z = z0; z <= z1; z++) walls.Add(new Vector2Int(x, z));
            }
            // the outer wall, the inner sanctum's back
            H(-12, 11, -48, gateX: -1);
            V(-12, -47, 4);
            V(11, -47, 4);
            H(-12, 11, 5);
            // the cross walls, their gates alternating sides
            H(-11, 10, -42, gateX: -8);
            H(-11, 10, -30, gateX: 6);
            H(-11, 10, -18, gateX: -8);
            H(-11, 10, -7, gateX: 6);
            // the sanctum's second wall round its sides and back
            V(-15, -10, 8);
            V(14, -10, 8);
            H(-15, 14, 8);
            foreach (var x in new[] { -14, -13, 12, 13 }) walls.Add(new Vector2Int(x, -10));
            // the corridor's way in: a side gate in the inner wall
            walls.Remove(new Vector2Int(-12, -2));
            walls.Remove(new Vector2Int(-12, -1));
            gates.Add((-12, -2, 1, 2));

            foreach (var t in new[]
                     {
                         (-12, -48), (10, -48), (-12, 4), (10, 4),
                         (-12, -43), (10, -43), (-12, -31), (10, -31), (-12, -19), (10, -19), (-12, -8), (10, -8),
                         (-3, -43), (1, -31), (-3, -19), (1, -8),
                         (10, -2), (-1, 4),
                         (-15, -10), (13, -10), (-15, 7), (13, 7), (-15, 1), (13, 1), (-1, 7),
                     })
            {
                pieces.Add(Piece(OrcCampBuildingKind.Watchtower, L, t.Item1, t.Item2, 2, 2));
                for (var dx = 0; dx < 2; dx++)
                    for (var dz = 0; dz < 2; dz++)
                        walls.Remove(new Vector2Int(t.Item1 + dx, t.Item2 + dz));
            }
            foreach (var cell in walls) pieces.Add(Piece(OrcCampBuildingKind.Palisade, L, cell.x, cell.y, 1, 1));
            foreach (var g in gates) pieces.Add(Piece(OrcCampBuildingKind.GateTower, L, g.x, g.z, g.w, g.h));

            void B(OrcCampBuildingKind kind, int x, int z, int size = 2) => pieces.Add(Piece(kind, L, x, z, size, size));
            B(OrcCampBuildingKind.WarTent, -10, -46);
            B(OrcCampBuildingKind.WarTent, 7, -46);
            B(OrcCampBuildingKind.Storehouse, -10, -39);
            B(OrcCampBuildingKind.Cage, -4, -39, 1);
            B(OrcCampBuildingKind.WarTent, 8, -40);
            B(OrcCampBuildingKind.Storehouse, 8, -35);
            B(OrcCampBuildingKind.ThrowerRange, 8, -27);
            B(OrcCampBuildingKind.Cage, 4, -24, 1);
            B(OrcCampBuildingKind.BrutePit, -10, -27);
            B(OrcCampBuildingKind.Storehouse, -10, -22);
            B(OrcCampBuildingKind.ChiefHouse, -10, -15, 3);
            B(OrcCampBuildingKind.Cage, -5, -16, 1);
            B(OrcCampBuildingKind.BrutePit, 8, -16);
            B(OrcCampBuildingKind.ThrowerRange, 8, -12);
            B(OrcCampBuildingKind.Totem, -5, 0, 1);
            B(OrcCampBuildingKind.Totem, 4, 0, 1);
            B(OrcCampBuildingKind.Treasury, -10, 1);
            B(OrcCampBuildingKind.Storehouse, 8, 1);

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
            // 1: the plaza by the gate
            Patrol(new[] { grunt, grunt, grunt }, (-6.5f, -46.5f), (5.5f, -46.5f), (5.5f, -43.5f), (-6.5f, -43.5f));
            // 2: the runner, down the whole snake and back
            Patrol(new[] { brute, grunt, grunt }, (-7f, -46f), (-7f, -32f), (7f, -32f), (7f, -20f), (-7f, -20f), (-7f, -9f),
                (7f, -9f), (7f, -4.5f), (7f, -9f), (-7f, -9f), (-7f, -20f), (7f, -20f), (7f, -32f), (-7f, -32f));
            // 3: round the middle gap
            Patrol(new[] { grunt, grunt }, (-5f, -24.5f), (-4f, -20f), (4f, -20f), (5.5f, -25f));
            // 4: the sanctum
            Patrol(new[] { brute, brute, grunt }, (-9f, -5f), (9f, -5f), (7f, 3.5f), (-7f, 3.5f));
            // 6: the corridor between the sanctum's walls, out and back
            Patrol(new[] { brute, brute }, (-12.5f, -8.5f), (-12.5f, 6.5f), (12.5f, 6.5f), (12.5f, -8.5f), (12.5f, 6.5f), (-12.5f, 6.5f));
            // 5: at the skull
            Patrol(new[] { grunt, grunt }, (0f, -49.5f), (0f, -60f));

            // two brutes at every gap in the spine, grunts at the fires
            Post(OrcKind.Brute, 2, (-2.2f, -32f), (2.2f, -32f), (-2.2f, -20f), (2.2f, -20f), (-2.2f, -9f), (2.2f, -9f));
            Post(OrcKind.Raider, 3, (-7.6f, -45.3f), (-4.5f, -45.5f), (2.6f, -13.5f), (5.4f, -14.6f));
            Post(OrcKind.Thrower, 1, (-10.5f, -45.5f), (9.5f, -45.5f), (-10.5f, -41f), (9.5f, -41.5f), (-10.5f, -29f), (9.5f, -29f),
                (-10.5f, -17f), (9.5f, -17.5f), (-10.5f, -5.5f), (9.5f, -5.5f), (-12.5f, -5f), (12.5f, -5f), (0.5f, 6.5f));
            Post(OrcKind.Boss, 1, (-4.8f, -13.5f));

            // the spine: its runs of cells, one obstacle each
            var spine = new List<RectInt>();
            for (var z = -41; z <= -8; z++)
            {
                if (!SpineCell(0, z)) continue;
                var z0 = z;
                while (SpineCell(0, z + 1)) z++;
                spine.Add(new RectInt(-1, z0, 2, z - z0 + 1));
            }
            return new CampLayout(L, new RectInt(-15, -48, 30, 57), pieces, guards, spine, 4)
            {
                BogMountains = true,
                Apron = new RectInt(-5, -62, 10, 14),
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
