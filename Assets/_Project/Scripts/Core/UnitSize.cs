namespace CityBuilder.Core
{
    /// <summary>
    /// One factor every walking figure is drawn at: citizens, soldiers and orcs.
    ///
    /// The cube figures are authored 0.72 m tall (a 0.5 body under a 0.22 head). The building set
    /// modelled in Blender is drawn around a door 0.5 m high and a storey of about 0.56 m, which
    /// put every citizen a head taller than the doors he walks through. The user chose to shrink
    /// the people rather than raise the buildings (2026-10-12): 0.625 brings a citizen to 0.45 m.
    ///
    /// Applied as the root's localScale, so the parts AND the CharacterController capsule shrink
    /// together and nothing walks with a collider bigger than its body.
    /// </summary>
    public static class UnitSize
    {
        public const float Scale = 0.625f;
    }
}
