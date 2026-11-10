using System.Text;
using CityBuilder.Combat;
using Unity.Profiling;
using UnityEngine;

namespace CityBuilder.Cheats
{
    /// <summary>
    /// A frame cost report for measuring on a phone over adb (the user, 2026-11-10: «что сильнее
    /// всего давит на производительность»). In a DEVELOPMENT build only (Debug.isDebugBuild) it
    /// writes one line to the log every few seconds -- `adb logcat -s Unity` shows it:
    ///   PERF fps … | cpu main/render, gpu (FrameTimingManager) | draws, setpass, tris, shadow casters |
    ///   the heaviest engine/script markers averaged over the window | GC per frame | what is on the map.
    /// Nothing here runs, or is even created, in a release build.
    /// </summary>
    public sealed class PerfProbe : MonoBehaviour
    {
        private const float Window = 3f;

        // engine markers worth a split (a missing one is skipped): scripts, physics, animation,
        // particles, the render loop and its shadow pass, UI, navigation
        private static readonly string[] Markers =
        {
            "BehaviourUpdate", "LateBehaviourUpdate", "FixedBehaviourUpdate", "CoroutinesDelayedCalls",
            "Physics.Simulate", "Physics.SyncTransforms", "Animators.Update", "Director.ProcessFrame",
            "ParticleSystem.Update", "ParticleSystem.WaitForUpdateThreads",
            "UniversalRenderPipeline.RenderSingleCameraInternal", "Inl_UniversalRenderPipeline.RenderSingleCameraInternal",
            "MainLightShadow", "Inl_MainLightShadow", "DrawOpaqueObjects", "Inl_DrawOpaqueObjects", "DrawTransparentObjects", "Inl_DrawTransparentObjects",
            "Culling", "CullScriptable", "Canvas.SendWillRenderCanvases", "UGUI.Rendering.UpdateBatches", "Canvas.BuildBatch",
            "NavMeshManager", "Gfx.WaitForPresentOnGfxThread", "Gfx.PresentFrame",
        };

        private ProfilerRecorder[] _markers;
        private ProfilerRecorder _draws, _setPass, _tris, _shadowCasters, _gc, _batches;
        private readonly FrameTiming[] _timing = new FrameTiming[1];
        private readonly double[] _markerSum = new double[Markers.Length];
        private double _cpuMain, _cpuRender, _gpu;
        private int _timed, _frames;
        private long _drawSum, _setPassSum, _trisSum, _gcSum, _batchSum;
        private float _clock;
        private readonly StringBuilder _line = new StringBuilder(512);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!Debug.isDebugBuild || Application.isEditor) return;
            var go = new GameObject("PerfProbe");
            DontDestroyOnLoad(go);
            go.AddComponent<PerfProbe>();
        }

        private void OnEnable()
        {
            _markers = new ProfilerRecorder[Markers.Length];
            for (var i = 0; i < Markers.Length; i++) {
                // by name alone, whatever its category; summed over every call in the frame
                _markers[i] = new ProfilerRecorder(Markers[i], 1, ProfilerRecorderOptions.Default | ProfilerRecorderOptions.SumAllSamplesInFrame);
                _markers[i].Start();
            }
            _draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            _batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            _setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            _tris = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            _shadowCasters = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Shadow Casters Count");
            _gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        }

        private void OnDisable()
        {
            if (_markers != null) foreach (var m in _markers) m.Dispose();
            _draws.Dispose(); _batches.Dispose(); _setPass.Dispose(); _tris.Dispose(); _shadowCasters.Dispose(); _gc.Dispose();
        }

        private void Update()
        {
            _frames++;
            _clock += Time.unscaledDeltaTime;
            for (var i = 0; i < _markers.Length; i++)
                if (_markers[i].Valid) _markerSum[i] += _markers[i].LastValue * 1e-6;   // ns -> ms
            _drawSum += _draws.LastValue;
            _batchSum += _batches.LastValue;
            _setPassSum += _setPass.LastValue;
            _trisSum += _tris.LastValue;
            _gcSum += _gc.LastValue;

            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, _timing) > 0)
            {
                _cpuMain += _timing[0].cpuMainThreadFrameTime;
                _cpuRender += _timing[0].cpuRenderThreadFrameTime;
                _gpu += _timing[0].gpuFrameTime;
                _timed++;
            }

            if (_clock < Window) return;
            Report();
            System.Array.Clear(_markerSum, 0, _markerSum.Length);
            _cpuMain = _cpuRender = _gpu = 0;
            _timed = _frames = 0;
            _drawSum = _setPassSum = _trisSum = _gcSum = _batchSum = 0;
            _clock = 0f;
        }

        private void Report()
        {
            var n = Mathf.Max(1, _frames);
            var t = Mathf.Max(1, _timed);
            _line.Clear();
            _line.Append("PERF fps ").Append((_frames / _clock).ToString("F1"))
                 .Append(" | cpu ").Append((_cpuMain / t).ToString("F1"))
                 .Append(" render ").Append((_cpuRender / t).ToString("F1"))
                 .Append(" gpu ").Append((_gpu / t).ToString("F1"))
                 .Append(" | draws ").Append(_drawSum / n).Append(" batches ").Append(_batchSum / n)
                 .Append(" setpass ").Append(_setPassSum / n)
                 .Append(" tris ").Append(_trisSum / n / 1000).Append("k shadowcasters ").Append(_shadowCasters.LastValue)
                 .Append(" | gc ").Append(_gcSum / n).Append("B |");
            for (var i = 0; i < _markers.Length; i++)
            {
                var ms = _markerSum[i] / n;
                if (ms >= 0.05) _line.Append(' ').Append(Markers[i]).Append(' ').Append(ms.ToString("F2"));
            }
            _line.Append(" | orcs ").Append(OrcUnit.All.Count)
                 .Append(" soldiers ").Append(SoldierUnit.All.Count)
                 .Append(" people ").Append(Citizens.CitizenManager.Instance != null ? Citizens.CitizenManager.Instance.TotalPopulation : 0)
                 .Append(" scene ").Append(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)
                 .Append(" map ").Append(Maps.MeshMapApplier.Instance != null ? Maps.MeshMapApplier.Instance.CurrentMapId : "-")
                 .Append(" res ").Append(Screen.width).Append('x').Append(Screen.height)
                 .Append(" tier ").Append(Core.GraphicsQuality.Effective);
            Debug.Log(_line.ToString());
        }
    }
}
