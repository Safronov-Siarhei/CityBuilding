using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Grid;
using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>
    /// Simple cubic fog-of-war: the whole map is visible until the mandatory Town Hall is placed
    /// (see Activate, called from BuildingPlacer.TryPlace), then everything not yet explored is
    /// covered by cloud blocks. Buildings permanently clear a radius around themselves
    /// (BuildingData.fogRevealRadius, applied from BuildingInstance.Initialize -- the single hook
    /// point for both fresh placement and save/load, so no separate persistence is needed here).
    /// Citizens temporarily clear a radius around themselves that closes back up once they move on.
    ///
    /// Reveal bookkeeping is exact, at the same 1m grid resolution as gameplay; rendering is
    /// chunked (ChunkSize cells per chunk, one combined mesh each) and each chunk merges
    /// contiguous fogged cells along a row into a single box before triangulating, so a mesh
    /// rebuild -- which happens only for chunks whose reveal state actually changed -- stays cheap
    /// even for a mostly-fogged 200x200 map.
    /// </summary>
    public class FogOfWarManager : MonoBehaviour
    {
        public static FogOfWarManager Instance { get; private set; }

        [SerializeField] private BuildingPlacer buildingPlacer;
        [SerializeField] private CitizenVisualsManager citizenVisualsManager;

        private const int ChunkSize = 20;
        private const int CitizenRevealRadiusCells = 7;
        private const float CitizenUpdateInterval = 0.3f;
        private const float CloudBottomY = 0f;
        private const float CloudTopY = 6f;

        private bool[,] _permanent;
        private bool[,] _citizenRevealed;
        private bool[,] _chunkDirty;
        private GameObject[,] _chunkObjects;
        private MeshFilter[,] _chunkFilters;
        private Mesh[,] _chunkMeshes;
        private int _chunksX;
        private int _chunksZ;
        private bool _active;
        private float _citizenTimer;
        private Material _cloudMaterial;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            var grid = GridManager.Instance;
            if (grid == null) return;

            _permanent = new bool[grid.GridSize.x, grid.GridSize.y];
            _citizenRevealed = new bool[grid.GridSize.x, grid.GridSize.y];

            _chunksX = Mathf.CeilToInt(grid.GridSize.x / (float)ChunkSize);
            _chunksZ = Mathf.CeilToInt(grid.GridSize.y / (float)ChunkSize);
            _chunkDirty = new bool[_chunksX, _chunksZ];
            _chunkObjects = new GameObject[_chunksX, _chunksZ];
            _chunkFilters = new MeshFilter[_chunksX, _chunksZ];
            _chunkMeshes = new Mesh[_chunksX, _chunksZ];

            _cloudMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.93f, 0.95f, 0.97f) };

            for (var cx = 0; cx < _chunksX; cx++)
            {
                for (var cz = 0; cz < _chunksZ; cz++)
                {
                    var go = new GameObject($"FogChunk_{cx}_{cz}", typeof(MeshFilter), typeof(MeshRenderer));
                    go.transform.SetParent(transform, false);
                    go.GetComponent<MeshRenderer>().sharedMaterial = _cloudMaterial;
                    go.SetActive(false);
                    _chunkObjects[cx, cz] = go;
                    _chunkFilters[cx, cz] = go.GetComponent<MeshFilter>();
                }
            }
        }

        private void Start()
        {
            // A loaded save already has mandatoryFirstBuilding placed (see GameSaveController.
            // Awake -> MarkMandatoryBuildingAlreadyPlaced, guaranteed to run before this Start
            // since Unity runs every Awake before any Start) -- fog starts active immediately.
            _active = buildingPlacer == null || !buildingPlacer.IsPlacingMandatoryBuilding;
            if (_active) MarkAllChunksDirtyAndRebuild();
        }

        private void Update()
        {
            if (!_active) return;

            _citizenTimer -= Time.deltaTime;
            if (_citizenTimer <= 0f)
            {
                _citizenTimer = CitizenUpdateInterval;
                UpdateCitizenReveal();
            }

            RebuildDirtyChunks();
        }

        /// <summary>Turns fog on for the first time -- called once, right after the mandatory Town Hall is placed.</summary>
        public void Activate()
        {
            if (_active) return;
            _active = true;
            MarkAllChunksDirtyAndRebuild();
        }

        /// <summary>Permanently clears a circular radius (in grid cells) around center. Cheap no-op for cells already revealed.</summary>
        public void RevealPermanent(Vector2Int center, int radiusCells)
        {
            if (_permanent == null || radiusCells <= 0) return;
            var grid = GridManager.Instance;
            if (grid == null) return;

            var radiusSq = radiusCells * radiusCells;
            var minX = Mathf.Max(0, center.x - radiusCells);
            var maxX = Mathf.Min(grid.GridSize.x - 1, center.x + radiusCells);
            var minZ = Mathf.Max(0, center.y - radiusCells);
            var maxZ = Mathf.Min(grid.GridSize.y - 1, center.y + radiusCells);

            for (var x = minX; x <= maxX; x++)
            {
                for (var z = minZ; z <= maxZ; z++)
                {
                    var dx = x - center.x;
                    var dz = z - center.y;
                    if (dx * dx + dz * dz > radiusSq) continue;
                    if (_permanent[x, z]) continue;
                    _permanent[x, z] = true;
                    MarkChunkDirty(x, z);
                }
            }
        }

        private void UpdateCitizenReveal()
        {
            var grid = GridManager.Instance;
            if (grid == null) return;

            var fresh = new bool[grid.GridSize.x, grid.GridSize.y];
            var agents = citizenVisualsManager != null ? citizenVisualsManager.AllAgents : null;
            if (agents != null)
            {
                foreach (var agent in agents)
                {
                    if (agent == null) continue;
                    var cell = grid.WorldToCell(agent.transform.position);
                    RevealCircleInto(fresh, cell, CitizenRevealRadiusCells, grid);
                }
            }

            for (var x = 0; x < grid.GridSize.x; x++)
            {
                for (var z = 0; z < grid.GridSize.y; z++)
                {
                    if (fresh[x, z] == _citizenRevealed[x, z]) continue;
                    MarkChunkDirty(x, z);
                }
            }

            _citizenRevealed = fresh;
        }

        private static void RevealCircleInto(bool[,] target, Vector2Int center, int radiusCells, GridManager grid)
        {
            var radiusSq = radiusCells * radiusCells;
            var minX = Mathf.Max(0, center.x - radiusCells);
            var maxX = Mathf.Min(grid.GridSize.x - 1, center.x + radiusCells);
            var minZ = Mathf.Max(0, center.y - radiusCells);
            var maxZ = Mathf.Min(grid.GridSize.y - 1, center.y + radiusCells);

            for (var x = minX; x <= maxX; x++)
            {
                for (var z = minZ; z <= maxZ; z++)
                {
                    var dx = x - center.x;
                    var dz = z - center.y;
                    if (dx * dx + dz * dz <= radiusSq) target[x, z] = true;
                }
            }
        }

        private bool IsRevealed(int x, int z) => _permanent[x, z] || _citizenRevealed[x, z];

        private void MarkChunkDirty(int x, int z)
        {
            if (_chunkDirty == null) return;
            _chunkDirty[x / ChunkSize, z / ChunkSize] = true;
        }

        private void MarkAllChunksDirtyAndRebuild()
        {
            for (var cx = 0; cx < _chunksX; cx++)
            {
                for (var cz = 0; cz < _chunksZ; cz++)
                {
                    _chunkDirty[cx, cz] = true;
                }
            }
            RebuildDirtyChunks();
        }

        private void RebuildDirtyChunks()
        {
            for (var cx = 0; cx < _chunksX; cx++)
            {
                for (var cz = 0; cz < _chunksZ; cz++)
                {
                    if (_chunkDirty[cx, cz]) RebuildChunk(cx, cz);
                }
            }
        }

        /// <summary>Rebuilds one chunk's combined mesh from scratch: contiguous fogged cells along each row are merged into a single box before triangulating, keeping a mostly- or fully-fogged chunk far cheaper than one box per cell.</summary>
        private void RebuildChunk(int cx, int cz)
        {
            var grid = GridManager.Instance;
            var x0 = cx * ChunkSize;
            var z0 = cz * ChunkSize;
            var x1 = Mathf.Min(x0 + ChunkSize, grid.GridSize.x);
            var z1 = Mathf.Min(z0 + ChunkSize, grid.GridSize.y);

            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            for (var z = z0; z < z1; z++)
            {
                var runStartX = -1;
                for (var x = x0; x <= x1; x++)
                {
                    var fogged = x < x1 && !IsRevealed(x, z);
                    if (fogged && runStartX < 0) runStartX = x;
                    if (!fogged && runStartX >= 0)
                    {
                        AddCloudRow(vertices, triangles, grid, runStartX, x, z);
                        runStartX = -1;
                    }
                }
            }

            var mesh = _chunkMeshes[cx, cz];
            if (mesh == null)
            {
                mesh = new Mesh { name = $"FogChunk_{cx}_{cz}" };
                _chunkMeshes[cx, cz] = mesh;
                _chunkFilters[cx, cz].sharedMesh = mesh;
            }

            mesh.Clear();
            if (vertices.Count > 0)
            {
                mesh.indexFormat = vertices.Count > 60000
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16;
                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
            }
            _chunkObjects[cx, cz].SetActive(vertices.Count > 0);
            _chunkDirty[cx, cz] = false;
        }

        private static void AddCloudRow(List<Vector3> vertices, List<int> triangles, GridManager grid, int xStart, int xEndExclusive, int z)
        {
            var corner = grid.CellToWorld(new Vector2Int(xStart, z));
            var x0 = corner.x;
            var x1 = corner.x + (xEndExclusive - xStart) * grid.CellSize;
            var z0 = corner.z;
            var z1 = corner.z + grid.CellSize;
            AddCloudBox(vertices, triangles, x0, x1, z0, z1);
        }

        /// <summary>Top + four sides (no bottom -- never seen). Each face is emitted with both winding orders so it renders regardless of which one Unity treats as front-facing, without needing shared vertices (which would otherwise average opposing normals to near-zero).</summary>
        private static void AddCloudBox(List<Vector3> vertices, List<int> triangles, float x0, float x1, float z0, float z1)
        {
            AddQuadBothSides(vertices, triangles, new Vector3(x0, CloudTopY, z0), new Vector3(x0, CloudTopY, z1), new Vector3(x1, CloudTopY, z1), new Vector3(x1, CloudTopY, z0));
            AddQuadBothSides(vertices, triangles, new Vector3(x0, CloudBottomY, z1), new Vector3(x0, CloudTopY, z1), new Vector3(x1, CloudTopY, z1), new Vector3(x1, CloudBottomY, z1));
            AddQuadBothSides(vertices, triangles, new Vector3(x1, CloudBottomY, z0), new Vector3(x1, CloudTopY, z0), new Vector3(x0, CloudTopY, z0), new Vector3(x0, CloudBottomY, z0));
            AddQuadBothSides(vertices, triangles, new Vector3(x1, CloudBottomY, z1), new Vector3(x1, CloudTopY, z1), new Vector3(x1, CloudTopY, z0), new Vector3(x1, CloudBottomY, z0));
            AddQuadBothSides(vertices, triangles, new Vector3(x0, CloudBottomY, z0), new Vector3(x0, CloudTopY, z0), new Vector3(x0, CloudTopY, z1), new Vector3(x0, CloudBottomY, z1));
        }

        private static void AddQuadBothSides(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            AddFace(vertices, triangles, a, b, c, d);
            AddFace(vertices, triangles, a, d, c, b);
        }

        private static void AddFace(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var baseIndex = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 3);
        }
    }
}
