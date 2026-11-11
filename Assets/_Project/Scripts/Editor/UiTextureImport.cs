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
        /// <summary>Raised when these rules change, so Unity reimports the pictures they cover (2: the Android ASTC override).</summary>
        public override uint GetVersion() => 3;

        public const string SkinFolder = "Assets/_Project/UI/Skin";
        public const string IconsFolder = "Assets/_Project/UI/Icons";

        /// <summary>The event feed's white glyphs (Tools/ui/make_event_glyphs.py) -- imported as icons are.</summary>
        public const string GlyphsFolder = "Assets/_Project/UI/Glyphs";

        /// <summary>The HUD's looks (Tools/ui/make_hud_skins.py), a folder per look with its own borders.json -- imported as the skin is.</summary>
        public const string HudSkinsFolder = "Assets/_Project/Resources/HudSkins";

        /// <summary>The main menu's looks (Tools/ui/make_menu_skins.py), a folder per HUD look, each with its own borders.json -- imported as the skin is.</summary>
        public const string MenuSkinsFolder = "Assets/_Project/Resources/MenuSkins";

        /// <summary>The mode cards' pictures, photographed in the game (MenuArtBake). Big, so compressed, unlike the rest of the interface.</summary>
        public const string MenuArtFolder = "Assets/_Project/UI/MenuArt";

        /// <summary>The loading screen's pictures (Tools/ui/illustrate.py, render_runner.py): big, so compressed as the menu's art.</summary>
        public const string LoadingFolder = "Assets/_Project/UI/Loading";
        /// <summary>The campaign map's painted chart (Tools/campaign/campaign_map.py).</summary>
        public const string CampaignFolder = "Assets/_Project/UI/Campaign";
        /// <summary>The rate-us window's pieces (Tools/store/rate_window_art.py), placed by SetupProjectRateUs.</summary>
        public const string RateUsFolder = "Assets/_Project/UI/RateUs";
        /// <summary>The sandbox offer shop's pieces (Tools/store/offer_shop_art.py), placed by SetupProjectOfferShop.</summary>
        public const string ShopFolder = "Assets/_Project/UI/Shop";
        /// <summary>The daily login bonus window's day cards (Tools/store/daily_bonus_art.py), placed by SetupProjectDailyBonus.</summary>
        public const string DailyBonusFolder = "Assets/_Project/UI/DailyBonus";

        private void OnPreprocessTexture()
        {
            var isHudSkin = assetPath.StartsWith(HudSkinsFolder + "/") || assetPath.StartsWith(MenuSkinsFolder + "/");
            var isSkin = assetPath.StartsWith(SkinFolder + "/") || isHudSkin;
            var isArt = assetPath.StartsWith(MenuArtFolder + "/") || assetPath.StartsWith(LoadingFolder + "/") || assetPath.StartsWith(CampaignFolder + "/") || assetPath.StartsWith(RateUsFolder + "/") || assetPath.StartsWith(ShopFolder + "/") || assetPath.StartsWith(DailyBonusFolder + "/");
            var isIcon = assetPath.StartsWith(IconsFolder + "/") || assetPath.StartsWith(GlyphsFolder + "/") || isArt;
            if (!isSkin && !isIcon) return;
            if (!(assetImporter is TextureImporter importer)) return;

            importer.textureType = TextureImporterType.Sprite;
            // a picture that came in through a bare meta (guid only) was imported as a CUBE and had no
            // sprite -- white boxes in the shop window (2026-11-11)
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = isArt ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = isSkin ? 200f : 100f;
            // On Android the interface would be some 40 MB of raw RGBA and pushed the APK past
            // Telegram's 50 MB (60,3 MB on 2026-10-22). ASTC 4x4 is a quarter of that and keeps
            // the 9-slice edges and the icons' outlines clean; the editor and Windows keep the raw pictures.
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                format = TextureImporterFormat.ASTC_4x4,
                // the rate-us window shows its art at half size: half the texture is enough
                maxTextureSize = assetPath.StartsWith(RateUsFolder + "/") || assetPath.StartsWith(ShopFolder + "/") || assetPath.StartsWith(DailyBonusFolder + "/") ? 1024 : 2048,
                compressionQuality = 100,
            });

            var folder = isHudSkin ? Path.GetDirectoryName(assetPath) : SkinFolder;
            if (isSkin && Borders(folder).TryGetValue(Path.GetFileNameWithoutExtension(assetPath), out var b))
            {
                // borders.json is (left, bottom, right, top); Unity's spriteBorder is (left, bottom, right, top) too.
                importer.spriteBorder = new Vector4(b[0], b[1], b[2], b[3]);
            }
        }

        private static Dictionary<string, int[]> Borders(string folder)
        {
            var result = new Dictionary<string, int[]>();
            var path = Path.Combine(folder, "borders.json");
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
