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
        private const string CitizensFolder = "Assets/_Project/Models/Citizens/";
        private const string ShipsFolder = "Assets/_Project/Models/Ships/";
        private const string MonstersFolder = "Assets/_Project/Models/Monsters/";
        private const string FaunaFolder = "Assets/_Project/Models/Fauna/";
        private const string TideFolder = "Assets/_Project/Models/Tide/";
        // the campaign's cutscene set (Tools/blender/cinematic/export_cine.py)
        public const string CinematicFolder = "Assets/_Project/Models/Cinematic/";
        /// <summary>The opening cutscene's cast (export_cast.py): rigged, every clip a take.</summary>
        public const string CinematicCastFolder = CinematicFolder + "Cast/";
        public const string OpeningCastPath = "Assets/_Project/Campaign/OpeningCast.json";

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
        // Dungeon 3's fire palette (Tools/blender/dungeon3.py), the same layout recoloured
        public const string FirePaletteTexturePath = "Assets/_Project/Models/Palette/TerrainPaletteFire.png";
        public const string FirePaletteMaterialPath = "Assets/_Project/Models/Palette/TerrainPaletteFire.mat";
        // the orcs' gloomy swamp island (Tools/blender/orc_palette.py, the user 2026-11-06): the same
        // layout recoloured; its name must NOT start with TerrainPalette (MeshMapApplier swaps those)
        public const string OrcIslandPaletteTexturePath = "Assets/_Project/Models/Palette/OrcIslandPalette.png";
        public const string OrcIslandPaletteMaterialPath = "Assets/_Project/Models/Palette/OrcIslandPalette.mat";

        /// <summary>The seasons' palettes (Tools/blender/seasons.py): imported exactly as the two above.</summary>
        public const string SeasonPalettesFolder = "Assets/_Project/Models/Palette/Seasons/";

        /// <summary>The bot kingdom's palette: roofs, gold and banners in its straw (Tools/palette/bot_palette.py), imported as the buildings' own.</summary>
        public const string BotPaletteTexturePath = "Assets/_Project/Models/Palette/BotPalette.png";
        public const string BotPaletteMaterialPath = "Assets/_Project/Models/Palette/BotPalette.mat";
        // the orcs' own darker palette (Tools/palette/orc_palette.py, approved by the user 2026-11-06):
        // every model under Models/Orcs draws through it, but the swamp island's set (OrcIslandModel)
        public const string OrcPaletteTexturePath = "Assets/_Project/Models/Palette/OrcPalette.png";
        public const string OrcPaletteMaterialPath = "Assets/_Project/Models/Palette/OrcPalette.mat";

        /// <summary>The swamp island's skeleton, trees, stakes, wrecks and crows (orcisland1.py): approved in the town's palette.</summary>
        public static bool OrcIslandModel(string fileName) => fileName.StartsWith("OrcSkullGate") || fileName.StartsWith("OrcRib")
            || fileName.StartsWith("OrcVertebra") || fileName.StartsWith("OrcDeadTree") || fileName.StartsWith("OrcSkullStake")
            || fileName.StartsWith("OrcWreck") || fileName.StartsWith("OrcCrow");

        /// <summary>Whether this model file draws through the orcs' palette.</summary>
        public static bool UsesOrcPalette(string assetPath) => assetPath.StartsWith(OrcsFolder)
            && !OrcIslandModel(System.IO.Path.GetFileNameWithoutExtension(assetPath));
        /// <summary>The bot's King: the figures' material with only his cape in the bot's straw.</summary>
        public const string BotKingPaletteTexturePath = "Assets/_Project/Models/Palette/BotKingPalette.png";
        public const string BotKingPaletteMaterialPath = "Assets/_Project/Models/Palette/BotKingPalette.mat";
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
            if (UsesOrcPalette(assetPath)) palette = AssetDatabase.LoadAssetAtPath<Material>(OrcPaletteMaterialPath) ?? palette;
            if (palette != null)
            {
                modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), ExportedMaterialName), palette);
            }
            var terrainPalette = AssetDatabase.LoadAssetAtPath<Material>(TerrainPaletteMaterialPath);
            if (terrainPalette != null)
            {
                modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), ExportedTerrainMaterialName), terrainPalette);
            }

            if (assetPath.StartsWith(CinematicCastFolder))
            {
                // the figures carry their animations: a generic rig, the orcs drawn in their palette
                var orcPalette = AssetDatabase.LoadAssetAtPath<Material>(OrcPaletteMaterialPath);
                if (orcPalette != null)
                {
                    modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "CineOrcPalette"), orcPalette);
                }
                modelImporter.bakeAxisConversion = false;   // baked, it bent the rigid limbs off their joints
                modelImporter.importAnimation = true;
                modelImporter.animationType = ModelImporterAnimationType.Generic;
                modelImporter.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
                modelImporter.animationCompression = ModelImporterAnimationCompression.KeyframeReduction;
                modelImporter.meshCompression = ModelImporterMeshCompression.Medium;
                // CineFigure cuts each figure into rigid parts at runtime (reads its vertices and
                // triangles): unreadable, Play mode logged «Not allowed to access triangles» and every
                // villager, orc and Ардан came out empty -- the batchmode checks never saw it, the
                // editor lets edit mode read any mesh (the user, 2026-11-11: «не вижу многих жителей»)
                modelImporter.isReadable = true;
                return;
            }

            var isSetPiece = assetPath.StartsWith(BuildingsFolder) || assetPath.StartsWith(TerrainFolder)
                             || assetPath.StartsWith(OrcsFolder) || assetPath.StartsWith(HeroesFolder) || assetPath.StartsWith(SoldiersFolder)
                             || assetPath.StartsWith(WildFolder) || assetPath.StartsWith(CitizensFolder) || assetPath.StartsWith(ShipsFolder)
                             || assetPath.StartsWith(MonstersFolder) || assetPath.StartsWith(FaunaFolder) || assetPath.StartsWith(TideFolder)
                             || assetPath.StartsWith(CinematicFolder);
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
            // ...except the moat's bank, which MoatAppearance combines into one mesh a cell at runtime
            modelImporter.isReadable = assetPath.EndsWith("/Moat-Bank.fbx");
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

        /// <summary>The campaign film's audio, weighed for phones: music and ambience (mus_/amb_) are
        /// streamed rather than held in memory; short effects are mono and decompressed on load.</summary>
        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/_Project/Audio/")) return;
            var audio = (AudioImporter)assetImporter;
            var name = System.IO.Path.GetFileName(assetPath);
            var stream = name.StartsWith("mus_") || name.StartsWith("amb_");
            audio.forceToMono = !stream;
            var s = audio.defaultSampleSettings;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = stream ? 0.5f : 0.6f;
            s.loadType = stream ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            audio.defaultSampleSettings = s;
        }

        /// <summary>The cast's takes come in as "<rig>|<clip>": named by the clip alone, and looped
        /// where OpeningCast.json says so.</summary>
        private void OnPreprocessAnimation()
        {
            if (!assetPath.StartsWith(CinematicCastFolder)) return;
            if (!(assetImporter is ModelImporter modelImporter)) return;
            var json = System.IO.File.Exists(OpeningCastPath) ? System.IO.File.ReadAllText(OpeningCastPath) : "";
            var figure = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            var clips = modelImporter.defaultClipAnimations;
            foreach (var c in clips)
            {
                var bar = c.name.LastIndexOf('|');
                if (bar >= 0) c.name = c.name.Substring(bar + 1);
                c.loopTime = CastClipLoops(json, figure, c.name);
            }
            modelImporter.clipAnimations = clips;
        }

        private static bool CastClipLoops(string json, string figure, string clip)
        {
            // the file is small and flat: find the figure's block, then the clip's entry in it
            var at = json.IndexOf("\"" + figure + "\"", System.StringComparison.Ordinal);
            if (at < 0) return false;
            var end = json.IndexOf("\"name\": \"Cine", at + figure.Length + 2, System.StringComparison.Ordinal);
            if (end < 0) end = json.Length;
            var entry = json.IndexOf("\"name\": \"" + clip + "\"", at, System.StringComparison.Ordinal);
            if (entry < 0 || entry > end) return false;
            var close = json.IndexOf('}', entry);
            return close > entry && json.Substring(entry, close - entry).Contains("\"loop\": true");
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

            if (assetPath != PaletteTexturePath && assetPath != TerrainPaletteTexturePath && assetPath != FirePaletteTexturePath && assetPath != OrcIslandPaletteTexturePath && assetPath != BotPaletteTexturePath && assetPath != OrcPaletteTexturePath && assetPath != BotKingPaletteTexturePath && !assetPath.StartsWith(SeasonPalettesFolder)) return;
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
