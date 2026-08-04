using CityBuilder.Buildings;
using UnityEngine;

namespace CityBuilder.UI
{
    public class PlacementHintUI : MonoBehaviour
    {
        [SerializeField] private BuildingPlacer buildingPlacer;
        [SerializeField] private GameObject hintRoot;

        private void Update()
        {
            if (buildingPlacer == null || hintRoot == null) return;
            hintRoot.SetActive(buildingPlacer.IsPlacingMandatoryBuilding);
        }
    }
}
