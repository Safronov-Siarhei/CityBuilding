using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>
    /// Draws only the dungeon's squares near the camera (the user, 2026-10-31: «cut the map into
    /// squares so the player loads only the nearest ones»). The dungeon comes out of Blender cut
    /// into an 8 x 8 grid of squares (ground, relief and props, Tools/blender/dungeon1.py
    /// CHUNKS); the decor is in MapDecorRenderer's own squares. Every renderer under the dungeon
    /// is switched on only while its square is within reach of the point the camera looks at: the
    /// square under it and the ring round it, widened when the camera is pulled back so nothing
    /// pops in on screen.
    ///
    /// Only the RENDERERS are switched: colliders stay, so a click, the ground-height probe and
    /// the baked NavMesh know the whole labyrinth wherever the camera is.
    /// </summary>
    public class DungeonChunkStreamer : MonoBehaviour
    {
        private const float CheckEverySeconds = 0.2f;

        private readonly List<Renderer> _renderers = new List<Renderer>();
        private readonly List<Vector2> _centres = new List<Vector2>();
        private readonly List<Vector2> _halfSizes = new List<Vector2>();
        private float _square;
        private float _timer;
        private Transform _focus;

        /// <summary>Renderers drawn now, out of all the dungeon's.</summary>
        public int Drawn { get; private set; }
        public int Total => _renderers.Count;

        /// <summary>The side of one square, in cells.</summary>
        public float Square => _square;

        /// <summary>Takes every renderer under these roots; `square` is one square's side in cells.</summary>
        public void Initialize(IEnumerable<GameObject> roots, float square, Transform focus)
        {
            _square = Mathf.Max(1f, square);
            _focus = focus;
            _renderers.Clear();
            _centres.Clear();
            _halfSizes.Clear();
            foreach (var root in roots)
            {
                if (root == null) continue;
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    var b = renderer.bounds;
                    _renderers.Add(renderer);
                    _centres.Add(new Vector2(b.center.x, b.center.z));
                    _halfSizes.Add(new Vector2(b.extents.x, b.extents.z));
                }
            }
            Refresh();
        }

        /// <summary>Adds renderers made later (the decor's squares are built after the meshes).</summary>
        public void Add(GameObject root)
        {
            if (root == null) return;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (_renderers.Contains(renderer)) continue;
                var b = renderer.bounds;
                _renderers.Add(renderer);
                _centres.Add(new Vector2(b.center.x, b.center.z));
                _halfSizes.Add(new Vector2(b.extents.x, b.extents.z));
            }
            Refresh();
        }

        private void Update()
        {
            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0f) return;
            _timer = CheckEverySeconds;
            Refresh();
        }

        /// <summary>How far round the looked-at point squares are drawn, in cells: one square, and more as the camera backs off.</summary>
        public float Reach
        {
            get
            {
                var cam = Camera.main;
                var distance = cam != null && _focus != null ? Vector3.Distance(cam.transform.position, _focus.position) : 0f;
                // the strip in frame runs about 2x the camera's distance deep (RTSCameraController)
                var squares = Mathf.Max(1, Mathf.CeilToInt(distance * 1.2f / _square));
                return (squares + 0.5f) * _square;
            }
        }

        public void Refresh()
        {
            if (_renderers.Count == 0) return;
            var at = _focus != null ? new Vector2(_focus.position.x, _focus.position.z) : Vector2.zero;
            var reach = Reach;
            var drawn = 0;
            for (var i = 0; i < _renderers.Count; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null) continue;
                var d = _centres[i] - at;
                var on = Mathf.Abs(d.x) - _halfSizes[i].x <= reach && Mathf.Abs(d.y) - _halfSizes[i].y <= reach;
                if (renderer.enabled != on) renderer.enabled = on;
                if (on) drawn++;
            }
            Drawn = drawn;
        }
    }
}
