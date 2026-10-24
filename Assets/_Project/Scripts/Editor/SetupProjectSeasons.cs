using CityBuilder.Core;
using CityBuilder.Maps;
using UnityEditor;
using UnityEngine;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// The seasons' materials and Resources/SeasonLooks.asset (Maps.SeasonStyle): a copy of the
    /// terrain material per season and a copy of the buildings' material per crown palette, each
    /// with a palette from Tools/blender/seasons.py. Copies are updated in place, as the palette
    /// materials themselves are.
    /// </summary>
    public static partial class SetupProject
    {
        private const string SeasonsFolder = "Assets/_Project/Models/Palette/Seasons/";
        private const string SeasonLooksPath = "Assets/_Project/Resources/SeasonLooks.asset";

        /// <summary>The tree models (trees1.py) that are conifers: they keep needles through the year.</summary>
        private static readonly string[] ConiferTrees = { "Tree2", "Tree5", "Tree6", "Tree9" };

        private static void BuildSeasonLooks()
        {
            var terrain = AssetDatabase.LoadAssetAtPath<Material>(ModelImportDefaults.TerrainPaletteMaterialPath);
            var buildings = AssetDatabase.LoadAssetAtPath<Material>(ModelImportDefaults.PaletteMaterialPath);
            if (terrain == null || buildings == null)
            {
                Debug.LogWarning("[SetupProject] No palette materials: the seasons are not built.");
                return;
            }

            var looks = AssetDatabase.LoadAssetAtPath<SeasonLooks>(SeasonLooksPath);
            if (looks == null)
            {
                looks = ScriptableObject.CreateInstance<SeasonLooks>();
                AssetDatabase.CreateAsset(looks, SeasonLooksPath);
            }
            looks.terrainSummer = terrain;
            looks.buildingsPalette = buildings;
            looks.conifers = ConiferTrees;
            looks.figures = MaterialCopy(buildings, SeasonsFolder + "Figures.mat", null);
            looks.figures.SetFloat("_SnowMask", 0f);

            looks.looks = new SeasonLooks.Look[4];
            // Summer is the look every map is imported with: nothing swapped, no snow.
            looks.looks[(int)MapSeason.Summer] = new SeasonLooks.Look { terrain = terrain, conifer = buildings, broadleaf = new[] { buildings } };
            looks.looks[(int)MapSeason.Autumn] = SeasonLook("Autumn", terrain, buildings, 0f, new Color(1f, 0.9f, 0.78f));
            looks.looks[(int)MapSeason.Winter] = SeasonLook("Winter", terrain, buildings, 1f, new Color(0.88f, 0.93f, 1f));
            looks.looks[(int)MapSeason.Spring] = SeasonLook("Spring", terrain, buildings, 0f, new Color(1f, 0.98f, 0.94f));
            EditorUtility.SetDirty(looks);
            AssetDatabase.SaveAssets();
        }

        private static SeasonLooks.Look SeasonLook(string season, Material terrain, Material buildings, float snow, Color sunTint)
        {
            var look = new SeasonLooks.Look { snow = snow, sunTint = sunTint };
            look.terrain = MaterialCopy(terrain, $"{SeasonsFolder}Terrain-{season}.mat", $"{SeasonsFolder}Terrain-{season}.png");
            look.conifer = MaterialCopy(buildings, $"{SeasonsFolder}Trees-{season}-Conifer.mat", $"{SeasonsFolder}Trees-{season}-Conifer.png");
            var broadleaf = new System.Collections.Generic.List<Material>();
            for (var n = 1; AssetDatabase.LoadAssetAtPath<Texture2D>($"{SeasonsFolder}Trees-{season}-{n}.png") != null; n++)
                broadleaf.Add(MaterialCopy(buildings, $"{SeasonsFolder}Trees-{season}-{n}.mat", $"{SeasonsFolder}Trees-{season}-{n}.png"));
            look.broadleaf = broadleaf.ToArray();
            if (look.terrain == terrain) Debug.LogWarning($"[SetupProject] {season} has no palette: run Tools/blender/seasons.py.");
            return look;
        }

        /// <summary>`source` copied to `path` (kept if it is there, so its GUID holds), with `palette` as its base map; `source` itself when the palette is missing.</summary>
        private static Material MaterialCopy(Material source, string path, string palette)
        {
            Texture2D texture = null;
            if (palette != null)
            {
                AssetDatabase.ImportAsset(palette, ImportAssetOptions.ForceUpdate);
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(palette);
                if (texture == null) return source;
            }
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(source);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = source.shader;
                material.CopyPropertiesFromMaterial(source);
            }
            if (texture != null) material.SetTexture("_BaseMap", texture);
            material.enableInstancing = source.enableInstancing;
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
