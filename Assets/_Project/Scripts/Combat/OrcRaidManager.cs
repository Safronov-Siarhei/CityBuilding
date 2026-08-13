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

        private const float RaidIntervalSeconds = 90f;
        private const int BaseRaidSize = 2;
        // +1 raider per this many elapsed days, capped below -- a slow ramp, not a hard wall.
        private const int DaysPerExtraRaider = 3;
        private const int MaxRaidSize = 8;
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
        private const string TownHallBuildingName = "TownHall";

        [SerializeField] private GameCalendar gameCalendar;

        private static readonly Color PortalColor = new Color(0.32f, 0.1f, 0.42f);
        private static readonly Color OrcSkinColor = new Color(0.28f, 0.42f, 0.22f);
        private static readonly Color OrcGearColor = new Color(0.22f, 0.2f, 0.18f);

        private Material _portalMaterial;
        private Material _orcSkinMaterial;
        private Material _orcGearMaterial;
        private Vector3 _portalPosition;
        private bool _portalSpawned;
        private float _raidTimer;

        /// <summary>Pauses the automatic raid clock without touching the portal. Set by the OrcSpawn cheat so hand-spawned squads can be observed in isolation; nothing in normal gameplay sets it.</summary>
        public bool RaidsSuspended { get; set; }

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
            _raidTimer = RaidIntervalSeconds;

            SpawnRaid();
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

            // Occupies its cell so the player can't drop a building on top of the arch.
            grid.SetAreaOccupied(cell, Vector2Int.one, true);
            // Testing convenience (see PortalFogRevealRadiusCells) -- the player can see where
            // raids come from instead of having them arrive out of unexplored fog.
            FogOfWarManager.Instance?.RevealPermanent(cell, PortalFogRevealRadiusCells);

            _portalSpawned = true;
            // Only starts counting once the portal actually exists, so the player isn't raided
            // from nowhere during the time it takes them to place the Town Hall.
            _raidTimer = RaidIntervalSeconds;
            EventLogManager.Instance?.Log("На карте открылся портал орков.");
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

        private void SpawnRaid()
        {
            var day = gameCalendar != null ? gameCalendar.CurrentDay : 1;
            var size = ComputeRaidSize(day);

            SpawnOrcs(_portalPosition, size, level: 1);
            EventLogManager.Instance?.Log($"Орки начали набег ({size})");
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

        /// <summary>Pure formula extracted so it's covered by an EditMode test without needing a live scene.</summary>
        public static int ComputeRaidSize(int day)
        {
            var bonus = Mathf.Max(0, day - 1) / DaysPerExtraRaider;
            return Mathf.Min(MaxRaidSize, BaseRaidSize + bonus);
        }

        private void SpawnOrc(Vector3 origin, int level)
        {
            var jitter = Random.insideUnitCircle * 1.5f;
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

            root.AddComponent<OrcUnit>().Initialize(level);
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

            _portalMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = PortalColor };
            _orcSkinMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = OrcSkinColor };
            _orcGearMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = OrcGearColor };
        }
    }
}
