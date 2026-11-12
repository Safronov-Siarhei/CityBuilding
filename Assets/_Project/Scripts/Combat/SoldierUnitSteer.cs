using CityBuilder.Core;
using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Combat
{
    /// <summary>
    /// A soldier run by the stick (campaign mission 2: Ардан, the user 2026-11-12 -- "управляет арданом"):
    /// while Steer is held he runs that way over the NavMesh, sliding along whatever edge he meets
    /// (water, a wall, a building) and striking nobody; let go, he is an ordinary soldier again and
    /// cuts down whoever is at hand (the mission holds his group where he stopped).
    ///
    /// SteerArea keeps him inside the lit part of the map (the dark beyond is not to be entered, the
    /// user's first playtest); Dash throws him a few cells along a line (his «Натиск»).
    /// </summary>
    public partial class SoldierUnit
    {
        /// <summary>The stick's direction on the ground (x, z), length 0..1; zero = his own fighting.</summary>
        public Vector3 Steer { get; set; }

        /// <summary>He was steered this frame (the mission's camera and hold point follow).</summary>
        public bool IsSteered => Steer.sqrMagnitude > 0.0004f;

        /// <summary>World x/z rectangle he may not leave while steered or dashing (zero size = anywhere).</summary>
        public Rect SteerArea { get; set; }

        /// <summary>World XZ rect he never walks out of, whatever he chases (campaign mission 2's garrison); empty = free.</summary>
        public Rect Pen { get; set; }

        private bool InReachOfPen(Vector3 at)
        {
            var reach = IsRanged ? UnitAttackRange + 0.3f : 0.1f;   // melee: only what stands at the pen's edge
            return at.x >= Pen.xMin - reach && at.x <= Pen.xMax + reach && at.z >= Pen.yMin - reach && at.z <= Pen.yMax + reach;
        }

        private Vector3 _dashDir;
        private float _dashSpeed, _dashLeft;

        /// <summary>He is in the middle of a dash.</summary>
        public bool IsDashing => _dashLeft > 0f;

        /// <summary>Throws him `distance` along `dir` over `seconds` (stopped by whatever edge he meets).</summary>
        public void Dash(Vector3 dir, float distance, float seconds)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
            _dashDir = dir.normalized;
            _dashLeft = Mathf.Max(0.05f, seconds);
            _dashSpeed = distance / _dashLeft;
            FaceTowards(transform.position + _dashDir);
        }

        private bool TickSteer()
        {
            if (_dashLeft > 0f)
            {
                _dashLeft -= Time.deltaTime;
                MoveAlong(_dashDir, _dashSpeed * Time.deltaTime, slide: false);
                _target = null;
                return true;
            }
            if (!IsSteered) return false;
            var dir = new Vector3(Steer.x, 0f, Steer.z);
            var push = Mathf.Clamp01(dir.magnitude);
            dir /= Mathf.Max(0.0001f, dir.magnitude);
            var speed = _walkSpeed * Weather.SpeedMultiplier * push;
            if (SlowedFor > 0f) speed *= _slowFactor;
            if (_hasteLeft > 0f) speed *= _hasteFactor;
            MoveAlong(dir, speed * Time.deltaTime, slide: true);
            FaceTowards(transform.position + dir);
            _target = null;
            return true;
        }

        private void MoveAlong(Vector3 dir, float step, bool slide)
        {
            var from = transform.position;
            var to = from + dir * step;
            if (NavMesh.Raycast(from, to, out var hit, NavMesh.AllAreas))
            {
                if (slide)
                {
                    // along the edge instead of stopping dead on it
                    var n = new Vector3(hit.normal.x, 0f, hit.normal.z);
                    var along = dir - Vector3.Dot(dir, n) * n;
                    to = from + along * step;
                    if (NavMesh.Raycast(from, to, out hit, NavMesh.AllAreas)) to = hit.position;
                }
                else
                {
                    to = hit.position;
                    _dashLeft = 0f;
                }
            }
            var area = SteerArea;
            if (area.width > 0f && area.height > 0f)
            {
                to.x = Mathf.Clamp(to.x, area.xMin, area.xMax);
                to.z = Mathf.Clamp(to.z, area.yMin, area.yMax);
            }
            var move = to - from;
            move.y = 0f;
            if (_controller != null && _controller.enabled) _controller.Move(move);
            else transform.position += move;
            PinToGroundHeight();
        }
    }
}
