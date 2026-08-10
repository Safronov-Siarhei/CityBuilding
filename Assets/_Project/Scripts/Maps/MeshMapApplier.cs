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
        private Collider[] _groundColliders = new Collider[0];

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

        /// <summary>
        /// Live downward raycast against the real Ground mesh collider(s) directly beneath
        /// worldPos, used internally to classify each grid cell as land or water (see
        /// ComputeWaterAndZoneCells / IsWaterCell). Ground can be several disconnected mesh
        /// pieces (see AddMeshCollidersToAll), so every one of them is checked, not just the first.
        /// </summary>
        private bool TryRaycastGround(Vector3 worldPos, out RaycastHit hit)
        {
            const float rayStartHeight = 500f;
            const float rayLength = 1000f;
            var ray = new Ray(new Vector3(worldPos.x, rayStartHeight, worldPos.z), Vector3.down);

            foreach (var collider in _groundColliders)
            {
                if (collider != null && collider.Raycast(ray, out hit, rayLength)) return true;
            }
            hit = default;
            return false;
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
            if (map.GroundPrefab != null)
            {
                var groundInstance = Instantiate(map.GroundPrefab, Vector3.zero, map.GroundPrefab.transform.rotation, transform);
                // Every sub-mesh gets its own collider, not just the first -- Map-1-Ground.fbx is
                // authored as several separate mesh pieces, and a single collider derived from
                // only the first one found left the rest of the terrain with nothing to raycast
                // against (silently misclassified as water / unreachable ground).
                AddMeshCollidersToAll(groundInstance);
                _groundColliders = groundInstance.GetComponentsInChildren<Collider>();
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

            Collider[] waterZoneColliders = new Collider[0];
            if (map.WaterPlacementZonePrefab != null)
            {
                var waterZoneInstance = Instantiate(map.WaterPlacementZonePrefab, Vector3.zero, map.WaterPlacementZonePrefab.transform.rotation, transform);
                SetRenderersEnabled(waterZoneInstance, false);
                AddMeshCollidersToAll(waterZoneInstance);
                waterZoneColliders = waterZoneInstance.GetComponentsInChildren<Collider>();
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

            ComputeWaterAndZoneCells(grid, waterZoneColliders);

            if (treesAreaInstance != null && TreesAreaSpawner.Instance != null)
            {
                TreesAreaSpawner.Instance.Initialize(treesAreaInstance, map.TreePrefabs);
            }
        }

        private void ComputeWaterAndZoneCells(GridManager grid, Collider[] waterZoneColliders)
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

                    var isLand = TryRaycastGround(new Vector3(center.x, 0f, center.z), out _);
                    if (!isLand)
                    {
                        _waterCells.Add(cell);
                    }

                    var isWaterZone = false;
                    foreach (var waterZoneCollider in waterZoneColliders)
                    {
                        if (waterZoneCollider != null && waterZoneCollider.Raycast(ray, out _, rayLength))
                        {
                            isWaterZone = true;
                            break;
                        }
                    }
                    if (isWaterZone)
                    {
                        _waterPlacementZoneCells.Add(cell);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a MeshCollider to every MeshFilter found under root (root itself or any nested
        /// child -- FBX hierarchy isn't guaranteed), each wired to its own mesh. Imported FBX
        /// assets have no collider by default (addColliders: 0), and a MeshCollider only reads
        /// from a MeshFilter on its own GameObject, not a parent/child's -- a hand-authored map
        /// mesh (Ground, the water-placement zone, TreesArea) can be several disconnected pieces,
        /// so every one of them needs its own collider rather than just the first found.
        /// </summary>
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
