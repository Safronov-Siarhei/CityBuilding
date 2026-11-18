using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CityBuilder.Core
{
    /// <summary>
    /// A phone profiling probe driven over adb (2026-11-18: the film and mission 1 stayed at 20-45 fps on a Pixel 6 Pro
    /// and the frame turned out to be GPU-bound with nothing to show which part). Push a text file to
    /// /sdcard/Android/data/&lt;package&gt;/files/perf.txt; every second the probe reads it and applies each line:
    ///   post off | dark off | particles off | skinned off | shadows off | figures off | shader &lt;part of a shader name&gt; off
    ///   scale 0.5 | msaa 1 | census
    /// «census» writes perf_out.txt next to it: renderers, triangles and screen share per shader. Delete the file to stop
    /// (what was switched off stays off until the scene changes). Without the file the probe costs one File.Exists a second.
    /// </summary>
    public class PerfProbe : MonoBehaviour
    {
        private string _path, _out;
        private float _next;
        private string _last;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!Application.isMobilePlatform) return;
            var go = new GameObject("PerfProbe");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideInHierarchy;
            go.AddComponent<PerfProbe>();
        }

        private void Awake()
        {
            _path = Path.Combine(Application.persistentDataPath, "perf.txt");
            _out = Path.Combine(Application.persistentDataPath, "perf_out.txt");
        }

        private void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 1f;
            if (!File.Exists(_path)) { _last = null; return; }
            var text = File.ReadAllText(_path);
            if (text == _last) return;
            _last = text;
            foreach (var raw in text.Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;
                Apply(line);
            }
        }

        private static void Apply(string line)
        {
            var w = line.Split(' ');
            var off = w[w.Length - 1] == "off";
            switch (w[0])
            {
                case "post":
                    foreach (var cam in Camera.allCameras)
                        cam.GetUniversalAdditionalCameraData().renderPostProcessing = !off;
                    break;
                case "dark":
                    foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                        if (r.name.Contains("Darkness")) r.enabled = !off;
                    break;
                case "particles":
                    foreach (var r in FindObjectsByType<ParticleSystemRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) r.enabled = !off;
                    break;
                case "skinned":
                    foreach (var r in FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) r.enabled = !off;
                    break;
                case "figures":
                    foreach (var f in FindObjectsByType<Campaign.CineFigure>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                        foreach (var r in f.GetComponentsInChildren<Renderer>()) r.enabled = !off;
                    break;
                case "shadows":
                    foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                        if (l.type == LightType.Directional) l.shadows = off ? LightShadows.None : LightShadows.Soft;
                    break;
                case "shader":
                    if (w.Length < 3) break;
                    foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    {
                        var m = r.sharedMaterial;
                        if (m != null && m.shader != null && m.shader.name.Contains(w[1])) r.enabled = !off;
                    }
                    break;
                case "ui":
                    foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                        if (c.isRootCanvas) c.enabled = !off;
                    break;
                case "sky":
                    foreach (var cam in Camera.allCameras)
                    {
                        cam.clearFlags = off ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox;
                        cam.backgroundColor = new Color(0.4f, 0.6f, 0.9f);
                    }
                    break;
                case "scale":
                    if (w.Length > 1 && float.TryParse(w[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var s))
                    { GraphicsQuality.OverrideScale = s; GraphicsQuality.Apply(); }
                    break;
                case "msaa":
                    if (w.Length > 1 && int.TryParse(w[1], out var n)) { GraphicsQuality.OverrideMsaa = n; GraphicsQuality.Apply(); }
                    break;
                case "census":
                    Census();
                    break;
            }
        }

        private static void Census()
        {
            var cam = Camera.main;
            var planes = cam != null ? GeometryUtility.CalculateFrustumPlanes(cam) : null;
            var rows = new Dictionary<string, (int count, long tris, float screen)>();
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (planes != null && !GeometryUtility.TestPlanesAABB(planes, r.bounds)) continue;
                var m = r.sharedMaterial;
                var key = (m != null && m.shader != null ? m.shader.name : "-") + " | " + r.GetType().Name + (m != null && m.renderQueue >= 2500 ? " | transp" : "");
                long tris = 0;
                if (r is MeshRenderer && r.TryGetComponent<MeshFilter>(out var mf) && mf.sharedMesh != null) tris = mf.sharedMesh.triangles.Length / 3;
                else if (r is SkinnedMeshRenderer smr && smr.sharedMesh != null) tris = smr.sharedMesh.triangles.Length / 3;
                var screen = 0f;
                if (cam != null)
                {
                    var b = r.bounds;
                    var a = cam.WorldToViewportPoint(b.min);
                    var c = cam.WorldToViewportPoint(b.max);
                    screen = Mathf.Clamp01(Mathf.Abs(c.x - a.x)) * Mathf.Clamp01(Mathf.Abs(c.y - a.y));
                }
                rows.TryGetValue(key, out var v);
                rows[key] = (v.count + 1, v.tris + tris, v.screen + screen);
            }
            var sb = new StringBuilder();
            sb.AppendLine($"scene {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}  cams {Camera.allCamerasCount}  scale {GraphicsQuality.AppliedScale}  {Screen.width}x{Screen.height}");
            foreach (var cm in Camera.allCameras)
                sb.AppendLine($"cam {cm.name} depth {cm.depth} post {cm.GetUniversalAdditionalCameraData().renderPostProcessing} depthTex {cm.GetUniversalAdditionalCameraData().requiresDepthTexture} far {cm.farClipPlane} hdr {cm.allowHDR} msaa {cm.allowMSAA}");
            sb.AppendLine($"skybox {(RenderSettings.skybox != null ? RenderSettings.skybox.shader.name : "-")}");
            // the UI: graphics drawn per root canvas, and the big ones (a quarter of the screen or more), which are overdraw
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!c.isRootCanvas || !c.enabled) continue;
                var drawn = 0;
                var big = new StringBuilder();
                foreach (var g in c.GetComponentsInChildren<UnityEngine.UI.Graphic>())
                {
                    if (!g.enabled) continue;
                    drawn++;
                    var rt = g.rectTransform.rect;
                    var sc = c.scaleFactor;
                    if (rt.width * rt.height * sc * sc > Screen.width * Screen.height * 0.25f)
                        big.Append($" {g.name}(a{g.canvasRenderer.GetInheritedAlpha():0.00}/{g.color.a:0.00}{(g.material != null && g.material.shader.name != "UI/Default" ? "," + g.material.shader.name : "")})");
                }
                sb.AppendLine($"canvas {c.name} order {c.sortingOrder} graphics {drawn} big:{big}");
            }
            var list = new List<KeyValuePair<string, (int count, long tris, float screen)>>(rows);
            list.Sort((x, y) => y.Value.tris.CompareTo(x.Value.tris));
            foreach (var kv in list) sb.AppendLine($"{kv.Value.count,5} r {kv.Value.tris,8} tris {kv.Value.screen,6:0.00} scr  {kv.Key}");
            File.WriteAllText(Path.Combine(Application.persistentDataPath, "perf_out.txt"), sb.ToString());
        }
    }
}
