using System.Collections;
using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Resources;
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
    /// The waves are InvasionWaves', the jungle's monsters, boss and shrines InvasionJungle's, the
    /// run's points InvasionScore's.
    /// </summary>
    public class InvasionMode : MonoBehaviour
    {
        public const string MapId = "Inv1";

        /// <summary>How far in from a causeway's middle its tower stands: just onto the island, at the causeway's foot.</summary>
        private const float TowerInsetMeters = 7f;

        /// <summary>
        /// Where the six houses stand round the castle on the small island (the user, 2026-10-21):
        /// two in each stretch between the causeways, either side of its grove -- (bearing in
        /// degrees, metres from the middle). Each is nudged to the nearest open ground.
        /// </summary>
        private static readonly Vector2[] HouseSpots =
        {
            new Vector2(15f, 7f), new Vector2(45f, 7f), new Vector2(135f, 7f),
            new Vector2(165f, 7f), new Vector2(255f, 7f), new Vector2(285f, 7f),
        };

        /// <summary>Where the King stands at the start and comes back to: in front of the castle, towards the south causeways.</summary>
        private static readonly Vector3 KingHomeOffset = new Vector3(0f, 0f, -4.5f);

        [SerializeField] private BuildingPlacer buildingPlacer;
        [SerializeField] private MonoBehaviour[] notInARun = new MonoBehaviour[0];
        [SerializeField] private GameObject shrineModel;

        public static InvasionMode Instance { get; private set; }

        /// <summary>This game is an Invasion run.</summary>
        public static bool IsActive => Instance != null && Instance._active;

        private bool _active;

        /// <summary>The castle, the towers and the King are in place.</summary>
        public bool RunStarted { get; private set; }

        public KingHero King { get; private set; }
        public InvasionPeople People { get; private set; }
        public InvasionWaves Waves { get; private set; }
        public InvasionJungle Jungle { get; private set; }
        public InvasionScore Score { get; private set; }
        public List<InvasionOrcCamp> OrcCamps { get; } = new List<InvasionOrcCamp>();
        public List<BuildingInstance> Houses { get; } = new List<BuildingInstance>();
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

            // A run is built of stone (the user, 2026-10-22: too little to dig, everything costs it):
            // the map's boulders hold inv_stone_reserve_mult times what they do in the sandbox.
            ResourceNode.ScaleStock(ResourceType.Stone, BalanceConfig.Instance.InvasionStoneReserveMultiplier);

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

            // the opening stock, with no storehouse limits; nothing else in the treasury
            var config = BalanceConfig.Instance;
            var resources = ResourceManager.Instance;
            if (resources != null)
            {
                resources.UnlimitedStorage = true;
                resources.SetAmount(ResourceType.Wood, config.InvasionStartWood);
                resources.SetAmount(ResourceType.Stone, config.InvasionStartStone);
                resources.SetAmount(InvasionRules.Money, config.InvasionStartCoins);
                resources.SetAmount(ResourceType.Food, 0);
                resources.SetAmount(ResourceType.Gold, 0);
            }

            // the houses and their people
            var house = Find(InvasionRules.House);
            if (house != null)
            {
                for (var i = 0; i < HouseSpots.Length && i < config.InvasionHouses; i++)
                {
                    var spot = HouseSpots[i];
                    var at = new Vector3(Mathf.Cos(spot.x * Mathf.Deg2Rad), 0f, Mathf.Sin(spot.x * Mathf.Deg2Rad)) * spot.y;
                    var placed = PlaceNear(house, at);
                    if (placed != null) Houses.Add(placed);
                }
            }
            People = gameObject.AddComponent<InvasionPeople>();
            People.Settle(Houses);

            var home = (Castle != null ? Castle.transform.position : Vector3.zero) + KingHomeOffset;
            home.y = grid.GroundHeight;
            if (ArmyManager.Instance != null) King = ArmyManager.Instance.SpawnKing(home);
            if (King != null && Castle != null) King.GuardCentre = Castle.transform.position;

            // the three round orc camps at the lanes' far ends (the map's CampSpots, not among
            // SpotsOf: the sandbox's camps read them too), their gates facing the castle
            var castleAt = Castle != null ? Castle.transform.position : Vector3.zero;
            var exits = new List<Vector3>();
            foreach (var spot in applier.CurrentMap.CampSpots)
            {
                var camp = new GameObject("InvasionOrcCamp").AddComponent<InvasionOrcCamp>();
                camp.Build(new Vector3(spot.x, grid.GroundHeight, spot.y), castleAt);
                OrcCamps.Add(camp);
                exits.Add(camp.WaveExit);
            }

            // the waves, out of the camps' portals
            Waves = gameObject.AddComponent<InvasionWaves>();
            Waves.Begin(exits);
            Score = gameObject.AddComponent<InvasionScore>();
            Score.Begin(Waves);

            // the jungle between the lanes: monster camps, the boss's lair, the healing shrines
            Jungle = gameObject.AddComponent<InvasionJungle>();
            var map = applier.CurrentMap;
            Jungle.Begin(map.SpotsOf("monsters"), map.SpotsOf("boss"), map.SpotsOf("shrine"), shrineModel);

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

        /// <summary>The building finished on the nearest open ground to `world` -- dry, not on a slope, nothing in the way.</summary>
        private static BuildingInstance PlaceNear(BuildingData data, Vector3 world)
        {
            var grid = GridManager.Instance;
            var applier = MeshMapApplier.Instance;
            var footprint = data.footprintSize;
            var origin = grid.WorldToCell(world) - footprint / 2;
            for (var ring = 0; ring <= 8; ring++)
            {
                for (var dx = -ring; dx <= ring; dx++)
                {
                    for (var dz = -ring; dz <= ring; dz++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != ring) continue;
                        var cell = origin + new Vector2Int(dx, dz);
                        if (!grid.IsWithinBounds(cell, footprint) || !grid.IsAreaFree(cell, footprint)) continue;
                        if (!OpenGround(applier, cell, footprint)) continue;
                        return PlaceFinished(data, cell);
                    }
                }
            }
            Debug.LogError($"InvasionMode: no open ground for a {data.buildingName} near {world}.");
            return null;
        }

        private static bool OpenGround(MeshMapApplier applier, Vector2Int cell, Vector2Int footprint)
        {
            if (applier == null) return true;
            for (var x = 0; x < footprint.x; x++)
            {
                for (var z = 0; z < footprint.y; z++)
                {
                    var c = cell + new Vector2Int(x, z);
                    if (applier.IsWaterCell(c) || applier.IsNoBuildCell(c)) return false;
                }
            }
            return true;
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
