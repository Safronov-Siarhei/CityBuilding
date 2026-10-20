using CityBuilder.Core;
using CityBuilder.Grid;
using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>
    /// Where the Town Hall may stand on a designed map (the user's rule, 2026-10-20): inside the
    /// map's start zone, and no nearer the orcs' camps and main base than
    /// town_hall_min_distance_to_orcs. A map with neither (Maps 1-3) lets it go anywhere.
    /// </summary>
    public static class StartZone
    {
        public static bool Allows(Vector2Int cell, Vector2Int footprint)
        {
            var map = MeshMapApplier.Instance != null ? MeshMapApplier.Instance.CurrentMap : null;
            var grid = GridManager.Instance;
            if (map == null || grid == null) return true;

            var centre = grid.GetFootprintCenterWorld(cell, footprint);
            var at = new Vector2(centre.x, centre.z);

            var zone = map.TownHallZone;
            if (zone.Length >= 3 && !Contains(zone, at)) return false;

            var minDistance = BalanceConfig.Instance.TownHallMinDistanceToOrcs;
            if (map.HasMainBaseSpot && Vector2.Distance(at, map.MainBaseSpot) < minDistance) return false;
            foreach (var camp in map.CampSpots)
            {
                if (Vector2.Distance(at, camp) < minDistance) return false;
            }
            return true;
        }

        /// <summary>The middle of the zone, on the ground plane (the average of its corners -- the zones are drawn convex).</summary>
        public static Vector3 Centre(Vector2[] zone)
        {
            var sum = Vector2.zero;
            foreach (var p in zone) sum += p;
            sum /= Mathf.Max(1, zone.Length);
            return new Vector3(sum.x, 0f, sum.y);
        }

        /// <summary>Whether a point is inside a polygon (even-odd rule; any winding).</summary>
        public static bool Contains(Vector2[] polygon, Vector2 p)
        {
            var inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                var a = polygon[i];
                var b = polygon[j];
                if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }
    }
}
