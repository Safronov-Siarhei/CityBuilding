using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace CityBuilder.Saving
{
    public struct SaveSlotInfo
    {
        public string Name;
        public DateTime LastModifiedUtc;
    }

    public static class SaveSystem
    {
        private const string SavesFolderName = "Saves";
        private const int MaxNameLength = 40;

        private static string SavesFolder => Path.Combine(Application.persistentDataPath, SavesFolderName);

        /// <summary>
        /// Keeps only ASCII letters/digits (English naming, as requested) plus space/-/_ (spaces
        /// become underscores), so the result is always a safe file name.
        /// </summary>
        public static string SanitizeName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName)) return string.Empty;

            var builder = new StringBuilder();
            foreach (var c in rawName.Trim())
            {
                if (c < 128 && char.IsLetterOrDigit(c))
                {
                    builder.Append(c);
                }
                else if (c == ' ' || c == '-' || c == '_')
                {
                    builder.Append('_');
                }
            }

            var result = builder.ToString().Trim('_');
            return result.Length > MaxNameLength ? result.Substring(0, MaxNameLength) : result;
        }

        private static string GetPath(string name) => Path.Combine(SavesFolder, $"{name}.json");

        public static bool SaveExists(string name) => !string.IsNullOrEmpty(name) && File.Exists(GetPath(name));

        public static bool HasAnySave() => Directory.Exists(SavesFolder) && Directory.GetFiles(SavesFolder, "*.json").Length > 0;

        public static void Save(string name, GameSaveData data)
        {
            if (string.IsNullOrEmpty(name)) return;
            Directory.CreateDirectory(SavesFolder);
            File.WriteAllText(GetPath(name), JsonUtility.ToJson(data, true));
        }

        public static GameSaveData Load(string name)
        {
            var path = GetPath(name);
            if (string.IsNullOrEmpty(name) || !File.Exists(path)) return null;
            return JsonUtility.FromJson<GameSaveData>(File.ReadAllText(path));
        }

        public static List<SaveSlotInfo> ListSaves()
        {
            var result = new List<SaveSlotInfo>();
            if (!Directory.Exists(SavesFolder)) return result;

            foreach (var file in Directory.GetFiles(SavesFolder, "*.json"))
            {
                result.Add(new SaveSlotInfo
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    LastModifiedUtc = File.GetLastWriteTimeUtc(file)
                });
            }

            result.Sort((a, b) => b.LastModifiedUtc.CompareTo(a.LastModifiedUtc));
            return result;
        }
    }
}
