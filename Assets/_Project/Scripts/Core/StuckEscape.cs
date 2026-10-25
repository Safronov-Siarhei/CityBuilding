using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Core
{
    /// <summary>
    /// Gets a walker out of a place it cannot walk out of (the user, 2026-10-25: the King, walking
    /// where two buildings were then put up against each other, stood wedged between them). A unit
    /// that keeps trying to walk, barely moves and stands off the NavMesh (inside a building's
    /// carve) passes through what holds it, straight to the nearest walkable ground, and walks on
    /// from there. Only to get free: everywhere else the buildings stay solid.
    ///
    /// One per walker (the King, a soldier, an orc, an islander), ticked by its own movement: a few
    /// float compares a frame, and a NavMesh query only once it looks stuck.
    /// </summary>
    public class StuckEscape
    {
        private const float CheckSeconds = 0.75f;
        /// <summary>Under this share of the distance it meant to cover in a check, it is stuck.</summary>
        private const float StuckShare = 0.15f;
        private const float SearchMeters = 4f;
        private const float ArriveMeters = 0.08f;
        /// <summary>Further off the NavMesh than a walker's own radius pressed against a wall (a carve's margin): really inside something.</summary>
        private const float OffMeshMeters = 0.25f;

        private float _clock;
        private Vector3 _from;
        private Vector3? _exit;
        private CharacterController _switchedOff;

        /// <summary>Passing through to free ground right now.</summary>
        public bool Escaping => _exit.HasValue;

        /// <summary>
        /// While escaping, moves the walker one step toward the way out with its collider off, and
        /// says so (the caller skips its own step). False the rest of the time.
        /// </summary>
        public bool Step(Transform walker, CharacterController controller, float speed)
        {
            if (!_exit.HasValue) return false;
            var to = _exit.Value - walker.position;
            to.y = 0f;
            var distance = to.magnitude;
            if (distance <= ArriveMeters)
            {
                _exit = null;
                if (_switchedOff != null) _switchedOff.enabled = true;
                _switchedOff = null;
                Restart(walker);
                return false;
            }
            // its collider off while it passes through -- and back on only if this turned it off
            if (controller != null && controller.enabled)
            {
                controller.enabled = false;
                _switchedOff = controller;
            }
            var step = Mathf.Min(distance, Mathf.Max(0.5f, speed) * Time.deltaTime);
            walker.position += to / distance * step;
            return true;
        }

        /// <summary>
        /// After a step it meant to take (`direction`, at `speed`): notices a walker that is not
        /// getting anywhere and picks its way out. `areaMask` is the NavMesh it may stand on.
        /// </summary>
        public void Watch(Transform walker, Vector3 direction, float speed, int areaMask = NavMesh.AllAreas)
        {
            if (_exit.HasValue) return;
            _clock += Time.deltaTime;
            if (_clock < CheckSeconds) return;

            var moved = walker.position - _from;
            moved.y = 0f;
            var meant = speed * _clock;
            Restart(walker);
            if (meant <= 0.01f || moved.magnitude >= meant * StuckShare) return;

            TryFree(walker, areaMask);
        }

        /// <summary>
        /// Inside a carve (a building put up round it, a gap closed): out to the nearest walkable
        /// ground. True when a way out was found. Held by a wall while ON the mesh is left alone --
        /// that is an orc at the fence it has to break, a soldier at a camp's palisade.
        /// </summary>
        public bool TryFree(Transform walker, int areaMask = NavMesh.AllAreas)
        {
            if (_exit.HasValue) return true;
            var at = walker.position;
            if (!NavMesh.SamplePosition(at, out var hit, SearchMeters, areaMask)) return false;
            var off = hit.position - at;
            off.y = 0f;
            if (off.magnitude <= OffMeshMeters) return false;
            _exit = new Vector3(hit.position.x, at.y, hit.position.z);
            return true;
        }

        /// <summary>Not trying to walk (standing, fighting): nothing to measure.</summary>
        public void Rest(Transform walker)
        {
            if (!_exit.HasValue) Restart(walker);
        }

        private void Restart(Transform walker)
        {
            _clock = 0f;
            _from = walker.position;
        }
    }
}
