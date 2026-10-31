using CityBuilder.Core;
using System.Collections.Generic;
using CityBuilder.Grid;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.Rendering;

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
    /// The boulders are the ten Blender models Stone1..Stone10 (Models/Terrain), carried by the
    /// map (MeshMapDefinition.RockPrefabs) and ordered smallest first. Small ones are common and
    /// big ones rare -- the weight falls linearly with size. Which model stands on a cell is a
    /// function of the CELL, not a dice roll, so a reloaded save puts the same boulder back on
    /// the same spot without the save having to say which one it was. Without models the old
    /// cluster of cubes stands in.
    ///
    /// A boulder's stock follows its size (BalanceConfig.StoneForRockSize, stone_rock_size_1..10
    /// in the sheet): a pebble gives a dozen, the biggest rock fifty. A cube cluster, with no model
    /// to measure, holds stone_per_rock.
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
        // Inside a stony clearing boulders lie a cell apart: the clearing is the landmark, and a
        // map's whole stock (InitialRockCount) has to fit into a few of them.
        private const int ZoneSpacingCells = 1;

        private static readonly Color RockColor = new Color(0.55f, 0.53f, 0.48f);
        private static readonly Color RockShadeColor = new Color(0.43f, 0.42f, 0.39f);

        private GameObject[] _rockPrefabs = new GameObject[0];
        private readonly HashSet<Vector2Int> _rockCells = new HashSet<Vector2Int>();
        private readonly Dictionary<GameObject, Vector2Int> _rockCellByInstance = new Dictionary<GameObject, Vector2Int>();

        private int _count = InitialRockCount;
        private float _scattered;
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

        /// <summary>Whether a boulder stands on this cell.</summary>
        public bool Has(Vector2Int cell) => _rockCells.Contains(cell);

        /// <summary>
        /// Takes the boulder off this cell, if there is one, and frees the cell -- an orc camp is
        /// putting up its square there (see OrcCamp.Reserve). Nothing grows back in its place:
        /// the camp holds the cell from then on.
        /// </summary>
        public bool ClearCell(Vector2Int cell)
        {
            if (!_rockCells.Remove(cell)) return false;
            GameObject instance = null;
            foreach (var pair in _rockCellByInstance)
            {
                if (pair.Value == cell) instance = pair.Key;
            }
            if (instance != null)
            {
                _rockCellByInstance.Remove(instance);
                Destroy(instance);
            }
            GridManager.Instance?.SetAreaOccupied(cell, Vector2Int.one, false);
            return true;
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
        /// <summary>`count` 0 = the designed maps' own InitialRockCount; `scattered` the share of them laid over open land rather than in the stony clearings.</summary>
        public void Initialize(GameObject[] rockPrefabs = null, int count = 0, float scattered = 0f)
        {
            if (_initialized) return;
            _initialized = true;
            if (count > 0) _count = count;
            _scattered = Mathf.Clamp01(scattered);
            _rockPrefabs = rockPrefabs ?? new GameObject[0];

            var spread = Mathf.RoundToInt(_count * _scattered);
            for (var i = 0; i < _count; i++)
            {
                SpawnOneRock(i < spread);
            }

            if (_rockCells.Count == 0)
            {
                Debug.LogWarning($"RockSpawner: 0 of {_count} boulders placed -- every attempt failed the water/ground/occupancy checks. Hand-gathering Stone will be impossible on this map.");
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

        private void SpawnOneRock(bool scatter = false)
        {
            var grid = GridManager.Instance;
            if (grid == null) return;

            var mapApplier = MeshMapApplier.Instance;
            EnsureMaterials();

            // A map with stony clearings (Map 3's RocksArea) keeps its stone there, packed close the
            // way an outcrop lies; a map without them scatters it over all dry land, spaced out.
            var zone = mapApplier != null ? mapApplier.RocksZoneCells : null;
            var inZone = !scatter && zone != null && zone.Count > 0;
            var spacing = inZone ? ZoneSpacingCells : MinSpacingCells;

            for (var attempt = 0; attempt < MaxPlacementAttempts; attempt++)
            {
                var cell = inZone
                    ? zone[Random.Range(0, zone.Count)]
                    : new Vector2Int(Random.Range(0, grid.GridSize.x), Random.Range(0, grid.GridSize.y));

                if (!grid.IsWithinBounds(cell, Vector2Int.one) || !grid.IsAreaFree(cell, Vector2Int.one)) continue;
                if (_rockCells.Contains(cell) || HasNearbyRock(cell, spacing)) continue;
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

            var hash = CellHash(cell);
            var sizeIndex = ModelIndexFor(hash);
            var model = sizeIndex >= 0 ? _rockPrefabs[sizeIndex] : null;
            var height = 0.6f;
            if (model != null)
            {
                // Turned by the cell too, so a reload shows the same face to the camera.
                root.transform.rotation = Quaternion.Euler(0f, (hash >> 8) % 360u, 0f);
                var instance = Instantiate(model, root.transform);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = model.transform.localRotation;
                SceneryShadows.DisableCasting(instance);
                height = Mathf.Max(0.3f, MeasuredHeight(instance));
            }
            else
            {
                root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                var boulderCount = Random.Range(2, 5);
                for (var i = 0; i < boulderCount; i++)
                {
                    var offset = new Vector3(Random.Range(-0.28f, 0.28f), 0f, Random.Range(-0.28f, 0.28f));
                    var size = Random.Range(0.3f, 0.55f);
                    var material = i % 2 == 0 ? _rockMaterial : _rockShadeMaterial;
                    AddCubePart(root.transform, $"Boulder{i}", offset + new Vector3(0f, size * 0.5f, 0f), Vector3.one * size, material);
                }
            }

            // One collider on the root (the cube parts' own are stripped) purely so the player can
            // click the boulder to send a citizen to gather it -- see CitizenSelector. Sized to
            // cover the whole cluster rather than any individual boulder. A TRIGGER specifically:
            // Physics.Raycast still hits it (queriesHitTriggers defaults on) but it doesn't
            // physically block CharacterController.Move, so boulders stay decor citizens can walk
            // over rather than becoming hundreds of new things to get wedged against.
            var clickCollider = root.AddComponent<BoxCollider>();
            clickCollider.isTrigger = true;
            // The whole cell wide whatever the boulder's size, so even the pebbles are easy to tap.
            clickCollider.center = new Vector3(0f, height * 0.5f, 0f);
            clickCollider.size = new Vector3(1f, height, 1f);

            var node = root.AddComponent<ResourceNode>();
            if (model != null) node.Initialize(ResourceType.Stone, BalanceConfig.Instance.StoneForRockSize(sizeIndex));
            else node.Initialize(ResourceType.Stone);

            GridManager.Instance?.SetAreaOccupied(cell, Vector2Int.one, true);
            _rockCells.Add(cell);
            _rockCellByInstance[root] = cell;
            SceneryStreamer.Instance?.Register(root);
            return root;
        }

        /// <summary>
        /// Which boulder model stands on a cell: a weighted pick driven by the cell's hash, so it is
        /// the same every time the cell is asked. The models are ordered smallest first and model i
        /// of n weighs n - i, so pebbles are the commonest find and the great outcrop the rarest
        /// (with ten: 18% down to under 2%).
        /// </summary>
        /// <summary>Which of the size-ordered models stands on a cell with this hash (-1 with none): small ones common, the weight falling linearly with size.</summary>
        private int ModelIndexFor(uint hash)
        {
            var n = _rockPrefabs.Length;
            if (n == 0) return -1;

            var total = n * (n + 1) / 2;
            var pick = (int)(hash % (uint)total);
            for (var i = 0; i < n; i++)
            {
                pick -= n - i;
                if (pick < 0) return i;
            }
            return n - 1;
        }

        /// <summary>A well-mixed integer from a cell's coordinates (stable across runs and saves).</summary>
        private static uint CellHash(Vector2Int cell)
        {
            unchecked
            {
                var h = (uint)cell.x * 73856093u ^ (uint)cell.y * 19349663u;
                h ^= h >> 16;
                h *= 0x7feb352du;
                h ^= h >> 15;
                h *= 0x846ca68bu;
                h ^= h >> 16;
                return h;
            }
        }

        private static float MeasuredHeight(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return 0f;
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds.max.y - instance.transform.position.y;
        }

        private bool HasNearbyRock(Vector2Int cell, int spacing)
        {
            for (var dx = -spacing; dx <= spacing; dx++)
            {
                for (var dz = -spacing; dz <= spacing; dz++)
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

            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            // A boulder is 30-55 cm: its own shadow reads as noise at this camera distance, and
            // casting costs a second draw of every one of the ~420 boulder cubes on the map.
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private void EnsureMaterials()
        {
            if (_rockMaterial != null) return;

            _rockMaterial = new Material(RuntimeShaders.Lit) { color = RockColor };
            _rockShadeMaterial = new Material(RuntimeShaders.Lit) { color = RockShadeColor };
        }
    }
}
