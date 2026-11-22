using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Resources;
using CityBuilder.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CityBuilder.Maps
{
    /// <summary>
    /// The sandbox hunt (the user, 2026-11-21; card Design/features/hunting.md). Numbers: hunt_* on «Еда и голод».
    ///
    /// Herds of hares, deer or boars appear on the islands -- the first ones `hunt_first_herd_sec` into the game, one of
    /// them on the player's own island, `hunt_herds` on the map in all, one an island, never on the orcs' or the bot's
    /// island, never within `hunt_building_gap_cells` of a building (a built-up island gets none, by design). A herd's
    /// size is rolled (small / medium / large); it roams `hunt_herd_radius_cells` round its centre for
    /// `hunt_herd_life_sec`, never leaving earlier, then goes, and a new herd appears on another island. A round sign
    /// with the animal hangs over its centre: a tap lights the herd yellow and says how many are left and when it goes.
    /// A notice tells the player when a herd appears (a tap there flies the camera to it; no minimap mark).
    ///
    /// A Хижина охотника (one an island) has 1..5 hunters by level (its workers). A worker goes into the hut
    /// (WorkSpots: none outside) and his HUNTER walks out: a jointed figure of his own (SoldierHunter) that roams the
    /// island and checks every `hunt_check_sec` for a herd on it. With one there he walks within
    /// `hunt_shot_range_cells` of an animal and shoots every `hunt_shot_sec`; a hit (the kind's `hunt_*_hit_pct`)
    /// kills it and pays its food and hides at once; a miss at a boar may set it on him (`hunt_boar_attack_pct`) and
    /// kill him -- the hut loses that worker, the town that citizen.
    ///
    /// Mobile first: a few dozen animals at most, moved by a step test on the map's own land (no NavMesh), the
    /// figures' FigureAnimator does the walking legs. Saved (GameSaveController): the clock and the herds.
    /// </summary>
    public class Hunting : MonoBehaviour
    {
        public enum Kind { Hare, Deer, Boar }

        [SerializeField] private GameObject[] animalModels = new GameObject[3];
        [SerializeField] private GameObject hunterModel;
        [SerializeField] private Sprite[] signIcons = new Sprite[3];
        [SerializeField] private Material highlightMaterial;

        public static Hunting Instance { get; private set; }

        /// <summary>The sandbox's rules are on (not the Invasion, the tutorial or the campaign town).</summary>
        public static bool Active => Instance != null && Infirmary.Active;

        // visual feel, not balance: how fast they go, how long a lit herd stays lit, the sign's height
        private const float HareSpeed = 0.5f, DeerSpeed = 0.7f, BoarSpeed = 0.45f, HunterSpeed = 0.9f, HunterHurry = 1.2f;
        private const float LitSeconds = 6f, SignHeight = 2.2f, SignDiameter = 86f, RetrySeconds = 30f;
        private const float LooseDelay = 0.35f, ArrowFlight = 0.45f, DeadSeconds = 5f, LeaveSeconds = 4f;
        private const int Stride = 2;

        internal sealed class Animal
        {
            public Kind Kind;
            public Transform Root;
            public FigureAnimator Animator;
            public Transform Ring;
            public Vector3 Velocity;
            public int State;          // 0 graze, 1 walk, 2 charge, 3 dead
            public float Timer;
            public float Age;
            public Vector3 Goal;
        }

        public sealed class Herd
        {
            public Kind Kind;
            public int Size;
            public int Total;
            public int Island;
            public Vector3 Centre;
            public float Radius;
            public float Life;
            public bool Leaving;
            internal readonly List<Animal> Animals = new List<Animal>();
            internal RectTransform Sign;
            internal Transform Area;
            internal float LitLeft;

            public int Alive
            {
                get
                {
                    var n = 0;
                    foreach (var a in Animals) if (a.State != 3 && a.Root != null) n++;
                    return n;
                }
            }
        }

        private sealed class Hunter
        {
            public ProductionBuilding Hut;
            public Transform Root;
            public FigureAnimator Animator;
            public Herd Herd;
            public Animal Prey;
            public Vector3 Goal;
            public bool Walking;
            public float Timer, CheckClock, ShotClock;
            public float Loose = -1f;
            public bool Dead;
        }

        private sealed class Arrow
        {
            public Hunter From;
            public Animal Prey;
            public bool Hit;
            public float Left;
        }

        private readonly List<Herd> _herds = new List<Herd>();
        private readonly List<Hunter> _hunters = new List<Hunter>();
        private readonly List<Arrow> _arrows = new List<Arrow>();
        private readonly Dictionary<int, List<Vector2Int>> _islandCells = new Dictionary<int, List<Vector2Int>>();
        private float _clock, _retry, _syncClock;
        private bool _started;
        private int _lastIsland = int.MinValue;
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private static Mesh _ringMesh;

        /// <summary>Seconds of sandbox play this game (saved): the first herds come at hunt_first_herd_sec.</summary>
        public float Clock => _clock;
        public IReadOnlyList<Herd> Herds => _herds;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private static BalanceConfig Config => BalanceConfig.Instance;
        private static float Cell => GridManager.Instance != null ? GridManager.Instance.CellSize : 1f;

        // ---------------------------------------------------------------------------------------- the clock

        private void Update()
        {
            var dt = Time.deltaTime;
            if (dt <= 0f || !Active || Config == null || GridManager.Instance == null || MeshMapApplier.Instance == null) return;
            _clock += dt;
            if (!_started && _clock >= Config.HuntFirstHerdSeconds)
            {
                _started = true;
                Fill(true);
            }

            for (var i = _herds.Count - 1; i >= 0; i--)
            {
                var herd = _herds[i];
                if (!herd.Leaving)
                {
                    herd.Life -= dt;
                    if (herd.Life <= 0f) Leave(herd);
                }
                herd.LitLeft = Mathf.Max(0f, herd.LitLeft - dt);
                var lit = herd.LitLeft > 0f && !herd.Leaving;
                if (herd.Area != null && herd.Area.gameObject.activeSelf != lit) herd.Area.gameObject.SetActive(lit);
                var any = false;
                for (var k = herd.Animals.Count - 1; k >= 0; k--)
                {
                    var a = herd.Animals[k];
                    if (a.Root == null) { herd.Animals.RemoveAt(k); continue; }
                    any = true;
                    a.Age += dt;
                    if (a.Ring != null && a.Ring.gameObject.activeSelf != (lit && a.State != 3)) a.Ring.gameObject.SetActive(lit && a.State != 3);
                    Move(herd, a, dt);
                }
                if (herd.Sign != null && (herd.Leaving || herd.Alive == 0))
                {
                    Destroy(herd.Sign.gameObject);
                    herd.Sign = null;
                }
                if (herd.Leaving && !any)
                {
                    if (herd.Area != null) Destroy(herd.Area.gameObject);
                    _herds.RemoveAt(i);
                }
            }

            if (_started && (_retry -= dt) <= 0f)
            {
                _retry = RetrySeconds;
                Fill(false);
            }

            if ((_syncClock -= dt) <= 0f)
            {
                _syncClock = 1f;
                SyncHunters();
            }
            for (var i = _hunters.Count - 1; i >= 0; i--) TickHunter(_hunters[i], dt);
            TickArrows(dt);
        }

        private int Standing()
        {
            var n = 0;
            foreach (var h in _herds) if (!h.Leaving) n++;
            return n;
        }

        /// <summary>Brings the map up to hunt_herds; the very first, on `first`, on the player's own island.</summary>
        private void Fill(bool first)
        {
            var want = Mathf.Max(0, Config.HuntHerds);
            var tries = 0;
            while (Standing() < want && tries++ < want + 2)
            {
                var island = first && Standing() == 0 ? PlayerIsland() : PickIsland();
                if (island == int.MinValue || !Spawn(island) && !(first && Spawn(PickIsland()))) break;
            }
        }

        // ---------------------------------------------------------------------------------------- islands

        private void LearnIslands()
        {
            if (_islandCells.Count > 0) return;
            var map = MeshMapApplier.Instance;
            var grid = GridManager.Instance;
            var size = grid.GridSize;
            for (var z = 0; z < size.y; z += Stride)
            for (var x = 0; x < size.x; x += Stride)
            {
                var cell = new Vector2Int(x, z);
                if (map.IsWaterCell(cell)) continue;
                var island = map.IslandLabels != null ? map.IslandAt(cell) : -1;
                if (map.IslandLabels != null && island < 0) continue;
                if (!_islandCells.TryGetValue(island, out var list)) _islandCells[island] = list = new List<Vector2Int>();
                list.Add(cell);
            }
        }

        private static int IslandOfCell(Vector2Int? cell)
        {
            var map = MeshMapApplier.Instance;
            return cell.HasValue && map != null && map.IslandLabels != null ? map.IslandAt(cell.Value) : int.MinValue;
        }

        private static int PlayerIsland()
        {
            foreach (var b in BuildingInstance.All)
                if (b != null && !b.IsBot && b.Data != null && b.Data.buildingName == BuildingIds.TownHall) return HousePeople.IslandOf(b);
            return int.MinValue;
        }

        /// <summary>A random island for a new herd: not the orcs', not the bot's, none with a herd, not the one just left.</summary>
        private int PickIsland()
        {
            LearnIslands();
            var map = MeshMapApplier.Instance;
            int orc = IslandOfCell(map.ArchipelagoOrcCell), bot = IslandOfCell(map.ArchipelagoBotCell);
            var candidates = new List<int>();
            foreach (var pair in _islandCells)
            {
                var k = pair.Key;
                if (k == orc || k == bot || pair.Value.Count < 40 || HerdOn(k, true) != null) continue;
                candidates.Add(k);
            }
            if (candidates.Count > 1) candidates.Remove(_lastIsland);
            return candidates.Count == 0 ? int.MinValue : candidates[Random.Range(0, candidates.Count)];
        }

        private Herd HerdOn(int island, bool includeLeaving = false)
        {
            foreach (var h in _herds)
                if (h.Island == island && (includeLeaving || !h.Leaving) && (includeLeaving || h.Alive > 0)) return h;
            return null;
        }

        /// <summary>Open flat land with nothing on it (the Wildlife's own test), and its height.</summary>
        private static bool OnLand(Vector3 p, out float y)
        {
            y = 0f;
            var map = MeshMapApplier.Instance;
            var grid = GridManager.Instance;
            var cell = grid.WorldToCell(p);
            if (!grid.IsWithinBounds(cell, Vector2Int.one) || map.IsWaterCell(cell)) return false;
            if (!map.TryGetGroundHeight(p, out y)) return false;
            return Mathf.Abs(y - grid.GroundHeight) < 0.6f && grid.IsAreaFree(cell, Vector2Int.one);
        }

        private static bool FarFromBuildings(Vector3 p, float gap)
        {
            foreach (var b in BuildingInstance.All)
            {
                if (b == null) continue;
                var d = b.transform.position - p;
                d.y = 0f;
                if (d.sqrMagnitude < gap * gap) return false;
            }
            return true;
        }

        private bool FindSpot(int island, out Vector3 centre)
        {
            centre = Vector3.zero;
            LearnIslands();
            if (!_islandCells.TryGetValue(island, out var cells) || cells.Count == 0) return false;
            var grid = GridManager.Instance;
            var gap = Config.HuntBuildingGapCells * Cell;
            var radius = Config.HuntHerdRadiusCells * Cell;
            for (var t = 0; t < 60; t++)
            {
                var p = grid.GetFootprintCenterWorld(cells[Random.Range(0, cells.Count)], Vector2Int.one);
                if (!OnLand(p, out var y) || !FarFromBuildings(p, gap + radius * 0.5f)) continue;
                var open = 0;
                for (var k = 0; k < 8; k++)
                {
                    var a = k * Mathf.PI / 4f;
                    if (OnLand(p + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius * 0.6f, out _)) open++;
                }
                if (open < 5) continue;
                centre = new Vector3(p.x, y, p.z);
                return true;
            }
            return false;
        }

        // ---------------------------------------------------------------------------------------- herds

        private bool Spawn(int island)
        {
            if (island == int.MinValue || !FindSpot(island, out var centre)) return false;
            var kind = (Kind)Random.Range(0, 3);
            var roll = Random.value * 100f;
            var size = roll < Config.HuntSizeSmallPct ? 0 : roll < Config.HuntSizeSmallPct + Config.HuntSizeMediumPct ? 1 : 2;
            var herd = new Herd
            {
                Kind = kind, Size = size, Total = Mathf.Max(1, Config.HuntCount((int)kind, size)), Island = island,
                Centre = centre, Radius = Config.HuntHerdRadiusCells * Cell, Life = Config.HuntHerdLifeSeconds,
            };
            Populate(herd, herd.Total);
            _herds.Add(herd);
            _lastIsland = island;
            // only where the player lives (the user, 2026-11-22: a notice for a herd on an empty island read as a herd
            // that was not there)
            if (PlayerBuildsOn(island))
                EventLogManager.Instance?.Post(GameEvent.At(Localization.Format("#log_herd_appeared", HerdName(kind)),
                EventSeverity.Good, EventGlyph.Star, "Herd" + kind, centre));
            return true;
        }

        /// <summary>
        /// The Hunter's Hut's card line: a hunter's chance to hit each kind (the user, 2026-11-22); null for any other building.
        /// </summary>
        public static string CardLine(BuildingInstance building)
        {
            if (building == null || building.Data == null || building.Data.buildingName != BuildingIds.HunterHut || Config == null) return null;
            return Localization.Format("#hunt_card_chances", Config.HuntHitPct(0), Config.HuntHitPct(1), Config.HuntHitPct(2));
        }

        /// <summary>Has the player a building of his own on this island?</summary>
        private static bool PlayerBuildsOn(int island)
        {
            foreach (var b in BuildingInstance.All)
                if (b != null && !b.IsBot && HousePeople.IslandOf(b) == island) return true;
            return false;
        }

        public static string HerdName(Kind kind) => Localization.Get("#herd_" + kind.ToString().ToLowerInvariant());

        private void Populate(Herd herd, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var p = herd.Centre + Random.insideUnitSphere.WithY(0f) * herd.Radius * 0.45f;
                if (!OnLand(p, out var y)) { p = herd.Centre; y = herd.Centre.y; }
                herd.Animals.Add(MakeAnimal(herd.Kind, new Vector3(p.x, y, p.z)));
            }
            herd.Area = Ring(transform, herd.Centre + Vector3.up * 0.04f, herd.Radius, 0.97f);
            herd.Area.gameObject.SetActive(false);
            herd.Sign = MakeSign(herd);
        }

        private Animal MakeAnimal(Kind kind, Vector3 at)
        {
            var root = new GameObject("Herd" + kind).transform;
            root.SetParent(transform, false);
            root.position = at;
            root.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var model = animalModels != null && (int)kind < animalModels.Length ? animalModels[(int)kind] : null;
            FigureAnimator animator = null;
            if (model != null)
            {
                var figure = Instantiate(model, root, false);
                // modelled facing Blender's -y: turned round to go nose first (as Wildlife)
                figure.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * figure.transform.localRotation;
                SceneryShadows.DisableCasting(figure);
                animator = root.gameObject.AddComponent<FigureAnimator>();
                animator.Bind(figure.transform);
            }
            var ring = Ring(root, at + Vector3.up * 0.03f, kind == Kind.Hare ? 0.22f : 0.4f, 0.72f);
            ring.gameObject.SetActive(false);
            return new Animal { Kind = kind, Root = root, Animator = animator, Ring = ring, Timer = Random.Range(1f, 5f) };
        }

        private float SpeedOf(Kind kind) => kind == Kind.Hare ? HareSpeed : kind == Kind.Deer ? DeerSpeed : BoarSpeed;

        private void Move(Herd herd, Animal a, float dt)
        {
            if (a.State == 3)
            {
                // lying where it fell, then sinking away
                if (a.Age > DeadSeconds) a.Root.position += Vector3.down * dt * 0.3f;
                if (a.Age > DeadSeconds + 1.2f) Destroy(a.Root.gameObject);
                return;
            }
            var here = a.Root.position;
            if (herd.Leaving)
            {
                // off out of the reach and away
                var out_ = (here - herd.Centre).WithY(0f);
                a.Velocity = (out_.sqrMagnitude > 0.01f ? out_.normalized : a.Root.forward) * SpeedOf(a.Kind) * 3f;
                a.Root.position = here + a.Velocity * dt;
                Face(a.Root, a.Velocity, dt * 8f);
                a.Timer -= dt;
                if (a.Timer <= 0f) Destroy(a.Root.gameObject);
                return;
            }
            a.Timer -= dt;
            if (a.State == 2)
            {
                // a boar charging its hunter
                a.Root.position = here + a.Velocity * dt;
                Face(a.Root, a.Velocity, dt * 10f);
                if (a.Timer <= 0f) { a.State = 0; a.Timer = 2f; }
                return;
            }
            if (a.State == 0)
            {
                if (a.Timer > 0f) return;
                var r = Random.insideUnitCircle * herd.Radius * 0.85f;
                a.Goal = herd.Centre + new Vector3(r.x, 0f, r.y);
                a.State = 1;
                a.Timer = 12f;
            }
            var to = (a.Goal - here).WithY(0f);
            if (to.sqrMagnitude < 0.04f || a.Timer <= 0f) { a.State = 0; a.Timer = Random.Range(2f, 6f); return; }
            a.Velocity = to.normalized * SpeedOf(a.Kind);
            var next = here + a.Velocity * dt;
            if (!OnLand(next, out var y)) { a.State = 0; a.Timer = 1f; return; }
            var hop = a.Kind == Kind.Hare ? Mathf.Abs(Mathf.Sin(a.Age * 9f)) * 0.08f : 0f;
            a.Root.position = new Vector3(next.x, y + hop, next.z);
            Face(a.Root, a.Velocity, dt * 8f);
        }

        private static void Face(Transform t, Vector3 heading, float rate)
        {
            heading.y = 0f;
            if (heading.sqrMagnitude < 0.0001f) return;
            t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(heading), Mathf.Clamp01(rate));
        }

        private void Leave(Herd herd)
        {
            herd.Leaving = true;
            foreach (var a in herd.Animals)
                if (a.State != 3) { a.Timer = LeaveSeconds; a.State = 1; }
        }

        private static void Die(Animal a)
        {
            a.State = 3;
            a.Age = 0f;
            if (a.Animator != null) a.Animator.enabled = false;
            if (a.Ring != null) a.Ring.gameObject.SetActive(false);
            // on its side
            a.Root.rotation = a.Root.rotation * Quaternion.Euler(0f, 0f, 80f);
            a.Root.position += Vector3.up * 0.05f;
        }

        // ---------------------------------------------------------------------------------------- the sign

        private void EnsureCanvas()
        {
            if (_canvas != null) return;
            var go = new GameObject("HuntCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            _canvas = go.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = -5;   // over the world, UNDER the HUD (its canvas is at 0; the user 2026-11-22: bubbles drew over the interface)
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            _canvasRect = (RectTransform)go.transform;
        }

        private RectTransform MakeSign(Herd herd)
        {
            EnsureCanvas();
            var go = new GameObject("HerdSign", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_canvasRect, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(SignDiameter, SignDiameter);
            var rim = go.GetComponent<Image>();
            rim.sprite = NodeYieldBubble.CircleSprite();
            rim.color = new Color(0.48f, 0.29f, 0.14f, 1f);
            var disc = new GameObject("Disc", typeof(RectTransform), typeof(Image));
            disc.transform.SetParent(rect, false);
            var discRect = (RectTransform)disc.transform;
            discRect.sizeDelta = new Vector2(SignDiameter - 10f, SignDiameter - 10f);
            var face = disc.GetComponent<Image>();
            face.sprite = rim.sprite;
            face.color = new Color(0.98f, 0.93f, 0.78f, 1f);
            face.raycastTarget = false;
            var icon = new GameObject("Animal", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(rect, false);
            ((RectTransform)icon.transform).sizeDelta = new Vector2(SignDiameter * 0.78f, SignDiameter * 0.78f);
            var image = icon.GetComponent<Image>();
            image.sprite = signIcons != null && (int)herd.Kind < signIcons.Length ? signIcons[(int)herd.Kind] : null;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var button = go.GetComponent<Button>();
            button.targetGraphic = rim;
            button.onClick.AddListener(() => Light(herd));
            return rect;
        }

        /// <summary>A tap on the sign: the herd lit yellow for a while, and how many are left and when it goes.</summary>
        public void Light(Herd herd)
        {
            if (herd == null || herd.Leaving) return;
            herd.LitLeft = LitSeconds;
            // and the camera to it (the user, 2026-11-22)
            CameraControl.RTSCameraController.Instance?.FocusOn(herd.Centre);
            var left = Mathf.CeilToInt(Mathf.Max(0f, herd.Life));
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Format("#log_herd_status", HerdName(herd.Kind), herd.Alive, herd.Total,
                    $"{left / 60}:{left % 60:00}"), EventSeverity.Good, EventGlyph.Star, "Herd" + herd.Kind, herd.Centre));
        }

        private void LateUpdate()
        {
            if (_canvas == null) return;
            var camera = Camera.main;
            var hidden = camera == null || ModalGate.IsBlocked || !Active;
            if (_canvas.enabled == hidden) _canvas.enabled = !hidden;
            if (hidden) return;
            var t = Time.unscaledTime;
            foreach (var herd in _herds)
            {
                if (herd.Sign == null) continue;
                var screen = camera.WorldToScreenPoint(herd.Centre + Vector3.up * SignHeight);
                if (screen.z <= 0f) { herd.Sign.localScale = Vector3.zero; continue; }
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out var local);
                herd.Sign.anchoredPosition = local + new Vector2(0f, 6f * Mathf.Sin(t * 2.4f));
                herd.Sign.localScale = Vector3.one * (herd.LitLeft > 0f ? 1.12f : 1f);
            }
        }

        // ---------------------------------------------------------------------------------------- the yellow rings

        private Transform Ring(Transform parent, Vector3 at, float radius, float inner)
        {
            var go = new GameObject("HuntRing", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, true);
            go.transform.position = at;
            go.transform.rotation = Quaternion.identity;
            var scale = parent != null ? Mathf.Max(0.0001f, parent.lossyScale.x) : 1f;
            go.transform.localScale = Vector3.one * (radius / scale);
            go.GetComponent<MeshFilter>().sharedMesh = RingMesh(inner);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = highlightMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go.transform;
        }

        private static readonly Dictionary<float, Mesh> RingMeshes = new Dictionary<float, Mesh>();

        /// <summary>A flat unit ring in XZ from `inner` to 1.</summary>
        private static Mesh RingMesh(float inner)
        {
            if (RingMeshes.TryGetValue(inner, out var mesh) && mesh != null) return mesh;
            const int n = 32;
            var v = new Vector3[n * 2];
            var tri = new int[n * 6];
            for (var i = 0; i < n; i++)
            {
                var a = Mathf.PI * 2f * i / n;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                v[i * 2] = d * inner;
                v[i * 2 + 1] = d;
                var j = (i + 1) % n;
                var t = i * 6;
                tri[t] = i * 2; tri[t + 1] = j * 2 + 1; tri[t + 2] = i * 2 + 1;
                tri[t + 3] = i * 2; tri[t + 4] = j * 2; tri[t + 5] = j * 2 + 1;
            }
            mesh = new Mesh { name = "HuntRing", vertices = v, triangles = tri };
            mesh.RecalculateBounds();
            RingMeshes[inner] = mesh;
            return mesh;
        }

        // ---------------------------------------------------------------------------------------- hunters

        private static bool IsHut(ProductionBuilding p) =>
            p != null && p.BuildingId == BuildingIds.HunterHut && p.Instance != null && !p.Instance.IsBot && p.Instance.IsOperational;

        private void SyncHunters()
        {
            for (var i = _hunters.Count - 1; i >= 0; i--)
            {
                var h = _hunters[i];
                if (h.Dead || h.Root == null || !IsHut(h.Hut)) { Drop(i); continue; }
            }
            foreach (var hut in ProductionBuilding.All)
            {
                if (!IsHut(hut)) continue;
                var have = 0;
                foreach (var h in _hunters) if (h.Hut == hut) have++;
                for (; have < hut.AssignedWorkers; have++) _hunters.Add(MakeHunter(hut));
                for (var i = _hunters.Count - 1; i >= 0 && have > hut.AssignedWorkers; i--)
                    if (_hunters[i].Hut == hut) { Drop(i); have--; }
            }
        }

        private void Drop(int index)
        {
            var h = _hunters[index];
            if (h.Root != null) Destroy(h.Root.gameObject);
            _hunters.RemoveAt(index);
        }

        private Hunter MakeHunter(ProductionBuilding hut)
        {
            var root = new GameObject("Hunter").transform;
            root.SetParent(transform, false);
            var at = hut.transform.position + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Cell;
            root.position = OnLand(at, out var y) ? new Vector3(at.x, y, at.z) : hut.transform.position;
            root.localScale = Vector3.one * UnitSize.Scale;
            FigureAnimator animator = null;
            if (hunterModel != null)
            {
                var figure = Instantiate(hunterModel, root, false);
                figure.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * figure.transform.localRotation;
                animator = root.gameObject.AddComponent<FigureAnimator>();
                animator.Bind(figure.transform);
            }
            return new Hunter
            {
                Hut = hut, Root = root, Animator = animator, Timer = Random.Range(1f, 4f),
                CheckClock = Random.Range(0f, Config.HuntCheckSeconds),
            };
        }

        private void TickHunter(Hunter h, float dt)
        {
            if (h.Dead || h.Root == null) return;
            if ((h.CheckClock -= dt) <= 0f)
            {
                h.CheckClock = Config.HuntCheckSeconds;
                var herd = HerdOn(h.Hut.Island);
                if (herd != h.Herd)
                {
                    h.Herd = herd;
                    h.Prey = null;
                    h.ShotClock = Config.HuntShotSeconds * Random.Range(0.3f, 1f);
                }
            }
            if (h.Herd != null && (h.Herd.Leaving || h.Herd.Alive == 0)) { h.Herd = null; h.Prey = null; }

            if (h.Loose >= 0f)
            {
                h.Loose -= dt;
                if (h.Loose < 0f) Shoot(h);
                return;
            }

            if (h.Herd == null)
            {
                // round the hut, idling between walks
                if (!h.Walking)
                {
                    if ((h.Timer -= dt) > 0f) return;
                    var r = Random.insideUnitCircle * 6f * Cell;
                    h.Goal = h.Hut.transform.position + new Vector3(r.x, 0f, r.y);
                    h.Walking = true;
                    h.Timer = 15f;
                }
                if (!WalkTo(h, h.Goal, HunterSpeed, dt, 0.2f) && (h.Timer -= dt) > 0f) return;
                h.Walking = false;
                h.Timer = Random.Range(3f, 8f);
                return;
            }

            if (h.Prey == null || h.Prey.State == 3 || h.Prey.Root == null) h.Prey = Nearest(h.Herd, h.Root.position);
            if (h.Prey == null) return;
            h.ShotClock -= dt;
            var range = Config.HuntShotRangeCells * Cell;
            var to = (h.Prey.Root.position - h.Root.position).WithY(0f);
            if (to.magnitude > range * 0.9f)
            {
                WalkTo(h, h.Prey.Root.position, HunterHurry, dt, range * 0.8f);
                return;
            }
            Face(h.Root, to, dt * 6f);
            if (h.ShotClock > 0f) return;
            h.ShotClock = Config.HuntShotSeconds;
            if (h.Animator != null) h.Animator.OnStruck();
            h.Loose = LooseDelay;
        }

        /// <summary>A step toward `goal` on the land; false once there (within `near`) or blocked.</summary>
        private static bool WalkTo(Hunter h, Vector3 goal, float speed, float dt, float near)
        {
            var here = h.Root.position;
            var to = (goal - here).WithY(0f);
            if (to.magnitude <= near) return false;
            var step = to.normalized * speed;
            var next = here + step * dt;
            if (!OnLand(next, out var y))
            {
                var turned = Quaternion.Euler(0f, 60f, 0f) * step;
                next = here + turned * dt;
                if (!OnLand(next, out y)) return false;
                step = turned;
            }
            h.Root.position = new Vector3(next.x, y, next.z);
            Face(h.Root, step, dt * 8f);
            return true;
        }

        private static Animal Nearest(Herd herd, Vector3 from)
        {
            Animal best = null;
            var bestD = float.MaxValue;
            foreach (var a in herd.Animals)
            {
                if (a.State == 3 || a.Root == null) continue;
                var d = (a.Root.position - from).sqrMagnitude;
                if (d < bestD) { bestD = d; best = a; }
            }
            return best;
        }

        private void Shoot(Hunter h)
        {
            var prey = h.Prey;
            if (prey == null || prey.Root == null || prey.State == 3) return;
            var hit = Random.value * 100f < Config.HuntHitPct((int)prey.Kind);
            var side = Quaternion.Euler(0f, Random.value < 0.5f ? 90f : -90f, 0f) * (prey.Root.position - h.Root.position).WithY(0f).normalized;
            CombatProjectiles.FireAt(ProjectileKind.Arrow, CombatProjectiles.MuzzleOfUnit(h.Root), prey.Root,
                hit ? Vector3.up * 0.18f : side * 0.6f + Vector3.up * 0.02f);
            _arrows.Add(new Arrow { From = h, Prey = prey, Hit = hit, Left = ArrowFlight });
        }

        private void TickArrows(float dt)
        {
            for (var i = _arrows.Count - 1; i >= 0; i--)
            {
                var arrow = _arrows[i];
                arrow.Left -= dt;
                if (arrow.Left > 0f) continue;
                _arrows.RemoveAt(i);
                var prey = arrow.Prey;
                if (prey == null || prey.Root == null || prey.State == 3) continue;
                if (arrow.Hit)
                {
                    Pay(prey);
                    Die(prey);
                }
                else if (prey.Kind == Kind.Boar && arrow.From != null && !arrow.From.Dead
                         && Random.value * 100f < Config.HuntBoarAttackPct)
                {
                    Charge(prey, arrow.From);
                }
            }
        }

        /// <summary>The kill's food and hides at once, into the stores nearest it, with their «+N» popups.</summary>
        private static void Pay(Animal prey)
        {
            var at = prey.Root.position;
            var resources = ResourceManager.Instance;
            if (resources == null) return;
            var food = resources.AddProduced(ResourceType.Food, Config.HuntFood((int)prey.Kind), at);
            var hides = resources.AddProduced(ResourceType.Hides, Config.HuntHides((int)prey.Kind), at);
            var popups = ResourceGainPopups.Instance;
            if (popups == null) return;
            popups.Show(at + Vector3.up * 1.1f, food, ResourceType.Food);
            popups.Show(at + Vector3.up * 0.7f, hides, ResourceType.Hides);
        }

        /// <summary>A boar sets on the hunter who missed it: a short charge, and the hunter is dead.</summary>
        private void Charge(Animal boar, Hunter h)
        {
            var to = (h.Root.position - boar.Root.position).WithY(0f);
            boar.State = 2;
            boar.Timer = Mathf.Clamp(to.magnitude / (BoarSpeed * 6f), 0.4f, 1.5f);
            boar.Velocity = to.normalized * BoarSpeed * 6f;
            KillHunter(h);
        }

        private void KillHunter(Hunter h)
        {
            h.Dead = true;
            var at = h.Root.position;
            h.Hut.LayOffWorker();
            var citizens = CitizenManager.Instance;
            if (citizens != null)
            {
                var died = citizens.KillCitizens(1, true);
                if (died > 0) Remains.Instance?.Drop(at);
            }
            EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_hunter_killed"), EventSeverity.Danger,
                EventGlyph.Skull, "HerdBoar", at));
            if (h.Root != null) Destroy(h.Root.gameObject);
        }

        // ---------------------------------------------------------------------------------------- save

        public struct SavedHerd
        {
            public int Kind, Size, Total, Alive, Island;
            public float X, Z, Life;
        }

        public void Capture(out float clock, List<SavedHerd> into)
        {
            clock = _clock;
            foreach (var h in _herds)
            {
                if (h.Leaving) continue;
                into.Add(new SavedHerd
                {
                    Kind = (int)h.Kind, Size = h.Size, Total = h.Total, Alive = h.Alive, Island = h.Island,
                    X = h.Centre.x, Z = h.Centre.z, Life = h.Life,
                });
            }
        }

        public void RestoreFromSave(float clock, List<SavedHerd> herds)
        {
            foreach (var h in _herds)
            {
                foreach (var a in h.Animals) if (a.Root != null) Destroy(a.Root.gameObject);
                if (h.Sign != null) Destroy(h.Sign.gameObject);
                if (h.Area != null) Destroy(h.Area.gameObject);
            }
            _herds.Clear();
            _clock = clock;
            _started = Config != null && clock >= Config.HuntFirstHerdSeconds;
            if (herds == null || GridManager.Instance == null || MeshMapApplier.Instance == null) return;
            foreach (var s in herds)
            {
                if (s.Alive <= 0) continue;
                var centre = new Vector3(s.X, GridManager.Instance.GroundHeight, s.Z);
                if (MeshMapApplier.Instance.TryGetGroundHeight(centre, out var y)) centre.y = y;
                var herd = new Herd
                {
                    Kind = (Kind)Mathf.Clamp(s.Kind, 0, 2), Size = s.Size, Total = s.Total, Island = s.Island, Centre = centre,
                    Radius = (Config != null ? Config.HuntHerdRadiusCells : 7f) * Cell, Life = s.Life,
                };
                Populate(herd, Mathf.Min(s.Alive, s.Total));
                _herds.Add(herd);
            }
        }
    }

    internal static class HuntVectorExtensions
    {
        public static Vector3 WithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);
    }
}
