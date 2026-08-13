using System.Collections;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using UnityEngine;
using UnityEngine.AI;
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
    /// of aiming straight into an obstacle. That cell gets a hollow square outline (yellow while
    /// the order is still in progress, red and brief if the click had nowhere valid to resolve
    /// to) alongside the existing center-screen OK!/NO! flash, so it's obvious both what was
    /// clicked and where the citizen actually ended up heading -- without a filled highlight
    /// blotting out whatever's on that cell.
    /// </summary>
    public class CitizenSelector : MonoBehaviour
    {
        private const float FeedbackSeconds = 1f;
        private const float MarkerHeight = 1.15f;
        private const float MarkerBobSpeed = 3f;
        private const float MarkerBobAmount = 0.08f;
        private const float InvalidHighlightSeconds = 1f;
        private const float HighlightHeightOffset = 0.03f;
        private const float HighlightCellFraction = 0.9f;
        private const float HighlightBorderFraction = 0.14f;
        // How far a clicked point may be pulled onto walkable NavMesh (see TryResolveDestination).
        // Deliberately tight: this is the click's precision budget, and anything larger starts
        // silently sending citizens somewhere the player didn't point at. Enough to forgive a
        // click on a building's wall or a step into the water's edge, not enough to teleport the
        // order across a clearing.
        private const float DestinationSampleRadius = 2f;

        [SerializeField] private Camera targetCamera;
        [SerializeField] private BuildingPlacer buildingPlacer;
        [SerializeField] private Text feedbackText;
        [SerializeField] private LayerMask raycastMask = ~0;

        // Reused across clicks rather than using Physics.RaycastAll, which allocates a fresh array
        // every call. 16 is far more than a click through a forest ever produces.
        private readonly RaycastHit[] _hits = new RaycastHit[16];

        private CitizenAgent _selected;
        private GameObject _marker;
        private GameObject _cellHighlight;
        private Material _cellHighlightMaterial;
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
                if (_cellHighlightPersistent && !_selected.IsExecutingOrder) HideCellHighlight();

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

            // Every hit along the ray, not just the nearest one. A tree's click collider is a box
            // around its whole canopy, so with the camera looking down at an angle it sits between
            // the cursor and any citizen standing near that tree -- a plain Physics.Raycast
            // returned the tree, the citizen check missed, and clicking your own people simply did
            // nothing. Selection now looks past whatever happens to be in front.
            var hitCount = Physics.RaycastNonAlloc(ray, _hits, 500f, raycastMask);
            if (hitCount == 0) return;

            // Citizens win over scenery at any depth: they're the thing the player is aiming at.
            for (var i = 0; i < hitCount; i++)
            {
                var citizen = _hits[i].collider.GetComponent<CitizenAgent>();
                if (citizen == null) continue;

                if (citizen.IsIdle) Select(citizen);
                return;
            }

            if (_selected == null) return;

            // Nothing else has priority, so the nearest hit is what was clicked.
            var nearest = 0;
            for (var i = 1; i < hitCount; i++)
            {
                if (_hits[i].distance < _hits[nearest].distance) nearest = i;
            }
            var hit = _hits[nearest];

            // GetComponentInParent, not GetComponent: a tree's click collider sits on its prefab
            // root but a boulder's cluster parts are children, and either could be what the
            // raycast reports depending on the model.
            var node = hit.collider.GetComponentInParent<ResourceNode>();
            if (node != null)
            {
                CommandGather(node);
                return;
            }

            // The ground under the cursor, taken from the nearest SOLID hit. Trigger props (a
            // tree's canopy box, a boulder, a road tile) are skipped: a canopy box is metres above
            // and to the side of the ground beneath it, so using its contact point as "where the
            // player clicked" skews the destination before the NavMesh ever sees it.
            var clickPoint = hit.point;
            var solid = -1;
            for (var i = 0; i < hitCount; i++)
            {
                if (_hits[i].collider.isTrigger) continue;
                if (solid < 0 || _hits[i].distance < _hits[solid].distance) solid = i;
            }
            if (solid >= 0) clickPoint = _hits[solid].point;

            var resolved = TryResolveDestination(clickPoint, out var destination);

            ShowFeedback(resolved);
            ShowCellHighlight(resolved ? destination : clickPoint, resolved);

            if (resolved) _selected.MoveTo(destination);
        }

        /// <summary>
        /// Nearest walkable point to where the player clicked, straight from the NavMesh.
        ///
        /// Replaces a ring search over grid cells that was wrong in two ways at once. It treated
        /// tree cells as blocked (GridManager marks them occupied) even though trees are fully
        /// walkable now -- no NavMesh carving, trigger colliders only -- so in a forest almost
        /// every click got bounced to a different cell. And when it did bounce, it scanned each
        /// ring from -radius upward and took the first hit, which is always the (-x,-z) corner:
        /// a systematic shove down-and-left, exactly the offset the player measured.
        ///
        /// SamplePosition has neither problem: it returns the CLOSEST walkable point with no
        /// directional bias, and it answers about walkability rather than build-site occupancy,
        /// so a click on open forest floor resolves to itself.
        /// </summary>
        private static bool TryResolveDestination(Vector3 clickPoint, out Vector3 destination)
        {
            if (NavMesh.SamplePosition(clickPoint, out var navHit, DestinationSampleRadius, NavMesh.AllAreas))
            {
                destination = navHit.position;
                return true;
            }

            // No NavMesh at all (a failed bake -- see MeshMapApplier.BuildNavMesh, which warns):
            // fall back to the raw point if it's at least real ground, so click-to-move degrades
            // rather than refusing every order.
            if (MeshMapApplier.Instance != null && MeshMapApplier.Instance.IsGroundAt(clickPoint))
            {
                destination = clickPoint;
                return true;
            }

            destination = default;
            return false;
        }

        /// <summary>
        /// Sends the selected citizen to hand-gather the clicked tree/boulder (see
        /// CitizenAgent.GatherFrom). Refused, with the same red NO! feedback a bad move order
        /// gets, when the node is already spoken for by a Lumberjack worker or another citizen
        /// (ResourceNode.IsClaimed) or is still a growing sapling -- matching the rule
        /// CitizenVisualsManager.FindNearestFreeNode already applies to building workers.
        /// </summary>
        private void CommandGather(ResourceNode node)
        {
            var growth = node.GetComponent<TreeGrowth>();
            var isUnripeSapling = growth != null && !growth.IsFullyGrown;

            if (isUnripeSapling || !node.TryClaim())
            {
                ShowFeedback(false);
                ShowCellHighlight(node.transform.position, false);
                return;
            }

            _selected.GatherFrom(node);
            ShowFeedback(true);
            ShowCellHighlight(node.transform.position, true);
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

        /// <summary>
        /// Hollow square frame (4 thin bars around a unit cell, empty in the middle) instead of a
        /// filled quad -- reads as "this cell" without blotting out whatever's on it (a tree,
        /// part of a building) the way a solid fill did. Built once in local unit-cell space;
        /// ShowCellHighlight scales/positions/colors the whole group per click.
        /// </summary>
        private void EnsureCellHighlight()
        {
            if (_cellHighlight != null) return;

            _cellHighlight = new GameObject("CitizenTargetCellHighlight");
            // Lies flat facing up (local -Z normal -> world +Y) instead of standing upright like
            // a default Quad, matching how a ground-decal-style highlight should sit.
            _cellHighlight.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var t = HighlightBorderFraction;
            CreateHighlightBar(_cellHighlight.transform, "Top", new Vector3(0f, 0.5f - t * 0.5f, 0f), new Vector3(1f, t, 1f));
            CreateHighlightBar(_cellHighlight.transform, "Bottom", new Vector3(0f, -0.5f + t * 0.5f, 0f), new Vector3(1f, t, 1f));
            CreateHighlightBar(_cellHighlight.transform, "Left", new Vector3(-0.5f + t * 0.5f, 0f, 0f), new Vector3(t, 1f, 1f));
            CreateHighlightBar(_cellHighlight.transform, "Right", new Vector3(0.5f - t * 0.5f, 0f, 0f), new Vector3(t, 1f, 1f));

            _cellHighlightMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            foreach (var bar in _cellHighlight.GetComponentsInChildren<Renderer>())
            {
                bar.sharedMaterial = _cellHighlightMaterial;
            }

            _cellHighlight.SetActive(false);
        }

        private static void CreateHighlightBar(Transform parent, string name, Vector3 localPos, Vector3 localScale)
        {
            var bar = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bar.name = name;
            Destroy(bar.GetComponent<Collider>());
            bar.transform.SetParent(parent, false);
            bar.transform.localPosition = localPos;
            bar.transform.localScale = localScale;
        }

        /// <summary>
        /// Snaps the frame onto the cell at worldPos (grid-cell-sized, matching how the ghost
        /// building preview reads during placement). Yellow + stays up while the order is in
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
            _cellHighlightMaterial.color = ok
                ? new Color(0.95f, 0.82f, 0.2f)
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
