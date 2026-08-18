using CityBuilder.Core;
using CityBuilder.Resources;
using System;
using System.Collections.Generic;

namespace CityBuilder.Combat
{
    /// <summary>
    /// The kinds of soldier the player can recruit. One entry today -- Militia, the design's
    /// "жители с вилами": no armour, little health, respectable damage, and coins as the only
    /// recruitment cost. Later tiers (armoured spearmen, archers) are the ones that will need the
    /// equipment buildings and the Laboratory unlock from the design backlog; the type-driven
    /// stat table below is what lets those be added as data rather than as new unit classes.
    /// </summary>
    public enum SoldierType
    {
        Militia = 0,
    }

    /// <summary>
    /// What a group does when it has no explicit attack order: which kind of enemy its members
    /// pick out of everything in range. A persistent per-group mode (the player sets it once on
    /// the group's icon), not a one-shot order -- so a group sent to assault a portal can be told
    /// to ignore the defenders and hit the structure, or to clear the defenders first, and it keeps
    /// doing that for the whole approach.
    /// </summary>
    public enum TargetPriority
    {
        /// <summary>Enemy units first; structures only when no unit is in range.</summary>
        Units = 0,

        /// <summary>Enemy structures (portals, later base buildings) first; units only when no structure is in range.</summary>
        Structures = 1,
    }

    /// <summary>
    /// Reads a soldier type's stats out of the balance sheet (see BalanceConfig): every number
    /// below is authored in the spreadsheet, not here. The lookup goes through this one place so
    /// callers -- and the balance tests -- keep asking the same question they always did, while the
    /// answer now comes from the sheet.
    /// </summary>
    public static class SoldierStats
    {
        /// <summary>Maps the code's unit types onto the sheet's row ids. The sheet is keyed by a readable id rather than by an enum's numeric value, so reordering the enum can't silently repoint a row.</summary>
        public static string SheetIdOf(SoldierType type)
        {
            switch (type)
            {
                case SoldierType.Militia:
                default:
                    return "militia";
            }
        }

        /// <summary>The reverse lookup, for anything walking the sheet rather than the enum -- ResearchCatalog uses it to leave the orcs' row out of the player's tech list.</summary>
        public static bool TryTypeFromSheetId(string sheetId, out SoldierType type)
        {
            foreach (SoldierType candidate in Enum.GetValues(typeof(SoldierType)))
            {
                if (SheetIdOf(candidate) != sheetId) continue;
                type = candidate;
                return true;
            }

            type = default;
            return false;
        }

        /// <summary>The whole sheet row, for callers (SoldierUnit) that want several of its numbers at once and cache them.</summary>
        public static UnitBalance Row(SoldierType type) => BalanceConfig.Instance.Unit(SheetIdOf(type));

        /// <summary>
        /// The level this type currently fights at -- the highest one researched in the Laboratory,
        /// or 1 before any research (and in a scene with no ResearchManager at all, which is how the
        /// balance tests read the sheet without a running game).
        /// </summary>
        public static int CurrentLevel(SoldierType type)
        {
            var research = Research.ResearchManager.Instance;
            return research != null ? research.UnitLevel(type) : 1;
        }

        /// <summary>This type's stats at its currently researched level -- what a soldier actually fights with.</summary>
        public static UnitLevelStats Stats(SoldierType type) => Row(type).LevelStats(CurrentLevel(type));

        /// <summary>This type's stats at a named level, for the window that has to show what the next one is worth.</summary>
        public static UnitLevelStats StatsAt(SoldierType type, int level) => Row(type).LevelStats(level);

        /// <summary>
        /// Army-wide cap across every group and type, per the design backlog: chosen partly for
        /// device performance. Deliberately NOT scaled by economy -- the intended difficulty lever
        /// is that upkeep makes filling all the slots painful, not the cap itself.
        /// </summary>
        public static int MaxArmySize => BalanceConfig.Instance.ArmyMaxSize;

        /// <summary>
        /// From the localization sheet under `#unit_<type>`, not from the units tab's display_name --
        /// that column stays as the sheet author's own label for the row, which nobody translates.
        /// </summary>
        public static string DisplayName(SoldierType type) => Localization.Get("#unit_" + type.ToString().ToLowerInvariant());

        /// <summary>Militia are meant to lose a 1v1 against a level 1 orc and win by numbers -- see ArmyBalanceTests, which pins that relationship down against whatever the sheet currently says.</summary>
        public static int MaxHealth(SoldierType type) => Stats(type).maxHealth;

        public static int AttackDamage(SoldierType type) => Stats(type).attackDamage;

        public static float AttackIntervalSeconds(SoldierType type) => Stats(type).attackIntervalSeconds;

        /// <summary>
        /// Coins only for Militia -- an armed peasant needs no forge. Later tiers will add their
        /// equipment items on top of this. A researched level makes them dearer to raise, per the
        /// design: the upgrade is army-wide, so it has to cost something ongoing.
        /// </summary>
        public static List<ResourceAmount> RecruitCost(SoldierType type)
        {
            return new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Coins, amount = Stats(type).recruitCoins } };
        }

        /// <summary>Coins deducted per soldier per game day, at the type's researched level. Unpayable upkeep disbands soldiers one at a time -- see ArmyManager.</summary>
        public static int UpkeepCoinsPerDay(SoldierType type) => Stats(type).upkeepCoinsPerDay;

        /// <summary>Total coins per day for a whole army -- extracted so the UI and the tests share one formula with the daily charge itself.</summary>
        public static int TotalUpkeepPerDay(IEnumerable<SoldierType> soldierTypes)
        {
            var total = 0;
            foreach (var type in soldierTypes)
            {
                total += UpkeepCoinsPerDay(type);
            }
            return total;
        }
    }
}
