using System.Collections;
using System.Linq;
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
    /// The orc camps round the portals, on the real map (the user's design, 2026-10-14): a
    /// watchtower, a war tent and a totem round every portal, the garrison standing guard rather
    /// than marching on the town, the totem shielding its portal until it falls, and a fallen camp
    /// building leaving its ruin.
    /// </summary>
    public class OrcCampTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map1";

        [UnitySetUp]
        public IEnumerator PrepareScene()
        {
            LogAssert.ignoreFailingMessages = true;

            Time.timeScale = 1f;
            GameSessionIntent.NewGameMapId = MapId;
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);

            ModalGate.SetBlocked(false);
            if (OrcRaidManager.Instance != null) OrcRaidManager.Instance.RaidsSuspended = true;
        }

        [UnityTest]
        public IEnumerator EveryPortalGetsACampOfAllThreeBuildingsAndAFullGarrison()
        {
            yield return OpenPortals();

            foreach (var portal in OrcPortal.All)
            {
                var camp = CampOf(portal);
                Assert.IsNotNull(camp, $"The portal at {portal.Cell} has no camp.");
                foreach (OrcCampBuildingKind kind in System.Enum.GetValues(typeof(OrcCampBuildingKind)))
                {
                    Assert.IsTrue(camp.Buildings.Any(b => b.Kind == kind), $"The camp at {portal.Cell} has no {kind} -- this map has room for all three.");
                }
                Assert.AreEqual(BalanceConfig.Instance.OrcGarrisonSize, camp.Guards.Count, $"The camp at {portal.Cell} did not muster its garrison.");
                Assert.IsTrue(camp.Guards.All(g => g.IsGuard), "A camp's garrison orc is not on guard duty.");
            }
        }

        [UnityTest]
        public IEnumerator TheTotemShieldsItsPortalUntilItFalls()
        {
            yield return OpenPortals();
            var portal = OrcPortal.All[0];
            var camp = CampOf(portal);
            var totem = camp.Buildings.First(b => b.Kind == OrcCampBuildingKind.Totem);

            Assert.IsTrue(portal.IsShielded, "The portal is not shielded while its totem stands.");
            portal.TakeDamage(50);
            Assert.AreEqual(portal.MaxHealth, portal.CurrentHealth, "A shielded portal took damage.");

            var totemCell = totem.Cell;
            totem.TakeDamage(totem.MaxHealth);
            yield return null;

            Assert.IsFalse(portal.IsShielded, "The totem fell and the portal stayed shielded.");
            portal.TakeDamage(50);
            Assert.AreEqual(portal.MaxHealth - 50, portal.CurrentHealth, "With its totem down the portal still took no damage.");
            Assert.IsNotNull(GameObject.Find($"OrcCampRuin {totemCell.x},{totemCell.y}"), "The totem left no ruin.");
            Assert.IsTrue(camp.Fallen.Any(f => f.Kind == OrcCampBuildingKind.Totem && f.Cell == totemCell), "The fallen totem is not recorded, so a save would lose its ruin.");
        }

        /// <summary>The garrison holds its camp: a few seconds on, with nobody to fight, every guard is still inside its guard radius -- not walking to the town as a raider would.</summary>
        [UnityTest]
        public IEnumerator TheGarrisonHoldsItsCampInsteadOfMarchingOnTheTown()
        {
            yield return OpenPortals();
            yield return new WaitForSeconds(4f);

            var radius = BalanceConfig.Instance.OrcGarrisonGuardRadiusMeters;
            foreach (var camp in OrcCamp.All)
            {
                foreach (var guard in camp.Guards)
                {
                    var offset = guard.transform.position - camp.GuardPost;
                    offset.y = 0f;
                    Assert.Less(offset.magnitude, radius, $"A guard of the camp at {camp.PortalCell} has wandered {offset.magnitude:0.0} m from it.");
                }
            }
        }

        /// <summary>Photographs one camp from the town's side (needs a run with graphics).</summary>
        [UnityTest]
        public IEnumerator PhotographACamp()
        {
            if (!PlaytestCapture.Available) Assert.Ignore("No graphics device: run without -nographics to photograph.");
            yield return OpenPortals();
            yield return new WaitForSeconds(0.5f);

            var portal = OrcPortal.All[0];
            var yaw = portal.transform.eulerAngles.y + 200f;
            yield return PlaytestCapture.Shoot("orc-camp", portal.transform.position, distance: 13f, pitch: 45f, yaw: yaw);
        }

        private static OrcCamp CampOf(OrcPortal portal) => OrcCamp.All.FirstOrDefault(c => c.Portal == portal);

        private static IEnumerator OpenPortals()
        {
            var townHall = PlaytestWorld.Building("Castle");
            var cell = PlaytestWorld.FindFreeAreaNearTheMiddle(townHall.footprintSize);
            PlaytestWorld.Place(townHall, cell);
            for (var frame = 0; frame < 120 && !OrcRaidManager.Instance.PortalsPlaced; frame++)
            {
                yield return null;
            }
            Assert.IsTrue(OrcRaidManager.Instance.PortalsPlaced, "The portals never opened.");
            CollectionAssert.IsNotEmpty(OrcPortal.All, "Not one portal opened.");
        }
    }
}
