using System;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Mission 1 «Бегство» (the user, 2026-11-11): the film ends and play begins on the spot. The
    /// camera eases from the film's last shot into the main game's view -- its pitch, its lens, no
    /// zoom, never turning -- and stays on Ардан, who is always in the middle and cannot be
    /// deselected. The floating stick runs him; the houses, props and trees stop him. The village
    /// goes on burning round him (the director keeps the world stepping).
    ///
    /// Stage 1: the handover and the controls in the film's village. The route beyond it (wheat,
    /// forest, bridge, the mage, the gates) waits for its models; see Design/mission1_map_v1.png.
    /// Every number is in Campaign/Mission1.json.
    /// </summary>
    public class MissionOne : MonoBehaviour
    {
        [Serializable] private class Config
        {
            public CameraConfig camera;
            public StickConfig joystick;
            public RunnerConfig runner;
            public string[] solidPieces;
            public string hintKey;
            public CalmConfig calm;
            public MissionChase.Config chase;
            public MissionMage.Config mage;
            public MissionEnd.Config finish;
            public MissionWorld.Config world;
            public MissionHud.Config hud;
            public SoundConfig sound;
            public ReinforceConfig reinforce;
        }

        [Serializable] private class ReinforceConfig { public int torch, spear; public float every; }

        [Serializable] private class SoundConfig { public float music, musicFadeIn, roar; }

        [Serializable] private class CameraConfig { public float pitch, yaw, distance, fov, aimHeight, blend, near, far; }
        [Serializable] private class StickConfig { public float radius, knob, deadzone, walkBelow, hintDelay, hintFadeAfter; public float[] well, gold; }
        [Serializable] private class CalmConfig { public float from, to; }
        [Serializable] private class RunnerConfig { public float radius, height, turnRate, trunkRadius, fade; }

        [SerializeField] private OpeningDirector director;
        [SerializeField] private OpeningVillage village;
        [SerializeField] private Camera cam;
        [SerializeField] private TextAsset configFile;
        [SerializeField] private OpeningFx fx;
        [SerializeField] private GameObject guardModel;
        [SerializeField] private Material darkMaterial;
        [SerializeField] private AudioClip music;
        [SerializeField] private AudioClip roar;
        [SerializeField] private MissionHud.Art hudArt;

        private MissionWorld _world;
        private MissionHud _hud;
        private AudioSource _music, _roar;

        /// <summary>The HUD (its objective), for the batchmode check.</summary>
        public MissionHud Hud => _hud;
        public MissionWorld World => _world;

        /// <summary>Set by «Ещё раз» before the scene is loaded again: the film is run through
        /// unseen (under the loading screen) and play begins at once.</summary>
        public static bool SkipFilm;

        private MissionEnd _end;
        private float _elapsed;
        // play waits for him to move (the user's second playtest): the orcs stand, the stick blinks
        private bool _armed;
        private Vector3 _armFrom;
        // the run's clock starts at his first step; a split at every checkpoint, kept as records
        private bool _clockOn;
        private int _lastReached = -1;
        private float _stretchFrom, _bestBefore;
        private const string BestKey = "m1_best";
        private const string SplitKey = "m1_split_";

        /// <summary>The end at the gate, for the batchmode check.</summary>
        public MissionEnd End => _end;

        private Config _c;
        private CineFigure _ardan;
        private CharacterController _body;
        private FloatingJoystick _stick;
        private MissionChase _chase;
        private UnityEngine.UI.Image _black;
        private UnityEngine.UI.Text _notice;

        /// <summary>The chase, for the batchmode check (how often he was caught).</summary>
        public MissionChase Chase => _chase;

        /// <summary>The mage at the bridge, for the batchmode check (its scene clock).</summary>
        public MissionMage Mage => _mage;
        private MissionMage _mage;
        private Vector3 _fromPos;
        private Quaternion _fromRot;
        private float _fromFov, _blend;

        public bool Running { get; private set; }
        public Transform Ardan => _ardan != null ? _ardan.transform : null;

        private void Awake()
        {
            if (director != null) director.Finished += Begin;
        }

        private void OnDestroy()
        {
            if (director != null) director.Finished -= Begin;
        }

        /// <summary>Play begins: Ардан is the player's, the camera eases into the game's view.</summary>
        public void Begin()
        {
            if (Running) return;
            _c = JsonUtility.FromJson<Config>(configFile.text);
            director.KeepAlive = true;
            director.CameraReleased = true;
            _ardan = director.Release("ardan");
            if (_ardan == null) return;
            var r = _c.runner;
            village.MakeSolid(_c.solidPieces, r.trunkRadius, r.height * 3f);
            _body = _ardan.gameObject.AddComponent<CharacterController>();
            _body.radius = r.radius;
            _body.height = Mathf.Max(r.height, r.radius * 2f);
            _body.center = new Vector3(0f, _body.height * 0.5f, 0f);
            _body.skinWidth = r.radius * 0.1f;
            _body.minMoveDistance = 0f;
            _body.stepOffset = 0f;

            // from the film's physical lens to the game's plain field of view, without a jump
            _fromPos = cam.transform.position;
            _fromRot = cam.transform.rotation;
            _fromFov = cam.usePhysicalProperties ? Camera.FocalLengthToFieldOfView(cam.focalLength, cam.sensorSize.y) : cam.fieldOfView;
            cam.usePhysicalProperties = false;
            cam.fieldOfView = _fromFov;
            // a deeper near plane: the film's 0.25 m made the gate's coat of arms flicker on its wall
            if (_c.camera.near > 0f) cam.nearClipPlane = _c.camera.near;
            if (_c.camera.far > 0f) cam.farClipPlane = _c.camera.far;
            _blend = 0f;

            var j = _c.joystick;
            _stick = FloatingJoystick.Create(j.radius, j.knob, j.deadzone, j.hintDelay, j.hintFadeAfter, Localization.Get(_c.hintKey));
            _stick.transform.SetParent(transform, false);
            _stick.Skin(hudArt.disc, hudArt.rim, Col(j.well), Col(j.gold));
            if (_c.chase != null)
            {
                _chase = new MissionChase(_c.chase, village, _ardan, _body, director.Member, director.FlameMaterial, transform, Localization.Get);
                Overlay();
            }
            if (_c.finish != null) _end = new MissionEnd(_c.finish, guardModel, transform, hudArt.restartGlyph, hudArt.homeGlyph);
            _bestBefore = PlayerPrefs.GetFloat(BestKey, 0f);
            Arm(false);
            if (_chase != null) _chase.Respawned += () => Arm(false);
            if (_c.world != null)
            {
                var finish = _c.finish != null ? new Vector2(_c.finish.runTo[0], _c.finish.runTo[1]) : Vector2.zero;
                _world = new MissionWorld(_c.world, village, transform, darkMaterial, fx != null ? fx.SparkMaterial : null,
                    _c.chase?.checkpoints, finish, director.Member, director.Release);
            }
            if (_c.hud != null) _hud = new MissionHud(_c.hud, hudArt, transform);
            if (_c.reinforce != null && director.Life != null) director.Life.Reinforce(_c.reinforce.torch, _c.reinforce.spear, _c.reinforce.every);
            Sound();
            if (_c.mage != null && fx != null)
                _mage = new MissionMage(_c.mage, village, director.Member, fx.SparkMaterial, fx.DustMaterial, director.FlameMaterial, transform);
            Running = true;
        }

        /// <summary>A light, uneasy loop under the run, and the orcs' roar when one first sees him.</summary>
        private void Sound()
        {
            var s = _c.sound;
            if (s == null) return;
            if (music != null)
            {
                _music = gameObject.AddComponent<AudioSource>();
                _music.clip = music;
                _music.loop = true;
                _music.spatialBlend = 0f;
                _music.volume = 0f;
                _music.Play();
            }
            if (roar != null && _chase != null)
            {
                _roar = gameObject.AddComponent<AudioSource>();
                _roar.spatialBlend = 0f;
                _chase.Spotted += at =>
                {
                    // nearer roars louder; a roar off screen is a warning
                    var d = (at - _ardan.transform.position).magnitude;
                    _roar.pitch = 0.9f + 0.2f * Mathf.PerlinNoise(at.x, at.z);
                    _roar.PlayOneShot(roar, s.roar * Mathf.Clamp01(1.2f - d / 14f));
                };
            }
        }

        /// <summary>Play waits (armed = false) until he has moved armAfter m from where he stands now.</summary>
        private void Arm(bool armed)
        {
            _armed = armed;
            if (_ardan != null) _armFrom = _ardan.transform.position;
            if (_stick != null) _stick.Beckon = !armed;
        }

        private static Color Col(float[] a) => a != null && a.Length >= 3 ? new Color(a[0], a[1], a[2], a.Length > 3 ? a[3] : 1f) : Color.white;

        private void Start()
        {
            if (!SkipFilm || director == null) return;
            SkipFilm = false;
            if (village.BuildingCount == 0) village.Build();
            if (!director.Playing) director.Play();
            while (director.Playing) director.Step(1f / 15f);
        }

        private void Update()
        {
            if (Running) Step(Time.deltaTime, null);
        }

        private void LateUpdate()
        {
            if (Running) StepCamera(Time.deltaTime);
        }

        /// <summary>One frame of play; `steer` stands in for the stick (the batchmode check).</summary>
        public void Step(float dt, Vector2? steer)
        {
            if (steer.HasValue) _stick.Feed(steer.Value, dt);
            if (_music != null && _c.sound != null) _music.volume = Mathf.MoveTowards(_music.volume, _end != null && _end.Started ? 0f : _c.sound.music, dt * _c.sound.music / Mathf.Max(0.1f, _c.sound.musicFadeIn));
            var here = _ardan.transform.position;
            if (!_armed && _c.chase != null && new Vector2(here.x - _armFrom.x, here.z - _armFrom.z).magnitude > _c.chase.armAfter)
            {
                Arm(true);
                _clockOn = true;
            }
            if (_world != null) _world.DarkLifted = _mage != null && _mage.Playing;
            _world?.Step(dt, here, _stick.Value.sqrMagnitude > 0f, _chase != null ? _chase.Reached : -1);
            if (_clockOn && (_end == null || !_end.Started)) _elapsed += dt;
            _hud?.Step(here, _elapsed, dt);
            Splits();
            if (_end != null)
            {
                if (_end.Ready(_ardan.transform.position))
                {
                    // at the gate: the say is taken for good; the last stretch and the record kept
                    _end.Begin();
                    _stick.Locked = true;
                    _hud?.Hide();
                    Stretch(_c.chase?.checkpoints != null ? _c.chase.checkpoints.Length : 0);
                    if (_bestBefore <= 0f || _elapsed < _bestBefore) PlayerPrefs.SetFloat(BestKey, _elapsed);
                    PlayerPrefs.Save();
                }
                if (_end.Started)
                {
                    _end.Step(dt, cam, _elapsed, _chase != null ? _chase.Catches : 0, _bestBefore);
                    if (_black != null) _black.color = new Color(0f, 0f, 0f, _end.Fade);
                    RunOn(dt, _end.RunTo);
                    return;
                }
            }
            if (_mage != null)
            {
                _mage.Step(dt);
                if (_mage.Ready(_ardan.transform.position))
                {
                    // the mage's scene: the player's say is taken, the stick put away
                    _mage.Begin(_chase != null ? _chase.Stop() : null);
                    _stick.Locked = true;
                    director.Say(_c.mage.lineKey, _c.mage.lineSeconds);
                }
                if (_mage.Playing)
                {
                    if (_black != null) _black.color = new Color(0f, 0f, 0f, _mage.Fade);
                    RunOn(dt, new Vector2(_c.mage.runTo[0], _c.mage.runTo[1]));
                    return;
                }
                if (_mage.Done && _stick.Locked)
                {
                    _stick.Locked = false;
                    if (_black != null) _black.color = Color.clear;
                }
            }
            if (_chase != null)
            {
                if (_armed) _chase.Step(dt);
                else _chase.Hold(dt);
                // struck down: a red flash, then the black of being brought back
                _black.color = _chase.Fade >= _chase.Hurt ? new Color(0f, 0f, 0f, _chase.Fade) : new Color(0.55f, 0.02f, 0.02f, _chase.Hurt);
                _notice.text = _chase.Notice;
                _notice.color = new Color(1f, 0.96f, 0.84f, _chase.NoticeAlpha);
                if (_chase.Frozen) return;
            }
            var v = _stick.Value;
            var m = v.magnitude;
            var t = _ardan.transform;
            if (m > 0f)
            {
                // screen up is where the camera looks, flattened; it never turns, so neither does this
                var yaw = _c.camera.yaw * Mathf.Deg2Rad;
                var up = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
                var right = new Vector3(Mathf.Cos(yaw), 0f, -Mathf.Sin(yaw));
                var dir = (right * v.x + up * v.y).normalized;
                var walk = m < _c.joystick.walkBelow;
                var clips = _ardan.Clips;
                var speed = walk ? clips.walkSpeed : clips.runSpeed;
                _body.Move(dir * speed * dt);
                var p = t.position;
                p.y = village.HeightAt(new Vector2(p.x, p.z));    // flat ground, or the bridge's deck
                t.position = p;
                t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(dir), Mathf.Clamp01(_c.runner.turnRate * dt));
                _ardan.Play(walk ? "Walk" : "Run", _c.runner.fade);
            }
            else _ardan.Play("Idle", _c.runner.fade);
            _ardan.Tick(dt);
            // the storm lets go as he gets away from the portal (the square, the village's origin)
            if (fx != null && _c.calm != null)
            {
                var d = new Vector2(t.position.x, t.position.z).magnitude;
                fx.Calm = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(_c.calm.from, _c.calm.to, d));
            }
        }

        /// <summary>A checkpoint reached: the stretch to it goes up over the clock.</summary>
        private void Splits()
        {
            if (_chase == null || _chase.Reached <= _lastReached) return;
            for (var i = _lastReached + 1; i <= _chase.Reached; i++) Stretch(i);
            _lastReached = _chase.Reached;
        }

        /// <summary>Stretch `i` (0-based) run: its time against the best one, the best kept.</summary>
        private void Stretch(int i)
        {
            var seconds = _elapsed - _stretchFrom;
            _stretchFrom = _elapsed;
            var best = PlayerPrefs.GetFloat(SplitKey + i, 0f);
            if (best <= 0f || seconds < best) PlayerPrefs.SetFloat(SplitKey + i, seconds);
            _hud?.Split(i + 1, seconds, best);
        }

        /// <summary>In the mage's scene Ардан runs on down the track by himself, past the mage.</summary>
        private void RunOn(float dt, Vector2 target)
        {
            var t = _ardan.transform;
            var to = new Vector3(target.x - t.position.x, 0f, target.y - t.position.z);
            if (to.magnitude > 0.2f)
            {
                var dir = to.normalized;
                _body.Move(dir * _ardan.Clips.runSpeed * dt);
                var p = t.position;
                p.y = village.HeightAt(new Vector2(p.x, p.z));
                t.position = p;
                t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(dir), Mathf.Clamp01(_c.runner.turnRate * dt));
                _ardan.Play("Run", _c.runner.fade);
            }
            else _ardan.Play("Idle", _c.runner.fade);
            _ardan.Tick(dt);
        }

        /// <summary>The black that falls when he is caught, and the checkpoint notice high in the middle.</summary>
        private void Overlay()
        {
            var canvas = _stick.GetComponent<Canvas>();
            var black = new GameObject("Black", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            black.transform.SetParent(canvas.transform, false);
            var r = (RectTransform)black.transform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            _black = black.GetComponent<UnityEngine.UI.Image>();
            _black.color = Color.clear;
            _black.raycastTarget = false;
            var note = new GameObject("Checkpoint", typeof(RectTransform), typeof(UnityEngine.UI.Text), typeof(UnityEngine.UI.Shadow));
            note.transform.SetParent(canvas.transform, false);
            var nr = (RectTransform)note.transform;
            // low in the middle, over the run's clock and its stretch line (the user: at the top it
            // hid behind Ардан's portrait)
            nr.anchorMin = nr.anchorMax = new Vector2(0.5f, 0f);
            nr.pivot = new Vector2(0.5f, 0f);
            nr.anchoredPosition = new Vector2(0f, 124f);
            nr.sizeDelta = new Vector2(600f, 50f);
            _notice = note.GetComponent<UnityEngine.UI.Text>();
            _notice.font = UI.UiTheme.Regular;
            _notice.fontSize = 30;
            note.AddComponent<UnityEngine.UI.Outline>().effectColor = new Color(0.1f, 0.06f, 0.03f, 0.9f);
            _notice.alignment = TextAnchor.MiddleCenter;
            _notice.raycastTarget = false;
            _notice.color = Color.clear;
            note.GetComponent<UnityEngine.UI.Shadow>().effectColor = new Color(0f, 0f, 0f, 0.6f);
        }

        /// <summary>The game's view, Ардан in the middle of it; eased into from the film's last shot.</summary>
        public void StepCamera(float dt)
        {
            var c = _c.camera;
            var aim = _ardan.transform.position + Vector3.up * c.aimHeight;
            var rot = Quaternion.Euler(c.pitch, c.yaw, 0f);
            var pos = aim - rot * Vector3.forward * c.distance;
            _blend = c.blend > 0f ? Mathf.Min(1f, _blend + dt / c.blend) : 1f;
            var w = Mathf.SmoothStep(0f, 1f, _blend);
            pos = Vector3.Lerp(_fromPos, pos, w);
            rot = Quaternion.Slerp(_fromRot, rot, w);
            var fov = Mathf.Lerp(_fromFov, c.fov, w);
            // the mage's scene takes the camera down to him, and gives it back
            if (_mage != null && _mage.OnShot)
            {
                _mage.Shot(out var from, out var at);
                pos = from;
                rot = Quaternion.LookRotation(at - from);
                fov = _mage.Fov;
                aim = at;
            }
            cam.transform.SetPositionAndRotation(pos, rot);
            cam.fieldOfView = fov;
            director.SetCameraAim(cam.transform.position, aim);
        }
    }
}
