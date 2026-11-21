using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.UI
{
    /// <summary>
    /// The special buildings' blocks on the v2 card (the user approved the concepts 2026-11-17, «Утверждаю»:
    /// Design/ui/Special_*.png, Tools/ui/special_cards_concepts.py). Each is laid out by BuildSpecial once per
    /// building and level, and its numbers refilled by RefreshSpecial with the rest of the card.
    /// </summary>
    public partial class BuildingInfoCard
    {
        [Header("The special buildings")]
        [SerializeField] private string[] portraitKeys = new string[0];
        [SerializeField] private Sprite[] portraits = new Sprite[0];
        [SerializeField] private Sprite lockGlyph;
        [SerializeField] private Sprite checkGlyph;
        [SerializeField] private Sprite skullGlyph;
        [SerializeField] private Sprite sickGlyph;
        [SerializeField] private Sprite crystalIcon;

        private static readonly Color Violet = new Color(0.59f, 0.39f, 0.78f);

        // the special rows' live parts
        private Text _hallPeople, _army, _unitPlaces, _waiting, _taxPeople, _taxTotal, _bodies, _stance, _trade, _mineStored, _collectLabel;
        private readonly List<Image> _beds = new List<Image>();
        private Image _gravesFill, _relationsFill, _mineFill;
        private Text _gravesText, _relationsText;
        private RectTransform _metalRow;
        private GameObject _collect;

        /// <summary>Where a Плавильня's metal buttons go (BuildingInfoPanelController builds them).</summary>
        public RectTransform MetalRow => _metalRow;

        private string Id => _building.Data.buildingName;
        private bool IsHall => Id == BuildingIds.TownHall;

        private void ClearSpecial()
        {
            _hallPeople = _army = _unitPlaces = _waiting = _taxPeople = _taxTotal = _bodies = _stance = _trade = _mineStored = _collectLabel = null;
            _beds.Clear();
            _gravesFill = _relationsFill = _mineFill = null;
            _gravesText = _relationsText = null;
            _metalRow = null;
            _collect = null;
        }

        /// <summary>The «what it is for» line of a special building, or null for an ordinary one.</summary>
        private string SpecialWhat()
        {
            switch (Id)
            {
                case BuildingIds.TownHall: return "#card_what_hall";
                case "Barracks": return "#card_what_barracks";
                case "Stable": return "#card_what_stable";
                case "Workshop": return "#card_what_workshop";
                case "Shipyard": return "#card_what_shipyard";
                case "Lighthouse": return "#card_what_lighthouse";
                case Infirmary.HealerId: return "#card_what_healer";
                case "FireBrigade": return "#card_what_firebrigade";
                case "TaxHouse": return "#card_what_taxhouse";
                case BuildingIds.Cemetery: return "#card_what_cemetery";
                case "DiplomacyHall": return "#card_what_diplomacy";
                case "Smelter": return "#card_what_smelter";
                case "CrystalMine": return "#card_what_crystalmine";
                case BuildingIds.HunterHut: return "#card_what_hunterhut";
                default: return null;
            }
        }

        /// <summary>What the workers row is called: «Лекари», «Пожарные», else «Рабочие».</summary>
        private string WorkersKey => Id == Infirmary.HealerId ? "#card_healers" : Id == "FireBrigade" ? "#card_firefighters" : "#card_workers";

        /// <summary>A healer, a brigade, a cemetery: no band of what it makes -- their own block says it.</summary>
        private bool HidesBand => Id == Infirmary.HealerId || Id == "FireBrigade" || Id == BuildingIds.Cemetery || Id == "CrystalMine";

        /// <summary>The blocks laid before the ordinary ones (the hall's people) -- see BuildBody.</summary>
        private void BuildSpecialTop()
        {
            if (!IsHall) return;
            var places = HousePeople.HallPlaces(_building);
            if (places <= 0) return;
            var row = Row(40f);
            Label(row, Localization.Get("#card_hall_residents"), 24, Ink, 0f, 480f, TextAnchor.MiddleLeft);
            _hallPeople = Label(row, string.Empty, 26, Ink, 0f, 200f, TextAnchor.MiddleRight, right: true);
        }

        /// <summary>The blocks laid after the ordinary ones.</summary>
        private void BuildSpecial()
        {
            if (IsHall) Heroes();
            Hires();
            switch (Id)
            {
                case "Shipyard": ShipTiles(); break;
                case "Lighthouse": Lighthouse(); break;
                case Infirmary.HealerId: Beds(); break;
                case "FireBrigade": FirePerks(); break;
                case "TaxHouse": Tax(); break;
                case BuildingIds.Cemetery: Graves(); break;
                case "DiplomacyHall": Relations(); break;
                case "CrystalMine": Mine(); break;
            }
        }

        // ---------------------------------------------------------------- the blocks

        private void Heroes()
        {
            var roster = HeroRoster.Instance;
            if (roster == null) return;
            Label(Row(32f), Localization.Get("#card_heroes"), 23, Ink, 0f, BodyWidth, TextAnchor.MiddleLeft);
            var tiles = Row(128f);
            var i = 0;
            foreach (var type in new[] { SoldierType.King, SoldierType.Queen, SoldierType.Captain })
            {
                var offer = roster.CanBuy(type);
                var balance = BalanceConfig.Instance.Hero(SoldierStats.SheetIdOf(type));
                var state = offer == HeroRoster.Offer.Owned ? -1 : offer == HeroRoster.Offer.CastleTooLow && balance != null ? balance.castleLevel : 0;
                Tile(tiles, i++, 100f, type.ToString(), Localization.Get("#hero_" + type.ToString().ToLowerInvariant()), state);
            }
            Label(Row(26f), Localization.Get("#card_heroes_note"), 19, Soft, 0f, BodyWidth, TextAnchor.MiddleLeft);
            Label(Row(26f), Localization.Get("#card_hall_fall"), 19, Warn, 0f, BodyWidth, TextAnchor.MiddleLeft);
        }

        /// <summary>The Barracks' soldiers, the Stable's knight, the Church's monk, the Workshop's machines: each opened by a level.</summary>
        private void Hires()
        {
            var types = new List<SoldierType>();
            foreach (var type in SoldierStats.Hireable)
                if (SoldierStats.HiredAt(type) == Id) types.Add(type);
            if (types.Count == 0) return;

            var machines = SoldierStats.IsMachine(types[0]);
            Label(Row(32f), Localization.Get(machines ? "#card_builds_machines" : "#card_trains"), 23, Ink, 0f, BodyWidth, TextAnchor.MiddleLeft);
            var size = types.Count > 4 ? 92f : 104f;
            var tiles = Row(size + 26f);
            for (var i = 0; i < types.Count; i++)
            {
                var need = SoldierStats.BarracksLevel(types[i]);
                Tile(tiles, i, size, types[i].ToString(), SoldierStats.DisplayName(types[i]), _building.Level < need ? need : 0);
            }
            var army = ArmyManager.Instance;
            if (types.Count == 1 && SoldierStats.CapPerLevel(types[0]) > 0)
            {
                _unitPlaces = Stat(peopleGlyph, "#card_unit_places");
                Label(Row(26f), Localization.Get("#card_each_level_place"), 19, Soft, 0f, BodyWidth, TextAnchor.MiddleLeft);
            }
            else
            {
                if (!machines && army != null) _army = Stat(peopleGlyph, "#card_army_count");
                Label(Row(26f), Localization.Get(machines ? "#card_each_level_machine" : "#card_each_level_soldier"), 19, Soft, 0f, BodyWidth, TextAnchor.MiddleLeft);
            }
            Label(Row(26f), Localization.Get("#card_hire_note"), 19, Soft, 0f, BodyWidth, TextAnchor.MiddleLeft);
        }

        private void ShipTiles()
        {
            Label(Row(32f), Localization.Get("#card_builds_ships"), 23, Ink, 0f, BodyWidth, TextAnchor.MiddleLeft);
            var tiles = Row(130f);
            var all = Ships.ShipStats.All;
            for (var i = 0; i < all.Length; i++)
            {
                var need = Ships.ShipStats.ShipyardLevel(all[i]);
                Tile(tiles, i, 104f, "Ship" + all[i], Localization.Get("#unit_" + Ships.ShipStats.SheetId(all[i])), _building.Level < need ? need : 0);
            }
            Label(Row(28f), Localization.Get("#card_ship_repair"), 20, Ink, 0f, BodyWidth, TextAnchor.MiddleLeft);
            Label(Row(26f), Localization.Get("#card_shipyard_shallows"), 19, Soft, 0f, BodyWidth, TextAnchor.MiddleLeft);
        }

        private void Lighthouse()
        {
            var config = BalanceConfig.Instance;
            if (config == null) return;
            Label(Row(32f), Localization.Get("#card_light_caption"), 22, Ink, 0f, BodyWidth, TextAnchor.MiddleLeft);
            var band = Plate(62f);
            var icon = NewImage(band, "Icon", radiusGlyph, Ink);
            Pin(icon.rectTransform, 16f, 36f, 36f);
            Label(band, Localization.Format("#card_light_radius", config.LighthouseLightCells(_building.Level)), 25, Ink, 64f, 440f, TextAnchor.MiddleLeft);
            if (_building.Level < BuildingInstance.MaxLevel)
                Label(band, Localization.Format("#card_next_level_cells", _building.Level + 1, config.LighthouseLightCells(_building.Level + 1)),
                    20, Soft, 16f, 220f, TextAnchor.MiddleRight, right: true);
            Label(Row(26f), Localization.Get("#card_light_note"), 19, Soft, 0f, BodyWidth, TextAnchor.MiddleLeft);
        }

        private void Beds()
        {
            var row = Row(34f);
            Label(row, Localization.Get("#card_beds"), 23, Ink, 0f, 400f, TextAnchor.MiddleLeft);
            var count = Label(row, string.Empty, 25, Ink, 0f, 200f, TextAnchor.MiddleRight, right: true);
            count.name = "BedCount";
            var beds = Row(58f);
            var n = Mathf.Min(8, Infirmary.BedsOf(_building));
            for (var i = 0; i < n; i++)
            {
                var bed = NewImage(beds, "Bed", rowSprite, new Color(0.92f, 0.84f, 0.67f));
                bed.type = Image.Type.Sliced;
                bed.preserveAspect = false;
                Pin(bed.rectTransform, i * 92f, 80f, 52f);
                var mark = NewImage(bed.transform, "Sick", sickGlyph, new Color(0.67f, 0.27f, 0.2f));
                mark.rectTransform.anchorMin = mark.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                mark.rectTransform.sizeDelta = new Vector2(34f, 34f);
                _beds.Add(mark);
            }
            _waiting = Stat(peopleGlyph, "#card_waiting_bed");
            var config = BalanceConfig.Instance;
            if (config != null)
                Label(Row(26f), Localization.Format("#card_healer_rule", Mathf.RoundToInt(config.InfirmaryTreatSeconds), Mathf.RoundToInt(config.InfirmaryWaitSeconds)),
                    19, Soft, 0f, BodyWidth, TextAnchor.MiddleLeft);
        }

        private void FirePerks()
        {
            Label(Row(28f), Localization.Get("#card_fire_island"), 20, Soft, 0f, BodyWidth, TextAnchor.MiddleLeft);
            for (var level = 2; level <= BuildingInstance.MaxLevel; level++)
            {
                var got = _building.Level >= level;
                var row = Row(38f);
                var mark = NewImage(row, "Mark", got ? checkGlyph : lockGlyph, got ? Good : Empty);
                Pin(mark.rectTransform, 2f, 28f, 28f);
                Label(row, Localization.Format("#card_level_short", level), 19, Soft, 42f, 80f, TextAnchor.MiddleLeft);
                Label(row, Localization.Get($"#card_fire_l{level}"), 21, got ? Ink : Soft, 120f, BodyWidth - 120f, TextAnchor.MiddleLeft);
            }
        }

        private void Tax()
        {
            var config = BalanceConfig.Instance;
            if (config == null) return;
            Label(Row(32f), Localization.Get("#card_tax_caption"), 22, Ink, 0f, BodyWidth, TextAnchor.MiddleLeft);
            var band = Plate(62f);
            var coin = NewImage(band, "Icon", iconLibrary != null ? iconLibrary.GetIcon(ResourceType.Coins) : null, Color.white);
            Pin(coin.rectTransform, 14f, 44f, 44f);
            var bonus = config.TaxHouseBonusPerLevel * _building.Level;
            Label(band, Localization.Format("#card_tax_formula", DecorBonus.Coins(config.CoinsPerCitizenPerDay), DecorBonus.Coins(bonus),
                DecorBonus.Coins(TaxManager.PerCitizenAt(_building.Level))), 25, Ink, 70f, 420f, TextAnchor.MiddleLeft);
            Label(band, Localization.Format("#card_tax_per_level", DecorBonus.Coins(config.TaxHouseBonusPerLevel)), 19, Soft, 16f, 220f, TextAnchor.MiddleRight, right: true);
            _taxPeople = Stat(peopleGlyph, "#card_town_people");
            var total = Row(36f);
            var totalCoin = NewImage(total, "Icon", iconLibrary != null ? iconLibrary.GetIcon(ResourceType.Coins) : null, Color.white);
            Pin(totalCoin.rectTransform, 0f, 30f, 30f);
            _taxTotal = Label(total, string.Empty, 22, Good, 44f, BodyWidth - 44f, TextAnchor.MiddleLeft);
            Label(Row(26f), Localization.Get("#card_tax_best"), 19, Soft, 0f, BodyWidth, TextAnchor.MiddleLeft);
            Label(Row(26f), Localization.Get("#card_tax_lab"), 19, Soft, 0f, BodyWidth, TextAnchor.MiddleLeft);
        }

        private void Graves()
        {
            Label(Row(32f), Localization.Get("#card_graves"), 23, Ink, 0f, BodyWidth, TextAnchor.MiddleLeft);
            (_gravesFill, _gravesText) = Bar(BarGood);
            _bodies = Stat(skullGlyph, "#card_bodies");
            var config = BalanceConfig.Instance;
            if (config != null)
            {
                Label(Row(24f), Localization.Format("#card_body_sickness", Mathf.RoundToInt(config.RemainsSicknessPerBody * 100f)), 19, Warn, 46f, BodyWidth - 46f, TextAnchor.MiddleLeft);
                Label(Row(26f), Localization.Get("#card_bodies_self"), 19, Soft, 0f, BodyWidth, TextAnchor.MiddleLeft);
                Label(Row(26f), Localization.Format("#card_cemetery_demolish", config.CemeteryDemolishCrystals), 19, Soft, 0f, BodyWidth, TextAnchor.MiddleLeft);
            }
        }

        private void Relations()
        {
            Label(Row(32f), Localization.Get("#card_neighbour"), 23, Ink, 0f, BodyWidth, TextAnchor.MiddleLeft);
            var band = Plate(76f);
            var face = NewImage(band, "Face", PortraitOf("KingNeighbour"), Color.white);
            Pin(face.rectTransform, 8f, 64f, 64f);
            var who = Label(band, Localization.Get("#card_neighbour_king"), 23, Ink, 84f, 500f, TextAnchor.UpperLeft);
            who.rectTransform.offsetMin = new Vector2(who.rectTransform.offsetMin.x, 8f);
            who.rectTransform.offsetMax = new Vector2(who.rectTransform.offsetMax.x, -8f);
            _stance = Label(band, string.Empty, 20, Good, 84f, 500f, TextAnchor.LowerLeft);
            _stance.rectTransform.offsetMin = new Vector2(_stance.rectTransform.offsetMin.x, 8f);
            _stance.rectTransform.offsetMax = new Vector2(_stance.rectTransform.offsetMax.x, -8f);
            Label(Row(26f), Localization.Get("#card_relations"), 21, Ink, 0f, BodyWidth, TextAnchor.MiddleLeft);
            (_relationsFill, _relationsText) = Bar(BarGood);
            var config = BalanceConfig.Instance;
            _trade = Stat(houseGlyph, null, Localization.Format("#card_trade_line", Mathf.RoundToInt(config != null ? config.BotTradeMinScore : 30f)));
            var row = Row(58f);
            var button = NewImage(row, "OpenDiplomacy", buttonSprite, Color.white);
            button.type = Image.Type.Sliced;
            button.preserveAspect = false;
            button.raycastTarget = true;
            button.rectTransform.anchorMin = button.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            button.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            button.rectTransform.anchoredPosition = Vector2.zero;
            button.rectTransform.sizeDelta = new Vector2(380f, 56f);
            var b = button.gameObject.AddComponent<Button>();
            b.targetGraphic = button;
            var hall = _building;
            b.onClick.AddListener(() =>
            {
                owner.Close();
                if (DiplomacyWindow.Instance != null) DiplomacyWindow.Instance.Show(hall);
            });
            var label = Label(button.transform, Localization.Get("#card_open_diplomacy"), 23, Ink, 0f, 0f, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
        }

        private void Mine()
        {
            var mines = CrystalMines.Instance;
            var config = BalanceConfig.Instance;
            if (mines == null || mines.Mine != _building || config == null) return;
            Label(Row(32f), Localization.Get("#card_mine_stored"), 23, Ink, 0f, BodyWidth, TextAnchor.MiddleLeft);
            (_mineFill, _mineStored) = Bar(Violet);
            var band = Plate(62f);
            var icon = NewImage(band, "Icon", crystalIcon, Color.white);
            Pin(icon.rectTransform, 14f, 44f, 44f);
            Label(band, Localization.Format("#card_mine_pace", Mathf.RoundToInt(config.CrystalMineSeconds(_building.Level))), 24, Ink, 70f, 420f, TextAnchor.MiddleLeft);
            if (_building.Level < BuildingInstance.MaxLevel)
                Label(band, Localization.Format("#card_next_level_sec", _building.Level + 1, Mathf.RoundToInt(config.CrystalMineSeconds(_building.Level + 1))),
                    19, Soft, 16f, 200f, TextAnchor.MiddleRight, right: true);
            Label(Row(26f), Localization.Get("#card_mine_full_note"), 19, Soft, 0f, BodyWidth, TextAnchor.MiddleLeft);
            var row = Row(60f);
            var button = NewImage(row, "Collect", buttonSprite, new Color(0.45f, 0.75f, 0.35f));
            button.type = Image.Type.Sliced;
            button.preserveAspect = false;
            button.raycastTarget = true;
            button.rectTransform.anchorMin = button.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            button.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            button.rectTransform.anchoredPosition = Vector2.zero;
            button.rectTransform.sizeDelta = new Vector2(320f, 58f);
            var b = button.gameObject.AddComponent<Button>();
            b.targetGraphic = button;
            b.onClick.AddListener(() =>
            {
                CrystalMines.Instance?.Collect();
                Refresh(null, false);
            });
            _collect = button.gameObject;
            _collectLabel = Label(button.transform, string.Empty, 24, Color.white, 0f, 0f, TextAnchor.MiddleCenter);
            Stretch(_collectLabel.rectTransform);
        }

        /// <summary>A Плавильня's metal buttons: a row the controller fills (RefreshRecipeControls).</summary>
        private void MetalButtons()
        {
            if (_work == null || !_work.HasRecipeChoice) return;
            Label(Row(28f), Localization.Get("#card_what_to_smelt"), 22, Ink, 0f, BodyWidth, TextAnchor.MiddleLeft);
            _metalRow = Row(62f);
        }

        // ---------------------------------------------------------------- the numbers

        private void RefreshSpecial()
        {
            if (_hallPeople != null)
                _hallPeople.text = $"{(HousePeople.Instance != null ? HousePeople.Instance.CitizensOf(_building) : 0)} / {HousePeople.HallPlaces(_building)}";
            var army = ArmyManager.Instance;
            if (_army != null && army != null) _army.text = army.SoldierCount.ToString();
            if (_unitPlaces != null && army != null)
            {
                foreach (var type in SoldierStats.Hireable)
                {
                    if (SoldierStats.HiredAt(type) != Id) continue;
                    _unitPlaces.text = $"{army.CountOf(type)} / {SoldierStats.CapPerLevel(type) * _building.Level}";
                    break;
                }
            }
            if (_beds.Count > 0 && Infirmary.Instance != null)
            {
                var taken = Infirmary.Instance.InBed(_building, new List<Infirmary.Patient>()).Count;
                for (var i = 0; i < _beds.Count; i++) _beds[i].gameObject.SetActive(i < taken);
                foreach (Transform row in body)
                {
                    var t = row.Find("BedCount");
                    if (t != null) t.GetComponent<Text>().text = $"{taken} / {Infirmary.BedsOf(_building)}";
                }
                var island = HousePeople.IslandOf(_building);
                var waiting = 0;
                foreach (var p in Infirmary.Instance.Patients)
                    if (p.Bed == null && HousePeople.Same(p.Island, island)) waiting++;
                _waiting.text = waiting.ToString();
                _waiting.color = waiting > 0 ? Warn : Ink;
            }
            if (_taxPeople != null)
            {
                var people = CitizenManager.Instance != null ? CitizenManager.Instance.TotalPopulation : 0;
                _taxPeople.text = people.ToString();
                _taxTotal.text = Localization.Format("#card_tax_total", TaxManager.ComputeDailyIncome(people, TaxManager.TaxHouseLevel()));
            }
            if (_gravesFill != null && Remains.Instance != null)
            {
                var have = Remains.Instance.GravesIn(_building);
                var room = Mathf.Max(1, Remains.CapacityOf(_building));
                _gravesFill.fillAmount = Mathf.Clamp01(have / (float)room);
                _gravesFill.color = have >= room ? BarFull : BarGood;
                _gravesText.text = $"{have} / {Remains.CapacityOf(_building)}";
                var bodies = Remains.Instance.BodiesOn(HousePeople.IslandOf(_building));
                _bodies.text = bodies.ToString();
                _bodies.color = bodies > 0 ? Warn : Ink;
            }
            if (_relationsFill != null)
            {
                var score = Diplomacy.BotRelations.Score;
                _relationsFill.fillAmount = Mathf.InverseLerp(Diplomacy.BotRelations.Min, Diplomacy.BotRelations.Max, score);
                _relationsFill.color = score < 0f ? BarFull : BarGood;
                _relationsText.text = (score > 0f ? "+" : string.Empty) + Mathf.RoundToInt(score);
                var stance = Diplomacy.BotRelations.Stance;
                _stance.text = Localization.Format("#card_stance", Localization.Get(DiplomacyWindow.StanceKey(stance)));
                _stance.color = stance == Diplomacy.BotStance.War ? Warn : Good;
                var config = BalanceConfig.Instance;
                var open = stance == Diplomacy.BotStance.Trade
                           || (_building.Level >= 2 && score >= (config != null ? config.BotTradeMinScore : 30f));
                _trade.text = Localization.Get(open ? "#card_trade_open" : "#card_trade_closed");
                _trade.color = open ? Good : Soft;
            }
            if (_mineFill != null && CrystalMines.Instance != null)
            {
                var mines = CrystalMines.Instance;
                var whole = Mathf.FloorToInt(mines.Stored);
                _mineFill.fillAmount = mines.Cap > 0 ? Mathf.Clamp01(mines.Stored / mines.Cap) : 0f;
                _mineStored.text = $"{whole} / {mines.Cap}";
                _collect.SetActive(whole > 0);
                _collectLabel.text = Localization.Format("#card_mine_collect", whole);
            }
        }

        // ---------------------------------------------------------------- helpers

        private Sprite PortraitOf(string name)
        {
            var key = "Portrait" + name;
            for (var i = 0; i < portraitKeys.Length && i < portraits.Length; i++)
                if (portraitKeys[i] == key) return portraits[i];
            return null;
        }

        /// <summary>A unit tile: its portrait, its name under it; `state` -1 = had (a tick), 0 = open, N = needs level N (dimmed, a padlock).</summary>
        private void Tile(RectTransform row, int index, float size, string portrait, string name, int state)
        {
            const float gap = 20f;
            var tile = NewImage(row, "Tile", rowSprite, state > 0 ? new Color(0.78f, 0.72f, 0.62f) : new Color(0.89f, 0.8f, 0.63f));
            tile.type = Image.Type.Sliced;
            tile.preserveAspect = false;
            var rect = tile.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(index * (size + gap), 0f);
            rect.sizeDelta = new Vector2(size, size);
            var face = NewImage(tile.transform, "Face", PortraitOf(portrait), state > 0 ? new Color(1f, 1f, 1f, 0.5f) : Color.white);
            face.rectTransform.anchorMin = face.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            face.rectTransform.sizeDelta = new Vector2(size - 14f, size - 14f);
            var caption = Label(tile.transform, name, 15, Ink, 0f, size + gap, TextAnchor.UpperCenter);
            caption.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            caption.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            caption.rectTransform.pivot = new Vector2(0.5f, 1f);
            caption.rectTransform.anchoredPosition = new Vector2(0f, -2f);
            caption.rectTransform.sizeDelta = new Vector2(size + gap, 22f);
            caption.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (state > 0)
            {
                var plate = NewImage(tile.transform, "Lock", rowSprite, new Color(0.43f, 0.29f, 0.16f));
                plate.type = Image.Type.Sliced;
                plate.preserveAspect = false;
                plate.rectTransform.anchorMin = new Vector2(0f, 0f);
                plate.rectTransform.anchorMax = new Vector2(1f, 0f);
                plate.rectTransform.pivot = new Vector2(0.5f, 0f);
                plate.rectTransform.offsetMin = new Vector2(8f, 6f);
                plate.rectTransform.offsetMax = new Vector2(-8f, 34f);
                var padlock = NewImage(plate.transform, "Icon", lockGlyph, new Color(1f, 0.93f, 0.8f));
                Pin(padlock.rectTransform, 6f, 18f, 18f);
                var level = Label(plate.transform, Localization.Format("#card_level_short", state), 16, new Color(1f, 0.93f, 0.8f), 26f, size - 40f, TextAnchor.MiddleLeft);
                level.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            else if (state < 0)
            {
                var tick = NewImage(tile.transform, "Tick", checkGlyph, Good);
                tick.rectTransform.anchorMin = tick.rectTransform.anchorMax = new Vector2(1f, 1f);
                tick.rectTransform.anchoredPosition = new Vector2(-6f, -6f);
                tick.rectTransform.sizeDelta = new Vector2(32f, 32f);
            }
        }

        /// <summary>A band with a plate behind it (the row the coins / light / pace lines sit on).</summary>
        private RectTransform Plate(float height)
        {
            var band = Row(height);
            var plate = NewImage(band, "Plate", rowSprite, new Color(0.55f, 0.38f, 0.2f, 0.25f));
            plate.type = Image.Type.Sliced;
            plate.preserveAspect = false;
            Stretch(plate.rectTransform);
            return band;
        }

        /// <summary>A bar the width of the body: its fill and its number.</summary>
        private (Image fill, Text text) Bar(Color colour)
        {
            var row = Row(36f);
            var back = NewImage(row, "Bar", barSprite, new Color(0.12f, 0.08f, 0.05f, 0.9f));
            back.preserveAspect = false;
            Stretch(back.rectTransform);
            var fill = NewImage(back.transform, "Fill", barSprite, colour);
            fill.preserveAspect = false;
            Stretch(fill.rectTransform);
            fill.rectTransform.offsetMin = new Vector2(3f, 3f);
            fill.rectTransform.offsetMax = new Vector2(-3f, -3f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            var text = Label(back.transform, string.Empty, 20, Color.white, 0f, 0f, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return (fill, text);
        }
    }
}
