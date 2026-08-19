using CityBuilder.Core;
using CityBuilder.Maps;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Citizens
{
    /// <summary>
    /// Resolves a hand-gathered ResourceNode: takes one trip's worth out of it, and hands the
    /// node to whichever spawner owns it if that was the last of it.
    ///
    /// Exists to break the game's bootstrap deadlock: production buildings are the only resource
    /// income, but every one of them costs the very resource it produces (no Wood -> no
    /// Lumberjack -> no Wood). A citizen sent to chop a tree by hand yields far less than a
    /// staffed building would, but it's always available, so the player can never be hard-stuck.
    /// </summary>
    public static class ManualGathering
    {
        /// <summary>
        /// One trip's worth out of the node and into the stores, and the node itself removed only
        /// if that emptied it.
        ///
        /// A hand-gatherer takes exactly what a hired worker takes -- the node decides, not who is
        /// swinging the axe. So a tree still comes down in one go, and a boulder still needs ten
        /// visits, whether the player is doing it by hand or a Quarry is doing it for them.
        ///
        /// Despawning is routed through the owning spawner (TreesAreaSpawner/RockSpawner) rather
        /// than a direct Destroy, so the grid cell is freed -- and so a felled tree is replaced
        /// while an emptied boulder is not. The plain Destroy fallback covers nodes from the
        /// legacy PNG map path (MapTerrainGenerator), which no spawner tracks.
        /// </summary>
        public static void Harvest(ResourceNode node)
        {
            if (node == null) return;

            var taken = node.TakeYield();
            if (taken > 0) ResourceManager.Instance?.AddProduced(node.ResourceType, taken);

            if (!node.IsDepleted) return;

            if (node.ResourceType == ResourceType.Wood && TreesAreaSpawner.Instance != null)
            {
                TreesAreaSpawner.Instance.NotifyTreeHarvested(node.gameObject);
            }
            else if (node.ResourceType == ResourceType.Stone && RockSpawner.Instance != null)
            {
                RockSpawner.Instance.NotifyRockHarvested(node.gameObject);
            }
            else
            {
                Object.Destroy(node.gameObject);
            }
        }
    }
}
