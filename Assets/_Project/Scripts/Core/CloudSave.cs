using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CityBuilder.Core
{
    /// <summary>
    /// The account's progress in the cloud (the user, 2026-11-25: «весь прогресс аккаунта» -- crystals, the campaign,
    /// records, the piggy bank, purchases' flags -- into Google Play Games saved games; the sandbox's map files later).
    ///
    /// The progress is the PlayerPrefs keys written through <see cref="Prefs"/>, minus the device's own (settings,
    /// graphics, language, the billing's pending flags): one snapshot, last change wins.
    ///   - Start: sign in, read the cloud. If the cloud's snapshot changed later than this phone's progress, it
    ///     replaces it (a fresh install takes the cloud). The main menu is reloaded to show it; inside a game the
    ///     snapshot waits for the next main menu.
    ///   - Then: every <see cref="CheckEvery"/> s and when the app goes to the background, a changed progress is
    ///     stamped with the time and written up (at most every <see cref="PushEvery"/> s, at once on pause).
    ///   - <see cref="Ready"/> turns true when the start's read is settled (or failed, or timed out): the billing holds
    ///     purchases until then so a cloud snapshot can never wipe a fresh purchase.
    /// No backend (the editor, Windows, a build without a Play Games app id): Ready at once, nothing else happens.
    /// </summary>
    public static class CloudSave
    {
        /// <summary>The cloud itself; the Play Games assembly puts it here before the first scene.</summary>
        public static ICloudBackend Backend;

        public const string ChangedUtcKey = "cloud.changed_utc";
        private const float CheckEvery = 20f;
        private const float PushEvery = 60f;
        private const float StartTimeout = 15f;
        private const string MenuScene = "MainMenu";
        private const string SplashScene = "Splash";

        /// <summary>Keys that belong to this phone, never to the account.</summary>
        private static readonly string[] LocalPrefixes =
        {
            Prefs.IndexKey, "cloud.", "store.pending.", "settings.", "graphics_", "CityBuilder.Language",
        };

        public static bool Ready { get; private set; }

        /// <summary>Raised once, when <see cref="Ready"/> turns true.</summary>
        public static event Action BecameReady;

        /// <summary>Raised after a cloud snapshot replaced this phone's progress.</summary>
        public static event Action Applied;

        private static bool _signedIn;
        private static bool _baselineTaken;
        private static ulong _lastHash;
        private static bool _dirty;
        private static bool _pushing;
        private static float _sinceCheck, _sincePush, _sinceStart;
        private static Snapshot _waiting;

        public static bool IsLocal(string key)
        {
            foreach (var prefix in LocalPrefixes)
                if (key.StartsWith(prefix, StringComparison.Ordinal)) return true;
            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            Ready = false;
            _signedIn = _baselineTaken = _dirty = _pushing = false;
            _waiting = null;
            _sinceCheck = _sincePush = _sinceStart = 0f;
            var go = new GameObject("CloudSave") { hideFlags = HideFlags.HideInHierarchy };
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
            SceneManager.activeSceneChanged += (_, to) => { if (to.name == MenuScene && _waiting != null) Apply(_waiting); };

            if (Backend == null) { SetReady(); return; }
            Backend.SignIn(ok =>
            {
                _signedIn = ok;
                if (!ok) { SetReady(); return; }
                Backend.Load((loaded, data) =>
                {
                    if (loaded && data != null && data.Length > 0) Consider(data);
                    SetReady();
                });
            });
        }

        private static void SetReady()
        {
            if (Ready) return;
            Ready = true;
            TakeBaseline();
            BecameReady?.Invoke();
        }

        private static void TakeBaseline()
        {
            _lastHash = Hash(Build(0).Joined());
            _baselineTaken = true;
        }

        /// <summary>The cloud's snapshot: taken when it changed later than this phone's progress.</summary>
        private static void Consider(byte[] data)
        {
            Snapshot snap;
            try { snap = JsonUtility.FromJson<Snapshot>(Encoding.UTF8.GetString(data)); }
            catch (Exception e) { Debug.LogWarning("CloudSave: unreadable snapshot: " + e.Message); return; }
            if (snap == null || snap.k == null) return;
            long local = LocalChangedUtc;
            if (snap.utc <= local) { if (snap.utc < local) _dirty = true; return; }
            var scene = SceneManager.GetActiveScene().name;
            if (scene == MenuScene || scene == SplashScene) Apply(snap);
            else _waiting = snap;
        }

        private static long LocalChangedUtc =>
            long.TryParse(PlayerPrefs.GetString(ChangedUtcKey, "0"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var t) ? t : 0L;

        private static void Apply(Snapshot snap)
        {
            _waiting = null;
            var keep = new HashSet<string>(snap.k);
            var stale = new List<string>();
            foreach (var pair in Prefs.Index)
                if (!IsLocal(pair.Key) && !keep.Contains(pair.Key)) stale.Add(pair.Key);
            foreach (var key in stale) Prefs.DeleteKey(key);
            for (var i = 0; i < snap.k.Count; i++)
            {
                var key = snap.k[i];
                if (IsLocal(key)) continue;
                var value = snap.v[i];
                switch (snap.t[i])
                {
                    case "i": Prefs.SetInt(key, int.Parse(value, CultureInfo.InvariantCulture)); break;
                    case "f": Prefs.SetFloat(key, float.Parse(value, CultureInfo.InvariantCulture)); break;
                    default: Prefs.SetString(key, value); break;
                }
            }
            PlayerPrefs.SetString(ChangedUtcKey, snap.utc.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
            TakeBaseline();
            _dirty = false;
            Applied?.Invoke();
            if (SceneManager.GetActiveScene().name == MenuScene) SceneManager.LoadScene(MenuScene);
        }

        /// <summary>Has the progress changed since the last look? Then stamp it and mark it for the cloud.</summary>
        private static void Check()
        {
            if (!_baselineTaken) return;
            var hash = Hash(Build(0).Joined());
            if (hash == _lastHash) return;
            _lastHash = hash;
            PlayerPrefs.SetString(ChangedUtcKey, DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
            _dirty = true;
        }

        private static void Push()
        {
            if (!_dirty || _pushing || !_signedIn || Backend == null) return;
            _pushing = true;
            _dirty = false;
            _sincePush = 0f;
            var snap = Build(LocalChangedUtc);
            var bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(snap));
            Backend.Save(bytes, TimeSpan.FromSeconds(PlayTime.Seconds), ok =>
            {
                _pushing = false;
                if (!ok) _dirty = true;
            });
        }

        private static Snapshot Build(long utc)
        {
            var keys = new List<string>();
            foreach (var pair in Prefs.Index)
                if (!IsLocal(pair.Key) && PlayerPrefs.HasKey(pair.Key)) keys.Add(pair.Key);
            keys.Sort(StringComparer.Ordinal);
            var snap = new Snapshot { utc = utc, k = keys, t = new List<string>(keys.Count), v = new List<string>(keys.Count) };
            foreach (var key in keys)
            {
                var type = Prefs.Index[key];
                snap.t.Add(type.ToString());
                snap.v.Add(type == 'i' ? PlayerPrefs.GetInt(key).ToString(CultureInfo.InvariantCulture)
                    : type == 'f' ? PlayerPrefs.GetFloat(key).ToString("R", CultureInfo.InvariantCulture)
                    : PlayerPrefs.GetString(key));
            }
            return snap;
        }

        private static ulong Hash(string text)
        {
            var h = 14695981039346656037UL; // FNV-1a: stable between launches, unlike string.GetHashCode
            foreach (var c in text) { h ^= c; h *= 1099511628211UL; }
            return h;
        }

        [Serializable]
        private sealed class Snapshot
        {
            public long utc;
            public List<string> k;
            public List<string> t;
            public List<string> v;

            public string Joined()
            {
                var sb = new StringBuilder();
                for (var i = 0; i < k.Count; i++) sb.Append(k[i]).Append('\u0001').Append(v[i]).Append('\u0002');
                return sb.ToString();
            }
        }

        private sealed class Runner : MonoBehaviour
        {
            private void Update()
            {
                var dt = Time.unscaledDeltaTime;
                if (!Ready)
                {
                    _sinceStart += dt;
                    if (_sinceStart >= StartTimeout) SetReady();
                    return;
                }
                _sinceCheck += dt;
                _sincePush += dt;
                if (_sinceCheck >= CheckEvery) { _sinceCheck = 0f; Check(); }
                if (_sincePush >= PushEvery) Push();
            }

            private void OnApplicationPause(bool paused)
            {
                if (!paused || !Ready) return;
                Check();
                Push();
            }

            private void OnApplicationQuit()
            {
                if (!Ready) return;
                Check();
                Push();
            }
        }
    }

    /// <summary>Where <see cref="CloudSave"/> keeps the snapshot (Google Play Games saved games on Android).</summary>
    public interface ICloudBackend
    {
        void SignIn(Action<bool> done);

        /// <summary>Reads the snapshot: (true, bytes) / (true, null or empty) when there is none yet / (false, null) on an error.</summary>
        void Load(Action<bool, byte[]> done);

        void Save(byte[] data, TimeSpan played, Action<bool> done);
    }
}
