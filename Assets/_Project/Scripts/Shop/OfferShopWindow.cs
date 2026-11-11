using System;
using CityBuilder.Core;
using CityBuilder.Maps;
using CityBuilder.Resources;
using CityBuilder.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.Shop
{
    /// <summary>
    /// The shop's window «Лавка» (mock-up Design/store/OfferShop_Mockup.png, the rate-us window's
    /// style): three parchment strips, each a bundle with its goods and its timer, and per state
    /// «Смотреть» + the blue crystal price, the gold «Забрать бесплатно», or the «Получено» stamp.
    /// The game pauses while it is up; the red seal or a tap outside the frame closes it. The player's
    /// crystals sit on the frame's top-right; a crystal buy without enough shakes them red with a line
    /// under them. «Смотреть» shows only while an ad is ready (<see cref="RewardedAds"/>). A timer
    /// turns red and pulses in its last shop_timer_warn_seconds; a new bundle pops in with a flash.
    /// One prefab in Resources (SetupProjectOfferShop); the window's picture per map season is
    /// loaded for it alone.
    /// </summary>
    public class OfferShopWindow : MonoBehaviour
    {
        private const string ResourcePath = "OfferShopWindow";
        private const string ShortagePath = "OfferShortageWindow";
        /// <summary>How long the «Не хватает ресурсов» window stays after the goods were given (their flight).</summary>
        private const float ShortageCloseDelay = 1.4f;
        private const float PopSeconds = 0.22f;
        private const float CloseSeconds = 0.15f;
        private const float PopFromScale = 0.85f;
        private const float ChangeSeconds = 0.45f;
        private const float ShakeSeconds = 0.45f;
        private const float ToastSeconds = 1.8f;
        private static readonly Color TimerColor = new Color(1f, 240 / 255f, 210 / 255f);
        /// <summary>The warning red of a running-out timer and of missing crystals.</summary>
        public static readonly Color WarnColor = new Color(1f, 0.33f, 0.27f);

        [Serializable]
        public class SlotView
        {
            public GameObject root;
            public GameObject back;
            public GameObject backGold;
            public GameObject backCrystal;
            public GameObject backPaid;
            public Image flash;
            public Text name;
            public Image[] goodIcons;
            public Text[] goodAmounts;
            public Text timer;
            public Button watch;
            public Text watchLabel;
            public Button buy;
            public Text price;
            public Button free;
            public GameObject stamp;
            public Button watchWide;
            public Text watchWideLabel;
            public Button buyWide;
            public Text buyWidePrice;
            public Button money;
            public Text moneyLabel;
            public GameObject badge;
            public Text badgeText;
            [NonSerialized] public int shownSeconds = -1;
            [NonSerialized] public string shownId;
            [NonSerialized] public int shownState = -1;
            [NonSerialized] public bool shownWarn;
            [NonSerialized] public string lastId;
            [NonSerialized] public float changeT = ChangeSeconds;
            [NonSerialized] public float shakeT = ShakeSeconds;
            [NonSerialized] public Vector2 buyHome;
            [NonSerialized] public Vector2 buyWideHome;
            [NonSerialized] public Vector2 widePriceHome, wideCrystalHome;
            [NonSerialized] public RectTransform wideCrystal;
        }

        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform window;
        [SerializeField] private Button shade;
        [SerializeField] private Button close;
        [SerializeField] private Image picture;
        [SerializeField] private float size = 1f;
        [SerializeField] private SlotView[] slots = new SlotView[0];
        [SerializeField] private ResourceIconLibrary icons;
        [SerializeField] private Sprite crystal;
        [SerializeField] private RectTransform counter;
        [SerializeField] private Text counterText;
        [SerializeField] private Text toast;
        /// <summary>The «Не хватает ресурсов» window (the user, 2026-11-11): the shop's look, one strip -- exactly what a building lacks.</summary>
        [SerializeField] private bool shortage;
        [SerializeField] private string pictureName = "Shop/Window_";

        private static OfferShopWindow _current;
        private readonly System.Collections.Generic.List<Canvas> _hidden = new System.Collections.Generic.List<Canvas>();
        private float _t;
        private bool _closing;
        private float _timeScaleBefore = 1f;
        private int _shownCrystals = -1;
        private float _counterShakeT = ShakeSeconds;
        private Vector2 _counterHome;
        private float _toastLeft;
        private float _warnSeconds;
        private OfferRewardFlight _flight;

        public static bool IsOpen => _current != null;

        private static BalanceConfig.OfferBundle _pendingShortage;
        private OfferSlot _shortageSlot;
        private float _closeIn = -1f;

        /// <summary>
        /// Puts up «Не хватает ресурсов» with this offer (OfferShop.ShortageFor): picking a building the
        /// player cannot afford sends them here; buying closes it and they go on placing the building.
        /// </summary>
        public static void ShowShortage(BalanceConfig.OfferBundle bundle)
        {
            if (bundle == null || _current != null || OfferShop.Instance == null || !OfferShop.Allowed) return;
            var prefab = UnityEngine.Resources.Load<OfferShopWindow>(ShortagePath);
            if (prefab == null) return;
            _pendingShortage = bundle;
            _current = Instantiate(prefab);
            _pendingShortage = null;
        }

        /// <summary>Puts the window up (nothing if it is up, the shop is off here, or the prefab is missing).</summary>
        public static void Show()
        {
            if (_current != null || OfferShop.Instance == null || !OfferShop.Allowed) return;
            var prefab = UnityEngine.Resources.Load<OfferShopWindow>(ResourcePath);
            if (prefab == null) return;
            _current = Instantiate(prefab);
        }

        private void Awake()
        {
            shade.onClick.AddListener(Close);
            close.onClick.AddListener(Close);
            for (var i = 0; i < slots.Length; i++)
            {
                var index = i;
                slots[i].watch.onClick.AddListener(() => Take(index, OfferShop.Pay.Ad));
                slots[i].buy.onClick.AddListener(() => Take(index, OfferShop.Pay.Crystals));
                slots[i].free.onClick.AddListener(() => Take(index, OfferShop.Pay.Free));
                slots[i].watchWide.onClick.AddListener(() => Take(index, OfferShop.Pay.Ad));
                slots[i].money.onClick.AddListener(() => Take(index, OfferShop.Pay.Money));
                slots[i].buyWide.onClick.AddListener(() => Take(index, OfferShop.Pay.Crystals));
                slots[i].buyHome = ((RectTransform)slots[i].buy.transform).anchoredPosition;
                slots[i].buyWideHome = ((RectTransform)slots[i].buyWide.transform).anchoredPosition;
                slots[i].wideCrystal = slots[i].buyWide.transform.Find("Crystal") as RectTransform;
                slots[i].widePriceHome = slots[i].buyWidePrice.rectTransform.anchoredPosition;
                if (slots[i].wideCrystal != null) slots[i].wideCrystal.anchoredPosition = slots[i].wideCrystalHome = slots[i].wideCrystal.anchoredPosition;
            }
            _counterHome = counter.anchoredPosition;
            toast.gameObject.SetActive(false);
            _warnSeconds = BalanceConfig.Sandbox.ShopTimerWarnSeconds;
            group.alpha = 0f;
            window.localScale = Vector3.one * (PopFromScale * size);
            _timeScaleBefore = Time.timeScale;
            Time.timeScale = 0f;
            HideTheInterfaceBehind();
            if (picture != null) picture.sprite = UnityEngine.Resources.Load<Sprite>(pictureName + ThemeId());
            if (shortage)
            {
                _shortageSlot = new OfferSlot { Bundle = _pendingShortage, EndsAt = (OfferShop.Instance != null ? OfferShop.Instance.Now : 0) + 1e6 };
                // no timer: the offer lasts as long as the window
                if (slots.Length > 0 && slots[0].timer != null) slots[0].timer.transform.parent.gameObject.SetActive(false);
            }
            _flight = gameObject.AddComponent<OfferRewardFlight>();
            if (OfferShop.Instance != null)
            {
                OfferShop.Instance.Changed += Redraw;
                OfferShop.Instance.Given += OnGiven;
            }
            Redraw();
        }

        private static string ThemeId()
        {
            switch (SeasonStyle.Current)
            {
                case MapSeason.Spring: return MapThemes.Id(MapTheme.Spring);
                case MapSeason.Autumn: return MapThemes.Id(MapTheme.Autumn);
                case MapSeason.Winter: return MapThemes.Id(MapTheme.Winter);
                default: return MapThemes.Id(MapTheme.Summer);
            }
        }

        private void Take(int slot, OfferShop.Pay pay)
        {
            if (_closing || OfferShop.Instance == null) return;
            var s = shortage ? _shortageSlot : OfferShop.Instance.Rotation.Slots[slot];
            if (s == null || s.Taken) return;
            if (pay == OfferShop.Pay.Money)
            {
                // no store yet: a line says purchases come later
                Toast("#shop_buy_soon");
                return;
            }
            if (pay == OfferShop.Pay.Crystals && s.Bundle != null && Wallet.UpgradeCrystals < s.Bundle.priceCrystals)
            {
                // not enough: the counter and the button shake red, a line says why
                _counterShakeT = 0f;
                slots[slot].shakeT = 0f;
                Toast("#shop_no_crystals");
                return;
            }
            if (shortage) OfferShop.Instance.BuyShortage(s.Bundle, pay);
            else OfferShop.Instance.Buy(slot, pay);
            Redraw();
        }

        private void Toast(string key)
        {
            toast.text = Localization.Get(key);
            _toastLeft = ToastSeconds;
            toast.gameObject.SetActive(true);
        }

        /// <summary>
        /// The wide blue button's price and crystal as one group in its middle (the user, 2026-11-11: it sat
        /// right of centre). The price is right-aligned, so the group's left edge is its right edge less the
        /// number's width; both move by the same amount from where SetupProject put them.
        /// </summary>
        private static void CentreWidePrice(SlotView view)
        {
            if (view.wideCrystal == null) return;
            var price = view.buyWidePrice.rectTransform;
            var button = (RectTransform)view.buyWide.transform;
            price.anchoredPosition = view.widePriceHome;
            view.wideCrystal.anchoredPosition = view.wideCrystalHome;
            var priceRight = button.InverseTransformPoint(price.TransformPoint(new Vector3(price.rect.xMax, 0f, 0f))).x;
            var crystalRight = button.InverseTransformPoint(view.wideCrystal.TransformPoint(new Vector3(view.wideCrystal.rect.xMax, 0f, 0f))).x;
            var left = priceRight - view.buyWidePrice.preferredWidth;
            var shift = new Vector2(button.rect.center.x - (left + crystalRight) * 0.5f, 0f);
            price.anchoredPosition = view.widePriceHome + shift;
            view.wideCrystal.anchoredPosition = view.wideCrystalHome + shift;
        }

        /// <summary>The goods fly from the strip to where they went (OfferRewardFlight), one good after another.</summary>
        private void OnGiven(int slot, BalanceConfig.OfferBundle bundle)
        {
            if (shortage)
            {
                if (slot != OfferShop.ShortageSlot || _shortageSlot == null || bundle != _shortageSlot.Bundle) return;
                _shortageSlot.Taken = true;
                _closeIn = ShortageCloseDelay;
                slot = 0;
            }
            else if (slot == OfferShop.ShortageSlot) return;
            if (_closing || slot < 0 || slot >= slots.Length || bundle == null) return;
            var view = slots[slot];
            for (var g = 0; g < bundle.goods.Count && g < view.goodIcons.Length; g++)
            {
                var good = bundle.goods[g];
                _flight.Fly(Icon(good.resource), good.resource, good.amount, view.goodIcons[g].rectTransform, counter, g * 0.15f);
            }
        }

        /// <summary>Forces every strip to redraw (a change or a purchase).</summary>
        private void Redraw()
        {
            foreach (var view in slots)
            {
                view.shownId = null;
                view.shownState = -1;
                view.shownSeconds = -1;
            }
        }

        private void Update()
        {
            var dt = Time.unscaledDeltaTime;
            _t += dt;
            if (_closing)
            {
                group.alpha = 1f - Mathf.Clamp01(_t / CloseSeconds);
                if (_t >= CloseSeconds) Destroy(gameObject);
                return;
            }
            var k = Mathf.Clamp01(_t / PopSeconds);
            group.alpha = k;
            window.localScale = Vector3.one * (size * Mathf.LerpUnclamped(PopFromScale, 1f, Back(k)));

            var shop = OfferShop.Instance;
            if (shop == null || shop.Rotation == null || !OfferShop.Allowed || (shortage && _shortageSlot?.Bundle == null))
            {
                Close();
                return;
            }
            DrawCounter(dt);
            var now = shop.Now;
            var adReady = RewardedAds.IsReady;
            if (shortage)
            {
                if (slots.Length > 0) Draw(slots[0], _shortageSlot, shop.AdPlayingSlot == OfferShop.ShortageSlot, adReady, now, dt);
                if (_closeIn >= 0f)
                {
                    _closeIn -= dt;
                    if (_closeIn < 0f) Close();
                }
                return;
            }
            for (var i = 0; i < slots.Length && i < shop.Rotation.SlotCount; i++)
                Draw(slots[i], shop.Rotation.Slots[i], i == shop.AdPlayingSlot, adReady, now, dt);
        }

        /// <summary>An ease with a small overshoot, 0..1.</summary>
        private static float Back(float k) => 1f + 2.2f * Mathf.Pow(k - 1f, 3f) + 1.2f * Mathf.Pow(k - 1f, 2f);

        /// <summary>A decaying side-to-side shake for t in 0..ShakeSeconds.</summary>
        private static float Shake(float t) => t >= ShakeSeconds ? 0f : Mathf.Sin(t * 60f) * 14f * (1f - t / ShakeSeconds);

        private void DrawCounter(float dt)
        {
            var crystals = Wallet.UpgradeCrystals;
            if (crystals != _shownCrystals)
            {
                _shownCrystals = crystals;
                counterText.text = crystals.ToString();
            }
            if (_counterShakeT < ShakeSeconds)
            {
                _counterShakeT += dt;
                counter.anchoredPosition = _counterHome + new Vector2(Shake(_counterShakeT), 0f);
                counterText.color = _counterShakeT < ShakeSeconds ? WarnColor : TimerColor;
            }
            if (_toastLeft > 0f)
            {
                _toastLeft -= dt;
                var c = toast.color;
                c.a = Mathf.Clamp01(_toastLeft / 0.4f);
                toast.color = c;
                if (_toastLeft <= 0f) toast.gameObject.SetActive(false);
            }
        }

        /// <summary>Writes a strip only when something on it changed (no strings built per frame).</summary>
        private void Draw(SlotView view, OfferSlot slot, bool adPlaying, bool adReady, double now, float dt)
        {
            var bundle = slot.Bundle;
            view.root.SetActive(bundle != null);
            if (bundle == null) return;
            // 0 taken, 1 free, 2 ad + crystals, 3 the ad playing, 4 crystals only (no ad to show, or worth
            // more than shop_ad_max_ads ads -- one wide blue button),
            // 5 crystals for an ad, 6 its ad playing, 7 no ad to show, 8 crystals for money
            int state;
            if (slot.Taken) state = 0;
            else if (slot.IsFree) state = 1;
            else if (slot.IsPaid) state = 8;
            else if (slot.IsAdOnly) state = adPlaying ? 6 : adReady ? 5 : 7;
            else if (!OfferRotation.AdAllowed(bundle, BalanceConfig.Sandbox.ShopAdMaxAds)) state = 4;
            else state = adPlaying ? 3 : adReady ? 2 : 4;
            if (view.shownId != bundle.id || view.shownState != state)
            {
                // a new bundle (not the window's first look): pop it in with a flash
                if (view.lastId != null && view.lastId != bundle.id) view.changeT = 0f;
                view.lastId = bundle.id;
                view.shownId = bundle.id;
                view.shownState = state;
                view.name.text = Localization.Get(bundle.nameKey);
                var crystalOffer = slot.IsAdOnly || slot.IsPaid;
                view.back.SetActive(!slot.IsFree && !crystalOffer);
                view.backGold.SetActive(slot.IsFree);
                view.backCrystal.SetActive(slot.IsAdOnly);
                view.backPaid.SetActive(slot.IsPaid);
                var config = BalanceConfig.Sandbox;
                var bonus = OfferRotation.BonusPercent(bundle, config.ShopCrystalUsd, config.ShopAdCrystalsBase);
                view.badge.SetActive(bonus > 0);
                if (bonus > 0) view.badgeText.text = Localization.Format("#shop_bonus", bonus);
                for (var g = 0; g < view.goodIcons.Length; g++)
                {
                    var has = g < bundle.goods.Count;
                    view.goodIcons[g].gameObject.SetActive(has);
                    view.goodAmounts[g].gameObject.SetActive(has);
                    if (!has) continue;
                    view.goodIcons[g].sprite = Icon(bundle.goods[g].resource);
                    view.goodAmounts[g].text = bundle.goods[g].amount.ToString();
                    CrystalOrder.Lay(view.goodIcons[g], view.goodAmounts[g], bundle.goods[g].resource == "Crystals");
                }
                view.stamp.SetActive(state == 0);
                Dim(view, state == 0);
                view.free.gameObject.SetActive(state == 1);
                view.watch.gameObject.SetActive(state == 2 || state == 3);
                view.buy.gameObject.SetActive(state == 2 || state == 3);
                view.buyWide.gameObject.SetActive(state == 4);
                var widePrice = bundle.priceCrystals.ToString();
                if (view.buyWidePrice.text != widePrice)
                {
                    view.buyWidePrice.text = widePrice;
                    CentreWidePrice(view);
                }
                view.watchWide.gameObject.SetActive(state >= 5 && state <= 7);
                view.watchWide.interactable = state == 5;
                view.watchWideLabel.text = Localization.Get(state == 6 ? "#shop_ad_playing" : "#shop_watch");
                view.money.gameObject.SetActive(state == 8);
                if (state == 8)
                    view.moneyLabel.text = Localization.Format("#shop_price_usd",
                        bundle.priceUsd.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
                view.watch.interactable = state == 2;
                view.buy.interactable = state == 2;
                view.watchLabel.text = Localization.Get(state == 3 ? "#shop_ad_playing" : "#shop_watch");
                view.price.text = bundle.priceCrystals.ToString();
            }

            var left = (float)(slot.EndsAt - now);
            var seconds = Mathf.Max(0, Mathf.CeilToInt(left));
            if (seconds != view.shownSeconds)
            {
                view.shownSeconds = seconds;
                view.timer.text = Clock(seconds);
            }
            // the last seconds: red and a pulse, so the player sees it is about to go
            var warn = !slot.Taken && left <= _warnSeconds;
            var pill = view.timer.transform.parent;
            if (warn != view.shownWarn)
            {
                view.shownWarn = warn;
                view.timer.color = warn ? WarnColor : TimerColor;
                if (slot.Taken) SetAlpha(view.timer, TakenFade);
                if (!warn) pill.localScale = Vector3.one;
            }
            if (warn)
            {
                var p = 1f + 0.08f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f));
                pill.localScale = new Vector3(p, p, 1f);
            }

            var rt = (RectTransform)view.root.transform;
            if (view.changeT < ChangeSeconds)
            {
                view.changeT += dt;
                var c = Mathf.Clamp01(view.changeT / ChangeSeconds);
                rt.localScale = new Vector3(1f, Mathf.LerpUnclamped(0.2f, 1f, Back(c)), 1f);
                var f = view.flash.color;
                f.a = 0.85f * (1f - c);
                view.flash.color = f;
                view.flash.gameObject.SetActive(c < 1f);
            }
            else if (view.flash.gameObject.activeSelf)
            {
                rt.localScale = Vector3.one;
                view.flash.gameObject.SetActive(false);
            }

            if (view.shakeT < ShakeSeconds)
            {
                view.shakeT += dt;
                var dx = new Vector2(Shake(view.shakeT), 0f);
                ((RectTransform)view.buy.transform).anchoredPosition = view.buyHome + dx;
                ((RectTransform)view.buyWide.transform).anchoredPosition = view.buyWideHome + dx;
            }
        }

        /// <summary>The grey of a taken strip's parchment, and how faint its words, goods and timer get (the user, 2026-11-11: taken reads as inactive).</summary>
        private static readonly Color TakenBack = new Color(0.62f, 0.62f, 0.62f, 1f);
        private const float TakenFade = 0.45f;

        /// <summary>A taken bundle goes grey and faint; its «Получено» stamp stays bright.</summary>
        private static void Dim(SlotView view, bool taken)
        {
            var a = taken ? TakenFade : 1f;
            var back = view.back.GetComponent<Image>();
            if (back != null) back.color = taken ? TakenBack : Color.white;
            var backGold = view.backGold.GetComponent<Image>();
            if (backGold != null) backGold.color = taken ? TakenBack : Color.white;
            SetAlpha(view.name, a);
            foreach (var icon in view.goodIcons) SetAlpha(icon, a);
            foreach (var amount in view.goodAmounts) SetAlpha(amount, a);
            SetAlpha(view.timer, a);
            var pill = view.timer.transform.parent.GetComponent<Image>();
            if (pill != null) SetAlpha(pill, a);
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            var c = graphic.color;
            c.a = alpha;
            graphic.color = c;
        }

        private Sprite Icon(string resource)
        {
            if (resource == "Crystals") return crystal;
            return icons != null && Enum.TryParse<ResourceType>(resource, out var type) ? icons.GetIcon(type) : null;
        }

        /// <summary>m:ss for the timers (shared by the HUD icon and the menu's cards).</summary>
        public static string Clock(int seconds)
        {
            var m = seconds / 60;
            var s = seconds % 60;
            return s < 10 ? m + ":0" + s : m + ":" + s;
        }

        private void HideTheInterfaceBehind()
        {
            var own = GetComponent<Canvas>();
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (canvas == own || !canvas.enabled || !canvas.isRootCanvas) continue;
                if (canvas.renderMode == RenderMode.WorldSpace || canvas.sortingOrder > own.sortingOrder) continue;
                canvas.enabled = false;
                _hidden.Add(canvas);
            }
        }

        private void ShowTheInterfaceBehind()
        {
            foreach (var canvas in _hidden)
                if (canvas != null) canvas.enabled = true;
            _hidden.Clear();
        }

        private void Close()
        {
            if (_closing) return;
            _closing = true;
            _t = 0f;
            group.interactable = false;
            Time.timeScale = _timeScaleBefore;
            ShowTheInterfaceBehind();
        }

        private void OnDestroy()
        {
            if (OfferShop.Instance != null)
            {
                OfferShop.Instance.Changed -= Redraw;
                OfferShop.Instance.Given -= OnGiven;
            }
            ShowTheInterfaceBehind();
            if (!_closing) Time.timeScale = _timeScaleBefore;
            if (_current != this) return;
            _current = null;
            UnityEngine.Resources.UnloadUnusedAssets();
        }
    }
}
