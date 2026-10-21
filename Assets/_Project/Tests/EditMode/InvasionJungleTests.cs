using CityBuilder.Combat;
using CityBuilder.Invasion;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// The jungle's rules with the numbers stated here: who lives in a camp of each level, how the
    /// monsters grow with the waves the base has stood, and the shape of a beast's bite.
    /// </summary>
    public class InvasionJungleTests
    {
        [Test]
        public void ACampsLevel_SaysWhoLivesThere()
        {
            Assert.AreEqual(OrcKind.Wolf, InvasionJungle.KindOfCamp(1));
            Assert.AreEqual(OrcKind.Bear, InvasionJungle.KindOfCamp(2));
            Assert.AreEqual(OrcKind.Troll, InvasionJungle.KindOfCamp(3));
            Assert.IsTrue(OrcUnit.IsMonster(OrcKind.Giant));
            Assert.IsFalse(OrcUnit.IsMonster(OrcKind.Chief), "An orc counted as a jungle monster.");
        }

        [Test]
        public void TheMonsters_GrowWithTheWavesSurvived()
        {
            Assert.AreEqual(1, InvasionJungle.MonsterLevel(0, 5));
            Assert.AreEqual(1, InvasionJungle.MonsterLevel(4, 5));
            Assert.AreEqual(2, InvasionJungle.MonsterLevel(5, 5));
            Assert.AreEqual(5, InvasionJungle.MonsterLevel(20, 5));
            Assert.AreEqual(1, InvasionJungle.MonsterLevel(20, 0), "A zero step must mean no growth, not a division by zero.");
        }

        [Test]
        public void ABite_DrawsBack_Snaps_AndComesHome()
        {
            Assert.AreEqual(0f, FigureAnimator.BiteAngle(0f), 0.01f);
            Assert.Less(FigureAnimator.BiteAngle(0.3f), -10f, "The head does not draw back before the bite.");
            Assert.Greater(FigureAnimator.BiteAngle(0.5f), 20f, "The head does not snap down.");
            Assert.AreEqual(0f, FigureAnimator.BiteAngle(1f), 0.01f, "The head does not come back to rest.");
        }
    }
}
