using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>
    /// Hands the harvest bars their look: the one material every bar shares (Shaders/HarvestBar)
    /// and the resource icons for the medallion. The bars are made on the fly by the nodes, far
    /// from anything that holds an asset reference, so the scene holds them here and passes them
    /// on once.
    /// </summary>
    public class HarvestBarSkin : MonoBehaviour
    {
        [SerializeField] private Material material;
        [SerializeField] private ResourceIconLibrary icons;

        private void Awake()
        {
            HarvestProgressBar.SharedMaterial = material;
            HarvestProgressBar.IconFor = icons != null ? icons.GetIcon : (System.Func<ResourceType, Sprite>)null;
        }

        private void OnDestroy()
        {
            if (HarvestProgressBar.SharedMaterial == material) HarvestProgressBar.SharedMaterial = null;
            HarvestProgressBar.IconFor = null;
        }
    }
}
