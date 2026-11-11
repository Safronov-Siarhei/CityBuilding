using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The game's rule for an amount of crystals (the user, 2026-11-11: «количество кристаллов слева, а
    /// иконка справа, это правило для всей игры»): the number LEFT, the crystal RIGHT. Other resources
    /// keep icon-then-number. For a pair that shows either (a shop bundle's goods): lays an icon and its
    /// amount out in the same span, in the order the good asks for. Idempotent: run it on every redraw.
    /// </summary>
    public static class CrystalOrder
    {
        private const float Gap = 8f;

        public static void Lay(Image icon, Text amount, bool crystal)
        {
            if (icon == null || amount == null) return;
            var i = icon.rectTransform;
            var a = amount.rectTransform;
            var wi = i.sizeDelta.x;
            var wa = a.sizeDelta.x;
            var left = Mathf.Min(i.anchoredPosition.x - wi * 0.5f, a.anchoredPosition.x - wa * 0.5f);
            if (crystal)
            {
                a.anchoredPosition = new Vector2(left + wa * 0.5f, a.anchoredPosition.y);
                i.anchoredPosition = new Vector2(left + wa + Gap + wi * 0.5f, i.anchoredPosition.y);
                amount.alignment = TextAnchor.MiddleRight;
            }
            else
            {
                i.anchoredPosition = new Vector2(left + wi * 0.5f, i.anchoredPosition.y);
                a.anchoredPosition = new Vector2(left + wi + Gap + wa * 0.5f, a.anchoredPosition.y);
                amount.alignment = TextAnchor.MiddleLeft;
            }
        }
    }
}
