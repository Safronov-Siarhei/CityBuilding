using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Diplomacy;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The Diplomacy Hall's window (the user, 2026-11-03): the relations scale with the bot kingdom
    /// (-100..100), the stance, and the three stances to move to -- Мир, Торговля, Война -- each
    /// greyed with the reason when it cannot be taken (BotRelations.CanSwitch). «Здание» opens the
    /// Hall's ordinary card for its upgrade and repair. Trade deals come later.
    /// </summary>
    public class DiplomacyWindow : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text title;
        [SerializeField] private Image scaleFill;
        [SerializeField] private Text scaleText;
        [SerializeField] private Text stanceText;
        [SerializeField] private Button[] stanceButtons = new Button[3];
        [SerializeField] private Text note;
        // the ransom at war (the user, 2026-11-04): in coins, or in crystals for a town short of coins
        [SerializeField] private Button payCoinsButton;
        [SerializeField] private Button payCrystalsButton;
        [SerializeField] private BuildingInfoPanelController infoPanel;

        public static DiplomacyWindow Instance { get; private set; }
        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private BuildingInstance _hall;

        private void Awake() => Instance = this;

        private void OnEnable() => BotRelations.Changed += Refresh;

        private void OnDisable() => BotRelations.Changed -= Refresh;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(BuildingInstance hall)
        {
            if (panelRoot == null) return;
            _hall = hall;
            panelRoot.SetActive(true);
            if (note != null) note.text = string.Empty;
            Refresh();
        }

        public void Close()
        {
            _hall = null;
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        public void ChoosePeace() => Choose(BotStance.Peace);
        public void ChooseTrade() => Choose(BotStance.Trade);
        public void ChooseWar() => Choose(BotStance.War);

        /// <summary>«Здание»: the Hall's own card, for its upgrade and repair.</summary>
        /// <summary>The war ends for the ransom in coins.</summary>
        public void PayCoins()
        {
            if (BotRelations.Stance != BotStance.War) return;
            var price = BotRelations.RansomCoins(BotRelations.HallLevel());
            var resources = CityBuilder.Resources.ResourceManager.Instance;
            var cost = new[] { new CityBuilder.Resources.ResourceAmount { type = CityBuilder.Resources.ResourceType.Coins, amount = price } };
            if (resources == null || !resources.TrySpend(cost))
            {
                if (note != null) note.text = Localization.Get("#diplomacy_pay_short");
                return;
            }
            BotRelations.EndWar();
            if (note != null) note.text = Localization.Get("#diplomacy_paid");
        }

        /// <summary>The war ends for the ransom in crystals (the upgrade crystals' purse).</summary>
        public void PayCrystals()
        {
            if (BotRelations.Stance != BotStance.War) return;
            var price = BotRelations.RansomCrystals(BotRelations.HallLevel());
            if (Wallet.UpgradeCrystals < price)
            {
                if (note != null) note.text = Localization.Get("#diplomacy_pay_short");
                return;
            }
            Wallet.UpgradeCrystals -= price;
            BotRelations.EndWar();
            if (note != null) note.text = Localization.Get("#diplomacy_paid");
        }

        public void OpenBuildingCard()
        {
            var hall = _hall;
            Close();
            if (infoPanel != null && hall != null) infoPanel.Show(hall);
        }

        private void Choose(BotStance stance)
        {
            if (BotRelations.CanSwitch(stance, out var why))
            {
                BotRelations.SwitchTo(stance);
                if (note != null) note.text = stance == BotStance.Trade ? Localization.Get("#diplomacy_trade_later") : string.Empty;
                return;
            }
            if (note != null && why != null) note.text = Reason(why);
        }

        private static string Reason(string key) =>
            key == "#diplomacy_trade_needs" && BalanceConfig.Instance != null
                ? Localization.Format(key, Mathf.RoundToInt(BalanceConfig.Instance.BotTradeMinScore))
                : Localization.Get(key);

        private void Refresh()
        {
            if (!IsOpen) return;
            if (title != null) title.text = Localization.Get("#building_diplomacyhall");
            var score = BotRelations.Score;
            if (scaleFill != null)
            {
                scaleFill.fillAmount = Mathf.InverseLerp(BotRelations.Min, BotRelations.Max, score);
                scaleFill.color = Color.Lerp(new Color(0.8f, 0.2f, 0.15f), new Color(0.25f, 0.65f, 0.25f), scaleFill.fillAmount);
            }
            if (scaleText != null) scaleText.text = Localization.Format("#diplomacy_relations", Mathf.RoundToInt(score).ToString("+0;-0;0"));
            if (stanceText != null) stanceText.text = Localization.Format("#diplomacy_stance", Localization.Get(StanceKey(BotRelations.Stance)));
            var war = BotRelations.Stance == BotStance.War;
            var hallLevel = war ? BotRelations.HallLevel() : 1;
            if (payCoinsButton != null)
            {
                payCoinsButton.gameObject.SetActive(war);
                var label = payCoinsButton.GetComponentInChildren<Text>();
                if (war && label != null) label.text = Localization.Format("#diplomacy_pay_coins", BotRelations.RansomCoins(hallLevel));
            }
            if (payCrystalsButton != null)
            {
                payCrystalsButton.gameObject.SetActive(war);
                var label = payCrystalsButton.GetComponentInChildren<Text>();
                if (war && label != null) label.text = Localization.Format("#diplomacy_pay_crystals", BotRelations.RansomCrystals(hallLevel));
            }
            for (var i = 0; i < stanceButtons.Length; i++)
            {
                var button = stanceButtons[i];
                if (button == null) continue;
                var stance = (BotStance)i;
                var current = stance == BotRelations.Stance;
                var image = button.GetComponent<Image>();
                if (image != null) image.color = current ? new Color(1f, 0.85f, 0.45f) : BotRelations.CanSwitch(stance, out _) ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            }
        }

        public static string StanceKey(BotStance stance) => stance switch
        {
            BotStance.Trade => "#diplomacy_trade",
            BotStance.War => "#diplomacy_war",
            _ => "#diplomacy_peace",
        };
    }
}
