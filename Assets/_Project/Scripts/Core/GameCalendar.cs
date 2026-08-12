using System;
using UnityEngine;

namespace CityBuilder.Core
{
    /// <summary>
    /// Ticks the game's day counter forward in real time. The only thing days currently drive is
    /// building decay (BuildingInstance subscribes to OnDayPassed), but this is deliberately
    /// generic infrastructure -- army upkeep, deaths-per-day, and a work week are all planned to
    /// hang off the same counter later rather than each growing their own timer.
    /// </summary>
    public class GameCalendar : MonoBehaviour
    {
        public static GameCalendar Instance { get; private set; }

        // First-pass pace, tunable: at this length, BuildingInstance.DecayPerDayBase (2%/day at
        // level 1) fully decays an un-repaired, un-upgraded building in about 100 minutes of
        // continuous play.
        private const float DayLengthSeconds = 120f;

        private float _timer;

        public int CurrentDay { get; private set; } = 1;

        public event Action OnDayPassed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < DayLengthSeconds) return;
            _timer -= DayLengthSeconds;

            CurrentDay++;
            OnDayPassed?.Invoke();
        }

        /// <summary>Used by save/load to restore an already-valid day count directly.</summary>
        public void SetCurrentDay(int day)
        {
            CurrentDay = Mathf.Max(1, day);
        }
    }
}
