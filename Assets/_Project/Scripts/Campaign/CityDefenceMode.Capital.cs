using System;
using System.Collections.Generic;
using CityBuilder.CameraControl;
using CityBuilder.Core;
using CityBuilder.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Mission 2's scene 2 «Гонец у короля» (sets, storyboard and every animation approved by the user 2026-11-15;
    /// Design/features/campaign.md «SCENE 2»). It plays when the courier reaches the capital -- the turn of the clock
    /// to «Подкрепление через» (HelpComing, the first 8 minutes): the rider gallops over the moat's bridge into the
    /// open capital, down a street of people leaping out of his way (an apple cart tips), pulls up at the castle's
    /// stairs and runs in; in the throne room he shoves a guard aside, kneels and tells; silence; the King rises and
    /// sends the cavalry -- he leads it himself. Then the siege goes on where it stood.
    ///
    /// It is shot far from the town (`places`: the capital and the throne room, Models/Cinematic/M2CapitalSet.fbx and
    /// M2ThroneSet.fbx, each one static mesh) with the approved takes (Models/Cinematic/Cast/Cap*.fbx, roots and props
    /// per frame in Campaign/M2Capital/&lt;group&gt;.json -- Tools/blender/cinematic/export_m2capital.py), laid out by
    /// Campaign/Mission2Capital.json: each `use` puts a group's Blender frame at an origin and turn in a place, from
    /// its start, shown for a window (the street's gallop is used twice: over the bridge, then down the street).
    /// While it plays the whole game stands (Time.timeScale 0; the scene steps on unscaled time), the HUD and the
    /// darkness over the land's edge are away, the camera is the scene's; «Пропустить» runs it to its end.
    /// </summary>
    public partial class CityDefenceMode
    {
        [SerializeField] private TextAsset capitalScript;                         // Campaign/Mission2Capital.json
        [SerializeField] private TextAsset[] capitalTakes = new TextAsset[0];      // Campaign/M2Capital/*.json
        [SerializeField] private GameObject[] capitalFigures = new GameObject[0];  // Models/Cinematic/Cast/Cap*.fbx
        [SerializeField] private CineClips[] capitalClips = new CineClips[0];      // their clips, in the same order
        [SerializeField] private GameObject[] capitalProps = new GameObject[0];    // Models/Cinematic/CapProp*.fbx
        [SerializeField] private GameObject[] capitalSets = new GameObject[0];     // M2CapitalSet, M2ThroneSet
        [SerializeField] private AudioClip capitalMusic;                          // Audio/Campaign/mus_gate.ogg (the streets)
        [SerializeField] private AudioClip capitalMusic2;                         // Audio/Campaign/mus_attack.ogg (the throne room)

        /// <summary>Set by the play-mode tests (with SkipIntroOnce): no scene at the turn either.</summary>
        private static bool _skipCapitalOnce;
        private bool _noCapital;
        private bool _capitalDone;

        [Serializable] private class CapitalScript
        {
            public Place[] places;
            public Use[] uses;
            public CapitalShot[] shots;
            public Line[] lines;
            public float end, fadeIn, fade, subFadeIn, subFadeOut, musicVolume = 0.5f, musicFadeOut,
                music2At = -1f, musicCross = 1f,     // the throne room's music from music2At, the streets' fading out before it
                hornAt = -1f, fireSize = 1f;         // the last scene: the King's horn; the burning houses' flames
            public int subSize, fires;               // the last scene: how many more houses burn besides the razed one
        }

        /// <summary>A set and where it stands (Unity, far from the town) -- or, for the last scene, a spot in the
        /// mission's own town: `cell` (the plan's x, y), `anchor` "house" = the north face of the standing house nearest
        /// it (CityDefenceMode.Help.cs).</summary>
        [Serializable] private class Place
        {
            public string name, set, anchor;
            public float[] at, cell;
        }

        /// <summary>A group's take in a place: its Blender frame at `origin` (Blender x, y, z) turned `turn` degrees
        /// about Blender's up; its clock from `start`; shown from `show[0]` to `show[1]` (film seconds); only the
        /// `figures` named (all when empty); `rise` [x0, x1, x2, x3, z] lifts a figure by z over the place's Blender
        /// x x1..x2 (a bridge's deck), ramping from x0 and down to x3.</summary>
        [Serializable] private class Use
        {
            public string group, place;
            public float[] origin, show, rise;
            public float turn, start;
            public string[] figures;
            public bool props = true;
        }

        private class CapitalActor
        {
            public CineFigure fig;
            public Transform tr;
            public string take;
            public float[] track;
            public Use use;
            public float fps, seconds;
            public int frames;
            public bool prop;
            public Transform place;
            public Actor at;           // the track as the opening scene's PlaceAt reads it
        }

        private class CapitalState : IFilm
        {
            public CityDefenceMode mode;
            public CapitalScript script;
            public Transform root;
            public readonly Dictionary<string, Transform> places = new Dictionary<string, Transform>();
            public readonly List<CapitalActor> actors = new List<CapitalActor>();
            public float time;
            public bool done;
            public Camera cam;
            public float camFov;
            public Transform camParent;
            public Vector3 camLocalPos;
            public Quaternion camLocalRot;
            public RTSCameraController rig;
            public readonly List<Canvas> hidden = new List<Canvas>();
            public Canvas canvas;
            public Text sub;
            public Image black;
            public AudioSource music, music2;
            public float timeScale = 1f;
            public bool darkWas;
            public bool inTown;                                    // the last scene: in the town itself, its darkness kept
            public Action onEnd;
            public AudioClip horn;
            public bool hornDone;
            public readonly List<Renderer> unseen = new List<Renderer>();   // the town's living, hidden while it plays

            bool IFilm.Playing => !done;
            float IFilm.Time => time;
            void IFilm.Step(float dt) => mode.TickCapital(dt);
        }

        private CapitalState _capital;

        /// <summary>The second scene's clock (s), -1 when none plays (the play-mode check).</summary>
        public float CapitalTime => _capital != null ? _capital.time : -1f;

        /// <summary>The second scene has played (or was skipped) this run.</summary>
        public bool CapitalDone => _capitalDone;

        /// <summary>The clock on to the courier's arrival (the play-mode check, the cheats).</summary>
        public void ClockToTheTurn() => TimeLeft = Mathf.Min(TimeLeft, B.reinforceSeconds + 0.05f);

        /// <summary>The cheats' «Перемотать к гонцу» (the user, 2026-11-16): the clock on to the courier's arrival,
        /// and the scene again even if it has played this run.</summary>
        public void CheatToCapital()
        {
            _capitalDone = false;
            _noCapital = false;
            ClockToTheTurn();
        }

        /// <summary>The second scene run to its end at once, as «Пропустить» does (the play-mode check).</summary>
        public void SkipCapital()
        {
            for (var i = 0; i < 10000 && _capital != null; i++) TickCapital(1f / 15f);
        }

        /// <summary>The scene's places: far out from the town, nothing of it in their view.</summary>
        private static readonly Vector3 CapitalFar = new Vector3(6000f, 0f, 6000f);

        private void WatchForCapital()
        {
            if (_capitalDone || _capital != null || _noCapital || !HelpComing) return;
            _capitalDone = true;
            try
            {
                if (!StartCapital()) return;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                AbortCapital();
            }
        }

        private bool StartCapital() => StartFilm(capitalScript, capitalTakes, capitalFigures, capitalClips, capitalProps, capitalSets,
            capitalMusic, capitalMusic2, null, false, null);

        /// <summary>A film of placed takes (scene 2 far out; the last scene, `inTown`, in the town itself).</summary>
        private bool StartFilm(TextAsset scriptAsset, TextAsset[] takeAssets, GameObject[] figures, CineClips[] clips, GameObject[] props,
            GameObject[] sets, AudioClip music1, AudioClip music2, AudioClip horn, bool inTown, Action onEnd)
        {
            if (scriptAsset == null || takeAssets == null || takeAssets.Length == 0) return false;
            var script = JsonUtility.FromJson<CapitalScript>(scriptAsset.text);
            if (script == null || script.places == null || script.uses == null) return false;
            var s = new CapitalState { mode = this, script = script, inTown = inTown, onEnd = onEnd, horn = horn };
            s.root = new GameObject(inTown ? "M2Help" : "M2Capital").transform;
            s.root.position = inTown ? Vector3.zero : CapitalFar;
            _capital = s;

            foreach (var p in script.places)
            {
                var place = new GameObject("Place " + p.name).transform;
                place.SetParent(s.root, false);
                place.localPosition = p.at != null && p.at.Length == 3 ? new Vector3(p.at[0], p.at[1], p.at[2]) : Vector3.zero;
                if (inTown && p.cell != null && p.cell.Length == 2) place.position = PlaceInTown(p, script);
                var set = Array.Find(sets, m => m != null && m.name == p.set);
                if (set != null)
                {
                    var go = Instantiate(set, place, false);
                    go.name = p.set;
                    go.transform.localScale = set.transform.localScale;
                }
                s.places[p.name] = place;
            }

            var takes = new Dictionary<string, TakeFile>();
            foreach (var asset in takeAssets)
            {
                if (asset == null) continue;
                var take = JsonUtility.FromJson<TakeFile>(asset.text);
                if (take != null) takes[take.group] = take;
            }
            foreach (var use in script.uses)
            {
                if (!takes.TryGetValue(use.group, out var take) || !s.places.TryGetValue(use.place ?? "", out var place)) continue;
                var frame = new GameObject($"Use {use.group} {use.start:F1}").transform;
                frame.SetParent(place, false);
                var o = use.origin != null && use.origin.Length == 3 ? use.origin : new float[3];
                frame.localPosition = new Vector3(o[0], o[2], o[1]);
                frame.localRotation = Quaternion.Euler(0f, -use.turn, 0f);
                foreach (var f in take.figures)
                {
                    if (use.figures != null && use.figures.Length > 0 && Array.IndexOf(use.figures, f.figure) < 0) continue;
                    var i = Array.FindIndex(figures, m => m != null && m.name == f.figure);
                    if (i < 0 || i >= clips.Length || clips[i] == null) continue;
                    var go = Instantiate(figures[i], frame, false);
                    go.name = f.figure + " " + f.take;
                    go.transform.localScale = figures[i].transform.localScale;
                    var fig = go.AddComponent<CineFigure>();
                    fig.Init(clips[i]);
                    fig.Play(f.take, 0f, 0f);
                    go.SetActive(false);
                    s.actors.Add(new CapitalActor { fig = fig, take = f.take, track = f.root, use = use, fps = take.fps, frames = take.frames, seconds = take.seconds, place = place });
                }
                if (!use.props || take.props == null) continue;
                foreach (var p in take.props)
                {
                    var model = Array.Find(props, m => m != null && m.name == p.piece);
                    if (model == null) continue;
                    var go = Instantiate(model, frame, false);
                    go.name = p.piece;
                    go.transform.localScale = model.transform.localScale;
                    CineTwoSided.Apply(go);
                    go.SetActive(false);
                    s.actors.Add(new CapitalActor { tr = go.transform, take = p.piece, prop = true, track = p.m, use = use, fps = take.fps, frames = take.frames, seconds = take.seconds, place = place });
                }
            }

            // the whole game stands while it plays: the units, the waves, the clock (the scene steps on unscaled time)
            s.timeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            s.cam = Camera.main;
            s.rig = RTSCameraController.Instance;
            if (s.rig != null) s.rig.enabled = false;
            if (s.cam != null)
            {
                s.camFov = s.cam.fieldOfView;
                s.camParent = s.cam.transform.parent;
                s.camLocalPos = s.cam.transform.localPosition;
                s.camLocalRot = s.cam.transform.localRotation;
            }
            // the darkness over the land's edge reads the world's place: out here all would be dark (in the town it stays)
            if (_darkRenderer != null && !inTown)
            {
                s.darkWas = _darkRenderer.enabled;
                _darkRenderer.enabled = false;
            }
            if (_music != null) _music.Pause();
            if (music1 != null)
            {
                s.music = s.root.gameObject.AddComponent<AudioSource>();
                s.music.clip = music1;
                s.music.loop = false;      // a 23 s clip looped mid-scene (the user, 2026-11-16): never again
                s.music.spatialBlend = 0f;
                s.music.volume = script.musicVolume;
                s.music.ignoreListenerPause = true;
                s.music.Play();
            }
            if (music2 != null && script.music2At >= 0f)
            {
                s.music2 = s.root.gameObject.AddComponent<AudioSource>();
                s.music2.clip = music2;
                s.music2.loop = false;
                s.music2.spatialBlend = 0f;
                s.music2.volume = 0f;
                s.music2.ignoreListenerPause = true;
            }
            if (inTown) HideTheLiving(s);
            BuildCapitalUi(s);
            HideCanvases(s.hidden, s.canvas);
            Debug.Log($"[{s.root.name}] places={s.places.Count} actors={s.actors.Count} end={script.end}");
            FilmSkip.Attach(s);
            TickCapital(0f);
            return true;
        }

        private void BuildCapitalUi(CapitalState s)
        {
            var go = new GameObject("M2CapitalUi", typeof(Canvas), typeof(CanvasScaler));
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
            s.black.color = Color.black;
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

        /// <summary>Every root canvas but the scene's own and «Пропустить» off (swept every frame: parts of the HUD
        /// come up late).</summary>
        private static void HideCanvases(List<Canvas> hidden, Canvas own)
        {
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (c == null || !c.isRootCanvas || !c.enabled || c == own) continue;
                if (c.GetComponentInParent<LoadingScreen>() != null || c.GetComponent<FilmSkip>() != null) continue;
                c.enabled = false;
                hidden.Add(c);
            }
        }

        private void TickCapital(float dt)
        {
            var s = _capital;
            if (s == null || s.done) return;
            HideCanvases(s.hidden, s.canvas);
            if (_music != null && _music.isPlaying) _music.Pause();
            s.time += dt;
            var t = s.time;
            var sc = s.script;
            foreach (var a in s.actors)
            {
                var show = a.use.show != null && a.use.show.Length == 2 ? a.use.show : new[] { a.use.start, sc.end };
                var on = t >= show[0] && t <= show[1];
                var go = a.prop ? a.tr.gameObject : a.fig.gameObject;
                if (go.activeSelf != on) go.SetActive(on);
                if (!on) continue;
                var local = Mathf.Max(0f, t - a.use.start);
                var actor = a.at ??= new Actor { track = a.track, fps = a.fps, frames = a.frames };
                if (a.prop)
                {
                    PlaceAt(a.tr, actor, local, false);
                    continue;
                }
                a.fig.Play(a.take, 0f, 0f);
                a.fig.SetTime(local);
                a.fig.Tick(dt);
                PlaceAt(a.fig.transform, actor, local, true);
                Rise(a);
            }
            CapitalCamera(s, t);
            CapitalLines(s, t);
            if (s.horn != null && !s.hornDone && sc.hornAt >= 0f && t >= sc.hornAt)
            {
                s.hornDone = true;
                var src = s.root.gameObject.AddComponent<AudioSource>();
                src.spatialBlend = 0f;
                src.ignoreListenerPause = true;
                src.PlayOneShot(s.horn, 1f);
            }
            if (s.black != null)
            {
                var fadeIn = Mathf.Max(0.01f, sc.fadeIn);
                var fade = Mathf.Max(0.01f, sc.fade);
                var dark = Mathf.Max(1f - Mathf.Clamp01(t / fadeIn), Mathf.Clamp01((t - (sc.end - fade)) / fade));
                s.black.color = new Color(0f, 0f, 0f, dark);
            }
            {
                var cross = Mathf.Max(0.01f, sc.musicCross);
                var tail = sc.musicFadeOut > 0f ? Mathf.Clamp01((sc.end - t) / sc.musicFadeOut) : 1f;
                var two = s.music2 != null;
                if (s.music != null)
                    s.music.volume = sc.musicVolume * tail * (two ? Mathf.Clamp01((sc.music2At - t) / cross) : 1f);
                if (two)
                {
                    if (t >= sc.music2At && !s.music2.isPlaying && s.music2.time <= 0f) s.music2.Play();
                    s.music2.volume = sc.musicVolume * tail * Mathf.Clamp01((t - sc.music2At) / 0.4f);
                }
            }
            if (t >= sc.end) EndCapital();
        }

        /// <summary>A figure up onto a bridge's deck over its span (the take was made on flat ground).</summary>
        private static void Rise(CapitalActor a)
        {
            var r = a.use.rise;
            if (r == null || r.Length != 5) return;
            var x = a.place.InverseTransformPoint(a.fig.transform.position).x;
            float w;
            if (x <= r[0] || x >= r[3]) w = 0f;
            else if (x < r[1]) w = (x - r[0]) / Mathf.Max(0.01f, r[1] - r[0]);
            else if (x <= r[2]) w = 1f;
            else w = (r[3] - x) / Mathf.Max(0.01f, r[3] - r[2]);
            a.fig.transform.position += Vector3.up * (r[4] * w);
        }

        private static Vector3 PlacePoint(CapitalState s, string place, float[] p)
        {
            if (p == null || p.Length < 3 || !s.places.TryGetValue(place ?? "", out var tr)) return s.root.position;
            return tr.TransformPoint(new Vector3(p[0], p[2], p[1]));
        }

        private static void CapitalCamera(CapitalState s, float t)
        {
            var shots = s.script.shots;
            if (s.cam == null || shots == null || shots.Length == 0) return;
            CapitalShot shot = null;
            foreach (var sh in shots)
                if (t >= sh.t0 && t < sh.t1) { shot = sh; break; }
            if (shot == null) shot = shots[shots.Length - 1];
            var u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - shot.t0) / Mathf.Max(0.01f, shot.t1 - shot.t0)));
            var from = PlacePoint(s, shot.place, shot.from);
            var at = PlacePoint(s, shot.place, shot.at);
            if (shot.from2 != null && shot.from2.Length == 3) from = Vector3.Lerp(from, PlacePoint(s, shot.place, shot.from2), u);
            if (shot.at2 != null && shot.at2.Length == 3) at = Vector3.Lerp(at, PlacePoint(s, shot.place, shot.at2), u);
            s.cam.transform.SetParent(null, true);
            s.cam.transform.position = from;
            if ((at - from).sqrMagnitude > 1e-6f) s.cam.transform.rotation = Quaternion.LookRotation(at - from, Vector3.up);
            if (shot.fov > 1f) s.cam.fieldOfView = shot.fov;
        }

        private static void CapitalLines(CapitalState s, float t)
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

        private void AbortCapital()
        {
            if (_capital != null)
            {
                try { EndCapital(); }
                catch (Exception e) { Debug.LogException(e); _capital = null; }
            }
            foreach (var name in new[] { "M2Capital", "M2Help" })
            {
                var leftover = GameObject.Find(name);
                if (leftover != null) Destroy(leftover);
            }
            if (RTSCameraController.Instance != null) RTSCameraController.Instance.enabled = true;
            if (Time.timeScale == 0f) Time.timeScale = 1f;
        }

        /// <summary>The scene over: the camera, the HUD, the darkness, the music and the clock back to the siege.</summary>
        private void EndCapital()
        {
            var s = _capital;
            if (s == null) return;
            s.done = true;
            _capital = null;
            foreach (var c in s.hidden) if (c != null) c.enabled = true;
            if (s.cam != null)
            {
                s.cam.fieldOfView = s.camFov;
                s.cam.transform.SetParent(s.camParent, false);
                s.cam.transform.localPosition = s.camLocalPos;
                s.cam.transform.localRotation = s.camLocalRot;
            }
            if (_darkRenderer != null) _darkRenderer.enabled = s.darkWas;
            if (s.rig != null) s.rig.enabled = true;
            foreach (var r in s.unseen) if (r != null) r.enabled = true;
            Time.timeScale = s.timeScale;
            if (!s.inTown) JumpToArden();
            if (s.root != null) Destroy(s.root.gameObject);
            s.onEnd?.Invoke();
        }

        /// <summary>A shot of the second scene: in a place's Blender frame.</summary>
        [Serializable] private class CapitalShot
        {
            public string place;
            public float t0, t1, fov;
            public float[] from, at, from2, at2;
        }
    }
}
