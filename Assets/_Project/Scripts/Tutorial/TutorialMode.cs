using CityBuilder.Maps.Archipelago;

namespace CityBuilder.Tutorial
{
    /// <summary>
    /// The tutorial (Design/features/tutorial.md, the user, 2026-11-05): a short guided game on its own
    /// two small islands (Blender's Isl-tutorial1, one frame in the middle of the archipelago's sea).
    /// It is an archipelago game with its own map id; what it switches off is asked of this flag:
    /// no fog, no bot, no random orcs, no lighthouse, no start scout, no dungeons/caches/fertile land,
    /// never saved. The step script, the trimmed build menu and the King's hints come in later stages.
    /// </summary>
    public static class TutorialMode
    {
        /// <summary>True from the scene's first Awake while the game being loaded is the tutorial.</summary>
        public static bool IsActive { get; private set; }

        /// <summary>Read once by MeshMapApplier.Awake, before any system's Start: the map about to load is the tutorial's.</summary>
        public static void DecideFor(string pendingMapId) => IsActive = ArchipelagoMapId.IsTutorial(pendingMapId);
    }
}
