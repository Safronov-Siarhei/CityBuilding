using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// «+1» rising over a building as its worker brings the load in (the user, 2026-10-22: the
    /// player should see the sawmill and the quarry earning). Screen-space labels on the HUD canvas,
    /// a few made once and reused: each follows its building's point on screen, rises and fades over
    /// <see cref="Seconds"/>. No allocation per popup, nothing running while none is up.
    /// </summary>
    public class ResourceGainPopups : MonoBehaviour
    {
        private const int PoolSize = 10;
        private const float Seconds = 1.1f;
        private const float RisePixels = 70f;
        private const float HeightOverBuilding = 2.2f;
        private static readonly Color GainColor = new Color(1f, 0.93f, 0.55f);

        public static ResourceGainPopups Instance { get; private set; }

        private struct Popup
        {
            public Text Label;
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
            if (building != null) Show(building.transform.position, amount);
        }

        /// <summary>Shows «+amount» over `world` (a building's foot: the label stands over its roof).</summary>
        public void Show(Vector3 world, int amount)
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
            popup.World = world + Vector3.up * HeightOverBuilding;
            popup.Age = 0f;
            popup.Label.text = Localization.Format("#popup_gain", amount);
            popup.Label.gameObject.SetActive(true);
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
                    popup.Label.gameObject.SetActive(false);
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
            if (screen.z <= 0f || !RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screen, null, out var local))
            {
                popup.Label.color = Color.clear;
                return;
            }
            popup.Label.rectTransform.anchoredPosition = local + new Vector2(0f, RisePixels * t);
            var c = GainColor;
            c.a = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
            popup.Label.color = c;
        }

        private void EnsurePool()
        {
            if (_pool != null) return;
            _rect = (RectTransform)transform;
            _pool = new Popup[PoolSize];
            for (var i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("Gain", typeof(RectTransform), typeof(Text), typeof(Outline));
                go.transform.SetParent(transform, false);
                var label = go.GetComponent<Text>();
                label.font = UiTheme.Heavy;
                label.fontSize = 38;
                label.alignment = TextAnchor.MiddleCenter;
                label.raycastTarget = false;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                var rect = label.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(160f, 50f);
                go.GetComponent<Outline>().effectColor = new Color(0.2f, 0.12f, 0.05f, 0.9f);
                go.SetActive(false);
                _pool[i] = new Popup { Label = label, Age = Seconds };
            }
        }
    }
}
