using UnityEngine;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Plays a cutscene figure's baked clips (CineClips) on its bones, crossfading from one clip to the
    /// next. Ticked by whoever owns the figure (OpeningLife, the director), so a scene can be stepped
    /// by hand in the editor exactly as it plays.
    /// </summary>
    public class CineFigure : MonoBehaviour
    {
        [SerializeField] private CineClips clips;

        private Transform[] _bones;
        private CineClips.Clip _clip;
        private CineClips.Clip _from;
        private float _time;
        private float _fromTime;
        private float _fade;
        private float _fadeSeconds;
        private float _rate = 1f;
        private float _fromRate = 1f;

        public CineClips Clips => clips;
        public string Current => _clip != null ? _clip.name : null;
        public float ClipTime => _time;

        public void Init(CineClips set)
        {
            clips = set;
            _bones = null;
        }

        /// <summary>Starts a clip, faded in over `fade` seconds from whatever played; `rate` scales its
        /// speed (a walk slowed to the pace it moves at). Playing the current clip again keeps its time.</summary>
        public void Play(string clipName, float fade = 0.2f, float rate = 1f, float startTime = 0f)
        {
            if (clips == null) return;
            var next = clips.Find(clipName);
            if (next == null) return;
            if (next == _clip)
            {
                _rate = rate;
                return;
            }
            _from = _clip;
            _fromTime = _time;
            _fromRate = _rate;
            _clip = next;
            _time = startTime;
            _rate = rate;
            _fadeSeconds = _from != null ? fade : 0f;
            _fade = 0f;
        }

        public void Tick(float dt)
        {
            if (_clip == null) return;
            if (_bones == null) Bind();
            _time += dt * _rate;
            _fromTime += dt * _fromRate;
            _fade += dt;
            var w = _fadeSeconds > 0f ? Mathf.Clamp01(_fade / _fadeSeconds) : 1f;
            if (w >= 1f) _from = null;
            for (var b = 0; b < _bones.Length; b++)
            {
                if (_bones[b] == null) continue;
                var q = Rotation(_clip, _time, b);
                if (_from != null) q = Quaternion.Slerp(Rotation(_from, _fromTime, b), q, w);
                _bones[b].localRotation = q;
            }
            for (var m = 0; m < clips.movingBones.Length; m++)
            {
                var t = _bones[clips.movingBones[m]];
                if (t == null) continue;
                var p = Position(_clip, _time, m);
                if (_from != null) p = Vector3.Lerp(Position(_from, _fromTime, m), p, w);
                t.localPosition = p;
            }
        }

        /// <summary>True once a clip that does not loop has played to its end.</summary>
        public bool Finished => _clip != null && !_clip.loop && _time >= _clip.seconds;

        private void Bind()
        {
            _bones = new Transform[clips.bones.Length];
            for (var i = 0; i < _bones.Length; i++) _bones[i] = transform.Find(clips.bones[i]);
        }

        private static void Frame(CineClips.Clip c, float time, out int a, out int b, out float f)
        {
            var span = c.frames - 1;
            var x = c.seconds > 0f ? time / c.seconds : 0f;
            x = c.loop ? x - Mathf.Floor(x) : Mathf.Clamp01(x);
            var at = x * span;
            a = Mathf.Min(span, (int)at);
            b = Mathf.Min(span, a + 1);
            f = at - a;
        }

        private Quaternion Rotation(CineClips.Clip c, float time, int bone)
        {
            Frame(c, time, out var a, out var b, out var f);
            var n = clips.bones.Length * 4;
            var i = a * n + bone * 4;
            var j = b * n + bone * 4;
            var qa = new Quaternion(c.rot[i], c.rot[i + 1], c.rot[i + 2], c.rot[i + 3]);
            var qb = new Quaternion(c.rot[j], c.rot[j + 1], c.rot[j + 2], c.rot[j + 3]);
            return Quaternion.Slerp(qa, qb, f);
        }

        private Vector3 Position(CineClips.Clip c, float time, int moving)
        {
            Frame(c, time, out var a, out var b, out var f);
            var n = clips.movingBones.Length * 3;
            var i = a * n + moving * 3;
            var j = b * n + moving * 3;
            return Vector3.Lerp(new Vector3(c.pos[i], c.pos[i + 1], c.pos[i + 2]), new Vector3(c.pos[j], c.pos[j + 1], c.pos[j + 2]), f);
        }
    }
}
