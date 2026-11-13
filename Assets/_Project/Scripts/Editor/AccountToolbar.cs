#if UNITY_EDITOR_WIN
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// «Аккаунт» next to Play / Pause / Step in the editor's main toolbar (the user, 2026-11-13: half-way through
    /// campaign level 2 he could not start it over). An account is the game's whole local state -- every PlayerPrefs
    /// key (campaign progress, a mission's saved part, settings) and everything under Application.persistentDataPath
    /// (sandbox saves, records) -- kept in Library/CB_Accounts/&lt;name&gt;/ while another account is in use.
    ///
    /// Switching (only out of Play mode): the current state is stored under the current name, wiped, and the chosen
    /// account's put back -- or nothing, for a new one (a fresh player). «Сбросить» wipes the current account only.
    /// Editor only, Windows only (PlayerPrefs live in the registry there); never in a build.
    /// </summary>
    public static class AccountToolbar
    {
        private const string ElementPath = "CityBuilder/Аккаунт";
        private const string CurrentKey = "CB_Account";
        private const string DefaultName = "Основной";
        private const string PrefsFile = "prefs.json";
        private const string DataFolder = "data";

        private static string Current
        {
            get => EditorPrefs.GetString(CurrentKey, DefaultName);
            set => EditorPrefs.SetString(CurrentKey, value);
        }

        private static string Root => Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "Library", "CB_Accounts");
        private static string RegistryPath => $@"Software\Unity\UnityEditor\{PlayerSettings.companyName}\{PlayerSettings.productName}";

        [MainToolbarElement(ElementPath, defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement Create()
        {
            var content = new MainToolbarContent("Аккаунт: " + Current, "Аккаунт для игры: прогресс кампании, сохранения, настройки");
            return new MainToolbarDropdown(content, ShowMenu);
        }

        private static void ShowMenu(Rect at)
        {
            var menu = new GenericMenu();
            foreach (var name in Accounts())
            {
                var picked = name;
                menu.AddItem(new GUIContent(name), name == Current, () => SwitchTo(picked));
            }
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Новый аккаунт"), false, () => SwitchTo(NextFreeName()));
            menu.AddItem(new GUIContent("Сбросить прогресс «" + Current + "»"), false, ResetCurrent);
            if (Current != DefaultName) menu.AddItem(new GUIContent("Удалить «" + Current + "»"), false, DeleteCurrent);
            else menu.AddDisabledItem(new GUIContent("Удалить «" + Current + "»"));
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Открыть папку аккаунтов"), false, () =>
            {
                Directory.CreateDirectory(Root);
                EditorUtility.RevealInFinder(Root);
            });
            menu.DropDown(at);
        }

        private static List<string> Accounts()
        {
            var names = new List<string> { DefaultName };
            if (Directory.Exists(Root))
                foreach (var dir in Directory.GetDirectories(Root))
                {
                    var name = Path.GetFileName(dir);
                    if (!names.Contains(name)) names.Add(name);
                }
            if (!names.Contains(Current)) names.Add(Current);
            names.Sort((a, b) => a == DefaultName ? -1 : b == DefaultName ? 1 : string.CompareOrdinal(a, b));
            return names;
        }

        private static string NextFreeName()
        {
            var names = Accounts();
            for (var i = 2; ; i++)
            {
                var name = "Аккаунт " + i;
                if (!names.Contains(name)) return name;
            }
        }

        private static bool NotWhilePlaying()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) return true;
            EditorUtility.DisplayDialog("Аккаунт", "Сначала остановите игру (Play), потом меняйте аккаунт.", "Понятно");
            return false;
        }

        private static void SwitchTo(string name)
        {
            if (name == Current || !NotWhilePlaying()) return;
            Store(Current);
            Wipe();
            Restore(name);
            Current = name;
            Refresh();
            Debug.Log($"[Аккаунт] теперь «{name}»");
        }

        private static void ResetCurrent()
        {
            if (!NotWhilePlaying()) return;
            if (!EditorUtility.DisplayDialog("Аккаунт", $"Стереть весь прогресс аккаунта «{Current}»: кампанию, сохранения, настройки?", "Стереть", "Отмена")) return;
            Wipe();
            var dir = Path.Combine(Root, Current);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Debug.Log($"[Аккаунт] «{Current}» сброшен");
        }

        private static void DeleteCurrent()
        {
            if (!NotWhilePlaying()) return;
            var gone = Current;
            if (!EditorUtility.DisplayDialog("Аккаунт", $"Удалить аккаунт «{gone}» насовсем?", "Удалить", "Отмена")) return;
            Wipe();
            var dir = Path.Combine(Root, gone);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Restore(DefaultName);
            Current = DefaultName;
            Refresh();
            Debug.Log($"[Аккаунт] «{gone}» удалён, теперь «{DefaultName}»");
        }

        private static void Refresh() => MainToolbar.Refresh(ElementPath);

        // ------------------------------------------------------------------ the state: PlayerPrefs + persistentDataPath

        [Serializable] private class Pref { public string name; public int kind; public long number; public string text; public string bytes; }
        [Serializable] private class PrefSet { public List<Pref> prefs = new List<Pref>(); }

        private static void Store(string name)
        {
            var dir = Path.Combine(Root, name);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            PlayerPrefs.Save();
            var set = new PrefSet();
            using (var key = Registry.CurrentUser.OpenSubKey(RegistryPath))
            {
                if (key != null)
                    foreach (var value in key.GetValueNames())
                    {
                        var kind = key.GetValueKind(value);
                        var raw = key.GetValue(value, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                        var pref = new Pref { name = value, kind = (int)kind };
                        switch (raw)
                        {
                            case int i: pref.number = i; break;
                            case long l: pref.number = l; break;
                            case byte[] b: pref.bytes = Convert.ToBase64String(b); break;
                            case string s: pref.text = s; break;
                            default: continue;
                        }
                        set.prefs.Add(pref);
                    }
            }
            File.WriteAllText(Path.Combine(dir, PrefsFile), JsonUtility.ToJson(set, true));
            CopyFolder(Application.persistentDataPath, Path.Combine(dir, DataFolder));
        }

        private static void Restore(string name)
        {
            var dir = Path.Combine(Root, name);
            var file = Path.Combine(dir, PrefsFile);
            if (File.Exists(file))
            {
                var set = JsonUtility.FromJson<PrefSet>(File.ReadAllText(file));
                using (var key = Registry.CurrentUser.CreateSubKey(RegistryPath))
                {
                    foreach (var pref in set.prefs)
                    {
                        var kind = (RegistryValueKind)pref.kind;
                        switch (kind)
                        {
                            case RegistryValueKind.DWord: key.SetValue(pref.name, (int)pref.number, kind); break;
                            case RegistryValueKind.QWord: key.SetValue(pref.name, pref.number, kind); break;
                            case RegistryValueKind.Binary: key.SetValue(pref.name, Convert.FromBase64String(pref.bytes ?? string.Empty), kind); break;
                            default: key.SetValue(pref.name, pref.text ?? string.Empty, kind); break;
                        }
                    }
                }
            }
            CopyFolder(Path.Combine(dir, DataFolder), Application.persistentDataPath);
        }

        /// <summary>A fresh player: no PlayerPrefs, nothing under persistentDataPath.</summary>
        private static void Wipe()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            var data = Application.persistentDataPath;
            if (!Directory.Exists(data)) return;
            foreach (var f in Directory.GetFiles(data))
                try { File.Delete(f); } catch (IOException e) { Debug.LogWarning($"[Аккаунт] не удалось стереть {f}: {e.Message}"); }
            foreach (var d in Directory.GetDirectories(data))
                try { Directory.Delete(d, true); } catch (IOException e) { Debug.LogWarning($"[Аккаунт] не удалось стереть {d}: {e.Message}"); }
        }

        private static void CopyFolder(string from, string to)
        {
            if (!Directory.Exists(from)) return;
            Directory.CreateDirectory(to);
            foreach (var f in Directory.GetFiles(from)) File.Copy(f, Path.Combine(to, Path.GetFileName(f)), true);
            foreach (var d in Directory.GetDirectories(from)) CopyFolder(d, Path.Combine(to, Path.GetFileName(d)));
        }
    }
}
#endif
