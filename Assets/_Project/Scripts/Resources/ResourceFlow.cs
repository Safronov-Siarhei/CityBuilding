using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CityBuilder.Resources
{
    /// <summary>
    /// What came into the stores and went out of them over the last game day, per resource (the user, 2026-11-20:
    /// a tap on a top-bar chip says «+4 в день»). Wood and stone are carried in by walkers, so a fixed rate would lie:
    /// this MEASURES -- every positive ResourceManager.Add is a gain (production, tax, gifts, refunds), every negative
    /// one a spending (building, research, the meals); crystals through Wallet's own changes. Ten buckets of a tenth
    /// of a day each on game time; before a whole day has run the rate is the part so far stretched to a day.
    /// Cheap: two float adds per change, no allocation.
    /// </summary>
    public static class ResourceFlow
    {
        private const int Buckets = 10;
        /// <summary>The key crystals are kept under (they live in Wallet, not in ResourceManager).</summary>
        public const int CrystalsKey = -1;

        private class Track
        {
            public readonly float[] gained = new float[Buckets];
            public readonly float[] spent = new float[Buckets];
        }

        private static readonly Dictionary<int, Track> Tracks = new Dictionary<int, Track>();
        private static long _bucket = -1;
        private static float _start = -1f;
        private static int _crystals = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            Tracks.Clear();
            _bucket = -1;
            _start = -1f;
            Wallet.UpgradeCrystalsChanged -= OnCrystals;
            Wallet.UpgradeCrystalsChanged += OnCrystals;
            SceneManager.sceneLoaded -= OnScene;
            SceneManager.sceneLoaded += OnScene;
        }

        /// <summary>A new scene is a new town: the window starts again.</summary>
        private static void OnScene(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            Tracks.Clear();
            _bucket = -1;
            _start = Time.time;
            _crystals = Wallet.UpgradeCrystals;
        }

        private static float DayLength => GameCalendar.Instance != null && GameCalendar.Instance.DayLengthSeconds > 0f
            ? GameCalendar.Instance.DayLengthSeconds
            : (BalanceConfig.Instance != null ? BalanceConfig.Instance.DayLengthSeconds : 120f);

        private static void OnCrystals()
        {
            var now = Wallet.UpgradeCrystals;
            if (_crystals >= 0 && now != _crystals) Note(CrystalsKey, now - _crystals);
            _crystals = now;
        }

        /// <summary>ResourceManager.Add's stored change.</summary>
        public static void Note(ResourceType type, int delta) => Note((int)type, delta);

        private static void Note(int key, int delta)
        {
            if (delta == 0) return;
            var i = Advance();
            if (!Tracks.TryGetValue(key, out var track)) Tracks[key] = track = new Track();
            if (delta > 0) track.gained[i] += delta;
            else track.spent[i] -= delta;
        }

        /// <summary>Clears the buckets the clock has moved past; returns the current one's slot.</summary>
        private static int Advance()
        {
            if (_start < 0f) _start = Time.time;
            var now = (long)(Time.time / (DayLength / Buckets));
            if (_bucket < 0) _bucket = now;
            for (var b = _bucket + 1; b <= now && b - _bucket <= Buckets; b++)
            {
                var slot = (int)(b % Buckets);
                foreach (var t in Tracks.Values)
                {
                    t.gained[slot] = 0f;
                    t.spent[slot] = 0f;
                }
            }
            _bucket = now;
            return (int)(now % Buckets);
        }

        /// <summary>Gained and spent over the last day (stretched to a day while less of one has run).</summary>
        public static void PerDay(ResourceType type, out int gained, out int spent) => PerDay((int)type, out gained, out spent);

        public static void PerDay(int key, out int gained, out int spent)
        {
            gained = spent = 0;
            Advance();
            if (!Tracks.TryGetValue(key, out var track)) return;
            float g = 0f, s = 0f;
            for (var i = 0; i < Buckets; i++)
            {
                g += track.gained[i];
                s += track.spent[i];
            }
            var ran = Mathf.Clamp(Time.time - _start, DayLength / Buckets, DayLength);
            var k = DayLength / ran;
            gained = Mathf.RoundToInt(g * k);
            spent = Mathf.RoundToInt(s * k);
        }
    }
}
