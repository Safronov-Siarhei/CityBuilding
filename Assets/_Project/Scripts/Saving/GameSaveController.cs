using System;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Grid;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Saving
{
    public class GameSaveController : MonoBehaviour
    {
        [SerializeField] private BuildingPlacer buildingPlacer;
        [SerializeField] private List<BuildingData> knownBuildings = new List<BuildingData>();

        private Dictionary<string, BuildingData> _catalog;
        private GameSaveData _pendingLoad;

        private void Awake()
        {
            _catalog = new Dictionary<string, BuildingData>();
            foreach (var data in knownBuildings)
            {
                if (data != null) _catalog[data.buildingName] = data;
            }

            if (GameSessionIntent.LoadSavedGame)
            {
                GameSessionIntent.LoadSavedGame = false;
                _pendingLoad = SaveSystem.HasSave() ? SaveSystem.Load() : null;

                // Marked here (Awake, guaranteed to run before every Start() in the scene,
                // including BuildingPlacer's) so its Start() doesn't force-select the Town Hall
                // again for what is actually a resumed game. Actually instantiating the saved
                // buildings/resources needs GridManager/ResourceManager's singletons, which are
                // only guaranteed ready once every Awake() has run — so that part waits for
                // Start() below.
                if (_pendingLoad != null && _pendingLoad.mandatoryBuildingPlaced && buildingPlacer != null)
                {
                    buildingPlacer.MarkMandatoryBuildingAlreadyPlaced();
                }
            }
        }

        private void Start()
        {
            if (_pendingLoad != null)
            {
                ApplyLoadedState(_pendingLoad);
                _pendingLoad = null;
            }

            if (buildingPlacer != null)
            {
                buildingPlacer.OnBuildingPlaced += SaveGame;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveGame();
        }

        private void OnApplicationQuit()
        {
            SaveGame();
        }

        public void SaveGame()
        {
            SaveSystem.Save(CollectSaveData());
        }

        private void ApplyLoadedState(GameSaveData data)
        {
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
            }
        }

        private GameSaveData CollectSaveData()
        {
            var data = new GameSaveData
            {
                mandatoryBuildingPlaced = buildingPlacer == null || !buildingPlacer.IsPlacingMandatoryBuilding
            };

            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                data.resources.Add(new ResourceEntry { type = type, amount = ResourceManager.Instance.GetAmount(type) });
            }

            foreach (var instance in FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
            {
                if (instance.Data == null) continue;
                data.buildings.Add(new BuildingEntry
                {
                    buildingName = instance.Data.buildingName,
                    cellX = instance.OriginCell.x,
                    cellY = instance.OriginCell.y
                });
            }

            return data;
        }
    }
}
