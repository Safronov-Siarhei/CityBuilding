using System.IO;
using UnityEngine;

namespace CityBuilder.Saving
{
    public static class SaveSystem
    {
        private const string SaveFileName = "citybuilder_save.json";

        private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static bool HasSave() => File.Exists(SavePath);

        public static void Save(GameSaveData data)
        {
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
        }

        public static GameSaveData Load()
        {
            if (!File.Exists(SavePath)) return null;
            var json = File.ReadAllText(SavePath);
            return JsonUtility.FromJson<GameSaveData>(json);
        }

        public static void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
        }
    }
}
