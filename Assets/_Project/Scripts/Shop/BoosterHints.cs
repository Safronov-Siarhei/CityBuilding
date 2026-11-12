using System;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Research;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.Shop
{
    /// <summary>
    /// A booster offered at the right moment (the user, 2026-11-12; card monetization.md, concept
    /// Design/store/Booster_Hints.png): a banner at the foot of the screen -- the booster, what it does,
    /// «Включить» when it is in stock (case A), or «▶ Смотреть» when it is not but lies in the shop now
    /// (case B: the ad, then it is switched on at once and its shop place recharges).
    ///
    /// The moments: Боевой дух -- raiders landed by the Town Hall; Стойкость -- raiders coming; Ремонт --
    /// damaged buildings after the fight; Вербовка -- the Barracks' hire row opened (FocusHireBar calls
    /// Offer); Зодчий / Учёный -- a long build or research started; Новосёлы -- many people left with the
    /// houses having room, or many free places; Казна -- few coins; Лесоруб / Каменотёс / Изобилие -- that
    /// resource nearly out while its working extraction has enough hands.
    ///
    /// How often (agreed, every number in the sheet's boosters tab): none in the first minutes, one on the
    /// screen, the same one once per hint_repeat_sec, it fades by itself (a bar on its foot) or is swiped,
    /// ignored hint_ignore_limit times in a row -> quiet till the next game; case B at most once per
    /// hint_shop_offer_sec. The checks run twice a second, not every frame.
    /// </summary>
    public class BoosterHints : MonoBehaviour
    {
        private const float CheckSeconds = 2f;
        private const float SlideSeconds = 0.3f;

        [SerializeField] private RectTransform banner;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image icon;
        [SerializeField] private Text title;
        [SerializeField] private Text line;
        [SerializeField] private Button use;
        [SerializeField] private Text useLabel;
        [SerializeField] private GameObject play;
        [SerializeField] private Button close;
        [SerializeField] private RectTransform bar;

        public static BoosterHints Instance { get; private set; }

        /// <summary>The booster on the banner now, or null (for the tests).</summary>
        public string Showing => _id;

        private readonly Dictionary<string, float> _lastShown = new Dictionary<string, float>();
        private readonly Dictionary<string, int> _ignored = new Dictionary<string, int>();
        private readonly HashSet<ConstructionSite> _sitesSeen = new HashSet<ConstructionSite>();
        private readonly HashSet<string> _researchSeen = new HashSet<string>();
        private float _play;
        private float _lastShopOffer = float.NegativeInfinity;
        private float _checkIn = CheckSeconds;
        private string _id;
        private bool _fromShop;
        private float _left;
        private float _shownFor;
        private int _peakPeople;
        private bool _wasRaid;
        private Vector2 _home;
        private Vector2 _dragFrom;

        private static BalanceConfig Config => BalanceConfig.Sandbox;

        private void Awake()
        {
            Instance = this;
            _home = banner.anchoredPosition;
            use.onClick.AddListener(Take);
            if (close != null) close.onClick.AddListener(() => Hide(true));
            banner.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>A moment the game itself knows of (the hire row opened): offer this booster if the rules allow.</summary>
        public static void Offer(string id)
        {
            if (Instance != null) Instance.TryShow(id);
        }

        private void Update()
        {
            if (!Boosters.Here)
            {
                if (_id != null) Hide(false);
                return;
            }
            _play += Time.deltaTime;
            if (_id != null) Animate();
            _checkIn -= Time.unscaledDeltaTime;
            if (_checkIn > 0f) return;
            _checkIn = CheckSeconds;
            Check();
        }

        // ---------------------------------------------------------------- the moments

        private void Check()
        {
            var config = Config;
            if (config == null) return;
            var resources = ResourceManager.Instance;
            var people = CitizenManager.Instance;
            var hall = BuildingInstance.TownHall;

            // the raiders: coming (anywhere), landed (by the Town Hall)
            var raid = false;
            var landed = false;
            var near = config.HintRaidNearCells * CellSize();
            foreach (var unit in OrcUnit.All)
            {
                if (unit == null || !unit.IsRaiding) continue;
                raid = true;
                if (hall != null && (unit.transform.position - hall.transform.position).sqrMagnitude <= near * near) landed = true;
            }
            if (landed) TryShow(Boosters.BattleSpirit);
            if (raid) TryShow(Boosters.Fortitude);
            // the fight over: the damage it left
            if (!raid)
            {
                var damaged = 0;
                foreach (var building in BuildingInstance.All)
                    if (building != null && !building.IsBot && building.IsOperational && building.CurrentHealth < building.MaxHealth) damaged++;
                if (damaged >= config.HintRepairDamaged) TryShow(Boosters.Repair);
            }
            _wasRaid = raid;

            // a long build or upgrade started
            foreach (var building in BuildingInstance.All)
            {
                if (building == null || building.IsBot || building.Site == null) continue;
                if (!_sitesSeen.Add(building.Site)) continue;
                if (building.Site.Total >= config.HintLongBuildSeconds) TryShow(Boosters.Architect);
            }
            _sitesSeen.RemoveWhere(site => site == null);
            // a long research started
            var lab = ResearchManager.Instance;
            if (lab != null)
                foreach (var run in lab.UnderWay)
                {
                    if (run?.Topic == null || !_researchSeen.Add(run.Topic.Id)) continue;
                    if (ResearchManager.TotalSeconds(run.Topic) >= config.HintLongResearchSeconds) TryShow(Boosters.Scholar);
                }

            // the people: many left with room for them, or many free places
            if (people != null)
            {
                var now = people.TotalPopulation;
                _peakPeople = Mathf.Max(_peakPeople, now);
                var free = people.Capacity - now;
                if ((_peakPeople - now >= config.HintSettlersLeft && free >= config.HintSettlersLeft) || free >= config.HintSettlersFree)
                {
                    if (TryShow(Boosters.Settlers)) _peakPeople = now;
                }
            }

            if (resources == null) return;
            if (resources.GetAmount(ResourceType.Coins) < config.HintCoinsBelow) TryShow(Boosters.Treasury);
            CheckResource(resources, ResourceType.Wood, Boosters.Lumberjack, config);
            CheckResource(resources, ResourceType.Stone, Boosters.Stonecutter, config);
            CheckResource(resources, ResourceType.Food, Boosters.Abundance, config);
        }

        /// <summary>The resource nearly out, while its working extraction buildings have enough hands between them.</summary>
        private void CheckResource(ResourceManager resources, ResourceType type, string booster, BalanceConfig config)
        {
            if (resources.GetAmount(type) >= config.HintResourceBelow) return;
            var hands = 0;
            foreach (var building in ProductionBuilding.All)
            {
                if (building == null || building.Instance == null || !building.Instance.IsOperational || building.Instance.IsBot) continue;
                var makes = type == ResourceType.Food ? FoodOutput.IsProducer(building.BuildingId) : building.ProducesAnything && building.ProducesResource == type;
                if (makes) hands += building.AssignedWorkers;
            }
            if (hands >= config.HintWorkersMin) TryShow(booster);
        }

        private static float CellSize() => Grid.GridManager.Instance != null ? Grid.GridManager.Instance.CellSize : 1f;

        // ---------------------------------------------------------------- the banner

        /// <summary>Shows the hint if every rule allows it; true when it went up.</summary>
        private bool TryShow(string id)
        {
            var config = Config;
            var def = Boosters.Def(id);
            if (config == null || def == null || _id != null || !Boosters.Here) return false;
            if (_play < config.HintGraceSeconds || OfferShopWindow.IsOpen) return false;
            if (def.durationSeconds > 0f && Boosters.Left(id) > 0f) return false;
            if (_lastShown.TryGetValue(id, out var last) && _play - last < config.HintRepeatSeconds) return false;
            if (_ignored.TryGetValue(id, out var ignored) && ignored >= config.HintIgnoreLimit) return false;
            var fromShop = false;
            if (Boosters.Stock(id) <= 0)
            {
                // case B: in the shop right now, an ad to show, and not too soon after the last such offer
                var shop = BoosterShop.Instance;
                if (shop == null || shop.SlotOf(id) < 0 || !def.ad || !RewardedAds.IsReady || shop.AdPlayingSlot >= 0) return false;
                if (_play - _lastShopOffer < config.HintShopOfferSeconds) return false;
                fromShop = true;
                _lastShopOffer = _play;
            }
            _lastShown[id] = _play;
            _id = id;
            _fromShop = fromShop;
            _left = config.HintFadeSeconds;
            _shownFor = 0f;
            icon.sprite = OfferShopWindow.LoadBoosterIcon(id);
            title.text = Localization.Get(def.nameKey);
            line.text = Boosters.Describe(def) + (def.durationSeconds > 0f ? " · " + Boosters.Duration(def) : string.Empty);
            useLabel.text = Localization.Get(fromShop ? "#shop_watch" : "#booster_on");
            if (play != null) play.SetActive(fromShop);
            use.interactable = true;
            banner.anchoredPosition = _home;
            banner.gameObject.SetActive(true);
            return true;
        }

        private void Take()
        {
            if (_id == null) return;
            var id = _id;
            if (_fromShop)
            {
                var shop = BoosterShop.Instance;
                var slot = shop != null ? shop.SlotOf(id) : -1;
                if (slot >= 0) shop.Buy(slot, OfferShop.Pay.Ad, useAtOnce: true);
            }
            else Boosters.Use(id);
            _ignored[id] = 0;
            Hide(false);
        }

        /// <summary>Takes the banner down; `ignored` counts towards falling quiet.</summary>
        private void Hide(bool ignored)
        {
            if (_id != null && ignored)
                _ignored[_id] = (_ignored.TryGetValue(_id, out var n) ? n : 0) + 1;
            _id = null;
            banner.gameObject.SetActive(false);
        }

        /// <summary>Slides in, the bar on its foot runs down, a swipe sideways or down throws it away.</summary>
        private void Animate()
        {
            var dt = Time.unscaledDeltaTime;
            _shownFor += dt;
            _left -= dt;
            var fade = Config != null ? Mathf.Max(0.1f, Config.HintFadeSeconds) : 8f;
            if (bar != null) bar.localScale = new Vector3(Mathf.Clamp01(_left / fade), 1f, 1f);
            var k = Mathf.Clamp01(_shownFor / SlideSeconds);
            var outK = Mathf.Clamp01(_left / SlideSeconds);
            group.alpha = Mathf.Min(k, outK);
            banner.anchoredPosition = _home + new Vector2(0f, -60f * (1f - k));
            var pointer = UnityEngine.InputSystem.Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame) _dragFrom = pointer.position.ReadValue();
            if (pointer != null && pointer.press.wasReleasedThisFrame && RectTransformUtility.RectangleContainsScreenPoint(banner, _dragFrom, null))
            {
                var d = pointer.position.ReadValue() - _dragFrom;
                if (d.magnitude > Screen.height * 0.06f && (Mathf.Abs(d.x) > Mathf.Abs(d.y) || d.y < 0f))
                {
                    Hide(true);
                    return;
                }
            }
            if (_left <= 0f) Hide(true);
        }
    }
}
