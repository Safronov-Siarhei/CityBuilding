using System;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using CityBuilder.Resources;
using UnityEngine;
using UnityEngine.Rendering;

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
        /// <summary>
        /// The townsfolk's looks (Tools/blender/villagers.py, Models/Citizens: four kinds in three
        /// colourings), one picked at random per citizen; with none, the old two cubes.
        /// </summary>
        [SerializeField] private GameObject[] villagerModels = new GameObject[0];
        /// <summary>The work tools by WorkKind - 1 (Tools/blender/tools.py: axe, pick, rod, hoe, basket, pail).</summary>
        [SerializeField] private GameObject[] toolModels = new GameObject[0];
        /// <summary>Each model's main colour -- what most of it is -- measured at project setup, parallel to villagerModels.</summary>
        [SerializeField] private Color[] villagerClothes = new Color[0];
        /// <summary>Each model's face (render_portrait.py), parallel to villagerModels: the workforce panel's row.</summary>
        [SerializeField] private Sprite[] villagerPortraits = new Sprite[0];

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

        // How many citizens this town has had: the next one is named after it. Counted, not taken
        // from _allAgents.Count, so a newcomer never inherits the name of one who left.
        private int _namesGiven;

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
            CitizenWaddle.ToolModels = toolModels;
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
            Rehome();

            foreach (var pair in _workingAgents)
            {
                var building = pair.Key;
                if (!building.GathersFromNodes) continue;

                foreach (var agent in pair.Value)
                {
                    // Not one on his way home with a load: a tree is felled in one visit, so its
                    // claim is gone the moment he picks up the wood, and re-assigning him here
                    // turned him round to the next tree (or kept restarting his rest at the
                    // sawmill) before he could hand it in. Every sawmill on every map earned
                    // nothing (the user, 2026-10-20). He gets his next tree when he delivers.
                    if (!_claimedNodes.ContainsKey(agent) && CarriedBy(agent) == 0)
                    {
                        AssignAgentToBuilding(agent, building);
                    }
                }
            }
        }

        /// <summary>Where a citizen works, or null for one who has no job -- for the citizens' list.</summary>
        public ProductionBuilding WorkplaceOf(CitizenAgent agent)
        {
            foreach (var pair in _workingAgents)
            {
                if (pair.Value.Contains(agent)) return pair.Key;
            }
            return null;
        }

        /// <summary>What a worker has on his back from the last dig, on the way home.</summary>
        public int CarriedBy(CitizenAgent agent) => _carriedByAgent.TryGetValue(agent, out var carried) ? carried : 0;

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

            var grew = false;
            while (_allAgents.Count < target)
            {
                var agent = SpawnAgent();
                if (agent == null) break; // town center not resolvable yet (Town Hall not placed)
                _allAgents.Add(agent);
                _idleAgents.Add(agent);
                grew = true;
            }
            if (grew) RefillShortBuildings();

            while (_allAgents.Count > target)
            {
                // Prefer removing idle agents first; a mismatched save/load edge case could
                // otherwise leave a building visually short a worker, which self-heals on the
                // next SyncBuildingWorkers pass.
                // ...and of those, one whose house has more figures than residents (it lost them)
                CountFigures();
                CitizenAgent agent = null;
                for (var i = _idleAgents.Count - 1; i >= 0 && agent == null; i--) if (Surplus(_idleAgents[i])) agent = _idleAgents[i];
                if (agent == null) agent = _idleAgents.Count > 0 ? _idleAgents[_idleAgents.Count - 1] : _allAgents[_allAgents.Count - 1];
                RemoveAgent(agent);
            }
        }

        private void SyncBuildingWorkers(ProductionBuilding building)
        {
            if (!_workingAgents.TryGetValue(building, out var agents)) return;

            while (agents.Count < building.AssignedWorkers)
            {
                var agent = PullIdleAgent(building);
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

        /// <summary>
        /// An idle figure for this workplace: one living on its island (the user, 2026-11-12: work goes
        /// by island). The town's ledger already allowed the place (HousePeople.IdleOn), so one should be
        /// there; if the figures are out of step, any idle one is moved over to the workplace's door.
        /// </summary>
        private CitizenAgent PullIdleAgent(ProductionBuilding building)
        {
            if (_idleAgents.Count == 0) return null;
            var island = building != null ? building.Island : -1;
            var index = _idleAgents.Count - 1;
            for (var i = _idleAgents.Count - 1; i >= 0; i--)
            {
                if (HousePeople.Same(HomeIsland(_idleAgents[i]), island)) { index = i; break; }
            }
            var agent = _idleAgents[index];
            _idleAgents.RemoveAt(index);
            if (!HousePeople.Same(HomeIsland(agent), island) && building != null && building.Instance != null)
                agent.Teleport(DoorOf(building.Instance));
            return agent;
        }

        private static int HomeIsland(CitizenAgent agent) =>
            agent != null && agent.Home != null ? HousePeople.IslandOf(agent.Home) : -1;

        // ---------------------------------------------------------------- homes (HousePeople, 2026-11-12)

        private readonly List<KeyValuePair<BuildingInstance, int>> _homes = new List<KeyValuePair<BuildingInstance, int>>();
        private readonly Dictionary<BuildingInstance, int> _figuresAt = new Dictionary<BuildingInstance, int>();

        /// <summary>Just outside a building's south edge, where its door faces (as the Town Hall's spawn point).</summary>
        public Vector3 DoorOf(BuildingInstance building)
        {
            if (building == null) return Vector3.zero;
            if (gridManager == null) return building.transform.position;
            var footprint = building.RotatedFootprint();
            var centre = gridManager.GetFootprintCenterWorld(building.OriginCell, footprint);
            return new Vector3(centre.x, centre.y, centre.z - footprint.y * gridManager.CellSize * 0.5f - gridManager.CellSize);
        }

        private Vector3 CentreOf(BuildingInstance building)
        {
            if (building == null) return Vector3.zero;
            return gridManager == null ? building.transform.position
                : gridManager.GetFootprintCenterWorld(building.OriginCell, building.RotatedFootprint());
        }

        private void CountFigures()
        {
            _figuresAt.Clear();
            foreach (var agent in _allAgents)
            {
                if (agent == null || agent.Home == null) continue;
                _figuresAt.TryGetValue(agent.Home, out var n);
                _figuresAt[agent.Home] = n + 1;
            }
        }

        /// <summary>A home with more residents than figures living there, or null (counts from CountFigures).</summary>
        private BuildingInstance HomeWanting()
        {
            var people = HousePeople.Instance;
            if (people == null) return null;
            people.Homes(_homes);
            foreach (var pair in _homes)
            {
                if (pair.Key == null) continue;
                _figuresAt.TryGetValue(pair.Key, out var n);
                if (pair.Value > n) return pair.Key;
            }
            return null;
        }

        /// <summary>Whether this figure's home has more figures than residents (or is gone): the one to let go first.</summary>
        private bool Surplus(CitizenAgent agent)
        {
            if (agent == null) return false;
            if (agent.Home == null || agent.Home.CurrentHealth <= 0) return true;
            var people = HousePeople.Instance;
            if (people == null) return false;
            _figuresAt.TryGetValue(agent.Home, out var n);
            return n > people.CitizensOf(agent.Home);
        }

        /// <summary>Idle figures whose home has too many move to a home with too few -- straight there when it is on another island.</summary>
        private void Rehome()
        {
            if (HousePeople.Instance == null) return;
            CountFigures();
            for (var i = 0; i < _idleAgents.Count; i++)
            {
                var agent = _idleAgents[i];
                if (!Surplus(agent)) continue;
                var home = HomeWanting();
                if (home == null) return;
                if (agent.Home == null || !HousePeople.Same(HomeIsland(agent), HousePeople.IslandOf(home))) agent.Teleport(DoorOf(home));
                agent.Home = home;
                agent.SetIdleWander(CentreOf(home));
                CountFigures();
            }
        }

        private void AssignAgentToBuilding(CitizenAgent agent, ProductionBuilding building)
        {
            var buildingInstance = building.GetComponent<BuildingInstance>();
            var buildingPos = buildingInstance != null && gridManager != null
                ? gridManager.GetFootprintCenterWorld(buildingInstance.OriginCell, buildingInstance.RotatedFootprint())
                : building.transform.position;

            ResourceNode claimed = null;
            // a yard being built or upgraded (ConstructionSite): its workers wait at it; the node
            // retry in Update sends them out once it works again
            var resting = buildingInstance != null && !buildingInstance.IsOperational;
            // ...and lets go of its rock: a claim kept through the rest also kept the retry from ever
            // sending it out again, and after an upgrade the quarry's men stood idle at it until
            // taken off and put back (playtest 5, 2026-10-22)
            if (resting && _claimedNodes.TryGetValue(agent, out var dropped))
            {
                if (dropped != null) dropped.Release();
                _claimedNodes.Remove(agent);
            }
            if (building.GathersFromNodes && !resting)
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

            var reach = 0f;
            if (buildingInstance != null && gridManager != null)
            {
                var footprint = buildingInstance.RotatedFootprint();
                reach = Mathf.Max(footprint.x, footprint.y) * gridManager.CellSize * 0.5f + 0.6f;
            }
            // his trade's tool and, with no tree or rock, his own spots to work at (field, orchard, pen, shore)
            agent.SetWorking(buildingPos, claimed, reach,
                resting ? WorkKind.None : WorkSpots.KindOf(building.BuildingId), resting ? null : WorkSpots.For(building));
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
            EventLogManager.Instance?.Post(GameEvent.About(building.GetComponent<BuildingInstance>(), Localization.Format("#log_nothing_in_radius", building.DisplayName), EventSeverity.Warning, EventGlyph.Alert));
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

            // Added, not set: whatever he already carries is not thrown away.
            _carriedByAgent[agent] = CarriedBy(agent) + node.TakeYield();
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
        /// <summary>A worker brought a load into his yard: the building and how much (the «+1» over it, ResourceGainPopups).</summary>
        public static event System.Action<ProductionBuilding, int> Delivered;

        private void HandleReturnedToBuilding(CitizenAgent agent, ProductionBuilding building)
        {
            if (_carriedByAgent.TryGetValue(agent, out var carried) && carried > 0)
            {
                ResourceManager.Instance?.AddProduced(building.ProducesResource, carried);
                Delivered?.Invoke(building, carried);
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
        /// <summary>Whether a gatherer could take this node now: not claimed, not worked out, a tree full grown. What HarvestRadiusOverlay marks.</summary>
        public static bool IsGatherable(ResourceNode node)
        {
            if (node == null || node.IsClaimed || node.IsDepleted) return false;
            var growth = node.GetComponent<TreeGrowth>();
            return growth == null || growth.IsFullyGrown;
        }

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

                // across the ground, not through it: on a slope a tree inside the drawn radius
                // measured farther than it (the user, 2026-10-22: trees in the circle left alone)
                var offset = node.transform.position - fromPosition;
                offset.y = 0f;
                var distanceSq = offset.sqrMagnitude;
                if (distanceSq > radiusSq) continue;
                if (distanceSq < nearestDistanceSq)
                {
                    nearestDistanceSq = distanceSq;
                    nearest = node;
                }
            }

            return nearest;
        }

        /// <summary>
        /// A villager falls ill (the sandbox Лечебница, Infirmary): an idle one if there is one, else
        /// one taken off his work (the place stays wanted, someone else steps in). He leaves the idle
        /// and working lists -- the Infirmary walks him from now on -- but stays one of the town's figures.
        /// </summary>
        public CitizenAgent TakeForPatient(Infirmary infirmary, BuildingInstance home = null)
        {
            CitizenAgent agent = null;
            // a loaded patient: one living in his own house, if one is free
            if (home != null)
            {
                for (var i = 0; i < _idleAgents.Count; i++)
                {
                    if (_idleAgents[i] == null || _idleAgents[i].Home != home) continue;
                    agent = _idleAgents[i];
                    _idleAgents.RemoveAt(i);
                    return agent;
                }
            }
            if (_idleAgents.Count > 0)
            {
                var index = UnityEngine.Random.Range(0, _idleAgents.Count);
                agent = _idleAgents[index];
                _idleAgents.RemoveAt(index);
                return agent;
            }
            foreach (var pair in _workingAgents)
            {
                if (pair.Value.Count == 0) continue;
                agent = pair.Value[pair.Value.Count - 1];
                pair.Value.RemoveAt(pair.Value.Count - 1);
                UnsubscribeWorkVisit(agent);
                if (_claimedNodes.TryGetValue(agent, out var node))
                {
                    node.Release();
                    _claimedNodes.Remove(agent);
                }
                if (pair.Key != null) pair.Key.LayOffWorker();
                agent.SetIdleWander(agent.Home != null && agent.Home.CurrentHealth > 0 ? CentreOf(agent.Home) : agent.transform.position);
                return agent;
            }
            return null;
        }

        /// <summary>Cured: back at the Лечебница's door, an idle villager again.</summary>
        public void ReturnFromPatient(CitizenAgent agent, Vector3 at)
        {
            if (agent == null || !_allAgents.Contains(agent) || _idleAgents.Contains(agent)) return;
            agent.Teleport(at);
            ReleaseAgentToIdle(agent);
            RefillShortBuildings();
        }

        private void ReleaseAgentToIdle(CitizenAgent agent)
        {
            UnsubscribeWorkVisit(agent);

            if (_claimedNodes.TryGetValue(agent, out var node))
            {
                node.Release();
                _claimedNodes.Remove(agent);
            }

            var center = agent.Home != null && agent.Home.CurrentHealth > 0 ? CentreOf(agent.Home) : (_townCenter ?? agent.transform.position);
            agent.SetIdleWander(center);
            _idleAgents.Add(agent);
        }

        /// <summary>
        /// This citizen, struck down (an orc in an Invasion run): the very one goes, and the town
        /// counts one fewer. Taken out of the lists first, so the population's sync does not take
        /// somebody else as well.
        /// </summary>
        public void KillAgent(CitizenAgent agent)
        {
            if (agent == null || !_allAgents.Contains(agent)) return;
            foreach (var pair in _workingAgents) pair.Value.Remove(agent);
            HousePeople.Instance?.Lose(agent.Home);   // this very one's house is one short
            RemoveAgent(agent);
            citizenManager?.KillCitizens(1);
            RefillShortBuildings();
        }

        /// <summary>
        /// A building still counts the worker who was killed (the town lost an idle man in his place,
        /// CitizenManager.RemoveCitizens), so it is short a figure: an idle one steps in now, or the
        /// next one born does. Before this a yard said «4 workers» with two at work until they were
        /// all taken off and put back (playtest 5, 2026-10-22).
        /// </summary>
        private void RefillShortBuildings()
        {
            if (_idleAgents.Count == 0) return;
            _shortBuildings.Clear();
            foreach (var pair in _workingAgents)
            {
                if (pair.Value.Count < pair.Key.AssignedWorkers) _shortBuildings.Add(pair.Key);
            }
            foreach (var building in _shortBuildings) SyncBuildingWorkers(building);
        }

        private readonly List<ProductionBuilding> _shortBuildings = new List<ProductionBuilding>();

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
            root.transform.localScale = Vector3.one * UnitSize.Scale;

            var pick = villagerModels != null && villagerModels.Length > 0 ? UnityEngine.Random.Range(0, villagerModels.Length) : -1;
            var model = pick >= 0 ? villagerModels[pick] : null;
            var clothes = Color.grey;
            Sprite portrait = null;
            if (model != null)
            {
                AddFigure(root.transform, model);
                if (villagerClothes != null && pick < villagerClothes.Length) clothes = villagerClothes[pick];
                if (villagerPortraits != null && pick < villagerPortraits.Length) portrait = villagerPortraits[pick];
            }
            else
            {
                var clothing = _clothingMaterials[UnityEngine.Random.Range(0, _clothingMaterials.Length)];
                clothes = clothing.color;
                AddCubePart(root.transform, "Body", new Vector3(0f, 0.25f, 0f), new Vector3(0.3f, 0.5f, 0.3f), clothing);
                AddCubePart(root.transform, "Head", new Vector3(0f, 0.61f, 0f), new Vector3(0.22f, 0.22f, 0.22f), _skinMaterial);
            }

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

            // at his own house's door (the user, 2026-11-12); the hall's for anyone not yet housed
            CountFigures();
            var home = HomeWanting();
            var spawnBase = home != null ? DoorOf(home) : (_citizenSpawnPoint ?? center.Value);
            var jitter = UnityEngine.Random.insideUnitCircle * SpawnJitterRadius;
            var spawnPosition = spawnBase + new Vector3(jitter.x, 0f, jitter.y);

            var agent = root.AddComponent<CitizenAgent>();
            agent.Home = home;
            agent.Initialize(spawnPosition, home != null ? CentreOf(home) : center.Value);
            agent.NameIndex = _namesGiven++;
            agent.Clothes = clothes;
            agent.Portrait = portrait;
            root.name = "Citizen " + agent.CitizenName;

            return agent;
        }

        /// <summary>
        /// A townsman who is nobody's: not counted, not housed, not given work -- a figure for a scene to
        /// run about (campaign mission 2: the people running for their houses, the user 2026-11-12).
        /// The caller moves him (CitizenAgent.MoveTo) and destroys him.
        /// </summary>
        public CitizenAgent SpawnLoose(Vector3 at)
        {
            EnsureMaterials();
            var root = new GameObject("Townsman");
            root.transform.SetParent(transform, false);
            root.transform.localScale = Vector3.one * UnitSize.Scale;
            var pick = villagerModels != null && villagerModels.Length > 0 ? UnityEngine.Random.Range(0, villagerModels.Length) : -1;
            if (pick >= 0 && villagerModels[pick] != null) AddFigure(root.transform, villagerModels[pick]);
            else
            {
                var clothing = _clothingMaterials[UnityEngine.Random.Range(0, _clothingMaterials.Length)];
                AddCubePart(root.transform, "Body", new Vector3(0f, 0.25f, 0f), new Vector3(0.3f, 0.5f, 0.3f), clothing);
                AddCubePart(root.transform, "Head", new Vector3(0f, 0.61f, 0f), new Vector3(0.22f, 0.22f, 0.22f), _skinMaterial);
            }
            var controller = root.AddComponent<CharacterController>();
            controller.height = 0.72f;
            controller.radius = 0.15f;
            controller.center = new Vector3(0f, 0.36f, 0f);
            controller.skinWidth = 0.02f;
            controller.minMoveDistance = 0f;
            var agent = root.AddComponent<CitizenAgent>();
            agent.Initialize(at, at);
            return agent;
        }

        private Vector3? ResolveTownCenter()
        {
            if (_townCenter.HasValue) return _townCenter;
            if (gridManager == null) return null;

            foreach (var instance in FindObjectsByType<BuildingInstance>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (instance.IsBot) continue;   // the bot kingdom's, not the town's
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
                _clothingMaterials[i] = new Material(RuntimeShaders.Lit) { color = ClothingColors[i] };
            }
            _skinMaterial = new Material(RuntimeShaders.Lit) { color = SkinColor };
        }

        /// <summary>
        /// A townsman's model under the citizen's root, at its real size (the root is scaled to
        /// UnitSize, as a soldier's is), turned round -- modelled facing Blender's -y -- and
        /// waddling as he walks (CitizenWaddle). No shadow, as the cubes had none.
        /// </summary>
        private static void AddFigure(Transform root, GameObject model)
        {
            var figure = Instantiate(model, root, false);
            figure.name = "Figure";
            figure.transform.localScale = Vector3.one / UnitSize.Scale;
            figure.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * figure.transform.localRotation;
            foreach (var renderer in figure.GetComponentsInChildren<Renderer>()) renderer.shadowCastingMode = ShadowCastingMode.Off;
            SeasonStyle.NoSnow(figure);
            root.gameObject.AddComponent<CitizenWaddle>().Bind(figure.transform);
        }

        private static void AddCubePart(Transform parent, string partName, Vector3 localPosition, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = partName;
            Destroy(go.GetComponent<BoxCollider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = size;

            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            // Two cubes per citizen, up to 150 citizens: casting would put 300 more objects through
            // the shadow pass to draw shadows the player cannot make out at this zoom.
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
    }
}
