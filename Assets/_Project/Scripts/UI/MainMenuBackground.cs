using CityBuilder.Maps;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// Instantiates a randomly chosen map's Ground/Water meshes -- terrain only, no grid,
    /// buildings, or citizens -- purely as scenery for the Main Menu's looping camera flythrough
    /// (see MenuCameraFlythrough). Picked fresh each time the Main Menu scene loads.
    /// </summary>
    public class MainMenuBackground : MonoBehaviour
    {
        private void Start()
        {
            var maps = MeshMapCatalog.All;
            if (maps.Count == 0) return;

            var map = maps[Random.Range(0, maps.Count)];

            if (map.GroundPrefab != null)
            {
                Instantiate(map.GroundPrefab, Vector3.zero, map.GroundPrefab.transform.rotation, transform);
            }

            if (map.WaterPrefab != null)
            {
                // Y pinned to -90 the same way MeshMapApplier does for the real gameplay map.
                var sourceEuler = map.WaterPrefab.transform.rotation.eulerAngles;
                var waterRotation = Quaternion.Euler(sourceEuler.x, -90f, sourceEuler.z);
                var waterInstance = Instantiate(map.WaterPrefab, Vector3.zero, waterRotation, transform);
                if (map.WaterMaterial != null)
                {
                    foreach (var renderer in waterInstance.GetComponentsInChildren<Renderer>())
                    {
                        renderer.sharedMaterial = map.WaterMaterial;
                    }
                }
            }
        }
    }
}
