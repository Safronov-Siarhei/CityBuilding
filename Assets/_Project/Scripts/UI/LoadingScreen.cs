using System.Collections;
using CityBuilder.Core;
using CityBuilder.Maps;
using CityBuilder.Saving;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The drawn loading screen between the menu and a game, both ways (the user, 2026-10-25:
    /// «прикольный загрузочный экран, который нарисован»; 1 for the sandbox, 2 for the Invasion,
    /// tips, shown on the way out too):
    ///
    /// - sandbox: the map's own ink-on-parchment chart (UI/Loading/Ink_&lt;map&gt;.png) drawn in from
    ///   the left by a quill riding the line as the scene loads; the map's name when it is done;
    /// - invasion: the theme's painted scene (Load_&lt;theme&gt;.png), a wooden plank filling along the
    ///   bottom with a militiaman running on it;
    /// - a tip under either, a new one every few seconds (#tip_sandbox_N / #tip_invasion_N).
    ///
    /// One prefab in Resources, made by SetupProject; it lives across the scene change and goes
    /// once the new scene is up (a game: its map laid out) and it has been seen for a moment.
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        public enum Look { Sandbox = 0, Invasion = 1 }

        private const string ResourcePath = "LoadingScreen";
        private const float MinSeconds = 2.4f;
        private const float MaxReadySeconds = 15f;
        private const float FadeSeconds = 0.25f;
        private const float TipSeconds = 4f;

        [SerializeField] private CanvasGroup group;
        [SerializeField] private GameObject sandboxRoot;
        [SerializeField] private Image chart;
        /// <summary>The same chart, faint, under the ink: the pencil the quill inks over.</summary>
        [SerializeField] private Image chartFaint;
        [SerializeField] private RectTransform quill;
        [SerializeField] private Text mapName;
        [SerializeField] private GameObject invasionRoot;
        [SerializeField] private Image scene;
        [SerializeField] private Image plankFill;
        [SerializeField] private RectTransform plank;
        [SerializeField] private Image runner;
        [SerializeField] private Sprite[] runnerFrames = new Sprite[0];
        [SerializeField] private Text tip;
        [SerializeField] private string[] chartKeys = new string[0];
        [SerializeField] private Sprite[] charts = new Sprite[0];
        [SerializeField] private string[] sceneKeys = new string[0];
        [SerializeField] private Sprite[] scenes = new Sprite[0];
        [SerializeField] private string[] sandboxTips = new string[0];
        [SerializeField] private string[] invasionTips = new string[0];

        /// <summary>A loading screen is up (a new load waits for it).</summary>
        public static bool Busy => _current != null;

        private static LoadingScreen _current;

        private Look _look;
        private float _shown;
        private string[] _tips;
        private int _tip;

        /// <summary>Into a game: the look and its picture from what the menu set up (GameSessionIntent).</summary>
        public static void IntoGame(string sceneName)
        {
            if (GameSessionIntent.NewGameIsInvasion)
            {
                Load(sceneName, Look.Invasion, MapThemes.Id(GameSettings.InvasionTheme), null);
                return;
            }
            var mapId = GameSessionIntent.NewGameMapId;
            if (string.IsNullOrEmpty(mapId) && !string.IsNullOrEmpty(GameSessionIntent.SaveNameToLoad))
            {
                var save = SaveSystem.Load(GameSessionIntent.SaveNameToLoad);
                if (save != null) mapId = save.mapId;
            }
            Load(sceneName, Look.Sandbox, mapId, mapId);
        }

        /// <summary>Out to the menu: the look of the game being left.</summary>
        public static void OutToMenu(string sceneName)
        {
            if (Invasion.InvasionMode.IsActive)
            {
                var theme = Invasion.InvasionMode.IsNewYear ? MapTheme.NewYear : ThemeOf(SeasonStyle.Session);
                Load(sceneName, Look.Invasion, MapThemes.Id(theme), null);
                return;
            }
            var mapId = MeshMapApplier.Instance != null ? MeshMapApplier.Instance.CurrentMapId : null;
            Load(sceneName, Look.Sandbox, mapId, mapId);
        }

        private static MapTheme ThemeOf(MapSeason season)
        {
            foreach (var theme in MapThemes.All)
            {
                if (theme != MapTheme.NewYear && MapThemes.SeasonOf(theme) == season) return theme;
            }
            return MapTheme.Summer;
        }

        /// <summary>Loads `sceneName` behind the drawn screen; without the prefab (a test scene), loads it plainly.</summary>
        public static void Load(string sceneName, Look look, string pictureKey, string mapId)
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
            _current.Begin(sceneName, look, pictureKey, mapId);
        }

        private void Begin(string sceneName, Look look, string pictureKey, string mapId)
        {
            _look = look;
            sandboxRoot.SetActive(look == Look.Sandbox);
            invasionRoot.SetActive(look == Look.Invasion);
            if (look == Look.Sandbox)
            {
                var sprite = Find(chartKeys, charts, pictureKey) ?? (charts.Length > 0 ? charts[0] : null);
                chart.sprite = sprite;
                if (chartFaint != null) chartFaint.sprite = sprite;
                chart.fillAmount = 0f;
                mapName.text = string.IsNullOrEmpty(mapId) ? string.Empty : Localization.Get(MapSelectPanelController.NameKey(mapId));
                mapName.color = new Color(mapName.color.r, mapName.color.g, mapName.color.b, 0f);
            }
            else
            {
                scene.sprite = Find(sceneKeys, scenes, pictureKey) ?? (scenes.Length > 0 ? scenes[0] : null);
                plankFill.fillAmount = 0f;
            }
            _tips = look == Look.Sandbox ? sandboxTips : invasionTips;
            _tip = _tips.Length > 0 ? Random.Range(0, _tips.Length) : 0;
            ShowTip();
            group.alpha = 0f;
            StartCoroutine(Run(sceneName));
        }

        private static Sprite Find(string[] keys, Sprite[] sprites, string key)
        {
            for (var i = 0; i < keys.Length && i < sprites.Length; i++)
            {
                if (keys[i] == key) return sprites[i];
            }
            return null;
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
            var shown = 0f;
            var load = SceneManager.LoadSceneAsync(sceneName);
            var readyFor = 0f;
            var started = Time.realtimeSinceStartup;
            while (true)
            {
                var loaded = load == null || load.isDone;
                var ready = loaded && (SceneReady(sceneName) || readyFor > MaxReadySeconds);
                if (loaded) readyFor += Time.unscaledDeltaTime;
                var elapsed = Time.realtimeSinceStartup - started;
                // the picture is drawn over at least MinSeconds, and only finishes when the scene is up
                var target = Mathf.Min(elapsed / MinSeconds, ready ? 1f : 0.9f * (load != null ? Mathf.Clamp01(load.progress / 0.9f) : 1f) + (loaded ? 0.05f : 0f));
                shown = Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime * 0.9f);
                Draw(shown);
                if (ready && shown >= 0.999f) break;
                yield return null;
            }
            // the map's name, then out
            for (var t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                if (_look == Look.Sandbox) SetAlpha(mapName, t / 0.5f);
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

        /// <summary>A game is up once its map is laid out; the menu, at once.</summary>
        private static bool SceneReady(string sceneName)
        {
            if (SceneManager.GetActiveScene().name != sceneName) return false;
            var applier = MeshMapApplier.Instance;
            return applier == null || applier.CurrentMap != null;
        }

        private void Draw(float progress)
        {
            if (_look == Look.Sandbox)
            {
                chart.fillAmount = progress;
                // the quill rides the drawn edge, its nib bobbing as it writes
                var rect = chart.rectTransform.rect;
                var x = rect.xMin + rect.width * progress;
                var y = Mathf.Sin(Time.unscaledTime * 9f) * rect.height * 0.18f + Mathf.Sin(Time.unscaledTime * 2.3f) * rect.height * 0.2f;
                quill.anchoredPosition = new Vector2(x, y);
                quill.gameObject.SetActive(progress < 0.999f);
            }
            else
            {
                plankFill.fillAmount = progress;
                var rect = plank.rect;
                var rr = runner.rectTransform;
                rr.anchoredPosition = new Vector2(rect.xMin + rect.width * progress, rr.anchoredPosition.y);
                if (runnerFrames.Length > 0) runner.sprite = runnerFrames[(int)(Time.unscaledTime * 8f) % runnerFrames.Length];
            }
            _shown += Time.unscaledDeltaTime;
            if (_shown >= TipSeconds && _tips.Length > 1)
            {
                _shown = 0f;
                _tip = (_tip + 1) % _tips.Length;
                ShowTip();
            }
        }

        private void ShowTip()
        {
            if (tip != null) tip.text = _tips.Length > 0 ? Localization.Get(_tips[_tip]) : string.Empty;
        }

        private static void SetAlpha(Text text, float a)
        {
            var c = text.color;
            text.color = new Color(c.r, c.g, c.b, a);
        }
    }
}
