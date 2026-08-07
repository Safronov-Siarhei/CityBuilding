using CityBuilder.Buildings;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// Toggles UI panels based on whether the mandatory first building (Town Hall) is still
    /// being placed: the placement hint shows only during that phase, the building hotbar only
    /// after it, since there's nothing else to build until the player commits to a location.
    /// </summary>
    public class BuildingPlacerUIVisibility : MonoBehaviour
    {
        [SerializeField] private BuildingPlacer buildingPlacer;
        [SerializeField] private GameObject showWhilePlacingMandatory;
        [SerializeField] private GameObject hideWhilePlacingMandatory;

        private void Update()
        {
            if (buildingPlacer == null) return;
            var mandatory = buildingPlacer.IsPlacingMandatoryBuilding;
            if (showWhilePlacingMandatory != null) showWhilePlacingMandatory.SetActive(mandatory);
            if (hideWhilePlacingMandatory != null) hideWhilePlacingMandatory.SetActive(!mandatory);
        }
    }
}
