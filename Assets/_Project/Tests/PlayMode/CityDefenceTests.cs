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
    /// Campaign mission 2 «Оборона города» (CityDefenceMode): the mission starts on its own map with the
    /// town standing, Ардан on the square, the guards at the gates and the first part's clock running.
    /// Written for the user's first playtest (2026-11-12), where the mission never started (the sandbox's
    /// HUD stayed, no Ардан): any exception on the way in fails it.
    /// </summary>
    public class CityDefenceTests
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
            CityDefenceMode.Pending = false;
            PlayerPrefs.DeleteKey(CityDefenceMode.Part2Key);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheMission_Starts_WithArdenOnTheSquare_AndTheGatesManned()
        {
            var mode = CityDefenceMode.Instance;
            Assert.IsNotNull(mode, "No CityDefenceMode in the gameplay scene.");
            Assert.IsTrue(CityDefenceMode.IsActive, "The mission did not take the scene.");
            Assert.AreEqual(CityDefenceMode.Stage.Part1, mode.Current, "Part 1 never began.");
            Assert.IsNotNull(mode.Arden, "No Ардан.");
            Assert.IsNotNull(mode.Castle, "The castle was not raised.");
            for (var i = 0; i < 3; i++) Assert.IsNotNull(mode.OuterGates[i], $"Outer gate {i} missing.");
            Assert.Greater(mode.Guards, 0, "No guards at the gates.");
                        Assert.Greater(mode.TimeLeft, 400f, "A part is 8 minutes (the user's first playtest).");
            Assert.AreEqual(1, mode.ArdenLevel, "Ардан starts at level 1.");
            Assert.IsTrue(mode.SkillOpen(CityDefenceMode.Cry) && !mode.SkillOpen(CityDefenceMode.Whirl), "Level 1 opens the cry only.");
            yield return new WaitForSeconds(2f);
            Assert.IsNotNull(mode.Arden, "Ардан vanished.");
        }

        /// <summary>Ардан falls and RISES AGAIN (the user's third playtest, 2026-11-12; he was mortal before): the
        /// mission goes on, the respawn clock runs.</summary>
        [UnityTest]
        public IEnumerator ArdenDying_RisesAgain_TheMissionGoesOn()
        {
            var mode = CityDefenceMode.Instance;
            Assert.IsNotNull(mode.Arden, "No Ардан.");
            mode.Arden.TakeDamage(mode.Arden.MaxHealth * 10);
            for (var frame = 0; frame < 30; frame++) yield return null;
            Assert.AreEqual(CityDefenceMode.Stage.Part1, mode.Current, "Ардан died and the mission ended.");
            Assert.Greater(mode.RespawnLeft, 0f, "No respawn clock after his fall.");
        }
    }
}
