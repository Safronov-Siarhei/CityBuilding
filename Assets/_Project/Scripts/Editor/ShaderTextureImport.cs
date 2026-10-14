using UnityEditor;
using UnityEngine;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// Import settings for the data textures the project's own shaders read (Assets/_Project/Shaders),
    /// such as the fog's cloud noise (Tools/ui/make_cloud_noise.py). They are numbers, not colours:
    /// linear rather than sRGB, so 0.5 stays 0.5; tiling, because the clouds scroll across them
    /// forever; mipmapped, because the camera sees them from 5 m and from 40.
    /// </summary>
    public class ShaderTextureImport : AssetPostprocessor
    {
        public const string ShadersFolder = "Assets/_Project/Shaders";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ShadersFolder + "/")) return;
            if (!(assetImporter is TextureImporter importer)) return;

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
