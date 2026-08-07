using System;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
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
        [SerializeField] private MapTerrainGenerator mapTerrainGenerator;
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

            foreach (var entry in data.resources)
            {
                ResourceManager.Instance.SetAmount(entry.type, entry.amount);
            }

            foreach (var entry in data.buildings)
            {
                if (!_catalog.TryGetValue(entry.buildingName, out var buildingData) || buildingData.prefab == null) continue;

                var cell = new Vector2Int(entry.cellX, entry.cellY);
                var footprint = buildingData.footprintSize;
                var center = GridManager.Instance.GetFootprintCenterWorld(cell, footprint);
                var instance = Instantiate(buildingData.prefab, center, Quaternion.identity);

                var buildingInstance = instance.GetComponent<BuildingInstance>();
                if (buildingInstance == null) buildingInstance = instance.AddComponent<BuildingInstance>();
                buildingInstance.Initialize(buildingData, cell);

                GridManager.Instance.SetAreaOccupied(cell, footprint, true);

                if (entry.assignedWorkers > 0)
                {
                    var production = instance.GetComponent<ProductionBuilding>();
                    production?.SetAssignedWorkers(entry.assignedWorkers);
                }
            }
        }

        private GameSaveData CollectSaveData()
        {
            var data = new GameSaveData
            {
                mapId = mapTerrainGenerator != null ? mapTerrainGenerator.CurrentMapId : string.Empty,
                mandatoryBuildingPlaced = buildingPlacer == null || !buildingPlacer.IsPlacingMandatoryBuilding,
                population = citizenManager != null ? citizenManager.TotalPopulation : 0
            };

            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                data.resources.Add(new ResourceEntry { type = type, amount = ResourceManager.Instance.GetAmount(type) });
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
                    assignedWorkers = production != null ? production.AssignedWorkers : 0
                });
            }

            return data;
        }
    }
}
