using CityBuilder.UI;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// The one number the player reads most often. The rule it follows is not "always show the
    /// ceiling": a top bar full of "50/200" teaches the player to stop reading it, so the limit only
    /// appears once the store is nearly full -- which is exactly when a stuck stockpile needs
    /// explaining.
    /// </summary>
    public class ResourceHUDFormatTests
    {
        [Test]
        public void AComfortableStockpile_ShowsJustTheAmount()
        {
            Assert.AreEqual("50", ResourceHUDController.Format(50, 200, false));
            Assert.AreEqual("0", ResourceHUDController.Format(0, 100, false));
        }

        [Test]
        public void ANearlyFullStore_ShowsWhatItCanHold()
        {
            // 80% of the ceiling is where the warning starts.
            Assert.AreEqual("79", ResourceHUDController.Format(79, 100, false));
            Assert.AreEqual("80/100", ResourceHUDController.Format(80, 100, false));
            Assert.AreEqual("100/100", ResourceHUDController.Format(100, 100, false));
        }

        [Test]
        public void SomethingWithNoCeilingAtAll_NeverShowsOne()
        {
            // Population is a headcount, not a warehoused resource (see ResourceStorage.GroupOf).
            Assert.AreEqual("12", ResourceHUDController.Format(12, int.MaxValue, false));
        }

        [Test]
        public void TheInfiniteResourcesCheat_OverridesEverything()
        {
            Assert.AreEqual("∞", ResourceHUDController.Format(100, 100, true));
            Assert.AreEqual("∞", ResourceHUDController.Format(0, int.MaxValue, true));
        }
    }
}
