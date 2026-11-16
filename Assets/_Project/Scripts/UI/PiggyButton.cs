using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The piggy bank's button (approved 2026-11-16, Design/store/Piggy_Button_Concept.webp): a square, the royal pig of the
    /// current level in its centre, four crystals bobbing round it, the crystals in it at its feet; full -- «Полна!» on the
    /// top edge, the number gold and the pig wobbling. A tap opens PiggyWindow. Put on a screen with Create.
    /// </summary>
    public class PiggyButton : MonoBehaviour
    {
        public const float Size = 200f;
        private const float PigWidth = 132f;
        private const float Feet = 152f;   // the pig's feet, from the square's top

        // (x, y from the square's top-left, height, phase, period) -- the approved four
        private static readonly float[,] Gems = { { 26, 44, 34, 0.0f, 1.9f }, { 174, 36, 30, 1.3f, 2.3f }, { 20, 126, 26, 2.4f, 2.1f }, { 182, 120, 28, 0.7f, 1.7f } };

        private RectTransform _pig;
        private Image _pigImage, _shine;
        private readonly RectTransform[] _gems = new RectTransform[4];
        private Text _count;
        private RectTransform _full;
        private int _shownLevel = -1;

        /// <summary>A piggy button under `parent`, anchored at `anchor` (and pivoted there), `offset` from it, `size` square.</summary>
        public static PiggyButton Create(Transform parent, Vector2 anchor, Vector2 offset, float size = Size)
        {
            var go = new GameObject("PiggyButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(Size, Size);
            rt.localScale = Vector3.one * (size / Size);
            var hit = go.GetComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);   // the whole square takes the tap
            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(PiggyWindow.Show);
            return go.AddComponent<PiggyButton>();
        }

        private static Vector2 At(float x, float y) => new Vector2(x - Size / 2f, Size / 2f - y);

        private void Start()
        {
            _shine = PiggyUi.Pic(transform, PiggyUi.Art + "Shine", At(100f, 95f), new Vector2(170f, 170f));
            for (var i = 0; i < 4; i++)
            {
                var g = PiggyUi.PicH(transform, PiggyUi.Crystal, At(Gems[i, 0], Gems[i, 1]), Gems[i, 2] * 1.25f);
                _gems[i] = g.rectTransform;
            }
            _pigImage = PiggyUi.Pic(transform, PiggyUi.Art + "Pig1", Vector2.zero, Vector2.one);
            _pig = _pigImage.rectTransform;
            _pig.pivot = new Vector2(0.5f, 0f);
            _count = PiggyUi.Stroke(PiggyUi.Label(transform, "", 34, PiggyUi.Cream, At(100f, 170f), new Vector2(200f, 44f)), 3f, PiggyUi.Ink);
            _full = PiggyUi.FullPill(transform, At(100f, 16f), 19);
            PiggyBank.Changed += Redraw;
            Redraw();
        }

        private void OnDestroy() => PiggyBank.Changed -= Redraw;

        private void Redraw()
        {
            if (_pig == null) return;
            var level = PiggyBank.Level;
            if (level != _shownLevel)
            {
                _shownLevel = level;
                var s = PiggyUi.Sprite(PiggyUi.Art + "Pig" + (level + 1));
                _pigImage.sprite = s;
                var h = s != null ? PigWidth * s.rect.height / Mathf.Max(1f, s.rect.width) : PigWidth;
                _pig.sizeDelta = new Vector2(PigWidth, h);
            }
            var full = PiggyBank.Full;
            _count.text = PiggyUi.Number(PiggyBank.Stored);
            _count.color = full ? PiggyUi.Gold : PiggyUi.Cream;
            _full.gameObject.SetActive(full);
            _shine.color = new Color(1f, 1f, 1f, full ? 0.75f : 0.45f);
        }

        private void Update()
        {
            if (_pig == null) return;
            var t = Time.unscaledTime;
            for (var i = 0; i < 4; i++)
            {
                PiggyUi.Drift(t, Gems[i, 3], Gems[i, 4], 4f, out var dx, out var dy, out var tilt);
                _gems[i].anchoredPosition = At(Gems[i, 0] + dx, Gems[i, 1] + dy);
                _gems[i].localRotation = Quaternion.Euler(0f, 0f, -tilt);
            }
            var full = PiggyBank.Full;
            float wx, rot;
            if (full) PiggyUi.Wobble(t, 1.8f, 0.9f, 2f, 3f, out wx, out rot);
            else PiggyUi.Wobble(t, 2.6f, 0.8f, 1f, 1.5f, out wx, out rot);
            _pig.anchoredPosition = At(100f + wx, Feet);
            _pig.localRotation = Quaternion.Euler(0f, 0f, rot);
            if (full)
            {
                var pulse = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * t / 0.9f);
                _count.color = Color.Lerp(PiggyUi.Gold, Color.white, pulse * 0.35f);
            }
        }
    }
}
