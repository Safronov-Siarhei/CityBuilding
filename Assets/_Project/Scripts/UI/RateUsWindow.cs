using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The rate-us window (the user, 2026-11-10; mock-up v6 in Design/store/RateWindow_Mockup.png): a
    /// popup that can be put up at any moment over whatever the game shows -- the King on the left, the
    /// orc chief on the right, a blurred town in the frame, a parchment with stars, «Оценить» and
    /// «Позже». No close X: «Позже» or a tap outside the frame closes it.
    ///
    /// «Оценить» opens the game's store page (Google Play's in-app review sheet has a quota and may
    /// silently not show, so a button must not lean on it). Google's rules: our stars are decoration
    /// only, no «do you like it?» question before it, no reward for rating.
    ///
    /// WHEN it shows is not decided yet (the user will say); the answers are kept in PlayerPrefs (the
    /// player's device, not a save) for that rule to read: <see cref="Rated"/>, <see cref="LaterCount"/>,
    /// <see cref="LastLaterUtc"/>. One prefab in Resources (SetupProjectRateUs); its art is loaded for
    /// the window alone and unloaded after.
    /// </summary>
    public class RateUsWindow : MonoBehaviour
    {
        private const string ResourcePath = "RateUsWindow";
        private const string RatedPref = "rate_us_rated";
        private const string LaterCountPref = "rate_us_later_count";
        private const string LaterAtPref = "rate_us_later_at";
        private const float PopSeconds = 0.22f;
        private const float CloseSeconds = 0.15f;
        private const float PopFromScale = 0.85f;

        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform window;
        [SerializeField] private Button shade;
        [SerializeField] private Button rate;
        [SerializeField] private Button later;
        /// <summary>The window's size against the art's 1920 x 1080 reference (the user, 2026-11-10: half).</summary>
        [SerializeField] private float size = 1f;

        private static RateUsWindow _current;
        // the interface behind, hidden while the window is up (the user, 2026-11-10: «скрывать весь
        // задний интерфейс везде»): every other screen-space root canvas under ours, switched off and back
        private readonly System.Collections.Generic.List<Canvas> _hidden = new System.Collections.Generic.List<Canvas>();
        private float _t;
        private bool _closing;

        /// <summary>The window is up.</summary>
        public static bool IsOpen => _current != null;

        /// <summary>The player pressed «Оценить» once (the window should not come back).</summary>
        public static bool Rated => PlayerPrefs.GetInt(RatedPref, 0) == 1;

        /// <summary>How many times the player put it off («Позже» or a tap outside).</summary>
        public static int LaterCount => PlayerPrefs.GetInt(LaterCountPref, 0);

        /// <summary>When the player last put it off (UTC), or null.</summary>
        public static System.DateTime? LastLaterUtc =>
            long.TryParse(PlayerPrefs.GetString(LaterAtPref, string.Empty), out var ticks)
                ? new System.DateTime(ticks, System.DateTimeKind.Utc)
                : (System.DateTime?)null;

        /// <summary>Puts the window up over the current scene (nothing if it is already up or the prefab is missing).</summary>
        public static void Show()
        {
            if (_current != null) return;
            var prefab = UnityEngine.Resources.Load<RateUsWindow>(ResourcePath);
            if (prefab == null) return;
            _current = Instantiate(prefab);
        }

        private void Awake()
        {
            shade.onClick.AddListener(PutOff);
            later.onClick.AddListener(PutOff);
            rate.onClick.AddListener(Rate);
            group.alpha = 0f;
            window.localScale = Vector3.one * (PopFromScale * size);
            HideTheInterfaceBehind();
        }

        private void HideTheInterfaceBehind()
        {
            var own = GetComponent<Canvas>();
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (canvas == own || !canvas.enabled || !canvas.isRootCanvas) continue;
                // the 3D world's own labels stay; anything drawn over us (the loading screen) too
                if (canvas.renderMode == RenderMode.WorldSpace || canvas.sortingOrder > own.sortingOrder) continue;
                canvas.enabled = false;
                _hidden.Add(canvas);
            }
        }

        private void ShowTheInterfaceBehind()
        {
            foreach (var canvas in _hidden)
                if (canvas != null) canvas.enabled = true;
            _hidden.Clear();
        }

        private void Update()
        {
            // unscaled: the window may come up over a paused game
            _t += Time.unscaledDeltaTime;
            if (_closing)
            {
                group.alpha = 1f - Mathf.Clamp01(_t / CloseSeconds);
                if (_t >= CloseSeconds) Destroy(gameObject);
                return;
            }
            var k = Mathf.Clamp01(_t / PopSeconds);
            group.alpha = k;
            // a small overshoot so the window pops rather than slides in
            var back = 1f + 2.2f * Mathf.Pow(k - 1f, 3f) + 1.2f * Mathf.Pow(k - 1f, 2f);
            window.localScale = Vector3.one * (size * Mathf.LerpUnclamped(PopFromScale, 1f, back));
        }

        private void Rate()
        {
            if (_closing) return;
            PlayerPrefs.SetInt(RatedPref, 1);
            PlayerPrefs.Save();
            var id = Application.identifier;
#if UNITY_ANDROID && !UNITY_EDITOR
            Application.OpenURL("market://details?id=" + id);
#else
            Application.OpenURL("https://play.google.com/store/apps/details?id=" + id);
#endif
            Close();
        }

        private void PutOff()
        {
            if (_closing) return;
            PlayerPrefs.SetInt(LaterCountPref, LaterCount + 1);
            PlayerPrefs.SetString(LaterAtPref, System.DateTime.UtcNow.Ticks.ToString());
            PlayerPrefs.Save();
            Close();
        }

        private void Close()
        {
            _closing = true;
            _t = 0f;
            group.interactable = false;
            ShowTheInterfaceBehind();
        }

        private void OnDestroy()
        {
            ShowTheInterfaceBehind();
            if (_current != this) return;
            _current = null;
            // the window's big pictures go with it
            UnityEngine.Resources.UnloadUnusedAssets();
        }
    }
}
