using UnityEngine;
using UnityEngine.SceneManagement;

namespace CityBuilder.Core
{
    /// <summary>
    /// The player's total time in the game, summed over every launch (the user, 2026-11-17: the news window opens by
    /// itself only for a player with enough hours behind them, news_auto_open_hours). Counts real seconds outside the
    /// main menu while the app is in front; kept on the device (PlayerPrefs), written every half minute and when the
    /// app goes to the background or quits -- never per frame.
    /// </summary>
    public static class PlayTime
    {
        private const string Key = "playtime.seconds";
        private const string MenuScene = "MainMenu";
        private const float SaveEvery = 30f;

        private static float _unsaved;

        /// <summary>Seconds played in total, this launch included.</summary>
        public static double Seconds => PlayerPrefs.GetInt(Key, 0) + (double)_unsaved;

        public static double Hours => Seconds / 3600.0;

        private static void Flush()
        {
            if (_unsaved < 1f) return;
            var whole = Mathf.FloorToInt(_unsaved);
            PlayerPrefs.SetInt(Key, PlayerPrefs.GetInt(Key, 0) + whole);
            PlayerPrefs.Save();
            _unsaved -= whole;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            _unsaved = 0f;
            var go = new GameObject("PlayTime");
            go.hideFlags = HideFlags.HideInHierarchy;
            Object.DontDestroyOnLoad(go);
            go.AddComponent<Ticker>();
        }

        private sealed class Ticker : MonoBehaviour
        {
            private float _sinceSave;
            private bool _inMenu;

            private void OnEnable() => SceneManager.activeSceneChanged += OnScene;
            private void OnDisable() => SceneManager.activeSceneChanged -= OnScene;
            private void Start() => _inMenu = SceneManager.GetActiveScene().name == MenuScene;

            private void OnScene(Scene from, Scene to)
            {
                _inMenu = to.name == MenuScene;
                Flush();
            }

            private void Update()
            {
                if (_inMenu) return;
                var dt = Mathf.Min(Time.unscaledDeltaTime, 1f);   // a hitch or a debugger stop is not play
                _unsaved += dt;
                _sinceSave += dt;
                if (_sinceSave < SaveEvery) return;
                _sinceSave = 0f;
                Flush();
            }

            private void OnApplicationPause(bool paused)
            {
                if (paused) Flush();
            }

            private void OnApplicationQuit() => Flush();
        }
    }
}
