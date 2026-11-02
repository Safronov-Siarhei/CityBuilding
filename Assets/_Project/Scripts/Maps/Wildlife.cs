using System.Collections.Generic;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Grid;
using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>
    /// The main map's harmless wildlife (the user, 2026-11-02: «карта безжизненная»; models
    /// approved the same day, Tools/blender/fauna.py). Nothing here touches the game: no resources,
    /// no fights, no saves -- only life.
    ///
    /// Every animal has its own place on the map, laid out once when the map stands (the user,
    /// 2026-11-02: they must not vanish and turn up elsewhere when the camera moves), from the map's
    /// own layout so the same map gets the same animals:
    /// - deer (in twos) and hares keep to a home on open land and walk a loop of waypoints round it,
    ///   grazing at each, and bolt from anyone who comes near (a citizen, a soldier, an orc), then
    ///   go back to their round;
    /// - gulls circle their own stretch of coast, and one more over each ship;
    /// - fish leap now and then at their spots by a shore, a pod of dolphins arcs through its stretch
    ///   of open water, and far out a whale surfaces, glides and dives with its flukes up;
    /// - and when the camera comes to a forest, a flock of songbirds may rise out of a tree.
    ///
    /// Mobile first: an animal away from the view or on unrevealed ground is switched off whole
    /// (no renderer, no animator, no thinking) and picks up where it was when it is back in view;
    /// its model is made the first time it is seen. Not in a dungeon or the Invasion
    /// (MeshMapApplier starts it). The archipelago's fishing shoals have their own fish.
    /// </summary>
    public class Wildlife : MonoBehaviour
    {
        public enum Kind { Gull, Bird, Deer, Rabbit, Fish, Dolphin, Whale }

        private const float FleeRadius = 3.5f;
        private const float StartDelay = 2f;
        // how far round the view an animal is kept on, in view reaches
        private const float KeepOn = 1.5f;
        // the layout's sampling stride, cells
        private const int Stride = 3;

        private sealed class Animal
        {
            public Kind Kind;
            public Transform Root;
            public Transform Tail;
            public Quaternion TailRest;
            public bool On;
            // the place: a land animal's home and its round, a gull's circle, a sea spot
            public Vector3 Home;
            public Vector3[] Round;
            public int Next;
            public Vector3 Offset;
            public Transform Follow;
            // land: 0 graze, 1 walk, 2 flee; sea: 0 under, 1 showing; a bird: in flight
            public int State;
            public float Timer;
            public float Age;
            public Vector3 Velocity;
            public Vector3 Heading;
            public float Radius, Speed, Phase, Height, Show;
        }

        private GameObject[] _models;
        private readonly List<Animal> _animals = new List<Animal>();
        private readonly List<Animal> _birds = new List<Animal>();
        private readonly Stack<Animal> _idleBirds = new Stack<Animal>();
        private readonly List<Vector3> _threats = new List<Vector3>();
        private Camera _camera;
        private Vector3 _focus;
        private float _reach;
        private float _clock;
        private bool _laidOut;
        private float _sea;
        private bool _hasSea;
        // the sea animals' pauses after their first (a showing's length is the place's, its timing need not be)
        private readonly System.Random _rhythm = new System.Random(5);
        private float _visibilityClock, _threatClock, _flockClock, _shipClock;

        /// <summary>Starts the wildlife under `parent`; `models` in Kind order. Null if a model is missing.</summary>
        public static Wildlife Begin(Transform parent, GameObject[] models)
        {
            if (models == null || models.Length < 7) return null;
            foreach (var model in models) if (model == null) return null;
            var go = new GameObject("Wildlife");
            go.transform.SetParent(parent, false);
            var life = go.AddComponent<Wildlife>();
            life._models = models;
            life._flockClock = Random.Range(6f, 14f);
            return life;
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            if (dt <= 0f) return;
            _clock += dt;
            if (_clock < StartDelay || !Look()) return;
            if (!_laidOut) { _laidOut = true; LayOut(); }

            if ((_visibilityClock -= dt) <= 0f) { _visibilityClock = 0.5f; SwitchByView(); }
            if ((_threatClock -= dt) <= 0f) { _threatClock = 1f; GatherThreats(); }
            if ((_shipClock -= dt) <= 0f) { _shipClock = 3f; GullsOverShips(); }
            if ((_flockClock -= dt) <= 0f) { _flockClock = Random.Range(15f, 30f); SpawnFlock(); }

            foreach (var a in _animals)
            {
                if (!a.On) continue;
                a.Age += dt;
                switch (a.Kind)
                {
                    case Kind.Deer:
                    case Kind.Rabbit: MoveLand(a, dt); break;
                    case Kind.Gull: MoveGull(a, dt); break;
                    default: MoveSea(a, dt); break;
                }
            }
            for (var i = _birds.Count - 1; i >= 0; i--)
            {
                var b = _birds[i];
                b.Age += dt;
                if (MoveBird(b, dt)) continue;
                b.Root.gameObject.SetActive(false);
                _birds.RemoveAt(i);
                _idleBirds.Push(b);
            }
        }

        // ------------------------------------------------------------------ where the player looks

        /// <summary>The ground point under the middle of the view, and how far round it life is kept.</summary>
        private bool Look()
        {
            if (_camera == null) _camera = Camera.main;
            var grid = GridManager.Instance;
            if (_camera == null || grid == null) return false;
            var ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            var plane = new Plane(Vector3.up, new Vector3(0f, grid.GroundHeight, 0f));
            if (!plane.Raycast(ray, out var along)) return false;
            _focus = ray.GetPoint(along);
            _reach = Mathf.Clamp((_camera.transform.position.y - grid.GroundHeight) * 1.3f, 10f, 28f);
            return true;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private bool InView(Vector3 p) => Flat(p - _focus).sqrMagnitude < _reach * _reach * KeepOn * KeepOn;

        private static bool Revealed(Vector2Int cell)
        {
            var fog = FogOfWarManager.Instance;
            return fog == null || fog.IsCellRevealed(cell);
        }

        private static Vector2Int CellOf(Vector3 p) => GridManager.Instance.WorldToCell(p);

        private static bool InBounds(Vector2Int cell) => GridManager.Instance.IsWithinBounds(cell, Vector2Int.one);

        /// <summary>Open, flat land at `p` with nothing built on it, and its height.</summary>
        private static bool OnLand(Vector3 p, out float y)
        {
            y = 0f;
            var map = MeshMapApplier.Instance;
            var cell = CellOf(p);
            if (map == null || !InBounds(cell) || map.IsWaterCell(cell)) return false;
            if (!map.TryGetGroundHeight(p, out y)) return false;
            return Mathf.Abs(y - GridManager.Instance.GroundHeight) < 0.6f && GridManager.Instance.IsAreaFree(cell, Vector2Int.one);
        }

        private static bool Water(Vector3 p)
        {
            var map = MeshMapApplier.Instance;
            var cell = CellOf(p);
            return map != null && InBounds(cell) && map.IsWaterCell(cell);
        }

        /// <summary>Water all round `p` out to `cells` cells (open sea), checked on a ring of eight.</summary>
        private static bool OpenWater(Vector3 p, int cells)
        {
            if (!Water(p)) return false;
            var size = GridManager.Instance.CellSize * cells;
            for (var k = 0; k < 8; k++)
            {
                var a = k * Mathf.PI / 4f;
                if (!Water(p + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * size)) return false;
            }
            return true;
        }

        /// <summary>Water with land within `cells` cells: a coast.</summary>
        private static bool Coast(Vector3 p, int cells)
        {
            if (!Water(p)) return false;
            var map = MeshMapApplier.Instance;
            var size = GridManager.Instance.CellSize * cells;
            for (var k = 0; k < 8; k++)
            {
                var a = k * Mathf.PI / 4f;
                var cell = CellOf(p + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * size);
                if (InBounds(cell) && !map.IsWaterCell(cell)) return true;
            }
            return false;
        }

        /// <summary>The sea's surface: the archipelago's sea level, else the top of the map's drawn water.</summary>
        private static bool WaterLevel(out float y)
        {
            var ships = Ships.ShipManager.Instance;
            if (ships != null && ships.HasSea) { y = Ships.ShipManager.WaterLevel; return true; }
            var water = MeshMapApplier.Instance != null ? MeshMapApplier.Instance.WaterInstance : null;
            var renderer = water != null ? water.GetComponentInChildren<Renderer>() : null;
            y = renderer != null ? renderer.bounds.max.y : 0f;
            return renderer != null;
        }

        // ------------------------------------------------------------------ the layout, once

        /// <summary>
        /// Walks the map once on a coarse stride, sorts what it finds into open land, coast and open
        /// water, and settles the animals there -- as many as the map has room for, spaced apart,
        /// seeded by the map's own layout so a map always has the same animals in the same places.
        /// </summary>
        private void LayOut()
        {
            var grid = GridManager.Instance;
            var map = MeshMapApplier.Instance;
            if (grid == null || map == null) return;
            _hasSea = WaterLevel(out _sea);
            var size = grid.GridSize;
            var land = new List<Vector3>();
            var coast = new List<Vector3>();
            var open = new List<Vector3>();
            var deep = new List<Vector3>();
            var seed = 17;
            for (var z = Stride / 2; z < size.y; z += Stride)
            for (var x = Stride / 2; x < size.x; x += Stride)
            {
                var cell = new Vector2Int(x, z);
                var p = grid.CellToWorld(cell);
                if (map.IsWaterCell(cell))
                {
                    seed = seed * 31 + x * 7 + z;
                    if (!_hasSea) continue;
                    if (Coast(p, 4)) coast.Add(p);
                    else if (OpenWater(p, 8)) deep.Add(p);
                    else if (OpenWater(p, 5)) open.Add(p);
                }
                else if (OnLand(p, out var y)) land.Add(new Vector3(p.x, y, p.z));
            }
            var rng = new System.Random(seed ^ map.CurrentMapId.Length * 7919);

            foreach (var home in Spread(land, Mathf.Clamp(land.Count / 220, 2, 24), 14f, rng))
                Herd(Kind.Deer, home, 2, rng);
            foreach (var home in Spread(land, Mathf.Clamp(land.Count / 120, 3, 40), 8f, rng))
                Herd(Kind.Rabbit, home, 1, rng);
            foreach (var spot in Spread(coast, Mathf.Clamp(coast.Count / 120, 2, 20), 18f, rng))
                Gull(spot, null, rng);
            foreach (var spot in Spread(coast, Mathf.Clamp(coast.Count / 80, 2, 24), 10f, rng))
                Pod(Kind.Fish, spot, 1, rng);
            foreach (var spot in Spread(open, Mathf.Clamp(open.Count / 300, 0, 6), 30f, rng))
                Pod(Kind.Dolphin, spot, 2 + rng.Next(2), rng);
            foreach (var spot in Spread(deep, Mathf.Clamp(deep.Count / 600, 0, 2), 60f, rng))
                Pod(Kind.Whale, spot, 1, rng);
        }

        /// <summary>Up to `count` of `points`, picked at random but no two within `apart` metres.</summary>
        private static List<Vector3> Spread(List<Vector3> points, int count, float apart, System.Random rng)
        {
            var chosen = new List<Vector3>();
            for (var tries = 0; tries < count * 12 && chosen.Count < count && points.Count > 0; tries++)
            {
                var p = points[rng.Next(points.Count)];
                var near = false;
                foreach (var c in chosen) if (Flat(c - p).sqrMagnitude < apart * apart) { near = true; break; }
                if (!near) chosen.Add(p);
            }
            return chosen;
        }

        private static float Range(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);

        /// <summary>`count` animals sharing a home and its round: four waypoints on open land round it.</summary>
        private void Herd(Kind kind, Vector3 home, int count, System.Random rng)
        {
            var round = new List<Vector3> { home };
            for (var k = 0; k < 12 && round.Count < 5; k++)
            {
                var a = (k / 12f + Range(rng, -0.04f, 0.04f)) * Mathf.PI * 2f;
                var r = Range(rng, 2.5f, kind == Kind.Deer ? 6f : 4f);
                var p = home + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                if (OnLand(p, out var y)) round.Add(new Vector3(p.x, y, p.z));
            }
            for (var i = 0; i < count; i++)
            {
                _animals.Add(new Animal
                {
                    Kind = kind, Home = home, Round = round.ToArray(), Next = rng.Next(round.Count),
                    Offset = new Vector3(Range(rng, -0.6f, 0.6f), 0f, Range(rng, -0.6f, 0.6f)) * i,
                    Velocity = Vector3.zero, Timer = Range(rng, 1f, 5f),
                    Heading = Quaternion.Euler(0f, Range(rng, 0f, 360f), 0f) * Vector3.forward,
                });
            }
        }

        private Animal Gull(Vector3 centre, Transform follow, System.Random rng)
        {
            var g = new Animal
            {
                Kind = Kind.Gull, Home = new Vector3(centre.x, _sea, centre.z), Follow = follow,
                Radius = Range(rng, 2f, 4f), Speed = Range(rng, 0.45f, 0.7f) * (rng.Next(2) == 0 ? -1f : 1f),
                Phase = Range(rng, 0f, Mathf.PI * 2f), Height = Range(rng, 3f, 4.5f),
            };
            _animals.Add(g);
            return g;
        }

        /// <summary>A sea spot: `count` swimmers side by side, a fixed line through the spot, a pause between showings.</summary>
        private void Pod(Kind kind, Vector3 spot, int count, System.Random rng)
        {
            var heading = Quaternion.Euler(0f, Range(rng, 0f, 360f), 0f) * Vector3.forward;
            var side = Vector3.Cross(Vector3.up, heading);
            var show = kind == Kind.Fish ? 1f : kind == Kind.Dolphin ? 8f : 16f;
            for (var k = 0; k < count; k++)
            {
                _animals.Add(new Animal
                {
                    Kind = kind, Home = new Vector3(spot.x, _sea, spot.z), Heading = heading,
                    Offset = side * ((k - (count - 1) * 0.5f) * 0.9f) - heading * (k % 2) * 0.6f,
                    Phase = k * 0.35f, Show = show, State = 0, Timer = Pause(kind, rng),
                });
            }
        }

        private static float Pause(Kind kind, System.Random rng) =>
            kind == Kind.Fish ? Range(rng, 4f, 10f) : kind == Kind.Dolphin ? Range(rng, 20f, 45f) : Range(rng, 40f, 90f);

        // ------------------------------------------------------------------ on and off with the view

        private Vector3 Where(Animal a)
        {
            if (a.Root != null) return a.Root.position;
            return a.Follow != null ? a.Follow.position : a.Home + a.Offset;
        }

        private void SwitchByView()
        {
            foreach (var a in _animals)
            {
                if (a.Follow != null && !a.Follow.gameObject.activeInHierarchy) a.Follow = null;
                var at = a.Kind == Kind.Gull ? (a.Follow != null ? a.Follow.position : a.Home) : Where(a);
                var on = InView(at) && Revealed(CellOf(at));
                if (on == a.On) continue;
                a.On = on;
                if (on && a.Root == null) Dress(a);
                if (a.Root != null) a.Root.gameObject.SetActive(on);
            }
        }

        /// <summary>The animal's model, made the first time it is seen, standing where its place says.</summary>
        private void Dress(Animal a)
        {
            var root = new GameObject("Fauna" + a.Kind);
            root.transform.SetParent(transform, false);
            var figure = Instantiate(_models[(int)a.Kind], root.transform, false);
            figure.name = a.Kind.ToString();
            // modelled facing Blender's -y: turned round to go nose first (as the jungle's beasts)
            figure.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * figure.transform.localRotation;
            SceneryShadows.DisableCasting(figure);
            SeasonStyle.NoSnow(figure);
            a.Root = root.transform;
            if (a.Kind == Kind.Fish || a.Kind == Kind.Dolphin || a.Kind == Kind.Whale)
            {
                foreach (var t in figure.GetComponentsInChildren<Transform>(true))
                    if (t.name.EndsWith("Tail")) { a.Tail = t; a.TailRest = t.localRotation; }
                if (a.Kind == Kind.Fish) root.transform.localScale = Vector3.one * 1.4f;
                // under water until its turn to show
                root.transform.position = a.Home + a.Offset + Vector3.down * 3f;
            }
            else if (a.Kind == Kind.Deer || a.Kind == Kind.Rabbit)
            {
                var p = a.Home + a.Offset;
                root.transform.position = new Vector3(p.x, OnLand(p, out var y) ? y : a.Home.y, p.z);
                root.transform.rotation = Quaternion.LookRotation(a.Heading);
            }
            if (a.Kind != Kind.Fish && a.Kind != Kind.Dolphin && a.Kind != Kind.Whale)
                root.AddComponent<FigureAnimator>().Bind(figure.transform);
        }

        private void GatherThreats()
        {
            _threats.Clear();
            var any = false;
            foreach (var a in _animals) if (a.On && (a.Kind == Kind.Deer || a.Kind == Kind.Rabbit)) { any = true; break; }
            if (!any) return;
            foreach (var c in FindObjectsByType<Citizens.CitizenAgent>(FindObjectsSortMode.None)) _threats.Add(c.transform.position);
            foreach (var s in SoldierUnit.All) if (s != null) _threats.Add(s.transform.position);
            foreach (var o in OrcUnit.All) if (o != null) _threats.Add(o.transform.position);
        }

        // ------------------------------------------------------------------ deer and hares

        private void MoveLand(Animal a, float dt)
        {
            var deer = a.Kind == Kind.Deer;
            var here = a.Root.position;

            // anyone close: bolt the other way
            if (a.State != 2)
            {
                foreach (var t in _threats)
                {
                    var away = Flat(here - t);
                    if (away.sqrMagnitude > FleeRadius * FleeRadius) continue;
                    a.State = 2;
                    a.Timer = Random.Range(2f, 3f);
                    a.Velocity = (away.sqrMagnitude > 0.001f ? away.normalized : a.Root.forward) * (deer ? 3.2f : 2.6f);
                    break;
                }
            }

            a.Timer -= dt;
            if (a.State == 0)
            {
                if (a.Timer > 0f) return;
                // on along the round to the next waypoint
                a.Next = (a.Next + 1) % a.Round.Length;
                a.State = 1;
                a.Timer = 12f;
            }
            if (a.State == 1)
            {
                var to = Flat(a.Round[a.Next] + a.Offset - here);
                if (to.sqrMagnitude < 0.04f || a.Timer <= 0f) { a.State = 0; a.Timer = Random.Range(2f, 6f); return; }
                a.Velocity = to.normalized * (deer ? 0.7f : 0.5f);
            }
            // after a scare: back to the round (the nearest leg of it is just the next waypoint)
            if (a.State == 2 && a.Timer <= 0f) { a.State = 1; a.Timer = 15f; return; }

            var next = here + a.Velocity * dt;
            if (!OnLand(next, out var y))
            {
                // a building, a shore or a slope ahead: turn aside, or stop and graze
                var turned = Quaternion.Euler(0f, Random.value < 0.5f ? 70f : -70f, 0f) * a.Velocity;
                next = here + turned * dt;
                if (!OnLand(next, out y)) { a.State = 0; a.Timer = 1f; return; }
                a.Velocity = turned;
            }
            // a hare hops: its runs are bounds, not a trot
            var hop = !deer ? Mathf.Abs(Mathf.Sin(a.Age * (a.State == 2 ? 14f : 9f))) * 0.08f : 0f;
            a.Root.position = new Vector3(next.x, y + hop, next.z);
            Face(a.Root, a.Velocity, dt * 8f);
        }

        private static void Face(Transform t, Vector3 heading, float rate)
        {
            heading.y = 0f;
            if (heading.sqrMagnitude < 0.0001f) return;
            t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(heading), Mathf.Clamp01(rate));
        }

        // ------------------------------------------------------------------ gulls

        /// <summary>One gull for each ship, made the first time the ship is in view and kept with it.</summary>
        private void GullsOverShips()
        {
            var ships = Ships.ShipManager.Instance;
            if (ships == null || !_hasSea) return;
            foreach (var ship in ships.Ships)
            {
                if (ship == null || !InView(ship.transform.position)) continue;
                var followed = false;
                foreach (var a in _animals) if (a.Follow == ship.transform) { followed = true; break; }
                if (followed) continue;
                var rng = new System.Random(ships.Ships.Count * 977 + _animals.Count);
                Gull(ship.transform.position, ship.transform, rng);
                // a ship lost later leaves its gull to circle where it was
            }
        }

        private void MoveGull(Animal g, float dt)
        {
            if (g.Follow != null) g.Home = new Vector3(g.Follow.position.x, g.Home.y, g.Follow.position.z);
            g.Phase += g.Speed * dt;
            var r = g.Radius * (1f + 0.15f * Mathf.Sin(g.Phase * 1.7f));
            var at = g.Home + new Vector3(Mathf.Cos(g.Phase) * r, g.Height + 0.3f * Mathf.Sin(g.Phase * 2.3f), Mathf.Sin(g.Phase) * r);
            var heading = new Vector3(-Mathf.Sin(g.Phase), 0f, Mathf.Cos(g.Phase)) * Mathf.Sign(g.Speed);
            g.Root.position = at;
            // banked into the turn
            g.Root.rotation = Quaternion.LookRotation(heading) * Quaternion.Euler(0f, 0f, 22f * Mathf.Sign(g.Speed));
        }

        // ------------------------------------------------------------------ a flock out of a tree

        private void SpawnFlock()
        {
            var trees = ResourceNode.All;
            if (trees.Count == 0) return;
            for (var tries = 0; tries < 20; tries++)
            {
                var tree = trees[Random.Range(0, trees.Count)];
                if (tree == null || tree.ResourceType != CityBuilder.Resources.ResourceType.Wood) continue;
                var p = tree.transform.position;
                if (Flat(p - _focus).sqrMagnitude > _reach * _reach || !Revealed(CellOf(p))) continue;
                var away = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
                var n = Random.Range(5, 8);
                for (var k = 0; k < n; k++)
                {
                    var b = _idleBirds.Count > 0 ? _idleBirds.Pop() : NewBird();
                    b.Root.position = p + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(1.2f, 1.8f), Random.Range(-0.5f, 0.5f));
                    b.Root.gameObject.SetActive(true);
                    b.Age = 0f;
                    b.Velocity = (away + Random.insideUnitSphere * 0.25f).normalized * Random.Range(2.6f, 3.4f) + Vector3.up * Random.Range(1.6f, 2.4f);
                    b.Phase = Random.Range(0f, 10f);
                    b.Root.localScale = Vector3.one * Random.Range(1.3f, 1.6f);
                    _birds.Add(b);
                }
                return;
            }
        }

        private Animal NewBird()
        {
            var b = new Animal { Kind = Kind.Bird };
            Dress(b);
            return b;
        }

        private static bool MoveBird(Animal b, float dt)
        {
            if (b.Age > 8f) return false;
            // the climb levels off; each bird weaves a little on its own
            b.Velocity.y = Mathf.Lerp(b.Velocity.y, 0.15f, dt * 0.7f);
            var weave = new Vector3(Mathf.Sin(b.Age * 3f + b.Phase), 0f, Mathf.Cos(b.Age * 2.6f + b.Phase)) * 0.6f;
            b.Root.position += (b.Velocity + weave) * dt;
            b.Root.rotation = Quaternion.LookRotation(b.Velocity + weave);
            return true;
        }

        // ------------------------------------------------------------------ fish, dolphins, a whale

        /// <summary>
        /// A sea animal waits under water at its spot, then shows along its fixed line: a fish leaps
        /// once, the dolphins arc out and (next time) back, the whale surfaces, glides and dives.
        /// </summary>
        private void MoveSea(Animal a, float dt)
        {
            a.Timer -= dt;
            if (a.State == 0)
            {
                if (a.Timer > 0f) return;
                a.State = 1;
                a.Age = 0f;
                // every showing goes the other way along the line, so they stay round their spot
                a.Heading = -a.Heading;
            }
            var t = a.Age;
            if (t >= a.Show)
            {
                a.State = 0;
                a.Timer = Pause(a.Kind, _rhythm);
                a.Root.position = a.Home + a.Offset + Vector3.down * 3f;
                return;
            }
            float y, pitch, along;
            switch (a.Kind)
            {
                case Kind.Fish:
                {
                    // one leap: up out of the water and back in, nose over
                    var u = t / a.Show;
                    along = (u - 0.5f) * 0.9f;
                    y = Mathf.Sin(u * Mathf.PI) * 0.6f - 0.05f;
                    pitch = Mathf.Lerp(-55f, 55f, u);
                    break;
                }
                case Kind.Dolphin:
                {
                    // arcs one after another, a little behind each other, across the spot
                    along = (t - a.Show * 0.5f) * 3.2f;
                    var cycle = Mathf.Repeat((t + a.Phase) / 1.7f, 1f);
                    y = Mathf.Sin(cycle * Mathf.PI * 2f) * 0.75f - 0.3f;
                    pitch = -Mathf.Cos(cycle * Mathf.PI * 2f) * 40f;
                    break;
                }
                default:
                {
                    // the whale: up to show its back, a slow glide, then down tail last
                    along = (t - a.Show * 0.5f) * 0.9f;
                    var rise = Mathf.SmoothStep(0f, 1f, t / 3f);
                    var dive = Mathf.SmoothStep(0f, 1f, (t - (a.Show - 5f)) / 4f);
                    y = (Mathf.Lerp(-1.4f, -0.12f, rise) - dive * 2.2f) * 1.4f;
                    pitch = dive * 30f - (1f - rise) * 8f;
                    if (a.Tail != null) a.Tail.localRotation = a.TailRest * Quaternion.Euler(-dive * 55f, 0f, 0f);
                    break;
                }
            }
            if (a.Tail != null && a.Kind != Kind.Whale)
                a.Tail.localRotation = a.TailRest * Quaternion.Euler(0f, Mathf.Sin(t * 14f) * 20f, 0f);
            a.Root.position = a.Home + a.Offset + a.Heading * along + Vector3.up * y;
            a.Root.rotation = Quaternion.LookRotation(a.Heading) * Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}
