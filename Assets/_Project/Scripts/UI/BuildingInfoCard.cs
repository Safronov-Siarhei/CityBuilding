using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The sandbox's building card v2 (the user approved the concepts 2026-11-17: Design/ui/Info_*.png, made by
    /// Tools/ui/info_cards_concepts.py; the Laboratory's own card came first). One frame for every building:
    ///
    ///   «Name, ур. N» on a wood plaque; the level's picture on a tile; a line saying what the building is for;
    ///   «Прочность» and its bar; then the building's blocks; at the foot «Улучшить», a round hammer (repair),
    ///   a red round bin (demolish) and «Закрыть».
    ///
    /// The blocks, stacked in the body as the building needs them (BuildBody):
    ///   a house      -- «Жители» N / M with figures, what the decor round it pays each resident;
    ///   a workplace  -- «Рабочие» − N / M +, the free people, a band: what it brings, how often, its reach;
    ///                   a warning when its reach is bare; a food producer says what its neighbours add;
    ///   a store      -- a bar per resource it keeps (orange when full);
    ///   a tower      -- damage, range in cells, shots a minute;
    ///   a decoration -- the coins it pays each resident in reach, the houses and people it covers, the total.
    ///
    /// «Улучшить», the hammer and the bin open one small window (the dialog): the next level's picture beside
    /// this one with its gains, price and build time; the repair's price; what demolition gives back. The
    /// window's own button does it through BuildingInfoPanelController, which owns every action.
    /// The sandbox and the Invasion; the tutorial, the campaign and the Moat keep RunBuildingCard. The rows are made here, once per building shown.
    /// </summary>
    public partial class BuildingInfoCard : MonoBehaviour
    {
        [SerializeField] private BuildingInfoPanelController owner;
        [SerializeField] private Text title;
        [SerializeField] private Image picture;
        [SerializeField] private Text what;
        [SerializeField] private Image healthFill;
        [SerializeField] private Text healthText;
        [SerializeField] private RectTransform body;
        [SerializeField] private GameObject upgradeButton;
        [SerializeField] private Text upgradeLock;
        [SerializeField] private Button repairButton;
        [SerializeField] private Button demolishButton;

        [Header("The confirm window")]
        [SerializeField] private GameObject dialog;
        [SerializeField] private Text dialogTitle;
        [SerializeField] private Image dialogNow;
        [SerializeField] private GameObject dialogArrow;
        [SerializeField] private Image dialogNext;
        [SerializeField] private Image dialogSingle;
        [SerializeField] private RectTransform dialogGains;
        [SerializeField] private Text dialogText;
        [SerializeField] private Text dialogPriceCaption;
        [SerializeField] private Transform dialogCostRow;
        [SerializeField] private Text dialogTime;
        [SerializeField] private Image dialogConfirmImage;
        [SerializeField] private Text dialogConfirmLabel;

        [Header("Rows")]
        [SerializeField] private Sprite rowSprite;
        [SerializeField] private Sprite buttonSprite;
        [SerializeField] private Sprite barSprite;
        [SerializeField] private ResourceIconLibrary iconLibrary;
        [SerializeField] private Sprite heartGlyph;
        [SerializeField] private Sprite shieldGlyph;
        [SerializeField] private Sprite houseGlyph;
        [SerializeField] private Sprite peopleGlyph;
        [SerializeField] private Sprite radiusGlyph;
        [SerializeField] private Sprite swordsGlyph;
        [SerializeField] private Sprite fireGlyph;

        [Header("The levels' pictures: <building id>_L<level>")]
        [SerializeField] private string[] levelIconKeys = new string[0];
        [SerializeField] private Sprite[] levelIcons = new Sprite[0];

        private static readonly Color Ink = new Color(0.27f, 0.16f, 0.07f);
        private static readonly Color Soft = new Color(0.5f, 0.36f, 0.22f);
        private static readonly Color Warn = new Color(0.72f, 0.3f, 0.08f);
        private static readonly Color Good = new Color(0.24f, 0.47f, 0.14f);
        private static readonly Color BarGood = new Color(0.36f, 0.7f, 0.26f);
        private static readonly Color BarFull = new Color(0.86f, 0.56f, 0.2f);
        private static readonly Color Empty = new Color(0.27f, 0.16f, 0.07f, 0.22f);
        // the confirm button's light green, white words (the user, 2026-11-19: the dark green read badly; like the game's other green buttons)
        private static readonly Color ConfirmGreen = new Color(0.45f, 0.75f, 0.35f);
        private const float BodyWidth = 740f;

        private enum DialogKind { Upgrade, Repair, Demolish }
        private DialogKind _dialogKind;

        private BuildingInstance _building;
        private ProductionBuilding _work;
        private float _tick;

        // the rows' live parts, refilled by Refresh
        private Text _residents;
        private readonly List<Image> _figures = new List<Image>();
        private Text _decorEach;
        private Text _workers;
        private GameObject _bandRow;
        private int _builtLevel;
        private Text _free;
        private Button _plus, _minus;
        private GameObject _bandIconHolder;
        private Image _bandIcon;
        private Text _bandText;
        private Text _bandRadius;
        private RectTransform _recipeRow;
        private Text _workNote;
        private readonly List<(ResourceType type, Image fill, Text text)> _bars = new List<(ResourceType, Image, Text)>();
        private Text _storeNote;
        private Text _damage, _range, _shots;
        private Text _decorGives, _decorRadius, _decorHouses, _decorPeople, _decorTotal;

        public RectTransform RecipeRow => _recipeRow;

        /// <summary>The workplace band's line as shown (the tests read it).</summary>
        public string BandLine => _bandText != null && _bandText.gameObject.activeSelf ? _bandText.text : null;

        /// <summary>Whether this card stands in for the run's: the sandbox and the Invasion (the user, 2026-11-17); not the tutorial, the campaign, a dungeon or the Moat.</summary>
        public static bool Serves(BuildingData data) =>
            data != null && !data.isMoat && !Dungeon.DungeonMode.IsActive && !Tutorial.TutorialMode.IsActive
            && !Campaign.CityDefenceMode.IsActive;

        /// <summary>Opens on `building`: lays its blocks out, then fills them.</summary>
        public void Show(BuildingInstance building, ProductionBuilding work)
        {
            _building = building;
            _work = work;
            if (dialog != null) dialog.SetActive(false);
            gameObject.SetActive(true);
            BuildBody();
        }

        public void Hide()
        {
            if (dialog != null) dialog.SetActive(false);
            gameObject.SetActive(false);
            _building = null;
            _work = null;
        }

        // a store's goods and a house's people move on their own: the numbers follow while the card is up
        private void Update()
        {
            if (_building == null) return;
            _tick -= Time.unscaledDeltaTime;
            if (_tick > 0f) return;
            _tick = 0.5f;
            Refresh(null, false);
        }

        private bool IsHouse => BuildingInstance.IsHouse(_building.Data);
        private bool IsWork => _work != null && _building.Data.LevelStats(BuildingInstance.MaxLevel).maxWorkers > 0;
        private bool IsStore => StoreLedger.Active && StoreLedger.IsStore(_building.Data);
        private bool IsDecor => DecorBonus.IsDecor(_building.Data);
        // a special building's own block says what it is for: only the Town Hall keeps its damage and range
        private bool IsDefence => _building.GetComponent<DefensiveBuilding>() != null && (SpecialWhat() == null || IsHall);
        private bool Gathers => _work != null && (_work.HarvestRadius > 0 || _building.Data.buildingName == BuildingIds.FisherHut);

        /// <summary>
        /// Fills the card. `makes` is the workplace's line (BuildingInfoPanelController: what it brings, a food
        /// building's day, a healer's beds), `converts` a Плавильня's recipe in pictures instead of a line.
        /// </summary>
        public void Refresh(string makes, bool converts)
        {
            var b = _building;
            if (b == null || b.Data == null) return;
            var data = b.Data;
            if (b.Level != _builtLevel) BuildBody();
            if (title != null) title.text = Localization.Format("#lab_info_title", data.LocalizedName, b.Level);
            if (picture != null) picture.sprite = LevelIcon(data.buildingName, b.Level);
            if (what != null) what.text = WhatFor();
            var most = Mathf.Max(1, b.MaxHealth);
            if (healthFill != null)
            {
                healthFill.fillAmount = Mathf.Clamp01(b.CurrentHealth / (float)most);
                healthFill.color = b.CurrentHealth * 2 > most ? BarGood : BarFull;
            }
            if (healthText != null) healthText.text = $"{b.CurrentHealth} / {b.MaxHealth}";

            RefreshHouse();
            if (makes != null || converts || _workers != null) RefreshWork(makes, converts);
            RefreshStore();
            RefreshDefence();
            RefreshDecor();
            RefreshSpecial();
            RefreshFoot();
        }

        private string WhatFor()
        {
            var special = SpecialWhat();
            if (special != null) return Localization.Get(special);
            if (IsHouse) return Localization.Get("#card_what_house");
            if (IsStore) return Localization.Get("#card_what_store");
            if (IsDecor) return Localization.Get("#card_what_decor");
            if (_work != null && FoodOutput.IsProducer(_work.BuildingId)) return Localization.Get("#card_what_food");
            if (_work != null && FoodOutput.IsBooster(_work.BuildingId)) return Localization.Get("#card_what_food_boost");
            if (IsWork && _work.SelectedRecipe != null && Infirmary.CardLine(_building) == null && Remains.CardLine(_building) == null)
            {
                var thing = ResourceNames.Of(_work.ProducesResource).ToLower(System.Globalization.CultureInfo.CurrentCulture);
                return Localization.Format(Gathers ? "#card_what_gather" : "#card_what_make", thing);
            }
            if (IsDefence) return Localization.Get("#card_what_defence");
            return string.Empty;
        }

        // ---------------------------------------------------------------- the foot

        private void RefreshFoot()
        {
            var b = _building;
            var site = b.Site;
            var hasNext = b.GetUpgradeCost() != null && b.Level < BuildingInstance.MaxLevel;
            var open = site == null && hasNext && b.NextLevelResearched;
            if (upgradeButton != null) upgradeButton.SetActive(open);
            if (upgradeLock != null)
            {
                upgradeLock.gameObject.SetActive(!open && (site != null || hasNext));
                if (site != null) upgradeLock.text = Localization.Format(site.IsUpgrade ? "#building_upgrading" : "#building_constructing", Clock(site.Remaining));
                else if (hasNext) upgradeLock.text = BuildHints.UpgradeLockText(b);
                LockLeadsToLab(upgradeLock, site == null && hasNext && !b.NextLevelResearched);
            }
            if (repairButton != null) repairButton.interactable = b.GetRepairCost() != null;
            if (demolishButton != null) demolishButton.interactable = b.CanDemolish;
        }

        /// <summary>
        /// «Уровень N ещё не изучен» is pressable (the user, 2026-11-19): the card closes and the Laboratory's window opens on
        /// that research -- no Laboratory: the way to build one (BuildHints.LeadToResearch). A sandbox thing (BuildHints.Allowed).
        /// </summary>
        private void LockLeadsToLab(Text label, bool locked)
        {
            if (label == null) return;
            var button = label.GetComponent<Button>();
            if (button == null)
            {
                button = label.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() =>
                {
                    var b = _building;
                    if (b == null || b.Data == null || !BuildHints.Allowed) return;
                    var topic = Research.ResearchTopic.BuildingLevelId(b.Data.buildingName, b.Level + 1);
                    owner.Close();
                    BuildHints.LeadToResearch(topic);
                });
            }
            var on = locked && BuildHints.Allowed;
            button.enabled = on;
            label.raycastTarget = on;
            label.fontStyle = on ? FontStyle.Bold : FontStyle.Normal;
        }

        private static string Clock(float seconds)
        {
            var s = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            return $"{s / 60}:{s % 60:00}";
        }

        // ---------------------------------------------------------------- the body

        private void BuildBody()
        {
            for (var i = body.childCount - 1; i >= 0; i--) Destroy(body.GetChild(i).gameObject);
            _residents = _workers = _free = _workNote = _storeNote = _damage = _range = _shots = null;
            _decorEach = _bandText = _bandRadius = _decorGives = _decorRadius = _decorHouses = _decorPeople = _decorTotal = null;
            _figures.Clear();
            _bars.Clear();
            _plus = _minus = null;
            _bandIcon = null;
            _bandIconHolder = null;
            _recipeRow = null;
            _bandRow = null;
            ClearSpecial();
            _builtLevel = _building.Level;
            BuildSpecialTop();

            if (IsHouse)
            {
                var row = Row(40f);
                Label(row, Localization.Get("#card_residents"), 26, Ink, 0f, 400f, TextAnchor.MiddleLeft);
                _residents = Label(row, string.Empty, 28, Ink, 0f, 200f, TextAnchor.MiddleRight, right: true);
                var figures = Row(56f).gameObject.AddComponent<HorizontalLayoutGroup>();
                figures.spacing = 10f;
                figures.childAlignment = TextAnchor.MiddleLeft;
                figures.childControlWidth = figures.childControlHeight = false;
                figures.childForceExpandWidth = figures.childForceExpandHeight = false;
                var places = Mathf.Min(12, HousePeople.PlacesOf(_building));
                for (var i = 0; i < places; i++)
                {
                    var figure = NewImage(figures.transform, "Figure", peopleGlyph, Ink);
                    figure.rectTransform.sizeDelta = new Vector2(48f, 48f);
                    _figures.Add(figure);
                }
                _decorEach = Label(Row(34f), string.Empty, 22, Soft, 0f, BodyWidth, TextAnchor.MiddleLeft);
            }

            if (IsWork)
            {
                var row = Row(70f);
                Label(row, Localization.Get(WorkersKey), 26, Ink, 0f, 200f, TextAnchor.MiddleLeft);
                _minus = RoundButton(row, "−", 250f, owner.UnassignWorker);
                _workers = Label(row, string.Empty, 30, Ink, 300f, 160f, TextAnchor.MiddleCenter);
                _plus = RoundButton(row, "+", 510f, owner.AssignWorker);
                _free = Label(Row(30f), string.Empty, 21, Soft, 0f, BodyWidth, TextAnchor.MiddleLeft);
                MetalButtons();

                var band = Row(72f);
                _bandRow = band.gameObject;
                var plate = NewImage(band, "Plate", rowSprite, new Color(0.55f, 0.38f, 0.2f, 0.25f));
                plate.type = Image.Type.Sliced;
                Stretch(plate.rectTransform);
                _bandIcon = NewImage(band, "Icon", null, Color.white);
                _bandIconHolder = _bandIcon.gameObject;
                Pin(_bandIcon.rectTransform, 14f, 44f, 44f);
                _bandText = Label(band, string.Empty, 24, Ink, 70f, 470f, TextAnchor.MiddleLeft);
                _bandText.resizeTextForBestFit = true;
                _bandText.resizeTextMinSize = 15;
                _bandText.resizeTextMaxSize = 24;
                var radiusIcon = NewImage(band, "RadiusIcon", radiusGlyph, Ink);
                Pin(radiusIcon.rectTransform, 560f, 32f, 32f);
                _bandRadius = Label(band, string.Empty, 22, Ink, 598f, 140f, TextAnchor.MiddleLeft);
                _recipeRow = new GameObject("RecipeRow", typeof(RectTransform)).GetComponent<RectTransform>();
                _recipeRow.SetParent(band, false);
                _recipeRow.anchorMin = _recipeRow.anchorMax = new Vector2(0.5f, 0.5f);
                _recipeRow.sizeDelta = new Vector2(BodyWidth - 40f, 58f);
                _workNote = Label(Row(34f), string.Empty, 21, Warn, 0f, BodyWidth, TextAnchor.MiddleLeft);
            }

            if (IsStore)
            {
                Label(Row(36f), Localization.Get("#card_stores"), 26, Ink, 0f, BodyWidth, TextAnchor.MiddleLeft);
                foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
                {
                    if (StoreLedger.CapOf(_building.Data.buildingName, type, BuildingInstance.MaxLevel) <= 0) continue;
                    if (_bars.Count >= 6) break;
                    var row = Row(40f);
                    var icon = NewImage(row, "Icon", iconLibrary != null ? iconLibrary.GetIcon(type) : null, Color.white);
                    Pin(icon.rectTransform, 0f, 36f, 36f);
                    var back = NewImage(row, "Bar", barSprite, new Color(0.12f, 0.08f, 0.05f, 0.9f));
                    back.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                    back.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                    back.rectTransform.offsetMin = new Vector2(52f, -15f);
                    back.rectTransform.offsetMax = new Vector2(0f, 15f);
                    var fill = NewImage(back.transform, "Fill", barSprite, BarGood);
                    Stretch(fill.rectTransform);
                    fill.rectTransform.offsetMin = new Vector2(3f, 3f);
                    fill.rectTransform.offsetMax = new Vector2(-3f, -3f);
                    fill.type = Image.Type.Filled;
                    fill.fillMethod = Image.FillMethod.Horizontal;
                    var text = Label(back.transform, string.Empty, 20, Color.white, 0f, 0f, TextAnchor.MiddleCenter);
                    Stretch(text.rectTransform);
                    _bars.Add((type, fill, text));
                }
                _storeNote = Label(Row(32f), Localization.Get("#card_store_full"), 20, Warn, 0f, BodyWidth, TextAnchor.MiddleLeft);
            }

            if (IsDefence)
            {
                _damage = Stat(swordsGlyph, "#card_damage");
                _range = Stat(radiusGlyph, "#card_range");
                if (!IsHall)
                {
                    _shots = Stat(fireGlyph, "#card_shots");
                    Label(Row(32f), Localization.Get("#card_radius_shown"), 20, Soft, 0f, BodyWidth, TextAnchor.MiddleLeft);
                }
            }

            if (IsDecor)
            {
                Label(Row(32f), Localization.Get("#card_decor_each"), 22, Ink, 0f, BodyWidth, TextAnchor.MiddleLeft);
                var band = Row(62f);
                var plate = NewImage(band, "Plate", rowSprite, new Color(0.55f, 0.38f, 0.2f, 0.25f));
                plate.type = Image.Type.Sliced;
                Stretch(plate.rectTransform);
                var coin = NewImage(band, "Icon", iconLibrary != null ? iconLibrary.GetIcon(ResourceType.Coins) : null, Color.white);
                Pin(coin.rectTransform, 14f, 44f, 44f);
                _decorGives = Label(band, string.Empty, 26, Ink, 70f, 470f, TextAnchor.MiddleLeft);
                var radiusIcon = NewImage(band, "RadiusIcon", radiusGlyph, Ink);
                Pin(radiusIcon.rectTransform, 560f, 32f, 32f);
                _decorRadius = Label(band, string.Empty, 22, Ink, 598f, 140f, TextAnchor.MiddleLeft);
                _decorHouses = Stat(houseGlyph, "#card_decor_houses");
                _decorPeople = Stat(peopleGlyph, "#card_decor_people");
                var total = Row(38f);
                var totalCoin = NewImage(total, "Icon", iconLibrary != null ? iconLibrary.GetIcon(ResourceType.Coins) : null, Color.white);
                Pin(totalCoin.rectTransform, 0f, 32f, 32f);
                _decorTotal = Label(total, string.Empty, 22, Good, 44f, BodyWidth - 44f, TextAnchor.MiddleLeft);
            }

            BuildSpecial();
        }

        private void RefreshHouse()
        {
            if (_residents == null) return;
            var places = HousePeople.PlacesOf(_building);
            var people = HousePeople.Instance != null ? HousePeople.Instance.CitizensOf(_building) : places;
            _residents.text = $"{people} / {places}";
            for (var i = 0; i < _figures.Count; i++) _figures[i].color = i < people ? Ink : Empty;
            var bonus = DecorBonus.PerResident(_building);
            _decorEach.text = bonus > 0f
                ? Localization.Format("#card_decor_house", DecorBonus.Coins(bonus))
                : Localization.Get("#card_no_decor");
            _decorEach.color = bonus > 0f ? Good : Soft;
        }

        private void RefreshWork(string makes, bool converts)
        {
            if (_workers == null || _work == null) return;
            var idle = CitizenManager.Instance != null ? CitizenManager.Instance.IdlePopulation : 0;
            _workers.text = $"{_work.AssignedWorkers} / {_work.MaxWorkers}";
            _free.text = Localization.Format("#building_free", idle);
            if (_plus != null) _plus.interactable = _work.AssignedWorkers < _work.MaxWorkers && idle > 0;
            if (_minus != null) _minus.interactable = _work.AssignedWorkers > 0;
            if (makes == null && !converts) return;   // the half-second tick: the line stays what the last full refresh said

            var empty = _work.NothingLeftInReach;
            if (_bandRow != null) _bandRow.SetActive(!HidesBand && (converts || !string.IsNullOrEmpty(makes)));
            var food = FoodOutput.IsProducer(_work.BuildingId) || FoodOutput.IsBooster(_work.BuildingId);
            var healer = Infirmary.CardLine(_building) != null || Remains.CardLine(_building) != null || Maps.Hunting.CardLine(_building) != null;
            var icon = healer || iconLibrary == null ? null : iconLibrary.GetIcon(food ? ResourceType.Food : _work.ProducesResource);
            _bandIconHolder.SetActive(!converts && icon != null);
            _bandIcon.sprite = icon;
            _bandText.gameObject.SetActive(!converts);
            _bandText.text = makes ?? string.Empty;
            _bandText.rectTransform.anchoredPosition = new Vector2(icon != null ? 70f : 14f, 0f);
            _recipeRow.gameObject.SetActive(converts);

            var reach = Gathers ? HarvestRadiusOverlay.RadiusOf(_building.Data, _building.Level) : _building.Data.LevelStats(_building.Level).serviceRadius * Cell;
            var cells = Mathf.RoundToInt(reach / Cell);
            _bandRadius.transform.parent.Find("RadiusIcon").gameObject.SetActive(!converts && cells > 0);
            _bandRadius.gameObject.SetActive(!converts && cells > 0);
            _bandRadius.text = Localization.Format("#card_cells", cells);

            var boost = FoodOutput.IsProducer(_work.BuildingId) ? FoodOutput.BoostFor(_work) : 0;
            _workNote.gameObject.SetActive(empty || boost > 0);
            _workNote.color = empty ? Warn : Soft;
            _workNote.text = empty ? Localization.Format("#building_nothing_in_reach", ResourceNames.Of(_work.ProducesResource))
                : Localization.Format("#card_food_boost", boost);
        }

        private static float Cell => GridManager.Instance != null ? GridManager.Instance.CellSize : 2f;

        private void RefreshStore()
        {
            if (_bars.Count == 0) return;
            var full = false;
            foreach (var (type, fill, text) in _bars)
            {
                var cap = StoreLedger.CapOf(_building, type);
                var have = StoreLedger.AmountIn(_building, type);
                var isFull = cap > 0 && have >= cap;
                full |= isFull;
                fill.fillAmount = cap > 0 ? Mathf.Clamp01(have / (float)cap) : 0f;
                fill.color = isFull ? BarFull : BarGood;
                text.text = $"{have} / {cap}";
            }
            if (_storeNote != null) _storeNote.gameObject.SetActive(full);
        }

        private void RefreshDefence()
        {
            if (_damage == null) return;
            _damage.text = _building.Defense.ToString();
            _range.text = Localization.Format("#card_cells", Mathf.RoundToInt(DefensiveBuilding.RangeOf(_building) / Cell));
            if (_shots == null) return;
            var interval = BalanceConfig.Instance != null ? BalanceConfig.Instance.DefenceAttackIntervalSeconds : 0f;
            _shots.text = interval > 0f ? Mathf.RoundToInt(60f / interval).ToString() : "—";
        }

        private void RefreshDecor()
        {
            if (_decorGives == null) return;
            var stats = _building.Data.LevelStats(_building.Level);
            _decorGives.text = Localization.Format("#card_per_day", DecorBonus.Coins(stats.residentCoins));
            _decorRadius.text = Localization.Format("#card_cells", stats.serviceRadius);
            var here = _building.transform.position;
            var reach = stats.serviceRadius * Cell;
            var houses = 0;
            var people = 0;
            foreach (var house in BuildingInstance.Houses)
            {
                if (house == null || house.IsBot) continue;
                var d = house.transform.position - here;
                d.y = 0f;
                if (d.sqrMagnitude > reach * reach) continue;
                houses++;
                people += HousePeople.Instance != null ? HousePeople.Instance.CitizensOf(house) : HousePeople.PlacesOf(house);
            }
            _decorHouses.text = houses.ToString();
            _decorPeople.text = people.ToString();
            var working = DecorBonus.GivenBy(_building) > 0f;
            _decorTotal.text = Localization.Format("#card_decor_total", DecorBonus.Coins(working ? people * stats.residentCoins : 0f));
        }

        // ---------------------------------------------------------------- the confirm window

        /// <summary>«Улучшить»: the next level beside this one, what it adds, its price and how long it builds.</summary>
        public void OpenUpgrade()
        {
            var b = _building;
            if (b == null || b.GetUpgradeCost() == null || b.Site != null) return;
            _dialogKind = DialogKind.Upgrade;
            var next = b.Level + 1;
            dialogTitle.text = Localization.Format("#card_upgrade_title", next);
            ShowPictures(true);
            dialogNow.sprite = LevelIcon(b.Data.buildingName, b.Level);
            dialogNext.sprite = LevelIcon(b.Data.buildingName, next);
            Gains(b.Data, next);
            dialogText.gameObject.SetActive(false);
            dialogPriceCaption.text = Localization.Get("#card_price");
            CostRowBuilder.Build(dialogCostRow, b.GetUpgradeCost(), iconLibrary);
            dialogTime.gameObject.SetActive(true);
            dialogTime.text = Localization.Format("#card_build_time", Clock(b.Data.BuildSeconds(next)));
            Confirm("#building_upgrade", ConfirmGreen);
        }

        /// <summary>The hammer: the repair's price.</summary>
        public void OpenRepair()
        {
            var b = _building;
            if (b == null || b.GetRepairCost() == null) return;
            _dialogKind = DialogKind.Repair;
            dialogTitle.text = Localization.Get("#lab_repair_question");
            ShowPictures(false);
            dialogSingle.sprite = LevelIcon(b.Data.buildingName, b.Level);
            dialogText.gameObject.SetActive(true);
            dialogText.color = Ink;
            dialogText.text = Localization.Format("#lab_repair_gain", b.CurrentHealth, b.MaxHealth);
            dialogPriceCaption.text = Localization.Get("#card_price");
            CostRowBuilder.Build(dialogCostRow, b.GetRepairCost(), iconLibrary);
            dialogTime.gameObject.SetActive(false);
            Confirm("#building_repair", ConfirmGreen);
        }

        /// <summary>The bin: what comes back, and what is lost (a store's goods, a cemetery's crystals).</summary>
        public void OpenDemolish()
        {
            var b = _building;
            if (b == null || !b.CanDemolish) return;
            _dialogKind = DialogKind.Demolish;
            dialogTitle.text = Localization.Get("#lab_demolish_question");
            ShowPictures(false);
            dialogSingle.sprite = LevelIcon(b.Data.buildingName, b.Level);
            dialogText.gameObject.SetActive(true);
            dialogText.color = Ink;
            dialogText.text = owner.DemolishWarning();
            dialogPriceCaption.text = Localization.Get("#card_refund");
            CostRowBuilder.Build(dialogCostRow, b.GetDemolishRefund(), iconLibrary);
            dialogTime.gameObject.SetActive(false);
            Confirm("#lab_demolish_confirm", new Color(0.78f, 0.22f, 0.16f));
        }

        public void ConfirmDialog()
        {
            if (dialog != null) dialog.SetActive(false);
            switch (_dialogKind)
            {
                case DialogKind.Upgrade: owner.Upgrade(); break;
                case DialogKind.Repair: owner.Repair(); break;
                default: owner.ConfirmDemolish(); break;
            }
        }

        public void CloseDialog()
        {
            if (dialog != null) dialog.SetActive(false);
        }

        private void ShowPictures(bool both)
        {
            dialogNow.gameObject.SetActive(both);
            dialogArrow.SetActive(both);
            dialogNext.gameObject.SetActive(both);
            dialogGains.gameObject.SetActive(both);
            dialogSingle.gameObject.SetActive(!both);
        }

        private void Confirm(string key, Color colour)
        {
            dialogConfirmLabel.text = Localization.Get(key);
            dialogConfirmLabel.color = Color.white;
            if (dialogConfirmLabel.GetComponent<Outline>() == null)
            {
                var edge = dialogConfirmLabel.gameObject.AddComponent<Outline>();
                edge.effectColor = new Color(0.12f, 0.22f, 0.06f, 0.85f);
                edge.effectDistance = new Vector2(2f, -2f);
            }
            if (dialogConfirmImage != null) dialogConfirmImage.color = colour;
            dialog.SetActive(true);
        }

        /// <summary>The chips: each number the next level raises, and by how much.</summary>
        private void Gains(BuildingData data, int next)
        {
            for (var i = dialogGains.childCount - 1; i >= 0; i--) Destroy(dialogGains.GetChild(i).gameObject);
            var now = data.LevelStats(next - 1);
            var then = data.LevelStats(next);
            Chip(heartGlyph, then.maxHealth - now.maxHealth);
            Chip(shieldGlyph, then.defense - now.defense);
            Chip(houseGlyph, then.housingCapacity - now.housingCapacity);
            Chip(peopleGlyph, then.maxWorkers - now.maxWorkers);
            var harvest = Mathf.RoundToInt((then.harvestRadius - now.harvestRadius) / Cell);
            Chip(radiusGlyph, Mathf.Max(harvest, then.serviceRadius - now.serviceRadius));
            // a store: what more it will hold, by resource (the user, 2026-11-19: the Кладовая's upgrade showed only its health)
            if (iconLibrary == null) return;
            var shown = 0;
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                if (shown >= 3 || !StoreLedger.Keeps(type)) continue;   // the plate fits four chips with the heart
                var more = StoreLedger.CapOf(data.buildingName, type, next) - StoreLedger.CapOf(data.buildingName, type, next - 1);
                if (more <= 0) continue;
                Chip(iconLibrary.GetIcon(type), more, Color.white);
                shown++;
            }
        }

        private void Chip(Sprite glyph, int amount) => Chip(glyph, amount, new Color(1f, 0.9f, 0.7f));

        private void Chip(Sprite glyph, int amount, Color tint)
        {
            if (amount <= 0) return;
            var plate = NewImage(dialogGains, "Gain", rowSprite, new Color(0.43f, 0.29f, 0.16f, 0.95f));
            plate.type = Image.Type.Sliced;
            var layout = plate.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 124f;
            layout.preferredHeight = 48f;
            var icon = NewImage(plate.transform, "Icon", glyph, tint);
            icon.preserveAspect = true;
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            icon.rectTransform.anchoredPosition = new Vector2(-32f, 0f);
            icon.rectTransform.sizeDelta = new Vector2(32f, 32f);
            var text = Label(plate.transform, $"+{amount}", 26, Color.white, 0f, 0f, TextAnchor.MiddleLeft);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            text.rectTransform.anchoredPosition = new Vector2(20f, 0f);
            text.rectTransform.sizeDelta = new Vector2(70f, 40f);
        }

        /// <summary>The level's picture; a building drawn once for all its levels shows the nearest one below.</summary>
        private Sprite LevelIcon(string buildingId, int level)
        {
            for (var l = level; l >= 1; l--)
            {
                var key = l >= 2 ? $"{buildingId}_L{l}" : buildingId;
                for (var i = 0; i < levelIconKeys.Length && i < levelIcons.Length; i++)
                    if (levelIconKeys[i] == key) return levelIcons[i];
                var later = LevelIconKit.Find(key);   // a picture in the download pack
                if (later != null) return later;
            }
            return null;
        }

        // ---------------------------------------------------------------- row helpers

        private RectTransform Row(float height)
        {
            var row = new GameObject("Row", typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            row.SetParent(body, false);
            var layout = row.GetComponent<LayoutElement>();
            layout.preferredHeight = layout.minHeight = height;
            return row;
        }

        /// <summary>A glyph, a label on the left and a value on the right.</summary>
        private Text Stat(Sprite glyph, string key, string label = null)
        {
            var row = Row(42f);
            var icon = NewImage(row, "Icon", glyph, Ink);
            Pin(icon.rectTransform, 0f, 32f, 32f);
            Label(row, label ?? Localization.Get(key), 23, Ink, 46f, 480f, TextAnchor.MiddleLeft);
            return Label(row, string.Empty, 25, Ink, 0f, 220f, TextAnchor.MiddleRight, right: true);
        }

        private static Image NewImage(Transform parent, string name, Sprite sprite, Color colour)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.sprite = sprite;
            image.color = colour;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A rect `x` from its parent's left edge (or right, `right`), `width` wide, the row's height.</summary>
        private static Text Label(Transform parent, string content, int size, Color colour, float x, float width, TextAnchor align, bool right = false)
        {
            var text = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = UiTheme.Bold;
            text.fontSize = size;
            text.color = colour;
            text.alignment = align;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = content;
            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(right ? 1f : 0f, 0f);
            rect.anchorMax = new Vector2(right ? 1f : 0f, 1f);
            rect.pivot = new Vector2(right ? 1f : 0f, 0.5f);
            rect.anchoredPosition = new Vector2(right ? -x : x, 0f);
            rect.sizeDelta = new Vector2(width, 0f);
            return text;
        }

        private static void Pin(RectTransform rect, float x, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(w, h);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private Button RoundButton(Transform row, string mark, float x, UnityEngine.Events.UnityAction action)
        {
            var image = NewImage(row, "Button", buttonSprite, Color.white);
            image.preserveAspect = false;
            image.type = Image.Type.Sliced;
            image.raycastTarget = true;
            Pin(image.rectTransform, x, 64f, 64f);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            var label = Label(image.transform, mark, 40, new Color(0.27f, 0.16f, 0.07f), 0f, 0f, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            return button;
        }
    }
}
