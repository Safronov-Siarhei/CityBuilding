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
        private const float PortalMaxRadiusDeltaMeters = 48f;
        // Revealed permanently so the portal is visible from the start -- explicitly a testing
        // convenience the user asked for; the intended behaviour is for it to stay hidden until
        // the player scouts it.
        private const int PortalFogRevealRadiusCells = 16;
        private const string TownHallBuildingName = BuildingIds.TownHall;

        private static readonly Color PortalColor = new Color(0.32f, 0.1f, 0.42f);
        private static readonly Color OrcSkinColor = new Color(0.28f, 0.42f, 0.22f);
        private static readonly Color OrcGearColor = new Color(0.22f, 0.2f, 0.18f);

        // The faction's Blender models (Tools/blender/orc*.py, exported to Models/Orcs), wired by
        // SetupProject. Any left empty falls back to the cube figure it replaced, so a build
        // without the models still plays.
        // Arrays are by LEVEL (index 0 = level 1): every level of a building or a warrior has its
        // own model (the user's call, 2026-10-16).
        [SerializeField] private GameObject[] portalActiveModels;
        [SerializeField] private GameObject[] portalPassiveModels;
        [SerializeField] private GameObject portalClosedModel;
        [SerializeField] private GameObject[] gruntModels;
        [SerializeField] private GameObject[] bruteModels;
        [SerializeField] private GameObject throwerModel;
        [SerializeField] private GameObject chiefModel;
        // The walled camps (see OrcCamp, OrcBaseLayout): their buildings by level, and the ruins.
        [SerializeField] private GameObject[] watchtowerModels;
        [SerializeField] private GameObject watchtowerRuinModel;
        [SerializeField] private GameObject warTentModel;
        [SerializeField] private GameObject warTentRuinModel;
        [SerializeField] private GameObject[] totemModels;
        [SerializeField] private GameObject totemRuinModel;
        // Four pieces per level, in FenceVariant order (Straight, Corner, T, Cross).
        [SerializeField] private GameObject[] palisadeModels;
        [SerializeField] private GameObject[] gateModels;
        [SerializeField] private GameObject gateRuinModel;
        [SerializeField] private GameObject gateTowerModel;
        [SerializeField] private GameObject gateTowerRuinModel;
        [SerializeField] private GameObject[] storehouseModels;
        [SerializeField] private GameObject storehouseRuinModel;
        [SerializeField] private GameObject brutePitModel;
        [SerializeField] private GameObject brutePitRuinModel;
        [SerializeField] private GameObject throwerRangeModel;
        [SerializeField] private GameObject throwerRangeRuinModel;
        // The main base's own stone-and-green set (the user's rule, 2026-10-19): nothing of the
        // camps is reused there.
        [SerializeField] private GameObject mainPortalModel;
        [SerializeField] private GameObject mainPortalRuinModel;
        [SerializeField] private GameObject[] mainWallModels;          // FenceVariant order
        [SerializeField] private GameObject mainGateModel;
        [SerializeField] private GameObject mainGateRuinModel;
        [SerializeField] private GameObject mainTowerModel;
        [SerializeField] private GameObject mainTowerRuinModel;
        [SerializeField] private GameObject mainTotemModel;
        [SerializeField] private GameObject mainTotemRuinModel;
        [SerializeField] private GameObject mainStorehouseModel;
        [SerializeField] private GameObject mainStorehouseRuinModel;
        [SerializeField] private GameObject mainBarracksModel;
        [SerializeField] private GameObject mainBarracksRuinModel;
        [SerializeField] private GameObject mainBrutePitModel;
        [SerializeField] private GameObject mainBrutePitRuinModel;
        [SerializeField] private GameObject mainThrowerRangeModel;
        [SerializeField] private GameObject mainThrowerRangeRuinModel;
        [SerializeField] private GameObject chiefHouseModel;
        [SerializeField] private GameObject chiefHouseRuinModel;
        [SerializeField] private GameObject[] mountainModels;
        [SerializeField] private GameObject bossModel;
        // The prisoners' cages: one model per camp level, and the main base's own.
        [SerializeField] private GameObject[] cageModels;
        [SerializeField] private GameObject cageRuinModel;
        [SerializeField] private GameObject mainCageModel;
        [SerializeField] private GameObject mainCageRuinModel;
        [SerializeField] private GameObject mainTreasuryModel;
        [SerializeField] private GameObject mainTreasuryRuinModel;

        /// <summary>The great portal's plot.</summary>
        public static readonly Vector2Int MainPortalFootprint = new Vector2Int(4, 4);

        /// <summary>Where the main base's portal stood or stands -- null when the map has no room for a main base.</summary>
        public Vector2Int? MainPortalCell { get; private set; }

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
            public PortalRestoreState(Vector2Int cell, int health, bool isMain = false)
            {
                Cell = cell;
                Health = health;
                IsMain = isMain;
            }

            public Vector2Int Cell { get; }
            public int Health { get; }
            public bool IsMain { get; }
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
            IReadOnlyList<Vector2Int> closedPortalCells, IReadOnlyList<CampRestoreState> camps,
            IReadOnlyList<CampBuildingRestoreState> campBuildings, float secondsUntilNextRaid,
            Vector2Int? mainPortalCell = null)
        {
            if (!portalsPlaced) return;
            MainPortalCell = mainPortalCell;

            // The flag alone stops Update from opening a fresh set over the ruins of the ones the
            // player fought for.
            _portalsPlaced = true;

            var grid = GridManager.Instance;
            if (grid == null || portals == null) return;

            // a designed map's main base faces south whatever the Town Hall's place
            var designed = MeshMapApplier.Instance != null ? MeshMapApplier.Instance.CurrentMap : null;
            Quaternion? mainFacing = designed != null && designed.HasMainBaseSpot ? Quaternion.identity : (Quaternion?)null;
            var restored = new List<(OrcPortal portal, int health)>();
            foreach (var state in portals)
            {
                if (state.Health <= 0) continue;
                restored.Add((SpawnPortal(grid, state.Cell, state.IsMain, state.IsMain ? mainFacing : null), state.Health));
            }

            if (closedPortalCells != null)
            {
                foreach (var cell in closedPortalCells)
                {
                    _closedPortalCells.Add(cell);
                    var main = MainPortalCell.HasValue && MainPortalCell.Value == cell;
                    var footprint = main ? MainPortalFootprint : PortalFootprint;
                    grid.SetAreaOccupied(cell, footprint, true);
                    var position = grid.GetFootprintCenterWorld(cell, footprint);
                    SpawnRuin(cell, position, main && mainFacing.HasValue ? mainFacing.Value : FacingTownHall(position), main);
                }
            }

            RestoreCamps(camps, campBuildings);
            // After the camps: building a camp gives its portal the health of the camp's level,
            // and the player's damage goes on top of that.
            foreach (var (portal, health) in restored)
            {
                portal.SetCurrentHealth(health);
            }

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
            PlunderCamp(portal);
            _closedPortalCells.Add(portal.Cell);
            SpawnRuin(portal.Cell, portal.transform.position, portal.transform.rotation, portal.IsMain);
            if (_nextSource == portal || _nextSource == null) PickNextSource();
        }

        /// <summary>Picks the portal the next wave comes out of, at random among those standing, and lights it.</summary>
        private void PickNextSource()
        {
            var sources = RaidSources();
            _nextSource = sources.Count > 0 ? sources[Random.Range(0, sources.Count)] : null;
            foreach (var portal in sources)
            {
                portal.ShowActive(portal == _nextSource);
            }
        }

        private readonly List<OrcPortal> _sources = new List<OrcPortal>();

        /// <summary>The portals raids may come out of: every standing one but the main base's (the user's rule: it sends none).</summary>
        private List<OrcPortal> RaidSources()
        {
            _sources.Clear();
            foreach (var portal in OrcPortal.All)
            {
                if (!portal.IsMain) _sources.Add(portal);
            }
            return _sources;
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
            GrowCamps(score);
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

            var designed = MeshMapApplier.Instance != null ? MeshMapApplier.Instance.CurrentMap : null;
            if (designed != null && (designed.HasMainBaseSpot || designed.CampSpots.Length > 0))
            {
                // A designed map says where the orcs stand: its rivers and mountains are laid out
                // round those spots, and a search round the Town Hall could put a camp anywhere.
                opened = OpenAtMapSpots(grid, townHallPosition, designed);
            }
            // The main base first, and furthest: its ground is the biggest thing on the map, and the
            // camps fit round it far more easily than it fits round them.
            else if (TryFindMainBaseSpot(grid, townHallPosition, out var mainCell))
            {
                var main = SpawnPortal(grid, mainCell, isMain: true);
                MainPortalCell = mainCell;
                BuildCamp(main, template.Count, OrcBaseLayout.MainLevel);
            }
            else
            {
                Debug.LogWarning("OrcRaidManager: no room for the main base on this map -- closing every camp's portal wins instead.");
            }

            foreach (var slot in designed != null && (designed.HasMainBaseSpot || designed.CampSpots.Length > 0)
                         ? new List<PortalPlacement.Slot>() : template)
            {
                // The biggest camp that fits round the slot: the square it will grow into is
                // reserved now, so a smaller one is only taken where the ground has no room.
                var found = false;
                var cell = default(Vector2Int);
                var maxLevel = OrcBaseLayout.MaxLevel;
                for (; maxLevel >= 1 && !found; maxLevel--)
                {
                    found = TryFindPortalSpot(grid, townHallPosition, slot, maxBearingDeviation, nearestAllowed, maxLevel, out cell);
                    if (found) break;
                }
                if (!found)
                {
                    Debug.LogWarning($"OrcRaidManager: no valid ground for the portal slot at {slot.DistanceMeters:0.#} m " +
                                     $"from the Town Hall at {townHallPosition} (all water/cliff/occupied). That portal is skipped.");
                    continue;
                }

                // The template is ordered nearest first, so the order they open in is the rank.
                var portal = SpawnPortal(grid, cell);
                BuildCamp(portal, opened, maxLevel);
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

        /// <summary>How far round a designed spot (metres) a camp may move to find ground it fits on.</summary>
        private const float SpotSearchMeters = 8f;

        /// <summary>
        /// Opens the portals on a designed map's own spots: the main base on its spot with its gate
        /// to the south (the map's cirque opens that way), then the camps, nearest the Town Hall
        /// first (that is their rank), each the biggest that fits within a few metres of its spot.
        /// Returns how many camps opened.
        /// </summary>
        private int OpenAtMapSpots(GridManager grid, Vector3 townHall, MeshMapDefinition map)
        {
            var mapApplier = MeshMapApplier.Instance;
            var camps = new List<Vector2>(map.CampSpots);
            camps.Sort((a, b) => (new Vector3(a.x, 0f, a.y) - townHall).sqrMagnitude
                .CompareTo((new Vector3(b.x, 0f, b.y) - townHall).sqrMagnitude));

            if (map.HasMainBaseSpot)
            {
                var spot = new Vector3(map.MainBaseSpot.x, 0f, map.MainBaseSpot.y);
                var half = MainPortalFootprint / 2;
                if (TryFindNear(grid, spot, half, cell => grid.IsWithinBounds(cell, MainPortalFootprint) &&
                        grid.IsAreaFree(cell, MainPortalFootprint) &&
                        CampSquareFits(grid, mapApplier, cell, OrcBaseLayout.MainLevel, townHall, quartersOverride: 0), out var mainCell))
                {
                    var main = SpawnPortal(grid, mainCell, isMain: true, rotation: Quaternion.identity);
                    MainPortalCell = mainCell;
                    BuildCamp(main, camps.Count, OrcBaseLayout.MainLevel);
                }
                else
                {
                    Debug.LogWarning($"OrcRaidManager: the main base does not fit on the map's spot {map.MainBaseSpot} -- closing every camp's portal wins instead.");
                }
            }

            var opened = 0;
            foreach (var camp in camps)
            {
                var spot = new Vector3(camp.x, 0f, camp.y);
                var found = false;
                var cell = default(Vector2Int);
                var maxLevel = OrcBaseLayout.MaxLevel;
                for (; maxLevel >= 1; maxLevel--)
                {
                    var level = maxLevel;
                    found = TryFindNear(grid, spot, PortalFootprint / 2, c => grid.IsWithinBounds(c, PortalFootprint) &&
                        grid.IsAreaFree(c, PortalFootprint) && IsSolidGround(grid, mapApplier, c) &&
                        CampSquareFits(grid, mapApplier, c, level, townHall), out cell);
                    if (found) break;
                }
                if (!found)
                {
                    Debug.LogWarning($"OrcRaidManager: no ground for the camp on the map's spot {camp}. That camp is skipped.");
                    continue;
                }
                var portal = SpawnPortal(grid, cell);
                BuildCamp(portal, opened, maxLevel);
                opened++;
            }
            return opened;
        }

        /// <summary>The plot nearest a spot (its centre on the spot first, then outwards in rings a metre apart) that passes `fits`.</summary>
        private static bool TryFindNear(GridManager grid, Vector3 spot, Vector2Int halfPlot, System.Func<Vector2Int, bool> fits, out Vector2Int cell)
        {
            var centre = grid.WorldToCell(spot) - halfPlot;
            var reach = Mathf.CeilToInt(SpotSearchMeters / grid.CellSize);
            for (var ring = 0; ring <= reach; ring++)
            {
                for (var dx = -ring; dx <= ring; dx++)
                {
                    for (var dz = -ring; dz <= ring; dz++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != ring) continue;
                        var candidate = centre + new Vector2Int(dx, dz);
                        if (!fits(candidate)) continue;
                        cell = candidate;
                        return true;
                    }
                }
            }
            cell = default;
            return false;
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
            float maxBearingDeviationRadians, float nearestAllowedMeters, int campLevel, out Vector2Int cell)
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
                    if (!CampSquareFits(grid, mapApplier, candidateCell, campLevel, townHallPosition)) continue;

                    cell = candidateCell;
                    return true;
                }
            }

            cell = default;
            return false;
        }

        /// <summary>Whether the whole square a camp of `level` covers round a portal on `cell` is on the map, free and standable.</summary>
        private static bool CampSquareFits(GridManager grid, MeshMapApplier mapApplier, Vector2Int cell, int level, Vector3 townHall,
            int? quartersOverride = null)
        {
            var layout = OrcBaseLayout.For(level);
            var plot = Vector2Int.one * layout.PortalSize;
            var position = grid.GetFootprintCenterWorld(cell, plot);
            var quarters = quartersOverride ?? OrcBaseLayout.QuarterTurns(FacingFrom(position, townHall).eulerAngles.y);
            var bounds = layout.Bounds;
            OrcBaseLayout.ToWorld(bounds.min, bounds.size, cell + plot / 2, quarters, out var min, out var size);
            if (!grid.IsWithinBounds(min, size)) return false;
            // cheap occupancy first, then the ground under each cell. A boulder or a tree does
            // not count against the square: the camp clears them when it goes up.
            for (var x = 0; x < size.x; x++)
            {
                for (var z = 0; z < size.y; z++)
                {
                    var c = min + new Vector2Int(x, z);
                    if (!grid.IsAreaFree(c, Vector2Int.one) && !IsScenery(c)) return false;
                }
            }
            return IsSolidGround(grid, mapApplier, min, size);
        }

        /// <summary>
        /// Where the main base goes: as far from the Town Hall as its ground fits, looking round
        /// the compass at each distance -- the furthest spot first, because the user's main base
        /// is the end of the map, not a neighbour. Its square is about 38x32 cells, so this runs
        /// the full check only where the cheap one (occupancy) passes.
        /// </summary>
        private static bool TryFindMainBaseSpot(GridManager grid, Vector3 townHall, out Vector2Int cell)
        {
            var mapApplier = MeshMapApplier.Instance;
            var start = Random.Range(0f, Mathf.PI * 2f);
            for (var radius = 88f; radius >= 30f; radius -= 3f)
            {
                for (var i = 0; i < 36; i++)
                {
                    var angle = start + Mathf.PI * 2f * i / 36f;
                    var centre = townHall + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                    var candidate = grid.WorldToCell(centre) - Vector2Int.one * 2;
                    if (!grid.IsWithinBounds(candidate, MainPortalFootprint) || !grid.IsAreaFree(candidate, MainPortalFootprint)) continue;
                    if (!CampSquareFits(grid, mapApplier, candidate, OrcBaseLayout.MainLevel, townHall)) continue;
                    cell = candidate;
                    return true;
                }
            }
            cell = default;
            return false;
        }

        /// <summary>A boulder or a tree on the cell -- scenery an orc camp clears for itself.</summary>
        public static bool IsScenery(Vector2Int cell) =>
            (RockSpawner.Instance != null && RockSpawner.Instance.Has(cell)) ||
            (TreesAreaSpawner.Instance != null && TreesAreaSpawner.Instance.Has(cell));

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
        /// <param name="rotation">A fixed facing (a designed map's main base faces south); otherwise the portal faces the Town Hall.</param>
        private OrcPortal SpawnPortal(GridManager grid, Vector2Int cell, bool isMain = false, Quaternion? rotation = null)
        {
            var footprint = isMain ? MainPortalFootprint : PortalFootprint;
            var position = grid.GetFootprintCenterWorld(cell, footprint);

            EnsureMaterials();

            var root = new GameObject(isMain ? "OrcMainPortal" : "OrcPortal");
            root.transform.SetPositionAndRotation(position, rotation ?? FacingTownHall(position));
            var portal = root.AddComponent<OrcPortal>();
            portal.SetCell(cell);
            portal.IsMain = isMain;

            if (isMain ? mainPortalModel != null : Pick(portalActiveModels, 1) != null && Pick(portalPassiveModels, 1) != null)
            {
                // Both looks are built once and switched (see OrcPortal.ShowActive); its camp
                // swaps them for its level's (ApplyPortalLevel).
                ApplyPortalLevel(portal, isMain ? OrcBaseLayout.MainLevel : 1);
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
            clickCollider.center = new Vector3(0f, isMain ? 1.8f : 1.2f, 0f);
            clickCollider.size = isMain ? new Vector3(3.6f, 3.6f, 1.8f) : new Vector3(1.9f, 2.4f, 1.2f);

            // Occupies its cell so the player can't drop a building on top of the arch -- and, with
            // several portals, so the search for the next slot cannot pick a cell one of them is
            // already standing on.
            grid.SetAreaOccupied(cell, footprint, true);
            // Testing convenience (see PortalFogRevealRadiusCells) -- the player can see where
            // raids come from instead of having them arrive out of unexplored fog.
            FogOfWarManager.Instance?.RevealPermanent(cell, PortalFogRevealRadiusCells);

            return portal;
        }

        // ---------------------------------------------------------------------------------------
        // Orc camps: the walled bases the user drew level by level (2026-10-16). OrcBaseLayout
        // holds the plans, OrcCamp runs a camp; what is here is building the pieces, since the
        // models live on this component.

        private static GameObject Pick(GameObject[] byLevel, int level)
        {
            if (byLevel == null || byLevel.Length == 0) return null;
            return byLevel[Mathf.Clamp(level, 1, byLevel.Length) - 1];
        }

        private GameObject ModelOf(OrcCampBuildingKind kind, int level) => level >= OrcBaseLayout.MainLevel ? MainModelOf(kind) : kind switch
        {
            OrcCampBuildingKind.Watchtower => Pick(watchtowerModels, level),
            OrcCampBuildingKind.WarTent => warTentModel,
            OrcCampBuildingKind.Totem => Pick(totemModels, level),
            OrcCampBuildingKind.Gate => Pick(gateModels, level),
            OrcCampBuildingKind.GateTower => gateTowerModel,
            OrcCampBuildingKind.Storehouse => Pick(storehouseModels, level),
            OrcCampBuildingKind.BrutePit => brutePitModel,
            OrcCampBuildingKind.ThrowerRange => throwerRangeModel,
            OrcCampBuildingKind.Cage => Pick(cageModels, level),
            _ => null,
        };

        private GameObject MainModelOf(OrcCampBuildingKind kind) => kind switch
        {
            OrcCampBuildingKind.Watchtower => mainTowerModel,
            OrcCampBuildingKind.WarTent => mainBarracksModel,
            OrcCampBuildingKind.Totem => mainTotemModel,
            OrcCampBuildingKind.Gate => mainGateModel,
            OrcCampBuildingKind.GateTower => mainGateModel,
            OrcCampBuildingKind.Storehouse => mainStorehouseModel,
            OrcCampBuildingKind.BrutePit => mainBrutePitModel,
            OrcCampBuildingKind.ThrowerRange => mainThrowerRangeModel,
            OrcCampBuildingKind.ChiefHouse => chiefHouseModel,
            OrcCampBuildingKind.Cage => mainCageModel,
            OrcCampBuildingKind.Treasury => mainTreasuryModel,
            _ => null,
        };

        private GameObject RuinOf(OrcCampBuildingKind kind, int level) => level >= OrcBaseLayout.MainLevel ? kind switch
        {
            OrcCampBuildingKind.Watchtower => mainTowerRuinModel,
            OrcCampBuildingKind.WarTent => mainBarracksRuinModel,
            OrcCampBuildingKind.Totem => mainTotemRuinModel,
            OrcCampBuildingKind.Gate => mainGateRuinModel,
            OrcCampBuildingKind.GateTower => mainGateRuinModel,
            OrcCampBuildingKind.Storehouse => mainStorehouseRuinModel,
            OrcCampBuildingKind.BrutePit => mainBrutePitRuinModel,
            OrcCampBuildingKind.ThrowerRange => mainThrowerRangeRuinModel,
            OrcCampBuildingKind.ChiefHouse => chiefHouseRuinModel,
            OrcCampBuildingKind.Cage => mainCageRuinModel,
            OrcCampBuildingKind.Treasury => mainTreasuryRuinModel,
            _ => null,
        } : RuinOf(kind);

        private GameObject RuinOf(OrcCampBuildingKind kind) => kind switch
        {
            OrcCampBuildingKind.Watchtower => watchtowerRuinModel,
            OrcCampBuildingKind.WarTent => warTentRuinModel,
            OrcCampBuildingKind.Totem => totemRuinModel,
            OrcCampBuildingKind.Gate => gateRuinModel,
            OrcCampBuildingKind.GateTower => gateTowerRuinModel,
            OrcCampBuildingKind.Storehouse => storehouseRuinModel,
            OrcCampBuildingKind.BrutePit => brutePitRuinModel,
            OrcCampBuildingKind.ThrowerRange => throwerRangeRuinModel,
            OrcCampBuildingKind.Cage => cageRuinModel,
            _ => null,
        };

        /// <summary>How tall the tap target of a camp building is.</summary>
        private static float ClickHeight(OrcCampBuildingKind kind) => kind switch
        {
            OrcCampBuildingKind.Watchtower => 3f,
            OrcCampBuildingKind.GateTower => 2.6f,
            OrcCampBuildingKind.Totem => 1.6f,
            OrcCampBuildingKind.Palisade => 1f,
            OrcCampBuildingKind.Gate => 1.4f,
            _ => 1.2f,
        };

        /// <param name="checkGround">False for a camp coming back from a save: its ground was checked when it went up, and on load the map's colliders may not be ready to be asked again.</param>
        private OrcCamp CreateCamp(Vector2Int portalCell, OrcPortal portal, int quarters, int rank, int maxLevel, bool cleared,
            bool checkGround = true)
        {
            var grid = GridManager.Instance;
            var root = new GameObject($"OrcCamp {portalCell.x},{portalCell.y}");
            root.transform.position = grid.GetFootprintCenterWorld(portalCell, PortalFootprint);
            var camp = root.AddComponent<OrcCamp>();
            camp.Initialize(portalCell, portal, quarters, rank, maxLevel, cleared);
            var mapApplier = MeshMapApplier.Instance;
            // without the raycasts, water is still known from the map's own data
            camp.Reserve(grid, cell => checkGround
                ? IsSolidGround(grid, mapApplier, cell, Vector2Int.one)
                : mapApplier == null || !mapApplier.IsWaterCell(cell));
            return camp;
        }

        /// <summary>Opens a fresh camp round a portal, at level 1, with a full garrison.</summary>
        private OrcCamp BuildCamp(OrcPortal portal, int rank, int maxLevel)
        {
            var quarters = OrcBaseLayout.QuarterTurns(portal.transform.eulerAngles.y);
            var camp = CreateCamp(portal.Cell, portal, quarters, rank, maxLevel, cleared: false);
            camp.Build(maxLevel == OrcBaseLayout.MainLevel ? OrcBaseLayout.MainLevel : 1, this);
            return camp;
        }

        /// <summary>
        /// The mountains the main base is backed into: per block of cells, one NavMesh obstacle
        /// (nothing walks through) and rock blocks laid over it, turned and alternated so the
        /// ridge does not read as a stamp. The cells are the camp's reserved ground, so nothing
        /// is ever built on them either.
        /// </summary>
        public GameObject SpawnMountains(OrcCamp camp, IReadOnlyList<RectInt> rects)
        {
            var grid = GridManager.Instance;
            var root = new GameObject($"OrcMountains {camp.PortalCell.x},{camp.PortalCell.y}");
            var n = 0;
            foreach (var rect in rects)
            {
                OrcBaseLayout.ToWorld(rect.min, rect.size, camp.Centre, camp.Quarters, out var min, out var size);
                var block = new GameObject("MountainBlock");
                block.transform.SetParent(root.transform, false);
                block.transform.position = grid.GetFootprintCenterWorld(min, size);
                var obstacle = block.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
                obstacle.center = new Vector3(0f, 1.5f, 0f);
                obstacle.size = new Vector3(size.x * grid.CellSize, 3f, size.y * grid.CellSize);
                obstacle.carving = true;
                if (mountainModels == null || mountainModels.Length == 0) continue;
                // rock blocks every three cells across the rect, the outer ones sunk a little
                for (var x = 1.5f; x < size.x; x += 3f)
                {
                    for (var z = 1.5f; z < size.y; z += 3f)
                    {
                        var model = mountainModels[(n * 7 + (int)x + (int)z) % mountainModels.Length];
                        var at = grid.CellToWorld(min) + new Vector3(x * grid.CellSize, grid.GroundHeight, z * grid.CellSize);
                        var rock = Instantiate(model, at, Quaternion.Euler(0f, 90f * ((n * 3 + (int)x) % 4), 0f), root.transform);
                        rock.name = "Mountain";
                        n++;
                    }
                }
            }
            return root;
        }

        /// <summary>
        /// Grows every camp still guarding a portal to the level the player's progression calls
        /// for (OrcBaseLayout.LevelFor). Called with the score the raid clock has just read, so it
        /// costs nothing extra. A camp only ever grows, and a grown camp is rebuilt whole.
        /// </summary>
        public void GrowCamps(int score)
        {
            var balance = BalanceConfig.Instance;
            // a copy: growing a camp rebuilds it, and the registry must not change under the loop
            foreach (var camp in new List<OrcCamp>(OrcCamp.All))
            {
                if (camp == null || camp.Cleared || camp.Portal == null) continue;
                var level = OrcBaseLayout.LevelFor(score, camp.Rank, balance.OrcCampLevel2Score, balance.OrcCampLevel3Score,
                    balance.OrcCampLevelScorePerRank, camp.MaxLevel);
                if (level > camp.Level) camp.GrowTo(level, this);
            }
        }

        /// <summary>Gives a portal the health and the look of its camp's level -- a camp that grows is rebuilt whole, its portal too.</summary>
        public void ApplyPortalLevel(OrcPortal portal, int level)
        {
            if (portal == null) return;
            if (level >= OrcBaseLayout.MainLevel)
            {
                // The great portal: one look of its own, never the next wave's source.
                portal.SetLevel(level, BalanceConfig.Instance.MainPortalHealth);
                if (mainPortalModel == null) return;
                var look = Instantiate(mainPortalModel, portal.transform, false);
                look.name = "PortalActive";
                portal.SetLooks(look, null);
                return;
            }
            portal.SetLevel(level, BalanceConfig.Instance.PortalHealthAt(level));
            var active = Pick(portalActiveModels, level);
            var passive = Pick(portalPassiveModels, level);
            if (active == null || passive == null) return;
            var activeLook = Instantiate(active, portal.transform, false);
            var passiveLook = Instantiate(passive, portal.transform, false);
            activeLook.name = "PortalActive";
            passiveLook.name = "PortalPassive";
            portal.SetLooks(activeLook, passiveLook);
        }

        /// <summary>One building of a camp's plan on its plot. The camp has already checked the plot is its own ground.</summary>
        public OrcCampBuilding SpawnCampBuilding(OrcCamp camp, CampPiece piece, Vector2Int cell, Vector2Int footprint)
        {
            EnsureMaterials();
            var grid = GridManager.Instance;
            var baseYaw = camp.Quarters * 90f;
            var yaw = baseYaw;
            GameObject model;
            if (piece.Kind == OrcCampBuildingKind.Palisade)
            {
                // The palisade autotiles like the town's fence: which of the four pieces, turned
                // how far, follows from which neighbours are wall (gates count).
                FenceShape.Resolve(camp.IsWallCell(cell + Vector2Int.up), camp.IsWallCell(cell + Vector2Int.right),
                    camp.IsWallCell(cell + Vector2Int.down), camp.IsWallCell(cell + Vector2Int.left),
                    out var variant, out var steps);
                if (piece.Level >= OrcBaseLayout.MainLevel)
                {
                    model = mainWallModels != null && mainWallModels.Length == 4 ? mainWallModels[(int)variant] : null;
                }
                else
                {
                    model = palisadeModels != null && palisadeModels.Length >= piece.Level * 4
                        ? palisadeModels[(piece.Level - 1) * 4 + (int)variant]
                        : null;
                }
                yaw = steps * 90f;
            }
            else
            {
                model = ModelOf(piece.Kind, piece.Level);
                // A gate's model runs its wall along its own Z with the outside on -X; in the camp
                // the wall runs across the front, the outside towards the town.
                if (piece.Kind == OrcCampBuildingKind.Gate || piece.Kind == OrcCampBuildingKind.GateTower) yaw = baseYaw - 90f;
            }

            var root = new GameObject($"OrcCamp{piece.Kind} {cell.x},{cell.y}");
            root.transform.SetPositionAndRotation(grid.GetFootprintCenterWorld(cell, footprint), Quaternion.Euler(0f, yaw, 0f));
            var building = root.AddComponent<OrcCampBuilding>();
            building.Initialize(piece.Kind, piece.Level, camp, cell, footprint, RuinOf(piece.Kind, piece.Level));

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
            // The palisade's carve is its camp's, one obstacle per straight run (OrcCamp).
            var height = ClickHeight(piece.Kind);
            var local = Quaternion.Inverse(root.transform.rotation) * new Vector3(footprint.x, 0f, footprint.y);
            var size = new Vector3(Mathf.Abs(local.x), 0f, Mathf.Abs(local.z));
            var click = root.AddComponent<BoxCollider>();
            click.isTrigger = true;
            click.center = new Vector3(0f, height / 2f, 0f);
            click.size = new Vector3(size.x * 0.95f, height, size.z * 0.95f);
            if (piece.Kind != OrcCampBuildingKind.Palisade)
            {
                var obstacle = root.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
                obstacle.size = new Vector3(size.x * 0.9f, 2f, size.z * 0.9f);
                obstacle.center = new Vector3(0f, 1f, 0f);
                obstacle.carving = true;
            }
            return building;
        }

        /// <summary>One guard of a camp: a `slot.Kind` orc of `slot.Level`, standing at `spawnAt` and holding `post`.</summary>
        public OrcUnit SpawnGuard(OrcCamp camp, GuardSlot slot, Vector3 post, Vector3? spawnAt = null)
        {
            EnsureMaterials();
            var unit = SpawnOrc(spawnAt ?? post, slot.Level, slot.Kind, scatter: false);
            unit.SetGuardPost(camp, post);
            return unit;
        }

        /// <summary>One saved camp: its portal's cell, its level, how big it may grow, its rank, its facing and whether it was plundered.</summary>
        public readonly struct CampRestoreState
        {
            public CampRestoreState(Vector2Int portalCell, int level, int maxLevel, int rank, int quarters, bool cleared)
            {
                PortalCell = portalCell;
                Level = level;
                MaxLevel = maxLevel;
                Rank = rank;
                Quarters = quarters;
                Cleared = cleared;
            }

            public Vector2Int PortalCell { get; }
            public int Level { get; }
            public int MaxLevel { get; }
            public int Rank { get; }
            public int Quarters { get; }
            public bool Cleared { get; }
        }

        /// <summary>
        /// Puts the camps back from the save: each rebuilt at the level it had reached, then every
        /// building the save names set back to the health it had -- 0 breaks it on the spot, leaving
        /// its ruin -- with a full garrison (guards are not saved; the spawners muster them again).
        /// A save from before the walled bases gets fresh level-1 camps round its standing portals.
        /// </summary>
        private void RestoreCamps(IReadOnlyList<CampRestoreState> camps, IReadOnlyList<CampBuildingRestoreState> campBuildings)
        {
            if (camps == null || camps.Count == 0)
            {
                var rank = 0;
                foreach (var portal in PortalsByDistance())
                {
                    BuildCamp(portal, rank++, OrcBaseLayout.MaxLevel);
                }
                return;
            }

            foreach (var state in camps)
            {
                OrcPortal portal = null;
                foreach (var p in OrcPortal.All)
                {
                    if (p.Cell == state.PortalCell) portal = p;
                }
                var camp = CreateCamp(state.PortalCell, portal, state.Quarters, state.Rank, state.MaxLevel, state.Cleared || portal == null,
                    checkGround: false);
                camp.Build(Mathf.Max(1, state.Level), this);
                if (campBuildings == null) continue;
                foreach (var entry in campBuildings)
                {
                    if (entry.PortalCell == state.PortalCell) camp.ApplySavedHealth(entry.Kind, entry.Cell, entry.Health);
                }
            }
        }

        /// <summary>The standing portals, nearest the Town Hall first -- the order a camp's rank follows.</summary>
        private static List<OrcPortal> PortalsByDistance()
        {
            var portals = new List<OrcPortal>(OrcPortal.All);
            if (TryFindTownHallPosition(out var townHall))
            {
                portals.Sort((a, b) => (a.transform.position - townHall).sqrMagnitude
                    .CompareTo((b.transform.position - townHall).sqrMagnitude));
            }
            return portals;
        }

        /// <summary>
        /// The camp round a closed portal is plundered: the player takes the loot (more for a
        /// bigger camp) and the orcs need time to regroup -- the next raid is put back by the
        /// breather. Both are the user's answer (2026-10-16) to what closing a portal buys.
        /// </summary>
        private void PlunderCamp(OrcPortal portal)
        {
            var camp = OrcCamp.Of(portal);
            if (camp == null || camp.Cleared) return;
            var level = Mathf.Max(1, camp.Level);
            camp.MarkCleared();

            var balance = BalanceConfig.Instance;
            var coins = balance.OrcCampLootCoins * level;
            var wood = balance.OrcCampLootWood * level;
            var stone = balance.OrcCampLootStone * level;
            var resources = CityBuilder.Resources.ResourceManager.Instance;
            if (resources != null)
            {
                resources.Add(CityBuilder.Resources.ResourceType.Coins, coins);
                resources.Add(CityBuilder.Resources.ResourceType.Wood, wood);
                resources.Add(CityBuilder.Resources.ResourceType.Stone, stone);
            }
            _raidTimer += balance.OrcCampBreatherSeconds;
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Format("#log_camp_plundered", coins, wood, stone),
                EventSeverity.Good, EventGlyph.Portal, null, portal.transform.position));
        }

        /// <summary>The closed portal's ruin, on the plot the portal stood on. Scenery only: no collider, nothing to attack.</summary>
        private void SpawnRuin(Vector2Int cell, Vector3 position, Quaternion rotation, bool main = false)
        {
            var model = main ? mainPortalRuinModel : portalClosedModel;
            if (model == null) return;
            var ruin = Instantiate(model, position, rotation);
            ruin.name = $"OrcPortalRuin {cell.x},{cell.y}";
        }

        /// <summary>
        /// A portal turns its front -- the side the whirlpool and the steps face -- towards the
        /// Town Hall, snapped to a quarter turn so the square platform keeps to its plot.
        /// </summary>
        private static Quaternion FacingTownHall(Vector3 position)
        {
            if (!TryFindTownHallPosition(out var townHall)) return Quaternion.identity;
            return FacingFrom(position, townHall);
        }

        private static Quaternion FacingFrom(Vector3 position, Vector3 townHall)
        {
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
            var portals = RaidSources();
            if (portals.Count == 0) return;

            var size = ComputeRaidSize(score);
            var level = ComputeOrcLevel(score);

            // Out of the portal that was lit for it; then the next one is picked and lit.
            var source = _nextSource != null && _nextSource.CurrentHealth > 0
                ? _nextSource
                : portals[Random.Range(0, portals.Count)];
            // Out of the camp's front gate, not the portal: the portal stands inside the walls, and
            // a raid must not have to break its own palisade to get out.
            var camp = OrcCamp.Of(source);
            var origin = camp != null ? camp.RaidExit : source.transform.position;
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

            // A raid's raider is drawn as the brute from BruteFromLevel (raids grow past the
            // grunt's three models); the bases' brutes are a kind of their own with their own levels.
            var brute = kind == OrcKind.Raider && level >= BruteFromLevel && Pick(bruteModels, 1) != null;
            var model = Pick(gruntModels, level);
            var figureName = "Grunt";
            if (kind == OrcKind.Thrower && throwerModel != null) { model = throwerModel; figureName = "Thrower"; }
            else if (kind == OrcKind.Chief && chiefModel != null) { model = chiefModel; figureName = "Chief"; }
            else if (kind == OrcKind.Boss && bossModel != null) { model = bossModel; figureName = "Boss"; }
            else if (kind == OrcKind.Brute && Pick(bruteModels, level) != null) { model = Pick(bruteModels, level); figureName = "Brute"; }
            else if (brute) { model = Pick(bruteModels, 1); figureName = "Brute"; }
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
