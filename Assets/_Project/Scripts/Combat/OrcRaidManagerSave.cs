using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// Restoring portals, orcs and camps from a save.
    /// </summary>
    public partial class OrcRaidManager
    {
        /// <summary>One saved camp building: its camp (by portal cell), what it is, where, facing, and its health (0 = its ruin).</summary>
        public readonly struct CampBuildingRestoreState
        {
            public CampBuildingRestoreState(Vector2Int portalCell, OrcCampBuildingKind kind, Vector2Int cell, float yaw, int health)
            {
                PortalCell = portalCell;
                Kind = kind;
                Cell = cell;
                Yaw = yaw;
                Health = health;
            }

            public Vector2Int PortalCell { get; }
            public OrcCampBuildingKind Kind { get; }
            public Vector2Int Cell { get; }
            public float Yaw { get; }
            public int Health { get; }
        }

        /// <summary>One saved portal: where it stood, and the health the player had ground it down to.</summary>
        public readonly struct PortalRestoreState
        {
            public PortalRestoreState(Vector2Int cell, int health, bool isMain = false)
            {
                Cell = cell;
                Health = health;
                IsMain = isMain;
            }

            public Vector2Int Cell { get; }
            public int Health { get; }
            public bool IsMain { get; }
        }

        /// <summary>
        /// Puts the raid sources back the way the save left them: every portal still standing where
        /// it stood and on the health it had, and the clock where it was.
        ///
        /// The portals come back IN SAVE ORDER, because that is the order a group's saved attack
        /// order indexes into -- see GameSaveController.RestoreArmyOrders.
        /// </summary>
        /// <param name="portalsPlaced">Whether the map had opened its portals at all. True with an empty list means the player had destroyed every one of them, and no fresh ones are opened over the ruins.</param>
        /// <param name="closedPortalCells">Where the portals the player had already destroyed stood: each gets its ruin back.</param>
        /// <param name="campBuildings">Every camp building the save holds, standing or fallen. Empty means a save from before the camps: fresh camps go up round the standing portals.</param>
        public void RestoreFromSave(bool portalsPlaced, IReadOnlyList<PortalRestoreState> portals,
            IReadOnlyList<Vector2Int> closedPortalCells, IReadOnlyList<CampRestoreState> camps,
            IReadOnlyList<CampBuildingRestoreState> campBuildings, float secondsUntilNextRaid,
            Vector2Int? mainPortalCell = null)
        {
            if (!portalsPlaced) return;
            MainPortalCell = mainPortalCell;

            // The flag alone stops Update from opening a fresh set over the ruins of the ones the
            // player fought for.
            _portalsPlaced = true;

            var grid = GridManager.Instance;
            if (grid == null || portals == null) return;

            // a designed map's main base faces south whatever the Town Hall's place
            var designed = MeshMapApplier.Instance != null ? MeshMapApplier.Instance.CurrentMap : null;
            Quaternion? mainFacing = designed != null && designed.HasMainBaseSpot ? Quaternion.identity : (Quaternion?)null;
            var restored = new List<(OrcPortal portal, int health)>();
            foreach (var state in portals)
            {
                if (state.Health <= 0) continue;
                restored.Add((SpawnPortal(grid, state.Cell, state.IsMain, state.IsMain ? mainFacing : null), state.Health));
            }

            if (closedPortalCells != null)
            {
                foreach (var cell in closedPortalCells)
                {
                    _closedPortalCells.Add(cell);
                    var main = MainPortalCell.HasValue && MainPortalCell.Value == cell;
                    var footprint = main ? MainPortalFootprint : PortalFootprint;
                    grid.SetAreaOccupied(cell, footprint, true);
                    var position = grid.GetFootprintCenterWorld(cell, footprint);
                    SpawnRuin(cell, position, main && mainFacing.HasValue ? mainFacing.Value : FacingTownHall(position), main);
                }
            }

            RestoreCamps(camps, campBuildings);
            // After the camps: building a camp gives its portal the health of the camp's level,
            // and the player's damage goes on top of that.
            foreach (var (portal, health) in restored)
            {
                portal.SetCurrentHealth(health);
            }

            // Which portal sends the next wave is not saved -- it is shown, not counted on -- so
            // a load picks afresh.
            PickNextSource();
            _raidTimer = Mathf.Max(0f, secondsUntilNextRaid);
        }

        /// <summary>
        /// Puts one saved orc back where it stood, on the health it had. Like the army's restore,
        /// it is deliberately not the spawn path: no jitter, and nothing is announced in the event
        /// log.
        ///
        /// Hands the unit back so the caller can match it to the save's orc list by position in
        /// that list -- which is how a group's attack order finds the orc it was chasing again.
        /// </summary>
        public OrcUnit RestoreOrc(Vector3 position, int level, int currentHealth, OrcKind kind = OrcKind.Raider)
        {
            EnsureMaterials();
            var unit = SpawnOrc(position, level, kind, scatter: false);
            unit.SetCurrentHealth(currentHealth);
            return unit;
        }

        /// <summary>One saved camp: its portal's cell, its level, how big it may grow, its rank, its facing and whether it was plundered.</summary>
        public readonly struct CampRestoreState
        {
            public CampRestoreState(Vector2Int portalCell, int level, int maxLevel, int rank, int quarters, bool cleared)
            {
                PortalCell = portalCell;
                Level = level;
                MaxLevel = maxLevel;
                Rank = rank;
                Quarters = quarters;
                Cleared = cleared;
            }

            public Vector2Int PortalCell { get; }
            public int Level { get; }
            public int MaxLevel { get; }
            public int Rank { get; }
            public int Quarters { get; }
            public bool Cleared { get; }
        }

        /// <summary>
        /// Puts the camps back from the save: each rebuilt at the level it had reached, then every
        /// building the save names set back to the health it had -- 0 breaks it on the spot, leaving
        /// its ruin -- with a full garrison (guards are not saved; the spawners muster them again).
        /// A save from before the walled bases gets fresh level-1 camps round its standing portals.
        /// </summary>
        private void RestoreCamps(IReadOnlyList<CampRestoreState> camps, IReadOnlyList<CampBuildingRestoreState> campBuildings)
        {
            if (camps == null || camps.Count == 0)
            {
                var rank = 0;
                foreach (var portal in PortalsByDistance())
                {
                    BuildCamp(portal, rank++, OrcBaseLayout.MaxLevel);
                }
                return;
            }

            foreach (var state in camps)
            {
                OrcPortal portal = null;
                foreach (var p in OrcPortal.All)
                {
                    if (p.Cell == state.PortalCell) portal = p;
                }
                var camp = CreateCamp(state.PortalCell, portal, state.Quarters, state.Rank, state.MaxLevel, state.Cleared || portal == null,
                    checkGround: false);
                camp.Build(Mathf.Max(1, state.Level), this);
                if (campBuildings == null) continue;
                foreach (var entry in campBuildings)
                {
                    if (entry.PortalCell == state.PortalCell) camp.ApplySavedHealth(entry.Kind, entry.Cell, entry.Health);
                }
            }
        }
    }
}
