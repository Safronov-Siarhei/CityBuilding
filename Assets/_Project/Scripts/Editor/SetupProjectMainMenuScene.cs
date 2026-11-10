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
            menuCamera.clearFlags = CameraClearFlags.SolidColor;
            menuCamera.backgroundColor = new Color(0.55f, 0.75f, 0.92f);
            menuCamera.cullingMask = ~0;
            menuCamera.orthographic = false;
            menuCamera.nearClipPlane = 0.3f;
            menuCamera.farClipPlane = 500f;
            menuCamera.fieldOfView = 40f;
            menuCamera.backgroundColor = new Color(0.62f, 0.78f, 0.92f);

            // The game's sun, as the city scene has it (the default scene's directional light), with
            // soft shadows: the menu draws one small scene, so it can afford them.
            var sunGO = new GameObject("Sun", typeof(Light));
            var sun = sunGO.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.957f, 0.839f);
            sun.intensity = 1f;
            sun.shadows = LightShadows.Soft;
            sunGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.sun = sun;

            // A live scene of people at work on one of the maps, a different one each time the menu
            // loads (the user's sketch, 2026-10-20) -- see MainMenuBackground.
            var background = new GameObject("MenuBackground").AddComponent<MainMenuBackground>();
            var backgroundSO = new SerializedObject(background);
            backgroundSO.FindProperty("menuCamera").objectReferenceValue = menuCamera;
            var props = backgroundSO.FindProperty("buildings");
            var ids = new[] { "Sawmill", "Quarry", "Farm", "Windmill", "Hovel", "Well", "Cottage" };
            props.arraySize = ids.Length;
            for (var i = 0; i < ids.Length; i++)
            {
                props.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue = ids[i];
                props.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>($"{BuildingPrefabsFolder}/{ids[i]}.prefab");
            }
            backgroundSO.ApplyModifiedPropertiesWithoutUndo();

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
