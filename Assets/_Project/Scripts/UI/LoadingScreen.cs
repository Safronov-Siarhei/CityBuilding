using System.Collections;
using CityBuilder.Core;
using CityBuilder.Maps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The loading screen between the menu and a game, both ways (the user, 2026-11-10, replacing the
    /// quill and the runner): a painted map of islands over the whole screen -- one of a few, at
    /// random, drawn for the screen's shape (Wide 20:9 / Tall 4:3, Tools/loading/loading_maps.py) and
    /// covering it, never stretched; along the bottom a course from an anchor to a red cross, dashed
    /// ahead, red behind, a flat ink galleon sailing it; a random tip above (#tip_1..30, empty ones
    /// skipped).
    ///
    /// The ship takes loading_seconds_enter into a game and loading_seconds_exit out to the menu, by
    /// the clock, smoothly, whatever the load does; if the scene is not up when it arrives, it waits at
    /// the cross and nothing moves; once both are done the screen fades out. One prefab in Resources,
    /// made by SetupProject; the map's texture is loaded for the screen alone and unloaded after.
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        private const string ResourcePath = "LoadingScreen";
        private const string MapsPath = "LoadingMaps/Map_";
        // a screen at least this wide for its height takes the Wide map (phones, 16:9 and wider), else the Tall one
        private const float WideFrom = 1.55f;
        private const float MaxReadySeconds = 15f;
        private const float FadeSeconds = 0.25f;
        private const float TipSeconds = 4f;
        // the course's shape, as baked (loading_maps.py ROUTE_*): y = -Amplitude * sin(2 pi u) on a strip this tall
        private const float CourseBakedHeight = 80f;
        private const float CourseBakedAmplitude = 16f;
        // the ship rocks a little on the swell
        private const float RockDegrees = 2.5f;
        private const float RockHz = 0.7f;
        private const float BobUnits = 3f;

        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image map;
        [SerializeField] private AspectRatioFitter mapFitter;
        [SerializeField] private RectTransform course;
        [SerializeField] private Image sailed;
        [SerializeField] private RectTransform ship;
        [SerializeField] private Text tip;
        [SerializeField] private string[] tips = new string[0];
        /// <summary>How many maps each shape has (Map_Wide_1..N, Map_Tall_1..N), written by the builder.</summary>
        [SerializeField] private int mapVariants = 1;

        /// <summary>A loading screen is up (a new load waits for it).</summary>
        public static bool Busy => _current != null;

        private static LoadingScreen _current;

        private float _sailSeconds;
        private float _shown;
        private int _tip;
        private Sprite _mapSprite;

        /// <summary>Into a game (a new one, a save, a dungeon and back).</summary>
        public static void IntoGame(string sceneName) => Load(sceneName, true);

        /// <summary>Out to the menu.</summary>
        public static void OutToMenu(string sceneName) => Load(sceneName, false);

        /// <summary>Loads `sceneName` behind the screen; without the prefab (a test scene), loads it plainly.</summary>
        public static void Load(string sceneName, bool intoGame) => Load(sceneName, intoGame, -1f);

        /// <summary>Into the campaign map: its own time (loading_seconds_campaign_map).</summary>
        public static void ToCampaignMap(string sceneName)
        {
            var config = BalanceConfig.Instance;
            Load(sceneName, true, config == null ? 5f : config.LoadingSecondsCampaignMap);
        }

        /// <summary>Loads `sceneName` behind the screen for `seconds` (below 0: the enter/exit time).</summary>
        public static void Load(string sceneName, bool intoGame, float seconds)
        {
            if (_current != null) return;
            var prefab = UnityEngine.Resources.Load<LoadingScreen>(ResourcePath);
            if (prefab == null)
            {
                SceneManager.LoadScene(sceneName);
                return;
            }
            _current = Instantiate(prefab);
            DontDestroyOnLoad(_current.gameObject);
            var config = BalanceConfig.Instance;
            if (seconds < 0f) seconds = config == null ? (intoGame ? 10f : 5f) : intoGame ? config.LoadingSecondsEnter : config.LoadingSecondsExit;
            _current.Begin(sceneName, Mathf.Max(0.5f, seconds));
        }

        private void Begin(string sceneName, float sailSeconds)
        {
            _sailSeconds = sailSeconds;
            var wide = Screen.height > 0 && Screen.width / (float)Screen.height >= WideFrom;
            _mapSprite = UnityEngine.Resources.Load<Sprite>($"{MapsPath}{(wide ? "Wide" : "Tall")}_{Random.Range(1, Mathf.Max(1, mapVariants) + 1)}");
            if (_mapSprite != null)
            {
                map.sprite = _mapSprite;
                map.color = Color.white;
                if (mapFitter != null) mapFitter.aspectRatio = _mapSprite.rect.width / _mapSprite.rect.height;
            }
            _tip = tips.Length > 0 ? Random.Range(0, tips.Length) : 0;
            ShowTip();
            Sail(0f);
            group.alpha = 0f;
            StartCoroutine(Run(sceneName));
        }

        private IEnumerator Run(string sceneName)
        {
            // in: the screen covers the old scene before it goes
            for (var t = 0f; t < FadeSeconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = t / FadeSeconds;
                yield return null;
            }
            group.alpha = 1f;
            var load = SceneManager.LoadSceneAsync(sceneName);
            var readyFor = 0f;
            var started = Time.realtimeSinceStartup;
            while (true)
            {
                var loaded = load == null || load.isDone;
                var ready = loaded && (SceneReady(sceneName) || readyFor > MaxReadySeconds);
                if (loaded) readyFor += Time.unscaledDeltaTime;
                // the ship goes by the clock alone; at the cross it waits for the scene
                var voyage = Mathf.Clamp01((Time.realtimeSinceStartup - started) / _sailSeconds);
                Sail(voyage);
                Tips();
                if (ready && voyage >= 1f) break;
                yield return null;
            }
            for (var t = 0f; t < FadeSeconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = 1f - t / FadeSeconds;
                yield return null;
            }
            _current = null;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (_current == this) _current = null;
            if (_mapSprite != null)
            {
                var texture = _mapSprite.texture;
                map.sprite = null;
                UnityEngine.Resources.UnloadAsset(texture);
                _mapSprite = null;
            }
        }

        /// <summary>A game is up once its map is laid out; the menu, at once.</summary>
        private static bool SceneReady(string sceneName)
        {
            if (SceneManager.GetActiveScene().name != sceneName) return false;
            var applier = MeshMapApplier.Instance;
            return applier == null || applier.CurrentMap != null;
        }

        /// <summary>The ship at `voyage` (0..1 of the time) along the course, eased in and out, the red line behind it.</summary>
        private void Sail(float voyage)
        {
            // half linear, half smoothstep: it gathers way and slows to the cross without crawling
            var u = Mathf.Lerp(voyage, voyage * voyage * (3f - 2f * voyage), 0.5f);
            if (sailed != null) sailed.fillAmount = u;
            if (course == null || ship == null) return;
            var rect = course.rect;
            var scale = rect.height / CourseBakedHeight;
            var phase = u * Mathf.PI * 2f;
            var y = -CourseBakedAmplitude * scale * Mathf.Sin(phase);
            // the course's slope (stretched across the screen's width), so the ship follows the curve
            var slope = -CourseBakedAmplitude * scale * Mathf.Cos(phase) * Mathf.PI * 2f / Mathf.Max(1f, rect.width);
            var time = Time.unscaledTime * RockHz * Mathf.PI * 2f;
            var arrived = voyage >= 1f;
            // at the cross it waits still, as the user asked: nothing on the screen changes
            var rock = arrived ? 0f : Mathf.Sin(time) * RockDegrees;
            var bob = arrived ? 0f : Mathf.Sin(time * 1.3f) * BobUnits;
            ship.anchoredPosition = new Vector2(rect.xMin + rect.width * u, y + bob);
            ship.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan(slope) * Mathf.Rad2Deg + rock);
        }

        private void Tips()
        {
            _shown += Time.unscaledDeltaTime;
            if (_shown < TipSeconds || tips.Length < 2) return;
            _shown = 0f;
            _tip = (_tip + 1) % tips.Length;
            ShowTip();
        }

        private void ShowTip()
        {
            if (tip == null) return;
            // a tip left empty in the sheet is skipped (#tip_1..30, the user fills them in over time)
            for (var k = 0; k < tips.Length; k++)
            {
                var text = Localization.Get(tips[(_tip + k) % tips.Length]);
                if (string.IsNullOrWhiteSpace(text) || text.StartsWith("#")) continue;
                _tip = (_tip + k) % tips.Length;
                tip.text = text;
                return;
            }
            tip.text = string.Empty;
        }
    }
}
