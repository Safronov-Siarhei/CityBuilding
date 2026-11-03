using CityBuilder.Grid;
using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>
    /// Where a walker's feet are in a dungeon (the user, 2026-11-04, Dungeon 2: the hero walked ON
    /// the water and through a bridge's deck -- every walker is pinned to the plain's one height):
    /// the dungeon's own floor under the water surface -- knee-deep in the flooded passages, up on a
    /// bridge's deck -- read once from the ground mesh when the dungeon loads, one sample a cell,
    /// so a walker's height is a lookup and a blend, never a raycast (mobile).
    ///
    /// Wading is SLOWER (the user: «всё же добавь замедление в воде»): `dungeon_wade_speed_multiplier`.
    /// Buildings.Moat.StandHeightAt / SpeedAt ask here first, so every walker that wades a moat
    /// wades a dungeon's water the same way.
    /// </summary>
    public static class DungeonFloor
    {
        // how far below the water surface the floor must be to count as wading (cells)
        private const float WadeBelow = 0.03f;
        // a sample further than this from the plain's height is rock or a hole: no lift there
        private const float MaxLift = 0.6f;
        private const float MaxSink = 0.5f;

        private static float[] _heights;
        private static int _size;
        private static Vector2 _origin;
        private static float _ground;

        /// <summary>The water's surface height (world y) -- what a buoy floats on, where ripples spread.</summary>
        public static float WaterLevel { get; private set; }

        public static bool Active => _heights != null;

        /// <summary>Samples the dungeon's floor, `half` cells round the map's middle, under `waterLevel`.</summary>
        public static void Build(MeshMapApplier applier, float half, float waterLevel)
        {
            Clear();
            var grid = GridManager.Instance;
            if (applier == null || grid == null) return;
            _ground = grid.GroundHeight;
            WaterLevel = waterLevel;
            _size = Mathf.CeilToInt(half * 2f) + 1;
            _origin = new Vector2(-half, -half);
            var heights = new float[_size * _size];
            for (var z = 0; z < _size; z++)
            for (var x = 0; x < _size; x++)
            {
                var at = new Vector3(_origin.x + x, 0f, _origin.y + z);
                var y = applier.TryGetFloorHeight(at, out var floor) ? floor - _ground : 0f;
                heights[z * _size + x] = y > MaxLift || y < -MaxSink ? 0f : y;
            }
            _heights = heights;
        }

        public static void Clear()
        {
            _heights = null;
            WaterLevel = 0f;
        }

        /// <summary>How far above (+) or below (-) the plain's height a walker at `position` stands.</summary>
        public static float StandHeightAt(Vector3 position)
        {
            if (_heights == null) return 0f;
            var fx = Mathf.Clamp(position.x - _origin.x, 0f, _size - 1.001f);
            var fz = Mathf.Clamp(position.z - _origin.y, 0f, _size - 1.001f);
            int x = (int)fx, z = (int)fz;
            float tx = fx - x, tz = fz - z;
            var i = z * _size + x;
            var a = Mathf.Lerp(_heights[i], _heights[i + 1], tx);
            var b = Mathf.Lerp(_heights[i + _size], _heights[i + _size + 1], tx);
            return Mathf.Lerp(a, b, tz);
        }

        /// <summary>The walker's feet are under the dungeon's water.</summary>
        public static bool Wading(Vector3 position) =>
            _heights != null && _ground + StandHeightAt(position) < WaterLevel - WadeBelow;
    }
}
