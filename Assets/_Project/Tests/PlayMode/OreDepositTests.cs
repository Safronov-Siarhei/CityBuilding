using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using CityBuilder.Saving;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// Ore veins (the user, 2026-10-21): glowing patches on the ground, a mine stands only on one of
    /// its own ore, and the same map always deals the same veins.
    /// </summary>
    public class OreDepositTests
    {
        private static IEnumerator Load(string mapId)
        {
            LogAssert.ignoreFailingMessages = true;
            GameSessionIntent.SaveNameToLoad = null;
            GameSessionIntent.NewGameMapId = mapId;
            SceneManager.LoadScene("CityBuilder", LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(mapId);
            ModalGate.SetBlocked(false);
        }

        [UnityTest]
        public IEnumerator EveryMapDealsItsVeins_TheSameEveryTime([Values("Map3", "Map4", "Map5")] string mapId)
        {
            yield return Load(mapId);
            var first = OreDeposit.All.Select(v => (v.Kind, v.Origin)).OrderBy(v => v.Origin.x).ThenBy(v => v.Origin.y).ToList();
            foreach (OreKind kind in System.Enum.GetValues(typeof(OreKind)))
            {
                Assert.IsTrue(first.Any(v => v.Kind == kind), $"{mapId} has no {kind} vein.");
            }

            yield return Load(mapId);
            var second = OreDeposit.All.Select(v => (v.Kind, v.Origin)).OrderBy(v => v.Origin.x).ThenBy(v => v.Origin.y).ToList();
            CollectionAssert.AreEqual(first, second, $"{mapId} dealt different veins on a second load -- a save would lose its mines' ground.");
        }

        [UnityTest]
        public IEnumerator AMineGoesOnlyOnAVeinOfItsOre()
        {
            yield return Load("Map3");
            var iron = OreDeposit.All.First(v => v.Kind == OreKind.Iron);
            var coal = OreDeposit.All.First(v => v.Kind == OreKind.Coal);
            var mine = PlaytestWorld.Building("IronMine");

            Assert.IsTrue(BuildingPlacer.IsFreeFor(mine, iron.Origin, mine.footprintSize), "An iron mine cannot go on an iron vein.");
            Assert.IsFalse(BuildingPlacer.IsFreeFor(mine, coal.Origin, mine.footprintSize), "An iron mine goes on a coal vein.");
            var open = PlaytestWorld.FindFreeAreaNearTheMiddle(new Vector2Int(2, 2));
            Assert.IsFalse(BuildingPlacer.IsFreeFor(mine, open, mine.footprintSize), "An iron mine goes on open ground.");

            var placed = PlaytestWorld.Place(mine, iron.Origin);
            Assert.AreEqual(placed, iron.Mine, "The vein does not know its mine.");
            Assert.IsFalse(iron.transform.Find("Visual").gameObject.activeSelf, "The vein still shows under its mine.");
            Assert.IsFalse(BuildingPlacer.IsFreeFor(mine, iron.Origin, mine.footprintSize), "A second mine fits on the same vein.");

            // a picture: the gold vein glowing, and the iron mine on its vein
            var gold = OreDeposit.All.First(v => v.Kind == OreKind.Gold);
            var placer = Object.FindAnyObjectByType<BuildingPlacer>();
            placer.MarkMandatoryBuildingAlreadyPlaced();
            placer.ClearSelection();
            foreach (var ghost in Object.FindObjectsByType<PlacementPreview>(FindObjectsSortMode.None)) ghost.gameObject.SetActive(false);
            FogOfWarManager.Instance?.RevealPermanent(GridManager.Instance.WorldToCell(gold.transform.position), 8);
            yield return PlaytestCapture.Shoot("ore-vein-gold", gold.transform.position, 6f, 40f, 25f);

            PlaytestWorld.Demolish(placed);
            yield return null;
            Assert.IsNull(iron.Mine, "The vein still thinks a mine stands on it.");
            Assert.IsTrue(iron.transform.Find("Visual").gameObject.activeSelf, "The vein did not come back after its mine went.");
            Assert.IsFalse(GridManager.Instance.IsAreaFree(iron.Origin, OreDeposit.Size), "Knocking the mine down freed the vein's ground.");
        }
    }
}
