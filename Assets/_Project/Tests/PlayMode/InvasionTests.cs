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

            king.AddExperience(Hero.XpForLevel(1) - 1);
            Assert.AreEqual(1, king.Level, "A level came before its experience was full.");
            king.AddExperience(1 + Hero.XpForLevel(2));
            Assert.AreEqual(3, king.Level, "Experience enough for two levels gave a different number.");
            Assert.AreEqual(health + 2 * BalanceConfig.Instance.KingHealthPerLevel, king.Unit.MaxHealth, "A level did not add the sheet's health.");
            Assert.AreEqual(king.DamageAt(3), king.DamageAt(1) + 2 * BalanceConfig.Instance.KingDamagePerLevel);
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
            Assert.AreEqual(worth, king.Experience + (king.Level - 1) * Hero.XpForLevel(1), "The orc that fell beside him was not worth its health in experience.");

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
            king.AddExperience(Hero.XpForLevel(1));
            var level = king.Level;
            king.transform.position += new Vector3(4f, 0f, 4f);

            king.Unit.TakeDamage(king.Unit.MaxHealth);
            Assert.IsTrue(king.IsFallen, "Struck to zero, the King did not fall.");
            Assert.IsNotNull(king, "The King was destroyed instead of falling.");
            Assert.IsFalse(SoldierUnit.All.Contains(king.Unit), "A fallen King is still there for the orcs to fight.");
            Assert.AreEqual(Hero.RespawnSecondsAt(level), king.RespawnRemaining, 0.01f);

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
            Assert.AreEqual(mode.Houses.Sum(InvasionPeople.PlacesOf), mode.People.Places);
            Assert.AreEqual(config.InvasionHouses * config.Building("Hovel").levels[0].housingCapacity, mode.People.Places, "A level-1 house does not hold the invasion tab's housing_capacity.");
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
                    Assert.IsTrue(cost.type == ResourceType.Wood || cost.type == ResourceType.Stone || cost.type == InvasionRules.Money, $"{id} costs {cost.type} in a run.");
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
            tower.TryDamage(tower.MaxHealth / 4);
            var light = tower.GetRepairCost().Sum(c => c.amount);
            tower.TryDamage(tower.MaxHealth / 2);
            var cost = tower.GetRepairCost();
            Assert.IsNotNull(cost, "A damaged building cannot be repaired.");
            // the price follows the damage (the user, 2026-10-25)
            Assert.Greater(cost.Sum(c => c.amount), light, "Mending three quarters of a tower costs no more than mending a quarter.");
            var full = InvasionRules.RepairCost(tower.Data, tower.Level, 1f).Sum(c => c.amount);
            Assert.Less(cost.Sum(c => c.amount), full, "Mending three quarters of a tower costs as much as mending it from nothing.");
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
            var perHouse = InvasionPeople.PlacesOf(house);
            house.TryDamage(house.MaxHealth);
            yield return null;
            yield return null;
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

        /// <summary>Let go of after an order, the King carries it out, stands hero_return_wait_sec and only then goes home to guard the base (the user, 2026-10-23).</summary>
        [UnityTest]
        public IEnumerator TheKing_LetGo_FinishesHisOrder_WaitsAndGoesHome()
        {
            var king = InvasionMode.Instance.King;
            var army = ArmyManager.Instance;
            army.SelectGroup(king.Unit.Group);
            var spot = king.Home + new Vector3(4f, 0f, 0f);
            king.Unit.Group.OrderMoveTo(spot);
            king.Unit.Group.NotifyPlayerOrder();
            Assert.IsFalse(king.Guarding, "The King kept guarding after the player told him to go somewhere.");

            // let go of on the way (the user, 2026-10-23): he goes on, stands a while, then goes home
            army.SelectGroup(null);
            Assert.IsFalse(king.Guarding, "Let go of, the King dropped his order at once.");
            Assert.Less(Vector3.Distance(Flat(king.Unit.Group.HoldPosition), Flat(spot)), 0.5f, "Let go of, the King's order was changed.");
            var wait = BalanceConfig.Instance.HeroReturnWaitSeconds;
            var stood = 0f;
            for (var t = 0f; t < wait + 15f && !king.Guarding; t += Time.deltaTime)
            {
                if (king.ReturnIn >= 0f && king.ReturnIn < wait) stood += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(king.Guarding, "Let go of, the King never went back to guarding the base.");
            Assert.GreaterOrEqual(stood, wait * 0.9f, "The King went home without standing his while.");
            Assert.Less(Vector3.Distance(Flat(king.Unit.Group.HoldPosition), Flat(king.Home)), 0.5f, "Guarding, the King does not head home.");
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

        /// <summary>A run has as many sawmills and quarries as the invasion tab's max_count says (the user: two, 2026-10-23; three, 2026-10-25): past that the card greys and the placer refuses.</summary>
        [Test]
        public void ARun_HasTwoSawmillsAndTwoQuarries()
        {
            foreach (var id in new[] { "Sawmill", "Quarry" })
            {
                var data = PlaytestWorld.Building(id);
                var max = BalanceConfig.Run.Building(id).maxCount;
                Assert.Greater(max, 0, $"The invasion tab sets no limit on {id}.");
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
            var portrait = Object.FindObjectsByType<CityBuilder.UI.HeroPortraitController>(FindObjectsSortMode.None).First(p => p.HeroType == SoldierType.King);
            Assert.IsNotNull(portrait, "No King's portrait in the game.");
            yield return null;
            Assert.IsNotNull(GameObject.Find("KingPortrait"), "The King's portrait is not shown in a run.");
            var face = GameObject.Find("KingPortrait").transform.Find("Portrait/Mask/Face").GetComponent<UnityEngine.UI.Image>();
            Assert.IsNotNull(face.sprite, "The portrait has no face.");

            var king = InvasionMode.Instance.King;
            portrait.SelectHero();
            Assert.AreEqual(king.Unit.Group, ArmyManager.Instance.SelectedGroup, "Tapping the portrait did not select the King.");
            ArmyManager.Instance.SelectGroup(null);

            // a tap on the King where he stands picks him up too
            var camera = Camera.main;
            yield return null;
            var onScreen = camera.WorldToScreenPoint(king.transform.position + Vector3.up * 0.4f);
            Assert.IsTrue(CityBuilder.UI.HeroPortraitController.TrySelectAt(onScreen, camera), "A tap on the King did not select him.");
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

        /// <summary>
        /// Every soldier is a jointed figure like the heroes (Tools/blender/soldiers.py), animated by
        /// FigureAnimator; one struck down falls with its limbs flying loose -- and the photographs show it.
        /// </summary>
        [UnityTest]
        public IEnumerator TheSoldiers_AreJointedFigures_AndFallLimp()
        {
            var king = InvasionMode.Instance.King;
            var types = new[] { SoldierType.Militia, SoldierType.Spearman, SoldierType.Archer, SoldierType.ManAtArms };
            var at = king.transform.position + new Vector3(0f, 0f, -1.5f);
            for (var i = 0; i < types.Length; i++)
                ArmyManager.Instance.SpawnSummons(types[i], at + new Vector3((i - 1.5f) * 0.7f, 0f, 0f), 1, 1, 60f);
            yield return new WaitForSeconds(0.6f);
            var soldiers = new System.Collections.Generic.List<SoldierUnit>();
            foreach (var type in types)
            {
                var unit = Object.FindObjectsByType<SoldierUnit>(FindObjectsInactive.Exclude).FirstOrDefault(u => u.Type == type);
                Assert.IsNotNull(unit, $"No {type} came out.");
                Assert.IsNotNull(unit.GetComponent<FigureAnimator>(), $"The {type} is not a jointed figure.");
                var parts = unit.GetComponentsInChildren<Transform>().Select(t => t.name).ToList();
                foreach (var joint in new[] { "LegL", "LegR", "Head" })
                    Assert.IsTrue(parts.Any(n => n.EndsWith(joint)), $"The {type} has no {joint}.");
                soldiers.Add(unit);
            }
            if (PlaytestCapture.Available)
            {
                foreach (var unit in soldiers) unit.transform.rotation = Quaternion.LookRotation(Vector3.forward);
                // the King stands between them and the camera
                foreach (var r in king.GetComponentsInChildren<Renderer>()) r.enabled = false;
                yield return PlaytestCapture.Shoot("soldiers-stand", at + Vector3.up * 0.3f, 3.2f, 15f, 180f, 35f);
                soldiers[2].GetComponent<FigureAnimator>().OnStruck();
                soldiers[1].GetComponent<FigureAnimator>().OnStruck();
                yield return new WaitForSeconds(FigureAnimator.StrikeSeconds * 0.6f);
                yield return PlaytestCapture.Shoot("soldiers-strike", at + Vector3.up * 0.3f, 3.2f, 15f, 180f, 35f);
            }

            foreach (var r in king.GetComponentsInChildren<Renderer>()) r.enabled = true;

            // a level's look: metal at 2, gold at 3 -- worn from the start, and changed on a living soldier
            var spearman = soldiers[1];
            Assert.AreEqual(1, spearman.LookLevel);
            Assert.IsFalse(spearman.GetComponentsInChildren<Transform>().Any(t => t.name.Contains("-lvl")), "A level-1 spearman wears a higher level's look.");
            ArmyManager.Instance.Redress(spearman, 3);
            yield return null;
            Assert.IsTrue(spearman.GetComponentsInChildren<Transform>().Any(t => t.name.EndsWith("-lvl3-Head")), "The spearman was not redressed for level 3.");
            Assert.AreEqual(1, spearman.GetComponentsInChildren<FigureAnimator>().Length);
            spearman.GetComponent<FigureAnimator>().OnStruck();
            Assert.IsTrue(spearman.GetComponent<FigureAnimator>().IsStriking, "The redressed spearman's figure is not animated.");
            ArmyManager.Instance.SpawnSummons(SoldierType.Archer, at, 1, 2, 60f);
            yield return null;
            var veteran = Object.FindObjectsByType<SoldierUnit>(FindObjectsInactive.Exclude).First(u => u.Type == SoldierType.Archer && u != soldiers[2]);
            Assert.AreEqual(2, veteran.LookLevel, "A level-2 summon does not wear level 2's look.");
            Assert.IsTrue(veteran.GetComponentsInChildren<Transform>().Any(t => t.name.EndsWith("-lvl2-ArmBow")));

            var dying = soldiers[3];
            dying.TakeDamage(dying.MaxHealth * 10);
            yield return new WaitForSeconds(0.4f);
            var corpse = Object.FindObjectsByType<OrcCorpse>(FindObjectsInactive.Exclude).FirstOrDefault();
            Assert.IsNotNull(corpse, "The man-at-arms did not fall.");
            Assert.Greater(corpse.Limp, 0.9f, "He fell stiff: his limbs did not fly loose.");
            if (PlaytestCapture.Available) yield return PlaytestCapture.Shoot("soldiers-death", at + Vector3.up * 0.2f, 3.2f, 15f, 180f, 35f);
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
            foreach (var orc in waves.WaveOrcs) Assert.AreSame(waves.Lanes[waves.LastLanes[0]], orc.Lane, "An orc of the wave was not sent down its portal's way.");
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
            Assert.AreEqual(BalanceConfig.Run.Building("Fence").levels[0].maxHealth, fence.MaxHealth, "A run's fence does not have the invasion tab's health.");
            Assert.Greater(fence.MaxHealth, BalanceConfig.Sandbox.Building("Fence").levels[0].maxHealth, "A run's fence is no sturdier than the sandbox's.");

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
            Assert.AreEqual(config.InvasionStonePerHarvest, ResourceNode.YieldPerHarvestFor(ResourceType.Stone));
            Assert.AreEqual(config.InvasionWoodPerHarvest, ResourceNode.YieldPerHarvestFor(ResourceType.Wood));

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
        /// Every man of a quarry by the island's east rocks gets to his rock (the user, 2026-10-23:
        /// the bar over a rock filled and emptied and no stone came -- its man was pinned against a
        /// house's corner on the way and retried the same leg for ever).
        /// </summary>
        [UnityTest]
        public IEnumerator QuarryMen_GetToTheirRocks_PastTheHouses()
        {
            var castle = InvasionMode.Instance.Castle.transform.position;
            var rock = ResourceNode.All.Where(n => n != null && n.ResourceType == ResourceType.Stone && (n.transform.position - castle).magnitude < 40f)
                .OrderByDescending(n => n.transform.position.x).First();
            var spot = rock.transform.position + (castle - rock.transform.position).normalized * 7f;
            var data = PlaytestWorld.Building("Quarry");
            var quarry = PlaytestWorld.Place(data, PlaytestWorld.FindFreeAreaNear(GridManager.Instance.WorldToCell(spot), data.footprintSize));
            var yard = quarry.GetComponent<ProductionBuilding>();
            yard.SetAssignedWorkers(yard.MaxWorkers);
            var visuals = CitizenVisualsManager.Instance;
            var reached = new System.Collections.Generic.HashSet<CitizenAgent>();
            Time.timeScale = 3f;
            // the stuck man got to his rock a time or two before the house's corner caught him: it
            // is the later trips that tell
            for (var t = 0f; t < 100f; t += Time.deltaTime)
            {
                foreach (var man in visuals.AllAgents)
                {
                    if (t < 50f || man == null || visuals.WorkplaceOf(man) != yard || man.WorkNode == null) continue;
                    if (Vector3.Distance(Flat(man.transform.position), Flat(man.WorkNode.transform.position)) < 1f) reached.Add(man);
                }
                yield return null;
            }
            Time.timeScale = 1f;
            Assert.AreEqual(yard.MaxWorkers, reached.Count, "A quarry man never got to his rock.");
        }

        /// <summary>
        /// A run plays on the sheet's invasion tab (the user, 2026-10-23): its own config, its own
        /// copies of the buildings, and the numbers the user asked for -- towers shoot 1.5 times
        /// further than the sandbox's, two sawmills and two quarries.
        /// </summary>
        [Test]
        public void TheRun_PlaysOnTheInvasionTab()
        {
            Assert.AreSame(BalanceConfig.Run, BalanceConfig.Instance, "A run reads the sandbox's numbers.");
            Assert.AreNotSame(BalanceConfig.Sandbox, BalanceConfig.Run, "There is no run config apart from the sandbox's.");
            Assert.AreEqual(BalanceConfig.Sandbox.DefenceAttackRangeMeters * 1.5f, BalanceConfig.Run.DefenceAttackRangeMeters, 0.01f, "A run's towers do not shoot 1.5 times further.");

            var tower = Object.FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None).First(b => b != null && b.Data != null && b.Data.buildingName == "Tower");
            Assert.AreEqual(BalanceConfig.Run.DefenceAttackRangeMeters, DefensiveBuilding.RangeOf(tower), 0.01f, "A run's tower does not shoot as far as the invasion tab says.");
            Assert.AreEqual(BalanceConfig.Run.Building("Tower").levels[0].maxHealth, tower.MaxHealth, "A run's tower does not have the invasion tab's health.");
            var quarry = PlaytestWorld.Building("Quarry");
            Assert.AreEqual(BalanceConfig.Run.Building("Quarry").maxCount, quarry.maxCount);
            Assert.AreEqual(BalanceConfig.Run.Building("Quarry").cost.Sum(c => c.amount), quarry.cost.Sum(c => c.amount), "The menu's quarry does not cost what the invasion tab says.");
        }

        /// <summary>
        /// The Barracks in a run (the user, 2026-10-23): every soldier is hired for gold alone with no
        /// Laboratory; nobody commands them -- they stand before the castle and go for orcs near it --
        /// and a hurt one shows his health.
        /// </summary>
        [UnityTest]
        public IEnumerator BarracksSoldiers_AreForGold_AndGuardTheBaseOnTheirOwn()
        {
            var mode = InvasionMode.Instance;
            var castle = mode.Castle.transform.position;
            var resources = ResourceManager.Instance;
            resources.SetAmount(InvasionRules.Money, 1000);
            var barracks = PlaytestWorld.Place(PlaytestWorld.Building("Barracks"), PlaytestWorld.FindFreeAreaNear(GridManager.Instance.WorldToCell(castle + new Vector3(-8f, 0f, 4f)), new Vector2Int(3, 3)));
            var army = ArmyManager.Instance;
            foreach (var type in new[] { SoldierType.Spearman, SoldierType.Archer, SoldierType.ManAtArms })
            {
                Assert.IsNull(army.DescribeRecruitBlocker(type), $"A run's Barracks will not hire a {type}: {army.DescribeRecruitBlocker(type)}");
                CollectionAssert.AreEqual(new[] { ResourceType.Coins }, SoldierStats.RecruitCost(type).Where(c => c.amount > 0).Select(c => c.type).ToArray(), $"A run's {type} costs more than gold.");
            }
            Assert.IsTrue(army.TryRecruit(SoldierType.Spearman, barracks.transform.position), "The spearman was not hired.");
            var spearman = SoldierUnit.All.First(u => u != null && u.Type == SoldierType.Spearman);
            var post = mode.Garrison.PostOf(SoldierType.Spearman);
            for (var t = 0f; t < 15f && Vector3.Distance(Flat(spearman.transform.position), Flat(post)) > 2f; t += Time.deltaTime) yield return null;
            Assert.Less(Vector3.Distance(Flat(spearman.Group.HoldPosition), Flat(post)), 0.1f, "The hired spearman's group was not sent to stand before the castle.");
            Assert.Less(Vector3.Distance(Flat(spearman.transform.position), Flat(post)), 5f, "The hired spearman did not go to stand before the castle.");

            var orc = OrcRaidManager.Instance.SpawnOrcOfKind(castle + new Vector3(12f, 0f, 0f), 1, OrcKind.Raider);
            orc.Toughen(100f);
            for (var t = 0f; t < 2f && spearman.Group.AttackTarget != (IDamageTarget)orc; t += Time.deltaTime) yield return null;
            Assert.AreSame(orc, spearman.Group.AttackTarget, "The soldiers did not go for an orc near the castle.");

            spearman.TakeDamage(1);
            Assert.IsNotNull(spearman.HealthBar, "A hurt soldier shows no health over his head.");
            Assert.IsTrue(spearman.HealthBar.gameObject.activeSelf);
            orc.TakeDamage(orc.MaxHealth);
        }

        /// <summary>A hero who falls goes down as a figure, as an orc does, and is gone until he rises (the user, 2026-10-23: the Queen stood and vanished).</summary>
        [UnityTest]
        public IEnumerator AFallenHero_GoesDown_AsAFigure()
        {
            var king = InvasionMode.Instance.King;
            var corpses = Object.FindObjectsByType<OrcCorpse>(FindObjectsSortMode.None).Length;
            king.Unit.TakeDamage(king.Unit.MaxHealth);
            yield return null;
            Assert.IsTrue(king.IsFallen);
            var fallen = Object.FindObjectsByType<OrcCorpse>(FindObjectsSortMode.None);
            Assert.Greater(fallen.Length, corpses, "The King fell with no figure going down.");
            Assert.IsTrue(fallen.Any(c => c.GetComponentsInChildren<Renderer>().Any(r => r.enabled)), "The King's falling figure is not drawn.");
            Assert.IsFalse(king.GetComponentsInChildren<Renderer>().Any(r => r.enabled), "The fallen King himself still stands there.");
        }

        /// <summary>A fence upgraded past level 1 is a stone fence, not the wooden one with a disc over it (the user, 2026-10-23).</summary>
        [Test]
        public void AnUpgradedFence_IsStone()
        {
            var fence = PlaytestWorld.Place(PlaytestWorld.Building("Fence"), PlaytestWorld.FindFreeAreaNearTheMiddle(Vector2Int.one));
            fence.SetLevel(2);
            var shown = fence.GetComponentsInChildren<Renderer>().Where(r => r.enabled && r.gameObject.activeInHierarchy).ToList();
            Assert.IsTrue(shown.Any(r => r.transform.parent != null && r.transform.parent.name.StartsWith("Stone")), "A level 2 fence does not show the stone pieces.");
            Assert.IsFalse(shown.Any(r => r.transform.parent != null && r.transform.parent.name == "Straight"), "A level 2 fence still shows the wooden one.");
            Assert.IsFalse(fence.transform.Cast<Transform>().Any(c => c.name == "LevelMarker" && c.gameObject.activeSelf), "A stone fence still has the level disc over it.");
        }

        /// <summary>
        /// An orc after someone inside a fence does not walk in at its gate (the user, 2026-10-23):
        /// for an orc a gate is a wall here as on the way to the castle, and it breaks in instead.
        /// </summary>
        [UnityTest]
        public IEnumerator AnOrc_DoesNotWalkInAtAGate_ItBreaksIn()
        {
            var grid = GridManager.Instance;
            var castle = InvasionMode.Instance.Castle.transform.position;
            // a ring two cells out, so an orc at it cannot reach over to whoever stands in the middle
            var centre = PlaytestWorld.FindFreeAreaNear(grid.WorldToCell(castle + new Vector3(-10f, 0f, -4f)) - 2 * Vector2Int.one, new Vector2Int(5, 5)) + 2 * Vector2Int.one;
            var ring = new System.Collections.Generic.List<BuildingInstance>();
            for (var dx = -2; dx <= 2; dx++)
            for (var dz = -2; dz <= 2; dz++)
            {
                if (Mathf.Abs(dx) < 2 && Mathf.Abs(dz) < 2) continue;
                var id = dx == 2 && dz == 0 ? "Gate" : "Fence";
                ring.Add(PlaytestWorld.Place(PlaytestWorld.Building(id), centre + new Vector2Int(dx, dz), id == "Gate" ? 1 : 0));
            }
            yield return new WaitForSeconds(2f);   // the gate's area and the carves land a moment later
            var inside = grid.GetFootprintCenterWorld(centre, Vector2Int.one);
            var outside = inside + Vector3.right * 5f;
            Assert.IsTrue(NavMesh.SamplePosition(outside, out var start, 2f, NavAreas.WithoutGates));
            var route = new NavRoute { AreaMask = NavAreas.WithoutGates };
            route.SetDestination(start.position, inside, straightLineFallback: false);
            Assert.IsTrue(route.Blocked, "An orc's route walks in at the gate.");

            // an archer inside shoots it; it comes for him (the King, who would come out to it, is away)
            InvasionMode.Instance.King.gameObject.SetActive(false);
            ArmyManager.Instance.SpawnSummons(SoldierType.Archer, inside, 1, 1, 60f);
            var archer = SoldierUnit.All.Where(u => u != null && u.Type == SoldierType.Archer).OrderBy(u => (u.transform.position - inside).sqrMagnitude).First();
            var orc = OrcRaidManager.Instance.SpawnOrcOfKind(start.position, 1, OrcKind.Raider);
            orc.Toughen(1000f);
            var health = ring.Sum(b => b.CurrentHealth);
            var walkedIn = false;
            for (var t = 0f; t < 20f && ring.Sum(b => b != null ? b.CurrentHealth : 0) == health; t += Time.deltaTime)
            {
                if (Vector3.Distance(Flat(orc.transform.position), Flat(inside)) < 1.5f) walkedIn = true;
                yield return null;
            }
            Assert.IsFalse(walkedIn, "The orc walked in at the gate.");
            Assert.Less(ring.Sum(b => b != null ? b.CurrentHealth : 0), health, "The orc did not break in -- it stood outside.");
            orc.TakeDamage(orc.MaxHealth);
            if (archer != null) archer.TakeDamage(archer.MaxHealth);
        }

        /// <summary>
        /// A wave orc keeps to its portal's way (the user, 2026-10-24: walled off, the waves went
        /// round to another causeway): a long wall across the way is broken where the way crosses
        /// it, not walked round, and the orc then walks on through the gap it made (the user saw
        /// orcs ignore a gap and break the pieces beside it).
        /// </summary>
        [UnityTest]
        public IEnumerator AWaveOrc_KeepsToItsWay_BreaksTheWallAcrossIt_AndGoesThroughTheGap()
        {
            var grid = GridManager.Instance;
            var castle = InvasionMode.Instance.Castle.transform.position;
            InvasionMode.Instance.King.gameObject.SetActive(false);
            foreach (var man in CitizenVisualsManager.Instance.AllAgents.ToList()) if (man != null) man.gameObject.SetActive(false);
            foreach (var house in InvasionMode.Instance.Houses) if (house != null) Object.Destroy(house.gameObject);

            // a wall of 17 pieces: going round it is ten metres longer than through it
            const int pieces = 17;
            // open ground for it and six cells either side (the island is too small: out beyond the river)
            var block = PlaytestWorld.FindFreeAreaNearTheMiddle(new Vector2Int(pieces, 13));
            Assert.GreaterOrEqual(block.x, 0, "No open ground on the map for the wall.");
            var origin = block + new Vector2Int(0, 6);
            var middle = grid.GetFootprintCenterWorld(origin + new Vector2Int(pieces / 2, 0), Vector2Int.one);
            Assert.IsTrue(NavMesh.SamplePosition(middle - Vector3.forward * 6f, out var south, 2f, NavAreas.WithoutGates), "No ground south of the wall.");
            Assert.IsTrue(NavMesh.SamplePosition(middle + Vector3.forward * 6f, out var north, 2f, NavAreas.WithoutGates), "No ground north of the wall.");
            var lane = OrcLane.Lay(south.position, north.position, 2f);
            Assert.IsNotNull(lane);
            Assert.Less(lane.Length, 14f, "The test's way does not run straight across where the wall goes.");

            var wall = new System.Collections.Generic.List<BuildingInstance>();
            for (var i = 0; i < pieces; i++) wall.Add(PlaytestWorld.Place(PlaytestWorld.Building("Fence"), origin + new Vector2Int(i, 0)));
            yield return new WaitForSeconds(2f);   // the carves land a moment later
            // a run's fence is sturdy: every piece left one blow from falling, so which one goes is the test, not how long
            foreach (var piece in wall) piece.TryDamage(piece.CurrentHealth - 1);

            var orc = OrcRaidManager.Instance.SpawnOrcOfKind(south.position, 1, OrcKind.Raider);
            orc.Toughen(1000f);   // the castle's shots must not end the test first
            orc.FollowLane(lane);
            var strayed = 0f;
            var strayedAt = "";
            var through = false;
            for (var t = 0f; t < 30f && !through; t += Time.deltaTime)
            {
                if (orc == null) break;
                var alongNow = lane.Project(orc.transform.position, out var off);
                if (off > strayed) strayedAt = $"{alongNow:0.0} m down the way of {lane.Length:0.0}, t {t:0.0} s, target {(orc.Target != null ? orc.Target.name : "none")}";
                strayed = Mathf.Max(strayed, off);
                through = alongNow >= lane.Length / 2f + 2f;   // past the wall (at the way's end it turns on the castle)
                yield return null;
            }
            var broken = wall.Where(w => w == null || w.CurrentHealth <= 0).Count();
            Assert.IsTrue(through, $"The orc did not get through the wall along its way ({broken} pieces broken, {strayed:0.0} m off the way at most).");
            Assert.Less(strayed, 3f, $"The orc left its way to go round the wall (at {strayedAt}).");
            Assert.LessOrEqual(broken, 2, "The orc broke more of the wall than the way through it.");
            if (orc != null) orc.TakeDamage(orc.MaxHealth);
        }

        /// <summary>
        /// The raiding party (the user, 2026-10-24): with every third wave 5 raiders and 2 throwers
        /// go for the player's buildings beyond the base, never for what stands in it, and walk back
        /// to their camp once nothing out there is left. None goes out while nothing is out there.
        /// </summary>
        [UnityTest]
        public IEnumerator TheRaidingParty_BreaksWhatIsBeyondTheBase_AndGoesHome()
        {
            var config = BalanceConfig.Instance;
            var mode = InvasionMode.Instance;
            var waves = mode.Waves;
            var castle = mode.Castle.transform.position;
            var radius = config.InvasionRaidPartyBaseRadius;
            Assert.IsTrue(InvasionWaves.IsRaidPartyWave(config.InvasionRaidPartyEvery, config.InvasionRaidPartyEvery));
            Assert.IsFalse(InvasionWaves.IsRaidPartyWave(config.InvasionRaidPartyEvery - 1, config.InvasionRaidPartyEvery));

            waves.SendRaidParty(1);
            Assert.AreEqual(0, waves.RaidParty.Count, "A party went out with nothing beyond the base to break.");

            mode.King.gameObject.SetActive(false);
            foreach (var man in CitizenVisualsManager.Instance.AllAgents.ToList()) if (man != null) man.gameObject.SetActive(false);
            var grid = GridManager.Instance;
            var data = PlaytestWorld.Building("Sawmill");
            var outside = PlaytestWorld.Place(data, PlaytestWorld.FindFreeAreaNear(grid.WorldToCell(castle + new Vector3(radius + 10f, 0f, 0f)), data.footprintSize));
            Assert.Greater(Flat(outside.transform.position - castle).magnitude, radius, "The test's sawmill stands inside the base.");
            outside.TryDamage(outside.CurrentHealth - 1);

            waves.SendRaidParty(1);
            Assert.AreEqual(config.InvasionRaidPartyRaiders + config.InvasionRaidPartyThrowers, waves.RaidParty.Count);
            Assert.AreEqual(config.InvasionRaidPartyThrowers, waves.RaidParty.Count(o => o.Kind == OrcKind.Thrower));
            Assert.IsTrue(waves.RaidParty.All(o => o.IsRaiding && o.Lane == null));
            foreach (var orc in waves.RaidParty) orc.Toughen(1000f);   // the towers on the way must not end the test first
            var alerts = Object.FindAnyObjectByType<UI.MinimapAttackAlerts>();
            Assert.IsNotNull(alerts, "No minimap alerts in a run.");
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsTrue(alerts.PartyMarkShown, "The minimap shows no mark for the raiding party.");

            var hitInside = false;
            Time.timeScale = 4f;
            for (var t = 0f; t < 40f && outside != null && outside.CurrentHealth > 0; t += Time.unscaledDeltaTime)
            {
                foreach (var orc in waves.RaidParty)
                    if (orc != null && orc.Target != null && !orc.Target.Data.isGate && Flat(orc.Target.transform.position - castle).magnitude <= radius) hitInside = true;
                yield return null;
            }
            Assert.IsTrue(outside == null || outside.CurrentHealth <= 0, "The party did not break the sawmill beyond the base.");
            for (var t = 0f; t < 40f && waves.RaidParty.Any(o => o != null); t += Time.unscaledDeltaTime) yield return null;
            Time.timeScale = 1f;
            Assert.IsFalse(hitInside, "The party went for a building inside the base.");
            Assert.IsFalse(waves.RaidParty.Any(o => o != null), "The party did not go home once nothing beyond the base was left.");
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsFalse(alerts.PartyMarkShown, "The raiding party's mark stayed on the minimap after it went home.");
        }

        /// <summary>The Дом лекаря in a run (the user, 2026-10-24): manned by one, it slowly heals the player's wounded in its radius; empty, it heals nobody.</summary>
        [UnityTest]
        public IEnumerator TheHealersHouse_HealsTheWoundedAround_OnceManned()
        {
            var data = PlaytestWorld.Building("HealerHouse");
            Assert.AreEqual(1, data.LevelStats(1).maxWorkers, "A run's healer takes more than one worker.");
            Assert.Greater(BalanceConfig.Instance.InvasionHealerHealShareAt(3), BalanceConfig.Instance.InvasionHealerHealShareAt(1), "A higher healer heals no better.");
            var healer = PlaytestWorld.Place(data, PlaytestWorld.FindFreeAreaNearTheMiddle(data.footprintSize));
            var near = healer.transform.position + Vector3.right * 4f;
            Assert.IsTrue(NavMesh.SamplePosition(near, out var hit, 3f, NavMesh.AllAreas));
            ArmyManager.Instance.SpawnSummons(SoldierType.Archer, hit.position, 1, 1, 60f);
            var archer = SoldierUnit.All.Where(u => u != null && u.Type == SoldierType.Archer).OrderBy(u => (u.transform.position - hit.position).sqrMagnitude).First();
            archer.SetCurrentHealth(archer.MaxHealth / 3);
            var wounded = archer.CurrentHealth;

            yield return new WaitForSeconds(2.5f);
            Assert.AreEqual(wounded, archer.CurrentHealth, "An empty healer's house healed.");

            healer.GetComponent<ProductionBuilding>().SetAssignedWorkers(1);
            yield return new WaitForSeconds(2.5f);
            Assert.Greater(archer.CurrentHealth, wounded, "A manned healer's house did not heal a wounded archer beside it.");
            archer.TakeDamage(archer.MaxHealth);
        }

        /// <summary>
        /// The build menu's cards hand the placer the RUN's buildings (the user, 2026-10-23: a third
        /// sawmill went down -- the cards are wired to the assets, which have no run limit and the
        /// sandbox's price).
        /// </summary>
        /// <summary>A run's price may ask for gold (the invasion tab's cost_gold, the user 2026-10-24) -- paid in the run's coins, as an upgrade's is; its repair never does.</summary>
        [Test]
        public void ARunsPrice_TakesGold_ButItsRepairDoesNot()
        {
            var data = ScriptableObject.CreateInstance<BuildingData>();
            data.cost = new System.Collections.Generic.List<ResourceAmount>
            {
                new ResourceAmount { type = ResourceType.Wood, amount = 10 },
                new ResourceAmount { type = ResourceType.Gold, amount = 7 },   // what the sheet's cost_gold reads as
                new ResourceAmount { type = ResourceType.Iron, amount = 4 },
            };
            var build = InvasionRules.BuildCost(data);
            Assert.AreEqual(2, build.Count, "A run's price kept something besides wood, stone and the run's money.");
            Assert.AreEqual(11, build.First(c => c.type == InvasionRules.Money).amount, "A run's price did not ask its gold (and the rest) in the run's money.");
            Assert.IsFalse(InvasionRules.RepairCost(data, 1, 1f).Any(c => c.type == InvasionRules.Money), "Mending asks for gold.");
            Object.DestroyImmediate(data);
        }

        [Test]
        public void TheBuildMenu_PicksUpTheRunsBuildings()
        {
            var placer = Object.FindAnyObjectByType<BuildingPlacer>();
            foreach (var id in new[] { "Sawmill", "Quarry", "Tower" })
            {
                var card = Object.FindObjectsByType<UI.HotbarButtonHandler>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(c => c.Building != null && c.Building.buildingName == id);
                Assert.IsNotNull(card, $"No {id} card in the build menu.");
                Assert.AreSame(PlaytestWorld.Building(id), card.Building, $"The {id} card shows the sandbox's building, not the run's.");
                card.SelectThisBuilding();
                Assert.AreSame(PlaytestWorld.Building(id), placer.SelectedBuilding, $"The {id} card puts the sandbox's building in hand.");
                placer.ClearSelection();
            }
            Assert.AreEqual(BalanceConfig.Run.Building("Sawmill").maxCount, PlaytestWorld.Building("Sawmill").maxCount);

            // and their price chips say the RUN's price (the invasion tab), gold included, not the sandbox's
            foreach (var card in Object.FindObjectsByType<UI.HotbarButtonHandler>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (card.Building == null || !InvasionRules.Allows(card.Building)) continue;
                var cost = BuildingPlacer.CostOf(card.Building);
                var chips = card.GetComponentsInChildren<UnityEngine.UI.Text>(true).Where(t => t.name.StartsWith("Amount_") && t.gameObject.activeSelf).ToList();
                Assert.AreEqual(cost.Count, chips.Count, $"The {card.Building.buildingName} card shows {chips.Count} prices for a run price of {cost.Count}.");
                foreach (var c in cost)
                {
                    var chip = chips.FirstOrDefault(t => t.name == $"Amount_{c.type}");
                    Assert.IsNotNull(chip, $"The {card.Building.buildingName} card has no {c.type} price.");
                    Assert.AreEqual(c.amount.ToString(), chip.text, $"The {card.Building.buildingName} card's {c.type} is not the run's price.");
                }
            }
        }

        /// <summary>
        /// An orc fighting hand to hand hits a building only standing at its wall (the user,
        /// 2026-10-23: one hit a tower over the fence round it) and never walks in at a gate on
        /// its way: it breaks the fence or the gate first. Only a thrower hits over a wall.
        /// </summary>
        [UnityTest]
        public IEnumerator AnOrc_BreaksTheWallBeforeTheTowerBehindIt()
        {
            var grid = GridManager.Instance;
            var castle = InvasionMode.Instance.Castle.transform.position;
            InvasionMode.Instance.King.gameObject.SetActive(false);
            // nobody about for it to go for instead: the King away, the islanders indoors
            foreach (var man in CitizenVisualsManager.Instance.AllAgents.ToList()) if (man != null) man.gameObject.SetActive(false);
            // and no other building near enough for it to turn on first
            foreach (var house in InvasionMode.Instance.Houses) if (house != null) Object.Destroy(house.gameObject);
            var centre = PlaytestWorld.FindFreeAreaNear(grid.WorldToCell(castle + new Vector3(-11f, 0f, 3f)) - Vector2Int.one, new Vector2Int(3, 3)) + Vector2Int.one;
            var tower = PlaytestWorld.Place(PlaytestWorld.Building("Tower"), centre);
            var ring = new System.Collections.Generic.List<BuildingInstance>();
            for (var dx = -1; dx <= 1; dx++)
            for (var dz = -1; dz <= 1; dz++)
            {
                if (dx == 0 && dz == 0) continue;
                var id = dx == 1 && dz == 0 ? "Gate" : "Fence";
                ring.Add(PlaytestWorld.Place(PlaytestWorld.Building(id), centre + new Vector2Int(dx, dz), id == "Gate" ? 1 : 0));
            }
            yield return new WaitForSeconds(2f);   // the gate's area and the carves land a moment later

            var gateAt = Flat(ring.First(b => b.Data.isGate).transform.position);
            Assert.IsTrue(NavMesh.SamplePosition(tower.transform.position + Vector3.right * 4.5f, out var start, 2f, NavAreas.WithoutGates));
            var orc = OrcRaidManager.Instance.SpawnOrcOfKind(start.position, 1, OrcKind.Raider);
            orc.Toughen(1000f);
            var towerHealth = tower.CurrentHealth;
            var wallHealth = ring.Sum(b => b.CurrentHealth);
            var onTheGate = false;
            for (var t = 0f; t < 25f && ring.Sum(b => b != null ? b.CurrentHealth : 0) == wallHealth; t += Time.deltaTime)
            {
                Assert.AreEqual(towerHealth, tower.CurrentHealth, "The orc hit the tower over its fence.");
                if (Vector3.Distance(Flat(orc.transform.position), gateAt) < 0.3f) onTheGate = true;
                yield return null;
            }
            Assert.IsFalse(onTheGate, "The orc walked onto the gate.");
            Assert.Less(ring.Sum(b => b != null ? b.CurrentHealth : 0), wallHealth, "The orc did not break the wall in its way -- it stood there.");
            orc.TakeDamage(orc.MaxHealth);
        }

        /// <summary>
        /// A fence put up between an orc and the tower it is hitting takes the next blow: the tower
        /// is out of its reach now (the user, 2026-10-25: the orcs hit on through the new fence).
        /// </summary>
        [UnityTest]
        public IEnumerator AFenceLaidBetween_TakesTheOrcsBlowsOffTheTower()
        {
            var grid = GridManager.Instance;
            var castle = InvasionMode.Instance.Castle.transform.position;
            InvasionMode.Instance.King.gameObject.SetActive(false);
            foreach (var man in CitizenVisualsManager.Instance.AllAgents.ToList()) if (man != null) man.gameObject.SetActive(false);
            foreach (var house in InvasionMode.Instance.Houses) if (house != null) Object.Destroy(house.gameObject);
            var centre = PlaytestWorld.FindFreeAreaNear(grid.WorldToCell(castle + new Vector3(-11f, 0f, 3f)) - Vector2Int.one, new Vector2Int(3, 3)) + Vector2Int.one;
            var tower = PlaytestWorld.Place(PlaytestWorld.Building("Tower"), centre);
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(NavMesh.SamplePosition(tower.transform.position + Vector3.right * 3f, out var start, 2f, NavAreas.WithoutGates));
            var orc = OrcRaidManager.Instance.SpawnOrcOfKind(start.position, 1, OrcKind.Raider);
            orc.Toughen(1000f);
            var full = tower.CurrentHealth;
            for (var t = 0f; t < 15f && tower.CurrentHealth == full; t += Time.deltaTime) yield return null;
            Assert.Less(tower.CurrentHealth, full, "The orc never got to the tower.");

            // the fence goes in on the orc's side of the tower, where it stands
            var side = grid.WorldToCell(orc.transform.position) - centre;
            var between = centre + new Vector2Int(System.Math.Sign(side.x), side.x == 0 ? System.Math.Sign(side.y) : 0);
            var fence = PlaytestWorld.Place(PlaytestWorld.Building("Fence"), between);
            var hit = tower.CurrentHealth;
            var fenceFull = fence.CurrentHealth;
            for (var t = 0f; t < 6f && fence != null && fence.CurrentHealth == fenceFull; t += Time.deltaTime) yield return null;
            Assert.IsTrue(fence == null || fence.CurrentHealth < fenceFull, "The orc did not turn on the fence put in its way.");
            Assert.AreEqual(hit, tower.CurrentHealth, "The orc hit the tower through the fence.");
            orc.TakeDamage(orc.MaxHealth);
        }

        /// <summary>
        /// A place a worker was lost from without the player's say is taken again by the next idle
        /// islander -- the healer's house as the sawmill (the user, 2026-10-25).
        /// </summary>
        [UnityTest]
        public IEnumerator AWorkplaceThatLostItsMan_GetsTheNextOne()
        {
            var castle = InvasionMode.Instance.Castle.transform.position;
            var cell = PlaytestWorld.FindFreeAreaNear(GridManager.Instance.WorldToCell(castle + new Vector3(-9f, 0f, 3f)), new Vector2Int(3, 3));
            var healer = PlaytestWorld.Place(PlaytestWorld.Building("HealerHouse"), cell).GetComponent<CityBuilder.Citizens.ProductionBuilding>();
            yield return null;
            var people = CitizenManager.Instance;
            Assert.IsTrue(healer.TryAssignWorker(), "Nobody idle to put in the healer's house.");
            // the whole island lost: everyone laid off, the places still wanted
            var everyone = people.TotalPopulation;
            people.KillCitizens(everyone);
            Assert.AreEqual(0, healer.AssignedWorkers);
            Assert.AreEqual(1, healer.WantedWorkers, "Losing its man made the house stop wanting one.");
            people.AddCitizens(everyone);
            Assert.AreEqual(1, healer.AssignedWorkers, "The islanders back did not step into the healer's house.");
            // one the player takes off is not given back
            healer.TryUnassignWorker();
            people.AddCitizens(1);
            Assert.AreEqual(0, healer.AssignedWorkers, "A worker the player took off came back by himself.");
            yield return null;
        }

        /// <summary>A building's reach is a round disc: the healer's house shows its service radius, a sawmill its yard -- the photographs show both.</summary>
        [UnityTest]
        public IEnumerator Photograph_TheReachDiscs()
        {
            var castle = InvasionMode.Instance.Castle.transform.position;
            var grid = GridManager.Instance;
            var healer = PlaytestWorld.Place(PlaytestWorld.Building("HealerHouse"), PlaytestWorld.FindFreeAreaNear(grid.WorldToCell(castle + new Vector3(-9f, 0f, 3f)), new Vector2Int(3, 3)));
            var sawmill = Object.FindObjectsByType<BuildingInstance>(FindObjectsInactive.Exclude).FirstOrDefault(b => b.Data != null && b.Data.buildingName == "Sawmill")
                          ?? PlaytestWorld.Place(PlaytestWorld.Building("Sawmill"), PlaytestWorld.FindFreeAreaNear(grid.WorldToCell(castle + new Vector3(9f, 0f, -6f)), new Vector2Int(3, 3)));
            yield return null;
            Assert.Greater(HarvestRadiusOverlay.RadiusOf(healer.Data, 1), 0, "The healer's house has no reach to show.");
            CityBuilder.UI.BuildingFocus.Instance.Select(healer);
            yield return new WaitForSeconds(0.6f);
            if (PlaytestCapture.Available) yield return PlaytestCapture.Shoot("reach-healer", healer.transform.position, 26f, 55f, 20f, 45f);
            CityBuilder.UI.BuildingFocus.Instance.Select(sawmill);
            yield return new WaitForSeconds(0.6f);
            if (PlaytestCapture.Available) yield return PlaytestCapture.Shoot("reach-sawmill", sawmill.transform.position, 30f, 55f, 20f, 45f);
            CityBuilder.UI.BuildingFocus.Instance.Clear();
        }

        /// <summary>A house upgraded holds one more islander a level (the invasion tab's housing_capacity per level; the user, 2026-10-25), and makes him.</summary>
        [UnityTest]
        public IEnumerator AnUpgradedHouse_HoldsOneMore()
        {
            var mode = InvasionMode.Instance;
            var house = mode.Houses[0];
            var places = mode.People.Places;
            var levels = BalanceConfig.Run.Building("Hovel").levels;
            Assert.AreEqual(levels[0].housingCapacity + 1, levels[1].housingCapacity, "Level 2 of a house does not hold one more in the sheet.");
            house.SetLevel(2);
            yield return null;
            Assert.AreEqual(places + 1, mode.People.Places, "The upgraded house holds no more people.");
            Assert.AreEqual(1, mode.People.Owed, "The house does not make its new islander.");
        }

        /// <summary>
        /// A unit wedged inside buildings put up round it passes through to free ground and walks on
        /// (the user, 2026-10-25: the King stood stuck between two buildings set against the castle).
        /// </summary>
        [UnityTest]
        public IEnumerator AUnitWalledInByBuildings_WalksOut()
        {
            var king = InvasionMode.Instance.King;
            var grid = GridManager.Instance;
            var castle = InvasionMode.Instance.Castle.transform.position;
            // two barracks side by side, the King in the seam between them, as if they went up round him
            var cell = PlaytestWorld.FindFreeAreaNear(grid.WorldToCell(castle + new Vector3(-12f, 0f, 6f)), new Vector2Int(6, 3));
            var west = PlaytestWorld.Place(PlaytestWorld.Building("Barracks"), cell);
            var east = PlaytestWorld.Place(PlaytestWorld.Building("Barracks"), cell + new Vector2Int(3, 0));
            yield return new WaitForSeconds(1f);   // the carves land
            var seam = (west.transform.position + east.transform.position) * 0.5f;
            king.transform.position = new Vector3(seam.x, king.transform.position.y, seam.z);
            Physics.SyncTransforms();
            Assert.IsFalse(NavMesh.SamplePosition(seam, out _, 0.25f, NavMesh.AllAreas), "The seam is walkable: the fixture does not wedge him.");
            // he guards the castle: walking home is what he tries, and the seam holds him
            for (var t = 0f; t < 10f && !NavMesh.SamplePosition(king.transform.position, out _, 0.25f, NavMesh.AllAreas); t += Time.deltaTime) yield return null;
            Assert.IsTrue(NavMesh.SamplePosition(king.transform.position, out _, 0.25f, NavMesh.AllAreas), "The King wedged between two buildings never got out.");
            yield return new WaitForSeconds(0.5f);
            var controller = king.GetComponent<CharacterController>();
            Assert.IsTrue(controller == null || controller.enabled, "The King was left with his collider off.");
        }

        /// <summary>
        /// A fence meeting the Barbican's end from the side gets a stone arm from that end to it, as
        /// fence pieces join (the user, 2026-10-25); the arm goes when the fence does.
        /// </summary>
        [UnityTest]
        public IEnumerator TheBarbicansEnd_TurnsTowardAFenceBesideIt()
        {
            var grid = GridManager.Instance;
            var castle = InvasionMode.Instance.Castle.transform.position;
            var cell = PlaytestWorld.FindFreeAreaNear(grid.WorldToCell(castle + new Vector3(-11f, 0f, 4f)), new Vector2Int(3, 3)) + Vector2Int.right;
            var barbican = PlaytestWorld.Place(PlaytestWorld.Building("FortifiedGate"), cell);
            yield return null;
            var arms = barbican.GetComponent<GateArms>();
            Assert.IsNotNull(arms, "The Barbican has no side arms.");
            GameObject Arm(string name) => barbican.GetComponentsInChildren<Transform>(true).First(t => t.name == name).gameObject;
            Assert.IsFalse(Arm("ArmNE").activeSelf || Arm("ArmNW").activeSelf || Arm("ArmSE").activeSelf || Arm("ArmSW").activeSelf, "An arm shows with no fence beside the Barbican.");

            // the end cells, read the way GateArms reads them: which of them is north depends on the placement
            var north = arms.EndCell(0);
            var fence = PlaytestWorld.Place(PlaytestWorld.Building("Fence"), north + Vector2Int.right);
            var tail = PlaytestWorld.Place(PlaytestWorld.Building("Fence"), north + Vector2Int.right * 2);
            yield return null;
            Assert.IsTrue(Arm("ArmNE").activeSelf, "The Barbican's end did not turn toward the fence beside it.");
            Assert.IsFalse(Arm("ArmNW").activeSelf || Arm("ArmSE").activeSelf || Arm("ArmSW").activeSelf, "An arm shows toward no fence.");
            if (PlaytestCapture.Available) yield return PlaytestCapture.Shoot("barbican-arm", grid.GetFootprintCenterWorld(north, Vector2Int.one), 7f, 60f, 30f, 40f);

            Object.DestroyImmediate(tail.gameObject);
            Object.Destroy(fence.gameObject);
            yield return null;
            Assert.IsFalse(Arm("ArmNE").activeSelf, "The arm stayed when the fence went.");
        }

        /// <summary>A hired soldier steps out where he can walk to the castle, not into the gap between the barracks and a house (the user, 2026-10-23).</summary>
        [UnityTest]
        public IEnumerator AHiredSoldier_StepsOutOnOpenGround()
        {
            var castle = InvasionMode.Instance.Castle.transform.position;
            ResourceManager.Instance.SetAmount(InvasionRules.Money, 1000);
            var data = PlaytestWorld.Building("Barracks");
            var cell = PlaytestWorld.FindFreeAreaNear(GridManager.Instance.WorldToCell(castle + new Vector3(-9f, 0f, 3f)), new Vector2Int(5, 3));
            var barracks = PlaytestWorld.Place(data, cell);
            // a house hard against its east wall
            PlaytestWorld.Place(PlaytestWorld.Building("Hovel"), cell + new Vector2Int(3, 0));
            yield return new WaitForSeconds(1f);
            for (var i = 0; i < 3; i++) Assert.IsTrue(ArmyManager.Instance.TryRecruit(SoldierType.Militia, barracks.transform.position), "A militiaman was not hired.");
            Assert.IsTrue(NavMesh.SamplePosition(castle, out var goal, 5f, NavMesh.AllAreas));
            foreach (var soldier in SoldierUnit.All.Where(u => u != null && u.Type == SoldierType.Militia))
            {
                Assert.IsTrue(NavMesh.SamplePosition(soldier.transform.position, out var at, 0.7f, NavMesh.AllAreas), "A recruit stands off the walkable ground.");
                var path = new NavMeshPath();
                NavMesh.CalculatePath(at.position, goal.position, NavMesh.AllAreas, path);
                Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status, "A recruit was put where he cannot walk to the castle.");
            }
        }

        /// <summary>More stone on the run's map (the user, 2026-10-23: "too little -- scatter it over the map"): three patches on the island (11 boulders on 2026-10-23), a dozen out beyond the river (42).</summary>
        [Test]
        public void TheRunsMap_HasStoneAllOver()
        {
            var castle = InvasionMode.Instance.Castle.transform.position;
            var rocks = ResourceNode.All.Where(n => n != null && n.ResourceType == ResourceType.Stone).ToList();
            var island = rocks.Count(n => Vector3.Distance(Flat(n.transform.position), Flat(castle)) < 16f);
            var beyond = rocks.Count - island;
            Assert.GreaterOrEqual(island, 6, "The island has too little stone.");
            Assert.GreaterOrEqual(beyond, 30, "The map beyond the river has too little stone.");
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

        /// <summary>
        /// The King's four skills (the user, 2026-10-23): they open at their hero levels, work only
        /// when used, then recharge; Лечение gives back a share of his health, Неуязвимость stops
        /// damage, Ярость makes him strike harder, Благословение makes the castle unhurtable.
        /// </summary>
        [Test]
        public void TheKingsSkills_OpenWithLevels_WorkAndRecharge()
        {
            var king = InvasionMode.Instance.King;
            var skills = king.GetComponent<HeroSkills>();
            Assert.IsNotNull(skills, "The King has no skills.");
            CollectionAssert.AreEqual(new[] { HeroSkills.KingHeal, HeroSkills.KingShield, HeroSkills.KingRage, HeroSkills.KingBlessing },
                skills.Slots.Select(slot => slot.Balance.id).ToArray(), "The King's skills are not the four agreed, in the order they open.");
            Assert.AreEqual(1, king.Level);
            Assert.IsTrue(skills.IsOpen(0), "Лечение is not open at level 1.");
            Assert.IsFalse(skills.Use(1), "Неуязвимость worked before its level.");

            var unit = king.Unit;
            unit.SetCurrentHealth(unit.MaxHealth / 4);
            var before = unit.CurrentHealth;
            Assert.IsTrue(skills.Use(0), "Лечение could not be used.");
            var heal = Mathf.RoundToInt(unit.MaxHealth * skills.Slots[0].Balance.PowerAt(king.Level));
            Assert.AreEqual(Mathf.Min(unit.MaxHealth, before + heal), unit.CurrentHealth, "Лечение did not give back its share of health.");
            Assert.IsFalse(skills.Use(0), "Лечение worked again without recharging.");

            while (king.Level < 15) king.AddExperience(Hero.XpForLevel(king.Level));
            for (var i = 1; i < 4; i++) Assert.IsTrue(skills.IsReady(i), $"Skill {i + 1} is not ready at level 15.");

            Assert.IsTrue(skills.Use(1));
            var health = unit.CurrentHealth;
            unit.TakeDamage(10);
            Assert.AreEqual(health, unit.CurrentHealth, "Неуязвимость let damage through.");

            Assert.IsTrue(skills.Use(2));
            Assert.Greater(unit.RageLeft, 0f, "Ярость did not take hold.");

            try
            {
                Assert.IsTrue(skills.Use(3));
                Assert.IsTrue(HeroBlessing.Active, "Благословение did not begin.");
                var castle = InvasionMode.Instance.Castle;
                var walls = castle.CurrentHealth;
                castle.TryDamage(50);
                Assert.AreEqual(walls, castle.CurrentHealth, "The castle was hurt under the Blessing.");
            }
            finally
            {
                HeroBlessing.End();
            }
        }

        /// <summary>The King in hand puts his skills where the build menu stands; let go, the menu is back (the user, 2026-10-23).</summary>
        [UnityTest]
        public IEnumerator TheKingInHand_ShowsHisSkills_InPlaceOfTheBuildMenu()
        {
            var king = InvasionMode.Instance.King;
            var bar = Object.FindAnyObjectByType<CityBuilder.UI.HeroSkillBar>();
            Assert.IsNotNull(bar, "No skill bar in the game.");
            ArmyManager.Instance.SelectGroup(king.Unit.Group);
            yield return null;
            yield return null;
            Assert.IsTrue(CityBuilder.UI.HeroSkillBar.Showing, "The King's skills did not come up.");
            Assert.IsNotNull(GameObject.Find("HeroSkillBar"), "The skill bar is not on screen.");
            Assert.IsNull(GameObject.Find("BuildingMenu"), "The build menu stayed beside the skills.");
            king.Unit.SetCurrentHealth(1);
            Assert.IsTrue(bar.Use(0), "A tap on Лечение did nothing.");
            Assert.Greater(king.Unit.CurrentHealth, 1);
            Assert.That(CityBuilder.UI.HeroSkillBar.Describe(bar.Current, 1), Does.Contain("5"), "The locked skill does not say its level.");

            ArmyManager.Instance.SelectGroup(null);
            yield return null;
            Assert.IsFalse(CityBuilder.UI.HeroSkillBar.Showing, "The skills stayed after the King was let go.");
        }

        /// <summary>Raises the castle to `level` at once (paid, its construction finished on the spot).</summary>
        private static void RaiseCastle(int level)
        {
            var castle = InvasionMode.Instance.Castle;
            while (castle.Level < level)
            {
                ResourceManager.Instance.Add(ResourceType.Wood, 5000);
                ResourceManager.Instance.Add(ResourceType.Stone, 5000);
                ResourceManager.Instance.Add(InvasionRules.Money, 5000);
                Assert.IsTrue(castle.TryUpgrade(), "The castle could not be upgraded.");
                if (castle.Site != null) castle.Site.Finish();
            }
        }

        private static Hero BuyHero(SoldierType type, int castleLevel)
        {
            RaiseCastle(castleLevel);
            ResourceManager.Instance.Add(InvasionRules.Money, 1000);
            Assert.AreEqual(InvasionMode.HeroOffer.Bought, InvasionMode.Instance.BuyHero(type), $"The {type} could not be bought.");
            return InvasionMode.Instance.HeroOf(type);
        }

        /// <summary>
        /// The Queen and the Captain are bought in the castle (the user, 2026-10-23): the Queen from
        /// its level 2, the Captain from 3, one of each, for their price in gold.
        /// </summary>
        [Test]
        public void Heroes_AreBoughtInTheCastle_ByItsLevel()
        {
            var mode = InvasionMode.Instance;
            var queen = BalanceConfig.Instance.Hero("queen");
            var captain = BalanceConfig.Instance.Hero("captain");
            Assert.AreEqual(InvasionMode.HeroOffer.Owned, mode.CanBuy(SoldierType.King), "The King is for sale.");
            Assert.AreEqual(InvasionMode.HeroOffer.CastleTooLow, mode.CanBuy(SoldierType.Queen), "The Queen is for sale at a level-1 castle.");

            RaiseCastle(queen.castleLevel);
            ResourceManager.Instance.Add(InvasionRules.Money, 1000);
            var gold = ResourceManager.Instance.GetAmount(InvasionRules.Money);
            Assert.AreEqual(InvasionMode.HeroOffer.CastleTooLow, mode.CanBuy(SoldierType.Captain), "The Captain came with the Queen's castle level.");
            Assert.AreEqual(InvasionMode.HeroOffer.Bought, mode.BuyHero(SoldierType.Queen));
            Assert.AreEqual(gold - queen.price, ResourceManager.Instance.GetAmount(InvasionRules.Money), "The Queen did not cost her price.");
            var hero = mode.HeroOf(SoldierType.Queen);
            Assert.IsNotNull(hero, "The bought Queen is not in the run.");
            Assert.IsTrue(hero.Unit.IsRanged, "The Queen does not shoot.");
            Assert.AreEqual(hero.MaxHealthAt(1), hero.Unit.MaxHealth, "The Queen's health is not the sheet's.");
            Assert.AreEqual(InvasionMode.HeroOffer.Owned, mode.BuyHero(SoldierType.Queen), "A second Queen could be bought.");

            RaiseCastle(captain.castleLevel);
            var spend = ResourceManager.Instance.GetAmount(InvasionRules.Money);
            ResourceManager.Instance.TrySpend(new[] { new ResourceAmount { type = InvasionRules.Money, amount = spend } });
            Assert.AreEqual(InvasionMode.HeroOffer.NotEnoughMoney, mode.BuyHero(SoldierType.Captain), "The Captain came for nothing.");
            ResourceManager.Instance.Add(InvasionRules.Money, captain.price);
            Assert.AreEqual(InvasionMode.HeroOffer.Bought, mode.BuyHero(SoldierType.Captain));
            Assert.AreEqual(3, mode.Heroes.Count);
        }

        /// <summary>The castle's card shows the three heroes; a tap on one the castle is too low for buys nothing (the user, 2026-10-23).</summary>
        [UnityTest]
        public IEnumerator TheCastleCard_ShowsTheHeroes_AndSellsThem()
        {
            var panel = Object.FindFirstObjectByType<CityBuilder.UI.BuildingInfoPanelController>(FindObjectsInactive.Include);
            var cards = Object.FindFirstObjectByType<CityBuilder.UI.CastleHeroCards>(FindObjectsInactive.Include);
            Assert.IsNotNull(cards, "No hero cards on the castle's card.");
            panel.Show(InvasionMode.Instance.Castle);
            yield return null;
            Assert.IsNotNull(GameObject.Find("CastleHeroCards"), "The castle's card shows no heroes.");
            Assert.AreEqual(InvasionMode.HeroOffer.CastleTooLow, cards.Buy(1), "The Queen's card sold her at a level-1 castle.");
            Assert.IsNull(InvasionMode.Instance.HeroOf(SoldierType.Queen));
            RaiseCastle(2);
            ResourceManager.Instance.Add(InvasionRules.Money, 1000);
            Assert.AreEqual(InvasionMode.HeroOffer.Bought, cards.Buy(1), "The Queen's card did not sell her.");
            yield return new WaitForSeconds(0.3f);
            Assert.IsNotNull(GameObject.Find("QueenPortrait"), "The bought Queen has no portrait.");
            if (PlaytestCapture.Available)
            {
                yield return PlaytestCapture.ShootWithUiAt("invasion-castle-heroes", 1600, 900, InvasionMode.Instance.Castle.transform.position, 18f, 52f, 0f);
            }
            panel.Close();
            yield return null;
            Assert.IsNull(GameObject.Find("CastleHeroCards"), "The hero cards stayed after the castle's card closed.");
        }

        /// <summary>Three heroes' portraits stand one under another down the right edge, none over another.</summary>
        [UnityTest]
        public IEnumerator TheHeroesPortraits_StackWithoutOverlapping()
        {
            BuyHero(SoldierType.Queen, 2);
            BuyHero(SoldierType.Captain, 3);
            yield return new WaitForSeconds(0.3f);
            var rects = new[] { "KingPortrait", "QueenPortrait", "CaptainPortrait" }.Select(n =>
            {
                var go = GameObject.Find(n);
                Assert.IsNotNull(go, $"{n} is not shown.");
                var corners = new Vector3[4];
                ((RectTransform)go.transform.Find("Portrait")).GetWorldCorners(corners);
                return new Rect(corners[0].x, corners[0].y, corners[2].x - corners[0].x, corners[2].y - corners[0].y);
            }).ToArray();
            for (var i = 0; i < rects.Length; i++)
            for (var j = i + 1; j < rects.Length; j++)
                Assert.IsFalse(rects[i].Overlaps(rects[j]), $"Portrait {i + 1} lies over portrait {j + 1}.");
            Assert.Greater(rects[0].y, rects[1].y, "The King's portrait is not on top.");
            if (PlaytestCapture.Available)
            {
                ArmyManager.Instance.SelectGroup(InvasionMode.Instance.HeroOf(SoldierType.Queen).Unit.Group);
                yield return null;
                yield return PlaytestCapture.ShootWithUiAt("invasion-three-heroes", 1600, 900, InvasionMode.Instance.King.transform.position, 14f, 50f, 0f);
                ArmyManager.Instance.SelectGroup(null);
            }
        }

        /// <summary>The Queen backs off from a melee enemy that comes too close, and then shoots again (the user, 2026-10-23).</summary>
        [UnityTest]
        public IEnumerator TheQueen_BacksOffFromMelee()
        {
            InvasionMode.Instance.King.Fall();
            var queen = BuyHero(SoldierType.Queen, 2);
            yield return new WaitForSeconds(0.5f);
            var at = queen.transform.position;
            var orc = OrcRaidManager.Instance.SpawnOrcOfKind(at + Vector3.right * 0.8f, 1, OrcKind.Raider);
            orc.Toughen(30f);
            var backedOff = false;
            for (var t = 0f; t < 4f && !backedOff; t += Time.deltaTime)
            {
                backedOff = queen.Unit.IsRetreating;
                yield return null;
            }
            Assert.IsTrue(backedOff, "The Queen stood still with an orc at her side.");
            for (var t = 0f; t < 4f && queen.Unit.IsRetreating; t += Time.deltaTime) yield return null;
            var d = Vector3.Distance(Flat(queen.transform.position), Flat(at));
            Assert.Greater(d, 1.5f, "The Queen did not get away.");
            if (orc != null) orc.TakeDamage(orc.MaxHealth);
        }

        /// <summary>The Queen's four skills (the user, 2026-10-23): a shot that kills, a volley over a cone, five soldiers for a while, and a sacrifice that ends in her fall.</summary>
        [UnityTest]
        public IEnumerator TheQueensSkills_Work()
        {
            InvasionMode.Instance.King.Fall();
            var queen = BuyHero(SoldierType.Queen, 2);
            while (queen.Level < 15) queen.AddExperience(Hero.XpForLevel(queen.Level));
            var skills = queen.GetComponent<HeroSkills>();
            CollectionAssert.AreEqual(new[] { HeroSkills.QueenShot, HeroSkills.QueenVolley, HeroSkills.QueenSummon, HeroSkills.QueenSacrifice },
                skills.Slots.Select(slot => slot.Balance.id).ToArray());
            Assert.IsFalse(skills.Use(0), "Выстрел went off with nobody to shoot.");

            var front = queen.transform.position + Vector3.forward * 4f;
            var target = OrcRaidManager.Instance.SpawnOrcOfKind(front, 3, OrcKind.Raider);
            target.Toughen(10f);
            Assert.IsTrue(skills.Use(0), "Выстрел did nothing with an orc in reach.");
            for (var t = 0f; t < 3f && target != null && target.CurrentHealth > 0; t += Time.deltaTime) yield return null;
            Assert.IsTrue(target == null || target.CurrentHealth <= 0, "Выстрел did not kill the orc.");

            // a line across her front, put exactly there (an orc spawns with a scatter)
            var line = new[] { -1f, 0f, 1f }.Select(x =>
            {
                var orc = OrcRaidManager.Instance.SpawnOrcOfKind(front, 1, OrcKind.Raider);
                var body = orc.GetComponent<CharacterController>();
                body.enabled = false;
                orc.transform.position = front + Vector3.right * x;
                body.enabled = true;
                orc.Toughen(50f);
                return orc;
            }).ToArray();
            Physics.SyncTransforms();
            var before = line.Select(o => o.CurrentHealth).ToArray();
            Assert.IsTrue(skills.Use(1), "Залп did nothing.");
            yield return new WaitForSeconds(1.5f);
            for (var i = 0; i < line.Length; i++) Assert.Less(line[i].CurrentHealth, before[i], $"Orc {i + 1} in the cone was not hit by Залп.");
            foreach (var orc in line) orc.TakeDamage(orc.MaxHealth);

            Assert.IsTrue(skills.Use(2), "Призыв did nothing.");
            var called = Object.FindObjectsByType<SummonedLifetime>(FindObjectsSortMode.None);
            Assert.AreEqual(Mathf.RoundToInt(skills.Slots[2].Balance.PowerAt(queen.Level)), called.Length, "Призыв called the wrong number of soldiers.");
            var level = SoldierStats.StatsAt(HeroSkills.SummonedType, skills.Slots[2].Balance.tier);
            Assert.AreEqual(level.maxHealth, called[0].GetComponent<SoldierUnit>().MaxHealth, "The summoned soldiers are not of the sheet's level.");

            Assert.IsTrue(skills.Use(3), "Жертва did nothing.");
            var health = queen.Unit.CurrentHealth;
            queen.Unit.TakeDamage(100);
            Assert.AreEqual(health, queen.Unit.CurrentHealth, "Жертва let damage through.");
            var seconds = skills.Slots[3].Balance.DurationAt(queen.Level);
            for (var t = 0f; t < seconds + 2f && !queen.IsFallen; t += Time.deltaTime) yield return null;
            Assert.IsTrue(queen.IsFallen, "The Queen did not fall when her Жертва was over.");
        }

        /// <summary>The Captain's four skills (the user, 2026-10-23): allies near him strike harder, the castle cannot be hurt, the jobless people fight, and he gets up where he fell.</summary>
        [UnityTest]
        public IEnumerator TheCaptainsSkills_Work()
        {
            var captain = BuyHero(SoldierType.Captain, 3);
            while (captain.Level < 15) captain.AddExperience(Hero.XpForLevel(captain.Level));
            var skills = captain.GetComponent<HeroSkills>();
            CollectionAssert.AreEqual(new[] { HeroSkills.CaptainMight, HeroSkills.CaptainRescue, HeroSkills.CaptainSpirit, HeroSkills.CaptainImmortal },
                skills.Slots.Select(slot => slot.Balance.id).ToArray());
            try
            {
                Assert.IsTrue(skills.Use(0), "Сила did nothing.");
                Assert.Greater(HeroAuras.MightAt(captain.transform.position), 1f, "Сила does not strengthen those beside him.");
                Assert.AreEqual(1f, HeroAuras.MightAt(captain.transform.position + Vector3.right * 60f), "Сила reaches across the map.");

                Assert.IsTrue(skills.Use(1), "Спасение did nothing.");
                var castle = InvasionMode.Instance.Castle;
                var walls = castle.CurrentHealth;
                castle.TryDamage(100);
                Assert.AreEqual(walls, castle.CurrentHealth, "The castle was hurt under Спасение.");

                Assert.IsTrue(skills.Use(2), "Боевой дух did nothing.");
                yield return null;
                var jobless = CitizenVisualsManager.Instance.AllAgents.Count(a => a != null && a.gameObject.activeInHierarchy && CitizenVisualsManager.Instance.WorkplaceOf(a) == null);
                Assert.AreEqual(jobless, CitizenMilitia.Fighting, "Not every jobless citizen took up arms.");

                Assert.IsFalse(skills.Use(3), "Бессмертие could be pressed -- it works by itself.");
                captain.Unit.TakeDamage(captain.Unit.MaxHealth * 10);
                Assert.IsFalse(captain.IsFallen, "Бессмертие did not keep the Captain on his feet.");
                Assert.AreEqual(Mathf.RoundToInt(captain.Unit.MaxHealth * skills.Slots[3].Balance.PowerAt(captain.Level)), captain.Unit.CurrentHealth, 1,
                    "The Captain did not get up on Бессмертие's share of his health.");
                yield return new WaitForSeconds(skills.Slots[3].Balance.DurationAt(captain.Level) + 0.2f);
                captain.Unit.TakeDamage(captain.Unit.MaxHealth * 10);
                Assert.IsTrue(captain.IsFallen, "Бессмертие worked twice without recharging.");
            }
            finally
            {
                HeroAuras.Clear();
                CitizenMilitia.Disband();
            }
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
            var expected = InvasionRules.BuildSeconds(tower, 1);
            Assert.AreEqual(BalanceConfig.Run.Building("Tower").levels[0].buildSeconds, expected, "A run builds a tower in other than the invasion tab's build_sec.");
            Assert.Greater(expected, 0f, "The tower builds at once: no build_sec on the invasion tab.");
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
            Assert.AreEqual(Mathf.RoundToInt(king.DamageAt(king.Level) * (1f + config.InvasionBossBuffDamage)), king.Unit.AttackDamage, "The blessing did not raise his damage.");
            Assert.AreEqual(Mathf.RoundToInt(king.MaxHealthAt(king.Level) * (1f + config.InvasionBossBuffHealth)), king.Unit.MaxHealth, "The blessing did not raise his health.");

            // a blessing runs out
            king.Empower(0.1f, config.InvasionBossBuffDamage, config.InvasionBossBuffHealth);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(0f, king.BuffRemaining);
            Assert.AreEqual(king.DamageAt(king.Level), king.Unit.AttackDamage, "The blessing never wore off.");
            Assert.AreEqual(king.MaxHealthAt(king.Level), king.Unit.MaxHealth);
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

        /// <summary>The King in hand at level 12: three skills open (one recharging), the fourth locked; then a held press's hint.</summary>
        [UnityTest]
        public IEnumerator Photograph_TheKingsSkills()
        {
            if (!PlaytestCapture.Available) Assert.Ignore("No graphics device: run without -nographics to photograph.");
            var king = InvasionMode.Instance.King;
            while (king.Level < 12) king.AddExperience(Hero.XpForLevel(king.Level));
            ArmyManager.Instance.SelectGroup(king.Unit.Group);
            king.Unit.SetCurrentHealth(king.Unit.MaxHealth / 2);
            var bar = Object.FindAnyObjectByType<CityBuilder.UI.HeroSkillBar>();
            yield return null;
            bar.Use(0);
            yield return new WaitForSeconds(6f);
            yield return PlaytestCapture.ShootWithUi("invasion-skills", king.transform.position, 14f, 50f, 0f);
            bar.ShowHint(3, true);
            yield return null;
            yield return PlaytestCapture.ShootWithUi("invasion-skill-hint", king.transform.position, 14f, 50f, 0f);
            bar.ShowHint(3, false);
            ArmyManager.Instance.SelectGroup(null);
        }

        /// <summary>The build cards with two prices and with three (the user, 2026-10-24: a third price on a second row, one row centred) -- a photo for the user.</summary>
        [UnityTest]
        public IEnumerator Photograph_TheBuildCards_WithTwoAndThreePrices()
        {
            if (!PlaytestCapture.Available) Assert.Ignore("No graphics device: run without -nographics to photograph.");
            var barracks = PlaytestWorld.Building("Barracks");
            var tower = PlaytestWorld.Building("Tower");
            barracks.cost.Add(new ResourceAmount { type = ResourceType.Gold, amount = 150 });
            tower.cost.Add(new ResourceAmount { type = ResourceType.Gold, amount = 25 });
            var menu = Object.FindAnyObjectByType<CityBuilder.UI.BuildingCategoryPanel>();
            menu.RefreshRunPrices();
            menu.Open(BuildingCategory.Defence, instant: true);
            yield return new WaitForSeconds(0.5f);
            yield return PlaytestCapture.ShootWithUi("invasion-build-cards-3", InvasionMode.Instance.Castle.transform.position, 18f, 55f, 0f);
            menu.Open(BuildingCategory.Production, instant: true);
            yield return new WaitForSeconds(0.5f);
            yield return PlaytestCapture.ShootWithUi("invasion-build-cards-2", InvasionMode.Instance.Castle.transform.position, 18f, 55f, 0f);
            barracks.cost.RemoveAll(c => c.type == ResourceType.Gold);
            tower.cost.RemoveAll(c => c.type == ResourceType.Gold);
            menu.RefreshRunPrices();
            menu.Close(instant: true);
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
