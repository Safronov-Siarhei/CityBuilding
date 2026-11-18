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
        /// Keeps only letters/digits of any alphabet plus space/-/_ (spaces
        /// become underscores), so the result is always a safe file name.
        /// </summary>
        public static string SanitizeName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName)) return string.Empty;

            var builder = new StringBuilder();
            foreach (var c in rawName.Trim())
            {
                // any alphabet's letters, not English only: a Russian name came out empty and the save was never
                // written (the user, 2026-11-18: «я создал своё сохранение, но оно не сохранилось»)
                if (char.IsLetterOrDigit(c))
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

        /// <summary>Removes a save slot. Missing file is not an error -- the caller wanted it gone, and it is.</summary>
        public static void Delete(string name)
        {
            if (string.IsNullOrEmpty(name)) return;

            var path = GetPath(name);
            if (File.Exists(path)) File.Delete(path);
        }

        /// <summary>Moves slot `from` onto slot `to`; if `to` is already there and newer, `from` is just dropped.</summary>
        public static void Replace(string from, string to)
        {
            var src = GetPath(from);
            if (!File.Exists(src)) return;
            var dst = GetPath(to);
            if (File.Exists(dst))
            {
                if (File.GetLastWriteTimeUtc(dst) >= File.GetLastWriteTimeUtc(src)) { File.Delete(src); return; }
                File.Delete(dst);
            }
            File.Move(src, dst);
        }

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

        /// <summary>The slot the town waits in while its heroes are below (Dungeon.DungeonTravel); never listed.</summary>
        public const string HiddenTownSlot = "_dungeon";

        public static List<SaveSlotInfo> ListSaves()
        {
            var result = new List<SaveSlotInfo>();
            if (!Directory.Exists(SavesFolder)) return result;

            foreach (var file in Directory.GetFiles(SavesFolder, "*.json"))
            {
                // the town waiting while its heroes are in a dungeon is not a save of the player's
                if (Path.GetFileNameWithoutExtension(file) == HiddenTownSlot) continue;
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
