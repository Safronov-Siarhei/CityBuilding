using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Core
{
    /// <summary>
    /// Small capped history of meaningful game events (building placed/lost to decay, settlement
    /// tier changes), each stamped with the day it happened on -- shown by EventLogController.
    /// Deliberately narrow scope: only events that already matter elsewhere in the game get
    /// logged here too (e.g. roads are excluded -- laying a dozen tiles in a row would otherwise
    /// flood this out of usefulness), not a general-purpose event bus for every little thing.
    /// </summary>
    public class EventLogManager : MonoBehaviour
    {
        public static EventLogManager Instance { get; private set; }

        private const int MaxEntries = 8;

        private readonly List<string> _entries = new List<string>();

        /// <summary>Newest first.</summary>
        public IReadOnlyList<string> Entries => _entries;

        public event Action OnLogChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Log(string message)
        {
            var day = GameCalendar.Instance != null ? GameCalendar.Instance.CurrentDay : 1;
            _entries.Insert(0, Localization.Format("#log_day", day, message));
            while (_entries.Count > MaxEntries) _entries.RemoveAt(_entries.Count - 1);
            OnLogChanged?.Invoke();
        }
    }
}
