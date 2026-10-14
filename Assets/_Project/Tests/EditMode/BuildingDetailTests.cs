using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// The buildings' soft painted detail (Shaders/BuildingDetail.shader, Tools/blender/detail.py):
    /// the one palette material draws through the detail shader with its atlas, the atlas comes
    /// in as linear grey (it MULTIPLIES the palette, so sRGB would darken every building), and
    /// every Blender-made model carries what the shader reads -- two extra UV sets and a vertex
    /// colour. A model exported without them still draws, but flat: the look would quietly fall
    /// apart one building at a time, which is what the last test is for.
    /// </summary>
    public class BuildingDetailTests
    {
        private const string PaletteMaterialPath = "Assets/_Project/Models/Palette/BuildingsPalette.mat";
        private const string DetailAtlasPath = "Assets/_Project/Models/Palette/BuildingsDetail.png";
        private const string ShaderName = "CityBuilder/BuildingDetail";

        [Test]
        public void TheDetailShader_CompilesWithoutErrors()
        {
            var shader = Shader.Find(ShaderName);
            Assert.IsNotNull(shader, $"No shader called {ShaderName}.");
            Assert.IsFalse(ShaderUtil.ShaderHasError(shader), "BuildingDetail.shader has compile errors -- every building would draw magenta.");
            Assert.IsTrue(shader.isSupported, "BuildingDetail.shader is not supported here.");
        }

        [Test]
        public void ThePaletteMaterial_DrawsThroughTheDetailShader_WithItsAtlas()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(PaletteMaterialPath);
            Assert.IsNotNull(material, "The palette material is missing.");
            Assert.AreEqual(ShaderName, material.shader.name, "The palette material is not on the detail shader -- the buildings draw as flat colour.");
            Assert.IsNotNull(material.GetTexture("_DetailAtlas"), "The palette material has no detail atlas.");
            Assert.IsNotNull(material.GetTexture("_BaseMap"), "The palette material lost its palette.");
        }

        [Test]
        public void TheDetailAtlas_IsLinearGrey()
        {
            var importer = AssetImporter.GetAtPath(DetailAtlasPath) as TextureImporter;
            Assert.IsNotNull(importer, $"No detail atlas at {DetailAtlasPath}.");
            Assert.IsFalse(importer.sRGBTexture,
                "The detail atlas is imported as sRGB: its 0.5 'no change' would arrive as about 0.21, and every building would draw at less than half its brightness.");
        }

        [Test]
        public void EveryBlenderModel_CarriesTheDetailData()
        {
            var missing = new List<string>();
            var checkedCount = 0;
            foreach (var folder in new[] { "Assets/_Project/Models/Buildings", "Assets/_Project/Models/Terrain" })
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { folder }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    // the placeholder boxes in Stubs/ stand in for models not made yet; they are not part of the set
                    if (path.Contains("/Stubs/")) continue;
                    foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                    {
                        if (!(asset is Mesh mesh)) continue;
                        checkedCount++;
                        if (!mesh.HasVertexAttribute(VertexAttribute.TexCoord1) ||
                            !mesh.HasVertexAttribute(VertexAttribute.TexCoord2) ||
                            !mesh.HasVertexAttribute(VertexAttribute.Color))
                        {
                            missing.Add(path);
                        }
                    }
                }
            }

            Assert.Greater(checkedCount, 40, "Found next to no models to check -- the folders moved?");
            CollectionAssert.IsEmpty(missing,
                "These models were exported without the detail's UV sets or vertex colour (re-run their script, then export_fbx.py): " + string.Join(", ", missing));
        }
    }
}
