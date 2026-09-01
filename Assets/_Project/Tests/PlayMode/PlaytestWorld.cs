using CityBuilder.Buildings;
using CityBuilder.Grid;
using CityBuilder.Maps;
using UnityEngine;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// Puts real buildings on the real map from a test, the same way BuildingPlacer does it minus
    /// the player: pick a catalogue entry, instantiate its prefab, initialise it, reserve its cells.
    ///
    /// Deliberately NOT a reimplementation of placement rules -- it skips the cost, prerequisite and
    /// water checks on purpose (a test decides what it wants standing where), but everything that
    /// happens to a building once it exists (fence registration, storage capacity, NavMesh carving,
    /// fog reveal) runs through exactly the same Initialize the game uses. That is the half these
    /// tests are here to watch.
    /// </summary>
    public static class PlaytestWorld
    {
        /// <summary>A catalogue entry by its stable id ("Fence", "Barn", ...), taken from the scene's own building lists rather than from AssetDatabase, which does not exist in a player. The Town Hall is not in the hotbar, so it is looked up separately.</summary>
        public static BuildingData Building(string buildingName)
        {
            var placer = Object.FindAnyObjectByType<BuildingPlacer>();
            if (placer == null) return null;

            foreach (var data in placer.AvailableBuildings)
            {
                if (data != null && data.buildingName == buildingName) return data;
            }

            var mandatory = placer.MandatoryFirstBuilding;
            return mandatory != null && mandatory.buildingName == buildingName ? mandatory : null;
        }

        public static BuildingInstance Place(BuildingData data, Vector2Int cell, int rotationSteps = 0)
        {
            var grid = GridManager.Instance;
            var footprint = RotatedFootprint(data, rotationSteps);
            var center = grid.GetFootprintCenterWorld(cell, footprint);

            var instance = Object.Instantiate(data.prefab, center, Quaternion.Euler(0f, rotationSteps * 90f, 0f));
            var buildingInstance = instance.GetComponent<BuildingInstance>();
            if (buildingInstance == null) buildingInstance = instance.AddComponent<BuildingInstance>();
            buildingInstance.Initialize(data, cell, rotationSteps);

            grid.SetAreaOccupied(cell, footprint, true);
            return buildingInstance;
        }

        /// <summary>
        /// Knocks a building down through the same path a raid does -- the point being that the
        /// destruction runs BuildingInstance.OnDestroy, which is what hands back the storage room it
        /// was providing and what re-shapes the fence segments either side of it.
        /// </summary>
        public static void Demolish(BuildingInstance building)
        {
            if (building != null) building.TryDamage(building.MaxHealth);
        }

        /// <summary>
        /// A free block as near the middle of the map as one can be found, searched outward in
        /// rings from the centre cell.
        ///
        /// FindFreeArea below starts at cell (1,1) and so hands back a spot in the map's CORNER,
        /// which is fine for a test that only needs somewhere to stand a building -- and wrong for
        /// any test about what happens AROUND a building. A Town Hall in the corner has half the
        /// compass off the edge of the map, and PortalOpeningTests spent a run proving that no
        /// placement rule can spread five portals around one.
        /// </summary>
        public static Vector2Int FindFreeAreaNearTheMiddle(Vector2Int size)
        {
            var grid = GridManager.Instance;
            var centre = new Vector2Int((grid.GridSize.x - size.x) / 2, (grid.GridSize.y - size.y) / 2);
            var reach = Mathf.Max(grid.GridSize.x, grid.GridSize.y) / 2;

            for (var ring = 0; ring < reach; ring++)
            {
                for (var dy = -ring; dy <= ring; dy++)
                {
                    for (var dx = -ring; dx <= ring; dx++)
                    {
                        // Only the ring's own edge: the inside of it was covered by earlier rings.
                        if (ring > 0 && Mathf.Abs(dx) != ring && Mathf.Abs(dy) != ring) continue;

                        var origin = centre + new Vector2Int(dx, dy);
                        if (origin.x < 1 || origin.y < 1) continue;
                        if (origin.x + size.x >= grid.GridSize.x - 1 || origin.y + size.y >= grid.GridSize.y - 1) continue;
                        if (IsBuildableArea(origin, size)) return origin;
                    }
                }
            }

            return new Vector2Int(-1, -1);
        }

        /// <summary>The bottom-left cell of the first size-sized block of dry, unoccupied, in-bounds cells -- somewhere a test can build without fighting the map's trees, rocks and water. In the map's CORNER; see FindFreeAreaNearTheMiddle when what surrounds the building matters.</summary>
        public static Vector2Int FindFreeArea(Vector2Int size)
        {
            var grid = GridManager.Instance;
            for (var y = 1; y + size.y < grid.GridSize.y - 1; y++)
            {
                for (var x = 1; x + size.x < grid.GridSize.x - 1; x++)
                {
                    var origin = new Vector2Int(x, y);
                    if (IsBuildableArea(origin, size)) return origin;
                }
            }
            return new Vector2Int(-1, -1);
        }

        private static bool IsBuildableArea(Vector2Int origin, Vector2Int size)
        {
            var grid = GridManager.Instance;
            if (!grid.IsAreaFree(origin, size)) return false;

            var water = MeshMapApplier.Instance;
            if (water == null) return true;

            for (var x = 0; x < size.x; x++)
            {
                for (var z = 0; z < size.y; z++)
                {
                    if (water.IsWaterCell(origin + new Vector2Int(x, z))) return false;
                }
            }
            return true;
        }

        public static Vector3 CellCenter(Vector2Int cell)
        {
            return GridManager.Instance.GetFootprintCenterWorld(cell, Vector2Int.one);
        }

        private static Vector2Int RotatedFootprint(BuildingData data, int rotationSteps)
        {
            return rotationSteps % 2 == 0
                ? data.footprintSize
                : new Vector2Int(data.footprintSize.y, data.footprintSize.x);
        }
    }
}
