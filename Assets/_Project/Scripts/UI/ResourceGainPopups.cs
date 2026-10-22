using CityBuilder.Core;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// «+N» with the resource's icon, rising over where something was earned (the user,
    /// 2026-10-22): over a sawmill or a quarry as its worker brings a load in, and over an enemy as
    /// it falls in an Invasion run, with the gold it paid. Screen-space labels on the HUD canvas,
    /// a few made once and reused: each follows its point on screen, rises and fades over
    /// <see cref="Seconds"/>. No allocation per popup, nothing running while none is up.
    /// </summary>
    public class ResourceGainPopups : MonoBehaviour
    {
        private const int PoolSize = 14;
        private const float Seconds = 1.1f;
        private const float RisePixels = 70f;
        private const float HeightOverBuilding = 2.2f;
        private const float HeightOverUnit = 1.1f;
        private const float IconPixels = 40f;
        private static readonly Color GainColor = new Color(1f, 0.93f, 0.55f);
        private static readonly Color CoinColor = new Color(1f, 0.82f, 0.25f);

        [SerializeField] private ResourceIconLibrary icons;

        public static ResourceGainPopups Instance { get; private set; }

        private struct Popup
        {
            public RectTransform Root;
            public Text Label;
            public Image Icon;
            public Color Color;
            public Vector3 World;
            public float Age;
        }

        private Popup[] _pool;
        private RectTransform _rect;
        private int _live;

        private void Awake() => Instance = this;

        private void Start() => Citizens.CitizenVisualsManager.Delivered += OnDelivered;

        private void OnDestroy()
        {
            Citizens.CitizenVisualsManager.Delivered -= OnDelivered;
            if (Instance == this) Instance = null;
        }

        private void OnDelivered(Citizens.ProductionBuilding building, int amount)
        {
            if (building != null) Show(building.transform.position + Vector3.up * HeightOverBuilding, amount, building.ProducesResource);
        }

        /// <summary>The gold an enemy paid as it fell, over where it stood.</summary>
        public void ShowCoins(Vector3 at, int amount) => Show(at + Vector3.up * HeightOverUnit, amount, ResourceType.Coins);

        /// <summary>Shows «+amount» and the resource's icon at `world`.</summary>
        public void Show(Vector3 world, int amount, ResourceType resource)
        {
            if (amount <= 0) return;
            EnsurePool();
            var slot = 0;
            for (var i = 0; i < _pool.Length; i++)
            {
                if (_pool[i].Age >= Seconds) { slot = i; break; }
                if (_pool[i].Age > _pool[slot].Age) slot = i;   // all up: the oldest makes way
            }
            ref var popup = ref _pool[slot];
            if (popup.Age >= Seconds) _live++;
            popup.World = world;
            popup.Age = 0f;
            popup.Color = resource == ResourceType.Coins ? CoinColor : GainColor;
            popup.Label.text = Localization.Format("#popup_gain", amount);
            var sprite = icons != null ? icons.GetIcon(resource) : null;
            popup.Icon.sprite = sprite;
            popup.Icon.enabled = sprite != null;
            popup.Root.gameObject.SetActive(true);
            enabled = true;
            Place(ref popup);
        }

        private void LateUpdate()
        {
            if (_pool == null || _live == 0)
            {
                enabled = false;
                return;
            }
            var dt = Time.deltaTime;
            for (var i = 0; i < _pool.Length; i++)
            {
                ref var popup = ref _pool[i];
                if (popup.Age >= Seconds) continue;
                popup.Age += dt;
                if (popup.Age >= Seconds)
                {
                    popup.Root.gameObject.SetActive(false);
                    _live--;
                    continue;
                }
                Place(ref popup);
            }
        }

        private void Place(ref Popup popup)
        {
            var camera = Camera.main;
            if (camera == null) return;
            var screen = camera.WorldToScreenPoint(popup.World);
            var t = popup.Age / Seconds;
            var alpha = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
            if (screen.z <= 0f || !RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screen, null, out var local)) alpha = 0f;
            else popup.Root.anchoredPosition = local + new Vector2(0f, RisePixels * t);
            var c = popup.Color;
            c.a = alpha;
            popup.Label.color = c;
            popup.Icon.color = new Color(1f, 1f, 1f, alpha);
        }

        private void EnsurePool()
        {
            if (_pool != null) return;
            _rect = (RectTransform)transform;
            _pool = new Popup[PoolSize];
            for (var i = 0; i < PoolSize; i++)
            {
                var root = new GameObject("Gain", typeof(RectTransform)).GetComponent<RectTransform>();
                root.SetParent(transform, false);
                root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
                root.sizeDelta = new Vector2(160f, 50f);

                var label = new GameObject("Amount", typeof(RectTransform), typeof(Text), typeof(Outline)).GetComponent<Text>();
                label.transform.SetParent(root, false);
                label.font = UiTheme.Heavy;
                label.fontSize = 38;
                label.alignment = TextAnchor.MiddleRight;
                label.raycastTarget = false;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                var labelRect = label.rectTransform;
                labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0.5f);
                labelRect.pivot = new Vector2(1f, 0.5f);
                labelRect.anchoredPosition = new Vector2(IconPixels * 0.25f, 0f);
                labelRect.sizeDelta = new Vector2(120f, 50f);
                label.GetComponent<Outline>().effectColor = new Color(0.2f, 0.12f, 0.05f, 0.9f);

                var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                icon.transform.SetParent(root, false);
                icon.raycastTarget = false;
                icon.preserveAspect = true;
                var iconRect = icon.rectTransform;
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.anchoredPosition = new Vector2(IconPixels * 0.25f + IconPixels * 0.6f, 0f);
                iconRect.sizeDelta = new Vector2(IconPixels, IconPixels);

                root.gameObject.SetActive(false);
                _pool[i] = new Popup { Root = root, Label = label, Icon = icon, Age = Seconds };
            }
        }
    }
}
