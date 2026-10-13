using UnityEditor;
using UnityEngine;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// Blender-exported FBX models (Assets/_Project/Models/) come in with Unity's Z-up-to-Y-up
    /// axis correction applied as a root Transform rotation (visible as ~-90 on X) rather than
    /// baked into the mesh, unless bakeAxisConversion is enabled. Runtime code that instantiates
    /// these prefabs with an explicit Quaternion.identity (see MeshMapApplier, TreesAreaSpawner)
    /// would otherwise silently discard that correction, so every model under this project's
    /// Models/ folder gets the axis conversion baked into its mesh data at import time instead,
    /// keeping the root Transform at zero rotation.
    ///
    /// The building, tree and boulder models (Models/Buildings, Models/Terrain) and Map 2's ground
    /// are made in Blender (Tools/blender): every one of them draws through ONE material,
    /// `Buildings`, whose colour comes from a small palette texture. That material is remapped here onto the
    /// project's own palette material, so the whole set is one material on the device -- and the
    /// palette image is imported as what it is, a lookup table, not a picture.
    /// </summary>
    public class ModelImportDefaults : AssetPostprocessor
    {
        private const string ModelsFolder = "Assets/_Project/Models/";
        private const string BuildingsFolder = "Assets/_Project/Models/Buildings/";
        private const string TerrainFolder = "Assets/_Project/Models/Terrain/";

        public const string PaletteTexturePath = "Assets/_Project/Models/Palette/BuildingsPalette.png";
        public const string PaletteMaterialPath = "Assets/_Project/Models/Palette/BuildingsPalette.mat";

        /// <summary>The material name every Blender-made model is exported with (Tools/blender/kit.py).</summary>
        public const string ExportedMaterialName = "Buildings";

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(ModelsFolder)) return;
            if (!(assetImporter is ModelImporter modelImporter)) return;

            modelImporter.bakeAxisConversion = true;

            // Anything made in Blender by Tools/blender -- buildings, trees, boulders, and a map's
            // ground -- draws through the one palette material. Files without a `Buildings`
            // material are untouched by the remap.
            var palette = AssetDatabase.LoadAssetAtPath<Material>(PaletteMaterialPath);
            if (palette != null)
            {
                modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), ExportedMaterialName), palette);
            }

            var isSetPiece = assetPath.StartsWith(BuildingsFolder) || assetPath.StartsWith(TerrainFolder);
            if (!isSetPiece)
            {
                // MeshMapApplier.BuildNavMesh (NavMeshBuilder.BuildNavMeshData) needs CPU access
                // to the Ground mesh's vertex/triangle data to voxelize it into a NavMesh --
                // Unity's default (Read/Write disabled) throws the moment anything tries to read
                // a mesh like that at runtime, which was aborting MeshMapApplier.Apply() partway
                // through and taking the rest of map setup down with it.
                modelImporter.isReadable = true;
                return;
            }

            // Buildings and trees are only ever drawn and measured by their bounds: a second,
            // CPU-side copy of every mesh would be RAM for nothing, and they carry no animation.
            modelImporter.isReadable = false;
            modelImporter.importAnimation = false;
            modelImporter.animationType = ModelImporterAnimationType.None;
        }

        private void OnPreprocessTexture()
        {
            if (assetPath != PaletteTexturePath) return;
            if (!(assetImporter is TextureImporter textureImporter)) return;

            // Each colour is a 4x4 cell and every face samples one cell's centre. Mipmaps would
            // average neighbouring cells into each other at a distance, and bilinear filtering or
            // compression would bleed them at the edges -- so: exact pixels, full size, no maps.
            textureImporter.textureType = TextureImporterType.Default;
            textureImporter.sRGBTexture = true;
            textureImporter.mipmapEnabled = false;
            textureImporter.filterMode = FilterMode.Point;
            textureImporter.wrapMode = TextureWrapMode.Clamp;
            textureImporter.npotScale = TextureImporterNPOTScale.None;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            textureImporter.isReadable = false;
        }
    }
}
