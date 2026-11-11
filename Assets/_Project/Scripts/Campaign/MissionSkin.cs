using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// The campaign's screens in the sandbox HUD's own kit (the user's second playtest, 2026-11-11:
    /// «стилистика интерфейса в компании как будто выбивается из общего стиля игры, посмотри на
    /// стиль в песочнице»): the very sprites the sandbox draws with -- Resources/HudSkins/&lt;the
    /// player's look&gt;/round_main, round, round_value, plaque, panel -- and its inks, loaded at run
    /// time, so the mission follows whatever look the player chose in the settings.
    /// </summary>
    public static class MissionSkin
    {
        private static Dictionary<string, Sprite> _sprites;
        private static HudSkin _loaded;

        public static Sprite Sprite(string role)
        {
            var skin = HudSkins.Current;
            if (_sprites == null || _loaded != skin)
            {
                _sprites = new Dictionary<string, Sprite>();
                _loaded = skin;
                foreach (var s in UnityEngine.Resources.LoadAll<Sprite>($"{HudSkins.ResourcesFolder}/{skin}")) _sprites[s.name] = s;
                if (_sprites.Count == 0)
                    foreach (var s in UnityEngine.Resources.LoadAll<Sprite>($"{HudSkins.ResourcesFolder}/{HudSkin.Classic}")) _sprites[s.name] = s;
            }
            return _sprites.TryGetValue(role, out var sprite) ? sprite : null;
        }

        public static Color Ink(HudInk role) => HudSkins.Ink(role);

        /// <summary>An image in a skin role, sliced where the sprite has borders.</summary>
        public static Image Image(Transform parent, string name, string role, Vector2 anchor, Vector2 pivot, Vector2 at, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = anchor;
            r.pivot = pivot;
            r.anchoredPosition = at;
            r.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.sprite = Sprite(role);
            if (img.sprite != null && img.sprite.border.sqrMagnitude > 0f) img.type = UnityEngine.UI.Image.Type.Sliced;
            if (img.sprite == null) img.color = role == "plaque" || role == "panel" ? UiTheme.Parchment : UiTheme.Wood;
            return img;
        }

        /// <summary>A round HUD button with a glyph, as the sandbox's (CreateGlyphButton): the glyph
        /// in the look's ink for what it stands on, 56% of the button, a hair above the middle.</summary>
        public static Button Round(Transform parent, string name, string role, HudInk ink, Sprite glyph, Vector2 anchor, Vector2 at, float size)
        {
            var img = Image(parent, name, role, anchor, new Vector2(0.5f, 0.5f), at, new Vector2(size, size));
            img.type = UnityEngine.UI.Image.Type.Simple;
            var button = img.gameObject.AddComponent<Button>();
            if (glyph != null)
            {
                var g = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                g.transform.SetParent(img.transform, false);
                var gr = (RectTransform)g.transform;
                gr.anchorMin = gr.anchorMax = new Vector2(0.5f, 0.5f);
                gr.anchoredPosition = new Vector2(0f, size * 3f / 108f);
                gr.sizeDelta = new Vector2(size * 0.56f, size * 0.56f);
                var gi = g.GetComponent<Image>();
                gi.sprite = glyph;
                gi.preserveAspect = true;
                gi.color = Ink(ink);
                gi.raycastTarget = false;
            }
            return button;
        }

        public static Text Label(Transform parent, string text, Font font, int size, Color colour, TextAnchor align, Vector2 min, Vector2 max, Vector2 inset)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = min;
            r.anchorMax = max;
            r.offsetMin = inset;
            r.offsetMax = -inset;
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = align;
            t.color = colour;
            t.text = text;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        /// <summary>A time as the run's clock shows it: m:ss.t.</summary>
        public static string Clock(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            var tenths = Mathf.FloorToInt(seconds * 10f);
            return $"{tenths / 600}:{tenths / 10 % 60:00}.{tenths % 10}";
        }
    }
}
