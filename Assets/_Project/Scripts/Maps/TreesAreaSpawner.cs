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
                var x = Random.Range(_zoneBounds.min.x, _zoneBounds.max.x);
                var z = Random.Range(_zoneBounds.min.z, _zoneBounds.max.z);
                var origin = new Vector3(x, _zoneBounds.max.y + 50f, z);

                // Raycast confirms true membership in the (possibly irregular) zone shape, not
                // just the bounding box.
                if (!_zoneCollider.Raycast(new Ray(origin, Vector3.down), out var hit, 1000f)) continue;

                var cell = grid.WorldToCell(hit.point);
                if (!grid.IsWithinBounds(cell, Vector2Int.one)) continue;
                // The TreesArea zone can overlap the shoreline right at its edge -- exclude water
                // cells explicitly rather than trusting the zone mesh alone.
                if (mapApplier != null && mapApplier.IsWaterCell(cell)) continue;
                // Explicit "don't spawn where a building/other object already stands" check.
                if (_treeCells.Contains(cell) || !grid.IsAreaFree(cell, Vector2Int.one)) continue;

                var prefab = _treePrefabs[Random.Range(0, _treePrefabs.Length)];
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

                grid.SetAreaOccupied(cell, Vector2Int.one, true);
                _treeCells.Add(cell);
                _treeCellByInstance[instance] = cell;
                return;
            }
        }
    }
}
