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

        /// <summary>Timber, stone, ore as it comes out of the ground, coal. The Склад.</summary>
        Materials,

        /// <summary>Anything anyone eats, cooked or raw -- bread, vegetables, fruit, smoked and fresh meat and fish. The Кладовая.</summary>
        Food,

        /// <summary>Coins and ore smelted into bars. The Сокровищница.</summary>
        Valuables,

        /// <summary>Wheat, and only wheat. The Амбар: what comes off the fields before a mill has touched it.</summary>
        Grain,
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
                // Flour is not food, but the design puts it in the Кладовая alongside the bread it
                // becomes -- only the raw wheat gets a building of its own.
                case ResourceType.Food:
                case ResourceType.Flour:
                case ResourceType.Bread:
                    return ResourceStorageGroup.Food;
                case ResourceType.Grain:
                    return ResourceStorageGroup.Grain;
                // Smelted metal and money go in the Сокровищница; the ore they came out of is a
                // raw good and stays in the Склад with the timber and stone (see the default).
                case ResourceType.Coins:
                case ResourceType.IronBar:
                case ResourceType.CopperBar:
                case ResourceType.GoldBar:
                    return ResourceStorageGroup.Valuables;
                default:
                    return ResourceStorageGroup.Materials;
            }
        }
    }
}
