using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>
    /// A pre-made 100x100 terrain layout, produced offline by MapImporter from a reference
    /// map image and picked at runtime by MapSelector. Lives under a Resources/Maps folder so
    /// dropping in a new map only needs an import pass — no scene or catalog wiring.
    /// </summary>
    [CreateAssetMenu(fileName = "Map", menuName = "CityBuilder/Map Definition")]
    public class MapDefinition : ScriptableObject
    {
        [SerializeField] private string mapId;
        [SerializeField] private int width = 100;
        [SerializeField] private int height = 100;
        [SerializeField] private byte[] cells = new byte[0];

        public string MapId => string.IsNullOrEmpty(mapId) ? name : mapId;
        public int Width => width;
        public int Height => height;

        public TerrainType GetCell(int x, int y)
        {
            var index = y * width + x;
            if (cells == null || index < 0 || index >= cells.Length) return TerrainType.Grass;
            return (TerrainType)cells[index];
        }

#if UNITY_EDITOR
        public void EditorInitialize(string id, int w, int h, byte[] data)
        {
            mapId = id;
            width = w;
            height = h;
            cells = data;
        }
#endif
    }
}
