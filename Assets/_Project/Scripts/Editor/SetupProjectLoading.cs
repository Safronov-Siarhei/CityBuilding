using System.Collections.Generic;
using System.IO;
using CityBuilder.Core;
using CityBuilder.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// The drawn loading screen (UI/LoadingScreen.cs; the user, 2026-10-25) as a prefab in
    /// Resources, so it can be put up from any scene and live across the change: a parchment page
    /// with the sandbox map's ink chart and its quill, the Invasion's painted scene with its wooden
    /// plank and runner, and a tip at the foot.
    /// </summary>
    public static partial class SetupProject
    {
        private const string LoadingPrefabPath = "Assets/_Project/Resources/LoadingScreen.prefab";

        private static Sprite LoadingArt(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{UiTextureImport.LoadingFolder}/{name}.png");

        private static void BuildLoadingScreen()
        {
            var root = new GameObject("LoadingScreen", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            ConfigureScaler(root.GetComponent<CanvasScaler>());
            var screen = root.AddComponent<LoadingScreen>();

            // the page under everything: parchment, which also hides the scenes changing behind it
            var page = CreateImage(root.transform, "Page", UiTheme.Parchment);
            StretchFull(page.rectTransform);

            // ---- the sandbox: the chart drawn in by the quill
            var sandbox = StretchedChild(root.transform, "Sandbox");
            var under = CreateImage(sandbox, "ChartFaint", new Color(1f, 1f, 1f, 0.16f));
            under.preserveAspect = true;
            Anchor(under.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(780f, 780f));
            var chart = CreateImage(sandbox, "Chart", Color.white);
            chart.preserveAspect = true;
            chart.type = Image.Type.Filled;
            chart.fillMethod = Image.FillMethod.Horizontal;
            chart.fillOrigin = (int)Image.OriginHorizontal.Left;
            Anchor(chart.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(780f, 780f));
            var quill = CreateImage(chart.transform, "Quill", Color.white);
            quill.sprite = LoadingArt("Quill");
            quill.preserveAspect = true;
            quill.raycastTarget = false;
            // the nib is the sprite's lower-left corner
            Anchor(quill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.08f, 0.08f), Vector2.zero, new Vector2(170f, 170f));
            var mapName = CreateText(sandbox, "MapName", string.Empty, 52, new Vector2(0f, -400f), new Vector2(1000f, 70f), UiTheme.Ink);
            mapName.font = UiTheme.Heavy;

            // ---- the invasion: the painted scene, the plank and its runner
            var invasion = StretchedChild(root.transform, "Invasion");
            var scene = CreateImage(invasion, "Scene", Color.white);
            StretchFull(scene.rectTransform);
            var fitter = scene.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;
            var plank = Plaque(invasion, "Plank");
            Anchor(plank.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1100f, 50f));
            var fill = CreateImage(plank.transform, "Fill", new Color(0.62f, 0.38f, 0.17f));
            // a Filled image without a sprite draws whole: the plaque's own, tinted wood
            fill.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UiTextureImport.HudSkinsFolder}/Classic/card.png");
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(12f, 12f);
            fillRect.offsetMax = new Vector2(-12f, -16f);
            var runner = CreateImage(plank.transform, "Runner", Color.white);
            runner.sprite = LoadingArt("Runner_A");
            runner.preserveAspect = true;
            runner.raycastTarget = false;
            Anchor(runner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.08f), new Vector2(0f, 14f), new Vector2(130f, 130f));
            // the figure was drawn facing left: it runs to the right along the plank
            runner.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

            // ---- the tip, on a plaque so it reads over the painting too
            var tipPlaque = Plaque(root.transform, "TipPlaque");
            Anchor(tipPlaque.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 64f), new Vector2(1400f, 76f));
            var tip = CreateText(tipPlaque.transform, "Tip", string.Empty, 30, new Vector2(0f, 3f), new Vector2(1340f, 64f), UiTheme.Ink);
            tip.font = UiTheme.Regular;
            tip.resizeTextForBestFit = true;
            tip.resizeTextMinSize = 20;
            tip.resizeTextMaxSize = 30;

            // the pictures: a chart per sandbox map, a scene per theme
            var chartKeys = new List<string>();
            var charts = new List<Object>();
            foreach (var id in Maps.MapSelector.PlayerMapIds)
            {
                var art = LoadingArt($"Ink_{id}");
                if (art == null) continue;
                chartKeys.Add(id);
                charts.Add(art);
            }
            var sceneKeys = new List<string>();
            var scenes = new List<Object>();
            foreach (var theme in MapThemes.All)
            {
                var art = LoadingArt($"Load_{MapThemes.Id(theme)}");
                if (art == null) continue;
                sceneKeys.Add(MapThemes.Id(theme));
                scenes.Add(art);
            }

            var so = new SerializedObject(screen);
            so.FindProperty("group").objectReferenceValue = root.GetComponent<CanvasGroup>();
            so.FindProperty("sandboxRoot").objectReferenceValue = sandbox.gameObject;
            so.FindProperty("chart").objectReferenceValue = chart;
            so.FindProperty("chartFaint").objectReferenceValue = under;
            so.FindProperty("quill").objectReferenceValue = quill.rectTransform;
            so.FindProperty("mapName").objectReferenceValue = mapName;
            so.FindProperty("invasionRoot").objectReferenceValue = invasion.gameObject;
            so.FindProperty("scene").objectReferenceValue = scene;
            so.FindProperty("plankFill").objectReferenceValue = fill;
            so.FindProperty("plank").objectReferenceValue = fillRect;
            so.FindProperty("runner").objectReferenceValue = runner;
            WriteArray(so.FindProperty("runnerFrames"), new Object[] { LoadingArt("Runner_A"), LoadingArt("Runner_B") });
            so.FindProperty("tip").objectReferenceValue = tip;
            WriteStrings(so.FindProperty("chartKeys"), chartKeys);
            WriteArray(so.FindProperty("charts"), charts.ToArray());
            WriteStrings(so.FindProperty("sceneKeys"), sceneKeys);
            WriteArray(so.FindProperty("scenes"), scenes.ToArray());
            WriteStrings(so.FindProperty("sandboxTips"), TipKeys("#tip_sandbox_"));
            WriteStrings(so.FindProperty("invasionTips"), TipKeys("#tip_invasion_"));
            so.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(LoadingPrefabPath));
            PrefabUtility.SaveAsPrefabAsset(root, LoadingPrefabPath);
            Object.DestroyImmediate(root);
        }

        /// <summary>The HUD's Classic parchment plaque, sliced -- loaded directly, not through the HUD's look switch (this prefab is no scene's).</summary>
        private static Image Plaque(Transform parent, string name)
        {
            var image = CreateImage(parent, name, Color.white);
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UiTextureImport.HudSkinsFolder}/Classic/plaque.png");
            image.type = Image.Type.Sliced;
            return image;
        }

        /// <summary>The tips' keys in the localization tab, in its order: every row starting with `prefix`.</summary>
        private static List<string> TipKeys(string prefix)
        {
            var keys = new List<string>();
            foreach (var line in File.ReadAllLines("Assets/_Project/Balance/localization.csv"))
            {
                if (line.StartsWith(prefix)) keys.Add(line.Substring(0, line.IndexOf(',')));
            }
            return keys;
        }
    }
}
