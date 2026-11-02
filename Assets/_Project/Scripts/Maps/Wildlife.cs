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
    /// no fights, no saves -- only life round where the player is looking.
    ///
    /// - Deer and hares graze and wander on the open land and bolt from anyone who comes near (a
    ///   citizen, a soldier, an orc).
    /// - Gulls circle over the coasts and over the ships.
    /// - Now and then a flock of songbirds rises out of a tree and flies off.
    /// - A fish leaps near a shore; dolphins arc through open water; rarely, far out, a whale
    ///   surfaces, glides and dives with its flukes up.
    ///
    /// Mobile first: a handful of animals at a time, only round the camera's view and only on
    /// revealed ground, pooled -- nothing is made or destroyed while the game runs once every kind
    /// has been seen. Not in a dungeon or the Invasion (MeshMapApplier starts it). The archipelago's
    /// fishing shoals have their own fish (ShipManagerFish).
    /// </summary>
    public class Wildlife : MonoBehaviour
    {
        public enum Kind { Gull, Bird, Deer, Rabbit, Fish, Dolphin, Whale }

        private const int Deer = 2;
        private const int Hares = 3;
        private const int Gulls = 3;
        private const float FleeRadius = 3.5f;
        private const float StartDelay = 3f;

        private sealed class Animal
        {
            public Kind Kind;
            public Transform Root;
            public Transform Tail;
            public Quaternion TailRest;
            // land: 0 graze, 1 walk, 2 flee; sea and air: the event's own clock
            public int State;
            public float Timer;
            public float Age;
            public Vector3 Target;
            public Vector3 Velocity;
            public Vector3 Centre;
            public Transform Follow;
            public float Radius, Speed, Phase, Height;
            public bool Active;
        }

        private GameObject[] _models;
        private readonly List<Animal> _live = new List<Animal>();
        private readonly Dictionary<Kind, Stack<Animal>> _pool = new Dictionary<Kind, Stack<Animal>>();
        private readonly List<Vector3> _threats = new List<Vector3>();
        private Camera _camera;
        private Vector3 _focus;
        private float _reach;
        private float _clock;
        private float _landClock, _gullClock, _flockClock, _fishClock, _dolphinClock, _whaleClock, _threatClock;

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
            life._fishClock = Random.Range(4f, 8f);
            life._dolphinClock = Random.Range(20f, 40f);
            life._whaleClock = Random.Range(60f, 100f);
            return life;
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            if (dt <= 0f) return;
            _clock += dt;
            if (_clock < StartDelay || !Look()) return;

            if ((_threatClock -= dt) <= 0f) { _threatClock = 1f; GatherThreats(); }
            if ((_landClock -= dt) <= 0f) { _landClock = 1.5f; SpawnLand(); }
            if ((_gullClock -= dt) <= 0f) { _gullClock = 3f; SpawnGulls(); }
            if ((_flockClock -= dt) <= 0f) { _flockClock = Random.Range(15f, 30f); SpawnFlock(); }
            if ((_fishClock -= dt) <= 0f) { _fishClock = Random.Range(6f, 12f); SpawnSea(Kind.Fish, 1, 2, 1); }
            if ((_dolphinClock -= dt) <= 0f) { _dolphinClock = Random.Range(35f, 70f); SpawnSea(Kind.Dolphin, Random.Range(2, 4), 4, 8); }
            if ((_whaleClock -= dt) <= 0f) { _whaleClock = Random.Range(80f, 150f); SpawnSea(Kind.Whale, 1, 7, 16); }

            for (var i = _live.Count - 1; i >= 0; i--)
            {
                var a = _live[i];
                a.Age += dt;
                bool keep;
                switch (a.Kind)
                {
                    case Kind.Deer:
                    case Kind.Rabbit: keep = MoveLand(a, dt); break;
                    case Kind.Gull: keep = MoveGull(a, dt); break;
                    case Kind.Bird: keep = MoveBird(a, dt); break;
                    default: keep = MoveSea(a, dt); break;
                }
                if (!keep) Retire(i);
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

        private Vector3 AroundFocus(float from, float to)
        {
            var a = Random.Range(0f, Mathf.PI * 2f);
            var r = Random.Range(from, to) * _reach;
            return _focus + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
        }

        private bool FarOff(Vector3 p) => Flat(p - _focus).sqrMagnitude > _reach * _reach * 2.6f;

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private static bool Revealed(Vector2Int cell)
        {
            var fog = FogOfWarManager.Instance;
            return fog == null || fog.IsCellRevealed(cell);
        }

        private static Vector2Int CellOf(Vector3 p) => GridManager.Instance.WorldToCell(p);

        private static bool InBounds(Vector2Int cell) => GridManager.Instance.IsWithinBounds(cell, Vector2Int.one);

        /// <summary>Open, flat, revealed land at `p`, and its height.</summary>
        private static bool OnLand(Vector3 p, out float y)
        {
            y = 0f;
            var map = MeshMapApplier.Instance;
            var cell = CellOf(p);
            if (map == null || !InBounds(cell) || map.IsWaterCell(cell) || !Revealed(cell)) return false;
            if (!map.TryGetGroundHeight(p, out y)) return false;
            return Mathf.Abs(y - GridManager.Instance.GroundHeight) < 0.6f && GridManager.Instance.IsAreaFree(cell, Vector2Int.one);
        }

        private static bool Water(Vector3 p)
        {
            var map = MeshMapApplier.Instance;
            var cell = CellOf(p);
            return map != null && InBounds(cell) && map.IsWaterCell(cell) && Revealed(cell);
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
            var size = GridManager.Instance.CellSize * cells;
            for (var k = 0; k < 8; k++)
            {
                var a = k * Mathf.PI / 4f;
                var q = p + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * size;
                var map = MeshMapApplier.Instance;
                var cell = CellOf(q);
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

        private void GatherThreats()
        {
            _threats.Clear();
            var any = false;
            foreach (var a in _live) if (a.Kind == Kind.Deer || a.Kind == Kind.Rabbit) { any = true; break; }
            if (!any) return;
            foreach (var c in FindObjectsByType<Citizens.CitizenAgent>(FindObjectsSortMode.None)) _threats.Add(c.transform.position);
            foreach (var s in SoldierUnit.All) if (s != null) _threats.Add(s.transform.position);
            foreach (var o in OrcUnit.All) if (o != null) _threats.Add(o.transform.position);
        }

        // ------------------------------------------------------------------ the pool

        private Animal Take(Kind kind, Vector3 at)
        {
            if (!_pool.TryGetValue(kind, out var stack)) _pool[kind] = stack = new Stack<Animal>();
            var a = stack.Count > 0 ? stack.Pop() : Make(kind);
            a.Root.position = at;
            a.Root.gameObject.SetActive(true);
            a.State = 0;
            a.Timer = 0f;
            a.Age = 0f;
            a.Follow = null;
            a.Velocity = Vector3.zero;
            a.Active = true;
            _live.Add(a);
            return a;
        }

        private Animal Make(Kind kind)
        {
            var root = new GameObject("Fauna" + kind);
            root.transform.SetParent(transform, false);
            var figure = Instantiate(_models[(int)kind], root.transform, false);
            figure.name = kind.ToString();
            // modelled facing Blender's -y: turned round to walk nose first (as the jungle's beasts)
            figure.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * figure.transform.localRotation;
            SceneryShadows.DisableCasting(figure);
            SeasonStyle.NoSnow(figure);
            var a = new Animal { Kind = kind, Root = root.transform };
            if (kind == Kind.Fish || kind == Kind.Dolphin || kind == Kind.Whale)
            {
                foreach (var t in figure.GetComponentsInChildren<Transform>(true))
                    if (t.name.EndsWith("Tail")) { a.Tail = t; a.TailRest = t.localRotation; }
            }
            else root.AddComponent<FigureAnimator>().Bind(figure.transform);
            return a;
        }

        private void Retire(int index)
        {
            var a = _live[index];
            _live.RemoveAt(index);
            a.Active = false;
            a.Root.gameObject.SetActive(false);
            _pool[a.Kind].Push(a);
        }

        private int Count(Kind kind)
        {
            var n = 0;
            foreach (var a in _live) if (a.Kind == kind) n++;
            return n;
        }

        // ------------------------------------------------------------------ deer and hares

        private void SpawnLand()
        {
            var kind = Count(Kind.Deer) < Deer ? Kind.Deer : Count(Kind.Rabbit) < Hares ? Kind.Rabbit : (Kind?)null;
            if (kind == null) return;
            for (var tries = 0; tries < 6; tries++)
            {
                var p = AroundFocus(0.4f, 1.1f);
                if (!OnLand(p, out var y)) continue;
                var a = Take(kind.Value, new Vector3(p.x, y, p.z));
                a.Root.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                a.Timer = Random.Range(1f, 4f);
                return;
            }
        }

        private bool MoveLand(Animal a, float dt)
        {
            if (FarOff(a.Root.position)) return false;
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
                if (a.Timer > 0f) return true;
                var to = here + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(1.5f, 4f);
                if (!OnLand(to, out _)) { a.Timer = 1f; return true; }
                a.State = 1;
                a.Target = to;
                a.Timer = 8f;
            }
            if (a.State == 1)
            {
                var to = Flat(a.Target - here);
                if (to.sqrMagnitude < 0.04f || a.Timer <= 0f) { a.State = 0; a.Timer = Random.Range(2f, 6f); return true; }
                a.Velocity = to.normalized * (deer ? 0.7f : 0.5f);
            }
            if (a.State == 2 && a.Timer <= 0f) { a.State = 0; a.Timer = Random.Range(1f, 3f); return true; }

            var next = here + a.Velocity * dt;
            if (!OnLand(next, out var y))
            {
                // a building, a shore or a slope ahead: turn aside, or give up and graze
                var turned = Quaternion.Euler(0f, Random.value < 0.5f ? 70f : -70f, 0f) * a.Velocity;
                next = here + turned * dt;
                if (!OnLand(next, out y)) { a.State = 0; a.Timer = 1f; return true; }
                a.Velocity = turned;
            }
            // a hare hops: its runs are bounds, not a trot
            var hop = !deer ? Mathf.Abs(Mathf.Sin(a.Age * (a.State == 2 ? 14f : 9f))) * 0.08f : 0f;
            a.Root.position = new Vector3(next.x, y + hop, next.z);
            Face(a.Root, a.Velocity, dt * 8f);
            return true;
        }

        private static void Face(Transform t, Vector3 heading, float rate)
        {
            heading.y = 0f;
            if (heading.sqrMagnitude < 0.0001f) return;
            t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(heading), Mathf.Clamp01(rate));
        }

        // ------------------------------------------------------------------ gulls

        private void SpawnGulls()
        {
            if (!WaterLevel(out var sea)) return;
            // one over each ship in view first, then the coasts
            var ships = Ships.ShipManager.Instance;
            if (ships != null && Count(Kind.Gull) < Gulls + 2)
            {
                foreach (var ship in ships.Ships)
                {
                    if (ship == null || FarOff(ship.transform.position) || Followed(ship.transform)) continue;
                    var g = Take(Kind.Gull, ship.transform.position);
                    Circle(g, ship.transform.position, sea);
                    g.Follow = ship.transform;
                    break;
                }
            }
            if (Count(Kind.Gull) >= Gulls) return;
            for (var tries = 0; tries < 10; tries++)
            {
                var p = AroundFocus(0.2f, 1f);
                if (!Coast(p, 3)) continue;
                Circle(Take(Kind.Gull, p), p, sea);
                return;
            }
        }

        private bool Followed(Transform ship)
        {
            foreach (var a in _live) if (a.Follow == ship) return true;
            return false;
        }

        private static void Circle(Animal g, Vector3 centre, float sea)
        {
            g.Centre = new Vector3(centre.x, sea, centre.z);
            g.Radius = Random.Range(2f, 4f);
            g.Speed = Random.Range(0.45f, 0.7f) * (Random.value < 0.5f ? -1f : 1f);
            g.Phase = Random.Range(0f, Mathf.PI * 2f);
            g.Height = Random.Range(3f, 4.5f);
            g.Timer = Random.Range(30f, 60f);
        }

        private bool MoveGull(Animal g, float dt)
        {
            if (g.Follow != null)
            {
                if (!g.Follow.gameObject.activeInHierarchy) g.Follow = null;
                else g.Centre = new Vector3(g.Follow.position.x, g.Centre.y, g.Follow.position.z);
            }
            if (FarOff(g.Centre) || (g.Timer -= dt) <= 0f && g.Follow == null) return false;
            g.Phase += g.Speed * dt;
            var r = g.Radius * (1f + 0.15f * Mathf.Sin(g.Phase * 1.7f));
            var at = g.Centre + new Vector3(Mathf.Cos(g.Phase) * r, g.Height + 0.3f * Mathf.Sin(g.Phase * 2.3f), Mathf.Sin(g.Phase) * r);
            var heading = new Vector3(-Mathf.Sin(g.Phase), 0f, Mathf.Cos(g.Phase)) * Mathf.Sign(g.Speed);
            g.Root.position = at;
            // banked into the turn
            g.Root.rotation = Quaternion.LookRotation(heading) * Quaternion.Euler(0f, 0f, 22f * Mathf.Sign(g.Speed));
            return true;
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
                    var b = Take(Kind.Bird, p + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(1.2f, 1.8f), Random.Range(-0.5f, 0.5f)));
                    b.Velocity = (away + Random.insideUnitSphere * 0.25f).normalized * Random.Range(2.6f, 3.4f) + Vector3.up * Random.Range(1.6f, 2.4f);
                    b.Phase = Random.Range(0f, 10f);
                    b.Root.localScale = Vector3.one * Random.Range(1.3f, 1.6f);
                }
                return;
            }
        }

        private bool MoveBird(Animal b, float dt)
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

        /// <summary>`count` of `kind` at a revealed water point round the view: a coast for a fish, open water `open` cells wide for the others.</summary>
        private void SpawnSea(Kind kind, int count, int open, float seconds)
        {
            if (!WaterLevel(out var sea)) return;
            for (var tries = 0; tries < 10; tries++)
            {
                var p = AroundFocus(kind == Kind.Whale ? 0.6f : 0.25f, 1f);
                if (kind == Kind.Fish ? !Coast(p, 4) : !OpenWater(p, open)) continue;
                var heading = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
                var side = Vector3.Cross(Vector3.up, heading);
                for (var k = 0; k < count; k++)
                {
                    var at = p + side * ((k - (count - 1) * 0.5f) * 0.9f) - heading * (k % 2) * 0.6f;
                    var a = Take(kind, new Vector3(at.x, sea, at.z));
                    a.Centre = a.Root.position;
                    a.Velocity = heading;
                    a.Height = sea;
                    a.Timer = seconds;
                    a.Phase = k * 0.35f;
                    a.Root.rotation = Quaternion.LookRotation(heading);
                    if (kind == Kind.Fish) a.Root.localScale = Vector3.one * 1.4f;
                }
                return;
            }
        }

        private bool MoveSea(Animal a, float dt)
        {
            if (a.Age >= a.Timer) return false;
            var t = a.Age;
            float y, pitch, along;
            switch (a.Kind)
            {
                case Kind.Fish:
                {
                    // one leap: up out of the water and back in, nose over
                    var u = t / a.Timer;
                    along = u * 0.9f;
                    y = Mathf.Sin(u * Mathf.PI) * 0.6f - 0.05f;
                    pitch = Mathf.Lerp(-55f, 55f, u);
                    break;
                }
                case Kind.Dolphin:
                {
                    // arcs one after another through the swell, out a little behind each other
                    along = t * 3.2f;
                    var cycle = Mathf.Repeat((t + a.Phase) / 1.7f, 1f);
                    y = Mathf.Sin(cycle * Mathf.PI * 2f) * 0.75f - 0.3f;
                    pitch = -Mathf.Cos(cycle * Mathf.PI * 2f) * 40f;
                    break;
                }
                default:
                {
                    // the whale: up to show its back, a slow glide, then down tail last
                    along = t * 0.9f;
                    var rise = Mathf.SmoothStep(0f, 1f, t / 3f);
                    var dive = Mathf.SmoothStep(0f, 1f, (t - (a.Timer - 5f)) / 4f);
                    y = Mathf.Lerp(-1.4f, -0.12f, rise) - dive * 2.2f;
                    pitch = dive * 30f - (1f - rise) * 8f;
                    if (a.Tail != null) a.Tail.localRotation = a.TailRest * Quaternion.Euler(-dive * 55f, 0f, 0f);
                    break;
                }
            }
            if (a.Tail != null && a.Kind != Kind.Whale)
                a.Tail.localRotation = a.TailRest * Quaternion.Euler(0f, Mathf.Sin(t * 14f) * 20f, 0f);
            a.Root.position = a.Centre + a.Velocity * along + Vector3.up * y * (a.Kind == Kind.Whale ? 1.4f : 1f);
            a.Root.rotation = Quaternion.LookRotation(a.Velocity) * Quaternion.Euler(pitch, 0f, 0f);
            return true;
        }

        private void OnDestroy()
        {
            _live.Clear();
            _pool.Clear();
        }
    }
}
