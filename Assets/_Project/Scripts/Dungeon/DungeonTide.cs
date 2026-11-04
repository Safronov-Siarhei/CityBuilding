using System.Collections.Generic;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Dungeon
{
    /// <summary>
    /// Dungeon 3's lava tide (the user, 2026-11-04: «Меняющийся путь»): the low passages and the
    /// fords have a basalt crust floor, dry at low tide. Every dungeon3_tide_low_seconds the lava
    /// rises over it (the `-Tide` skin climbs up through the floor for dungeon3_tide_warn_seconds,
    /// a log line warns), stays dungeon3_tide_high_seconds -- a hero on it then burns like in the
    /// lava (DungeonLava asks Floods) -- and sinks again. Where the floor is: the cells under the
    /// skin's triangles, read once from its mesh.
    /// </summary>
    public class DungeonTide : MonoBehaviour
    {
        private const float Cell = 0.5f;
        private const float HiddenDrop = 0.3f;      // world units under its place: under the crust
        private const float SinkSeconds = 2f;
        private const float StandAbove = 0.35f;     // a hero this far over the skin's top is not on the tide floor

        private static readonly HashSet<long> Flooded = new HashSet<long>();
        private static bool _high;
        private static float _top;

        private Transform _skin;
        private Vector3 _home;
        private float _clock;
        private int _phase;     // 0 low, 1 rising (warn), 2 high, 3 sinking

        /// <summary>The tide is up now.</summary>
        public static bool High => _high;

        /// <summary>A hero here stands in the risen tide.</summary>
        public static bool Floods(Vector3 position) =>
            _high && position.y < _top + StandAbove && Flooded.Contains(Key(position.x, position.z));

        private static long Key(float x, float z) =>
            ((long)Mathf.FloorToInt(x / Cell) << 32) ^ (uint)Mathf.FloorToInt(z / Cell);

        public void Begin(GameObject skin)
        {
            Flooded.Clear();
            _high = false;
            if (skin == null) return;
            _skin = skin.transform;
            _home = _skin.position;
            _top = float.NegativeInfinity;
            foreach (var filter in skin.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh;
                if (mesh == null || !mesh.isReadable) continue;
                var vertices = mesh.vertices;
                var triangles = mesh.triangles;
                var tf = filter.transform;
                var world = new Vector3[vertices.Length];
                for (var i = 0; i < vertices.Length; i++)
                {
                    world[i] = tf.TransformPoint(vertices[i]);
                    _top = Mathf.Max(_top, world[i].y);
                }
                for (var t = 0; t + 2 < triangles.Length; t += 3)
                {
                    var a = world[triangles[t]];
                    var b = world[triangles[t + 1]];
                    var c = world[triangles[t + 2]];
                    var x0 = Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x)) / Cell);
                    var x1 = Mathf.FloorToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x)) / Cell);
                    var z0 = Mathf.FloorToInt(Mathf.Min(a.z, Mathf.Min(b.z, c.z)) / Cell);
                    var z1 = Mathf.FloorToInt(Mathf.Max(a.z, Mathf.Max(b.z, c.z)) / Cell);
                    for (var x = x0; x <= x1; x++)
                    for (var z = z0; z <= z1; z++)
                        Flooded.Add(((long)x << 32) ^ (uint)z);
                }
            }
            Place(0f);
            _clock = BalanceConfig.Instance.TideLowSeconds;
        }

        private void OnDestroy()
        {
            Flooded.Clear();
            _high = false;
        }

        /// <summary>The skin `up` of the way from hidden (0) to its place (1).</summary>
        private void Place(float up)
        {
            if (_skin == null) return;
            _skin.position = _home + Vector3.down * (HiddenDrop * (1f - up));
            _skin.gameObject.SetActive(up > 0.01f);
        }

        private void Update()
        {
            if (_skin == null) return;
            var c = BalanceConfig.Instance;
            _clock -= Time.deltaTime;
            switch (_phase)
            {
                case 0:
                    if (_clock > 0f) return;
                    _phase = 1;
                    _clock = Mathf.Max(0.5f, c.TideWarnSeconds);
                    EventLogManager.Instance?.Post(GameEvent.At(Localization.Get("#log_dungeon_tide_rising"), EventSeverity.Warning,
                        EventGlyph.Skull, null, _home));
                    return;
                case 1:
                    Place(1f - Mathf.Clamp01(_clock / Mathf.Max(0.5f, c.TideWarnSeconds)));
                    if (_clock > 0f) return;
                    Place(1f);
                    _phase = 2;
                    _high = true;
                    _clock = c.TideHighSeconds;
                    return;
                case 2:
                    if (_clock > 0f) return;
                    _phase = 3;
                    _high = false;
                    _clock = SinkSeconds;
                    return;
                default:
                    Place(Mathf.Clamp01(_clock / SinkSeconds));
                    if (_clock > 0f) return;
                    Place(0f);
                    _phase = 0;
                    _clock = c.TideLowSeconds;
                    return;
            }
        }

        /// <summary>Floods at once -- for the tests.</summary>
        public void FloodNow()
        {
            Place(1f);
            _phase = 2;
            _high = true;
            _clock = BalanceConfig.Instance.TideHighSeconds;
        }
    }
}
