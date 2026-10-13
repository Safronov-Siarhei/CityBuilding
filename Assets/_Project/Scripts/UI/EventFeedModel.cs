using System.Collections.Generic;
using CityBuilder.Core;

namespace CityBuilder.UI
{
    /// <summary>One icon in the event feed: what it shows, how many times it happened, and how long it has left.</summary>
    public class EventFeedSlot
    {
        public GameEvent Event;
        public int Count;
        public float Duration;
        public float Remaining;

        /// <summary>Seconds since the icon appeared or last counted up -- what the pop animation runs on.</summary>
        public float Age;

        /// <summary>Which of the controller's prebuilt icon views draws this slot.</summary>
        public int View = -1;
    }

    /// <summary>
    /// The event feed's bookkeeping, apart from anything drawn: which icons are up, newest first,
    /// and when each one goes. A plain class so the rules can be tested without a scene.
    ///
    /// The rules:
    /// - log-only events never get an icon;
    /// - an event drawing the same icon as one already up does not add a second -- that icon counts
    ///   up, starts its time over and moves to the top (three fences falling apart is one icon
    ///   reading 3, not three icons);
    /// - with every slot taken, the quietest icon goes, the oldest of those first, so a fire is
    ///   never pushed out by "research finished".
    /// </summary>
    public class EventFeedModel
    {
        private readonly List<EventFeedSlot> _slots = new List<EventFeedSlot>();
        private readonly Stack<EventFeedSlot> _free = new Stack<EventFeedSlot>();
        private readonly int _capacity;
        private readonly float[] _lifetimeBySeverity;

        /// <param name="lifetimeBySeverity">Seconds an icon stays up, indexed by (int)EventSeverity.</param>
        public EventFeedModel(int capacity, float[] lifetimeBySeverity)
        {
            _capacity = capacity < 1 ? 1 : capacity;
            _lifetimeBySeverity = lifetimeBySeverity;
            for (var i = 0; i < _capacity; i++) _free.Push(new EventFeedSlot());
        }

        /// <summary>Newest first.</summary>
        public IReadOnlyList<EventFeedSlot> Slots => _slots;

        public int Capacity => _capacity;

        /// <summary>
        /// Puts the event up, or counts it into the icon already showing it. Returns the slot, or
        /// null for an event that gets no icon. When an icon had to make room,
        /// <paramref name="evictedView"/> is the view it was drawn on (else -1) -- a view index and
        /// not the slot, because the slot object is reused for the new event straight away and
        /// would no longer say which view to hide.
        /// </summary>
        public EventFeedSlot Add(GameEvent gameEvent, out int evictedView)
        {
            evictedView = -1;
            if (gameEvent == null || gameEvent.Severity == EventSeverity.LogOnly) return null;

            var slot = FindSameIcon(gameEvent);
            if (slot != null)
            {
                _slots.Remove(slot);
                slot.Count++;
            }
            else
            {
                if (_free.Count == 0)
                {
                    var evicted = PickEviction();
                    evictedView = evicted.View;
                    Remove(evicted);
                }

                slot = _free.Pop();
                slot.Count = 1;
                slot.View = -1;
            }

            // The newest report is the one the camera should go to: a second fire of the same kind
            // is somewhere else.
            slot.Event = gameEvent;
            slot.Duration = Lifetime(gameEvent.Severity);
            slot.Remaining = slot.Duration;
            slot.Age = 0f;
            _slots.Insert(0, slot);
            return slot;
        }

        /// <summary>Runs every icon's clock down. Returns how many expired, which are moved into <paramref name="expired"/> for the caller to hide.</summary>
        public int Tick(float deltaTime, List<EventFeedSlot> expired)
        {
            var count = 0;
            for (var i = _slots.Count - 1; i >= 0; i--)
            {
                var slot = _slots[i];
                slot.Age += deltaTime;
                slot.Remaining -= deltaTime;
                if (slot.Remaining > 0f) continue;

                _slots.RemoveAt(i);
                _free.Push(slot);
                expired?.Add(slot);
                count++;
            }
            return count;
        }

        public void Remove(EventFeedSlot slot)
        {
            if (slot == null || !_slots.Remove(slot)) return;
            _free.Push(slot);
        }

        private EventFeedSlot FindSameIcon(GameEvent gameEvent)
        {
            foreach (var slot in _slots)
            {
                if (slot.Event.SameIconAs(gameEvent)) return slot;
            }
            return null;
        }

        /// <summary>The quietest icon, and of those the one put up longest ago -- the list runs newest first, so that is the last one found.</summary>
        private EventFeedSlot PickEviction()
        {
            EventFeedSlot pick = null;
            foreach (var slot in _slots)
            {
                if (pick == null || slot.Event.Severity <= pick.Event.Severity) pick = slot;
            }
            return pick;
        }

        private float Lifetime(EventSeverity severity)
        {
            var index = (int)severity;
            return _lifetimeBySeverity != null && index < _lifetimeBySeverity.Length ? _lifetimeBySeverity[index] : 6f;
        }
    }
}
