using CityBuilder.Grid;
using CityBuilder.Maps;
using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// A stone footing under a building whose plot falls away below the town's level -- a hut on
    /// the sloping shore stood in the air over the sand (the user, 2026-11-02). Every building
    /// stands at the grid's one ground height; where the land under its plot is lower, a block of
    /// stone fills the gap down into it. Water buildings (half in the sea), roads and fences have none.
    /// Made once, when the building is made or loaded (BuildingInstance.Initialize).
    /// </summary>
    public static class BuildingFooting
    {
        /// <summary>The footing's stone (Materials/Rock), set by BuildingPlacer.</summary>
        public static Material Stone;

        // a drop smaller than this is the ground's own unevenness: no footing
        private const float MinDrop = 0.08f;
        private const float SinkBelow = 0.15f;
        private const float Inset = 0.96f;
        private const float RayFrom = 20f;

        public static void Add(BuildingInstance building)
        {
            var data = building != null ? building.Data : null;
            var grid = GridManager.Instance;
            var map = MeshMapApplier.Instance;
            if (data == null || grid == null || map == null || Stone == null) return;
            if (data.buildingName == Ships.ShipManager.ShipyardId)
            {
                SettleOnShore(building, grid, map);
                return;
            }
            if (data.isWaterCategory || data.keepSelectedAfterPlacement) return;
            var size = building.RotatedFootprint();
            var origin = grid.CellToWorld(building.OriginCell);
            var top = building.transform.position.y;
            var lowest = top;
            var cell = grid.CellSize;
            for (var ix = 0; ix <= 2; ix++)
            for (var iz = 0; iz <= 2; iz++)
            {
                var p = new Vector3(origin.x + size.x * cell * (0.05f + 0.45f * ix), top + RayFrom, origin.z + size.y * cell * (0.05f + 0.45f * iz));
                if (map.TryRaycastGround(new Ray(p, Vector3.down), out var hit)) lowest = Mathf.Min(lowest, hit.point.y);
            }
            if (top - lowest < MinDrop) return;

            var footing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            footing.name = "Footing";
            Object.Destroy(footing.GetComponent<Collider>());
            footing.transform.SetParent(building.transform, true);
            var bottom = lowest - SinkBelow;
            var centre = grid.GetFootprintCenterWorld(building.OriginCell, size);
            footing.transform.SetPositionAndRotation(new Vector3(centre.x, (top + bottom) * 0.5f, centre.z), Quaternion.identity);
            var parentScale = building.transform.lossyScale;
            footing.transform.localScale = new Vector3(size.x * cell * Inset / parentScale.x, (top - bottom) / parentScale.y, size.y * cell * Inset / parentScale.z);
            var renderer = footing.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = Stone;
        }

        /// <summary>
        /// The Shipyard stood in the air over a low beach (the user, 2026-11-05: it must stand in the
        /// ground and the water): it comes down to the HIGHEST land under its plot, never below the sea.
        /// A plot with no land above the sea keeps the town's height.
        /// </summary>
        private static void SettleOnShore(BuildingInstance building, GridManager grid, MeshMapApplier map)
        {
            var size = building.RotatedFootprint();
            var origin = grid.CellToWorld(building.OriginCell);
            var top = building.transform.position.y;
            var sea = Ships.ShipManager.WaterLevel;
            var highest = float.NegativeInfinity;
            var cell = grid.CellSize;
            for (var ix = 0; ix <= 4; ix++)
            for (var iz = 0; iz <= 4; iz++)
            {
                var p = new Vector3(origin.x + size.x * cell * (0.05f + 0.225f * ix), top + RayFrom, origin.z + size.y * cell * (0.05f + 0.225f * iz));
                if (map.TryRaycastGround(new Ray(p, Vector3.down), out var hit) && hit.point.y > sea) highest = Mathf.Max(highest, hit.point.y);
            }
            if (float.IsNegativeInfinity(highest) || top - highest < MinDrop) return;
            var at = building.transform.position;
            at.y = highest;
            building.transform.position = at;
        }
    }
}
