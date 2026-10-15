using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// The map's raid sources: opens every portal the template calls for once the Town Hall is
    /// down (see PortalPlacement for where they go and why), then periodically sends a squad of
    /// OrcUnits out of one of them. Closing them all is how the map is won -- see OrcPortal and
    /// GameOverManager.
    ///
    /// Raid size, strength and pacing are read against the player's progression score rather than
    /// the calendar (see PlayerProgression), and all three come from the balance sheet.
    ///
    /// ONE wave per interval, out of a portal picked at random from those still standing --
    /// deliberately not one wave per portal. Five portals each running their own clock would put
    /// five simultaneous squads on the settlement on day one, when the player has no coins and no
    /// soldiers; this way opening the map up to five objectives does not multiply the pressure on
    /// the way there, it multiplies the work of finishing it. What closing a portal buys the player
    /// beyond one fewer direction to watch is a balance question still open with the user.
    /// </summary>
    public class OrcRaidManager : MonoBehaviour
    {
        public static OrcRaidManager Instance { get; private set; }

        // Raid pacing, size and strength all come from the balance sheet's economy tab (raid_*),
        // and all three are read against the PLAYER'S PROGRESSION SCORE rather than the calendar
        // (see PlayerProgression). The day count used to do this job, and it measured how long the
        // player had sat there rather than what they had built.

        // Where the portals go is PortalPlacement's job now; what is left here is the search that
        // turns one of its slots into a cell that is actually standable.
        private const int PortalDirectionSamples = 16;
        // How far in and out along the bearing the search will walk, and in what steps. Deliberately
        // long: moving a portal thirty metres further out along its OWN direction costs the design
        // nothing, while turning it far enough to sit beside its neighbour costs the spread, which
        // is the point of having five of them. So the distance gives way and the bearing does not.
        private const float PortalRadiusStepMeters = 4f;
        private const float PortalMaxRadiusDeltaMeters = 32f;
        // Revealed permanently so the portal is visible from the start -- explicitly a testing
        // convenience the user asked for; the intended behaviour is for it to stay hidden until
        // the player scouts it.
        private const int PortalFogRevealRadiusCells = 12;
        private const string TownHallBuildingName = BuildingIds.TownHall;

        private static readonly Color PortalColor = new Color(0.32f, 0.1f, 0.42f);
        private static readonly Color OrcSkinColor = new Color(0.28f, 0.42f, 0.22f);
        private static readonly Color OrcGearColor = new Color(0.22f, 0.2f, 0.18f);

        // The faction's Blender models (Tools/blender/orc*.py, exported to Models/Orcs), wired by
        // SetupProject. Any left empty falls back to the cube figure it replaced, so a build
        // without the models still plays.
        [SerializeField] private GameObject portalActiveModel;
        [SerializeField] private GameObject portalPassiveModel;
        [SerializeField] private GameObject portalClosedModel;
        [SerializeField] private GameObject gruntModel;
        [SerializeField] private GameObject bruteModel;
        [SerializeField] private GameObject throwerModel;
        [SerializeField] private GameObject chiefModel;
        // The camp around each portal (see OrcCamp): its three buildings, their ruins, and the
        // decorative palisade pieces flanking the totem.
        [SerializeField] private GameObject watchtowerModel;
        [SerializeField] private GameObject watchtowerRuinModel;
        [SerializeField] private GameObject warTentModel;
        [SerializeField] private GameObject warTentRuinModel;
        [SerializeField] private GameObject totemModel;
        [SerializeField] private GameObject totemRuinModel;
        [SerializeField] private GameObject palisadeModel;

        /// <summary>
        /// The level from which a raider is drawn as the brute rather than the grunt (the user's
        /// call, 2026-10-14): the game has one orc that grows by level, and a strong one should
        /// look strong. The thrower and the chief are kinds of their own (OrcKind), not levels.
        /// </summary>
        public const int BruteFromLevel = 4;

        /// <summary>A portal's plot: the model is a 2x2 arch on its platform.</summary>
        public static readonly Vector2Int PortalFootprint = new Vector2Int(2, 2);

        private Material _portalMaterial;
        private Material _orcSkinMaterial;
        private Material _orcGearMaterial;
        private bool _portalsPlaced;
        private float _raidTimer;
        private OrcPortal _nextSource;
        private readonly List<Vector2Int> _closedPortalCells = new List<Vector2Int>();

        /// <summary>
        /// The portal the next wave will come out of, picked when the last one left -- and shown
        /// burning bright while the others smoulder, so the player can see where it is coming
        /// from. Which portal it is changes nothing about the raids' size or pace.
        /// </summary>
        public OrcPortal NextRaidSource => _nextSource;

        /// <summary>Where the portals the player destroyed stood -- each leaves its ruin, which the save keeps.</summary>
        public IReadOnlyList<Vector2Int> ClosedPortalCells => _closedPortalCells;

        /// <summary>Pauses the automatic raid clock without touching the portal. Set by the OrcSpawn cheat so hand-spawned squads can be observed in isolation; nothing in normal gameplay sets it.</summary>
        public bool RaidsSuspended { get; set; }

        /// <summary>Whether the map's portals have already been opened -- false only while the player has yet to put down a Town Hall for them to be anchored to.</summary>
        public bool PortalsPlaced => _portalsPlaced;

        /// <summary>How long the settlement has before the next wave. Saved, so reloading is not a way to push the raid clock back to a full interval.</summary>
        public float SecondsUntilNextRaid => _raidTimer;

        /// <summary>One saved camp building: its camp (by portal cell), what it is, where, facing, and its health (0 = its ruin).</summary>
        public readonly struct CampBuildingRestoreState
        {
            public CampBuildingRestoreState(Vector2Int portalCell, OrcCampBuildingKind kind, Vector2Int cell, float yaw, int health)
            {
                PortalCell = portalCell;
                Kind = kind;
                Cell = cell;
                Yaw = yaw;
                Health = health;
            }

            public Vector2Int PortalCell { get; }
            public OrcCampBuildingKind Kind { get; }
            public Vector2Int Cell { get; }
            public float Yaw { get; }
            public int Health { get; }
        }

        /// <summary>One saved portal: where it stood, and the health the player had ground it down to.</summary>
        public readonly struct PortalRestoreState
        {
            public PortalRestoreState(Vector2Int cell, int health)
            {
                Cell = cell;
                Health = health;
            }

            public Vector2Int Cell { get; }
            public int Health { get; }
        }

        /// <summary>
        /// Puts the raid sources back the way the save left them: every portal still standing where
        /// it stood and on the health it had, and the clock where it was.
        ///
        /// The portals come back IN SAVE ORDER, because that is the order a group's saved attack
        /// order indexes into -- see GameSaveController.RestoreArmyOrders.
        /// </summary>
        /// <param name="portalsPlaced">Whether the map had opened its portals at all. True with an empty list means the player had destroyed every one of them, and no fresh ones are opened over the ruins.</param>
        /// <param name="closedPortalCells">Where the portals the player had already destroyed stood: each gets its ruin back.</param>
        /// <param name="campBuildings">Every camp building the save holds, standing or fallen. Empty means a save from before the camps: fresh camps go up round the standing portals.</param>
        public void RestoreFromSave(bool portalsPlaced, IReadOnlyList<PortalRestoreState> portals,
            IReadOnlyList<Vector2Int> closedPortalCells, IReadOnlyList<CampBuildingRestoreState> campBuildings,
            float secondsUntilNextRaid)
        {
            if (!portalsPlaced) return;

            // The flag alone stops Update from opening a fresh set over the ruins of the ones the
            // player fought for.
            _portalsPlaced = true;

            var grid = GridManager.Instance;
            if (grid == null || portals == null) return;

            foreach (var state in portals)
            {
                if (state.Health <= 0) continue;
                SpawnPortal(grid, state.Cell).SetCurrentHealth(state.Health);
            }

            if (closedPortalCells != null)
            {
                foreach (var cell in closedPortalCells)
                {
                    _closedPortalCells.Add(cell);
                    grid.SetAreaOccupied(cell, PortalFootprint, true);
                    var position = grid.GetFootprintCenterWorld(cell, PortalFootprint);
                    SpawnRuin(cell, position, FacingTownHall(position));
                }
            }

            RestoreCamps(grid, campBuildings);

            // Which portal sends the next wave is not saved -- it is shown, not counted on -- so
            // a load picks afresh.
            PickNextSource();
            _raidTimer = Mathf.Max(0f, secondsUntilNextRaid);
        }

        /// <summary>
        /// Puts one saved orc back where it stood, on the health it had. Like the army's restore,
        /// it is deliberately not the spawn path: no jitter, and nothing is announced in the event
        /// log.
        ///
        /// Hands the unit back so the caller can match it to the save's orc list by position in
        /// that list -- which is how a group's attack order finds the orc it was chasing again.
        /// </summary>
        public OrcUnit RestoreOrc(Vector3 position, int level, int currentHealth, OrcKind kind = OrcKind.Raider)
        {
            EnsureMaterials();
            var unit = SpawnOrc(position, level, kind, scatter: false);
            unit.SetCurrentHealth(currentHealth);
            return unit;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            OrcPortal.OnPortalDestroyed += HandlePortalDestroyed;
        }

        private void OnDisable()
        {
            OrcPortal.OnPortalDestroyed -= HandlePortalDestroyed;
        }

        /// <summary>A portal the player has closed leaves its ruin where it stood (still taking up its plot), and if it was the one the next wave was coming from, another takes over.</summary>
        private void HandlePortalDestroyed(OrcPortal portal)
        {
            if (portal == null) return;
            _closedPortalCells.Add(portal.Cell);
            SpawnRuin(portal.Cell, portal.transform.position, portal.transform.rotation);
            if (_nextSource == portal || _nextSource == null) PickNextSource();
        }

        /// <summary>Picks the portal the next wave comes out of, at random among those standing, and lights it.</summary>
        private void PickNextSource()
        {
            var portals = OrcPortal.All;
            _nextSource = portals.Count > 0 ? portals[Random.Range(0, portals.Count)] : null;
            foreach (var portal in portals)
            {
                portal.ShowActive(portal == _nextSource);
            }
        }

        private void Update()
        {
            if (!_portalsPlaced)
            {
                // The portals are positioned relative to the Town Hall, so nothing can happen until
                // the player has placed it. HasAny is an O(1) registry lookup (see
                // BuildingInstance), so polling it per frame is free -- the one real scan below
                // runs exactly once, on the frame the Town Hall appears. Also covers a loaded
                // save, where the Town Hall exists before this component's first Update and no
                // placement event ever fires.
                if (!BuildingInstance.HasAny(TownHallBuildingName)) return;

                TrySpawnPortalsNearTownHall();
                return;
            }

            // Portal placement above still runs while suspended -- the OrcSpawn cheat needs a
            // portal to aim at, it just doesn't want the automatic waves interleaving with the
            // squads it spawns deliberately.
            if (RaidsSuspended) return;

            _raidTimer -= Time.deltaTime;
            if (_raidTimer > 0f) return;

            // The score is read ONCE here and used for all three of size, strength and the wait
            // until the next wave -- it costs a scan over the buildings, and this is the once a
            // minute where that is affordable.
            var score = PlayerProgression.Score();
            _raidTimer = ComputeRaidIntervalSeconds(score);
            SpawnRaid(score);
        }

        /// <summary>
        /// Opens the map's portals: one per slot of PortalPlacement's template, each on solid
        /// ground as near its slot as the map allows. "Solid" is MeshMapApplier.IsGroundAt, i.e.
        /// actually standing on the map's Ground mesh at the flat playable height -- which rules
        /// out the lake and the decorative cliffs, either of which would strand every spawned orc
        /// off the NavMesh and send them walking through the world in a straight line.
        ///
        /// A slot with no standable ground anywhere near it is SKIPPED rather than fatal: a map
        /// whose outermost ring falls in the water should lose that one portal, not all of them.
        /// Losing every slot is the error, because then the map has no objective at all.
        /// </summary>
        private void TrySpawnPortalsNearTownHall()
        {
            var grid = GridManager.Instance;
            if (grid == null) return;

            if (!TryFindTownHallPosition(out var townHallPosition))
            {
                // HasAny said one exists, so this means it was destroyed within the same frame.
                // Nothing to anchor to; try again next frame.
                return;
            }

            // Random starting bearing so repeat playthroughs on the same map don't always put the
            // portals in the exact same places relative to the base.
            var template = PortalPlacement.BuildTemplate(Random.Range(0f, Mathf.PI * 2f));
            var opened = 0;

            var maxBearingDeviation = PortalPlacement.MaxBearingDeviationRadians(template.Count);
            // The first slot's own distance is the nearest any portal may end up, however the
            // search has to move it -- that is what the sheet's portal_distance_from_town_hall
            // means, and the template puts every other slot further out again.
            var nearestAllowed = template[0].DistanceMeters;

            foreach (var slot in template)
            {
                if (!TryFindPortalSpot(grid, townHallPosition, slot, maxBearingDeviation, nearestAllowed, out var cell))
                {
                    Debug.LogWarning($"OrcRaidManager: no valid ground for the portal slot at {slot.DistanceMeters:0.#} m " +
                                     $"from the Town Hall at {townHallPosition} (all water/cliff/occupied). That portal is skipped.");
                    continue;
                }

                var portal = SpawnPortal(grid, cell);
                BuildCamp(grid, portal);
                opened++;
            }

            PickNextSource();

            // Set before the early return below, and whatever happened: the map geometry won't
            // change, so re-running this every frame would just repeat the same failure forever.
            _portalsPlaced = true;

            if (opened == 0)
            {
                Debug.LogError($"OrcRaidManager: not one of the {template.Count} portal slots around the Town Hall at {townHallPosition} " +
                               "found valid ground. No portals will exist, no raids will happen, and the map cannot be won this session.");
                return;
            }

            // Only starts counting once the portals actually exist, so the player isn't raided
            // from nowhere during the time it takes them to place the Town Hall.
            //
            // The full interval rather than the score's, deliberately: the portals open on the
            // frame the Town Hall goes down, when the score IS a Town Hall and five settlers, and
            // asking PlayerProgression for that would buy a scan over the buildings to be told so.
            _raidTimer = BalanceConfig.Instance.RaidIntervalSeconds;
            // The nearest portal is the one the camera goes to: the closest threat to the town.
            EventLogManager.Instance?.Post(GameEvent.At(opened == 1
                    ? Localization.Get("#log_portal_opened")
                    : Localization.Format("#log_portals_opened", opened),
                EventSeverity.Danger, EventGlyph.Portal, null, NearestPortal(townHallPosition)));
        }

        private static Vector3 NearestPortal(Vector3 from)
        {
            var nearest = from;
            var best = float.MaxValue;
            foreach (var portal in OrcPortal.All)
            {
                var distance = (portal.transform.position - from).sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                nearest = portal.transform.position;
            }
            return nearest;
        }

        private static bool TryFindTownHallPosition(out Vector3 position)
        {
            foreach (var instance in FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
            {
                if (instance.Data != null && instance.Data.buildingName == TownHallBuildingName)
                {
                    position = instance.transform.position;
                    return true;
                }
            }

            position = default;
            return false;
        }

        /// <summary>
        /// The nearest standable cell to one template slot: its own bearing at its own distance
        /// first, then the same bearing walked in and out to either side, and only then a bearing
        /// nudged off it -- never by more than the cap the spread depends on.
        ///
        /// BEARING outside, radius inside, and the nesting is the whole point. The other way round
        /// -- sweeping every bearing before nudging the radius by four metres -- is what a first
        /// cut of this did, and it would rather send a portal a hundred and eighty degrees around
        /// the settlement than move it four metres. On the real map that produced two portals four
        /// degrees apart (PortalOpeningTests caught it), which is exactly the crowding the spread
        /// exists to prevent. A portal's bearing is what the player plans their walls around; its
        /// exact distance is not, so the distance is what gives way.
        /// </summary>
        private static bool TryFindPortalSpot(GridManager grid, Vector3 townHallPosition, PortalPlacement.Slot slot,
            float maxBearingDeviationRadians, float nearestAllowedMeters, out Vector2Int cell)
        {
            var mapApplier = MeshMapApplier.Instance;

            foreach (var angle in PortalPlacement.BearingsToTry(slot.AngleRadians, PortalDirectionSamples, maxBearingDeviationRadians))
            {
                foreach (var radius in RadiiToTry(slot.DistanceMeters, nearestAllowedMeters))
                {
                    var candidate = townHallPosition + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                    var candidateCell = grid.WorldToCell(candidate);

                    if (!grid.IsWithinBounds(candidateCell, PortalFootprint) || !grid.IsAreaFree(candidateCell, PortalFootprint)) continue;
                    if (!IsSolidGround(grid, mapApplier, candidateCell)) continue;

                    cell = candidateCell;
                    return true;
                }
            }

            cell = default;
            return false;
        }

        /// <summary>Every cell of a portal's plot is dry, flat playable ground -- one cell of lake under a corner of the platform would leave the arch hanging over the water.</summary>
        private static bool IsSolidGround(GridManager grid, MeshMapApplier mapApplier, Vector2Int cell)
        {
            return IsSolidGround(grid, mapApplier, cell, PortalFootprint);
        }

        private static bool IsSolidGround(GridManager grid, MeshMapApplier mapApplier, Vector2Int cell, Vector2Int footprint)
        {
            if (mapApplier == null) return true;
            for (var dx = 0; dx < footprint.x; dx++)
            {
                for (var dy = 0; dy < footprint.y; dy++)
                {
                    var c = cell + new Vector2Int(dx, dy);
                    if (mapApplier.IsWaterCell(c)) return false;
                    if (!mapApplier.IsGroundAt(grid.GetFootprintCenterWorld(c, Vector2Int.one))) return false;
                }
            }
            return true;
        }

        /// <summary>
        /// The distances to try along one bearing, nearest the slot's own first: it, then a step
        /// out, a step in, two steps out, two steps in, and so on.
        ///
        /// The inward walk stops at the template's FIRST slot -- the distance the sheet says a
        /// portal may not be nearer than. Letting it run to PortalPlacement's absolute floor
        /// instead put a portal eight metres from the Town Hall on a real map, inside the
        /// settlement and past every wall, which is precisely what that sheet number exists to
        /// prevent. Outward has no such limit: the grid bounds check turns back anything that
        /// leaves the map, and a portal further out costs the design nothing.
        /// </summary>
        private static IEnumerable<float> RadiiToTry(float slotDistanceMeters, float nearestAllowedMeters)
        {
            yield return slotDistanceMeters;

            for (var delta = PortalRadiusStepMeters; delta <= PortalMaxRadiusDeltaMeters; delta += PortalRadiusStepMeters)
            {
                yield return slotDistanceMeters + delta;

                var inward = slotDistanceMeters - delta;
                if (inward >= nearestAllowedMeters) yield return inward;
            }
        }

        /// <summary>Builds one portal on a cell already known to be standable, and hands it back so the caller can set the health a save left it on.</summary>
        private OrcPortal SpawnPortal(GridManager grid, Vector2Int cell)
        {
            var position = grid.GetFootprintCenterWorld(cell, PortalFootprint);

            EnsureMaterials();

            var root = new GameObject("OrcPortal");
            root.transform.SetPositionAndRotation(position, FacingTownHall(position));
            var portal = root.AddComponent<OrcPortal>();
            portal.SetCell(cell);

            if (portalActiveModel != null && portalPassiveModel != null)
            {
                // Both looks are built once and switched (see OrcPortal.ShowActive).
                var active = Instantiate(portalActiveModel, root.transform, false);
                var passive = Instantiate(portalPassiveModel, root.transform, false);
                active.name = "PortalActive";
                passive.name = "PortalPassive";
                portal.SetLooks(active, passive);
            }
            else
            {
                // The cube arch the models replaced: two posts and a lintel.
                AddPortalPart(root.transform, new Vector3(-0.6f, 1.5f, 0f), new Vector3(0.4f, 3f, 0.4f));
                AddPortalPart(root.transform, new Vector3(0.6f, 1.5f, 0f), new Vector3(0.4f, 3f, 0.4f));
                AddPortalPart(root.transform, new Vector3(0f, 3.1f, 0f), new Vector3(1.6f, 0.4f, 0.4f));
            }

            // One click target covering the whole arch (the parts' own colliders are stripped), so
            // the player can tap the portal to order an assault on it -- see ArmyOrderInput. A
            // TRIGGER, like every other click affordance in this project: it must not become a
            // wall that raiding orcs and marching soldiers get wedged against.
            var clickCollider = root.AddComponent<BoxCollider>();
            clickCollider.isTrigger = true;
            clickCollider.center = new Vector3(0f, 1.2f, 0f);
            clickCollider.size = new Vector3(1.9f, 2.4f, 1.2f);

            // Occupies its cell so the player can't drop a building on top of the arch -- and, with
            // several portals, so the search for the next slot cannot pick a cell one of them is
            // already standing on.
            grid.SetAreaOccupied(cell, PortalFootprint, true);
            // Testing convenience (see PortalFogRevealRadiusCells) -- the player can see where
            // raids come from instead of having them arrive out of unexplored fog.
            FogOfWarManager.Instance?.RevealPermanent(cell, PortalFogRevealRadiusCells);

            return portal;
        }

        // ---------------------------------------------------------------------------------------
        // Orc camps (the user's design, 2026-10-14): a camp without walls round each portal --
        // a watchtower that shoots soldiers, a war tent that keeps a garrison, and a totem that
        // shields the portal. See OrcCamp.

        /// <summary>
        /// Where each camp building wants to stand, in the portal's own frame (metres; +x its
        /// right, +z its BACK -- the portal faces the town along -z): the tower forward on the left
        /// to meet an army coming from the town, the tent off to the right, and the totem behind
        /// the portal so it can only be reached through the camp. Each has a mirrored spare and a
        /// further-out pair, tried in order, for when the map is in the way.
        /// </summary>
        private static readonly (OrcCampBuildingKind kind, Vector2 offset)[] CampLayout =
        {
            (OrcCampBuildingKind.Watchtower, new Vector2(-2.5f, -1.5f)),
            (OrcCampBuildingKind.WarTent, new Vector2(3f, 0.5f)),
            (OrcCampBuildingKind.Totem, new Vector2(0f, 2.5f)),
        };

        /// <summary>
        /// Where to try a camp building, best first: its own spot, mirrored, the pair further out,
        /// then right round the portal at both distances -- so a camp backed onto the lake or the
        /// map's edge still gets every building (the totem above all: without it the portal has
        /// no shield).
        /// </summary>
        private static IEnumerable<Vector2> CampSpots(Vector2 offset)
        {
            yield return offset;
            yield return new Vector2(-offset.x, offset.y);
            yield return offset * 1.4f;
            yield return new Vector2(-offset.x, offset.y) * 1.4f;
            var start = Mathf.Atan2(offset.y, offset.x);
            foreach (var scale in new[] { 1f, 1.4f, 1.8f })
            {
                for (var step = 1; step < 12; step++)
                {
                    // alternate either side of the preferred bearing: +30, -30, +60, -60 ...
                    var turn = ((step + 1) / 2) * (Mathf.PI / 6f) * (step % 2 == 1 ? 1f : -1f);
                    var angle = start + turn;
                    var radius = offset.magnitude * scale;
                    yield return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                }
            }
        }

        private static Vector2Int FootprintOf(OrcCampBuildingKind kind)
        {
            return kind == OrcCampBuildingKind.WarTent ? new Vector2Int(2, 2) : Vector2Int.one;
        }

        private GameObject ModelOf(OrcCampBuildingKind kind) => kind switch
        {
            OrcCampBuildingKind.Watchtower => watchtowerModel,
            OrcCampBuildingKind.WarTent => warTentModel,
            _ => totemModel,
        };

        private GameObject RuinOf(OrcCampBuildingKind kind) => kind switch
        {
            OrcCampBuildingKind.Watchtower => watchtowerRuinModel,
            OrcCampBuildingKind.WarTent => warTentRuinModel,
            _ => totemRuinModel,
        };

        private OrcCamp CreateCamp(Vector2Int portalCell, OrcPortal portal, Vector3 position, Quaternion rotation)
        {
            var root = new GameObject($"OrcCamp {portalCell.x},{portalCell.y}");
            root.transform.SetPositionAndRotation(position, rotation);
            var camp = root.AddComponent<OrcCamp>();
            camp.Initialize(portalCell, portal);
            return camp;
        }

        /// <summary>Puts up a fresh camp round a portal and musters its garrison. A building with nowhere to stand is left out rather than failing the camp.</summary>
        private void BuildCamp(GridManager grid, OrcPortal portal)
        {
            var camp = CreateCamp(portal.Cell, portal, portal.transform.position, portal.transform.rotation);
            var mapApplier = MeshMapApplier.Instance;
            var rotation = portal.transform.rotation;

            foreach (var (kind, offset) in CampLayout)
            {
                var footprint = FootprintOf(kind);
                foreach (var candidate in CampSpots(offset))
                {
                    var world = portal.transform.position + rotation * new Vector3(candidate.x, 0f, candidate.y);
                    var cell = grid.WorldToCell(world - new Vector3((footprint.x - 1) * 0.5f * grid.CellSize, 0f, (footprint.y - 1) * 0.5f * grid.CellSize));
                    if (!grid.IsWithinBounds(cell, footprint) || !grid.IsAreaFree(cell, footprint)) continue;
                    if (!IsSolidGround(grid, mapApplier, cell, footprint)) continue;

                    SpawnCampBuilding(grid, camp, kind, cell, rotation.eulerAngles.y);
                    if (kind == OrcCampBuildingKind.Totem) FlankWithPalisade(grid, mapApplier, cell, rotation);
                    break;
                }
            }

            camp.FillGarrison();
        }

        /// <summary>A short run of palisade either side of the totem -- decoration, not a wall: the camp has no walls (the user's call).</summary>
        private void FlankWithPalisade(GridManager grid, MeshMapApplier mapApplier, Vector2Int totemCell, Quaternion rotation)
        {
            if (palisadeModel == null) return;
            var right = rotation * Vector3.right;
            var side = new Vector2Int(Mathf.RoundToInt(right.x), Mathf.RoundToInt(right.z));
            foreach (var cell in new[] { totemCell + side, totemCell - side })
            {
                if (!grid.IsWithinBounds(cell, Vector2Int.one) || !grid.IsAreaFree(cell, Vector2Int.one)) continue;
                if (!IsSolidGround(grid, mapApplier, cell, Vector2Int.one)) continue;

                grid.SetAreaOccupied(cell, Vector2Int.one, true);
                // The straight piece runs along its local Z; turned a quarter from the portal's
                // facing, it runs across the camp, beside the totem.
                var piece = Instantiate(palisadeModel, grid.GetFootprintCenterWorld(cell, Vector2Int.one),
                    rotation * Quaternion.Euler(0f, 90f, 0f));
                piece.name = $"OrcPalisade {cell.x},{cell.y}";
            }
        }

        private OrcCampBuilding SpawnCampBuilding(GridManager grid, OrcCamp camp, OrcCampBuildingKind kind, Vector2Int cell, float yaw)
        {
            EnsureMaterials();
            var footprint = FootprintOf(kind);
            grid.SetAreaOccupied(cell, footprint, true);

            var root = new GameObject($"OrcCamp{kind} {cell.x},{cell.y}");
            root.transform.SetPositionAndRotation(grid.GetFootprintCenterWorld(cell, footprint), Quaternion.Euler(0f, yaw, 0f));
            var building = root.AddComponent<OrcCampBuilding>();
            building.Initialize(kind, camp, cell, RuinOf(kind));

            var model = ModelOf(kind);
            if (model != null)
            {
                Instantiate(model, root.transform, false).name = "Model";
            }
            else
            {
                AddPortalPart(root.transform, new Vector3(0f, 0.6f, 0f), new Vector3(footprint.x * 0.8f, 1.2f, footprint.y * 0.8f));
            }

            // Tappable, so a group can be sent at it (ArmyOrderInput), and a trigger so it never
            // becomes a wall units wedge against; the NavMesh carve is what routes them round it.
            var height = kind == OrcCampBuildingKind.Watchtower ? 2.4f : kind == OrcCampBuildingKind.WarTent ? 1.2f : 1.6f;
            var click = root.AddComponent<BoxCollider>();
            click.isTrigger = true;
            click.center = new Vector3(0f, height / 2f, 0f);
            click.size = new Vector3(footprint.x * 0.95f, height, footprint.y * 0.95f);
            var obstacle = root.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
            obstacle.size = new Vector3(footprint.x * 0.9f, 2f, footprint.y * 0.9f);
            obstacle.center = new Vector3(0f, 1f, 0f);
            obstacle.carving = true;

            camp.Add(building);
            return building;
        }

        /// <summary>One guard for a camp's garrison, posted where it came out of the tent. `index` is its place in the garrison, which decides whether it is a thrower (see KindInGroup; a garrison has no chief).</summary>
        public OrcUnit SpawnGuard(Vector3 position, int level, OrcCamp camp, int index = 0)
        {
            EnsureMaterials();
            var unit = SpawnOrc(position, level, KindInGroup(index, level, withChief: false), scatter: true);
            var post = unit.transform.position;
            unit.SetGuardPost(post, BalanceConfig.Instance.OrcGarrisonGuardRadiusMeters);
            return unit;
        }

        /// <summary>
        /// Puts the camps back from the save: every building where it stood on the health it had,
        /// every ruin where it fell, and a full garrison (guards are not saved). A save from before
        /// the camps existed gets fresh camps round its standing portals.
        /// </summary>
        private void RestoreCamps(GridManager grid, IReadOnlyList<CampBuildingRestoreState> campBuildings)
        {
            if (campBuildings == null || campBuildings.Count == 0)
            {
                foreach (var portal in OrcPortal.All)
                {
                    BuildCamp(grid, portal);
                }
                return;
            }

            var camps = new Dictionary<Vector2Int, OrcCamp>();
            foreach (var entry in campBuildings)
            {
                if (!camps.TryGetValue(entry.PortalCell, out var camp))
                {
                    OrcPortal portal = null;
                    foreach (var p in OrcPortal.All)
                    {
                        if (p.Cell == entry.PortalCell) portal = p;
                    }
                    var position = grid.GetFootprintCenterWorld(entry.PortalCell, PortalFootprint);
                    camp = CreateCamp(entry.PortalCell, portal, position, portal != null ? portal.transform.rotation : FacingTownHall(position));
                    camps[entry.PortalCell] = camp;
                }

                if (entry.Health > 0)
                {
                    SpawnCampBuilding(grid, camp, entry.Kind, entry.Cell, entry.Yaw).SetCurrentHealth(entry.Health);
                }
                else
                {
                    var footprint = FootprintOf(entry.Kind);
                    grid.SetAreaOccupied(entry.Cell, footprint, true);
                    OrcCampBuilding.SpawnRuin(RuinOf(entry.Kind), entry.Cell, grid.GetFootprintCenterWorld(entry.Cell, footprint),
                        Quaternion.Euler(0f, entry.Yaw, 0f));
                    camp.AddFallen(new OrcCamp.FallenBuilding(entry.Kind, entry.Cell, entry.Yaw));
                }
            }

            foreach (var camp in camps.Values)
            {
                if (camp.Portal != null) camp.Portal.IsShielded = camp.TotemStanding;
                camp.FillGarrison();
            }
        }

        /// <summary>The closed portal's ruin, on the plot the portal stood on. Scenery only: no collider, nothing to attack.</summary>
        private void SpawnRuin(Vector2Int cell, Vector3 position, Quaternion rotation)
        {
            if (portalClosedModel == null) return;
            var ruin = Instantiate(portalClosedModel, position, rotation);
            ruin.name = $"OrcPortalRuin {cell.x},{cell.y}";
        }

        /// <summary>
        /// A portal turns its front -- the side the whirlpool and the steps face -- towards the
        /// Town Hall, snapped to a quarter turn so the square platform keeps to its plot.
        /// </summary>
        private static Quaternion FacingTownHall(Vector3 position)
        {
            if (!TryFindTownHallPosition(out var townHall)) return Quaternion.identity;
            var toTown = townHall - position;
            toTown.y = 0f;
            if (toTown.sqrMagnitude < 0.01f) return Quaternion.identity;
            // The model's front is its -Z, so its +Z points away from the town.
            var yaw = Mathf.Atan2(-toTown.x, -toTown.z) * Mathf.Rad2Deg;
            return Quaternion.Euler(0f, Mathf.Round(yaw / 90f) * 90f, 0f);
        }

        private void AddPortalPart(Transform parent, Vector3 localPosition, Vector3 size)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = "PortalPart";
            Destroy(part.GetComponent<BoxCollider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = size;
            part.GetComponent<Renderer>().sharedMaterial = _portalMaterial;
        }

        private void SpawnRaid(int score)
        {
            // Out of one of the portals still standing, picked at random -- so which side of the
            // settlement a wave arrives on is not something the player can settle once and then
            // stop watching, and so the portals the player has closed stop sending anything. None
            // left means the map has just been won; the win handler runs on the same frame.
            var portals = OrcPortal.All;
            if (portals.Count == 0) return;

            var size = ComputeRaidSize(score);
            var level = ComputeOrcLevel(score);

            // Out of the portal that was lit for it; then the next one is picked and lit.
            var source = _nextSource != null && _nextSource.CurrentHealth > 0
                ? _nextSource
                : portals[Random.Range(0, portals.Count)];
            var origin = source.transform.position;
            var leader = SpawnWave(origin, size, level);
            PickNextSource();
            // Two messages, because "the orcs are raiding (5)" says nothing about the wave that has
            // quietly become five times as hard as the last one at the very same size. The icon
            // follows the first orc of the wave, so a tap a minute later finds the orcs, not the
            // portal they left.
            EventLogManager.Instance?.Post(GameEvent.At(level > 1
                    ? Localization.Format("#log_raid_levelled", size, level)
                    : Localization.Format("#log_raid", size),
                EventSeverity.Danger, EventGlyph.Swords, null, origin, leader != null ? leader.transform : null));
        }

        /// <summary>
        /// Spawns a squad at an explicit position -- the entry point the OrcSpawn cheat uses to
        /// place orcs at a chosen portal, at a chosen level, on demand. Safe to call before the
        /// portal exists: it builds its own materials rather than assuming SpawnPortal ran first.
        /// </summary>
        /// <returns>The first orc of the squad, or null when none was spawned.</returns>
        public OrcUnit SpawnOrcs(Vector3 origin, int count, int level)
        {
            if (count <= 0) return null;

            EnsureMaterials();
            OrcUnit first = null;
            for (var i = 0; i < count; i++)
            {
                var orc = SpawnOrc(origin, level);
                if (first == null) first = orc;
            }
            return first;
        }

        /// <summary>A raid's wave: raiders, with throwers among them and a chief at its head once the raids have grown enough (KindInGroup). The first orc is the one handed back -- the chief when there is one.</summary>
        private OrcUnit SpawnWave(Vector3 origin, int count, int level)
        {
            EnsureMaterials();
            OrcUnit first = null;
            for (var i = 0; i < count; i++)
            {
                var orc = SpawnOrc(origin, level, KindInGroup(i, level, withChief: true));
                if (first == null) first = orc;
            }
            return first;
        }

        /// <summary>
        /// What the orc at `index` of a wave or a garrison is. The chief leads (index 0) once the
        /// orcs reach chiefFromLevel; from throwerFromLevel every `throwerEvery`-th orc throws.
        /// Below both levels everything is a raider, so the opening raids are unchanged.
        /// Pure, with its numbers passed in, so a test can state them.
        /// </summary>
        public static OrcKind KindInGroup(int index, int level, bool withChief, int throwerFromLevel, int throwerEvery, int chiefFromLevel)
        {
            if (withChief && index == 0 && chiefFromLevel > 0 && level >= chiefFromLevel) return OrcKind.Chief;
            if (throwerFromLevel > 0 && throwerEvery > 0 && level >= throwerFromLevel && (index + 1) % throwerEvery == 0) return OrcKind.Thrower;
            return OrcKind.Raider;
        }

        public static OrcKind KindInGroup(int index, int level, bool withChief)
        {
            var balance = BalanceConfig.Instance;
            return KindInGroup(index, level, withChief, balance.RaidThrowerFromLevel, balance.RaidThrowerEvery, balance.RaidChiefFromLevel);
        }

        /// <summary>Pure formula, kept separate so an EditMode test can check the ramp without a live scene. Takes its numbers explicitly so the test can state them rather than depend on today's sheet.</summary>
        public static int ComputeRaidSize(int score, int baseSize, int progressPerExtraRaider, int maxSize)
        {
            var bonus = progressPerExtraRaider > 0 ? Mathf.Max(0, score) / progressPerExtraRaider : 0;
            return Mathf.Min(maxSize, baseSize + bonus);
        }

        /// <summary>The size of the raid a settlement at this score would draw, at the sheet's current numbers.</summary>
        public static int ComputeRaidSize(int score)
        {
            var balance = BalanceConfig.Instance;
            return ComputeRaidSize(score, balance.RaidBaseSize, balance.RaidProgressPerExtraRaider, balance.RaidMaxSize);
        }

        /// <summary>
        /// How hard each raider hits, as a level -- OrcUnit scales health and damage by it, and
        /// makes itself visibly bigger. This is what keeps a raid meaningful once the size cap is
        /// reached: past that point, growing stops making the orcs more numerous and starts making
        /// them worse.
        /// </summary>
        public static int ComputeOrcLevel(int score, int progressPerOrcLevel, int maxOrcLevel)
        {
            if (progressPerOrcLevel <= 0) return 1;
            return Mathf.Clamp(1 + Mathf.Max(0, score) / progressPerOrcLevel, 1, Mathf.Max(1, maxOrcLevel));
        }

        public static int ComputeOrcLevel(int score)
        {
            var balance = BalanceConfig.Instance;
            return ComputeOrcLevel(score, balance.RaidProgressPerOrcLevel, balance.RaidMaxOrcLevel);
        }

        /// <summary>
        /// How long until the next wave: the full interval at a standing start, falling linearly to
        /// the floor by the time the player has reached scoreAtMinInterval, and never past it.
        ///
        /// Interpolated rather than stepped, so growing the town never causes a jump in pressure
        /// the player cannot account for -- and clamped at both ends so a sheet edited to nonsense
        /// (a floor above the ceiling, a zero threshold) slows raids down rather than dividing by
        /// zero or spawning one per frame.
        /// </summary>
        public static float ComputeRaidIntervalSeconds(int score, float intervalAtZeroScore, float minIntervalSeconds, int scoreAtMinInterval)
        {
            if (scoreAtMinInterval <= 0 || minIntervalSeconds >= intervalAtZeroScore) return intervalAtZeroScore;

            var t = Mathf.Clamp01(Mathf.Max(0, score) / (float)scoreAtMinInterval);
            return Mathf.Lerp(intervalAtZeroScore, minIntervalSeconds, t);
        }

        public static float ComputeRaidIntervalSeconds(int score)
        {
            var balance = BalanceConfig.Instance;
            return ComputeRaidIntervalSeconds(score, balance.RaidIntervalSeconds, balance.RaidMinIntervalSeconds, balance.RaidProgressAtMinInterval);
        }

        /// <summary>Builds one orc. `scatter` is what separates a raider stepping out of the portal from a loaded one, which has to land exactly where the save says.</summary>
        private OrcUnit SpawnOrc(Vector3 origin, int level, bool scatter = true)
        {
            return SpawnOrc(origin, level, OrcKind.Raider, scatter);
        }

        private OrcUnit SpawnOrc(Vector3 origin, int level, OrcKind kind, bool scatter = true)
        {
            var jitter = scatter ? Random.insideUnitCircle * 1.5f : Vector2.zero;
            var spawnPos = origin + new Vector3(jitter.x, 0f, jitter.y);

            var label = kind == OrcKind.Raider ? "Orc" : $"Orc {kind}";
            var root = new GameObject(level > 1 ? $"{label} (lvl {level})" : label);
            root.transform.position = spawnPos;
            // Visibly bigger with level, so a cheat-spawned elite is tellable from a raider at a
            // glance without opening the inspector. Deliberately sublinear -- a level 5 orc has 5x
            // the health but must not be a five-times-wider wall of cubes.
            var scale = LevelScale(level);
            root.transform.localScale = Vector3.one * (scale * UnitSize.Scale);

            var brute = kind == OrcKind.Raider && level >= BruteFromLevel && bruteModel != null;
            var model = gruntModel;
            var figureName = "Grunt";
            if (kind == OrcKind.Thrower && throwerModel != null) { model = throwerModel; figureName = "Thrower"; }
            else if (kind == OrcKind.Chief && chiefModel != null) { model = chiefModel; figureName = "Chief"; }
            else if (brute) { model = bruteModel; figureName = "Brute"; }
            if (model != null)
            {
                // The models are authored at their real size (a grunt 0.58 m, a brute 0.73), so
                // the root's unit scale is divided back out -- and for the brute, the growth the
                // levels below it already bought, or a level-4 brute would stand a metre tall.
                var figure = Instantiate(model, root.transform, false);
                figure.name = figureName;
                var undo = UnitSize.Scale * (brute ? LevelScale(BruteFromLevel) : 1f);
                figure.transform.localScale = Vector3.one / undo;
                // Units stay out of the shadow pass, like the citizens.
                SceneryShadows.DisableCasting(figure);
            }
            else
            {
                AddCubePart(root.transform, "Body", new Vector3(0f, 0.3f, 0f), new Vector3(0.36f, 0.6f, 0.36f), _orcSkinMaterial);
                AddCubePart(root.transform, "Head", new Vector3(0f, 0.72f, 0f), new Vector3(0.26f, 0.26f, 0.26f), _orcSkinMaterial);
                AddCubePart(root.transform, "Shoulders", new Vector3(0f, 0.55f, 0f), new Vector3(0.5f, 0.14f, 0.4f), _orcGearMaterial);
            }

            var controller = root.AddComponent<CharacterController>();
            controller.height = 0.86f;
            controller.radius = 0.2f;
            controller.center = new Vector3(0f, 0.43f, 0f);
            controller.skinWidth = 0.02f;
            controller.minMoveDistance = 0f;

            var unit = root.AddComponent<OrcUnit>();
            unit.Initialize(level, kind);
            return unit;
        }

        /// <summary>How much bigger a raider of this level is drawn than a level-1 one. Deliberately sublinear: a level-5 orc has five times the health, not five times the width.</summary>
        public static float LevelScale(int level)
        {
            return 1f + (Mathf.Max(1, level) - 1) * 0.12f;
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

        private void EnsureMaterials()
        {
            if (_portalMaterial != null) return;

            _portalMaterial = new Material(RuntimeShaders.Lit) { color = PortalColor };
            _orcSkinMaterial = new Material(RuntimeShaders.Lit) { color = OrcSkinColor };
            _orcGearMaterial = new Material(RuntimeShaders.Lit) { color = OrcGearColor };
        }
    }
}
