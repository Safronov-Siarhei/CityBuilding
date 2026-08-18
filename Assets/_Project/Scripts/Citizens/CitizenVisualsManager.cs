using System;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using CityBuilder.Resources;
using UnityEngine;

namespace CityBuilder.Citizens
{
    /// <summary>
    /// Spawns/despawns a simple cube-built citizen model per population point. Idle citizens
    /// wander ambiently near the town center (see CitizenAgent); citizens assigned as workers on
    /// a ProductionBuilding are pulled from the idle pool and sent to commute between that
    /// building and a nearby ResourceNode (Wood/Stone buildings) or just stay at the building
    /// (Food buildings, which have no tree/rock concept). Population and worker counts stay
    /// owned by CitizenManager / ProductionBuilding respectively — this only mirrors them.
    /// </summary>
    public class CitizenVisualsManager : MonoBehaviour
    {
        public static CitizenVisualsManager Instance { get; private set; }

        [SerializeField] private CitizenManager citizenManager;
        [SerializeField] private GridManager gridManager;

        private static readonly Color[] ClothingColors =
        {
            new Color(0.72f, 0.26f, 0.24f),
            new Color(0.24f, 0.42f, 0.68f),
            new Color(0.32f, 0.55f, 0.28f),
            new Color(0.62f, 0.5f, 0.28f),
            new Color(0.45f, 0.42f, 0.48f),
        };
        private static readonly Color SkinColor = new Color(0.85f, 0.68f, 0.55f);

        private const float NodeRetryIntervalSeconds = 5f;
        // Every citizen used to spawn at the exact same _citizenSpawnPoint -- multiple
        // CharacterControllers landing at literally the same position is a physics edge case
        // Unity doesn't reliably resolve on its own (no separation happens without an actual
        // Move() call), so they'd visually stack into what looked like a single frozen citizen.
        // A small random offset keeps them clustered near the entrance but not coincident.
        private const float SpawnJitterRadius = 0.6f;

        private readonly List<CitizenAgent> _allAgents = new List<CitizenAgent>();

        /// <summary>Every spawned citizen (idle or working) -- e.g. FogOfWarManager scans this instead of a FindObjectsByType scene scan.</summary>
        public IReadOnlyList<CitizenAgent> AllAgents => _allAgents;
        private readonly List<CitizenAgent> _idleAgents = new List<CitizenAgent>();
        private readonly Dictionary<ProductionBuilding, List<CitizenAgent>> _workingAgents = new Dictionary<ProductionBuilding, List<CitizenAgent>>();
        private readonly Dictionary<CitizenAgent, ResourceNode> _claimedNodes = new Dictionary<CitizenAgent, ResourceNode>();
        private readonly Dictionary<CitizenAgent, Action> _workVisitHandlers = new Dictionary<CitizenAgent, Action>();
        private readonly Dictionary<CitizenAgent, Action> _deliveryHandlers = new Dictionary<CitizenAgent, Action>();

        /// <summary>What each worker prised out of a node on its last visit and has not yet walked home. Zero for anyone standing at a building that does not gather.</summary>
        private readonly Dictionary<CitizenAgent, int> _carriedByAgent = new Dictionary<CitizenAgent, int>();

        /// <summary>Gatherers already told, once, that there is nothing left inside their radius -- see ReportRadiusEmpty.</summary>
        private readonly HashSet<ProductionBuilding> _reportedEmptyRadius = new HashSet<ProductionBuilding>();

        private Material[] _clothingMaterials;
        private Material _skinMaterial;
        private Vector3? _townCenter;
        private Vector3? _citizenSpawnPoint;
        private float _nodeRetryTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (citizenManager != null)
            {
                citizenManager.OnPopulationChanged += SyncVisualCount;
            }
            SyncVisualCount();
        }

        private void Update()
        {
            // A worker whose tree was just felled has no node to walk to until the next one
            // grows back (see TreesAreaSpawner's respawn delay) -- periodically retry so it
            // doesn't sit idle-at-building forever once one does.
            _nodeRetryTimer -= Time.deltaTime;
            if (_nodeRetryTimer > 0f) return;
            _nodeRetryTimer = NodeRetryIntervalSeconds;

            foreach (var pair in _workingAgents)
            {
                var building = pair.Key;
                if (!building.GathersFromNodes) continue;

                foreach (var agent in pair.Value)
                {
                    if (!_claimedNodes.ContainsKey(agent))
                    {
                        AssignAgentToBuilding(agent, building);
                    }
                }
            }
        }

        public void RegisterProductionBuilding(ProductionBuilding building)
        {
            if (_workingAgents.ContainsKey(building)) return;

            _workingAgents[building] = new List<CitizenAgent>();
            building.OnAssignedWorkersChanged += () => SyncBuildingWorkers(building);
            SyncBuildingWorkers(building);
        }

        public void UnregisterProductionBuilding(ProductionBuilding building)
        {
            if (!_workingAgents.TryGetValue(building, out var agents)) return;

            foreach (var agent in agents)
            {
                ReleaseAgentToIdle(agent);
            }
            _workingAgents.Remove(building);
        }

        private void SyncVisualCount()
        {
            if (citizenManager == null) return;

            var target = citizenManager.TotalPopulation;

            while (_allAgents.Count < target)
            {
                var agent = SpawnAgent();
                if (agent == null) break; // town center not resolvable yet (Town Hall not placed)
                _allAgents.Add(agent);
                _idleAgents.Add(agent);
            }

            while (_allAgents.Count > target)
            {
                // Prefer removing idle agents first; a mismatched save/load edge case could
                // otherwise leave a building visually short a worker, which self-heals on the
                // next SyncBuildingWorkers pass.
                var agent = _idleAgents.Count > 0 ? _idleAgents[_idleAgents.Count - 1] : _allAgents[_allAgents.Count - 1];
                RemoveAgent(agent);
            }
        }

        private void SyncBuildingWorkers(ProductionBuilding building)
        {
            if (!_workingAgents.TryGetValue(building, out var agents)) return;

            while (agents.Count < building.AssignedWorkers)
            {
                var agent = PullIdleAgent();
                if (agent == null) break; // no idle agents available yet
                agents.Add(agent);
                AssignAgentToBuilding(agent, building);
            }

            while (agents.Count > building.AssignedWorkers)
            {
                var agent = agents[agents.Count - 1];
                agents.RemoveAt(agents.Count - 1);
                ReleaseAgentToIdle(agent);
            }
        }

        private CitizenAgent PullIdleAgent()
        {
            if (_idleAgents.Count == 0) return null;
            var agent = _idleAgents[_idleAgents.Count - 1];
            _idleAgents.RemoveAt(_idleAgents.Count - 1);
            return agent;
        }

        private void AssignAgentToBuilding(CitizenAgent agent, ProductionBuilding building)
        {
            var buildingInstance = building.GetComponent<BuildingInstance>();
            var buildingPos = buildingInstance != null && gridManager != null
                ? gridManager.GetFootprintCenterWorld(buildingInstance.OriginCell, buildingInstance.RotatedFootprint())
                : building.transform.position;

            ResourceNode claimed = null;
            if (building.GathersFromNodes)
            {
                // Already holding one (a boulder with stone left in it) -- keep going back to the
                // same rock rather than re-searching every trip and hopping between two of them.
                if (_claimedNodes.TryGetValue(agent, out var held) && held != null && !held.IsDepleted)
                {
                    claimed = held;
                }
                else
                {
                    var node = FindNearestFreeNode(buildingPos, building.ProducesResource, building.HarvestRadius);
                    if (node != null && node.TryClaim())
                    {
                        _claimedNodes[agent] = node;
                        claimed = node;
                    }
                }

                ReportRadiusEmpty(building, claimed == null);
            }

            agent.SetWorking(buildingPos, claimed);
            EnsureWorkVisitSubscription(agent, building);
        }

        /// <summary>
        /// Says once that a gatherer has nothing left within reach, and stays quiet until it does
        /// again -- the same latch ProductionBuilding uses for a starved converter, and needed for
        /// the same reason: the node search retries every few seconds forever.
        ///
        /// This is the one message a player genuinely cannot work out for themselves. A quarry
        /// whose boulders are gone looks exactly like a quarry that is working, because the
        /// workers still stand on the plot -- and stone never comes back, so "wait a while" is the
        /// wrong thing to conclude.
        /// </summary>
        private void ReportRadiusEmpty(ProductionBuilding building, bool empty)
        {
            var wasEmpty = _reportedEmptyRadius.Contains(building);
            if (!empty)
            {
                if (wasEmpty) _reportedEmptyRadius.Remove(building);
                return;
            }

            if (wasEmpty) return;
            _reportedEmptyRadius.Add(building);
            EventLogManager.Instance?.Log(Localization.Format("#log_nothing_in_radius", building.DisplayName));
        }

        /// <summary>
        /// One-time (per agent) subscription to CitizenAgent.OnWorkVisitCompleted, torn down in
        /// ReleaseAgentToIdle/RemoveAgent. Re-running AssignAgentToBuilding for the same
        /// still-working agent (e.g. the periodic retry, or a fresh node after a harvest) is a
        /// no-op here since the closure below already targets the right building.
        /// </summary>
        private void EnsureWorkVisitSubscription(CitizenAgent agent, ProductionBuilding building)
        {
            if (_workVisitHandlers.ContainsKey(agent)) return;

            void Handler() => HandleWorkVisitCompleted(agent, building);
            agent.OnWorkVisitCompleted += Handler;
            _workVisitHandlers[agent] = Handler;

            void DeliveryHandler() => HandleReturnedToBuilding(agent, building);
            agent.OnReturnedToBuilding += DeliveryHandler;
            _deliveryHandlers[agent] = DeliveryHandler;
        }

        /// <summary>
        /// A citizen just finished one dig at its claimed node: a slice comes out of the node and
        /// onto the citizen's back. It is banked at the building, not here -- see
        /// HandleReturnedToBuilding.
        ///
        /// A node emptied by this visit leaves the map. A tree always is, in one visit, and the
        /// forest grows another; a boulder usually is not, and the same worker walks back to the
        /// same rock until it is.
        /// </summary>
        private void HandleWorkVisitCompleted(CitizenAgent agent, ProductionBuilding building)
        {
            if (!_claimedNodes.TryGetValue(agent, out var node) || node == null) return;

            _carriedByAgent[agent] = node.TakeYield();
            if (!node.IsDepleted) return;

            _claimedNodes.Remove(agent);
            node.Release();
            DespawnNode(node);
        }

        /// <summary>
        /// The worker got home. Whatever came out of the node is the settlement's now -- and this,
        /// not any timer, is the entire income of every Sawmill and Quarry standing (see
        /// ProductionBuilding.Update, which steps aside for them).
        ///
        /// Then straight back out: a re-search here is what finds the next tree once this one has
        /// been felled, and what keeps a worker on the same half-worked boulder otherwise.
        /// </summary>
        private void HandleReturnedToBuilding(CitizenAgent agent, ProductionBuilding building)
        {
            if (_carriedByAgent.TryGetValue(agent, out var carried) && carried > 0)
            {
                ResourceManager.Instance?.Add(building.ProducesResource, carried);
            }
            _carriedByAgent[agent] = 0;

            AssignAgentToBuilding(agent, building);
        }

        /// <summary>
        /// Hands an emptied node to whichever spawner owns it. A felled tree comes back after a
        /// while; an exhausted boulder does not, and RockSpawner is where that difference lives.
        /// The plain Destroy fallback covers nodes from the legacy PNG map path, which no spawner
        /// tracks.
        /// </summary>
        private static void DespawnNode(ResourceNode node)
        {
            if (node.ResourceType == ResourceType.Wood && TreesAreaSpawner.Instance != null)
            {
                TreesAreaSpawner.Instance.NotifyTreeHarvested(node.gameObject);
            }
            else if (node.ResourceType == ResourceType.Stone && RockSpawner.Instance != null)
            {
                RockSpawner.Instance.NotifyRockHarvested(node.gameObject);
            }
            else
            {
                Destroy(node.gameObject);
            }
        }

        /// <summary>
        /// The closest unclaimed, fully grown, non-empty node of this kind INSIDE the building's
        /// reach. The radius is the point: where a gatherer is put decides what it can ever work,
        /// and upgrading it is what widens that. A radius of zero means unlimited, which is what
        /// every building that is not a Sawmill or Quarry has and what the hand-gathering path
        /// wants.
        /// </summary>
        private static ResourceNode FindNearestFreeNode(Vector3 fromPosition, ResourceType resourceType, int radiusMetres)
        {
            ResourceNode nearest = null;
            var nearestDistanceSq = float.MaxValue;
            var radiusSq = radiusMetres > 0 ? (float)radiusMetres * radiusMetres : float.MaxValue;

            foreach (var node in ResourceNode.All)
            {
                if (node.IsClaimed || node.ResourceType != resourceType || node.IsDepleted) continue;

                var growth = node.GetComponent<TreeGrowth>();
                if (growth != null && !growth.IsFullyGrown) continue;

                var distanceSq = (node.transform.position - fromPosition).sqrMagnitude;
                if (distanceSq > radiusSq) continue;
                if (distanceSq < nearestDistanceSq)
                {
                    nearestDistanceSq = distanceSq;
                    nearest = node;
                }
            }

            return nearest;
        }

        private void ReleaseAgentToIdle(CitizenAgent agent)
        {
            UnsubscribeWorkVisit(agent);

            if (_claimedNodes.TryGetValue(agent, out var node))
            {
                node.Release();
                _claimedNodes.Remove(agent);
            }

            var center = _townCenter ?? agent.transform.position;
            agent.SetIdleWander(center);
            _idleAgents.Add(agent);
        }

        private void RemoveAgent(CitizenAgent agent)
        {
            UnsubscribeWorkVisit(agent);
            _idleAgents.Remove(agent);
            _allAgents.Remove(agent);
            if (_claimedNodes.TryGetValue(agent, out var node))
            {
                node.Release();
                _claimedNodes.Remove(agent);
            }
            if (agent != null) Destroy(agent.gameObject);
        }

        private void UnsubscribeWorkVisit(CitizenAgent agent)
        {
            if (agent != null && _workVisitHandlers.TryGetValue(agent, out var handler))
            {
                agent.OnWorkVisitCompleted -= handler;
            }
            _workVisitHandlers.Remove(agent);

            if (agent != null && _deliveryHandlers.TryGetValue(agent, out var deliveryHandler))
            {
                agent.OnReturnedToBuilding -= deliveryHandler;
            }
            _deliveryHandlers.Remove(agent);
            _carriedByAgent.Remove(agent);
        }

        private CitizenAgent SpawnAgent()
        {
            var center = ResolveTownCenter();
            if (center == null) return null;

            EnsureMaterials();

            var root = new GameObject("Citizen");
            root.transform.SetParent(transform, false);

            var clothing = _clothingMaterials[UnityEngine.Random.Range(0, _clothingMaterials.Length)];
            AddCubePart(root.transform, "Body", new Vector3(0f, 0.25f, 0f), new Vector3(0.3f, 0.5f, 0.3f), clothing);
            AddCubePart(root.transform, "Head", new Vector3(0f, 0.61f, 0f), new Vector3(0.22f, 0.22f, 0.22f), _skinMaterial);

            // Added before CitizenAgent so its Awake() can find it via GetComponent. Sized to
            // roughly match the cube body+head (feet at local y=0, top of head around y=0.72),
            // so citizens now physically collide with building colliders instead of walking
            // through them.
            var controller = root.AddComponent<CharacterController>();
            controller.height = 0.72f;
            controller.radius = 0.15f;
            controller.center = new Vector3(0f, 0.36f, 0f);
            controller.skinWidth = 0.02f;
            controller.minMoveDistance = 0f;

            var spawnBase = _citizenSpawnPoint ?? center.Value;
            var jitter = UnityEngine.Random.insideUnitCircle * SpawnJitterRadius;
            var spawnPosition = spawnBase + new Vector3(jitter.x, 0f, jitter.y);

            var agent = root.AddComponent<CitizenAgent>();
            agent.Initialize(spawnPosition, center.Value);

            return agent;
        }

        private Vector3? ResolveTownCenter()
        {
            if (_townCenter.HasValue) return _townCenter;
            if (gridManager == null) return null;

            foreach (var instance in FindObjectsByType<BuildingInstance>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (instance.Data != null && instance.Data.buildingName == BuildingIds.TownHall)
                {
                    var footprint = instance.RotatedFootprint();
                    _townCenter = gridManager.GetFootprintCenterWorld(instance.OriginCell, footprint);

                    // The footprint center sits inside the Town Hall's own solid BoxCollider --
                    // fine when citizens just walked through it, but now that they carry a
                    // CharacterController (physical collision with buildings) spawning there
                    // would trap them inside its walls from frame one. Spawn just south of the
                    // building instead, past its footprint edge (matching the procedurally
                    // generated entrance, which also faces -Z -- see SetupProject.CreateTownHallPrefab).
                    var southEdgeZ = _townCenter.Value.z - footprint.y * gridManager.CellSize * 0.5f;
                    _citizenSpawnPoint = new Vector3(_townCenter.Value.x, _townCenter.Value.y, southEdgeZ - gridManager.CellSize);

                    return _townCenter;
                }
            }

            return null;
        }

        private void EnsureMaterials()
        {
            if (_clothingMaterials != null) return;

            _clothingMaterials = new Material[ClothingColors.Length];
            for (var i = 0; i < ClothingColors.Length; i++)
            {
                _clothingMaterials[i] = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = ClothingColors[i] };
            }
            _skinMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = SkinColor };
        }

        private static void AddCubePart(Transform parent, string partName, Vector3 localPosition, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = partName;
            Destroy(go.GetComponent<BoxCollider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
