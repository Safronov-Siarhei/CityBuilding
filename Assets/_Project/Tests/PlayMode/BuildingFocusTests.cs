using System.Collections;
using CityBuilder.Buildings;
using CityBuilder.CameraControl;
using CityBuilder.Saving;
using CityBuilder.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>A building held with a tap (BuildingFocus) is let go once the camera is taken away and it is off the screen (the user, 2026-10-28).</summary>
    public class BuildingFocusTests
    {
        [UnitySetUp]
        public IEnumerator NewGame()
        {
            LogAssert.ignoreFailingMessages = true;
            Time.timeScale = 1f;
            GameSessionIntent.SaveNameToLoad = null;
            GameSessionIntent.NewGameMapId = "Map3";
            SceneManager.LoadScene("CityBuilder", LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady("Map3");
        }

        [UnityTest]
        public IEnumerator AHeldBuilding_IsLetGo_OnceTheCameraLeavesIt()
        {
            var origin = PlaytestWorld.FindFreeAreaNearTheMiddle(new Vector2Int(6, 6));
            // a new game opens on placing the Town Hall, which would let any building go at once
            var placer = Object.FindAnyObjectByType<BuildingPlacer>();
            placer.MarkMandatoryBuildingAlreadyPlaced();
            placer.ClearSelection();
            var barn = PlaytestWorld.Place(PlaytestWorld.Building("Barn"), origin);
            var focus = BuildingFocus.Instance;
            focus.Select(barn);
            var rig = Object.FindAnyObjectByType<RTSCameraController>();
            for (var t = 0f; t < 5f && rig.IsFocusing; t += Time.unscaledDeltaTime) yield return null;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual(barn, focus.Current, "The building was let go while it was on the screen.");

            // a little way off, still in view: held
            rig.JumpTo(barn.transform.position + new Vector3(3f, 0f, 0f));
            yield return null;
            yield return null;
            Assert.AreEqual(barn, focus.Current, "The building was let go with the camera barely moved.");

            // far off: let go
            rig.JumpTo(barn.transform.position + new Vector3(60f, 0f, 60f));
            yield return null;
            yield return null;
            Assert.IsNull(focus.Current, "The building is still held with the camera far away from it.");
        }
    }
}
