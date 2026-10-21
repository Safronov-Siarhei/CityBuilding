using CityBuilder.Combat;
using CityBuilder.Invasion;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// The ramp an Invasion run's waves are built from, with the numbers stated here rather than
    /// whatever the sheet holds today: the lanes open one by one, each lane grows to a ceiling, the
    /// orcs level up every few waves, the kinds come in at their waves, a boss wave every few.
    /// </summary>
    public class InvasionWavesTests
    {
        [Test]
        public void TheLanes_OpenOneByOne_AndNeverPastTheMap()
        {
            Assert.AreEqual(1, InvasionWaves.LanesAt(1, 4, 8, 3));
            Assert.AreEqual(1, InvasionWaves.LanesAt(3, 4, 8, 3));
            Assert.AreEqual(2, InvasionWaves.LanesAt(4, 4, 8, 3));
            Assert.AreEqual(2, InvasionWaves.LanesAt(7, 4, 8, 3));
            Assert.AreEqual(3, InvasionWaves.LanesAt(8, 4, 8, 3));
            Assert.AreEqual(3, InvasionWaves.LanesAt(50, 4, 8, 3));
            Assert.AreEqual(2, InvasionWaves.LanesAt(50, 4, 8, 2), "More lanes than the map has camps.");
        }

        [Test]
        public void ALane_Grows_UpToItsCeiling()
        {
            Assert.AreEqual(3, InvasionWaves.OrcsPerLane(1, 3, 2, 10));
            Assert.AreEqual(3, InvasionWaves.OrcsPerLane(2, 3, 2, 10));
            Assert.AreEqual(4, InvasionWaves.OrcsPerLane(3, 3, 2, 10));
            Assert.AreEqual(7, InvasionWaves.OrcsPerLane(10, 3, 2, 10));
            Assert.AreEqual(10, InvasionWaves.OrcsPerLane(40, 3, 2, 10));
            Assert.AreEqual(3, InvasionWaves.OrcsPerLane(40, 3, 0, 10), "A zero step must mean no growth, not a division by zero.");
        }

        [Test]
        public void TheOrcs_LevelUp_EveryFewWaves()
        {
            Assert.AreEqual(1, InvasionWaves.OrcLevel(1, 5, 12));
            Assert.AreEqual(1, InvasionWaves.OrcLevel(5, 5, 12));
            Assert.AreEqual(2, InvasionWaves.OrcLevel(6, 5, 12));
            Assert.AreEqual(5, InvasionWaves.OrcLevel(21, 5, 12));
            Assert.AreEqual(12, InvasionWaves.OrcLevel(200, 5, 12));
        }

        [Test]
        public void TheKinds_ComeIn_AtTheirWaves()
        {
            for (var i = 0; i < 10; i++)
            {
                Assert.AreEqual(OrcKind.Raider, InvasionWaves.KindAt(i, 2, false, 3, 3, 6, 4), $"Wave 2, orc {i}: only raiders before the throwers' wave.");
            }
            Assert.AreEqual(OrcKind.Thrower, InvasionWaves.KindAt(2, 3, false, 3, 3, 6, 4));
            Assert.AreEqual(OrcKind.Raider, InvasionWaves.KindAt(3, 3, false, 3, 3, 6, 4), "A brute came before its wave.");
            Assert.AreEqual(OrcKind.Brute, InvasionWaves.KindAt(3, 6, false, 3, 3, 6, 4));
            Assert.AreEqual(OrcKind.Brute, InvasionWaves.KindAt(11, 6, false, 3, 3, 6, 4), "Where a brute and a thrower fall on one orc, the brute wins.");
            Assert.AreEqual(OrcKind.Chief, InvasionWaves.KindAt(0, 5, true, 3, 3, 6, 4));
            Assert.AreEqual(OrcKind.Raider, InvasionWaves.KindAt(0, 5, false, 3, 3, 6, 4));
        }

        [Test]
        public void BossWaves_AndTheWavesGold()
        {
            Assert.IsFalse(InvasionWaves.IsBossWave(4, 5));
            Assert.IsTrue(InvasionWaves.IsBossWave(5, 5));
            Assert.IsTrue(InvasionWaves.IsBossWave(10, 5));
            Assert.IsFalse(InvasionWaves.IsBossWave(5, 0));
            Assert.AreEqual(70, InvasionWaves.GoldForWave(7, 10), "A wave pays 10 × its number (the user, 2026-10-21).");
        }
    }
}
