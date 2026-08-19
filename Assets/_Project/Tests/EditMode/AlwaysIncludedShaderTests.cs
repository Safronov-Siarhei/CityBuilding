using CityBuilder.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// Every shader that only runtime code asks for must be in Always Included Shaders.
    ///
    /// This is the test the first Android build needed and did not have. `Shader.Find` searches the
    /// whole project in the editor and only the BUILT-IN shaders in a player, and a shader is built
    /// in only if something references it. URP/Unlit was referenced by no material anywhere -- just
    /// by `new Material(RuntimeShaders.Unlit)` in a dozen places -- so it silently did not travel,
    /// and every progress bar, health bar, radius carpet and cell highlight came out magenta on the
    /// phone while looking perfect in the editor.
    ///
    /// Note what this asserts and what it cannot: it reads the SETTING, not the built player. That
    /// is the right thing to pin here anyway -- the setting is what a careless merge or a
    /// regenerated project would drop, and checking it costs no build.
    /// </summary>
    public class AlwaysIncludedShaderTests
    {
        [TestCase(RuntimeShaders.LitName)]
        [TestCase(RuntimeShaders.UnlitName)]
        public void RuntimeShaderIsAlwaysIncluded(string shaderName)
        {
            var shader = Shader.Find(shaderName);
            Assert.IsNotNull(shader, $"'{shaderName}' does not exist in this project at all.");

            var graphicsSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            Assert.IsNotNull(graphicsSettings);
            Assert.Greater(graphicsSettings.Length, 0, "GraphicsSettings.asset could not be loaded.");

            var included = new SerializedObject(graphicsSettings[0]).FindProperty("m_AlwaysIncludedShaders");
            Assert.IsNotNull(included, "GraphicsSettings has no m_AlwaysIncludedShaders property.");

            for (var i = 0; i < included.arraySize; i++)
            {
                if (included.GetArrayElementAtIndex(i).objectReferenceValue == shader) return;
            }

            Assert.Fail($"'{shaderName}' is built from at runtime but is not in Always Included Shaders. " +
                        "It will not reach a player, Shader.Find will return null there, and everything using it will render magenta. " +
                        "SetupProject.IncludeRuntimeShadersInBuilds is what puts it there.");
        }
    }
}
