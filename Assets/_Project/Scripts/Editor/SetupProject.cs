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
        private const string ModelsMap1Folder = "Assets/_Project/Models/Map1";
        private const string ModelsTerrainFolder = "Assets/_Project/Models/Terrain";
        private const string ModelsBuildingsFolder = "Assets/_Project/Models/Buildings";

        /// <summary>How many Tree{n}.fbx variants live in ModelsTerrainFolder -- the forest picks between all of them.</summary>
        private const int TreeModelCount = 5;

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

            ForceReimportModels();

            BuildMainMenuScene();
            BuildCityScene();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene($"{ScenesFolder}/MainMenu.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/CityBuilder.unity", true),
            };

            CleanupTemplateAssets();

            // BuildCityScene() runs last above, so CityBuilder is still the active scene here --
            // left as-is, that becomes the scene the Editor restores on its next normal (non-
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
        }

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

            var wanted = new[] { RuntimeShaders.LitName, RuntimeShaders.UnlitName };
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

        private static void BuildMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Created after the scene switch above (see the NewScene() comment in BuildCityScene
            // for why) and used only within this method, before this scene is saved.
            var panelSprite = CreatePanelSprite();

            var cameraGO = new GameObject("Menu Camera", typeof(Camera));
            var menuCamera = cameraGO.GetComponent<Camera>();
            menuCamera.clearFlags = CameraClearFlags.SolidColor;
            menuCamera.backgroundColor = new Color(0.55f, 0.75f, 0.92f);
            menuCamera.cullingMask = ~0;
            menuCamera.orthographic = false;
            menuCamera.nearClipPlane = 0.3f;
            menuCamera.farClipPlane = 500f;
            cameraGO.AddComponent<MenuCameraFlythrough>();

            // Terrain-only scenery (no grid/buildings/citizens) for the flythrough to fly over --
            // a random pick each time this scene loads, see MainMenuBackground.
            new GameObject("MenuBackground").AddComponent<MainMenuBackground>();

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // Landscape mobile screens vary a lot in width but far less in height, so anchoring
            // UI scale to height keeps button/text sizes consistent across phones and tablets.
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;

            Localize(CreateText(canvasGO.transform, "Title", string.Empty, 72, new Vector2(0f, 260f), new Vector2(1200f, 140f), addShadow: true), "#menu_title");
            Localize(CreateText(canvasGO.transform, "Subtitle", string.Empty, 26, new Vector2(0f, 175f), new Vector2(900f, 60f), new Color(1f, 1f, 1f, 0.6f), addShadow: true), "#menu_subtitle");

            var menuController = canvasGO.AddComponent<MainMenuController>();

            var newGameButton = CreateButton(canvasGO.transform, panelSprite, "NewGameButton", "#menu_new_game", new Vector2(0f, 60f), new Vector2(460f, 100f));
            UnityEventTools.AddPersistentListener(newGameButton.onClick, menuController.StartNewGame);

            var loadGameButton = CreateButton(canvasGO.transform, panelSprite, "LoadGameButton", "#menu_load_game", new Vector2(0f, -64f), new Vector2(460f, 100f));
            UnityEventTools.AddPersistentListener(loadGameButton.onClick, menuController.LoadGame);

            // The stack grew a fourth entry, so everything below the load button moved down one
            // slot (124 apart, as the three above already were).
            var settingsButton = CreateButton(canvasGO.transform, panelSprite, "SettingsButton", "#menu_settings", new Vector2(0f, -188f), new Vector2(460f, 100f));

            var quitButton = CreateButton(canvasGO.transform, panelSprite, "QuitButton", "#menu_quit", new Vector2(0f, -312f), new Vector2(460f, 100f));
            UnityEventTools.AddPersistentListener(quitButton.onClick, menuController.QuitGame);

            var menuControllerSO = new SerializedObject(menuController);
            menuControllerSO.FindProperty("loadGameButton").objectReferenceValue = loadGameButton;
            menuControllerSO.ApplyModifiedPropertiesWithoutUndo();

            BuildLoadPanel(canvasGO.transform, panelSprite, menuController);
            BuildSettingsPanel(canvasGO.transform, panelSprite, settingsButton);

            Directory.CreateDirectory(ScenesFolder);
            DeleteIfExists($"{ScenesFolder}/MainMenu.unity");
            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/MainMenu.unity");
        }

        /// <summary>
        /// The settings card, on the main menu. The language buttons inside are built at play time
        /// from whatever columns the localization sheet has -- see SettingsPanelController -- so a
        /// third language never needs this method touched.
        /// </summary>
        private static void BuildSettingsPanel(Transform canvasParent, Sprite panelSprite, Button openButton)
        {
            var panelRoot = new GameObject("SettingsPanel", typeof(RectTransform));
            panelRoot.transform.SetParent(canvasParent, false);
            StretchFull(panelRoot.GetComponent<RectTransform>());

            var backdrop = CreateImage(panelRoot.transform, "Backdrop", new Color(0f, 0f, 0f, 0.7f));
            StretchFull(backdrop.GetComponent<RectTransform>());

            var card = CreateImage(panelRoot.transform, "Card", new Color(0.16f, 0.18f, 0.15f, 0.98f));
            card.sprite = panelSprite;
            card.type = Image.Type.Sliced;
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(760f, 420f);
            cardRect.anchoredPosition = Vector2.zero;

            Localize(CreateText(card.transform, "Title", string.Empty, 40, new Vector2(0f, 140f), new Vector2(680f, 70f)), "#settings_title");
            Localize(CreateText(card.transform, "LanguageLabel", string.Empty, 26, new Vector2(0f, 66f), new Vector2(680f, 50f), new Color(1f, 1f, 1f, 0.75f)), "#settings_language");

            var languageRow = new GameObject("LanguageRow", typeof(RectTransform));
            languageRow.transform.SetParent(card.transform, false);
            var languageRowRect = languageRow.GetComponent<RectTransform>();
            languageRowRect.anchorMin = languageRowRect.anchorMax = new Vector2(0.5f, 0.5f);
            languageRowRect.anchoredPosition = new Vector2(0f, -10f);
            languageRowRect.sizeDelta = new Vector2(700f, 72f);

            var backButton = CreateButton(card.transform, panelSprite, "SettingsBackButton", "#settings_back", new Vector2(0f, -140f), new Vector2(300f, 80f));

            var controller = panelRoot.AddComponent<SettingsPanelController>();
            var controllerSO = new SerializedObject(controller);
            controllerSO.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            controllerSO.FindProperty("languageRow").objectReferenceValue = languageRowRect;
            controllerSO.FindProperty("buttonSprite").objectReferenceValue = panelSprite;
            controllerSO.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(backButton.onClick, controller.ClosePanel);
            UnityEventTools.AddPersistentListener(openButton.onClick, controller.OpenPanel);

            panelRoot.SetActive(false);
        }

        private static void BuildCityScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Building data/prefab assets are created here, after the scene switch above, so no
            // further NewScene() call happens before they're consumed below. EditorSceneManager.
            // NewScene() unloads objects that aren't rooted in the new scene yet, which silently
            // turns freshly created ScriptableObject asset references stale (Unity "fake null").
            var trimMaterial = CreateLitMaterial("Trim", new Color(0.14f, 0.11f, 0.08f));
            var doorMaterial = CreateLitMaterial("Door", new Color(0.32f, 0.18f, 0.09f));
            var windowMaterial = CreateLitMaterial("Window", new Color(0.88f, 0.78f, 0.48f));

            // Only shape below: every number these buildings are balanced on -- cost, health,
            // production, defence, upgrades, prerequisites -- comes from the balance sheet's
            // buildings tab, looked up by the id in the first argument (see ApplyBalance).
            var houseData = CreateBuildingData(
                "Hovel", new Vector2Int(2, 2), height: 2.4f,
                wallColor: new Color(0.75f, 0.55f, 0.35f), roofColor: new Color(0.25f, 0.45f, 0.65f),
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                hasChimney: true);

            var cottageData = CreateBuildingData(
                "Cottage", new Vector2Int(2, 2), height: 3f,
                wallColor: new Color(0.62f, 0.42f, 0.55f), roofColor: new Color(0.3f, 0.2f, 0.4f),
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                hasChimney: true);

            // The third rung of the housing ladder: Лачуга -> Коттедж -> Поместье, told apart by
            // size and height rather than by any ornament -- a manor is a bigger house.
            var manorData = CreateBuildingData(
                "Manor", new Vector2Int(3, 3), height: 3.8f,
                wallColor: new Color(0.78f, 0.72f, 0.6f), roofColor: new Color(0.35f, 0.28f, 0.42f),
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                hasChimney: true);

            // Decorations, and the town square people gather on. They cost something, they stand
            // somewhere, and (bar the happiness they carry) that is all they do -- but a town of
            // nothing but workshops does not look like a town.
            var townSquareData = CreateBuildingData(
                "TownSquare", new Vector2Int(2, 2), height: 1f,
                wallColor: new Color(0.62f, 0.6f, 0.56f), roofColor: new Color(0.5f, 0.48f, 0.45f),
                style: BuildingStyle.Plot, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.Fountain);

            var flagData = CreateBuildingData(
                "Flag", new Vector2Int(1, 1), height: 2.4f,
                wallColor: new Color(0.6f, 0.58f, 0.54f), roofColor: new Color(0.45f, 0.32f, 0.18f),
                style: BuildingStyle.Plot, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.FlagPole);

            var decTreeData = CreateBuildingData(
                "DecTree", new Vector2Int(1, 1), height: 2.6f,
                wallColor: new Color(0.42f, 0.5f, 0.34f), roofColor: new Color(0.36f, 0.44f, 0.3f),
                style: BuildingStyle.Plot, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.Tree);

            var decBushData = CreateBuildingData(
                "DecBush", new Vector2Int(1, 1), height: 0.9f,
                wallColor: new Color(0.44f, 0.5f, 0.36f), roofColor: new Color(0.38f, 0.46f, 0.32f),
                style: BuildingStyle.Plot, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.Bush);

            var decGardenData = CreateBuildingData(
                "DecGarden", new Vector2Int(3, 1), height: 0.6f,
                wallColor: new Color(0.5f, 0.55f, 0.4f), roofColor: new Color(0.46f, 0.52f, 0.36f),
                style: BuildingStyle.Plot, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.GardenBeds);

            // The one building whose loss ends the game, so it is the one silhouette that has to be
            // findable from anywhere on the map: a walled base with a keep and a banner over it.
            var townHallData = CreateBuildingData(
                BuildingIds.TownHall, new Vector2Int(5, 5), height: 4.4f,
                wallColor: new Color(0.5f, 0.48f, 0.44f), roofColor: new Color(0.42f, 0.24f, 0.22f),
                style: BuildingStyle.Fortification, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.Keep);

            var fishermanHutData = CreateBuildingData(
                "FisherHut", new Vector2Int(1, 1), height: 2f,
                wallColor: new Color(0.55f, 0.52f, 0.45f), roofColor: new Color(0.2f, 0.5f, 0.55f),
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.DryingRacks);

            var farmData = CreateBuildingData(
                "Farm", new Vector2Int(1, 1), height: 1.9f,
                wallColor: new Color(0.68f, 0.55f, 0.3f), roofColor: new Color(0.42f, 0.58f, 0.24f),
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.Field);

            var lumberjackData = CreateBuildingData(
                "Sawmill", new Vector2Int(2, 2), height: 2.4f,
                wallColor: new Color(0.45f, 0.3f, 0.18f), roofColor: new Color(0.32f, 0.22f, 0.13f),
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.LogPile);

            var quarryData = CreateBuildingData(
                "Quarry", new Vector2Int(2, 2), height: 2f,
                wallColor: new Color(0.55f, 0.53f, 0.48f), roofColor: new Color(0.3f, 0.29f, 0.27f),
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.StoneYard);

            // All four mines share the pit-head frame and are told apart by what comes out of the
            // ground: the ore heap and the walls carry the metal's own colour.
            var mineData = CreateBuildingData(
                "IronMine", new Vector2Int(2, 2), height: 2.2f,
                wallColor: new Color(0.4f, 0.38f, 0.36f), roofColor: new Color(0.5f, 0.5f, 0.56f),
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.Headframe);

            var coalMineData = CreateBuildingData(
                "CoalMine", new Vector2Int(2, 2), height: 2.2f,
                wallColor: new Color(0.3f, 0.28f, 0.27f), roofColor: new Color(0.14f, 0.14f, 0.15f),
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.Headframe);

            var towerData = CreateBuildingData(
                "Tower", new Vector2Int(1, 1), height: 4.2f,
                wallColor: new Color(0.4f, 0.38f, 0.34f), roofColor: new Color(0.42f, 0.24f, 0.22f),
                style: BuildingStyle.Tower, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                connectsToFences: true);

            var barracksData = CreateBuildingData(
                "Barracks", new Vector2Int(3, 3), height: 2.8f,
                wallColor: new Color(0.5f, 0.48f, 0.44f), roofColor: new Color(0.42f, 0.24f, 0.22f),
                style: BuildingStyle.Fortification, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.Banner);

            // Towers and gates stand IN the fence line rather than beside it: connectsToFences
            // registers their cells with FenceNetwork, so the segments either side shape themselves
            // as if the line ran straight through. A gate is built on the flat shell rather than the
            // fortified one on purpose -- it has to read as something you can walk through.
            var gateData = CreateBuildingData(
                "Gate", new Vector2Int(1, 2), height: 2.2f,
                wallColor: new Color(0.4f, 0.38f, 0.34f), roofColor: new Color(0.34f, 0.32f, 0.3f),
                style: BuildingStyle.Plot, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                connectsToFences: true, motif: BuildingMotif.Archway);

            // The second tier of the defensive line. Separate buildings, not upgrade levels -- the
            // taxonomy treats them that way, and each still has its own three levels on top.
            var fortifiedTowerData = CreateBuildingData(
                "FortifiedTower", new Vector2Int(2, 2), height: 5f,
                wallColor: new Color(0.46f, 0.45f, 0.42f), roofColor: new Color(0.42f, 0.24f, 0.22f),
                style: BuildingStyle.Tower, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                connectsToFences: true, motif: BuildingMotif.Banner);

            var fortifiedGateData = CreateBuildingData(
                "FortifiedGate", new Vector2Int(2, 2), height: 2.8f,
                wallColor: new Color(0.44f, 0.42f, 0.4f), roofColor: new Color(0.34f, 0.32f, 0.3f),
                style: BuildingStyle.Plot, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                connectsToFences: true, motif: BuildingMotif.Archway);

            // One storehouse per resource family, so every group has somewhere to go -- what each
            // one holds is the storage_group column, not anything decided here. A store is a wide
            // double door and no windows; a granary is bins; a treasury is a strongbox behind walls.
            var warehouseData = CreateBuildingData(
                "Warehouse", new Vector2Int(2, 2), height: 2.6f,
                wallColor: new Color(0.58f, 0.46f, 0.3f), roofColor: new Color(0.36f, 0.3f, 0.24f),
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.BarnDoors);

            var barnData = CreateBuildingData(
                "Barn", new Vector2Int(1, 1), height: 2.4f,
                wallColor: new Color(0.7f, 0.5f, 0.28f), roofColor: new Color(0.5f, 0.36f, 0.2f),
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.BarnDoors);

            var treasuryData = CreateBuildingData(
                "Treasury", new Vector2Int(3, 2), height: 2.4f,
                wallColor: new Color(0.5f, 0.48f, 0.44f), roofColor: new Color(0.72f, 0.6f, 0.24f),
                style: BuildingStyle.Fortification, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.Vault);

            // The second tier of each storehouse. Nothing new mechanically -- storage_group and
            // storage_capacity already do all of it -- so what these add is the reason to keep
            // building outward once the first three are full. Each needs its smaller version
            // standing first (the requires column), which is also what keeps them out of the
            // opening minutes of a game.
            var bigWarehouseData = CreateBuildingData(
                "BigWarehouse", new Vector2Int(3, 3), height: 3.2f,
                wallColor: new Color(0.52f, 0.41f, 0.27f), roofColor: new Color(0.3f, 0.25f, 0.2f),
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.BarnDoors);

            var bigBarnData = CreateBuildingData(
                "BigBarn", new Vector2Int(2, 2), height: 3.4f,
                wallColor: new Color(0.66f, 0.45f, 0.24f), roofColor: new Color(0.45f, 0.31f, 0.17f),
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.BarnDoors);

            var bigTreasuryData = CreateBuildingData(
                "BigTreasury", new Vector2Int(4, 3), height: 3f,
                wallColor: new Color(0.46f, 0.44f, 0.41f), roofColor: new Color(0.78f, 0.66f, 0.28f),
                style: BuildingStyle.Fortification, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                motif: BuildingMotif.Vault);

            // Two models, one prefab: a fence segment shows the straight run or the corner depending
            // on the neighbours it finds (see FenceShape), and neither is a separate building.
            var fenceData = CreateFenceBuildingData("Fence", trimMaterial);

            var roadData = CreateBuildingData(
                "Road", new Vector2Int(1, 1), height: 0.05f,
                wallColor: new Color(0.32f, 0.32f, 0.34f), roofColor: new Color(0.85f, 0.75f, 0.3f),
                style: BuildingStyle.Road, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                isRoad: true, keepSelectedAfterPlacement: true);

            var bridgeData = CreateBuildingData(
                "Bridge", new Vector2Int(1, 1), height: 0.05f,
                wallColor: new Color(0.5f, 0.35f, 0.2f), roofColor: new Color(0.36f, 0.24f, 0.13f),
                style: BuildingStyle.Road, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                isRoad: true, keepSelectedAfterPlacement: true, isWaterCategory: true,
                // The whole point of a bridge: it's the only building that makes previously
                // uncrossable terrain walkable (see BuildingData.providesWalkableSurface).
                providesWalkableSurface: true);

            var waterMillData = CreateWaterBuildingData(
                "WaterMill", new Vector2Int(2, 2),
                deckColor: new Color(0.5f, 0.4f, 0.28f), accentColor: new Color(0.35f, 0.24f, 0.14f),
                addWheel: true);

            var dockData = CreateWaterBuildingData(
                "Dock", new Vector2Int(2, 2),
                deckColor: new Color(0.55f, 0.4f, 0.24f), accentColor: new Color(0.68f, 0.5f, 0.3f),
                addCrates: true);

            // Buildings whose models arrived ahead of their mechanics. They can be placed, they
            // cost what the sheet says and they do nothing whatsoever -- no production, no storage,
            // no defence, no workers. Each is waiting on a system that does not exist yet
            // (processing chains, fire, illness, happiness, soldier upgrades), and the balance row
            // for each says so in its comment. They are in the game because a town that can only
            // build the implemented half of the design does not look like the design.
            var shellBuildings = new List<BuildingData>();
            foreach (var shell in new[]
            {
                ("Granary", new Vector2Int(2, 1), 2.6f, new Color(0.68f, 0.56f, 0.34f), new Color(0.45f, 0.35f, 0.22f), BuildingStyle.Hut, BuildingMotif.Silo),
                ("BigGranary", new Vector2Int(3, 2), 3.2f, new Color(0.64f, 0.52f, 0.3f), new Color(0.4f, 0.31f, 0.2f), BuildingStyle.Hut, BuildingMotif.Silo),
                ("Baker", new Vector2Int(1, 2), 2.4f, new Color(0.8f, 0.66f, 0.46f), new Color(0.5f, 0.3f, 0.2f), BuildingStyle.Hut, BuildingMotif.Stack),
                ("Windmill", new Vector2Int(2, 2), 3.4f, new Color(0.75f, 0.72f, 0.64f), new Color(0.42f, 0.34f, 0.24f), BuildingStyle.Hut, BuildingMotif.Sails),
                ("Smokehouse", new Vector2Int(1, 2), 2.4f, new Color(0.5f, 0.4f, 0.32f), new Color(0.3f, 0.24f, 0.2f), BuildingStyle.Hut, BuildingMotif.Stack),
                ("PigFarm", new Vector2Int(3, 4), 1.9f, new Color(0.72f, 0.6f, 0.5f), new Color(0.45f, 0.36f, 0.26f), BuildingStyle.Hut, BuildingMotif.Pen),
                ("Orchard", new Vector2Int(3, 3), 2.6f, new Color(0.46f, 0.5f, 0.32f), new Color(0.4f, 0.46f, 0.28f), BuildingStyle.Plot, BuildingMotif.Orchard),
                ("Smelter", new Vector2Int(2, 3), 2.8f, new Color(0.42f, 0.4f, 0.38f), new Color(0.28f, 0.26f, 0.25f), BuildingStyle.Hut, BuildingMotif.Stack),
                ("CopperMine", new Vector2Int(2, 2), 2.2f, new Color(0.55f, 0.4f, 0.3f), new Color(0.5f, 0.5f, 0.55f), BuildingStyle.Hut, BuildingMotif.Headframe),
                ("GoldMine", new Vector2Int(2, 2), 2.2f, new Color(0.6f, 0.55f, 0.35f), new Color(0.5f, 0.5f, 0.55f), BuildingStyle.Hut, BuildingMotif.Headframe),
                ("Laboratory", new Vector2Int(4, 4), 3f, new Color(0.55f, 0.5f, 0.6f), new Color(0.3f, 0.28f, 0.4f), BuildingStyle.Hut, BuildingMotif.Dome),
                ("HealerHouse", new Vector2Int(2, 4), 2.6f, new Color(0.86f, 0.84f, 0.8f), new Color(0.6f, 0.25f, 0.22f), BuildingStyle.Hut, BuildingMotif.Cross),
                ("FireBrigade", new Vector2Int(2, 2), 2.6f, new Color(0.7f, 0.3f, 0.24f), new Color(0.35f, 0.3f, 0.28f), BuildingStyle.Hut, BuildingMotif.HoseTower),
                ("Well", new Vector2Int(1, 1), 1.4f, new Color(0.55f, 0.53f, 0.5f), new Color(0.45f, 0.43f, 0.4f), BuildingStyle.Plot, BuildingMotif.WellHead),
                ("Church", new Vector2Int(3, 3), 3.4f, new Color(0.82f, 0.8f, 0.76f), new Color(0.35f, 0.4f, 0.5f), BuildingStyle.Hut, BuildingMotif.Spire),
                ("Tavern", new Vector2Int(2, 1), 2.6f, new Color(0.66f, 0.48f, 0.3f), new Color(0.4f, 0.28f, 0.18f), BuildingStyle.Hut, BuildingMotif.TavernSign),
                ("Theater", new Vector2Int(4, 4), 3.2f, new Color(0.78f, 0.74f, 0.66f), new Color(0.5f, 0.34f, 0.3f), BuildingStyle.Hut, BuildingMotif.Colonnade),
                ("Colosseum", new Vector2Int(4, 6), 3.6f, new Color(0.72f, 0.68f, 0.6f), new Color(0.5f, 0.47f, 0.42f), BuildingStyle.Plot, BuildingMotif.Arena),
            })
            {
                shellBuildings.Add(CreateBuildingData(
                    shell.Item1, shell.Item2, height: shell.Item3,
                    wallColor: shell.Item4, roofColor: shell.Item5,
                    style: shell.Item6, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                    motif: shell.Item7));
            }

            var hotbarBuildingData = new List<BuildingData>
            {
                houseData, cottageData, manorData, fishermanHutData, farmData,
                lumberjackData, quarryData, mineData, coalMineData,
                warehouseData, barnData, treasuryData,
                bigWarehouseData, bigBarnData, bigTreasuryData,
                roadData, fenceData, towerData, barracksData, gateData,
                fortifiedTowerData, fortifiedGateData,
                bridgeData, waterMillData, dockData,
                townSquareData, flagData, decTreeData, decBushData, decGardenData
            };
            hotbarBuildingData.AddRange(shellBuildings);
            var allBuildingData = new List<BuildingData>(hotbarBuildingData) { townHallData };

            ResolveBuildingRequirements(allBuildingData);

            AssetDatabase.SaveAssets();

            // The buildable area matches GridManager's logical grid exactly, but the visible
            // ground extends further out with a decorative forest border around it (see
            // CreateForestBorder), so the map doesn't just stop at a bare edge.
            var groundMaterial = CreateGroundMaterial();
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            var groundCellsX = GridCellsX + ForestMarginCells * 2;
            var groundCellsZ = GridCellsZ + ForestMarginCells * 2;
            var groundWidth = groundCellsX * CellSize;
            var groundDepth = groundCellsZ * CellSize;
            ground.transform.localScale = new Vector3(groundWidth / 10f, 1f, groundDepth / 10f);
            ground.transform.position = Vector3.zero; // GroundOrigin is symmetric around world origin
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMaterial;

            // Also handed to MapTerrainGenerator below, so its Forest/Stone resource-node props
            // (trees/rocks) share the exact same materials as the decorative forest border.
            var treeTrunkMaterial = CreateLitMaterial("TreeTrunk", new Color(0.36f, 0.24f, 0.14f));
            var treeCanopyMaterial = CreateLitMaterial("TreeCanopy", new Color(0.16f, 0.38f, 0.18f));
            var rockMaterial = CreateLitMaterial("Rock", new Color(0.55f, 0.53f, 0.48f));

            CreateForestBorder(GridCellsX * CellSize * 0.5f, GridCellsZ * CellSize * 0.5f, groundWidth * 0.5f, groundDepth * 0.5f, treeTrunkMaterial, treeCanopyMaterial);

            var minimapTexture = CreateMinimapCamera();

            var mainCameraGO = GameObject.Find("Main Camera");
            var camera = mainCameraGO.GetComponent<Camera>();

            var rig = new GameObject("CameraRig");
            var pivot = new GameObject("Pivot");
            pivot.transform.SetParent(rig.transform, false);
            pivot.transform.localRotation = Quaternion.Euler(50f, 0f, 0f);

            mainCameraGO.transform.SetParent(pivot.transform, false);
            mainCameraGO.transform.localPosition = new Vector3(0f, 0f, -25f);
            mainCameraGO.transform.localRotation = Quaternion.identity;
            rig.transform.position = Vector3.zero;

            var rtsCamera = rig.AddComponent<RTSCameraController>();
            var rigSO = new SerializedObject(rtsCamera);
            rigSO.FindProperty("pivot").objectReferenceValue = pivot.transform;
            rigSO.FindProperty("cameraTransform").objectReferenceValue = mainCameraGO.transform;
            rigSO.FindProperty("targetCamera").objectReferenceValue = camera;
            var boundsMargin = 6f;
            rigSO.FindProperty("panBoundsMin").vector2Value = new Vector2(GroundOrigin.x + boundsMargin, GroundOrigin.z + boundsMargin);
            rigSO.FindProperty("panBoundsMax").vector2Value = new Vector2(-GroundOrigin.x - boundsMargin, -GroundOrigin.z - boundsMargin);
            rigSO.ApplyModifiedPropertiesWithoutUndo();

            var managers = new GameObject("GameManagers");
            managers.AddComponent<FrameRateController>();

            var gridManager = managers.AddComponent<GridManager>();
            var gridSO = new SerializedObject(gridManager);
            gridSO.FindProperty("cellSize").floatValue = CellSize;
            gridSO.FindProperty("originWorldPosition").vector3Value = GroundOrigin;
            gridSO.FindProperty("gridSize").vector2IntValue = new Vector2Int(GridCellsX, GridCellsZ);
            gridSO.FindProperty("groundHeight").floatValue = GroundHeight;
            gridSO.ApplyModifiedPropertiesWithoutUndo();

            // Every world gesture in the game is recognised here and nowhere else -- added before
            // the camera, the placer and the selectors so its Instance exists when they subscribe.
            managers.AddComponent<CityBuilder.InputControl.TouchInputRouter>();

            managers.AddComponent<ResourceManager>();
            managers.AddComponent<RoadNetwork>();
            managers.AddComponent<FenceNetwork>();
            var gameCalendar = managers.AddComponent<GameCalendar>();
            managers.AddComponent<EventLogManager>();

            var placer = managers.AddComponent<BuildingPlacer>();
            var placerSO = new SerializedObject(placer);
            placerSO.FindProperty("targetCamera").objectReferenceValue = camera;
            placerSO.FindProperty("mandatoryFirstBuilding").objectReferenceValue = townHallData;
            var availableProp = placerSO.FindProperty("availableBuildings");
            availableProp.arraySize = hotbarBuildingData.Count;
            for (var i = 0; i < hotbarBuildingData.Count; i++)
            {
                availableProp.GetArrayElementAtIndex(i).objectReferenceValue = hotbarBuildingData[i];
            }
            placerSO.ApplyModifiedPropertiesWithoutUndo();

            // The placer switches the camera's inertia off while a building is being aimed: a
            // glide after the finger lifts would slide the aim point off the cell just lined up.
            var placerCameraSO = new SerializedObject(placer);
            placerCameraSO.FindProperty("cameraController").objectReferenceValue = rtsCamera;
            placerCameraSO.ApplyModifiedPropertiesWithoutUndo();

            var citizenManager = managers.AddComponent<CitizenManager>();
            var citizenManagerSO = new SerializedObject(citizenManager);
            citizenManagerSO.FindProperty("buildingPlacer").objectReferenceValue = placer;
            citizenManagerSO.ApplyModifiedPropertiesWithoutUndo();

            var settlementTierManager = managers.AddComponent<SettlementTierManager>();
            var settlementTierManagerSO = new SerializedObject(settlementTierManager);
            settlementTierManagerSO.FindProperty("citizenManager").objectReferenceValue = citizenManager;
            settlementTierManagerSO.ApplyModifiedPropertiesWithoutUndo();

            var taxManager = managers.AddComponent<TaxManager>();
            var taxManagerSO = new SerializedObject(taxManager);
            taxManagerSO.FindProperty("citizenManager").objectReferenceValue = citizenManager;
            taxManagerSO.ApplyModifiedPropertiesWithoutUndo();

            // The settlement's daily meal. HappinessManager reads its numbers for two of the five
            // happiness factors, and GameOverManager ends the map when it starves the town out.
            var foodConsumptionManager = managers.AddComponent<FoodConsumptionManager>();
            var foodConsumptionManagerSO = new SerializedObject(foodConsumptionManager);
            foodConsumptionManagerSO.FindProperty("citizenManager").objectReferenceValue = citizenManager;
            foodConsumptionManagerSO.ApplyModifiedPropertiesWithoutUndo();

            // Illness, on the same daily clock as the meal above and reading it: hunger is half of
            // why people fall ill, and a well being out of reach is the other half.
            var sicknessManager = managers.AddComponent<CityBuilder.Citizens.SicknessManager>();
            var sicknessManagerSO = new SerializedObject(sicknessManager);
            sicknessManagerSO.FindProperty("citizenManager").objectReferenceValue = citizenManager;
            sicknessManagerSO.ApplyModifiedPropertiesWithoutUndo();

            // Fires. No wiring: it finds what is standing, what is burning and who can reach it by
            // scanning, once a game day, and every number it needs is in the sheet.
            managers.AddComponent<FireManager>();

            managers.AddComponent<HappinessManager>();

            // What contentment is finally FOR: it sets how fast people find their way here, or
            // away. Needs the placer as well as the citizens, because the settling-in grace period
            // starts when the Town Hall is placed rather than when the scene loads.
            var migrationManager = managers.AddComponent<MigrationManager>();
            var migrationManagerSO = new SerializedObject(migrationManager);
            migrationManagerSO.FindProperty("citizenManager").objectReferenceValue = citizenManager;
            migrationManagerSO.FindProperty("buildingPlacer").objectReferenceValue = placer;
            migrationManagerSO.ApplyModifiedPropertiesWithoutUndo();

            managers.AddComponent<GameOverManager>();

            // No wiring: raids are sized and paced against the player's progression score rather
            // than the calendar (see PlayerProgression), so the raid clock stopped needing the day
            // count the moment that changed.
            managers.AddComponent<OrcRaidManager>();

            // The player's side of the war: recruitment, groups and the daily upkeep charge (hence
            // the calendar reference, same as the raid clock's).
            var armyManager = managers.AddComponent<ArmyManager>();
            var armyManagerSO = new SerializedObject(armyManager);
            armyManagerSO.FindProperty("gameCalendar").objectReferenceValue = gameCalendar;
            armyManagerSO.ApplyModifiedPropertiesWithoutUndo();

            // The Laboratory's brain. No wiring of its own: it finds the Laboratory through
            // BuildingInstance's registry, because the building comes and goes while it does not.
            var researchManager = managers.AddComponent<CityBuilder.Research.ResearchManager>();

            var citizenVisualsManager = managers.AddComponent<CitizenVisualsManager>();
            var citizenVisualsManagerSO = new SerializedObject(citizenVisualsManager);
            citizenVisualsManagerSO.FindProperty("citizenManager").objectReferenceValue = citizenManager;
            citizenVisualsManagerSO.FindProperty("gridManager").objectReferenceValue = gridManager;
            citizenVisualsManagerSO.ApplyModifiedPropertiesWithoutUndo();

            var fogOfWarManager = managers.AddComponent<FogOfWarManager>();
            var fogOfWarManagerSO = new SerializedObject(fogOfWarManager);
            fogOfWarManagerSO.FindProperty("buildingPlacer").objectReferenceValue = placer;
            fogOfWarManagerSO.FindProperty("citizenVisualsManager").objectReferenceValue = citizenVisualsManager;
            fogOfWarManagerSO.ApplyModifiedPropertiesWithoutUndo();

            var saveController = managers.AddComponent<GameSaveController>();
            var saveControllerSO = new SerializedObject(saveController);
            saveControllerSO.FindProperty("buildingPlacer").objectReferenceValue = placer;
            saveControllerSO.FindProperty("citizenManager").objectReferenceValue = citizenManager;
            saveControllerSO.FindProperty("gameCalendar").objectReferenceValue = gameCalendar;
            saveControllerSO.FindProperty("taxManager").objectReferenceValue = taxManager;
            saveControllerSO.FindProperty("researchManager").objectReferenceValue = researchManager;
            var knownBuildingsProp = saveControllerSO.FindProperty("knownBuildings");
            knownBuildingsProp.arraySize = allBuildingData.Count;
            for (var i = 0; i < allBuildingData.Count; i++)
            {
                knownBuildingsProp.GetArrayElementAtIndex(i).objectReferenceValue = allBuildingData[i];
            }
            saveControllerSO.ApplyModifiedPropertiesWithoutUndo();

            CreateMeshMapDefinition("Map1");

            // Added BEFORE MapTerrainGenerator (Unity runs Start() in add-order on the same
            // GameObject) so it consumes GameSessionIntent.NewGameMapId first for a mesh-map new
            // game; MapTerrainGenerator's existing "not found in catalog" no-op then harmlessly
            // skips it, and vice versa for a legacy PNG map id.
            var meshMapApplier = managers.AddComponent<MeshMapApplier>();
            var meshMapApplierSO = new SerializedObject(meshMapApplier);
            meshMapApplierSO.FindProperty("saveController").objectReferenceValue = saveController;
            meshMapApplierSO.FindProperty("baseGroundToHide").objectReferenceValue = ground;
            meshMapApplierSO.FindProperty("baseForestBorderToHide").objectReferenceValue = GameObject.Find("Forest");
            meshMapApplierSO.ApplyModifiedPropertiesWithoutUndo();

            managers.AddComponent<TreesAreaSpawner>();
            managers.AddComponent<RockSpawner>();

            BuildCheats();

            // Picks up whichever map MainMenuController/MapSelector chose for a new game, or the
            // one stored in a loaded save (see GameSaveController.LoadedMapId), and paints it.
            var mapTerrainGenerator = managers.AddComponent<MapTerrainGenerator>();
            var mapTerrainGeneratorSO = new SerializedObject(mapTerrainGenerator);
            mapTerrainGeneratorSO.FindProperty("saveController").objectReferenceValue = saveController;
            mapTerrainGeneratorSO.FindProperty("treeTrunkMaterial").objectReferenceValue = treeTrunkMaterial;
            mapTerrainGeneratorSO.FindProperty("treeCanopyMaterial").objectReferenceValue = treeCanopyMaterial;
            mapTerrainGeneratorSO.FindProperty("rockMaterial").objectReferenceValue = rockMaterial;
            mapTerrainGeneratorSO.ApplyModifiedPropertiesWithoutUndo();

            var saveControllerMapFieldSO = new SerializedObject(saveController);
            saveControllerMapFieldSO.FindProperty("mapTerrainGenerator").objectReferenceValue = mapTerrainGenerator;
            saveControllerMapFieldSO.FindProperty("meshMapApplier").objectReferenceValue = meshMapApplier;
            saveControllerMapFieldSO.ApplyModifiedPropertiesWithoutUndo();

            BuildGameplayUI(placer, saveController, camera, rtsCamera, settlementTierManager, gameCalendar, hotbarBuildingData, minimapTexture, townHallData);

            Directory.CreateDirectory(ScenesFolder);
            DeleteIfExists($"{ScenesFolder}/CityBuilder.unity");
            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/CityBuilder.unity");
        }

        private static void BuildGameplayUI(BuildingPlacer placer, GameSaveController saveController, Camera targetCamera, RTSCameraController cameraRig, SettlementTierManager settlementTierManager, GameCalendar gameCalendar, List<BuildingData> hotbarBuildings, RenderTexture minimapTexture, BuildingData mandatoryBuilding)
        {
            // Created here (no NewScene() call happens between this and its uses below/this
            // scene being saved) rather than reused from BuildMainMenuScene's sprite, since that
            // one goes stale the moment this method's caller (BuildCityScene) switched scenes.
            var panelSprite = CreatePanelSprite();

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;

            // Everything in the HUD hangs off this rather than off the Canvas directly: it is the
            // rect that shrinks to Screen.safeArea, keeping the HUD clear of the camera cutout and
            // the system gesture bar. The Canvas itself cannot do the job -- Unity drives its own
            // RectTransform, so anchors written onto it are overwritten every frame.
            var safeAreaGO = new GameObject("SafeArea", typeof(RectTransform));
            safeAreaGO.transform.SetParent(canvasGO.transform, false);
            StretchFull(safeAreaGO.GetComponent<RectTransform>());
            safeAreaGO.AddComponent<SafeAreaFitter>();
            var uiRoot = safeAreaGO.transform;


            // Hint shown only while the player must place the mandatory Town Hall.
            var hintRoot = CreateImage(uiRoot, "PlacementHint", new Color(0f, 0f, 0f, 0.6f));
            hintRoot.sprite = panelSprite;
            hintRoot.type = Image.Type.Sliced;
            var hintRect = hintRoot.GetComponent<RectTransform>();
            hintRect.anchorMin = hintRect.anchorMax = new Vector2(0.5f, 1f);
            hintRect.anchoredPosition = new Vector2(0f, -70f);
            hintRect.sizeDelta = new Vector2(780f, 84f);
            var footprint = mandatoryBuilding != null ? mandatoryBuilding.footprintSize : new Vector2Int(4, 4);
            CreateText(hintRoot.transform, "Text", Localization.Format("#hud_place_hint", footprint.x, footprint.y), 30, Vector2.zero, new Vector2(740f, 74f));

            // Touch-friendly building hotbar, grouped into categories (a small category row above
            // it picks which category's buildings the hotbar shows -- see BuildingCategoryPanel).
            // Shown only once the Town Hall is placed (there's nothing else buildable before that
            // point). Number-key hotkeys still work on PC, against the same availableBuildings
            // list regardless of which category is currently shown.
            var menuGO = new GameObject("BuildingMenu", typeof(RectTransform));
            menuGO.transform.SetParent(uiRoot, false);
            StretchFull(menuGO.GetComponent<RectTransform>());
            var categoryPanel = menuGO.AddComponent<BuildingCategoryPanel>();

            const float buttonSize = 130f;
            const float spacing = 16f;

            // HorizontalLayoutGroup (not manual per-button positions) because only one category's
            // buildings are active at a time (see BuildingCategoryPanel) -- the layout group
            // automatically excludes inactive children and re-centers around whichever subset is
            // currently shown, where fixed absolute offsets computed from the full 12-building
            // list would leave a category's buttons stranded off-center.
            var hotbarGO = new GameObject("Hotbar", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            hotbarGO.transform.SetParent(menuGO.transform, false);
            var hotbarRect = hotbarGO.GetComponent<RectTransform>();
            hotbarRect.anchorMin = new Vector2(0.5f, 0f);
            hotbarRect.anchorMax = new Vector2(0.5f, 0f);
            hotbarRect.pivot = new Vector2(0.5f, 0f);
            hotbarRect.anchoredPosition = new Vector2(0f, 28f);
            hotbarRect.sizeDelta = new Vector2(0f, buttonSize);

            var hotbarLayout = hotbarGO.GetComponent<HorizontalLayoutGroup>();
            hotbarLayout.spacing = spacing;
            hotbarLayout.childAlignment = TextAnchor.MiddleCenter;
            hotbarLayout.childControlWidth = false;
            hotbarLayout.childControlHeight = false;
            hotbarLayout.childForceExpandWidth = false;
            hotbarLayout.childForceExpandHeight = false;

            var hotbarFitter = hotbarGO.GetComponent<ContentSizeFitter>();
            hotbarFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            hotbarFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            for (var i = 0; i < hotbarBuildings.Count; i++)
            {
                var data = hotbarBuildings[i];
                var icon = CreateBuildingIcon(data.buildingName);
                var button = CreateIconButton(hotbarGO.transform, panelSprite, icon, $"Building_{data.buildingName}", Vector2.zero, new Vector2(buttonSize, buttonSize));

                var handler = button.gameObject.AddComponent<HotbarButtonHandler>();
                var handlerSO = new SerializedObject(handler);
                handlerSO.FindProperty("buildingPlacer").objectReferenceValue = placer;
                handlerSO.FindProperty("building").objectReferenceValue = data;
                handlerSO.ApplyModifiedPropertiesWithoutUndo();

                UnityEventTools.AddPersistentListener(button.onClick, handler.SelectThisBuilding);
            }

            // Fixed display order; only categories that actually have a hotbar building get a tab.
            var categoryOrder = new[] { BuildingCategory.City, BuildingCategory.Storage, BuildingCategory.Entertainment, BuildingCategory.Defence, BuildingCategory.Production, BuildingCategory.Food, BuildingCategory.Water };
            var presentCategories = new List<BuildingCategory>();
            foreach (var cat in categoryOrder)
            {
                if (hotbarBuildings.Exists(b => b.category == cat)) presentCategories.Add(cat);
            }

            // 110, not 80: at a 1080-high reference 80 px lands at roughly 5 mm on a dense
            // phone screen, half of what a finger reliably hits.
            const float categoryButtonSize = 110f;
            var categoryTotalWidth = presentCategories.Count * categoryButtonSize + Mathf.Max(0, presentCategories.Count - 1) * spacing;

            var categoryBarGO = new GameObject("CategoryBar", typeof(RectTransform));
            categoryBarGO.transform.SetParent(menuGO.transform, false);
            var categoryBarRect = categoryBarGO.GetComponent<RectTransform>();
            categoryBarRect.anchorMin = new Vector2(0.5f, 0f);
            categoryBarRect.anchorMax = new Vector2(0.5f, 0f);
            categoryBarRect.pivot = new Vector2(0.5f, 0f);
            categoryBarRect.anchoredPosition = new Vector2(0f, hotbarRect.anchoredPosition.y + buttonSize + spacing);
            categoryBarRect.sizeDelta = new Vector2(categoryTotalWidth, categoryButtonSize);

            for (var i = 0; i < presentCategories.Count; i++)
            {
                var category = presentCategories[i];
                var icon = CreateCategoryIcon(category);
                var x = -categoryTotalWidth * 0.5f + categoryButtonSize * 0.5f + i * (categoryButtonSize + spacing);
                var categoryButton = CreateIconButton(categoryBarGO.transform, panelSprite, icon, $"Category_{category}", new Vector2(x, categoryButtonSize * 0.5f), new Vector2(categoryButtonSize, categoryButtonSize));

                var categoryHandler = categoryButton.gameObject.AddComponent<CategoryButtonHandler>();
                var categoryHandlerSO = new SerializedObject(categoryHandler);
                categoryHandlerSO.FindProperty("panel").objectReferenceValue = categoryPanel;
                categoryHandlerSO.FindProperty("category").enumValueIndex = (int)category;
                categoryHandlerSO.ApplyModifiedPropertiesWithoutUndo();

                UnityEventTools.AddPersistentListener(categoryButton.onClick, categoryHandler.SelectThisCategory);
            }

            var visibility = canvasGO.AddComponent<BuildingPlacerUIVisibility>();
            var visibilitySO = new SerializedObject(visibility);
            visibilitySO.FindProperty("buildingPlacer").objectReferenceValue = placer;
            visibilitySO.FindProperty("showWhilePlacingMandatory").objectReferenceValue = hintRoot.gameObject;
            visibilitySO.FindProperty("hideWhilePlacingMandatory").objectReferenceValue = menuGO;
            visibilitySO.ApplyModifiedPropertiesWithoutUndo();

            // The placement controls, stacked up the right edge: confirm at thumb height, then
            // rotate, then cancel. Confirm is the biggest of the three because it is pressed on
            // every single building, and because a mis-tap there costs a wrong placement -- see
            // PlacementHudController for which of them is visible when.
            var confirmIcon = CreateConfirmIcon();
            var confirmButton = CreateIconButton(uiRoot, panelSprite, confirmIcon, "ConfirmPlacementButton", Vector2.zero, new Vector2(130f, 130f));
            var confirmRect = confirmButton.GetComponent<RectTransform>();
            confirmRect.anchorMin = confirmRect.anchorMax = confirmRect.pivot = new Vector2(1f, 0f);
            confirmRect.anchoredPosition = new Vector2(-40f, 40f);
            UnityEventTools.AddPersistentListener(confirmButton.onClick, placer.ConfirmPlacement);

            // Rotation stays available even while placing the mandatory Town Hall -- it is not a
            // cancel, so there is no dead end to protect against. PC also has the R key.
            var rotateIcon = CreateRotateIcon();
            var rotateButton = CreateIconButton(uiRoot, panelSprite, rotateIcon, "RotateButton", Vector2.zero, new Vector2(110f, 110f));
            var rotateRect = rotateButton.GetComponent<RectTransform>();
            rotateRect.anchorMin = rotateRect.anchorMax = rotateRect.pivot = new Vector2(1f, 0f);
            rotateRect.anchoredPosition = new Vector2(-50f, 190f);
            UnityEventTools.AddPersistentListener(rotateButton.onClick, placer.RotateSelection);

            // Cancel. On a phone there was NO way to put a building back down: ClearSelection was
            // reachable only through Escape and the right mouse button, so a selected building the
            // player could not afford stayed stuck for the rest of the game.
            var cancelIcon = CreateCancelIcon();
            var cancelButton = CreateIconButton(uiRoot, panelSprite, cancelIcon, "CancelPlacementButton", Vector2.zero, new Vector2(110f, 110f));
            var cancelRect = cancelButton.GetComponent<RectTransform>();
            cancelRect.anchorMin = cancelRect.anchorMax = cancelRect.pivot = new Vector2(1f, 0f);
            cancelRect.anchoredPosition = new Vector2(-50f, 310f);
            UnityEventTools.AddPersistentListener(cancelButton.onClick, placer.ClearSelection);

            // The crosshair marking where the ghost is aimed. Positioned in SCREEN pixels by the
            // controller, which is why it is anchored to a corner rather than centred here.
            var aimMarker = CreateAimMarker(uiRoot);

            // One line saying why confirm is dark, or what a drawn line will cost. Bottom-centre
            // above the hotbar, where the eye already is while placing.
            var placementStatus = CreateText(uiRoot, "PlacementStatus", string.Empty, 30,
                Vector2.zero, new Vector2(900f, 44f), new Color(1f, 0.72f, 0.62f), addShadow: true);
            var statusRect = placementStatus.GetComponent<RectTransform>();
            statusRect.anchorMin = statusRect.anchorMax = statusRect.pivot = new Vector2(0.5f, 0f);
            statusRect.anchoredPosition = new Vector2(0f, 200f);
            placementStatus.gameObject.SetActive(false);

            // Lives on the Canvas itself (an always-active object), not on the buttons it
            // controls -- a script polling its own GameObject's active state would stop
            // receiving Update() calls the moment it deactivated it, and never reactivate it.
            var placementHud = canvasGO.AddComponent<PlacementHudController>();
            var placementHudSO = new SerializedObject(placementHud);
            placementHudSO.FindProperty("buildingPlacer").objectReferenceValue = placer;
            placementHudSO.FindProperty("confirmButton").objectReferenceValue = confirmButton.gameObject;
            placementHudSO.FindProperty("confirmImage").objectReferenceValue = confirmButton.GetComponent<Image>();
            placementHudSO.FindProperty("rotateButton").objectReferenceValue = rotateButton.gameObject;
            placementHudSO.FindProperty("cancelButton").objectReferenceValue = cancelButton.gameObject;
            placementHudSO.FindProperty("aimMarker").objectReferenceValue = aimMarker;
            placementHudSO.FindProperty("statusLabel").objectReferenceValue = placementStatus;
            placementHudSO.ApplyModifiedPropertiesWithoutUndo();

            BuildSaveUI(uiRoot, panelSprite, saveController);
            BuildExitUI(uiRoot, panelSprite);
            BuildGameOverUI(uiRoot, panelSprite);
            BuildResourceHUD(uiRoot);
            BuildMinimap(uiRoot, panelSprite, minimapTexture, cameraRig);

            var iconLibrary = CreateResourceIconLibrary();

            var infoPanel = BuildBuildingInfoPanel(uiRoot, panelSprite, iconLibrary);
            var researchPanel = BuildResearchPanel(uiRoot, panelSprite, iconLibrary);
            var buildingSelector = BuildBuildingSelector(uiRoot, targetCamera, infoPanel, researchPanel);
            var citizenSelector = BuildCitizenSelector(uiRoot, targetCamera);
            BuildSettlementTierToast(uiRoot, settlementTierManager);
            BuildEventLog(uiRoot, panelSprite);
            BuildTaxRateControl(uiRoot, panelSprite);
            BuildSettlementStatus(uiRoot, gameCalendar, settlementTierManager);
            BuildHappinessPanel(uiRoot, panelSprite);
            BuildMigrationPanel(uiRoot, panelSprite);
            BuildWorkforcePanel(uiRoot, panelSprite);
            BuildArmyPanel(uiRoot, panelSprite);
            var armyOrderInput = BuildArmyOrderInput(uiRoot, targetCamera);

            // One tap, one owner, decided in one place -- see WorldInputDispatcher for why the
            // four systems no longer check each other's state.
            var dispatcherGO = new GameObject("WorldInputDispatcher");
            dispatcherGO.transform.SetParent(uiRoot, false);
            var dispatcher = dispatcherGO.AddComponent<CityBuilder.InputControl.WorldInputDispatcher>();
            var dispatcherSO = new SerializedObject(dispatcher);
            dispatcherSO.FindProperty("buildingPlacer").objectReferenceValue = placer;
            dispatcherSO.FindProperty("armyOrderInput").objectReferenceValue = armyOrderInput;
            dispatcherSO.FindProperty("citizenSelector").objectReferenceValue = citizenSelector;
            dispatcherSO.FindProperty("buildingSelector").objectReferenceValue = buildingSelector;
            dispatcherSO.ApplyModifiedPropertiesWithoutUndo();

            BuildSelectionBadge(uiRoot, panelSprite, citizenSelector);
        }

        /// <summary>
        /// The army panel, directly under the Happiness panel on the right (same x 760 and width,
        /// see BuildHappinessPanel for why it isn't 810). Hidden by ArmyPanelController until the
        /// player actually has soldiers, so a settlement with no army isn't staring at an empty
        /// box -- which is also why the rows inside are built at runtime rather than here: their
        /// number changes with recruitment.
        /// </summary>
        private static void BuildArmyPanel(Transform canvasParent, Sprite panelSprite)
        {
            var frame = CreateImage(canvasParent, "Army", new Color(0.16f, 0.18f, 0.15f, 0.85f));
            frame.sprite = panelSprite;
            frame.type = Image.Type.Sliced;
            var frameRect = frame.GetComponent<RectTransform>();
            frameRect.anchorMin = frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameRect.anchoredPosition = new Vector2(760f, -60f);
            frameRect.sizeDelta = new Vector2(400f, 220f);

            var summaryLabel = CreateText(frame.transform, "ArmySummary", string.Empty, 19, new Vector2(0f, 88f), new Vector2(370f, 36f), new Color(1f, 1f, 1f, 0.75f));

            // Rows are added top-down from here by the controller; the container leaves room for
            // the summary line above it.
            var rowContainer = new GameObject("ArmyGroupRows", typeof(RectTransform));
            rowContainer.transform.SetParent(frame.transform, false);
            var rowRect = rowContainer.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 0f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.offsetMin = new Vector2(15f, 15f);
            rowRect.offsetMax = new Vector2(-15f, -50f);

            // On its own always-active object, NOT on the panel it shows: a component on a
            // deactivated GameObject never runs Start() and would never subscribe (same trap
            // documented on GameOverController).
            var go = new GameObject("ArmyPanelController");
            go.transform.SetParent(canvasParent, false);
            var controller = go.AddComponent<ArmyPanelController>();
            var so = new SerializedObject(controller);
            so.FindProperty("panelRoot").objectReferenceValue = frame.gameObject;
            so.FindProperty("rowContainer").objectReferenceValue = rowContainer.GetComponent<RectTransform>();
            so.FindProperty("summaryLabel").objectReferenceValue = summaryLabel;
            so.FindProperty("buttonSprite").objectReferenceValue = panelSprite;
            so.ApplyModifiedPropertiesWithoutUndo();

            frame.gameObject.SetActive(false);
        }

        /// <summary>Turns world taps into orders for the selected group (see ArmyOrderInput) -- inert while no group is selected, which is how it coexists with the citizen/building selectors.</summary>
        private static ArmyOrderInput BuildArmyOrderInput(Transform canvasParent, Camera targetCamera)
        {
            var go = new GameObject("ArmyOrderInput");
            go.transform.SetParent(canvasParent, false);
            var input = go.AddComponent<ArmyOrderInput>();
            var so = new SerializedObject(input);
            so.FindProperty("targetCamera").objectReferenceValue = targetCamera;
            so.ApplyModifiedPropertiesWithoutUndo();
            return input;
        }

        /// <summary>
        /// Right side, mirroring the EventLog panel's position/size on the left (x -760, y 100) --
        /// sits below the Minimap's bottom edge (y 192, see BuildMinimap) with a clear gap, same
        /// standing-panel treatment as EventLog/TaxRate rather than a toast, since happiness is
        /// meant to be glanced at over time. x is 760, not the 810 the Minimap/Menu/Save corners
        /// use -- at this panel's 400 width that would put its right edge at 1010, past the
        /// canvas's actual visible edge (960 at exact 16:9) and clip it, as seen in a real
        /// gameplay screenshot (2026-08-12) where the equivalent TaxRate panel's minus button was
        /// invisible off-screen for the same reason.
        /// </summary>
        private static void BuildHappinessPanel(Transform canvasParent, Sprite panelSprite)
        {
            var frame = CreateImage(canvasParent, "Happiness", new Color(0.16f, 0.18f, 0.15f, 0.85f));
            frame.sprite = panelSprite;
            frame.type = Image.Type.Sliced;
            var frameRect = frame.GetComponent<RectTransform>();
            frameRect.anchorMin = frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameRect.anchoredPosition = new Vector2(760f, 100f);
            // 130 rather than 110: the breakdown became two lines when food and losses joined the
            // three original factors (see HappinessController).
            frameRect.sizeDelta = new Vector2(400f, 130f);

            var happinessLabel = CreateText(frame.transform, "HappinessLabel", string.Empty, 26, new Vector2(0f, 32f), new Vector2(370f, 44f));
            var breakdownLabel = CreateText(frame.transform, "BreakdownLabel", string.Empty, 17, new Vector2(0f, -22f), new Vector2(370f, 56f), new Color(1f, 1f, 1f, 0.7f));

            var go = new GameObject("HappinessController");
            go.transform.SetParent(canvasParent, false);
            var controller = go.AddComponent<HappinessController>();
            var so = new SerializedObject(controller);
            so.FindProperty("happinessLabel").objectReferenceValue = happinessLabel;
            so.FindProperty("breakdownLabel").objectReferenceValue = breakdownLabel;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// The migration line, in the bottom-right corner where the player's thumb already is.
        ///
        /// Its own panel rather than another line inside the Happiness one, because it is a live
        /// countdown and the happiness panel is a thing you glance at -- and because the resource
        /// bar along the top is laid out in fixed-width slots (see ResourceHUDController.PlaceChip)
        /// where squeezing in a seventh chip would re-proportion the whole row.
        ///
        /// y is 200, which is what it takes to clear everything already stacked in that corner:
        /// the rotate button (40 to 130) and, more to the point, the hotbar (28 to 158), which at
        /// a full category of twelve buildings is 1736 wide and reaches well past x 520. The
        /// category bar above it is only 656 wide and never gets out this far. The Army panel is
        /// the ceiling, at 370 up from the bottom.
        ///
        /// Anchored to the corner itself rather than positioned from the centre, so it stays put
        /// on a phone's aspect ratio instead of drifting off the edge the way the Happiness panel
        /// had to be pulled in from 810 to 760 to avoid.
        /// </summary>
        private static void BuildMigrationPanel(Transform canvasParent, Sprite panelSprite)
        {
            var frame = CreateImage(canvasParent, "Migration", new Color(0.16f, 0.18f, 0.15f, 0.85f));
            frame.sprite = panelSprite;
            frame.type = Image.Type.Sliced;
            var frameRect = frame.GetComponent<RectTransform>();
            frameRect.anchorMin = frameRect.anchorMax = frameRect.pivot = new Vector2(1f, 0f);
            frameRect.anchoredPosition = new Vector2(-40f, 200f);
            frameRect.sizeDelta = new Vector2(400f, 72f);

            var label = CreateText(frame.transform, "MigrationLabel", string.Empty, 22, Vector2.zero, new Vector2(370f, 60f));

            var go = new GameObject("MigrationController");
            go.transform.SetParent(canvasParent, false);
            var controller = go.AddComponent<MigrationController>();
            var so = new SerializedObject(controller);
            so.FindProperty("panel").objectReferenceValue = frame.gameObject;
            so.FindProperty("label").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Testing-only tools, kept in their own GameCheats branch of the hierarchy rather than
        /// mixed into GameManagers so it's obvious at a glance which objects are the game and
        /// which are scaffolding. Each cheat is its own child so it gets its own Inspector with
        /// its own buttons (see CheatInspectors). Nothing here runs unless the tester acts --
        /// they're event-driven with no Update.
        /// </summary>
        private static void BuildCheats()
        {
            var root = new GameObject("GameCheats");

            var orcSpawn = new GameObject("OrcSpawn");
            orcSpawn.transform.SetParent(root.transform, false);
            orcSpawn.AddComponent<OrcSpawnCheat>();

            var resources = new GameObject("Resources");
            resources.transform.SetParent(root.transform, false);
            resources.AddComponent<ResourceCheat>();

            // The Laboratory gates thirty-one buildings and every upgrade level, so testing anything
            // late in the game without this would mean playing the whole tech list first.
            var research = new GameObject("Research");
            research.transform.SetParent(root.transform, false);
            research.AddComponent<ResearchCheat>();
        }

        /// <summary>
        /// Every workplace in the settlement in one scrollable card, with the same +/- controls the
        /// building's own info panel carries -- see WorkforcePanelController for why it lists
        /// buildings rather than people.
        ///
        /// A modal card like BuildingInfoPanel rather than a standing panel: at a settlement's
        /// worth of workshops this list is far too tall to leave on screen, and it is something the
        /// player consults when rearranging rather than watches.
        /// </summary>
        private static void BuildWorkforcePanel(Transform canvasParent, Sprite panelSprite)
        {
            var panelRoot = new GameObject("WorkforcePanel", typeof(RectTransform));
            panelRoot.transform.SetParent(canvasParent, false);
            StretchFull(panelRoot.GetComponent<RectTransform>());

            var backdrop = CreateImage(panelRoot.transform, "Backdrop", new Color(0f, 0f, 0f, 0.7f));
            StretchFull(backdrop.GetComponent<RectTransform>());

            var card = CreateImage(panelRoot.transform, "Card", new Color(0.16f, 0.18f, 0.15f, 0.98f));
            card.sprite = panelSprite;
            card.type = Image.Type.Sliced;
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(960f, 780f);
            cardRect.anchoredPosition = Vector2.zero;

            Localize(CreateText(card.transform, "Title", string.Empty, 34, new Vector2(0f, 320f), new Vector2(880f, 60f)), "#workforce_title");
            var summaryLabel = CreateText(card.transform, "Summary", string.Empty, 22, new Vector2(0f, 272f), new Vector2(880f, 40f), new Color(1f, 1f, 1f, 0.8f));

            BuildScrollList(card.transform, new Vector2(0f, -20f), new Vector2(900f, 520f),
                "#workforce_empty", out var content, out var emptyLabelGO);

            var closeButton = CreateButton(card.transform, panelSprite, "CloseButton", "#ui_close", new Vector2(0f, -320f), new Vector2(300f, 70f));

            var controller = panelRoot.AddComponent<WorkforcePanelController>();
            var controllerSO = new SerializedObject(controller);
            controllerSO.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            controllerSO.FindProperty("listContent").objectReferenceValue = content;
            controllerSO.FindProperty("emptyLabel").objectReferenceValue = emptyLabelGO;
            controllerSO.FindProperty("summaryLabel").objectReferenceValue = summaryLabel;
            controllerSO.FindProperty("rowSprite").objectReferenceValue = panelSprite;
            controllerSO.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(closeButton.onClick, controller.ClosePanel);

            // In the top row's left gap: Меню occupies -930..-690 and the settlement status text
            // -250..250, so a 240-wide button centred at -520 sits clear of both.
            var openButton = CreateButton(canvasParent, panelSprite, "WorkforceButton", "#hud_workforce", new Vector2(-520f, 465f), new Vector2(240f, 90f));
            UnityEventTools.AddPersistentListener(openButton.onClick, controller.OpenPanel);

            panelRoot.SetActive(false);
        }

        /// <summary>Top-center, same y band as the Меню/Сохранить corner buttons (465) but in the open gap between them (each 240 wide at x +-810, leaving -690..690 clear).</summary>
        private static void BuildSettlementStatus(Transform canvasParent, GameCalendar gameCalendar, SettlementTierManager settlementTierManager)
        {
            var statusText = CreateText(canvasParent, "SettlementStatus", string.Empty, 26, new Vector2(0f, 465f), new Vector2(500f, 60f), new Color(1f, 1f, 1f, 0.85f), addShadow: true);

            var go = new GameObject("SettlementStatusController");
            go.transform.SetParent(canvasParent, false);
            var controller = go.AddComponent<SettlementStatusController>();
            var so = new SerializedObject(controller);
            so.FindProperty("gameCalendar").objectReferenceValue = gameCalendar;
            so.FindProperty("tierManager").objectReferenceValue = settlementTierManager;
            so.FindProperty("statusText").objectReferenceValue = statusText;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Wedged into the x -760 column between the ResourceHUD's bottom edge (y 345) and the
        /// EventLog panel's top edge (y 260) -- centered at y 302 with a 60-tall frame keeps a
        /// ~12px clear margin on both sides instead of the two overlapping (an earlier pass at y
        /// 340/70-tall clipped into the ResourceHUD's leftmost icon column). x moved in from -810
        /// to -760 (2026-08-12): at 400 wide, -810 put this panel's left edge at -1010, past the
        /// canvas's actual visible edge (-960 at exact 16:9) -- confirmed in a real gameplay
        /// screenshot where the minus button (the panel's leftmost element) was clipped off-screen
        /// entirely, not just visually tight.
        /// </summary>
        private static void BuildTaxRateControl(Transform canvasParent, Sprite panelSprite)
        {
            var frame = CreateImage(canvasParent, "TaxRate", new Color(0.16f, 0.18f, 0.15f, 0.85f));
            frame.sprite = panelSprite;
            frame.type = Image.Type.Sliced;
            var frameRect = frame.GetComponent<RectTransform>();
            frameRect.anchorMin = frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameRect.anchoredPosition = new Vector2(-760f, 302f);
            frameRect.sizeDelta = new Vector2(400f, 60f);

            var minusButton = CreateButton(frame.transform, panelSprite, "MinusButton", "-", new Vector2(-150f, 0f), new Vector2(52f, 52f));
            var rateLabel = CreateText(frame.transform, "RateLabel", string.Empty, 24, Vector2.zero, new Vector2(220f, 52f));
            var plusButton = CreateButton(frame.transform, panelSprite, "PlusButton", "+", new Vector2(150f, 0f), new Vector2(52f, 52f));

            var go = new GameObject("TaxRateController");
            go.transform.SetParent(canvasParent, false);
            var controller = go.AddComponent<TaxRateController>();
            var so = new SerializedObject(controller);
            so.FindProperty("rateLabel").objectReferenceValue = rateLabel;
            so.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(minusButton.onClick, controller.Decrease);
            UnityEventTools.AddPersistentListener(plusButton.onClick, controller.Increase);
        }

        /// <summary>
        /// Left side, below the Меню button (y 465) and clear of the hotbar at the bottom --
        /// a standing panel (not a toast) since it's meant to be glanced at over time, unlike
        /// SettlementTierToast's momentary flash for the same kind of milestone. x is -760, not
        /// -810 -- see BuildTaxRateControl's comment on why (same off-screen-clipping fix).
        /// </summary>
        private static void BuildEventLog(Transform canvasParent, Sprite panelSprite)
        {
            var frame = CreateImage(canvasParent, "EventLog", new Color(0.16f, 0.18f, 0.15f, 0.85f));
            frame.sprite = panelSprite;
            frame.type = Image.Type.Sliced;
            var frameRect = frame.GetComponent<RectTransform>();
            frameRect.anchorMin = frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameRect.anchoredPosition = new Vector2(-760f, 100f);
            frameRect.sizeDelta = new Vector2(400f, 320f);

            var logText = CreateText(frame.transform, "LogText", string.Empty, 20, Vector2.zero, new Vector2(370f, 290f), new Color(1f, 1f, 1f, 0.85f));
            logText.alignment = TextAnchor.UpperLeft;
            logText.verticalOverflow = VerticalWrapMode.Truncate;

            var go = new GameObject("EventLogController");
            go.transform.SetParent(canvasParent, false);
            var controller = go.AddComponent<EventLogController>();
            var so = new SerializedObject(controller);
            so.FindProperty("logText").objectReferenceValue = logText;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Sits between the ResourceHUD (y 370) and the dead-center CitizenMoveFeedback text (y
        /// 0) so a tier-up toast never overlaps either -- hidden until SettlementTierManager
        /// actually reports one (see SettlementTierToastController).
        /// </summary>
        private static void BuildSettlementTierToast(Transform canvasParent, SettlementTierManager settlementTierManager)
        {
            var toastText = CreateText(canvasParent, "SettlementTierToast", string.Empty, 32, new Vector2(0f, 300f), new Vector2(1100f, 70f), addShadow: true);
            toastText.gameObject.SetActive(false);

            var go = new GameObject("SettlementTierToastController");
            go.transform.SetParent(canvasParent, false);
            var controller = go.AddComponent<SettlementTierToastController>();
            var so = new SerializedObject(controller);
            so.FindProperty("tierManager").objectReferenceValue = settlementTierManager;
            so.FindProperty("toastText").objectReferenceValue = toastText;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildResourceHUD(Transform canvasParent)
        {
            // Sits just below the top row (hint banner + Меню/Сохранить corners, all in the
            // y 420-510 band) in the same center-anchored coordinate space as those, so it
            // never overlaps them regardless of which of those happen to be visible. Laid out as
            // icon+number chips (see CreateResourceIcon) instead of one plain-text label so the
            // always-on bar reads at a glance rather than as a wall of Russian resource names.
            var root = new GameObject("ResourceHUD", typeof(RectTransform));
            root.transform.SetParent(canvasParent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 370f);

            const float barWidth = 1800f;
            rect.sizeDelta = new Vector2(barWidth, 50f);

            // Reading order: raw materials, then the ore and the metal it becomes side by side,
            // then money, then the food chain in the order it is actually made. A chip is only on
            // screen once its resource has existed -- ResourceHUDController lays the visible ones
            // out, which is why nothing here is positioned.
            var resourceTypes = new[]
            {
                ResourceType.Wood, ResourceType.Stone, ResourceType.Coal,
                ResourceType.Iron, ResourceType.IronBar,
                ResourceType.CopperOre, ResourceType.CopperBar,
                ResourceType.Gold, ResourceType.GoldBar,
                ResourceType.Coins,
                ResourceType.Grain, ResourceType.Flour, ResourceType.Bread, ResourceType.Food,
            };
            var icons = new RectTransform[resourceTypes.Length];
            var amountTexts = new Text[resourceTypes.Length];

            for (var i = 0; i < resourceTypes.Length; i++)
            {
                var icon = CreateImage(root.transform, $"Icon_{resourceTypes[i]}", Color.white);
                icon.sprite = CreateResourceIcon(resourceTypes[i]);
                var iconRect = icon.GetComponent<RectTransform>();
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.sizeDelta = new Vector2(32f, 32f);
                icons[i] = iconRect;

                var amountText = CreateText(root.transform, $"Amount_{resourceTypes[i]}", "0", 24,
                    Vector2.zero, new Vector2(160f, 50f));
                amountText.alignment = TextAnchor.MiddleLeft;
                amountTexts[i] = amountText;
            }

            var popIcon = CreateImage(root.transform, "Icon_Population", Color.white);
            popIcon.sprite = CreatePopulationIcon();
            var popIconRect = popIcon.GetComponent<RectTransform>();
            popIconRect.anchorMin = popIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            popIconRect.sizeDelta = new Vector2(32f, 32f);

            var populationText = CreateText(root.transform, "Amount_Population", "0", 24, Vector2.zero, new Vector2(160f, 50f));
            populationText.alignment = TextAnchor.MiddleLeft;

            var hud = root.AddComponent<ResourceHUDController>();
            var hudSO = new SerializedObject(hud);
            var resourceOrderProp = hudSO.FindProperty("resourceOrder");
            resourceOrderProp.arraySize = resourceTypes.Length;
            var iconsProp = hudSO.FindProperty("icons");
            iconsProp.arraySize = icons.Length;
            var amountTextsProp = hudSO.FindProperty("amountTexts");
            amountTextsProp.arraySize = amountTexts.Length;
            for (var i = 0; i < resourceTypes.Length; i++)
            {
                resourceOrderProp.GetArrayElementAtIndex(i).enumValueIndex = (int)resourceTypes[i];
                iconsProp.GetArrayElementAtIndex(i).objectReferenceValue = icons[i];
                amountTextsProp.GetArrayElementAtIndex(i).objectReferenceValue = amountTexts[i];
            }
            hudSO.FindProperty("populationIcon").objectReferenceValue = popIconRect;
            hudSO.FindProperty("populationText").objectReferenceValue = populationText;
            hudSO.FindProperty("barWidth").floatValue = barWidth;
            hudSO.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Top-right corner, stacked just below the Меню/Сохранить row (see BuildResourceHUD's
        /// comment for that shared y 420-510 band) so it never overlaps them. Square, not
        /// circular -- the game's no-circles cubic style applies to UI chrome too.
        /// </summary>
        private static void BuildMinimap(Transform canvasParent, Sprite panelSprite, RenderTexture minimapTexture, RTSCameraController cameraRig)
        {
            if (minimapTexture == null) return;

            const float size = 200f;
            const float border = 8f;

            var frame = CreateImage(canvasParent, "Minimap", new Color(0.26f, 0.29f, 0.24f, 0.95f));
            frame.sprite = panelSprite;
            frame.type = Image.Type.Sliced;
            var frameRect = frame.GetComponent<RectTransform>();
            frameRect.anchorMin = frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameRect.anchoredPosition = new Vector2(810f, 300f);
            frameRect.sizeDelta = new Vector2(size + border * 2f, size + border * 2f);

            var viewGO = new GameObject("MinimapView", typeof(RectTransform), typeof(RawImage));
            viewGO.transform.SetParent(frame.transform, false);
            var viewRect = viewGO.GetComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.offsetMin = new Vector2(border, border);
            viewRect.offsetMax = new Vector2(-border, -border);
            viewGO.GetComponent<RawImage>().texture = minimapTexture;

            // Diamond dot tracking the camera rig's ground position (see MinimapCameraIndicator)
            // -- so a player who's panned/zoomed off somewhere can tell where they're looking
            // from at a glance, instead of having to recognize terrain on the small live render.
            var markerGO = new GameObject("CameraMarker", typeof(RectTransform), typeof(Image));
            markerGO.transform.SetParent(viewGO.transform, false);
            var markerRect = markerGO.GetComponent<RectTransform>();
            markerRect.anchorMin = markerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerRect.pivot = new Vector2(0.5f, 0.5f);
            markerRect.sizeDelta = new Vector2(12f, 12f);
            markerRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            markerGO.GetComponent<Image>().color = new Color(1f, 0.92f, 0.3f);

            var indicator = viewGO.AddComponent<MinimapCameraIndicator>();
            var indicatorSO = new SerializedObject(indicator);
            indicatorSO.FindProperty("cameraRig").objectReferenceValue = cameraRig;
            indicatorSO.FindProperty("marker").objectReferenceValue = markerRect;
            // Same GridCellsX/CellSize world span the minimap camera (CreateMinimapCamera) is
            // framed to, expressed as UI units per world unit -- see MinimapCameraIndicator.
            indicatorSO.FindProperty("worldToMapScale").floatValue = size / (GridCellsX * CellSize);
            indicatorSO.FindProperty("mapHalfExtent").floatValue = size * 0.5f;
            indicatorSO.ApplyModifiedPropertiesWithoutUndo();
        }

        private static BuildingInfoPanelController BuildBuildingInfoPanel(Transform canvasParent, Sprite panelSprite, ResourceIconLibrary iconLibrary)
        {
            var panelRoot = new GameObject("BuildingInfoPanel", typeof(RectTransform));
            panelRoot.transform.SetParent(canvasParent, false);
            StretchFull(panelRoot.GetComponent<RectTransform>());

            var backdrop = CreateImage(panelRoot.transform, "Backdrop", new Color(0f, 0f, 0f, 0.7f));
            StretchFull(backdrop.GetComponent<RectTransform>());

            var card = CreateImage(panelRoot.transform, "Card", new Color(0.16f, 0.18f, 0.15f, 0.98f));
            card.sprite = panelSprite;
            card.type = Image.Type.Sliced;
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(700f, 780f);
            cardRect.anchoredPosition = Vector2.zero;

            // The header stack was pushed up to 330 to open the band at y 160 for the recipe
            // buttons; the blocks below the worker controls are untouched.
            var title = CreateText(card.transform, "Title", string.Empty, 34, new Vector2(0f, 330f), new Vector2(640f, 60f));
            var level = CreateText(card.transform, "Level", string.Empty, 24, new Vector2(0f, 280f), new Vector2(640f, 40f), new Color(1f, 1f, 1f, 0.85f));
            var condition = CreateText(card.transform, "Condition", string.Empty, 20, new Vector2(0f, 240f), new Vector2(640f, 40f), new Color(1f, 1f, 1f, 0.6f));

            // What the building makes, and out of what. Above the worker block because it is the
            // answer to "why would I staff this" -- and for a converter it is the only place the
            // conversion is written down at all.
            var production = CreateText(card.transform, "Production", string.Empty, 20, new Vector2(0f, 205f), new Vector2(640f, 30f), new Color(0.82f, 0.88f, 0.7f));

            // The metal buttons on a Плавильня, hidden for every building that has only one recipe.
            // The buttons themselves are built at play time by the controller: how many there are
            // is a property of the sheet, not of the scene.
            var recipeControls = new GameObject("RecipeControls", typeof(RectTransform));
            recipeControls.transform.SetParent(card.transform, false);
            StretchFull(recipeControls.GetComponent<RectTransform>());

            var recipeRow = new GameObject("RecipeRow", typeof(RectTransform));
            recipeRow.transform.SetParent(recipeControls.transform, false);
            var recipeRowRect = recipeRow.GetComponent<RectTransform>();
            recipeRowRect.anchorMin = recipeRowRect.anchorMax = new Vector2(0.5f, 0.5f);
            recipeRowRect.anchoredPosition = new Vector2(0f, 160f);
            recipeRowRect.sizeDelta = new Vector2(660f, 58f);

            // Passthrough containers (StretchFull, zero offset) purely to group-toggle visibility --
            // children keep the same card-relative positions they'd have without the wrapper.
            var workerControls = new GameObject("WorkerControls", typeof(RectTransform));
            workerControls.transform.SetParent(card.transform, false);
            StretchFull(workerControls.GetComponent<RectTransform>());

            var workers = CreateText(workerControls.transform, "Workers", string.Empty, 26, new Vector2(0f, 105f), new Vector2(640f, 50f), new Color(1f, 1f, 1f, 0.85f));
            var idle = CreateText(workerControls.transform, "Idle", string.Empty, 22, new Vector2(0f, 65f), new Vector2(640f, 40f), new Color(1f, 1f, 1f, 0.6f));
            var assignButton = CreateButton(workerControls.transform, panelSprite, "AssignButton", "#building_assign", new Vector2(-170f, 10f), new Vector2(300f, 80f));
            var unassignButton = CreateButton(workerControls.transform, panelSprite, "UnassignButton", "#building_unassign", new Vector2(170f, 10f), new Vector2(300f, 80f));

            var upgradeControls = new GameObject("UpgradeControls", typeof(RectTransform));
            upgradeControls.transform.SetParent(card.transform, false);
            StretchFull(upgradeControls.GetComponent<RectTransform>());

            var upgradeCostRow = CreateCostRow(upgradeControls.transform, "UpgradeCostRow", new Vector2(0f, -55f));
            var upgradeButton = CreateButton(upgradeControls.transform, panelSprite, "UpgradeButton", "#building_upgrade", new Vector2(0f, -125f), new Vector2(360f, 70f));

            // Stands in for the price and the button when the next level has not been researched --
            // in the same band, so the card does not change height depending on the answer.
            var upgradeLock = CreateText(upgradeControls.transform, "UpgradeLock", string.Empty, 22, new Vector2(0f, -90f), new Vector2(640f, 60f), new Color(1f, 0.78f, 0.5f));

            var repairControls = new GameObject("RepairControls", typeof(RectTransform));
            repairControls.transform.SetParent(card.transform, false);
            StretchFull(repairControls.GetComponent<RectTransform>());

            var repairCostRow = CreateCostRow(repairControls.transform, "RepairCostRow", new Vector2(0f, -195f));
            var repairButton = CreateButton(repairControls.transform, panelSprite, "RepairButton", "#building_repair", new Vector2(0f, -265f), new Vector2(360f, 70f));

            // Recruitment (Barracks only) reuses the worker block's slot on the card: the two are
            // mutually exclusive in practice -- the Barracks has no maxWorkers, so its worker
            // block never shows -- and this keeps the card from growing a fourth stacked section.
            var recruitControls = new GameObject("RecruitControls", typeof(RectTransform));
            recruitControls.transform.SetParent(card.transform, false);
            StretchFull(recruitControls.GetComponent<RectTransform>());

            // Which tier to raise. In the same band as the Плавильня's metal buttons (y 160) --
            // the two are the same question asked of two buildings, and the recipe block is hidden
            // for a Barracks anyway, so nothing overlaps. The buttons are built at play time by the
            // controller: how many tiers there are is a property of the sheet, not of the scene.
            var recruitTypeRow = new GameObject("RecruitTypeRow", typeof(RectTransform));
            recruitTypeRow.transform.SetParent(recruitControls.transform, false);
            var recruitTypeRowRect = recruitTypeRow.GetComponent<RectTransform>();
            recruitTypeRowRect.anchorMin = recruitTypeRowRect.anchorMax = new Vector2(0.5f, 0.5f);
            recruitTypeRowRect.anchoredPosition = new Vector2(0f, 160f);
            recruitTypeRowRect.sizeDelta = new Vector2(660f, 58f);

            var recruitLabel = CreateText(recruitControls.transform, "RecruitLabel", string.Empty, 22, new Vector2(0f, 105f), new Vector2(640f, 44f), new Color(1f, 1f, 1f, 0.8f));
            var recruitCostRow = CreateCostRow(recruitControls.transform, "RecruitCostRow", new Vector2(0f, 62f));
            var recruitButton = CreateButton(recruitControls.transform, panelSprite, "RecruitButton", "#building_recruit", new Vector2(0f, 10f), new Vector2(360f, 70f));

            // Demolition shares the bottom row with Close, and is deliberately the one button on
            // this card wearing a warning colour: it is the only irreversible thing here, and it
            // sits a finger's width from Upgrade.
            var demolishControls = new GameObject("DemolishControls", typeof(RectTransform));
            demolishControls.transform.SetParent(card.transform, false);
            StretchFull(demolishControls.GetComponent<RectTransform>());

            var demolishButton = CreateButton(demolishControls.transform, panelSprite, "DemolishButton", "#building_demolish", new Vector2(-170f, -345f), new Vector2(300f, 70f));
            demolishButton.GetComponent<Image>().color = new Color(0.45f, 0.2f, 0.18f, 0.95f);

            var closeButton = CreateButton(card.transform, panelSprite, "CloseButton", "#ui_close", new Vector2(170f, -345f), new Vector2(300f, 70f));

            // The confirmation, over the card rather than over the world -- the card is already
            // modal (ModalGate), so this only has to beat it in draw order.
            var demolishConfirm = new GameObject("DemolishConfirm", typeof(RectTransform));
            demolishConfirm.transform.SetParent(panelRoot.transform, false);
            StretchFull(demolishConfirm.GetComponent<RectTransform>());

            var demolishBackdrop = CreateImage(demolishConfirm.transform, "Backdrop", new Color(0f, 0f, 0f, 0.75f));
            StretchFull(demolishBackdrop.GetComponent<RectTransform>());

            var demolishCard = CreateImage(demolishConfirm.transform, "Card", new Color(0.18f, 0.14f, 0.13f, 0.98f));
            demolishCard.sprite = panelSprite;
            demolishCard.type = Image.Type.Sliced;
            var demolishCardRect = demolishCard.GetComponent<RectTransform>();
            demolishCardRect.anchorMin = demolishCardRect.anchorMax = new Vector2(0.5f, 0.5f);
            demolishCardRect.sizeDelta = new Vector2(760f, 320f);
            demolishCardRect.anchoredPosition = Vector2.zero;

            // Filled in at run time: it names the building and what the refund actually is.
            var demolishConfirmLabel = CreateText(demolishCard.transform, "Question", string.Empty, 28, new Vector2(0f, 60f), new Vector2(680f, 120f));

            var demolishConfirmButton = CreateButton(demolishCard.transform, panelSprite, "ConfirmDemolishButton", "#building_demolish", new Vector2(-160f, -80f), new Vector2(300f, 84f));
            demolishConfirmButton.GetComponent<Image>().color = new Color(0.45f, 0.2f, 0.18f, 0.95f);
            var demolishCancelButton = CreateButton(demolishCard.transform, panelSprite, "CancelDemolishButton", "#ui_cancel", new Vector2(160f, -80f), new Vector2(300f, 84f));

            demolishConfirm.SetActive(false);

            var controller = panelRoot.AddComponent<BuildingInfoPanelController>();
            var controllerSO = new SerializedObject(controller);
            controllerSO.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            controllerSO.FindProperty("titleLabel").objectReferenceValue = title;
            controllerSO.FindProperty("levelLabel").objectReferenceValue = level;
            controllerSO.FindProperty("conditionLabel").objectReferenceValue = condition;
            controllerSO.FindProperty("productionLabel").objectReferenceValue = production;
            controllerSO.FindProperty("demolishControls").objectReferenceValue = demolishControls;
            controllerSO.FindProperty("demolishConfirmRoot").objectReferenceValue = demolishConfirm;
            controllerSO.FindProperty("demolishConfirmLabel").objectReferenceValue = demolishConfirmLabel;
            controllerSO.FindProperty("recipeControls").objectReferenceValue = recipeControls;
            controllerSO.FindProperty("recipeRow").objectReferenceValue = recipeRowRect;
            controllerSO.FindProperty("recipeButtonSprite").objectReferenceValue = panelSprite;
            controllerSO.FindProperty("workerControls").objectReferenceValue = workerControls;
            controllerSO.FindProperty("workersLabel").objectReferenceValue = workers;
            controllerSO.FindProperty("idleLabel").objectReferenceValue = idle;
            controllerSO.FindProperty("upgradeControls").objectReferenceValue = upgradeControls;
            controllerSO.FindProperty("upgradeCostRow").objectReferenceValue = upgradeCostRow;
            controllerSO.FindProperty("upgradeButton").objectReferenceValue = upgradeButton;
            controllerSO.FindProperty("upgradeLockLabel").objectReferenceValue = upgradeLock;
            controllerSO.FindProperty("repairControls").objectReferenceValue = repairControls;
            controllerSO.FindProperty("repairCostRow").objectReferenceValue = repairCostRow;
            controllerSO.FindProperty("recruitControls").objectReferenceValue = recruitControls;
            controllerSO.FindProperty("recruitLabel").objectReferenceValue = recruitLabel;
            controllerSO.FindProperty("recruitCostRow").objectReferenceValue = recruitCostRow;
            controllerSO.FindProperty("recruitTypeRow").objectReferenceValue = recruitTypeRowRect;
            controllerSO.FindProperty("iconLibrary").objectReferenceValue = iconLibrary;
            controllerSO.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(assignButton.onClick, controller.AssignWorker);
            UnityEventTools.AddPersistentListener(unassignButton.onClick, controller.UnassignWorker);
            UnityEventTools.AddPersistentListener(upgradeButton.onClick, controller.Upgrade);
            UnityEventTools.AddPersistentListener(repairButton.onClick, controller.Repair);
            UnityEventTools.AddPersistentListener(demolishButton.onClick, controller.OpenDemolishConfirm);
            UnityEventTools.AddPersistentListener(demolishConfirmButton.onClick, controller.ConfirmDemolish);
            UnityEventTools.AddPersistentListener(demolishCancelButton.onClick, controller.CancelDemolish);
            UnityEventTools.AddPersistentListener(recruitButton.onClick, controller.Recruit);
            UnityEventTools.AddPersistentListener(closeButton.onClick, controller.Close);

            panelRoot.SetActive(false);
            return controller;
        }

        private static BuildingSelector BuildBuildingSelector(Transform canvasParent, Camera targetCamera,
            BuildingInfoPanelController infoPanel, ResearchPanelController researchPanel)
        {
            var go = new GameObject("BuildingSelector");
            go.transform.SetParent(canvasParent, false);
            var selector = go.AddComponent<BuildingSelector>();
            var so = new SerializedObject(selector);
            so.FindProperty("targetCamera").objectReferenceValue = targetCamera;
            so.FindProperty("infoPanel").objectReferenceValue = infoPanel;
            so.FindProperty("researchPanel").objectReferenceValue = researchPanel;
            so.ApplyModifiedPropertiesWithoutUndo();
            return selector;
        }

        /// <summary>
        /// The Laboratory's own window, opened by tapping the Laboratory INSTEAD of the ordinary
        /// building card -- which is why its header carries the scientists, the upgrade and the
        /// repair: nothing else would reach them (see ResearchPanelController).
        ///
        /// Wider and taller than the other cards (1240x980 against the workforce panel's 960x780)
        /// because a row here holds a title, a price and a button, and there are two tabs of them.
        /// 980 tall spans y -490..490 on a 1080-high canvas, which clears the edge.
        /// </summary>
        private static ResearchPanelController BuildResearchPanel(Transform canvasParent, Sprite panelSprite, ResourceIconLibrary iconLibrary)
        {
            var panelRoot = new GameObject("ResearchPanel", typeof(RectTransform));
            panelRoot.transform.SetParent(canvasParent, false);
            StretchFull(panelRoot.GetComponent<RectTransform>());

            var backdrop = CreateImage(panelRoot.transform, "Backdrop", new Color(0f, 0f, 0f, 0.7f));
            StretchFull(backdrop.GetComponent<RectTransform>());

            var card = CreateImage(panelRoot.transform, "Card", new Color(0.16f, 0.18f, 0.15f, 0.98f));
            card.sprite = panelSprite;
            card.type = Image.Type.Sliced;
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(1240f, 980f);
            cardRect.anchoredPosition = Vector2.zero;

            var title = CreateText(card.transform, "Title", string.Empty, 34, new Vector2(0f, 430f), new Vector2(1160f, 60f));
            var condition = CreateText(card.transform, "Condition", string.Empty, 20, new Vector2(0f, 390f), new Vector2(1160f, 36f), new Color(1f, 1f, 1f, 0.6f));

            // Header band: the scientists on the left, the building's own two actions on the right.
            var scientists = CreateText(card.transform, "Scientists", string.Empty, 24, new Vector2(-330f, 335f), new Vector2(500f, 44f), new Color(1f, 1f, 1f, 0.9f), addShadow: false);
            scientists.alignment = TextAnchor.MiddleLeft;
            var removeScientist = CreateButton(card.transform, panelSprite, "RemoveScientistButton", "-", new Vector2(-120f, 335f), new Vector2(64f, 64f));
            var addScientist = CreateButton(card.transform, panelSprite, "AddScientistButton", "+", new Vector2(-40f, 335f), new Vector2(64f, 64f));

            var upgradeControls = new GameObject("UpgradeControls", typeof(RectTransform));
            upgradeControls.transform.SetParent(card.transform, false);
            StretchFull(upgradeControls.GetComponent<RectTransform>());
            var upgradeButton = CreateButton(upgradeControls.transform, panelSprite, "UpgradeButton", "#building_upgrade", new Vector2(200f, 345f), new Vector2(260f, 64f));
            var upgradeCostRow = CreateCostRow(upgradeControls.transform, "UpgradeCostRow", new Vector2(200f, 297f));

            var repairControls = new GameObject("RepairControls", typeof(RectTransform));
            repairControls.transform.SetParent(card.transform, false);
            StretchFull(repairControls.GetComponent<RectTransform>());
            var repairButton = CreateButton(repairControls.transform, panelSprite, "RepairButton", "#building_repair", new Vector2(490f, 345f), new Vector2(260f, 64f));
            var repairCostRow = CreateCostRow(repairControls.transform, "RepairCostRow", new Vector2(490f, 297f));

            // What is being researched right now, and the way out of it.
            var progress = CreateText(card.transform, "Progress", string.Empty, 22, new Vector2(-160f, 240f), new Vector2(840f, 40f), new Color(0.82f, 0.88f, 0.7f));
            progress.alignment = TextAnchor.MiddleLeft;

            var cancelControls = new GameObject("CancelControls", typeof(RectTransform));
            cancelControls.transform.SetParent(card.transform, false);
            StretchFull(cancelControls.GetComponent<RectTransform>());
            var cancelButton = CreateDynamicButton(cancelControls.transform, panelSprite, "CancelResearchButton", new Vector2(400f, 240f), new Vector2(420f, 60f), out var cancelLabel);

            var buildingsTab = CreateButton(card.transform, panelSprite, "BuildingsTab", "#research_tab_buildings", new Vector2(-200f, 178f), new Vector2(380f, 60f));
            var unitsTab = CreateButton(card.transform, panelSprite, "UnitsTab", "#research_tab_units", new Vector2(200f, 178f), new Vector2(380f, 60f));

            BuildScrollList(card.transform, new Vector2(0f, -130f), new Vector2(1180f, 560f),
                "#research_empty", out var content, out var emptyLabelGO);

            var closeButton = CreateButton(card.transform, panelSprite, "CloseButton", "#ui_close", new Vector2(0f, -450f), new Vector2(300f, 70f));

            var controller = panelRoot.AddComponent<ResearchPanelController>();
            var so = new SerializedObject(controller);
            so.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            so.FindProperty("titleLabel").objectReferenceValue = title;
            so.FindProperty("conditionLabel").objectReferenceValue = condition;
            so.FindProperty("scientistsLabel").objectReferenceValue = scientists;
            so.FindProperty("addScientistButton").objectReferenceValue = addScientist;
            so.FindProperty("removeScientistButton").objectReferenceValue = removeScientist;
            so.FindProperty("upgradeControls").objectReferenceValue = upgradeControls;
            so.FindProperty("upgradeCostRow").objectReferenceValue = upgradeCostRow;
            so.FindProperty("repairControls").objectReferenceValue = repairControls;
            so.FindProperty("repairCostRow").objectReferenceValue = repairCostRow;
            so.FindProperty("progressLabel").objectReferenceValue = progress;
            so.FindProperty("cancelControls").objectReferenceValue = cancelControls;
            so.FindProperty("cancelLabel").objectReferenceValue = cancelLabel;
            so.FindProperty("buildingsTabBackground").objectReferenceValue = buildingsTab.GetComponent<Image>();
            so.FindProperty("unitsTabBackground").objectReferenceValue = unitsTab.GetComponent<Image>();
            so.FindProperty("listContent").objectReferenceValue = content;
            so.FindProperty("emptyLabel").objectReferenceValue = emptyLabelGO;
            so.FindProperty("rowSprite").objectReferenceValue = panelSprite;
            so.FindProperty("completedIcon").objectReferenceValue = CreateCheckIcon();
            so.FindProperty("iconLibrary").objectReferenceValue = iconLibrary;
            so.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(addScientist.onClick, controller.AddScientist);
            UnityEventTools.AddPersistentListener(removeScientist.onClick, controller.RemoveScientist);
            UnityEventTools.AddPersistentListener(upgradeButton.onClick, controller.UpgradeLab);
            UnityEventTools.AddPersistentListener(repairButton.onClick, controller.RepairLab);
            UnityEventTools.AddPersistentListener(cancelButton.onClick, controller.CancelResearch);
            UnityEventTools.AddPersistentListener(buildingsTab.onClick, controller.SelectBuildingsTab);
            UnityEventTools.AddPersistentListener(unitsTab.onClick, controller.SelectUnitsTab);
            UnityEventTools.AddPersistentListener(closeButton.onClick, controller.Close);

            panelRoot.SetActive(false);
            return controller;
        }

        /// <summary>The tick on a researched row. Drawn rather than typed: the built-in font has no check-mark glyph, and a missing one renders as an empty box.</summary>
        private static Sprite CreateCheckIcon()
        {
            return CreateIconSprite("Action_Check", 64, (p, s) =>
            {
                var stroke = new Color(0.95f, 0.98f, 0.95f);
                FillIconStroke(p, s, new Vector2(0.16f, 0.52f), new Vector2(0.4f, 0.26f), 0.11f, stroke);
                FillIconStroke(p, s, new Vector2(0.4f, 0.26f), new Vector2(0.86f, 0.76f), 0.11f, stroke);
            });
        }

        /// <summary>A thick line between two normalized points, as two triangles -- the icon painters only rasterize rects and triangles.</summary>
        private static void FillIconStroke(Color[] pixels, int size, Vector2 from, Vector2 to, float thickness, Color color)
        {
            var direction = (to - from).normalized;
            var offset = new Vector2(-direction.y, direction.x) * (thickness * 0.5f);

            var a = from + offset;
            var b = to + offset;
            var c = to - offset;
            var d = from - offset;
            FillIconTriangle(pixels, size, a, b, c, color);
            FillIconTriangle(pixels, size, a, c, d, color);
        }

        /// <summary>
        /// Click-to-select-a-citizen-then-click-to-move-it (see CitizenSelector). The feedback
        /// label is dead center, large and shadowed like the main menu title so the momentary
        /// OK!/NO! flash reads clearly regardless of what's behind it; hidden until the first
        /// destination click.
        /// </summary>
        private static CitizenSelector BuildCitizenSelector(Transform canvasParent, Camera targetCamera)
        {
            var feedbackText = CreateText(canvasParent, "CitizenMoveFeedback", string.Empty, 64, Vector2.zero, new Vector2(400f, 100f), addShadow: true);
            feedbackText.gameObject.SetActive(false);

            var go = new GameObject("CitizenSelector");
            go.transform.SetParent(canvasParent, false);
            var selector = go.AddComponent<CitizenSelector>();
            var so = new SerializedObject(selector);
            so.FindProperty("targetCamera").objectReferenceValue = targetCamera;
            so.FindProperty("feedbackText").objectReferenceValue = feedbackText;
            so.ApplyModifiedPropertiesWithoutUndo();
            return selector;
        }

        /// <summary>
        /// The crosshair sitting at the placement aim point: a hollow square of four thin bars,
        /// which reads as "this cell" without covering the ghost standing in it. Positioned in
        /// screen pixels every frame by PlacementHudController.
        /// </summary>
        private static RectTransform CreateAimMarker(Transform canvasParent)
        {
            var root = new GameObject("PlacementAimMarker", typeof(RectTransform));
            root.transform.SetParent(canvasParent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(96f, 96f);

            var color = new Color(1f, 1f, 1f, 0.85f);
            const float length = 34f;
            const float thickness = 4f;

            CreateAimBar(root.transform, "Top", new Vector2(0f, 48f), new Vector2(length, thickness), color);
            CreateAimBar(root.transform, "Bottom", new Vector2(0f, -48f), new Vector2(length, thickness), color);
            CreateAimBar(root.transform, "Left", new Vector2(-48f, 0f), new Vector2(thickness, length), color);
            CreateAimBar(root.transform, "Right", new Vector2(48f, 0f), new Vector2(thickness, length), color);

            root.SetActive(false);
            return rect;
        }

        private static void CreateAimBar(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color color)
        {
            var bar = CreateImage(parent, name, color);
            var rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
        }

        /// <summary>
        /// The badge naming whatever is selected, with the X that clears it. Top-centre, clear of
        /// the minimap on the right and the event log on the left.
        ///
        /// It exists because selection had no visible exit on a phone at all: a selected citizen
        /// could only be released with Escape or a right click, and while one is selected every
        /// world tap is an order to them -- so the player was stuck in a mode they could not see
        /// and could not leave.
        /// </summary>
        private static void BuildSelectionBadge(Transform canvasParent, Sprite panelSprite, CitizenSelector citizenSelector)
        {
            var badge = CreateImage(canvasParent, "SelectionBadge", new Color(0.16f, 0.18f, 0.15f, 0.92f));
            badge.sprite = panelSprite;
            badge.type = Image.Type.Sliced;
            var badgeRect = badge.GetComponent<RectTransform>();
            badgeRect.anchorMin = badgeRect.anchorMax = badgeRect.pivot = new Vector2(0.5f, 1f);
            badgeRect.anchoredPosition = new Vector2(0f, -30f);
            badgeRect.sizeDelta = new Vector2(460f, 96f);

            var label = CreateText(badge.transform, "Label", string.Empty, 30, new Vector2(-40f, 0f), new Vector2(300f, 60f));

            var cancelIcon = CreateCancelIcon();
            var clearButton = CreateIconButton(badge.transform, panelSprite, cancelIcon, "ClearSelectionButton", new Vector2(170f, 0f), new Vector2(76f, 76f));

            // On the Canvas, not on the badge: a component that deactivates its own GameObject
            // stops getting Update() and could never switch itself back on.
            var controller = canvasParent.gameObject.AddComponent<SelectionBadgeController>();
            var controllerSO = new SerializedObject(controller);
            controllerSO.FindProperty("citizenSelector").objectReferenceValue = citizenSelector;
            controllerSO.FindProperty("badgeRoot").objectReferenceValue = badge.gameObject;
            controllerSO.FindProperty("label").objectReferenceValue = label;
            controllerSO.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(clearButton.onClick, controller.ClearSelection);

            badge.gameObject.SetActive(false);
        }

        private static void BuildExitUI(Transform canvasParent, Sprite panelSprite)
        {
            var menuButton = CreateButton(canvasParent, panelSprite, "MenuButton", "#hud_menu", new Vector2(-810f, 465f), new Vector2(240f, 90f));

            var dialogRoot = new GameObject("ExitDialog", typeof(RectTransform));
            dialogRoot.transform.SetParent(canvasParent, false);
            StretchFull(dialogRoot.GetComponent<RectTransform>());

            var backdrop = CreateImage(dialogRoot.transform, "Backdrop", new Color(0f, 0f, 0f, 0.7f));
            StretchFull(backdrop.GetComponent<RectTransform>());

            var card = CreateImage(dialogRoot.transform, "Card", new Color(0.16f, 0.18f, 0.15f, 0.98f));
            card.sprite = panelSprite;
            card.type = Image.Type.Sliced;
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(760f, 380f);
            cardRect.anchoredPosition = Vector2.zero;

            Localize(CreateText(card.transform, "Title", string.Empty, 36, new Vector2(0f, 120f), new Vector2(680f, 70f)), "#exit_title");
            Localize(CreateText(card.transform, "Warning", string.Empty, 24, new Vector2(0f, 40f), new Vector2(680f, 60f), new Color(1f, 0.7f, 0.6f, 1f)), "#exit_warning");

            var confirmButton = CreateButton(card.transform, panelSprite, "ConfirmExitButton", "#exit_confirm", new Vector2(-160f, -90f), new Vector2(300f, 90f));
            var cancelButton = CreateButton(card.transform, panelSprite, "CancelExitButton", "#ui_cancel", new Vector2(160f, -90f), new Vector2(300f, 90f));

            var dialogController = dialogRoot.AddComponent<ExitToMenuController>();
            var dialogSO = new SerializedObject(dialogController);
            dialogSO.FindProperty("dialogRoot").objectReferenceValue = dialogRoot;
            dialogSO.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(menuButton.onClick, dialogController.OpenDialog);
            UnityEventTools.AddPersistentListener(confirmButton.onClick, dialogController.ConfirmExit);
            UnityEventTools.AddPersistentListener(cancelButton.onClick, dialogController.CloseDialog);

            dialogRoot.SetActive(false);
        }

        /// <summary>Full-screen defeat panel, same backdrop+card pattern as the dialog above -- shown by GameOverController once GameOverManager reports the Town Hall destroyed. Only action is back to the menu; there's nothing left to confirm/cancel.</summary>
        private static void BuildGameOverUI(Transform canvasParent, Sprite panelSprite)
        {
            var panelRoot = new GameObject("GameOverPanel", typeof(RectTransform));
            panelRoot.transform.SetParent(canvasParent, false);
            StretchFull(panelRoot.GetComponent<RectTransform>());

            var backdrop = CreateImage(panelRoot.transform, "Backdrop", new Color(0f, 0f, 0f, 0.8f));
            StretchFull(backdrop.GetComponent<RectTransform>());

            var card = CreateImage(panelRoot.transform, "Card", new Color(0.16f, 0.18f, 0.15f, 0.98f));
            card.sprite = panelSprite;
            card.type = Image.Type.Sliced;
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(760f, 380f);
            cardRect.anchoredPosition = Vector2.zero;

            // Text and colour are set per outcome by GameOverController -- one panel serves both
            // the defeat and the "all portals closed" victory.
            var title = CreateText(card.transform, "Title", string.Empty, 44, new Vector2(0f, 100f), new Vector2(680f, 70f), new Color(0.9f, 0.35f, 0.3f));
            var reason = CreateText(card.transform, "Reason", string.Empty, 26, new Vector2(0f, 30f), new Vector2(680f, 60f), new Color(1f, 1f, 1f, 0.8f));

            var menuButton = CreateButton(card.transform, panelSprite, "GameOverMenuButton", "#over_to_menu", new Vector2(0f, -100f), new Vector2(360f, 90f));

            // The controller lives on its own always-active object, NOT on the panel it shows.
            // A component on a deactivated GameObject never gets Start(), so putting it on
            // panelRoot (deactivated just below) meant it never subscribed to
            // GameOverManager.OnGameOver -- the Town Hall could be destroyed and the defeat
            // screen would simply never appear. Same reasoning as PlacementHudController.
            var controllerGO = new GameObject("GameOverController");
            controllerGO.transform.SetParent(canvasParent, false);
            var controller = controllerGO.AddComponent<GameOverController>();
            var controllerSO = new SerializedObject(controller);
            controllerSO.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            controllerSO.FindProperty("titleLabel").objectReferenceValue = title;
            controllerSO.FindProperty("reasonLabel").objectReferenceValue = reason;
            controllerSO.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(menuButton.onClick, controller.ReturnToMenu);

            panelRoot.SetActive(false);
        }

        private static void BuildSaveUI(Transform canvasParent, Sprite panelSprite, GameSaveController saveController)
        {
            // Manual save only, per design: the player decides if/when to save, so this button
            // is the only way a save ever happens — there is no autosave to fall back on.
            var saveButton = CreateButton(canvasParent, panelSprite, "SaveButton", "#hud_save", new Vector2(810f, 465f), new Vector2(240f, 90f));

            var dialogRoot = new GameObject("SaveDialog", typeof(RectTransform));
            dialogRoot.transform.SetParent(canvasParent, false);
            StretchFull(dialogRoot.GetComponent<RectTransform>());

            var backdrop = CreateImage(dialogRoot.transform, "Backdrop", new Color(0f, 0f, 0f, 0.7f));
            StretchFull(backdrop.GetComponent<RectTransform>());

            var card = CreateImage(dialogRoot.transform, "Card", new Color(0.16f, 0.18f, 0.15f, 0.98f));
            card.sprite = panelSprite;
            card.type = Image.Type.Sliced;
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(760f, 420f);
            cardRect.anchoredPosition = Vector2.zero;

            Localize(CreateText(card.transform, "Title", string.Empty, 40, new Vector2(0f, 150f), new Vector2(680f, 70f)), "#save_title");

            var nameInput = CreateInputField(card.transform, "NameInput", "MyCityName", new Vector2(0f, 30f), new Vector2(600f, 90f), panelSprite);

            var confirmButton = CreateButton(card.transform, panelSprite, "ConfirmSaveButton", "#save_confirm", new Vector2(-160f, -100f), new Vector2(300f, 90f));
            var cancelButton = CreateButton(card.transform, panelSprite, "CancelSaveButton", "#ui_cancel", new Vector2(160f, -100f), new Vector2(300f, 90f));

            var dialogController = dialogRoot.AddComponent<SaveDialogController>();
            var dialogSO = new SerializedObject(dialogController);
            dialogSO.FindProperty("saveController").objectReferenceValue = saveController;
            dialogSO.FindProperty("dialogRoot").objectReferenceValue = dialogRoot;
            dialogSO.FindProperty("nameInput").objectReferenceValue = nameInput;
            dialogSO.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(saveButton.onClick, dialogController.OpenDialog);
            UnityEventTools.AddPersistentListener(confirmButton.onClick, dialogController.ConfirmSave);
            UnityEventTools.AddPersistentListener(cancelButton.onClick, dialogController.CloseDialog);

            dialogRoot.SetActive(false);
        }

        private static void BuildLoadPanel(Transform canvasParent, Sprite panelSprite, MainMenuController menuController)
        {
            var panelRoot = new GameObject("LoadPanel", typeof(RectTransform));
            panelRoot.transform.SetParent(canvasParent, false);
            StretchFull(panelRoot.GetComponent<RectTransform>());

            var backdrop = CreateImage(panelRoot.transform, "Backdrop", new Color(0f, 0f, 0f, 0.7f));
            StretchFull(backdrop.GetComponent<RectTransform>());

            var card = CreateImage(panelRoot.transform, "Card", new Color(0.16f, 0.18f, 0.15f, 0.98f));
            card.sprite = panelSprite;
            card.type = Image.Type.Sliced;
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(1000f, 760f);
            cardRect.anchoredPosition = Vector2.zero;

            Localize(CreateText(card.transform, "Title", string.Empty, 48, new Vector2(0f, 320f), new Vector2(900f, 80f)), "#load_title");

            BuildScrollList(card.transform, new Vector2(0f, 60f), new Vector2(900f, 420f), "#load_empty", out var content, out var emptyLabelGO);

            var selectedLabel = CreateText(card.transform, "SelectedLabel", string.Empty, 26, new Vector2(0f, -170f), new Vector2(900f, 50f), new Color(1f, 1f, 1f, 0.8f));

            var loadButton = CreateButton(card.transform, panelSprite, "LoadSelectedButton", "#load_confirm", new Vector2(-190f, -270f), new Vector2(360f, 90f));
            var backButton = CreateButton(card.transform, panelSprite, "BackButton", "#ui_back", new Vector2(190f, -270f), new Vector2(360f, 90f));

            var panelController = panelRoot.AddComponent<LoadGamePanelController>();
            var panelSO = new SerializedObject(panelController);
            panelSO.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            panelSO.FindProperty("listContent").objectReferenceValue = content;
            panelSO.FindProperty("emptyLabel").objectReferenceValue = emptyLabelGO;
            panelSO.FindProperty("loadSelectedButton").objectReferenceValue = loadButton;
            panelSO.FindProperty("selectedNameLabel").objectReferenceValue = selectedLabel;
            panelSO.FindProperty("rowSprite").objectReferenceValue = panelSprite;
            panelSO.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(loadButton.onClick, panelController.ConfirmLoad);
            UnityEventTools.AddPersistentListener(backButton.onClick, panelController.ClosePanel);

            panelRoot.SetActive(false);

            var menuSO = new SerializedObject(menuController);
            menuSO.FindProperty("loadGamePanel").objectReferenceValue = panelController;
            menuSO.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>A scrollable, vertically stacked list whose rows are built at runtime -- the save browser's slots, the workforce panel's workplaces.</summary>
        private static void BuildScrollList(Transform parent, Vector2 position, Vector2 size, string emptyKey, out RectTransform content, out GameObject emptyLabelGO)
        {
            var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollGO.transform.SetParent(parent, false);
            var scrollRectTransform = scrollGO.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = scrollRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchoredPosition = position;
            scrollRectTransform.sizeDelta = size;

            var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var viewportRect = viewportGO.GetComponent<RectTransform>();
            StretchFull(viewportRect);
            viewportGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);

            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRect = contentGO.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            var layoutGroup = contentGO.GetComponent<VerticalLayoutGroup>();
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 12f;
            layoutGroup.padding = new RectOffset(8, 8, 8, 8);

            var sizeFitter = contentGO.GetComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = scrollGO.GetComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            var emptyLabel = Localize(CreateText(viewportGO.transform, "EmptyLabel", string.Empty, 28, Vector2.zero, new Vector2(700f, 80f), new Color(1f, 1f, 1f, 0.6f)), emptyKey);

            content = contentRect;
            emptyLabelGO = emptyLabel.gameObject;
        }

        /// <summary>
        /// Architectural archetype driving which procedural generator builds a prefab. What makes
        /// one building look like ITSELF on top of the shell is its BuildingMotif -- see
        /// SetupProjectMotifs.cs.
        ///
        /// `Plot` is the shell for everything that is not really a building: a well, a flag, a
        /// garden, a gate you walk through, an arena with an open floor. It lays a paved base the
        /// size of the footprint and leaves the rest to the motif.
        /// </summary>
        private enum BuildingStyle { Hut, Fortification, Tower, Road, Plot }

        private static BuildingData CreateBuildingData(
            string id, Vector2Int footprint, float height, Color wallColor, Color roofColor,
            BuildingStyle style, Material trimMaterial, Material windowMaterial, Material doorMaterial = null,
            bool hasChimney = false,
            bool isRoad = false, bool keepSelectedAfterPlacement = false, bool isWaterCategory = false,
            bool providesWalkableSurface = false, bool connectsToFences = false,
            BuildingMotif motif = BuildingMotif.None)
        {
            // Two of the sheet's numbers shape the prefab rather than just sitting on the data asset,
            // so the row has to be read before it's built: worker slots become geometry, and a real
            // defence stat decides whether the building shoots back at all.
            var balance = Balance(id);

            // An authored model wins over anything generated here: the moment `<id>1-lvl1.fbx`
            // exists, the placeholder below stops being built and the model's own size becomes the
            // building's footprint. That is the whole loop for adding art -- drop the file in, run
            // the build, see it in the game. No code change, which matters when there are ~48
            // buildings to get through and most of them will arrive as rough stand-ins first. The
            // footprint passed in is what the placeholder falls back to until then.
            GameObject prefab;
            var authoredModel = AuthoredModel(id);
            if (authoredModel != null)
            {
                prefab = CreateAuthoredModelPrefab(id, height, authoredModel, balance, out footprint);
            }
            else switch (style)
            {
                case BuildingStyle.Hut:
                    // Level 1's worker count: the hut's geometry is built once, and its level-2
                    // model is a separate FBX rather than a re-generated mesh.
                    prefab = CreateHutPrefab(id, footprint, height, wallColor, roofColor, LevelOne(balance).maxWorkers, hasChimney, trimMaterial, doorMaterial, windowMaterial, motif);
                    break;
                case BuildingStyle.Fortification:
                    prefab = CreateFortificationPrefab(id, footprint, height, wallColor, roofColor, trimMaterial, isTower: false, motif: motif);
                    break;
                case BuildingStyle.Tower:
                    prefab = CreateFortificationPrefab(id, footprint, height, wallColor, roofColor, trimMaterial, isTower: true, motif: motif);
                    break;
                case BuildingStyle.Road:
                    prefab = CreateRoadPrefab(id, wallColor, roofColor);
                    break;
                case BuildingStyle.Plot:
                    prefab = CreatePlotPrefab(id, footprint, height, wallColor, roofColor, LevelOne(balance).maxWorkers, trimMaterial, motif);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(style), style, null);
            }

            // Any building with a real defense stat (Wall/Tower/Barracks/Gate) fights back against
            // OrcUnit raiders on its own -- see DefensiveBuilding. Attached here (once, at prefab
            // creation) rather than conditionally at placement time, same as ProductionBuilding's
            // maxWorkers check in CreateHutPrefab.
            // Attached if the building EVER fights back, not just at level 1 -- a structure that
            // only gains defence when upgraded still needs the component from the start, since
            // upgrading swaps stats and models but never rebuilds the prefab.
            if (FightsBackAtAnyLevel(balance)) prefab.AddComponent<DefensiveBuilding>();

            var data = ScriptableObject.CreateInstance<BuildingData>();
            data.buildingName = id;
            data.prefab = prefab;
            data.footprintSize = footprint;
            ApplyBalance(data, balance);
            data.isRoad = isRoad;
            data.providesWalkableSurface = providesWalkableSurface;
            data.keepSelectedAfterPlacement = keepSelectedAfterPlacement;
            data.isWaterCategory = isWaterCategory;
            data.connectsToFences = connectsToFences;

            Directory.CreateDirectory(BuildingDataFolder);
            var dataPath = $"{BuildingDataFolder}/{id}.asset";
            DeleteIfExists(dataPath);
            AssetDatabase.CreateAsset(data, dataPath);
            return data;
        }

        /// <summary>
        /// The authored model for one of a building's levels, found by name rather than wired up in
        /// code: `<id>1-lvl<level>.fbx` in Models/Buildings, matching the naming the models
        /// themselves use (the digit is the building variant, lvl1 its upgrade level). Null while
        /// nobody has drawn it -- at level 1 the procedural placeholder stands in, above it the
        /// level below keeps being shown (see AddLevelModels).
        /// </summary>
        private static GameObject AuthoredModel(string id, int level = 1)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsBuildingsFolder}/{id}1-lvl{level}.fbx");
        }

        /// <summary>
        /// A building whose look comes entirely from an FBX. Everything else about it -- its stats,
        /// its components -- is unchanged from the generated version, so swapping the file in
        /// changes the picture and nothing else.
        ///
        /// Both of the numbers that used to be typed in by hand are MEASURED here instead. The
        /// height, because it is what the player's clicks meet: a hand-written height that
        /// disagrees with the model gives either a roof that cannot be clicked or an invisible box
        /// beside the building that eats clicks meant for the ground, and stand-in models are
        /// deliberately not the final height. The footprint, because the models carry the sizes the
        /// design wants -- a 2x2 model IS a 2x2 building -- and the alternative is two sources of
        /// truth that drift apart every time something is re-exported.
        /// </summary>
        private static GameObject CreateAuthoredModelPrefab(string id, float fallbackHeight, GameObject source, BuildingBalance balance, out Vector2Int footprint)
        {
            var root = new GameObject(id);
            root.AddComponent<BuildingInstance>();
            // Same rule as DefensiveBuilding below: attached if the building EVER employs anyone,
            // since upgrading swaps stats and models but never rebuilds the prefab.
            if (EmploysAtAnyLevel(balance)) root.AddComponent<ProductionBuilding>();

            var model = Object.Instantiate(source, Vector3.zero, source.transform.rotation, root.transform);
            var size = FitModelToGround(model, id);

            footprint = FootprintOf(size, id);
            var height = size.y > 0.01f ? size.y : fallbackHeight;

            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(footprint.x * CellSize - BuildingInset, height, footprint.y * CellSize - BuildingInset);
            collider.center = new Vector3(0f, height * 0.5f, 0f);

            AddLevelModels(root, id, model, height, footprint);

            return SavePrefab(root, id);
        }

        /// <summary>
        /// Hangs the models for levels 2 and 3 in the same prefab, where the artist has drawn them:
        /// `<id>1-lvl2.fbx`, `<id>1-lvl3.fbx`, found by name exactly the way level 1 is. Upgrading
        /// then swaps which one is active (see BuildingLevelAppearance) instead of rebuilding the
        /// building, so its workers, damage, storage and NavMesh hole all survive the upgrade.
        ///
        /// Two rules worth keeping in mind before exporting a level-2 model:
        ///
        /// The FOOTPRINT stays level 1's, whatever the later models measure. A level-2 model a cell
        /// wider would otherwise have to fit into ground the player has already built over -- an
        /// upgrade that fails on geometry at the exact moment it has been paid for. A wider model is
        /// warned about here and left to overhang, the same tolerance a too-wide level-1 model gets.
        ///
        /// The HEIGHT does follow the level, and carries the click box with it, so a tower that
        /// doubles in height at level 3 is still clickable at the top.
        ///
        /// Nothing is attached at all when only level 1 exists, which is every building today: the
        /// component would resolve every level back to the one model and cost a component on 45
        /// prefabs to do it. The day an FBX lands, this build wires it up.
        /// </summary>
        private static void AddLevelModels(GameObject root, string id, GameObject levelOneModel, float levelOneHeight, Vector2Int footprint)
        {
            var models = new List<GameObject> { levelOneModel };
            var heights = new List<float> { levelOneHeight };
            var anyDrawn = false;

            for (var level = 2; level <= BuildingInstance.MaxLevel; level++)
            {
                var source = AuthoredModel(id, level);
                if (source == null)
                {
                    // A hole, not a shortened list: the levels have to stay at their own index, or
                    // a building with only levels 1 and 3 drawn would show level 3's model at 2.
                    models.Add(null);
                    heights.Add(0f);
                    continue;
                }

                anyDrawn = true;
                var model = Object.Instantiate(source, Vector3.zero, source.transform.rotation, root.transform);
                model.name = $"{id}-lvl{level}";
                var size = FitModelToGround(model, $"{id} lvl{level}");
                var levelFootprint = FootprintOf(size, $"{id} lvl{level}");
                if (levelFootprint != footprint)
                {
                    Debug.LogWarning($"[SetupProject] '{id}' level {level} measures {levelFootprint.x}x{levelFootprint.y} cells against level 1's {footprint.x}x{footprint.y}. The building keeps level 1's footprint, so this model will overhang its neighbours instead of claiming more ground.");
                }

                // Level 1 is what a newly placed building shows; the rest wait to be upgraded into.
                model.SetActive(false);
                models.Add(model);
                heights.Add(size.y);
            }

            if (!anyDrawn) return;
            root.AddComponent<BuildingLevelAppearance>().SetModels(models, heights);
        }

        /// <summary>
        /// Centres a model on its own origin, stands it on the ground, and reports how big it turned
        /// out to be.
        ///
        /// Measuring instead of trusting the FBX's origin is what makes a model droppable: these
        /// files come out of Blender with the geometry wherever the scene happened to leave it (the
        /// fence pair were exported side by side with a zero translation and would otherwise have
        /// stood metres from their cell). Only the horizontal centre and the base are corrected --
        /// nothing is ever scaled.
        /// </summary>
        private static Vector3 FitModelToGround(GameObject model, string id)
        {
            var renderers = model.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0)
            {
                Debug.LogError($"FitModelToGround: '{id}' has an authored model with no MeshRenderer -- nothing to place or measure.");
                return Vector3.zero;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            model.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            return bounds.size;
        }

        /// <summary>
        /// How many cells a model claims: its own size in metres, rounded to the nearest whole cell
        /// and never less than one. Rounding rather than ceiling on purpose -- a roof overhanging its
        /// walls by a few centimetres is a roof, not another cell of ground.
        /// </summary>
        private static Vector2Int FootprintOf(Vector3 size, string id)
        {
            var footprint = new Vector2Int(
                Mathf.Max(1, Mathf.RoundToInt(size.x / CellSize)),
                Mathf.Max(1, Mathf.RoundToInt(size.z / CellSize)));

            Debug.Log($"FootprintOf: '{id}' measures {size.x:0.##}x{size.z:0.##}m, {size.y:0.##}m tall -> {footprint.x}x{footprint.y} cells.");
            return footprint;
        }

        /// <summary>
        /// A fence segment. Unlike every other building, its prefab carries TWO models -- the
        /// straight run and the corner -- and FenceAppearance shows whichever one matches the
        /// neighbouring segments (see FenceShape). Nothing about which model is visible is decided
        /// here or saved: it is derived from the cells around it every time the line changes, so a
        /// destroyed segment turns its neighbours back into dead ends for free.
        ///
        /// Two deliberate differences from CreateBuildingData: the collider spans the WHOLE cell
        /// with no BuildingInset, because segments have to touch to read as one fence, and each
        /// model hangs inside a wrapper so FenceAppearance can turn the wrapper without disturbing
        /// whatever local rotation the FBX brought with it.
        /// </summary>
        private static BuildingData CreateFenceBuildingData(string id, Material trimMaterial)
        {
            var root = new GameObject(id);
            root.AddComponent<BuildingInstance>();
            var appearance = root.AddComponent<FenceAppearance>();

            // One wood material for both models, made here rather than inside each of them.
            var woodMaterial = CreateLitMaterial("Building_Fence_Wood", new Color(0.5f, 0.36f, 0.22f));
            var straight = CreateFenceModel("Straight", root.transform, trimMaterial, woodMaterial, isCorner: false, height: out var modelHeight);
            var corner = CreateFenceModel("Corner", root.transform, trimMaterial, woodMaterial, isCorner: true, height: out _);
            appearance.SetModels(straight, corner);
            // The straight run is what a lone segment shows; FenceAppearance swaps them the moment
            // the piece learns about its neighbours.
            if (corner != null) corner.SetActive(false);

            var height = modelHeight > 0.01f ? modelHeight : 1.2f;
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(CellSize, height, CellSize);
            collider.center = new Vector3(0f, height * 0.5f, 0f);

            var prefab = SavePrefab(root, id);

            var data = ScriptableObject.CreateInstance<BuildingData>();
            data.buildingName = id;
            data.prefab = prefab;
            data.footprintSize = Vector2Int.one;
            ApplyBalance(data, Balance(id));
            data.connectsToFences = true;
            // Laid out tile by tile like a road, so the hotbar selection survives each placement.
            data.keepSelectedAfterPlacement = true;

            Directory.CreateDirectory(BuildingDataFolder);
            var dataPath = $"{BuildingDataFolder}/{id}.asset";
            DeleteIfExists(dataPath);
            AssetDatabase.CreateAsset(data, dataPath);
            return data;
        }

        /// <summary>
        /// One fence segment, built rather than imported: posts and two rails, laid along Z for a
        /// straight run and turned into the south-east elbow for a corner. FenceAppearance shows
        /// whichever of the two matches the neighbours and rotates the wrapper to face them, so the
        /// geometry below only ever has to describe ONE orientation of each -- see FenceShape, and
        /// FenceShapeTests, which fails deliberately if either is drawn facing another way.
        /// </summary>
        private static GameObject CreateFenceModel(string wrapperName, Transform parent, Material trimMaterial, Material woodMaterial, bool isCorner, out float height)
        {
            const float postSide = 0.14f;
            const float postHeight = 0.95f;
            const float railThickness = 0.07f;
            var half = CellSize * 0.5f;

            var wrapper = new GameObject(wrapperName);
            wrapper.transform.SetParent(parent, false);

            void Post(string partName, float x, float z)
            {
                AddCubePart(wrapper.transform, partName, new Vector3(x, postHeight * 0.5f, z), new Vector3(postSide, postHeight, postSide), woodMaterial);
                AddCubePart(wrapper.transform, partName + "Cap", new Vector3(x, postHeight + 0.03f, z), new Vector3(postSide * 1.3f, 0.06f, postSide * 1.3f), trimMaterial);
            }

            // A rail from the centre of the cell out to one edge, so a straight run is two of them
            // back to back and a corner is two at right angles. Length half a cell either way, which
            // is what makes neighbouring segments meet exactly on the cell border.
            void Rail(string partName, float y, Vector3 direction)
            {
                var centre = direction * (half * 0.5f);
                var size = direction.z != 0f
                    ? new Vector3(railThickness, railThickness * 1.6f, half)
                    : new Vector3(half, railThickness * 1.6f, railThickness);
                AddCubePart(wrapper.transform, partName, new Vector3(centre.x, y, centre.z), size, woodMaterial);
            }

            Post("PostCentre", 0f, 0f);

            if (isCorner)
            {
                // South and east, matching the corner the fence network expects.
                Post("PostSouth", 0f, -half);
                Post("PostEast", half, 0f);
                Rail("RailSouthHigh", postHeight * 0.74f, Vector3.back);
                Rail("RailSouthLow", postHeight * 0.36f, Vector3.back);
                Rail("RailEastHigh", postHeight * 0.74f, Vector3.right);
                Rail("RailEastLow", postHeight * 0.36f, Vector3.right);
            }
            else
            {
                Post("PostSouth", 0f, -half);
                Post("PostNorth", 0f, half);
                Rail("RailSouthHigh", postHeight * 0.74f, Vector3.back);
                Rail("RailSouthLow", postHeight * 0.36f, Vector3.back);
                Rail("RailNorthHigh", postHeight * 0.74f, Vector3.forward);
                Rail("RailNorthLow", postHeight * 0.36f, Vector3.forward);
            }

            height = postHeight + 0.06f;
            return wrapper;
        }

        /// <summary>
        /// The balance sheet's row for a building. A missing row is a build error rather than a
        /// silent default: the asset would still be generated, but full of placeholder numbers that
        /// look deliberate in the Inspector and would be very hard to trace back to a renamed row.
        /// </summary>
        private static BuildingBalance Balance(string id)
        {
            var row = _balance != null ? _balance.Building(id) : null;
            if (row != null) return row;

            Debug.LogError($"SetupProject: the balance sheet's buildings tab has no row with id '{id}'. " +
                           $"'{id}' is being generated with placeholder numbers -- add the row to {BalanceImporter.BuildingsCsvPath} and rebuild.");
            return new BuildingBalance { id = id, displayName = id };
        }

        /// <summary>Copies every balanced number from a sheet row onto a building's data asset. Shape (footprint, prefab, road/water flags) is the caller's business.</summary>
        private static void ApplyBalance(BuildingData data, BuildingBalance balance)
        {
            data.displayName = balance.displayName;
            data.category = balance.category;
            data.cost = CopyCost(balance.cost);
            data.recipes = CopyRecipes(balance.recipes);
            data.productionIntervalSeconds = balance.productionIntervalSeconds;
            data.fogRevealRadius = balance.fogRevealRadius;
            data.citizensOnBuild = balance.citizensOnBuild;
            data.storageGroup = balance.storageGroup;
            data.levels = CopyLevels(balance.levels);
            data.upgradeToLevel2Cost = CopyCost(balance.upgradeToLevel2Cost);
            data.upgradeToLevel3Cost = CopyCost(balance.upgradeToLevel3Cost);
        }

        /// <summary>The level-1 stats of a sheet row, or defaults if the row is missing its levels entirely (already reported by Balance).</summary>
        private static BuildingLevelStats LevelOne(BuildingBalance balance)
        {
            return balance.levels != null && balance.levels.Count > 0 && balance.levels[0] != null
                ? balance.levels[0]
                : new BuildingLevelStats();
        }

        /// <summary>Whether this building has a defence stat at any of its levels -- see the DefensiveBuilding attachment.</summary>
        private static bool FightsBackAtAnyLevel(BuildingBalance balance)
        {
            if (balance.levels == null) return false;
            foreach (var level in balance.levels)
            {
                if (level != null && level.defense > 0) return true;
            }
            return false;
        }

        /// <summary>Whether this building has worker slots at any of its levels -- see the ProductionBuilding attachment. Same reasoning as FightsBackAtAnyLevel: a workshop that only takes hands on at level 2 is upgraded, never rebuilt.</summary>
        private static bool EmploysAtAnyLevel(BuildingBalance balance)
        {
            if (balance.levels == null) return false;
            foreach (var level in balance.levels)
            {
                if (level != null && level.maxWorkers > 0) return true;
            }
            return false;
        }

        /// <summary>A fresh set of level stats, for the same reason CopyCost exists: the config asset's own objects must never be shared with a building asset.</summary>
        private static List<BuildingLevelStats> CopyLevels(List<BuildingLevelStats> source)
        {
            var copy = new List<BuildingLevelStats>(source.Count);
            foreach (var level in source)
            {
                copy.Add(new BuildingLevelStats
                {
                    maxHealth = level.maxHealth,
                    defense = level.defense,
                    housingCapacity = level.housingCapacity,
                    maxWorkers = level.maxWorkers,
                    batchesPerWorkerPerTick = level.batchesPerWorkerPerTick,
                    storageCapacity = level.storageCapacity,
                    happiness = level.happiness,
                    harvestRadius = level.harvestRadius,
                    serviceRadius = level.serviceRadius,
                });
            }
            return copy;
        }

        /// <summary>A fresh recipe list, for the same reason as CopyCost: the config asset's own objects must never be shared with a building asset.</summary>
        private static List<BuildingRecipe> CopyRecipes(List<BuildingRecipe> source)
        {
            var copy = new List<BuildingRecipe>();
            if (source == null) return copy;

            foreach (var recipe in source)
            {
                copy.Add(new BuildingRecipe
                {
                    id = recipe.id,
                    displayName = recipe.displayName,
                    inputs = CopyCost(recipe.inputs),
                    output = recipe.output,
                    outputAmount = recipe.outputAmount,
                });
            }
            return copy;
        }

        /// <summary>A fresh cost list -- the config asset's own ResourceAmount objects must never end up referenced by a building asset.</summary>
        private static List<ResourceAmount> CopyCost(List<ResourceAmount> source)
        {
            var copy = new List<ResourceAmount>(source.Count);
            foreach (var amount in source)
            {
                copy.Add(new ResourceAmount { type = amount.type, amount = amount.amount });
            }
            return copy;
        }

        /// <summary>
        /// Wires BuildingData.requiredBuilding from the sheet's "requires" column, once every asset
        /// exists -- a prerequisite is a reference to another building, so it cannot be resolved
        /// while the buildings are still being created one by one.
        ///
        /// CreateAsset has already written each asset to disk by now, so mutating one afterwards
        /// needs an explicit SetDirty; without it the closing SaveAssets silently drops the edit.
        /// </summary>
        private static void ResolveBuildingRequirements(List<BuildingData> buildings)
        {
            var byId = new Dictionary<string, BuildingData>(buildings.Count);
            foreach (var building in buildings)
            {
                byId[building.buildingName] = building;
            }

            foreach (var building in buildings)
            {
                var requiredId = Balance(building.buildingName).requiredBuildingId;
                if (string.IsNullOrWhiteSpace(requiredId)) continue;

                if (!byId.TryGetValue(requiredId.Trim(), out var required))
                {
                    Debug.LogError($"SetupProject: '{building.buildingName}' requires '{requiredId}', which is not a building id. " +
                                   $"Check the requires column in {BalanceImporter.BuildingsCsvPath} -- the prerequisite is being left empty.");
                    continue;
                }

                building.requiredBuilding = required;
                EditorUtility.SetDirty(building);
            }
        }

        /// <summary>
        /// Procedurally assembles a dwelling/workshop from cube primitives: a stepped plinth,
        /// corner posts, a framed door with a step, a window count derived from wall width
        /// (placed on both the front and back faces), a banded three-tier roof with a ridge cap,
        /// and an optional chimney with a cap. The part count and placement all come from
        /// footprint/height, not hand-picked positions, so the same generator produces a small
        /// House and a wider Lumberjack alike.
        /// </summary>
        private static GameObject CreateHutPrefab(string name, Vector2Int footprint, float height, Color wallColor, Color roofColor, int maxWorkers, bool hasChimney, Material trimMaterial, Material doorMaterial, Material windowMaterial, BuildingMotif motif = BuildingMotif.None)
        {
            var sizeX = footprint.x * CellSize - BuildingInset;
            var sizeZ = footprint.y * CellSize - BuildingInset;

            var root = new GameObject(name);
            root.AddComponent<BuildingInstance>();
            if (maxWorkers > 0) root.AddComponent<ProductionBuilding>();

            var wallMaterial = CreateLitMaterial($"Building_{name}_Walls", wallColor);
            var roofMaterial = CreateLitMaterial($"Building_{name}_Roof", roofColor);
            var roofShadeMaterial = CreateLitMaterial($"Building_{name}_RoofShade", Shade(roofColor, 0.8f));

            var y = AddSteppedPlinth(root.transform, sizeX, sizeZ, height * 0.1f, trimMaterial, wallMaterial);

            var wallHeight = height * 0.42f;
            AddCubePart(root.transform, "Walls", new Vector3(0f, y + wallHeight * 0.5f, 0f), new Vector3(sizeX, wallHeight, sizeZ), wallMaterial);
            AddCornerPosts(root.transform, sizeX, sizeZ, y, wallHeight, trimMaterial);

            var doorWidth = Mathf.Clamp(sizeX * 0.24f, 0.36f, 0.6f);
            var doorHeight = wallHeight * 0.62f;
            AddFramedOpening(root.transform, "Door", 0f, y + doorHeight * 0.5f, -sizeZ * 0.5f, -1f, doorWidth, doorHeight, trimMaterial, doorMaterial);
            AddCubePart(root.transform, "Doorstep", new Vector3(0f, y - 0.03f, -sizeZ * 0.5f - 0.22f), new Vector3(doorWidth + 0.3f, 0.06f, 0.3f), trimMaterial);

            // Window count derived from wall width — wider buildings procedurally get more,
            // mirrored on the front and back faces.
            var windowCount = Mathf.Clamp(Mathf.FloorToInt(sizeX / 1.3f), 0, 2);
            var windowY = y + wallHeight * 0.7f;
            for (var i = 0; i < windowCount; i++)
            {
                var t = (i + 1f) / (windowCount + 1f);
                var wx = Mathf.Lerp(-sizeX * 0.5f + 0.35f, sizeX * 0.5f - 0.35f, t);
                if (Mathf.Abs(wx) < doorWidth * 0.7f) continue; // skip over the door
                AddFramedOpening(root.transform, $"WindowFront{i}", wx, windowY, -sizeZ * 0.5f, -1f, 0.22f, 0.22f, trimMaterial, windowMaterial);
                AddFramedOpening(root.transform, $"WindowBack{i}", wx, windowY, sizeZ * 0.5f, 1f, 0.22f, 0.22f, trimMaterial, windowMaterial);
            }

            y += wallHeight;

            AddCubePart(root.transform, "Fascia", new Vector3(0f, y + 0.03f, 0f), new Vector3(sizeX * 1.05f, 0.06f, sizeZ * 1.05f), trimMaterial);
            y += 0.06f;

            y = AddShingleRoof(root.transform, sizeX, sizeZ, y, height - y, 3, roofMaterial, roofShadeMaterial, trimMaterial);

            if (hasChimney)
            {
                var chimneyHeight = height * 0.2f;
                var chimneySize = Mathf.Min(sizeX, sizeZ) * 0.15f;
                var chimneyBase = Mathf.Max(0f, y - height * 0.3f);
                AddCubePart(root.transform, "Chimney", new Vector3(sizeX * 0.28f, chimneyBase + chimneyHeight * 0.5f, sizeZ * 0.28f), new Vector3(chimneySize, chimneyHeight, chimneySize), wallMaterial);
                AddCubePart(root.transform, "ChimneyCap", new Vector3(sizeX * 0.28f, chimneyBase + chimneyHeight + 0.03f, sizeZ * 0.28f), new Vector3(chimneySize * 1.4f, 0.06f, chimneySize * 1.4f), trimMaterial);
                y = Mathf.Max(y, chimneyBase + chimneyHeight + 0.06f);
            }

            y = AddMotif(root.transform, motif, name, sizeX, sizeZ, y, height, wallMaterial, roofMaterial, trimMaterial);

            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(sizeX * 1.1f, y, sizeZ * 1.1f);
            collider.center = new Vector3(0f, y * 0.5f, 0f);

            return SavePrefab(root, name);
        }

        /// <summary>
        /// Procedurally assembles a wall segment or tower: a stepped plinth, wall block(s) with
        /// arrow slits, and a crenellation ring generated by looping around the footprint's
        /// perimeter (a merlon count derived from size, not four hand-typed corner positions).
        /// Towers get two stacked, slightly inset wall tiers plus a lookout cap instead of one
        /// solid block, reading as noticeably taller and more fortified than a plain wall.
        /// </summary>
        private static GameObject CreateFortificationPrefab(string name, Vector2Int footprint, float height, Color wallColor, Color roofColor, Material trimMaterial, bool isTower, BuildingMotif motif = BuildingMotif.None)
        {
            var sizeX = footprint.x * CellSize - BuildingInset;
            var sizeZ = footprint.y * CellSize - BuildingInset;

            var root = new GameObject(name);
            root.AddComponent<BuildingInstance>();

            var wallMaterial = CreateLitMaterial($"Building_{name}_Walls", wallColor);
            var shadeMaterial = CreateLitMaterial($"Building_{name}_Shade", Shade(wallColor, 0.85f));
            var roofMaterial = CreateLitMaterial($"Building_{name}_Roof", roofColor);

            var y = AddSteppedPlinth(root.transform, sizeX, sizeZ, height * 0.08f, trimMaterial, wallMaterial);

            if (isTower)
            {
                var tier1Height = height * 0.34f;
                AddCubePart(root.transform, "WallsLower", new Vector3(0f, y + tier1Height * 0.5f, 0f), new Vector3(sizeX, tier1Height, sizeZ), wallMaterial);
                AddArrowSlits(root.transform, "SlitLower", sizeX, sizeZ, y + tier1Height * 0.6f, trimMaterial);
                y += tier1Height;

                const float inset = 0.9f;
                var tier2Height = height * 0.28f;
                AddCubePart(root.transform, "WallsUpper", new Vector3(0f, y + tier2Height * 0.5f, 0f), new Vector3(sizeX * inset, tier2Height, sizeZ * inset), shadeMaterial);
                AddArrowSlits(root.transform, "SlitUpper", sizeX * inset, sizeZ * inset, y + tier2Height * 0.6f, trimMaterial);
                y += tier2Height;

                var merlonHeight = height * 0.12f;
                AddCrenellationRing(root.transform, "Merlon", sizeX * inset, sizeZ * inset, y + merlonHeight * 0.5f, merlonHeight, trimMaterial);
                y += merlonHeight;

                var capHeight = height * 0.14f;
                var capSize = Mathf.Min(sizeX, sizeZ) * 0.5f;
                AddCubePart(root.transform, "Lookout", new Vector3(0f, y + capHeight * 0.5f, 0f), new Vector3(capSize, capHeight, capSize), wallMaterial);
                y += capHeight;
            }
            else
            {
                var wallHeight = height * 0.66f;
                AddCubePart(root.transform, "Walls", new Vector3(0f, y + wallHeight * 0.5f, 0f), new Vector3(sizeX, wallHeight, sizeZ), wallMaterial);
                AddArrowSlits(root.transform, "Slit", sizeX, sizeZ, y + wallHeight * 0.62f, trimMaterial);
                y += wallHeight;

                var merlonHeight = height * 0.15f;
                AddCrenellationRing(root.transform, "Merlon", sizeX, sizeZ, y + merlonHeight * 0.5f, merlonHeight, trimMaterial);
                y += merlonHeight;
            }

            y = AddMotif(root.transform, motif, name, sizeX, sizeZ, y, height, wallMaterial, roofMaterial, trimMaterial);

            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(sizeX * 1.1f, y, sizeZ * 1.1f);
            collider.center = new Vector3(0f, y * 0.5f, 0f);

            return SavePrefab(root, name);
        }

        /// <summary>
        /// The shell for everything that is not a building with walls: a well, a flag, a garden, a
        /// gate you walk through, an arena with an open floor. It lays a paved base the size of the
        /// footprint and leaves the rest to the motif, which is the whole object here rather than
        /// an ornament on top of one.
        ///
        /// The base is deliberately thin and NOT a trigger, unlike a road: these are things a
        /// player taps and a citizen walks around, not surfaces they walk over.
        /// </summary>
        private static GameObject CreatePlotPrefab(string name, Vector2Int footprint, float height, Color baseColor, Color accentColor, int maxWorkers, Material trimMaterial, BuildingMotif motif)
        {
            var sizeX = footprint.x * CellSize - BuildingInset;
            var sizeZ = footprint.y * CellSize - BuildingInset;

            var root = new GameObject(name);
            root.AddComponent<BuildingInstance>();
            if (maxWorkers > 0) root.AddComponent<ProductionBuilding>();

            var baseMaterial = CreateLitMaterial($"Building_{name}_Base", baseColor);
            var accentMaterial = CreateLitMaterial($"Building_{name}_Accent", accentColor);

            const float paving = 0.08f;
            AddCubePart(root.transform, "Paving", new Vector3(0f, paving * 0.5f, 0f), new Vector3(sizeX, paving, sizeZ), baseMaterial);
            AddCubePart(root.transform, "PavingEdge", new Vector3(0f, paving * 0.5f + 0.01f, 0f), new Vector3(sizeX * 0.86f, paving, sizeZ * 0.86f), accentMaterial);

            var y = AddMotif(root.transform, motif, name, sizeX, sizeZ, paving, height, baseMaterial, accentMaterial, trimMaterial);
            // Never flatter than a step: a collider a few centimetres tall is a building nobody can
            // tap, and half of these are one cell across to begin with.
            y = Mathf.Max(y, 0.4f);

            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(sizeX * 1.05f, y, sizeZ * 1.05f);
            collider.center = new Vector3(0f, y * 0.5f, 0f);

            return SavePrefab(root, name);
        }

        /// <summary>
        /// A flat, thin paved tile with a dashed centerline stripe -- players lay these one at a
        /// time (footprint 1x1) to build a continuous road; see RoadNetwork/CitizenAgent for how
        /// citizens detect and speed up on them. Its collider is a trigger, unlike every other
        /// building's solid BoxCollider: CharacterController ignores triggers for movement
        /// blocking, so citizens walk straight over a road (staying grounded on the terrain
        /// collider beneath) instead of treating it as an obstacle, while the trigger still lets
        /// BuildingPlacer/BuildingSelector's raycasts detect it for ground-picking and clicking.
        /// </summary>
        private static GameObject CreateRoadPrefab(string name, Color roadColor, Color stripeColor)
        {
            var sizeX = CellSize - 0.04f;
            var sizeZ = CellSize - 0.04f;
            const float thickness = 0.05f;

            var root = new GameObject(name);
            root.AddComponent<BuildingInstance>();

            var roadMaterial = CreateLitMaterial($"Building_{name}_Surface", roadColor);
            var stripeMaterial = CreateLitMaterial($"Building_{name}_Stripe", stripeColor);

            AddCubePart(root.transform, "Surface", new Vector3(0f, thickness * 0.5f, 0f), new Vector3(sizeX, thickness, sizeZ), roadMaterial);
            AddCubePart(root.transform, "StripeNorth", new Vector3(0f, thickness + 0.01f, sizeZ * 0.27f), new Vector3(sizeX * 0.12f, 0.02f, sizeZ * 0.34f), stripeMaterial);
            AddCubePart(root.transform, "StripeSouth", new Vector3(0f, thickness + 0.01f, -sizeZ * 0.27f), new Vector3(sizeX * 0.12f, 0.02f, sizeZ * 0.34f), stripeMaterial);

            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(sizeX, thickness, sizeZ);
            collider.center = new Vector3(0f, thickness * 0.5f, 0f);
            collider.isTrigger = true;

            return SavePrefab(root, name);
        }

        /// <summary>
        /// A wooden deck on short corner posts (a "built on stilts over the water" read), used for
        /// every isWaterCategory production building -- see CreateWaterBuildingData. addWheel adds
        /// a mill paddle wheel (a cross of two thin blades, not a true circle -- matching the
        /// no-circles style everywhere else); addCrates adds a couple of cargo props for a dock.
        /// </summary>
        private static GameObject CreateWaterBuildingPrefab(string name, Vector2Int footprint, Color deckColor, Color accentColor, bool addWheel, bool addCrates)
        {
            var sizeX = footprint.x * CellSize - BuildingInset;
            var sizeZ = footprint.y * CellSize - BuildingInset;
            const float deckThickness = 0.14f;
            const float postDepth = 0.4f;

            var root = new GameObject(name);
            root.AddComponent<BuildingInstance>();

            var deckMaterial = CreateLitMaterial($"Building_{name}_Deck", deckColor);
            var accentMaterial = CreateLitMaterial($"Building_{name}_Accent", accentColor);

            AddCubePart(root.transform, "Deck", new Vector3(0f, deckThickness * 0.5f, 0f), new Vector3(sizeX, deckThickness, sizeZ), deckMaterial);

            var halfX = sizeX * 0.5f - 0.15f;
            var halfZ = sizeZ * 0.5f - 0.15f;
            var postIndex = 0;
            for (var sx = -1; sx <= 1; sx += 2)
            {
                for (var sz = -1; sz <= 1; sz += 2)
                {
                    AddCubePart(root.transform, $"Post{postIndex}", new Vector3(sx * halfX, -postDepth * 0.5f, sz * halfZ), new Vector3(0.12f, postDepth, 0.12f), accentMaterial);
                    postIndex++;
                }
            }

            if (addWheel)
            {
                var postX = sizeX * 0.5f + 0.12f;
                AddCubePart(root.transform, "WheelPost", new Vector3(postX, 0.8f, 0f), new Vector3(0.14f, 1.6f, 0.14f), accentMaterial);
                AddCubePart(root.transform, "BladeVertical", new Vector3(postX, 0.9f, 0f), new Vector3(0.08f, 1.3f, 0.16f), deckMaterial);
                AddCubePart(root.transform, "BladeHorizontal", new Vector3(postX, 0.9f, 0f), new Vector3(0.08f, 0.16f, 1.3f), deckMaterial);
            }

            if (addCrates)
            {
                AddCubePart(root.transform, "CrateA", new Vector3(sizeX * 0.22f, deckThickness + 0.15f, sizeZ * 0.2f), new Vector3(0.3f, 0.3f, 0.3f), accentMaterial);
                AddCubePart(root.transform, "CrateB", new Vector3(-sizeX * 0.18f, deckThickness + 0.12f, -sizeZ * 0.25f), new Vector3(0.24f, 0.24f, 0.24f), accentMaterial);
            }

            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(sizeX, deckThickness + postDepth, sizeZ);
            collider.center = new Vector3(0f, (deckThickness - postDepth) * 0.5f, 0f);

            return SavePrefab(root, name);
        }

        /// <summary>
        /// A water-only production building (BuildingData.isWaterCategory) -- placeable exclusively
        /// within a mesh map's water-placement zone, and never touching dry Ground (see
        /// BuildingPlacer.CanPlaceSelectedBuilding). Mirrors CreateBuildingData's data-assignment
        /// tail but drives CreateWaterBuildingPrefab instead of the land-building style switch.
        /// </summary>
        private static BuildingData CreateWaterBuildingData(
            string id, Vector2Int footprint, Color deckColor, Color accentColor,
            bool addWheel = false, bool addCrates = false)
        {
            var prefab = CreateWaterBuildingPrefab(id, footprint, deckColor, accentColor, addWheel, addCrates);

            var data = ScriptableObject.CreateInstance<BuildingData>();
            data.buildingName = id;
            data.prefab = prefab;
            data.footprintSize = footprint;
            ApplyBalance(data, Balance(id));
            data.isWaterCategory = true;

            Directory.CreateDirectory(BuildingDataFolder);
            var dataPath = $"{BuildingDataFolder}/{id}.asset";
            DeleteIfExists(dataPath);
            AssetDatabase.CreateAsset(data, dataPath);
            return data;
        }

        /// <summary>Two-tier foundation (a wide dark base course, then a narrower top course).</summary>
        private static float AddSteppedPlinth(Transform parent, float sizeX, float sizeZ, float totalHeight, Material baseMaterial, Material topMaterial)
        {
            var baseHeight = totalHeight * 0.55f;
            var topHeight = totalHeight * 0.45f;
            AddCubePart(parent, "PlinthBase", new Vector3(0f, baseHeight * 0.5f, 0f), new Vector3(sizeX * 1.1f, baseHeight, sizeZ * 1.1f), baseMaterial);
            AddCubePart(parent, "PlinthTop", new Vector3(0f, baseHeight + topHeight * 0.5f, 0f), new Vector3(sizeX * 1.03f, topHeight, sizeZ * 1.03f), topMaterial);
            return totalHeight;
        }

        /// <summary>Four thin trim-colored pillars at the wall corners, looped over corner sign combinations.</summary>
        private static void AddCornerPosts(Transform parent, float sizeX, float sizeZ, float y, float postHeight, Material material)
        {
            const float thickness = 0.12f;
            var halfX = sizeX * 0.5f - thickness * 0.5f;
            var halfZ = sizeZ * 0.5f - thickness * 0.5f;
            var index = 0;
            for (var sx = -1; sx <= 1; sx += 2)
            {
                for (var sz = -1; sz <= 1; sz += 2)
                {
                    AddCubePart(parent, $"CornerPost{index}", new Vector3(sx * halfX, y + postHeight * 0.5f, sz * halfZ), new Vector3(thickness, postHeight, thickness), material);
                    index++;
                }
            }
        }

        /// <summary>A door or window as two layers — a slightly larger dark frame behind a smaller inset pane — instead of one flat cube.</summary>
        private static void AddFramedOpening(Transform parent, string name, float x, float y, float zFace, float outwardSign, float width, float height, Material frameMaterial, Material paneMaterial)
        {
            AddCubePart(parent, $"{name}Frame", new Vector3(x, y, zFace + outwardSign * 0.01f), new Vector3(width + 0.08f, height + 0.08f, 0.04f), frameMaterial);
            AddCubePart(parent, $"{name}Pane", new Vector3(x, y, zFace + outwardSign * 0.05f), new Vector3(width, height, 0.05f), paneMaterial ?? frameMaterial);
        }

        /// <summary>Thin vertical arrow-slit accents on the front face, count derived from wall width.</summary>
        private static void AddArrowSlits(Transform parent, string prefix, float sizeX, float sizeZ, float y, Material material)
        {
            var count = Mathf.Clamp(Mathf.RoundToInt(sizeX / 1.3f), 1, 2);
            for (var i = 0; i < count; i++)
            {
                var t = (i + 1f) / (count + 1f);
                var x = Mathf.Lerp(-sizeX * 0.5f + 0.3f, sizeX * 0.5f - 0.3f, t);
                AddCubePart(parent, $"{prefix}{i}", new Vector3(x, y, -sizeZ * 0.5f - 0.02f), new Vector3(0.08f, 0.32f, 0.04f), material);
            }
        }

        /// <summary>
        /// A crenellation (battlement) ring: merlons spaced along all four footprint edges,
        /// with the count per edge derived from size — not four hand-typed corner positions.
        /// </summary>
        private static void AddCrenellationRing(Transform parent, string prefix, float sizeX, float sizeZ, float y, float merlonHeight, Material material)
        {
            const float thickness = 0.22f;
            var halfX = sizeX * 0.5f - thickness * 0.5f;
            var halfZ = sizeZ * 0.5f - thickness * 0.5f;
            var perSide = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(sizeX, sizeZ) / 1.4f), 2, 4);

            var index = 0;
            for (var i = 0; i < perSide; i++)
            {
                var t = perSide == 1 ? 0f : Mathf.Lerp(-0.82f, 0.82f, (float)i / (perSide - 1));
                AddCubePart(parent, $"{prefix}{index++}", new Vector3(t * halfX, y, -halfZ), new Vector3(thickness, merlonHeight, thickness), material);
                AddCubePart(parent, $"{prefix}{index++}", new Vector3(t * halfX, y, halfZ), new Vector3(thickness, merlonHeight, thickness), material);
                AddCubePart(parent, $"{prefix}{index++}", new Vector3(-halfX, y, t * halfZ), new Vector3(thickness, merlonHeight, thickness), material);
                AddCubePart(parent, $"{prefix}{index++}", new Vector3(halfX, y, t * halfZ), new Vector3(thickness, merlonHeight, thickness), material);
            }
        }

        /// <summary>
        /// N stepped, shrinking roof tiers with alternating shade (a cheap banded/"shingle"
        /// look) topped with a thin ridge cap, returning the y after the ridge.
        /// </summary>
        private static float AddShingleRoof(Transform parent, float sizeX, float sizeZ, float yStart, float heightBudget, int tiers, Material roofMaterial, Material roofShadeMaterial, Material trimMaterial)
        {
            var y = yStart;
            var scale = 1f;
            for (var tier = 0; tier < tiers; tier++)
            {
                var tierHeight = (heightBudget / tiers) * (1f - tier * 0.1f);
                scale *= Mathf.Lerp(0.92f, 0.6f, tiers <= 1 ? 0f : (float)tier / (tiers - 1));
                var material = tier % 2 == 0 ? roofMaterial : roofShadeMaterial;
                AddCubePart(parent, $"RoofTier{tier}", new Vector3(0f, y + tierHeight * 0.5f, 0f), new Vector3(sizeX * scale, tierHeight, sizeZ * scale), material);
                y += tierHeight;
            }

            AddCubePart(parent, "RoofRidge", new Vector3(0f, y + 0.03f, 0f), new Vector3(sizeX * scale * 0.9f, 0.06f, 0.12f), trimMaterial);
            y += 0.06f;

            return y;
        }

        private static Color Shade(Color color, float factor)
        {
            return new Color(Mathf.Clamp01(color.r * factor), Mathf.Clamp01(color.g * factor), Mathf.Clamp01(color.b * factor), color.a);
        }

        private static GameObject AddCubePart(Transform parent, string partName, Vector3 localPosition, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = partName;
            Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static GameObject SavePrefab(GameObject root, string name)
        {
            Directory.CreateDirectory(BuildingPrefabsFolder);
            var prefabPath = $"{BuildingPrefabsFolder}/{name}.prefab";
            DeleteIfExists(prefabPath);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreateForestBorder(float innerHalfWidth, float innerHalfDepth, float outerHalfWidth, float outerHalfDepth, Material trunkMaterial, Material canopyMaterial)
        {
            var root = new GameObject("Forest");

            // Fixed seed so re-running the setup script produces the same layout every time.
            Random.InitState(12345);
            const int treeCount = 160;
            var placed = 0;
            var attempts = 0;

            while (placed < treeCount && attempts < treeCount * 10)
            {
                attempts++;
                var x = Random.Range(-outerHalfWidth, outerHalfWidth);
                var z = Random.Range(-outerHalfDepth, outerHalfDepth);
                if (Mathf.Abs(x) < innerHalfWidth && Mathf.Abs(z) < innerHalfDepth) continue; // inside the buildable area

                var scale = Random.Range(0.8f, 1.3f);
                var trunkHeight = 1f * scale;
                var canopySize = 1.6f * scale;
                var canopyHeight = 1.4f * scale;

                var treeRoot = new GameObject("Tree");
                treeRoot.transform.position = new Vector3(x, 0f, z);
                treeRoot.transform.SetParent(root.transform, true);

                // Fully cubic tree: a box trunk with a box canopy on top — no rounded primitives.
                var trunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trunk.name = "Trunk";
                Object.DestroyImmediate(trunk.GetComponent<BoxCollider>());
                trunk.transform.SetParent(treeRoot.transform, false);
                trunk.transform.localScale = new Vector3(0.35f * scale, trunkHeight, 0.35f * scale);
                trunk.transform.localPosition = new Vector3(0f, trunkHeight * 0.5f, 0f);
                trunk.GetComponent<Renderer>().sharedMaterial = trunkMaterial;

                var canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
                canopy.name = "Canopy";
                Object.DestroyImmediate(canopy.GetComponent<BoxCollider>());
                canopy.transform.SetParent(treeRoot.transform, false);
                canopy.transform.localScale = new Vector3(canopySize, canopyHeight, canopySize);
                canopy.transform.localPosition = new Vector3(0f, trunkHeight + canopyHeight * 0.5f, 0f);
                canopy.GetComponent<Renderer>().sharedMaterial = canopyMaterial;

                placed++;
            }
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
        /// Loads a hand-authored map's FBX prefabs by their fixed Models/ paths and writes a
        /// MeshMapDefinition asset under Resources/MeshMaps so MeshMapCatalog/MapSelector can find
        /// it. Parameterized rather than folder-scanning like the old MapImporter — a second map
        /// is just one more call to this with a different id/paths, not a new system.
        /// </summary>
        private static void CreateMeshMapDefinition(string mapId)
        {
            var ground = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsMap1Folder}/Map-1-Ground.fbx");
            var water = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsMap1Folder}/Map-1-Water.fbx");
            var waterPlacementZone = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsMap1Folder}/Map-1-PlaceForWaterObjects.fbx");
            var treesArea = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsMap1Folder}/Map-1-TreesArea.fbx");
            // Every Tree*.fbx variant, so TreesAreaSpawner has the full set to pick from at random
            // (see SpawnOneTree). Adding a sixth model is one more file plus a bump of
            // TreeModelCount -- nothing here is written per-model.
            var trees = new GameObject[TreeModelCount];
            var treesMissing = false;
            for (var i = 0; i < TreeModelCount; i++)
            {
                trees[i] = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsTerrainFolder}/Tree{i + 1}.fbx");
                if (trees[i] == null) treesMissing = true;
            }

            if (ground == null || water == null || waterPlacementZone == null || treesArea == null || treesMissing)
            {
                Debug.LogWarning($"CreateMeshMapDefinition: one or more Map1 FBX assets not found under {ModelsMap1Folder}/{ModelsTerrainFolder} -- skipping Map1 MeshMapDefinition.");
                return;
            }

            var waterMaterial = CreateWaterMaterial();

            var map = ScriptableObject.CreateInstance<MeshMapDefinition>();
            map.EditorInitialize(mapId, ground, water, waterPlacementZone, treesArea, trees, waterMaterial);

            Directory.CreateDirectory(MeshMapsFolder);
            var path = $"{MeshMapsFolder}/{mapId}.asset";
            DeleteIfExists(path);
            AssetDatabase.CreateAsset(map, path);
        }

        /// <summary>
        /// A dedicated top-down orthographic camera rendering into a RenderTexture for the HUD
        /// minimap (see BuildMinimap) -- a live view of the actual scene rather than a separately
        /// drawn icon map, so buildings/trees/roads the player places show up automatically.
        /// Sized to exactly frame the GridCellsX x GridCellsZ world span.
        /// </summary>
        private static RenderTexture CreateMinimapCamera()
        {
            var rtPath = $"{TexturesFolder}/Minimap_RT.renderTexture";
            Directory.CreateDirectory(TexturesFolder);
            DeleteIfExists(rtPath);
            var renderTexture = new RenderTexture(512, 512, 16) { name = "Minimap_RT" };
            AssetDatabase.CreateAsset(renderTexture, rtPath);

            var cameraGO = new GameObject("MinimapCamera", typeof(Camera));
            cameraGO.transform.position = new Vector3(0f, 150f, 0f);
            cameraGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var minimapCamera = cameraGO.GetComponent<Camera>();
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = GridCellsX * CellSize * 0.5f;
            minimapCamera.nearClipPlane = 1f;
            minimapCamera.farClipPlane = 400f;
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor = new Color(0.08f, 0.14f, 0.22f);
            minimapCamera.targetTexture = renderTexture;
            minimapCamera.cullingMask = ~0;

            var updater = cameraGO.AddComponent<MinimapCameraUpdater>();
            var updaterSO = new SerializedObject(updater);
            updaterSO.FindProperty("minimapCamera").objectReferenceValue = minimapCamera;
            updaterSO.ApplyModifiedPropertiesWithoutUndo();

            return renderTexture;
        }

        private static Material CreateGroundMaterial()
        {
            var path = $"{MaterialsFolder}/Ground.mat";
            DeleteIfExists(path);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.42f, 0.62f, 0.32f) };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        /// <summary>
        /// User-supplied Water.png (hand-authored alongside the Map1 FBX assets) applied as the
        /// main texture on a URP Lit material, assigned to the Water mesh's renderer at runtime
        /// by MeshMapApplier -- same "materials controlled explicitly through code" pattern as
        /// every other material in this file, just texture-backed instead of flat-color.
        /// </summary>
        private static Material CreateWaterMaterial()
        {
            var texturePath = $"{ModelsMap1Folder}/Water.png";
            AssetDatabase.ImportAsset(texturePath);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                Debug.LogWarning($"CreateWaterMaterial: Water.png not found at {texturePath} -- water will keep its FBX-imported default material.");
                return null;
            }

            var path = $"{MaterialsFolder}/Water.mat";
            Directory.CreateDirectory(MaterialsFolder);
            DeleteIfExists(path);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { mainTexture = texture };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        /// <summary>
        /// "Резной камень" (Style A): a carved-stone bevel -- a dark recessed groove around the
        /// rim, then a directional raised bevel (light catching the top/left edges, shadow on the
        /// bottom/right, the classic raised-panel look), dark rivet squares in the corners, flat
        /// mid-grey fill in the middle. Sharp 90-degree corners throughout (the game's cubic/
        /// angular style, no rounded UI). Colors are pushed to near-black/near-white specifically
        /// so the bevel still reads clearly after the heavy per-context dark .color tints used
        /// across the UI (a subtler greyscale range washed out to near-invisible once multiplied
        /// by those tints). 9-sliced (see the returned sprite's border) so it stretches cleanly to
        /// any button/panel size without smearing the bevel or rivets.
        /// </summary>
        private static Sprite CreatePanelSprite()
        {
            // Thick border (roughly a third of the texture per side) and near-black/near-white
            // contrast -- an earlier, subtler version (thin 8px border in a 32px texture) turned
            // out imperceptible once shrunk to actual button size and compressed in a screenshot.
            const int size = 48;
            const int border = 16;

            var fill = new Color(0.55f, 0.55f, 0.52f);
            var groove = new Color(0.02f, 0.02f, 0.02f);
            var lightEdge = new Color(0.96f, 0.96f, 0.92f);
            var darkEdge = new Color(0.1f, 0.09f, 0.08f);
            var rivet = new Color(0.02f, 0.02f, 0.02f);

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distLeft = x;
                    var distRight = size - 1 - x;
                    var distTop = size - 1 - y;
                    var distBottom = y;
                    var minEdge = Mathf.Min(Mathf.Min(distLeft, distRight), Mathf.Min(distTop, distBottom));

                    Color color;
                    if (minEdge < 3) color = groove;
                    else if (minEdge < border)
                    {
                        var isLightSide = distLeft <= distRight && distLeft <= distTop && distLeft <= distBottom
                            || distTop <= distRight && distTop <= distLeft && distTop <= distBottom;
                        color = isLightSide ? lightEdge : darkEdge;
                    }
                    else color = fill;
                    pixels[y * size + x] = color;
                }
            }

            void Rivet(int cx, int cy)
            {
                for (var dy = 0; dy < 4; dy++)
                {
                    for (var dx = 0; dx < 4; dx++)
                    {
                        pixels[(cy + dy) * size + (cx + dx)] = rivet;
                    }
                }
            }
            Rivet(5, 5);
            Rivet(size - 9, 5);
            Rivet(5, size - 9);
            Rivet(size - 9, size - 9);

            texture.SetPixels(pixels);
            texture.Apply();

            Directory.CreateDirectory(TexturesFolder);
            var texPath = $"{TexturesFolder}/UI_Panel.asset";
            DeleteIfExists(texPath);
            AssetDatabase.CreateAsset(texture, texPath);

            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            sprite.name = "UI_Panel_Sprite";
            AssetDatabase.AddObjectToAsset(sprite, texture);
            AssetDatabase.ImportAsset(texPath);
            return sprite;
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

        private static Sprite CreateIconSprite(string key, int size, System.Action<Color[], int> paint)
        {
            if (_iconSpriteCache.TryGetValue(key, out var cached) && cached != null) return cached;

            var pixels = new Color[size * size];
            paint(pixels, size);

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(pixels);
            texture.Apply();

            Directory.CreateDirectory(TexturesFolder);
            var path = $"{TexturesFolder}/Icon_{key}.asset";
            DeleteIfExists(path);
            AssetDatabase.CreateAsset(texture, path);

            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = $"Icon_{key}_Sprite";
            AssetDatabase.AddObjectToAsset(sprite, texture);
            AssetDatabase.ImportAsset(path);

            // Re-read the sprite through the AssetDatabase rather than handing back the instance
            // built above: ImportAsset re-creates the objects behind that path, so the local
            // reference is no longer the one that actually lives in the asset, and serializing it
            // into a scene writes a reference to nothing.
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite imported)
                {
                    _iconSpriteCache[key] = imported;
                    return imported;
                }
            }

            _iconSpriteCache[key] = sprite;
            return sprite;
        }

        /// <summary>Normalized (0,0)=bottom-left..(1,1)=top-right, axis-aligned fill.</summary>
        private static void FillIconRect(Color[] pixels, int size, float x0, float y0, float x1, float y1, Color color)
        {
            var px0 = Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(x0, x1) * size), 0, size);
            var px1 = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(x0, x1) * size), 0, size);
            var py0 = Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(y0, y1) * size), 0, size);
            var py1 = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(y0, y1) * size), 0, size);
            for (var y = py0; y < py1; y++)
            {
                for (var x = px0; x < px1; x++)
                {
                    pixels[y * size + x] = color;
                }
            }
        }

        /// <summary>Normalized (0,0)=bottom-left..(1,1)=top-right triangle fill (simple edge-function rasterization).</summary>
        private static void FillIconTriangle(Color[] pixels, int size, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            a *= size; b *= size; c *= size;
            var minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))), 0, size);
            var maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))), 0, size);
            var minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))), 0, size);
            var maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))), 0, size);

            for (var y = minY; y < maxY; y++)
            {
                for (var x = minX; x < maxX; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    var d1 = IconTriangleSign(p, a, b);
                    var d2 = IconTriangleSign(p, b, c);
                    var d3 = IconTriangleSign(p, c, a);
                    var hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
                    var hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
                    if (!(hasNeg && hasPos)) pixels[y * size + x] = color;
                }
            }
        }

        private static float IconTriangleSign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        private static Sprite CreateCategoryIcon(BuildingCategory category)
        {
            switch (category)
            {
                case BuildingCategory.City:
                    return CreateIconSprite("Cat_City", 64, (p, s) =>
                    {
                        FillIconTriangle(p, s, new Vector2(0.15f, 0.55f), new Vector2(0.85f, 0.55f), new Vector2(0.5f, 0.88f), new Color(0.55f, 0.18f, 0.16f));
                        FillIconRect(p, s, 0.22f, 0.14f, 0.78f, 0.55f, new Color(0.82f, 0.68f, 0.5f));
                        FillIconRect(p, s, 0.44f, 0.14f, 0.56f, 0.38f, new Color(0.35f, 0.22f, 0.12f));
                    });
                case BuildingCategory.Food:
                    return CreateIconSprite("Cat_Food", 64, (p, s) =>
                    {
                        FillIconRect(p, s, 0.15f, 0.12f, 0.85f, 0.4f, new Color(0.45f, 0.32f, 0.18f));
                        FillIconRect(p, s, 0.22f, 0.4f, 0.42f, 0.62f, new Color(0.75f, 0.18f, 0.16f));
                        FillIconRect(p, s, 0.42f, 0.4f, 0.58f, 0.68f, new Color(0.86f, 0.68f, 0.24f));
                        FillIconRect(p, s, 0.58f, 0.4f, 0.78f, 0.6f, new Color(0.35f, 0.55f, 0.28f));
                    });
                case BuildingCategory.Defence:
                    return CreateIconSprite("Cat_Defence", 64, (p, s) =>
                    {
                        var shield = new Color(0.45f, 0.46f, 0.5f);
                        FillIconRect(p, s, 0.22f, 0.42f, 0.78f, 0.85f, shield);
                        FillIconTriangle(p, s, new Vector2(0.22f, 0.42f), new Vector2(0.78f, 0.42f), new Vector2(0.5f, 0.12f), shield);
                        var trim = new Color(0.7f, 0.62f, 0.3f);
                        FillIconRect(p, s, 0.46f, 0.2f, 0.54f, 0.78f, trim);
                        FillIconRect(p, s, 0.3f, 0.58f, 0.7f, 0.66f, trim);
                    });
                case BuildingCategory.Storage:
                    return CreateIconSprite("Cat_Storage", 64, (p, s) =>
                    {
                        // Stacked crates -- the category is four storehouses and their big versions.
                        var crate = new Color(0.58f, 0.44f, 0.26f);
                        var band = new Color(0.36f, 0.28f, 0.18f);
                        FillIconRect(p, s, 0.1f, 0.1f, 0.5f, 0.5f, crate);
                        FillIconRect(p, s, 0.1f, 0.28f, 0.5f, 0.34f, band);
                        FillIconRect(p, s, 0.52f, 0.1f, 0.9f, 0.5f, crate);
                        FillIconRect(p, s, 0.52f, 0.28f, 0.9f, 0.34f, band);
                        FillIconRect(p, s, 0.3f, 0.52f, 0.7f, 0.9f, crate);
                        FillIconRect(p, s, 0.3f, 0.7f, 0.7f, 0.76f, band);
                    });
                case BuildingCategory.Water:
                    return CreateIconSprite("Cat_Water", 64, (p, s) =>
                    {
                        // Two waves under a jetty. The category is a stub in the design and a stub
                        // here: bridge, dock and water mill sit in it until it gets thought through.
                        var water = new Color(0.32f, 0.55f, 0.8f);
                        var deck = new Color(0.5f, 0.38f, 0.24f);
                        FillIconRect(p, s, 0.08f, 0.16f, 0.92f, 0.28f, water);
                        FillIconRect(p, s, 0.08f, 0.36f, 0.92f, 0.48f, water);
                        FillIconRect(p, s, 0.1f, 0.6f, 0.9f, 0.72f, deck);
                        FillIconRect(p, s, 0.22f, 0.48f, 0.3f, 0.6f, deck);
                        FillIconRect(p, s, 0.7f, 0.48f, 0.78f, 0.6f, deck);
                    });
                case BuildingCategory.Entertainment:
                    return CreateIconSprite("Cat_Entertainment", 64, (p, s) =>
                    {
                        // A little tree: the one thing every decoration in this category has in
                        // common is that it is there to look at.
                        FillIconRect(p, s, 0.45f, 0.1f, 0.55f, 0.42f, new Color(0.42f, 0.28f, 0.16f));
                        FillIconTriangle(p, s, new Vector2(0.16f, 0.42f), new Vector2(0.84f, 0.42f), new Vector2(0.5f, 0.92f), new Color(0.32f, 0.6f, 0.28f));
                    });
                default: // Production
                    return CreateIconSprite("Cat_Production", 64, (p, s) =>
                    {
                        FillIconRect(p, s, 0.44f, 0.12f, 0.56f, 0.62f, new Color(0.42f, 0.28f, 0.16f));
                        FillIconRect(p, s, 0.24f, 0.6f, 0.76f, 0.82f, new Color(0.52f, 0.52f, 0.55f));
                    });
            }
        }

        private static Sprite CreateBuildingIcon(string buildingId)
        {
            switch (buildingId)
            {
                case "Hovel":
                    return CreateIconSprite("Bld_Hovel", 64, (p, s) =>
                    {
                        FillIconTriangle(p, s, new Vector2(0.15f, 0.52f), new Vector2(0.85f, 0.52f), new Vector2(0.5f, 0.88f), new Color(0.5f, 0.14f, 0.14f));
                        FillIconRect(p, s, 0.22f, 0.14f, 0.78f, 0.52f, new Color(0.75f, 0.55f, 0.35f));
                        FillIconRect(p, s, 0.44f, 0.14f, 0.56f, 0.36f, new Color(0.32f, 0.18f, 0.09f));
                        FillIconRect(p, s, 0.64f, 0.62f, 0.72f, 0.82f, new Color(0.4f, 0.4f, 0.42f));
                    });
                case "Cottage":
                    return CreateIconSprite("Bld_Cottage", 64, (p, s) =>
                    {
                        FillIconTriangle(p, s, new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.5f), new Vector2(0.5f, 0.86f), new Color(0.35f, 0.2f, 0.4f));
                        FillIconRect(p, s, 0.16f, 0.14f, 0.84f, 0.5f, new Color(0.85f, 0.75f, 0.55f));
                        FillIconRect(p, s, 0.42f, 0.14f, 0.58f, 0.38f, new Color(0.32f, 0.18f, 0.09f));
                        FillIconRect(p, s, 0.22f, 0.28f, 0.32f, 0.4f, new Color(0.4f, 0.55f, 0.65f));
                        FillIconRect(p, s, 0.68f, 0.28f, 0.78f, 0.4f, new Color(0.4f, 0.55f, 0.65f));
                    });
                case "FisherHut":
                    return CreateIconSprite("Bld_FisherHut", 64, (p, s) =>
                    {
                        var fish = new Color(0.3f, 0.52f, 0.68f);
                        FillIconTriangle(p, s, new Vector2(0.15f, 0.5f), new Vector2(0.7f, 0.72f), new Vector2(0.7f, 0.28f), fish);
                        FillIconTriangle(p, s, new Vector2(0.7f, 0.28f), new Vector2(0.7f, 0.72f), new Vector2(0.92f, 0.5f), fish);
                    });
                case "Farm":
                    return CreateIconSprite("Bld_Farm", 64, (p, s) =>
                    {
                        var stem = new Color(0.42f, 0.58f, 0.24f);
                        var grain = new Color(0.86f, 0.68f, 0.24f);
                        FillIconRect(p, s, 0.47f, 0.12f, 0.53f, 0.55f, stem);
                        FillIconTriangle(p, s, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.9f), new Vector2(0.22f, 0.65f), grain);
                        FillIconTriangle(p, s, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.9f), new Vector2(0.78f, 0.65f), grain);
                    });
                case "Sawmill":
                    return CreateIconSprite("Bld_Sawmill", 64, (p, s) =>
                    {
                        FillIconRect(p, s, 0.46f, 0.12f, 0.54f, 0.7f, new Color(0.42f, 0.28f, 0.16f));
                        FillIconTriangle(p, s, new Vector2(0.3f, 0.55f), new Vector2(0.3f, 0.85f), new Vector2(0.62f, 0.7f), new Color(0.55f, 0.55f, 0.58f));
                    });
                case "Quarry":
                    return CreateIconSprite("Bld_Quarry", 64, (p, s) =>
                    {
                        var head = new Color(0.5f, 0.5f, 0.52f);
                        FillIconRect(p, s, 0.46f, 0.12f, 0.54f, 0.62f, new Color(0.42f, 0.28f, 0.16f));
                        FillIconTriangle(p, s, new Vector2(0.5f, 0.7f), new Vector2(0.15f, 0.85f), new Vector2(0.5f, 0.55f), head);
                        FillIconTriangle(p, s, new Vector2(0.5f, 0.7f), new Vector2(0.85f, 0.85f), new Vector2(0.5f, 0.55f), head);
                    });
                case "IronMine":
                    return CreateIconSprite("Bld_IronMine", 64, (p, s) =>
                    {
                        var ore = new Color(0.58f, 0.62f, 0.68f);
                        FillIconRect(p, s, 0.15f, 0.15f, 0.85f, 0.4f, new Color(0.35f, 0.35f, 0.38f));
                        FillIconRect(p, s, 0.25f, 0.4f, 0.45f, 0.58f, ore);
                        FillIconRect(p, s, 0.45f, 0.4f, 0.65f, 0.65f, ore);
                        FillIconRect(p, s, 0.6f, 0.4f, 0.78f, 0.55f, ore);
                        FillIconRect(p, s, 0.2f, 0.08f, 0.32f, 0.15f, new Color(0.15f, 0.15f, 0.15f));
                        FillIconRect(p, s, 0.68f, 0.08f, 0.8f, 0.15f, new Color(0.15f, 0.15f, 0.15f));
                    });
                case "CoalMine":
                    return CreateIconSprite("Bld_CoalMine", 64, (p, s) =>
                    {
                        var coal = new Color(0.12f, 0.12f, 0.13f);
                        FillIconRect(p, s, 0.15f, 0.15f, 0.85f, 0.4f, new Color(0.3f, 0.28f, 0.27f));
                        FillIconRect(p, s, 0.25f, 0.4f, 0.45f, 0.58f, coal);
                        FillIconRect(p, s, 0.45f, 0.4f, 0.65f, 0.65f, coal);
                        FillIconRect(p, s, 0.6f, 0.4f, 0.78f, 0.55f, coal);
                        FillIconRect(p, s, 0.2f, 0.08f, 0.32f, 0.15f, new Color(0.15f, 0.15f, 0.15f));
                        FillIconRect(p, s, 0.68f, 0.08f, 0.8f, 0.15f, new Color(0.15f, 0.15f, 0.15f));
                    });
                case "Warehouse":
                    return CreateIconSprite("Bld_Warehouse", 64, (p, s) =>
                    {
                        var crate = new Color(0.58f, 0.44f, 0.26f);
                        var band = new Color(0.36f, 0.28f, 0.18f);
                        FillIconRect(p, s, 0.12f, 0.12f, 0.88f, 0.7f, crate);
                        FillIconRect(p, s, 0.12f, 0.38f, 0.88f, 0.44f, band);
                        FillIconRect(p, s, 0.46f, 0.12f, 0.54f, 0.7f, band);
                        FillIconTriangle(p, s, new Vector2(0.06f, 0.7f), new Vector2(0.94f, 0.7f), new Vector2(0.5f, 0.94f), band);
                    });
                case "Barn":
                    return CreateIconSprite("Bld_Barn", 64, (p, s) =>
                    {
                        var wall = new Color(0.72f, 0.5f, 0.28f);
                        var grain = new Color(0.88f, 0.74f, 0.34f);
                        FillIconTriangle(p, s, new Vector2(0.1f, 0.62f), new Vector2(0.9f, 0.62f), new Vector2(0.5f, 0.94f), new Color(0.5f, 0.34f, 0.18f));
                        FillIconRect(p, s, 0.16f, 0.1f, 0.84f, 0.64f, wall);
                        FillIconRect(p, s, 0.38f, 0.1f, 0.62f, 0.46f, grain);
                    });
                case "Treasury":
                    return CreateIconSprite("Bld_Treasury", 64, (p, s) =>
                    {
                        var stone = new Color(0.52f, 0.5f, 0.46f);
                        var gold = new Color(0.85f, 0.7f, 0.24f);
                        FillIconRect(p, s, 0.12f, 0.1f, 0.88f, 0.66f, stone);
                        FillIconRect(p, s, 0.12f, 0.66f, 0.88f, 0.76f, new Color(0.36f, 0.34f, 0.32f));
                        FillIconRect(p, s, 0.3f, 0.28f, 0.7f, 0.5f, gold);
                        FillIconRect(p, s, 0.44f, 0.18f, 0.56f, 0.3f, new Color(0.3f, 0.28f, 0.26f));
                    });
                // The second tier of each storehouse: the same pictogram, doubled up, so the pair
                // reads as "more of the same" in the hotbar rather than as two unrelated buildings.
                case "BigWarehouse":
                    return CreateIconSprite("Bld_BigWarehouse", 64, (p, s) =>
                    {
                        var crate = new Color(0.58f, 0.44f, 0.26f);
                        var band = new Color(0.36f, 0.28f, 0.18f);
                        FillIconRect(p, s, 0.06f, 0.08f, 0.52f, 0.6f, crate);
                        FillIconRect(p, s, 0.06f, 0.3f, 0.52f, 0.36f, band);
                        FillIconRect(p, s, 0.48f, 0.24f, 0.94f, 0.82f, crate);
                        FillIconRect(p, s, 0.48f, 0.5f, 0.94f, 0.56f, band);
                        FillIconTriangle(p, s, new Vector2(0.42f, 0.82f), new Vector2(1f, 0.82f), new Vector2(0.71f, 0.98f), band);
                    });
                case "BigBarn":
                    return CreateIconSprite("Bld_BigBarn", 64, (p, s) =>
                    {
                        var wall = new Color(0.72f, 0.5f, 0.28f);
                        var grain = new Color(0.88f, 0.74f, 0.34f);
                        var roof = new Color(0.5f, 0.34f, 0.18f);
                        FillIconTriangle(p, s, new Vector2(0.02f, 0.5f), new Vector2(0.56f, 0.5f), new Vector2(0.29f, 0.74f), roof);
                        FillIconRect(p, s, 0.06f, 0.06f, 0.52f, 0.52f, wall);
                        FillIconTriangle(p, s, new Vector2(0.44f, 0.66f), new Vector2(0.98f, 0.66f), new Vector2(0.71f, 0.94f), roof);
                        FillIconRect(p, s, 0.48f, 0.22f, 0.94f, 0.68f, wall);
                        FillIconRect(p, s, 0.62f, 0.22f, 0.8f, 0.5f, grain);
                    });
                case "BigTreasury":
                    return CreateIconSprite("Bld_BigTreasury", 64, (p, s) =>
                    {
                        var stone = new Color(0.52f, 0.5f, 0.46f);
                        var gold = new Color(0.85f, 0.7f, 0.24f);
                        FillIconRect(p, s, 0.06f, 0.06f, 0.52f, 0.54f, stone);
                        FillIconRect(p, s, 0.06f, 0.54f, 0.52f, 0.62f, new Color(0.36f, 0.34f, 0.32f));
                        FillIconRect(p, s, 0.48f, 0.2f, 0.94f, 0.76f, stone);
                        FillIconRect(p, s, 0.48f, 0.76f, 0.94f, 0.84f, new Color(0.36f, 0.34f, 0.32f));
                        FillIconRect(p, s, 0.6f, 0.38f, 0.82f, 0.6f, gold);
                    });
                case "Fence":
                    return CreateIconSprite("Bld_Fence", 64, (p, s) =>
                    {
                        // Two posts and two rails, read as a wooden fence rather than the stone
                        // courses the Wall icon uses.
                        var post = new Color(0.45f, 0.3f, 0.16f);
                        var rail = new Color(0.62f, 0.45f, 0.25f);
                        FillIconRect(p, s, 0.12f, 0.46f, 0.88f, 0.58f, rail);
                        FillIconRect(p, s, 0.12f, 0.24f, 0.88f, 0.36f, rail);
                        FillIconRect(p, s, 0.2f, 0.12f, 0.34f, 0.78f, post);
                        FillIconRect(p, s, 0.66f, 0.12f, 0.8f, 0.78f, post);
                    });
                case "Tower":
                    return CreateIconSprite("Bld_Tower", 64, (p, s) =>
                    {
                        var stone = new Color(0.4f, 0.38f, 0.34f);
                        FillIconRect(p, s, 0.28f, 0.1f, 0.72f, 0.8f, stone);
                        FillIconRect(p, s, 0.22f, 0.8f, 0.36f, 0.92f, stone);
                        FillIconRect(p, s, 0.44f, 0.8f, 0.56f, 0.92f, stone);
                        FillIconRect(p, s, 0.64f, 0.8f, 0.78f, 0.92f, stone);
                    });
                case "Barracks":
                    return CreateIconSprite("Bld_Barracks", 64, (p, s) =>
                    {
                        FillIconRect(p, s, 0.16f, 0.12f, 0.84f, 0.55f, new Color(0.5f, 0.48f, 0.44f));
                        FillIconTriangle(p, s, new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.55f), new Vector2(0.5f, 0.8f), new Color(0.35f, 0.3f, 0.26f));
                        FillIconRect(p, s, 0.48f, 0.8f, 0.52f, 0.95f, new Color(0.3f, 0.2f, 0.1f));
                        FillIconRect(p, s, 0.52f, 0.85f, 0.72f, 0.95f, new Color(0.75f, 0.18f, 0.16f));
                    });
                case "Gate":
                    return CreateIconSprite("Bld_Gate", 64, (p, s) =>
                    {
                        var stone = new Color(0.4f, 0.38f, 0.34f);
                        FillIconRect(p, s, 0.14f, 0.12f, 0.32f, 0.85f, stone);
                        FillIconRect(p, s, 0.68f, 0.12f, 0.86f, 0.85f, stone);
                        FillIconRect(p, s, 0.14f, 0.78f, 0.86f, 0.9f, stone);
                    });
                // The fortified pair: the same silhouettes in darker stone, with the battlements
                // filled in solid, so the tier reads at hotbar size without a second look.
                case "FortifiedTower":
                    return CreateIconSprite("Bld_FortifiedTower", 64, (p, s) =>
                    {
                        var stone = new Color(0.3f, 0.29f, 0.27f);
                        var band = new Color(0.52f, 0.5f, 0.46f);
                        FillIconRect(p, s, 0.24f, 0.08f, 0.76f, 0.8f, stone);
                        FillIconRect(p, s, 0.24f, 0.44f, 0.76f, 0.52f, band);
                        FillIconRect(p, s, 0.18f, 0.8f, 0.34f, 0.94f, stone);
                        FillIconRect(p, s, 0.42f, 0.8f, 0.58f, 0.94f, stone);
                        FillIconRect(p, s, 0.66f, 0.8f, 0.82f, 0.94f, stone);
                    });
                case "FortifiedGate":
                    return CreateIconSprite("Bld_FortifiedGate", 64, (p, s) =>
                    {
                        var stone = new Color(0.3f, 0.29f, 0.27f);
                        var band = new Color(0.52f, 0.5f, 0.46f);
                        FillIconRect(p, s, 0.1f, 0.1f, 0.34f, 0.86f, stone);
                        FillIconRect(p, s, 0.66f, 0.1f, 0.9f, 0.86f, stone);
                        FillIconRect(p, s, 0.1f, 0.76f, 0.9f, 0.92f, stone);
                        FillIconRect(p, s, 0.34f, 0.1f, 0.66f, 0.2f, band);
                    });
                case "Road":
                    return CreateIconSprite("Bld_Road", 64, (p, s) =>
                    {
                        FillIconRect(p, s, 0.1f, 0.1f, 0.9f, 0.9f, new Color(0.35f, 0.35f, 0.36f));
                        var stripe = new Color(0.85f, 0.75f, 0.3f);
                        FillIconRect(p, s, 0.46f, 0.14f, 0.54f, 0.32f, stripe);
                        FillIconRect(p, s, 0.46f, 0.42f, 0.54f, 0.6f, stripe);
                        FillIconRect(p, s, 0.46f, 0.7f, 0.54f, 0.86f, stripe);
                    });
                case "Bridge":
                    return CreateIconSprite("Bld_Bridge", 64, (p, s) =>
                    {
                        var water = new Color(0.3f, 0.5f, 0.75f);
                        var plank = new Color(0.5f, 0.35f, 0.2f);
                        FillIconRect(p, s, 0.05f, 0.1f, 0.95f, 0.9f, water);
                        FillIconRect(p, s, 0.1f, 0.42f, 0.9f, 0.58f, plank);
                        FillIconRect(p, s, 0.16f, 0.58f, 0.22f, 0.72f, plank);
                        FillIconRect(p, s, 0.78f, 0.58f, 0.84f, 0.72f, plank);
                        FillIconRect(p, s, 0.16f, 0.28f, 0.22f, 0.42f, plank);
                        FillIconRect(p, s, 0.78f, 0.28f, 0.84f, 0.42f, plank);
                    });
                case "WaterMill":
                    return CreateIconSprite("Bld_WaterMill", 64, (p, s) =>
                    {
                        var wall = new Color(0.5f, 0.4f, 0.28f);
                        var roof = new Color(0.32f, 0.22f, 0.15f);
                        var wheel = new Color(0.35f, 0.24f, 0.14f);
                        FillIconRect(p, s, 0.14f, 0.16f, 0.56f, 0.6f, wall);
                        FillIconTriangle(p, s, new Vector2(0.1f, 0.6f), new Vector2(0.6f, 0.6f), new Vector2(0.35f, 0.82f), roof);
                        FillIconRect(p, s, 0.6f, 0.22f, 0.9f, 0.72f, wheel);
                        FillIconRect(p, s, 0.68f, 0.14f, 0.82f, 0.8f, wheel);
                    });
                case "Dock":
                    return CreateIconSprite("Bld_Dock", 64, (p, s) =>
                    {
                        var water = new Color(0.3f, 0.5f, 0.75f);
                        var deck = new Color(0.55f, 0.4f, 0.24f);
                        var crate = new Color(0.68f, 0.5f, 0.3f);
                        FillIconRect(p, s, 0.05f, 0.1f, 0.95f, 0.5f, water);
                        FillIconRect(p, s, 0.1f, 0.44f, 0.9f, 0.58f, deck);
                        FillIconRect(p, s, 0.2f, 0.58f, 0.42f, 0.8f, crate);
                        FillIconRect(p, s, 0.5f, 0.58f, 0.68f, 0.74f, crate);
                    });
                default:
                    return CreateIconSprite($"Bld_{buildingId}", 64, (p, s) =>
                    {
                        FillIconRect(p, s, 0.25f, 0.25f, 0.75f, 0.75f, new Color(0.6f, 0.6f, 0.6f));
                    });
            }
        }

        /// <summary>One stacked ingot, tinted per metal -- the three bars differ only in colour, which is exactly how a player tells them apart at icon size.</summary>
        private static Sprite CreateBarIcon(string name, Color metal, Color shine)
        {
            return CreateIconSprite(name, 64, (p, s) =>
            {
                FillIconRect(p, s, 0.14f, 0.26f, 0.86f, 0.54f, metal);
                FillIconRect(p, s, 0.24f, 0.54f, 0.76f, 0.7f, metal);
                FillIconRect(p, s, 0.2f, 0.32f, 0.66f, 0.38f, shine);
            });
        }

        /// <summary>Small pictogram per ResourceType, used by the top resource HUD bar in place of plain-text resource names (see ResourceHUDController/BuildResourceHUD).</summary>
        private static Sprite CreateResourceIcon(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Wood:
                    return CreateIconSprite("Res_Wood", 64, (p, s) =>
                    {
                        var log = new Color(0.55f, 0.36f, 0.2f);
                        var ring = new Color(0.78f, 0.6f, 0.4f);
                        FillIconRect(p, s, 0.12f, 0.36f, 0.88f, 0.64f, log);
                        FillIconRect(p, s, 0.12f, 0.36f, 0.24f, 0.64f, ring);
                        FillIconRect(p, s, 0.76f, 0.36f, 0.88f, 0.64f, ring);
                        FillIconRect(p, s, 0.16f, 0.44f, 0.2f, 0.56f, log);
                        FillIconRect(p, s, 0.8f, 0.44f, 0.84f, 0.56f, log);
                    });
                case ResourceType.Stone:
                    return CreateIconSprite("Res_Stone", 64, (p, s) =>
                    {
                        var stone = new Color(0.58f, 0.56f, 0.52f);
                        var shade = new Color(0.42f, 0.4f, 0.37f);
                        FillIconTriangle(p, s, new Vector2(0.12f, 0.2f), new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.2f), stone);
                        FillIconTriangle(p, s, new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.78f), new Vector2(0.88f, 0.2f), shade);
                    });
                case ResourceType.Iron:
                    return CreateIconSprite("Res_Iron", 64, (p, s) =>
                    {
                        var ingot = new Color(0.62f, 0.65f, 0.7f);
                        var shine = new Color(0.82f, 0.85f, 0.88f);
                        FillIconRect(p, s, 0.14f, 0.3f, 0.86f, 0.58f, ingot);
                        FillIconRect(p, s, 0.22f, 0.58f, 0.78f, 0.68f, ingot);
                        FillIconRect(p, s, 0.2f, 0.36f, 0.32f, 0.52f, shine);
                    });
                case ResourceType.Coal:
                    return CreateIconSprite("Res_Coal", 64, (p, s) =>
                    {
                        var coal = new Color(0.14f, 0.14f, 0.15f);
                        var shine = new Color(0.34f, 0.34f, 0.36f);
                        FillIconRect(p, s, 0.18f, 0.18f, 0.5f, 0.5f, coal);
                        FillIconRect(p, s, 0.42f, 0.3f, 0.82f, 0.62f, coal);
                        FillIconRect(p, s, 0.22f, 0.55f, 0.5f, 0.82f, coal);
                        FillIconRect(p, s, 0.48f, 0.4f, 0.56f, 0.48f, shine);
                    });
                case ResourceType.Coins:
                    // A stack of square coins (not circular discs -- straight edges only, same
                    // "no circles" rule every other icon in this project follows) in a brighter,
                    // lighter yellow than the Gold bar's more amber tone, so the two read as
                    // visually distinct currencies at a glance.
                    return CreateIconSprite("Res_Coins", 64, (p, s) =>
                    {
                        var coin = new Color(0.95f, 0.82f, 0.3f);
                        var edge = new Color(0.7f, 0.58f, 0.16f);
                        FillIconRect(p, s, 0.18f, 0.14f, 0.7f, 0.32f, edge);
                        FillIconRect(p, s, 0.2f, 0.16f, 0.68f, 0.28f, coin);
                        FillIconRect(p, s, 0.26f, 0.34f, 0.78f, 0.52f, edge);
                        FillIconRect(p, s, 0.28f, 0.36f, 0.76f, 0.48f, coin);
                        FillIconRect(p, s, 0.34f, 0.54f, 0.86f, 0.72f, edge);
                        FillIconRect(p, s, 0.36f, 0.56f, 0.84f, 0.68f, coin);
                    });
                case ResourceType.CopperOre:
                    // Ore, not metal: rough lumps with a copper tint, matching the Stone icon's
                    // silhouette so the three ores read as a family.
                    return CreateIconSprite("Res_CopperOre", 64, (p, s) =>
                    {
                        var rock = new Color(0.48f, 0.44f, 0.4f);
                        var ore = new Color(0.78f, 0.45f, 0.24f);
                        FillIconRect(p, s, 0.16f, 0.2f, 0.84f, 0.6f, rock);
                        FillIconRect(p, s, 0.28f, 0.6f, 0.66f, 0.72f, rock);
                        FillIconRect(p, s, 0.26f, 0.3f, 0.4f, 0.44f, ore);
                        FillIconRect(p, s, 0.54f, 0.38f, 0.7f, 0.52f, ore);
                    });
                case ResourceType.IronBar:
                    return CreateBarIcon("Res_IronBar", new Color(0.62f, 0.65f, 0.7f), new Color(0.85f, 0.88f, 0.92f));
                case ResourceType.CopperBar:
                    return CreateBarIcon("Res_CopperBar", new Color(0.78f, 0.45f, 0.24f), new Color(0.93f, 0.66f, 0.42f));
                case ResourceType.GoldBar:
                    return CreateBarIcon("Res_GoldBar", new Color(0.85f, 0.68f, 0.24f), new Color(0.97f, 0.88f, 0.55f));
                case ResourceType.Grain:
                    // A sheaf: three upright stalks with the ears blocked in above them. Straight
                    // edges only, same as every other icon here.
                    return CreateIconSprite("Res_Grain", 64, (p, s) =>
                    {
                        var straw = new Color(0.85f, 0.7f, 0.3f);
                        var ear = new Color(0.95f, 0.83f, 0.45f);
                        FillIconRect(p, s, 0.22f, 0.12f, 0.3f, 0.6f, straw);
                        FillIconRect(p, s, 0.46f, 0.12f, 0.54f, 0.7f, straw);
                        FillIconRect(p, s, 0.7f, 0.12f, 0.78f, 0.6f, straw);
                        FillIconRect(p, s, 0.18f, 0.56f, 0.34f, 0.78f, ear);
                        FillIconRect(p, s, 0.42f, 0.66f, 0.58f, 0.88f, ear);
                        FillIconRect(p, s, 0.66f, 0.56f, 0.82f, 0.78f, ear);
                        FillIconRect(p, s, 0.14f, 0.24f, 0.86f, 0.32f, ear);
                    });
                case ResourceType.Flour:
                    // A sack, tied at the neck, with a spill of white in front of it -- flour is
                    // otherwise the same pale colour as nothing at all.
                    return CreateIconSprite("Res_Flour", 64, (p, s) =>
                    {
                        var sack = new Color(0.82f, 0.76f, 0.62f);
                        var tie = new Color(0.6f, 0.52f, 0.38f);
                        var powder = new Color(0.96f, 0.95f, 0.92f);
                        FillIconRect(p, s, 0.2f, 0.16f, 0.7f, 0.68f, sack);
                        FillIconRect(p, s, 0.3f, 0.68f, 0.6f, 0.78f, tie);
                        FillIconRect(p, s, 0.26f, 0.78f, 0.64f, 0.86f, sack);
                        FillIconRect(p, s, 0.62f, 0.16f, 0.88f, 0.24f, powder);
                        FillIconRect(p, s, 0.7f, 0.24f, 0.84f, 0.3f, powder);
                    });
                case ResourceType.Bread:
                    // A loaf: a darker crust with the slashes cut across its top.
                    return CreateIconSprite("Res_Bread", 64, (p, s) =>
                    {
                        var crust = new Color(0.72f, 0.48f, 0.24f);
                        var crumb = new Color(0.88f, 0.68f, 0.4f);
                        FillIconRect(p, s, 0.12f, 0.26f, 0.88f, 0.66f, crust);
                        FillIconRect(p, s, 0.2f, 0.66f, 0.8f, 0.74f, crust);
                        FillIconRect(p, s, 0.28f, 0.58f, 0.36f, 0.7f, crumb);
                        FillIconRect(p, s, 0.46f, 0.58f, 0.54f, 0.72f, crumb);
                        FillIconRect(p, s, 0.64f, 0.58f, 0.72f, 0.7f, crumb);
                    });
                case ResourceType.Food:
                    // Everything edible that isn't bread yet: fish, fruit, meat. One generic
                    // basket rather than an attempt to draw all three.
                    return CreateIconSprite("Res_Food", 64, (p, s) =>
                    {
                        var basket = new Color(0.6f, 0.42f, 0.24f);
                        var weave = new Color(0.75f, 0.56f, 0.34f);
                        var produce = new Color(0.72f, 0.24f, 0.22f);
                        var leaf = new Color(0.35f, 0.6f, 0.28f);
                        FillIconRect(p, s, 0.32f, 0.6f, 0.52f, 0.8f, produce);
                        FillIconRect(p, s, 0.54f, 0.62f, 0.7f, 0.78f, leaf);
                        FillIconRect(p, s, 0.16f, 0.2f, 0.84f, 0.62f, basket);
                        FillIconRect(p, s, 0.16f, 0.38f, 0.84f, 0.44f, weave);
                    });
                default: // Gold
                    return CreateIconSprite("Res_Gold", 64, (p, s) =>
                    {
                        var bar = new Color(0.85f, 0.68f, 0.24f);
                        var shine = new Color(0.95f, 0.86f, 0.5f);
                        FillIconRect(p, s, 0.14f, 0.28f, 0.86f, 0.58f, bar);
                        FillIconRect(p, s, 0.22f, 0.58f, 0.78f, 0.7f, bar);
                        FillIconRect(p, s, 0.2f, 0.34f, 0.68f, 0.4f, shine);
                    });
            }
        }

        /// <summary>
        /// Runtime-queryable counterpart to CreateResourceIcon/CreatePopulationIcon -- those two
        /// only ever get called from Editor code (this script), but BuildingInfoPanelController's
        /// upgrade/repair cost chips need the same sprites at play time, so they're captured into
        /// a saved ScriptableObject asset indexed by ResourceType (see ResourceIconLibrary).
        /// </summary>
        private static ResourceIconLibrary CreateResourceIconLibrary()
        {
            // Sized off the enum rather than a literal: this array used to be `new Sprite[8]` and
            // silently stopped covering the enum the moment a resource was added -- the library's
            // bounds check then answered "no icon" for it, which looks exactly like a drawing that
            // was never written.
            // (System.Enum spelled out: a plain `using System;` here would make the file's bare
            // `Random.Range` calls ambiguous with System.Random.)
            var icons = new Sprite[System.Enum.GetValues(typeof(ResourceType)).Length];
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                icons[(int)type] = type == ResourceType.Population
                    ? CreatePopulationIcon()
                    : CreateResourceIcon(type);
            }

            var library = ScriptableObject.CreateInstance<ResourceIconLibrary>();
            library.EditorInitialize(icons);

            Directory.CreateDirectory(ResourceScriptableObjectsFolder);
            var path = $"{ResourceScriptableObjectsFolder}/ResourceIconLibrary.asset";
            DeleteIfExists(path);
            AssetDatabase.CreateAsset(library, path);
            return library;
        }

        private static Sprite CreatePopulationIcon()
        {
            return CreateIconSprite("Res_Population", 64, (p, s) =>
            {
                var body = new Color(0.85f, 0.82f, 0.75f);
                FillIconRect(p, s, 0.38f, 0.56f, 0.62f, 0.74f, body);
                FillIconTriangle(p, s, new Vector2(0.2f, 0.1f), new Vector2(0.8f, 0.1f), new Vector2(0.5f, 0.54f), body);
            });
        }

        /// <summary>An open square bracket (three sides) with an arrowhead where the fourth side would start -- reads as "turn/cycle" without needing a curved stroke (straight-edges-only, matching every other icon here).</summary>
        private static Sprite CreateRotateIcon()
        {
            return CreateIconSprite("Action_Rotate", 64, (p, s) =>
            {
                var line = new Color(0.92f, 0.92f, 0.9f);
                FillIconRect(p, s, 0.2f, 0.78f, 0.8f, 0.86f, line);
                FillIconRect(p, s, 0.72f, 0.24f, 0.8f, 0.86f, line);
                FillIconRect(p, s, 0.2f, 0.24f, 0.8f, 0.32f, line);
                FillIconTriangle(p, s, new Vector2(0.06f, 0.44f), new Vector2(0.28f, 0.56f), new Vector2(0.28f, 0.32f), line);
            });
        }

        /// <summary>A tick, drawn as two thick strokes -- the short one down-right, the long one up-right. Straight edges only, like every other icon here.</summary>
        private static Sprite CreateConfirmIcon()
        {
            return CreateIconSprite("Action_Confirm", 64, (p, s) =>
            {
                var line = new Color(0.82f, 0.98f, 0.82f);
                const int halfThickness = 3;

                // Short stroke: down from the left toward the elbow.
                var elbow = new Vector2Int(Mathf.RoundToInt(s * 0.42f), Mathf.RoundToInt(s * 0.28f));
                var start = new Vector2Int(Mathf.RoundToInt(s * 0.18f), Mathf.RoundToInt(s * 0.52f));
                var end = new Vector2Int(Mathf.RoundToInt(s * 0.84f), Mathf.RoundToInt(s * 0.78f));

                StrokeIconLine(p, s, start, elbow, halfThickness, line);
                StrokeIconLine(p, s, elbow, end, halfThickness, line);
            });
        }

        /// <summary>A straight line between two pixel positions, thickened square. Used by the tick; kept general because every icon here is drawn from straight strokes.</summary>
        private static void StrokeIconLine(Color[] pixels, int size, Vector2Int from, Vector2Int to, int halfThickness, Color color)
        {
            var steps = Mathf.Max(Mathf.Abs(to.x - from.x), Mathf.Abs(to.y - from.y));
            if (steps <= 0) return;

            for (var i = 0; i <= steps; i++)
            {
                var t = i / (float)steps;
                var cx = Mathf.RoundToInt(Mathf.Lerp(from.x, to.x, t));
                var cy = Mathf.RoundToInt(Mathf.Lerp(from.y, to.y, t));

                for (var dy = -halfThickness; dy <= halfThickness; dy++)
                {
                    for (var dx = -halfThickness; dx <= halfThickness; dx++)
                    {
                        var x = cx + dx;
                        var y = cy + dy;
                        if (x < 0 || x >= size || y < 0 || y >= size) continue;
                        pixels[y * size + x] = color;
                    }
                }
            }
        }

        /// <summary>A plain X, drawn as two thick diagonals. Deliberately not a "back" arrow: this abandons the placement, it does not step anywhere.</summary>
        private static Sprite CreateCancelIcon()
        {
            return CreateIconSprite("Action_Cancel", 64, (p, s) =>
            {
                var line = new Color(0.95f, 0.72f, 0.68f);
                var margin = Mathf.RoundToInt(s * 0.18f);
                // Thick enough to survive being scaled down to a phone-sized button.
                const int halfThickness = 3;

                for (var i = margin; i < s - margin; i++)
                {
                    for (var offset = -halfThickness; offset <= halfThickness; offset++)
                    {
                        var x = i + offset;
                        if (x < 0 || x >= s) continue;
                        p[i * s + x] = line;
                        p[(s - 1 - i) * s + x] = line;
                    }
                }
            });
        }

        private static Button CreateIconButton(Transform parent, Sprite backgroundSprite, Sprite iconSprite, string name, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            var background = go.GetComponent<Image>();
            background.sprite = backgroundSprite;
            background.type = Image.Type.Sliced;
            background.color = new Color(0.26f, 0.29f, 0.24f, 0.95f);

            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(go.transform, false);
            var iconRect = iconGO.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            var pad = Mathf.Min(sizeDelta.x, sizeDelta.y) * 0.16f;
            iconRect.offsetMin = new Vector2(pad, pad);
            iconRect.offsetMax = new Vector2(-pad, -pad);
            var iconImage = iconGO.GetComponent<Image>();
            iconImage.sprite = iconSprite;
            iconImage.preserveAspect = true;

            return go.GetComponent<Button>();
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go.GetComponent<Image>();
        }

        /// <summary>Empty horizontal row for BuildingInfoPanelController to fill with icon+number cost chips at runtime (see its BuildCostRow) -- the layout group handles spacing/centering, so runtime code only ever adds/removes leaf children.</summary>
        private static Transform CreateCostRow(Transform parent, string name, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(640f, 44f);

            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 18f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            return go.transform;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static InputField CreateInputField(Transform parent, string name, string placeholderText, Vector2 anchoredPos, Vector2 sizeDelta, Sprite backgroundSprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            var bgImage = go.GetComponent<Image>();
            bgImage.sprite = backgroundSprite;
            bgImage.type = Image.Type.Sliced;
            bgImage.color = Color.white;

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(go.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20f, 6f);
            textRect.offsetMax = new Vector2(-20f, -6f);
            var textComp = textGO.GetComponent<Text>();
            textComp.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComp.fontSize = 30;
            textComp.alignment = TextAnchor.MiddleLeft;
            textComp.color = Color.black;
            textComp.supportRichText = false;

            var placeholderGO = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            placeholderGO.transform.SetParent(go.transform, false);
            var placeholderRect = placeholderGO.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(20f, 6f);
            placeholderRect.offsetMax = new Vector2(-20f, -6f);
            var placeholderComp = placeholderGO.GetComponent<Text>();
            placeholderComp.font = textComp.font;
            placeholderComp.fontSize = 30;
            placeholderComp.alignment = TextAnchor.MiddleLeft;
            placeholderComp.color = new Color(0f, 0f, 0f, 0.4f);
            placeholderComp.text = placeholderText;
            placeholderComp.fontStyle = FontStyle.Italic;

            var inputField = go.GetComponent<InputField>();
            inputField.textComponent = textComp;
            inputField.placeholder = placeholderComp;
            inputField.characterLimit = 24;

            return inputField;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, Vector2 anchoredPos, Vector2 sizeDelta, Color? color = null, bool addShadow = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color ?? Color.white;

            // Only for standalone display text (e.g. the menu title) -- never on button labels,
            // which stay flat against their own carved-stone panel background.
            if (addShadow)
            {
                var shadow = go.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
                shadow.effectDistance = new Vector2(3f, -3f);
            }

            return text;
        }

        /// <summary>Hangs a LocalizedText on a fixed caption so it fills itself in and follows a language change. Returns the label, so it can wrap a CreateText call inline.</summary>
        private static Text Localize(Text label, string key)
        {
            label.gameObject.AddComponent<LocalizedText>().SetKey(key);
            return label;
        }

        /// <summary>
        /// A button whose caption comes from the localization sheet: `labelKey` is a key, not text.
        /// The label carries a LocalizedText, so it re-reads itself when the player switches
        /// language without anything having to hunt down every button in the scene.
        /// </summary>
        private static Button CreateButton(Transform parent, Sprite sprite, string name, string labelKey, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.26f, 0.29f, 0.24f, 0.95f);

            Localize(CreateText(go.transform, "Label", string.Empty, 28, Vector2.zero, sizeDelta - new Vector2(20f, 20f)), labelKey);
            return go.GetComponent<Button>();
        }

        /// <summary>
        /// A button whose caption its controller writes at play time. Deliberately NOT CreateButton:
        /// that hangs a LocalizedText on the label, which refills it from a fixed key and would wipe
        /// out a caption carrying a number in it (the cancel button's refund).
        /// </summary>
        private static Button CreateDynamicButton(Transform parent, Sprite sprite, string name, Vector2 anchoredPos, Vector2 sizeDelta, out Text label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.26f, 0.29f, 0.24f, 0.95f);

            label = CreateText(go.transform, "Label", string.Empty, 24, Vector2.zero, sizeDelta - new Vector2(20f, 20f));
            return go.GetComponent<Button>();
        }

        private static void DeleteIfExists(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
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
