using System.Collections;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Grid;
using CityBuilder.Saving;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// A building under construction (the user, 2026-10-28): wrapped in a green net inside
    /// scaffolding, never reaching past its own cells; at the finish the site goes under the dust and
    /// the building is left at its own size. Photographed, the dust in three moments.
    /// </summary>
    public class ConstructionLookTests
    {
        [UnitySetUp]
        public IEnumerator OpenTheMap()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            GameSessionIntent.NewGameMapId = "Map3";
            SceneManager.LoadScene("CityBuilder", LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady("Map3");
        }

        [UnityTest]
        public IEnumerator ASite_StandsInTheBuildingsPlace_WithinItsCells_AndTheFinishBringsTheBuildingBack()
        {
            var origin = PlaytestWorld.FindFreeAreaNearTheMiddle(new Vector2Int(14, 8));
            PlaytestWorld.Place(PlaytestWorld.Building(BuildingIds.TownHall), origin + new Vector2Int(10, 3));
            Object.FindAnyObjectByType<BuildingPlacer>().MarkMandatoryBuildingAlreadyPlaced();
            var church = PlaytestWorld.Place(PlaytestWorld.Building("Church"), origin);
            var cottage = PlaytestWorld.Place(PlaytestWorld.Building("Cottage"), origin + new Vector2Int(5, 0));
            var barn = PlaytestWorld.Place(PlaytestWorld.Building("Barn"), origin + new Vector2Int(5, 4));
            Assert.IsNotNull(church);
            Assert.IsNotNull(cottage);
            Assert.IsNotNull(barn);

            church.BuildOnTimer(600f);
            cottage.BuildOnTimer(600f);
            ConstructionSite.Begin(barn, 600f, upgrade: true);
            yield return null;

            var model = church.GetComponent<BuildingLevelAppearance>().ModelFor(church.Level);

            foreach (var building in new[] { church, cottage, barn })
            {
                var site = building.GetComponent<ConstructionLook>().Site;
                Assert.IsNotNull(site, $"{building.Data.buildingName}: no scaffolding.");
                var upgrade = building == barn;
                Assert.AreEqual(!upgrade, site.transform.Find("Net") != null,
                    upgrade ? "An upgrade wraps the building in the net." : $"{building.Data.buildingName}: not wrapped in the net.");
                Assert.AreEqual(upgrade, site.GetComponentInChildren<ConstructionHoist>() != null,
                    upgrade ? "An upgrade's scaffolding has no hoist." : "A new building got the upgrade's hoist.");
                var cell = GridManager.Instance.CellSize;
                var half = new Vector2(building.RotatedFootprint().x, building.RotatedFootprint().y) * (cell * 0.5f);
                var centre = building.transform.position;
                foreach (var r in site.GetComponentsInChildren<Renderer>())
                {
                    var b = r.bounds;
                    Assert.LessOrEqual(Mathf.Abs(b.min.x - centre.x), half.x + 0.01f, $"{building.Data.buildingName}'s scaffolding reaches past its cells.");
                    Assert.LessOrEqual(Mathf.Abs(b.max.x - centre.x), half.x + 0.01f, $"{building.Data.buildingName}'s scaffolding reaches past its cells.");
                    Assert.LessOrEqual(Mathf.Abs(b.min.z - centre.z), half.y + 0.01f, $"{building.Data.buildingName}'s scaffolding reaches past its cells.");
                    Assert.LessOrEqual(Mathf.Abs(b.max.z - centre.z), half.y + 0.01f, $"{building.Data.buildingName}'s scaffolding reaches past its cells.");
                }
            }
            var focus = cottage.transform.position;
            yield return PlaytestCapture.Shoot("construction-sites", focus, 14f, 40f, 25f);

            var scale = model.transform.localScale;
            yield return PlaytestCapture.Shoot("construction-net-close", church.transform.position, 9f, 30f, 25f);
            church.Site.Finish();
            yield return new WaitForSecondsRealtime(0.1f);
            yield return PlaytestCapture.Shoot("construction-dust-1", church.transform.position, 12f, 35f, 25f);
            yield return new WaitForSecondsRealtime(0.35f);
            yield return PlaytestCapture.Shoot("construction-dust-2", church.transform.position, 12f, 35f, 25f);
            yield return new WaitForSecondsRealtime(0.4f);
            yield return PlaytestCapture.Shoot("construction-dust-3", church.transform.position, 12f, 35f, 25f);

            cottage.Site.Finish();

            yield return new WaitForSecondsRealtime(2.2f);
            yield return PlaytestCapture.Shoot("construction-upgrade-hoist", barn.transform.position + Vector3.up, 6f, 20f, 200f);
            Assert.IsNull(Object.FindAnyObjectByType<BuildFinishEffect>(), "A finish effect never went.");
            barn.Site.Finish();
            yield return null;
            Assert.IsNull(Object.FindAnyObjectByType<BuildFinishEffect>(), "An upgrade's end raised the new building's dust.");
            yield return new WaitForSecondsRealtime(0.2f);
            yield return PlaytestCapture.Shoot("construction-done", focus, 14f, 40f, 25f);

            Assert.AreEqual(scale, model.transform.localScale, "The church is left squashed after the finish.");
            foreach (var building in new[] { church, cottage, barn })
            {
                Assert.IsNull(building.GetComponent<ConstructionLook>(), $"{building.Data.buildingName}'s site outlived its finish.");
                Assert.IsNull(building.transform.Find("ConstructionSite"), $"{building.Data.buildingName}'s scaffolding outlived its finish.");
            }
            Assert.IsNull(Object.FindAnyObjectByType<BuildFinishEffect>(), "A finish effect never went.");
        }

        /// <summary>Every building in the hotbar under construction, photographed a dozen at a time: wrapped, all but what is walked on.</summary>
        [UnityTest]
        public IEnumerator EveryBuilding_GoesUpUnderTheNet_ButRoadsAndBridges()
        {
            const int columns = 4;
            const int perShot = 12;
            const int spacing = 5;
            var catalogue = new List<BuildingData>(Object.FindAnyObjectByType<BuildingPlacer>().AvailableBuildings);
            var shot = 0;
            for (var first = 0; first < catalogue.Count; first += perShot)
            {
                var count = Mathf.Min(perShot, catalogue.Count - first);
                var plot = new Vector2Int(columns * spacing, Mathf.CeilToInt(count / (float)columns) * spacing);
                var origin = PlaytestWorld.FindFreeArea(plot);
                Assert.AreNotEqual(new Vector2Int(-1, -1), origin, "No room on the map for the next dozen.");
                var placed = new List<BuildingInstance>();
                for (var i = 0; i < count; i++)
                {
                    var data = catalogue[first + i];
                    if (data == null) continue;
                    var building = PlaytestWorld.Place(data, origin + new Vector2Int(i % columns * spacing, i / columns * spacing));
                    if (building == null) continue;
                    building.BuildOnTimer(600f);
                    placed.Add(building);
                }
                yield return null;
                foreach (var building in placed)
                {
                    var look = building.GetComponent<ConstructionLook>();
                    var flat = building.Data.isRoad || building.Data.providesWalkableSurface;
                    if (flat) Assert.IsNull(look, $"{building.Data.buildingName} is walked on and got a net and scaffolding.");
                    if (flat) continue;
                    Assert.IsTrue(look != null && look.Site.transform.Find("Net") != null, $"{building.Data.buildingName} goes up without its net.");
                    // nothing of the scaffolding over its poles' tops (0.35 over the net): the user saw rails in the air
                    var wood = look.Site.transform.Find("Wood").GetComponent<MeshFilter>().sharedMesh.bounds;
                    var net = look.Site.transform.Find("Net").GetComponent<MeshFilter>().sharedMesh.bounds;
                    Assert.LessOrEqual(wood.max.y, net.max.y + 0.36f, $"{building.Data.buildingName}'s scaffolding has a piece hanging over its poles.");
                }
                shot++;
                yield return PlaytestCapture.Shoot($"construction-catalogue-{shot}", PlaytestWorld.CellCenter(origin + plot / 2), 22f, 40f, 20f);
                foreach (var building in placed) Object.DestroyImmediate(building.gameObject);
                yield return null;
            }
        }
    }
}
