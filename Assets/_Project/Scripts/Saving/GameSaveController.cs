using System;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Saving
{
    /// <summary>
    /// Saving is entirely player-initiated (see SaveDialogController) — the player decides
    /// when and under what name to save, there is no autosave.
    /// </summary>
    public class GameSaveController : MonoBehaviour
    {
        [SerializeField] private BuildingPlacer buildingPlacer;
        [SerializeField] private CitizenManager citizenManager;
        [SerializeField] private GameCalendar gameCalendar;
        [SerializeField] private TaxManager taxManager;
        [SerializeField] private Research.ResearchManager researchManager;
        [SerializeField] private MapTerrainGenerator mapTerrainGenerator;
        [SerializeField] private MeshMapApplier meshMapApplier;
        [SerializeField] private List<BuildingData> knownBuildings = new List<BuildingData>();

        private Dictionary<string, BuildingData> _catalog;
        private GameSaveData _pendingLoad;

        public string CurrentSaveName { get; private set; } = string.Empty;

        /// <summary>Map id carried by the save being loaded this session, resolved in Awake so MapTerrainGenerator's Start() can read it. Empty when starting a fresh game.</summary>
        public string LoadedMapId { get; private set; } = string.Empty;

        private void Awake()
        {
            _catalog = new Dictionary<string, BuildingData>();
            foreach (var data in knownBuildings)
            {
                if (data != null) _catalog[data.buildingName] = data;
            }

            var nameToLoad = GameSessionIntent.SaveNameToLoad;
            GameSessionIntent.SaveNameToLoad = null;
            if (string.IsNullOrEmpty(nameToLoad)) return;

            _pendingLoad = SaveSystem.Load(nameToLoad);
            if (_pendingLoad == null) return;

            CurrentSaveName = nameToLoad;
            LoadedMapId = _pendingLoad.mapId;

            // Marked here (Awake, guaranteed to run before every Start() in the scene,
            // including BuildingPlacer's) so its Start() doesn't force-select the Town Hall
            // again for what is actually a resumed game. Actually instantiating the saved
            // buildings/resources needs GridManager/ResourceManager/CitizenManager's singletons,
            // which are only guaranteed ready once every Awake() has run — so that part waits
            // for Start() below.
            if (_pendingLoad.mandatoryBuildingPlaced && buildingPlacer != null)
            {
                buildingPlacer.MarkMandatoryBuildingAlreadyPlaced();
            }
        }

        private void Start()
        {
            if (_pendingLoad == null) return;
            ApplyLoadedState(_pendingLoad);
            _pendingLoad = null;
        }

        /// <summary>Explicit, player-triggered save under the given (already sanitized) name.</summary>
        public void SaveGame(string saveName)
        {
            if (string.IsNullOrEmpty(saveName)) return;
            CurrentSaveName = saveName;
            SaveSystem.Save(saveName, CollectSaveData());
        }

        private void ApplyLoadedState(GameSaveData data)
        {
            if (citizenManager != null)
            {
                citizenManager.SetPopulation(data.population);
            }

            if (gameCalendar != null)
            {
                gameCalendar.SetCurrentDay(data.currentDay);
            }

            if (taxManager != null)
            {
                taxManager.SetTaxRate(data.taxRatePercent);
            }

            foreach (var entry in data.resources)
            {
                ResourceManager.Instance.SetAmount(entry.type, entry.amount);
            }

            // Before the buildings: a restored building asks whether its level is researched the
            // moment it is placed, and a fresh ResearchManager would answer "no".
            if (researchManager != null)
            {
                researchManager.RestoreFromSave(data.completedResearch, data.currentResearchId,
                    data.currentResearchElapsedSeconds, data.currentResearchPaidCoins);
            }

            foreach (var entry in data.buildings)
            {
                if (!_catalog.TryGetValue(entry.buildingName, out var buildingData) || buildingData.prefab == null) continue;

                var cell = new Vector2Int(entry.cellX, entry.cellY);
                // Rotated 90/270 means the footprint's X/Z are swapped for grid-occupancy
                // purposes -- matches BuildingPlacer.RotatedFootprint at placement time.
                var footprint = entry.rotationSteps % 2 == 0
                    ? buildingData.footprintSize
                    : new Vector2Int(buildingData.footprintSize.y, buildingData.footprintSize.x);
                var center = GridManager.Instance.GetFootprintCenterWorld(cell, footprint);
                var rotation = Quaternion.Euler(0f, entry.rotationSteps * 90f, 0f);
                var instance = Instantiate(buildingData.prefab, center, rotation);

                var buildingInstance = instance.GetComponent<BuildingInstance>();
                if (buildingInstance == null) buildingInstance = instance.AddComponent<BuildingInstance>();
                buildingInstance.Initialize(buildingData, cell, entry.rotationSteps);
                buildingInstance.SetLevel(entry.level);
                // entry.currentHealth is 0 for saves made before this field existed (missing JSON
                // field deserializes to the int default) -- treat that as "unknown", not "destroyed".
                var health = entry.currentHealth > 0 ? entry.currentHealth : buildingData.LevelStats(entry.level).maxHealth;
                buildingInstance.SetCondition(health, entry.decay);

                GridManager.Instance.SetAreaOccupied(cell, footprint, true);

                if (buildingData.isRoad && RoadNetwork.Instance != null)
                {
                    for (var x = 0; x < footprint.x; x++)
                    {
                        for (var z = 0; z < footprint.y; z++)
                        {
                            RoadNetwork.Instance.RegisterRoad(cell + new Vector2Int(x, z));
                        }
                    }
                }

                var production = instance.GetComponent<ProductionBuilding>();
                if (production != null)
                {
                    // Before the workers: the recipe decides what those workers will be making on
                    // the very first tick after the load.
                    production.SelectRecipeById(entry.selectedRecipeId);
                    if (entry.assignedWorkers > 0) production.SetAssignedWorkers(entry.assignedWorkers);
                }
            }

            RestoreArmy(data);
            RestoreRaids(data);

            // After the army: what the settlement has to feed tomorrow counts its soldiers too.
            FoodConsumptionManager.Instance?.RestoreFromSave(data.hungryDaysInARow, data.recentStarvationDeaths);

            // After the buildings, which is the whole point: MigrationManager decides whether a
            // settlement exists by asking whether a Town Hall is standing, and until the loop
            // above ran, none was.
            Citizens.MigrationManager.Instance?.RestoreFromSave(data.migrationTimerSeconds, data.settlingInSecondsRemaining);

            RestoreRocks(data);
        }

        /// <summary>
        /// Puts back the exact boulders the save was made with, each as worked-out as it was.
        /// Runs after the buildings so the cells a restored building sits on are already marked
        /// occupied -- a boulder never shared a cell with a building when the save was written,
        /// and it must not be dropped onto one now.
        /// </summary>
        private static void RestoreRocks(GameSaveData data)
        {
            var spawner = Maps.RockSpawner.Instance;
            if (spawner == null || data.rocks == null || data.rocks.Count == 0) return;

            var rocks = new List<(Vector2Int cell, int remaining)>(data.rocks.Count);
            foreach (var entry in data.rocks)
            {
                rocks.Add((new Vector2Int(entry.cellX, entry.cellY), entry.remaining));
            }

            spawner.RestoreFromSave(rocks);
        }

        /// <summary>
        /// Puts the portal, the raid clock and the orcs already in the field back. Runs here in
        /// Start rather than being left to OrcRaidManager's own Update, which opens a portal on the
        /// first frame it sees a Town Hall -- and the buildings above have just given it one.
        /// </summary>
        private static void RestoreRaids(GameSaveData data)
        {
            var raids = Combat.OrcRaidManager.Instance;
            if (raids == null) return;

            raids.RestoreFromSave(data.portalPlaced, data.portalCell, data.portalHealth, data.secondsUntilNextRaid);

            if (data.orcs == null) return;
            foreach (var orc in data.orcs)
            {
                raids.RestoreOrc(orc.position, orc.level, orc.currentHealth);
            }
        }

        /// <summary>
        /// Rebuilds the army the save was written with. Runs after the research above, not before:
        /// a soldier reads its health and damage off the level its type has been researched to, and
        /// a militia restored against a fresh ResearchManager would come back at level 1.
        /// </summary>
        private static void RestoreArmy(GameSaveData data)
        {
            var army = Combat.ArmyManager.Instance;
            if (army == null || data.armyGroups == null) return;

            foreach (var groupEntry in data.armyGroups)
            {
                var group = army.RestoreGroup(groupEntry.type, groupEntry.holdPosition, groupEntry.priority);
                if (groupEntry.soldiers == null) continue;

                foreach (var soldier in groupEntry.soldiers)
                {
                    army.RestoreSoldier(group, soldier.position, soldier.currentHealth);
                }
            }
        }

        private GameSaveData CollectSaveData()
        {
            var data = new GameSaveData
            {
                mapId = meshMapApplier != null && !string.IsNullOrEmpty(meshMapApplier.CurrentMapId)
                    ? meshMapApplier.CurrentMapId
                    : (mapTerrainGenerator != null ? mapTerrainGenerator.CurrentMapId : string.Empty),
                mandatoryBuildingPlaced = buildingPlacer == null || !buildingPlacer.IsPlacingMandatoryBuilding,
                population = citizenManager != null ? citizenManager.TotalPopulation : 0,
                currentDay = gameCalendar != null ? gameCalendar.CurrentDay : 1,
                taxRatePercent = taxManager != null ? taxManager.TaxRatePercent : 10
            };

            var migration = Citizens.MigrationManager.Instance;
            if (migration != null)
            {
                data.migrationTimerSeconds = migration.Timer;
                data.settlingInSecondsRemaining = migration.SettlingInRemaining;
            }

            var rocks = Maps.RockSpawner.Instance;
            if (rocks != null)
            {
                foreach (var (cell, remaining) in rocks.LiveRocks)
                {
                    data.rocks.Add(new RockEntry { cellX = cell.x, cellY = cell.y, remaining = remaining });
                }
            }

            if (researchManager != null)
            {
                data.completedResearch.AddRange(researchManager.CompletedTopicIds);
                researchManager.ReadCurrentForSave(out var currentResearchId, out var elapsed, out var paid);
                data.currentResearchId = currentResearchId;
                data.currentResearchElapsedSeconds = elapsed;
                data.currentResearchPaidCoins = paid;
            }

            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                data.resources.Add(new ResourceEntry { type = type, amount = ResourceManager.Instance.GetAmount(type) });
            }

            var army = Combat.ArmyManager.Instance;
            if (army != null)
            {
                foreach (var group in army.Groups)
                {
                    // An empty group is still worth writing down: it carries the rally point and
                    // priority the player set, which ArmyManager deliberately keeps when the last
                    // member dies.
                    var entry = new ArmyGroupEntry { type = group.Type, holdPosition = group.HoldPosition, priority = group.Priority };
                    foreach (var soldier in group.Members)
                    {
                        if (soldier == null) continue;
                        entry.soldiers.Add(new SoldierEntry { position = soldier.transform.position, currentHealth = soldier.CurrentHealth });
                    }
                    data.armyGroups.Add(entry);
                }
            }

            var raids = Combat.OrcRaidManager.Instance;
            if (raids != null)
            {
                data.portalPlaced = raids.PortalPlaced;
                data.portalCell = raids.PortalCell;
                // No portal in the registry means the one that was placed has since been destroyed,
                // which zero says -- and which is what stops a fresh one opening on load.
                data.portalHealth = Combat.OrcPortal.All.Count > 0 ? Combat.OrcPortal.All[0].CurrentHealth : 0;
                data.secondsUntilNextRaid = raids.SecondsUntilNextRaid;
            }

            foreach (var orc in Combat.OrcUnit.All)
            {
                if (orc == null) continue;
                data.orcs.Add(new OrcEntry { position = orc.transform.position, level = orc.Level, currentHealth = orc.CurrentHealth });
            }

            var food = FoodConsumptionManager.Instance;
            if (food != null)
            {
                data.hungryDaysInARow = food.HungryDaysInARow;
                data.recentStarvationDeaths.AddRange(food.RecentDeathsPerDay);
            }

            foreach (var instance in FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
            {
                if (instance.Data == null) continue;

                var production = instance.GetComponent<ProductionBuilding>();
                data.buildings.Add(new BuildingEntry
                {
                    buildingName = instance.Data.buildingName,
                    cellX = instance.OriginCell.x,
                    cellY = instance.OriginCell.y,
                    assignedWorkers = production != null ? production.AssignedWorkers : 0,
                    selectedRecipeId = production != null && production.SelectedRecipe != null ? production.SelectedRecipe.id : string.Empty,
                    level = instance.Level,
                    currentHealth = instance.CurrentHealth,
                    decay = instance.Decay,
                    rotationSteps = instance.RotationSteps
                });
            }

            return data;
        }
    }
}
