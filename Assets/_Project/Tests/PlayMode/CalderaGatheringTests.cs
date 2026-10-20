using System.Collections;
using System.Linq;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using CityBuilder.Resources;
using CityBuilder.Saving;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// The user's start on the Caldera (2026-10-20): a Town Hall in the start zone, a sawmill by
    /// the forest, a quarry by the boulders, workers on both -- and no wood or stone ever came in.
    /// Two faults in the walk: a worker walking home went straight at the building's middle,
    /// which is inside its walls, and with no NavMesh route round to a partial end he was "stuck"
    /// and retried the same walk for ever; and a long step carried a walker over a 5 cm corner
    /// and back, round it for good. Run at eight times speed, which is where the second one bit.
    /// </summary>
    public class CalderaGatheringTests
    {
        [UnitySetUp]
        public IEnumerator StartOnTheCaldera()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            GameSessionIntent.SaveNameToLoad = null;
            GameSessionIntent.NewGameMapId = "Map5";
            SceneManager.LoadScene("CityBuilder", LoadSceneMode.Single);
            yield return null;
            yield return null;
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady("Map5");
            ModalGate.SetBlocked(false);
        }

        [TearDown]
        public void NormalSpeed() => Time.timeScale = 1f;

        [UnityTest]
        public IEnumerator SawmillAndQuarry_BringHomeWoodAndStone()
        {
            var grid = GridManager.Instance;
            var castle = PlaytestWorld.Building("Castle");
            var zone = StartZone.Centre(MeshMapApplier.Instance.CurrentMap.TownHallZone);
            var castleCell = grid.WorldToCell(zone) - castle.footprintSize / 2;
            PlaytestWorld.Place(castle, castleCell);
            for (var f = 0; f < 240 && !OrcRaidManager.Instance.PortalsPlaced; f++) yield return null;
            OrcRaidManager.Instance.RaidsSuspended = true;

            var sawmill = PlaceNear("Sawmill", ResourceType.Wood, zone).GetComponent<ProductionBuilding>();
            var quarry = PlaceNear("Quarry", ResourceType.Stone, zone).GetComponent<ProductionBuilding>();
            CitizenManager.Instance.SetPopulation(6);
            yield return null;
            for (var i = 0; i < 3; i++)
            {
                Assert.IsTrue(sawmill.TryAssignWorker(), "Could not put a worker on the sawmill.");
                Assert.IsTrue(quarry.TryAssignWorker(), "Could not put a worker on the quarry.");
            }

            var resources = ResourceManager.Instance;
            var wood = resources.GetAmount(ResourceType.Wood);
            var stone = resources.GetAmount(ResourceType.Stone);
            Time.timeScale = 8f;
            for (var t = 0f; t < 18f && (resources.GetAmount(ResourceType.Wood) <= wood || resources.GetAmount(ResourceType.Stone) <= stone); t += 0.5f)
            {
                yield return new WaitForSecondsRealtime(0.5f);
            }
            Time.timeScale = 1f;

            Assert.Greater(resources.GetAmount(ResourceType.Wood), wood, "Two game minutes of woodcutting brought no wood home.");
            Assert.Greater(resources.GetAmount(ResourceType.Stone), stone, "Two game minutes of quarrying brought no stone home.");
        }

        /// <summary>The building on the first free spot near the nearest node of its kind to `from`.</summary>
        private static BuildingInstance PlaceNear(string id, ResourceType kind, Vector3 from)
        {
            var data = PlaytestWorld.Building(id);
            var grid = GridManager.Instance;
            var nearest = ResourceNode.All.Where(n => n != null && n.ResourceType == kind)
                .OrderBy(n => (n.transform.position - from).sqrMagnitude).First();
            var target = grid.WorldToCell(nearest.transform.position);
            for (var r = 2; r < 30; r++)
            {
                for (var dx = -r; dx <= r; dx++)
                {
                    for (var dz = -r; dz <= r; dz++)
                    {
                        var cell = target + new Vector2Int(dx, dz);
                        if (!grid.IsWithinBounds(cell, data.footprintSize) || !grid.IsAreaFree(cell, data.footprintSize)) continue;
                        return PlaytestWorld.Place(data, cell);
                    }
                }
            }
            Assert.Fail($"No room for the {id} near the {kind}.");
            return null;
        }
    }
}
