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
    /// </summary>
    public partial class SoldierUnit
    {
        /// <summary>The stick's direction on the ground (x, z), length 0..1; zero = his own fighting.</summary>
        public Vector3 Steer { get; set; }

        /// <summary>He was steered this frame (the mission's camera and hold point follow).</summary>
        public bool IsSteered => Steer.sqrMagnitude > 0.0004f;

        private bool TickSteer()
        {
            if (!IsSteered) return false;
            var dir = new Vector3(Steer.x, 0f, Steer.z);
            var push = Mathf.Clamp01(dir.magnitude);
            dir /= Mathf.Max(0.0001f, dir.magnitude);
            var speed = _walkSpeed * Weather.SpeedMultiplier * push;
            if (SlowedFor > 0f) speed *= _slowFactor;
            if (_hasteLeft > 0f) speed *= _hasteFactor;
            var step = speed * Time.deltaTime;
            var from = transform.position;
            var to = from + dir * step;
            if (NavMesh.Raycast(from, to, out var hit, NavMesh.AllAreas))
            {
                // along the edge instead of stopping dead on it
                var n = new Vector3(hit.normal.x, 0f, hit.normal.z);
                var slide = dir - Vector3.Dot(dir, n) * n;
                to = from + slide * step;
                if (NavMesh.Raycast(from, to, out hit, NavMesh.AllAreas)) to = hit.position;
            }
            var move = to - from;
            move.y = 0f;
            if (_controller != null && _controller.enabled) _controller.Move(move);
            else transform.position += move;
            PinToGroundHeight();
            FaceTowards(transform.position + dir);
            _target = null;
            return true;
        }
    }
}
