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

            var worldWidth = width * cellSize;
            var worldDepth = height * cellSize;
            var origin = grid.CellToWorld(Vector2Int.zero);
            // Nudged just above the base ground plane to avoid z-fighting where it covers it.
            var groundY = origin.y + 0.02f;

            // A hand-built quad (rather than the CreatePrimitive Plane used before) so the
            // texture-to-world mapping is guaranteed exact: texel (x,y) always lands at the same
            // world position as CellToWorld(x,y), which is what SpawnTree/SpawnRock/props use —
            // relying on the primitive Plane's built-in UV orientation previously left the
            // painted ground (water especially) visually offset from where props actually sit.
            var planeGO = new GameObject("MapTerrain");
            planeGO.transform.SetParent(transform, false);

            var mesh = new Mesh
            {
                vertices = new[]
                {
                    new Vector3(origin.x, groundY, origin.z),
                    new Vector3(origin.x + worldWidth, groundY, origin.z),
                    new Vector3(origin.x, groundY, origin.z + worldDepth),
                    new Vector3(origin.x + worldWidth, groundY, origin.z + worldDepth)
                },
                uv = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f)
                },
                triangles = new[] { 0, 2, 1, 2, 3, 1 }
            };
            mesh.RecalculateNormals();

            planeGO.AddComponent<MeshFilter>().sharedMesh = mesh;
            var meshRenderer = planeGO.AddComponent<MeshRenderer>();

            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { mainTexture = texture };
            meshRenderer.sharedMaterial = material;
        }

        private Color ColorFor(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Water: return waterColor;
                case TerrainType.Stone: return stoneColor;
                // Forest is a spawn designation for tree props (see SpawnTree below), not a
                // distinct ground color — the tile itself still reads as ordinary grass.
                case TerrainType.Forest:
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
