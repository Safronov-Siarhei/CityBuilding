using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace CityBuilder.Campaign
{
    /// <summary>
    /// The village of the campaign's opening cutscene (the user, 2026-11-10: «надо сделать
    /// полноценную большую деревню, небо»), laid out the same every time from a seed.
    ///
    /// The square in the middle carries the cutscene's own set (the ruins, the church, the market),
    /// placed from OpeningSet.json exactly where the Blender film had them; round it the game's own
    /// buildings line streets that run out of the square into fields, and a forest closes it in.
    /// The buildings are the game's prefabs with everything but their looks stripped (as the main
    /// menu's background does), grown by <see cref="BuildingScale"/> to the cutscene figures' size.
    /// </summary>
    public class OpeningVillage : MonoBehaviour
    {
        [Serializable]
        public struct Prop
        {
            public string id;
            public GameObject prefab;
        }

        [Serializable]
        private class Placement
        {
            public string name;
            public float[] p;
            public float[] r;
            public float[] s;
        }

        [Serializable]
        private class StonePlacement
        {
            public string name;
            public Placement ruin;
            public Placement arch;
            public string glow;
        }

        [Serializable]
        private class SetFile
        {
            public Placement[] pieces;
            public StonePlacement[] stones;
        }

        /// <summary>The cutscene's figures stand a head over the game's townsfolk; its houses grow with them.</summary>
        public const float BuildingScale = 1.6f;

        [SerializeField] private Prop[] buildings = new Prop[0];
        [SerializeField] private GameObject[] trees = new GameObject[0];
        [SerializeField] private Prop[] setPieces = new Prop[0];
        [SerializeField] private TextAsset setFile;
        [SerializeField] private TextAsset propsFile;
        [SerializeField] private Material grass;
        [SerializeField] private Material cobble;
        [SerializeField] private Material dirt;
        [SerializeField] private Material field;
        [SerializeField] private CityBuilder.Maps.MapDecorData decorKit;
        [SerializeField] private Material decorMaterial;
        [SerializeField] private int seed = 11;

        private readonly Dictionary<string, Transform> _pieces = new Dictionary<string, Transform>();
        private readonly Dictionary<string, (Pose ruin, Pose arch)> _stonePoses = new Dictionary<string, (Pose, Pose)>();
        private readonly List<(Vector2 at, float r)> _taken = new List<(Vector2, float)>();
        /// <summary>What a runner cannot pass through: the houses and props (boxed), the trees (by the trunk).</summary>
        private readonly List<Transform> _solids = new List<Transform>();
        private readonly List<Transform> _trunks = new List<Transform>();
        private readonly List<Vector2[]> _streets = new List<Vector2[]>();
        private readonly List<Vector2[]> _lanes = new List<Vector2[]>();
        private readonly List<Transform> _houses = new List<Transform>();
        private readonly List<(string stone, string glow)> _glows = new List<(string, string)>();
        private Random _rnd;
        private Transform _root;

        /// <summary>A set piece by its model name (CineChurch, CinePortalStone_Arch3...).</summary>
        public Transform Piece(string name) => _pieces.TryGetValue(name, out var t) ? t : null;

        /// <summary>A set piece's model as imported (the rune glows are laid over their stones).</summary>
        public GameObject SetPieceModel(string name)
        {
            foreach (var p in setPieces) if (p.id == name) return p.prefab;
            return null;
        }

        /// <summary>Each portal stone and the name of its runes' glow model.</summary>
        public IEnumerable<(Transform stone, string glow)> StoneGlows()
        {
            foreach (var (name, glow) in _glows) yield return (Piece(name), glow);
        }

        /// <summary>Where a ruin stone lies, and where the arch puts it (village frame).</summary>
        public bool StonePoses(string name, out Pose ruin, out Pose arch)
        {
            var found = _stonePoses.TryGetValue(name, out var poses);
            ruin = poses.ruin;
            arch = poses.arch;
            return found;
        }

        public int BuildingCount { get; private set; }

        /// <summary>The roads out of the square, each from its square end outwards (not the ring road):
        /// where the villagers stroll and which way they flee.</summary>
        public IReadOnlyList<Vector2[]> Lanes => _lanes;

        /// <summary>Every house and farm building placed, for the orcs' torches.</summary>
        public IReadOnlyList<Transform> Houses => _houses;

        private void Start()
        {
            if (_root == null) Build();
        }

        /// <summary>Lays the whole village out under this object (also from the editor, for photos).</summary>
        public void Build()
        {
            if (_root != null) Kill(_root.gameObject);
            _root = new GameObject("Village").transform;
            _root.SetParent(transform, false);
            _pieces.Clear();
            _stonePoses.Clear();
            _taken.Clear();
            _streets.Clear();
            _lanes.Clear();
            _houses.Clear();
            _solids.Clear();
            _trunks.Clear();
            BuildingCount = 0;
            _rnd = new Random(seed);

            Ground();
            SetPieces();
            Streets();
            Featured();
            SquareRing();
            StreetHouses();
            Outskirts();
            MoreFields();
            Props();
            Trees();
            Decor();
        }

        // ------------------------------------------------------------ the land

        private void Ground()
        {
            Flat("Grass", Quad(new Vector2(-400, -400), new Vector2(400, -400), new Vector2(400, 400), new Vector2(-400, 400), -0.01f), grass);
            Flat("Square", Disc(Vector2.zero, 4.3f, 40, CobbleY + 0.003f), cobble);
            Reserve(Vector2.zero, 5.4f);                                       // the square stays open
            Reserve(new Vector2(0f, 7.4f), 3.4f);                              // the church
            Reserve(new Vector2(2.4f, -5.6f), 2.6f);                           // the market
        }

        private void Streets()
        {
            // the roads out of the square: into the market street, to the fields east and west,
            // up past the church, and lanes between them; then a ring road round the old houses
            Street(new[] { new Vector2(0, -4), new Vector2(0, -12), new Vector2(-3, -24), new Vector2(-6, -44) }, 2.4f, cobble);
            Street(new[] { new Vector2(0, 4), new Vector2(0, 5.4f) }, 1.8f, cobble);
            Street(new[] { new Vector2(4.2f, 0.6f), new Vector2(14, 2), new Vector2(26, 6), new Vector2(46, 11) }, 2.2f, cobble);
            Street(new[] { new Vector2(-4.2f, -0.4f), new Vector2(-14, -2), new Vector2(-26, -1), new Vector2(-46, 4) }, 2.2f, cobble);
            Street(new[] { new Vector2(3.1f, 3.1f), new Vector2(9, 11), new Vector2(14, 22), new Vector2(19, 40) }, 1.8f, dirt);
            Street(new[] { new Vector2(-3.1f, 3.1f), new Vector2(-9, 12), new Vector2(-12, 24), new Vector2(-16, 40) }, 1.8f, dirt);
            Street(new[] { new Vector2(9, 1.4f), new Vector2(12, -8), new Vector2(17, -18), new Vector2(22, -32) }, 1.6f, dirt);
            Street(new[] { new Vector2(-9, -1.2f), new Vector2(-12, -12), new Vector2(-16, -24) }, 1.6f, dirt);
            var ring = new Vector2[25];
            for (var i = 0; i < ring.Length; i++)
            {
                var a = Mathf.PI * 2f * i / (ring.Length - 1);
                ring[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 17f;
            }
            Street(ring, 1.6f, dirt, false, RingY);
        }

        // Every flat layer at its own height, 3 mm apart and more, so no two ever share a depth and
        // flicker (the user, 2026-11-11: a sandy lane blinked where it met the cobbles). Cobbles over
        // dirt lanes over the ring road over fields over grass; a lane's own segments alternate, as
        // two segments meet at a bend with their ruts running different ways. Cobble streets and the
        // square share one world-space pattern, so where they overlap nothing can show.
        private const float FieldY = 0.004f;
        private const float RingY = 0.008f;
        private const float LaneY = 0.014f;
        private const float CobbleY = 0.021f;

        private void Street(Vector2[] line, float width, Material material, bool lane = true, float y = -1f)
        {
            _streets.Add(line);
            var cobbled = material == cobble;
            if (y < 0f) y = cobbled ? CobbleY : LaneY;
            var seed = cobbled ? 0f : (float)_rnd.NextDouble() * 50f;
            if (lane && line.Length > 2) _lanes.Add(line);
            for (var i = 0; i + 1 < line.Length; i++)
            {
                var a = line[i];
                var b = line[i + 1];
                var side = Perp((b - a).normalized) * (width / 2f);
                // a little past each end, so the bends close
                var ext = (b - a).normalized * (width / 2f);
                var h = cobbled ? y : y + 0.003f * (i % 2);
                Flat("Street", Quad(a - ext - side, b + ext - side, b + ext + side, a - ext + side, h), material, b - a, seed);
            }
        }

        // ------------------------------------------------------------ the houses

        private static readonly string[] SquareHouses = { "Manor", "Cottage", "Baker", "Tavern", "Cottage", "Manor", "Smokehouse", "Cottage" };
        private static readonly string[] TownHouses = { "Hovel", "Cottage", "Hovel", "Cottage", "Manor", "Hovel", "Cottage", "Baker", "Hovel" };
        private static readonly string[] FarmHouses = { "Farm", "Barn", "Hovel", "Orchard", "PigFarm", "Windmill", "Granary", "Farm", "Hovel" };

        private void SquareRing()
        {
            // the old houses that face the square, between the roads into it
            for (var i = 0; i < 18; i++)
            {
                var a = Mathf.PI * 2f * i / 18f + 0.09f;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var id = SquareHouses[i % SquareHouses.Length];
                PlaceFacing(id, dir * 7.4f, -dir, 0.2f);
            }
            Place("Well", new Vector2(3.3f, 2.2f), 30f);
        }

        private void StreetHouses()
        {
            foreach (var line in _streets)
            {
                for (var i = 0; i + 1 < line.Length; i++)
                {
                    var a = line[i];
                    var b = line[i + 1];
                    var len = (b - a).magnitude;
                    var dir = (b - a) / len;
                    for (var side = -1; side <= 1; side += 2)
                    {
                        var n = Perp(dir) * side;
                        var t = 1.5f;
                        while (t < len)
                        {
                            var along = a + dir * t;
                            var far = along.magnitude;
                            var pool = far > 30f ? FarmHouses : TownHouses;
                            var id = pool[_rnd.Next(pool.Length)];
                            var size = Size(id);
                            var depth = size.y;
                            var spot = along + n * (1.6f + depth / 2f + (float)_rnd.NextDouble() * 0.6f);
                            var placed = far < 52f && _rnd.NextDouble() < (far > 30f ? 0.45 : 0.85) && PlaceFacing(id, spot, -n, 0.1f);
                            t += (placed ? size.x : 1.2f) + 0.7f + (float)_rnd.NextDouble() * 0.9f;
                        }
                    }
                }
            }
        }

        private void Outskirts()
        {
            // farmsteads out in the fields, each with its patch of ploughed land
            for (var i = 0; i < 14; i++)
            {
                var a = (float)_rnd.NextDouble() * Mathf.PI * 2f;
                var r = 34f + (float)_rnd.NextDouble() * 18f;
                var at = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (NearStreet(at, 3f)) continue;
                var w = 5f + (float)_rnd.NextDouble() * 4f;
                var d = 3.5f + (float)_rnd.NextDouble() * 3f;
                var turn = (float)_rnd.NextDouble() * 180f;
                if (!Free(at, Mathf.Max(w, d) * 0.6f)) continue;
                var q = Quaternion.Euler(0f, turn, 0f);
                Vector2 C(float x, float z) { var v = q * new Vector3(x, 0, z); return at + new Vector2(v.x, v.z); }
                Flat("Field", Quad(C(-w / 2, -d / 2), C(w / 2, -d / 2), C(w / 2, d / 2), C(-w / 2, d / 2), FieldY), field, C(1, 0) - at);
                Reserve(at, Mathf.Max(w, d) * 0.55f);
                PlaceFacing(FarmHouses[_rnd.Next(FarmHouses.Length)], at + (Vector2)(q * Vector3.right * (w / 2f + 2.2f)).XZ(), Vector2.zero - at, 0.1f);
            }
        }

        /// <summary>Ploughed strips all round the village out to the forest (the user, 2026-11-11: the
        /// country round the village looked empty), each its own size and turn, clear of the roads.</summary>
        private void MoreFields()
        {
            var placed = 0;
            for (var i = 0; i < 900 && placed < 70; i++)
            {
                var a = (float)_rnd.NextDouble() * Mathf.PI * 2f;
                var r = 20f + (float)_rnd.NextDouble() * 34f;
                var at = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                var w = 5f + (float)_rnd.NextDouble() * 7f;
                var d = 3f + (float)_rnd.NextDouble() * 4f;
                var radius = Mathf.Max(w, d) * 0.55f;
                if (NearStreet(at, radius + 0.8f) || !Free(at, radius)) continue;
                // furrows mostly along the way out from the village, as strip fields lie
                var turn = Mathf.Atan2(at.x, at.y) * Mathf.Rad2Deg + ((float)_rnd.NextDouble() - 0.5f) * 40f;
                var q = Quaternion.Euler(0f, turn, 0f);
                Vector2 C(float x, float z) { var v = q * new Vector3(x, 0, z); return at + new Vector2(v.x, v.z); }
                Flat("Field", Quad(C(-w / 2, -d / 2), C(w / 2, -d / 2), C(w / 2, d / 2), C(-w / 2, d / 2), FieldY), field, C(1, 0) - at);
                Reserve(at, radius);
                placed++;
            }
        }

        [Serializable] private class PropsFile { public float search; public FeaturedProp[] featured; }
        [Serializable] private class FeaturedProp { public string id; public float x, z, yaw; }

        private bool PutProp(string id, Vector2 at, float radius, float yaw)
        {
            var model = SetPieceModel(id);
            if (model == null || !Free(at, radius) || NearStreet(at, radius * 0.7f)) return false;
            var go = Instantiate(model, _root);
            go.name = id;
            go.transform.SetPositionAndRotation(new Vector3(at.x, 0f, at.y), Quaternion.Euler(0f, yaw, 0f) * model.transform.rotation);
            CineTwoSided.Apply(go);
            Reserve(at, radius);
            _solids.Add(go.transform);
            return true;
        }

        /// <summary>The model's footprint radius, from its mesh.</summary>
        private float PropRadius(string id)
        {
            var model = SetPieceModel(id);
            var mf = model != null ? model.GetComponentInChildren<MeshFilter>() : null;
            if (mf == null || mf.sharedMesh == null) return 0.7f;
            var size = Vector3.Scale(mf.sharedMesh.bounds.size, mf.transform.lossyScale);
            return Mathf.Max(size.x, size.z) * 0.5f;
        }

        /// <summary>
        /// The farm props set by hand where the cameras look (Campaign/OpeningProps.json), before the
        /// houses so they keep their places; a spot on a road is nudged outwards in rings.
        /// </summary>
        private void Featured()
        {
            if (propsFile == null) return;
            var file = JsonUtility.FromJson<PropsFile>(propsFile.text);
            foreach (var f in file.featured)
            {
                var want = new Vector2(f.x, f.z);
                var radius = PropRadius(f.id);
                var done = PutProp(f.id, want, radius, f.yaw);
                for (var r = 0.5f; !done && r <= file.search; r += 0.5f)
                {
                    for (var k = 0; k < 8 && !done; k++)
                    {
                        var a = k * Mathf.PI / 4f;
                        done = PutProp(f.id, want + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, radius, f.yaw);
                    }
                }
                if (!done) Debug.Log($"[OpeningVillage] no room for {f.id} at {want}");
            }
        }

        /// <summary>
        /// Country life (the user, 2026-11-11): haystacks and stacked bales along the roads out of
        /// town, pig pens and hen yards by the farmsteads, washing lines by the town houses -- the
        /// approved farm_props.py pieces, each kept clear of roads, houses and each other.
        /// </summary>
        private void Props()
        {
            bool Put(string id, Vector2 at, float radius, float yaw) => PutProp(id, at, radius, yaw);
            float Yaw() => (float)_rnd.NextDouble() * 360f;

            // by the roads, past the old town
            foreach (var line in _streets)
            {
                for (var i = 0; i + 1 < line.Length; i++)
                {
                    var a = line[i];
                    var b = line[i + 1];
                    var len = (b - a).magnitude;
                    var dir = (b - a) / len;
                    for (var t = 2f; t < len; t += 4.5f + (float)_rnd.NextDouble() * 4f)
                    {
                        var along = a + dir * t;
                        if (along.magnitude < 13f || _rnd.NextDouble() > 0.45) continue;
                        var side = Perp(dir) * (_rnd.NextDouble() < 0.5 ? -1f : 1f);
                        var spot = along + side * (2.0f + (float)_rnd.NextDouble() * 0.8f);
                        if (_rnd.NextDouble() < 0.6) Put("CineHaystack", spot, 0.6f, Yaw());
                        else Put("CineHayBales", spot, 0.7f, Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg + 90f);
                    }
                }
            }
            // by the houses: pens and hen yards at the farms, washing at the town houses
            foreach (var house in _houses.ToArray())
            {
                var at = house.position.XZ();
                var far = at.magnitude > 26f;
                var chance = far ? 0.7 : 0.25;
                if (_rnd.NextDouble() > chance) continue;
                for (var k = 0; k < 6; k++)
                {
                    var a = (float)_rnd.NextDouble() * Mathf.PI * 2f;
                    var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    if (far)
                    {
                        var pen = _rnd.NextDouble() < 0.55;
                        if (Put(pen ? "CinePigPen" : "CineHenYard", at + dir * (pen ? 3.6f : 3.0f), pen ? 1.7f : 1.1f, Yaw())) break;
                    }
                    else if (Put("CineWashing", at + dir * 2.4f, 0.8f, Mathf.Atan2(dir.y, -dir.x) * Mathf.Rad2Deg)) break;
                }
            }
            // and a stack or two at the field edges
            for (var i = 0; i < 60; i++)
            {
                var a = (float)_rnd.NextDouble() * Mathf.PI * 2f;
                var at = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (22f + (float)_rnd.NextDouble() * 30f);
                Put(_rnd.NextDouble() < 0.7 ? "CineHaystack" : "CineHayBales", at, 0.7f, Yaw());
            }
        }

        /// <summary>
        /// The islands' meadow props (the map decor kit: grass tufts, flowers, pebbles, bushes) strewn
        /// over the grass in clumps, kept off roads, fields and buildings, combined into a few big
        /// meshes in 25 m chunks (a renderer per prop would cost far more than it is worth).
        /// </summary>
        private void Decor()
        {
            if (decorKit == null || decorMaterial == null) return;
            var protos = decorKit.Prototypes;
            List<Mesh> Kind(string k)
            {
                var l = new List<Mesh>();
                foreach (var m in protos) if (m != null && m.name.Contains(k)) l.Add(m);
                return l;
            }
            var tufts = Kind("tuft");
            var flowers = Kind("flowers");
            var pebbles = Kind("pebbles");
            var bushes = Kind("bush");
            if (tufts.Count == 0) return;
            var chunks = new Dictionary<Vector2Int, List<CombineInstance>>();
            void Add(List<Mesh> kind, Vector2 at, float scale)
            {
                if (kind.Count == 0) return;
                var key = new Vector2Int(Mathf.FloorToInt(at.x / 25f), Mathf.FloorToInt(at.y / 25f));
                if (!chunks.TryGetValue(key, out var list)) chunks[key] = list = new List<CombineInstance>();
                list.Add(new CombineInstance
                {
                    mesh = kind[_rnd.Next(kind.Count)],
                    transform = Matrix4x4.TRS(new Vector3(at.x, 0f, at.y), Quaternion.Euler(0f, (float)_rnd.NextDouble() * 360f, 0f), Vector3.one * scale),
                });
            }
            for (var i = 0; i < 26000; i++)
            {
                var at = new Vector2(((float)_rnd.NextDouble() - 0.5f) * 180f, ((float)_rnd.NextDouble() - 0.5f) * 180f);
                if (at.magnitude < 4.6f || NearStreet(at, 1.3f) || !Free(at, 0.25f)) continue;
                var clump = Mathf.PerlinNoise(at.x / 3f + 40f, at.y / 3f + 40f);
                var meadow = Mathf.PerlinNoise(at.x / 14f + 7f, at.y / 14f + 7f);
                var r = (float)_rnd.NextDouble();
                var s = BuildingScale * (0.8f + (float)_rnd.NextDouble() * 0.5f);
                // bushes line the roads' verges (the user, 2026-11-11)
                if (NearStreet(at, 2.6f) && r < 0.22f) { Add(bushes, at, s * 1.2f); continue; }
                if (meadow > 0.62f && r < 0.5f) Add(flowers, at, s);
                else if (r < 0.32f * clump * 1.6f) Add(tufts, at, s);
                else if (r < 0.05f * clump) Add(bushes, at, s);
                else if (r < 0.03f) Add(pebbles, at, s);
            }
            foreach (var kv in chunks)
            {
                var mesh = new Mesh { name = "Decor", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(kv.Value.ToArray(), true, true);
                var go = new GameObject("Decor", typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(_root, false);
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.GetComponent<MeshRenderer>();
                mr.sharedMaterial = decorMaterial;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private void Trees()
        {
            if (trees.Length == 0) return;
            var count = 0;
            for (var i = 0; i < 5000 && count < 1100; i++)
            {
                var a = (float)_rnd.NextDouble() * Mathf.PI * 2f;
                // most in the forest ring, some among the houses and fields
                var r = _rnd.NextDouble() < 0.85 ? 48f + (float)_rnd.NextDouble() * 45f : 9f + (float)_rnd.NextDouble() * 40f;
                var at = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (NearStreet(at, 1.8f) || !Free(at, 0.9f)) continue;
                var model = trees[_rnd.Next(trees.Length)];
                var go = Instantiate(model, _root);
                go.name = "Tree";
                go.transform.SetPositionAndRotation(new Vector3(at.x, 0f, at.y),
                    Quaternion.Euler(0f, (float)_rnd.NextDouble() * 360f, 0f) * model.transform.rotation);
                go.transform.localScale = model.transform.localScale * BuildingScale * (0.8f + (float)_rnd.NextDouble() * 0.5f);
                Reserve(at, 0.7f);
                _trunks.Add(go.transform);
                count++;
            }
        }

        private bool PlaceFacing(string id, Vector2 at, Vector2 facing, float jitterDeg)
        {
            var yaw = facing.sqrMagnitude < 1e-6f ? 0f : Mathf.Atan2(-facing.x, -facing.y) * Mathf.Rad2Deg;
            yaw += ((float)_rnd.NextDouble() - 0.5f) * 2f * jitterDeg * 40f;
            return Place(id, at, yaw);
        }

        private bool Place(string id, Vector2 at, float yaw)
        {
            var prefab = Prefab(id);
            if (prefab == null) return false;
            var size = Size(id);
            var radius = Mathf.Max(size.x, size.y) * 0.5f;
            if (!Free(at, radius) || NearStreet(at, Mathf.Min(size.x, size.y) * 0.45f)) return false;
            var go = Strip(prefab);
            CineTwoSided.Apply(go);
            go.name = id;
            go.transform.SetParent(_root, false);
            go.transform.SetPositionAndRotation(new Vector3(at.x, 0f, at.y), Quaternion.Euler(0f, yaw, 0f) * prefab.transform.rotation);
            go.transform.localScale = prefab.transform.localScale * BuildingScale;
            Reserve(at, radius);
            if (id != "Well") _houses.Add(go.transform);
            _solids.Add(go.transform);
            BuildingCount++;
            return true;
        }

        private readonly Dictionary<string, Vector2> _sizes = new Dictionary<string, Vector2>();

        /// <summary>A building's width (x) and depth (z) once grown, measured on a stripped copy.</summary>
        private Vector2 Size(string id)
        {
            if (_sizes.TryGetValue(id, out var s)) return s;
            var prefab = Prefab(id);
            s = new Vector2(2f, 2f);
            if (prefab != null)
            {
                var go = Strip(prefab);
                CineTwoSided.Apply(go);
                go.transform.localScale = prefab.transform.localScale * BuildingScale;
                var any = false;
                var b = new Bounds();
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                {
                    if (!r.gameObject.activeInHierarchy) continue;
                    if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
                }
                if (any) s = new Vector2(b.size.x, b.size.z);
                Kill(go);
            }
            _sizes[id] = s;
            return s;
        }

        private GameObject Prefab(string id)
        {
            foreach (var b in buildings) if (b.id == id) return b.prefab;
            return null;
        }

        /// <summary>The prefab with everything but its looks stripped: here a building is scenery.</summary>
        private static GameObject Strip(GameObject prefab)
        {
            var holder = new GameObject("holder");
            holder.SetActive(false);
            var go = Instantiate(prefab, holder.transform);
            foreach (var behaviour in go.GetComponentsInChildren<Behaviour>(true))
            {
                if (behaviour is Light) continue;
                DestroyImmediate(behaviour);
            }
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) DestroyImmediate(c);
            go.transform.SetParent(null, false);
            Kill(holder);
            return go;
        }

        // ------------------------------------------------------------ the cutscene's own set

        private void SetPieces()
        {
            if (setFile == null) return;
            var file = JsonUtility.FromJson<SetFile>(setFile.text);
            if (file == null) return;
            _glows.Clear();
            foreach (var p in file.pieces)
            {
                var t = Spawn(p.name, ToPose(p));
                if (t != null && p.name == "CinePortalSwirl") t.gameObject.SetActive(false);   // the portal is not open yet
            }
            foreach (var s in file.stones)
            {
                var ruin = ToPose(s.ruin);
                _stonePoses[s.name] = (ruin, ToPose(s.arch));
                if (!string.IsNullOrEmpty(s.glow)) _glows.Add((s.name, s.glow));
                Spawn(s.name, ruin);
            }
        }

        private Transform Spawn(string name, Pose pose)
        {
            GameObject model = null;
            foreach (var p in setPieces) if (p.id == name) model = p.prefab;
            if (model == null) return null;
            var go = Instantiate(model, _root);
            CineTwoSided.Apply(go);
            go.name = name;
            go.transform.SetPositionAndRotation(pose.position, pose.rotation * model.transform.rotation);
            _pieces[name] = go.transform;
            return go.transform;
        }

        /// <summary>
        /// Colliders for a runner (mission 1, the user, 2026-11-11: Ардан runs through the village the
        /// film was shot in): a box round each house, prop and named set piece, fitted to its meshes
        /// in its own frame so a turned house gets a turned box, and a post round each tree's trunk.
        /// Only built when a mission asks -- the film needs none.
        /// </summary>
        public void MakeSolid(IEnumerable<string> pieces, float trunkRadius, float height)
        {
            var list = new List<Transform>(_solids);
            if (pieces != null)
                foreach (var name in pieces)
                    if (_pieces.TryGetValue(name, out var t) && t != null) list.Add(t);
            foreach (var t in list)
            {
                if (t == null || t.GetComponent<Collider>() != null) continue;
                var any = false;
                var b = new Bounds();
                foreach (var mf in t.GetComponentsInChildren<MeshFilter>())
                {
                    if (mf.sharedMesh == null || mf.name == "Back") continue;
                    var mb = mf.sharedMesh.bounds;
                    for (var i = 0; i < 8; i++)
                    {
                        var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                        var local = t.InverseTransformPoint(mf.transform.TransformPoint(c));
                        if (!any) { b = new Bounds(local, Vector3.zero); any = true; } else b.Encapsulate(local);
                    }
                }
                if (!any) continue;
                var box = t.gameObject.AddComponent<BoxCollider>();
                box.center = b.center;
                box.size = b.size;
            }
            foreach (var t in _trunks)
            {
                if (t == null || t.Find("Trunk") != null) continue;
                // a child of its own, upright in the world: the tree's frame may be turned by its import
                var trunk = new GameObject("Trunk", typeof(CapsuleCollider));
                trunk.transform.SetParent(_root, false);
                trunk.transform.position = t.position;
                var post = trunk.GetComponent<CapsuleCollider>();
                post.radius = trunkRadius;
                post.height = height;
                post.center = new Vector3(0f, height * 0.5f, 0f);
            }
        }

        private static Pose ToPose(Placement p) =>
            new Pose(new Vector3(p.p[0], p.p[1], p.p[2]), new Quaternion(p.r[0], p.r[1], p.r[2], p.r[3]));

        // ------------------------------------------------------------ helpers

        private void Reserve(Vector2 at, float r) => _taken.Add((at, r));

        private bool Free(Vector2 at, float r)
        {
            foreach (var (p, pr) in _taken)
            {
                if ((p - at).sqrMagnitude < (pr + r) * (pr + r)) return false;
            }
            return true;
        }

        private bool NearStreet(Vector2 at, float clearance)
        {
            foreach (var line in _streets)
            {
                for (var i = 0; i + 1 < line.Length; i++)
                {
                    if (DistanceToSegment(at, line[i], line[i + 1]) < clearance + 1.2f) return true;
                }
            }
            return false;
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
            return (a + ab * t - p).magnitude;
        }

        private static Vector2 Perp(Vector2 v) => new Vector2(-v.y, v.x);

        private MaterialPropertyBlock _mpb;

        /// <summary>A flat ground piece; dir lays a lane's ruts or a field's furrows along it (CineGround).</summary>
        private void Flat(string name, Mesh mesh, Material material, Vector2 dir = default, float seed = -1f)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(_root, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = material;
            if (dir != default)
            {
                _mpb ??= new MaterialPropertyBlock();
                _mpb.SetVector("_Dir", new Vector4(dir.x, 0f, dir.y, 0f));
                _mpb.SetFloat("_Seed", seed >= 0f ? seed : (float)_rnd.NextDouble() * 50f);
                r.SetPropertyBlock(_mpb);
            }
        }

        private static Mesh Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float y)
        {
            var mesh = new Mesh { name = "Flat" };
            mesh.vertices = new[] { new Vector3(a.x, y, a.y), new Vector3(b.x, y, b.y), new Vector3(c.x, y, c.y), new Vector3(d.x, y, d.y) };
            // wound to face up whichever way round the corners were given
            var up = Vector3.Cross(mesh.vertices[1] - mesh.vertices[0], mesh.vertices[2] - mesh.vertices[0]).y < 0f;
            mesh.triangles = up ? new[] { 0, 2, 1, 0, 3, 2 } : new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Mesh Disc(Vector2 centre, float radius, int sides, float y)
        {
            var mesh = new Mesh { name = "Disc" };
            var v = new Vector3[sides + 1];
            var tri = new int[sides * 3];
            v[0] = new Vector3(centre.x, y, centre.y);
            for (var i = 0; i < sides; i++)
            {
                var a = Mathf.PI * 2f * i / sides;
                v[i + 1] = new Vector3(centre.x + Mathf.Cos(a) * radius, y, centre.y + Mathf.Sin(a) * radius);
                tri[i * 3] = 0;
                tri[i * 3 + 1] = 1 + (i + 1) % sides;
                tri[i * 3 + 2] = 1 + i;
            }
            // wound to face up (Unity's front face: cross(b - a, c - a) points out of it)
            if (Vector3.Cross(v[tri[1]] - v[0], v[tri[2]] - v[0]).y < 0f)
            {
                for (var i = 0; i < sides; i++) (tri[i * 3 + 1], tri[i * 3 + 2]) = (tri[i * 3 + 2], tri[i * 3 + 1]);
            }
            mesh.vertices = v;
            mesh.triangles = tri;
            mesh.RecalculateNormals();
            return mesh;
        }

        private static void Kill(GameObject go)
        {
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }

    internal static class VillageVectors
    {
        public static Vector2 XZ(this Vector3 v) => new Vector2(v.x, v.z);
    }
}
