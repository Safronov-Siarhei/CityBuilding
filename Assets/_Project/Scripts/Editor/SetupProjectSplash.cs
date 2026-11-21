using System.IO;
using CityBuilder.Core;
using CityBuilder.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// The studio splash scene «Чернильное перо» (Design/features/studio-splash.md; the user, 2026-11-21), the
    /// first scene of the build: paper, the drawing (pen strokes + ink soak + quill) zoomed slowly as one, and a
    /// black fade on top. Its art and timeline come from Design/branding/depigo/splash/export_splash_unity.py
    /// (UI/Splash/: splash_ink.json, paper.png, fill.png, pen.png) -- regenerate them there, not here.
    /// </summary>
    public static partial class SetupProject
    {
        private static void BuildSplashScene()
        {
            var dataJson = AssetDatabase.LoadAssetAtPath<TextAsset>($"{SplashFolder}/splash_ink.json");
            if (dataJson == null)
            {
                Debug.LogError($"[SetupProject] {SplashFolder}/splash_ink.json is missing -- run Design/branding/depigo/splash/export_splash_unity.py. The splash scene is not built.");
                return;
            }
            // fill: one alpha channel at 1024, pen: full colour -- both UNCOMPRESSED (the user, 2026-11-21: on the
            // tablet the splash looked short of resolution; ASTC smeared the petals' edges and the vein cuts)
            var fillTex = SplashTexture("fill.png", 1024, alphaOnly: true);
            var penTex = SplashTexture("pen.png", 512, alphaOnly: false);
            var soak = SplashMaterial("SplashInkSoak", "CityBuilder/UI/InkSoak");
            var data = SplashInkData.Parse(dataJson.text);
            var paperColour = SplashInkData.Hex(data.paper, Color.white);
            var paperMat = SplashMaterial("SplashPaper", "CityBuilder/UI/SplashPaper");
            var look = data.paperLook;
            if (paperMat != null && look != null)
            {
                paperMat.SetColor("_VignetteColor", SplashInkData.Hex(look.vignette, Color.black));
                paperMat.SetVector("_Vignette", new Vector4(look.cx, look.cy, look.r, look.inner));
                paperMat.SetFloat("_VignetteAlpha", look.vignetteAlpha);
                paperMat.SetColor("_GrainColor", SplashInkData.Hex(look.grain, Color.black));
                paperMat.SetFloat("_GrainAlpha", look.grainAlpha);
                paperMat.SetFloat("_GrainFreq", look.grainFreq);
                EditorUtility.SetDirty(paperMat);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGO = new GameObject("Splash Camera", typeof(Camera));
            var cam = camGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = paperColour;
            cam.cullingMask = 0;                    // nothing in the world: the canvas is an overlay
            camGO.AddComponent<FrameRateController>();   // the menu scene's 60 fps cap, from the first frame

            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            ConfigureScaler(canvasGO.GetComponent<CanvasScaler>());

            // the paper: no picture, the SplashPaper shader draws colour + vignette + grain at the screen's own resolution
            var paperImg = SplashRaw("Paper", canvasGO.transform, null);
            paperImg.color = paperColour;
            paperImg.material = paperMat;
            Stretch(paperImg.rectTransform);

            var content = new GameObject("Drawing", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(canvasGO.transform, false);
            content.sizeDelta = new Vector2(data.width, data.height);

            var fill = SplashRaw("InkFill", content, fillTex);
            fill.material = soak;

            var strokesGO = new GameObject("Strokes", typeof(RectTransform), typeof(CanvasRenderer), typeof(InkStrokeGraphic));
            strokesGO.transform.SetParent(content, false);
            var strokes = strokesGO.GetComponent<InkStrokeGraphic>();
            strokes.raycastTarget = false;
            strokes.rectTransform.sizeDelta = new Vector2(data.width, data.height);

            var pen = SplashRaw("Pen", content, penTex);

            var fadeGO = new GameObject("Fade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fadeGO.transform.SetParent(canvasGO.transform, false);
            var fade = fadeGO.GetComponent<Image>();
            fade.color = Color.black;
            fade.raycastTarget = false;
            Stretch(fade.rectTransform);

            var splash = canvasGO.AddComponent<SplashInk>();
            var so = new SerializedObject(splash);
            so.FindProperty("dataJson").objectReferenceValue = dataJson;
            so.FindProperty("nextScene").stringValue = "MainMenu";
            so.FindProperty("content").objectReferenceValue = content;
            so.FindProperty("strokes").objectReferenceValue = strokes;
            so.FindProperty("fill").objectReferenceValue = fill;
            so.FindProperty("pen").objectReferenceValue = pen.rectTransform;
            so.FindProperty("fade").objectReferenceValue = fade;
            so.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(ScenesFolder);
            DeleteIfExists(SplashScenePath);
            EditorSceneManager.SaveScene(scene, SplashScenePath);
        }

        private const string SplashFolder = "Assets/_Project/UI/Splash";
        private const string SplashScenePath = ScenesFolder + "/Splash.unity";

        private static RawImage SplashRaw(string name, Transform parent, Texture tex)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<RawImage>();
            img.texture = tex;
            img.raycastTarget = false;
            return img;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// A splash picture as a clamped, mip-less, UNCOMPRESSED texture on every platform: Alpha8 when only its
        /// shape matters (the ink's colour comes from the vertex colour), RGBA32 otherwise. Both are small.
        /// </summary>
        private static Texture2D SplashTexture(string file, int maxSize, bool alphaOnly)
        {
            var path = $"{SplashFolder}/{file}";
            if (AssetImporter.GetAtPath(path) is TextureImporter imp)
            {
                imp.textureType = alphaOnly ? TextureImporterType.SingleChannel : TextureImporterType.Default;
                if (alphaOnly)
                {
                    var s = new TextureImporterSettings();
                    imp.ReadTextureSettings(s);
                    s.singleChannelComponent = TextureImporterSingleChannelComponent.Alpha;
                    imp.SetTextureSettings(s);
                }
                imp.mipmapEnabled = false;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.alphaIsTransparency = !alphaOnly;
                imp.npotScale = TextureImporterNPOTScale.None;
                imp.maxTextureSize = maxSize;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                foreach (var platform in new[] { "Android", "Standalone", "iPhone" })
                {
                    var ps = imp.GetPlatformTextureSettings(platform);
                    ps.overridden = true;
                    ps.maxTextureSize = maxSize;
                    ps.format = alphaOnly ? TextureImporterFormat.Alpha8 : TextureImporterFormat.RGBA32;
                    ps.textureCompression = TextureImporterCompression.Uncompressed;
                    imp.SetPlatformTextureSettings(ps);
                }
                imp.SaveAndReimport();
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) Debug.LogError($"[SetupProject] {path} is missing -- run Design/branding/depigo/splash/export_splash_unity.py.");
            return tex;
        }

        /// <summary>A splash material; an asset referenced by the scene, so its shader goes into every build.</summary>
        private static Material SplashMaterial(string name, string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"[SetupProject] Shader {shaderName} not found -- the splash draws without it.");
                return null;
            }
            var path = $"{MaterialsFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else mat.shader = shader;
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
