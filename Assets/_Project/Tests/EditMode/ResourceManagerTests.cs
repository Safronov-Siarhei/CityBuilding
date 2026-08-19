using System.Collections.Generic;
using CityBuilder.Resources;
using NUnit.Framework;
using UnityEngine;

namespace CityBuilder.Tests.EditMode
{
    public class ResourceManagerTests
    {
        private GameObject _go;
        private ResourceManager _manager;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestResourceManager");
            _manager = _go.AddComponent<ResourceManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        /// <summary>
        /// The tally raids are sized against (see PlayerProgression). Spending is what it must not
        /// notice: a stockpile-based score would make emptying the stores just before a raid the
        /// cheapest defence in the game, and this is the property that rules that out.
        /// </summary>
        [Test]
        public void LifetimeProduced_CountsOutputAndIgnoresSpending()
        {
            _manager.AddProduced(ResourceType.Wood, 30);
            Assert.AreEqual(30, _manager.LifetimeProduced);

            _manager.Add(ResourceType.Wood, -30);
            Assert.AreEqual(30, _manager.LifetimeProduced, "Spending lowered the lifetime total, so a player could be poor on demand and be raided as a pauper.");
            Assert.AreEqual(0, _manager.GetAmount(ResourceType.Wood), "The test's own spending was supposed to empty the store.");
        }

        /// <summary>
        /// Refunds, taxes and the cheats all go through plain Add, and none of them is the
        /// settlement producing anything. A progression score a cancelled research could raise is
        /// not measuring progression.
        /// </summary>
        [Test]
        public void LifetimeProduced_IgnoresResourcesTheSettlementDidNotMake()
        {
            _manager.Add(ResourceType.Coins, 100);
            Assert.AreEqual(0, _manager.LifetimeProduced);
        }

        /// <summary>Output that overflowed a full store went on the floor: a town whose granary has been full for an hour has not been getting richer for that hour.</summary>
        [Test]
        public void LifetimeProduced_CountsOnlyWhatFitted()
        {
            var capacity = _manager.GetCapacity(ResourceType.Wood);
            _manager.SetAmount(ResourceType.Wood, capacity - 5);

            var stored = _manager.AddProduced(ResourceType.Wood, 100);

            Assert.AreEqual(5, stored, "The fixture was meant to leave room for exactly five.");
            Assert.AreEqual(5, _manager.LifetimeProduced);
        }

        /// <summary>Without the restore every reload knocked the player's progression to zero and the next raid arrived as though the town had just been founded.</summary>
        [Test]
        public void LifetimeProduced_ComesBackFromASave()
        {
            _manager.RestoreLifetimeProduced(4321);
            Assert.AreEqual(4321, _manager.LifetimeProduced);
        }

        [Test]
        public void GetAmount_UnsetType_IsZero()
        {
            Assert.AreEqual(0, _manager.GetAmount(ResourceType.Coal));
        }

        [Test]
        public void Add_IncreasesAmount()
        {
            _manager.SetAmount(ResourceType.Wood, 10);
            _manager.Add(ResourceType.Wood, 5);
            Assert.AreEqual(15, _manager.GetAmount(ResourceType.Wood));
        }

        [Test]
        public void Add_NegativeAmount_Subtracts()
        {
            _manager.SetAmount(ResourceType.Wood, 10);
            _manager.Add(ResourceType.Wood, -4);
            Assert.AreEqual(6, _manager.GetAmount(ResourceType.Wood));
        }

        [Test]
        public void SetAmount_OverridesExisting()
        {
            _manager.SetAmount(ResourceType.Gold, 100);
            _manager.SetAmount(ResourceType.Gold, 7);
            Assert.AreEqual(7, _manager.GetAmount(ResourceType.Gold));
        }

        [Test]
        public void HasEnough_ExactAmount_IsTrue()
        {
            _manager.SetAmount(ResourceType.Wood, 10);
            var cost = new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Wood, amount = 10 } };
            Assert.IsTrue(_manager.HasEnough(cost));
        }

        [Test]
        public void HasEnough_OneShortOfOneResource_IsFalse()
        {
            _manager.SetAmount(ResourceType.Wood, 10);
            _manager.SetAmount(ResourceType.Stone, 10);
            var cost = new List<ResourceAmount>
            {
                new ResourceAmount { type = ResourceType.Wood, amount = 10 },
                new ResourceAmount { type = ResourceType.Stone, amount = 11 },
            };
            Assert.IsFalse(_manager.HasEnough(cost));
        }

        [Test]
        public void TrySpend_SufficientResources_DeductsAllAndReturnsTrue()
        {
            _manager.SetAmount(ResourceType.Wood, 10);
            _manager.SetAmount(ResourceType.Stone, 10);
            var cost = new List<ResourceAmount>
            {
                new ResourceAmount { type = ResourceType.Wood, amount = 4 },
                new ResourceAmount { type = ResourceType.Stone, amount = 3 },
            };

            Assert.IsTrue(_manager.TrySpend(cost));
            Assert.AreEqual(6, _manager.GetAmount(ResourceType.Wood));
            Assert.AreEqual(7, _manager.GetAmount(ResourceType.Stone));
        }

        [Test]
        public void TrySpend_InsufficientOnSecondResource_SpendsNothing()
        {
            // Regression guard: TrySpend must check the whole cost list (HasEnough) before
            // deducting anything -- a naive spend-as-you-go implementation would deduct Wood here
            // before discovering Stone is short, leaving the player short-changed on a failed
            // purchase.
            _manager.SetAmount(ResourceType.Wood, 10);
            _manager.SetAmount(ResourceType.Stone, 1);
            var cost = new List<ResourceAmount>
            {
                new ResourceAmount { type = ResourceType.Wood, amount = 4 },
                new ResourceAmount { type = ResourceType.Stone, amount = 3 },
            };

            Assert.IsFalse(_manager.TrySpend(cost));
            Assert.AreEqual(10, _manager.GetAmount(ResourceType.Wood));
            Assert.AreEqual(1, _manager.GetAmount(ResourceType.Stone));
        }

        [Test]
        public void InfiniteResources_HasEnough_AlwaysTrueRegardlessOfCost()
        {
            _manager.SetInfiniteResources(true);
            var cost = new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Gold, amount = 999999 } };
            Assert.IsTrue(_manager.HasEnough(cost));
        }

        [Test]
        public void InfiniteResources_TrySpend_DoesNotActuallyDeduct()
        {
            _manager.SetInfiniteResources(true);
            _manager.SetAmount(ResourceType.Gold, 5);
            var cost = new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Gold, amount = 999999 } };

            Assert.IsTrue(_manager.TrySpend(cost));
            Assert.AreEqual(5, _manager.GetAmount(ResourceType.Gold));
        }
    }
}
