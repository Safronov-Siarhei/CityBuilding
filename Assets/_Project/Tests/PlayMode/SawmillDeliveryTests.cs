using System.Collections;
using System.Linq;
using System.Text;
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
    /// A sawmill in the forest brings its wood home (the user, 2026-10-20: "каменоломня работает,
    /// лесопилка не приносит ресурсы"). A tree falls in one visit, so the woodcutter's claim on it
    /// is gone the moment he shoulders the wood -- and the five-second "find the idle gatherers a
    /// node" sweep took that for idle and turned him round, again and again, so not one load ever
    /// reached the sawmill on any map. A quarry was spared only because a boulder outlasts a visit.
    /// </summary>
    public class SawmillDeliveryTests
    {
        [TearDown]
        public void NormalSpeed() => Time.timeScale = 1f;

        [UnityTest]
        public IEnumerator ASawmillInTheForest_BringsItsWoodHome([Values("Map3", "Map5")] string mapId)
        {
            LogAssert.ignoreFailingMessages = true;
            GameSessionIntent.SaveNameToLoad = null;
            GameSessionIntent.NewGameMapId = mapId;
            SceneManager.LoadScene("CityBuilder", LoadSceneMode.Single);
            yield return null;
            yield return null;
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(mapId);
            ModalGate.SetBlocked(false);

            var grid = GridManager.Instance;
            var castle = PlaytestWorld.Building("Castle");
            var zone = StartZone.Centre(MeshMapApplier.Instance.CurrentMap.TownHallZone);
            PlaytestWorld.Place(castle, grid.WorldToCell(zone) - castle.footprintSize / 2);
            for (var f = 0; f < 240 && !OrcRaidManager.Instance.PortalsPlaced; f++) yield return null;
            OrcRaidManager.Instance.RaidsSuspended = true;

            // where a player would put it: the free spot nearest the town with the most trees in reach
            var data = PlaytestWorld.Building("Sawmill");
            var radius = data.LevelStats(1).harvestRadius;
            var townCell = grid.WorldToCell(zone);
            Vector2Int best = default; var bestTrees = -1; var bestDist = float.MaxValue;
            for (var dx = -40; dx <= 40; dx += 2)
            for (var dz = -40; dz <= 40; dz += 2)
            {
                var cell = townCell + new Vector2Int(dx, dz);
                if (!grid.IsWithinBounds(cell, data.footprintSize) || !grid.IsAreaFree(cell, data.footprintSize)) continue;
                var at = grid.GetFootprintCenterWorld(cell, data.footprintSize);
                var trees = ResourceNode.All.Count(n => n != null && n.ResourceType == ResourceType.Wood && (n.transform.position - at).sqrMagnitude <= radius * radius);
                var dist = new Vector2(dx, dz).magnitude;
                if (trees >= 8 && dist < bestDist) { best = cell; bestTrees = trees; bestDist = dist; }
            }
            Assert.GreaterOrEqual(bestTrees, 8, "No spot near the town with eight trees in reach.");
            var sawmill = PlaytestWorld.Place(data, best).GetComponent<ProductionBuilding>();
            CitizenManager.Instance.SetPopulation(6);
            yield return null;
            for (var i = 0; i < 3; i++) sawmill.TryAssignWorker();

            var resources = ResourceManager.Instance;
            var log = new StringBuilder($"{mapId}: sawmill {best} ({bestDist:0} cells from town), trees in {radius} m: {bestTrees}, workers {sawmill.AssignedWorkers}\n");
            var start = resources.GetAmount(ResourceType.Wood);
            Time.timeScale = 8f;
            var visuals = CitizenVisualsManager.Instance;
            for (var s = 0; s < 24; s++)
            {
                yield return new WaitForSecondsRealtime(1f);
                var acts = string.Join(",", visuals.AllAgents.Where(a => visuals.WorkplaceOf(a) == sawmill).Select(a => a.CurrentActivity.ToString()));
                if (s % 5 == 4)
                {
                    log.Append($"  t={(s + 1) * 8,3}s wood {resources.GetAmount(ResourceType.Wood)}/{resources.GetCapacity(ResourceType.Wood)} (+{resources.GetAmount(ResourceType.Wood) - start})  [{acts}]\n");
                }
            }
            Time.timeScale = 1f;
            // three woodcutters, three game minutes: each walks at least one load home
            Assert.GreaterOrEqual(resources.GetAmount(ResourceType.Wood) - start, 15, log.ToString());
        }
    }
}
