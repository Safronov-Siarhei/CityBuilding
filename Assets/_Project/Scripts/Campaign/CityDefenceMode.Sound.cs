using UnityEngine;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Mission 2's sound (the user's thirteenth playtest: «не хватает звукового сопровождения»): a music loop under the
    /// whole mission (the project's mus_attack) and the orcs' roar (sfx_roar) as each wave sets off. Volumes are on the
    /// sheet (m2_music_volume, m2_roar_volume).
    /// </summary>
    public partial class CityDefenceMode
    {
        [SerializeField] private AudioClip music;   // Audio/Campaign/mus_attack.ogg
        [SerializeField] private AudioClip roar;    // Audio/Campaign/sfx_roar.ogg

        private AudioSource _music, _roar;

        private void StartSound()
        {
            if (music != null && _music == null)
            {
                _music = gameObject.AddComponent<AudioSource>();
                _music.clip = music;
                _music.loop = true;
                _music.spatialBlend = 0f;
                _music.volume = B.musicVolume;
                _music.Play();
            }
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
            if (_music != null) _music.Stop();
        }
    }
}
