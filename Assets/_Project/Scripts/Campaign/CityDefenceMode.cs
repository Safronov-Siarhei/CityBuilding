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
        public const string GameplayScene = "CityBuilder";
        /// <summary>PlayerPrefs: part 1 is won, a restart begins at part 2 (cleared when the mission is won).</summary>
        public const string Part2Key = "m2_part2_saved";

        public enum Stage { Setup, Part1, Courier, Part2, Won, Lost }

        public enum LossReason { None, Gate, Castle, Arden }

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

        public MissionHud.Art HudArt => hudArt;
        public Sprite SkillGlyph(int skill) => skill >= 0 && skill < skillGlyphs.Length ? skillGlyphs[skill] : null;
        public Sprite LockGlyph => lockGlyph;

        private bool _active;

        public Stage Current { get; private set; } = Stage.Setup;
        public LossReason Loss { get; private set; }

        /// <summary>The part's clock: seconds until the courier is away (part 1) / the help comes (part 2).</summary>
        public float TimeLeft { get; private set; }

        /// <summary>Past half the part: the waves much harder.</summary>
        public bool Late { get; private set; }

        public int Wave { get; private set; }

        /// <summary>How long the town has held in this part (the end window's numbers).</summary>
        public float HeldSeconds => Mathf.Max(0f, (Current == Stage.Part2 || (Current != Stage.Part1 && PlayerPrefs.GetInt(Part2Key, 0) == 1) ? B.part2Seconds : B.part1Seconds) - TimeLeft);

        /// <summary>The gates by lane (W, N, E). Since the user's third playtest the castle has ONE gate in each ring,
        /// so every lane's entry is that same gate (null once broken).</summary>
        public BuildingInstance[] OuterGates { get; } = new BuildingInstance[3];
        public BuildingInstance[] InnerGates { get; } = new BuildingInstance[3];

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
            GameSessionIntent.SaveNameToLoad = null;
            GameSessionIntent.NewGameMapId = MapId;
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
            // nobody fights across a wall (the user's fourth playtest)
            OrcUnit.WallBetween = WallBetween;
            SoldierUnit.PenBreach = Breach;
            // no residents grow in the houses (their bars stood over every roof, the user's first playtest)
            var people = GetComponent<Citizens.HousePeople>();
            if (people != null) people.enabled = false;
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
            }
        }

        private IEnumerator Start()
        {
            if (!_active) yield break;
            for (var frame = 0; frame < 600; frame++)
            {
                var applier = MeshMapApplier.Instance;
                if (applier != null && applier.CurrentMapId == MapId && GridManager.Instance != null) break;
                yield return null;
            }
            yield return new WaitForFixedUpdate();
            Physics.SyncTransforms();
            Setup();
        }

        // ------------------------------------------------------------------ the town

        private void Setup()
        {
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
            Darkness();
            RaiseHealers();
            CountOrcs();

            gameObject.AddComponent<CityDefenceHud>().Bind(this);

            if (PlayerPrefs.GetInt(Part2Key, 0) == 1) BeginPart2(atOnce: true);
            else BeginPart1();
        }

        /// <summary>A plan point (x right, y down = south, in plan cells) in the world.</summary>
        public Vector3 World(float px, float py) => new Vector3(px + _origin.x, GridManager.Instance.GroundHeight, _origin.y - py);

        private Vector2Int CellOf(int px, int py) => GridManager.Instance.WorldToCell(World(px + 0.5f, py + 0.5f));

        private void RaiseTown()
        {
            var catalog = new Dictionary<string, BuildingData>();
            foreach (var data in buildingPlacer.AvailableBuildings)
                if (data != null) catalog[data.buildingName] = data;
            if (buildingPlacer.MandatoryFirstBuilding != null) catalog[buildingPlacer.MandatoryFirstBuilding.buildingName] = buildingPlacer.MandatoryFirstBuilding;
            var grid = GridManager.Instance;
            var missing = new HashSet<string>();
            foreach (var piece in _plan.buildings)
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
                Sort(piece, building);
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
            TimeLeft = B.part1Seconds;
            Late = false;
            Wave = 0;
            _waveBeaten = false;
            _nextWave = B.firstWaveSeconds;
            Man(outer: true);
            RaisePatrols();
            StartTownsfolk();
            RaiseArden();
            HangDoors(OuterGates[0]);
            HangDoors(InnerGates[0]);
            PartBegan?.Invoke(1);
        }

        /// <summary>Part 1 won: the courier is away (later: the cutscene at the King's); part 2 from the ruins.</summary>
        private IEnumerator Courier()
        {
            Current = Stage.Courier;
            PlayerPrefs.SetInt(Part2Key, 1);
            PlayerPrefs.Save();
            yield return new WaitForSeconds(3f);
            BeginPart2(atOnce: false);
        }

        private void BeginPart2(bool atOnce)
        {
            CreditPart1();
            // the waves of part 1 are gone with the courier's ride; the outer works and half the lower town lie in ruins
            foreach (var orc in new List<OrcUnit>(OrcUnit.All))
                if (orc != null) Destroy(orc.gameObject);
            foreach (var lane in _laneOrcs) lane.Clear();
            ForgetSiege();
            for (var i = 0; i < 3; i++)
            {
                if (OuterGates[i] != null) OuterGates[i].TryDamage(OuterGates[i].CurrentHealth);
                OuterGates[i] = null;
            }
            // the wall pieces either side of each gate: breaches the orcs pour through
            foreach (var piece in _outerWall)
            {
                if (piece == null) continue;
                var near = false;
                if (Flat(piece.transform.position - World(50.5f, 34.5f)) < 4.5f) near = true;
                if (near || Random.value < 0.25f) piece.TryDamage(piece.CurrentHealth);
            }
            var burnt = Mathf.RoundToInt(_lowerTown.Count * Mathf.Clamp01(B.part2Burnt));
            var order = new List<BuildingInstance>(_lowerTown);
            for (var i = 0; i < order.Count; i++)
            {
                var j = Random.Range(i, order.Count);
                (order[i], order[j]) = (order[j], order[i]);
            }
            for (var i = 0; i < burnt && i < order.Count; i++)
                if (order[i] != null) order[i].TryDamage(order[i].CurrentHealth);

            ClearGarrison();
            Man(outer: false);
            if (Arden == null) RaiseArden();
            else JumpToArden();

            Current = Stage.Part2;
            TimeLeft = B.part2Seconds;
            Late = false;
            Wave = 0;
            _waveBeaten = false;
            _nextWave = atOnce ? B.firstWaveSeconds : B.firstWaveSeconds * 0.5f;
            HangDoors(InnerGates[0]);
            PartBegan?.Invoke(2);
        }

        private void Update()
        {
            if (!_active || Current == Stage.Setup || Current == Stage.Won || Current == Stage.Lost) return;
            var dt = Time.deltaTime;
            TickArden(dt);
            TickFx(dt);
            TickHealers(dt);
            TickGarrison(dt);
            TickTownsfolk(dt);
            TickSiege(dt);
            TickGates(dt);
            if (Current == Stage.Courier || Current == Stage.Lost) return;

            TimeLeft -= dt;
            var length = Current == Stage.Part2 ? B.part2Seconds : B.part1Seconds;
            Late = TimeLeft <= length * 0.5f;
            TickWaves(dt);
            if (TimeLeft > 0f) return;
            if (Current == Stage.Part1) StartCoroutine(Courier());
            else Win();
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
                var row = BalanceConfig.Instance.M2Wave(Current == Stage.Part2 ? 2 : 1, Wave);
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
            var town = PenOf(true);
            foreach (var lane in _laneOrcs)
                foreach (var orc in lane)
                {
                    if (orc == null || orc.Lane == null || orc.IgnoresSoldiers || _toInner.Contains(orc)) continue;
                    var at = orc.transform.position;
                    if (!town.Contains(new Vector2(at.x, at.z))) continue;
                    ToInnerGate(orc);
                }
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
            if (Current != Stage.Part1 || InnerGates[0] == null)
            {
                orc.LaneOnly = false;
                orc.FollowLane(null);
                return;
            }
            var front = OnMesh(World(50.5f + Random.Range(-1f, 1f), InnerNorthWallY - 1.3f));
            orc.FollowLane(new OrcLane(new[] { orc.transform.position, OnMesh(World(50.5f, 44f)), front }));
            orc.LaneOnly = true;
        }

        private void SendWave()
        {
            var raids = OrcRaidManager.Instance;
            if (raids == null) return;
            Wave++;
            // the part's first wave shuts its gate (the user's third playtest)
            if (Wave == 1) ShutGate(Current == Stage.Part2 ? InnerGates[0] : OuterGates[0]);
            // the m2waves tab's row (the user's third playtest): each kind's count per road (the user, 2026-11-13)
            var row = BalanceConfig.Instance.M2Wave(Current == Stage.Part2 ? 2 : 1, Wave);
            if (row == null) return;
            var b = B;
            for (var road = 0; road < 3; road++)
            {
                // a siege tower leads its road and the road's orcs walk behind it (the user, 2026-11-13)
                Tower lead = null;
                for (var i = 0; i < At(row.towers, road); i++) lead = SpawnMachine(false, road, row.level) ?? lead;
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
                    _laneOrcs[lane].Add(orc);
                    if (lead != null) Escort(lead, orc);
                }
            }
        }

        /// <summary>The foremost orc of a lane's wave (nearest the castle) -- the minimap's «!».</summary>
        public bool TryLaneFront(int lane, out Vector3 front)
        {
            front = Vector3.zero;
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
            for (var i = 0; i < 3; i++)
            {
                if (InnerGates[i] == building) { Lose(LossReason.Gate); return; }
                if (OuterGates[i] == building) OuterGates[i] = null;
            }
        }

        /// <summary>Cheat (the user, 2026-11-12): the running part's clock runs out now.</summary>
        public void CheatEndPart()
        {
            if (_active && (Current == Stage.Part1 || Current == Stage.Part2)) TimeLeft = 0.01f;
        }

        /// <summary>Cheat (the user, 2026-11-12): the level won on the spot.</summary>
        public void CheatWin()
        {
            if (_active && Current != Stage.Won && Current != Stage.Setup) Win();
        }

        private void Win()
        {
            Current = Stage.Won;
            // the help rides in and cuts them all down (later: the scene)
            foreach (var orc in new List<OrcUnit>(OrcUnit.All))
                if (orc != null) orc.TakeDamage(orc.CurrentHealth * 10 + 1);
            PlayerPrefs.DeleteKey(Part2Key);
            CampaignProgress.MarkDone(LevelId);
            Ended?.Invoke(true);
        }

        private void Lose(LossReason why)
        {
            if (Current == Stage.Won || Current == Stage.Lost) return;
            Current = Stage.Lost;
            Loss = why;
            if (Arden != null) Arden.Steer = Vector3.zero;
            Ended?.Invoke(false);
        }

        /// <summary>«Заново»: the mission again (from part 2 once part 1 is won).</summary>
        public static void Restart()
        {
            Time.timeScale = 1f;
            Begin();
        }

        private static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;
    }
}
