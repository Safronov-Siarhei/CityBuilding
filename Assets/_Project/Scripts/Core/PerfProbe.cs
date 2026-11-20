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
    ///   scale 0.5 | msaa 1 | census | town 100 220 (PerfTown) | capsule off|on (citizens' CharacterController)
    ///   kinds | off &lt;Type&gt; | on &lt;Type&gt; | marks &lt;seconds&gt; (Core/PerfMarks)
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
#if CB_RELEASE
            return;   // a build for players carries no probe
#endif
#pragma warning disable CS0162
            if (!Application.isMobilePlatform) return;
            var go = new GameObject("PerfProbe");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideInHierarchy;
            go.AddComponent<PerfProbe>();
#pragma warning restore CS0162
        }

        private void Awake()
        {
            _self = this;
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
                // town 100 220 -- a big town round the hall (PerfTown), result in the log and perf_out.txt
                case "town":
                    if (w.Length > 2 && int.TryParse(w[1], out var b) && int.TryParse(w[2], out var p))
                    {
                        var said = PerfTown.Build(b, p);
                        Debug.Log("[PerfProbe] " + said);
                        File.WriteAllText(Path.Combine(Application.persistentDataPath, "perf_out.txt"), said);
                    }
                    break;
                // capsule off -- citizens walk without their CharacterController (CitizenAgent.SimpleWalk); on -- with it
                // kinds -- every enabled script kind that has an Update/LateUpdate, by count, into perf_out.txt
                case "kinds":
                    Kinds();
                    break;
                // off <TypeName> / on <TypeName> -- switch every one of that kind (a script, or a CharacterController) off and back
                case "off":
                case "on":
                    if (w.Length > 1) Switch(w[1], w[0] == "off");
                    break;
                // marks <seconds> -- Core/PerfMarks (the army's insides) for that long, ms per frame into perf_out.txt
                case "marks":
                    if (w.Length > 1 && float.TryParse(w[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var secs) && _self != null)
                        _self.StartCoroutine(_self.Marks(secs));
                    break;
                case "capsule":
                    Citizens.CitizenAgent.SimpleWalkOverride = off;
                    Debug.Log("[PerfProbe] citizens' capsule " + (off ? "OFF" : "ON"));
                    break;
            }
        }

        private const char NL = (char)10;
        private static PerfProbe _self;
        private static readonly List<Component> _switchedOff = new List<Component>();

        private static void Write(string text)
        {
            File.WriteAllText(Path.Combine(Application.persistentDataPath, "perf_out.txt"), text);
            Debug.Log("[PerfProbe] " + text.Replace(NL, ';'));
        }

        private static void Kinds()
        {
            const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            var counts = new Dictionary<System.Type, int>();
            foreach (var b in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!b.enabled || !b.gameObject.activeInHierarchy) continue;
                var t = b.GetType();
                counts.TryGetValue(t, out var n);
                counts[t] = n + 1;
            }
            var rows = new List<(string name, int n, bool upd)>();
            foreach (var kv in counts)
            {
                var upd = kv.Key.GetMethod("Update", F) != null || kv.Key.GetMethod("LateUpdate", F) != null;
                rows.Add((kv.Key.Name, kv.Value, upd));
            }
            rows.Sort((a, b) => b.n.CompareTo(a.n));
            var sb = new StringBuilder();
            foreach (var r in rows) if (r.upd) sb.Append(r.name).Append(' ').Append(r.n).Append(NL);
            int cc = 0, ccOn = 0;
            foreach (var c in FindObjectsByType<CharacterController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) { cc++; if (c.enabled) ccOn++; }
            sb.Append("CharacterController ").Append(ccOn).Append(" on of ").Append(cc).Append(NL);
            Write(sb.ToString());
        }

        private static void Switch(string typeName, bool off)
        {
            if (!off)
            {
                var back = 0;
                for (var i = _switchedOff.Count - 1; i >= 0; i--)
                {
                    var c = _switchedOff[i];
                    if (c == null || c.GetType().Name != typeName) continue;
                    if (c is Behaviour b) b.enabled = true; else if (c is Collider col) col.enabled = true;
                    _switchedOff.RemoveAt(i);
                    back++;
                }
                Debug.Log("[PerfProbe] on " + typeName + " " + back);
                return;
            }
            var n = 0;
            foreach (var c in FindObjectsByType<Component>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (c == null || c.GetType().Name != typeName) continue;
                if (c is Behaviour b && b.enabled) { b.enabled = false; _switchedOff.Add(c); n++; }
                else if (c is Collider col && col.enabled) { col.enabled = false; _switchedOff.Add(c); n++; }
            }
            Debug.Log("[PerfProbe] off " + typeName + " " + n);
        }

        private System.Collections.IEnumerator Marks(float seconds)
        {
            foreach (var m in PerfMarks.All) m.Reset();
            PerfMarks.On = true;
            var frames = 0;
            var until = Time.unscaledTime + seconds;
            while (Time.unscaledTime < until) { frames++; yield return null; }
            PerfMarks.On = false;
            var sb = new StringBuilder();
            foreach (var m in PerfMarks.All)
                sb.Append(m.Name).Append(' ').Append((m.Ms / Mathf.Max(1, frames)).ToString("0.000")).Append(" ms/frame, ")
                  .Append(((double)m.Calls / Mathf.Max(1, frames)).ToString("0.0")).Append(" calls").Append(NL);
            Write(sb.ToString());
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
