using CityBuilder.Core;
using System.Collections.Generic;
using CityBuilder.Grid;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>
    /// Scatters harvestable stone boulders across a mesh map's dry land. The Stone counterpart to
    /// TreesAreaSpawner with one decisive difference: a worked-out boulder is never replaced. The
    /// map is made with a fixed amount of stone in it and that is all it will ever have.
    ///
    /// Which is why the boulders are in the save file. They are scattered by unseeded Random, so
    /// without saving them a reload would deal a fresh map AND refill every one -- depletion would
    /// have been undone by the act of saving, which is no mechanic at all.
    ///
    /// Mesh maps (MeshMapDefinition) only carry tree prefabs, so before this existed the current
    /// maps had zero Stone ResourceNodes anywhere: only the legacy PNG map path
    /// (MapTerrainGenerator.SpawnRock) ever produced any, and new games don't use it. Boulders are
    /// built from cube primitives here rather than an FBX for the same reason every other prop in
    /// the project is (no rock model exists in Models/), and it matches the game's low-poly style.
    ///
    /// Unlike trees these carry no NavMeshObstacle: they're knee-high, 100+ more carving obstacles
    /// on top of the forest's ~600 would cost real frame time, and citizens visually stepping over
    /// a boulder reads fine. They do occupy their grid cell, so buildings still can't be placed
    /// on top of one.
    /// </summary>
    public class RockSpawner : MonoBehaviour
    {
        public static RockSpawner Instance { get; private set; }

        private const int InitialRockCount = 140;
        private const int MaxPlacementAttempts = 40;
        // Wider than the forest's 1-cell spacing -- boulders read as scattered landmarks rather
        // than a field, and keeps them from crowding a build site.
        private const int MinSpacingCells = 3;

        private static readonly Color RockColor = new Color(0.55f, 0.53f, 0.48f);
        private static readonly Color RockShadeColor = new Color(0.43f, 0.42f, 0.39f);

        private readonly HashSet<Vector2Int> _rockCells = new HashSet<Vector2Int>();
        private readonly Dictionary<GameObject, Vector2Int> _rockCellByInstance = new Dictionary<GameObject, Vector2Int>();

        private Material _rockMaterial;
        private Material _rockShadeMaterial;
        private bool _initialized;

        /// <summary>Every boulder standing, with its cell and what is left in it -- what the save writes down.</summary>
        public IEnumerable<(Vector2Int cell, int remaining)> LiveRocks
        {
            get
            {
                foreach (var pair in _rockCellByInstance)
                {
                    if (pair.Key == null) continue;
                    var node = pair.Key.GetComponent<ResourceNode>();
                    yield return (pair.Value, node != null ? node.RemainingYield : 0);
                }
            }
        }

        /// <summary>
        /// Replaces the freshly scattered boulders with the exact ones a save was made with, each
        /// as picked-over as it was. Called after Initialize has already run (MeshMapApplier does
        /// it as the map comes up), so the first job is clearing what it scattered -- otherwise
        /// the loaded map would carry both sets.
        ///
        /// An empty list means a save made before boulders were written down. That is left alone
        /// rather than clearing the map to nothing: an older save should come back playable, not
        /// stripped of every rock on it.
        /// </summary>
        public void RestoreFromSave(IReadOnlyList<(Vector2Int cell, int remaining)> rocks)
        {
            if (rocks == null || rocks.Count == 0) return;

            foreach (var pair in new List<KeyValuePair<GameObject, Vector2Int>>(_rockCellByInstance))
            {
                GridManager.Instance?.SetAreaOccupied(pair.Value, Vector2Int.one, false);
                if (pair.Key != null) Destroy(pair.Key);
            }
            _rockCellByInstance.Clear();
            _rockCells.Clear();

            EnsureMaterials();
            var grid = GridManager.Instance;
            if (grid == null) return;

            foreach (var (cell, remaining) in rocks)
            {
                if (remaining <= 0) continue; // an emptied boulder is gone; it is not coming back

                var rock = CreateRock(grid.GetFootprintCenterWorld(cell, Vector2Int.one), cell);
                var node = rock != null ? rock.GetComponent<ResourceNode>() : null;
                if (node != null) node.SetRemainingYield(remaining);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>Called once from MeshMapApplier.Apply, after the map's water cells are known (boulders must not land in the lake) and its ground colliders exist (IsGroundAt raycasts against them).</summary>
        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            for (var i = 0; i < InitialRockCount; i++)
            {
                SpawnOneRock();
            }

            if (_rockCells.Count == 0)
            {
                Debug.LogWarning($"RockSpawner: 0 of {InitialRockCount} boulders placed -- every attempt failed the water/ground/occupancy checks. Hand-gathering Stone will be impossible on this map.");
            }
        }

        /// <summary>
        /// A boulder has been worked out -- its cell is freed and it is gone. No replacement is
        /// scheduled, deliberately: stone is the map's one finite resource, and the whole point of
        /// the boulder holding a stock is that the map contains a fixed amount of it and never
        /// makes more. A quarry that has cleared its radius has to be replaced, not waited out.
        /// Contrast TreesAreaSpawner, which does grow a new tree.
        /// </summary>
        public void NotifyRockHarvested(GameObject rockInstance)
        {
            if (rockInstance == null) return;

            if (_rockCellByInstance.TryGetValue(rockInstance, out var cell))
            {
                _rockCells.Remove(cell);
                _rockCellByInstance.Remove(rockInstance);
                GridManager.Instance?.SetAreaOccupied(cell, Vector2Int.one, false);
            }

            Destroy(rockInstance);
        }

        private void SpawnOneRock()
        {
            var grid = GridManager.Instance;
            if (grid == null) return;

            var mapApplier = MeshMapApplier.Instance;
            EnsureMaterials();

            for (var attempt = 0; attempt < MaxPlacementAttempts; attempt++)
            {
                var cell = new Vector2Int(Random.Range(0, grid.GridSize.x), Random.Range(0, grid.GridSize.y));

                if (!grid.IsWithinBounds(cell, Vector2Int.one) || !grid.IsAreaFree(cell, Vector2Int.one)) continue;
                if (_rockCells.Contains(cell) || HasNearbyRock(cell)) continue;
                if (mapApplier != null && mapApplier.IsWaterCell(cell)) continue;

                var position = grid.GetFootprintCenterWorld(cell, Vector2Int.one);
                // Rejects the map's decorative relief (hills/cliffs outside the flat playable
                // field) the same way CitizenAgent vets its wander targets -- a boulder up a
                // cliff is one no citizen could ever walk to.
                if (mapApplier != null && !mapApplier.IsGroundAt(position)) continue;

                CreateRock(position, cell);
                return;
            }
        }

        private GameObject CreateRock(Vector3 position, Vector2Int cell)
        {
            var root = new GameObject("Rock");
            root.transform.SetParent(transform, false);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            var boulderCount = Random.Range(2, 5);
            for (var i = 0; i < boulderCount; i++)
            {
                var offset = new Vector3(Random.Range(-0.28f, 0.28f), 0f, Random.Range(-0.28f, 0.28f));
                var size = Random.Range(0.3f, 0.55f);
                var material = i % 2 == 0 ? _rockMaterial : _rockShadeMaterial;
                AddCubePart(root.transform, $"Boulder{i}", offset + new Vector3(0f, size * 0.5f, 0f), Vector3.one * size, material);
            }

            // One collider on the root (the cube parts' own are stripped) purely so the player can
            // click the boulder to send a citizen to gather it -- see CitizenSelector. Sized to
            // cover the whole cluster rather than any individual boulder. A TRIGGER specifically:
            // Physics.Raycast still hits it (queriesHitTriggers defaults on) but it doesn't
            // physically block CharacterController.Move, so boulders stay decor citizens can walk
            // over rather than becoming hundreds of new things to get wedged against.
            var clickCollider = root.AddComponent<BoxCollider>();
            clickCollider.isTrigger = true;
            clickCollider.center = new Vector3(0f, 0.3f, 0f);
            clickCollider.size = new Vector3(1f, 0.6f, 1f);

            root.AddComponent<ResourceNode>().Initialize(ResourceType.Stone);

            GridManager.Instance?.SetAreaOccupied(cell, Vector2Int.one, true);
            _rockCells.Add(cell);
            _rockCellByInstance[root] = cell;
            return root;
        }

        private bool HasNearbyRock(Vector2Int cell)
        {
            for (var dx = -MinSpacingCells; dx <= MinSpacingCells; dx++)
            {
                for (var dz = -MinSpacingCells; dz <= MinSpacingCells; dz++)
                {
                    if (_rockCells.Contains(cell + new Vector2Int(dx, dz))) return true;
                }
            }
            return false;
        }

        private static void AddCubePart(Transform parent, string partName, Vector3 localPosition, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = partName;
            Destroy(go.GetComponent<BoxCollider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
        }

        private void EnsureMaterials()
        {
            if (_rockMaterial != null) return;

            _rockMaterial = new Material(RuntimeShaders.Lit) { color = RockColor };
            _rockShadeMaterial = new Material(RuntimeShaders.Lit) { color = RockShadeColor };
        }
    }
}
