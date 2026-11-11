using System.Collections;
using System.Collections.Generic;
using CityBuilder.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.Shop
{
    /// <summary>
    /// «You got it» for a bundle taken in the shop (the user, 2026-11-11): each good's icons fly out of
    /// its strip to where the good went, and that place is shown IN the shop window while they fly --
    /// the top bar's chip for wood, stone, coins and food (`Icon_&lt;type&gt;` under ResourceHUD), the
    /// sack (`InventoryButton`) for everything else, the window's own crystal counter for crystals.
    /// The target bounces as they land, a «+N» rises by it, a «+N» also rises over the good in the
    /// strip. The HUD behind the window is hidden, so a copy of the target's picture stands in at its
    /// screen place and fades after. Rare (a purchase), so it simply makes and destroys its pieces.
    /// </summary>
    public class OfferRewardFlight : MonoBehaviour
    {
        private const int IconsPerGood = 6;
        private const float FlySeconds = 0.6f;
        private const float StaggerSeconds = 0.06f;
        private const float IconSize = 60f;
        private static readonly Color PlusInk = new Color(1f, 0.97f, 0.86f);
        private static readonly Color PlusRim = new Color(0.33f, 0.2f, 0.09f);

        private RectTransform _root;

        private void Awake()
        {
            _root = (RectTransform)transform;
        }

        /// <summary>One good: `amount` of `resource` from `from` (a strip's icon) to its place; `counter` is the window's crystal counter.</summary>
        public void Fly(Sprite icon, string resource, int amount, RectTransform from, RectTransform counter, float delay)
        {
            StartCoroutine(Run(icon, resource, amount, from, counter, delay));
        }

        private IEnumerator Run(Sprite icon, string resource, int amount, RectTransform from, RectTransform counter, float delay)
        {
            var start = LocalCentre(from);
            Plus(start + new Vector2(0f, 40f), amount, 46);
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

            RectTransform target;
            Image ghost = null;
            if (resource == "Crystals") target = counter;
            else
            {
                var hud = Find(resource);
                ghost = hud != null ? Ghost(hud) : null;
                target = ghost != null ? ghost.rectTransform : counter;
            }
            var end = LocalCentre(target);

            var flyers = new List<RectTransform>();
            for (var i = 0; i < IconsPerGood; i++)
            {
                var image = new GameObject("Flyer", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                image.transform.SetParent(_root, false);
                image.sprite = icon;
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.rectTransform.sizeDelta = new Vector2(IconSize, IconSize);
                image.rectTransform.anchoredPosition = start;
                image.gameObject.SetActive(false);
                flyers.Add(image.rectTransform);
            }

            var total = FlySeconds + StaggerSeconds * (IconsPerGood - 1);
            var landed = 0;
            var spread = new Vector2[IconsPerGood];
            for (var i = 0; i < IconsPerGood; i++) spread[i] = Random.insideUnitCircle * 50f;
            var targetScale = target.localScale;
            for (var t = 0f; t < total + 0.25f; t += Time.unscaledDeltaTime)
            {
                if (ghost != null) SetAlpha(ghost, Mathf.Clamp01(t / 0.15f));
                for (var i = 0; i < flyers.Count; i++)
                {
                    var f = flyers[i];
                    if (f == null) continue;
                    var k = (t - i * StaggerSeconds) / FlySeconds;
                    if (k < 0f) continue;
                    if (k >= 1f)
                    {
                        Destroy(f.gameObject);
                        flyers[i] = null;
                        if (landed++ == 0) Plus(end + new Vector2(0f, 46f), amount, 40);
                        continue;
                    }
                    f.gameObject.SetActive(true);
                    // out with a little spread, then a high arc into the target, shrinking as it lands
                    var a = start + spread[i] * Mathf.Clamp01(k * 3f);
                    var c = (a + end) * 0.5f + new Vector2(0f, 220f);
                    var e = k * k;
                    f.anchoredPosition = (1 - e) * (1 - e) * a + 2 * (1 - e) * e * c + e * e * end;
                    var s = Mathf.Lerp(1.1f, 0.6f, k);
                    f.localScale = new Vector3(s, s, 1f);
                }
                // the target punches each time one lands
                var punch = landed > 0 ? 1f + 0.18f * Mathf.Abs(Mathf.Sin((t - FlySeconds) * 18f)) * Mathf.Clamp01(1f - (t - total) / 0.25f) : 1f;
                target.localScale = targetScale * punch;
                yield return null;
            }
            target.localScale = targetScale;
            foreach (var f in flyers)
                if (f != null) Destroy(f.gameObject);
            if (ghost == null) yield break;
            for (var t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                SetAlpha(ghost, 1f - t / 0.5f);
                yield return null;
            }
            Destroy(ghost.gameObject);
        }

        /// <summary>Where the good lands on the hidden HUD: its top-bar chip if it has one on screen, else the sack.</summary>
        private static RectTransform Find(string resource)
        {
            var chip = GameObject.Find("Icon_" + resource);
            if (chip != null && chip.activeInHierarchy) return (RectTransform)chip.transform;
            var sack = GameObject.Find("InventoryButton");
            return sack != null ? (RectTransform)sack.transform : null;
        }

        /// <summary>A copy of a HUD picture (and its glyph, for the sack) at its screen place, in this window.</summary>
        private Image Ghost(RectTransform hud)
        {
            var source = hud.GetComponent<Image>();
            if (source == null) return null;
            var ghost = Copy(source, _root);
            var corners = new Vector3[4];
            hud.GetWorldCorners(corners);
            var min = ToLocal(corners[0]);
            var max = ToLocal(corners[2]);
            var rect = ghost.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = (min + max) * 0.5f;
            rect.sizeDelta = max - min;
            var glyph = hud.Find("Icon");
            if (glyph != null && glyph.GetComponent<Image>() is Image g)
            {
                var copy = Copy(g, rect);
                var gr = (RectTransform)glyph;
                copy.rectTransform.anchorMin = gr.anchorMin;
                copy.rectTransform.anchorMax = gr.anchorMax;
                copy.rectTransform.pivot = gr.pivot;
                copy.rectTransform.anchoredPosition = gr.anchoredPosition;
                copy.rectTransform.sizeDelta = gr.sizeDelta;
            }
            SetAlpha(ghost, 0f);
            return ghost;
        }

        private static Image Copy(Image source, Transform parent)
        {
            var image = new GameObject(source.name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.sprite = source.sprite;
            image.type = source.type;
            image.preserveAspect = source.preserveAspect;
            image.color = source.color;
            image.material = source.material;
            image.raycastTarget = false;
            return image;
        }

        private static void SetAlpha(Image image, float alpha)
        {
            foreach (var g in image.GetComponentsInChildren<Graphic>())
            {
                var c = g.color;
                c.a = alpha;
                g.color = c;
            }
        }

        private void Plus(Vector2 at, int amount, int size)
        {
            var text = new GameObject("Plus", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(_root, false);
            text.font = UiTheme.Heavy;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.color = PlusInk;
            text.text = "+" + amount;
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = PlusRim;
            outline.effectDistance = new Vector2(3f, -3f);
            text.rectTransform.sizeDelta = new Vector2(240f, 70f);
            text.rectTransform.anchoredPosition = at;
            StartCoroutine(Rise(text));
        }

        private static IEnumerator Rise(Text text)
        {
            var from = text.rectTransform.anchoredPosition;
            for (var t = 0f; t < 1f; t += Time.unscaledDeltaTime)
            {
                if (text == null) yield break;
                text.rectTransform.anchoredPosition = from + new Vector2(0f, 70f * t);
                var c = text.color;
                c.a = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
                text.color = c;
                yield return null;
            }
            if (text != null) Destroy(text.gameObject);
        }

        /// <summary>A rect's centre in this (the window canvas's root) rect's local space.</summary>
        private Vector2 LocalCentre(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return ToLocal((corners[0] + corners[2]) * 0.5f);
        }

        /// <summary>Overlay canvases: a world point is a screen point.</summary>
        private Vector2 ToLocal(Vector3 world)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, world, null, out var local);
            return local;
        }
    }
}
