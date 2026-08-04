using System.Collections.Generic;
using System.IO;
using CityBuilder.Buildings;
using CityBuilder.CameraControl;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Resources;
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
        private const string ModelsFolder = "Assets/_Project/Models";
        private const string BuildingPrefabsFolder = "Assets/_Project/Prefabs/Buildings";
        private const string BuildingDataFolder = "Assets/_Project/ScriptableObjects/Buildings";

        private const int GridCellsX = 30;
        private const int GridCellsZ = 30;
        private const float CellSize = 2f;
        private const float CubeGap = 0.08f;
        private const float GroundHeight = 1f;
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

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void BuildMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var background = CreateImage(canvasGO.transform, "Background", new Color(0.12f, 0.14f, 0.11f, 1f));
            StretchFull(background.GetComponent<RectTransform>());

            CreateText(canvasGO.transform, "Title", "СТРОИТЕЛЬ ГОРОДОВ", 64, new Vector2(0f, 220f), new Vector2(900f, 120f));

            var menuController = canvasGO.AddComponent<MainMenuController>();

            var newGameButton = CreateButton(canvasGO.transform, "NewGameButton", "Новая игра", new Vector2(0f, 20f), new Vector2(360f, 70f));
            UnityEventTools.AddPersistentListener(newGameButton.onClick, menuController.StartNewGame);

            var quitButton = CreateButton(canvasGO.transform, "QuitButton", "Выход", new Vector2(0f, -70f), new Vector2(360f, 70f));
            UnityEventTools.AddPersistentListener(quitButton.onClick, menuController.QuitGame);

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
            var houseData = CreateBuildingData(
                "House", new Vector2Int(1, 1), height: 2f, color: new Color(0.55f, 0.35f, 0.2f),
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Wood, amount = 10 } });

            var townHallData = CreateBuildingData(
                "TownHall", new Vector2Int(5, 5), height: 4f, color: new Color(0.62f, 0.6f, 0.65f),
                cost: new List<ResourceAmount>());

            AssetDatabase.SaveAssets();

            var groundMesh = CubeMeshBuilder.BuildGrid(GridCellsX, GridCellsZ, CellSize, CubeGap, GroundHeight, GroundOrigin);
            Directory.CreateDirectory(ModelsFolder);
            DeleteIfExists($"{ModelsFolder}/Ground_CubeGrid.asset");
            AssetDatabase.CreateAsset(groundMesh, $"{ModelsFolder}/Ground_CubeGrid.asset");

            var groundMaterial = CreateMaterial("Ground", new Color(0.35f, 0.55f, 0.25f));

            var ground = new GameObject("Ground", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            ground.GetComponent<MeshFilter>().sharedMesh = groundMesh;
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMaterial;
            ground.GetComponent<MeshCollider>().sharedMesh = groundMesh;

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
            availableProp.arraySize = 1;
            availableProp.GetArrayElementAtIndex(0).objectReferenceValue = houseData;
            placerSO.ApplyModifiedPropertiesWithoutUndo();

            BuildHintUI(placer);

            Directory.CreateDirectory(ScenesFolder);
            DeleteIfExists($"{ScenesFolder}/CityBuilder.unity");
            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/CityBuilder.unity");
        }

        private static void BuildHintUI(BuildingPlacer placer)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var hintRoot = CreateImage(canvasGO.transform, "PlacementHint", new Color(0f, 0f, 0f, 0.55f));
            var hintRect = hintRoot.GetComponent<RectTransform>();
            hintRect.anchorMin = hintRect.anchorMax = new Vector2(0.5f, 1f);
            hintRect.anchoredPosition = new Vector2(0f, -60f);
            hintRect.sizeDelta = new Vector2(720f, 64f);
            CreateText(hintRoot.transform, "Text", "Выберите место для Ратуши (5x5)", 28, Vector2.zero, new Vector2(700f, 60f));

            var hintUI = canvasGO.AddComponent<PlacementHintUI>();
            var hintSO = new SerializedObject(hintUI);
            hintSO.FindProperty("buildingPlacer").objectReferenceValue = placer;
            hintSO.FindProperty("hintRoot").objectReferenceValue = hintRoot.gameObject;
            hintSO.ApplyModifiedPropertiesWithoutUndo();
        }

        private static BuildingData CreateBuildingData(string name, Vector2Int footprint, float height, Color color, List<ResourceAmount> cost)
        {
            var prefab = CreateBuildingPrefab(name, footprint, height, color);

            var data = ScriptableObject.CreateInstance<BuildingData>();
            data.buildingName = name;
            data.prefab = prefab;
            data.footprintSize = footprint;
            data.cost = cost;

            Directory.CreateDirectory(BuildingDataFolder);
            var dataPath = $"{BuildingDataFolder}/{name}.asset";
            DeleteIfExists(dataPath);
            AssetDatabase.CreateAsset(data, dataPath);
            return data;
        }

        private static GameObject CreateBuildingPrefab(string name, Vector2Int footprint, float height, Color color)
        {
            var sizeX = footprint.x * CellSize - CubeGap;
            var sizeZ = footprint.y * CellSize - CubeGap;

            var root = new GameObject(name);
            root.AddComponent<BuildingInstance>();

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            Object.DestroyImmediate(visual.GetComponent<BoxCollider>());
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = new Vector3(sizeX, height, sizeZ);
            visual.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            visual.GetComponent<Renderer>().sharedMaterial = CreateMaterial($"Building_{name}", color);

            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(sizeX, height, sizeZ);
            collider.center = new Vector3(0f, height * 0.5f, 0f);

            Directory.CreateDirectory(BuildingPrefabsFolder);
            var prefabPath = $"{BuildingPrefabsFolder}/{name}.prefab";
            DeleteIfExists(prefabPath);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            var path = $"{MaterialsFolder}/{name}.mat";
            Directory.CreateDirectory(MaterialsFolder);
            DeleteIfExists(path);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go.GetComponent<Image>();
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, Vector2 anchoredPos, Vector2 sizeDelta)
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
            text.color = Color.white;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;
            go.GetComponent<Image>().color = new Color(0.22f, 0.22f, 0.26f, 0.95f);

            CreateText(go.transform, "Label", label, 26, Vector2.zero, sizeDelta);
            return go.GetComponent<Button>();
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
        }
    }
}
