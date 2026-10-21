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
            yield return null;
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

        [Test]
        public void APlayersOrder_EndsTheGuard_AndTheCastleButtonResumesIt()
        {
            var king = InvasionMode.Instance.King;
            king.Unit.Group.OrderMoveTo(king.Home + new Vector3(5f, 0f, 0f));
            king.Unit.Group.NotifyPlayerOrder();
            Assert.IsFalse(king.Guarding, "The King kept guarding after the player told him to go somewhere.");
            Object.FindAnyObjectByType<CityBuilder.UI.KingPortraitController>().GuardBase();
            Assert.IsTrue(king.Guarding, "The castle button did not send him back to guard the base.");
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
            yield return new WaitForSeconds(0.8f);
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
            foreach (var orc in OrcUnit.All) Assert.Less(Vector3.Distance(Flat(orc.transform.position), Flat(start)), 4f, "An orc of the wave stepped out away from its camp.");
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

        /// <summary>A wave beaten off pays its orcs' gold and 10 × its number, and the breather begins.</summary>
        [UnityTest]
        public IEnumerator AWaveBeatenOff_PaysItsGold_AndTheBreatherBegins()
        {
            var waves = InvasionMode.Instance.Waves;
            var coins = ResourceManager.Instance.GetAmount(InvasionRules.Money);
            waves.SendNextWave();
            var orcs = OrcUnit.All.ToList();
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

        [UnityTest]
        public IEnumerator Photograph_TheRun()
        {
            if (!PlaytestCapture.Available) Assert.Ignore("No graphics device: run without -nographics to photograph.");
            yield return new WaitForSeconds(1f);
            yield return PlaytestCapture.Shoot("invasion-overview", Vector3.zero, 200f, 55f, 0f);
            yield return PlaytestCapture.Shoot("invasion-base", Vector3.zero, 44f, 52f, 0f, 60f);
            yield return PlaytestCapture.Shoot("invasion-king", InvasionMode.Instance.King.transform.position, 9f, 40f, 0f, 60f);
            yield return PlaytestCapture.Shoot("invasion-causeway", Causeways[1], 22f, 50f, 0f, 60f);

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
