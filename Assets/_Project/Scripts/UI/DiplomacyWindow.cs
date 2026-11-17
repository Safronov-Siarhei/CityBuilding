using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Diplomacy;
using UnityEngine;

namespace CityBuilder.UI
{
    /// <summary>
    /// The Diplomacy Hall's window (redone after the user's approved concept, 2026-11-17; art from
    /// Tools/ui/diplomacy_game_art.py, folder UI/Diplomacy/Window): the two kings, the relations number and
    /// how much it falls a day, the scale (danger / trade / gifts) with its needle, then at peace «Подарок»,
    /// «Торговля» (locked below bot_trade_min_score) and «Война» (a second tap declares it), at war the wave
    /// and the ransom in coins or crystals. Opened by the round «Дипломатия» button beside the «!»
    /// (DiplomacyBar); the «!» itself opens the hall's ordinary card.
    /// </summary>
    public class DiplomacyWindow : MonoBehaviour
    {
        private const float RefreshSeconds = 0.25f;
        private const float WarConfirmSeconds = 3f;

        [SerializeField] private BakedUi ui;

        public static DiplomacyWindow Instance { get; private set; }
        public bool IsOpen => ui != null && ui.IsOpen;

        private bool _bound;
        private float _refreshIn;
        private float _warConfirm;
        private float _noteLeft;

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
            ui.OnTap("Gift", () => { if (DiplomacyGiftWindow.Instance != null) DiplomacyGiftWindow.Instance.Show(); });
            ui.OnTap("Trade", () => { if (DiplomacyTradeWindow.Instance != null) DiplomacyTradeWindow.Instance.Show(); });
            ui.OnTap("TradeLocked", () => Note(Localization.Format("#diplomacy_trade_needs", Mathf.RoundToInt(BalanceConfig.Instance.BotTradeMinScore))));
            ui.OnTap("War", War);
            ui.OnTap("PeaceCoins", PayCoins);
            ui.OnTap("PeaceCrystals", PayCrystals);
        }

        /// <summary>The window (the hall is not needed: there is one neighbour).</summary>
        public void Show(BuildingInstance hall)
        {
            if (ui == null) return;
            Bind();
            _warConfirm = 0f;
            Note(string.Empty);
            ui.Open();
            Refresh();
        }

        public void Close()
        {
            if (ui != null) ui.Hide();
        }

        private void Note(string text)
        {
            if (ui == null) return;
            ui.SetText("Note", text);
            _noteLeft = string.IsNullOrEmpty(text) ? 0f : 4f;
        }

        private void War()
        {
            if (_warConfirm <= 0f)
            {
                _warConfirm = WarConfirmSeconds;
                Note(Localization.Get("#diplomacy_war_sure"));
                return;
            }
            _warConfirm = 0f;
            if (BotRelations.CanSwitch(BotStance.War, out var why)) BotRelations.SwitchTo(BotStance.War);
            else if (why != null) Note(Localization.Get(why));
            Refresh();
        }

        /// <summary>The war ends for the ransom in coins.</summary>
        public void PayCoins()
        {
            if (BotRelations.Stance != BotStance.War) return;
            var price = BotRelations.RansomCoins(BotRelations.HallLevel());
            var resources = CityBuilder.Resources.ResourceManager.Instance;
            var cost = new[] { new CityBuilder.Resources.ResourceAmount { type = CityBuilder.Resources.ResourceType.Coins, amount = price } };
            if (resources == null || !resources.TrySpend(cost))
            {
                Note(Localization.Get("#diplomacy_pay_short"));
                return;
            }
            BotRelations.EndWar();
            Note(Localization.Get("#diplomacy_paid"));
        }

        /// <summary>The war ends for the ransom in crystals (the upgrade crystals' purse).</summary>
        public void PayCrystals()
        {
            if (BotRelations.Stance != BotStance.War) return;
            var price = BotRelations.RansomCrystals(BotRelations.HallLevel());
            if (Wallet.UpgradeCrystals < price)
            {
                Note(Localization.Get("#diplomacy_pay_short"));
                return;
            }
            Wallet.UpgradeCrystals -= price;
            BotRelations.EndWar();
            Note(Localization.Get("#diplomacy_paid"));
        }

        private void Update()
        {
            if (!IsOpen) return;
            var dt = Time.unscaledDeltaTime;
            if (_warConfirm > 0f) _warConfirm -= dt;
            if (_noteLeft > 0f)
            {
                _noteLeft -= dt;
                if (_noteLeft <= 0f) ui.SetText("Note", string.Empty);
            }
            _refreshIn -= dt;
            if (_refreshIn > 0f) return;
            _refreshIn = RefreshSeconds;
            Refresh();
        }

        private void Refresh()
        {
            if (!IsOpen) return;
            var config = BalanceConfig.Instance;
            var score = BotRelations.Score;
            ui.SetText("Value", Mathf.RoundToInt(score).ToString("+0;-0;0"));
            var fall = config != null ? config.DiplomacyRelationsFallPerDay : 1f;
            ui.SetText("PerDay", Localization.Format("#diplomacy_per_day", (-fall).ToString("+0.#;-0.#;0")));
            PlaceNeedle(ui, score);
            ui.SetText("State", Localization.Format("#diplomacy_stance", Localization.Get(StanceKey(BotRelations.Stance))));

            var war = BotRelations.Stance == BotStance.War;
            ui.Show("AtPeace", !war);
            ui.Show("AtWar", war);
            if (!war)
            {
                var tradeOpen = DiplomacyRules.TradeOpen;
                ui.Show("Trade", tradeOpen);
                ui.Show("TradeLocked", !tradeOpen);
                ui.SetText("TradeSub", tradeOpen
                    ? Localization.Format("#diplomacy_trade_sub", config != null ? config.DiplomacyTradeDeals : 2)
                    : Localization.Format("#diplomacy_trade_needs", Mathf.RoundToInt(config != null ? config.BotTradeMinScore : 30f)));
                return;
            }
            var hallLevel = BotRelations.HallLevel();
            var clock = Mathf.FloorToInt(BotRelations.WarSeconds);
            ui.SetText("Wave", Localization.Format("#diplomacy_war_wave", BotRelations.WavesSent(), $"{clock / 60}:{clock % 60:00}"));
            ui.SetText("PeaceCoinsLabel", BotRelations.RansomCoins(hallLevel).ToString());
            ui.SetText("PeaceCrystalsLabel", BotRelations.RansomCrystals(hallLevel).ToString());
            var discount = config != null ? Mathf.RoundToInt(Mathf.Clamp01(config.BotWarContributionHallDiscount * Mathf.Max(0, hallLevel - 1)) * 100f) : 0;
            ui.SetText("PeaceCoinsSub", Localization.Format("#diplomacy_peace_coins_sub", discount));
        }

        /// <summary>The needle over the scale's span (-100 at the left end, 100 at the right).</summary>
        public static void PlaceNeedle(BakedUi baked, float score)
        {
            var span = baked.Get("ScaleSpan");
            var needle = baked.Get("Needle");
            if (span == null || needle == null) return;
            var s = (RectTransform)span.transform;
            var n = (RectTransform)needle.transform;
            var t = Mathf.InverseLerp(BotRelations.Min, BotRelations.Max, score);
            n.anchoredPosition = new Vector2(s.anchoredPosition.x + (t - 0.5f) * s.sizeDelta.x, n.anchoredPosition.y);
        }

        public static string StanceKey(BotStance stance) => stance switch
        {
            BotStance.Trade => "#diplomacy_trade",
            BotStance.War => "#diplomacy_war",
            _ => "#diplomacy_peace",
        };
    }
}
