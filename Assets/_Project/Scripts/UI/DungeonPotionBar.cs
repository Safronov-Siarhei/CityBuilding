using CityBuilder.Dungeon;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The potions in the bag, under the heroes' portraits in the top-right corner (the user,
    /// 2026-11-01: «под иконкой героев справа в углу появятся их значки, при нажатии они будут
    /// активироваться»): one icon per kind the party carries, with its count; a tap drinks one
    /// (DungeonPotions.Drink). Only in a dungeon; kinds the bag has none of are not shown, and the
    /// rest close up to the right.
    /// </summary>
    public class DungeonPotionBar : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private Button[] buttons = new Button[DungeonPotions.Kinds];
        [SerializeField] private Text[] counts = new Text[DungeonPotions.Kinds];
        /// <summary>One icon's width plus the gap, and the gap under the portraits.</summary>
        [SerializeField] private float slot = 96f;
        [SerializeField] private float below = 14f;

        private DungeonPotions _bound;

        /// <summary>The kinds on the bar now -- for the tests.</summary>
        public static int Shown { get; private set; }

        public static DungeonPotionBar Instance { get; private set; }

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>An icon tapped: one potion of that kind drunk.</summary>
        public void Tap(int kind)
        {
            if (DungeonPotions.Instance != null) DungeonPotions.Instance.Drink((PotionKind)kind);
        }

        private void LateUpdate()
        {
            var potions = DungeonPotions.Instance;
            if (potions != _bound)
            {
                if (_bound != null) _bound.Changed -= Redraw;
                _bound = potions;
                if (_bound != null) _bound.Changed += Redraw;
                Redraw();
            }
            if (root == null || !root.gameObject.activeSelf) return;
            // under the portraits, wherever their stack ends
            var y = HeroPortraitController.ShownRows > 0 ? HeroPortraitController.StackBottom - below : root.anchoredPosition.y;
            if (!Mathf.Approximately(root.anchoredPosition.y, y)) root.anchoredPosition = new Vector2(root.anchoredPosition.x, y);
        }

        private void Redraw()
        {
            var potions = DungeonPotions.Instance;
            var on = potions != null && DungeonMode.IsActive;
            var shown = 0;
            // right to left in kind order from the corner: the first kind the bag has stands rightmost
            for (var i = 0; i < buttons.Length; i++)
            {
                var have = on ? potions.Count((PotionKind)i) : 0;
                var button = buttons[i];
                if (button == null) continue;
                button.gameObject.SetActive(have > 0);
                if (have <= 0) continue;
                ((RectTransform)button.transform).anchoredPosition = new Vector2(-slot * shown, 0f);
                if (i < counts.Length && counts[i] != null) counts[i].text = have.ToString();
                shown++;
            }
            Shown = shown;
            if (root != null) root.gameObject.SetActive(shown > 0);
        }
    }
}
