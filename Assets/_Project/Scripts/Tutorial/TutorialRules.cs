using CityBuilder.Buildings;
using CityBuilder.Combat;
using CityBuilder.Core;
using CityBuilder.Ships;

namespace CityBuilder.Tutorial
{
    /// <summary>
    /// The tutorial's own rules (the user, 2026-11-05; Design/features/tutorial.md): one build-menu
    /// category with the Town Hall, the Hovel, the Barracks and the Shipyard, one of each (the card
    /// stays, «1/1»); every building up in tutorial_build_sec; resources never run out; the Town Hall
    /// brings tutorial_start_people; the Barracks hires the Латник only, the Shipyard the transport
    /// only, whatever their levels; the King never dies (he heals himself when low); the building
    /// cards show nothing extra. Asked by the systems through TutorialMode.IsActive, so nothing
    /// changes outside the tutorial.
    /// </summary>
    public static class TutorialRules
    {
        public static readonly string[] Buildings = { BuildingIds.TownHall, "Hovel", BuildingIds.Barracks, ShipManager.ShipyardId };

        /// <summary>The one soldier the Barracks offers: the armoured man-at-arms (the user's «гвардеец», a level-3 soldier).</summary>
        public const SoldierType Soldier = SoldierType.ManAtArms;

        /// <summary>The one ship the Shipyard offers: it carries the King and the soldier across.</summary>
        public const ShipKind Ship = ShipKind.Transport;

        public static bool Allows(BuildingData data) => data != null && System.Array.IndexOf(Buildings, data.buildingName) >= 0;

        /// <summary>One of each: built, its card stays greyed with «1/1».</summary>
        public static int LimitOf(BuildingData data) => Allows(data) ? 1 : 0;

        public static bool AtLimit(BuildingData data) => LimitOf(data) > 0 && BuildingInstance.CountOf(data.buildingName) >= LimitOf(data);

        /// <summary>
        /// The one building the script asks for now (TutorialDirector sets it; null between the build
        /// steps): its card is the one not locked, the arrow points at it (the user, 2026-11-05).
        /// </summary>
        public static string Wanted { get; set; }

        /// <summary>This card may be tapped now: the step's own building, not yet built.</summary>
        public static bool IsWanted(BuildingData data) => data != null && data.buildingName == Wanted && !AtLimit(data);

        public static float BuildSeconds => BalanceConfig.Instance.TutorialBuildSeconds;

        /// <summary>The chop at a tree, the dig at a boulder (the walk is the usual one).</summary>
        public static float GatherSeconds => BalanceConfig.Instance.TutorialGatherSeconds;

        public static int StartPeople => BalanceConfig.Instance.TutorialStartPeople;

        /// <summary>The King's health share at which he heals himself back to full.</summary>
        public static float KingHealBelow => BalanceConfig.Instance.TutorialKingHealBelow;
    }
}
