using System;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Shop
{
    /// <summary>
    /// The sandbox boosters (the user, 2026-11-12; card Design/features/monetization.md): the player's
    /// stock of each and the time each one switched on has left. One stock for the account -- every
    /// sandbox map shares it -- kept on the device (PlayerPrefs). A running booster's time goes down
    /// ONLY while a sandbox game is played (BoosterShop ticks it with the game's own clock, so a pause
    /// stops it too); «Продлить» adds one more full duration. Off outside the sandbox (OfferShop.Allowed):
    /// an Invasion run or the tutorial never feels a booster.
    ///
    /// What each one does is here, by its sheet id; how strong, how long and how dear is the sheet's
    /// (BalanceConfig.Boosters, «Песочница (Лавка): Бустеры»). The game asks the helpers at the bottom.
    /// </summary>
    public static class Boosters
    {
        public const string Lumberjack = "lumberjack";
        public const string Stonecutter = "stonecutter";
        public const string Abundance = "abundance";
        public const string Treasury = "treasury";
        public const string Architect = "architect";
        public const string Scholar = "scholar";
        public const string BattleSpirit = "battle_spirit";
        public const string Fortitude = "fortitude";
        public const string Recruit = "recruit";
        public const string Repair = "repair";
        public const string Settlers = "settlers";

        private const string StockPref = "booster.stock.";
        private const string LeftPref = "booster.left.";
        /// <summary>How often a running booster's time left is written down (not every frame).</summary>
        private const float SaveEverySeconds = 5f;

        /// <summary>A stock or a running time changed (a booster bought, switched on, or run out): redraw.</summary>
        public static event Action Changed;
        /// <summary>A booster was switched on (its id): the HUD flashes, the auras appear.</summary>
        public static event Action<string> Used;

        private static readonly Dictionary<string, float> _left = new Dictionary<string, float>();
        private static bool _loaded;
        private static float _saveIn = SaveEverySeconds;

        private static BalanceConfig Config => BalanceConfig.Sandbox;

        public static IReadOnlyList<BalanceConfig.BoosterDef> All => Config != null ? Config.Boosters : (IReadOnlyList<BalanceConfig.BoosterDef>)Array.Empty<BalanceConfig.BoosterDef>();

        public static BalanceConfig.BoosterDef Def(string id) => Config != null ? Config.Booster(id) : null;

        /// <summary>Boosters work here: a sandbox game (not a run, the tutorial or a dungeon).</summary>
        public static bool Here => OfferShop.Allowed;

        public static int Stock(string id) => PlayerPrefs.GetInt(StockPref + id, 0);

        public static void AddStock(string id, int amount)
        {
            if (string.IsNullOrEmpty(id) || amount == 0) return;
            PlayerPrefs.SetInt(StockPref + id, Mathf.Max(0, Stock(id) + amount));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>Seconds of play this booster still has (0 = not running).</summary>
        public static float Left(string id)
        {
            Load();
            return _left.TryGetValue(id, out var left) ? left : 0f;
        }

        /// <summary>Running and felt: switched on, time left, in a sandbox game.</summary>
        public static bool IsActive(string id) => Left(id) > 0f && Here;

        /// <summary>Lasts a while (not an at-once one like Ремонт or Новосёлы).</summary>
        public static bool Lasts(string id)
        {
            var def = Def(id);
            return def != null && def.durationSeconds > 0f;
        }

        /// <summary>
        /// Takes one from the stock and switches it on: an at-once booster does its thing now, a lasting one
        /// starts (or, running, gets one more full duration -- «Продлить»). False with none in stock or outside the sandbox.
        /// </summary>
        public static bool Use(string id)
        {
            var def = Def(id);
            if (def == null || !Here || Stock(id) <= 0) return false;
            PlayerPrefs.SetInt(StockPref + id, Stock(id) - 1);
            if (def.durationSeconds <= 0f) DoAtOnce(id, def);
            else
            {
                Load();
                _left[id] = Left(id) + def.durationSeconds;
                SaveLeft();
            }
            PlayerPrefs.Save();
            EventLogManager.Instance?.Post(GameEvent.ForSettlement(Localization.Format("#booster_used", Localization.Get(def.nameKey)),
                EventSeverity.Good, EventGlyph.Star, null));
            Used?.Invoke(id);
            Changed?.Invoke();
            return true;
        }

        /// <summary>The running boosters' clocks, by the game's own time (BoosterShop.Update, a sandbox game only).</summary>
        public static void Tick(float dt)
        {
            if (dt <= 0f) return;
            Load();
            var ended = false;
            var any = false;
            foreach (var def in All)
            {
                if (!_left.TryGetValue(def.id, out var left) || left <= 0f) continue;
                any = true;
                left -= dt;
                if (left <= 0f)
                {
                    left = 0f;
                    ended = true;
                }
                _left[def.id] = left;
            }
            if (!any) return;
            _saveIn -= dt;
            if (ended || _saveIn <= 0f) SaveLeft();
            if (ended) Changed?.Invoke();
        }

        /// <summary>Writes the running times down now (the game leaving, the app going to the background).</summary>
        public static void SaveLeft()
        {
            _saveIn = SaveEverySeconds;
            foreach (var pair in _left) PlayerPrefs.SetFloat(LeftPref + pair.Key, pair.Value);
            PlayerPrefs.Save();
        }

        private static void Load()
        {
            if (_loaded || Config == null) return;
            _loaded = true;
            foreach (var def in All) _left[def.id] = PlayerPrefs.GetFloat(LeftPref + def.id, 0f);
        }

        private static void DoAtOnce(string id, BalanceConfig.BoosterDef def)
        {
            switch (id)
            {
                case Repair:
                    foreach (var building in UnityEngine.Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
                        building.RepairFree();
                    break;
                case Settlers:
                    // may go past the houses' room: then they leave one by one (HousePeople.Overcrowded)
                    CitizenManager.Instance?.AddCitizens(Mathf.Max(0, Mathf.RoundToInt(def.effect)));
                    break;
            }
        }

        // ---------------------------------------------------------------- what the game asks

        private static float Effect(string id, float fallback)
        {
            var def = Def(id);
            return def != null && def.effect > 0f ? def.effect : fallback;
        }

        /// <summary>How much faster a citizen's own work on this resource goes (the chop, the dig -- never the walk).</summary>
        public static float WorkSpeed(ResourceType type)
        {
            if (type == ResourceType.Wood && IsActive(Lumberjack)) return Effect(Lumberjack, 1f);
            if (type == ResourceType.Stone && IsActive(Stonecutter)) return Effect(Stonecutter, 1f);
            return 1f;
        }

        /// <summary>Food added to every food building's day.</summary>
        public static int FoodBonus => IsActive(Abundance) ? Mathf.RoundToInt(Effect(Abundance, 0f)) : 0;

        public static float TaxFactor => IsActive(Treasury) ? Effect(Treasury, 1f) : 1f;

        public static bool InstantBuild => IsActive(Architect);

        public static bool InstantResearch => IsActive(Scholar);

        public static float ArmyDamageFactor => IsActive(BattleSpirit) ? 1f + Effect(BattleSpirit, 0f) : 1f;

        /// <summary>A player's building takes this share of a blow (Стойкость: strength +25 % = a blow / 1.25).</summary>
        public static float BuildingDamageFactor => IsActive(Fortitude) ? 1f / (1f + Effect(Fortitude, 0f)) : 1f;

        public static float HireCostFactor => IsActive(Recruit) ? Mathf.Clamp(Effect(Recruit, 1f), 0f, 1f) : 1f;

        // ---------------------------------------------------------------- words

        /// <summary>What it does, its strength put in (#booster_desc_&lt;id&gt;: ×1,5 / +1 / +50 % / в 2 раза / +20).</summary>
        public static string Describe(BalanceConfig.BoosterDef def)
        {
            if (def == null) return string.Empty;
            string value;
            switch (def.id)
            {
                case BattleSpirit:
                case Fortitude:
                    value = Mathf.RoundToInt(def.effect * 100f).ToString();
                    break;
                case Recruit:
                    value = Number(def.effect > 0f ? 1f / def.effect : 1f);
                    break;
                default:
                    value = Number(def.effect);
                    break;
            }
            return Localization.Format(def.descKey, value);
        }

        /// <summary>«3 мин» / «сразу».</summary>
        public static string Duration(BalanceConfig.BoosterDef def) =>
            def == null || def.durationSeconds <= 0f ? Localization.Get("#booster_instant")
                : Localization.Format("#booster_minutes", Number(def.durationSeconds / 60f));

        /// <summary>A number the way the language writes it: 1,5 in Russian, 1.5 in English, no trailing zeros.</summary>
        public static string Number(float v)
        {
            var text = v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            return Localization.Language == "ru" ? text.Replace('.', ',') : text;
        }
    }
}
