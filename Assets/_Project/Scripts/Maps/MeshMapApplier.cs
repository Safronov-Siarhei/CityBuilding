using System;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Grid;
using CityBuilder.Saving;
using UnityEngine;
using UnityEngine.AI;

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

        // Sized for a citizen, not Unity's default humanoid navmesh agent -- CitizenAgent's
        // CharacterController is radius 0.15/height 0.72 (see CitizenVisualsManager.SpawnAgent).
        // agentSlope deliberately excludes Map-1-Ground.fbx's decorative relief (hills/cliffs
        // outside the flat playable field, see GroundHeightTolerance below) from the walkable
        // surface, so a path never routes across it even if IsGroundAt somehow let a point there
        // through.
        private static readonly NavMeshBuildSettings CitizenNavMeshSettings = new NavMeshBuildSettings
        {
            agentTypeID = 0,
            agentRadius = 0.22f,
            agentHeight = 0.8f,
            agentSlope = 40f,
            agentClimb = 0.3f,
            minRegionArea = 0.5f,
        };

        // Vertical size of the box a walkable-surface building (a Bridge) contributes to the
        // NavMesh. Thin, and centred on the building's own origin, so its walkable top face lands
        // within agentClimb of the surrounding ground and the two connect into one region.
        private const float WalkableSurfaceThickness = 0.2f;
        // Bridges are keepSelectedAfterPlacement -- players lay a run of tiles in quick
        // succession, and rebuilding the whole map's NavMesh per tile would hitch on every click.
        // Coalesces a burst of placements into a single rebuild shortly after the last one.
        private const float NavMeshRebuildDebounceSeconds = 0.4f;

        private readonly HashSet<Vector2Int> _waterCells = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> _waterPlacementZoneCells = new HashSet<Vector2Int>();
        // Every grid cell inside the map's TreesArea zone, resolved once at load so the zone's
        // own geometry can be thrown away immediately afterwards (see Apply) instead of staying
        // in the scene as an invisible obstacle. A list, not a set: TreesAreaSpawner picks random
        // cells from it, which wants indexing.
        private readonly List<Vector2Int> _treesZoneCells = new List<Vector2Int>();
        private Collider[] _groundColliders = new Collider[0];
        private NavMeshDataInstance _navMeshDataInstance;

        // Kept past the initial bake so walkable-surface buildings can be folded in later without
        // re-deriving the (unchanging) ground geometry every time.
        private NavMeshData _navMeshData;
        private Bounds _navMeshBounds;
        private readonly List<NavMeshBuildSource> _groundSources = new List<NavMeshBuildSource>();
        private readonly Dictionary<BuildingInstance, NavMeshBuildSource> _walkableSurfaces = new Dictionary<BuildingInstance, NavMeshBuildSource>();
        private readonly List<NavMeshBuildSource> _rebuildSources = new List<NavMeshBuildSource>();
        private float _rebuildTimer = -1f;

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

        // Map-1-Ground.fbx isn't one flat plane -- it also carries decorative relief (rises/dips
        // outside the flat playable field) as part of the same mesh/collider set. Citizens are
        // pinned to one fixed flat height every frame (see CitizenAgent.PinToGroundHeight), so a
        // destination that's real ground but at the wrong height is still physically unreachable
        // at that pinned height -- the agent walks into the relief's actual geometry and gets
        // stuck forever instead of arriving. Same tolerance CitizenAgent.TryFindWalkablePoint
        // already uses to vet ambient wander targets, applied here too.
        private const float GroundHeightTolerance = 0.5f;

        /// <summary>
        /// Used by CitizenSelector to validate a click-to-move destination. Deliberately ignores
        /// whatever the click's own raycast actually hit first (a building roof, a tree canopy)
        /// -- a click that's visually on top of one of those still reads as a valid destination
        /// on the ground beneath it, exactly like clicking bare ground next to it would. The
        /// citizen still has to physically get there: CitizenAgent has no pathfinding around
        /// obstacles, so if the target is genuinely behind a building's solid collider it'll walk
        /// into it and get stuck (see CitizenAgent's StuckTimeoutSeconds retry) same as any other
        /// unreachable point. Also rejects real ground that's off the flat playable height (see
        /// GroundHeightTolerance above) -- without this a click on a decorative slope/cliff piece
        /// of the same Ground mesh would read OK! but never actually be reachable.
        /// </summary>
        public bool IsGroundAt(Vector3 worldPos)
        {
            if (!TryRaycastGround(worldPos, out var hit)) return false;

            var grid = GridManager.Instance;
            if (grid == null) return true;

            return Mathf.Abs(hit.point.y - grid.GroundHeight) <= GroundHeightTolerance;
        }

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

        /// <summary>
        /// Where an arbitrary ray (in practice: the camera ray under the cursor) meets the map's
        /// real ground surface -- the nearest hit across every ground mesh piece.
        ///
        /// Asked of the ground colliders BY NAME rather than via a Physics.Raycast that takes
        /// whatever it meets first, which is the only way a click can mean "this spot on the
        /// ground" reliably: a scene-wide ray reports the first collider along it, and over a
        /// forest that is a tree's canopy box, a boulder, a building roof -- each of them metres
        /// above and beside the ground the player is pointing at. Callers that want the thing
        /// that was clicked (a citizen, a tree) still scan all hits themselves; this answers the
        /// separate question of where on the map that click landed.
        /// </summary>
        public bool TryRaycastGround(Ray ray, out RaycastHit hit)
        {
            const float rayLength = 1000f;

            hit = default;
            var found = false;
            foreach (var collider in _groundColliders)
            {
                if (collider == null || !collider.Raycast(ray, out var candidate, rayLength)) continue;
                if (found && candidate.distance >= hit.distance) continue;
                hit = candidate;
                found = true;
            }
            return found;
        }

        private void Apply(MeshMapDefinition map)
        {
            var grid = GridManager.Instance;
            if (grid == null) return;

            if (baseGroundToHide != null) baseGroundToHide.SetActive(false);
            if (baseForestBorderToHide != null) baseForestBorderToHide.SetActive(false);

            // Before any of the map's colliders exist, so nothing is ever swept against even once:
            // half the cost of a walking citizen was the capsule hitting this geometry, and none of
            // it was ever holding an agent up. See TerrainPhysicsLayer for the measurement.
            TerrainPhysicsLayer.ExcludeFromCollisions();

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
                TerrainPhysicsLayer.Assign(groundInstance);
                _groundColliders = groundInstance.GetComponentsInChildren<Collider>();
                BuildNavMesh();
            }

            if (map.WaterPrefab != null)
            {
                // Y is pinned to -90 regardless of whatever the FBX bake produces for it --
                // X/Z are kept from the source prefab since those carry the Blender Z-up
                // correction (see the comment above on GroundPrefab).
                var sourceEuler = map.WaterPrefab.transform.rotation.eulerAngles;
                var waterRotation = Quaternion.Euler(sourceEuler.x, -90f, sourceEuler.z);
                var waterInstance = Instantiate(map.WaterPrefab, Vector3.zero, waterRotation, transform);
                TerrainPhysicsLayer.Assign(waterInstance);
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
                TerrainPhysicsLayer.Assign(waterZoneInstance);
                waterZoneColliders = waterZoneInstance.GetComponentsInChildren<Collider>();
            }

            GameObject treesAreaInstance = null;
            Collider[] treesZoneColliders = new Collider[0];
            if (map.TreesAreaPrefab != null)
            {
                treesAreaInstance = Instantiate(map.TreesAreaPrefab, Vector3.zero, map.TreesAreaPrefab.transform.rotation, transform);
                SetRenderersEnabled(treesAreaInstance, false);
                // Every sub-mesh gets its own collider (not just the first, unlike AddMeshCollider
                // above) -- a hand-authored TreesArea zone can be several disconnected forest
                // patches, and deriving a single collider from just the first one found would
                // silently make the rest of the zone read as outside the zone below.
                AddMeshCollidersToAll(treesAreaInstance);
                TerrainPhysicsLayer.Assign(treesAreaInstance);
                treesZoneColliders = treesAreaInstance.GetComponentsInChildren<Collider>();
            }

            ComputeWaterAndZoneCells(grid, waterZoneColliders, treesZoneColliders);

            // The zone is spawn DATA, and now that it's been read into cells it must stop being
            // physics. Map-1-TreesArea.fbx is not a flat marker plane: it's an extruded volume a
            // full metre tall sitting directly ON the walkable ground (y 0..1), invisible,
            // covering the whole forest. Every physics query over the forest hit its top face
            // first instead of what the player was actually pointing at:
            //
            // - Click-to-move took the destination from that face, a metre above the ground and,
            //   at the camera's angle, about a metre off horizontally -- the "clicks land next to
            //   where I clicked" report.
            // - A boulder (0.6m) sits entirely inside the slab, and a tree's lower half does too,
            //   so a click meant to order a chop/quarry never reached the ResourceNode underneath
            //   -- it became a move order, or a flat NO! where the shifted point had no NavMesh.
            // - BuildingPlacer's ghost read the same shifted cell, and CitizenAgent's ground probe
            //   (TryFindWalkablePoint) rejected every forest point as "wrong height".
            // - Its side walls are solid geometry standing in the citizens' walking layer.
            //
            // Deactivated before Destroy: Destroy only takes effect at end of frame, and the
            // colliders must be out of the physics scene for the tree spawning right below.
            if (treesAreaInstance != null)
            {
                treesAreaInstance.SetActive(false);
                Destroy(treesAreaInstance);
            }

            // Called even when the zone resolved to nothing, so TreesAreaSpawner gets to report a
            // forest that silently failed to materialise rather than staying quiet about it.
            if (map.TreesAreaPrefab != null && TreesAreaSpawner.Instance != null)
            {
                TreesAreaSpawner.Instance.Initialize(_treesZoneCells.ToArray(), map.TreePrefabs);
            }

            // After the forest, so boulders can't land on a cell a tree already took (they share
            // GridManager occupancy), and after ComputeWaterAndZoneCells so they stay out of the
            // water. Mesh maps carry no rock prefabs of their own -- see RockSpawner.
            RockSpawner.Instance?.Initialize();
        }

        /// <summary>
        /// Bakes a walkable NavMesh from the Ground mesh's own colliders (water and the
        /// decorative forest border have none, so both are automatically excluded -- no manual
        /// carving needed for those). Buildings and trees aren't part of this bake at all: they
        /// carve themselves out dynamically at runtime via their own NavMeshObstacle component
        /// (see BuildingInstance.Initialize / TreesAreaSpawner.SpawnOneTree), so placing/removing
        /// one never needs a re-bake here. RemoveAllNavMeshData first since this can re-run if a
        /// map is ever reapplied within the same play session (nothing else in this project adds
        /// NavMesh data, so this is safe to own outright).
        /// </summary>
        private void BuildNavMesh()
        {
            // NavMesh is an enhancement (CitizenAgent falls back to a direct line when no path is
            // found -- see BuildRoute), never a requirement -- a failure here (e.g. a mesh with a
            // bad import setting, an unexpected geometry edge case) must never take down the rest
            // of Apply() with it, since everything after this call (water, the water-placement
            // zone, TreesArea/tree spawning) -- and every later gameplay action that depends on
            // this method having run to completion, like population grants firing when the Town
            // Hall is placed -- would otherwise silently stop working too.
            try
            {
                NavMesh.RemoveAllNavMeshData();

                _groundSources.Clear();
                foreach (var collider in _groundColliders)
                {
                    if (collider is MeshCollider meshCollider && meshCollider.sharedMesh != null)
                    {
                        _groundSources.Add(new NavMeshBuildSource
                        {
                            shape = NavMeshBuildSourceShape.Mesh,
                            sourceObject = meshCollider.sharedMesh,
                            transform = meshCollider.transform.localToWorldMatrix,
                            area = 0
                        });
                    }
                }
                var sources = _groundSources;
                if (sources.Count == 0)
                {
                    // Previously a silent no-op -- left citizens permanently on the direct-line
                    // fallback (see CitizenAgent.BuildRoute) with zero trace of why in the
                    // Console, which made "citizens aren't moving right" reports impossible to
                    // diagnose remotely. _groundColliders came from AddMeshCollidersToAll just
                    // above in Apply() -- an empty result here means either the Ground prefab had
                    // no MeshFilters, or none of them got a MeshCollider (e.g. an unreadable mesh).
                    Debug.LogWarning($"MeshMapApplier.BuildNavMesh: 0 usable MeshCollider sources out of {_groundColliders.Length} ground collider(s) -- NavMesh was NOT built. Citizens will fall back to walking in a straight line toward every destination, ignoring obstacles.");
                    return;
                }

                var grid = GridManager.Instance;
                var worldWidth = grid != null ? grid.GridSize.x * grid.CellSize : 200f;
                var worldDepth = grid != null ? grid.GridSize.y * grid.CellSize : 200f;
                _navMeshBounds = new Bounds(Vector3.zero, new Vector3(worldWidth + 20f, 40f, worldDepth + 20f));

                _navMeshData = NavMeshBuilder.BuildNavMeshData(CitizenNavMeshSettings, sources, _navMeshBounds, Vector3.zero, Quaternion.identity);
                _navMeshDataInstance = NavMesh.AddNavMeshData(_navMeshData);
                Debug.Log($"MeshMapApplier.BuildNavMesh: baked NavMesh from {sources.Count} ground source(s).");

                // A loaded save instantiates its buildings from GameSaveController.Start(), which
                // Unity may run before this one -- any bridge that already registered itself would
                // have just been baked away by the ground-only build above, so fold them back in.
                if (_walkableSurfaces.Count > 0) ScheduleNavMeshRebuild();
            }
            catch (Exception e)
            {
                Debug.LogError($"MeshMapApplier.BuildNavMesh failed -- citizens will fall back to direct-line movement instead of pathfinding around obstacles. {e}");
            }
        }

        /// <summary>
        /// Adds this building's footprint to the NavMesh as walkable ground -- see
        /// BuildingData.providesWalkableSurface, currently only the Bridge. Called from
        /// BuildingInstance.Initialize, so it covers both fresh placement and save/load restore
        /// through the same single hook the fog reveal already uses.
        ///
        /// A Box source built from the footprint rather than the prefab's collider: the road/
        /// bridge collider is a trigger sized for clicking, and driving navigation off it would
        /// tie two unrelated things together. The building's own transform carries its placement
        /// rotation, so the unrotated footprint is correct here for the same reason it is in
        /// BuildingInstance.SetupNavMeshObstacle.
        /// </summary>
        public void RegisterWalkableSurface(BuildingInstance instance)
        {
            if (instance == null || instance.Data == null) return;

            var cellSize = GridManager.Instance != null ? GridManager.Instance.CellSize : 1f;
            var footprint = instance.Data.footprintSize;

            _walkableSurfaces[instance] = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                size = new Vector3(footprint.x * cellSize, WalkableSurfaceThickness, footprint.y * cellSize),
                transform = Matrix4x4.TRS(instance.transform.position, instance.transform.rotation, Vector3.one),
                area = 0
            };

            ScheduleNavMeshRebuild();
        }

        /// <summary>Drops a destroyed bridge's contribution (decay, combat damage) so the water underneath goes back to being uncrossable.</summary>
        public void UnregisterWalkableSurface(BuildingInstance instance)
        {
            if (instance == null) return;
            if (_walkableSurfaces.Remove(instance)) ScheduleNavMeshRebuild();
        }

        private void ScheduleNavMeshRebuild()
        {
            _rebuildTimer = NavMeshRebuildDebounceSeconds;
        }

        private void Update()
        {
            if (_rebuildTimer < 0f) return;

            _rebuildTimer -= Time.deltaTime;
            if (_rebuildTimer > 0f) return;

            _rebuildTimer = -1f;
            RebuildNavMeshWithWalkableSurfaces();
        }

        /// <summary>
        /// Re-bakes the map's NavMesh from the (cached) ground geometry plus every registered
        /// walkable surface. Updates the existing NavMeshData in place and asynchronously, so the
        /// current one stays live and usable while it runs -- citizens mid-route keep walking
        /// rather than freezing for the duration of a full-map bake.
        /// </summary>
        private void RebuildNavMeshWithWalkableSurfaces()
        {
            if (_navMeshData == null || _groundSources.Count == 0) return;

            try
            {
                _rebuildSources.Clear();
                _rebuildSources.AddRange(_groundSources);
                foreach (var surface in _walkableSurfaces.Values)
                {
                    _rebuildSources.Add(surface);
                }

                NavMeshBuilder.UpdateNavMeshDataAsync(_navMeshData, CitizenNavMeshSettings, _rebuildSources, _navMeshBounds);
            }
            catch (Exception e)
            {
                Debug.LogError($"MeshMapApplier.RebuildNavMeshWithWalkableSurfaces failed -- bridges placed this session won't be walkable. {e}");
            }
        }

        /// <summary>
        /// One downward probe per grid cell that resolves, in a single pass, which cells are
        /// water (no ground mesh beneath them) and which fall inside each of the map's authored
        /// zones. Runs once at map load; afterwards the zone geometry itself is disposable (see
        /// Apply) and every later query is a plain set/list lookup.
        /// </summary>
        private void ComputeWaterAndZoneCells(GridManager grid, Collider[] waterZoneColliders, Collider[] treesZoneColliders)
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

                    foreach (var treesZoneCollider in treesZoneColliders)
                    {
                        if (treesZoneCollider == null || !treesZoneCollider.Raycast(ray, out _, rayLength)) continue;
                        _treesZoneCells.Add(cell);
                        break;
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
