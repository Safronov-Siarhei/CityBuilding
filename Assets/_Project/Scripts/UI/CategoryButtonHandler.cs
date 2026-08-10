using CityBuilder.Buildings;
using UnityEngine;

namespace CityBuilder.UI
{
    public class CategoryButtonHandler : MonoBehaviour
    {
        [SerializeField] private BuildingCategoryPanel panel;
        [SerializeField] private BuildingCategory category;

        public BuildingCategory Category => category;

        public void SelectThisCategory()
        {
            if (panel != null) panel.SelectCategory(category);
        }
    }
}
