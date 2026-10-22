using System.Collections.Generic;
using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// Every workplace in the settlement in one list, with the +/- controls next to each.
    ///
    /// Reassigning a citizen used to mean finding the building that had them on the map, opening
    /// its card, taking one off, then finding the building that needed them and opening that -- and
    /// knowing, before starting, which of a dozen workshops was the one to rob. This is the same
    /// two buttons, gathered where the comparison can actually be made.
    ///
    /// A row per BUILDING, not per citizen: citizens have no identity in this game (CitizenManager
    /// is a headcount pair and each ProductionBuilding keeps its own count), and the thing the
    /// player is actually doing is moving hands between workplaces, not managing people.
    /// </summary>
    public class WorkforcePanelController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform listContent;
        [SerializeField] private GameObject emptyLabel;
        [SerializeField] private Text summaryLabel;
        [SerializeField] private Sprite rowSprite;

        private static readonly Color RowColor = UiTheme.RowNormal;
        private static readonly Color IdleRowColor = UiTheme.RowDim;
        private static readonly Color StarvedTextColor = UiTheme.Warning;

        /// <summary>One built row, kept so a tap can refresh the numbers instead of rebuilding the whole list.</summary>
        private class Row
        {
            public ProductionBuilding Building;
            public Image Background;
            public Text Detail;
            public Text Count;
            public Button Add;
            public Button Remove;
        }

        private readonly List<Row> _rows = new List<Row>();

        [Header("Citizens tab")]
        // The second tab (the user, 2026-10-20: "игрок должен иметь возможность посмотреть статус
        // каждого жителя"): a row per citizen -- the colour of his clothes, so he can be told apart
        // on the map, what he is doing this moment, and a button that takes the camera to him.
        [SerializeField] private GameObject workplacesView;
        [SerializeField] private GameObject citizensView;
        [SerializeField] private RectTransform citizenContent;
        [SerializeField] private GameObject citizensEmptyLabel;
        [SerializeField] private Image workplacesTab;
        [SerializeField] private Image citizensTab;

        private class CitizenRow
        {
            public CitizenAgent Agent;
            public Text Status;
            public Text Name;
        }

        private readonly List<CitizenRow> _citizenRows = new List<CitizenRow>();
        private float _refreshIn;

        /// <summary>How many citizens the citizens' tab lists -- for the PlayMode test, as RowCount is for the workplaces.</summary>
        public int CitizenRowCount => _citizenRows.Count;

        /// <summary>A citizen's line, as the tab shows it -- for the PlayMode test.</summary>
        public string CitizenTitle(int index) => index >= 0 && index < _citizenRows.Count ? _citizenRows[index].Name.text : null;

        /// <summary>A citizen's line, as the tab shows it -- for the PlayMode test.</summary>
        public string CitizenStatus(int index) => index >= 0 && index < _citizenRows.Count ? _citizenRows[index].Status.text : null;

        /// <summary>
        /// How many workplaces the list currently holds. Exposed for the PlayMode test, which
        /// cannot count the rows in the hierarchy instead: Destroy is deferred to the end of the
        /// frame, so a freshly repopulated list still has last time's row objects parented to it.
        /// </summary>
        public int RowCount => _rows.Count;

        public void OpenPanel()
        {
            Populate();
            ShowWorkplaces();
            if (panelRoot != null) panelRoot.SetActive(true);
            ModalGate.SetBlocked(true);
            // The buttons behind the panel go while it is open (the user's rule, 2026-10-14).
            if (hud != null) hud.Hide(this);
        }

        public void ClosePanel()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            ModalGate.SetBlocked(false);
            if (hud != null) hud.Show(this);
        }

        [SerializeField] private HudHider hud;

        public void ShowWorkplaces() => SelectTab(false);

        public void ShowCitizens()
        {
            PopulateCitizens();
            SelectTab(true);
        }

        private void SelectTab(bool citizens)
        {
            if (workplacesView != null) workplacesView.SetActive(!citizens);
            if (citizensView != null) citizensView.SetActive(citizens);
            if (workplacesTab != null) workplacesTab.color = citizens ? UiTheme.TabIdle : UiTheme.TabSelected;
            if (citizensTab != null) citizensTab.color = citizens ? UiTheme.TabSelected : UiTheme.TabIdle;
        }

        /// <summary>The citizens' lines move with what they do, so they are re-read a couple of times a second while the tab is open -- the rows themselves are kept.</summary>
        private void Update()
        {
            if (citizensView == null || !citizensView.activeInHierarchy) return;
            _refreshIn -= Time.unscaledDeltaTime;
            if (_refreshIn > 0f) return;
            _refreshIn = 0.4f;
            RefreshCitizens();
        }

        private void PopulateCitizens()
        {
            if (citizenContent == null) return;
            for (var i = citizenContent.childCount - 1; i >= 0; i--) Destroy(citizenContent.GetChild(i).gameObject);
            _citizenRows.Clear();

            var visuals = CitizenVisualsManager.Instance;
            if (visuals == null) return;
            for (var i = 0; i < visuals.AllAgents.Count; i++) CreateCitizenRow(visuals.AllAgents[i], i + 1);
            if (citizensEmptyLabel != null) citizensEmptyLabel.SetActive(_citizenRows.Count == 0);
            RefreshCitizens();
            _refreshIn = 0.4f;
        }

        private void RefreshCitizens()
        {
            var visuals = CitizenVisualsManager.Instance;
            var citizens = CitizenManager.Instance;
            if (visuals == null) return;
            if (_citizenRows.Count != visuals.AllAgents.Count)
            {
                PopulateCitizens();
                return;
            }

            // The sick are a count, not people (CitizenManager): the first of the jobless are the ones laid up.
            var sickLeft = citizens != null ? citizens.SickPopulation : 0;
            foreach (var row in _citizenRows)
            {
                if (row.Agent == null) continue;
                var workplace = visuals.WorkplaceOf(row.Agent);
                if (workplace == null && row.Agent.CurrentActivity == CitizenAgent.Activity.Idle && sickLeft > 0)
                {
                    sickLeft--;
                    row.Status.text = Localization.Get("#citizen_sick");
                    row.Status.color = UiTheme.Danger;
                    continue;
                }
                row.Status.text = Describe(row.Agent, workplace, visuals.CarriedBy(row.Agent));
                row.Status.color = workplace != null ? UiTheme.InkSoft : UiTheme.Good;
            }
        }

        /// <summary>One citizen's moment in words: free, or which job and which part of it.</summary>
        public static string Describe(CitizenAgent agent, ProductionBuilding workplace, int carried)
        {
            var place = workplace != null ? workplace.DisplayName : string.Empty;
            switch (agent.CurrentActivity)
            {
                case CitizenAgent.Activity.Order: return Localization.Get("#citizen_order");
                case CitizenAgent.Activity.Gathering: return Localization.Get("#citizen_gathering");
                case CitizenAgent.Activity.Idle: return workplace == null ? Localization.Get("#citizen_idle") : Localization.Format("#citizen_at_work", place);
                case CitizenAgent.Activity.ToWork: return Localization.Format("#citizen_to_work", place);
                case CitizenAgent.Activity.Harvesting:
                    return Localization.Format("#citizen_harvesting", place,
                        ResourceNames.Of(workplace != null ? workplace.ProducesResource : ResourceType.Wood), Mathf.RoundToInt(agent.HarvestProgress * 100f));
                case CitizenAgent.Activity.GoingHome:
                    return carried > 0 && workplace != null
                        ? Localization.Format("#citizen_carrying", place, ResourceNames.Of(workplace.ProducesResource), carried)
                        : Localization.Format("#citizen_to_work", place);
                default: return Localization.Format("#citizen_at_work", place);
            }
        }

        private void CreateCitizenRow(CitizenAgent agent, int number)
        {
            var rowGO = new GameObject($"Citizen_{number}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            rowGO.transform.SetParent(citizenContent, false);
            var layout = rowGO.GetComponent<LayoutElement>();
            layout.preferredHeight = layout.minHeight = 76f;
            var background = rowGO.GetComponent<Image>();
            background.sprite = rowSprite;
            background.type = Image.Type.Sliced;
            background.color = RowColor;

            // his clothes as a little figure, a body and a head, so he can be found on the map
            var swatch = new GameObject("Swatch", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            swatch.transform.SetParent(rowGO.transform, false);
            var swatchRect = swatch.rectTransform;
            swatchRect.anchorMin = swatchRect.anchorMax = new Vector2(0.5f, 0.5f);
            swatchRect.anchoredPosition = new Vector2(-395f, -8f);
            swatchRect.sizeDelta = new Vector2(30f, 38f);
            var body = agent.transform.Find("Body");
            var bodyRenderer = body != null ? body.GetComponent<Renderer>() : null;
            swatch.color = bodyRenderer != null ? bodyRenderer.sharedMaterial.color : UiTheme.InkSoft;
            var head = new GameObject("Head", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            head.transform.SetParent(swatch.transform, false);
            head.rectTransform.anchoredPosition = new Vector2(0f, 30f);
            head.rectTransform.sizeDelta = new Vector2(22f, 22f);
            head.color = new Color(0.85f, 0.68f, 0.55f);

            var name = CreateRowText(rowGO.transform, "Title", Localization.Format("#citizen_name", agent.CitizenName), 22, TextAnchor.MiddleLeft,
                new Vector2(-235f, 0f), new Vector2(240f, 40f), UiTheme.Ink);
            var status = CreateRowText(rowGO.transform, "Status", string.Empty, 22, TextAnchor.MiddleLeft,
                new Vector2(110f, 0f), new Vector2(440f, 60f), UiTheme.InkSoft);

            // "where is he": the camera goes to him
            var show = CreateRowButton(rowGO.transform, "Show", Localization.Get("#citizen_show"), new Vector2(390f, 0f));
            var captured = agent;
            show.onClick.AddListener(() => ShowCitizen(captured));

            _citizenRows.Add(new CitizenRow { Agent = agent, Status = status, Name = name });
        }

        /// <summary>Takes the camera to a citizen, closes the panel, and picks him up if he is free, so the next tap is his order.</summary>
        public void ShowCitizen(CitizenAgent agent)
        {
            if (agent == null) return;
            var rig = FindAnyObjectByType<CameraControl.RTSCameraController>();
            ClosePanel();
            if (rig != null) rig.FocusOn(agent.transform.position);
            var selector = FindAnyObjectByType<CitizenSelector>();
            if (selector != null) selector.SelectAgent(agent);
        }

        public void AssignTo(ProductionBuilding building)
        {
            if (building == null) return;
            building.TryAssignWorker();
            Refresh();
        }

        public void RemoveFrom(ProductionBuilding building)
        {
            if (building == null) return;
            building.TryUnassignWorker();
            Refresh();
        }

        private void Populate()
        {
            for (var i = listContent.childCount - 1; i >= 0; i--)
            {
                Destroy(listContent.GetChild(i).gameObject);
            }
            _rows.Clear();

            var workplaces = new List<ProductionBuilding>();
            foreach (var building in FindObjectsByType<ProductionBuilding>(FindObjectsSortMode.None))
            {
                if (building.MaxWorkers > 0) workplaces.Add(building);
            }

            // Alphabetical, so the same workshop is in the same place every time the panel opens --
            // scene order is whatever the engine happens to return and would shuffle between opens.
            workplaces.Sort((a, b) => string.CompareOrdinal(a.DisplayName, b.DisplayName));

            if (emptyLabel != null) emptyLabel.SetActive(workplaces.Count == 0);

            foreach (var workplace in workplaces)
            {
                CreateRow(workplace);
            }

            Refresh();
        }

        /// <summary>
        /// Re-reads the numbers into the existing rows. Rebuilding the list instead would throw
        /// away every row on each tap of +/-, which is both wasteful and visibly janky at a
        /// settlement's worth of buildings (see the mobile-first note in the design backlog).
        /// </summary>
        private void Refresh()
        {
            var citizens = CitizenManager.Instance;
            var idle = citizens != null ? citizens.IdlePopulation : 0;

            if (summaryLabel != null)
            {
                var total = citizens != null ? citizens.TotalPopulation : 0;
                summaryLabel.text = Localization.Format("#workforce_summary", idle, total);
            }

            var lostABuilding = false;
            foreach (var row in _rows)
            {
                // A workshop can decay to destruction while the panel is open -- the game keeps
                // running behind it (ModalGate blocks input, it does not stop time).
                if (row.Building == null)
                {
                    lostABuilding = true;
                    continue;
                }

                var assigned = row.Building.AssignedWorkers;
                var max = row.Building.MaxWorkers;

                row.Count.text = $"{assigned} / {max}";
                row.Background.color = assigned > 0 ? RowColor : IdleRowColor;
                // A gatherer with an empty radius says so INSTEAD of what it makes: "даёт Дерево"
                // is a promise the building cannot keep, and this panel is where the player comes
                // to ask why nothing is arriving. Warned rather than dimmed, since a staffed
                // building with nothing to gather still reads as busy on the row above.
                var stranded = row.Building.NothingLeftInReach;
                row.Detail.text = stranded
                    ? Localization.Format("#building_nothing_in_reach", ResourceNames.Of(row.Building.ProducesResource))
                    : DescribeWork(row.Building);
                row.Detail.color = assigned > 0 && !stranded ? UiTheme.InkSoft : StarvedTextColor;
                row.Add.interactable = assigned < max && idle > 0;
                row.Remove.interactable = assigned > 0;
            }

            if (lostABuilding) Populate();
        }

        /// <summary>
        /// What a workshop does, in one line: the resource it makes, and the one it eats if it is
        /// a converter. Public and static so the wording is covered by an EditMode test.
        /// </summary>
        public static string DescribeWork(ProductionBuilding building)
        {
            if (building == null) return string.Empty;

            // The Laboratory employs people and makes no resource at all: "ничего не производит"
            // would be true and useless, since scientists are the only reason it has worker slots.
            if (building.BuildingId == Research.ResearchManager.LaboratoryBuildingId) return Localization.Get("#workforce_research");

            var recipe = building.SelectedRecipe;
            if (recipe == null) return Localization.Get("#workforce_idle_building");

            return Localization.Format("#workforce_gives", recipe.Describe());
        }

        private void CreateRow(ProductionBuilding building)
        {
            var rowGO = new GameObject($"Workplace_{building.DisplayName}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            rowGO.transform.SetParent(listContent, false);

            var layoutElement = rowGO.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 88f;
            layoutElement.minHeight = 88f;

            var background = rowGO.GetComponent<Image>();
            background.sprite = rowSprite;
            background.type = Image.Type.Sliced;
            background.color = RowColor;

            var title = CreateRowText(rowGO.transform, "Title", building.DisplayName, 24, TextAnchor.LowerLeft,
                new Vector2(-140f, 16f), new Vector2(520f, 34f), UiTheme.Ink);
            var detail = CreateRowText(rowGO.transform, "Detail", string.Empty, 18, TextAnchor.UpperLeft,
                new Vector2(-140f, -18f), new Vector2(520f, 30f), UiTheme.InkSoft);
            var count = CreateRowText(rowGO.transform, "Count", string.Empty, 24, TextAnchor.MiddleCenter,
                new Vector2(190f, 0f), new Vector2(120f, 60f), UiTheme.Ink);

            var remove = CreateRowButton(rowGO.transform, "Remove", "-", new Vector2(290f, 0f));
            var add = CreateRowButton(rowGO.transform, "Add", "+", new Vector2(370f, 0f));

            var captured = building;
            remove.onClick.AddListener(() => RemoveFrom(captured));
            add.onClick.AddListener(() => AssignTo(captured));

            _rows.Add(new Row
            {
                Building = building,
                Background = background,
                Detail = detail,
                Count = count,
                Add = add,
                Remove = remove,
            });

            // Title never changes after this, so it is not kept on the Row.
            title.text = building.DisplayName;
        }

        private static Text CreateRowText(Transform parent, string name, string content, int fontSize, TextAnchor anchor, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var text = go.GetComponent<Text>();
            text.font = UiTheme.Regular;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.text = content;
            return text;
        }

        private Button CreateRowButton(Transform parent, string name, string label, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(64f, 64f);

            var image = go.GetComponent<Image>();
            image.sprite = rowSprite;
            image.type = Image.Type.Sliced;
            image.color = UiTheme.RowNormal;

            CreateRowText(go.transform, "Label", label, 30, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(64f, 64f), UiTheme.Ink);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            return button;
        }
    }
}
