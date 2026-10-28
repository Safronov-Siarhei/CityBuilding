using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Grid;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// What a building under construction looks like (the user, 2026-10-28, with a photo of a
    /// house under renovation): the building wrapped in a green construction net hanging between
    /// the scaffolding's poles, the scaffolding round it; when the time is up the dust finish
    /// (BuildFinishEffect) takes the site away and the building stands. Nothing is revealed bit by
    /// bit (rejected as risky).
    ///
    /// An upgrade (the user's pick В, same day): the building in plain sight, scaffolding round it
    /// without the net, a hoist on top lifting a pallet of stones up and down a gap in the front
    /// decks (ConstructionHoist); when it is done a lighter finish than a new building's
    /// (BuildFinishEffect.PlayUpgrade: gold sparks up the walls, a little dust, a nod).
    ///
    /// All of it is made here from the footprint and the model's own size, the same for every
    /// building, and never leaves the building's own cells, so it cannot run into a neighbour
    /// (the user's worry about upgrades). Two meshes per site (wood, net; an upgrade's wood and
    /// its pallet and rope) = two or three draws.
    /// </summary>
    public class ConstructionLook : MonoBehaviour
    {
        private const float Inset = 0.06f;
        private const float Pole = 0.08f;
        private const float BayMetres = 1.6f;
        private const float StoreyMetres = 1.1f;
        /// <summary>The net's mesh, per metre of cloth (one tile of the texture).</summary>
        private const float NetTiles = 2.6f;

        private static readonly Color WoodColour = new Color(0.55f, 0.37f, 0.2f);
        private static Material _wood, _net;

        private BuildingInstance _building;
        private bool _upgrade;
        private GameObject _site;
        private readonly List<Mesh> _meshes = new List<Mesh>();

        /// <summary>The site standing: the scaffolding and the net (for the tests).</summary>
        public GameObject Site => _site;

        public static ConstructionLook Raise(BuildingInstance building, bool upgrade)
        {
            if (!Wraps(building)) return null;
            var look = building.gameObject.AddComponent<ConstructionLook>();
            look._building = building;
            look._upgrade = upgrade;
            look.Build();
            return look;
        }

        /// <summary>
        /// Every building gets a site (the user, 2026-10-28: «добавляй в игру на все здания") but what
        /// is walked on -- a road, a bridge -- which a net and scaffolding would stand over as a box.
        /// </summary>
        public static bool Wraps(BuildingInstance building) =>
            building != null && building.Data != null && !building.Data.isRoad && !building.Data.providesWalkableSurface;

        private void Build()
        {
            var cell = GridManager.Instance != null ? GridManager.Instance.CellSize : 1f;
            var size = new Vector2(_building.Data.footprintSize.x * cell, _building.Data.footprintSize.y * cell);
            var appearance = GetComponent<BuildingLevelAppearance>();
            var height = appearance != null ? appearance.HeightFor(_building.Level) : 0f;
            // an upgrade wraps whichever is taller, the building now or the one it becomes
            if (_upgrade && appearance != null) height = Mathf.Max(height, appearance.HeightFor(_building.Level + 1));
            var box = GetComponent<BoxCollider>();
            if (box != null) height = Mathf.Max(height, box.size.y);
            if (height < 0.5f) height = 2f;

            var netHeight = height + 0.08f;
            var top = netHeight + 0.35f;
            var wood = new MeshBits();
            var hoist = Scaffolding(wood, size, top, _upgrade);

            _site = new GameObject("ConstructionSite");
            _site.transform.SetParent(transform, false);
            if (_upgrade)
            {
                Gallows(wood, hoist, top);
                AddPart("Wood", wood.ToMesh("Scaffolding"), WoodMaterial(), true);
                ConstructionHoist.Raise(_site.transform, hoist, top - 0.12f, WoodMaterial(), _meshes);
                return;
            }
            AddPart("Wood", wood.ToMesh("Scaffolding"), WoodMaterial(), true);
            AddPart("Net", Net(NetHalf(size, appearance), netHeight), NetMaterial(), false);
        }

        /// <summary>The hoist's frame over the gap: an arm in from the front poles' tops and the drum the rope winds on.</summary>
        private static void Gallows(MeshBits wood, Vector3 hoist, float top)
        {
            var pole = new Vector3(hoist.x, 0f, hoist.z - HoistInward);
            wood.Box(new Vector3(hoist.x, top - 0.04f, (pole.z + hoist.z) * 0.5f + 0.03f), new Vector3(0.07f, 0.07f, HoistInward + 0.12f), Quaternion.identity);
            wood.Box(pole + Vector3.up * (top * 0.5f), new Vector3(Pole, top, Pole), Quaternion.identity);
            wood.Box(new Vector3(hoist.x, top - 0.08f, hoist.z), new Vector3(0.26f, 0.1f, 0.1f), Quaternion.identity);
            // a crank on the drum's side
            wood.Box(new Vector3(hoist.x + 0.16f, top - 0.08f, hoist.z - 0.05f), new Vector3(0.03f, 0.03f, 0.12f), Quaternion.identity);
        }

        private void AddPart(string name, Mesh mesh, Material material, bool shadows)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(_site.transform, false);
            _meshes.Add(mesh);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        }

        /// <summary>
        /// Half the net's width and depth: just round the model, inside the poles where it fits; a
        /// model built out to its cells' edge gets the net out there too, over the poles, rather
        /// than walls poking through it. Never past the building's own cells.
        /// </summary>
        private Vector2 NetHalf(Vector2 size, BuildingLevelAppearance appearance)
        {
            var most = new Vector2(size.x * 0.5f - 0.03f, size.y * 0.5f - 0.03f);
            var model = appearance != null ? appearance.ModelFor(_building.Level) : null;
            var reach = Vector2.zero;
            if (model != null)
            {
                foreach (var renderer in model.GetComponentsInChildren<Renderer>())
                {
                    var b = renderer.bounds;
                    for (var k = 0; k < 4; k++)
                    {
                        var corner = new Vector3(k % 2 == 0 ? b.min.x : b.max.x, b.center.y, k < 2 ? b.min.z : b.max.z);
                        var local = transform.InverseTransformPoint(corner);
                        reach = Vector2.Max(reach, new Vector2(Mathf.Abs(local.x), Mathf.Abs(local.z)));
                    }
                }
            }
            if (reach == Vector2.zero) reach = most - new Vector2(0.3f, 0.3f);
            return new Vector2(Mathf.Clamp(reach.x + 0.06f, 0.3f, most.x), Mathf.Clamp(reach.y + 0.06f, 0.3f, most.y));
        }

        /// <summary>
        /// The net: four hanging sides and a cover, sagging inward between the poles and bellying
        /// between the decks, with a ripple so it reads as cloth and not as a glass box.
        /// </summary>
        private static Mesh Net(Vector2 half, float height)
        {
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            var corners = new[] { new Vector3(-half.x, 0f, -half.y), new Vector3(half.x, 0f, -half.y), new Vector3(half.x, 0f, half.y), new Vector3(-half.x, 0f, half.y) };
            var rows = Mathf.Max(3, Mathf.CeilToInt(height / 0.35f));
            for (var side = 0; side < 4; side++)
            {
                var a = corners[side];
                var b = corners[(side + 1) % 4];
                var length = (b - a).magnitude;
                var dir = (b - a) / length;
                var inward = new Vector3(-dir.z, 0f, dir.x);
                var bays = Mathf.Max(1, Mathf.CeilToInt(length / BayMetres));
                var columns = bays * 4;
                var start = vertices.Count;
                for (var j = 0; j <= rows; j++)
                {
                    var y = height * j / rows;
                    var storey = Mathf.Repeat(y / StoreyMetres, 1f);
                    for (var i = 0; i <= columns; i++)
                    {
                        var along = length * i / columns;
                        var bay = Mathf.Repeat(along / (length / bays), 1f);
                        // pinned at the poles and at the decks, slack between, a little ripple
                        var sag = 0.09f * Mathf.Sin(bay * Mathf.PI) * (0.45f + 0.55f * Mathf.Sin(storey * Mathf.PI));
                        sag += 0.025f * Mathf.Sin(along * 5.1f + y * 2.3f + side);
                        if (j == 0 || j == rows) sag *= 0.3f;
                        vertices.Add(a + dir * along + Vector3.up * y + inward * sag);
                        uv.Add(new Vector2((along + side * 7f) * NetTiles, y * NetTiles));
                    }
                }
                for (var j = 0; j < rows; j++)
                {
                    for (var i = 0; i < columns; i++)
                    {
                        var v00 = start + j * (columns + 1) + i;
                        var v10 = v00 + 1;
                        var v01 = v00 + columns + 1;
                        var v11 = v01 + 1;
                        // outward-facing (cross(dir, up) points inward)
                        triangles.Add(v00); triangles.Add(v11); triangles.Add(v10);
                        triangles.Add(v00); triangles.Add(v01); triangles.Add(v11);
                    }
                }
            }
            // the cover, sagging to the middle
            var nx = Mathf.Max(2, Mathf.CeilToInt(half.x * 2f / 0.5f));
            var nz = Mathf.Max(2, Mathf.CeilToInt(half.y * 2f / 0.5f));
            var top = vertices.Count;
            for (var k = 0; k <= nz; k++)
            {
                for (var i = 0; i <= nx; i++)
                {
                    var u = (float)i / nx;
                    var v = (float)k / nz;
                    var droop = 0.12f * Mathf.Sin(u * Mathf.PI) * Mathf.Sin(v * Mathf.PI);
                    var p = new Vector3(Mathf.Lerp(-half.x, half.x, u), height - droop, Mathf.Lerp(-half.y, half.y, v));
                    vertices.Add(p);
                    uv.Add(new Vector2(p.x, p.z) * NetTiles);
                }
            }
            for (var k = 0; k < nz; k++)
            {
                for (var i = 0; i < nx; i++)
                {
                    var a = top + k * (nx + 1) + i;
                    var b = a + 1;
                    var d = a + nx + 1;
                    var c = d + 1;
                    triangles.Add(a); triangles.Add(c); triangles.Add(b);
                    triangles.Add(a); triangles.Add(d); triangles.Add(c);
                }
            }
            var mesh = new Mesh { name = "ConstructionNet" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Poles round the footprint's edge, a plank deck every storey, a brace in every other bay.</summary>
        /// <summary>How far in from the front poles the hoist's rope hangs: clear of them, over the gap in the decks.</summary>
        private const float HoistInward = 0.17f;

        /// <summary>
        /// Poles round the footprint's edge, a plank deck every storey, a brace in every other bay.
        /// With `hoistGap` the front decks leave the first bay's middle open for the hoist; the
        /// point the rope hangs from comes back (on the ground).
        /// </summary>
        private static Vector3 Scaffolding(MeshBits wood, Vector2 size, float height, bool hoistGap)
        {
            var hoist = Vector3.zero;
            var hx = size.x * 0.5f - Inset;
            var hz = size.y * 0.5f - Inset;
            var corners = new[] { new Vector3(-hx, 0f, -hz), new Vector3(hx, 0f, -hz), new Vector3(hx, 0f, hz), new Vector3(-hx, 0f, hz) };
            var storeys = Mathf.Max(1, Mathf.FloorToInt(height / StoreyMetres));
            for (var side = 0; side < 4; side++)
            {
                var a = corners[side];
                var b = corners[(side + 1) % 4];
                var along = b - a;
                var length = along.magnitude;
                var dir = along / length;
                var inward = new Vector3(-dir.z, 0f, dir.x);
                var bays = Mathf.Max(1, Mathf.CeilToInt(length / BayMetres));
                var bay = length / bays;
                for (var i = 0; i < bays; i++)
                {
                    var p = a + dir * (bay * i);
                    wood.Box(p + Vector3.up * (height * 0.5f), new Vector3(Pole, height, Pole), Quaternion.identity);
                    if (i % 2 == side % 2)
                    {
                        // a brace across the bay's first storey
                        var rise = Mathf.Min(StoreyMetres, height - 0.05f);
                        var from = p + Vector3.up * 0.1f;
                        var to = p + dir * bay + Vector3.up * rise;
                        var span = to - from;
                        wood.Box((from + to) * 0.5f, new Vector3(0.05f, 0.05f, span.magnitude), Quaternion.LookRotation(span));
                    }
                }
                var turn = Quaternion.LookRotation(dir);
                var gap = hoistGap && side == 0;
                if (gap) hoist = a + dir * (bay * 0.5f) + inward * HoistInward;
                for (var s = 1; s <= storeys; s++)
                {
                    var y = Mathf.Min(s * StoreyMetres, height - 0.05f);
                    // the deck along the poles, on the building's own cells
                    if (gap)
                    {
                        // the deck in two, round the hoist's way up
                        var from = bay * 0.5f - 0.24f;
                        var to = bay * 0.5f + 0.24f;
                        if (from > 0f) wood.Box(a + dir * (from * 0.5f) + inward * 0.06f + Vector3.up * y, new Vector3(0.18f, 0.05f, from + Pole), turn);
                        wood.Box(a + dir * ((to + length) * 0.5f) + inward * 0.06f + Vector3.up * y, new Vector3(0.18f, 0.05f, length - to + Pole), turn);
                    }
                    else
                    {
                        var deck = (a + b) * 0.5f + inward * 0.06f + Vector3.up * y;
                        wood.Box(deck, new Vector3(0.18f, 0.05f, length + Pole), turn);
                    }
                    // the rail on the outside, on the poles: never over their tops (a low building's
                    // top rail stood in the air), and none where it would lie on the deck
                    var railY = Mathf.Min(y + 0.45f, height - 0.03f);
                    if (railY - y < 0.2f) continue;
                    var rail = (a + b) * 0.5f + Vector3.up * railY;
                    wood.Box(rail, new Vector3(0.04f, 0.04f, length), turn);
                }
            }
            return hoist;
        }

        /// <summary>The time is up: the site goes (an upgrade's new level already on), a new building's under the dust.</summary>
        public void Complete()
        {
            var height = 2f;
            if (_site != null)
            {
                foreach (var renderer in _site.GetComponentsInChildren<Renderer>()) height = Mathf.Max(height, renderer.bounds.size.y);
                Gone(_site);
                _site = null;
            }
            foreach (var mesh in _meshes) Gone(mesh);
            _meshes.Clear();

            var appearance = GetComponent<BuildingLevelAppearance>();
            var model = appearance != null && _building != null ? appearance.ModelFor(_building.Level) : null;
            // a new building under the dust; an upgrade with a lighter touch (the user, 2026-10-28:
            // none at first, then «нужно всё же какой-то эффектик»)
            if (Application.isPlaying && _building != null && _building.Data != null)
            {
                var cell = GridManager.Instance != null ? GridManager.Instance.CellSize : 1f;
                var size = new Vector2(_building.Data.footprintSize.x * cell, _building.Data.footprintSize.y * cell);
                var shown = model != null ? model.transform : null;
                if (_upgrade) BuildFinishEffect.PlayUpgrade(transform, shown, size, height);
                else BuildFinishEffect.Play(transform, shown, size, height);
            }
            Gone(this);
        }

        private void OnDestroy()
        {
            // demolished mid-build: the meshes are this site's own
            foreach (var mesh in _meshes)
            {
                if (mesh != null) Gone(mesh);
            }
        }

        private static void Gone(Object thing)
        {
            // hidden at once: Destroy waits for the frame's end, and until then the scaffolding would
            // stand beside the new level's model as a second one
            if (thing is GameObject go) go.SetActive(false);
            if (Application.isPlaying) Destroy(thing);
            else DestroyImmediate(thing);
        }

        private static Material WoodMaterial() => _wood != null ? _wood : _wood = new Material(RuntimeShaders.Lit) { name = "ScaffoldWood", color = WoodColour };

        /// <summary>The construction net: see-through green mesh with darker threads, lit, so it shades as cloth.</summary>
        private static Material NetMaterial()
        {
            if (_net != null) return _net;
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "ConstructionNet", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear };
            var cloth = new Color(0.34f, 0.48f, 0.4f, 0.8f);
            var thread = new Color(0.22f, 0.33f, 0.27f, 0.92f);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var onThread = x % 4 == 0 || y % 4 == 0;
                    // a darker band every so often, as the photo's strips of tighter weave
                    var band = y >= 12 && y < 15;
                    texture.SetPixel(x, y, onThread ? thread : band ? Color.Lerp(cloth, thread, 0.5f) : cloth);
                }
            }
            texture.Apply(true);
            _net = new Material(RuntimeShaders.Lit) { name = "ConstructionNet", color = Color.white };
            _net.SetTexture("_BaseMap", texture);
            _net.SetFloat("_Smoothness", 0.1f);
            _net.SetFloat("_Surface", 1f);
            _net.SetFloat("_Blend", 0f);
            _net.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            _net.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            _net.SetFloat("_ZWrite", 0f);
            _net.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _net.renderQueue = (int)RenderQueue.Transparent;
            return _net;
        }

        /// <summary>Boxes gathered into one mesh, each with its own face normals.</summary>
        internal class MeshBits
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<Vector3> _normals = new List<Vector3>();
            private readonly List<int> _triangles = new List<int>();

            private static readonly Vector3[] Faces = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };

            public void Box(Vector3 centre, Vector3 size, Quaternion rotation)
            {
                var half = size * 0.5f;
                foreach (var face in Faces)
                {
                    // two axes across the face
                    var u = face.x != 0f ? Vector3.forward : Vector3.right;
                    var v = Vector3.Cross(face, u);
                    var start = _vertices.Count;
                    for (var k = 0; k < 4; k++)
                    {
                        var su = k == 0 || k == 3 ? -1f : 1f;
                        var sv = k < 2 ? -1f : 1f;
                        var local = Vector3.Scale(face + u * su + v * sv, half);
                        _vertices.Add(centre + rotation * local);
                        _normals.Add(rotation * face);
                    }
                    // clockwise from outside (cross(u, v) is the face itself)
                    _triangles.Add(start);
                    _triangles.Add(start + 1);
                    _triangles.Add(start + 2);
                    _triangles.Add(start);
                    _triangles.Add(start + 2);
                    _triangles.Add(start + 3);
                }
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name };
                if (_vertices.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(_vertices);
                mesh.SetNormals(_normals);
                mesh.SetTriangles(_triangles, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
