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
    /// The big windows: building card, build menu selector, research, workforce, army panel and
    /// army orders.
    /// </summary>
    public static partial class SetupProject
    {
        /// <summary>
        /// The army panel, directly under the Happiness panel on the right (same x 760 and width,
        /// see BuildHappinessPanel for why it isn't 810). Hidden by ArmyPanelController until the
        /// player actually has soldiers, so a settlement with no army isn't staring at an empty
        /// box -- which is also why the rows inside are built at runtime rather than here: their
        /// number changes with recruitment.
        /// </summary>
        private static void BuildArmyPanel(Transform canvasParent, Sprite panelSprite)
        {
            var frame = CreateImage(canvasParent, "Army", new Color(0.16f, 0.18f, 0.15f, 0.85f));
            frame.sprite = panelSprite;
            frame.type = Image.Type.Sliced;
            var frameRect = frame.GetComponent<RectTransform>();
            // Down the right edge under the migration line.
            frameRect.anchorMin = frameRect.anchorMax = frameRect.pivot = new Vector2(1f, 1f);
            frameRect.anchoredPosition = new Vector2(-HudMargin, -HudMargin * 0.7f - RoundButton - 90f);
            frameRect.sizeDelta = new Vector2(400f, 220f);

            var summaryLabel = CreateText(frame.transform, "ArmySummary", string.Empty, 19, new Vector2(0f, 88f), new Vector2(370f, 36f), new Color(1f, 1f, 1f, 0.75f));

            // Rows are added top-down from here by the controller; the container leaves room for
            // the summary line above it.
            var rowContainer = new GameObject("ArmyGroupRows", typeof(RectTransform));
            rowContainer.transform.SetParent(frame.transform, false);
            var rowRect = rowContainer.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 0f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.offsetMin = new Vector2(15f, 15f);
            rowRect.offsetMax = new Vector2(-15f, -50f);

            // On its own always-active object, NOT on the panel it shows: a component on a
            // deactivated GameObject never runs Start() and would never subscribe (same trap
            // documented on GameOverController).
            var go = new GameObject("ArmyPanelController");
            go.transform.SetParent(canvasParent, false);
            var controller = go.AddComponent<ArmyPanelController>();
            var so = new SerializedObject(controller);
            so.FindProperty("panelRoot").objectReferenceValue = frame.gameObject;
            so.FindProperty("rowContainer").objectReferenceValue = rowContainer.GetComponent<RectTransform>();
            so.FindProperty("summaryLabel").objectReferenceValue = summaryLabel;
            so.FindProperty("buttonSprite").objectReferenceValue = SkinSprite("pill_parchment") ?? panelSprite;
            so.ApplyModifiedPropertiesWithoutUndo();

            frame.gameObject.SetActive(false);
        }

        /// <summary>Turns world taps into orders for the selected group (see ArmyOrderInput) -- inert while no group is selected, which is how it coexists with the citizen/building selectors.</summary>
        private static ArmyOrderInput BuildArmyOrderInput(Transform canvasParent, Camera targetCamera)
        {
            var go = new GameObject("ArmyOrderInput");
            go.transform.SetParent(canvasParent, false);
            var input = go.AddComponent<ArmyOrderInput>();
            var so = new SerializedObject(input);
            so.FindProperty("targetCamera").objectReferenceValue = targetCamera;
            // the same flag a citizen's move order plants
            so.FindProperty("orderFlagPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsBuildingsFolder}/OrderFlag.fbx");
            so.ApplyModifiedPropertiesWithoutUndo();
            return input;
        }

        /// <summary>
        /// Every workplace in the settlement in one scrollable card, with the same +/- controls the
        /// building's own info panel carries -- see WorkforcePanelController for why it lists
        /// buildings rather than people.
        ///
        /// A modal card like BuildingInfoPanel rather than a standing panel: at a settlement's
        /// worth of workshops this list is far too tall to leave on screen, and it is something the
        /// player consults when rearranging rather than watches.
        /// </summary>
        private static void BuildWorkforcePanel(Transform canvasParent, Sprite panelSprite, HudHider hud)
        {
            var panelRoot = new GameObject("WorkforcePanel", typeof(RectTransform));
            panelRoot.AddComponent<HudOverlay>();
            panelRoot.transform.SetParent(canvasParent, false);
            StretchFull(panelRoot.GetComponent<RectTransform>());

            var backdrop = CreateImage(panelRoot.transform, "Backdrop", new Color(0f, 0f, 0f, 0.7f));
            StretchFull(backdrop.GetComponent<RectTransform>());

            var card = CreateImage(panelRoot.transform, "Card", new Color(0.16f, 0.18f, 0.15f, 0.98f));
            card.sprite = panelSprite;
            card.type = Image.Type.Sliced;
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(960f, 780f);
            cardRect.anchoredPosition = Vector2.zero;

            Localize(CreateText(card.transform, "Title", string.Empty, 34, new Vector2(0f, 336f), new Vector2(880f, 56f)), "#workforce_title");

            // Two tabs: the workplaces (move hands between jobs) and the citizens (what each one is
            // doing -- the user, 2026-10-20).
            var workTab = CreateButton(card.transform, panelSprite, "WorkplacesTab", "#citizens_tab_work", new Vector2(-150f, 274f), new Vector2(280f, 62f));
            var peopleTab = CreateButton(card.transform, panelSprite, "CitizensTab", "#citizens_tab_people", new Vector2(150f, 274f), new Vector2(280f, 62f));
            var summaryLabel = CreateText(card.transform, "Summary", string.Empty, 22, new Vector2(0f, 220f), new Vector2(880f, 36f), new Color(1f, 1f, 1f, 0.8f));

            var workView = new GameObject("WorkplacesView", typeof(RectTransform));
            workView.transform.SetParent(card.transform, false);
            StretchFull((RectTransform)workView.transform);
            BuildScrollList(workView.transform, new Vector2(0f, -45f), new Vector2(900f, 470f),
                "#workforce_empty", out var content, out var emptyLabelGO);

            var peopleView = new GameObject("CitizensView", typeof(RectTransform));
            peopleView.transform.SetParent(card.transform, false);
            StretchFull((RectTransform)peopleView.transform);
            BuildScrollList(peopleView.transform, new Vector2(0f, -45f), new Vector2(900f, 470f),
                "#citizens_empty", out var peopleContent, out var peopleEmpty);
            peopleView.SetActive(false);

            var closeButton = CreateButton(card.transform, panelSprite, "CloseButton", "#ui_close", new Vector2(0f, -330f), new Vector2(300f, 64f));

            var controller = panelRoot.AddComponent<WorkforcePanelController>();
            var controllerSO = new SerializedObject(controller);
            controllerSO.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            controllerSO.FindProperty("listContent").objectReferenceValue = content;
            controllerSO.FindProperty("emptyLabel").objectReferenceValue = emptyLabelGO;
            controllerSO.FindProperty("summaryLabel").objectReferenceValue = summaryLabel;
            controllerSO.FindProperty("rowSprite").objectReferenceValue = SkinSprite("card") ?? panelSprite;
            controllerSO.FindProperty("hud").objectReferenceValue = hud;
            controllerSO.FindProperty("workplacesView").objectReferenceValue = workView;
            controllerSO.FindProperty("citizensView").objectReferenceValue = peopleView;
            controllerSO.FindProperty("citizenContent").objectReferenceValue = peopleContent;
            controllerSO.FindProperty("citizensEmptyLabel").objectReferenceValue = peopleEmpty;
            controllerSO.FindProperty("workplacesTab").objectReferenceValue = workTab.GetComponent<Image>();
            controllerSO.FindProperty("citizensTab").objectReferenceValue = peopleTab.GetComponent<Image>();
            controllerSO.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(closeButton.onClick, controller.ClosePanel);
            UnityEventTools.AddPersistentListener(workTab.onClick, controller.ShowWorkplaces);
            UnityEventTools.AddPersistentListener(peopleTab.onClick, controller.ShowCitizens);

            // On the left edge under the menu button (the user moved it there, 2026-10-14): two figures, no word.
            var openButton = CreateGlyphButton(canvasParent, "WorkforceButton", "round", HudInk.OnRound, "People", CreatePopulationIcon(), WorkforceButtonSize, 0.62f);
            Anchor(openButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(HudMargin + (RoundButton - WorkforceButtonSize) * 0.5f, WorkforceButtonTop), new Vector2(WorkforceButtonSize, WorkforceButtonSize));
            UnityEventTools.AddPersistentListener(openButton.onClick, controller.OpenPanel);

            panelRoot.SetActive(false);
        }

        private static BuildingInfoPanelController BuildBuildingInfoPanel(Transform canvasParent, Sprite panelSprite, ResourceIconLibrary iconLibrary)
        {
            var panelRoot = new GameObject("BuildingInfoPanel", typeof(RectTransform));
            panelRoot.transform.SetParent(canvasParent, false);
            StretchFull(panelRoot.GetComponent<RectTransform>());

            var backdrop = CreateImage(panelRoot.transform, "Backdrop", new Color(0f, 0f, 0f, 0.7f));
            StretchFull(backdrop.GetComponent<RectTransform>());

            var card = CreateImage(panelRoot.transform, "Card", new Color(0.16f, 0.18f, 0.15f, 0.98f));
            card.sprite = panelSprite;
            card.type = Image.Type.Sliced;
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(700f, 780f);
            cardRect.anchoredPosition = Vector2.zero;

            // The header stack was pushed up to 330 to open the band at y 160 for the recipe
            // buttons; the blocks below the worker controls are untouched.
            var title = CreateText(card.transform, "Title", string.Empty, 34, new Vector2(0f, 330f), new Vector2(640f, 60f));
            var level = CreateText(card.transform, "Level", string.Empty, 24, new Vector2(0f, 280f), new Vector2(640f, 40f), new Color(1f, 1f, 1f, 0.85f));
            var condition = CreateText(card.transform, "Condition", string.Empty, 20, new Vector2(0f, 240f), new Vector2(640f, 40f), new Color(1f, 1f, 1f, 0.6f));

            // What the building makes, and out of what. Above the worker block because it is the
            // answer to "why would I staff this" -- and for a converter it is the only place the
            // conversion is written down at all.
            var production = CreateText(card.transform, "Production", string.Empty, 20, new Vector2(0f, 205f), new Vector2(640f, 30f), new Color(0.82f, 0.88f, 0.7f));

            // Why this gatherer is earning nothing, in the band the recipe buttons use -- a
            // Sawmill or a Quarry has exactly one recipe, so that band is empty on every building
            // this line can appear on. Amber rather than red: the building is not broken, it is
            // standing in the wrong place.
            var warning = CreateText(card.transform, "Warning", string.Empty, 22, new Vector2(0f, 160f), new Vector2(600f, 56f), new Color(1f, 0.72f, 0.35f));
            warning.horizontalOverflow = HorizontalWrapMode.Wrap;
            warning.gameObject.SetActive(false);

            // The metal buttons on a Плавильня, hidden for every building that has only one recipe.
            // The buttons themselves are built at play time by the controller: how many there are
            // is a property of the sheet, not of the scene.
            var recipeControls = new GameObject("RecipeControls", typeof(RectTransform));
            recipeControls.transform.SetParent(card.transform, false);
            StretchFull(recipeControls.GetComponent<RectTransform>());

            var recipeRow = new GameObject("RecipeRow", typeof(RectTransform));
            recipeRow.transform.SetParent(recipeControls.transform, false);
            var recipeRowRect = recipeRow.GetComponent<RectTransform>();
            recipeRowRect.anchorMin = recipeRowRect.anchorMax = new Vector2(0.5f, 0.5f);
            recipeRowRect.anchoredPosition = new Vector2(0f, 160f);
            recipeRowRect.sizeDelta = new Vector2(660f, 58f);

            // Passthrough containers (StretchFull, zero offset) purely to group-toggle visibility --
            // children keep the same card-relative positions they'd have without the wrapper.
            var workerControls = new GameObject("WorkerControls", typeof(RectTransform));
            workerControls.transform.SetParent(card.transform, false);
            StretchFull(workerControls.GetComponent<RectTransform>());

            var workers = CreateText(workerControls.transform, "Workers", string.Empty, 26, new Vector2(0f, 105f), new Vector2(640f, 50f), new Color(1f, 1f, 1f, 0.85f));
            var idle = CreateText(workerControls.transform, "Idle", string.Empty, 22, new Vector2(0f, 65f), new Vector2(640f, 40f), new Color(1f, 1f, 1f, 0.6f));
            var assignButton = CreateButton(workerControls.transform, panelSprite, "AssignButton", "#building_assign", new Vector2(-170f, 10f), new Vector2(300f, 80f));
            var unassignButton = CreateButton(workerControls.transform, panelSprite, "UnassignButton", "#building_unassign", new Vector2(170f, 10f), new Vector2(300f, 80f));

            var upgradeControls = new GameObject("UpgradeControls", typeof(RectTransform));
            upgradeControls.transform.SetParent(card.transform, false);
            StretchFull(upgradeControls.GetComponent<RectTransform>());

            var upgradeCostRow = CreateCostRow(upgradeControls.transform, "UpgradeCostRow", new Vector2(0f, -55f));
            var upgradeButton = CreateButton(upgradeControls.transform, panelSprite, "UpgradeButton", "#building_upgrade", new Vector2(0f, -125f), new Vector2(360f, 70f));

            // Stands in for the price and the button when the next level has not been researched --
            // in the same band, so the card does not change height depending on the answer.
            var upgradeLock = CreateText(upgradeControls.transform, "UpgradeLock", string.Empty, 22, new Vector2(0f, -90f), new Vector2(640f, 60f), new Color(1f, 0.78f, 0.5f));

            var repairControls = new GameObject("RepairControls", typeof(RectTransform));
            repairControls.transform.SetParent(card.transform, false);
            StretchFull(repairControls.GetComponent<RectTransform>());

            var repairCostRow = CreateCostRow(repairControls.transform, "RepairCostRow", new Vector2(0f, -195f));
            var repairButton = CreateButton(repairControls.transform, panelSprite, "RepairButton", "#building_repair", new Vector2(0f, -265f), new Vector2(360f, 70f));

            // Recruitment (Barracks only) reuses the worker block's slot on the card: the two are
            // mutually exclusive in practice -- the Barracks has no maxWorkers, so its worker
            // block never shows -- and this keeps the card from growing a fourth stacked section.
            var recruitControls = new GameObject("RecruitControls", typeof(RectTransform));
            recruitControls.transform.SetParent(card.transform, false);
            StretchFull(recruitControls.GetComponent<RectTransform>());

            // Which tier to raise. In the same band as the Плавильня's metal buttons (y 160) --
            // the two are the same question asked of two buildings, and the recipe block is hidden
            // for a Barracks anyway, so nothing overlaps. The buttons are built at play time by the
            // controller: how many tiers there are is a property of the sheet, not of the scene.
            var recruitTypeRow = new GameObject("RecruitTypeRow", typeof(RectTransform));
            recruitTypeRow.transform.SetParent(recruitControls.transform, false);
            var recruitTypeRowRect = recruitTypeRow.GetComponent<RectTransform>();
            recruitTypeRowRect.anchorMin = recruitTypeRowRect.anchorMax = new Vector2(0.5f, 0.5f);
            recruitTypeRowRect.anchoredPosition = new Vector2(0f, 160f);
            recruitTypeRowRect.sizeDelta = new Vector2(660f, 58f);

            var recruitLabel = CreateText(recruitControls.transform, "RecruitLabel", string.Empty, 22, new Vector2(0f, 105f), new Vector2(640f, 44f), new Color(1f, 1f, 1f, 0.8f));
            var recruitCostRow = CreateCostRow(recruitControls.transform, "RecruitCostRow", new Vector2(0f, 62f));
            var recruitButton = CreateButton(recruitControls.transform, panelSprite, "RecruitButton", "#building_recruit", new Vector2(0f, 10f), new Vector2(360f, 70f));

            // Demolition shares the bottom row with Close, and is deliberately the one button on
            // this card wearing a warning colour: it is the only irreversible thing here, and it
            // sits a finger's width from Upgrade.
            var demolishControls = new GameObject("DemolishControls", typeof(RectTransform));
            demolishControls.transform.SetParent(card.transform, false);
            StretchFull(demolishControls.GetComponent<RectTransform>());

            var demolishButton = CreateButton(demolishControls.transform, panelSprite, "DemolishButton", "#building_demolish", new Vector2(-170f, -345f), new Vector2(300f, 70f));
            demolishButton.GetComponent<Image>().color = new Color(0.45f, 0.2f, 0.18f, 0.95f);

            var closeButton = CreateButton(card.transform, panelSprite, "CloseButton", "#ui_close", new Vector2(170f, -345f), new Vector2(300f, 70f));

            // The confirmation, over the card rather than over the world -- the card is already
            // modal (ModalGate), so this only has to beat it in draw order.
            var demolishConfirm = new GameObject("DemolishConfirm", typeof(RectTransform));
            demolishConfirm.transform.SetParent(panelRoot.transform, false);
            StretchFull(demolishConfirm.GetComponent<RectTransform>());

            var demolishBackdrop = CreateImage(demolishConfirm.transform, "Backdrop", new Color(0f, 0f, 0f, 0.75f));
            StretchFull(demolishBackdrop.GetComponent<RectTransform>());

            var demolishCard = CreateImage(demolishConfirm.transform, "Card", new Color(0.18f, 0.14f, 0.13f, 0.98f));
            demolishCard.sprite = panelSprite;
            demolishCard.type = Image.Type.Sliced;
            var demolishCardRect = demolishCard.GetComponent<RectTransform>();
            demolishCardRect.anchorMin = demolishCardRect.anchorMax = new Vector2(0.5f, 0.5f);
            demolishCardRect.sizeDelta = new Vector2(760f, 320f);
            demolishCardRect.anchoredPosition = Vector2.zero;

            // Filled in at run time: it names the building and what the refund actually is.
            var demolishConfirmLabel = CreateText(demolishCard.transform, "Question", string.Empty, 28, new Vector2(0f, 60f), new Vector2(680f, 120f));

            var demolishConfirmButton = CreateButton(demolishCard.transform, panelSprite, "ConfirmDemolishButton", "#building_demolish", new Vector2(-160f, -80f), new Vector2(300f, 84f));
            demolishConfirmButton.GetComponent<Image>().color = new Color(0.45f, 0.2f, 0.18f, 0.95f);
            var demolishCancelButton = CreateButton(demolishCard.transform, panelSprite, "CancelDemolishButton", "#ui_cancel", new Vector2(160f, -80f), new Vector2(300f, 84f));

            demolishConfirm.SetActive(false);

            var controller = panelRoot.AddComponent<BuildingInfoPanelController>();
            var controllerSO = new SerializedObject(controller);
            controllerSO.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            controllerSO.FindProperty("titleLabel").objectReferenceValue = title;
            controllerSO.FindProperty("levelLabel").objectReferenceValue = level;
            controllerSO.FindProperty("conditionLabel").objectReferenceValue = condition;
            controllerSO.FindProperty("productionLabel").objectReferenceValue = production;
            controllerSO.FindProperty("warningLabel").objectReferenceValue = warning;
            controllerSO.FindProperty("demolishControls").objectReferenceValue = demolishControls;
            controllerSO.FindProperty("demolishConfirmRoot").objectReferenceValue = demolishConfirm;
            controllerSO.FindProperty("demolishConfirmLabel").objectReferenceValue = demolishConfirmLabel;
            controllerSO.FindProperty("recipeControls").objectReferenceValue = recipeControls;
            controllerSO.FindProperty("recipeRow").objectReferenceValue = recipeRowRect;
            controllerSO.FindProperty("recipeButtonSprite").objectReferenceValue = SkinSprite("card") ?? panelSprite;
            controllerSO.FindProperty("workerControls").objectReferenceValue = workerControls;
            controllerSO.FindProperty("workersLabel").objectReferenceValue = workers;
            controllerSO.FindProperty("idleLabel").objectReferenceValue = idle;
            controllerSO.FindProperty("upgradeControls").objectReferenceValue = upgradeControls;
            controllerSO.FindProperty("upgradeCostRow").objectReferenceValue = upgradeCostRow;
            controllerSO.FindProperty("upgradeButton").objectReferenceValue = upgradeButton;
            controllerSO.FindProperty("upgradeLockLabel").objectReferenceValue = upgradeLock;
            controllerSO.FindProperty("repairControls").objectReferenceValue = repairControls;
            controllerSO.FindProperty("repairCostRow").objectReferenceValue = repairCostRow;
            controllerSO.FindProperty("recruitControls").objectReferenceValue = recruitControls;
            controllerSO.FindProperty("recruitLabel").objectReferenceValue = recruitLabel;
            controllerSO.FindProperty("recruitCostRow").objectReferenceValue = recruitCostRow;
            controllerSO.FindProperty("recruitTypeRow").objectReferenceValue = recruitTypeRowRect;
            controllerSO.FindProperty("iconLibrary").objectReferenceValue = iconLibrary;
            controllerSO.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(assignButton.onClick, controller.AssignWorker);
            UnityEventTools.AddPersistentListener(unassignButton.onClick, controller.UnassignWorker);
            UnityEventTools.AddPersistentListener(upgradeButton.onClick, controller.Upgrade);
            UnityEventTools.AddPersistentListener(repairButton.onClick, controller.Repair);
            UnityEventTools.AddPersistentListener(demolishButton.onClick, controller.OpenDemolishConfirm);
            UnityEventTools.AddPersistentListener(demolishConfirmButton.onClick, controller.ConfirmDemolish);
            UnityEventTools.AddPersistentListener(demolishCancelButton.onClick, controller.CancelDemolish);
            UnityEventTools.AddPersistentListener(recruitButton.onClick, controller.Recruit);
            UnityEventTools.AddPersistentListener(closeButton.onClick, controller.Close);

            panelRoot.SetActive(false);
            return controller;
        }

        private static BuildingSelector BuildBuildingSelector(Transform canvasParent, Camera targetCamera,
            BuildingInfoPanelController infoPanel, ResearchPanelController researchPanel)
        {
            var go = new GameObject("BuildingSelector");
            go.transform.SetParent(canvasParent, false);
            var selector = go.AddComponent<BuildingSelector>();
            var so = new SerializedObject(selector);
            so.FindProperty("targetCamera").objectReferenceValue = targetCamera;
            so.FindProperty("infoPanel").objectReferenceValue = infoPanel;
            so.FindProperty("researchPanel").objectReferenceValue = researchPanel;
            so.ApplyModifiedPropertiesWithoutUndo();
            return selector;
        }

        /// <summary>
        /// The Laboratory's own window, opened by tapping the Laboratory INSTEAD of the ordinary
        /// building card -- which is why its header carries the scientists, the upgrade and the
        /// repair: nothing else would reach them (see ResearchPanelController).
        ///
        /// Wider and taller than the other cards (1240x980 against the workforce panel's 960x780)
        /// because a row here holds a title, a price and a button, and there are two tabs of them.
        /// 980 tall spans y -490..490 on a 1080-high canvas, which clears the edge.
        /// </summary>
        private static ResearchPanelController BuildResearchPanel(Transform canvasParent, Sprite panelSprite, ResourceIconLibrary iconLibrary)
        {
            var panelRoot = new GameObject("ResearchPanel", typeof(RectTransform));
            panelRoot.transform.SetParent(canvasParent, false);
            StretchFull(panelRoot.GetComponent<RectTransform>());

            var backdrop = CreateImage(panelRoot.transform, "Backdrop", new Color(0f, 0f, 0f, 0.7f));
            StretchFull(backdrop.GetComponent<RectTransform>());

            var card = CreateImage(panelRoot.transform, "Card", new Color(0.16f, 0.18f, 0.15f, 0.98f));
            card.sprite = panelSprite;
            card.type = Image.Type.Sliced;
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(1240f, 980f);
            cardRect.anchoredPosition = Vector2.zero;

            var title = CreateText(card.transform, "Title", string.Empty, 34, new Vector2(0f, 430f), new Vector2(1160f, 60f));
            var condition = CreateText(card.transform, "Condition", string.Empty, 20, new Vector2(0f, 390f), new Vector2(1160f, 36f), new Color(1f, 1f, 1f, 0.6f));

            // Header band: the scientists on the left, the building's own two actions on the right.
            var scientists = CreateText(card.transform, "Scientists", string.Empty, 24, new Vector2(-330f, 335f), new Vector2(500f, 44f), new Color(1f, 1f, 1f, 0.9f), addShadow: false);
            scientists.alignment = TextAnchor.MiddleLeft;
            var removeScientist = CreateButton(card.transform, panelSprite, "RemoveScientistButton", "-", new Vector2(-120f, 335f), new Vector2(64f, 64f));
            var addScientist = CreateButton(card.transform, panelSprite, "AddScientistButton", "+", new Vector2(-40f, 335f), new Vector2(64f, 64f));

            var upgradeControls = new GameObject("UpgradeControls", typeof(RectTransform));
            upgradeControls.transform.SetParent(card.transform, false);
            StretchFull(upgradeControls.GetComponent<RectTransform>());
            var upgradeButton = CreateButton(upgradeControls.transform, panelSprite, "UpgradeButton", "#building_upgrade", new Vector2(200f, 345f), new Vector2(260f, 64f));
            var upgradeCostRow = CreateCostRow(upgradeControls.transform, "UpgradeCostRow", new Vector2(200f, 297f));

            var repairControls = new GameObject("RepairControls", typeof(RectTransform));
            repairControls.transform.SetParent(card.transform, false);
            StretchFull(repairControls.GetComponent<RectTransform>());
            var repairButton = CreateButton(repairControls.transform, panelSprite, "RepairButton", "#building_repair", new Vector2(490f, 345f), new Vector2(260f, 64f));
            var repairCostRow = CreateCostRow(repairControls.transform, "RepairCostRow", new Vector2(490f, 297f));

            // What is being researched right now, and the way out of it.
            var progress = CreateText(card.transform, "Progress", string.Empty, 22, new Vector2(-160f, 240f), new Vector2(840f, 40f), new Color(0.82f, 0.88f, 0.7f));
            progress.alignment = TextAnchor.MiddleLeft;

            var cancelControls = new GameObject("CancelControls", typeof(RectTransform));
            cancelControls.transform.SetParent(card.transform, false);
            StretchFull(cancelControls.GetComponent<RectTransform>());
            var cancelButton = CreateDynamicButton(cancelControls.transform, panelSprite, "CancelResearchButton", new Vector2(400f, 240f), new Vector2(420f, 60f), out var cancelLabel);

            var buildingsTab = CreateButton(card.transform, panelSprite, "BuildingsTab", "#research_tab_buildings", new Vector2(-200f, 178f), new Vector2(380f, 60f));
            var unitsTab = CreateButton(card.transform, panelSprite, "UnitsTab", "#research_tab_units", new Vector2(200f, 178f), new Vector2(380f, 60f));

            BuildScrollList(card.transform, new Vector2(0f, -130f), new Vector2(1180f, 560f),
                "#research_empty", out var content, out var emptyLabelGO);

            var closeButton = CreateButton(card.transform, panelSprite, "CloseButton", "#ui_close", new Vector2(0f, -450f), new Vector2(300f, 70f));

            var controller = panelRoot.AddComponent<ResearchPanelController>();
            var so = new SerializedObject(controller);
            so.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            so.FindProperty("titleLabel").objectReferenceValue = title;
            so.FindProperty("conditionLabel").objectReferenceValue = condition;
            so.FindProperty("scientistsLabel").objectReferenceValue = scientists;
            so.FindProperty("addScientistButton").objectReferenceValue = addScientist;
            so.FindProperty("removeScientistButton").objectReferenceValue = removeScientist;
            so.FindProperty("upgradeControls").objectReferenceValue = upgradeControls;
            so.FindProperty("upgradeCostRow").objectReferenceValue = upgradeCostRow;
            so.FindProperty("repairControls").objectReferenceValue = repairControls;
            so.FindProperty("repairCostRow").objectReferenceValue = repairCostRow;
            so.FindProperty("progressLabel").objectReferenceValue = progress;
            so.FindProperty("cancelControls").objectReferenceValue = cancelControls;
            so.FindProperty("cancelLabel").objectReferenceValue = cancelLabel;
            so.FindProperty("buildingsTabBackground").objectReferenceValue = buildingsTab.GetComponent<Image>();
            so.FindProperty("unitsTabBackground").objectReferenceValue = unitsTab.GetComponent<Image>();
            so.FindProperty("listContent").objectReferenceValue = content;
            so.FindProperty("emptyLabel").objectReferenceValue = emptyLabelGO;
            so.FindProperty("rowSprite").objectReferenceValue = SkinSprite("card") ?? panelSprite;
            so.FindProperty("completedIcon").objectReferenceValue = CreateCheckIcon();
            so.FindProperty("iconLibrary").objectReferenceValue = iconLibrary;
            so.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(addScientist.onClick, controller.AddScientist);
            UnityEventTools.AddPersistentListener(removeScientist.onClick, controller.RemoveScientist);
            UnityEventTools.AddPersistentListener(upgradeButton.onClick, controller.UpgradeLab);
            UnityEventTools.AddPersistentListener(repairButton.onClick, controller.RepairLab);
            UnityEventTools.AddPersistentListener(cancelButton.onClick, controller.CancelResearch);
            UnityEventTools.AddPersistentListener(buildingsTab.onClick, controller.SelectBuildingsTab);
            UnityEventTools.AddPersistentListener(unitsTab.onClick, controller.SelectUnitsTab);
            UnityEventTools.AddPersistentListener(closeButton.onClick, controller.Close);

            panelRoot.SetActive(false);
            return controller;
        }
    }
}
