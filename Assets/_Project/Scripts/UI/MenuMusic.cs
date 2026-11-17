using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// The main menu's music (the user, 2026-11-17: calm music in the main menu and its screens -- modes, sandbox,
    /// invasion -- «medieval-exploration», RandomMind, CC0; Audio/Music/, credits in Audio/Campaign/CREDITS.md): one
    /// streamed track on a loop, faded in, its loudness the sheet's (menu_music_volume). The scene's own: it stops
    /// when the game is entered. Built by SetupProjectMainMenu.
    /// </summary>
    public class MenuMusic : MonoBehaviour
    {
        [SerializeField] private AudioClip track;

        private AudioSource _source;
        private float _t;

        private void Start()
        {
            if (track == null) { enabled = false; return; }
            _source = gameObject.AddComponent<AudioSource>();
            _source.clip = track;
            _source.loop = true;
            _source.spatialBlend = 0f;
            _source.playOnAwake = false;
            _source.ignoreListenerPause = true;
            _source.volume = 0f;
            _source.Play();
        }

        private void Update()
        {
            var config = BalanceConfig.Instance;
            var target = config != null ? config.MenuMusicVolume : 0.4f;
            var fade = config != null ? Mathf.Max(0.01f, config.MenuMusicFadeSeconds) : 2f;
            _t += Time.unscaledDeltaTime;
            _source.volume = target * Mathf.Clamp01(_t / fade);
            if (_t > fade) enabled = false;
        }
    }
}
