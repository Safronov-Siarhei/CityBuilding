using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// First-pass Orc raid source: spawns a single portal near a corner of the map, then
    /// periodically spawns a squad of OrcUnits from it. Raid size grows slowly with the day count
    /// as a simple stand-in for the design backlog's full "player progression score" formula
    /// (buildings/citizens/soldiers/defense/production) -- that composite doesn't exist yet, and
    /// this slice has no player army to weigh into it anyway. Numbers here are first-pass, tunable.
    /// No win condition yet -- the portal isn't destructible (see OrcPortal); this is the threat
    /// side only, defended against with existing Wall/Tower/Gate/Barracks defense (see
    /// DefensiveBuilding) until a player army exists to go on the offensive.
    /// </summary>
    public class OrcRaidManager : MonoBehaviour
    {
        public static OrcRaidManager Instance { get; private set; }

        // Raid pacing, size and strength all come from the balance sheet's economy tab (raid_*),
        // and all three are read against the PLAYER'S PROGRESSION SCORE rather than the calendar
        // (see PlayerProgression). The day count used to do this job, and it measured how long the
        // player had sat there rather than what they had built.

        // Placeholder placement rule for testing, not the final design: the user's intent is
        // hand-authored portal locations per map (like the TreesArea zone), which don't exist as
        // map data yet. Until then one portal goes a fixed distance from the Town Hall -- close
        // enough to reach on foot while testing raids, and anchored to the player's base so it's
        // never stranded in an unreachable corner.
        private const float PortalDistanceFromTownHallMeters = 20f;
        // Directions tried around the Town Hall at each radius before widening the search.
        private const int PortalDirectionSamples = 16;
        private static readonly float[] PortalFallbackRadiiMeters = { 0f, -4f, 4f, -8f, 8f };
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
        private Vector3 _portalPosition;
        private Vector2Int _portalCell;
        private bool _portalSpawned;
        private float _raidTimer;

        /// <summary>Pauses the automatic raid clock without touching the portal. Set by the OrcSpawn cheat so hand-spawned squads can be observed in isolation; nothing in normal gameplay sets it.</summary>
        public bool RaidsSuspended { get; set; }

        /// <summary>Whether the portal has already been placed -- false only while the player has yet to put down a Town Hall for it to be anchored to.</summary>
        public bool PortalPlaced => _portalSpawned;

        /// <summary>Where it stands. The save keeps the cell rather than the world position, because that is what the placement rule produced and what the grid has reserved.</summary>
        public Vector2Int PortalCell => _portalCell;

        /// <summary>How long the settlement has before the next wave. Saved, so reloading is not a way to push the raid clock back to a full interval.</summary>
        public float SecondsUntilNextRaid => _raidTimer;

        /// <summary>
        /// Puts the raid source back the way the save left it: the portal where it stood and on the
        /// health the player had ground it down to, and the clock where it was.
        ///
        /// Single portal, matching what this class spawns today. The design calls for about five per
        /// map, hand-placed; when they arrive this and the save entry beside it both become lists.
        /// </summary>
        public void RestoreFromSave(bool portalPlaced, Vector2Int portalCell, int portalHealth, float secondsUntilNextRaid)
        {
            if (!portalPlaced) return;

            // Placed once and since destroyed: the flag alone stops Update from opening a fresh one
            // over the ruins of the one the player fought for.
            _portalSpawned = true;
            if (portalHealth <= 0) return;

            var grid = GridManager.Instance;
            if (grid == null) return;

            SpawnPortal(grid, portalCell);
            if (OrcPortal.All.Count > 0) OrcPortal.All[OrcPortal.All.Count - 1].SetCurrentHealth(portalHealth);

            // After SpawnPortal, which starts a fresh interval of its own.
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
            if (!_portalSpawned)
            {
                // The portal is positioned relative to the Town Hall, so nothing can happen until
                // the player has placed it. HasAny is an O(1) registry lookup (see
                // BuildingInstance), so polling it per frame is free -- the one real scan below
                // runs exactly once, on the frame the Town Hall appears. Also covers a loaded
                // save, where the Town Hall exists before this component's first Update and no
                // placement event ever fires.
                if (!BuildingInstance.HasAny(TownHallBuildingName)) return;

                TrySpawnPortalNearTownHall();
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
        /// Places the single portal on solid ground at roughly PortalDistanceFromTownHallMeters
        /// from the Town Hall, sampling directions around it and widening the radius if none of
        /// them land somewhere valid. "Valid" is MeshMapApplier.IsGroundAt, i.e. actually standing
        /// on the map's Ground mesh at the flat playable height -- which rules out the lake and
        /// the decorative cliffs, either of which would strand every spawned orc off the NavMesh
        /// and send them walking through the world in a straight line.
        /// </summary>
        private void TrySpawnPortalNearTownHall()
        {
            var grid = GridManager.Instance;
            if (grid == null) return;

            if (!TryFindTownHallPosition(out var townHallPosition))
            {
                // HasAny said one exists, so this means it was destroyed within the same frame.
                // Nothing to anchor to; try again next frame.
                return;
            }

            if (!TryFindPortalSpot(grid, townHallPosition, out var cell))
            {
                Debug.LogError($"OrcRaidManager: no valid ground found for the portal around the Town Hall at {townHallPosition} " +
                               $"({PortalDirectionSamples} directions x {PortalFallbackRadiiMeters.Length} radii, all water/cliff/occupied). " +
                               "No portal will exist and no raids will happen this session.");
                // Stops the per-frame retry: the map geometry won't change, so re-running this
                // every frame would just repeat the same failure forever.
                _portalSpawned = true;
                return;
            }

            SpawnPortal(grid, cell);
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

        private static bool TryFindPortalSpot(GridManager grid, Vector3 townHallPosition, out Vector2Int cell)
        {
            var mapApplier = MeshMapApplier.Instance;
            // Random starting angle so repeat playthroughs on the same map don't always put the
            // portal in the exact same spot relative to the base.
            var angleOffset = Random.Range(0f, Mathf.PI * 2f);

            foreach (var radiusDelta in PortalFallbackRadiiMeters)
            {
                var radius = PortalDistanceFromTownHallMeters + radiusDelta;
                if (radius <= 0f) continue;

                for (var i = 0; i < PortalDirectionSamples; i++)
                {
                    var angle = angleOffset + i * (Mathf.PI * 2f / PortalDirectionSamples);
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

        private void SpawnPortal(GridManager grid, Vector2Int cell)
        {
            _portalCell = cell;
            _portalPosition = grid.GetFootprintCenterWorld(cell, Vector2Int.one);

            EnsureMaterials();

            var root = new GameObject("OrcPortal");
            root.transform.position = _portalPosition;
            root.AddComponent<OrcPortal>();

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

            // Occupies its cell so the player can't drop a building on top of the arch.
            grid.SetAreaOccupied(cell, Vector2Int.one, true);
            // Testing convenience (see PortalFogRevealRadiusCells) -- the player can see where
            // raids come from instead of having them arrive out of unexplored fog.
            FogOfWarManager.Instance?.RevealPermanent(cell, PortalFogRevealRadiusCells);

            _portalSpawned = true;
            // Only starts counting once the portal actually exists, so the player isn't raided
            // from nowhere during the time it takes them to place the Town Hall.
            //
            // The full interval rather than the score's, deliberately: a portal opens on the frame
            // the Town Hall goes down, when the score IS a Town Hall and five settlers, and asking
            // PlayerProgression for that would buy a scan over the buildings to be told so.
            _raidTimer = BalanceConfig.Instance.RaidIntervalSeconds;
            EventLogManager.Instance?.Log(Localization.Get("#log_portal_opened"));
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
            var size = ComputeRaidSize(score);
            var level = ComputeOrcLevel(score);

            SpawnOrcs(_portalPosition, size, level);
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
