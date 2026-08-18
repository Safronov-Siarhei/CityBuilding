using CityBuilder.Citizens;
using CityBuilder.Core;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// The shape of the migration curve, pinned the way FoodBalanceTests and ArmyBalanceTests pin
    /// theirs: relationships, not the numbers themselves, so the sheet stays free to be retuned
    /// without a red test, while a retune that inverts the design still fails loudly.
    ///
    /// What the design actually says, and what these hold to it: above the threshold people come
    /// and the happier the town the sooner; below it they go and the more miserable the town the
    /// sooner; and the threshold itself is a dead point.
    /// </summary>
    public class MigrationBalanceTests
    {
        private static int Threshold => BalanceConfig.Instance.MigrationHappinessThreshold;

        [Test]
        public void Arriving_IsSlowestJustAboveTheThreshold()
        {
            var justAbove = MigrationManager.ArriveIntervalSeconds(Threshold + 1);
            var content = MigrationManager.ArriveIntervalSeconds(Threshold + 20);
            var delighted = MigrationManager.ArriveIntervalSeconds(100);

            Assert.Greater(justAbove, content, "A barely tolerable settlement should take longer to attract anyone than a pleasant one.");
            Assert.Greater(content, delighted, "A delighted settlement should be the fastest-growing one there is.");
        }

        [Test]
        public void Arriving_GetsSteadilyFasterAllTheWayUp()
        {
            // Monotonic across the whole band, not just at the ends: a curve that dipped in the
            // middle would mean a stretch where making the town nicer slowed its growth down.
            var previous = MigrationManager.ArriveIntervalSeconds(Threshold + 1);
            for (var happiness = Threshold + 2; happiness <= 100; happiness++)
            {
                var current = MigrationManager.ArriveIntervalSeconds(happiness);
                Assert.LessOrEqual(current, previous, $"Contentment {happiness} attracts settlers more slowly than {happiness - 1} does.");
                previous = current;
            }
        }

        [Test]
        public void Leaving_IsSlowestJustBelowTheThreshold()
        {
            var justBelow = MigrationManager.LeaveIntervalSeconds(Threshold - 1);
            var unhappy = MigrationManager.LeaveIntervalSeconds(Threshold / 2);
            var rockBottom = MigrationManager.LeaveIntervalSeconds(0);

            Assert.Greater(justBelow, unhappy, "A settlement barely under the threshold should bleed people more slowly than a thoroughly unhappy one.");
            Assert.Greater(unhappy, rockBottom, "A settlement at zero contentment should empty fastest of all.");
        }

        [Test]
        public void Leaving_GetsSteadilyFasterAllTheWayDown()
        {
            var previous = MigrationManager.LeaveIntervalSeconds(Threshold - 1);
            for (var happiness = Threshold - 2; happiness >= 0; happiness--)
            {
                var current = MigrationManager.LeaveIntervalSeconds(happiness);
                Assert.LessOrEqual(current, previous, $"Contentment {happiness} drives people out more slowly than {happiness + 1} does.");
                previous = current;
            }
        }

        [Test]
        public void OnePointOfMisery_CostsMoreThanOnePointOfContentmentBuys()
        {
            // The design's asymmetry, and the reason a town in trouble empties faster than a happy
            // one fills: the departure band is 29 points wide against the arrival band's 69, so the
            // same total swing in pace is packed into less than half the room.
            var arrivalSwingPerPoint =
                (MigrationManager.ArriveIntervalSeconds(Threshold + 1) - MigrationManager.ArriveIntervalSeconds(100))
                / (100 - Threshold - 1);
            var departureSwingPerPoint =
                (MigrationManager.LeaveIntervalSeconds(Threshold - 1) - MigrationManager.LeaveIntervalSeconds(0))
                / (Threshold - 1);

            Assert.Greater(departureSwingPerPoint, arrivalSwingPerPoint,
                "Losing a point of contentment should hurt more than gaining one helps -- otherwise a bad patch is no more urgent than a good one is rewarding.");
        }

        [Test]
        public void TheSettlingInGrace_OutlastsSeveralArrivals()
        {
            // The grace exists so a first settlement is not abandoned before the player has built
            // anything to be content about. A grace shorter than the wait for a couple of settlers
            // would be decoration.
            Assert.Greater(BalanceConfig.Instance.SettlingInSeconds,
                MigrationManager.ArriveIntervalSeconds(BalanceConfig.Instance.MigrationHappinessThreshold + 1) * 2f,
                "The settling-in period is too short to shelter a new settlement through even two arrivals.");
        }

        [Test]
        public void TheSettlementIsNeverAllowedToEmpty()
        {
            // Unhappiness stalls a town; it does not end the map. Losing the last citizens stays
            // starvation's business (see GameOverManager).
            Assert.GreaterOrEqual(BalanceConfig.Instance.MigrationMinPopulation, 1,
                "With a floor of zero, a miserable settlement would quietly play out its own defeat.");
        }
    }
}
