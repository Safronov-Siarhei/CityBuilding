namespace CityBuilder.Resources
{
    /// <summary>
    /// Which storehouse a resource belongs in. Capacity is tracked per group rather than per
    /// resource: a building would otherwise need one capacity column per resource in the balance
    /// sheet, and "how much timber fits versus how much iron fits" is not a distinction the game
    /// has any use for -- what matters is that grain and gold need different buildings.
    /// </summary>
    public enum ResourceStorageGroup
    {
        /// <summary>Not stored at all -- population is a headcount, not something a warehouse holds.</summary>
        None,
        Materials,
        Food,
        Valuables,
    }

    public static class ResourceStorage
    {
        /// <summary>The storehouse a resource needs. Anything new defaults to Materials, which is the group that holds raw goods.</summary>
        public static ResourceStorageGroup GroupOf(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Population:
                    return ResourceStorageGroup.None;
                case ResourceType.Food:
                    return ResourceStorageGroup.Food;
                case ResourceType.Gold:
                case ResourceType.Coins:
                    return ResourceStorageGroup.Valuables;
                default:
                    return ResourceStorageGroup.Materials;
            }
        }
    }
}
