using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// The pictures the event feed draws with, gathered once at scene-generation time (see
    /// SetupProject.CreateEventIconLibrary): every icon rendered from the models, by file name --
    /// building ids and "Res_" resources alike -- and one glyph per <see cref="EventGlyph"/>.
    /// </summary>
    public class EventIconLibrary : ScriptableObject
    {
        [SerializeField] private string[] pictureNames = new string[0];
        [SerializeField] private Sprite[] pictures = new Sprite[0];

        /// <summary>Indexed by (int)EventGlyph; None's entry is empty.</summary>
        [SerializeField] private Sprite[] glyphs = new Sprite[0];

        public void EditorInitialize(string[] names, Sprite[] sprites, Sprite[] glyphSprites)
        {
            pictureNames = names;
            pictures = sprites;
            glyphs = glyphSprites;
        }

        /// <summary>A linear search over some sixty names, run once per event rather than per frame.</summary>
        public Sprite GetPicture(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            for (var i = 0; i < pictureNames.Length && i < pictures.Length; i++)
            {
                if (pictureNames[i] == name) return pictures[i];
            }
            // a level picture of the download pack, else (not downloaded yet) the level below's
            var later = LevelIconKit.Find(name);
            if (later != null) return later;
            var cut = name.LastIndexOf("_L", System.StringComparison.Ordinal);
            if (cut > 0 && int.TryParse(name.Substring(cut + 2), out var level) && level >= 2)
                return GetPicture(level == 2 ? name.Substring(0, cut) : $"{name.Substring(0, cut)}_L{level - 1}");
            return null;
        }

        public Sprite GetGlyph(EventGlyph glyph)
        {
            var index = (int)glyph;
            return index > 0 && index < glyphs.Length ? glyphs[index] : null;
        }
    }
}
