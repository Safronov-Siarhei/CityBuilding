using System;
using System.Collections.Generic;
using CityBuilder.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.Shop
{
    /// <summary>
    /// The running boosters on the HUD (the user approved, 2026-11-12; mock-up Design/store/Booster_Hud.png):
    /// a row of round chips under the resource bar, where «День» was (the day went to the minimap's foot,
    /// DayOnMinimap) -- each the booster's picture in a gold ring that empties as its time runs out, the
    /// time left under it. A booster just switched on pops in. A tap opens «Запасы» with its window up.
    /// The sandbox only (SandboxOnly on the row). Built by SetupProjectOfferShop.BuildBoosterHud.
    /// </summary>
    public class BoosterHud : MonoBehaviour
    {
        [Serializable]
        public class Chip
        {
            public Button button;
            public Image ring;
            public Image icon;
            public Text time;
            [NonSerialized] public string id;
            [NonSerialized] public int shownSeconds = -1;
            [NonSerialized] public float popT = 1f;
        }

        private const float PopSeconds = 0.35f;

        [SerializeField] private Chip[] chips = new Chip[0];
        [SerializeField] private float step = 110f;

        private readonly List<string> _running = new List<string>();
        private readonly Dictionary<string, float> _full = new Dictionary<string, float>();
        private bool _dirty = true;

        private void Awake()
        {
            foreach (var chip in chips)
            {
                var c = chip;
                c.button.onClick.AddListener(() =>
                {
                    if (c.id != null && InventoryPanelController.Instance != null) InventoryPanelController.Instance.OpenBooster(c.id);
                });
                c.button.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            Boosters.Changed += MarkDirty;
            Boosters.Used += OnUsed;
            _dirty = true;
        }

        private void OnDisable()
        {
            Boosters.Changed -= MarkDirty;
            Boosters.Used -= OnUsed;
        }

        private void MarkDirty() => _dirty = true;

        private void OnUsed(string id)
        {
            _dirty = true;
            // «Продлить»: the ring fills up again from the new total
            _full.Remove(id);
            Rebuild();
            foreach (var chip in chips)
                if (chip.id == id) chip.popT = 0f;
        }

        private void Rebuild()
        {
            _dirty = false;
            _running.Clear();
            if (Boosters.Here)
                foreach (var def in Boosters.All)
                    if (def.durationSeconds > 0f && Boosters.Left(def.id) > 0f) _running.Add(def.id);
            for (var i = 0; i < chips.Length; i++)
            {
                var chip = chips[i];
                var has = i < _running.Count;
                chip.button.gameObject.SetActive(has);
                if (!has)
                {
                    chip.id = null;
                    continue;
                }
                if (chip.id != _running[i]) chip.icon.sprite = OfferShopWindow.LoadBoosterIcon(_running[i]);
                chip.id = _running[i];
                chip.shownSeconds = -1;
                ((RectTransform)chip.button.transform).anchoredPosition = new Vector2((i - (_running.Count - 1) * 0.5f) * step, 0f);
            }
        }

        private void Update()
        {
            if (_dirty) Rebuild();
            var dt = Time.unscaledDeltaTime;
            foreach (var chip in chips)
            {
                if (chip.id == null) continue;
                var left = Boosters.Left(chip.id);
                if (!_full.TryGetValue(chip.id, out var full) || left > full) _full[chip.id] = full = left;
                chip.ring.fillAmount = full > 0f ? Mathf.Clamp01(left / full) : 0f;
                var seconds = Mathf.CeilToInt(left);
                if (seconds != chip.shownSeconds)
                {
                    chip.shownSeconds = seconds;
                    chip.time.text = OfferShopWindow.Clock(seconds);
                }
                if (chip.popT < PopSeconds)
                {
                    chip.popT += dt;
                    var k = Mathf.Clamp01(chip.popT / PopSeconds);
                    var s = 1f + 0.45f * Mathf.Sin(k * Mathf.PI) * (1f - k * 0.5f);
                    chip.button.transform.localScale = new Vector3(s, s, 1f);
                }
                else if (chip.button.transform.localScale.x != 1f) chip.button.transform.localScale = Vector3.one;
            }
        }
    }
}
