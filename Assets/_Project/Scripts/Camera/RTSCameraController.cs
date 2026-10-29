using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.InputControl;
using CityBuilder.Maps;
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

        /// <summary>A map bigger than the scene was built for (the archipelago) widens where the camera may go.</summary>
        public void SetPanBounds(Vector2 min, Vector2 max)
        {
            panBoundsMin = min;
            panBoundsMax = max;
        }

        [Header("Zoom")]
        /// <summary>
        /// How far the camera may sit from the rig point, in metres. These two numbers decide how
        /// much of the world is on screen, so they are a performance setting as much as a feel one.
        ///
        /// **Why the ceiling is 25 and not the 220 it used to be.** At 50 degrees of pitch and a 60
        /// degree lens, distance d puts a strip of ground roughly 2x d deep in frame: 5 m shows
        /// about 10 m, 15 m about 30 m (the camera STARTS there -- see SetupProject, where the
        /// camera's local z is -15), 25 m about 49 m, and the old 220 showed the entire 200x200 map
        /// at once. That last case was the worst frame in the game and the one nothing could be
        /// culled out of: every one of the ~600 trees, ~140 rock clusters and every building was
        /// inside the frustum, all of it drawn. It also put the grey nothing beyond the map's edge
        /// on screen.
        ///
        /// The player's overview comes from the minimap instead, which frames the whole map and
        /// marks where this camera is looking.
        ///
        /// The floor of 5 m puts the camera about 3,8 m above the ground, which is BELOW the roof of
        /// the taller buildings -- expect to be looking between them rather than down at them there.
        /// </summary>
        [SerializeField] private float zoomSpeed = 40f;
        [SerializeField] private float minDistance = 5f;
        [SerializeField] private float maxDistance = 25f;

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

        [Header("Smoothing")]
        /// <summary>
        /// The rig follows where the gestures put it rather than jumping there (the user,
        /// 2026-10-28: «камера двигается как будто не плавно»): touches arrive unevenly against the
        /// frames, so moving the rig straight to each one stuttered. Gestures move a goal; each
        /// frame the rig closes on it exponentially at this rate per second, ~50 ms behind.
        /// </summary>
        [SerializeField] private float followSharpness = 18f;
        /// <summary>The same for the mouse wheel's zoom, whose notches jumped. A pinch zooms at once: it is already smooth, and the pan that keeps the fingers' point under them needs the real distance.</summary>
        [SerializeField] private float zoomSharpness = 14f;

        private Vector3 _goal;
        private float _goalDistance;
        private bool _hasGoal;
        /// <summary>What the follow last moved the rig to, so a move from outside (JumpTo, a test) is taken as the new goal.</summary>
        private Vector3 _written;
        private float _writtenDistance;

        /// <summary>The drag's speed, averaged over its last steps -- one uneven touch no longer decides the fling.</summary>
        private Vector3 _panVelocity;
        private float _lastStepTime;

        [Header("Focus")]
        /// <summary>How fast FocusOn closes on its target -- a rate, so a long trip is quick and a short one gentle.</summary>
        [SerializeField] private float focusSharpness = 6f;

        private Vector3 _focusTarget;
        private bool _focusing;

        [Header("Over the hills")]
        /// <summary>
        /// The camera keeps this far above the land under it and along its line of sight. The rig
        /// stands on the flat ground plane the grid uses, so over the Caldera's mountains the view
        /// used to pass straight through the rock (the user, 2026-10-20: "камера пролетает сквозь
        /// горы, надо чтобы она их обтекала"). Now the whole rig rises over the relief and settles
        /// back past it, like a camera flown along the hillside rather than through it.
        /// </summary>
        [SerializeField] private float terrainClearance = 2.2f;
        /// <summary>How quickly the rise follows the land: up quickly, so the rock never shows; down gently, so it does not bob.</summary>
        [SerializeField] private float riseSeconds = 0.12f;
        [SerializeField] private float settleSeconds = 0.45f;

        private const int LineOfSightSamples = 6;
        private float _lift;
        private float _liftVelocity;

        /// <summary>
        /// Glides the view to centre on this world point -- an event icon's tap (see
        /// UI.EventFeedController). Any gesture of the player's takes the camera back at once.
        /// The rig's position IS the point the camera looks at: the pivot sits on it and the camera
        /// looks down the pivot's forward axis.
        /// </summary>
        public void FocusOn(Vector3 worldPoint)
        {
            _focusTarget = new Vector3(worldPoint.x, 0f, worldPoint.z);
            _focusing = true;
            _flingVelocity = Vector3.zero;
        }

        /// <summary>Puts the view on this point at once, with no glide -- where a new game opens.</summary>
        public void JumpTo(Vector3 worldPoint)
        {
            transform.position = new Vector3(worldPoint.x, transform.position.y, worldPoint.z);
            _focusing = false;
            _flingVelocity = Vector3.zero;
            TakeGoalFromRig();
        }

        public bool IsFocusing => _focusing;

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
            AdoptOutsideMoves();

            ApplyFocus(dt);
            ApplyFling(dt);

            var keyboard = Keyboard.current;
            if (keyboard != null) HandleKeyboardPan(keyboard, dt);

            var mouse = Mouse.current;
            if (mouse != null) HandleScrollZoom(mouse, dt);

            Follow(dt);
            KeepAboveTheLand(dt);
        }

        // ---------------------------------------------------------------- the follow

        private void TakeGoalFromRig()
        {
            _goal = new Vector3(transform.position.x, 0f, transform.position.z);
            _written = _goal;
            if (cameraTransform != null) _goalDistance = _writtenDistance = -cameraTransform.localPosition.z;
            _hasGoal = true;
        }

        /// <summary>The rig or the camera moved by someone else since the last follow: that is where the goal is now.</summary>
        private void AdoptOutsideMoves()
        {
            if (!_hasGoal)
            {
                TakeGoalFromRig();
                return;
            }
            var at = new Vector3(transform.position.x, 0f, transform.position.z);
            if ((at - _written).sqrMagnitude > 1e-6f) _goal = _written = at;
            if (cameraTransform != null && Mathf.Abs(-cameraTransform.localPosition.z - _writtenDistance) > 1e-4f)
            {
                _goalDistance = _writtenDistance = -cameraTransform.localPosition.z;
            }
        }

        /// <summary>The rig and the zoom closing on their goals.</summary>
        private void Follow(float dt)
        {
            var pos = transform.position;
            var flat = Vector3.Lerp(new Vector3(pos.x, 0f, pos.z), _goal, 1f - Mathf.Exp(-followSharpness * dt));
            if ((flat - _goal).sqrMagnitude < 1e-6f) flat = _goal;
            transform.position = new Vector3(flat.x, pos.y, flat.z);
            _written = flat;

            if (cameraTransform == null) return;
            var local = cameraTransform.localPosition;
            var distance = Mathf.Lerp(-local.z, _goalDistance, 1f - Mathf.Exp(-zoomSharpness * dt));
            if (Mathf.Abs(distance - _goalDistance) < 1e-3f) distance = _goalDistance;
            local.z = -distance;
            cameraTransform.localPosition = local;
            _writtenDistance = distance;
        }

        // ---------------------------------------------------------------- over the hills

        /// <summary>
        /// How high the rig must stand for the camera, and every point of its line of sight down to
        /// the rig, to clear the land by terrainClearance -- the most any one of them needs. The
        /// points are on the segment from the camera to the rig, taken as if the rig stood on the
        /// ground plane; each needs (land height + clearance) minus its own height.
        /// </summary>
        private float LiftNeeded()
        {
            if (cameraTransform == null) return 0f;

            var groundY = GridManager.Instance != null ? GridManager.Instance.GroundHeight : 0f;
            var rig = new Vector3(transform.position.x, groundY, transform.position.z);
            var offset = cameraTransform.position - transform.position;
            var cam = rig + offset;
            var mask = 1 << TerrainPhysicsLayer.Layer;

            var need = 0f;
            for (var i = 0; i <= LineOfSightSamples; i++)
            {
                var t = (float)i / LineOfSightSamples;
                var p = Vector3.Lerp(cam, rig, t);
                if (!Physics.Raycast(new Vector3(p.x, p.y + 300f, p.z), Vector3.down, out var hit, 600f, mask, QueryTriggerInteraction.Ignore)) continue;
                // the clearance tapers toward the rig: the camera needs room, the point it looks at only to be above the rock
                var clearance = Mathf.Lerp(terrainClearance, 0f, t);
                need = Mathf.Max(need, hit.point.y + clearance - p.y);
            }
            return need;
        }

        private void KeepAboveTheLand(float dt)
        {
            var target = Mathf.Max(0f, LiftNeeded());
            _lift = Mathf.SmoothDamp(_lift, target, ref _liftVelocity, target > _lift ? riseSeconds : settleSeconds, Mathf.Infinity, dt);
            // never inside the rock, even for the frame the smoothing lags
            if (_lift < target - terrainClearance * 0.5f) _lift = target - terrainClearance * 0.5f;

            var groundY = GridManager.Instance != null ? GridManager.Instance.GroundHeight : 0f;
            var pos = transform.position;
            pos.y = groundY + _lift;
            transform.position = pos;
        }

        // ---------------------------------------------------------------- gestures

        private void HandleDragStarted(Vector2 screenPosition)
        {
            if (!OwnsSingleDrag()) return;

            _focusing = false;
            _flingVelocity = Vector3.zero;
            _panVelocity = Vector3.zero;
            _lastStepTime = Time.unscaledTime;
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

            // a finger held still before it lifted throws nothing
            if (Time.unscaledTime - _lastStepTime > 0.1f) return;
            if (_panVelocity.magnitude >= minFlingSpeed) _flingVelocity = _panVelocity;
        }

        private void HandlePinchStarted(Vector2 centroid, float distance)
        {
            _focusing = false;
            _flingVelocity = Vector3.zero;
            _panVelocity = Vector3.zero;
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
            // the goal: where the rig must stand, measured from where it stands now, so it is exact
            // however far the follow lags behind
            var before = _goal;
            _goal = Clamped(new Vector3(transform.position.x, 0f, transform.position.z) + step);

            var now = Time.unscaledTime;
            var since = Mathf.Max(now - _lastStepTime, 1f / 120f);
            _lastStepTime = now;
            _panVelocity = Vector3.Lerp(_panVelocity, (_goal - before) / since, 0.4f);
        }

        private void ApplyFocus(float dt)
        {
            if (!_focusing) return;

            var offset = _focusTarget - _goal;
            offset.y = 0f;
            if (offset.sqrMagnitude < 0.01f)
            {
                MoveClamped(offset);
                _focusing = false;
                return;
            }

            var before = _goal;
            MoveClamped(offset * (1f - Mathf.Exp(-focusSharpness * dt)));

            // A target beyond the pan bounds is never reached: stop once the clamp holds the goal still.
            if ((_goal - before).sqrMagnitude < 1e-6f) _focusing = false;
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

        /// <summary>Moves the goal, which the rig follows: a fling, a focus, the keys.</summary>
        private void MoveClamped(Vector3 move)
        {
            move.y = 0f;
            _goal = Clamped(_goal + move);
        }

        private Vector3 Clamped(Vector3 at) =>
            new Vector3(Mathf.Clamp(at.x, panBoundsMin.x, panBoundsMax.x), 0f, Mathf.Clamp(at.z, panBoundsMin.y, panBoundsMax.y));

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
            _focusing = false;

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

        /// <summary>The wheel's zoom: moves the goal, the follow glides there.</summary>
        private void ApplyZoomDelta(float distanceDelta)
        {
            if (cameraTransform == null) return;
            _goalDistance = Mathf.Clamp(_goalDistance + distanceDelta, minDistance, maxDistance);
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
            // at once, goal and all (see zoomSharpness)
            _goalDistance = _writtenDistance = distance;
        }
    }
}
