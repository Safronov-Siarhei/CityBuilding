using System;
using CityBuilder.Core;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.Shop
{
    /// <summary>
    /// The same three offers in the sandbox's menu (the user, 2026-11-11): small cards to the right of
    /// the menu's entries with the bundle's name, goods and timer («Получено» once taken); a tap on
    /// any opens the shop. Hidden where the shop is off (a run, the tutorial, a dungeon).
    /// </summary>
    public class OfferMenuStrip : MonoBehaviour
    {
        [Serializable]
        public class Card
        {
            public Button button;
            public Text name;
            public Image[] goodIcons;
            public Text[] goodAmounts;
            public Text timer;
            [NonSerialized] public string shownId;
            [NonSerialized] public int shownSeconds = -1;
            [NonSerialized] public int shownTaken = -1;
        }

        [SerializeField] private GameObject content;
        [SerializeField] private Card[] cards = new Card[0];
        [SerializeField] private ResourceIconLibrary icons;
        [SerializeField] private Sprite crystal;

        private void Awake()
        {
            foreach (var card in cards) card.button.onClick.AddListener(OfferShopWindow.Show);
        }

        private void OnEnable()
        {
            foreach (var card in cards)
            {
                card.shownId = null;
                card.shownSeconds = -1;
                card.shownTaken = -1;
            }
        }

        private void Update()
        {
            var shop = OfferShop.Instance;
            var on = shop != null && shop.Rotation != null && OfferShop.Allowed;
            if (content.activeSelf != on) content.SetActive(on);
            if (!on) return;
            var now = shop.Now;
            for (var i = 0; i < cards.Length && i < OfferRotation.SlotCount; i++)
            {
                var card = cards[i];
                var slot = shop.Rotation.Slots[i];
                var bundle = slot.Bundle;
                card.button.gameObject.SetActive(bundle != null);
                if (bundle == null) continue;
                if (card.shownId != bundle.id)
                {
                    card.shownId = bundle.id;
                    card.shownTaken = -1;
                    card.name.text = Localization.Get("#bundle_" + bundle.id);
                    for (var g = 0; g < card.goodIcons.Length; g++)
                    {
                        var has = g < bundle.goods.Count;
                        card.goodIcons[g].gameObject.SetActive(has);
                        card.goodAmounts[g].gameObject.SetActive(has);
                        if (!has) continue;
                        var res = bundle.goods[g].resource;
                        card.goodIcons[g].sprite = res == "Crystals" ? crystal
                            : icons != null && Enum.TryParse<ResourceType>(res, out var type) ? icons.GetIcon(type) : null;
                        card.goodAmounts[g].text = bundle.goods[g].amount.ToString();
                    }
                }
                var taken = slot.Taken ? 1 : 0;
                var seconds = Mathf.Max(0, Mathf.CeilToInt((float)(slot.EndsAt - now)));
                if (taken != card.shownTaken || (!slot.Taken && seconds != card.shownSeconds))
                {
                    card.shownTaken = taken;
                    card.shownSeconds = seconds;
                    card.timer.text = slot.Taken ? Localization.Get("#shop_taken") : OfferShopWindow.Clock(seconds);
                }
            }
        }
    }
}
