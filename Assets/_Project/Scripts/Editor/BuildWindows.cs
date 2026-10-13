using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// Builds the Windows 64-bit player from the command line, the counterpart of BuildAndroid.
    ///
    /// The game is mobile-first; this build exists so the game can be shown and played at a desk
    /// without a phone. TouchInputRouter already speaks mouse, so nothing platform-specific is needed.
    ///
    /// The backend is whatever Standalone is set to (Mono by default) -- only the Mono player is
    /// installed for Windows on this machine, and an IL2CPP setting would fail the build outright.
    /// The output is a folder, not a single file; build_windows.ps1 zips it for sending.
    /// </summary>
    public static class BuildWindows
    {
        private const string OutputFolder = "Builds/Windows";
        private const string ExeName = "CityBuilding.exe";

        /// <summary>Entry point for -executeMethod. Pass -development for the profiler-carrying build.</summary>
        public static void Run()
        {
            var development = HasFlag("-development");

            var scenes = ScenePaths();
            if (scenes.Length == 0)
            {
                Fail("No scenes are enabled in EditorBuildSettings -- run SetupProject first.");
                return;
            }

            // A clean folder each time: leftovers from an older build would ride along in the zip.
            if (Directory.Exists(OutputFolder)) Directory.Delete(OutputFolder, true);
            Directory.CreateDirectory(OutputFolder);
            var exePath = Path.Combine(OutputFolder, ExeName);

            var options = BuildOptions.None;
            if (development) options |= BuildOptions.Development | BuildOptions.ConnectWithProfiler;

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = options,
            };

            Debug.Log($"[BuildWindows] Building {(development ? "DEVELOPMENT" : "release")} player to {exePath}");

            var summary = BuildPipeline.BuildPlayer(buildPlayerOptions).summary;
            if (summary.result != BuildResult.Succeeded)
            {
                Fail($"Build {summary.result} with {summary.totalErrors} error(s) after {summary.totalTime}.");
                return;
            }

            Debug.Log($"[BuildWindows] OK: {exePath}, took {summary.totalTime}.");
            EditorApplication.Exit(0);
        }

        private static string[] ScenePaths()
        {
            var enabled = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene != null && scene.enabled) enabled.Add(scene.path);
            }
            return enabled.ToArray();
        }

        private static bool HasFlag(string flag)
        {
            foreach (var argument in Environment.GetCommandLineArgs())
            {
                if (string.Equals(argument, flag, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        /// <summary>Exits non-zero so the shell script can tell a failed build from a good one.</summary>
        private static void Fail(string message)
        {
            Debug.LogError("[BuildWindows] " + message);
            EditorApplication.Exit(1);
        }
    }
}
