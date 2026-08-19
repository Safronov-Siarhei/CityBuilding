using System.Collections;
using CityBuilder.Core;
using CityBuilder.Maps;
using CityBuilder.Saving;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// The map's geometry must stay out of the collision matrix and stay in every raycast -- the
    /// two halves of TerrainPhysicsLayer, which together buy half the cost of every walking agent.
    ///
    /// Worth pinning down because the failure mode is silent in both directions. Put the ground
    /// back in the matrix and nothing breaks; the game just gets slower on a phone, where nobody is
    /// running a profiler. Break the raycast half and the ground stops answering clicks -- which
    /// looks like a UI bug, three systems away from the line that caused it.
    /// </summary>
    public class TerrainPhysicsTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map1";

        [UnitySetUp]
        public IEnumerator PrepareScene()
        {
            LogAssert.ignoreFailingMessages = true;

            Time.timeScale = 1f;
            GameSessionIntent.NewGameMapId = MapId;
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);
            ModalGate.SetBlocked(false);
            yield return null;
        }

        [Test]
        public void TheGroundIsOnTheTerrainLayerAndCollidesWithNothing()
        {
            var applier = MeshMapApplier.Instance;
            Assert.IsNotNull(applier, "No MeshMapApplier -- the scene did not come up.");

            var ground = GameObject.FindFirstObjectByType<MeshCollider>();
            Assert.IsNotNull(ground, "No mesh collider in the scene at all, so the map never applied.");
            Assert.AreEqual(TerrainPhysicsLayer.Layer, ground.gameObject.layer,
                "The map's mesh colliders are not on the terrain layer, so every CharacterController.Move is sweeping against them again.");

            // Layer 0 is where every agent lives (citizens, soldiers, orcs are all created without
            // a layer of their own), so this is the pair that actually costs frames.
            Assert.IsTrue(Physics.GetIgnoreLayerCollision(TerrainPhysicsLayer.Layer, 0),
                "The terrain layer still collides with the default layer.");
        }

        [Test]
        public void TheGroundStillAnswersRaycasts()
        {
            var applier = MeshMapApplier.Instance;
            Assert.IsNotNull(applier);

            var grid = CityBuilder.Grid.GridManager.Instance;
            var target = grid.GetFootprintCenterWorld(new Vector2Int(grid.GridSize.x / 2, grid.GridSize.y / 2), Vector2Int.one);
            var straightDown = new Ray(target + Vector3.up * 50f, Vector3.down);

            // The path BuildingPlacer uses for placement: a scene-wide ray, triggers ignored. The
            // collision matrix must not touch it -- Physics.Raycast selects by layerMask argument,
            // and every mask in this project is ~0.
            Assert.IsTrue(Physics.Raycast(straightDown, out var sceneHit, 500f, ~0, QueryTriggerInteraction.Ignore),
                "A downward ray over the middle of the map hit nothing -- placement raycasts are broken.");
            Assert.AreEqual(TerrainPhysicsLayer.Layer, sceneHit.collider.gameObject.layer,
                "The ray hit something other than the terrain, so this assertion proves nothing about the ground.");

            // And the path clicks use, which asks the ground colliders directly.
            Assert.IsTrue(applier.TryRaycastGround(straightDown, out _),
                "MeshMapApplier.TryRaycastGround no longer finds the ground.");
        }
    }
}
