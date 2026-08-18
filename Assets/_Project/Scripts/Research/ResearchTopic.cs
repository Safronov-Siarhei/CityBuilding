using CityBuilder.Buildings;
using CityBuilder.Core;

namespace CityBuilder.Research
{
    /// <summary>What a research topic is about. The two tabs of the Laboratory's window are these, two apiece.</summary>
    public enum ResearchKind
    {
        /// <summary>Makes a building appear in the hotbar at all.</summary>
        UnlockBuilding = 0,

        /// <summary>Permits BuildingInstance.TryUpgrade to reach one particular level.</summary>
        BuildingLevel = 1,

        /// <summary>Makes a soldier type recruitable at the Barracks.</summary>
        UnlockUnit = 2,

        /// <summary>Raises one soldier type's stats -- for every soldier of that type, including the ones already standing.</summary>
        UnitLevel = 3,
    }

    /// <summary>
    /// One thing the player can research, built from a balance sheet row (see ResearchCatalog).
    /// Immutable: what has actually been researched lives in ResearchManager, so a topic can be
    /// shared freely between the catalogue, the save file and the window listing it.
    /// </summary>
    public class ResearchTopic
    {
        /// <summary>Stable across sessions and sheet reorderings -- this is what the save file stores. Built from the sheet id, so renaming a row loses its research the same way it already loses its balance.</summary>
        public string Id { get; }

        public ResearchKind Kind { get; }

        /// <summary>Building id ("Smelter") or unit sheet id ("militia").</summary>
        public string TargetId { get; }

        /// <summary>2 or 3 for the level topics; 0 for an unlock, which has no level of its own.</summary>
        public int Level { get; }

        public int Coins { get; }

        /// <summary>How long it takes at a Laboratory staffed by one scientist. More of them shorten it -- see ResearchManager.DurationSeconds.</summary>
        public float BaseSeconds { get; }

        /// <summary>Which tab section a building topic is filed under. Meaningless for unit topics.</summary>
        public BuildingCategory Category { get; }

        /// <summary>The sheet's own label for the target, as the fallback when localization has no key for it.</summary>
        public string TargetFallbackName { get; }

        /// <summary>Another topic that must be finished first, or null. Level 3 needs level 2; a locked building's levels need its unlock.</summary>
        public string PrerequisiteTopicId { get; }

        public ResearchTopic(string id, ResearchKind kind, string targetId, int level, int coins, float baseSeconds,
            BuildingCategory category, string targetFallbackName, string prerequisiteTopicId)
        {
            Id = id;
            Kind = kind;
            TargetId = targetId;
            Level = level;
            Coins = coins;
            BaseSeconds = baseSeconds;
            Category = category;
            TargetFallbackName = targetFallbackName;
            PrerequisiteTopicId = prerequisiteTopicId;
        }

        public bool IsBuildingTopic => Kind == ResearchKind.UnlockBuilding || Kind == ResearchKind.BuildingLevel;

        /// <summary>
        /// The Laboratory level this needs, straight from the design: level 1 only OPENS things,
        /// level 2 is what lets anything be researched to level 2, level 3 to level 3.
        /// </summary>
        public int RequiredLabLevel => Kind == ResearchKind.UnlockBuilding || Kind == ResearchKind.UnlockUnit ? 1 : Level;

        /// <summary>What the target is called, localized -- a building through `#building_<id>`, a unit through `#unit_<id>`, matching BuildingData.LocalizedName and SoldierStats.DisplayName.</summary>
        public string TargetName
        {
            get
            {
                var prefix = IsBuildingTopic ? "#building_" : "#unit_";
                return Localization.GetOrDefault(prefix + TargetId.ToLowerInvariant(), TargetFallbackName);
            }
        }

        /// <summary>The row's own caption: "Открыть: Плавильня" or "Плавильня — уровень 2".</summary>
        public string Title
        {
            get
            {
                return Level >= 2
                    ? Localization.Format("#research_level", TargetName, Level)
                    : Localization.Format("#research_unlock", TargetName);
            }
        }

        // Id spellings, kept here so the save format and the catalogue can never disagree about them.
        public static string UnlockBuildingId(string buildingId) => "unlock_building:" + buildingId;
        public static string BuildingLevelId(string buildingId, int level) => "level_building:" + buildingId + ":" + level;
        public static string UnlockUnitId(string unitId) => "unlock_unit:" + unitId;
        public static string UnitLevelId(string unitId, int level) => "level_unit:" + unitId + ":" + level;
    }
}
