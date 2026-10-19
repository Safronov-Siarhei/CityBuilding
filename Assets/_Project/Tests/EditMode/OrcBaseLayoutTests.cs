using System.Collections.Generic;
using CityBuilder.Combat;
using NUnit.Framework;
using UnityEngine;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// The plans of the walled orc bases (the user's sketches, 2026-10-16): every plan has to be
    /// buildable on its own square, its wall has to be closed with one way in, and every kind of
    /// guard it posts has to have the spawner that tops that kind up.
    /// </summary>
    public class OrcBaseLayoutTests
    {
        private static IEnumerable<int> Levels()
        {
            for (var level = 1; level <= OrcBaseLayout.MaxLevel; level++) yield return level;
        }

        private static HashSet<Vector2Int> Cells(CampPiece piece)
        {
            var cells = new HashSet<Vector2Int>();
            for (var x = 0; x < piece.Size.x; x++)
            {
                for (var z = 0; z < piece.Size.y; z++) cells.Add(piece.Min + new Vector2Int(x, z));
            }
            return cells;
        }

        private static readonly HashSet<Vector2Int> PortalCells = new HashSet<Vector2Int>
        {
            new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(0, 0),
        };

        [Test]
        public void EveryPlotIsInsideTheSquareAndNoTwoOverlap([ValueSource(nameof(Levels))] int level)
        {
            var layout = OrcBaseLayout.For(level);
            var taken = new HashSet<Vector2Int>(PortalCells);
            foreach (var piece in layout.Pieces)
            {
                foreach (var cell in Cells(piece))
                {
                    Assert.IsTrue(cell.x >= -layout.HalfExtent && cell.x < layout.HalfExtent
                                  && cell.y >= -layout.HalfExtent && cell.y < layout.HalfExtent,
                        $"Level {level}: the {piece.Kind} at {piece.Min} leaves the camp's square.");
                    Assert.IsTrue(taken.Add(cell), $"Level {level}: the {piece.Kind} at {piece.Min} overlaps another plot at {cell}.");
                }
            }
        }

        [Test]
        public void NoGuardStandsOnAPlot([ValueSource(nameof(Levels))] int level)
        {
            var layout = OrcBaseLayout.For(level);
            var taken = new HashSet<Vector2Int>(PortalCells);
            foreach (var piece in layout.Pieces) taken.UnionWith(Cells(piece));
            foreach (var guard in layout.Guards)
            {
                var cell = new Vector2Int(Mathf.FloorToInt(guard.Local.x), Mathf.FloorToInt(guard.Local.y));
                Assert.IsFalse(taken.Contains(cell), $"Level {level}: the {guard.Kind} posted at {guard.Local} stands on a building.");
            }
        }

        [Test]
        public void EveryKindOfGuardHasItsSpawner([ValueSource(nameof(Levels))] int level)
        {
            var layout = OrcBaseLayout.For(level);
            var spawned = new HashSet<OrcKind>();
            foreach (var piece in layout.Pieces)
            {
                var kind = OrcBaseLayout.SpawnedBy(piece.Kind);
                if (kind.HasValue) spawned.Add(kind.Value);
            }
            foreach (var guard in layout.Guards)
            {
                Assert.IsTrue(spawned.Contains(guard.Kind), $"Level {level} posts a {guard.Kind} but has nothing to replace it.");
            }
        }

        /// <summary>The user's sketches, counted: what each level has.</summary>
        [Test]
        public void TheSketchesAreBuiltAsDrawn()
        {
            int Count(int level, OrcCampBuildingKind kind) =>
                new List<CampPiece>(OrcBaseLayout.For(level).Pieces).FindAll(p => p.Kind == kind).Count;
            int Guards(int level, OrcKind kind) =>
                new List<GuardSlot>(OrcBaseLayout.For(level).Guards).FindAll(g => g.Kind == kind).Count;

            Assert.AreEqual(4, Count(1, OrcCampBuildingKind.Watchtower));
            Assert.AreEqual(0, Count(1, OrcCampBuildingKind.Palisade), "Level 1 has no wall.");
            Assert.AreEqual(4, Guards(1, OrcKind.Raider));

            Assert.AreEqual(4, Count(2, OrcCampBuildingKind.Watchtower));
            Assert.AreEqual(1, Count(2, OrcCampBuildingKind.Gate));
            Assert.AreEqual(1, Count(2, OrcCampBuildingKind.Totem));
            Assert.AreEqual(1, Count(2, OrcCampBuildingKind.Storehouse));
            Assert.AreEqual(8, Guards(2, OrcKind.Raider));
            Assert.AreEqual(4, Guards(2, OrcKind.Brute));

            Assert.AreEqual(8, Count(3, OrcCampBuildingKind.Watchtower));
            Assert.AreEqual(1, Count(3, OrcCampBuildingKind.Gate));
            Assert.AreEqual(1, Count(3, OrcCampBuildingKind.GateTower));
            Assert.AreEqual(2, Count(3, OrcCampBuildingKind.Storehouse));
            Assert.AreEqual(13, Guards(3, OrcKind.Raider));
            Assert.AreEqual(4, Guards(3, OrcKind.Brute));
            Assert.AreEqual(14, Guards(3, OrcKind.Thrower));
        }

        /// <summary>Each ring of wall is closed: walking round it from the gate meets nothing but wall and the gate.</summary>
        [Test]
        public void EveryRingIsClosedWithOneGate([ValueSource(nameof(Levels))] int level)
        {
            var layout = OrcBaseLayout.For(level);
            var wall = new HashSet<Vector2Int>();
            foreach (var piece in layout.Pieces)
            {
                if (OrcBaseLayout.IsWall(piece.Kind)) wall.UnionWith(Cells(piece));
            }
            if (level == 1)
            {
                Assert.IsEmpty(wall);
                return;
            }

            // flood the outside from the square's corner: the portal must not be reachable
            var half = layout.HalfExtent + 1;
            var seen = new HashSet<Vector2Int>();
            var open = new Queue<Vector2Int>();
            open.Enqueue(new Vector2Int(-half, -half));
            while (open.Count > 0)
            {
                var cell = open.Dequeue();
                if (cell.x < -half || cell.x > half || cell.y < -half || cell.y > half) continue;
                if (wall.Contains(cell) || !seen.Add(cell)) continue;
                open.Enqueue(cell + Vector2Int.up);
                open.Enqueue(cell + Vector2Int.down);
                open.Enqueue(cell + Vector2Int.left);
                open.Enqueue(cell + Vector2Int.right);
            }
            Assert.IsFalse(seen.Contains(Vector2Int.zero), $"Level {level}: the outside reaches the portal -- the wall has a hole.");
        }

        /// <summary>
        /// The main base (the user's sketch, 2026-10-19): every plot inside its bounds, none on
        /// another, none on its mountains or the great portal; the walls and the mountains between
        /// them leave no way in but the three gates; and it has what the sketch has.
        /// </summary>
        [Test]
        public void TheMainBaseIsBuildableClosedAndAsDrawn()
        {
            var main = OrcBaseLayout.For(OrcBaseLayout.MainLevel);
            Assert.IsTrue(main.IsMain);
            Assert.AreEqual(4, main.PortalSize);

            var taken = new HashSet<Vector2Int>();
            for (var x = -2; x < 2; x++)
            {
                for (var z = -2; z < 2; z++) taken.Add(new Vector2Int(x, z));
            }
            var rock = new HashSet<Vector2Int>();
            foreach (var m in main.Mountains)
            {
                foreach (var cell in m.allPositionsWithin) rock.Add(cell);
            }
            var wall = new HashSet<Vector2Int>(rock);
            foreach (var piece in main.Pieces)
            {
                foreach (var cell in Cells(piece))
                {
                    Assert.IsTrue(main.Bounds.Contains(cell), $"The {piece.Kind} at {piece.Min} leaves the main base.");
                    Assert.IsFalse(rock.Contains(cell), $"The {piece.Kind} at {piece.Min} stands in the mountains.");
                    Assert.IsTrue(taken.Add(cell), $"The {piece.Kind} at {piece.Min} overlaps another plot at {cell}.");
                    if (OrcBaseLayout.IsWall(piece.Kind)) wall.Add(cell);
                }
            }
            foreach (var guard in main.Guards)
            {
                var cell = new Vector2Int(Mathf.FloorToInt(guard.Local.x), Mathf.FloorToInt(guard.Local.y));
                Assert.IsFalse(taken.Contains(cell) || rock.Contains(cell), $"The {guard.Kind} posted at {guard.Local} stands on a building or in the rock.");
            }

            // flood from outside the base: the great portal must be out of reach
            var b = main.Bounds;
            var seen = new HashSet<Vector2Int>();
            var open = new Queue<Vector2Int>();
            open.Enqueue(new Vector2Int(b.xMin - 1, b.yMin - 1));
            while (open.Count > 0)
            {
                var cell = open.Dequeue();
                if (cell.x < b.xMin - 1 || cell.x > b.xMax || cell.y < b.yMin - 1 || cell.y > b.yMax) continue;
                if (wall.Contains(cell) || !seen.Add(cell)) continue;
                open.Enqueue(cell + Vector2Int.up);
                open.Enqueue(cell + Vector2Int.down);
                open.Enqueue(cell + Vector2Int.left);
                open.Enqueue(cell + Vector2Int.right);
            }
            Assert.IsFalse(seen.Contains(Vector2Int.zero), "The outside reaches the great portal -- the walls and the mountains leave a hole.");

            int Count(OrcCampBuildingKind kind) => new List<CampPiece>(main.Pieces).FindAll(p => p.Kind == kind).Count;
            Assert.AreEqual(2, Count(OrcCampBuildingKind.Totem), "Two totems shield the great portal.");
            Assert.AreEqual(3, Count(OrcCampBuildingKind.GateTower), "Three stone gates under towers, one per wall.");
            Assert.AreEqual(1, Count(OrcCampBuildingKind.ChiefHouse));
            Assert.AreEqual(1, Count(OrcCampBuildingKind.Treasury), "One treasury, the chief's.");
            Assert.AreEqual(1, new List<GuardSlot>(main.Guards).FindAll(g => g.Kind == OrcKind.Boss).Count, "The chief, the miniboss.");
            var spawned = new HashSet<OrcKind>();
            foreach (var piece in main.Pieces)
            {
                var kind = OrcBaseLayout.SpawnedBy(piece.Kind);
                if (kind.HasValue) spawned.Add(kind.Value);
            }
            foreach (var guard in main.Guards)
            {
                Assert.IsTrue(spawned.Contains(guard.Kind), $"The main base posts a {guard.Kind} but has nothing to replace it.");
            }
        }

        [Test]
        public void TurningThePlanKeepsItOnWholeCells()
        {
            var centre = new Vector2Int(50, 50);
            for (var q = 0; q < 4; q++)
            {
                OrcBaseLayout.ToWorld(new Vector2Int(-1, -1), new Vector2Int(2, 2), centre, q, out var min, out var size);
                Assert.AreEqual(new Vector2Int(49, 49), min, $"The portal's own plot moved at {q} quarter turns.");
                Assert.AreEqual(new Vector2Int(2, 2), size);
            }
            // the gate in the front (-z) ends up on the side away from +z when turned half round
            OrcBaseLayout.ToWorld(new Vector2Int(-1, -7), new Vector2Int(2, 1), centre, 2, out var gate, out var gateSize);
            Assert.AreEqual(new Vector2Int(49, 56), gate);
            Assert.AreEqual(new Vector2Int(2, 1), gateSize);
            OrcBaseLayout.ToWorld(new Vector2Int(-1, -7), new Vector2Int(2, 1), centre, 1, out _, out var turnedSize);
            Assert.AreEqual(new Vector2Int(1, 2), turnedSize);
        }

        /// <summary>The prisoners' cages (2026-10-19): one in the level-1 and level-2 camps, two in level 3, three on the main base, each at its camp's level.</summary>
        [Test]
        public void EveryCampHoldsItsCages()
        {
            var expected = new Dictionary<int, int> { { 1, 1 }, { 2, 1 }, { 3, 2 }, { OrcBaseLayout.MainLevel, 3 } };
            foreach (var pair in expected)
            {
                var cages = new List<CampPiece>(OrcBaseLayout.For(pair.Key).Pieces).FindAll(p => p.Kind == OrcCampBuildingKind.Cage);
                Assert.AreEqual(pair.Value, cages.Count, $"The level-{pair.Key} camp has the wrong number of cages.");
                foreach (var cage in cages)
                {
                    Assert.AreEqual(pair.Key, cage.Level, $"A cage in the level-{pair.Key} camp is drawn at another level.");
                    Assert.AreEqual(Vector2Int.one, cage.Size, "A cage is a 1x1 plot.");
                }
            }
        }

        [Test]
        public void CampsGrowWithTheScoreAndFurtherOnesSooner()
        {
            Assert.AreEqual(1, OrcBaseLayout.LevelFor(0, 0, 200, 450, 40, 3));
            Assert.AreEqual(2, OrcBaseLayout.LevelFor(200, 0, 200, 450, 40, 3));
            Assert.AreEqual(3, OrcBaseLayout.LevelFor(450, 0, 200, 450, 40, 3));
            Assert.AreEqual(1, OrcBaseLayout.LevelFor(119, 2, 200, 450, 40, 3));
            Assert.AreEqual(2, OrcBaseLayout.LevelFor(120, 2, 200, 450, 40, 3), "A camp two ranks out grows 80 points sooner.");
            Assert.AreEqual(2, OrcBaseLayout.LevelFor(10000, 0, 200, 450, 40, 2), "A camp never grows past what its ground has room for.");
        }
    }
}
