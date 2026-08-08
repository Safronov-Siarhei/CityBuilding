using System;
using System.Collections.Generic;
using CityBuilder.Buildings;
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

        private readonly List<CitizenAgent> _allAgents = new List<CitizenAgent>();
        private readonly List<CitizenAgent> _idleAgents = new List<CitizenAgent>();
        private readonly Dictionary<ProductionBuilding, List<CitizenAgent>> _workingAgents = new Dictionary<ProductionBuilding, List<CitizenAgent>>();
        private readonly Dictionary<CitizenAgent, ResourceNode> _claimedNodes = new Dictionary<CitizenAgent, ResourceNode>();
        private readonly Dictionary<CitizenAgent, Action> _workVisitHandlers = new Dictionary<CitizenAgent, Action>();

        private Material[] _clothingMaterials;
        private Material _skinMaterial;
        private Vector3? _townCenter;
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
                var resourceType = building.ProducesResource;
                if (resourceType != ResourceType.Wood && resourceType != ResourceType.Stone) continue;

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
                ? gridManager.GetFootprintCenterWorld(buildingInstance.OriginCell, buildingInstance.Data.footprintSize)
                : building.transform.position;

            Vector3? nodePos = null;
            var resourceType = building.ProducesResource;
            if (resourceType == ResourceType.Wood || resourceType == ResourceType.Stone)
            {
                var node = FindNearestFreeNode(buildingPos, resourceType);
                if (node != null && node.TryClaim())
                {
                    _claimedNodes[agent] = node;
                    nodePos = node.transform.position;
                }
            }

            agent.SetWorking(buildingPos, nodePos);
            EnsureWorkVisitSubscription(agent, building);
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
        }

        /// <summary>A citizen just finished one visit at its claimed node. Wood nodes (real trees) are felled by a single visit; Stone (no node source yet) and anything else is left alone.</summary>
        private void HandleWorkVisitCompleted(CitizenAgent agent, ProductionBuilding building)
        {
            if (!_claimedNodes.TryGetValue(agent, out var node) || node == null) return;
            if (node.ResourceType != ResourceType.Wood) return;

            _claimedNodes.Remove(agent);
            TreesAreaSpawner.Instance?.NotifyTreeHarvested(node.gameObject);
            AssignAgentToBuilding(agent, building); // re-search; comes up empty until the tree respawns
        }

        private static ResourceNode FindNearestFreeNode(Vector3 fromPosition, ResourceType resourceType)
        {
            ResourceNode nearest = null;
            var nearestDistanceSq = float.MaxValue;

            foreach (var node in FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (node.IsClaimed || node.ResourceType != resourceType) continue;

                var growth = node.GetComponent<TreeGrowth>();
                if (growth != null && !growth.IsFullyGrown) continue;

                var distanceSq = (node.transform.position - fromPosition).sqrMagnitude;
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

            var agent = root.AddComponent<CitizenAgent>();
            agent.Initialize(center.Value);

            return agent;
        }

        private Vector3? ResolveTownCenter()
        {
            if (_townCenter.HasValue) return _townCenter;
            if (gridManager == null) return null;

            foreach (var instance in FindObjectsByType<BuildingInstance>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (instance.Data != null && instance.Data.buildingName == "TownHall")
                {
                    _townCenter = gridManager.GetFootprintCenterWorld(instance.OriginCell, instance.Data.footprintSize);
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
