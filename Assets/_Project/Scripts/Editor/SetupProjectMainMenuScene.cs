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
            StretchFull(backdrop.GetComponent<RectTransform>());

            var card = CreateImage(panelRoot.transform, "Card", new Color(0.16f, 0.18f, 0.15f, 0.98f));
            card.sprite = panelSprite;
            card.type = Image.Type.Sliced;
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            // The language, the fog's look, the HUD's look, how construction ends (the season and the Invasion's map left for
            // the menu's theme buttons, the user 2026-10-25).
            cardRect.sizeDelta = new Vector2(800f, 890f);
            cardRect.anchoredPosition = Vector2.zero;

            Localize(CreateText(card.transform, "Title", string.Empty, 40, new Vector2(0f, 385f), new Vector2(680f, 70f)), "#settings_title");
            Localize(CreateText(card.transform, "LanguageLabel", string.Empty, 26, new Vector2(0f, 315f), new Vector2(680f, 50f), new Color(1f, 1f, 1f, 0.75f)), "#settings_language");
            var languageRowRect = CreateSettingsRow(card.transform, "LanguageRow", 251f);

            Localize(CreateText(card.transform, "FogLabel", string.Empty, 26, new Vector2(0f, 185f), new Vector2(680f, 50f), new Color(1f, 1f, 1f, 0.75f)), "#settings_fog");
            var fogRowRect = CreateSettingsRow(card.transform, "FogRow", 121f);

            Localize(CreateText(card.transform, "HudLabel", string.Empty, 26, new Vector2(0f, 55f), new Vector2(680f, 50f), new Color(1f, 1f, 1f, 0.75f)), "#settings_hud");
            var hudRowRect = CreateSettingsRow(card.transform, "HudRow", -9f);

            Localize(CreateText(card.transform, "FinishLabel", string.Empty, 26, new Vector2(0f, -75f), new Vector2(680f, 50f), new Color(1f, 1f, 1f, 0.75f)), "#settings_build_finish");
            var finishRowRect = CreateSettingsRow(card.transform, "FinishRow", -139f);

            var backButton = CreateButton(card.transform, panelSprite, "SettingsBackButton", "#settings_back", new Vector2(0f, -355f), new Vector2(300f, 80f));

            var controller = panelRoot.AddComponent<SettingsPanelController>();
            var controllerSO = new SerializedObject(controller);
            controllerSO.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            controllerSO.FindProperty("languageRow").objectReferenceValue = languageRowRect;
            controllerSO.FindProperty("fogRow").objectReferenceValue = fogRowRect;
            controllerSO.FindProperty("hudRow").objectReferenceValue = hudRowRect;
            controllerSO.FindProperty("finishRow").objectReferenceValue = finishRowRect;
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
