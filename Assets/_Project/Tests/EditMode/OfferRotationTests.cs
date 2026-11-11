using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Shop;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// The sandbox offer shop's rules (the user, 2026-11-11) on a made-up table and a driven clock:
    /// the slots change one at a time, never show the same bundle twice, keep to the player's level,
    /// never give a bonus before its minute, hold one free bundle at most, and the level table.
    /// Four places (round 4): two of goods, crystals for an ad, crystals for money -- each place keeps
    /// to its kind, a gift and the starter come only on a goods place; the bonus percent of a crystal offer.
    /// </summary>
    public class OfferRotationTests
    {
        private static BalanceConfig.OfferBundle B(string id, string kind, int from, int to) =>
            new BalanceConfig.OfferBundle { id = id, kind = kind, weight = 1f, levelFrom = from, levelTo = to, goods = new List<BalanceConfig.OfferGood>() };

        private static List<BalanceConfig.OfferBundle> Table()
        {
            var list = new List<BalanceConfig.OfferBundle>();
            for (var i = 0; i < 6; i++) list.Add(B("n" + i, OfferRotation.KindNormal, 1, 1));
            for (var i = 0; i < 6; i++) list.Add(B("h" + i, OfferRotation.KindNormal, 2, 2));
            list.Add(B("gift_a", OfferRotation.KindBonus, 1, 2));
            list.Add(B("gift_b", OfferRotation.KindBonus, 1, 2));
            list.Add(B("starter", OfferRotation.KindStarter, 1, 2));
            for (var i = 0; i < 3; i++) list.Add(B("ad" + i, OfferRotation.KindAdCrystals, 1, 99));
            for (var i = 0; i < 3; i++) list.Add(B("paid" + i, OfferRotation.KindPaidCrystals, 1, 99));
            return list;
        }

        [Test]
        public void SlotsChangeOneAtATimeWithoutRepeatsAndBonusOnlyLate()
        {
            for (var seed = 0; seed < 40; seed++)
            {
                var r = new OfferRotation(Table(), OfferRotation.DefaultPlaces, 180, 45, 0.5, 300, seed);
                r.Start(0, 1);
                var free = 0;
                for (var t = 1; t <= 1800; t++)
                {
                    var n = r.SlotCount;
                    Assert.That(n, Is.EqualTo(4));
                    var before = new string[n];
                    for (var i = 0; i < n; i++) before[i] = r.Slots[i].Bundle.id;
                    r.Tick(t, 1, out var bonus);
                    var changes = 0;
                    var ids = new HashSet<string>();
                    for (var i = 0; i < n; i++)
                    {
                        var b = r.Slots[i].Bundle;
                        var place = OfferRotation.DefaultPlaces[i];
                        if (place == OfferRotation.KindNormal)
                            Assert.That(b.kind, Is.EqualTo(OfferRotation.KindNormal).Or.EqualTo(OfferRotation.KindBonus), $"seed {seed} t {t}: {b.id} on a goods place");
                        else
                            Assert.That(b.kind, Is.EqualTo(place), $"seed {seed} t {t}: {b.id} on the {place} place");
                        Assert.IsNotNull(b);
                        Assert.IsTrue(ids.Add(b.id), $"seed {seed} t {t}: {b.id} on two slots");
                        Assert.That(b.levelFrom, Is.LessThanOrEqualTo(1));
                        Assert.That(b.kind, Is.Not.EqualTo(OfferRotation.KindStarter));
                        if (b.id != before[i]) changes++;
                        if (r.Slots[i].IsFree && t < 300) Assert.Fail($"seed {seed} t {t}: a bonus before its minute");
                    }
                    Assert.That(changes, Is.LessThanOrEqualTo(1), $"seed {seed} t {t}: two slots changed at once");
                    // the first change at 180, then one each 45 s (4 places x 45 = the 180 s period: never two at once)
                    if (t < 180) Assert.That(changes, Is.Zero);
                    var onScreen = 0;
                    foreach (var s in r.Slots) if (s.IsFree) onScreen++;
                    Assert.That(onScreen, Is.LessThanOrEqualTo(1));
                    if (bonus) free++;
                }
                Assert.That(free, Is.GreaterThan(0), $"seed {seed}: a 50% bonus never came in 30 minutes");
            }
        }

        [Test]
        public void TakenHoldsUntilTheSlotChangesAndLevelPicksItsRows()
        {
            var r = new OfferRotation(Table(), OfferRotation.DefaultPlaces, 180, 45, 0, 300, 7);
            r.Start(0, 2);
            StringAssert.StartsWith("h", r.Slots[0].Bundle.id);
            StringAssert.StartsWith("h", r.Slots[1].Bundle.id);
            StringAssert.StartsWith("ad", r.Slots[2].Bundle.id);
            StringAssert.StartsWith("paid", r.Slots[3].Bundle.id);
            r.Slots[1].Taken = true;
            r.Tick(200, 2, out _);   // slot 0 changed, slot 1 (ends at 225) not yet
            Assert.IsTrue(r.Slots[1].Taken);
            r.Tick(225, 2, out _);
            Assert.IsFalse(r.Slots[1].Taken);

            Assert.IsTrue(r.PutStarter(250, 2));
            Assert.That(r.Slots[2].Bundle.kind, Is.EqualTo(OfferRotation.KindAdCrystals), "the starter never takes a crystal place");
            Assert.That(r.Slots[3].Bundle.kind, Is.EqualTo(OfferRotation.KindPaidCrystals));
            Assert.IsTrue(r.FreeWaiting);
            Assert.IsFalse(r.PutStarter(251, 2), "one free bundle on screen at most");

            var levels = new List<BalanceConfig.OfferLevel>
            {
                new BalanceConfig.OfferLevel { level = 1, labFrom = 0, citizensFrom = 0 },
                new BalanceConfig.OfferLevel { level = 2, labFrom = 0, citizensFrom = 20 },
                new BalanceConfig.OfferLevel { level = 3, labFrom = 1, citizensFrom = 40 },
            };
            Assert.That(OfferRotation.LevelFor(levels, 0, 5), Is.EqualTo(1));
            Assert.That(OfferRotation.LevelFor(levels, 0, 60), Is.EqualTo(2), "no lab holds the level at 2");
            Assert.That(OfferRotation.LevelFor(levels, 1, 40), Is.EqualTo(3));
        }

        [Test]
        public void BonusPercentIsAWholeNumberFromTheBase()
        {
            BalanceConfig.OfferBundle C(string kind, int crystals, float usd)
            {
                var b = B("c", kind, 1, 99);
                b.goods.Add(new BalanceConfig.OfferGood { resource = "Crystals", amount = crystals });
                b.priceUsd = usd;
                return b;
            }
            // 1 crystal = $0.01, one ad = 10 crystals (the user, 2026-11-11)
            Assert.That(OfferRotation.BonusPercent(C(OfferRotation.KindPaidCrystals, 199, 0.99f), 0.01f, 10f), Is.EqualTo(101));
            Assert.That(OfferRotation.BonusPercent(C(OfferRotation.KindPaidCrystals, 3499, 9.99f), 0.01f, 10f), Is.EqualTo(250));
            Assert.That(OfferRotation.BonusPercent(C(OfferRotation.KindAdCrystals, 15, 0f), 0.01f, 10f), Is.EqualTo(50));
            Assert.That(OfferRotation.BonusPercent(C(OfferRotation.KindAdCrystals, 10, 0f), 0.01f, 10f), Is.Zero, "the base ad has no badge");
            Assert.That(OfferRotation.BonusPercent(C(OfferRotation.KindNormal, 100, 0f), 0.01f, 10f), Is.Zero);
        }

        [Test]
        public void AGoodsBundleOverTheAdLimitIsForCrystalsOnly()
        {
            // the user, 2026-11-11: a bundle worth more than shop_ad_max_ads ads is never given for an ad
            var cheap = B("cheap", OfferRotation.KindNormal, 1, 1);
            cheap.ads = 2f;
            var dear = B("dear", OfferRotation.KindNormal, 1, 1);
            dear.ads = 2.01f;
            Assert.IsTrue(OfferRotation.AdAllowed(cheap, 2f), "exactly the limit is still an ad");
            Assert.IsFalse(OfferRotation.AdAllowed(dear, 2f));
            Assert.IsTrue(OfferRotation.AdAllowed(dear, 0f), "a limit of 0 is off");
            Assert.IsFalse(OfferRotation.AdAllowed(B("gift", OfferRotation.KindBonus, 1, 1), 2f), "only goods bundles");
        }

        [Test]
        public void TheShortageOfferIsTheShortfallAtItsWorth()
        {
            // the user, 2026-11-11: exactly what a building lacks, priced by shop_price10_*, ad-or-crystals by the ad limit
            float Price(string r) => r == "Wood" ? 0.7f : r == "Stone" ? 0.9f : 0f;
            var small = OfferRotation.ShortageBundle(new List<BalanceConfig.OfferGood>
            {
                new BalanceConfig.OfferGood { resource = "Wood", amount = 40 },
                new BalanceConfig.OfferGood { resource = "Stone", amount = 15 },
            }, Price, 10f);
            // 40 x 0.07 + 15 x 0.09 = 4.15 -> 5 crystals, 0.415 ads
            Assert.That(small.priceCrystals, Is.EqualTo(5));
            Assert.That(small.ads, Is.EqualTo(0.415f).Within(0.001f));
            Assert.IsTrue(OfferRotation.AdAllowed(small, 2f));
            var big = OfferRotation.ShortageBundle(new List<BalanceConfig.OfferGood>
            {
                new BalanceConfig.OfferGood { resource = "Wood", amount = 400 },
            }, Price, 10f);
            Assert.That(big.priceCrystals, Is.EqualTo(28));
            Assert.IsFalse(OfferRotation.AdAllowed(big, 2f), "worth 2.8 ads: crystals only");
            var tiny = OfferRotation.ShortageBundle(new List<BalanceConfig.OfferGood>
            {
                new BalanceConfig.OfferGood { resource = "Wood", amount = 1 },
            }, Price, 10f);
            Assert.That(tiny.priceCrystals, Is.EqualTo(1), "never free");
            Assert.That(OfferRotation.BonusPercent(small, 0.01f, 10f), Is.Zero, "no badge");
        }
    }
}
