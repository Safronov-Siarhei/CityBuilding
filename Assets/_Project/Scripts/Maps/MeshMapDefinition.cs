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

        public string MapId => string.IsNullOrEmpty(mapId) ? name : mapId;
        public GameObject GroundPrefab => groundPrefab;
        public GameObject WaterPrefab => waterPrefab;
        public GameObject WaterPlacementZonePrefab => waterPlacementZonePrefab;
        public GameObject TreesAreaPrefab => treesAreaPrefab;
        public GameObject[] TreePrefabs => treePrefabs;
        /// <summary>The hand-gathered boulders, smallest first (Stone1..Stone10) -- RockSpawner relies on that order.</summary>
        public GameObject[] RockPrefabs => rockPrefabs;
        public Material WaterMaterial => waterMaterial;
        public GameObject ReliefPrefab => reliefPrefab;
        public GameObject RocksAreaPrefab => rocksAreaPrefab;
        public GameObject NoBuildAreaPrefab => noBuildAreaPrefab;
        public MapDecorData DecorData => decorData;
        public Material DecorMaterial => decorMaterial;
        public bool KeepWaterRotation => keepWaterRotation;

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

        public void EditorInitializeExtras(GameObject relief, GameObject rocksArea, GameObject noBuildArea, MapDecorData decor, Material decorMat, bool keepWaterRot)
        {
            reliefPrefab = relief;
            rocksAreaPrefab = rocksArea;
            noBuildAreaPrefab = noBuildArea;
            decorData = decor;
            decorMaterial = decorMat;
            keepWaterRotation = keepWaterRot;
        }
#endif
    }
}
