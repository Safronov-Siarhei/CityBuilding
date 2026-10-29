using UnityEngine;

namespace CityBuilder.Maps.Archipelago
{
    /// <summary>
    /// One island of the library (Tools/blender/islands.py, the user, 2026-10-29: islands made in
    /// Blender at the designed maps' quality, which the seed picks, turns and lays out). Its meshes
    /// -- the plain, the relief, the coastal water -- centred on its square frame; its cells, a byte
    /// each (see the flags); its props; and the spots its kind needs, in frame coordinates (metres
    /// from the frame's centre, x east, y north).
    /// </summary>
    public class IslandAsset : ScriptableObject
    {
        public const byte Land = 1, NoBuild = 2, Forest = 4, Stony = 8, Shore = 16;

        [SerializeField] private string islandName;
        [SerializeField] private ArchipelagoGenerator.Kind kind;
        [SerializeField] private int frame;
        [SerializeField] private byte[] cells = new byte[0];
        [SerializeField] private GameObject ground;
        [SerializeField] private GameObject relief;
        [SerializeField] private GameObject water;
        [SerializeField] private MapDecorData decor;
        [SerializeField] private Vector2[] startZone = new Vector2[0];
        [SerializeField] private bool hasMainBase;
        [SerializeField] private Vector2 mainBase;
        [SerializeField] private bool hasBot;
        [SerializeField] private Vector2 bot;
        [SerializeField] private Vector2[] beaches = new Vector2[0];

        public string IslandName => islandName;
        public ArchipelagoGenerator.Kind Kind => kind;
        /// <summary>The square frame's side in metres (cells); even, so a quarter turn keeps every cell on a cell.</summary>
        public int Frame => frame;
        public GameObject Ground => ground;
        public GameObject Relief => relief;
        public GameObject Water => water;
        public MapDecorData Decor => decor;
        public Vector2[] StartZone => startZone;
        public bool HasMainBase => hasMainBase;
        public Vector2 MainBase => mainBase;
        public bool HasBot => hasBot;
        public Vector2 Bot => bot;
        public Vector2[] Beaches => beaches;

        /// <summary>The flags of cell (i, j), i east and j north from the frame's south-west corner.</summary>
        public byte Cell(int i, int j) => cells[j * frame + i];

#if UNITY_EDITOR
        public void EditorInitialize(string name, ArchipelagoGenerator.Kind islandKind, byte[] cellBytes, GameObject groundMesh,
            GameObject reliefMesh, GameObject waterMesh, MapDecorData props, Vector2[] zone, Vector2? main, Vector2? botSpot, Vector2[] beachSpots)
        {
            islandName = name;
            kind = islandKind;
            frame = cellBytes[0] | (cellBytes[1] << 8);
            cells = new byte[cellBytes.Length - 2];
            System.Array.Copy(cellBytes, 2, cells, 0, cells.Length);
            ground = groundMesh;
            relief = reliefMesh;
            water = waterMesh;
            decor = props;
            startZone = zone ?? new Vector2[0];
            hasMainBase = main.HasValue;
            mainBase = main ?? Vector2.zero;
            hasBot = botSpot.HasValue;
            bot = botSpot ?? Vector2.zero;
            beaches = beachSpots ?? new Vector2[0];
        }
#endif
    }
}
