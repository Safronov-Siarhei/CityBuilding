using CityBuilder.Combat;
using CityBuilder.Resources;
using CityBuilder.Saving;
using NUnit.Framework;
using UnityEngine;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// The save file is JsonUtility's idea of these classes, not C#'s: it writes public FIELDS of
    /// serializable types and silently ignores everything else. A property instead of a field, or a
    /// type it does not know, costs the player that part of their game with no error anywhere --
    /// which is how the army came to be dropped by every save in the first place.
    ///
    /// So this round-trips a filled-in save through the same serializer SaveSystem uses and reads
    /// the interesting parts back.
    /// </summary>
    public class SaveDataShapeTests
    {
        private static GameSaveData RoundTrip(GameSaveData data)
        {
            return JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(data));
        }

        private static GameSaveData WithAnArmyAndAHungryTown()
        {
            var data = new GameSaveData { population = 12, hungryDaysInARow = 2 };
            data.recentStarvationDeaths.AddRange(new[] { 0, 3, 1 });
            data.resources.Add(new ResourceEntry { type = ResourceType.Bread, amount = 7 });

            var group = new ArmyGroupEntry
            {
                type = SoldierType.Militia,
                holdPosition = new Vector3(4f, 0f, -9f),
                priority = TargetPriority.Structures
            };
            group.soldiers.Add(new SoldierEntry { position = new Vector3(1f, 0f, 2f), currentHealth = 5 });
            group.soldiers.Add(new SoldierEntry { position = new Vector3(3f, 0f, 4f), currentHealth = 12 });
            data.armyGroups.Add(group);

            return data;
        }

        [Test]
        public void TheArmy_SurvivesTheSaveFile()
        {
            var loaded = RoundTrip(WithAnArmyAndAHungryTown());

            Assert.AreEqual(1, loaded.armyGroups.Count, "The army did not survive serialization -- soldiers would vanish on every load.");

            var group = loaded.armyGroups[0];
            Assert.AreEqual(SoldierType.Militia, group.type);
            Assert.AreEqual(TargetPriority.Structures, group.priority, "The group's standing target priority is an order the player gave; it has to come back with them.");
            Assert.AreEqual(new Vector3(4f, 0f, -9f), group.holdPosition);

            Assert.AreEqual(2, group.soldiers.Count);
            Assert.AreEqual(5, group.soldiers[0].currentHealth, "A wounded soldier must reload wounded, not healed.");
            Assert.AreEqual(new Vector3(3f, 0f, 4f), group.soldiers[1].position);
        }

        [Test]
        public void TheHungerStreak_SurvivesTheSaveFile()
        {
            var loaded = RoundTrip(WithAnArmyAndAHungryTown());

            Assert.AreEqual(2, loaded.hungryDaysInARow, "Reloading would hand a starving town its whole grace period back.");
            CollectionAssert.AreEqual(new[] { 0, 3, 1 }, loaded.recentStarvationDeaths, "Happiness remembers recent deaths by day, in order.");
        }

        /// <summary>A file written before any of this existed has no such fields at all, and has to read as a town with no army that has never gone hungry -- not as a crash.</summary>
        [Test]
        public void AnOlderSave_ReadsAsNoArmyAndNoHunger()
        {
            var loaded = JsonUtility.FromJson<GameSaveData>("{\"version\":1,\"population\":9,\"currentDay\":3}");

            Assert.AreEqual(9, loaded.population);
            Assert.IsNotNull(loaded.armyGroups);
            Assert.IsEmpty(loaded.armyGroups);
            Assert.AreEqual(0, loaded.hungryDaysInARow);
            Assert.IsNotNull(loaded.recentStarvationDeaths);
            Assert.IsEmpty(loaded.recentStarvationDeaths);
        }

        /// <summary>The empty group is not an oversight: ArmyManager keeps a group whose last member died because it still holds the player's rally point and priority.</summary>
        [Test]
        public void AnEmptyGroup_IsStillWorthSaving()
        {
            var data = new GameSaveData();
            data.armyGroups.Add(new ArmyGroupEntry { type = SoldierType.Militia, priority = TargetPriority.Structures });

            var loaded = RoundTrip(data);

            Assert.AreEqual(1, loaded.armyGroups.Count);
            Assert.IsEmpty(loaded.armyGroups[0].soldiers);
            Assert.AreEqual(TargetPriority.Structures, loaded.armyGroups[0].priority);
        }
    }
}
