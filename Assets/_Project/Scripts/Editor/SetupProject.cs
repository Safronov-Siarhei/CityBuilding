using System.Collections.Generic;
using System.IO;
using CityBuilder.Buildings;
using CityBuilder.CameraControl;
using CityBuilder.Citizens;
using CityBuilder.Core;
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
        private const string ResourceScriptableObjectsFolder = "Assets/_Project/ScriptableObjects/Resources";
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
            ForceReimportModels();

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

            CreateText(canvasGO.transform, "Title", "СТРОИТЕЛЬ ГОРОДОВ", 72, new Vector2(0f, 260f), new Vector2(1200f, 140f), addShadow: true);
            CreateText(canvasGO.transform, "Subtitle", "мобильный градостроитель", 26, new Vector2(0f, 175f), new Vector2(900f, 60f), new Color(1f, 1f, 1f, 0.6f), addShadow: true);

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
                hasChimney: true, citizensGranted: 5, category: BuildingCategory.Housing, maxHealth: 80, fogRevealRadius: 10);

            var cottageData = CreateBuildingData(
                "Cottage", "Коттедж", new Vector2Int(1, 1), height: 2.3f,
                wallColor: new Color(0.62f, 0.42f, 0.55f), roofColor: new Color(0.3f, 0.2f, 0.4f),
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Wood, amount = 25 }, new ResourceAmount { type = ResourceType.Stone, amount = 8 } },
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                hasChimney: true, citizensGranted: 8, category: BuildingCategory.Housing, maxHealth: 120, fogRevealRadius: 10);
            // First example wiring of the requirement system (BuildingData.requiredBuilding):
            // upscale housing needs a basic House placed first. AssetDatabase.CreateAsset (inside
            // CreateBuildingData) writes the asset immediately -- see the Iron/Coal upgrade-cost
            // bug this same gotcha caused earlier -- so this mutation needs an explicit SetDirty
            // or the later AssetDatabase.SaveAssets() silently drops it.
            cottageData.requiredBuilding = houseData;
            EditorUtility.SetDirty(cottageData);

            var townHallData = CreateFbxBuildingData(
                "TownHall", "Ратуша", new Vector2Int(4, 4), height: 3f,
                fbxFileName: "MainCastle-1.fbx",
                cost: new List<ResourceAmount>(),
                citizensGranted: 5, maxHealth: 400, defense: 20,
                upgradeToLevel2Cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Wood, amount = 100 }, new ResourceAmount { type = ResourceType.Stone, amount = 60 } },
                upgradeToLevel3Cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Wood, amount = 220 }, new ResourceAmount { type = ResourceType.Stone, amount = 150 }, new ResourceAmount { type = ResourceType.Gold, amount = 40 }, new ResourceAmount { type = ResourceType.Coal, amount = 20 } });

            var fishermanHutData = CreateBuildingData(
                "FishermanHut", "Хижина рыбака", new Vector2Int(2, 1), height: 2f,
                wallColor: new Color(0.55f, 0.52f, 0.45f), roofColor: new Color(0.2f, 0.5f, 0.55f),
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Wood, amount = 15 } },
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                hasChimney: true, maxWorkers: 2, producesResource: ResourceType.Food, productionPerTick: 2, category: BuildingCategory.Food, maxHealth: 90, fogRevealRadius: 12);

            var hunterHutData = CreateBuildingData(
                "HunterHut", "Хижина охотника", new Vector2Int(2, 1), height: 2f,
                wallColor: new Color(0.38f, 0.28f, 0.18f), roofColor: new Color(0.22f, 0.42f, 0.24f),
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Wood, amount = 15 }, new ResourceAmount { type = ResourceType.Stone, amount = 5 } },
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                hasChimney: true, maxWorkers: 2, producesResource: ResourceType.Food, productionPerTick: 2, category: BuildingCategory.Food, maxHealth: 90, fogRevealRadius: 14);

            var farmData = CreateBuildingData(
                "Farm", "Ферма", new Vector2Int(2, 2), height: 1.8f,
                wallColor: new Color(0.68f, 0.55f, 0.3f), roofColor: new Color(0.42f, 0.58f, 0.24f),
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Wood, amount = 15 }, new ResourceAmount { type = ResourceType.Stone, amount = 5 } },
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                maxWorkers: 3, producesResource: ResourceType.Food, productionPerTick: 3, category: BuildingCategory.Food, maxHealth: 70, fogRevealRadius: 10);

            var lumberjackData = CreateBuildingData(
                "Lumberjack", "Лесопилка", new Vector2Int(2, 2), height: 2.4f,
                wallColor: new Color(0.45f, 0.3f, 0.18f), roofColor: new Color(0.32f, 0.22f, 0.13f),
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Wood, amount = 20 }, new ResourceAmount { type = ResourceType.Stone, amount = 5 } },
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                maxWorkers: 3, producesResource: ResourceType.Wood, productionPerTick: 2, category: BuildingCategory.Production, maxHealth: 100, fogRevealRadius: 15);

            var quarryData = CreateBuildingData(
                "Quarry", "Каменоломня", new Vector2Int(2, 2), height: 2f,
                wallColor: new Color(0.55f, 0.53f, 0.48f), roofColor: new Color(0.3f, 0.29f, 0.27f),
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Wood, amount = 20 } },
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                maxWorkers: 3, producesResource: ResourceType.Stone, productionPerTick: 2, category: BuildingCategory.Production, maxHealth: 110, fogRevealRadius: 14);

            var mineData = CreateBuildingData(
                "Mine", "Шахта", new Vector2Int(2, 2), height: 2.2f,
                wallColor: new Color(0.4f, 0.38f, 0.36f), roofColor: new Color(0.5f, 0.5f, 0.56f),
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Wood, amount = 25 }, new ResourceAmount { type = ResourceType.Stone, amount = 15 } },
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                maxWorkers: 3, producesResource: ResourceType.Iron, productionPerTick: 1, category: BuildingCategory.Production, maxHealth: 110, fogRevealRadius: 14);
            // Digging deeper for iron ore needs fuel to run the forge/pumps -- coal from CoalMine.
            // AssetDatabase.CreateAsset (inside CreateBuildingData) writes the asset to disk
            // immediately; mutating its lists afterward needs an explicit SetDirty or the later
            // AssetDatabase.SaveAssets() call silently skips these edits.
            mineData.upgradeToLevel2Cost.Add(new ResourceAmount { type = ResourceType.Coal, amount = 10 });
            mineData.upgradeToLevel3Cost.Add(new ResourceAmount { type = ResourceType.Coal, amount = 22 });
            EditorUtility.SetDirty(mineData);

            var coalMineData = CreateBuildingData(
                "CoalMine", "Угольная шахта", new Vector2Int(2, 2), height: 2.2f,
                wallColor: new Color(0.3f, 0.28f, 0.27f), roofColor: new Color(0.14f, 0.14f, 0.15f),
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Wood, amount = 25 }, new ResourceAmount { type = ResourceType.Stone, amount = 10 } },
                style: BuildingStyle.Hut, trimMaterial: trimMaterial, doorMaterial: doorMaterial, windowMaterial: windowMaterial,
                maxWorkers: 3, producesResource: ResourceType.Coal, productionPerTick: 2, category: BuildingCategory.Production, maxHealth: 100, fogRevealRadius: 14);
            // Iron tools/props are needed to shore up deeper coal seams -- ties the two mines together.
            coalMineData.upgradeToLevel2Cost.Add(new ResourceAmount { type = ResourceType.Iron, amount = 6 });
            coalMineData.upgradeToLevel3Cost.Add(new ResourceAmount { type = ResourceType.Iron, amount = 14 });
            EditorUtility.SetDirty(coalMineData);

            var wallData = CreateBuildingData(
                "Wall", "Стена", new Vector2Int(1, 1), height: 1.6f,
                wallColor: new Color(0.55f, 0.53f, 0.48f), roofColor: Color.white,
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Stone, amount = 5 } },
                style: BuildingStyle.Fortification, trimMaterial: trimMaterial, windowMaterial: windowMaterial, category: BuildingCategory.Military, maxHealth: 150, defense: 15, fogRevealRadius: 6);
            // Iron reinforcement/fittings gate the defense line's later tiers -- a smithing chain
            // (Mine -> Iron) has to exist before walls/towers/barracks/gates can be hardened.
            wallData.upgradeToLevel2Cost.Add(new ResourceAmount { type = ResourceType.Iron, amount = 6 });
            wallData.upgradeToLevel3Cost.Add(new ResourceAmount { type = ResourceType.Iron, amount = 14 });
            EditorUtility.SetDirty(wallData);

            var towerData = CreateBuildingData(
                "Tower", "Башня", new Vector2Int(2, 2), height: 4.2f,
                wallColor: new Color(0.4f, 0.38f, 0.34f), roofColor: Color.white,
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Stone, amount = 15 }, new ResourceAmount { type = ResourceType.Wood, amount = 5 } },
                style: BuildingStyle.Tower, trimMaterial: trimMaterial, windowMaterial: windowMaterial, category: BuildingCategory.Military, maxHealth: 220, defense: 25, fogRevealRadius: 18);
            towerData.upgradeToLevel2Cost.Add(new ResourceAmount { type = ResourceType.Iron, amount = 12 });
            towerData.upgradeToLevel3Cost.Add(new ResourceAmount { type = ResourceType.Iron, amount = 26 });
            // Second example wiring of the requirement system: a Tower reinforces an existing
            // wall line rather than standing alone.
            towerData.requiredBuilding = wallData;
            EditorUtility.SetDirty(towerData);

            var barracksData = CreateBuildingData(
                "Barracks", "Казармы", new Vector2Int(2, 2), height: 2.6f,
                wallColor: new Color(0.5f, 0.48f, 0.44f), roofColor: Color.white,
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Stone, amount = 30 }, new ResourceAmount { type = ResourceType.Wood, amount = 15 } },
                style: BuildingStyle.Fortification, trimMaterial: trimMaterial, windowMaterial: windowMaterial, category: BuildingCategory.Military, maxHealth: 180, defense: 10, fogRevealRadius: 10);
            // Weapons/armor for a bigger garrison -- the steepest iron sink in the game.
            barracksData.upgradeToLevel2Cost.Add(new ResourceAmount { type = ResourceType.Iron, amount = 15 });
            barracksData.upgradeToLevel3Cost.Add(new ResourceAmount { type = ResourceType.Iron, amount = 32 });
            EditorUtility.SetDirty(barracksData);

            var gateData = CreateBuildingData(
                "Gate", "Ворота", new Vector2Int(2, 1), height: 1.8f,
                wallColor: new Color(0.4f, 0.38f, 0.34f), roofColor: Color.white,
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Stone, amount = 12 }, new ResourceAmount { type = ResourceType.Wood, amount = 5 } },
                style: BuildingStyle.Fortification, trimMaterial: trimMaterial, windowMaterial: windowMaterial, category: BuildingCategory.Military, maxHealth: 160, defense: 12, fogRevealRadius: 8);
            gateData.upgradeToLevel2Cost.Add(new ResourceAmount { type = ResourceType.Iron, amount = 8 });
            gateData.upgradeToLevel3Cost.Add(new ResourceAmount { type = ResourceType.Iron, amount = 18 });
            EditorUtility.SetDirty(gateData);

            var roadData = CreateBuildingData(
                "Road", "Дорога", new Vector2Int(1, 1), height: 0.05f,
                wallColor: new Color(0.32f, 0.32f, 0.34f), roofColor: new Color(0.85f, 0.75f, 0.3f),
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Stone, amount = 3 } },
                style: BuildingStyle.Road, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                category: BuildingCategory.Infrastructure, maxHealth: 40,
                isRoad: true, keepSelectedAfterPlacement: true, fogRevealRadius: 4);

            var bridgeData = CreateBuildingData(
                "Bridge", "Мост", new Vector2Int(1, 1), height: 0.05f,
                wallColor: new Color(0.5f, 0.35f, 0.2f), roofColor: new Color(0.36f, 0.24f, 0.13f),
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Wood, amount = 8 } },
                style: BuildingStyle.Road, trimMaterial: trimMaterial, windowMaterial: windowMaterial,
                category: BuildingCategory.Infrastructure, maxHealth: 40,
                isRoad: true, keepSelectedAfterPlacement: true, isWaterCategory: true, fogRevealRadius: 4);

            var waterMillData = CreateWaterBuildingData(
                "WaterMill", "Водяная мельница", new Vector2Int(2, 2),
                deckColor: new Color(0.5f, 0.4f, 0.28f), accentColor: new Color(0.35f, 0.24f, 0.14f),
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Wood, amount = 25 }, new ResourceAmount { type = ResourceType.Stone, amount = 10 } },
                addWheel: true, maxWorkers: 3, producesResource: ResourceType.Food, productionPerTick: 3,
                category: BuildingCategory.Food, maxHealth: 90, fogRevealRadius: 12);

            var dockData = CreateWaterBuildingData(
                "Dock", "Пристань", new Vector2Int(2, 2),
                deckColor: new Color(0.55f, 0.4f, 0.24f), accentColor: new Color(0.68f, 0.5f, 0.3f),
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Wood, amount = 20 }, new ResourceAmount { type = ResourceType.Stone, amount = 8 } },
                addCrates: true, maxWorkers: 2, producesResource: ResourceType.Gold, productionPerTick: 1,
                category: BuildingCategory.Production, maxHealth: 80, fogRevealRadius: 12);

            var hotbarBuildingData = new List<BuildingData>
            {
                houseData, cottageData, fishermanHutData, hunterHutData, farmData,
                lumberjackData, quarryData, mineData, coalMineData, roadData, wallData, towerData, barracksData, gateData,
                bridgeData, waterMillData, dockData
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
            managers.AddComponent<RoadNetwork>();
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

            // Hint shown only while the player must place the mandatory Town Hall.
            var hintRoot = CreateImage(canvasGO.transform, "PlacementHint", new Color(0f, 0f, 0f, 0.6f));
            hintRoot.sprite = panelSprite;
            hintRoot.type = Image.Type.Sliced;
            var hintRect = hintRoot.GetComponent<RectTransform>();
            hintRect.anchorMin = hintRect.anchorMax = new Vector2(0.5f, 1f);
            hintRect.anchoredPosition = new Vector2(0f, -70f);
            hintRect.sizeDelta = new Vector2(780f, 84f);
            var footprint = mandatoryBuilding != null ? mandatoryBuilding.footprintSize : new Vector2Int(4, 4);
            CreateText(hintRoot.transform, "Text", $"Выберите место для Ратуши ({footprint.x}x{footprint.y})", 30, Vector2.zero, new Vector2(740f, 74f));

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
            var categoryOrder = new[] { BuildingCategory.Housing, BuildingCategory.Food, BuildingCategory.Production, BuildingCategory.Infrastructure, BuildingCategory.Military };
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

            // Mobile rotate button (PC also has the 'R' key -- see BuildingPlacer.Update).
            // Lives outside menuGO/BuildingPlacerUIVisibility's mandatory-placement gating since
            // rotation is useful even while placing the mandatory Town Hall; ShowWhileSelectingBuilding
            // handles its own visibility instead, tied directly to IsSelecting.
            var rotateIcon = CreateRotateIcon();
            var rotateButton = CreateIconButton(canvasGO.transform, panelSprite, rotateIcon, "RotateButton", Vector2.zero, new Vector2(90f, 90f));
            var rotateRect = rotateButton.GetComponent<RectTransform>();
            rotateRect.anchorMin = new Vector2(1f, 0f);
            rotateRect.anchorMax = new Vector2(1f, 0f);
            rotateRect.pivot = new Vector2(1f, 0f);
            rotateRect.anchoredPosition = new Vector2(-40f, 40f);

            UnityEventTools.AddPersistentListener(rotateButton.onClick, placer.RotateSelection);

            // Lives on the Canvas itself (an always-active object), not on the button it
            // controls -- a script polling its own GameObject's active state would stop
            // receiving Update() calls the moment it deactivates it, and never reactivate it.
            var rotateVisibility = canvasGO.AddComponent<ShowWhileSelectingBuilding>();
            var rotateVisibilitySO = new SerializedObject(rotateVisibility);
            rotateVisibilitySO.FindProperty("buildingPlacer").objectReferenceValue = placer;
            rotateVisibilitySO.FindProperty("target").objectReferenceValue = rotateButton.gameObject;
            rotateVisibilitySO.ApplyModifiedPropertiesWithoutUndo();

            BuildSaveUI(canvasGO.transform, panelSprite, saveController);
            BuildExitUI(canvasGO.transform, panelSprite);
            BuildResourceHUD(canvasGO.transform);
            BuildMinimap(canvasGO.transform, panelSprite, minimapTexture, cameraRig);

            var iconLibrary = CreateResourceIconLibrary();

            var infoPanel = BuildBuildingInfoPanel(canvasGO.transform, panelSprite, iconLibrary);
            BuildBuildingSelector(canvasGO.transform, targetCamera, placer, infoPanel);
            BuildCitizenSelector(canvasGO.transform, targetCamera, placer);
            BuildSettlementTierToast(canvasGO.transform, settlementTierManager);
            BuildEventLog(canvasGO.transform, panelSprite);
            BuildTaxRateControl(canvasGO.transform, panelSprite);
            BuildSettlementStatus(canvasGO.transform, gameCalendar, settlementTierManager);
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
        /// Wedged into the x -810 column between the ResourceHUD's bottom edge (y 345) and the
        /// EventLog panel's top edge (y 260) -- centered at y 302 with a 60-tall frame keeps a
        /// ~12px clear margin on both sides instead of the two overlapping (an earlier pass at y
        /// 340/70-tall clipped into the ResourceHUD's leftmost icon column).
        /// </summary>
        private static void BuildTaxRateControl(Transform canvasParent, Sprite panelSprite)
        {
            var frame = CreateImage(canvasParent, "TaxRate", new Color(0.16f, 0.18f, 0.15f, 0.85f));
            frame.sprite = panelSprite;
            frame.type = Image.Type.Sliced;
            var frameRect = frame.GetComponent<RectTransform>();
            frameRect.anchorMin = frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameRect.anchoredPosition = new Vector2(-810f, 302f);
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
        /// SettlementTierToast's momentary flash for the same kind of milestone.
        /// </summary>
        private static void BuildEventLog(Transform canvasParent, Sprite panelSprite)
        {
            var frame = CreateImage(canvasParent, "EventLog", new Color(0.16f, 0.18f, 0.15f, 0.85f));
            frame.sprite = panelSprite;
            frame.type = Image.Type.Sliced;
            var frameRect = frame.GetComponent<RectTransform>();
            frameRect.anchorMin = frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameRect.anchoredPosition = new Vector2(-810f, 100f);
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
            rect.sizeDelta = new Vector2(1300f, 50f);

            var resourceTypes = new[] { ResourceType.Wood, ResourceType.Stone, ResourceType.Iron, ResourceType.Coal, ResourceType.Gold, ResourceType.Coins };
            var slotCount = resourceTypes.Length + 1; // + population slot
            var slotWidth = 1300f / slotCount;
            var amountTexts = new Text[resourceTypes.Length];

            for (var i = 0; i < resourceTypes.Length; i++)
            {
                var slotCenter = -650f + slotWidth * (i + 0.5f);

                var icon = CreateImage(root.transform, $"Icon_{resourceTypes[i]}", Color.white);
                icon.sprite = CreateResourceIcon(resourceTypes[i]);
                var iconRect = icon.GetComponent<RectTransform>();
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.anchoredPosition = new Vector2(slotCenter - slotWidth * 0.22f, 0f);
                iconRect.sizeDelta = new Vector2(32f, 32f);

                var amountText = CreateText(root.transform, $"Amount_{resourceTypes[i]}", "0", 24,
                    new Vector2(slotCenter + slotWidth * 0.14f, 0f), new Vector2(slotWidth * 0.6f, 50f));
                amountText.alignment = TextAnchor.MiddleLeft;
                amountTexts[i] = amountText;
            }

            var popCenter = -650f + slotWidth * (resourceTypes.Length + 0.5f);
            var popIcon = CreateImage(root.transform, "Icon_Population", Color.white);
            popIcon.sprite = CreatePopulationIcon();
            var popIconRect = popIcon.GetComponent<RectTransform>();
            popIconRect.anchorMin = popIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            popIconRect.anchoredPosition = new Vector2(popCenter - slotWidth * 0.22f, 0f);
            popIconRect.sizeDelta = new Vector2(32f, 32f);

            var populationText = CreateText(root.transform, "Amount_Population", "0", 24,
                new Vector2(popCenter + slotWidth * 0.14f, 0f), new Vector2(slotWidth * 0.6f, 50f));
            populationText.alignment = TextAnchor.MiddleLeft;

            var hud = root.AddComponent<ResourceHUDController>();
            var hudSO = new SerializedObject(hud);
            var resourceOrderProp = hudSO.FindProperty("resourceOrder");
            resourceOrderProp.arraySize = resourceTypes.Length;
            var amountTextsProp = hudSO.FindProperty("amountTexts");
            amountTextsProp.arraySize = amountTexts.Length;
            for (var i = 0; i < resourceTypes.Length; i++)
            {
                resourceOrderProp.GetArrayElementAtIndex(i).enumValueIndex = (int)resourceTypes[i];
                amountTextsProp.GetArrayElementAtIndex(i).objectReferenceValue = amountTexts[i];
            }
            hudSO.FindProperty("populationText").objectReferenceValue = populationText;
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

            var upgradeCostRow = CreateCostRow(upgradeControls.transform, "UpgradeCostRow", new Vector2(0f, -55f));
            var upgradeButton = CreateButton(upgradeControls.transform, panelSprite, "UpgradeButton", "Улучшить", new Vector2(0f, -125f), new Vector2(360f, 70f));

            var repairControls = new GameObject("RepairControls", typeof(RectTransform));
            repairControls.transform.SetParent(card.transform, false);
            StretchFull(repairControls.GetComponent<RectTransform>());

            var repairCostRow = CreateCostRow(repairControls.transform, "RepairCostRow", new Vector2(0f, -195f));
            var repairButton = CreateButton(repairControls.transform, panelSprite, "RepairButton", "Отремонтировать", new Vector2(0f, -265f), new Vector2(360f, 70f));

            var closeButton = CreateButton(card.transform, panelSprite, "CloseButton", "Закрыть", new Vector2(0f, -345f), new Vector2(300f, 70f));

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
            controllerSO.FindProperty("upgradeCostRow").objectReferenceValue = upgradeCostRow;
            controllerSO.FindProperty("repairControls").objectReferenceValue = repairControls;
            controllerSO.FindProperty("repairCostRow").objectReferenceValue = repairCostRow;
            controllerSO.FindProperty("iconLibrary").objectReferenceValue = iconLibrary;
            controllerSO.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(assignButton.onClick, controller.AssignWorker);
            UnityEventTools.AddPersistentListener(unassignButton.onClick, controller.UnassignWorker);
            UnityEventTools.AddPersistentListener(upgradeButton.onClick, controller.Upgrade);
            UnityEventTools.AddPersistentListener(repairButton.onClick, controller.Repair);
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

        /// <summary>
        /// Click-to-select-a-citizen-then-click-to-move-it (see CitizenSelector). The feedback
        /// label is dead center, large and shadowed like the main menu title so the momentary
        /// OK!/NO! flash reads clearly regardless of what's behind it; hidden until the first
        /// destination click.
        /// </summary>
        private static void BuildCitizenSelector(Transform canvasParent, Camera targetCamera, BuildingPlacer placer)
        {
            var feedbackText = CreateText(canvasParent, "CitizenMoveFeedback", string.Empty, 64, Vector2.zero, new Vector2(400f, 100f), addShadow: true);
            feedbackText.gameObject.SetActive(false);

            var go = new GameObject("CitizenSelector");
            go.transform.SetParent(canvasParent, false);
            var selector = go.AddComponent<CitizenSelector>();
            var so = new SerializedObject(selector);
            so.FindProperty("targetCamera").objectReferenceValue = targetCamera;
            so.FindProperty("buildingPlacer").objectReferenceValue = placer;
            so.FindProperty("feedbackText").objectReferenceValue = feedbackText;
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
            card.type = Image.Type.Sliced;
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
            card.type = Image.Type.Sliced;
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
            card.type = Image.Type.Sliced;
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
        private enum BuildingStyle { Hut, Fortification, Tower, Road }

        private static BuildingData CreateBuildingData(
            string id, string displayName, Vector2Int footprint, float height, Color wallColor, Color roofColor, List<ResourceAmount> cost,
            BuildingStyle style, Material trimMaterial, Material windowMaterial, Material doorMaterial = null,
            bool hasChimney = false, int citizensGranted = 0,
            int maxWorkers = 0, ResourceType producesResource = ResourceType.Wood, int productionPerTick = 0, float productionInterval = 6f,
            BuildingCategory category = BuildingCategory.Production, int maxHealth = 100, int defense = 0,
            bool isRoad = false, bool keepSelectedAfterPlacement = false, bool isWaterCategory = false, int fogRevealRadius = 8)
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
                case BuildingStyle.Road:
                    prefab = CreateRoadPrefab(id, wallColor, roofColor);
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
            data.isRoad = isRoad;
            data.keepSelectedAfterPlacement = keepSelectedAfterPlacement;
            data.isWaterCategory = isWaterCategory;
            data.fogRevealRadius = fogRevealRadius;

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
            string id, string displayName, Vector2Int footprint, Color deckColor, Color accentColor, List<ResourceAmount> cost,
            bool addWheel = false, bool addCrates = false, int maxWorkers = 0, ResourceType producesResource = ResourceType.Wood, int productionPerTick = 0,
            BuildingCategory category = BuildingCategory.Production, int maxHealth = 80, int defense = 0, int fogRevealRadius = 8)
        {
            var prefab = CreateWaterBuildingPrefab(id, footprint, deckColor, accentColor, addWheel, addCrates);

            var data = ScriptableObject.CreateInstance<BuildingData>();
            data.buildingName = id;
            data.displayName = displayName;
            data.prefab = prefab;
            data.footprintSize = footprint;
            data.cost = cost;
            data.maxWorkers = maxWorkers;
            data.producesResource = producesResource;
            data.productionPerWorkerPerTick = productionPerTick;
            data.productionIntervalSeconds = 6f;
            data.category = category;
            data.maxHealth = maxHealth;
            data.defense = defense;
            data.upgradeToLevel2Cost = ScaleCost(cost, 1.6f);
            data.upgradeToLevel3Cost = ScaleCost(cost, 2.8f);
            data.isWaterCategory = true;
            data.fogRevealRadius = fogRevealRadius;

            Directory.CreateDirectory(BuildingDataFolder);
            var dataPath = $"{BuildingDataFolder}/{id}.asset";
            DeleteIfExists(dataPath);
            AssetDatabase.CreateAsset(data, dataPath);
            return data;
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
            int maxHealth = 200, int defense = 0, List<ResourceAmount> upgradeToLevel2Cost = null, List<ResourceAmount> upgradeToLevel3Cost = null, int fogRevealRadius = 20)
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
            data.fogRevealRadius = fogRevealRadius;

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

            var waterMaterial = CreateWaterMaterial();

            var map = ScriptableObject.CreateInstance<MeshMapDefinition>();
            map.EditorInitialize(mapId, ground, water, waterPlacementZone, treesArea, new[] { tree1, tree2 }, waterMaterial);

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
                case BuildingCategory.Infrastructure:
                    return CreateIconSprite("Cat_Infrastructure", 64, (p, s) =>
                    {
                        FillIconRect(p, s, 0.1f, 0.42f, 0.9f, 0.58f, new Color(0.38f, 0.38f, 0.4f));
                        var stripe = new Color(0.88f, 0.78f, 0.3f);
                        FillIconRect(p, s, 0.18f, 0.47f, 0.34f, 0.53f, stripe);
                        FillIconRect(p, s, 0.46f, 0.47f, 0.62f, 0.53f, stripe);
                        FillIconRect(p, s, 0.74f, 0.47f, 0.86f, 0.53f, stripe);
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
            var icons = new Sprite[8]; // one slot per ResourceType value (Wood..Coins)
            icons[(int)ResourceType.Wood] = CreateResourceIcon(ResourceType.Wood);
            icons[(int)ResourceType.Stone] = CreateResourceIcon(ResourceType.Stone);
            icons[(int)ResourceType.Gold] = CreateResourceIcon(ResourceType.Gold);
            icons[(int)ResourceType.Iron] = CreateResourceIcon(ResourceType.Iron);
            icons[(int)ResourceType.Coal] = CreateResourceIcon(ResourceType.Coal);
            icons[(int)ResourceType.Coins] = CreateResourceIcon(ResourceType.Coins);
            icons[(int)ResourceType.Population] = CreatePopulationIcon();
            // Food has no icon yet -- no building cost currently includes it; left null,
            // BuildingInfoPanelController just renders an icon-less chip if that ever changes.

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
            image.type = Image.Type.Sliced;
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
