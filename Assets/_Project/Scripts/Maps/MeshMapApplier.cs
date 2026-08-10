using System.Collections.Generic;
using CityBuilder.Grid;
using CityBuilder.Saving;
using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>
    /// Applies a hand-authored MeshMapDefinition (Blender-modeled Ground/Water/water-placement-
    /// zone/trees-area) for this session, resolved the same way MapTerrainGenerator resolves a
    /// PNG map (new game pick via GameSessionIntent, or a loaded save's stored id). Added to
    /// GameManagers BEFORE MapTerrainGenerator so it consumes GameSessionIntent.NewGameMapId
    /// first -- MapTerrainGenerator's existing "not found in catalog" no-op then handles a mesh
    /// map id harmlessly with zero changes there.
    ///
    /// Water is tracked separately from GridManager's building-occupancy set (not via
    /// SetAreaOccupied) specifically so water-category buildings can still be placed inside the
    /// water-placement zone -- see IsWaterCell/IsWaterPlacementZone and BuildingPlacer.
    /// </summary>
    public class MeshMapApplier : MonoBehaviour
    {
        public static MeshMapApplier Instance { get; private set; }

        [SerializeField] private GameSaveController saveController;
        [SerializeField] private GameObject baseGroundToHide;
        [SerializeField] private GameObject baseForestBorderToHide;

        private readonly HashSet<Vector2Int> _waterCells = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> _waterPlacementZoneCells = new HashSet<Vector2Int>();
        private Collider _groundCollider;
        private GameObject _groundInstance;

        public string CurrentMapId { get; private set; } = string.Empty;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            var mapId = saveController != null && !string.IsNullOrEmpty(saveController.LoadedMapId)
                ? saveController.LoadedMapId
                : GameSessionIntent.NewGameMapId;
            GameSessionIntent.NewGameMapId = null;

            if (string.IsNullOrEmpty(mapId)) return;

            var map = MeshMapCatalog.GetById(mapId);
            if (map == null) return; // not a mesh map id -- leave it for MapTerrainGenerator (legacy PNG saves)

            CurrentMapId = mapId;
            Apply(map);
        }

        public bool IsWaterCell(Vector2Int cell) => _waterCells.Contains(cell);
        public bool IsWaterPlacementZone(Vector2Int cell) => _waterPlacementZoneCells.Contains(cell);

        /// <summary>The instantiated Ground mesh's own transform -- e.g. TreesAreaSpawner parents its trees under this instead of under itself.</summary>
        public Transform GroundTransform => _groundInstance != null ? _groundInstance.transform : null;

        /// <summary>World-space bounds of the real Ground mesh collider, for sampling random points that are guaranteed to fall within its extent.</summary>
        public Bounds GroundBounds => _groundCollider != null ? _groundCollider.bounds : default;

        /// <summary>
        /// Live downward raycast against the actual Ground mesh collider (not the cached per-cell
        /// water set) directly beneath worldPos. Used where a placement needs to confirm real
        /// solid ground exists at an exact point rather than "this whole cell counts as land".
        /// </summary>
        public bool HasGroundBeneath(Vector3 worldPos)
        {
            return TryRaycastGround(worldPos, out _);
        }

        /// <summary>
        /// Same raycast as HasGroundBeneath, but also returns the exact hit (surface point/
        /// normal) -- e.g. TreesAreaSpawner samples a random point over Ground first and places
        /// the tree exactly at the hit, rather than guessing a point elsewhere and hoping it lands
        /// on solid ground.
        /// </summary>
        public bool TryRaycastGround(Vector3 worldPos, out RaycastHit hit)
        {
            hit = default;
            if (_groundCollider == null) return false;

            const float rayStartHeight = 500f;
            const float rayLength = 1000f;
            var ray = new Ray(new Vector3(worldPos.x, rayStartHeight, worldPos.z), Vector3.down);
            return _groundCollider.Raycast(ray, out hit, rayLength);
        }

        private void Apply(MeshMapDefinition map)
        {
            var grid = GridManager.Instance;
            if (grid == null) return;

            if (baseGroundToHide != null) baseGroundToHide.SetActive(false);
            if (baseForestBorderToHide != null) baseForestBorderToHide.SetActive(false);

            // These FBX assets carry a corrective root rotation from Blender's Z-up authoring
            // (visible in the Inspector, e.g. ~90 degrees on X) that must NOT be overridden with
            // Quaternion.identity -- doing so would leave the mesh (and its collider, used below
            // for the land/water raycast split) rotated out of alignment with the world. Passing
            // each prefab's own transform.rotation preserves whatever correction it needs.
            Collider groundCollider = null;
            if (map.GroundPrefab != null)
            {
                var groundInstance = Instantiate(map.GroundPrefab, Vector3.zero, map.GroundPrefab.transform.rotation, transform);
                groundCollider = AddMeshCollider(groundInstance);
                _groundCollider = groundCollider;
                _groundInstance = groundInstance;
            }

            if (map.WaterPrefab != null)
            {
                // Y is pinned to -90 regardless of whatever the FBX bake produces for it --
                // X/Z are kept from the source prefab since those carry the Blender Z-up
                // correction (see the comment above on GroundPrefab).
                var sourceEuler = map.WaterPrefab.transform.rotation.eulerAngles;
                var waterRotation = Quaternion.Euler(sourceEuler.x, -90f, sourceEuler.z);
                var waterInstance = Instantiate(map.WaterPrefab, Vector3.zero, waterRotation, transform);
                if (map.WaterMaterial != null)
                {
                    foreach (var renderer in waterInstance.GetComponentsInChildren<Renderer>())
                    {
                        renderer.sharedMaterial = map.WaterMaterial;
                    }
                }
            }

            Collider waterZoneCollider = null;
            if (map.WaterPlacementZonePrefab != null)
            {
                var waterZoneInstance = Instantiate(map.WaterPlacementZonePrefab, Vector3.zero, map.WaterPlacementZonePrefab.transform.rotation, transform);
                SetRenderersEnabled(waterZoneInstance, false);
                waterZoneCollider = AddMeshCollider(waterZoneInstance);
            }

            GameObject treesAreaInstance = null;
            if (map.TreesAreaPrefab != null)
            {
                treesAreaInstance = Instantiate(map.TreesAreaPrefab, Vector3.zero, map.TreesAreaPrefab.transform.rotation, transform);
                SetRenderersEnabled(treesAreaInstance, false);
                // Every sub-mesh gets its own collider (not just the first, unlike AddMeshCollider
                // above) -- a hand-authored TreesArea zone can be several disconnected forest
                // patches, and deriving a single collider from just the first one found would
                // silently make the rest of the zone impossible for TreesAreaSpawner to sample.
                AddMeshCollidersToAll(treesAreaInstance);
            }

            ComputeWaterAndZoneCells(grid, groundCollider, waterZoneCollider);

            if (treesAreaInstance != null && TreesAreaSpawner.Instance != null)
            {
                TreesAreaSpawner.Instance.Initialize(treesAreaInstance, map.TreePrefabs);
            }
        }

        private void ComputeWaterAndZoneCells(GridManager grid, Collider groundCollider, Collider waterZoneCollider)
        {
            const float rayStartHeight = 500f;
            const float rayLength = 1000f;

            for (var x = 0; x < grid.GridSize.x; x++)
            {
                for (var z = 0; z < grid.GridSize.y; z++)
                {
                    var cell = new Vector2Int(x, z);
                    var center = grid.GetFootprintCenterWorld(cell, Vector2Int.one);
                    var ray = new Ray(new Vector3(center.x, rayStartHeight, center.z), Vector3.down);

                    var isLand = groundCollider != null && groundCollider.Raycast(ray, out _, rayLength);
                    if (!isLand)
                    {
                        _waterCells.Add(cell);
                    }

                    if (waterZoneCollider != null && waterZoneCollider.Raycast(ray, out _, rayLength))
                    {
                        _waterPlacementZoneCells.Add(cell);
                    }
                }
            }
        }

        /// <summary>
        /// Finds the mesh (root or nested child -- FBX hierarchy isn't guaranteed) and adds a
        /// MeshCollider on the same GameObject, explicitly wired to that mesh. Imported FBX
        /// assets have no collider by default (addColliders: 0), and a MeshCollider only reads
        /// from a MeshFilter on its own GameObject, not a parent/child's.
        /// </summary>
        private static Collider AddMeshCollider(GameObject root)
        {
            var meshFilter = root.GetComponentInChildren<MeshFilter>();
            if (meshFilter == null) return null;

            var existing = meshFilter.GetComponent<Collider>();
            if (existing != null) return existing;

            var collider = meshFilter.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = meshFilter.sharedMesh;
            return collider;
        }

        /// <summary>Like AddMeshCollider, but adds a collider to every MeshFilter found under root instead of just the first.</summary>
        private static void AddMeshCollidersToAll(GameObject root)
        {
            foreach (var meshFilter in root.GetComponentsInChildren<MeshFilter>())
            {
                if (meshFilter.GetComponent<Collider>() != null) continue;
                var collider = meshFilter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = meshFilter.sharedMesh;
            }
        }

        private static void SetRenderersEnabled(GameObject root, bool enabled)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = enabled;
            }
        }
    }
}
