using System;
using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The campaign's «Поддержи разработку» banner (the user, 2026-11-15; card Design/features/monetization.md, art v2
    /// approved, mockup Design/store/SupportBanner_v2_Mockup.png). The campaign has no ads and no paywall: after a WON
    /// level -- every one from support_banner_from_level on (the 2nd; never after the first) -- the frame of the plague
    /// banner with a field of crystals, Ардан left and the mage right, and on the parchment the three crystal packs at
    /// their sale prices (the same products as «Подожди!»). A purchase thanks the player in the two lines; «Продолжить»
    /// closes it and goes on. Built in code; its pictures and their places come from Tools/store/support_banner_v2_mockup.py
    /// (Resources/Shop/Support/*.png + layout.json, rects in the 1920 x 1080 reference, y down); the words, prices and
    /// buttons are written here. The campaign calls ShowAfterWin(level number, what comes next) after its victory screen.
    /// </summary>
    public class SupportOfferWindow : MonoBehaviour
    {
        private const string Art = "Shop/Support/";
        private const string ShopArt = "Shop/MenuShop/";
        private const int Order = 960;            // over the HUD and the victory screen

        private static readonly Color Ink = new Color(0.29f, 0.17f, 0.086f);
        private static readonly Color Cream = new Color(0.98f, 0.925f, 0.784f);
        private static readonly Color Brown = new Color(0.27f, 0.2f, 0.094f);
        private static readonly Color DarkRed = new Color(0.59f, 0.16f, 0.12f);
        private static readonly Color Faded = new Color(0.47f, 0.34f, 0.16f);
        private static readonly Color Lilac = new Color(0.88f, 0.9f, 0.94f);
        private static readonly Color Ribbon = new Color(0.19f, 0.055f, 0.29f);

        private static SupportOfferWindow _current;

        public static bool IsOpen => _current != null;

        [Serializable] private class Rect4 { public string name; public int[] rect; }
        [Serializable] private class Layout { public int[] reference; public Rect4[] pieces; public Rect4[] texts; }

        private Action _onDone;
        private RectTransform _root;
        private Layout _layout;
        private Text _line1, _line2;
        private readonly List<RectTransform> _rows = new List<RectTransform>();
        private float _timeScaleBefore = 1f;
        private bool _gateBefore;
        private float _lift;                                         // the window's middle to the screen's (layout px)
        private readonly List<Canvas> _hidden = new List<Canvas>();  // the HUD behind, off while it shows

        /// <summary>
        /// After the campaign's level `level` (1-based) is won: the banner when it is due, then onDone; else onDone at
        /// once. Never after the first level (support_banner_from_level).
        /// </summary>
        public static void ShowAfterWin(int level, Action onDone)
        {
            var config = BalanceConfig.Instance;
            if (_current != null || config == null || level < config.SupportBannerFromLevel || !AnyOffer(config))
            {
                onDone?.Invoke();
                return;
            }
            var go = new GameObject("SupportOfferWindow", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = Order;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            _current = go.AddComponent<SupportOfferWindow>();
            _current._onDone = onDone;
        }

        private static bool AnyOffer(BalanceConfig config)
        {
            for (var p = 0; p < config.MenuShopPackCount; p++)
                if (config.MenuShopPackSaleOffPercent(p) > 0) return true;
            return false;
        }

        private void Start()
        {
            _root = (RectTransform)transform;
            _timeScaleBefore = Time.timeScale;
            Time.timeScale = 0f;
            _gateBefore = ModalGate.IsBlocked;
            ModalGate.SetBlocked(true);
            var json = UnityEngine.Resources.Load<TextAsset>(Art + "layout");
            _layout = json != null ? JsonUtility.FromJson<Layout>(json.text) : null;
            if (_layout == null || _layout.pieces == null)
            {
                Debug.LogWarning("SupportOfferWindow: no Resources/Shop/Support/layout.json -- run Tools/store/support_banner_v2_mockup.py.");
                Continue();
                return;
            }
            // the mock-up's window sits low (its ribbon reaches up, its foot is the frame's): its middle on the
            // screen's (the user, 2026-11-16: «подними окно ... чтобы оно было по центру»)
            foreach (var r in _layout.pieces)
                if (r.name == "Window" && r.rect != null && r.rect.Length >= 4)
                {
                    var h = _layout.reference != null && _layout.reference.Length > 1 ? _layout.reference[1] : 1080f;
                    _lift = (r.rect[1] + r.rect[3]) / 2f - h / 2f;
                }
            // nothing of the game's interface behind it (the user, the same day: «скрывай задний интерфейс»)
            var own = GetComponent<Canvas>();
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (c == null || c == own || !c.isRootCanvas || !c.enabled) continue;
                c.enabled = false;
                _hidden.Add(c);
            }
            Store.Changed += RedrawRows;
            Build();
        }

        private void Build()
        {
            var dim = Img<Image>(_root, "Dim");
            dim.rectTransform.anchorMin = Vector2.zero;
            dim.rectTransform.anchorMax = Vector2.one;
            dim.rectTransform.offsetMin = dim.rectTransform.offsetMax = Vector2.zero;
            dim.color = new Color(0.055f, 0.024f, 0.12f, 0.78f);
            dim.raycastTarget = true;   // nothing behind takes a tap

            var window = Piece("Window");
            if (window != null) window.raycastTarget = true;
            var title = Words("Title", Localization.Get("#support_title"), 66, Cream, true);
            Stroke(title, 4f, Ribbon);
            _line1 = Words("Line1", Localization.Get("#support_line1"), 30, Brown, false);
            _line2 = Words("Line2", Localization.Get("#support_line2"), 32, DarkRed, true);
            RedrawRows();
            var go = Words("Continue", Localization.Get("#support_continue"), 44, Faded, true);
            go.raycastTarget = true;
            go.gameObject.AddComponent<Button>().onClick.AddListener(Continue);
        }

        /// <summary>The three packs at their sale prices (again after a purchase: a pending payment greys its button).</summary>
        private void RedrawRows()
        {
            foreach (var r in _rows) if (r != null) Destroy(r.gameObject);
            _rows.Clear();
            var config = BalanceConfig.Instance;
            if (config == null) return;
            for (var p = 0; p < Mathf.Min(3, config.MenuShopPackCount); p++)
            {
                var off = config.MenuShopPackSaleOffPercent(p);
                if (off <= 0) continue;
                var bg = Piece("Row" + (p + 1));
                if (bg == null) continue;
                var row = bg.rectTransform;
                _rows.Add(row);
                // the places in the row as the mock-up draws them (the row's centre is 0,0; its box 762 x 118)
                var name = Label(row, Localization.Get("#shop_pack_" + (p + 1)), 28, Cream, new Vector2(-181f + 300f, 25f), new Vector2(600f, 44f), true);
                name.alignment = TextAnchor.MiddleLeft;
                Stroke(name, 2f, Ink);
                Amount(row, Store.CrystalsOf(Store.PackSaleId(p)), 44, new Vector2(-181f, -21f));
                var old = Label(row, Store.PriceText(Store.PackId(p)), 28, Lilac, new Vector2(269f, 35f), new Vector2(200f, 40f), true);
                Stroke(old, 2f, Ink);
                var strike = Icon(row, ShopArt + "White", new Vector2(269f, 35f), 8f);
                strike.rectTransform.sizeDelta = new Vector2(old.preferredWidth + 12f, 5f);
                strike.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 6f);
                strike.color = new Color(0.92f, 0.2f, 0.16f);
                var sticker = Icon(row, ShopArt + "Sticker", new Vector2(119f, -21f), 80f);
                sticker.color = new Color(0.86f, 0.24f, 0.2f);
                Stroke(Label(sticker.transform, Localization.Format("#shop_off", off), 26, Cream, new Vector2(0f, -1f), new Vector2(96f, 48f), true), 2f, Ink);
                PriceButton(row, new Vector2(269f, -14f), new Vector2(190f, 70f), Store.PackSaleId(p));
            }
        }

        private void PriceButton(Transform parent, Vector2 at, Vector2 size, string product)
        {
            var image = Icon(parent, ShopArt + "ButtonGreen", at, size.x);
            image.rectTransform.sizeDelta = size;
            image.raycastTarget = true;
            var pending = Store.IsPending(product);
            var text = Label(image.transform, pending ? Localization.Get("#shop_pending") : Store.PriceText(product),
                pending ? 22 : 36, Cream, new Vector2(0f, 3f), size - new Vector2(16f, 8f), true);
            Stroke(text, 2f, Ink);
            var button = image.gameObject.AddComponent<Button>();
            button.interactable = !pending;
            if (pending) image.color = new Color(0.7f, 0.7f, 0.7f);
            button.onClick.AddListener(() => Buy(product));
        }

        private void Buy(string product)
        {
            if (!Store.Buy(product)) return;
            // thanks in the two lines; the packs stay (a player may want another)
            _line1.text = Localization.Get("#support_thanks");
            _line2.text = Localization.Format("#support_got", Store.CrystalsOf(product));
        }

        /// <summary>«Продолжить» (and Android's back key through BackCurrent): out, on with the campaign.</summary>
        public void Continue()
        {
            var next = _onDone;
            _onDone = null;
            Destroy(gameObject);
            next?.Invoke();
        }

        public static void BackCurrent()
        {
            if (_current != null) _current.Continue();
        }

        private void OnDestroy()
        {
            Store.Changed -= RedrawRows;
            foreach (var c in _hidden) if (c != null) c.enabled = true;
            Time.timeScale = _timeScaleBefore;
            ModalGate.SetBlocked(_gateBefore);
            if (_current == this) _current = null;
        }

        // ------------------------------------------------------------ building blocks

        /// <summary>A layout rect (1920 x 1080, y down) as a centre in the canvas (y up) and a size.</summary>
        private bool Place(Rect4[] list, string name, out Vector2 centre, out Vector2 size)
        {
            centre = size = Vector2.zero;
            if (list == null) return false;
            foreach (var r in list)
            {
                if (r.name != name || r.rect == null || r.rect.Length < 4) continue;
                float w = _layout.reference != null && _layout.reference.Length > 1 ? _layout.reference[0] : 1920f;
                float h = _layout.reference != null && _layout.reference.Length > 1 ? _layout.reference[1] : 1080f;
                centre = new Vector2((r.rect[0] + r.rect[2]) / 2f - w / 2f, h / 2f - (r.rect[1] + r.rect[3]) / 2f + _lift);
                size = new Vector2(r.rect[2] - r.rect[0], r.rect[3] - r.rect[1]);
                return true;
            }
            return false;
        }

        private Image Piece(string name)
        {
            if (!Place(_layout.pieces, name, out var centre, out var size)) return null;
            var image = Icon(_root, Art + name, centre, 1f);
            image.rectTransform.sizeDelta = size;
            image.name = name;
            return image;
        }

        private Text Words(string name, string text, int fontSize, Color colour, bool heavy)
        {
            Place(_layout.texts, name, out var centre, out var size);
            var label = Label(_root, text, fontSize, colour, centre, size, heavy);
            label.name = name;
            return label;
        }

        private static T Img<T>(Transform parent, string name) where T : Graphic
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(T));
            go.transform.SetParent(parent, false);
            var g = go.GetComponent<T>();
            g.raycastTarget = false;
            return g;
        }

        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        private static Image Icon(Transform parent, string path, Vector2 at, float size)
        {
            var image = Img<Image>(parent, path.Substring(path.LastIndexOf('/') + 1));
            if (!Sprites.TryGetValue(path, out var s) || s == null) Sprites[path] = s = UnityEngine.Resources.Load<Sprite>(path);
            image.sprite = s;
            image.rectTransform.sizeDelta = new Vector2(size, size);
            image.rectTransform.anchoredPosition = at;
            return image;
        }

        private static Text Label(Transform parent, string text, int size, Color colour, Vector2 at, Vector2 box, bool heavy)
        {
            var label = Img<Text>(parent, "Text");
            label.font = heavy ? UiTheme.Heavy : UiTheme.Bold;
            label.fontSize = size;
            label.color = colour;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.text = text;
            label.rectTransform.sizeDelta = box;
            label.rectTransform.anchoredPosition = at;
            return label;
        }

        private static void Stroke(Text label, float width, Color colour)
        {
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = colour;
            outline.effectDistance = new Vector2(width, -width);
        }

        /// <summary>«1 200 [crystal]» starting at `left` (the game's rule: the number, then the crystal).</summary>
        private static void Amount(Transform parent, int crystals, int size, Vector2 left)
        {
            var number = crystals.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', ' ');
            var label = Label(parent, number, size, Cream, new Vector2(left.x + 300f, left.y), new Vector2(600f, size * 1.4f), true);
            label.alignment = TextAnchor.MiddleLeft;
            Stroke(label, 2f, Ink);
            var icon = size * 1.1f;
            Icon(parent, ShopArt + "Crystal", new Vector2(left.x + label.preferredWidth + 10f + icon / 2f, left.y), icon);
        }
    }
}
