using System.Collections;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Core;
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
    /// Every destination click flashes "OK!"/"NO!" for a second (see MeshMapApplier.IsGroundHit)
    /// depending on whether it landed on the actual Map-1-Ground mesh -- water, buildings, trees
    /// and empty space off the map all read as NO! and don't move the citizen.
    /// </summary>
    public class CitizenSelector : MonoBehaviour
    {
        private const float FeedbackSeconds = 1f;
        private const float MarkerHeight = 1.15f;
        private const float MarkerBobSpeed = 3f;
        private const float MarkerBobAmount = 0.08f;

        [SerializeField] private Camera targetCamera;
        [SerializeField] private BuildingPlacer buildingPlacer;
        [SerializeField] private Text feedbackText;
        [SerializeField] private LayerMask raycastMask = ~0;

        private CitizenAgent _selected;
        private GameObject _marker;
        private Coroutine _feedbackRoutine;

        private void Update()
        {
            if (ModalGate.IsBlocked) return;
            if (buildingPlacer != null && buildingPlacer.IsSelecting) return;
            if (targetCamera == null) return;

            if (_selected != null)
            {
                BobMarker();

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

            var onGround = MeshMapApplier.IsGroundHit(hit);
            ShowFeedback(onGround);
            if (onGround) _selected.MoveTo(hit.point);
        }

        private void Select(CitizenAgent citizen)
        {
            _selected = citizen;
            if (_marker == null) _marker = CreateMarker();
            _marker.SetActive(true);
        }

        private void Deselect()
        {
            _selected = null;
            if (_marker != null) _marker.SetActive(false);
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
