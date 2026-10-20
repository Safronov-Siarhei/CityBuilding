using CityBuilder.Buildings;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The controls placement needs on a phone: a crosshair showing where the building will land,
    /// a confirm button, rotate, cancel, and one line of text saying why confirm is dark.
    ///
    /// Replaces ShowWhileSelectingBuilding, which could only answer "is something selected". The
    /// question here has four parts -- selected, on touch, drawing a line, and placing the
    /// mandatory Town Hall -- and each control answers a different combination of them.
    /// </summary>
    public class PlacementHudController : MonoBehaviour
    {
        [SerializeField] private BuildingPlacer buildingPlacer;

        /// <summary>Confirm. Touch only: a mouse places by clicking the cell, the way it always has.</summary>
        [SerializeField] private GameObject confirmButton;

        [SerializeField] private Image confirmImage;
        [SerializeField] private GameObject rotateButton;
        [SerializeField] private GameObject cancelButton;

        /// <summary>Crosshair at the aim point. Without it the fixed ghost looks like it is stuck rather than aimed.</summary>
        [SerializeField] private RectTransform aimMarker;

        [SerializeField] private Text statusLabel;

        private static readonly Color ConfirmReady = new Color(0.35f, 0.75f, 0.4f, 1f);
        private static readonly Color ConfirmBlocked = new Color(0.35f, 0.35f, 0.35f, 0.8f);

        private void Update()
        {
            if (buildingPlacer == null) return;

            var selecting = buildingPlacer.IsSelecting && !ModalGate.IsBlocked;
            var touchAiming = selecting && buildingPlacer.UsesTouchAiming;
            var chain = selecting && buildingPlacer.IsChainMode;

            // Confirm belongs to every placement on a touch screen, roads and fences included:
            // each piece is put down on its own tick (the user's call, 2026-10-20).
            SetActive(confirmButton, touchAiming);
            SetActive(aimMarker != null ? aimMarker.gameObject : null, touchAiming);

            // Rotation is meaningless for a road or a fence -- their shape comes from their
            // neighbours -- but it IS allowed while placing the mandatory Town Hall.
            SetActive(rotateButton, selecting && !chain);

            // Cancel is hidden for the Town Hall: the game has no state where nothing is being
            // placed and no Town Hall exists, so cancelling there would be a dead end.
            SetActive(cancelButton, selecting && !buildingPlacer.IsPlacingMandatoryBuilding);

            if (aimMarker != null && touchAiming)
            {
                aimMarker.position = buildingPlacer.AimScreenPosition;
            }

            if (confirmImage != null) confirmImage.color = buildingPlacer.CanConfirm ? ConfirmReady : ConfirmBlocked;

            UpdateStatus(selecting);
        }

        /// <summary>The confirm button: places the building (or the road or fence piece) in hand.</summary>
        public void ConfirmPressed()
        {
            if (buildingPlacer == null) return;
            buildingPlacer.ConfirmPlacement();
        }

        private void UpdateStatus(bool selecting)
        {
            if (statusLabel == null) return;

            if (!selecting)
            {
                statusLabel.gameObject.SetActive(false);
                return;
            }

            var text = buildingPlacer.StatusText;

            statusLabel.gameObject.SetActive(!string.IsNullOrEmpty(text));
            statusLabel.text = text;
            statusLabel.color = buildingPlacer.CanConfirm
                ? new Color(0.85f, 0.9f, 0.85f)
                : new Color(1f, 0.72f, 0.62f);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active) target.SetActive(active);
        }
    }
}
