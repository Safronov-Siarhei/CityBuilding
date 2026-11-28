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

        /// <summary>Label of every map definition in the pack (MeshMapCatalog loads them all by it).</summary>
        public const string MapLabel = "later_map";

        private const float RetryEvery = 30f;

        public static bool Ready { get; private set; }

        /// <summary>0..1 while downloading.</summary>
        public static float Progress { get; private set; }

        /// <summary>The last request failed, waits for Wi-Fi / the player's consent, or the phone is offline: say «нужен интернет».</summary>
        public static bool Stalled
        {
            get => !Ready && (_stalled || Application.internetReachability == NetworkReachability.NotReachable);
            private set => _stalled = value;
        }

        private static bool _stalled;

        /// <summary>Raised once, when <see cref="Ready"/> turns true.</summary>
        public static event Action BecameReady;

        private static bool _addressablesUp;
        private static bool _packHere;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            Ready = _addressablesUp = _packHere = Stalled = false;
            Progress = 0f;
            Addressables.InitializeAsync().Completed += _ => { _addressablesUp = true; Check(); };
#if UNITY_ANDROID && !UNITY_EDITOR
            var go = new GameObject("ContentPacks") { hideFlags = HideFlags.HideInHierarchy };
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
            Request();
#else
            _packHere = true;
#endif
        }

        /// <summary>Text for a screen that waits on the pack: «Загружаем… N %» or «Нужен интернет…».</summary>
        public static string StatusText =>
            Stalled ? Localization.Get("#content_offline") : Localization.Format("#content_loading", Mathf.RoundToInt(Progress * 100f));

        private static void Check()
        {
            if (Ready || !_addressablesUp || !_packHere) return;
            Ready = true;
            Progress = 1f;
            Stalled = false;
            Debug.Log("ContentPacks: the later content is here");
            BecameReady?.Invoke();
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static bool _asking;
        private static float _sinceAsk;

        private static void Request()
        {
            if (_packHere || _asking) return;
            if (!string.IsNullOrEmpty(AndroidAssetPacks.GetAssetPackPath(PackName))) { _packHere = true; Check(); return; }
            _asking = true;
            _sinceAsk = 0f;
            try { AndroidAssetPacks.DownloadAssetPackAsync(new[] { PackName }, OnState); }
            catch (InvalidOperationException e)
            {
                // no Play Core in this build (a plain APK): the bundles are in StreamingAssets
                Debug.Log("ContentPacks: no asset packs here (" + e.Message + ") -- the content is built in");
                _asking = false;
                _packHere = true;
                Check();
            }
        }

        private static void OnState(AndroidAssetPackInfo info)
        {
            if (info.name != PackName) return;
            if (info.size > 0) Progress = Mathf.Clamp01(info.bytesDownloaded / (float)info.size);
            switch (info.status)
            {
                case AndroidAssetPackStatus.Completed:
                    _asking = false;
                    _packHere = true;
                    Check();
                    break;
                case AndroidAssetPackStatus.Unknown:
                    // not installed from Google Play (adb, an APK): no packs -- the content is in the app itself
                    _asking = false;
                    _packHere = true;
                    Check();
                    break;
                case AndroidAssetPackStatus.Failed:
                case AndroidAssetPackStatus.Canceled:
                case AndroidAssetPackStatus.WaitingForWifi:
                case AndroidAssetPackStatus.RequiresUserConfirmation:
                    // never a dialog of its own: the pack waits, the game asks again later
                    _asking = false;
                    Stalled = true;
                    break;
                default:
                    Stalled = false;
                    break;
            }
        }

        private sealed class Runner : MonoBehaviour
        {
            private void Update()
            {
                if (_packHere) { enabled = false; return; }
                if (_asking) return;
                _sinceAsk += Time.unscaledDeltaTime;
                if (_sinceAsk >= RetryEvery) Request();
            }

            private void OnApplicationPause(bool paused)
            {
                if (!paused && !_packHere && !_asking) Request();
            }
        }
#endif
    }
}
