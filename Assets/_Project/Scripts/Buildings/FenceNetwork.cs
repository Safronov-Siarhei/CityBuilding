using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// Which grid cells hold something a fence should join up with, and the fence segments that
    /// need re-shaping when that changes. Same shape as RoadNetwork (a per-cell registry fed by
    /// BuildingPlacer for new buildings and by GameSaveController for restored ones), because
    /// GridManager only knows that a cell is occupied, never by what.
    ///
    /// Membership comes from BuildingData.connectsToFences, so a Gate or a Tower can join the line
    /// later by setting that flag -- they register as connection points without owning a
    /// FenceAppearance of their own, since they don't change their model.
    ///
    /// Registering or removing a cell only re-shapes that cell and its four neighbours, never the
    /// whole map: a wall built tile by tile would otherwise get quadratically more expensive the
    /// longer it gets, which on a phone is exactly the wrong direction.
    /// </summary>
    public class FenceNetwork : MonoBehaviour
    {
        public static FenceNetwork Instance { get; private set; }

        private readonly Dictionary<Vector2Int, FenceAppearance> _cells = new Dictionary<Vector2Int, FenceAppearance>();

        private static readonly Vector2Int[] Neighbours =
        {
            new Vector2Int(0, 1),   // north
            new Vector2Int(1, 0),   // east
            new Vector2Int(0, -1),  // south
            new Vector2Int(-1, 0),  // west
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Adds a cell to the line. `appearance` is null for anything that connects but keeps its own look (a gate, a tower).</summary>
        public void Register(Vector2Int cell, FenceAppearance appearance)
        {
            _cells[cell] = appearance;
            RefreshCellAndNeighbours(cell);
        }

        public void Unregister(Vector2Int cell)
        {
            if (!_cells.Remove(cell)) return;

            // The neighbours are refreshed, not the cell itself -- it is gone. This is what turns
            // the two segments either side of a destroyed one back into dead ends.
            RefreshNeighbours(cell);
        }

        public bool Connects(Vector2Int cell)
        {
            return _cells.ContainsKey(cell);
        }

        private void RefreshCellAndNeighbours(Vector2Int cell)
        {
            Refresh(cell);
            RefreshNeighbours(cell);
        }

        private void RefreshNeighbours(Vector2Int cell)
        {
            foreach (var offset in Neighbours)
            {
                Refresh(cell + offset);
            }
        }

        private void Refresh(Vector2Int cell)
        {
            if (!_cells.TryGetValue(cell, out var appearance) || appearance == null) return;

            appearance.Apply(
                Connects(cell + Neighbours[0]),
                Connects(cell + Neighbours[1]),
                Connects(cell + Neighbours[2]),
                Connects(cell + Neighbours[3]));
        }
    }
}
