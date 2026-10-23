using CityBuilder.Buildings;
using UnityEngine;

namespace CityBuilder.UI
{
    public class HotbarButtonHandler : MonoBehaviour
    {
        [SerializeField] private BuildingPlacer buildingPlacer;
        [SerializeField] private BuildingData building;

        /// <summary>The card's building as the game has it now: in an Invasion run, the run's copy with the invasion tab's numbers (BuildingPlacer.Resolve) -- the card itself is wired to the asset.</summary>
        public BuildingData Building => buildingPlacer != null ? buildingPlacer.Resolve(building) : building;

        public void SelectThisBuilding()
        {
            if (buildingPlacer != null && building != null) buildingPlacer.SelectBuilding(Building);
        }
    }
}
