using System.Collections;
using System.Linq;
using System.Reflection;
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
    /// The main base on the game's map (the user's sketch, 2026-10-19): the great portal opens
    /// with its own base round it, every building in the base's own style, the chief at his
    /// house; two totems shield the portal; it never sends a raid; the chief comes back while
    /// his house stands; and closing the great portal wins the map.
    /// </summary>
    public class MainBaseTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map3";

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
        public IEnumerator TheMainBaseStandsWholeWithItsChief()
        {
            yield return OpenPortals();
            var portal = MainPortal();
            var camp = OrcCamp.Of(portal);
            Assert.IsNotNull(camp, "The great portal has no base round it.");
            Assert.IsTrue(camp.IsMain);
            Assert.AreEqual(OrcBaseLayout.MainLevel, camp.Level);
            Assert.AreEqual(camp.Layout.Pieces.Count, camp.Buildings.Count, "The main base is missing buildings of its plan.");
            Assert.AreEqual(camp.Layout.Guards.Count, camp.Guards.Count, "The main base did not muster its garrison.");
            Assert.AreEqual(1, camp.Guards.Count(g => g.Kind == OrcKind.Boss), "The chief is not at his house.");
            Assert.AreEqual(BalanceConfig.Instance.MainPortalHealth, portal.MaxHealth);
            Assert.IsTrue(portal.IsShielded, "The great portal is not shielded while its totems stand.");
        }

        [UnityTest]
        public IEnumerator BothTotemsMustFallAndTheMainPortalSendsNoRaids()
        {
            yield return OpenPortals();
            var portal = MainPortal();
            var camp = OrcCamp.Of(portal);
            Assert.AreNotSame(portal, OrcRaidManager.Instance.NextRaidSource, "A raid was lined up to come out of the main base.");

            var totems = camp.Buildings.Where(b => b.Kind == OrcCampBuildingKind.Totem).ToList();
            Assert.AreEqual(2, totems.Count);
            totems[0].TakeDamage(totems[0].MaxHealth);
            yield return null;
            Assert.IsTrue(portal.IsShielded, "One totem down, and the great portal lost its shield -- the user asked for two.");
            totems[1].TakeDamage(totems[1].MaxHealth);
            yield return null;
            Assert.IsFalse(portal.IsShielded, "Both totems down, and the great portal is still shielded.");
        }

        [UnityTest]
        public IEnumerator TheChiefComesBackWhileHisHouseStands()
        {
            yield return OpenPortals();
            var camp = OrcCamp.Of(MainPortal());
            var field = typeof(BalanceConfig).GetField("orcChiefRespawnSeconds", BindingFlags.Instance | BindingFlags.NonPublic);
            var saved = (float)field.GetValue(BalanceConfig.Instance);
            field.SetValue(BalanceConfig.Instance, 0.5f);
            try
            {
                camp.RebuildAt(OrcBaseLayout.MainLevel, OrcRaidManager.Instance);
                yield return null;
                camp.Guards.First(g => g.Kind == OrcKind.Boss).TakeDamage(1000000);
                yield return new WaitForSeconds(1.5f);
                Assert.AreEqual(1, camp.Guards.Count(g => g.Kind == OrcKind.Boss), "The chief did not come back with his house standing.");

                var house = camp.Buildings.First(b => b.Kind == OrcCampBuildingKind.ChiefHouse);
                house.TakeDamage(house.MaxHealth);
                camp.Guards.First(g => g.Kind == OrcKind.Boss).TakeDamage(1000000);
                yield return new WaitForSeconds(1.5f);
                Assert.AreEqual(0, camp.Guards.Count(g => g.Kind == OrcKind.Boss), "The chief came back with his house in ruins.");
            }
            finally
            {
                field.SetValue(BalanceConfig.Instance, saved);
            }
        }

        [UnityTest]
        public IEnumerator ClosingTheGreatPortalWinsTheMap()
        {
            yield return OpenPortals();
            var portal = MainPortal();
            portal.IsShielded = false;
            portal.TakeDamage(portal.MaxHealth);
            yield return null;
            Time.timeScale = 1f;
            Assert.IsTrue(GameOverManager.Instance.IsVictory, "Closing the great portal did not win the map.");
            Assert.IsTrue(GameOverManager.Instance.WonByMainPortal);
            Assert.Greater(OrcPortal.All.Count, 0, "This test meant to win with camps' portals still open.");
        }

        /// <summary>Photographs the main base from the town's side (needs a run with graphics).</summary>
        [UnityTest]
        public IEnumerator PhotographTheMainBase()
        {
            if (!PlaytestCapture.Available) Assert.Ignore("No graphics device: run without -nographics to photograph.");
            yield return OpenPortals();
            var camp = OrcCamp.Of(MainPortal());
            yield return new WaitForSeconds(0.5f);
            var yaw = camp.Quarters * 90f + 200f;
            yield return PlaytestCapture.Shoot("orc-main-base", camp.LocalToWorld(new Vector2(0f, -8f)), distance: 46f, pitch: 55f, yaw: yaw, fieldOfView: 50f);
            yield return PlaytestCapture.Shoot("orc-main-base-back", camp.LocalToWorld(new Vector2(0f, -2f)), distance: 20f, pitch: 45f, yaw: yaw);
        }

        private static OrcPortal MainPortal()
        {
            var portal = OrcPortal.All.FirstOrDefault(p => p.IsMain);
            Assert.IsNotNull(portal, "No main base opened on the game's map.");
            return portal;
        }

        private static IEnumerator OpenPortals()
        {
            var townHall = PlaytestWorld.Building("Castle");
            var cell = PlaytestWorld.FindFreeAreaNearTheMiddle(townHall.footprintSize);
            PlaytestWorld.Place(townHall, cell);
            for (var frame = 0; frame < 240 && !OrcRaidManager.Instance.PortalsPlaced; frame++)
            {
                yield return null;
            }
            Assert.IsTrue(OrcRaidManager.Instance.PortalsPlaced, "The portals never opened.");
        }
    }
}
