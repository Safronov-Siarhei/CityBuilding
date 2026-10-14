using System.Collections;
using System.Linq;
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
    /// The orc faction's Blender models in the running game (Tools/blender/orc*.py, wired by
    /// SetupProject.WireOrcModels): every portal wears its two looks with exactly one of them --
    /// the next wave's source -- burning bright, a closed portal leaves its ruin and hands the
    /// light on, and a raider is drawn as the grunt or, from BruteFromLevel, the brute, at the size
    /// the model was made.
    /// </summary>
    public class OrcModelTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map1";

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
        public IEnumerator EveryPortalWearsItsModelAndOnlyTheNextSourceBurns()
        {
            yield return OpenPortals();

            foreach (var portal in OrcPortal.All)
            {
                Assert.IsNotNull(portal.transform.Find("PortalActive"), $"The portal at {portal.Cell} has no active-look model -- it is still the cube arch.");
                Assert.IsNotNull(portal.transform.Find("PortalPassive"), $"The portal at {portal.Cell} has no passive-look model.");
            }
            AssertExactlyTheNextSourceBurns();
        }

        [UnityTest]
        public IEnumerator AClosedPortalLeavesItsRuinAndAnotherTakesTheLight()
        {
            yield return OpenPortals();
            Assume.That(OrcPortal.All.Count, Is.GreaterThanOrEqualTo(2), "Needs two portals to see the light move on.");

            var closed = OrcRaidManager.Instance.NextRaidSource;
            Assert.IsNotNull(closed, "No portal was picked as the next wave's source.");
            var cell = closed.Cell;
            closed.TakeDamage(closed.MaxHealth);
            yield return null;

            Assert.IsNotNull(GameObject.Find($"OrcPortalRuin {cell.x},{cell.y}"), "The closed portal left no ruin behind.");
            CollectionAssert.Contains(OrcRaidManager.Instance.ClosedPortalCells.ToList(), cell, "The ruin's cell is not recorded, so a save would lose it.");
            Assert.IsNotNull(OrcRaidManager.Instance.NextRaidSource, "With portals still standing, none was lit after the source was closed.");
            AssertExactlyTheNextSourceBurns();
        }

        [UnityTest]
        public IEnumerator ARaiderIsTheGruntUntilItsLevelMakesItTheBrute()
        {
            yield return OpenPortals();
            var origin = OrcPortal.All[0].transform.position;

            var grunt = OrcRaidManager.Instance.SpawnOrcs(origin, 1, 1);
            var brute = OrcRaidManager.Instance.SpawnOrcs(origin, 1, OrcRaidManager.BruteFromLevel);
            yield return null;

            Assert.IsNotNull(grunt.transform.Find("Grunt"), "A level-1 orc is not drawn as the grunt model.");
            Assert.IsNotNull(brute.transform.Find("Brute"), $"A level-{OrcRaidManager.BruteFromLevel} orc is not drawn as the brute model.");
            // At the heights the models were made: a grunt 0.58 m to its topknot, a brute 0.86 to
            // the top of the maul on its shoulder at the level it first appears -- the unit scale
            // must not shrink them, nor the level growth double up.
            Assert.AreEqual(0.58f, HeightOf(grunt.transform, "Grunt"), 0.06f, "The grunt is drawn at the wrong size.");
            Assert.AreEqual(0.86f, HeightOf(brute.transform, "Brute"), 0.08f, "The brute is drawn at the wrong size.");
        }

        /// <summary>Photographs a lit portal, a smouldering one and a squad of both kinds (needs a run with graphics).</summary>
        [UnityTest]
        public IEnumerator PhotographThePortalsAndASquad()
        {
            if (!PlaytestCapture.Available) Assert.Ignore("No graphics device: run without -nographics to photograph.");
            yield return OpenPortals();

            var lit = OrcRaidManager.Instance.NextRaidSource;
            var front = lit.transform.position - lit.transform.forward * 2.2f;
            for (var i = 0; i < 4; i++)
            {
                var level = i < 2 ? 1 : OrcRaidManager.BruteFromLevel;
                OrcRaidManager.Instance.SpawnOrcs(front + lit.transform.right * (i - 1.5f) * 0.6f, 1, level);
            }
            yield return new WaitForSeconds(0.2f);
            yield return PlaytestCapture.Shoot("orc-portal-active", lit.transform.position, distance: 7f, pitch: 40f, yaw: 20f);

            var smouldering = OrcPortal.All.FirstOrDefault(p => p != lit);
            if (smouldering != null)
            {
                yield return PlaytestCapture.Shoot("orc-portal-passive", smouldering.transform.position, distance: 6f, pitch: 40f, yaw: 20f);
                var cell = smouldering.Cell;
                smouldering.TakeDamage(smouldering.MaxHealth);
                yield return null;
                yield return PlaytestCapture.Shoot("orc-portal-closed", PlaytestWorld.CellCenter(cell) + new Vector3(0.5f, 0f, 0.5f), distance: 6f, pitch: 40f, yaw: 20f);
            }
        }

        private static void AssertExactlyTheNextSourceBurns()
        {
            var burning = OrcPortal.All.Where(p => p.transform.Find("PortalActive").gameObject.activeSelf).ToList();
            Assert.AreEqual(1, burning.Count, "Exactly one portal -- the next wave's -- should show the bright look.");
            Assert.AreSame(OrcRaidManager.Instance.NextRaidSource, burning[0], "The bright portal is not the one the next wave comes out of.");
            foreach (var portal in OrcPortal.All)
            {
                Assert.AreNotEqual(portal.transform.Find("PortalActive").gameObject.activeSelf,
                    portal.transform.Find("PortalPassive").gameObject.activeSelf, $"The portal at {portal.Cell} shows both looks or neither.");
            }
        }

        private static float HeightOf(Transform unit, string figure)
        {
            var renderers = unit.Find(figure).GetComponentsInChildren<Renderer>();
            Assert.IsNotEmpty(renderers, "The orc has nothing to draw.");
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            return bounds.max.y - unit.position.y;
        }

        private static IEnumerator OpenPortals()
        {
            var townHall = PlaytestWorld.Building("Castle");
            var cell = PlaytestWorld.FindFreeAreaNearTheMiddle(townHall.footprintSize);
            PlaytestWorld.Place(townHall, cell);
            for (var frame = 0; frame < 120 && !OrcRaidManager.Instance.PortalsPlaced; frame++)
            {
                yield return null;
            }
            Assert.IsTrue(OrcRaidManager.Instance.PortalsPlaced, "The portals never opened.");
            CollectionAssert.IsNotEmpty(OrcPortal.All, "Not one portal opened.");
        }
    }
}
