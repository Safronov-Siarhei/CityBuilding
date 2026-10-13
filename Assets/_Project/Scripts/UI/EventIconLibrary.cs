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
            return null;
        }

        public Sprite GetGlyph(EventGlyph glyph)
        {
            var index = (int)glyph;
            return index > 0 && index < glyphs.Length ? glyphs[index] : null;
        }
    }
}
