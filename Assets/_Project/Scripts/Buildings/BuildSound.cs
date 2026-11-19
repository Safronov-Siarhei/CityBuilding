using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// The building sounds in every mode (the user, 2026-11-17: «когда игрок ставит какое-то здание на карте - должен
    /// быть звук, при окончании его улучшения на карте тоже»): a wooden thump-knock when the player puts a building
    /// down (BuildingPlacer.PlaceAt -- roads and fences too, piece by piece; round 2: the user picked C, a deep thud with a puff of dust), a dust whoosh and a
    /// «ding-dong» when a new building is done on its timer, a rising chime when a player's upgrade is done (both
    /// ConstructionSite.Finish). Clips: Audio/Resources/Build/{place,upgraded} (our own, Tools/audio/build_sounds.py).
    /// One 2D source made once and kept; volume build_sfx_volume (sheet). A road laid piece after piece is not a rattle:
    /// the same sound no oftener than every 0.08 s.
    /// </summary>
    public static class BuildSound
    {
        private static AudioSource _source;
        private static AudioClip _place, _upgraded, _built, _reward, _roar;
        private static float _lastPlace = -1f;

        public static void Placed()
        {
            var now = Time.unscaledTime;
            if (now - _lastPlace < 0.08f) return;
            _lastPlace = now;
            Play(ref _place, "Build/place", Random.Range(0.94f, 1.06f));
        }

        public static void Upgraded() => Play(ref _upgraded, "Build/upgraded", 1f);

        /// <summary>A new building done on its timer, with the dust of BuildFinishEffect (the user, 2026-11-17).</summary>
        public static void Built() => Play(ref _built, "Build/built", 1f);

        /// <summary>A reward taken -- the daily bonus (the user, 2026-11-18): coins clinking and a rising sparkle.</summary>
        public static void Reward() => Play(ref _reward, "Build/reward", 1f);

        /// <summary>The orcs' roar as an Invasion wave comes out of the portals (the user, 2026-11-19; mission 2's sfx_roar), volume inv_roar_volume.</summary>
        public static void WaveRoar() =>
            Play(ref _roar, "Invasion/roar", Random.Range(0.92f, 1.06f), BalanceConfig.Instance != null ? BalanceConfig.Instance.InvasionRoarVolume : 0.8f);

        private static void Play(ref AudioClip clip, string path, float pitch) =>
            Play(ref clip, path, pitch, BalanceConfig.Instance != null ? BalanceConfig.Instance.BuildSfxVolume : 0.7f);

        private static void Play(ref AudioClip clip, string path, float pitch, float volume)
        {
            if (volume <= 0f) return;
            if (clip == null) clip = UnityEngine.Resources.Load<AudioClip>(path);
            if (clip == null) return;
            if (_source == null)
            {
                var host = new GameObject("BuildSound");
                Object.DontDestroyOnLoad(host);
                _source = host.AddComponent<AudioSource>();
                _source.playOnAwake = false;
                _source.spatialBlend = 0f;
                _source.ignoreListenerPause = true;   // the reward windows open over a paused game
            }
            _source.pitch = pitch;
            _source.PlayOneShot(clip, volume * GameSettings.SfxVolume);
        }
    }
}
