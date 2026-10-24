using UnityEngine;
using UnityEngine.AI;

namespace CityBuilder.Combat
{
    /// <summary>
    /// A portal's way to the castle in an Invasion run (the user, 2026-10-24): laid once over the
    /// bare map, before the player has built anything, and never laid again. A wave's orcs keep to
    /// their portal's way -- a fence across it is broken, not walked round by another causeway.
    /// Distances along it are horizontal metres from its start.
    /// </summary>
    public class OrcLane
    {
        private readonly Vector3[] _corners;
        private readonly float[] _at;

        public float Length { get; }
        public Vector3 Start => _corners[0];
        public Vector3 End => _corners[_corners.Length - 1];

        public OrcLane(Vector3[] corners)
        {
            _corners = corners != null && corners.Length > 0 ? corners : new[] { Vector3.zero };
            _at = new float[_corners.Length];
            for (var i = 1; i < _corners.Length; i++) _at[i] = _at[i - 1] + Flat(_corners[i] - _corners[i - 1]).magnitude;
            Length = _at[_at.Length - 1];
        }

        /// <summary>
        /// Lays the way from `from` to `to` over the NavMesh as it is now, never across a gate.
        /// Null when the NavMesh has no way at all; a partial way (water between) is kept, as far as it goes.
        /// </summary>
        public static OrcLane Lay(Vector3 from, Vector3 to, float snapMeters)
        {
            if (NavMesh.SamplePosition(to, out var onMesh, snapMeters, Maps.NavAreas.WithoutGates)) to = onMesh.position;
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(from, to, Maps.NavAreas.WithoutGates, path) || path.corners.Length < 2) return null;
            if (path.status != NavMeshPathStatus.PathComplete)
                Debug.LogWarning($"OrcLane: the way from {from} reaches only {path.corners[path.corners.Length - 1]}, short of {to}.");
            return new OrcLane(path.corners);
        }

        /// <summary>How far along the way the point nearest `position` is, and how far off the way `position` stands.</summary>
        public float Project(Vector3 position, out float offLane)
        {
            var best = float.MaxValue;
            var along = 0f;
            for (var i = 1; i < _corners.Length; i++)
            {
                var a = Flat(_corners[i - 1]);
                var ab = Flat(_corners[i]) - a;
                var length = ab.magnitude;
                var t = length > 0.001f ? Mathf.Clamp01(Vector3.Dot(Flat(position) - a, ab) / (length * length)) : 0f;
                var distance = (Flat(position) - (a + ab * t)).sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                along = _at[i - 1] + length * t;
            }
            offLane = _corners.Length > 1 ? Mathf.Sqrt(best) : Flat(position - _corners[0]).magnitude;
            return along;
        }

        /// <summary>The point `along` metres down the way, clamped to its ends.</summary>
        public Vector3 PointAt(float along)
        {
            if (along <= 0f) return _corners[0];
            for (var i = 1; i < _corners.Length; i++)
            {
                if (along > _at[i]) continue;
                var span = _at[i] - _at[i - 1];
                return Vector3.Lerp(_corners[i - 1], _corners[i], span > 0.001f ? (along - _at[i - 1]) / span : 1f);
            }
            return End;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
