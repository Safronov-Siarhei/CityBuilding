using System.Collections.Generic;
using CityBuilder.Grid;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Maps
{
    /// <summary>
    /// Draws a map's decoration (MapDecorData) as a few dozen combined meshes, one per square
    /// chunk of the map, and keeps it out from under anything that takes a cell.
    ///
    /// Why chunks of combined meshes rather than one object per prop or GPU instancing:
    /// - thousands of renderers would cost far more CPU than the props are worth on a phone;
    /// - one mesh for the whole map could never be culled, and the camera only ever sees a few
    ///   chunks of it;
    /// - a chunk is cheap to rebuild, which is what hiding a prop needs: when a building, a road
    ///   tile, a tree or a boulder takes a cell (GridManager.OnAreaOccupancyChanged), the chunks
    ///   it touches are marked and rebuilt once at the end of the frame without the props in
    ///   occupied cells. A whole road drawn in one gesture is one rebuild per chunk, not per tile.
    ///
    /// The props cast no shadow (at 5-30 cm their shadows are noise, and casting would draw every
    /// chunk a second time) and have no collider: they are scenery, nothing ever hits them.
    /// </summary>
    public class MapDecorRenderer : MonoBehaviour
    {
        public static MapDecorRenderer Instance { get; private set; }

        // 25 m: an 8x8 grid on a 200 m map. The camera sees a handful of them at once, and one
        // rebuild touches a couple of hundred props at most.
        private const float ChunkSize = 25f;

        private MapDecorData _data;
        private Material _material;
        private GridManager _grid;

        private Vector3[][] _protoVertices;
        private Vector3[][] _protoNormals;
        private Vector2[][] _protoUvs;
        private int[][] _protoTriangles;

        private int _chunksX, _chunksZ;
        private Vector2 _origin;
        private List<int>[] _chunkProps;
        private Vector2Int[] _propCell;
        private Mesh[] _chunkMeshes;
        private bool[] _dirty;
        private bool _anyDirty;

        // Reused across rebuilds -- a rebuild allocates nothing but the mesh's own upload.
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Vector3> _normals = new List<Vector3>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly List<int> _triangles = new List<int>();

        /// <summary>How many props are drawn right now (all chunks) -- for the tests; counted on demand.</summary>
        public int VisibleCount
        {
            get
            {
                if (_data == null || _propCell == null) return 0;
                var n = 0;
                for (var i = 0; i < _data.Count; i++)
                {
                    if (IsShown(i)) n++;
                }
                return n;
            }
        }

        /// <summary>How many props the map has in all.</summary>
        public int TotalCount => _data != null ? _data.Count : 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (_grid != null) _grid.OnAreaOccupancyChanged -= HandleOccupancyChanged;
            if (Instance == this) Instance = null;
        }

        public void Initialize(MapDecorData data, Material material)
        {
            _data = data;
            _material = material;
            _grid = GridManager.Instance;
            if (_data == null || _material == null || _data.Count == 0) return;

            CachePrototypes();
            AssignPropsToChunks();
            for (var c = 0; c < _chunkMeshes.Length; c++) RebuildChunk(c);

            if (_grid != null) _grid.OnAreaOccupancyChanged += HandleOccupancyChanged;
        }

        /// <summary>True when the prop at this world position is drawn -- for the tests.</summary>
        public bool IsCellDecorVisible(Vector2Int cell)
        {
            if (_data == null) return false;
            for (var i = 0; i < _data.Count; i++)
            {
                if (_propCell[i] == cell && IsShown(i)) return true;
            }
            return false;
        }

        /// <summary>A cell that has at least one prop on it, nearest to `near` -- for the tests.</summary>
        public bool TryFindDecoratedCell(Vector2Int near, int radius, out Vector2Int found)
        {
            found = default;
            if (_data == null) return false;
            var best = int.MaxValue;
            for (var i = 0; i < _data.Count; i++)
            {
                var d = Mathf.Abs(_propCell[i].x - near.x) + Mathf.Abs(_propCell[i].y - near.y);
                if (d >= best || d > radius || !IsShown(i)) continue;
                best = d;
                found = _propCell[i];
            }
            return best != int.MaxValue;
        }

        private void CachePrototypes()
        {
            var count = _data.Prototypes.Length;
            _protoVertices = new Vector3[count][];
            _protoNormals = new Vector3[count][];
            _protoUvs = new Vector2[count][];
            _protoTriangles = new int[count][];
            for (var p = 0; p < count; p++)
            {
                var mesh = _data.Prototypes[p];
                if (mesh == null || !mesh.isReadable)
                {
                    _protoVertices[p] = new Vector3[0];
                    _protoNormals[p] = new Vector3[0];
                    _protoUvs[p] = new Vector2[0];
                    _protoTriangles[p] = new int[0];
                    continue;
                }
                _protoVertices[p] = mesh.vertices;
                _protoNormals[p] = mesh.normals;
                _protoUvs[p] = mesh.uv;
                _protoTriangles[p] = mesh.triangles;
            }
        }

        private void AssignPropsToChunks()
        {
            var cellSize = _grid != null ? _grid.CellSize : 1f;
            var size = _grid != null ? new Vector2(_grid.GridSize.x * cellSize, _grid.GridSize.y * cellSize) : new Vector2(200f, 200f);
            var originWorld = _grid != null ? _grid.CellToWorld(Vector2Int.zero) : new Vector3(-100f, 0f, -100f);
            _origin = new Vector2(originWorld.x, originWorld.z);
            _chunksX = Mathf.Max(1, Mathf.CeilToInt(size.x / ChunkSize));
            _chunksZ = Mathf.Max(1, Mathf.CeilToInt(size.y / ChunkSize));

            var chunkCount = _chunksX * _chunksZ;
            _chunkProps = new List<int>[chunkCount];
            _chunkMeshes = new Mesh[chunkCount];
            _dirty = new bool[chunkCount];
            for (var c = 0; c < chunkCount; c++) _chunkProps[c] = new List<int>();

            _propCell = new Vector2Int[_data.Count];
            for (var i = 0; i < _data.Count; i++)
            {
                var p = _data.PositionOf(i);
                _propCell[i] = _grid != null ? _grid.WorldToCell(p) : new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.z));
                _chunkProps[ChunkOf(p.x, p.z)].Add(i);
            }

            for (var c = 0; c < chunkCount; c++)
            {
                if (_chunkProps[c].Count == 0) continue;

                var go = new GameObject($"Decor_{c % _chunksX}_{c / _chunksX}", typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(transform, false);
                go.transform.position = Vector3.zero;
                var mesh = new Mesh { name = go.name };
                mesh.MarkDynamic();
                _chunkMeshes[c] = mesh;
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = _material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = true;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
        }

        private int ChunkOf(float x, float z)
        {
            var cx = Mathf.Clamp(Mathf.FloorToInt((x - _origin.x) / ChunkSize), 0, _chunksX - 1);
            var cz = Mathf.Clamp(Mathf.FloorToInt((z - _origin.y) / ChunkSize), 0, _chunksZ - 1);
            return cz * _chunksX + cx;
        }

        private bool IsShown(int prop)
        {
            return _grid == null || _grid.IsAreaFree(_propCell[prop], Vector2Int.one);
        }

        private void HandleOccupancyChanged(Vector2Int origin, Vector2Int size)
        {
            if (_grid == null || _chunkMeshes == null) return;

            var cellSize = _grid.CellSize;
            var a = _grid.CellToWorld(origin);
            var b = _grid.CellToWorld(origin + size);
            var c0 = ChunkOf(a.x, a.z);
            var c1 = ChunkOf(b.x - cellSize * 0.5f, b.z - cellSize * 0.5f);
            for (var cz = c0 / _chunksX; cz <= c1 / _chunksX; cz++)
            {
                for (var cx = c0 % _chunksX; cx <= c1 % _chunksX; cx++)
                {
                    _dirty[cz * _chunksX + cx] = true;
                    _anyDirty = true;
                }
            }
        }

        private void LateUpdate()
        {
            if (!_anyDirty) return;
            _anyDirty = false;
            for (var c = 0; c < _dirty.Length; c++)
            {
                if (!_dirty[c]) continue;
                _dirty[c] = false;
                RebuildChunk(c);
            }
        }

        private void RebuildChunk(int chunk)
        {
            var mesh = _chunkMeshes[chunk];
            if (mesh == null) return;

            _vertices.Clear();
            _normals.Clear();
            _uvs.Clear();
            _triangles.Clear();

            foreach (var i in _chunkProps[chunk])
            {
                if (!IsShown(i)) continue;

                var p = _data.PrototypeOf(i);
                var verts = _protoVertices[p];
                if (verts.Length == 0) continue;
                var normals = _protoNormals[p];
                var uvs = _protoUvs[p];
                var tris = _protoTriangles[p];

                var rotation = Quaternion.Euler(0f, _data.YawOf(i), 0f);
                var scale = _data.ScaleOf(i);
                var position = _data.PositionOf(i);
                var start = _vertices.Count;

                for (var v = 0; v < verts.Length; v++)
                {
                    _vertices.Add(position + rotation * (verts[v] * scale));
                    _normals.Add(v < normals.Length ? rotation * normals[v] : Vector3.up);
                    _uvs.Add(v < uvs.Length ? uvs[v] : Vector2.zero);
                }
                for (var t = 0; t < tris.Length; t++) _triangles.Add(start + tris[t]);
            }

            mesh.Clear();
            mesh.indexFormat = _vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(_vertices);
            mesh.SetNormals(_normals);
            mesh.SetUVs(0, _uvs);
            mesh.SetTriangles(_triangles, 0, true);
        }
    }
}
