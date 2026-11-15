using System.Collections;
using CityBuilder.Campaign;
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
    /// Mission 2's opening scene «У ворот» (CityDefenceMode.Intro.cs) PLAYS and hands over: the user's first look
    /// (2026-11-15) found it standing still on its first shot. Its clock must run, its figures stand, and at its end
    /// the siege goes on with Ардан in the player's hands. (Found: losing the window's focus opened the game menu, which
    /// paused the game under the scene's hidden HUD.)
    /// </summary>
    public class CityDefenceIntroTests
    {
        [UnitySetUp]
        public IEnumerator StartTheMission()
        {
            Time.timeScale = 1f;
            PlayerPrefs.DeleteKey(CityDefenceMode.Part2Key);
            GameSessionIntent.SaveNameToLoad = null;
            GameSessionIntent.NewGameMapId = CityDefenceMode.MapId;
            GameSessionIntent.NewGameSeason = MapSeason.Summer;
            GameSessionIntent.NewGameIsInvasion = false;
            CityDefenceMode.Pending = true;
            SceneManager.LoadScene(CityDefenceMode.GameplayScene, LoadSceneMode.Single);
            yield return PlayModeScene.WaitUntilMapIsPhysicsReady(CityDefenceMode.MapId);
            for (var frame = 0; frame < 600 && (CityDefenceMode.Instance == null || CityDefenceMode.Instance.Current == CityDefenceMode.Stage.Setup); frame++)
                yield return null;
        }

        [UnityTearDown]
        public IEnumerator Reset()
        {
            Time.timeScale = 1f;
            CityDefenceMode.Pending = false;
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheOpeningScene_Runs_AndHandsOver()
        {
            var mode = CityDefenceMode.Instance;
            Assert.IsNotNull(mode, "No CityDefenceMode.");
            Debug.Log("[M2Intro test] " + mode.IntroReport);
            Assert.GreaterOrEqual(mode.IntroTime, 0f, "The opening scene did not start.");
            var t0 = mode.IntroTime;
            var left = mode.TimeLeft;
            for (var frame = 0; frame < 30; frame++) yield return null;
            Assert.Greater(mode.IntroTime, t0, $"The scene's clock stands still ({t0}). {mode.IntroReport}");
            Assert.AreEqual(left, mode.TimeLeft, "The siege's clock ran during the scene.");
            Time.timeScale = 8f;
            var waited = 0f;
            while (mode.IntroTime >= 0f && waited < 15f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            Time.timeScale = 1f;
            yield return null;                     // Destroy lands at the frame's end
            Assert.Less(mode.IntroTime, 0f, "The scene never ended.");
            Assert.AreEqual(CityDefenceMode.Stage.Part1, mode.Current, "The siege is not on after the scene.");
            Assert.IsNotNull(mode.Arden, "No Ардан after the scene.");
            Assert.IsNull(GameObject.Find("M2Intro"), "The scene's figures were left behind.");

            // scene 2 «Гонец у короля» (CityDefenceMode.Capital.cs): at the courier's arrival the game stands under it,
            // and its end gives the siege back where it was
            mode.ClockToTheTurn();
            for (var frame = 0; frame < 10 && mode.CapitalTime < 0f; frame++) yield return null;
            Assert.GreaterOrEqual(mode.CapitalTime, 0f, "The capital scene did not start at the turn of the clock.");
            var c0 = mode.CapitalTime;
            var left2 = mode.TimeLeft;
            for (var frame = 0; frame < 20; frame++) yield return null;
            Assert.Greater(mode.CapitalTime, c0, "The capital scene's clock stands still.");
            Assert.AreEqual(left2, mode.TimeLeft, "The siege's clock ran during the capital scene.");
            Assert.AreEqual(0f, Time.timeScale, "The game did not stand under the capital scene.");
            Assert.IsNotNull(GameObject.Find("M2CapitalSet"), "The capital's set is not there.");
            mode.SkipCapital();
            yield return null;
            Assert.Less(mode.CapitalTime, 0f, "The capital scene never ended.");
            Assert.AreEqual(1f, Time.timeScale, "The game's time was not given back.");
            Assert.AreEqual(CityDefenceMode.Stage.Part1, mode.Current, "The siege is not on after the capital scene.");
            Assert.IsNull(GameObject.Find("M2Capital"), "The capital scene was left behind.");
        }
    }
}
