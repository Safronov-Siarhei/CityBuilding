using CityBuilder.Core;
using CityBuilder.Diplomacy;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// «Торговля с соседом» (the user's approved concept, 2026-11-17; art UI/Diplomacy/Trade): up to ten
    /// offers (the trade rows of «Зал дипломатии: таблицы», rare ones in a purple card), diplomacy_trade_deals
    /// deals until the offers change; a deal taken gets the «КУПЛЕНО» seal, the rest are locked once the
    /// deals run out. New offers every diplomacy_trade_refresh_sec, or now for an ad or crystals.
    /// </summary>
    public class DiplomacyTradeWindow : MonoBehaviour
    {
        public const int Slots = 10;
        private const float RefreshSeconds = 0.25f;

        [SerializeField] private BakedUi ui;

        public static DiplomacyTradeWindow Instance { get; private set; }
        public bool IsOpen => ui != null && ui.IsOpen;

        private bool _bound;
        private float _refreshIn;
        private float _adLeft = -1f;

        private void Awake()
        {
            Instance = this;
            Bind();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Bind()
        {
            if (_bound || ui == null) return;
            _bound = true;
            ui.OnTap("Close", Close);
            for (var i = 0; i < Slots; i++)
            {
                var slot = i;
                ui.OnTap("Offer" + i + "Swap", () =>
                {
                    DiplomacyRules.Swap(slot);
                    Refresh();
                });
            }
            ui.OnTap("Ad", () =>
            {
                if (_adLeft >= 0f || !Shop.RewardedAds.IsReady) return;
                _adLeft = 0f;
                Shop.RewardedAds.Show("diplomacy", paid =>
                {
                    if (_adLeft < 0f) return;   // the window was closed meanwhile
                    _adLeft = -1f;
                    if (paid) DiplomacyRules.NewOffers();
                    Refresh();
                });
            });
            ui.OnTap("Crystals", () =>
            {
                if (!DiplomacyRules.RefreshForCrystals()) Shop.OfferShopWindow.Show();
                Refresh();
            });
        }

        public void Show()
        {
            if (ui == null) return;
            Bind();
            DiplomacyRules.EnsureOffers();
            ui.Open();
            Refresh();
        }

        public void Close()
        {
            _adLeft = -1f;
            if (ui != null) ui.Hide();
        }

        private void Update()
        {
            if (!IsOpen) return;
            var dt = Time.unscaledDeltaTime;
            _refreshIn -= dt;
            if (_refreshIn > 0f) return;
            _refreshIn = RefreshSeconds;
            Refresh();
        }

        private void Refresh()
        {
            if (!IsOpen) return;
            var config = BalanceConfig.Instance;
            if (config == null) return;
            // the relations fell below trade while it was open: the window shuts
            if (!DiplomacyRules.TradeOpen)
            {
                Close();
                return;
            }
            var total = config.DiplomacyTradeDeals;
            ui.SetText("Deals", Localization.Format("#diplomacy_trade_deals", Mathf.Max(0, total - DiplomacyRules.DealsDone), total));
            ui.SetText("TimerText", Shop.OfferShopWindow.Clock(Mathf.CeilToInt(DiplomacyRules.RefreshLeft)));
            var dealsLeft = DiplomacyRules.DealsLeft;
            for (var i = 0; i < Slots; i++)
            {
                var g = "Offer" + i;
                var row = DiplomacyRules.Offer(i);
                ui.Show(g, row != null);
                if (row == null) continue;
                ui.Show(g + "Card", !row.rare);
                ui.Show(g + "CardRare", row.rare);
                ui.Show(g + "Rare", row.rare);
                ui.SetIcon(g + "Give", row.resource);
                ui.SetIcon(g + "Get", row.get);
                ui.SetText(g + "GiveN", row.amount.ToString());
                ui.SetText(g + "GetN", DiplomacyRules.GetAmount(row).ToString());
                var taken = DiplomacyRules.Taken(i);
                ui.Show(g + "Bought", taken);
                ui.Show(g + "Locked", !taken && !dealsLeft);
                ui.Show(g + "Swap", !taken && dealsLeft);
                ui.Interactable(g + "Swap", DiplomacyRules.CanSwap(i));
            }
            ui.Show("Ad", Shop.RewardedAds.IsReady || _adLeft >= 0f);
            ui.Interactable("Ad", _adLeft < 0f);
            ui.SetText("CrystalsLabel", config.DiplomacyTradeRefreshCrystals.ToString());
        }
    }
}
