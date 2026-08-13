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
                // Before TreeGrowth, so the collider is measured at the tree's full size and then
                // scales down/up along with the transform as the sapling grows, instead of being
                // permanently sized to a 10% sapling.
                AddClickCollider(instance);
                if (!startGrown)
                {
                    instance.AddComponent<TreeGrowth>();
                }
                instance.AddComponent<ResourceNode>().Initialize(ResourceType.Wood);

                // Deliberately NO NavMeshObstacle. Trees used to carve themselves out of the
                // NavMesh so pathing routed around them, and that turned out to be both the single
                // most expensive thing in the scene and an outright bug:
                //
                // - Cost: 600 carving obstacles re-voxelize the NavMesh when they appear (a stall
                //   on entering a map), are tracked for movement every frame forever despite never
                //   moving, re-carve locally on every harvest/respawn, and inflate the NavMesh with
                //   600 holes that every single pathfinding query then has to navigate around.
                // - Bug: enough holes fragment the forest floor into disconnected islands. An agent
                //   standing on one gets a "successful" but degenerate path (see
                //   CitizenAgent.BuildRoute) and stops dead -- the "Town Hall built among trees
                //   freezes the citizens" report.
                //
                // Buildings still carve and stay solid, so those are still routed around properly.
                // Nothing changes visually here: a tree's collider is a trigger (AddClickCollider),
                // so citizens always walked THROUGH trees physically -- they just take a straight
                // line through the trunk now instead of an elaborate detour around it.
                grid.SetAreaOccupied(cell, Vector2Int.one, true);
                _treeCells.Add(cell);
                _treeCellByInstance[instance] = cell;
                return;
            }
        }

        /// <summary>
        /// Gives the tree a click target so the player can send a citizen to chop it by hand (see
        /// CitizenSelector/ManualGathering) -- the tree FBXs import with addColliders off, so
        /// until this existed a click went straight through a tree into the ground behind it.
        ///
        /// A TRIGGER, not a solid collider: Physics.Raycast still hits it (queriesHitTriggers is
        /// on by default) but it doesn't block CharacterController.Move, so this stays a pure
        /// input affordance. Making ~600 trees physically solid instead would hand every citizen
        /// hundreds of new obstacles to get wedged against -- and trees are deliberately walk-
        /// through now, in pathing as well (see SpawnOneTree on why the NavMeshObstacle went away).
        ///
        /// Sized from the combined renderer bounds (converted back into local space) rather than a
        /// hardcoded box, since Tree1/Tree2 aren't the same size -- approximate under any prefab
        /// rotation, which is fine for something only ever used as a click target.
        /// </summary>
        private static void AddClickCollider(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            var lossyScale = instance.transform.lossyScale;
            if (Mathf.Approximately(lossyScale.x, 0f) || Mathf.Approximately(lossyScale.y, 0f) || Mathf.Approximately(lossyScale.z, 0f)) return;

            var box = instance.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = instance.transform.InverseTransformPoint(bounds.center);
            box.size = new Vector3(
                bounds.size.x / lossyScale.x,
                bounds.size.y / lossyScale.y,
                bounds.size.z / lossyScale.z);
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
