using CityBuilder.Core;
using CityBuilder.Grid;
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
        // Offset from the grid's own edge, not its center -- keeps the portal away from wherever
        // the player's Town Hall ends up (always placed first, near the middle of the buildable
        // area) without needing per-map authoring data that doesn't exist yet.
        private const int PortalCornerMarginCells = 15;

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
            _raidTimer = RaidIntervalSeconds;
            SpawnPortal();
        }

        private void Update()
        {
            if (!_portalSpawned) return;

            _raidTimer -= Time.deltaTime;
            if (_raidTimer > 0f) return;
            _raidTimer = RaidIntervalSeconds;

            SpawnRaid();
        }

        private void SpawnPortal()
        {
            var grid = GridManager.Instance;
            if (grid == null) return;

            var cell = new Vector2Int(grid.GridSize.x - PortalCornerMarginCells, grid.GridSize.y - PortalCornerMarginCells);
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

            _portalSpawned = true;
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

            for (var i = 0; i < size; i++)
            {
                SpawnOrc();
            }

            EventLogManager.Instance?.Log($"Орки начали набег ({size})");
        }

        /// <summary>Pure formula extracted so it's covered by an EditMode test without needing a live scene.</summary>
        public static int ComputeRaidSize(int day)
        {
            var bonus = Mathf.Max(0, day - 1) / DaysPerExtraRaider;
            return Mathf.Min(MaxRaidSize, BaseRaidSize + bonus);
        }

        private void SpawnOrc()
        {
            var jitter = Random.insideUnitCircle * 1.5f;
            var spawnPos = _portalPosition + new Vector3(jitter.x, 0f, jitter.y);

            var root = new GameObject("Orc");
            root.transform.position = spawnPos;

            AddCubePart(root.transform, "Body", new Vector3(0f, 0.3f, 0f), new Vector3(0.36f, 0.6f, 0.36f), _orcSkinMaterial);
            AddCubePart(root.transform, "Head", new Vector3(0f, 0.72f, 0f), new Vector3(0.26f, 0.26f, 0.26f), _orcSkinMaterial);
            AddCubePart(root.transform, "Shoulders", new Vector3(0f, 0.55f, 0f), new Vector3(0.5f, 0.14f, 0.4f), _orcGearMaterial);

            var controller = root.AddComponent<CharacterController>();
            controller.height = 0.86f;
            controller.radius = 0.2f;
            controller.center = new Vector3(0f, 0.43f, 0f);
            controller.skinWidth = 0.02f;
            controller.minMoveDistance = 0f;

            root.AddComponent<OrcUnit>();
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
