using System.Collections;
using System.Collections.Generic;
using CityBuilder.Grid;
using CityBuilder.Maps;
using CityBuilder.Resources;
using CityBuilder.Saving;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// Loads the real game scene with the real mesh map and checks what a click ray actually meets
    /// over the forest -- the one thing the EditMode suite structurally cannot cover, and the
    /// reason a whole family of "clicks land next to where I clicked / I can't order a citizen to
    /// chop this tree" bugs kept surviving "108/108 tests green".
    ///
    /// The bug these were written for: Map-1-TreesArea.fbx, the authored zone that tells
    /// TreesAreaSpawner where the forest goes, is not a flat marker plane -- it's an extruded
    /// volume a metre tall sitting directly ON the walkable ground. It was instantiated with mesh
    /// colliders and left in the scene, invisible, so every physics query over the forest met its
    /// top face first: click-to-move read its destination a metre above and (at the camera's 50
    /// degree pitch) most of a cell beside the real ground, and boulders -- 0.6m tall, entirely
    /// underneath it -- could never be clicked at all. MeshMapApplier now reads the zone into grid
    /// cells and disposes of the geometry.
    /// </summary>
    public class ForestClickTargetingTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map1";
        // SetupProject pitches the camera pivot 50 degrees. Any invisible obstacle standing h
        // metres above the ground displaces a click by h/tan(50) ~= 0.84h horizontally, which is
        // what these tests measure.
        private const float CameraPitchDegrees = 50f;
        private const int SamplesPerResourceType = 8;
        // A click is expected to land on the exact spot it was aimed at, not "roughly there":
        // the only thing between the two is floating-point error on a mesh raycast.
        private const float AllowedClickErrorMetres = 0.05f;

        private static bool _sceneLoaded;

        [UnitySetUp]
        public IEnumerator LoadGameSceneOnce()
        {
            // -nographics has no real render target, so the minimap's RenderTexture fails to
            // create and logs an error the test runner would otherwise count as a failure. It
            // says nothing about the geometry these tests are here to check.
            LogAssert.ignoreFailingMessages = true;

            if (_sceneLoaded) yield break;

            // Another test class may have left time running fast; every wait below is in scaled
            // time, so start from a known rate.
            Time.timeScale = 1f;

            // Same handoff the main menu uses to start a new game on a chosen map.
            GameSessionIntent.NewGameMapId = MapId;
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);

            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);

            _sceneLoaded = true;
        }

        [Test]
        public void MapLoaded_WithForestAndBoulders()
        {
            Assert.IsNotNull(MeshMapApplier.Instance, "MeshMapApplier did not survive scene load.");
            Assert.AreEqual(MapId, MeshMapApplier.Instance.CurrentMapId, "The mesh map never applied.");
            Assert.Greater(CountNodes(ResourceType.Wood), 0, "No trees spawned -- the rest of these tests would pass vacuously.");
            Assert.Greater(CountNodes(ResourceType.Stone), 0, "No boulders spawned -- the rest of these tests would pass vacuously.");
        }

        /// <summary>
        /// A click aimed at a point on the ground must resolve to that point. Anything solid
        /// standing invisibly above the forest shifts it along the view ray instead -- the
        /// "citizen walks to a spot next to the one I clicked" report.
        /// </summary>
        [Test]
        public void ClickRayAtGroundPoint_LandsOnThatPoint()
        {
            foreach (var target in SampleNodePositions())
            {
                var ray = ClickRayTo(target);

                Assert.IsTrue(Physics.Raycast(ray, out var hit, 500f, ~0, QueryTriggerInteraction.Ignore),
                    $"A click aimed at {target} hit nothing solid at all. {Diagnose(target, ray)}");
                Assert.Less(HorizontalDistance(hit.point, target), AllowedClickErrorMetres,
                    $"A click aimed at {target} landed on {hit.point} ({hit.collider.name}) -- something is standing " +
                    "invisibly above the ground and intercepting clicks.");
            }
        }

        /// <summary>
        /// The same point asked of the ground mesh by name (what CitizenSelector and
        /// BuildingPlacer actually use now) -- immune to whatever else the ray passes through.
        /// </summary>
        [Test]
        public void GroundRaycast_AgreesWithTheAimedPoint()
        {
            foreach (var target in SampleNodePositions())
            {
                var ray = ClickRayTo(target);

                Assert.IsTrue(MeshMapApplier.Instance.TryRaycastGround(ray, out var hit),
                    $"The ground mesh was not found under a click aimed at {target}. {Diagnose(target, ray)}");
                Assert.Less(HorizontalDistance(hit.point, target), AllowedClickErrorMetres,
                    $"The ground under {target} resolved to {hit.point}.");
            }
        }

        /// <summary>
        /// Nothing solid may stand between the camera and a tree/boulder's click collider: a
        /// citizen is ordered to chop or quarry by clicking the node itself, and a solid collider
        /// in front of it turns that order into a move order (or a flat NO!) -- the "I can't send
        /// anyone to chop a tree or mine a rock any more" report. Boulders are 0.6m tall, so they
        /// sat entirely under the forest zone's slab.
        /// </summary>
        /// <summary>
        /// A tree/boulder's click collider has to sit on the tree/boulder the player can see. It
        /// is derived from the prefab's renderer bounds at spawn time (TreesAreaSpawner.
        /// AddClickCollider), and that derivation has to survive the corrective root rotation the
        /// map's FBX assets carry -- get it wrong and the click box ends up beside, or lying
        /// across, the thing it is supposed to represent.
        /// </summary>
        [Test]
        public void NodeClickCollider_CoversItsVisibleGeometry()
        {
            foreach (var node in SampleNodes())
            {
                var visual = VisualBounds(node);
                var collider = node.GetComponentInChildren<Collider>();

                Assert.IsNotNull(collider, $"{node.name} at {node.transform.position} has no click collider at all.");
                Assert.IsTrue(collider.bounds.Contains(visual.center),
                    $"{node.name}'s click collider {Describe(collider.bounds)} does not even contain the centre of the " +
                    $"geometry it represents {Describe(visual)} -- clicks on it land on whatever is behind it.");

                // And it has to cover the whole of it, not just the middle: a box that misses the
                // trunk swallows clicks on bare ground beside the tree while ignoring clicks on
                // the tree itself. Half a cell of slack, since this is a click target, not physics.
                const float slack = 0.5f;
                Assert.IsTrue(collider.bounds.min.x <= visual.min.x + slack && collider.bounds.min.y <= visual.min.y + slack && collider.bounds.min.z <= visual.min.z + slack &&
                              collider.bounds.max.x >= visual.max.x - slack && collider.bounds.max.y >= visual.max.y - slack && collider.bounds.max.z >= visual.max.z - slack,
                    $"{node.name}'s click collider {Describe(collider.bounds)} does not cover the geometry it " +
                    $"represents {Describe(visual)}.");
            }
        }

        [Test]
        public void NodeClickCollider_IsNotShadowedBySomethingSolid()
        {
            foreach (var node in SampleNodes())
            {
                // Aimed at the middle of what the player sees, which is what they click.
                var ray = ClickRayTo(VisualBounds(node).center);
                var hits = Physics.RaycastAll(ray, 500f, ~0, QueryTriggerInteraction.Collide);

                var nodeDistance = float.MaxValue;
                foreach (var hit in hits)
                {
                    if (hit.collider.GetComponentInParent<ResourceNode>() != node) continue;
                    nodeDistance = Mathf.Min(nodeDistance, hit.distance);
                }
                Assert.Less(nodeDistance, float.MaxValue,
                    $"A click aimed at the middle of {node.name} {Describe(VisualBounds(node))} never reached its own " +
                    $"click collider {Describe(node.GetComponentInChildren<Collider>().bounds)}.");

                foreach (var hit in hits)
                {
                    if (hit.collider.isTrigger || hit.distance >= nodeDistance) continue;
                    Assert.Fail($"'{hit.collider.name}' is solid and sits in front of {node.name} " +
                                $"({hit.distance:F2}m vs {nodeDistance:F2}m) -- it swallows the gather click.");
                }
            }
        }

        /// <summary>
        /// The straight-down ground probe CitizenAgent.TryFindWalkablePoint uses to vet a wander
        /// target. A slab lying on the forest floor answers it with its own top face, every forest
        /// point reads as "wrong height", and citizens in the woods stop finding anywhere to go.
        /// </summary>
        [Test]
        public void DownwardGroundProbe_OverForest_HitsTheWalkableHeight()
        {
            var grid = GridManager.Instance;
            Assert.IsNotNull(grid, "No GridManager in the loaded scene.");

            foreach (var target in SampleNodePositions())
            {
                var origin = new Vector3(target.x, 50f, target.z);
                Assert.IsTrue(Physics.Raycast(origin, Vector3.down, out var hit, 100f, ~0, QueryTriggerInteraction.Ignore),
                    $"Nothing under {target} to stand on.");
                Assert.LessOrEqual(Mathf.Abs(hit.point.y - grid.GroundHeight), 0.5f,
                    $"The probe over {target} landed on '{hit.collider.name}' at y={hit.point.y:F2} instead of the " +
                    $"walkable height {grid.GroundHeight:F2}.");
            }
        }

        /// <summary>Everything worth knowing when a ray that should have hit the map hits nothing -- printed into the failure so a rerun isn't needed to find out what the scene actually contained.</summary>
        private static string Diagnose(Vector3 target, Ray ray)
        {
            var down = new Ray(new Vector3(target.x, 50f, target.z), Vector3.down);
            var verticalHit = Physics.Raycast(down, out var below, 100f, ~0, QueryTriggerInteraction.Ignore)
                ? $"'{below.collider.name}' at y={below.point.y:F2}"
                : "nothing";

            var allHits = Physics.RaycastAll(ray, 500f, ~0, QueryTriggerInteraction.Collide);
            var names = allHits.Length == 0 ? "none" : string.Empty;
            foreach (var hit in allHits)
            {
                names += $"{hit.collider.name}@{hit.distance:F1}m ";
            }

            var mapApplier = MeshMapApplier.Instance;
            var groundObject = GameObject.Find("Map-1-Ground(Clone)");
            var groundColliders = groundObject != null ? groundObject.GetComponentsInChildren<Collider>().Length : -1;

            return $"[ray from {ray.origin} dir {ray.direction}; straight down finds {verticalHit}; " +
                   $"all hits along the ray: {names}; MeshMapApplier={(mapApplier == null ? "null" : mapApplier.CurrentMapId)}; " +
                   $"ground colliders in scene={groundColliders}; timeScale={Time.timeScale}; nodes={ResourceNode.All.Count}]";
        }

        private static Ray ClickRayTo(Vector3 groundPoint)
        {
            var direction = Quaternion.Euler(CameraPitchDegrees, 0f, 0f) * Vector3.forward;
            return new Ray(groundPoint - direction * 30f, direction);
        }

        /// <summary>The world-space extent of what this node actually draws -- the shape a player aims at.</summary>
        private static Bounds VisualBounds(ResourceNode node)
        {
            var renderers = node.GetComponentsInChildren<Renderer>();
            Assert.Greater(renderers.Length, 0, $"{node.name} draws nothing at all.");

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
        }

        private static string Describe(Bounds bounds)
        {
            return $"[centre {bounds.center}, size {bounds.size}]";
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private static int CountNodes(ResourceType type)
        {
            var count = 0;
            foreach (var node in ResourceNode.All)
            {
                if (node.ResourceType == type) count++;
            }
            return count;
        }

        /// <summary>A spread of real trees and boulders to aim clicks at -- both types, since a boulder is short enough to hide under an obstacle a tree pokes through.</summary>
        private static List<ResourceNode> SampleNodes()
        {
            var wood = new List<ResourceNode>();
            var stone = new List<ResourceNode>();

            foreach (var node in ResourceNode.All)
            {
                var bucket = node.ResourceType == ResourceType.Wood ? wood : node.ResourceType == ResourceType.Stone ? stone : null;
                if (bucket == null || bucket.Count >= SamplesPerResourceType) continue;
                bucket.Add(node);
            }

            wood.AddRange(stone);
            return wood;
        }

        private static IEnumerable<Vector3> SampleNodePositions()
        {
            foreach (var node in SampleNodes())
            {
                yield return node.transform.position;
            }
        }
    }
}
