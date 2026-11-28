using System;
using System.Collections;
using Unity.Services.LevelPlay;
using UnityEngine;

namespace CityBuilder.Shop
{
    /// <summary>
    /// Rewarded ads: Unity LevelPlay with the Unity Ads network (the user, 2026-11-24 / 2026-11-28; card
    /// Design/features/ads.md). Rewarded only, always started by the player's tap -- no forced ads.
    ///
    ///   - Live in an Android player whose ids file (Resources/levelplay_ids.txt: line 1 the app key, line 2 the
    ///     rewarded ad unit id, both from the LevelPlay dashboard) is filled; the editor, Windows and a build without
    ///     ids keep the old stand-in (a few real seconds, then the reward).
    ///   - The shop shows «Смотреть» only while <see cref="IsReady"/> (the user, 2026-11-11: no fill -> only the
    ///     crystal button). An ad is loaded at start and again after each show; a failed load retries in 30 s.
    ///   - <see cref="Show"/> answers once: true = the ad paid out (OnAdRewarded), false = it did not show or was
    ///     closed early. The reward is given only on true.
    /// </summary>
    public static class RewardedAds
    {
        public const string IdsResource = "levelplay_ids";
        private const float RetrySeconds = 30f;
        private const float LateRewardSeconds = 1.5f;

        /// <summary>The cheat panel's «no ads» switch, to see the shop without fill.</summary>
        public static bool NoFillForTest;

        private static LevelPlayRewardedAd _ad;
        private static bool _live;
        private static Action<bool> _done;
        private static bool _rewarded;
        private static bool _stubPlaying;

        /// <summary>A real network is in charge (not the stand-in).</summary>
        public static bool Live => _live;

        /// <summary>An ad can be shown now.</summary>
        public static bool IsReady
        {
            get
            {
                if (NoFillForTest || _done != null || _stubPlaying) return false;
                if (!_live) return true;
                return _ad != null && _ad.IsAdReady();
            }
        }

        /// <summary>Raised when an ad became ready or stopped being so (the windows redraw their «Смотреть»).</summary>
        public static event Action Changed;

        /// <summary>Shows a rewarded ad; <paramref name="done"/> gets true once it paid out. False if none can be shown now.</summary>
        public static bool Show(string placement, Action<bool> done)
        {
            if (!IsReady) return false;
            if (!_live)
            {
                Runner.Instance.StartCoroutine(Stub(done));
                return true;
            }
            _done = done;
            _rewarded = false;
            _ad.ShowAd(placement);
            return true;
        }

        /// <summary><see cref="Show"/> for a coroutine: waits until the ad is over; <paramref name="done"/> as there. Ends at once (false) if none could be shown.</summary>
        public static IEnumerator Watch(string placement, Action<bool> done)
        {
            var over = false;
            var paid = false;
            if (!Show(placement, ok => { paid = ok; over = true; })) over = true;
            while (!over) yield return null;
            done?.Invoke(paid);
        }

        private static IEnumerator Stub(Action<bool> done)
        {
            _stubPlaying = true;
            Changed?.Invoke();
            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, Core.BalanceConfig.Sandbox.ShopAdStubSeconds));
            _stubPlaying = false;
            done?.Invoke(true);
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Application.isEditor || Application.platform != RuntimePlatform.Android) return;
            var ids = UnityEngine.Resources.Load<TextAsset>(IdsResource);
            var lines = ids != null ? ids.text.Split('\n') : Array.Empty<string>();
            var appKey = lines.Length > 0 ? lines[0].Trim() : "";
            var unitId = lines.Length > 1 ? lines[1].Trim() : "";
            if (appKey.Length == 0 || unitId.Length == 0)
            {
                Debug.Log("RewardedAds: no LevelPlay ids in Resources/" + IdsResource + " -- the stand-in stays");
                return;
            }
            LevelPlay.OnInitSuccess += _ => Created(unitId);
            LevelPlay.OnInitFailed += error => Debug.LogWarning("RewardedAds: LevelPlay init failed: " + error.ErrorMessage);
            LevelPlay.Init(appKey);
        }

        private static void Created(string unitId)
        {
            if (_ad != null) return;
            _ad = new LevelPlayRewardedAd(unitId);
            _ad.OnAdLoaded += _ => Changed?.Invoke();
            _ad.OnAdLoadFailed += error =>
            {
                Debug.Log("RewardedAds: no ad loaded: " + error.ErrorMessage);
                Runner.Instance.Retry();
            };
            _ad.OnAdRewarded += (_, _) => _rewarded = true;
            _ad.OnAdDisplayFailed += (_, error) =>
            {
                Debug.LogWarning("RewardedAds: ad not shown: " + error.ErrorMessage);
                Finish();
            };
            // some networks report the reward a moment AFTER the close: wait for it a little
            _ad.OnAdClosed += _ => Runner.Instance.StartCoroutine(FinishSoon());
            _live = true;
            _ad.LoadAd();
            Debug.Log("RewardedAds: LevelPlay live");
        }

        private static IEnumerator FinishSoon()
        {
            var until = Time.realtimeSinceStartup + LateRewardSeconds;
            while (!_rewarded && Time.realtimeSinceStartup < until) yield return null;
            Finish();
        }

        private static void Finish()
        {
            if (_done == null) return;
            var done = _done;
            var paid = _rewarded;
            _done = null;
            _rewarded = false;
            _ad?.LoadAd();
            done?.Invoke(paid);
            Changed?.Invoke();
        }

        /// <summary>A hidden object for the stand-in's coroutine and the load retry.</summary>
        private sealed class Runner : MonoBehaviour
        {
            private static Runner _instance;

            public static Runner Instance
            {
                get
                {
                    if (_instance != null) return _instance;
                    var go = new GameObject("RewardedAds") { hideFlags = HideFlags.HideAndDontSave };
                    DontDestroyOnLoad(go);
                    return _instance = go.AddComponent<Runner>();
                }
            }

            public void Retry()
            {
                CancelInvoke(nameof(Load));
                Invoke(nameof(Load), RetrySeconds);
            }

            private void Load()
            {
                if (_ad != null && !_ad.IsAdReady()) _ad.LoadAd();
            }
        }
    }
}
