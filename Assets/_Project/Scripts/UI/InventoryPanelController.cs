using System;
using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Resources;
using CityBuilder.Shop;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The inventory (the user, 2026-10-25): a round button on the left edge, under the citizens
    /// button, opens a card with every resource the settlement can hold -- its picture, its name and
    /// how much there is (not the room: a storehouse's room is shared by its whole group, so a
    /// per-row «/ 300» would read as wrong) -- the food chain's middle links (wheat,
    /// flour, pork) included, which the resource bar along the top no longer shows: up there is
    /// only Food, the one food counter. A resource the town has none of is dimmed, not hidden, so
    /// the card does not jump about as things come and go.
    ///
    /// Under the resources, the boosters (the user, 2026-11-12; mock-ups Design/store/Booster_Inventory*.png):
    /// a tile, 6 to a row, for every booster the player has or has running -- its picture, «×N», a running
    /// one in a gold frame with its time left. The card grows by a row as the tiles need. A tap on a tile
    /// opens the window beside the card (the card moves left): the booster, what it does, «В запасе ×N»
    /// and «Включить» -- «Продлить» while it runs, which adds one more full duration.
    ///
    /// The rows and tiles are built by SetupProject; this only fills them, on opening and whenever an
    /// amount changes while the card is up.
    /// </summary>
    public class InventoryPanelController : MonoBehaviour
    {
        [Serializable]
        public class BoosterTile
        {
            public Button button;
            public Image back;
            public Image icon;
            public Text count;
            public Text time;
            [NonSerialized] public string id;
            [NonSerialized] public int shownSeconds = -1;
        }

        private static readonly Color Dimmed = new Color(1f, 1f, 1f, 0.35f);

        // the card is parchment (the theme pass): white words vanished into it (the user,
        // 2026-10-31) -- the words are dark ink, an empty store's paler
        private static readonly Color Ink = new Color(0.27f, 0.16f, 0.07f, 1f);
        private static readonly Color InkDimmed = new Color(0.27f, 0.16f, 0.07f, 0.4f);

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private ResourceType[] types = new ResourceType[0];
        [SerializeField] private Image[] icons = new Image[0];
        [SerializeField] private Text[] names = new Text[0];
        [SerializeField] private Text[] amounts = new Text[0];
        [SerializeField] private ResourceIconLibrary iconLibrary;
        /// <summary>The upgrade crystals' row, last on the card (a wallet count, not a ResourceType; the user, 2026-11-02).</summary>
        [SerializeField] private Image crystalIcon;
        [SerializeField] private Text crystalName;
        [SerializeField] private Text crystalAmount;
        [SerializeField] private HudHider hud;

        [SerializeField] private RectTransform card;
        /// <summary>From the card's top to the first tile row's top; one tile row's height; below the last row (the close button).</summary>
        [SerializeField] private float tilesTop = 400f;
        [SerializeField] private float tileStep = 170f;
        [SerializeField] private float bottomSpace = 130f;
        [SerializeField] private int tilesPerRow = 6;
        [SerializeField] private Text boostersEmpty;
        [SerializeField] private Sprite tileSprite;
        [SerializeField] private Sprite tileOnSprite;
        [SerializeField] private BoosterTile[] tiles = new BoosterTile[0];
        [SerializeField] private RectTransform side;
        [SerializeField] private float sideGap = 30f;
        [SerializeField] private Image sideIcon;
        [SerializeField] private Text sideName;
        [SerializeField] private Text sideDesc;
        [SerializeField] private Text sideTime;
        [SerializeField] private Text sideStock;
        [SerializeField] private Text sideLeft;
        [SerializeField] private Button sideUse;
        [SerializeField] private Text sideUseLabel;
        [SerializeField] private Button sideClose;

        private readonly List<string> _shown = new List<string>();
        private string _picked;
        private int _sideSeconds = -1;

        public static InventoryPanelController Instance { get; private set; }

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        /// <summary>The resources the card lists, in its order. For the tests.</summary>
        public ResourceType[] Types => types;

        private void Awake()
        {
            Instance = this;
            foreach (var tile in tiles)
            {
                var t = tile;
                if (t.button != null) t.button.onClick.AddListener(() => Pick(t.id));
            }
            if (sideUse != null) sideUse.onClick.AddListener(UsePicked);
            if (sideClose != null) sideClose.onClick.AddListener(() => Pick(null));
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // The controller sits on the card itself, so it listens exactly while the card is up.
        private void OnEnable()
        {
            if (ResourceManager.Instance != null) ResourceManager.Instance.OnResourceChanged += HandleResourceChanged;
            Boosters.Changed += RefreshBoosters;
        }

        private void OnDisable()
        {
            if (ResourceManager.Instance != null) ResourceManager.Instance.OnResourceChanged -= HandleResourceChanged;
            Boosters.Changed -= RefreshBoosters;
        }

        public void OpenPanel()
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            ModalGate.SetBlocked(true);
            // the buttons behind the card go while it is open (the user's rule, 2026-10-14)
            if (hud != null) hud.Hide(this);
            Refresh();
        }

        /// <summary>Opens the card with this booster's window up (the HUD's running-booster chip).</summary>
        public void OpenBooster(string id)
        {
            if (!IsOpen) OpenPanel();
            Pick(id);
        }

        public void ClosePanel()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            _picked = null;
            ModalGate.SetBlocked(false);
            if (hud != null) hud.Show(this);
        }

        private void HandleResourceChanged(ResourceType type, int amount)
        {
            if (IsOpen) Refresh();
        }

        /// <summary>Every row: picture, name, amount. Cheap -- a dozen texts, and only while the card is up.</summary>
        public void Refresh()
        {
            var resources = ResourceManager.Instance;
            if (resources == null || !IsOpen) return;
            for (var i = 0; i < types.Length; i++)
            {
                var type = types[i];
                var amount = resources.GetAmount(type);
                var tint = amount > 0 ? Color.white : Dimmed;
                if (i < icons.Length && icons[i] != null)
                {
                    if (icons[i].sprite == null && iconLibrary != null) icons[i].sprite = iconLibrary.GetIcon(type);
                    icons[i].color = tint;
                }
                if (i < names.Length && names[i] != null)
                {
                    names[i].text = Capitalized(ResourceNames.Of(type));
                    names[i].color = amount > 0 ? Ink : InkDimmed;
                }
                if (i < amounts.Length && amounts[i] != null)
                {
                    amounts[i].text = amount.ToString();
                    amounts[i].color = amount > 0 ? Ink : InkDimmed;
                }
            }
            ShowCrystals();
            RefreshBoosters();
        }

        private void ShowCrystals()
        {
            var crystals = Wallet.UpgradeCrystals;
            if (crystalIcon != null) crystalIcon.color = crystals > 0 ? Color.white : Dimmed;
            if (crystalName != null)
            {
                crystalName.text = Localization.Get("#inventory_crystals");
                crystalName.color = crystals > 0 ? Ink : InkDimmed;
            }
            if (crystalAmount != null)
            {
                crystalAmount.text = crystals.ToString();
                crystalAmount.color = crystals > 0 ? Ink : InkDimmed;
            }
        }

        // ---------------------------------------------------------------- the boosters

        /// <summary>The tiles: every booster the player has or has running, in the sheet's order; the card's height to fit.</summary>
        private void RefreshBoosters()
        {
            if (!IsOpen) return;
            _shown.Clear();
            foreach (var def in Boosters.All)
                if (Boosters.Stock(def.id) > 0 || Boosters.Left(def.id) > 0f) _shown.Add(def.id);
            for (var i = 0; i < tiles.Length; i++)
            {
                var tile = tiles[i];
                var has = i < _shown.Count;
                tile.button.gameObject.SetActive(has);
                if (!has)
                {
                    tile.id = null;
                    continue;
                }
                var id = _shown[i];
                tile.id = id;
                var running = Boosters.Left(id) > 0f;
                tile.back.sprite = running ? tileOnSprite : tileSprite;
                tile.icon.sprite = OfferShopWindow.LoadBoosterIcon(id);
                var stock = Boosters.Stock(id);
                tile.count.gameObject.SetActive(stock > 0);
                tile.count.text = "×" + stock;
                tile.time.gameObject.SetActive(running);
                tile.shownSeconds = -1;
            }
            if (boostersEmpty != null) boostersEmpty.gameObject.SetActive(_shown.Count == 0);
            if (card != null)
            {
                var rows = Mathf.Max(1, (_shown.Count + tilesPerRow - 1) / tilesPerRow);
                var size = card.sizeDelta;
                size.y = tilesTop + rows * tileStep + bottomSpace;
                card.sizeDelta = size;
            }
            if (_picked != null && !_shown.Contains(_picked)) _picked = null;
            ShowSide();
            UpdateTimes();
        }

        private void Pick(string id)
        {
            _picked = id;
            ShowSide();
        }

        /// <summary>The window beside the card, or none; the card moves left to make room.</summary>
        private void ShowSide()
        {
            var def = _picked != null ? Boosters.Def(_picked) : null;
            if (side != null) side.gameObject.SetActive(def != null);
            if (card != null && side != null)
            {
                var shift = def != null ? (side.sizeDelta.x + sideGap) * 0.5f : 0f;
                card.anchoredPosition = new Vector2(-shift, 0f);
                side.anchoredPosition = new Vector2(card.sizeDelta.x * 0.5f - shift + sideGap + side.sizeDelta.x * 0.5f, 0f);
            }
            if (def == null) return;
            sideIcon.sprite = OfferShopWindow.LoadBoosterIcon(def.id);
            sideName.text = Localization.Get(def.nameKey);
            sideDesc.text = Boosters.Describe(def);
            sideTime.text = Boosters.Duration(def);
            var stock = Boosters.Stock(def.id);
            sideStock.text = Localization.Format("#booster_in_stock", stock);
            var running = Boosters.Left(def.id) > 0f;
            sideUseLabel.text = Localization.Get(running && Boosters.Lasts(def.id) ? "#booster_extend" : "#booster_on");
            sideUse.interactable = stock > 0 && Boosters.Here;
            sideLeft.gameObject.SetActive(running);
            _sideSeconds = -1;
        }

        private void UsePicked()
        {
            if (_picked == null) return;
            var id = _picked;
            if (!Boosters.Use(id)) return;
            // an at-once one used up: nothing left to show for it
            if (Boosters.Stock(id) <= 0 && Boosters.Left(id) <= 0f) _picked = null;
            RefreshBoosters();
        }

        private void Update()
        {
            if (IsOpen) UpdateTimes();
        }

        /// <summary>The running tiles' and the window's time left, written once a second (Boosters.Changed redraws when one runs out).</summary>
        private void UpdateTimes()
        {
            foreach (var tile in tiles)
            {
                if (tile.id == null || !tile.time.gameObject.activeSelf) continue;
                var left = Mathf.CeilToInt(Boosters.Left(tile.id));
                if (left == tile.shownSeconds) continue;
                tile.shownSeconds = left;
                tile.time.text = OfferShopWindow.Clock(left);
            }
            if (_picked != null && sideLeft != null && sideLeft.gameObject.activeSelf)
            {
                var left = Mathf.CeilToInt(Boosters.Left(_picked));
                if (left != _sideSeconds)
                {
                    _sideSeconds = left;
                    sideLeft.text = Localization.Format("#booster_left", OfferShopWindow.Clock(left));
                }
            }
        }

        private static string Capitalized(string name) =>
            string.IsNullOrEmpty(name) ? name : char.ToUpperInvariant(name[0]) + name.Substring(1);
    }
}
