using System.Collections.Generic;
using System.IO;
using CityBuilder.Buildings;
using CityBuilder.CameraControl;
using CityBuilder.Cheats;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using CityBuilder.Resources;
using CityBuilder.Saving;
using CityBuilder.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CityBuilder.EditorTools
{
    public static partial class SetupProject
    {
        private const string ScenesFolder = "Assets/_Project/Scenes";
        private const string MaterialsFolder = "Assets/_Project/Materials";
        private const string TexturesFolder = "Assets/_Project/Textures";
        private const string BuildingPrefabsFolder = "Assets/_Project/Prefabs/Buildings";
        private const string BuildingDataFolder = "Assets/_Project/ScriptableObjects/Buildings";
        private const string ResourceScriptableObjectsFolder = "Assets/_Project/ScriptableObjects/Resources";
        private const string MeshMapsFolder = "Assets/_Project/Resources/MeshMaps";
        private const string ModelsTerrainFolder = "Assets/_Project/Models/Terrain";
        private const string ModelsBuildingsFolder = "Assets/_Project/Models/Buildings";

        /// <summary>How many Tree{n}.fbx variants live in ModelsTerrainFolder -- the forest picks between all of them.</summary>
        private const int TreeModelCount = 10;

        /// <summary>How many Stone{n}.fbx boulders live in ModelsTerrainFolder, smallest first -- RockSpawner weights them by that order.</summary>
        private const int StoneModelCount = 10;

        private const int GridCellsX = 200;
        private const int GridCellsZ = 200;
        private const int ForestMarginCells = 20; // same 20m world-space margin as before the cellSize halved
        private const float CellSize = 1f;
        private const float BuildingInset = 0.08f;
        private const float GroundHeight = 0f;
        private static readonly Vector3 GroundOrigin = new Vector3(-GridCellsX * CellSize * 0.5f, 0f, -GridCellsZ * CellSize * 0.5f);

        /// <summary>
        /// The balance the current run is building against, straight from RebuildConfig's return
        /// value rather than BalanceConfig.Instance -- Instance caches whatever Resources.Load
        /// happened to return first, which in an Editor session can predate the rebuild.
        /// </summary>
        private static BalanceConfig _balance;

        [MenuItem("CityBuilder/Setup Project (Scenes + Prefabs)")]
        public static void Run()
        {
            // Per-run, not per-Editor-session: assets are deleted and rebuilt below, so sprites
            // cached by a previous run would point at objects that no longer exist.
            _iconSpriteCache.Clear();

            // Before anything is built: the scene's components and the generated BuildingData both
            // read balance numbers, and a stale config would bake yesterday's balance into today's
            // assets. Regenerating from the committed CSVs (never from the network) keeps a
            // batchmode build reproducible on any machine.
            _balance = BalanceImporter.RebuildConfig();

            // Before the models are reimported: their `Buildings` material is remapped onto this
            // one at import (ModelImportDefaults), so it has to exist by then.
            EnsurePaletteMaterial();
            EnsureTerrainPaletteMaterial();
            BuildSeasonLooks();
            ForceReimportModels();
            EnsureUiTextures();

            // The city first: its building prefabs are what the main menu's background stages its
            // scenes with, so they have to be this run's, not the last one's.
            BuildCityScene();
            BuildMainMenuScene();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene($"{ScenesFolder}/MainMenu.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/CityBuilder.unity", true),
            };

            CleanupTemplateAssets();

            // Whichever scene was built last is the active one here -- left as-is, that becomes the scene the Editor restores on its next normal (non-
            // batchmode) launch, silently replacing the "opens on MainMenu" behavior every prior
            // session had. Re-opening MainMenu explicitly (matches BuildMainMenuScene's own
            // path/name) keeps that behavior intact regardless of how batchmode was invoked.
            EditorSceneManager.OpenScene($"{ScenesFolder}/MainMenu.unity");

            // Applied last: EditorSceneManager.NewScene()/OpenScene() calls were observed to
            // revert PlayerSettings changes made beforehand, so nothing here can precede a scene
            // switch -- including the MainMenu re-open just above.
            ConfigureMobileLandscape();
            NameTerrainLayer();
            IncludeRuntimeShadersInBuilds();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Guarantees ModelImportDefaults' import settings (isReadable, bakeAxisConversion) are
        /// actually applied to the FBX assets already committed under Models/ -- Unity doesn't
        /// reliably retroactively reimport existing assets just because the AssetPostprocessor
        /// script that governs them changed.
        /// </summary>
        private static void ForceReimportModels()
        {
            AssetDatabase.ImportAsset("Assets/_Project/Models", ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
        }

        /// <summary>
        /// The one material every Blender-made building and tree draws through: the palette's flat
        /// colour times the soft painted detail (Shaders/BuildingDetail.shader -- tiles, plaster,
        /// planks, stone, laid by Tools/blender/detail.py), lit the way Simple Lit lit it before.
        /// Updated in place rather than deleted and recreated like the generated materials,
        /// because the FBX importers point at it by GUID -- recreating it would leave every model's
        /// remap dangling until the next reimport. Still one material for the whole set.
        /// </summary>
        private static void EnsurePaletteMaterial()
        {
            AssetDatabase.ImportAsset(ModelImportDefaults.PaletteTexturePath, ImportAssetOptions.ForceUpdate);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ModelImportDefaults.PaletteTexturePath);
            if (texture == null)
            {
                Debug.LogError($"EnsurePaletteMaterial: no palette at {ModelImportDefaults.PaletteTexturePath} -- run Tools/blender/export_fbx.py. Every Blender-made model will draw without its colours.");
                return;
            }

            AssetDatabase.ImportAsset(ModelImportDefaults.DetailAtlasPath, ImportAssetOptions.ForceUpdate);
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(ModelImportDefaults.DetailAtlasPath);
            if (atlas == null)
            {
                Debug.LogError($"EnsurePaletteMaterial: no detail atlas at {ModelImportDefaults.DetailAtlasPath} -- run Tools/blender/export_fbx.py. The buildings will draw as flat colour.");
            }

            var shader = Shader.Find(BuildingDetailShaderName);
            var material = AssetDatabase.LoadAssetAtPath<Material>(ModelImportDefaults.PaletteMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, ModelImportDefaults.PaletteMaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_DetailAtlas", atlas);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.15f);
            material.SetFloat("_DetailStrength", 1f);
            material.SetFloat("_ShadeStrength", 1f);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
        }

        public const string BuildingDetailShaderName = "CityBuilder/BuildingDetail";

        /// <summary>
        /// The map's own palette material (Map 3 onwards; see ModelImportDefaults) -- same rules as
        /// EnsurePaletteMaterial: Simple Lit, updated in place because the importers remap onto it
        /// by GUID. Without the texture it is simply not made, and the maps that need it say so.
        /// </summary>
        private static void EnsureTerrainPaletteMaterial()
        {
            AssetDatabase.ImportAsset(ModelImportDefaults.TerrainPaletteTexturePath, ImportAssetOptions.ForceUpdate);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ModelImportDefaults.TerrainPaletteTexturePath);
            if (texture == null) return;

            var shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            var material = AssetDatabase.LoadAssetAtPath<Material>(ModelImportDefaults.TerrainPaletteMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, ModelImportDefaults.TerrainPaletteMaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.1f);
            material.DisableKeyword("_SPECULAR_COLOR");
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureMobileLandscape()
        {
            // We're building primarily for mobile in landscape; PC keyboard/mouse controls stay
            // as a secondary input path (see RTSCameraController / BuildingPlacer touch support).
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // The project was created from the URP template and inherited its identifier
            // (com.UnityTechnologies.com.unity.template.urpblank) -- which is not ours, and which
            // every other project made from that template also claims. Android treats the
            // identifier as the app's identity: installing this build would replace, or be replaced
            // by, any other app carrying it.
            PlayerSettings.companyName = "Depigo Games";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.depigogames.citybuilding");

            // Optimized Frame Pacing (Google's Swappy), off by default and wrong for this game.
            // FrameRateController caps the game at 60, and four of the six phones this is tested
            // on have 90 or 120 Hz screens: without Swappy, Android hands those 60 frames to a
            // 90 Hz display unevenly, so two frames are shown for one refresh and the next for
            // two. That reads as judder at a perfectly healthy frame time -- the kind of defect
            // that gets blamed on a slow phone and hunted in the wrong place for months.
            PlayerSettings.Android.optimizedFramePacing = true;

            // The app's icon: a placeholder the user picked (2026-10-16) -- the village with the
            // windmill, rendered from the game's own models. One default icon; Unity derives every
            // size and the Android legacy/round icons from it.
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
            if (icon != null)
            {
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            }
            else
            {
                Debug.LogWarning($"[SetupProject] {AppIconPath} is missing -- the build keeps Unity's default icon.");
            }
        }

        private const string AppIconPath = "Assets/_Project/UI/AppIcon.png";

        /// <summary>
        /// Puts the shaders that only runtime code asks for into every build.
        ///
        /// A shader reaches a player only if something references it. Every material this project
        /// generates is Lit, so URP/Lit travelled in on their backs -- but URP/Unlit is referenced
        /// by no material at all, only by `new Material(RuntimeShaders.Unlit)` in a dozen runtime
        /// call sites. It was therefore left out of the first Android build entirely, `Shader.Find`
        /// returned null in the player, and every progress bar, health bar, radius carpet and cell
        /// highlight rendered as a magenta rectangle. The editor could never reproduce it: there
        /// `Shader.Find` searches the whole project.
        ///
        /// Always Included Shaders is the setting that says "build this in regardless", which is
        /// exactly the guarantee those call sites need.
        /// </summary>
        private static void IncludeRuntimeShadersInBuilds()
        {
            var graphicsSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (graphicsSettings == null || graphicsSettings.Length == 0) return;

            var settings = new SerializedObject(graphicsSettings[0]);
            var included = settings.FindProperty("m_AlwaysIncludedShaders");
            if (included == null) return;

            var wanted = new[] { RuntimeShaders.LitName, RuntimeShaders.UnlitName, RuntimeShaders.PlacementGhostName };
            var changed = false;

            foreach (var shaderName in wanted)
            {
                var shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    Debug.LogWarning($"[SetupProject] Shader '{shaderName}' not found in the project -- cannot always-include it.");
                    continue;
                }

                var alreadyThere = false;
                for (var i = 0; i < included.arraySize; i++)
                {
                    if (included.GetArrayElementAtIndex(i).objectReferenceValue == shader) { alreadyThere = true; break; }
                }
                if (alreadyThere) continue;

                included.InsertArrayElementAtIndex(included.arraySize);
                included.GetArrayElementAtIndex(included.arraySize - 1).objectReferenceValue = shader;
                changed = true;
            }

            if (changed) settings.ApplyModifiedProperties();
        }

        /// <summary>
        /// Gives the map's physics layer a name in the Inspector. The layer INDEX is what actually
        /// matters (TerrainPhysicsLayer.Layer, applied at runtime where the map is built) -- this
        /// only stops it reading as "Layer 6" to anyone looking at the ground object.
        /// </summary>
        private static void NameTerrainLayer()
        {
            var tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAssets == null || tagManagerAssets.Length == 0) return;

            var tagManager = new SerializedObject(tagManagerAssets[0]);
            var layers = tagManager.FindProperty("layers");
            if (layers == null || layers.arraySize <= TerrainPhysicsLayer.Layer) return;

            var slot = layers.GetArrayElementAtIndex(TerrainPhysicsLayer.Layer);
            if (slot.stringValue == TerrainPhysicsLayer.LayerName) return;

            slot.stringValue = TerrainPhysicsLayer.LayerName;
            tagManager.ApplyModifiedProperties();
        }

        /// <summary>
        /// Writes the assembled building out as a prefab, baking its loose parts into one mesh on
        /// the way (see CombineParts) unless the caller keeps its own child models.
        ///
        /// `combineParts: false` is for the two prefab shapes whose children are switched at
        /// RUNTIME and therefore have to stay separate objects: an authored model with level 2/3
        /// variants (BuildingLevelAppearance) and a fence with its straight/corner pair
        /// (FenceAppearance). Merging at their root would fuse the alternatives into one object
        /// and lose the switch. The level models are single-mesh FBX instances with nothing to
        /// merge; the fence's two variants are built from parts and are each baked on their own,
        /// at their own root, in CreateFenceBuildingData.
        /// </summary>
        private static GameObject SavePrefab(GameObject root, string name, bool combineParts = true)
        {
            if (combineParts) CombineParts(root, name);

            Directory.CreateDirectory(BuildingPrefabsFolder);
            var prefabPath = $"{BuildingPrefabsFolder}/{name}.prefab";
            DeleteIfExists(prefabPath);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>
        /// A material asset, one per name.
        ///
        /// **Asking for the same name twice used to destroy the first one.** This deletes the asset
        /// at that path before writing a new one, so a second call for a name already used in this
        /// run left every renderer that had taken the first material pointing at a destroyed object
        /// -- which Unity draws in bright magenta. It cost the whole fence: the straight run and the
        /// corner each asked for `Building_Fence_Wood`, and the straight run came out pink. Nothing
        /// warned; the geometry was perfect and the colour was not.
        ///
        /// So a name asked for twice now answers with the SAME material rather than a replacement.
        /// The cache lives as long as the batchmode process, which is exactly one SetupProject run.
        /// </summary>
        private static readonly Dictionary<string, Material> _litMaterialsByName = new Dictionary<string, Material>();

        private static Material CreateLitMaterial(string name, Color color)
        {
            if (_litMaterialsByName.TryGetValue(name, out var existing) && existing != null) return existing;

            var path = $"{MaterialsFolder}/{name}.mat";
            Directory.CreateDirectory(MaterialsFolder);
            DeleteIfExists(path);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            AssetDatabase.CreateAsset(material, path);
            _litMaterialsByName[name] = material;
            return material;
        }

        /// <summary>
        /// Flat-color pictogram icons for the hotbar/category buttons (see CreateIconButton),
        /// drawn as filled rectangles/triangles onto a small transparent-background texture --
        /// kept to straight edges only, matching the project's no-circles cubic style used
        /// everywhere else. Returned sprites are cached assets, safe to reuse across buttons.
        /// </summary>
        // Every icon this run, keyed the same way its asset path is. Several icons are requested
        // twice per run -- the resource ones by both BuildResourceHUD and
        // CreateResourceIconLibrary -- and without this the second request would DeleteIfExists
        // the asset the first caller had already pointed a scene Image at, silently breaking that
        // reference and leaving a blank white square in the HUD. Cleared at the start of Run().
        private static readonly Dictionary<string, Sprite> _iconSpriteCache = new Dictionary<string, Sprite>();

        private static void DeleteIfExists(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) == null) return;

            AssetDatabase.DeleteAsset(path);
            WaitUntilGone(path);
            WaitUntilGone(path + ".meta");
        }

        /// <summary>
        /// Windows only really removes a deleted file once every handle on it closes. While an
        /// antivirus or the search indexer is still reading a file that was just written, a delete
        /// leaves it "pending", and writing a new file of that name fails -- Unity reports "Cannot
        /// open file ... for write" / "Failed to delete" and the whole run aborts on whichever
        /// asset lost the race (2026-10-20: FisherHut, Fence, DecGarden, run after run). So wait
        /// for the name to come free: two seconds at most, and only when the race happens.
        /// </summary>
        private static void WaitUntilGone(string path)
        {
            for (var i = 0; i < 200 && File.Exists(path); i++) System.Threading.Thread.Sleep(10);
        }

        private static void CleanupTemplateAssets()
        {
            AssetDatabase.DeleteAsset("Assets/Scenes");
            AssetDatabase.DeleteAsset("Assets/TutorialInfo");
            AssetDatabase.DeleteAsset("Assets/Readme.asset");
            // Stale assets from earlier iterations of the ground/buildings.
            DeleteIfExists($"{TexturesFolder}/GridCell.asset");
            DeleteIfExists($"{MaterialsFolder}/Building_House.mat");
            DeleteIfExists($"{MaterialsFolder}/Building_TownHall.mat");
        }
    }
}
