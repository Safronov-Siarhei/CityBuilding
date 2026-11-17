using System;
using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Diplomacy
{
    /// <summary>What a diplomacy banner is about (DiplomacyRules.Raised).</summary>
    public enum DiplomacyNoticeKind { Help, BotGift, Edge, War, Razed }

    /// <summary>One thing the neighbour did: the banner shows it, the notice panel logs it.</summary>
    public class DiplomacyNotice
    {
        public DiplomacyNoticeKind Kind;
        /// <summary>The help request's row (Help).</summary>
        public BalanceConfig.DiplomacyRow Row;
        /// <summary>The resource and amount: the help asked (one) or the bot's gift (up to three).</summary>
        public readonly List<DiplomacyRules.Amount> Items = new List<DiplomacyRules.Amount>();
    }

    /// <summary>
    /// The Diplomacy Hall's rules (the user, 2026-11-17; card bot.md). All of it only while a Diplomacy
    /// Hall stands finished (BotRelations.Open), the numbers in the sheet tab «Зал дипломатии», the
    /// lists in «Зал дипломатии: таблицы» (diplomacy.csv):
    /// - the relations fall diplomacy_relations_fall_per_day a day (at peace);
    /// - a gift to the neighbour once a day, three sizes (coins -> relations);
    /// - every diplomacy_help_every_days the neighbour asks for help (a banner): sent = +relations, refused = -;
    /// - above diplomacy_bot_gift_min_score the neighbour may send a gift (a chance a day, growing to 100);
    /// - below 0 it may declare war (the chance grows to diplomacy_war_chance_at_minus100 at -100);
    /// - at diplomacy_edge_score a warning banner, once per crossing;
    /// - trade (relations at least bot_trade_min_score): diplomacy_trade_offers offers, diplomacy_trade_deals
    ///   deals, new offers every diplomacy_trade_refresh_sec or at once for an ad or crystals; a higher
    ///   hall level gives more per deal;
    /// - the neighbour's Town Hall razed at war: diplomacy_hall_razed_crystals crystals.
    /// </summary>
    public static class DiplomacyRules
    {
        public const string Crystals = "Crystals";

        /// <summary>A resource name (a ResourceType name or Crystals) and how many.</summary>
        [Serializable]
        public struct Amount
        {
            public string resource;
            public int amount;
            public Amount(string resource, int amount) { this.resource = resource; this.amount = amount; }
        }

        private const float OpenCheckSeconds = 1f;

        /// <summary>A banner to show: the UI (DiplomacyBanners) listens; queued while one is up.</summary>
        public static event Action<DiplomacyNotice> Raised;

        /// <summary>Anything in the gift or trade state changed: the windows redraw.</summary>
        public static event Action Changed;

        /// <summary>Game seconds into the diplomacy day (counted only while the hall stands).</summary>
        public static float DayClock { get; private set; }
        public static int DaysSinceHelp { get; private set; }
        public static bool GiftSentToday { get; private set; }
        public static bool EdgeShown { get; private set; }

        private static readonly List<string> offers = new List<string>();
        private static readonly List<bool> taken = new List<bool>();
        public static int DealsDone { get; private set; }
        public static float RefreshLeft { get; private set; }

        private static bool _open;
        private static float _openCheckIn;

        public static IReadOnlyList<string> OfferIds => offers;

        private static BalanceConfig Config => BalanceConfig.Instance;

        private static float DayLength => Config != null && Config.DayLengthSeconds > 0f ? Config.DayLengthSeconds : 60f;

        /// <summary>Seconds of the game left till the next diplomacy day (the gift comes back).</summary>
        public static float SecondsToNextDay => Mathf.Max(0f, DayLength - DayClock);

        public static void Reset()
        {
            DayClock = 0f;
            DaysSinceHelp = 0;
            GiftSentToday = false;
            EdgeShown = false;
            offers.Clear();
            taken.Clear();
            DealsDone = 0;
            RefreshLeft = 0f;
            _openCheckIn = 0f;
            Changed?.Invoke();
        }

        public static void Restore(float dayClock, int daysSinceHelp, bool giftSent, bool edgeShown, string[] offerIds, bool[] offerTaken, int deals, float refreshLeft)
        {
            DayClock = Mathf.Max(0f, dayClock);
            DaysSinceHelp = Mathf.Max(0, daysSinceHelp);
            GiftSentToday = giftSent;
            EdgeShown = edgeShown;
            offers.Clear();
            taken.Clear();
            if (offerIds != null)
                for (var i = 0; i < offerIds.Length; i++)
                    if (FindRow("trade", offerIds[i]) != null)
                    {
                        offers.Add(offerIds[i]);
                        taken.Add(offerTaken != null && i < offerTaken.Length && offerTaken[i]);
                    }
            DealsDone = Mathf.Max(0, deals);
            RefreshLeft = Mathf.Max(0f, refreshLeft);
            Changed?.Invoke();
        }

        public static string[] SaveOffers() => offers.ToArray();
        public static bool[] SaveTaken() => taken.ToArray();

        // ---- the clock ------------------------------------------------------------------------

        /// <summary>Every frame (GameCalendar through BotRelations.Tick).</summary>
        public static void Tick(float deltaSeconds)
        {
            var config = Config;
            if (config == null || deltaSeconds <= 0f) return;
            // the hall's search is not a per-frame cost: once a second
            _openCheckIn -= deltaSeconds;
            if (_openCheckIn <= 0f)
            {
                _openCheckIn = OpenCheckSeconds;
                _open = BotRelations.Open;
            }
            if (!_open) return;

            if (BotRelations.Stance != BotStance.War)
                BotRelations.AddScore(-config.DiplomacyRelationsFallPerDay * deltaSeconds / DayLength);
            CheckEdge();

            if (offers.Count > 0)
            {
                RefreshLeft -= deltaSeconds;
                if (RefreshLeft <= 0f) NewOffers();
            }

            DayClock += deltaSeconds;
            if (DayClock < DayLength) return;
            DayClock -= DayLength;
            NewDay();
        }

        private static void NewDay()
        {
            var config = Config;
            GiftSentToday = false;
            DaysSinceHelp++;
            var score = BotRelations.Score;
            var atWar = BotRelations.Stance == BotStance.War;

            if (!atWar && score < 0f && BotAround() && !WorldSettings.Current.neighbourPeace)
            {
                var chance = config.DiplomacyWarChanceAtMinus100 * Mathf.Clamp01(-score / 100f);
                if (UnityEngine.Random.value < chance)
                {
                    BotRelations.DeclaredByBot();
                    Raise(new DiplomacyNotice { Kind = DiplomacyNoticeKind.War });
                    Changed?.Invoke();
                    return;
                }
            }
            if (!atWar && DaysSinceHelp >= Mathf.RoundToInt(config.DiplomacyHelpEveryDays))
            {
                DaysSinceHelp = 0;
                var row = Pick("help", HallLevel());
                if (row != null)
                {
                    var notice = new DiplomacyNotice { Kind = DiplomacyNoticeKind.Help, Row = row };
                    notice.Items.Add(new Amount(row.resource, row.amount));
                    Raise(notice);
                }
            }
            else if (!atWar && score >= config.DiplomacyBotGiftMinScore)
            {
                var t = Mathf.InverseLerp(config.DiplomacyBotGiftMinScore, BotRelations.Max, score);
                if (UnityEngine.Random.value < Mathf.Lerp(config.DiplomacyBotGiftChanceAtMin, config.DiplomacyBotGiftChanceAtMax, t))
                    RaiseBotGift();
            }
            Changed?.Invoke();
        }

        private static void CheckEdge()
        {
            var edge = Config.DiplomacyEdgeScore;
            var score = BotRelations.Score;
            if (EdgeShown)
            {
                if (score > edge + 5f) EdgeShown = false;
                return;
            }
            if (score > edge || BotRelations.Stance == BotStance.War) return;
            EdgeShown = true;
            Raise(new DiplomacyNotice { Kind = DiplomacyNoticeKind.Edge });
        }

        private static void RaiseBotGift()
        {
            var level = HallLevel();
            var scale = 1f + Config.DiplomacyBotGiftPerLevel * (level - 1);
            var notice = new DiplomacyNotice { Kind = DiplomacyNoticeKind.BotGift };
            var used = new HashSet<string>();
            for (var i = 0; i < Config.DiplomacyBotGiftItems; i++)
            {
                var row = Pick("botgift", level, used);
                if (row == null) break;
                used.Add(row.id);
                notice.Items.Add(new Amount(row.resource, Mathf.Max(1, Mathf.RoundToInt(row.amount * scale))));
            }
            if (notice.Items.Count > 0) Raise(notice);
        }

        private static void Raise(DiplomacyNotice notice) => Raised?.Invoke(notice);

        private static bool BotAround()
        {
            var bot = Bot.BotKingdom.Instance;
            return bot != null && bot.Placed && !bot.HallRazed;
        }

        // ---- answers to the banners -------------------------------------------------------------

        /// <summary>«Отправить»: the help is paid, the relations rise. False when the town is short.</summary>
        public static bool SendHelp(DiplomacyNotice notice)
        {
            if (notice == null || notice.Items.Count == 0 || !TrySpend(notice.Items[0])) return false;
            BotRelations.AddScore(Config.DiplomacyHelpSendRelations);
            Changed?.Invoke();
            return true;
        }

        /// <summary>«Отказать» (or closed): the relations drop a little.</summary>
        public static void RefuseHelp()
        {
            BotRelations.AddScore(-Config.DiplomacyHelpRefuseRelations);
            Changed?.Invoke();
        }

        /// <summary>«Забрать»: the bot's gift lands in the town.</summary>
        public static void TakeGift(DiplomacyNotice notice)
        {
            if (notice == null) return;
            foreach (var item in notice.Items) Give(item);
        }

        /// <summary>The neighbour's Town Hall razed at war (BotKingdom): the crystals and the window.</summary>
        public static void HallRazed()
        {
            var crystals = Config != null ? Config.DiplomacyHallRazedCrystals : 0;
            var notice = new DiplomacyNotice { Kind = DiplomacyNoticeKind.Razed };
            notice.Items.Add(new Amount(Crystals, crystals));
            Raise(notice);
        }

        // ---- the gift to the neighbour ----------------------------------------------------------

        public static bool CanGift(int size) => !GiftSentToday && BotRelations.Stance != BotStance.War && Have(ResourceType.Coins.ToString()) >= Config.DiplomacyGiftCoins(size);

        public static bool SendGift(int size)
        {
            if (!CanGift(size) || !TrySpend(new Amount(ResourceType.Coins.ToString(), Config.DiplomacyGiftCoins(size)))) return false;
            GiftSentToday = true;
            BotRelations.AddScore(Config.DiplomacyGiftRelations(size));
            Changed?.Invoke();
            return true;
        }

        // ---- trade ------------------------------------------------------------------------------

        public static bool TradeOpen => BotRelations.Stance != BotStance.War && BotRelations.Score >= (Config != null ? Config.BotTradeMinScore : 30f);

        /// <summary>The trade window opens: the first offers are drawn now.</summary>
        public static void EnsureOffers()
        {
            if (offers.Count == 0) NewOffers();
        }

        public static void NewOffers()
        {
            offers.Clear();
            taken.Clear();
            var level = HallLevel();
            var used = new HashSet<string>();
            for (var i = 0; i < Config.DiplomacyTradeOffers; i++)
            {
                var row = Pick("trade", level, used);
                if (row == null) break;
                used.Add(row.id);
                offers.Add(row.id);
                taken.Add(false);
            }
            DealsDone = 0;
            RefreshLeft = Config.DiplomacyTradeRefreshSeconds;
            Changed?.Invoke();
        }

        public static BalanceConfig.DiplomacyRow Offer(int i) => i >= 0 && i < offers.Count ? FindRow("trade", offers[i]) : null;
        public static bool Taken(int i) => i >= 0 && i < taken.Count && taken[i];
        public static bool DealsLeft => DealsDone < Config.DiplomacyTradeDeals;

        /// <summary>What the town gets for offer `row` at the hall's level.</summary>
        public static int GetAmount(BalanceConfig.DiplomacyRow row)
        {
            if (row == null) return 0;
            var bonus = 1f + Config.DiplomacyTradeBonusPerLevel * (HallLevel() - 1);
            return Mathf.Max(1, Mathf.RoundToInt(row.getAmount * bonus));
        }

        public static bool CanSwap(int i)
        {
            var row = Offer(i);
            return row != null && TradeOpen && !Taken(i) && DealsLeft && Have(row.resource) >= row.amount;
        }

        public static bool Swap(int i)
        {
            if (!CanSwap(i)) return false;
            var row = Offer(i);
            if (!TrySpend(new Amount(row.resource, row.amount))) return false;
            Give(new Amount(row.get, GetAmount(row)));
            taken[i] = true;
            DealsDone++;
            Changed?.Invoke();
            return true;
        }

        /// <summary>«Обновить за N кристаллов».</summary>
        public static bool RefreshForCrystals()
        {
            var price = Config.DiplomacyTradeRefreshCrystals;
            if (Wallet.UpgradeCrystals < price) return false;
            Wallet.UpgradeCrystals -= price;
            NewOffers();
            return true;
        }

        // ---- resources --------------------------------------------------------------------------

        public static bool IsCrystals(string resource) => string.Equals(resource, Crystals, StringComparison.OrdinalIgnoreCase);

        public static bool TryParse(string resource, out ResourceType type) => Enum.TryParse(resource, true, out type);

        public static int Have(string resource)
        {
            if (IsCrystals(resource)) return Wallet.UpgradeCrystals;
            var rm = ResourceManager.Instance;
            return rm != null && TryParse(resource, out var type) ? rm.GetAmount(type) : 0;
        }

        private static bool TrySpend(Amount a)
        {
            if (a.amount <= 0) return true;
            if (IsCrystals(a.resource))
            {
                if (Wallet.UpgradeCrystals < a.amount) return false;
                Wallet.UpgradeCrystals -= a.amount;
                return true;
            }
            var rm = ResourceManager.Instance;
            return rm != null && TryParse(a.resource, out var type) && rm.TrySpend(new[] { new ResourceAmount { type = type, amount = a.amount } });
        }

        private static void Give(Amount a)
        {
            if (a.amount <= 0) return;
            if (IsCrystals(a.resource))
            {
                Wallet.UpgradeCrystals += a.amount;
                return;
            }
            // a gift never vanishes for a full barn
            if (ResourceManager.Instance != null && TryParse(a.resource, out var type)) ResourceManager.Instance.AddOverCapacity(type, a.amount);
        }

        // ---- the tables -------------------------------------------------------------------------

        public static int HallLevel() => BotRelations.HallLevel();

        private static BalanceConfig.DiplomacyRow FindRow(string kind, string id)
        {
            var rows = Config != null ? Config.DiplomacyRows : null;
            if (rows == null) return null;
            foreach (var row in rows)
                if (row.kind == kind && row.id == id) return row;
            return null;
        }

        /// <summary>A weighted draw among `kind`'s rows open at hall `level`, skipping `used`.</summary>
        private static BalanceConfig.DiplomacyRow Pick(string kind, int level, HashSet<string> used = null)
        {
            var rows = Config != null ? Config.DiplomacyRows : null;
            if (rows == null) return null;
            var total = 0f;
            foreach (var row in rows)
                if (row.kind == kind && row.minHall <= level && (used == null || !used.Contains(row.id))) total += row.weight;
            if (total <= 0f) return null;
            var roll = UnityEngine.Random.value * total;
            BalanceConfig.DiplomacyRow last = null;
            foreach (var row in rows)
            {
                if (row.kind != kind || row.minHall > level || (used != null && used.Contains(row.id)) || row.weight <= 0f) continue;
                last = row;
                roll -= row.weight;
                if (roll <= 0f) return row;
            }
            return last;
        }
    }
}
