namespace CityBuilder.Combat
{
    /// <summary>
    /// A mode's cut of every MELEE reach against units (soldiers and orcs alike; shooters untouched), read when a
    /// unit loads its stats. Campaign mission 2 sets it (m2_melee_reach_mult; the user, 2026-11-12: «солдаты и орки
    /// дерутся на большом расстоянии друг от друга, нужно чтобы они подходили вплотную») and puts it back to 1.
    /// </summary>
    public static class MeleeReach
    {
        public static float Factor = 1f;
    }
}
