using System.Collections;
using CityBuilder.Buildings;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Maps;
using CityBuilder.Maps.Archipelago;
using CityBuilder.Resources;
using CityBuilder.Saving;
using CityBuilder.Ships;
using CityBuilder.Tutorial;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// The tutorial (Design/features/tutorial.md, the user, 2026-11-05): its own map with the tutorial
    /// frame alone, no fog, no bot, its rules (four buildings, 5-s builds, infinite resources, the
    /// Латник and the transport only), and once the Town Hall stands the orcs' island: one portal,
    /// a tower, a guard -- with the script past its first step.
    /// </summary>
    public class TutorialTests
    {
        [UnitySetUp]
        public IEnumerator NewTutorial()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            GameSessionIntent.SaveNameToLoad = null;
            GameSessionIntent.NewGameMapId = ArchipelagoMapId.TutorialId;
            SceneManager.LoadScene("CityBuilder", LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(ArchipelagoMapId.TutorialId);
        }

        [UnityTearDown]
        public IEnumerator Reset()
        {
            TutorialMode.DecideFor(null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheTutorial_LoadsItsIslands_KeepsItsRules_AndLaysTheOrcIsland()
        {
            var applier = MeshMapApplier.Instance;
            Assert.IsTrue(TutorialMode.IsActive, "The tutorial's map did not switch the tutorial on.");
            Assert.AreEqual(ArchipelagoMapId.TutorialId, applier.CurrentMapId);
            Assert.IsFalse(applier.ArchipelagoBotCell.HasValue, "A bot in the tutorial.");
            Assert.IsFalse(applier.CurrentMap.HasMainBaseSpot, "An orc main base in the tutorial.");
            Assert.IsTrue(FogOfWarManager.Instance == null || FogOfWarManager.Instance.DisabledCheat, "Fog in the tutorial.");
            foreach (var spot in new[] { "town_hall_zone", "orc_landing", "orc_barge", "player_landing", "portal", "orc_tower", "orc_guard" })
            {
                Assert.IsTrue(applier.TryTutorialSpot(spot, out var at), $"The tutorial frame has no {spot} spot.");
                if (spot == "orc_barge") continue;
                Assert.IsFalse(applier.IsWaterCell(Grid.GridManager.Instance.WorldToCell(at)), $"The {spot} spot is in the sea.");
            }

            // the rules
            Assert.IsTrue(TutorialRules.Allows(PlaytestWorld.Building("Hovel")));
            Assert.IsFalse(TutorialRules.Allows(PlaytestWorld.Building("Tower")), "A building outside the tutorial's four is offered.");
            Assert.AreEqual(BalanceConfig.Instance.TutorialBuildSeconds, PlaytestWorld.Building(BuildingIds.Barracks).BuildSeconds(1));
            Assert.IsTrue(SoldierStats.IsUnlocked(SoldierType.ManAtArms));
            Assert.IsFalse(SoldierStats.IsUnlocked(SoldierType.Militia), "The Barracks offers more than the Латник.");
            Assert.AreEqual(1, SoldierStats.BarracksLevel(SoldierType.ManAtArms));
            Assert.AreEqual(1, ShipStats.ShipyardLevel(TutorialRules.Ship));
            yield return null;
            Assert.IsTrue(ResourceManager.Instance.InfiniteResources, "The tutorial's resources run out.");

            // the Town Hall in its zone: the orcs' island appears and the script moves on
            Assert.IsTrue(applier.TryTutorialSpot("town_hall_zone", out var zone));
            var hallData = PlaytestWorld.Building(BuildingIds.TownHall);
            var hall = PlaytestWorld.Place(hallData, Grid.GridManager.Instance.WorldToCell(zone) - hallData.footprintSize / 2);
            Assert.IsNotNull(hall, "No Town Hall in the tutorial's zone.");
            for (var t = 0f; t < 3f && OrcPortal.All.Count == 0; t += Time.deltaTime) yield return null;
            Assert.AreEqual(1, OrcPortal.All.Count, "The orcs' island has not exactly one portal.");
            var towers = 0;
            foreach (var b in OrcCampBuilding.All) if (b != null && b.Kind == OrcCampBuildingKind.Watchtower) towers++;
            Assert.AreEqual(1, towers, "The orcs' island has not exactly one tower.");
            Assert.AreEqual(1, OrcUnit.All.Count, "The orcs' island has not exactly one guard.");
            Assert.IsTrue(OrcUnit.All[0].IsGuard, "The orc on their island is not on guard.");
            Assert.IsNotNull(TutorialDirector.Instance, "No tutorial script.");
            for (var t = 0f; t < 2f && TutorialDirector.Instance.Current == TutorialDirector.Step.TownHall; t += Time.deltaTime) yield return null;
            Assert.AreNotEqual(TutorialDirector.Step.TownHall, TutorialDirector.Instance.Current, "The script did not move past the Town Hall.");

            // the orcs' barge waits by their island from the start, empty (the user, 2026-11-05)
            ShipUnit barge = null;
            for (var t = 0f; t < 2f && barge == null; t += Time.deltaTime)
            {
                foreach (var ship in ShipManager.Instance.Ships)
                    if (ship != null && ship.IsOrc) barge = ship;
                yield return null;
            }
            Assert.IsNotNull(barge, "No orc barge waiting by the orcs' island.");
            Assert.AreEqual(0, barge.OrcCargo, "The waiting barge already carries orcs.");
            var portalAt = OrcPortal.All[0].transform.position;
            Assert.Less(Vector3.Distance(new Vector3(barge.transform.position.x, 0f, barge.transform.position.z), new Vector3(portalAt.x, 0f, portalAt.z)), 60f,
                "The barge does not wait by the orcs' island.");
        }
    }
}
