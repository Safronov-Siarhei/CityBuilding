using System.Collections.Generic;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Grid;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Combat
{
    /// <summary>
    /// A ranged fighter -- an archer, the Queen, a gnome crossbowman -- up on the walls (the user,
    /// 2026-11-02; Buildings/WallWalk):
    /// - sent there (tap a wall with them selected) or going up by himself when orcs come near and a
    ///   free wall cell is close (`wall_seek_radius_cells`);
    /// - up only through a stair tower: he walks to its foot, climbs inside it
    ///   (`wall_climb_sec`), then along the walk cell by cell to his post; one fighter a cell, the
    ///   others pass through him to cells further on;
    /// - up there he shoots `wall_range_bonus_cells` further and never leaves the wall to chase;
    ///   melee orcs cannot reach him (OrcUnit), throwers can;
    /// - an order on the ground brings him down through the nearest stair first;
    /// - the wall cell under him destroyed: he falls and dies.
    /// </summary>
    public partial class SoldierUnit
    {
        private enum WallPhase { None, ToStair, Up, Walking, Posted, Down }

        private WallPhase _wallPhase;
        private Vector2Int _wallPost;
        private Vector2Int _wallStair;
        private Vector2Int _wallAt;
        private readonly List<Vector2Int> _wallPath = new List<Vector2Int>();
        private int _wallStep;
        private float _climbT;
        private Vector3 _climbFrom;
        private Vector3 _climbTo;
        private bool _wallLeaving;
        private float _wallSeekIn;

        /// <summary>On the walls or inside a stair tower: melee orcs leave him be.</summary>
        public bool OnWall => _wallPhase == WallPhase.Up || _wallPhase == WallPhase.Walking
                              || _wallPhase == WallPhase.Posted || _wallPhase == WallPhase.Down;

        /// <summary>Has a wall to go to, is on it, or is coming down from it.</summary>
        public bool WallBound => _wallPhase != WallPhase.None;

        private static float CellMetres => GridManager.Instance != null ? GridManager.Instance.CellSize : 1f;

        private float WallRangeBonus => (_wallPhase == WallPhase.Walking || _wallPhase == WallPhase.Posted) && BalanceConfig.Instance != null
            ? BalanceConfig.Instance.WallRangeBonusCells * CellMetres
            : 0f;

        /// <summary>Sends a ranged fighter up to the free wall cell nearest `cell`; false when there is no way up to it.</summary>
        public bool GoToWall(Vector2Int cell)
        {
            // every fighter but the cavalry and the machines (the user, 2026-11-05)
            if (IsCavalry || SoldierStats.IsMachine(Type) || CurrentHealth <= 0 || Dungeon.DungeonMode.IsActive) return false;
            if (!WallWalk.FreeCellNear(cell, this, out var post)) return false;
            if (_wallPhase == WallPhase.Walking || _wallPhase == WallPhase.Posted)
            {
                // already up: along the walk, if this stretch reaches it
                if (WallWalk.Path(_wallAt, post, _wallPath))
                {
                    WallWalk.Claim(post, this);
                    _wallPost = post;
                    _wallStep = 1;
                    _wallLeaving = false;
                    _wallPhase = WallPhase.Walking;
                    return true;
                }
                return false;
            }
            if (!WallWalk.NearestStair(post, transform.position, out var stair)) return false;
            WallWalk.Claim(post, this);
            _wallPost = post;
            _wallStair = stair;
            _wallLeaving = false;
            _wallPhase = WallPhase.ToStair;
            _hasRoute = false;
            _target = null;
            Marching = false;
            return true;
        }

        /// <summary>Comes down (by the nearest stair) or gives the way up up -- an order on the ground.</summary>
        public void LeaveWall() => LeaveWall(null);

        /// <summary>
        /// An order on the ground toward `toward`: down by the stair nearest it -- or, where he
        /// stands nearer to it than any stair does, a jump off the wall (SoldierUnitSiege). A
        /// siege tower sent elsewhere leaves its wall.
        /// </summary>
        public void LeaveWall(Vector3? toward)
        {
            if (IsCarrier)
            {
                Undock();
                return;
            }
            if (toward.HasValue && (_wallPhase == WallPhase.Walking || _wallPhase == WallPhase.Posted))
            {
                var there = toward.Value;
                // the stair nearest where he is going lies behind him (the siege tower on the near
                // side of the wall, the order beyond it): no way down on that side, he jumps
                var stairFound = WallWalk.NearestStair(_wallAt, there, out var near);
                var toStair = stairFound ? WallWalk.Top(near) - transform.position : Vector3.zero;
                var toThere = there - transform.position;
                toStair.y = toThere.y = 0f;
                if (stairFound && WallWalk.IsDockStair(near) && Vector3.Dot(toStair, toThere) <= 0f)
                {
                    JumpDown(there);
                    return;
                }
            }
            switch (_wallPhase)
            {
                case WallPhase.ToStair:
                    _wallPhase = WallPhase.None;
                    WallWalk.Release(this);
                    return;
                case WallPhase.Up:
                    _wallLeaving = true;
                    return;
                case WallPhase.Walking:
                case WallPhase.Posted:
                    WallWalk.Release(this);
                    if (!WallWalk.NearestStair(_wallAt, toward ?? transform.position, out var stair) || !WallWalk.Path(_wallAt, stair, _wallPath))
                    {
                        DropToGround();
                        return;
                    }
                    _wallStair = stair;
                    _wallStep = 1;
                    _wallLeaving = true;
                    _wallPhase = WallPhase.Walking;
                    return;
            }
        }

        /// <summary>One frame of the wall's business; true when it took the frame (Update returns).</summary>
        private bool TickWall()
        {
            if (_wallPhase == WallPhase.None) return TrySeekWall();
            if (Dungeon.DungeonMode.IsActive)
            {
                DropToGround();
                return false;
            }
            WallWalk.Ensure();
            // the wall broken under him: he falls (the user: and dies)
            if ((_wallPhase == WallPhase.Walking || _wallPhase == WallPhase.Posted) && !WallWalk.IsWalk(_wallAt))
            {
                Fall();
                return true;
            }

            switch (_wallPhase)
            {
                case WallPhase.ToStair:
                {
                    if (!WallWalk.IsStair(_wallStair))
                    {
                        _wallPhase = WallPhase.None;
                        WallWalk.Release(this);
                        return false;
                    }
                    var foot = StairFoot(_wallStair);
                    var distance = HorizontalDistance(foot, transform.position);
                    if (distance <= CellMetres * 1.1f || (_hasRoute && _route.IsFinished && distance <= CellMetres * 2f))
                    {
                        BeginClimb(true);
                        return true;
                    }
                    WalkTowards(foot);
                    return true;
                }
                case WallPhase.Up:
                case WallPhase.Down:
                {
                    var seconds = BalanceConfig.Instance != null ? Mathf.Max(0.1f, BalanceConfig.Instance.WallClimbSeconds) : 1.2f;
                    _climbT += Time.deltaTime / seconds;
                    transform.position = Vector3.Lerp(_climbFrom, _climbTo, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_climbT)));
                    if (_climbT < 1f) return true;
                    if (_wallPhase == WallPhase.Down)
                    {
                        _wallPhase = WallPhase.None;
                        if (_controller != null) _controller.enabled = true;
                        _hasRoute = false;
                        return false;
                    }
                    _wallAt = _wallStair;
                    if (_wallLeaving || !WallWalk.Path(_wallStair, _wallPost, _wallPath))
                    {
                        BeginClimb(false);
                        return true;
                    }
                    _wallStep = 1;
                    _wallPhase = WallPhase.Walking;
                    return true;
                }
                case WallPhase.Walking:
                {
                    if (_wallStep >= _wallPath.Count)
                    {
                        if (_wallLeaving) BeginClimb(false);
                        else _wallPhase = WallPhase.Posted;
                        return true;
                    }
                    var next = _wallPath[_wallStep];
                    if (!WallWalk.IsWalk(next))
                    {
                        // broken ahead: a new way, or a stand where he is
                        var goal = _wallLeaving ? _wallStair : _wallPost;
                        if (!WallWalk.IsWalk(goal) || !WallWalk.Path(_wallAt, goal, _wallPath))
                        {
                            if (_wallLeaving) DropToGround();
                            else _wallPhase = WallPhase.Posted;
                            return true;
                        }
                        _wallStep = 1;
                        return true;
                    }
                    var to = WallWalk.Top(next);
                    var from = WallWalk.Top(_wallAt);
                    var step = _walkSpeed * Time.deltaTime;
                    var way = to - transform.position;
                    way.y = 0f;
                    if (way.magnitude <= step)
                    {
                        transform.position = new Vector3(to.x, StepHeight(_wallAt, next, from.y, to.y, 1f), to.z);
                        _wallAt = next;
                        _wallStep++;
                    }
                    else
                    {
                        var p = transform.position + way.normalized * step;
                        var span = new Vector2(to.x - from.x, to.z - from.z).magnitude;
                        var done = span > 0.001f ? 1f - new Vector2(to.x - p.x, to.z - p.z).magnitude / span : 1f;
                        p.y = StepHeight(_wallAt, next, from.y, to.y, done);
                        transform.position = p;
                        FaceTowards(to);
                    }
                    return true;
                }
                case WallPhase.Posted:
                    WallFight();
                    return true;
            }
            return false;
        }

        /// <summary>
        /// The height on the way from one walk cell to the next, `done` 0..1 along it: level
        /// between walls of the same level; up or down the steps between walls of different levels
        /// -- they stand on the lower cell's half by the joint (WallWalk.PlaceSteps); in and out of a
        /// stair tower at the wall's own height (inside, where nobody sees, it is the stair's).
        /// </summary>
        private static float StepHeight(Vector2Int a, Vector2Int b, float ya, float yb, float done)
        {
            var aStair = WallWalk.IsStair(a);
            var bStair = WallWalk.IsStair(b);
            if (aStair && !bStair) return yb;
            if (bStair && !aStair) return done < 0.5f ? ya : yb;
            if (Mathf.Approximately(ya, yb)) return yb;
            // rising: the steps are on a's half, ahead of him; falling: on b's half, after the joint
            return yb > ya
                ? Mathf.Lerp(ya, yb, Mathf.Clamp01((done - 0.04f) / 0.46f))
                : Mathf.Lerp(ya, yb, Mathf.Clamp01((done - 0.5f) / 0.46f));
        }

        /// <summary>Shoots the nearest enemy in reach -- never leaves the wall to chase one.</summary>
        private void WallFight()
        {
            _retargetTimer -= Time.deltaTime;
            if (_target == null || !_target.IsAlive || _retargetTimer <= 0f)
            {
                _retargetTimer = RetargetIntervalSeconds;
                _target = NearestEnemyInReach();
            }
            if (_target == null || !_target.IsAlive) return;
            if (HorizontalDistance(_target.Transform.position, transform.position) > UnitAttackRange + BodyRadius(_target))
            {
                _target = null;
                return;
            }
            FaceTowards(_target.Transform.position);
            TickAttack();
        }

        private IDamageTarget NearestEnemyInReach()
        {
            IDamageTarget nearest = null;
            var best = float.MaxValue;
            foreach (var candidate in EnumerateEnemies())
            {
                if (candidate == null || !candidate.IsAlive) continue;
                if (candidate is OrcPortal portal && portal.IsShielded) continue;
                var d = HorizontalDistance(candidate.Transform.position, transform.position) - BodyRadius(candidate);
                if (d > UnitAttackRange || d >= best) continue;
                best = d;
                nearest = candidate;
            }
            return nearest;
        }

        /// <summary>A ranged fighter left to himself goes up the nearest wall when orcs come near.</summary>
        private bool TrySeekWall()
        {
            if (!IsRanged || SoldierStats.IsMachine(Type) || Marching || IsRetreating || Group == null || Group.AttackTarget != null || Dungeon.DungeonMode.IsActive) return false;
            _wallSeekIn -= Time.deltaTime;
            if (_wallSeekIn > 0f) return false;
            _wallSeekIn = 1f + Random.value * 0.5f;
            if (!WallWalk.Any) return false;
            var reach = (BalanceConfig.Instance != null ? BalanceConfig.Instance.WallSeekRadiusCells : 14f) * CellMetres;
            OrcUnit nearest = null;
            var best = reach * reach;
            foreach (var orc in OrcUnit.All)
            {
                if (orc == null || orc.CurrentHealth <= 0) continue;
                var d = orc.transform.position - transform.position;
                d.y = 0f;
                if (d.sqrMagnitude >= best) continue;
                best = d.sqrMagnitude;
                nearest = orc;
            }
            if (nearest == null) return false;
            if (!WallWalk.BestPost(transform.position, nearest.transform.position, reach, this, out var post)) return false;
            return GoToWall(post);
        }

        private void BeginClimb(bool up)
        {
            _climbT = 0f;
            _climbFrom = transform.position;
            if (_controller != null) _controller.enabled = false;
            if (up)
            {
                _climbTo = WallWalk.Top(_wallStair);
                _wallPhase = WallPhase.Up;
                return;
            }
            WallWalk.Release(this);
            var foot = StairFoot(_wallStair);
            if (NavMesh.SamplePosition(foot, out var hit, CellMetres * 2.5f, NavMesh.AllAreas)) foot = hit.position;
            foot.y = GridManager.Instance != null ? GridManager.Instance.GroundHeight : foot.y;
            _climbTo = foot;
            _wallPhase = WallPhase.Down;
        }

        private static Vector3 StairFoot(Vector2Int cell)
        {
            var grid = GridManager.Instance;
            if (grid == null) return Vector3.zero;
            var at = grid.GetFootprintCenterWorld(cell, Vector2Int.one);
            at.y = grid.GroundHeight;
            return at;
        }

        /// <summary>Down on the ground where he stands, alive (a dungeon, a way down that is gone).</summary>
        private void DropToGround()
        {
            WallWalk.Release(this);
            _wallPhase = WallPhase.None;
            _wallLeaving = false;
            if (GridManager.Instance != null)
            {
                var p = transform.position;
                p.y = GridManager.Instance.GroundHeight;
                transform.position = p;
            }
            if (_controller != null) _controller.enabled = true;
            _hasRoute = false;
        }

        /// <summary>The wall cell under him is gone: he falls and dies (the user, 2026-11-02).</summary>
        private void Fall()
        {
            DropToGround();
            if (InvulnerableCheat || CurrentHealth <= 0) return;
            CurrentHealth = 0;
            ArmyManager.Instance?.NotifySoldierKilled(this);
        }
    }
}
