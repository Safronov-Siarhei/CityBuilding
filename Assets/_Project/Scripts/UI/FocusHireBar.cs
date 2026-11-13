using CityBuilder.Buildings;
using CityBuilder.Citizens;
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
        // the Shipyard (the user, 2026-10-28): a ship per button, the shipyard's level opens them
        // the dungeon's hire camp (the user, 2026-11-01): a gnome per button, held like the Barracks
        // the Stable and the Church (the user, 2026-11-05): the knight, the monk -- hired like the Barracks' soldiers
        // the Smelter (the user, 2026-11-12): a bar per button -- copper, iron, gold; a tap picks what is smelted
        // the sandbox Лечебница (the user, 2026-11-13): six beds -- a plus when free, the patient's face
        // (in a ring while he is treated), a lock with «Ур. N» for one a higher level opens
        public enum Kind { Barracks = 0, Castle = 1, Shipyard = 2, HireCamp = 3, Stable = 4, Church = 5, Workshop = 6, Smelter = 7, Healer = 8 }

        /// <summary>The Smelter's buttons, in order: the bar each one smelts.</summary>
        public static readonly ResourceType[] SmeltedBars = { ResourceType.CopperBar, ResourceType.IronBar, ResourceType.GoldBar };

        private const float RefreshSeconds = 0.25f;
        private static readonly Color Dimmed = new Color(0.45f, 0.45f, 0.45f, 1f);

        [SerializeField] private Kind kind;

        /// <summary>Whose buttons these are.</summary>
        public Kind BarKind => kind;
        [SerializeField] private SoldierType[] types = new SoldierType[0];
        [SerializeField] private GameObject[] buttons = new GameObject[0];
        [SerializeField] private Image[] faces = new Image[0];
        [SerializeField] private GameObject[] prices = new GameObject[0];
        [SerializeField] private Text[] priceTexts = new Text[0];
        [SerializeField] private Image[] priceIcons = new Image[0];
        [SerializeField] private GameObject[] ticks = new GameObject[0];
        [SerializeField] private GameObject[] picked = new GameObject[0];
        [SerializeField] private GameObject[] locks = new GameObject[0];
        [SerializeField] private Text[] lockTexts = new Text[0];
        [SerializeField] private ResourceIconLibrary iconLibrary;
        [SerializeField] private Image[] rings = new Image[0];
        [SerializeField] private Sprite freeBedSprite;
        [SerializeField] private Sprite lockedBedSprite;
        private readonly System.Collections.Generic.List<Citizens.Infirmary.Patient> _beds = new System.Collections.Generic.List<Citizens.Infirmary.Patient>();

        private float _refreshIn;
        private bool _shown;

        /// <summary>The building whose buttons these are, while it is in focus.</summary>
        private BuildingInstance Target
        {
            get
            {
                // beside the «!», and away with it while the building's card is up
                var focus = BuildingFocus.Instance != null && BuildingFocus.Instance.ButtonsShown ? BuildingFocus.Instance.Current : null;
                if (focus == null || focus.Data == null) return null;
                if (kind == Kind.Castle) return focus == HeroRoster.Castle && HeroRoster.Instance != null ? focus : null;
                if (kind == Kind.Shipyard) return focus.Data.buildingName == Ships.ShipManager.ShipyardId && focus.IsOperational ? focus : null;
                if (kind == Kind.Smelter) return focus.Data.buildingName == BuildingIds.Smelter && !focus.IsBot ? focus : null;
                if (kind == Kind.Healer) return Citizens.Infirmary.Active && Citizens.Infirmary.IsHealer(focus) ? focus : null;
                var hall = kind == Kind.Stable ? BuildingIds.Stable : kind == Kind.Church ? BuildingIds.Church
                    : kind == Kind.Workshop ? BuildingIds.Workshop : BuildingIds.Barracks;
                return focus.Data.buildingName == hall && focus.IsOperational ? focus : null;
            }
        }

        /// <summary>The dungeon's hire camp held (it has no «!» -- its buttons are its card).</summary>
        private static bool CampHeld
        {
            get
            {
                var focus = BuildingFocus.Instance;
                return focus != null && focus.Entrance != null && focus.Entrance.Kind == Dungeon.DungeonEntrance.Place.HireCamp;
            }
        }

        /// <summary>One gnome of the button's kind from the hire camp; says why not when it cannot.</summary>
        public SoldierUnit HireGnome(int index)
        {
            var party = Dungeon.DungeonHirelings.Instance;
            if (!CampHeld || party == null || index < 0 || index >= types.Length) return null;
            var hired = party.Hire(types[index]);
            if (hired == null)
            {
                var why = party.Count >= Dungeon.DungeonHirelings.Max
                    ? Localization.Format("#dungeon_hire_full", Dungeon.DungeonHirelings.Max)
                    : Localization.Format("#dungeon_hire_nocoins", Dungeon.DungeonHirelings.Price(types[index]));
                EventLogManager.Instance?.Post(GameEvent.Nowhere(why, EventSeverity.Warning, EventGlyph.Alert, null));
            }
            _refreshIn = 0f;
            return hired;
        }

        /// <summary>A face per gnome with his price; dimmed while the party is full or the purse short.</summary>
        private void ShowGnomes()
        {
            var party = Dungeon.DungeonHirelings.Instance;
            var coin = iconLibrary != null ? iconLibrary.GetIcon(ResourceType.Coins) : null;
            for (var i = 0; i < buttons.Length && i < types.Length; i++)
            {
                buttons[i].SetActive(true);
                if (i < locks.Length && locks[i] != null) locks[i].SetActive(false);
                prices[i].SetActive(true);
                priceTexts[i].text = Dungeon.DungeonHirelings.Price(types[i]).ToString();
                if (priceIcons[i] != null) priceIcons[i].sprite = coin;
                faces[i].color = party != null && party.CanHire(types[i]) ? Color.white : Dimmed;
            }
        }

        /// <summary>A button tapped (a persistent listener wants no answer back).</summary>
        public void Tap(int index)
        {
            if (kind == Kind.HireCamp) HireGnome(index);
            else if (kind == Kind.Castle) Buy(index);
            else if (kind == Kind.Shipyard) BuildShip(index);
            else if (kind == Kind.Smelter) Smelt(index);
            else if (kind == Kind.Healer) return;   // the beds only show
            else Hire(index);
        }

        /// <summary>One soldier of the button's tier from the Barracks in focus; says why not when it cannot.</summary>
        public bool Hire(int index)
        {
            var barracks = Target;
            var army = ArmyManager.Instance;
            if (barracks == null || army == null || index < 0 || index >= types.Length) return false;
            if (Tutorial.TutorialMode.IsActive && army.CountOf(types[index]) > 0) return false;
            var blocker = army.DescribeRecruitBlocker(types[index], barracks.Level);
            var hired = blocker == null && army.TryRecruit(types[index], barracks.transform.position, barracks.Level);
            if (!hired && blocker != null)
            {
                EventLogManager.Instance?.Post(GameEvent.ForSettlement(blocker, EventSeverity.Warning, EventGlyph.Alert, null));
                // the Barracks too low for this tier: to its «Улучшить» (the user, 2026-11-11)
                if (barracks.Level < SoldierStats.BarracksLevel(types[index]) && BuildHints.Allowed) BuildHints.LeadToUpgrade(barracks);
            }
            _refreshIn = 0f;
            return hired;
        }

        /// <summary>The ship of this button from the Shipyard in focus; says why not when it cannot.</summary>
        public Ships.ShipUnit BuildShip(int index)
        {
            var shipyard = Target;
            var ships = Ships.ShipManager.Instance;
            if (shipyard == null || ships == null || index < 0 || index >= Ships.ShipStats.All.Length) return null;
            var shipKind = Ships.ShipStats.All[index];
            var blocker = ships.DescribeBuildBlocker(shipKind, shipyard);
            var built = blocker == null ? ships.TryBuild(shipKind, shipyard) : null;
            if (built == null && blocker != null)
            {
                EventLogManager.Instance?.Post(GameEvent.ForSettlement(blocker, EventSeverity.Warning, EventGlyph.Alert, null));
                if (shipyard.Level < Ships.ShipStats.ShipyardLevel(shipKind) && BuildHints.Allowed) BuildHints.LeadToUpgrade(shipyard);
            }
            _refreshIn = 0f;
            return built;
        }

        /// <summary>A face per ship; one the Shipyard is too low for dimmed under «Верфь ур. N».</summary>
        private void ShowShips(BuildingInstance shipyard)
        {
            var coin = iconLibrary != null ? iconLibrary.GetIcon(ResourceType.Coins) : null;
            for (var i = 0; i < buttons.Length && i < Ships.ShipStats.All.Length; i++)
            {
                var shipKind = Ships.ShipStats.All[i];
                // the tutorial: its one ship
                var offered = !Tutorial.TutorialMode.IsActive || shipKind == Tutorial.TutorialRules.Ship;
                buttons[i].SetActive(offered);
                if (!offered) continue;
                var need = Ships.ShipStats.ShipyardLevel(shipKind);
                var tooLow = shipyard.Level < need;
                if (i < locks.Length && locks[i] != null) locks[i].SetActive(tooLow);
                if (tooLow && i < lockTexts.Length && lockTexts[i] != null) lockTexts[i].text = Localization.Format("#ship_card_shipyard", need);
                prices[i].SetActive(!tooLow);
                priceTexts[i].text = Ships.ShipStats.PriceCoins(shipKind).ToString();
                if (priceIcons[i] != null) priceIcons[i].sprite = coin;
                TutorialPrice(i);   // the tutorial: the coin alone (the user, 2026-11-06)
                faces[i].color = !tooLow && Ships.ShipManager.Instance != null && Ships.ShipManager.Instance.DescribeBuildBlocker(shipKind, shipyard) == null ? Color.white : Dimmed;
            }
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
                    if (BuildHints.Allowed) BuildHints.LeadToUpgrade(Target ?? BuildingInstance.TownHall);
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

        /// <summary>The tutorial shows no price, only the coin, centred on its plaque (the user, 2026-11-06).</summary>
        private void TutorialPrice(int i)
        {
            if (!Tutorial.TutorialMode.IsActive) return;
            if (i < priceTexts.Length && priceTexts[i] != null && priceTexts[i].gameObject.activeSelf) priceTexts[i].gameObject.SetActive(false);
            if (i < priceIcons.Length && priceIcons[i] != null) priceIcons[i].rectTransform.anchoredPosition = new Vector2(0f, 1f);
        }

        /// <summary>The first button showing (the tutorial's arrow points at it), or null.</summary>
        public RectTransform FirstShownButton
        {
            get
            {
                if (!_shown) return null;
                foreach (var button in buttons)
                    if (button != null && button.activeInHierarchy) return (RectTransform)button.transform;
                return null;
            }
        }

        private void Update()
        {
            var target = kind == Kind.HireCamp ? null : Target;
            var show = kind == Kind.HireCamp ? CampHeld : target != null;
            // the tutorial's Town Hall sells no heroes: the King comes with it (the user: nothing extra)
            if (kind == Kind.Castle && Tutorial.TutorialMode.IsActive) show = false;
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
            if (kind == Kind.HireCamp) ShowGnomes();
            else if (kind == Kind.Castle) ShowHeroes(target);
            else if (kind == Kind.Shipyard) ShowShips(target);
            else if (kind == Kind.Smelter) ShowMetals(target);
            else if (kind == Kind.Healer) ShowBeds(target);
            else ShowSoldiers(target);
        }

        /// <summary>The Smelter's recipe that makes this button's bar, or -1.</summary>
        private static int RecipeFor(ProductionBuilding smelter, int index)
        {
            if (smelter == null || index < 0 || index >= SmeltedBars.Length) return -1;
            var recipes = smelter.Recipes;
            for (var i = 0; i < recipes.Count; i++)
            {
                if (recipes[i] != null && recipes[i].output == SmeltedBars[index]) return i;
            }
            return -1;
        }

        /// <summary>The Smelter in focus now smelts this button's bar (the user, 2026-11-12).</summary>
        public bool Smelt(int index)
        {
            var smelter = Target != null ? Target.GetComponent<ProductionBuilding>() : null;
            var recipe = RecipeFor(smelter, index);
            if (recipe < 0) return false;
            smelter.SelectRecipe(recipe);
            _refreshIn = 0f;
            return true;
        }

        /// <summary>The Лечебница's six beds: free (a plus), taken (the patient's face, ringed while treated), locked («Ур. N»).</summary>
        private void ShowBeds(BuildingInstance healer)
        {
            var infirmary = Citizens.Infirmary.Instance;
            if (infirmary == null) return;
            var beds = Citizens.Infirmary.BedsOf(healer);
            infirmary.InBed(healer, _beds);
            for (var i = 0; i < buttons.Length; i++)
            {
                buttons[i].SetActive(true);
                if (i < prices.Length && prices[i] != null) prices[i].SetActive(false);
                if (i < ticks.Length && ticks[i] != null) ticks[i].SetActive(false);
                var ring = i < rings.Length ? rings[i] : null;
                var locked = i >= beds;
                if (i < locks.Length && locks[i] != null) locks[i].SetActive(locked);
                if (locked)
                {
                    var need = healer.Level + 1;
                    while (need < BuildingInstance.MaxLevel && healer.Data.LevelStats(need).beds <= i) need++;
                    if (i < lockTexts.Length && lockTexts[i] != null) lockTexts[i].text = Localization.Format("#healer_bed_level", need);
                    faces[i].sprite = lockedBedSprite;
                    faces[i].color = Dimmed;
                    if (ring != null) ring.gameObject.SetActive(false);
                    continue;
                }
                faces[i].color = Color.white;
                var patient = i < _beds.Count ? _beds[i] : null;
                faces[i].sprite = patient != null && patient.Figure != null && patient.Figure.Portrait != null ? patient.Figure.Portrait : freeBedSprite;
                var progress = patient != null ? infirmary.Progress(patient) : -1f;
                if (ring != null)
                {
                    ring.gameObject.SetActive(progress >= 0f);
                    ring.fillAmount = Mathf.Max(0f, progress);
                }
            }
        }

        /// <summary>A bar per button, no price; the one smelting now on the gold button with a tick.</summary>
        private void ShowMetals(BuildingInstance target)
        {
            var smelter = target != null ? target.GetComponent<ProductionBuilding>() : null;
            for (var i = 0; i < buttons.Length; i++)
            {
                var recipe = RecipeFor(smelter, i);
                buttons[i].SetActive(recipe >= 0);
                if (recipe < 0) continue;
                var on = smelter.SelectedRecipeIndex == recipe;
                if (i < prices.Length && prices[i] != null) prices[i].SetActive(false);
                if (i < locks.Length && locks[i] != null) locks[i].SetActive(false);
                if (i < ticks.Length && ticks[i] != null) ticks[i].SetActive(on);
                if (i < picked.Length && picked[i] != null) picked[i].SetActive(on);
                faces[i].color = Color.white;
            }
        }

        /// <summary>
        /// A face per tier the Laboratory has opened; one the Barracks is still too low for stands
        /// dimmed under «Казарма ур. N», its price hidden (the user, 2026-10-28: each level opens
        /// the next soldier).
        /// </summary>
        private void ShowSoldiers(BuildingInstance barracks)
        {
            // the hire row up: the moment for «Вербовка» (the user, 2026-11-12; the hints keep their own pace)
            Shop.BoosterHints.Offer(Shop.Boosters.Recruit);
            var army = ArmyManager.Instance;
            for (var i = 0; i < types.Length; i++)
            {
                var open = SoldierStats.IsUnlocked(types[i]);
                // the tutorial hires its one soldier, again only if he falls (the user, 2026-11-06: hired the town away)
                if (open && Tutorial.TutorialMode.IsActive && army != null && army.CountOf(types[i]) > 0) open = false;
                buttons[i].SetActive(open);
                if (!open) continue;
                var need = SoldierStats.BarracksLevel(types[i]);
                var tooLow = barracks.Level < need;
                if (i < locks.Length && locks[i] != null) locks[i].SetActive(tooLow);
                if (tooLow && i < lockTexts.Length && lockTexts[i] != null)
                    lockTexts[i].text = kind == Kind.Barracks ? Localization.Format("#army_card_barracks", need)
                        : Localization.Format("#army_card_building", Localization.Get("#building_" + SoldierStats.HiredAt(types[i]).ToLowerInvariant()), need);
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
                    TutorialPrice(i);
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
