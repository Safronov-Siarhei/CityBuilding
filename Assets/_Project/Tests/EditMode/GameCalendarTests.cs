using CityBuilder.Core;
using NUnit.Framework;
using UnityEngine;

namespace CityBuilder.Tests.EditMode
{
    public class GameCalendarTests
    {
        private GameObject _go;
        private GameCalendar _calendar;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestGameCalendar");
            _calendar = _go.AddComponent<GameCalendar>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void DefaultDay_IsOne()
        {
            Assert.AreEqual(1, _calendar.CurrentDay);
        }

        [Test]
        public void SetCurrentDay_ValidValue_SetsExactly()
        {
            _calendar.SetCurrentDay(42);
            Assert.AreEqual(42, _calendar.CurrentDay);
        }

        [Test]
        public void SetCurrentDay_Zero_ClampsToOne()
        {
            // A save file corrupted/predating this field must never leave the game on "day 0".
            _calendar.SetCurrentDay(0);
            Assert.AreEqual(1, _calendar.CurrentDay);
        }

        [Test]
        public void SetCurrentDay_Negative_ClampsToOne()
        {
            _calendar.SetCurrentDay(-5);
            Assert.AreEqual(1, _calendar.CurrentDay);
        }
    }
}
