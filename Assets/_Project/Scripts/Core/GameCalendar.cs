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

        // From the balance sheet (day_length_seconds), cached at Awake: at the shipped length, a
        // building at 2%/day fully decays in about 100 minutes of continuous play.
        private float _dayLengthSeconds;

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
            _dayLengthSeconds = BalanceConfig.Instance.DayLengthSeconds;
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < _dayLengthSeconds) return;
            _timer -= _dayLengthSeconds;

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
