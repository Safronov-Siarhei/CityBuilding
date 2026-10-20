using System.Collections;
using System.Linq;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Saving;
using CityBuilder.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// The citizens' tab of the workforce panel (the user, 2026-10-20: the player should be able to
    /// see what every citizen is doing): a row per citizen, the workers saying where they work,
    /// the rest saying they are free.
    /// </summary>
    public class CitizensTabTests
    {
        [UnitySetUp]
        public IEnumerator ATownWithASawmill()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            GameSessionIntent.SaveNameToLoad = null;
            GameSessionIntent.NewGameMapId = "Map3";
            SceneManager.LoadScene("CityBuilder", LoadSceneMode.Single);
            yield return null;
            yield return null;
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady("Map3");
            ModalGate.SetBlocked(false);
        }

        [TearDown]
        public void Close()
        {
            var panel = Object.FindFirstObjectByType<WorkforcePanelController>(FindObjectsInactive.Include);
            if (panel != null) panel.ClosePanel();
            ModalGate.SetBlocked(false);
        }

        [UnityTest]
        public IEnumerator EveryCitizenHasALine_WorkersSayWhere()
        {
            var castle = PlaytestWorld.Building("Castle");
            var hall = PlaytestWorld.Place(castle, PlaytestWorld.FindFreeAreaNearTheMiddle(castle.footprintSize));
            var sawmillData = PlaytestWorld.Building("Sawmill");
            var sawmill = PlaytestWorld.Place(sawmillData, PlaytestWorld.FindFreeAreaNearTheMiddle(sawmillData.footprintSize)).GetComponent<ProductionBuilding>();
            CitizenManager.Instance.SetPopulation(4);
            for (var f = 0; f < 60 && CitizenVisualsManager.Instance.AllAgents.Count < 4; f++) yield return null;
            Assert.IsTrue(sawmill.TryAssignWorker() && sawmill.TryAssignWorker(), "Could not put two on the sawmill.");
            yield return new WaitForSeconds(0.5f);

            var panel = Object.FindFirstObjectByType<WorkforcePanelController>(FindObjectsInactive.Include);
            panel.OpenPanel();
            panel.ShowCitizens();
            yield return null;

            Assert.AreEqual(4, panel.CitizenRowCount, "The tab does not list every citizen.");
            var empty = panel.transform.Find("Card/CitizensView/ScrollView/Viewport/EmptyLabel");
            Assert.IsNotNull(empty);
            Assert.IsFalse(empty.gameObject.activeSelf, "\"No citizens yet\" shows over a list of four.");
            var lines = Enumerable.Range(0, 4).Select(panel.CitizenStatus).ToList();
            foreach (var line in lines) StringAssert.DoesNotContain("#", line, $"A raw key in a citizen's line: {line}");
            // every one is somebody: a name of his own, not a number (the user, 2026-10-20)
            var titles = Enumerable.Range(0, 4).Select(panel.CitizenTitle).ToList();
            Assert.AreEqual(4, titles.Distinct().Count(), $"Two citizens share a name: {string.Join(" | ", titles)}");
            for (var i = 0; i < 4; i++)
                Assert.AreEqual(Localization.Format("#citizen_name", CitizenVisualsManager.Instance.AllAgents[i].CitizenName), titles[i], "A row is not titled with its citizen's name.");
            Assert.AreEqual(2, lines.Count(l => l.StartsWith(sawmill.DisplayName)), $"The two on the sawmill do not say so: {string.Join(" | ", lines)}");
            Assert.AreEqual(2, lines.Count(l => l == Localization.Get("#citizen_idle")), $"The two without a job do not say they are free: {string.Join(" | ", lines)}");

            yield return PlaytestCapture.ShootWithUiAt("citizens-tab", 2400, 1080, hall.transform.position, 30f, 50f, 20f);
        }
    }
}
