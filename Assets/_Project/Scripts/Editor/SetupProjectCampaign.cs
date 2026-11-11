using System.Collections.Generic;
using System.IO;
using CityBuilder.Campaign;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        private const string OpeningScriptPath = "Assets/_Project/Campaign/OpeningScript.json";
        private const string OpeningTracksPath = "Assets/_Project/Campaign/OpeningTracks.json";
        public const string CampaignAudioFolder = "Assets/_Project/Audio/Campaign";
        private const string ChimneysPath = "Assets/_Project/Campaign/Chimneys.json";
        private const string OpeningVolumePath = "Assets/_Project/Campaign/OpeningVolume.asset";

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
            lso.FindProperty("autoBegin").boolValue = false;            // the director starts it on its clock
            lso.ApplyModifiedPropertiesWithoutUndo();

            // the director: the film itself, with its subtitles drawn by the cutscene camera
            var canvasGO = new GameObject("Subtitles", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 0.5f;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 1f;
            var textGO = new GameObject("Line", typeof(RectTransform), typeof(Text), typeof(Outline));
            textGO.transform.SetParent(canvasGO.transform, false);
            var rect = (RectTransform)textGO.transform;
            rect.anchorMin = new Vector2(0.1f, 0f);
            rect.anchorMax = new Vector2(0.9f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 46f);
            rect.sizeDelta = new Vector2(0f, 80f);
            var line = textGO.GetComponent<Text>();
            line.font = CityBuilder.UI.UiTheme.Regular;
            line.fontSize = 34;
            line.alignment = TextAnchor.LowerCenter;
            line.color = Color.white;
            line.supportRichText = true;
            line.horizontalOverflow = HorizontalWrapMode.Wrap;
            line.raycastTarget = false;
            var outline = textGO.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);

            // the speech bubble over the speaker's head (the user, 2026-11-11: instead of subtitles)
            var bubbleGO = new GameObject("Bubble", typeof(RectTransform), typeof(Image), typeof(CanvasGroup),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            bubbleGO.transform.SetParent(canvasGO.transform, false);
            var brect = (RectTransform)bubbleGO.transform;
            brect.anchorMin = brect.anchorMax = new Vector2(0.5f, 0.5f);
            brect.pivot = new Vector2(0.5f, 0f);
            var bimg = bubbleGO.GetComponent<Image>();
            bimg.sprite = BubbleSprite("BubbleBody", false);
            bimg.type = Image.Type.Sliced;
            bimg.raycastTarget = false;
            var vlg = bubbleGO.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(26, 26, 16, 18);
            vlg.childControlWidth = vlg.childControlHeight = true;
            vlg.childForceExpandWidth = vlg.childForceExpandHeight = false;
            var fit = bubbleGO.GetComponent<ContentSizeFitter>();
            fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            bubbleGO.GetComponent<CanvasGroup>().blocksRaycasts = false;
            var btextGO = new GameObject("Line", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            btextGO.transform.SetParent(bubbleGO.transform, false);
            var btext = btextGO.GetComponent<Text>();
            btext.font = CityBuilder.UI.UiTheme.Regular;
            btext.fontSize = 30;
            btext.alignment = TextAnchor.MiddleCenter;
            btext.color = new Color(0.22f, 0.13f, 0.06f);
            btext.horizontalOverflow = HorizontalWrapMode.Wrap;
            btext.raycastTarget = false;
            var tailGO = new GameObject("Tail", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            tailGO.transform.SetParent(bubbleGO.transform, false);
            tailGO.GetComponent<LayoutElement>().ignoreLayout = true;
            var tail = (RectTransform)tailGO.transform;
            tail.anchorMin = tail.anchorMax = new Vector2(0.5f, 0f);
            tail.pivot = new Vector2(0.5f, 1f);
            tail.sizeDelta = new Vector2(34f, 30f);
            tail.anchoredPosition = new Vector2(0f, 9f);       // over the bubble's own rim, so the rim opens into it
            var timg = tailGO.GetComponent<Image>();
            timg.sprite = BubbleSprite("BubbleTail", true);
            timg.raycastTarget = false;
            bubbleGO.SetActive(false);

            // the title over the first wide shot
            var titleGO = new GameObject("Title", typeof(RectTransform), typeof(Text), typeof(Outline));
            titleGO.transform.SetParent(canvasGO.transform, false);
            var trect = (RectTransform)titleGO.transform;
            trect.anchorMin = new Vector2(0.05f, 0.5f);
            trect.anchorMax = new Vector2(0.95f, 0.5f);
            trect.pivot = new Vector2(0.5f, 0.5f);
            trect.anchoredPosition = new Vector2(0f, 90f);
            trect.sizeDelta = new Vector2(0f, 120f);
            var title = titleGO.GetComponent<Text>();
            title.font = CityBuilder.UI.UiTheme.Regular;
            title.fontSize = 56;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(0.98f, 0.9f, 0.68f);
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.raycastTarget = false;
            var toutline = titleGO.GetComponent<Outline>();
            toutline.effectColor = new Color(0.1f, 0.06f, 0.02f, 0.9f);
            toutline.effectDistance = new Vector2(3f, -3f);

            // the film's effects: the portal, the sky turning, the post, the chimney smoke
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var volume = new GameObject("FilmVolume", typeof(Volume)).GetComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = OpeningVolume();
            var fx = village.gameObject.AddComponent<OpeningFx>();
            var fso = new SerializedObject(fx);
            fso.FindProperty("village").objectReferenceValue = village;
            fso.FindProperty("scriptFile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(OpeningScriptPath);
            fso.FindProperty("tracksFile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(OpeningTracksPath);
            fso.FindProperty("chimneysFile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(ChimneysPath);
            fso.FindProperty("vortexMaterial").objectReferenceValue = ShaderMaterial("CampaignVortex", "CityBuilder/CineVortex");
            fso.FindProperty("groundMaterial").objectReferenceValue = ShaderMaterial("CampaignGroundFx", "CityBuilder/CineGroundFx");
            fso.FindProperty("sparkMaterial").objectReferenceValue = ParticleMaterial("CampaignSpark", dot, true);
            fso.FindProperty("dustMaterial").objectReferenceValue = ParticleMaterial("CampaignDust", dot, false);
            fso.FindProperty("debrisMaterial").objectReferenceValue = Matte("CampaignDebris", new Color(0.55f, 0.53f, 0.5f));
            fso.FindProperty("stormMaterial").objectReferenceValue = ShaderMaterial("CampaignStormSky", "CityBuilder/CineStormSky");
            fso.FindProperty("volume").objectReferenceValue = volume;
            fso.FindProperty("title").objectReferenceValue = title;
            fso.ApplyModifiedPropertiesWithoutUndo();

            // the film's sound: every clip of Audio/Campaign, played by the cue sheet in OpeningScript.json
            var sound = new GameObject("FilmSound").AddComponent<OpeningSound>();
            var sso = new SerializedObject(sound);
            sso.FindProperty("scriptFile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(OpeningScriptPath);
            var audio = new List<AudioClip>();
            if (Directory.Exists(CampaignAudioFolder))
            {
                foreach (var file in Directory.GetFiles(CampaignAudioFolder, "*.ogg"))
                {
                    var a = AssetDatabase.LoadAssetAtPath<AudioClip>(file.Replace('\\', '/'));
                    if (a != null) audio.Add(a);
                }
            }
            var clipsProp = sso.FindProperty("clips");
            clipsProp.arraySize = audio.Count;
            for (var i = 0; i < audio.Count; i++) clipsProp.GetArrayElementAtIndex(i).objectReferenceValue = audio[i];
            sso.ApplyModifiedPropertiesWithoutUndo();
            cameraGO.AddComponent<AudioListener>();

            var director = new GameObject("OpeningDirector").AddComponent<OpeningDirector>();
            var dso = new SerializedObject(director);
            dso.FindProperty("village").objectReferenceValue = village;
            dso.FindProperty("life").objectReferenceValue = life;
            dso.FindProperty("scriptFile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(OpeningScriptPath);
            dso.FindProperty("tracksFile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(OpeningTracksPath);
            dso.FindProperty("cam").objectReferenceValue = cam;
            dso.FindProperty("subtitle").objectReferenceValue = line;
            dso.FindProperty("bubble").objectReferenceValue = brect;
            dso.FindProperty("bubbleText").objectReferenceValue = btext;
            dso.FindProperty("bubbleTail").objectReferenceValue = tail;
            dso.FindProperty("fx").objectReferenceValue = fx;
            dso.FindProperty("sound").objectReferenceValue = sound;
            dso.FindProperty("ropeMaterial").objectReferenceValue = Matte("CampaignRope", new Color(0.55f, 0.42f, 0.26f));
            var dcast = dso.FindProperty("cast");
            dcast.arraySize = castProp.arraySize;
            for (var i = 0; i < castProp.arraySize; i++)
            {
                var src = castProp.GetArrayElementAtIndex(i);
                var dst = dcast.GetArrayElementAtIndex(i);
                dst.FindPropertyRelative("id").stringValue = src.FindPropertyRelative("id").stringValue;
                dst.FindPropertyRelative("model").objectReferenceValue = src.FindPropertyRelative("model").objectReferenceValue;
                dst.FindPropertyRelative("clips").objectReferenceValue = src.FindPropertyRelative("clips").objectReferenceValue;
            }
            dso.ApplyModifiedPropertiesWithoutUndo();

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

        /// <summary>The speech bubble's pictures, drawn here: a parchment-white rounded body with a dark
        /// brown rim (9-sliced), and its tail -- a triangle pointing down with the rim on its two slanted
        /// sides only, its top laid over the body's rim.</summary>
        private static Sprite BubbleSprite(string name, bool tail)
        {
            var path = $"Assets/_Project/Campaign/{name}.png";
            if (!File.Exists(path))
            {
                var w = tail ? 68 : 96;
                var h = tail ? 60 : 96;
                var fill = new Color(1f, 0.97f, 0.9f, 1f);
                var rim = new Color(0.25f, 0.15f, 0.07f, 1f);
                const float line = 5f;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                for (var y = 0; y < h; y++)
                {
                    for (var x = 0; x < w; x++)
                    {
                        float d;                                    // signed distance, negative inside
                        if (tail)
                        {
                            // the triangle (0,h)-(w,h)-(w/2,0) in pixels; the top edge has no rim
                            var px = x + 0.5f;
                            var py = y + 0.5f;
                            var ax = Mathf.Abs(px - w * 0.5f);
                            var slope = (w * 0.5f) / h;
                            var side = (ax - py * slope) / Mathf.Sqrt(1f + slope * slope);
                            d = side;
                        }
                        else
                        {
                            const float r = 30f;
                            var qx = Mathf.Max(Mathf.Abs(x + 0.5f - w * 0.5f) - (w * 0.5f - r - 3f), 0f);
                            var qy = Mathf.Max(Mathf.Abs(y + 0.5f - h * 0.5f) - (h * 0.5f - r - 3f), 0f);
                            d = Mathf.Sqrt(qx * qx + qy * qy) - r;
                        }
                        Color c;
                        if (d > 0.5f) c = Color.clear;
                        else if (d > -line) c = rim;
                        else c = fill;
                        if (d > -0.5f && d <= 0.5f) c.a = 0.5f - d;    // a soft outer edge
                        tex.SetPixel(x, y, c);
                    }
                }
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.textureType = TextureImporterType.Sprite;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                if (!tail) imp.spriteBorder = new Vector4(40f, 40f, 40f, 40f);
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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

        /// <summary>A material on one of the film's own shaders.</summary>
        private static Material ShaderMaterial(string name, string shader)
        {
            var path = $"{MaterialsFolder}/{name}.mat";
            DeleteIfExists(path);
            var m = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        /// <summary>The film's post: bloom, a touch of grading, a vignette, depth of field (off until
        /// a close shot asks for it). The numbers are set from OpeningScript.json's fx block at play.</summary>
        private static VolumeProfile OpeningVolume()
        {
            DeleteIfExists(OpeningVolumePath);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, OpeningVolumePath);
            void Add<T>() where T : VolumeComponent
            {
                var c = profile.Add<T>(true);
                c.name = typeof(T).Name;
                AssetDatabase.AddObjectToAsset(c, profile);
            }
            Add<Bloom>();
            Add<Vignette>();
            Add<ColorAdjustments>();
            Add<DepthOfField>();
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
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
