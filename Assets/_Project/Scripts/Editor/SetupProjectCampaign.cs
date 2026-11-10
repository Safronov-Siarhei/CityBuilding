using System.Collections.Generic;
using System.IO;
using CityBuilder.Campaign;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// The campaign's opening cutscene scene (the user, 2026-11-10: the film moves from Blender into
    /// Unity). Part one: the village, the sky and the morning light -- OpeningVillage lays the
    /// village out when the scene starts. Not in the build list yet: nothing loads it until the
    /// campaign does.
    /// </summary>
    public static partial class SetupProject
    {
        public const string CampaignOpeningScenePath = ScenesFolder + "/CampaignOpening.unity";
        private const string CinematicModelsFolder = "Assets/_Project/Models/Cinematic";
        private const string OpeningSetPath = "Assets/_Project/Campaign/OpeningSet.json";

        // the village's houses: the game's own buildings (OpeningVillage picks among them by id)
        private static readonly string[] OpeningVillageBuildings =
        {
            "Hovel", "Cottage", "Manor", "Baker", "Tavern", "Smokehouse", "Well", "Farm", "Barn",
            "Orchard", "PigFarm", "Windmill", "Granary",
        };

        private static void BuildCampaignOpeningScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGO = new GameObject("Cutscene Camera", typeof(Camera));
            cameraGO.tag = "MainCamera";
            var cam = cameraGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 400f;
            cam.fieldOfView = 40f;
            cameraGO.transform.SetPositionAndRotation(new Vector3(-9f, 8.5f, -15f), Quaternion.LookRotation(new Vector3(9f, -7f, 17f)));

            // a morning sun, low in the east and warm, as in the Blender film
            var sunGO = new GameObject("Sun", typeof(Light));
            var sun = sunGO.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.9f, 0.76f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            sunGO.transform.rotation = Quaternion.Euler(32f, 60f, 0f);
            RenderSettings.sun = sun;

            RenderSettings.skybox = CampaignSky();
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.72f, 0.86f);
            RenderSettings.ambientEquatorColor = new Color(0.58f, 0.62f, 0.6f);
            RenderSettings.ambientGroundColor = new Color(0.36f, 0.4f, 0.3f);
            // the distance fades into the morning haze, so the forest's edge never ends in a line
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.74f, 0.82f, 0.9f);
            RenderSettings.fogStartDistance = 45f;
            RenderSettings.fogEndDistance = 140f;

            var village = new GameObject("OpeningVillage").AddComponent<OpeningVillage>();
            var so = new SerializedObject(village);
            var props = so.FindProperty("buildings");
            props.arraySize = OpeningVillageBuildings.Length;
            for (var i = 0; i < OpeningVillageBuildings.Length; i++)
            {
                props.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue = OpeningVillageBuildings[i];
                props.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>($"{BuildingPrefabsFolder}/{OpeningVillageBuildings[i]}.prefab");
            }
            var treeModels = new List<GameObject>();
            for (var i = 1; i <= 10; i++)
            {
                var t = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsTerrainFolder}/Tree{i}.fbx");
                if (t != null) treeModels.Add(t);
            }
            var treesProp = so.FindProperty("trees");
            treesProp.arraySize = treeModels.Count;
            for (var i = 0; i < treeModels.Count; i++) treesProp.GetArrayElementAtIndex(i).objectReferenceValue = treeModels[i];
            var set = new List<GameObject>();
            if (Directory.Exists(CinematicModelsFolder))
            {
                foreach (var file in Directory.GetFiles(CinematicModelsFolder, "*.fbx"))
                {
                    var m = AssetDatabase.LoadAssetAtPath<GameObject>(file.Replace('\\', '/'));
                    if (m != null) set.Add(m);
                }
            }
            var setProp = so.FindProperty("setPieces");
            setProp.arraySize = set.Count;
            for (var i = 0; i < set.Count; i++)
            {
                setProp.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue = set[i].name;
                setProp.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue = set[i];
            }
            so.FindProperty("setFile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(OpeningSetPath);
            so.FindProperty("grass").objectReferenceValue = Matte("CampaignGrass", new Color(0.36f, 0.6f, 0.27f));
            so.FindProperty("cobble").objectReferenceValue = Matte("CampaignCobble", new Color(0.66f, 0.64f, 0.6f));
            so.FindProperty("dirt").objectReferenceValue = Matte("CampaignDirt", new Color(0.62f, 0.5f, 0.34f));
            so.FindProperty("field").objectReferenceValue = Matte("CampaignField", new Color(0.5f, 0.38f, 0.24f));
            so.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(ScenesFolder);
            DeleteIfExists(CampaignOpeningScenePath);
            EditorSceneManager.SaveScene(scene, CampaignOpeningScenePath);
        }

        /// <summary>A lit material with no shine: the default smoothness mirrored the sky's brown
        /// lower half in the grass seen from above.</summary>
        private static Material Matte(string name, Color color)
        {
            var m = CreateLitMaterial(name, color);
            m.SetFloat("_Smoothness", 0f);
            m.SetFloat("_SpecularHighlights", 0f);
            m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>A procedural morning sky: pale blue overhead, a warm haze at the horizon.</summary>
        private static Material CampaignSky()
        {
            var path = $"{MaterialsFolder}/CampaignSky.mat";
            Directory.CreateDirectory(MaterialsFolder);
            DeleteIfExists(path);
            var sky = new Material(Shader.Find("Skybox/Procedural"));
            sky.SetFloat("_SunDisk", 2f);
            sky.SetFloat("_SunSize", 0.035f);
            sky.SetFloat("_SunSizeConvergence", 6f);
            sky.SetFloat("_AtmosphereThickness", 0.75f);
            sky.SetColor("_SkyTint", new Color(0.5f, 0.6f, 0.78f));
            sky.SetColor("_GroundColor", new Color(0.42f, 0.5f, 0.38f));
            sky.SetFloat("_Exposure", 1.25f);
            AssetDatabase.CreateAsset(sky, path);
            return sky;
        }
    }
}
