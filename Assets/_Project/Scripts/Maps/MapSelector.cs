using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>
    /// Picks which pre-made map a new game starts on. Maps the player hasn't seen yet (per
    /// MapHistoryStore) are always drawn first; once every available map has been seen at least
    /// once, it falls back to a plain random pick across the whole catalog.
    ///
    /// Draws from MeshMapCatalog (hand-authored Blender maps) — the older PNG-driven
    /// MapCatalog/MapDefinition path is still fully intact in the codebase (existing saves that
    /// reference a PNG map id still load fine via MapTerrainGenerator), it's just no longer fed
    /// into new games.
    /// </summary>
    public static class MapSelector
    {
        /// <summary>
        /// While a map is being tested every new game starts on it (Map 3 from 2026-10-13; Map 4,
        /// the Two Rivers Valley, from 2026-10-20). The other maps stay in the catalog, so their
        /// saves still load. Empty restores the unseen-first pick below.
        /// </summary>
        public const string OnlyMapId = "Map4";

        /// <summary>Returns null if no maps have been imported into the catalog yet.</summary>
        public static MeshMapDefinition PickForNewGame()
        {
            var all = MeshMapCatalog.All;
            if (all.Count == 0) return null;

            if (!string.IsNullOrEmpty(OnlyMapId))
            {
                var only = MeshMapCatalog.GetById(OnlyMapId);
                if (only != null)
                {
                    MapHistoryStore.MarkSeen(only.MapId);
                    return only;
                }
            }

            var unseen = new List<MeshMapDefinition>();
            foreach (var map in all)
            {
                if (!MapHistoryStore.HasSeen(map.MapId)) unseen.Add(map);
            }

            var pool = unseen.Count > 0 ? unseen : all;
            var chosen = pool[Random.Range(0, pool.Count)];
            MapHistoryStore.MarkSeen(chosen.MapId);
            return chosen;
        }
    }
}
