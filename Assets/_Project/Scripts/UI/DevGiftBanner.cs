using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// «Подарок от разработчиков» (the user, 2026-11-19; art Tools/ui/dev_gift_concept.py, mock-up
    /// Design/ui/DevGift_Banner.png approved; card Design/features/monetization.md): a sandbox town that is STUCK gets a
    /// free set to climb out. Checked every devgift_check_sec of play; stuck = one of these held for devgift_hold_sec:
    ///   1. no store of the player's standing;
    ///   2. no food at all and no food building;
    ///   3. wood or stone under devgift_low_res and neither has grown.
    /// The set: devgift_wood/stone/food/coins/crystals, plus the cheapest store's price when no store stands. Goods past
    /// the stores' ceiling lie in the Town Hall (AddOverCapacity). At most devgift_max_per_world in a world, the next no
    /// sooner than devgift_gap_sec of play after the last (both saved per world). Waits for a free screen; pauses the
    /// game and hides the HUD while it shows.
    /// </summary>
    public class DevGiftBanner : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private RectTransform plaque;
        [SerializeField] private Text plaqueLabel;
        [SerializeField] private ResourceIconLibrary iconLibrary;
        [SerializeField] private Sprite crystalIcon;

        public static DevGiftBanner Instance { get; private set; }

        /// <summary>Gifts given in this world (saved).</summary>
        public static int Count;

        /// <summary>Play seconds until the next gift may come in this world (saved).</summary>
        public static float Wait;

        private const string Crystals = "Crystals";
        private const int PerRow = 3;   // at most three to a row (the user, 2026-11-19)
        private const float LabelHeight = 44f, RowHeight = 54f, Icon = 42f, NumberSize = 34f, GapIn = 8f, GapOut = 32f, Pad = 32f;

        private readonly List<(string what, int amount)> _gift = new List<(string, int)>();
        private readonly List<GameObject> _items = new List<GameObject>();
        private BuildingPlacer _placer;
        private float _timeScale = 1f;
        private float _nextCheck;
        private float _stuck;
        private int _wood = -1, _stone = -1;
        private float _plaqueTop = float.NaN;

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            Instance = this;
            if (root != null) root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private static bool Eligible
        {
            get
            {
                var config = BalanceConfig.Instance;
                return config != null && PiggyTasks.InSandbox && !Dungeon.DungeonMode.IsActive && Shop.OfferShop.Allowed
                       && Count < config.DevGiftMaxPerWorld && !(GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver);
            }
        }

        private void Update()
        {
            if (IsOpen || !Eligible || Time.timeScale <= 0f) return;
            var config = BalanceConfig.Instance;
            var dt = Time.unscaledDeltaTime;
            if (Wait > 0f) Wait = Mathf.Max(0f, Wait - dt);
            if (Time.unscaledTime < _nextCheck) return;
            var step = Mathf.Max(1f, config.DevGiftCheckSeconds);
            _nextCheck = Time.unscaledTime + step;

            if (_placer == null) _placer = FindAnyObjectByType<BuildingPlacer>();
            // the start chain still being placed is not «stuck»
            if (BuildingInstance.TownHall == null || _placer == null || _placer.IsPlacingMandatoryBuilding)
            {
                _stuck = 0f;
                return;
            }
            _stuck = Stuck(config) ? _stuck + step : 0f;
            if (_stuck < config.DevGiftHoldSeconds || Wait > 0f) return;
            // a free screen only: nothing in hand, no window
            if (ModalGate.IsBlocked || _placer.IsSelecting) return;
            Show();
        }

        private bool Stuck(BalanceConfig config)
        {
            if (NoStore()) return true;
            var resources = ResourceManager.Instance;
            if (resources == null) return false;
            if (resources.GetAmount(ResourceType.Food) <= 0 && !HasFoodBuilding()) return true;
            var wood = resources.GetAmount(ResourceType.Wood);
            var stone = resources.GetAmount(ResourceType.Stone);
            var grew = (_wood >= 0 && wood > _wood) || (_stone >= 0 && stone > _stone);
            _wood = wood;
            _stone = stone;
            var low = config.DevGiftLowResource;
            return !grew && (wood < low || stone < low);
        }

        private static bool NoStore()
        {
            foreach (var s in StoreLedger.All)
                if (s != null && !s.IsBot) return false;
            return true;
        }

        private static bool HasFoodBuilding()
        {
            foreach (var b in BuildingInstance.All)
                if (b != null && !b.IsBot && b.Data != null && FoodOutput.IsProducer(b.Data.buildingName)) return true;
            return false;
        }

        /// <summary>The cheapest store the player can build (by its whole price), or null.</summary>
        private BuildingData CheapestStore()
        {
            if (_placer == null) _placer = FindAnyObjectByType<BuildingPlacer>();
            if (_placer == null) return null;
            BuildingData best = null;
            var bestSum = int.MaxValue;
            foreach (var data in _placer.AvailableBuildings)
            {
                if (data == null || !StoreLedger.IsStore(data)) continue;
                var sum = 0;
                foreach (var a in BuildingPlacer.CostOf(data)) sum += a.amount;
                if (sum < bestSum) { bestSum = sum; best = data; }
            }
            return best;
        }

        private void MakeGift()
        {
            var config = BalanceConfig.Instance;
            var sums = new Dictionary<string, int>();
            void Add(string what, int n) { if (n > 0) sums[what] = (sums.TryGetValue(what, out var v) ? v : 0) + n; }
            Add(nameof(ResourceType.Wood), config.DevGiftWood);
            Add(nameof(ResourceType.Stone), config.DevGiftStone);
            Add(nameof(ResourceType.Food), config.DevGiftFood);
            Add(nameof(ResourceType.Coins), config.DevGiftCoins);
            if (NoStore())
            {
                var store = CheapestStore();
                if (store != null)
                    foreach (var a in BuildingPlacer.CostOf(store)) Add(a.type.ToString(), a.amount);
            }
            Add(Crystals, config.DevGiftCrystals);
            _gift.Clear();
            // the order of the mock-up: wood, stone, food, coins, then whatever a store asks, crystals last
            foreach (var kv in sums) if (kv.Key != Crystals) _gift.Add((kv.Key, kv.Value));
            if (sums.TryGetValue(Crystals, out var c)) _gift.Add((Crystals, c));
        }

        /// <summary>The banner over a paused game (also the cheats' way to see it).</summary>
        public void Show()
        {
            if (root == null || IsOpen || BalanceConfig.Instance == null) return;
            MakeGift();
            _stuck = 0f;
            _timeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            HudHider.Instance?.Hide(this);
            LayOut();
        }

        /// <summary>«Забрать»: the set into the town, the game goes on.</summary>
        public void Claim()
        {
            var resources = ResourceManager.Instance;
            var words = new System.Text.StringBuilder();
            foreach (var (what, amount) in _gift)
            {
                if (what == Crystals) Wallet.UpgradeCrystals += amount;
                else if (System.Enum.TryParse<ResourceType>(what, out var type)) resources?.AddOverCapacity(type, amount);
                else continue;
                if (words.Length > 0) words.Append(", ");
                words.Append(amount).Append(' ').Append(what == Crystals ? Localization.Get("#inventory_crystals") : ResourceNames.Of((ResourceType)System.Enum.Parse(typeof(ResourceType), what)));
            }
            if (words.Length > 0)
                EventLogManager.Instance?.Post(GameEvent.ForSettlement(Localization.Format("#log_devgift", words.ToString()),
                    EventSeverity.Good, EventGlyph.Check, BuildingIds.TownHall));
            Count++;
            Wait = BalanceConfig.Instance != null ? BalanceConfig.Instance.DevGiftGapSeconds : 7200f;
            _gift.Clear();
            if (root != null) root.SetActive(false);
            HudHider.Instance?.Show(this);
            Time.timeScale = _timeScale;
        }

        // ------------------------------------------------------------------ the plaque: label, then number + icon, 3 to a row
        private void LayOut()
        {
            foreach (var go in _items) if (go != null) Destroy(go);
            _items.Clear();
            if (plaque == null) return;
            if (float.IsNaN(_plaqueTop)) _plaqueTop = plaque.anchoredPosition.y + plaque.sizeDelta.y * 0.5f;
            var font = UiTheme.Heavy;
            var widths = new float[_gift.Count];
            for (var k = 0; k < _gift.Count; k++) widths[k] = NumberWidth(_gift[k].amount.ToString(), font) + GapIn + IconWidth(_gift[k].what);
            var rows = (_gift.Count + PerRow - 1) / PerRow;
            var widest = plaqueLabel != null ? plaqueLabel.preferredWidth : 0f;
            for (var r = 0; r < rows; r++) widest = Mathf.Max(widest, RowWidth(widths, r));
            var height = LabelHeight + RowHeight * rows + 10f;
            plaque.sizeDelta = new Vector2(widest + Pad * 2f, height);
            plaque.anchoredPosition = new Vector2(plaque.anchoredPosition.x, _plaqueTop - height * 0.5f);
            if (plaqueLabel != null) plaqueLabel.rectTransform.anchoredPosition = new Vector2(0f, height * 0.5f - LabelHeight * 0.5f - 4f);

            for (var r = 0; r < rows; r++)
            {
                var y = height * 0.5f - LabelHeight - RowHeight * (r + 0.5f);
                var x = -RowWidth(widths, r) * 0.5f;
                for (var k = r * PerRow; k < Mathf.Min(_gift.Count, (r + 1) * PerRow); k++)
                {
                    var n = _gift[k].amount.ToString();
                    var numW = NumberWidth(n, font);
                    var text = new GameObject("Amount", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                    text.transform.SetParent(plaque, false);
                    text.font = font;
                    text.fontSize = Mathf.RoundToInt(NumberSize);
                    text.color = new Color(0.27f, 0.19f, 0.09f);
                    text.alignment = TextAnchor.MiddleLeft;
                    text.horizontalOverflow = HorizontalWrapMode.Overflow;
                    text.raycastTarget = false;
                    text.text = n;
                    text.rectTransform.sizeDelta = new Vector2(numW + 4f, RowHeight);
                    text.rectTransform.anchoredPosition = new Vector2(x + numW * 0.5f, y);
                    _items.Add(text.gameObject);
                    x += numW + GapIn;
                    var iw = IconWidth(_gift[k].what);
                    var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    icon.transform.SetParent(plaque, false);
                    icon.sprite = SpriteOf(_gift[k].what);
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;
                    icon.rectTransform.sizeDelta = new Vector2(iw, Icon);
                    icon.rectTransform.anchoredPosition = new Vector2(x + iw * 0.5f, y);
                    _items.Add(icon.gameObject);
                    x += iw + GapOut;
                }
            }
        }

        private Sprite SpriteOf(string what)
        {
            if (what == Crystals) return crystalIcon;
            return iconLibrary != null && System.Enum.TryParse<ResourceType>(what, out var t) ? iconLibrary.GetIcon(t) : null;
        }

        // the icon's real width at Icon height (the user, 2026-11-19: fixed widths ran the icons into each other)
        private float IconWidth(string what)
        {
            var s = SpriteOf(what);
            return s != null && s.rect.height > 0f ? Icon * s.rect.width / s.rect.height : Icon;
        }

        private static float RowWidth(float[] widths, int row)
        {
            var w = 0f;
            var count = 0;
            for (var k = row * PerRow; k < Mathf.Min(widths.Length, (row + 1) * PerRow); k++, count++) w += widths[k];
            return w + GapOut * Mathf.Max(0, count - 1);
        }

        private static float NumberWidth(string n, Font font)
        {
            var settings = new TextGenerationSettings
            {
                font = font, fontSize = Mathf.RoundToInt(NumberSize), scaleFactor = 1f, horizontalOverflow = HorizontalWrapMode.Overflow,
                verticalOverflow = VerticalWrapMode.Overflow, generateOutOfBounds = true, pivot = new Vector2(0.5f, 0.5f),
                richText = false, color = Color.white,
            };
            return new TextGenerator().GetPreferredWidth(n, settings);
        }
    }
}
