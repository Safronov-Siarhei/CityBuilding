using CityBuilder.Combat;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// The progression score's shape, with the weights stated here rather than read from the sheet
    /// -- the same split the raid ramps use. What must survive a retune is that every one of the
    /// five terms actually moves the number, and that nothing about the score can go backwards
    /// when the player is doing well.
    ///
    /// The reason to pin the terms individually: a weight left at zero, or a term quietly dropped
    /// out of the sum, produces a score that still looks reasonable and still ramps -- and raids
    /// would simply stop noticing one whole half of what the player had done.
    /// </summary>
    public class PlayerProgressionTests
    {
        private const float PerBuildingLevel = 1f;
        private const float PerCitizen = 1f;
        private const float PerSoldierLevel = 2f;
        private const float PerDefencePoint = 0.2f;
        private const float PerProducedUnit = 0.02f;

        private static int Score(int buildingLevels = 0, int population = 0, int soldierLevels = 0, int defence = 0, int produced = 0)
        {
            return PlayerProgression.Compute(buildingLevels, population, soldierLevels, defence, produced,
                PerBuildingLevel, PerCitizen, PerSoldierLevel, PerDefencePoint, PerProducedUnit);
        }

        [Test]
        public void AnEmptyMap_ScoresZero()
        {
            Assert.AreEqual(0, Score());
        }

        [Test]
        public void EveryTerm_MovesTheScore()
        {
            Assert.Greater(Score(buildingLevels: 10), 0, "What the player has built does not count towards the raids they draw.");
            Assert.Greater(Score(population: 10), 0, "How many people live there does not count.");
            Assert.Greater(Score(soldierLevels: 10), 0, "The garrison does not count -- an army could be raised for free, as far as the orcs are concerned.");
            Assert.Greater(Score(defence: 100), 0, "How well defended the place is does not count.");
            Assert.Greater(Score(produced: 1000), 0, "Everything the settlement has ever made does not count.");
        }

        /// <summary>
        /// Upgrading is progress, not just sprawling: levels are summed rather than buildings
        /// counted, so ten level-1 huts and five level-2 ones are not the same settlement.
        /// </summary>
        [Test]
        public void UpgradingCountsTheSameAsBuildingMore()
        {
            Assert.Greater(Score(buildingLevels: 30), Score(buildingLevels: 20));
        }

        /// <summary>A trained army is worth more to the orcs than a bigger untrained one -- soldiers are counted as heads TIMES the level their type has been researched to.</summary>
        [Test]
        public void ATrainedArmyOutweighsALargerUntrainedOne()
        {
            var tenAtLevelThree = Score(soldierLevels: 30);
            var twentyAtLevelOne = Score(soldierLevels: 20);

            Assert.Greater(tenAtLevelThree, twentyAtLevelOne);
        }

        /// <summary>
        /// The production term rises with everything the settlement has ever made. That it cannot
        /// FALL is the other half, and it is not testable from here -- Compute is handed the total
        /// rather than reading it -- so ResourceManagerTests owns that half.
        /// </summary>
        [Test]
        public void TheProductionTerm_RisesWithTheLifetimeTotal()
        {
            Assert.Greater(Score(produced: 5000), Score(produced: 500));
        }

        [Test]
        public void NegativeInputs_CannotDragTheScoreDown()
        {
            // Nothing should ever hand these a negative, but a score below zero would run the raid
            // ramps backwards into their own clamps, and the map would get EASIER as it went wrong.
            Assert.AreEqual(Score(population: 10), Score(population: 10, buildingLevels: -50));
            Assert.GreaterOrEqual(Score(buildingLevels: -100, population: -100, produced: -100), 0);
        }

        /// <summary>The whole point of the composite: two settlements that reached the same place by different routes are worth roughly the same to the orcs.</summary>
        [Test]
        public void ABuilderAndAWarlord_AreBothProgress()
        {
            var builder = Score(buildingLevels: 40, population: 40, produced: 2000);
            var warlord = Score(buildingLevels: 10, population: 15, soldierLevels: 40, defence: 200, produced: 500);

            Assert.Greater(builder, 0);
            Assert.Greater(warlord, 0);
        }
    }
}
