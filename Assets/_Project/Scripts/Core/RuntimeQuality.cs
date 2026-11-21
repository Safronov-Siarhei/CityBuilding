using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace CityBuilder.Core
{
    /// <summary>
    /// The run-time step-down of the Auto graphics (the user, 2026-11-21: «меньше 40 фпс»). No tier chosen at boot
    /// can see heat: the Pixel 6 Pro (High) fell 48 -> 38 fps within minutes of a big fight (thermal status 2), the
    /// A71 31 -> 18 fps in its menu. So while the game runs:
    ///   * two 10-second windows in a row under <see cref="DownBelowFps"/> -> one step down;
    ///   * Android's thermal status rising to SEVERE-ish (<see cref="HotStatus"/>) -> one step down at once;
    ///   * 30 s at <see cref="UpFromFps"/>+ and not hot -> one step back up (never above the boot tier).
    /// Steps, cheapest look loss first: resolution x0.85, x0.7, post effects off, shadows off, MSAA off; a step that
    /// changes nothing on this tier (post on Low...) is skipped. A step back up that drops the frame rate again within a
    /// minute blocks further ups for 5 min (doubling), so a phone on the edge does not flicker between two looks.
    /// Manual graphics are never touched. Thresholds are rendering technique, not balance (as GraphicsQuality's).
    /// </summary>
    public class RuntimeQuality : MonoBehaviour
    {
        public const float DownBelowFps = 40f;
        public const float UpFromFps = 55f;
        private const float Window = 10f;
        private const float UpAfter = 30f;
        private const float MinGap = 30f;
        private const int HotStatus = 2;   // PowerManager.THERMAL_STATUS_MODERATE

        private const int MaxStep = 5;

        /// <summary>0 = the boot tier as is .. <see cref="MaxStep"/>.</summary>
        public static int Step { get; private set; }
        /// <summary>The probe's switch (PerfProbe «governor off»): measuring needs a fixed look.</summary>
        public static bool Enabled = true;

        public static float ScaleFactor => Step >= 2 ? 0.7f : Step >= 1 ? 0.85f : 1f;
        public static bool PostOff => Step >= 3;
        public static bool ShadowsOff => Step >= 4;
        public static bool MsaaOff => Step >= 5;

        public static int ThermalStatus { get; private set; }

        private float _time, _upTime, _lastChange = -999f, _upBlockedUntil, _upBlock = 300f, _lastUpAt = -999f;
        private int _frames, _slowWindows, _hotSeen;
        private float _thermalPoll;
        private bool _reapplyPost;
#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _power;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (!Application.isMobilePlatform) return;
            var go = new GameObject("RuntimeQuality");
            DontDestroyOnLoad(go);
            go.AddComponent<RuntimeQuality>();
        }

        private void Awake()
        {
            SceneManager.sceneLoaded += OnScene;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    _power = activity.Call<AndroidJavaObject>("getSystemService", "power");
            }
            catch (System.Exception) { _power = null; }
#endif
        }

        private void OnDestroy() => SceneManager.sceneLoaded -= OnScene;

        // a loading frame is no measure; a new scene's cameras come with post on
        private void OnScene(Scene s, LoadSceneMode m)
        {
            ResetWindow();
            _slowWindows = 0;
            _reapplyPost = true;
        }

        private void ResetWindow() { _time = 0f; _frames = 0; }

        private void Update()
        {
            var dt = Time.unscaledDeltaTime;
            if (_reapplyPost) { _reapplyPost = false; ApplyPost(); }
            if (!Enabled || GraphicsQuality.Manual) { ResetWindow(); _upTime = 0f; return; }
            var now = Time.realtimeSinceStartup;

            _thermalPoll -= dt;
            if (_thermalPoll <= 0f)
            {
                _thermalPoll = Window;
                ThermalStatus = ReadThermal();
                if (ThermalStatus >= HotStatus && _hotSeen < HotStatus) { _hotSeen = ThermalStatus; Down("hot " + ThermalStatus, now); }
                else if (ThermalStatus < HotStatus) _hotSeen = ThermalStatus;
            }

            if (dt > 0.5f) { ResetWindow(); return; }   // a load / a pause, not a frame
            _time += dt;
            _frames++;
            if (_time < Window) return;
            var fps = _frames / _time;
            ResetWindow();

            if (fps < DownBelowFps)
            {
                _upTime = 0f;
                if (++_slowWindows >= 2) { _slowWindows = 0; Down($"{fps:0} fps", now); }
                return;
            }
            _slowWindows = 0;
            if (fps < UpFromFps || ThermalStatus >= HotStatus || Step == 0 || now < _upBlockedUntil) { _upTime = 0f; return; }
            _upTime += Window;
            if (_upTime >= UpAfter && now - _lastChange >= MinGap) { _upTime = 0f; Up(now); }
        }

        private void Down(string why, float now)
        {
            // a step up undone within a minute: the phone is on the edge, stop trying for a while
            if (now - _lastUpAt < 60f) { _upBlockedUntil = now + _upBlock; _upBlock *= 2f; }
            if (now - _lastChange < MinGap && !why.StartsWith("hot")) return;
            var next = Step;
            do next++; while (next <= MaxStep && NoChange(next));
            if (next > MaxStep) return;
            Set(next, why, now);
        }

        private void Up(float now)
        {
            var next = Step;
            do next--; while (next > 0 && NoChange(next));
            _lastUpAt = now;
            Set(Mathf.Max(0, next), "up", now);
        }

        /// <summary>A step that takes away nothing on this tier (post / MSAA already off).</summary>
        private static bool NoChange(int step)
        {
            var low = GraphicsQuality.Effective == GraphicsTier.Low;
            if (step == 3) return low;
            if (step == 5) return GraphicsQuality.AppliedMsaa <= 1 && Step < 5;
            return false;
        }

        private void Set(int step, string why, float now)
        {
            if (step == Step) return;
            Step = step;
            _lastChange = now;
            GraphicsQuality.Apply();
            ApplyPost();
            Debug.Log($"[RuntimeQuality] step {Step} ({why}): scale x{ScaleFactor}, post {(PostOff ? "off" : "on")}, shadows {(ShadowsOff ? "off" : "on")}, msaa {(MsaaOff ? "off" : "tier")}");
        }

        // the cameras' post switch: off on a step >= 3; back on only where this class turned it off
        private static readonly System.Collections.Generic.List<Camera> Muted = new System.Collections.Generic.List<Camera>();

        private static void ApplyPost()
        {
            if (PostOff && !GraphicsQuality.Manual)
            {
                foreach (var cam in Camera.allCameras)
                {
                    var data = cam.GetUniversalAdditionalCameraData();
                    if (!data.renderPostProcessing) continue;
                    data.renderPostProcessing = false;
                    Muted.Add(cam);
                }
                return;
            }
            foreach (var cam in Muted)
                if (cam != null) cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            Muted.Clear();
        }

        private int ReadThermal()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_power == null) return 0;
            try { return _power.Call<int>("getCurrentThermalStatus"); }   // API 29+
            catch (System.Exception) { _power = null; }
#endif
            return 0;
        }
    }
}
