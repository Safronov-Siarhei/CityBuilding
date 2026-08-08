using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>
    /// A harvestable tree/rock prop spawned by MapTerrainGenerator on Forest/Stone tiles.
    /// Purely a visual anchor for workers to walk to — permanent, no depletion (see
    /// CitizenVisualsManager for how workers claim/release these).
    /// </summary>
    public class ResourceNode : MonoBehaviour
    {
        public ResourceType ResourceType { get; private set; }
        public bool IsClaimed { get; private set; }

        public void Initialize(ResourceType resourceType)
        {
            ResourceType = resourceType;
        }

        public bool TryClaim()
        {
            if (IsClaimed) return false;
            IsClaimed = true;
            return true;
        }

        public void Release()
        {
            IsClaimed = false;
        }
    }
}
