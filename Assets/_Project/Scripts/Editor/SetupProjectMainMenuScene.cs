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
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// The main-menu scene: its root, the settings card and its rows. The menu screens themselves
    /// (mode cards, panels) are in SetupProjectMainMenu.
    /// </summary>
    public static partial class SetupProject
    {
        private static void BuildMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Created after the scene switch above (see the NewScene() comment in BuildCityScene
            // for why) and used only within this method, before this scene is saved.
            var panelSprite = CreatePanelSprite();

            var cameraGO = new GameObject("Menu Camera", typeof(Camera));
            var menuCamera = cameraGO.GetComponent<Camera>();
            menuCamera.clearFlags = CameraClearFlags.Skybox;
            menuCamera.cullingMask = ~0;
            menuCamera.orthographic = false;
            menuCamera.nearClipPlane = 0.25f;               // the village's flat ground layers far off
            menuCamera.farClipPlane = 400f;
            menuCamera.fieldOfView = 40f;
            cameraGO.transform.SetPositionAndRotation(new Vector3(2.8f, 2.3f, -9.5f), Quaternion.LookRotation(new Vector3(-6f, -0.9f, 11f)));
            BuildMenuVillage(menuCamera);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Expand, not match-height: a tablet or a 4:3 window is narrower than 16:9 and would
            // push the menu's sides off the screen (see SetupProjectLayout).
            ConfigureScaler(canvasGO.GetComponent<CanvasScaler>());

            // The menu after the user's sketch (2026-10-20), in the look picked in the settings --
            // see SetupProjectMainMenu.
            BuildMainMenu(canvasGO.transform, panelSprite);
            ApplyTheme(canvasGO.transform);

            Directory.CreateDirectory(ScenesFolder);
            DeleteIfExists($"{ScenesFolder}/MainMenu.unity");
            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/MainMenu.unity");
        }

        /// <summary>
        /// Main menu concept 1 «День в деревне Ардана» (the user, 2026-11-17: the menu «красивее и сочнее»,
        /// the empty scene was its fault): the campaign's opening village behind the menu, by day, alive --
        /// the folk strolling and chatting, the market's trade, the kids on the stones, chimney smoke,
        /// Ардан walking through once a loop. The film's own village, folk, effects and director play
        /// Campaign/MenuVillage.json, a script that loops (OpeningDirector.loopFrom). Replaces the
        /// four random work scenes (MainMenuBackground, 2026-10-20).
        /// </summary>
        private static void BuildMenuVillage(Camera cam)
        {
            // a day sun, higher and whiter than the film's morning
            var sunGO = new GameObject("Sun", typeof(Light));
            var sun = sunGO.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.86f);
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            sunGO.transform.rotation = Quaternion.Euler(48f, 35f, 0f);
            RenderSettings.sun = sun;
            RenderSettings.skybox = CampaignSky();
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.64f, 0.74f, 0.88f);
            RenderSettings.ambientEquatorColor = new Color(0.6f, 0.64f, 0.6f);
            RenderSettings.ambientGroundColor = new Color(0.38f, 0.42f, 0.3f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.76f, 0.84f, 0.92f);
            RenderSettings.fogStartDistance = 45f;
            RenderSettings.fogEndDistance = 140f;

            var clips = BakeCastClips();
            var (village, life) = CreateOpeningVillage(clips);
            // the house going up (stage 2, approved 2026-11-17): the site, its two builders on their loops
            var site = village.gameObject.AddComponent<CityBuilder.Campaign.MenuBuildSite>();
            var sso = new SerializedObject(site);
            sso.FindProperty("village").objectReferenceValue = village;
            sso.FindProperty("siteFile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(MenuVillagePath);
            sso.FindProperty("layoutFile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Campaign/MenuBuilders.json");
            var workers = new[] { "CineCarpenter", "CineLabourer" };
            var castProp = sso.FindProperty("cast");
            castProp.arraySize = workers.Length;
            for (var i = 0; i < workers.Length; i++)
            {
                var e = castProp.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("id").stringValue = workers[i];
                e.FindPropertyRelative("model").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelImportDefaults.CinematicCastFolder}{workers[i]}.fbx");
                e.FindPropertyRelative("clips").objectReferenceValue = clips.TryGetValue(workers[i], out var c) ? c : null;
                if (!clips.ContainsKey(workers[i])) Debug.LogError($"[MainMenu] builder {workers[i]} has no clips");
            }
            sso.ApplyModifiedPropertiesWithoutUndo();
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var volume = new GameObject("MenuVolume", typeof(UnityEngine.Rendering.Volume)).GetComponent<UnityEngine.Rendering.Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = OpeningVolume();
            var fx = CreateOpeningFx(village, MenuVillagePath, volume, null);
            // no storm over the menu's day, ever
            var fso = new SerializedObject(fx);
            fso.FindProperty("stormMaterial").objectReferenceValue = null;
            fso.ApplyModifiedPropertiesWithoutUndo();
            CreateOpeningDirector(village, life, MenuVillagePath, cam, null, fx, null);
        }

        private const string MenuVillagePath = "Assets/_Project/Campaign/MenuVillage.json";

        /// <summary>
        /// The settings card, on the main menu. The language buttons inside are built at play time
        /// from whatever columns the localization sheet has -- see SettingsPanelController -- so a
        /// third language never needs this method touched.
        /// </summary>
        private static SettingsPanelController BuildSettingsPanel(Transform canvasParent, Sprite panelSprite, Button openButton)
        {
            var panelRoot = new GameObject("SettingsPanel", typeof(RectTransform));
            panelRoot.transform.SetParent(canvasParent, false);
            StretchFull(panelRoot.GetComponent<RectTransform>());

            var backdrop = CreateImage(panelRoot.transform, "Backdrop", new Color(0f, 0f, 0f, 0.7f));
            StretchBleed(backdrop.GetComponent<RectTransform>());

            var card = CreateImage(panelRoot.transform, "Card", new Color(0.16f, 0.18f, 0.15f, 0.98f));
            card.sprite = panelSprite;
            card.type = Image.Type.Sliced;
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            // The language, the fog's look, the HUD's look (the season and the Invasion's map left for
            // the menu's theme buttons, the user 2026-10-25).
            // + the graphics tier (the user, 2026-11-09), Auto/Manual with details (2026-11-10)
            cardRect.sizeDelta = new Vector2(800f, 900f);
            cardRect.anchoredPosition = Vector2.zero;

            Localize(CreateText(card.transform, "Title", string.Empty, 40, new Vector2(0f, 390f), new Vector2(680f, 70f)), "#settings_title");

            // the rows scroll between the title and the Back button (the user, 2026-11-10: the manual
            // graphics settings open under the graphics row and make the card taller than the screen)
            var scroll = new GameObject("Scroll", typeof(RectTransform), typeof(RectMask2D), typeof(ScrollRect));
            scroll.transform.SetParent(card.transform, false);
            var scrollRect = scroll.GetComponent<RectTransform>();
            scrollRect.anchorMin = scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRect.anchoredPosition = new Vector2(0f, 30f);
            scrollRect.sizeDelta = new Vector2(780f, 640f);
            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(scroll.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = Vector2.zero;
            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 6f;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroller = scroll.GetComponent<ScrollRect>();
            scroller.viewport = scrollRect;
            scroller.content = contentRect;
            scroller.horizontal = false;
            scroller.movementType = ScrollRect.MovementType.Clamped;
            scroller.scrollSensitivity = 30f;
            var rows = content.transform;

            Localize(CreateText(rows, "LanguageLabel", string.Empty, 26, Vector2.zero, new Vector2(680f, 50f), new Color(1f, 1f, 1f, 0.75f)), "#settings_language");
            var languageRowRect = CreateSettingsRow(rows, "LanguageRow", 0f);

            Localize(CreateText(rows, "FogLabel", string.Empty, 26, Vector2.zero, new Vector2(680f, 50f), new Color(1f, 1f, 1f, 0.75f)), "#settings_fog");
            var fogRowRect = CreateSettingsRow(rows, "FogRow", 0f);

            Localize(CreateText(rows, "HudLabel", string.Empty, 26, Vector2.zero, new Vector2(680f, 50f), new Color(1f, 1f, 1f, 0.75f)), "#settings_hud");
            var hudRowRect = CreateSettingsRow(rows, "HudRow", 0f);

            var graphicsLabel = CreateText(rows, "GraphicsLabel", string.Empty, 26, Vector2.zero, new Vector2(680f, 50f), new Color(1f, 1f, 1f, 0.75f));
            Localize(graphicsLabel, "#settings_graphics");
            var graphicsRowRect = CreateSettingsRow(rows, "GraphicsRow", 0f);

            // Auto: one line of what the device got; Manual: the level and the details (built at play time)
            var details = new GameObject("GraphicsDetails", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            details.transform.SetParent(rows, false);
            var detailsRect = details.GetComponent<RectTransform>();
            detailsRect.sizeDelta = new Vector2(760f, 0f);
            var detailsLayout = details.GetComponent<VerticalLayoutGroup>();
            detailsLayout.childAlignment = TextAnchor.UpperCenter;
            detailsLayout.spacing = 6f;
            detailsLayout.childControlWidth = detailsLayout.childControlHeight = false;
            detailsLayout.childForceExpandWidth = detailsLayout.childForceExpandHeight = false;
            details.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var backButton = CreateButton(card.transform, panelSprite, "SettingsBackButton", "#settings_back", new Vector2(0f, -340f), new Vector2(300f, 80f));

            var controller = panelRoot.AddComponent<SettingsPanelController>();
            var controllerSO = new SerializedObject(controller);
            controllerSO.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            controllerSO.FindProperty("languageRow").objectReferenceValue = languageRowRect;
            controllerSO.FindProperty("fogRow").objectReferenceValue = fogRowRect;
            controllerSO.FindProperty("hudRow").objectReferenceValue = hudRowRect;
            controllerSO.FindProperty("graphicsRow").objectReferenceValue = graphicsRowRect;
            controllerSO.FindProperty("graphicsDetails").objectReferenceValue = detailsRect;
            controllerSO.FindProperty("labelTemplate").objectReferenceValue = graphicsLabel;
            controllerSO.FindProperty("cardImage").objectReferenceValue = card;
            controllerSO.FindProperty("buttonSprite").objectReferenceValue = SkinSprite("pill_parchment") ?? panelSprite;
            controllerSO.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(backButton.onClick, controller.ClosePanel);
            if (openButton != null) UnityEventTools.AddPersistentListener(openButton.onClick, controller.OpenPanel);

            panelRoot.SetActive(false);
            return controller;
        }

        /// <summary>An empty row the settings panel fills with buttons at play time.</summary>
        private static RectTransform CreateSettingsRow(Transform card, string name, float y)
        {
            var row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(card, false);
            var rect = row.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(700f, 72f);
            return rect;
        }
    }
}
