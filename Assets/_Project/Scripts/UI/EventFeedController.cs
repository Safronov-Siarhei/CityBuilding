using System.Collections.Generic;
using CityBuilder.CameraControl;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// The event feed: what happens to the settlement, as a column of round icons at the screen's
    /// edge instead of a panel of text. Each icon is a picture (a building's own icon, a resource)
    /// with a small glyph in its corner saying what happened, ringed in the event's colour; the
    /// ring drains as its time runs out and the icon fades away. Tapping one sends the camera to
    /// where it happened -- the building, the place a soldier fell, the orcs of a raid -- and
    /// leaves the icon up: it goes only when its time runs out (the user's rule, 2026-10-14), so
    /// the player can tap it again to go back there.
    ///
    /// The bookkeeping is EventFeedModel's; this class only draws it. The icons are built ahead of
    /// time (one per slot, see SetupProject.BuildEventFeed) and live on a canvas of their own, so a
    /// draining ring re-meshes five small images and not the whole HUD every frame.
    /// </summary>
    public class EventFeedController : MonoBehaviour
    {
        [SerializeField] private EventIconView[] views = new EventIconView[0];
        [SerializeField] private EventIconLibrary library;
        [SerializeField] private RTSCameraController cameraRig;

        /// <summary>Distance from one icon's centre to the next one down.</summary>
        [SerializeField] private float spacing = 100f;

        [Header("Seconds an icon stays up")]
        [SerializeField] private float goodSeconds = 6f;
        [SerializeField] private float warningSeconds = 9f;
        [SerializeField] private float dangerSeconds = 14f;

        private const float PopSeconds = 0.25f;
        private const float FadeSeconds = 0.5f;

        /// <summary>How fast an icon slides into its new row when the column reshuffles -- a rate, not a duration.</summary>
        private const float SlideSharpness = 14f;

        private EventFeedModel _model;

        /// <summary>Icons hang from the feed's top edge by their centres: the first row sits half an icon down.</summary>
        private float _firstRowOffset;
        private readonly List<EventFeedSlot> _expired = new List<EventFeedSlot>();
        private bool _subscribed;

        public EventFeedModel Model => _model;

        private void Awake()
        {
            var lifetimes = new float[4];
            lifetimes[(int)EventSeverity.Good] = goodSeconds;
            lifetimes[(int)EventSeverity.Warning] = warningSeconds;
            lifetimes[(int)EventSeverity.Danger] = dangerSeconds;
            _model = new EventFeedModel(views.Length, lifetimes);
            _firstRowOffset = views.Length > 0 ? views[0].Rect.rect.height * 0.5f : 0f;

            foreach (var view in views)
            {
                view.Clicked += HandleClicked;
                view.Hide();
            }
        }

        private void OnEnable() => Subscribe();

        // Again in Start: EventLogManager may wake after this does -- no guaranteed order between scene objects.
        private void Start() => Subscribe();

        private void OnDisable()
        {
            if (_subscribed && EventLogManager.Instance != null) EventLogManager.Instance.OnEventPosted -= HandleEventPosted;
            _subscribed = false;
        }

        private void Subscribe()
        {
            if (_subscribed || EventLogManager.Instance == null) return;
            EventLogManager.Instance.OnEventPosted += HandleEventPosted;
            _subscribed = true;
        }

        private void HandleEventPosted(GameEvent gameEvent)
        {
            var slot = _model.Add(gameEvent, out var evictedView);
            if (evictedView >= 0 && evictedView < views.Length) views[evictedView].Hide();
            if (slot == null) return;

            if (slot.View < 0)
            {
                slot.View = FreeView();
                if (slot.View < 0) return;

                // A new icon appears at its own row rather than sliding in from wherever its view was last.
                views[slot.View].Rect.anchoredPosition = RowPosition(0);
            }

            var view = views[slot.View];
            var glyph = library != null ? library.GetGlyph(gameEvent.Glyph) : null;
            var picture = library != null ? library.GetPicture(gameEvent.Picture) : null;
            view.Show(picture, glyph, Colour(gameEvent.Severity), gameEvent.Text, slot.Count);
            view.transform.SetAsLastSibling();
        }

        private void Update()
        {
            var slots = _model.Slots;
            if (slots.Count == 0) return;

            var dt = Time.unscaledDeltaTime;
            _expired.Clear();
            if (_model.Tick(dt, _expired) > 0)
            {
                foreach (var slot in _expired) ReleaseView(slot);
            }

            var slide = 1f - Mathf.Exp(-SlideSharpness * dt);
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot.View < 0) continue;

                var view = views[slot.View];
                view.Rect.anchoredPosition = Vector2.Lerp(view.Rect.anchoredPosition, RowPosition(i), slide);
                view.SetCount(slot.Count);
                view.SetProgress(
                    slot.Duration > 0f ? Mathf.Clamp01(slot.Remaining / slot.Duration) : 0f,
                    Mathf.Clamp01(slot.Remaining / FadeSeconds),
                    PopScale(slot.Age));
            }
        }

        private void HandleClicked(EventIconView view)
        {
            EventFeedSlot clicked = null;
            foreach (var slot in _model.Slots)
            {
                if (slot.View >= 0 && views[slot.View] == view) clicked = slot;
            }
            if (clicked == null) return;

            if (cameraRig != null && clicked.Event.TryGetTarget(out var target)) cameraRig.FocusOn(target);
        }

        private void ReleaseView(EventFeedSlot slot)
        {
            if (slot.View >= 0 && slot.View < views.Length) views[slot.View].Hide();
            slot.View = -1;
        }

        private int FreeView()
        {
            for (var i = 0; i < views.Length; i++)
            {
                if (!views[i].gameObject.activeSelf) return i;
            }
            return -1;
        }

        private Vector2 RowPosition(int row) => new Vector2(0f, -_firstRowOffset - row * spacing);

        /// <summary>A quick overshoot -- up to 1.15 and back to 1 -- so a new icon, or one that just counted up, catches the eye.</summary>
        private static float PopScale(float age)
        {
            if (age >= PopSeconds) return 1f;
            var t = age / PopSeconds;
            return t < 0.6f ? Mathf.Lerp(0.3f, 1.15f, t / 0.6f) : Mathf.Lerp(1.15f, 1f, (t - 0.6f) / 0.4f);
        }

        // A shade brighter than UiTheme's text colours of the same names: these are rims and
        // badges read at a glance against dark wood, not ink on parchment.
        private static readonly Color DangerColour = UiTheme.Danger;
        private static readonly Color WarningColour = UiTheme.Rgb(0xe08c1a);
        private static readonly Color GoodColour = UiTheme.Rgb(0x4f9a2e);

        private static Color Colour(EventSeverity severity)
        {
            switch (severity)
            {
                case EventSeverity.Danger: return DangerColour;
                case EventSeverity.Warning: return WarningColour;
                default: return GoodColour;
            }
        }
    }
}
