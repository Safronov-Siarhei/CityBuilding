using System;
using System.IO;
using CityBuilder.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// The rate-us window (UI/RateUsWindow.cs) as a prefab in Resources, so it can be put up over any
    /// scene. Its pieces and their places come from Tools/store/rate_window_art.py
    /// (Assets/_Project/UI/RateUs/*.png + layout.json, rects in the 1920 x 1080 reference, y down); the
    /// words are keys on the localization sheet (#rateus_*).
    /// </summary>
    public static partial class SetupProject
    {
        private const string RateUsPrefabPath = "Assets/_Project/Resources/RateUsWindow.prefab";

        [Serializable] private class RateUsRect { public string name; public float[] rect; }
        [Serializable] private class RateUsLayout { public float[] reference; public float[] frame; public RateUsRect[] pieces; public RateUsRect[] texts; }

        private static void BuildRateUsWindow()
        {
            var layoutPath = $"{UiTextureImport.RateUsFolder}/layout.json";
            if (!File.Exists(layoutPath))
            {
                Debug.LogWarning("BuildRateUsWindow: no UI/RateUs/layout.json -- run Tools/store/rate_window_art.py.");
                return;
            }
            var layout = JsonUtility.FromJson<RateUsLayout>(File.ReadAllText(layoutPath));
            var refW = layout.reference[0];
            var refH = layout.reference[1];

            var root = new GameObject("RateUsWindow", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;   // over every scene's interface, under the loading screen (1000)
            ConfigureScaler(root.GetComponent<CanvasScaler>());
            var screen = root.AddComponent<RateUsWindow>();

            // the game behind, dimmed; a tap on it puts the window off
            var shade = CreateImage(root.transform, "Shade", new Color(0.04f, 0.03f, 0.02f, 0.63f));
            StretchFull(shade.rectTransform);
            var shadeButton = shade.gameObject.AddComponent<Button>();
            shadeButton.transition = Selectable.Transition.None;

            // the window: the reference screen's rect, centred, the pieces placed in it as drawn
            var window = new GameObject("Window", typeof(RectTransform)).GetComponent<RectTransform>();
            window.SetParent(root.transform, false);
            window.anchorMin = window.anchorMax = window.pivot = new Vector2(0.5f, 0.5f);
            window.sizeDelta = new Vector2(refW, refH);

            void Place(RectTransform rect, float[] r)
            {
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2((r[0] + r[2]) / 2f - refW / 2f, refH / 2f - (r[1] + r[3]) / 2f);
                rect.sizeDelta = new Vector2(r[2] - r[0], r[3] - r[1]);
            }

            float[] TextRect(string name) => Array.Find(layout.texts, t => t.name == name).rect;

            // the frame catches taps, so only a tap outside it reaches the shade
            var hit = CreateImage(window, "FrameHit", new Color(0f, 0f, 0f, 0f));
            Place(hit.rectTransform, layout.frame);

            Image rateImage = null;
            foreach (var piece in layout.pieces)
            {
                var image = CreateImage(window, piece.name, Color.white);
                image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UiTextureImport.RateUsFolder}/{piece.name}.png");
                image.raycastTarget = piece.name == "Band" || piece.name == "Button";
                Place(image.rectTransform, piece.rect);
                if (piece.name == "Button") rateImage = image;

                if (piece.name == "Band")
                {
                    var title = Words(window, "Title", "#rateus_title", 60, UiTheme.Heavy, new Color(1f, 0.94f, 0.78f));
                    Place(title.rectTransform, TextRect("Title"));
                    Rim(title, new Color(0.35f, 0.05f, 0.06f), 4f);
                    title.resizeTextForBestFit = true;
                    title.resizeTextMinSize = 36;
                    title.resizeTextMaxSize = 60;
                    var body = Words(window, "Body", "#rateus_body", 46, UiTheme.Regular, UiTheme.Rgb(0x543218));
                    Place(body.rectTransform, TextRect("Body"));
                    body.resizeTextForBestFit = true;
                    body.resizeTextMinSize = 28;
                    body.resizeTextMaxSize = 46;
                    body.lineSpacing = 1.1f;
                }
            }

            var rateButton = rateImage.gameObject.AddComponent<Button>();
            rateButton.targetGraphic = rateImage;
            var label = Words(rateImage.transform, "Label", "#rateus_rate", 66, UiTheme.Heavy, Color.white);
            StretchFull(label.rectTransform);
            var lr = TextRect("RateLabel");
            var br = Array.Find(layout.pieces, p => p.name == "Button").rect;
            // the label over the button's face (the piece also holds its hard shadow below)
            label.rectTransform.offsetMin = new Vector2(lr[0] - br[0], br[3] - lr[3]);
            label.rectTransform.offsetMax = new Vector2(lr[2] - br[2], br[1] - lr[1]);
            Rim(label, UiTheme.Rgb(0x1c5412), 4f);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 36;
            label.resizeTextMaxSize = 66;

            var laterText = Words(window, "Later", "#rateus_later", 44, UiTheme.Bold, UiTheme.Rgb(0x8c623a));
            Place(laterText.rectTransform, TextRect("Later"));
            laterText.raycastTarget = true;
            var laterButton = laterText.gameObject.AddComponent<Button>();
            laterButton.targetGraphic = laterText;

            var so = new SerializedObject(screen);
            so.FindProperty("group").objectReferenceValue = root.GetComponent<CanvasGroup>();
            so.FindProperty("window").objectReferenceValue = window;
            so.FindProperty("shade").objectReferenceValue = shadeButton;
            so.FindProperty("rate").objectReferenceValue = rateButton;
            so.FindProperty("later").objectReferenceValue = laterButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(RateUsPrefabPath));
            PrefabUtility.SaveAsPrefabAsset(root, RateUsPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static Text Words(Transform parent, string name, string key, int size, Font font, Color color)
        {
            var text = Localize(CreateText(parent, name, key, size, Vector2.zero, new Vector2(100f, 100f), color), key);
            text.font = font;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void Rim(Text text, Color color, float width)
        {
            // an outline drawn twice (8 directions) reads like the mock-up's stroke
            var a = text.gameObject.AddComponent<Outline>();
            a.effectColor = color;
            a.effectDistance = new Vector2(width, -width);
            var b = text.gameObject.AddComponent<Outline>();
            b.effectColor = color;
            b.effectDistance = new Vector2(width * 0.7f, width * 0.7f);
        }
    }
}
