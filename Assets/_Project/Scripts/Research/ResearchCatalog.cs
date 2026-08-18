using System.Collections.Generic;
using CityBuilder.Combat;
using CityBuilder.Core;
using UnityEngine;

namespace CityBuilder.Research
{
    /// <summary>
    /// Every research topic the game has, derived from the balance sheet rather than authored
    /// anywhere else: a building's row says whether it starts unlocked and what its unlock and its
    /// two levels cost, and that is the whole definition of the tech list. Adding a research is
    /// filling in cells, exactly like adding a recipe.
    ///
    /// Built once and cached -- the list is a hundred-odd small objects and the window rebuilds its
    /// rows from it on every open.
    /// </summary>
    public static class ResearchCatalog
    {
        private static List<ResearchTopic> _all;
        private static List<ResearchTopic> _buildingTopics;
        private static List<ResearchTopic> _unitTopics;
        private static Dictionary<string, ResearchTopic> _byId;
        private static HashSet<string> _lockedBuildings;

        public static IReadOnlyList<ResearchTopic> All { get { Build(); return _all; } }

        /// <summary>The "Изучение зданий" tab, grouped by category (in the design's own order) and, inside a category, by building with its levels in order.</summary>
        public static IReadOnlyList<ResearchTopic> BuildingTopics { get { Build(); return _buildingTopics; } }

        /// <summary>The "Изучение солдат" tab. Only rows the code has a SoldierType for -- the orcs' row is balance for the enemy, not something the player researches.</summary>
        public static IReadOnlyList<ResearchTopic> UnitTopics { get { Build(); return _unitTopics; } }

        public static ResearchTopic ById(string id)
        {
            Build();
            return id != null && _byId.TryGetValue(id, out var topic) ? topic : null;
        }

        /// <summary>Whether this building needs opening in the Laboratory before it can be built at all.</summary>
        public static bool NeedsUnlock(string buildingId)
        {
            Build();
            return buildingId != null && _lockedBuildings.Contains(buildingId);
        }

        /// <summary>Drops the cache. Only for tests that rebuild BalanceConfig underneath it -- the running game's sheet never changes.</summary>
        public static void Invalidate()
        {
            _all = null;
        }

        private static void Build()
        {
            if (_all != null) return;

            _all = new List<ResearchTopic>();
            _buildingTopics = new List<ResearchTopic>();
            _unitTopics = new List<ResearchTopic>();
            _byId = new Dictionary<string, ResearchTopic>();
            _lockedBuildings = new HashSet<string>();

            var config = BalanceConfig.Instance;

            foreach (var building in config.Buildings)
            {
                if (string.IsNullOrEmpty(building.id)) continue;
                AddBuildingTopics(building);
            }

            foreach (var unit in config.Units)
            {
                if (string.IsNullOrEmpty(unit.id)) continue;
                if (!SoldierStats.TryTypeFromSheetId(unit.id, out _)) continue;
                AddUnitTopics(unit);
            }

            _buildingTopics.Sort(CompareBuildingTopics);
            _unitTopics.Sort((a, b) => a.Level.CompareTo(b.Level));

            foreach (var topic in _all)
            {
                _byId[topic.Id] = topic;
            }
        }

        private static void AddBuildingTopics(BuildingBalance building)
        {
            string unlockId = null;

            if (!building.startsUnlocked)
            {
                if (!building.unlockResearch.IsAuthored)
                {
                    // A building nobody can open and nobody can build is invisible to the player and
                    // impossible to diagnose from inside the game -- say so, and leave it buildable.
                    Debug.LogError($"ResearchCatalog: '{building.id}' is marked starts_unlocked=0 but names no research_coins/research_sec, " +
                                   "so nothing could ever open it. Treating it as unlocked.");
                }
                else
                {
                    unlockId = ResearchTopic.UnlockBuildingId(building.id);
                    _lockedBuildings.Add(building.id);
                    Add(new ResearchTopic(unlockId, ResearchKind.UnlockBuilding, building.id, 0,
                        building.unlockResearch.coins, building.unlockResearch.seconds,
                        building.category, building.displayName, null), _buildingTopics);
                }
            }

            // A level's prerequisite is the level below it, and the first gated level's is the
            // building's own unlock -- so a locked building's list opens up in one order only.
            var previous = unlockId;
            for (var level = 2; level <= Buildings.BuildingInstance.MaxLevel; level++)
            {
                var step = building.ResearchToReach(level);
                if (step == null)
                {
                    // An ungated level does not become the next one's prerequisite: skipping it
                    // would otherwise make level 3 unreachable.
                    continue;
                }

                var id = ResearchTopic.BuildingLevelId(building.id, level);
                Add(new ResearchTopic(id, ResearchKind.BuildingLevel, building.id, level, step.coins, step.seconds,
                    building.category, building.displayName, previous), _buildingTopics);
                previous = id;
            }
        }

        private static void AddUnitTopics(UnitBalance unit)
        {
            string unlockId = null;

            if (!unit.startsUnlocked)
            {
                if (!unit.unlockResearch.IsAuthored)
                {
                    Debug.LogError($"ResearchCatalog: unit '{unit.id}' is marked starts_unlocked=0 but names no unlock research, " +
                                   "so it could never be recruited. Treating it as unlocked.");
                }
                else
                {
                    unlockId = ResearchTopic.UnlockUnitId(unit.id);
                    Add(new ResearchTopic(unlockId, ResearchKind.UnlockUnit, unit.id, 0,
                        unit.unlockResearch.coins, unit.unlockResearch.seconds,
                        default, unit.displayName, null), _unitTopics);
                }
            }

            var previous = unlockId;
            for (var level = 2; level <= UnitBalance.MaxLevel; level++)
            {
                var step = unit.ResearchToReach(level);
                if (step == null) continue;

                var id = ResearchTopic.UnitLevelId(unit.id, level);
                Add(new ResearchTopic(id, ResearchKind.UnitLevel, unit.id, level, step.coins, step.seconds,
                    default, unit.displayName, previous), _unitTopics);
                previous = id;
            }
        }

        private static void Add(ResearchTopic topic, List<ResearchTopic> tab)
        {
            _all.Add(topic);
            tab.Add(topic);
        }

        /// <summary>
        /// Category first (the enum is in the design's own order: Город, Склады, Развлечения,
        /// Оборонительные, Производственные, Производство еды, Водные), then the building's id, then
        /// its levels in order -- so one building's unlock and levels always sit together.
        ///
        /// Sorted on the sheet id rather than the localized name so the list does not reshuffle when
        /// the player switches language mid-game.
        /// </summary>
        private static int CompareBuildingTopics(ResearchTopic a, ResearchTopic b)
        {
            if (a.Category != b.Category) return ((int)a.Category).CompareTo((int)b.Category);

            var byTarget = string.CompareOrdinal(a.TargetId, b.TargetId);
            return byTarget != 0 ? byTarget : a.Level.CompareTo(b.Level);
        }
    }
}
