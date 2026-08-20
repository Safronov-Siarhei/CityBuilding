using System;
using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CityBuilder.InputControl
{
    /// <summary>Who receives a one-finger drag. Everything else in the game leaves this alone; only the road/fence drawing mode takes it (see BuildingPlacer), and hands it straight back.</summary>
    public enum DragOwner
    {
        Camera,
        World,
    }

    /// <summary>
    /// The single place a raw pointer is turned into a gesture, and the reason the rest of the
    /// game no longer reads <see cref="Pointer.current"/> at all.
    ///
    /// Before this existed, four systems (BuildingPlacer, BuildingSelector, CitizenSelector,
    /// ArmyOrderInput) each polled the pointer themselves and each acted on
    /// <c>press.wasPressedThisFrame</c> -- the frame the finger LANDS. On a phone that is not a
    /// tap, it is the first frame of whatever the player is about to do: dragging the camera over
    /// a rooftop opened that building's card, and dragging it with a citizen selected sent them
    /// walking. There was no slop threshold and no notion of releasing, so panning across a built
    /// -up town was effectively impossible.
    ///
    /// The rules this class enforces, in order of how much trouble they save:
    ///
    /// 1. **A tap fires on RELEASE**, and only if the finger stayed inside <see cref="TapSlopPixels"/>
    ///    for less than <see cref="tapMaxDuration"/>. Anything longer or wider is not a tap.
    /// 2. **Crossing the slop kills the tap permanently** for that touch. There is no path back
    ///    from Drag to Tap: a finger that has moved is panning, full stop.
    /// 3. **A second finger cancels whatever the first was doing** -- pending tap, long press,
    ///    even a drag already in progress -- and the gesture becomes a pinch.
    /// 4. **Lifting one finger out of a pinch does not resume a drag.** The remaining finger is
    ///    ignored until the screen is clear (Phase.Dead). Without this the camera lurches every
    ///    time a pinch ends, because the surviving touch reports a huge delta on that frame.
    /// 5. **A gesture that starts on UI never reaches the world**, for its whole life, even if the
    ///    finger later slides off the button.
    ///
    /// The slop is measured in PHYSICAL units (dp), not pixels: the same 8 px is a third of a
    /// millimetre on one phone and two millimetres on another, which would make the tap/drag split
    /// feel different on every device.
    /// </summary>
    public class TouchInputRouter : MonoBehaviour
    {
        public static TouchInputRouter Instance { get; private set; }

        [Header("Thresholds")]
        /// <summary>Android's own touch slop. In dp so the threshold is the same distance on every screen.</summary>
        [SerializeField] private float tapSlopDp = 8f;

        [SerializeField] private float tapMaxDuration = 0.35f;
        [SerializeField] private float longPressDelay = 0.5f;

        /// <summary>Fired on RELEASE for a touch that never left the slop circle. Screen position of the finger.</summary>
        public event Action<Vector2> Tapped;

        /// <summary>Fired ON THE TIMER, while the finger is still down -- a long press the player has to wait out is one they can feel happening.</summary>
        public event Action<Vector2> LongPressed;

        public event Action<Vector2> DragStarted;
        public event Action<Vector2> DragMoved;
        /// <summary>True when the finger simply lifted, false when the drag was CANCELLED by a second finger (or a modal). A line being drawn must be thrown away in the second case, not committed.</summary>
        public event Action<bool> DragEnded;

        /// <summary>Centroid of the two fingers, and the distance between them.</summary>
        public event Action<Vector2, float> PinchStarted;

        /// <summary>Centroid, current distance, previous distance. The ratio of the last two is the zoom step.</summary>
        public event Action<Vector2, float, float> PinchMoved;

        public event Action PinchEnded;

        /// <summary>
        /// Whether a one-finger drag goes to the camera (always, except while drawing roads or
        /// fences) or to whoever claimed it. Deliberately a single flag set in one place rather
        /// than every listener second-guessing the game state, which is the pattern this class
        /// exists to end.
        /// </summary>
        public DragOwner SingleDragOwner { get; set; } = DragOwner.Camera;

        /// <summary>
        /// True when the gesture in progress (or the last completed one) came from a touchscreen.
        /// Placement reads this to decide between the phone's aim-and-confirm flow and the mouse's
        /// hover-and-click one -- a mouse HAS a hover position before it clicks, and a finger does
        /// not, which is the whole reason the two need different flows.
        /// </summary>
        public bool LastGestureWasTouch { get; private set; }

        private enum Phase
        {
            Idle,
            Pending,
            Drag,
            Pinch,
            /// <summary>Gesture disqualified (started on UI, already fired its long press, or is the tail of a pinch). Ignored until every finger is up.</summary>
            Dead,
        }

        private Phase _phase = Phase.Idle;
        private Vector2 _startPosition;
        private float _startTime;
        private float _lastPinchDistance;

        // Reused so a UI hit test costs no allocation. The test runs on touch-down only, never
        // per frame.
        private static readonly List<RaycastResult> UiResults = new List<RaycastResult>();
        private PointerEventData _uiPointerData;

        /// <summary>The slop in screen pixels for THIS device. 160 dpi is the density Android calls 1x, and the fallback when a platform reports no dpi at all.</summary>
        public float TapSlopPixels
        {
            get
            {
                var dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
                return tapSlopDp * Mathf.Max(1f, dpi / 160f);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (ModalGate.IsBlocked)
            {
                // A dialog opening mid-gesture must not leave a drag running underneath it.
                AbortGesture();
                return;
            }

            var count = ReadPointers(out var positionA, out var positionB, out var fromTouch);

            switch (_phase)
            {
                case Phase.Idle:
                    if (count >= 2) BeginPinch(positionA, positionB, fromTouch);
                    else if (count == 1) BeginPending(positionA, fromTouch);
                    break;

                case Phase.Pending:
                    if (count >= 2)
                    {
                        // Rule 3: the first finger's pending tap never happened.
                        BeginPinch(positionA, positionB, fromTouch);
                    }
                    else if (count == 0)
                    {
                        if (Time.unscaledTime - _startTime <= tapMaxDuration) Tapped?.Invoke(_startPosition);
                        _phase = Phase.Idle;
                    }
                    else if ((positionA - _startPosition).sqrMagnitude > TapSlopPixels * TapSlopPixels)
                    {
                        // Rule 2: no way back to a tap from here.
                        _phase = Phase.Drag;
                        DragStarted?.Invoke(positionA);
                    }
                    else if (Time.unscaledTime - _startTime >= longPressDelay)
                    {
                        LongPressed?.Invoke(_startPosition);
                        _phase = Phase.Dead;
                    }
                    break;

                case Phase.Drag:
                    if (count >= 2)
                    {
                        DragEnded?.Invoke(false);
                        BeginPinch(positionA, positionB, fromTouch);
                    }
                    else if (count == 0)
                    {
                        DragEnded?.Invoke(true);
                        _phase = Phase.Idle;
                    }
                    else
                    {
                        DragMoved?.Invoke(positionA);
                    }
                    break;

                case Phase.Pinch:
                    if (count >= 2)
                    {
                        var distance = Vector2.Distance(positionA, positionB);
                        PinchMoved?.Invoke((positionA + positionB) * 0.5f, distance, _lastPinchDistance);
                        _lastPinchDistance = distance;
                    }
                    else
                    {
                        // Rule 4: whether one finger remains or none, the pinch is over and the
                        // survivor is not promoted to a drag.
                        PinchEnded?.Invoke();
                        _phase = count == 0 ? Phase.Idle : Phase.Dead;
                    }
                    break;

                case Phase.Dead:
                    if (count == 0) _phase = Phase.Idle;
                    break;
            }
        }

        private void BeginPending(Vector2 position, bool fromTouch)
        {
            LastGestureWasTouch = fromTouch;

            // Rule 5, decided once here rather than re-tested on every later frame of the gesture.
            if (IsOverUI(position))
            {
                _phase = Phase.Dead;
                return;
            }

            _phase = Phase.Pending;
            _startPosition = position;
            _startTime = Time.unscaledTime;
        }

        private void BeginPinch(Vector2 positionA, Vector2 positionB, bool fromTouch)
        {
            LastGestureWasTouch = fromTouch;
            _phase = Phase.Pinch;
            _lastPinchDistance = Vector2.Distance(positionA, positionB);
            PinchStarted?.Invoke((positionA + positionB) * 0.5f, _lastPinchDistance);
        }

        /// <summary>Ends whatever is running without producing a tap, and refuses to start anything until the screen is clear. Used when a modal takes over mid-gesture.</summary>
        private void AbortGesture()
        {
            if (_phase == Phase.Drag) DragEnded?.Invoke(false);
            else if (_phase == Phase.Pinch) PinchEnded?.Invoke();

            _phase = Phase.Dead;
        }

        /// <summary>
        /// How many pointers are down, and where. Touch wins over the mouse when both exist, so a
        /// laptop's trackpad cannot fight a touchscreen. Only the first two touches matter -- a
        /// third finger changes nothing, which is what the schema calls for.
        /// </summary>
        private static int ReadPointers(out Vector2 positionA, out Vector2 positionB, out bool fromTouch)
        {
            positionA = default;
            positionB = default;
            fromTouch = false;

            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var touches = touchscreen.touches;
                var active = 0;
                for (var i = 0; i < touches.Count; i++)
                {
                    if (!touches[i].press.isPressed) continue;

                    if (active == 0) positionA = touches[i].position.ReadValue();
                    else if (active == 1) positionB = touches[i].position.ReadValue();
                    active++;
                }

                if (active > 0)
                {
                    fromTouch = true;
                    return active;
                }
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                positionA = mouse.position.ReadValue();
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// Whether UI sits under this screen point, asked of the EventSystem's own raycasters
        /// rather than of <c>IsPointerOverGameObject(pointerId)</c>. That overload needs the id the
        /// UI module assigned, which is not the same number as a Touchscreen touchId -- the old
        /// code passed the latter, so a tap on a button could also reach the world underneath it.
        /// </summary>
        private bool IsOverUI(Vector2 screenPosition)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return false;

            if (_uiPointerData == null) _uiPointerData = new PointerEventData(eventSystem);
            _uiPointerData.Reset();
            _uiPointerData.position = screenPosition;

            UiResults.Clear();
            eventSystem.RaycastAll(_uiPointerData, UiResults);
            return UiResults.Count > 0;
        }
    }
}
