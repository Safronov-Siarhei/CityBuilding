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
            return list;
        }

        [Test]
        public void SlotsChangeOneAtATimeWithoutRepeatsAndBonusOnlyLate()
        {
            for (var seed = 0; seed < 40; seed++)
            {
                var r = new OfferRotation(Table(), 180, 60, 0.5, 300, seed);
                r.Start(0, 1);
                var free = 0;
                for (var t = 1; t <= 1800; t++)
                {
                    var before = new string[3];
                    for (var i = 0; i < 3; i++) before[i] = r.Slots[i].Bundle.id;
                    r.Tick(t, 1, out var bonus);
                    var changes = 0;
                    var ids = new HashSet<string>();
                    for (var i = 0; i < 3; i++)
                    {
                        var b = r.Slots[i].Bundle;
                        Assert.IsNotNull(b);
                        Assert.IsTrue(ids.Add(b.id), $"seed {seed} t {t}: {b.id} on two slots");
                        Assert.That(b.levelFrom, Is.LessThanOrEqualTo(1));
                        Assert.That(b.kind, Is.Not.EqualTo(OfferRotation.KindStarter));
                        if (b.id != before[i]) changes++;
                        if (r.Slots[i].IsFree && t < 300) Assert.Fail($"seed {seed} t {t}: a bonus before its minute");
                    }
                    Assert.That(changes, Is.LessThanOrEqualTo(1), $"seed {seed} t {t}: two slots changed at once");
                    // the first change at 180, then one each minute
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
            var r = new OfferRotation(Table(), 180, 60, 0, 300, 7);
            r.Start(0, 2);
            foreach (var s in r.Slots) StringAssert.StartsWith("h", s.Bundle.id);
            r.Slots[1].Taken = true;
            r.Tick(200, 2, out _);   // slot 0 changed, slot 1 (ends at 240) not yet
            Assert.IsTrue(r.Slots[1].Taken);
            r.Tick(240, 2, out _);
            Assert.IsFalse(r.Slots[1].Taken);

            Assert.IsTrue(r.PutStarter(250, 2));
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
    }
}
