using System;
using CityBuilder.Buildings;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Diplomacy
{
    /// <summary>Where the town stands with the bot kingdom (the user, 2026-11-03).</summary>
    public enum BotStance { Peace, Trade, War }

    /// <summary>
    /// The town's relations with the bot kingdom (the user, 2026-11-03): a scale from -100 to 100 and
    /// one of three stances -- Мир, Торговля, Война -- all of it closed until the player has built a
    /// Diplomacy Hall. The skeleton: the scale drifts back to 0 day by day, Trade needs
    /// bot_trade_min_score, declaring war costs bot_war_declare_score_loss. A war ends by paying a
    /// contribution (not designed yet) or by razing the bot's Town Hall (EndWar). Trade deals, the
    /// bot's own declarations and its gifts come later.
    /// </summary>
    public static class BotRelations
    {
        public const float Min = -100f, Max = 100f;

        public static float Score { get; private set; }
        public static BotStance Stance { get; private set; } = BotStance.Peace;

        /// <summary>Seconds of the war so far (0 at peace): the ransom and the raids grow with it.</summary>
        public static float WarSeconds { get; private set; }

        /// <summary>The LEVELS of the bot's buildings the town's army razed in this war: each level takes bot_war_contribution_per_building off the ransom (the user, 2026-11-05: a lvl-5 tower is worth more than a lvl-1 hovel).</summary>
        public static int WarRazed { get; private set; }

        /// <summary>Wars declared on the bot so far, this one counted: each new war starts its raids bot_war_start_wave_step waves further on (the user, 2026-11-05).</summary>
        public static int WarsFought { get; private set; }

        /// <summary>The town's army razed a bot building of `level` at war (BotWarTarget).</summary>
        public static void NoteRazed(int level)
        {
            if (Stance != BotStance.War) return;
            WarRazed += Mathf.Max(1, level);
            Changed?.Invoke();
        }

        /// <summary>The botwaves wave this war's first raid is: 1 in the first war, then bot_war_start_wave_step further per war.</summary>
        public static int FirstWave()
        {
            var step = BalanceConfig.Instance != null ? Mathf.Max(0, BalanceConfig.Instance.BotWarStartWaveStep) : 0;
            return 1 + Mathf.Max(0, WarsFought - 1) * step;
        }

        /// <summary>Anything on the scale or the stance changed: the Diplomacy window redraws.</summary>
        public static event Action Changed;

        /// <summary>A new game: the sheet's starting relations, at peace.</summary>
        public static void Reset()
        {
            Score = BalanceConfig.Instance != null ? Mathf.Clamp(BalanceConfig.Instance.BotRelationsStart, Min, Max) : 0f;
            Stance = BotStance.Peace;
            WarSeconds = 0f;
            WarRazed = 0;
            WarsFought = 0;
            Changed?.Invoke();
        }

        public static void Restore(float score, int stance, float warSeconds = 0f, int warRazed = 0, int warsFought = 0)
        {
            WarsFought = Mathf.Max(0, warsFought);
            Score = Mathf.Clamp(score, Min, Max);
            Stance = Enum.IsDefined(typeof(BotStance), stance) ? (BotStance)stance : BotStance.Peace;
            WarSeconds = Stance == BotStance.War ? Mathf.Max(0f, warSeconds) : 0f;
            WarRazed = Stance == BotStance.War ? Mathf.Max(0, warRazed) : 0;
            if (Stance == BotStance.War && WarsFought == 0) WarsFought = 1;
            Changed?.Invoke();
        }

        /// <summary>The war's clock runs (BotWar, every frame at war).</summary>
        public static void TickWar(float deltaSeconds)
        {
            if (Stance != BotStance.War) return;
            var before = Mathf.FloorToInt(WarSeconds / 60f);
            WarSeconds += deltaSeconds;
            // the ransom grows a step a minute: the window redraws its price
            if (Mathf.FloorToInt(WarSeconds / 60f) != before) Changed?.Invoke();
        }

        /// <summary>
        /// The ransom that ends the war (the user, 2026-11-04): bot_war_contribution_base coins plus
        /// bot_war_contribution_per_min for every whole minute of war, less
        /// bot_war_contribution_per_building for every bot building the army razed (never below
        /// bot_war_contribution_min, the user 2026-11-05), less
        /// bot_war_contribution_hall_discount for every Diplomacy Hall level above the first.
        /// </summary>
        public static int RansomCoins(int hallLevel)
        {
            var c = BalanceConfig.Instance;
            if (c == null) return 0;
            var coins = c.BotWarContributionBase + c.BotWarContributionPerMinute * Mathf.FloorToInt(WarSeconds / 60f)
                        - c.BotWarContributionPerBuilding * WarRazed;
            coins = Mathf.Max(c.BotWarContributionMin, coins);
            var discount = Mathf.Clamp01(c.BotWarContributionHallDiscount * Mathf.Max(0, hallLevel - 1));
            return Mathf.Max(0, Mathf.RoundToInt(coins * (1f - discount)));
        }

        /// <summary>The same ransom in crystals, for a town short of coins: one per bot_war_coins_per_crystal, rounded up.</summary>
        public static int RansomCrystals(int hallLevel)
        {
            var c = BalanceConfig.Instance;
            var per = c != null ? Mathf.Max(1, c.BotWarCoinsPerCrystal) : 100;
            return Mathf.Max(1, Mathf.CeilToInt(RansomCoins(hallLevel) / (float)per));
        }

        /// <summary>The highest level of the town's Diplomacy Halls standing (1 when none -- the ransom's discount starts above it).</summary>
        public static int HallLevel()
        {
            var best = 1;
            foreach (var hall in UnityEngine.Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
                if (!hall.IsBot && hall.Data != null && hall.Data.buildingName == BuildingIds.DiplomacyHall) best = Mathf.Max(best, hall.Level);
            return best;
        }

        /// <summary>Relations are open at all: the town has a Diplomacy Hall standing and finished.</summary>
        public static bool Open
        {
            get
            {
                if (!BuildingInstance.HasAny(BuildingIds.DiplomacyHall)) return false;
                foreach (var hall in UnityEngine.Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
                    if (!hall.IsBot && hall.Data != null && hall.Data.buildingName == BuildingIds.DiplomacyHall && hall.IsOperational) return true;
                return false;
            }
        }

        /// <summary>Whether the player may move to `wanted` now; `why` is a #key saying what stands in the way.</summary>
        public static bool CanSwitch(BotStance wanted, out string why)
        {
            why = null;
            if (!Open) { why = "#diplomacy_needs_hall"; return false; }
            if (wanted == Stance) return false;
            if (Stance == BotStance.War)
            {
                // the user's rule: a war ends with the ransom paid (PayRansom) or the bot's Town Hall razed
                why = "#diplomacy_war_ends";
                return false;
            }
            if (wanted == BotStance.Trade && Score < (BalanceConfig.Instance != null ? BalanceConfig.Instance.BotTradeMinScore : 30f))
            {
                why = "#diplomacy_trade_needs";
                return false;
            }
            return true;
        }

        public static bool SwitchTo(BotStance wanted)
        {
            if (!CanSwitch(wanted, out _)) return false;
            if (wanted == BotStance.War && BalanceConfig.Instance != null)
                Score = Mathf.Clamp(Score - BalanceConfig.Instance.BotWarDeclareScoreLoss, Min, Max);
            if (wanted == BotStance.War) WarsFought++;
            Stance = wanted;
            WarSeconds = 0f;
            WarRazed = 0;
            Changed?.Invoke();
            return true;
        }

        /// <summary>The war is over (the bot's Town Hall razed, or the ransom paid): back to peace.</summary>
        public static void EndWar()
        {
            if (Stance != BotStance.War) return;
            Stance = BotStance.Peace;
            WarSeconds = 0f;
            WarRazed = 0;
            Changed?.Invoke();
        }

        /// <summary>The scale creeps back towards 0, bot_relations_drift_per_day a day.</summary>
        public static void Tick(float deltaSeconds)
        {
            var config = BalanceConfig.Instance;
            if (config == null || Score == 0f || config.DayLengthSeconds <= 0f) return;
            var step = config.BotRelationsDriftPerDay * deltaSeconds / config.DayLengthSeconds;
            var before = Score;
            Score = Score > 0f ? Mathf.Max(0f, Score - step) : Mathf.Min(0f, Score + step);
            if (Mathf.RoundToInt(before) != Mathf.RoundToInt(Score)) Changed?.Invoke();
        }
    }
}
