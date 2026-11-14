using CityBuilder.Core;

namespace CityBuilder.UI
{
    /// <summary>
    /// «Чума!» (the user, 2026-11-13): once illness has killed plague_banner_share of an island since
    /// its plague broke out (Citizens/Plague), the game pauses and this asks whether to end it now for
    /// an ad or plague_end_crystals; ended, the town is shielded for plague_shield_sec.
    /// </summary>
    public class PlagueBanner : PauseBanner
    {
        public static PlagueBanner Instance { get; private set; }

        protected override void Awake()
        {
            Instance = this;
            base.Awake();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        protected override string Stem => "plague_banner";

        protected override int Price => BalanceConfig.Instance.PlagueEndCrystals;

        protected override void EndIt(int island) => Citizens.Plague.Instance?.EndByBanner(island);
    }
}
