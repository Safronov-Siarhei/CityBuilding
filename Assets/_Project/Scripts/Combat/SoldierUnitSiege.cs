using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Grid;
using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// The siege rules (the user, 2026-11-05):
    /// - the SIEGE TOWER is sent at any fence or gate (tap it with the towers selected): it rolls to
    ///   the free cell beside the wall nearest it, lets its bridge down and is a stair onto that
    ///   wall (Buildings/WallWalk.Dock) -- over a low fence (levels 1-2) a gangway the height of
    ///   `siege_gangway_height`;
    /// - every fighter but the cavalry and the machines may be sent up a wall (SoldierUnitWalls);
    /// - coming down where no stair is nearer than the wall's own edge: he JUMPS, and loses
    ///   `wall_jump_health_share` of his health.
    /// </summary>
    public partial class SoldierUnit
    {
        private enum DockPhase { None, Rolling, Docked }

        private DockPhase _dockPhase;
        private Vector2Int _dockCell;
        private Vector2Int _dockWall;

        /// <summary>Going up or down a stair (or a siege tower) this moment.</summary>
        public bool IsClimbing => _wallPhase == WallPhase.Up || _wallPhase == WallPhase.Down;

        /// <summary>The siege tower stands at a wall, its bridge down.</summary>
        public bool IsDocked => _dockPhase == DockPhase.Docked;

        /// <summary>Sends a siege tower at this fence or gate; false when it is no tower or no free cell is beside the wall.</summary>
        public bool DockAt(BuildingInstance wall, Vector3 tap)
        {
            var grid = GridManager.Instance;
            if (!IsCarrier || wall == null || grid == null || CurrentHealth <= 0) return false;
            var best = float.MaxValue;
            var found = false;
            foreach (var cell in wall.Cells())
            {
                foreach (var step in new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left })
                {
                    var beside = cell + step;
                    if (!grid.IsWithinBounds(beside, Vector2Int.one) || !grid.IsAreaFree(beside, Vector2Int.one) || WallWalk.IsDockCell(beside, this)) continue;
                    var at = grid.GetFootprintCenterWorld(beside, Vector2Int.one);
                    // nearest the tap's wall cell first, then the tower's own side of the wall
                    var d = (grid.GetFootprintCenterWorld(cell, Vector2Int.one) - tap).sqrMagnitude * 4f + (at - transform.position).sqrMagnitude;
                    if (d >= best) continue;
                    best = d;
                    _dockCell = beside;
                    _dockWall = cell;
                    found = true;
                }
            }
            if (!found) return false;
            Undock();
            _dockPhase = DockPhase.Rolling;
            _hasRoute = false;
            Marching = false;
            return true;
        }

        /// <summary>Back from the wall: the bridge up, the stair gone (whoever is up there comes down as he can).</summary>
        public void Undock()
        {
            if (_dockPhase == DockPhase.Docked) WallWalk.Undock(this);
            _dockPhase = DockPhase.None;
            var machine = GetComponent<SiegeMachine>();
            if (machine != null) machine.Docked = false;
        }

        /// <summary>The tower's frame: rolling to its cell, or standing docked. True when it took the frame.</summary>
        private bool TickSiegeTower()
        {
            if (_dockPhase == DockPhase.None) return false;
            var grid = GridManager.Instance;
            if (grid == null) return false;
            if (_dockPhase == DockPhase.Docked)
            {
                // the wall razed under the bridge: nothing to stand at
                if (!WallWalk.IsDockCell(_dockCell, this)) Undock();
                return true;
            }
            var at = grid.GetFootprintCenterWorld(_dockCell, Vector2Int.one);
            if (HorizontalDistance(at, transform.position) > CellMetres * 0.3f)
            {
                WalkTowards(at);
                return true;
            }
            FaceTowards(grid.GetFootprintCenterWorld(_dockWall, Vector2Int.one));
            var height = BalanceConfig.Instance != null ? BalanceConfig.Instance.SiegeGangwayHeight : 0.6f;
            WallWalk.Dock(this, _dockCell, _dockWall, grid.GroundHeight + height);
            _dockPhase = DockPhase.Docked;
            var machine = GetComponent<SiegeMachine>();
            if (machine != null) machine.Docked = true;
            return true;
        }

        /// <summary>
        /// Off the wall toward `toward` with no stair nearer to it than he is: he jumps from the
        /// wall's edge on that side (the user, 2026-11-05) and loses a share of his health.
        /// </summary>
        private void JumpDown(Vector3 toward)
        {
            var grid = GridManager.Instance;
            WallWalk.Release(this);
            _wallPhase = WallPhase.None;
            _wallLeaving = false;
            var p = transform.position;
            if (grid != null)
            {
                // the cell beside his wall cell on the side he is going
                var best = float.MaxValue;
                foreach (var step in new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left })
                {
                    var c = _wallAt + step;
                    if (WallWalk.IsWalk(c) || !grid.IsAreaFree(c, Vector2Int.one)) continue;
                    var at = grid.GetFootprintCenterWorld(c, Vector2Int.one);
                    var d = (at - toward).sqrMagnitude;
                    if (d >= best) continue;
                    best = d;
                    p = at;
                }
                p.y = grid.GroundHeight;
            }
            transform.position = p;
            if (_controller != null) _controller.enabled = true;
            _hasRoute = false;
            var share = BalanceConfig.Instance != null ? BalanceConfig.Instance.WallJumpHealthShare : 0.3f;
            var loss = Mathf.Max(1, Mathf.RoundToInt(MaxHealth * share));
            if (InvulnerableCheat) return;
            if (CurrentHealth > loss) CurrentHealth -= loss;
            else
            {
                CurrentHealth = 0;
                ArmyManager.Instance?.NotifySoldierKilled(this);
                return;
            }
            ShowHealth();
        }
    }
}
