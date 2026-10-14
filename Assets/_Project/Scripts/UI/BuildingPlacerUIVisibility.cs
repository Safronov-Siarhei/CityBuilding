using CityBuilder.Buildings;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// Toggles UI panels around placement: the placement hint shows only while the mandatory Town
    /// Hall is being placed, and the build menu shows only when nothing is being placed at all --
    /// while a building is in hand the placement controls (cancel, rotate, confirm) take its
    /// place at the bottom of the screen, and it comes back once the building is placed or
    /// dropped (the user's layout, 2026-10-14).
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
            if (showWhilePlacingMandatory != null && showWhilePlacingMandatory.activeSelf != mandatory)
            {
                showWhilePlacingMandatory.SetActive(mandatory);
            }
            var showMenu = !mandatory && !buildingPlacer.IsSelecting;
            if (hideWhilePlacingMandatory != null && hideWhilePlacingMandatory.activeSelf != showMenu)
            {
                hideWhilePlacingMandatory.SetActive(showMenu);
            }
        }
    }
}
