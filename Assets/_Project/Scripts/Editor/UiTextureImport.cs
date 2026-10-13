using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// Import settings for the interface's own pictures: the skin (Assets/_Project/UI/Skin, drawn
    /// by Tools/ui/make_skin.py) and the icons rendered from the models
    /// (Assets/_Project/UI/Icons, Tools/blender/render_icons.py).
    ///
    /// Skin sprites are drawn at twice the size they show at, so they import at 200 pixels per
    /// unit against the canvas's 100, and each is 9-sliced by the border its generator wrote into
    /// borders.json beside them. Icons are plain sprites. Neither gets mipmaps (a UI image is
    /// drawn at one size) or compression (a few small images; banding on the soft edges of a
    /// rounded pill shows).
    /// </summary>
    public class UiTextureImport : AssetPostprocessor
    {
        public const string SkinFolder = "Assets/_Project/UI/Skin";
        public const string IconsFolder = "Assets/_Project/UI/Icons";

        private void OnPreprocessTexture()
        {
            var isSkin = assetPath.StartsWith(SkinFolder + "/");
            var isIcon = assetPath.StartsWith(IconsFolder + "/");
            if (!isSkin && !isIcon) return;
            if (!(assetImporter is TextureImporter importer)) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = isSkin ? 200f : 100f;

            if (isSkin && Borders().TryGetValue(Path.GetFileNameWithoutExtension(assetPath), out var b))
            {
                // borders.json is (left, bottom, right, top); Unity's spriteBorder is (left, bottom, right, top) too.
                importer.spriteBorder = new Vector4(b[0], b[1], b[2], b[3]);
            }
        }

        private static Dictionary<string, int[]> Borders()
        {
            var result = new Dictionary<string, int[]>();
            var path = Path.Combine(SkinFolder, "borders.json");
            if (!File.Exists(path)) return result;

            // A flat {"name": [l, b, r, t], ...} object -- small enough not to need a JSON library.
            var text = File.ReadAllText(path);
            var i = 0;
            while ((i = text.IndexOf('"', i)) >= 0)
            {
                var end = text.IndexOf('"', i + 1);
                var name = text.Substring(i + 1, end - i - 1);
                var open = text.IndexOf('[', end);
                var close = text.IndexOf(']', open);
                var parts = text.Substring(open + 1, close - open - 1).Split(',');
                var values = new int[4];
                for (var k = 0; k < 4 && k < parts.Length; k++) int.TryParse(parts[k].Trim(), out values[k]);
                result[name] = values;
                i = close + 1;
            }
            return result;
        }
    }
}
