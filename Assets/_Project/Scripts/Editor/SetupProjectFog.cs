using UnityEditor;
using UnityEngine;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// The fog of war's cloud material (see FogOfWarManager and Shaders/FogClouds.shader). Saved as
    /// an asset and referenced from the scene, which is also what carries the shader into a
    /// player build: a shader nothing references is left out of the build, and Shader.Find then
    /// returns null on the device (see RuntimeShaders for the time that happened).
    /// </summary>
    public static partial class SetupProject
    {
        private const string FogCloudShaderPath = ShaderTextureImport.ShadersFolder + "/FogClouds.shader";
        private const string CloudNoisePath = ShaderTextureImport.ShadersFolder + "/CloudNoise.png";

        private const string FogParchmentShaderPath = ShaderTextureImport.ShadersFolder + "/FogParchment.shader";

        private static Material CreateFogCloudMaterial() => CreateFogMaterial("FogClouds", FogCloudShaderPath);

        /// <summary>The other look of the fog, an old map on parchment -- the player picks one in the settings.</summary>
        private static Material CreateFogParchmentMaterial() => CreateFogMaterial("FogParchment", FogParchmentShaderPath);

        private static Material CreateFogMaterial(string name, string shaderPath)
        {
            // Through the importer first, so the noise is linear and tiling before anything samples it.
            AssetDatabase.ImportAsset(CloudNoisePath, ImportAssetOptions.ForceUpdate);

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            if (shader == null)
            {
                Debug.LogError($"SetupProject: no shader at {shaderPath} -- that look of the fog of war will not be drawn.");
                return null;
            }

            var noise = AssetDatabase.LoadAssetAtPath<Texture2D>(CloudNoisePath);
            if (noise == null) Debug.LogWarning($"SetupProject: no cloud noise at {CloudNoisePath} -- run Tools/ui/make_cloud_noise.py.");

            var material = new Material(shader) { name = name };
            material.SetTexture("_NoiseTex", noise);

            var path = $"{MaterialsFolder}/{name}.mat";
            DeleteIfExists(path);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
