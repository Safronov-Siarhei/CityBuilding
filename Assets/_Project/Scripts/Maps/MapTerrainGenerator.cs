using CityBuilder.Grid;
using CityBuilder.Saving;
using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>
    /// Applies the map picked for this session — a new game's random pick (GameSessionIntent) or
    /// a loaded save's stored map (GameSaveController) — by painting the buildable area with a
    /// terrain texture and blocking placement on water cells. With no resolved map (e.g. the
    /// scene opened directly in the Editor, or no maps imported yet) it does nothing and the
    /// scene's plain ground plane shows as before.
    /// </summary>
    public class MapTerrainGenerator : MonoBehaviour
    {
        [SerializeField] private GameSaveController saveController;
        [SerializeField] private Color waterColor = new Color(0.25f, 0.5f, 0.75f);
        [SerializeField] private Color grassColor = new Color(0.42f, 0.62f, 0.32f);
        [SerializeField] private Color forestColor = new Color(0.16f, 0.38f, 0.18f);
        [SerializeField] private Color stoneColor = new Color(0.58f, 0.56f, 0.52f);

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

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var terrain = map.GetCell(x, y);
                    texture.SetPixel(x, y, ColorFor(terrain));

                    if (terrain == TerrainType.Water)
                    {
                        grid.SetAreaOccupied(new Vector2Int(x, y), Vector2Int.one, true);
                    }
                }
            }
            texture.Apply();

            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "MapTerrain";
            Destroy(plane.GetComponent<Collider>());
            plane.transform.SetParent(transform, false);

            var cellSize = grid.CellSize;
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
    }
}
