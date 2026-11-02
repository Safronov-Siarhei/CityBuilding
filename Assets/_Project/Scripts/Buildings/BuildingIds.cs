namespace CityBuilder.Buildings
{
    /// <summary>
    /// The handful of building ids the game's rules name directly -- the ones where losing the
    /// building or standing next to it means something, rather than just another entry in the
    /// hotbar.
    ///
    /// They live here because an id is the model's file name as much as it is a row in the balance
    /// sheet, and the taxonomy renames things: the Ратуша's id is "Castle" because its model is
    /// Castle1-lvl1.fbx. The last rename of this one had to be made in six files at once, and a
    /// missed one would not have failed to compile -- it would have quietly stopped the town hall
    /// from being a town hall.
    /// </summary>
    public static class BuildingIds
    {
        /// <summary>The Ратуша: the settlement's first and mandatory building, whose loss is the game's defeat condition and whose model is the castle.</summary>
        public const string TownHall = "Castle";

        /// <summary>The Казарма: where soldiers are hired (in a run, by the buttons beside the «!», UI.FocusHireBar).</summary>
        public const string Barracks = "Barracks";

        /// <summary>
        /// Buildings put away for now (the user, 2026-11-02, the food rework): the Windmill is gone
        /// from the food chain and the Barns from storage -- every food sits in the Pantries. They
        /// keep their data and models; only the build menu and the Laboratory leave them out.
        /// </summary>
        public static readonly System.Collections.Generic.HashSet<string> Hidden =
            new System.Collections.Generic.HashSet<string> { "Windmill", "Barn", "BigBarn" };

        // the food rework's buildings (FoodOutput)
        public const string FisherHut = "FisherHut";
        public const string Farm = "Farm";
        public const string Orchard = "Orchard";
        public const string PigFarm = "PigFarm";
        public const string Baker = "Baker";
        public const string Smokehouse = "Smokehouse";
        /// <summary>The Tax House (the user, 2026-11-02): each level raises what a citizen pays a day; its levels cost crystals.</summary>
        public const string TaxHouse = "TaxHouse";
    }
}
