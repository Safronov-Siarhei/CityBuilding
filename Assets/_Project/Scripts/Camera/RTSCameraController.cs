using CityBuilder.Buildings;
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
        // Keyboard only, deliberately: edge scrolling (camera pans when the cursor nears a screen
        // border) used to be here and was removed -- it hijacked the camera during ordinary
        // pointing, e.g. reaching for the hotbar or a building near the edge. It also has no
        // meaning on the touch devices this game primarily targets, where panning is a finger drag.
        [SerializeField] private float panSpeed = 50f;
        [SerializeField] private Vector2 panBoundsMin = new Vector2(-100f, -100f);
        [SerializeField] private Vector2 panBoundsMax = new Vector2(100f, 100f);

        [Header("Zoom (PC)")]
        [SerializeField] private float zoomSpeed = 40f;
        [SerializeField] private float minDistance = 8f;
        [SerializeField] private float maxDistance = 220f;

        [Header("Touch")]
        [SerializeField] private float touchPanSpeed = 0.06f;
        [SerializeField] private float touchZoomSpeed = 0.05f;

        [Header("Placement")]
        // While a building is on the player's finger, that finger belongs to the BUILDING. A drag
        // used to move both at once -- the ghost followed the touch and the camera panned under it,
        // so the building appeared to slide across the map at double speed and could never be put
        // where it was aimed. Panning during placement now happens only from the screen EDGE, which
        // is how the player carries a building somewhere off-screen.
        [SerializeField] private BuildingPlacer buildingPlacer;

        /// <summary>Width of the edge band, as a fraction of the screen's shorter side -- a fraction rather than pixels because the same band has to feel the same on a 1080p phone and a 1440p one.</summary>
        [SerializeField] private float edgePanZoneFraction = 0.08f;

        /// <summary>Screen pixels per second at the very edge, fed through touchPanSpeed like any other drag. Ramps up from zero at the band's inner boundary, so grazing the edge does not fling the camera.</summary>
        [SerializeField] private float edgePanPixelsPerSecond = 320f;

        private float _lastPinchDistance;

        private void Update()
        {
            if (ModalGate.IsBlocked) return;

            var dt = Time.unscaledDeltaTime;

            var handledByTouch = HandleTouch(dt);
            if (handledByTouch) return;

            // Panning and zooming are checked independently now that panning no longer reads the
            // mouse -- previously both were skipped entirely if no mouse was present, so keyboard
            // panning silently did nothing on a machine without one.
            var keyboard = Keyboard.current;
            if (keyboard != null) HandlePan(keyboard, dt);

            var mouse = Mouse.current;
            if (mouse != null) HandleZoom(mouse, dt);
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

                // Two fingers still pinch-zoom while placing: that gesture cannot be confused with
                // dragging a building, and losing zoom mid-placement would be its own annoyance.
                if (buildingPlacer != null && buildingPlacer.IsSelecting)
                {
                    EdgePan(posA, dt);
                    return true;
                }

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

        /// <summary>
        /// Pans while the finger sits in the band along a screen edge, at a speed that ramps from
        /// zero at the band's inner boundary to full at the very edge. Pure geometry, exposed for
        /// a test: EdgePush is what decides whether a touch pans at all and how hard.
        /// </summary>
        private void EdgePan(Vector2 touchPosition, float dt)
        {
            var push = EdgePush(touchPosition, Screen.width, Screen.height, edgePanZoneFraction);
            if (push == Vector2.zero) return;

            // Negated because PanByScreenDelta takes a FINGER delta and moves the world with it:
            // a finger at the right edge means "show me what is further right", i.e. the camera
            // travels right, which is the opposite of dragging the world rightwards.
            PanByScreenDelta(-push * (edgePanPixelsPerSecond * dt), touchPanSpeed);
        }

        /// <summary>
        /// How hard a touch at this screen position pushes the camera, per axis, in -1..1. Zero
        /// anywhere outside the edge band, so a player dragging a building around the middle of the
        /// screen never moves the camera by accident.
        /// </summary>
        public static Vector2 EdgePush(Vector2 touchPosition, float screenWidth, float screenHeight, float zoneFraction)
        {
            var zone = Mathf.Min(screenWidth, screenHeight) * Mathf.Max(0.0001f, zoneFraction);
            var push = Vector2.zero;

            if (touchPosition.x < zone) push.x = -(zone - touchPosition.x) / zone;
            else if (touchPosition.x > screenWidth - zone) push.x = (touchPosition.x - (screenWidth - zone)) / zone;

            if (touchPosition.y < zone) push.y = -(zone - touchPosition.y) / zone;
            else if (touchPosition.y > screenHeight - zone) push.y = (touchPosition.y - (screenHeight - zone)) / zone;

            return new Vector2(Mathf.Clamp(push.x, -1f, 1f), Mathf.Clamp(push.y, -1f, 1f));
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

        private void HandlePan(Keyboard keyboard, float dt)
        {
            var input = Vector2.zero;
            if (keyboard[Key.W].isPressed || keyboard[Key.UpArrow].isPressed) input.y += 1f;
            if (keyboard[Key.S].isPressed || keyboard[Key.DownArrow].isPressed) input.y -= 1f;
            if (keyboard[Key.D].isPressed || keyboard[Key.RightArrow].isPressed) input.x += 1f;
            if (keyboard[Key.A].isPressed || keyboard[Key.LeftArrow].isPressed) input.x -= 1f;

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

    }
}
