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

        private void Awake()
        {
            _categoryButtons = GetComponentsInChildren<CategoryButtonHandler>(true);
            _buildingButtons = GetComponentsInChildren<HotbarButtonHandler>(true);
        }

        private void Start()
        {
            if (_categoryButtons.Length > 0) SelectCategory(_categoryButtons[0].Category);
        }

        public void SelectCategory(BuildingCategory category)
        {
            foreach (var categoryButton in _categoryButtons)
            {
                var image = categoryButton.GetComponent<Image>();
                if (image != null) image.color = categoryButton.Category == category ? selectedColor : unselectedColor;
            }

            foreach (var buildingButton in _buildingButtons)
            {
                buildingButton.gameObject.SetActive(buildingButton.Building != null && buildingButton.Building.category == category);
            }
        }
    }
}
