using System.Collections;
using System.Collections.Generic;
using System.Text;
using CityBuilder.Buildings;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Saving;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// A real fence line, built out of real prefabs on the real map, checked for the one thing the
    /// EditMode FenceShapeTests structurally cannot see: whether the chosen models actually MEET.
    ///
    /// FenceShape's 16 neighbour combinations are pure logic and already covered. What is not
    /// covered by them is everything between that decision and the picture: the FBX pair, the
    /// re-centring SetupProject does from renderer bounds, the wrapper rotation, the cell the
    /// segment was placed on. A corner model whose geometry sits in the middle of its cell instead
    /// of reaching both edges passes every existing test and leaves a visible hole in the wall.
    ///
    /// So these tests measure the drawn geometry against the cell borders it is supposed to reach,
    /// and print what they measured -- the numbers are worth reading even when everything passes,
    /// because "just inside tolerance" is how the next re-export will fail.
    /// </summary>
    public class FenceAutotilingTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map1";

        /// <summary>A rectangle big enough to contain all four corner orientations plus straight runs on both axes.</summary>
        private static readonly Vector2Int RingSize = new Vector2Int(6, 5);

        /// <summary>
        /// How far short of the shared cell border a segment's geometry may stop. Two neighbours
        /// each allowed this much leaves at most twice it as a hole; at a 1m cell, 5cm a side is
        /// about the width of a fence post -- visible if you look for it, not a hole you can walk
        /// through. Tightening this is a decision about the models, not about the test.
        /// </summary>
        private const float MaxGapPerSide = 0.05f;

        /// <summary>How far a segment may spill over its own cell before it starts intersecting whatever stands next door.</summary>
        private const float MaxOverhang = 0.15f;

        private static bool _sceneLoaded;

        private readonly List<BuildingInstance> _placed = new List<BuildingInstance>();
        private readonly Dictionary<Vector2Int, BuildingInstance> _segments = new Dictionary<Vector2Int, BuildingInstance>();
        private Vector2Int _origin;

        [UnitySetUp]
        public IEnumerator BuildTheRing()
        {
            LogAssert.ignoreFailingMessages = true;

            if (!_sceneLoaded)
            {
                Time.timeScale = 1f;
                GameSessionIntent.NewGameMapId = MapId;
                SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
                yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);
                _sceneLoaded = true;
            }

            ModalGate.SetBlocked(false);
            Time.timeScale = 1f;

            var fence = PlaytestWorld.Building("Fence");
            Assert.IsNotNull(fence, "No Fence in the building catalogue -- the scene's BuildingPlacer has no such entry.");

            _origin = PlaytestWorld.FindFreeArea(RingSize);
            Assert.AreNotEqual(new Vector2Int(-1, -1), _origin, $"Nowhere on the map has {RingSize.x}x{RingSize.y} free dry cells to build a test fence on.");

            foreach (var cell in RingCells())
            {
                _segments[cell] = PlaytestWorld.Place(fence, cell);
                _placed.Add(_segments[cell]);
            }
            yield return null;
        }

        [TearDown]
        public void ClearTheRing()
        {
            foreach (var segment in _placed)
            {
                // Immediate, not deferred: the next test looks for free cells straight away, and
                // OnDestroy is what hands them back.
                if (segment != null) Object.DestroyImmediate(segment.gameObject);
            }
            _placed.Clear();
            _segments.Clear();
        }

        /// <summary>The autotiling rule, but applied to the models the player actually sees rather than to FenceShape's return value.</summary>
        [Test]
        public void EverySegment_ShowsTheModelItsNeighboursCallFor()
        {
            foreach (var pair in _segments)
            {
                var cell = pair.Key;
                FenceShape.Resolve(Connects(cell, Vector2Int.up), Connects(cell, Vector2Int.right),
                    Connects(cell, Vector2Int.down), Connects(cell, Vector2Int.left),
                    out var expectedVariant, out var expectedSteps);

                var straight = ModelWrapper(pair.Value, "Straight");
                var corner = ModelWrapper(pair.Value, "Corner");
                Assert.IsNotNull(straight, $"The fence prefab at {cell} has no 'Straight' model child at all.");
                Assert.IsNotNull(corner, $"The fence prefab at {cell} has no 'Corner' model child at all.");

                var expected = expectedVariant == FenceVariant.Corner ? corner : straight;
                var other = expectedVariant == FenceVariant.Corner ? straight : corner;

                Assert.IsTrue(expected.gameObject.activeSelf,
                    $"The segment at {cell} ({Describe(cell)}) should be showing its {expectedVariant} model and is not.");
                Assert.IsFalse(other.gameObject.activeSelf,
                    $"The segment at {cell} ({Describe(cell)}) is showing BOTH models at once -- they will z-fight.");

                var actualSteps = Mathf.RoundToInt(expected.rotation.eulerAngles.y / 90f) % 4;
                Assert.AreEqual(expectedSteps, actualSteps,
                    $"The segment at {cell} ({Describe(cell)}) is turned {actualSteps * 90} degrees instead of {expectedSteps * 90}.");
            }
        }

        /// <summary>
        /// The gap check. Every side of a segment that has a fence next to it must have geometry
        /// reaching that shared border, or the wall has a hole in it exactly where the two pieces
        /// were supposed to join -- the failure mode a corner model is most likely to have.
        /// </summary>
        [Test]
        public void EverySegment_ReachesTheBorderItSharesWithItsNeighbours()
        {
            var half = GridManager.Instance.CellSize * 0.5f;
            var report = new StringBuilder("[Playtest] fence reach per side (metres short of the cell border, + is a gap):\n");
            var worst = 0f;
            var worstWhere = string.Empty;

            foreach (var pair in _segments)
            {
                var cell = pair.Key;
                var centre = PlaytestWorld.CellCenter(cell);
                var bounds = VisibleBounds(pair.Value);
                report.Append($"  {cell} {Describe(cell),-22}");

                foreach (var direction in Directions)
                {
                    if (!Connects(cell, direction)) continue;

                    var reach = ReachTowards(bounds, centre, direction);
                    var shortfall = half - reach;
                    report.Append($" {Name(direction)}:{shortfall:+0.000;-0.000}");

                    if (shortfall > worst)
                    {
                        worst = shortfall;
                        worstWhere = $"{cell} ({Describe(cell)}) towards {Name(direction)}";
                    }
                }
                report.Append('\n');
            }

            Debug.Log(report.ToString());
            Assert.LessOrEqual(worst, MaxGapPerSide,
                $"The fence stops {worst:0.000}m short of a border it shares with a neighbour at {worstWhere} -- " +
                "the two pieces do not meet, and the wall has a visible hole there.");
        }

        /// <summary>A segment that spills over its cell overlaps whatever is built next door; one that floats or sinks reads as broken however well it tiles.</summary>
        [Test]
        public void EverySegment_StaysOnItsOwnCellAndOnTheGround()
        {
            var half = GridManager.Instance.CellSize * 0.5f;
            var groundHeight = GridManager.Instance.GroundHeight;

            foreach (var pair in _segments)
            {
                var cell = pair.Key;
                var centre = PlaytestWorld.CellCenter(cell);
                var bounds = VisibleBounds(pair.Value);

                Assert.LessOrEqual(ReachTowards(bounds, centre, Vector2Int.up), half + MaxOverhang, $"The segment at {cell} overhangs its cell to the north.");
                Assert.LessOrEqual(ReachTowards(bounds, centre, Vector2Int.down), half + MaxOverhang, $"The segment at {cell} overhangs its cell to the south.");
                Assert.LessOrEqual(ReachTowards(bounds, centre, Vector2Int.right), half + MaxOverhang, $"The segment at {cell} overhangs its cell to the east.");
                Assert.LessOrEqual(ReachTowards(bounds, centre, Vector2Int.left), half + MaxOverhang, $"The segment at {cell} overhangs its cell to the west.");

                Assert.AreEqual(groundHeight, bounds.min.y, 0.05f,
                    $"The segment at {cell} sits at y={bounds.min.y:0.000} instead of standing on the ground at y={groundHeight:0.000}.");
            }
        }

        /// <summary>What a raider breaking through is supposed to leave behind: a hole, with the two pieces either side turned back into dead ends rather than still pretending to be a straight run.</summary>
        [UnityTest]
        public IEnumerator BreakingASegment_TurnsItsNeighboursIntoDeadEnds()
        {
            // The middle of the ring's south edge: both its neighbours are plain straight runs, so
            // the only thing that can change their shape is losing the piece between them.
            var broken = new Vector2Int(_origin.x + RingSize.x / 2, _origin.y);
            var west = broken + Vector2Int.left;
            var east = broken + Vector2Int.right;
            Assert.IsTrue(_segments.ContainsKey(west) && _segments.ContainsKey(east), "The test picked a cell that is not in the middle of a straight run.");

            PlaytestWorld.Demolish(_segments[broken]);
            _placed.Remove(_segments[broken]);
            _segments.Remove(broken);
            yield return null;

            Assert.IsFalse(FenceNetwork.Instance.Connects(broken), "The destroyed segment is still registered as part of the fence line.");

            // A dead end pointing east/west shows the straight model laid along the east-west axis
            // (FenceShape's "line up with whichever side does have a neighbour").
            foreach (var cell in new[] { west, east })
            {
                var straight = ModelWrapper(_segments[cell], "Straight");
                Assert.IsTrue(straight.gameObject.activeSelf, $"The segment at {cell} next to the hole is not showing the straight model.");
                Assert.AreEqual(1, Mathf.RoundToInt(straight.rotation.eulerAngles.y / 90f) % 4,
                    $"The dead end at {cell} did not re-shape when its neighbour was destroyed.");
            }
        }

        /// <summary>Not an assertion -- the photographs. See PlaytestCapture for why a test suite takes them.</summary>
        [UnityTest]
        public IEnumerator Photograph_TheFinishedFence()
        {
            var centre = PlaytestWorld.CellCenter(_origin + new Vector2Int(RingSize.x / 2, RingSize.y / 2));
            yield return PlaytestCapture.Shoot("fence-ring", centre, 16f, 45f, 25f);

            // Close enough to see whether the two models actually touch at the corner.
            var corner = PlaytestWorld.CellCenter(_origin);
            yield return PlaytestCapture.Shoot("fence-corner", corner, 5f, 25f, -135f);
        }

        private static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        private static string Name(Vector2Int direction)
        {
            if (direction == Vector2Int.up) return "N";
            if (direction == Vector2Int.right) return "E";
            if (direction == Vector2Int.down) return "S";
            return "W";
        }

        /// <summary>How far this segment's geometry extends from its cell centre towards one side.</summary>
        private static float ReachTowards(Bounds bounds, Vector3 centre, Vector2Int direction)
        {
            if (direction == Vector2Int.up) return bounds.max.z - centre.z;
            if (direction == Vector2Int.down) return centre.z - bounds.min.z;
            if (direction == Vector2Int.right) return bounds.max.x - centre.x;
            return centre.x - bounds.min.x;
        }

        /// <summary>World-space extent of what this segment currently DRAWS -- inactive models are excluded, which is the point.</summary>
        private static Bounds VisibleBounds(BuildingInstance segment)
        {
            var renderers = segment.GetComponentsInChildren<MeshRenderer>();
            Assert.Greater(renderers.Length, 0, $"The fence segment at {segment.OriginCell} draws nothing at all.");

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static Transform ModelWrapper(BuildingInstance segment, string wrapperName)
        {
            return segment.transform.Find(wrapperName);
        }

        private bool Connects(Vector2Int cell, Vector2Int direction)
        {
            return _segments.ContainsKey(cell + direction);
        }

        private string Describe(Vector2Int cell)
        {
            var local = cell - _origin;
            var onWestEdge = local.x == 0;
            var onEastEdge = local.x == RingSize.x - 1;
            var onSouthEdge = local.y == 0;
            var onNorthEdge = local.y == RingSize.y - 1;

            if ((onWestEdge || onEastEdge) && (onSouthEdge || onNorthEdge)) return "corner";
            return onSouthEdge || onNorthEdge ? "east-west run" : "north-south run";
        }

        /// <summary>The perimeter of the rectangle, in no particular order -- the shape is what matters, not the order it was laid in.</summary>
        private IEnumerable<Vector2Int> RingCells()
        {
            for (var x = 0; x < RingSize.x; x++)
            {
                yield return _origin + new Vector2Int(x, 0);
                yield return _origin + new Vector2Int(x, RingSize.y - 1);
            }
            for (var y = 1; y < RingSize.y - 1; y++)
            {
                yield return _origin + new Vector2Int(0, y);
                yield return _origin + new Vector2Int(RingSize.x - 1, y);
            }
        }
    }
}
