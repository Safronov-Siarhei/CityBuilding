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
    /// What the player sees of a dungeon (the user, 2026-11-01, screenshot 9): only the tunnels
    /// the heroes have reached, never one behind the rock beside them, and a tunnel once walked
    /// stays open with whatever is in it now; the rest lies under the map's fog.
    ///
    /// - The fog is the town's (FogOfWarManager, switched on here). The heroes' own round reveal
    ///   is off below (Hero.RevealAround): a circle sees through rock.
    /// - Instead every hero floods out over the FLOOR (the NavMesh, sampled once per cell) up to
    ///   dungeon_reveal_cells steps, and each floor cell reached is opened for good with a ring
    ///   round it, so the tunnel's walls show. A tunnel behind a wall is many steps away round it.
    /// - The minimap is a picture of the same thing (the user: «миникарта работает
    ///   неправильно» -- the town's minimap camera saw rock tops and the streamer's holes): one
    ///   texel a cell, the explored floor light, its walls dark, the unknown parchment, the
    ///   heroes gold and the monsters in the open red. Redrawn a few times a second.
    /// </summary>
    public class DungeonFog : MonoBehaviour
    {
        private const float RevealEverySeconds = 0.25f;
        private const float DrawEverySeconds = 0.4f;

        private static readonly Color32 Unknown = new Color32(118, 92, 60, 255);
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
        private Color32[] _pixels;
        private readonly Queue<int> _queue = new Queue<int>();
        private int[] _steps;
        private readonly List<int> _touched = new List<int>();

        /// <summary>The minimap's picture -- for the tests.</summary>
        public Texture2D Picture => _picture;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_picture != null) Destroy(_picture);
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

            var fog = FogOfWarManager.Instance;
            if (fog != null) fog.Activate();

            _picture = new Texture2D(_size, _size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "DungeonMinimap" };
            _pixels = new Color32[_size * _size];
            var minimap = FindAnyObjectByType<MinimapCameraUpdater>();
            if (minimap != null) minimap.ShowPicture(_picture, Vector3.zero, half);
            Reveal();
            Draw();
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
            _drawIn -= Time.deltaTime;
            if (_drawIn <= 0f)
            {
                _drawIn = DrawEverySeconds;
                Draw();
            }
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
                Flood(grid.WorldToCell(hero.transform.position));
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
            var fog = FogOfWarManager.Instance;
            while (_queue.Count > 0)
            {
                var i = _queue.Dequeue();
                if (!_open[i])
                {
                    Open(i);
                    // the ring round it, so the walls of the tunnel show
                    fog?.RevealPermanent(_origin + new Vector2Int(i % _size, i / _size), 1);
                }
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
            // the walls beside an opened floor cell are known too (the minimap draws them)
            for (var k = 0; k < 8; k++)
            {
                var n = Neighbour(i, k);
                if (n >= 0 && !_floor[n]) _open[n] = true;
            }
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
