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
    }
}
