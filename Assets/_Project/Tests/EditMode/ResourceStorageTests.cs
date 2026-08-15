using CityBuilder.Resources;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// The resource-to-storehouse mapping, which decides which building a player has to put up
    /// before they can keep something. It's a plain switch, but it's the kind of switch where
    /// adding a resource and forgetting to place it silently drops it into the wrong warehouse --
    /// and the symptom would be "my gold stops at 200", far away from the cause.
    /// </summary>
    public class ResourceStorageTests
    {
        [TestCase(ResourceType.Wood)]
        [TestCase(ResourceType.Stone)]
        [TestCase(ResourceType.Iron)]
        [TestCase(ResourceType.Coal)]
        public void RawGoods_GoInTheWarehouse(ResourceType type)
        {
            Assert.AreEqual(ResourceStorageGroup.Materials, ResourceStorage.GroupOf(type));
        }

        [Test]
        public void Food_HasItsOwnStore()
        {
            Assert.AreEqual(ResourceStorageGroup.Food, ResourceStorage.GroupOf(ResourceType.Food));
        }

        [TestCase(ResourceType.Gold)]
        [TestCase(ResourceType.Coins)]
        public void MoneyGoesInTheTreasury(ResourceType type)
        {
            Assert.AreEqual(ResourceStorageGroup.Valuables, ResourceStorage.GroupOf(type));
        }

        /// <summary>Population is a headcount, not something a building holds -- giving it a ceiling would cap the town's growth by accident.</summary>
        [Test]
        public void Population_IsNotStored()
        {
            Assert.AreEqual(ResourceStorageGroup.None, ResourceStorage.GroupOf(ResourceType.Population));
        }

        [Test]
        public void EveryResource_HasAHome()
        {
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                if (type == ResourceType.Population) continue;

                Assert.AreNotEqual(ResourceStorageGroup.None, ResourceStorage.GroupOf(type),
                    $"{type} belongs to no storage group, so nothing the player can build will ever raise its ceiling.");
            }
        }
    }
}
