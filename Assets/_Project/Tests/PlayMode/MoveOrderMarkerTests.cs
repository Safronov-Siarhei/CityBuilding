using System.Collections;
using System.Reflection;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Core;
using CityBuilder.Grid;
using CityBuilder.Maps;
using CityBuilder.Saving;
using CityBuilder.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// The way a citizen's order is drawn on the ground (MoveOrderMarker), on the map new games
    /// start on: the flag stands where the citizen is going and ON the land there, the trail lies
    /// on the land along the route and is eaten as the citizen walks it, everything comes down when
    /// the citizen arrives -- and a photograph, because a look is only verified by being seen.
    /// </summary>
    public class MoveOrderMarkerTests
    {
        private const string GameSceneName = "CityBuilder";
        private const string MapId = "Map3";
        private const float GroundTolerance = 0.08f;

        private static bool _sceneLoaded;
        private static Vector3 _townHall;

        private MoveOrderMarker _marker;

        [UnitySetUp]
        public IEnumerator PrepareScene()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            if (!_sceneLoaded)
            {
                GameSessionIntent.NewGameMapId = MapId;
                SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
                yield return PlayModeScene.WaitUntilMapIsPhysicsReady(MapId);

                var hall = PlaytestWorld.Building(BuildingIds.TownHall);
                var origin = PlaytestWorld.FindFreeAreaNearTheMiddle(hall.footprintSize + new Vector2Int(6, 6));
                _townHall = PlaytestWorld.Place(hall, origin + new Vector2Int(3, 3)).transform.position;
                Object.FindAnyObjectByType<BuildingPlacer>().MarkMandatoryBuildingAlreadyPlaced();
                CitizenManager.Instance.SetPopulation(6);
                _sceneLoaded = true;
            }

            for (var frame = 0; frame < 300 && IdleCitizen() == null; frame++) yield return null;
            Assert.IsNotNull(IdleCitizen(), "No idle citizen appeared beside the Town Hall to give an order to.");

            _marker = new GameObject("TestMoveOrderMarker").AddComponent<MoveOrderMarker>();
            _marker.Configure(ScenesFlag());
        }

        [TearDown]
        public void CleanUp()
        {
            Time.timeScale = 1f;
            if (_marker != null) Object.Destroy(_marker.gameObject);
        }

        [OneTimeTearDown]
        public void ForgetTheScene() => _sceneLoaded = false;

        [Test]
        public void TheScene_GivesTheSelectorTheFlagModel()
        {
            Assert.IsNotNull(ScenesFlag(), "CitizenSelector has no order flag: SetupProject did not find Models/Buildings/OrderFlag.fbx, and orders plant nothing.");
        }

        [UnityTest]
        public IEnumerator AMoveOrder_PlantsTheFlagOnTheLand_AndLaysTheTrailAlongTheRoute()
        {
            var citizen = IdleCitizen();
            var destination = WalkableSpotAway(citizen.transform.position, 7f);

            citizen.MoveTo(destination);
            _marker.ShowOrder(citizen, destination, true);
            yield return new WaitForSecondsRealtime(0.5f);   // the flag has finished dropping in

            var flag = Piece("OrderFlag");
            Assert.IsNotNull(flag, "The order planted no flag.");
            Assert.IsTrue(flag.gameObject.activeSelf, "The flag is not showing while the citizen walks to it.");
            Assert.Less(Horizontal(flag.position, destination), 0.01f, "The flag stands somewhere other than where the citizen is going.");
            Assert.AreEqual(GroundAt(destination), flag.position.y, GroundTolerance, "The flag is sunk into the land or floating over it.");

            var dots = ActiveDots();
            Assert.Greater(dots.Length, 5, "A 7 m walk drew next to no trail.");
            foreach (var dot in dots)
            {
                Assert.AreEqual(GroundAt(dot.position), dot.position.y, GroundTolerance, $"A trail dot at {dot.position} is not lying on the land.");
            }
            // The trail runs FROM the citizen TO the flag, not off somewhere of its own.
            var nearest = float.MaxValue;
            foreach (var dot in dots) nearest = Mathf.Min(nearest, Horizontal(dot.position, destination));
            Assert.Less(nearest, 1f, "The trail does not reach the flag.");
        }

        [UnityTest]
        public IEnumerator TheTrail_IsEatenAsTheCitizenWalks_AndEverythingComesDownOnArrival()
        {
            var citizen = IdleCitizen();
            var destination = WalkableSpotAway(citizen.transform.position, 7f);

            citizen.MoveTo(destination);
            _marker.ShowOrder(citizen, destination, true);
            yield return null;
            var atStart = ActiveDots().Length;

            Time.timeScale = 3f;
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.IsTrue(citizen.IsExecutingOrder, "The citizen got there too fast to watch the trail shrink -- the test needs a longer walk.");
            Assert.Less(ActiveDots().Length, atStart, "The citizen has walked part of the way and the trail behind it is still all there.");

            for (var i = 0; i < 600 && citizen.IsExecutingOrder; i++) yield return null;
            Assert.IsFalse(citizen.IsExecutingOrder, "The citizen never arrived.");
            yield return new WaitForSecondsRealtime(0.4f);   // the flag's pop-out

            Assert.IsFalse(Piece("OrderFlag").gameObject.activeSelf, "The flag is still standing after the citizen arrived.");
            Assert.AreEqual(0, ActiveDots().Length, "Trail dots are left lying about after the citizen arrived.");
        }

        /// <summary>
        /// The order taken down halfway -- the player put the citizen down, or picked someone
        /// else. Arrival alone cannot prove the clean-up: by then the citizen has eaten the whole
        /// trail itself, so a clean-up that did nothing still left nothing behind.
        /// </summary>
        [UnityTest]
        public IEnumerator PuttingTheCitizenDownMidWalk_TakesTheOrderOffTheGround()
        {
            var citizen = IdleCitizen();
            var destination = WalkableSpotAway(citizen.transform.position, 7f);

            citizen.MoveTo(destination);
            _marker.ShowOrder(citizen, destination, true);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.Greater(ActiveDots().Length, 3, "No trail to take down -- the test proves nothing.");

            _marker.HideOrder();
            yield return new WaitForSecondsRealtime(0.4f);

            Assert.AreEqual(0, ActiveDots().Length, "The trail stayed on the ground after the order was taken down.");
            Assert.IsFalse(Piece("OrderFlag").gameObject.activeSelf, "The flag stayed up after the order was taken down.");
            Assert.IsFalse(Piece("OrderTargetRing").gameObject.activeSelf, "The flag's ring stayed on the ground after the order was taken down.");
        }

        [UnityTest]
        public IEnumerator ARefusedTap_RipplesRed_AndPlantsNothing()
        {
            _marker.ShowRefused(_townHall + new Vector3(4f, 0f, 4f));
            yield return new WaitForSecondsRealtime(0.1f);

            var flag = Piece("OrderFlag");
            Assert.IsTrue(flag == null || !flag.gameObject.activeSelf, "A refused tap planted a flag.");
            Assert.AreEqual(0, ActiveDots().Length, "A refused tap drew a trail.");
            var rippling = false;
            foreach (Transform child in _marker.transform)
            {
                if (child.name != "OrderRipple" || !child.gameObject.activeSelf) continue;
                rippling = true;
                var color = child.GetComponent<Renderer>().sharedMaterial.color;
                Assert.Greater(color.r, color.g + 0.3f, "The refusal's ripple is not red.");
            }
            Assert.IsTrue(rippling, "A refused tap showed no ripple at all.");

            yield return new WaitForSecondsRealtime(1.3f);
            foreach (Transform child in _marker.transform)
            {
                Assert.IsFalse(child.name == "OrderRipple" && child.gameObject.activeSelf, "The refusal's ripple never faded.");
            }
        }

        [UnityTest]
        public IEnumerator Photograph_TheMoveOrder()
        {
            var citizen = IdleCitizen();
            var destination = WalkableSpotAway(citizen.transform.position, 6f);
            citizen.MoveTo(destination);
            _marker.ShowOrder(citizen, destination, true);
            yield return new WaitForSecondsRealtime(0.15f);
            var middle = Vector3.Lerp(citizen.transform.position, destination, 0.5f);
            yield return PlaytestCapture.Shoot("move-order-ripple", middle, 9f, 50f, 0f);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return PlaytestCapture.Shoot("move-order-walking", middle, 9f, 50f, 0f);
            yield return PlaytestCapture.Shoot("move-order-game-distance", middle, 26f, 55f, 0f, 60f);
        }

        // --- helpers ----------------------------------------------------------------------------

        private static CitizenAgent IdleCitizen()
        {
            foreach (var agent in Object.FindObjectsByType<CitizenAgent>())
            {
                if (agent.isActiveAndEnabled && agent.IsIdle) return agent;
            }
            return null;
        }

        private static GameObject ScenesFlag()
        {
            var selector = Object.FindAnyObjectByType<CitizenSelector>(FindObjectsInactive.Include);
            if (selector == null) return null;
            var field = typeof(CitizenSelector).GetField("orderFlagPrefab", BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(selector) as GameObject;
        }

        /// <summary>A spot on the NavMesh about `distance` away, tried round the compass so a lake or a building on one side does not sink the test.</summary>
        private static Vector3 WalkableSpotAway(Vector3 from, float distance)
        {
            for (var step = 0; step < 16; step++)
            {
                var angle = step * Mathf.PI * 2f / 16f;
                var candidate = from + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
                if (!NavMesh.SamplePosition(candidate, out var hit, 0.5f, NavMesh.AllAreas)) continue;
                var path = new NavMeshPath();
                if (NavMesh.CalculatePath(from, hit.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete) return hit.position;
            }
            Assert.Fail($"Nowhere walkable {distance} m from the citizen at {from}.");
            return from;
        }

        private Transform Piece(string pieceName)
        {
            foreach (Transform child in _marker.transform)
            {
                if (child.name == pieceName) return child;
            }
            return null;
        }

        private Transform[] ActiveDots()
        {
            var list = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in _marker.transform)
            {
                if (child.name == "OrderTrailDot" && child.gameObject.activeSelf) list.Add(child);
            }
            return list.ToArray();
        }

        private static float GroundAt(Vector3 point)
        {
            Assert.IsTrue(MeshMapApplier.Instance.TryGetGroundHeight(point, out var height), $"No land under {point}.");
            return height;
        }

        private static float Horizontal(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
