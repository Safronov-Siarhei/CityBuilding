using System.Collections;
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
    /// How the buildings look in the game, on the map new games start on: a placed building draws
    /// through the detail shader (the soft painted tiles, plaster and planks -- see
    /// Shaders/BuildingDetail.shader), and a village of them is photographed, close and from the
    /// game's own distance, because a look is only verified by being seen.
    /// </summary>
    public class BuildingLookTests
    {
        private static readonly string[] Village =
            { "Hovel", "Cottage", "Barn", "Sawmill", "Church", "Well", "Windmill", "Farm", "Tavern", "Manor", "Granary", "Smelter" };

        private static readonly Vector2Int[] Spots =
        {
            new Vector2Int(0, 0), new Vector2Int(3, 0), new Vector2Int(6, 0), new Vector2Int(9, 0), new Vector2Int(12, 0),
            new Vector2Int(0, 3), new Vector2Int(14, 4), new Vector2Int(0, 7), new Vector2Int(14, 8), new Vector2Int(2, 10),
            new Vector2Int(10, 11), new Vector2Int(5, 11),
        };

        [UnityTest]
        public IEnumerator AVillage_DrawsThroughTheDetailShader_AndIsPhotographed()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            GameSessionIntent.NewGameMapId = "Map3";
            SceneManager.LoadScene("CityBuilder", LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady("Map3");

            var origin = PlaytestWorld.FindFreeAreaNearTheMiddle(new Vector2Int(16, 12));
            var hall = PlaytestWorld.Place(PlaytestWorld.Building(BuildingIds.TownHall), origin + new Vector2Int(6, 6));
            Object.FindAnyObjectByType<BuildingPlacer>().MarkMandatoryBuildingAlreadyPlaced();

            BuildingInstance cottage = null;
            for (var i = 0; i < Village.Length; i++)
            {
                var data = PlaytestWorld.Building(Village[i]);
                if (data == null) continue;
                var cell = origin + Spots[i];
                if (!GridManager.Instance.IsAreaFree(cell, data.footprintSize)) continue;
                var placed = PlaytestWorld.Place(data, cell);
                if (Village[i] == "Cottage") cottage = placed;
            }
            Assert.IsNotNull(cottage, "No room for the Cottage in the test village.");

            // The placer's red "can't build here" ghost of the Town Hall lies over the real one in a
            // scene nobody plays; it is not part of the look.
            foreach (var r in Object.FindObjectsByType<Renderer>())
            {
                var m = r.sharedMaterial;
                if (m != null && m.name.Contains("(Instance)") && m.color.a < 0.99f) r.enabled = false;
            }
            for (var i = 0; i < 5; i++) yield return null;

            var renderer = cottage.GetComponentInChildren<MeshRenderer>();
            Assert.IsNotNull(renderer, "The Cottage draws nothing.");
            Assert.AreEqual("CityBuilder/BuildingDetail", renderer.sharedMaterial.shader.name,
                "A placed building draws without the detail shader -- flat colour, the old look.");

            var focus = hall.transform.position + new Vector3(0f, 0f, -1f);
            yield return PlaytestCapture.Shoot("buildings-soft-near", focus, 15f, 45f, 20f);
            yield return PlaytestCapture.Shoot("buildings-soft-close", cottage.transform.position, 5f, 45f, 15f);
            yield return PlaytestCapture.Shoot("buildings-soft-game", focus, 30f, 55f, 0f, 60f);
        }
    }
}
