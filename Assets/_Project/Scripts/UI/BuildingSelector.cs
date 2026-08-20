using CityBuilder.Buildings;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// Opens an already-placed building's card -- worker assignment, upgrade, repair, demolition.
    ///
    /// Driven by WorldInputDispatcher rather than by its own pointer polling. The tap that gets
    /// here has already been confirmed as a tap (finger released without travelling) and has
    /// already lost to placement, army orders and citizen orders, so all the mutual stand-down
    /// checks this class used to carry are gone.
    /// </summary>
    public class BuildingSelector : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private BuildingInfoPanelController infoPanel;

        /// <summary>The Laboratory opens this instead of the ordinary card -- see ResearchPanelController, whose header carries the worker/upgrade/repair controls it displaces.</summary>
        [SerializeField] private ResearchPanelController researchPanel;

        [SerializeField] private LayerMask raycastMask = ~0;

        // Reused across clicks rather than using Physics.RaycastAll, which allocates every call.
        // Sized for a shallow camera ray crossing tens of metres of forest: RaycastNonAlloc
        // returns hits unsorted, so a buffer that fills up can drop the clicked building itself
        // and keep a dozen tree click boxes instead. Same sizing as CitizenSelector.
        private readonly RaycastHit[] _hits = new RaycastHit[64];

        /// <summary>
        /// Opens the card of the building under this screen point. A tap on open ground closes
        /// whatever card is open instead -- on a phone the card covers a good part of the screen
        /// and hunting for its close button is a chore.
        /// </summary>
        public void HandleWorldTap(Vector2 screenPosition)
        {
            if (targetCamera == null) return;

            var nearest = FindBuilding(screenPosition);
            if (nearest == null)
            {
                if (infoPanel != null) infoPanel.Close();
                if (researchPanel != null) researchPanel.Close();
                return;
            }

            if (researchPanel != null && nearest.Data != null
                && nearest.Data.buildingName == Research.ResearchManager.LaboratoryBuildingId)
            {
                researchPanel.Show(nearest);
                return;
            }

            if (infoPanel != null) infoPanel.Show(nearest);
        }

        /// <summary>
        /// The nearest building along the ray, looking past everything else. All hits, not just
        /// the closest collider: a tree's click box is a box around its whole canopy, so one
        /// standing between the camera and a building would otherwise swallow the tap entirely.
        /// </summary>
        private BuildingInstance FindBuilding(Vector2 screenPosition)
        {
            var ray = targetCamera.ScreenPointToRay(screenPosition);
            var hitCount = Physics.RaycastNonAlloc(ray, _hits, 500f, raycastMask);

            BuildingInstance nearest = null;
            var nearestDistance = float.MaxValue;
            for (var i = 0; i < hitCount; i++)
            {
                if (_hits[i].distance >= nearestDistance) continue;
                var instance = _hits[i].collider.GetComponent<BuildingInstance>();
                if (instance == null) continue;

                nearest = instance;
                nearestDistance = _hits[i].distance;
            }

            return nearest;
        }
    }
}
