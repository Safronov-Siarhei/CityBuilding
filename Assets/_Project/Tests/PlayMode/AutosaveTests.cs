using System.Collections;
using System.Diagnostics;
using System.IO;
using CityBuilder.Buildings;
using CityBuilder.Citizens;
using CityBuilder.Grid;
using CityBuilder.Saving;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace CityBuilder.Tests.PlayMode
{
    /// <summary>
    /// The autosave (GameSaveController.Autosave): one slot of its own that overwrites itself on a
    /// timer and when the app goes to the background, and never becomes the save the dialog offers
    /// to write back to. These run against the developer's own save folder, so whatever autosave
    /// was there is put back afterwards.
    /// </summary>
    public class AutosaveTests
    {
        private static bool _sceneLoaded;
        private static string AutosavePath => Path.Combine(Application.persistentDataPath, "Saves", GameSaveController.AutosaveName + ".json");

        private byte[] _developersAutosave;
        private GameSaveController _saves;

        [UnitySetUp]
        public IEnumerator BuildATownOnce()
        {
            LogAssert.ignoreFailingMessages = true;
            _developersAutosave = File.Exists(AutosavePath) ? File.ReadAllBytes(AutosavePath) : null;

            if (!_sceneLoaded)
            {
                Time.timeScale = 1f;
                GameSessionIntent.NewGameMapId = "Map3";
                SceneManager.LoadScene("CityBuilder", LoadSceneMode.Single);
                yield return PlayModeScene.WaitUntilMapIsPhysicsReady("Map3");

                var origin = PlaytestWorld.FindFreeAreaNearTheMiddle(new Vector2Int(16, 12));
                PlaytestWorld.Place(PlaytestWorld.Building(BuildingIds.TownHall), origin + new Vector2Int(6, 6));
                Object.FindAnyObjectByType<BuildingPlacer>().MarkMandatoryBuildingAlreadyPlaced();
                string[] town = { "Hovel", "Cottage", "Barn", "Sawmill", "Well", "Farm", "Granary", "Tavern" };
                for (var i = 0; i < town.Length; i++)
                {
                    var data = PlaytestWorld.Building(town[i]);
                    var cell = origin + new Vector2Int((i % 4) * 4, i < 4 ? 0 : 11);
                    if (data != null && GridManager.Instance.IsAreaFree(cell, data.footprintSize)) PlaytestWorld.Place(data, cell);
                }
                CitizenManager.Instance.SetPopulation(20);
                for (var i = 0; i < 5; i++) yield return null;
                _sceneLoaded = true;
            }

            _saves = Object.FindAnyObjectByType<GameSaveController>();
            Assert.IsNotNull(_saves, "No GameSaveController in the game scene.");
            if (File.Exists(AutosavePath)) File.Delete(AutosavePath);
        }

        [TearDown]
        public void PutTheDevelopersAutosaveBack()
        {
            if (_saves != null) _saves.AutosaveIntervalSeconds = 240f;
            if (_developersAutosave != null) File.WriteAllBytes(AutosavePath, _developersAutosave);
            else if (File.Exists(AutosavePath)) File.Delete(AutosavePath);
        }

        [OneTimeTearDown]
        public void ForgetTheScene() => _sceneLoaded = false;

        [Test]
        public void TheIntervalIsFourMinutes()
        {
            Assert.AreEqual(240f, _saves.AutosaveIntervalSeconds, "The user asked for an autosave every 4 minutes.");
        }

        [UnityTest]
        public IEnumerator TheAutosave_WritesItsOwnSlotOnTheInterval_AndLeavesTheCurrentSaveNameAlone()
        {
            var currentName = _saves.CurrentSaveName;
            _saves.AutosaveIntervalSeconds = 1f;
            _saves.Autosave();                 // starts the interval over
            File.Delete(AutosavePath);

            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsFalse(File.Exists(AutosavePath), "The autosave fired long before its interval.");

            yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsTrue(File.Exists(AutosavePath), "No autosave was written when the interval ran out.");

            var data = SaveSystem.Load(GameSaveController.AutosaveName);
            Assert.IsNotNull(data, "The autosave cannot be read back.");
            Assert.IsTrue(data.mandatoryBuildingPlaced, "The autosave does not know the Town Hall stands.");
            Assert.Greater(data.buildings.Count, 3, "The autosave holds next to none of the town.");
            Assert.AreEqual(currentName, _saves.CurrentSaveName, "Autosaving changed the save the dialog offers to write back to.");
        }

        [UnityTest]
        public IEnumerator GoingToTheBackground_Autosaves()
        {
            _saves.SendMessage("OnApplicationPause", true);
            yield return null;
            Assert.IsTrue(File.Exists(AutosavePath), "Putting the app in the background did not autosave -- on a phone that is when the game gets killed.");
        }

        [Test]
        public void OneAutosave_IsQuickEnoughNotToHitch()
        {
            _saves.Autosave();   // warm-up: first-use JIT and file creation are not the steady cost
            var clock = Stopwatch.StartNew();
            const int runs = 5;
            for (var i = 0; i < runs; i++) _saves.Autosave();
            clock.Stop();
            var ms = clock.Elapsed.TotalMilliseconds / runs;
            Debug.Log($"[Autosave] one autosave of a {new FileInfo(AutosavePath).Length / 1024} KB town takes {ms:F1} ms in the editor.");
            Assert.Less(ms, 50.0, "An autosave stalls the game for a visible moment -- spread it over frames or move the write off the main thread.");
        }

        [Test]
        public void NoSaveThePlayerNames_CanBeTheAutosave()
        {
            foreach (var typed in new[] { "_autosave", "__autosave", " _autosave ", "-autosave", "autosave" })
            {
                Assert.AreNotEqual(GameSaveController.AutosaveName, SaveSystem.SanitizeName(typed),
                    $"Typing '{typed}' in the save dialog would overwrite the autosave slot.");
            }
        }
    }
}
