using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Maps;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The Рудник кристаллов (the user, 2026-11-14; card Design/features/monetization.md «Crystal Mine»): the PAID
    /// building, one a map. While the player is on the map it mines one crystal every crystal_mine_seconds_N
    /// (60/50/40/30/20 s at levels 1-5) into its own store, which stops at crystal_mine_cap_N (10..50) -- so a
    /// game left running overnight gains no more than a full store. The crystals wait in it until the player
    /// taps the shimmering crystal over the mine (Clash of Clans' way): then they go to the wallet.
    ///
    /// At crystal_mine_notice_share (80 %) full a notice comes; a tap on it sends the camera to the mine (the feed
    /// does that) and the yellow arrow points at the crystal over it (GuideHint). It comes again every
    /// crystal_mine_notice_repeat_sec until the crystals are taken.
    ///
    /// The clock runs in real seconds while the game runs (paused = stopped; the speed buttons do not hurry it).
    /// One overlay bubble, placed over the mine each frame, as DecorGifts does. The store is saved with the map.
    ///
    /// Bought or not (the user, 2026-11-15): a purchase taken back (a refund) stops every mine -- nothing grows,
    /// a tap opens the purchase (MenuShopWindow.ShowMine). The hints: once the Town Hall and the Lighthouse stand on
    /// a map with no mine, the build menu opens on the mine's card with the arrow (and a notice that leads there
    /// again) -- on every map; the first crystals get the arrow on the bubble until the player really taps it
    /// (once per account: PlayerPrefs hint.crystal_mine_collected).
    /// </summary>
    public class CrystalMines : MonoBehaviour
    {
        private const float TickSeconds = 0.25f;
        private const float FindEverySeconds = 1f;
        private const float Diameter = 104f;
        private const float AboveRoof = 0.45f;

        [SerializeField] private Sprite crystalIcon;

        public static CrystalMines Instance { get; private set; }

        private BuildingInstance _mine;
        private float _stored;
        private float _noticeWait;
        private float _tick;
        private float _find;
        private float _height;
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private RectTransform _bubble;
        private Image _icon;
        private Image _glint;
        private Text _label;
        private bool _placeHinted;          // this map, this session
        private bool _collectNoticed;
        private float _collectHintWait;
        private const string CollectedKey = "hint.crystal_mine_collected";

        /// <summary>The mine standing on this map (the player's, built), or null.</summary>
        public BuildingInstance Mine => _mine;

        /// <summary>The crystals in the mine's store (a fraction is the next one on its way).</summary>
        public float Stored => _stored;

        /// <summary>What the mine's store holds at its level.</summary>
        public int Cap => _mine == null ? 0 : CapAt(_mine.Level);

        public static int CapAt(int level) => BalanceConfig.Instance != null ? BalanceConfig.Instance.CrystalMineCap(level) : 10;

        private void Awake()
        {
            Instance = this;
            var go = new GameObject("CrystalMineCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            _canvas = go.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = -5; // over the world, UNDER the HUD (its canvas is at 0; the user 2026-11-22: bubbles drew over the interface)
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            _canvasRect = (RectTransform)go.transform;
            MakeBubble();
            _bubble.gameObject.SetActive(false);
            Wallet.CrystalMineChanged += OnOwnedChanged;
        }

        private void OnDestroy()
        {
            Wallet.CrystalMineChanged -= OnOwnedChanged;
            if (Instance == this) Instance = null;
        }

        /// <summary>Bought (the place hint may come again at once) or taken back (the bubble goes).</summary>
        private void OnOwnedChanged()
        {
            _placeHinted = false;
            ShowBubble();
        }

        private void Update()
        {
            // real seconds while the game runs: a pause stops the mine, the speed buttons don't hurry it
            var dt = Time.timeScale > 0f ? Time.unscaledDeltaTime : 0f;
            _find -= dt;
            if (_find <= 0f || (_mine != null && !Usable(_mine)))
            {
                _find = FindEverySeconds;
                FindMine();
                HintPlace();
                HintCollect();
            }
            _tick -= dt;
            if (_tick > 0f) return;
            var step = TickSeconds - _tick;
            _tick = TickSeconds;
            Grow(step);
        }

        /// <summary>Moves the mine's clock on by `dt` seconds: the store fills up to its cap, the notice comes at 80 %.</summary>
        public void Grow(float dt)
        {
            var config = BalanceConfig.Instance;
            if (_mine == null || config == null || dt <= 0f || !Wallet.CrystalMineOwned) return;
            var cap = Cap;
            _stored = Mathf.Min(cap, _stored + dt / Mathf.Max(0.1f, config.CrystalMineSeconds(_mine.Level)));
            ShowBubble();

            if (_stored >= cap * config.CrystalMineNoticeShare)
            {
                _noticeWait -= dt;
                if (_noticeWait <= 0f)
                {
                    _noticeWait = Mathf.Max(5f, config.CrystalMineNoticeRepeatSeconds);
                    PostNotice();
                }
            }
            else _noticeWait = 0f;
        }

        /// <summary>Takes the whole crystals out of the store into the wallet (the tap on the bubble).</summary>
        public int Collect()
        {
            if (!Wallet.CrystalMineOwned)
            {
                MenuShopWindow.ShowMine();
                return 0;
            }
            var n = Mathf.FloorToInt(_stored);
            if (n <= 0) return 0;
            _stored -= n;
            _noticeWait = 0f;
            Wallet.UpgradeCrystals += n;
            ShowBubble();
            return n;
        }

        /// <summary>The store as it was saved (after the buildings are back).</summary>
        public void RestoreFromSave(float stored)
        {
            FindMine();
            _stored = Mathf.Max(0f, stored);
            if (_mine != null) _stored = Mathf.Min(_stored, Cap);
            ShowBubble();
        }

        /// <summary>Cheats: the store full at once.</summary>
        public void FillCheat()
        {
            FindMine();
            if (_mine == null) return;
            _stored = Cap;
            ShowBubble();
        }

        private static bool Usable(BuildingInstance b) =>
            b != null && b.isActiveAndEnabled && !b.IsBot && b.IsOperational && b.CurrentHealth > 0
            && b.Data != null && b.Data.buildingName == BuildingIds.CrystalMine;

        private void FindMine()
        {
            if (Usable(_mine)) return;
            _mine = null;
            var all = BuildingInstance.All;
            for (var i = 0; i < all.Count; i++)
            {
                if (!Usable(all[i])) continue;
                _mine = all[i];
                _height = HarvestProgressBar.MeasuredHeightOf(_mine.transform) + AboveRoof;
                break;
            }
            if (_mine == null)
            {
                // a mine pulled down takes its store with it
                _stored = 0f;
                _noticeWait = 0f;
            }
            ShowBubble();
        }

        /// <summary>
        /// The mine bought, no mine on this map, the Town Hall and the Lighthouse standing: the build menu on the mine's
        /// card with the arrow, and a notice that leads there again. Once a map a session; again after a purchase.
        /// </summary>
        private void HintPlace()
        {
            if (_placeHinted || !Wallet.CrystalMineOwned || !BuildHints.Allowed || ModalGate.IsBlocked || MenuShopWindow.IsOpen) return;
            var hall = false;
            var lighthouse = false;
            var all = BuildingInstance.All;
            for (var i = 0; i < all.Count; i++)
            {
                var b = all[i];
                if (b == null || b.IsBot || b.Data == null) continue;
                var id = b.Data.buildingName;
                if (id == BuildingIds.CrystalMine) return;           // one stands (or is being built): nothing to say
                if (!b.IsOperational) continue;
                if (id == BuildingIds.TownHall) hall = true;
                else if (id == BuildingIds.Lighthouse) lighthouse = true;
            }
            if (!hall || !lighthouse) return;
            var placer = FindAnyObjectByType<BuildingPlacer>();
            if (placer != null && placer.IsSelecting) return;
            _placeHinted = true;
            var notice = GameEvent.Nowhere(Localization.Get("#crystal_mine_place"), EventSeverity.Good, EventGlyph.Star, BuildingIds.CrystalMine);
            notice.Tapped = () => BuildHints.LeadToBuild(BuildingIds.CrystalMine);
            EventLogManager.Instance?.Post(notice);
            BuildHints.LeadToBuild(BuildingIds.CrystalMine);
        }

        /// <summary>Crystals to take and the player never took any: the arrow on the bubble, again and again until they do.</summary>
        private void HintCollect()
        {
            if (PlayerPrefs.GetInt(CollectedKey, 0) == 1 || _mine == null || _bubble == null || !_bubble.gameObject.activeSelf) return;
            if (!BuildHints.Allowed || ModalGate.IsBlocked || GuideHint.Active || !OnScreen()) return;
            _collectHintWait -= FindEverySeconds;
            if (_collectHintWait > 0f) return;
            _collectHintWait = Mathf.Max(1f, BalanceConfig.Instance.HintSeconds) + 6f;
            if (!_collectNoticed)
            {
                _collectNoticed = true;
                EventLogManager.Instance?.Post(GameEvent.About(_mine, Localization.Get("#crystal_mine_collect"), EventSeverity.Good, EventGlyph.Star));
            }
            GuideHint.Point(_bubble);
        }

        private bool OnScreen()
        {
            var camera = Camera.main;
            if (camera == null || _mine == null) return false;
            var v = camera.WorldToViewportPoint(_mine.transform.position + Vector3.up * _height);
            return v.z > 0f && v.x > 0.05f && v.x < 0.95f && v.y > 0.08f && v.y < 0.92f;
        }

        private void PostNotice()
        {
            var text = Localization.Get("#crystal_mine_almost_full");
            var notice = GameEvent.About(_mine, text, EventSeverity.Warning, EventGlyph.Alert);
            notice.Tapped = () => { if (_bubble != null && _bubble.gameObject.activeInHierarchy) GuideHint.Point(_bubble); };
            EventLogManager.Instance?.Post(notice);
        }

        private void ShowBubble()
        {
            if (_bubble == null) return;
            var whole = Mathf.FloorToInt(_stored);
            var show = _mine != null && whole >= 1 && Wallet.CrystalMineOwned;
            if (_bubble.gameObject.activeSelf != show) _bubble.gameObject.SetActive(show);
            if (show) _label.text = whole + "/" + Cap;
        }

        private void MakeBubble()
        {
            var go = new GameObject("CrystalMineBubble", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_canvasRect, false);
            _bubble = (RectTransform)go.transform;
            _bubble.sizeDelta = new Vector2(Diameter, Diameter);
            var disc = go.GetComponent<Image>();
            disc.sprite = NodeYieldBubble.CircleSprite();
            disc.color = new Color(0.36f, 0.2f, 0.62f, 0.92f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = disc;
            button.onClick.AddListener(() =>
            {
                // the collect hint is done only by a real tap that took crystals
                var taken = Collect();
                // they fly from the bubble into the crystal counter (the user, 2026-11-17)
                if (taken > 0) Shop.OfferRewardFlight.FromWorld(crystalIcon, "Crystals", taken, _bubble);
                if (taken > 0 && PlayerPrefs.GetInt(CollectedKey, 0) != 1)
                {
                    PlayerPrefs.SetInt(CollectedKey, 1);
                    PlayerPrefs.Save();
                }
            });

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(_bubble, false);
            var iconRect = (RectTransform)icon.transform;
            iconRect.sizeDelta = new Vector2(Diameter * 0.72f, Diameter * 0.72f);
            iconRect.anchoredPosition = new Vector2(0f, 6f);
            _icon = icon.GetComponent<Image>();
            _icon.sprite = crystalIcon;
            _icon.preserveAspect = true;
            _icon.raycastTarget = false;

            // the shimmer: the same crystal, white and additive-looking, flashing over it
            var glint = new GameObject("Glint", typeof(RectTransform), typeof(Image));
            glint.transform.SetParent(iconRect, false);
            var glintRect = (RectTransform)glint.transform;
            glintRect.anchorMin = Vector2.zero;
            glintRect.anchorMax = Vector2.one;
            glintRect.offsetMin = glintRect.offsetMax = Vector2.zero;
            _glint = glint.GetComponent<Image>();
            _glint.sprite = crystalIcon;
            _glint.preserveAspect = true;
            _glint.raycastTarget = false;
            _glint.color = new Color(1f, 1f, 1f, 0f);

            var label = new GameObject("Amount", typeof(RectTransform), typeof(Text), typeof(Outline));
            label.transform.SetParent(_bubble, false);
            var labelRect = (RectTransform)label.transform;
            labelRect.sizeDelta = new Vector2(Diameter * 1.6f, 38f);
            labelRect.anchoredPosition = new Vector2(0f, -Diameter * 0.52f);
            _label = label.GetComponent<Text>();
            _label.font = UiTheme.Heavy;
            _label.fontSize = 30;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.color = Color.white;
            _label.raycastTarget = false;
            label.GetComponent<Outline>().effectColor = new Color(0.2f, 0.08f, 0.35f, 1f);
        }

        private void LateUpdate()
        {
            var camera = Camera.main;
            var hidden = camera == null || ModalGate.IsBlocked || _mine == null;
            if (_canvas.enabled == hidden) _canvas.enabled = !hidden;
            if (hidden || !_bubble.gameObject.activeSelf) return;

            var screen = camera.WorldToScreenPoint(_mine.transform.position + Vector3.up * _height);
            if (screen.z <= 0f)
            {
                _bubble.localScale = Vector3.zero;
                return;
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out var local);
            var t = Time.unscaledTime;
            // it bobs, breathes and flashes so the player sees there is something to take; full, it hurries
            var full = _stored >= Cap - 0.001f;
            var speed = full ? 2f : 1f;
            _bubble.anchoredPosition = local + new Vector2(0f, 7f * Mathf.Sin(t * 3f));
            _bubble.localScale = Vector3.one * (1f + 0.07f * Mathf.Sin(t * 5f * speed));
            var flash = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 2.6f * speed)), 6f);
            _glint.color = new Color(1f, 1f, 1f, 0.75f * flash);
        }
    }
}
