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

        private Material _portalMaterial;
        private Material _orcSkinMaterial;
        private Material _orcGearMaterial;
        private bool _portalsPlaced;
        private float _raidTimer;

        /// <summary>Pauses the automatic raid clock without touching the portal. Set by the OrcSpawn cheat so hand-spawned squads can be observed in isolation; nothing in normal gameplay sets it.</summary>
        public bool RaidsSuspended { get; set; }

        /// <summary>Whether the map's portals have already been opened -- false only while the player has yet to put down a Town Hall for them to be anchored to.</summary>
        public bool PortalsPlaced => _portalsPlaced;

        /// <summary>How long the settlement has before the next wave. Saved, so reloading is not a way to push the raid clock back to a full interval.</summary>
        public float SecondsUntilNextRaid => _raidTimer;

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
        public void RestoreFromSave(bool portalsPlaced, IReadOnlyList<PortalRestoreState> portals, float secondsUntilNextRaid)
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
        public OrcUnit RestoreOrc(Vector3 position, int level, int currentHealth)
        {
            EnsureMaterials();
            var unit = SpawnOrc(position, level, scatter: false);
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

                SpawnPortal(grid, cell);
                opened++;
            }

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
            EventLogManager.Instance?.Log(opened == 1
                ? Localization.Get("#log_portal_opened")
                : Localization.Format("#log_portals_opened", opened));
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

                    if (!grid.IsWithinBounds(candidateCell, Vector2Int.one) || !grid.IsAreaFree(candidateCell, Vector2Int.one)) continue;
                    if (mapApplier != null && mapApplier.IsWaterCell(candidateCell)) continue;

                    var worldPosition = grid.GetFootprintCenterWorld(candidateCell, Vector2Int.one);
                    if (mapApplier != null && !mapApplier.IsGroundAt(worldPosition)) continue;

                    cell = candidateCell;
                    return true;
                }
            }

            cell = default;
            return false;
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
            var position = grid.GetFootprintCenterWorld(cell, Vector2Int.one);

            EnsureMaterials();

            var root = new GameObject("OrcPortal");
            root.transform.position = position;
            var portal = root.AddComponent<OrcPortal>();
            portal.SetCell(cell);

            // A simple freestanding stone arch -- two posts and a lintel, in the same
            // cube-primitive style as every other procedural prop in the game.
            AddPortalPart(root.transform, new Vector3(-0.6f, 1.5f, 0f), new Vector3(0.4f, 3f, 0.4f));
            AddPortalPart(root.transform, new Vector3(0.6f, 1.5f, 0f), new Vector3(0.4f, 3f, 0.4f));
            AddPortalPart(root.transform, new Vector3(0f, 3.1f, 0f), new Vector3(1.6f, 0.4f, 0.4f));

            // One click target covering the whole arch (the parts' own colliders are stripped), so
            // the player can tap the portal to order an assault on it -- see ArmyOrderInput. A
            // TRIGGER, like every other click affordance in this project: it must not become a
            // wall that raiding orcs and marching soldiers get wedged against.
            var clickCollider = root.AddComponent<BoxCollider>();
            clickCollider.isTrigger = true;
            clickCollider.center = new Vector3(0f, 1.7f, 0f);
            clickCollider.size = new Vector3(2f, 3.4f, 0.8f);

            // Occupies its cell so the player can't drop a building on top of the arch -- and, with
            // several portals, so the search for the next slot cannot pick a cell one of them is
            // already standing on.
            grid.SetAreaOccupied(cell, Vector2Int.one, true);
            // Testing convenience (see PortalFogRevealRadiusCells) -- the player can see where
            // raids come from instead of having them arrive out of unexplored fog.
            FogOfWarManager.Instance?.RevealPermanent(cell, PortalFogRevealRadiusCells);

            return portal;
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

            SpawnOrcs(portals[Random.Range(0, portals.Count)].transform.position, size, level);
            // Two messages, because "the orcs are raiding (5)" says nothing about the wave that has
            // quietly become five times as hard as the last one at the very same size.
            EventLogManager.Instance?.Log(level > 1
                ? Localization.Format("#log_raid_levelled", size, level)
                : Localization.Format("#log_raid", size));
        }

        /// <summary>
        /// Spawns a squad at an explicit position -- the entry point the OrcSpawn cheat uses to
        /// place orcs at a chosen portal, at a chosen level, on demand. Safe to call before the
        /// portal exists: it builds its own materials rather than assuming SpawnPortal ran first.
        /// </summary>
        public void SpawnOrcs(Vector3 origin, int count, int level)
        {
            if (count <= 0) return;

            EnsureMaterials();
            for (var i = 0; i < count; i++)
            {
                SpawnOrc(origin, level);
            }
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
            var jitter = scatter ? Random.insideUnitCircle * 1.5f : Vector2.zero;
            var spawnPos = origin + new Vector3(jitter.x, 0f, jitter.y);

            var root = new GameObject(level > 1 ? $"Orc (lvl {level})" : "Orc");
            root.transform.position = spawnPos;
            // Visibly bigger with level, so a cheat-spawned elite is tellable from a raider at a
            // glance without opening the inspector. Deliberately sublinear -- a level 5 orc has 5x
            // the health but must not be a five-times-wider wall of cubes.
            var scale = 1f + (Mathf.Max(1, level) - 1) * 0.12f;
            root.transform.localScale = Vector3.one * scale;

            AddCubePart(root.transform, "Body", new Vector3(0f, 0.3f, 0f), new Vector3(0.36f, 0.6f, 0.36f), _orcSkinMaterial);
            AddCubePart(root.transform, "Head", new Vector3(0f, 0.72f, 0f), new Vector3(0.26f, 0.26f, 0.26f), _orcSkinMaterial);
            AddCubePart(root.transform, "Shoulders", new Vector3(0f, 0.55f, 0f), new Vector3(0.5f, 0.14f, 0.4f), _orcGearMaterial);

            var controller = root.AddComponent<CharacterController>();
            controller.height = 0.86f;
            controller.radius = 0.2f;
            controller.center = new Vector3(0f, 0.43f, 0f);
            controller.skinWidth = 0.02f;
            controller.minMoveDistance = 0f;

            var unit = root.AddComponent<OrcUnit>();
            unit.Initialize(level);
            return unit;
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
