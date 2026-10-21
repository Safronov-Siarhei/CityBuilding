using System.Collections;
using CityBuilder.Buildings;
using CityBuilder.Combat;
using CityBuilder.Grid;
using CityBuilder.Maps;
using CityBuilder.Saving;
using UnityEngine;

namespace CityBuilder.Invasion
{
    /// <summary>
    /// The Invasion mode (the user's design, 2026-10-20/21): an endless defence of a base on the
    /// island in the middle of its own map (Tools/blender/invasion1.py), orc waves coming down three
    /// lanes over three earth causeways, and the King as a hero who levels up in the jungle between them.
    ///
    /// Sits on GameManagers in every game and does nothing unless the menu's «В бой» started the
    /// game (GameSessionIntent.NewGameIsInvasion). Then it switches off what a run does not have
    /// -- the sandbox's raids and camps, illness, fires -- and, once the map is down, sets the
    /// run up: the castle in the middle, a tower at the island's end of each causeway, the King.
    /// The waves and the jungle's monsters come in later steps.
    /// </summary>
    public class InvasionMode : MonoBehaviour
    {
        public const string MapId = "Inv1";

        /// <summary>How far in from a causeway's middle its tower stands, on the island.</summary>
        private const float TowerInsetMeters = 11f;

        /// <summary>Where the King stands at the start and comes back to: in front of the castle, towards the south causeways.</summary>
        private static readonly Vector3 KingHomeOffset = new Vector3(0f, 0f, -7f);

        [SerializeField] private BuildingPlacer buildingPlacer;
        [SerializeField] private MonoBehaviour[] notInARun = new MonoBehaviour[0];

        public static InvasionMode Instance { get; private set; }

        /// <summary>This game is an Invasion run.</summary>
        public static bool IsActive => Instance != null && Instance._active;

        private bool _active;

        /// <summary>The castle, the towers and the King are in place.</summary>
        public bool RunStarted { get; private set; }

        public KingHero King { get; private set; }
        public BuildingInstance Castle { get; private set; }

        private void Awake()
        {
            Instance = this;
            _active = GameSessionIntent.NewGameIsInvasion;
            GameSessionIntent.NewGameIsInvasion = false;
            if (!_active) return;

            foreach (var behaviour in notInARun)
            {
                if (behaviour != null) behaviour.enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private IEnumerator Start()
        {
            if (!_active) yield break;

            // The map is put down in MeshMapApplier's Start; the colliders the NavMesh
            // need come a physics step later.
            for (var frame = 0; frame < 600; frame++)
            {
                var applier = MeshMapApplier.Instance;
                if (applier != null && applier.CurrentMapId == MapId && GridManager.Instance != null) break;
                yield return null;
            }
            yield return new WaitForFixedUpdate();
            Physics.SyncTransforms();

            BeginRun();
        }

        private void BeginRun()
        {
            var applier = MeshMapApplier.Instance;
            var grid = GridManager.Instance;
            if (applier == null || applier.CurrentMap == null || grid == null)
            {
                Debug.LogError("InvasionMode: the Invasion map is not loaded -- the run cannot start.");
                return;
            }

            // The castle stands from the start: the placer is told, so it stops asking for a Town
            // Hall, and the fog closes in round it as it does when a player puts one down.
            var castle = buildingPlacer != null ? buildingPlacer.MandatoryFirstBuilding : null;
            if (castle != null)
            {
                Castle = PlaceFinished(castle, grid.WorldToCell(Vector3.zero) - castle.footprintSize / 2);
                buildingPlacer.MarkMandatoryBuildingAlreadyPlaced();
                buildingPlacer.ClearSelection();
                FogOfWarManager.Instance?.Activate();
            }
            else Debug.LogError("InvasionMode: no Town Hall to stand in the middle -- the run starts without it.");

            // a tower at the island's end of each causeway (the map's "bridge" spots: where each
            // lane crosses the river -- earth, not planks, since 2026-10-21)
            var tower = Find("Tower");
            foreach (var spot in applier.CurrentMap.SpotsOf("bridge"))
            {
                var crossing = new Vector3(spot.position.x, 0f, spot.position.y);
                if (tower != null)
                {
                    var at = crossing - crossing.normalized * TowerInsetMeters;
                    PlaceFinished(tower, grid.WorldToCell(at) - tower.footprintSize / 2);
                }
            }

            var home = (Castle != null ? Castle.transform.position : Vector3.zero) + KingHomeOffset;
            home.y = grid.GroundHeight;
            if (ArmyManager.Instance != null) King = ArmyManager.Instance.SpawnKing(home);

            RunStarted = true;
        }

        private BuildingData Find(string buildingName)
        {
            if (buildingPlacer == null) return null;
            foreach (var data in buildingPlacer.AvailableBuildings)
            {
                if (data != null && data.buildingName == buildingName) return data;
            }
            Debug.LogError($"InvasionMode: no '{buildingName}' among the buildings -- the run starts without it.");
            return null;
        }

        /// <summary>A building standing finished from the start, the way a save puts one back.</summary>
        private static BuildingInstance PlaceFinished(BuildingData data, Vector2Int cell)
        {
            var grid = GridManager.Instance;
            var footprint = data.footprintSize;
            if (data.prefab == null || !grid.IsWithinBounds(cell, footprint)) return null;

            var instance = Instantiate(data.prefab, grid.GetFootprintCenterWorld(cell, footprint), Quaternion.identity);
            var building = instance.GetComponent<BuildingInstance>();
            if (building == null) building = instance.AddComponent<BuildingInstance>();
            building.Initialize(data, cell, 0);
            grid.SetAreaOccupied(cell, footprint, true);
            return building;
        }
    }
}
