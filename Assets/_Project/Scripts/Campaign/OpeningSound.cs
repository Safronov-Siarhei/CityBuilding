using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// The opening film's sound (the user, 2026-11-11: free CC0 sounds and music): the "sounds" cue
    /// sheet of Campaign/OpeningScript.json, played on the film's clock (the director steps it). Each
    /// cue starts a clip at its time with its volume, optional length (cut short), fades and loop.
    /// The same sheet makes the mp4's soundtrack (Tools/blender/cinematic/encode_frames.py), so the
    /// film and the game sound alike. Only sounds in play mode: the editor's frame-by-frame film is mute.
    /// </summary>
    public class OpeningSound : MonoBehaviour
    {
        [Serializable] private class File { public Cue[] sounds; }

        [Serializable] private class Cue
        {
            public float t;
            public string clip;
            public float volume = 1f;
            public float length;
            public float fadeIn;
            public float fadeOut;
            public bool loop;
        }

        [SerializeField] private TextAsset scriptFile;
        [SerializeField] private AudioClip[] clips = new AudioClip[0];

        private Cue[] _cues = new Cue[0];
        private readonly Dictionary<Cue, AudioSource> _playing = new Dictionary<Cue, AudioSource>();
        private readonly HashSet<Cue> _done = new HashSet<Cue>();
        private readonly Stack<AudioSource> _free = new Stack<AudioSource>();
        private readonly List<Cue> _ended = new List<Cue>();

        public void Begin()
        {
            foreach (var s in _playing.Values) Release(s);
            _playing.Clear();
            _done.Clear();
            var file = JsonUtility.FromJson<File>(scriptFile.text);
            _cues = file?.sounds ?? new Cue[0];
        }

        public void Step(float time)
        {
            if (!Application.isPlaying) return;
            foreach (var c in _cues)
            {
                if (time < c.t || _done.Contains(c) || _playing.ContainsKey(c)) continue;
                var clip = Find(c.clip);
                _done.Add(c);
                if (clip == null) continue;
                var src = _free.Count > 0 ? _free.Pop() : gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                src.clip = clip;
                src.loop = c.loop;
                src.time = Mathf.Clamp(time - c.t, 0f, clip.length - 0.01f);
                src.volume = 0f;
                src.Play();
                _playing[c] = src;
            }
            _ended.Clear();
            foreach (var kv in _playing)
            {
                var c = kv.Key;
                var src = kv.Value;
                var u = time - c.t;
                var len = c.length > 0f ? c.length : (c.loop ? float.MaxValue : src.clip.length);
                if (u >= len || (!c.loop && !src.isPlaying))
                {
                    _ended.Add(c);
                    continue;
                }
                var w = 1f;
                if (c.fadeIn > 0f) w *= Mathf.Clamp01(u / c.fadeIn);
                if (c.fadeOut > 0f && len < float.MaxValue) w *= Mathf.Clamp01((len - u) / c.fadeOut);
                src.volume = c.volume * w;
            }
            foreach (var c in _ended)
            {
                Release(_playing[c]);
                _playing.Remove(c);
            }
        }

        /// <summary>Silences everything (the film skipped or ended).</summary>
        public void StopAll()
        {
            foreach (var s in _playing.Values) Release(s);
            _playing.Clear();
        }

        private void Release(AudioSource s)
        {
            if (s == null) return;
            s.Stop();
            s.clip = null;
            _free.Push(s);
        }

        private AudioClip Find(string name)
        {
            foreach (var c in clips) if (c != null && c.name == name) return c;
            return null;
        }
    }
}
