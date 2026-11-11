using System;
using CityBuilder.Core;
using CityBuilder.Maps;
using CityBuilder.Resources;
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
            [NonSerialized] public int shownSeconds = -1;
            [NonSerialized] public string shownId;
            [NonSerialized] public int shownState = -1;
            [NonSerialized] public bool shownWarn;
            [NonSerialized] public string lastId;
            [NonSerialized] public float changeT = ChangeSeconds;
            [NonSerialized] public float shakeT = ShakeSeconds;
            [NonSerialized] public Vector2 buyHome;
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
                slots[i].buyHome = ((RectTransform)slots[i].buy.transform).anchoredPosition;
            }
            _counterHome = counter.anchoredPosition;
            toast.gameObject.SetActive(false);
            _warnSeconds = BalanceConfig.Sandbox.ShopTimerWarnSeconds;
            group.alpha = 0f;
            window.localScale = Vector3.one * (PopFromScale * size);
            _timeScaleBefore = Time.timeScale;
            Time.timeScale = 0f;
            HideTheInterfaceBehind();
            if (picture != null) picture.sprite = UnityEngine.Resources.Load<Sprite>("Shop/Window_" + ThemeId());
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
            var s = OfferShop.Instance.Rotation.Slots[slot];
            if (pay == OfferShop.Pay.Crystals && s.Bundle != null && Wallet.UpgradeCrystals < s.Bundle.priceCrystals)
            {
                // not enough: the counter and the button shake red, a line says why
                _counterShakeT = 0f;
                slots[slot].shakeT = 0f;
                _toastLeft = ToastSeconds;
                toast.gameObject.SetActive(true);
                return;
            }
            OfferShop.Instance.Buy(slot, pay);
            Redraw();
        }

        /// <summary>The goods fly from the strip to where they went (OfferRewardFlight), one good after another.</summary>
        private void OnGiven(int slot, BalanceConfig.OfferBundle bundle)
        {
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
            if (shop == null || shop.Rotation == null || !OfferShop.Allowed)
            {
                Close();
                return;
            }
            DrawCounter(dt);
            var now = shop.Now;
            var adReady = RewardedAds.IsReady;
            for (var i = 0; i < slots.Length && i < OfferRotation.SlotCount; i++)
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
            // 0 taken, 1 free, 2 ad + crystals, 3 the ad playing, 4 crystals only (no ad to show)
            var state = slot.Taken ? 0 : slot.IsFree ? 1 : adPlaying ? 3 : adReady ? 2 : 4;
            if (view.shownId != bundle.id || view.shownState != state)
            {
                // a new bundle (not the window's first look): pop it in with a flash
                if (view.lastId != null && view.lastId != bundle.id) view.changeT = 0f;
                view.lastId = bundle.id;
                view.shownId = bundle.id;
                view.shownState = state;
                view.name.text = Localization.Get("#bundle_" + bundle.id);
                view.back.SetActive(!slot.IsFree);
                view.backGold.SetActive(slot.IsFree);
                for (var g = 0; g < view.goodIcons.Length; g++)
                {
                    var has = g < bundle.goods.Count;
                    view.goodIcons[g].gameObject.SetActive(has);
                    view.goodAmounts[g].gameObject.SetActive(has);
                    if (!has) continue;
                    view.goodIcons[g].sprite = Icon(bundle.goods[g].resource);
                    view.goodAmounts[g].text = bundle.goods[g].amount.ToString();
                }
                view.stamp.SetActive(state == 0);
                view.free.gameObject.SetActive(state == 1);
                view.watch.gameObject.SetActive(state == 2 || state == 3);
                view.buy.gameObject.SetActive(state >= 2);
                view.watch.interactable = state == 2;
                view.buy.interactable = state == 2 || state == 4;
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
                ((RectTransform)view.buy.transform).anchoredPosition = view.buyHome + new Vector2(Shake(view.shakeT), 0f);
            }
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
