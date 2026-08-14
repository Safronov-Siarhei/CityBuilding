using CityBuilder.Core;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// Guards the balance sheet itself. Once numbers are authored in a spreadsheet, the ways they
    /// break change: not "someone wrote a bad line of code" but "a cell got emptied, a decimal comma
    /// slipped in, a row was renamed, the CSV never got re-imported". None of those produce a
    /// compile error, and most of them produce a game that boots and then behaves like a ghost.
    ///
    /// So this checks that the asset is real, that every unit the code asks for exists in it, and
    /// that nothing landed in a range that would quietly break the game (a zero interval divides
    /// time into infinite swings, zero health kills a unit on spawn). It does NOT assert specific
    /// values -- that's what the sheet is for.
    /// </summary>
    public class BalanceConfigTests
    {
        [Test]
        public void ConfigAsset_Exists()
        {
            var config = UnityEngine.Resources.Load<BalanceConfig>(BalanceConfig.ResourcePath);

            Assert.IsNotNull(config,
                $"No BalanceConfig at Resources/{BalanceConfig.ResourcePath}. Rebuild it from the CSVs " +
                "(CityBuilder/Balance/Rebuild Config From CSV, or any full SetupProject run) -- without it " +
                "the game silently falls back to the values hardcoded in BalanceConfig.");
        }

        [TestCase("militia")]
        [TestCase("orc")]
        public void UnitsTab_HasEveryUnitTheCodeAsksFor(string id)
        {
            var config = UnityEngine.Resources.Load<BalanceConfig>(BalanceConfig.ResourcePath);
            Assert.IsNotNull(config);

            var found = false;
            foreach (var unit in config.Units)
            {
                if (unit.id != id) continue;
                found = true;
                break;
            }

            Assert.IsTrue(found, $"The units tab has no row with id '{id}'.");
        }

        [Test]
        public void EveryUnit_HasNumbersThatCanBeFoughtWith()
        {
            var config = UnityEngine.Resources.Load<BalanceConfig>(BalanceConfig.ResourcePath);
            Assert.IsNotNull(config);
            Assert.IsNotEmpty(config.Units, "The units tab imported as empty -- most likely the CSV lost its rows or its header.");

            foreach (var unit in config.Units)
            {
                Assert.Greater(unit.maxHealth, 0, $"{unit.id}: zero health dies the frame it spawns.");
                Assert.Greater(unit.attackDamage, 0, $"{unit.id}: zero damage can never finish a fight.");
                Assert.Greater(unit.attackIntervalSeconds, 0f, $"{unit.id}: a zero attack interval swings every frame.");
                Assert.Greater(unit.walkSpeed, 0f, $"{unit.id}: zero speed never reaches anything.");
                Assert.Greater(unit.attackRangeUnits, 0f, $"{unit.id}: zero reach can never land a hit.");
                Assert.GreaterOrEqual(unit.attackRangeStructures, unit.attackRangeUnits,
                    $"{unit.id}: reach against a structure should be at least the melee reach -- a building's transform sits at its centre.");
                Assert.GreaterOrEqual(unit.engageRadius, unit.attackRangeUnits,
                    $"{unit.id}: a unit that engages from closer than it can hit will walk up and stare.");
                Assert.IsNotEmpty(unit.displayName, $"{unit.id}: an empty display name shows up as a blank row in the army panel.");
            }
        }

        [Test]
        public void Economy_IsInsideSaneRanges()
        {
            var config = BalanceConfig.Instance;

            Assert.Greater(config.DayLengthSeconds, 0f, "A zero-length day fires every day-based system every frame.");
            Assert.Greater(config.ArmyMaxSize, 0);
            Assert.Greater(config.PortalMaxHealth, 0, "A portal with no health is destroyed by the first swing, ending the map instantly.");
            Assert.Greater(config.RaidIntervalSeconds, 0f, "A zero raid interval spawns a squad every frame.");
            Assert.GreaterOrEqual(config.RaidMaxSize, config.RaidBaseSize, "The raid ceiling can't be below the opening squad.");
            Assert.Greater(config.DefenceAttackIntervalSeconds, 0f);
            Assert.GreaterOrEqual(config.CoinsPerCitizenPerDayAtMaxTax, 0f);
            Assert.Greater(config.DecayPerDayAtLevel1, 0f, "Zero decay per day quietly disables the whole decay/repair loop.");
            Assert.That(config.DecayPenaltyThreshold, Is.InRange(0f, 1f), "Decay is a 0..1 fraction.");
            Assert.That(config.MinDecayProductionMultiplier, Is.InRange(0f, 1f), "A decayed building produces LESS, never more.");
            Assert.That(config.RepairCostFraction, Is.InRange(0f, 1f), "Repair is meant to be cheaper than rebuilding.");
            Assert.Greater(config.WoodPerTree, 0);
            Assert.Greater(config.StonePerRock, 0);
        }
    }
}
