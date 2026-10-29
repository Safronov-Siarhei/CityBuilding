using CityBuilder.Core;
using CityBuilder.Saving;

namespace CityBuilder.Maps.Archipelago
{
    /// <summary>
    /// A generated map's id carries what made it: "Archipelago-v{Version}-{seed}". That id is what a
    /// save stores, so a save brings back its own islands; the version keeps an old seed from
    /// silently drawing a different map after the generator changes (the user, 2026-10-28).
    /// </summary>
    public static class ArchipelagoMapId
    {
        /// <summary>The template definition's id under Resources/MeshMaps.</summary>
        public const string TemplateId = "Archipelago";

        public static string For(int seed) => $"{TemplateId}-v{ArchipelagoGenerator.Version}-{seed}";

        public static bool IsArchipelago(string mapId) => !string.IsNullOrEmpty(mapId) && mapId.StartsWith(TemplateId);

        /// <summary>The seed in an id, or false for an id that is not a generated map (or a plain template id).</summary>
        public static bool TryParse(string mapId, out int seed, out int version)
        {
            seed = 0;
            version = 0;
            if (!IsArchipelago(mapId)) return false;
            var parts = mapId.Split('-');
            return parts.Length == 3 && parts[1].Length > 1 && int.TryParse(parts[1].Substring(1), out version)
                   && int.TryParse(parts[2], out seed);
        }

        /// <summary>
        /// The code a player sees and shares (the user, 2026-10-28): "{version}-{seed}", e.g. "2-482915"
        /// -- digits only, easy to type on a phone; the version in front keeps an old code from
        /// silently drawing another map. Empty for an id that is not a generated map.
        /// </summary>
        public static string Code(string mapId) => TryParse(mapId, out var seed, out var version) ? $"{version}-{seed}" : string.Empty;

        /// <summary>
        /// A typed code back to a map id: "2-482915" (spaces round it forgiven) for this version of
        /// the generator. False for anything else -- a bad code, or one from another version, which
        /// would draw a different map than the friend's.
        /// </summary>
        public static bool TryParseCode(string code, out string mapId, out bool otherVersion)
        {
            mapId = null;
            otherVersion = false;
            if (string.IsNullOrWhiteSpace(code)) return false;
            var parts = code.Trim().Split('-');
            if (parts.Length != 2 || !int.TryParse(parts[0], out var version) || !int.TryParse(parts[1], out var seed) || seed <= 0) return false;
            if (version != ArchipelagoGenerator.Version)
            {
                otherVersion = true;
                return false;
            }
            mapId = For(seed);
            return true;
        }

        /// <summary>A fresh random seed for a new game, as an id.</summary>
        public static string NewRandom() => For(UnityEngine.Random.Range(1, 1000000));

        /// <summary>
        /// The map the scene is about to load -- a new game's pick or the save's -- read before any
        /// system sets itself up, so the grid can be sized for it (GridManager.Awake).
        /// </summary>
        public static string Pending()
        {
            if (!string.IsNullOrEmpty(GameSessionIntent.NewGameMapId)) return GameSessionIntent.NewGameMapId;
            var save = GameSessionIntent.SaveNameToLoad;
            if (string.IsNullOrEmpty(save)) return null;
            var data = SaveSystem.Load(save);
            return data != null ? data.mapId : null;
        }
    }
}
