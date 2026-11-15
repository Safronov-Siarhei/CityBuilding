using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// The fight's sounds in every mode (the user, 2026-11-15: «звуки выстрелов для арбалетов и луков и башен + звуки
    /// ударов мечей, чтобы бои были не такими скучными»): a bow's or a tower's twang, a crossbow's, a javelin's, a
    /// catapult's throw, an arrow's clink where it lands and the clash of blades. Clips from
    /// Audio/Resources/Combat/&lt;kind&gt;_N (CC0, Audio/Campaign/CREDITS.md).
    ///
    /// Weighed for phones: a small pool of 2D sources (combat_sfx_voices) taken in turn, nothing heard off screen or
    /// past combat_sfx_hear_range from the screen's centre on the ground (quieter towards it), and one kind no oftener
    /// than combat_sfx_gap_sec -- a battle of fifty is a battle, not a rattle. No allocation per call.
    /// </summary>
    public static class CombatSound
    {
        public enum Kind { Bow = 0, Crossbow = 1, Javelin = 2, Catapult = 3, ArrowHit = 4, Clash = 5 }

        private static readonly string[] Names = { "bow", "crossbow", "javelin", "catapult", "arrowhit", "clash" };
        private static AudioClip[][] _clips;
        private static readonly float[] Last = new float[6];
        private static AudioSource[] _voices;
        private static int _next;
        private static GameObject _host;

        public static void Shot(ProjectileKind kind, Vector3 at, SoldierUnit attacker)
        {
            var k = kind == ProjectileKind.Cannonball ? Kind.Catapult
                : kind == ProjectileKind.Javelin ? Kind.Javelin
                : attacker != null && (attacker.Type == SoldierType.Crossbowman || attacker.Type == SoldierType.GnomeCrossbow) ? Kind.Crossbow
                : Kind.Bow;
            Play(k, at, 1f);
        }

        public static void Landed(ProjectileKind kind, Vector3 at)
        {
            if (kind != ProjectileKind.Arrow) return;
            var config = BalanceConfig.Instance;
            if (config != null && Random.value > config.CombatSfxHitShare) return;
            Play(Kind.ArrowHit, at, 0.6f);
        }

        public static void Clash(Vector3 at) => Play(Kind.Clash, at, 1f);

        private static void Play(Kind kind, Vector3 at, float scale)
        {
            var config = BalanceConfig.Instance;
            if (config == null || config.CombatSfxVolume <= 0f) return;
            var now = Time.unscaledTime;
            var i = (int)kind;
            if (now - Last[i] < config.CombatSfxGapSeconds) return;
            var cam = Camera.main;
            if (cam == null) return;
            var vp = cam.WorldToViewportPoint(at);
            if (vp.z <= 0f || vp.x < -0.1f || vp.x > 1.1f || vp.y < -0.1f || vp.y > 1.1f) return;
            // the screen's centre on the ground, at the sound's height
            var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            var centre = ray.direction.y < -0.01f ? ray.origin + ray.direction * ((at.y - ray.origin.y) / ray.direction.y) : at;
            var dx = at.x - centre.x;
            var dz = at.z - centre.z;
            var range = Mathf.Max(1f, config.CombatSfxHearRange);
            var near = 1f - Mathf.Sqrt(dx * dx + dz * dz) / range;
            if (near <= 0f) return;
            var clips = Clips(kind);
            if (clips == null || clips.Length == 0) return;
            var voice = Voice(config.CombatSfxVoices);
            if (voice == null) return;
            Last[i] = now;
            // Play, not PlayOneShot: a voice taken in turn cuts its old sound, so never more than `voices` at once
            voice.clip = clips[Random.Range(0, clips.Length)];
            voice.volume = config.CombatSfxVolume * scale * (0.35f + 0.65f * near);
            voice.pitch = Random.Range(0.92f, 1.08f);
            voice.Play();
        }

        private static AudioClip[] Clips(Kind kind)
        {
            if (_clips == null)
            {
                _clips = new AudioClip[Names.Length][];
                var all = UnityEngine.Resources.LoadAll<AudioClip>("Combat");
                for (var k = 0; k < Names.Length; k++)
                {
                    var count = 0;
                    foreach (var c in all) if (c.name.StartsWith(Names[k] + "_")) count++;
                    var list = new AudioClip[count];
                    count = 0;
                    foreach (var c in all) if (c.name.StartsWith(Names[k] + "_")) list[count++] = c;
                    _clips[k] = list;
                }
            }
            return _clips[(int)kind];
        }

        /// <summary>The next voice in turn; a pool made once per scene (the host dies with it).</summary>
        private static AudioSource Voice(int voices)
        {
            if (_host == null)
            {
                _host = new GameObject("CombatSound");
                voices = Mathf.Clamp(voices, 1, 32);
                _voices = new AudioSource[voices];
                for (var v = 0; v < voices; v++)
                {
                    var s = _host.AddComponent<AudioSource>();
                    s.playOnAwake = false;
                    s.spatialBlend = 0f;
                    _voices[v] = s;
                }
                _next = 0;
            }
            var voice = _voices[_next];
            _next = (_next + 1) % _voices.Length;
            return voice;
        }
    }
}
