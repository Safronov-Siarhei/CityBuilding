using System.Collections;
using System.Linq;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Saving;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// The townsfolk (the user, 2026-10-28, pick A): every citizen is one of the villager models in
    /// one piece -- no more two cubes -- and waddles as he walks. Photographed: the twelve in a row,
    /// and a town's own people.
    /// </summary>
    public class CitizenLookTests
    {
        [UnitySetUp]
        public IEnumerator NewGame()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            GameSessionIntent.SaveNameToLoad = null;
            GameSessionIntent.NewGameMapId = "Map3";
            SceneManager.LoadScene("CityBuilder", LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady("Map3");
        }

        [UnityTest]
        public IEnumerator EveryCitizen_IsAVillagerModel_InOnePiece()
        {
            var origin = PlaytestWorld.FindFreeAreaNearTheMiddle(new Vector2Int(12, 12));
            var hall = PlaytestWorld.Place(PlaytestWorld.Building(BuildingIds.TownHall), origin + new Vector2Int(4, 4));
            var placer = Object.FindAnyObjectByType<BuildingPlacer>();
            placer.MarkMandatoryBuildingAlreadyPlaced();
            placer.ClearSelection();
            CitizenManager.Instance.SetPopulation(8);
            var visuals = Object.FindAnyObjectByType<CitizenVisualsManager>();
            for (var t = 0f; t < 10f && visuals.AllAgents.Count < 3; t += Time.deltaTime) yield return null;
            Assert.Greater(visuals.AllAgents.Count, 0, "No citizens came.");
            foreach (var agent in visuals.AllAgents)
            {
                var figure = agent.transform.Find("Figure");
                Assert.IsNotNull(figure, $"{agent.name} is still the two cubes.");
                Assert.AreEqual(1, figure.GetComponentsInChildren<Renderer>().Length, $"{agent.name} is not in one piece.");
                Assert.IsTrue(figure.name.StartsWith("Figure") && agent.GetComponent<CitizenWaddle>() != null, $"{agent.name} does not waddle.");
                // the workforce panel draws him in his clothes: the model's main colour, measured at setup
                Assert.AreNotEqual(Color.grey, agent.Clothes, $"{agent.name} has no clothes colour for the workforce panel.");
            }

            // he faces the way he walks, and only rises and falls on it (the user: no sway)
            var walker = visuals.AllAgents[0];
            walker.enabled = false;
            var controller = walker.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            var waddle = walker.GetComponent<CitizenWaddle>();
            var walkerFigure = walker.transform.Find("Figure");
            foreach (var way in new[] { Vector3.right, Vector3.back })
            {
                for (var f = 0; f < 40; f++)
                {
                    walker.transform.position += way * 0.02f;
                    yield return null;
                    var tilt = Vector3.Angle(walkerFigure.up, Vector3.up);
                    Assert.Less(tilt, 1f, $"{walker.name} tips over {tilt:0.0}° as he walks.");
                }
                Assert.Less(Vector3.Angle(waddle.Facing, way), 10f, $"{walker.name} walks {way} facing {waddle.Facing}.");
                // the model's own front -- its local -z, Blender's -y -- is where he is going
                var front = walkerFigure.TransformDirection(Vector3.back);
                front.y = 0f;
                Assert.Less(Vector3.Angle(front, way), 10f, $"{walker.name}'s face looks {front} while he walks {way}.");
            }

            // the twelve in a row before the hall, for the user
            var models = UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>().Where(g => g.name.StartsWith("Villager") && g.transform.parent == null)
                .OrderBy(g => g.name).ToList();
            var start = PlaytestWorld.CellCenter(origin + new Vector2Int(0, 1));
            for (var i = 0; i < models.Count; i++)
            {
                var copy = Object.Instantiate(models[i]);
                copy.transform.position = start + new Vector3(i * 0.55f, 0f, 0f);
                copy.transform.rotation = Quaternion.identity;   // facing the camera (it looks along +z)
            }
            yield return null;
            yield return PlaytestCapture.Shoot("citizens-lineup", start + new Vector3((models.Count - 1) * 0.55f * 0.5f, 0.3f, 0f), 3.4f, 10f, 0f);
            yield return new WaitForSeconds(3f);
            yield return PlaytestCapture.Shoot("citizens-town", hall.transform.position, 9f, 40f, 20f);
        }
    }
}
