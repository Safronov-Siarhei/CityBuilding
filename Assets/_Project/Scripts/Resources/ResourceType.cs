namespace CityBuilder.Resources
{
    public enum ResourceType
    {
        Wood,
        Stone,
        Food,
        Gold,
        Population,
        Iron,
        Coal,
        Coins,

        /// <summary>Wheat, straight off the farm and into the Амбар. The first link of the chain the Ферма starts and the Пекарня ends.</summary>
        Grain,

        /// <summary>Milled wheat. The Ветряк's only output and the Пекарня's only input -- nobody eats it, which is the point of a middle link.</summary>
        Flour,

        /// <summary>The chain's end: the first food the settlement makes rather than gathers.</summary>
        Bread
    }
}
