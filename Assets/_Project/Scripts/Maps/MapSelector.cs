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
        /// <summary>Returns null if no maps have been imported into the catalog yet.</summary>
        public static MeshMapDefinition PickForNewGame()
        {
            var all = MeshMapCatalog.All;
            if (all.Count == 0) return null;

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
