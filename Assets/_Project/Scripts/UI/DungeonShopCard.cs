using CityBuilder.Core;
using CityBuilder.Dungeon;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The card of the dungeon's potion shop (the user, 2026-11-01): the four potions, each with
    /// its picture, what it does, how many are in the bag and a «Купить: N» button. Bought ones are
    /// not drunk: they go under the heroes' portraits (DungeonPotionBar). A button works only with
    /// a hero at the shop and the coins on the bar.
    /// </summary>
    public class DungeonShopCard : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text title;
        [SerializeField] private Text hint;
        [SerializeField] private Text[] names = new Text[DungeonPotions.Kinds];
        [SerializeField] private Text[] descriptions = new Text[DungeonPotions.Kinds];
        [SerializeField] private Button[] buyButtons = new Button[DungeonPotions.Kinds];

        public static DungeonShopCard Instance { get; private set; }

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        /// <summary>The shop the card is open on.</summary>
        public DungeonEntrance Shown { get; private set; }

        private Text[] _labels;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(DungeonEntrance shop)
        {
            if (shop == null || panelRoot == null) return;
            Shown = shop;
            panelRoot.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            Shown = null;
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        /// <summary>A «Купить» button: the potion of that kind (PotionKind as an index).</summary>
        public void Buy(int kind)
        {
            var potions = DungeonPotions.Instance;
            if (Shown == null || potions == null || !Shown.HeroNear() || kind < 0 || kind >= DungeonPotions.Kinds) return;
            potions.Buy((PotionKind)kind);
            Refresh();
        }

        private void Update()
        {
            if (IsOpen) Refresh();
        }

        private void Refresh()
        {
            if (Shown == null)
            {
                Close();
                return;
            }
            var potions = DungeonPotions.Instance;
            var near = Shown.HeroNear();
            if (title != null) title.text = Localization.Get("#dungeon_shop_title");
            if (hint != null) hint.text = Localization.Get(near ? "#dungeon_shop_hint" : "#dungeon_shop_far");
            _labels ??= new Text[buyButtons.Length];
            for (var i = 0; i < DungeonPotions.Kinds && i < buyButtons.Length; i++)
            {
                var kind = (PotionKind)i;
                var id = kind.ToString().ToLowerInvariant();
                if (i < names.Length && names[i] != null)
                    names[i].text = Localization.Get("#potion_" + id) + "  " + string.Format(Localization.Get("#dungeon_shop_have"), potions != null ? potions.Count(kind) : 0);
                if (i < descriptions.Length && descriptions[i] != null)
                {
                    var power = DungeonPotions.Power(kind);
                    // shares as percents, factors as «x1.5»
                    var shown = kind == PotionKind.Healing || kind == PotionKind.Defence ? Mathf.RoundToInt(power * 100f).ToString() : power.ToString("0.#");
                    descriptions[i].text = string.Format(Localization.Get("#potion_" + id + "_desc"), shown, Mathf.RoundToInt(DungeonPotions.Seconds(kind)));
                }
                var button = buyButtons[i];
                if (button == null) continue;
                button.interactable = near && potions != null && potions.CanBuy(kind);
                if (_labels[i] == null) _labels[i] = button.GetComponentInChildren<Text>();
                var word = string.Format(Localization.Get("#dungeon_shop_buy"), DungeonPotions.Price(kind));
                if (_labels[i] != null && _labels[i].text != word) _labels[i].text = word;
            }
        }
    }
}
