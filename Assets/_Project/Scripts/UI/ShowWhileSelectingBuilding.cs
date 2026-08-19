using CityBuilder.Buildings;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>Shows a GameObject only while the player has a building selected for placement -- e.g. the mobile rotate button, meaningless otherwise.</summary>
    public class ShowWhileSelectingBuilding : MonoBehaviour
    {
        [SerializeField] private BuildingPlacer buildingPlacer;
        [SerializeField] private GameObject target;

        /// <summary>
        /// Keeps the target hidden while the mandatory Town Hall is being placed. Rotation is fine
        /// during that phase; CANCELLING is not -- the game has no state where no Town Hall has
        /// been placed and nothing is being placed either, so a cancel button there would offer the
        /// player a dead end.
        /// </summary>
        [SerializeField] private bool hideWhilePlacingMandatory;

        private void Update()
        {
            if (buildingPlacer == null || target == null) return;

            var visible = buildingPlacer.IsSelecting;
            if (hideWhilePlacingMandatory && buildingPlacer.IsPlacingMandatoryBuilding) visible = false;

            target.SetActive(visible);
        }
    }
}
