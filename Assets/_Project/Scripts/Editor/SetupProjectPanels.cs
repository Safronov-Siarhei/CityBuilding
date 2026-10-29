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

        /// <summary>
        /// The inventory (the user, 2026-10-25): every resource the town can hold in one card, three
        /// to a row -- picture, name, amount (InventoryPanelController fills them) -- opened by a
        /// round sack button under the citizens button. The resource bar along the top keeps only
        /// Food of the food chain; the wheat, flour and pork are here.
        /// </summary>
        private static void BuildInventoryPanel(Transform canvasParent, Sprite panelSprite, HudHider hud, ResourceIconLibrary iconLibrary)
        {
            var panelRoot = new GameObject("InventoryPanel", typeof(RectTransform));
            panelRoot.AddComponent<HudOverlay>();
            panelRoot.transform.SetParent(canvasParent, false);
            StretchFull(panelRoot.GetComponent<RectTransform>());

            var backdrop = CreateImage(panelRoot.transform, "Backdrop", new Color(0f, 0f, 0f, 0.7f));
            StretchFull(backdrop.GetComponent<RectTransform>());

            var types = new List<ResourceType>();
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                // people are counted on their own button; bread is only an old save's
                if (type == ResourceType.Population || type == ResourceType.Bread) continue;
                types.Add(type);
            }

            const int perRow = 3;
            const float rowHeight = 84f;
            const float columnWidth = 300f;
            var rows = (types.Count + perRow - 1) / perRow;
            var card = CreateImage(panelRoot.transform, "Card", new Color(0.16f, 0.18f, 0.15f, 0.98f));
            card.sprite = panelSprite;
            card.type = Image.Type.Sliced;
            var cardHeight = 230f + rows * rowHeight;
            SetRect(card.rectTransform, Vector2.zero, new Vector2(perRow * columnWidth + 60f, cardHeight));

            Localize(CreateText(card.transform, "Title", string.Empty, 34, new Vector2(0f, cardHeight * 0.5f - 50f), new Vector2(880f, 56f)), "#inventory_title");

            var icons = new Image[types.Count];
            var names = new Text[types.Count];
            var amounts = new Text[types.Count];
            var top = cardHeight * 0.5f - 120f;
            for (var i = 0; i < types.Count; i++)
            {
                var column = i % perRow;
                var row = i / perRow;
                var centre = new Vector2((column - (perRow - 1) * 0.5f) * columnWidth, top - row * rowHeight);
                var cell = CreateImage(card.transform, "Row_" + types[i], new Color(1f, 1f, 1f, 0.06f));
                SetRect(cell.rectTransform, centre, new Vector2(columnWidth - 12f, rowHeight - 10f));
                cell.raycastTarget = false;
                icons[i] = CreateImage(cell.transform, "Icon", Color.white);
                icons[i].preserveAspect = true;
                icons[i].raycastTarget = false;
                icons[i].sprite = iconLibrary != null ? iconLibrary.GetIcon(types[i]) : null;
                SetRect(icons[i].rectTransform, new Vector2(-columnWidth * 0.5f + 46f, 0f), new Vector2(56f, 56f));
                names[i] = CreateText(cell.transform, "Name", string.Empty, 22, new Vector2(10f, 12f), new Vector2(170f, 30f), new Color(1f, 1f, 1f, 0.8f));
                names[i].alignment = TextAnchor.MiddleLeft;
                names[i].resizeTextForBestFit = true;
                names[i].resizeTextMinSize = 14;
                names[i].resizeTextMaxSize = 22;
                amounts[i] = CreateText(cell.transform, "Amount", string.Empty, 28, new Vector2(10f, -16f), new Vector2(170f, 34f));
                amounts[i].alignment = TextAnchor.MiddleLeft;
            }

            var closeButton = CreateButton(card.transform, panelSprite, "CloseButton", "#ui_close", new Vector2(0f, -cardHeight * 0.5f + 56f), new Vector2(300f, 64f));

            var controller = panelRoot.AddComponent<InventoryPanelController>();
            var so = new SerializedObject(controller);
            so.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            var typesProp = so.FindProperty("types");
            typesProp.arraySize = types.Count;
            for (var i = 0; i < types.Count; i++) typesProp.GetArrayElementAtIndex(i).enumValueIndex = (int)types[i];
            WriteObjects(so.FindProperty("icons"), icons);
            WriteObjects(so.FindProperty("names"), names);
            WriteObjects(so.FindProperty("amounts"), amounts);
            so.FindProperty("iconLibrary").objectReferenceValue = iconLibrary;
            so.FindProperty("hud").objectReferenceValue = hud;
            so.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(closeButton.onClick, controller.ClosePanel);

            // under the citizens button, the same size: a sack -- everything the town holds
            var openButton = CreateGlyphButton(canvasParent, "InventoryButton", "round", HudInk.OnRound, "Bag", null, WorkforceButtonSize, 0.62f);
            Anchor(openButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(HudMargin + (RoundButton - WorkforceButtonSize) * 0.5f, InventoryButtonTop), new Vector2(WorkforceButtonSize, WorkforceButtonSize));
            UnityEventTools.AddPersistentListener(openButton.onClick, controller.OpenPanel);
            // a run's three resources are all on the bar: no sack there
            openButton.gameObject.AddComponent<SandboxOnly>();

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
            controllerSO.FindProperty("sandboxCard").objectReferenceValue = card.gameObject;
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

            BuildRunBuildingCard(panelRoot.transform, panelSprite, controller);

            panelRoot.SetActive(false);
            return controller;
        }

        /// <summary>
        /// A building's card after the user's sketch (2026-10-25; a run's first, the sandbox's too
        /// since the same day) -- see RunBuildingCard. Its upper and lower halves stand in two
        /// holders, so the sandbox's band (what it makes, a Плавильня's recipes) can open between
        /// them; the old card beside it is no longer shown.
        /// </summary>
        private static void BuildRunBuildingCard(Transform panelRoot, Sprite panelSprite, BuildingInfoPanelController controller)
        {
            var card = CreateImage(panelRoot, "RunCard", new Color(0.16f, 0.18f, 0.15f, 0.98f));
            card.sprite = panelSprite;
            card.type = Image.Type.Sliced;
            // under the wave line at the top of the screen
            SetRect(card.rectTransform, new Vector2(0f, -20f), new Vector2(700f, 800f));
            // drawn before the demolition question, which must stay on top
            card.transform.SetSiblingIndex(1);
            var runCard = card.gameObject.AddComponent<RunBuildingCard>();

            // the halves the band pushes apart
            var upper = new GameObject("Top", typeof(RectTransform));
            upper.transform.SetParent(card.transform, false);
            StretchFull((RectTransform)upper.transform);
            var lower = new GameObject("Bottom", typeof(RectTransform));
            lower.transform.SetParent(card.transform, false);
            StretchFull((RectTransform)lower.transform);

            var title = CreateText(upper.transform, "Title", string.Empty, 38, new Vector2(0f, 352f), new Vector2(640f, 60f));
            var level = CreateText(upper.transform, "Level", string.Empty, 28, new Vector2(0f, 310f), new Vector2(640f, 44f), new Color(1f, 1f, 1f, 0.85f));

            // a heart and a shield with their numbers
            var heart = CreateImage(upper.transform, "HealthIcon", new Color(0.85f, 0.25f, 0.22f));
            heart.sprite = HudGlyph("Heart");
            heart.preserveAspect = true;
            SetRect(heart.rectTransform, new Vector2(-150f, 266f), new Vector2(44f, 44f));
            var health = CreateText(upper.transform, "Health", string.Empty, 28, new Vector2(-50f, 266f), new Vector2(150f, 44f));
            health.alignment = TextAnchor.MiddleLeft;
            var shield = CreateImage(upper.transform, "DefenseIcon", new Color(0.3f, 0.45f, 0.75f));
            shield.sprite = HudGlyph("Shield");
            shield.preserveAspect = true;
            SetRect(shield.rectTransform, new Vector2(70f, 266f), new Vector2(44f, 44f));
            var defense = CreateText(upper.transform, "Defense", string.Empty, 28, new Vector2(150f, 266f), new Vector2(110f, 44f));
            defense.alignment = TextAnchor.MiddleLeft;

            // − workers / free +  (minus on the left, the user: «поменяй местами»)
            var workerRow = new GameObject("WorkerRow", typeof(RectTransform));
            workerRow.transform.SetParent(upper.transform, false);
            StretchFull((RectTransform)workerRow.transform);
            var minus = CreateDynamicButton(workerRow.transform, panelSprite, "MinusWorker", new Vector2(-230f, 208f), new Vector2(76f, 76f), out var minusLabel);
            minusLabel.text = "\u2212";
            minusLabel.fontSize = 44;
            var plus = CreateDynamicButton(workerRow.transform, panelSprite, "PlusWorker", new Vector2(230f, 208f), new Vector2(76f, 76f), out var plusLabel);
            plusLabel.text = "+";
            plusLabel.fontSize = 44;
            var workers = CreateText(workerRow.transform, "Workers", string.Empty, 26, new Vector2(0f, 224f), new Vector2(360f, 36f));
            var free = CreateText(workerRow.transform, "Free", string.Empty, 22, new Vector2(0f, 192f), new Vector2(360f, 32f), new Color(1f, 1f, 1f, 0.7f));
            UnityEventTools.AddPersistentListener(minus.onClick, controller.UnassignWorker);
            UnityEventTools.AddPersistentListener(plus.onClick, controller.AssignWorker);

            // the sandbox's band between the halves (hidden in a run): one line, and the recipe row
            const float bandHeight = 110f;
            var band = new GameObject("Band", typeof(RectTransform));
            band.transform.SetParent(card.transform, false);
            StretchFull((RectTransform)band.transform);
            var bandText = CreateText(band.transform, "Makes", string.Empty, 24, new Vector2(0f, 206f), new Vector2(640f, 34f), new Color(1f, 1f, 1f, 0.85f));
            bandText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bandText.resizeTextForBestFit = true;
            bandText.resizeTextMinSize = 16;
            bandText.resizeTextMaxSize = 24;
            var recipeRow = new GameObject("RecipeRow", typeof(RectTransform));
            recipeRow.transform.SetParent(band.transform, false);
            SetRect((RectTransform)recipeRow.transform, new Vector2(0f, 152f), new Vector2(660f, 58f));
            band.SetActive(false);

            // the next level: its picture and what it adds, over the picture's foot
            var next = new GameObject("NextLevel", typeof(RectTransform));
            next.transform.SetParent(lower.transform, false);
            StretchFull((RectTransform)next.transform);
            var picture = CreateImage(next.transform, "Picture", Color.white);
            picture.preserveAspect = true;
            picture.raycastTarget = false;
            SetRect(picture.rectTransform, new Vector2(0f, 82f), new Vector2(180f, 180f));
            var gains = new GameObject("Gains", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            gains.transform.SetParent(next.transform, false);
            SetRect((RectTransform)gains.transform, new Vector2(0f, 2f), new Vector2(640f, 50f));
            var gainsLayout = gains.GetComponent<HorizontalLayoutGroup>();
            gainsLayout.spacing = 8f;
            gainsLayout.childAlignment = TextAnchor.MiddleCenter;
            gainsLayout.childControlWidth = gainsLayout.childControlHeight = true;
            gainsLayout.childForceExpandWidth = gainsLayout.childForceExpandHeight = false;

            var upgradeBlock = new GameObject("UpgradeBlock", typeof(RectTransform));
            upgradeBlock.transform.SetParent(lower.transform, false);
            StretchFull((RectTransform)upgradeBlock.transform);
            Localize(CreateText(upgradeBlock.transform, "Heading", string.Empty, 28, new Vector2(0f, -50f), new Vector2(640f, 40f)), "#building_upgrade_cost");
            var upgradeCostRow = CreateCostRow(upgradeBlock.transform, "UpgradeCostRow", new Vector2(0f, -90f));
            var upgradeButton = CreateButton(upgradeBlock.transform, panelSprite, "UpgradeButton", "#building_upgrade", new Vector2(0f, -142f), new Vector2(360f, 70f));
            var upgradeLock = CreateText(upgradeBlock.transform, "UpgradeLock", string.Empty, 22, new Vector2(0f, -110f), new Vector2(640f, 60f), new Color(1f, 0.78f, 0.5f));
            UnityEventTools.AddPersistentListener(upgradeButton.onClick, controller.Upgrade);

            var repairBlock = new GameObject("RepairBlock", typeof(RectTransform));
            repairBlock.transform.SetParent(lower.transform, false);
            StretchFull((RectTransform)repairBlock.transform);
            Localize(CreateText(repairBlock.transform, "Heading", string.Empty, 28, new Vector2(0f, -198f), new Vector2(640f, 40f)), "#building_repair_cost");
            var repairCostRow = CreateCostRow(repairBlock.transform, "RepairCostRow", new Vector2(0f, -236f));
            var repairButton = CreateButton(repairBlock.transform, panelSprite, "RepairButton", "#building_repair", new Vector2(0f, -288f), new Vector2(360f, 70f));
            UnityEventTools.AddPersistentListener(repairButton.onClick, controller.Repair);

            var demolish = CreateButton(lower.transform, panelSprite, "DemolishButton", "#building_demolish", new Vector2(-170f, -355f), new Vector2(300f, 74f));
            demolish.GetComponent<Image>().color = new Color(0.45f, 0.2f, 0.18f, 0.95f);
            var close = CreateButton(lower.transform, panelSprite, "CloseButton", "#ui_close", new Vector2(170f, -355f), new Vector2(300f, 74f));
            UnityEventTools.AddPersistentListener(demolish.onClick, controller.OpenDemolishConfirm);
            UnityEventTools.AddPersistentListener(close.onClick, controller.Close);

            var keys = new List<string>();
            var sprites = new List<Object>();
            foreach (var path in AssetDatabase.FindAssets("t:Sprite", new[] { UiTextureImport.IconsFolder }))
            {
                var file = AssetDatabase.GUIDToAssetPath(path);
                var name = System.IO.Path.GetFileNameWithoutExtension(file);
                if (!System.Text.RegularExpressions.Regex.IsMatch(name, "_L[2-5]$")) continue;
                keys.Add(name);
                sprites.Add(AssetDatabase.LoadAssetAtPath<Sprite>(file));
            }

            var so = new SerializedObject(runCard);
            so.FindProperty("title").objectReferenceValue = title;
            so.FindProperty("level").objectReferenceValue = level;
            so.FindProperty("health").objectReferenceValue = health;
            so.FindProperty("defense").objectReferenceValue = defense;
            so.FindProperty("workerRow").objectReferenceValue = workerRow;
            so.FindProperty("workers").objectReferenceValue = workers;
            so.FindProperty("free").objectReferenceValue = free;
            so.FindProperty("nextLevel").objectReferenceValue = next;
            so.FindProperty("nextLevelImage").objectReferenceValue = picture;
            so.FindProperty("gains").objectReferenceValue = gains.transform;
            so.FindProperty("upgradeBlock").objectReferenceValue = upgradeBlock;
            so.FindProperty("upgradeCostRow").objectReferenceValue = upgradeCostRow;
            so.FindProperty("upgradeButton").objectReferenceValue = upgradeButton.gameObject;
            so.FindProperty("upgradeLock").objectReferenceValue = upgradeLock;
            so.FindProperty("repairBlock").objectReferenceValue = repairBlock;
            so.FindProperty("repairCostRow").objectReferenceValue = repairCostRow;
            so.FindProperty("demolishButton").objectReferenceValue = demolish.gameObject;
            so.FindProperty("card").objectReferenceValue = card.rectTransform;
            so.FindProperty("top").objectReferenceValue = upper.transform;
            so.FindProperty("bottom").objectReferenceValue = lower.transform;
            so.FindProperty("band").objectReferenceValue = band;
            so.FindProperty("bandText").objectReferenceValue = bandText;
            so.FindProperty("recipeRow").objectReferenceValue = recipeRow.transform;
            so.FindProperty("bandHeight").floatValue = bandHeight;
            var keyProp = so.FindProperty("levelIconKeys");
            keyProp.arraySize = keys.Count;
            for (var i = 0; i < keys.Count; i++) keyProp.GetArrayElementAtIndex(i).stringValue = keys[i];
            WriteObjects(so.FindProperty("levelIcons"), sprites.ToArray());
            so.FindProperty("gainPlate").objectReferenceValue = panelSprite;
            so.FindProperty("heartGlyph").objectReferenceValue = HudGlyph("Heart");
            so.FindProperty("shieldGlyph").objectReferenceValue = HudGlyph("Shield");
            so.FindProperty("houseGlyph").objectReferenceValue = HudGlyph("Home");
            so.FindProperty("workerGlyph").objectReferenceValue = HudGlyph("People");
            so.FindProperty("radiusGlyph").objectReferenceValue = HudGlyph("Radius");
            so.ApplyModifiedPropertiesWithoutUndo();

            var panelSO = new SerializedObject(controller);
            panelSO.FindProperty("runCard").objectReferenceValue = runCard;
            panelSO.ApplyModifiedPropertiesWithoutUndo();
            card.gameObject.SetActive(false);
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void WriteObjects(SerializedProperty property, Object[] values)
        {
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
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
        /// <summary>
        /// The Laboratory's window after the user's sketch (2026-10-28): «Лаборатория: уровень N» on a
        /// wooden plaque between the two tabs, the crystals and the status line under them, the
        /// rows of levels in a scroll list, and along the foot the scientists, the Laboratory's own
        /// upgrade and repair, «Закрыть» and the «!» hint. The rows are built at run time
        /// (ResearchPanelController.Populate).
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
            cardRect.sizeDelta = new Vector2(1760f, 1030f);
            cardRect.anchoredPosition = Vector2.zero;

            var wood = SkinSprite("pill_wood") ?? panelSprite;
            var plaque = CreateImage(card.transform, "TitlePlaque", Color.white);
            plaque.sprite = wood;
            plaque.type = Image.Type.Sliced;
            var plaqueRect = plaque.rectTransform;
            plaqueRect.anchorMin = plaqueRect.anchorMax = new Vector2(0.5f, 0.5f);
            plaqueRect.anchoredPosition = new Vector2(0f, 452f);
            plaqueRect.sizeDelta = new Vector2(560f, 76f);
            var title = CreateText(plaque.transform, "Title", string.Empty, 34, Vector2.zero, new Vector2(540f, 70f));
            title.fontStyle = FontStyle.Bold;

            var buildingsTab = CreateButton(card.transform, panelSprite, "BuildingsTab", "#research_tab_buildings", new Vector2(-560f, 452f), new Vector2(400f, 68f));
            var unitsTab = CreateButton(card.transform, panelSprite, "UnitsTab", "#research_tab_units", new Vector2(560f, 452f), new Vector2(400f, 68f));


            // the player's coins and upgrade crystals, top centre -- nothing else (the user's mockup)
            var crystalIcon = IconSprite("Res_UpgradeCrystal");
            Text Counter(string name, Sprite icon, float x)
            {
                var image = CreateImage(card.transform, name + "Icon", Color.white);
                image.sprite = icon;
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                image.rectTransform.anchoredPosition = new Vector2(x, 380f);
                image.rectTransform.sizeDelta = new Vector2(46f, 46f);
                var amount = CreateText(card.transform, name, "0", 30, new Vector2(x + 82f, 380f), new Vector2(110f, 44f), addShadow: false);
                amount.alignment = TextAnchor.MiddleLeft;
                amount.fontStyle = FontStyle.Bold;
                return amount;
            }
            var coins = Counter("Coins", iconLibrary != null ? iconLibrary.GetIcon(ResourceType.Coins) : null, -150f);
            var crystals = Counter("Crystals", crystalIcon, 40f);

            BuildScrollList(card.transform, new Vector2(0f, -8f), new Vector2(1700f, 720f),
                "#research_empty", out var content, out var emptyLabelGO);

            // the foot: scientists, the Laboratory's own upgrade and repair, close, the hint
            const float footY = -425f;
            var removeScientist = CreateButton(card.transform, panelSprite, "RemoveScientistButton", "-", new Vector2(-810f, footY), new Vector2(68f, 68f));
            var scientists = CreateText(card.transform, "Scientists", string.Empty, 22, new Vector2(-575f, footY), new Vector2(390f, 60f), new Color(1f, 1f, 1f, 0.9f), addShadow: false);
            var addScientist = CreateButton(card.transform, panelSprite, "AddScientistButton", "+", new Vector2(-340f, footY), new Vector2(68f, 68f));

            var upgradeControls = new GameObject("UpgradeControls", typeof(RectTransform));
            upgradeControls.transform.SetParent(card.transform, false);
            StretchFull(upgradeControls.GetComponent<RectTransform>());
            var upgradeButton = CreateButton(upgradeControls.transform, panelSprite, "UpgradeButton", "#research_upgrade_lab", new Vector2(-20f, footY), new Vector2(380f, 70f));

            var repairControls = new GameObject("RepairControls", typeof(RectTransform));
            repairControls.transform.SetParent(card.transform, false);
            StretchFull(repairControls.GetComponent<RectTransform>());
            var repairButton = CreateButton(repairControls.transform, panelSprite, "RepairButton", "#building_repair", new Vector2(345f, footY), new Vector2(330f, 70f));

            var closeButton = CreateButton(card.transform, panelSprite, "CloseButton", "#ui_close", new Vector2(640f, footY), new Vector2(230f, 70f));
            var hintButton = CreateButton(card.transform, panelSprite, "HintButton", string.Empty, new Vector2(815f, footY), new Vector2(72f, 72f));
            var hintGlyph = hintButton.GetComponentInChildren<Text>();
            var glyphKey = hintGlyph.GetComponent<LocalizedText>();
            if (glyphKey != null) Object.DestroyImmediate(glyphKey);
            hintGlyph.text = "!";
            hintGlyph.fontStyle = FontStyle.Bold;

            // the «!»'s card over the list; a tap anywhere on it puts it away
            var hint = CreateImage(card.transform, "Hint", Color.white);
            hint.sprite = panelSprite;
            hint.type = Image.Type.Sliced;
            hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            hint.rectTransform.anchoredPosition = new Vector2(0f, -40f);
            hint.rectTransform.sizeDelta = new Vector2(1180f, 380f);
            var hintText = CreateText(hint.transform, "Text", string.Empty, 26, Vector2.zero, new Vector2(1100f, 340f), addShadow: false);
            hintText.alignment = TextAnchor.MiddleLeft;
            var hintClose = hint.gameObject.AddComponent<Button>();
            hintClose.targetGraphic = hint;

            // the bubble over a tapped picture: what the level gives, or why it cannot start
            var bubble = CreateImage(card.transform, "Bubble", Color.white);
            bubble.sprite = panelSprite;
            bubble.type = Image.Type.Sliced;
            bubble.raycastTarget = false;
            bubble.rectTransform.anchorMin = bubble.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            bubble.rectTransform.sizeDelta = new Vector2(440f, 110f);
            var bubbleText = CreateText(bubble.transform, "Text", string.Empty, 22, Vector2.zero, new Vector2(410f, 96f), addShadow: false);

            // the small window «Улучшить лабораторию» / «Отремонтировать» opens: picture, what it gives, price, confirm
            var dialog = CreateImage(card.transform, "Dialog", new Color(0f, 0f, 0f, 0.55f));
            StretchFull(dialog.rectTransform);
            var dialogDismiss = dialog.gameObject.AddComponent<Button>();
            dialogDismiss.targetGraphic = dialog;
            var dialogCard = CreateImage(dialog.transform, "DialogCard", Color.white);
            dialogCard.sprite = panelSprite;
            dialogCard.type = Image.Type.Sliced;
            dialogCard.rectTransform.anchorMin = dialogCard.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            dialogCard.rectTransform.sizeDelta = new Vector2(680f, 620f);
            dialogCard.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;   // a tap on the card is not a tap outside it
            var dialogTitle = CreateText(dialogCard.transform, "Title", string.Empty, 32, new Vector2(0f, 250f), new Vector2(620f, 50f), addShadow: false);
            dialogTitle.fontStyle = FontStyle.Bold;
            var dialogIcon = CreateImage(dialogCard.transform, "Icon", Color.white);
            dialogIcon.preserveAspect = true;
            dialogIcon.raycastTarget = false;
            dialogIcon.rectTransform.anchorMin = dialogIcon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            dialogIcon.rectTransform.anchoredPosition = new Vector2(0f, 100f);
            dialogIcon.rectTransform.sizeDelta = new Vector2(220f, 220f);
            var dialogGain = CreateText(dialogCard.transform, "Gain", string.Empty, 22, new Vector2(0f, -55f), new Vector2(620f, 80f), addShadow: false);
            var dialogCostRow = CreateCostRow(dialogCard.transform, "CostRow", new Vector2(0f, -130f));
            var dialogConfirm = CreateDynamicButton(dialogCard.transform, panelSprite, "Confirm", new Vector2(-150f, -230f), new Vector2(280f, 76f), out var dialogConfirmLabel);
            var dialogClose = CreateButton(dialogCard.transform, panelSprite, "Close", "#ui_close", new Vector2(150f, -230f), new Vector2(240f, 76f));

            var keys = new List<string>();
            var sprites = new List<Object>();
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { UiTextureImport.IconsFolder }))
            {
                var file = AssetDatabase.GUIDToAssetPath(guid);
                keys.Add(System.IO.Path.GetFileNameWithoutExtension(file));
                sprites.Add(AssetDatabase.LoadAssetAtPath<Sprite>(file));
            }

            var controller = panelRoot.AddComponent<ResearchPanelController>();
            var so = new SerializedObject(controller);
            so.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            so.FindProperty("titleLabel").objectReferenceValue = title;
            so.FindProperty("coinsLabel").objectReferenceValue = coins;
            so.FindProperty("crystalsLabel").objectReferenceValue = crystals;
            so.FindProperty("bubble").objectReferenceValue = bubble.rectTransform;
            so.FindProperty("bubbleText").objectReferenceValue = bubbleText;
            so.FindProperty("dialog").objectReferenceValue = dialog.gameObject;
            so.FindProperty("dialogTitle").objectReferenceValue = dialogTitle;
            so.FindProperty("dialogIcon").objectReferenceValue = dialogIcon;
            so.FindProperty("dialogGain").objectReferenceValue = dialogGain;
            so.FindProperty("dialogCostRow").objectReferenceValue = dialogCostRow;
            so.FindProperty("dialogConfirmLabel").objectReferenceValue = dialogConfirmLabel;
            so.FindProperty("scientistsLabel").objectReferenceValue = scientists;
            so.FindProperty("addScientistButton").objectReferenceValue = addScientist;
            so.FindProperty("removeScientistButton").objectReferenceValue = removeScientist;
            so.FindProperty("upgradeControls").objectReferenceValue = upgradeControls;
            so.FindProperty("repairControls").objectReferenceValue = repairControls;
            so.FindProperty("buildingsTab").objectReferenceValue = buildingsTab.transform;
            so.FindProperty("unitsTab").objectReferenceValue = unitsTab.transform;
            so.FindProperty("listContent").objectReferenceValue = content;
            so.FindProperty("emptyLabel").objectReferenceValue = emptyLabelGO;
            so.FindProperty("hintPanel").objectReferenceValue = hint.gameObject;
            so.FindProperty("hintText").objectReferenceValue = hintText;
            so.FindProperty("rowSprite").objectReferenceValue = SkinSprite("card") ?? panelSprite;
            so.FindProperty("buttonSprite").objectReferenceValue = SkinSprite("pill_parchment") ?? panelSprite;
            so.FindProperty("plaqueSprite").objectReferenceValue = wood;
            so.FindProperty("completedIcon").objectReferenceValue = CreateCheckIcon();
            so.FindProperty("crystalIcon").objectReferenceValue = crystalIcon;
            so.FindProperty("iconLibrary").objectReferenceValue = iconLibrary;
            var keyProp = so.FindProperty("iconKeys");
            keyProp.arraySize = keys.Count;
            for (var i = 0; i < keys.Count; i++) keyProp.GetArrayElementAtIndex(i).stringValue = keys[i];
            WriteObjects(so.FindProperty("icons"), sprites.ToArray());
            so.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(addScientist.onClick, controller.AddScientist);
            UnityEventTools.AddPersistentListener(removeScientist.onClick, controller.RemoveScientist);
            UnityEventTools.AddPersistentListener(upgradeButton.onClick, controller.OpenUpgradeDialog);
            UnityEventTools.AddPersistentListener(repairButton.onClick, controller.OpenRepairDialog);
            UnityEventTools.AddPersistentListener(dialogConfirm.onClick, controller.ConfirmDialog);
            UnityEventTools.AddPersistentListener(dialogClose.onClick, controller.CloseDialog);
            UnityEventTools.AddPersistentListener(dialogDismiss.onClick, controller.CloseDialog);
            UnityEventTools.AddPersistentListener(buildingsTab.onClick, controller.SelectBuildingsTab);
            UnityEventTools.AddPersistentListener(unitsTab.onClick, controller.SelectUnitsTab);
            UnityEventTools.AddPersistentListener(closeButton.onClick, controller.Close);
            UnityEventTools.AddPersistentListener(hintButton.onClick, controller.ToggleHint);
            UnityEventTools.AddPersistentListener(hintClose.onClick, controller.ToggleHint);

            hint.gameObject.SetActive(false);
            bubble.gameObject.SetActive(false);
            dialog.gameObject.SetActive(false);
            panelRoot.SetActive(false);
            return controller;
        }
    }
}
