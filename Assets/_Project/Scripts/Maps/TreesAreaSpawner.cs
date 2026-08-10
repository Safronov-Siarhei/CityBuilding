using System.Collections;
using System.Collections.Generic;
using CityBuilder.Grid;
using CityBuilder.Resources;
using UnityEngine;

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

        private Collider[] _zoneColliders = new Collider[0];
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
            // Plural: MeshMapApplier now adds a collider to every mesh piece under the zone
            // instance, not just the first -- a hand-authored TreesArea can be several
            // disconnected forest patches, and a single collider derived from only the first
            // piece would silently leave the rest of the zone with nothing to sample against.
            _zoneColliders = treesAreaInstance != null ? treesAreaInstance.GetComponentsInChildren<Collider>() : new Collider[0];
            _treePrefabs = treePrefabs ?? new GameObject[0];
            if (_zoneColliders.Length == 0 || _treePrefabs.Length == 0) return;
            if (MeshMapApplier.Instance == null || MeshMapApplier.Instance.GroundTransform == null) return;

            for (var i = 0; i < InitialTreeCount; i++)
            {
                // The starting forest is already mature -- only trees planted later (after a
                // harvest) grow up from a sapling. See SpawnOneTree's startGrown parameter.
                SpawnOneTree(startGrown: true);
            }

            if (_treeCells.Count == 0)
            {
                Debug.LogWarning($"TreesAreaSpawner: 0 of {InitialTreeCount} initial trees placed -- zone colliders: {_zoneColliders.Length}, tree prefabs: {_treePrefabs.Length}. Every placement attempt failed either the zone-membership or the Ground raycast check.");
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

        /// <summary>
        /// Samples a random point within the (small, tight) TreesArea zone bounds -- sampling
        /// from the whole Ground extent instead (an earlier version of this method) made a valid
        /// hit rare-to-nonexistent whenever the zone was a small fraction of the map, since most
        /// random Ground points would land outside it and get rejected within MaxPlacementAttempts
        /// tries. A raycast against the real Ground collider at that same point still gates the
        /// final placement, so a tree can only ever land somewhere solid ground truly is -- that
        /// was the actual bug this method fixes (the zone mesh can overlap the shoreline). The
        /// tree is spawned as a child of the Ground instance itself (mapApplier.GroundTransform),
        /// not of this spawner.
        /// </summary>
        private void SpawnOneTree(bool startGrown)
        {
            var grid = GridManager.Instance;
            var mapApplier = MeshMapApplier.Instance;
            if (grid == null || mapApplier == null || _zoneColliders.Length == 0 || _treePrefabs.Length == 0) return;

            var groundTransform = mapApplier.GroundTransform;
            if (groundTransform == null) return;

            for (var attempt = 0; attempt < MaxPlacementAttempts; attempt++)
            {
                // A different zone piece each attempt so multi-patch zones get sampled fairly
                // across all of them, not just whichever one happened to be picked first.
                var zoneCollider = _zoneColliders[Random.Range(0, _zoneColliders.Length)];
                var zoneBounds = zoneCollider.bounds;
                var x = Random.Range(zoneBounds.min.x, zoneBounds.max.x);
                var z = Random.Range(zoneBounds.min.z, zoneBounds.max.z);
                var origin = new Vector3(x, zoneBounds.max.y + 50f, z);

                // Raycast confirms true membership in the (possibly irregular) zone shape, not
                // just the bounding box.
                if (!zoneCollider.Raycast(new Ray(origin, Vector3.down), out _, 1000f)) continue;
                // ...and that solid ground truly exists at this exact point, rather than trusting
                // the zone mesh alone right at a shoreline it might overlap.
                if (!mapApplier.TryRaycastGround(origin, out var groundHit)) continue;

                var cell = grid.WorldToCell(groundHit.point);
                if (!grid.IsWithinBounds(cell, Vector2Int.one)) continue;
                // Explicit "don't spawn where a building/other object already stands" check.
                if (_treeCells.Contains(cell) || !grid.IsAreaFree(cell, Vector2Int.one)) continue;
                if (HasNearbyTree(cell)) continue;

                var prefab = _treePrefabs[Random.Range(0, _treePrefabs.Length)];
                if (prefab == null) continue;
                // groundHit.point (not a grid-cell-center approximation) so the tree sits exactly
                // on the real mesh surface. Preserve the prefab's own corrective root rotation
                // (see MeshMapApplier) rather than forcing identity, which would tip it over.
                var instance = Instantiate(prefab, groundHit.point, prefab.transform.rotation, groundTransform);
                // Instantiate(pos, rot, parent) only sets world position/rotation -- localScale is
                // copied verbatim from the prefab, so it still combines with the new parent's own
                // scale. Map-1-Ground.fbx's imported root carries a 100x transform scale (its mesh
                // data is baked down to compensate, which is why Ground itself still looks correct
                // size) -- a tree parented under it without this correction would render 100x too
                // big. Counteract it explicitly so the tree's final world scale is unchanged.
                var parentScale = groundTransform.lossyScale;
                instance.transform.localScale = new Vector3(
                    parentScale.x != 0f ? 1f / parentScale.x : 1f,
                    parentScale.y != 0f ? 1f / parentScale.y : 1f,
                    parentScale.z != 0f ? 1f / parentScale.z : 1f);
                if (!startGrown)
                {
                    instance.AddComponent<TreeGrowth>();
                }
                instance.AddComponent<ResourceNode>().Initialize(ResourceType.Wood);

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
