using System.Collections;
using System.Linq;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Invasion;
using CityBuilder.Maps;
using CityBuilder.Resources;
using CityBuilder.Saving;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// The Invasion's New Year map (the user, 2026-10-25): always winter in a fir forest with its tree
    /// and falling snow; gifts the island picks up and a Thief runs off with; the blizzard; midnight's
    /// bigger wave, salute and Ice Shaman. Every number is the invasion tab's inv_ny_* keys.
    /// </summary>
    public class NewYearTests
    {
        private const string GameSceneName = "CityBuilder";

        [UnitySetUp]
        public IEnumerator StartANewYearRun()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            InvasionRecords.FilePathOverride = System.IO.Path.Combine(Application.temporaryCachePath, "invasion_records_test.json");
            GameSessionIntent.NewGameMapId = InvasionMode.NewYearMapId;
            GameSessionIntent.NewGameIsInvasion = true;
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(InvasionMode.NewYearMapId);
            for (var frame = 0; frame < 300 && (InvasionMode.Instance == null || !InvasionMode.Instance.RunStarted); frame++) yield return null;
            Assert.IsTrue(InvasionMode.IsActive && InvasionMode.Instance.RunStarted, "The run never set itself up on the New Year map.");
        }

        [UnityTearDown]
        public IEnumerator Reset()
        {
            Time.timeScale = 1f;
            GameSessionIntent.NewGameIsInvasion = false;
            InvasionRecords.FilePathOverride = null;
            Weather.Clear();
            yield return null;
        }

        private static InvasionNewYear Rules => InvasionMode.Instance.NewYear;

        private static void Quiet()
        {
            InvasionMode.Instance.King.gameObject.SetActive(false);
            foreach (var man in CitizenVisualsManager.Instance.AllAgents.ToList()) if (man != null) man.gameObject.SetActive(false);
        }

        [UnityTest]
        public IEnumerator TheNewYearMap_IsWinterInAFirForest_WithItsTreeAndSnow()
        {
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(InvasionMode.IsNewYear);
            Assert.IsNotNull(Rules, "The New Year map has no New Year rules.");
            Assert.AreEqual(MapSeason.Winter, SeasonStyle.Current, "The New Year map is not winter.");
            Assert.AreEqual(1f, Shader.GetGlobalFloat("_SeasonSnow"), "No snow on the roofs.");
            Assert.IsNotNull(Rules.Tree, "No decorated tree before the castle.");
            Assert.IsNotNull(Camera.main.GetComponent<SnowFall>(), "No snow falling.");
            var trees = Object.FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude).Where(n => n.ResourceType == ResourceType.Wood).ToList();
            Assert.Greater(trees.Count, 0);
            Assert.IsTrue(trees.All(t => new[] { "Tree2", "Tree5", "Tree6", "Tree9" }.Any(c => t.name.StartsWith(c))), "A broadleaf tree in the fir forest.");

            if (PlaytestCapture.Available)
            {
                var castle = InvasionMode.Instance.Castle.transform.position;
                yield return PlaytestCapture.Shoot("newyear-base", castle + new Vector3(0f, 0f, -4f), 24f, 45f, 15f, 45f);
                FogOfWarManager.Instance?.RevealPermanent(Grid.GridManager.Instance.WorldToCell(castle), 60);
                yield return new WaitForSeconds(FogOfWarManager.RevealSeconds + 0.3f);
                yield return PlaytestCapture.Shoot("newyear-wide", castle, 70f, 55f, 15f, 45f);
                Rules.StartBlizzard(5f);
                yield return new WaitForSeconds(2.5f);
                yield return PlaytestCapture.Shoot("newyear-blizzard", castle + new Vector3(0f, 0f, -4f), 24f, 45f, 15f, 45f);
            }
        }

        /// <summary>A gift is taken by whoever of the island walks up to it: its contents go into the stock.</summary>
        [UnityTest]
        public IEnumerator AGift_IsTakenByTheKing()
        {
            var king = InvasionMode.Instance.King;
            var wood = ResourceManager.Instance.GetAmount(ResourceType.Wood);
            var gift = Rules.LayGift(king.transform.position + new Vector3(0.4f, 0f, 0f), ResourceType.Wood);
            for (var t = 0f; t < 2f && gift != null; t += Time.deltaTime) yield return null;
            Assert.IsTrue(gift == null, "The King stood by a gift and did not take it.");
            Assert.AreEqual(wood + BalanceConfig.Instance.NyGiftWood, ResourceManager.Instance.GetAmount(ResourceType.Wood), "The gift's wood did not come into the stock.");
        }

        /// <summary>A Thief runs to a gift and off with it; killed on the way, he drops it where he fell.</summary>
        [UnityTest]
        public IEnumerator AThief_RunsOffWithAGift_AndDropsItWhenKilled()
        {
            Quiet();
            foreach (var tower in Object.FindObjectsByType<DefensiveBuilding>(FindObjectsSortMode.None)) tower.enabled = false;
            var start = InvasionMode.Instance.Waves.LaneStarts[0];
            var castle = InvasionMode.Instance.Castle.transform.position;
            Assert.IsTrue(NavMesh.SamplePosition(Vector3.Lerp(start, castle, 0.4f), out var spot, 4f, NavMesh.AllAreas));
            var gift = Rules.LayGift(spot.position, ResourceType.Stone);
            Rules.SendThieves(1);
            Assert.AreEqual(1, Rules.Thieves.Count, "No Thief came for the gift.");
            var thief = Rules.Thieves[0];
            for (var t = 0f; t < 40f && gift.Carrier == null; t += Time.deltaTime) yield return null;
            Assert.IsNotNull(gift.Carrier, "The Thief never got to the gift.");
            var alerts = Object.FindAnyObjectByType<UI.MinimapAttackAlerts>();
            Assert.IsTrue(alerts == null || alerts.ThiefMarkShown, "The Thief has no mark on the minimap.");
            yield return new WaitForSeconds(1f);
            var carriedAt = gift.transform.position;
            Assert.Greater(Vector3.Distance(carriedAt, spot.position), 0.5f, "The Thief stands on the gift instead of running off with it.");

            thief.TakeDamage(thief.MaxHealth * 10);
            yield return null;
            yield return null;
            Assert.IsTrue(gift != null, "The gift went with the Thief.");
            Assert.IsNull(gift.Carrier, "The fallen Thief still holds the gift.");
        }

        /// <summary>Midnight's wave is bigger; the salute takes a share of every orc's health but the Ice Shaman's.</summary>
        [UnityTest]
        public IEnumerator Midnight_IsABiggerWave_AndTheSaluteSparesOnlyTheShaman()
        {
            Quiet();
            var config = BalanceConfig.Instance;
            var waves = InvasionMode.Instance.Waves;
            Assert.IsTrue(InvasionNewYear.IsMidnight(config.NyChimesEvery, config.NyChimesEvery));
            Assert.IsFalse(InvasionNewYear.IsMidnight(config.NyChimesEvery - 1, config.NyChimesEvery));
            Assert.AreEqual(config.NyChimesOrcsMult, waves.WaveSizeMultiplier(config.NyChimesEvery), 0.001f, "Midnight's wave is no bigger.");
            Assert.AreEqual(1f, waves.WaveSizeMultiplier(1), 0.001f);

            var at = waves.LaneStarts[0];
            var orc = OrcRaidManager.Instance.SpawnOrcOfKind(at, 1, OrcKind.Raider);
            var shaman = OrcRaidManager.Instance.SpawnOrcOfKind(at, 1, OrcKind.IceShaman);
            orc.Toughen(10f);
            yield return null;
            var orcFull = orc.CurrentHealth;
            var shamanFull = shaman.CurrentHealth;
            Rules.Salute();
            Assert.AreEqual(orcFull - Mathf.CeilToInt(orc.MaxHealth * config.NySaluteDamageShare), orc.CurrentHealth, "The salute did not take its share of an orc's health.");
            Assert.AreEqual(shamanFull, shaman.CurrentHealth, "The salute hurt the Ice Shaman.");
            if (PlaytestCapture.Available)
            {
                yield return new WaitForSeconds(0.6f);
                yield return PlaytestCapture.Shoot("newyear-salute", InvasionMode.Instance.Castle.transform.position, 28f, 30f, 15f, 50f);
            }
        }

        /// <summary>The Ice Shaman freezes the nearest tower: it does not shoot till it thaws.</summary>
        [UnityTest]
        public IEnumerator TheIceShaman_FreezesATowerNearHim()
        {
            Quiet();
            var config = BalanceConfig.Instance;
            var tower = Object.FindObjectsByType<DefensiveBuilding>(FindObjectsSortMode.None)
                .First(t => t.GetComponent<BuildingInstance>().Data.buildingName == "Tower");
            Assert.IsTrue(NavMesh.SamplePosition(tower.transform.position + new Vector3(3f, 0f, 3f), out var spot, 3f, NavMesh.AllAreas));
            var shaman = OrcRaidManager.Instance.SpawnOrcOfKind(spot.position, 1, OrcKind.IceShaman);
            shaman.Toughen(1000f);
            shaman.Steered = true;   // stand still for the test
            shaman.gameObject.AddComponent<IceShamanPower>();
            for (var t = 0f; t < config.NyShamanFreezeEverySeconds + 1f && tower.FrozenFor <= 0f; t += Time.deltaTime) yield return null;
            Assert.Greater(tower.FrozenFor, 0f, "The Ice Shaman did not freeze the tower beside him.");
            if (PlaytestCapture.Available) yield return PlaytestCapture.Shoot("newyear-shaman", tower.transform.position, 7f, 40f, 15f, 45f);
            Object.Destroy(shaman.gameObject);
            for (var t = 0f; t < config.NyShamanFreezeSeconds + 1f && tower.FrozenFor > 0f; t += Time.deltaTime) yield return null;
            Assert.LessOrEqual(tower.FrozenFor, 0f, "The tower never thawed.");
        }

        /// <summary>A blizzard shortens the towers' reach and slows everyone while it lasts, and ends.</summary>
        [UnityTest]
        public IEnumerator TheBlizzard_ShortensTheTowers_AndSlowsEveryone_WhileItLasts()
        {
            var config = BalanceConfig.Instance;
            var tower = Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None).First(b => b.Data != null && b.Data.buildingName == "Tower");
            var clear = DefensiveBuilding.RangeOf(tower);
            Rules.StartBlizzard(1.5f);
            Assert.AreEqual(clear * config.NyBlizzardRangeMult, DefensiveBuilding.RangeOf(tower), 0.01f, "A tower shoots as far in a blizzard.");
            Assert.AreEqual(config.NyBlizzardSpeedMult, Weather.SpeedMultiplier, 0.001f, "Nobody slows in a blizzard.");
            yield return new WaitForSeconds(2f);
            Assert.AreEqual(clear, DefensiveBuilding.RangeOf(tower), 0.01f, "The towers stay short after the blizzard.");
            Assert.AreEqual(1f, Weather.SpeedMultiplier, 0.001f);
        }
    }
}
