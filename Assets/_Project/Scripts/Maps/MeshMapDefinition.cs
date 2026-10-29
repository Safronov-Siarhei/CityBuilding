using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>
    /// A hand-authored map built from real FBX meshes (Blender-modeled), as opposed to the
    /// older flat/PNG-driven MapDefinition. Lives under Resources/MeshMaps so MeshMapCatalog
    /// picks it up automatically at runtime, same convention as MapDefinition/MapCatalog.
    /// </summary>
    [CreateAssetMenu(fileName = "MeshMap", menuName = "CityBuilder/Mesh Map Definition")]
    public class MeshMapDefinition : ScriptableObject
    {
        [SerializeField] private string mapId;
        [SerializeField] private GameObject groundPrefab;
        [SerializeField] private GameObject waterPrefab;
        [SerializeField] private GameObject waterPlacementZonePrefab;
        [SerializeField] private GameObject treesAreaPrefab;
        [SerializeField] private GameObject[] treePrefabs = new GameObject[0];
        [SerializeField] private GameObject[] rockPrefabs = new GameObject[0];
        [SerializeField] private Material waterMaterial;

        // Map 3 onwards (Tools/blender/map3.py). All optional: a map without them behaves exactly
        // as Maps 1 and 2 always have.
        [Tooltip("Hills and mountains as their own mesh: land for the water test and the camera, but kept out of the NavMesh.")]
        [SerializeField] private GameObject reliefPrefab;
        [Tooltip("Where the boulders go. Without it they are scattered over all dry flat land.")]
        [SerializeField] private GameObject rocksAreaPrefab;
        [Tooltip("Cells nothing may be built on: the slopes and feet of the relief.")]
        [SerializeField] private GameObject noBuildAreaPrefab;
        [SerializeField] private MapDecorData decorData;
        [SerializeField] private Material decorMaterial;
        [Tooltip("Keep the water mesh's own rotation. Map 1's water needs a -90 degree turn the others must not get.")]
        [SerializeField] private bool keepWaterRotation;

        // A map that is always one season, whatever the settings say (the New Year map: winter); -1 for none.
        [SerializeField] private int forcedSeason = -1;
        // Where a designed map puts the orcs (world x, z): the main base's centre and the camps'.
        // A map without them leaves OrcRaidManager to search round the Town Hall.
        [SerializeField] private bool hasMainBaseSpot;
        [SerializeField] private Vector2 mainBaseSpot;
        [SerializeField] private Vector2[] campSpots = new Vector2[0];
        // The level each camp may grow to, by index into campSpots (its ground was made for it).
        [SerializeField] private int[] campLevels = new int[0];
        // Where the Town Hall may stand (world x, z corners, in order); empty = anywhere.
        [SerializeField] private Vector2[] townHallZone = new Vector2[0];
        // Every other spot the generator marked (the Invasion map's bridges, monster camps,
        // shrines, boss lair...), by kind; read by whatever mode needs it.
        [SerializeField] private MapSpot[] otherSpots = new MapSpot[0];

        // The archipelago (the user, 2026-10-29): not meshes from Blender but a template the game
        // generates its islands into from the seed in the map's id (MeshMapApplier.ApplyGenerated).
        [SerializeField] private bool generated;
        [SerializeField] private Material terrainMaterial;
        // no orc raids over land: on the archipelago they will come by sea (later)
        [SerializeField] private bool noRaids;

        public string MapId => string.IsNullOrEmpty(mapId) ? name : mapId;
        public bool IsGenerated => generated;
        public Material TerrainMaterial => terrainMaterial;
        public bool NoRaids => noRaids;
        public GameObject GroundPrefab => groundPrefab;
        public GameObject WaterPrefab => waterPrefab;
        public GameObject WaterPlacementZonePrefab => waterPlacementZonePrefab;
        public GameObject TreesAreaPrefab => treesAreaPrefab;
        public GameObject[] TreePrefabs => treePrefabs;

        /// <summary>The season this map always wears (the New Year map's winter), or null to follow the settings.</summary>
        public Core.MapSeason? ForcedSeason => forcedSeason >= 0 ? (Core.MapSeason)forcedSeason : (Core.MapSeason?)null;
        /// <summary>The hand-gathered boulders, smallest first (Stone1..Stone10) -- RockSpawner relies on that order.</summary>
        public GameObject[] RockPrefabs => rockPrefabs;
        public Material WaterMaterial => waterMaterial;
        public GameObject ReliefPrefab => reliefPrefab;
        public GameObject RocksAreaPrefab => rocksAreaPrefab;
        public GameObject NoBuildAreaPrefab => noBuildAreaPrefab;
        public MapDecorData DecorData => decorData;
        public Material DecorMaterial => decorMaterial;
        public bool KeepWaterRotation => keepWaterRotation;
        public bool HasMainBaseSpot => hasMainBaseSpot;
        public Vector2 MainBaseSpot => mainBaseSpot;
        public Vector2[] CampSpots => campSpots ?? new Vector2[0];
        public int[] CampLevels => campLevels ?? new int[0];
        public Vector2[] TownHallZone => townHallZone ?? new Vector2[0];

        /// <summary>The spots of one kind ("bridge", "monsters", "shrine", "boss"), in the generator's order.</summary>
        public List<MapSpot> SpotsOf(string kind)
        {
            var found = new List<MapSpot>();
            if (otherSpots == null) return found;
            foreach (var spot in otherSpots)
            {
                if (spot.kind == kind) found.Add(spot);
            }
            return found;
        }

#if UNITY_EDITOR
        public void EditorInitialize(string id, GameObject ground, GameObject water, GameObject waterPlacementZone, GameObject treesArea, GameObject[] trees, GameObject[] rocks, Material waterMat)
        {
            mapId = id;
            groundPrefab = ground;
            waterPrefab = water;
            waterPlacementZonePrefab = waterPlacementZone;
            treesAreaPrefab = treesArea;
            treePrefabs = trees;
            rockPrefabs = rocks;
            waterMaterial = waterMat;
        }

        public void EditorForceSeason(Core.MapSeason season) => forcedSeason = (int)season;

        public void EditorInitializeGenerated(Material terrain, bool withoutRaids)
        {
            generated = true;
            terrainMaterial = terrain;
            noRaids = withoutRaids;
        }

        public void EditorInitializeExtras(GameObject relief, GameObject rocksArea, GameObject noBuildArea, MapDecorData decor, Material decorMat, bool keepWaterRot)
        {
            reliefPrefab = relief;
            rocksAreaPrefab = rocksArea;
            noBuildAreaPrefab = noBuildArea;
            decorData = decor;
            decorMaterial = decorMat;
            keepWaterRotation = keepWaterRot;
        }

        public void EditorInitializeSpots(Vector2? mainBase, Vector2[] camps, int[] levels, Vector2[] zone, MapSpot[] others = null)
        {
            otherSpots = others ?? new MapSpot[0];
            hasMainBaseSpot = mainBase.HasValue;
            mainBaseSpot = mainBase ?? Vector2.zero;
            campSpots = camps ?? new Vector2[0];
            campLevels = levels ?? new int[0];
            townHallZone = zone ?? new Vector2[0];
        }
#endif
    }

    /// <summary>A marked place on a designed map: its kind, where (world x, z) and a level where the kind has one.</summary>
    [System.Serializable]
    public struct MapSpot
    {
        public string kind;
        public Vector2 position;
        public int level;
    }
}
