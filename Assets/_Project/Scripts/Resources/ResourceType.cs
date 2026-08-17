namespace CityBuilder.Resources
{
    /// <summary>
    /// Every resource the settlement can hold.
    ///
    /// Two names are historical and mean something narrower than they read: <see cref="Iron"/> and
    /// <see cref="Gold"/> are the ORE as it comes out of the mine, not the smelted metal -- the
    /// Плавильня turns them into <see cref="IronBar"/> and <see cref="GoldBar"/>. They keep their
    /// short names because the whole balance sheet and every building cost already spells them
    /// that way; what a player reads comes from ResourceNames.
    /// </summary>
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
        Bread,

        /// <summary>Copper ore. The third mine's output, and the only one of the three whose name says "ore" out loud.</summary>
        CopperOre,

        /// <summary>Smelted metal -- what the Плавильня makes out of ore and coal, and what anything better than timber and stone is built from.</summary>
        IronBar,
        CopperBar,
        GoldBar
    }
}
