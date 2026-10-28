using System.Collections;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Saving;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// A building under construction (the user, 2026-10-28): a site with scaffolding in its place,
    /// never reaching past its own cells; at the finish the site goes, the building is back at its
    /// own size, under dust or a gold flash. Photographed both ways for the user to pick.
    /// </summary>
    public class ConstructionLookTests
    {
        private BuildFinish _finish;

        [UnitySetUp]
        public IEnumerator OpenTheMap()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            _finish = GameSettings.BuildFinish;
            GameSessionIntent.NewGameMapId = "Map3";
            SceneManager.LoadScene("CityBuilder", LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady("Map3");
        }

        [UnityTearDown]
        public IEnumerator PutItBack()
        {
            GameSettings.BuildFinish = _finish;
            yield return null;
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
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                Assert.IsFalse(r.enabled, "The church shows through its own building site.");
            }
            Assert.IsTrue(barn.GetComponent<BuildingLevelAppearance>().ModelFor(barn.Level).GetComponentInChildren<Renderer>().enabled,
                "An upgrade hides the building it works on.");

            foreach (var building in new[] { church, cottage, barn })
            {
                var site = building.GetComponent<ConstructionLook>().Site;
                Assert.IsNotNull(site, $"{building.Data.buildingName}: no scaffolding.");
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
            GameSettings.BuildFinish = BuildFinish.Dust;
            church.Site.Finish();
            yield return new WaitForSecondsRealtime(0.12f);
            yield return PlaytestCapture.Shoot("construction-finish-dust", church.transform.position, 12f, 35f, 25f);

            GameSettings.BuildFinish = BuildFinish.Fanfare;
            cottage.Site.Finish();
            yield return new WaitForSecondsRealtime(0.1f);
            yield return PlaytestCapture.Shoot("construction-finish-fanfare", cottage.transform.position, 9f, 35f, 25f);

            barn.Site.Finish();
            yield return new WaitForSecondsRealtime(2f);
            yield return PlaytestCapture.Shoot("construction-done", focus, 14f, 40f, 25f);

            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                Assert.IsTrue(r.enabled, "The church is still hidden after its site finished.");
            }
            Assert.AreEqual(scale, model.transform.localScale, "The church is left squashed after the finish.");
            foreach (var building in new[] { church, cottage, barn })
            {
                Assert.IsNull(building.GetComponent<ConstructionLook>(), $"{building.Data.buildingName}'s site outlived its finish.");
                Assert.IsNull(building.transform.Find("ConstructionSite"), $"{building.Data.buildingName}'s scaffolding outlived its finish.");
            }
            Assert.IsNull(Object.FindAnyObjectByType<BuildFinishEffect>(), "A finish effect never went.");
        }
    }
}
