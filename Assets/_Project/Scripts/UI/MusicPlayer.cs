using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// The background music (the user, 2026-11-17): quiet, «просто слегка на фоне»; every track swells in at its start
    /// and fades out at its end, and a PAUSE of silence follows it «чтобы слух игрока отдохнул». Tracks (Audio/Music/,
    /// CC0, credits in Audio/Campaign/CREDITS.md), the user's choice:
    ///   main menu  -- mus_exploration;
    ///   tutorial   -- mus_the_bards_tale;
    ///   sandbox    -- mus_harvest_season and mus_market_day by turns;
    ///   invasion   -- mus_kings_feast, and while a wave is under way the campaign's battle track (mus_mission1),
    ///                 the switch a quick fade out, a short breath, the other track faded in.
    /// The campaign plays its own music: in a mission (CityDefenceMode, MissionOne) this one keeps silent.
    /// Loudness = music_volume (sheet) x the player's «Музыка» slider (GameSettings.MusicVolume); the envelope times
    /// are music_fade_in_sec, music_fade_out_sec, music_pause_sec, music_switch_sec. One 2D streamed source.
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        public enum Place { Menu, Game }

        [SerializeField] private Place place = Place.Game;
        [SerializeField] private AudioClip menuTrack;
        [SerializeField] private AudioClip tutorialTrack;
        [SerializeField] private AudioClip[] sandboxTracks = new AudioClip[0];
        [SerializeField] private AudioClip invasionTrack;
        [SerializeField] private AudioClip battleTrack;

        private AudioSource _source;
        private bool _campaign;
        private AudioClip[] _list;
        private int _next;
        private float _pauseLeft;
        private float _switchFade = 1f;     // 1 = full; goes to 0 when the list changes, then the new list starts
        private bool _switching;
        private AudioClip[] _wanted;

        /// <summary>The track playing now (null in a pause) -- for the tests.</summary>
        public AudioClip Playing => _source != null && _source.isPlaying ? _source.clip : null;

        private void Start()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.loop = false;
            _source.spatialBlend = 0f;
            _source.playOnAwake = false;
            _source.ignoreListenerPause = true;
            _source.volume = 0f;
            _campaign = FindAnyObjectByType<Campaign.MissionOne>() != null;
            _pauseLeft = 0.5f;   // a breath before the first note, while the scene settles
        }

        private AudioClip[] WantedList()
        {
            if (place == Place.Menu) return One(menuTrack);
            if (_campaign || Campaign.CityDefenceMode.IsActive) return null;
            if (Tutorial.TutorialMode.IsActive) return One(tutorialTrack);
            var invasion = Invasion.InvasionMode.Instance;
            if (Invasion.InvasionMode.IsActive)
            {
                var underway = invasion.Waves != null && invasion.Waves.Current == Invasion.InvasionWaves.Phase.Underway;
                return One(underway && battleTrack != null ? battleTrack : invasionTrack);
            }
            return sandboxTracks;
        }

        private AudioClip[] _one;
        private AudioClip[] One(AudioClip clip)
        {
            if (clip == null) return null;
            if (_one == null || _one[0] != clip) _one = new[] { clip };
            return _one;
        }

        private void Update()
        {
            var config = BalanceConfig.Instance;
            var dt = Time.unscaledDeltaTime;
            var fadeIn = config != null ? Mathf.Max(0.01f, config.MusicFadeInSeconds) : 4f;
            var fadeOut = config != null ? Mathf.Max(0.01f, config.MusicFadeOutSeconds) : 5f;
            var pause = config != null ? Mathf.Max(0f, config.MusicPauseSeconds) : 20f;
            var switchTime = config != null ? Mathf.Max(0.01f, config.MusicSwitchSeconds) : 2f;

            var wanted = WantedList();
            if (wanted != _list && !SameList(wanted, _list))
            {
                if (!_source.isPlaying) Begin(wanted, 0.5f);          // in a pause: the new list after a breath
                else if (!_switching || !SameList(wanted, _wanted))
                {
                    _switching = true;
                    _wanted = wanted;
                }
            }
            else if (_switching)
            {
                _switching = false;   // changed its mind back before the fade ended: swell again
            }

            if (_switching)
            {
                _switchFade -= dt / switchTime;
                if (_switchFade <= 0f)
                {
                    _source.Stop();
                    _switching = false;
                    Begin(_wanted, Mathf.Min(pause, 2f));
                }
            }
            else _switchFade = Mathf.Min(1f, _switchFade + dt / switchTime);

            if (!_source.isPlaying)
            {
                if (_list == null || _list.Length == 0) return;
                _pauseLeft -= dt;
                if (_pauseLeft > 0f) return;
                var clip = _list[_next % _list.Length];
                _next++;
                if (clip == null) return;
                _source.clip = clip;
                _source.time = 0f;
                _source.Play();
                _pauseLeft = clip == battleTrack ? 0.3f : pause;   // the battle goes on without a gap while the wave lasts
            }

            var length = _source.clip != null ? _source.clip.length : 1f;
            var t = _source.time;
            var envelope = Mathf.Clamp01(Mathf.Min(t / fadeIn, (length - t) / fadeOut));
            var loud = (config != null ? config.MusicVolume : 0.3f) * GameSettings.MusicVolume;
            _source.volume = loud * envelope * Mathf.Clamp01(_switchFade);
        }

        private void Begin(AudioClip[] list, float breath)
        {
            _list = list;
            _next = 0;
            _pauseLeft = breath;
            _switchFade = 1f;
        }

        private static bool SameList(AudioClip[] a, AudioClip[] b)
        {
            if (a == b) return true;
            if (a == null || b == null || a.Length != b.Length) return false;
            for (var i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }
    }
}
