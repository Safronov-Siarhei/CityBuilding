using System.Linq;
using CityBuilder.Combat;
using CityBuilder.Invasion;
using NUnit.Framework;
using UnityEngine;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// The round orc camp's plan (the scheme the user approved, 2026-10-21): what stands in it, that
    /// both rings close with a gate facing the base, and that the waves' way from the portal out
    /// through both gates is clear.
    /// </summary>
    public class InvasionCampLayoutTests
    {
        [Test]
        public void TheCamp_HoldsWhatTheSchemeSays()
        {
            var pieces = InvasionCampLayout.Pieces();
            int Count(OrcCampBuildingKind kind) => pieces.Count(p => p.Kind == InvasionCampLayout.PieceKind.Building && p.Building == kind);
            Assert.AreEqual(InvasionCampLayout.OuterSections + InvasionCampLayout.InnerSections - 2, pieces.Count(p => p.Kind == InvasionCampLayout.PieceKind.Wall));
            Assert.AreEqual(2, pieces.Count(p => p.Kind == InvasionCampLayout.PieceKind.Gate), "Not a gate in each ring.");
            Assert.IsTrue(pieces.Where(p => p.Kind == InvasionCampLayout.PieceKind.Gate).All(p => p.Angle == 0f), "A gate does not face the base.");
            Assert.AreEqual(6 + 4, Count(OrcCampBuildingKind.Watchtower), "Six towers on the wall and four by the portal.");
            Assert.AreEqual(4, Count(OrcCampBuildingKind.Totem));
            Assert.AreEqual(2, Count(OrcCampBuildingKind.WarTent));
            Assert.AreEqual(2, Count(OrcCampBuildingKind.ThrowerRange));
            Assert.AreEqual(2, Count(OrcCampBuildingKind.Storehouse));
        }

        [Test]
        public void TheRings_Close()
        {
            foreach (var (radius, sections) in new[] { (InvasionCampLayout.OuterRadius, InvasionCampLayout.OuterSections), (InvasionCampLayout.InnerRadius, InvasionCampLayout.InnerSections) })
            {
                var chord = 2f * radius * Mathf.Sin(Mathf.PI / sections);
                Assert.LessOrEqual(chord, InvasionCampLayout.WallLength + 0.1f, $"The ring of {radius} m has gaps between its sections.");
                Assert.GreaterOrEqual(chord, InvasionCampLayout.WallLength - 0.2f, $"The ring of {radius} m has its sections piled on each other.");
            }
        }

        [Test]
        public void TheWayOut_FromThePortal_IsClear()
        {
            foreach (var piece in InvasionCampLayout.Pieces().Where(p => p.Kind == InvasionCampLayout.PieceKind.Building))
            {
                InvasionCampLayout.ToWorld(piece, Vector3.zero, 0f, out var at, out _);
                if (at.z < InvasionCampLayout.WaveExitMeters - 1f) continue;
                Assert.Greater(Mathf.Abs(at.x), piece.Size.x / 2f + 0.4f, $"A {piece.Building} at {at} stands in the waves' way out.");
            }
        }
    }
}
