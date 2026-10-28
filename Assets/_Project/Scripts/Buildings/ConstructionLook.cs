using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Grid;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// What a building under construction looks like (the user, 2026-10-28: «строительная площадка
    /// с лесами -> эффект при окончании таймера стройки -> готовое здание», nothing revealed bit by
    /// bit):
    ///
    /// - built: the building's model is hidden; in its place a stone foundation, a low ring of wall
    ///   begun, a stack of planks and a heap of stones, all inside wooden scaffolding;
    /// - upgraded: the building stays as it is, the scaffolding stands round it.
    ///
    /// The scaffolding is made here from the footprint, the same for every building, and never
    /// leaves the building's own cells, so it cannot run into a neighbour (the user's worry about
    /// upgrades). Two meshes per site (wood, stone), so a site is two draws.
    ///
    /// When the time is up the site goes under the finish the settings pick (GameSettings.BuildFinish,
    /// both made to choose from): dust and thrown planks with the building springing up, or a gold
    /// flash and sparks with the building popping out of it.
    /// </summary>
    public class ConstructionLook : MonoBehaviour
    {
        private const float Inset = 0.06f;
        private const float Pole = 0.08f;
        private const float BayMetres = 1.6f;
        private const float StoreyMetres = 1.1f;

        private static readonly Color WoodColour = new Color(0.55f, 0.37f, 0.2f);
        private static readonly Color StoneColour = new Color(0.63f, 0.6f, 0.55f);
        private static Material _wood, _stone;

        private BuildingInstance _building;
        private bool _upgrade;
        private GameObject _site;
        private readonly List<Mesh> _meshes = new List<Mesh>();
        private readonly List<Renderer> _hidden = new List<Renderer>();

        /// <summary>The scaffolding standing (for the tests).</summary>
        public GameObject Site => _site;

        public static ConstructionLook Raise(BuildingInstance building, bool upgrade)
        {
            if (building == null || building.Data == null) return null;
            var look = building.gameObject.AddComponent<ConstructionLook>();
            look._building = building;
            look._upgrade = upgrade;
            look.Build();
            return look;
        }

        private void Build()
        {
            var cell = GridManager.Instance != null ? GridManager.Instance.CellSize : 1f;
            var size = new Vector2(_building.Data.footprintSize.x * cell, _building.Data.footprintSize.y * cell);
            var appearance = GetComponent<BuildingLevelAppearance>();
            var level = _upgrade ? _building.Level + 1 : _building.Level;
            var height = appearance != null ? appearance.HeightFor(level) : 0f;
            var box = GetComponent<BoxCollider>();
            if (box != null) height = Mathf.Max(height, _upgrade ? box.size.y : 0f);
            if (height < 0.5f) height = 2f;

            if (!_upgrade)
            {
                // the building waits under the site; its own renderers come back at the finish
                var model = appearance != null ? appearance.ModelFor(_building.Level) : null;
                if (model != null)
                {
                    foreach (var renderer in model.GetComponentsInChildren<Renderer>())
                    {
                        if (!renderer.enabled) continue;
                        renderer.enabled = false;
                        _hidden.Add(renderer);
                    }
                }
            }

            // a built site's scaffolding is a storey or so lower than the building it is for; an
            // upgrade's stands a little over the roof it works on
            var scaffold = _upgrade ? height + 0.25f : Mathf.Max(1.3f, height * 0.7f);
            var wood = new MeshBits();
            var stone = new MeshBits();
            Scaffolding(wood, size, scaffold);
            if (!_upgrade) Groundwork(wood, stone, size);

            _site = new GameObject("ConstructionSite");
            _site.transform.SetParent(transform, false);
            AddPart("Wood", wood, WoodMaterial());
            if (!_upgrade) AddPart("Stone", stone, StoneMaterial());
        }

        private void AddPart(string name, MeshBits bits, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(_site.transform, false);
            var mesh = bits.ToMesh(name);
            _meshes.Add(mesh);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        /// <summary>Poles round the footprint's edge, a plank deck every storey, a brace in every other bay.</summary>
        private static void Scaffolding(MeshBits wood, Vector2 size, float height)
        {
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
                        var rise = Mathf.Min(StoreyMetres, height);
                        var from = p + Vector3.up * 0.1f;
                        var to = p + dir * bay + Vector3.up * rise;
                        var mid = (from + to) * 0.5f;
                        var span = to - from;
                        wood.Box(mid, new Vector3(0.05f, 0.05f, span.magnitude), Quaternion.LookRotation(span));
                    }
                }
                var turn = Quaternion.LookRotation(dir);
                for (var s = 1; s <= storeys; s++)
                {
                    var y = Mathf.Min(s * StoreyMetres, height - 0.05f);
                    // the deck, laid inward from the poles so it stays on the building's own cells
                    var deck = (a + b) * 0.5f + inward * 0.17f + Vector3.up * y;
                    wood.Box(deck, new Vector3(0.3f, 0.05f, length + Pole), turn);
                    // the rail on the outside
                    var rail = (a + b) * 0.5f + Vector3.up * (y + 0.45f);
                    wood.Box(rail, new Vector3(0.04f, 0.04f, length), turn);
                }
            }
        }

        /// <summary>A built site's ground: the foundation, a ring of wall begun, planks and stones waiting.</summary>
        private static void Groundwork(MeshBits wood, MeshBits stone, Vector2 size)
        {
            var fx = size.x - 0.3f;
            var fz = size.y - 0.3f;
            stone.Box(new Vector3(0f, 0.09f, 0f), new Vector3(fx, 0.18f, fz), Quaternion.identity);
            var wx = Mathf.Max(0.4f, size.x - 0.9f);
            var wz = Mathf.Max(0.4f, size.y - 0.9f);
            const float wall = 0.18f;
            const float course = 0.45f;
            stone.Box(new Vector3(0f, 0.18f + course * 0.5f, -wz * 0.5f), new Vector3(wx, course, wall), Quaternion.identity);
            stone.Box(new Vector3(-wx * 0.5f, 0.18f + course * 0.5f, 0f), new Vector3(wall, course, wz), Quaternion.identity);
            // two walls half up, so it reads as work in hand
            stone.Box(new Vector3(0f, 0.18f + course * 0.3f, wz * 0.5f), new Vector3(wx * 0.6f, course * 0.6f, wall), Quaternion.identity);
            stone.Box(new Vector3(wx * 0.5f, 0.18f + course * 0.25f, -wz * 0.15f), new Vector3(wall, course * 0.5f, wz * 0.7f), Quaternion.identity);
            if (size.x < 1.5f || size.y < 1.5f) return;
            // a stack of planks and a heap of stones on the floor inside
            var stack = new Vector3(size.x * 0.12f, 0.18f, size.y * 0.1f);
            for (var i = 0; i < 3; i++)
            {
                wood.Box(stack + new Vector3(0f, 0.04f + i * 0.07f, 0f), new Vector3(0.6f, 0.06f, 0.16f), Quaternion.Euler(0f, 20f + i * 6f, 0f));
            }
            var heap = new Vector3(-size.x * 0.15f, 0.18f, size.y * 0.18f);
            stone.Box(heap + new Vector3(0f, 0.09f, 0f), new Vector3(0.22f, 0.18f, 0.2f), Quaternion.Euler(0f, 15f, 0f));
            stone.Box(heap + new Vector3(0.2f, 0.07f, 0.05f), new Vector3(0.18f, 0.14f, 0.18f), Quaternion.Euler(0f, 40f, 0f));
            stone.Box(heap + new Vector3(0.08f, 0.22f, 0.02f), new Vector3(0.16f, 0.12f, 0.16f), Quaternion.Euler(0f, 70f, 0f));
        }

        /// <summary>The time is up: the site goes, the building is back (an upgrade's new level already on), the finish plays.</summary>
        public void Complete()
        {
            foreach (var renderer in _hidden)
            {
                if (renderer != null) renderer.enabled = true;
            }
            _hidden.Clear();
            if (_site != null)
            {
                Gone(_site);
                _site = null;
            }
            foreach (var mesh in _meshes) Gone(mesh);
            _meshes.Clear();

            var appearance = GetComponent<BuildingLevelAppearance>();
            var model = appearance != null && _building != null ? appearance.ModelFor(_building.Level) : null;
            if (Application.isPlaying && _building != null && _building.Data != null)
            {
                var cell = GridManager.Instance != null ? GridManager.Instance.CellSize : 1f;
                var size = new Vector2(_building.Data.footprintSize.x * cell, _building.Data.footprintSize.y * cell);
                BuildFinishEffect.Play(transform, model != null ? model.transform : null, size, GameSettings.BuildFinish);
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
            if (Application.isPlaying) Destroy(thing);
            else DestroyImmediate(thing);
        }

        private static Material WoodMaterial() => _wood != null ? _wood : _wood = new Material(RuntimeShaders.Lit) { name = "ScaffoldWood", color = WoodColour };

        private static Material StoneMaterial() => _stone != null ? _stone : _stone = new Material(RuntimeShaders.Lit) { name = "SiteStone", color = StoneColour };

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
