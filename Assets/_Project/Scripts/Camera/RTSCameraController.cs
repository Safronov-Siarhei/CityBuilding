using CityBuilder.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CityBuilder.CameraControl
{
    public class RTSCameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform pivot;
        [SerializeField] private Transform cameraTransform;

        [Header("Pan (PC)")]
        [SerializeField] private float panSpeed = 50f;
        [SerializeField] private bool edgeScrollEnabled = true;
        [SerializeField] private float edgeScrollBorder = 12f;
        [SerializeField] private Vector2 panBoundsMin = new Vector2(-100f, -100f);
        [SerializeField] private Vector2 panBoundsMax = new Vector2(100f, 100f);

        [Header("Zoom (PC)")]
        [SerializeField] private float zoomSpeed = 40f;
        [SerializeField] private float minDistance = 8f;
        [SerializeField] private float maxDistance = 220f;

        [Header("Rotate (PC only)")]
        [SerializeField] private float rotateSpeed = 90f;

        [Header("Touch")]
        [SerializeField] private float touchPanSpeed = 0.06f;
        [SerializeField] private float touchZoomSpeed = 0.05f;

        private float _lastPinchDistance;

        private void Update()
        {
            if (ModalGate.IsBlocked) return;

            var dt = Time.unscaledDeltaTime;

            var handledByTouch = HandleTouch(dt);
            if (!handledByTouch)
            {
                var keyboard = Keyboard.current;
                var mouse = Mouse.current;
                if (keyboard != null && mouse != null)
                {
                    HandlePan(keyboard, mouse, dt);
                    HandleZoom(mouse, dt);
                    HandleRotate(keyboard, mouse, dt);
                }
            }
        }

        private bool HandleTouch(float dt)
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null) return false;

            var touches = touchscreen.touches;
            var activeCount = 0;
            Vector2 posA = default;
            Vector2 posB = default;

            for (var i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (!touch.press.isPressed) continue;

                if (activeCount == 0) posA = touch.position.ReadValue();
                else if (activeCount == 1) posB = touch.position.ReadValue();
                activeCount++;
            }

            if (activeCount == 0)
            {
                _lastPinchDistance = 0f;
                return false;
            }

            if (activeCount == 1)
            {
                _lastPinchDistance = 0f;
                var delta = touchscreen.primaryTouch.delta.ReadValue();
                if (delta != Vector2.zero) PanByScreenDelta(delta, touchPanSpeed);
            }
            else
            {
                var distance = Vector2.Distance(posA, posB);
                if (_lastPinchDistance > 0f)
                {
                    // Fingers spreading apart (distance increasing) should zoom in, i.e. reduce
                    // the camera's distance from the pivot — hence the sign flip.
                    var pinchDelta = distance - _lastPinchDistance;
                    ApplyZoomDelta(-pinchDelta * touchZoomSpeed);
                }
                _lastPinchDistance = distance;
            }

            return true;
        }

        private void PanByScreenDelta(Vector2 screenDelta, float speed)
        {
            var forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();
            var right = transform.right;
            right.y = 0f;
            right.Normalize();

            // Dragging the finger moves the world with it, so the camera moves opposite the delta.
            var move = (-right * screenDelta.x - forward * screenDelta.y) * speed;
            MoveClamped(move);
        }

        private void MoveClamped(Vector3 move)
        {
            var newPos = transform.position + move;
            newPos.x = Mathf.Clamp(newPos.x, panBoundsMin.x, panBoundsMax.x);
            newPos.z = Mathf.Clamp(newPos.z, panBoundsMin.y, panBoundsMax.y);
            transform.position = newPos;
        }

        private void HandlePan(Keyboard keyboard, Mouse mouse, float dt)
        {
            var input = Vector2.zero;
            if (keyboard[Key.W].isPressed || keyboard[Key.UpArrow].isPressed) input.y += 1f;
            if (keyboard[Key.S].isPressed || keyboard[Key.DownArrow].isPressed) input.y -= 1f;
            if (keyboard[Key.D].isPressed || keyboard[Key.RightArrow].isPressed) input.x += 1f;
            if (keyboard[Key.A].isPressed || keyboard[Key.LeftArrow].isPressed) input.x -= 1f;

            if (edgeScrollEnabled && input == Vector2.zero)
            {
                var mousePos = mouse.position.ReadValue();
                if (mousePos.x <= edgeScrollBorder) input.x -= 1f;
                else if (mousePos.x >= Screen.width - edgeScrollBorder) input.x += 1f;
                if (mousePos.y <= edgeScrollBorder) input.y -= 1f;
                else if (mousePos.y >= Screen.height - edgeScrollBorder) input.y += 1f;
            }

            if (input == Vector2.zero) return;

            var forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();
            var right = transform.right;
            right.y = 0f;
            right.Normalize();

            var move = (forward * input.y + right * input.x) * (panSpeed * dt);
            MoveClamped(move);
        }

        private void HandleZoom(Mouse mouse, float dt)
        {
            var scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Approximately(scroll, 0f)) return;
            ApplyZoomDelta(-scroll * zoomSpeed * dt);
        }

        private void ApplyZoomDelta(float distanceDelta)
        {
            if (cameraTransform == null) return;

            var localPos = cameraTransform.localPosition;
            var distance = -localPos.z;
            distance = Mathf.Clamp(distance + distanceDelta, minDistance, maxDistance);
            localPos.z = -distance;
            cameraTransform.localPosition = localPos;
        }

        private void HandleRotate(Keyboard keyboard, Mouse mouse, float dt)
        {
            var rotateInput = 0f;
            if (keyboard[Key.Q].isPressed) rotateInput -= 1f;
            if (keyboard[Key.E].isPressed) rotateInput += 1f;

            if (mouse.middleButton.isPressed)
            {
                rotateInput += mouse.delta.ReadValue().x * 0.1f;
            }

            if (Mathf.Approximately(rotateInput, 0f)) return;
            transform.Rotate(Vector3.up, rotateInput * rotateSpeed * dt, Space.World);
        }
    }
}
