using UnityEditor;
using UnityEngine;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// The user, 2026-11-13: opening the project in the Editor runs, once per Editor session, «Balance → Pull From
    /// Google Sheets» and then «Setup Project» -- in that order, so the game is generated from the sheet's fresh
    /// numbers (Setup reads the CSVs the pull wrote). Never in batchmode (run.ps1's builds stay on the committed
    /// CSVs). A failed pull still runs Setup on the CSVs there are. Switched off/on by the checked menu item
    /// «CityBuilder/Автозапуск при открытии» (EditorPrefs, per machine).
    /// </summary>
    [InitializeOnLoad]
    public static class AutoSetupOnOpen
    {
        private const string MenuPath = "CityBuilder/Автозапуск при открытии (Pull + Setup)";
        private const string EnabledKey = "CB_AutoSetupOnOpen";
        private const string DoneKey = "CB_AutoSetupOnOpen_Done";

        private static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledKey, true);
            set => EditorPrefs.SetBool(EnabledKey, value);
        }

        static AutoSetupOnOpen()
        {
            // SessionState lives as long as the Editor: domain reloads after a compile do not run it again
            if (Application.isBatchMode || SessionState.GetBool(DoneKey, false)) return;
            EditorApplication.update += WaitUntilReady;
        }

        private static void WaitUntilReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorApplication.update -= WaitUntilReady;
            SessionState.SetBool(DoneKey, true);
            if (!Enabled) return;
            Debug.Log("[Автозапуск] Pull From Google Sheets, затем Setup Project (выключить: меню CityBuilder → Автозапуск при открытии)");
            try { BalanceImporter.Pull(quiet: true); }
            catch (System.Exception e) { Debug.LogError("[Автозапуск] pull failed, Setup runs on the CSVs there are: " + e.Message); }
            SetupProject.Run();
        }

        [MenuItem(MenuPath, priority = 2)]
        private static void Toggle() => Enabled = !Enabled;

        [MenuItem(MenuPath, true)]
        private static bool ToggleCheck()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }
    }
}
