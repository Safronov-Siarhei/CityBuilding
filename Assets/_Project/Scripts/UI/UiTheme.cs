using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// The interface's look -- style "D" from the mockups: rounded pills and panels with a thick
    /// dark-wood outline and a hard shadow, parchment surfaces, gold accents, the Rubik typeface.
    ///
    /// The surfaces are sprites baked in their own colours (Assets/_Project/UI/Skin, drawn by
    /// Tools/ui/make_skin.py), so this holds what code still has to choose: the ink a label is
    /// written in, the fonts, and the translation of the colours the UI used to be designed in.
    ///
    /// That translation is the point of <see cref="OnParchment"/>. The old interface was light
    /// text on dark stone, and its colours -- white, a pale red for "can't afford", a soft green
    /// for "ready" -- are spread through every panel and controller. On parchment each needs to be
    /// its dark counterpart instead. Rather than each script re-deciding its palette, they pass
    /// the colour they always used through this, and the meaning (neutral, warning, good) is kept.
    /// </summary>
    public static class UiTheme
    {
        public static readonly Color Ink = Rgb(0x3b2414);
        public static readonly Color InkSoft = Rgb(0x7a5532);
        /// <summary>Text written on dark wood (a status pill, an idle tab): the parchment colour.</summary>
        public static readonly Color OnWood = Rgb(0xf3e2b8);
        public static readonly Color Parchment = Rgb(0xf3e2b8);
        public static readonly Color Wood = Rgb(0x7a4a22);
        public static readonly Color WoodEdge = Rgb(0x5a3218);
        public static readonly Color Gold = Rgb(0xe0a83a);
        public static readonly Color Danger = Rgb(0xb5321e);
        public static readonly Color Warning = Rgb(0xa8600c);
        public static readonly Color Good = Rgb(0x3f7f2c);
        public static readonly Color Info = Rgb(0x2c4fa8);
        public static readonly Color Magic = Rgb(0x5b3aa8);

        // Tints for a list row or a tab drawn on the skin's card or pill (multiplied over the
        // sprite's own parchment): plain, picked, done/idle, locked, a section header.
        public static readonly Color RowNormal = Color.white;
        public static readonly Color RowSelected = Rgb(0xcfe8b4);
        public static readonly Color RowDim = Rgb(0xdcd4c4);
        public static readonly Color RowLocked = Rgb(0xb3aa9b);
        public static readonly Color RowHeader = Rgb(0xe0c48f);
        public static readonly Color TabSelected = Color.white;
        public static readonly Color TabIdle = Rgb(0xc8b088);

        private static Font _regular;
        private static Font _bold;
        private static Font _heavy;

        /// <summary>Rubik Medium -- body text, numbers.</summary>
        public static Font Regular => Load(ref _regular, "Fonts/Rubik-Medium");

        /// <summary>Rubik Bold -- titles, button captions.</summary>
        public static Font Bold => Load(ref _bold, "Fonts/Rubik-Bold");

        /// <summary>Rubik ExtraBold -- the big numbers on the resource bar.</summary>
        public static Font Heavy => Load(ref _heavy, "Fonts/Rubik-ExtraBold");

        private static Font Load(ref Font cache, string path)
        {
            if (cache != null) return cache;
            cache = UnityEngine.Resources.Load<Font>(path);
            // A build without the font still has to show text: the engine's own face stands in.
            if (cache == null) cache = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return cache;
        }

        public static Color Rgb(int rgb, float alpha = 1f)
        {
            return new Color(((rgb >> 16) & 0xff) / 255f, ((rgb >> 8) & 0xff) / 255f, (rgb & 0xff) / 255f, alpha);
        }

        /// <summary>
        /// What a colour designed for the old dark interface becomes on parchment. A colour that is
        /// already dark was meant for a light surface and passes through untouched. Greys and white
        /// become ink (a dimmed one -- alpha below 0.9, or a grey -- the softer ink, which is what
        /// "secondary text" meant before); anything with real colour keeps its hue family but is
        /// written dark enough to read: reds and oranges as danger, yellows as warning, greens as
        /// good, blues as info, violets as magic. Alpha is carried over.
        /// </summary>
        public static Color OnParchment(Color legacy)
        {
            Color.RGBToHSV(legacy, out var h, out var s, out var v);
            // Already a colour for a light surface (ink, a dark red): nothing to translate.
            if (v < 0.55f) return legacy;
            Color result;
            if (s < 0.25f)
            {
                result = legacy.a < 0.9f || v < 0.85f ? InkSoft : Ink;
                return new Color(result.r, result.g, result.b, Mathf.Max(legacy.a, 0.9f));
            }

            if (h < 0.07f || h > 0.92f) result = Danger;
            else if (h < 0.17f) result = Warning;
            else if (h < 0.47f) result = Good;
            else if (h < 0.75f) result = Info;
            else result = Magic;
            return new Color(result.r, result.g, result.b, legacy.a);
        }
    }
}
