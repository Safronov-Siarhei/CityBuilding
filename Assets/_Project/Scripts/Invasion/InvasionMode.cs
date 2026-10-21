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
    /// lanes over three bridges, and the King as a hero who levels up in the jungle between them.
    ///
    /// Sits on GameManagers in every game and does nothing unless the menu's «В бой» started the
    /// game (GameSessionIntent.NewGameIsInvasion). Then it switches off what a run does not have
    /// -- the sandbox's raids and camps, illness, fires -- and, once the map is down, sets the
    /// run up: the castle in the middle, a tower at the head of each bridge, the bridges, the King.
    /// The waves and the jungle's monsters come in later steps.
    /// </summary>
    public class InvasionMode : MonoBehaviour
    {
        public const string MapId = "Inv1";

        /// <summary>How far in from a bridge's middle its tower stands, on the island.</summary>
        private const float TowerInsetMeters = 11f;

        /// <summary>A bridge's half width and how far along the lane its planks may reach either side of the bridge spot.</summary>
        private const float BridgeHalfWidth = 1.2f;
        private const float BridgeReach = 8f;

        /// <summary>Where the King stands at the start and comes back to: in front of the castle, towards the south bridges.</summary>
        private static readonly Vector3 KingHomeOffset = new Vector3(0f, 0f, -7f);

        [SerializeField] private BuildingPlacer buildingPlacer;
        [SerializeField] private MonoBehaviour[] notInARun = new MonoBehaviour[0];

        public static InvasionMode Instance { get; private set; }

        /// <summary>This game is an Invasion run.</summary>
        public static bool IsActive => Instance != null && Instance._active;

        private bool _active;

        /// <summary>The castle, the towers, the bridges and the King are in place.</summary>
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

            // The map is put down in MeshMapApplier's Start; the colliders a bridge and the NavMesh
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

            var tower = Find("Tower");
            var bridge = Find("Bridge");
            foreach (var spot in applier.CurrentMap.SpotsOf("bridge"))
            {
                var across = new Vector3(spot.position.x, 0f, spot.position.y);
                var direction = across.normalized;
                if (tower != null)
                {
                    var at = across - direction * TowerInsetMeters;
                    PlaceFinished(tower, grid.WorldToCell(at) - tower.footprintSize / 2);
                }
                if (bridge != null) LayBridge(bridge, across, direction);
            }

            var home = (Castle != null ? Castle.transform.position : Vector3.zero) + KingHomeOffset;
            home.y = grid.GroundHeight;
            if (ArmyManager.Instance != null) King = ArmyManager.Instance.SpawnKing(home);

            RunStarted = true;
        }

        /// <summary>
        /// Planks over every shallow water cell of the reach, along the lane and a couple of metres
        /// wide -- wide enough that a lane at an angle still joins up corner to corner.
        /// </summary>
        private void LayBridge(BuildingData bridge, Vector3 middle, Vector3 direction)
        {
            var applier = MeshMapApplier.Instance;
            var grid = GridManager.Instance;
            var centre = grid.WorldToCell(middle);
            var reach = Mathf.CeilToInt(BridgeReach / grid.CellSize) + 1;
            var side = new Vector3(-direction.z, 0f, direction.x);
            var radius = middle.magnitude;
            for (var dx = -reach; dx <= reach; dx++)
            {
                for (var dz = -reach; dz <= reach; dz++)
                {
                    var cell = centre + new Vector2Int(dx, dz);
                    var world = grid.CellToWorld(cell);
                    world.y = 0f;
                    if (Mathf.Abs(Vector3.Dot(world, direction) - radius) > BridgeReach) continue;
                    if (Mathf.Abs(Vector3.Dot(world, side)) > BridgeHalfWidth) continue;
                    if (!applier.IsWaterCell(cell) || !applier.IsWaterPlacementZone(cell)) continue;
                    if (!grid.IsAreaFree(cell, Vector2Int.one)) continue;
                    PlaceFinished(bridge, cell);
                }
            }
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
