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
    /// The loading screen (UI/LoadingScreen.cs) as a prefab in Resources, so it can be put up from any
    /// scene and live across the change: since 2026-11-10 (the user) a painted map covering the screen,
    /// a course along the bottom with a flat ink galleon sailing it, a tip above; the art comes from
    /// Tools/loading/loading_maps.py.
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

            // under everything a dark page, so nothing of the scenes changing shows at any edge
            var page = CreateImage(root.transform, "Page", new Color(0.24f, 0.16f, 0.09f));
            StretchFull(page.rectTransform);

            // ---- the map: a painted chart covering the screen, never stretched (LoadingScreen picks it)
            var map = CreateImage(root.transform, "Map", Color.white);
            map.raycastTarget = false;
            StretchFull(map.rectTransform);
            var fitter = map.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 20f / 9f;

            // ---- the course along the bottom: anchor, dashed ahead, red behind, the cross, the ship
            var course = new GameObject("Course", typeof(RectTransform)).GetComponent<RectTransform>();
            course.SetParent(root.transform, false);
            course.anchorMin = new Vector2(0.1f, 0f);
            course.anchorMax = new Vector2(0.9f, 0f);
            course.pivot = new Vector2(0.5f, 0.5f);
            course.anchoredPosition = new Vector2(0f, 92f);
            course.sizeDelta = new Vector2(0f, 80f);
            var dashed = CreateImage(course, "Dashed", Color.white);
            dashed.sprite = LoadingArt("Route_Dashed");
            dashed.raycastTarget = false;
            StretchFull(dashed.rectTransform);
            var sailed = CreateImage(course, "Sailed", Color.white);
            sailed.sprite = LoadingArt("Route_Sailed");
            sailed.raycastTarget = false;
            sailed.type = Image.Type.Filled;
            sailed.fillMethod = Image.FillMethod.Horizontal;
            sailed.fillOrigin = (int)Image.OriginHorizontal.Left;
            sailed.fillAmount = 0f;
            StretchFull(sailed.rectTransform);
            var anchor = CreateImage(course, "Anchor", Color.white);
            anchor.sprite = LoadingArt("Route_Anchor");
            anchor.raycastTarget = false;
            Anchor(anchor.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-46f, 0f), new Vector2(76f, 76f));
            var cross = CreateImage(course, "Cross", Color.white);
            cross.sprite = LoadingArt("Route_Cross");
            cross.raycastTarget = false;
            Anchor(cross.rectTransform, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(46f, 0f), new Vector2(76f, 76f));
            var ship = CreateImage(course, "Ship", Color.white);
            ship.sprite = LoadingArt("Ship");
            ship.raycastTarget = false;
            // the sprite is 340 x 240 with its waterline 30 px up at x 160 (loading_maps.py ship_sprite)
            Anchor(ship.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(160f / 340f, 30f / 240f), Vector2.zero, new Vector2(204f, 144f));

            // ---- the tip, in ink over the sea above the course, a paper rim so it reads over land too
            var tip = CreateText(root.transform, "Tip", string.Empty, 34, Vector2.zero, new Vector2(1600f, 64f), UiTheme.Ink);
            Anchor(tip.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 196f), new Vector2(1600f, 64f));
            tip.font = UiTheme.Bold;
            tip.resizeTextForBestFit = true;
            tip.resizeTextMinSize = 22;
            tip.resizeTextMaxSize = 34;
            tip.raycastTarget = false;
            var rim = tip.gameObject.AddComponent<Outline>();
            rim.effectColor = new Color(0.95f, 0.9f, 0.78f, 0.85f);
            rim.effectDistance = new Vector2(2f, -2f);

            // the maps under Resources: how many of each shape (Map_Wide_N / Map_Tall_N)
            var variants = 0;
            while (File.Exists($"{UiTextureImport.LoadingFolder}/Resources/LoadingMaps/Map_Wide_{variants + 1}.png")
                   && File.Exists($"{UiTextureImport.LoadingFolder}/Resources/LoadingMaps/Map_Tall_{variants + 1}.png")) variants++;
            if (variants == 0) Debug.LogWarning("BuildLoadingScreen: no maps in UI/Loading/Resources/LoadingMaps -- run Tools/loading/loading_maps.py.");

            var so = new SerializedObject(screen);
            so.FindProperty("group").objectReferenceValue = root.GetComponent<CanvasGroup>();
            so.FindProperty("map").objectReferenceValue = map;
            so.FindProperty("mapFitter").objectReferenceValue = fitter;
            so.FindProperty("course").objectReferenceValue = course;
            so.FindProperty("sailed").objectReferenceValue = sailed;
            so.FindProperty("ship").objectReferenceValue = ship.rectTransform;
            so.FindProperty("tip").objectReferenceValue = tip;
            so.FindProperty("mapVariants").intValue = Mathf.Max(1, variants);
            // one pool of tips (the user, 2026-11-10: #tip_1..30, shown at random)
            WriteStrings(so.FindProperty("tips"), TipKeys("#tip_"));
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
