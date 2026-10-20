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
    /// The camera over the Caldera's mountains (the user, 2026-10-20: it flew straight through
    /// them). Put where the old flat rig would have buried the camera in the rock, it now stands
    /// above the land, and nothing of the mountain lies between it and the point it looks at.
    /// </summary>
    public class CameraTerrainTests
    {
        [UnitySetUp]
        public IEnumerator OnTheCaldera()
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
        }

        [UnityTest]
        public IEnumerator OverTheMountains_TheCameraRidesAboveTheRock()
        {
            var rig = Object.FindAnyObjectByType<RTSCameraController>();
            var camera = rig.GetComponentInChildren<Camera>();
            var offset = camera.transform.position - rig.transform.position;
            var mask = 1 << TerrainPhysicsLayer.Layer;

            // somewhere the flat rig would put the camera inside a mountain
            var found = false;
            var spot = Vector3.zero;
            for (var x = -90f; x <= 90f && !found; x += 3f)
            {
                for (var z = -90f; z <= 90f && !found; z += 3f)
                {
                    var cam = new Vector3(x, 0f, z) + offset;
                    if (!Physics.Raycast(new Vector3(cam.x, 300f, cam.z), Vector3.down, out var hit, 600f, mask)) continue;
                    if (hit.point.y > cam.y + 1.5f)
                    {
                        found = true;
                        spot = new Vector3(x, 0f, z);
                    }
                }
            }
            Assert.IsTrue(found, "No mountain on the Caldera tall enough to bury the camera -- the fixture needs another map.");

            rig.JumpTo(spot);
            yield return new WaitForSeconds(1f);

            var at = camera.transform.position;
            Assert.IsTrue(Physics.Raycast(new Vector3(at.x, 300f, at.z), Vector3.down, out var under, 600f, mask));
            Assert.Greater(at.y, under.point.y + 1f, "The camera is in the rock, or skimming it.");
            var look = rig.transform.position + Vector3.up * 0.05f;
            Assert.IsFalse(Physics.Linecast(at, look, mask), "A mountain stands between the camera and what it looks at.");

            yield return PlaytestCapture.ShootWithUiAt("camera-over-mountain", 2400, 1080, rig.transform.position, offset.magnitude, camera.transform.eulerAngles.x, camera.transform.eulerAngles.y);
        }
    }
}
