namespace CityBuilder.Shop
{
    /// <summary>
    /// The rewarded-ad network's door, a stand-in until one is chosen. The shop shows «Смотреть» only
    /// while <see cref="IsReady"/> (the user, 2026-11-11: no fill -> only the crystal button). A real
    /// network answers here whether it has an ad loaded.
    /// </summary>
    public static class RewardedAds
    {
        /// <summary>The cheat panel's «no ads» switch, to see the shop without fill.</summary>
        public static bool NoFillForTest;

        /// <summary>An ad can be shown now.</summary>
        public static bool IsReady => !NoFillForTest;
    }
}
