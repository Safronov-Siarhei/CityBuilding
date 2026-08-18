using CityBuilder.Buildings;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// Groups the building hotbar into categories (BuildingData.category): a row of category
    /// buttons picks which category's buildings the hotbar shows, so the hotbar doesn't need to
    /// fit every building at once as the roster grows. Discovers its category/building buttons
    /// via CategoryButtonHandler/HotbarButtonHandler already present among its children rather
    /// than needing separate wiring -- SetupProject only has to parent things under this
    /// GameObject correctly.
    /// </summary>
    public class BuildingCategoryPanel : MonoBehaviour
    {
        [SerializeField] private Color selectedColor = new Color(0.36f, 0.5f, 0.3f, 0.95f);
        [SerializeField] private Color unselectedColor = new Color(0.26f, 0.29f, 0.24f, 0.95f);

        private CategoryButtonHandler[] _categoryButtons;
        private HotbarButtonHandler[] _buildingButtons;

        // Which category is on show, so the hotbar can be rebuilt in place when a research opens a
        // new building -- without it, a finished research would only appear after the player tapped
        // a category button.
        private BuildingCategory _category;
        private bool _hasCategory;

        private void Awake()
        {
            _categoryButtons = GetComponentsInChildren<CategoryButtonHandler>(true);
            _buildingButtons = GetComponentsInChildren<HotbarButtonHandler>(true);
        }

        private void OnEnable()
        {
            var research = Research.ResearchManager.Instance;
            if (research != null) research.OnResearchChanged += HandleResearchChanged;

            // The hotbar spends the whole mandatory-Town-Hall placement deactivated (see
            // BuildingPlacerUIVisibility), so this runs again when it comes back -- catching up on
            // anything researched while it was hidden.
            if (_hasCategory) SelectCategory(_category);
        }

        private void OnDisable()
        {
            var research = Research.ResearchManager.Instance;
            if (research != null) research.OnResearchChanged -= HandleResearchChanged;
        }

        private void Start()
        {
            if (_categoryButtons.Length > 0) SelectCategory(_categoryButtons[0].Category);
        }

        private void HandleResearchChanged()
        {
            if (_hasCategory) SelectCategory(_category);
        }

        public void SelectCategory(BuildingCategory category)
        {
            _category = category;
            _hasCategory = true;

            foreach (var categoryButton in _categoryButtons)
            {
                var image = categoryButton.GetComponent<Image>();
                if (image != null) image.color = categoryButton.Category == category ? selectedColor : unselectedColor;
            }

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
    }
}
