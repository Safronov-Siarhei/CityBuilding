using System.Collections.Generic;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using CityBuilder.UI;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Dungeon
{
    /// <summary>
    /// What the player sees of a dungeon (the user, 2026-11-01): the whole dungeon lies in
    /// DARKNESS, and it lifts only along the passages a hero walks -- never behind the rock beside
    /// him; a passage once walked stays lit with whatever is in it now.
    ///
    /// - Not the town's fog: its blanket lies 3.5 m over the highest rock and its holes slid off
    ///   the floor (one playtest saw no fog, another nothing but parchment). It is switched off
    ///   below. The heroes' own round reveal is off too (Hero.RevealAround): a circle sees through rock.
    /// - Every hero floods out over the FLOOR (the NavMesh, sampled once per cell) up to
    ///   dungeon_reveal_cells steps; each floor cell reached is opened for good with the walls
    ///   round it. A passage behind a wall is many steps away round it.
    /// - The darkness is drawn over the whole screen (Shaders/DungeonDark): each pixel's depth gives
    ///   its world cell, the explored mask says how dark it is -- floor, rock and monsters alike, and
    ///   the void past the map's edge black. It needs the depth texture, turned on for the main
    ///   camera here, in a dungeon only. Cells lift gently (eased over LiftSeconds).
    /// - The minimap is a picture of the same thing (the user: «миникарта работает
    ///   неправильно» -- the town's minimap camera saw rock tops and the streamer's holes): one
    ///   texel a cell, the explored floor light, its walls dark, the unknown black, the
    ///   heroes gold and the monsters in the open red. Redrawn a few times a second.
    /// </summary>
    public class DungeonFog : MonoBehaviour
    {
        private const float RevealEverySeconds = 0.25f;
        private const float DrawEverySeconds = 0.4f;
        private const float LiftSeconds = 0.6f;

        // the unknown is the dungeon's darkness on the minimap too
        private static readonly Color32 Unknown = new Color32(16, 14, 20, 255);
        private static readonly Color32 Floor = new Color32(222, 204, 160, 255);
        private static readonly Color32 Wall = new Color32(70, 60, 52, 255);
        private static readonly Color32 HeroDot = new Color32(255, 214, 64, 255);
        private static readonly Color32 MonsterDot = new Color32(214, 40, 30, 255);
        private static readonly Color32 ExitDot = new Color32(90, 220, 110, 255);

        public static DungeonFog Instance { get; private set; }

        private bool[] _floor;
        private bool[] _open;
        private int _size;
        private Vector2Int _origin;
        private float _cell = 1f;
        private int _reach = 7;
        private float _revealIn, _drawIn;
        private Texture2D _picture;
        private Texture2D _mask;
        private byte[] _shown;
        private bool _easing;
        private GameObject _darkness;
        private Color32[] _pixels;
        private readonly Queue<int> _queue = new Queue<int>();
        private int[] _steps;
        private readonly List<int> _touched = new List<int>();
        // the lakes: each cell's lake (0 = none), each lake's cells, and whether it is in view yet
        private int[] _lakeOf;
        private readonly List<List<int>> _lakes = new List<List<int>> { null };
        private readonly List<bool> _lakeOpen = new List<bool> { true };
        // a hero this many cells from a lake's water sees the whole lake
        private const int LakeSightCells = 2;
        // and the rock this many cells round its water (the statue's islet, the shore)
        private const int LakeRimCells = 2;

        /// <summary>The minimap's picture -- for the tests.</summary>
        public Texture2D Picture => _picture;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_picture != null) Destroy(_picture);
            if (_mask != null) Destroy(_mask);
        }

        /// <summary>Samples the floor, switches the fog on and puts the picture on the minimap. `half`: half the dungeon's side, world units, square round the origin.</summary>
        public void Begin(float half)
        {
            var grid = GridManager.Instance;
            if (grid == null) return;
            _cell = grid.CellSize;
            var balance = BalanceConfig.Instance;
            _reach = balance != null ? balance.DungeonRevealCells : 7;
            _origin = grid.WorldToCell(new Vector3(-half, 0f, -half));
            _size = Mathf.Max(1, Mathf.CeilToInt(half * 2f / _cell));
            _floor = new bool[_size * _size];
            _open = new bool[_size * _size];
            _steps = new int[_size * _size];
            var ground = grid.GroundHeight;
            for (var z = 0; z < _size; z++)
            for (var x = 0; x < _size; x++)
            {
                var at = grid.GetFootprintCenterWorld(_origin + new Vector2Int(x, z), Vector2Int.one);
                at.y = ground;
                _floor[z * _size + x] = NavMesh.SamplePosition(at, out var hit, _cell * 0.45f, NavMesh.AllAreas)
                    && hit.position.y < ground + 0.6f;
            }

            FindLakes();

            // the town's fog has no place below: the darkness is the dungeon's own
            var fog = FogOfWarManager.Instance;
            if (fog != null) fog.DisabledCheat = true;
            _shown = new byte[_size * _size];
            _mask = new Texture2D(_size, _size, TextureFormat.R8, false, true) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "DungeonDarkMask" };
            _mask.SetPixelData(_shown, 0);
            _mask.Apply(false);
            var corner = grid.GetFootprintCenterWorld(_origin, Vector2Int.one) - new Vector3(_cell * 0.5f, 0f, _cell * 0.5f);
            Darken(new Vector4(corner.x, corner.z, 1f / (_size * _cell), 1f / (_size * _cell)));

            _picture = new Texture2D(_size, _size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "DungeonMinimap" };
            _pixels = new Color32[_size * _size];
            var minimap = FindAnyObjectByType<MinimapCameraUpdater>();
            if (minimap != null) minimap.ShowPicture(_picture, Vector3.zero, half);
            Reveal();
            // what is lit on arrival is lit at once, not faded in
            for (var i = 0; i < _shown.Length; i++) if (_open[i]) _shown[i] = 255;
            _easing = false;
            _mask.SetPixelData(_shown, 0);
            _mask.Apply(false);
            Draw();
        }

        /// <summary>The darkness over the screen: a quad on the main camera drawn last, and the camera's depth texture on.</summary>
        private void Darken(Vector4 rect)
        {
            var cam = Camera.main;
            var raids = OrcRaidManager.Instance;
            var source = raids != null ? raids.DungeonDarkMaterial : null;
            if (cam == null || source == null) return;
            // the camera may not carry URP's data yet: this adds it when missing
            UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(cam).requiresDepthTexture = true;
            // the void past the map's edge black too, whatever the darkness lets through
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            var material = new Material(source) { name = "DungeonDark (runtime)" };
            material.SetTexture("_DarkMask", _mask);
            material.SetVector("_DarkRect", rect);
            material.SetFloat("_Strength", 1f);
            _darkness = new GameObject("DungeonDarkness", typeof(MeshFilter), typeof(MeshRenderer));
            _darkness.transform.SetParent(cam.transform, false);
            _darkness.transform.localPosition = Vector3.forward * (cam.nearClipPlane + 0.5f);
            var mesh = new Mesh
            {
                name = "DungeonDarkQuad",
                vertices = new[] { new Vector3(-1f, -1f, 0f), new Vector3(1f, -1f, 0f), new Vector3(1f, 1f, 0f), new Vector3(-1f, 1f, 0f) },
                triangles = new[] { 0, 1, 2, 0, 2, 3 },
            };
            // the shader puts it over the whole screen whatever the camera does: never cull it
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000f);
            _darkness.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = _darkness.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        /// <summary>How light a cell is drawn now, 0..1 (eased) -- for the tests.</summary>
        public float Lightness(Vector2Int cell)
        {
            var i = Index(cell);
            return i >= 0 && _shown != null ? _shown[i] / 255f : 0f;
        }

        /// <summary>Has a hero's flood reached this cell (or the ring round it)? -- for the tests.</summary>
        public bool IsOpen(Vector2Int cell)
        {
            var i = Index(cell);
            return i >= 0 && _open[i];
        }

        /// <summary>Is this cell floor a hero can stand on? -- for the tests.</summary>
        public bool IsFloor(Vector2Int cell)
        {
            var i = Index(cell);
            return i >= 0 && _floor[i];
        }

        private int Index(Vector2Int cell)
        {
            if (_floor == null) return -1;
            var x = cell.x - _origin.x;
            var z = cell.y - _origin.y;
            return x < 0 || z < 0 || x >= _size || z >= _size ? -1 : z * _size + x;
        }

        private void Update()
        {
            if (_floor == null) return;
            _revealIn -= Time.deltaTime;
            if (_revealIn <= 0f)
            {
                _revealIn = RevealEverySeconds;
                Reveal();
            }
            if (_easing) Ease(Time.deltaTime);
            _drawIn -= Time.deltaTime;
            if (_drawIn <= 0f)
            {
                _drawIn = DrawEverySeconds;
                Draw();
            }
        }

        /// <summary>
        /// The cheat window's «Снять туман подземелья» (the user, 2026-11-04: not to run the whole
        /// map to see it): every cell open, faded in as a reveal is.
        /// </summary>
        public void RevealAll()
        {
            if (_open == null) return;
            for (var i = 0; i < _open.Length; i++) _open[i] = true;
            _easing = true;
            Draw();
        }

        /// <summary>Each standing hero floods the floor round him, _reach steps out.</summary>
        private void Reveal()
        {
            var roster = HeroRoster.Instance;
            var grid = GridManager.Instance;
            if (roster == null || grid == null) return;
            foreach (var hero in roster.Heroes)
            {
                if (hero == null || hero.IsFallen) continue;
                var cell = grid.WorldToCell(hero.transform.position);
                Flood(cell);
                SeeLakesNear(cell);
            }
        }

        /// <summary>
        /// Every lake below: the cells under the map's water mesh (its bed is ground, so the
        /// ground cannot tell), joined together.
        /// </summary>
        private void FindLakes()
        {
            _lakeOf = new int[_size * _size];
            var wet = WaterCells();
            if (wet == null) return;
            var queue = new Queue<int>();
            for (var start = 0; start < _lakeOf.Length; start++)
            {
                if (_lakeOf[start] != 0 || _floor[start] || !wet[start]) continue;
                var id = _lakes.Count;
                var cells = new List<int>();
                var touchesEdge = false;
                _lakeOf[start] = id;
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    var i = queue.Dequeue();
                    cells.Add(i);
                    int x = i % _size, z = i / _size;
                    if (x == 0 || z == 0 || x == _size - 1 || z == _size - 1) touchesEdge = true;
                    for (var k = 0; k < 4; k++)
                    {
                        var n = Neighbour(i, k);
                        if (n < 0 || _lakeOf[n] != 0 || _floor[n] || !wet[n]) continue;
                        _lakeOf[n] = id;
                        queue.Enqueue(n);
                    }
                }
                _lakes.Add(cells);
                // the void round the map is "water" too: never a lake
                _lakeOpen.Add(touchesEdge);
            }
        }

        /// <summary>The cells whose middles the water mesh covers (its triangles laid flat on the cells), or null with no water.</summary>
        private bool[] WaterCells()
        {
            var water = MeshMapApplier.Instance != null ? MeshMapApplier.Instance.WaterInstance : null;
            if (water == null) return null;
            var wet = new bool[_size * _size];
            var grid = GridManager.Instance;
            var corner = grid.GetFootprintCenterWorld(_origin, Vector2Int.one);
            foreach (var filter in water.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh;
                if (mesh == null || !mesh.isReadable) continue;
                var vertices = mesh.vertices;
                var triangles = mesh.triangles;
                var toWorld = filter.transform.localToWorldMatrix;
                for (var t = 0; t < triangles.Length; t += 3)
                {
                    Vector2 a = Flat2(toWorld.MultiplyPoint3x4(vertices[triangles[t]])), b = Flat2(toWorld.MultiplyPoint3x4(vertices[triangles[t + 1]])),
                        c = Flat2(toWorld.MultiplyPoint3x4(vertices[triangles[t + 2]]));
                    // the cells whose middles fall in this triangle
                    int x0 = Mathf.FloorToInt((Mathf.Min(a.x, Mathf.Min(b.x, c.x)) - corner.x) / _cell), x1 = Mathf.CeilToInt((Mathf.Max(a.x, Mathf.Max(b.x, c.x)) - corner.x) / _cell);
                    int z0 = Mathf.FloorToInt((Mathf.Min(a.y, Mathf.Min(b.y, c.y)) - corner.z) / _cell), z1 = Mathf.CeilToInt((Mathf.Max(a.y, Mathf.Max(b.y, c.y)) - corner.z) / _cell);
                    for (var z = Mathf.Max(0, z0); z <= Mathf.Min(_size - 1, z1); z++)
                    for (var x = Mathf.Max(0, x0); x <= Mathf.Min(_size - 1, x1); x++)
                    {
                        var p = new Vector2(corner.x + x * _cell, corner.z + z * _cell);
                        if (InTriangle(p, a, b, c)) wet[z * _size + x] = true;
                    }
                }
            }
            return wet;
        }

        private static Vector2 Flat2(Vector3 v) => new Vector2(v.x, v.z);

        private static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float Side(Vector2 u, Vector2 v, Vector2 w) => (u.x - w.x) * (v.y - w.y) - (v.x - w.x) * (u.y - w.y);
            var d1 = Side(p, a, b);
            var d2 = Side(p, b, c);
            var d3 = Side(p, c, a);
            var negative = d1 < 0f || d2 < 0f || d3 < 0f;
            var positive = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(negative && positive);
        }

        /// <summary>
        /// A hero come to a lake's shore sees all of it, the statue in its middle included (the
        /// user, 2026-11-02: «открытие всего озера если игрок подошёл к какому-нибудь берегу,
        /// чтобы статуя не была вечно сокрыта»).
        /// </summary>
        private void SeeLakesNear(Vector2Int cell)
        {
            if (_lakeOf == null || _lakes.Count <= 1) return;
            var centre = Index(cell);
            if (centre < 0) return;
            int cx = centre % _size, cz = centre / _size;
            for (var dz = -LakeSightCells; dz <= LakeSightCells; dz++)
            for (var dx = -LakeSightCells; dx <= LakeSightCells; dx++)
            {
                int x = cx + dx, z = cz + dz;
                if (x < 0 || z < 0 || x >= _size || z >= _size) continue;
                var lake = _lakeOf[z * _size + x];
                if (lake == 0 || _lakeOpen[lake]) continue;
                _lakeOpen[lake] = true;
                foreach (var i in _lakes[lake])
                {
                    int ix = i % _size, iz = i / _size;
                    for (var rz = -LakeRimCells; rz <= LakeRimCells; rz++)
                    for (var rx = -LakeRimCells; rx <= LakeRimCells; rx++)
                    {
                        int nx = ix + rx, nz = iz + rz;
                        if (nx < 0 || nz < 0 || nx >= _size || nz >= _size) continue;
                        var n = nz * _size + nx;
                        if (!_open[n]) Open(n);
                    }
                }
            }
        }

        private void Flood(Vector2Int from)
        {
            var start = Index(from);
            if (start < 0) return;
            // a hero standing on a cell the sampling missed (a slope, a doorway) still sees
            if (!_floor[start])
            {
                Open(start);
                for (var k = 0; k < 4; k++)
                {
                    var n = Neighbour(start, k);
                    if (n >= 0 && _floor[n]) { start = n; break; }
                }
                if (!_floor[start]) return;
            }
            _queue.Clear();
            _touched.Clear();
            _steps[start] = 1;
            _touched.Add(start);
            _queue.Enqueue(start);
            while (_queue.Count > 0)
            {
                var i = _queue.Dequeue();
                if (!_open[i]) Open(i);
                if (_steps[i] > _reach) continue;
                for (var k = 0; k < 4; k++)
                {
                    var n = Neighbour(i, k);
                    if (n < 0 || !_floor[n] || _steps[n] != 0) continue;
                    _steps[n] = _steps[i] + 1;
                    _touched.Add(n);
                    _queue.Enqueue(n);
                }
            }
            foreach (var i in _touched) _steps[i] = 0;
        }

        private void Open(int i)
        {
            _open[i] = true;
            _easing = true;
            // the walls beside an opened floor cell are known too (the minimap draws them)
            for (var k = 0; k < 8; k++)
            {
                var n = Neighbour(i, k);
                if (n >= 0 && !_floor[n]) _open[n] = true;
            }
        }

        /// <summary>Opened cells lift out of the dark over LiftSeconds; the mask goes up only while some cell is still lifting.</summary>
        private void Ease(float dt)
        {
            var step = Mathf.Max(1, Mathf.RoundToInt(255f * dt / LiftSeconds));
            var any = false;
            for (var i = 0; i < _shown.Length; i++)
            {
                if (!_open[i] || _shown[i] == 255) continue;
                _shown[i] = (byte)Mathf.Min(255, _shown[i] + step);
                any = true;
            }
            _easing = any;
            if (_mask == null) return;
            _mask.SetPixelData(_shown, 0);
            _mask.Apply(false);
        }

        private int Neighbour(int i, int k)
        {
            int x = i % _size, z = i / _size;
            switch (k)
            {
                case 0: x++; break;
                case 1: x--; break;
                case 2: z++; break;
                case 3: z--; break;
                case 4: x++; z++; break;
                case 5: x--; z++; break;
                case 6: x++; z--; break;
                default: x--; z--; break;
            }
            return x < 0 || z < 0 || x >= _size || z >= _size ? -1 : z * _size + x;
        }

        private void Draw()
        {
            if (_picture == null) return;
            for (var i = 0; i < _pixels.Length; i++)
                _pixels[i] = !_open[i] ? Unknown : _floor[i] ? Floor : Wall;
            var grid = GridManager.Instance;
            if (grid != null)
            {
                var mode = DungeonMode.Instance;
                if (mode != null) Dot(grid, mode.ExitSpot, ExitDot, false);
                foreach (var orc in OrcUnit.All)
                    if (orc != null && orc.CurrentHealth > 0) Dot(grid, orc.transform.position, MonsterDot, true);
                var roster = HeroRoster.Instance;
                if (roster != null)
                    foreach (var hero in roster.Heroes)
                        if (hero != null && !hero.IsFallen) Dot(grid, hero.transform.position, HeroDot, false);
            }
            _picture.SetPixels32(_pixels);
            _picture.Apply(false);
        }

        /// <summary>A 2x2 dot at a world point; `onlyIfOpen`: only where the heroes have been (a monster in the dark stays hidden).</summary>
        private void Dot(GridManager grid, Vector3 world, Color32 colour, bool onlyIfOpen)
        {
            var i = Index(grid.WorldToCell(world));
            if (i < 0 || (onlyIfOpen && !_open[i])) return;
            int x = i % _size, z = i / _size;
            for (var dz = 0; dz <= 1; dz++)
            for (var dx = 0; dx <= 1; dx++)
            {
                int px = x + dx, pz = z + dz;
                if (px < _size && pz < _size) _pixels[pz * _size + px] = colour;
            }
        }
    }
}
