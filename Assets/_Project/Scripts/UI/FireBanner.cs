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

        /// <summary>
        /// The text, and under it what putting it out also gives back (the user, 2026-11-19): fire_comp_share of the restore
        /// price of every building this fire took down (FireManager.Compensation), handed over by EndIt.
        /// </summary>
        protected override string BodyText
        {
            get
            {
                // Refresh runs every frame: the words are made at most once a second (the game stands still under it anyway)
                if (_text != null && UnityEngine.Time.unscaledTime < _textUntil) return _text;
                _textUntil = UnityEngine.Time.unscaledTime + 1f;
                var text = base.BodyText;
                var fires = Buildings.FireManager.Instance;
                var comp = fires != null ? fires.Compensation() : null;
                _text = comp == null || comp.Count == 0
                    ? text
                    : text + "\n" + Localization.Format("#fire_banner_comp", Buildings.FireManager.CompensationText(comp));
                return _text;
            }
        }

        private string _text;
        private float _textUntil;

        protected override void EndIt(int island) => Buildings.FireManager.Instance?.EndByBanner();
    }
}
