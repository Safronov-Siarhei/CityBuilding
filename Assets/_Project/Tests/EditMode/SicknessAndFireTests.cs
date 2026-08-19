using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Core;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// The two formulas the new hazards are built from, with their weights stated here rather than
    /// read from the sheet -- the same split the raid ramps and the progression score use. What has
    /// to survive a retune is the SHAPE: illness rises with hunger and with thirst and can never
    /// exceed certainty, and a fire gets shorter with every firefighter but never instant.
    /// </summary>
    public class SicknessAndFireTests
    {
        private const float Base = 0.01f;
        private const float Hunger = 0.15f;
        private const float Thirst = 0.12f;

        private static float Risk(bool hungry, float unwatered) =>
            SicknessManager.ComputeRisk(hungry, unwatered, Base, Hunger, Thirst);

        [Test]
        public void AFedAndWateredTown_RunsOnlyTheBackgroundRisk()
        {
            Assert.AreEqual(Base, Risk(hungry: false, unwatered: 0f), 0.0001f);
        }

        [Test]
        public void HungerAndThirst_EachRaiseTheRisk()
        {
            Assert.Greater(Risk(hungry: true, unwatered: 0f), Risk(hungry: false, unwatered: 0f),
                "Going hungry does not make people ill, so the Дом лекаря has nothing to do with the harvest.");
            Assert.Greater(Risk(hungry: false, unwatered: 1f), Risk(hungry: false, unwatered: 0f),
                "Living out of a well's reach does not make people ill, so the Колодец is decoration again.");
            Assert.Greater(Risk(hungry: true, unwatered: 1f), Risk(hungry: true, unwatered: 0f),
                "The two causes do not add up, so a starving dry town is no worse off than a starving watered one.");
        }

        /// <summary>Half the housing dry has to be half the penalty: weighting by capacity is what makes where a well goes a decision rather than a formality.</summary>
        [Test]
        public void ThirstScalesWithHowMuchHousingIsDry()
        {
            var half = Risk(hungry: false, unwatered: 0.5f);

            Assert.Greater(half, Risk(hungry: false, unwatered: 0f));
            Assert.Less(half, Risk(hungry: false, unwatered: 1f));
        }

        [Test]
        public void RiskIsNeverMoreThanCertainAndNeverLessThanNothing()
        {
            Assert.AreEqual(1f, SicknessManager.ComputeRisk(true, 1f, 0.9f, 0.9f, 0.9f), 0.0001f);
            Assert.GreaterOrEqual(SicknessManager.ComputeRisk(false, -5f, 0f, 0f, 0f), 0f);
        }

        private const float Unattended = 60f;
        private const float SavedEach = 12f;
        private const float Minimum = 5f;

        private static float Burn(int firefighters) => BuildingFire.BurnSeconds(firefighters, Unattended, SavedEach, Minimum);

        [Test]
        public void AFireNobodyAttends_BurnsForTheFullTime()
        {
            Assert.AreEqual(Unattended, Burn(0), 0.0001f);
        }

        [Test]
        public void EveryFirefighterShortensIt()
        {
            Assert.AreEqual(Unattended - SavedEach, Burn(1), 0.0001f);
            Assert.Less(Burn(3), Burn(2));
        }

        /// <summary>A crowd of firefighters must not make a fire free: without the floor, enough of them would put one out before it did any damage at all, and the mechanic would stop existing above a certain town size.</summary>
        [Test]
        public void NoNumberOfFirefightersMakesAFireInstant()
        {
            Assert.AreEqual(Minimum, Burn(100), 0.0001f);
            Assert.Greater(Burn(100), 0f);
        }

        [Test]
        public void ANegativeCrewIsTreatedAsNoneAtAll()
        {
            Assert.AreEqual(Burn(0), BuildingFire.BurnSeconds(-4, Unattended, SavedEach, Minimum), 0.0001f);
        }

        /// <summary>The sheet's own numbers, checked for the handful of ways they could be authored into nonsense.</summary>
        [Test]
        public void TheSheetsHazardNumbersAreSane()
        {
            var config = BalanceConfig.Instance;

            Assert.Greater(config.FireBurnSeconds, 0f, "A fire that lasts no time does no damage, so nothing ever burns down.");
            Assert.Greater(config.FireMinBurnSeconds, 0f, "A zero floor lets a big enough brigade put fires out before they start.");
            Assert.LessOrEqual(config.FireMinBurnSeconds, config.FireBurnSeconds, "Firefighters make fires last longer.");
            Assert.Greater(config.FireDamagePerSecond, 0, "A fire that does no damage is a decoration.");
            Assert.Greater(config.FireSpreadRadiusMeters, 0f, "Fire never spreads, so the Пожарная бригада only ever has one building to worry about.");

            Assert.GreaterOrEqual(config.SicknessBaseChancePerDay, 0f);
            Assert.Greater(config.SicknessHungerChancePerDay, 0f, "Hunger does not cause illness, which is half the design.");
            Assert.Greater(config.SicknessThirstChancePerDay, 0f, "Thirst does not cause illness, so the Колодец does nothing again.");
            Assert.Greater(config.SicknessDaysBeforeDeath, 0, "The ill die the same day they take to bed, with no chance to treat them.");
            Assert.Greater(config.HealPerHealerPerDay, 0, "A healer heals nobody.");

            // The background risk alone must not be enough to keep a town permanently ill: a
            // settlement doing everything right should be able to empty its sickbeds.
            Assert.Less(config.SicknessBaseChancePerDay, 0.1f,
                "A fed, watered settlement still loses a tenth of its people to illness every day, which no healer could keep up with.");
        }

        /// <summary>The three buildings this all exists for have to actually have a radius, or every one of them is still decoration.</summary>
        [Test]
        public void TheServiceBuildingsHaveAReach()
        {
            foreach (var id in new[] { "Well", "HealerHouse", "FireBrigade" })
            {
                var building = BalanceConfig.Instance.Building(id);
                Assert.IsNotNull(building, $"No buildings row for '{id}'.");
                Assert.GreaterOrEqual(building.levels.Count, 2, $"{id} has fewer than two levels in the sheet.");
                Assert.Greater(building.levels[0].serviceRadius, 0, $"{id} serves nothing at all -- its service_radius is zero.");
                Assert.GreaterOrEqual(building.levels[1].serviceRadius, building.levels[0].serviceRadius,
                    $"Upgrading {id} does not widen its reach, which is the only reason to upgrade it.");
            }
        }

        /// <summary>A Дом лекаря and a Пожарная бригада with no worker slots would stand there staffed by nobody, and both mechanics would be dead on arrival.</summary>
        [Test]
        public void TheStaffedServiceBuildingsHaveSlots()
        {
            foreach (var id in new[] { "HealerHouse", "FireBrigade" })
            {
                var building = BalanceConfig.Instance.Building(id);
                Assert.Greater(building.levels[0].maxWorkers, 0, $"{id} has nowhere to put a worker, so it can never do its job.");
            }
        }
    }
}
