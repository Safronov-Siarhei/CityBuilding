using System.Collections;
using CityBuilder.CameraControl;
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
    /// The first moment of a new game (the user, 2026-10-20): the view opens on the middle of the
    /// start zone, so the Town Hall in hand starts somewhere it may stand; the hint "choose a spot
    /// for the Town Hall" sits at the bottom in the middle, with the rotate button just above it.
    /// </summary>
    public class StartPlacementTests
    {
        [UnitySetUp]
        public IEnumerator NewGameOnTheCaldera()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            GameSessionIntent.SaveNameToLoad = null;
            GameSessionIntent.NewGameMapId = "Map5";
            SceneManager.LoadScene("CityBuilder", LoadSceneMode.Single);
            yield return null;
            yield return null;
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady("Map5");
            ModalGate.SetBlocked(false);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheViewOpensOnTheStartZone_TheHintAtTheBottom_RotateAboveIt()
        {
            var zone = StartZone.Centre(MeshMapApplier.Instance.CurrentMap.TownHallZone);
            var rig = Object.FindAnyObjectByType<RTSCameraController>();
            var at = rig.transform.position;
            Assert.Less(Vector2.Distance(new Vector2(at.x, at.z), new Vector2(zone.x, zone.z)), 0.5f, "The view does not open on the start zone.");

            var root = GameObject.Find("SafeArea").transform;
            var hint = (RectTransform)root.Find("PlacementHint");
            var controls = (RectTransform)root.Find("PlacementControls");
            Assert.IsTrue(hint.gameObject.activeInHierarchy, "No hint while the Town Hall is in hand.");
            Assert.AreEqual(0f, hint.anchorMin.y, "The hint is not on the bottom edge.");
            Assert.AreEqual(0.5f, hint.anchorMin.x, "The hint is not in the middle.");
            var hintTop = hint.anchoredPosition.y + hint.rect.height * (1f - hint.pivot.y);
            var controlsBottom = controls.anchoredPosition.y - controls.rect.height * controls.pivot.y;
            Assert.GreaterOrEqual(controlsBottom, hintTop - 1f, "The rotate button is not above the hint.");

            yield return PlaytestCapture.ShootWithUiAt("start-placement", 2400, 1080, zone, 34f, 52f, 0f);
        }
    }
}
