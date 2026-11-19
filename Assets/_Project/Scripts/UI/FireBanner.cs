using CityBuilder.Core;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// «Пожар!» (the user, 2026-11-14; art Tools/ui/fire_banner_art.py, approved): once fire has destroyed
    /// fire_banner_share of the player's buildings within fire_banner_window_sec (Buildings/FireManager),
    /// the game pauses and this offers to put EVERY fire out for an ad or fire_end_crystals; then no fire
    /// starts for fire_shield_sec.
    /// </summary>
    public class FireBanner : PauseBanner
    {
        public static FireBanner Instance { get; private set; }

        protected override void Awake()
        {
            Instance = this;
            base.Awake();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        protected override string Stem => "fire_banner";

        protected override int Price => BalanceConfig.Instance.FireEndCrystals;

        // the compensation plaque (the user approved Design/ui/Fire_Banner_Comp.png, 2026-11-19), built by
        // SetupProjectPanels.BuildDisasterBanner, filled here: «+ на отстройку:» on its own line, then the resources
        // (number + icon) PerRow to a row, wrapping; the words above move up while it shows
        [SerializeField] private RectTransform compPlaque;
        [SerializeField] private Text compLabel;
        [SerializeField] private ResourceIconLibrary iconLibrary;
        [SerializeField] private RectTransform bodyRect;
        [SerializeField] private Vector2 bodyPosWithComp;
        [SerializeField] private Vector2 bodySizeWithComp;

        private const int PerRow = 4;
        private const float LabelHeight = 40f, RowHeight = 38f, Icon = 32f, NumberSize = 28f, GapIn = 4f, GapOut = 18f, Pad = 24f;

        private Vector2 _bodyPos, _bodySize;
        private bool _bodyKept;
        private readonly System.Collections.Generic.List<GameObject> _items = new System.Collections.Generic.List<GameObject>();

        protected override void OnShown()
        {
            if (compPlaque == null) return;
            if (bodyRect != null && !_bodyKept)
            {
                _bodyPos = bodyRect.anchoredPosition;
                _bodySize = bodyRect.sizeDelta;
                _bodyKept = true;
            }
            foreach (var go in _items) if (go != null) Destroy(go);
            _items.Clear();
            var fires = Buildings.FireManager.Instance;
            var comp = fires != null ? fires.Compensation() : null;
            var any = comp != null && comp.Count > 0;
            compPlaque.gameObject.SetActive(any);
            if (bodyRect != null)
            {
                bodyRect.anchoredPosition = any ? bodyPosWithComp : _bodyPos;
                bodyRect.sizeDelta = any ? bodySizeWithComp : _bodySize;
            }
            if (!any) return;

            if (compLabel != null) compLabel.text = Localization.Format("#fire_banner_comp", string.Empty).Trim();
            var rows = (comp.Count + PerRow - 1) / PerRow;
            var font = UiTheme.Heavy;
            // each item's width: its number (measured) + the icon
            var widths = new float[comp.Count];
            for (var k = 0; k < comp.Count; k++) widths[k] = NumberWidth(comp[k].amount.ToString(), font) + GapIn + Icon * 1.1f;
            var widest = compLabel != null ? compLabel.preferredWidth : 0f;
            for (var r = 0; r < rows; r++) widest = Mathf.Max(widest, RowWidth(widths, r));
            var height = LabelHeight + RowHeight * rows + 6f;
            var top = compPlaque.anchoredPosition.y + compPlaque.sizeDelta.y * 0.5f;   // the plaque grows down from its top
            compPlaque.sizeDelta = new Vector2(widest + Pad * 2f, height);
            compPlaque.anchoredPosition = new Vector2(compPlaque.anchoredPosition.x, top - height * 0.5f);
            if (compLabel != null) compLabel.rectTransform.anchoredPosition = new Vector2(0f, height * 0.5f - LabelHeight * 0.5f - 2f);

            for (var r = 0; r < rows; r++)
            {
                var y = height * 0.5f - LabelHeight - RowHeight * (r + 0.5f);
                var x = -RowWidth(widths, r) * 0.5f;
                for (var k = r * PerRow; k < Mathf.Min(comp.Count, (r + 1) * PerRow); k++)
                {
                    var n = comp[k].amount.ToString();
                    var numW = NumberWidth(n, font);
                    var text = new GameObject("Amount", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                    text.transform.SetParent(compPlaque, false);
                    text.font = font;
                    text.fontSize = Mathf.RoundToInt(NumberSize);
                    text.color = new Color(0.27f, 0.19f, 0.09f);
                    text.alignment = TextAnchor.MiddleLeft;
                    text.horizontalOverflow = HorizontalWrapMode.Overflow;
                    text.raycastTarget = false;
                    text.text = n;
                    text.rectTransform.sizeDelta = new Vector2(numW + 4f, RowHeight);
                    text.rectTransform.anchoredPosition = new Vector2(x + numW * 0.5f, y);
                    _items.Add(text.gameObject);
                    x += numW + GapIn;
                    var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    icon.transform.SetParent(compPlaque, false);
                    icon.sprite = iconLibrary != null ? iconLibrary.GetIcon(comp[k].type) : null;
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;
                    icon.rectTransform.sizeDelta = new Vector2(Icon * 1.1f, Icon);
                    icon.rectTransform.anchoredPosition = new Vector2(x + Icon * 0.55f, y);
                    _items.Add(icon.gameObject);
                    x += Icon * 1.1f + GapOut;
                }
            }
        }

        private static float RowWidth(float[] widths, int row)
        {
            var w = 0f;
            var count = 0;
            for (var k = row * PerRow; k < Mathf.Min(widths.Length, (row + 1) * PerRow); k++, count++) w += widths[k];
            return w + GapOut * Mathf.Max(0, count - 1);
        }

        private static float NumberWidth(string n, Font font)
        {
            var settings = new TextGenerationSettings
            {
                font = font, fontSize = Mathf.RoundToInt(NumberSize), scaleFactor = 1f, horizontalOverflow = HorizontalWrapMode.Overflow,
                verticalOverflow = VerticalWrapMode.Overflow, generateOutOfBounds = true, pivot = new Vector2(0.5f, 0.5f),
                richText = false, color = Color.white,
            };
            return new TextGenerator().GetPreferredWidth(n, settings);
        }

        protected override void EndIt(int island) => Buildings.FireManager.Instance?.EndByBanner();
    }
}
