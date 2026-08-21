using System.Collections;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Resources;
using CityBuilder.Saving;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// The two systems that shipped without ever being seen running: a ceiling on every stockpile
    /// with storehouses to raise it, and an upgrade that actually changes what a building is.
    ///
    /// Both were built and proved through EditMode tests on their pure formulas. What those cannot
    /// reach is the wiring: whether a storehouse hands its room to ResourceManager on placement and
    /// takes it back when it burns down, whether a producer notices it has nowhere to put its
    /// output, whether an upgraded building really gains the health and the citizens its level says
    /// it has. Every one of those lives in a MonoBehaviour's lifecycle, so it takes a live scene.
    /// </summary>
    public class StorageAndUpgradeTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map1";

        /// <summary>Production is measured in six-second ticks; a test that waits for one in real time is a test nobody runs twice.</summary>
        private const float TestTimeScale = 8f;

        private static bool _sceneLoaded;

        private readonly List<BuildingInstance> _placed = new List<BuildingInstance>();

        [UnitySetUp]
        public IEnumerator PrepareScene()
        {
            LogAssert.ignoreFailingMessages = true;

            if (!_sceneLoaded)
            {
                Time.timeScale = 1f;
                GameSessionIntent.NewGameMapId = MapId;
                SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
                yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);
                _sceneLoaded = true;
            }

            ModalGate.SetBlocked(false);
            Time.timeScale = 1f;
            ResourceManager.Instance.SetInfiniteResources(false);
            CitizenManager.Instance.SetPopulation(20);

            // Upgrading now needs the level researched in the Laboratory (see ResearchGateTests,
            // which is where that rule is actually tested). These tests are about what an upgrade
            // DOES, so they start with the whole tech list granted.
            Research.ResearchManager.Instance?.CompleteEverything();
            yield return null;
        }

        [TearDown]
        public void ClearPlacedBuildings()
        {
            Time.timeScale = 1f;
            foreach (var building in _placed)
            {
                if (building != null) Object.DestroyImmediate(building.gameObject);
            }
            _placed.Clear();
        }

        /// <summary>
        /// The entertainment factor end to end: a Таверна is a building with no workers and no
        /// recipe, so the only thing it does is lift the settlement's mood -- and the only way to
        /// see that it does is to put one down and ask HappinessManager. Every one of these
        /// buildings was a placeholder until this worked.
        /// </summary>
        [Test]
        public void ATavern_LiftsTheSettlementsMood()
        {
            var happiness = HappinessManager.Instance;
            Assert.IsNotNull(happiness, "The scene has no HappinessManager.");

            happiness.Recompute();
            var before = happiness.EntertainmentScore;
            Assert.AreEqual(0, before, "A settlement with nothing to do is supposed to score nothing on this factor.");

            var tavern = Place(PlaytestWorld.Building("Tavern"));
            Assert.Greater(tavern.Happiness, 0, "The Таверна's own row says it is worth nothing to the settlement.");

            happiness.Recompute();
            Assert.Greater(happiness.EntertainmentScore, before,
                "The Таверна is standing and the settlement is no happier for it -- its happiness value never reaches the score.");
        }

        [Test]
        public void EveryResource_StartsAtTheSettlementsOwnCeiling()
        {
            var config = BalanceConfig.Instance;
            var resources = ResourceManager.Instance;

            Assert.AreEqual(config.BaseCapacityMaterials, resources.GetCapacity(ResourceType.Wood));
            // Iron and Gold are the ORE now (the Плавильня makes the metal), so both are raw goods
            // and live in the Склад -- it is the bar that goes in the treasury.
            Assert.AreEqual(config.BaseCapacityMaterials, resources.GetCapacity(ResourceType.Iron));
            Assert.AreEqual(config.BaseCapacityMaterials, resources.GetCapacity(ResourceType.Gold));
            Assert.AreEqual(config.BaseCapacityFood, resources.GetCapacity(ResourceType.Food));
            Assert.AreEqual(config.BaseCapacityValuables, resources.GetCapacity(ResourceType.GoldBar));
            Assert.AreEqual(config.BaseCapacityValuables, resources.GetCapacity(ResourceType.Coins));

            // Headcount is not warehoused -- a ceiling here would quietly cap the town's growth.
            Assert.AreEqual(int.MaxValue, resources.GetCapacity(ResourceType.Population));
            Assert.Greater(config.BaseCapacityFood, 0, "A settlement that can hold no food at all cannot be played.");
        }

        [Test]
        public void AddingMoreThanFits_StoresWhatFitsAndReportsTheRest()
        {
            var resources = ResourceManager.Instance;
            var capacity = resources.GetCapacity(ResourceType.Food);
            resources.SetAmount(ResourceType.Food, capacity - 5);

            Assert.AreEqual(5, resources.Add(ResourceType.Food, 20), "Add is supposed to answer with what actually went into store.");
            Assert.AreEqual(capacity, resources.GetAmount(ResourceType.Food), "The stockpile went over its ceiling.");
            Assert.AreEqual(0, resources.Add(ResourceType.Food, 10), "A full store accepted more.");

            // Spending is never blocked by a ceiling.
            Assert.AreEqual(-10, resources.Add(ResourceType.Food, -10));
        }

        [UnityTest]
        public IEnumerator AGranary_RaisesTheFoodCeiling_AndLosingItSpillsTheSurplus()
        {
            var resources = ResourceManager.Instance;
            var baseCapacity = resources.GetCapacity(ResourceType.Food);

            var granaryData = PlaytestWorld.Building("Granary");
            Assert.IsNotNull(granaryData, "No Granary in the building catalogue.");
            var room = granaryData.LevelStats(1).storageCapacity;
            Assert.Greater(room, 0, "The Кладовая's balance row gives it no storage capacity at all -- it is a storehouse that stores nothing.");

            var granary = Place(granaryData);
            Assert.AreEqual(baseCapacity + room, resources.GetCapacity(ResourceType.Food), "Building a Кладовая did not raise the food ceiling.");

            // Only the Кладовая's own group moves: bread does not fit in a treasury.
            Assert.AreEqual(BalanceConfig.Instance.BaseCapacityMaterials, resources.GetCapacity(ResourceType.Wood));

            var stocked = baseCapacity + room;
            resources.SetAmount(ResourceType.Food, stocked);

            _placed.Remove(granary);
            PlaytestWorld.Demolish(granary);
            yield return null;

            Assert.AreEqual(baseCapacity, resources.GetCapacity(ResourceType.Food), "Losing the Кладовая did not take its room back.");
            Assert.AreEqual(baseCapacity, resources.GetAmount(ResourceType.Food),
                "The food that was being kept in the destroyed Кладовая is still in store -- the surplus should have spilled with the building.");
        }

        [UnityTest]
        public IEnumerator UpgradingAStorehouse_RaisesItsCeilingAgain()
        {
            var resources = ResourceManager.Instance;
            resources.SetInfiniteResources(true);

            var warehouseData = PlaytestWorld.Building("Warehouse");
            Assert.IsNotNull(warehouseData, "No Warehouse in the building catalogue.");

            var baseCapacity = resources.GetCapacity(ResourceType.Wood);
            var warehouse = Place(warehouseData);
            Assert.AreEqual(baseCapacity + warehouseData.LevelStats(1).storageCapacity, resources.GetCapacity(ResourceType.Wood));

            for (var level = 2; level <= BuildingInstance.MaxLevel; level++)
            {
                Assert.IsTrue(warehouse.TryUpgrade(), $"The Warehouse refused to upgrade to level {level} with infinite resources.");
                Assert.AreEqual(level, warehouse.Level);
                Assert.AreEqual(baseCapacity + warehouseData.LevelStats(level).storageCapacity, resources.GetCapacity(ResourceType.Wood),
                    $"A level-{level} Warehouse is not holding what its balance row says it should.");
            }

            // The sheet's own promise for this building: capacity is what upgrading it is for.
            Assert.Greater(warehouseData.LevelStats(3).storageCapacity, warehouseData.LevelStats(1).storageCapacity);
            yield break;
        }

        /// <summary>
        /// The player-facing half of a full store: a producer working for nothing has to say so, and
        /// has to say it once rather than every tick for every farm in the settlement.
        /// </summary>
        [UnityTest]
        public IEnumerator AProducerWithNowhereToPutItsOutput_SaysSoOnce()
        {
            var resources = ResourceManager.Instance;
            var hutData = PlaytestWorld.Building("FisherHut");
            Assert.IsNotNull(hutData, "No FisherHut in the building catalogue.");

            var hut = Place(hutData);
            var production = hut.GetComponent<ProductionBuilding>();
            Assert.IsNotNull(production, "The FisherHut prefab has no ProductionBuilding -- it can never produce anything.");

            production.SetAssignedWorkers(production.MaxWorkers);
            Assert.Greater(production.AssignedWorkers, 0, "Nobody could be put to work at the hut.");

            resources.SetAmount(ResourceType.Food, resources.GetCapacity(ResourceType.Food));
            var before = CountOverflowNotices();

            Time.timeScale = TestTimeScale;
            yield return WaitForProductionTicks(hutData.productionIntervalSeconds, 3);
            Time.timeScale = 1f;

            var overflowLines = CountOverflowNotices() - before;

            Assert.AreEqual(1, overflowLines,
                overflowLines == 0
                    ? "A hut producing food into a full store said nothing -- the player has no way to know their fishermen are working for nothing."
                    : $"The full-store notice was logged {overflowLines} times in three production ticks; it is supposed to latch and stay quiet.");

            // And it really did have nowhere to put it.
            Assert.AreEqual(resources.GetCapacity(ResourceType.Food), resources.GetAmount(ResourceType.Food));
        }

        [UnityTest]
        public IEnumerator UpgradingAHouse_MakesItToughAndRoomierAtEveryLevel()
        {
            ResourceManager.Instance.SetInfiniteResources(true);

            var houseData = PlaytestWorld.Building("Hovel");
            Assert.IsNotNull(houseData, "No Hovel in the building catalogue.");

            var house = Place(houseData);
            Assert.AreEqual(houseData.LevelStats(1).maxHealth, house.MaxHealth);

            for (var level = 2; level <= BuildingInstance.MaxLevel; level++)
            {
                var before = houseData.LevelStats(level - 1);
                var after = houseData.LevelStats(level);
                var capacityBefore = CitizenManager.Instance.Capacity;
                var healthBefore = house.CurrentHealth;

                Assert.IsTrue(house.TryUpgrade(), $"The House refused to upgrade to level {level} with infinite resources.");

                Assert.AreEqual(level, house.Level);
                Assert.AreEqual(after.maxHealth, house.MaxHealth, $"A level-{level} House is not as tough as its balance row says.");
                Assert.AreEqual(healthBefore + (after.maxHealth - before.maxHealth), house.CurrentHealth,
                    "An upgrade should add the difference in hit points, not silently repair the building.");
                Assert.AreEqual(capacityBefore + (after.housingCapacity - before.housingCapacity), CitizenManager.Instance.Capacity,
                    $"A level-{level} House did not give the settlement the extra room it houses.");

                Assert.Greater(after.maxHealth, before.maxHealth, $"Level {level} of the House is no tougher than level {level - 1} -- there is nothing to upgrade for.");
            }
            yield break;
        }

        /// <summary>
        /// Levels 2 and 3 have no models yet, and the agreed rule is that a building must never
        /// vanish because a file has not been drawn: it shows the nearest level below instead. This
        /// is that rule, checked on the thing the player sees rather than on the resolver.
        /// </summary>
        [Test]
        public void AnUpgradedBuilding_KeepsShowingExactlyOneModel()
        {
            ResourceManager.Instance.SetInfiniteResources(true);

            var houseData = PlaytestWorld.Building("Hovel");
            var house = Place(houseData);
            var atLevelOne = VisibleBounds(house);

            Assert.IsTrue(house.TryUpgrade() && house.TryUpgrade(), "The House refused to upgrade with infinite resources.");

            var atLevelThree = VisibleBounds(house);
            Assert.Greater(atLevelThree.size.sqrMagnitude, 0f, "The House stopped drawing anything at all after being upgraded.");
            Assert.AreEqual(atLevelOne.size, atLevelThree.size,
                "The House changed shape on upgrade -- which would be the right answer once levels 2 and 3 have models, and means this test now needs to check the new ones instead.");
        }

        /// <summary>
        /// An upgrade the player can SEE. Until levels 2 and 3 have models of their own, a
        /// building that has been upgraded is pixel-for-pixel the building that was not, and the
        /// only trace of the coins is a number on a card the player has to open. The disc is the
        /// whole feedback loop, so it is worth a test that it appears at all, that it appears
        /// nowhere at level 1, and that it never becomes something the player can tap by accident.
        /// </summary>
        [Test]
        public void UpgradingABuilding_HangsALevelDiscOverIt()
        {
            ResourceManager.Instance.SetInfiniteResources(true);

            var house = Place(PlaytestWorld.Building("Hovel"));
            Assert.IsNull(LevelMarker(house), "A freshly placed building is already wearing a level marker -- level 1 is not an achievement.");

            Assert.IsTrue(house.TryUpgrade(), "The House refused to upgrade with infinite resources.");
            var marker = LevelMarker(house);
            Assert.IsNotNull(marker, "An upgraded building shows nothing at all: the player has no way to tell it apart from one they never paid for.");
            Assert.IsTrue(marker.activeInHierarchy, "The level marker exists but is switched off.");

            // Not IsNull: Destroy only takes effect at the end of the frame, and the frame that
            // matters is this one -- the player's finger is on the building they just upgraded.
            var markerCollider = marker.GetComponent<Collider>();
            Assert.IsTrue(markerCollider == null || !markerCollider.enabled,
                "The level marker is still catching taps: it floats over the roof, so it swallows the ones meant for the building underneath it.");

            var box = house.GetComponent<BoxCollider>();
            Assert.Greater(marker.transform.localPosition.y, box.size.y,
                "The level marker is sunk into the building instead of floating over its roof.");
        }

        /// <summary>The top level has to be legible as the top level -- otherwise the second upgrade is the invisible one.</summary>
        [Test]
        public void TheTopLevel_LooksDifferentFromTheOneBelowIt()
        {
            ResourceManager.Instance.SetInfiniteResources(true);

            var house = Place(PlaytestWorld.Building("Hovel"));
            Assert.IsTrue(house.TryUpgrade(), "The House refused to upgrade with infinite resources.");

            var marker = LevelMarker(house);
            var atLevelTwo = marker.transform.localScale.x;
            var colorAtLevelTwo = marker.GetComponent<Renderer>().sharedMaterial.color;

            Assert.IsTrue(house.TryUpgrade(), "The House refused its second upgrade with infinite resources.");

            Assert.AreEqual(BuildingInstance.MaxLevel, house.Level);
            Assert.AreNotEqual(colorAtLevelTwo, marker.GetComponent<Renderer>().sharedMaterial.color,
                "Levels 2 and 3 wear the same marker -- the second upgrade changes nothing the player can see.");
            Assert.Greater(marker.transform.localScale.x, atLevelTwo, "The top level's marker is no bigger than the one below it.");
        }

        /// <summary>
        /// Roads are laid a tile at a time, dozens at a time. A disc over every tile of an upgraded
        /// road would bury the town it is meant to describe.
        /// </summary>
        [Test]
        public void AnUpgradedRoad_WearsNoMarker()
        {
            ResourceManager.Instance.SetInfiniteResources(true);

            var road = Place(PlaytestWorld.Building("Road"));
            Assert.IsTrue(road.TryUpgrade(), "The Road refused to upgrade with infinite resources.");

            Assert.AreEqual(2, road.Level);
            Assert.IsNull(LevelMarker(road), "An upgraded road tile is wearing a level marker.");
        }

        /// <summary>The marker as the player would find it: a child object of the building, not a component anyone can ask for.</summary>
        private static GameObject LevelMarker(BuildingInstance building)
        {
            foreach (var indicator in building.GetComponentsInChildren<WorldIndicator>(true))
            {
                if (indicator.name == "LevelMarker") return indicator.gameObject;
            }
            return null;
        }

        [UnityTest]
        public IEnumerator Photograph_TheStorehouses()
        {
            // Placeholder geometry the user has never seen: worth a look before any of it is drawn
            // properly.
            var origin = PlaytestWorld.FindFreeArea(new Vector2Int(9, 3));
            Assert.AreNotEqual(new Vector2Int(-1, -1), origin, "Nowhere free to stand three storehouses side by side.");

            var names = new[] { "Warehouse", "Granary", "Treasury" };
            for (var i = 0; i < names.Length; i++)
            {
                var data = PlaytestWorld.Building(names[i]);
                if (data == null) continue;
                _placed.Add(PlaytestWorld.Place(data, origin + new Vector2Int(i * 3, 0)));
            }
            yield return null;

            yield return PlaytestCapture.Shoot("storehouses", PlaytestWorld.CellCenter(origin + new Vector2Int(4, 1)), 14f, 35f, 20f);
        }

        /// <summary>
        /// The three levels standing next to each other, for a human to look at. Nothing here is
        /// asserted: the question a picture answers is whether a silver disc and a gold one are
        /// actually TELLABLE APART on a phone-sized screen from the game's own camera angle, and no
        /// assertion can answer that.
        /// </summary>
        [UnityTest]
        public IEnumerator Photograph_TheLevelMarkers()
        {
            ResourceManager.Instance.SetInfiniteResources(true);

            var origin = PlaytestWorld.FindFreeArea(new Vector2Int(9, 3));
            Assert.AreNotEqual(new Vector2Int(-1, -1), origin, "Nowhere free to stand three houses side by side.");

            var houseData = PlaytestWorld.Building("Hovel");
            for (var level = 1; level <= BuildingInstance.MaxLevel; level++)
            {
                var house = PlaytestWorld.Place(houseData, origin + new Vector2Int((level - 1) * 3, 0));
                _placed.Add(house);
                for (var step = 1; step < level; step++) house.TryUpgrade();
            }
            yield return null;

            yield return PlaytestCapture.Shoot("building-levels", PlaytestWorld.CellCenter(origin + new Vector2Int(4, 1)), 13f, 30f, 20f);
        }

        private BuildingInstance Place(BuildingData data)
        {
            var cell = PlaytestWorld.FindFreeArea(data.footprintSize + Vector2Int.one);
            Assert.AreNotEqual(new Vector2Int(-1, -1), cell, $"Nowhere free on the map to place a {data.buildingName}.");

            var building = PlaytestWorld.Place(data, cell);
            _placed.Add(building);
            return building;
        }

        /// <summary>
        /// What the building itself draws. The in-world indicators hung on it -- the level disc,
        /// the decay diamond, the health bar -- are deliberately left out: they are HUD drawn in
        /// world space, and an upgraded building would otherwise measure taller than its own model
        /// purely because a disc is floating over the roof.
        /// </summary>
        private static Bounds VisibleBounds(BuildingInstance building)
        {
            var renderers = building.GetComponentsInChildren<MeshRenderer>();

            var bounds = new Bounds();
            var found = false;
            foreach (var renderer in renderers)
            {
                if (renderer.GetComponentInParent<WorldIndicator>() != null) continue;

                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                    continue;
                }
                bounds.Encapsulate(renderer.bounds);
            }

            Assert.IsTrue(found, $"{building.Data.buildingName} draws nothing at all.");
            return bounds;
        }

        /// <summary>The event log is newest-first, capped, and stamps every line with the day -- so the notice is counted by what it says, not by where it sits.</summary>
        private static int CountOverflowNotices()
        {
            var count = 0;
            foreach (var entry in EventLogManager.Instance.Entries)
            {
                if (entry.Contains("Некуда складывать")) count++;
            }
            return count;
        }

        /// <summary>Frames, not seconds: a WaitForSeconds here would be at the mercy of whatever timeScale the previous test left behind.</summary>
        private static IEnumerator WaitForProductionTicks(float intervalSeconds, int ticks)
        {
            var elapsed = 0f;
            var target = intervalSeconds * ticks + 0.5f;
            while (elapsed < target)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }
}
