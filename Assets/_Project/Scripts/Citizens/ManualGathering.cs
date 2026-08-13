using CityBuilder.Maps;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Citizens
{
    /// <summary>
    /// Resolves a hand-gathered ResourceNode: grants the resource, then hands the node itself to
    /// whichever spawner owns it so it despawns and eventually comes back.
    ///
    /// Exists to break the game's bootstrap deadlock: production buildings are the only resource
    /// income, but every one of them costs the very resource it produces (no Wood -> no
    /// Lumberjack -> no Wood). A citizen sent to chop a tree by hand yields far less than a
    /// staffed building would, but it's always available, so the player can never be hard-stuck.
    /// </summary>
    public static class ManualGathering
    {
        // Deliberately small relative to building costs (a Lumberjack is 40 Wood) -- hand
        // gathering is the anti-deadlock floor, not a competitive alternative to actually
        // building an economy. First-pass, tunable.
        private const int WoodPerTree = 5;
        private const int StonePerRock = 4;

        /// <summary>How much of its own resource one node yields when gathered by hand. 0 for anything not hand-gatherable.</summary>
        public static int YieldFor(ResourceType resourceType)
        {
            switch (resourceType)
            {
                case ResourceType.Wood: return WoodPerTree;
                case ResourceType.Stone: return StonePerRock;
                default: return 0;
            }
        }

        /// <summary>
        /// Grants the node's yield and despawns it. Routed through the owning spawner
        /// (TreesAreaSpawner/RockSpawner) rather than destroyed directly, so its grid cell is
        /// freed and a replacement is scheduled exactly like a Lumberjack-felled tree already is.
        /// The plain Destroy fallback covers nodes from the legacy PNG map path
        /// (MapTerrainGenerator), which no spawner tracks.
        /// </summary>
        public static void Harvest(ResourceNode node)
        {
            if (node == null) return;

            var yield = YieldFor(node.ResourceType);
            if (yield > 0) ResourceManager.Instance?.Add(node.ResourceType, yield);

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
