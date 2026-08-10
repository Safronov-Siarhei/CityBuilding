using CityBuilder.Buildings;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>Shows a GameObject only while the player has a building selected for placement -- e.g. the mobile rotate button, meaningless otherwise.</summary>
    public class ShowWhileSelectingBuilding : MonoBehaviour
    {
        [SerializeField] private BuildingPlacer buildingPlacer;
        [SerializeField] private GameObject target;

        private void Update()
        {
            if (buildingPlacer == null || target == null) return;
            target.SetActive(buildingPlacer.IsSelecting);
        }
    }
}
