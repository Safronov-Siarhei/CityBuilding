using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CityBuilder.Maps
{
    [Serializable]
    internal class MapHistoryData
    {
        public List<string> seenMapIds = new List<string>();
    }

    /// <summary>
    /// Tracks which pre-made maps this player has already started a game on, so MapSelector can
    /// prioritize the ones they haven't seen yet. This is player-profile data, independent of
    /// any single save slot (see SaveSystem), so it lives in its own file.
    /// </summary>
    public static class MapHistoryStore
    {
        private const string FileName = "map_history.json";

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        private static MapHistoryData _data;

        public static bool HasSeen(string mapId)
        {
            Load();
            return !string.IsNullOrEmpty(mapId) && _data.seenMapIds.Contains(mapId);
        }

        public static void MarkSeen(string mapId)
        {
            if (string.IsNullOrEmpty(mapId)) return;

            Load();
            if (_data.seenMapIds.Contains(mapId)) return;

            _data.seenMapIds.Add(mapId);
            File.WriteAllText(FilePath, JsonUtility.ToJson(_data));
        }

        private static void Load()
        {
            if (_data != null) return;
            _data = File.Exists(FilePath)
                ? JsonUtility.FromJson<MapHistoryData>(File.ReadAllText(FilePath))
                : new MapHistoryData();
        }
    }
}
