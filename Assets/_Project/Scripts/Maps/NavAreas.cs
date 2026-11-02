using UnityEngine.AI;

namespace CityBuilder.Maps
{
    /// <summary>
    /// NavMesh areas beyond the built-in ones. A gate (the user, 2026-10-21: "ворота должны
    /// открываться для жителей и закрываться для противников") does not carve itself out of the
    /// NavMesh the way every other building does; it marks its cell as the Gate area instead
    /// (MeshMapApplier.RegisterGate). Citizens and soldiers path over every area, so the gate is
    /// open to them; orcs path with <see cref="WithoutGates"/>, so for them the cell is a wall --
    /// their route stops at it and they break it down like any other building in the way.
    /// </summary>
    public static class NavAreas
    {
        public const int Gate = 3;

        /// <summary>A moat's water (the user, 2026-11-02): open to all, but dear to path over (moat_path_cost), so a road or dry land round it is taken when there is one.</summary>
        public const int Moat = 4;

        public static int WithoutGates => NavMesh.AllAreas & ~(1 << Gate);
    }
}
