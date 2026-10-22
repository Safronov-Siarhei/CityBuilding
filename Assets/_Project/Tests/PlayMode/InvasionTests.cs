using System.Collections;
using System.Linq;
using CityBuilder.Buildings;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Invasion;
using CityBuilder.Maps;
using CityBuilder.Resources;
using CityBuilder.Saving;
using CityBuilder.Citizens;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// The Invasion mode's first step: «В бой» starts a run on the mode's own map with the castle,
    /// a tower at each causeway and the King in place, and the King is a hero -- he levels on
    /// what he kills and comes back after he falls. The landmarks are invasion1.py's: Blender
    /// (x, y) is Unity (x, z).
    /// </summary>
    public class InvasionTests
    {
        private const string GameSceneName = "CityBuilder";
        private static readonly Vector3[] Causeways = { new Vector3(0f, 0f, 20f), new Vector3(-17.3f, 0f, -10f), new Vector3(17.3f, 0f, -10f) };

        /// <summary>Fresh per test: a run sets itself up once, on the frames after the scene loads.</summary>
        [UnitySetUp]
        public IEnumerator StartARun()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            // a run that ends writes its points: never into the player's own records
            InvasionRecords.FilePathOverride = System.IO.Path.Combine(Application.temporaryCachePath, "invasion_records_test.json");
            System.IO.File.Delete(InvasionRecords.FilePathOverride);
            GameSessionIntent.NewGameMapId = InvasionMode.MapId;
            GameSessionIntent.NewGameIsInvasion = true;
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(InvasionMode.MapId);
            for (var frame = 0; frame < 300 && (InvasionMode.Instance == null || !InvasionMode.Instance.RunStarted); frame++) yield return null;
            Assert.IsTrue(InvasionMode.IsActive && InvasionMode.Instance.RunStarted, "The run never set itself up.");
        }

        [UnityTearDown]
        public IEnumerator Reset()
        {
            Time.timeScale = 1f;
            GameSessionIntent.NewGameIsInvasion = false;
            System.IO.File.Delete(InvasionRecords.FilePathOverride);
            InvasionRecords.FilePathOverride = null;
            yield return null;
        }

        /// <summary>
        /// The castle falls and the run is over: its points are the waves survived squared plus the
        /// giant and the camp buildings razed (a wall piece is not one), the end panel says so, and
        /// the run is written into today's records.
        /// </summary>
        [UnityTest]
        public IEnumerator TheCastleFalling_EndsTheRun_AndWritesItsPoints()
        {
            var mode = InvasionMode.Instance;
            var config = BalanceConfig.Instance;

            mode.Waves.SendNextWave();
            foreach (var orc in mode.Waves.WaveOrcs.ToList()) orc.TakeDamage(orc.MaxHealth);
            for (var frame = 0; frame < 10 && mode.Waves.WavesSurvived == 0; frame++) yield return null;
            Assert.AreEqual(1, mode.Waves.WavesSurvived, "The wave was not beaten off.");

            var giant = mode.Jungle.BossLair.Monsters[0];
            giant.TakeDamage(giant.MaxHealth);
            var camp = mode.OrcCamps[0];
            var tower = camp.Buildings.First(b => b.Kind == OrcCampBuildingKind.Watchtower);
            var wall = OrcCampBuilding.All.First(b => b.Kind == OrcCampBuildingKind.Palisade);
            tower.TakeDamage(tower.MaxHealth);
            wall.TakeDamage(wall.MaxHealth);
            yield return null;

            var score = mode.Score;
            Assert.AreEqual(1, score.BossesSlain, "The giant did not count.");
            Assert.AreEqual(1, score.CampBuildingsRazed, "The camp tower did not count, or the wall piece did.");
            var expected = 1 * config.InvasionScorePerWaveSquared + config.InvasionScoreBoss + config.InvasionScoreCampBuilding;
            Assert.AreEqual(expected, score.Total);
            Assert.IsFalse(System.IO.File.Exists(InvasionRecords.FilePathOverride), "The run was written down before it ended.");

            mode.Castle.TryDamage(mode.Castle.MaxHealth);
            yield return null;
            Assert.IsTrue(GameOverManager.Instance.IsGameOver, "The castle fell and the run went on.");
            Assert.IsTrue(score.Finished);
            var days = InvasionRecords.LoadDays();
            Assert.AreEqual(expected, InvasionRecords.DayPoints(days, System.DateTime.Now, config.InvasionRunsCountedPerDay), "The run's points are not in today's records.");
            var reason = GameObject.Find("GameOverPanel/Card/Reason").GetComponent<UnityEngine.UI.Text>().text;
            StringAssert.Contains(expected.ToString(), reason, "The end panel does not show the run's points.");
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator TheRun_StartsWithTheCastleTheTowersAndTheKing_AndTheCausewaysCross()
        {
            var mode = InvasionMode.Instance;
            Assert.IsNotNull(mode.Castle, "No castle.");
            var castleAt = mode.Castle.transform.position;
            Assert.Less(new Vector2(castleAt.x, castleAt.z).magnitude, 3f, "The castle is not in the middle of the island.");
            Assert.AreEqual(3, Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None).Count(b => b.Data != null && b.Data.buildingName == "Tower"), "Not one tower per causeway.");
            Assert.AreEqual(0, Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None).Count(b => b.Data != null && b.Data.buildingName == "Bridge"), "Plank bridges were laid -- the crossings are earth now.");
            Assert.IsNotNull(mode.King, "No King.");
            Assert.IsNotNull(mode.King.transform.Find("King"), "The King is drawn in cubes -- his model (Models/Heroes/HeroKing) is not wired.");
            Assert.IsFalse(OrcRaidManager.Instance.enabled, "The sandbox's raids are still running in a run.");

            // every causeway carries the way off the island: from the tower to the lane beyond, on the ground
            yield return null;
            foreach (var bridge in Causeways)
            {
                var direction = bridge.normalized;
                var length = PathLength(bridge - direction * 9f, bridge + direction * 9f, out var complete);
                Assert.IsTrue(complete, $"No way across the causeway at {bridge}.");
                Assert.IsFalse(MeshMapApplier.Instance.IsWaterCell(GridManager.Instance.WorldToCell(bridge)), $"The causeway at {bridge} is water.");
                Assert.Less(length, 30f, $"The way across the causeway at {bridge} is {length:0} m -- it does not carry the lane.");
            }
        }

        [Test]
        public void TheKing_LevelsUp_AndGrowsStronger()
        {
            var king = InvasionMode.Instance.King;
            var health = king.Unit.MaxHealth;
            Assert.AreEqual(1, king.Level);

            king.AddExperience(KingHero.XpForLevel(1) - 1);
            Assert.AreEqual(1, king.Level, "A level came before its experience was full.");
            king.AddExperience(1 + KingHero.XpForLevel(2));
            Assert.AreEqual(3, king.Level, "Experience enough for two levels gave a different number.");
            Assert.AreEqual(health + 2 * BalanceConfig.Instance.KingHealthPerLevel, king.Unit.MaxHealth, "A level did not add the sheet's health.");
            Assert.AreEqual(KingHero.DamageAt(3), KingHero.DamageAt(1) + 2 * BalanceConfig.Instance.KingDamagePerLevel);
        }

        /// <summary>As in Dota: an orc that falls near the King is his experience, whoever struck it; one far off is not.</summary>
        [UnityTest]
        public IEnumerator TheKing_GainsExperience_FromOrcsFallingNearHim()
        {
            var king = InvasionMode.Instance.King;
            var near = king.transform.position + new Vector3(3f, 0f, 0f);
            Assert.IsTrue(NavMesh.SamplePosition(near, out var hit, 3f, NavMesh.AllAreas), "Nowhere by the King to stand an orc.");
            var orc = OrcRaidManager.Instance.SpawnOrcs(hit.position, count: 1, level: 1);
            var worth = orc.MaxHealth;

            for (var t = 0f; t < 30f && orc != null && orc.CurrentHealth > 0; t += Time.deltaTime) yield return null;
            Assert.IsTrue(orc == null || orc.CurrentHealth <= 0, "The orc beside the King never fell.");
            Assert.AreEqual(worth, king.Experience + (king.Level - 1) * KingHero.XpForLevel(1), "The orc that fell beside him was not worth its health in experience.");

            var before = king.Experience;
            var far = OrcRaidManager.Instance.SpawnOrcs(new Vector3(0f, 0f, 60f), count: 1, level: 1);
            far.TakeDamage(far.MaxHealth);
            yield return null;
            Assert.AreEqual(before, king.Experience, "An orc that fell across the map taught the King something.");
        }

        [UnityTest]
        public IEnumerator TheKing_Falls_AndComesBackAtTheCastle_KeepingHisLevel()
        {
            var king = InvasionMode.Instance.King;
            king.AddExperience(KingHero.XpForLevel(1));
            var level = king.Level;
            king.transform.position += new Vector3(4f, 0f, 4f);

            king.Unit.TakeDamage(king.Unit.MaxHealth);
            Assert.IsTrue(king.IsFallen, "Struck to zero, the King did not fall.");
            Assert.IsNotNull(king, "The King was destroyed instead of falling.");
            Assert.IsFalse(SoldierUnit.All.Contains(king.Unit), "A fallen King is still there for the orcs to fight.");
            Assert.AreEqual(KingHero.RespawnSecondsAt(level), king.RespawnRemaining, 0.01f);

            Time.timeScale = 4f;
            for (var t = 0f; t < 20f && king.IsFallen; t += Time.unscaledDeltaTime) yield return null;
            Time.timeScale = 1f;
            Assert.IsFalse(king.IsFallen, "The King never came back.");
            Assert.AreEqual(level, king.Level, "Coming back cost him his level.");
            Assert.AreEqual(king.Unit.MaxHealth, king.Unit.CurrentHealth, "He came back hurt.");
            Assert.Less(Vector3.Distance(Flat(king.transform.position), Flat(king.Home)), 1f, "He came back somewhere other than the castle.");
            Assert.IsTrue(SoldierUnit.All.Contains(king.Unit), "Back on his feet, he is still out of the fight.");
        }

        [Test]
        public void TheRun_OpensWithSixHousesEighteenPeopleAndItsOwnStock()
        {
            var mode = InvasionMode.Instance;
            var config = BalanceConfig.Instance;
            Assert.AreEqual(config.InvasionHouses, mode.Houses.Count, "Not every house found its place on the island.");
            Assert.AreEqual(config.InvasionHouses * config.InvasionPeoplePerHouse, mode.People.Places);
            Assert.AreEqual(mode.People.Places, InvasionPeople.People, "The island does not open with a full house.");
            Assert.AreEqual(mode.People.Places, CitizenManager.Instance.Capacity, "The town's room is not just the houses' places.");

            var resources = ResourceManager.Instance;
            Assert.AreEqual(config.InvasionStartWood, resources.GetAmount(ResourceType.Wood));
            Assert.AreEqual(config.InvasionStartStone, resources.GetAmount(ResourceType.Stone));
            Assert.AreEqual(config.InvasionStartCoins, resources.GetAmount(InvasionRules.Money));
            Assert.AreEqual(0, resources.GetAmount(ResourceType.Food), "A run has no food.");
            resources.Add(ResourceType.Wood, 100000);
            Assert.AreEqual(config.InvasionStartWood + 100000, resources.GetAmount(ResourceType.Wood), "A run's stock is capped by storehouses it cannot build.");
        }

        [Test]
        public void TheRun_BuildsItsNineBuildings_ForWoodAndStone()
        {
            var placer = Object.FindAnyObjectByType<BuildingPlacer>();
            Assert.IsFalse(InvasionRules.Allows(PlaytestWorld.Building(InvasionRules.House)), "Houses can be built in a run.");
            foreach (var id in InvasionRules.Buildings)
            {
                var data = PlaytestWorld.Building(id);
                Assert.IsNotNull(data, $"{id} is not among the game's buildings.");
                foreach (var cost in BuildingPlacer.CostOf(data))
                    Assert.IsTrue(cost.type == ResourceType.Wood || cost.type == ResourceType.Stone, $"{id} costs {cost.type} in a run.");
            }

            // an upgrade's iron is paid in gold
            var tower = Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None).First(b => b.Data != null && b.Data.buildingName == "Tower");
            var upgrade = tower.GetUpgradeCost();
            Assert.IsTrue(upgrade.All(c => c.type == ResourceType.Wood || c.type == ResourceType.Stone || c.type == InvasionRules.Money), "A run's upgrade asks for something besides wood, stone and gold.");
            Assert.IsTrue(upgrade.Any(c => c.type == InvasionRules.Money), "The tower's upgrade lost its iron instead of paying it in gold.");
            Assert.IsTrue(tower.NextLevelResearched, "A run's upgrade waits on a Laboratory it does not have.");
        }

        [Test]
        public void ADamagedBuilding_IsMendedForWoodAndStone()
        {
            var tower = Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None).First(b => b.Data != null && b.Data.buildingName == "Tower");
            Assert.IsNull(tower.GetRepairCost(), "An unhurt building offers a repair.");
            tower.TryDamage(tower.MaxHealth / 2);
            var cost = tower.GetRepairCost();
            Assert.IsNotNull(cost, "A damaged building cannot be repaired.");
            Assert.IsTrue(cost.All(c => c.type == ResourceType.Wood || c.type == ResourceType.Stone));
            Assert.IsTrue(tower.TryRepair(), "The repair was refused with the stock to pay for it.");
            Assert.AreEqual(tower.MaxHealth, tower.CurrentHealth, "The repair did not mend the damage.");
        }

        [UnityTest]
        public IEnumerator AFallenPerson_IsReplacedByTheHouses_AfterTheWait()
        {
            var people = InvasionMode.Instance.People;
            var full = InvasionPeople.People;
            CitizenManager.Instance.KillCitizens(2);
            yield return null;
            Assert.AreEqual(2, people.Owed, "The houses do not owe the island the two it lost.");
            Assert.IsFalse(GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver, "Losing people ended the run.");

            Time.timeScale = 4f;
            var wait = BalanceConfig.Instance.InvasionPersonRespawnSeconds;
            for (var t = 0f; t < wait * 1.6f && InvasionPeople.People < full; t += Time.deltaTime) yield return null;
            Time.timeScale = 1f;
            Assert.AreEqual(full, InvasionPeople.People, "The houses did not replace the people the island lost.");
        }

        [UnityTest]
        public IEnumerator ABurntHouse_TakesItsPlacesForGood()
        {
            var mode = InvasionMode.Instance;
            var places = mode.People.Places;
            var house = mode.Houses[0];
            house.TryDamage(house.MaxHealth);
            yield return null;
            yield return null;
            var perHouse = BalanceConfig.Instance.InvasionPeoplePerHouse;
            Assert.AreEqual(places - perHouse, mode.People.Places, "A burnt house kept its places.");
            Assert.AreEqual(places, InvasionPeople.People, "The house's people died with it -- they should only stop being replaced.");

            // more people than places: a death is not made good
            CitizenManager.Instance.KillCitizens(perHouse - 1);
            yield return null;
            Assert.AreEqual(0, mode.People.Owed, "The houses make people the island has no room for.");
        }

        /// <summary>With no order from the player the King defends the base: he goes for an orc that comes near the castle and walks home when it is gone.</summary>
        [UnityTest]
        public IEnumerator TheKing_WithNoOrder_DefendsTheBase()
        {
            var king = InvasionMode.Instance.King;
            Assert.IsTrue(king.Guarding, "The King does not start out guarding the base.");
            var castle = InvasionMode.Instance.Castle.transform.position;
            var spot = castle + new Vector3(9f, 0f, 9f);
            Assert.IsTrue(NavMesh.SamplePosition(spot, out var hit, 4f, NavMesh.AllAreas), "Nowhere on the island to stand an orc.");
            var orc = OrcRaidManager.Instance.SpawnOrcs(hit.position, count: 1, level: 1);

            for (var t = 0f; t < 3f && king.Unit.Group.AttackTarget == null; t += Time.deltaTime) yield return null;
            Assert.AreEqual((IDamageTarget)orc, king.Unit.Group.AttackTarget, "An orc came to the castle and the King did not go for it.");

            for (var t = 0f; t < 30f && orc != null && orc.CurrentHealth > 0; t += Time.deltaTime) yield return null;
            for (var t = 0f; t < 15f && Vector3.Distance(Flat(king.transform.position), Flat(king.Home)) > 1.2f; t += Time.deltaTime) yield return null;
            Assert.Less(Vector3.Distance(Flat(king.transform.position), Flat(king.Home)), 1.2f, "With the base clear, the King did not walk home.");
        }

        /// <summary>He takes the player's orders only while the player holds him: let go, he walks back to the castle and guards the base by himself.</summary>
        [Test]
        public void TheKing_LetGo_GoesBackToGuardTheBase()
        {
            var king = InvasionMode.Instance.King;
            var army = ArmyManager.Instance;
            army.SelectGroup(king.Unit.Group);
            king.Unit.Group.OrderMoveTo(king.Home + new Vector3(25f, 0f, 0f));
            king.Unit.Group.NotifyPlayerOrder();
            Assert.IsFalse(king.Guarding, "The King kept guarding after the player told him to go somewhere.");

            army.SelectGroup(null);
            Assert.IsTrue(king.Guarding, "Let go of, the King did not go back to guarding the base.");
            Assert.Less(Vector3.Distance(Flat(king.Unit.Group.HoldPosition), Flat(king.Home)), 0.5f, "Let go of, the King stays where he was sent.");
        }

        /// <summary>A wave's orcs take several of a tower's arrows (the user, 2026-10-22: they fell to one), and a hurt one shows its health over its head.</summary>
        [UnityTest]
        public IEnumerator WaveOrcs_AreTougher_AndShowTheirHealth()
        {
            var config = BalanceConfig.Instance;
            var waves = InvasionMode.Instance.Waves;
            waves.SendNextWave();
            var orc = waves.WaveOrcs[0];
            var sandbox = config.Unit("orc").LevelStats(1).maxHealth * orc.Level;
            Assert.AreEqual(Mathf.RoundToInt(sandbox * config.InvasionOrcHealthMultiplier), orc.MaxHealth, "A wave's orc is no tougher than a raid's.");
            var blow = config.Unit("orc").LevelStats(1).attackDamage * orc.Level;
            Assert.AreEqual(Mathf.Max(1, Mathf.RoundToInt(blow * config.InvasionOrcDamageMultiplier)), orc.AttackDamage, "A wave's orc does not hit as inv_orc_damage_mult says.");
            var arrow = PlaytestWorld.Building("Tower").LevelStats(1).defense;
            Assert.Greater(orc.MaxHealth, arrow * 2, "One or two of a tower's arrows still drop a wave's orc.");

            orc.TakeDamage(1);
            yield return null;
            var bar = orc.transform.Find("HealthBar");
            Assert.IsNotNull(bar, "A hurt orc shows no health over its head.");
            Assert.IsTrue(bar.gameObject.activeSelf);
            foreach (var o in waves.WaveOrcs.ToList()) o.TakeDamage(o.MaxHealth);
        }

        /// <summary>A run has one sawmill and one quarry at most (the user, 2026-10-22; two quarries were tried and taken back): past that the card greys and the placer refuses.</summary>
        [Test]
        public void ARun_HasOneSawmillAndOneQuarry()
        {
            foreach (var (id, max) in new[] { ("Sawmill", 1), ("Quarry", 1) })
            {
                var data = PlaytestWorld.Building(id);
                Assert.AreEqual(max, InvasionRules.LimitOf(data), $"A run's limit of {id} is not the sheet's.");
                for (var i = 0; i < max; i++)
                {
                    Assert.IsFalse(InvasionRules.AtLimit(data), $"A run's {id} no. {i + 1} is refused.");
                    PlaytestWorld.Place(data, PlaytestWorld.FindFreeAreaNearTheMiddle(data.footprintSize));
                }
                Assert.IsTrue(InvasionRules.AtLimit(data), $"A run can have more than {max} {id}.");
            }
            Assert.IsFalse(InvasionRules.AtLimit(PlaytestWorld.Building("Tower")), "Towers are limited too.");
        }

        [UnityTest]
        public IEnumerator ThePortrait_Shows_AndSelectsTheKing()
        {
            var portrait = Object.FindAnyObjectByType<CityBuilder.UI.KingPortraitController>();
            Assert.IsNotNull(portrait, "No King's portrait in the game.");
            yield return null;
            Assert.IsNotNull(GameObject.Find("KingPortrait"), "The King's portrait is not shown in a run.");
            var face = GameObject.Find("KingPortrait").transform.Find("Portrait/Mask/Face").GetComponent<UnityEngine.UI.Image>();
            Assert.IsNotNull(face.sprite, "The portrait has no face.");

            var king = InvasionMode.Instance.King;
            portrait.SelectKing();
            Assert.AreEqual(king.Unit.Group, ArmyManager.Instance.SelectedGroup, "Tapping the portrait did not select the King.");
            ArmyManager.Instance.SelectGroup(null);

            // a tap on the King where he stands picks him up too
            var camera = Camera.main;
            yield return null;
            var onScreen = camera.WorldToScreenPoint(king.transform.position + Vector3.up * 0.4f);
            Assert.IsTrue(CityBuilder.UI.KingPortraitController.TrySelectAt(onScreen, camera), "A tap on the King did not select him.");
            Assert.AreEqual(king.Unit.Group, ArmyManager.Instance.SelectedGroup);
            if (PlaytestCapture.Available)
            {
                yield return new WaitForSeconds(0.3f);
                yield return PlaytestCapture.ShootWithUiAt("invasion-portrait", 1600, 900, king.transform.position, 18f, 52f, 0f);
                king.Unit.TakeDamage(king.Unit.MaxHealth);
                yield return new WaitForSeconds(0.3f);
                yield return PlaytestCapture.ShootWithUiAt("invasion-portrait-fallen", 1600, 900, king.Home, 18f, 52f, 0f);
            }
            ArmyManager.Instance.SelectGroup(null);
        }

        /// <summary>The King's figure stands, runs when he walks and swings when he strikes -- and the photographs show it.</summary>
        [UnityTest]
        public IEnumerator TheKing_RunsAndStrikes()
        {
            var king = InvasionMode.Instance.King;
            var animator = king.GetComponent<FigureAnimator>();
            Assert.IsNotNull(animator, "The King's figure is not animated.");
            var leg = king.GetComponentsInChildren<Transform>().First(t => t.name.EndsWith("LegL"));
            yield return new WaitForSeconds(0.5f);
            Assert.Less(animator.Run, 0.2f, "Standing still, the King is drawn running.");
            if (PlaytestCapture.Available) yield return PlaytestCapture.Shoot("king-stand", king.transform.position + Vector3.up * 0.35f, 2.2f, 20f, 200f, 35f);

            var target = king.transform.position + new Vector3(-8f, 0f, 0f);
            king.Unit.Group.OrderMoveTo(target);
            // up to speed within two seconds -- a fixed 0.8 s wait failed now and then on a slow frame
            for (var t = 0f; t < 2f && animator.Run <= 0.6f; t += Time.deltaTime) yield return null;
            Assert.Greater(animator.Run, 0.6f, "Walking, the King is not drawn running.");
            var swing = 0f;
            for (var t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                swing = Mathf.Max(swing, Quaternion.Angle(leg.localRotation, Quaternion.identity));
                yield return null;
            }
            Assert.Greater(swing, 15f, "Running, his legs do not stride.");
            if (PlaytestCapture.Available) yield return PlaytestCapture.Shoot("king-run", king.transform.position + Vector3.up * 0.35f, 2.2f, 20f, 160f, 35f);

            king.Unit.Group.OrderMoveTo(king.transform.position);
            yield return new WaitForSeconds(1f);
            animator.OnStruck();
            Assert.IsTrue(animator.IsStriking, "A blow is not drawn.");
            yield return new WaitForSeconds(FigureAnimator.StrikeSeconds * 0.3f);
            if (PlaytestCapture.Available) yield return PlaytestCapture.Shoot("king-strike-back", king.transform.position + Vector3.up * 0.35f, 2.2f, 20f, 250f, 35f);
            animator.OnStruck();
            yield return new WaitForSeconds(FigureAnimator.StrikeSeconds * 0.55f);
            if (PlaytestCapture.Available) yield return PlaytestCapture.Shoot("king-strike-cut", king.transform.position + Vector3.up * 0.35f, 2.2f, 20f, 250f, 35f);
        }

        /// <summary>The first wave is a countdown away, comes down one lane, and the line under the resource bar follows it.</summary>
        [UnityTest]
        public IEnumerator TheFirstWave_IsACountdownAway_AndComesDownOneLane()
        {
            var waves = InvasionMode.Instance.Waves;
            var config = BalanceConfig.Instance;
            Assert.AreEqual(InvasionWaves.Phase.Breather, waves.Current, "The run did not start with a countdown to the first wave.");
            Assert.AreEqual(0, waves.Wave);
            Assert.AreEqual(config.InvasionFirstWaveSeconds, waves.SecondsUntilNextWave, 3f, "The first wave is not the sheet's wait away.");
            Assert.AreEqual(3, waves.LaneStarts.Count, "Not one lane per orc camp.");
            var status = GameObject.Find("SettlementStatus").GetComponent<UnityEngine.UI.Text>();
            yield return new WaitForSecondsRealtime(0.4f);
            StringAssert.Contains(Localization.Format("#hud_inv_wave_in", 1, ""), status.text, "The line under the resource bar does not count down to wave 1.");

            waves.SendNextWave();
            Assert.AreEqual(InvasionWaves.Phase.Underway, waves.Current);
            Assert.AreEqual(1, waves.LastLanes.Count, "The first wave came down more than one lane.");
            Assert.AreEqual(InvasionWaves.OrcsPerLane(1, config.InvasionWaveBaseOrcs, config.InvasionWaveExtraOrcEvery, config.InvasionWaveMaxOrcs), waves.OrcsLeft);
            var start = waves.LaneStarts[waves.LastLanes[0]];
            foreach (var orc in waves.WaveOrcs) Assert.Less(Vector3.Distance(Flat(orc.transform.position), Flat(start)), 4f, "An orc of the wave stepped out away from its camp.");
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.AreEqual(Localization.Format("#hud_inv_wave", 1, waves.OrcsLeft), status.text, "The line under the resource bar does not show the wave underway.");
        }

        /// <summary>Every lane carries a wave all the way: from where its orcs step out to the castle.</summary>
        [Test]
        public void EveryLane_LeadsFromItsCamp_ToTheCastle()
        {
            var castle = InvasionMode.Instance.Castle.transform.position;
            Assert.AreEqual(3, InvasionMode.Instance.Waves.LaneStarts.Count, "Not one lane per orc camp.");
            foreach (var start in InvasionMode.Instance.Waves.LaneStarts)
            {
                var near = castle + Flat(start - castle).normalized * 6f;
                var length = PathLength(start, near, out var complete);
                Assert.IsTrue(complete, $"No way from the lane at {start} to the castle.");
                Assert.Less(length, 90f, $"The way from the lane at {start} to the castle is {length:0} m -- it wanders off the lane.");
            }
        }

        /// <summary>The user's rule: run orcs go straight for the castle and turn only on a building they pass close by.</summary>
        [UnityTest]
        public IEnumerator RunOrcs_GoForTheCastle_AndTurnOnlyOnABuildingCloseBy()
        {
            var mode = InvasionMode.Instance;
            var far = OrcRaidManager.Instance.SpawnOrcs(mode.Waves.LaneStarts[0], count: 1, level: 1);
            var house = mode.Houses[0];
            var beside = house.transform.position + Flat(house.transform.position - mode.Castle.transform.position).normalized * 3f;
            Assert.IsTrue(NavMesh.SamplePosition(beside, out var hit, 2f, NavMesh.AllAreas), "Nowhere beside the house to stand an orc.");
            var close = OrcRaidManager.Instance.SpawnOrcs(hit.position, count: 1, level: 1);
            yield return null;
            yield return null;
            Assert.AreEqual(mode.Castle, far.Target, "An orc far out on the lane is not marching on the castle.");
            Assert.AreEqual(house, close.Target, "An orc right beside a house walked past it.");
        }

        /// <summary>A run's walls and gates are obstacles (the user, 2026-10-22): an orc beside one walks past it to the castle, a plain gate does not shoot, and they are sturdier than the sandbox's.</summary>
        [UnityTest]
        public IEnumerator Walls_AreObstacles_NotTargets()
        {
            var mode = InvasionMode.Instance;
            var config = BalanceConfig.Instance;
            var fenceData = PlaytestWorld.Building("Fence");
            var gateData = PlaytestWorld.Building("Gate");
            for (var level = 1; level <= 5; level++)
                Assert.AreEqual(0, gateData.LevelStats(level).defense, $"A plain gate shoots at level {level}.");

            var fence = PlaytestWorld.Place(fenceData, PlaytestWorld.FindFreeAreaNearTheMiddle(Vector2Int.one));
            Assert.AreEqual(Mathf.RoundToInt(fenceData.LevelStats(1).maxHealth * config.InvasionWallHealthMultiplier), fence.MaxHealth, "A run's fence is no sturdier than the sandbox's.");

            // an orc right beside the fence (and nothing else close) does not turn on it
            var at = fence.transform.position + Vector3.right * 1.2f;
            Assert.IsTrue(NavMesh.SamplePosition(at, out var hit, 2f, NavMesh.AllAreas));
            var orc = OrcRaidManager.Instance.SpawnOrcs(hit.position, count: 1, level: 1);
            yield return null;
            yield return null;
            Assert.AreNotEqual(fence, orc.Target, "An orc went for a fence that was not in its way.");
            orc.TakeDamage(orc.MaxHealth);
        }

        /// <summary>The orcs go for the island's people too: a citizen beside an orc is struck down, and the town has one fewer.</summary>
        [UnityTest]
        public IEnumerator Orcs_StrikeDownCitizens()
        {
            var visuals = CitizenVisualsManager.Instance;
            CitizenAgent citizen = null;
            for (var t = 0f; t < 5f && citizen == null; t += Time.deltaTime)
            {
                citizen = visuals.AllAgents.FirstOrDefault(a => a != null && a.gameObject.activeInHierarchy);
                yield return null;
            }
            Assert.IsNotNull(citizen, "No citizen out on the island.");
            // the King, guarding the base, would be the orc's first target
            InvasionMode.Instance.King.Fall();
            var people = CitizenManager.Instance.TotalPopulation;
            var orc = OrcRaidManager.Instance.SpawnOrcs(citizen.transform.position + Vector3.right * 0.5f, count: 1, level: 3);
            orc.Toughen(20f);   // a tower in reach must not end the test first
            // whoever stands nearest the orc falls first -- the one it was put beside may walk off
            for (var t = 0f; t < 15f && CitizenManager.Instance.TotalPopulation >= people; t += Time.deltaTime) yield return null;
            Assert.Less(CitizenManager.Instance.TotalPopulation, people, "The orc struck down nobody standing beside it.");
            yield return null;
            Assert.AreEqual(CitizenManager.Instance.TotalPopulation, visuals.AllAgents.Count(a => a != null), "The dead and the town's count disagree: somebody else vanished in a dead man's place.");
            if (orc != null) orc.TakeDamage(orc.MaxHealth);
        }

        /// <summary>
        /// A quarry's men go back to the rocks once its upgrade is done (playtest 5, 2026-10-22: they
        /// stood idle at it, still holding their rock, until taken off and put back), and a run's
        /// boulders hold inv_stone_reserve_mult times the sandbox's stone.
        /// </summary>
        [UnityTest]
        public IEnumerator QuarryMen_GoBackToTheRocks_AfterAnUpgrade()
        {
            var config = BalanceConfig.Instance;
            var castle = InvasionMode.Instance.Castle.transform.position;
            var rock = ResourceNode.All.Where(n => n != null && n.ResourceType == ResourceType.Stone && !n.IsDepleted)
                .OrderBy(n => (n.transform.position - castle).sqrMagnitude).FirstOrDefault();
            Assert.IsNotNull(rock, "No stone on the run's map.");
            Assert.GreaterOrEqual(rock.TotalYield, Mathf.RoundToInt(config.StoneForRockSize(1) * config.InvasionStoneReserveMultiplier), "A run's boulder holds no more than a sandbox one.");
            Assert.AreEqual(config.InvasionPerHarvest, ResourceNode.YieldPerHarvestFor(ResourceType.Stone));
            Assert.AreEqual(config.InvasionPerHarvest, ResourceNode.YieldPerHarvestFor(ResourceType.Wood), "The sawmill and the quarry bring different loads.");

            var data = PlaytestWorld.Building("Quarry");
            var quarry = PlaytestWorld.Place(data, PlaytestWorld.FindFreeAreaNear(GridManager.Instance.WorldToCell(rock.transform.position), data.footprintSize));
            var yard = quarry.GetComponent<ProductionBuilding>();
            yard.SetAssignedWorkers(1);
            var visuals = CitizenVisualsManager.Instance;
            System.Func<CitizenAgent[]> men = () => visuals.AllAgents.Where(a => a != null && visuals.WorkplaceOf(a) == yard).ToArray();
            var loads = 0;
            System.Action<ProductionBuilding, int> count = (b, n) => { if (b == yard) loads++; };
            CitizenVisualsManager.Delivered += count;
            try
            {
                for (var t = 0f; t < 40f && loads == 0; t += Time.deltaTime) yield return null;
                Assert.Greater(loads, 0, "The quarry's man brought no stone to begin with.");
                Assert.IsNotNull(men().Single().WorkNode, "The quarry's man has no rock to go back to.");

                // an upgrade long enough to outlast the wait; he comes home and rests
                ConstructionSite.Begin(quarry, 600f, upgrade: true);
                var before = loads;
                for (var t = 0f; t < 40f && loads == before; t += Time.deltaTime) yield return null;
                Assert.Greater(loads, before, "He did not come home during the upgrade.");
                Assert.IsNull(men().Single().WorkNode, "He went back out to the rocks while the quarry was being upgraded.");

                quarry.Site.Finish();
                for (var t = 0f; t < 8f && men().Any(a => a.WorkNode == null); t += Time.deltaTime) yield return null;
                Assert.IsTrue(men().All(a => a.WorkNode != null), "The quarry's man stood idle after its upgrade.");
            }
            finally
            {
                CitizenVisualsManager.Delivered -= count;
            }
        }

        /// <summary>
        /// A yard's killed worker is replaced by an idle man (playtest 5,
        /// 2026-10-22: «4 workers» on the card, two at work, until all were taken off and put back).
        /// </summary>
        [UnityTest]
        public IEnumerator AKilledWorker_IsReplaced()
        {
            var data = PlaytestWorld.Building("Sawmill");
            var yard = PlaytestWorld.Place(data, PlaytestWorld.FindFreeAreaNearTheMiddle(data.footprintSize)).GetComponent<ProductionBuilding>();
            yard.SetAssignedWorkers(2);
            yield return null;
            var visuals = CitizenVisualsManager.Instance;
            System.Func<int> working = () => visuals.AllAgents.Count(a => a != null && visuals.WorkplaceOf(a) == yard);
            Assert.AreEqual(2, working());

            visuals.KillAgent(visuals.AllAgents.First(a => a != null && visuals.WorkplaceOf(a) == yard));
            yield return null;
            Assert.AreEqual(yard.AssignedWorkers, working(), "The killed worker's place stayed empty with idle men about.");
        }

        /// <summary>The fog lifts round the King and closes again behind him (the user, 2026-10-22).</summary>
        [UnityTest]
        public IEnumerator TheKing_LiftsTheFog_OnlyWhileHeIsThere()
        {
            var king = InvasionMode.Instance.King;
            var fog = CityBuilder.Maps.FogOfWarManager.Instance;
            var far = InvasionMode.Instance.Waves.LaneStarts[0] + (InvasionMode.Instance.Castle.transform.position - InvasionMode.Instance.Waves.LaneStarts[0]).normalized * 20f;
            Assert.IsTrue(NavMesh.SamplePosition(far, out var hit, 4f, NavMesh.AllAreas), "Nowhere out on the lane for the King to stand.");
            var cell = GridManager.Instance.WorldToCell(hit.position);
            Assert.IsFalse(fog.IsCellRevealed(cell), "The lane is clear of fog before the King went there.");

            king.GetComponent<CharacterController>().enabled = false;
            king.transform.position = hit.position;
            for (var t = 0f; t < 1f && !fog.IsCellRevealed(cell); t += Time.deltaTime) yield return null;
            Assert.IsTrue(fog.IsCellRevealed(cell), "The fog did not lift round the King.");

            king.transform.position = king.Home;
            king.GetComponent<CharacterController>().enabled = true;
            for (var t = 0f; t < 1f && fog.IsCellRevealed(cell); t += Time.deltaTime) yield return null;
            Assert.IsFalse(fog.IsCellRevealed(cell), "The fog stayed lifted after the King left.");
        }

        /// <summary>A run has no Laboratory: every one of its buildings can be taken in hand (towers, gates, the healer's were refused).</summary>
        [Test]
        public void EveryRunBuilding_CanBePickedUp()
        {
            var placer = Object.FindAnyObjectByType<BuildingPlacer>();
            foreach (var id in InvasionRules.Buildings)
            {
                placer.SelectBuilding(PlaytestWorld.Building(id));
                Assert.IsTrue(placer.IsSelecting, $"The {id} cannot be picked up in a run.");
                placer.ClearSelection();
            }
        }

        /// <summary>
        /// A run's building takes time to put up and does nothing meanwhile (the user, 2026-10-22):
        /// a tower placed is a construction site that does not shoot, a hammer label stands over it,
        /// and when the time is up it works; an upgrade likewise rests the building and raises its
        /// level only at the end.
        /// </summary>
        [UnityTest]
        public IEnumerator Building_AndUpgrading_TakeTime_AndTheBuildingRests()
        {
            var placer = Object.FindAnyObjectByType<BuildingPlacer>();
            var tower = PlaytestWorld.Building("Tower");
            ResourceManager.Instance.Add(ResourceType.Wood, 2000);
            ResourceManager.Instance.Add(ResourceType.Stone, 2000);
            ResourceManager.Instance.Add(InvasionRules.Money, 2000);
            var cell = PlaytestWorld.FindFreeAreaNearTheMiddle(tower.footprintSize);
            placer.PinnedOriginForTests = cell;
            placer.SelectBuilding(tower);
            yield return null;
            placer.ConfirmPlacement();
            placer.PinnedOriginForTests = null;
            placer.ClearSelection();
            var built = Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None).FirstOrDefault(b => b.Data == tower && b.OriginCell == cell);
            Assert.IsNotNull(built, "The tower was not placed.");
            Assert.IsFalse(built.IsOperational, "A run's tower stood finished the moment it was placed.");
            var expected = InvasionRules.BuildSeconds(BuildingPlacer.CostOf(tower));
            Assert.AreEqual(expected, built.Site.Total, 0.01f, "The build time is not the price's.");
            yield return null;
            Assert.IsTrue(GameObject.Find("ConstructionLabel") != null, "No hammer over the construction site.");
            Assert.IsFalse(built.TryUpgrade(), "A building under construction could be upgraded.");

            built.Site.Finish();
            yield return null;
            Assert.IsTrue(built.IsOperational, "The tower did not work once built.");

            var level = built.Level;
            Assert.IsTrue(built.TryUpgrade(), "The finished tower could not be upgraded.");
            Assert.IsFalse(built.IsOperational, "An upgrade did not rest the building.");
            Assert.AreEqual(level, built.Level, "The level rose before the upgrade's time was up.");
            built.Site.Finish();
            Assert.AreEqual(level + 1, built.Level, "The upgrade's level was not applied at the end.");
            Assert.IsTrue(built.IsOperational);
        }

        /// <summary>
        /// A tap on a building holds it (the user, 2026-10-22): the «!» comes up in place of the
        /// build menu, the health bar stays up, and the «!» opens the card; a tap on open ground lets go.
        /// </summary>
        [UnityTest]
        public IEnumerator ATappedBuilding_IsHeld_AndItsBangOpensTheCard()
        {
            var focus = CityBuilder.UI.BuildingFocus.Instance;
            Assert.IsNotNull(focus, "No BuildingFocus in the game.");
            var tower = Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None).First(b => b.Data != null && b.Data.buildingName == "Tower");
            focus.Select(tower);
            yield return null;
            Assert.AreEqual(tower, focus.Current);
            Assert.IsNotNull(GameObject.Find("BuildingDetailsButton"), "No «!» button while a building is held.");
            var bar = tower.transform.Find("HealthBar");
            Assert.IsTrue(bar != null && bar.gameObject.activeSelf, "The held building's health is not shown.");
            Assert.IsTrue(GameObject.Find("BuildingFocusRing") != null, "No ring round the held building.");

            focus.OpenDetails();
            var panel = Object.FindFirstObjectByType<CityBuilder.UI.BuildingInfoPanelController>(FindObjectsInactive.Include);
            Assert.IsTrue(panel != null && GameObject.Find("BuildingInfoPanel") != null, "The «!» did not open the building's card.");
            yield return null;
            Assert.IsNull(GameObject.Find("BuildingDetailsButton"), "The «!» stayed over the card it opened (playtest 4).");
            panel.Close();
            yield return null;
            Assert.IsNotNull(GameObject.Find("BuildingDetailsButton"), "The «!» did not come back when the card closed.");
            focus.Clear();
            yield return null;
            Assert.IsNull(focus.Current);
            Assert.IsNull(GameObject.Find("BuildingDetailsButton"), "The «!» stayed after letting the building go.");
        }

        /// <summary>A wave beaten off pays its orcs' gold and 10 × its number, and the breather begins.</summary>
        [UnityTest]
        public IEnumerator AWaveBeatenOff_PaysItsGold_AndTheBreatherBegins()
        {
            var waves = InvasionMode.Instance.Waves;
            var coins = ResourceManager.Instance.GetAmount(InvasionRules.Money);
            waves.SendNextWave();
            var orcs = waves.WaveOrcs.ToList();
            Assert.Greater(orcs.Count, 0, "The wave sent no orcs.");
            var expected = InvasionWaves.GoldForWave(1, BalanceConfig.Instance.InvasionGoldPerWave);
            foreach (var orc in orcs)
            {
                expected += InvasionWaves.GoldFor(orc.Kind);
                orc.TakeDamage(orc.MaxHealth);
            }
            yield return null;
            yield return null;
            Assert.AreEqual(InvasionWaves.Phase.Breather, waves.Current, "The wave's last orc fell and the wave went on.");
            Assert.AreEqual(1, waves.WavesSurvived);
            Assert.AreEqual(BalanceConfig.Instance.InvasionBreatherSeconds, waves.SecondsUntilNextWave, 0.5f, "The breather is not the sheet's.");
            Assert.AreEqual(coins + expected, ResourceManager.Instance.GetAmount(InvasionRules.Money), "The gold for the orcs and the wave did not add up.");

            // the next wave is the next number, and once its time comes the lanes open up
            waves.SendNextWave();
            Assert.AreEqual(2, waves.Wave);
        }

        /// <summary>A camp at every "monsters" spot with the beasts its level says, the giant in his lair, jointed figures all.</summary>
        [Test]
        public void TheJungle_HasACampAtEverySpot_WithItsBeasts()
        {
            var jungle = InvasionMode.Instance.Jungle;
            var spots = MeshMapApplier.Instance.CurrentMap.SpotsOf("monsters");
            Assert.Greater(spots.Count, 0, "The map has no monster spots.");
            Assert.AreEqual(spots.Count + 1, jungle.Camps.Count, "Not one camp per monster spot plus the boss's lair.");
            foreach (var camp in jungle.Camps)
            {
                var kind = camp.IsBoss ? OrcKind.Giant : InvasionJungle.KindOfCamp(camp.Tier);
                Assert.AreEqual(camp.IsBoss ? 1 : InvasionJungle.CampSize(camp.Tier), camp.Monsters.Count, $"The camp at {camp.Post} is not full.");
                foreach (var monster in camp.Monsters)
                {
                    Assert.AreEqual(kind, monster.Kind, $"The camp of level {camp.Tier} at {camp.Post} holds the wrong beast.");
                    Assert.IsTrue(monster.IsGuard, "A monster is not holding its camp.");
                    Assert.IsNotNull(monster.transform.Find(kind.ToString()), $"The {kind} is drawn in cubes -- its model (Models/Wild) is not wired.");
                    Assert.IsNotNull(monster.GetComponent<FigureAnimator>(), $"The {kind} is not jointed.");
                }
            }
            Assert.IsNotNull(jungle.BossLair, "No boss.");
            Assert.AreEqual(OrcKind.Giant, jungle.BossLair.Monsters[0].Kind);
            Assert.AreEqual(3, jungle.Shrines.Count, "Not a shrine at each shrine spot.");
            Assert.AreEqual(3, GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => t.name == "Shrine"), "The shrines are not drawn.");
        }

        /// <summary>Left alone, the monsters stay at their camps: they do not march on the castle.</summary>
        [UnityTest]
        public IEnumerator TheMonsters_HoldTheirGround()
        {
            var monsters = InvasionMode.Instance.Jungle.Camps.SelectMany(c => c.Monsters).ToList();
            var posts = monsters.Select(m => m.GuardPost).ToList();
            yield return new WaitForSeconds(3f);
            for (var i = 0; i < monsters.Count; i++)
            {
                Assert.IsNotNull(monsters[i], "A monster fell with nobody near it.");
                Assert.Less(Vector3.Distance(Flat(monsters[i].transform.position), Flat(posts[i])), 1.5f, $"A {monsters[i].Kind} left its camp.");
                Assert.IsNull(monsters[i].Target, $"A {monsters[i].Kind} is going for a building.");
            }
        }

        /// <summary>A camp wiped out pays its gold and comes back after the wait -- a level higher once the base has stood enough waves.</summary>
        [UnityTest]
        public IEnumerator AWipedCamp_ComesBack_AfterTheWait_StrongerWithTheWaves()
        {
            var jungle = InvasionMode.Instance.Jungle;
            var waves = InvasionMode.Instance.Waves;
            var config = BalanceConfig.Instance;
            var camp = jungle.Camps.First(c => c.Tier == 1 && !c.IsBoss);
            Assert.AreEqual(1, camp.Level);

            var coins = ResourceManager.Instance.GetAmount(InvasionRules.Money);
            var beasts = camp.Monsters.ToList();
            foreach (var beast in beasts) beast.TakeDamage(beast.MaxHealth);
            yield return null;
            Assert.AreEqual(0, camp.Monsters.Count);
            Assert.AreEqual(coins + beasts.Count * InvasionWaves.GoldForKill(OrcKind.Wolf, 1), ResourceManager.Instance.GetAmount(InvasionRules.Money), "The wolves did not pay their gold.");
            Assert.AreEqual(config.InvasionCampRespawnSeconds, camp.RespawnIn, 0.5f, "The camp's wait is not the sheet's.");

            // the base stands through enough waves for the jungle to grow a level
            for (var w = 0; w < config.InvasionMonsterLevelEveryWaves; w++)
            {
                waves.SendNextWave();
                foreach (var orc in waves.WaveOrcs.ToList()) orc.TakeDamage(orc.MaxHealth);
                yield return null;
                yield return null;
            }
            Assert.AreEqual(config.InvasionMonsterLevelEveryWaves, waves.WavesSurvived);

            jungle.Tick(config.InvasionCampRespawnSeconds - 1f);
            Assert.AreEqual(0, camp.Monsters.Count, "The camp came back before its time.");
            jungle.Tick(1.5f);
            Assert.AreEqual(InvasionJungle.CampSize(1), camp.Monsters.Count, "The camp did not come back.");
            Assert.AreEqual(2, camp.Level, "The camp came back no stronger after the waves.");
            Assert.AreEqual(2, camp.Monsters[0].Level);
        }

        /// <summary>A shrine heals the King standing at it; away from it, nothing.</summary>
        [Test]
        public void AShrine_HealsTheKing_StandingAtIt()
        {
            var jungle = InvasionMode.Instance.Jungle;
            var king = InvasionMode.Instance.King;
            var hurt = king.Unit.MaxHealth / 2;
            king.Unit.SetCurrentHealth(hurt);
            jungle.Tick(1f);
            Assert.AreEqual(hurt, king.Unit.CurrentHealth, "The King was healed at home, nowhere near a shrine.");

            var controller = king.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            king.transform.position = jungle.Shrines[0] + new Vector3(1.5f, 0f, 0f);
            jungle.Tick(1f);
            if (controller != null) controller.enabled = true;
            var expected = Mathf.CeilToInt(king.Unit.MaxHealth * BalanceConfig.Instance.InvasionShrineHealPerSecond);
            Assert.AreEqual(hurt + expected, king.Unit.CurrentHealth, 1, "A second at the shrine did not give back the sheet's share of his health.");
        }

        /// <summary>The giant down: gold, wood and stone by his level, the King blessed for a while, the lair empty until it comes back.</summary>
        [UnityTest]
        public IEnumerator TheBoss_PaysAndBlessesTheKing()
        {
            var jungle = InvasionMode.Instance.Jungle;
            var king = InvasionMode.Instance.King;
            var config = BalanceConfig.Instance;
            var resources = ResourceManager.Instance;
            var coins = resources.GetAmount(InvasionRules.Money);
            var wood = resources.GetAmount(ResourceType.Wood);
            var stone = resources.GetAmount(ResourceType.Stone);
            var giant = jungle.BossLair.Monsters[0];
            var level = giant.Level;

            giant.TakeDamage(giant.MaxHealth);
            yield return null;
            Assert.AreEqual(coins + config.InvasionBossCoins * level, resources.GetAmount(InvasionRules.Money), "The giant's gold is wrong.");
            Assert.AreEqual(wood + config.InvasionBossWood * level, resources.GetAmount(ResourceType.Wood));
            Assert.AreEqual(stone + config.InvasionBossStone * level, resources.GetAmount(ResourceType.Stone));
            Assert.AreEqual(config.InvasionBossRespawnSeconds, jungle.BossLair.RespawnIn, 0.5f);
            Assert.Greater(king.BuffRemaining, config.InvasionBossBuffSeconds - 1f, "The King was not blessed.");
            Assert.AreEqual(Mathf.RoundToInt(KingHero.DamageAt(king.Level) * (1f + config.InvasionBossBuffDamage)), king.Unit.AttackDamage, "The blessing did not raise his damage.");
            Assert.AreEqual(Mathf.RoundToInt(KingHero.MaxHealthAt(king.Level) * (1f + config.InvasionBossBuffHealth)), king.Unit.MaxHealth, "The blessing did not raise his health.");

            // a blessing runs out
            king.Empower(0.1f, config.InvasionBossBuffDamage, config.InvasionBossBuffHealth);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(0f, king.BuffRemaining);
            Assert.AreEqual(KingHero.DamageAt(king.Level), king.Unit.AttackDamage, "The blessing never wore off.");
            Assert.AreEqual(KingHero.MaxHealthAt(king.Level), king.Unit.MaxHealth);
        }

        /// <summary>A round camp at each lane's end: its buildings at their level with their models, both gates, the garrison, and no portal to close.</summary>
        [Test]
        public void TheRoundCamps_Stand_WithTheirBuildingsAndGarrison()
        {
            var camps = InvasionMode.Instance.OrcCamps;
            var config = BalanceConfig.Instance;
            var plan = InvasionCampLayout.Pieces().Count(p => p.Kind != InvasionCampLayout.PieceKind.Gate);
            Assert.AreEqual(3, camps.Count, "Not a round camp at each lane's end.");
            foreach (var camp in camps)
            {
                Assert.AreEqual(plan, camp.Buildings.Count, $"The camp at {camp.Centre} is missing buildings.");
                foreach (var building in camp.Buildings)
                {
                    Assert.AreEqual(config.InvasionCampBuildingLevel, building.Level);
                    Assert.IsNotNull(building.transform.Find("Model"), $"A {building.Kind} of the round camp is drawn in a box -- its model is not wired.");
                }
                Assert.AreEqual(2, camp.GetComponentsInChildren<Transform>().Count(t => t.name == "OrcRingGate" && t.GetComponentInChildren<Renderer>() != null), "The gates are not drawn.");
                Assert.AreEqual(config.InvasionCampRaiders, camp.GuardsOf(OrcKind.Raider));
                Assert.AreEqual(config.InvasionCampThrowers, camp.GuardsOf(OrcKind.Thrower));
                Assert.IsTrue(camp.GuardUnits.All(g => g.Level == config.InvasionCampGuardLevel && g.IsGuard), "A guard is not of the top level or not on guard.");
            }
            Assert.AreEqual(0, OrcPortal.All.Count, "A run has a portal the player can close -- the camps' portals cannot be.");
        }

        /// <summary>The rings are walls -- going round them is the long way -- and the waves still find their way out of the portal through both gates.</summary>
        [UnityTest]
        public IEnumerator TheWalls_Block_AndTheGatesLetTheWaveOut()
        {
            yield return new WaitForSeconds(0.5f);       // the walls carve the NavMesh on a later frame
            var castle = InvasionMode.Instance.Castle.transform.position;
            foreach (var camp in InvasionMode.Instance.OrcCamps)
            {
                var near = castle + Flat(camp.Centre - castle).normalized * 6f;
                var length = PathLength(camp.WaveExit, near, out var complete);
                Assert.IsTrue(complete, $"The wave cannot get out of the camp at {camp.Centre}.");
                Assert.Less(length, 90f, $"The way out of the camp at {camp.Centre} is {length:0} m.");

                // from beside the camp, just outside its wall, in to the portal: round to the gates
                var front = Flat(castle - camp.Centre).normalized;
                var side = new Vector3(front.z, 0f, -front.x);
                var outside = camp.Centre + side * (InvasionCampLayout.OuterRadius + 1.5f);
                var straight = Vector3.Distance(Flat(outside), Flat(camp.WaveExit));
                var around = PathLength(outside, camp.WaveExit, out var reached);
                Assert.IsTrue(reached, $"No way into the camp at {camp.Centre} at all.");
                Assert.Greater(around, straight + 12f, $"The way in from beside the camp at {camp.Centre} is {around:0} m for {straight:0} m straight -- the walls do not block.");
            }
        }

        /// <summary>The garrison comes back one by one while its spawners stand; with the war tents down, the orcs do not.</summary>
        [UnityTest]
        public IEnumerator AFallenSpawner_SendsNoMoreOfItsKind()
        {
            var camp = InvasionMode.Instance.OrcCamps[0];
            var config = BalanceConfig.Instance;
            foreach (var tent in camp.Buildings.Where(b => b.Kind == OrcCampBuildingKind.WarTent).ToList()) tent.TakeDamage(tent.MaxHealth);
            foreach (var guard in camp.GuardUnits.ToList()) guard.TakeDamage(guard.MaxHealth);
            yield return null;
            Assert.AreEqual(0, camp.GuardsStanding);

            camp.Tick(config.InvasionCampGuardRespawnSeconds + 0.1f);
            Assert.AreEqual(1, camp.GuardsStanding, "Not one guard back for one wait.");
            for (var i = 0; i < config.InvasionCampRaiders + config.InvasionCampThrowers; i++) camp.Tick(config.InvasionCampGuardRespawnSeconds + 0.1f);
            Assert.AreEqual(config.InvasionCampThrowers, camp.GuardsOf(OrcKind.Thrower), "The throwers' ranges stand, and not all the throwers came back.");
            Assert.AreEqual(0, camp.GuardsOf(OrcKind.Raider), "The war tents are down, and the orcs came back anyway.");
        }

        [UnityTest]
        public IEnumerator Photograph_TheRun()
        {
            if (!PlaytestCapture.Available) Assert.Ignore("No graphics device: run without -nographics to photograph.");
            yield return new WaitForSeconds(1f);
            yield return PlaytestCapture.Shoot("invasion-overview", Vector3.zero, 200f, 55f, 0f);
            yield return PlaytestCapture.Shoot("invasion-base", Vector3.zero, 44f, 52f, 0f, 60f);
            yield return PlaytestCapture.Shoot("invasion-king", InvasionMode.Instance.King.transform.position, 9f, 40f, 0f, 60f);
            yield return PlaytestCapture.Shoot("invasion-causeway", Causeways[1], 22f, 50f, 0f, 60f);
            // the jungle and the camps lie in the fog until scouted: lift it where the shots look
            var jungle = InvasionMode.Instance.Jungle;
            Reveal(jungle.Camps.First(c => c.Tier == 1).Post, 8);
            Reveal(jungle.Shrines[0], 8);
            Reveal(jungle.BossLair.Post, 10);
            foreach (var c in InvasionMode.Instance.OrcCamps) Reveal(c.Centre, 22);
            yield return new WaitForSeconds(FogOfWarManager.RevealSeconds + 0.5f);
            yield return PlaytestCapture.Shoot("invasion-wolves", InvasionMode.Instance.Jungle.Camps.First(c => c.Tier == 1).Post, 6f, 40f, 0f, 45f);
            yield return PlaytestCapture.Shoot("invasion-shrine", InvasionMode.Instance.Jungle.Shrines[0], 5f, 40f, 0f, 45f);
            yield return PlaytestCapture.Shoot("invasion-boss", InvasionMode.Instance.Jungle.BossLair.Post, 8f, 40f, 0f, 45f);
            var orcCamp = InvasionMode.Instance.OrcCamps[0];
            var gateBearing = Mathf.Atan2(-orcCamp.Centre.x, -orcCamp.Centre.z) * Mathf.Rad2Deg;
            yield return PlaytestCapture.Shoot("invasion-orc-camp", orcCamp.Centre, 40f, 55f, gateBearing, 45f);
            yield return PlaytestCapture.Shoot("invasion-orc-camp-gate", orcCamp.Centre + Flat(-orcCamp.Centre).normalized * 12f, 14f, 40f, gateBearing, 45f);

            // a house making a new person, and the run's build menu
            CitizenManager.Instance.KillCitizens(1);
            yield return new WaitForSeconds(6f);
            var house = InvasionMode.Instance.Houses.First(h => InvasionMode.Instance.People.ProgressOf(h) > 0f);
            yield return PlaytestCapture.Shoot("invasion-house", house.transform.position + Vector3.up * 0.5f, 6f, 35f, 0f, 45f);
            var menu = Object.FindAnyObjectByType<CityBuilder.UI.BuildingCategoryPanel>();
            menu.Open(BuildingCategory.Defence, instant: true);
            yield return null;
            yield return PlaytestCapture.ShootWithUi("invasion-menu", Vector3.zero, 44f, 52f, 0f);
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private static void Reveal(Vector3 at, int radiusCells) =>
            FogOfWarManager.Instance?.RevealPermanent(GridManager.Instance.WorldToCell(at), radiusCells);

        private static NavMeshPath _path;

        private static float PathLength(Vector3 from, Vector3 to, out bool complete)
        {
            Assert.IsTrue(NavMesh.SamplePosition(from, out var a, 3f, NavMesh.AllAreas), $"No NavMesh near {from}.");
            Assert.IsTrue(NavMesh.SamplePosition(to, out var b, 3f, NavMesh.AllAreas), $"No NavMesh near {to}.");
            _path ??= new NavMeshPath();
            NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, _path);
            complete = _path.status == NavMeshPathStatus.PathComplete;
            var length = 0f;
            for (var i = 1; i < _path.corners.Length; i++) length += Vector3.Distance(_path.corners[i - 1], _path.corners[i]);
            return length;
        }
    }
}
