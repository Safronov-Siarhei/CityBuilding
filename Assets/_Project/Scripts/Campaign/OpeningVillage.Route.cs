using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Mission 1's way out of the village, laid by the map the user approved (2026-11-11,
    /// Design/mission1_map_v1.png): south down the market street, a dirt track through a field of
    /// tall wheat and a forest (a fallen tree across it), a river crossed by the sandbox's own bridge
    /// -- three pieces, the river as wide as they are, the middle one the mage brings down -- and the
    /// town's wall: the sandbox's level-3 Barbican, Fence and wall towers (the user: the made-for-the-
    /// film bridge and castle were rejected). Everything is in Campaign/Mission1.json "route".
    ///
    /// The route's ground is reserved right after the streets, so the village's houses, fields,
    /// trees and decor keep out of it; the route itself is laid last.
    /// </summary>
    public partial class OpeningVillage
    {
        [Serializable] private class RouteFile { public Route route; }

        [Serializable] private class Route
        {
            public float[] path;
            public int pathFrom;
            public float laneWidth, wiggle;
            public FieldConfig field;
            public ForestConfig forest;
            public FallenConfig fallenTree;
            public RiverConfig river;
            public BridgeConfig bridge;
            public TownConfig town;
        }

        [Serializable] private class FieldConfig { public float[] poly, cartTrack; public float tile, cell, earsBand, height, cartWidth, sway, swayStrength, swayHeight; }
        [Serializable] private class ForestConfig { public float[] min, max; public float treeEvery, clearPath, bushEvery; }
        [Serializable] private class FallenConfig { public float along, offset, clear; }
        [Serializable] private class RiverConfig { public float[] line; public float width, bank, wiggle; }
        [Serializable] private class BridgeConfig { public string model, steps; public float x, z, piece, yaw, deck, ramp, stepsYaw; public int pieces; }

        [Serializable] private class TownConfig
        {
            public float wallZ, gateX, piece, passage, gateYaw, fenceYaw, towerYaw, depth, houseEvery, width;
            public string gate, fence, tower;
            public int fencePieces;
            public string[] houses;
        }

        [SerializeField] private TextAsset routeFile;
        [SerializeField] private Prop[] routeModels = new Prop[0];
        [SerializeField] private Material water;
        [SerializeField] private Material wheatBody;

        private Route _route;
        private readonly List<Rect> _routeAreas = new List<Rect>();
        private readonly List<Transform> _bridgePieces = new List<Transform>();
        private Vector2[] _path;
        /// <summary>The track as laid (smoothed, meandering): what the wheat and the trees keep off.</summary>
        private Vector2[] _track;
        /// <summary>The field's cart track, across it (the orcs' beat), as laid.</summary>
        private Vector2[] _cart;
        private Vector2[] _fieldPoly;
        private float _bridgeZ;

        /// <summary>The route's track, village frame (x, z), from the square to the town gate.</summary>
        public IReadOnlyList<Vector2> RoutePath => _path;

        /// <summary>The track as laid, from the end of the market street (null before the route is built).</summary>
        public IReadOnlyList<Vector2> RouteTrack => _track;

        /// <summary>The bridge's pieces in order across the river; the middle one is the mage's.</summary>
        public IReadOnlyList<Transform> BridgePieces => _bridgePieces;

        /// <summary>The ground's height under a runner: the bridge's deck (ramped at its ends), else 0.</summary>
        public float HeightAt(Vector2 at)
        {
            var b = _route?.bridge;
            if (b == null || _bridgePieces.Count == 0) return 0f;
            var half = b.pieces * b.piece * 0.5f;
            var across = Mathf.Abs(at.x - b.x);
            if (across > b.piece * 0.5f) return 0f;
            var along = Mathf.Abs(at.y - _bridgeZ);
            if (along <= half) return b.deck;
            return along < half + b.ramp ? b.deck * (1f - (along - half) / b.ramp) : 0f;
        }

        /// <summary>The mage's orb took this piece: the gap stops a runner, and the deck's height
        /// ends at its edge.</summary>
        public void BreakBridge(Transform piece)
        {
            var b = _route?.bridge;
            if (b == null || piece == null) return;
            var gap = new GameObject("BridgeGap", typeof(BoxCollider));
            gap.transform.SetParent(_root, false);
            gap.transform.position = new Vector3(b.x, 0.5f + b.deck, piece.position.z);
            gap.GetComponent<BoxCollider>().size = new Vector3(b.piece + 1.4f, 1f + b.deck * 2f, b.piece * 0.9f);
            UnityEngine.Physics.SyncTransforms();
        }

        private void RouteReserve()
        {
            _route = null;
            _routeAreas.Clear();
            _bridgePieces.Clear();
            _path = null;
            if (routeFile == null) return;
            _route = JsonUtility.FromJson<RouteFile>(routeFile.text)?.route;
            if (_route == null || _route.path == null) return;
            _path = Points(_route.path);
            _track = null;
            _way = null;
            if (_route.bridge != null) _pins.Add((new Vector2(_route.bridge.x, _route.bridge.z), _route.bridge.pieces * _route.bridge.piece * 0.5f + 2f));
            if (_route.town != null) _pins.Add((new Vector2(_route.town.gateX, _route.town.wallZ), 3f));
            if (_route.field?.poly != null) _routeAreas.Add(Bounds(Points(_route.field.poly), 1f));
            if (_route.forest?.min != null)
                _routeAreas.Add(Rect.MinMaxRect(_route.forest.min[0], _route.forest.min[1], _route.forest.max[0], _route.forest.max[1]));
            if (_route.river?.line != null) _routeAreas.Add(Bounds(Points(_route.river.line), _route.river.width * 0.5f + _route.river.bank + 1f));
            var t = _route.town;
            if (t != null) _routeAreas.Add(Rect.MinMaxRect(t.gateX - t.width * 0.5f, t.wallZ - t.depth, t.gateX + t.width * 0.5f, t.wallZ + 3f));
            // the track itself, from where the market street ends
            for (var i = Mathf.Max(0, _route.pathFrom); i + 1 < _path.Length; i++)
                _routeAreas.Add(Bounds(new[] { _path[i], _path[i + 1] }, _route.laneWidth));
        }

        private bool InRoute(Vector2 at, float r)
        {
            foreach (var a in _routeAreas)
                if (at.x + r > a.xMin && at.x - r < a.xMax && at.y + r > a.yMin && at.y - r < a.yMax) return true;
            return false;
        }

        private void RouteBuild()
        {
            if (_route == null) return;
            var track = new List<Vector2>();
            for (var i = Mathf.Max(0, _route.pathFrom); i < _path.Length; i++) track.Add(_path[i]);
            if (track.Count > 1) _track = Street(track.ToArray(), _route.laneWidth, dirt, false, -1f, _route.wiggle);
            _cart = _route.field?.cartTrack != null ? Street(Points(_route.field.cartTrack), _route.field.cartWidth, dirt, false, -1f, 0.5f) : null;
            River();
            Bridge();
            WheatField();
            Forest();
            Town();
        }

        // ------------------------------------------------------------ the wheat

        /// <summary>
        /// One slab for the body of the field, and the ear tiles stood on it only within earsBand of
        /// the track -- a whole field of ears would be far too many triangles for a phone, and the
        /// camera never sees that far from the track. The slab's edge follows the track and the
        /// field's outline smoothly (marching squares over "how far inside the wheat", each cell cut
        /// where that crosses zero): square cells left a staircase along a curving track.
        /// </summary>
        private void WheatField()
        {
            var f = _route.field;
            var ears = SetPieceModel("CineWheat");
            if (f?.poly == null || wheatBody == null) return;
            var poly = _fieldPoly = Points(f.poly);
            var box = Bounds(poly, 0f);
            var cell = f.cell > 0f ? f.cell : f.tile;
            var nx = Mathf.CeilToInt(box.width / cell);
            var nz = Mathf.CeilToInt(box.height / cell);
            var clear = _route.laneWidth * 0.5f + 0.15f;
            // how far inside the wheat each grid corner is (negative: outside it)
            var inside = new float[nx + 1, nz + 1];
            for (var i = 0; i <= nx; i++)
                for (var j = 0; j <= nz; j++)
                {
                    var c = new Vector2(box.xMin + i * cell, box.yMin + j * cell);
                    inside[i, j] = Mathf.Min(Mathf.Min(PathDistance(c) - clear, CartDistance(c) - (f.cartWidth * 0.5f + 0.15f)), PolySigned(poly, c));
                }
            var v = new List<Vector3>();
            var tri = new List<int>();
            var h = f.height;
            var corner = new Vector2[4];
            var value = new float[4];
            var ring = new List<Vector2>(8);
            var cut = new List<Vector2>(4);
            for (var i = 0; i < nx; i++)
            {
                for (var j = 0; j < nz; j++)
                {
                    corner[0] = new Vector2(box.xMin + i * cell, box.yMin + j * cell);
                    corner[1] = corner[0] + new Vector2(cell, 0f);
                    corner[2] = corner[0] + new Vector2(cell, cell);
                    corner[3] = corner[0] + new Vector2(0f, cell);
                    value[0] = inside[i, j];
                    value[1] = inside[i + 1, j];
                    value[2] = inside[i + 1, j + 1];
                    value[3] = inside[i, j + 1];
                    if (value[0] <= 0f && value[1] <= 0f && value[2] <= 0f && value[3] <= 0f) continue;
                    // the cell clipped to the wheat: corners inside, and where an edge crosses out
                    ring.Clear();
                    cut.Clear();
                    for (var k = 0; k < 4; k++)
                    {
                        var n = (k + 1) % 4;
                        if (value[k] > 0f) ring.Add(corner[k]);
                        if ((value[k] > 0f) != (value[n] > 0f))
                        {
                            var q = Vector2.Lerp(corner[k], corner[n], value[k] / (value[k] - value[n]));
                            ring.Add(q);
                            cut.Add(q);
                        }
                    }
                    // the top: a fan, wound clockwise from above (the ring runs anticlockwise)
                    var start = v.Count;
                    foreach (var q in ring) v.Add(new Vector3(q.x, h, q.y));
                    for (var k = 1; k + 1 < ring.Count; k++)
                    {
                        tri.Add(start); tri.Add(start + k + 1); tri.Add(start + k);
                    }
                    // the side where the wheat ends, both faces (cheap: only along the edge)
                    for (var k = 0; k + 1 < cut.Count; k += 2)
                    {
                        var a0 = new Vector3(cut[k].x, 0f, cut[k].y);
                        var a1 = new Vector3(cut[k + 1].x, 0f, cut[k + 1].y);
                        // each face its own corners: shared ones got normals that cancelled out (black)
                        var w = v.Count;
                        v.Add(a0); v.Add(a1); v.Add(a1 + Vector3.up * h); v.Add(a0 + Vector3.up * h);
                        tri.Add(w); tri.Add(w + 1); tri.Add(w + 2); tri.Add(w); tri.Add(w + 2); tri.Add(w + 3);
                        w = v.Count;
                        v.Add(a0); v.Add(a1); v.Add(a1 + Vector3.up * h); v.Add(a0 + Vector3.up * h);
                        tri.Add(w); tri.Add(w + 2); tri.Add(w + 1); tri.Add(w); tri.Add(w + 3); tri.Add(w + 2);
                    }
                }
            }
            var earsRoot = new GameObject("WheatEars").transform;
            earsRoot.SetParent(_root, false);
            // the ears: whole tiles, only where the whole tile stands on the body (clear of the
            // track and inside the field) and near enough the track for the camera to see
            // some tiles sway in the wind (the user's second playtest: the ears stood unnaturally
            // still): the buildings' own shader's wind (BuildingDetail _WIND, by height over the
            // tile's foot) on a copy of the ears' material, given to a share `sway` of the tiles
            Material[] windy = null;
            if (ears != null && f.sway > 0f)
            {
                var r = ears.GetComponentInChildren<Renderer>();
                if (r != null && r.sharedMaterials.Length > 0 && r.sharedMaterials[0] != null && r.sharedMaterials[0].HasProperty("_WindStrength"))
                {
                    windy = new Material[r.sharedMaterials.Length];
                    for (var i = 0; i < windy.Length; i++)
                    {
                        windy[i] = new Material(r.sharedMaterials[i]) { name = r.sharedMaterials[i].name + " (wind)" };
                        windy[i].SetFloat("_Wind", 1f);
                        windy[i].EnableKeyword("_WIND");
                        windy[i].SetFloat("_WindStrength", f.swayStrength);
                        windy[i].SetFloat("_WindHeight", f.swayHeight);
                        windy[i].SetFloat("_WindByUV3", 0f);
                    }
                }
            }
            WindyEars = windy;
            SwayingEars = 0;
            if (ears != null)
            {
                var half = f.tile * 0.5f;
                for (var x = box.xMin + half; x < box.xMax - half; x += f.tile)
                {
                    for (var z = box.yMin + half; z < box.yMax - half; z += f.tile)
                    {
                        var c = new Vector2(x, z);
                        var d = PathDistance(c);
                        if (d < clear + half * 1.42f || d > f.earsBand || CartDistance(c) < f.cartWidth * 0.5f + 0.15f + half * 1.42f) continue;
                        if (!Inside(poly, c + new Vector2(-half, -half)) || !Inside(poly, c + new Vector2(half, half)) ||
                            !Inside(poly, c + new Vector2(-half, half)) || !Inside(poly, c + new Vector2(half, -half))) continue;
                        var e = Instantiate(ears, earsRoot);
                        e.name = "Ears";
                        // a quarter turn now and then, so the tiles do not repeat in rows
                        e.transform.SetPositionAndRotation(new Vector3(c.x, 0f, c.y), Quaternion.Euler(0f, 90f * _rnd.Next(4), 0f) * ears.transform.rotation);
                        if (windy != null && Mathf.Repeat(Mathf.Sin(c.x * 12.9898f + c.y * 78.233f) * 43758.55f, 1f) < f.sway)
                        {
                            foreach (var er in e.GetComponentsInChildren<Renderer>()) er.sharedMaterials = windy;
                            SwayingEars++;
                        }
                    }
                }
            }
            var mesh = new Mesh { name = "WheatBody", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(v);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var go = new GameObject("WheatBody", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(_root, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = wheatBody;
        }

        // ------------------------------------------------------------ the forest

        private void Forest()
        {
            var f = _route.forest;
            if (f?.min == null || trees.Length == 0) return;
            // the fallen tree first, across the track; the trees keep clear of both its ends, so
            // there is a way round it whichever end the crown lies at
            Vector2 endA = new Vector2(1e6f, 1e6f), endB = endA;
            var fallen = SetPieceModel("CineFallenTree");
            var fc = _route.fallenTree;
            if (fallen != null && fc != null && PointAlong(fc.along, out var at, out var dir))
            {
                var side = Perp(dir);
                var centre = at + side * fc.offset;
                var go = Instantiate(fallen, _root);
                go.name = "CineFallenTree";
                go.transform.SetPositionAndRotation(new Vector3(centre.x, 0f, centre.y),
                    Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.y)) * fallen.transform.rotation);
                CineTwoSided.Apply(go);
                _solids.Add(go.transform);
                endA = centre + side * 1.6f;
                endB = centre - side * 1.6f;
            }
            var clearAround = fc != null ? fc.clear : 0f;
            var step = f.treeEvery;
            for (var x = f.min[0]; x < f.max[0]; x += step)
            {
                for (var z = f.min[1]; z < f.max[1]; z += step)
                {
                    var p = new Vector2(x + ((float)_rnd.NextDouble() - 0.5f) * step * 0.8f, z + ((float)_rnd.NextDouble() - 0.5f) * step * 0.8f);
                    if (PathDistance(p) < f.clearPath || (p - endA).magnitude < clearAround || (p - endB).magnitude < clearAround) continue;
                    var model = trees[_rnd.Next(trees.Length)];
                    var go = Instantiate(model, _root);
                    go.name = "Tree";
                    go.transform.SetPositionAndRotation(new Vector3(p.x, 0f, p.y),
                        Quaternion.Euler(0f, (float)_rnd.NextDouble() * 360f, 0f) * model.transform.rotation);
                    go.transform.localScale = model.transform.localScale * BuildingScale * (0.9f + (float)_rnd.NextDouble() * 0.5f);
                    _trunks.Add(go.transform);
                }
            }
            var bush = SetPieceModel("CineBushClump");
            if (bush == null || f.bushEvery <= 0f) return;
            for (var x = f.min[0]; x < f.max[0]; x += f.bushEvery)
            {
                for (var z = f.min[1]; z < f.max[1]; z += f.bushEvery)
                {
                    var p = new Vector2(x + ((float)_rnd.NextDouble() - 0.5f) * f.bushEvery, z + ((float)_rnd.NextDouble() - 0.5f) * f.bushEvery);
                    if (PathDistance(p) < _route.laneWidth * 0.5f + 0.5f || (p - endA).magnitude < clearAround || (p - endB).magnitude < clearAround) continue;
                    var go = Instantiate(bush, _root);
                    go.name = "Bush";
                    go.transform.SetPositionAndRotation(new Vector3(p.x, 0f, p.y),
                        Quaternion.Euler(0f, (float)_rnd.NextDouble() * 360f, 0f) * bush.transform.rotation);
                }
            }
        }

        // ------------------------------------------------------------ the river and the bridge

        private void River()
        {
            var r = _route.river;
            if (r?.line == null || water == null) return;
            var line = Smooth(Points(r.line), 1.5f, r.wiggle);
            Street(line, r.width + r.bank * 2f, dirt, false, -1f, 0f, false);
            var b = _route.bridge;
            // the bridge stands where the river's centre crosses its x
            if (b != null)
            {
                _bridgeZ = b.z;
                for (var i = 0; i + 1 < line.Length; i++)
                {
                    if ((line[i].x - b.x) * (line[i + 1].x - b.x) > 0f) continue;
                    var u = Mathf.Abs(line[i + 1].x - line[i].x) < 1e-4f ? 0f : (b.x - line[i].x) / (line[i + 1].x - line[i].x);
                    _bridgeZ = Mathf.Lerp(line[i].y, line[i + 1].y, u);
                    break;
                }
            }
            Flat("River", Ribbon(line, r.width, LaneY + 0.006f, 0f), water, line[line.Length - 1] - line[0], 3f);
            for (var i = 0; i + 1 < line.Length; i++)
            {
                var a = line[i];
                var c = line[i + 1];
                var d = (c - a).normalized;
                // the water stops a runner, all but where the bridge spans it
                var len = (c - a).magnitude;
                for (var s = 0f; s < len; s += 1f)
                {
                    var m = a + d * Mathf.Min(len, s + 0.5f);
                    if (b != null && Mathf.Abs(m.x - b.x) < b.piece * 0.5f + 0.55f) continue;
                    var wall = new GameObject("RiverEdge", typeof(BoxCollider));
                    wall.transform.SetParent(_root, false);
                    wall.transform.SetPositionAndRotation(new Vector3(m.x, 0.5f, m.y), Quaternion.LookRotation(new Vector3(d.x, 0f, d.y)));
                    wall.GetComponent<BoxCollider>().size = new Vector3(r.width, 1f, 1.05f);
                }
            }
        }

        private void Bridge()
        {
            var b = _route.bridge;
            var model = RouteModel(b?.model);
            if (model == null) return;
            for (var i = 0; i < b.pieces; i++)
            {
                var z = _bridgeZ + (i - (b.pieces - 1) * 0.5f) * b.piece;
                var go = Instantiate(model, _root);
                go.name = "BridgePiece" + i;
                go.transform.SetPositionAndRotation(new Vector3(b.x, 0f, z), Quaternion.Euler(0f, b.yaw, 0f) * model.transform.rotation);
                go.transform.localScale = model.transform.localScale * BuildingScale;
                CineTwoSided.Apply(go);
                _bridgePieces.Add(go.transform);
            }
            // timber steps down to each bank (the user, 2026-11-11: the deck's ends hung in the air)
            var steps = SetPieceModel(b.steps);
            if (steps == null) return;
            var half = b.pieces * b.piece * 0.5f;
            foreach (var s in new[] { 1f, -1f })
            {
                var go = Instantiate(steps, _root);
                go.name = "BridgeSteps";
                go.transform.SetPositionAndRotation(new Vector3(b.x, 0f, _bridgeZ + s * half),
                    Quaternion.Euler(0f, b.stepsYaw + (s > 0f ? 0f : 180f), 0f) * steps.transform.rotation);
                CineTwoSided.Apply(go);
            }
        }

        // ------------------------------------------------------------ the town

        /// <summary>The Barbican in the middle of the wall (its middle cell the way through, its end
        /// cells walls), fencePieces of the stone wall each side, a wall tower at each end, and
        /// the town's houses behind, a street up from the gate.</summary>
        private void Town()
        {
            var t = _route.town;
            if (t == null) return;
            var gate = RouteModel(t.gate);
            if (gate != null)
            {
                var go = PutRouteModel(gate, new Vector2(t.gateX, t.wallZ), t.gateYaw, "TownGate", false);
                // two posts either side of the passage instead of one box over it
                foreach (var s in new[] { -1f, 1f })
                {
                    var post = new GameObject("GateSide", typeof(BoxCollider));
                    post.transform.SetParent(_root, false);
                    post.transform.position = new Vector3(t.gateX + s * (t.passage * 0.5f + t.piece * 0.5f), 1f, t.wallZ);
                    post.GetComponent<BoxCollider>().size = new Vector3(t.piece, 2f, t.piece);
                }
                _ = go;
            }
            var fence = RouteModel(t.fence);
            var gateHalf = t.piece * 1.5f;
            for (var k = 0; k < t.fencePieces && fence != null; k++)
            {
                foreach (var s in new[] { -1f, 1f })
                    PutRouteModel(fence, new Vector2(t.gateX + s * (gateHalf + (k + 0.5f) * t.piece), t.wallZ), t.fenceYaw, "TownWall", true);
            }
            var tower = RouteModel(t.tower);
            if (tower != null)
            {
                foreach (var s in new[] { -1f, 1f })
                    PutRouteModel(tower, new Vector2(t.gateX + s * (gateHalf + (t.fencePieces + 0.5f) * t.piece), t.wallZ), t.towerYaw, "TownTower", true);
            }
            // the town behind: a cobbled street up from the gate, houses either side
            Street(new[] { new Vector2(t.gateX, t.wallZ + 1f), new Vector2(t.gateX, t.wallZ - t.depth) }, 2.2f, cobble, false);
            if (t.houses == null || t.houses.Length == 0) return;
            var n = 0;
            for (var z = t.wallZ - 3.5f; z > t.wallZ - t.depth; z -= t.houseEvery)
            {
                for (var x = t.gateX - t.width * 0.5f + 2f; x < t.gateX + t.width * 0.5f - 2f; x += t.houseEvery)
                {
                    if (Mathf.Abs(x - t.gateX) < 2.6f) continue;
                    var id = t.houses[n++ % t.houses.Length];
                    var prefab = Prefab(id);
                    if (prefab == null) continue;
                    var go = Strip(prefab);
                    CineTwoSided.Apply(go);
                    go.name = id;
                    go.transform.SetParent(_root, false);
                    var yaw = x < t.gateX ? 90f : -90f;
                    go.transform.SetPositionAndRotation(new Vector3(x, 0f, z), Quaternion.Euler(0f, yaw, 0f) * prefab.transform.rotation);
                    go.transform.localScale = prefab.transform.localScale * BuildingScale;
                    _houses.Add(go.transform);
                    _solids.Add(go.transform);
                }
            }
        }

        private GameObject PutRouteModel(GameObject model, Vector2 at, float yaw, string name, bool solid)
        {
            var go = Instantiate(model, _root);
            go.name = name;
            go.transform.SetPositionAndRotation(new Vector3(at.x, 0f, at.y), Quaternion.Euler(0f, yaw, 0f) * model.transform.rotation);
            go.transform.localScale = model.transform.localScale * BuildingScale;
            CineTwoSided.Apply(go);
            if (solid) _solids.Add(go.transform);
            return go;
        }

        private GameObject RouteModel(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var m in routeModels) if (m.id == id) return m.prefab;
            return null;
        }

        // ------------------------------------------------------------ geometry

        /// <summary>How far a point is from the way (the market street and the laid track).</summary>
        /// <summary>How many ear tiles sway (the batchmode check).</summary>
        public int SwayingEars { get; private set; }

        /// <summary>The ears' swaying material (null without wind): the far bank's wheat sways with it too.</summary>
        public Material[] WindyEars { get; private set; }

        /// <summary>How far from the track the field's ears stand (the darkness keeps to them).</summary>
        public float EarsBand => _route?.field != null ? _route.field.earsBand : 0f;

        public float TrackDistance(Vector2 p) => _route == null || _path == null ? float.MaxValue : PathDistance(p);

        private List<Vector2> _way;

        /// <summary>The way as one line: the market street's raw points, then the laid track.</summary>
        private List<Vector2> Way()
        {
            if (_way != null) return _way;
            var line = new List<Vector2>();
            for (var i = 0; i < _route.pathFrom && i < _path.Length; i++) line.Add(_path[i]);
            if (_track != null) line.AddRange(_track);
            if (_track != null) _way = line;           // cached once the track is laid
            return line;
        }

        /// <summary>The way's point `ahead` metres on from the point of it nearest `p` (the guide arrow).</summary>
        public Vector2 TrackAhead(Vector2 p, float ahead)
        {
            if (_route == null || _path == null) return p;
            var line = Way();
            if (line.Count < 2) return p;
            var at = Nearest(line, p, out _, out var run);
            var want = Mathf.Min(run, at + ahead);
            for (var i = 0; i + 1 < line.Count; i++)
            {
                var l = (line[i + 1] - line[i]).magnitude;
                if (want <= l) return Vector2.Lerp(line[i], line[i + 1], l > 1e-5f ? want / l : 0f);
                want -= l;
            }
            return line[line.Count - 1];
        }

        /// <summary>The way's point nearest `p` and the way's direction there (down the route): the
        /// mission puts its checkpoints, patrols, the dead and the animals ON the road by it (the
        /// user's second playtest: «контрольные точки должны стоять прямо на дороге»).</summary>
        public Vector2 TrackNearest(Vector2 p, out Vector2 dir)
        {
            dir = Vector2.down;
            if (_route == null || _path == null) return p;
            var line = Way();
            if (line.Count < 2) return p;
            var along = Nearest(line, p, out dir, out _);
            for (var i = 0; i + 1 < line.Count; i++)
            {
                var l = (line[i + 1] - line[i]).magnitude;
                if (along <= l || i + 2 == line.Count) return Vector2.Lerp(line[i], line[i + 1], l > 1e-5f ? Mathf.Clamp01(along / l) : 0f);
                along -= l;
            }
            return line[line.Count - 1];
        }

        /// <summary>How far along `line` its point nearest `p` lies; the segment's direction there and the line's length.</summary>
        private static float Nearest(List<Vector2> line, Vector2 p, out Vector2 dir, out float length)
        {
            var best = float.MaxValue;
            var at = 0f;
            var run = 0f;
            dir = Vector2.down;
            for (var i = 0; i + 1 < line.Count; i++)
            {
                var a = line[i];
                var ab = line[i + 1] - a;
                var l = ab.magnitude;
                var t = l > 1e-5f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / (l * l)) : 0f;
                var d = (a + ab * t - p).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    at = run + t * l;
                    if (l > 1e-5f) dir = ab / l;
                }
                run += l;
            }
            length = run;
            return at;
        }

        /// <summary>The field's outline's bounds, and how far inside it a point is (negative outside).</summary>
        public Rect FieldBounds => _fieldPoly != null ? Bounds(_fieldPoly, 0f) : new Rect();
        public float FieldSigned(Vector2 p) => _fieldPoly != null ? PolySigned(_fieldPoly, p) : float.MinValue;

        /// <summary>Any model the village knows by name: the route's (sandbox buildings, fauna) or the film's set pieces.</summary>
        public GameObject Model(string id) => RouteModel(id) ?? SetPieceModel(id);

        private float PathDistance(Vector2 p)
        {
            var best = float.MaxValue;
            for (var i = 0; i < _route.pathFrom && i + 1 < _path.Length; i++)
                best = Mathf.Min(best, DistanceToSegment(p, _path[i], _path[i + 1]));
            var line = _track ?? _path;
            for (var i = 0; i + 1 < line.Length; i++)
                best = Mathf.Min(best, DistanceToSegment(p, line[i], line[i + 1]));
            return best;
        }

        /// <summary>The point `along` metres down the track (from its start) and the track's way there.</summary>
        private bool PointAlong(float along, out Vector2 at, out Vector2 dir)
        {
            at = dir = Vector2.zero;
            // along the market street first (the raw points), then the laid track
            var line = new List<Vector2>();
            for (var i = 0; i <= _route.pathFrom && i < _path.Length; i++) line.Add(_path[i]);
            if (_track != null) for (var i = 1; i < _track.Length; i++) line.Add(_track[i]);
            for (var i = 0; i + 1 < line.Count; i++)
            {
                var seg = (line[i + 1] - line[i]).magnitude;
                if (along <= seg)
                {
                    dir = (line[i + 1] - line[i]).normalized;
                    at = line[i] + dir * along;
                    return true;
                }
                along -= seg;
            }
            return false;
        }

        private static Vector2[] Points(float[] xz)
        {
            var p = new Vector2[xz.Length / 2];
            for (var i = 0; i < p.Length; i++) p[i] = new Vector2(xz[i * 2], xz[i * 2 + 1]);
            return p;
        }

        private static Rect Bounds(Vector2[] pts, float pad)
        {
            Vector2 lo = pts[0], hi = pts[0];
            foreach (var p in pts)
            {
                lo = Vector2.Min(lo, p);
                hi = Vector2.Max(hi, p);
            }
            return Rect.MinMaxRect(lo.x - pad, lo.y - pad, hi.x + pad, hi.y + pad);
        }

        private float CartDistance(Vector2 p)
        {
            if (_cart == null) return float.MaxValue;
            var best = float.MaxValue;
            for (var i = 0; i + 1 < _cart.Length; i++) best = Mathf.Min(best, DistanceToSegment(p, _cart[i], _cart[i + 1]));
            return best;
        }

        /// <summary>Is a point in the standing wheat (in the field, off its tracks)? The orcs do not
        /// see Ардан there unless right beside him.</summary>
        public bool InWheat(Vector2 p)
        {
            var f = _route?.field;
            if (f == null || _fieldPoly == null || !Inside(_fieldPoly, p)) return false;
            return PathDistance(p) > _route.laneWidth * 0.5f + 0.15f && CartDistance(p) > f.cartWidth * 0.5f + 0.15f;
        }

        /// <summary>How far inside the polygon a point is (negative outside).</summary>
        private static float PolySigned(Vector2[] poly, Vector2 p)
        {
            var d = float.MaxValue;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++) d = Mathf.Min(d, DistanceToSegment(p, poly[j], poly[i]));
            return Inside(poly, p) ? d : -d;
        }

        private static bool Inside(Vector2[] poly, Vector2 p)
        {
            var inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x) inside = !inside;
            }
            return inside;
        }
    }
}
