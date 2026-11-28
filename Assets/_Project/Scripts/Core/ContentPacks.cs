using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace CityBuilder.Core
{
    /// <summary>
    /// The content that is NOT in the install (the user, 2026-11-28: a small first download, the rest downloads quietly
    /// right after -- Design/features/download-size.md). It is one Addressables group, <see cref="PackName"/>, built as a
    /// Play Asset Delivery FAST-FOLLOW pack: Google Play starts downloading it as soon as the app is installed; if that
    /// did not finish (no internet), every launch asks for it again, in the background, until it is on the phone.
    ///
    /// <see cref="Ready"/> -- the pack is on the phone and Addressables are up, so its assets load at once (even with
    /// WaitForCompletion: the Play Asset Delivery provider refuses a synchronous load only while a pack still has to
    /// download). The editor, Windows and an APK built without Play Asset Delivery (the pack's bundles then sit in
    /// StreamingAssets; Google reports the pack as Unknown) are Ready as soon as Addressables initialise.
    /// </summary>
    public static class ContentPacks
    {
        /// <summary>The fast-follow Addressables group and Play asset pack (letters, digits, underscores only: Google's rule).</summary>
        public const string PackName = "LaterContent";

        /// <summary>
        /// The small pack asked for FIRST (the user, 2026-11-28: «поставить первый приоритет»): the level-1 dungeon the
        /// player may walk into in the first minutes. Its own Play asset pack, so it lands in seconds, ahead of the big one.
        /// </summary>
        public const string FirstPackName = "FirstContent";

        /// <summary>Label of every map definition in the big pack (MeshMapCatalog loads them all by it).</summary>
        public const string MapLabel = "later_map";

        private const float RetryEvery = 30f;

        private static readonly string[] Names = { FirstPackName, PackName };
        private static readonly bool[] Here = new bool[2];
        private static readonly bool[] Asking = new bool[2];
        private static readonly bool[] Stuck = new bool[2];
        private static readonly float[] Done = new float[2];
        private static bool _addressablesUp;

        /// <summary>Every pack is on the phone and Addressables are up.</summary>
        public static bool Ready { get; private set; }

        /// <summary>The first (small) pack is on the phone and Addressables are up.</summary>
        public static bool FirstReady => _addressablesUp && Here[0];

        /// <summary>0..1 of the big pack while downloading.</summary>
        public static float Progress => Ready ? 1f : Done[1];

        /// <summary>0..1 of the first pack.</summary>
        public static float FirstProgress => FirstReady ? 1f : Done[0];

        /// <summary>The last request failed, waits for Wi-Fi / the player's consent, or the phone is offline: say «нужен интернет».</summary>
        public static bool Stalled => !Ready && (Stuck[0] || Stuck[1] || Offline);

        public static bool FirstStalled => !FirstReady && (Stuck[0] || Offline);

        private static bool Offline => Application.internetReachability == NetworkReachability.NotReachable;

        /// <summary>Raised once, when <see cref="Ready"/> turns true.</summary>
        public static event Action BecameReady;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            Ready = _addressablesUp = false;
            for (var i = 0; i < 2; i++) { Here[i] = Asking[i] = Stuck[i] = false; Done[i] = 0f; }
            Addressables.InitializeAsync().Completed += _ => { _addressablesUp = true; Check(); };
#if UNITY_ANDROID && !UNITY_EDITOR
            var go = new GameObject("ContentPacks") { hideFlags = HideFlags.HideInHierarchy };
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
            Request();
#else
            Here[0] = Here[1] = true;
#endif
        }

        /// <summary>Text for a screen that waits on the big pack: «Загружаем… N %» or «Нужен интернет…».</summary>
        public static string StatusText =>
            Stalled ? Localization.Get("#content_offline") : Localization.Format("#content_loading", Mathf.RoundToInt(Progress * 100f));

        /// <summary>The same for the first pack (a dungeon entered before it landed).</summary>
        public static string FirstStatusText =>
            FirstStalled ? Localization.Get("#content_offline") : Localization.Format("#content_loading", Mathf.RoundToInt(FirstProgress * 100f));

        private static void Check()
        {
            if (Ready || !_addressablesUp || !Here[0] || !Here[1]) return;
            Ready = true;
            Debug.Log("ContentPacks: the later content is here");
            BecameReady?.Invoke();
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static float _sinceAsk;

        private static void Arrived(int i)
        {
            Asking[i] = false;
            Stuck[i] = false;
            Here[i] = true;
            Done[i] = 1f;
            Check();
            // the big one only after the first (its priority)
            if (i == 0) Request();
        }

        private static void Request()
        {
            _sinceAsk = 0f;
            for (var i = 0; i < 2; i++)
            {
                if (Here[i] || Asking[i]) { if (!Here[i]) return; continue; }
                if (!string.IsNullOrEmpty(AndroidAssetPacks.GetAssetPackPath(Names[i]))) { Arrived(i); continue; }
                Asking[i] = true;
                try { AndroidAssetPacks.DownloadAssetPackAsync(new[] { Names[i] }, OnState); }
                catch (InvalidOperationException e)
                {
                    // no Play Core in this build (a plain APK): the bundles are in StreamingAssets
                    Debug.Log("ContentPacks: no asset packs here (" + e.Message + ") -- the content is built in");
                    for (var j = 0; j < 2; j++) { Here[j] = true; Asking[j] = false; Done[j] = 1f; }
                    Check();
                }
                return;   // one pack at a time, the first one first
            }
        }

        private static void OnState(AndroidAssetPackInfo info)
        {
            var i = Array.IndexOf(Names, info.name);
            if (i < 0) return;
            if (info.size > 0) Done[i] = Mathf.Clamp01(info.bytesDownloaded / (float)info.size);
            switch (info.status)
            {
                case AndroidAssetPackStatus.Completed:
                case AndroidAssetPackStatus.Unknown:
                    // Unknown: not installed from Google Play (adb, an APK): no packs -- the content is in the app itself
                    Arrived(i);
                    break;
                case AndroidAssetPackStatus.Failed:
                case AndroidAssetPackStatus.Canceled:
                case AndroidAssetPackStatus.WaitingForWifi:
                case AndroidAssetPackStatus.RequiresUserConfirmation:
                    // never a dialog of its own: the pack waits, the game asks again later
                    Asking[i] = false;
                    Stuck[i] = true;
                    break;
                default:
                    Stuck[i] = false;
                    break;
            }
        }

        private sealed class Runner : MonoBehaviour
        {
            private void Update()
            {
                if (Here[0] && Here[1]) { enabled = false; return; }
                if (Asking[0] || Asking[1]) return;
                _sinceAsk += Time.unscaledDeltaTime;
                if (_sinceAsk >= RetryEvery) Request();
            }

            private void OnApplicationPause(bool paused)
            {
                if (!paused && !(Here[0] && Here[1]) && !Asking[0] && !Asking[1]) Request();
            }
        }
#endif
    }
}
