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
            var paper = SplashTexture("paper.png", 2048);
            var fillTex = SplashTexture("fill.png", 512);
            var penTex = SplashTexture("pen.png", 512);
            var soak = SplashSoakMaterial();
            var data = SplashInkData.Parse(dataJson.text);
            var paperColour = SplashInkData.Hex(data.paper, Color.white);

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

            var paperImg = SplashRaw("Paper", canvasGO.transform, paper);
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

        /// <summary>A splash picture as a plain clamped, mip-less texture (the paper's grain must stay crisp).</summary>
        private static Texture2D SplashTexture(string file, int maxSize)
        {
            var path = $"{SplashFolder}/{file}";
            if (AssetImporter.GetAtPath(path) is TextureImporter imp)
            {
                var changed = imp.textureType != TextureImporterType.Default || imp.mipmapEnabled
                              || imp.wrapMode != TextureWrapMode.Clamp || !imp.alphaIsTransparency
                              || imp.maxTextureSize != maxSize || imp.npotScale != TextureImporterNPOTScale.None;
                imp.textureType = TextureImporterType.Default;
                imp.mipmapEnabled = false;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.alphaIsTransparency = true;
                imp.npotScale = TextureImporterNPOTScale.None;
                imp.maxTextureSize = maxSize;
                if (changed) imp.SaveAndReimport();
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) Debug.LogError($"[SetupProject] {path} is missing -- run Design/branding/depigo/splash/export_splash_unity.py.");
            return tex;
        }

        /// <summary>The ink-soak material; an asset referenced by the scene, so its shader goes into every build.</summary>
        private static Material SplashSoakMaterial()
        {
            var shader = Shader.Find("CityBuilder/UI/InkSoak");
            if (shader == null)
            {
                Debug.LogError("[SetupProject] Shader CityBuilder/UI/InkSoak not found -- the splash's petals will show without the soak.");
                return null;
            }
            var path = $"{MaterialsFolder}/SplashInkSoak.mat";
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
