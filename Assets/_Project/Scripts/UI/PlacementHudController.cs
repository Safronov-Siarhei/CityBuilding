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
        /// <summary>Over the building in hand: the food bonus it would get or give there (the user, 2026-11-10).</summary>
        [SerializeField] private Text bonusLabel;

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
            UpdateBonus(selecting);
        }

        private int _shownBonus = -1, _shownReached = -1;

        /// <summary>
        /// A Farm, Fisher's Hut or Pig Farm aimed inside a working Bakery's/Smokehouse's reach shows
        /// "+N" over it; a Bakery or Smokehouse shows "+N x M", the producers it would feed.
        /// </summary>
        private void UpdateBonus(bool selecting)
        {
            if (bonusLabel == null) return;
            var data = selecting ? buildingPlacer.SelectedBuilding : null;
            var show = false;
            if (data != null && Citizens.FoodOutput.IsFood(data.buildingName) && buildingPlacer.TryGetGhostCentre(out var centre))
            {
                int bonus, reached = 0;
                if (Citizens.FoodOutput.IsProducer(data.buildingName))
                    bonus = Citizens.FoodOutput.BoostAt(data.buildingName, centre);
                else
                {
                    var config = BalanceConfig.Instance;
                    bonus = config != null ? config.FoodBoost(data.buildingName, 1) : 0;
                    reached = Citizens.FoodOutput.ReachedFrom(data.buildingName, centre, data.LevelStats(1).serviceRadius);
                    if (reached == 0) bonus = 0;
                }
                show = bonus > 0;
                if (show)
                {
                    if (bonus != _shownBonus || reached != _shownReached)
                    {
                        _shownBonus = bonus;
                        _shownReached = reached;
                        bonusLabel.text = reached > 0 ? Localization.Format("#place_food_boosts", bonus, reached) : Localization.Format("#place_food_bonus", bonus);
                    }
                    var camera = UnityEngine.Camera.main;
                    if (camera != null)
                    {
                        var height = data.footprintSize.x * 0.9f + 1.6f;
                        // bobbing gently, a sign over the building
                        bonusLabel.rectTransform.position = camera.WorldToScreenPoint(centre + Vector3.up * height)
                            + Vector3.up * (6f * Mathf.Sin(Time.unscaledTime * 3f));
                    }
                }
            }
            if (bonusLabel.gameObject.activeSelf != show) bonusLabel.gameObject.SetActive(show);
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
