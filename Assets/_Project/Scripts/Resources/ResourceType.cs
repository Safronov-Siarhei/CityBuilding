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

        /// <summary>
        /// Wheat, straight off the farm. Nothing produces or consumes it yet -- it exists because
        /// the Амбар exists to hold it and nothing else, and a storehouse for a resource the game
        /// has no name for is a building that can never be explained to a player. The mill and the
        /// bakery are what will give it a purpose (see the processing chains in the design backlog).
        /// </summary>
        Grain
    }
}
