using System.Collections.Generic;
using System.IO;
using CityBuilder.Buildings;
using CityBuilder.CameraControl;
using CityBuilder.Citizens;
using CityBuilder.Grid;
using CityBuilder.Maps;
using CityBuilder.Resources;
using CityBuilder.Saving;
using CityBuilder.UI;
using UnityEditor;
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
    public static class SetupProject
    {
        private const string ScenesFolder = "Assets/_Project/Scenes";
        private const string MaterialsFolder = "Assets/_Project/Materials";
        private const string TexturesFolder = "Assets/_Project/Textures";
        private const string BuildingPrefabsFolder = "Assets/_Project/Prefabs/Buildings";
        private const string BuildingDataFolder = "Assets/_Project/ScriptableObjects/Buildings";
        private const string MeshMapsFolder = "Assets/_Project/Resources/MeshMaps";
        private const string ModelsMap1Folder = "Assets/_Project/Models/Map1";
        private const string ModelsTerrainFolder = "Assets/_Project/Models/Terrain";
        private const string ModelsBuildingsFolder = "Assets/_Project/Models/Buildings";

        private const int GridCellsX = 200;
        private const int GridCellsZ = 200;
        private const int ForestMarginCells = 20; // same 20m world-space margin as before the cellSize halved
        private const float CellSize = 1f;
        private const float BuildingInset = 0.08f;
        private const float GroundHeight = 0f;
        private static readonly Vector3 GroundOrigin = new Vector3(-GridCellsX * CellSize * 0.5f, 0f, -GridCellsZ * CellSize * 0.5f);

        [MenuItem("CityBuilder/Setup Project (Scenes + Prefabs)")]
        public static void Run()
        {
            BuildMainMenuScene();
            BuildCityScene();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene($"{ScenesFolder}/MainMenu.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/CityBuilder.unity", true),
            };

            CleanupTemplateAssets();

            // Applied last: EditorSceneManager.NewScene() calls earlier in this method (in
            // BuildMainMenuScene/BuildCityScene) were observed to revert PlayerSettings changes
            // made beforehand, so nothing here can precede a scene switch.
            ConfigureMobileLandscape();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
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
            menuCamera.backgroundColor = new Color(0.08f, 0.11f, 0.14f);
            menuCamera.cullingMask = 0;
            menuCamera.orthographic = true;

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

            CreateText(canvasGO.transform, "Title", "СТРОИТЕЛЬ ГОРОДОВ", 72, new Vector2(0f, 260f), new Vector2(1200f, 140f));
            CreateText(canvasGO.transform, "Subtitle", "мобильный градостроитель", 26, new Vector2(0f, 175f), new Vector2(900f, 60f), new Color(1f, 1f, 1f, 0.6f));

            var menuController = canvasGO.AddComponent<MainMenuController>();

            var newGameButton = CreateButton(canvasGO.transform, panelSprite, "NewGameButton", "Новая игра", new Vector2(0f, 60f), new Vector2(460f, 100f));
            UnityEventTools.AddPersistentListener(newGameButton.onClick, menuController.StartNewGame);

            var loadGameButton = CreateButton(canvasGO.transform, panelSprite, "LoadGameButton", "Загрузить игру", new Vector2(0f, -64f), new Vector2(460f, 100f));
            UnityEventTools.AddPersistentListener(loadGameButton.onClick, menuController.LoadGame);

            var quitButton = CreateButton(canvasGO.transform, panelSprite, "QuitButton", "Выход", new Vector2(0f, -188f), new Vector2(460f, 100f));
            UnityEventTools.AddPersistentListener(quitButton.onClick, menuController.QuitGame);

            var menuControllerSO = new SerializedObject(menuController);
            menuControllerSO.FindProperty("loadGameButton").objectReferenceValue = loadGameButton;
            menuControllerSO.ApplyModifiedPropertiesWithoutUndo();

            BuildLoadPanel(canvasGO.transform, panelSprite, menuController);

            Directory.CreateDirectory(ScenesFolder);
            DeleteIfExists($"{ScenesFolder}/MainMenu.unity");
            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/MainMenu.unity");
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

            var houseData = CreateBuildingData(
                "House", "Дом", new Vector2Int(1, 1), height: 2f,
                wallColor: new Color(0.75f, 0.55f, 0.35f), roofColor: new Color(0.25f, 0.45f, 0.65f),
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Wood, amount = 10 } },
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                hasChimney: true, citizensGranted: 5, category: BuildingCategory.Housing, maxHealth: 80);

            var cottageData = CreateBuildingData(
                "Cottage", "Коттедж", new Vector2Int(1, 1), height: 2.3f,
                wallColor: new Color(0.62f, 0.42f, 0.55f), roofColor: new Color(0.3f, 0.2f, 0.4f),
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Wood, amount = 25 }, new ResourceAmount { type = ResourceType.Stone, amount = 8 } },
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                hasChimney: true, citizensGranted: 8, category: BuildingCategory.Housing, maxHealth: 120);

            var townHallData = CreateFbxBuildingData(
                "TownHall", "Ратуша", new Vector2Int(4, 4), height: 3f,
                fbxFileName: "MainCastle-1.fbx",
                cost: new List<ResourceAmount>(),
                citizensGranted: 5, maxHealth: 400, defense: 20,
                upgradeToLevel2Cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Wood, amount = 100 }, new ResourceAmount { type = ResourceType.Stone, amount = 60 } },
                upgradeToLevel3Cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Wood, amount = 220 }, new ResourceAmount { type = ResourceType.Stone, amount = 150 }, new ResourceAmount { type = ResourceType.Gold, amount = 40 } });

            var fishermanHutData = CreateBuildingData(
                "FishermanHut", "Хижина рыбака", new Vector2Int(2, 1), height: 2f,
                wallColor: new Color(0.55f, 0.52f, 0.45f), roofColor: new Color(0.2f, 0.5f, 0.55f),
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Wood, amount = 15 } },
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                hasChimney: true, maxWorkers: 2, producesResource: ResourceType.Food, productionPerTick: 2, category: BuildingCategory.Food, maxHealth: 90);

            var hunterHutData = CreateBuildingData(
                "HunterHut", "Хижина охотника", new Vector2Int(2, 1), height: 2f,
                wallColor: new Color(0.38f, 0.28f, 0.18f), roofColor: new Color(0.22f, 0.42f, 0.24f),
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Wood, amount = 15 }, new ResourceAmount { type = ResourceType.Stone, amount = 5 } },
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                hasChimney: true, maxWorkers: 2, producesResource: ResourceType.Food, productionPerTick: 2, category: BuildingCategory.Food, maxHealth: 90);

            var farmData = CreateBuildingData(
                "Farm", "Ферма", new Vector2Int(2, 2), height: 1.8f,
                wallColor: new Color(0.68f, 0.55f, 0.3f), roofColor: new Color(0.42f, 0.58f, 0.24f),
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Wood, amount = 15 }, new ResourceAmount { type = ResourceType.Stone, amount = 5 } },
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                maxWorkers: 3, producesResource: ResourceType.Food, productionPerTick: 3, category: BuildingCategory.Food, maxHealth: 70);

            var lumberjackData = CreateBuildingData(
                "Lumberjack", "Лесопилка", new Vector2Int(2, 2), height: 2.4f,
                wallColor: new Color(0.45f, 0.3f, 0.18f), roofColor: new Color(0.32f, 0.22f, 0.13f),
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Wood, amount = 20 }, new ResourceAmount { type = ResourceType.Stone, amount = 5 } },
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                maxWorkers: 3, producesResource: ResourceType.Wood, productionPerTick: 2, category: BuildingCategory.Production, maxHealth: 100);

            var quarryData = CreateBuildingData(
                "Quarry", "Каменоломня", new Vector2Int(2, 2), height: 2f,
                wallColor: new Color(0.55f, 0.53f, 0.48f), roofColor: new Color(0.3f, 0.29f, 0.27f),
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Wood, amount = 20 } },
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                maxWorkers: 3, producesResource: ResourceType.Stone, productionPerTick: 2, category: BuildingCategory.Production, maxHealth: 110);

            var mineData = CreateBuildingData(
                "Mine", "Шахта", new Vector2Int(2, 2), height: 2.2f,
                wallColor: new Color(0.4f, 0.38f, 0.36f), roofColor: new Color(0.6f, 0.5f, 0.2f),
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Wood, amount = 25 }, new ResourceAmount { type = ResourceType.Stone, amount = 15 } },
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                maxWorkers: 3, producesResource: ResourceType.Gold, productionPerTick: 1, category: BuildingCategory.Production, maxHealth: 110);

            var wallData = CreateBuildingData(
                "Wall", "Стена", new Vector2Int(1, 1), height: 1.6f,
                wallColor: new Color(0.55f, 0.53f, 0.48f), roofColor: Color.white,
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Stone, amount = 5 } },
                style: BuildingStyle.Fortification, trimMaterial: trimMaterial, windowMaterial: windowMaterial, category: BuildingCategory.Military, maxHealth: 150, defense: 15);

            var towerData = CreateBuildingData(
                "Tower", "Башня", new Vector2Int(2, 2), height: 4.2f,
                wallColor: new Color(0.4f, 0.38f, 0.34f), roofColor: Color.white,
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Stone, amount = 15 }, new ResourceAmount { type = ResourceType.Wood, amount = 5 } },
                style: BuildingStyle.Tower, trimMaterial: trimMaterial, windowMaterial: windowMaterial, category: BuildingCategory.Military, maxHealth: 220, defense: 25);

            var barracksData = CreateBuildingData(
                "Barracks", "Казармы", new Vector2Int(2, 2), height: 2.6f,
                wallColor: new Color(0.5f, 0.48f, 0.44f), roofColor: Color.white,
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Stone, amount = 30 }, new ResourceAmount { type = ResourceType.Wood, amount = 15 } },
                style: BuildingStyle.Fortification, trimMaterial: trimMaterial, windowMaterial: windowMaterial, category: BuildingCategory.Military, maxHealth: 180, defense: 10);

            var gateData = CreateBuildingData(
                "Gate", "Ворота", new Vector2Int(2, 1), height: 1.8f,
                wallColor: new Color(0.4f, 0.38f, 0.34f), roofColor: Color.white,
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Stone, amount = 12 }, new ResourceAmount { type = ResourceType.Wood, amount = 5 } },
                style: BuildingStyle.Fortification, trimMaterial: trimMaterial, windowMaterial: windowMaterial, category: BuildingCategory.Military, maxHealth: 160, defense: 12);

            var hotbarBuildingData = new List<BuildingData>
            {
                houseData, cottageData, fishermanHutData, hunterHutData, farmData,
                lumberjackData, quarryData, mineData, wallData, towerData, barracksData, gateData
            };
            var allBuildingData = new List<BuildingData>(hotbarBuildingData) { townHallData };

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
            var boundsMargin = 6f;
            rigSO.FindProperty("panBoundsMin").vector2Value = new Vector2(GroundOrigin.x + boundsMargin, GroundOrigin.z + boundsMargin);
            rigSO.FindProperty("panBoundsMax").vector2Value = new Vector2(-GroundOrigin.x - boundsMargin, -GroundOrigin.z - boundsMargin);
            rigSO.ApplyModifiedPropertiesWithoutUndo();

            var managers = new GameObject("GameManagers");

            var gridManager = managers.AddComponent<GridManager>();
            var gridSO = new SerializedObject(gridManager);
            gridSO.FindProperty("cellSize").floatValue = CellSize;
            gridSO.FindProperty("originWorldPosition").vector3Value = GroundOrigin;
            gridSO.FindProperty("gridSize").vector2IntValue = new Vector2Int(GridCellsX, GridCellsZ);
            gridSO.FindProperty("groundHeight").floatValue = GroundHeight;
            gridSO.ApplyModifiedPropertiesWithoutUndo();

            managers.AddComponent<ResourceManager>();

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

            var citizenManager = managers.AddComponent<CitizenManager>();
            var citizenManagerSO = new SerializedObject(citizenManager);
            citizenManagerSO.FindProperty("buildingPlacer").objectReferenceValue = placer;
            citizenManagerSO.ApplyModifiedPropertiesWithoutUndo();

            var citizenVisualsManager = managers.AddComponent<CitizenVisualsManager>();
            var citizenVisualsManagerSO = new SerializedObject(citizenVisualsManager);
            citizenVisualsManagerSO.FindProperty("citizenManager").objectReferenceValue = citizenManager;
            citizenVisualsManagerSO.FindProperty("gridManager").objectReferenceValue = gridManager;
            citizenVisualsManagerSO.ApplyModifiedPropertiesWithoutUndo();

            var saveController = managers.AddComponent<GameSaveController>();
            var saveControllerSO = new SerializedObject(saveController);
            saveControllerSO.FindProperty("buildingPlacer").objectReferenceValue = placer;
            saveControllerSO.FindProperty("citizenManager").objectReferenceValue = citizenManager;
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

            BuildGameplayUI(placer, saveController, camera, hotbarBuildingData);

            Directory.CreateDirectory(ScenesFolder);
            DeleteIfExists($"{ScenesFolder}/CityBuilder.unity");
            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/CityBuilder.unity");
        }

        private static void BuildGameplayUI(BuildingPlacer placer, GameSaveController saveController, Camera targetCamera, List<BuildingData> hotbarBuildings)
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

            // Hint shown only while the player must place the mandatory Town Hall.
            var hintRoot = CreateImage(canvasGO.transform, "PlacementHint", new Color(0f, 0f, 0f, 0.6f));
            hintRoot.sprite = panelSprite;
            var hintRect = hintRoot.GetComponent<RectTransform>();
            hintRect.anchorMin = hintRect.anchorMax = new Vector2(0.5f, 1f);
            hintRect.anchoredPosition = new Vector2(0f, -70f);
            hintRect.sizeDelta = new Vector2(780f, 84f);
            CreateText(hintRoot.transform, "Text", "Выберите место для Ратуши (5x5)", 30, Vector2.zero, new Vector2(740f, 74f));

            // Touch-friendly building hotbar, grouped into categories (a small category row above
            // it picks which category's buildings the hotbar shows -- see BuildingCategoryPanel).
            // Shown only once the Town Hall is placed (there's nothing else buildable before that
            // point). Number-key hotkeys still work on PC, against the same availableBuildings
            // list regardless of which category is currently shown.
            var menuGO = new GameObject("BuildingMenu", typeof(RectTransform));
            menuGO.transform.SetParent(canvasGO.transform, false);
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
            var categoryOrder = new[] { BuildingCategory.Housing, BuildingCategory.Food, BuildingCategory.Production, BuildingCategory.Military };
            var presentCategories = new List<BuildingCategory>();
            foreach (var cat in categoryOrder)
            {
                if (hotbarBuildings.Exists(b => b.category == cat)) presentCategories.Add(cat);
            }

            const float categoryButtonSize = 80f;
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

            BuildSaveUI(canvasGO.transform, panelSprite, saveController);
            BuildExitUI(canvasGO.transform, panelSprite);
            BuildResourceHUD(canvasGO.transform);

            var infoPanel = BuildBuildingInfoPanel(canvasGO.transform, panelSprite);
            BuildBuildingSelector(canvasGO.transform, targetCamera, placer, infoPanel);
        }

        private static void BuildResourceHUD(Transform canvasParent)
        {
            // Sits just below the top row (hint banner + Меню/Сохранить corners, all in the
            // y 420-510 band) in the same center-anchored coordinate space as those, so it
            // never overlaps them regardless of which of those happen to be visible.
            var root = new GameObject("ResourceHUD", typeof(RectTransform));
            root.transform.SetParent(canvasParent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 370f);
            rect.sizeDelta = new Vector2(1300f, 50f);

            var label = CreateText(root.transform, "Label", string.Empty, 24, Vector2.zero, new Vector2(1300f, 50f));

            var hud = root.AddComponent<ResourceHUDController>();
            var hudSO = new SerializedObject(hud);
            hudSO.FindProperty("label").objectReferenceValue = label;
            hudSO.ApplyModifiedPropertiesWithoutUndo();
        }

        private static BuildingInfoPanelController BuildBuildingInfoPanel(Transform canvasParent, Sprite panelSprite)
        {
            var panelRoot = new GameObject("BuildingInfoPanel", typeof(RectTransform));
            panelRoot.transform.SetParent(canvasParent, false);
            StretchFull(panelRoot.GetComponent<RectTransform>());

            var backdrop = CreateImage(panelRoot.transform, "Backdrop", new Color(0f, 0f, 0f, 0.7f));
            StretchFull(backdrop.GetComponent<RectTransform>());

            var card = CreateImage(panelRoot.transform, "Card", new Color(0.16f, 0.18f, 0.15f, 0.98f));
            card.sprite = panelSprite;
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(700f, 600f);
            cardRect.anchoredPosition = Vector2.zero;

            var title = CreateText(card.transform, "Title", string.Empty, 34, new Vector2(0f, 260f), new Vector2(640f, 60f));
            var level = CreateText(card.transform, "Level", string.Empty, 24, new Vector2(0f, 205f), new Vector2(640f, 40f), new Color(1f, 1f, 1f, 0.85f));
            var condition = CreateText(card.transform, "Condition", string.Empty, 20, new Vector2(0f, 165f), new Vector2(640f, 40f), new Color(1f, 1f, 1f, 0.6f));

            // Passthrough containers (StretchFull, zero offset) purely to group-toggle visibility --
            // children keep the same card-relative positions they'd have without the wrapper.
            var workerControls = new GameObject("WorkerControls", typeof(RectTransform));
            workerControls.transform.SetParent(card.transform, false);
            StretchFull(workerControls.GetComponent<RectTransform>());

            var workers = CreateText(workerControls.transform, "Workers", string.Empty, 26, new Vector2(0f, 105f), new Vector2(640f, 50f), new Color(1f, 1f, 1f, 0.85f));
            var idle = CreateText(workerControls.transform, "Idle", string.Empty, 22, new Vector2(0f, 65f), new Vector2(640f, 40f), new Color(1f, 1f, 1f, 0.6f));
            var assignButton = CreateButton(workerControls.transform, panelSprite, "AssignButton", "+ Назначить", new Vector2(-170f, 10f), new Vector2(300f, 80f));
            var unassignButton = CreateButton(workerControls.transform, panelSprite, "UnassignButton", "- Снять", new Vector2(170f, 10f), new Vector2(300f, 80f));

            var upgradeControls = new GameObject("UpgradeControls", typeof(RectTransform));
            upgradeControls.transform.SetParent(card.transform, false);
            StretchFull(upgradeControls.GetComponent<RectTransform>());

            var upgradeCost = CreateText(upgradeControls.transform, "UpgradeCost", string.Empty, 22, new Vector2(0f, -55f), new Vector2(640f, 40f), new Color(1f, 1f, 1f, 0.7f));
            var upgradeButton = CreateButton(upgradeControls.transform, panelSprite, "UpgradeButton", "Улучшить", new Vector2(0f, -125f), new Vector2(360f, 70f));

            var closeButton = CreateButton(card.transform, panelSprite, "CloseButton", "Закрыть", new Vector2(0f, -215f), new Vector2(300f, 70f));

            var controller = panelRoot.AddComponent<BuildingInfoPanelController>();
            var controllerSO = new SerializedObject(controller);
            controllerSO.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            controllerSO.FindProperty("titleLabel").objectReferenceValue = title;
            controllerSO.FindProperty("levelLabel").objectReferenceValue = level;
            controllerSO.FindProperty("conditionLabel").objectReferenceValue = condition;
            controllerSO.FindProperty("workerControls").objectReferenceValue = workerControls;
            controllerSO.FindProperty("workersLabel").objectReferenceValue = workers;
            controllerSO.FindProperty("idleLabel").objectReferenceValue = idle;
            controllerSO.FindProperty("upgradeControls").objectReferenceValue = upgradeControls;
            controllerSO.FindProperty("upgradeCostLabel").objectReferenceValue = upgradeCost;
            controllerSO.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(assignButton.onClick, controller.AssignWorker);
            UnityEventTools.AddPersistentListener(unassignButton.onClick, controller.UnassignWorker);
            UnityEventTools.AddPersistentListener(upgradeButton.onClick, controller.Upgrade);
            UnityEventTools.AddPersistentListener(closeButton.onClick, controller.Close);

            panelRoot.SetActive(false);
            return controller;
        }

        private static void BuildBuildingSelector(Transform canvasParent, Camera targetCamera, BuildingPlacer placer, BuildingInfoPanelController infoPanel)
        {
            var go = new GameObject("BuildingSelector");
            go.transform.SetParent(canvasParent, false);
            var selector = go.AddComponent<BuildingSelector>();
            var so = new SerializedObject(selector);
            so.FindProperty("targetCamera").objectReferenceValue = targetCamera;
            so.FindProperty("buildingPlacer").objectReferenceValue = placer;
            so.FindProperty("infoPanel").objectReferenceValue = infoPanel;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildExitUI(Transform canvasParent, Sprite panelSprite)
        {
            var menuButton = CreateButton(canvasParent, panelSprite, "MenuButton", "Меню", new Vector2(-810f, 465f), new Vector2(240f, 90f));

            var dialogRoot = new GameObject("ExitDialog", typeof(RectTransform));
            dialogRoot.transform.SetParent(canvasParent, false);
            StretchFull(dialogRoot.GetComponent<RectTransform>());

            var backdrop = CreateImage(dialogRoot.transform, "Backdrop", new Color(0f, 0f, 0f, 0.7f));
            StretchFull(backdrop.GetComponent<RectTransform>());

            var card = CreateImage(dialogRoot.transform, "Card", new Color(0.16f, 0.18f, 0.15f, 0.98f));
            card.sprite = panelSprite;
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(760f, 380f);
            cardRect.anchoredPosition = Vector2.zero;

            CreateText(card.transform, "Title", "Выйти в главное меню?", 36, new Vector2(0f, 120f), new Vector2(680f, 70f));
            CreateText(card.transform, "Warning", "Несохранённые изменения будут потеряны.", 24, new Vector2(0f, 40f), new Vector2(680f, 60f), new Color(1f, 0.7f, 0.6f, 1f));

            var confirmButton = CreateButton(card.transform, panelSprite, "ConfirmExitButton", "Выйти", new Vector2(-160f, -90f), new Vector2(300f, 90f));
            var cancelButton = CreateButton(card.transform, panelSprite, "CancelExitButton", "Отмена", new Vector2(160f, -90f), new Vector2(300f, 90f));

            var dialogController = dialogRoot.AddComponent<ExitToMenuController>();
            var dialogSO = new SerializedObject(dialogController);
            dialogSO.FindProperty("dialogRoot").objectReferenceValue = dialogRoot;
            dialogSO.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(menuButton.onClick, dialogController.OpenDialog);
            UnityEventTools.AddPersistentListener(confirmButton.onClick, dialogController.ConfirmExit);
            UnityEventTools.AddPersistentListener(cancelButton.onClick, dialogController.CloseDialog);

            dialogRoot.SetActive(false);
        }

        private static void BuildSaveUI(Transform canvasParent, Sprite panelSprite, GameSaveController saveController)
        {
            // Manual save only, per design: the player decides if/when to save, so this button
            // is the only way a save ever happens — there is no autosave to fall back on.
            var saveButton = CreateButton(canvasParent, panelSprite, "SaveButton", "Сохранить", new Vector2(810f, 465f), new Vector2(240f, 90f));

            var dialogRoot = new GameObject("SaveDialog", typeof(RectTransform));
            dialogRoot.transform.SetParent(canvasParent, false);
            StretchFull(dialogRoot.GetComponent<RectTransform>());

            var backdrop = CreateImage(dialogRoot.transform, "Backdrop", new Color(0f, 0f, 0f, 0.7f));
            StretchFull(backdrop.GetComponent<RectTransform>());

            var card = CreateImage(dialogRoot.transform, "Card", new Color(0.16f, 0.18f, 0.15f, 0.98f));
            card.sprite = panelSprite;
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(760f, 420f);
            cardRect.anchoredPosition = Vector2.zero;

            CreateText(card.transform, "Title", "Сохранить игру", 40, new Vector2(0f, 150f), new Vector2(680f, 70f));

            var nameInput = CreateInputField(card.transform, "NameInput", "MyCityName", new Vector2(0f, 30f), new Vector2(600f, 90f), panelSprite);

            var confirmButton = CreateButton(card.transform, panelSprite, "ConfirmSaveButton", "Сохранить", new Vector2(-160f, -100f), new Vector2(300f, 90f));
            var cancelButton = CreateButton(card.transform, panelSprite, "CancelSaveButton", "Отмена", new Vector2(160f, -100f), new Vector2(300f, 90f));

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
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(1000f, 760f);
            cardRect.anchoredPosition = Vector2.zero;

            CreateText(card.transform, "Title", "Загрузить игру", 48, new Vector2(0f, 320f), new Vector2(900f, 80f));

            BuildSaveScrollList(card.transform, out var content, out var emptyLabelGO);

            var selectedLabel = CreateText(card.transform, "SelectedLabel", string.Empty, 26, new Vector2(0f, -170f), new Vector2(900f, 50f), new Color(1f, 1f, 1f, 0.8f));

            var loadButton = CreateButton(card.transform, panelSprite, "LoadSelectedButton", "Загрузить", new Vector2(-190f, -270f), new Vector2(360f, 90f));
            var backButton = CreateButton(card.transform, panelSprite, "BackButton", "Назад", new Vector2(190f, -270f), new Vector2(360f, 90f));

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

        private static void BuildSaveScrollList(Transform parent, out RectTransform content, out GameObject emptyLabelGO)
        {
            var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollGO.transform.SetParent(parent, false);
            var scrollRectTransform = scrollGO.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = scrollRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchoredPosition = new Vector2(0f, 60f);
            scrollRectTransform.sizeDelta = new Vector2(900f, 420f);

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

            var emptyLabel = CreateText(viewportGO.transform, "EmptyLabel", "Нет сохранений", 28, Vector2.zero, new Vector2(700f, 80f), new Color(1f, 1f, 1f, 0.6f));

            content = contentRect;
            emptyLabelGO = emptyLabel.gameObject;
        }

        /// <summary>Architectural archetype driving which procedural generator builds a prefab.</summary>
        private enum BuildingStyle { Hut, Fortification, Tower }

        private static BuildingData CreateBuildingData(
            string id, string displayName, Vector2Int footprint, float height, Color wallColor, Color roofColor, List<ResourceAmount> cost,
            BuildingStyle style, Material trimMaterial, Material windowMaterial, Material doorMaterial = null,
            bool hasChimney = false, int citizensGranted = 0,
            int maxWorkers = 0, ResourceType producesResource = ResourceType.Wood, int productionPerTick = 0, float productionInterval = 6f,
            BuildingCategory category = BuildingCategory.Production, int maxHealth = 100, int defense = 0)
        {
            GameObject prefab;
            switch (style)
            {
                case BuildingStyle.Hut:
                    prefab = CreateHutPrefab(id, footprint, height, wallColor, roofColor, maxWorkers, hasChimney, trimMaterial, doorMaterial, windowMaterial);
                    break;
                case BuildingStyle.Fortification:
                    prefab = CreateFortificationPrefab(id, footprint, height, wallColor, trimMaterial, isTower: false);
                    break;
                case BuildingStyle.Tower:
                    prefab = CreateFortificationPrefab(id, footprint, height, wallColor, trimMaterial, isTower: true);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(style), style, null);
            }

            var data = ScriptableObject.CreateInstance<BuildingData>();
            data.buildingName = id;
            data.displayName = displayName;
            data.prefab = prefab;
            data.footprintSize = footprint;
            data.cost = cost;
            data.citizensGranted = citizensGranted;
            data.maxWorkers = maxWorkers;
            data.producesResource = producesResource;
            data.productionPerWorkerPerTick = productionPerTick;
            data.productionIntervalSeconds = productionInterval;
            data.category = category;
            data.maxHealth = maxHealth;
            data.defense = defense;
            data.upgradeToLevel2Cost = ScaleCost(cost, 1.6f);
            data.upgradeToLevel3Cost = ScaleCost(cost, 2.8f);

            Directory.CreateDirectory(BuildingDataFolder);
            var dataPath = $"{BuildingDataFolder}/{id}.asset";
            DeleteIfExists(dataPath);
            AssetDatabase.CreateAsset(data, dataPath);
            return data;
        }

        /// <summary>Derives an upgrade-level cost from a building's base placement cost -- e.g. level 2 costs 1.6x the base, level 3 costs 2.8x. Each amount is rounded up to at least 1 so a cheap base cost still produces a meaningful upgrade cost.</summary>
        private static List<ResourceAmount> ScaleCost(List<ResourceAmount> baseCost, float multiplier)
        {
            var scaled = new List<ResourceAmount>();
            foreach (var amount in baseCost)
            {
                scaled.Add(new ResourceAmount { type = amount.type, amount = Mathf.Max(1, Mathf.RoundToInt(amount.amount * multiplier)) });
            }
            return scaled;
        }

        /// <summary>
        /// Procedurally assembles a dwelling/workshop from cube primitives: a stepped plinth,
        /// corner posts, a framed door with a step, a window count derived from wall width
        /// (placed on both the front and back faces), a banded three-tier roof with a ridge cap,
        /// and an optional chimney with a cap. The part count and placement all come from
        /// footprint/height, not hand-picked positions, so the same generator produces a small
        /// House and a wider Lumberjack alike.
        /// </summary>
        private static GameObject CreateHutPrefab(string name, Vector2Int footprint, float height, Color wallColor, Color roofColor, int maxWorkers, bool hasChimney, Material trimMaterial, Material doorMaterial, Material windowMaterial)
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
        private static GameObject CreateFortificationPrefab(string name, Vector2Int footprint, float height, Color wallColor, Material trimMaterial, bool isTower)
        {
            var sizeX = footprint.x * CellSize - BuildingInset;
            var sizeZ = footprint.y * CellSize - BuildingInset;

            var root = new GameObject(name);
            root.AddComponent<BuildingInstance>();

            var wallMaterial = CreateLitMaterial($"Building_{name}_Walls", wallColor);
            var shadeMaterial = CreateLitMaterial($"Building_{name}_Shade", Shade(wallColor, 0.85f));

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

            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(sizeX * 1.1f, y, sizeZ * 1.1f);
            collider.center = new Vector3(0f, y * 0.5f, 0f);

            return SavePrefab(root, name);
        }

        /// <summary>
        /// Builds a BuildingData backed by a hand-authored FBX model (Assets/_Project/Models/
        /// Buildings/) instead of one of the procedural generators above -- currently just the
        /// Town Hall. The model's own root rotation is preserved rather than forced to identity
        /// (see MeshMapApplier for why: Blender-authored FBX carry a corrective root rotation that
        /// must survive instantiation), and it's parented under an unrotated wrapper GameObject so
        /// the BoxCollider's size/center -- sized to the logical footprint, not the mesh -- stay
        /// aligned to world axes regardless of that rotation. Expects the model's own pivot at the
        /// footprint's base center (matching the convention used for the hand-authored map meshes).
        /// </summary>
        private static BuildingData CreateFbxBuildingData(string id, string displayName, Vector2Int footprint, float height, string fbxFileName, List<ResourceAmount> cost, int citizensGranted = 0,
            int maxHealth = 200, int defense = 0, List<ResourceAmount> upgradeToLevel2Cost = null, List<ResourceAmount> upgradeToLevel3Cost = null)
        {
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsBuildingsFolder}/{fbxFileName}");

            GameObject prefab = null;
            if (sourcePrefab != null)
            {
                var root = new GameObject(id);
                root.AddComponent<BuildingInstance>();
                Object.Instantiate(sourcePrefab, Vector3.zero, sourcePrefab.transform.rotation, root.transform);

                var sizeX = footprint.x * CellSize - BuildingInset;
                var sizeZ = footprint.y * CellSize - BuildingInset;
                var collider = root.AddComponent<BoxCollider>();
                collider.size = new Vector3(sizeX, height, sizeZ);
                collider.center = new Vector3(0f, height * 0.5f, 0f);

                prefab = SavePrefab(root, id);
            }
            else
            {
                Debug.LogError($"CreateFbxBuildingData: FBX not found at {ModelsBuildingsFolder}/{fbxFileName} -- '{id}' will have no prefab.");
            }

            var data = ScriptableObject.CreateInstance<BuildingData>();
            data.buildingName = id;
            data.displayName = displayName;
            data.prefab = prefab;
            data.footprintSize = footprint;
            data.cost = cost;
            data.citizensGranted = citizensGranted;
            data.maxHealth = maxHealth;
            data.defense = defense;
            data.upgradeToLevel2Cost = upgradeToLevel2Cost ?? new List<ResourceAmount>();
            data.upgradeToLevel3Cost = upgradeToLevel3Cost ?? new List<ResourceAmount>();

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

        private static Material CreateLitMaterial(string name, Color color)
        {
            var path = $"{MaterialsFolder}/{name}.mat";
            Directory.CreateDirectory(MaterialsFolder);
            DeleteIfExists(path);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        /// <summary>
        /// Loads a hand-authored map's five FBX prefabs by their fixed Models/ paths and writes a
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
            var tree1 = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsTerrainFolder}/Tree1.fbx");
            var tree2 = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsTerrainFolder}/Tree2.fbx");

            if (ground == null || water == null || waterPlacementZone == null || treesArea == null || tree1 == null || tree2 == null)
            {
                Debug.LogWarning($"CreateMeshMapDefinition: one or more Map1 FBX assets not found under {ModelsMap1Folder}/{ModelsTerrainFolder} -- skipping Map1 MeshMapDefinition.");
                return;
            }

            var map = ScriptableObject.CreateInstance<MeshMapDefinition>();
            map.EditorInitialize(mapId, ground, water, waterPlacementZone, treesArea, new[] { tree1, tree2 });

            Directory.CreateDirectory(MeshMapsFolder);
            var path = $"{MeshMapsFolder}/{mapId}.asset";
            DeleteIfExists(path);
            AssetDatabase.CreateAsset(map, path);
        }

        private static Material CreateGroundMaterial()
        {
            var path = $"{MaterialsFolder}/Ground.mat";
            DeleteIfExists(path);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.42f, 0.62f, 0.32f) };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Sprite CreatePanelSprite()
        {
            // Plain solid rectangle, sharp 90-degree corners — the whole game is meant to read
            // as cubic/angular, so no rounded UI corners either.
            const int size = 8;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[size * size];
            for (var i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();

            Directory.CreateDirectory(TexturesFolder);
            var texPath = $"{TexturesFolder}/UI_Panel.asset";
            DeleteIfExists(texPath);
            AssetDatabase.CreateAsset(texture, texPath);

            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
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
        private static Sprite CreateIconSprite(string key, int size, System.Action<Color[], int> paint)
        {
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
                case BuildingCategory.Housing:
                    return CreateIconSprite("Cat_Housing", 64, (p, s) =>
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
                case BuildingCategory.Military:
                    return CreateIconSprite("Cat_Military", 64, (p, s) =>
                    {
                        var shield = new Color(0.45f, 0.46f, 0.5f);
                        FillIconRect(p, s, 0.22f, 0.42f, 0.78f, 0.85f, shield);
                        FillIconTriangle(p, s, new Vector2(0.22f, 0.42f), new Vector2(0.78f, 0.42f), new Vector2(0.5f, 0.12f), shield);
                        var trim = new Color(0.7f, 0.62f, 0.3f);
                        FillIconRect(p, s, 0.46f, 0.2f, 0.54f, 0.78f, trim);
                        FillIconRect(p, s, 0.3f, 0.58f, 0.7f, 0.66f, trim);
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
                case "House":
                    return CreateIconSprite("Bld_House", 64, (p, s) =>
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
                case "FishermanHut":
                    return CreateIconSprite("Bld_Fisherman", 64, (p, s) =>
                    {
                        var fish = new Color(0.3f, 0.52f, 0.68f);
                        FillIconTriangle(p, s, new Vector2(0.15f, 0.5f), new Vector2(0.7f, 0.72f), new Vector2(0.7f, 0.28f), fish);
                        FillIconTriangle(p, s, new Vector2(0.7f, 0.28f), new Vector2(0.7f, 0.72f), new Vector2(0.92f, 0.5f), fish);
                    });
                case "HunterHut":
                    return CreateIconSprite("Bld_Hunter", 64, (p, s) =>
                    {
                        var wood = new Color(0.4f, 0.28f, 0.16f);
                        FillIconRect(p, s, 0.18f, 0.46f, 0.62f, 0.54f, wood);
                        FillIconTriangle(p, s, new Vector2(0.6f, 0.36f), new Vector2(0.6f, 0.64f), new Vector2(0.85f, 0.5f), wood);
                        FillIconTriangle(p, s, new Vector2(0.18f, 0.5f), new Vector2(0.3f, 0.62f), new Vector2(0.3f, 0.38f), new Color(0.75f, 0.18f, 0.16f));
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
                case "Lumberjack":
                    return CreateIconSprite("Bld_Lumberjack", 64, (p, s) =>
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
                case "Mine":
                    return CreateIconSprite("Bld_Mine", 64, (p, s) =>
                    {
                        var ore = new Color(0.78f, 0.68f, 0.22f);
                        FillIconRect(p, s, 0.15f, 0.15f, 0.85f, 0.4f, new Color(0.35f, 0.35f, 0.38f));
                        FillIconRect(p, s, 0.25f, 0.4f, 0.45f, 0.58f, ore);
                        FillIconRect(p, s, 0.45f, 0.4f, 0.65f, 0.65f, ore);
                        FillIconRect(p, s, 0.6f, 0.4f, 0.78f, 0.55f, ore);
                        FillIconRect(p, s, 0.2f, 0.08f, 0.32f, 0.15f, new Color(0.15f, 0.15f, 0.15f));
                        FillIconRect(p, s, 0.68f, 0.08f, 0.8f, 0.15f, new Color(0.15f, 0.15f, 0.15f));
                    });
                case "Wall":
                    return CreateIconSprite("Bld_Wall", 64, (p, s) =>
                    {
                        var brick = new Color(0.55f, 0.53f, 0.48f);
                        FillIconRect(p, s, 0.1f, 0.1f, 0.9f, 0.9f, new Color(0.3f, 0.29f, 0.26f));
                        FillIconRect(p, s, 0.12f, 0.62f, 0.46f, 0.86f, brick);
                        FillIconRect(p, s, 0.5f, 0.62f, 0.88f, 0.86f, brick);
                        FillIconRect(p, s, 0.12f, 0.38f, 0.3f, 0.6f, brick);
                        FillIconRect(p, s, 0.34f, 0.38f, 0.68f, 0.6f, brick);
                        FillIconRect(p, s, 0.72f, 0.38f, 0.88f, 0.6f, brick);
                        FillIconRect(p, s, 0.12f, 0.14f, 0.46f, 0.36f, brick);
                        FillIconRect(p, s, 0.5f, 0.14f, 0.88f, 0.36f, brick);
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
                default:
                    return CreateIconSprite($"Bld_{buildingId}", 64, (p, s) =>
                    {
                        FillIconRect(p, s, 0.25f, 0.25f, 0.75f, 0.75f, new Color(0.6f, 0.6f, 0.6f));
                    });
            }
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

        private static Text CreateText(Transform parent, string name, string content, int fontSize, Vector2 anchoredPos, Vector2 sizeDelta, Color? color = null)
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
            return text;
        }

        private static Button CreateButton(Transform parent, Sprite sprite, string name, string label, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = new Color(0.26f, 0.29f, 0.24f, 0.95f);

            CreateText(go.transform, "Label", label, 28, Vector2.zero, sizeDelta - new Vector2(20f, 20f));
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
