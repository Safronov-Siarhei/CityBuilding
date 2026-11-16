using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>The piggy bank's building blocks (PiggyButton, PiggyWindow): pictures from Resources/Shop/Piggy, words.</summary>
    internal static class PiggyUi
    {
        public const string Art = "Shop/Piggy/";
        public const string ShopArt = "Shop/MenuShop/";

        /// <summary>The game's crystal (the wallet's, the shop's) -- the user 2026-11-16: not the pig's pink gem render.</summary>
        public const string Crystal = ShopArt + "Crystal";

        public static readonly Color Ink = new Color(0.29f, 0.17f, 0.086f);
        public static readonly Color Dark = new Color(0.23f, 0.12f, 0.055f);
        public static readonly Color Cream = new Color(1f, 0.957f, 0.863f);
        public static readonly Color Gold = new Color(1f, 0.83f, 0.35f);
        public static readonly Color Ribbon = new Color(0.19f, 0.055f, 0.29f);
        public static readonly Color Red = new Color(0.863f, 0.196f, 0.157f);
        public static readonly Color RedInk = new Color(0.35f, 0.055f, 0.04f);
        public static readonly Color Violet = new Color(0.47f, 0.235f, 0.667f);

        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        public static Sprite Sprite(string path)
        {
            if (!Sprites.TryGetValue(path, out var s) || s == null) Sprites[path] = s = UnityEngine.Resources.Load<Sprite>(path);
            return s;
        }

        public static T Img<T>(Transform parent, string name) where T : Graphic
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(T));
            go.transform.SetParent(parent, false);
            var g = go.GetComponent<T>();
            g.raycastTarget = false;
            return g;
        }

        /// <summary>A picture centred at `at` (canvas units, y up) sized `size`.</summary>
        public static Image Pic(Transform parent, string path, Vector2 at, Vector2 size)
        {
            var image = Img<Image>(parent, path.Substring(path.LastIndexOf('/') + 1));
            image.sprite = Sprite(path);
            image.rectTransform.sizeDelta = size;
            image.rectTransform.anchoredPosition = at;
            return image;
        }

        /// <summary>A picture `height` tall, as wide as its sprite says.</summary>
        public static Image PicH(Transform parent, string path, Vector2 at, float height)
        {
            var s = Sprite(path);
            var w = s != null ? height * s.rect.width / Mathf.Max(1f, s.rect.height) : height;
            return Pic(parent, path, at, new Vector2(w, height));
        }

        public static Text Label(Transform parent, string text, int size, Color colour, Vector2 at, Vector2 box, bool heavy = true)
        {
            var label = Img<Text>(parent, "Text");
            label.font = heavy ? UiTheme.Heavy : UiTheme.Bold;
            label.fontSize = size;
            label.color = colour;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.text = text;
            label.rectTransform.sizeDelta = box;
            label.rectTransform.anchoredPosition = at;
            return label;
        }

        public static Text Stroke(Text label, float width, Color colour)
        {
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = colour;
            outline.effectDistance = new Vector2(width, -width);
            return label;
        }

        /// <summary>«1 200» with a thin space between thousands, as the game writes crystals.</summary>
        public static string Number(int n) =>
            n.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', ' ');

        /// <summary>The red «Полна!» pill centred at `at`.</summary>
        public static RectTransform FullPill(Transform parent, Vector2 at, int fontSize)
        {
            var text = Localization.Get("#piggy_full");
            var root = new GameObject("FullPill", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(parent, false);
            root.anchoredPosition = at;
            var probe = Label(root, text, fontSize, Cream, Vector2.zero, new Vector2(300f, fontSize * 1.5f));
            var size = new Vector2(probe.preferredWidth + fontSize * 1.1f, fontSize * 1.6f);
            Object.DestroyImmediate(probe.gameObject);
            root.sizeDelta = size;
            Capsule(root, size + new Vector2(6f, 6f), RedInk);
            Capsule(root, size, Red);
            Stroke(Label(root, text, fontSize, Cream, Vector2.zero, new Vector2(300f, fontSize * 1.5f)), 2f, RedInk);
            return root;
        }

        /// <summary>A capsule of `size` centred on its parent: two round ends and a box, so its ends stay round at any width.</summary>
        public static void Capsule(Transform parent, Vector2 size, Color colour)
        {
            var h = size.y;
            var mid = Pic(parent, ShopArt + "White", Vector2.zero, new Vector2(Mathf.Max(0f, size.x - h), h));
            mid.color = colour;
            var left = Pic(parent, Art + "Dot", new Vector2(-(size.x - h) / 2f, 0f), new Vector2(h, h) * 1.03f);
            left.color = colour;
            var right = Pic(parent, Art + "Dot", new Vector2((size.x - h) / 2f, 0f), new Vector2(h, h) * 1.03f);
            right.color = colour;
        }

        /// <summary>Two-wave drift of a floating crystal (the approved button: no two crystals move alike).</summary>
        public static void Drift(float t, float phase, float period, float amp, out float dx, out float dy, out float tilt)
        {
            var w = 2f * Mathf.PI * t;
            dy = amp * (Mathf.Sin(w / period + phase) + 0.45f * Mathf.Sin(w / (period * 0.57f) + phase * 1.7f));
            dx = amp * (0.55f * Mathf.Sin(w / (period * 1.43f) + phase * 2.3f) + 0.22f * Mathf.Sin(w / (period * 0.71f) + phase * 0.6f));
            tilt = 7f * Mathf.Sin(w / (period * 1.21f) + phase) + 3f * Mathf.Sin(w / (period * 0.63f) + phase * 0.4f);
        }

        /// <summary>The pig's wobble: `every` s a burst of `len` s, easing in and out, ±`px` and ±`deg`.</summary>
        public static void Wobble(float t, float every, float len, float px, float deg, out float dx, out float rot)
        {
            dx = rot = 0f;
            var ph = t % every;
            if (ph >= len) return;
            var fade = Mathf.Pow(Mathf.Sin(Mathf.PI * ph / len), 2f);
            dx = px * fade * Mathf.Sin(2f * Mathf.PI * 3.2f * ph);
            rot = deg * fade * Mathf.Sin(2f * Mathf.PI * 3.2f * ph + 0.6f);
        }
    }
}
