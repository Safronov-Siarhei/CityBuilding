using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// «Запас здания» (the user, 2026-11-17; card resources-and-storage.md): a sandbox store held (Склад, Кладовая,
    /// Сокровищница or a Big one) gets a round sack button beside its «!»; a tap opens this card -- what lies in THIS
    /// store, each resource it keeps with its icon and «amount/room» (full blinks red). Read only.
    /// The slots are made by SetupProject (BuildStoreInventory); unused ones hide.
    /// </summary>
    public class StoreInventoryCard : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text title;
        [SerializeField] private Image[] icons = new Image[0];
        [SerializeField] private Text[] amounts = new Text[0];
        [SerializeField] private ResourceIconLibrary iconLibrary;
        /// <summary>The sack beside the «!» (shown while a store of the player's is held).</summary>
        [SerializeField] private GameObject button;

        private static readonly Color Ink = new Color(0.27f, 0.16f, 0.07f, 1f);
        private static readonly ResourceType[] AllTypes = (ResourceType[])System.Enum.GetValues(typeof(ResourceType));

        public static StoreInventoryCard Instance { get; private set; }

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private BuildingInstance _store;
        private readonly ResourceType[] _shown = new ResourceType[16];
        private int _count;
        private float _refresh;

        // the first slot's own places, for a store of one resource that moves it to the middle and back
        private Vector2 _iconAt, _amountAt;

        private void Awake()
        {
            Instance = this;
            if (icons.Length > 0 && icons[0] != null) _iconAt = icons[0].rectTransform.anchoredPosition;
            if (amounts.Length > 0 && amounts[0] != null) _amountAt = amounts[0].rectTransform.anchoredPosition;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private static BuildingInstance Target
        {
            get
            {
                if (!StoreLedger.Active || Tutorial.TutorialMode.IsActive) return null;
                var focus = BuildingFocus.Instance != null && BuildingFocus.Instance.ButtonsShown ? BuildingFocus.Instance.Current : null;
                return focus != null && !focus.IsBot && focus.Data != null && StoreLedger.IsStore(focus.Data) ? focus : null;
            }
        }

        /// <summary>The sack (a persistent listener).</summary>
        public void Tap()
        {
            var target = Target;
            if (target != null) Show(target);
        }

        public void Show(BuildingInstance store)
        {
            if (store == null || panelRoot == null) return;
            _store = store;
            _count = 0;
            foreach (var type in AllTypes)
                if (_count < _shown.Length && StoreLedger.CapOf(store.Data.buildingName, type, 1) > 0) _shown[_count++] = type;
            panelRoot.SetActive(true);
            ModalGate.SetBlocked(true);
            Refresh();
        }

        public void Close()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            _store = null;
            ModalGate.SetBlocked(false);
        }

        private void Refresh()
        {
            if (_store == null) return;
            if (title != null) title.text = Localization.Format("#store_card_title", _store.Data.LocalizedName, _store.Level);
            var pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
            var red = Color.Lerp(new Color(0.75f, 0.08f, 0.04f), new Color(1f, 0.35f, 0.25f), pulse);
            for (var i = 0; i < icons.Length; i++)
            {
                var on = i < _count;
                if (icons[i] != null)
                {
                    icons[i].gameObject.SetActive(on);
                    if (on && iconLibrary != null) icons[i].sprite = iconLibrary.GetIcon(_shown[i]);
                }
                if (i >= amounts.Length || amounts[i] == null) continue;
                amounts[i].gameObject.SetActive(on);
                if (!on) continue;
                var have = StoreLedger.AmountIn(_store, _shown[i]);
                var room = StoreLedger.CapOf(_store, _shown[i]);
                amounts[i].text = have + "/" + room;
                amounts[i].color = room > 0 && have >= room ? red : Ink;
            }
            Centre();
        }

        /// <summary>A store of ONE resource (Кладовая, Сокровищница): its icon and number in the card's middle (the user,
        /// 2026-11-17); more than one keep the grid.</summary>
        private void Centre()
        {
            if (icons.Length == 0 || amounts.Length == 0 || icons[0] == null || amounts[0] == null) return;
            var icon = icons[0].rectTransform;
            var amount = amounts[0].rectTransform;
            if (_count != 1)
            {
                icon.anchoredPosition = _iconAt;
                amount.anchoredPosition = _amountAt;
                return;
            }
            const float gap = 12f;
            var iconW = icon.sizeDelta.x;
            var textW = Mathf.Min(amounts[0].preferredWidth, amount.sizeDelta.x);
            var left = -(iconW + gap + textW) * 0.5f;
            icon.anchoredPosition = new Vector2(left + iconW * 0.5f, 0f);
            // the number is left-aligned in its box: its box's left edge right after the icon
            amount.anchoredPosition = new Vector2(left + iconW + gap + amount.sizeDelta.x * amount.pivot.x, 0f);
        }

        private void Update()
        {
            var show = Target != null && !IsOpen;
            if (button != null && button.activeSelf != show) button.SetActive(show);
            if (!IsOpen) return;
            if (_store == null) { Close(); return; }
            _refresh -= Time.unscaledDeltaTime;
            if (_refresh > 0f) return;
            _refresh = 0.1f;
            Refresh();
        }
    }
}
