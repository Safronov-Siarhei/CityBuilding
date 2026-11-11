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
        public const string CampaignMapScenePath = ScenesFolder + "/CampaignMap.unity";

        /// <summary>The campaign map (the user, 2026-11-12): banner -> loading -> this map -> a level ->
        /// «Играть». A camera, and CampaignMapScreen, which draws the rest at run time.</summary>
        private static void BuildCampaignMapScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Main Camera", typeof(Camera));
            cam.tag = "MainCamera";
            var c = cam.GetComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.16f, 0.11f, 0.07f);
            cam.AddComponent<AudioListener>();
            var screen = new GameObject("CampaignMapScreen").AddComponent<CityBuilder.Campaign.CampaignMapScreen>();
            var so = new SerializedObject(screen);
            so.FindProperty("mapFile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Campaign/CampaignMap.json");
            void Art(string field, string path)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) Debug.LogError($"[Campaign] map sprite {path} not found");
                so.FindProperty(field).objectReferenceValue = sprite;
            }
            // the chart came in before its folder was a UI one: import it again as a sprite
            AssetDatabase.ImportAsset(UiTextureImport.CampaignFolder + "/CampaignMap_Wide.png", ImportAssetOptions.ForceUpdate);
            Art("picture", UiTextureImport.CampaignFolder + "/CampaignMap_Wide.png");
            Art("backGlyph", UiTextureImport.GlyphsFolder + "/Hud_Back.png");
            Art("playGlyph", UiTextureImport.GlyphsFolder + "/Hud_Play.png");
            Art("replayGlyph", UiTextureImport.GlyphsFolder + "/Hud_Rotate.png");
            Art("lockGlyph", UiTextureImport.GlyphsFolder + "/Hud_Lock.png");
            so.ApplyModifiedPropertiesWithoutUndo();
            Directory.CreateDirectory(ScenesFolder);
            DeleteIfExists(CampaignMapScenePath);
            EditorSceneManager.SaveScene(scene, CampaignMapScenePath);
        }
        private const string CinematicModelsFolder = "Assets/_Project/Models/Cinematic";
        private const string OpeningSetPath = "Assets/_Project/Campaign/OpeningSet.json";
        private const string OpeningLifePath = "Assets/_Project/Campaign/OpeningLife.json";
        private const string OpeningScriptPath = "Assets/_Project/Campaign/OpeningScript.json";
        private const string OpeningTracksPath = "Assets/_Project/Campaign/OpeningTracks.json";
        public const string CampaignAudioFolder = "Assets/_Project/Audio/Campaign";
        private const string ChimneysPath = "Assets/_Project/Campaign/Chimneys.json";
        private const string Mission1Path = "Assets/_Project/Campaign/Mission1.json";
        private const string OpeningVolumePath = "Assets/_Project/Campaign/OpeningVolume.asset";

        // the village's houses: the game's own buildings (OpeningVillage picks among them by id)
        private static readonly string[] OpeningVillageBuildings =
        {
            "Hovel", "Cottage", "Manor", "Baker", "Tavern", "Smokehouse", "Well", "Farm", "Barn",
            "Orchard", "PigFarm", "Windmill", "Granary",
        };

        [System.Serializable] private class RouteModelNames { public string[] models; }

        private static void BuildCampaignOpeningScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGO = new GameObject("Cutscene Camera", typeof(Camera));
            cameraGO.tag = "MainCamera";
            var cam = cameraGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.nearClipPlane = 0.25f;                       // depth precision for the flat ground layers far off
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
            so.FindProperty("propsFile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Campaign/OpeningProps.json");
            // the ground: the islands' grass zones, cobbles, rutted lanes, furrowed fields (CineGround)
            so.FindProperty("grass").objectReferenceValue = GroundMaterial("CampaignGrass", 0f);
            so.FindProperty("cobble").objectReferenceValue = GroundMaterial("CampaignCobble", 1f);
            so.FindProperty("dirt").objectReferenceValue = GroundMaterial("CampaignDirt", 2f);
            so.FindProperty("field").objectReferenceValue = GroundMaterial("CampaignField", 3f);
            // mission 1's route (the approved map): its plan, the river and the wheat's body, and the
            // sandbox's own models it uses (bridge, Barbican, wall, towers) named in Mission1.json
            so.FindProperty("routeFile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(Mission1Path);
            so.FindProperty("water").objectReferenceValue = GroundMaterial("CampaignWater", 4f);
            so.FindProperty("wheatBody").objectReferenceValue = GroundMaterial("CampaignWheat", 5f);
            var routeNames = JsonUtility.FromJson<RouteModelNames>(File.ReadAllText(Mission1Path)).models ?? new string[0];
            var routeProp = so.FindProperty("routeModels");
            routeProp.arraySize = routeNames.Length;
            for (var i = 0; i < routeNames.Length; i++)
            {
                // a bare name is a sandbox building; "Fauna/FaunaDeer" is any folder under Models
                var file = routeNames[i].Contains("/") ? $"Assets/_Project/Models/{routeNames[i]}.fbx" : $"Assets/_Project/Models/Buildings/{routeNames[i]}.fbx";
                var m = AssetDatabase.LoadAssetAtPath<GameObject>(file);
                if (m == null) Debug.LogError($"[Campaign] route model {routeNames[i]} not found");
                routeProp.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue = routeNames[i].Substring(routeNames[i].LastIndexOf('/') + 1);
                routeProp.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue = m;
            }
            // and the islands' meadow props: Map 3's decor kit and its material
            var map3 = AssetDatabase.LoadAssetAtPath<CityBuilder.Maps.MeshMapDefinition>("Assets/_Project/Resources/MeshMaps/Map3.asset");
            if (map3 != null)
            {
                so.FindProperty("decorKit").objectReferenceValue = map3.DecorData;
                so.FindProperty("decorMaterial").objectReferenceValue = map3.DecorMaterial;
            }
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
            // the line at the bottom middle, white with a soft shadow, kept to the middle half of the
            // screen so a long line wraps instead of running edge to edge (the user, 2026-11-11)
            var textGO = new GameObject("Line", typeof(RectTransform), typeof(Text), typeof(Outline), typeof(Shadow));
            textGO.transform.SetParent(canvasGO.transform, false);
            var rect = (RectTransform)textGO.transform;
            rect.anchorMin = new Vector2(0.22f, 0f);
            rect.anchorMax = new Vector2(0.78f, 0f);
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
            outline.effectColor = new Color(0f, 0f, 0f, 0.55f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            var shadow = textGO.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            shadow.effectDistance = new Vector2(3f, -4f);

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

            // mission 1 takes over where the film ends: the game's camera over Ардан and the floating stick
            var mission = new GameObject("MissionOne").AddComponent<MissionOne>();
            var mso = new SerializedObject(mission);
            mso.FindProperty("director").objectReferenceValue = director;
            mso.FindProperty("village").objectReferenceValue = village;
            mso.FindProperty("cam").objectReferenceValue = cam;
            mso.FindProperty("configFile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(Mission1Path);
            mso.FindProperty("darkMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/DungeonDark.mat");
            mso.FindProperty("music").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Campaign/mus_mission1.ogg");
            mso.FindProperty("roar").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Campaign/sfx_roar.ogg");
            var art = mso.FindProperty("hudArt");
            void ArtSprite(string field, string path)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) Debug.LogError($"[Campaign] HUD sprite {path} not found");
                art.FindPropertyRelative(field).objectReferenceValue = sprite;
            }
            ArtSprite("disc", UiTextureImport.SkinFolder + "/event_disc.png");
            ArtSprite("rim", UiTextureImport.SkinFolder + "/event_rim.png");
            ArtSprite("mask", UiTextureImport.SkinFolder + "/circle_mask.png");
            ArtSprite("panel", UiTextureImport.SkinFolder + "/panel.png");
            ArtSprite("pillParchment", UiTextureImport.SkinFolder + "/pill_parchment.png");
            ArtSprite("pillWood", UiTextureImport.SkinFolder + "/pill_wood.png");
            ArtSprite("menuGlyph", UiTextureImport.GlyphsFolder + "/Hud_Menu.png");
            ArtSprite("closeGlyph", UiTextureImport.GlyphsFolder + "/Hud_Cancel.png");
            ArtSprite("portrait", UiTextureImport.IconsFolder + "/PortraitArdan.png");
            ArtSprite("playGlyph", UiTextureImport.GlyphsFolder + "/Hud_Play.png");
            ArtSprite("restartGlyph", UiTextureImport.GlyphsFolder + "/Hud_Rotate.png");
            ArtSprite("homeGlyph", UiTextureImport.GlyphsFolder + "/Hud_Home.png");
            mso.FindProperty("guardModel").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Soldiers/SoldierSpearman-lvl3.fbx");
            mso.FindProperty("fx").objectReferenceValue = fx;
            mso.ApplyModifiedPropertiesWithoutUndo();

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

        private static Material GroundMaterial(string name, float mode)
        {
            var m = ShaderMaterial(name, "CityBuilder/CineGround");
            m.SetFloat("_Mode", mode);
            EditorUtility.SetDirty(m);
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
