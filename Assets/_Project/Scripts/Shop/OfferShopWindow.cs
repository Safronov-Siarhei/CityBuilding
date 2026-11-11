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
    /// The game pauses while it is up; a tap outside the frame closes it. One prefab in Resources
    /// (SetupProjectOfferShop); the window's picture per map season is loaded for it alone.
    /// </summary>
    public class OfferShopWindow : MonoBehaviour
    {
        private const string ResourcePath = "OfferShopWindow";
        private const float PopSeconds = 0.22f;
        private const float CloseSeconds = 0.15f;
        private const float PopFromScale = 0.85f;

        [Serializable]
        public class SlotView
        {
            public GameObject root;
            public GameObject back;
            public GameObject backGold;
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
        }

        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform window;
        [SerializeField] private Button shade;
        [SerializeField] private Image picture;
        [SerializeField] private float size = 1f;
        [SerializeField] private SlotView[] slots = new SlotView[0];
        [SerializeField] private ResourceIconLibrary icons;
        [SerializeField] private Sprite crystal;

        private static OfferShopWindow _current;
        private readonly System.Collections.Generic.List<Canvas> _hidden = new System.Collections.Generic.List<Canvas>();
        private float _t;
        private bool _closing;
        private float _timeScaleBefore = 1f;

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
            for (var i = 0; i < slots.Length; i++)
            {
                var index = i;
                slots[i].watch.onClick.AddListener(() => Take(index, OfferShop.Pay.Ad));
                slots[i].buy.onClick.AddListener(() => Take(index, OfferShop.Pay.Crystals));
                slots[i].free.onClick.AddListener(() => Take(index, OfferShop.Pay.Free));
            }
            group.alpha = 0f;
            window.localScale = Vector3.one * (PopFromScale * size);
            _timeScaleBefore = Time.timeScale;
            Time.timeScale = 0f;
            HideTheInterfaceBehind();
            if (picture != null) picture.sprite = UnityEngine.Resources.Load<Sprite>("Shop/Window_" + ThemeId());
            if (OfferShop.Instance != null) OfferShop.Instance.Changed += Redraw;
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
            OfferShop.Instance.Buy(slot, pay);
            Redraw();
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
            _t += Time.unscaledDeltaTime;
            if (_closing)
            {
                group.alpha = 1f - Mathf.Clamp01(_t / CloseSeconds);
                if (_t >= CloseSeconds) Destroy(gameObject);
                return;
            }
            var k = Mathf.Clamp01(_t / PopSeconds);
            group.alpha = k;
            var back = 1f + 2.2f * Mathf.Pow(k - 1f, 3f) + 1.2f * Mathf.Pow(k - 1f, 2f);
            window.localScale = Vector3.one * (size * Mathf.LerpUnclamped(PopFromScale, 1f, back));

            var shop = OfferShop.Instance;
            if (shop == null || shop.Rotation == null || !OfferShop.Allowed)
            {
                Close();
                return;
            }
            var now = shop.Now;
            for (var i = 0; i < slots.Length && i < OfferRotation.SlotCount; i++)
                Draw(slots[i], shop.Rotation.Slots[i], i == shop.AdPlayingSlot, now);
        }

        /// <summary>Writes a strip only when something on it changed (no strings built per frame).</summary>
        private void Draw(SlotView view, OfferSlot slot, bool adPlaying, double now)
        {
            var bundle = slot.Bundle;
            view.root.SetActive(bundle != null);
            if (bundle == null) return;
            var state = slot.Taken ? 0 : slot.IsFree ? 1 : adPlaying ? 3 : 2;
            if (view.shownId != bundle.id || view.shownState != state)
            {
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
                view.watch.gameObject.SetActive(state >= 2);
                view.buy.gameObject.SetActive(state >= 2);
                view.watch.interactable = state == 2;
                view.buy.interactable = state == 2;
                view.watchLabel.text = Localization.Get(state == 3 ? "#shop_ad_playing" : "#shop_watch");
                view.price.text = bundle.priceCrystals.ToString();
            }
            var seconds = Mathf.Max(0, Mathf.CeilToInt((float)(slot.EndsAt - now)));
            if (seconds != view.shownSeconds)
            {
                view.shownSeconds = seconds;
                view.timer.text = Clock(seconds);
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
            if (OfferShop.Instance != null) OfferShop.Instance.Changed -= Redraw;
            ShowTheInterfaceBehind();
            if (!_closing) Time.timeScale = _timeScaleBefore;
            if (_current != this) return;
            _current = null;
            UnityEngine.Resources.UnloadUnusedAssets();
        }
    }
}
