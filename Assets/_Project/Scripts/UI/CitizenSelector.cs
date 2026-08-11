using System.Collections;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// Tap/click an idle citizen to select it (a small floating marker appears above its head),
    /// then tap/click anywhere else to send it walking there. Only idle citizens respond
    /// (CitizenAgent.IsIdle) -- redirecting a working citizen mid-commute would desync
    /// CitizenVisualsManager's job bookkeeping (ProductionBuilding.AssignedWorkers, claimed
    /// ResourceNodes) without a matching release, so those clicks are simply ignored.
    ///
    /// Every destination click resolves to one grid cell (see ResolveDestinationCell) -- a
    /// building/tree-occupied cell is redirected to the nearest free walkable cell nearby instead
    /// of aiming straight into an obstacle. That cell is highlighted (green while the order is
    /// still in progress, red and brief if the click had nowhere valid to resolve to) alongside
    /// the existing center-screen OK!/NO! flash, so it's obvious both what was clicked and where
    /// the citizen actually ended up heading.
    /// </summary>
    public class CitizenSelector : MonoBehaviour
    {
        private const float FeedbackSeconds = 1f;
        private const float MarkerHeight = 1.15f;
        private const float MarkerBobSpeed = 3f;
        private const float MarkerBobAmount = 0.08f;
        private const float InvalidHighlightSeconds = 1f;
        private const float HighlightHeightOffset = 0.03f;
        private const float HighlightCellFraction = 0.85f;
        // How far (in cells) to search for a free, walkable cell when the clicked cell itself is
        // occupied by a building -- ring-expanding search, so a click on a building sends the
        // citizen to stand next to it instead of walking straight into its collider and getting
        // permanently stuck (see CitizenAgent.MaxManualMoveStuckRetries).
        private const int OccupiedSearchRadiusCells = 4;

        [SerializeField] private Camera targetCamera;
        [SerializeField] private BuildingPlacer buildingPlacer;
        [SerializeField] private Text feedbackText;
        [SerializeField] private LayerMask raycastMask = ~0;

        private CitizenAgent _selected;
        private GameObject _marker;
        private GameObject _cellHighlight;
        private bool _cellHighlightPersistent;
        private Coroutine _feedbackRoutine;
        private Coroutine _highlightRoutine;

        private void Update()
        {
            if (ModalGate.IsBlocked) return;
            if (buildingPlacer != null && buildingPlacer.IsSelecting) return;
            if (targetCamera == null) return;

            if (_selected != null)
            {
                BobMarker();

                // The persistent (green, order-in-progress) highlight tracks the agent's own
                // manual-move state rather than a fixed timer -- it disappears the moment the
                // citizen either arrives or gives up (see CitizenAgent.OnStuck's retry cap).
                if (_cellHighlightPersistent && !_selected.IsManualMoving) HideCellHighlight();

                var keyboard = Keyboard.current;
                if (keyboard != null && keyboard[Key.Escape].wasPressedThisFrame)
                {
                    Deselect();
                    return;
                }
                if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                {
                    Deselect();
                    return;
                }
            }

            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame) return;
            if (IsPointerOverUI()) return;

            var ray = targetCamera.ScreenPointToRay(pointer.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit, 500f, raycastMask)) return;

            var citizen = hit.collider.GetComponent<CitizenAgent>();
            if (citizen != null)
            {
                if (citizen.IsIdle) Select(citizen);
                return;
            }

            if (_selected == null) return;

            var grid = GridManager.Instance;
            if (grid == null) return;

            var clickedCell = grid.WorldToCell(hit.point);
            var resolved = ResolveDestinationCell(grid, clickedCell, out var destination);

            ShowFeedback(resolved);
            ShowCellHighlight(resolved ? destination : grid.GetFootprintCenterWorld(clickedCell, Vector2Int.one), resolved);

            if (resolved) _selected.MoveTo(destination);
        }

        /// <summary>
        /// True + destination if clickedCell (or, when that's occupied by a building, the
        /// nearest free walkable cell within OccupiedSearchRadiusCells) sits on real flat ground
        /// -- see MeshMapApplier.IsGroundAt. Ignores trees entirely (GridManager has no concept
        /// of them; only buildings occupy cells) -- a tree-blocked destination still resolves
        /// here and relies on CitizenAgent's stuck-retry give-up instead.
        /// </summary>
        private static bool ResolveDestinationCell(GridManager grid, Vector2Int clickedCell, out Vector3 destination)
        {
            for (var radius = 0; radius <= OccupiedSearchRadiusCells; radius++)
            {
                for (var dx = -radius; dx <= radius; dx++)
                {
                    for (var dz = -radius; dz <= radius; dz++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != radius) continue; // ring only, nearest radius first

                        var candidate = clickedCell + new Vector2Int(dx, dz);
                        if (!grid.IsWithinBounds(candidate, Vector2Int.one) || !grid.IsAreaFree(candidate, Vector2Int.one)) continue;

                        var worldPos = grid.GetFootprintCenterWorld(candidate, Vector2Int.one);
                        if (MeshMapApplier.Instance != null && !MeshMapApplier.Instance.IsGroundAt(worldPos)) continue;

                        destination = worldPos;
                        return true;
                    }
                }
            }

            destination = default;
            return false;
        }

        private void Select(CitizenAgent citizen)
        {
            _selected = citizen;
            if (_marker == null) _marker = CreateMarker();
            _marker.SetActive(true);
            HideCellHighlight();
        }

        private void Deselect()
        {
            _selected = null;
            if (_marker != null) _marker.SetActive(false);
            HideCellHighlight();
        }

        private void BobMarker()
        {
            if (_marker == null || _selected == null) return;
            var bob = Mathf.Sin(Time.time * MarkerBobSpeed) * MarkerBobAmount;
            _marker.transform.position = _selected.transform.position + new Vector3(0f, MarkerHeight + bob, 0f);
        }

        private static GameObject CreateMarker()
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "CitizenSelectionMarker";
            Destroy(marker.GetComponent<BoxCollider>());
            marker.transform.localScale = new Vector3(0.16f, 0.16f, 0.16f);
            marker.transform.rotation = Quaternion.Euler(45f, 45f, 0f);
            marker.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = new Color(1f, 0.85f, 0.2f)
            };
            return marker;
        }

        private void EnsureCellHighlight()
        {
            if (_cellHighlight != null) return;

            _cellHighlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _cellHighlight.name = "CitizenTargetCellHighlight";
            Destroy(_cellHighlight.GetComponent<Collider>());
            // Lies flat facing up (local -Z normal -> world +Y) instead of standing upright like
            // a default Quad, matching how a ground-decal-style cell highlight should sit.
            _cellHighlight.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _cellHighlight.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            _cellHighlight.SetActive(false);
        }

        /// <summary>
        /// Snaps a flat quad onto the cell at worldPos (grid-cell-sized, matching how the ghost
        /// building preview reads during placement). Green + stays up while the order is in
        /// progress (persistent=true, cleared once IsManualMoving goes false); red + a one-second
        /// flash when the click had nowhere valid to resolve to.
        /// </summary>
        private void ShowCellHighlight(Vector3 worldPos, bool ok)
        {
            var grid = GridManager.Instance;
            if (grid == null) return;

            EnsureCellHighlight();

            _cellHighlight.transform.position = new Vector3(worldPos.x, grid.GroundHeight + HighlightHeightOffset, worldPos.z);
            var size = grid.CellSize * HighlightCellFraction;
            _cellHighlight.transform.localScale = new Vector3(size, size, 1f);
            _cellHighlight.GetComponent<Renderer>().sharedMaterial.color = ok
                ? new Color(0.35f, 0.9f, 0.35f)
                : new Color(0.9f, 0.3f, 0.25f);
            _cellHighlight.SetActive(true);
            _cellHighlightPersistent = ok;

            if (_highlightRoutine != null)
            {
                StopCoroutine(_highlightRoutine);
                _highlightRoutine = null;
            }
            if (!ok) _highlightRoutine = StartCoroutine(HideHighlightAfterDelay());
        }

        private void HideCellHighlight()
        {
            if (_highlightRoutine != null)
            {
                StopCoroutine(_highlightRoutine);
                _highlightRoutine = null;
            }
            _cellHighlightPersistent = false;
            if (_cellHighlight != null) _cellHighlight.SetActive(false);
        }

        private IEnumerator HideHighlightAfterDelay()
        {
            yield return new WaitForSeconds(InvalidHighlightSeconds);
            if (_cellHighlight != null) _cellHighlight.SetActive(false);
            _highlightRoutine = null;
        }

        private void ShowFeedback(bool ok)
        {
            if (feedbackText == null) return;

            feedbackText.text = ok ? "OK!" : "NO!";
            feedbackText.color = ok ? new Color(0.4f, 0.9f, 0.4f) : new Color(0.95f, 0.35f, 0.3f);
            feedbackText.gameObject.SetActive(true);

            if (_feedbackRoutine != null) StopCoroutine(_feedbackRoutine);
            _feedbackRoutine = StartCoroutine(HideFeedbackAfterDelay());
        }

        private IEnumerator HideFeedbackAfterDelay()
        {
            yield return new WaitForSeconds(FeedbackSeconds);
            if (feedbackText != null) feedbackText.gameObject.SetActive(false);
            _feedbackRoutine = null;
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
