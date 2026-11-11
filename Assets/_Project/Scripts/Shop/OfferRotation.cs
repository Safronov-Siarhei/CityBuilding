using System.Collections.Generic;
using CityBuilder.Core;

namespace CityBuilder.Shop
{
    /// <summary>One of the shop's three places: the bundle on it, when it changes (real seconds), whether it was taken this period.</summary>
    public class OfferSlot
    {
        public BalanceConfig.OfferBundle Bundle;
        public double EndsAt;
        public bool Taken;

        /// <summary>The kind of bundles this place shows (normal, ad_crystals, paid_crystals); a normal place also takes the free ones.</summary>
        public string Kind = OfferRotation.KindNormal;

        public bool IsFree => Bundle != null && (Bundle.kind == OfferRotation.KindBonus || Bundle.kind == OfferRotation.KindStarter);
        public bool IsPaid => Bundle != null && Bundle.kind == OfferRotation.KindPaidCrystals;
        public bool IsAdOnly => Bundle != null && Bundle.kind == OfferRotation.KindAdCrystals;
    }

    /// <summary>
    /// The sandbox offer shop's rules (the user, 2026-11-11), plain C# so a test can drive the clock:
    /// four places (round 4: resources, resources, crystals for an ad, crystals for money -- each
    /// place picks only its own kind), each on its own `period`, the first changes `period` after the start and the others
    /// `stagger` later each, so they change one at a time. A new bundle is a weighted pick among the
    /// `normal` bundles of the player's level, never one already on another slot nor the one it
    /// replaces. On a change, once `bonusMinSeconds` have passed since the start (re-entering must
    /// not farm it), the free bonus comes instead with `bonusChance` -- one bonus on screen at most.
    /// Each slot can be taken once per period (<see cref="OfferSlot.Taken"/>). Kept per session only.
    /// </summary>
    public class OfferRotation
    {
        public const string KindNormal = "normal";
        public const string KindBonus = "bonus";
        public const string KindStarter = "starter";
        public const string KindAdCrystals = "ad_crystals";
        public const string KindPaidCrystals = "paid_crystals";

        /// <summary>The shop's places in order (the user, 2026-11-11).</summary>
        public static readonly string[] DefaultPlaces = { KindNormal, KindNormal, KindAdCrystals, KindPaidCrystals };

        public readonly OfferSlot[] Slots;
        public int SlotCount => Slots.Length;

        private readonly IReadOnlyList<BalanceConfig.OfferBundle> _bundles;
        private readonly double _period;
        private readonly double _stagger;
        private readonly double _bonusChance;
        private readonly double _bonusMinSeconds;
        private readonly System.Random _random;
        private readonly List<BalanceConfig.OfferBundle> _pool = new List<BalanceConfig.OfferBundle>();
        private double _startedAt;

        public OfferRotation(IReadOnlyList<BalanceConfig.OfferBundle> bundles, string[] places, double period, double stagger,
                             double bonusChance, double bonusMinSeconds, int seed)
        {
            places = places ?? DefaultPlaces;
            Slots = new OfferSlot[places.Length];
            _bundles = bundles ?? new List<BalanceConfig.OfferBundle>();
            _period = period > 1 ? period : 1;
            _stagger = stagger > 0 ? stagger : 0;
            _bonusChance = bonusChance;
            _bonusMinSeconds = bonusMinSeconds;
            _random = new System.Random(seed);
            for (var i = 0; i < SlotCount; i++) Slots[i] = new OfferSlot { Kind = places[i] };
        }

        /// <summary>The first three bundles: ordinary ones only (a bonus never on entry).</summary>
        public void Start(double now, int level)
        {
            _startedAt = now;
            for (var i = 0; i < SlotCount; i++)
            {
                Slots[i].Bundle = null;
                Slots[i].Taken = false;
                Slots[i].EndsAt = now + _period + i * _stagger;
            }
            for (var i = 0; i < SlotCount; i++) Slots[i].Bundle = Pick(Slots[i].Kind, level, i);
        }

        /// <summary>
        /// Changes every slot whose time ran out. Returns true if anything changed; `bonusAppeared`
        /// says a free bonus came up (the loud notice).
        /// </summary>
        public bool Tick(double now, int level, out bool bonusAppeared)
        {
            bonusAppeared = false;
            var changed = false;
            for (var i = 0; i < SlotCount; i++)
            {
                var slot = Slots[i];
                if (now < slot.EndsAt) continue;
                // a long pause (the phone asleep) changes a slot once, onto the next period from now
                while (slot.EndsAt <= now) slot.EndsAt += _period;
                BalanceConfig.OfferBundle next = null;
                // the free bonus comes only on a resources place
                if (slot.Kind == KindNormal && now - _startedAt >= _bonusMinSeconds && !HasFreeOnScreen() && _random.NextDouble() < _bonusChance)
                    next = Pick(KindBonus, level, i);
                if (next != null) bonusAppeared = true;
                else next = Pick(slot.Kind, level, i);
                slot.Bundle = next;
                slot.Taken = false;
                changed = true;
            }
            return changed;
        }

        /// <summary>Puts the once-per-account starter gift on the slot that would change soonest, for a full period.</summary>
        public bool PutStarter(double now, int level)
        {
            if (HasFreeOnScreen()) return false;
            var starter = Pick(KindStarter, level, -1);
            if (starter == null) return false;
            var soonest = -1;
            for (var i = 0; i < SlotCount; i++)
                if (Slots[i].Kind == KindNormal && (soonest < 0 || Slots[i].EndsAt < Slots[soonest].EndsAt)) soonest = i;
            if (soonest < 0) return false;
            Slots[soonest].Bundle = starter;
            Slots[soonest].Taken = false;
            Slots[soonest].EndsAt = now + _period;
            return true;
        }

        /// <summary>A free bundle is on screen and not taken yet (the HUD icon glows).</summary>
        public bool FreeWaiting
        {
            get
            {
                foreach (var slot in Slots)
                    if (slot.IsFree && !slot.Taken) return true;
                return false;
            }
        }

        /// <summary>The soonest change (the HUD icon's timer).</summary>
        public double NextChangeAt
        {
            get
            {
                var at = double.MaxValue;
                foreach (var slot in Slots)
                    if (slot.EndsAt < at) at = slot.EndsAt;
                return at;
            }
        }

        private bool HasFreeOnScreen()
        {
            foreach (var slot in Slots)
                if (slot.IsFree) return true;
            return false;
        }

        /// <summary>Weighted among the `kind`'s bundles of `level`, skipping those on the other slots and the one on `slot`; the replaced one again if nothing else fits.</summary>
        private BalanceConfig.OfferBundle Pick(string kind, int level, int slot)
        {
            for (var pass = 0; pass < 2; pass++)
            {
                _pool.Clear();
                var total = 0.0;
                foreach (var b in _bundles)
                {
                    if (b.kind != kind || level < b.levelFrom || level > b.levelTo) continue;
                    if (OnSlots(b, slot, includeOwn: pass == 0)) continue;
                    _pool.Add(b);
                    total += b.weight;
                }
                if (_pool.Count == 0) continue;
                var roll = _random.NextDouble() * total;
                foreach (var b in _pool)
                {
                    roll -= b.weight;
                    if (roll < 0) return b;
                }
                return _pool[_pool.Count - 1];
            }
            return null;
        }

        private bool OnSlots(BalanceConfig.OfferBundle bundle, int slot, bool includeOwn)
        {
            for (var i = 0; i < SlotCount; i++)
            {
                if (i == slot && !includeOwn) continue;
                if (Slots[i].Bundle != null && Slots[i].Bundle.id == bundle.id) return true;
            }
            return false;
        }

        /// <summary>
        /// A crystal offer's bonus in whole percent (the user: always an integer): crystals for an ad
        /// against `adBase` crystals per ad, crystals for money against `crystalUsd` dollars a
        /// crystal. 0 for anything else or no bonus.
        /// </summary>
        public static int BonusPercent(BalanceConfig.OfferBundle bundle, float crystalUsd, float adBase)
        {
            if (bundle == null || bundle.goods.Count == 0) return 0;
            var crystals = 0;
            foreach (var good in bundle.goods)
                if (good.resource == "Crystals") crystals += good.amount;
            double ratio;
            if (bundle.kind == KindAdCrystals && adBase > 0) ratio = crystals / (double)adBase;
            else if (bundle.kind == KindPaidCrystals && bundle.priceUsd > 0 && crystalUsd > 0) ratio = crystals * (double)crystalUsd / bundle.priceUsd;
            else return 0;
            var percent = (int)System.Math.Round((ratio - 1) * 100);
            return percent > 0 ? percent : 0;
        }

        /// <summary>The player's bundle level: the highest row of «Наборы: уровни» whose lab and citizens are both met; 1 if none.</summary>
        public static int LevelFor(IReadOnlyList<BalanceConfig.OfferLevel> levels, int labLevel, int citizens)
        {
            var best = 1;
            if (levels == null) return best;
            foreach (var row in levels)
                if (labLevel >= row.labFrom && citizens >= row.citizensFrom && row.level > best) best = row.level;
            return best;
        }
    }
}
