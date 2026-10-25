namespace CityBuilder.Core
{
    /// <summary>
    /// What the weather does to everyone at once (the New Year map's blizzard, InvasionNewYear): how
    /// fast anyone walks and how far a tower shoots, as multiples. 1 and 1 when the sky is clear --
    /// set back so at the start of every run and when the blizzard ends.
    /// </summary>
    public static class Weather
    {
        public static float SpeedMultiplier { get; set; } = 1f;
        public static float TowerRangeMultiplier { get; set; } = 1f;

        public static void Clear()
        {
            SpeedMultiplier = 1f;
            TowerRangeMultiplier = 1f;
        }
    }
}
