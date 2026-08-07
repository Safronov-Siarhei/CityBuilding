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
        private const string TexturesFolder = "Assets/_Project/Textures";
        private const string ModelsFolder = "Assets/_Project/Models";
        private const string BuildingPrefabsFolder = "Assets/_Project/Prefabs/Buildings";
        private const string BuildingDataFolder = "Assets/_Project/ScriptableObjects/Buildings";

        private const int GridCellsX = 30;
        private const int GridCellsZ = 30;
        private const int ForestMarginCells = 10;
        private const float CellSize = 2f;
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
            var panelSprite = CreateRoundedRectSprite();

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

            var newGameButton = CreateButton(canvasGO.transform, panelSprite, "NewGameButton", "Новая игра", new Vector2(0f, 10f), new Vector2(460f, 120f));
            UnityEventTools.AddPersistentListener(newGameButton.onClick, menuController.StartNewGame);

            var quitButton = CreateButton(canvasGO.transform, panelSprite, "QuitButton", "Выход", new Vector2(0f, -150f), new Vector2(460f, 120f));
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
                "House", new Vector2Int(1, 1), height: 2f,
                wallColor: new Color(0.75f, 0.55f, 0.35f), roofColor: new Color(0.25f, 0.45f, 0.65f),
                cost: new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Wood, amount = 10 } });

            var townHallData = CreateBuildingData(
                "TownHall", new Vector2Int(5, 5), height: 4f,
                wallColor: new Color(0.75f, 0.72f, 0.68f), roofColor: new Color(0.5f, 0.14f, 0.14f),
                cost: new List<ResourceAmount>());

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

            CreateForestBorder(GridCellsX * CellSize * 0.5f, GridCellsZ * CellSize * 0.5f, groundWidth * 0.5f, groundDepth * 0.5f);

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

            BuildGameplayUI(placer, new List<BuildingData> { houseData });

            Directory.CreateDirectory(ScenesFolder);
            DeleteIfExists($"{ScenesFolder}/CityBuilder.unity");
            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/CityBuilder.unity");
        }

        private static void BuildGameplayUI(BuildingPlacer placer, List<BuildingData> hotbarBuildings)
        {
            // Created here (no NewScene() call happens between this and its uses below/this
            // scene being saved) rather than reused from BuildMainMenuScene's sprite, since that
            // one goes stale the moment this method's caller (BuildCityScene) switched scenes.
            var panelSprite = CreateRoundedRectSprite();

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
            CreateText(hintRoot.transform, "Text", "Выберите место для Ратуши (5x5)", 30, Vector2.zero, new Vector2(740f, 74f));

            // Touch-friendly building hotbar, shown only once the Town Hall is placed (there's
            // nothing else buildable before that point). Number-key hotkeys still work on PC.
            var hotbarGO = new GameObject("Hotbar", typeof(RectTransform));
            hotbarGO.transform.SetParent(canvasGO.transform, false);
            var hotbarRect = hotbarGO.GetComponent<RectTransform>();
            hotbarRect.anchorMin = new Vector2(0.5f, 0f);
            hotbarRect.anchorMax = new Vector2(0.5f, 0f);
            hotbarRect.pivot = new Vector2(0.5f, 0f);
            hotbarRect.anchoredPosition = new Vector2(0f, 28f);

            const float buttonSize = 150f;
            const float spacing = 22f;
            var totalWidth = hotbarBuildings.Count * buttonSize + Mathf.Max(0, hotbarBuildings.Count - 1) * spacing;
            hotbarRect.sizeDelta = new Vector2(totalWidth, buttonSize);

            for (var i = 0; i < hotbarBuildings.Count; i++)
            {
                var data = hotbarBuildings[i];
                var x = -totalWidth * 0.5f + buttonSize * 0.5f + i * (buttonSize + spacing);
                var button = CreateButton(hotbarGO.transform, panelSprite, $"Building_{data.buildingName}", data.buildingName, new Vector2(x, buttonSize * 0.5f), new Vector2(buttonSize, buttonSize));

                var handler = button.gameObject.AddComponent<HotbarButtonHandler>();
                var handlerSO = new SerializedObject(handler);
                handlerSO.FindProperty("buildingPlacer").objectReferenceValue = placer;
                handlerSO.FindProperty("building").objectReferenceValue = data;
                handlerSO.ApplyModifiedPropertiesWithoutUndo();

                UnityEventTools.AddPersistentListener(button.onClick, handler.SelectThisBuilding);
            }

            var visibility = canvasGO.AddComponent<BuildingPlacerUIVisibility>();
            var visibilitySO = new SerializedObject(visibility);
            visibilitySO.FindProperty("buildingPlacer").objectReferenceValue = placer;
            visibilitySO.FindProperty("showWhilePlacingMandatory").objectReferenceValue = hintRoot.gameObject;
            visibilitySO.FindProperty("hideWhilePlacingMandatory").objectReferenceValue = hotbarGO;
            visibilitySO.ApplyModifiedPropertiesWithoutUndo();
        }

        private static BuildingData CreateBuildingData(string name, Vector2Int footprint, float height, Color wallColor, Color roofColor, List<ResourceAmount> cost)
        {
            var prefab = CreateBuildingPrefab(name, footprint, height, wallColor, roofColor);

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

        private static GameObject CreateBuildingPrefab(string name, Vector2Int footprint, float height, Color wallColor, Color roofColor)
        {
            var sizeX = footprint.x * CellSize - BuildingInset;
            var sizeZ = footprint.y * CellSize - BuildingInset;
            var wallHeight = height * 0.6f;
            var roofHeight = height * 0.4f;
            const float roofOverhang = 1.08f;

            var root = new GameObject(name);
            root.AddComponent<BuildingInstance>();

            var walls = GameObject.CreatePrimitive(PrimitiveType.Cube);
            walls.name = "Walls";
            Object.DestroyImmediate(walls.GetComponent<BoxCollider>());
            walls.transform.SetParent(root.transform, false);
            walls.transform.localScale = new Vector3(sizeX, wallHeight, sizeZ);
            walls.transform.localPosition = new Vector3(0f, wallHeight * 0.5f, 0f);
            walls.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial($"Building_{name}_Walls", wallColor);

            var roofMesh = RoofMeshBuilder.BuildGableRoof(sizeX * roofOverhang, sizeZ * roofOverhang, roofHeight);
            Directory.CreateDirectory(ModelsFolder);
            var roofMeshPath = $"{ModelsFolder}/Roof_{name}.asset";
            DeleteIfExists(roofMeshPath);
            AssetDatabase.CreateAsset(roofMesh, roofMeshPath);

            var roofGO = new GameObject("Roof", typeof(MeshFilter), typeof(MeshRenderer));
            roofGO.transform.SetParent(root.transform, false);
            roofGO.transform.localPosition = new Vector3(0f, wallHeight, 0f);
            roofGO.GetComponent<MeshFilter>().sharedMesh = roofMesh;
            roofGO.GetComponent<MeshRenderer>().sharedMaterial = CreateLitMaterial($"Building_{name}_Roof", roofColor);

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

        private static void CreateForestBorder(float innerHalfWidth, float innerHalfDepth, float outerHalfWidth, float outerHalfDepth)
        {
            var root = new GameObject("Forest");
            var canopyMaterial = CreateLitMaterial("TreeCanopy", new Color(0.16f, 0.38f, 0.18f));

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

                var tree = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                tree.name = "Tree";
                Object.DestroyImmediate(tree.GetComponent<SphereCollider>());
                var scale = Random.Range(1.4f, 2.2f);
                tree.transform.localScale = new Vector3(scale, scale * 1.3f, scale);
                tree.transform.position = new Vector3(x, scale * 0.55f, z);
                tree.GetComponent<Renderer>().sharedMaterial = canopyMaterial;
                tree.transform.SetParent(root.transform, true);
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

        private static Material CreateGroundMaterial()
        {
            var path = $"{MaterialsFolder}/Ground.mat";
            DeleteIfExists(path);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.42f, 0.62f, 0.32f) };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Sprite CreateRoundedRectSprite()
        {
            const int size = 128;
            const int radius = 30;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var cx = Mathf.Clamp(x, radius, size - 1 - radius);
                    var cy = Mathf.Clamp(y, radius, size - 1 - radius);
                    var dx = x - cx;
                    var dy = y - cy;
                    var inside = (dx * dx + dy * dy) <= radius * radius;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, inside ? 1f : 0f));
                }
            }
            texture.Apply();

            Directory.CreateDirectory(TexturesFolder);
            var texPath = $"{TexturesFolder}/UI_Panel.asset";
            DeleteIfExists(texPath);
            AssetDatabase.CreateAsset(texture, texPath);

            var sprite = Sprite.Create(
                texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            sprite.name = "UI_Panel_Sprite";
            AssetDatabase.AddObjectToAsset(sprite, texture);
            AssetDatabase.ImportAsset(texPath);
            return sprite;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go.GetComponent<Image>();
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
