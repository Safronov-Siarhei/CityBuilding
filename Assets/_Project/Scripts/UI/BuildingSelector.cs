using CityBuilder.Buildings;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CityBuilder.UI
{
    /// <summary>
    /// Tap/click an already-placed building (when not currently placing a new one) to open its
    /// info panel -- worker assignment for production buildings, upgrade for every building.
    /// </summary>
    public class BuildingSelector : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private BuildingPlacer buildingPlacer;
        [SerializeField] private BuildingInfoPanelController infoPanel;
        [SerializeField] private LayerMask raycastMask = ~0;

        // Reused across clicks rather than using Physics.RaycastAll, which allocates every call.
        private readonly RaycastHit[] _hits = new RaycastHit[16];

        private void Update()
        {
            if (ModalGate.IsBlocked) return;
            if (buildingPlacer != null && buildingPlacer.IsSelecting) return;
            if (targetCamera == null) return;

            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame) return;
            if (IsPointerOverUI()) return;

            var ray = targetCamera.ScreenPointToRay(pointer.position.ReadValue());

            // All hits, not just the nearest: a tree's click collider is a box around its whole
            // canopy, so one standing between the camera and a building would otherwise swallow
            // the click and the info panel would never open. Same fix as CitizenSelector.
            var hitCount = Physics.RaycastNonAlloc(ray, _hits, 500f, raycastMask);

            for (var i = 0; i < hitCount; i++)
            {
                var instance = _hits[i].collider.GetComponent<BuildingInstance>();
                if (instance == null) continue;

                if (infoPanel != null) infoPanel.Show(instance);
                return;
            }
        }

        private static bool IsPointerOverUI()
        {
            if (EventSystem.current == null) return false;

            var touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
            {
                return EventSystem.current.IsPointerOverGameObject(touchscreen.primaryTouch.touchId.ReadValue());
            }

            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}
