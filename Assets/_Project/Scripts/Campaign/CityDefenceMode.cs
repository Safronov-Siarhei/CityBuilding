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
    /// The player runs Ардан alone with the stick (sword strikes by themselves, one button: the battle
    /// cry); the guards fight by themselves -- men-at-arms at the gates, archers on the walls. Orc waves
    /// come down three lanes (W, N, E) over the river's fords to the outer gates, through the lower town
    /// to the inner gates. Part 1: hold until the courier is away (the outer works WILL fall -- the point
    /// is that the inner gates do not). Part 2 starts inside the inner walls with the outer works and half
    /// the lower town in ruins: hold until the help arrives. ANY inner gate broken = the mission lost
    /// (restart; after part 1 a restart begins at part 2). Numbers: m2_* (BalanceConfig.CityDefence).
    /// The cutscenes (Ардан at the gate, the courier at the King's, the help) come last, at the user's word.
    /// </summary>
    public class CityDefenceMode : MonoBehaviour
    {
        public const string MapId = "Mission2";
        public const string LevelId = "m2";
        public const string GameplayScene = "CityBuilder";
        /// <summary>PlayerPrefs: part 1 is won, a restart begins at part 2 (cleared when the mission is won).</summary>
        public const string Part2Key = "m2_part2_saved";

        public enum Stage { Setup, Part1, Courier, Part2, Won, Lost }

        /// <summary>Set by the campaign map before the gameplay scene loads (Begin).</summary>
        public static bool Pending;

        public static CityDefenceMode Instance { get; private set; }

        /// <summary>This game is mission 2.</summary>
        public static bool IsActive => Instance != null && Instance._active;

        [SerializeField] private BuildingPlacer buildingPlacer;
        [SerializeField] private MonoBehaviour[] notInAMission = new MonoBehaviour[0];
        [SerializeField] private TextAsset town;
        // the HUD's pictures (SetupProjectCampaign wires them, as mission 1's)
        [SerializeField] private MissionHud.Art hudArt;
        [SerializeField] private Sprite cryGlyph;
        [SerializeField] private Sprite gateIcon;

        public MissionHud.Art HudArt => hudArt;
        public Sprite CryGlyph => cryGlyph;
        public Sprite GateIcon => gateIcon;

        private bool _active;

        public Stage Current { get; private set; } = Stage.Setup;

        /// <summary>The part's clock: seconds until the courier is away (part 1) / the help comes (part 2).</summary>
        public float TimeLeft { get; private set; }

        public SoldierUnit Arden { get; private set; }
        public float ArdenRiseIn { get; private set; }
        public float CryCooldown { get; private set; }

        /// <summary>The gates in lane order W, N, E (null once broken).</summary>
        public BuildingInstance[] OuterGates { get; } = new BuildingInstance[3];
        public BuildingInstance[] InnerGates { get; } = new BuildingInstance[3];

        public BuildingInstance Castle { get; private set; }

        /// <summary>The mission ended: true won, false lost (the HUD's end panel).</summary>
        public event System.Action<bool> Ended;

        /// <summary>A part began (1 or 2): the HUD's banner.</summary>
        public event System.Action<int> PartBegan;

        /// <summary>The battle cry was given (the HUD's ring and the sound).</summary>
        public event System.Action Cried;

        private static CityDefenceBalance B => BalanceConfig.Instance.CityDefence;

        [System.Serializable] private class Blueprint
        {
            public int width, height;
            public float[] origin;
            public List<Lane> lanes = new List<Lane>();
            public List<Piece> buildings = new List<Piece>();
        }

        [System.Serializable] private class Lane { public float[] points; }

        [System.Serializable] private class Piece { public string id; public int x, y, w, h, rot, level; }

        private Blueprint _plan;
        private Vector2 _origin;
        private readonly OrcLane[] _lanes = new OrcLane[3];
        private readonly Vector3[] _spawns = new Vector3[3];
        private readonly List<BuildingInstance> _lowerTown = new List<BuildingInstance>();
        private readonly List<BuildingInstance> _outerWall = new List<BuildingInstance>();
        private readonly List<SoldierUnit> _guards = new List<SoldierUnit>();
        private ArmyGroup _ardenGroup;
        private int _wave;
        private float _nextWave;
        private Vector3 _square;

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
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            BuildingInstance.OnDestroyedInCombat -= OnBuildingDown;
            if (_active) RTSCameraController.GesturesLocked = false;
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
            BuildingInstance.OnDestroyedInCombat += OnBuildingDown;
            RTSCameraController.GesturesLocked = true;

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
                var lane = piece.y == 34 || piece.y == 51 ? 1 : piece.x < 50 ? 0 : 2;
                if (OnOuterRing(piece)) OuterGates[lane] = building;
                else InnerGates[lane] = building;
                return;
            }
            if (data.connectsToFences)
            {
                if (OnOuterRing(piece)) _outerWall.Add(building);
                return;
            }
            // the lower town's houses (the trees and wells stay): half of them in ruins by part 2
            var house = data.buildingName == "Cottage" || data.buildingName == "Hovel";
            if (house && !InsideInner(piece) && piece.y > 34 && piece.y < 66 && piece.x > 28 && piece.x < 72) _lowerTown.Add(building);
        }

        private void LayLanes()
        {
            for (var i = 0; i < 3 && i < _plan.lanes.Count; i++)
            {
                var p = _plan.lanes[i].points;
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
            _wave = 0;
            _nextWave = B.firstWaveSeconds;
            Man(outer: true);
            RaiseArden(jump: true);
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
            // the waves of part 1 are gone with the courier's ride; the outer works and half the lower town lie in ruins
            foreach (var orc in new List<OrcUnit>(OrcUnit.All))
                if (orc != null) Destroy(orc.gameObject);
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
                foreach (var spot in new[] { World(28.5f, 40.5f), World(50.5f, 34.5f), World(72.5f, 40.5f) })
                    if (Flat(piece.transform.position - spot) < 4.5f) near = true;
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

            foreach (var guard in _guards)
                if (guard != null) Destroy(guard.gameObject);
            _guards.Clear();
            Man(outer: false);
            RaiseArden(jump: true);

            Current = Stage.Part2;
            TimeLeft = B.part2Seconds;
            _wave = 0;
            _nextWave = atOnce ? B.firstWaveSeconds : B.firstWaveSeconds * 0.5f;
            PartBegan?.Invoke(2);
        }

        private void Update()
        {
            if (!_active || Current == Stage.Setup || Current == Stage.Won || Current == Stage.Lost) return;
            var dt = Time.deltaTime;
            TickArden(dt);
            if (Current == Stage.Courier) return;

            TimeLeft -= dt;
            _nextWave -= dt;
            if (_nextWave <= 0f)
            {
                SendWave();
                _nextWave = B.waveEverySeconds;
            }
            if (TimeLeft > 0f) return;
            if (Current == Stage.Part1) StartCoroutine(Courier());
            else Win();
        }

        private void SendWave()
        {
            var raids = OrcRaidManager.Instance;
            if (raids == null) return;
            _wave++;
            var b = B;
            var count = Mathf.Min(b.waveOrcsMax, b.waveOrcs + (b.waveOrcsGrowEvery > 0 ? (_wave - 1) / b.waveOrcsGrowEvery : 0));
            var level = Current == Stage.Part2 ? b.orcLevelPart2 : b.orcLevel;
            for (var lane = 0; lane < 3; lane++)
            {
                if (_lanes[lane] == null) continue;
                for (var i = 0; i < count; i++)
                {
                    var kind = OrcKind.Raider;
                    if (i == 0 && b.bruteEvery > 0 && _wave % b.bruteEvery == 0) kind = OrcKind.Brute;
                    else if (i == 1 && b.throwerEvery > 0 && _wave % b.throwerEvery == 0) kind = OrcKind.Thrower;
                    var at = _spawns[lane] + new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
                    var orc = raids.SpawnOrcOfKind(at, level, kind);
                    if (orc == null) continue;
                    orc.Toughen(b.orcHealthMult, b.orcDamageMult);
                    orc.FollowLane(_lanes[lane]);
                }
            }
        }

        private void OnBuildingDown(BuildingInstance building)
        {
            if (Current == Stage.Won || Current == Stage.Lost || building == null) return;
            if (building == Castle) { Lose(); return; }
            for (var i = 0; i < 3; i++)
            {
                if (InnerGates[i] == building) { Lose(); return; }
                if (OuterGates[i] == building) OuterGates[i] = null;
            }
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

        private void Lose()
        {
            Current = Stage.Lost;
            Ended?.Invoke(false);
        }

        // ------------------------------------------------------------------ the guards

        private void Man(bool outer)
        {
            var army = ArmyManager.Instance;
            if (army == null) return;
            var b = B;
            var stats = SoldierStats.StatsAt(SoldierType.ManAtArms, b.guardLevel);
            var archer = SoldierStats.StatsAt(SoldierType.Archer, b.guardLevel);
            // each gate: men-at-arms just inside it; archers on the wall of its side
            var gates = outer
                ? new[] { (World(28.5f, 40.5f), new Vector3(1f, 0f, 0f)), (World(50.5f, 34.5f), new Vector3(0f, 0f, -1f)), (World(72.5f, 40.5f), new Vector3(-1f, 0f, 0f)) }
                : new[] { (World(41.5f, 56.5f), new Vector3(1f, 0f, 0f)), (World(50.5f, 51.5f), new Vector3(0f, 0f, -1f)), (World(59.5f, 56.5f), new Vector3(-1f, 0f, 0f)) };
            var guards = outer ? b.gateGuards : b.innerGuards;
            var archers = outer ? b.wallArchers : b.innerArchers;
            for (var lane = 0; lane < 3; lane++)
            {
                var (gate, inward) = gates[lane];
                var post = gate + inward * 2.5f;
                var group = new ArmyGroup(SoldierType.ManAtArms, post);
                for (var k = 0; k < guards; k++)
                {
                    var unit = army.SpawnHireling(SoldierType.ManAtArms, post, group);
                    if (unit == null) continue;
                    unit.SetHeroStats(stats.maxHealth, stats.attackDamage);
                    unit.SetCurrentHealth(stats.maxHealth);
                    _guards.Add(unit);
                }
                group.OrderMoveTo(post);
                var cells = WallCellsBeside(lane, outer, archers);
                foreach (var cell in cells)
                {
                    var g = new ArmyGroup(SoldierType.Archer, post);
                    var unit = army.SpawnHireling(SoldierType.Archer, post, g);
                    if (unit == null) continue;
                    unit.SetHeroStats(archer.maxHealth, archer.attackDamage);
                    unit.SetCurrentHealth(archer.maxHealth);
                    unit.GoToWall(cell);
                    _guards.Add(unit);
                }
            }
        }

        /// <summary>`count` wall cells on the gate's side, spread either side of it (not the gate's own three).</summary>
        private List<Vector2Int> WallCellsBeside(int lane, bool outer, int count)
        {
            var found = new List<Vector2Int>();
            if (count <= 0) return found;
            var candidates = new List<Vector2Int>();
            // (fixed coordinate, along-from, along-to, vertical side?)
            int fixedC, from, to, gateAt;
            bool vertical;
            if (outer)
            {
                if (lane == 0) { fixedC = 28; from = 35; to = 65; gateAt = 40; vertical = true; }
                else if (lane == 1) { fixedC = 34; from = 29; to = 71; gateAt = 50; vertical = false; }
                else { fixedC = 72; from = 35; to = 65; gateAt = 40; vertical = true; }
            }
            else
            {
                if (lane == 0) { fixedC = 41; from = 52; to = 65; gateAt = 56; vertical = true; }
                else if (lane == 1) { fixedC = 51; from = 42; to = 58; gateAt = 50; vertical = false; }
                else { fixedC = 59; from = 52; to = 65; gateAt = 56; vertical = true; }
            }
            // nearest the gate first, alternating sides
            for (var d = 2; d <= Mathf.Max(gateAt - from, to - gateAt); d++)
            {
                foreach (var a in new[] { gateAt - d, gateAt + d })
                {
                    if (a < from || a > to) continue;
                    candidates.Add(vertical ? CellOf(fixedC, a) : CellOf(a, fixedC));
                }
            }
            for (var i = 0; i < candidates.Count && found.Count < count; i += 2) found.Add(candidates[i]);
            return found;
        }

        // ------------------------------------------------------------------ Ардан

        private void RaiseArden(bool jump)
        {
            var army = ArmyManager.Instance;
            if (army == null) return;
            if (Arden != null) Destroy(Arden.gameObject);
            var at = _square;
            if (NavMesh.SamplePosition(at, out var hit, 4f, NavMesh.AllAreas)) at = hit.position;
            _ardenGroup = new ArmyGroup(SoldierType.ManAtArms, at);
            Arden = army.SpawnHireling(SoldierType.ManAtArms, at, _ardenGroup);
            if (Arden == null) return;
            Arden.name = "Arden";
            Arden.SetHeroStats(B.ardenHealth, B.ardenDamage);
            Arden.SetCurrentHealth(B.ardenHealth);
            ArdenRiseIn = 0f;
            if (jump) RTSCameraController.Instance?.JumpTo(at);
        }

        /// <summary>The stick's reading (screen up = away from the camera = north), from the HUD.</summary>
        public Vector2 Stick { get; set; }

        private void TickArden(float dt)
        {
            if (CryCooldown > 0f) CryCooldown -= dt;
            if (Arden == null || Arden.CurrentHealth <= 0)
            {
                if (ArdenRiseIn <= 0f) ArdenRiseIn = B.ardenRiseSeconds;
                ArdenRiseIn -= dt;
                if (ArdenRiseIn <= 0f) RaiseArden(jump: true);
                return;
            }
            // the camera looks north: the stick's up is +z
            var steer = new Vector3(Stick.x, 0f, Stick.y);
            var was = Arden.IsSteered;
            Arden.Steer = steer;
            if (was && !Arden.IsSteered) _ardenGroup.OrderMoveTo(Arden.transform.position);   // let go: he holds there and fights
            RTSCameraController.Instance?.FocusOn(Arden.transform.position);
        }

        /// <summary>The battle cry: the guards and Ардан round him strike harder for a while, the orcs at hand stagger.</summary>
        public bool Cry()
        {
            if (Arden == null || Arden.CurrentHealth <= 0 || CryCooldown > 0f || Current == Stage.Won || Current == Stage.Lost) return false;
            var b = B;
            var at = Arden.transform.position;
            Arden.Enrage(b.cryDamageMult, b.crySeconds);
            foreach (var guard in _guards)
                if (guard != null && guard.CurrentHealth > 0 && Flat(guard.transform.position - at) <= b.cryRadius) guard.Enrage(b.cryDamageMult, b.crySeconds);
            foreach (var orc in OrcUnit.All)
                if (orc != null && orc.CurrentHealth > 0 && Flat(orc.transform.position - at) <= b.cryRadius) orc.BusyFor = Mathf.Max(orc.BusyFor, b.cryStunSeconds);
            CryCooldown = b.cryCooldownSeconds;
            Cried?.Invoke();
            return true;
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
