using System.Collections;
using CityBuilder.Maps;
using UnityEngine;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// Shared "the map is actually ready" wait for PlayMode tests.
    ///
    /// A fixed `yield return new WaitForSeconds(0.5f)` after LoadScene is not that, and pretending
    /// it is produced a genuinely confusing flake: with two test classes in one Unity process, the
    /// second one's rays sometimes hit nothing at all while a straight-down probe at the same spot
    /// still found ground. Two reasons, both about racing the engine rather than about the game:
    ///
    /// - The wait is in SCALED time. A class that leaves Time.timeScale at 8 turns "half a second"
    ///   into 60ms of real time.
    /// - Colliders instantiated during Start are not in the physics scene at their final pose until
    ///   transforms are synced (Physics.autoSyncTransforms is off by default). The map's meshes
    ///   carry a corrective 90-degree rotation, so querying too early can hit a ground slab that
    ///   PhysX still has lying in the wrong plane -- which is exactly what "the vertical ray hits,
    ///   the angled ray doesn't" looks like.
    ///
    /// So: wait for the map to report itself applied and populated, take real physics steps, and
    /// force a sync before anyone raycasts.
    /// </summary>
    public static class PlayModeScene
    {
        private const int MaxFramesToWait = 600;

        public static IEnumerator WaitUntilMapIsPhysicsReady(string mapId)
        {
            // Frames, not seconds: independent of timeScale, and it cannot hang the suite.
            for (var frame = 0; frame < MaxFramesToWait; frame++)
            {
                if (IsMapApplied(mapId) && ResourceNode.All.Count > 0) break;
                yield return null;
            }

            // Two real physics steps, so every collider created during Start is registered and
            // posed, then an explicit sync for anything moved since the last step.
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Physics.SyncTransforms();
        }

        private static bool IsMapApplied(string mapId)
        {
            var applier = MeshMapApplier.Instance;
            return applier != null && applier.CurrentMapId == mapId;
        }
    }
}
