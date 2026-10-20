using System;
using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Maps;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CityBuilder.UI
{
    /// <summary>
    /// The live scene behind the main menu (the user's sketch, 2026-10-20: "3д сцены, которые
    /// меняются рандомно при перезаходе в игру, на 3д сценах происходит какое-то действие --
    /// люди добывают камень, рубят дерево, пашут поле, строят дом").
    ///
    /// Each time the menu loads it takes one of the designed maps -- ground, hills and water, as in
    /// the game -- finds a flat dry meadow on it, and stages one of four scenes there out of the
    /// game's own buildings, trees and rocks: woodcutters felling a grove, a quarry, ploughmen in a
    /// field, a house going up. The people are the game's cube citizens with an arm and a tool
    /// (MenuWorker), and the camera hangs over it, breathing a little (MenuVignetteCamera).
    ///
    /// The prefabs come in with everything but their looks stripped: a building or a tree is only
    /// scenery here, and its gameplay components would go looking for managers the menu has none of.
    /// </summary>
    public class MainMenuBackground : MonoBehaviour
    {
        public enum Vignette { Woodcutters = 0, Quarry = 1, Fields = 2, Building = 3 }

        [Serializable]
        public struct MenuProp
        {
            public string id;
            public GameObject prefab;
        }

        [SerializeField] private MenuProp[] buildings = new MenuProp[0];
        [SerializeField] private Camera menuCamera;

        /// <summary>Set by a test to stage one scene rather than a random one; cleared after use.</summary>
        public static Vignette? ForcedVignette;
        public static string ForcedMapId;

        public Vignette Current { get; private set; }
        public Vector3 Spot { get; private set; }

        private static readonly Color[] Clothing =
        {
            new Color(0.72f, 0.26f, 0.24f), new Color(0.24f, 0.42f, 0.68f), new Color(0.32f, 0.55f, 0.28f),
            new Color(0.62f, 0.5f, 0.28f), new Color(0.45f, 0.42f, 0.48f),
        };

        private MeshMapDefinition _map;
        private Transform _stage;
        private Vector3 _forward;
        private Vector3 _right;
        private float _waterLevel = float.NegativeInfinity;
        private readonly List<Vector3> _taken = new List<Vector3>();

        private void Start()
        {
            var maps = MeshMapCatalog.All;
            if (maps.Count == 0) return;
            _map = null;
            if (!string.IsNullOrEmpty(ForcedMapId)) _map = MeshMapCatalog.GetById(ForcedMapId);
            if (_map == null) _map = maps[Random.Range(0, maps.Count)];

            BuildLand();
            Spot = FindMeadow();

            Current = ForcedVignette ?? (Vignette)Random.Range(0, 4);
            ForcedVignette = null;
            ForcedMapId = null;

            // The scene is laid out facing the camera: x to the right, z away from it.
            var yaw = Random.Range(0f, 360f);
            _forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            _right = Vector3.Cross(Vector3.up, _forward);
            _stage = new GameObject("Vignette").transform;
            _stage.SetParent(transform, false);

            switch (Current)
            {
                case Vignette.Woodcutters: Woodcutters(); break;
                case Vignette.Quarry: Quarry(); break;
                case Vignette.Fields: Fields(); break;
                default: Building(); break;
            }
            Surroundings();

            if (menuCamera != null)
            {
                var rig = menuCamera.GetComponent<MenuVignetteCamera>();
                if (rig == null) rig = menuCamera.gameObject.AddComponent<MenuVignetteCamera>();
                rig.Frame(Spot + Vector3.up * 0.4f, yaw);
            }
        }

        // ------------------------------------------------------------ the land

        private void BuildLand()
        {
            var ground = Place(_map.GroundPrefab, keepColliders: true);
            AddColliders(ground);
            var relief = Place(_map.ReliefPrefab, keepColliders: true);
            AddColliders(relief);

            if (_map.WaterPrefab != null)
            {
                var euler = _map.WaterPrefab.transform.rotation.eulerAngles;
                var rotation = _map.KeepWaterRotation ? _map.WaterPrefab.transform.rotation : Quaternion.Euler(euler.x, -90f, euler.z);
                var water = Instantiate(_map.WaterPrefab, Vector3.zero, rotation, transform);
                foreach (var r in water.GetComponentsInChildren<Renderer>())
                {
                    if (_map.WaterMaterial != null) r.sharedMaterial = _map.WaterMaterial;
                    _waterLevel = Mathf.Max(_waterLevel, r.bounds.max.y);
                }
            }
        }

        private GameObject Place(GameObject prefab, bool keepColliders)
        {
            if (prefab == null) return null;
            var go = Instantiate(prefab, Vector3.zero, prefab.transform.rotation, transform);
            if (!keepColliders) foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            return go;
        }

        private static void AddColliders(GameObject root)
        {
            if (root == null) return;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.GetComponent<Collider>() != null || filter.sharedMesh == null) continue;
                filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
            }
        }

        /// <summary>The ground's height under a point, or NaN over nothing.</summary>
        private static float Height(float x, float z)
        {
            return Physics.Raycast(new Vector3(x, 400f, z), Vector3.down, out var hit, 800f) ? hit.point.y : float.NaN;
        }

        /// <summary>The flattest dry patch near the middle of the map: candidates scored by how much the ground rises and falls round them.</summary>
        private Vector3 FindMeadow()
        {
            var best = Vector3.zero;
            var bestScore = float.MaxValue;
            for (var i = 0; i < 120; i++)
            {
                var p = i == 0 ? Vector2.zero : Random.insideUnitCircle * 70f;
                var h = Height(p.x, p.y);
                if (float.IsNaN(h) || h < _waterLevel + 0.25f) continue;

                float lo = h, hi = h;
                var dry = true;
                for (var k = 0; k < 16 && dry; k++)
                {
                    var a = k * Mathf.PI / 8f;
                    var r = k % 2 == 0 ? 7f : 4f;
                    var s = Height(p.x + Mathf.Cos(a) * r, p.y + Mathf.Sin(a) * r);
                    if (float.IsNaN(s) || s < _waterLevel + 0.15f) dry = false;
                    lo = Mathf.Min(lo, s);
                    hi = Mathf.Max(hi, s);
                }
                if (!dry) continue;
                var score = (hi - lo) + p.magnitude * 0.004f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = new Vector3(p.x, h, p.y);
                }
            }
            return best;
        }

        /// <summary>A point of the stage: x to the right of the camera's view, z away from it, on the ground.</summary>
        private Vector3 At(float x, float z)
        {
            var p = Spot + _right * x + _forward * z;
            var h = Height(p.x, p.z);
            p.y = float.IsNaN(h) ? Spot.y : h;
            return p;
        }

        // ------------------------------------------------------------ props

        private GameObject Prop(GameObject prefab, Vector3 position, float yaw, float scale = 1f)
        {
            if (prefab == null) return null;
            // Made under an inactive parent, so nothing of the prefab's wakes up before it is stripped.
            var holder = new GameObject("holder");
            holder.SetActive(false);
            var go = Instantiate(prefab, holder.transform);
            foreach (var behaviour in go.GetComponentsInChildren<Behaviour>(true))
            {
                if (behaviour is Light) continue;
                DestroyImmediate(behaviour);
            }
            foreach (var collider in go.GetComponentsInChildren<Collider>(true)) DestroyImmediate(collider);
            go.transform.SetParent(_stage, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f) * prefab.transform.rotation);
            go.transform.localScale = prefab.transform.localScale * scale;
            Destroy(holder);
            return go;
        }

        private GameObject House(string id, float x, float z, float turn = 0f)
        {
            GameObject prefab = null;
            foreach (var b in buildings) if (b.id == id) prefab = b.prefab;
            // The door (the model's -Z) faces the camera, give or take.
            var yaw = Quaternion.LookRotation(_forward).eulerAngles.y + turn + Random.Range(-14f, 14f);
            var position = At(x, z);
            _taken.Add(position);
            return Prop(prefab, position, yaw);
        }

        private GameObject Tree(float x, float z, float scale = 1f)
        {
            var trees = _map.TreePrefabs;
            if (trees == null || trees.Length == 0) return null;
            var position = At(x, z);
            if (position.y < _waterLevel + 0.1f) return null;
            _taken.Add(position);
            return Prop(trees[Random.Range(0, trees.Length)], position, Random.Range(0f, 360f), scale * Random.Range(0.85f, 1.15f));
        }

        private GameObject Rock(float x, float z, float scale = 1f)
        {
            var rocks = _map.RockPrefabs;
            if (rocks == null || rocks.Length == 0) return null;
            var position = At(x, z);
            _taken.Add(position);
            // the bigger boulders, from the end of the list (it is ordered smallest first)
            var pick = rocks[Mathf.Clamp(rocks.Length - 1 - Random.Range(0, 3), 0, rocks.Length - 1)];
            return Prop(pick, position, Random.Range(0f, 360f), scale);
        }

        private static Material _woodMaterial;
        private static Material _stoneMaterial;
        private static Material _soilMaterial;
        private static Material _leafMaterial;

        private static Material Mat(ref Material cache, Color color)
        {
            if (cache == null) cache = new Material(RuntimeShaders.Lit) { color = color };
            return cache;
        }

        private static Material Wood => Mat(ref _woodMaterial, new Color(0.52f, 0.34f, 0.18f));
        private static Material Stone => Mat(ref _stoneMaterial, new Color(0.6f, 0.58f, 0.55f));
        private static Material Soil => Mat(ref _soilMaterial, new Color(0.42f, 0.29f, 0.17f));
        private static Material Leaf => Mat(ref _leafMaterial, new Color(0.33f, 0.62f, 0.22f));

        private GameObject Piece(PrimitiveType type, Vector3 position, Vector3 euler, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(_stage, false);
            go.transform.SetPositionAndRotation(position, Quaternion.LookRotation(_forward) * Quaternion.Euler(euler));
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private void LogPile(float x, float z)
        {
            var b = At(x, z);
            for (var row = 0; row < 3; row++)
            {
                for (var i = 0; i < 3 - row; i++)
                {
                    var off = _right * ((i - (2 - row) * 0.5f) * 0.17f) + Vector3.up * (0.08f + row * 0.15f);
                    Piece(PrimitiveType.Cylinder, b + off, new Vector3(90f, 0f, 0f), new Vector3(0.16f, 0.36f, 0.16f), Wood);
                }
            }
        }

        private void StonePile(float x, float z)
        {
            var b = At(x, z);
            for (var i = 0; i < 7; i++)
            {
                var off = new Vector3(Random.Range(-0.3f, 0.3f), 0.08f + (i > 4 ? 0.14f : 0f), Random.Range(-0.25f, 0.25f));
                Piece(PrimitiveType.Cube, b + off, new Vector3(Random.Range(0f, 30f), Random.Range(0f, 90f), 0f), Vector3.one * Random.Range(0.14f, 0.22f), Stone);
            }
        }

        private void PlankPile(float x, float z)
        {
            var b = At(x, z);
            for (var i = 0; i < 5; i++)
            {
                Piece(PrimitiveType.Cube, b + Vector3.up * (0.03f + i * 0.055f), new Vector3(0f, 90f + Random.Range(-6f, 6f), 0f), new Vector3(0.14f, 0.05f, 0.9f), Wood);
            }
        }

        private MenuWorker Worker(Vector3 position, Vector3 lookAt, MenuWorker.Tool tool, MenuWorker.Motion motion)
        {
            var go = new GameObject("Worker");
            go.transform.SetParent(_stage, false);
            go.transform.position = position;
            var flat = lookAt - position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.001f) go.transform.rotation = Quaternion.LookRotation(flat);
            var worker = go.AddComponent<MenuWorker>();
            worker.Build(Clothing[Random.Range(0, Clothing.Length)], tool, motion, Random.value);
            return worker;
        }

        private void Carrier(Vector3 from, Vector3 to, MenuWorker.Tool load)
        {
            var worker = Worker(from, to, load, MenuWorker.Motion.Carry);
            worker.Route(from, to);
        }

        // ------------------------------------------------------------ the four scenes

        private void Woodcutters()
        {
            House("Sawmill", -3.4f, 1.8f);
            LogPile(-1.5f, 0.2f);
            var felled = Tree(1.4f, 0.9f, 1.1f);
            if (felled != null) felled.AddComponent<MenuFallingTree>().Fall(-_right);
            var target = At(1.4f, 0.9f);
            Worker(At(0.85f, 0.45f), target, MenuWorker.Tool.Axe, MenuWorker.Motion.Chop);
            Worker(At(1.95f, 0.4f), target, MenuWorker.Tool.Axe, MenuWorker.Motion.Chop);
            Carrier(At(-1.1f, -0.2f), At(0.6f, 0.0f), MenuWorker.Tool.Log);
            Grove(2.8f, 3f, 14, 3.2f);
        }

        private void Quarry()
        {
            House("Quarry", -3.4f, 1.8f);
            StonePile(-1.4f, 0.1f);
            for (var i = 0; i < 6; i++) Rock(1.2f + Random.Range(0f, 3.2f), 0.8f + Random.Range(0f, 2.4f), 1.2f);
            var rockA = At(1.4f, 1.0f);
            var rockB = At(2.7f, 1.4f);
            Worker(At(0.9f, 0.4f), rockA, MenuWorker.Tool.Pick, MenuWorker.Motion.Chop);
            Worker(At(2.4f, 0.75f), rockB, MenuWorker.Tool.Pick, MenuWorker.Motion.Chop);
            Carrier(At(-1.0f, -0.3f), At(0.5f, 0.1f), MenuWorker.Tool.Stone);
            Grove(-2f, 4.5f, 8, 2.5f);
        }

        private void Fields()
        {
            House("Farm", -3.6f, 2.2f);
            House("Windmill", 3.8f, 3.2f);
            // furrows: dark rows, the far half already green
            for (var row = 0; row < 8; row++)
            {
                var z = -0.6f + row * 0.36f;
                for (var seg = 0; seg < 6; seg++)
                {
                    var x = -1.2f + seg * 0.75f;
                    var p = At(x, z);
                    Piece(PrimitiveType.Cube, p + Vector3.up * 0.02f, Vector3.zero, new Vector3(0.78f, 0.06f, 0.2f), Soil);
                    if (row >= 4)
                    {
                        for (var s = 0; s < 3; s++)
                        {
                            Piece(PrimitiveType.Cube, At(x - 0.25f + s * 0.25f, z) + Vector3.up * 0.09f, new Vector3(0f, Random.Range(0f, 90f), 0f), new Vector3(0.09f, 0.12f, 0.09f), Leaf);
                        }
                    }
                }
            }
            var w1 = Worker(At(-1.0f, -0.42f), At(3.5f, -0.42f), MenuWorker.Tool.Hoe, MenuWorker.Motion.Hoe);
            w1.Route(At(-1.0f, -0.42f), At(2.6f, -0.42f));
            var w2 = Worker(At(2.6f, 0.3f), At(-1f, 0.3f), MenuWorker.Tool.Hoe, MenuWorker.Motion.Hoe);
            w2.Route(At(2.6f, 0.3f), At(-0.6f, 0.3f));
            Grove(-4.5f, 5f, 7, 2.2f);
        }

        private void Building()
        {
            House("Hovel", -3.6f, 2.0f, 10f);
            House("Well", 3.2f, 0.4f);
            House("Cottage", 3.9f, 3.0f, -15f);
            var rising = House("Cottage", 0.6f, 1.0f);
            if (rising != null) rising.AddComponent<MenuRisingBuilding>().Begin(Wood);
            PlankPile(-1.7f, -0.1f);
            Worker(At(-0.35f, 0.35f), At(0.6f, 1.0f), MenuWorker.Tool.Hammer, MenuWorker.Motion.Hammer);
            Worker(At(1.6f, 0.3f), At(0.6f, 1.0f), MenuWorker.Tool.Hammer, MenuWorker.Motion.Hammer);
            Carrier(At(-1.4f, -0.3f), At(0.0f, 0.1f), MenuWorker.Tool.Plank);
            Grove(-1f, 5.5f, 9, 2.6f);
        }

        private void Grove(float cx, float cz, int count, float radius)
        {
            for (var i = 0; i < count; i++)
            {
                var p = Random.insideUnitCircle * radius;
                if (Crowded(At(cx + p.x, cz + p.y), 0.7f)) continue;
                Tree(cx + p.x, cz + p.y);
            }
        }

        /// <summary>Trees and the odd rock round the stage -- a frame of countryside, kept off the camera's side and the stage itself.</summary>
        private void Surroundings()
        {
            for (var i = 0; i < 140; i++)
            {
                var a = Random.Range(0f, Mathf.PI * 2f);
                var r = Random.Range(4.8f, 15f);
                var x = Mathf.Cos(a) * r;
                var z = Mathf.Sin(a) * r;
                if (z < -1.5f) continue;
                if (Crowded(At(x, z), 0.75f)) continue;
                if (Random.value < 0.15f) Rock(x, z, 0.9f);
                else Tree(x, z);
            }
        }

        private bool Crowded(Vector3 p, float minDistance)
        {
            foreach (var t in _taken)
            {
                if ((t - p).sqrMagnitude < minDistance * minDistance) return true;
            }
            return false;
        }
    }
}
