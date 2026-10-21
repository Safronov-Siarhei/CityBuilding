using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Maps
{
    public enum OreKind { Iron, Coal, Copper, Gold }

    /// <summary>
    /// A vein of ore on the map (the user, 2026-10-21: "рудник ставится только на жилу"). A 2x2
    /// patch -- a mine's own plot -- that the mine of its kind is built ON: the vein's patch hides
    /// while a mine stands on it and comes back when the mine goes. It never runs dry; what makes a
    /// vein worth having is where it is (OreDepositSpawner puts gold far out, towards the orcs).
    /// It shows as a patch of broken ground with the ore breaking through and glowing.
    ///
    /// Its cells are taken on the grid like a boulder's, so nothing else can be built there; the
    /// placer lets only the matching mine through (BuildingPlacer.IsFreeFor).
    /// </summary>
    public class OreDeposit : MonoBehaviour
    {
        public static readonly Vector2Int Size = new Vector2Int(2, 2);

        private static readonly List<OreDeposit> _all = new List<OreDeposit>();
        public static IReadOnlyList<OreDeposit> All => _all;

        public OreKind Kind { get; private set; }
        public Vector2Int Origin { get; private set; }
        public Buildings.BuildingInstance Mine { get; private set; }

        private GameObject _visual;

        /// <param name="visual">The patch and its glow -- what a mine on the vein hides.</param>
        public void Initialize(OreKind kind, Vector2Int origin, GameObject visual)
        {
            Kind = kind;
            Origin = origin;
            _visual = visual;
        }

        /// <summary>Which ore a building mines, or null for one that is not a mine.</summary>
        public static OreKind? KindForBuilding(string buildingName)
        {
            switch (buildingName)
            {
                case "IronMine": return OreKind.Iron;
                case "CoalMine": return OreKind.Coal;
                case "CopperMine": return OreKind.Copper;
                case "GoldMine": return OreKind.Gold;
                default: return null;
            }
        }

        /// <summary>The vein covering this cell, or null.</summary>
        public static OreDeposit At(Vector2Int cell)
        {
            foreach (var deposit in _all)
            {
                var d = cell - deposit.Origin;
                if (d.x >= 0 && d.y >= 0 && d.x < Size.x && d.y < Size.y) return deposit;
            }
            return null;
        }

        /// <summary>A mine has gone up on this vein: the outcrop gives way to the mine's own hill.</summary>
        public void Attach(Buildings.BuildingInstance mine)
        {
            Mine = mine;
            if (_visual != null) _visual.SetActive(false);
        }

        public void Detach(Buildings.BuildingInstance mine)
        {
            if (Mine != mine) return;
            Mine = null;
            if (_visual != null) _visual.SetActive(true);
        }

        private void OnEnable() => _all.Add(this);
        private void OnDisable() => _all.Remove(this);
    }
}
