using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Maps
{
    /// <summary>
    /// A coarse road map over a dungeon's NavMesh, for the long ways round (the user, 2026-11-04,
    /// Dungeon 2: «нельзя пройти в левый угол», «герой входит в мост и не может пройти дальше»):
    /// a labyrinth this big has thousands of NavMesh polygons, and NavMesh.CalculatePath's search
    /// runs out of nodes on a long detour -- it hands back a PARTIAL path ending in a dead end near
    /// the target, and every re-plan from there finds the same dead end.
    ///
    /// So when a route comes back partial, NavRoute asks here: nodes laid every NodeSpacing cells
    /// and snapped onto the NavMesh, joined to their neighbours by short paths found once at load
    /// (each one well within the search's reach), Dijkstra over them, and the legs' own corners
    /// strung together. A short route never comes here.
    ///
    /// The lever door (DungeonLevers) shuts the ways that pass its bars until it opens: Gate.
    /// Mobile: built once on the loading screen (~2k short path queries); a route that needs it
    /// costs a Dijkstra over a few hundred nodes and a few path queries.
    /// </summary>
    public static class DungeonWaypoints
    {
        private const float NodeSpacing = 6f;
        private const float SnapRadius = 3.5f;
        private const float MergeDistance = 1.2f;
        private const float LinkRadius = NodeSpacing * 1.6f;
        private const float LinkDetour = 1.6f;       // a link's path may be at most this many times the straight line
        private const int EndCandidates = 6;

        private class Edge
        {
            public int To;
            public float Cost;
            public Vector3[] Corners;   // from this node to To, both ends included
            public bool Gated;
        }

        private static List<Vector3> _nodes;
        private static List<List<Edge>> _edges;
        private static NavMeshPath _path;
        private static Vector3 _gateAt;
        private static float _gateRadius;
        private static bool _gateShut;

        public static bool Active => _nodes != null && _nodes.Count > 1;
        public static int NodeCount => _nodes != null ? _nodes.Count : 0;

        public static void Clear()
        {
            _nodes = null;
            _edges = null;
            _gateShut = false;
        }

        /// <summary>Lays the nodes over the dungeon's `half` cells round the origin and links them.</summary>
        public static void Build(float half)
        {
            Clear();
            _path ??= new NavMeshPath();
            var nodes = new List<Vector3>();
            for (var z = -half + NodeSpacing * 0.5f; z < half; z += NodeSpacing)
            for (var x = -half + NodeSpacing * 0.5f; x < half; x += NodeSpacing)
            {
                if (!NavMesh.SamplePosition(new Vector3(x, 0f, z), out var hit, SnapRadius, NavMesh.AllAreas)) continue;
                var p = hit.position;
                var near = false;
                foreach (var n in nodes)
                    if ((n - p).sqrMagnitude < MergeDistance * MergeDistance) { near = true; break; }
                if (!near) nodes.Add(p);
            }
            var edges = new List<List<Edge>>(nodes.Count);
            for (var i = 0; i < nodes.Count; i++) edges.Add(new List<Edge>());
            for (var i = 0; i < nodes.Count; i++)
            for (var j = i + 1; j < nodes.Count; j++)
            {
                var straight = Flat(nodes[i], nodes[j]);
                if (straight > LinkRadius) continue;
                if (!NavMesh.CalculatePath(nodes[i], nodes[j], NavMesh.AllAreas, _path) || _path.status != NavMeshPathStatus.PathComplete) continue;
                var corners = _path.corners;
                var length = Length(corners);
                if (length > Mathf.Max(straight, 1f) * LinkDetour + 1f) continue;
                edges[i].Add(new Edge { To = j, Cost = length, Corners = corners });
                var back = (Vector3[])corners.Clone();
                System.Array.Reverse(back);
                edges[j].Add(new Edge { To = i, Cost = length, Corners = back });
            }
            _nodes = nodes;
            _edges = edges;
        }

        /// <summary>The ways within `radius` of `at` are shut (a barred door) until Ungate.</summary>
        public static void Gate(Vector3 at, float radius)
        {
            _gateAt = at;
            _gateRadius = radius;
            _gateShut = true;
            if (_edges == null) return;
            foreach (var list in _edges)
                foreach (var e in list)
                    e.Gated = Passes(e.Corners, at, radius);
        }

        public static void Ungate()
        {
            _gateShut = false;
            if (_edges == null) return;
            foreach (var list in _edges)
                foreach (var e in list) e.Gated = false;
        }

        /// <summary>A way from `from` to `to` over the road map, as corners; false if there is none.</summary>
        public static bool TryRoute(Vector3 from, Vector3 to, int areaMask, out Vector3[] corners)
        {
            corners = null;
            if (!Active) return false;
            _path ??= new NavMeshPath();
            var starts = Ends(from, areaMask, false);
            var goals = Ends(to, areaMask, true);
            if (starts.Count == 0 || goals.Count == 0) return false;

            var count = _nodes.Count;
            var dist = new float[count];
            var prev = new int[count];
            var done = new bool[count];
            for (var i = 0; i < count; i++) { dist[i] = float.MaxValue; prev[i] = -1; }
            foreach (var s in starts) dist[s.Node] = s.Cost;
            var goalCost = new Dictionary<int, (float cost, Vector3[] corners)>();
            foreach (var g in goals) goalCost[g.Node] = (g.Cost, g.Corners);
            var best = float.MaxValue;
            var bestGoal = -1;
            while (true)
            {
                var u = -1;
                var du = float.MaxValue;
                for (var i = 0; i < count; i++)
                    if (!done[i] && dist[i] < du) { du = dist[i]; u = i; }
                if (u < 0 || du >= best) break;
                done[u] = true;
                if (goalCost.TryGetValue(u, out var g) && du + g.cost < best) { best = du + g.cost; bestGoal = u; }
                foreach (var e in _edges[u])
                {
                    if (e.Gated || done[e.To]) continue;
                    var d = du + e.Cost;
                    if (d < dist[e.To]) { dist[e.To] = d; prev[e.To] = u; }
                }
            }
            if (bestGoal < 0) return false;

            var chain = new List<int>();
            for (var n = bestGoal; n >= 0; n = prev[n]) chain.Add(n);
            chain.Reverse();
            var list = new List<Vector3>();
            Vector3[] first = null;
            foreach (var s in starts) if (s.Node == chain[0]) first = s.Corners;
            Append(list, first);
            for (var k = 0; k + 1 < chain.Count; k++)
                foreach (var e in _edges[chain[k]])
                    if (e.To == chain[k + 1]) { Append(list, e.Corners); break; }
            Append(list, goalCost[bestGoal].corners);
            corners = list.ToArray();
            return corners.Length > 0;
        }

        private struct End
        {
            public int Node;
            public float Cost;
            public Vector3[] Corners;   // from the point to the node, or from the node to the point
        }

        private static readonly List<int> _candidates = new List<int>();

        private static List<End> Ends(Vector3 point, int areaMask, bool toPoint)
        {
            var found = new List<End>();
            _candidates.Clear();
            for (var i = 0; i < _nodes.Count; i++)
                if (Flat(_nodes[i], point) <= LinkRadius) _candidates.Add(i);
            _candidates.Sort((a, b) => Flat(_nodes[a], point).CompareTo(Flat(_nodes[b], point)));
            foreach (var i in _candidates)
            {
                if (found.Count >= EndCandidates) break;
                var a = toPoint ? _nodes[i] : point;
                var b = toPoint ? point : _nodes[i];
                if (!NavMesh.CalculatePath(a, b, areaMask, _path) || _path.status != NavMeshPathStatus.PathComplete) continue;
                var corners = _path.corners;
                if (_gateShut && Passes(corners, _gateAt, _gateRadius)) continue;
                found.Add(new End { Node = i, Cost = Length(corners), Corners = corners });
            }
            return found;
        }

        private static void Append(List<Vector3> list, Vector3[] corners)
        {
            if (corners == null) return;
            foreach (var c in corners)
                if (list.Count == 0 || (list[list.Count - 1] - c).sqrMagnitude > 0.0001f) list.Add(c);
        }

        private static bool Passes(Vector3[] corners, Vector3 at, float radius)
        {
            for (var i = 0; i + 1 < corners.Length; i++)
            {
                var a = new Vector2(corners[i].x, corners[i].z);
                var b = new Vector2(corners[i + 1].x, corners[i + 1].z);
                var p = new Vector2(at.x, at.z);
                var ab = b - a;
                var t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
                if ((a + ab * t - p).sqrMagnitude < radius * radius) return true;
            }
            return false;
        }

        private static float Length(Vector3[] corners)
        {
            var total = 0f;
            for (var i = 0; i + 1 < corners.Length; i++) total += Flat(corners[i], corners[i + 1]);
            return total;
        }

        private static float Flat(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
