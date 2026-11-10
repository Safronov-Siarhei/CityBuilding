using System.IO;
using System.Linq;
using CityBuilder.Campaign;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// Photographs the opening cutscene's village for the user (run.ps1 -Steps campaign, with
    /// graphics): opens the scene, lays the village out in the editor and renders a few views to
    /// RunLogs/campaign/. The scene is not saved afterwards.
    /// </summary>
    public static class CampaignPhotos
    {
        private static readonly (string name, Vector3 at, Vector3 from, float fov)[] Views =
        {
            ("1_over_village", new Vector3(0f, 0f, 2f), new Vector3(-26f, 22f, -34f), 40f),
            ("2_square", new Vector3(0f, 0.6f, 1.5f), new Vector3(3.2f, 3.2f, -10f), 45f),
            ("3_market_street", new Vector3(1.2f, 0.6f, -4.5f), new Vector3(-0.6f, 1.6f, -11f), 45f),
            ("4_church", new Vector3(0f, 1.6f, 6.5f), new Vector3(-4.5f, 1.4f, -1.5f), 45f),
            ("5_far_fields", new Vector3(0f, 0f, 0f), new Vector3(60f, 30f, -55f), 40f),
            ("6_street_level", new Vector3(14f, 0.8f, 3f), new Vector3(5f, 1.1f, 0.6f), 50f),
        };

        public static void Capture()
        {
            EditorSceneManager.OpenScene(SetupProject.CampaignOpeningScenePath);
            var village = Object.FindAnyObjectByType<OpeningVillage>();
            village.Build();
            Debug.Log($"[CampaignPhotos] village: {village.BuildingCount} buildings");
            var cam = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "RunLogs", "campaign");
            Directory.CreateDirectory(dir);
            var rt = new RenderTexture(1280, 720, 24) { antiAliasing = 4 };
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            // the first render skipped the ground while its shader still compiled in the background
            // (the first view showed the sky's lower half instead of the grass): compile up front
            // and throw a warm-up frame away
            var wasAsync = EditorSettings.asyncShaderCompilation;   // put back at the end: it is a project setting
            EditorSettings.asyncShaderCompilation = false;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
            foreach (var (name, at, from, fov) in Views)
            {
                cam.transform.SetPositionAndRotation(from, Quaternion.LookRotation(at - from));
                cam.fieldOfView = fov;
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
                RenderTexture.active = null;
                cam.targetTexture = null;
            }
            Debug.Log($"[CampaignPhotos] wrote {Views.Length} views to {dir}");

            // the cast in a row on the square's west side, facing the camera: once standing, once running
            var cast = Directory.Exists(ModelImportDefaults.CinematicCastFolder)
                ? Directory.GetFiles(ModelImportDefaults.CinematicCastFolder, "*.fbx").Select(f => f.Replace('\\', '/')).ToArray()
                : new string[0];
            var figures = new (GameObject go, AnimationClip[] clips)[cast.Length];
            for (var i = 0; i < cast.Length; i++)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(cast[i]);
                var go = (GameObject)Object.Instantiate(model);
                go.transform.SetPositionAndRotation(new Vector3(-3.2f, 0f, -2.5f + 5f * i / Mathf.Max(1, cast.Length - 1)), Quaternion.Euler(0f, -90f, 0f));   // a figure faces Unity +Z
                figures[i] = (go, AssetDatabase.LoadAllAssetsAtPath(cast[i]).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToArray());
            }
            Debug.Log($"[CampaignPhotos] cast: {string.Join(", ", figures.Select(f => f.go.name + "[" + string.Join("/", f.clips.Select(c => c.name + (c.isLooping ? "*" : ""))) + "]"))}");
            foreach (var (shot, clipName, time) in new[] { ("7_cast_idle", "Idle", 0.5f), ("8_cast_run", "Run", 0.2f), ("9_cast_walk", "Walk", 0.3f) })
            {
                foreach (var (go, clips) in figures)
                {
                    // standing they face the camera; walking and running they go across it, side-on
                    go.transform.rotation = Quaternion.Euler(0f, clipName == "Idle" ? -90f : 0f, 0f);
                    var clip = clips.FirstOrDefault(c => c.name == clipName);
                    if (clip != null) Pose(go, clip, time);
                }
                var from = new Vector3(-7f, 0.75f, 0f);
                cam.transform.SetPositionAndRotation(from, Quaternion.LookRotation(new Vector3(-3.2f, 0.32f, 0f) - from));
                cam.fieldOfView = 50f;
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(dir, shot + ".png"), tex.EncodeToPNG());
                RenderTexture.active = null;
                cam.targetTexture = null;
            }
            EditorSettings.asyncShaderCompilation = wasAsync;
        }

        /// <summary>Puts a figure in a clip's pose at a time by reading the clip's curves straight
        /// onto its bones (in the editor, without play mode, nothing else poses an imported clip).</summary>
        private static void Pose(GameObject go, AnimationClip clip, float time)
        {
            var values = new System.Collections.Generic.Dictionary<string, float>();
            foreach (var b in AnimationUtility.GetCurveBindings(clip))
            {
                values[b.path + "|" + b.propertyName] = AnimationUtility.GetEditorCurve(clip, b).Evaluate(time);
            }
            foreach (var t in go.GetComponentsInChildren<Transform>())
            {
                var path = AnimationUtility.CalculateTransformPath(t, go.transform);
                if (values.TryGetValue(path + "|m_LocalRotation.w", out var w))
                {
                    t.localRotation = new Quaternion(values[path + "|m_LocalRotation.x"], values[path + "|m_LocalRotation.y"],
                        values[path + "|m_LocalRotation.z"], w).normalized;
                }
                if (values.TryGetValue(path + "|m_LocalPosition.x", out var x))
                {
                    t.localPosition = new Vector3(x, values[path + "|m_LocalPosition.y"], values[path + "|m_LocalPosition.z"]);
                }
            }
        }
    }
}