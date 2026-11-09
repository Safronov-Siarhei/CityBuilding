using System;
using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Grid;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// The PHONE's look of the building being placed (the user, 2026-11-09, variant A -- PC keeps
    /// PlacementPreview's ink drawing; chosen by platform): the building in its own colours, jumping
    /// cell by cell like Clash of Clans as the dot in the middle of the screen crosses the grid, over its footprint's cells filled green where it can stand and
    /// red where it cannot (only the cells in the way, or all of them when the reason is elsewhere:
    /// money, the start zone). The ink drawing's fine lines and pale paper melted into a grey box
    /// on a phone screen.
    ///
    /// Cheap: the building keeps its own materials (a red copy of each, made once, when refused);
    /// the cells are one small mesh rebuilt only when the cell, the footprint or the verdict changes.
    /// </summary>
    public class PlacementTilePreview : MonoBehaviour
    {
        private const float Lift = 0.12f;          // the building hovers this far: "in hand", not built
        private const float TileLift = 0.06f;      // the cells, over the grass
        private const float TileInset = 0.06f;     // gap between cells, in cell sizes
        private const float HopSharpness = 40f;    // a quick glide onto the next cell, not a teleport

        private static readonly Color Green = new Color(0.25f, 0.85f, 0.3f, 0.55f);
        private static readonly Color Red = new Color(0.95f, 0.2f, 0.15f, 0.6f);
        private static readonly Color RefusedTint = new Color(1f, 0.45f, 0.4f, 1f);
        private static readonly Dictionary<Material, Material> RedMaterials = new Dictionary<Material, Material>();
        private static Material _green;
        private static Material _red;

        private readonly List<Renderer> _renderers = new List<Renderer>();
        private readonly List<Material[]> _own = new List<Material[]>();
        private readonly List<Material[]> _refused = new List<Material[]>();
        private bool? _shownRefused;

        private GameObject _tiles;
        private Mesh _mesh;
        private Vector2Int _shownCell;
        private Vector2Int _shownFootprint;
        private bool? _shownValid;
        private readonly List<bool> _blocked = new List<bool>();
        private readonly List<bool> _shownBlocked = new List<bool>();
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<int> _greenTris = new List<int>();
        private readonly List<int> _redTris = new List<int>();

        private Vector3 _target;
        private bool _placedOnce;

        public static PlacementTilePreview Attach(GameObject ghost, List<Renderer> renderers)
        {
            var preview = ghost.AddComponent<PlacementTilePreview>();
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                var own = renderer.sharedMaterials;
                var red = new Material[own.Length];
                for (var i = 0; i < own.Length; i++) red[i] = RedOf(own[i]);
                preview._renderers.Add(renderer);
                preview._own.Add(own);
                preview._refused.Add(red);
            }
            return preview;
        }

        /// <summary>`centre`: the centre of the cells it will land on; `blockedCell` says, per cell, whether that cell is in the way.</summary>
        public void Show(Vector3 centre, Quaternion rotation, Vector2Int origin, Vector2Int footprint, bool valid, Func<Vector2Int, bool> blockedCell)
        {
            transform.rotation = rotation;
            _target = centre + Vector3.up * Lift;
            if (!_placedOnce)
            {
                _placedOnce = true;
                transform.position = _target;
            }

            if (_shownRefused != !valid)
            {
                _shownRefused = !valid;
                for (var i = 0; i < _renderers.Count; i++)
                {
                    if (_renderers[i] != null) _renderers[i].sharedMaterials = valid ? _own[i] : _refused[i];
                }
            }

            _blocked.Clear();
            for (var z = 0; z < footprint.y; z++)
            {
                for (var x = 0; x < footprint.x; x++) _blocked.Add(blockedCell(origin + new Vector2Int(x, z)));
            }
            if (_shownValid == valid && _shownCell == origin && _shownFootprint == footprint && SameBlocked()) return;
            _shownValid = valid;
            _shownCell = origin;
            _shownFootprint = footprint;
            _shownBlocked.Clear();
            _shownBlocked.AddRange(_blocked);
            BuildTiles(centre.y, origin, footprint, valid);
        }

        public void SetVisible(bool visible)
        {
            if (_tiles != null && _tiles.activeSelf != visible) _tiles.SetActive(visible);
        }

        private bool SameBlocked()
        {
            if (_blocked.Count != _shownBlocked.Count) return false;
            for (var i = 0; i < _blocked.Count; i++)
            {
                if (_blocked[i] != _shownBlocked[i]) return false;
            }
            return true;
        }

        private void BuildTiles(float groundY, Vector2Int origin, Vector2Int footprint, bool valid)
        {
            if (_tiles == null)
            {
                _tiles = new GameObject("PlacementTiles");
                _mesh = new Mesh { name = "PlacementTiles" };
                _mesh.MarkDynamic();
                _tiles.AddComponent<MeshFilter>().sharedMesh = _mesh;
                var renderer = _tiles.AddComponent<MeshRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sharedMaterials = new[] { TileMaterial(ref _green, Green), TileMaterial(ref _red, Red) };
            }

            var any = false;
            foreach (var b in _shownBlocked) any |= b;

            var grid = GridManager.Instance;
            var size = grid.CellSize;
            var inset = TileInset * size;
            var y = groundY + TileLift;
            _vertices.Clear();
            _greenTris.Clear();
            _redTris.Clear();
            var k = 0;
            for (var z = 0; z < footprint.y; z++)
            {
                for (var x = 0; x < footprint.x; x++, k++)
                {
                    // refused for a reason of its own (money, the zone) with no cell in the way: all red
                    var red = !valid && (!any || _shownBlocked[k]);
                    var corner = grid.CellToWorld(origin + new Vector2Int(x, z));
                    var b = _vertices.Count;
                    _vertices.Add(new Vector3(corner.x + inset, y, corner.z + inset));
                    _vertices.Add(new Vector3(corner.x + inset, y, corner.z + size - inset));
                    _vertices.Add(new Vector3(corner.x + size - inset, y, corner.z + size - inset));
                    _vertices.Add(new Vector3(corner.x + size - inset, y, corner.z + inset));
                    var tris = red ? _redTris : _greenTris;
                    tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                    tris.Add(b); tris.Add(b + 2); tris.Add(b + 3);
                }
            }
            _mesh.Clear();
            _mesh.subMeshCount = 2;
            _mesh.SetVertices(_vertices);
            _mesh.SetTriangles(_greenTris, 0);
            _mesh.SetTriangles(_redTris, 1);
            _mesh.RecalculateBounds();
        }

        private void LateUpdate()
        {
            if (!_placedOnce) return;
            var t = 1f - Mathf.Exp(-HopSharpness * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, _target, t);
        }

        private void OnDestroy()
        {
            if (_tiles != null) Destroy(_tiles);
            if (_mesh != null) Destroy(_mesh);
        }

        /// <summary>The building's own material tinted red: made once per material, kept for every later placement.</summary>
        private static Material RedOf(Material original)
        {
            if (original == null) return null;
            if (RedMaterials.TryGetValue(original, out var red) && red != null) return red;
            red = new Material(original) { name = original.name + " (refused)" };
            if (red.HasProperty("_BaseColor")) red.SetColor("_BaseColor", red.GetColor("_BaseColor") * RefusedTint);
            else red.color *= RefusedTint;
            RedMaterials[original] = red;
            return red;
        }

        private static Material TileMaterial(ref Material cached, Color colour)
        {
            if (cached != null) return cached;
            cached = new Material(RuntimeShaders.Unlit) { name = "PlacementTile", color = colour };
            cached.SetFloat("_Surface", 1f);
            cached.SetFloat("_Blend", 0f);
            cached.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            cached.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            cached.SetFloat("_ZWrite", 0f);
            cached.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            cached.renderQueue = (int)RenderQueue.Transparent;
            return cached;
        }
    }
}
