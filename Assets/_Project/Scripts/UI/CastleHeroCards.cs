using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Invasion;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The heroes on the castle's card in an Invasion run (the user, 2026-10-23): three cards --
    /// the King, the Queen, the Captain -- shown from the castle's first level. The one the run
    /// has wears a tick; the others show their price, and while the castle is too low «Замок ур. N»
    /// over a dimmed face. A tap buys; too early, it says the castle must be raised first.
    /// Sits in the band the Barracks' recruiting uses (BuildingInfoPanelController) -- the castle
    /// has neither workers nor recruits.
    /// </summary>
    public class CastleHeroCards : MonoBehaviour
    {
        private const float RefreshSeconds = 0.25f;

        [SerializeField] private BuildingInfoPanelController panel;
        [SerializeField] private GameObject row;
        [SerializeField] private SoldierType[] heroes;
        [SerializeField] private Image[] faces;
        [SerializeField] private GameObject[] ticks;
        [SerializeField] private GameObject[] prices;
        [SerializeField] private Text[] priceTexts;
        [SerializeField] private Image[] priceIcons;
        [SerializeField] private Text[] lockTexts;
        [SerializeField] private ResourceIconLibrary iconLibrary;

        private float _refreshIn;

        /// <summary>The card's button (a persistent listener wants no answer back).</summary>
        public void BuyCard(int index) => Buy(index);

        /// <summary>The card of this hero tapped: buy him if the castle and the purse allow; say why not otherwise.</summary>
        public InvasionMode.HeroOffer Buy(int index)
        {
            var mode = InvasionMode.Instance;
            if (mode == null || index < 0 || index >= heroes.Length) return InvasionMode.HeroOffer.NoSuchHero;
            var type = heroes[index];
            var offer = mode.BuyHero(type);
            var balance = BalanceConfig.Instance.Hero(SoldierStats.SheetIdOf(type));
            var name = Localization.Get("#hero_" + SoldierStats.SheetIdOf(type));
            switch (offer)
            {
                case InvasionMode.HeroOffer.CastleTooLow:
                    EventLogManager.Instance?.Post(GameEvent.ForSettlement(Localization.Format("#hero_need_castle", name, balance.castleLevel),
                        EventSeverity.Warning, EventGlyph.Alert, null));
                    break;
                case InvasionMode.HeroOffer.NotEnoughMoney:
                    EventLogManager.Instance?.Post(GameEvent.ForSettlement(Localization.Format("#hero_no_coins", name, balance.price),
                        EventSeverity.Warning, EventGlyph.Alert, null));
                    break;
            }
            _refreshIn = 0f;
            return offer;
        }

        private void Update()
        {
            var castle = InvasionMode.Instance != null ? InvasionMode.Instance.Castle : null;
            var show = InvasionMode.IsActive && castle != null && panel != null && panel.IsOpen && panel.CurrentInstance == castle;
            if (row != null && row.activeSelf != show) row.SetActive(show);
            if (!show) return;
            _refreshIn -= Time.unscaledDeltaTime;
            if (_refreshIn > 0f) return;
            _refreshIn = RefreshSeconds;

            var mode = InvasionMode.Instance;
            for (var i = 0; i < heroes.Length; i++)
            {
                var balance = BalanceConfig.Instance.Hero(SoldierStats.SheetIdOf(heroes[i]));
                var owned = mode.HeroOf(heroes[i]) != null;
                var tooLow = !owned && balance != null && castle.Level < balance.castleLevel;
                ticks[i].SetActive(owned);
                prices[i].SetActive(!owned);
                if (!owned && balance != null)
                {
                    priceTexts[i].text = balance.price.ToString();
                    if (iconLibrary != null && priceIcons[i].sprite == null) priceIcons[i].sprite = iconLibrary.GetIcon(InvasionRules.Money);
                }
                lockTexts[i].transform.parent.gameObject.SetActive(tooLow);
                if (tooLow) lockTexts[i].text = Localization.Format("#hero_card_castle", balance.castleLevel);
                faces[i].color = tooLow ? new Color(0.45f, 0.45f, 0.45f, 1f) : Color.white;
            }
        }
    }
}
