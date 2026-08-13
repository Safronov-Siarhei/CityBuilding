using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Combat
{
    /// <summary>
    /// A NavMesh route being walked: the corner list plus which corner is next. Pulled out as its
    /// own object because unit locomotion is the single most bug-prone thing in this project --
    /// "the citizen accepted the order and didn't move" was chased across five commits, and every
    /// one of those bugs lived in a hand-rolled copy of this logic. SoldierUnit uses this one;
    /// CitizenAgent and OrcUnit still carry their own older copies.
    ///
    /// The lessons from those bugs are baked in here:
    /// - Every distance test is HORIZONTAL. Units are pinned to a fixed ground height each frame
    ///   while NavMesh corners sit on the baked surface, so a 3D comparison between the two mixes
    ///   two different vertical conventions and fails unpredictably.
    /// - Leading corners the unit is already standing on are skipped, not just corners[0]. A route
    ///   whose first waypoint is the unit's own feet reads as "arrived" on the first frame.
    /// - A "successful" path that ends where it started is rejected: NavMesh.CalculatePath reports
    ///   success for a PARTIAL path too, and a partial path from an enclosed spot is a single
    ///   corner going nowhere.
    /// </summary>
    public class NavRoute
    {
        /// <summary>How close (horizontally) counts as standing on a waypoint.</summary>
        private const float ArrivalThreshold = 0.12f;

        /// <summary>A route whose far end is no further than this from the start isn't a route at all -- see the partial-path note above.</summary>
        private const float MinProgressDistance = 0.35f;

        // Shared across every route: CalculatePath overwrites the path's contents, so one instance
        // is enough for the whole army and saves an allocation per order. Built lazily rather than
        // in a field initializer -- Unity forbids constructing a NavMeshPath during a
        // MonoBehaviour's field/static initialization, which is how this crashed CitizenAgent once.
        private static NavMeshPath _sharedPath;

        private Vector3[] _corners = { Vector3.zero };
        private int _index;

        public Vector3 Destination { get; private set; }

        /// <summary>True once every corner has been reached -- i.e. there is nowhere left to walk.</summary>
        public bool IsFinished => _index >= _corners.Length;

        /// <summary>
        /// Plans a route from `from` to `to`. Falls back to a straight line at the destination when
        /// the NavMesh has nothing to offer, so a unit always attempts something rather than
        /// standing still with no explanation.
        /// </summary>
        public void SetDestination(Vector3 from, Vector3 to)
        {
            Destination = to;
            _index = 0;
            _sharedPath ??= new NavMeshPath();

            if (NavMesh.CalculatePath(from, to, NavMesh.AllAreas, _sharedPath))
            {
                var corners = _sharedPath.corners;
                var reachesSomewhere = corners.Length > 0 &&
                                       HorizontalDistance(corners[corners.Length - 1], from) > MinProgressDistance;
                if (reachesSomewhere)
                {
                    _corners = corners;
                    SkipCornersAlreadyReached(from);
                    return;
                }
            }

            _corners = new[] { to };
        }

        /// <summary>
        /// Horizontal unit vector toward the next waypoint, advancing past waypoints as they're
        /// reached. False when the route is done (nothing left to walk toward).
        /// </summary>
        public bool TryGetDirection(Vector3 currentPosition, out Vector3 direction)
        {
            SkipCornersAlreadyReached(currentPosition);

            if (IsFinished)
            {
                direction = Vector3.zero;
                return false;
            }

            var toWaypoint = _corners[_index] - currentPosition;
            toWaypoint.y = 0f;
            var distance = toWaypoint.magnitude;
            if (distance <= Mathf.Epsilon)
            {
                direction = Vector3.zero;
                return false;
            }

            direction = toWaypoint / distance;
            return true;
        }

        private void SkipCornersAlreadyReached(Vector3 currentPosition)
        {
            while (_index < _corners.Length && HorizontalDistance(_corners[_index], currentPosition) < ArrivalThreshold)
            {
                _index++;
            }
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
