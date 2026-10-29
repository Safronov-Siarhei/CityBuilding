using CityBuilder.Buildings;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Research;
using NUnit.Framework;

namespace CityBuilder.Tests.EditMode
{
    /// <summary>
    /// The Laboratory's arithmetic and the shape of its tech list -- both readable without a scene,
    /// and both easy to get wrong in ways nothing else would notice.
    ///
    /// The arithmetic: scientists shorten a research by subtracting seconds, with the first one
    /// buying no speed. The "first one is free" rule and the floor are exactly the two places an
    /// off-by-one lives.
    ///
    /// The list: it is derived from the balance sheet, so a mis-filled cell shows up here as a
    /// building nobody can open or a level with no way in, rather than as a puzzled player.
    /// </summary>
    public class ResearchTests
    {
        [Test]
        public void CancellingPaysBackTheStatedShare_RoundedDown()
        {
            Assert.AreEqual(56, ResearchManager.RefundCoins(80, 70));
            Assert.AreEqual(7, ResearchManager.RefundCoins(11, 70), "11 * 70% is 7.7 -- cancelling must never round up into free money.");
            Assert.AreEqual(0, ResearchManager.RefundCoins(0, 70));
            Assert.AreEqual(0, ResearchManager.RefundCoins(80, 0));
        }

        /// <summary>Level 1 of the Laboratory only OPENS things; the second and third levels of anything need a Laboratory of that same level.</summary>
        [Test]
        public void ALabLevelIsNeededPerResearchKind()
        {
            foreach (var topic in ResearchCatalog.All)
            {
                var expected = topic.Kind == ResearchKind.UnlockBuilding || topic.Kind == ResearchKind.UnlockUnit ? 1 : topic.Level;
                Assert.AreEqual(expected, topic.RequiredLabLevel, $"{topic.Id} asks for the wrong Laboratory level.");
            }
        }

        [Test]
        public void EveryLockedBuilding_HasAWayToOpenIt()
        {
            var config = BalanceConfig.Instance;
            var lockedCount = 0;

            foreach (var building in config.Buildings)
            {
                if (building.startsUnlocked) continue;
                lockedCount++;

                var topic = ResearchCatalog.ById(ResearchTopic.UnlockBuildingId(building.id));
                Assert.IsNotNull(topic, $"'{building.id}' is locked but has no unlock research -- nothing could ever build it.");
                Assert.Greater(topic.BaseSeconds, 0f, $"'{building.id}': an unlock that takes no time is not a gate.");
            }

            Assert.Greater(lockedCount, 0, "Nothing is locked at all -- the whole Laboratory has no purpose.");
        }

        /// <summary>
        /// The Laboratory is deliberately outside its own gate: locking its level 2 behind a
        /// level-2 research would need a level-2 Laboratory to research it. If someone fills in its
        /// research columns, this is what says why they must not.
        /// </summary>
        [Test]
        public void TheLaboratory_GatesNeitherItsOwnLevelsNorItsConstruction()
        {
            Assert.IsFalse(ResearchCatalog.NeedsUnlock(ResearchManager.LaboratoryBuildingId),
                "The Laboratory must be buildable from the start -- it is the only thing that can unlock anything else.");

            for (var level = 2; level <= BuildingInstance.MaxLevel; level++)
            {
                Assert.IsNull(ResearchCatalog.ById(ResearchTopic.BuildingLevelId(ResearchManager.LaboratoryBuildingId, level)),
                    $"The Laboratory's level {level} is gated behind research, which would need a level-{level} Laboratory to unlock. " +
                    $"Clear research_coins_{level} and research_sec_{level} on its row.");
            }
        }

        /// <summary>A level 3 nobody can reach without level 2 -- and, for a locked building, a level 2 nobody can reach without opening it first.</summary>
        [Test]
        public void LevelsOpenInOrder()
        {
            foreach (var topic in ResearchCatalog.All)
            {
                if (topic.Level != 3) continue;

                var expected = topic.IsBuildingTopic
                    ? ResearchTopic.BuildingLevelId(topic.TargetId, 2)
                    : ResearchTopic.UnitLevelId(topic.TargetId, 2);

                // Only when level 2 is itself a research: an ungated level 2 cannot be a prerequisite.
                if (ResearchCatalog.ById(expected) == null) continue;
                Assert.AreEqual(expected, topic.PrerequisiteTopicId, $"{topic.Id} does not require level 2 first.");
            }
        }

        [Test]
        public void ALockedBuildingsFirstLevel_WaitsForItsUnlock()
        {
            foreach (var topic in ResearchCatalog.BuildingTopics)
            {
                if (topic.Kind != ResearchKind.BuildingLevel || topic.Level != 2) continue;
                if (!ResearchCatalog.NeedsUnlock(topic.TargetId)) continue;

                Assert.AreEqual(ResearchTopic.UnlockBuildingId(topic.TargetId), topic.PrerequisiteTopicId,
                    $"{topic.Id} can be researched before the building it upgrades has even been opened.");
            }
        }

        /// <summary>The orcs' row is enemy balance, not something the player researches -- it must never appear in the soldiers tab.</summary>
        [Test]
        public void OnlyThePlayersUnits_AreInTheSoldiersTab()
        {
            Assert.IsNotEmpty(ResearchCatalog.UnitTopics, "The soldiers tab is empty -- the militia's levels never made it into the catalogue.");

            foreach (var topic in ResearchCatalog.UnitTopics)
            {
                Assert.IsTrue(SoldierStats.TryTypeFromSheetId(topic.TargetId, out _),
                    $"'{topic.TargetId}' is in the soldiers tab but the game has no unit type for it.");
            }
        }

        [Test]
        public void MilitiaLevels_AreWorthResearching()
        {
            for (var level = 2; level <= UnitBalance.MaxLevel; level++)
            {
                var topic = ResearchCatalog.ById(ResearchTopic.UnitLevelId("militia", level));
                Assert.IsNotNull(topic, $"The militia's level {level} has no research row.");

                var before = SoldierStats.StatsAt(SoldierType.Militia, level - 1);
                var after = SoldierStats.StatsAt(SoldierType.Militia, level);

                Assert.Greater(after.maxHealth, before.maxHealth, $"Level {level} militia are no tougher -- nothing to research for.");
                Assert.Greater(after.attackDamage, before.attackDamage, $"Level {level} militia hit no harder.");
                Assert.Greater(after.recruitCoins, before.recruitCoins, "A better soldier is supposed to cost more to raise (the design's own decision).");
                Assert.GreaterOrEqual(after.upkeepCoinsPerDay, before.upkeepCoinsPerDay, "A better soldier is supposed to cost at least as much to keep.");
            }
        }

        /// <summary>
        /// Tapping a level's picture says what it buys -- "уровень 2" alone tells the player nothing;
        /// and levels 3-5 cost an upgrade crystal on top of the coins (the user, 2026-10-28).
        /// </summary>
        [Test]
        public void ALevelSaysWhatItBuys_AndFromLevelThreeCostsACrystal()
        {
            Assert.AreEqual(0, ResearchCatalog.ById(ResearchTopic.BuildingLevelId("Warehouse", 2)).Crystals, "Level 2 costs a crystal.");
            Assert.AreEqual(1, ResearchCatalog.ById(ResearchTopic.BuildingLevelId("Warehouse", 3)).Crystals, "Level 3 costs no crystal.");
            Assert.AreEqual(1, ResearchCatalog.ById(ResearchTopic.BuildingLevelId("Warehouse", 5)).Crystals, "Level 5 costs no crystal.");

            var line = UI.ResearchPanelController.DescribeGain("militia", 2, isUnit: true);
            var health = SoldierStats.StatsAt(SoldierType.Militia, 2).maxHealth - SoldierStats.StatsAt(SoldierType.Militia, 1).maxHealth;
            StringAssert.Contains(health.ToString(), line, $"A soldier level's row does not say what it adds: \"{line}\"");
        }

        /// <summary>The eighteen the design lists as available from the first minute, spot-checked against both ends of the rule.</summary>
        [TestCase("Castle", false)]
        [TestCase("Laboratory", false)]
        [TestCase("Sawmill", false)]
        [TestCase("Fence", false)]
        [TestCase("Barracks", false)]
        [TestCase("Smelter", true)]
        [TestCase("Treasury", true)]
        [TestCase("Bridge", true)]
        [TestCase("Colosseum", true)]
        public void TheStartingRoster_IsWhatTheDesignSays(string buildingId, bool needsUnlock)
        {
            Assert.AreEqual(needsUnlock, ResearchCatalog.NeedsUnlock(buildingId));
        }
    }
}
