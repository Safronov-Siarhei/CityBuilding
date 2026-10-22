using CityBuilder.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Grid
{
    /// <summary>
    /// The green carpet of cells showing how far a gatherer reaches, drawn under the ghost while
    /// the player is choosing where to put it and again when they tap the finished building.
    ///
    /// Worth showing at all because stone does not come back: a Quarry is placed once, works out
    /// the boulders it can reach, and is then a dead building on a spot that will never be worth
    /// anything again. Asking the player to guess 20 metres by eye and live with it is the kind of
    /// decision a game should never make them take blind.
    ///
    /// ONE mesh, not one object per cell. A Quarry at level 3 reaches 38 metres over a 1-metre
    /// grid, which is about 4500 cells -- as separate GameObjects that is 4500 transforms and 4500
    /// draw calls following the cursor every frame, which no phone is going to enjoy. Built as a
    /// single procedural mesh it is one draw call, and it is rebuilt only when the RADIUS changes,
    /// never when the building moves: following the cursor is a transform write and nothing else.
    ///
    /// Created on demand rather than wired up in SetupProject, the same way BuildingHealthBar and
    /// the citizen's target-cell frame are -- it has no state worth authoring and nothing else
    /// needs a reference to it.
    /// </summary>
    public class HarvestRadiusOverlay : MonoBehaviour
    {
        // How much of each cell the green square fills. The gap is what makes it read as a grid of
        // cells rather than one flat green disc -- the point is to show the player the shape in
        // the units the game actually thinks in.
        private const float CellFillFraction = 0.82f;
        private const float HeightAboveGround = 0.04f;

        private static readonly Color FillColor = new Color(0.35f, 0.85f, 0.4f, 0.35f);

        private static HarvestRadiusOverlay _instance;

        private MeshFilter _filter;
        private Mesh _mesh;
        private int _builtForRadius = -1;
        private float _builtForCellSize = -1f;

        /// <summary>
        /// The overlay, made on first use. The Unity null check matters here and is not noise: on
        /// a scene load the object is destroyed while this static reference still points at it,
        /// and a destroyed object compares equal to null, so the next caller quietly gets a fresh
        /// one instead of a dangling reference.
        /// </summary>
        public static HarvestRadiusOverlay Instance
        {
            get
            {
                if (_instance != null) return _instance;

                var go = new GameObject("HarvestRadiusOverlay");
                _instance = go.AddComponent<HarvestRadiusOverlay>();
                _instance.Build();
                go.SetActive(false);
                return _instance;
            }
        }

        /// <summary>Lays the carpet under a building centred at this point. A radius of zero hides it, which is every building that is not a Sawmill or a Quarry.</summary>
        public static void ShowFor(Vector3 center, int radiusMetres)
        {
            if (radiusMetres <= 0)
            {
                HideIfShown();
                return;
            }

            Instance.Show(center, radiusMetres);
        }

        /// <summary>
        /// The carpet, and a gold ring at the foot of every tree or boulder of the yard's kind
        /// inside it that its workers could take now (the user, 2026-10-22: a tree inside the
        /// circle looked gatherable and was not -- a sapling, or measured through a slope).
        /// </summary>
        public static void ShowFor(Vector3 center, int radiusMetres, Buildings.BuildingData yard)
        {
            ShowFor(center, radiusMetres);
            if (radiusMetres > 0 && _instance != null) _instance.MarkNodes(center, radiusMetres, GatheredBy(yard));
        }

        /// <summary>What a yard gathers from the map's nodes (wood, stone), or null for anything else.</summary>
        public static Resources.ResourceType? GatheredBy(Buildings.BuildingData data)
        {
            if (data == null || data.recipes == null || data.recipes.Count == 0) return null;
            var output = data.recipes[0].output;
            return output == Resources.ResourceType.Wood || output == Resources.ResourceType.Stone ? output : (Resources.ResourceType?)null;
        }

        /// <summary>Hides the overlay, without creating one just to hide it -- the common case is a building that never had a radius.</summary>
        public static void HideIfShown()
        {
            if (_instance == null) return;
            _instance.gameObject.SetActive(false);
            _instance.HideMarks();
        }

        private void Show(Vector3 center, int radiusMetres)
        {
            var grid = GridManager.Instance;
            if (grid == null) return;

            if (radiusMetres != _builtForRadius || !Mathf.Approximately(grid.CellSize, _builtForCellSize))
            {
                Rebuild(radiusMetres, grid.CellSize);
            }

            transform.position = new Vector3(center.x, grid.GroundHeight + HeightAboveGround, center.z);
            gameObject.SetActive(true);
        }

        // ---------------------------------------------------------------- the gatherable nodes

        private const int MaxMarks = 96;
        private const float MarkRefreshSeconds = 0.4f;
        private static readonly Color MarkColor = new Color(1f, 0.85f, 0.3f, 0.9f);
        private readonly List<Transform> _marks = new List<Transform>();
        private Material _markMaterial;
        private Mesh _markMesh;
        private Vector3 _markedAt = new Vector3(float.MaxValue, 0f, 0f);
        private float _markedTime = -10f;
        private int _shownMarks;

        private void MarkNodes(Vector3 center, int radiusMetres, Resources.ResourceType? type)
        {
            if (type == null)
            {
                HideMarks();
                return;
            }
            // a ghost following the cursor asks every frame: refresh when it has moved or a while has passed
            var flat = new Vector3(center.x, 0f, center.z);
            if ((flat - _markedAt).sqrMagnitude < 0.25f && Time.unscaledTime - _markedTime < MarkRefreshSeconds) return;
            _markedAt = flat;
            _markedTime = Time.unscaledTime;

            var radiusSq = (float)radiusMetres * radiusMetres;
            var shown = 0;
            foreach (var node in Maps.ResourceNode.All)
            {
                if (shown >= MaxMarks) break;
                if (node.ResourceType != type.Value || !Citizens.CitizenVisualsManager.IsGatherable(node)) continue;
                var p = node.transform.position;
                var dx = p.x - center.x;
                var dz = p.z - center.z;
                if (dx * dx + dz * dz > radiusSq) continue;
                var mark = Mark(shown++);
                mark.position = new Vector3(p.x, p.y + HeightAboveGround * 2f, p.z);
                mark.gameObject.SetActive(true);
            }
            for (var i = shown; i < _shownMarks; i++) _marks[i].gameObject.SetActive(false);
            _shownMarks = shown;
        }

        private void HideMarks()
        {
            for (var i = 0; i < _shownMarks && i < _marks.Count; i++) _marks[i].gameObject.SetActive(false);
            _shownMarks = 0;
            _markedTime = -10f;
        }

        private Transform Mark(int index)
        {
            while (_marks.Count <= index)
            {
                if (_markMesh == null)
                {
                    _markMesh = RingMesh(0.42f, 0.56f, 24);
                    _markMaterial = CreateTransparentMaterial();
                    _markMaterial.color = MarkColor;
                }
                var go = new GameObject("GatherableMark", typeof(MeshFilter), typeof(MeshRenderer));
                go.GetComponent<MeshFilter>().sharedMesh = _markMesh;
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = _markMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                go.SetActive(false);
                _marks.Add(go.transform);
            }
            return _marks[index];
        }

        /// <summary>A flat ring between two radii, facing up.</summary>
        private static Mesh RingMesh(float inner, float outer, int sides)
        {
            var vertices = new Vector3[sides * 2];
            var triangles = new int[sides * 6];
            for (var i = 0; i < sides; i++)
            {
                var a = i / (float)sides * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                vertices[i * 2] = dir * inner;
                vertices[i * 2 + 1] = dir * outer;
                var n = (i + 1) % sides;
                var t = i * 6;
                triangles[t + 0] = i * 2; triangles[t + 1] = n * 2; triangles[t + 2] = i * 2 + 1;
                triangles[t + 3] = i * 2 + 1; triangles[t + 4] = n * 2; triangles[t + 5] = n * 2 + 1;
            }
            var mesh = new Mesh { name = "GatherableMark", vertices = vertices, triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnDestroy()
        {
            foreach (var mark in _marks) if (mark != null) Destroy(mark.gameObject);
        }

        private void Build()
        {
            _filter = gameObject.AddComponent<MeshFilter>();
            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = CreateTransparentMaterial();

            _mesh = new Mesh { name = "HarvestRadius" };
            _filter.sharedMesh = _mesh;
        }

        /// <summary>
        /// A URP Unlit material built in code is opaque and throws the alpha in its colour away --
        /// the surface type is a set of shader keywords and blend states, not a property of the
        /// colour. This is the whole incantation for turning one transparent, which is what lets
        /// the ground, the trees and the boulders show through the carpet instead of the overlay
        /// hiding the very things it is drawn to help the player judge.
        /// </summary>
        private static Material CreateTransparentMaterial()
        {
            var material = new Material(RuntimeShaders.Unlit) { color = FillColor };

            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.Transparent;

            return material;
        }

        private void Rebuild(int radiusMetres, float cellSize)
        {
            _builtForRadius = radiusMetres;
            _builtForCellSize = cellSize;

            var cells = CellsWithin(radiusMetres, cellSize);
            var half = cellSize * CellFillFraction * 0.5f;

            var vertices = new Vector3[cells.Count * 4];
            var triangles = new int[cells.Count * 6];

            for (var i = 0; i < cells.Count; i++)
            {
                var c = cells[i];
                var x = c.x * cellSize;
                var z = c.y * cellSize;
                var v = i * 4;

                // Wound counter-clockwise seen from above, so the quads face up at the camera
                // rather than being back-face culled into invisibility.
                vertices[v + 0] = new Vector3(x - half, 0f, z - half);
                vertices[v + 1] = new Vector3(x - half, 0f, z + half);
                vertices[v + 2] = new Vector3(x + half, 0f, z + half);
                vertices[v + 3] = new Vector3(x + half, 0f, z - half);

                var t = i * 6;
                triangles[t + 0] = v + 0;
                triangles[t + 1] = v + 1;
                triangles[t + 2] = v + 2;
                triangles[t + 3] = v + 0;
                triangles[t + 4] = v + 2;
                triangles[t + 5] = v + 3;
            }

            _mesh.Clear();
            // A 16-bit index buffer tops out at 65535 vertices, which a radius of about 64 cells
            // would exceed. Switched only when it has to be: the wider buffer is twice the memory
            // for every mesh that never needed it.
            _mesh.indexFormat = vertices.Length > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            _mesh.vertices = vertices;
            _mesh.triangles = triangles;
            _mesh.RecalculateBounds();
        }

        /// <summary>
        /// Which cell offsets, measured from the building's own cell, fall inside the radius.
        ///
        /// Measured to each cell's CENTRE, which is what makes the edge of the carpet agree with
        /// the rule the workers actually follow (see CitizenVisualsManager.FindNearestFreeNode,
        /// which compares the node's position against the same radius). An overlay that promised
        /// a ring of cells the workers then refused to walk to would be worse than no overlay.
        ///
        /// Pure and static so an EditMode test can pin the shape without a scene.
        /// </summary>
        public static List<Vector2Int> CellsWithin(int radiusMetres, float cellSize)
        {
            var cells = new List<Vector2Int>();
            if (radiusMetres <= 0 || cellSize <= 0f) return cells;

            var reach = Mathf.CeilToInt(radiusMetres / cellSize);
            var radiusSq = (float)radiusMetres * radiusMetres;

            for (var dz = -reach; dz <= reach; dz++)
            {
                for (var dx = -reach; dx <= reach; dx++)
                {
                    var x = dx * cellSize;
                    var z = dz * cellSize;
                    if (x * x + z * z > radiusSq) continue;

                    cells.Add(new Vector2Int(dx, dz));
                }
            }

            return cells;
        }
    }
}
