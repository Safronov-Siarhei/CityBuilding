using CityBuilder.Core;
using CityBuilder.Diplomacy;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// «Подарок соседу» (the user's approved concept, 2026-11-17; art UI/Diplomacy/Gift): three chests -- small,
    /// medium, large -- for coins (diplomacy_gift_*_coins) that raise the relations (diplomacy_gift_*_relations),
    /// one gift a diplomacy day; once sent, a veil with the time till the next day.
    /// </summary>
    public class DiplomacyGiftWindow : MonoBehaviour
    {
        private static readonly string[] Sizes = { "Small", "Medium", "Large" };
        private const float RefreshSeconds = 0.25f;

        [SerializeField] private BakedUi ui;

        public static DiplomacyGiftWindow Instance { get; private set; }
        public bool IsOpen => ui != null && ui.IsOpen;

        private bool _bound;
        private float _refreshIn;

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
            for (var i = 0; i < Sizes.Length; i++)
            {
                var size = i;
                ui.OnTap(Sizes[i] + "Buy", () =>
                {
                    DiplomacyRules.SendGift(size);
                    Refresh();
                });
            }
        }

        public void Show()
        {
            if (ui == null) return;
            Bind();
            ui.Open();
            Refresh();
        }

        public void Close()
        {
            if (ui != null) ui.Hide();
        }

        private void Update()
        {
            if (!IsOpen) return;
            _refreshIn -= Time.unscaledDeltaTime;
            if (_refreshIn > 0f) return;
            _refreshIn = RefreshSeconds;
            Refresh();
        }

        private void Refresh()
        {
            if (!IsOpen) return;
            var config = BalanceConfig.Instance;
            if (config == null) return;
            for (var i = 0; i < Sizes.Length; i++)
            {
                ui.SetText(Sizes[i] + "Plus", Localization.Format("#diplomacy_gift_plus", config.DiplomacyGiftRelations(i)));
                ui.SetText(Sizes[i] + "Price", config.DiplomacyGiftCoins(i).ToString());
                ui.Interactable(Sizes[i] + "Buy", DiplomacyRules.CanGift(i));
            }
            var sent = DiplomacyRules.GiftSentToday;
            ui.Show("Sent", sent);
            if (sent) ui.SetText("TimerText", Shop.OfferShopWindow.Clock(Mathf.CeilToInt(DiplomacyRules.SecondsToNextDay)));
        }
    }
}
