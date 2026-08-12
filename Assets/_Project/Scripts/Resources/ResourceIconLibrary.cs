using UnityEngine;

namespace CityBuilder.Resources
{
    /// <summary>
    /// Runtime lookup from ResourceType to its icon sprite (see SetupProject.CreateResourceIcon,
    /// which builds these once at scene-generation time) -- used by BuildingInfoPanelController
    /// to render upgrade/repair costs as icon+number chips instead of "40 дерева, 24 камня" text.
    /// </summary>
    public class ResourceIconLibrary : ScriptableObject
    {
        // Indexed directly by (int)ResourceType -- simpler and faster than a dictionary for a
        // fixed, small enum, at the cost of needing to stay sized to cover every enum value.
        [SerializeField] private Sprite[] iconsByType = new Sprite[0];

        public void EditorInitialize(Sprite[] icons)
        {
            iconsByType = icons;
        }

        public Sprite GetIcon(ResourceType type)
        {
            var index = (int)type;
            return index >= 0 && index < iconsByType.Length ? iconsByType[index] : null;
        }
    }
}
