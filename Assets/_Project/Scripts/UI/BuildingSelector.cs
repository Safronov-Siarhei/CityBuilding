using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CityBuilder.UI
{
    /// <summary>
    /// Tap/click an already-placed production building (when not currently placing a new one)
    /// to open its worker-assignment panel.
    /// </summary>
    public class BuildingSelector : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private BuildingPlacer buildingPlacer;
        [SerializeField] private BuildingInfoPanelController infoPanel;
        [SerializeField] private LayerMask raycastMask = ~0;

        private void Update()
        {
            if (ModalGate.IsBlocked) return;
            if (buildingPlacer != null && buildingPlacer.IsSelecting) return;
            if (targetCamera == null) return;

            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame) return;
            if (IsPointerOverUI()) return;

            var ray = targetCamera.ScreenPointToRay(pointer.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit, 500f, raycastMask)) return;

            var production = hit.collider.GetComponent<ProductionBuilding>();
            if (production != null && infoPanel != null)
            {
                infoPanel.Show(production);
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
