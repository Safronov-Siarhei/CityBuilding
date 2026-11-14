using CityBuilder.Core;

namespace CityBuilder.UI
{
    /// <summary>
    /// «Пожар!» (the user, 2026-11-14; art Tools/ui/fire_banner_art.py, approved): once fire has destroyed
    /// fire_banner_share of the player's buildings within fire_banner_window_sec (Buildings/FireManager),
    /// the game pauses and this offers to put EVERY fire out for an ad or fire_end_crystals; then no fire
    /// starts for fire_shield_sec.
    /// </summary>
    public class FireBanner : PauseBanner
    {
        public static FireBanner Instance { get; private set; }

        protected override void Awake()
        {
            Instance = this;
            base.Awake();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        protected override string Stem => "fire_banner";

        protected override int Price => BalanceConfig.Instance.FireEndCrystals;

        protected override void EndIt(int island) => Buildings.FireManager.Instance?.EndByBanner();
    }
}
