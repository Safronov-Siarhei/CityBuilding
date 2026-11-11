using System;
using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Plays the campaign's opening film in the village (the user, 2026-11-10: the Blender film moves
    /// into Unity and is finished there). One world, one clock: every figure stays where the story
    /// left it, so a shot's background is never empty.
    ///
    /// Campaign/OpeningScript.json is the film: the shots (camera keys, lens in mm), the cast, and
    /// the cues on the clock -- a figure plays one of its approved performances (its root follows
    /// the performance's path from Campaign/OpeningTracks.json, exported from Blender with it), or
    /// runs a path, or stands; set pieces follow their tracks (the cart, the toppled stall, the
    /// stones flying into the arch, the swirl); the lights follow theirs; subtitles come from the
    /// localization sheet; the living background (OpeningLife) panics and lets the orcs loose.
    /// Step(dt) runs it, so the editor can render it frame by frame.
    /// </summary>
    public class OpeningDirector : MonoBehaviour
    {
        // ------------------------------------------------------------ the files

        [Serializable] private class Script
        {
            public float end;
            public LensInfo lens;
            public LightInfo lights;
            public SackInfo sack;
            public RopeInfo rope;
            public SubInfo subtitle;
            public ActorInfo[] actors;
            public Shot[] shots;
            public Cue[] cues;
        }

        [Serializable] private class LensInfo { public float sensorWidth; public float sensorHeight; }

        [Serializable] private class LightInfo
        {
            public float amuletScale, amuletRange;
            public float[] amuletColor;
            public float runesScale, runesRange;
            public float[] runesColor, runesAt;
            public float flashScale, flashRange;
            public float[] flashColor, flashAt;
            public float swirlScale, swirlRange;
            public float[] swirlColor;
        }

        [Serializable] private class SackInfo { public float vx, vy, vz, gravity, spinX, spinZ; }
        [Serializable] private class RopeInfo { public float width; }
        [Serializable] private class SubInfo { public float fadeIn, fadeOut; }
        [Serializable] private class ActorInfo { public string id; public string figure; public bool hidden; }

        [Serializable] private class Shot
        {
            public string name;
            public float start;
            public bool ease;
            public bool dof;
            public string follow;
            public Key[] keys;
        }

        [Serializable] private class Key
        {
            public float t;
            public float[] from;
            public float[] at;
            public float lens;
            public float shake;
        }

        [Serializable] private class Cue
        {
            public float t;
            public string evt;
            public string actor;
            public string perf;
            public string clip;
            public float[] at;
            public float[] face;
            public float yaw;
            public float speed;
            public float[] path;
            public bool hideAtEnd;
            public string track;
            public bool hold;
            public string who;
            public string key;
            public float seconds;
        }

        [Serializable] private class Tracks
        {
            public float fps;
            public FigureTrack[] figures;
            public ObjectTrack[] objects;
            public ValueTrack[] values;
            public PointTrack[] points;
        }

        [Serializable] private class FigureTrack { public string name; public int frames; public float[] root; }
        [Serializable] private class ObjectTrack { public string name; public string piece; public int frames; public float[] m; }
        [Serializable] private class ValueTrack { public string name; public float[] v; }
        [Serializable] private class PointTrack { public string name; public float[] p; }

        // ------------------------------------------------------------ the scene

        [SerializeField] private OpeningVillage village;
        [SerializeField] private OpeningLife life;
        [SerializeField] private TextAsset scriptFile;
        [SerializeField] private TextAsset tracksFile;
        [SerializeField] private Camera cam;
        [SerializeField] private Text subtitle;
        [SerializeField] private Material ropeMaterial;
        [SerializeField] private OpeningFx fx;
        [SerializeField] private OpeningSound sound;
        [SerializeField] private OpeningLife.CastMember[] cast = new OpeningLife.CastMember[0];
        [SerializeField] private bool autoPlay = true;

        /// <summary>Fired once when the film ends (the handover to mission 1).</summary>
        public event Action Finished;

        public float Time { get; private set; }
        public float Length => _s != null ? _s.end : 0f;
        public bool Playing { get; private set; }
        public string ShotName { get; private set; }

        private class Actor
        {
            public string id;
            public string figure;
            public Transform t;
            public CineFigure fig;
            public FigureTrack perf;
            public float t0;
            public List<Vector3> path;
            public float s;
            public float speed;
            public bool hideAtEnd;
        }

        private class Playing_
        {
            public string name;
            public float t0;
            public bool hold;
        }

        private Script _s;
        private Tracks _k;
        private readonly Dictionary<string, Actor> _actors = new Dictionary<string, Actor>();
        private readonly List<Playing_> _objects = new List<Playing_>();
        private readonly List<Playing_> _values = new List<Playing_>();
        private int _next;
        private Transform _root;
        private Light _amulet, _runes, _flash, _swirl;
        private Transform _rope;
        private float _ropeT0 = -1f;
        private Transform _sack;
        private Vector3 _sackVel;
        private float _subT0 = -1f, _subLen;
        private string _subText;
        private bool _finished;
        private Vector3 _camFrom, _camAt;
        private bool _shallow;

        private void Start()
        {
            if (!autoPlay) return;
            if (village.BuildingCount == 0) village.Build();
            Play();
        }

        private void Update()
        {
            // after the end the village goes on living under the mission (MissionOne): the orcs, the
            // fires, the smoke, the folk -- only the cues have run out and the camera is the mission's
            if (Application.isPlaying && (Playing || (_finished && KeepAlive))) Step(UnityEngine.Time.deltaTime);
        }

        /// <summary>Go on stepping the world once the film has ended (the mission plays in it).</summary>
        public bool KeepAlive { get; set; }

        /// <summary>The camera stops following the shots: the mission drives it from here on.</summary>
        public bool CameraReleased { get; set; }

        /// <summary>Takes an actor out of the film's hands (Ардан, for the player): the director no
        /// longer moves or animates it.</summary>
        public CineFigure Release(string id)
        {
            if (!_actors.TryGetValue(id, out var a)) return null;
            _actors.Remove(id);
            return a.fig;
        }

        /// <summary>Where the camera stands and looks, for the effects' depth of field and the storm dome.</summary>
        public void SetCameraAim(Vector3 from, Vector3 at)
        {
            _camFrom = from;
            _camAt = at;
        }

        public void Play()
        {
            _s = JsonUtility.FromJson<Script>(scriptFile.text);
            _k = JsonUtility.FromJson<Tracks>(tracksFile.text);
            if (_root != null) Kill(_root.gameObject);
            _root = new GameObject("Film").transform;
            _root.SetParent(transform, false);
            _actors.Clear();
            _objects.Clear();
            _values.Clear();
            _next = 0;
            _finished = false;
            _ropeT0 = -1f;
            _subT0 = -1f;
            _sack = null;
            Array.Sort(_s.cues, (a, b) => a.t.CompareTo(b.t));
            foreach (var a in _s.actors)
            {
                var member = Array.Find(cast, m => m.id == a.figure);
                if (member.model == null) continue;
                var go = Instantiate(member.model, _root);
                go.name = a.id;
                var fig = go.AddComponent<CineFigure>();
                fig.Init(member.clips);
                if (a.figure == "CineOrcTorch" && life != null) fig.LightTorch(life.FlameMaterial);
                fig.Play("Idle", 0f);
                go.SetActive(!a.hidden);
                _actors[a.id] = new Actor { id = a.id, figure = a.figure, t = go.transform, fig = fig };
            }
            var L = _s.lights;
            _amulet = MakeLight("AmuletLight", Col(L.amuletColor), L.amuletRange);
            _runes = MakeLight("RuneLight", Col(L.runesColor), L.runesRange);
            _runes.transform.position = V(L.runesAt);
            _flash = MakeLight("FlashLight", Col(L.flashColor), L.flashRange);
            _flash.transform.position = V(L.flashAt);
            _swirl = MakeLight("SwirlLight", Col(L.swirlColor), L.swirlRange);
            var swirlAt = Array.Find(_k.points, p => p.name == "swirl/light");
            if (swirlAt != null) _swirl.transform.position = new Vector3(swirlAt.p[0], swirlAt.p[1], swirlAt.p[2]);
            var swirlPiece = village.Piece("CinePortalSwirl");
            if (swirlPiece != null) swirlPiece.gameObject.SetActive(false);
            if (subtitle != null) subtitle.text = string.Empty;
            if (fx != null) fx.Begin();
            if (sound != null) sound.Begin();

            cam.usePhysicalProperties = true;
            cam.sensorSize = new Vector2(_s.lens.sensorWidth, _s.lens.sensorHeight);
            cam.gateFit = Camera.GateFitMode.Horizontal;

            if (life != null) life.External = true;
            Time = 0f;
            Playing = true;
            Step(0f);
        }

        public void Step(float dt)
        {
            if (_s == null) return;
            Time += dt;
            while (_next < _s.cues.Length && _s.cues[_next].t <= Time + 1e-4f) Fire(_s.cues[_next++]);
            foreach (var a in _actors.Values) StepActor(a, dt);
            foreach (var o in _objects) StepObject(o);
            foreach (var v in _values) StepValue(v);
            StepRope();
            StepSack(dt);
            if (life != null) life.Step(dt);
            if (!CameraReleased) StepCamera();
            StepSubtitle();
            if (fx != null) fx.Step(Time, dt, _camFrom, _camAt, cam.focalLength, _shallow);
            if (sound != null) sound.Step(Time);
            if (!_finished && Time >= _s.end)
            {
                _finished = true;
                Playing = false;
                if (sound != null) sound.StopAll();
                Finished?.Invoke();
            }
        }

        // ------------------------------------------------------------ cues

        private void Fire(Cue c)
        {
            if (!string.IsNullOrEmpty(c.actor))
            {
                if (!_actors.TryGetValue(c.actor, out var a)) return;
                a.t.gameObject.SetActive(true);
                a.perf = null;
                a.path = null;
                if (!string.IsNullOrEmpty(c.perf))
                {
                    a.perf = Array.Find(_k.figures, f => f.name == a.figure + "/" + c.perf);
                    a.t0 = c.t;
                    a.fig.Play(c.perf, 0.12f);
                    PlaceOnTrack(a, 0f);
                }
                else if (c.path != null && c.path.Length >= 6)
                {
                    a.path = new List<Vector3>();
                    for (var i = 0; i + 2 < c.path.Length; i += 3) a.path.Add(new Vector3(c.path[i], c.path[i + 1], c.path[i + 2]));
                    a.s = 0f;
                    a.speed = c.speed;
                    a.hideAtEnd = c.hideAtEnd;
                    var full = c.clip == "Run" ? a.fig.Clips.runSpeed : a.fig.Clips.walkSpeed;
                    a.fig.Play(c.clip, 0.2f, full > 0f && c.speed > 0f ? c.speed / full : 1f);
                    a.t.position = a.path[0];
                    Face(a.t, a.path[1] - a.path[0], 1f);
                }
                else
                {
                    if (c.at != null) a.t.position = V(c.at);
                    if (c.face != null) Face(a.t, V(c.face) - a.t.position, 1f);
                    else if (c.at != null) a.t.rotation = Quaternion.Euler(0f, c.yaw, 0f);
                    if (!string.IsNullOrEmpty(c.clip)) a.fig.Play(c.clip, 0.25f);
                }
                return;
            }
            switch (c.evt)
            {
                case "life":
                    if (life != null) life.Begin();
                    break;
                case "panic":
                    if (life != null) life.Panic(V(c.at));
                    break;
                case "orcs":
                    if (life != null) life.ReleaseOrcs(V(c.at));
                    break;
                case "ignite":
                    if (life != null) life.IgniteNearest(V(c.at));
                    break;
                case "objects":
                    _objects.RemoveAll(o => o.name == c.track);
                    _objects.Add(new Playing_ { name = c.track, t0 = c.t, hold = c.hold });
                    if (c.track == "swirl")
                    {
                        var sw = village.Piece("CinePortalSwirl");
                        // the effects draw the swirl themselves (a living vortex, not the flat disc)
                        if (sw != null && fx == null) sw.gameObject.SetActive(true);
                    }
                    break;
                case "values":
                    _values.RemoveAll(o => o.name == c.track);
                    _values.Add(new Playing_ { name = c.track, t0 = c.t });
                    break;
                case "rope":
                    _ropeT0 = c.t;
                    break;
                case "sack":
                    DropSack();
                    break;
                case "sub":
                    _subT0 = c.t;
                    _subLen = c.seconds;
                    _subText = Localization.Get(c.key);
                    break;
            }
        }

        // ------------------------------------------------------------ figures

        private void StepActor(Actor a, float dt)
        {
            if (!a.t.gameObject.activeSelf) return;
            if (a.perf != null) PlaceOnTrack(a, Time - a.t0);
            else if (a.path != null)
            {
                a.s += a.speed * dt;
                var left = a.s;
                var i = 0;
                for (; i + 1 < a.path.Count; i++)
                {
                    var seg = (a.path[i + 1] - a.path[i]).magnitude;
                    if (left <= seg) break;
                    left -= seg;
                }
                if (i + 1 >= a.path.Count)
                {
                    a.t.position = a.path[a.path.Count - 1];
                    a.path = null;
                    if (a.hideAtEnd) a.t.gameObject.SetActive(false);
                    else a.fig.Play("Idle", 0.3f);
                }
                else
                {
                    var d = a.path[i + 1] - a.path[i];
                    a.t.position = a.path[i] + d.normalized * left;
                    Face(a.t, d, Mathf.Clamp01(8f * dt));
                }
            }
            a.fig.Tick(dt);
        }

        private void PlaceOnTrack(Actor a, float time)
        {
            if (a.perf == null || a.perf.frames == 0) return;
            var x = Mathf.Clamp(time * _k.fps, 0f, a.perf.frames - 1);
            var i = Mathf.Min((int)x, a.perf.frames - 1);
            var j = Mathf.Min(i + 1, a.perf.frames - 1);
            var f = x - i;
            var r = a.perf.root;
            var p = Vector3.Lerp(new Vector3(r[i * 4], r[i * 4 + 1], r[i * 4 + 2]), new Vector3(r[j * 4], r[j * 4 + 1], r[j * 4 + 2]), f);
            a.t.SetPositionAndRotation(p, Quaternion.Euler(0f, Mathf.LerpAngle(r[i * 4 + 3], r[j * 4 + 3], f), 0f));
        }

        private static void Face(Transform t, Vector3 dir, float w)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f) return;
            t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(dir), w);   // a figure faces +Z
        }

        // ------------------------------------------------------------ set pieces, lights, rope, sack

        private void StepObject(Playing_ o)
        {
            var x = o.hold ? 0f : (Time - o.t0) * _k.fps;
            foreach (var tr in _k.objects)
            {
                if (!tr.name.StartsWith(o.name + "/")) continue;
                var piece = village.Piece(tr.piece);
                if (piece == null) continue;
                var fx = Mathf.Clamp(x, 0f, tr.frames - 1);
                var i = (int)fx;
                var j = Mathf.Min(i + 1, tr.frames - 1);
                var f = fx - i;
                piece.SetPositionAndRotation(
                    Vector3.Lerp(Pos(tr.m, i), Pos(tr.m, j), f),
                    Quaternion.Slerp(Rot(tr.m, i), Rot(tr.m, j), f));
            }
        }

        private void StepValue(Playing_ o)
        {
            var tr = Array.Find(_k.values, v => v.name == o.name);
            if (tr == null || tr.v.Length == 0) return;
            var x = Mathf.Clamp((Time - o.t0) * _k.fps, 0f, tr.v.Length - 1);
            var i = (int)x;
            var value = Mathf.Lerp(tr.v[i], tr.v[Mathf.Min(i + 1, tr.v.Length - 1)], x - i);
            var L = _s.lights;
            switch (o.name)
            {
                case "talk/amulet":
                    _amulet.intensity = value * L.amuletScale;
                    if (_actors.TryGetValue("ardan", out var ardan))
                    {
                        // the stone hangs at 0.335 of the model, the chest bone spans 0.27-0.39 and
                        // the stone sits 0.075 in front of it (ardan.py, ardan_anim.BONES)
                        var chest = FindBone(ardan.t, "chest");
                        var head = FindBone(ardan.t, "head");
                        if (chest != null && head != null)
                        {
                            var span = head.position - chest.position;
                            _amulet.transform.position = chest.position + span * 0.54f + ardan.t.forward * (span.magnitude * 0.62f);
                        }
                        if (fx != null) fx.AmuletGlow(_amulet.transform.position, value, cam);
                    }
                    break;
                case "talk/runes":
                    _runes.intensity = value * L.runesScale;
                    break;
                case "assembly/flash":
                    _flash.intensity = value * L.flashScale;
                    break;
                case "swirl/light":
                    _swirl.intensity = value * L.swirlScale;
                    break;
                case "bell/bell":
                    Swing("CineBell", value);
                    break;
                case "bell/clapper":
                    Swing("CineClapper", value);
                    break;
            }
        }

        private void Swing(string piece, float degrees)
        {
            var t = village.Piece(piece);
            if (t != null) t.localRotation = Quaternion.Euler(-degrees, 0f, 0f);   // Blender's +X turn is Unity's -X
        }

        private void StepRope()
        {
            if (_ropeT0 < 0f) return;
            var top = Array.Find(_k.points, p => p.name == "bell/ropeTop");
            var bottom = Array.Find(_k.points, p => p.name == "bell/ropeBottom");
            if (top == null || bottom == null) return;
            if (_rope == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "BellRope";
                Kill(go.GetComponent<Collider>());
                go.GetComponent<Renderer>().sharedMaterial = ropeMaterial;
                go.transform.SetParent(_root, false);
                _rope = go.transform;
            }
            var n = top.p.Length / 3;
            var x = Mathf.Clamp((Time - _ropeT0) * _k.fps, 0f, n - 1);
            var i = (int)x;
            var j = Mathf.Min(i + 1, n - 1);
            var a = Vector3.Lerp(P3(top.p, i), P3(top.p, j), x - i);
            var b = Vector3.Lerp(P3(bottom.p, i), P3(bottom.p, j), x - i);
            var d = b - a;
            _rope.position = (a + b) / 2f;
            _rope.rotation = Quaternion.FromToRotation(Vector3.up, d.sqrMagnitude > 1e-8f ? d.normalized : Vector3.down);
            _rope.localScale = new Vector3(_s.rope.width, d.magnitude, _s.rope.width);
        }

        private void DropSack()
        {
            if (!_actors.TryGetValue("ardan", out var ardan)) return;
            // the sack is a rigid part on his chest bone (CineFigure): it comes off and falls
            foreach (var r in ardan.t.GetComponentsInChildren<MeshRenderer>())
            {
                if (!r.name.StartsWith("CineArdanSack")) continue;
                r.transform.SetParent(_root, true);
                _sack = r.transform;
                _sackVel = new Vector3(_s.sack.vx, _s.sack.vy, _s.sack.vz);
            }
        }

        private void StepSack(float dt)
        {
            if (_sack == null) return;
            var r = _sack.GetComponent<MeshRenderer>();
            if (r.bounds.min.y <= 0.002f && _sackVel.y <= 0f)
            {
                _sackVel = Vector3.Lerp(_sackVel, Vector3.zero, Mathf.Clamp01(9f * dt));
                _sack.position += new Vector3(_sackVel.x, 0f, _sackVel.z) * dt;
                return;
            }
            _sackVel.y -= _s.sack.gravity * dt;
            _sack.position += _sackVel * dt;
            _sack.Rotate(_s.sack.spinX * dt, 0f, _s.sack.spinZ * dt, Space.World);
            if (r.bounds.min.y < 0f) _sack.position += Vector3.up * -r.bounds.min.y;
        }

        // ------------------------------------------------------------ camera and subtitles

        private void StepCamera()
        {
            Shot shot = null;
            foreach (var s in _s.shots)
            {
                if (s.start <= Time + 1e-4f) shot = s;
            }
            if (shot == null || shot.keys.Length == 0) return;
            ShotName = shot.name;
            _shallow = shot.dof;
            var u = Time - shot.start;
            var ia = 0;
            while (ia + 1 < shot.keys.Length && shot.keys[ia + 1].t <= u) ia++;
            var a = shot.keys[ia];
            var b = shot.keys[Mathf.Min(ia + 1, shot.keys.Length - 1)];
            var w = b.t > a.t ? Mathf.Clamp01((u - a.t) / (b.t - a.t)) : 0f;
            if (shot.ease) w = w * w * (3f - 2f * w);
            var from = Vector3.Lerp(V(a.from), V(b.from), w);
            var at = Vector3.Lerp(V(a.at), V(b.at), w);
            if (!string.IsNullOrEmpty(shot.follow) && _actors.TryGetValue(shot.follow, out var target))
            {
                from += target.t.position;
                at += target.t.position;
            }
            var shake = Mathf.Lerp(a.shake, b.shake, w);
            if (shake > 0f)
            {
                from += new Vector3(Mathf.Sin(Time * 37f), Mathf.Sin(Time * 43f + 2f), Mathf.Sin(Time * 29f + 1f)) * shake;
            }
            cam.transform.SetPositionAndRotation(from, Quaternion.LookRotation(at - from));
            _camFrom = from;
            _camAt = at;
            cam.focalLength = Mathf.Lerp(a.lens, b.lens, w);
        }

        private void StepSubtitle()
        {
            if (subtitle == null) return;
            if (_subT0 < 0f || Time > _subT0 + _subLen + _s.subtitle.fadeOut)
            {
                subtitle.text = string.Empty;
                return;
            }
            var u = Time - _subT0;
            var alpha = Mathf.Clamp01(u / Mathf.Max(0.01f, _s.subtitle.fadeIn)) *
                        (u > _subLen ? 1f - Mathf.Clamp01((u - _subLen) / Mathf.Max(0.01f, _s.subtitle.fadeOut)) : 1f);
            subtitle.text = _subText;
            var c = subtitle.color;
            c.a = alpha;
            subtitle.color = c;
        }

        // ------------------------------------------------------------ helpers

        private static Transform FindBone(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        private Light MakeLight(string name, Color colour, float range)
        {
            var go = new GameObject(name, typeof(Light));
            go.transform.SetParent(_root, false);
            var l = go.GetComponent<Light>();
            l.type = LightType.Point;
            l.color = colour;
            l.range = range;
            l.intensity = 0f;
            l.shadows = LightShadows.None;
            return l;
        }

        private static Vector3 V(float[] a) => a != null && a.Length >= 3 ? new Vector3(a[0], a[1], a[2]) : Vector3.zero;
        private static Color Col(float[] a) => a != null && a.Length >= 3 ? new Color(a[0], a[1], a[2]) : Color.white;
        private static Vector3 P3(float[] p, int i) => new Vector3(p[i * 3], p[i * 3 + 1], p[i * 3 + 2]);
        private static Vector3 Pos(float[] m, int i) => new Vector3(m[i * 7], m[i * 7 + 1], m[i * 7 + 2]);
        private static Quaternion Rot(float[] m, int i) => new Quaternion(m[i * 7 + 3], m[i * 7 + 4], m[i * 7 + 5], m[i * 7 + 6]);

        private static void Kill(UnityEngine.Object o)
        {
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }
    }
}
