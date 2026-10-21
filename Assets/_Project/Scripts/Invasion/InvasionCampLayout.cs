using System.Collections.Generic;
using CityBuilder.Combat;
using UnityEngine;

namespace CityBuilder.Invasion
{
    /// <summary>
    /// The plan of an Invasion round orc camp, after the user's description (2026-10-21; the scheme
    /// they approved): an outer palisade with towers, an inner wall, between them the spawners of
    /// orcs and throwers and the storehouses, in the middle the portal with four totems and four
    /// towers. Both rings have an open gate facing the base -- the waves march out of the portal
    /// through both -- and nothing stands on that straight line.
    ///
    /// Pure: angles are degrees round the camp's middle from its front (0 = towards the base,
    /// positive = clockwise seen from above), radii are metres. InvasionOrcCamp builds it.
    /// </summary>
    public static class InvasionCampLayout
    {
        public const float OuterRadius = 15f;
        public const float InnerRadius = 8.5f;
        public const int OuterSections = 24;
        public const int InnerSections = 14;
        /// <summary>Between the rings: the spawners and the storehouses.</summary>
        public const float YardRadius = 11.75f;
        public const float TotemRadius = 3f;
        public const float InnerTowerRadius = 5.5f;
        /// <summary>A section of ring wall (Tools/blender/orcringcamp1.py RING_SECTION), and its thickness for the NavMesh.</summary>
        public const float WallLength = 3.9f;
        public const float WallThickness = 0.4f;
        /// <summary>How far in front of the portal, towards the base, a wave steps out.</summary>
        public const float WaveExitMeters = 2.2f;

        public enum PieceKind { Wall, Gate, Building }

        public struct Piece
        {
            public PieceKind Kind;
            public OrcCampBuildingKind Building;
            public float Radius;
            public float Angle;
            public Vector2 Size;
        }

        public static List<Piece> Pieces()
        {
            var pieces = new List<Piece>();
            Ring(pieces, OuterRadius, OuterSections);
            Ring(pieces, InnerRadius, InnerSections);

            // six towers in the outer ring: a pair flanking the gate, a pair on each side
            foreach (var angle in new[] { -7.5f, 7.5f, -82.5f, 82.5f, -157.5f, 157.5f })
            {
                pieces.Add(Building(OrcCampBuildingKind.Watchtower, OuterRadius, angle, 2f));
            }
            // the yard: storehouses by the gate, the war tents on the left, the throwers' ranges on the right
            pieces.Add(Building(OrcCampBuildingKind.Storehouse, YardRadius, -40f, 2f));
            pieces.Add(Building(OrcCampBuildingKind.Storehouse, YardRadius, 40f, 2f));
            pieces.Add(Building(OrcCampBuildingKind.WarTent, YardRadius, -95f, 2f));
            pieces.Add(Building(OrcCampBuildingKind.WarTent, YardRadius, -145f, 2f));
            pieces.Add(Building(OrcCampBuildingKind.ThrowerRange, YardRadius, 95f, 2f));
            pieces.Add(Building(OrcCampBuildingKind.ThrowerRange, YardRadius, 145f, 2f));
            // round the portal, on the diagonals: four totems close in, four towers behind them
            foreach (var angle in new[] { -45f, 45f, -135f, 135f })
            {
                pieces.Add(Building(OrcCampBuildingKind.Totem, TotemRadius, angle, 1f));
                pieces.Add(Building(OrcCampBuildingKind.Watchtower, InnerTowerRadius, angle, 2f));
            }
            return pieces;
        }

        /// <summary>A ring of wall sections, the one facing the base (angle 0) an open gate.</summary>
        private static void Ring(List<Piece> pieces, float radius, int sections)
        {
            for (var k = 0; k < sections; k++)
            {
                pieces.Add(new Piece
                {
                    Kind = k == 0 ? PieceKind.Gate : PieceKind.Wall,
                    Building = OrcCampBuildingKind.Palisade,
                    Radius = radius,
                    Angle = k * 360f / sections,
                    Size = new Vector2(WallLength, WallThickness),
                });
            }
        }

        private static Piece Building(OrcCampBuildingKind kind, float radius, float angle, float size) =>
            new Piece { Kind = PieceKind.Building, Building = kind, Radius = radius, Angle = angle, Size = new Vector2(size, size) };

        /// <summary>
        /// Where a piece stands in the world for a camp at `centre` whose front faces `frontYaw`
        /// (degrees, Unity's: 0 = +Z), and how it is turned: a wall section or gate runs along the
        /// ring (its X along the tangent), a building faces the camp's middle.
        /// </summary>
        public static void ToWorld(Piece piece, Vector3 centre, float frontYaw, out Vector3 position, out float yaw)
        {
            var bearing = frontYaw + piece.Angle;
            var rad = bearing * Mathf.Deg2Rad;
            position = centre + new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * piece.Radius;
            yaw = piece.Kind == PieceKind.Building ? bearing + 180f : bearing;
        }

        /// <summary>The yaw that faces from `centre` towards `target`.</summary>
        public static float YawTowards(Vector3 centre, Vector3 target)
        {
            var d = target - centre;
            return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        }
    }
}
