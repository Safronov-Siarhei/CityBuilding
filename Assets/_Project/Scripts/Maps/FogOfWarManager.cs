using CityBuilder.Core;
using System;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Grid;
using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>
    /// Fog of war: the whole map is visible until the mandatory Town Hall is placed (see Activate,
    /// called from BuildingPlacer.TryPlace), then the land not yet explored lies under cloud.
    /// Buildings permanently clear a radius around themselves (BuildingData.fogRevealRadius,
    /// applied from BuildingInstance.Initialize -- the single hook point for both fresh placement
    /// and save/load, so no separate persistence is needed here). Citizens temporarily clear a
    /// radius around themselves that closes back up once they move on.
    ///
    /// **How it looks.** It used to be a flat black lid hovering 5 m over the map, cut to the 1 m
    /// grid: the explored land read as an island floating in a void, its edge was a staircase,
    /// the mountains poked through it, and land appeared and vanished in one frame. The user found
    /// it crude and cheap and chose clouds (2026-10-14). Now:
    ///
    /// - a cloud BLANKET follows the land's shape a few metres above it (<see cref="Clearance"/>),
    ///   so hills are covered like the plain and nothing pokes through;
    /// - its shader (Shaders/FogClouds.shader) draws drifting white cloud with a lumpy edge, and
    ///   reads the reveal state from a one-texel-per-cell mask where the view ray meets the
    ///   GROUND, so the holes sit exactly over the explored land instead of sliding towards the
    ///   camera as the lid's did;
    /// - the mask eases every cell from fogged to revealed and back over <see cref="RevealSeconds"/>,
    ///   so new land clears gradually.
    ///
    /// **What it costs.** Reveal bookkeeping is exact, at the gameplay grid's resolution, and runs
    /// as before. The mask is 1 byte a cell and is uploaded only on frames where some cell is still
    /// easing. The blanket is cut into chunks, and a chunk with no fog anywhere near it is switched
    /// off -- the explored town, which is where the camera spends its time, draws no cloud at all.
    /// Transparent, unlit, no shadows.
    ///
    /// **Two looks, the player's choice** (GameSettings.FogStyle, in the main menu's settings):
    /// the clouds, or an old map on parchment (Shaders/FogParchment.shader) with the land under it
    /// drawn in ink. Both draw on the same blanket from the same mask; the parchment also reads a
    /// chart of the map (<see cref="BuildChart"/>), made once, the first time it is shown.
    /// </summary>
    public class FogOfWarManager : MonoBehaviour
    {
        public static FogOfWarManager Instance { get; private set; }

        [SerializeField] private BuildingPlacer buildingPlacer;
        [SerializeField] private CitizenVisualsManager citizenVisualsManager;

        /// <summary>The cloud material (Shaders/FogClouds.shader). Copied at runtime, so the asset is never written to.</summary>
        [SerializeField] private Material cloudMaterial;

        /// <summary>The parchment material (Shaders/FogParchment.shader). Copied at runtime, like the clouds.</summary>
        [SerializeField] private Material parchmentMaterial;

        /// <summary>The metres of height the chart's height channel spans above the plain -- Map 3's tallest mountain with room to spare.</summary>
        private const float ChartHeightScale = 32f;

        /// <summary>How long the parchment waits for the trees and rocks to be placed before charting the map without them.</summary>
        private const float ChartWaitSeconds = 3f;

        private const int ChunkSize = 20;
        private const int CitizenRevealRadiusCells = 7;
        private const float CitizenUpdateInterval = 0.3f;

        /// <summary>How far the blanket hangs above the highest land near it: over the portals (3.3 m) and every tree, under the camera at its closest zoom on the plain (3.8 m).</summary>
        public const float Clearance = 3.5f;

        /// <summary>How long a cell takes to clear (or cloud over again).</summary>
        public const float RevealSeconds = 0.8f;

        /// <summary>Chunks of cloud beyond the map's edge on every side: the camera can look about 40 m past it at its widest zoom.</summary>
        private const int MarginChunks = 2;

        /// <summary>The blanket's vertex spacing -- fine enough to follow the hills, coarse enough to be a few hundred vertices a chunk.</summary>
        private const float LatticeStep = 2f;

        /// <summary>A chunk stays on while any fog lies this many cells around it: the cloud's edge reaches past the fog by the mask blur, the noise and the view-ray offset.</summary>
        private const int ActivityMarginCells = 8;

        private const float RecheckInterval = 0.5f;

        /// <summary>The mask blur radius, in metres.</summary>
        private const float MaskBlurMeters = 2.2f;

        private bool[,] _permanent;
        private bool[,] _citizenRevealed;
        // Ping-ponged with _citizenRevealed in UpdateCitizenReveal instead of allocating a fresh
        // grid-sized array every 0.3s (CitizenUpdateInterval) -- on a 200x200 map that was a 40KB
        // GC allocation several times a second for the life of the session, a steady source of GC
        // pressure/stutter that has nothing to do with how many citizens are actually on screen.
        private bool[,] _citizenRevealedScratch;

        // What the clouds show, per cell, 0 (fog) .. 255 (revealed), eased towards IsRevealed.
        private byte[] _display;
        private bool[] _easing;
        private readonly List<int> _easingCells = new List<int>();
        private Texture2D _mask;
        private bool _maskDirty;

        private int _width;
        private int _depth;
        private int _chunksX;
        private int _chunksZ;
        private GameObject _cloudRoot;
        private GameObject[,] _chunkObjects;
        private MeshFilter[,] _chunkFilters;
        private bool[,] _chunkRecheck;
        private bool _anyRecheck;
        private float _recheckTimer;
        private Material _cloudMaterialInstance;
        private Material _parchmentMaterialInstance;
        private Texture2D _chart;
        private bool _chartBuilt;
        private float _chartWait;
        private FogStyle _style;
        private bool _listeningToSettings;

        private bool _active;
        private bool _initialized;
        private bool _blanketBuilt;
        private bool _needsSnap;
        private float _citizenTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            EnsureInitialized();
        }

        /// <summary>
        /// Sets up the reveal grids and chunk GameObjects the first time GridManager.Instance is
        /// actually available. Component Awake() order across different GameObjects is NOT
        /// something Unity guarantees (only "all Awakes before any Start" is guaranteed) --
        /// relying on add-order alone let this run before GridManager.Awake() had set Instance,
        /// leaving every array here null and throwing a NullReferenceException the first time
        /// anything touched them. Idempotent and cheap once done, so it's safe to call from every
        /// entry point (Awake, Start, Update, Activate, RevealPermanent) as a self-healing guard.
        /// </summary>
        private void EnsureInitialized()
        {
            if (_initialized) return;
            var grid = GridManager.Instance;
            if (grid == null) return;
            _initialized = true;

            _width = grid.GridSize.x;
            _depth = grid.GridSize.y;
            _permanent = new bool[_width, _depth];
            _citizenRevealed = new bool[_width, _depth];
            _citizenRevealedScratch = new bool[_width, _depth];
            _display = new byte[_width * _depth];
            _easing = new bool[_width * _depth];

            _mask = new Texture2D(_width, _depth, TextureFormat.R8, false, true)
            {
                name = "FogRevealMask",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            _chunksX = Mathf.CeilToInt(_width / (float)ChunkSize) + MarginChunks * 2;
            _chunksZ = Mathf.CeilToInt(_depth / (float)ChunkSize) + MarginChunks * 2;
            _chunkObjects = new GameObject[_chunksX, _chunksZ];
            _chunkFilters = new MeshFilter[_chunksX, _chunksZ];
            _chunkRecheck = new bool[_chunksX, _chunksZ];

            _cloudMaterialInstance = RuntimeCopy(cloudMaterial, "CityBuilder/FogClouds", "FogClouds (runtime)");
            _parchmentMaterialInstance = RuntimeCopy(parchmentMaterial, "CityBuilder/FogParchment", "FogParchment (runtime)");
            ConfigureMaterial(_cloudMaterialInstance, grid);
            ConfigureMaterial(_parchmentMaterialInstance, grid);
            if (_parchmentMaterialInstance != null) _parchmentMaterialInstance.SetFloat("_HeightScale", ChartHeightScale);
            _style = GameSettings.FogStyle;

            // World-aligned and unscaled whatever this component's object is, because the chunk
            // meshes are written in world coordinates.
            _cloudRoot = new GameObject("FogClouds");
            _cloudRoot.transform.SetParent(transform, false);
            _cloudRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _cloudRoot.SetActive(false);

            for (var cx = 0; cx < _chunksX; cx++)
            {
                for (var cz = 0; cz < _chunksZ; cz++)
                {
                    var go = new GameObject($"FogChunk_{cx}_{cz}", typeof(MeshFilter), typeof(MeshRenderer));
                    go.transform.SetParent(_cloudRoot.transform, false);
                    var chunkRenderer = go.GetComponent<MeshRenderer>();
                    chunkRenderer.sharedMaterial = CurrentMaterial;
                    // Cloud lying over ground the player cannot see: its shadow would fall on
                    // hidden ground and cost a second draw of every chunk -- see SceneryShadows.
                    chunkRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    chunkRenderer.receiveShadows = false;
                    chunkRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                    chunkRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                    go.SetActive(false);
                    _chunkObjects[cx, cz] = go;
                    _chunkFilters[cx, cz] = go.GetComponent<MeshFilter>();
                }
            }
        }

        private static Material RuntimeCopy(Material asset, string shaderName, string name)
        {
            var source = asset;
            if (source == null)
            {
                var shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    Debug.LogError($"[FogOfWar] The shader '{shaderName}' is not in this build -- that look of the fog will not be drawn.");
                    return null;
                }
                source = new Material(shader);
            }
            return new Material(source) { name = name };
        }

        private void ConfigureMaterial(Material material, GridManager grid)
        {
            if (material == null) return;
            var origin = grid.CellToWorld(Vector2Int.zero);
            var sizeX = _width * grid.CellSize;
            var sizeZ = _depth * grid.CellSize;
            material.SetTexture("_MaskTex", _mask);
            material.SetVector("_MapRect", new Vector4(origin.x, origin.z, 1f / sizeX, 1f / sizeZ));
            material.SetVector("_MaskStep", new Vector4(MaskBlurMeters / sizeX, MaskBlurMeters / sizeZ, 0f, 0f));
            material.SetFloat("_Clearance", Clearance);
        }

        /// <summary>The look in use, falling back to the other one if its shader is missing from the build.</summary>
        private Material CurrentMaterial =>
            _style == FogStyle.Parchment && _parchmentMaterialInstance != null
                ? _parchmentMaterialInstance
                : _cloudMaterialInstance != null ? _cloudMaterialInstance : _parchmentMaterialInstance;

        public FogStyle Style => _style;

        private void ApplyStyle()
        {
            if (!_initialized) return;
            _style = GameSettings.FogStyle;
            var material = CurrentMaterial;
            for (var cx = 0; cx < _chunksX; cx++)
            {
                for (var cz = 0; cz < _chunksZ; cz++)
                {
                    _chunkObjects[cx, cz].GetComponent<MeshRenderer>().sharedMaterial = material;
                }
            }
        }

        private void OnEnable()
        {
            if (_listeningToSettings) return;
            GameSettings.OnChanged += ApplyStyle;
            _listeningToSettings = true;
            ApplyStyle();
        }

        private void OnDisable()
        {
            if (!_listeningToSettings) return;
            GameSettings.OnChanged -= ApplyStyle;
            _listeningToSettings = false;
        }

        private void Start()
        {
            EnsureInitialized();
            // A loaded save already has mandatoryFirstBuilding placed (see GameSaveController.
            // Awake -> MarkMandatoryBuildingAlreadyPlaced, guaranteed to run before this Start
            // since Unity runs every Awake before any Start) -- fog starts active immediately.
            if (buildingPlacer == null || !buildingPlacer.IsPlacingMandatoryBuilding) Activate();
        }

        private void Update()
        {
            if (!_active) return;
            EnsureInitialized();
            if (!_initialized) return;

            // Here rather than in Activate: the land's meshes exist once MeshMapApplier.Start has
            // run, and every Start runs before the first Update.
            if (!_blanketBuilt) BuildBlanket();
            if (_needsSnap) Snap();

            // The chart is wanted only by the parchment, and only once: when the player first
            // sees it, after the trees and rocks are standing (or once it is clear none will be).
            if (_style == FogStyle.Parchment && !_chartBuilt)
            {
                _chartWait += Time.deltaTime;
                if (ResourceNode.All.Count > 0 || _chartWait >= ChartWaitSeconds) BuildChart();
            }

            _citizenTimer -= Time.deltaTime;
            if (_citizenTimer <= 0f)
            {
                _citizenTimer = CitizenUpdateInterval;
                UpdateCitizenReveal();
            }

            Ease(Time.deltaTime);

            _recheckTimer -= Time.deltaTime;
            if (_anyRecheck && _recheckTimer <= 0f)
            {
                _recheckTimer = RecheckInterval;
                RecheckChunks();
            }

            if (_maskDirty) UploadMask();
        }

        /// <summary>Turns fog on for the first time -- called once, right after the mandatory Town Hall is placed.</summary>
        public void Activate()
        {
            EnsureInitialized();
            if (_active) return;
            _active = true;
            // Whatever is known when the fog first appears is shown as it is, without easing: a
            // map that starts by un-clouding the Town Hall's surroundings would be a show nobody
            // asked for.
            _needsSnap = true;
        }

        /// <summary>Whether this cell is open to view right now (a building's permanent reveal, or a citizen nearby).</summary>
        public bool IsCellRevealed(Vector2Int cell)
        {
            if (!_initialized || cell.x < 0 || cell.y < 0 || cell.x >= _width || cell.y >= _depth) return false;
            return IsRevealed(cell.x, cell.y);
        }

        /// <summary>How clear of cloud this cell is drawn right now, 0..1 -- the eased value the shader sees.</summary>
        public float DisplayedClearness(Vector2Int cell)
        {
            if (!_initialized || cell.x < 0 || cell.y < 0 || cell.x >= _width || cell.y >= _depth) return 0f;
            return _display[cell.y * _width + cell.x] / 255f;
        }

        /// <summary>Permanently clears a circular radius (in grid cells) around center. Cheap no-op for cells already revealed.</summary>
        public void RevealPermanent(Vector2Int center, int radiusCells)
        {
            EnsureInitialized();
            if (_permanent == null || radiusCells <= 0) return;

            var radiusSq = radiusCells * radiusCells;
            var minX = Mathf.Max(0, center.x - radiusCells);
            var maxX = Mathf.Min(_width - 1, center.x + radiusCells);
            var minZ = Mathf.Max(0, center.y - radiusCells);
            var maxZ = Mathf.Min(_depth - 1, center.y + radiusCells);

            for (var x = minX; x <= maxX; x++)
            {
                for (var z = minZ; z <= maxZ; z++)
                {
                    var dx = x - center.x;
                    var dz = z - center.y;
                    if (dx * dx + dz * dz > radiusSq) continue;
                    if (_permanent[x, z]) continue;
                    _permanent[x, z] = true;
                    MarkCellChanged(x, z);
                }
            }
        }

        /// <summary>
        /// Something else that clears the fog round itself while it is there and lets it close
        /// again behind it, as a citizen does -- the Invasion's King (the user, 2026-10-22).
        /// </summary>
        public void AddWalker(Transform walker, int radiusCells)
        {
            if (walker == null) return;
            _walkers.RemoveAll(w => w.Transform == walker);
            _walkers.Add((walker, radiusCells));
        }

        public void RemoveWalker(Transform walker) => _walkers.RemoveAll(w => w.Transform == walker);

        private readonly System.Collections.Generic.List<(Transform Transform, int Radius)> _walkers = new System.Collections.Generic.List<(Transform, int)>();

        private void UpdateCitizenReveal()
        {
            var grid = GridManager.Instance;
            if (grid == null) return;

            var fresh = _citizenRevealedScratch;
            Array.Clear(fresh, 0, fresh.Length);

            var agents = citizenVisualsManager != null ? citizenVisualsManager.AllAgents : null;
            if (agents != null)
            {
                foreach (var agent in agents)
                {
                    if (agent == null) continue;
                    var cell = grid.WorldToCell(agent.transform.position);
                    RevealCircleInto(fresh, cell, CitizenRevealRadiusCells, _width, _depth);
                }
            }
            foreach (var walker in _walkers)
            {
                if (walker.Transform == null || !walker.Transform.gameObject.activeInHierarchy) continue;
                RevealCircleInto(fresh, grid.WorldToCell(walker.Transform.position), walker.Radius, _width, _depth);
            }

            for (var x = 0; x < _width; x++)
            {
                for (var z = 0; z < _depth; z++)
                {
                    if (fresh[x, z] == _citizenRevealed[x, z]) continue;
                    MarkCellChanged(x, z);
                }
            }

            // Swap references instead of assigning a newly allocated array -- the old
            // _citizenRevealed becomes next tick's scratch buffer, so this never allocates.
            _citizenRevealedScratch = _citizenRevealed;
            _citizenRevealed = fresh;
        }

        private static void RevealCircleInto(bool[,] target, Vector2Int center, int radiusCells, int width, int depth)
        {
            var radiusSq = radiusCells * radiusCells;
            var minX = Mathf.Max(0, center.x - radiusCells);
            var maxX = Mathf.Min(width - 1, center.x + radiusCells);
            var minZ = Mathf.Max(0, center.y - radiusCells);
            var maxZ = Mathf.Min(depth - 1, center.y + radiusCells);

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

        // ---------------------------------------------------------------- easing the mask

        private void MarkCellChanged(int x, int z)
        {
            if (_easing == null) return;
            var index = z * _width + x;
            if (_easing[index]) return;
            _easing[index] = true;
            _easingCells.Add(index);
        }

        /// <summary>Moves every changing cell one frame's worth towards what it should show.</summary>
        private void Ease(float deltaTime)
        {
            if (_easingCells.Count == 0) return;

            var step = Mathf.Max(1, Mathf.RoundToInt(deltaTime * 255f / RevealSeconds));
            for (var i = _easingCells.Count - 1; i >= 0; i--)
            {
                var index = _easingCells[i];
                var x = index % _width;
                var z = index / _width;
                var target = IsRevealed(x, z) ? 255 : 0;
                int shown = _display[index];

                if (shown == 255 && target == 0) ActivateChunksAround(x, z);
                shown = shown < target ? Mathf.Min(target, shown + step) : Mathf.Max(target, shown - step);
                _display[index] = (byte)shown;
                if (shown == 255) RequestRecheckAround(x, z);

                if (shown != target) continue;
                _easing[index] = false;
                var last = _easingCells.Count - 1;
                _easingCells[i] = _easingCells[last];
                _easingCells.RemoveAt(last);
            }
            _maskDirty = true;
        }

        /// <summary>Shows every cell as it is now, with no easing -- when the fog first comes on.</summary>
        private void Snap()
        {
            _needsSnap = false;
            for (var x = 0; x < _width; x++)
            {
                for (var z = 0; z < _depth; z++)
                {
                    _display[z * _width + x] = IsRevealed(x, z) ? (byte)255 : (byte)0;
                }
            }
            foreach (var index in _easingCells) _easing[index] = false;
            _easingCells.Clear();

            for (var cx = 0; cx < _chunksX; cx++)
            {
                for (var cz = 0; cz < _chunksZ; cz++) _chunkRecheck[cx, cz] = true;
            }
            _anyRecheck = true;
            RecheckChunks();
            _maskDirty = true;
            if (_cloudRoot != null) _cloudRoot.SetActive(true);
        }

        private void UploadMask()
        {
            _maskDirty = false;
            _mask.SetPixelData(_display, 0);
            _mask.Apply(false, false);
        }

        // ---------------------------------------------------------------- which chunks draw

        private void ChunkRangeAround(int x, int z, out int cx0, out int cx1, out int cz0, out int cz1)
        {
            // Floored, not truncated: a margin reaching below cell 0 belongs to the chunk before it.
            cx0 = Mathf.Max(0, Mathf.FloorToInt((x - ActivityMarginCells) / (float)ChunkSize) + MarginChunks);
            cx1 = Mathf.Min(_chunksX - 1, Mathf.FloorToInt((x + ActivityMarginCells) / (float)ChunkSize) + MarginChunks);
            cz0 = Mathf.Max(0, Mathf.FloorToInt((z - ActivityMarginCells) / (float)ChunkSize) + MarginChunks);
            cz1 = Mathf.Min(_chunksZ - 1, Mathf.FloorToInt((z + ActivityMarginCells) / (float)ChunkSize) + MarginChunks);
        }

        /// <summary>A cell is clouding over: every chunk that may draw its cloud must be on now, not at the next recheck.</summary>
        private void ActivateChunksAround(int x, int z)
        {
            ChunkRangeAround(x, z, out var cx0, out var cx1, out var cz0, out var cz1);
            for (var cx = cx0; cx <= cx1; cx++)
            {
                for (var cz = cz0; cz <= cz1; cz++) SetChunkActive(cx, cz, true);
            }
        }

        /// <summary>A cell has cleared completely: the chunks around it may have nothing left to draw.</summary>
        private void RequestRecheckAround(int x, int z)
        {
            ChunkRangeAround(x, z, out var cx0, out var cx1, out var cz0, out var cz1);
            for (var cx = cx0; cx <= cx1; cx++)
            {
                for (var cz = cz0; cz <= cz1; cz++) _chunkRecheck[cx, cz] = true;
            }
            _anyRecheck = true;
        }

        private void RecheckChunks()
        {
            _anyRecheck = false;
            for (var cx = 0; cx < _chunksX; cx++)
            {
                for (var cz = 0; cz < _chunksZ; cz++)
                {
                    if (!_chunkRecheck[cx, cz]) continue;
                    _chunkRecheck[cx, cz] = false;
                    SetChunkActive(cx, cz, ChunkHasFogNearby(cx, cz));
                }
            }
        }

        private bool ChunkHasFogNearby(int cx, int cz)
        {
            var x0 = (cx - MarginChunks) * ChunkSize - ActivityMarginCells;
            var z0 = (cz - MarginChunks) * ChunkSize - ActivityMarginCells;
            var x1 = x0 + ChunkSize + ActivityMarginCells * 2;
            var z1 = z0 + ChunkSize + ActivityMarginCells * 2;

            // Beyond the map is cloud for good.
            if (x0 < 0 || z0 < 0 || x1 > _width || z1 > _depth) return true;

            for (var z = z0; z < z1; z++)
            {
                var row = z * _width;
                for (var x = x0; x < x1; x++)
                {
                    if (_display[row + x] < 255) return true;
                }
            }
            return false;
        }

        private void SetChunkActive(int cx, int cz, bool active)
        {
            var go = _chunkObjects[cx, cz];
            if (go.activeSelf != active) go.SetActive(active);
        }

        // ---------------------------------------------------------------- the blanket

        /// <summary>
        /// Builds every chunk's mesh: a lattice <see cref="LatticeStep"/> metres apart, each point
        /// <see cref="Clearance"/> above the highest land around it. The land's height comes from
        /// its own meshes' vertices, binned once -- no physics query per point, and nothing that
        /// depends on colliders having been baked yet.
        /// </summary>
        private void BuildBlanket()
        {
            _blanketBuilt = true;
            var grid = GridManager.Instance;
            var chunkWorld = ChunkSize * grid.CellSize;
            var steps = Mathf.Max(1, Mathf.RoundToInt(chunkWorld / LatticeStep));
            var step = chunkWorld / steps;
            var origin = grid.CellToWorld(Vector2Int.zero) - new Vector3(MarginChunks * chunkWorld, 0f, MarginChunks * chunkWorld);
            var nodesX = _chunksX * steps + 1;
            var nodesZ = _chunksZ * steps + 1;

            var heights = LandHeights(origin, step, nodesX, nodesZ, grid.GroundHeight);

            var vertices = new List<Vector3>((steps + 1) * (steps + 1));
            var triangles = new List<int>(steps * steps * 6);
            for (var cx = 0; cx < _chunksX; cx++)
            {
                for (var cz = 0; cz < _chunksZ; cz++)
                {
                    vertices.Clear();
                    triangles.Clear();
                    for (var j = 0; j <= steps; j++)
                    {
                        for (var i = 0; i <= steps; i++)
                        {
                            var nx = cx * steps + i;
                            var nz = cz * steps + j;
                            vertices.Add(new Vector3(origin.x + nx * step, heights[nx, nz], origin.z + nz * step));
                        }
                    }
                    for (var j = 0; j < steps; j++)
                    {
                        for (var i = 0; i < steps; i++)
                        {
                            var a = j * (steps + 1) + i;
                            var b = a + steps + 1;
                            triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                            triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
                        }
                    }

                    var mesh = new Mesh { name = $"FogChunk_{cx}_{cz}" };
                    mesh.SetVertices(vertices);
                    mesh.SetTriangles(triangles, 0);
                    mesh.RecalculateBounds();
                    _chunkFilters[cx, cz].sharedMesh = mesh;
                }
            }
        }

        /// <summary>The blanket's height at every lattice node: the highest land within two nodes, plus the clearance, smoothed once so the cloud has no creases.</summary>
        private static float[,] LandHeights(Vector3 origin, float step, int nodesX, int nodesZ, float groundHeight)
        {
            var highest = new float[nodesX, nodesZ];
            for (var x = 0; x < nodesX; x++)
            {
                for (var z = 0; z < nodesZ; z++) highest[x, z] = groundHeight;
            }

            var meshes = new List<MeshFilter>();
            if (MeshMapApplier.Instance != null) MeshMapApplier.Instance.CollectTerrainMeshes(meshes);
            foreach (var filter in meshes)
            {
                var mesh = filter.sharedMesh;
                if (mesh == null || !mesh.isReadable) continue;

                var toWorld = filter.transform.localToWorldMatrix;
                foreach (var local in mesh.vertices)
                {
                    var p = toWorld.MultiplyPoint3x4(local);
                    var nx = Mathf.RoundToInt((p.x - origin.x) / step);
                    var nz = Mathf.RoundToInt((p.z - origin.z) / step);
                    if (nx < 0 || nz < 0 || nx >= nodesX || nz >= nodesZ) continue;
                    if (p.y > highest[nx, nz]) highest[nx, nz] = p.y;
                }
            }

            // Two nodes either way: a peak between lattice points must not slip under the cloud.
            var grown = new float[nodesX, nodesZ];
            for (var x = 0; x < nodesX; x++)
            {
                for (var z = 0; z < nodesZ; z++)
                {
                    var top = groundHeight;
                    for (var dx = -2; dx <= 2; dx++)
                    {
                        for (var dz = -2; dz <= 2; dz++)
                        {
                            var sx = x + dx;
                            var sz = z + dz;
                            if (sx < 0 || sz < 0 || sx >= nodesX || sz >= nodesZ) continue;
                            if (highest[sx, sz] > top) top = highest[sx, sz];
                        }
                    }
                    grown[x, z] = top;
                }
            }

            var heights = new float[nodesX, nodesZ];
            for (var x = 0; x < nodesX; x++)
            {
                for (var z = 0; z < nodesZ; z++)
                {
                    var sum = 0f;
                    var count = 0;
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        for (var dz = -1; dz <= 1; dz++)
                        {
                            var sx = x + dx;
                            var sz = z + dz;
                            if (sx < 0 || sz < 0 || sx >= nodesX || sz >= nodesZ) continue;
                            sum += grown[sx, sz];
                            count++;
                        }
                    }
                    heights[x, z] = sum / count + Clearance;
                }
            }
            return heights;
        }

        // ---------------------------------------------------------------- the chart

        /// <summary>Whether the parchment's chart of the map has been drawn.</summary>
        public bool ChartBuilt => _chartBuilt;

        /// <summary>The chart's value at a cell, 0..1 per channel: water, height above the plain / ChartHeightScale, forest, stony ground. For tests.</summary>
        public Color ChartAt(Vector2Int cell)
        {
            return _chart != null ? _chart.GetPixel(cell.x, cell.y) : Color.clear;
        }

        /// <summary>
        /// A survey of the map for the parchment to draw from, one texel per cell, made once. Every
        /// channel is softened a little after it is filled, so the 0.5 contour the shader inks is a
        /// smooth curve rather than the cell grid's staircase.
        ///
        /// Height comes from rasterising the land's own triangles -- exact at every cell, where
        /// binning vertices would leave holes wherever the plain's large flat triangles have none.
        /// Forests and stony ground come from where the trees and rocks actually stand.
        /// </summary>
        private void BuildChart()
        {
            _chartBuilt = true;
            var grid = GridManager.Instance;
            var cells = _width * _depth;
            var water = new float[cells];
            var height = new float[cells];
            var forest = new float[cells];
            var stone = new float[cells];

            var applier = MeshMapApplier.Instance;
            if (applier != null)
            {
                for (var z = 0; z < _depth; z++)
                {
                    for (var x = 0; x < _width; x++)
                    {
                        if (applier.IsWaterCell(new Vector2Int(x, z))) water[z * _width + x] = 1f;
                    }
                }

                var meshes = new List<MeshFilter>();
                applier.CollectTerrainMeshes(meshes);
                foreach (var filter in meshes) RasteriseHeights(filter, grid, height);
            }

            foreach (var node in ResourceNode.All)
            {
                if (node == null) continue;
                var target = node.ResourceType == CityBuilder.Resources.ResourceType.Wood ? forest
                    : node.ResourceType == CityBuilder.Resources.ResourceType.Stone ? stone : null;
                if (target == null) continue;

                var cell = grid.WorldToCell(node.transform.position);
                for (var dz = -1; dz <= 1; dz++)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        var x = cell.x + dx;
                        var z = cell.y + dz;
                        if (x >= 0 && z >= 0 && x < _width && z < _depth) target[z * _width + x] = 1f;
                    }
                }
            }

            Soften(water, 2);
            Soften(height, 1);
            Soften(forest, 2);
            Soften(stone, 2);

            var pixels = new Color32[cells];
            for (var i = 0; i < cells; i++)
            {
                pixels[i] = new Color32(ToByte(water[i]), ToByte(height[i] / ChartHeightScale), ToByte(forest[i]), ToByte(stone[i]));
            }

            _chart = new Texture2D(_width, _depth, TextureFormat.RGBA32, false, true)
            {
                name = "FogChart",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            _chart.SetPixels32(pixels);
            _chart.Apply(false, false);
            if (_parchmentMaterialInstance != null) _parchmentMaterialInstance.SetTexture("_ChartTex", _chart);
        }

        private static byte ToByte(float value) => (byte)Mathf.RoundToInt(Mathf.Clamp01(value) * 255f);

        /// <summary>Writes into `heights` (metres above the plain, per cell) the land's height at every cell centre one mesh's triangles cover, keeping the highest.</summary>
        private void RasteriseHeights(MeshFilter filter, GridManager grid, float[] heights)
        {
            var mesh = filter.sharedMesh;
            if (mesh == null || !mesh.isReadable) return;

            var toWorld = filter.transform.localToWorldMatrix;
            var local = mesh.vertices;
            var world = new Vector3[local.Length];
            for (var i = 0; i < local.Length; i++) world[i] = toWorld.MultiplyPoint3x4(local[i]);

            var origin = grid.CellToWorld(Vector2Int.zero);
            var cellSize = grid.CellSize;
            var plain = grid.GroundHeight;
            var triangles = mesh.triangles;
            for (var t = 0; t < triangles.Length; t += 3)
            {
                var a = world[triangles[t]];
                var b = world[triangles[t + 1]];
                var c = world[triangles[t + 2]];

                // The cells whose centres can lie inside this triangle.
                var x0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.x, Mathf.Min(b.x, c.x)) - origin.x) / cellSize - 0.5f));
                var x1 = Mathf.Min(_width - 1, Mathf.CeilToInt((Mathf.Max(a.x, Mathf.Max(b.x, c.x)) - origin.x) / cellSize - 0.5f));
                var z0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.z, Mathf.Min(b.z, c.z)) - origin.z) / cellSize - 0.5f));
                var z1 = Mathf.Min(_depth - 1, Mathf.CeilToInt((Mathf.Max(a.z, Mathf.Max(b.z, c.z)) - origin.z) / cellSize - 0.5f));
                if (x0 > x1 || z0 > z1) continue;

                var area = (b.x - a.x) * (c.z - a.z) - (c.x - a.x) * (b.z - a.z);
                if (Mathf.Abs(area) < 1e-6f) continue;

                for (var z = z0; z <= z1; z++)
                {
                    var pz = origin.z + (z + 0.5f) * cellSize;
                    for (var x = x0; x <= x1; x++)
                    {
                        var px = origin.x + (x + 0.5f) * cellSize;
                        var wa = ((b.x - px) * (c.z - pz) - (c.x - px) * (b.z - pz)) / area;
                        var wb = ((c.x - px) * (a.z - pz) - (a.x - px) * (c.z - pz)) / area;
                        var wc = 1f - wa - wb;
                        if (wa < -1e-4f || wb < -1e-4f || wc < -1e-4f) continue;

                        var h = wa * a.y + wb * b.y + wc * c.y - plain;
                        var index = z * _width + x;
                        if (h > heights[index]) heights[index] = h;
                    }
                }
            }
        }

        /// <summary>A 3x3 box blur, `passes` times, in place.</summary>
        private void Soften(float[] field, int passes)
        {
            var scratch = new float[field.Length];
            for (var pass = 0; pass < passes; pass++)
            {
                for (var z = 0; z < _depth; z++)
                {
                    for (var x = 0; x < _width; x++)
                    {
                        var sum = 0f;
                        var count = 0;
                        for (var dz = -1; dz <= 1; dz++)
                        {
                            var sz = z + dz;
                            if (sz < 0 || sz >= _depth) continue;
                            for (var dx = -1; dx <= 1; dx++)
                            {
                                var sx = x + dx;
                                if (sx < 0 || sx >= _width) continue;
                                sum += field[sz * _width + sx];
                                count++;
                            }
                        }
                        scratch[z * _width + x] = sum / count;
                    }
                }
                Array.Copy(scratch, field, field.Length);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_mask != null) Destroy(_mask);
            if (_chart != null) Destroy(_chart);
            if (_cloudMaterialInstance != null) Destroy(_cloudMaterialInstance);
            if (_parchmentMaterialInstance != null) Destroy(_parchmentMaterialInstance);
        }
    }
}
