using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Resources;
using CityBuilder.UI;
using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>
    /// The sandbox's predators (the user, 2026-11-22; card Design/features/predators.md). Numbers: pred_* and
    /// mon_pred_wolf_* / mon_pred_bear_* on «Еда и голод».
    ///
    /// At the start `pred_caves` islands get a small animal CAVE at their edge: the player's main island and random
    /// others (never the orcs' or the bot's). Eternal, under the fog like everything else; the sign predators live
    /// there. Not before `pred_first_attack_sec`, then every `pred_attack_min_sec`..`pred_attack_max_sec`, one of
    /// those islands is attacked: a pack of wolves (one per `pred_wolf_per_residents` of the island's residents) or,
    /// one time in three (`pred_bear_chance_pct`), bears (none up to `pred_bear_from_residents`, then one more per
    /// `pred_bear_per_residents`). They come out at a random spot not within `pred_spawn_gap_cells` of a building.
    ///
    /// They are OrcUnits of their own kinds -- soldiers, heroes, the King and towers fight them as they do any enemy,
    /// hp bars and all -- steered by a <see cref="PredatorErrand"/>: a wolf's bite wounds a villager (a patient of
    /// the Лечебница, Infirmary.Wound), a bear's kills him; both go for the island's food stores, whose food goes with
    /// their health (StoreLedger.Eat). The pack goes back to its cave once it has wounded `pred_wolf_success_pct` of
    /// the residents (wolves) or taken a store `pred_store_success_share` of its health, or after `pred_retreat_sec`;
    /// in the cave it is gone. The island's hunters shoot them (Hunting). A kill pays food; a hunter's kill hides too.
    ///
    /// Saved (GameSaveController): the caves and the clock. The beasts themselves are not -- a load starts without them.
    /// </summary>
    public class Predators : MonoBehaviour
    {
        [SerializeField] private GameObject caveModel;

        public static Predators Instance { get; private set; }

        /// <summary>The sandbox's rules are on (not the Invasion, the tutorial or the campaign town).</summary>
        public static bool Active => Instance != null && Infirmary.Active;

        /// <summary>The notices' picture (UI/Icons): the angry bear (the user, 2026-11-22).</summary>
        public const string NoticeIcon = "PredatorBear";

        // feel, not balance: how often a failed attack (no island with people) tries again, the spread of a pack
        private const float RetrySeconds = 60f, PackSpread = 1.2f, CaveGapCells = 4f;

        public sealed class Cave
        {
            public int Island;
            public Vector3 Position;
            public float Yaw;
            public Transform Root;
        }

        internal sealed class Raid
        {
            public int Island;
            public Cave Cave;
            public bool Bears;
            public int Residents;
            public int Wounded;
            public float Time;
            public bool Going;
            public bool ToldStore;
            public readonly Dictionary<BuildingInstance, int> StoreDamage = new Dictionary<BuildingInstance, int>();
            public readonly List<OrcUnit> Beasts = new List<OrcUnit>();
        }

        private readonly List<Cave> _caves = new List<Cave>();
        private readonly List<Raid> _raids = new List<Raid>();
        private float _clock, _next = -1f;
        private bool _cavesMade;
        private float _caveRetry;
        private const float CaveRetrySeconds = 5f;
        private OrcUnit _hunterBlow;

        public IReadOnlyList<Cave> Caves => _caves;

        /// <summary>Seconds of sandbox play this game (saved).</summary>
        public float Clock => _clock;

        private static BalanceConfig Config => BalanceConfig.Instance;
        private static float Cell => GridManager.Instance != null ? GridManager.Instance.CellSize : 1f;

        private void Awake() => Instance = this;

        private void OnEnable() => OrcUnit.Died += OnBeastDied;

        private void OnDisable() => OrcUnit.Died -= OnBeastDied;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---------------------------------------------------------------------------------------- the clock

        private void Update()
        {
            var dt = Time.deltaTime;
            if (dt <= 0f || !Active || Config == null || GridManager.Instance == null || MeshMapApplier.Instance == null
                || Hunting.Instance == null) return;
            _clock += dt;
            if (!_cavesMade && (_caveRetry -= dt) <= 0f)
            {
                _caveRetry = CaveRetrySeconds;
                _cavesMade = MakeCaves();
            }
            if (_next < 0f) _next = Config.PredFirstAttackSeconds;
            if (_cavesMade && _clock >= _next)
                _next = _clock + (Attack() ? Random.Range(Config.PredAttackMinSeconds, Mathf.Max(Config.PredAttackMinSeconds, Config.PredAttackMaxSeconds)) : RetrySeconds);

            for (var i = _raids.Count - 1; i >= 0; i--)
            {
                var raid = _raids[i];
                raid.Beasts.RemoveAll(b => b == null || b.CurrentHealth <= 0);
                if (raid.Beasts.Count == 0) { _raids.RemoveAt(i); continue; }
                raid.Time += dt;
                if (!raid.Going && raid.Time >= Config.PredRetreatSeconds) raid.Going = true;
            }
        }

        // ---------------------------------------------------------------------------------------- the caves

        /// <summary>Picks the cave islands and sets a cave on each: false while the map or the Town Hall is not there yet.</summary>
        private bool MakeCaves()
        {
            var home = Hunting.PlayerIsland();
            if (home == int.MinValue) return false;
            var map = MeshMapApplier.Instance;
            int orc = Hunting.IslandOfCell(map.ArchipelagoOrcCell), bot = Hunting.IslandOfCell(map.ArchipelagoBotCell);
            var others = new List<int>();
            foreach (var island in Hunting.Instance.LandIslands())
                if (island != home && island != orc && island != bot) others.Add(island);
            var picked = new List<int> { home };
            while (picked.Count < Mathf.Max(1, Config.PredCaves) && others.Count > 0)
            {
                var k = Random.Range(0, others.Count);
                picked.Add(others[k]);
                others.RemoveAt(k);
            }
            foreach (var island in picked)
            {
                if (!FindCaveSpot(island, out var at, out var yaw)) continue;
                _caves.Add(new Cave { Island = island, Position = at, Yaw = yaw });
            }
            foreach (var cave in _caves) Raise(cave);
            // none found (the NavMesh not baked yet): again in a moment
            return _caves.Count > 0;
        }

        /// <summary>
        /// A spot near the island's edge for its cave (the user, 2026-11-22: not on the beach, not behind the mountains):
        /// flat free GRASS (no sand under it or before its mouth), the sea 3..`CaveEdgeCells` cells away, away from
        /// buildings, the mouth turned inland onto land -- and from before the mouth the beasts can walk (NavMesh) to most
        /// of the island and to the Town Hall on the player's island. No such spot on a map without the archipelago's
        /// grass: the same without the grass test.
        /// </summary>
        private bool FindCaveSpot(int island, out Vector3 at, out float yaw)
        {
            at = Vector3.zero;
            yaw = 0f;
            var cells = Hunting.Instance.CellsOf(island);
            if (cells == null || cells.Count == 0) return false;
            var grid = GridManager.Instance;
            var centre = Vector3.zero;
            foreach (var c in cells) centre += grid.GetFootprintCenterWorld(c, Vector2Int.one);
            centre /= cells.Count;
            var anchors = Anchors(island, cells, out var hall);
            return Search(cells, centre, anchors, hall, true, out at, out yaw) || Search(cells, centre, anchors, hall, false, out at, out yaw);
        }

        private const int CaveEdgeCells = 5, CaveTries = 300, AnchorCount = 6;

        private bool Search(List<Vector2Int> cells, Vector3 centre, List<Vector3> anchors, bool hall, bool grass, out Vector3 at, out float yaw)
        {
            at = Vector3.zero;
            yaw = 0f;
            var grid = GridManager.Instance;
            var map = MeshMapApplier.Instance;
            for (var t = 0; t < CaveTries; t++)
            {
                var cell = cells[Random.Range(0, cells.Count)];
                var sea = WaterWithin(map, cell, CaveEdgeCells);
                if (sea < 3) continue;   // none in reach: not the edge; nearer than 3: the beach
                var p = grid.GetFootprintCenterWorld(cell, Vector2Int.one);
                if (!Hunting.OnLand(p, out var y) || !Hunting.FarFromBuildings(p, CaveGapCells * Cell)) continue;
                var inland = (centre - p).WithY(0f);
                if (inland.sqrMagnitude < 0.01f) continue;
                var front = p + inland.normalized * Cell;
                // the mouth must open onto land, not the water it backs onto
                if (!Hunting.OnLand(front, out var frontY)) continue;
                if (grass && (!OnGrass(p) || !OnGrass(front))) continue;
                if (!Reaches(new Vector3(front.x, frontY, front.z), anchors, hall)) continue;
                at = new Vector3(p.x, y, p.z);
                yaw = Quaternion.LookRotation(inland).eulerAngles.y;
                return true;
            }
            return false;
        }

        /// <summary>How many cells away the nearest water is (a ring search up to `max`); max + 1 when none.</summary>
        private static int WaterWithin(MeshMapApplier map, Vector2Int cell, int max)
        {
            for (var r = 1; r <= max; r++)
            for (var dz = -r; dz <= r; dz++)
            for (var dx = -r; dx <= r; dx++)
            {
                if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != r) continue;
                if (map.IsWaterCell(cell + new Vector2Int(dx, dz))) return r;
            }
            return max + 1;
        }

        /// <summary>The archipelago's grass under this point (not sand, rock, a cliff).</summary>
        private static bool OnGrass(Vector3 p)
        {
            var map = MeshMapApplier.Instance;
            var ray = new Ray(new Vector3(p.x, p.y + 50f, p.z), Vector3.down);
            return map.TryRaycastGround(ray, out var hit) && hit.collider is MeshCollider
                   && Archipelago.ArchipelagoMeshes.IsGrassUv(hit.textureCoord);
        }

        /// <summary>Points of the island the beasts must be able to walk to from the cave: the Town Hall's door on the player's island, a few open spots of its land.</summary>
        private static List<Vector3> Anchors(int island, List<Vector2Int> cells, out bool hallFirst)
        {
            var anchors = new List<Vector3>();
            hallFirst = false;
            var hall = BuildingInstance.TownHall;
            if (hall != null && !hall.IsBot && HousePeople.IslandOf(hall) == island)
            {
                var visuals = CitizenVisualsManager.Instance;
                anchors.Add(visuals != null ? visuals.DoorOf(hall) : hall.transform.position);
                hallFirst = true;
            }
            var grid = GridManager.Instance;
            for (var t = 0; t < 60 && anchors.Count < AnchorCount; t++)
            {
                var p = grid.GetFootprintCenterWorld(cells[Random.Range(0, cells.Count)], Vector2Int.one);
                if (Hunting.OnLand(p, out var y)) anchors.Add(new Vector3(p.x, y, p.z));
            }
            return anchors;
        }

        private static readonly UnityEngine.AI.NavMeshPath Path = new UnityEngine.AI.NavMeshPath();

        /// <summary>From `from` a full NavMesh path to the Town Hall (if it is an anchor) and to most of the others.</summary>
        private static bool Reaches(Vector3 from, List<Vector3> anchors, bool hall)
        {
            if (anchors.Count == 0) return true;
            if (!UnityEngine.AI.NavMesh.SamplePosition(from, out var start, Cell, UnityEngine.AI.NavMesh.AllAreas)) return false;
            var reached = 0;
            for (var i = 0; i < anchors.Count; i++)
            {
                var ok = UnityEngine.AI.NavMesh.SamplePosition(anchors[i], out var end, Cell * 1.5f, UnityEngine.AI.NavMesh.AllAreas)
                         && UnityEngine.AI.NavMesh.CalculatePath(start.position, end.position, UnityEngine.AI.NavMesh.AllAreas, Path)
                         && Path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete;
                if (ok) reached++;
                else if (i == 0 && hall) return false;
            }
            return reached * 3 >= anchors.Count * 2;
        }

        private void Raise(Cave cave)
        {
            if (cave.Root != null) return;
            var grid = GridManager.Instance;
            var root = new GameObject("PredatorCave").transform;
            root.SetParent(transform, false);
            root.position = cave.Position;
            root.rotation = Quaternion.Euler(0f, cave.Yaw, 0f);
            if (caveModel != null)
            {
                var model = Instantiate(caveModel, root, false);
                // modelled with its mouth to Blender's -y, like the figures: turned round to face the root's forward
                model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * model.transform.localRotation;
                SceneryShadows.DisableCasting(model);
            }
            // nothing is built on it
            if (grid != null) grid.SetAreaOccupied(grid.WorldToCell(cave.Position), Vector2Int.one, true);
            FogCulling.Cull(root.gameObject, 1);
            cave.Root = root;
        }

        private Cave CaveOn(int island)
        {
            foreach (var c in _caves) if (c.Island == island) return c;
            return null;
        }

        // ---------------------------------------------------------------------------------------- an attack

        /// <summary>How many wolves and bears come for an island of `residents` (the user's table, rule 5).</summary>
        public static void GroupFor(int residents, out int wolves, out int bears)
        {
            var c = Config;
            wolves = Mathf.Max(1, Mathf.CeilToInt(residents / (float)Mathf.Max(1, c.PredWolfPerResidents)));
            bears = residents > c.PredBearFromResidents ? 1 + (residents - 1) / Mathf.Max(1, c.PredBearPerResidents) : 0;
        }

        /// <summary>One attack on a cave island with people on it; false when there is none.</summary>
        private bool Attack()
        {
            var people = HousePeople.Instance;
            if (people == null || OrcRaidManager.Instance == null) return false;
            var candidates = new List<Cave>();
            foreach (var c in _caves)
                if (people.CitizensOn(c.Island) > 0 && !Raided(c.Island)) candidates.Add(c);
            if (candidates.Count == 0) return false;
            var cave = candidates[Random.Range(0, candidates.Count)];
            var residents = people.CitizensOn(cave.Island);
            GroupFor(residents, out var wolves, out var bears);
            var asBears = bears > 0 && Random.value * 100f < Config.PredBearChancePct;
            if (!FindSpawn(cave, out var at)) return false;

            var raid = new Raid { Island = cave.Island, Cave = cave, Bears = asBears, Residents = residents };
            var count = asBears ? bears : wolves;
            for (var i = 0; i < count; i++)
            {
                var r = Random.insideUnitCircle * PackSpread * Cell;
                var p = at + new Vector3(r.x, 0f, r.y);
                if (!Hunting.OnLand(p, out var y)) p = at;
                else p.y = y;
                var beast = Spawn(p, asBears ? OrcKind.PredatorBear : OrcKind.PredatorWolf, raid);
                if (beast != null) raid.Beasts.Add(beast);
            }
            if (raid.Beasts.Count == 0) return false;
            _raids.Add(raid);
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Get(asBears ? (count > 1 ? "#log_pred_bears" : "#log_pred_bear") : "#log_pred_wolves"),
                EventSeverity.Danger, EventGlyph.Alert, NoticeIcon, at, raid.Beasts[0].transform));
            return true;
        }

        /// <summary>Seconds until the next attack is due (the cheats).</summary>
        public float SecondsToAttack => _next < 0f ? Config.PredFirstAttackSeconds - _clock : _next - _clock;

        /// <summary>An attack at once (the cheats window); the clock goes on from now.</summary>
        public bool AttackNow()
        {
            if (!_cavesMade) _cavesMade = MakeCaves();
            if (!Attack()) return false;
            _next = _clock + Random.Range(Config.PredAttackMinSeconds, Mathf.Max(Config.PredAttackMinSeconds, Config.PredAttackMaxSeconds));
            return true;
        }

        private bool Raided(int island)
        {
            foreach (var r in _raids) if (r.Island == island) return true;
            return false;
        }

        /// <summary>A random spot on the island, `pred_spawn_gap_cells` from every building; on a built-up island the farthest one found, or the cave.</summary>
        private bool FindSpawn(Cave cave, out Vector3 at)
        {
            at = cave.Position;
            var cells = Hunting.Instance.CellsOf(cave.Island);
            if (cells == null || cells.Count == 0) return cave.Root != null;
            var grid = GridManager.Instance;
            var gap = Config.PredSpawnGapCells * Cell;
            var best = -1f;
            for (var t = 0; t < 80; t++)
            {
                var p = grid.GetFootprintCenterWorld(cells[Random.Range(0, cells.Count)], Vector2Int.one);
                if (!Hunting.OnLand(p, out var y)) continue;
                // walkable to the cave, so not behind the mountains (the user, 2026-11-22)
                if (cave.Root != null && !Walkable(new Vector3(p.x, y, p.z), cave.Position)) continue;
                var near = NearestBuilding(p);
                if (near >= gap)
                {
                    at = new Vector3(p.x, y, p.z);
                    return true;
                }
                if (near > best) { best = near; at = new Vector3(p.x, y, p.z); }
            }
            if (NearestBuilding(cave.Position) > best) at = cave.Position;
            return true;
        }

        /// <summary>A full NavMesh path between the two points.</summary>
        private static bool Walkable(Vector3 a, Vector3 b)
        {
            return UnityEngine.AI.NavMesh.SamplePosition(a, out var from, Cell, UnityEngine.AI.NavMesh.AllAreas)
                   && UnityEngine.AI.NavMesh.SamplePosition(b, out var to, Cell * 1.5f, UnityEngine.AI.NavMesh.AllAreas)
                   && UnityEngine.AI.NavMesh.CalculatePath(from.position, to.position, UnityEngine.AI.NavMesh.AllAreas, Path)
                   && Path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete;
        }

        private static float NearestBuilding(Vector3 p)
        {
            var best = float.MaxValue;
            foreach (var b in BuildingInstance.All)
            {
                if (b == null) continue;
                var d = (b.transform.position - p).WithY(0f).sqrMagnitude;
                if (d < best) best = d;
            }
            return Mathf.Sqrt(best);
        }

        private OrcUnit Spawn(Vector3 at, OrcKind kind, Raid raid)
        {
            var beast = OrcRaidManager.Instance.SpawnOrcOfKind(at, 1, kind);
            if (beast == null) return null;
            beast.Steered = true;
            beast.transform.position = at;
            beast.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            beast.gameObject.AddComponent<PredatorErrand>().Raid = raid;
            return beast;
        }

        // ---------------------------------------------------------------------------------------- what the beasts do

        /// <summary>The nearest live predator on this island, for its hunters; null if none.</summary>
        public OrcUnit NearestOn(int island, Vector3 from)
        {
            OrcUnit best = null;
            var bestD = float.MaxValue;
            foreach (var raid in _raids)
            {
                if (!HousePeople.Same(raid.Island, island)) continue;
                foreach (var b in raid.Beasts)
                {
                    if (b == null || b.CurrentHealth <= 0) continue;
                    var d = (b.transform.position - from).sqrMagnitude;
                    if (d < bestD) { bestD = d; best = b; }
                }
            }
            return best;
        }

        /// <summary>A hunter's arrow lands on a predator: its blow, remembered, so the kill pays hides.</summary>
        public void HunterHit(OrcUnit beast, int damage)
        {
            if (beast == null || beast.CurrentHealth <= 0) return;
            _hunterBlow = beast;
            beast.TakeDamage(damage);
            _hunterBlow = null;
        }

        private void OnBeastDied(OrcUnit beast)
        {
            if (beast == null || !OrcUnit.IsPredator(beast.Kind) || beast.GetComponent<PredatorErrand>() == null) return;
            var resources = ResourceManager.Instance;
            if (resources == null) return;
            var bear = beast.Kind == OrcKind.PredatorBear;
            var at = beast.transform.position;
            var food = resources.AddProduced(ResourceType.Food, bear ? Config.PredBearFood : Config.PredWolfFood, at);
            ResourceGainPopups.Instance?.Show(at + Vector3.up * 1.1f, food, ResourceType.Food);
            if (beast != _hunterBlow) return;
            var hides = resources.AddProduced(ResourceType.Hides, bear ? Config.PredBearHides : Config.PredWolfHides, at);
            ResourceGainPopups.Instance?.Show(at + Vector3.up * 0.7f, hides, ResourceType.Hides);
        }

        /// <summary>The pack has had its fill (rule 8): wolves by the people they wounded, anyone by a store half eaten.</summary>
        internal static void Check(Raid raid)
        {
            if (raid.Going) return;
            if (!raid.Bears && raid.Wounded >= Mathf.Max(1, Mathf.CeilToInt(raid.Residents * Config.PredWolfSuccessPct / 100f))) raid.Going = true;
            foreach (var pair in raid.StoreDamage)
                if (pair.Key != null && pair.Value >= pair.Key.MaxHealth * Config.PredStoreSuccessShare) raid.Going = true;
        }

        // ---------------------------------------------------------------------------------------- save

        public struct SavedCave
        {
            public int Island;
            public float X, Y, Z, Yaw;
        }

        public void Capture(out float clock, out float next, List<SavedCave> into)
        {
            clock = _clock;
            next = _next;
            foreach (var c in _caves)
                into.Add(new SavedCave { Island = c.Island, X = c.Position.x, Y = c.Position.y, Z = c.Position.z, Yaw = c.Yaw });
        }

        public void RestoreFromSave(float clock, float next, List<SavedCave> caves)
        {
            foreach (var r in _raids)
                foreach (var b in r.Beasts)
                    if (b != null) Destroy(b.gameObject);
            _raids.Clear();
            foreach (var c in _caves)
                if (c.Root != null) Destroy(c.Root.gameObject);
            _caves.Clear();
            _clock = clock;
            _next = next;
            _cavesMade = caves != null && caves.Count > 0;
            if (caves == null) return;
            foreach (var s in caves)
            {
                var cave = new Cave { Island = s.Island, Position = new Vector3(s.X, s.Y, s.Z), Yaw = s.Yaw };
                _caves.Add(cave);
                Raise(cave);
            }
        }
    }

    /// <summary>
    /// One predator's mind (Predators): it fights back a soldier, hero or the King in reach the usual way (OrcUnit.DefendSelf),
    /// else goes for a hunter near it, a villager near it, the island's nearest food store, any villager of the island -- and,
    /// once its pack has had its fill or its time is up, walks to the cave and is gone.
    /// </summary>
    public class PredatorErrand : MonoBehaviour
    {
        internal Predators.Raid Raid;

        private OrcUnit _orc;
        private CitizenAgent _citizen;
        private BuildingInstance _store;
        private Transform _hunter;
        private Vector3 _goal;
        private float _look, _strike;

        private const float LookSeconds = 0.5f, StoreReach = 0.8f;

        private void Start()
        {
            _orc = GetComponent<OrcUnit>();
            if (_orc != null) _orc.Steered = true;
        }

        private bool Bear => _orc.Kind == OrcKind.PredatorBear;

        private void Update()
        {
            if (_orc == null || Raid == null || _orc.CurrentHealth <= 0) return;
            var dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (_orc.DefendSelf()) return;

            if (Raid.Going)
            {
                if (_orc.SteerTo(Raid.Cave.Position)) return;
                Destroy(gameObject);   // into the cave: gone
                return;
            }

            if ((_look -= dt) <= 0f)
            {
                _look = LookSeconds;
                Choose();
            }
            _strike -= dt;

            if (_hunter != null)
            {
                if (Close(_hunter.position, _orc.UnitRange)) Bite(_hunter.position, () => Hunting.Instance?.HurtHunter(_hunter, _orc.AttackDamage));
                else _orc.SteerTo(_hunter.position);
                return;
            }
            if (_citizen != null)
            {
                if (!_citizen.gameObject.activeInHierarchy) { _citizen = null; return; }
                if (Close(_citizen.transform.position, _orc.UnitRange)) Bite(_citizen.transform.position, BiteCitizen);
                else _orc.SteerTo(_citizen.transform.position);
                return;
            }
            if (_store != null)
            {
                if (_store.CurrentHealth <= 0) { _store = null; return; }
                if (Close(_goal, StoreReach + _orc.UnitRange) || !_orc.SteerTo(_goal)) Bite(_store.transform.position, BiteStore);
                return;
            }
            // nothing on the island to go for: home
            Raid.Going = true;
        }

        private bool Close(Vector3 p, float reach) => (p - transform.position).WithY(0f).magnitude <= reach;

        private void Bite(Vector3 at, System.Action land)
        {
            if (_strike > 0f) return;
            _strike = _orc.AttackInterval;
            _orc.ShowStrike(at);
            CombatSound.Clash(transform.position);
            land();
        }

        private void Choose()
        {
            var here = transform.position;
            var aggro = _orc.AggroRadius;
            _hunter = Hunting.Instance != null ? Hunting.Instance.NearestHunter(Raid.Island, here, aggro) : null;
            if (_hunter != null) return;
            if (_citizen != null && (!_citizen.gameObject.activeInHierarchy || Wounded(_citizen))) _citizen = null;
            // a bear is after the stores: it takes the people in its path, it does not hunt them down
            var near = NearestCitizen(here, Bear ? _orc.UnitRange * 2.5f : aggro);
            if (near != null) { _citizen = near; _store = null; return; }
            if (_store == null || _store.CurrentHealth <= 0)
            {
                _store = NearestStore(here);
                if (_store != null)
                {
                    var visuals = CitizenVisualsManager.Instance;
                    _goal = visuals != null ? visuals.DoorOf(_store) : _store.transform.position;
                }
            }
            if (_store != null) { _citizen = null; return; }
            if (_citizen == null) _citizen = NearestCitizen(here, float.MaxValue);
        }

        private bool Wounded(CitizenAgent agent) => Infirmary.Instance != null && Infirmary.Instance.IsWounded(agent);

        private CitizenAgent NearestCitizen(Vector3 from, float radius)
        {
            var visuals = CitizenVisualsManager.Instance;
            if (visuals == null) return null;
            CitizenAgent best = null;
            var bestD = radius < float.MaxValue ? radius * radius : float.MaxValue;
            foreach (var agent in visuals.AllAgents)
            {
                if (agent == null || !agent.gameObject.activeInHierarchy || Wounded(agent)) continue;
                var d = (agent.transform.position - from).sqrMagnitude;
                if (d >= bestD || !HousePeople.Same(HousePeople.IslandOf(agent.transform.position), Raid.Island)) continue;
                bestD = d;
                best = agent;
            }
            return best;
        }

        private BuildingInstance NearestStore(Vector3 from)
        {
            BuildingInstance best = null;
            var bestD = float.MaxValue;
            foreach (var b in BuildingInstance.All)
            {
                if (b == null || b.IsBot || b.CurrentHealth <= 0 || b.Data == null) continue;
                var id = b.Data.buildingName;
                if (id != BuildingIds.Granary && id != "BigGranary") continue;
                if (!HousePeople.Same(HousePeople.IslandOf(b), Raid.Island)) continue;
                var d = (b.transform.position - from).sqrMagnitude;
                if (d < bestD) { bestD = d; best = b; }
            }
            return best;
        }

        private void BiteCitizen()
        {
            var agent = _citizen;
            if (agent == null) return;
            if (Bear)
            {
                // a bear kills (rule 6): a body, then the cemetery and the plague as they are
                var at = agent.transform.position;
                CitizenVisualsManager.Instance?.KillAgent(agent);
                EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_pred_bear_killed"), EventSeverity.Danger,
                    EventGlyph.Skull, Predators.NoticeIcon, at, transform));
            }
            else if (Infirmary.Instance != null && Infirmary.Instance.Wound(agent))
            {
                Raid.Wounded++;
            }
            _citizen = null;
            Predators.Check(Raid);
        }

        private void BiteStore()
        {
            var store = _store;
            if (store == null) return;
            var before = store.CurrentHealth;
            store.TryDamage(_orc.AttackDamage);
            var after = store.CurrentHealth;
            var dealt = before - after;
            if (dealt <= 0) return;
            // the food goes with the health (rule 7); a store brought down loses the lot (StoreLedger.OnStoreLost)
            if (after > 0) StoreLedger.Eat(store, ResourceType.Food, after / (float)before);
            Raid.StoreDamage.TryGetValue(store, out var sum);
            Raid.StoreDamage[store] = sum + dealt;
            if (!Raid.ToldStore)
            {
                Raid.ToldStore = true;
                EventLogManager.Instance?.Post(GameEvent.At(Localization.Format("#log_pred_store", store.Data.LocalizedName), EventSeverity.Danger,
                    EventGlyph.Alert, Predators.NoticeIcon, store.transform.position, store.transform));
            }
            Predators.Check(Raid);
        }
    }
}
