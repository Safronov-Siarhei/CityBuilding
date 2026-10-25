using System.Collections;
using System.Linq;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
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
    /// The prisoners' cages (the user's idea, 2026-10-19): a broken cage lets its townsfolk out,
    /// they walk to the Town Hall and become citizens -- or wait there while the town has no free
    /// home; an orc with no soldier to fight kills them on the way; and a cage a save puts back
    /// broken lets nobody out a second time.
    /// </summary>
    public class PrisonerCageTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map3";

        private BuildingInstance _hall;

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
        public IEnumerator ABrokenCageLetsItsPrisonersOutAndTheyBecomeCitizens()
        {
            yield return OpenPortals();
            var camp = OrcCamp.All.First(c => !c.IsMain && c.Portal != null);
            var cage = camp.Buildings.First(b => b.Kind == OrcCampBuildingKind.Cage);
            var expected = OrcCampBuilding.PrisonersIn(cage.Level, BalanceConfig.Instance);
            var citizens = CitizenManager.Instance;
            // settlers arriving on their own would muddle the count
            MigrationManager.Instance.enabled = false;
            // nor may the houses make people, or force the town's room back to their places (HousePeople)
            HousePeople.Instance.enabled = false;
            citizens.ChangeCapacity(expected + 20);
            var before = citizens.TotalPopulation;

            cage.TakeDamage(cage.MaxHealth);
            yield return null;
            Assert.AreEqual(expected, FreedPrisoner.All.Count, "The broken cage did not let its prisoners out.");
            Assert.IsTrue(camp.Fallen.Any(f => f.Kind == OrcCampBuildingKind.Cage), "The cage left no ruin.");

            // the road from the camp is a long one; the walk itself is what the last stretch tests
            var near = NearTheHall(7f);
            foreach (var prisoner in FreedPrisoner.All.ToList()) prisoner.transform.position = near;
            for (var t = 0f; t < 15f && FreedPrisoner.All.Count > 0; t += Time.deltaTime) yield return null;

            Assert.AreEqual(0, FreedPrisoner.All.Count, "The freed prisoners never reached the Town Hall.");
            Assert.GreaterOrEqual(citizens.TotalPopulation, before + expected, "The prisoners reached the hall but did not become citizens.");
        }

        [UnityTest]
        public IEnumerator WithNoFreeHomeThePrisonerWaitsAtTheHall()
        {
            yield return OpenPortals();
            var citizens = CitizenManager.Instance;
            // a settler arriving on his own would take the home this test frees for the prisoner
            MigrationManager.Instance.enabled = false;
            // nor may the houses make people, or force the town's room back to their places (HousePeople)
            HousePeople.Instance.enabled = false;
            citizens.ChangeCapacity(-citizens.FreeSpace);
            var before = citizens.TotalPopulation;

            var prisoner = FreedPrisoner.Spawn(NearTheHall(1f));
            yield return new WaitForSeconds(2f);
            Assert.IsTrue(prisoner != null && prisoner.Waiting, "With no free home the prisoner did not wait at the hall.");
            Assert.AreEqual(before, citizens.TotalPopulation, "A prisoner moved in with no free home (the user's rule: he waits).");

            citizens.ChangeCapacity(1);
            for (var t = 0f; t < 3f && prisoner != null; t += Time.deltaTime) yield return null;
            Assert.IsTrue(prisoner == null, "A home came free and the waiting prisoner did not take it.");
            Assert.AreEqual(before + 1, citizens.TotalPopulation);
        }

        [UnityTest]
        public IEnumerator AnOrcWithNoSoldierToFightKillsAPrisoner()
        {
            yield return OpenPortals();
            // on the open ground before the hall, where nothing can wedge the orc
            var orc = OrcRaidManager.Instance.SpawnOrcs(NearTheHall(14f), 1, 3);
            Assert.IsNotNull(orc);
            var prisoner = FreedPrisoner.Spawn(orc.transform.position + Vector3.right * 1.5f);

            for (var t = 0f; t < 10f && prisoner != null; t += Time.deltaTime) yield return null;
            Assert.IsTrue(prisoner == null, "A raider with nobody else to fight let a freed prisoner walk past.");
        }

        [UnityTest]
        public IEnumerator ACageASaveBreaksLetsNobodyOut()
        {
            yield return OpenPortals();
            var camp = OrcCamp.All.First(c => !c.IsMain && c.Portal != null);
            var cage = camp.Buildings.First(b => b.Kind == OrcCampBuildingKind.Cage);

            camp.ApplySavedHealth(OrcCampBuildingKind.Cage, cage.Cell, 0);
            yield return null;

            Assert.IsTrue(camp.Fallen.Any(f => f.Kind == OrcCampBuildingKind.Cage), "The save did not put the cage back broken.");
            Assert.AreEqual(0, FreedPrisoner.All.Count, "A cage broken in an old save let its prisoners out again on load.");
        }

        private Vector3 NearTheHall(float metresOut)
        {
            var grid = CityBuilder.Grid.GridManager.Instance;
            var footprint = _hall.RotatedFootprint();
            var centre = grid.GetFootprintCenterWorld(_hall.OriginCell, footprint);
            return centre + Vector3.back * (footprint.y * grid.CellSize * 0.5f + metresOut);
        }

        private IEnumerator OpenPortals()
        {
            var townHall = PlaytestWorld.Building("Castle");
            var cell = PlaytestWorld.FindFreeAreaNearTheMiddle(townHall.footprintSize);
            _hall = PlaytestWorld.Place(townHall, cell);
            for (var frame = 0; frame < 120 && !OrcRaidManager.Instance.PortalsPlaced; frame++)
            {
                yield return null;
            }
            Assert.IsTrue(OrcRaidManager.Instance.PortalsPlaced, "The portals never opened.");
        }
    }
}
