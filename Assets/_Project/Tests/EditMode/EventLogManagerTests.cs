using CityBuilder.Core;
using NUnit.Framework;
using UnityEngine;

namespace CityBuilder.Tests.EditMode
{
    public class EventLogManagerTests
    {
        private GameObject _go;
        private EventLogManager _log;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestEventLogManager");
            _log = _go.AddComponent<EventLogManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void Log_PrependsNewestFirst()
        {
            _log.Log("first");
            _log.Log("second");

            Assert.AreEqual(2, _log.Entries.Count);
            StringAssert.Contains("second", _log.Entries[0]);
            StringAssert.Contains("first", _log.Entries[1]);
        }

        [Test]
        public void Log_NoGameCalendarInScene_DefaultsToDayOne()
        {
            // GameCalendar.Instance is null in this isolated test (no GameCalendar was created) --
            // Log must fall back to day 1 rather than throwing or omitting the day prefix.
            _log.Log("message");
            StringAssert.StartsWith("День 1:", _log.Entries[0]);
        }

        [Test]
        public void Log_BeyondMaxEntries_DropsOldest()
        {
            for (var i = 0; i < 10; i++)
            {
                _log.Log($"entry {i}");
            }

            Assert.AreEqual(8, _log.Entries.Count);
            StringAssert.Contains("entry 9", _log.Entries[0]);
            StringAssert.Contains("entry 2", _log.Entries[7]);
        }

        [Test]
        public void Log_FiresOnLogChanged()
        {
            var fireCount = 0;
            _log.OnLogChanged += () => fireCount++;

            _log.Log("a");
            _log.Log("b");

            Assert.AreEqual(2, fireCount);
        }
    }
}
