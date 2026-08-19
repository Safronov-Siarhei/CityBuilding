using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Resources;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// The soldier ladder, checked as RELATIONSHIPS against whatever the sheet currently says --
    /// the same shape as ArmyBalanceTests and FoodBalanceTests, and for the same reason: the point
    /// is that each rung is a real trade against the one below it, and that has to survive a retune.
    ///
    /// A tier that is strictly better than another for less money is the failure this is here to
    /// catch, because it does not look like a bug from inside the game. It looks like one tier
    /// nobody ever builds.
    /// </summary>
    public class SoldierTierTests
    {
        private static UnitBalance Row(SoldierType type) => BalanceConfig.Instance.Unit(SoldierStats.SheetIdOf(type));

        private static UnitLevelStats Level1(SoldierType type) => Row(type).LevelStats(1);

        [Test]
        public void EveryTierHasARowInTheSheet()
        {
            foreach (var type in SoldierStats.All)
            {
                var row = Row(type);
                Assert.IsNotNull(row, $"No units row for '{SoldierStats.SheetIdOf(type)}'.");
                Assert.Greater(Level1(type).maxHealth, 0, $"{type} has no health, so it would die to the first thing that touched it.");
                Assert.Greater(Level1(type).attackDamage, 0, $"{type} deals no damage, so it is upkeep with legs.");
            }
        }

        /// <summary>Militia is the tier a settlement can raise on day one; everything above it has to be opened, or the Laboratory has nothing to do with the army at all.</summary>
        [Test]
        public void OnlyMilitiaStartsOpen_AndEveryOtherTierNamesItsUnlock()
        {
            Assert.IsTrue(Row(SoldierType.Militia).startsUnlocked, "Militia has to be recruitable before anything is researched, or a new game has no army at all.");

            foreach (var type in SoldierStats.All)
            {
                if (type == SoldierType.Militia) continue;

                Assert.IsFalse(Row(type).startsUnlocked, $"{type} is open from the start, so the Laboratory never gates it.");
                Assert.IsTrue(Row(type).unlockResearch.IsAuthored,
                    $"{type} is marked starts_unlocked=0 but names no unlock research, so nothing could ever open it.");
            }
        }

        /// <summary>
        /// Militia is coins alone -- an armed peasant needs no forge, and that is exactly why it is
        /// the tier available before a settlement has an industry. Every tier above it is paid for
        /// partly in smelted metal, which is what makes the Плавильня worth building.
        /// </summary>
        [Test]
        public void MilitiaCostsCoinsOnly_AndEveryTierAboveItCostsMetal()
        {
            var militia = SoldierStats.RecruitCost(SoldierType.Militia);
            Assert.AreEqual(1, militia.Count, "Militia is supposed to cost coins and nothing else.");
            Assert.AreEqual(ResourceType.Coins, militia[0].type);

            foreach (var type in SoldierStats.All)
            {
                if (type == SoldierType.Militia) continue;

                var bars = Row(type).recruitIronBars + Row(type).recruitCopperBars;
                Assert.Greater(bars, 0, $"{type} costs no metal, so an army needs no industry behind it.");
                Assert.Greater(SoldierStats.RecruitCost(type).Count, 1, $"{type}'s cost list does not carry its metal, so the Barracks would raise it for coins.");
            }
        }

        /// <summary>Every rung costs more to raise and more to keep than the militia at the bottom of the ladder -- otherwise the ladder points downwards.</summary>
        [Test]
        public void EveryTierCostsMoreThanMilitia()
        {
            foreach (var type in SoldierStats.All)
            {
                if (type == SoldierType.Militia) continue;

                Assert.Greater(Level1(type).recruitCoins, Level1(SoldierType.Militia).recruitCoins, $"{type} is cheaper to raise than militia.");
                Assert.Greater(Level1(type).upkeepCoinsPerDay, Level1(SoldierType.Militia).upkeepCoinsPerDay, $"{type} is cheaper to keep than militia.");
            }
        }

        /// <summary>Each tier is bought for one specific thing, and this is what each of the three is for.</summary>
        [Test]
        public void EachTierIsBoughtForSomethingTheOthersLack()
        {
            Assert.Greater(Row(SoldierType.Spearman).attackRangeUnits, Row(SoldierType.Militia).attackRangeUnits,
                "The spearman's whole case is that it hits first; without the reach it is an expensive militiaman.");

            Assert.Greater(Level1(SoldierType.ManAtArms).maxHealth, 3 * Level1(SoldierType.Militia).maxHealth,
                "The man-at-arms is armour, and armour that is not several times a peasant's is not worth its price.");

            Assert.Greater(Row(SoldierType.Archer).attackRangeUnits, 3f * Row(SoldierType.Spearman).attackRangeUnits,
                "The archer's case is fighting at a distance nothing else can.");
        }

        /// <summary>The man-at-arms trades speed for armour; without that trade it is strictly better than every other tier and the choice disappears.</summary>
        [Test]
        public void TheManAtArmsIsSlowerThanTheRestOfTheLine()
        {
            foreach (var type in SoldierStats.All)
            {
                if (type == SoldierType.ManAtArms) continue;
                Assert.Less(Level1(SoldierType.ManAtArms).walkSpeed, Level1(type).walkSpeed,
                    $"The man-at-arms keeps up with {type} while outlasting it four times over.");
            }
        }

        /// <summary>
        /// The relationship the archer's whole balance rests on, and the reason
        /// OrcUnit.NotifyAttackedBy exists: an archer outranges an orc's own eyes. Should the sheet
        /// ever be retuned the other way, retaliation becomes dead code and nobody would notice --
        /// the archer would simply be a short-ranged unit that costs too much.
        /// </summary>
        [Test]
        public void TheArcherOutrangesAnOrcsAwareness()
        {
            var orc = BalanceConfig.Instance.Unit("orc");

            Assert.Greater(Row(SoldierType.Archer).attackRangeUnits, orc.engageRadius,
                "An archer that shoots from inside an orc's own aggro radius is just a fragile militiaman.");
            Assert.Greater(Row(SoldierType.Archer).engageRadius, Row(SoldierType.Archer).attackRangeUnits,
                "An archer has to notice something before it is in range to shoot it, or it never opens fire without an explicit order.");
        }

        /// <summary>The archer pays for its range in survivability. If it were also sturdy there would be no reason to raise anything else.</summary>
        [Test]
        public void TheArcherIsFragile()
        {
            foreach (var type in SoldierStats.All)
            {
                if (type == SoldierType.Archer || type == SoldierType.Militia) continue;
                Assert.Less(Level1(SoldierType.Archer).maxHealth, Level1(type).maxHealth,
                    $"The archer outlasts {type} as well as outranging it.");
            }
        }

        /// <summary>Levels are the Laboratory's half of the ladder: each one has to be worth the research it costs.</summary>
        [Test]
        public void EveryTierGetsStrongerWithItsResearchedLevels()
        {
            foreach (var type in SoldierStats.All)
            {
                var row = Row(type);
                for (var level = 2; level <= UnitBalance.MaxLevel; level++)
                {
                    if (row.ResearchToReach(level) == null) continue;

                    Assert.Greater(row.LevelStats(level).maxHealth, row.LevelStats(level - 1).maxHealth,
                        $"{type} level {level} is researched and no sturdier than level {level - 1}.");
                    Assert.Greater(row.LevelStats(level).attackDamage, row.LevelStats(level - 1).attackDamage,
                        $"{type} level {level} is researched and hits no harder than level {level - 1}.");
                }
            }
        }

        /// <summary>Militia is the whole army until the Laboratory is built, so its unlock must not exist and its levels must.</summary>
        [Test]
        public void MilitiaKeepsItsOwnLevels()
        {
            Assert.IsNotNull(Row(SoldierType.Militia).ResearchToReach(2), "Militia lost its level 2, which is the Laboratory's only soldier topic in a young settlement.");
        }
    }
}
