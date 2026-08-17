using CityBuilder.Core;
using CityBuilder.Resources;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// The settlement's daily meal: how much it needs, how it is served out of several stores, and
    /// what going short costs.
    ///
    /// Worth pinning down because every one of these numbers is either a division that rounds the
    /// wrong way at the edges (a town of one still has to eat) or a rule with a player-visible
    /// consequence the code alone doesn't state -- most of all the grace period, which is the whole
    /// difference between a warning and a wipe.
    /// </summary>
    public class FoodConsumptionTests
    {
        /// <summary>Rounded up, deliberately: at half a ration each, three citizens must cost two units rather than a free one and a half.</summary>
        [Test]
        public void Demand_RoundsUp()
        {
            var perMouth = BalanceConfig.Instance.FoodPerMouthPerDay;
            var expected = UnityEngine.Mathf.CeilToInt(3 * perMouth);

            Assert.AreEqual(expected, FoodConsumptionManager.ComputeDemand(3));
        }

        [Test]
        public void Demand_IsNothingForAnEmptySettlement()
        {
            Assert.AreEqual(0, FoodConsumptionManager.ComputeDemand(0));
        }

        [Test]
        public void TheMeal_IsSpreadAcrossEveryStore()
        {
            var eaten = FoodConsumptionManager.Distribute(4, new[] { 10, 10 });

            Assert.AreEqual(2, eaten[0]);
            Assert.AreEqual(2, eaten[1], "A settlement with two kinds of food must eat both -- variety is scored from what actually reached the table.");
        }

        /// <summary>The rest of the demand has to fall through to the stores that do have something, not stop at the empty one.</summary>
        [Test]
        public void TheMeal_FallsBackToWhatIsLeft()
        {
            var eaten = FoodConsumptionManager.Distribute(5, new[] { 1, 10 });

            Assert.AreEqual(1, eaten[0]);
            Assert.AreEqual(4, eaten[1]);
        }

        [Test]
        public void TheMeal_TakesNoMoreThanIsThere()
        {
            var eaten = FoodConsumptionManager.Distribute(20, new[] { 2, 3 });

            Assert.AreEqual(2, eaten[0]);
            Assert.AreEqual(3, eaten[1]);
        }

        [Test]
        public void TheMeal_TakesNothingWhenNobodyIsHungry()
        {
            var eaten = FoodConsumptionManager.Distribute(0, new[] { 5, 5 });

            Assert.AreEqual(0, eaten[0]);
            Assert.AreEqual(0, eaten[1]);
        }

        /// <summary>A shortfall of one ration costs one citizen -- the loss is proportional to how badly short the settlement was, not a flat toll.</summary>
        [Test]
        public void Starvation_KillsInProportionToTheShortfall()
        {
            var perMouth = BalanceConfig.Instance.FoodPerMouthPerDay;
            var shortfall = UnityEngine.Mathf.CeilToInt(perMouth * 4);

            Assert.AreEqual(4, FoodConsumptionManager.ComputeStarvationDeaths(shortfall));
        }

        /// <summary>
        /// Being short at all has to cost somebody. The guard matters at rations above one unit,
        /// where a shortfall smaller than a single ration divides down to zero -- a settlement one
        /// crumb short would otherwise starve for free, forever.
        /// </summary>
        [Test]
        public void Starvation_AlwaysCostsAtLeastOne()
        {
            Assert.GreaterOrEqual(FoodConsumptionManager.ComputeStarvationDeaths(1), 1);
        }

        [Test]
        public void Starvation_CostsNothingWhenThereIsNoShortfall()
        {
            Assert.AreEqual(0, FoodConsumptionManager.ComputeStarvationDeaths(0));
        }

        /// <summary>Flour lives in the same storehouse as bread but is not food -- eating it would drain the bakery's supply before it ever got there.</summary>
        [Test]
        public void FlourIsNotEaten()
        {
            Assert.IsFalse(ResourceDiet.IsEdible(ResourceType.Flour));
            Assert.IsTrue(ResourceDiet.IsEdible(ResourceType.Bread));
            Assert.IsTrue(ResourceDiet.IsEdible(ResourceType.Food));
        }

        /// <summary>Everything the settlement eats has to be storable somewhere, or it could never be kept long enough to eat.</summary>
        [Test]
        public void EverythingEdible_HasAStorehouse()
        {
            foreach (var type in ResourceDiet.Edible)
            {
                Assert.AreNotEqual(ResourceStorageGroup.None, ResourceStorage.GroupOf(type), $"{type} is eaten but has nowhere to be kept.");
            }
        }

        [Test]
        public void HappinessFood_IsFullOnAVariedTable()
        {
            var target = BalanceConfig.Instance.FoodVarietyTarget;

            Assert.AreEqual(100, HappinessManager.ComputeFoodScore(target, hungryDaysInARow: 0));
        }

        /// <summary>Hunger is not a milder form of a dull diet: a settlement going short scores zero however many kinds of food it managed to scrape together.</summary>
        [Test]
        public void HappinessFood_IsZeroWhileHungry()
        {
            var target = BalanceConfig.Instance.FoodVarietyTarget;

            Assert.AreEqual(0, HappinessManager.ComputeFoodScore(target, hungryDaysInARow: 1));
        }

        [Test]
        public void HappinessDeaths_IsFullWhenNobodyHasDied()
        {
            Assert.AreEqual(100, HappinessManager.ComputeDeathScore(0));
        }

        [Test]
        public void HappinessDeaths_FallsWithEachLoss()
        {
            var penalty = BalanceConfig.Instance.HappinessPenaltyPerDeath;

            Assert.AreEqual(100 - penalty, HappinessManager.ComputeDeathScore(1));
            Assert.AreEqual(0, HappinessManager.ComputeDeathScore(1000), "The score has to bottom out at 0 rather than going negative and dragging the average below the floor.");
        }

        /// <summary>Every one of these is authored in the economy tab; a zero or a missing key turns the whole mechanic off silently.</summary>
        [Test]
        public void TheFoodNumbers_AreUsable()
        {
            var config = BalanceConfig.Instance;

            Assert.Greater(config.FoodPerMouthPerDay, 0f, "Nobody eats anything, so food can never run out.");
            Assert.Greater(config.HungryDaysBeforeDeaths, 0, "Deaths on the very first hungry day leave the player no chance to react.");
            Assert.Greater(config.FoodVarietyTarget, 0, "A zero variety target makes the food score meaningless.");
            Assert.Greater(config.DeathsMemoryDays, 0, "Deaths would be forgotten the instant they happened.");
            Assert.Greater(config.HappinessPenaltyPerDeath, 0, "Losing citizens would cost nothing in happiness.");

            Assert.LessOrEqual(config.FoodVarietyTarget, ResourceDiet.Edible.Count,
                "The variety target is higher than the number of foods that exist, so a full score is unreachable no matter what the player builds.");
        }
    }
}
