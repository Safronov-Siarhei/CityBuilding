using System.Collections;
using System.Linq;
using System.Reflection;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Maps;
using CityBuilder.Resources;
using CityBuilder.Saving;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// The walled orc bases on the real map (the user's sketches, 2026-10-16): every portal opens
    /// a level-1 camp with its garrison; a camp grows and comes back whole; its wall keeps the way
    /// shut until a piece of it is broken; each spawner tops up only its own kind and stops when
    /// broken; the totem shields the portal; closing a portal plunders its camp.
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
        public IEnumerator EveryPortalOpensALevelOneCampWithAFullGarrison()
        {
            yield return OpenPortals();

            Assert.IsTrue(OrcCamp.All.Any(c => c.MaxLevel == OrcBaseLayout.MaxLevel),
                "Not one portal found room for a camp that can grow to the top level -- the map has plenty.");
            foreach (var portal in OrcPortal.All)
            {
                var camp = OrcCamp.Of(portal);
                Assert.IsNotNull(camp, $"The portal at {portal.Cell} has no camp.");
                Assert.AreEqual(1, camp.Level, "A fresh camp starts at level 1 (the user's call: they grow).");
                Assert.AreEqual(camp.Layout.Pieces.Count, camp.Buildings.Count, $"The camp at {portal.Cell} is missing buildings of its plan.");
                Assert.AreEqual(camp.Layout.Guards.Count, camp.Guards.Count, $"The camp at {portal.Cell} did not muster its garrison.");
                Assert.IsTrue(camp.Guards.All(g => g.IsGuard), "A camp's garrison orc is not on guard duty.");
                Assert.IsFalse(portal.IsShielded, "A level-1 camp has no totem, so its portal is open to attack.");
            }
        }

        [UnityTest]
        public IEnumerator ACampGrowsWithTheTownAndComesBackWhole()
        {
            yield return OpenPortals();
            var camp = TopCamp();
            var tower = camp.Buildings.First(b => b.Kind == OrcCampBuildingKind.Watchtower);
            tower.TakeDamage(tower.MaxHealth);
            yield return null;
            Assert.AreEqual(1, camp.Fallen.Count);

            OrcRaidManager.Instance.GrowCamps(BalanceConfig.Instance.OrcCampLevel3Score);
            yield return null;

            Assert.AreEqual(OrcBaseLayout.MaxLevel, camp.Level, "The camp did not grow with the score.");
            Assert.IsEmpty(camp.Fallen, "A grown camp is rebuilt whole, its broken tower too.");
            Assert.AreEqual(camp.Layout.Pieces.Count, camp.Buildings.Count, "The grown camp is missing buildings of its plan.");
            Assert.AreEqual(camp.Layout.Guards.Count, camp.Guards.Count, "The grown camp did not muster its new garrison.");
            Assert.AreEqual(BalanceConfig.Instance.PortalHealthAt(camp.Level), camp.Portal.MaxHealth, "The portal did not take its camp's level.");
            Assert.IsTrue(camp.Portal.IsShielded, "The grown camp's totem does not shield its portal.");
            Assert.AreEqual(camp.Layout.Guards.Count(g => g.Kind == OrcKind.Thrower), camp.Guards.Count(g => g.Kind == OrcKind.Thrower));
        }

        [UnityTest]
        public IEnumerator TheWallKeepsTheWayShutUntilItIsBroken()
        {
            yield return OpenPortals();
            var camp = TopCamp();
            camp.GrowTo(2, OrcRaidManager.Instance);
            for (var i = 0; i < 5; i++) yield return null;

            var outside = OnNavMesh(camp.LocalToWorld(new Vector2(0f, -(camp.Layout.HalfExtent + 3f))));
            var inside = OnNavMesh(camp.LocalToWorld(new Vector2(-3f, -3f)));
            var path = new NavMeshPath();
            NavMesh.CalculatePath(outside, inside, NavMesh.AllAreas, path);
            Assert.AreNotEqual(NavMeshPathStatus.PathComplete, path.status, "The palisade does not keep anyone out.");

            var gate = camp.Buildings.First(b => b.Kind == OrcCampBuildingKind.Gate);
            gate.TakeDamage(gate.MaxHealth);
            for (var i = 0; i < 10; i++) yield return null;

            NavMesh.CalculatePath(outside, inside, NavMesh.AllAreas, path);
            Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status, "Breaking the gate did not open the way in.");
        }

        [UnityTest]
        public IEnumerator EachSpawnerTopsUpOnlyItsOwnKindAndStopsWhenBroken()
        {
            yield return OpenPortals();
            var camp = TopCamp();
            // Quick before the camp is built: a spawner's clock, once wound, runs its whole length.
            var field = typeof(BalanceConfig).GetField("orcGarrisonRespawnSeconds", BindingFlags.Instance | BindingFlags.NonPublic);
            var saved = (float)field.GetValue(BalanceConfig.Instance);
            field.SetValue(BalanceConfig.Instance, 0.3f);
            try
            {
                camp.GrowTo(2, OrcRaidManager.Instance);
                yield return null;

                var raiders = camp.Layout.Guards.Count(g => g.Kind == OrcKind.Raider);
                var brutes = camp.Layout.Guards.Count(g => g.Kind == OrcKind.Brute);
                var raider = camp.Guards.First(g => g.Kind == OrcKind.Raider);
                var brute = camp.Guards.First(g => g.Kind == OrcKind.Brute);
                raider.TakeDamage(100000);
                brute.TakeDamage(100000);
                var pit = camp.Buildings.First(b => b.Kind == OrcCampBuildingKind.BrutePit);
                pit.TakeDamage(pit.MaxHealth);

                yield return new WaitForSeconds(1.5f);

                Assert.AreEqual(raiders, camp.Guards.Count(g => g.Kind == OrcKind.Raider), "The war tent did not replace its fallen grunt.");
                Assert.AreEqual(brutes - 1, camp.Guards.Count(g => g.Kind == OrcKind.Brute), "A broken brute pit still sent a brute.");
            }
            finally
            {
                field.SetValue(BalanceConfig.Instance, saved);
            }
        }

        [UnityTest]
        public IEnumerator TheTotemShieldsItsPortalUntilItFalls()
        {
            yield return OpenPortals();
            var camp = TopCamp();
            camp.GrowTo(2, OrcRaidManager.Instance);
            yield return null;
            var portal = camp.Portal;
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

        [UnityTest]
        public IEnumerator ClosingAPortalPlundersItsCampAndPutsTheNextRaidBack()
        {
            yield return OpenPortals();
            var camp = TopCamp();
            var portal = camp.Portal;
            var resources = ResourceManager.Instance;
            resources.SetAmount(ResourceType.Coins, 0);
            var timer = OrcRaidManager.Instance.SecondsUntilNextRaid;

            portal.TakeDamage(portal.MaxHealth);
            yield return null;

            Assert.IsTrue(camp.Cleared, "The camp of a closed portal is not marked plundered.");
            Assert.Greater(resources.GetAmount(ResourceType.Coins), 0, "Closing the portal paid nothing.");
            Assert.AreEqual(timer + BalanceConfig.Instance.OrcCampBreatherSeconds, OrcRaidManager.Instance.SecondsUntilNextRaid, 0.01f,
                "Closing the portal did not put the next raid back by the breather.");

            OrcRaidManager.Instance.GrowCamps(BalanceConfig.Instance.OrcCampLevel3Score);
            Assert.AreEqual(1, camp.Level, "A plundered camp grew.");
        }

        /// <summary>The garrison holds its camp: a few seconds on, with nobody to fight, every guard is still inside its camp's square.</summary>
        [UnityTest]
        public IEnumerator TheGarrisonHoldsItsCamp()
        {
            yield return OpenPortals();
            OrcRaidManager.Instance.GrowCamps(BalanceConfig.Instance.OrcCampLevel3Score);
            yield return new WaitForSeconds(4f);

            foreach (var camp in OrcCamp.All)
            {
                foreach (var guard in camp.Guards)
                {
                    Assert.IsTrue(camp.IsInZone(guard.transform.position, OrcCamp.ZoneMarginMeters),
                        $"A guard of the camp at {camp.PortalCell} has wandered out of it, to {guard.transform.position}.");
                }
            }
        }

        /// <summary>Photographs one camp at each level from the town's side (needs a run with graphics).</summary>
        [UnityTest]
        public IEnumerator PhotographTheCampsLevels()
        {
            if (!PlaytestCapture.Available) Assert.Ignore("No graphics device: run without -nographics to photograph.");
            yield return OpenPortals();
            var camp = TopCamp();
            for (var level = 1; level <= camp.MaxLevel; level++)
            {
                camp.GrowTo(level, OrcRaidManager.Instance);
                yield return new WaitForSeconds(0.5f);
                var yaw = camp.Quarters * 90f + 200f;
                var distance = 10f + camp.Layout.HalfExtent * 1.6f;
                yield return PlaytestCapture.Shoot($"orc-camp-level{level}", camp.LocalToWorld(Vector2.zero), distance: distance, pitch: 50f, yaw: yaw);
            }
        }

        /// <summary>A camp with room to grow to the top level.</summary>
        private static OrcCamp TopCamp()
        {
            var camp = OrcCamp.All.FirstOrDefault(c => c.MaxLevel == OrcBaseLayout.MaxLevel && c.Portal != null);
            Assert.IsNotNull(camp, "No camp has room for the top level.");
            return camp;
        }

        private static Vector3 OnNavMesh(Vector3 point)
        {
            Assert.IsTrue(NavMesh.SamplePosition(point, out var hit, 2f, NavMesh.AllAreas), $"No NavMesh near {point}.");
            return hit.position;
        }

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
