using System.Collections;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.CameraControl;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using CityBuilder.Saving;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Campaign mission 2 «Оборона города» (Design/features/campaign.md; the user, 2026-11-12): the town
    /// Ардан ran to holds out on three fronts until the King's help comes. Played on the gameplay scene
    /// on its own mesh map (Tools/blender/mission2.py, map id Mission2); the town stands from the start
    /// (Assets/_Project/Campaign/Mission2Town.json, Tools/campaign/mission2_town.py, plan v2).
    ///
    /// The player runs Ардан alone with the stick (the sword strikes by itself); he levels on his own kills
    /// and opens four skills (CityDefenceMode.Arden.cs), and he is MORTAL -- dead = lost; the healers on the
    /// square mend him. The garrison fights by itself (CityDefenceMode.Garrison.cs): a squad inside each
    /// gate (infantry, crossbows, a monk), archers and crossbows on the walls, patrols in the streets; the
    /// town's people run for their houses. Waves come from all three sides at once (W, N, E) over the
    /// river's fords, growing, with short pauses, much harder past half the part. Past the fields and the
    /// fords it is dark and he cannot go (CityDefenceMode.World.cs). Part 1 (8 minutes): hold until the
    /// courier is away. Part 2 (8 minutes, to be specified with the user): inside the inner walls. Lost:
    /// an inner gate, the castle or Ардан. Numbers: m2_* (BalanceConfig.CityDefence).
    /// The cutscenes (Ардан at the gate, the courier at the King's, the help) come last, at the user's word.
    /// </summary>
    public partial class CityDefenceMode : MonoBehaviour
    {
        public const string MapId = "Mission2";
        public const string LevelId = "m2";
        /// <summary>Its place in the campaign (1-based): the support banner comes after won levels from the 2nd on.</summary>
        public const int LevelNumber = 2;
        public const string GameplayScene = "CityBuilder";

        /// <summary>Which mission this mode plays (the user, 2026-11-23): mission 2's siege, or mission 3b «Клетка» -- the King
        /// escorting the cage cart to the capital (CityDefenceMode.Escort.cs) on its own map (Mission3).</summary>
        public enum Mission { Siege, Escort }

        public static Mission Kind { get; private set; } = Mission.Siege;
        public const string EscortMapId = "Mission3";
        public const string EscortLevelId = "m3b";
        public const int EscortLevelNumber = 3;

        /// <summary>The map the mission waits for, by its kind.</summary>
        public static string ActiveMapId => Kind == Mission.Escort ? EscortMapId : MapId;

        /// <summary>Its place in the campaign, by its kind (the support banner after a win).</summary>
        public static int CurrentLevelNumber => Kind == Mission.Escort ? EscortLevelNumber : LevelNumber;
        /// <summary>PlayerPrefs of the old two-part mission (a restart began at part 2): cleared on every start since the
        /// mission is one part (the user, 2026-11-13).</summary>
        public const string Part2Key = "m2_part2_saved";

        /// <summary>Part1 = the siege, the mission's one part since 2026-11-13 (the name kept for the tests).</summary>
        public enum Stage { Setup, Part1, Won, Lost }

        public enum LossReason { None, Square, Castle, Arden, Cage }

        /// <summary>
        /// What the player holds (the user, 2026-11-13: the two parts made one, 11 minutes): the outer gate; once it is
        /// broken, the inner gate; once that is broken, the central square -- an orc on it loses the mission.
        /// </summary>
        public enum Goal { OuterGate, InnerGate, Square }

        public Goal CurrentGoal { get; private set; }

        /// <summary>The gate the goal is about (its bar under the goal plaque), null once the square is the goal.</summary>
        public BuildingInstance GoalGate => CurrentGoal == Goal.OuterGate ? OuterGates[0] : CurrentGoal == Goal.InnerGate ? InnerGates[0] : null;

        /// <summary>The goal changed (the HUD's banner).</summary>
        public event System.Action<Goal> GoalChanged;

        /// <summary>The courier has reached the capital (m2_reinforce_sec before the end): the clock counts down the help.</summary>
        public bool HelpComing => TimeLeft <= B.reinforceSeconds;

        /// <summary>m2_reinforce_sec: the help's last stretch of the clock.</summary>
        public float ReinforceSeconds => B.reinforceSeconds;

        /// <summary>Set by the campaign map before the gameplay scene loads (Begin).</summary>
        public static bool Pending;

        public static CityDefenceMode Instance { get; private set; }

        /// <summary>This game is mission 2.</summary>
        public static bool IsActive => Instance != null && Instance._active;

        [SerializeField] private BuildingPlacer buildingPlacer;
        [SerializeField] private MonoBehaviour[] notInAMission = new MonoBehaviour[0];
        [SerializeField] private TextAsset town;
        // the HUD's pictures (SetupProjectCityScene wires them)
        [SerializeField] private MissionHud.Art hudArt;
        [SerializeField] private Sprite[] skillGlyphs = new Sprite[0];
        [SerializeField] private Sprite lockGlyph;
        // the skill label's stat pictures (the user's choice 2026-11-14, concept A): damage, radius, stun, cooldown,
        // heal, allies -- Swords, Hud_Radius, Star, Hud_Rotate, Heal, Hud_People
        [SerializeField] private Sprite[] statGlyphs = new Sprite[0];
        [SerializeField] private Sprite gateIcon;
        // the captain of the town guard (Tools/blender/soldier_captain.py, approved 2026-11-14): a man-at-arms in his own figure
        [SerializeField] private GameObject captainModel;

        public MissionHud.Art HudArt => hudArt;
        public Sprite SkillGlyph(int skill) => skill >= 0 && skill < skillGlyphs.Length ? skillGlyphs[skill] : null;
        public Sprite LockGlyph => lockGlyph;
        public Sprite StatGlyph(Stat stat) => (int)stat < statGlyphs.Length ? statGlyphs[(int)stat] : null;

        public enum Stat { Damage, Radius, Stun, Cooldown, Heal, Allies }

        /// <summary>A skill's label figures (concept A): picture + the number as shown, cooldown last.</summary>
        public void SkillStats(int skill, List<(Stat stat, string text)> into)
        {
            into.Clear();
            var b = B;
            string Sec(float v) => Localization.Format("#m2_stat_sec", Num(v));
            string Cells(float v) => Localization.Format("#m2_stat_cells", Num(v));
            string Pct(float v) => Localization.Format("#m2_stat_pct", Mathf.RoundToInt(v * 100f));
            switch (skill)
            {
                case Cry:
                    into.Add((Stat.Damage, Localization.Format("#m2_stat_mult", Num(b.cryDamageMult)) + " · " + Sec(b.crySeconds)));
                    into.Add((Stat.Stun, Sec(b.cryStunSeconds)));
                    into.Add((Stat.Radius, Cells(b.cryRadius)));
                    break;
                case Whirl:
                    into.Add((Stat.Damage, OrcShare(b.whirlOrcShare).ToString()));
                    into.Add((Stat.Radius, Cells(b.whirlRadius)));
                    break;
                case Dash:
                    into.Add((Stat.Stun, Sec(b.dashStunSeconds)));
                    into.Add((Stat.Damage, OrcShare(b.dashOrcShare).ToString()));
                    into.Add((Stat.Radius, Cells(b.dashSeekCells)));
                    break;
                default:
                    into.Add((Stat.Heal, Pct(b.breathHeal)));
                    into.Add((Stat.Allies, Pct(b.breathAllyHeal)));
                    into.Add((Stat.Radius, Cells(b.breathRadius)));
                    break;
            }
            into.Add((Stat.Cooldown, Sec(CooldownOf(skill))));
        }

        private static string Num(float v) => Mathf.Approximately(v, Mathf.Round(v)) ? Mathf.RoundToInt(v).ToString() : v.ToString("0.#", System.Globalization.CultureInfo.GetCultureInfo("ru-RU"));
        public Sprite GateIcon => gateIcon;

        private bool _active;

        public Stage Current { get; private set; } = Stage.Setup;
        public LossReason Loss { get; private set; }

        /// <summary>The mission's clock: seconds until the help comes (the courier reaches the capital m2_reinforce_sec before).</summary>
        public float TimeLeft { get; private set; }

        /// <summary>Past half the part: the waves much harder.</summary>
        public bool Late { get; private set; }

        public int Wave { get; private set; }

        /// <summary>How long the town has held in this part (the end window's numbers).</summary>
        public float HeldSeconds => Kind == Mission.Escort ? _escortClock : Mathf.Max(0f, B.missionSeconds - TimeLeft);

        /// <summary>The gates by lane (W, N, E). Since the user's third playtest the castle has ONE gate in each ring,
        /// so every lane's entry is that same gate (null once broken).</summary>
        public BuildingInstance[] OuterGates { get; } = new BuildingInstance[3];
        public BuildingInstance[] InnerGates { get; } = new BuildingInstance[3];

        private bool AnyGateStands()
        {
            foreach (var g in OuterGates) if (g != null && g.CurrentHealth > 0) return true;
            foreach (var g in InnerGates) if (g != null && g.CurrentHealth > 0) return true;
            return false;
        }

        public BuildingInstance Castle { get; private set; }

        /// <summary>The mission ended: true won, false lost (the HUD's end panel).</summary>
        public event System.Action<bool> Ended;

        /// <summary>A part began (1 or 2): the HUD's banner.</summary>
        public event System.Action<int> PartBegan;

        private static CityDefenceBalance B => BalanceConfig.Instance.CityDefence;

        [System.Serializable] private class Blueprint
        {
            public int width, height;
            public float[] origin;
            public List<Lane> lanes = new List<Lane>();
            public List<Piece> buildings = new List<Piece>();
            public List<Field> fields = new List<Field>();
            public List<Spot> fires = new List<Spot>();
            public List<Prop> decor = new List<Prop>();
        }

        [System.Serializable] private class Lane { public float[] points; }

        [System.Serializable] private class Piece { public string id; public int x, y, w, h, rot, level; public string state; }

        private Blueprint _plan;
        private Vector2 _origin;
        private readonly OrcLane[] _lanes = new OrcLane[3];
        private readonly Vector3[] _spawns = new Vector3[3];
        private readonly List<BuildingInstance> _lowerTown = new List<BuildingInstance>();
        private readonly List<BuildingInstance> _houses = new List<BuildingInstance>();
        private readonly List<BuildingInstance> _outerWall = new List<BuildingInstance>();
        // the orcs of the waves, by lane (the minimap's «!», the wave beaten)
        private readonly List<OrcUnit>[] _laneOrcs = { new List<OrcUnit>(), new List<OrcUnit>(), new List<OrcUnit>() };
        private float _nextWave;
        private Vector3 _square;
        // where Ардан stands at the start: north of the square, the castle behind the camera (the user's
        // second playtest: «замок мешает обзору»)
        private Vector3 _start;

        /// <summary>The campaign map's «Играть» on level 2: the gameplay scene on this mission's map.</summary>
        public static void Begin()
        {
            // never into the mission with the game stopped: Start waits for a physics step, and at timeScale 0 none
            // comes -- the town was never raised (the user, 2026-11-23: the prologue started again showed an empty map
            // asking for a castle)
            Time.timeScale = 1f;
            _skipIntroOnce = false;
            Kind = Mission.Siege;
            GameSessionIntent.SaveNameToLoad = null;
            GameSessionIntent.NewGameMapId = MapId;
            GameSessionIntent.NewGameSeason = MapSeason.Summer;
            GameSessionIntent.NewGameIsInvasion = false;
            Pending = true;
            UI.LoadingScreen.IntoGame(GameplayScene);
        }

        /// <summary>The campaign map's film flag «Пролог» (the user, 2026-11-23: the level-3 prologue its own icon on the
        /// map, a video glyph on it): mission 2's town, sacked, and only the prologue (Prologue.cs); then back to the map.</summary>
        public static void BeginPrologue()
        {
            Begin();
            _filmOnly = true;
        }

        private static bool _filmOnly;

        /// <summary>The campaign map's «Играть» on m3b «Клетка»: the gameplay scene on the escort's map.</summary>
        public static void BeginEscort()
        {
            Time.timeScale = 1f;
            _skipIntroOnce = false;
            _filmOnly = false;
            Kind = Mission.Escort;
            GameSessionIntent.SaveNameToLoad = null;
            GameSessionIntent.NewGameMapId = EscortMapId;
            GameSessionIntent.NewGameSeason = MapSeason.Summer;
            GameSessionIntent.NewGameIsInvasion = false;
            Pending = true;
            UI.LoadingScreen.IntoGame(GameplayScene);
        }

        private void Awake()
        {
            Instance = this;
            _active = Pending;
            Pending = false;
            if (!_active) return;
            foreach (var behaviour in notInAMission)
                if (behaviour != null) behaviour.enabled = false;
            // fighters close in to strike (the user's third playtest: «подходили вплотную»)
            MeleeReach.Factor = B.meleeReachMult;
            if (Kind == Mission.Escort)
            {
                // the escort has no town of its own people and no wall fights (its capital's walls are scenery)
                var folk = GetComponent<Citizens.HousePeople>();
                if (folk != null) folk.enabled = false;
                return;
            }
            // nobody fights across a wall (the user's fourth playtest)
            OrcUnit.WallBetween = WallBetween;
            SoldierUnit.PenBreach = Breach;
            SoldierUnit.WallBlocks = SoldierBlocked;
            SoldierUnit.HealBlocks = HealBlocked;
            // no residents grow in the houses (their bars stood over every roof, the user's first playtest)
            var people = GetComponent<Citizens.HousePeople>();
            if (people != null) people.enabled = false;
        }

        /// <summary>The game put away mid-mission (the user's twelfth playtest: «свернёт и вернётся -- миссия должна
        /// продолжиться»): it waits behind the game menu, frozen, and goes on from there when the menu is closed.
        /// A phone that kills the app while it is away still loses the mission (no mission save).</summary>
        private void OnApplicationPause(bool paused)
        {
            // not over the opening scene: it hides the HUD, so the menu would pause the game unseen (the user's first look,
            // 2026-11-15: «сцена не проигрывается» -- the screenshot's lost focus opened it)
            if (!paused || Current != Stage.Part1 || _intro != null || _capital != null || PrologueAlone) return;   // the film alone: no mission to pause
            var menu = FindAnyObjectByType<UI.GameMenuController>();
            if (menu != null && !menu.IsOpen) menu.Open();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            BuildingInstance.OnDestroyedInCombat -= OnBuildingDown;
            OrcUnit.Died -= OnOrcDied;
            OrcUnit.Wounded -= OnOrcWounded;
            SoldierUnit.Healed -= OnHealed;
            ForgetDarkness();
            if (_active)
            {
                RTSCameraController.GesturesLocked = false;
                MeleeReach.Factor = 1f;
                OrcUnit.WallBetween = null;
                SoldierUnit.PenBreach = null;
                SoldierUnit.WallBlocks = null;
                SoldierUnit.HealBlocks = null;
                SoldierUnit.StairFootAt = null;
            }
        }

        private IEnumerator Start()
        {
            if (!_active) yield break;
            if (Time.timeScale <= 0f) Time.timeScale = 1f;
            for (var frame = 0; frame < 600; frame++)
            {
                var applier = MeshMapApplier.Instance;
                if (applier != null && applier.CurrentMapId == ActiveMapId && GridManager.Instance != null) break;
                yield return null;
            }
            yield return new WaitForFixedUpdate();
            Physics.SyncTransforms();
            Setup();
        }

        // ------------------------------------------------------------------ the town

        private void Setup()
        {
            if (Kind == Mission.Escort)
            {
                SetupEscort();
                return;
            }
            _plan = town != null ? JsonUtility.FromJson<Blueprint>(town.text) : null;
            if (_plan == null || GridManager.Instance == null || buildingPlacer == null)
            {
                Debug.LogError("CityDefenceMode: no town plan (Campaign/Mission2Town.json) or no grid -- the mission cannot start.");
                return;
            }
            _origin = _plan.origin != null && _plan.origin.Length == 2 ? new Vector2(_plan.origin[0], _plan.origin[1]) : new Vector2(-50f, 46f);
            if (FogOfWarManager.Instance != null) FogOfWarManager.Instance.DisabledCheat = true;
            RaiseTown();
            if (Castle != null) buildingPlacer.MarkMandatoryBuildingAlreadyPlaced();
            buildingPlacer.ClearSelection();
            Resources.ResourceManager.Instance?.SetInfiniteResources(true);
            LayLanes();
            _square = World(50.5f, 59.5f);
            _start = World(50.5f, 55.5f);
            DressTheSiege();
            BuildingInstance.OnDestroyedInCombat += OnBuildingDown;
            OrcUnit.Died += OnOrcDied;
            OrcUnit.Wounded += OnOrcWounded;
            RTSCameraController.GesturesLocked = true;
            SetUpView();
            NoShadowsOnLow();
            Darkness();
            if (_filmOnly)
            {
                _filmOnly = false;
                PlayPrologueAlone();
                return;
            }
            RaiseHealers();
            CountOrcs();

            gameObject.AddComponent<CityDefenceHud>().Bind(this);

            PlayerPrefs.DeleteKey(Part2Key);
            BeginPart1();
            // the scene at the capital when the courier gets there (CityDefenceMode.Capital.cs), not in the tests
            _noCapital = _skipCapitalOnce;
            _noHelp = _skipCapitalOnce;      // the tests' runs: no last scene either
            _skipCapitalOnce = false;
            // the opening scene at the north gate first (CityDefenceMode.Intro.cs): the siege's clock waits for it
            try { StartIntro(); }
            catch (System.Exception e)
            {
                // never let the scene cost the mission: straight to the siege
                Debug.LogException(e);
                AbortIntro();
            }
        }

        /// <summary>A plan point (x right, y down = south, in plan cells) in the world.</summary>
        public Vector3 World(float px, float py) => new Vector3(px + _origin.x, GridManager.Instance.GroundHeight, _origin.y - py);

        private Vector2Int CellOf(int px, int py) => GridManager.Instance.WorldToCell(World(px + 0.5f, py + 0.5f));

        private void RaiseTown() => RaisePieces(_plan.buildings, Sort);

        /// <summary>A plan's buildings onto the grid (mission 2's town, mission 3b's capital); `sort` sees each standing one.</summary>
        private void RaisePieces(List<Piece> pieces, System.Action<Piece, BuildingInstance> sort)
        {
            var catalog = new Dictionary<string, BuildingData>();
            foreach (var data in buildingPlacer.AvailableBuildings)
                if (data != null) catalog[data.buildingName] = data;
            if (buildingPlacer.MandatoryFirstBuilding != null) catalog[buildingPlacer.MandatoryFirstBuilding.buildingName] = buildingPlacer.MandatoryFirstBuilding;
            var grid = GridManager.Instance;
            var missing = new HashSet<string>();
            foreach (var piece in pieces)
            {
                if (!catalog.TryGetValue(piece.id, out var data) || data.prefab == null)
                {
                    missing.Add(piece.id);
                    continue;
                }
                var steps = ((piece.rot % 4) + 4) % 4;
                var footprint = steps % 2 == 0 ? data.footprintSize : new Vector2Int(data.footprintSize.y, data.footprintSize.x);
                // the plan's piece by its south-west cell (the plan's y runs south)
                var origin = CellOf(piece.x, piece.y + piece.h - 1);
                if (!grid.IsWithinBounds(origin, footprint)) continue;
                var go = Instantiate(data.prefab, grid.GetFootprintCenterWorld(origin, footprint), Quaternion.Euler(0f, steps * 90f, 0f));
                var building = go.GetComponent<BuildingInstance>();
                if (building == null) building = go.AddComponent<BuildingInstance>();
                building.Initialize(data, origin, steps);
                if (piece.level > 1) building.SetLevel(piece.level);
                grid.SetAreaOccupied(origin, footprint, true);
                if (piece.state == "ruin")
                {
                    Ruin(building);
                    continue;
                }
                if (data.isRoad && RoadNetwork.Instance != null)
                    for (var x = 0; x < footprint.x; x++)
                    for (var z = 0; z < footprint.y; z++)
                        RoadNetwork.Instance.RegisterRoad(origin + new Vector2Int(x, z));
                sort(piece, building);
            }
            if (missing.Count > 0) Debug.LogWarning("CityDefenceMode: no building for " + string.Join(", ", missing) + " -- the town stands without them.");
        }

        // the plan v2's frames (Tools/campaign/mission2_town.py): outer wall x 28..72, y 34..66; inner x 41..59, y 51..66
        private static bool OnOuterRing(Piece p) => p.x == 28 || p.x == 72 || p.y == 34 || (p.y == 66 && (p.x < 41 || p.x > 59));
        private static bool InsideInner(Piece p) => p.x >= 41 && p.x <= 59 && p.y >= 51;

        private void Sort(Piece piece, BuildingInstance building)
        {
            var data = building.Data;
            if (data.buildingName == BuildingIds.TownHall) Castle = building;
            if (data.isGate)
            {
                var gates = OnOuterRing(piece) ? OuterGates : InnerGates;
                // their own health and a bar low enough to be seen (the user, 2026-11-13)
                building.OverrideMaxHealth(OnOuterRing(piece) ? B.outerGateHealth : B.innerGateHealth);
                building.HealthBarHeight = B.gateBarHeight * (GridManager.Instance != null ? GridManager.Instance.CellSize : 1f);
                // its health is the bar under the goal (the user liked it, 2026-11-13), none over the gate
                building.HideHealthBar = true;
                for (var lane = 0; lane < gates.Length; lane++) gates[lane] = building;
                return;
            }
            if (data.connectsToFences)
            {
                if (OnOuterRing(piece)) _outerWall.Add(building);
                return;
            }
            var house = data.buildingName == "Cottage" || data.buildingName == "Hovel";
            if (!house) return;
            _houses.Add(building);
            // the lower town's houses: half of them in ruins by part 2
            if (!InsideInner(piece) && piece.y > 34 && piece.y < 66 && piece.x > 28 && piece.x < 72) _lowerTown.Add(building);
        }

        private void LayLanes()
        {
            for (var i = 0; i < 3 && i < _plan.lanes.Count; i++)
            {
                var p = _plan.lanes[i].points;
                if (p == null || p.Length < 4) continue;
                var corners = new Vector3[p.Length / 2];
                for (var k = 0; k < corners.Length; k++) corners[k] = World(p[2 * k], p[2 * k + 1]);
                _spawns[i] = corners[0];
                if (NavMesh.SamplePosition(_spawns[i], out var hit, 6f, NavMesh.AllAreas)) _spawns[i] = hit.position;
                _lanes[i] = new OrcLane(corners);
            }
        }

        // ------------------------------------------------------------------ the parts

        private void BeginPart1()
        {
            Current = Stage.Part1;
            TimeLeft = B.missionSeconds;
            CurrentGoal = Goal.OuterGate;
            Late = false;
            Wave = 0;
            _waveBeaten = false;
            _nextWave = B.firstWaveSeconds;
            Man(outer: true);
            RaisePatrols();
            // the inner gate's own guard from the start, kept behind it (the user, 2026-11-13); the patrols and
            // the rest keep the outer pen
            Man(outer: false);
            _pen = PenOf(true);
            StartTownsfolk();
            RaiseArden();
            HangDoors(OuterGates[0]);
            HangDoors(InnerGates[0]);
            MarkTowerDoors();
            StartSound();
            PartBegan?.Invoke(1);
        }

        private void Update()
        {
            TickMusic();
            if (!_active || Current == Stage.Setup || Current == Stage.Won || Current == Stage.Lost) return;
            if (Kind == Mission.Escort)
            {
                TickEscort(Time.deltaTime);
                return;
            }
            // the second scene (CityDefenceMode.Capital.cs): the game stands under it, it steps on unscaled time
            if (_capital != null)
            {
                try { TickCapital(Time.unscaledDeltaTime); }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                    AbortCapital();
                }
                return;
            }
            var dt = Time.deltaTime;
            if (_intro != null)
            {
                TickTownsfolk(dt);
                try { TickIntro(dt); }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                    AbortIntro();
                }
                return;
            }
            TickArden(dt);
            TickFx(dt);
            TickDoorSparks(dt);
            TickHealers(dt);
            TickGarrison(dt);
            TickTownsfolk(dt);
            TickSiege(dt);
            TickWallOrcs(dt);
            TickArdenWall(dt);
            TickArdenXp();
            TickStuns();
            TickGates(dt);
            TickWreckers(dt);
            TickTowerShots(dt);
            if (Current == Stage.Lost) return;

            TimeLeft -= dt;
            Late = TimeLeft <= B.missionSeconds * 0.5f;
            TickWaves(dt);
            WatchTheSquare(dt);
            if (Current == Stage.Lost) return;
            WatchForCapital();
            // the clock out: the last scene «Подкрепление» (CityDefenceMode.Help.cs), the win at its end
            if (TimeLeft <= 0f && _capital == null && !StartHelp()) Win();
        }

        // ------------------------------------------------------------------ the waves

        /// <summary>
        /// The waves (the user's first playtest): all three sides at once, each bigger than the last; the
        /// next comes a short pause after this one is beaten -- or after m2_wave_max_sec whatever -- and
        /// past half the part the waves are much harder (more orcs, a level up, brutes more often).
        /// </summary>
        private void TickWaves(float dt)
        {
            var alive = 0;
            foreach (var lane in _laneOrcs)
            {
                lane.RemoveAll(o => o == null || o.CurrentHealth <= 0);
                // rams and towers left alone do not hold the next wave back (the user's sixth playtest)
                foreach (var o in lane)
                    if (!o.IgnoresSoldiers) alive++;
            }
            LetThemLoose();
            // the next wave only once this one is beaten to the last orc (the user's third playtest), after its pause
            if (alive > 0) return;
            if (_waveBeaten)
            {
                _waveBeaten = false;
                var row = BalanceConfig.Instance.M2Wave(1, Wave);
                _nextWave = Mathf.Max(_nextWave, row != null ? row.pause : 0f);
            }
            _nextWave -= dt;
            if (_nextWave > 0f) return;
            SendWave();
            _nextWave = 0f;
            _waveBeaten = true;
        }

        private bool _waveBeaten;
        private float _looseCheck;

        /// <summary>
        /// The gate is down: every orc inside the walls leaves its road and razes the town, house by house, the
        /// inner line last as it comes nearest (the user's third playtest: «после уничтожения ворот орки должны идти
        /// разрушать весь город, после этого нападать на вторую линию»).
        /// </summary>
        private void LetThemLoose()
        {
            _looseCheck -= Time.deltaTime;
            if (_looseCheck > 0f) return;
            _looseCheck = 0.5f;
            if (OuterGates[0] != null || Current != Stage.Part1) return;
            // inside the outer walls (the inner gate standing) / inside the inner ones (it broken): on to the next goal
            var town = InnerGates[0] != null ? PenOf(true) : PenOf(false);
            foreach (var lane in _laneOrcs)
                foreach (var orc in lane)
                {
                    if (orc == null || orc.Lane == null || _toInner.Contains(orc)) continue;
                    // of the machines only a ram rolls on (a tower stays at its wall)
                    if (orc.IgnoresSoldiers && !IsRam(orc)) continue;
                    var at = orc.transform.position;
                    if (!town.Contains(new Vector2(at.x, at.z))) continue;
                    ToInnerGate(orc);
                }
        }

        private static bool IsRam(OrcUnit orc) => orc != null && orc.name == "OrcRam";

        /// <summary>A ram with no gate left to break: it stops for good where it stands.</summary>
        private static void Park(OrcUnit orc)
        {
            orc.FollowLane(null);
            orc.OnlyTarget = null;
            orc.BusyFor = 1e6f;
        }

        /// <summary>The men kept inside `pen` are kept no more.</summary>
        private void FreePen(Rect pen)
        {
            foreach (var s in SoldierUnit.All)
                if (s != null && s != Arden && s.Pen == pen) s.Pen = Rect.zero;
        }

        /// <summary>Every ram on the map stops: no gate stands any more.</summary>
        private void ParkRams()
        {
            foreach (var orc in OrcUnit.All)
                if (IsRam(orc) && orc.CurrentHealth > 0) Park(orc);
        }

        // the orcs already sent on to the inner gate
        private readonly HashSet<OrcUnit> _toInner = new HashSet<OrcUnit>();

        /// <summary>
        /// Inside the outer walls in part 1 (through the broken gate, a breach or off a tower): the INNER main gate is the
        /// next goal (the user's sixth playtest, 2026-11-13, replacing the third's «raze the whole town»): a road from
        /// where it stands to the gate's front; fighters on the way it still fights, houses it leaves alone.
        /// In part 2 (it is already at the last line) it is let loose.
        /// </summary>
        private void ToInnerGate(OrcUnit orc)
        {
            if (orc == null) return;
            _toInner.Add(orc);
            if (InnerGates[0] == null)
            {
                // no gate left: a ram stays where it is (the user's fourteenth playtest: it rolled on to the square)
                if (IsRam(orc))
                {
                    Park(orc);
                    return;
                }
                // the inner gate is broken: through it to the square (the user, 2026-11-13)
                orc.FollowLane(new OrcLane(new[] { orc.transform.position, OnMesh(World(50.5f, InnerNorthWallY - 1.5f)),
                    OnMesh(World(50.5f, InnerNorthWallY + 1.5f)), _square }));
                orc.LaneOnly = true;
                orc.OnlyTarget = null;
                return;
            }
            var front = OnMesh(World(50.5f + Random.Range(-1f, 1f), InnerNorthWallY - 1.3f));
            orc.FollowLane(new OrcLane(new[] { orc.transform.position, OnMesh(World(50.5f, 44f)), front }));
            orc.LaneOnly = true;
            // the inner gate only, never the wall beside it (the user's eighth playtest)
            orc.OnlyTarget = InnerGates[0];
        }

        private void SendWave()
        {
            var raids = OrcRaidManager.Instance;
            if (raids == null) return;
            Wave++;
            RoarForWave();
            // the first wave shuts the gates (the user's third playtest)
            if (Wave == 1)
            {
                ShutGate(OuterGates[0]);
                ShutGate(InnerGates[0]);
            }
            // the m2waves tab's row (the user's third playtest): each kind's count per road (the user, 2026-11-13)
            var row = BalanceConfig.Instance.M2Wave(1, Wave);
            if (row == null) return;
            var b = B;
            for (var road = 0; road < 3; road++)
            {
                Tower lead = null;   // stays null: Send() makes no escorts now
                // the tower rolls ALONE with its crew inside; the road's orcs run at the gate without waiting for it
                // (the user, 2026-11-14, replacing «the tower leads its road»): no escorts any more
                for (var i = 0; i < At(row.towers, road); i++) SpawnMachine(false, road, row.level);
                // no gate left standing: no more rams (the user's twelfth playtest)
                if (AnyGateStands())
                    for (var i = 0; i < At(row.rams, road); i++) SpawnMachine(true, road, row.level);
                Send(OrcKind.Brute, road, At(row.brutes, road), lead);
                Send(OrcKind.Raider, road, At(row.raiders, road), lead);
                Send(OrcKind.Thrower, road, At(row.throwers, road), lead);
            }

            static int At(int[] perRoad, int road) => perRoad != null && road < perRoad.Length ? perRoad[road] : 0;

            void Send(OrcKind kind, int lane, int count, Tower lead)
            {
                if (_lanes[lane] == null) return;
                for (var i = 0; i < count; i++)
                {
                    var at = _spawns[lane] + new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
                    var orc = raids.SpawnOrcOfKind(at, row.level, kind);
                    if (orc == null) continue;
                    orc.Toughen(b.orcHealthMult, b.orcDamageMult);
                    // an orc shooter reaches as far as the kingdom's archers, no farther (the user, 2026-11-13)
                    if (orc.IsRanged) orc.SetUnitRange(SoldierStats.Row(SoldierType.Archer).attackRangeUnits * b.rangedRangeMult);
                    orc.FollowLane(_lanes[lane]);
                    orc.LaneOnly = true;
                    // at the road's end only the gate is broken, not the wall beside it (the user's eighth playtest)
                    orc.OnlyTarget = OuterGates[lane];
                    _laneOrcs[lane].Add(orc);
                    if (lead != null) Escort(lead, orc);
                }
            }
        }

        /// <summary>The foremost orc of a lane's wave (nearest the castle) -- the minimap's «!».</summary>
        public bool TryLaneFront(int lane, out Vector3 front)
        {
            front = Vector3.zero;
            if (Kind == Mission.Escort) return TryEscortFront(lane, out front);
            if (lane < 0 || lane >= 3) return false;
            var best = float.MaxValue;
            var target = Castle != null ? Castle.transform.position : _square;
            foreach (var orc in _laneOrcs[lane])
            {
                if (orc == null || orc.CurrentHealth <= 0) continue;
                var d = (orc.transform.position - target).sqrMagnitude;
                if (d >= best) continue;
                best = d;
                front = orc.transform.position;
            }
            return best < float.MaxValue;
        }

        private void OnBuildingDown(BuildingInstance building)
        {
            if (Current == Stage.Won || Current == Stage.Lost || building == null) return;
            NoteBreach(building);
            if (building == Castle) { Lose(LossReason.Castle); return; }
            if (_houses.Contains(building)) _fallenHouses.Add(building);
            var outer = false;
            var inner = false;
            for (var i = 0; i < 3; i++)
            {
                if (OuterGates[i] == building) { OuterGates[i] = null; outer = true; }
                if (InnerGates[i] == building) { InnerGates[i] = null; inner = true; }
            }
            // the goal moves on (the user, 2026-11-13): outer gate -> inner gate -> the square
            if (outer && CurrentGoal == Goal.OuterGate)
            {
                CurrentGoal = InnerGates[0] != null ? Goal.InnerGate : Goal.Square;
                GoalChanged?.Invoke(CurrentGoal);
                StartWreckers();
                LayTheDead();
                if (InnerGates[0] != null) TowersToInnerWall();
            }
            // the gate they kept is down: its men may go out through it after the orcs near (the user's fifteenth playtest)
            if (outer) FreePen(PenOf(true));
            if (inner) FreePen(PenOf(false));
            if (!AnyGateStands()) ParkRams();
            if (inner && CurrentGoal != Goal.Square)
            {
                CurrentGoal = Goal.Square;
                GoalChanged?.Invoke(CurrentGoal);
                // the orcs bound for the inner gate make for the square now
                foreach (var orc in new List<OrcUnit>(_toInner))
                    if (orc != null && orc.CurrentHealth > 0 && !orc.Steered) ToInnerGate(orc);
                _toInner.RemoveWhere(o => o == null);
            }
        }

        private float _squareCheck;

        /// <summary>An orc of the waves on the central square: the mission is lost (the user, 2026-11-13).</summary>
        private void WatchTheSquare(float dt)
        {
            if (CurrentGoal != Goal.Square) return;
            _squareCheck -= dt;
            if (_squareCheck > 0f) return;
            _squareCheck = 0.25f;
            var reach = B.squareLossCells * (GridManager.Instance != null ? GridManager.Instance.CellSize : 1f);
            foreach (var lane in _laneOrcs)
                foreach (var orc in lane)
                {
                    if (orc == null || orc.CurrentHealth <= 0 || orc.Steered) continue;
                    if (Flat(orc.transform.position - _square) > reach) continue;
                    Lose(LossReason.Square);
                    return;
                }
        }

        /// <summary>Cheat (the user, 2026-11-12): the running part's clock runs out now.</summary>
        public void CheatEndPart()
        {
            if (!_active || Current != Stage.Part1) return;
            if (Kind == Mission.Escort) CheatEscortAhead();   // mission 3b: the cart a minute on
            else TimeLeft = 0.01f;
        }

        /// <summary>Cheat (the user, 2026-11-12): the level won on the spot.</summary>
        public void CheatWin()
        {
            if (!_active || Current == Stage.Won || Current == Stage.Setup) return;
            if (_capital != null) AbortCapital();      // a scene playing: closed first (the last one wins on its own)
            Win();
        }

        private void Win()
        {
            if (Current == Stage.Won) return;
            Current = Stage.Won;
            // the help rides in and cuts them all down (later: the scene)
            foreach (var orc in new List<OrcUnit>(OrcUnit.All))
                if (orc != null) orc.TakeDamage(orc.CurrentHealth * 10 + 1);
            PlayerPrefs.DeleteKey(Part2Key);
            CampaignProgress.MarkDone(Kind == Mission.Escort ? EscortLevelId : LevelId);
            Core.PiggyTasks.Report(Core.PiggyTasks.CampaignWin);
            StopMusic();
            Ended?.Invoke(true);
        }

        private void Lose(LossReason why)
        {
            if (Current == Stage.Won || Current == Stage.Lost) return;
            Current = Stage.Lost;
            Loss = why;
            if (Arden != null) Arden.Steer = Vector3.zero;
            StopMusic();
            Ended?.Invoke(false);
        }

        /// <summary>«Заново»: the mission again.</summary>
        public static void Restart()
        {
            Time.timeScale = 1f;
            if (Kind == Mission.Escort)
            {
                BeginEscort();
                return;
            }
            Begin();
            _skipIntroOnce = true;       // «Заново» goes straight to the siege
        }

        private static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;
    }
}
