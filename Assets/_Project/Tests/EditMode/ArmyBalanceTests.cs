using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Resources;
using NUnit.Framework;
using UnityEngine;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// Pins down the balance relationships the militia tier was designed around, rather than the
    /// specific numbers -- every stat here is meant to stay tunable, but the RELATIONSHIPS are the
    /// design ("жители с вилами": weak alone, dangerous in numbers) and shouldn't drift silently
    /// when someone nudges a constant.
    ///
    /// Time-to-kill is modelled as hits x interval, which is how the units actually resolve it:
    /// SoldierUnit and OrcUnit both count down a fixed timer and swing when it elapses.
    /// </summary>
    public class ArmyBalanceTests
    {
        // Both sides straight from the balance sheet, so a retune of either one is checked against
        // the other rather than against a number copied into this file once.
        private static UnitBalance Orc => BalanceConfig.Instance.Unit("orc");
        private static int OrcHealth => Orc.LevelStats(1).maxHealth;
        private static int OrcDamage => Orc.LevelStats(1).attackDamage;
        private static float OrcAttackInterval => Orc.LevelStats(1).attackIntervalSeconds;

        private static float TimeToKill(int targetHealth, int damagePerHit, float interval, int attackers = 1)
        {
            var hits = Mathf.CeilToInt(targetHealth / (float)(damagePerHit * attackers));
            return hits * interval;
        }

        [Test]
        public void Militia_LosesOneOnOneAgainstAnOrc()
        {
            var militiaKillsOrcIn = TimeToKill(OrcHealth, SoldierStats.AttackDamage(SoldierType.Militia), SoldierStats.AttackIntervalSeconds(SoldierType.Militia));
            var orcKillsMilitiaIn = TimeToKill(SoldierStats.MaxHealth(SoldierType.Militia), OrcDamage, OrcAttackInterval);

            Assert.Less(orcKillsMilitiaIn, militiaKillsOrcIn,
                "An unarmoured peasant with a pitchfork is supposed to lose a straight duel with an orc -- " +
                "if this flips, militia stop being the cheap-and-expendable tier the design calls for.");
        }

        [Test]
        public void Militia_WinInNumbers()
        {
            var threeKillOrcIn = TimeToKill(OrcHealth, SoldierStats.AttackDamage(SoldierType.Militia), SoldierStats.AttackIntervalSeconds(SoldierType.Militia), attackers: 3);
            var orcKillsOneIn = TimeToKill(SoldierStats.MaxHealth(SoldierType.Militia), OrcDamage, OrcAttackInterval);

            Assert.Less(threeKillOrcIn, orcKillsOneIn,
                "Three militia should bring an orc down before it can kill even one of them -- numbers are " +
                "the entire point of the tier.");
        }

        [Test]
        public void RecruitCost_IsCoinsOnly()
        {
            var cost = SoldierStats.RecruitCost(SoldierType.Militia);

            Assert.AreEqual(1, cost.Count, "Militia are meant to need no equipment building -- coins only.");
            Assert.AreEqual(ResourceType.Coins, cost[0].type);
            Assert.Greater(cost[0].amount, 0);
        }

        [Test]
        public void Upkeep_IsPositive_AndScalesWithArmySize()
        {
            var one = SoldierStats.TotalUpkeepPerDay(new[] { SoldierType.Militia });
            var three = SoldierStats.TotalUpkeepPerDay(new[] { SoldierType.Militia, SoldierType.Militia, SoldierType.Militia });

            Assert.Greater(one, 0, "A free army removes the whole economic tradeoff behind the size cap.");
            Assert.AreEqual(one * 3, three);
        }

        [Test]
        public void ArmyCap_IsSmallEnoughForAPhone()
        {
            // The cap is a device-performance decision as much as a design one, so the sheet is
            // free to tune it -- within a range that keeps twenty-ish units on screen, not two
            // hundred.
            Assert.GreaterOrEqual(SoldierStats.MaxArmySize, 5);
            Assert.LessOrEqual(SoldierStats.MaxArmySize, 40);
        }

        [Test]
        public void FormationOffset_FirstSlotIsTheCentre()
        {
            Assert.AreEqual(Vector3.zero, ArmyGroup.FormationOffset(0));
        }

        [Test]
        public void FormationOffset_NeverPutsTwoSoldiersInTheSameSpot()
        {
            // A full army's worth of slots -- if any two collide, that many soldiers try to stand
            // in one point and shove each other around the map.
            for (var a = 0; a < SoldierStats.MaxArmySize; a++)
            {
                for (var b = a + 1; b < SoldierStats.MaxArmySize; b++)
                {
                    var distance = Vector3.Distance(ArmyGroup.FormationOffset(a), ArmyGroup.FormationOffset(b));
                    Assert.Greater(distance, 0.3f, $"Formation slots {a} and {b} overlap.");
                }
            }
        }

        [Test]
        public void FormationOffset_StaysCompact()
        {
            // The whole formation has to fit in a sane patch of ground -- a group that spreads over
            // half the map can't be commanded as one thing.
            for (var i = 0; i < SoldierStats.MaxArmySize; i++)
            {
                Assert.Less(ArmyGroup.FormationOffset(i).magnitude, 4f);
            }
        }
    }
}
