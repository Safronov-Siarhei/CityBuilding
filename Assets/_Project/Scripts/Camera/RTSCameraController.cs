using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.InputControl;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CityBuilder.CameraControl
{
    /// <summary>
    /// The camera, driven entirely by gestures that <see cref="TouchInputRouter"/> has already
    /// recognised -- this class never reads a pointer itself.
    ///
    /// Panning is WORLD-ANCHORED: the ground point under the finger when the drag began stays
    /// under the finger for the whole drag. That replaces a <c>touchPanSpeed</c> constant that
    /// multiplied a pixel delta, which was wrong twice over -- the same swipe moved the town by
    /// different amounts on screens of different density, and by the same number of METRES whether
    /// the camera was 8 m up or 220 m up, so the map crawled when zoomed out and shot away when
    /// zoomed in. Anchoring to the world needs no constant at all and is correct at every zoom by
    /// construction.
    ///
    /// Edge panning is gone. It existed because a finger carrying a building could not also pan,
    /// so the camera had to be driven from a band at the screen border; placement now keeps the
    /// ghost at a fixed aim point and lets the player drag the world underneath it (see
    /// BuildingPlacer), which is both more precise and one fewer thing to explain.
    /// </summary>
    public class RTSCameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform pivot;
        [SerializeField] private Transform cameraTransform;

        /// <summary>Needed for the screen-to-ground ray every gesture is resolved through.</summary>
        [SerializeField] private Camera targetCamera;

        [Header("Pan (PC)")]
        // Keyboard only, deliberately: edge scrolling (camera pans when the cursor nears a screen
        // border) used to be here and was removed -- it hijacked the camera during ordinary
        // pointing, e.g. reaching for the hotbar or a building near the edge. It also has no
        // meaning on the touch devices this game primarily targets, where panning is a finger drag.
        [SerializeField] private float panSpeed = 50f;
        [SerializeField] private Vector2 panBoundsMin = new Vector2(-100f, -100f);
        [SerializeField] private Vector2 panBoundsMax = new Vector2(100f, 100f);

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 40f;
        [SerializeField] private float minDistance = 8f;
        [SerializeField] private float maxDistance = 220f;

        [Header("Inertia")]
        /// <summary>How fast a fling dies out: velocity is multiplied by exp(-damping * dt) every frame.</summary>
        [SerializeField] private float flingDamping = 6f;

        /// <summary>Below this world speed the glide stops rather than creeping for another second.</summary>
        [SerializeField] private float minFlingSpeed = 0.5f;

        /// <summary>
        /// Turned off by placement and by road drawing. A camera that keeps gliding after the
        /// finger lifts is pleasant while looking around and actively harmful while aiming: the
        /// aim point would drift off the cell the player just lined up.
        /// </summary>
        public bool InertiaEnabled { get; set; } = true;

        /// <summary>The world point held under the finger (or under the pinch centroid) for the duration of the gesture.</summary>
        private Vector3 _grabPoint;
        private bool _hasGrab;

        private Vector3 _flingVelocity;
        private Vector3 _lastPanStep;

        private void OnEnable()
        {
            var router = TouchInputRouter.Instance;
            if (router == null) return;

            router.DragStarted += HandleDragStarted;
            router.DragMoved += HandleDragMoved;
            router.DragEnded += HandleDragEnded;
            router.PinchStarted += HandlePinchStarted;
            router.PinchMoved += HandlePinchMoved;
            router.PinchEnded += HandlePinchEnded;
        }

        private void OnDisable()
        {
            var router = TouchInputRouter.Instance;
            if (router == null) return;

            router.DragStarted -= HandleDragStarted;
            router.DragMoved -= HandleDragMoved;
            router.DragEnded -= HandleDragEnded;
            router.PinchStarted -= HandlePinchStarted;
            router.PinchMoved -= HandlePinchMoved;
            router.PinchEnded -= HandlePinchEnded;
        }

        /// <summary>
        /// Subscribing in Start as well as OnEnable, because the router may not have existed yet
        /// when this component was enabled -- both live on scene objects with no guaranteed order.
        /// The handlers are removed first so a double subscription is impossible.
        /// </summary>
        private void Start()
        {
            OnDisable();
            OnEnable();
        }

        private void Update()
        {
            if (ModalGate.IsBlocked) return;

            var dt = Time.unscaledDeltaTime;

            ApplyFling(dt);

            var keyboard = Keyboard.current;
            if (keyboard != null) HandleKeyboardPan(keyboard, dt);

            var mouse = Mouse.current;
            if (mouse != null) HandleScrollZoom(mouse, dt);
        }

        // ---------------------------------------------------------------- gestures

        private void HandleDragStarted(Vector2 screenPosition)
        {
            if (!OwnsSingleDrag()) return;

            _flingVelocity = Vector3.zero;
            _lastPanStep = Vector3.zero;
            _hasGrab = TryGroundPoint(screenPosition, out _grabPoint);
        }

        private void HandleDragMoved(Vector2 screenPosition)
        {
            if (!OwnsSingleDrag() || !_hasGrab) return;
            PanToKeepGrabUnder(screenPosition);
        }

        private void HandleDragEnded(bool completed)
        {
            _hasGrab = false;

            // A drag cancelled by a second finger must not fling: the player is starting a pinch,
            // not throwing the map.
            if (!InertiaEnabled || !completed) return;

            var dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            var velocity = _lastPanStep / dt;
            if (velocity.magnitude >= minFlingSpeed) _flingVelocity = velocity;
        }

        private void HandlePinchStarted(Vector2 centroid, float distance)
        {
            _flingVelocity = Vector3.zero;
            _lastPanStep = Vector3.zero;
            _hasGrab = TryGroundPoint(centroid, out _grabPoint);
        }

        /// <summary>
        /// Zoom first, then pan the grabbed point back under the centroid. Doing it in that order
        /// is what makes the zoom happen AT THE FINGERS rather than at the middle of the screen:
        /// the zoom moves the world under the centroid, and the pan puts it back.
        /// </summary>
        private void HandlePinchMoved(Vector2 centroid, float distance, float previousDistance)
        {
            if (previousDistance > 0.01f && distance > 0.01f)
            {
                // Fingers spreading (distance up) must bring the camera closer, hence previous/current.
                ApplyZoomFactor(previousDistance / distance);
            }

            if (_hasGrab) PanToKeepGrabUnder(centroid);
        }

        private void HandlePinchEnded()
        {
            _hasGrab = false;
        }

        /// <summary>A one-finger drag belongs to the camera unless the drawing mode has taken it (see DragOwner).</summary>
        private static bool OwnsSingleDrag()
        {
            var router = TouchInputRouter.Instance;
            return router == null || router.SingleDragOwner == DragOwner.Camera;
        }

        // ---------------------------------------------------------------- movement

        /// <summary>
        /// Moves the rig so the world point grabbed at the start of the gesture sits under this
        /// screen position again. Both points are measured with the camera where it is NOW, so the
        /// correction is exact for a single frame and self-correcting across frames.
        /// </summary>
        private void PanToKeepGrabUnder(Vector2 screenPosition)
        {
            if (!TryGroundPoint(screenPosition, out var current)) return;

            var step = _grabPoint - current;
            step.y = 0f;
            MoveClamped(step);
            _lastPanStep = step;
        }

        private void ApplyFling(float dt)
        {
            if (_flingVelocity.sqrMagnitude <= 0f) return;

            if (!InertiaEnabled || _flingVelocity.magnitude < minFlingSpeed)
            {
                _flingVelocity = Vector3.zero;
                return;
            }

            MoveClamped(_flingVelocity * dt);
            _flingVelocity *= Mathf.Exp(-flingDamping * dt);
        }

        private void MoveClamped(Vector3 move)
        {
            var newPos = transform.position + move;
            newPos.x = Mathf.Clamp(newPos.x, panBoundsMin.x, panBoundsMax.x);
            newPos.z = Mathf.Clamp(newPos.z, panBoundsMin.y, panBoundsMax.y);
            transform.position = newPos;
        }

        /// <summary>
        /// Where this screen ray meets the ground PLANE -- not the ground mesh. A mesh raycast
        /// would make the anchor jump every time the finger crossed a hillside, and panning would
        /// jitter with the terrain; the plane gives a stable frame of reference at exactly the
        /// height the grid itself uses.
        /// </summary>
        private bool TryGroundPoint(Vector2 screenPosition, out Vector3 point)
        {
            point = default;
            if (targetCamera == null) return false;

            var groundY = GridManager.Instance != null ? GridManager.Instance.GroundHeight : 0f;
            var plane = new Plane(Vector3.up, new Vector3(0f, groundY, 0f));
            var ray = targetCamera.ScreenPointToRay(screenPosition);

            if (!plane.Raycast(ray, out var enter)) return false;

            point = ray.GetPoint(enter);
            return true;
        }

        // ---------------------------------------------------------------- desktop

        private void HandleKeyboardPan(Keyboard keyboard, float dt)
        {
            var input = Vector2.zero;
            if (keyboard[Key.W].isPressed || keyboard[Key.UpArrow].isPressed) input.y += 1f;
            if (keyboard[Key.S].isPressed || keyboard[Key.DownArrow].isPressed) input.y -= 1f;
            if (keyboard[Key.D].isPressed || keyboard[Key.RightArrow].isPressed) input.x += 1f;
            if (keyboard[Key.A].isPressed || keyboard[Key.LeftArrow].isPressed) input.x -= 1f;

            if (input == Vector2.zero) return;

            _flingVelocity = Vector3.zero;

            var forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();
            var right = transform.right;
            right.y = 0f;
            right.Normalize();

            var move = (forward * input.y + right * input.x) * (panSpeed * dt);
            MoveClamped(move);
        }

        private void HandleScrollZoom(Mouse mouse, float dt)
        {
            var scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Approximately(scroll, 0f)) return;
            ApplyZoomDelta(-scroll * zoomSpeed * dt);
        }

        private void ApplyZoomDelta(float distanceDelta)
        {
            if (cameraTransform == null) return;

            var localPos = cameraTransform.localPosition;
            var distance = Mathf.Clamp(-localPos.z + distanceDelta, minDistance, maxDistance);
            localPos.z = -distance;
            cameraTransform.localPosition = localPos;
        }

        /// <summary>
        /// Multiplicative zoom, which is what a pinch actually expresses: the fingers report a
        /// RATIO, and a ratio applied to the distance feels identical whether the camera is close
        /// or far. The old pixel-linear step took forever to cross the range from 220 m and was
        /// twitchy at 8 m.
        /// </summary>
        private void ApplyZoomFactor(float factor)
        {
            if (cameraTransform == null || factor <= 0f) return;

            var localPos = cameraTransform.localPosition;
            var distance = Mathf.Clamp(-localPos.z * factor, minDistance, maxDistance);
            localPos.z = -distance;
            cameraTransform.localPosition = localPos;
        }
    }
}
