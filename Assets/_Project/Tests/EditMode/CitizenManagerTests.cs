using CityBuilder.Citizens;
using NUnit.Framework;
using UnityEngine;

namespace CityBuilder.Tests.EditMode
{
    public class CitizenManagerTests
    {
        private GameObject _go;
        private CitizenManager _manager;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestCitizenManager");
            _manager = _go.AddComponent<CitizenManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void FreshManager_HasZeroPopulation()
        {
            Assert.AreEqual(0, _manager.TotalPopulation);
            Assert.AreEqual(0, _manager.IdlePopulation);
        }

        [Test]
        public void AddCitizens_IncreasesBothTotalAndIdle()
        {
            _manager.AddCitizens(5);
            Assert.AreEqual(5, _manager.TotalPopulation);
            Assert.AreEqual(5, _manager.IdlePopulation);
        }

        [Test]
        public void NotifyWorkerAssigned_MovesOneFromIdleToAssigned()
        {
            _manager.AddCitizens(3);
            var accepted = _manager.NotifyWorkerAssigned();

            Assert.IsTrue(accepted);
            Assert.AreEqual(3, _manager.TotalPopulation);
            Assert.AreEqual(2, _manager.IdlePopulation);
        }

        [Test]
        public void NotifyWorkerAssigned_NoIdleCitizens_ReturnsFalseAndChangesNothing()
        {
            var accepted = _manager.NotifyWorkerAssigned();

            Assert.IsFalse(accepted);
            Assert.AreEqual(0, _manager.TotalPopulation);
            Assert.AreEqual(0, _manager.IdlePopulation);
        }

        [Test]
        public void NotifyWorkerAssigned_CannotOverAssignPastPopulation()
        {
            // Regression guard: assigning more workers than total population must never happen --
            // IdlePopulation would go negative (clamped to 0 by Max, but the assignment itself
            // must still be refused, not silently accepted).
            _manager.AddCitizens(2);
            Assert.IsTrue(_manager.NotifyWorkerAssigned());
            Assert.IsTrue(_manager.NotifyWorkerAssigned());
            Assert.IsFalse(_manager.NotifyWorkerAssigned());
            Assert.AreEqual(0, _manager.IdlePopulation);
        }

        [Test]
        public void NotifyWorkerUnassigned_MovesOneBackToIdle()
        {
            _manager.AddCitizens(2);
            _manager.NotifyWorkerAssigned();
            _manager.NotifyWorkerUnassigned();

            Assert.AreEqual(2, _manager.IdlePopulation);
        }

        [Test]
        public void NotifyWorkerUnassigned_NoAssignedWorkers_DoesNotGoNegative()
        {
            _manager.AddCitizens(2);
            _manager.NotifyWorkerUnassigned();

            Assert.AreEqual(2, _manager.IdlePopulation);
        }

        [Test]
        public void SetPopulation_ResetsTotalButKeepsAssignedCount()
        {
            // Used by save/load restore -- population and worker-assignment are set independently
            // there (see GameSaveController.ApplyLoadedState), so SetPopulation alone must not
            // touch the assigned count.
            _manager.AddCitizens(5);
            _manager.NotifyWorkerAssigned();
            _manager.SetPopulation(10);

            Assert.AreEqual(10, _manager.TotalPopulation);
            Assert.AreEqual(9, _manager.IdlePopulation);
        }

        [Test]
        public void NotifyWorkersAssignedBulk_SkipsTheIdleAvailabilityCheck()
        {
            // Used by save/load to restore an already-valid worker count directly -- must not
            // require idle citizens to "accept" the assignment the way NotifyWorkerAssigned does.
            _manager.AddCitizens(3);
            _manager.NotifyWorkersAssignedBulk(3);

            Assert.AreEqual(3, _manager.TotalPopulation);
            Assert.AreEqual(0, _manager.IdlePopulation);
        }
    }
}
