using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.CameraControl;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// Campaign mission 3b «Клетка» (Design/features/campaign.md; the user's design 2026-11-23): the King escorts the cart
    /// with the caged monster along the forest road to the capital's gate. The cart and the column ride by themselves,
    /// slowly (the whole road m3_ride_sec at the plain pace); monsters come out of the forest at the cart, and their goal
    /// is the CAGE -- its health (only ever down) is the bar at the top of the screen; broken = lost. The riders guard it
    /// but cannot hold alone; the player runs the King (mission 2's hero controls, CityDefenceMode.Arden.cs) with two
    /// skills. The cart slows while monsters crowd it, stands at the felled tree (the block), crosses the river on the
    /// bridge (an ambush from the far bank), picks up riders at the watch-tower, meets the brute, and in the chase runs
    /// for the gate where archers shoot from the walls; reaching the gate wins.
    /// The map (Map-M3) and this data: Tools/campaign/mission3b_map.py, mission3b_town.py -> Campaign/Mission3Escort.json.
    /// Numbers: m3_* (BalanceConfig.Escort).
    /// </summary>
    public partial class CityDefenceMode
    {
        [SerializeField] private TextAsset escortPlan;   // Campaign/Mission3Escort.json
        // the opening scene (the forest ambush, approved 2026-11-23; Tools/blender/cinematic/export_m3opening.py, laid out by
        // Tools/campaign/mission3b_opening_json.py): its figures (CapM3*) and props are among the capital's Cap* lists
        [SerializeField] private TextAsset openingScript;                         // Campaign/Mission3Opening.json
        [SerializeField] private TextAsset[] openingTakes = new TextAsset[0];      // Campaign/M3Opening/*.json
        [SerializeField] private GameObject[] openingSets = new GameObject[0];    // Models/Cinematic/M3RoadSet.fbx
        [SerializeField] private AudioClip openingMusic;                          // the ride
        [SerializeField] private AudioClip openingMusic2;                         // from the horn

        /// <summary>«Заново»: the escort again without its opening scene.</summary>
        private static bool _skipEscortFilmOnce;

        [System.Serializable] private class EscortPlan
        {
            public int width, height;
            public float[] origin;
            public float[] road;
            public float roadLength;
            public List<EscortEvent> events = new List<EscortEvent>();
            public float[] gate;
            public float[] capital;
            public float[] post;
            public List<Piece> buildings = new List<Piece>();
            public List<RigPiece> rig = new List<RigPiece>();
            public float rigSpeed = 0.55f, wheelRadius = 0.176f;
            public float[] rigLoop;
            public KingRig king;
        }

        [System.Serializable] private class RigPiece { public string kind, name, take; public float[] pos, rot; }

        [System.Serializable] private class EscortEvent { public string id; public float minute, x, y, dx, dy; }

        private static EscortBalance E => BalanceConfig.Instance.Escort;

        /// <summary>The cage's health, 0..1 (the HUD's bar at the top).</summary>
        public float CageShare => _cageMax > 0f ? Mathf.Clamp01(_cage / _cageMax) : 0f;

        /// <summary>How far along the road the cart is, 0..1 (the HUD's strip under the bar).</summary>
        public float RouteShare => _roadLen > 0f ? Mathf.Clamp01(_cartS / _roadLen) : 0f;

        /// <summary>The cart stands at the felled tree.</summary>
        public bool CartBlocked => _blockLeft > 0f;

        /// <summary>Seconds since the escort set out (its clock: the King's levels, the waves).</summary>
        public float EscortSeconds => _escortClock;

        /// <summary>The cart's place (the HUD's arrows, the King's respawn).</summary>
        public Vector3 CartPosition => _cart != null ? _cart.position : Vector3.zero;

        /// <summary>A set piece began (the HUD's banner): "block", "bridge", "tower", "brute", "chase", "gate".</summary>
        public event System.Action<string> EscortBeat;

        private EscortPlan _escort;
        private Vector3[] _road;
        private float[] _roadS;
        private float _roadLen, _cartS, _cartSpeed, _cage, _cageMax, _escortClock, _blockLeft;
        private Transform _cart;
        private readonly Dictionary<string, float> _beatS = new Dictionary<string, float>();
        private readonly HashSet<string> _beatsDone = new HashSet<string>();
        private readonly List<SoldierUnit> _riders = new List<SoldierUnit>();
        private readonly Dictionary<SoldierUnit, ArmyGroup> _riderGroup = new Dictionary<SoldierUnit, ArmyGroup>();
        private readonly Dictionary<SoldierUnit, float> _followUntil = new Dictionary<SoldierUnit, float>();
        private readonly List<OrcUnit> _escortOrcs = new List<OrcUnit>();
        private readonly Dictionary<OrcUnit, float> _cageSwing = new Dictionary<OrcUnit, float>();
        private bool _chase;
        private float _lastCageHit = -10f;

        /// <summary>The cage was struck in the last moment (the HUD's red blink).</summary>
        public bool CageStruck => Time.time - _lastCageHit < 0.6f;

        // ------------------------------------------------------------------ setup

        private void SetupEscort()
        {
            _escort = escortPlan != null ? JsonUtility.FromJson<EscortPlan>(escortPlan.text) : null;
            if (_escort == null || _escort.road == null || _escort.road.Length < 4 || GridManager.Instance == null || buildingPlacer == null)
            {
                Debug.LogError("CityDefenceMode: no escort plan (Campaign/Mission3Escort.json) or no grid -- mission 3b cannot start.");
                return;
            }
            _origin = _escort.origin != null && _escort.origin.Length == 2 ? new Vector2(_escort.origin[0], _escort.origin[1]) : new Vector2(-60f, 43f);
            if (FogOfWarManager.Instance != null) FogOfWarManager.Instance.DisabledCheat = true;
            RaisePieces(_escort.buildings, SortEscortPiece);
            if (Castle != null) buildingPlacer.MarkMandatoryBuildingAlreadyPlaced();
            buildingPlacer.ClearSelection();
            Resources.ResourceManager.Instance?.SetInfiniteResources(true);
            LayRoad();
            OrcUnit.Died += OnOrcDied;
            OrcUnit.Wounded += OnOrcWounded;
            RTSCameraController.GesturesLocked = true;
            SetUpView();
            NoShadowsOnLow();

            var e = E;
            _cageMax = Mathf.Max(1, e.cageHealth);
            _cage = _cageMax;
            _cartS = 0f;
            _cartSpeed = _roadLen / Mathf.Max(30f, e.rideSeconds);
            _escortClock = 0f;
            _orcsTotal = MaxLevel - 1;   // one point a level: XpAt(level) = level - 1 (the clock gives them, TickArdenXp)
            RaiseCart();
            for (var i = 0; i < e.riders; i++) HireRider(SlotAt(i, e.riders));
            FitCart(_riders.Count > 0 ? _riders[0] : null);

            gameObject.AddComponent<CityDefenceHud>().Bind(this);

            Current = Stage.Part1;
            Wave = 0;
            _nextWave = e.firstWaveSeconds;
            RaiseArden(SlotAt(0, 1) + RoadSide(_cartS) * 1.5f * Cell);
            StartSound();
            // the opening scene first (the game stands under it), then the banner; «Заново» goes straight to the road
            var skip = _skipEscortFilmOnce;
            _skipEscortFilmOnce = false;
            if (skip || !StartEscortFilm()) PartBegan?.Invoke(1);
        }

        private bool StartEscortFilm()
        {
            try
            {
                return StartFilm(openingScript, openingTakes, capitalFigures, capitalClips, capitalProps, openingSets,
                    openingMusic, openingMusic2, null, false, () => PartBegan?.Invoke(1));
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                AbortCapital();
                return false;
            }
        }

        private void SortEscortPiece(Piece piece, BuildingInstance building)
        {
            var data = building.Data;
            if (data.buildingName == BuildingIds.TownHall) Castle = building;
            // the capital's walls are scenery: no monster breaks them, no bar over them
            building.HideHealthBar = true;
        }

        /// <summary>The road in the world, and where each set piece lies along it.</summary>
        private void LayRoad()
        {
            var n = _escort.road.Length / 2;
            _road = new Vector3[n];
            _roadS = new float[n];
            for (var i = 0; i < n; i++)
            {
                _road[i] = Ground(World(_escort.road[2 * i], _escort.road[2 * i + 1]));
                _roadS[i] = i == 0 ? 0f : _roadS[i - 1] + Flat(_road[i] - _road[i - 1]);
            }
            _roadLen = _roadS[n - 1];
            foreach (var ev in _escort.events)
            {
                if (ev == null || string.IsNullOrEmpty(ev.id)) continue;
                var at = World(ev.x, ev.y);
                var best = 0;
                var bestD = float.MaxValue;
                for (var i = 0; i < n; i++)
                {
                    var d = (Flat(_road[i] - at));
                    if (d < bestD) { bestD = d; best = i; }
                }
                _beatS[ev.id] = _roadS[best];
            }
        }

        /// <summary>Where the King may ride: the plan's land, a cell in from its edge (the sea beyond).</summary>
        private Rect EscortArea()
        {
            var w = _escort != null ? _escort.width : 120;
            var h = _escort != null ? _escort.height : 86;
            var a = World(1f, h - 1f);
            var c = World(w - 1f, 1f);
            return Rect.MinMaxRect(Mathf.Min(a.x, c.x), Mathf.Min(a.z, c.z), Mathf.Max(a.x, c.x), Mathf.Max(a.z, c.z));
        }

        private static Vector3 Ground(Vector3 at)
        {
            if (NavMesh.SamplePosition(at, out var hit, 3f, NavMesh.AllAreas)) return hit.position;
            return at;
        }

        /// <summary>A point `s` metres along the road.</summary>
        private Vector3 RoadAt(float s)
        {
            if (_road == null || _road.Length == 0) return Vector3.zero;
            if (s <= 0f) return _road[0];
            if (s >= _roadLen) return _road[_road.Length - 1];
            var lo = 0;
            var hi = _roadS.Length - 1;
            while (hi - lo > 1)
            {
                var mid = (lo + hi) / 2;
                if (_roadS[mid] <= s) lo = mid; else hi = mid;
            }
            var span = _roadS[hi] - _roadS[lo];
            return Vector3.Lerp(_road[lo], _road[hi], span > 0.0001f ? (s - _roadS[lo]) / span : 0f);
        }

        /// <summary>The road's way (flat, unit) at `s`.</summary>
        private Vector3 RoadDir(float s)
        {
            var d = RoadAt(s + 1.5f * Cell) - RoadAt(s - 1.5f * Cell);
            d.y = 0f;
            return d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.forward;
        }

        /// <summary>The road's left hand at `s`.</summary>
        private Vector3 RoadSide(float s)
        {
            var d = RoadDir(s);
            return new Vector3(-d.z, 0f, d.x);
        }

        // ------------------------------------------------------------------ the cart

        // the cart as the opening scene has it (the user's playtest 2026-11-23: «клетка маленькая, повёрнута не в ту
        // сторону, лошадь тоже и их должно быть две как в сцене, внутри клетки нет орка ... на повозке должен сидеть
        // солдат»): the scene's own models laid out by Mission3Escort.json `rig` (mission3b_town.py from the column take)
        private Transform _cartModel;                    // the rig, turned and scaled under _cart
        private readonly List<(Transform tr, Quaternion rest)> _wheels = new List<(Transform, Quaternion)>();
        private readonly List<CineFigure> _cartFigs = new List<CineFigure>();
        private readonly List<string> _cartTakes = new List<string>();
        private float _cartSpeedNow, _wheelRoll, _rigScale = 1f;
        private Transform _cartHorse;                    // its length sizes the rig to the riders' horses

        private void RaiseCart()
        {
            var root = new GameObject("CageCart").transform;
            root.SetParent(transform, false);
            _cart = root;
            _cartModel = new GameObject("Rig").transform;
            _cartModel.SetParent(root, false);
            var any = false;
            if (_escort.rig != null)
                foreach (var piece in _escort.rig)
                {
                    if (piece == null || piece.pos == null || piece.pos.Length < 3 || piece.rot == null || piece.rot.Length < 4) continue;
                    var at = new Vector3(piece.pos[0], piece.pos[1], piece.pos[2]);
                    var turn = new Quaternion(piece.rot[0], piece.rot[1], piece.rot[2], piece.rot[3]);
                    if (piece.kind == "figure")
                    {
                        var i = System.Array.FindIndex(capitalFigures, m => m != null && m.name == piece.name);
                        if (i < 0 || i >= capitalClips.Length || capitalClips[i] == null) continue;
                        var go = Instantiate(capitalFigures[i], _cartModel, false);
                        go.name = piece.name;
                        go.transform.localPosition = at;
                        go.transform.localRotation = turn;
                        var fig = go.AddComponent<CineFigure>();
                        fig.Init(capitalClips[i]);
                        var loop = _escort.rigLoop != null && _escort.rigLoop.Length == 2 ? _escort.rigLoop[0] : 0f;
                        fig.Play(piece.take, 0f, 0f, loop);
                        _cartFigs.Add(fig);
                        _cartTakes.Add(piece.take);
                        if (piece.name == "CapM3CartHorse" && _cartHorse == null) _cartHorse = go.transform;
                        any = true;
                        continue;
                    }
                    var model = CapitalProp(piece.name);
                    if (model == null) continue;
                    var p = Instantiate(model, _cartModel, false);
                    p.name = piece.name;
                    p.transform.localPosition = at;
                    p.transform.localRotation = turn;
                    p.transform.localScale = model.transform.localScale;
                    CineTwoSided.Apply(p);
                    if (piece.kind == "wheel") _wheels.Add((p.transform, turn));
                    any = true;
                }
            if (!any)
            {
                // the models missing: a crate stands in, so the mission still plays
                var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(box.GetComponent<Collider>());
                box.transform.SetParent(_cartModel, false);
                box.transform.localScale = new Vector3(1f, 1f, 1.6f) * Cell;
                box.transform.localPosition = Vector3.up * 0.5f * Cell;
            }
            PlaceCart();
        }

        /// <summary>The rig as big as the column round it: its shaft horse as long as a rider's horse (as in the scene,
        /// where both are the same horse), times m3_cart_scale.</summary>
        private void FitCart(SoldierUnit rider)
        {
            if (_cartModel == null) return;
            var k = 1f;
            if (_cartHorse != null && rider != null)
            {
                var horse = Extent(_cartHorse, _cartHorse);
                var mount = Extent(rider.transform, rider.transform);
                var a = Mathf.Max(horse.x, horse.z);
                var b = Mathf.Max(mount.x, mount.z);
                if (a > 0.01f && b > 0.01f) k = b / a;
            }
            _rigScale = Mathf.Clamp(k, 0.2f, 5f) * Mathf.Max(0.1f, E.cartScale);
            _cartModel.localScale = Vector3.one * _rigScale;
        }

        private GameObject CapitalProp(string name)
        {
            foreach (var p in capitalProps)
                if (p != null && p.name == name) return p;
            return null;
        }

        /// <summary>A model's size in a frame (x across, z along).</summary>
        private static Vector3 Extent(Transform model, Transform frame)
        {
            var lo = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var hi = -lo;
            var any = false;
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled) continue;
                var b = r.bounds;
                for (var i = 0; i < 8; i++)
                {
                    var corner = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                    var local = frame.InverseTransformPoint(corner);
                    lo = Vector3.Min(lo, local);
                    hi = Vector3.Max(hi, local);
                    any = true;
                }
            }
            return any ? Vector3.Scale(hi - lo, frame.lossyScale) : Vector3.zero;
        }

        private void PlaceCart()
        {
            if (_cart == null) return;
            var at = RoadAt(_cartS);
            _cart.position = at;
            var dir = RoadDir(_cartS);
            var want = Quaternion.LookRotation(dir, Vector3.up);
            _cart.rotation = Quaternion.RotateTowards(_cart.rotation, want, 90f * Time.deltaTime + (_cartS <= 0f ? 360f : 0f));
        }

        /// <summary>The wheels roll with the way gone, the horses walk at the cart's pace, the driver and the monster
        /// loop their part of the scene's take.</summary>
        private void TickCartRig(float dt, float moved)
        {
            var radius = Mathf.Max(0.01f, _escort.wheelRadius * _rigScale);
            _wheelRoll += moved / radius * Mathf.Rad2Deg;
            foreach (var (tr, rest) in _wheels)
                if (tr != null) tr.localRotation = rest * Quaternion.Euler(-_wheelRoll, 0f, 0f);
            var walk = Mathf.Max(0.01f, _escort.rigSpeed * _rigScale);
            var rate = Mathf.Clamp(_cartSpeedNow / walk, 0f, 2f);
            var loop = _escort.rigLoop != null && _escort.rigLoop.Length == 2 ? _escort.rigLoop : new[] { 0f, 1e9f };
            for (var i = 0; i < _cartFigs.Count; i++)
            {
                var fig = _cartFigs[i];
                if (fig == null) continue;
                fig.Play(_cartTakes[i], 0f, rate);          // the same clip again only takes the new rate
                if (fig.ClipTime >= loop[1]) fig.SetTime(loop[0] + (fig.ClipTime - loop[1]));
                fig.Tick(dt);
            }
        }

        /// <summary>Monsters crowding the cart (they slow it).</summary>
        private int CrowdAtCart()
        {
            var r = E.slowCells * Cell;
            var at = CartPosition;
            var n = 0;
            foreach (var orc in _escortOrcs)
                if (orc != null && orc.CurrentHealth > 0 && Flat(orc.transform.position - at) <= r) n++;
            return n;
        }

        private void TickCart(float dt)
        {
            var e = E;
            if (_blockLeft > 0f)
            {
                _blockLeft -= dt;
                _cartSpeedNow = 0f;
                PlaceCart();
                TickCartRig(dt, 0f);
                return;
            }
            var speed = _cartSpeed * Mathf.Max(e.minSpeed, 1f - e.slowPerOrc * CrowdAtCart());
            if (_chase) speed *= e.chaseSpeedMult;
            var was = _cartS;
            _cartS = Mathf.Min(_roadLen, _cartS + speed * dt);
            // the felled tree: the cart stands until the riders clear it
            if (_beatS.TryGetValue("block", out var block) && was < block && _cartS >= block && !_beatsDone.Contains("block"))
            {
                _cartS = block;
                _blockLeft = e.blockSeconds;
            }
            _cartSpeedNow = dt > 0f ? (_cartS - was) / dt : 0f;
            PlaceCart();
            TickCartRig(dt, _cartS - was);
        }

        // ------------------------------------------------------------------ the column

        /// <summary>A rider's place round the cart: pairs down both sides, the last ones ahead and behind.</summary>
        private Vector3 SlotAt(int i, int count)
        {
            var c = Cell;
            var side = RoadSide(_cartS);
            var dir = RoadDir(_cartS);
            var pair = i / 2;
            var left = i % 2 == 0 ? 1f : -1f;
            var rows = Mathf.Max(1, (count + 1) / 2);
            var along = rows > 1 ? Mathf.Lerp(2.2f, -2.2f, pair / (float)(rows - 1)) : 0f;
            var at = CartPosition + dir * along * c + side * left * 1.3f * c;
            return at;
        }

        private SoldierUnit HireRider(Vector3 at)
        {
            var army = ArmyManager.Instance;
            if (army == null) return null;
            at = Ground(at);
            var group = new ArmyGroup(SoldierType.Knight, at);
            var unit = army.SpawnHireling(SoldierType.Knight, at, group);
            if (unit == null) return null;
            var stats = SoldierStats.StatsAt(SoldierType.Knight, E.riderLevel);
            unit.SetHeroStats(stats.maxHealth, stats.attackDamage);
            unit.SetCurrentHealth(stats.maxHealth);
            unit.name = "Rider";
            _riders.Add(unit);
            _riderGroup[unit] = group;
            _garrison.Add(unit);       // «За мной!» enrages the garrison near the King
            return unit;
        }

        /// <summary>«За мной!» (the user's design): the riders near the King ride with him for a while.</summary>
        private void CallRiders(Vector3 at, float radius)
        {
            var until = Time.time + E.followSeconds;
            foreach (var r in _riders)
                if (r != null && r.CurrentHealth > 0 && Flat(r.transform.position - at) <= radius) _followUntil[r] = until;
        }

        /// <summary>
        /// The riders ride WITH the cart (the user's playtest 2026-11-23: «кавалерия двигается рывками, она должна идти с
        /// такой же скоростью как клетка»): each is steered every frame at the cart's own pace plus a little to close on
        /// its place; with a monster in its reach (or already fighting) it is let go to fight; called by «За мной!» it
        /// rides by the King instead.
        /// </summary>
        private void TickRiders()
        {
            _riders.RemoveAll(r => r == null || r.CurrentHealth <= 0);
            var count = Mathf.Max(1, _riders.Count);
            var fightR = E.riderFightCells * Cell;
            for (var i = 0; i < _riders.Count; i++)
            {
                var r = _riders[i];
                if (r.IsFighting || MonsterNear(r.transform.position, fightR))
                {
                    if (r.IsSteered) r.Steer = Vector3.zero;
                    continue;
                }
                var following = Arden != null && Arden.CurrentHealth > 0 && _followUntil.TryGetValue(r, out var until) && Time.time < until;
                var slot = following ? Arden.transform.position + (r.transform.position - Arden.transform.position).normalized * 1.2f * Cell : SlotAt(i, count);
                var to = slot - r.transform.position;
                to.y = 0f;
                var gap = to.magnitude;
                var walk = Mathf.Max(0.1f, SoldierStats.StatsAt(SoldierType.Knight, E.riderLevel).walkSpeed);
                // the cart's pace along the road, and the gap closed over about a second
                var pace = following ? Vector3.zero : RoadDir(_cartS) * _cartSpeedNow;
                var want = pace + (gap > 0.05f * Cell ? to : Vector3.zero);
                var speed = want.magnitude;
                if (speed < 0.02f)
                {
                    if (r.IsSteered) r.Steer = Vector3.zero;
                    continue;
                }
                r.Steer = want / speed * Mathf.Clamp01(speed / walk);
            }
        }

        private bool MonsterNear(Vector3 at, float radius)
        {
            foreach (var orc in _escortOrcs)
                if (orc != null && orc.CurrentHealth > 0 && Flat(orc.transform.position - at) <= radius) return true;
            return false;
        }

        // ------------------------------------------------------------------ the monsters

        private void TickEscortWaves(float dt)
        {
            var e = E;
            _nextWave -= dt;
            if (_nextWave > 0f) return;
            _nextWave = _chase ? e.chaseWaveGapSeconds : e.waveGapSeconds;
            Wave++;
            RoarForWave();
            var count = e.waveBase + e.waveGrowth * (Wave - 1);
            var throwers = _escortClock >= e.throwersFromSeconds ? e.throwersPerWave : 0;
            // a side each wave, both sides past the brute, from behind in the chase
            if (_chase)
            {
                Ambush(count, throwers, ahead: -e.aheadCells * 1.5f, side: 0);
                return;
            }
            var both = _beatsDone.Contains("brute");
            var side = Wave % 2 == 0 ? 1 : -1;
            if (both)
            {
                Ambush(count / 2 + count % 2, throwers, e.aheadCells, side);
                Ambush(count / 2, 0, e.aheadCells * 0.5f, -side);
            }
            else Ambush(count, throwers, e.aheadCells, side);
        }

        /// <summary>
        /// A band out of the forest: `count` spears and `throwers` throwers `ahead` cells along the road from the cart
        /// (negative = behind), on the road's `side` (1 left, -1 right, 0 on the road itself), m3_spawn_cells off it.
        /// </summary>
        private void Ambush(int count, int throwers, float ahead, int side, OrcKind? lead = null)
        {
            var s = Mathf.Clamp(_cartS + ahead * Cell, 0f, _roadLen);
            var at = RoadAt(s) + RoadSide(s) * side * E.spawnCells * Cell;
            if (lead.HasValue) SpawnEscortOrc(at, lead.Value);
            for (var i = 0; i < count; i++) SpawnEscortOrc(at, OrcKind.Raider);
            for (var i = 0; i < throwers; i++) SpawnEscortOrc(at, OrcKind.Thrower);
        }

        private OrcUnit SpawnEscortOrc(Vector3 near, OrcKind kind)
        {
            var raids = OrcRaidManager.Instance;
            if (raids == null) return null;
            var e = E;
            var at = Ground(near + new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f)) * Cell);
            var orc = raids.SpawnOrcOfKind(at, e.orcLevel, kind);
            if (orc == null) return null;
            orc.Toughen(e.orcHealthMult * (kind == OrcKind.Brute ? e.bruteHealthMult : 1f), e.orcDamageMult);
            orc.Steered = true;
            _escortOrcs.Add(orc);
            return orc;
        }

        private int CageDamageOf(OrcUnit orc)
        {
            var e = E;
            return orc.Kind == OrcKind.Brute ? e.cageDamageBrute : orc.IsRanged ? e.cageDamageThrower : e.cageDamageRaider;
        }

        /// <summary>Every monster goes for the cage: a spear or a torch fights a rider or the King in its way, a thrower
        /// keeps to its range, the brute walks through them all to the cage.</summary>
        private void TickEscortOrcs(float dt)
        {
            _escortOrcs.RemoveAll(o => o == null || o.CurrentHealth <= 0);
            var cart = CartPosition;
            var e = E;
            var reach = (e.cageReachCells + 0.6f) * Cell;
            for (var i = 0; i < _escortOrcs.Count; i++)
            {
                var orc = _escortOrcs[i];
                if (orc.BusyFor > 0f) continue;   // stunned or knocked
                var brute = orc.Kind == OrcKind.Brute;
                if (!brute && !orc.IsRanged && orc.DefendSelf()) continue;
                var to = cart - orc.transform.position;
                to.y = 0f;
                var range = orc.IsRanged ? e.throwReachCells * Cell : reach;
                if (to.magnitude > range)
                {
                    // to its own spot round the cart, so a band does not stack on one point
                    var spot = cart - to.normalized * (orc.IsRanged ? range * 0.8f : e.cageReachCells * Cell * 0.8f);
                    orc.SteerTo(spot);
                    continue;
                }
                _cageSwing.TryGetValue(orc, out var swing);
                swing -= dt;
                if (swing <= 0f)
                {
                    swing = e.cageHitSeconds * Random.Range(0.85f, 1.15f);
                    orc.ShowStrike(cart + Vector3.up * 0.5f * Cell);
                    HitCage(CageDamageOf(orc));
                }
                _cageSwing[orc] = swing;
            }
        }

        private void HitCage(int damage)
        {
            if (damage <= 0 || Current != Stage.Part1) return;
            _cage = Mathf.Max(0f, _cage - damage);
            _lastCageHit = Time.time;
            if (_cage <= 0f) Lose(LossReason.Cage);
        }

        // ------------------------------------------------------------------ the set pieces

        private void TickBeats()
        {
            var e = E;
            Beat("block", () => Ambush(e.blockWave, 0, 3f, Wave % 2 == 0 ? 1 : -1));
            // the ambush from the far bank: they wait over the river and come at the cart on the bridge
            Beat("bridge", () => Ambush(e.bridgeWave, e.throwersPerWave, 6f, 0));
            Beat("tower", () =>
            {
                var post = _escort.post != null && _escort.post.Length == 2 ? World(_escort.post[0], _escort.post[1]) : CartPosition;
                for (var i = 0; i < e.postRiders; i++) HireRider(post + new Vector3(i - e.postRiders * 0.5f, 0f, 0f) * Cell);
            });
            Beat("brute", () =>
            {
                for (var i = 0; i < e.brutes; i++) Ambush(2, 0, e.aheadCells * 0.5f, i % 2 == 0 ? 1 : -1, OrcKind.Brute);
            });
            Beat("chase", () =>
            {
                _chase = true;
                _nextWave = 2f;
                for (var i = 0; i < e.chaseBrutes; i++) Ambush(3, 0, -e.aheadCells * 1.5f, 0, OrcKind.Brute);
                ManTheWalls();
            });
        }

        private void Beat(string id, System.Action act)
        {
            if (_beatsDone.Contains(id) || !_beatS.TryGetValue(id, out var s)) return;
            // the set piece a little before the cart gets there, so its monsters are on their way
            var lead = id == "tower" || id == "block" ? 0f : E.aheadCells * Cell;
            if (_cartS + lead < s) return;
            _beatsDone.Add(id);
            act();
            EscortBeat?.Invoke(id);
        }

        /// <summary>The capital's archers up on its walls by the gate (the chase's end).</summary>
        private void ManTheWalls()
        {
            var army = ArmyManager.Instance;
            var grid = GridManager.Instance;
            if (army == null || grid == null || _escort.gate == null || _escort.gate.Length < 2) return;
            var gx = _escort.gate[0];
            var gy = _escort.gate[1];
            for (var i = 0; i < E.wallArchers; i++)
            {
                var dx = (i / 2 + 2) * (i % 2 == 0 ? 1 : -1);
                var cell = grid.WorldToCell(World(gx + dx + 0.5f, gy + 0.5f));
                var inside = World(gx + dx + 0.5f, gy - 2.5f);
                var group = new ArmyGroup(SoldierType.Archer, inside);
                var archer = army.SpawnHireling(SoldierType.Archer, Ground(inside), group);
                if (archer == null) continue;
                archer.GoToWall(cell);
            }
        }

        /// <summary>Seconds left to the gate at the plain pace (the clock on the minimap's plaque).</summary>
        public float EscortSecondsLeft => _cartSpeed > 0f ? Mathf.Max(0f, (_roadLen - _cartS) / _cartSpeed) : 0f;

        /// <summary>Cheat: the cart a minute further on (its set pieces fire as it passes them).</summary>
        private void CheatEscortAhead()
        {
            _blockLeft = 0f;
            _cartS = Mathf.Min(_roadLen - 0.1f, _cartS + _cartSpeed * 60f);
            _escortClock += 60f;
        }

        /// <summary>The minimap's «!» in mission 3b: lane 0 the monster nearest the cart, lane 1 a brute.</summary>
        private bool TryEscortFront(int lane, out Vector3 front)
        {
            front = Vector3.zero;
            var best = float.MaxValue;
            var cart = CartPosition;
            foreach (var orc in _escortOrcs)
            {
                if (orc == null || orc.CurrentHealth <= 0) continue;
                if (lane == 1 && orc.Kind != OrcKind.Brute) continue;
                if (lane > 1) break;
                var d = (orc.transform.position - cart).sqrMagnitude;
                if (d >= best) continue;
                best = d;
                front = orc.transform.position;
            }
            return best < float.MaxValue;
        }

        // ------------------------------------------------------------------ the tick

        private void TickEscort(float dt)
        {
            if (Current != Stage.Part1) return;
            _escortClock += dt;
            TickArden(dt);
            TickFx(dt);
            TickArdenXp();
            TickStuns();
            TickCart(dt);
            TickRiders();
            TickEscortOrcs(dt);
            TickBeats();
            TickEscortWaves(dt);
            if (Current != Stage.Part1) return;
            if (_cartS >= _roadLen - 0.05f)
            {
                EscortBeat?.Invoke("gate");
                Win();
            }
        }
    }
}
