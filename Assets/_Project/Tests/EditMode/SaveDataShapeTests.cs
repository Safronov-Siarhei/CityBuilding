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
                priority = TargetPriority.Structures,
                attackTargetKind = ArmyAttackTargetKind.Orc,
                attackTargetOrcIndex = 0
            };
            group.soldiers.Add(new SoldierEntry { position = new Vector3(1f, 0f, 2f), currentHealth = 5 });
            group.soldiers.Add(new SoldierEntry { position = new Vector3(3f, 0f, 4f), currentHealth = 12 });
            data.armyGroups.Add(group);

            data.portalPlaced = true;
            data.portalCell = new Vector2Int(11, 23);
            data.portalHealth = 190;
            data.secondsUntilNextRaid = 41.5f;
            data.orcs.Add(new OrcEntry { position = new Vector3(-8f, 0f, 5f), level = 3, currentHealth = 44 });
            data.raidsSuspended = true;
            data.selectedGroupIndex = 0;

            data.migrationTimerSeconds = 37.5f;
            data.settlingInSecondsRemaining = 118.25f;

            data.rocks.Add(new RockEntry { cellX = 40, cellY = 91, remaining = 14 });
            data.rocks.Add(new RockEntry { cellX = 7, cellY = 3, remaining = 2 });

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

        [Test]
        public void ThePortalAndTheOrcs_SurviveTheSaveFile()
        {
            var loaded = RoundTrip(WithAnArmyAndAHungryTown());

            Assert.IsTrue(loaded.portalPlaced);
            Assert.AreEqual(new Vector2Int(11, 23), loaded.portalCell, "A portal restored somewhere else is a second portal, not the same one.");
            Assert.AreEqual(190, loaded.portalHealth, "A repaired portal would make reloading a way to undo an assault on the map's objective.");
            Assert.AreEqual(41.5f, loaded.secondsUntilNextRaid, 0.01f);

            Assert.AreEqual(1, loaded.orcs.Count, "The orcs already in the field vanished, so reloading would call off a raid.");
            Assert.AreEqual(3, loaded.orcs[0].level, "Level is what an orc's health and damage are scaled by; losing it downgrades the raider.");
            Assert.AreEqual(44, loaded.orcs[0].currentHealth);
        }

        /// <summary>
        /// The order the player gave and the group they were giving it to. Both used to be dropped:
        /// a group sent to break the portal came back holding its ground, and the player came back
        /// in build mode instead of command mode.
        /// </summary>
        [Test]
        public void TheAttackOrderAndTheSelection_SurviveTheSaveFile()
        {
            var loaded = RoundTrip(WithAnArmyAndAHungryTown());

            Assert.AreEqual(ArmyAttackTargetKind.Orc, loaded.armyGroups[0].attackTargetKind, "The group came back holding instead of attacking.");
            Assert.AreEqual(0, loaded.armyGroups[0].attackTargetOrcIndex, "The order came back pointing at nothing, which is the same as no order at all.");
            Assert.AreEqual(0, loaded.selectedGroupIndex, "Selection decides what a tap on the world does, so losing it changes the player's input mode under them.");
        }

        /// <summary>The other half of the same field: the map's objective, which is what a group is actually sent to break.</summary>
        [Test]
        public void APortalAttackOrder_SurvivesTheSaveFile()
        {
            var data = new GameSaveData();
            data.armyGroups.Add(new ArmyGroupEntry { type = SoldierType.Militia, attackTargetKind = ArmyAttackTargetKind.Portal });

            var loaded = RoundTrip(data);

            Assert.AreEqual(ArmyAttackTargetKind.Portal, loaded.armyGroups[0].attackTargetKind);
            Assert.AreEqual(-1, loaded.armyGroups[0].attackTargetOrcIndex, "A portal order carries no orc index -- pointing it at orc 0 would retarget the group on load.");
        }

        /// <summary>Only the OrcSpawn cheat turns this off, which is the whole reason it is saved: a wave arriving because the flag reset itself lands in the middle of whatever it was turned off to watch.</summary>
        [Test]
        public void TheSuspendedRaidClock_SurvivesTheSaveFile()
        {
            Assert.IsTrue(RoundTrip(WithAnArmyAndAHungryTown()).raidsSuspended);
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
            Assert.IsFalse(loaded.portalPlaced, "An older save has to load as a game whose portal has not opened yet, so the raid manager opens one as usual.");
            Assert.IsNotNull(loaded.orcs);
            Assert.IsEmpty(loaded.orcs);
            Assert.AreEqual(-1, loaded.selectedGroupIndex, "A missing field deserializes to zero, and zero here would select the first group of an army that does not exist.");
            Assert.IsFalse(loaded.raidsSuspended, "An older save has to load as a game whose raids run normally.");
        }

        [Test]
        public void TheMigrationClocks_SurviveTheSaveFile()
        {
            var loaded = RoundTrip(WithAnArmyAndAHungryTown());

            Assert.AreEqual(37.5f, loaded.migrationTimerSeconds, 0.001f, "The wait for the next settler restarted -- reloading would be a way to skip it.");
            Assert.AreEqual(118.25f, loaded.settlingInSecondsRemaining, 0.001f, "The settling-in grace came back whole -- reloading would be a way to extend it.");
        }

        [Test]
        public void TheBoulders_SurviveTheSaveFile()
        {
            // Stone is the one resource the map never makes more of, and the boulders are
            // scattered by unseeded Random -- so without these a reload dealt a fresh map with
            // every rock full again, and saving became a way to undo an hour of quarrying.
            var loaded = RoundTrip(WithAnArmyAndAHungryTown());

            Assert.AreEqual(2, loaded.rocks.Count, "The boulders did not survive serialization -- depletion would be undone by every load.");
            Assert.AreEqual(40, loaded.rocks[0].cellX);
            Assert.AreEqual(91, loaded.rocks[0].cellY);
            Assert.AreEqual(14, loaded.rocks[0].remaining, "A half-worked boulder came back with a different amount of stone in it.");
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
            Assert.AreEqual(ArmyAttackTargetKind.None, loaded.armyGroups[0].attackTargetKind, "A group with nobody in it cannot be attacking anything.");
        }
    }
}
