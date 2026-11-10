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
        private const string OpeningLifePath = "Assets/_Project/Campaign/OpeningLife.json";

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

            // the living background: villagers, their flight, the orcs and the fires
            var clips = BakeCastClips();
            var life = village.gameObject.AddComponent<OpeningLife>();
            var lso = new SerializedObject(life);
            lso.FindProperty("village").objectReferenceValue = village;
            var castProp = lso.FindProperty("cast");
            castProp.arraySize = clips.Count;
            var n = 0;
            foreach (var kv in clips)
            {
                var e = castProp.GetArrayElementAtIndex(n++);
                e.FindPropertyRelative("id").stringValue = kv.Key;
                e.FindPropertyRelative("model").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelImportDefaults.CinematicCastFolder}{kv.Key}.fbx");
                e.FindPropertyRelative("clips").objectReferenceValue = kv.Value;
            }
            lso.FindProperty("config").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(OpeningLifePath);
            var dot = SoftDot();
            lso.FindProperty("flameMaterial").objectReferenceValue = ParticleMaterial("CampaignFlame", dot, true);
            lso.FindProperty("smokeMaterial").objectReferenceValue = ParticleMaterial("CampaignSmoke", dot, false);
            lso.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(ScenesFolder);
            DeleteIfExists(CampaignOpeningScenePath);
            EditorSceneManager.SaveScene(scene, CampaignOpeningScenePath);
        }

        // ------------------------------------------------------------ the cast's clips

        private const string CastClipsFolder = "Assets/_Project/Campaign/Cast";

        [System.Serializable]
        private class CastFile
        {
            public CastFigure[] figures;
        }

        [System.Serializable]
        private class CastFigure
        {
            public string name;
            public float walkSpeed;
            public float runSpeed;
        }

        /// <summary>Bakes every cast FBX's takes into a CineClips asset (CineFigure plays them by
        /// hand: an imported clip poses nothing in the editor, and no Animator is needed in play).</summary>
        private static Dictionary<string, CineClips> BakeCastClips()
        {
            var result = new Dictionary<string, CineClips>();
            var folder = ModelImportDefaults.CinematicCastFolder.TrimEnd('/');
            if (!Directory.Exists(folder)) return result;
            Directory.CreateDirectory(CastClipsFolder);
            var speeds = new Dictionary<string, CastFigure>();
            var castJson = AssetDatabase.LoadAssetAtPath<TextAsset>(ModelImportDefaults.OpeningCastPath);
            if (castJson != null)
            {
                foreach (var f in JsonUtility.FromJson<CastFile>(castJson.text).figures) speeds[f.name] = f;
            }
            foreach (var file in Directory.GetFiles(folder, "*.fbx"))
            {
                var path = file.Replace('\\', '/');
                var name = Path.GetFileNameWithoutExtension(path);
                var takes = new List<AnimationClip>();
                foreach (var a in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (a is AnimationClip c && !c.name.StartsWith("__preview")) takes.Add(c);
                }
                if (takes.Count == 0) continue;

                // the bones: every path with a rotation curve; the moving ones: a position that changes
                var bones = new List<string>();
                var moving = new HashSet<string>();
                foreach (var c in takes)
                {
                    foreach (var b in AnimationUtility.GetCurveBindings(c))
                    {
                        if (b.propertyName == "m_LocalRotation.w" && !bones.Contains(b.path)) bones.Add(b.path);
                        if (b.propertyName.StartsWith("m_LocalPosition"))
                        {
                            var cv = AnimationUtility.GetEditorCurve(c, b);
                            var lo = float.MaxValue;
                            var hi = float.MinValue;
                            foreach (var k in cv.keys)
                            {
                                lo = Mathf.Min(lo, k.value);
                                hi = Mathf.Max(hi, k.value);
                            }
                            if (hi - lo > 1e-4f) moving.Add(b.path);
                        }
                    }
                }
                var movingIdx = new List<int>();
                for (var i = 0; i < bones.Count; i++) if (moving.Contains(bones[i])) movingIdx.Add(i);

                var set = ScriptableObject.CreateInstance<CineClips>();
                set.bones = bones.ToArray();
                set.movingBones = movingIdx.ToArray();
                var list = new List<CineClips.Clip>();
                foreach (var c in takes)
                {
                    var curves = new Dictionary<string, AnimationCurve>();
                    foreach (var b in AnimationUtility.GetCurveBindings(c)) curves[b.path + "|" + b.propertyName] = AnimationUtility.GetEditorCurve(c, b);
                    float Value(string bone, string prop, float t, float rest) =>
                        curves.TryGetValue(bone + "|" + prop, out var cv) ? cv.Evaluate(t) : rest;
                    var frames = Mathf.Max(2, Mathf.RoundToInt(c.length * CineClips.FramesPerSecond) + 1);
                    var rot = new float[frames * bones.Count * 4];
                    var pos = new float[frames * movingIdx.Count * 3];
                    for (var f = 0; f < frames; f++)
                    {
                        var t = c.length * f / (frames - 1);
                        for (var i = 0; i < bones.Count; i++)
                        {
                            var q = new Quaternion(Value(bones[i], "m_LocalRotation.x", t, 0f), Value(bones[i], "m_LocalRotation.y", t, 0f),
                                Value(bones[i], "m_LocalRotation.z", t, 0f), Value(bones[i], "m_LocalRotation.w", t, 1f)).normalized;
                            var o = (f * bones.Count + i) * 4;
                            rot[o] = q.x;
                            rot[o + 1] = q.y;
                            rot[o + 2] = q.z;
                            rot[o + 3] = q.w;
                        }
                        for (var m = 0; m < movingIdx.Count; m++)
                        {
                            var bone = bones[movingIdx[m]];
                            var o = (f * movingIdx.Count + m) * 3;
                            pos[o] = Value(bone, "m_LocalPosition.x", t, 0f);
                            pos[o + 1] = Value(bone, "m_LocalPosition.y", t, 0f);
                            pos[o + 2] = Value(bone, "m_LocalPosition.z", t, 0f);
                        }
                    }
                    list.Add(new CineClips.Clip { name = c.name, seconds = c.length, loop = AnimationUtility.GetAnimationClipSettings(c).loopTime, frames = frames, rot = rot, pos = pos });
                }
                set.clips = list.ToArray();
                if (speeds.TryGetValue(name, out var sp))
                {
                    set.walkSpeed = sp.walkSpeed;
                    set.runSpeed = sp.runSpeed;
                }
                var assetPath = $"{CastClipsFolder}/{name}Clips.asset";
                DeleteIfExists(assetPath);
                AssetDatabase.CreateAsset(set, assetPath);
                result[name] = set;
            }
            AssetDatabase.SaveAssets();
            return result;
        }

        /// <summary>A soft round dot for the fire's and smoke's particles (white, alpha falling to the rim).</summary>
        private static Texture2D SoftDot()
        {
            var path = "Assets/_Project/Campaign/SoftDot.png";
            if (!File.Exists(path))
            {
                const int size = 64;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var d = new Vector2(x - size / 2f + 0.5f, y - size / 2f + 0.5f).magnitude / (size / 2f);
                        var a = Mathf.Clamp01(1f - d);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a * (3f - 2f * a)));
                    }
                }
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>URP's unlit particle material: additive for flames, alpha-blended for smoke.</summary>
        private static Material ParticleMaterial(string name, Texture2D dot, bool additive)
        {
            var path = $"{MaterialsFolder}/{name}.mat";
            DeleteIfExists(path);
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            m.SetTexture("_BaseMap", dot);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", additive ? 2f : 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(m, path);
            return m;
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
