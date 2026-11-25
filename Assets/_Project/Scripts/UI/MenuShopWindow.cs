using System;
using System.Collections;
using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The shop for real money (the user, 2026-11-15; card Design/features/monetization.md «Main-menu Магазин window»,
    /// mockups Design/store/MenuShop_*.png). Built in code, its pictures in Resources/Shop/MenuShop
    /// (Tools/store/menu_shop_art.py) and the menu's skin. Two ways in:
    ///
    ///   ShowShop  the main menu's «Магазин»: the scene behind BLURRED, the menu hidden; the bundles in one list that
    ///             scrolls (the Crystal Mine while not owned, then the three crystal packs); «Назад» bottom centre --
    ///             it may put up «Подожди!»: ONE offer out of a pool -- the mine at its sale price while not owned, or a
    ///             crystal pack at a discount -- the shop hidden, «Всё равно выйти» under it. Not on the first visit
    ///             (menu_shop_exit_free_visits), at most every menu_shop_exit_offer_hours, the mine with
    ///             menu_shop_exit_mine_chance, never the same offer twice running, never after a purchase that visit
    ///             (the user, 2026-11-15; mockups Design/store/ExitOffer_*.png);
    ///   ShowMine  the sandbox's locked mine card (or a refunded mine's building): the same mine bundle alone, the game
    ///             PAUSED, its interface hidden, «Закрыть» bottom centre.
    ///
    /// A purchase (Store) puts up «Покупка успешна!»: the picture, +crystals, and in the menu «В Песочницу» / «Позже».
    /// </summary>
    public class MenuShopWindow : MonoBehaviour
    {
        private enum Kind { Shop, Mine }

        private const string Art = "Shop/MenuShop/";
        private const string Skin = "MenuSkins/Classic/";
        private const string ExitShownKey = "menushop.exit_shown_utc";
        private const string VisitsKey = "menushop.visits";
        private const string LastOfferKey = "menushop.exit_last";
        private const int Order = 950;            // over the HUD, the shop and every panel
        // the shop list's pack cards are lower than the mine's and «Подожди!»'s (the user, 2026-11-15: little text on them)
        private const float CardW = 1320f, CardH = 330f, PackH = 200f, CardGap = 60f, PackGap = 50f;
        // every card's words start on one line (the user, 2026-11-15): 540 from the card's left edge
        private const float TextX = -CardW / 2f + 540f;

        private static readonly Color Ink = new Color(0.29f, 0.17f, 0.086f);
        private static readonly Color Cream = new Color(0.98f, 0.925f, 0.784f);
        private static readonly Color Gold = new Color(1f, 0.86f, 0.47f);
        private static readonly Color Red = new Color(0.86f, 0.24f, 0.2f);
        private static readonly Color Blue = new Color(0.24f, 0.51f, 0.82f);
        private static readonly Color Green = new Color(0.27f, 0.59f, 0.24f);
        private static readonly Color Lilac = new Color(0.9f, 0.84f, 1f);

        private static MenuShopWindow _current;

        public static bool IsOpen => _current != null;

        private Kind _kind;
        private Action _toSandbox;
        private RectTransform _root;
        private RawImage _blur;
        private RenderTexture _blurTexture;
        private RectTransform _shopView, _waitView, _mineView, _boughtView, _list;
        private Text _balance;
        private int _shownBalance, _targetBalance;
        private float _balanceRoll = 1f;
        private bool _boughtFromWait;
        private bool _boughtThisVisit;
        private string _awaiting;   // bought here, waiting for Store.Granted
        private int _visit;
        private Text _waitText;
        private RectTransform _waitCard;
        private readonly List<Canvas> _hidden = new List<Canvas>();
        private float _timeScaleBefore = 1f;
        private bool _gateBefore;

        /// <summary>The main menu's «Магазин». toSandbox: «В Песочницу» after buying the mine.</summary>
        public static void ShowShop(Action toSandbox) => Open(Kind.Shop, toSandbox);

        /// <summary>The mine's bundle alone, over a paused sandbox game.</summary>
        public static void ShowMine() => Open(Kind.Mine, null);

        private static void Open(Kind kind, Action toSandbox)
        {
            if (_current != null) return;
            var go = new GameObject("MenuShopWindow", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = Order;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            _current = go.AddComponent<MenuShopWindow>();
            _current._kind = kind;
            _current._toSandbox = toSandbox;
        }

        private void Start()
        {
            _root = (RectTransform)transform;
            if (_kind == Kind.Mine)
            {
                _timeScaleBefore = Time.timeScale;
                Time.timeScale = 0f;
                _gateBefore = ModalGate.IsBlocked;
                ModalGate.SetBlocked(true);
            }
            if (_kind == Kind.Shop)
            {
                _visit = PlayerPrefs.GetInt(VisitsKey, 0) + 1;
                CityBuilder.Core.Prefs.SetInt(VisitsKey, _visit);
                PlayerPrefs.Save();
            }
            HideTheInterfaceBehind();
            Store.Changed += Redraw;
            Store.Granted += OnGranted;
            Wallet.UpgradeCrystalsChanged += OnCrystals;
            StartCoroutine(Build());
        }

        /// <summary>One frame with the interface hidden, the screen taken and blurred, then the window over it.</summary>
        private IEnumerator Build()
        {
            yield return new WaitForEndOfFrame();
            _blur = Img<RawImage>(_root, "Blur");
            Stretch(_blur.rectTransform);
            _blur.texture = _blurTexture = BlurredScreen();
            // the screen behind DARKER, never lighter (the user, 2026-11-17: the blurred shot came out washed light):
            // the shot itself toned down, then a warm dark over it
            _blur.color = new Color(0.55f, 0.55f, 0.55f, 1f);
            var dim = Img<Image>(_root, "Dim");
            Stretch(dim.rectTransform);
            dim.raycastTarget = true;   // nothing behind takes a tap
            dim.color = new Color(0.08f, 0.05f, 0.02f, 0.5f);

            if (_kind == Kind.Shop)
            {
                BuildShopView();
                _waitView = BuildWaitView();
                _waitView.gameObject.SetActive(false);
            }
            else _mineView = BuildMineView();
        }

        private static RenderTexture BlurredScreen()
        {
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            int w = Mathf.Max(8, shot.width / 8), h = Mathf.Max(8, shot.height / 8);
            var a = RenderTexture.GetTemporary(Mathf.Max(8, shot.width / 4), Mathf.Max(8, shot.height / 4), 0);
            var b = RenderTexture.GetTemporary(Mathf.Max(4, w / 2), Mathf.Max(4, h / 2), 0);
            a.filterMode = b.filterMode = FilterMode.Bilinear;
            var result = new RenderTexture(w, h, 0) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            // down in steps and back up: each bilinear step smears a little more
            Graphics.Blit(shot, a);
            Graphics.Blit(a, result);
            Graphics.Blit(result, b);
            Graphics.Blit(b, result);
            RenderTexture.ReleaseTemporary(a);
            RenderTexture.ReleaseTemporary(b);
            Destroy(shot);
            return result;
        }

        // ------------------------------------------------------------ the views

        private void BuildShopView()
        {
            _shopView = Child(_root, "Shop");
            Stretch(_shopView);
            var title = Sliced(_shopView, "Title", Skin + "title", new Vector2(620f, 120f));
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -72f));
            Label(title.transform, Localization.Get("#menu_offers"), 60, Ink, new Vector2(0f, 4f), new Vector2(580f, 100f), heavy: true);

            // top left (the piggy bank stands top right, 2026-11-16), drawn as the piggy window's wallet:
            // the crystal and a stroked cream number on the picture, no plaque (the user, 2026-11-17)
            var wallet = Child(_shopView, "Balance");
            wallet.anchorMin = wallet.anchorMax = new Vector2(0f, 1f);
            wallet.sizeDelta = new Vector2(72f, 72f);
            wallet.anchoredPosition = new Vector2(72f, -60f);
            PiggyButton.Create(_shopView, new Vector2(1f, 1f), new Vector2(-20f, -10f), 170f);
            _shownBalance = _targetBalance = Wallet.UpgradeCrystals;
            PiggyUi.Pic(wallet, PiggyUi.Crystal, Vector2.zero, new Vector2(72f, 72f));
            _balance = PiggyUi.Stroke(PiggyUi.Label(wallet, _shownBalance.ToString(), 44, PiggyUi.Cream, new Vector2(44f + 200f, 0f), new Vector2(400f, 60f)), 3f, PiggyUi.Ink);
            _balance.alignment = TextAnchor.MiddleLeft;

            // the list: a viewport with a mask, the content scrolls up
            var view = Child(_shopView, "List");
            view.anchorMin = new Vector2(0.5f, 0f);
            view.anchorMax = new Vector2(0.5f, 1f);
            view.sizeDelta = new Vector2(CardW + 80f, -255f);
            view.anchoredPosition = new Vector2(0f, -2f);
            view.gameObject.AddComponent<RectMask2D>();
            var catcher = view.gameObject.AddComponent<Image>();
            catcher.color = new Color(0f, 0f, 0f, 0f);
            _list = Child(view, "Content");
            _list.anchorMin = new Vector2(0.5f, 1f);
            _list.anchorMax = new Vector2(0.5f, 1f);
            _list.pivot = new Vector2(0.5f, 1f);
            // the scroll hangs on the WHOLE window, the list is its viewport: a drag anywhere on the screen moves the
            // list (the user, 2026-11-18: «скролл работает только при нажатии на предложения, сделай по всему»)
            var anywhere = _shopView.gameObject.AddComponent<Image>();
            anywhere.color = new Color(0f, 0f, 0f, 0f);
            var scroll = _shopView.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = view;
            scroll.content = _list;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;
            FillList();

            var back = Sliced(_shopView, "Back", Skin + "plaque", new Vector2(360f, 96f));
            Anchor(back.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 56f));
            Label(back.transform, Localization.Get("#ui_back"), 46, Ink, new Vector2(0f, 3f), new Vector2(320f, 80f), heavy: true);
            back.gameObject.AddComponent<Button>().onClick.AddListener(Back);
        }

        private void FillList()
        {
            for (var i = _list.childCount - 1; i >= 0; i--) Destroy(_list.GetChild(i).gameObject);
            // every bundle a wide card, one under another (the packs as «Подожди!» draws them, the user, 2026-11-15)
            var y = -40f;
            if (!Wallet.CrystalMineOwned)
            {
                var card = MineCard(_list, Store.Mine);
                card.anchorMin = card.anchorMax = new Vector2(0.5f, 1f);
                card.anchoredPosition = new Vector2(0f, y - CardH / 2f);
                y -= CardH + CardGap;
            }
            var config = BalanceConfig.Instance;
            var packs = config != null ? config.MenuShopPackCount : 0;
            for (var p = 0; p < packs; p++)
            {
                var card = PackOfferCard(_list, p, false);
                card.anchorMin = card.anchorMax = new Vector2(0.5f, 1f);
                card.anchoredPosition = new Vector2(0f, y - PackH / 2f);
                y -= PackH + PackGap;
            }
            y += PackGap - 40f;
            _list.sizeDelta = new Vector2(CardW + 40f, -y);
            _list.anchoredPosition = Vector2.zero;
        }

        private RectTransform BuildWaitView()
        {
            var view = Child(_root, "Wait");
            Stretch(view);
            var panel = Sliced(view, "Panel", Skin + "panel", new Vector2(1440f, 600f));
            panel.rectTransform.anchoredPosition = new Vector2(0f, 50f);
            var title = Sliced(view, "Title", Skin + "title", new Vector2(640f, 116f));
            title.rectTransform.anchoredPosition = new Vector2(0f, 346f);
            var t = Label(title.transform, Localization.Get("#shop_wait_title"), 60, Cream, new Vector2(0f, 4f), new Vector2(600f, 100f), heavy: true);
            Stroke(t, 3f);
            _waitText = Label(view, Localization.Get("#shop_wait_text"), 38, Ink, new Vector2(0f, 250f), new Vector2(1300f, 60f));
            // the card itself is made when the offer is picked (ShowWait)
            var leave = Label(view, Localization.Get("#shop_wait_leave"), 38, Cream, new Vector2(0f, -320f), new Vector2(600f, 70f), heavy: true);
            Stroke(leave, 3f);
            leave.raycastTarget = true;
            leave.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            var line = Icon(view, "White", new Vector2(0f, -346f), 8f);
            line.rectTransform.sizeDelta = new Vector2(leave.preferredWidth, 4f);
            line.color = Cream;
            return view;
        }

        private RectTransform BuildMineView()
        {
            var view = Child(_root, "Mine");
            Stretch(view);
            var card = MineCard(view, Store.Mine);
            card.anchoredPosition = new Vector2(0f, 40f);
            var close = Sliced(view, "Close", Skin + "plaque", new Vector2(360f, 96f));
            Anchor(close.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 56f));
            Label(close.transform, Localization.Get("#ui_close"), 46, Ink, new Vector2(0f, 3f), new Vector2(320f, 80f), heavy: true);
            close.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            return view;
        }

        private void ShowBought(string id)
        {
            if (_boughtView != null) Destroy(_boughtView.gameObject);
            foreach (var v in new[] { _shopView, _waitView, _mineView }) if (v != null) v.gameObject.SetActive(false);
            var view = Child(_root, "Bought");
            Stretch(view);
            _boughtView = view;
            var panel = Sliced(view, "Panel", Skin + "panel", new Vector2(900f, 580f));
            panel.rectTransform.anchoredPosition = new Vector2(0f, 10f);
            var title = Sliced(view, "Title", Skin + "title", new Vector2(640f, 116f));
            title.rectTransform.anchoredPosition = new Vector2(0f, 300f);
            Stroke(Label(title.transform, Localization.Get("#shop_bought_title"), 52, Cream, new Vector2(0f, 4f), new Vector2(600f, 100f), heavy: true), 3f);

            var glow = Icon(view, "Glow", new Vector2(0f, 90f), 420f);
            glow.color = new Color(1f, 1f, 1f, 0.9f);
            var picture = Store.IsMine(id) ? "Mine" : "Geode" + PackIndex(id);
            var art = Icon(view, picture, new Vector2(0f, 90f), 300f);
            art.preserveAspect = true;
            var crystals = Store.CrystalsOf(id);
            if (crystals > 0) Amount(view, "+" + crystals, 56, Cream, new Vector2(0f, -100f));

            var mineInMenu = Store.IsMine(id) && _kind == Kind.Shop && _toSandbox != null;
            if (mineInMenu)
            {
                MakeButton(view, Localization.Get("#shop_to_sandbox"), Skin + "button", new Vector2(170f, -215f), new Vector2(320f, 110f), Cream, () =>
                {
                    var go = _toSandbox;
                    Close();
                    go?.Invoke();
                });
                MakeButton(view, Localization.Get("#shop_later"), Skin + "plaque", new Vector2(-170f, -215f), new Vector2(300f, 100f), Ink, AfterBought);
            }
            else MakeButton(view, Localization.Get("#shop_great"), Skin + "button", new Vector2(0f, -215f), new Vector2(340f, 110f), Cream, AfterBought);
        }

        /// <summary>«Покупка успешна!» closed: back to the list (the mine card gone), or out if the player was leaving.</summary>
        private void AfterBought()
        {
            if (_boughtView != null) Destroy(_boughtView.gameObject);
            _boughtView = null;
            if (_kind == Kind.Mine || _boughtFromWait)
            {
                Close();
                return;
            }
            _shopView.gameObject.SetActive(true);
            FillList();
            RollBalance();
        }

        // ------------------------------------------------------------ the cards

        /// <summary>The mine's bundle: the shop's (no discount) or «Подожди!»'s (the old price struck out, the sticker).</summary>
        private RectTransform MineCard(Transform parent, string product)
        {
            var config = BalanceConfig.Instance;
            var sale = product == Store.MineSale;
            var bg = Icon(parent, "MineCardBg", Vector2.zero, CardW);
            bg.rectTransform.sizeDelta = new Vector2(CardW, CardH);
            bg.name = "MineCard";
            var card = bg.rectTransform;
            Alive(card, CardH, 0);
            var mine = Icon(card, "Mine", new Vector2(-360f, 10f), 304f);
            mine.preserveAspect = true;
            mine.rectTransform.sizeDelta = new Vector2(420f, 304f);
            Ribbon(card, new Vector2(-360f, CardH / 2f - 18f), 220f, Localization.Get("#shop_forever"), Red);   // centred over the mine (the user, 2026-11-15)

            var x = TextX;
            var name = Label(card, Localization.GetOrDefault("#building_crystalmine", "Crystal Mine"), 58, Cream, new Vector2(x + 400f, CardH / 2f - 62f), new Vector2(800f, 80f), heavy: true);
            name.alignment = TextAnchor.MiddleLeft;
            Stroke(name, 3f);

            // the green chip: the castle off the Sandbox banner, «Только для Песочницы»
            var chipText = Localization.Get("#shop_sandbox_only");
            var chipY = CardH / 2f - 128f;
            var probe = Label(card, chipText, 30, Cream, Vector2.zero, new Vector2(800f, 50f), heavy: true);
            var chipW = 70f + probe.preferredWidth + 24f;
            Destroy(probe.gameObject);
            Pill(card, new Vector2(x + 6f, chipY), chipW - 6f, 50f, Green);
            Icon(card, "SandboxMark", new Vector2(x + 28f, chipY), 66f);
            // a left-aligned label 800 wide, its left edge at x + 70
            var chip = Label(card, chipText, 30, Cream, new Vector2(x + 70f + 400f, chipY - 1f), new Vector2(800f, 50f), heavy: true);
            chip.alignment = TextAnchor.MiddleLeft;
            Stroke(chip, 2f);

            var rate = config != null ? config.CrystalMineBestPerHour : 180;
            Row(card, new Vector2(x, CardH / 2f - 198f), 40, Cream, Localization.Format("#shop_mine_rate", rate), null, Localization.Get("#shop_per_hour"));
            var gift = config != null ? config.MenuShopMineGift : 0;
            if (gift > 0) Row(card, new Vector2(x, CardH / 2f - 256f), 40, Gold, "+" + gift, null, Localization.Get("#shop_mine_gift"));

            var buttonAt = new Vector2(CardW / 2f - 170f, -CardH / 2f + 64f);
            if (sale) SaleMarks(card, buttonAt, Store.Mine, config != null ? config.MenuShopMineSaleOffPercent : 30);
            PriceButton(card, buttonAt, new Vector2(270f, 106f), product, 48);
            return card;
        }

        /// <summary>
        /// A pack's bundle: the mine card's layout, the pack's colour, its rock of crystals. In the shop (sale false) the
        /// full price and the bonus sticker; in «Подожди!» the full price struck out, the discount sticker, the sale price.
        /// </summary>
        private RectTransform PackOfferCard(Transform parent, int index, bool sale)
        {
            var config = BalanceConfig.Instance;
            var low = !sale;                      // the shop list's low card
            var h = low ? PackH : CardH;
            var size = Mathf.Clamp(index, 0, 2);
            var bg = Icon(parent, (low ? "PackCardBgShort" : "PackCardBg") + (index + 1), Vector2.zero, CardW);
            bg.rectTransform.sizeDelta = new Vector2(CardW, h);
            bg.name = "PackOfferCard";
            var card = bg.rectTransform;
            Alive(card, h, index + 1);
            const float gx = -CardW / 2f + 300f;   // the ground's glow centre
            var geode = Icon(card, "Geode" + (index + 1), new Vector2(gx, low ? 0f : 12f), 10f);
            geode.preserveAspect = true;
            geode.rectTransform.sizeDelta = new Vector2(CardW * 0.38f, h * (low ? 0.66f + 0.08f * size : 0.62f + 0.1f * size));
            var badge = index == 1 ? "#shop_badge_popular" : index == 2 ? "#shop_badge_best" : null;
            // the ribbon centred over the crystals (the user, 2026-11-15)
            if (badge != null) Ribbon(card, new Vector2(gx, h / 2f - 18f), 260f, Localization.Get(badge), index == 1 ? Blue : Red);

            var x = TextX;
            var name = Label(card, Localization.Get("#shop_pack_" + (index + 1)), low ? 44 : 58, Cream,
                new Vector2(x + 400f, h / 2f - (low ? 62f : 90f)), new Vector2(800f, 80f), heavy: true);
            name.alignment = TextAnchor.MiddleLeft;
            Stroke(name, 3f);
            var crystals = config != null ? config.MenuShopPackCrystals(index) : 0;
            Row(card, new Vector2(x, h / 2f - (low ? 140f : 210f)), low ? 64 : 84, Cream,
                crystals.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', ' '), null, null);

            var buttonAt = low ? new Vector2(CardW / 2f - 170f, -6f) : new Vector2(CardW / 2f - 170f, -CardH / 2f + 64f);
            if (sale)
            {
                SaleMarks(card, buttonAt, Store.PackId(index), config != null ? config.MenuShopPackSaleOffPercent(index) : 0);
                PriceButton(card, buttonAt, new Vector2(270f, 106f), Store.PackSaleId(index), 48);
                return card;
            }
            var bonus = config != null ? config.MenuShopPackBonusPercent(index) : 0;
            // ABOVE the price button, not in the corner over it (the user, 2026-11-18: «процент выгоды уходит под цену»)
            if (bonus > 0) Sticker(card, new Vector2(buttonAt.x, h / 2f - 4f), 96f, Localization.Format("#shop_bonus", bonus), 30);
            PriceButton(card, buttonAt, new Vector2(270f, 100f), Store.PackId(index), 48);
            return card;
        }

        /// <summary>
        /// The card's life (the user, 2026-11-18): the gold rim laid over the ground and pulsed, the four sparkles of the
        /// ground as separate pictures that twinkle (ShopCardLife). Spots as Tools/store/menu_shop_art.py sparkle_spots.
        /// </summary>
        private static void Alive(RectTransform card, float h, int seed)
        {
            var rim = Icon(card, h < CardH ? "CardRimShort" : "CardRim", Vector2.zero, CardW);
            rim.rectTransform.sizeDelta = new Vector2(CardW + 48f, h + 48f);
            rim.color = new Color(1f, 0.9f, 0.45f, 0f);
            var spots = new[] { new Vector3(150f, 70f, 16f), new Vector3(470f, 90f, 12f), new Vector3(420f, h - 60f, 14f), new Vector3(110f, h - 90f, 10f) };
            var sparkles = new RectTransform[spots.Length];
            for (var i = 0; i < spots.Length; i++)
            {
                var sp = spots[i];
                var star = Icon(card, "Sparkle", new Vector2(sp.x - CardW / 2f, h / 2f - sp.y), sp.z * 2.8f);
                sparkles[i] = star.rectTransform;
            }
            card.gameObject.AddComponent<ShopCardLife>().Init(rim, sparkles, seed);
        }

        /// <summary>The full price struck out over the button and the discount sticker in the corner.</summary>
        private static void SaleMarks(RectTransform card, Vector2 buttonAt, string fullPriceProduct, int offPercent)
        {
            var old = Label(card, Store.PriceText(fullPriceProduct), 40, Lilac, buttonAt + new Vector2(0f, 92f), new Vector2(260f, 56f), heavy: true);
            Stroke(old, 2f);
            var strike = Icon(card, "White", buttonAt + new Vector2(0f, 92f), 8f);
            strike.rectTransform.sizeDelta = new Vector2(old.preferredWidth + 16f, 6f);
            strike.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 6f);
            strike.color = new Color(0.92f, 0.2f, 0.16f);
            if (offPercent > 0)
                Sticker(card, new Vector2(CardW / 2f - 26f, CardH / 2f - 26f), 112f, Localization.Format("#shop_off", offPercent), 34);
        }

        private void PriceButton(Transform parent, Vector2 at, Vector2 size, string product, int fontSize)
        {
            var image = Icon(parent, "ButtonGreen", at, size.x);
            image.rectTransform.sizeDelta = size;
            image.raycastTarget = true;
            var pending = Store.IsPending(product);
            var text = Label(image.transform, pending ? Localization.Get("#shop_pending") : Store.PriceText(product),
                pending ? Mathf.RoundToInt(fontSize * 0.6f) : fontSize, Cream, new Vector2(0f, 4f), size - new Vector2(20f, 10f), heavy: true);
            Stroke(text, 2f);
            var button = image.gameObject.AddComponent<Button>();
            button.interactable = !pending;
            if (pending) image.color = new Color(0.7f, 0.7f, 0.7f);
            button.onClick.AddListener(() => Buy(product));
        }

        private void Buy(string product)
        {
            _boughtFromWait = _waitView != null && _waitView.gameObject.activeSelf;
            _awaiting = product;
            if (!Store.Buy(product)) _awaiting = null;
        }

        /// <summary>The product came (at once from the stand-in, later from Google Play's payment sheet): «Покупка успешна».</summary>
        private void OnGranted(string id)
        {
            if (_awaiting == null || id != _awaiting) return;
            _awaiting = null;
            _boughtThisVisit = true;
            ShowBought(id);
        }

        // ------------------------------------------------------------ leaving

        /// <summary>Back (the button, Android's back key): «Подожди!» with one offer of the pool once in a while, else out.</summary>
        public void Back()
        {
            if (_boughtView != null) { AfterBought(); return; }
            if (_waitView != null && _waitView.gameObject.activeSelf) { Close(); return; }
            if (_kind == Kind.Shop && _shopView != null && ExitOfferDue(out var offer))
            {
                ShowWait(offer);
                return;
            }
            Close();
        }

        private void ShowWait(string offer)
        {
            CityBuilder.Core.Prefs.SetString(ExitShownKey, DateTime.UtcNow.Ticks.ToString());
            CityBuilder.Core.Prefs.SetString(LastOfferKey, offer);
            PlayerPrefs.Save();
            if (_waitCard != null) Destroy(_waitCard.gameObject);
            var mine = Store.IsMine(offer);
            _waitText.text = Localization.Get(mine ? "#shop_wait_text" : "#shop_wait_text_pack");
            _waitCard = mine ? MineCard(_waitView, Store.MineSale) : PackOfferCard(_waitView, Store.PackIndexOf(offer), true);
            _waitCard.anchoredPosition = new Vector2(0f, 35f);
            _shopView.gameObject.SetActive(false);
            _waitView.gameObject.SetActive(true);
        }

        /// <summary>Android's back key / Escape for whatever window is up.</summary>
        public static void BackCurrent()
        {
            if (_current != null) _current.Back();
        }

        /// <summary>Whether «Подожди!» comes up on this way out, and with which offer.</summary>
        private bool ExitOfferDue(out string offer)
        {
            offer = null;
            var config = BalanceConfig.Instance;
            if (_waitView == null || _boughtThisVisit || config == null) return false;
            if (_visit <= config.MenuShopExitFreeVisits) return false;     // the first visit shows the plain prices
            var hours = config.MenuShopExitOfferHours;
            if (hours > 0f && long.TryParse(PlayerPrefs.GetString(ExitShownKey, ""), out var ticks)
                && (DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc)).TotalHours < hours) return false;
            offer = PickExitOffer(config);
            return offer != null;
        }

        /// <summary>The mine (while not owned) with its chance, else one of the discounted packs; never the last one again.</summary>
        private static string PickExitOffer(BalanceConfig config)
        {
            var last = PlayerPrefs.GetString(LastOfferKey, "");
            var packs = new List<string>();
            for (var p = 0; p < config.MenuShopPackCount; p++)
            {
                var id = Store.PackSaleId(p);
                if (config.MenuShopPackSaleOffPercent(p) > 0 && Store.CanBuy(id)) packs.Add(id);
            }
            if (packs.Count > 1) packs.Remove(last);
            var mine = !Wallet.CrystalMineOwned && Store.CanBuy(Store.MineSale) && !(last == Store.MineSale && packs.Count > 0);
            if (mine && (packs.Count == 0 || UnityEngine.Random.value < config.MenuShopExitMineChance)) return Store.MineSale;
            return packs.Count > 0 ? packs[UnityEngine.Random.Range(0, packs.Count)] : null;
        }

        /// <summary>Closes whatever shop window is open (no exit offer).</summary>
        public static void CloseCurrent()
        {
            if (_current != null) _current.Close();
        }

        public void Close()
        {
            if (_current == this) _current = null;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            Store.Changed -= Redraw;
            Store.Granted -= OnGranted;
            Wallet.UpgradeCrystalsChanged -= OnCrystals;
            foreach (var canvas in _hidden) if (canvas != null) canvas.enabled = true;
            _hidden.Clear();
            if (_kind == Kind.Mine)
            {
                Time.timeScale = _timeScaleBefore;
                ModalGate.SetBlocked(_gateBefore);
            }
            if (_blurTexture != null)
            {
                _blurTexture.Release();
                Destroy(_blurTexture);
            }
            if (_current == this) _current = null;
        }

        private void HideTheInterfaceBehind()
        {
            var own = GetComponent<Canvas>();
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (canvas == own || !canvas.enabled || !canvas.isRootCanvas) continue;
                if (canvas.renderMode == RenderMode.WorldSpace || canvas.sortingOrder > own.sortingOrder) continue;
                canvas.enabled = false;
                _hidden.Add(canvas);
            }
        }

        private void Redraw()
        {
            if (_list != null && _boughtView == null) FillList();
        }

        private void OnCrystals()
        {
            if (_balance == null || _boughtView != null) return;
            RollBalance();
        }

        private void RollBalance()
        {
            _targetBalance = Wallet.UpgradeCrystals;
            _balanceRoll = 0f;
        }

        private void Update()
        {
            if (_balance == null || _balanceRoll >= 1f) return;
            _balanceRoll = Mathf.Min(1f, _balanceRoll + Time.unscaledDeltaTime / 0.8f);
            var n = Mathf.RoundToInt(Mathf.Lerp(_shownBalance, _targetBalance, _balanceRoll));
            _balance.text = n.ToString();
            _balance.transform.parent.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(_balanceRoll * Mathf.PI));
            if (_balanceRoll >= 1f) _shownBalance = _targetBalance;
        }

        private static int PackIndex(string id) => Mathf.Clamp(Store.PackIndexOf(id) + 1, 1, 3);

        // ------------------------------------------------------------ building blocks

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

        private static void Anchor(RectTransform r, Vector2 anchor, Vector2 position)
        {
            r.anchorMin = r.anchorMax = anchor;
            r.anchoredPosition = position;
        }

        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        private static Sprite Load(string path)
        {
            if (!Sprites.TryGetValue(path, out var s) || s == null) Sprites[path] = s = UnityEngine.Resources.Load<Sprite>(path);
            return s;
        }

        /// <summary>A picture from Resources/Shop/MenuShop, square at this size (callers reshape it).</summary>
        private static Image Icon(Transform parent, string name, Vector2 at, float size)
        {
            var image = Img<Image>(parent, name);
            image.sprite = Load(Art + name);
            image.rectTransform.sizeDelta = new Vector2(size, size);
            image.rectTransform.anchoredPosition = at;
            return image;
        }

        /// <summary>A menu-skin piece (its borders make it 9-sliced).</summary>
        private static Image Sliced(Transform parent, string name, string path, Vector2 size)
        {
            var image = Img<Image>(parent, name);
            image.sprite = Load(path);
            image.type = image.sprite != null && image.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.rectTransform.sizeDelta = size;
            image.raycastTarget = true;
            return image;
        }

        private static Text Label(Transform parent, string text, int size, Color colour, Vector2 at, Vector2 box, bool heavy = false)
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
            if (colour != Ink) Stroke(label, 2f);
            return label;
        }

        private static void Stroke(Text label, float width)
        {
            var outline = label.GetComponent<Outline>();
            if (outline == null) outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = Ink;
            outline.effectDistance = new Vector2(width, -width);
        }

        /// <summary>«12 345 [crystal]»: the number left, the crystal right, centred on `at` (the game's rule for crystals).</summary>
        private static void Amount(Transform parent, string number, int size, Color colour, Vector2 at)
        {
            var label = Label(parent, number, size, colour, at, new Vector2(600f, size * 1.4f), heavy: true);
            var icon = size * 1.25f;
            var total = label.preferredWidth + 10f + icon;
            label.alignment = TextAnchor.MiddleLeft;
            label.rectTransform.anchoredPosition = new Vector2(at.x - total / 2f + 300f, at.y);
            Icon(parent, "Crystal", new Vector2(at.x - total / 2f + label.preferredWidth + 10f + icon / 2f, at.y), icon);
        }

        /// <summary>A left-aligned row «before [crystal] after» starting at `at`.</summary>
        private static void Row(Transform parent, Vector2 at, int size, Color colour, string before, string middle, string after)
        {
            var x = at.x;
            var first = Label(parent, before, size, colour, Vector2.zero, new Vector2(600f, size * 1.4f), heavy: true);
            first.alignment = TextAnchor.MiddleLeft;
            first.rectTransform.anchoredPosition = new Vector2(x + 300f, at.y);
            x += first.preferredWidth + 10f;
            var icon = size * 1.25f;
            Icon(parent, "Crystal", new Vector2(x + icon / 2f, at.y), icon);
            x += icon + 10f;
            if (string.IsNullOrEmpty(after)) return;
            var last = Label(parent, after, size, colour, Vector2.zero, new Vector2(600f, size * 1.4f), heavy: true);
            last.alignment = TextAnchor.MiddleLeft;
            last.rectTransform.anchoredPosition = new Vector2(x + 300f, at.y);
        }

        private static void Ribbon(Transform parent, Vector2 at, float width, string text, Color colour)
        {
            var probe = Label(parent, text, 26, Cream, at, new Vector2(800f, 50f), heavy: true);
            width = Mathf.Max(width, probe.preferredWidth + 50f);
            Destroy(probe.gameObject);
            var dark = new Color(colour.r * 0.72f, colour.g * 0.72f, colour.b * 0.72f);
            var left = Icon(parent, "RibbonTail", new Vector2(at.x - width / 2f - 8f, at.y - 8f), 44f);
            left.rectTransform.sizeDelta = new Vector2(44f, 72f);
            left.color = dark;
            var right = Icon(parent, "RibbonTail", new Vector2(at.x + width / 2f + 8f, at.y - 8f), 44f);
            right.rectTransform.sizeDelta = new Vector2(44f, 72f);
            right.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            right.color = dark;
            var mid = Icon(parent, "RibbonMid", at, 16f);
            mid.rectTransform.sizeDelta = new Vector2(width, 56f);
            mid.color = colour;
            Stroke(Label(parent, text, 26, Cream, at + new Vector2(0f, -1f), new Vector2(width, 50f), heavy: true), 2f);
        }

        private static void Pill(Transform parent, Vector2 left, float width, float height, Color colour)
        {
            var end = height * 29f / 54f;
            var l = Icon(parent, "PillL", new Vector2(left.x + end / 2f, left.y), height);
            l.rectTransform.sizeDelta = new Vector2(end, height);
            var m = Icon(parent, "PillM", new Vector2(left.x + width / 2f, left.y), height);
            m.rectTransform.sizeDelta = new Vector2(Mathf.Max(1f, width - 2f * end + 2f), height);
            var r = Icon(parent, "PillR", new Vector2(left.x + width - end / 2f, left.y), height);
            r.rectTransform.sizeDelta = new Vector2(end, height);
            l.color = m.color = r.color = colour;
        }

        private static void Sticker(Transform parent, Vector2 at, float size, string text, int fontSize)
        {
            var disc = Icon(parent, "Sticker", at, size);
            disc.color = Red;
            Stroke(Label(disc.transform, text, fontSize, Cream, new Vector2(0f, -1f), new Vector2(size * 1.2f, size * 0.6f), heavy: true), 2f);
        }

        private static void MakeButton(Transform parent, string text, string skin, Vector2 at, Vector2 size, Color ink, Action onClick)
        {
            var image = Sliced(parent, "Button", skin, size);
            image.rectTransform.anchoredPosition = at;
            Label(image.transform, text, 40, ink, new Vector2(0f, 4f), size - new Vector2(30f, 16f), heavy: true);
            image.gameObject.AddComponent<Button>().onClick.AddListener(() => onClick());
        }
    }
}
