using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Maps;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// Gift bubbles over decorated houses (the user, 2026-11-12): a house that decor reaches
    /// (DecorBonus) now and then shows a round bubble with coins -- rarely an upgrade crystal -- and a
    /// tap takes it. The bigger the house's decor bonus, the sooner the next one: a gift every
    /// decor_gift_seconds at decor_gift_bonus_ref, faster or slower in proportion, never more often
    /// than decor_gift_min_seconds; a house without decor gives none, and one waiting gift is the most
    /// a house holds. Coins = decor_gift_coins_base + bonus x residents x decor_gift_coins_per_bonus;
    /// decor_gift_crystal_chance makes it decor_gift_crystals crystals instead. All on the sheet.
    ///
    /// One overlay canvas for every bubble, pooled buttons placed over the houses each frame (as
    /// NodeYieldBubble does) -- no world-space canvas per house on the phone. Not saved: a reload
    /// starts every house's wait afresh.
    /// </summary>
    public class DecorGifts : MonoBehaviour
    {
        private const float TickSeconds = 0.5f;
        private const float Diameter = 84f;
        private const float AboveRoof = 0.7f;

        [SerializeField] private Sprite coinIcon;
        [SerializeField] private Sprite crystalIcon;

        private class Gift
        {
            public float Progress;
            public bool Ready;
            public int Coins;
            public int Crystals;
            public float Height;
            public RectTransform Bubble;
            public Text Label;
            public Image Icon;
        }

        private readonly Dictionary<BuildingInstance, Gift> _gifts = new Dictionary<BuildingInstance, Gift>();
        private readonly List<BuildingInstance> _gone = new List<BuildingInstance>();
        private readonly Stack<RectTransform> _pool = new Stack<RectTransform>();
        private RectTransform _canvasRect;
        private Canvas _canvas;
        private Sprite _circle;
        private float _tick;

        /// <summary>How many gifts wait to be taken -- for the tests and cheats.</summary>
        public int Waiting
        {
            get
            {
                var n = 0;
                foreach (var g in _gifts.Values) if (g.Ready) n++;
                return n;
            }
        }

        private void Awake()
        {
            var go = new GameObject("DecorGiftsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            _canvas = go.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 4; // over the world, under the HUD and every panel
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            _canvasRect = (RectTransform)go.transform;
            _circle = NodeYieldBubble.CircleSprite();
        }

        private void Update()
        {
            _tick -= Time.deltaTime;
            if (_tick > 0f) return;
            var dt = TickSeconds - _tick;
            _tick = TickSeconds;
            Grow(dt);
        }

        /// <summary>Moves every decorated house's wait on by `dt` game seconds and makes the gifts that are due.</summary>
        public void Grow(float dt)
        {
            var config = BalanceConfig.Instance;
            if (config == null || dt <= 0f) return;

            var houses = BuildingInstance.Houses;
            for (var i = 0; i < houses.Count; i++)
            {
                var house = houses[i];
                if (house == null || house.IsBot || !house.IsOperational) continue;
                var people = HousePeople.PlacesOf(house);
                var bonus = people > 0 ? DecorBonus.PerResident(house) : 0f;
                _gifts.TryGetValue(house, out var gift);
                if (bonus <= 0f && (gift == null || !gift.Ready)) continue;
                if (gift == null) _gifts[house] = gift = new Gift();
                if (gift.Ready) continue;

                var interval = Mathf.Max(config.DecorGiftMinSeconds,
                    config.DecorGiftSeconds * config.DecorGiftBonusRef / Mathf.Max(0.0001f, bonus));
                gift.Progress += dt / interval;
                if (gift.Progress < 1f) continue;

                gift.Progress = 0f;
                gift.Ready = true;
                if (config.DecorGiftCrystals > 0 && Random.value < config.DecorGiftCrystalChance)
                {
                    gift.Crystals = config.DecorGiftCrystals;
                    gift.Coins = 0;
                }
                else
                {
                    gift.Crystals = 0;
                    gift.Coins = Mathf.Max(1, Mathf.RoundToInt(config.DecorGiftCoinsBase + bonus * people * config.DecorGiftCoinsPerBonus));
                }
                Show(house, gift);
            }

            // houses pulled down or burnt take their gift with them
            _gone.Clear();
            foreach (var pair in _gifts)
            {
                if (pair.Key == null || !pair.Key.isActiveAndEnabled || pair.Key.CurrentHealth <= 0) _gone.Add(pair.Key);
            }
            foreach (var house in _gone)
            {
                Hide(_gifts[house]);
                _gifts.Remove(house);
            }
        }

        /// <summary>Takes a waiting gift: its coins to the store, or its crystals to the wallet.</summary>
        public void Take(BuildingInstance house)
        {
            if (house == null || !_gifts.TryGetValue(house, out var gift) || !gift.Ready) return;
            if (gift.Crystals > 0) Wallet.UpgradeCrystals += gift.Crystals;
            else if (gift.Coins > 0) ResourceManager.Instance?.Add(ResourceType.Coins, gift.Coins);
            gift.Ready = false;
            gift.Coins = gift.Crystals = 0;
            Hide(gift);
        }

        private void Show(BuildingInstance house, Gift gift)
        {
            gift.Height = HarvestProgressBar.MeasuredHeightOf(house.transform) + AboveRoof;
            gift.Bubble = _pool.Count > 0 ? _pool.Pop() : MakeBubble();
            gift.Icon = gift.Bubble.Find("Icon").GetComponent<Image>();
            gift.Label = gift.Bubble.Find("Amount").GetComponent<Text>();
            gift.Icon.sprite = gift.Crystals > 0 ? crystalIcon : coinIcon;
            gift.Label.text = "+" + (gift.Crystals > 0 ? gift.Crystals : gift.Coins);
            var button = gift.Bubble.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => Take(house));
            gift.Bubble.gameObject.SetActive(true);
        }

        private void Hide(Gift gift)
        {
            if (gift == null || gift.Bubble == null) return;
            gift.Bubble.GetComponent<Button>().onClick.RemoveAllListeners();
            gift.Bubble.gameObject.SetActive(false);
            _pool.Push(gift.Bubble);
            gift.Bubble = null;
        }

        private RectTransform MakeBubble()
        {
            var go = new GameObject("Gift", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_canvasRect, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(Diameter, Diameter);
            var image = go.GetComponent<Image>();
            image.sprite = _circle;
            go.GetComponent<Button>().targetGraphic = image;

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(rect, false);
            var iconRect = (RectTransform)icon.transform;
            iconRect.sizeDelta = new Vector2(Diameter * 0.62f, Diameter * 0.62f);
            iconRect.anchoredPosition = new Vector2(0f, 6f);
            var iconImage = icon.GetComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            var label = new GameObject("Amount", typeof(RectTransform), typeof(Text), typeof(Shadow));
            label.transform.SetParent(rect, false);
            var labelRect = (RectTransform)label.transform;
            labelRect.sizeDelta = new Vector2(Diameter * 1.4f, 36f);
            labelRect.anchoredPosition = new Vector2(0f, -Diameter * 0.5f);
            var text = label.GetComponent<Text>();
            text.font = UiTheme.Heavy;
            text.fontSize = 30;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return rect;
        }

        private void LateUpdate()
        {
            var camera = Camera.main;
            var hidden = camera == null || ModalGate.IsBlocked;
            if (_canvas.enabled == hidden) _canvas.enabled = !hidden;
            if (hidden) return;

            var bob = 6f * Mathf.Sin(Time.unscaledTime * 3f);
            foreach (var pair in _gifts)
            {
                var gift = pair.Value;
                if (gift.Bubble == null || pair.Key == null) continue;
                var screen = camera.WorldToScreenPoint(pair.Key.transform.position + Vector3.up * gift.Height);
                gift.Bubble.localScale = screen.z > 0f ? Vector3.one : Vector3.zero;
                if (screen.z <= 0f) continue;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out var local);
                gift.Bubble.anchoredPosition = local + new Vector2(0f, bob);
            }
        }
    }
}
