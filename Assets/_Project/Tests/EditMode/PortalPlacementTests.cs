using System.Collections.Generic;
using System.Linq;
using CityBuilder.Combat;
using NUnit.Framework;
using UnityEngine;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// The shape of the portal template: how many slots, how far out each one sits, and how they
    /// are spread around the settlement. Stated with explicit numbers rather than with whatever the
    /// balance sheet currently holds -- this is about the RULES (never fewer than one, never on top
    /// of the Town Hall, never all on one side, always further out than the last), which have to
    /// survive any retune. The sheet's own numbers are checked in BalanceConfigTests instead.
    /// </summary>
    public class PortalPlacementTests
    {
        private const float BaseDistance = 20f;
        private const float Step = 8f;

        private static List<PortalPlacement.Slot> Template(int count) =>
            PortalPlacement.BuildTemplate(count, BaseDistance, Step, 0f);

        [Test]
        public void BuildTemplate_MakesOneSlotPerPortal()
        {
            Assert.AreEqual(5, Template(5).Count);
        }

        [Test]
        public void BuildTemplate_StartsAtTheGivenDistance()
        {
            Assert.AreEqual(BaseDistance, Template(5)[0].DistanceMeters, 0.001f);
        }

        /// <summary>Each portal one step further out than the last, so five portals are five different walks and not one ring the player can sweep in a single pass.</summary>
        [Test]
        public void BuildTemplate_PutsEachPortalAStepFurtherOut()
        {
            var template = Template(5);

            for (var i = 1; i < template.Count; i++)
            {
                Assert.AreEqual(Step, template[i].DistanceMeters - template[i - 1].DistanceMeters, 0.001f,
                    $"Portal {i} is not one step beyond portal {i - 1}.");
            }
        }

        /// <summary>
        /// The spread is the whole point of the template: portals that all came out on one side of
        /// the settlement would leave the other side permanently safe and make four of the five
        /// walls the player builds pointless.
        /// </summary>
        [Test]
        public void BuildTemplate_SpreadsTheBearingsEvenlyAroundTheTownHall()
        {
            var template = Template(5);
            var expected = Mathf.PI * 2f / 5f;

            for (var i = 1; i < template.Count; i++)
            {
                Assert.AreEqual(expected, template[i].AngleRadians - template[i - 1].AngleRadians, 0.001f,
                    $"Portal {i} does not sit an even share of the circle from portal {i - 1}.");
            }
        }

        /// <summary>The offset is what the spawner actually adds to the Town Hall's position, so it has to carry the distance -- and stay on the ground plane, where the map is.</summary>
        [Test]
        public void Offset_IsTheSlotsDistanceAwayAndFlat()
        {
            foreach (var slot in Template(5))
            {
                Assert.AreEqual(slot.DistanceMeters, slot.Offset.magnitude, 0.001f);
                Assert.AreEqual(0f, slot.Offset.y, 0.001f, "A slot lifted off the ground plane would miss the map's ground mesh.");
            }
        }

        [Test]
        public void BuildTemplate_TurnsTheWholeTemplateWithTheAngleOffset()
        {
            var turned = PortalPlacement.BuildTemplate(5, BaseDistance, Step, Mathf.PI);

            Assert.AreEqual(Mathf.PI, turned[0].AngleRadians, 0.001f,
                "The random per-playthrough bearing did not reach the first slot, so every game would open its portals in the same places.");
        }

        /// <summary>A sheet edited to zero portals would leave the map with no objective and no raids, which is not a balance choice anyone can have meant.</summary>
        [Test]
        [TestCase(0)]
        [TestCase(-3)]
        public void BuildTemplate_AlwaysMakesAtLeastOnePortal(int count)
        {
            Assert.AreEqual(1, Template(count).Count);
        }

        /// <summary>A portal on top of the Town Hall would spawn its raiders inside the settlement, past every wall the player has built.</summary>
        [Test]
        [TestCase(0f)]
        [TestCase(-10f)]
        public void BuildTemplate_KeepsThePortalsOffTheTownHall(float baseDistance)
        {
            var template = PortalPlacement.BuildTemplate(5, baseDistance, Step, 0f);

            Assert.AreEqual(PortalPlacement.MinDistanceMeters, template[0].DistanceMeters, 0.001f);
        }

        /// <summary>A negative step read literally would march the portals in towards the settlement, and the fifth one would land inside it.</summary>
        [Test]
        public void BuildTemplate_NeverWalksThePortalsInwards()
        {
            var template = PortalPlacement.BuildTemplate(5, BaseDistance, -6f, 0f);

            foreach (var slot in template)
            {
                Assert.GreaterOrEqual(slot.DistanceMeters, BaseDistance,
                    "A portal ended up nearer the Town Hall than the first one.");
            }
        }

        /// <summary>
        /// The fallback bearings a displaced portal walks through. Alternating to either side keeps
        /// it near the bearing the template gave it; sweeping one way round the circle would let a
        /// portal pushed off water slide into the next portal's arc and undo the spread above.
        /// </summary>
        [Test]
        public void BearingsToTry_StartAtTheSlotAndFanOutBothWays()
        {
            var step = Mathf.PI * 2f / 8f;
            var bearings = PortalPlacement.BearingsToTry(0f, 8, 2f * step).ToList();
            var expected = new[] { 0f, step, -step, 2f * step, -2f * step };

            Assert.AreEqual(expected.Length, bearings.Count, "The fan did not offer as many bearings as it was asked for.");
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], bearings[i], 0.001f,
                    $"Bearing {i} is not the slot's own first, then alternating to either side of it.");
            }
        }

        [Test]
        public void BearingsToTry_AlwaysOffersTheSlotsOwnBearingFirst()
        {
            Assert.AreEqual(1.25f, PortalPlacement.BearingsToTry(1.25f, 16, Mathf.PI).First(), 0.001f);
        }

        /// <summary>A sample count of zero would otherwise divide the circle by nothing; the slot's own bearing is still worth trying.</summary>
        [Test]
        public void BearingsToTry_SurvivesAZeroSampleCount()
        {
            CollectionAssert.AreEqual(new[] { 2f }, PortalPlacement.BearingsToTry(2f, 0, Mathf.PI).ToList());
        }

        /// <summary>
        /// The cap is what stops a portal whose own direction is all water from walking round the
        /// settlement and parking beside its neighbour. Without it the real map opened two portals
        /// four degrees apart.
        /// </summary>
        [Test]
        public void BearingsToTry_NeverLeavesTheCap()
        {
            var cap = PortalPlacement.MaxBearingDeviationRadians(5);

            foreach (var bearing in PortalPlacement.BearingsToTry(0f, 16, cap))
            {
                Assert.LessOrEqual(Mathf.Abs(bearing), cap + 0.001f,
                    $"A fallback bearing {Mathf.Abs(bearing) * Mathf.Rad2Deg:0.#}° off the slot got past a cap of {cap * Mathf.Rad2Deg:0.#}°.");
            }
        }

        /// <summary>A cap of zero leaves exactly one bearing to try -- the slot's own. Anything else would be the cap failing open.</summary>
        [Test]
        public void BearingsToTry_WithNoRoomToMoveOffersOnlyTheSlotsOwnBearing()
        {
            CollectionAssert.AreEqual(new[] { 0.5f }, PortalPlacement.BearingsToTry(0.5f, 16, 0f).ToList());
        }

        /// <summary>
        /// The guarantee the whole spread rests on: two NEIGHBOURING portals, each displaced as far
        /// towards the other as the cap allows, still cannot meet. This is why the cap is derived
        /// from the portal count instead of being a number someone picked -- change the count and
        /// this has to keep holding.
        /// </summary>
        [Test]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(9)]
        public void TwoNeighbouringPortalsCannotBeDisplacedIntoEachOther(int count)
        {
            var share = Mathf.PI * 2f / count;
            var cap = PortalPlacement.MaxBearingDeviationRadians(count);

            Assert.Greater(share - 2f * cap, 0f,
                $"With {count} portals the cap lets two neighbours close the whole {share * Mathf.Rad2Deg:0.#}° between them.");
        }
    }
}
