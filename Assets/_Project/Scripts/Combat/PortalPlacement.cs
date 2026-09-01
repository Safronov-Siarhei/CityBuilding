using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Combat
{
    /// <summary>
    /// Where a map's orc portals go, as a list of slots rather than as one hardcoded spot.
    ///
    /// This is the template the design has been waiting for: the portals are described here as
    /// (distance, bearing) pairs around the Town Hall, and OrcRaidManager's job is reduced to
    /// finding the nearest valid ground to each slot. The eventual intent is hand-authored
    /// positions per map (like the TreesArea zone), which do not exist as map data yet -- when
    /// they do, a map supplies its own list of slots and nothing downstream changes, because the
    /// spawner already works off a list.
    ///
    /// Generated as a SPIRAL: each portal gets its own bearing (evenly spread, so they never clump
    /// on one side of the settlement) and its own ring (each one step further out than the last, so
    /// they are not all the same walk away). Both the count and the two distances come from the
    /// balance sheet, so spreading the portals across the whole map is a number change, not a code
    /// change.
    ///
    /// Pure and static, with every input passed in, so the shape of the template can be tested
    /// without a scene, a Town Hall or today's sheet.
    /// </summary>
    public static class PortalPlacement
    {
        /// <summary>One portal's intended place, relative to the Town Hall.</summary>
        public readonly struct Slot
        {
            public Slot(float distanceMeters, float angleRadians)
            {
                DistanceMeters = distanceMeters;
                AngleRadians = angleRadians;
            }

            /// <summary>How far from the Town Hall, in metres.</summary>
            public float DistanceMeters { get; }

            /// <summary>Which way from the Town Hall, in radians counter-clockwise from +X.</summary>
            public float AngleRadians { get; }

            /// <summary>The offset to add to the Town Hall's position to land on this slot.</summary>
            public Vector3 Offset =>
                new Vector3(Mathf.Cos(AngleRadians) * DistanceMeters, 0f, Mathf.Sin(AngleRadians) * DistanceMeters);
        }

        /// <summary>The smallest distance a slot may sit at, whatever the sheet says. A portal on top of the Town Hall would spawn its raiders inside the settlement with nothing in between.</summary>
        public const float MinDistanceMeters = 4f;

        /// <summary>
        /// Builds the slots for one map.
        ///
        /// Clamped on every input so a mistyped sheet degrades instead of breaking: a count of zero
        /// still yields one portal (a map with none has no objective and can never be won), a
        /// distance under <see cref="MinDistanceMeters"/> is pushed out to it, and a negative step
        /// is read as no step rather than as portals marching in towards the Town Hall.
        /// </summary>
        /// <param name="count">How many portals the map gets.</param>
        /// <param name="baseDistanceMeters">How far out the first one sits.</param>
        /// <param name="distanceStepMeters">How much further out each subsequent one sits.</param>
        /// <param name="angleOffsetRadians">Which way the first one lies. Randomised per playthrough by the caller, so the same map does not always put the same portal in the same place.</param>
        public static List<Slot> BuildTemplate(int count, float baseDistanceMeters, float distanceStepMeters, float angleOffsetRadians)
        {
            var slots = Mathf.Max(1, count);
            var baseDistance = Mathf.Max(MinDistanceMeters, baseDistanceMeters);
            var step = Mathf.Max(0f, distanceStepMeters);
            var angleStep = Mathf.PI * 2f / slots;

            var template = new List<Slot>(slots);
            for (var i = 0; i < slots; i++)
            {
                template.Add(new Slot(baseDistance + i * step, angleOffsetRadians + i * angleStep));
            }

            return template;
        }

        /// <summary>The slots for one map, at the balance sheet's current numbers.</summary>
        public static List<Slot> BuildTemplate(float angleOffsetRadians)
        {
            var balance = Core.BalanceConfig.Instance;
            return BuildTemplate(balance.PortalCount, balance.PortalDistanceFromTownHallMeters, balance.PortalDistanceStepMeters, angleOffsetRadians);
        }

        /// <summary>
        /// How far off its own bearing a slot may be pushed when the ground search cannot use its
        /// exact spot: a third of the share of the circle each portal was given.
        ///
        /// A third, so that two NEIGHBOURS displaced towards each other still cannot meet -- they
        /// start a full share apart and can close at most two thirds of it. That is the whole
        /// guarantee the spread rests on, and it is why the cap is derived from the portal count
        /// rather than being a number someone picked.
        /// </summary>
        public static float MaxBearingDeviationRadians(int count) => Mathf.PI * 2f / Mathf.Max(1, count) / 3f;

        /// <summary>
        /// The bearings to try for one slot, in the order to try them: the slot's own first, then
        /// alternating to either side of it, and never further off it than
        /// <paramref name="maxDeviationRadians"/>.
        ///
        /// Both halves of that matter, and each was learned the hard way. Alternating rather than
        /// sweeping one way round the circle, so a displaced portal stays on the side it belongs
        /// to. And CAPPED, because without a cap a slot whose whole direction is water walks right
        /// around the settlement and parks next to its neighbour -- on the real map that put two
        /// portals four degrees apart. A slot that cannot be honoured within the cap is better
        /// given up on than granted somewhere it undoes the spread; the caller widens its search
        /// along the bearing first, and only then skips the portal.
        /// </summary>
        public static IEnumerable<float> BearingsToTry(float angleRadians, int samples, float maxDeviationRadians)
        {
            yield return angleRadians;

            var count = Mathf.Max(1, samples);
            var step = Mathf.PI * 2f / count;
            var maxDeviation = Mathf.Max(0f, maxDeviationRadians);

            for (var i = 1; i <= count / 2 && i * step <= maxDeviation; i++)
            {
                yield return angleRadians + i * step;
                yield return angleRadians - i * step;
            }
        }
    }
}
