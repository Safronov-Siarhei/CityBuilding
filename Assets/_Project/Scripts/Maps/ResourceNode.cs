using System.Collections.Generic;
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
        // Self-registering registry so CitizenVisualsManager.FindNearestFreeNode can iterate
        // active nodes directly instead of scanning the whole scene with FindObjectsByType every
        // time a worker needs a node -- matters once tree counts get into the hundreds.
        private static readonly List<ResourceNode> _all = new List<ResourceNode>();
        public static IReadOnlyList<ResourceNode> All => _all;

        public ResourceType ResourceType { get; private set; }
        public bool IsClaimed { get; private set; }

        public void Initialize(ResourceType resourceType)
        {
            ResourceType = resourceType;
        }

        private void OnEnable()
        {
            _all.Add(this);
        }

        private void OnDisable()
        {
            _all.Remove(this);
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
