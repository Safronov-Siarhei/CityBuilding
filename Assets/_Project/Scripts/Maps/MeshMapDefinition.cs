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

        public string MapId => string.IsNullOrEmpty(mapId) ? name : mapId;
        public GameObject GroundPrefab => groundPrefab;
        public GameObject WaterPrefab => waterPrefab;
        public GameObject WaterPlacementZonePrefab => waterPlacementZonePrefab;
        public GameObject TreesAreaPrefab => treesAreaPrefab;
        public GameObject[] TreePrefabs => treePrefabs;

#if UNITY_EDITOR
        public void EditorInitialize(string id, GameObject ground, GameObject water, GameObject waterPlacementZone, GameObject treesArea, GameObject[] trees)
        {
            mapId = id;
            groundPrefab = ground;
            waterPrefab = water;
            waterPlacementZonePrefab = waterPlacementZone;
            treesAreaPrefab = treesArea;
            treePrefabs = trees;
        }
#endif
    }
}
