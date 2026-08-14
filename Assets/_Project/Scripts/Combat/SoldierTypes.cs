using CityBuilder.Core;
using CityBuilder.Resources;
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
        private static string SheetId(SoldierType type)
        {
            switch (type)
            {
                case SoldierType.Militia:
                default:
                    return "militia";
            }
        }

        /// <summary>The whole sheet row, for callers (SoldierUnit) that want several of its numbers at once and cache them.</summary>
        public static UnitBalance Row(SoldierType type) => BalanceConfig.Instance.Unit(SheetId(type));

        /// <summary>
        /// Army-wide cap across every group and type, per the design backlog: chosen partly for
        /// device performance. Deliberately NOT scaled by economy -- the intended difficulty lever
        /// is that upkeep makes filling all the slots painful, not the cap itself.
        /// </summary>
        public static int MaxArmySize => BalanceConfig.Instance.ArmyMaxSize;

        public static string DisplayName(SoldierType type) => Row(type).displayName;

        /// <summary>Militia are meant to lose a 1v1 against a level 1 orc and win by numbers -- see ArmyBalanceTests, which pins that relationship down against whatever the sheet currently says.</summary>
        public static int MaxHealth(SoldierType type) => Row(type).maxHealth;

        public static int AttackDamage(SoldierType type) => Row(type).attackDamage;

        public static float AttackIntervalSeconds(SoldierType type) => Row(type).attackIntervalSeconds;

        /// <summary>Coins only for Militia -- an armed peasant needs no forge. Later tiers will add their equipment items on top of this.</summary>
        public static List<ResourceAmount> RecruitCost(SoldierType type)
        {
            return new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Coins, amount = Row(type).recruitCoins } };
        }

        /// <summary>Coins deducted per soldier per game day. Unpayable upkeep disbands soldiers one at a time -- see ArmyManager.</summary>
        public static int UpkeepCoinsPerDay(SoldierType type) => Row(type).upkeepCoinsPerDay;

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
