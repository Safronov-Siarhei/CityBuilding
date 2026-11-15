using System;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.CameraControl;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Mission 2's opening cutscene, scene 1 «У ворот» (storyboard and every animation approved by the user,
    /// 2026-11-14/15; Design/features/campaign.md «CUTSCENES»). It plays at the north gate before the siege: Ардан
    /// runs in to the captain and tells him; the lookout on the wall shouts «Враг! Из леса идут!» and blows the horn;
    /// guards run up the wall, townsfolk scatter, the gate shuts; the captain sends the rider to the capital, the
    /// horse rears and gallops off; the captain gives Ардан the sword -- and the player takes him from there.
    ///
    /// The approved Blender takes come in as in-place clips of their figures (Models/Cinematic/Cast/M2*.fbx, baked
    /// into CineClips by setup) and, per frame, each figure's root and each prop's matrix in its group's own Blender
    /// frame (Campaign/M2Intro/&lt;group&gt;.json, Tools/blender/cinematic/export_m2intro.py). Campaign/Mission2Intro.json
    /// places each frame at the north gate (in cells along the wall / out to the field, on the ground or the wall
    /// walk), times the groups on one clock (the gate talk holds while the alarm and the rider play), and holds the
    /// shots, the lines (localization keys) and when the gate shuts. A figure is sized as Ардан's game figure is.
    /// While it plays the siege's clock stands still, the HUD is hidden and the camera is the scene's; «Пропустить»
    /// (FilmSkip) runs it to its end. A restart («Заново») skips it.
    /// </summary>
    public partial class CityDefenceMode : IFilm
    {
        [SerializeField] private TextAsset introScript;              // Campaign/Mission2Intro.json
        [SerializeField] private TextAsset[] introTakes = new TextAsset[0];      // Campaign/M2Intro/*.json
        [SerializeField] private GameObject[] introFigures = new GameObject[0];  // Models/Cinematic/Cast/M2*.fbx
        [SerializeField] private CineClips[] introClips = new CineClips[0];      // their clips, in the same order
        [SerializeField] private GameObject[] introProps = new GameObject[0];    // Models/Cinematic/M2Intro*.fbx
        [SerializeField] private AudioClip introAmbient;    // Audio/Campaign/amb_morning.ogg: under the scene, no music
        [SerializeField] private AudioClip introCrowd;      // Audio/Campaign/sfx_crowd.ogg: the townsfolk at the alarm

        /// <summary>Set by «Заново»: the mission again without its opening scene.</summary>
        private static bool _skipIntroOnce;

        /// <summary>The next start goes straight to the siege (the play-mode tests).</summary>
        public static void SkipIntroOnce() => _skipIntroOnce = true;

        [Serializable] private class IntroScript
        {
            public Placement[] placements;
            public Timing[] groups;
            public Prelude prelude;
            public Shot[] shots;
            public Line[] lines;
            public float shutAt, end, fade, subFadeIn, subFadeOut, musicFadeOut;
            public float ambientVolume = 0.6f, crowdAt = -1f, crowdVolume = 0.7f;
            public int subSize;
        }

        /// <summary>Where a group's Blender frame stands: `along` the wall (cells, + to the right looking out),
        /// `outward` (cells, + to the field), on the wall walk or the ground; `flip` turns it to face the town
        /// (the alarm take faces +y out); `origin` (Blender units) is the frame's point put there.</summary>
        [Serializable] private class Placement
        {
            public string group, match;
            public float along, outward;
            public bool deck, flip;
            public float[] origin;
        }

        [Serializable] private class Timing
        {
            public string group;
            public float start;
            public float holdAt = -1f, resume;
            public bool hideAfter;
        }

        [Serializable] private class Prelude
        {
            public string figure, take;
            public float[] from;
            public float seconds;
        }

        [Serializable] private class Shot
        {
            public float t0, t1, fov;
            public bool deck;
            public float[] from, at, from2, at2;
        }

        [Serializable] private class Line
        {
            public float t, seconds;
            public string who, key;
        }

        [Serializable] private class TakeFile
        {
            public string group;
            public float fps, seconds;
            public int frames;
            public TakeFigure[] figures;
            public TakeProp[] props;
        }

        [Serializable] private class TakeFigure { public string figure, take; public float[] root; }
        [Serializable] private class TakeProp { public string piece; public float[] m; }

        private class Actor
        {
            public CineFigure fig;
            public Transform tr;           // a prop
            public string take;
            public float[] track;
            public Timing timing;
            public float fps, seconds;
            public int frames;
            public bool prop;
        }

        private class IntroState
        {
            public IntroScript script;
            public Transform root;
            public readonly List<Actor> actors = new List<Actor>();
            public Actor prelude;
            public Vector3 preludeFrom, preludeTo;
            public float time;
            public bool shut;
            public Camera cam;
            public float camFov;
            public Transform camParent;
            public Vector3 camLocalPos;
            public Quaternion camLocalRot;
            public AudioSource ambient, crowd;
            public bool crowdPlayed;
            public RTSCameraController rig;
            public readonly List<Canvas> hidden = new List<Canvas>();
            public Canvas canvas;
            public Text sub;
            public Image black;
            public Vector3 gate, outward, along;
            public float cell, deck;
        }

        private IntroState _intro;

        bool IFilm.Playing => _intro != null;

        /// <summary>The opening scene's clock (s), -1 when none plays (the play-mode check).</summary>
        public float IntroTime => _intro != null ? _intro.time : -1f;

        /// <summary>A short account of the scene as built, for the play-mode check's log.</summary>
        public string IntroReport { get; private set; } = "";

        float IFilm.Time => _intro != null ? _intro.time : _introPlayed;

        private float _introPlayed;

        void IFilm.Step(float dt) => TickIntro(dt);

        /// <summary>The opening scene, once the town, the garrison and Ардан stand. False when there is none to play.</summary>
        private bool StartIntro()
        {
            var skip = _skipIntroOnce;
            _skipIntroOnce = false;
            if (skip || introScript == null || introTakes.Length == 0 || OuterGates[0] == null) return false;
            var script = JsonUtility.FromJson<IntroScript>(introScript.text);
            if (script == null || script.groups == null || script.placements == null) return false;
            var s = new IntroState { script = script };
            s.cell = GridManager.Instance != null ? GridManager.Instance.CellSize : 1f;
            var gate = OuterGates[0].transform;
            s.gate = gate.position;
            // out to the field: the gatehouse's own axis nearest the line from the square through the gate
            var away = s.gate - _square;
            away.y = 0f;
            var axes = new[] { gate.right, -gate.right, gate.forward, -gate.forward };
            var best = axes[0];
            foreach (var a in axes)
                if (Vector3.Dot(a, away) > Vector3.Dot(best, away)) best = a;
            best.y = 0f;
            s.outward = best.normalized;
            s.along = Vector3.Cross(Vector3.up, s.outward);
            s.deck = WallWalk.HeightFor(OuterGates[0].Level);
            // a figure as tall as Ардан's game figure: his scale per Blender unit
            var k = CineScale;
            if (_ardenFig != null && ardenModel != null && ardenModel.transform.localScale.x > 0f)
                k = _ardenFig.transform.lossyScale.x / ardenModel.transform.localScale.x;
            s.root = new GameObject("M2Intro").transform;
            _intro = s;                  // from here a failure can be undone (AbortIntro)

            var frames = new Dictionary<string, Transform>();
            Transform FrameFor(string group, string name)
            {
                Placement p = null;
                foreach (var q in script.placements)
                    if (q.group == group && name.StartsWith(q.match ?? "")) { p = q; break; }
                if (p == null) return null;
                var id = group + "/" + p.match;
                if (frames.TryGetValue(id, out var f)) return f;
                var frame = new GameObject("Frame " + id).transform;
                frame.SetParent(s.root, false);
                frame.position = s.gate + s.along * (p.along * s.cell) + s.outward * (p.outward * s.cell) + Vector3.up * (p.deck ? s.deck : 0f);
                frame.rotation = Quaternion.LookRotation(p.flip ? s.outward : -s.outward, Vector3.up);
                frame.localScale = Vector3.one * k;
                var content = new GameObject("Content").transform;
                content.SetParent(frame, false);
                var o = p.origin != null && p.origin.Length == 3 ? p.origin : new float[3];
                content.localPosition = -new Vector3(o[0], o[2], o[1]);
                frames[id] = content;
                return content;
            }

            foreach (var asset in introTakes)
            {
                if (asset == null) continue;
                var take = JsonUtility.FromJson<TakeFile>(asset.text);
                var timing = Array.Find(script.groups, g => g.group == take.group);
                if (timing == null) continue;
                foreach (var f in take.figures)
                {
                    var i = Array.FindIndex(introFigures, m => m != null && m.name == f.figure);
                    var parent = FrameFor(take.group, f.figure);
                    if (i < 0 || i >= introClips.Length || introClips[i] == null || parent == null) continue;
                    var go = Instantiate(introFigures[i], parent, false);
                    go.name = f.figure + " " + f.take;
                    go.transform.localScale = introFigures[i].transform.localScale;
                    var fig = go.AddComponent<CineFigure>();
                    fig.Init(introClips[i]);
                    fig.Play(f.take, 0f, 0f);
                    var actor = new Actor { fig = fig, take = f.take, track = f.root, timing = timing, fps = take.fps, frames = take.frames, seconds = take.seconds };
                    s.actors.Add(actor);
                    if (script.prelude != null && f.figure == script.prelude.figure && s.prelude == null) s.prelude = actor;
                }
                foreach (var p in take.props)
                {
                    var model = Array.Find(introProps, m => m != null && m.name == p.piece);
                    var parent = FrameFor(take.group, p.piece);
                    if (model == null || parent == null) continue;
                    var go = Instantiate(model, parent, false);
                    go.name = p.piece;
                    go.transform.localScale = model.transform.localScale;
                    CineTwoSided.Apply(go);
                    s.actors.Add(new Actor { tr = go.transform, take = p.piece, prop = true, track = p.m, timing = timing, fps = take.fps, frames = take.frames, seconds = take.seconds });
                }
            }
            if (s.prelude != null && script.prelude.from != null && script.prelude.from.Length == 3)
            {
                var f = script.prelude.from;
                s.preludeFrom = new Vector3(f[0], f[2], f[1]);
                s.preludeTo = TrackPos(s.prelude.track, 0);
            }

            // the scene's own say: the HUD away, the camera taken, the siege's clock stopped (Update)
            s.cam = Camera.main;
            s.rig = RTSCameraController.Instance;
            if (s.rig != null) s.rig.enabled = false;
            if (s.cam != null)
            {
                // the camera hangs on the RTS rig: its own place on the rig comes back after the scene (the user's
                // second look: «у окна после проигрывания сцены сломан вид»)
                s.camFov = s.cam.fieldOfView;
                s.camParent = s.cam.transform.parent;
                s.camLocalPos = s.cam.transform.localPosition;
                s.camLocalRot = s.cam.transform.localRotation;
            }
            // no battle music under the scene (the user: «мне не нравится музыка») -- the morning's air, the crowd at
            // the alarm; the siege's music starts with the player's say
            if (_music != null) _music.Pause();   // TickMusic holds it while _intro is set
            // the wall's yellow door circles are the game's, not the scene's (the user's third look)
            foreach (var door in _doorCircles) if (door.mark != null) door.mark.SetActive(false);
            if (introAmbient != null)
            {
                s.ambient = s.root.gameObject.AddComponent<AudioSource>();
                s.ambient.clip = introAmbient;
                s.ambient.loop = true;
                s.ambient.spatialBlend = 0f;
                s.ambient.volume = script.ambientVolume;
                s.ambient.playOnAwake = false;   // started by TickIntro once the loading screen is gone
            }
            if (introCrowd != null)
            {
                s.crowd = s.root.gameObject.AddComponent<AudioSource>();
                s.crowd.clip = introCrowd;
                s.crowd.spatialBlend = 0f;
                s.crowd.volume = script.crowdVolume;
            }
            BuildIntroUi(s);
            HideInterface(s);
            var figs = 0;
            foreach (var a in s.actors) if (!a.prop) figs++;
            IntroReport = $"k={k:F3} cell={s.cell} deck={s.deck:F2} gate={s.gate} out={s.outward} figures={figs} props={s.actors.Count - figs} prelude={(s.prelude != null)}";
            Debug.Log("[M2Intro] " + IntroReport);
            if (_ardenFig != null) _ardenFig.gameObject.SetActive(false);
            FilmSkip.Attach(this);
            TickIntro(0f);
            return true;
        }

        /// <summary>Nothing of the interface but «Пропустить» while the scene plays (the user's second look: the HUD, the
        /// joystick's hint, the skill buttons showed) -- swept every frame: the HUD builds parts of itself late.</summary>
        private static void HideInterface(IntroState s)
        {
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (c == null || !c.isRootCanvas || !c.enabled || c == s.canvas) continue;
                if (c.GetComponentInParent<LoadingScreen>() != null || c.GetComponent<FilmSkip>() != null) continue;
                c.enabled = false;
                s.hidden.Add(c);
            }
        }

        private void BuildIntroUi(IntroState s)
        {
            var go = new GameObject("M2IntroUi", typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(s.root, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 70;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 1f;
            s.canvas = canvas;

            var black = new GameObject("Black", typeof(RectTransform), typeof(Image));
            black.transform.SetParent(go.transform, false);
            var br = (RectTransform)black.transform;
            br.anchorMin = Vector2.zero;
            br.anchorMax = Vector2.one;
            br.offsetMin = new Vector2(-50f, -50f);
            br.offsetMax = new Vector2(50f, 50f);
            s.black = black.GetComponent<Image>();
            s.black.color = new Color(0f, 0f, 0f, 0f);
            s.black.raycastTarget = false;

            var line = new GameObject("Line", typeof(RectTransform), typeof(Text), typeof(Outline));
            line.transform.SetParent(go.transform, false);
            var lr = (RectTransform)line.transform;
            lr.anchorMin = new Vector2(0.1f, 0f);
            lr.anchorMax = new Vector2(0.9f, 0f);
            lr.pivot = new Vector2(0.5f, 0f);
            lr.anchoredPosition = new Vector2(0f, 46f);
            lr.sizeDelta = new Vector2(0f, 110f);
            s.sub = line.GetComponent<Text>();
            s.sub.font = UiTheme.Bold;
            s.sub.fontSize = s.script.subSize > 0 ? s.script.subSize : 30;
            s.sub.alignment = TextAnchor.LowerCenter;
            s.sub.horizontalOverflow = HorizontalWrapMode.Wrap;
            s.sub.verticalOverflow = VerticalWrapMode.Overflow;
            s.sub.supportRichText = true;
            s.sub.color = Color.white;
            s.sub.raycastTarget = false;
            line.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);
            line.GetComponent<Outline>().effectDistance = new Vector2(2f, -2f);
        }

        // ------------------------------------------------------------------ playing

        private void TickIntro(float dt)
        {
            var s = _intro;
            if (s == null) return;
            HideInterface(s);
            // the siege's music may not sound under the scene, not even under the loading screen (a Pause in the frame
            // of its Play does not hold: the user heard it)
            if (_music != null && _music.isPlaying) _music.Pause();
            // the scene begins once the loading screen is gone (the user's rule, 2026-11-15) -- its sound too: the
            // ambient was started in StartIntro and played under the loading screen (the user's fourth look)
            if (LoadingScreen.Busy) dt = 0f;
            else if (s.ambient != null && !s.ambient.isPlaying && s.time <= 0f) s.ambient.Play();
            s.time += dt;
            var t = s.time;
            var sc = s.script;
            for (var i = 0; i < s.actors.Count; i++)
            {
                var a = s.actors[i];
                var local = LocalTime(a.timing, t);
                if (a.prop)
                {
                    if (a.tr != null) PlaceAt(a.tr, a, local, false);
                    continue;
                }
                if (a == s.prelude && t < a.timing.start)
                {
                    // the run-up from the field to where his take begins
                    var u = sc.prelude.seconds > 0f ? Mathf.Clamp01(1f - (a.timing.start - t) / sc.prelude.seconds) : 1f;
                    var tf = a.fig.transform;
                    tf.localPosition = Vector3.Lerp(s.preludeFrom, s.preludeTo, u);
                    var dir = s.preludeTo - s.preludeFrom;
                    if (dir.sqrMagnitude > 1e-6f) tf.localRotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                    var speed = sc.prelude.seconds > 0f ? dir.magnitude / sc.prelude.seconds : 0f;
                    var full = Mathf.Max(0.05f, a.fig.Clips.runSpeed);
                    a.fig.Play(sc.prelude.take, 0f, speed / full);
                    a.fig.Tick(dt);
                    continue;
                }
                a.fig.Play(a.take, a == s.prelude ? 0.15f : 0f, 0f);
                a.fig.SetTime(local);
                a.fig.Tick(dt);
                PlaceAt(a.fig.transform, a, local, true);
                var hide = a.timing.hideAfter && t > a.timing.start + a.seconds;
                if (a.fig.gameObject.activeSelf == hide) a.fig.gameObject.SetActive(!hide);
            }
            if (s.crowd != null && !s.crowdPlayed && sc.crowdAt >= 0f && t >= sc.crowdAt)
            {
                s.crowdPlayed = true;
                s.crowd.Play();
            }
            if (!s.shut && t >= sc.shutAt)
            {
                s.shut = true;
                ShutGate(OuterGates[0]);
            }
            TickGates(dt);
            IntroCamera(s, t);
            IntroLines(s, t);
            if (s.black != null)
            {
                var fade = Mathf.Max(0.01f, sc.fade);
                var dark = Mathf.Clamp01((t - (sc.end - fade)) / fade);
                s.black.color = new Color(0f, 0f, 0f, dark);
            }
            // the scene's music (mus_gate) goes quiet over its last seconds, not cut by the handover
            if (s.ambient != null && sc.musicFadeOut > 0f)
                s.ambient.volume = sc.ambientVolume * Mathf.Clamp01((sc.end - t) / sc.musicFadeOut);
            if (t >= sc.end) EndIntro();
        }

        /// <summary>A group's take time at the scene's time: from its start, held at `holdAt` until `resume`.</summary>
        private static float LocalTime(Timing g, float t)
        {
            var u = t - g.start;
            if (g.holdAt >= 0f && u > g.holdAt) u = t < g.resume ? g.holdAt : g.holdAt + (t - g.resume);
            return Mathf.Max(0f, u);
        }

        private static Vector3 TrackPos(float[] m, int i) => new Vector3(m[i * 7], m[i * 7 + 1], m[i * 7 + 2]);

        private static Quaternion TrackRot(float[] m, int i) => new Quaternion(m[i * 7 + 3], m[i * 7 + 4], m[i * 7 + 5], m[i * 7 + 6]);

        /// <summary>A root or a prop at its take's time; a figure is turned half round (the exported rig faces -Z).</summary>
        private static void PlaceAt(Transform tr, Actor a, float local, bool figure)
        {
            if (a.track == null || a.frames < 1 || a.track.Length < 7) return;
            var count = Mathf.Min(a.frames, a.track.Length / 7);
            var x = Mathf.Clamp(local * a.fps, 0f, count - 1);
            var i = (int)x;
            var j = Mathf.Min(i + 1, count - 1);
            var f = x - i;
            tr.localPosition = Vector3.Lerp(TrackPos(a.track, i), TrackPos(a.track, j), f);
            var q = Quaternion.Slerp(TrackRot(a.track, i), TrackRot(a.track, j), f);
            tr.localRotation = figure ? q * Quaternion.Euler(0f, 180f, 0f) : q;
        }

        private Vector3 IntroPoint(IntroState s, float[] p, bool deck)
        {
            if (p == null || p.Length < 3) return s.gate;
            return s.gate + s.along * (p[0] * s.cell) + Vector3.up * (p[1] + (deck ? s.deck : 0f)) + s.outward * (p[2] * s.cell);
        }

        private void IntroCamera(IntroState s, float t)
        {
            if (s.cam == null || s.script.shots == null) return;
            Shot shot = null;
            foreach (var sh in s.script.shots)
                if (t >= sh.t0 && t < sh.t1) { shot = sh; break; }
            if (shot == null) shot = s.script.shots[s.script.shots.Length - 1];
            var u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - shot.t0) / Mathf.Max(0.01f, shot.t1 - shot.t0)));
            var from = IntroPoint(s, shot.from, shot.deck);
            var at = IntroPoint(s, shot.at, shot.deck);
            if (shot.from2 != null && shot.from2.Length == 3) from = Vector3.Lerp(from, IntroPoint(s, shot.from2, shot.deck), u);
            if (shot.at2 != null && shot.at2.Length == 3) at = Vector3.Lerp(at, IntroPoint(s, shot.at2, shot.deck), u);
            s.cam.transform.position = from;
            if ((at - from).sqrMagnitude > 1e-6f) s.cam.transform.rotation = Quaternion.LookRotation(at - from, Vector3.up);
            if (shot.fov > 1f) s.cam.fieldOfView = shot.fov;
        }

        private static void IntroLines(IntroState s, float t)
        {
            if (s.sub == null) return;
            Line line = null;
            if (s.script.lines != null)
                foreach (var l in s.script.lines)
                    if (t >= l.t && t < l.t + l.seconds + s.script.subFadeOut) { line = l; break; }
            if (line == null)
            {
                s.sub.text = string.Empty;
                return;
            }
            var u = t - line.t;
            var alpha = Mathf.Clamp01(u / Mathf.Max(0.01f, s.script.subFadeIn))
                        * (u > line.seconds ? 1f - Mathf.Clamp01((u - line.seconds) / Mathf.Max(0.01f, s.script.subFadeOut)) : 1f);
            var who = string.IsNullOrEmpty(line.who) ? "" : "<color=#f2c46d>" + Localization.Get(line.who) + ":</color> ";
            s.sub.text = who + Localization.Get(line.key);
            s.sub.color = new Color(1f, 1f, 1f, alpha);
        }

        /// <summary>The scene broke: whatever it built away, the siege as if it had never been.</summary>
        private void AbortIntro()
        {
            if (_intro != null)
            {
                try { EndIntro(); }
                catch (Exception e) { Debug.LogException(e); _intro = null; }
            }
            var leftover = GameObject.Find("M2Intro");
            if (leftover != null) Destroy(leftover);
            if (_ardenFig != null) _ardenFig.gameObject.SetActive(true);
            if (RTSCameraController.Instance != null) RTSCameraController.Instance.enabled = true;
        }

        /// <summary>The scene over: Ардан where his take ended, the say back to the player.</summary>
        private void EndIntro()
        {
            var s = _intro;
            if (s == null) return;
            _intro = null;
            _introPlayed = Mathf.Max(0.01f, s.time);
            var end = s.prelude != null ? s.prelude.fig.transform.position : _start;
            var face = s.prelude != null ? s.prelude.fig.transform.rotation : Quaternion.identity;
            if (Arden != null)
            {
                end.y = Arden.transform.position.y;
                Arden.transform.SetPositionAndRotation(end, Quaternion.Euler(0f, face.eulerAngles.y, 0f));
                Arden.Steer = Vector3.zero;
                _ardenWas = end;
            }
            if (_ardenFig != null) _ardenFig.gameObject.SetActive(true);
            foreach (var c in s.hidden) if (c != null) c.enabled = true;
            if (s.cam != null)
            {
                s.cam.fieldOfView = s.camFov;
                s.cam.transform.SetParent(s.camParent, false);
                s.cam.transform.localPosition = s.camLocalPos;
                s.cam.transform.localRotation = s.camLocalRot;
            }
            // the siege's music: TickMusic starts it now that _intro is null
            foreach (var door in _doorCircles) if (door.mark != null) door.mark.SetActive(true);
            if (s.rig != null) s.rig.enabled = true;
            JumpToArden();
            if (s.root != null) Destroy(s.root.gameObject);
        }
    }
}
