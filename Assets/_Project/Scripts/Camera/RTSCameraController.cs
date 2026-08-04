using UnityEngine;
using UnityEngine.InputSystem;

namespace CityBuilder.CameraControl
{
    public class RTSCameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform pivot;
        [SerializeField] private Transform cameraTransform;

        [Header("Pan")]
        [SerializeField] private float panSpeed = 25f;
        [SerializeField] private bool edgeScrollEnabled = true;
        [SerializeField] private float edgeScrollBorder = 12f;
        [SerializeField] private Vector2 panBoundsMin = new Vector2(-100f, -100f);
        [SerializeField] private Vector2 panBoundsMax = new Vector2(100f, 100f);

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 20f;
        [SerializeField] private float minDistance = 8f;
        [SerializeField] private float maxDistance = 60f;

        [Header("Rotate")]
        [SerializeField] private float rotateSpeed = 90f;

        private void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;

            var dt = Time.unscaledDeltaTime;
            HandlePan(keyboard, mouse, dt);
            HandleZoom(mouse, dt);
            HandleRotate(keyboard, mouse, dt);
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
            var newPos = transform.position + move;
            newPos.x = Mathf.Clamp(newPos.x, panBoundsMin.x, panBoundsMax.x);
            newPos.z = Mathf.Clamp(newPos.z, panBoundsMin.y, panBoundsMax.y);
            transform.position = newPos;
        }

        private void HandleZoom(Mouse mouse, float dt)
        {
            var scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Approximately(scroll, 0f) || cameraTransform == null) return;

            var localPos = cameraTransform.localPosition;
            var distance = -localPos.z;
            distance = Mathf.Clamp(distance - scroll * zoomSpeed * dt, minDistance, maxDistance);
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
