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

        /// <summary>Anything on the scale or the stance changed: the Diplomacy window redraws.</summary>
        public static event Action Changed;

        /// <summary>A new game: the sheet's starting relations, at peace.</summary>
        public static void Reset()
        {
            Score = BalanceConfig.Instance != null ? Mathf.Clamp(BalanceConfig.Instance.BotRelationsStart, Min, Max) : 0f;
            Stance = BotStance.Peace;
            Changed?.Invoke();
        }

        public static void Restore(float score, int stance)
        {
            Score = Mathf.Clamp(score, Min, Max);
            Stance = Enum.IsDefined(typeof(BotStance), stance) ? (BotStance)stance : BotStance.Peace;
            Changed?.Invoke();
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
                // the user's rule: a war ends with a contribution (later) or the bot's Town Hall razed
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
            Stance = wanted;
            Changed?.Invoke();
            return true;
        }

        /// <summary>The war is over (the bot's Town Hall razed; later, a contribution paid): back to peace.</summary>
        public static void EndWar()
        {
            if (Stance != BotStance.War) return;
            Stance = BotStance.Peace;
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
