using System.Collections.Generic;
using System.IO;
using CityBuilder.Buildings;
using CityBuilder.CameraControl;
using CityBuilder.Grid;
using CityBuilder.Resources;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CityBuilder.EditorTools
{
    public static class SetupProject
    {
        private const string ScenesFolder = "Assets/_Project/Scenes";
        private const string MaterialsFolder = "Assets/_Project/Materials";
        private const string BuildingPrefabsFolder = "Assets/_Project/Prefabs/Buildings";
        private const string BuildingDataFolder = "Assets/_Project/ScriptableObjects/Buildings";

        [MenuItem("CityBuilder/Setup Project Scene")]
        public static void Run()
        {
            var groundMaterial = CreateMaterial("Ground", new Color(0.35f, 0.55f, 0.25f));
            var houseData = CreateHouseBuilding();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(10f, 1f, 10f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

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
            rigSO.FindProperty("panBoundsMin").vector2Value = new Vector2(-48f, -48f);
            rigSO.FindProperty("panBoundsMax").vector2Value = new Vector2(48f, 48f);
            rigSO.ApplyModifiedPropertiesWithoutUndo();

            var managers = new GameObject("GameManagers");
            managers.AddComponent<GridManager>();
            managers.AddComponent<ResourceManager>();
            var placer = managers.AddComponent<BuildingPlacer>();
            var placerSO = new SerializedObject(placer);
            placerSO.FindProperty("targetCamera").objectReferenceValue = camera;
            var availableProp = placerSO.FindProperty("availableBuildings");
            availableProp.arraySize = 1;
            availableProp.GetArrayElementAtIndex(0).objectReferenceValue = houseData;
            placerSO.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(ScenesFolder);
            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/CityBuilder.unity");

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene($"{ScenesFolder}/CityBuilder.unity", true)
            };

            CleanupTemplateAssets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static BuildingData CreateHouseBuilding()
        {
            var houseMaterial = CreateMaterial("Building_House", new Color(0.55f, 0.35f, 0.2f));

            var houseGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            houseGO.name = "House";
            houseGO.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            houseGO.GetComponent<Renderer>().sharedMaterial = houseMaterial;
            houseGO.AddComponent<BuildingInstance>();

            Directory.CreateDirectory(BuildingPrefabsFolder);
            var prefab = PrefabUtility.SaveAsPrefabAsset(houseGO, $"{BuildingPrefabsFolder}/House.prefab");
            Object.DestroyImmediate(houseGO);

            var data = ScriptableObject.CreateInstance<BuildingData>();
            data.buildingName = "House";
            data.prefab = prefab;
            data.footprintSize = new Vector2Int(1, 1);
            data.cost = new List<ResourceAmount>
            {
                new ResourceAmount { type = ResourceType.Wood, amount = 10 }
            };

            Directory.CreateDirectory(BuildingDataFolder);
            AssetDatabase.CreateAsset(data, $"{BuildingDataFolder}/House.asset");
            return data;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            var path = $"{MaterialsFolder}/{name}.mat";
            Directory.CreateDirectory(MaterialsFolder);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void CleanupTemplateAssets()
        {
            AssetDatabase.DeleteAsset("Assets/Scenes");
            AssetDatabase.DeleteAsset("Assets/TutorialInfo");
            AssetDatabase.DeleteAsset("Assets/Readme.asset");
        }
    }
}
