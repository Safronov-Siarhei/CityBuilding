using CityBuilder.Buildings;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The hiring buttons beside the «!» at the bottom of the screen while a building is in focus
    /// (the user's sketch, 2026-10-25: hiring moved off the cards, «не забудь отцентровать иконки»
    /// -- the buttons and the «!» share one centred row; an Invasion run first, the sandbox too
    /// since the same day):
    ///
    /// - the Barracks: a face per soldier tier open in the run, its price in coins; a tap hires one;
    /// - the castle (the Town Hall): the King, the Queen, the Captain (HeroRoster): the hero there
    ///   is wears a tick, the others their price, «Замок ур. N» while the castle is too low; a tap
    ///   buys, or says why not.
    /// </summary>
    public class FocusHireBar : MonoBehaviour
    {
        public enum Kind { Barracks = 0, Castle = 1 }

        private const float RefreshSeconds = 0.25f;
        private static readonly Color Dimmed = new Color(0.45f, 0.45f, 0.45f, 1f);

        [SerializeField] private Kind kind;
        [SerializeField] private SoldierType[] types = new SoldierType[0];
        [SerializeField] private GameObject[] buttons = new GameObject[0];
        [SerializeField] private Image[] faces = new Image[0];
        [SerializeField] private GameObject[] prices = new GameObject[0];
        [SerializeField] private Text[] priceTexts = new Text[0];
        [SerializeField] private Image[] priceIcons = new Image[0];
        [SerializeField] private GameObject[] ticks = new GameObject[0];
        [SerializeField] private GameObject[] locks = new GameObject[0];
        [SerializeField] private Text[] lockTexts = new Text[0];
        [SerializeField] private ResourceIconLibrary iconLibrary;

        private float _refreshIn;
        private bool _shown;

        /// <summary>The building whose buttons these are, while it is in focus.</summary>
        private BuildingInstance Target
        {
            get
            {
                // beside the «!», and away with it while the building's card is up
                var focus = BuildingFocus.Instance != null && BuildingFocus.Instance.DetailsShown ? BuildingFocus.Instance.Current : null;
                if (focus == null || focus.Data == null) return null;
                if (kind == Kind.Castle) return focus == HeroRoster.Castle && HeroRoster.Instance != null ? focus : null;
                return focus.Data.buildingName == BuildingIds.Barracks && focus.IsOperational ? focus : null;
            }
        }

        /// <summary>A button tapped (a persistent listener wants no answer back).</summary>
        public void Tap(int index)
        {
            if (kind == Kind.Castle) Buy(index);
            else Hire(index);
        }

        /// <summary>One soldier of the button's tier from the Barracks in focus; says why not when it cannot.</summary>
        public bool Hire(int index)
        {
            var barracks = Target;
            var army = ArmyManager.Instance;
            if (barracks == null || army == null || index < 0 || index >= types.Length) return false;
            var blocker = army.DescribeRecruitBlocker(types[index], barracks.Level);
            var hired = blocker == null && army.TryRecruit(types[index], barracks.transform.position, barracks.Level);
            if (!hired && blocker != null)
            {
                EventLogManager.Instance?.Post(GameEvent.ForSettlement(blocker, EventSeverity.Warning, EventGlyph.Alert, null));
            }
            _refreshIn = 0f;
            return hired;
        }

        /// <summary>The hero of this button: bought if the castle and the purse allow; why not otherwise.</summary>
        public HeroRoster.Offer Buy(int index)
        {
            var roster = HeroRoster.Instance;
            if (roster == null || index < 0 || index >= types.Length) return HeroRoster.Offer.NoSuchHero;
            var type = types[index];
            var offer = roster.Buy(type);
            var balance = BalanceConfig.Instance.Hero(SoldierStats.SheetIdOf(type));
            var name = Localization.Get("#hero_" + SoldierStats.SheetIdOf(type));
            switch (offer)
            {
                case HeroRoster.Offer.CastleTooLow:
                    EventLogManager.Instance?.Post(GameEvent.ForSettlement(Localization.Format("#hero_need_castle", name, balance.castleLevel),
                        EventSeverity.Warning, EventGlyph.Alert, null));
                    break;
                case HeroRoster.Offer.NotEnoughMoney:
                    EventLogManager.Instance?.Post(GameEvent.ForSettlement(Localization.Format("#hero_no_coins", name, balance.price),
                        EventSeverity.Warning, EventGlyph.Alert, null));
                    break;
            }
            _refreshIn = 0f;
            return offer;
        }

        /// <summary>Whether this bar's buttons are up. For the tests.</summary>
        public bool Shown => _shown;

        private void Update()
        {
            var target = Target;
            var show = target != null;
            if (show != _shown)
            {
                _shown = show;
                _refreshIn = 0f;
                if (!show) foreach (var button in buttons) if (button != null) button.SetActive(false);
            }
            if (!show) return;
            _refreshIn -= Time.unscaledDeltaTime;
            if (_refreshIn > 0f) return;
            _refreshIn = RefreshSeconds;
            if (kind == Kind.Castle) ShowHeroes(target);
            else ShowSoldiers(target);
        }

        /// <summary>
        /// A face per tier the Laboratory has opened; one the Barracks is still too low for stands
        /// dimmed under «Казарма ур. N», its price hidden (the user, 2026-10-28: each level opens
        /// the next soldier).
        /// </summary>
        private void ShowSoldiers(BuildingInstance barracks)
        {
            var army = ArmyManager.Instance;
            for (var i = 0; i < types.Length; i++)
            {
                var open = SoldierStats.IsUnlocked(types[i]);
                buttons[i].SetActive(open);
                if (!open) continue;
                var need = SoldierStats.BarracksLevel(types[i]);
                var tooLow = barracks.Level < need;
                if (i < locks.Length && locks[i] != null) locks[i].SetActive(tooLow);
                if (tooLow && i < lockTexts.Length && lockTexts[i] != null) lockTexts[i].text = Localization.Format("#army_card_barracks", need);
                if (tooLow)
                {
                    prices[i].SetActive(false);
                    faces[i].color = Dimmed;
                    continue;
                }
                var cost = SoldierStats.RecruitCost(types[i]);
                var price = cost != null && cost.Count > 0 ? cost[0] : null;
                prices[i].SetActive(price != null);
                if (price != null)
                {
                    priceTexts[i].text = price.amount.ToString();
                    if (iconLibrary != null) priceIcons[i].sprite = iconLibrary.GetIcon(price.type);
                }
                faces[i].color = army != null && army.DescribeRecruitBlocker(types[i], barracks.Level) == null ? Color.white : Dimmed;
            }
        }

        private void ShowHeroes(BuildingInstance castle)
        {
            var roster = HeroRoster.Instance;
            for (var i = 0; i < types.Length; i++)
            {
                buttons[i].SetActive(true);
                var balance = BalanceConfig.Instance.Hero(SoldierStats.SheetIdOf(types[i]));
                var owned = roster.HeroOf(types[i]) != null;
                var tooLow = !owned && balance != null && castle.Level < balance.castleLevel;
                if (i < ticks.Length && ticks[i] != null) ticks[i].SetActive(owned);
                prices[i].SetActive(!owned && balance != null);
                if (!owned && balance != null)
                {
                    priceTexts[i].text = balance.price.ToString();
                    if (iconLibrary != null && priceIcons[i].sprite == null) priceIcons[i].sprite = iconLibrary.GetIcon(HeroRoster.Money);
                }
                if (i < locks.Length && locks[i] != null) locks[i].SetActive(tooLow);
                if (tooLow && i < lockTexts.Length) lockTexts[i].text = Localization.Format("#hero_card_castle", balance.castleLevel);
                faces[i].color = tooLow ? Dimmed : Color.white;
            }
        }
    }
}
