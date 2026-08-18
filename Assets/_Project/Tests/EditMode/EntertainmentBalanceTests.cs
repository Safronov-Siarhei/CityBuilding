using CityBuilder.Buildings;
using CityBuilder.Core;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// The entertainment category against the sheet. The failure this guards is the one the whole
    /// feature was built to end: a building that stands there, costs resources and does nothing,
    /// which is indistinguishable in play from a building that is simply not implemented yet.
    /// </summary>
    public class EntertainmentBalanceTests
    {
        private static BalanceConfig Config
        {
            get
            {
                var config = UnityEngine.Resources.Load<BalanceConfig>(BalanceConfig.ResourcePath);
                Assert.IsNotNull(config, "No BalanceConfig asset -- rebuild it from the CSVs.");
                return config;
            }
        }

        [Test]
        public void EveryEntertainmentBuilding_ActuallyEntertains()
        {
            var counted = 0;

            foreach (var building in Config.Buildings)
            {
                if (building.category != BuildingCategory.Entertainment) continue;

                counted++;
                Assert.Greater(building.levels[0].happiness, 0,
                    $"{building.id} is in the Развлечения category and contributes nothing to the settlement's mood -- " +
                    "fill in its happiness column, or it is a placeholder the player pays for and gets nothing from.");
            }

            Assert.Greater(counted, 0, "The buildings tab has no Развлечения at all, so this test is watching nothing.");
        }

        /// <summary>The mirror: a granary or a wall quietly cheering the town up would be a column filled in on the wrong row.</summary>
        [Test]
        public void NothingOutsideTheCategory_Entertains()
        {
            foreach (var building in Config.Buildings)
            {
                if (building.category == BuildingCategory.Entertainment) continue;

                Assert.AreEqual(0, building.levels[0].happiness,
                    $"{building.id} is not entertainment but has a happiness value -- check which row that cell landed on.");
            }
        }

        /// <summary>Upgrading has to be worth it here too: the whole point of a level is that the same plot does more.</summary>
        [Test]
        public void UpgradingAnEntertainmentBuilding_LiftsTheMoodFurther()
        {
            foreach (var building in Config.Buildings)
            {
                if (building.category != BuildingCategory.Entertainment) continue;

                for (var level = 2; level <= building.levels.Count; level++)
                {
                    Assert.Greater(building.levels[level - 1].happiness, building.levels[level - 2].happiness,
                        $"{building.id} at level {level} is worth no more to the settlement's mood than at level {level - 1}.");
                }
            }
        }
    }
}
