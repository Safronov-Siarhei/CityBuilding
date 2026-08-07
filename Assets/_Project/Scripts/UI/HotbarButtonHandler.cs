using CityBuilder.Buildings;
using UnityEngine;

namespace CityBuilder.UI
{
    public class HotbarButtonHandler : MonoBehaviour
    {
        [SerializeField] private BuildingPlacer buildingPlacer;
        [SerializeField] private BuildingData building;

        public void SelectThisBuilding()
        {
            if (buildingPlacer != null && building != null) buildingPlacer.SelectBuilding(building);
        }
    }
}
