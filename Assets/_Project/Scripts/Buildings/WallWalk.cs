using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Grid;
using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// The walk along the top of the walls (the user, 2026-11-02): a Fence or a Gate from
    /// `wall_walk_from_level` (3) up is thick enough to walk on, and a Tower or a Fortified Tower of
    /// that level standing against such a wall is the stair up -- its model turns into the wall
    /// tower (WallTowerLook) and its cells join the walk. Ranged soldiers and heroes go up there
    /// (Combat/SoldierUnitWalls): one fighter a cell, the rest pass through to free ones.
    ///
    /// A graph of cells, not a NavMesh on top of the walls: who stands where and the way from one
    /// cell to another along the wall. Rebuilt from the buildings standing whenever one of the kinds
    /// above is put up, levelled or lost (MarkDirty) -- rarely, so a scan then is cheap enough.
    /// </summary>
    public static class WallWalk
    {
        public const string FenceId = "Fence";
        public const string GateId = "Gate";
        public const string TowerId = "Tower";
        public const string FortifiedTowerId = "FortifiedTower";
        public const string BarbicanId = "FortifiedGate";

        private enum Kind { Wall, Tower }

        private struct Node
        {
            public Kind Kind;
            public BuildingInstance Building;
            public float Top;
            public int Level;
        }

        /// <summary>The steps' models (WallSteps-3-4, -3-5, -4-5), set by BuildingPlacer.</summary>
        public static GameObject[] StepModels;

        // the steps standing now: on the lower cell, turned toward the higher neighbour
        private static readonly Dictionary<(Vector2Int, Vector2Int), GameObject> StepsShown = new Dictionary<(Vector2Int, Vector2Int), GameObject>();
        private static readonly HashSet<(Vector2Int, Vector2Int)> StepsWanted = new HashSet<(Vector2Int, Vector2Int)>();

        private static readonly Dictionary<Vector2Int, Node> Nodes = new Dictionary<Vector2Int, Node>();
        private static readonly Dictionary<Vector2Int, Combat.SoldierUnit> Posts = new Dictionary<Vector2Int, Combat.SoldierUnit>();
        private static readonly Dictionary<BuildingInstance, List<Vector2Int>> TowerJoins = new Dictionary<BuildingInstance, List<Vector2Int>>();
        private static readonly Queue<Vector2Int> Frontier = new Queue<Vector2Int>();
        private static readonly Dictionary<Vector2Int, Vector2Int> CameFrom = new Dictionary<Vector2Int, Vector2Int>();
        private static readonly Vector2Int[] Steps = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        // the walk cells a stair leads to (stretches without a tower are no use to anyone)
        private static readonly HashSet<Vector2Int> Reachable = new HashSet<Vector2Int>();
        private static bool _dirty = true;

        // the siege towers standing at walls (the user, 2026-11-05): each a stair onto its wall cell,
        // over a low fence a gangway of its own
        private struct DockInfo
        {
            public Vector2Int Tower;
            public Vector2Int Wall;
            public float Top;
        }

        // keyed by the tower: the player's (a SoldierUnit) or campaign level 2's orc tower (an OrcUnit) -- both a stair
        private static readonly Dictionary<Object, DockInfo> Docks = new Dictionary<Object, DockInfo>();

        /// <summary>A siege tower docked at `wall` from `tower` (cells); `top` the gangway's height over a low fence.</summary>
        public static void Dock(Object who, Vector2Int tower, Vector2Int wall, float top)
        {
            Docks[who] = new DockInfo { Tower = tower, Wall = wall, Top = top };
            _dirty = true;
        }

        public static void Undock(Object who)
        {
            if (Docks.Remove(who)) _dirty = true;
        }

        /// <summary>That cell is where a siege tower stands docked (another than `except`), or `except`'s own dock still stands at a wall.</summary>
        public static bool IsDockCell(Vector2Int cell, Combat.SoldierUnit except)
        {
            foreach (var pair in Docks)
            {
                if (pair.Key == null || pair.Value.Tower != cell) continue;
                if (pair.Key != except) return true;
                // its own: alive while the wall it leans on stands
                return Nodes.ContainsKey(pair.Value.Wall) || WallStandsAt(pair.Value.Wall);
            }
            return false;
        }

        /// <summary>That stair cell is a docked siege tower, not a stair tower of the wall's own.</summary>
        public static bool IsDockStair(Vector2Int cell)
        {
            foreach (var pair in Docks) if (pair.Key != null && pair.Value.Tower == cell) return true;
            return false;
        }

        /// <summary>A docked siege tower's stair leads onto this building's walk: a tap on it sends fighters up rather than at it.</summary>
        public static bool ReachedByDock(BuildingInstance b)
        {
            if (b == null || Docks.Count == 0) return false;
            Ensure();
            var cells = new HashSet<Vector2Int>(b.Cells());
            foreach (var pair in Docks)
            {
                if (pair.Key == null || !Nodes.ContainsKey(pair.Value.Tower)) continue;
                if (Walk(pair.Value.Tower, c => cells.Contains(c))) return true;
            }
            return false;
        }

        private static bool WallStandsAt(Vector2Int cell)
        {
            foreach (var b in Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
            {
                if (b == null || b.Data == null || b.CurrentHealth <= 0) continue;
                var id = b.Data.buildingName;
                if (id != FenceId && id != GateId && id != BarbicanId) continue;
                foreach (var c in b.Cells()) if (c == cell) return true;
            }
            return false;
        }

        /// <summary>Counts the rebuilds, so a walker can tell its plan is stale.</summary>
        public static int Version { get; private set; }

        /// <summary>Whether a building of this id takes part in the walk at all.</summary>
        public static bool Concerns(string id) => id == FenceId || id == GateId || id == TowerId || id == FortifiedTowerId || id == BarbicanId;

        public static void MarkDirty() => _dirty = true;

        private static int FromLevel => BalanceConfig.Instance != null ? BalanceConfig.Instance.WallWalkFromLevel : 3;

        /// <summary>Height of the walk above the ground on a wall of `level` (cells = metres here).</summary>
        public static float HeightFor(int level) => BalanceConfig.Instance != null ? BalanceConfig.Instance.WallWalkHeight(level) : 0.9f + 0.2f * (level - 3);

        public static bool Any
        {
            get
            {
                Ensure();
                return Nodes.Count > 0;
            }
        }

        public static void Ensure()
        {
            if (!_dirty) return;
            _dirty = false;
            Rebuild();
        }

        private static void Rebuild()
        {
            Nodes.Clear();
            Reachable.Clear();
            TowerJoins.Clear();
            var grid = GridManager.Instance;
            var towers = new List<BuildingInstance>();
            if (grid != null)
            {
                var ground = grid.GroundHeight;
                foreach (var b in Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
                {
                    if (b == null || b.Data == null || b.Level < FromLevel) continue;
                    var id = b.Data.buildingName;
                    if (id == TowerId || id == FortifiedTowerId)
                    {
                        towers.Add(b);
                        continue;
                    }
                    if (id == BarbicanId)
                    {
                        // the Barbican (the user, 2026-11-02): the walk over its passage, and a stair
                        // in each end tower -- a way up of its own, doorways at the walk's height
                        var cells = new HashSet<Vector2Int>(b.Cells());
                        foreach (var cell in cells)
                        {
                            var inner = 0;
                            foreach (var step in Steps) if (cells.Contains(cell + step)) inner++;
                            Nodes[cell] = new Node { Kind = inner >= 2 ? Kind.Wall : Kind.Tower, Building = b, Top = ground + HeightFor(b.Level), Level = b.Level };
                        }
                        continue;
                    }
                    if (id != FenceId && id != GateId) continue;
                    foreach (var cell in b.Cells())
                        Nodes[cell] = new Node { Kind = Kind.Wall, Building = b, Top = ground + HeightFor(b.Level), Level = b.Level };
                }
                // a tower is the stair up only where a walk wall meets it
                foreach (var t in towers)
                {
                    List<Vector2Int> joins = null;
                    var own = new HashSet<Vector2Int>(t.Cells());
                    foreach (var cell in own)
                    foreach (var step in Steps)
                    {
                        var next = cell + step;
                        if (own.Contains(next) || !Nodes.TryGetValue(next, out var n) || n.Kind != Kind.Wall) continue;
                        (joins ??= new List<Vector2Int>()).Add(next);
                    }
                    if (joins == null) continue;
                    TowerJoins[t] = joins;
                    // inside, the stair reaches the highest wall it opens onto
                    var top = 0f;
                    foreach (var j in joins) top = Mathf.Max(top, Nodes[j].Top);
                    foreach (var cell in own)
                        Nodes[cell] = new Node { Kind = Kind.Tower, Building = t, Top = top, Level = t.Level };
                }
            }
            // the docked siege towers: a stair, and over a low fence the gangway onto it
            if (grid != null && Docks.Count > 0)
            {
                var standing = new HashSet<Vector2Int>();
                foreach (var b in Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
                {
                    if (b == null || b.Data == null || b.CurrentHealth <= 0) continue;
                    var id = b.Data.buildingName;
                    if (id == FenceId || id == GateId || id == BarbicanId) foreach (var c in b.Cells()) standing.Add(c);
                }
                foreach (var pair in Docks)
                {
                    if (pair.Key == null) continue;
                    var dock = pair.Value;
                    if (!Nodes.TryGetValue(dock.Wall, out var wallNode))
                    {
                        if (!standing.Contains(dock.Wall)) continue;
                        wallNode = new Node { Kind = Kind.Wall, Top = dock.Top, Level = 0 };
                        Nodes[dock.Wall] = wallNode;
                    }
                    if (!Nodes.ContainsKey(dock.Tower)) Nodes[dock.Tower] = new Node { Kind = Kind.Tower, Top = wallNode.Top, Level = 0 };
                }
            }
            Reachable.Clear();
            foreach (var pair in Nodes)
            {
                if (pair.Value.Kind != Kind.Tower || Reachable.Contains(pair.Key)) continue;
                Walk(pair.Key, c =>
                {
                    Reachable.Add(c);
                    return false;
                });
            }
            PlaceSteps();
            // posts on cells that are gone are no posts
            var lost = new List<Vector2Int>();
            foreach (var post in Posts)
                if (!Nodes.ContainsKey(post.Key) || post.Value == null) lost.Add(post.Key);
            foreach (var cell in lost) Posts.Remove(cell);
            Version++;
            WallTowerLook.RefreshAll();
        }

        /// <summary>
        /// Steps wherever two walk walls of different levels meet (the user, 2026-11-02: fighters
        /// must not float through the stone): on the lower cell, rising to the higher one's edge.
        /// Made and removed as walls change, parented to the lower wall so they go with it.
        /// </summary>
        private static void PlaceSteps()
        {
            StepsWanted.Clear();
            foreach (var pair in Nodes)
            {
                if (pair.Value.Kind != Kind.Wall) continue;
                foreach (var step in Steps)
                {
                    if (!Nodes.TryGetValue(pair.Key + step, out var next) || next.Kind != Kind.Wall) continue;
                    if (next.Level > pair.Value.Level && StepModel(pair.Value.Level, next.Level) != null) StepsWanted.Add((pair.Key, step));
                }
            }
            var gone = new List<(Vector2Int, Vector2Int)>();
            foreach (var shown in StepsShown)
            {
                if (StepsWanted.Contains(shown.Key) && shown.Value != null) continue;
                if (shown.Value != null) Object.Destroy(shown.Value);
                gone.Add(shown.Key);
            }
            foreach (var key in gone) StepsShown.Remove(key);
            var grid = GridManager.Instance;
            if (grid == null) return;
            foreach (var key in StepsWanted)
            {
                if (StepsShown.ContainsKey(key)) continue;
                var low = Nodes[key.Item1];
                var model = StepModel(low.Level, Nodes[key.Item1 + key.Item2].Level);
                var at = grid.GetFootprintCenterWorld(key.Item1, Vector2Int.one);
                at.y = grid.GroundHeight;
                // modelled rising north; a step of +90 turns north to east
                var yaw = key.Item2 == Vector2Int.up ? 0f : key.Item2 == Vector2Int.right ? 90f : key.Item2 == Vector2Int.down ? 180f : -90f;
                var piece = Object.Instantiate(model, at, Quaternion.Euler(0f, yaw, 0f) * model.transform.rotation, low.Building.transform);
                piece.name = "WallSteps";
                StepsShown[key] = piece;
            }
        }

        private static GameObject StepModel(int low, int high)
        {
            if (StepModels == null) return null;
            var i = low == 3 && high == 4 ? 0 : low == 3 && high >= 5 ? 1 : low == 4 && high >= 5 ? 2 : -1;
            return i >= 0 && i < StepModels.Length ? StepModels[i] : null;
        }

        /// <summary>The level of the wall on a walk cell (a stair: its tower's), 0 off the walk.</summary>
        public static int LevelAt(Vector2Int cell)
        {
            Ensure();
            return Nodes.TryGetValue(cell, out var n) ? n.Level : 0;
        }

        /// <summary>The wall cells a tower joins (its doorways onto the walk), or null when it is no stair.</summary>
        public static List<Vector2Int> JoinsOf(BuildingInstance tower)
        {
            Ensure();
            return TowerJoins.TryGetValue(tower, out var joins) ? joins : null;
        }

        public static bool IsWalk(Vector2Int cell)
        {
            Ensure();
            return Nodes.ContainsKey(cell);
        }

        /// <summary>Every stair (tower) cell of the walk, into `into` (cleared first).</summary>
        public static void StairsInto(List<Vector2Int> into)
        {
            Ensure();
            into.Clear();
            foreach (var pair in Nodes)
                if (pair.Value.Kind == Kind.Tower) into.Add(pair.Key);
        }

        public static bool IsStair(Vector2Int cell)
        {
            Ensure();
            return Nodes.TryGetValue(cell, out var n) && n.Kind == Kind.Tower;
        }

        /// <summary>The middle of a walk cell's top, in the world.</summary>
        public static Vector3 Top(Vector2Int cell)
        {
            Ensure();
            var grid = GridManager.Instance;
            var at = grid != null ? grid.GetFootprintCenterWorld(cell, Vector2Int.one) : Vector3.zero;
            at.y = Nodes.TryGetValue(cell, out var n) ? n.Top : at.y;
            return at;
        }

        public static bool IsWalkBuilding(BuildingInstance b)
        {
            if (b == null) return false;
            Ensure();
            foreach (var cell in b.Cells()) return Nodes.ContainsKey(cell);
            return false;
        }

        /// <summary>The way along the walk from one cell to another, both included, into `path`; false when they are not joined.</summary>
        public static bool Path(Vector2Int from, Vector2Int to, List<Vector2Int> path)
        {
            Ensure();
            path.Clear();
            if (!Nodes.ContainsKey(from) || !Nodes.ContainsKey(to)) return false;
            CameFrom.Clear();
            Frontier.Clear();
            Frontier.Enqueue(from);
            CameFrom[from] = from;
            while (Frontier.Count > 0)
            {
                var c = Frontier.Dequeue();
                if (c == to) break;
                foreach (var step in Steps)
                {
                    var next = c + step;
                    if (!Nodes.ContainsKey(next) || CameFrom.ContainsKey(next)) continue;
                    CameFrom[next] = c;
                    Frontier.Enqueue(next);
                }
            }
            if (!CameFrom.ContainsKey(to)) return false;
            for (var c = to; ; c = CameFrom[c])
            {
                path.Add(c);
                if (c == from) break;
            }
            path.Reverse();
            return true;
        }

        /// <summary>The stair (a tower cell) joined to `cell` that is nearest to `from` in the world; false when its stretch has none.</summary>
        public static bool NearestStair(Vector2Int cell, Vector3 from, out Vector2Int stair)
        {
            Ensure();
            stair = default;
            if (!Nodes.ContainsKey(cell)) return false;
            var grid = GridManager.Instance;
            var best = float.MaxValue;
            var found = false;
            var chosen = default(Vector2Int);
            Walk(cell, c =>
            {
                if (Nodes[c].Kind != Kind.Tower) return false;
                var d = grid != null ? (grid.GetFootprintCenterWorld(c, Vector2Int.one) - from).sqrMagnitude : 0f;
                if (d < best)
                {
                    best = d;
                    chosen = c;
                    found = true;
                }
                return false;
            });
            stair = chosen;
            return found;
        }

        /// <summary>The free wall cell (not a stair) nearest along the walk to `cell`; `who` may keep its own.</summary>
        public static bool FreeCellNear(Vector2Int cell, Combat.SoldierUnit who, out Vector2Int free)
        {
            Ensure();
            var result = default(Vector2Int);
            var found = Nodes.ContainsKey(cell) && Walk(cell, c =>
            {
                if (Nodes[c].Kind != Kind.Wall) return false;
                if (Posts.TryGetValue(c, out var holder) && holder != null && holder != who && holder.CurrentHealth > 0) return false;
                result = c;
                return true;
            });
            free = result;
            return found;
        }

        /// <summary>
        /// For a fighter seeking the walls on his own: the free wall cell nearest to `toward` (the
        /// orc he means to shoot) among those within `reach` of `from` whose stretch has a stair.
        /// </summary>
        public static bool BestPost(Vector3 from, Vector3 toward, float reach, Combat.SoldierUnit who, out Vector2Int post)
        {
            Ensure();
            post = default;
            var grid = GridManager.Instance;
            if (grid == null || Nodes.Count == 0) return false;
            var best = float.MaxValue;
            var found = false;
            var reachSq = reach * reach;
            foreach (var pair in Nodes)
            {
                if (pair.Value.Kind != Kind.Wall) continue;
                if (Posts.TryGetValue(pair.Key, out var holder) && holder != null && holder != who && holder.CurrentHealth > 0) continue;
                var at = grid.GetFootprintCenterWorld(pair.Key, Vector2Int.one);
                var mine = at - from;
                mine.y = 0f;
                if (mine.sqrMagnitude > reachSq) continue;
                var to = at - toward;
                to.y = 0f;
                var d = to.sqrMagnitude;
                if (d >= best || !Reachable.Contains(pair.Key)) continue;
                best = d;
                post = pair.Key;
                found = true;
            }
            return found;
        }

        /// <summary>Takes a cell for a fighter to stand on (giving up any other he held).</summary>
        public static void Claim(Vector2Int cell, Combat.SoldierUnit who)
        {
            Release(who);
            Posts[cell] = who;
        }

        public static void Release(Combat.SoldierUnit who)
        {
            Vector2Int? held = null;
            foreach (var post in Posts)
                if (post.Value == who) held = post.Key;
            if (held.HasValue) Posts.Remove(held.Value);
        }

        /// <summary>Breadth-first along the walk from `start`; stops when `visit` says so (true).</summary>
        private static bool Walk(Vector2Int start, System.Func<Vector2Int, bool> visit)
        {
            CameFrom.Clear();
            Frontier.Clear();
            Frontier.Enqueue(start);
            CameFrom[start] = start;
            while (Frontier.Count > 0)
            {
                var c = Frontier.Dequeue();
                if (visit(c)) return true;
                foreach (var step in Steps)
                {
                    var next = c + step;
                    if (!Nodes.ContainsKey(next) || CameFrom.ContainsKey(next)) continue;
                    CameFrom[next] = c;
                    Frontier.Enqueue(next);
                }
            }
            return false;
        }
    }
}
