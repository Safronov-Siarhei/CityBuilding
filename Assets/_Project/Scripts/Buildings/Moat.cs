using System.Collections.Generic;
using CityBuilder.Core;
using CityBuilder.Grid;
using UnityEngine;

namespace CityBuilder.Buildings
{
    /// <summary>
    /// The Moat (Ров, the user 2026-11-02): cells of water laid piece by piece like a fence. It
    /// never blocks -- anyone may wade it -- but in its water every walker goes at
    /// `moat_speed_multiplier`, routes keep off it while dry land or a road will do (the Moat
    /// NavMesh area, `moat_path_cost`), and an orc standing in it fights no soldier or hero (it
    /// still breaks a wall it is up against: the user chose that, so a moat dug right under a wall
    /// cannot make the wall untouchable). Opened in the Laboratory for `moat_research_crystals`;
    /// one level; its card shows only its name and Demolish.
    /// </summary>
    public static class Moat
    {
        public const string Id = "Moat";

        private static readonly Dictionary<Vector2Int, BuildingInstance> Cells = new Dictionary<Vector2Int, BuildingInstance>();
        private static readonly Vector2Int[] Sides = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        // cells a crossing (the Bridge, the Drawbridge) stands over: dry going there
        private static readonly Dictionary<Vector2Int, BuildingInstance> Spans = new Dictionary<Vector2Int, BuildingInstance>();

        public static bool Has(Vector2Int cell) => Cells.ContainsKey(cell);

        public static bool IsSpanned(Vector2Int cell) => Spans.ContainsKey(cell);

        /// <summary>Whether this moat piece has a crossing over it (its water is no water then).</summary>
        public static bool IsSpanned(BuildingInstance moat)
        {
            foreach (var cell in moat.Cells()) if (Spans.ContainsKey(cell)) return true;
            return false;
        }

        /// <summary>A crossing put over (true) or taken off (false) its cells; the moats under it re-mark their water.</summary>
        public static void Span(BuildingInstance crossing, bool on)
        {
            foreach (var cell in crossing.Cells())
            {
                if (on) Spans[cell] = crossing;
                else if (Spans.TryGetValue(cell, out var at) && at == crossing) Spans.Remove(cell);
                if (Cells.TryGetValue(cell, out var moat) && moat != null) Maps.MeshMapApplier.Instance?.RegisterMoat(moat);
            }
        }

        public static void Add(BuildingInstance moat)
        {
            foreach (var cell in moat.Cells()) Cells[cell] = moat;
            RefreshAround(moat);
        }

        public static void Remove(BuildingInstance moat)
        {
            foreach (var cell in moat.Cells())
                if (Cells.TryGetValue(cell, out var at) && at == moat) Cells.Remove(cell);
            RefreshAround(moat);
        }

        /// <summary>Whether a point stands in a moat's water.</summary>
        public static bool Wading(Vector3 position)
        {
            if (Cells.Count == 0) return false;
            var grid = GridManager.Instance;
            if (grid == null) return false;
            var cell = grid.WorldToCell(position);
            return Cells.ContainsKey(cell) && !Spans.ContainsKey(cell);
        }

        /// <summary>The walking speed factor at a point: the moat's in its water, 1 elsewhere.</summary>
        public static float SpeedAt(Vector3 position) =>
            BalanceConfig.Instance != null && Wading(position) ? BalanceConfig.Instance.MoatSpeedMultiplier : 1f;

        /// <summary>How far below the ground a figure standing here is drawn: sunk to the knee in the moat's water, 0 elsewhere.</summary>
        public static float WadeDepthAt(Vector3 position) =>
            BalanceConfig.Instance != null && Wading(position) ? BalanceConfig.Instance.MoatWadeDepth : 0f;

        // the cell's own banks and its neighbours' change with every piece laid or lost
        private static void RefreshAround(BuildingInstance moat)
        {
            foreach (var cell in moat.Cells())
            {
                Refresh(cell);
                foreach (var side in Sides) Refresh(cell + side);
            }
        }

        private static void Refresh(Vector2Int cell)
        {
            if (!Cells.TryGetValue(cell, out var at) || at == null) return;
            var look = at.GetComponent<MoatAppearance>();
            if (look != null) look.Refresh(cell);
        }
    }
}
