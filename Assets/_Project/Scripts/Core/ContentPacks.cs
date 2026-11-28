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
    /// right after -- Design/features/download-size.md). Play Asset Delivery ON-DEMAND packs (fast-follow made Google
    /// Play show the whole 200 MB and shut «Играть» until all landed, AAB 1643), one Addressables group each, split by
    /// what the player may need first (the user: «поэтапно подгружались части игры»): the game asks for them from its
    /// first launch, one pack at a time in <see cref="Pack"/> order, at every launch until all are on the phone. A pack is usable the moment IT is here -- the sandbox does not wait for the dungeons.
    ///
    /// The editor, Windows and an APK built without Play Asset Delivery (the bundles sit in StreamingAssets; Google
    /// reports a pack as Unknown) have every pack as soon as Addressables initialise.
    /// </summary>
    public static class ContentPacks
    {
        /// <summary>The packs in the order they are asked for (names: letters, digits, underscores -- Google's rule).</summary>
        public enum Pack
        {
            /// <summary>The start map's level-1 dungeon: the first minutes may walk into it.</summary>
            First,
            /// <summary>The archipelago's other islands, the sandbox-only level 3-5 models and pictures.</summary>
            Sandbox,
            /// <summary>Campaign missions 2-3: films, maps.</summary>
            Campaign,
            Dungeons1,
            Dungeons2,
            Dungeons3,
        }

        private static readonly string[] Names = { "FirstContent", "SandboxContent", "CampaignContent", "Dungeon1Content", "Dungeon2Content", "Dungeon3Content" };
        private static readonly int Count = Names.Length;

        public static string NameOf(Pack pack) => Names[(int)pack];

        /// <summary>Label of a pack's map definitions (MeshMapCatalog loads each pack's by its own).</summary>
        public static string MapLabelOf(Pack pack) => "maps_" + Names[(int)pack];

        /// <summary>Which pack a map definition of the pack folders is in (SetupProjectContent files them by this, the
        /// loaders wait on it). Null: not a pack map (it is in the install).</summary>
        public static Pack? PackForMap(string mapId)
        {
            if (string.IsNullOrEmpty(mapId)) return null;
            if (mapId == "Dungeon1") return Pack.First;
            if (mapId.StartsWith("Dungeon1")) return Pack.Dungeons1;
            if (mapId.StartsWith("Dungeon2")) return Pack.Dungeons2;
            if (mapId.StartsWith("Dungeon3")) return Pack.Dungeons3;
            if (mapId.StartsWith("Mission")) return Pack.Campaign;
            return null;
        }

        private const float RetryEvery = 30f;

        private static readonly bool[] Here = new bool[Names.Length];
        private static readonly bool[] Asking = new bool[Names.Length];
        private static readonly bool[] Stuck = new bool[Names.Length];
        private static readonly float[] Done = new float[Names.Length];
        private static bool _addressablesUp;

        /// <summary>Every pack is on the phone and Addressables are up.</summary>
        public static bool Ready { get; private set; }

        /// <summary>This pack is on the phone and Addressables are up: its assets load at once.</summary>
        public static bool IsReady(Pack pack) => _addressablesUp && Here[(int)pack];

        /// <summary>0..1 of this pack while it downloads.</summary>
        public static float ProgressOf(Pack pack) => IsReady(pack) ? 1f : Done[(int)pack];

        /// <summary>The pack's last request failed, waits for Wi-Fi / the player's consent, or the phone is offline.</summary>
        public static bool IsStalled(Pack pack) => !IsReady(pack) && (Stuck[(int)pack] || Offline);

        private static bool Offline => Application.internetReachability == NetworkReachability.NotReachable;

        /// <summary>Raised once, when <see cref="Ready"/> turns true.</summary>
        public static event Action BecameReady;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            Ready = _addressablesUp = false;
            for (var i = 0; i < Count; i++) { Here[i] = Asking[i] = Stuck[i] = false; Done[i] = 0f; }
            Addressables.InitializeAsync().Completed += _ => { _addressablesUp = true; Check(); };
#if UNITY_ANDROID && !UNITY_EDITOR
            var go = new GameObject("ContentPacks") { hideFlags = HideFlags.HideInHierarchy };
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
            Request();
#else
            for (var i = 0; i < Count; i++) Here[i] = true;
#endif
        }

        /// <summary>Text for a screen that waits on a pack: «Для этого режима докачиваются файлы: N %» or «Нужен интернет…».</summary>
        public static string StatusOf(Pack pack) =>
            IsStalled(pack) ? Localization.Get("#content_offline") : Localization.Format("#content_loading", Mathf.RoundToInt(ProgressOf(pack) * 100f));

        private static void Check()
        {
            if (Ready || !_addressablesUp) return;
            for (var i = 0; i < Count; i++) if (!Here[i]) return;
            Ready = true;
            Debug.Log("ContentPacks: all the later content is here");
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
            Debug.Log("ContentPacks: " + Names[i] + " is here");
            Check();
            Request();   // the next one
        }

        private static bool AnyAsking()
        {
            for (var i = 0; i < Count; i++) if (Asking[i]) return true;
            return false;
        }

        private static bool AllHere()
        {
            for (var i = 0; i < Count; i++) if (!Here[i]) return false;
            return true;
        }

        /// <summary>Asks Play for the first pack not on the phone yet -- one at a time, in order.</summary>
        private static void Request()
        {
            _sinceAsk = 0f;
            if (AnyAsking()) return;
            for (var i = 0; i < Count; i++)
            {
                if (Here[i]) continue;
                if (!string.IsNullOrEmpty(AndroidAssetPacks.GetAssetPackPath(Names[i]))) { Arrived(i); return; }
                Asking[i] = true;
                try { AndroidAssetPacks.DownloadAssetPackAsync(new[] { Names[i] }, OnState); }
                catch (InvalidOperationException e)
                {
                    // no Play Core in this build (a plain APK): the bundles are in StreamingAssets
                    Debug.Log("ContentPacks: no asset packs here (" + e.Message + ") -- the content is built in");
                    for (var j = 0; j < Count; j++) { Here[j] = true; Asking[j] = false; Done[j] = 1f; }
                    Check();
                }
                return;
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
                    if (!Here[i]) Arrived(i);
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
                if (AllHere()) { enabled = false; return; }
                if (AnyAsking()) return;
                _sinceAsk += Time.unscaledDeltaTime;
                if (_sinceAsk >= RetryEvery) Request();
            }

            private void OnApplicationPause(bool paused)
            {
                if (!paused && !AllHere() && !AnyAsking()) Request();
            }
        }
#endif
    }
}
