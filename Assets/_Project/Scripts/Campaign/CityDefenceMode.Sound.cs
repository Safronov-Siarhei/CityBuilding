using UnityEngine;
using CityBuilder.UI;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Mission 2's sound (the user's thirteenth playtest: «не хватает звукового сопровождения»): a music loop under the
    /// whole mission (mus_mission1, looped: the user, 2026-11-15) and the orcs' roar (sfx_roar) as each wave sets off. Volumes are on the
    /// sheet (m2_music_volume, m2_roar_volume).
    /// </summary>
    public partial class CityDefenceMode
    {
        [SerializeField] private AudioClip music;   // Audio/Campaign/mus_mission1.ogg
        [SerializeField] private AudioClip roar;    // Audio/Campaign/sfx_roar.ogg

        private AudioSource _music, _roar;
        // the siege's music is WANTED from the mission's start, but it sounds only once the loading screen is gone and
        // the opening scene is over (the user, twice: «музыка всё ещё начинается во время загрузочного экрана») --
        // TickMusic starts it, every frame, never a Play in Setup
        private bool _musicWanted;

        private void StartSound()
        {
            if (music != null && _music == null)
            {
                _music = gameObject.AddComponent<AudioSource>();
                _music.clip = music;
                _music.loop = true;
                _music.spatialBlend = 0f;
                _music.volume = B.musicVolume;
                _music.playOnAwake = false;
            }
            _musicWanted = true;
            if (roar != null && _roar == null)
            {
                _roar = gameObject.AddComponent<AudioSource>();
                _roar.spatialBlend = 0f;
            }
        }

        private void RoarForWave()
        {
            if (_roar == null || roar == null) return;
            _roar.pitch = Random.Range(0.92f, 1.06f);
            _roar.PlayOneShot(roar, B.roarVolume);
        }

        /// <summary>The music goes quiet with the mission's end.</summary>
        private void StopMusic()
        {
            _musicWanted = false;
            if (_music != null) _music.Stop();
        }

        /// <summary>Every frame: the music plays only when wanted, with no loading screen up and no film running.</summary>
        private void TickMusic()
        {
            if (_music == null) return;
            var may = _musicWanted && _intro == null && _capital == null && !LoadingScreen.Busy;
            if (may && !_music.isPlaying) _music.Play();
            else if (!may && _music.isPlaying) _music.Pause();
        }
    }
}
