using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Maps
{
    /// <summary>
    /// Which pre-made maps a new game may start on. The player picks one of PlayerMapIds in the
    /// map panel (MapSelectPanelController); PickForNewGame is the default when there is no
    /// choice to make. Only with none of those imported does it fall back to the old unseen-first
    /// random pick across the whole catalog.
    ///
    /// Draws from MeshMapCatalog (hand-authored Blender maps) — the older PNG-driven
    /// MapCatalog/MapDefinition path is still fully intact in the codebase (existing saves that
    /// reference a PNG map id still load fine via MapTerrainGenerator), it's just no longer fed
    /// into new games.
    /// </summary>
    public static class MapSelector
    {
        /// <summary>
        /// The maps a player may pick for a new game, newest first (the first is the default).
        /// Every designed map joins this list (the user, 2026-10-20: «каждая новая карта должна
        /// быть доступна в выборе у игрока»). Maps 1 and 2 stay in the catalog only for old saves.
        /// </summary>
        public static readonly string[] PlayerMapIds = { "Map5", "Map4", "Map3" };

        /// <summary>The pickable maps that are actually imported, in PlayerMapIds order.</summary>
        public static List<MeshMapDefinition> Choosable()
        {
            var maps = new List<MeshMapDefinition>();
            foreach (var id in PlayerMapIds)
            {
                var map = MeshMapCatalog.GetById(id);
                if (map != null) maps.Add(map);
            }
            return maps;
        }

        /// <summary>Records the player's pick so the unseen-first fallback below knows about it.</summary>
        public static void MarkChosen(MeshMapDefinition map)
        {
            if (map != null) MapHistoryStore.MarkSeen(map.MapId);
        }

        /// <summary>Returns null if no maps have been imported into the catalog yet.</summary>
        public static MeshMapDefinition PickForNewGame()
        {
            var all = MeshMapCatalog.All;
            if (all.Count == 0) return null;

            // The default pick, used when the player gets no choice: the newest designed map.
            var choosable = Choosable();
            if (choosable.Count > 0)
            {
                MarkChosen(choosable[0]);
                return choosable[0];
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
