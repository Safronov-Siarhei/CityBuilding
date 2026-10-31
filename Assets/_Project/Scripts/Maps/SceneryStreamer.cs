using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>
    /// Draws the archipelago's trees and boulders only near the camera (the user, 2026-10-31: a
    /// denser forest, on the phone's budget). The map is cut into squares of
    /// scenery_stream_square_cells; a tree or a boulder registers into its square as it is made,
    /// and only the squares within reach of the point the camera looks at are drawn -- the square
    /// under it and a ring round it, widened as the camera pulls back so nothing pops in on screen
    /// (the same reach as DungeonChunkStreamer). Only a square that changes side is touched, so a
    /// still camera costs nothing.
    ///
    /// Only RENDERERS are switched: a tree's click box and its place on the grid stay, so a
    /// woodcutter's search and a tap know the whole forest wherever the camera is.
    /// </summary>
    public class SceneryStreamer : MonoBehaviour
    {
        private const float CheckEverySeconds = 0.2f;

        public static SceneryStreamer Instance { get; private set; }

        private readonly Dictionary<Vector2Int, List<Renderer>> _squares = new Dictionary<Vector2Int, List<Renderer>>();
        private readonly HashSet<Vector2Int> _drawn = new HashSet<Vector2Int>();
        private readonly List<Vector2Int> _scratch = new List<Vector2Int>();
        private float _square = 24f;
        private float _timer;
        private Transform _focus;
        private RectInt _shown;
        private bool _any;

        /// <summary>Squares drawn now, and all squares with something in them.</summary>
        public int DrawnSquares => _drawn.Count;
        public int TotalSquares => _squares.Count;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Initialize(Transform focus)
        {
            var balance = BalanceConfig.Instance;
            _square = Mathf.Max(4f, balance != null ? balance.SceneryStreamSquare : 24f);
            _focus = focus;
            _timer = 0f;
        }

        /// <summary>A tree or boulder just made: drawn now if its square is, hidden if not.</summary>
        public void Register(GameObject scenery)
        {
            if (scenery == null) return;
            var key = KeyOf(scenery.transform.position);
            if (!_squares.TryGetValue(key, out var list)) _squares[key] = list = new List<Renderer>();
            var on = _drawn.Contains(key) || !_any;
            foreach (var renderer in scenery.GetComponentsInChildren<Renderer>(true))
            {
                list.Add(renderer);
                renderer.enabled = on;
            }
        }

        private Vector2Int KeyOf(Vector3 world) =>
            new Vector2Int(Mathf.FloorToInt(world.x / _square), Mathf.FloorToInt(world.z / _square));

        private void Update()
        {
            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0f) return;
            _timer = CheckEverySeconds;
            Refresh();
        }

        public void Refresh()
        {
            if (_focus == null)
            {
                var rig = FindAnyObjectByType<CameraControl.RTSCameraController>();
                if (rig == null) return;
                _focus = rig.transform;
            }
            var cam = Camera.main;
            var distance = cam != null ? Vector3.Distance(cam.transform.position, _focus.position) : 0f;
            // the strip in frame runs about 2x the camera's distance deep (RTSCameraController)
            var reach = (Mathf.Max(1, Mathf.CeilToInt(distance * 1.2f / _square)) + 0.5f) * _square;
            var at = _focus.position;
            var min = KeyOf(at - new Vector3(reach, 0f, reach));
            var max = KeyOf(at + new Vector3(reach, 0f, reach));
            var shown = new RectInt(min.x, min.y, max.x - min.x + 1, max.y - min.y + 1);
            if (_any && shown.Equals(_shown)) return;
            _shown = shown;
            _any = true;

            // off: the drawn squares now out of reach
            _scratch.Clear();
            foreach (var key in _drawn)
                if (!shown.Contains(key)) _scratch.Add(key);
            foreach (var key in _scratch)
            {
                _drawn.Remove(key);
                Switch(key, false);
            }
            // on: the squares in reach not drawn yet (and every square once, the first time)
            foreach (var pair in _squares)
            {
                var inside = shown.Contains(pair.Key);
                if (inside && _drawn.Add(pair.Key)) Switch(pair.Key, true);
                else if (!inside && !_drawn.Contains(pair.Key) && pair.Value.Count > 0 && pair.Value[0] != null && pair.Value[0].enabled)
                    Switch(pair.Key, false);
            }
        }

        private void Switch(Vector2Int key, bool on)
        {
            if (!_squares.TryGetValue(key, out var list)) return;
            // a felled tree's renderers are gone: drop them as they turn up
            for (var i = list.Count - 1; i >= 0; i--)
            {
                var renderer = list[i];
                if (renderer == null)
                {
                    list.RemoveAt(i);
                    continue;
                }
                if (renderer.enabled != on) renderer.enabled = on;
            }
        }
    }
}
