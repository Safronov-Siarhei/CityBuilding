using CityBuilder.Grid;
using CityBuilder.Resources;
using CityBuilder.Saving;
using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>
    /// Applies the map picked for this session — a new game's random pick (GameSessionIntent) or
    /// a loaded save's stored map (GameSaveController) — by painting the buildable area with a
    /// terrain texture, blocking placement on water cells, and scattering harvestable
    /// tree/rock props (ResourceNode) on Forest/Stone tiles. With no resolved map (e.g. the
    /// scene opened directly in the Editor, or no maps imported yet) it does nothing and the
    /// scene's plain ground plane shows as before.
    /// </summary>
    public class MapTerrainGenerator : MonoBehaviour
    {
        private const float TreeChancePerForestCell = 0.25f;
        private const float RockChancePerStoneCell = 0.15f;
        private const int MaxTreeProps = 500;
        private const int MaxRockProps = 200;
        private const int PropRandomSeed = 20260807;

        [SerializeField] private GameSaveController saveController;
        [SerializeField] private Color waterColor = new Color(0.25f, 0.5f, 0.75f);
        [SerializeField] private Color grassColor = new Color(0.42f, 0.62f, 0.32f);
        [SerializeField] private Color forestColor = new Color(0.16f, 0.38f, 0.18f);
        [SerializeField] private Color stoneColor = new Color(0.58f, 0.56f, 0.52f);
        [SerializeField] private Material treeTrunkMaterial;
        [SerializeField] private Material treeCanopyMaterial;
        [SerializeField] private Material rockMaterial;

        public string CurrentMapId { get; private set; } = string.Empty;

        private void Start()
        {
            var mapId = saveController != null && !string.IsNullOrEmpty(saveController.LoadedMapId)
                ? saveController.LoadedMapId
                : GameSessionIntent.NewGameMapId;
            GameSessionIntent.NewGameMapId = null;

            if (string.IsNullOrEmpty(mapId)) return;

            var map = MapCatalog.GetById(mapId);
            if (map == null)
            {
                Debug.LogWarning($"[MapTerrainGenerator] Map '{mapId}' not found in the catalog, keeping the plain ground.");
                return;
            }

            CurrentMapId = mapId;
            Apply(map);
        }

        private void Apply(MapDefinition map)
        {
            var grid = GridManager.Instance;
            if (grid == null) return;

            var width = map.Width;
            var height = map.Height;

            var texture = new Texture2D(width, height, TextureFormat.RGB24, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var cellSize = grid.CellSize;
            var cellCenterOffset = new Vector3(cellSize * 0.5f, 0f, cellSize * 0.5f);

            // Fixed seed so the same map always scatters props the same way (same spirit as
            // SetupProject.cs's decorative forest border), independent of whatever else in this
            // play session has already drawn from UnityEngine.Random.
            Random.InitState(PropRandomSeed);
            var treeCount = 0;
            var rockCount = 0;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var terrain = map.GetCell(x, y);
                    texture.SetPixel(x, y, ColorFor(terrain));

                    if (terrain == TerrainType.Water)
                    {
                        grid.SetAreaOccupied(new Vector2Int(x, y), Vector2Int.one, true);
                        continue;
                    }

                    var cellCenter = grid.CellToWorld(new Vector2Int(x, y)) + cellCenterOffset;

                    if (terrain == TerrainType.Forest && treeCount < MaxTreeProps && Random.value < TreeChancePerForestCell)
                    {
                        SpawnTree(cellCenter);
                        treeCount++;
                    }
                    else if (terrain == TerrainType.Stone && rockCount < MaxRockProps && Random.value < RockChancePerStoneCell)
                    {
                        SpawnRock(cellCenter);
                        rockCount++;
                    }
                }
            }
            texture.Apply();

            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "MapTerrain";
            Destroy(plane.GetComponent<Collider>());
            plane.transform.SetParent(transform, false);

            var worldWidth = width * cellSize;
            var worldDepth = height * cellSize;
            var origin = grid.CellToWorld(Vector2Int.zero);
            // Nudged just above the base ground plane to avoid z-fighting where it covers it.
            plane.transform.position = new Vector3(origin.x + worldWidth * 0.5f, 0.02f, origin.z + worldDepth * 0.5f);
            plane.transform.localScale = new Vector3(worldWidth / 10f, 1f, worldDepth / 10f);

            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { mainTexture = texture };
            plane.GetComponent<Renderer>().sharedMaterial = material;
        }

        private Color ColorFor(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Water: return waterColor;
                case TerrainType.Forest: return forestColor;
                case TerrainType.Stone: return stoneColor;
                default: return grassColor;
            }
        }

        private void SpawnTree(Vector3 position)
        {
            var root = new GameObject("Tree");
            root.transform.SetParent(transform, false);
            root.transform.position = position;

            var scale = Random.Range(0.8f, 1.2f);
            var trunkHeight = 1f * scale;
            var canopySize = 1.6f * scale;
            var canopyHeight = 1.4f * scale;

            AddCubePart(root.transform, "Trunk", new Vector3(0f, trunkHeight * 0.5f, 0f), new Vector3(0.35f * scale, trunkHeight, 0.35f * scale), treeTrunkMaterial);
            AddCubePart(root.transform, "Canopy", new Vector3(0f, trunkHeight + canopyHeight * 0.5f, 0f), new Vector3(canopySize, canopyHeight, canopySize), treeCanopyMaterial);

            root.AddComponent<ResourceNode>().Initialize(ResourceType.Wood);
        }

        private void SpawnRock(Vector3 position)
        {
            var root = new GameObject("Rock");
            root.transform.SetParent(transform, false);
            root.transform.position = position;

            var clusterSize = Random.Range(2, 4);
            for (var i = 0; i < clusterSize; i++)
            {
                var offset = new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
                var size = Random.Range(0.3f, 0.55f);
                AddCubePart(root.transform, $"Boulder{i}", offset + new Vector3(0f, size * 0.5f, 0f), Vector3.one * size, rockMaterial);
            }

            root.AddComponent<ResourceNode>().Initialize(ResourceType.Stone);
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
    }
}
