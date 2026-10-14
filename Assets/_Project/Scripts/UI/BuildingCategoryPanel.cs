using CityBuilder.Buildings;
using CityBuilder.InputControl;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The build menu at the bottom of the screen: a row of category tabs and, above the screen's
    /// edge, the cards of whichever category is open (BuildingData.category). Discovers its
    /// category/building buttons via CategoryButtonHandler/HotbarButtonHandler already present among
    /// its children rather than needing separate wiring -- SetupProject only has to parent things
    /// under this GameObject correctly.
    ///
    /// **Closed until the player opens it** (the user's layout, 2026-10-14): at rest only the tabs
    /// show, sitting on the bottom edge, so the player does not see what is in a category before
    /// tapping it. Tapping a tab slides the row up and opens that category's cards beneath it;
    /// tapping another tab switches; tapping the open tab again, or tapping the map, closes it and
    /// the row slides back down. A map tap while a building is being placed does not close it --
    /// that tap belongs to the placement.
    /// </summary>
    public class BuildingCategoryPanel : MonoBehaviour
    {
        [SerializeField] private Color selectedColor = new Color(0.36f, 0.5f, 0.3f, 0.95f);
        [SerializeField] private Color unselectedColor = new Color(0.26f, 0.29f, 0.24f, 0.95f);

        // The skin's way of marking the open tab (see UiTheme): parchment for the open one, dark
        // wood for the rest, each sprite carrying its own colours. When both are set they replace
        // the tint above, which only made sense on the old grey stone.
        [SerializeField] private Sprite selectedSprite;
        [SerializeField] private Sprite unselectedSprite;

        [Header("Open and closed")]
        [SerializeField] private RectTransform categoryBar;
        [SerializeField] private GameObject hotbar;
        [SerializeField] private BuildingPlacer buildingPlacer;
        /// <summary>The tab row's height off the bottom while closed, and while the cards are open under it.</summary>
        [SerializeField] private float closedBarY = 20f;
        [SerializeField] private float openBarY = 232f;

        /// <summary>How fast the row slides to its new height -- a rate, not a duration.</summary>
        private const float SlideSharpness = 18f;

        private CategoryButtonHandler[] _categoryButtons;
        private HotbarButtonHandler[] _buildingButtons;

        // Which category is open, so the cards can be rebuilt in place when a research opens a new
        // building -- without it, a finished research would only appear after the next tab tap.
        private BuildingCategory _category;
        private bool _open;
        private bool _subscribedToTaps;

        public bool IsOpen => _open;
        public BuildingCategory OpenCategory => _category;

        private void Awake()
        {
            _categoryButtons = GetComponentsInChildren<CategoryButtonHandler>(true);
            _buildingButtons = GetComponentsInChildren<HotbarButtonHandler>(true);
            Close(true);
        }

        private void OnEnable()
        {
            var research = Research.ResearchManager.Instance;
            if (research != null) research.OnResearchChanged += HandleResearchChanged;
            SubscribeToTaps();

            // The menu spends the whole mandatory-Town-Hall placement deactivated (see
            // BuildingPlacerUIVisibility), so this runs again when it comes back -- catching up on
            // anything researched while it was hidden.
            if (_open) ShowCards(_category);
        }

        // Again in Start: the touch router may wake after this does.
        private void Start() => SubscribeToTaps();

        private void OnDisable()
        {
            var research = Research.ResearchManager.Instance;
            if (research != null) research.OnResearchChanged -= HandleResearchChanged;
            if (_subscribedToTaps && TouchInputRouter.Instance != null) TouchInputRouter.Instance.Tapped -= HandleMapTap;
            _subscribedToTaps = false;
        }

        private void SubscribeToTaps()
        {
            if (_subscribedToTaps || TouchInputRouter.Instance == null) return;
            TouchInputRouter.Instance.Tapped += HandleMapTap;
            _subscribedToTaps = true;
        }

        private void Update()
        {
            if (categoryBar == null) return;
            var target = _open ? openBarY : closedBarY;
            var position = categoryBar.anchoredPosition;
            if (Mathf.Abs(position.y - target) < 0.5f)
            {
                if (position.y != target) categoryBar.anchoredPosition = new Vector2(position.x, target);
                return;
            }
            var t = 1f - Mathf.Exp(-SlideSharpness * Time.unscaledDeltaTime);
            categoryBar.anchoredPosition = new Vector2(position.x, Mathf.Lerp(position.y, target, t));
        }

        private void HandleResearchChanged()
        {
            if (_open) ShowCards(_category);
        }

        /// <summary>A tap on a category's tab: opens it, switches to it, or -- when it is the one already open -- closes the menu.</summary>
        public void TapCategory(BuildingCategory category)
        {
            if (_open && _category == category) Close();
            else Open(category);
        }

        /// <summary>A tap on the map, from the touch router (only taps that did not start on the interface reach it).</summary>
        public void HandleMapTap(Vector2 screenPosition)
        {
            if (!_open) return;
            if (buildingPlacer != null && buildingPlacer.IsSelecting) return;
            Close();
        }

        public void Open(BuildingCategory category, bool instant = false)
        {
            _category = category;
            _open = true;
            if (hotbar != null) hotbar.SetActive(true);
            ShowCards(category);
            if (instant) SnapBar();
        }

        public void Close(bool instant = false)
        {
            _open = false;
            if (hotbar != null) hotbar.SetActive(false);
            PaintTabs(null);
            if (instant) SnapBar();
        }

        private void SnapBar()
        {
            if (categoryBar == null) return;
            categoryBar.anchoredPosition = new Vector2(categoryBar.anchoredPosition.x, _open ? openBarY : closedBarY);
        }

        private void ShowCards(BuildingCategory category)
        {
            PaintTabs(category);

            foreach (var buildingButton in _buildingButtons)
            {
                var building = buildingButton.Building;
                // A building nobody has researched yet is absent, not greyed: the hotbar is a row of
                // icons with no room to explain itself, and the Laboratory's window is where the
                // player is told what is still locked and what it costs.
                var show = building != null
                           && building.category == category
                           && Research.ResearchManager.BuildingUnlocked(building.buildingName);
                buildingButton.gameObject.SetActive(show);
            }
        }

        /// <summary>The open tab in parchment, the rest in wood; null paints them all as closed.</summary>
        private void PaintTabs(BuildingCategory? open)
        {
            if (_categoryButtons == null) return;
            foreach (var categoryButton in _categoryButtons)
            {
                var image = categoryButton.GetComponent<Image>();
                if (image == null) continue;
                var selected = open.HasValue && categoryButton.Category == open.Value;
                if (selectedSprite != null && unselectedSprite != null)
                {
                    image.sprite = selected ? selectedSprite : unselectedSprite;
                    image.color = Color.white;
                }
                else
                {
                    image.color = selected ? selectedColor : unselectedColor;
                }
            }
        }
    }
}
