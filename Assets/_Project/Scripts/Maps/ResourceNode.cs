using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>
    /// A harvestable tree or boulder. Not a visual anchor any more: what a worker carries home
    /// comes out of THIS, so the node is where the settlement's wood and stone actually live.
    ///
    /// Every node holds a stock and gives up a slice of it per visit. The difference between a
    /// tree and a boulder is entirely in those two numbers: a tree's slice is its whole stock, so
    /// one visit fells it and the forest regrows it a minute later; a boulder holds many slices
    /// and, once empty, is gone from the map for good (see RockSpawner). That is the whole of the
    /// depletion mechanic — the map has a finite amount of stone in it and no more is ever made.
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

        /// <summary>How much of its resource is left in this node. Reaching zero is what removes it from the map.</summary>
        public int RemainingYield { get; private set; }

        /// <summary>What the node held when it appeared -- the denominator of the progress bar's "how picked-over is this" reading.</summary>
        public int TotalYield { get; private set; }

        public bool IsDepleted => RemainingYield <= 0;

        private HarvestProgressBar _progressBar;

        public void Initialize(ResourceType resourceType)
        {
            ResourceType = resourceType;
            TotalYield = TotalYieldFor(resourceType);
            RemainingYield = TotalYield;
        }

        /// <summary>Used by save/load to put a part-worked boulder back exactly as picked-over as it was.</summary>
        public void SetRemainingYield(int remaining)
        {
            RemainingYield = Mathf.Clamp(remaining, 0, Mathf.Max(TotalYield, remaining));
        }

        /// <summary>Everything one node of this kind holds, from the sheet. Zero for anything nobody harvests.</summary>
        public static int TotalYieldFor(ResourceType resourceType)
        {
            switch (resourceType)
            {
                case ResourceType.Wood: return BalanceConfig.Instance.WoodPerTree;
                case ResourceType.Stone: return BalanceConfig.Instance.StonePerRock;
                default: return 0;
            }
        }

        /// <summary>What one visit carries away, before the node's own remaining stock caps it.</summary>
        public static int YieldPerHarvestFor(ResourceType resourceType)
        {
            switch (resourceType)
            {
                case ResourceType.Wood: return BalanceConfig.Instance.WoodPerHarvest;
                case ResourceType.Stone: return BalanceConfig.Instance.StonePerHarvest;
                default: return 0;
            }
        }

        /// <summary>
        /// One visit's worth out of the node, capped by what is actually left in it -- a boulder
        /// with one stone in it hands over one, not the full slice. Returns what was taken so the
        /// caller can credit exactly that and no more.
        /// </summary>
        public int TakeYield()
        {
            var taken = Mathf.Min(YieldPerHarvestFor(ResourceType), RemainingYield);
            if (taken <= 0) return 0;

            RemainingYield -= taken;
            return taken;
        }

        /// <summary>
        /// Shows how far along the current dig is, 0 to 1. Called every frame by whoever is
        /// working this node; the bar is built on the first call and never exists at all for the
        /// hundreds of nodes nobody is standing at.
        /// </summary>
        public void ReportHarvestProgress(float progress)
        {
            if (_progressBar == null) _progressBar = HarvestProgressBar.CreateFor(transform);
            _progressBar.Report(progress);
        }

        /// <summary>Hides the bar -- the worker left, was reassigned, or finished.</summary>
        public void ClearHarvestProgress()
        {
            if (_progressBar != null) _progressBar.Hide();
        }

        private void OnEnable()
        {
            _all.Add(this);
        }

        private void OnDisable()
        {
            _all.Remove(this);
            ClearHarvestProgress();
        }

        /// <summary>The bar lives in world space rather than under this transform (see HarvestProgressBar.CreateFor for why), so it has to be taken down by hand instead of going with the tree.</summary>
        private void OnDestroy()
        {
            if (_progressBar != null) Destroy(_progressBar.gameObject);
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
            ClearHarvestProgress();
        }
    }
}
