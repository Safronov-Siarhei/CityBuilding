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
        /// <summary>Raised when these rules change, so Unity reimports the models they cover (2: the maps' meshes compressed; 3: set pieces at Medium).</summary>
        public override uint GetVersion() => 3;

        private const string ModelsFolder = "Assets/_Project/Models/";
        private const string BuildingsFolder = "Assets/_Project/Models/Buildings/";
        private const string TerrainFolder = "Assets/_Project/Models/Terrain/";
        private const string OrcsFolder = "Assets/_Project/Models/Orcs/";
        private const string HeroesFolder = "Assets/_Project/Models/Heroes/";
        private const string SoldiersFolder = "Assets/_Project/Models/Soldiers/";
        private const string WildFolder = "Assets/_Project/Models/Wild/";

        public const string PaletteTexturePath = "Assets/_Project/Models/Palette/BuildingsPalette.png";
        public const string PaletteMaterialPath = "Assets/_Project/Models/Palette/BuildingsPalette.mat";

        /// <summary>The grey pattern atlas the palette material multiplies in (Tools/blender/detail.py, BuildingDetail.shader).</summary>
        public const string DetailAtlasPath = "Assets/_Project/Models/Palette/BuildingsDetail.png";

        /// <summary>The material name every Blender-made model is exported with (Tools/blender/kit.py).</summary>
        public const string ExportedMaterialName = "Buildings";

        // Map 3 onwards draws its ground, water, relief and props through a palette of its own
        // (Tools/blender/terrain.py): the buildings' one is full, and the land needs many greens.
        public const string TerrainPaletteTexturePath = "Assets/_Project/Models/Palette/TerrainPalette.png";
        public const string TerrainPaletteMaterialPath = "Assets/_Project/Models/Palette/TerrainPalette.mat";
        public const string ExportedTerrainMaterialName = "Terrain";

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
            var terrainPalette = AssetDatabase.LoadAssetAtPath<Material>(TerrainPaletteMaterialPath);
            if (terrainPalette != null)
            {
                modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), ExportedTerrainMaterialName), terrainPalette);
            }

            var isSetPiece = assetPath.StartsWith(BuildingsFolder) || assetPath.StartsWith(TerrainFolder)
                             || assetPath.StartsWith(OrcsFolder) || assetPath.StartsWith(HeroesFolder) || assetPath.StartsWith(SoldiersFolder)
                             || assetPath.StartsWith(WildFolder);
            if (!isSetPiece)
            {
                // MeshMapApplier.BuildNavMesh (NavMeshBuilder.BuildNavMeshData) needs CPU access
                // to the Ground mesh's vertex/triangle data to voxelize it into a NavMesh --
                // Unity's default (Read/Write disabled) throws the moment anything tries to read
                // a mesh like that at runtime, which was aborting MeshMapApplier.Apply() partway
                // through and taking the rest of map setup down with it.
                modelImporter.isReadable = true;
                // The maps' ground, relief and water were the build's biggest meshes left raw
                // (4 MB a map ground). Low quantises the stored copy only, as for the buildings
                // below: far finer than a cell, so the map's chunks still meet.
                modelImporter.meshCompression = ModelImporterMeshCompression.Low;
                return;
            }

            // Buildings and trees are only ever drawn and measured by their bounds: a second,
            // CPU-side copy of every mesh would be RAM for nothing, and they carry no animation.
            modelImporter.isReadable = false;
            modelImporter.importAnimation = false;
            modelImporter.animationType = ModelImporterAnimationType.None;
            // Five levels of every building plus the orc bases made meshes 62% of the build and
            // pushed the APK past Telegram's 50 MB (2026-10-16). Low quantises what is stored on
            // disk only -- the mesh is unpacked at load, so the frame costs nothing -- and is
            // fine-grained enough for the palette UVs and the fence joins. Medium since 2026-10-22
            // (the APK past 50 MB again): 16 bits across a model a few metres wide is still a
            // hundredth of a millimetre. The maps stay at Low -- across 200 m, Medium would open
            // seams between their chunks.
            modelImporter.meshCompression = ModelImporterMeshCompression.Medium;
        }

        private void OnPreprocessTexture()
        {
            if (assetPath == DetailAtlasPath)
            {
                // Grey values that MULTIPLY the palette (0.5 = no change), so they must arrive as
                // written: linear, not sRGB. Filtered and mipmapped -- it is a picture, unlike the
                // palette -- and sampled within its own cell by the shader, so the wrap mode only
                // matters at the atlas edge.
                if (!(assetImporter is TextureImporter detailImporter)) return;
                detailImporter.textureType = TextureImporterType.Default;
                detailImporter.sRGBTexture = false;
                detailImporter.mipmapEnabled = true;
                detailImporter.filterMode = FilterMode.Bilinear;
                detailImporter.wrapMode = TextureWrapMode.Clamp;
                detailImporter.textureCompression = TextureImporterCompression.CompressedHQ;
                detailImporter.isReadable = false;
                return;
            }

            if (assetPath != PaletteTexturePath && assetPath != TerrainPaletteTexturePath) return;
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
