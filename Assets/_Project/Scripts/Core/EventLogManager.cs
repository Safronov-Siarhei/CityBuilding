using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Core
{
    /// <summary>
    /// Where the game reports what happened to the settlement. Two outlets:
    ///
    /// - a small capped text history, each line stamped with its day (<see cref="Entries"/>) --
    ///   nothing on screen shows it any more, but it is what the tests read and the obvious source
    ///   for a history window should one ever be wanted;
    /// - <see cref="OnEventPosted"/>, which the event feed (see UI.EventFeedController) turns into
    ///   round icons that fade away and send the camera to the event when tapped. That replaced a
    ///   panel of text the user found too wordy.
    ///
    /// Deliberately narrow scope: only events that already matter elsewhere in the game get
    /// logged here (e.g. roads are excluded -- laying a dozen tiles in a row would otherwise flood
    /// this out of usefulness), not a general-purpose event bus for every little thing.
    /// </summary>
    public class EventLogManager : MonoBehaviour
    {
        public static EventLogManager Instance { get; private set; }

        private const int MaxEntries = 8;

        private readonly List<string> _entries = new List<string>();

        /// <summary>Newest first.</summary>
        public IReadOnlyList<string> Entries => _entries;

        public event Action OnLogChanged;

        /// <summary>Every event, including the log-only ones -- the listener decides what to draw.</summary>
        public event Action<GameEvent> OnEventPosted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>A line for the history and no icon -- the cheats, and anything with no picture worth drawing.</summary>
        public void Log(string message)
        {
            Post(GameEvent.Nowhere(message, EventSeverity.LogOnly, EventGlyph.None, null));
        }

        public void Post(GameEvent gameEvent)
        {
            if (gameEvent == null) return;
            AddEntry(gameEvent.Text);
            OnEventPosted?.Invoke(gameEvent);
        }

        private void AddEntry(string message)
        {
            var day = GameCalendar.Instance != null ? GameCalendar.Instance.CurrentDay : 1;
            _entries.Insert(0, Localization.Format("#log_day", day, message));
            while (_entries.Count > MaxEntries) _entries.RemoveAt(_entries.Count - 1);
            OnLogChanged?.Invoke();
        }
    }
}
