using System.Collections;
using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The main menu's «Новости» window, concept A «Свиток» (the user, 2026-11-17; card Design/features/news.md), built
    /// in code from the menu skin like MenuShopWindow: the title plate, the list of the newest items on the left (each a
    /// row with its title and a wax seal -- whole while unread, broken once opened -- and a red dot while its reward
    /// waits), the open item on a parchment card right: its title, its text (the sheet's rich-text tags kept, scrolls
    /// when long), the pictures of its buildings and its reward with «Забрать». Nothing in it knows any one item: it
    /// lists whatever the sheet's tab «Новости» holds (News.Shown).
    /// </summary>
    public class NewsWindow : MonoBehaviour
    {
        private const string Skin = "MenuSkins/Classic/";
        private const string Art = "News/";
        private const string ShopArt = "Shop/MenuShop/";
        private const int Order = 900;

        private const float PanelW = 1720f, PanelH = 920f;
        private const float ListW = 520f, RowH = 132f, RowGap = 14f;
        private const float PageW = 1080f, PageH = 760f;
        private const float ListX = -PanelW / 2f + 50f + ListW / 2f;
        private const float PageX = PanelW / 2f - 50f - PageW / 2f;
        private const float BodyY = -40f;

        private static readonly Color Ink = new Color(0.29f, 0.17f, 0.086f);
        private static readonly Color Muted = new Color(0.55f, 0.36f, 0.16f);
        private static readonly Color Cream = new Color(0.98f, 0.925f, 0.784f);
        private static readonly Color Red = new Color(0.84f, 0.18f, 0.15f);

        private static NewsWindow _current;

        public static bool IsOpen => _current != null;

        private RectTransform _root, _panel, _list, _page;
        private List<BalanceConfig.NewsItem> _items;
        private readonly List<Row> _rows = new List<Row>();
        private BalanceConfig.NewsItem _open;

        private sealed class Row
        {
            public BalanceConfig.NewsItem item;
            public Image back, seal;
            public GameObject dot;
        }

        /// <summary>Opens the window (the letter button, or NewsButton by itself).</summary>
        public static void Show()
        {
            if (_current != null) return;
            var go = new GameObject("NewsWindow", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = Order;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            _current = go.AddComponent<NewsWindow>();
        }

        public static void CloseCurrent()
        {
            if (_current != null) _current.Close();
        }

        private void Start()
        {
            _root = (RectTransform)transform;
            _items = News.Shown();
            News.Changed += RedrawRows;
            Build();
            if (_items.Count > 0) Open(_items[0]);
        }

        private void OnDestroy()
        {
            News.Changed -= RedrawRows;
            if (_current == this) _current = null;
        }

        public void Close()
        {
            if (_current == this) _current = null;
            Destroy(gameObject);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        // ------------------------------------------------------------ building

        private void Build()
        {
            var dim = Img<Image>(_root, "Dim");
            Stretch(dim.rectTransform);
            dim.color = new Color(0.06f, 0.035f, 0.015f, 0.72f);
            dim.raycastTarget = true;   // nothing behind takes a tap

            _panel = Sliced(_root, "Panel", Skin + "panel", new Vector2(PanelW, PanelH)).rectTransform;
            _panel.anchoredPosition = new Vector2(0f, -40f);

            var title = Sliced(_panel, "Title", Skin + "title", new Vector2(600f, 120f));
            title.rectTransform.anchoredPosition = new Vector2(0f, PanelH / 2f + 6f);
            Label(title.transform, Localization.Get("#news_title"), 58, Ink, new Vector2(0f, 4f), new Vector2(560f, 100f), true);

            var close = Img<Image>(_panel, "Close");
            close.sprite = Load(Art + "Close");
            close.raycastTarget = true;
            close.rectTransform.sizeDelta = new Vector2(110f, 110f);
            close.rectTransform.anchoredPosition = new Vector2(PanelW / 2f - 14f, PanelH / 2f - 14f);
            close.gameObject.AddComponent<Button>().onClick.AddListener(Close);

            if (_items.Count == 0)
            {
                Label(_panel, Localization.Get("#news_empty"), 40, Ink, new Vector2(0f, BodyY), new Vector2(1200f, 200f), false);
                return;
            }
            BuildList();
            _page = Sliced(_panel, "Page", Skin + "card", new Vector2(PageW, PageH)).rectTransform;
            _page.anchoredPosition = new Vector2(PageX, BodyY);
        }

        private void BuildList()
        {
            var view = Child(_panel, "List");
            view.sizeDelta = new Vector2(ListW + 20f, PageH);
            view.anchoredPosition = new Vector2(ListX, BodyY);
            view.gameObject.AddComponent<RectMask2D>();
            var catcher = view.gameObject.AddComponent<Image>();
            catcher.color = new Color(0f, 0f, 0f, 0f);
            _list = Child(view, "Content");
            _list.anchorMin = _list.anchorMax = new Vector2(0.5f, 1f);
            _list.pivot = new Vector2(0.5f, 1f);
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = _list;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;

            var y = -6f;
            foreach (var item in _items)
            {
                var row = new Row { item = item };
                row.back = Sliced(_list, "Row_" + item.number, Skin + "row", new Vector2(ListW, RowH));
                row.back.rectTransform.anchorMin = row.back.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                row.back.rectTransform.anchoredPosition = new Vector2(0f, y - RowH / 2f);
                var t = Label(row.back.transform, News.Title(item), 32, Ink, new Vector2(-46f, 0f), new Vector2(ListW - 150f, RowH - 24f), true);
                t.alignment = TextAnchor.MiddleLeft;
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                t.resizeTextForBestFit = true;
                t.resizeTextMinSize = 22;
                t.resizeTextMaxSize = 32;
                row.seal = Img<Image>(row.back.transform, "Seal");
                row.seal.rectTransform.sizeDelta = new Vector2(84f, 84f);
                row.seal.rectTransform.anchoredPosition = new Vector2(ListW / 2f - 62f, 0f);
                row.dot = Dot(row.back.transform, new Vector2(ListW / 2f - 18f, RowH / 2f - 16f));
                var captured = item;
                row.back.gameObject.AddComponent<Button>().onClick.AddListener(() => Open(captured));
                _rows.Add(row);
                y -= RowH + RowGap;
            }
            _list.sizeDelta = new Vector2(ListW + 20f, -y + 6f);
            _list.anchoredPosition = Vector2.zero;
            RedrawRows();
        }

        private void RedrawRows()
        {
            foreach (var row in _rows)
            {
                var selected = row.item == _open;
                row.back.sprite = Load(Skin + (selected ? "row_selected" : "row"));
                var read = News.IsRead(row.item);
                row.seal.sprite = Load(Art + (read ? "SealBroken" : "SealWhole"));
                row.dot.SetActive(News.RewardWaiting(row.item) && read);
            }
        }

        // ------------------------------------------------------------ the open item

        private void Open(BalanceConfig.NewsItem item)
        {
            var wasRead = News.IsRead(item);
            _open = item;
            News.MarkRead(item);
            RedrawRows();
            FillPage(item);
            if (!wasRead)
            {
                // the seal breaks: a little pop on the row's mark
                var row = _rows.Find(r => r.item == item);
                if (row != null) StartCoroutine(Pop(row.seal.rectTransform));
            }
        }

        private void FillPage(BalanceConfig.NewsItem item)
        {
            for (var i = _page.childCount - 1; i >= 0; i--) Destroy(_page.GetChild(i).gameObject);
            const float pad = 54f;
            var top = PageH / 2f - pad;
            var bottom = -PageH / 2f + 34f;

            var title = Label(_page, News.Title(item), 52, Ink, Vector2.zero, new Vector2(PageW - 2f * pad, 70f), true);
            title.alignment = TextAnchor.MiddleLeft;
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 30;
            title.resizeTextMaxSize = 52;
            title.rectTransform.anchoredPosition = new Vector2(0f, top - 35f);
            var line = Img<Image>(_page, "Divider");
            line.sprite = Load(Skin + "divider");
            line.type = Image.Type.Sliced;
            line.rectTransform.sizeDelta = new Vector2(PageW - 2f * pad, 10f);
            line.rectTransform.anchoredPosition = new Vector2(0f, top - 84f);
            var textTop = top - 104f;

            // the reward bar and the pictures sit at the bottom; the text takes what is left and scrolls
            if (News.HasReward(item))
            {
                RewardBar(item, bottom);
                bottom += 132f;
            }
            if (item.icons != null && item.icons.Count > 0)
            {
                IconRow(item, bottom);
                bottom += 132f;
            }
            TextView(News.Text(item), textTop, bottom + 10f, PageW - 2f * pad);
        }

        private void TextView(string text, float top, float bottom, float width)
        {
            var view = Child(_page, "TextView");
            view.sizeDelta = new Vector2(width, Mathf.Max(80f, top - bottom));
            view.anchoredPosition = new Vector2(0f, (top + bottom) / 2f);
            view.gameObject.AddComponent<RectMask2D>();
            var catcher = view.gameObject.AddComponent<Image>();
            catcher.color = new Color(0f, 0f, 0f, 0f);
            var label = Img<Text>(view, "Text");
            label.font = UiTheme.Regular;
            label.fontSize = 32;
            label.lineSpacing = 1.1f;
            label.color = Ink;
            label.supportRichText = true;
            label.alignment = TextAnchor.UpperLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.text = text;
            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var fitter = label.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = rect;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;
        }

        private void IconRow(BalanceConfig.NewsItem item, float bottom)
        {
            const float tile = 116f, gap = 16f;
            var x = -PageW / 2f + 54f + tile / 2f;
            foreach (var sprite in item.icons)
            {
                if (sprite == null) continue;
                var back = Sliced(_page, "Tile", Skin + "row", new Vector2(tile, tile));
                back.raycastTarget = false;
                back.rectTransform.anchoredPosition = new Vector2(x, bottom + tile / 2f);
                var icon = Img<Image>(back.transform, "Icon");
                icon.sprite = sprite;
                icon.preserveAspect = true;
                icon.rectTransform.sizeDelta = new Vector2(tile - 20f, tile - 20f);
                x += tile + gap;
                if (x > PageW / 2f - tile / 2f) break;
            }
        }

        private void RewardBar(BalanceConfig.NewsItem item, float bottom)
        {
            const float h = 112f;
            var bar = Sliced(_page, "Reward", Skin + "row_selected", new Vector2(PageW - 80f, h));
            bar.raycastTarget = false;
            bar.rectTransform.anchoredPosition = new Vector2(0f, bottom + h / 2f);
            var left = -(PageW - 80f) / 2f + 30f;
            var caption = Label(bar.transform, Localization.Get("#news_reward"), 36, Muted, Vector2.zero, new Vector2(400f, 60f), true);
            caption.alignment = TextAnchor.MiddleLeft;
            caption.rectTransform.anchoredPosition = new Vector2(left + 200f, 0f);
            var x = left + caption.preferredWidth + 24f;
            foreach (var reward in item.rewards)
            {
                var icon = Img<Image>(bar.transform, "RewardIcon");
                icon.sprite = reward.kind == "crystal" ? Load(ShopArt + "Crystal") : Load("Shop/Boosters/Booster_" + reward.id);
                icon.preserveAspect = true;
                icon.rectTransform.sizeDelta = new Vector2(80f, 80f);
                icon.rectTransform.anchoredPosition = new Vector2(x + 40f, 2f);
                x += 84f;
                var count = Label(bar.transform, "x" + reward.amount, 36, Cream, Vector2.zero, new Vector2(200f, 60f), true);
                count.alignment = TextAnchor.MiddleLeft;
                count.rectTransform.anchoredPosition = new Vector2(x + 100f, -14f);
                x += count.preferredWidth + 28f;
            }

            var waiting = News.RewardWaiting(item);
            var button = Sliced(bar.transform, "Claim", waiting ? ShopArt + "ButtonGreen" : Skin + "plaque", new Vector2(280f, 92f));
            button.rectTransform.anchoredPosition = new Vector2((PageW - 80f) / 2f - 160f, 0f);
            Label(button.transform, Localization.Get(waiting ? "#news_claim" : "#news_claimed"), 38, waiting ? Cream : Muted,
                new Vector2(0f, 3f), new Vector2(250f, 80f), true);
            if (!waiting) return;
            var captured = item;
            button.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                News.Claim(captured);
                FillPage(captured);
            });
        }

        private static IEnumerator Pop(RectTransform target)
        {
            for (var t = 0f; t < 1f && target != null; t += Time.unscaledDeltaTime / 0.35f)
            {
                target.localScale = Vector3.one * (1f + 0.35f * Mathf.Sin(t * Mathf.PI));
                yield return null;
            }
            if (target != null) target.localScale = Vector3.one;
        }

        // ------------------------------------------------------------ building blocks

        private static GameObject Dot(Transform parent, Vector2 at)
        {
            var dot = Img<Image>(parent, "Dot");
            dot.sprite = Load(ShopArt + "Sticker");
            dot.color = Red;
            dot.rectTransform.sizeDelta = new Vector2(40f, 40f);
            dot.rectTransform.anchoredPosition = at;
            var rim = dot.gameObject.AddComponent<Outline>();
            rim.effectColor = Color.white;
            rim.effectDistance = new Vector2(2f, -2f);
            return dot.gameObject;
        }

        private static RectTransform Child(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static T Img<T>(Transform parent, string name) where T : Graphic
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(T));
            go.transform.SetParent(parent, false);
            var g = go.GetComponent<T>();
            g.raycastTarget = false;
            return g;
        }

        private static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        private static Sprite Load(string path)
        {
            if (!Sprites.TryGetValue(path, out var s) || s == null) Sprites[path] = s = UnityEngine.Resources.Load<Sprite>(path);
            return s;
        }

        private static Image Sliced(Transform parent, string name, string path, Vector2 size)
        {
            var image = Img<Image>(parent, name);
            image.sprite = Load(path);
            image.type = image.sprite != null && image.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.rectTransform.sizeDelta = size;
            image.raycastTarget = true;
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
            if (colour == Cream)
            {
                var outline = label.gameObject.AddComponent<Outline>();
                outline.effectColor = Ink;
                outline.effectDistance = new Vector2(2f, -2f);
            }
            return label;
        }
    }
}
