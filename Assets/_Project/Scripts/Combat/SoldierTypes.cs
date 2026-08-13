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
    /// Per-type stats and costs, as pure static data. Deliberately not a ScriptableObject: every
    /// number here is gameplay balance that belongs in version control next to the code that reads
    /// it, and being plain statics means the EditMode tests can assert on the balance relationships
    /// (a militia must lose a straight 1v1 against a level 1 orc, recruitment must be affordable
    /// from a day's taxes, and so on) without a live scene.
    ///
    /// All numbers are first-pass and freely tunable.
    /// </summary>
    public static class SoldierStats
    {
        /// <summary>
        /// Army-wide cap across every group and type, per the design backlog: fixed at 20, chosen
        /// partly for device performance. Deliberately NOT scaled by economy -- the intended
        /// difficulty lever is that upkeep makes filling all 20 slots painful, not the cap itself.
        /// </summary>
        public const int MaxArmySize = 20;

        public static string DisplayName(SoldierType type)
        {
            switch (type)
            {
                case SoldierType.Militia:
                default:
                    return "Ополчение";
            }
        }

        /// <summary>Militia are meant to lose a 1v1 against a level 1 orc (20 HP, 4 damage) and win by numbers -- see SoldierStatsTests, which pins that relationship down.</summary>
        public static int MaxHealth(SoldierType type)
        {
            switch (type)
            {
                case SoldierType.Militia:
                default:
                    return 12;
            }
        }

        public static int AttackDamage(SoldierType type)
        {
            switch (type)
            {
                case SoldierType.Militia:
                default:
                    return 5;
            }
        }

        public static float AttackIntervalSeconds(SoldierType type)
        {
            switch (type)
            {
                case SoldierType.Militia:
                default:
                    return 1.1f;
            }
        }

        /// <summary>Coins only for Militia -- an armed peasant needs no forge. Later tiers add their equipment items on top of this.</summary>
        public static List<ResourceAmount> RecruitCost(SoldierType type)
        {
            switch (type)
            {
                case SoldierType.Militia:
                default:
                    return new List<ResourceAmount> { new ResourceAmount { type = ResourceType.Coins, amount = 25 } };
            }
        }

        /// <summary>Coins deducted per soldier per game day. Unpayable upkeep disbands soldiers one at a time -- see ArmyManager.</summary>
        public static int UpkeepCoinsPerDay(SoldierType type)
        {
            switch (type)
            {
                case SoldierType.Militia:
                default:
                    return 1;
            }
        }

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
