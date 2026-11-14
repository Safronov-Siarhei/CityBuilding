using System.Collections.Generic;
using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Grid;
using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// The questions the sandbox fires ask of the town (the user, 2026-11-14): what burns, what touches
    /// what, where the water is.
    /// </summary>
    public static class FireRules
    {
        /// <summary>Fires are the sandbox's only (Infirmary.Active: not Invasion, tutorial or a campaign town).</summary>
        public static bool Active => Infirmary.Active;

        /// <summary>
        /// A player's standing, finished building that can catch: its fireproof_level is 0 (always
        /// burns) or above its level (a wooden fence burns, the stone one from level 3 does not).
        /// </summary>
        public static bool Burns(BuildingInstance b)
        {
            if (b == null || b.Data == null || b.IsBot || !b.IsOperational || b.CurrentHealth <= 0) return false;
            var proof = b.Data.fireproofLevel;
            return proof <= 0 || b.Level < proof;
        }

        /// <summary>The cells a building covers: origin + its turned footprint.</summary>
        public static RectInt Cells(BuildingInstance b)
        {
            var size = b.RotatedFootprint();
            return new RectInt(b.OriginCell, new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y)));
        }

        /// <summary>No gap between them: their footprints share an edge or a corner. A one-cell gap stops a fire.</summary>
        public static bool Touching(BuildingInstance a, BuildingInstance b)
        {
            if (a == null || b == null || a == b) return false;
            var ra = Cells(a);
            var rb = Cells(b);
            return ra.xMin <= rb.xMax && rb.xMin <= ra.xMax && ra.yMin <= rb.yMax && rb.yMin <= ra.yMax;
        }

        private static readonly Dictionary<BuildingInstance, (bool wet, float until)> WetCache = new Dictionary<BuildingInstance, (bool, float)>();

        /// <summary>
        /// Water within fire_water_cells of the building: a Колодец, or the sea / a river / a lake cell
        /// of the map. Remembered for a minute (the town changes slowly; this is asked once a game day).
        /// </summary>
        public static bool WaterNear(BuildingInstance b)
        {
            if (b == null) return false;
            if (WetCache.TryGetValue(b, out var c) && c.until > Time.time) return c.wet;
            var wet = ComputeWaterNear(b, BalanceConfig.Instance.FireWaterCells);
            WetCache[b] = (wet, Time.time + 60f);
            return wet;
        }

        private static bool ComputeWaterNear(BuildingInstance b, int reach)
        {
            var area = Cells(b);
            foreach (var other in BuildingInstance.All)
            {
                if (other == null || other.IsBot || other.Data == null || other.Data.buildingName != "Well" || !other.IsOperational) continue;
                if (CellGap(area, Cells(other)) <= reach) return true;
            }
            var map = Maps.MeshMapApplier.Instance;
            if (map == null) return false;
            for (var x = area.xMin - reach; x < area.xMax + reach; x++)
                for (var y = area.yMin - reach; y < area.yMax + reach; y++)
                    if (map.IsWaterCell(new Vector2Int(x, y))) return true;
            return false;
        }

        private static int CellGap(RectInt a, RectInt b)
        {
            var dx = Mathf.Max(0, Mathf.Max(a.xMin - b.xMax + 1, b.xMin - a.xMax + 1));
            var dy = Mathf.Max(0, Mathf.Max(a.yMin - b.yMax + 1, b.yMin - a.yMax + 1));
            return Mathf.Max(dx, dy);
        }

        /// <summary>
        /// Where a firefighter fills his bucket: the nearest Колодец's door on the island, or the
        /// nearest dry cell beside the sea or a river -- whichever is closer. Null when there is none
        /// within reach.
        /// </summary>
        public static Vector3? NearestWater(Vector3 from, int island)
        {
            var grid = GridManager.Instance;
            var visuals = CitizenVisualsManager.Instance;
            Vector3? best = null;
            var bestD = float.MaxValue;
            foreach (var w in BuildingInstance.All)
            {
                if (w == null || w.IsBot || w.Data == null || w.Data.buildingName != "Well" || !w.IsOperational) continue;
                if (!HousePeople.Same(HousePeople.IslandOf(w), island)) continue;
                var at = visuals != null ? visuals.DoorOf(w) : w.transform.position;
                var d = (at - from).sqrMagnitude;
                if (d < bestD) { bestD = d; best = at; }
            }
            var map = Maps.MeshMapApplier.Instance;
            if (grid == null || map == null) return best;
            var start = grid.WorldToCell(from);
            const int reach = 30;
            for (var r = 1; r <= reach; r++)
            {
                // a ring at a time: the first shore found within the ring beats the rest further out
                var ringBest = float.MaxValue;
                Vector3? ringAt = null;
                for (var dx = -r; dx <= r; dx++)
                    for (var dy = -r; dy <= r; dy++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                        var cell = new Vector2Int(start.x + dx, start.y + dy);
                        if (map.IsWaterCell(cell) || !Shore(map, cell)) continue;
                        var at = grid.CellToWorld(cell);
                        var d = (at - from).sqrMagnitude;
                        if (d < ringBest) { ringBest = d; ringAt = at; }
                    }
                if (ringAt.HasValue)
                {
                    if (ringBest < bestD) best = ringAt;
                    return best;
                }
                if (best.HasValue && r * grid.CellSize * r * grid.CellSize > bestD) return best;
            }
            return best;
        }

        private static bool Shore(Maps.MeshMapApplier map, Vector2Int c) =>
            map.IsWaterCell(c + Vector2Int.right) || map.IsWaterCell(c + Vector2Int.left) ||
            map.IsWaterCell(c + Vector2Int.up) || map.IsWaterCell(c + Vector2Int.down);

        /// <summary>
        /// The nearest cell to `near` no building stands on -- a road, a Площадь or a Торговая
        /// площадь count as open ground -- and not water: where a body is laid so the player sees it
        /// (the user, 2026-11-14: never inside the burning house or its neighbour).
        /// </summary>
        public static Vector3 OpenGroundNear(Vector3 near)
        {
            var grid = GridManager.Instance;
            if (grid == null) return near;
            var map = Maps.MeshMapApplier.Instance;
            var start = grid.WorldToCell(near);
            for (var r = 0; r <= 12; r++)
            {
                var bestD = float.MaxValue;
                Vector3? found = null;
                for (var dx = -r; dx <= r; dx++)
                    for (var dy = -r; dy <= r; dy++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                        var cell = new Vector2Int(start.x + dx, start.y + dy);
                        if (map != null && map.IsWaterCell(cell)) continue;
                        if (!grid.IsWithinBounds(cell, Vector2Int.one)) continue;
                        if (!grid.IsAreaFree(cell, Vector2Int.one) && !OpenGroundBuilding(cell)) continue;
                        var at = grid.CellToWorld(cell);
                        var d = (at - near).sqrMagnitude;
                        if (d < bestD) { bestD = d; found = at; }
                    }
                if (found.HasValue)
                {
                    var p = found.Value;
                    if (UnityEngine.AI.NavMesh.SamplePosition(p, out var hit, 1f, UnityEngine.AI.NavMesh.AllAreas)) p = hit.position;
                    return p;
                }
            }
            return near;
        }

        private static bool OpenGroundBuilding(Vector2Int cell)
        {
            foreach (var b in BuildingInstance.All)
            {
                if (b == null || b.Data == null) continue;
                if (!Cells(b).Contains(cell)) continue;
                var id = b.Data.buildingName;
                return b.Data.isRoad || id == "Square" || id == "TownSquare";
            }
            return false;
        }
    }
}
