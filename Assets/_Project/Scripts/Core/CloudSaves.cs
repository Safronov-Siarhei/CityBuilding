using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using CityBuilder.Saving;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CityBuilder.Core
{
    /// <summary>
    /// The sandbox's saves in the cloud (the user, 2026-11-28: a player who deletes the game must not lose them), next to
    /// the account's progress (CloudSave) in the same Google Play Games account, as ONE more file: every slot's text,
    /// gzipped (a 30-80 KB save packs to a few KB), with the time it was written; a slot deleted on a phone travels as a
    /// tombstone so the cloud does not bring it back.
    ///   - Start (signed in, the main menu): read the file; a slot newer in the cloud than here (or missing here) is
    ///     written, a tombstone newer than the slot deletes it. Then, if this phone has anything the cloud has not, push.
    ///   - Then: when a slot changed (SaveSystem.Changes), push at most every <see cref="PushEvery"/> s, and on pause/quit.
    /// Nothing happens without a backend or a sign-in.
    /// </summary>
    public static class CloudSaves
    {
        public const string FileName = "sandbox_saves";
        private const float PushEvery = 60f;
        // the deleted slots, «name|ticks;...» -- a key of this phone («cloud.»), never in the progress snapshot
        private const string GoneKey = "cloud.saves_gone";
        private const string MenuScene = "MainMenu";
        private const string SplashScene = "Splash";

        private static bool _merged, _loading, _pushing, _dirty;
        private static int _lastChanges = -1;
        private static float _sincePush;
        private static float _retryAt;
        private static Bundle _waiting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            _merged = _loading = _pushing = _dirty = false;
            _lastChanges = -1;
            _sincePush = 0f;
            _retryAt = 0f;
            _waiting = null;
            SaveSystem.Removed += OnRemoved;
            var go = new GameObject("CloudSaves") { hideFlags = HideFlags.HideInHierarchy };
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
            SceneManager.activeSceneChanged += (_, to) => { if (to.name == MenuScene && _waiting != null) Merge(_waiting); };
        }

        private static bool Online => CloudSave.Ready && CloudSave.SignedIn && CloudSave.Backend != null;

        private static void OnRemoved(string name)
        {
            var gone = Gone();
            gone[name] = DateTime.UtcNow.Ticks;
            SaveGone(gone);
        }

        private static void Start()
        {
            if (_merged || _loading || _waiting != null || !Online || Time.unscaledTime < _retryAt) return;
            _loading = true;
            CloudSave.Backend.LoadFile(FileName, (ok, data) =>
            {
                _loading = false;
                if (!ok) { Debug.LogWarning("CloudSaves: load FAILED"); _retryAt = Time.unscaledTime + PushEvery; return; }
                var bundle = data != null && data.Length > 0 ? Unpack(data) : new Bundle();
                var scene = SceneManager.GetActiveScene().name;
                if (scene == MenuScene || scene == SplashScene) Merge(bundle);
                else _waiting = bundle;   // never under a game that is running
            });
        }

        private static void Merge(Bundle cloud)
        {
            _waiting = null;
            _merged = true;
            var local = new Dictionary<string, long>();
            foreach (var (name, utc) in SaveSystem.Slots()) local[name] = utc;
            var gone = Gone();
            var written = 0;
            var cloudHas = new Dictionary<string, long>();
            foreach (var e in cloud.slots)
            {
                cloudHas[e.n] = e.utc;
                local.TryGetValue(e.n, out var here);
                gone.TryGetValue(e.n, out var goneHere);
                if (e.gone)
                {
                    // deleted on another phone after this one last wrote it
                    if (here > 0 && e.utc >= here) { SaveSystem.DeleteQuietly(e.n); local.Remove(e.n); }
                    if (e.utc > goneHere) gone[e.n] = e.utc;
                    continue;
                }
                if (e.utc > here && e.utc > goneHere)
                {
                    SaveSystem.WriteRaw(e.n, e.d, e.utc);
                    local[e.n] = e.utc;
                    gone.Remove(e.n);
                    written++;
                }
            }
            SaveGone(gone);
            // anything here the cloud has not (or older): send it
            foreach (var pair in local)
                if (!cloudHas.TryGetValue(pair.Key, out var there) || there < pair.Value) _dirty = true;
            foreach (var pair in gone)
                if (!cloudHas.TryGetValue(pair.Key, out var there) || there < pair.Value) _dirty = true;
            _lastChanges = SaveSystem.Changes;
            Debug.Log($"CloudSaves: merged -- {written} slot(s) from the cloud, push {_dirty}");
        }

        private static void Push()
        {
            if (!_merged || _pushing || !Online) return;
            if (SaveSystem.Changes != _lastChanges) { _lastChanges = SaveSystem.Changes; _dirty = true; }
            if (!_dirty) return;
            _pushing = true;
            _dirty = false;
            _sincePush = 0f;
            var bundle = new Bundle();
            foreach (var (name, utc) in SaveSystem.Slots())
            {
                var text = SaveSystem.ReadRaw(name);
                if (text != null) bundle.slots.Add(new Slot { n = name, utc = utc, d = text });
            }
            foreach (var pair in Gone()) bundle.slots.Add(new Slot { n = pair.Key, utc = pair.Value, gone = true });
            var bytes = Pack(bundle);
            CloudSave.Backend.SaveFile(FileName, bytes, TimeSpan.FromSeconds(PlayTime.Seconds), ok =>
            {
                _pushing = false;
                if (!ok) _dirty = true;
                Debug.Log("CloudSaves: push " + (ok ? "ok, " + bytes.Length + " bytes" : "FAILED"));
            });
        }

        private static Dictionary<string, long> Gone()
        {
            var gone = new Dictionary<string, long>();
            foreach (var part in PlayerPrefs.GetString(GoneKey, string.Empty).Split(';'))
            {
                var bar = part.LastIndexOf('|');
                if (bar <= 0) continue;
                if (long.TryParse(part.Substring(bar + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var t)) gone[part.Substring(0, bar)] = t;
            }
            return gone;
        }

        private static void SaveGone(Dictionary<string, long> gone)
        {
            var sb = new StringBuilder();
            foreach (var pair in gone) sb.Append(pair.Key).Append('|').Append(pair.Value.ToString(CultureInfo.InvariantCulture)).Append(';');
            PlayerPrefs.SetString(GoneKey, sb.ToString());
            PlayerPrefs.Save();
        }

        private static byte[] Pack(Bundle bundle)
        {
            var raw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(bundle));
            using (var output = new MemoryStream())
            {
                using (var zip = new GZipStream(output, CompressionMode.Compress)) zip.Write(raw, 0, raw.Length);
                return output.ToArray();
            }
        }

        private static Bundle Unpack(byte[] data)
        {
            try
            {
                using (var input = new GZipStream(new MemoryStream(data), CompressionMode.Decompress))
                using (var reader = new StreamReader(input, Encoding.UTF8))
                    return JsonUtility.FromJson<Bundle>(reader.ReadToEnd()) ?? new Bundle();
            }
            catch (Exception e)
            {
                Debug.LogWarning("CloudSaves: unreadable file: " + e.Message);
                return new Bundle();
            }
        }

        [Serializable]
        private sealed class Bundle
        {
            public List<Slot> slots = new List<Slot>();
        }

        [Serializable]
        private sealed class Slot
        {
            public string n;     // the slot's name
            public long utc;     // when it was written (or deleted), UTC ticks
            public string d;     // its text
            public bool gone;    // deleted: a tombstone
        }

        private sealed class Runner : MonoBehaviour
        {
            private void Update()
            {
                if (!Online) return;
                if (!_merged) { Start(); return; }
                _sincePush += Time.unscaledDeltaTime;
                if (_sincePush >= PushEvery) { _sincePush = 0f; Push(); }
            }

            private void OnApplicationPause(bool paused)
            {
                if (paused && _merged) Push();
            }

            private void OnApplicationQuit()
            {
                if (_merged) Push();
            }
        }
    }
}
