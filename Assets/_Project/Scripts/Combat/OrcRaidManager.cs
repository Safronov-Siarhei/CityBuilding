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
    public partial class OrcRaidManager : MonoBehaviour
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
        // The Invasion jungle's monsters (Tools/blender/wild.py, Models/Wild), jointed.
        [SerializeField] private GameObject wolfModel;
        [SerializeField] private GameObject bearModel;
        [SerializeField] private GameObject trollModel;
        [SerializeField] private GameObject giantModel;
        // The Invasion mode's round camps (Tools/blender/orcringcamp1.py): the ring wall's section and the open gateway.
        [SerializeField] private GameObject ringWallModel;
        [SerializeField] private GameObject ringWallRuinModel;
        [SerializeField] private GameObject ringGateModel;
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

        /// <summary>How far round a designed spot (metres) a camp may move to find ground it fits on.</summary>
        private const float SpotSearchMeters = 8f;

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

        /// <summary>One orc of a given kind and level -- the Invasion run's waves make up their own mixes (InvasionWaves). Works with the raids switched off, as they are in a run.</summary>
        public OrcUnit SpawnOrcOfKind(Vector3 origin, int level, OrcKind kind)
        {
            EnsureMaterials();
            return SpawnOrc(origin, level, kind);
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

            var label = OrcUnit.IsMonster(kind) ? kind.ToString() : kind == OrcKind.Raider ? "Orc" : $"Orc {kind}";
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
            if (OrcUnit.IsMonster(kind))
            {
                model = MonsterModel(kind);
                figureName = kind.ToString();
            }
            if (model != null)
            {
                // The models are authored at their real size (a grunt 0.58 m, a brute 0.73), so
                // the root's unit scale is divided back out -- and for the brute, the growth the
                // levels below it already bought, or a level-4 brute would stand a metre tall.
                var figure = Instantiate(model, root.transform, false);
                figure.name = figureName;
                var undo = UnitSize.Scale * (brute ? LevelScale(BruteFromLevel) : 1f);
                figure.transform.localScale = Vector3.one / undo;
                // modelled facing Blender's -y, which lands facing back along the root's forward:
                // turned round, or they walk backwards (the user, 2026-10-22)
                figure.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * figure.transform.localRotation;
                // Units stay out of the shadow pass, like the citizens.
                SceneryShadows.DisableCasting(figure);
                Maps.SeasonStyle.NoSnow(figure);
                // the jungle's beasts are jointed: they stand, run and strike; an orc is one mesh,
                // and waddles and lunges as a whole
                if (OrcUnit.IsMonster(kind)) root.AddComponent<FigureAnimator>().Bind(figure.transform);
                else root.AddComponent<OrcBodyAnimator>().Bind(figure.transform);
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

        private GameObject MonsterModel(OrcKind kind)
        {
            switch (kind)
            {
                case OrcKind.Wolf: return wolfModel;
                case OrcKind.Bear: return bearModel;
                case OrcKind.Troll: return trollModel;
                case OrcKind.Giant: return giantModel;
                default: return null;
            }
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
