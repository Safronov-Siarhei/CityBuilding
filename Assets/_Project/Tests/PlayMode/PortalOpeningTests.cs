using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CityBuilder.Buildings;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Maps;
using CityBuilder.Saving;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// The portals as they actually open on the real map, which is the half PortalPlacementTests
    /// cannot see: the template says where they SHOULD go, and this says where the ground search
    /// put them.
    ///
    /// Everything here would pass a reading of the code and still be wrong in the running game.
    /// A slot whose spot is water is displaced, and a displacement that goes wrong is invisible --
    /// portals stacked on one cell, portals standing in the lake with every raider they spawn
    /// walking through the world off the NavMesh, portals all crowded onto one side of the
    /// settlement so half the walls the player builds face nothing.
    /// </summary>
    public class PortalOpeningTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map3";

        /// <summary>Loaded fresh per test: the portals open ONCE, on the first frame a Town Hall exists, so a scene shared with another test has already spent that frame.</summary>
        [UnitySetUp]
        public IEnumerator PrepareScene()
        {
            LogAssert.ignoreFailingMessages = true;

            Time.timeScale = 1f;
            GameSessionIntent.NewGameMapId = MapId;
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);

            ModalGate.SetBlocked(false);
            // The portals still open while raids are suspended -- that is the point of the flag --
            // and a wave arriving mid-test would only add orcs to walk over the assertions.
            if (OrcRaidManager.Instance != null) OrcRaidManager.Instance.RaidsSuspended = true;
        }

        [UnityTest]
        public IEnumerator TheMapOpensEveryPortalTheSheetAsksFor()
        {
            yield return PlaceTownHallAndWait();

            // the camps' portals; the main base's great portal comes on top. A designed map plans its
            // own camps (the user's rule, 2026-10-20); a map without spots opens the sheet's count.
            var spots = MeshMapApplier.Instance.CurrentMap.CampSpots.Length;
            var expected = spots > 0 ? spots : BalanceConfig.Instance.PortalCount;
            Assert.AreEqual(expected, OrcPortal.All.Count(p => !p.IsMain),
                "The map opened a different number of portals than it plans -- a slot with no standable ground is skipped, and this map is supposed to have room for all of them.");
        }

        /// <summary>
        /// Every portal on real ground at the playable height. A portal on the lake or up a
        /// decorative cliff strands every orc it spawns off the NavMesh, and they then walk to the
        /// settlement through the world in a straight line.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryPortalStandsOnGroundAnOrcCouldWalkOff()
        {
            yield return PlaceTownHallAndWait();

            var mapApplier = MeshMapApplier.Instance;
            Assert.IsNotNull(mapApplier, "No map applier, so this test cannot tell ground from water.");

            foreach (var portal in OrcPortal.All)
            {
                Assert.IsFalse(mapApplier.IsWaterCell(portal.Cell), $"The portal at {portal.Cell} opened on water.");
                Assert.IsTrue(mapApplier.IsGroundAt(portal.transform.position),
                    $"The portal at {portal.Cell} is not standing on the map's ground mesh.");
            }
        }

        /// <summary>Two portals on one cell would read as one arch and count as two objectives, so the map could not be finished by breaking what the player can see.</summary>
        [UnityTest]
        public IEnumerator NoTwoPortalsShareACell()
        {
            yield return PlaceTownHallAndWait();

            var cells = new HashSet<Vector2Int>();
            foreach (var portal in OrcPortal.All)
            {
                Assert.IsTrue(cells.Add(portal.Cell), $"Two portals opened on the same cell {portal.Cell}.");
            }
        }

        /// <summary>
        /// All of them out at a distance. The floor is the sheet's first slot less the fallback
        /// search's widest step inwards (8 m): a portal displaced by water may end up nearer than
        /// its slot, but a portal any nearer than that came from something other than the template
        /// -- and one on the settlement's doorstep spawns its raiders past every wall the player
        /// has built.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryPortalOpensOutAtADistanceFromTheTownHall()
        {
            var townHall = PlaceTownHall();
            yield return WaitForPortals();

            // The sheet's own first-slot distance, less two metres for the grid snapping each
            // portal to a cell centre. Nothing may end up nearer: that is what the search's inward
            // walk is floored at, and a portal inside that ring spawns its raiders past the walls.
            var floor = BalanceConfig.Instance.PortalDistanceFromTownHallMeters - 2f;
            var townHallPosition = townHall.transform.position;

            foreach (var portal in OrcPortal.All)
            {
                var distance = Vector3.Distance(
                    new Vector3(townHallPosition.x, 0f, townHallPosition.z),
                    new Vector3(portal.transform.position.x, 0f, portal.transform.position.z));

                Assert.GreaterOrEqual(distance, floor,
                    $"The portal at {portal.Cell} opened {distance:0.#} m from the Town Hall, nearer than any slot of the template puts one.");
            }
        }

        /// <summary>
        /// The spread, measured the only way that means anything: no two portals on the same
        /// bearing. Five portals that all came out on one side would leave the other side of the
        /// settlement permanently safe -- and that is exactly what a fallback search sweeping one
        /// way around the circle produces, which is why it fans out both ways instead.
        /// </summary>
        [UnityTest]
        public IEnumerator ThePortalsAreSpreadAroundTheSettlement()
        {
            var townHall = PlaceTownHall();
            yield return WaitForPortals();

            if (OrcPortal.All.Count(p => !p.IsMain) < 2) Assert.Ignore("One portal cannot be spread around anything.");

            var townHallPosition = townHall.transform.position;
            var bearings = new List<float>();
            var layout = new System.Text.StringBuilder();
            // the camps' portals, which the template spreads; the main base is placed on its own
            foreach (var portal in OrcPortal.All.Where(p => !p.IsMain))
            {
                var offset = portal.transform.position - townHallPosition;
                bearings.Add(Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg);
                // Printed on failure: which portal went where is the only thing that says whether
                // the search displaced one, or the map simply has no ground on that side.
                layout.Append($" [{bearings.Count - 1}: {bearings[bearings.Count - 1]:0.#}° at {new Vector2(offset.x, offset.z).magnitude:0.#} m]");
            }

            // Exactly what PortalPlacement's bearing cap guarantees: neighbours start a full share
            // of the circle apart and each may be displaced by at most a third of one, so a third
            // is what must survive. Less three degrees of slack for the grid, which snaps every
            // portal to a cell centre and so moves its bearing a little either way -- most at the
            // innermost ring, where a metre of snapping is worth the most degrees.
            var minSeparationDegrees = 360f / bearings.Count / 3f - 3f;

            for (var i = 0; i < bearings.Count; i++)
            {
                for (var j = i + 1; j < bearings.Count; j++)
                {
                    var separation = Mathf.Abs(Mathf.DeltaAngle(bearings[i], bearings[j]));
                    Assert.GreaterOrEqual(separation, minSeparationDegrees,
                        $"Portals {i} and {j} opened {separation:0.#}° apart, so they are on the same side of the settlement. " +
                        $"The whole layout, as bearing and distance from the Town Hall:{layout}");
                }
            }
        }

        /// <summary>
        /// Near the middle of the map, not in the corner FindFreeArea hands back. Every assertion
        /// in this file is about what happens AROUND the Town Hall, and a Town Hall in the corner
        /// has half its compass off the edge of the map -- no placement rule can spread five
        /// portals around that, so a corner would test the map's shape rather than the template.
        /// </summary>
        private static BuildingInstance PlaceTownHall()
        {
            var townHall = PlaytestWorld.Building("Castle");
            Assert.IsNotNull(townHall, "No Town Hall in the catalogue, so the portals have nothing to anchor to.");

            var cell = PlaytestWorld.FindFreeAreaNearTheMiddle(townHall.footprintSize);
            Assert.AreNotEqual(new Vector2Int(-1, -1), cell, "Nowhere near the middle of the map to stand a Town Hall.");
            return PlaytestWorld.Place(townHall, cell);
        }

        private static IEnumerator PlaceTownHallAndWait()
        {
            PlaceTownHall();
            yield return WaitForPortals();
        }

        /// <summary>OrcRaidManager opens them all in one Update, on the first frame it sees a Town Hall.</summary>
        private static IEnumerator WaitForPortals()
        {
            for (var frame = 0; frame < 120 && !OrcRaidManager.Instance.PortalsPlaced; frame++)
            {
                yield return null;
            }

            Assert.IsTrue(OrcRaidManager.Instance.PortalsPlaced, "The raid manager never got round to opening the portals.");
            CollectionAssert.IsNotEmpty(OrcPortal.All, "The Town Hall went down and not one portal opened.");
        }
    }
}
