using System.Collections;
using System.Diagnostics;
using System.Text;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Saving;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// The price of one walking citizen, broken into the operations CitizenAgent actually performs
    /// every frame -- measured in the real scene, against the real 740 resource-node colliders and
    /// the real building colliders, because the cost of a capsule sweep is entirely a function of
    /// what else is in the physics scene.
    ///
    /// PerformanceProfileTests established WHERE the frame goes (walking citizens, and nothing
    /// else). This establishes WHY, which is the half that decides what the fix is:
    ///
    /// - `CharacterController.Move` is a capsule sweep through PhysX.
    /// - `transform.position = ...` right after it (PinToGroundHeight) is not free either: writing
    ///   a transform under a CharacterController pushes the pose back into PhysX a second time.
    /// - `NavMesh.CalculatePath` is not per-frame, but at 150 citizens finishing legs constantly it
    ///   is called often enough to matter, and it is the one operation here that is genuinely heavy.
    ///
    /// Every number is microseconds per call, averaged over enough calls that the timer's own
    /// resolution is irrelevant. Absolute values are desktop values; the RATIOS are what transfer
    /// to a phone, and the ratios are what choose the fix.
    /// </summary>
    public class WalkCostTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map1";

        /// <summary>Roughly the population a developed town reaches, so the totals read as "a frame of that town".</summary>
        private const int Movers = 150;

        private const int Frames = 120;
        private const int PathCalls = 300;

        [UnitySetUp]
        public IEnumerator PrepareScene()
        {
            LogAssert.ignoreFailingMessages = true;

            Time.timeScale = 1f;
            GameSessionIntent.NewGameMapId = MapId;
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);

            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;
            ModalGate.SetBlocked(false);
            yield return null;
        }

        [TearDown]
        public void RestoreFrameRate()
        {
            Application.targetFrameRate = 60;
        }

        [UnityTest]
        public IEnumerator WhatOneStepCosts()
        {
            var grid = GridManager.Instance;
            Assert.IsNotNull(grid, "No GridManager -- the scene did not come up.");

            var controllers = new CharacterController[Movers];
            var plain = new Transform[Movers];
            var origin = grid.GetFootprintCenterWorld(new Vector2Int(grid.GridSize.x / 2, grid.GridSize.y / 2), Vector2Int.one);

            for (var i = 0; i < Movers; i++)
            {
                controllers[i] = MakeMover("Controlled" + i, origin + new Vector3(i % 30 * 0.8f, 0f, i / 30 * 0.8f), true).GetComponent<CharacterController>();
                plain[i] = MakeMover("Plain" + i, origin + new Vector3(i % 30 * 0.8f, 0f, 20f + i / 30 * 0.8f), false).transform;
            }

            // Let the colliders settle into the physics scene before anything is timed.
            yield return new WaitForFixedUpdate();
            Physics.SyncTransforms();
            for (var i = 0; i < 10; i++) yield return null;

            var step = new Vector3(0.02f, 0f, 0.01f);
            var groundHeight = grid.GroundHeight;

            var moveUs = 0.0;
            var moveAndPinUs = 0.0;
            var plainUs = 0.0;

            var watch = new Stopwatch();

            for (var frame = 0; frame < Frames; frame++)
            {
                // 1. The sweep on its own.
                watch.Restart();
                for (var i = 0; i < Movers; i++) controllers[i].Move(step);
                watch.Stop();
                moveUs += watch.Elapsed.TotalMilliseconds * 1000.0;

                // 2. The sweep plus the transform write CitizenAgent does right after it.
                watch.Restart();
                for (var i = 0; i < Movers; i++)
                {
                    controllers[i].Move(step);
                    var t = controllers[i].transform;
                    var pos = t.position;
                    pos.y = groundHeight;
                    t.position = pos;
                }
                watch.Stop();
                moveAndPinUs += watch.Elapsed.TotalMilliseconds * 1000.0;

                // 3. What the same movement costs with no CharacterController involved at all.
                watch.Restart();
                for (var i = 0; i < Movers; i++) plain[i].position += step;
                watch.Stop();
                plainUs += watch.Elapsed.TotalMilliseconds * 1000.0;

                yield return null;
            }

            // 4. Route building. Not per-frame, but every citizen asks for one every time it
            //    finishes a leg, and this is the operation with the worst constant.
            var path = new NavMeshPath();
            var pathWatch = Stopwatch.StartNew();
            var pathsFound = 0;
            for (var i = 0; i < PathCalls; i++)
            {
                var from = origin + new Vector3(i % 17 * 1.3f, 0f, i % 11 * 1.7f);
                var to = origin + new Vector3(20f - i % 13 * 1.1f, 0f, 25f - i % 7 * 2.3f);
                if (NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path)) pathsFound++;
            }
            pathWatch.Stop();

            var perFrameCalls = (double)(Frames * Movers);
            var report = new StringBuilder();
            report.AppendLine();
            report.AppendLine("=== COST OF ONE STEP (microseconds per call, real scene, desktop editor) ===");
            report.AppendLine(Line("CharacterController.Move", moveUs / perFrameCalls));
            report.AppendLine(Line("Move + PinToGroundHeight (what CitizenAgent does)", moveAndPinUs / perFrameCalls));
            report.AppendLine(Line("transform.position += (no controller)", plainUs / perFrameCalls));
            report.AppendLine(Line("NavMesh.CalculatePath (" + pathsFound + "/" + PathCalls + " found)", pathWatch.Elapsed.TotalMilliseconds * 1000.0 / PathCalls));
            report.AppendLine();
            report.AppendLine("At " + Movers + " citizens, one frame of each:");
            report.AppendLine("  Move only            : " + (moveUs / Frames / 1000.0).ToString("F3") + " ms");
            report.AppendLine("  Move + pin           : " + (moveAndPinUs / Frames / 1000.0).ToString("F3") + " ms");
            report.AppendLine("  transform only       : " + (plainUs / Frames / 1000.0).ToString("F3") + " ms");
            Debug.Log(report.ToString());

            for (var i = 0; i < Movers; i++)
            {
                if (controllers[i] != null) Object.Destroy(controllers[i].gameObject);
                if (plain[i] != null) Object.Destroy(plain[i].gameObject);
            }

            Assert.Greater(moveUs, 0.0, "Nothing was timed.");
        }

        /// <summary>
        /// Which colliders make the sweep expensive, established by removing them from the physics
        /// scene one group at a time and re-timing the identical Move.
        ///
        /// The two candidates are the map's ground mesh (one big MeshCollider the capsule sweeps
        /// against every step) and the 740 trigger boxes on trees and boulders. A trigger does not
        /// BLOCK a CharacterController -- both spawners say so in as many words -- but it is still
        /// in the broadphase, and the sweep still has to resolve every overlap it finds and raise
        /// the enter/exit events for it. "Does not block" and "is not paid for" are different
        /// claims, and only this can tell them apart.
        /// </summary>
        [UnityTest]
        public IEnumerator WhatMakesTheSweepExpensive()
        {
            var grid = GridManager.Instance;
            Assert.IsNotNull(grid, "No GridManager -- the scene did not come up.");

            var controllers = new CharacterController[Movers];
            var origin = grid.GetFootprintCenterWorld(new Vector2Int(grid.GridSize.x / 2, grid.GridSize.y / 2), Vector2Int.one);
            for (var i = 0; i < Movers; i++)
            {
                controllers[i] = MakeMover("Sweeper" + i, origin + new Vector3(i % 30 * 0.8f, 0f, i / 30 * 0.8f), true).GetComponent<CharacterController>();
            }

            yield return new WaitForFixedUpdate();
            Physics.SyncTransforms();
            for (var i = 0; i < 10; i++) yield return null;

            var nodeColliders = new System.Collections.Generic.List<Collider>();
            foreach (var node in CityBuilder.Maps.ResourceNode.All)
            {
                if (node == null) continue;
                var collider = node.GetComponent<Collider>();
                if (collider != null && collider.enabled) nodeColliders.Add(collider);
            }

            var groundColliders = new System.Collections.Generic.List<Collider>();
            foreach (var collider in Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None))
            {
                if (collider != null && collider.enabled) groundColliders.Add(collider);
            }

            var report = new StringBuilder();
            report.AppendLine();
            report.AppendLine("=== WHAT THE SWEEP IS PAYING FOR (us per Move, " + Movers + " capsules) ===");
            report.AppendLine(Line("everything on (" + nodeColliders.Count + " node triggers, " + groundColliders.Count + " mesh colliders)", TimeMoveBurst(controllers)));

            SetEnabled(nodeColliders, false);
            Physics.SyncTransforms();
            yield return null;
            report.AppendLine(Line("node triggers off", TimeMoveBurst(controllers)));

            SetEnabled(groundColliders, false);
            Physics.SyncTransforms();
            yield return null;
            report.AppendLine(Line("node triggers off + mesh colliders off", TimeMoveBurst(controllers)));

            SetEnabled(nodeColliders, true);
            Physics.SyncTransforms();
            yield return null;
            report.AppendLine(Line("mesh colliders off only", TimeMoveBurst(controllers)));

            SetEnabled(groundColliders, true);
            Physics.SyncTransforms();

            Debug.Log(report.ToString());

            for (var i = 0; i < Movers; i++)
            {
                if (controllers[i] != null) Object.Destroy(controllers[i].gameObject);
            }

            Assert.Greater(nodeColliders.Count, 0, "No resource-node colliders were found, so nothing was isolated.");
        }

        /// <summary>Times one frame's worth of Move for every capsule, averaged over a fixed burst -- no yields inside, so the whole burst is measured against one unchanging physics state.</summary>
        private static double TimeMoveBurst(CharacterController[] controllers)
        {
            var step = new Vector3(0.02f, 0f, 0.01f);
            var watch = Stopwatch.StartNew();
            var calls = 0;
            for (var burst = 0; burst < 20; burst++)
            {
                for (var i = 0; i < controllers.Length; i++)
                {
                    controllers[i].Move(step);
                    calls++;
                }
            }
            watch.Stop();
            return watch.Elapsed.TotalMilliseconds * 1000.0 / calls;
        }

        private static void SetEnabled(System.Collections.Generic.List<Collider> colliders, bool enabled)
        {
            foreach (var collider in colliders)
            {
                if (collider != null) collider.enabled = enabled;
            }
        }

        private static string Line(string label, double microseconds)
        {
            return string.Format("{0,-50} | {1,8:F3} us", label, microseconds);
        }

        private static GameObject MakeMover(string moverName, Vector3 position, bool withController)
        {
            var go = new GameObject(moverName);
            go.transform.position = position;
            if (withController)
            {
                var controller = go.AddComponent<CharacterController>();
                controller.radius = 0.3f;
                controller.height = 1.6f;
            }
            return go;
        }
    }
}
