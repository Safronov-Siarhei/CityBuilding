using CityBuilder.Core;
using NUnit.Framework;
using UnityEngine;

namespace CityBuilder.Tests.EditMode
{
    public class GameSettingsTests
    {
        private const string FogStyleKey = "settings.fog_style.v2";

        private bool _hadValue;
        private int _savedValue;

        [SetUp]
        public void RememberThePlayersChoice()
        {
            // These run in the editor, against the same PlayerPrefs the developer plays with.
            _hadValue = PlayerPrefs.HasKey(FogStyleKey);
            _savedValue = PlayerPrefs.GetInt(FogStyleKey, 0);
            GameSettings.ReloadForTests();
        }

        [TearDown]
        public void PutItBack()
        {
            if (_hadValue) PlayerPrefs.SetInt(FogStyleKey, _savedValue);
            else PlayerPrefs.DeleteKey(FogStyleKey);
            PlayerPrefs.Save();
            GameSettings.ReloadForTests();
        }

        [Test]
        public void TheFogStyle_IsParchmentUntilChosen()
        {
            PlayerPrefs.DeleteKey(FogStyleKey);
            GameSettings.ReloadForTests();

            Assert.AreEqual(FogStyle.Parchment, GameSettings.FogStyle);
        }

        [Test]
        public void TheFogStyle_IsRememberedAcrossSessions()
        {
            // Clouds, not the default: remembering the default proves nothing.
            GameSettings.FogStyle = FogStyle.Clouds;
            GameSettings.ReloadForTests();   // as a fresh launch would read it

            Assert.AreEqual(FogStyle.Clouds, GameSettings.FogStyle);
        }

        [Test]
        public void ChangingTheFogStyle_IsAnnounced_ButSettingItAgainIsNot()
        {
            GameSettings.FogStyle = FogStyle.Clouds;
            var calls = 0;
            void Count() => calls++;
            GameSettings.OnChanged += Count;
            try
            {
                GameSettings.FogStyle = FogStyle.Parchment;
                GameSettings.FogStyle = FogStyle.Parchment;
            }
            finally
            {
                GameSettings.OnChanged -= Count;
            }

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void AnUnknownSavedValue_FallsBackToTheDefault()
        {
            PlayerPrefs.SetInt(FogStyleKey, 99);
            GameSettings.ReloadForTests();

            Assert.AreEqual(FogStyle.Parchment, GameSettings.FogStyle);
        }
    }
}
