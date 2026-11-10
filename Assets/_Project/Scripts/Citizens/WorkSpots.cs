using System;
using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Grid;
using CityBuilder.Maps;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Citizens
{
    /// <summary>
    /// Where a worker with no tree or rock does his work, and with what (the user, 2026-11-02):
    /// the ploughman walks the Farm's field hoeing it, the picker goes from tree to tree in the
    /// Orchard with a basket, the swineherd tips his pail into the Pig Farm's troughs, the
    /// fisherman stands on the shore within the hut's fishing reach casting into the water. A
    /// woodcutter's axe and a quarryman's pick work at their tree or rock (CitizenAgent).
    /// </summary>
    public static class WorkSpots
    {
        private static readonly Dictionary<ProductionBuilding, List<CitizenAgent.WorkSpot>> Shores = new Dictionary<ProductionBuilding, List<CitizenAgent.WorkSpot>>();
        private static readonly List<ProductionBuilding> Gone = new List<ProductionBuilding>();
        // how far inside its plot a field/orchard/pen worker keeps (cells)
        private const float PlotInset = 0.3f;
        private const int MaxShoreSpots = 48;

        /// <summary>The trade of a workplace's men, by its building.</summary>
        public static WorkKind KindOf(string buildingId)
        {
            switch (buildingId)
            {
                case "Sawmill": return WorkKind.Chop;
                case "Quarry": return WorkKind.Mine;
                case BuildingIds.FisherHut: return WorkKind.Fish;
                case BuildingIds.Farm: return WorkKind.Plough;
                case BuildingIds.Orchard: return WorkKind.Pick;
                case BuildingIds.PigFarm: return WorkKind.Feed;
                default: return WorkKind.None;
            }
        }

        /// <summary>The spots of a workplace whose men work on its own ground or shore, or null.</summary>
        public static Func<CitizenAgent.WorkSpot> For(ProductionBuilding building)
        {
            var instance = building != null ? building.Instance : null;
            var grid = GridManager.Instance;
            if (instance == null || grid == null) return null;
            switch (building.BuildingId)
            {
                case BuildingIds.Farm:
                case BuildingIds.Orchard:
                case BuildingIds.PigFarm:
                {
                    var size = instance.RotatedFootprint();
                    var origin = grid.CellToWorld(instance.OriginCell);
                    var cell = grid.CellSize;
                    var centre = grid.GetFootprintCenterWorld(instance.OriginCell, size);
                    var pen = building.BuildingId == BuildingIds.PigFarm;
                    return () =>
                    {
                        var at = new Vector3(origin.x + UnityEngine.Random.Range(PlotInset, size.x - PlotInset) * cell, centre.y,
                            origin.z + UnityEngine.Random.Range(PlotInset, size.y - PlotInset) * cell);
                        // the swineherd faces the middle of the pen; the others down a furrow, along a row
                        var face = pen ? centre : at + Quaternion.Euler(0f, UnityEngine.Random.Range(0, 4) * 90f, 0f) * Vector3.forward;
                        return new CitizenAgent.WorkSpot { At = at, Face = face };
                    };
                }
                case BuildingIds.FisherHut:
                {
                    var spots = ShoreOf(building, grid);
                    if (spots.Count == 0) return null;
                    return () => spots[UnityEngine.Random.Range(0, spots.Count)];
                }
                default:
                    return null;
            }
        }

        /// <summary>A water cell whose ground lies under the sea's surface: one a line can drop into.</summary>
        private static bool Wet(MeshMapApplier map, GridManager grid, Vector2Int cell)
        {
            if (!map.IsWaterCell(cell)) return false;
            var at = grid.GetFootprintCenterWorld(cell, Vector2Int.one);
            return !map.TryGetGroundHeight(at, out var ground) || ground < Ships.ShipManager.WaterLevel - 0.15f;
        }

        /// <summary>
        /// Land cells at the water within the hut's fishing reach (fish_hut_reach_meters), on its own
        /// island, that a walker can stand on -- each facing its water. Found once per hut.
        /// </summary>
        private static List<CitizenAgent.WorkSpot> ShoreOf(ProductionBuilding hut, GridManager grid)
        {
            if (Shores.TryGetValue(hut, out var known)) return known;
            Gone.Clear();
            foreach (var key in Shores.Keys)
                if (key == null) Gone.Add(key);
            foreach (var key in Gone) Shores.Remove(key);

            var list = new List<CitizenAgent.WorkSpot>();
            var map = MeshMapApplier.Instance;
            var config = Core.BalanceConfig.Instance;
            if (map != null && config != null)
            {
                var reach = Mathf.Max(1, Mathf.RoundToInt(config.FishHutReachMeters / grid.CellSize));
                var home = grid.WorldToCell(hut.transform.position);
                var steps = new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
                for (var dz = -reach; dz <= reach && list.Count < MaxShoreSpots; dz++)
                for (var dx = -reach; dx <= reach && list.Count < MaxShoreSpots; dx++)
                {
                    if (dx * dx + dz * dz > reach * reach) continue;
                    var c = home + new Vector2Int(dx, dz);
                    // he stands on land or the beach's dry-looking wet margin, never in real water
                    if (Wet(map, grid, c)) continue;
                    foreach (var step in steps)
                    {
                        // real water under the line (the user, 2026-11-10: he cast into the sand):
                        // a "water" cell on the beach's wet margin can be ground above the sea's
                        // surface, so the cell he faces must lie below it
                        if (!Wet(map, grid, c + step)) continue;
                        var at = grid.GetFootprintCenterWorld(c, Vector2Int.one);
                        if (!map.SameIsland(hut.transform.position, at)) break;
                        if (!NavMesh.SamplePosition(at, out var hit, grid.CellSize * 0.6f, NavMesh.AllAreas)) break;
                        var water = grid.GetFootprintCenterWorld(c + step, Vector2Int.one);
                        list.Add(new CitizenAgent.WorkSpot { At = hit.position, Face = new Vector3(water.x, hit.position.y, water.z) });
                        break;
                    }
                }
            }
            Shores[hut] = list;
            return list;
        }
    }
}
