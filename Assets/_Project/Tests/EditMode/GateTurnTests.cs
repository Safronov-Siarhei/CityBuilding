using System.Collections.Generic;
using CityBuilder.Buildings;
using NUnit.Framework;
using UnityEngine;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// A gate held between two fence pieces turns to stand in their line, as a fence piece joins its
    /// neighbours (the user, 2026-10-23). At no turn a gate's wall runs along Z, so a fence along X
    /// wants a quarter turn.
    /// </summary>
    public class GateTurnTests
    {
        private static System.Func<Vector2Int, bool> Fences(params Vector2Int[] cells)
        {
            var set = new HashSet<Vector2Int>(cells);
            return set.Contains;
        }

        [Test]
        public void AGate_TakesTheWayOfTheFenceItStandsIn()
        {
            var at = new Vector2Int(10, 10);
            Assert.AreEqual(1, BuildingPlacer.GateTurnInAFence(at, Vector2Int.one, Fences(at + Vector2Int.left, at + Vector2Int.right)), "Between fences east and west it does not lie along X.");
            Assert.AreEqual(0, BuildingPlacer.GateTurnInAFence(at, Vector2Int.one, Fences(at + Vector2Int.up, at + Vector2Int.down)), "Between fences north and south it does not lie along Z.");
            Assert.AreEqual(1, BuildingPlacer.GateTurnInAFence(at, Vector2Int.one, Fences(at + Vector2Int.right)), "At a fence's end it does not carry the line on.");
            Assert.IsNull(BuildingPlacer.GateTurnInAFence(at, Vector2Int.one, Fences()), "A lone gate turned itself.");
            Assert.IsNull(BuildingPlacer.GateTurnInAFence(at, Vector2Int.one, Fences(at + Vector2Int.right, at + Vector2Int.up)), "A gate at a corner turned itself.");
        }

        [Test]
        public void AFortifiedGate_LooksPastItsOwnEnds()
        {
            var at = new Vector2Int(10, 10);
            var footprint = new Vector2Int(1, 3);
            Assert.AreEqual(1, BuildingPlacer.GateTurnInAFence(at, footprint, Fences(at + Vector2Int.left * 2, at + Vector2Int.right * 2)), "A fortified gate between fences east and west does not lie along X.");
            Assert.AreEqual(0, BuildingPlacer.GateTurnInAFence(at, footprint, Fences(at + Vector2Int.up * 2, at + Vector2Int.down * 2)), "A fortified gate between fences north and south does not lie along Z.");
        }
    }
}
