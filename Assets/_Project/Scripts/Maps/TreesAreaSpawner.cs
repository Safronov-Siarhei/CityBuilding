using System;
using System.Collections;
using System.Collections.Generic;
using CityBuilder.Grid;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Maps
{
    /// <summary>
    /// Spawns and maintains the tree population within a map's TreesArea zone: an initial batch
    /// on map load, and one replacement (elsewhere in the same zone, after a delay) each time a
    /// tree is felled -- see NotifyTreeHarvested, called by CitizenVisualsManager when a citizen
    /// finishes one work visit at a tree's ResourceNode.
    /// </summary>
    public class TreesAreaSpawner : MonoBehaviour
    {
        public static TreesAreaSpawner Instance { get; private set; }

        private const int InitialTreeCount = 600;
        private const float RespawnDelaySeconds = 60f;
        private const int MaxPlacementAttempts = 40;
        // Cell size is 1m on this map, so a 1-cell exclusion radius keeps trees at least ~2 cells
        // (~2m center-to-center, ~1m canopy-to-canopy) apart instead of allowed to touch directly.
        private const int MinSpacingCells = 1;
        // Trunk-sized, not canopy-sized -- comfortably inside the MinSpacingCells gap between
        // neighboring trees so two adjacent trees' carved regions never overlap/merge.
        private const float TreeObstacleRadius = 0.3f;
        private const float TreeObstacleHeight = 2f;

        private Collider _zoneCollider;
        private Bounds _zoneBounds;
        private GameObject[] _treePrefabs = new GameObject[0];
        private readonly HashSet<Vector2Int> _treeCells = new HashSet<Vector2Int>();
        private readonly Dictionary<GameObject, Vector2Int> _treeCellByInstance = new Dictionary<GameObject, Vector2Int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Initialize(GameObject treesAreaInstance, GameObject[] treePrefabs)
        {
            _zoneCollider = treesAreaInstance != null ? treesAreaInstance.GetComponentInChildren<Collider>() : null;
            _treePrefabs = treePrefabs ?? new GameObject[0];
            if (_zoneCollider == null || _treePrefabs.Length == 0) return;

            _zoneBounds = _zoneCollider.bounds;
            for (var i = 0; i < InitialTreeCount; i++)
            {
                // The starting forest is already mature -- only trees planted later (after a
                // harvest) grow up from a sapling. See SpawnOneTree's startGrown parameter.
                SpawnOneTree(startGrown: true);
            }

            if (_treeCells.Count == 0)
            {
                Debug.LogWarning($"TreesAreaSpawner: 0 of {InitialTreeCount} initial trees placed -- zone bounds: {_zoneBounds}. Every placement attempt failed either the zone-membership or the water-cell check.");
            }
        }

        /// <summary>Called once a citizen finishes a single work visit at this tree's ResourceNode -- one visit fells it.</summary>
        public void NotifyTreeHarvested(GameObject treeInstance)
        {
            if (treeInstance == null) return;

            if (_treeCellByInstance.TryGetValue(treeInstance, out var cell))
            {
                _treeCells.Remove(cell);
                _treeCellByInstance.Remove(treeInstance);
                GridManager.Instance?.SetAreaOccupied(cell, Vector2Int.one, false);
            }

            Destroy(treeInstance);
            StartCoroutine(RespawnAfterDelay());
        }

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(RespawnDelaySeconds);
            SpawnOneTree(startGrown: false);
        }

        private void SpawnOneTree(bool startGrown)
        {
            var grid = GridManager.Instance;
            if (grid == null || _zoneCollider == null || _treePrefabs.Length == 0) return;

            var mapApplier = MeshMapApplier.Instance;

            for (var attempt = 0; attempt < MaxPlacementAttempts; attempt++)
            {
                var x = UnityEngine.Random.Range(_zoneBounds.min.x, _zoneBounds.max.x);
                var z = UnityEngine.Random.Range(_zoneBounds.min.z, _zoneBounds.max.z);
                var origin = new Vector3(x, _zoneBounds.max.y + 50f, z);

                // Raycast confirms true membership in the (possibly irregular) zone shape, not
                // just the bounding box.
                if (!_zoneCollider.Raycast(new Ray(origin, Vector3.down), out var hit, 1000f)) continue;

                var cell = grid.WorldToCell(hit.point);
                if (!grid.IsWithinBounds(cell, Vector2Int.one)) continue;
                // The TreesArea zone can overlap the shoreline right at its edge -- exclude water
                // cells explicitly rather than trusting the zone mesh alone. This now benefits
                // from MeshMapApplier's Ground collider covering every mesh piece (not just the
                // first), so the water/land classification itself is accurate.
                if (mapApplier != null && mapApplier.IsWaterCell(cell)) continue;
                // Explicit "don't spawn where a building/other object already stands" check.
                if (_treeCells.Contains(cell) || !grid.IsAreaFree(cell, Vector2Int.one)) continue;
                if (HasNearbyTree(cell)) continue;

                var prefab = _treePrefabs[UnityEngine.Random.Range(0, _treePrefabs.Length)];
                if (prefab == null) continue;

                var position = grid.GetFootprintCenterWorld(cell, Vector2Int.one);
                // Preserve the prefab's own corrective root rotation (see MeshMapApplier) rather
                // than forcing identity, which would render the tree tipped over.
                var instance = Instantiate(prefab, position, prefab.transform.rotation, transform);
                if (!startGrown)
                {
                    instance.AddComponent<TreeGrowth>();
                }
                instance.AddComponent<ResourceNode>().Initialize(ResourceType.Wood);

                // Carves this tree out of the baked NavMesh (see MeshMapApplier.BuildNavMesh) so
                // CitizenAgent's pathfinding routes around it -- destroyed automatically along
                // with the tree GameObject on harvest (NotifyTreeHarvested), no cleanup needed.
                // Wrapped: an exception here must not abort this whole method -- it's called in a
                // loop of up to InitialTreeCount trees, and letting one bad obstacle add take the
                // rest of the forest (and grid/cell bookkeeping below) down with it would be far
                // worse than just that one tree not carving itself out.
                try
                {
                    var obstacle = instance.AddComponent<NavMeshObstacle>();
                    obstacle.shape = NavMeshObstacleShape.Capsule;
                    obstacle.radius = TreeObstacleRadius;
                    obstacle.height = TreeObstacleHeight;
                    obstacle.carving = true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"TreesAreaSpawner: NavMeshObstacle setup failed for a tree -- it may not block citizen pathing. {e}");
                }

                grid.SetAreaOccupied(cell, Vector2Int.one, true);
                _treeCells.Add(cell);
                _treeCellByInstance[instance] = cell;
                return;
            }
        }

        /// <summary>True if any already-placed tree occupies a cell within MinSpacingCells of the candidate (a square neighborhood check, cheap and sufficient at this grid resolution).</summary>
        private bool HasNearbyTree(Vector2Int cell)
        {
            for (var dx = -MinSpacingCells; dx <= MinSpacingCells; dx++)
            {
                for (var dz = -MinSpacingCells; dz <= MinSpacingCells; dz++)
                {
                    if (_treeCells.Contains(cell + new Vector2Int(dx, dz))) return true;
                }
            }
            return false;
        }
    }
}
